using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal.Grid;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote.NameServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.NameServer.IRpcNameServerAdapter;

namespace DeepFrozen.RPC.Remote.GridImpl
{
    class GridNameServerUVAdapter : IRpcNameServerAdapter
    {
        internal IGridAdapter grid;
        internal string localAddress;
        internal RpcNameServer server;
        internal Logger log;

        public GridNameServerUVAdapter(Properties hostConfig)
        {
            this.localAddress = $"{hostConfig["Host"]}:{hostConfig["Listen"]}";
            this.grid = GridFactory.Instance.CreateAdapter(RpcMessageFactory.MessageFactory, hostConfig);
            this.grid.OnHandleMessage += Grid_OnHandleMessage;
            this.grid.OnHandleMessageAsync += Grid_OnHandleMessageAsync;
        }
        public void Init(RpcNameServer server)
        {
            this.server = server as RpcNameServer;
            this.log = this.server.log;
        }
        public Task StartAsync(RpcNameServer nameserver)
        {
            return this.grid.StartAsync(localAddress);
        }
        public Task StopAsync(RpcNameServer nameserver)
        {
            this.grid.Dispose();
            return Task.CompletedTask;
        }
        //----------------------------------------------------------------------------------
        public ServiceProxy CreateServiceInfo(NodeProxy node, ServiceInfo info)
        {
            return new SServiceInfo(node, info);
        }
        public NodeProxy CreateNodeInfo(NodeInfo info)
        {
            return new SNodeInfo(this, info);
        }
        public class SNodeInfo : NodeProxy
        {
            internal IGridProxy proxy;
            internal SNodeInfo(GridNameServerUVAdapter ns, NodeInfo req) : base(ns, req)
            {
            }
        }
        public class SServiceInfo : ServiceProxy
        {
            internal IGridProxy proxy { get => (base.Node as SNodeInfo).proxy; }
            internal SServiceInfo(NodeProxy node, ServiceInfo info) : base(node, info)
            {
            }
        }
        //----------------------------------------------------------------------------------

        internal async Task<ISerializable> Grid_OnHandleMessageAsync(IGridProxy proxy, ISerializable req)
        {
            switch (req)
            {
                case s2n_RegistNodeREQ s2n_RegistNode:
                    return await HandleAsync(proxy, s2n_RegistNode);
                case s2n_UnregistNodeREQ s2n_UnregistNode:
                    return await HandleAsync(proxy, s2n_UnregistNode);
                case s2n_GetOrCreateRemoteServiceREQ s2n_GetOrCreateRemoteService:
                    return await HandleAsync(proxy, s2n_GetOrCreateRemoteService);
                case s2n_DestoryRemoteServiceREQ s2n_DestoryRemoteService:
                    return await HandleAsync(proxy, s2n_DestoryRemoteService);
                case s2n_GetServiceCountREQ s2n_GetServiceCount:
                    return await HandleAsync(proxy, s2n_GetServiceCount);
                case s2n_GetRemoteServicesREQ s2n_GetRemoteServices:
                    return await HandleAsync(proxy, s2n_GetRemoteServices);
                case s2n_GetStaticNodesREQ s2n_GetStaticNodes:
                    return await HandleAsync(proxy, s2n_GetStaticNodes);
                case AppBroadcastCommandREQ appCommand:
                    return await HandleAsync(proxy, appCommand);
                default:
                    return null;
            }
        }
        internal void Grid_OnHandleMessage(IGridProxy proxy, ISerializable msg)
        {
            switch (msg)
            {
                case s2n_UpdateNodeStateNTF s2n_UpdateNodeState:
                    Handle(proxy, s2n_UpdateNodeState); break;
                case AppBroadcastMessageNTF appMessage:
                    Handle(proxy, appMessage); break;
                case ServiceBroadcastMessageNTF broadcast:
                    Handle(proxy, broadcast); break;
            }
        }

