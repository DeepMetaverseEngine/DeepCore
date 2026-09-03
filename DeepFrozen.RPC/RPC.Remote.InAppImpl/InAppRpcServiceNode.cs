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
using System.Security.Cryptography;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.ServiceNode.IRpcServiceNodeAdapter;

namespace DeepFrozen.RPC.Remote.InAppImpl
{


    internal class InAppRpcServiceNode : IRpcServiceNodeAdapter
    {
        public static InAppRpcServiceNode Instance { get; private set; }
        public static InAppNameServerNode NameProxy { get => InAppNameServerNode.Instance; }
        public InAppRpcServiceNode() { Instance = this; }
        //----------------------------------------------------------------------------------------------------------------------
        public void Attach(RpcServiceNode server) { }
        public Task<bool> StartAsync(RpcServiceNode server)
        {
            return Task.FromResult(true);
        }
        public Task<int> ShutdownAsync(RpcServiceNode server)
        {
            return Task.FromResult(0);
        }
        public Task<bool> StopAsync(RpcServiceNode server)
        {
            return Task.FromResult(true);
        }
        //----------------------------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------
        #region IRpcServiceNodeAdapter
        async Task<bool> IRpcServiceNodeAdapter.node2name_RegistNodeAsync(ServiceNodeStartInfo start)
        {
            return await NameProxy.s2n_RegistNodeAsync(start) != null;
        }
        async Task<bool> IRpcServiceNodeAdapter.node2name_UnregistNodeAsync(string nodeName)
        {
            return await NameProxy.s2n_UnregistNodeAsync(nodeName) != null;
        }
        void IRpcServiceNodeAdapter.node2name_UpdateNodeState(ServiceNodeStateInfo state)
        {
            NameProxy.s2n_UpdateNodeState(state);
        }
        async Task<RemoteProxyInfo> IRpcServiceNodeAdapter.s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config)
        {
            var name_svc = await NameProxy.s2n_GetOrCreateRemoteServiceAsync(op, from, path, config);
            if (name_svc != null)
            {
                return name_svc.info;
            }
            return null;
        }
        async Task<bool> IRpcServiceNodeAdapter.s2n_DestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            var name_svc = await NameProxy.s2n_DestoryRemoteServiceAsync(from, path, reason);
            if (name_svc != null)
            {
                return true;
            }
            return false;
        }
        Task<int> IRpcServiceNodeAdapter.s2n_GetServiceCountAsync(string serviceNode, string serviceType)
        {
            return NameProxy.s2n_GetServiceCountAsync(serviceNode, serviceType);
        }
        async Task<RemoteProxyInfo[]> IRpcServiceNodeAdapter.s2n_GetRemoteServicesAsync(ICollection<string> servicesName)
        {
            var array = await NameProxy.s2n_GetRemoteServicesAsync(servicesName);
            if (array != null)
            {
                return Array.ConvertAll(array, svc => svc.info);
            }
            return null;
        }
        async Task<RemoteProxyInfo[]> IRpcServiceNodeAdapter.s2n_GetRemoteServicesWithAddressPatternAsync(string pattern)
        {
            var array = await NameProxy.s2n_GetRemoteServicesWithAddressPatternAsync(pattern);
            if (array != null)
            {
                return Array.ConvertAll(array, svc => svc.info);
            }
            return null;
        }
        async Task<RemoteProxyInfo[]> IRpcServiceNodeAdapter.s2n_GetRemoteServicesWithInfoLinqAsync(string where, string orderBy)
        {
            var array = await NameProxy.s2n_GetRemoteServicesWithInfoLinqAsync(where, orderBy);
            if (array != null)
            {
                return Array.ConvertAll(array, svc => svc.info);
            }
            return null;
        }
        async Task<RemoteProxyInfo[]> IRpcServiceNodeAdapter.s2n_GetStaticServicesAsync()
        {
            var array = await NameProxy.s2n_GetStaticServicesAsync();
            if (array != null)
            {
                return Array.ConvertAll(array, svc => svc.info);
            }
            return null;
        }
        async Task<ServiceNodeStartInfo[]> IRpcServiceNodeAdapter.s2n_GetStaticNodesInfoAsync()
        {
            var array = await NameProxy.s2n_GetStaticNodesAsync();
            if (array != null)
            {
                return Array.ConvertAll(array, svc => svc.token);
            }
            return null;
        }
        async Task<string> IRpcServiceNodeAdapter.s2n_BroadcastAppCommandAsync(string notify)
        {
            return await NameProxy.s2n_AppBroadcastCommandAsync(notify);
        }
        void IRpcServiceNodeAdapter.s2n_BroadcastAppMessage(BinaryMessage notify)
        {
            NameProxy.s2n_AppBroadcastMessage(notify);
        }
        void IRpcServiceNodeAdapter.s2n_BroadcastServiceMessage(RemoteAddress from, BinaryMessage notify)
        {
            NameProxy.s2n_ServiceBroadcastMessage(from, notify);
        }
        //----------------------------------------------------------------------------------
        void IRpcServiceNodeAdapter.s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback)
        {
            this.r2s_RemoteRpcRequest(from, to, msg, callback);
        }
        void IRpcServiceNodeAdapter.s2r_RpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnVoid callback)
        {
            this.r2s_RemoteRpcRequest(from, to, msg, (rsp, err) => callback());
        }
        void IRpcServiceNodeAdapter.s2r_RpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg)
        {
            this.r2s_RemoteRpcNotify(from, to, msg);
        }
        void IRpcServiceNodeAdapter.s2r_RpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg)
        {
            this.r2s_RemoteRpcBatchNotify(from, to, msg);
        }
        void IRpcServiceNodeAdapter.s2r_RpcNotifyWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg)
        {
            this.r2s_RemoteRpcNotifyWithType(from, serviceType, msg);
        }
        void IRpcServiceNodeAdapter.s2r_RpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        {
            this.r2s_RemoteRpcWormhole(from, to, msg, srcIsBin);
        }
        Task<BinaryMessage> IRpcServiceNodeAdapter.s2r_RpcWormholeAsync(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        {
            return this.r2s_RemoteRpcWormholeAsync(from, to, msg, srcIsBin);
        }
        void IRpcServiceNodeAdapter.s2r_RpcWormholeWithType(RemoteAddress from, string serviceNode, string serviceType, BinaryMessage msg, bool srcIsBin)
        {
            this.r2s_RemoteRpcWormholeWithType(from, serviceType, msg, srcIsBin);
        }

        #endregion
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
        public event HandleRemoteRpcWormholeAsync r2s_HandleRemoteRpcWormholeAsync;
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
        public Task<BinaryMessage> r2s_RemoteRpcWormholeAsync(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin) => r2s_HandleRemoteRpcWormholeAsync(from, to, msg, srcIsBin);
        public void r2s_RemoteRpcWormholeWithType(RemoteAddress from, string serviceType, BinaryMessage msg, bool srcIsBin) => r2s_HandleRemoteRpcWormholeWithType(from, serviceType, msg, srcIsBin);

        #endregion
        //----------------------------------------------------------------------------------


    }
}
