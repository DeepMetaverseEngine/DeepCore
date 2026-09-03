using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCore.Threading;
using DeepCrystal.NetServer;
using DeepCrystal.Server;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CloseReason = SuperSocket.SocketBase.CloseReason;

namespace FuckPomeloServer.SSocket
{
    /// <summary>
    /// <pre>
    /// cfg.name: 服务器实例的名称;
    /// cfg.serverType: 服务器实例的类型的完整名称;
    /// cfg.serverTypeName: 所选用的服务器类型在 serverTypes 节点的名字，配置节点 serverTypes 用于定义所有可用的服务器类型，我们将在后面再做详细介绍;
    /// cfg.ip: 服务器监听的ip地址。你可以设置具体的地址，也可以设置为下面的值 Any - 所有的IPv4地址 IPv6Any - 所有的IPv6地址
    /// cfg.port: 服务器监听的端口;
    /// cfg.listenBacklog: 监听队列的大小;
    /// cfg.mode: Socket服务器运行的模式, Tcp (默认) 或者 Udp;
    /// cfg.disabled: 服务器实例是否禁用了;
    /// cfg.startupOrder: 服务器实例启动顺序, bootstrap 将按照此值的顺序来启动多个服务器实例;
    /// cfg.sendTimeOut: 发送数据超时时间;
    /// cfg.sendingQueueSize: 发送队列最大长度, 默认值为5;
    /// cfg.maxConnectionNumber: 可允许连接的最大连接数;
    /// cfg.receiveBufferSize: 接收缓冲区大小;
    /// cfg.sendBufferSize: 发送缓冲区大小;
    /// cfg.syncSend: 是否启用同步发送模式, 默认值: false;
    /// cfg.logCommand: 是否记录命令执行的记录;
    /// cfg.logBasicSessionActivity: 是否记录session的基本活动，如连接和断开;
    /// cfg.clearIdleSession: true 或 false, 是否定时清空空闲会话，默认值是 false;
    /// cfg.clearIdleSessionInterval: 清空空闲会话的时间间隔, 默认值是120, 单位为秒;
    /// cfg.idleSessionTimeOut: 会话空闲超时时间; 当此会话空闲时间超过此值，同时clearIdleSession被配置成true时，此会话将会被关闭; 默认值为300，单位为秒;
    /// cfg.security: Empty, Tls, Ssl3. Socket服务器所采用的传输层加密协议，默认值为空;
    /// cfg.maxRequestLength: 最大允许的请求长度，默认值为1024;
    /// cfg.textEncoding: 文本的默认编码，默认值是 ASCII;
    /// cfg.defaultCulture: 此服务器实例的默认 thread culture, 只在.Net 4.5中可用而且在隔离级别为 'None' 时无效;
    /// cfg.disableSessionSnapshot: 是否禁用会话快照, 默认值为 false.
    /// cfg.sessionSnapshotInterval: 会话快照时间间隔, 默认值是 5, 单位为秒;
    /// cfg.keepAliveTime: 网络连接正常情况下的keep alive数据的发送间隔, 默认值为 600, 单位为秒;
    /// cfg.keepAliveInterval: Keep alive失败之后, keep alive探测包的发送间隔，默认值为 60, 单位为秒;
    /// </pre>
    /// </summary>
    public class SSPomeloServerFactory : ServerFactory
    {
        private static SSPomeloServerFactory s_instance;
        public static SSPomeloServerFactory SuperInstance
        {
            get
            {
                if (s_instance == null) { s_instance = new SSPomeloServerFactory(); }
                return s_instance;
            }
        }

        public SSPomeloServerFactory()
        {
            SSPomeloServerFactory.s_instance = this;
        }

