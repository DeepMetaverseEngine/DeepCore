
using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.IO.Utils;
using DeepCore.Log;
using DeepCrystal;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepCrystal.RpcTest
{


    //-------------------------------------------------------------------------------------------------------------------

    internal class RemoteServiceProxy : IRemoteService
    {
        internal readonly Logger log;
        internal readonly RpcTest rpc;
        internal readonly RemoteAddress local_address;
        internal readonly RemoteAddress remote_address;
        internal readonly DateTime remote_start_time;
        internal readonly Properties remote_config;

        internal RemoteServiceProxy(RpcTest rpc, RemoteAddress local, RemoteAddress remote, DateTime remote_start_time, Properties remote_cfg)
        {
            this.rpc = rpc;
            this.local_address = local;
            this.remote_address = remote;
            this.remote_start_time = remote_start_time;
            this.remote_config = remote_cfg;
            this.log = LoggerFactory.GetLogger("Prx:" + remote.FullPath);
        }
        public DateTime StartTimeUTC
        {
            get { return remote_start_time; }
        }
        public RemoteAddress Address
        {
            get { return remote_address; }
        }
        public Properties Config
        {
            get { return remote_config; }
        }
        //-------------------------------------------------------------------------------------------------
        public void Call<RSP>(ISerializable req, OnRpcReturn<RSP> callback)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, local_address, remote_address);
            {
                evt.obj = req;
                evt.callback_obj = cb_Call;
            }
            try
            {
                rpc.PostRequest(evt);
            }
            catch (Exception err)
            {
                callback(default(RSP), err);
                evt.Dispose();
            }
            void cb_Call(ISerializable rsp, Exception err)
            {
                callback((RSP)rsp, err);
            }
        }
        public void Call(BinaryMessage req, OnRpcBinaryReturn callback)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, local_address, remote_address);
            {
                evt.bin = req;
                evt.callback_bin = cb_Call;
            }
            try
            {
                rpc.PostRequest(evt);
            }
            catch (Exception err)
            {
                callback(BinaryMessage.NULL, err);
                evt.Dispose();
            }
            void cb_Call(BinaryMessage rsp, Exception err)
            {
                callback(rsp, err);
            }
        }
        public void Invoke(ISerializable msg)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.NOTIFY, local_address, remote_address);
            {
                evt.obj = msg;
            }
            try
            {
                rpc.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
                evt.Dispose();
            }
        }
        public void Invoke(BinaryMessage msg)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.NOTIFY, local_address, remote_address);
            {
                evt.bin = msg;
            }
            try
            {
                rpc.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
                evt.Dispose();
            }
        }
        public void Shutdown(string reason, OnRpcShutdown callback = null)
        {
            var evt = RpcMessage.AllocAutoRelease(RpcEvent.DESTORY, local_address, remote_address);
            {
                evt.destroy_reason = reason;
                evt.callback = (rst, err) => { callback?.Invoke((RemoteAddress)rst, err); };
            }
            try
            {
                rpc.PostRequest(evt);
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
                callback?.Invoke(remote_address, err);
                evt.Dispose();
            }
        }
        //-------------------------------------------------------------------------------------------------
        public Task<RSP> CallAsync<RSP>(ISerializable req, int timeout = Timeout.Infinite)
        {
            var target = rpc.GetService(remote_address);
            if (target != null)
            {
                return target.CallAsync<RSP>(local_address, req, timeout);
            }
            else
            {
                return Task.FromException<RSP>(new Exception("Service Not Exist : " + remote_address));
            }
        }
        public Task<BinaryMessage> CallAsync(BinaryMessage req, int timeout = Timeout.Infinite)
        {
            var target = rpc.GetService(remote_address);
            if (target != null)
            {
                return target.CallAsync(local_address, req, timeout);
            }
            else
            {
                return Task.FromException<BinaryMessage>(new Exception("Service Not Exist : " + remote_address));
            }
        }
        public Task<RemoteAddress> ShutdownAsync(string reason)
        {
            var target = rpc.GetService(remote_address);
            if (target != null)
            {
                return target.DestoryAsync(local_address, reason);
            }
            else
            {
                return Task.FromException<RemoteAddress>(new Exception("Service Not Exist : " + remote_address));
            }
        }

    }




}


