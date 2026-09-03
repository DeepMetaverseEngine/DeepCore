using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Threading;
using DeepCrystal.Grid;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.ServiceNode.IRpcServiceNodeAdapter;

namespace DeepFrozen.RPC.Remote.GridImpl
{

    //----------------------------------------------------------------------------------------------------------------------
    public class GridServiceNodeUVAdapter : IRpcServiceNodeAdapter
    {
        private IGridAdapter grid;
        private string localAddress;
        private RpcNodeConfig rpcConfig;
        private IGridProxy nameProxy;
        private ConcurrentDictionary<string, IGridProxy> proxyPool;
        internal RpcServiceNode server;
        internal Logger log;

        public GridServiceNodeUVAdapter(RpcNodeConfig rpc_cfg, Properties host_cfg)
        {
            this.rpcConfig = rpc_cfg;
            this.localAddress = rpc_cfg.LocalEndPoint;
            this.proxyPool = new ConcurrentDictionary<string, IGridProxy>();
            this.grid = GridFactory.Instance.CreateAdapter(RpcMessageFactory.MessageFactory, host_cfg);
            this.grid.OnHandleMessage += Grid_OnHandleMessage;
            this.grid.OnHandleMessageAsync += Grid_OnHandleMessageAsync;
        }
        public void Attach(RpcServiceNode server)
        {
            this.server = server;
            this.log = this.server.log;
        }
        public async Task<bool> StartAsync(RpcServiceNode server)
        {
            try
            {
                var ret = await this.grid.StartAsync(localAddress);
                this.nameProxy = grid.GetProxy(rpcConfig.NameServerEndPoint);
                return ret;
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }
        public Task<int> ShutdownAsync(RpcServiceNode server)
        {
            return Task.FromResult(0);
        }
        public Task<bool> StopAsync(RpcServiceNode server)
        {
            try
            {
                this.grid.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return Task.FromResult(true);
        }
        //----------------------------------------------------------------------------------
        IGridProxy NameProxy { get => nameProxy; }
        IGridProxy GetNodeProxyByName(string nodeName)
        {
            return proxyPool[nodeName];
        }
        IGridProxy GetOrAddNodeServiceProxy(string nodeName, string endPoint)
        {
            if (string.IsNullOrEmpty(endPoint))
            {
                return proxyPool[nodeName];
            }
            else
            {
                return proxyPool.GetOrAdd(nodeName, (key) =>
                {
                    return grid.GetProxy(endPoint);
                });
            }
        }
        //----------------------------------------------------------------------------------

        internal async Task<ISerializable> Grid_OnHandleMessageAsync(IGridProxy proxy, ISerializable req)
        {
            switch (req)
            {
                case n2s_DispatchCreateServiceREQ n2s_DispatchCreateService:
                    return await HandleAsync(proxy, n2s_DispatchCreateService);
                case n2s_DispatchDestoryServiceREQ n2s_DispatchDestoryService:
                    return await HandleAsync(proxy, n2s_DispatchDestoryService);
                case AppBroadcastCommandREQ appCommand:
                    return await HandleAsync(proxy, appCommand);
                case RpcRequestMessageREQ rpcRequest:
                    return await HandleAsync(proxy, rpcRequest);
                default:
                    return null;
            }
        }
        internal void Grid_OnHandleMessage(IGridProxy proxy, ISerializable msg)
        {
            switch (msg)
            {
                case n2s_ServiceDisposingNTF n2s_ServiceDisposing:
                    Handle(proxy, n2s_ServiceDisposing); break;
                case n2s_ServiceDestoryedNTF n2s_ServiceDestoryed:
                    Handle(proxy, n2s_ServiceDestoryed); break;
                case AppBroadcastMessageNTF appBroadcast:
                    Handle(proxy, appBroadcast); break;
                case RpcNotifyMessageNTF rpcNotify:
                    Handle(proxy, rpcNotify); break;
                case RpcNotifyBatchMessageNTF rpcBatchNotify:
                    Handle(proxy, rpcBatchNotify); break;
                case RpcNotifyTypeMessageNTF rpcTypeNotify:
                    Handle(proxy, rpcTypeNotify); break;
                case RpcWormholeMessageNTF rpcWormhole:
                    Handle(proxy, rpcWormhole); break;
                case RpcWormholeTypeMessageNTF rpcWormholeType:
                    Handle(proxy, rpcWormholeType); break;
            }
        }
        public async Task<n2s_DispatchCreateServiceRSP> HandleAsync(IGridProxy proxy, n2s_DispatchCreateServiceREQ req)
        {
            try
            {
                var result = await this.n2s_HandleCreateLocalServiceAsync(
                    req.from,
                    req.path,
                    req.config,
                    req.is_static);
                return new n2s_DispatchCreateServiceRSP()
                {
                    result = result,
                };
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new n2s_DispatchCreateServiceRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<n2s_DispatchDestoryServiceRSP> HandleAsync(IGridProxy proxy, n2s_DispatchDestoryServiceREQ req)
        {
            try
            {
                var result = await this.n2s_HandleDestoryLocalServiceAsync(
                    req.from,
                    req.path,
                    req.reason);
                return new n2s_DispatchDestoryServiceRSP()
                {
                    result = result,
                };
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new n2s_DispatchDestoryServiceRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<AppBroadcastCommandRSP> HandleAsync(IGridProxy proxy, AppBroadcastCommandREQ req)
        {
            try
            {
                var result = await this.n2s_HandleAppCommandAsync(req.notify);
                return new AppBroadcastCommandRSP()
                {
                    notify = result,
                };
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new AppBroadcastCommandRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<RpcResponseMessageRSP> HandleAsync(IGridProxy proxy, RpcRequestMessageREQ req)
        {
            try
            {
                var tcs = server.CreateDefaultTaskCompletionSource<RpcResponseMessageRSP>("RpcRequest");
                this.r2s_HandleRemoteRpcRequest(req.from, req.to, req.msg, (rsp, err) =>
                {
                    tcs.TrySetResult(new RpcResponseMessageRSP()
                    {
                        error = err != null ? new RpcException(err) : null,
                        from = req.to,
                        to = req.from,
                        msg = rsp,
                        result = err == null
                    });
                });
                return await tcs.Task;
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new RpcResponseMessageRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public void Handle(IGridProxy proxy, n2s_ServiceDisposingNTF msg)
        {
            this.n2s_HandleRemoteDisposing(msg.addr);
        }
        public void Handle(IGridProxy proxy, n2s_ServiceDestoryedNTF msg)
        {
            this.n2s_HandleRemoteDestoryed(msg.addr);
        }
        public void Handle(IGridProxy proxy, AppBroadcastMessageNTF msg)
        {
            this.n2s_HandleAppMessage(msg.notify);
        }
        public void Handle(IGridProxy proxy, RpcNotifyMessageNTF msg)
        {
            this.r2s_HandleRemoteRpcNotify(msg.from, msg.to, msg.msg);
        }
        public void Handle(IGridProxy proxy, RpcNotifyBatchMessageNTF msg)
        {
            this.r2s_HandleRemoteRpcBatchNotify(msg.from, msg.to, msg.batch);
        }
        public void Handle(IGridProxy proxy, RpcNotifyTypeMessageNTF msg)
        {
            this.r2s_HandleRemoteRpcNotifyWithType(msg.from, msg.serviceType, msg.msg);
        }
        public void Handle(IGridProxy proxy, RpcWormholeMessageNTF msg)
        {
            this.r2s_HandleRemoteRpcWormhole(msg.from, msg.to, msg.msg, msg.srcIsBin);
        }
        public void Handle(IGridProxy proxy, RpcWormholeTypeMessageNTF msg)
        {
            this.r2s_HandleRemoteRpcWormholeWithType(msg.from, msg.serviceType, msg.msg, msg.srcIsBin);
        }
        //----------------------------------------------------------------------------------
        #region IRpcServiceNodeHandler

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
        public event HandleRemoteRpcWormholeWithType r2s_HandleRemoteRpcWormholeWithType;
        public Task<bool> n2s_CreateLocalServiceAsync(RemoteAddress from, RemoteAddress addr, Dictionary<string, string> config, bool isStatic) => n2s_HandleCreateLocalServiceAsync(from, addr, config, isStatic);
        public Task<bool> n2s_DestoryLocalServiceAsync(RemoteAddress from, RemoteAddress addr, string reason) => n2s_HandleDestoryLocalServiceAsync(from, addr, reason);
        public void n2s_RemoteDisposing(RemoteAddress addr) => n2s_HandleRemoteDisposing(addr);
        public void n2s_RemoteDestoryed(RemoteAddress addr) => n2s_HandleRemoteDestoryed(addr);
        public void n2s_AppMessage(BinaryMessage notify) => n2s_HandleAppMessage(notify);
        public Task<string> n2s_AppCommandAsync(string notify) => n2s_HandleAppCommandAsync(notify);
        public void r2s_RemoteRpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback) => r2s_HandleRemoteRpcRequest(from, to, msg, callback);
        public void r2s_RemoteRpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg) => r2s_HandleRemoteRpcNotify(from, to, msg);
        public void r2s_RemoteRpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg) => r2s_HandleRemoteRpcBatchNotify(from, to, msg);
        public void r2s_RemoteRpcNotifyWithType(RemoteAddress from, string serviceType, BinaryMessage msg) => r2s_HandleRemoteRpcNotifyWithType(from, serviceType, msg);
        public void r2s_RemoteRpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin) => r2s_HandleRemoteRpcWormhole(from, to, msg, srcIsBin);
        public void r2s_RemoteRpcWormholeWithType(RemoteAddress from, string serviceType, BinaryMessage msg, bool srcIsBin) => r2s_HandleRemoteRpcWormholeWithType(from, serviceType, msg, srcIsBin);

        #endregion
        //----------------------------------------------------------------------------------
        #region IRpcServiceNodeAdapter
        public async Task<bool> node2name_RegistNodeAsync(ServiceNodeStartInfo start)
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_RegistNodeREQ()
            {
                info = start,
            }) as s2n_RegistNodeRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    throw rsp.error;
                }
                return rsp.result;
            }
            return false;
        }
        public async Task<bool> node2name_UnregistNodeAsync(string nodeName)
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_UnregistNodeREQ()
            {
                nodeName = nodeName,
            }) as s2n_UnregistNodeRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    throw rsp.error;
                }
                return rsp.result;
            }
            return false;
        }
        public void node2name_UpdateNodeState(ServiceNodeStateInfo state)
        {
            NameProxy.Send(new s2n_UpdateNodeStateNTF() { info = state, });
        }
        public async Task<RemoteProxyInfo> s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config)
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_GetOrCreateRemoteServiceREQ()
            {
                operation = op,
                from = from,
                path = path,
                config = new Properties(config),
            }) as s2n_GetOrCreateRemoteServiceRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    throw rsp.error;
                }
                GetOrAddNodeServiceProxy(rsp.info.Address.ServiceNode, rsp.info.EndPoint);
                return rsp.info;
            }
            return null;
        }
        public async Task<bool> s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_DestoryRemoteServiceREQ()
            {
                from = from,
                path = path,
                reason = reason,
            }) as s2n_DestoryRemoteServiceRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    throw rsp.error;
                }
                return rsp.result;
            }
            return false;
        }
        public async Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType)
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_GetServiceCountREQ()
            {
                serviceNode = serviceNode,
                serviceType = serviceType,
            }) as s2n_GetServiceCountRSP;
            if (rsp == null) { return 0; }
            if (rsp.error != null) { throw rsp.error; }
            return rsp.count;
        }
        public async Task<RemoteProxyInfo[]> s2n_GetRemoteServicesInternalAsync(s2n_GetRemoteServicesREQ req)
        {
            var rsp = await NameProxy.SendRequestAsync(req) as s2n_GetRemoteServicesRSP;
            if (rsp == null) { return null; }
            if (rsp.error != null) { throw rsp.error; }
            try
            {
                if (rsp.infos != null)
                {
                    foreach (var info in rsp.infos)
                    {
                        GetOrAddNodeServiceProxy(info.Address.ServiceNode, info.EndPoint);
                    }
                    return rsp.infos.ToArray();
                }
                return null;
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }
        public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesAsync(ICollection<string> servicesName)
        {
            return s2n_GetRemoteServicesInternalAsync(new s2n_GetRemoteServicesREQ()
            {
                paths = CUtils.ToArray(servicesName),
                isStatic = false,
            });
        }
        public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithAddressPatternAsync(string pattern)
        {
            return s2n_GetRemoteServicesInternalAsync(new s2n_GetRemoteServicesREQ()
            {
                paths = null,
                isStatic = false,
                pattern = pattern,
            });
        }
        public Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy)
        {
            return s2n_GetRemoteServicesInternalAsync(new s2n_GetRemoteServicesREQ()
            {
                paths = null,
                isStatic = false,
                where = where,
                orderBy = orderBy,
            });
        }
        public Task<RemoteProxyInfo[]> s2n_GetStaticServicesAsync()
        {
            return s2n_GetRemoteServicesInternalAsync(new s2n_GetRemoteServicesREQ()
            {
                isStatic = true,
            });
        }
        public async Task<ServiceNodeStartInfo[]> s2n_GetStaticNodesInfoAsync()
        {
            var rsp = await NameProxy.SendRequestAsync(new s2n_GetStaticNodesREQ()) as s2n_GetStaticNodesRSP;
            if (rsp == null) { return null; }
            if (rsp.error != null) { throw rsp.error; }
            return rsp.infos.ToArray();
        }
        public async Task<string> s2n_BroadcastAppCommandAsync(string notify)
        {
            var rsp = await NameProxy.SendRequestAsync(new AppBroadcastCommandREQ()
            {
                notify = notify
            }) as AppBroadcastCommandRSP;
            if (rsp == null) { return null; }
            if (rsp.error != null) { throw rsp.error; }
            return rsp.notify;
        }
        public void s2n_BroadcastAppMessage(BinaryMessage notify)
        {
            NameProxy.Send(new AppBroadcastMessageNTF()
            {
                notify = notify
            });
        }
        public void s2n_BroadcastServiceMessage(RemoteAddress from, BinaryMessage notify)
        {
            NameProxy.Send(new ServiceBroadcastMessageNTF()
            {
                from = from,
                msg = notify,
            });
        }
        //----------------------------------------------------------------------------------
        public void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback)
        {
            var proxy = GetNodeProxyByName(to.ServiceNode);
            proxy.SendRequestAsync(new RpcRequestMessageREQ()
            {
                from = from,
                to = to,
                msg = msg,
            }).ContinueWith(t =>
            {
                var rsp = t.GetResultAs() as RpcResponseMessageRSP;
                if (rsp == null) { callback(BinaryMessage.NULL, null); }
                else if (t.IsCompleted) { callback(rsp.msg, rsp.error); }
                else if (t.Exception != null) { callback(BinaryMessage.NULL, t.Exception); }
                else if (rsp.error != null) { callback(BinaryMessage.NULL, rsp.error); }
                else { callback(rsp.msg, rsp.error); }
            });
        }
        public void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnVoid callback)
        {
            var proxy = GetNodeProxyByName(to.ServiceNode);
            proxy.SendRequestAsync(new RpcRequestMessageREQ()
            {
                from = from,
                to = to,
                msg = msg,
            }).ContinueWith(t =>
            {
                var rsp = t.GetResultAs() as RpcResponseMessageRSP;
                if (rsp == null) { callback(null); }
                else if (t.IsCompleted) { callback(rsp.error); }
                else if (t.Exception != null) { callback(t.Exception); }
                else if (rsp.error != null) { callback(rsp.error); }
                else { callback(rsp.error); }
            });
        }
        public void s2r_RpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg)
        {
            var proxy = GetNodeProxyByName(to.ServiceNode);
            proxy.Send(new RpcNotifyMessageNTF()
            {
                from = from,
                to = to,
                msg = msg,
            });
        }
        public void s2r_RpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg)
        {
            var proxy = GetNodeProxyByName(to.ServiceNode);
            proxy.Send(new RpcNotifyBatchMessageNTF()
            {
                from = from,
                to = to,
                batch = new List<BinaryMessage>(msg),
            });
        }
        public void s2r_RpcNotifyWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg)
        {
            var proxy = GetNodeProxyByName(serviceNode);
            proxy.Send(new RpcNotifyTypeMessageNTF()
            {
                from = from,
                serviceNode = serviceNode,
                serviceType = serviceType,
                msg = msg,
            });
        }
        public void s2r_RpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        {
            var proxy = GetNodeProxyByName(to.ServiceNode);
            proxy.Send(new RpcWormholeMessageNTF()
            {
                from = from,
                to = to,
                msg = msg,
                srcIsBin = srcIsBin,
            });
        }
        public void s2r_RpcWormholeWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg, bool srcIsBin)
        {
            var proxy = GetNodeProxyByName(serviceNode);
            proxy.Send(new RpcWormholeTypeMessageNTF()
            {
                from = from,
                serviceNode = serviceNode,
                serviceType = serviceType,
                msg = msg,
                srcIsBin = srcIsBin,
            });
        }

        #endregion
        //----------------------------------------------------------------------------------
    }
}