        public async Task<s2n_RegistNodeRSP> HandleAsync(IGridProxy proxy, s2n_RegistNodeREQ req)
        {
            try
            {
                var node = await this.s2n_HandleRegistNodeAsync(req.info) as SNodeInfo;
                if (node != null)
                {
                    node.proxy = proxy;
                    return new s2n_RegistNodeRSP() { };
                }
                throw new Exception(req.ToString());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_RegistNodeRSP()
                {
                    result = false,
                    error = new RpcException(err)
                };
            }
        }
        public async Task<s2n_UnregistNodeRSP> HandleAsync(IGridProxy proxy, s2n_UnregistNodeREQ req)
        {
            try
            {
                var node = await this.s2n_HandleUnregistNodeAsync(req.nodeName);
                if (node != null)
                {
                    return new s2n_UnregistNodeRSP() { };
                }
                throw new Exception(req.ToString());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_UnregistNodeRSP()
                {
                    result = false,
                    error = new RpcException(err)
                };
            }
        }
        public async Task<s2n_GetOrCreateRemoteServiceRSP> HandleAsync(IGridProxy proxy, s2n_GetOrCreateRemoteServiceREQ req)
        {
            try
            {
                var svc = await this.s2n_HandleGetOrCreateRemoteServiceAsync(
                    req.operation,
                    req.from,
                    req.path,
                    req.config);
                if (svc != null)
                {
                    return new s2n_GetOrCreateRemoteServiceRSP() { info = svc.Info };
                }
                throw new Exception(req.ToString());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_GetOrCreateRemoteServiceRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<s2n_DestoryRemoteServiceRSP> HandleAsync(IGridProxy proxy, s2n_DestoryRemoteServiceREQ req)
        {
            try
            {
                var svc = await this.s2n_HandleDestoryRemoteServiceAsync(
                    req.from,
                    req.path,
                    req.reason);
                if (svc != null)
                {
                    return new s2n_DestoryRemoteServiceRSP() { path = svc.Address };
                }
                throw new Exception(req.ToString());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_DestoryRemoteServiceRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<s2n_GetServiceCountRSP> HandleAsync(IGridProxy proxy, s2n_GetServiceCountREQ req)
        {
            try
            {
                var count = await this.s2n_HandleGetServiceCountAsync(req.serviceNode, req.serviceType);
                return new s2n_GetServiceCountRSP() { count = count };
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_GetServiceCountRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<s2n_GetRemoteServicesRSP> HandleAsync(IGridProxy proxy, s2n_GetRemoteServicesREQ req)
        {
            try
            {
                if (req.isStatic)
                {
                    var array = await this.s2n_HandleGetStaticServicesAsync();
                    return new s2n_GetRemoteServicesRSP()
                    {
                        infos = new List<RemoteProxyInfo>(Array.ConvertAll(array, t => t.Info))
                    };
                }
                else if (!string.IsNullOrEmpty(req.pattern))
                {
                    var array = await this.s2n_HandleGetRemoteServicesWithAddressPatternAsync(req.pattern);
                    return new s2n_GetRemoteServicesRSP()
                    {
                        infos = new List<RemoteProxyInfo>(Array.ConvertAll(array, t => t.Info))
                    };
                }
                else if (!string.IsNullOrEmpty(req.where))
                {
                    var array = await this.s2n_HandleGetRemoteServicesWithInfoLinqAsync(req.where, req.orderBy);
                    return new s2n_GetRemoteServicesRSP()
                    {
                        infos = new List<RemoteProxyInfo>(Array.ConvertAll(array, t => t.Info))
                    };
                }
                else if (req.paths != null)
                {
                    var array = await this.s2n_HandleGetRemoteServicesAsync(req.paths);
                    return new s2n_GetRemoteServicesRSP()
                    {
                        infos = new List<RemoteProxyInfo>(Array.ConvertAll(array, t => t.Info))
                    };
                }
                else
                {
                    return new s2n_GetRemoteServicesRSP() { infos = new List<RemoteProxyInfo>() };
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_GetRemoteServicesRSP()
                {
                    result = false,
                    error = new RpcException(err),
                };
            }
        }
        public async Task<s2n_GetStaticNodesRSP> HandleAsync(IGridProxy proxy, s2n_GetStaticNodesREQ req)
        {
            try
            {
                var result = await this.s2n_HandleGetStaticNodesAsync();
                return new s2n_GetStaticNodesRSP()
                {
                    infos = new List<ServiceNodeStartInfo>(Array.ConvertAll(result, o => o.Token))
                };
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return new s2n_GetStaticNodesRSP()
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
                var result = await this.s2n_HandleAppBroadcastCommandAsync(req.notify);
                return new AppBroadcastCommandRSP() { notify = result };
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
        public void Handle(IGridProxy proxy, s2n_UpdateNodeStateNTF msg)
        {
            this.s2n_HandleUpdateNodeState(msg.info);
        }
        public void Handle(IGridProxy proxy, AppBroadcastMessageNTF msg)
        {
            this.s2n_HandleAppBroadcastMessage(msg.notify);
        }
        public void Handle(IGridProxy proxy, ServiceBroadcastMessageNTF msg)
        {
            this.s2n_HandleServiceBroadcastMessage(msg.from, msg.msg);
        }

        //----------------------------------------------------------------------------------
        #region IRpcNameServerAdapter
        public async Task<bool> n2s_DispatchCreateServiceAsync(RemoteAddress from, ServiceProxy svc, Dictionary<string, string> config, bool isStatic)
        {
            var ssvc = svc as SServiceInfo;
            var session = ssvc.proxy;
            var rsp = await session.SendRequestAsync(new n2s_DispatchCreateServiceREQ()
            {
                from = from,
                path = svc.Address,
                config = new Properties(config),
                is_static = isStatic,
            }) as n2s_DispatchCreateServiceRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    server.log.Error(rsp.error);
                }
                return rsp.result;
            }
            return false;
        }
        public async Task<bool> n2s_DispatchDestoryServiceAsync(RemoteAddress from, ServiceProxy svc, string reason)
        {
            var ssvc = svc as SServiceInfo;
            var session = ssvc.proxy;
            var rsp = await session.SendRequestAsync(new n2s_DispatchDestoryServiceREQ()
            {
                from = from,
                path = svc.Address,
                reason = reason,
            }) as n2s_DispatchDestoryServiceRSP;
            if (rsp != null)
            {
                if (rsp.error != null)
                {
                    server.log.Error(rsp.error);
                }
                return rsp.result;
            }
            return false;
        }
        public void n2s_BroadcastRemoteDisposing(RemoteAddress addr)
        {
            server.ForEachNodes<SNodeInfo>((node) =>
            {
                var session = node.proxy;
                session.Send(new n2s_ServiceDisposingNTF() { addr = addr });
            });
        }
        public void n2s_BroadcastRemoteDestoryed(RemoteAddress addr)
        {
            server.ForEachNodes<SNodeInfo>((node) =>
            {
                var session = node.proxy;
                session.Send(new n2s_ServiceDestoryedNTF() { addr = addr });
            });
        }
        public void n2s_AppBroadcastMessage(BinaryMessage notify)
        {
            server.ForEachNodes<SNodeInfo>((node) =>
            {
                var session = node.proxy;
                session.Send(new AppBroadcastMessageNTF() { notify = notify });
            });
        }
        public async Task<string> n2s_AppBroadcastCommandAsync(string notify)
        {
            StringBuilder sb = new StringBuilder();
            var nodes = server.GetAllNodes();
            foreach (var node in nodes)
            {
                var session = (node as SNodeInfo).proxy;
                var rsp = await session.SendRequestAsync(new AppBroadcastCommandREQ() { notify = notify }) as AppBroadcastCommandRSP;
                if (rsp != null && !string.IsNullOrEmpty(rsp.notify))
                {
                    sb.AppendLine(rsp.notify);
                }
            }
            return sb.ToString();
        }

        #endregion
        //----------------------------------------------------------------------------------
        #region IRpcNameServerHandler

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

        #endregion
    }
}
