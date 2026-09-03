#pragma once

#include <Ice/Identity.ice>
#include "DeepFrozen.ice"

module DeepFrozenIceImpl
{
    interface IRpcServiceAdapter
    {
        ["amd"] bool          n2s_CreateLocalService(RpcAddress from, RpcAddress addr, StringMap config, bool isStatic) throws RpcException;
        ["amd"] bool          n2s_DestoryLocalService(RpcAddress from, RpcAddress addr, string reason) throws RpcException;
        ["ami"] void          n2s_AppMessageNotify(BinaryMessage notify);
		["amd"] string        n2s_AppCommand(string notify);

		["ami"] void          r2s_RpcRequest(string fromNodeEndPoint, RpcAddress from, RpcAddress addr, int sendID, BinaryMessage req);
		["ami"] void          s2r_RpcResponse(int sendID, BinaryMessage rsp, RpcExceptionMeta err);
		["ami"] void          r2s_RpcNotify(RpcAddress from, RpcAddress addr, BinaryMessage msg);
		["ami"] void          r2s_RpcBatchNotify(RpcAddress from, RpcAddress addr, BinaryMessageArray msg);
		["ami"] void          r2s_RpcNotifyWithType(RpcAddress from, string serviceType, BinaryMessage msg);
		["ami"] void          r2s_RpcWormhole(RpcAddress from, RpcAddress addr, BinaryMessage msg, bool srcIsBin);
		["amd"] BinaryMessage r2s_RpcWormholeReturn(RpcAddress from, RpcAddress addr, BinaryMessage msg, bool srcIsBin);
		["ami"] void          r2s_RpcWormholeWithType(RpcAddress from, string serviceType, BinaryMessage msg, bool srcIsBin);
		
		["ami"] void          r2s_RemoteServiceDisposing(RpcAddress addr);
		["ami"] void          r2s_RemoteServiceDestoryed(RpcAddress addr);
    }

};
