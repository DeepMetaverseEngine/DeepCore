using DeepCore;
using DeepFrozenIceImpl;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepFrozen.ICE
{


    public static class Utils
    {

        public static DeepCore.IO.BinaryMessage ToBinary(this BinaryMessage value)
        {
            if (value != null)
                return DeepCore.IO.BinaryMessage.FromSegment(value.route, new ArraySegment<byte>(value.bytes));
            else
                return DeepCore.IO.BinaryMessage.NULL;
        }
        public static BinaryMessage ToIceBinary(this DeepCore.IO.BinaryMessage value)
        {
            var ret = new BinaryMessage(value.Route, value.ToArray());
            return ret;
        }
        public static DeepFrozenIceImpl.BinaryMessage[] ToIceBinaryArray(this ICollection<DeepCore.IO.BinaryMessage> value)
        {
            if (value != null)
            {
                int i = 0;
                var ret = new DeepFrozenIceImpl.BinaryMessage[value.Count];
                foreach (var bin in value)
                {
                    ret[i] = bin.ToIceBinary();
                    i++;
                }
                return ret;
            }
            else
                return null;
        }
        public static RpcAddress ToIceAddress(this DeepCrystal.RPC.RemoteAddress addr)
        {
            return new RpcAddress(addr.ServiceName, addr.ServiceNode, addr.ServiceType);
        }

        public static Exception ToException(this RpcExceptionMeta value)
        {
            if (value != null)
            {
                return new DeepFrozen.RPC.Remote.RpcException(value.RpcMessage, value.RpcStackTrace);//new System.Exception(value.RpcMessage, value.RpcStackTrace);
            }
            else
                return null;
        }

        public static DeepCrystal.RPC.RemoteAddress ToRemoteAddress(this RpcAddress value)
        {
            if (value != null)
                return new DeepCrystal.RPC.RemoteAddress(value.ServiceName, value.ServiceNode, value.ServiceType);
            else
                return DeepCrystal.RPC.RemoteAddress.NULL;
        }
        public static DeepFrozen.RPC.Remote.RemoteProxyInfo ToRemoteProxyInfo(this ServiceProxyInfo value)
        {
            if (value != null)
                return new DeepFrozen.RPC.Remote.RemoteProxyInfo()
                {
                    Address = new RPC.Remote.RemoteAddressInfo(value.Address.ToRemoteAddress()),
                    EndPoint = value.EndPoint,
                    Config = new Properties(value.Config),
                    StartTimeUTC = DateTime.FromBinary(value.StartTimeUTC),
                    IsStatic = value.IsStatic,
                };
            else
                return null;
        }
        public static DeepFrozen.RPC.Remote.ServiceNodeStartInfo ToServiceNodeStartInfo(this NodeStartInfo value)
        {
            if (value != null)
                return new DeepFrozen.RPC.Remote.ServiceNodeStartInfo()
                {
                    NodeName = value.NodeName,
                    EndPoint = value.EndPoint,
                    AcceptServiceType = new List<string>(value.AcceptServiceType),
                };
            else
                return null;
        }
        public static DeepFrozen.RPC.Remote.ServiceNodeStateInfo ToServiceNodeStateInfo(this NodeStateInfo value)
        {
            if (value != null)
                return new DeepFrozen.RPC.Remote.ServiceNodeStateInfo()
                {
                    NodeName = value.NodeName,
                    ServiceCount = value.ServiceCount,
                    CpuPercent = value.CpuPercent,
                    MemoryTotal = value.MemoryTotal,
                    MemoryUse = value.MemoryUse,
                    Info = value.Info,
                };
            else
                return null;
        }
    }
}
namespace DeepFrozenIceImpl
{

    public partial class RpcExceptionMeta
    {
        public static implicit operator RpcExceptionMeta(Exception value)
        {
            if (value != null)
                return new RpcExceptionMeta(value.Message, value.StackTrace);
            else
                return null;
        }
    }

    public partial class RpcAddress
    {
        public static implicit operator RpcAddress(DeepCrystal.RPC.RemoteAddress value)
        {
            if (value != null)
                return new RpcAddress(value.ServiceName, value.ServiceNode, value.ServiceType);
            else
                return null;
        }
    }

    public partial class ServiceProxyInfo
    {
        public static implicit operator ServiceProxyInfo(DeepFrozen.RPC.Remote.RemoteProxyInfo value)
        {
            if (value != null)
                return new ServiceProxyInfo(value.Address.ToAddress(), value.EndPoint, value.Config, value.StartTimeUTC.ToBinary(), value.IsStatic);
            else
                return null;
        }
    }

    public partial class NodeStartInfo
    {
        public static implicit operator NodeStartInfo(DeepFrozen.RPC.Remote.ServiceNodeStartInfo value)
        {
            if (value != null)
                return new NodeStartInfo(value.NodeName, value.EndPoint, value.AcceptServiceType.ToArray());
            else
                return null;
        }
    }

    public partial class NodeStateInfo
    {
        public static implicit operator NodeStateInfo(DeepFrozen.RPC.Remote.ServiceNodeStateInfo value)
        {
            if (value != null)
                return new NodeStateInfo(value.NodeName, value.ServiceCount, value.CpuPercent, value.MemoryUse, value.MemoryTotal, value.Info);
            else
                return null;
        }
    }

}
