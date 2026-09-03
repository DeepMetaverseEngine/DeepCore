#pragma once

#include <Ice/Identity.ice>

module DeepFrozenIceImpl
{

	//==========================================================
	//异常定义
	//==========================================================
	exception RpcException
	{
		string RpcMessage;
		string RpcStackTrace;
	};
	
	class RpcExceptionMeta
	{
		string RpcMessage;
		string RpcStackTrace;
	};


	///==========================================================
	///类型定义
	///==========================================================

	sequence<byte>				ByteArray;
	sequence<string>			StringArray;
	dictionary<string, string>	StringMap;

	class BinaryMessage 
	{
		int       route;
		ByteArray bytes;
	};

	class RpcAddress
    {
        string ServiceName;
        string ServiceNode;
        string ServiceType;
    };

    class ServiceProxyInfo
    {
        RpcAddress Address;
		string     EndPoint;
        StringMap  Config;
        long       StartTimeUTC;
		bool       IsStatic;
    };

	class NodeProxyInfo
    {
        string      NodeName;
		string      EndPoint;
    };

    class NodeStartInfo
    {
        string      NodeName;
		string      EndPoint;
        StringArray AcceptServiceType;
    };

    class NodeStateInfo
    {
        string NodeName;
		int    ServiceCount;
		float  CpuPercent;
		long   MemoryUse;
		long   MemoryTotal;
		string Info;
    };

	enum GetServiceOperation 
	{      
		GetOrCreate = 1,
        Create = 2,
        Get = 3,
	};

	sequence<ServiceProxyInfo> ServiceProxyArray;

	sequence<NodeProxyInfo> NodeProxyArray;

	sequence<NodeStartInfo> NodeInfoArray;

	sequence<BinaryMessage> BinaryMessageArray;

	///==========================================================
};