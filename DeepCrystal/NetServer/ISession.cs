using DeepCore.IO;
using DeepCore.Net;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DeepCrystal.NetServer
{
    /// <summary>
    /// 描述一个链接
    /// </summary>

    [Reflectible]
    public interface ISession : INetSession
    {
        string ID { get; }
        EndPoint RemoteAddress { get; }

        DateTime LastReceivedTimeUTC { get; }
        object UserTag { get; set; }

        void Disconnect(string reason);
        void Send(ISerializable message);
        void Send(BinaryMessage message);
        void SendResponse(ISerializable response, uint sendID);
        void SendResponse(BinaryMessage response, uint sendID);

        Task<bool> DisconnectAsync(string reason);
        Task<bool> SendAsync(ISerializable message);
        Task<bool> SendAsync(BinaryMessage message);
        Task<bool> SendResponseAsync(ISerializable response, uint sendID);
        Task<bool> SendResponseAsync(BinaryMessage response, uint sendID);

        Task<T> SendRequestAsync<T>(ISerializable request) where T : ISerializable;
        Task<BinaryMessage> SendRequestAsync(BinaryMessage request);

        IMessageHandler HandleMessage<T>(int route, Action<T, uint> action) where T : ISerializable;
        IMessageHandler HandleBinary(int route, Action<BinaryMessage, uint> action);

        void AppendDataFilter(ISessionDataFilter filter);

        event SessionValidateAsyncHandler OnValidateAsync;
        event SessionClosedHandler OnClosed;
        event SessionErrorHandler OnError;
        event SessionReceivedMessageHandler OnReceivedMessage;
        event SessionReceivedBinaryHandler OnReceivedBinary;
        event SessionReceivedRequestMessageHandler OnRequestMessageAsync;
        event SessionReceivedRequestBinaryHandler OnRequestBinaryAsync;
        event SessionSentHandler OnSent;

    }

    public interface IMessageHandler
    {
        void Cancel();
    }
    public interface ISessionDataFilter
    {
        ArraySegment<byte> Receiving(ISession session, ref EndPoint endpoint, in ArraySegment<byte> data);
    }
    //----------------------------------------------------------------------------------------------------------------------
    public class ValidateResult
    {
        public bool IsValidate;
        public ISerializable Token;
        public ValidateResult(bool isValidate, ISerializable token)
        {
            IsValidate = isValidate;
            Token = token;
        }
        public ValidateResult() { }
    }
    /// <summary>
    /// 验证Session合法性
    /// </summary>
    public delegate Task<ValidateResult> SessionValidateAsyncHandler(ISession session, ISerializable user);
    public delegate Task<ISerializable> SessionReceivedRequestMessageHandler(ISession session, ISerializable message);
    public delegate Task<BinaryMessage> SessionReceivedRequestBinaryHandler(ISession session, BinaryMessage message);

    public delegate void SessionClosedHandler(ISession session, string reason);
    public delegate void SessionErrorHandler(ISession session, Exception err);
    public delegate void SessionReceivedMessageHandler(ISession session, ISerializable message, uint sendID);
    public delegate void SessionReceivedBinaryHandler(ISession session, BinaryMessage message, uint sendID);
    public delegate void SessionSentHandler(ISession session, object msg);


    //----------------------------------------------------------------------------------------------------------------------
}
