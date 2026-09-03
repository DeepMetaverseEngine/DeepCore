using DeepCore.MinaClient;
using DeepCore.Net;
using DeepCore.Protocol;
using DeepCore.Reflection;
using DeepCrystal.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DeepCrystal.SharpMinaServer
{
    /// <summary>
    /// 
    /// </summary>

    [Reflectible]
    public interface IMinaServerFactory
    {
        IMinaServer CreateServer(ServerConfig config, INetPackageCodec codec);
    }


    /// <summary>
    /// 服务器监听器
    /// </summary>

    [Reflectible]
    public interface IMinaServerListener
    {
        /// <summary>
        /// 服务器初始化回调
        /// </summary>
        /// <param name="server"></param>
        void OnInit(IMinaServer server);

        /// <summary>
        /// 服务器关闭回调
        /// </summary>
        void OnDestory();

        /// <summary>
        /// 一个链接建立成功
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        IMinaSessionListener OnSessionConnected(IMinaSession session);

    }

    [Reflectible]
    public interface IMinaServer
    {
        /// <summary>
        /// 客户端连接套接字
        /// </summary>
        string ClientConnectString { get; }

        /// <summary>
        /// 获取编解码器
        /// </summary>
        INetPackageCodec PackageCodec { get; }

        /// <summary>
        /// 获取当前已连接数
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="port"></param>
        /// <param name="listener"></param>
        void Open(IMinaServerListener listener);

        /// <summary>
        /// 关闭服务器
        /// </summary>
        void Close();

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="message"></param>
        void Broadcast(object message);

        /// <summary>
        /// 服务器是否有此链接
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        bool HasSession(IMinaSession session);

        /// <summary>
        /// 根据 Session ID 获取链接
        /// </summary>
        /// <param name="sessionID"></param>
        /// <returns></returns>
        IMinaSession GetSessionByID(string sessionID);

        /// <summary>
        /// 获取所有链接
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMinaSession> GetSessions();
        void SetEmulateLaggingMS(int min, int max);
        void GetEmulateLaggingMS(out int min, out int max);
    }
    /// <summary>
    /// 描述一个链接
    /// </summary>

    [Reflectible]
    public interface IMinaSession : INetSession
    {
        /// <summary>
        /// Session ID
        /// </summary>
        string ID { get; }

        IMinaSessionListener Listener { get; }

        /// <summary>
        /// 关闭此链接
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        bool Disconnect(bool force);

        /// <summary>
        /// 发送消息【通知】
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Send(object message);

        /// <summary>
        /// 发送消息【回馈】
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool SendResponse(IMessage request, IMessage response)
        {
            response.MessageID = request.MessageID;
            return this.Send(response);
        }

        /// <summary>
        /// 获取远端地址
        /// </summary>
        /// <returns></returns>
        IPEndPoint GetRemoteAddress();

    }

    /// <summary>
    /// 服务端监听器
    /// </summary>

    [Reflectible]
    public interface IMinaSessionListener
    {
        /// <summary>
        /// Session建立回调
        /// </summary>
        /// <param name="session"></param>
        void OnConnected(IMinaSession session);

        /// <summary>
        /// Session关闭回调
        /// </summary>
        /// <param name="session"></param>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        void OnDisconnected(IMinaSession session, String reason);

        /// <summary>
        /// 错误【编解码或者网络底层】
        /// </summary>
        /// <param name="session"></param>
        /// <param name="err"></param>
        void OnError(IMinaSession session, Exception err);

        /// <summary>
        /// 消息发送成功
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        void OnSentMessage(IMinaSession session, object message);

        /// <summary>
        /// 消息接收到
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        void OnReceivedMessage(IMinaSession session, object message);

    }
}
