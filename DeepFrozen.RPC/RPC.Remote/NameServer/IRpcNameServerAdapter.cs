using DeepCore;
using DeepCore.IO;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.NameServer
{
    public struct RpcNameConfig
    {
        public string LocalEndPoint;
        public int NetworkTimeoutMS;
        public IExternalizableFactory RpcCodec;
    }

    public interface IRpcNameServerAdapter
    {
        void Init(RpcNameServer nameserver);
        Task StartAsync(RpcNameServer nameserver);
        Task StopAsync(RpcNameServer nameserver);
       
        //--------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// NameServer通知ServiceNode，创建服务，并等待创建完毕
        /// </summary>
        /// <param name="from"></param>
        /// <param name="svc"></param>
        /// <param name="config"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        Task<bool> n2s_DispatchCreateServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc);
        /// <summary>
        /// NameServer通知ServiceNode，销毁服务，并等待销毁完毕
        /// </summary>
        /// <param name="from"></param>
        /// <param name="svc"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        Task<bool> n2s_DispatchDestoryServiceAsync(RemoteAddress from, NodeInfo node, ServiceInfo svc, string reason);

        /// <summary>
        /// NameServer通知所有ServiceNode某个服务要被卸载，用于删除ProxyCache
        /// </summary>
        /// <param name="addr"></param>
        Task n2s_BroadcastRemoteDisposing(RemoteAddress addr);
        /// <summary>
        /// NameServer通知所有ServiceNode某个服务卸载完毕，用于删除Proxy监听
        /// </summary>
        /// <param name="addr"></param>
        Task n2s_BroadcastRemoteDestoryed(RemoteAddress addr);

        /// <summary>
        /// 集群广播系统消息
        /// </summary>
        /// <param name="notify"></param>
        Task n2s_AppBroadcastMessage(BinaryMessage notify);
        /// <summary>
        /// 集群广播系统消息
        /// </summary>
        /// <param name="notify"></param>
        /// <returns></returns>
        Task<string> n2s_AppBroadcastCommandAsync(string notify);

        //--------------------------------------------------------------------------------------------------------------------------

        event HandleRegistNodeAsync s2n_HandleRegistNodeAsync;
        event HandleUnregistNodeAsync s2n_HandleUnregistNodeAsync;
        event HandleUpdateNodeState s2n_HandleUpdateNodeState;
        event HandleGetOrCreateRemoteServiceAsync s2n_HandleGetOrCreateRemoteServiceAsync;
        event HandleDestoryRemoteServiceAsync s2n_HandleDestoryRemoteServiceAsync;
        event HandleGetServiceCountAsync s2n_HandleGetServiceCountAsync;
        event HandleGetRemoteServicesAsync s2n_HandleGetRemoteServicesAsync;
        event HandleGetRemoteServicesWithAddressPatternAsync s2n_HandleGetRemoteServicesWithAddressPatternAsync;
        event HandleGetRemoteServicesWithInfoLinqAsync s2n_HandleGetRemoteServicesWithInfoLinqAsync;
        event HandleGetStaticServicesAsync s2n_HandleGetStaticServicesAsync;
        event HandleGetStaticNodesAsync s2n_HandleGetStaticNodesAsync;
        event HandleServiceBroadcastMessage s2n_HandleServiceBroadcastMessage;
        event HandleAppBroadcastMessage s2n_HandleAppBroadcastMessage;
        event HandleAppBroadcastCommandAsync s2n_HandleAppBroadcastCommandAsync;

        public delegate Task<NodeInfo> HandleRegistNodeAsync(ServiceNodeStartInfo start);
        public delegate Task<NodeInfo> HandleUnregistNodeAsync(string nodeName);
        public delegate Task<NodeInfo[]> HandleGetStaticNodesAsync();
        public delegate Task HandleUpdateNodeState(ServiceNodeStateInfo state);

        public delegate Task<ServiceInfo> HandleGetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config);
        public delegate Task<ServiceInfo> HandleDestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason);
        public delegate Task<int> HandleGetServiceCountAsync(string serviceNode, string serviceType);
        public delegate Task<ServiceInfo[]> HandleGetRemoteServicesAsync(ICollection<string> paths);
        public delegate Task<ServiceInfo[]> HandleGetRemoteServicesWithAddressPatternAsync(string pattern);
        public delegate Task<ServiceInfo[]> HandleGetRemoteServicesWithInfoLinqAsync(string where, string orderBy);
        public delegate Task<ServiceInfo[]> HandleGetStaticServicesAsync();
        public delegate Task HandleServiceBroadcastMessage(RemoteAddress from, BinaryMessage notify);
        public delegate Task HandleAppBroadcastMessage(BinaryMessage notify);
        public delegate Task<string> HandleAppBroadcastCommandAsync(string notify);

        //--------------------------------------------------------------------------------------------------------------------------
    }

}
