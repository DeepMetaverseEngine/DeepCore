using DeepCore;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.ORM;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    //--------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 远端服务
    /// </summary>

    [Reflectible]
    public interface IRemoteService : IRemoteServiceInfo
    {
        /// <summary>
        /// 调用RPC（带返回值）
        /// </summary>
        void Call<RSP>(ISerializable req, OnRpcReturn<RSP> callback) where RSP : ISerializable;
        /// <summary>
        /// 调用RPC（带返回值）
        /// </summary>
        void Call(BinaryMessage req, OnRpcReturnBinary callback);
        /// <summary>
        /// 调用RPC（无返回值）
        /// </summary>
        void Invoke(ISerializable msg);
        /// <summary>
        /// 调用RPC（无返回值）
        /// </summary>
        void Invoke(BinaryMessage msg);

        /// <summary>
        /// 一次批量调用RPC（无返回值）
        /// </summary>
        void BatchInvoke(ICollection<ISerializable> batch);
        /// <summary>
        /// 一次批量调用RPC（无返回值）
        /// </summary>
        void BatchInvoke(ICollection<BinaryMessage> batch);

        /// <summary>
        /// 跨进程直接将消息传送过去，线程不安全。
        /// </summary>
        void WormholeTransport(object message);
        /// <summary>
        /// 跨进程直接将消息传送过去，线程不安全。
        /// </summary>
        Task<object> WormholeTransportAsync(object message);

        /// <summary>
        /// 调用RPC（带返回值）
        /// </summary>
        Task<RSP> CallAsync<RSP>(ISerializable req) where RSP : ISerializable;
        /// <summary>
        /// 调用RPC（带返回值）
        /// </summary>
        Task<BinaryMessage> CallAsync(BinaryMessage req);

        /// <summary>
        /// 调用RPC（无返回值，需等待对方RPC执行完毕）
        /// </summary>
        Task InvokeAsync(ISerializable msg);
        /// <summary>
        /// 调用RPC（无返回值，需等待对方RPC执行完毕）
        /// </summary>
        Task InvokeAsync(BinaryMessage msg);

        /// <summary>
        /// 请求销毁服务
        /// </summary>
        Task<bool> ShutdownAsync(string reason);
        /// <summary>
        /// 监听代理被删除
        /// </summary>
        void ListenOnServiceDestroyed(Action<RemoteAddress> action);
    }

    public interface IRemoteServiceInfo
    {
        /// <summary>
        /// 服务名字
        /// </summary>
        string ServiceName { get; }
        /// <summary>
        /// 服务全地址
        /// </summary>
        RemoteAddress Address { get; }
        /// <summary>
        /// 创建服务的参数
        /// </summary>
        Properties Config { get; }
        /// <summary>
        /// 服务启动时间
        /// </summary>
        DateTime StartTimeUTC { get; }
        /// <summary>
        /// 是否为静态服务，一般为默认启动服务
        /// </summary>
        bool IsStatic { get; }
    }

    public interface IRemoteNodeInfo
    {
        /// <summary>
        /// Node节点名
        /// </summary>
        string NodeName { get; }
        /// <summary>
        /// Node节点地址
        /// </summary>
        string EndPoint { get; }
        /// <summary>
        /// 可创建服务类型列表
        /// </summary>
        List<string> AcceptServiceType { get; }
    }
    //--------------------------------------------------------------------------------------------------------------------------------------

    //--------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 远端服务地址
    /// </summary>
    public struct RemoteAddress
    {
        public readonly static RemoteAddress NULL = new RemoteAddress();

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; private set; }
        /// <summary>
        /// 服务具体进程地址
        /// </summary>
        public string ServiceNode { get; private set; }
        /// <summary>
        /// 服务类型名称（一般用于Create）
        /// </summary>
        public string ServiceType { get; private set; }
        /// <summary>
        /// 全局唯一全名
        /// </summary>
        public string FullPath
        {
            get
            {
                if (ServiceNode != null)
                    return string.Format("{0}@{1}", ServiceName, ServiceNode);
                else
                    return ServiceName;
            }
        }
        public bool IsNull
        {
            get
            {
                return string.IsNullOrEmpty(ServiceName);
            }
        }
        public bool NotNull
        {
            get
            {
                return !string.IsNullOrEmpty(ServiceName);
            }
        }

        public RemoteAddress(RemoteAddress addr)
        {
            this.ServiceName = addr.ServiceName;
            this.ServiceNode = addr.ServiceNode;
            this.ServiceType = addr.ServiceType;
        }
        public RemoteAddress(Tuple<string, string, string> addr)
        {
            this.ServiceName = addr.Item1;
            this.ServiceNode = addr.Item2;
            this.ServiceType = addr.Item3;
        }
        public RemoteAddress(string svcName)
        {
            this.ServiceName = svcName;
            this.ServiceNode = null;
            this.ServiceType = null;
        }
        public RemoteAddress(string svcName, string svcNode)
        {
            this.ServiceName = svcName;
            this.ServiceNode = svcNode;
            this.ServiceType = null;
        }
        public RemoteAddress(string svcName, string svcNode, string svcType)
        {
            this.ServiceName = svcName;
            this.ServiceNode = svcNode;
            this.ServiceType = svcType;
        }
        public static RemoteAddress FromName(string svcName)
        {
            return new RemoteAddress(svcName);
        }
        public static RemoteAddress FromNameType(string svcName, string svcType)
        {
            return new RemoteAddress(svcName, null, svcType);
        }
        public static RemoteAddress FromNameNode(string svcName, string svcNode = null)
        {
            return new RemoteAddress(svcName, svcNode, null);
        }
        public override string ToString()
        {
            using (var sb = new StringWriter())
            {
                sb.Write(ServiceName);
                if (ServiceNode != null)
                {
                    sb.Write(split_char[0]);
                    sb.Write(ServiceNode);
                }
                if (ServiceType != null)
                {
                    sb.Write(split_char[0]);
                    sb.Write(ServiceType);
                }
                return sb.ToString();
            }
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this.ServiceName == ((RemoteAddress)obj).ServiceName;
        }
        public override int GetHashCode()
        {
            return ServiceName.GetHashCode();
        }

        private static char[] split_char = new char[] { '@' };
        public static RemoteAddress Parse(string text)
        {
            if (text == null) return NULL;
            var kv = text.Split(split_char, 3);
            if (kv.Length >= 3)
            {
                return new RemoteAddress(kv[0], kv[1], kv[2]);
            }
            if (kv.Length >= 2)
            {
                return new RemoteAddress(kv[0], kv[1], null);
            }
            if (kv.Length >= 1)
            {
                return new RemoteAddress(kv[0], null, null);
            }
            return NULL;
        }
        public static implicit operator RemoteAddress(string name)
        {
            return new RemoteAddress(name);
        }
        public static bool operator ==(RemoteAddress value1, RemoteAddress value2)
        {
            return value1.ServiceName == value2.ServiceName;
        }
        public static bool operator !=(RemoteAddress value1, RemoteAddress value2)
        {
            return value1.ServiceName != value2.ServiceName;
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

    //--------------------------------------------------------------------------------------------------------------------------------------

    public delegate void OnRpcReturn<in T>(T message, Exception error = null) where T : ISerializable;
    public delegate void OnRpcReturnBinary(BinaryMessage message, Exception error = null);
    public delegate void OnRpcReturnVoid(Exception error = null);

    //--------------------------------------------------------------------------------------------------------------------------------------
}