using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.NetServer
{

    [Reflectible]
    public interface IServer : IDisposable
    {
        int SessionCount { get; }
        int ListenPort { get; }
        Task<bool> StartAsync();
        Task<bool> StopAsync(string reason);
        void Broadcast(ISerializable message);
        bool HasSession(ISession session);
        int GetSessions(IList<ISession> ret);
        ISession GetSessionByID(string sessionID);

        event SessionHandler OnCreateSession;   
        event SessionMessageFilter MessageFilter;
        event ServerErrorHandler OnServerError;
        event SessionConnectedHandler OnSessionConnected;
        event SessionDisconnectedHandler OnSessionDisconnected;
        event SessionValidateAsyncHandler OnSessionValidateAsync;
        event SessionReceivedMessageHandler OnSessionReceivedMessage;
        event SessionReceivedBinaryHandler OnSessionReceivedBinary;
        event SessionReceivedRequestMessageHandler OnSessionRequestMessageAsync;
        event SessionReceivedRequestBinaryHandler OnSessionRequestBinaryAsync;

    }


    public delegate void SessionHandler(ISession session);
    public delegate void ServerErrorHandler(IServer server, Exception err);
    public delegate void SessionConnectedHandler(ISession session);
    public delegate void SessionDisconnectedHandler(ISession session);

    /// <summary>
    /// 非主线程调度
    /// </summary>
    /// <param name="session"></param>
    /// <param name="message"></param>
    /// <returns>True If Block Message</returns>
    public delegate bool SessionMessageFilter(ISession session, IProtocol message);

}
