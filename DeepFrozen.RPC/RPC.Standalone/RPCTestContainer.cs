using DeepCore;
using DeepCore.IO;
using DeepCore.IO.Utils;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Invoker;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepCrystal.RpcTest
{
    public class ServiceContainer
    {
        //--------------------------------------------------------------------------------------------
        public static TypeAllocRecorder Alloc { get; private set; } = new TypeAllocRecorder("ServiceContainer");
        public static int ActiveCount { get { return Alloc.ActiveCount; } }
        public static int AllocCount { get { return Alloc.AllocCount; } }
        //--------------------------------------------------------------------------------------------
        internal readonly Logger log;
        internal readonly ServiceInfo info;
        internal readonly Type serviceType;
        internal readonly RpcTest rpc;
        internal readonly string Key;
        private readonly ServiceHandler manager;
        private readonly ActionBlock<RpcMessage> callback_action;
        private IService service;
        private RpcServiceInvoker invokes;
        private PushInvokers push_handler;
        private bool is_disposing = false;
        private bool is_starting = false;
        public bool IsDisposed { get; private set; } = false;
        public bool IsStarted { get; private set; } = false;
        //---------------------------------------------------------------------------------------------------------------------------------
        internal ServiceContainer(ServiceInfo info, Type svcType, RpcTest rpc)
        {
            Alloc.RecordConstructor(svcType);
            this.log = LoggerFactory.GetLogger(info.Address.FullPath);
            this.info = info;
            this.rpc = rpc;
            this.serviceType = svcType;
            this.Key = info.Address.ServiceName;
            this.manager = new ServiceHandler(rpc, this);
            this.invokes = rpc.invoke_manager.GetServiceInvoker(serviceType);
            this.push_handler = new PushInvokers(rpc.codec);
            try
            {
                var start = new ServiceStartInfo(manager, info.Address, info.Config, rpc.codec, info.Creator);
                this.service = (IService)Activator.CreateInstance(serviceType, new object[] { start });
            }
            catch
            {
                log.Error("[CreateInstance error]\t" + info.Address.ToString() + "\t" + info.Config.ToParseString("\t"));
                throw;
            }
            if (rpc.taskScheduler != null)
            {
                this.callback_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(rpc_invoke), new ExecutionDataflowBlockOptions() { TaskScheduler = rpc.taskScheduler, });
            }
            else
            {
                this.callback_action = new ActionBlock<RpcMessage>(new Action<RpcMessage>(rpc_invoke));
            }
        }
        ~ServiceContainer()
        {
            Alloc.RecordDestructor(serviceType);
        }
        private void DoDispose()
        {
            try
            {
                lock (this)
                {
                    try { (this.service as IDisposable).Dispose(); }
                    catch (Exception err) { log.Error(err.Message, err); }
                    this.service = null;
                    this.invokes = null;
                    this.IsDisposed = true;
                }
            }
            finally
            {
                Alloc.RecordDispose(serviceType);
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        internal Task<RemoteAddress> StartAsync(RemoteAddress from)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.START, from, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<RemoteAddress>(evt.ToString(), Timeout.Infinite);
            try
            {
                {
                    evt.callback_async = true;
                    evt.callback = cb_Started;
                }
                //<-先占领第一个元素//
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Started(object obj, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    taskCompletion.TrySetResult(info.Address);
                }
            }
            return taskCompletion.Task;
        }
        internal Task<RemoteAddress> DestoryAsync(RemoteAddress from, string reason)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.DESTORY, from, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<RemoteAddress>(evt.ToString(), Timeout.Infinite);
            try
            {
                {
                    evt.callback_async = true;
                    evt.destroy_reason = reason;
                    evt.callback = cb_Destory;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Destory(object obj, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    taskCompletion.TrySetResult(info.Address);
                }
            }
            return taskCompletion.Task;
        }
        internal Task<RSP> CallAsync<RSP>(RemoteAddress from, ISerializable req, int timeout)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, from, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<RSP>(evt.ToString(), timeout);
            try
            {
                {
                    evt.obj = req;
                    evt.callback_obj = cb_Call;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Call(ISerializable rsp, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    taskCompletion.TrySetResult((RSP)rsp);
                }
            }
            return taskCompletion.Task;
        }
        internal Task<BinaryMessage> CallAsync(RemoteAddress from, BinaryMessage req, int timeout)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<BinaryMessage>(evt.ToString(), timeout);
            try
            {
                {
                    evt.bin = req;
                    evt.callback_bin = cb_Call;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Call(BinaryMessage rsp, Exception err)
            {
                if (err != null)
                {
                    taskCompletion.TrySetException(err);
                }
                else
                {
                    taskCompletion.TrySetResult(rsp);
                }
            }
            return taskCompletion.Task;
        }
        internal Task Execute(Action<object> action, object state)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.EXECUTE, info.Address, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<int>(evt.ToString(), Timeout.Infinite);
            try
            {
                {
                    evt.state = state;
                    evt.callback = cb_Callback;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    action(st);
                    taskCompletion.TrySetResult(0);
                }
                catch (Exception error)
                {
                    log.Error(error.Message, error);
                    taskCompletion.TrySetException(error);
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TResult>(Func<object, TResult> function, object state)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.EXECUTE, info.Address, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<TResult>(evt.ToString(), Timeout.Infinite);
            try
            {
                {
                    evt.state = state;
                    evt.callback = cb_Callback;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var result = function(st);
                    taskCompletion.TrySetResult(result);
                }
                catch (Exception error)
                {
                    log.Error(error.Message, error);
                    taskCompletion.TrySetException(error);
                }
            }
            return taskCompletion.Task;
        }
        internal Task<TResult> ExecuteAsync<TResult>(Func<object, Task<TResult>> function, object state)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.EXECUTE, info.Address, info.Address);
            var taskCompletion = rpc.CreateAsyncCompletionSource<TResult>(evt.ToString(), Timeout.Infinite);
            try
            {
                {
                    evt.state = state;
                    evt.callback = cb_Callback;
                }
                this.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                taskCompletion.TrySetException(err);
                evt.Dispose();
            }
            void cb_Callback(object st, Exception err)
            {
                try
                {
                    var task = function(st);
                    task.GetAwaiter().OnCompleted(() =>
                    {
                        var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.EXECUTE, info.Address, info.Address);
                        try
                        {
                            {
                                evt2.state = state;
                                evt2.callback = cb_TaskComplete;
                            }
                            this.PostRequest(evt2);
                        }
                        catch (Exception err2)
                        {
                            log.Error(err2.Message, err2);
                            taskCompletion.TrySetException(err2);
                            evt2.Dispose();
                        }
                        void cb_TaskComplete(object st2, Exception err2)
                        {
                            if (task.IsCanceled)
                            {
                                taskCompletion.TrySetCanceled();
                            }
                            else if (task.IsFaulted)
                            {
                                taskCompletion.TrySetException(task.Exception);
                            }
                            else if (task.IsCompleted)
                            {
                                taskCompletion.TrySetResult(task.Result);
                            }
                        }
                    });
                }
                catch (Exception error)
                {
                    log.Error(error.Message, error);
                    taskCompletion.TrySetException(error);
                }
            }
            return taskCompletion.Task;
        }
        internal async Task<TResult> ExecuteAsync<TResult>(Task<TResult> task)
        {
            var taskCompletion = rpc.CreateAsyncCompletionSource<TResult>("ExecuteAsync(Task)", Timeout.Infinite);
            try
            {
                var result = await task;
                var evt2 = RpcMessage.AllocAutoRelease(RpcEvent.EXECUTE, info.Address, info.Address);
                try
                {
                    {
                        evt2.callback = cb_TaskComplete;
                    }
                    this.PostRequest(evt2);
                }
                catch (Exception err2)
                {
                    log.Error(err2.Message, err2);
                    taskCompletion.TrySetException(err2);
                    evt2.Dispose();
                }
                void cb_TaskComplete(object st2, Exception err2)
                {
                    taskCompletion.TrySetResult(result);
                }
            }
            catch (Exception error)
            {
                log.Error(error.Message, error);
                taskCompletion.TrySetException(error);
            }
            return await taskCompletion.Task;
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        internal void PostRequest(RpcMessage msg)
        {
            lock (this)
            {
                if (IsDisposed == true)
                {
                    throw new Exception(string.Format("RPC Error : Service Destroyed : \n    {0}", msg));
                }
                if (msg.rpc_event == RpcEvent.START)
                {
                    if (is_starting)
                    {
                        throw new Exception(string.Format("RPC Error : Service Already Start : \n    {0}", msg));
                    }
                    else
                    {
                        is_starting = true;
                    }
                }
                if (msg.rpc_event == RpcEvent.DESTORY)
                {
                    if (is_disposing)
                    {
                        throw new Exception(string.Format("RPC Error : Service Disposing : \n    {0}", msg));
                    }
                    else
                    {
                        is_disposing = true;
                    }
                }
            }
            if (callback_action.Post(msg) == false)
            {
                throw new Exception(string.Format("RPC Error : Service Destroyed : \n    {0}", msg));
            }
        }
        internal void PostResponse(RpcMessage msg)
        {
            if (callback_action.Post(msg) == false)
            {
                throw new Exception(string.Format("RPC Error : Service Destroyed : \n    {0}", msg));
            }
        }
        //-----------------------------------------------------------------------------------------------------------

        private void rpc_invoke(RpcMessage msg)
        {
            try
            {
                switch (msg.rpc_event)
                {
                    case RpcEvent.START:
                        rpc_Start(msg);
                        break;
                    case RpcEvent.DESTORY:
                        rpc_Destory(msg);
                        break;
                    case RpcEvent.CALLBACK:
                        rpc_Callback(msg);
                        break;
                    case RpcEvent.EXECUTE:
                        rpc_Exe(msg);
                        break;
                    case RpcEvent.NOTIFY:
                        rpc_Notify(msg);
                        break;

                    case RpcEvent.REQUEST_OBJ:
                    case RpcEvent.REQUEST_BIN:
                        rpc_Request(msg);
                        break;
                    case RpcEvent.RESPONSE_OBJ:
                    case RpcEvent.RESPONSE_BIN:
                        rpc_Response(msg);
                        break;

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
        private void rpc_Start(RpcMessage msg)
        {
            this.log.Info("Service Starting...");
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.START, msg.from, msg.to);
            {
                evt.destroy_reason = msg.destroy_reason;
                evt.callback_async = msg.callback_async;
                evt.callback = msg.callback;
                evt.AppendError(msg);
            }
            try
            {
                var task = (this.service as IServiceStart).Start();
                task.GetAwaiter().OnCompleted(() =>
                {
                    lock (this)
                    {
                        this.IsStarted = true;
                    }
                    this.log.Info("Service Started!!!");
                    if (task.Exception != null)
                    {
                        log.Error(task.Exception.Message, task.Exception);
                        evt.AppendError(task.Exception);
                    }
                    try
                    {
                        if (evt.callback != null)
                        {
                            var callback = evt.callback;
                            if (evt.callback_async)
                            {
                                callback(this.info.Address, evt.Error);
                            }
                            else if (evt.from != null && evt.from != info.Address)
                            {
                                var rsp = RpcMessage.AllocAutoRelease(RpcEvent.CALLBACK, this.info.Address, evt.from);
                                {
                                    rsp.state = this.info.Address;
                                    rsp.callback = callback;
                                    rsp.AppendError(evt);
                                }
                                try
                                {
                                    rpc.PostResponse(rsp);
                                }
                                catch (Exception rerr)
                                {
                                    log.Error(rerr.Message, rerr);
                                    rsp.Dispose();
                                }
                            }
                        }
                        try
                        {
                            (this.service as IServiceStart).Started();
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            evt.AppendError(task.Exception);
                        }
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
            catch (Exception err)
            {
                log.Error(err.Message, err);
                evt.Dispose();
            }
        }
        private void rpc_Destory(RpcMessage msg)
        {
            this.log.Warn("Service Disposing...");
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.DESTORY, msg.from, msg.to);
            {
                evt.destroy_reason = msg.destroy_reason;
                evt.callback_async = msg.callback_async;
                evt.callback = msg.callback;
                evt.AppendError(msg);
            }
            try
            {
                //执行Destory//
                var task = (this.service as IServiceStop).Stop(msg.destroy_reason);
                task.GetAwaiter().OnCompleted(() =>
                {
                    Task.Run(() => // rpc_Response, SetResult was owner Task //
                    {
                        try
                        {
                            if (task.Exception != null)
                            {
                                log.Error(task.Exception.Message, task.Exception);
                                evt.AppendError(task.Exception);
                            }
                            // remove from name server
                            rpc.RemoveService(info.Address);
                            this.callback_action.Complete();
                            this.callback_action.Completion.Wait();
                            this.push_handler.ClearPush();
                            this.DoDispose();
                            this.log.Warn("Service Destoryed!!!");
                            if (evt.callback != null)
                            {
                                var callback = evt.callback;
                                if (evt.callback_async)
                                {
                                    callback(this.info.Address, evt.Error);
                                }
                                else if (evt.from != null && evt.from != info.Address)
                                {
                                    var rsp = RpcMessage.AllocAutoRelease(RpcEvent.CALLBACK, this.info.Address, evt.from);
                                    {
                                        rsp.state = this.info.Address;
                                        rsp.callback = callback;
                                        rsp.AppendError(evt);
                                    }
                                    try
                                    {
                                        rpc.PostResponse(rsp);
                                    }
                                    catch (Exception rerr)
                                    {
                                        log.Error(rerr.Message, rerr);
                                        rsp.Dispose();
                                    }
                                }
                            }
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
                });
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                evt.Dispose();
            }
        }
        private void rpc_Exe(RpcMessage msg)
        {
            if (msg.callback != null)
            {
                msg.callback(msg.state, msg.Error);
            }
        }
        private void rpc_Callback(RpcMessage msg)
        {
            if (msg.callback != null)
            {
                msg.callback(msg.state, msg.Error);
            }
        }
        private void rpc_Notify(RpcMessage msg)
        {
            if (msg.obj != null)
            {
                push_handler.Notify(msg.obj);
                invokes.Invoke(msg.from.ServiceType, service, msg.obj, cb_Notify);
                void cb_Notify(ISerializable ret, Exception err)
                {
                    if (err != null) log.Error(err.Message, err);
                }
            }
            else
            {
                push_handler.Notify(msg.bin);
                invokes.Invoke(msg.from.ServiceType, service, msg.bin, cb_Notify);
                void cb_Notify(BinaryMessage ret, Exception err)
                {
                    if (err != null) log.Error(err.Message, err);
                }
            }
        }
        private void rpc_Request(RpcMessage msg)
        {
            if (msg.rpc_event == RpcEvent.REQUEST_OBJ)
            {
                var _callback_async = msg.callback_async;
                var _callback_obj = msg.callback_obj;
                var _from = msg.from;
                var _to = msg.to;
                invokes.Invoke(msg.from.ServiceType, service, msg.obj, cb_Request);
                void cb_Request(ISerializable ret, Exception err)
                {
                    if (err != null) log.Error(err.Message, err);
                    if (_callback_async)
                    {
                        _callback_obj(ret, err);
                    }
                    else
                    {
                        var rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_OBJ, _to, _from);
                        {
                            rsp.obj = ret;
                            rsp.callback_obj = _callback_obj;
                            rsp.AppendError(msg);
                            rsp.AppendError(err);
                        }
                        try
                        {
                            rpc.PostResponse(rsp);
                        }
                        catch (Exception rerr)
                        {
                            log.Error(rerr.Message, rerr);
                            rsp.Dispose();
                        }
                    }
                }
            }
            else
            {
                var _callback_async = msg.callback_async;
                var _callback_bin = msg.callback_bin;
                var _from = msg.from;
                var _to = msg.to;
                invokes.Invoke(msg.from.ServiceType, service, msg.bin, cb_Request);
                void cb_Request(BinaryMessage ret, Exception err)
                {
                    if (err != null) log.Error(err.Message, err);
                    if (_callback_async)
                    {
                        _callback_bin(ret, err);
                    }
                    else
                    {
                        var rsp = RpcMessage.AllocAutoRelease(RpcEvent.RESPONSE_BIN, _to, _from);
                        {
                            rsp.bin = ret;
                            rsp.callback_bin = _callback_bin;
                            rsp.AppendError(msg);
                            rsp.AppendError(err);
                        }
                        try
                        {
                            rpc.PostResponse(rsp);
                        }
                        catch (Exception rerr)
                        {
                            log.Error(rerr.Message, rerr);
                            rsp.Dispose();
                        }
                    }
                }
            }
        }
        private void rpc_Response(RpcMessage msg)
        {
            if (msg.rpc_event == RpcEvent.RESPONSE_BIN)
            {
                msg.callback_bin(msg.bin, msg.Error);
            }
            else
            {
                msg.callback_obj(msg.obj, msg.Error);
            }
        }

        //---------------------------------------------------------------------------------------------
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

        //---------------------------------------------------------------------------------------------

    }

}
