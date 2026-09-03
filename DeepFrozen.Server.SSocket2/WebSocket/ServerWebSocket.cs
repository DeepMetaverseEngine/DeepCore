using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCore.Threading;
using DeepCrystal.NetServer;
using DeepCrystal.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SuperSocket;
using SuperSocket.Connection;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;



namespace DeepFrozen.Server.SSocket2.WebSocket
{
    public class WSServerFactory : ServerFactory
    {
        public static int HEARTBEAT_INTERVAL_SEC = 15;
        private static WSServerFactory s_instance;
        public static WSServerFactory WSInstance
        {
            get
            {
                if (s_instance == null) { s_instance = new WSServerFactory(); }
                return s_instance;
            }
        }
        public WSServerFactory()
        {
            WSServerFactory.s_instance = this;
        }
        public override DeepCrystal.NetServer.IServer CreateServer(ServerConfig config, IExternalizableFactory codec)
        {
            return new WSServer(config, codec);
        }
        public override void Shutdown()
        {
        }
    }

    public class WSServer : DeepCrystal.NetServer.IServer
    {
        public static bool TRACE_PROTOCOL = false;
        //------------------------------------------------------------------------------
        private readonly Logger log;
        private readonly ServerProtocolPool messagePool;
        private readonly TaskCompletionSourcePool tpool;
        private readonly ConcurrentDictionary<string, WSNetSession> sessions = new ConcurrentDictionary<string, WSNetSession>();
        private IHost host;
        private Properties config;
        //------------------------------------------------------------------------------
        public bool EnableRequest { get; private set; } = false;
        public int RequestTimeoutMS { get; private set; } = 30000;
        public IExternalizableFactory Codec { get; }
        public ServerConfig Config { get; }
        public ServerProtocolPool MessagePool { get => messagePool; }
        public TaskCompletionSourcePool TcsPool { get => tpool; }
        public int SessionCount => sessions.Count;
        public int ListenPort { get; private set; }
        public int ListenBacklog { get; private set; } = 1024;
        public int MaxConnections { get; private set; } = 0;
        //------------------------------------------------------------------------------
        public WSServer(ServerConfig cfg, IExternalizableFactory codec)
        {
            this.log = DeepCore.Log.LoggerFactory.GetLogger(typeof(WSServer));
            this.Codec = codec;
            this.Config = cfg;
            this.messagePool = new ServerProtocolPool(Codec);
            this.tpool = new TimerTaskCompletionSourcePool(cfg.Name, CollectionPool.Shared, 1000);
            this.config = cfg.Config;
            this.ListenPort = cfg.Port;
            if (config.TryGetAsBool(nameof(EnableRequest), out var boolValue))
            {
                EnableRequest = boolValue;
            }
            if (config.TryGetAsInt(nameof(RequestTimeoutMS), out var intValue))
            {
                RequestTimeoutMS = intValue;
            }
            if (config.TryGetAsInt("Port", out intValue))
            {
                this.ListenPort = intValue;
            }
            if (config.TryGetAsInt("Listen", out intValue))
            {
                this.ListenPort = intValue;
            }
            if (config.TryGetAsInt("ListenPort", out intValue))
            {
                this.ListenPort = intValue;
            }
            if (config.TryGetAsInt(nameof(MaxConnections), out intValue))
            {
                this.MaxConnections = intValue;
            }
            if (config.TryGetAsInt(nameof(ListenBacklog), out intValue))
            {
                this.ListenBacklog = intValue;
            }
            if (config.TryGetAsInt("Backlog", out intValue))
            {
                this.ListenBacklog = intValue;
            }
            //             if (config.TryGetAsInt(nameof(MaxRequestLength), out intValue))
            //             {
            //                 this.MaxRequestLength = intValue;
            //             }
        }
        public void SetListenPort(int value)
        {
            if (value > 0)
            {
                ListenPort = value;
                config["Port"] = value.ToString();
                config["Listen"] = value.ToString();
                config["ListenPort"] = value.ToString();
            }
        }
        public void Dispose()
        {
            onDisposingEvents();
            this.StopAsync("Dispose").Forget();
        }
        public async Task<bool> StartAsync()
        {
            try
            {
                bool create = false;
                lock (this)
                {
                    if (host == null)
                    {
                        var builder = WebSocketHostBuilder.Create();
                        builder.UseSession<WSNetSession>();
                        builder.UseHostedService<WSNetServer>();
                        builder.UseWebSocketMessageHandler(async (session, message) =>
                        {
                            if (message.OpCode == OpCode.Binary)
                            {
                                await (session as WSNetSession).HandleMessageAsync(message);
                            }
                        });
                        builder.ConfigureAppConfiguration((hostCtx, configApp) =>
                        {
                            var cfg = new Dictionary<string, string>
                            {
                                    { "serverOptions:name", $"{Config.Name}" },
                                    { "serverOptions:listenBacklog", $"{this.ListenBacklog}" },
                                    { "serverOptions:listeners:0:ip", "Any" },
                                    { "serverOptions:listeners:0:port", $"{this.ListenPort}" },
                                    { "serverOptions:listeners:0:backlog", $"{this.ListenBacklog}" },
                            };
                            cfg.AddRange(config);
                            configApp.AddInMemoryCollection(cfg);
                        });
                        builder.ConfigureSuperSocket(options =>
                        {
                            foreach (var listenerOptions in options.Listeners.Where(l => l.AuthenticationOptions != null && l.AuthenticationOptions.ClientCertificateRequired))
                            {
                                listenerOptions.AuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                                {
                                    return true;
                                };
                            }
                        });
                        //                         builder.ConfigureLogging((hostCtx, loggingBuilder) =>
                        //                         {
                        //                             loggingBuilder.AddConsole();
                        //                         });
                        this.host = builder.Build();
                        var server = host.AsServer() as WSNetServer;
                        server.Init(this);
                        create = true;
                    }
                }
                if (create)
                {
                    await host.StartAsync();
                }
                return create;
            }
            catch (Exception ex)
            {
                cb_OnServerError(ex);
            }
            return false;
        }
        public async Task<bool> StopAsync(string reason)
        {
            try
            {
                var h = host;
                lock (this)
                {
                    if (h == null)
                    {
                        return false;
                    }
                    h = host;
                    this.host = null;
                }
                if (h != null)
                {
                    await h.StopAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                cb_OnServerError(ex);
            }
            return false;
        }
        //------------------------------------------------------------------------------
        public void Broadcast(ISerializable message)
        {
            var list = new List<WSNetSession>(sessions.Values);
            {
                foreach (ISession e in list)
                {
                    try
                    {
                        e.Send(message);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        public ISession GetSessionByID(string sessionID)
        {
            return sessions[sessionID];
        }
        public int GetSessions(IList<ISession> ret)
        {
            int count = 0;
            var list = new List<ISession>(sessions.Values);
            foreach (ISession s in list)
            {
                count++;
                ret.Add(s);
            }
            return count;
        }
        public bool HasSession(ISession session)
        {
            return sessions.ContainsKey(session.ID);
        }
        //------------------------------------------------------------------------------

        #region Event
        internal void cb_OnSessionError(WSNetSession session, Exception err)
        {
            this.log.Error(err.Message, err);
            try
            {
                event_OnServerError?.Invoke(this, err);
            }
            catch (Exception err2)
            {
                log.Error(err2.Message, err2);
            }
        }
        internal void cb_OnServerError(Exception err)
        {
            this.log.Error(err.Message, err);
            try
            {
                event_OnServerError?.Invoke(this, err);
            }
            catch (Exception err2)
            {
                log.Error(err2.Message, err2);
            }
        }

        internal bool cb_NewMessageFilter(WSNetSession session, IRecvMessage message)
        {
            try
            {
                var handle = event_MessageFilter;
                if (handle != null)
                {
                    return handle.Invoke(session, message);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return false;
        }
        internal async ValueTask cb_OnSessionConnected(WSNetSession session)
        {
            try
            {
                if (MaxConnections > 0 && sessions.Count >= MaxConnections)
                {
                    await session.DisconnectAsync("MaxConnections");
                }
                else
                {
                    if (sessions.TryAdd(session.SessionID, session))
                    {
                        try
                        {
                            event_OnSessionConnected?.Invoke(session);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                        session.cb_SessionReady();
                    }
                    else
                    {
                        await session.DisconnectAsync("Session Exist");
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        internal void cb_OnSessionDisconnected(WSNetSession session)
        {
            if (sessions.Remove(session.SessionID, out var exist))
            {
                try
                {
                    event_OnSessionDisconnected?.Invoke(session);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            }
        }
        internal async Task cb_SessionReceived(WSNetSession session, IRecvMessage recv)
        {
            BinaryMessage bin = BinaryMessage.NULL;
            ISerializable ser = null;
            uint sendID = recv.MsgSendID;
            if (event_OnSessionReceivedBinary != null || event_OnSessionRequestBinaryAsync != null)
            {
                bin = recv.ReadBodyBinary();
            }
            if (event_OnSessionReceivedMessage != null || event_OnSessionRequestMessageAsync != null)
            {
                ser = recv.ReadBody();
            }
            try
            {
                event_OnSessionReceivedBinary?.Invoke(session, bin, sendID);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            try
            {
                event_OnSessionReceivedMessage?.Invoke(session, ser, sendID);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            try
            {
                if (event_OnSessionRequestMessageAsync != null)
                {
                    var rsp = await event_OnSessionRequestMessageAsync.Invoke(session, ser);
                    if (rsp != null)
                    {
                        await session.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            try
            {
                if (event_OnSessionRequestBinaryAsync != null)
                {
                    var rsp = await event_OnSessionRequestBinaryAsync.Invoke(session, bin);
                    if (rsp.HasRoute)
                    {
                        await session.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------

        private SessionMessageFilter event_MessageFilter;
        private ServerErrorHandler event_OnServerError;
        private SessionConnectedHandler event_OnSessionConnected;
        private SessionDisconnectedHandler event_OnSessionDisconnected;
        private SessionValidateAsyncHandler event_OnSessionValidateAsync;
        private SessionReceivedMessageHandler event_OnSessionReceivedMessage;
        private SessionReceivedBinaryHandler event_OnSessionReceivedBinary;
        private SessionReceivedRequestMessageHandler event_OnSessionRequestMessageAsync;
        private SessionReceivedRequestBinaryHandler event_OnSessionRequestBinaryAsync;

        public event SessionMessageFilter MessageFilter { add { event_MessageFilter += value; } remove { event_MessageFilter -= value; } }
        public event ServerErrorHandler OnServerError { add { event_OnServerError += value; } remove { event_OnServerError -= value; } }
        public event SessionConnectedHandler OnSessionConnected { add { event_OnSessionConnected += value; } remove { event_OnSessionConnected -= value; } }
        public event SessionDisconnectedHandler OnSessionDisconnected { add { event_OnSessionDisconnected += value; } remove { event_OnSessionDisconnected -= value; } }
        public event SessionValidateAsyncHandler OnSessionValidateAsync { add { event_OnSessionValidateAsync += value; } remove { event_OnSessionValidateAsync -= value; } }
        public event SessionReceivedMessageHandler OnSessionReceivedMessage { add { event_OnSessionReceivedMessage += value; } remove { event_OnSessionReceivedMessage -= value; } }
        public event SessionReceivedBinaryHandler OnSessionReceivedBinary { add { event_OnSessionReceivedBinary += value; } remove { event_OnSessionReceivedBinary -= value; } }
        public event SessionReceivedRequestMessageHandler OnSessionRequestMessageAsync { add { event_OnSessionRequestMessageAsync += value; } remove { event_OnSessionRequestMessageAsync -= value; } }
        public event SessionReceivedRequestBinaryHandler OnSessionRequestBinaryAsync { add { event_OnSessionRequestBinaryAsync += value; } remove { event_OnSessionRequestBinaryAsync -= value; } }
        public event SessionHandler OnCreateSession;
        private void onDisposingEvents()
        {
            event_MessageFilter = null;
            event_OnServerError = null;
            event_OnSessionConnected = null;
            event_OnSessionDisconnected = null;
            event_OnSessionValidateAsync = null;
            event_OnSessionReceivedMessage = null;
            event_OnSessionReceivedBinary = null;
            event_OnSessionRequestMessageAsync = null;
            event_OnSessionRequestBinaryAsync = null;
            OnCreateSession = null;
        }

        #endregion
        /*--------------------------------------------------------------------------------------------------------------------------------------------*/
        public class WSNetServer : SuperSocketService<WebSocketPackage>
        {
            public WSServer Server { get; private set; }
            internal void Init(WSServer server)
            {
                this.Server = server;
            }
            public WSNetServer(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions) : base(serviceProvider, serverOptions) { }
            protected override async ValueTask FireSessionConnectedEvent(AppSession session)
            {
                await base.FireSessionConnectedEvent(session);
                if (session is WSNetSession wssession)
                {
                    wssession.Init(this);
                    Server.OnCreateSession?.Invoke(wssession);
                }
            }
            protected override async ValueTask OnSessionConnectedAsync(IAppSession session)
            {
                await Server.cb_OnSessionConnected(session as WSNetSession);
                await base.OnSessionConnectedAsync(session);
            }
            protected override ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
            {
                Server.cb_OnSessionDisconnected(session as WSNetSession);
                return base.OnSessionClosedAsync(session, e);
            }
            protected override ValueTask<bool> OnSessionErrorAsync(IAppSession session, PackageHandlingException<WebSocketPackage> exception)
            {
                Server.cb_OnSessionError(session as WSNetSession, exception);
                return base.OnSessionErrorAsync(session, exception);
            }
        }
        /*--------------------------------------------------------------------------------------------------------------------------------------------*/
        public class WSNetSession : WebSocketSession, ISession
        {
            private TaskCompletionSource waitReady;
            private bool validated = false;
            private List<ISessionDataFilter> filters = new List<ISessionDataFilter>();
            public DateTime LastReceivedTimeUTC { get; private set; } = DateTime.UtcNow;
            public WSNetSession()
            {
            }
            internal void Init(WSNetServer server)
            {
                this.endpoint = this.RemoteEndPoint;
            }
            protected override void Reset()
            {
                this.OnDisposeEvents();
                this.OnDisposeListening();
                base.Reset();
            }
            public void AppendDataFilter(ISessionDataFilter filter)
            {
                filters.Add(filter);
            }
            protected override ValueTask OnSessionConnectedAsync()
            {
                this.waitReady = new TaskCompletionSource();
                return base.OnSessionConnectedAsync();
            }
            protected override ValueTask OnSessionClosedAsync(CloseEventArgs e)
            {
                try
                {
                    event_OnClosed?.Invoke(this, $"{e.Reason}");
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                this.OnDisposeEvents();
                this.OnDisposeListening();
                return base.OnSessionClosedAsync(e);
            }
            internal void cb_SessionReady()
            {
                waitReady.TrySetResult();
            }
            //---------------------------------------------------------------------------------
            #region Recv
            internal async Task HandleMessageAsync(WebSocketPackage package)
            {
                this.LastReceivedTimeUTC = DateTime.UtcNow;
                var bytes = new ArraySegment<byte>(package.Data.ToArray());
                foreach (var filter in filters)
                {
                    bytes = filter.Receiving(this, ref this.endpoint, bytes);
                }
                if (waitReady != null)
                {
                    try
                    {
                        await waitReady.Task;
                    }
                    catch (Exception err)
                    {
                        Logger.LogError(err, err.Message);
                        await DisconnectAsync(err.Message);
                        return;
                    }
                    finally
                    {
                        waitReady = null;
                    }
                }
                this.recv_bytes += bytes.Count;
                try
                {
                    using (var recv = WSServer.MessagePool.AllocRecv())
                    {
                        recv.FillBuffer(bytes);
                        recv.ReadHead();
                        recv.BeginBody();
                        if (TRACE_PROTOCOL) Logger.LogInformation("Recv <-------- " + recv);
                        await recv_onProtocolReceived(recv);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                    await this.DisconnectAsync(err.Message);
                }
            }
            private async Task recv_onProtocolReceived(RecvMessage recv)
            {
                if (WSServer.cb_NewMessageFilter(this, recv))
                {
                    return;
                }
                if (!validated)
                {
                    if (recv.PkgType == PackageType.PKG_HANDSHAKE)
                    {
                        var handshake = recv.ReadBodySystemMessage() as SystemHandshake;
                        if (handshake.local_info != null)
                        {
                        }
                        var do_validate = (event_OnValidate != null) ? event_OnValidate : WSServer.event_OnSessionValidateAsync;
                        if (do_validate != null)
                        {
                            var rst = await do_validate.Invoke(this, handshake.user);
                            if (rst != null)
                            {
                                var v_result = rst.IsValidate;
                                var v_token = rst.Token;
                                await this.SendHandshake(v_token);
                                this.validated = v_result;
                                if (!validated)
                                {
                                    await this.DisconnectAsync("Not Validate");
                                }
                            }
                            else
                            {
                                await this.DisconnectAsync("Not Validate");
                            }
                        }
                        else
                        {
                            validated = true;
                            await SendHandshake(null);
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Session Not Validate : {this} : {recv} : {recv.BodyType} : Drop!!!");
                    }
                }
                else
                {
                    switch (recv.PkgType)
                    {
                        case PackageType.PKG_HEARTBEAT:
                            await SendHeartbeat(recv.ReadBodySystemMessage() as SystemHeartbeat);
                            break;
                        case PackageType.PKG_MESSAGE:
                            await recv_ProcessMessage(recv);
                            break;
                        default:
                            this.Log( LogLevel.Warning, $"Unknow Protocol : PkgType={recv.PkgType}");
                            //await this.DisconnectAsync("Unknow Protocol");
                            break;
                    }
                }
            }
            private async Task recv_ProcessMessage(IRecvMessage body)
            {
                try
                {
                    recv_InvokeListening(body);
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                try
                {
                    if (event_OnReceivedMessage != null)
                    {
                        event_OnReceivedMessage.Invoke(this, body.ReadBody(), body.MsgSendID);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                try
                {
                    if (event_OnReceivedBinary != null)
                    {
                        event_OnReceivedBinary.Invoke(this, body.ReadBodyBinary(), body.MsgSendID);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                try
                {
                    if (event_OnReceivedMessageAsync != null)
                    {
                        var msg = body.ReadBody();
                        var sendID = body.MsgSendID;
                        var rsp = await event_OnReceivedMessageAsync.Invoke(this, msg);
                        if (rsp != null)
                        {
                            await this.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                try
                {
                    if (event_OnReceivedBinaryAsync != null)
                    {
                        var msg = body.ReadBodyBinary();
                        var sendID = body.MsgSendID;
                        var rsp = await event_OnReceivedBinaryAsync.Invoke(this, msg);
                        if (rsp.HasRoute)
                        {
                            await this.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError(err, err.Message);
                }
                await WSServer.cb_SessionReceived(this, body);
            }

            #endregion
            //---------------------------------------------------------------------------------
            #region Listen Request

            private int request_indexer = 0;
            private ConcurrentDictionary<int, List<MessageHandler>> listening = new ConcurrentDictionary<int, List<MessageHandler>>();
            private ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>> request_msg = new ConcurrentDictionary<uint, TaskCompletionSource<ISerializable>>();
            private ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>> request_bin = new ConcurrentDictionary<uint, TaskCompletionSource<BinaryMessage>>();

            private void recv_InvokeListening(IRecvMessage recv)
            {
                if (recv.MsgType == MessageType.MSG_RPC_RESPONSE_C2S)
                {
                    if (request_msg.TryGetValue(recv.MsgSendID, out var tcs_msg))
                    {
                        tcs_msg.TrySetResult(recv.ReadBody());
                        return;
                    }
                    else if (request_bin.TryGetValue(recv.MsgSendID, out var tcs_bin))
                    {
                        tcs_bin.TrySetResult(recv.ReadBodyBinary());
                        return;
                    }
                }
                if (listening.Count > 0)
                {
                    var invoking = new List<MessageHandler>();
                    {
                        if (listening.TryGetValue(recv.MsgRoute, out var list))
                        {
                            invoking.AddRange(list);
                        }
                        foreach (var handler in invoking)
                        {
                            handler.Invoke(recv);
                        }
                    }
                }
            }
            private void AddListening(MessageHandler handler)
            {
                var list = listening.GetOrAdd(handler.route, _ => new List<MessageHandler>(1));
                lock (list) list.Add(handler);
            }
            private void RemoveListening(MessageHandler handler)
            {
                if (listening.TryGetValue(handler.route, out var list))
                {
                    lock (list) list.Remove(handler);
                }
            }
            protected void OnDisposeListening()
            {
                listening.Clear();
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
            }
            class MessageHandler : IMessageHandler
            {
                public readonly int route;
                public readonly WSNetSession session;
                public readonly Action<object, uint> action_msg;
                public readonly Action<BinaryMessage, uint> action_bin;
                internal MessageHandler(int route, WSNetSession session, Action<object, uint> action, Action<BinaryMessage, uint> action_bin)
                {
                    this.route = route;
                    this.session = session;
                    this.action_msg = action;
                    this.action_bin = action_bin;
                }
                internal void Invoke(IRecvMessage recv)
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

            //---------------------------------------------------------------------------------
            #region Send
            internal async Task<bool> InternalSend(SendMessage send)
            {
                if (this.State == SessionState.Connected)
                {
                    if (!this.Connection.CloseReason.HasValue)
                    {
                        var sendObject = send.SendingObject;
                        try
                        {
                            var len = send.BufferLength;
                            await base.SendAsync(new Memory<byte>(send.Buffer, 0, len));
                            event_OnSent?.Invoke(this, sendObject);
                            sent_bytes += len;
                            if (TRACE_PROTOCOL) Logger.LogInformation("Sent --------> " + send);
                            return true;
                        }
                        catch (Exception err)
                        {
                            WSServer.cb_OnSessionError(this, err);
                        }
                    }
                    else
                    {
                        WSServer.log.Warn($"InternalSend: Connection[{this.SessionID}] is Closed >>>{this.Connection.CloseReason}<<<");
                    }
                }
                return false;
            }

            internal async Task<bool> InternalSend(ISerializable msg, MessageType msgType, uint sendID)
            {
                using (var send = WSServer.MessagePool.AllocSend())
                {
                    send.InitWithMessage(msgType, sendID, msg);
                    return await this.InternalSend(send);
                }
            }
            internal async Task<bool> InternalSend(BinaryMessage msg, MessageType msgType, uint sendID)
            {
                using (var send = WSServer.MessagePool.AllocSend())
                {
                    send.InitWithMessage(msgType, sendID, msg);
                    return await this.InternalSend(send);
                }
            }
            private async Task<bool> SendHandshake(ISerializable token = null)
            {
                using (var send = WSServer.MessagePool.AllocSend())
                {
                    var rsp = new SystemHandshakeAck();
                    rsp.token = token;
                    rsp.remote_info = WSServer.Config.Name;
                    rsp.heartbeat_interval_ms = WSServerFactory.HEARTBEAT_INTERVAL_SEC * 1000;
                    send.InitWithSystemMessage(rsp);
                    return await this.InternalSend(send);
                }
            }
            private async Task<bool> SendHeartbeat(SystemHeartbeat hb)
            {
                using (var send = WSServer.MessagePool.AllocSend())
                {
                    send.InitWithSystemMessage(hb);
                    return await this.InternalSend(send);
                }
            }
            private async Task<bool> SendKick(string reason)
            {
                using (var send = WSServer.MessagePool.AllocSend())
                {
                    send.InitWithSystemMessage(new SystemKick() { reason = reason });
                    return await this.InternalSend(send);
                }
            }
            #endregion

            //------------------------------------------------------------------------------
            #region Implements ISession
            public WSServer WSServer => (base.Server as WSNetServer).Server;
            private HashMap<string, object> attributes = new();
            private long sent_bytes = 0;
            private long recv_bytes = 0;
            private EndPoint endpoint;
            private int disconnecting = 0;
            //------------------------------------------------------------------------------
            string ISession.ID => this.SessionID;
            EndPoint ISession.RemoteAddress => endpoint;
            IDictionary<string, object> INetSession.Attributes => this.attributes;
            object ISession.UserTag { get; set; }
            bool INetSession.IsConnected => this.State == SessionState.Connected;
            long INetSession.TotalSentBytes => this.sent_bytes;
            long INetSession.TotalRecvBytes => this.recv_bytes;
            public void Disconnect(string reason)
            {
                DisconnectAsync(reason).Forget();
            }
            public async Task<bool> DisconnectAsync(string reason)
            {
                if (Interlocked.CompareExchange(ref disconnecting, 1, 0) != 0)
                {
                    return false;
                }
                await SendKick(reason);
                try
                {
                    await base.CloseAsync();
                }
                catch (Exception err)
                {
                    WSServer.cb_OnSessionError(this, err);
                }
                return true;
            }
            #endregion
            //------------------------------------------------------------------------------
            #region Implements Send

            public void Send(ISerializable message)
            {
                this.InternalSend(message, MessageType.MSG_NOTIFY, 0).Forget();
            }
            public void Send(BinaryMessage message)
            {
                this.InternalSend(message, MessageType.MSG_NOTIFY, 0).Forget();
            }
            public void SendResponse(ISerializable response, uint sendID)
            {
                this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID).Forget();
            }
            public void SendResponse(BinaryMessage response, uint sendID)
            {
                this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID).Forget();
            }
            public async Task<bool> SendAsync(ISerializable message)
            {
                return await this.InternalSend(message, MessageType.MSG_NOTIFY, 0);
            }
            public async Task<bool> SendAsync(BinaryMessage message)
            {
                return await this.InternalSend(message, MessageType.MSG_NOTIFY, 0);
            }
            public async Task<bool> SendResponseAsync(ISerializable response, uint sendID)
            {
                return await this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID);
            }
            public async Task<bool> SendResponseAsync(BinaryMessage response, uint sendID)
            {
                return await this.InternalSend(response, MessageType.MSG_RESPONSE_S2C, sendID);
            }
            public async Task<T> SendRequestAsync<T>(ISerializable request) where T : ISerializable
            {
                if (!WSServer.EnableRequest) throw new NotImplementedException();
                var sendID = (uint)Interlocked.Increment(ref request_indexer);
                var timeout = TimeSpan.FromMilliseconds(WSServer.RequestTimeoutMS);
                var tcs = WSServer.TcsPool.CreateTaskCompletionSource<ISerializable>(request + " : SendRequestAsync(ISerializable)", null, timeout);
                request_msg.TryAdd(sendID, tcs);
                if (!await this.InternalSend(request, MessageType.MSG_RPC_REQUEST_S2C, sendID))
                {
                    tcs.SetCanceled();
                    return default(T);
                }
                var result = await tcs.Task;
                return (T)result;
            }
            public async Task<BinaryMessage> SendRequestAsync(BinaryMessage request)
            {
                if (!WSServer.EnableRequest) throw new NotImplementedException();
                var sendID = (uint)Interlocked.Increment(ref request_indexer);
                var timeout = TimeSpan.FromMilliseconds(WSServer.RequestTimeoutMS);
                var tcs = WSServer.TcsPool.CreateTaskCompletionSource<BinaryMessage>(request.Route + " : SendRequestAsync(BinaryMessage)", null, timeout);
                request_bin.TryAdd(sendID, tcs);
                if (!await this.InternalSend(request, MessageType.MSG_RPC_REQUEST_S2C, sendID))
                {
                    tcs.SetCanceled();
                    return default(BinaryMessage);
                }
                return await tcs.Task;
            }
            public IMessageHandler HandleMessage<T>(int route, Action<T, uint> action) where T : ISerializable
            {
                var handler = new MessageHandler(route, this, (msg, sid) => { action((T)msg, sid); }, null);
                AddListening(handler);
                return handler;
            }
            public IMessageHandler HandleBinary(int route, Action<BinaryMessage, uint> action)
            {
                var handler = new MessageHandler(route, this, null, action);
                AddListening(handler);
                return handler;
            }

            #endregion
            //------------------------------------------------------------------------------
            #region Implements Event

            protected void OnDisposeEvents()
            {
                event_OnValidate = null;
                event_OnClosed = null;
                event_OnError = null;
                event_OnReceivedMessage = null;
                event_OnReceivedBinary = null;
                event_OnReceivedMessageAsync = null;
                event_OnReceivedBinaryAsync = null;
                event_OnSent = null;
            }

            private SessionValidateAsyncHandler event_OnValidate;
            private SessionClosedHandler event_OnClosed;
            private SessionErrorHandler event_OnError;
            private SessionReceivedMessageHandler event_OnReceivedMessage;
            private SessionReceivedBinaryHandler event_OnReceivedBinary;
            private SessionReceivedRequestMessageHandler event_OnReceivedMessageAsync;
            private SessionReceivedRequestBinaryHandler event_OnReceivedBinaryAsync;
            private SessionSentHandler event_OnSent;

            event SessionValidateAsyncHandler ISession.OnValidateAsync { add { event_OnValidate += value; } remove { event_OnValidate -= value; } }
            event SessionClosedHandler ISession.OnClosed { add { event_OnClosed += value; } remove { event_OnClosed -= value; } }
            event SessionErrorHandler ISession.OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
            event SessionReceivedMessageHandler ISession.OnReceivedMessage { add { event_OnReceivedMessage += value; } remove { event_OnReceivedMessage -= value; } }
            event SessionReceivedBinaryHandler ISession.OnReceivedBinary { add { event_OnReceivedBinary += value; } remove { event_OnReceivedBinary -= value; } }
            event SessionReceivedRequestMessageHandler ISession.OnRequestMessageAsync { add { event_OnReceivedMessageAsync += value; } remove { event_OnReceivedMessageAsync -= value; } }
            event SessionReceivedRequestBinaryHandler ISession.OnRequestBinaryAsync { add { event_OnReceivedBinaryAsync += value; } remove { event_OnReceivedBinaryAsync -= value; } }
            event SessionSentHandler ISession.OnSent { add { event_OnSent += value; } remove { event_OnSent -= value; } }

            #endregion

            //------------------------------------------------------------------------------

        }
    }

}