        /// <summary>
        /// config.Host
        /// config.Port
        /// config.MaxConnections
        /// config.Timeout
        /// </summary>
        /// <param name="config"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public override IServer CreateServer(ServerConfig fconfig, IExternalizableFactory codec)
        {
            var config = fconfig.Config;
            string host = config["Host"];
            int port = int.Parse(config["Port"]);
            int timeout = int.Parse(config["Timeout"]);
            int connections = int.Parse(config["MaxConnections"]);
            int hbtime = 0;
            if (config.ContainsKey("KeepAliveInterval"))
            {
                int.TryParse(config["KeepAliveInterval"], out hbtime);
            }
            string name;
            if (!config.TryGetValue("Name", out name))
            {
                name = "SSocketServer:" + host + ":" + port;
            }

            var cfg = new SuperSocket.SocketBase.Config.ServerConfig();
            {
                cfg.Port = port;
                cfg.MaxConnectionNumber = connections;
                cfg.ReceiveBufferSize = 16384;
                cfg.SendBufferSize = 16384;
                cfg.MaxRequestLength = 4 * 1024 * 1024;

                cfg.ListenBacklog = 100;
                cfg.SendingQueueSize = 100;
                cfg.Name = name;
                cfg.KeepAliveInterval = hbtime > 0 ? (hbtime) : (timeout / 1000);
                cfg.KeepAliveTime = cfg.KeepAliveInterval * 2;
                cfg.SyncSend = false;
                cfg.Mode = SocketMode.Tcp;
                cfg.DisableSessionSnapshot = true;
                cfg.LogCommand = false;
                cfg.LogBasicSessionActivity = false;
                cfg.LogAllSocketException = false;
                cfg.ClearIdleSession = true;
                cfg.ClearIdleSessionInterval = timeout / 1000;
                cfg.IdleSessionTimeOut = timeout / 1000 * 2;
                cfg.SendTimeOut = timeout / 1000;
                cfg.TextEncoding = "UTF-8";
            }
            return new SSPomeloAppServer(cfg, codec);
        }

