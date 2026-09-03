
using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.IO.Utils;
using DeepCore.Log;
using DeepCrystal;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Invoker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepCrystal.RpcTest
{


    //-------------------------------------------------------------------------------------------------------------------

    public struct ServiceInfo
    {
        public RemoteAddress Address;
        public RemoteAddress Creator;
        public Properties Config;
        public DateTime StartTimeUTC;
    }

    //-------------------------------------------------------------------------------------------------------------------

    internal class ServiceHandler : RPC.IServiceProvider
    {
        internal readonly RpcTest rpc;
        internal readonly ServiceContainer local;
        //-------------------------------------------------------------------------------------------------------------------------
        internal ServiceHandler(RpcTest rpc, ServiceContainer local)
        {
            this.rpc = rpc;
            this.local = local;
        }
        public void ShutdownSelf(string reason)
        {
            local.DestoryAsync(local.info.Address, reason);
        }
        private RemoteServiceProxy CreatePrx(ServiceContainer remote)
        {
            if (remote != null)
            {
                var prx = new RemoteServiceProxy(rpc, local.info.Address, remote.info.Address, remote.info.StartTimeUTC, remote.info.Config);
                return prx;
            }
            return null;
        }
        public IRemoteService GetOrCreate(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = rpc.GetOrCreate(local.info.Address, path, cfg);
            return CreatePrx(svc);
        }
        public IRemoteService Create(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = rpc.Create(local.info.Address, path, cfg);
            return CreatePrx(svc);
        }
        public IRemoteService Get(RemoteAddress path)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = rpc.GetService(path);
            return CreatePrx(svc);
        }
        public async Task<IRemoteService> GetOrCreateAsync(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = await rpc.GetOrCreateAsync(local.info.Address, path, cfg);
            return CreatePrx(svc);
        }
        public async Task<IRemoteService> CreateAsync(RemoteAddress path, IDictionary<string, string> cfg)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = await rpc.CreateAsync(local.info.Address, path, cfg);
            return CreatePrx(svc);
        }
        public async Task<IRemoteService> GetAsync(RemoteAddress path)
        {
            if (path.ServiceName == local.Key) throw new Exception("Remote Service is self : " + path);
            var svc = await rpc.GetServiceAsync(path);
            return CreatePrx(svc);
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public IDisposable CreateTimer(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var ret = rpc.timers.CreateTimer(cb_timer, state, dueTime, period);
            void cb_timer(object st)
            {
                var evt = RpcMessage.AllocRetain(RpcEvent.CALLBACK, local.info.Address, local.info.Address);
                {
                    evt.state = st;
                    evt.callback = cb_main;
                }
                try
                {
                    local.PostResponse(evt);
                }
                catch (Exception err)
                {
                    local.log.Warn("Timer Error : Callback : " + callback.Method);
                    local.log.Warn(err.Message, err);
                    evt.Dispose();
                }
            }
            void cb_main(object rsp, Exception err)
            {
                callback(rsp);
            }
            return ret;
        }
        public IDisposable Delay(Action<object> callback, object state, TimeSpan dueTime)
        {
            var ret = new OnceDelayTimer(cb_timer, state, dueTime);
            void cb_timer(object st)
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.CALLBACK, local.info.Address, local.info.Address);
                {
                    evt.state = st;
                    evt.callback = cb_main;
                }
                try
                {
                    local.PostResponse(evt);
                }
                catch (Exception err)
                {
                    local.log.Warn("Timer Error : Callback : " + callback.Method);
                    local.log.Warn(err.Message, err);
                    evt.Dispose();
                }
            }
            void cb_main(object rsp, Exception err)
            {
                callback(rsp);
            }
            return ret.Timer;
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public Task Execute(Action<object> callback, object state = null)
        {
            return local.Execute(callback, state);
        }
        public Task Execute(Func<object, Task> function, object state = null)
        {
            return local.ExecuteAsync<int>(o => { return function(o).ContinueWith(pt => 0); }, state);
        }
        public Task<TResult> Execute<TResult>(Func<object, TResult> function, object state = null)
        {
            return local.ExecuteAsync(function, state);
        }
        public Task<TResult> Execute<TResult>(Func<object, Task<TResult>> function, object state = null)
        {
            return local.ExecuteAsync(function, state);
        }
        public Task Execute(Action callback)
        {
            return local.Execute(o => callback(), null);
        }
        public Task Execute(Func<Task> function)
        {
            return local.ExecuteAsync<int>(o => { return function().ContinueWith(pt => 0); }, null);
        }
        public Task<TResult> Execute<TResult>(Func<TResult> function)
        {
            return local.ExecuteAsync(o => function(), null);
        }
        public Task<TResult> Execute<TResult>(Func<Task<TResult>> function)
        {
            return local.ExecuteAsync(o => function(), null);
        }
        public Task Execute(Task task)
        {
            return local.ExecuteAsync<int>(task.ContinueWith(pt => 0));
        }
        public Task<TResult> Execute<TResult>(Task<TResult> task)
        {
            return local.ExecuteAsync<TResult>(task);
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public IPushHandler Listen<T>(Action<T> action, bool recursion_base_type = true) where T : ISerializable
        {
            return local.PushHander.ListenPush(typeof(T), 0, (push) => { action((T)push); }, null, recursion_base_type) as IPushHandler;
        }
        public IPushHandler Listen(Type type, Action<ISerializable> action, bool recursion_base_type = true)
        {
            return local.PushHander.ListenPush(type, 0, action, null, recursion_base_type) as IPushHandler;
        }
        public IPushHandlerBinary ListenBinary(int route, Action<BinaryMessage> action, bool recursion_base_type = true)
        {
            return local.PushHander.ListenPush(null, route, null, action, recursion_base_type) as IPushHandlerBinary;
        }
        public IPushHandler Listen(Action<ISerializable> action)
        {
            return local.PushHander.ListenPush(null, IOStream.INVALID_MESSAGE_CODE, action, null, false) as IPushHandler;
        }
        public IPushHandlerBinary ListenBinary(Action<BinaryMessage> action)
        {
            return local.PushHander.ListenPush(null, IOStream.INVALID_MESSAGE_CODE, null, action, false) as IPushHandlerBinary;
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public IAsyncLock CreateLock()
        {
            return new ServiceAsyncLock(this);
        }
    }

}


