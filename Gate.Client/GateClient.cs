using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCore.Protocol;
using DeepCore.Threading;
using Gate.Client.Modules;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Client
{
    public partial class GateClient : Disposable
    {
        protected internal readonly Logger log;
        protected internal readonly IExternalizableFactory codec;
        protected internal readonly ClientInfo clientInfo;
        protected internal readonly INetClient gate_session;
        protected internal readonly INetClient game_session;
        private readonly TypeCodec pong_codec;
        private readonly TypeCodec ping_codec;
        private readonly MessageActionQueue<GateClient> tasks;
        private readonly TimeTaskQueue timer_tasks;
        public TimeSpan ConnectTimeOut { get; set; } = TimeSpan.FromSeconds(30);

        public IExternalizableFactory NetCodec { get { return codec; } }
        public INetClient GateSession { get { return gate_session; } }
        public INetClient GameSession { get { return game_session; } }
        public int CurrentPing { get; private set; }
        public SingleThreadCollectionPool ObjectPool { get; } = new SingleThreadCollectionPool();
        public MessageActionQueue<GateClient> TaskQueue { get { return tasks; } }
        public Logger Log => log;
        /// <summary>
        /// 曾经链接到游戏后掉线
        /// </summary>
        public bool IsGameDisconnected
        {
            get { return (this.GameSession.IsConnected == false && this.last_EnterGameResponse != null); }
        }

        public GateClient()
        {
            this.CurrentPing = 0;

            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.codec = GateClientManager.Instance.ClientCodec;
            this.clientInfo = GateClientManager.Instance.ClientInfo;
            this.ping_codec = codec.GetCodec(typeof(ClientPing));
            this.pong_codec = codec.GetCodec(typeof(ClientPong));

            this.tasks = new MessageActionQueue<GateClient>();
            this.timer_tasks = new TimeTaskQueue(ObjectPool);

            this.gate_session = GateClientManager.Instance.CreateNetClient("GATE");
            this.gate_session.Listen<ClientEnterGateInQueueNotify>(Gate_OnClientEnterGateInQueueNotify);
            this.gate_session.NetError += gate_session_AsyncError;

            this.game_session = GateClientManager.Instance.CreateNetClient("GAME");
            this.game_session.NetHandleResponseImmediately += game_session_AsyncHandleResponseImmediately;
            this.game_session.NetHandleBodyImmediately += game_session_AsyncHandleBodyImmediately;
            this.game_session.OnRequestEnd += game_session_OnRequestEndRead;
            this.game_session.OnRequestEnd += game_session_OnRequestEnd;
            this.game_session.NetError += game_session_AsyncError;
            this.game_session.OnConnected += game_session_OnConnected;
            this.game_session.OnDisconnected += game_session_OnDisconnected;

            this.OnInitModules();
        }

        public void Disconnect()
        {
            this.gate_session.Disconnect();
            this.game_session.Disconnect();
        }
        protected override void Disposing()
        {

            this.gate_session.Disconnect();
            this.gate_session.Dispose();

            this.game_session.Disconnect();
            this.game_session.Dispose();
            this.OnGameConnected = null;
            this.OnGameDisconnected = null;

            this.tasks.Dispose();
            this.timer_tasks.Dispose();

            this.OnError = null;
            this.OnGameConnected = null;
            this.OnGameEntered = null;
            this.OnGameDisconnected = null;

            foreach (IDisposable module in mModules)
            {
                try
                {
                    module.Dispose();
                }
                catch (Exception e) { e.PrintStackTrace(); }
            }
            this.mModules.Clear();

            this.ObjectPool.Dispose();
        }
        //----------------------------------------------------------------------------------------------------------
        #region Protocol
        private void game_session_AsyncHandleResponseImmediately(IRecvMessage protocol)
        {
            if (protocol.MsgRoute == pong_codec.MessageID)
            {
                var pong = protocol.ReadBody() as ClientPong;
                this.CurrentPing = (int)(DateTime.Now - pong.time).TotalMilliseconds;
            }
        }
        private void game_session_AsyncHandleBodyImmediately(object message)
        {
            //             if (message is Response response)
            //             {
            //                 response.EndRead();
            //             }
        }
        private void game_session_OnRequestEndRead(string route, Exception error, ISerializable response, object option)
        {
            if (response is Response rsp)
            {
                rsp.EndRead();
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region Modules

        public _dummy DummyModule { get; protected set; }
#if false
        public ChannelModule ChannelModule { get; protected set; }
#endif
        protected virtual void OnInitModules()
        {
            this.DummyModule = AddModule(new _dummy(this));
#if false
            this.ChannelModule = AddModule(new ChannelModule(this));
#endif
        }

        private List<GateClientModule> mModules = new List<GateClientModule>();
        public M AddModule<M>(M m) where M : GateClientModule
        {
            if (mModules.Contains(m)) throw new Exception("Module Already Exist!");
            mModules.Add(m);
            return m;
        }

        public M GetModel<M>(Predicate<M> predicate = null) where M : GateClientModule
        {
            foreach (var m in mModules)
            {
                if (m is M model && (predicate == null || predicate.Invoke(model)))
                {
                    return model;
                }
            }
            return null;
        }
        public bool TryGetModel<M>(out M ret, Predicate<M> predicate = null) where M : GateClientModule
        {
            foreach (var m in mModules)
            {
                if (m is M model && (predicate == null || predicate.Invoke(model)))
                {
                    ret = model;
                    return true;
                }
            }
            ret = null;
            return false;
        }

        public void RemoveModule(GateClientModule module)
        {
            mModules.Remove(module);
            module.Dispose();
        }
        public void ForEachModules(Action<GateClientModule> action)
        {
            foreach (var m in mModules)
            {
                action(m);
            }
        }
        public void ForEachModules<M>(Action<M> action) where M : GateClientModule
        {
            foreach (var m in mModules)
            {
                if (m is M model)
                {
                    action(model);
                }
            }
        }

        private void modules_OnGameClientEntered(ClientEnterGameResponse enter)
        {
            foreach (var module in mModules)
            {
                try
                {
                    module.OnEnterGame(enter);
                }
                catch (Exception ex)
                {
                    DoError(ex);
                }
            }
        }
        private void modules_OnGameClientDisconnected(CloseReason reason, string err)
        {
            foreach (var module in mModules)
            {
                try
                {
                    module.OnGameClientDisconnected(reason);
                }
                catch (Exception ex)
                {
                    DoError(ex);
                }
            }
        }

        private NotifyInvoker notifyHandlers = new NotifyInvoker();




        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region Update

        public virtual void QueueTask(Action action)
        {
            tasks.Enqueue(action);
        }
        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler)
        {
            return timer_tasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler)
        {
            return timer_tasks.AddTimeDelayMS(delayMS, handler);
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler)
        {
            return timer_tasks.AddTimePeriodicMS(intervalMS, handler);
        }


        public virtual void Update(float intervalMS)
        {
            foreach (var module in mModules)
            {
                module.BeginUpdate(intervalMS);
            }
            tasks.ProcessMessages(this);
            timer_tasks.Update(intervalMS);
            if (gate_session != null)
            {
                gate_session.Update();
            }
            if (game_session != null)
            {
                game_session.Update();
            }
            foreach (var module in mModules)
            {
                module.Update(intervalMS);
            }
        }

        private void do_tasks(Action act)
        {
            try { act.Invoke(); } catch (Exception err) { DoError(err); }
        }
        private void game_session_AsyncError(Exception obj)
        {
            log.Error(obj);
            QueueTask(() => { DoError(obj); });
        }
        private void gate_session_AsyncError(Exception obj)
        {
            log.Error(obj);
            QueueTask(() => { DoError(obj); });
        }
        protected virtual void DoError(Exception err)
        {
            OnError?.Invoke(err);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region Events

        public event Action<Exception> OnError;

        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region ConnectorSession
        public ClientEnterServerRequest last_EnterServerRequest { get; private set; }
        public ClientEnterServerResponse last_EnterServerResponse { get; private set; }
        public ClientEnterGameResponse last_EnterGameResponse { get; private set; }

        public ServerRoleData LastRoleData { get => last_EnterGameResponse?.s2c_role; }
        public string RoleName { get => LastRoleData?.name; }
        public string RoleUUID { get => LastRoleData?.uuid; }

        public Task<ClientEnterServerResponse> ConnectGateAndServerAsync(ServerInfo server, string account, string token)
        {
            var tcs = new TaskCompletionSource<ClientEnterServerResponse>();
            ConnectGateAndServer(server, account, token, (err, rsp) =>
            {
                if (err != null) tcs.TrySetException(err);
                else tcs.TrySetResult(rsp);
            });
            return tcs.Task;
        }
        public void ConnectGateAndServer(ServerInfo server, string account, string token, Action<Exception, ClientEnterServerResponse> callback)
        {
            ConnectGate(server, account, token, (err, rsp1) =>
            {
                if (err != null)
                {
                    callback(err, null);
                }
                else if (rsp1.s2c_code == ClientEnterGateResponse.CODE_OK)
                {
                    ConnectGameServer((err2, rsp2) =>
                    {
                        callback(err2, rsp2);
                    });
                }
                else
                {
                    callback(null, new ClientEnterServerResponse()
                    {
                        s2c_code = rsp1.s2c_code,
                        s2c_msg = rsp1.s2c_msg,
                        InnerResponse = rsp1,
                    });
                }
            });
        }
        public Task<ClientEnterServerResponse> ConnectGameServerAsync()
        {
            var tcs = new TaskCompletionSource<ClientEnterServerResponse>();
            ConnectGameServer((err, rsp) =>
            {
                if (err != null) tcs.TrySetException(err);
                else tcs.TrySetResult(rsp);
            });
            return tcs.Task;
        }
        public void ConnectGameServer(Action<Exception, ClientEnterServerResponse> callback)
        {
            if (last_EnterGateResponse != null && last_EnterGateResponse.IsSuccess)
            {
                ConnectGameServer(last_EnterGateResponse, callback);
            }
            else
            {
                callback(null, new ClientEnterServerResponse()
                {
                    s2c_code = Response.CODE_ERROR,
                    s2c_msg = "not enter gate",
                });
            }
        }
        public virtual void ConnectGameServer(ClientEnterGateResponse gate, Action<Exception, ClientEnterServerResponse> callback)
        {
            this.last_EnterServerRequest = new ClientEnterServerRequest()
            {
                c2s_account = gate.s2c_accountUUID,
                c2s_gate_token = gate.s2c_connectToken,
                c2s_login_token = gate.s2c_lastLoginToken,
                c2s_session_token = last_EnterServerResponse != null ? last_EnterServerResponse.s2c_session_token : null,
                c2s_time = DateTime.Now,
            };
            if (this.game_session.IsConnected)
            {
                this.game_session.Disconnect();
            }
            var address = gate.s2c_connectAddress;
            if (IPUtil.TryParseHostPort(address, out var game_host, out var game_port))
            {
                if (game_host == "0.0.0.0")
                {
                    if (IPUtil.TryParseHostPort(last_GateAddress, out var gate_host, out var gate_port))
                    {
                        game_host = gate_host;
                        address = $"{game_host}:{game_port}";
                    }
                }
            }
            //             var port = gate.s2c_connectPort;
            //             if (host == "0.0.0.0")
            //             {
            //                 host = last_GateHost;
            //             }
            //             var cts = new CancellationTokenSource();
            //             cts.CancelAfter(ConnectTimeOut); // 设置超时时间
            //             Task.WhenAny(
            //                 game_session.ConnectAsync(host, port, ConnectTimeOut, last_EnterServerRequest),
            //                 Task.Delay(ConnectTimeOut, cts.Token)).ContinueWith(t =>
            //                 {
            //                     if (t.Exception != null)
            //                     {
            //                         QueueTask(() =>
            //                         {
            //                             callback(t.Exception, null);
            //                         });
            //                     }
            //                     else if (t.Result is Task<ISerializable> rsp)
            //                     {
            //                         QueueTask(() =>
            //                         {
            //                             callback(rsp.Exception, rsp.Result as ClientEnterServerResponse);
            //                         });
            //                     }
            //                     else
            //                     {
            //                         QueueTask(() =>
            //                         {
            //                             callback(new Exception("Timeout"), null);
            //                         });
            //                     }
            //                 });
            this.game_session.Connect(address, ConnectTimeOut, last_EnterServerRequest, (err, response) =>
            {
                //   game_client.Request<ClientPong>(new ClientPing(), (s, a) => { });
                callback(err, response as ClientEnterServerResponse);
            });
        }
        private void game_session_OnConnected(ISerializable token)
        {
            this.last_EnterServerResponse = token as ClientEnterServerResponse;
            if (this.OnGameConnected != null) this.OnGameConnected(game_session, last_EnterServerResponse);
        }
        private void game_session_OnDisconnected(CloseReason reason, string err)
        {
            modules_OnGameClientDisconnected(reason, err);
            if (this.OnGameDisconnected != null) this.OnGameDisconnected(game_session, reason);
        }
        private void game_session_OnRequestEnd(string route, Exception error, ISerializable response, object option)
        {
            if (response is ClientEnterGameResponse enter && enter.IsSuccess)
            {
                last_EnterGameResponse = response as ClientEnterGameResponse;
                QueueTask(() =>
                {
                    modules_OnGameClientEntered(last_EnterGameResponse);
                    if (this.OnGameEntered != null) this.OnGameEntered(game_session, last_EnterGameResponse);
                });
            }
        }

        public delegate void OnGameConnectedHandler(INetClient client, ClientEnterServerResponse response);
        public delegate void OnGameEnteredHandler(INetClient client, ClientEnterGameResponse response);
        public delegate void OnGameDisconnectedHandler(INetClient client, CloseReason reason);

        public event OnGameConnectedHandler OnGameConnected;
        public event OnGameEnteredHandler OnGameEntered;
        public event OnGameDisconnectedHandler OnGameDisconnected;
        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region GateSession

        public ClientEnterGateResponse last_EnterGateResponse { get; private set; }
        public ClientEnterGateRequest last_EnterGateRequest { get; private set; }
        public ServerInfo last_ServerInfo { get; private set; }
        public string last_GateAddress { get; private set; }

        public string AccountName
        {
            get { return last_EnterGateRequest != null ? last_EnterGateRequest.c2s_account : ""; }
        }
        private void Gate_OnClientEnterGateInQueueNotify(ClientEnterGateInQueueNotify notify)
        {
            event_OnGateQueueUpdated?.Invoke(notify);
            if (notify.IsEnetered)
            {
                this.last_EnterGateResponse.s2c_code = ClientEnterGateResponse.CODE_OK;
                this.last_EnterGateResponse.s2c_connectAddress = notify.s2c_connectAddress;
                this.last_EnterGateResponse.s2c_connectToken = notify.s2c_connectToken;
                this.last_EnterGateResponse.s2c_lastLoginToken = notify.s2c_lastLoginToken;
                this.event_OnGateEntered?.Invoke(last_EnterGateResponse);
                if (this.gate_session.IsConnected) gate_session.Disconnect();
            }
        }
        public Task<ClientEnterGateResponse> ConnectGateAsync(ServerInfo server, string account, string token)
        {
            var tcs = new TaskCompletionSource<ClientEnterGateResponse>();
            try
            {
                ConnectGate(server, account, token, (err, rsp) =>
                {
                    if (err != null) tcs.TrySetException(err);
                    else tcs.TrySetResult(rsp);
                });
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                return tcs.Task;
            }
            return tcs.Task;
        }


        public virtual void ConnectGate(ServerInfo server, string account, string token, Action<Exception, ClientEnterGateResponse> callback)
        {
            last_ServerInfo = server;
            //if (IPUtil.TryParseHostPort(server.address, out var host, out var port))
            {
                last_GateAddress = server.address;
                this.last_EnterGateRequest = GateClientManager.Instance.Passport.SignPassportData(new ClientEnterGateRequest()
                {
                    c2s_account = account,
                    c2s_token = token,
                    c2s_clientInfo = clientInfo,
                    c2s_serverID = server.id,
                });
                if (this.gate_session.IsConnected)
                {
                    this.gate_session.Disconnect();
                }
                this.gate_session.Connect(server.address, ConnectTimeOut, last_EnterGateRequest, (err, response) =>
                {
                    if (err != null)
                    {
                        callback(err, null);
                        return;
                    }
                    var rsp = response as ClientEnterGateResponse;
                    this.last_EnterGateResponse = rsp;
                    if (rsp == null)
                    {
                        gate_session.Disconnect();
                        callback(new Exception("Null Response"), rsp);
                    }               
                    else if (rsp.s2c_code == ClientEnterGateResponse.CODE_OK_IN_QUEUE)
                    {
                        callback(err, rsp);
                    }
                    else
                    {
                        gate_session.Disconnect();
                        callback(err, rsp);
                        event_OnGateEntered?.Invoke(rsp);
                    }
                });
            }
            //             else
            //             {
            //                 callback(new Exception("Unknow Host"), null);
            //             }
        }

        private Action<ClientEnterGateResponse> event_OnGateEntered;
        private Action<ClientEnterGateInQueueNotify> event_OnGateQueueUpdated;
        public event Action<ClientEnterGateResponse> OnGateEntered { add { event_OnGateEntered += value; } remove { event_OnGateEntered -= value; } }
        public event Action<ClientEnterGateInQueueNotify> OnGateQueueUpdated { add { event_OnGateQueueUpdated += value; } remove { event_OnGateQueueUpdated -= value; } }

        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region MockLogin
        /// <summary>
        /// 模拟登陆并且获取角色列表
        /// </summary>
        /// <param name="account"></param>
        /// <param name="pswd"></param>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public virtual async Task<ClientGetRolesResponse> MockLoginGetRolesAsync(string account, string pswd, string serverID)
        {
            var server = GateClientManager.Instance.ServerList.GetServer(serverID);
            if (server != null)
            {
                log.Info("ServerID : " + server);
                //链接Gate获取链接信息
                var conn = await ConnectGateAndServerAsync(server, account, pswd);
                log.Info("ConnectGateAndServer : " + conn);
                //获取角色列表
                var roleList = await GameSession.RequestAsync<ClientGetRolesResponse>(new ClientGetRolesRequest() { });
                log.Info("ClientGetRoles : " + roleList);
                if (!Response.CheckSuccess(roleList))
                {
                    log.Error(roleList);
                    return null;
                }
                return roleList;
            }
            else
            {
                log.Error("ServerID not exist : " + serverID);
            }
            return null;
        }
        /// <summary>
        /// 模拟直接进入游戏，如果没有角色则创建一个角色再进入游戏
        /// </summary>
        /// <param name="account"></param>
        /// <param name="pswd"></param>
        /// <param name="roleName"></param>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public virtual async Task<ClientEnterGameResponse> MockEnterGameAsync(string account, string pswd, string roleName, string serverID)
        {
            //获取角色列表
            var roleList = await MockLoginGetRolesAsync(account, pswd, serverID);
            log.Info("ClientGetRoles : " + roleList);
            if (!Response.CheckSuccess(roleList))
            {
                log.Error(roleList);
                return null;
            }
            if (roleList.s2c_snaps == null || roleList.s2c_snaps.Count == 0)
            {
                //模拟创建角色
                var create = await GameSession.RequestAsync<ClientCreateRoleResponse>(
                    new ClientCreateRoleRequest() { c2s_name = roleName, });
                log.Info("ClientCreateRole : " + create);
                if (!Response.CheckSuccess(create))
                {
                    log.Error(create);
                    return null;
                }
                //获取角色列表
                roleList = await GameSession.RequestAsync<ClientGetRolesResponse>(
                    new ClientGetRolesRequest() { });
                log.Info("ClientGetRoles : " + roleList);
                if (!Response.CheckSuccess(roleList))
                {
                    log.Error(roleList);
                    return null;
                }
            }
            var role = roleList.s2c_snaps[0];
            //进入游戏
            var enter = await GameSession.RequestAsync<ClientEnterGameResponse>(
                new ClientEnterGameRequest()
                {
                    c2s_roleUUID = role.uuid
                });
            log.Info("ClientEnterGame : " + enter);
            if (!Response.CheckSuccess(enter))
            {
                log.Error(enter);
            }
            return enter;
            // enter default zone
            //client.GameSession.Notify(new ClientEnterZoneRequest() { });
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public abstract class GateClientModule : Disposable
    {
        private NotifyInvoker listeners = new NotifyInvoker();
        public GateClient Client { get; private set; }
        public INetClient GameSession { get; private set; }
        public Logger log { get => Client.log; }
        protected GateClientModule(GateClient client)
        {
            this.Client = client;
            this.GameSession = client.GameSession;
            this.listeners.Regist(this, client.GameSession);
        }
        sealed protected override void Disposing()
        {
            this.listeners.Dispose();
            this.OnDisposing();
        }
        protected virtual void OnDisposing() { }
        internal protected virtual void OnEnterGame(ClientEnterGameResponse enter) { }
        internal protected virtual void OnGameClientDisconnected(DeepCore.NetClient.CloseReason reason) { }
        internal protected virtual void BeginUpdate(float intervalMS) { }
        internal protected virtual void Update(float intervalMS) { }
    }

    public abstract class GateClientModule<T> : GateClientModule where T : GateClient
    {
        new public T Client { get => base.Client as T; }
        protected GateClientModule(T client) : base(client)
        {
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
}
