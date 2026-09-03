using DeepCore;
using DeepCore.IO;
using DeepFrozen.RPC.Remote;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozenIceImpl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepFrozen.ICE.NameServer
{
    //-------------------------------------------------------------------------------------------------------------------
    public class IceNameServerDisp : DeepFrozenIceImpl.IRpcNameServerAdapterDisp_
    {
        protected internal readonly DeepCore.Log.Logger log;
        protected internal readonly Ice.Communicator communicator;
        protected internal readonly IceNameServerAdapter nameServer;

        public IceNameServerDisp(DeepCore.Log.Logger log, Ice.Communicator com, IceNameServerAdapter server)
        {
            this.log = log;
            this.communicator = com;
            this.nameServer = server;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Node - NameServer

        public override async Task<bool> node_RegistNodeAsync(DeepFrozenIceImpl.NodeStartInfo start, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcRegistNodeRequestS2N(start);
            }
            catch (System.Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<bool> node_UnregistNodeAsync(string nodeName, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcUnregistNodeRequestS2N(nodeName);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override void node_UpdateNodeState(DeepFrozenIceImpl.NodeStateInfo state, Ice.Current current = null)
        {
            try
            {
                nameServer.main_RpcUpdateNodeStateNotifyS2N(state);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Service - NameServer

        public override async Task<DeepFrozenIceImpl.ServiceProxyInfo> svc_GetOrCreateRemoteServiceAsync(DeepFrozenIceImpl.GetServiceOperation op, DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress path, Dictionary<string, string> config, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetOrCreateServiceRequestS2N(op, from, path, config);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<bool> svc_DestoryRemoteServiceAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress path, string reason, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcDestoryServiceRequestS2N(from, path, reason);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<int> svc_GetServiceCountAsync(string serviceNode, string serviceType, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetServicesCountS2N(serviceNode, serviceType);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> svc_GetRemoteServicesAsync(string[] paths, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetServicesRequestS2N(paths);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> svc_GetRemoteServicesWithPatternAsync(string pattern, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetServicesWithPatternRequestS2N(pattern);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> svc_GetRemoteServicesWithLinqAsync(string where, string orderBy, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetServicesWithLinqRequestS2N(where, orderBy);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> svc_GetStaticServicesAsync(Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetStaticServicesRequestS2N();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override async Task<DeepFrozenIceImpl.NodeStartInfo[]> svc_GetStaticNodesInfoAsync(Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcGetStaticNodesInfoAsyncS2N();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw new DeepFrozenIceImpl.RpcException(err.Message, err.FullStackTrace(), err);
            }
        }
        public override void svc_Broadcast(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.BinaryMessage msg, Ice.Current current = null)
        {
            try
            {
                nameServer.main_RpcBroadcastS2N(from, msg);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void svc_BroadcastAppMessage(DeepFrozenIceImpl.BinaryMessage msg, Ice.Current current = null)
        {
            try
            {
                nameServer.main_RpcBroadcastAppS2N(msg.ToBinary());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override async Task<string> svc_AppCommandAsync(string msg, Ice.Current current = null)
        {
            try
            {
                return await nameServer.main_RpcAppCommandAsyncS2N(msg);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return err.Message + Environment.NewLine + err.FullStackTrace();
            }

        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------------

    }

}
