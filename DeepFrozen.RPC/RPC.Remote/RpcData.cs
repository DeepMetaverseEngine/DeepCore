using DeepCore;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.ORM;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote.NameServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote
{
    public class RemoteAddressInfo : IStructMapping
    {
        public string ServiceName;
        public string ServiceNode;
        public string ServiceType;
        public RemoteAddressInfo(RemoteAddress addr)
        {
            this.ServiceName = addr.ServiceName;
            this.ServiceNode = addr.ServiceNode;
            this.ServiceType = addr.ServiceType;
        }
        public RemoteAddressInfo(string svcName, string svcNode, string svcType)
        {
            this.ServiceName = svcName;
            this.ServiceNode = svcNode;
            this.ServiceType = svcType;
        }
        public RemoteAddressInfo() { }
        public RemoteAddress ToAddress()
        {
            return new RemoteAddress(ServiceName, ServiceNode, ServiceType);
        }
        public RemoteAddressInfo Clone()
        {
            return new RemoteAddressInfo(ServiceName, ServiceNode, ServiceType);
        }
        public void ReadExternal(IInputStream input)
        {
            this.ServiceName = input.GetUTF();
            this.ServiceNode = input.GetUTF();
            this.ServiceType = input.GetUTF();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(this.ServiceName);
            output.PutUTF(this.ServiceNode);
            output.PutUTF(this.ServiceType);
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 描述远端服务
    /// </summary>
    public class RemoteProxyInfo : IRemoteServiceInfo, IStructMapping
    {
        public RemoteAddressInfo Address;
        public Properties Config;
        public string EndPoint;
        public bool IsStatic;
        public DateTime StartTimeUTC;

        string IRemoteServiceInfo.ServiceName => Address.ServiceName;
        RemoteAddress IRemoteServiceInfo.Address => Address.ToAddress();
        Properties IRemoteServiceInfo.Config => Config;
        DateTime IRemoteServiceInfo.StartTimeUTC => StartTimeUTC;
        bool IRemoteServiceInfo.IsStatic => IsStatic;
        public override string ToString()
        {
            return "Prx:" + Address;
        }

        public static void ReadExternal(RemoteProxyInfo info, IInputStream input)
        {
            info.Address = new RemoteAddressInfo();
            info.Address.ReadExternal(input);
            info.Config = input.Decode(new Properties(), Properties.ReadExternal);
            info.EndPoint = input.GetUTF();
            info.StartTimeUTC = input.GetDateTime();
            info.IsStatic = input.GetBool();
        }
        public static void WriteExternal(RemoteProxyInfo info, IOutputStream output)
        {
            info.Address.WriteExternal(output);
            output.Encode(info.Config, Properties.WriteExternal);
            output.PutUTF(info.EndPoint);
            output.PutDateTime(info.StartTimeUTC);
            output.PutBool(info.IsStatic);
        }
        public RemoteProxyInfo Clone()
        {
            var ret = new RemoteProxyInfo();
            ret.Address = this.Address.Clone();
            ret.Config = new Properties(this.Config);
            ret.EndPoint = this.EndPoint;
            ret.IsStatic = this.IsStatic;
            ret.StartTimeUTC = this.StartTimeUTC;
            return ret;
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Node启动相关信息
    /// </summary>
    public class ServiceNodeStartInfo : IRemoteNodeInfo, IStructMapping
    {
        /// <summary>
        /// Node节点名
        /// </summary>
        public string NodeName;
        /// <summary>
        /// Node节点地址
        /// </summary>
        public string EndPoint;
        /// <summary>
        /// 可创建服务类型列表
        /// </summary>
        public List<string> AcceptServiceType;

        string IRemoteNodeInfo.NodeName => this.NodeName;
        string IRemoteNodeInfo.EndPoint => this.EndPoint;
        List<string> IRemoteNodeInfo.AcceptServiceType => this.AcceptServiceType;

        public static void ReadExternal(ServiceNodeStartInfo info, IInputStream input)
        {
            info.NodeName = input.GetUTF();
            info.EndPoint = input.GetUTF();
            info.AcceptServiceType = input.GetUTFList();
        }
        public static void WriteExternal(ServiceNodeStartInfo info, IOutputStream output)
        {
            output.PutUTF(info.NodeName);
            output.PutUTF(info.EndPoint);
            output.PutUTFList(info.AcceptServiceType);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Node状态相关信息
    /// </summary>
    public class ServiceNodeStateInfo : IStructMapping
    {
        /// <summary>
        /// Node节点名
        /// </summary>
        public string NodeName;
        public int ServiceCount;
        public float CpuPercent;
        public long MemoryUse;
        public long MemoryTotal;
        public string Info;

        public static void ReadExternal(ServiceNodeStateInfo info, IInputStream input)
        {
            info.NodeName = input.GetUTF();
            info.ServiceCount = input.GetS32();
            info.CpuPercent = input.GetF32();
            info.MemoryUse = input.GetS64();
            info.MemoryTotal = input.GetS64();
            info.Info = input.GetUTF();
        }
        public static void WriteExternal(ServiceNodeStateInfo info, IOutputStream output)
        {
            output.PutUTF(info.NodeName);
            output.PutS32(info.ServiceCount);
            output.PutF32(info.CpuPercent);
            output.PutS64(info.MemoryUse);
            output.PutS64(info.MemoryTotal);
            output.PutUTF(info.Info);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------
    public enum GetServiceOperation : byte
    {
        GetOrCreate = 1,
        Create = 2,
        Get = 3,
    }
    //----------------------------------------------------------------------------------------------------------------------------
    public enum ServiceStatus
    {
        NA,
        Starting,
        Started,
        Stopping,
        Stopped,
    }

    //----------------------------------------------------------------------------------------------------------------------------

    [PersistType]
    public class NodeInfo : IObjectMapping
    {
        [PersistField] public ServiceNodeStartInfo token;
        [PersistField] public ServiceNodeStateInfo state;
        [PersistField] public int serviceCount = 0;
        public NodeInfo(ServiceNodeStartInfo req)
        {
            this.token = req;
        }
        public NodeInfo() { }
        public string NodeName { get => token.NodeName; }
        public string EndPoint { get => token.EndPoint; }
        public int ServiceCount { get { return serviceCount; } }
        public ServiceNodeStateInfo StateInfo { get => state; }
        public ServiceNodeStartInfo ToToken()
        {
            var ret = new ServiceNodeStartInfo();
            ret.AcceptServiceType = new List<string>(token.AcceptServiceType);
            ret.EndPoint = token.EndPoint;
            ret.NodeName = token.NodeName;
            return ret;
        }
        public override string ToString()
        {
            return token.NodeName;
        }
        public bool AcceptType(string serviceType)
        {
            return token.AcceptServiceType.Contains(serviceType);
        }
        public void GetStatus(TextWriter output)
        {
            output.WriteLine("                  NodeName = " + NodeName);
            output.WriteLine("                  EndPoint = " + EndPoint);
            output.WriteLine("              ServiceCount = " + ServiceCount);
            output.WriteLine("         AcceptServiceType = " + CUtils.ListToString(token.AcceptServiceType, " "));
            if (state == null) return;
            output.WriteLine("                CpuPercent = " + state.CpuPercent);
            output.WriteLine("                 MemoryUse = " + CUtils.ToBytesSizeString(state.MemoryUse));
            output.WriteLine("               MemoryTotal = " + CUtils.ToBytesSizeString(state.MemoryTotal));
            output.WriteLine("           ServiceBoxCount = " + state.ServiceCount);
            output.Write(state.Info);
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------

    [PersistType]
    public class ServiceInfo : IObjectMapping, IRemoteServiceInfo
    {
        [PersistField] public RemoteAddressInfo creater;
        [PersistField] public RemoteProxyInfo info;
        [PersistField] public bool isStatic;
        [PersistField] public ServiceStatus status;
        [PersistField] public DateTime startTimeUTC;
        public ServiceInfo(NodeInfo node, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config, bool isStatic)
        {
            this.creater = new RemoteAddressInfo(from);
            this.isStatic = isStatic;
            this.info = new RemoteProxyInfo()
            {
                Address = new RemoteAddressInfo(path.ServiceName, node.NodeName, path.ServiceType),
                Config = new Properties(config),
                EndPoint = node.EndPoint,
                IsStatic = isStatic,
            };
            this.status = ServiceStatus.NA;
        }
        public ServiceInfo() { }
        public string ServiceName { get => info.Address.ServiceName; }
        public RemoteAddress Address { get => info.Address.ToAddress(); }
        public DateTime StartTimeUTC { get => startTimeUTC; }
        public ServiceStatus Status { get => status; }
        public bool IsStatic { get => isStatic; }
        public Properties Config => info.Config;
        public RemoteProxyInfo ToInfo() => info.Clone();
        public override string ToString()
        {
            return info.Address.ToString();
        }

    }

    //----------------------------------------------------------------------------------------------------------------------------
}
