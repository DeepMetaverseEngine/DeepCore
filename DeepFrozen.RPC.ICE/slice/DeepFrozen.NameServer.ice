#pragma once

#include <Ice/Identity.ice>
#include "DeepFrozen.ice"

module DeepFrozenIceImpl
{
    interface IRpcNameServerAdapter
    {
        ["amd"] bool              node_RegistNode(NodeStartInfo start) throws RpcException;
        ["amd"] bool              node_UnregistNode(string nodeName) throws RpcException;
        ["ami"] void              node_UpdateNodeState(NodeStateInfo state);

        ["amd"] ServiceProxyInfo  svc_GetOrCreateRemoteService(GetServiceOperation op, RpcAddress from, RpcAddress path, StringMap config) throws RpcException;
        ["amd"] bool              svc_DestoryRemoteService(RpcAddress from, RpcAddress path, string reason) throws RpcException;

		["amd"] int               svc_GetServiceCount(string serviceNode, string serviceType) throws RpcException;
        ["amd"] ServiceProxyArray svc_GetStaticServices() throws RpcException;
        ["amd"] ServiceProxyArray svc_GetRemoteServices(StringArray serviceNames) throws RpcException;
        ["amd"] ServiceProxyArray svc_GetRemoteServicesWithPattern(string pattern) throws RpcException;
        ["amd"] ServiceProxyArray svc_GetRemoteServicesWithLinq(string where, string orderBy)throws RpcException;
		["amd"] NodeInfoArray     svc_GetStaticNodesInfo() throws RpcException;

		["ami"] void              svc_Broadcast(RpcAddress from, BinaryMessage msg);
		["ami"] void              svc_BroadcastAppMessage(BinaryMessage msg);
		["amd"] string            svc_AppCommand(string msg);

    }

    
    interface IRpcNameServerConsole
    {
        ["amd"] string DoCommand(string cmd) throws RpcException;
        ["amd"] string DoStart() throws RpcException;
        ["amd"] string DoClose() throws RpcException;
        ["amd"] string DoStat() throws RpcException;
		["amd","ami"] long Ping(long time);
    }

};
