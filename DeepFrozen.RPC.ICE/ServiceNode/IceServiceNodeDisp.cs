using DeepCore;
using DeepFrozen.RPC.Remote.ServiceNode;
using DeepFrozenIceImpl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepFrozen.ICE.ServiceNode
{
    //-------------------------------------------------------------------------------------------------------------------
    public class IceServiceNodeDisp : DeepFrozenIceImpl.IRpcServiceAdapterDisp_
    {
        protected readonly DeepCore.Log.Logger log;
        protected readonly Ice.Communicator communicator;
        protected readonly IceServiceNodeAdapter nodeService;

        public IceServiceNodeDisp(DeepCore.Log.Logger log, Ice.Communicator com, IceServiceNodeAdapter server)
        {
            this.log = log;
            this.communicator = com;
            this.nodeService = server;
        }
        public override async Task<string> n2s_AppCommandAsync(string notify, Ice.Current current = null)
        {
            try
            {
                return await nodeService.n2s_RpcAppCommandAsync(notify);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return err.Message + Environment.NewLine + err.FullStackTrace();
            }
        }
        public override void n2s_AppMessageNotify(DeepFrozenIceImpl.BinaryMessage notify, Ice.Current current = null)
        {
            try
            {
                nodeService.n2s_RpcAppMessageNotify(notify);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override async Task<bool> n2s_CreateLocalServiceAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, Dictionary<string, string> config, bool isStatic, Ice.Current current = null)
        {
            try
            {
                return await nodeService.n2s_CreateLocalServiceAsync(from, addr, config, isStatic);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<bool> n2s_DestoryLocalServiceAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, string reason, Ice.Current current = null)
        {
            try
            {
                return await nodeService.n2s_DestoryLocalServiceAsync(from, addr, reason);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }

        public override void r2s_RpcRequest(string fromNodeEndPoint, DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, int sendID, DeepFrozenIceImpl.BinaryMessage req, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_ProcessRequest(fromNodeEndPoint, from.ToRemoteAddress(), addr.ToRemoteAddress(), sendID, req.ToBinary());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void s2r_RpcResponse(int sendID, DeepFrozenIceImpl.BinaryMessage rsp, DeepFrozenIceImpl.RpcExceptionMeta err, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_RpcResponse(sendID, rsp, err.ToException());
            }
            catch (Exception err2)
            {
                log.Error(err2.Message, err2);
            }
        }
        public override void r2s_RpcNotify(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, DeepFrozenIceImpl.BinaryMessage msg, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_RemoteRpcNotify(from.ToRemoteAddress(), addr.ToRemoteAddress(), msg.ToBinary());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void r2s_RpcBatchNotify(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, DeepFrozenIceImpl.BinaryMessage[] msg, Ice.Current current = null)
        {
            try
            {
                var array = Array.ConvertAll(msg, b => b.ToBinary());
                nodeService.r2s_RemoteRpcBatchNotify(from.ToRemoteAddress(), addr.ToRemoteAddress(), array);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void r2s_RpcNotifyWithType(DeepFrozenIceImpl.RpcAddress from, string serviceType, DeepFrozenIceImpl.BinaryMessage msg, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_RemoteRpcNotifyWithType(from.ToRemoteAddress(), serviceType, msg.ToBinary());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void r2s_RemoteServiceDisposing(DeepFrozenIceImpl.RpcAddress addr, Ice.Current current = null)
        {
            try
            {
                nodeService.n2s_RemoteDisposing(addr.ToRemoteAddress());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void r2s_RemoteServiceDestoryed(DeepFrozenIceImpl.RpcAddress addr, Ice.Current current = null)
        {
            try
            {
                nodeService.n2s_RemoteDestoryed(addr.ToRemoteAddress());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void r2s_RpcWormhole(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, DeepFrozenIceImpl.BinaryMessage msg, bool srcIsBin, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_RemoteRpcWormhole(from.ToRemoteAddress(), addr.ToRemoteAddress(), msg.ToBinary(), srcIsBin);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override async Task<DeepFrozenIceImpl.BinaryMessage> r2s_RpcWormholeReturnAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, DeepFrozenIceImpl.BinaryMessage msg, bool srcIsBin, Ice.Current current = null)
        {
            try
            {
                var rbin = await nodeService.r2s_RemoteRpcWormholeAsync(from.ToRemoteAddress(), addr.ToRemoteAddress(), msg.ToBinary(), srcIsBin);
                return rbin.ToIceBinary();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        public override void r2s_RpcWormholeWithType(RpcAddress from, string serviceType, DeepFrozenIceImpl.BinaryMessage msg, bool srcIsBin, Ice.Current current = null)
        {
            try
            {
                nodeService.r2s_RemoteRpcWormholeWithType(from.ToRemoteAddress(), serviceType, msg.ToBinary(), srcIsBin);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
    }
}
