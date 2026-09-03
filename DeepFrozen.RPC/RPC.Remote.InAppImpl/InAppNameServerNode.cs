using DeepCore;
using DeepCore.IO;
using DeepCrystal.Grid;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote.NameServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.NameServer.IRpcNameServerAdapter;

namespace DeepFrozen.RPC.Remote.InAppImpl
{
    internal class InAppNameServerNode : IRpcNameServerAdapter
    {
        public static InAppNameServerNode Instance { get; private set; }
        public static InAppRpcServiceNode ServiceProxy { get => InAppRpcServiceNode.Instance; }
        //----------------------------------------------------------------------------------------------------------------------
        private RpcNameServer handler;
        public InAppNameServerNode() { Instance = this; }
        public void Init(RpcNameServer nameserver)
        {
            this.handler = nameserver;
        }
        public Task StartAsync(RpcNameServer ns)
        {
            return Task.CompletedTask;
        }
        public Task StopAsync(RpcNameServer ns)
        {
            return Task.CompletedTask;
        }
        //----------------------------------------------------------------------------------
        #region IRpcNameServerAdapter
        Task<bool> IRpcNameServerAdapter.n2s_DispatchCreateServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc)
        {
            return ServiceProxy.n2s_CreateLocalServiceAsync(from, svc.Address, svc.info.Config, svc.isStatic);
        }
        Task<bool> IRpcNameServerAdapter.n2s_DispatchDestoryServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc, string reason)
        {
            return ServiceProxy.n2s_DestoryLocalServiceAsync(from, svc.Address, reason);
        }
        Task IRpcNameServerAdapter.n2s_BroadcastRemoteDisposing(RemoteAddress addr)
        {
            ServiceProxy.n2s_RemoteDisposing(addr); 
            return Task.CompletedTask;
        }
        Task IRpcNameServerAdapter.n2s_BroadcastRemoteDestoryed(RemoteAddress addr)
        {
            ServiceProxy.n2s_RemoteDestoryed(addr); 
            return Task.CompletedTask;
        }
        Task IRpcNameServerAdapter.n2s_AppBroadcastMessage(BinaryMessage notify)
        {
            ServiceProxy.n2s_AppMessage(notify);
            return Task.CompletedTask;
        }
        Task<string> IRpcNameServerAdapter.n2s_AppBroadcastCommandAsync(string notify)
        {
            return ServiceProxy.n2s_AppCommandAsync(notify);
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

        public Task<NodeInfo> s2n_RegistNodeAsync(ServiceNodeStartInfo start) => s2n_HandleRegistNodeAsync(start);
        public Task<NodeInfo> s2n_UnregistNodeAsync(string nodeName) => s2n_HandleUnregistNodeAsync(nodeName);
        public Task s2n_UpdateNodeState(ServiceNodeStateInfo state) => s2n_HandleUpdateNodeState(state);
        public Task<ServiceInfo> s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config) => s2n_HandleGetOrCreateRemoteServiceAsync(op, from, path, config);
        public Task<ServiceInfo> s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason) => s2n_HandleDestoryRemoteServiceAsync(from, path, reason);
        public Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType) => s2n_HandleGetServiceCountAsync(serviceNode, serviceType);
        public Task<ServiceInfo[]> s2n_GetRemoteServicesAsync(ICollection<string> paths) => s2n_HandleGetRemoteServicesAsync(paths);
        public Task<ServiceInfo[]> s2n_GetRemoteServicesWithAddressPatternAsync(string pattern) => s2n_HandleGetRemoteServicesWithAddressPatternAsync(pattern);
        public Task<ServiceInfo[]> s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy) => s2n_HandleGetRemoteServicesWithInfoLinqAsync(where, orderBy);
        public Task<ServiceInfo[]> s2n_GetStaticServicesAsync() => s2n_HandleGetStaticServicesAsync();
        public Task<NodeInfo[]> s2n_GetStaticNodesAsync() => s2n_HandleGetStaticNodesAsync();
        public Task s2n_ServiceBroadcastMessage(RemoteAddress from, BinaryMessage notify) => s2n_HandleServiceBroadcastMessage(from, notify);
        public Task s2n_AppBroadcastMessage(BinaryMessage notify) => s2n_HandleAppBroadcastMessage(notify);
        public Task<string> s2n_AppBroadcastCommandAsync(string notify) => s2n_HandleAppBroadcastCommandAsync(notify);

        #endregion
        //----------------------------------------------------------------------------------


        //----------------------------------------------------------------------------------------------------------------------
//         private SNodeInfo nodeProxy;
//         private SServiceInfo serviceProxy;
//         public NodeProxy GetProxy(NodeInfo node) { }
//         public ServiceProxy GetProxy(ServiceInfo svc) { }
// 
// //         public ServiceProxy CreateServiceProxy(NodeProxy node, ServiceInfoMapping info)
// //         {
// //             return new SServiceInfo(node, info);
// //         }
// //         public NodeProxy CreateNodeProxy(NodeInfoMapping info)
// //         {
// //             return new SNodeInfo(this, info);
// //         }
//         public class SNodeInfo : NodeProxy
//         {
//             internal SNodeInfo(InAppNameServerNode ns, NodeInfoMapping req) : base(ns, req)
//             {
//             }
//         }
//         public class SServiceInfo : ServiceProxy
//         {
//             internal SServiceInfo(NodeProxy node, ServiceInfoMapping info)                : base(node, info)
//             {
//             }
//         }
        //----------------------------------------------------------------------------------------------------------------------
    }
}
