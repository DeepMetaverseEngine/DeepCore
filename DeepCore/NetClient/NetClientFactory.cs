using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore.NetClient
{
    //---------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class NetClientFactory
    {
        public static NetClientFactory Instance { get; private set; }
        public NetClientFactory() { Instance = this; }
        //---------------------------------------------------------------------------------------------------------------------
        public abstract IClientAdapter CreateAdapter(INetClient client);
        //---------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------

        private static HashMap<string, string> addressMapping = new HashMap<string, string>();
        public static bool EnableAddressMapping { get; set; }
        public static void SetAddressMapping(IDictionary<string, string> mapping)
        {
            addressMapping = new HashMap<string, string>();
            addressMapping.AddAll(mapping);
        }
        public static bool TryAddressMapping(string address, out string addressOut)
        {
            addressOut = address;
            if (addressMapping != null)
            {
                if (addressMapping.TryGetValue(address, out var _addressOut))
                {
                    addressOut = _addressOut;
                    return true;
                }
            }
            return false;
        }
        //---------------------------------------------------------------------------------------------------------------------
    }


    //-----------------------------------------------------------------------------------------------------------------
    public interface IClientAdapter : IDisposable
    {
        bool IsConnected { get; }
        bool IsHandshake { get; }
        long TotalRecvBytes { get; }
        long TotalSentBytes { get; }
        int Ping { get; }
        DateTime ConnectTime { get; }
        bool Connect(string address, int timeout, ISerializable user, Action<Exception, ISerializable> callback);
        Task<ISerializable> ConnectAsync(string address, int timeout, ISerializable user);
        bool Disconnect(Action callback);
        bool Send(ISerializable msg, MessageType msgType, uint send_id);
        bool Send(BinaryMessage msg, MessageType msgType, uint send_id);
        void Update();

        event Action<IRecvMessage> OnReceivedMessage;
        event Action<ISendMessage> OnSentMessage;
        event Action<Exception> OnError;
        event Action<CloseReason, string> OnDisconnected;
        event Action<SystemHandshakeAck, ISerializable> OnConnected;
    }

    //-----------------------------------------------------------------------------------------------------------------


    public interface IPushHandler : IDisposable
    {
        INetClient Client { get; }
        bool IsBinary { get; }
        bool IsClear { get; }
        bool IsRecursion { get; }
        int Route { get; }
        void Clear();
    }

    public interface IRpcRequestHandler : IDisposable
    {
        INetClient Client { get; }
        bool IsBinary { get; }
        bool IsClear { get; }
        void Clear();
    }
    public struct TResponse<T> where T : ISerializable
    {
        public T rsp;
        public Exception err;
        public object state;
        public bool IsSuccess { get { return err == null && rsp is Response r && r.IsSuccess; } }
        public override string ToString()
        {
            return err != null ? err.Message : $"{rsp}";
        }
    }
    public enum CloseReason : int
    {
        Unknown = 0,

        ClientClose = 1,

        Disconnect = 2,

        KickByServer = 3,

        Error = 4,

        TimeOut = 5,

    }
    //-----------------------------------------------------------------------------------------------------------------


    public class NetException : Exception
    {
        public bool Timeout
        {
            get; internal set;
        }
        public NetException(string message)
            : base(message)
        {

        }
        public NetException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
