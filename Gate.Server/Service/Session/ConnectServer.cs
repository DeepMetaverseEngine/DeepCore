//#define SHOW_MESSAGE

using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCrystal.NetServer;
using DeepCrystal.RPC;
using DeepCrystal.RPC.Protocol;
using DeepCrystal.Server;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gate.Server.Service.Session
{
    public class ConnectServer : IService
    {
        public static IOStreamPool ClientCodec { get; private set; }
        public static bool TraceRoute { get; set; } = true;
        private static bool KickOnError { get; set; }
        public string acceptor_Address { get; protected set; }
        public IServer acceptor { get; }


        public ConnectServer(ServiceStartInfo start) : base(start)
        {
            ConnectServer.KickOnError = GlobalConfig.GetAsBool("KickOnError");
            var client_codec = GateServerManager.ClientCodec;
            //var factory = GateServerManager.ClientHostFactory;
            if (ConnectServer.ClientCodec == null)
            {
                ConnectServer.ClientCodec = new IOStreamPool(client_codec);
            }
            if (!string.IsNullOrEmpty(GateServerManager.Config.ReplaceNetHost))
            {
                start.Config["Host"] = GateServerManager.Config.ReplaceNetHost;
            }
            this.acceptor_Address = $"{start.Config.Get("Host")}:{start.Config.Get("Port")}";
            this.acceptor = GateServerManager.Instance.CreateServer(new ServerConfig() {  Config = new Properties(start.Config) });
            this.acceptor.OnSessionConnected += Acceptor_OnSessionConnected;
            this.acceptor.OnSessionDisconnected += Acceptor_OnSessionDisconnected;
            this.acceptor.OnServerError += Acceptor_OnServerError;
            this.current_token = Guid.NewGuid().ToString();

            this.Provider.OnWormholeTransported += rpc_OnWormholeTransported;
        }
        protected override void OnDisposed()
        {
            sessionMap.Dispose();
        }
        protected override async Task OnStartAsync()
        {
            log.Info("[ConnectServer Start]");
            this.gate_service = await base.Provider.GetAsync(GateServerManager.ServerName.GetGateService());
            this.heartbeat_timer = base.Provider.CreateTimer(CheckHeartbeat, this,
                TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionKeepTimeout),
                TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionKeepTimeout));
        }
        protected override Task OnStopAsync()
        {
            this.heartbeat_timer.Dispose();
            this.gate_sync_timer.Dispose();
            this.acceptor.Dispose();
            return Task.FromResult(0);
        }

        [RpcHandler(typeof(SystemShutdownNotify))]
        public virtual void system_rpc_Handle(SystemShutdownNotify shutdown)
        {
            this.acceptor.StopAsync(shutdown.reason);
        }
        [RpcHandler(typeof(SystemStaticServicesStartedNotify))]
        public virtual void system_rpc_Handle(SystemStaticServicesStartedNotify shutdown)
        {
            //if (DeepCore.Log.Logger.SHOW_LOG)
            {
            log.InfoFormat("HostAddress={0}", acceptor_Address);
            log.InfoFormat("[ConnectServer Started]");
            }
            this.acceptor.StartAsync();
            this.OnGateSyncTimerTick(this);
            var intervalSec = TimeSpan.FromSeconds(GateTimerConfig.timer_sec_ConnectSyncToGateNotify);
            this.gate_sync_timer = base.Provider.CreateTimer(OnGateSyncTimerTick, this, intervalSec, intervalSec);
        }

        [RpcHandler(typeof(ConnectorBroadcastNotify))]
        public virtual void rpc_Handle(ConnectorBroadcastNotify notify)
        {
            var sockets = sessionMap.GetNotifySockets(notify.serverGroups, notify.sessions);
            var binary = ClientCodec.ToBinary(notify.notify);
            foreach (var socket in sockets)
            {
                socket.Send(binary);
            }
        }

        public virtual void rpc_OnWormholeTransported(RemoteAddress from, object message)
        {
            if (message is ConnectorBroadcastNotify notify)
            {
                var sockets = sessionMap.GetNotifySockets(notify.serverGroups, notify.sessions);
                var binary = ClientCodec.ToBinary(notify.notify);
                foreach (var socket in sockets)
                {
                    socket.Send(binary);
                }
            }
            else if (message is ISerializable toall)
            {
                acceptor.Broadcast(toall);
            }
        }

        //------------------------------------------------------------------------------------------
        #region __Gate__

        protected IRemoteService gate_service { get; private set; }
        protected IDisposable gate_sync_timer { get; private set; }
        protected string current_token = "";
        protected virtual void OnGateSyncTimerTick(object state)
        {
            Provider.Execute(async () =>
            {
                if (gate_service == null)
                {
                    this.gate_service = await base.Provider.GetAsync(GateServerManager.ServerName.GetGateService());
                }
                if (gate_service != null)
                {
                    var gate_sync = new SyncConnectToGateNotify()
                    {
                        connectServiceAddress = base.SelfAddress.FullPath,
                        connectAddress = acceptor_Address,
                        connectToken = current_token,
                        clientNumber = acceptor.SessionCount,
                        groupClientNumbers = sessionMap.GetGroupSessionNumbers(),
                    };
                    gate_service.Invoke(gate_sync);
                }
            });
        }
        public virtual bool ValidateGateToken(ClientEnterServerRequest enter)
        {
            if (enter.c2s_gate_token == current_token) { return true; }
            return false;
        }


        #endregion
        //------------------------------------------------------------------------------------------
        #region __SessionInfo__

        private IDisposable heartbeat_timer;
        private TimeSpan heartbeat_timeout = TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionKeepTimeout);

        public class SessionInfo
        {
            private IRemoteService session;
            private DateTime last_heartbeat = DateTime.Now;

            public SessionInfo(IRemoteService session)
            {
                this.session = session;
            }
            public void Refresh()
            {
                last_heartbeat = DateTime.Now;
            }
            public bool CheckHeartbeat(TimeSpan heartbeat_timeout)
            {
                if (DateTime.Now - last_heartbeat > heartbeat_timeout)
                {
                    if (session != null)
                    {
                        session.ShutdownAsync("timeout");
                        session = null;
                        return true;
                    }
                }
                return false;
            }
        }

        protected void CheckHeartbeat(object state)
        {

        }

        #endregion
        //------------------------------------------------------------------------------------------
        #region __NetSession__
        //-------------------------------------------------------------------------------------------------------------------
        public class SessionMap : Disposable
        {
            private ReaderWriterLockSlim lock_rw = new ReaderWriterLockSlim();
            private HashMap<string, HashMap<string, ViewSession>> groupSessionsMap = new HashMap<string, HashMap<string, ViewSession>>();
            private HashMap<string, ViewSession> sessionsMap = new HashMap<string, ViewSession>();

            protected override void Disposing()
            {
                lock_rw.Dispose();
            }
            internal void RegistViewSession(ViewSession session)
            {
                using (lock_rw.EnterWrite())
                {
                    var group = groupSessionsMap.GetOrAdd(session.ServerGroupID, (g) => new HashMap<string, ViewSession>());
                    group[session.ServiceID] = session;
                    sessionsMap[session.ServiceID] = session;
                }
            }
            internal void UnregistViewSession(ViewSession session)
            {
                if (session.IsValidate)
                {
                    using (lock_rw.EnterWrite())
                    {
                        if (sessionsMap.TryGetValue(session.ServiceID, out var exist))
                        {
                            if (exist == session)
                            {
                                sessionsMap.Remove(session.ServiceID);
                                var group = groupSessionsMap.GetOrAdd(session.ServerGroupID, (g) => new HashMap<string, ViewSession>());
                                group.Remove(session.ServiceID);
                            }
                        }
                    }
                }
            }
            internal HashMap<string, int> GetGroupSessionNumbers()
            {
                var ret = new HashMap<string, int>();
                using (lock_rw.EnterRead())
                {
                    foreach (var e in groupSessionsMap)
                    {
                        ret.Add(e.Key, e.Value.Count);
                    }
                }
                return ret;
            }
            internal void Clear()
            {
                using (lock_rw.EnterWrite())
                {
                    groupSessionsMap.Clear();
                    sessionsMap.Clear();
                }
            }
            public List<ViewSession> GetNotifySessions(List<string> serverGroups, List<string> accept)
            {
                using (lock_rw.EnterRead())
                {
                    var list = new List<ViewSession>(sessionsMap.Count);
                    var sessions = new HashMap<string, ViewSession>(this.sessionsMap.Count);
                    if (serverGroups != null && serverGroups.Count > 0)
                    {
                        if (serverGroups.Count == 1)
                        {
                            if (groupSessionsMap.TryGetValue(serverGroups[0], out var group))
                            {
                                sessions.PutAll(group);
                            }
                        }
                        else
                        {
                            foreach (var gid in serverGroups)
                            {
                                if (groupSessionsMap.TryGetValue(gid, out var group))
                                {
                                    sessions.PutAll(group);
                                }
                            }
                        }
                    }
                    else
                    {
                        sessions = this.sessionsMap;
                    }
                    if (accept != null && accept.Count > 0)
                    {
                        foreach (var sid in accept)
                        {
                            if (sessions.TryGetValue(sid, out var session))
                            {
                                list.Add(session);
                            }
                        }
                    }
                    else
                    {
                        list.AddRange(sessions.Values);
                    }
                    return list;
                }
            }
            public List<ISession> GetNotifySockets(List<string> serverGroups, List<string> accept)
            {
                var list = GetNotifySessions(serverGroups, accept);
                return list.ConvertAll(e => e.socket);
            }

        }

        private SessionMap sessionMap = new SessionMap();

        protected virtual async Task<IRemoteService> CreateSessionServiceAsync(string socketID, 
            ClientEnterServerRequest enter)
        {
            var cfg = new HashMap<string, string>();
            cfg["sessionID"] = socketID;
            cfg["accountID"] = enter.c2s_account;
            cfg["connectName"] = this.SelfAddress.ServiceName;
            //             cfg["channel"] = enter.c2s_clientInfo.channel;
            //             cfg["passport"] = enter.c2s_clientInfo.sdkName;
            //由参数动态创建一个SessionService，
            //每个链接创建一个SessionService用户作为角色服务和网络链接的中转。
            var addr = GateServerManager.ServerName.GetSessionService(enter.c2s_account, this.SelfAddress.ServiceNode);
            var session = await this.Provider.GetOrCreateAsync(addr, cfg);
            if (session.Config["connectName"] != this.SelfAddress.ServiceName)
            {
                //如果Session在别的进程//
                this.log.WarnFormat("已存在相同Session在其他Connect：{0}", session.Address);
                this.log.WarnFormat("关闭老的Session：{0}", session.Address);
                var newAddress = GateServerManager.ServerName.GetSessionService(enter.c2s_account,
                    this.SelfAddress.ServiceNode);
                //log.Log(string.Format("Session Replace {0} -> {1}", session.Address, newAddress));
                try { await session.ShutdownAsync("new connect"); } catch (Exception err) { log.Error(err.Message, err); }
                session = await this.Provider.CreateAsync(newAddress, cfg);
                //throw new Exception("Session Alread Exist In Node : " + ret.Address.ServiceNode);
            }
            return session;
        }

        protected virtual void Acceptor_OnSessionConnected(ISession session)
        {
            //if (DeepCore.Log.Logger.SHOW_LOG)
            {
             log.Info("Acceptor_OnSessionConnected : " + session);
            }  

            new ViewSession(this, session);
        }
        protected virtual void Acceptor_OnSessionDisconnected(ISession session)
        {
            //if (DeepCore.Log.Logger.SHOW_LOG)
            {
                log.Info("Acceptor_OnSessionDisconnected : " + session);
            }
        }
        protected virtual void Acceptor_OnServerError(IServer server, Exception err)
        {
            log.Error("Acceptor_OnServerError : " + err.Message, err);
        }
        public class ViewSession : Disposable
        {
            private static TypeAllocRecorder Alloc = new TypeAllocRecorder(nameof(ViewSession));
            public readonly ConnectServer connect;
            public readonly ISession socket;
            public Logger log { get => connect.log; }
            public string ServiceID { get => service_address.ServiceName; }
            public string ServerGroupID { get => serverGroupID; }
            public bool IsValidate { get => session_service != null; }
            protected RemoteAddress service_address;
            protected SessionService session_service;
            protected IRemoteService session_service_prx;
            protected string serverGroupID;

            public ViewSession(ConnectServer connect, ISession session)
            {
                Alloc.RecordConstructor(GetType());
                this.connect = connect;
                this.socket = session;
                this.socket.OnValidateAsync += Session_OnValidateAsync;
                this.socket.OnClosed += Session_OnClosed;
                this.socket.OnReceivedBinary += Session_OnReceivedBinary;
                this.socket.OnError += Session_OnError;
            }
            ~ViewSession()
            {
                if (!IsDisposed)
                {
                    Alloc.RecordDispose(GetType());
                }
                Alloc.RecordDestructor(GetType());
            }
            sealed protected override void RecordDisposing()
            {
                Alloc.RecordDispose(this.GetType());
            }
            protected override void Disposing()
            {
                connect.sessionMap.UnregistViewSession(this);
                var prx = session_service_prx;
                this.session_service_prx = null;
                this.session_service = null;
                if (prx != null)
                {
                    prx.Invoke(new SessionDisconnectNotify() { socketID = socket.ID });
                }
            }
            protected virtual Task<ValidateResult> Session_OnValidateAsync(ISession socket, ISerializable user)
            {
                if (user is ClientEnterServerRequest enter)
                {
                    return connect.Provider.Execute(new Func<Task<ValidateResult>>(async () =>
                    {
                        var validate = connect.ValidateGateToken(enter);
                        if (validate || (enter.c2s_session_token != null))
                        {
                            //Gate验证成功，忽略SessionToken//
                            if (validate)
                            {
                                enter.c2s_session_token = null;
                            }
                            else
                            {
                                connect.log.InfoFormat("玩家断线重连: {0}", enter.c2s_account);
                            }
                            //log.Log(enter);
                            var session = await connect.CreateSessionServiceAsync(socket.ID, enter);
                            var rsp = await session.CallAsync<LocalBindSessionResponse>(new LocalBindSessionRequest()
                            {
                                session = this,
                                enter = enter,
                            });
                            if (Response.CheckSuccess(rsp))
                            {
                                this.session_service = rsp.session;
                                this.session_service_prx = session;
                                this.service_address = session_service_prx.Address;
                                this.serverGroupID = rsp.serverGroupID;
                                connect.sessionMap.RegistViewSession(this);
                                var ret = new ClientEnterServerResponse() { s2c_session_token = rsp.sessionToken };
                                log.Info($"ClientEnterServer: {ret}");
                                return new ValidateResult(true, ret);
                            }
                            else
                            {
                                log.Error($"Connect绑定Session失败: {enter.c2s_account}");
                                return new ValidateResult(false, null);
                            }
                        }
                        else
                        {
                            return new ValidateResult(false, null);
                        }
                    }));
                }
                else
                {
                    return Task.FromResult(new ValidateResult(false, null));
                }
            }
            protected virtual void Session_OnReceivedBinary(ISession session, BinaryMessage message, uint sendID)
            {
                try
                {
                    var prx = session_service_prx;
                    var svc = session_service;
                    if (prx != null)
                    {
                        var route_codec = ConnectServer.ClientCodec.Factory.GetCodec(message.Route);
                        if (route_codec == null)
                        {
                            throw new Exception("Bad Message Route : " + message.Route);
                        }
                        //--------------------------------------------------------------------------
                        // protocol check //
                        //if (!route_codec.MessageType.IsInterfaceOf(typeof(INetProtocolC2S)))
                        //{
                        //    throw new Exception($"Client Protocol '{route_codec.MessageType.FullName}' Not Interface Of '{nameof(INetProtocolC2S)}'");
                        //}
                        //--------------------------------------------------------------------------
                        if (typeof(INetRequest).IsAssignableFrom(route_codec.MessageType))
                        {
                            if (typeof(ISessionProtocol).IsAssignableFrom(route_codec.MessageType))
                            {
                                //链接服协议//
                                prx.Call(message, do_SessionPrxCall);
                                void do_SessionPrxCall(BinaryMessage rsp, Exception err)
                                {
                                    if (rsp.HasRoute)
                                    {
                                        session.SendResponse(rsp, sendID);
                                    }
                                    else if (err != null)
                                    {
                                        connect.log.Error("Request is : " + route_codec);
                                        connect.log.Error(err.Message, err);
                                        if (KickOnError)
                                        {
                                            session.Disconnect(err.Message);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //逻辑服协议//
                                svc.call_connect_OnReceivedBinaryImmediately(route_codec, message, do_async_OnReceivedBinaryImmediately);
                                void do_async_OnReceivedBinaryImmediately(BinaryMessage rsp, Exception err)
                                {
                                    if (rsp.HasRoute)
                                    {
                                        this.SocketSend(rsp, sendID);
                                    }
                                    else if (err != null)
                                    {
                                        connect.log.Error("Request is : " + route_codec);
                                        connect.log.Error(err.Message, err);
                                        if (KickOnError)
                                        {
                                            session.Disconnect(err.Message);
                                        }
                                    }
                                }
                            }
                        }
                        else if (typeof(Notify).IsAssignableFrom(route_codec.MessageType))
                        {
                            if (typeof(ISessionProtocol).IsAssignableFrom(route_codec.MessageType))
                            {
                                //链接服协议//
                                prx.Invoke(message);
                            }
                            else
                            {
                                //逻辑服协议//
                                svc.call_connect_OnReceivedBinaryImmediately(route_codec, message);
                            }
                        }
                        else
                        {
                            throw new Exception("Bad Message Type : " + route_codec.MessageType);
                        }
                    }
                }
                catch (Exception err)
                {
                    connect.log.Error(err.Message, err);
                    if (KickOnError)
                    {
                        session.Disconnect(err.Message);
                    }
                }
            }
            protected virtual void Session_OnClosed(ISession session, string reason)
            {
                this.Dispose();
            }
            protected virtual void Session_OnError(ISession session, Exception err)
            {
                connect.log.Error("Session_OnError : " + err.Message, err);
            }
            public void SocketSend(BinaryMessage bin, uint sendID)
            {
                connect.Execute(() =>
                {
                    socket.SendResponse(bin, sendID);
                });
            }
            public void SocketSend(BinaryMessage bin)
            {
                connect.Execute(() =>
                {
                    socket.Send(bin);
                });
            }
            public void SocketSend(ISerializable bin, uint sendID)
            {
                connect.Execute(() =>
                {
                    socket.SendResponse(bin, sendID);
                });
            }
            public void SocketSend(ISerializable bin)
            {
                connect.Execute(() =>
                {
                    socket.Send(bin);
                });
            }
#if SHOW_MESSAGE
            public void Trace(string message, TypeCodec codec, uint sendID)
            {
                if (TraceRoute)
                {
                    if (codec.MessageType.FullName != "DeepMMO.Protocol.Client.ClientBattleAction"
                          && codec.MessageType.FullName != "TLProtocol.Protocol.Client.ClientSyncServerTimeRequest"
                          )
                    {
                        log.Trace(string.Format(message, codec.MessageType.FullName, sendID));
                    }
                }
            }

            public void Trace(string message, ISerializable msg, uint sendID)
            {
                if (TraceRoute)
                {
                    log.Trace(string.Format(message, msg.GetType().FullName, sendID));
                }
            }
            public void Trace(string message, BinaryMessage msg, uint sendID)
            {
                if (TraceRoute)
                {
                    var route_codec = ConnectServer.ClientCodec.Factory.GetCodec(msg.Route);
                    if (route_codec == null)
                    {
                        throw new Exception("Bad Message Route : " + msg.Route);
                    }
                    if(route_codec.MessageType.FullName != "DeepMMO.Protocol.Client.ClientBattleAction"
                            && route_codec.MessageType.FullName != "TLProtocol.Protocol.Client.ClientSyncServerTimeResponse"
                            )
                    {
                        Trace(message, route_codec, sendID);
                    }

                }
            }
#endif
        }

        #endregion


    }

    //--------------------------------------------------------------------------------------------------------------------------------------------


}
