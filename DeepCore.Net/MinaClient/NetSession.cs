using DeepCore.Net;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DeepCore.MinaClient
{
    public delegate void OnSessionOpenedHandler(IMinaClientSession session);
    public delegate void OnSessionClosedHandler(IMinaClientSession session);
    public delegate void OnMessageReceivedHandler(IMinaClientSession session, Object data);
    public delegate void OnMessageSentHandler(IMinaClientSession session, Object data);
    public delegate void OnErrorHandler(IMinaClientSession session, Exception err);
    public delegate void OnUpdateHandler(IMinaClientSession session);

    public interface IMinaClientSession : INetSession, IDisposable
    {
        string URL { get; }
        IPEndPoint RemoteAddress { get; }
        long TotalSentPackages { get; }
        long TotalRecvPackages { get; }

        bool Open(string url, INetPackageCodec codec, IMinaClientSessionListener listener);

        bool Close();

        /// <summary>
        /// 发送一个消息，该方法将立即返回。
        /// </summary>
        /// <param name="data"></param>
        void Send(Object data);
        
        event OnSessionOpenedHandler OnSessionOpened;
        event OnSessionClosedHandler OnSessionClosed;
        event OnMessageReceivedHandler OnMessageReceived;
        event OnMessageSentHandler OnMessageSent;
        event OnErrorHandler OnError;

    }
}
