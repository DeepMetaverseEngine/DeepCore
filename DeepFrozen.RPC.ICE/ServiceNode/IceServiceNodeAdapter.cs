using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.ServiceNode.IRpcServiceNodeAdapter;

namespace DeepFrozen.ICE.ServiceNode
{
    //-------------------------------------------------------------------------------------------------------------------
    public class IceServiceNodeAdapter : IRpcServiceNodeAdapter
    {
        internal DeepCore.Log.Logger log;
        internal Ice.Communicator communicator;
        internal string nameServerEndPoint;
        private DeepCore.IO.Utils.RequestListener<DeepFrozenIceImpl.BinaryMessage> requestMap;
        private DeepFrozenIceImpl.IRpcNameServerAdapterPrx nameProxy;
        private ConcurrentDictionary<string, ServiceProxyInfo> proxyPool;
        private RpcServiceNode handler;
        public RpcServiceNode serviceNode { get; private set; }
        public IceServiceNodeAdapter(Ice.Communicator com, RpcNodeConfig cfg)
        {
            this.nameServerEndPoint = cfg.NameServerEndPoint;
            this.communicator = com;
            this.proxyPool = new ConcurrentDictionary<string, ServiceProxyInfo>();
            this.requestMap = new DeepCore.IO.Utils.RequestListener<DeepFrozenIceImpl.BinaryMessage>(RpcServiceNode.NETWORK_TIMEOUT_MS);
        }
        //-------------------------------------------------------------------------------------------------------------------
        public void Attach(RpcServiceNode server)
        {
            this.log = server.log;
            this.serviceNode = server;
            this.handler = server;
        }
        public Task<bool> StartAsync(RpcServiceNode server)
        {
            this.nameProxy = CreateNameServerProxy(nameServerEndPoint);
            if (this.nameProxy == null)
            {
                throw new Exception("Can Not Find NameServer From : " + nameServerEndPoint);
            }
            try
            {
                var tick = TimeSpan.FromMilliseconds(RpcServiceNode.REQUEST_TICK_TIME_MS);
                server.RpcTimers.CreateTimer(tick, tick, false, this, t => requestMap.CheckRequestTimeout());
                return Task.FromResult(true);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public Task<int> ShutdownAsync(RpcServiceNode server)
        {
            try
            {
                this.requestMap.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err);
                return Task.FromResult(-1);
            }
            return Task.FromResult(1);
        }
        public Task<bool> StopAsync(RpcServiceNode server)
        {
            return Task.FromResult(true);
        }
        //----------------------------------------------------------------------------------------------------------------  
        public IceServiceNodeDisp CreateServiceNodeI(DeepCore.Log.Logger log)
        {
            return new IceServiceNodeDisp(log, communicator, this);
        }
        protected DeepFrozenIceImpl.IRpcNameServerAdapterPrx CreateNameServerProxy(string endPoint)
        {
            return IceProxyFactory.Instance.CreateNameServerProxy(communicator, endPoint);
        }
        protected ServiceProxyInfo GetCachedServiceProxy(string nodeName)
        {
            return proxyPool[nodeName];
        }
        protected ServiceProxyInfo GetOrAddNodeServiceProxy(string nodeName, string endPoint)
        {
            if (string.IsNullOrEmpty(endPoint))
            {
                return proxyPool[nodeName];
            }
            else
            {
                return proxyPool.GetOrAdd(nodeName, (key) =>
                {
                    var prx = IceProxyFactory.Instance.CreateNodeServiceProxy(communicator, nodeName, endPoint);
                    return new ServiceProxyInfo(prx);
                });
            }
        }
        //----------------------------------------------------------------------------------------------------------------
        #region Node->NameServer

        public Task<bool> s2n_RegistNodeAsync(ServiceNodeStartInfo start)
        {
            return nameProxy.node_RegistNodeAsync(start);
        }
        public Task<bool> s2n_UnregistNodeAsync(string nodeName)
        {
            return nameProxy.node_UnregistNodeAsync(nodeName);
        }
        public void s2n_UpdateNodeState(ServiceNodeStateInfo state)
        {
            nameProxy.node_UpdateNodeStateAsync(state);
        }
        public async Task<RemoteProxyInfo> s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config)
        {
            var info = await nameProxy.svc_GetOrCreateRemoteServiceAsync((DeepFrozenIceImpl.GetServiceOperation)op, from, path, config);
            if (info == null || info.Address == null || string.IsNullOrEmpty(info.Address.ServiceName))
            {
                if (op == GetServiceOperation.Create || op == GetServiceOperation.GetOrCreate)
                {
                    throw new System.Exception("Can Not Get Or Create Service : " + path);
                }
                return null;
            }
            GetOrAddNodeServiceProxy(info.Address.ServiceNode, info.EndPoint);
            return info.ToRemoteProxyInfo();
        }
        public async Task<bool> s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            var rst = await nameProxy.svc_DestoryRemoteServiceAsync(from, path, reason);
            if (rst == false)
            {
                throw new System.Exception("Can Not Destory Service : " + path);
            }
            return true;
        }
        public Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType)
        {
            return nameProxy.svc_GetServiceCountAsync(serviceNode, serviceType);
        }
        public async Task<RemoteProxyInfo[]> s2n_GetRemoteServicesAsync(ICollection<string> serviceNames)
        {
            var rsp = await nameProxy.svc_GetRemoteServicesAsync(serviceNames.ToArray());
            return Array.ConvertAll(rsp, (e) =>
            {
                GetOrAddNodeServiceProxy(e.Address.ServiceNode, e.EndPoint);
                return e.ToRemoteProxyInfo();
            });
        }
        public async Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithPatternAsync(string pattern)
        {
            var rsp = await nameProxy.svc_GetRemoteServicesWithPatternAsync(pattern);
            return Array.ConvertAll(rsp, (e) =>
            {
                GetOrAddNodeServiceProxy(e.Address.ServiceNode, e.EndPoint);
                return e.ToRemoteProxyInfo();
            });
        }
        public async Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy)
        {
            var rsp = await nameProxy.svc_GetRemoteServicesWithLinqAsync(where, orderBy);
            return Array.ConvertAll(rsp, (e) =>
            {
                GetOrAddNodeServiceProxy(e.Address.ServiceNode, e.EndPoint);
                return e.ToRemoteProxyInfo();
            });
        }
        public async Task<RemoteProxyInfo[]> s2n_GetStaticServicesAsync()
        {
            var rsp = await nameProxy.svc_GetStaticServicesAsync();
            return Array.ConvertAll(rsp, (e) =>
            {
                GetOrAddNodeServiceProxy(e.Address.ServiceNode, e.EndPoint);
                return e.ToRemoteProxyInfo();
            });
        }
        public async Task<ServiceNodeStartInfo[]> s2n_GetStaticNodesInfoAsync()
        {
            var rsp = await nameProxy.svc_GetStaticNodesInfoAsync();
            return Array.ConvertAll(rsp, (e) =>
            {
                GetOrAddNodeServiceProxy(e.NodeName, e.EndPoint);
                return e.ToServiceNodeStartInfo();
            });
        }
        public void s2n_Broadcast(RemoteAddress from, BinaryMessage msg)
        {
            var bin = msg.ToIceBinary();
            nameProxy.svc_BroadcastAsync(from, bin);
        }
        public void s2n_BroadcastApp(BinaryMessage msg)
        {
            nameProxy.svc_BroadcastAppMessageAsync(msg.ToIceBinary());
        }
        public Task<string> s2n_AppCommandAsync(string msg)
        {
            return nameProxy.svc_AppCommandAsync(msg);
        }
        #endregion


        //----------------------------------------------------------------------------------------------------------------
        #region NameServer->Node

        public Task<string> n2s_RpcAppCommandAsync(string msg)
        {
            return n2s_HandleAppCommandAsync(msg);
        }
        public void n2s_RpcAppMessageNotify(DeepFrozenIceImpl.BinaryMessage system)
        {
            var msg = system.ToBinary();
            if (msg.HasRoute)
            {
                n2s_HandleAppMessage(msg);
            }
            else
            {
                log.Error("Can not decode system message : " + system.route);
            }
        }
        public Task<bool> n2s_CreateLocalServiceAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, Dictionary<string, string> config, bool isStatic)
        {
            return n2s_HandleCreateLocalServiceAsync(from.ToRemoteAddress(), addr.ToRemoteAddress(), config, isStatic);
        }
        public Task<bool> n2s_DestoryLocalServiceAsync(DeepFrozenIceImpl.RpcAddress from, DeepFrozenIceImpl.RpcAddress addr, string reason)
        {
            return n2s_HandleDestoryLocalServiceAsync(from.ToRemoteAddress(), addr.ToRemoteAddress(), reason);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------
        #region LocalService<->RemoteService

        public void s2r_RpcNotify(RemoteAddress from, RemoteAddress path, BinaryMessage msg)
        {
            var prx = GetCachedServiceProxy(path.ServiceNode);
            if (prx != null)
            {
                var bin = msg.ToIceBinary();
                prx.prx_oneway.r2s_RpcNotifyAsync(from, path, bin);
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : RpcNotify : from={0} to={1}", from, path);
            }
        }
        public void s2r_RpcBatchNotify(RemoteAddress from, RemoteAddress path, ICollection<BinaryMessage> msg)
        {
            var prx = GetCachedServiceProxy(path.ServiceNode);
            if (prx != null)
            {
                prx.prx_oneway.r2s_RpcBatchNotifyAsync(from, path, msg.ToIceBinaryArray());
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : RpcBatchNotify : from={0} to={1}", from, path);
            }
        }
        public void s2r_RpcRequest(RemoteAddress from, RemoteAddress path, BinaryMessage msg, OnRpcReturnBinary callback)
        {
            var prx = GetCachedServiceProxy(path.ServiceNode);
            if (prx != null)
            {
                void on_response(DeepFrozenIceImpl.BinaryMessage rsp, Exception err)
                {
                    if (err != null)
                        callback(BinaryMessage.NULL, err);
                    else if (rsp != null)
                        callback(rsp.ToBinary(), null);
                    else
                        callback(BinaryMessage.NULL, null);
                }
                if (requestMap.Listen(on_response, out var sendID))
                {
                    var bin = msg.ToIceBinary();
                    prx.prx_oneway.r2s_RpcRequestAsync(handler.LocalEndPoint, from, path, sendID, bin);
                }
                else
                {
                    callback(BinaryMessage.NULL);
                }
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : RpcRequest : from={0} to={1}", from, path);
                callback(BinaryMessage.NULL);
            }
        }
        public void r2s_RpcResponse(int sendID, DeepFrozenIceImpl.BinaryMessage msg, Exception err)
        {
            if (!requestMap.OnHandleResponse(sendID, msg, err))
            {
                log.WarnFormat("r2s_RpcResponse Not Exist : RpcResponse : sendID={0} msg={1}", sendID, msg.route);
            }
        }

        public void s2r_RpcWormhole(RemoteAddress from, RemoteAddress path, BinaryMessage msg, bool srcIsBin)
        {
            var prx = GetCachedServiceProxy(path.ServiceNode);
            if (prx != null)
            {
                var bin = msg.ToIceBinary();
                prx.prx_oneway.r2s_RpcWormholeAsync(from, path, bin, srcIsBin);
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : RpcWormhole : from={0} to={1}", from, path);
            }
        }
        public async Task<BinaryMessage> s2r_RpcWormholeAsync(RemoteAddress from, RemoteAddress path, BinaryMessage msg, bool srcIsBin)
        {
            var prx = GetCachedServiceProxy(path.ServiceNode);
            if (prx != null)
            {
                var bin = msg.ToIceBinary();
                var rbin = await prx.prx_oneway.r2s_RpcWormholeReturnAsync(from, path, bin, srcIsBin);
                return rbin.ToBinary();
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : RpcWormhole : from={0} to={1}", from, path);
            }
            return BinaryMessage.NULL;
        }
        #endregion

        public event HandleCreateLocalServiceAsync n2s_HandleCreateLocalServiceAsync;
        public event HandleDestoryLocalServiceAsync n2s_HandleDestoryLocalServiceAsync;
        public event HandleRemoteDisposing n2s_HandleRemoteDisposing;
        public event HandleRemoteDestoryed n2s_HandleRemoteDestoryed;
        public event HandleAppMessage n2s_HandleAppMessage;
        public event HandleAppCommandAsync n2s_HandleAppCommandAsync;
        public event HandleRemoteRpcRequest r2s_HandleRemoteRpcRequest;
        public event HandleRemoteRpcNotify r2s_HandleRemoteRpcNotify;
        public event HandleRemoteRpcBatchNotify r2s_HandleRemoteRpcBatchNotify;
        public event HandleRemoteRpcNotifyWithType r2s_HandleRemoteRpcNotifyWithType;
        public event HandleRemoteRpcWormhole r2s_HandleRemoteRpcWormhole;
        public event HandleRemoteRpcWormholeAsync r2s_HandleRemoteRpcWormholeAsync;
        public event HandleRemoteRpcWormholeWithType r2s_HandleRemoteRpcWormholeWithType;

        // process request with local
        public void r2s_ProcessRequest(string fromNodeEndPoint, RemoteAddress from, RemoteAddress path, int sendID, BinaryMessage msg)
        {
            var fromPrx = GetOrAddNodeServiceProxy(from.ServiceNode, fromNodeEndPoint);
            if (fromPrx != null)
            {
                r2s_HandleRemoteRpcRequest(from, path, msg, (rsp, err) =>
                {
                    var bin = rsp.ToIceBinary();
                    fromPrx.prx_oneway.s2r_RpcResponseAsync(sendID, bin, err);
                });
            }
            else
            {
                log.ErrorFormat("Remote Service Proxy Not Exist : r2s_RpcRequest : from={0} to={1}", from, path);
            }
        }
        public void s2r_RpcNotifyWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg)
        {
            if (serviceNode == handler.NodeName)
            {
                r2s_HandleRemoteRpcNotifyWithType(from, serviceType, msg);
            }
            else
            {
                var prx = GetCachedServiceProxy(serviceNode);
                if (prx != null)
                {
                    var bin = msg.ToIceBinary();
                    prx.prx_oneway.r2s_RpcNotifyWithTypeAsync(from, serviceType, bin);
                }
            }
        }
        public void s2r_RpcWormholeWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg, bool srcIsBin)
        {
            if (serviceNode == handler.NodeName)
            {
                r2s_HandleRemoteRpcWormholeWithType(from, serviceType, msg, srcIsBin);
            }
            else
            {
                var prx = GetCachedServiceProxy(serviceNode);
                if (prx != null)
                {
                    var bin = msg.ToIceBinary();
                    prx.prx_oneway.r2s_RpcWormholeWithTypeAsync(from, serviceType, bin, srcIsBin);
                }
            }
        }
        public void r2s_RemoteRpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg)
        {
            r2s_HandleRemoteRpcNotify(from, to, msg);
        }
        public void r2s_RemoteRpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg)
        {
            r2s_HandleRemoteRpcBatchNotify(from, to, msg);
        }
        public void r2s_RemoteRpcNotifyWithType(RemoteAddress from, string serviceType, BinaryMessage msg)
        {
            r2s_HandleRemoteRpcNotifyWithType(from, serviceType, msg);
        }
        public void n2s_RemoteDisposing(RemoteAddress addr)
        {
            n2s_HandleRemoteDisposing(addr);
        }
        public void n2s_RemoteDestoryed(RemoteAddress addr)
        {
            n2s_HandleRemoteDestoryed(addr);
        }
        public void r2s_RemoteRpcWormhole(RemoteAddress from, RemoteAddress addr, BinaryMessage msg, bool srcIsBin)
        {
            r2s_HandleRemoteRpcWormhole(from, addr, msg, srcIsBin);
        }
        public Task<BinaryMessage> r2s_RemoteRpcWormholeAsync(RemoteAddress from, RemoteAddress addr, BinaryMessage msg, bool srcIsBin)
        {
            return r2s_HandleRemoteRpcWormholeAsync(from, addr, msg, srcIsBin);
        }
        public void r2s_RemoteRpcWormholeWithType(RemoteAddress from, string serviceType, BinaryMessage msg, bool srcIsBin)
        {
            r2s_HandleRemoteRpcWormholeWithType(from, serviceType, msg, srcIsBin);
        }

        //----------------------------------------------------------------------------------------------------------------
        public Task<bool> node2name_RegistNodeAsync(ServiceNodeStartInfo start)
        {
            return this.s2n_RegistNodeAsync(start);
        }
        public Task<bool> node2name_UnregistNodeAsync(string nodeName)
        {
            return this.s2n_UnregistNodeAsync(nodeName);
        }
        public void node2name_UpdateNodeState(ServiceNodeStateInfo state)
        {
            this.s2n_UpdateNodeState(state);
        }
        //----------------------------------------------------------------------------------------------------------------
        //         public Task<RemoteProxyInfo> s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config)
        //         {
        //             return this.s2n_GetOrCreateRemoteServiceAsync(op, from, path, config);
        //         }
        //         public Task<bool> s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        //         {
        //             return this.s2n_DestoryRemoteServiceAsync(from, path, reason);
        //         }
        //----------------------------------------------------------------------------------------------------------------
        //         public Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType)
        //         {
        //             return this.s2n_GetServiceCountAsync(serviceNode, serviceType);
        //         }
        //         public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesAsync(ICollection<string> servicesName)
        //         {
        //             return this.s2n_GetRemoteServicesAsync(servicesName);
        //         }
        public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithAddressPatternAsync(string pattern)
        {
            return this.s2n_GetRemoteServicesWithPatternAsync(pattern);
        }
        //         public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy)
        //         {
        //             return this.s2n_GetRemoteServicesWithInfoLinqAsync(where, orderBy);
        //         }
        //         public Task<RemoteProxyInfo[]> s2n_GetStaticServicesAsync()
        //         {
        //             return this.s2n_GetStaticServicesAsync();
        //         }
        //         public Task<ServiceNodeStartInfo[]> s2n_GetStaticNodesInfoAsync()
        //         {
        //             return this.s2n_GetStaticNodesInfoAsync();
        //         }
        public void s2n_BroadcastServiceMessage(RemoteAddress from, BinaryMessage notify)
        {
            this.s2n_Broadcast(from, notify);
        }
        public void s2n_BroadcastAppMessage(BinaryMessage notify)
        {
            this.s2n_BroadcastApp(notify);
        }
        public Task<string> s2n_BroadcastAppCommandAsync(string notify)
        {
            return this.s2n_AppCommandAsync(notify);
        }
        //----------------------------------------------------------------------------------------------------------------
        //         public void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback)
        //         {
        //             this.s2r_RpcRequest(from, to, msg, callback);
        //         }
        public void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnVoid callback)
        {
            this.s2r_RpcRequest(from, to, msg, (bin, err) => callback(err));
        }
        //         public void s2r_RpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg)
        //         {
        //             this.s2r_RpcNotify(from, to, msg);
        //         }
        //         public void s2r_RpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg)
        //         {
        //             this.s2r_RpcBatchNotify(from, to, msg);
        //         }
        //         public void s2r_RpcNotifyWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg)
        //         {
        //             this.s2r_RpcNotifyWithType(from, serviceNode, serviceType, msg);
        //         }
        //         public void s2r_RpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        //         {
        //             this.s2r_RpcWormhole(from, to, msg, srcIsBin);
        //         }
        //         public void s2r_RpcWormholeWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg, bool srcIsBin)
        //         {
        //             this.s2r_RpcWormholeWithType(from, serviceNode, serviceType, msg, srcIsBin);
        //         }
        //----------------------------------------------------------------------------------------------------------------

    }
}