        public override void Shutdown()
        {

        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------

    class SSPomeloAppServer : AppServer<SSPomeloSession, SSPomeloProtocolRequestInfo>, IServer
    {
        private readonly Logger log;
        private readonly SuperSocket.SocketBase.Config.ServerConfig config;
        private readonly IExternalizableFactory codec;
        private readonly ObjectPool<SSPomeloProtocolRequestInfo> requestPool;
        private readonly TimerTaskCompletionSourcePool tcsPool;

        //---------------------------------------------------------------------------------------------------------------------

        public IExternalizableFactory Codec { get { return codec; } }
        public string HostAddress { get => $"127.0.0.1:{Config.Port}"; }
        public int ListenPort { get => Config.Port; }

        public SSPomeloAppServer(SuperSocket.SocketBase.Config.ServerConfig cfg, IExternalizableFactory codec) : base(new SSPomeloReceiveFilterFactory())
        {
            this.log = LoggerFactory.GetLogger(cfg.Name);
            this.config = cfg;
            this.codec = codec;
            this.requestPool = new ConcurrentObjectPool<SSPomeloProtocolRequestInfo>();
            this.tcsPool = new TimerTaskCompletionSourcePool(cfg.Name, CollectionPool.Shared, 1000);
            base.NewRequestReceived += OnNewRequestReceived;
        }
        protected override void OnStopped()
        {
            base.OnStopped();
            tcsPool.Dispose();
        }
        protected override SSPomeloSession CreateAppSession(ISocketSession socketSession)
        {
            var ret = base.CreateAppSession(socketSession);
            OnCreateSession?.Invoke(ret);
            return ret;
        }
        protected override void OnNewSessionConnected(SSPomeloSession session)
        {
            base.OnNewSessionConnected(session);
            try
            {
                event_OnSessionConnected?.Invoke(session);
            }
            catch (Exception err) { log.Error(err.Message, err); }
        }
        protected override void OnSessionClosed(SSPomeloSession session, CloseReason reason)
        {
            try
            {
                event_OnSessionDisconnected?.Invoke(session);
            }
            catch (Exception err) { log.Error(err.Message, err); }
            base.OnSessionClosed(session, reason);
        }
        private void OnNewRequestReceived(SSPomeloSession session, SSPomeloProtocolRequestInfo bin)
        {
            if (event_OnSessionHandleNewMessage != null && event_OnSessionHandleNewMessage.Invoke(session, bin.Body))
            {
                return;
            }
            session.OnNewRequestReceived(bin);
        }
        private void OnError(Exception err)
        {
            if (event_OnServerError != null)
                event_OnServerError.Invoke(this, err);
        }
        //---------------------------------------------------------------------------------------------------------------------
        private SSPomeloProtocolRequestInfo NewRequestInfo(ObjectPool pool)
        {
            return new SSPomeloProtocolRequestInfo(this);
        }
        internal SSPomeloProtocolRequestInfo AllocRequestInfo(string key)
        {
            var ret = requestPool.Get(this, static (t, p) => t.NewRequestInfo(p));
            return ret;
        }
        internal void ReleaseRequestInfo(SSPomeloProtocolRequestInfo key)
        {
            requestPool.Release(key);
        }
        internal TaskCompletionSource<T> CreateAsyncCompletionSource<T>(string name, int timeoutMS = Timeout.Infinite)
        {
            return tcsPool.CreateTaskCompletionSource<T>(name, null, TaskCreationOptions.AttachedToParent, TimeSpan.FromMilliseconds(timeoutMS));
        }
        //---------------------------------------------------------------------------------------------------------------------
        #region Implements

        Task<bool> IServer.StartAsync()
        {
            try
            {
                if (this.Setup(config))
                {
                    var ret = this.Start();
                    log.Info("Start Server On Port : " + config.Port);
                    return Task.FromResult(ret);
                }
                else
                {
                    throw new Exception("Open Server Failed !");
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                OnError(err);
                return Task.FromResult(false);
            }
        }
        Task<bool> IServer.StopAsync(string reason)
        {
            try
            {
                base.Stop();
                return Task.FromResult(true);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                OnError(err);
                return Task.FromResult(false);
            }
        }
        void IDisposable.Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                OnError(err);
            }
            finally
            {
                this.disposing_events();
            }
        }
        void IServer.Broadcast(ISerializable message)
        {
            foreach (var s in base.GetAllSessions())
            {
                (s as ISession).Send(message);
            }
        }
        bool IServer.HasSession(ISession session)
        {
            return base.GetSessionByID(session.ID) != null;
        }
        ISession IServer.GetSessionByID(string sessionID)
        {
            return base.GetSessionByID(sessionID);
        }
        int IServer.GetSessions(IList<ISession> ret)
        {
            int count = 0;
            foreach (var s in base.GetAllSessions())
            {
                count++;
                ret.Add(s);
            }
            return count;
        }
        int IServer.SessionCount
        {
            get => base.SessionCount;
        }


        private ServerErrorHandler event_OnServerError;
        private SessionConnectedHandler event_OnSessionConnected;
        private SessionDisconnectedHandler event_OnSessionDisconnected;
        private SessionValidateAsyncHandler event_OnSessionValidateAsync;
        private SessionReceivedMessageHandler event_OnSessionReceivedMessage;
        private SessionReceivedBinaryHandler event_OnSessionReceivedBinary;
        private SessionReceivedRequestMessageHandler event_OnSessionReceivedMessageAsync;
        private SessionReceivedRequestBinaryHandler event_OnSessionReceivedBinaryAsync;
        private SessionMessageFilter event_OnSessionHandleNewMessage;

        private void disposing_events()
        {
            OnCreateSession = null;
            event_OnServerError = null;
            event_OnSessionConnected = null;
            event_OnSessionDisconnected = null;
            event_OnSessionReceivedMessage = null;
            event_OnSessionReceivedBinary = null;
            event_OnSessionReceivedMessageAsync = null;
            event_OnSessionReceivedBinaryAsync = null;
            event_OnSessionHandleNewMessage = null;
        }

        public event SessionHandler OnCreateSession;
        event ServerErrorHandler IServer.OnServerError { add { event_OnServerError += value; } remove { event_OnServerError -= value; } }
        event SessionConnectedHandler IServer.OnSessionConnected { add { event_OnSessionConnected += value; } remove { event_OnSessionConnected -= value; } }
        event SessionDisconnectedHandler IServer.OnSessionDisconnected { add { event_OnSessionDisconnected += value; } remove { event_OnSessionDisconnected -= value; } }
        event SessionValidateAsyncHandler IServer.OnSessionValidateAsync { add { event_OnSessionValidateAsync += value; } remove { event_OnSessionValidateAsync -= value; } }
        event SessionMessageFilter IServer.MessageFilter { add { event_OnSessionHandleNewMessage += value; } remove { event_OnSessionHandleNewMessage -= value; } }
        event SessionReceivedMessageHandler IServer.OnSessionReceivedMessage { add { event_OnSessionReceivedMessage += value; } remove { event_OnSessionReceivedMessage -= value; } }
        event SessionReceivedBinaryHandler IServer.OnSessionReceivedBinary { add { event_OnSessionReceivedBinary += value; } remove { event_OnSessionReceivedBinary -= value; } }
        event SessionReceivedRequestMessageHandler IServer.OnSessionRequestMessageAsync { add { event_OnSessionReceivedMessageAsync += value; } remove { event_OnSessionReceivedMessageAsync -= value; } }
        event SessionReceivedRequestBinaryHandler IServer.OnSessionRequestBinaryAsync { add { event_OnSessionReceivedBinaryAsync += value; } remove { event_OnSessionReceivedBinaryAsync -= value; } }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        internal void do_SessionReceived(SSPomeloSession session, RecvMessage body)
        {
            if (event_OnSessionReceivedBinary != null)
            {
                event_OnSessionReceivedBinary.Invoke(session, body.ReadBodyBinary(), body.MsgSendID);
            }
            if (event_OnSessionReceivedMessage != null)
            {
                event_OnSessionReceivedMessage.Invoke(session, body.ReadBody(), body.MsgSendID);
            }
            if (event_OnSessionReceivedMessageAsync != null)
            {
                var msg = body.ReadBody();
                var sendID = body.MsgSendID;
                Task.Run(async () =>
                {
                    var rsp = await event_OnSessionReceivedMessageAsync.Invoke(session, msg);
                    if (rsp != null)
                    {
                        session.SendInternal(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                });
            }
            if (event_OnSessionReceivedBinaryAsync != null)
            {
                var msg = body.ReadBodyBinary();
                var sendID = body.MsgSendID;
                Task.Run(async () =>
                {
                    var rsp = await event_OnSessionReceivedBinaryAsync.Invoke(session, msg);
                    if (rsp.HasRoute)
                    {
                        session.SendInternal(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                });
            }
        }
        internal SessionValidateAsyncHandler get_SessionValidateAsyncHandler() { return event_OnSessionValidateAsync; }

    }

    //------------------------------------------------------------------------------------------------------------------------------------

    class SSPomeloSession : AppSession<SSPomeloSession, SSPomeloProtocolRequestInfo>, ISession
    {
        public SSPomeloAppServer ssServer;
        private List<ISessionDataFilter> filters = new List<ISessionDataFilter>();
        public object UserTag { get; set; }
        public void AppendDataFilter(ISessionDataFilter filter)
        {
            filters.Add(filter);
        }
        public override void Initialize(IAppServer<SSPomeloSession, SSPomeloProtocolRequestInfo> appServer, ISocketSession socketSession)
        {
            this.ssServer = (SSPomeloAppServer)appServer;
            base.Initialize(appServer, socketSession);
            socketSession.Client.NoDelay = true;
        }
        protected override void HandleException(Exception e)
        {
            base.HandleException(e);
            this.OnError(e);
        }
        protected override void OnSessionClosed(SuperSocket.SocketBase.CloseReason reason)
        {
            base.OnSessionClosed(reason);
            if (event_OnClosed != null)
                event_OnClosed.Invoke(this, reason.ToString());
            this.ClearListening();
            this.disposing_events();
        }
        internal void OnError(Exception err)
        {
            if (event_OnError != null)
                event_OnError.Invoke(this, err);
        }
        //--------------------------------------------------------------------------------------------------------------------------------
        #region Listening

        private int request_indexer = 0;
        private ConcurrentDictionary<int, List<MessageHandler>> listening = new ConcurrentDictionary<int, List<MessageHandler>>();
        private ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>> request_msg = new ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>>();
        private ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>> request_bin = new ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>>();

        private void ClearListening()
        {
            foreach (var tcs in request_msg.Values)
            {
                tcs.TrySetCanceled();
            }
            request_msg.Clear();
            foreach (var tcs in request_bin.Values)
            {
                tcs.TrySetCanceled();
            }
            request_bin.Clear();
            lock (listening)
            {
                listening.Clear();
            }
        }
        private void InvokeListening(RecvMessage recv)
        {
            List<MessageHandler> invoking = null;
            {
                lock (listening)
                {
                    if (listening.TryGetValue(recv.MsgRoute, out var list))
                    {
                        if (invoking == null) invoking = new List<MessageHandler>();
                        invoking.AddRange(list);
                    }
                }
                if (invoking != null)
                {
                    foreach (var handler in invoking)
                    {
                        handler.Invoke(recv);
                    }
                }
            }
        }
        private void AddListening(MessageHandler handler)
        {
            lock (listening)
            {
                var list = listening.GetOrAdd(handler.route, _ => new List<MessageHandler>(1));
                list.Add(handler);
            }
        }
        private void RemoveListening(MessageHandler handler)
        {
            lock (listening)
            {
                if (listening.TryGetValue(handler.route, out var list))
                {
                    list.Remove(handler);
                }
            }
        }
        class MessageHandler : IMessageHandler
        {
            public readonly int route;
            public readonly SSPomeloSession session;
            public readonly Action<object, uint> action_msg;
            public readonly Action<BinaryMessage, uint> action_bin;
            internal MessageHandler(int route, SSPomeloSession session, Action<object, uint> action, Action<BinaryMessage, uint> action_bin)
            {
                this.route = route;
                this.session = session;
                this.action_msg = action;
                this.action_bin = action_bin;
            }
            internal void Invoke(RecvMessage recv)
            {
                if (this.action_msg != null) { this.action_msg.Invoke(recv.ReadBody(), recv.MsgSendID); }
                if (this.action_bin != null) { this.action_bin.Invoke(recv.ReadBodyBinary(), recv.MsgSendID); }
            }
            public void Cancel()
            {
                session.RemoveListening(this);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region Send

        private long total_sent_bytes;

        internal void SendInternal(ISerializable msg, MessageType msgType, uint sendID)
        {
            try
            {
                var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
                send.InitWithMessage(msgType, sendID, msg);
                this.total_sent_bytes += send.BufferLength;
                this.Send(send.Buffer, 0, send.BufferLength);
            }
            catch (Exception err)
            {
                OnError(err);
                this.Close(CloseReason.InternalError);
            }
        }
        internal void SendInternal(BinaryMessage msg, MessageType msgType, uint sendID)
        {
            try
            {
                var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
                send.InitWithMessage(msgType, sendID, msg);
                this.total_sent_bytes += send.BufferLength;
                this.Send(send.Buffer, 0, send.BufferLength);
            }
            catch (Exception err)
            {
                OnError(err);
                this.Close(CloseReason.InternalError);
            }
        }
        internal void SendInternal(SendMessage send)
        {
            try
            {
                this.total_sent_bytes += send.BufferLength;
                this.Send(send.Buffer, 0, send.BufferLength);
            }
            catch (Exception err)
            {
                OnError(err);
                this.Close(CloseReason.InternalError);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region Received

        private bool validated = false;
        private long total_recv_bytes;

        internal void OnNewRequestReceived(SSPomeloProtocolRequestInfo bin)
        {
            try
            {
                this.total_recv_bytes += bin.TotalLength;
                if (!validated)
                {
                    if (bin.Body.PkgType == PackageType.PKG_HANDSHAKE)
                    {
                        var handshake = bin.Body.ReadBodySystemMessage() as SystemHandshake;
                        var do_validate = (event_OnValidate != null) ? event_OnValidate : ssServer.get_SessionValidateAsyncHandler();
                        if (do_validate != null)
                        {
                            do_validate.Invoke(this, handshake.user).ContinueWith(task =>
                            {
                                try
                                {
                                    if (task.IsFaulted)
                                    {
                                        this.OnError(task.Exception);
                                        this.Close(CloseReason.ApplicationError);
                                    }
                                    else if (task.IsCanceled)
                                    {
                                        this.Close(CloseReason.ApplicationError);
                                    }
                                    else if (task.IsCompleted)
                                    {
                                        var v_result = task.Result.IsValidate;
                                        var v_token = task.Result.Token;
                                        validated = v_result;
                                        SendHandshake(v_token);
                                        if (v_result == false)
                                        {
                                            this.Close(CloseReason.ProtocolError);
                                        }
                                    }
                                }
                                catch (Exception err)
                                {
                                    this.OnError(err);
                                    this.Close(CloseReason.ProtocolError);
                                }
                            });
                        }
                        else
                        {
                            validated = true;
                            SendHandshake(null);
                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Session Not Validate : {0} : {1} : Drop!!!", this, bin.Body);
                    }
                }
                else
                {
                    switch (bin.Body.PkgType)
                    {
                        case PackageType.PKG_HEARTBEAT:
                            SendHeartbeat(bin.Body.ReadBodySystemMessage() as SystemHeartbeat);
                            break;
                        case PackageType.PKG_MESSAGE:
                            ProcessMessage(bin.Body);
                            break;
                        default:
                            this.Close(CloseReason.ProtocolError);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                this.OnError(e);
                this.Close(CloseReason.ProtocolError);
            }
            finally
            {
                bin.Dispose();
            }
        }
        private void SendHandshake(ISerializable token)
        {
            var rsp = new SystemHandshakeAck();
            rsp.token = token;
            rsp.remote_info = base.Config.Name;
            rsp.heartbeat_interval_ms = base.Config.KeepAliveInterval * 1000;
            var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
            send.InitWithSystemMessage(rsp);
            SendInternal(send);
        }
        private void SendHeartbeat(SystemHeartbeat hb)
        {
            var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
            send.InitWithSystemMessage(hb);
            SendInternal(send);
        }
        private void ProcessMessage(RecvMessage body)
        {
            if (body.MsgType == MessageType.MSG_RPC_RESPONSE_C2S)
            {
                if (request_msg.TryGetValue(body.MsgSendID, out var tcs_msg))
                {
                    tcs_msg.TrySetResult(body.ReadBody());
                    return;
                }
                else if (request_bin.TryGetValue(body.MsgSendID, out var tcs_bin))
                {
                    tcs_bin.TrySetResult(body.ReadBodyBinary());
                    return;
                }
            }
            InvokeListening(body);
            if (event_OnReceivedMessage != null)
            {
                event_OnReceivedMessage.Invoke(this, body.ReadBody(), body.MsgSendID);
            }
            if (event_OnReceivedBinary != null)
            {
                event_OnReceivedBinary.Invoke(this, body.ReadBodyBinary(), body.MsgSendID);
            }
            if (event_OnReceivedMessageAsync != null)
            {
                var msg = body.ReadBody();
                var sendID = body.MsgSendID;
                Task.Run(async () =>
                {
                    var rsp = await event_OnReceivedMessageAsync.Invoke(this, msg);
                    if (rsp != null)
                    {
                        this.SendInternal(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                });
            }
            if (event_OnReceivedBinaryAsync != null)
            {
                var msg = body.ReadBodyBinary();
                var sendID = body.MsgSendID;
                Task.Run(async () =>
                {
                    var rsp = await event_OnReceivedBinaryAsync.Invoke(this, msg);
                    if (rsp.HasRoute)
                    {
                        this.SendInternal(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                });
            }
            ssServer.do_SessionReceived(this, body);
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region Implements

        private DeepCore.HashMap<string, object> attributes = new DeepCore.HashMap<string, object>();

        string ISession.ID
        {
            get { return base.SessionID; }
        }
        bool INetSession.IsConnected
        {
            get { return base.Connected; }
        }
        long INetSession.TotalRecvBytes
        {
            get { return total_recv_bytes; }
        }
        long INetSession.TotalSentBytes
        {
            get { return total_sent_bytes; }
        }
        EndPoint ISession.RemoteAddress
        {
            get { return base.RemoteEndPoint; }
        }
        public IDictionary<string, object> Attributes
        {
            get { return attributes; }
        }

        object ISession.UserTag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsConnected => base.Connected;
        public long TotalSentBytes => total_sent_bytes;
        public long TotalRecvBytes => total_recv_bytes;

        void ISession.Disconnect(string reason)
        {
            var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
            send.InitWithSystemMessage(new SystemKick() { reason = reason });
            this.SendInternal(send);
            base.Close(CloseReason.ServerClosing);
        }
        void ISession.Send(ISerializable message)
        {
            this.SendInternal(message, MessageType.MSG_NOTIFY, 0);
        }
        void ISession.Send(BinaryMessage message)
        {
            this.SendInternal(message, MessageType.MSG_NOTIFY, 0);
        }
        void ISession.SendResponse(ISerializable response, uint sendID)
        {
            this.SendInternal(response, MessageType.MSG_RESPONSE_S2C, sendID);
        }
        void ISession.SendResponse(BinaryMessage response, uint sendID)
        {
            this.SendInternal(response, MessageType.MSG_RESPONSE_S2C, sendID);
        }
        Task<bool> ISession.DisconnectAsync(string reason)
        {
            var send = new SendMessage((this.AppServer as SSPomeloAppServer).Codec);
            send.InitWithSystemMessage(new SystemKick() { reason = reason });
            this.SendInternal(send);
            base.Close(CloseReason.ServerClosing);
            return Task.FromResult(true);
        }
        Task<bool> ISession.SendAsync(ISerializable message)
        {
            ((ISession)this).Send(message);
            return Task.FromResult(true);
        }
        Task<bool> ISession.SendAsync(BinaryMessage message)
        {
            ((ISession)this).Send(message);
            return Task.FromResult(true);
        }
        Task<bool> ISession.SendResponseAsync(ISerializable response, uint sendID)
        {
            ((ISession)this).SendResponse(response, sendID);
            return Task.FromResult(true);
        }
        Task<bool> ISession.SendResponseAsync(BinaryMessage response, uint sendID)
        {
            ((ISession)this).SendResponse(response, sendID);
            return Task.FromResult(true);
        }

        async Task<T> ISession.SendRequestAsync<T>(ISerializable request)
        {
            var sendID = (uint)Interlocked.Increment(ref request_indexer);
            var timeout = (Config.SendTimeOut + Config.SendTimeOut) * 1000;
            var tcs = ssServer.CreateAsyncCompletionSource<ISerializable>(request + " : SendRequestAsync(ISerializable)", timeout);
            //             var tcs = new TaskCompletionSource<ISerializable>();
            //             var ct = new CancellationTokenSource();
            //             ct.Token.Register(() =>
            //             {
            //                 if (tcs.TrySetCanceled())
            //                 {
            //                     Logger.Warn(request + " : SendRequestAsync(ISerializable) Task Timeout, Canceled!!!");
            //                 }
            //             }, useSynchronizationContext: false);
            request_msg.TryAdd(sendID, tcs);
            this.SendInternal(request, MessageType.MSG_RPC_REQUEST_S2C, sendID);
            var rsp = await tcs.Task;
            return (T)rsp;
        }
        Task<BinaryMessage> ISession.SendRequestAsync(BinaryMessage request)
        {
            var sendID = (uint)Interlocked.Increment(ref request_indexer);
            var timeout = (Config.SendTimeOut + Config.SendTimeOut) * 1000;
            var tcs = ssServer.CreateAsyncCompletionSource<BinaryMessage>(request.Route + " : SendRequestAsync(BinaryMessage)", timeout);
            //             var tcs = new TaskCompletionSource<BinaryMessage>();
            //             var ct = new CancellationTokenSource((Config.SendTimeOut + Config.SendTimeOut) * 1000);
            //             ct.Token.Register(() =>
            //             {
            //                 if (tcs.TrySetCanceled())
            //                 {
            //                     Logger.Warn(request.Route + " : SendRequestAsync(BinaryMessage) Task Timeout, Canceled!!!");
            //                 }
            //             }, useSynchronizationContext: false);
            request_bin.TryAdd(sendID, tcs);
            this.SendInternal(request, MessageType.MSG_RPC_REQUEST_S2C, sendID);
            return tcs.Task;
        }
        IMessageHandler ISession.HandleMessage<T>(int route, Action<T, uint> action)
        {
            var handler = new MessageHandler(route, this, (msg, sid) => { action((T)msg, sid); }, null);
            AddListening(handler);
            return handler;
        }
        IMessageHandler ISession.HandleBinary(int route, Action<BinaryMessage, uint> action)
        {
            var handler = new MessageHandler(route, this, null, action);
            AddListening(handler);
            return handler;
        }
   



        private SessionValidateAsyncHandler event_OnValidate;
        private SessionClosedHandler event_OnClosed;
        private SessionErrorHandler event_OnError;
        private SessionReceivedMessageHandler event_OnReceivedMessage;
        private SessionReceivedBinaryHandler event_OnReceivedBinary;
        private SessionReceivedRequestMessageHandler event_OnReceivedMessageAsync;
        private SessionReceivedRequestBinaryHandler event_OnReceivedBinaryAsync;
        private SessionSentHandler event_OnSent;

        private void disposing_events()
        {
            listening.Clear();
            event_OnValidate = null;
            event_OnClosed = null;
            event_OnError = null;
            event_OnReceivedMessage = null;
            event_OnReceivedBinary = null;
            event_OnReceivedMessageAsync = null;
            event_OnReceivedBinaryAsync = null;
            event_OnSent = null;
        }

        event SessionValidateAsyncHandler ISession.OnValidateAsync { add { event_OnValidate += value; } remove { event_OnValidate -= value; } }
        event SessionClosedHandler ISession.OnClosed { add { event_OnClosed += value; } remove { event_OnClosed -= value; } }
        event SessionErrorHandler ISession.OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
        event SessionReceivedMessageHandler ISession.OnReceivedMessage { add { event_OnReceivedMessage += value; } remove { event_OnReceivedMessage -= value; } }
        event SessionReceivedBinaryHandler ISession.OnReceivedBinary { add { event_OnReceivedBinary += value; } remove { event_OnReceivedBinary -= value; } }
        event SessionReceivedRequestMessageHandler ISession.OnRequestMessageAsync { add { event_OnReceivedMessageAsync += value; } remove { event_OnReceivedMessageAsync -= value; } }
        event SessionReceivedRequestBinaryHandler ISession.OnRequestBinaryAsync { add { event_OnReceivedBinaryAsync += value; } remove { event_OnReceivedBinaryAsync -= value; } }
        event SessionSentHandler ISession.OnSent { add { event_OnSent += value; } remove { event_OnSent -= value; } }


        #endregion

    }

    //------------------------------------------------------------------------------------------------------------------------------------
    #region Protocol

    class SendMessage : ISendMessage
    {
        internal SendMessage(IExternalizableFactory codec) : base(codec)
        {
        }
        internal void Dispose()
        {
            base.Disposing();
        }
    }
    class RecvMessage : IRecvMessage
    {
        internal RecvMessage(IExternalizableFactory codec) : base(codec)
        {
        }
        internal void Dispose()
        {
            base.Disposing();
        }
    }
    class SSPomeloProtocolRequestInfo : IRequestInfo<RecvMessage>
    {
        private readonly SSPomeloAppServer server;
        private readonly RecvMessage body;
        private string key;
        public RecvMessage Body
        {
            get { return body; }
        }
        public string Key
        {
            get { return key; }
            set { key = value; }
        }
        public int TotalLength
        {
            get; internal set;
        }
        internal SSPomeloProtocolRequestInfo(SSPomeloAppServer server)
        {
            this.server = server;
            this.body = new RecvMessage(server.Codec);
        }
        internal void Dispose()
        {
            this.key = null;
            this.body.Dispose();
            this.server.ReleaseRequestInfo(this);
        }
    }

    class SSPomeloReceiveFilterFactory : IReceiveFilterFactory<SSPomeloProtocolRequestInfo>
    {
        public IReceiveFilter<SSPomeloProtocolRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, System.Net.IPEndPoint remoteEndPoint)
        {
            return new SSPomeloReceiveFilter(appSession as SSPomeloSession);
        }
    }

    class SSPomeloReceiveFilter : FixedHeaderReceiveFilter<SSPomeloProtocolRequestInfo>
    {
        private readonly SSPomeloSession appSession;
        public SSPomeloReceiveFilter(SSPomeloSession appSession) : base(RecvMessage.FIXED_HEAD_SIZE)
        {
            this.appSession = appSession;
        }
        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            PackageType pkg_type;
            PackageMask pkg_mask;
            int pkg_length;
            int max = appSession.AppServer.Config.MaxRequestLength;
            IProtocol.DoDecodeHead(header, offset, out pkg_type, out pkg_mask, out pkg_length);
            if (pkg_length > max)
            {
                appSession.Close(CloseReason.InternalError);
                throw new Exception(string.Format("PkgLength:{0} out of limit:{1} {2}", 
                    pkg_length, 
                    max,
                    pkg_type));
            }
            return pkg_length;
        }
        protected override SSPomeloProtocolRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int bodyOffset, int bodyLength)
        {
            var ret = (appSession.AppServer as SSPomeloAppServer).AllocRequestInfo("");
            try
            {
                ret.Body.BufferLength = RecvMessage.FIXED_HEAD_SIZE + bodyLength;
                ret.Body.BufferPosition = RecvMessage.FIXED_HEAD_SIZE;
                Buffer.BlockCopy(header.Array, header.Offset, ret.Body.Buffer, 0, RecvMessage.FIXED_HEAD_SIZE);
                ret.Body.ReadHead();
                if (ret.Body.PkgLength != bodyLength)
                {
                    throw new Exception(string.Format("Decode Error : Package Length({0}) Not Equal BodyLength({1})", ret.Body.PkgLength, bodyLength));
                }
                if (bodyLength > 0)
                {
                    Buffer.BlockCopy(bodyBuffer, bodyOffset, ret.Body.Buffer, RecvMessage.FIXED_HEAD_SIZE, bodyLength);
                }
                ret.TotalLength = RecvMessage.FIXED_HEAD_SIZE + bodyLength;
                ret.Body.BeginBody();
                return ret;
            }
            catch (Exception err)
            {
                ret.Dispose();
                throw err;
            }
        }
    }
    #endregion
    //------------------------------------------------------------------------------------------------------------------------------------
}
