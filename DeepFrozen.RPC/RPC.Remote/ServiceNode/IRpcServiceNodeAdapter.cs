using DeepCore;
using DeepCore.IO;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    //----------------------------------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------------------------------
    public interface IRpcServiceNodeAdapter
    {
        void Attach(RpcServiceNode server);
        Task<bool> StartAsync(RpcServiceNode server);
        Task<bool> StopAsync(RpcServiceNode server);
        Task<int> ShutdownAsync(RpcServiceNode server);

        //---------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// ServiceNode -> NameServer
        /// 请求将此Node节点注册到NameServer
        /// </summary>
        Task<bool> node2name_RegistNodeAsync(ServiceNodeStartInfo start);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 请求将此Node节点从NameServer移除
        /// </summary>
        Task<bool> node2name_UnregistNodeAsync(string nodeName);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 请求将此Node节点状态更新到NameServer
        /// </summary>
        void node2name_UpdateNodeState(ServiceNodeStateInfo state);
        //---------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// ServiceNode -> NameServer
        /// 请求获取或创建远端服务
        /// </summary>
        Task<RemoteProxyInfo> s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 请求NameServer销毁服务
        /// </summary>
        Task<bool> s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取服务数量
        /// </summary>
        Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取远端服务组
        /// </summary>
        Task<RemoteProxyInfo[]> s2n_GetRemoteServicesAsync(ICollection<string> servicesName);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取远端服务组
        /// </summary>
        Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithAddressPatternAsync(string pattern);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取远端服务组
        /// </summary>
        Task<RemoteProxyInfo[]> s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取所有静态远端服务
        /// </summary>
        Task<RemoteProxyInfo[]> s2n_GetStaticServicesAsync();
        /// <summary>
        /// ServiceNode -> NameServer
        /// 获取所有静态节点
        /// </summary>
        /// <returns></returns>
        Task<ServiceNodeStartInfo[]> s2n_GetStaticNodesInfoAsync();
        /// <summary>
        /// ServiceNode -> NameServer
        /// 向域内所有进程广播消息
        /// </summary>
        Task<string> s2n_BroadcastAppCommandAsync(string notify);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 向域内所有进程广播消息
        /// </summary>
        void s2n_BroadcastAppMessage(BinaryMessage notify);
        /// <summary>
        /// ServiceNode -> NameServer
        /// 向域内所有服务广播消息
        /// </summary>
        void s2n_BroadcastServiceMessage(RemoteAddress from, BinaryMessage notify);
        //---------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端Rpc
        /// </summary>
        void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback);
        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端Rpc
        /// </summary>
        void s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnVoid callback);
        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端Rpc
        /// </summary>
        void s2r_RpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg);
        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端Rpc
        /// </summary>
        void s2r_RpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg);
        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端Rpc
        /// </summary>
        void s2r_RpcNotifyWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg);

        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端虫洞
        /// </summary>
        void s2r_RpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin);

        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端虫洞
        /// </summary>
        Task<BinaryMessage> s2r_RpcWormholeAsync(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin);

        /// <summary>
        /// ServiceNode -> ServiceNode
        /// 请求远端虫洞
        /// </summary>
        void s2r_RpcWormholeWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg, bool srcIsBin);
        //---------------------------------------------------------------------------------------------------------------------------------------

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

        public delegate Task<bool> HandleCreateLocalServiceAsync(RemoteAddress from, RemoteAddress addr, Dictionary<string, string> config, bool isStatic);
        public delegate Task<bool> HandleDestoryLocalServiceAsync(RemoteAddress from, RemoteAddress addr, string reason);
        public delegate void HandleRemoteDisposing(RemoteAddress addr);
        public delegate void HandleRemoteDestoryed(RemoteAddress addr);
        public delegate void HandleAppMessage(BinaryMessage notify);
        public delegate Task<string> HandleAppCommandAsync(string notify);
        public delegate void HandleRemoteRpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback);
        public delegate void HandleRemoteRpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg);
        public delegate void HandleRemoteRpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg);
        public delegate void HandleRemoteRpcNotifyWithType(RemoteAddress from, string serviceType, BinaryMessage msg);
        public delegate void HandleRemoteRpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin);
        public delegate Task<BinaryMessage> HandleRemoteRpcWormholeAsync(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin);
        public delegate void HandleRemoteRpcWormholeWithType(RemoteAddress from, string serviceType, BinaryMessage msg, bool srcIsBin);

    }

}
