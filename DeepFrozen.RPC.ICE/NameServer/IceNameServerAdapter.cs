using DeepCore;
using DeepCore.IO;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote;
using DeepFrozen.RPC.Remote.NameServer;
using Ice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.NameServer.IRpcNameServerAdapter;

namespace DeepFrozen.ICE.NameServer
{
    //-------------------------------------------------------------------------------------------------------------------
    public class IceNameServerAdapter : IRpcNameServerAdapter
    {
        internal Ice.Communicator communicator;
        private readonly ConcurrentDictionary<string, DeepFrozenIceImpl.IRpcServiceAdapterPrx> nodeProxies = new();
        public RpcNameServer handler { get; private set; }
        public Ice.Communicator Communicator { get => communicator; }
        public IceNameServerAdapter(Ice.Communicator com, RpcNameConfig cfg)
        {
            this.communicator = com;
        }
        public virtual IceNameServerDisp CreateNameServerI(DeepCore.Log.Logger log)
        {
            return new IceNameServerDisp(log, communicator, this);
        }
        protected virtual DeepFrozenIceImpl.IRpcServiceAdapterPrx GetNodeProxy(NodeInfo node)
        {
            return nodeProxies.GetOrAdd(node.NodeName, name => IceProxyFactory.Instance.CreateNodeServiceProxy(communicator, name, node.EndPoint));
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region IRpcNameServerAdapter
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        void IRpcNameServerAdapter.Init(RpcNameServer nameserver)
        {
            this.handler = nameserver;
        }
        Task IRpcNameServerAdapter.StartAsync(RpcNameServer nameserver)
        {
            return Task.CompletedTask;
        }
        Task IRpcNameServerAdapter.StopAsync(RpcNameServer nameserver)
        {
            return Task.CompletedTask;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        Task<bool> IRpcNameServerAdapter.n2s_DispatchCreateServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc)
        {
            var session = GetNodeProxy(node);
            return session.n2s_CreateLocalServiceAsync(from, svc.Address, svc.info.Config, svc.isStatic);
        }
        Task<bool> IRpcNameServerAdapter.n2s_DispatchDestoryServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc, string reason)
        {
            var session = GetNodeProxy(node);
            return session.n2s_DestoryLocalServiceAsync(from, svc.Address, reason);
        }
        async Task IRpcNameServerAdapter.n2s_AppBroadcastMessage(BinaryMessage notify)
        {
            var bin = notify.ToIceBinary();
            var nodes = await handler.GetAllNodesAsync();
            foreach (var node in nodes)
            {
                var session = GetNodeProxy(node);
                await session.n2s_AppMessageNotifyAsync(bin);
            }
        }
        async Task<string> IRpcNameServerAdapter.n2s_AppBroadcastCommandAsync(string notify)
        {
            var sb = new StringBuilder();
            foreach (var node in await handler.GetAllNodesAsync())
            {
                var session = GetNodeProxy(node);
                sb.AppendLine("------------------------------------------------------");
                sb.AppendLine("- " + node.NodeName + "@" + node.EndPoint);
                sb.AppendLine("------------------------------------------------------");
                var rst = await session.n2s_AppCommandAsync(notify);
                if (rst != null)
                {
                    sb.AppendLine(rst);
                }
            }
            sb.AppendLine("------------------------------------------------------");
            return sb.ToString();
        }
        async Task IRpcNameServerAdapter.n2s_BroadcastRemoteDisposing(RemoteAddress addr)
        {
            var raddr = addr.ToIceAddress();
            var nodes = await handler.GetAllNodesAsync();
            foreach (var node in nodes)
            {
                var session = GetNodeProxy(node);
                await session.r2s_RemoteServiceDisposingAsync(raddr);
            }
        }
        async Task IRpcNameServerAdapter.n2s_BroadcastRemoteDestoryed(RemoteAddress addr)
        {
            var raddr = addr.ToIceAddress();
            var nodes = await handler.GetAllNodesAsync();
            foreach (var node in nodes)
            {
                var session = GetNodeProxy(node);
                await session.r2s_RemoteServiceDestoryedAsync(raddr);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        public event HandleRegistNodeAsync s2n_HandleRegistNodeAsync;
        public event HandleUnregistNodeAsync s2n_HandleUnregistNodeAsync;
        public event HandleUpdateNodeState s2n_HandleUpdateNodeState;
        public event HandleGetOrCreateRemoteServiceAsync s2n_HandleGetOrCreateRemoteServiceAsync;
        public event HandleDestoryRemoteServiceAsync s2n_HandleDestoryRemoteServiceAsync;
        public event HandleGetServiceCountAsync s2n_HandleGetServiceCountAsync;
        public event HandleGetRemoteServicesAsync s2n_HandleGetRemoteServicesAsync;
        public event HandleGetRemoteServicesWithAddressPatternAsync s2n_HandleGetRemoteServicesWithAddressPatternAsync;
        public event HandleGetRemoteServicesWithInfoLinqAsync s2n_HandleGetRemoteServicesWithInfoLinqAsync;
        public event HandleGetStaticServicesAsync s2n_HandleGetStaticServicesAsync;
        public event HandleGetStaticNodesAsync s2n_HandleGetStaticNodesAsync;
        public event HandleServiceBroadcastMessage s2n_HandleServiceBroadcastMessage;
        public event HandleAppBroadcastMessage s2n_HandleAppBroadcastMessage;
        public event HandleAppBroadcastCommandAsync s2n_HandleAppBroadcastCommandAsync;

        #endregion IRpcNameServerAdapter
        //----------------------------------------------------------------------------------------------------------------------
        #region NameServer <-> Node
        protected internal virtual async Task<bool> main_RpcRegistNodeRequestS2N(DeepFrozenIceImpl.NodeStartInfo start)
        {
            //TODO 所有负载加载完成后，启动初始化服务

            var node = await s2n_HandleRegistNodeAsync(start.ToServiceNodeStartInfo());
            if (node != null)
            {
                GetNodeProxy(node);
                //node.proxy = session;
                return true;
            }
            return false;
            //throw new System.Exception("Can Not Regist Node : " + start.NodeName);
        }
        protected internal virtual async Task<bool> main_RpcUnregistNodeRequestS2N(string nodeName)
        {
            var node = await s2n_HandleUnregistNodeAsync(nodeName);
            if (node != null)
            {
                return true;
            }
            return false;
            //throw new System.Exception("Unregist Node Not Found : " + nodeName);
        }
        protected internal virtual void main_RpcUpdateNodeStateNotifyS2N(DeepFrozenIceImpl.NodeStateInfo state)
        {
            s2n_HandleUpdateNodeState(state.ToServiceNodeStateInfo());
        }
        #endregion
        //----------------------------------------------------------------------------------
        #region NameServer <-> Services
        //----------------------------------------------------------------------------------
        protected internal virtual async Task<DeepFrozenIceImpl.ServiceProxyInfo> main_RpcGetOrCreateServiceRequestS2N(DeepFrozenIceImpl.GetServiceOperation op, DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress path, Dictionary<string, string> config)
        {
            var svc = await s2n_HandleGetOrCreateRemoteServiceAsync((GetServiceOperation)op, from.ToRemoteAddress(), path.ToRemoteAddress(), config);
            if (svc != null)
            {
                return svc.info;
            }
            else
            {
                return null;
            }
            //throw new Exception("Get Or Create Service Error !!!");
        }
        protected internal virtual async Task<bool> main_RpcDestoryServiceRequestS2N(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress path, string reason)
        {
            var svc = await s2n_HandleDestoryRemoteServiceAsync(from.ToRemoteAddress(), path.ToRemoteAddress(), reason);
            if (svc != null)
            {
                return true;
            }
            return false;
        }
        protected internal virtual Task<int> main_RpcGetServicesCountS2N(string serviceNode, string serviceType)
        {
            return s2n_HandleGetServiceCountAsync(serviceNode, serviceType);
        }
        protected internal virtual async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> main_RpcGetServicesRequestS2N(string[] paths)
        {
            var array = await s2n_HandleGetRemoteServicesAsync(paths);
            var ret = new DeepFrozenIceImpl.ServiceProxyInfo[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].info;
            }
            return ret;
        }
        protected internal virtual async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> main_RpcGetServicesWithPatternRequestS2N(string pattern)
        {
            var array = await s2n_HandleGetRemoteServicesWithAddressPatternAsync(pattern);
            var ret = new DeepFrozenIceImpl.ServiceProxyInfo[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].info;
            }
            return ret;
        }
        protected internal virtual async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> main_RpcGetServicesWithLinqRequestS2N(string where, string orderBy)
        {
            var array = await s2n_HandleGetRemoteServicesWithInfoLinqAsync(where, orderBy);
            var ret = new DeepFrozenIceImpl.ServiceProxyInfo[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].info;
            }
            return ret;
        }
        protected internal virtual async Task<DeepFrozenIceImpl.ServiceProxyInfo[]> main_RpcGetStaticServicesRequestS2N()
        {
            var array = await s2n_HandleGetStaticServicesAsync();
            var ret = new DeepFrozenIceImpl.ServiceProxyInfo[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].info;
            }
            return ret;
        }
        protected internal virtual async Task<DeepFrozenIceImpl.NodeStartInfo[]> main_RpcGetStaticNodesInfoAsyncS2N()
        {
            var array = await s2n_HandleGetStaticNodesAsync();
            var ret = new DeepFrozenIceImpl.NodeStartInfo[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i].token;
            }
            return ret;
        }
        protected internal virtual void main_RpcBroadcastS2N(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.BinaryMessage msg)
        {
            s2n_HandleServiceBroadcastMessage(from.ToRemoteAddress(), msg.ToBinary());
        }
        protected internal virtual Task<string> main_RpcAppCommandAsyncS2N(string msg)
        {
            return s2n_HandleAppBroadcastCommandAsync(msg);
        }
        protected internal virtual void main_RpcBroadcastAppS2N(BinaryMessage msg)
        {
            s2n_HandleAppBroadcastMessage(msg);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------


    }
}
