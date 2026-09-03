using DeepCore;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Invoker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    public abstract class RpcServiceProxy : DeepCrystal.RPC.IRemoteService
    {
        private RpcServiceNode node;
        private IRemoteServiceInfo remote_info;
        private RpcServiceBox local;

        public string ServiceName { get { return remote_info.Address.ServiceName; } }
        public RemoteAddress Address { get { return remote_info.Address; } }
        public Properties Config { get { return remote_info.Config; } }
        public DateTime StartTimeUTC { get { return remote_info.StartTimeUTC; } }
        public bool IsStatic { get { return remote_info.IsStatic; } }

        public RpcServiceProxy(RpcServiceBox local, IRemoteServiceInfo remote)
        {
           // this.AsSynchronizedDisposing();
            this.local = local;
            this.remote_info = remote;
            this.node = local.currentNode;
          //  local.AutoDispose(this);
        }
//         protected override void Disposing()
//         {
//             node = null;
//             remote_info = null;
//             local = null;
//         }

        internal void RemoteWormholeTransport(object message)
        {
            this.node.s2r_RpcWormholeTransport(local.Address, this, message);
        }
        internal Task<object> RemoteWormholeTransportAsync(object message)
        {
            return this.node.s2r_RpcWormholeTransportAsync(local.Address, this, message);
        }
        internal void RemoteInvoke(ISerializable msg)
        {
            this.node.s2r_RpcNotify(local.Address, this, msg);
        }
        internal void RemoteInvoke(BinaryMessage msg)
        {
            this.node.s2r_RpcNotify(local.Address, this, msg);
        }
        internal void RemoteBatchInvoke(ICollection<ISerializable> msg)
        {
            this.node.s2r_RpcBatchNotify(local.Address, this, msg);
        }
        internal void RemoteBatchInvoke(ICollection<BinaryMessage> msg)
        {
            this.node.s2r_RpcBatchNotify(local.Address, this, msg);
        }
        //----------------------------------------------------------------------------------------------------
        void DeepCrystal.RPC.IRemoteService.Call<RSP>(ISerializable req, OnRpcReturn<RSP> callback)
        {
            var trace = RpcStatistics.AllocTrace();
            this.local.s2r_RemoteCall<RSP>(this, req, callback, trace);
        }
        void DeepCrystal.RPC.IRemoteService.Call(BinaryMessage req, OnRpcReturnBinary callback)
        {
            var trace = RpcStatistics.AllocTrace();
            this.local.s2r_RemoteCall(this, req, callback, trace);
        }
        Task<RSP> DeepCrystal.RPC.IRemoteService.CallAsync<RSP>(ISerializable req)
        {
            var trace = RpcStatistics.AllocTrace();
            var tcs = local.CreateTaskCompletionSource<RSP>(
                $"{this.local.Address}::CallAsync(req:{req.GetType().Name})",
                Timeout.InfiniteTimeSpan,
                trace);
            this.local.s2r_RemoteCall<RSP>(this, req, (rsp, err) =>
            {
                if (err != null)
                {
                    tcs.TrySetException(err);
                }
                else
                {
                    tcs.TrySetResult(rsp);
                }
            }, trace);
            return tcs.Task;
        }
        Task<BinaryMessage> DeepCrystal.RPC.IRemoteService.CallAsync(BinaryMessage req)
        {
            var trace = RpcStatistics.AllocTrace();
            var tcs = local.CreateTaskCompletionSource<BinaryMessage>(
                $"{this.local.Address}::CallAsync(req:{req.Route})",
                Timeout.InfiniteTimeSpan,
                trace);
            this.local.s2r_RemoteCall(this, req, (rsp_bin, rsp_err) =>
            {
                if (rsp_err != null)
                {
                    tcs.TrySetException(rsp_err);
                }
                else
                {
                    tcs.TrySetResult(rsp_bin);
                }
            }, trace);
            return tcs.Task;
        }
        Task DeepCrystal.RPC.IRemoteService.InvokeAsync(ISerializable msg)
        {
            var trace = RpcStatistics.AllocTrace();
            var tcs = local.CreateTaskCompletionSource<int>(
                $"{this.local.Address}::CallAsync(req:{msg.GetType().Name})",
                Timeout.InfiniteTimeSpan,
                trace);
            this.local.s2r_RemoteCall(this, msg, (err) =>
            {
                if (err != null)
                {
                    tcs.TrySetException(err);
                }
                else
                {
                    tcs.TrySetResult(1);
                }
            }, trace);
            return tcs.Task;
        }
        Task DeepCrystal.RPC.IRemoteService.InvokeAsync(BinaryMessage msg)
        {
            var trace = RpcStatistics.AllocTrace();
            var tcs = local.CreateTaskCompletionSource<int>(
                $"{this.local.Address}::CallAsync(req:{msg.Route})",
                Timeout.InfiniteTimeSpan,
                trace);
            this.local.s2r_RemoteCall(this, msg, (rsp_err) =>
            {
                if (rsp_err != null)
                {
                    tcs.TrySetException(rsp_err);
                }
                else
                {
                    tcs.TrySetResult(1);
                }
            }, trace);
            return tcs.Task;
        }
        void DeepCrystal.RPC.IRemoteService.Invoke(ISerializable msg)
        {
            this.RemoteInvoke(msg);
        }
        void DeepCrystal.RPC.IRemoteService.Invoke(BinaryMessage msg)
        {
            this.RemoteInvoke(msg);
        }
        void DeepCrystal.RPC.IRemoteService.BatchInvoke(ICollection<ISerializable> batch)
        {
            this.RemoteBatchInvoke(batch);
        }
        void DeepCrystal.RPC.IRemoteService.BatchInvoke(ICollection<BinaryMessage> batch)
        {
            this.RemoteBatchInvoke(batch);
        }
        void DeepCrystal.RPC.IRemoteService.WormholeTransport(object message)
        {
            this.RemoteWormholeTransport(message);
        }
        Task<object> DeepCrystal.RPC.IRemoteService.WormholeTransportAsync(object message)
        {
            return this.RemoteWormholeTransportAsync(message);
        }
        Task<bool> DeepCrystal.RPC.IRemoteService.ShutdownAsync(string reason)
        {
            if (this.IsStatic) throw new Exception("Can Not shutdown static service !!!");
            return local.ExecuteAsync(this.node.s2n2r_RpcShutdownAsync(local.Address, this.Address, reason), Timeout.InfiniteTimeSpan);
        }
        void DeepCrystal.RPC.IRemoteService.ListenOnServiceDestroyed(Action<RemoteAddress> action)
        {
            local.ListenRemoteDestoryed(this.Address.ServiceName, (addr) =>
            {
                local.Execute((st) => { action((RemoteAddress)st); }, addr, Timeout.InfiniteTimeSpan);
            });
        }
        //----------------------------------------------------------------------------------------------------
    }

    /// <summary>
    /// 不同节点下服务代理
    /// </summary>
    public class RpcRemoteProxy : RpcServiceProxy
    {
        public object remote { get; private set; }
        public RpcRemoteProxy(RpcServiceBox local, IRemoteServiceInfo remote) : base(local, remote)
        {
        }
//         protected override void Disposing()
//         {
//             remote = null;
//             base.Disposing();
//         }
        public override string ToString()
        {
            return "RPrx:" + base.Address.ServiceName;
        }
    }


    /// <summary>
    /// 相同节点下服务代理
    /// </summary>
    public class RpcLocalProxy : RpcServiceProxy
    {
        public RpcServiceBox remote { get; private set; }
        public bool IsIgnoreError { get; private set; }
        public bool IsremoteDisposed { get => remote.IsDisposed; }
        public RpcLocalProxy(RpcServiceBox local, RpcServiceBox remote) : base(local, remote.LocalServiceInfo)
        {
            this.remote = remote;
            this.IsIgnoreError = remote.serviceProperties.IgnoreRequestError;
        }
//         protected override void Disposing()
//         {
//             remote = null;
//             base.Disposing();
//         }
        internal void PostRequest(RpcMessage msg)
        {
            remote.PostRequest(msg);
        }
        public override string ToString()
        {
            return "LPrx:" + base.Address.ServiceName;
        }
    }
}
