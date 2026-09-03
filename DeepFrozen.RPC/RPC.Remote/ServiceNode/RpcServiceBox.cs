using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.IO.Utils;
using DeepCore.Json;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepCrystal;
using DeepCrystal.RPC;
using DeepCrystal.Threading;
using DeepCrystal.Threading.Timer;
using DeepFrozen.RPC.Invoker;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    public class RpcServiceBox : DeepCrystal.RPC.IServiceProvider
    {
        //--------------------------------------------------------------------------------------------
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("RPC:ServiceBox");
        public static int ActiveCount { get { return Alloc.ActiveCount; } }
        public static int AllocCount { get { return Alloc.AllocCount; } }
        //--------------------------------------------------------------------------------------------
        internal readonly Logger log;
        internal readonly Type serviceType;
        internal readonly RpcServiceNode currentNode;
        internal readonly string serviceName;
        internal readonly DateTime startTimeUTC;
        internal readonly bool isStatic;
        internal readonly ServiceProperties serviceProperties;
        private readonly Properties config;
        private readonly RemoteAddress address;
        private readonly RemoteAddress creator_address;
        private readonly RemoteProxyInfo localInfo;
        private IService service;
        private RpcServiceInvoker invokes;
        private List<IDisposable> waittingTimer = new List<IDisposable>();
        private List<IDisposable> autoDisposables = new List<IDisposable>();
        //---------------------------------------------------------------------------------------------------------------------------------
        private bool is_disposing = false;
        private bool is_disposed = false;
        private bool is_starting = false;
        private bool is_started = false;
        private bool is_self_closing = false;
        //---------------------------------------------------------------------------------------------------------------------------------
        public bool IsDisposing { get => is_disposing; }
        public bool IsDisposed { get => is_disposed; }
        public bool IsStarted { get => is_started; }
        public RemoteAddress Address { get => address; }
        public Properties Config { get => config; }
        public DateTime StartTimeUTC { get => startTimeUTC; }
        public RemoteAddress CreatorAddress { get => creator_address; }
        public IRemoteServiceInfo LocalServiceInfo { get => localInfo; }
        //---------------------------------------------------------------------------------------------------------------------------------
        internal RpcServiceBox(RemoteAddress addr, IDictionary<string, string> config, Type svcType, RpcServiceNode node, RemoteAddress from, bool isStatic)
        {
            Alloc.RecordConstructor("B:" + svcType.ToVisibleName());
            this.isStatic = isStatic;
            this.startTimeUTC = DateTime.UtcNow;
            this.config = new Properties(config);
            this.currentNode = node;
            this.serviceType = svcType;
            this.creator_address = from;
            this.serviceName = addr.ServiceName;
            this.address = new RemoteAddress(addr.ServiceName, node.NodeName, addr.ServiceType);
            this.log = LoggerFactory.GetLogger(addr.FullPath);
            this.invokes = node.InvokeManager.GetServiceInvoker(serviceType);
            this.push_handler = new PushInvokers(node.RpcCodec);
            this.localInfo = new RemoteProxyInfo()
            {
                Address = new RemoteAddressInfo(address),
                Config = new Properties(config),
                EndPoint = node.NodeInfo.EndPoint,
                IsStatic = isStatic,
                StartTimeUTC = startTimeUTC,
            };
            try
            {
                var start = new ServiceStartInfo(node.Application, this, address, config, node.RpcCodec, from, isStatic, node.SharedMemory);
                this.service = (IService)DeepActivator.CreateInstance(serviceType, new object[] { start });
                this.serviceProperties = service.Properties;
            }
            catch (Exception err)
            {
                log.Error(
                    "[CreateInstance error]\n" +
                    address.ToString() + "\n" +
                    config.ToString() +
                    err.Message, err);
                throw;
            }
            if (node.TaskScheduler != null)
            {
                if (!serviceProperties.IsConcurrent)
                    this.request_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(process_rpc_request),
                        new ExecutionDataflowBlockOptions() { TaskScheduler = node.TaskScheduler, });
                this.main_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(process_rpc_main),
                    new ExecutionDataflowBlockOptions() { TaskScheduler = node.TaskScheduler, });
            }
            else
            {
                if (!serviceProperties.IsConcurrent)
                    this.request_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(process_rpc_request));
                this.main_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(process_rpc_main));
            }
            if (!serviceProperties.IsConcurrent)
            {
                this.request_lock = new SemaphoreSlim(1, 1);
            }
        }
        ~RpcServiceBox()
        {
            Alloc.RecordDestructor("B:" + serviceType.ToVisibleName());
        }
        private void DoDispose()
        {
            try
            {
                try
                {
                    DisposeEvents();
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
                lock (this)
                {
                    try
                    {
                        if (this.request_lock != null) this.request_lock.Dispose();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    try
                    {
                        var dlist = new List<IDisposable>();
                        lock (autoDisposables)
                        {
                            dlist.AddRange(autoDisposables);
                            autoDisposables.Clear();
                        }
                        Task.Run(async () =>
                        {
                            foreach (var d in dlist)
                            {
                                try
                                {
                                    if (d is AsyncDisposable ad)
                                    {
                                        await ad.DisposeAsync();
                                    }
                                    else
                                    {
                                        d.Dispose();
                                    }
                                }
                                catch (Exception err)
                                {
                                    log.Error(err.Message, err);
                                }
                            }
                        }).Wait();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    try
                    {
                        (this.service as IDisposable).Dispose();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    if (modules != null)
                    {
                        modules.Clear();
                    }
                    this.request_lock = null;
                    this.service = null;
                    this.invokes = null;
                }
            }
            finally
            {
                this.is_disposed = true;
                Alloc.RecordDispose("B:" + serviceType.ToVisibleName());
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        #region Action
        internal void s2r_RemoteCall<RSP>(IRemoteService proxy, ISerializable req, OnRpcReturn<RSP> callback, StackTrace trace) where RSP : ISerializable
        {
            currentNode.s2r_RpcRequest(this.Address, proxy, req, cb_Call);
            void cb_Call(ISerializable rsp, Exception err)
            {
                var evt_rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_OBJ, this.Address, this.Address);
                try
                {
                    evt_rsp.State = rsp;
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                        {
                            evt_rsp.AppendError(err);
                        }
                        else
                        {
                            evt_rsp.AppendError(LogError(err, trace));
                        }
                    }
                    evt_rsp.SetCallbackRsp(callback);
                    this.PostResponse(evt_rsp);
                }
                catch (Exception err2)
                {
                    evt_rsp.Dispose();
                    callback(default(RSP), LogError(err2, trace));
                }
            }
        }
        internal void s2r_RemoteCall(IRemoteService proxy, BinaryMessage req, OnRpcReturnBinary callback, StackTrace trace)
        {
            currentNode.s2r_RpcRequest(this.Address, proxy, req, cb_Call);
            void cb_Call(BinaryMessage rsp, Exception err)
            {
                var evt_rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_BIN, this.Address, this.Address);
                try
                {
                    evt_rsp.State = rsp;
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                        {
                            evt_rsp.AppendError(err);
                        }
                        else
                        {
                            evt_rsp.AppendError(LogError(err, trace));
                        }
                    }
                    evt_rsp.SetCallbackRsp(callback);
                    this.PostResponse(evt_rsp);
                }
                catch (Exception err2)
                {
                    evt_rsp.Dispose();
                    callback(BinaryMessage.NULL, LogError(err2, trace));
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------
        internal void s2r_RemoteCall(IRemoteService proxy, ISerializable req, OnRpcReturnVoid callback, StackTrace trace)
        {
            currentNode.s2r_RpcRequest(this.Address, proxy, req, cb_Call);
            void cb_Call(Exception err)
            {
                var evt_rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_VOID, this.Address, this.Address);
                try
                {
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                        {
                            evt_rsp.AppendError(err);
                        }
                        else
                        {
                            evt_rsp.AppendError(LogError(err, trace));
                        }
                    }
                    evt_rsp.SetCallbackRsp(callback);
                    this.PostResponse(evt_rsp);
                }
                catch (Exception err2)
                {
                    evt_rsp.Dispose();
                    callback(LogError(err2, trace));
                }
            }
        }
        internal void s2r_RemoteCall(IRemoteService proxy, BinaryMessage req, OnRpcReturnVoid callback, StackTrace trace)
        {
            currentNode.s2r_RpcRequest(this.Address, proxy, req, cb_Call);
            void cb_Call(BinaryMessage rsp, Exception err)
            {
                var evt_rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_VOID, this.Address, this.Address);
                try
                {
                    evt_rsp.State = rsp;
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                        {
                            evt_rsp.AppendError(err);
                        }
                        else
                        {
                            evt_rsp.AppendError(LogError(err, trace));
                        }
                    }
                    evt_rsp.SetCallbackRsp(callback);
                    this.PostResponse(evt_rsp);
                }
                catch (Exception err2)
                {
                    evt_rsp.Dispose();
                    callback(LogError(err2, trace));
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------
        internal void r2s_PushNotify(in RemoteAddress from, ISerializable req)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_OBJ, from, this.address);
            try
            {
                evt.State = req;
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (!serviceProperties.IgnoreRequestError)
                    log.Warn(err.Message);
            }
        }
        internal void r2s_PushNotify(in RemoteAddress from, BinaryMessage req)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_BIN, from, this.address);
            try
            {
                evt.State = req;
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (!serviceProperties.IgnoreRequestError)
                    log.Warn(err.Message);
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        internal void r2s_PushBatchNotify(in RemoteAddress from, ICollection<ISerializable> req)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_OBJ, from, this.address);
            try
            {
                evt.State = req;
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (!serviceProperties.IgnoreRequestError)
                    log.Warn(err.Message);
            }
        }
        internal void r2s_PushBatchNotify(in RemoteAddress from, ICollection<BinaryMessage> req)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_BIN, from, this.address);
            try
            {
                evt.State = req;
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (!serviceProperties.IgnoreRequestError)
                    log.Warn(err.Message);
            }
        }
        internal void r2s_PushWormhole(in RemoteAddress from, object message)
        {
            try
            {
                if (this.service != null)
                {
                    this.InvokeWormholeTransported(from, message);
                }
                else
                {
                    log.Warn($"No Wormhole Handler Service : {from} : {message}");
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        internal Task<object> r2s_PushWormholeAsync(in RemoteAddress from, object message)
        {
            try
            {
                if (this.service != null)
                {
                    return this.InvokeWormholeTransportedAsync(from, message);
                }
                else
                {
                    log.Warn($"No Wormhole Handler Service : {from} : {message}");
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return Task.FromResult<object>(null);
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        private Exception LogError(Exception err)
        {
            log.Error(err);
            return err;
        }
        private Exception LogError(Exception err, StackTrace trace)
        {
            var rerr = new RpcException(err, trace);
            log.Error(rerr);
            return rerr;
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        internal Task Execute(Action action, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<int>($"{this.address}::Execute(action:{action})", addTimeoutMS, trace);
            try
            {
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(LogError(err, trace));
                }
                try
                {
                    action();
                    taskCompletion.TrySetResult(0);
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task Execute<TInput>(Action<TInput> action, TInput state, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<int>($"{this.address}::Execute(action:{action}, state:{state})", addTimeoutMS, trace);
            try
            {
                evt.State = state;
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(LogError(err, trace));
                }
                try
                {
                    action((TInput)st);
                    taskCompletion.TrySetResult(0);
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TInput, TResult>(Func<TInput, TResult> function, TInput state, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(function:{function}, state:{state})", addTimeoutMS, trace);
            try
            {
                evt.State = state;
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(LogError(err, trace));
                }
                try
                {
                    var result = function((TInput)st);
                    taskCompletion.TrySetResult(result);
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TResult>(Func<TResult> function, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(function:{function})", addTimeoutMS, trace);
            try
            {
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(LogError(err, trace));
                }
                try
                {
                    var result = function();
                    taskCompletion.TrySetResult(result);
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task ExecuteAsync(Func<Task> function, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<int>($"{this.address}::ExecuteAsync(function:{function})", addTimeoutMS, trace);
            try
            {
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var task = function();
                    task.ContinueWith(t =>
                    {
                        var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                        try
                        {
                            evt2.State = 0;
                            if (t.Exception != null)
                            {
                                evt2.AppendError(LogError(t.Exception, trace));
                            }
                            evt2.SetCallbackTcs(taskCompletion);
                            this.PostResponse(evt2);
                        }
                        catch (Exception err2)
                        {
                            evt2.Dispose();
                            if (serviceProperties.IgnoreResponseError)
                                taskCompletion.TrySetCanceled();
                            else
                                taskCompletion.TrySetException(LogError(err2, trace));
                        }
                    });
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task ExecuteAsync<TInput>(Func<TInput, Task> function, TInput state, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<int>($"{this.address}::ExecuteAsync(function:{function}, state:{state})", addTimeoutMS, trace);
            try
            {
                evt.State = state;
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var task = function((TInput)st);
                    task.ContinueWith(t =>
                    {
                        var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                        try
                        {
                            evt2.State = 0;
                            if (t.Exception != null)
                            {
                                evt2.AppendError(LogError(t.Exception, trace));
                            }
                            evt2.SetCallbackTcs(taskCompletion);
                            this.PostResponse(evt2);
                        }
                        catch (Exception err2)
                        {
                            evt2.Dispose();
                            if (serviceProperties.IgnoreResponseError)
                                taskCompletion.TrySetCanceled();
                            else
                                taskCompletion.TrySetException(LogError(err2, trace));
                        }
                    });
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> function, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(function:{function})", addTimeoutMS, trace);
            try
            {
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var task = function();
                    task.ContinueWith(t =>
                    {
                        var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                        try
                        {
                            evt2.State = t.GetResultAs();
                            if (t.Exception != null)
                            {
                                evt2.AppendError(LogError(t.Exception, trace));
                            }
                            evt2.SetCallbackTcs(taskCompletion);
                            this.PostResponse(evt2);
                        }
                        catch (Exception err2)
                        {
                            evt2.Dispose();
                            if (serviceProperties.IgnoreResponseError)
                                taskCompletion.TrySetCanceled();
                            else
                                taskCompletion.TrySetException(LogError(err2, trace));
                        }
                    });
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(function:{function}, state:{state})", addTimeoutMS, trace);
            try
            {
                evt.State = state;
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(LogError(err));
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var task = function((TInput)st);
                    task.ContinueWith(t =>
                    {
                        var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                        try
                        {
                            evt2.State = t.GetResultAs();
                            if (t.Exception != null)
                            {
                                evt2.AppendError(LogError(t.Exception, trace));
                            }
                            evt2.SetCallbackTcs(taskCompletion);
                            this.PostResponse(evt2);
                        }
                        catch (Exception err2)
                        {
                            evt2.Dispose();
                            if (serviceProperties.IgnoreResponseError)
                                taskCompletion.TrySetCanceled();
                            else
                                taskCompletion.TrySetException(LogError(err2, trace));
                        }
                    });
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TResult>(Task<TResult> task, TimeSpan addTimeoutMS)
        {
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                return task;
            }
            var trace = RpcStatistics.AllocTrace();
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(task:{task})", addTimeoutMS, trace);
            try
            {
                task.ContinueWith(t =>
                {
                    var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                    try
                    {
                        if (t.Exception != null)
                        {
                            evt2.AppendError(LogError(t.Exception, trace));
                        }
                        evt2.State = t.GetResultAs();
                        evt2.SetCallbackTcs(taskCompletion);
                        this.PostResponse(evt2);
                    }
                    catch (Exception err)
                    {
                        evt2.Dispose();
                        if (serviceProperties.IgnoreResponseError)
                            taskCompletion.TrySetCanceled();
                        else
                            taskCompletion.TrySetException(LogError(err, trace));
                    }
                });
            }
            catch (Exception error)
            {
                taskCompletion.TrySetException(LogError(error, trace));
            }
            return taskCompletion.Task;
        }
        internal Task ExecuteAsync(Task task, TimeSpan addTimeoutMS)
        {
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                return task;
            }
            var trace = RpcStatistics.AllocTrace();
            var taskCompletion = this.CreateTaskCompletionSource<int>($"{this.address}::ExecuteAsync(task:{task})", addTimeoutMS, trace);
            try
            {
                task.ContinueWith(t =>
                {
                    var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                    try
                    {
                        evt2.State = 0;
                        if (t.Exception != null)
                        {
                            evt2.AppendError(LogError(t.Exception, trace));
                        }
                        evt2.SetCallbackTcs(taskCompletion);
                        this.PostResponse(evt2);
                    }
                    catch (Exception err)
                    {
                        evt2.Dispose();
                        if (serviceProperties.IgnoreResponseError)
                            taskCompletion.TrySetCanceled();
                        else
                            taskCompletion.TrySetException(LogError(err, trace));
                    }
                });
            }
            catch (Exception error)
            {
                taskCompletion.TrySetException(LogError(error, trace));
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteFromResult<TResult>(TResult state, TimeSpan addTimeoutMS)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, this.address, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<TResult>($"{this.address}::ExecuteAsync(state:{state})", addTimeoutMS, trace);
            try
            {
                evt.State = state;
                evt.SetCallback(cb_Callback);
                this.PostResponse(evt);
            }
            catch (Exception err)
            {
                LogError(err);
                if (serviceProperties.IgnoreResponseError)
                    taskCompletion.TrySetCanceled();
                else
                    taskCompletion.TrySetException(err);
                evt.Dispose();
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    taskCompletion.TrySetResult(state);
                }
                catch (Exception error)
                {
                    taskCompletion.TrySetException(LogError(error, trace));
                }
            }
            return taskCompletion.Task;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------
        #region ActionBlock

        internal Task<bool> n2s_HandleStartAsync(RemoteAddress from)
        {
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_START, from, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<bool>($"{this.address}::HandleStartAsync(from:{from})", Timeout.InfiniteTimeSpan, trace);
            try
            {
                evt.SetCallbackTcs(taskCompletion);
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            return taskCompletion.Task;
        }
        internal Task<bool> n2s_HandleDestoryAsync(RemoteAddress from, string reason)
        {
            this.ClearTimer();
            var trace = RpcStatistics.AllocTrace();
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_DESTORY, from, this.address);
            var taskCompletion = this.CreateTaskCompletionSource<bool>($"{this.address}::HandleDestoryAsync(from:{from}, reason:{reason})", Timeout.InfiniteTimeSpan, trace);
            try
            {
                evt.State = reason;
                evt.SetCallbackTcs(taskCompletion);
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                evt.Dispose();
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            return taskCompletion.Task;
        }
        private void DestorySelf(string reason)
        {
            if (!is_self_closing)
            {
                is_self_closing = true;
                Task.Run(async () =>
                {
                    try
                    {
                        await currentNode.s2n2r_RpcShutdownSelfAsync(this.address, reason);
                    }
                    catch (Exception err)
                    {
                        log.Warn(err.Message);
                    }
                });
            }
        }
        private void DestoryOnError(Exception err, RemoteAddress from)
        {
            try
            {
                this.log.Warn("Service Disposing On Error ...");
                lock (this)
                {
                    this.is_disposing = true;
                    this.ClearPushHandler();
                    this.ClearTimer();
                }
                Task.Run(async () =>
                {
                    if (!serviceProperties.IsConcurrent)
                    {
                        try
                        {
                            this.request_action.Complete();
                            await this.request_action.Completion;
                        }
                        catch
                        {
                            log.Error(err.Message, err);
                        }
                    }
                    try
                    {
                        //执行Destory//
                        await (this.service as IServiceStop).StopAsync(new ServiceStopInfo(ServiceStopInfo.ShutdownEvent.START_ERROR, from, err, err.Message));
                    }
                    catch (Exception err2)
                    {
                        log.Error(err2.Message, err2);
                    }
                    try
                    {
                        this.DoDispose();
                        this.log.Warn("Service Destoryed!!!");
                    }
                    catch (Exception err3)
                    {
                        log.Error(err3.Message, err3);
                    }
                    try
                    {
                        this.main_action.Complete();
                        await this.main_action.Completion;
                    }
                    catch (Exception err2)
                    {
                        log.Error(err2.Message, err2);
                    }
                });
            }
            catch (Exception err4)
            {
                log.Error(err4.Message, err4);
            }
        }

        private ActionBlock<RpcMessage> request_action;
        private ActionBlock<RpcMessage> main_action;
        private SemaphoreSlim request_lock;

        private void RpcLockRelease()
        {
            var rlock = request_lock;
            if (rlock != null)
            {
                try
                {
                    rlock.Release();
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            }
        }
        private Task RpcLockAsync()
        {
            var rlock = request_lock;
            if (rlock != null)
            {
                return rlock.WaitAsync();
            }
            return Task.CompletedTask;
        }

        internal void PostRequest(RpcMessage msg)
        {
            lock (this)
            {
                if (is_disposed == true)
                    throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
                if (is_disposing == true)
                    throw new Exception($"RPC Error : Service Disposing : {this.address.ServiceName} \n{msg}");
                if (service == null)
                    throw new Exception($"Service '{address}' Not Init !!!");
                if (msg.Event == RpcEvent.REQUEST_START)
                {
                    if (is_starting)
                    {
                        throw new Exception($"RPC Error : Service Already Start : {this.address.ServiceName} \n{msg}");
                    }
                    else
                    {
                        is_starting = true;
                    }
                }
                if (msg.Event == RpcEvent.REQUEST_DESTORY)
                {
                    this.is_disposing = true;
                    this.ClearPushHandler();
                    this.ClearTimer();
                    if (!serviceProperties.IsConcurrent)
                    {
                        this.request_action.Complete();
                        this.request_action.Completion.ContinueWith(t =>
                        {
                            main_action.Post(msg);
                        });
                        return;
                    }
                }
            }
            if (serviceProperties.IsConcurrent)
            {
                if (main_action.Post(msg) == false)
                {
                    throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
                }
            }
            else
            {
                if (request_action.Post(msg) == false)
                {
                    throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
                }
            }
        }
        internal void PostResponse(RpcMessage msg)
        {
            //             if (is_disposed == true)
            //                 throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
            //             if (is_disposing == true)
            //                 throw new Exception($"RPC Error : Service Disposing : {this.address.ServiceName} \n{msg}");
            //             if (service == null)
            //                 throw new Exception($"Service '{address}' Not Init !!!");
            if (main_action.Post(msg) == false)
            {
                throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
            }
        }

        //---------------------------------------------------------------------------------------------

        private void process_rpc_request(RpcMessage msg)
        {
            try
            {
                RpcLockAsync().ContinueWith(t =>
                {
                    try
                    {
                        if (main_action.Post(msg) == false)
                        {
                            throw new Exception($"RPC Error : Service Destroyed : {this.address.ServiceName} \n{msg}");
                        }
                    }
                    catch (Exception err)
                    {
                        try
                        {
                            log.Error(err.Message, err);
                            msg.InvokeError(err);
                        }
                        finally
                        {
                            msg.Dispose();
                            RpcLockRelease();
                        }
                    }
                });
            }
            catch (Exception err)
            {
                try
                {
                    log.Error(err.Message, err);
                    msg.InvokeError(err);
                }
                finally
                {
                    msg.Dispose();
                }
            }
        }
        private void process_rpc_main(RpcMessage msg)
        {
            try
            {
                switch (msg.Event)
                {
                    case RpcEvent.REQUEST_START: rpc_Start(msg); break;
                    case RpcEvent.REQUEST_DESTORY: rpc_Destory(msg); break;
                    case RpcEvent.REQUEST_OBJ: rpc_RequestObj(msg); break;
                    case RpcEvent.REQUEST_BIN: rpc_RequestBin(msg); break;
                    case RpcEvent.REQUEST_NOTIFY_OBJ: rpc_NotifyObj(msg); break;
                    case RpcEvent.REQUEST_NOTIFY_BIN: rpc_NotifyBin(msg); break;
                    case RpcEvent.REQUEST_BATCH_NOTIFY_OBJ: rpc_BatchNotifyObj(msg); break;
                    case RpcEvent.REQUEST_BATCH_NOTIFY_BIN: rpc_BatchNotifyBin(msg); break;

                    case RpcEvent.RESPONES_CALLBACK: main_Callback(msg); break;
                    case RpcEvent.RESPONES_EXECUTE: main_Exe(msg); break;
                    case RpcEvent.RESPONSE_OBJ: main_ResponseObj(msg); break;
                    case RpcEvent.RESPONSE_BIN: main_ResponseBin(msg); break;
                    case RpcEvent.RESPONSE_VOID: main_ResponseVoid(msg); break;
                    default:
                        log.Error("Unknow RPC Event : " + msg);
                        break;
                }
            }
            catch (Exception err)
            {
                msg.AppendError(err);
                err = msg.Error;
                log.Error(err.Message, err);
            }
            finally
            {
                msg.Dispose();
            }
        }

        //---------------------------------------------------------------------------------------------
        private void rpc_Start(RpcMessage msg)
        {
            this.log.Info("Service Starting...");
            var from = msg.From;
            var callback = msg.GetCallback();
            try
            {
                (this.service as IServiceStart).StartAsync().ContinueWith(task =>
               {
                   lock (this)
                   {
                       this.is_starting = false;
                       this.is_started = true;
                   }
                   if (task.Exception != null)
                   {
                       log.Error(task.Exception.Message, task.Exception);
                       //TODO stop action block and Destory
                       try
                       {
                           callback(false, task.Exception);
                           this.log.Error("Service Start Error!!!");
                       }
                       catch (Exception err)
                       {
                           log.Error(err.Message, err);
                       }
                       finally
                       {
                           RpcLockRelease();
                       }
                       this.DestoryOnError(task.Exception, msg.From);
                   }
                   else
                   {
                       try
                       {
                           callback(true, task.Exception);
                           this.log.Info("Service Start Over!!!");
                       }
                       catch (Exception err)
                       {
                           log.Error(err.Message, err);
                       }
                       finally
                       {
                           RpcLockRelease();
                       }
                       var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.RESPONES_EXECUTE, from, this.address);
                       try
                       {
                           evt2.SetCallback(cb_TaskComplete);
                           this.PostResponse(evt2);
                       }
                       catch (Exception err2)
                       {
                           evt2.Dispose();
                           cb_TaskComplete(null, err2);
                           //log.Error(err2.Message, err2);
                       }
                       void cb_TaskComplete(object st2, Exception err2)
                       {
                           this.log.Info("Service Started!!!");
                       }
                   }

               });
            }
            catch (Exception err)
            {
                try
                {
                    log.Error(err.Message, err);
                    callback(false, err);
                }
                finally
                {
                    RpcLockRelease();
                }
                this.DestoryOnError(err, msg.From);
            }
        }
        private void rpc_Destory(RpcMessage msg)
        {
            this.log.Warn("Service Disposing...");
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_DESTORY, msg.From, msg.To);
            try
            {
                evt.State = msg.State;
                evt.AppendError(msg.Error);
                evt.SetCallback(msg.GetCallback());
                //执行Destory//
                var s_evt = ServiceStopInfo.ShutdownEvent.RPC_SHUTDOWN;
                if (msg.From.ServiceName == this.address.ServiceName)
                {
                    s_evt = ServiceStopInfo.ShutdownEvent.SELF_SHUTDOWN;
                }
                (this.service as IServiceStop).StopAsync(new ServiceStopInfo(s_evt, msg.From, null, Convert.ToString(evt.State))).ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        log.Error(task.Exception.Message, task.Exception);
                        evt.AppendError(task.Exception);
                    }
                    do_Dispose();
                });
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                evt.AppendError(err);
                do_Dispose();
            }
            void do_Dispose()
            {
                Task.Run(async () => // rpc_Response, SetResult was owner Task //
                {
                    try
                    {
                        this.DoDispose();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    finally
                    {
                        this.log.Warn("Service Destoryed!!!");
                    }
                    try
                    {
                        this.main_action.Complete();
                        await this.main_action.Completion;
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    try
                    {
                        evt.Invoke(true, evt.Error);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    finally
                    {
                        evt.Dispose();
                    }
                });
            }
        }
        private void rpc_NotifyObj(RpcMessage msg)
        {
            try
            {
                var obj = (ISerializable)msg.State;
                push_handler.Notify(obj);
                rpc_Invoke(msg, obj, cb_Notify);
            }
            finally
            {
                RpcLockRelease();
            }
            void cb_Notify(ISerializable ret, Exception err)
            {
                if (err != null)
                {
                    if (err is NoHandlerException)
                        log.Warn(err.Message);
                    else
                        log.Error(err.Message, err);
                }
            }
        }
        private void rpc_NotifyBin(RpcMessage msg)
        {
            try
            {
                var bin = (BinaryMessage)msg.State;
                push_handler.Notify(bin);
                rpc_Invoke(msg, bin, cb_Notify);
            }
            finally
            {
                RpcLockRelease();
            }
            void cb_Notify(BinaryMessage ret, Exception err)
            {
                if (err != null)
                {
                    if (err is NoHandlerException)
                        log.Warn(err.Message);
                    else
                        log.Error(err.Message, err);
                }
            }
        }
        private void rpc_BatchNotifyObj(RpcMessage msg)
        {
            try
            {
                var list = (ISerializable[])msg.State;
                foreach (var obj in list)
                {
                    push_handler.Notify(obj);
                    rpc_Invoke(msg, obj, cb_Notify);
                }
            }
            finally
            {
                RpcLockRelease();
            }
            void cb_Notify(ISerializable ret, Exception err)
            {
                if (err != null)
                {
                    if (err is NoHandlerException)
                        log.Warn(err.Message);
                    else
                        log.Error(err.Message, err);
                }
            }
        }
        private void rpc_BatchNotifyBin(RpcMessage msg)
        {
            try
            {
                var list = (BinaryMessage[])msg.State;
                foreach (var bin in list)
                {
                    push_handler.Notify(bin);
                    rpc_Invoke(msg, bin, cb_Notify);
                }
            }
            finally
            {
                RpcLockRelease();
            }
            void cb_Notify(BinaryMessage ret, Exception err)
            {
                if (err != null)
                {
                    if (err is NoHandlerException)
                        log.Warn(err.Message);
                    else
                        log.Error(err.Message, err);
                }
            }
        }
        private void rpc_RequestObj(RpcMessage msg)
        {
            var _obj = (ISerializable)msg.State;
            var _callback_obj = msg.GetCallback();
            try
            {
                rpc_Invoke(msg, _obj, cb_Request);
            }
            catch (Exception err)
            {
                RpcLockRelease();
                _callback_obj(null, err);
            }
            void cb_Request(ISerializable ret, Exception err)
            {
                try
                {
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                            log.Warn(err.Message);
                        else
                            log.Error(err.Message, err);
                    }
                    _callback_obj(ret, err);
                }
                finally
                {
                    RpcLockRelease();
                }
            }
        }
        private void rpc_RequestBin(RpcMessage msg)
        {
            var _bin = (BinaryMessage)msg.State;
            var _callback_bin = msg.GetCallback();
            try
            {
                rpc_Invoke(msg, _bin, cb_Request);
            }
            catch (Exception err)
            {
                RpcLockRelease();
                _callback_bin(BinaryMessage.NULL, err);
            }
            void cb_Request(BinaryMessage ret, Exception err)
            {
                try
                {
                    if (err != null)
                    {
                        if (err is NoHandlerException)
                            log.Warn(err.Message);
                        else
                            log.Error(err.Message, err);
                    }
                    _callback_bin(ret, err);
                }
                finally
                {
                    RpcLockRelease();
                }
            }
        }
        private void main_ResponseObj(RpcMessage msg)
        {
            msg.Invoke((ISerializable)msg.State, msg.Error);
        }
        private void main_ResponseBin(RpcMessage msg)
        {
            msg.Invoke((BinaryMessage)msg.State, msg.Error);
        }
        private void main_ResponseVoid(RpcMessage msg)
        {
            msg.Invoke(null, msg.Error);
        }
        private void main_Exe(RpcMessage msg)
        {
            msg.Invoke(msg.State, msg.Error);
        }
        private void main_Callback(RpcMessage msg)
        {
            msg.Invoke(msg.State, msg.Error);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region PushHandler
        //---------------------------------------------------------------------------------------------------------------------------------
        private PushInvokers push_handler;
        internal PushInvokers PushHander
        {
            get { return push_handler; }
        }
        internal class PushHandler : NotifyInvokers.NotifyHandler, IPushHandler, IPushHandlerBinary
        {
            internal protected PushHandler(NotifyInvokers client, Type route_type, int route_id, Action<ISerializable> cb, Action<BinaryMessage> cbb, bool recursion)
                : base(client, route_type, route_id, cb, cbb, recursion)
            {
            }
            Type IPushHandler.Route { get { return base.RouteType; } }
            int IPushHandlerBinary.Route { get { return base.RouteID; } }
        }
        internal class PushInvokers : NotifyInvokers
        {
            public PushInvokers(IOStreamPool codec) : base(codec)
            {
            }
            protected override NotifyHandler CreatePushHandler(Type route_type, int route_id, Action<ISerializable> cb, Action<BinaryMessage> cbb, bool recursion_base_type)
            {
                return new PushHandler(this, route_type, route_id, cb, cbb, recursion_base_type);
            }
        }
        internal void ClearPushHandler()
        {
            try
            {
                this.event_OnWormholeTransported = null;
                this.event_OnWormholeTransportedAsync = null;
                if (push_handler != null)
                {
                    this.push_handler.ClearPush();
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        private void ClearTimer()
        {
            try
            {
                var dis = new List<IDisposable>();
                lock (waittingTimer)
                {
                    dis.AddRange(waittingTimer);
                    waittingTimer.Clear();
                }
                foreach (var t in dis) { t.Dispose(); }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region Events
        private readonly object event_lock = new object();
        //-------------------------------------------------------------------------------------------------------------------------
        #region event_Destory
        private readonly HashMap<string, Action<RemoteAddress>> event_RemoteDestoryMap = new HashMap<string, Action<RemoteAddress>>();
        private readonly HashSet<Action<RemoteAddress>> event_OnDestoryed = new HashSet<Action<RemoteAddress>>();
        private bool event_RemoteDestoryMapListening = false;
        public event Action<RemoteAddress> OnDestoryed
        {
            add { lock (event_lock) { event_OnDestoryed.Add(value); } }
            remove { lock (event_lock) { event_OnDestoryed.Remove(value); } }
        }

        internal void ListenRemoteDestoryed(string svcName, Action<RemoteAddress> value)
        {
            lock (event_lock)
            {
                if (event_RemoteDestoryMapListening == false)
                {
                    event_RemoteDestoryMapListening = true;
                    this.currentNode.OnHandleRemoteDestoryed += OnRemoteServiceDestoryed;
                }
                if (event_RemoteDestoryMap.TryGetOrCreate(svcName, out var exist, (name) => value))
                {
                    exist += value;
                }
            }
        }
        private void OnRemoteServiceDestoryed(RemoteAddress addr)
        {
            Action<RemoteAddress> action = null;
            lock (event_lock) { event_RemoteDestoryMap.TryGetValue(addr.ServiceName, out action); }
            if (action != null)
            {
                action.Invoke(addr);
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region event_OnWormholeTransported
        private OnWormholeTransportedHandler event_OnWormholeTransported;
        private OnWormholeAsyncTransportedHandler event_OnWormholeTransportedAsync;
        public event OnWormholeTransportedHandler OnWormholeTransported
        {
            add { event_OnWormholeTransported += value; }
            remove { event_OnWormholeTransported -= value; }
        }
        public event OnWormholeAsyncTransportedHandler OnWormholeTransportedAsync
        {
            add { event_OnWormholeTransportedAsync += value; }
            remove { event_OnWormholeTransportedAsync -= value; }
        }
        internal void InvokeWormholeTransported(in RemoteAddress from, object message)
        {
            var count = 0;
            if (event_OnWormholeTransported != null)
            {
                event_OnWormholeTransported.Invoke(from, message);
                count++;
            }
            count += wormhole_Invoke(in from, message);
            if (count == 0)
            {
                log.Warn($"No Wormhole Handler : {from} : {message}");
            }
        }
        internal async Task<object> InvokeWormholeTransportedAsync(RemoteAddress from, object message)
        {
            if (event_OnWormholeTransportedAsync != null)
            {
                var ret = await event_OnWormholeTransportedAsync.Invoke(from, message);
                if (ret != null)
                {
                    return ret;
                }
            }
            {
                var ret = await wormhole_InvokeAsync(from, message);
                if (ret != null)
                {
                    return ret;
                }
            }
            log.Warn($"No Wormhole Handler : {from} : {message}");
            return null;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        private void DisposeEvents()
        {
            this.event_OnWormholeTransported = null;
            this.event_OnWormholeTransportedAsync = null;
            var list = new List<Action<RemoteAddress>>();
            {
                lock (event_lock)
                {
                    if (event_RemoteDestoryMapListening)
                    {
                        this.currentNode.OnHandleRemoteDestoryed -= OnRemoteServiceDestoryed;
                    }
                    this.event_RemoteDestoryMap.Clear();
                    list.AddRange(event_OnDestoryed);
                    event_OnDestoryed.Clear();
                }
                foreach (var act in list)
                {
                    try
                    {
                        act(this.address);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region IServiceProvider
        //-------------------------------------------------------------------------------------------------------------------------

        public void AutoDispose(IEnumerable<IDisposable> disposable)
        {
            lock (autoDisposables)
            {
                var list = disposable.ConvertAll(t => (IDisposable)t);
                autoDisposables.AddRange(list);
            }
        }
        public T AutoDispose<T>(T disposable) where T : IDisposable
        {
            lock (autoDisposables) { autoDisposables.Add(disposable); }
            return disposable;
        }

        void DeepCrystal.RPC.IServiceProvider.ShutdownSelf(string reason)
        {
            this.DestorySelf(reason);
        }
        List<IRemoteServiceInfo> DeepCrystal.RPC.IServiceProvider.GetLocalServices()
        {
            return currentNode.GetAllLocalServicesInfo();
        }
        IAsyncLock DeepCrystal.RPC.IServiceProvider.CreateLock()
        {
            var ret = new ServiceAsyncLock(this);
            AutoDispose(ret);
            return ret;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        #region GetService

        private Properties EncodeConfig(object cfg)
        {
            var config = new Properties();
            if (cfg != null)
            {
                if (cfg.GetType().IsAnonymousType())
                {
                    dynamic exp = cfg;
                    foreach (var k in exp.GetType().GetProperties())
                    {
                        config.PutObject(k.Name, k.GetValue(exp));
                    }
                }
                else if (typeof(IDictionary<string, string>).IsAssignableFrom(cfg.GetType()))
                {
                    var map = (IDictionary<string, string>)cfg;
                    foreach (var e in map)
                    {
                        config.Put(e.Key, e.Value);
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(cfg.GetType()))
                {
                    var map = (IDictionary)cfg;
                    foreach (DictionaryEntry e in map)
                    {
                        config.PutObject(e.Key.ToString(), e.Value);
                    }
                }
                else if (cfg.GetType().IsClass)
                {
                    config.SaveFields(cfg);
                }
                else
                {
                    throw new Exception($"Can Not Encode Config : {cfg.GetType()}");
                }
            }
            return config;
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.GetOrCreateAsync(RemoteAddress path, object cfg)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            var config = EncodeConfig(cfg);
            return this.ExecuteAsync(this.currentNode.s2n_GetOrCreateProxyAsync(this, path, config), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.CreateAsync(RemoteAddress path, object cfg)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            var config = EncodeConfig(cfg);
            return this.ExecuteAsync(this.currentNode.s2n_CreateProxyAsync(this, path, config), Timeout.InfiniteTimeSpan);
        }

        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.GetOrCreateAsync(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            var config = new Properties();
            if (cfg != null)
            {
                config.AddAll(cfg);
            }
            return this.ExecuteAsync(this.currentNode.s2n_GetOrCreateProxyAsync(this, path, config), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.CreateAsync(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            var config = new Properties();
            if (cfg != null)
            {
                config.AddAll(cfg);
            }
            return this.ExecuteAsync(this.currentNode.s2n_CreateProxyAsync(this, path, config), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.GetAsync(RemoteAddress path)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            return this.ExecuteAsync(this.currentNode.s2n_GetProxyAsync(this, path), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.GetStaticAsync(RemoteAddress path)
        {
            if (path.ServiceName == this.serviceName) throw new Exception("Remote Service is self : " + path);
            return this.ExecuteAsync(this.currentNode.s2n_GetStaticServiceAsync(this, path), Timeout.InfiniteTimeSpan);
        }
        Task<int> DeepCrystal.RPC.IServiceProvider.GetServiceCountAsync(string serviceNode, string serviceType)
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetServiceCountAsync(serviceNode, serviceType), Timeout.InfiniteTimeSpan);
        }
        Task<int> DeepCrystal.RPC.IServiceProvider.GetServiceCountWithNodeAsync(string serviceNode)
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetServiceCountAsync(serviceNode, null), Timeout.InfiniteTimeSpan);
        }
        Task<int> DeepCrystal.RPC.IServiceProvider.GetServiceCountWithTypeAsync(string serviceType)
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetServiceCountAsync(null, serviceType), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService[]> DeepCrystal.RPC.IServiceProvider.GetServicesAsync(ICollection<string> servicesName)
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetServicesAsync(this, servicesName), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService[]> DeepCrystal.RPC.IServiceProvider.GetServicesWithAddressPatternAsync(string pattern)
        {
            return this.ExecuteAsync(this.currentNode.s2n_FindServicesWithAddressPatternAsync(this, pattern), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService[]> DeepCrystal.RPC.IServiceProvider.GetServicesWithInfoLinqAsync(string where, string orderBy)
        {
            return this.ExecuteAsync(this.currentNode.s2n_FindServicesWithInfoLinqAsync(this, where, orderBy), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService[]> DeepCrystal.RPC.IServiceProvider.GetStaticServicesAsync()
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetStaticServicesAsync(this), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.FindStaticServiceAsync(Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            return this.ExecuteAsync(this.currentNode.s2n_FindStaticServiceAsync(this, select), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.FindStaticServiceWithTypeAsync(string serviceType, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            return this.ExecuteAsync(this.currentNode.s2n_FindStaticServiceWithTypeAsync(this, serviceType, select), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteService> DeepCrystal.RPC.IServiceProvider.FindStaticServiceWithNodeAsync(string serviceNode, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            return this.ExecuteAsync(this.currentNode.s2n_FindStaticServiceWithNodeAsync(this, serviceNode, select), Timeout.InfiniteTimeSpan);
        }
        Task<IRemoteNodeInfo[]> DeepCrystal.RPC.IServiceProvider.GetStaticNodesInfoAsync()
        {
            return this.ExecuteAsync(this.currentNode.s2n_GetStaticNodesInfoAsync(), Timeout.InfiniteTimeSpan);
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #region Remote
        void DeepCrystal.RPC.IServiceProvider.RemoteCall<RSP>(string serviceName, ISerializable req, OnRpcReturn<RSP> callback)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.Call(req, callback);
                }
                else if (err != null)
                {
                    this.Execute<RSP>((rsp) => callback(rsp, err), default(RSP), TimeSpan.Zero);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteCall(string serviceName, BinaryMessage req, OnRpcReturnBinary callback)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.Call(req, callback);
                }
                else if (err != null)
                {
                    this.Execute<BinaryMessage>((rsp) => callback(rsp, err), BinaryMessage.NULL, TimeSpan.Zero);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteInvoke(string serviceName, ISerializable msg)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.Invoke(msg);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteInvoke(string serviceName, BinaryMessage msg)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.Invoke(msg);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteBatchInvoke(string serviceName, ICollection<ISerializable> batch)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.BatchInvoke(batch);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteBatchInvoke(string serviceName, ICollection<BinaryMessage> batch)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.BatchInvoke(batch);
                }
            });
        }
        void DeepCrystal.RPC.IServiceProvider.RemoteWormholeTransport(string serviceName, object message)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName)).ContinueWithCallback((svc, err) =>
            {
                if (svc != null)
                {
                    svc.WormholeTransport(message);
                }
            });
        }
        async Task<RSP> DeepCrystal.RPC.IServiceProvider.RemoteCallAsync<RSP>(string serviceName, ISerializable req)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            var svc = await this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName));
            return await svc.CallAsync<RSP>(req);
        }
        async Task<BinaryMessage> DeepCrystal.RPC.IServiceProvider.RemoteCallAsync(string serviceName, BinaryMessage req)
        {
            if (serviceName == this.serviceName) throw new Exception("Remote Service is self : " + serviceName);
            var svc = await this.currentNode.s2n_GetProxyAsync(this, new RemoteAddress(serviceName));
            return await svc.CallAsync(req);
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #region Borcast
        void DeepCrystal.RPC.IServiceProvider.BroadcastWithName(ICollection<string> servicesName, ISerializable notify)
        {
            this.currentNode.s2r_RpcBroadcastWithName(this.address, servicesName, notify);
        }
        void DeepCrystal.RPC.IServiceProvider.BroadcastWithNode(string serviceNode, ISerializable notify)
        {
            this.currentNode.s2r_RpcBroadcastWithNodeAndType(this.address, serviceNode, null, notify);
        }
        void DeepCrystal.RPC.IServiceProvider.BroadcastWithType(string serviceType, ISerializable notify)
        {
            this.currentNode.s2r_RpcBroadcastWithNodeAndType(this.address, null, serviceType, notify);
        }
        void DeepCrystal.RPC.IServiceProvider.BroadcastWithNodeAndType(string serviceNode, string serviceType, ISerializable notify)
        {
            this.currentNode.s2r_RpcBroadcastWithNodeAndType(this.address, serviceNode, serviceType, notify);
        }
        void DeepCrystal.RPC.IServiceProvider.Broadcast(ISerializable notify)
        {
            this.currentNode.s2r_RpcBroadcast(this.address, notify);
        }
        void DeepCrystal.RPC.IServiceProvider.WormholeBroadcastWithNode(string serviceNode, object message)
        {
            this.currentNode.s2r_RpcWormholeBroadcastWithNodeAndType(this.address, serviceNode, null, message);
        }
        void DeepCrystal.RPC.IServiceProvider.WormholeBroadcastWithType(string serviceType, object message)
        {
            this.currentNode.s2r_RpcWormholeBroadcastWithNodeAndType(this.address, null, serviceType, message);
        }
        void DeepCrystal.RPC.IServiceProvider.WormholeBroadcastWithNodeAndType(string serviceNode, string serviceType, object message)
        {
            this.currentNode.s2r_RpcWormholeBroadcastWithNodeAndType(this.address, serviceNode, serviceType, message);
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #region Timer       

        //-------------------------------------------------------------------------------------------------------------------------


        public async Task<IDisposable> CreateCornJobAsync(string corn_expression, object state, Action<ICornJobContext> callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            var trace = RpcStatistics.AllocTrace();
            var job = await this.currentNode.CreateCornJobAsync(this.Address, corn_expression, state, cb_timer, missFire);
            lock (waittingTimer) { waittingTimer.Add(job); }
            return await this.ExecuteFromResult(job, TimeSpan.Zero);
            void cb_timer(ICornJobContext st)
            {
                var evt = RpcMessage.AllocRetain(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                {
                    evt.State = st;
                    evt.SetCallback((rsp, err) =>
                    {
                        try
                        {
                            callback(st);
                        }
                        finally
                        {
                            RpcLockRelease();
                        }
                    });
                }
                try
                {
                    this.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    LogError(err, trace);
                }
            }
        }
        public async Task<IDisposable> CreateCornJobAsync(string corn_expression, object state, Func<ICornJobContext, Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            var trace = RpcStatistics.AllocTrace();
            var job = await this.currentNode.CreateCornJobAsync(this.Address, corn_expression, state, cb_timer, missFire);
            lock (waittingTimer) { waittingTimer.Add(job); }
            return await this.ExecuteFromResult(job, TimeSpan.Zero);
            void cb_timer(ICornJobContext st)
            {
                var evt = RpcMessage.AllocRetain(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                {
                    evt.State = st;
                    evt.SetCallback((rsp, err) =>
                    {
                        callbackAsync(st).ContinueWith(t =>
                        {
                            RpcLockRelease();
                        });
                    });
                }
                try
                {
                    this.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    LogError(err, trace);
                }
            }
            //             return this.CreateCornJobAsync(corn_expression, state, st =>
            //             {
            //                 callbackAsync(st).ContinueWith(t =>
            //                 {
            //                     if (t.Exception != null)
            //                     {
            //                         log.Error(t.Exception);
            //                     }
            //                 });
            //             }, missFire);
        }
        public Task<IDisposable> CreateCornJobAsync(string corn_expression, Action<ICornJobContext> callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            return this.CreateCornJobAsync(corn_expression, null, callback, missFire);
        }
        public Task<IDisposable> CreateCornJobAsync(string corn_expression, Func<ICornJobContext, Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            return this.CreateCornJobAsync(corn_expression, null, callbackAsync, missFire);
        }
        public Task<IDisposable> CreateCornJobAsync(string corn_expression, Action callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            return this.CreateCornJobAsync(corn_expression, null, st => callback(), missFire);
        }
        public Task<IDisposable> CreateCornJobAsync(string corn_expression, Func<Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            return this.CreateCornJobAsync(corn_expression, null, st => callbackAsync(), missFire);
        }
        //---------------------------------------------------------------------------------------------
        public IDisposable CreateTimer(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period, bool missfire)
        {
            var trace = RpcStatistics.AllocTrace();
            var ret = this.currentNode.CreateTimer(cb_timer, state, dueTime, period, missfire);
            lock (waittingTimer) { waittingTimer.Add(ret); }
            void cb_timer(object st)
            {
                var evt = RpcMessage.AllocRetain(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                {
                    evt.State = st;
                    evt.SetCallback((rsp, err) =>
                    {
                        try
                        {
                            callback(st);
                        }
                        finally
                        {
                            RpcLockRelease();
                        }
                    });
                }
                try
                {
                    this.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    LogError(err, trace);
                }
            }
            return ret;
        }
        public IDisposable CreateTimer(Func<object, Task> callback, object state, TimeSpan dueTime, TimeSpan period, bool missfire)
        {
            var trace = RpcStatistics.AllocTrace();
            var ret = this.currentNode.CreateTimer(cb_timer, state, dueTime, period, missfire);
            lock (waittingTimer) { waittingTimer.Add(ret); }
            void cb_timer(object st)
            {
                var evt = RpcMessage.AllocRetain(RpcEvent.RESPONES_CALLBACK, this.address, this.address);
                {
                    evt.State = st;
                    evt.SetCallback((rsp, err) =>
                    {
                        callback(st).ContinueWith(t =>
                        {
                            RpcLockRelease();
                        });
                    });
                }
                try
                {
                    this.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    LogError(err, trace);
                }
            }
            return ret;
            //             return CreateTimer(st =>
            //             {
            //                 callback(st).ContinueWith(t =>
            //                 {
            //                     if (t.Exception != null)
            //                     {
            //                         log.Error(t.Exception);
            //                     }
            //                 });
            //             }, this, dueTime, period, missfire);
        }
        public IDisposable CreateTimer(Action callback, TimeSpan dueTime, TimeSpan period, bool missfire)
        {
            return CreateTimer(st => callback(), null, dueTime, period, missfire);
        }
        public IDisposable CreateTimer(Func<Task> callback, TimeSpan dueTime, TimeSpan period, bool missfire)
        {
            return CreateTimer(st => callback(), null, dueTime, period, missfire);
        }
        public IDisposable CreateTimer(Action<object> callback, object state, TimeSpan period, bool missfire)
        {
            return CreateTimer(callback, state, TimeSpan.FromSeconds(-1), period, missfire);
        }
        public IDisposable CreateTimer(Action callback, TimeSpan period, bool missfire)
        {
            return CreateTimer(callback, TimeSpan.FromSeconds(-1), period, missfire);
        }
        public IDisposable CreateTimer(Func<object, Task> callback, object state, TimeSpan period, bool missfire)
        {
            return CreateTimer(callback, state, TimeSpan.FromSeconds(-1), period, missfire);
        }
        public IDisposable CreateTimer(Func<Task> callback, TimeSpan period, bool missfire)
        {
            return CreateTimer(callback, TimeSpan.FromSeconds(-1), period, missfire);
        }
        //---------------------------------------------------------------------------------------------

        public TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS, StackTrace trace)
        {
            if (this.is_starting || this.is_disposing)
            {
                return currentNode.CreateAsyncCompletionSource<T>(name, Timeout.InfiniteTimeSpan, trace);
            }
            return currentNode.CreateAsyncCompletionSource<T>(name, timeoutMS, trace);
        }
        public TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout, StackTrace trace)
        {
            if (this.is_starting || this.is_disposing)
            {
                return currentNode.CreateTaskCompletionSource<T>(name, Timeout.InfiniteTimeSpan, timeout, trace);
            }
            return currentNode.CreateTaskCompletionSource<T>(name, timeoutMS, timeout, trace);
        }

        TaskCompletionSource<T> DeepCrystal.RPC.IServiceProvider.CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS)
        {
            return CreateTaskCompletionSource<T>(name, timeoutMS, RpcStatistics.AllocTrace());
        }
        TaskCompletionSource<T> DeepCrystal.RPC.IServiceProvider.CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout)
        {
            return CreateTaskCompletionSource<T>(name, timeoutMS, timeout, RpcStatistics.AllocTrace());
        }

        //-------------------------------------------------------------------------------------------------------------------------
        Task ITaskExecutor.Delay(TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            return this.ExecuteAsync(Task.Delay(delayMS), Timeout.InfiniteTimeSpan);
        }
        async Task ITaskExecutor.Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            if (!this.is_disposing)
            {
                await this.Execute(callback, state, TimeSpan.Zero);
            }
        }
        async Task<TResult> ITaskExecutor.Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            if (!this.is_disposing)
            {
                return await this.ExecuteAsync(callback, state, TimeSpan.Zero);
            }
            else
            {
                return default(TResult);
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region Executor
        Task ITaskExecutor.Execute<TInput>(Action<TInput> callback, TInput state)
        {
            return this.Execute<TInput>(callback, state, TimeSpan.Zero);
        }
        Task ITaskExecutor.Execute(Action callback)
        {
            return this.Execute(callback, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return this.ExecuteAsync<TInput, TResult>(function, state, TimeSpan.Zero);
        }
        Task ITaskExecutor.Execute<TInput>(Func<TInput, Task> function, TInput state)
        {
            return this.ExecuteAsync<TInput>(function, state, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return this.ExecuteAsync<TInput, TResult>(function, state, TimeSpan.Zero);
        }
        Task ITaskExecutor.Execute(Func<Task> function)
        {
            return this.ExecuteAsync(function, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<TResult> function)
        {
            return this.ExecuteAsync<TResult>(function, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<Task<TResult>> function)
        {
            return this.ExecuteAsync<TResult>(function, TimeSpan.Zero);
        }
        //---------------------------------------------------------------------------------------------
        Task ITaskExecutor.Execute(Task task)
        {
            return this.ExecuteAsync(task, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Task<TResult> task)
        {
            return this.ExecuteAsync<TResult>(task, TimeSpan.Zero);
        }
        Task<TResult> ITaskExecutor.FromResult<TResult>(TResult result)
        {
            return this.ExecuteFromResult<TResult>(result, TimeSpan.Zero);
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #region Listen
        IPushHandler DeepCrystal.RPC.IServiceProvider.Listen<T>(Action<T> action, bool recursion_base_type)
        {
            return this.PushHander.ListenPush(typeof(T), 0, (push) => { action((T)push); }, null, recursion_base_type) as IPushHandler;
        }
        IPushHandler DeepCrystal.RPC.IServiceProvider.Listen(Type type, Action<ISerializable> action, bool recursion_base_type)
        {
            return this.PushHander.ListenPush(type, 0, action, null, recursion_base_type) as IPushHandler;
        }
        IPushHandlerBinary DeepCrystal.RPC.IServiceProvider.ListenBinary(int route, Action<BinaryMessage> action, bool recursion_base_type)
        {
            return this.PushHander.ListenPush(null, route, null, action, recursion_base_type) as IPushHandlerBinary;
        }
        IPushHandler DeepCrystal.RPC.IServiceProvider.Listen(Action<ISerializable> action)
        {
            return this.PushHander.ListenPush(null, IOStream.INVALID_MESSAGE_CODE, action, null, false) as IPushHandler;
        }
        IPushHandlerBinary DeepCrystal.RPC.IServiceProvider.ListenBinary(Action<BinaryMessage> action)
        {
            return this.PushHander.ListenPush(null, IOStream.INVALID_MESSAGE_CODE, null, action, false) as IPushHandlerBinary;
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #endregion //IServiceProvider
        //-------------------------------------------------------------------------------------------------------------------------

        #region InvokerHandler
        class RpcHandler
        {
            public object module;
            public RpcServiceInvoker invoker;
        }
        private List<RpcHandler> modules;
        private void rpc_Invoke(RpcMessage msg, BinaryMessage bin, Action<BinaryMessage, Exception> cb)
        {
            if (invokes.RpcInvoke(msg.From, service, bin, cb)) { return; }
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    if (module.invoker.RpcInvoke(msg.From.ServiceType, module.module, bin, cb)) { return; }
                }
            }
            if (!msg.From.IsNull)
            {
                var route = bin.Route;
                var codec = currentNode.RpcCodec.Factory.GetCodec(route);
                cb(BinaryMessage.NULL, new NoHandlerException(
                      $"No Handler Exception : Rote={route}({codec?.MessageType.FullName}) From={msg.From} To={service.GetType().FullName}"));
            }
            else
            {
                cb(BinaryMessage.NULL, null);
            }
        }
        private void rpc_Invoke(RpcMessage msg, ISerializable ser, Action<ISerializable, Exception> cb)
        {
            if (invokes.RpcInvoke(msg.From, service, ser, cb)) { return; }
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    if (module.invoker.RpcInvoke(msg.From.ServiceType, module.module, ser, cb)) { return; }
                }
            }
            if (!msg.From.IsNull)
            {
                cb(null, new NoHandlerException(
                    $"No Handler Exception : Rote={ser?.GetType().FullName} From={msg.From} To={service.GetType().FullName}"));
            }
            else
            {
                cb(null, null);
            }
        }
        private int wormhole_Invoke(in RemoteAddress from, object message)
        {
            var count = 0;
            if (invokes.WormholeInvoke(from, service, message)) { count++; }
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    if (module.invoker.WormholeInvoke(from, module.module, message)) { count++; }
                }
            }
            return count;
        }
        private async Task<object> wormhole_InvokeAsync(RemoteAddress from, object message)
        {
            var ret = await invokes.WormholeInvokeAsync(from, service, message);
            if (ret != null) { return ret; }
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    ret = await module.invoker.WormholeInvokeAsync(from, module.module, message);
                    if (ret != null) { return ret; }
                }
            }
            return ret;
        }
        public void RegistInvoker(object module)
        {
            if (modules == null) modules = new List<RpcHandler>(1);
            modules.Add(new RpcHandler()
            {
                module = module,
                invoker = this.currentNode.InvokeManager.GetServiceInvoker(module.GetType())
            });
        }

        #endregion
    }
}
