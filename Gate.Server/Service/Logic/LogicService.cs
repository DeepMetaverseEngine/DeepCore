using DeepCore;
using DeepCore.Protocol;
using DeepCore.Statistics;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using Gate.Data;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using Gate.Server.Service.Logic.Module;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Gate.Server.Service.Logic
{
    public partial class LogicService : IService
    {
        public static int SAVE_EXPECT_TIME_LIMIT = 200;
        public static int LOAD_EXPECT_TIME_LIMIT = 200;
        public static TimeStatisticsRecoder Statistics { get; private set; } =
            new TimeStatisticsRecoder("LogicStatistics");

        //-----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 账号ID.
        /// </summary>
        public string AccountID { get; }
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string ServerID { get; }
        public string ServerGroupID { get; }
        public string SessionName { get; }
        public string SessionNode { get; }
        public string RoleID { get; }
        public string RoleDigitID { get => RoleData.digitID; }
        public IMappingAdapter DBAdapter { get; }
        //-----------------------------------------------------------------------------------------------------------
        public IRemoteService Session { get; private set; }
        //public LanguageManager Language { get; private set; }
        public GateClientInfo ClientInfo { get; private set; }
        public bool Disconnect { get; private set; }
        public bool IsClientEntered { get; private set; }
        //-----------------------------------------------------------------------------------------------------------
        public ServerRoleData RoleData { get { return RoleMapping.Data; } }
        public MappingReference<ServerRoleData> RoleMapping { get; protected set; }
        public MappingReference<RoleSnap> SnapMapping { get; protected set; }
        //-----------------------------------------------------------------------------------------------------------
        public override ServiceProperties Properties
        {
            get
            {
                var ret = base.Properties;
                ret.IsConcurrent = false;
                return ret;
            }
        }
        public LogicService(ServiceStartInfo start) : base(start)
        {
            this.Disconnect = false;
            this.ClientInfo = GateClientInfo.LoadFrom(start.Config);
            this.SessionName = ClientInfo.SessionName;
            this.SessionNode = ClientInfo.SessionNode.ToString();
            this.AccountID = ClientInfo.AccountID.ToString();
            this.ServerID = ClientInfo.ServerID.ToString();
            this.RoleID = ClientInfo.RoleID.ToString();
            this.ServerGroupID = ClientInfo.ServerGroupID.ToString();
            this.DBAdapter = ORMFactory.Instance.DefaultAdapter;

            this.OnInitModules();
        }
        protected override async Task OnStartAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                this.Session = await base.Provider.GetAsync(new RemoteAddress(SessionName, SessionNode));
                //定期存数据.
                int interval = Math.Max(5, GateTimerConfig.timer_minute_LogicSaveDataTimer);
                AutoDispose(Provider.CreateTimer(OnFlushDataTickAsync, this, TimeSpan.FromMinutes(interval)));
                var rd = await LoadRoleDataAsync();
                //                 if (GateServerManager.Language.TryGetLanguage(rd.local_code, out var lang))
                //                 {
                //                     this.Language = lang;
                //                 }
                //                 else
                //                 {
                //                     log.Error("LanguageManager Not Exist : " + rd.local_code);
                //                     this.Language = new LanguageManager();
                //                 }
            }
            catch (Exception hzdsb)
            {
                hzdsb.PrintStackTrace();
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > LOAD_EXPECT_TIME_LIMIT)
                {
                    log.Warn("LogicService : OnStartAsync Use Time = " + stopwatch.Elapsed);
                }
            }
        }

        protected override async Task OnStopAsync()
        {
            IsClientEntered = false;
            this.RoleMapping.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_OFFLINE);
            this.SnapMapping.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_OFFLINE);
            if (StopInfo.Event != ServiceStopInfo.ShutdownEvent.START_ERROR)
            {
                await OnModulesSaveDataAsync();
            }
        }
        protected override void OnDisposed()
        {
            this.Session = null;
            this.OnClearModules();
            this.OnEventsDesposed();
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region Events
        protected void OnEventsDesposed()
        {
            event_OnSessionDisconnect = null;
            event_OnSessionReconnect = null;
            event_OnBeforeSaveData = null;
            event_OnEndSaveData = null;
            event_OnClientEntered = null;
        }
        public event Action OnSessionDisconnect { add { event_OnSessionDisconnect += value; } remove { event_OnSessionDisconnect -= value; } }
        public event Action OnSessionReconnect { add { event_OnSessionReconnect += value; } remove { event_OnSessionReconnect -= value; } }
        public event Action OnBeforeSaveData { add { event_OnBeforeSaveData += value; } remove { event_OnBeforeSaveData -= value; } }
        public event Action OnEndSaveData { add { event_OnEndSaveData += value; } remove { event_OnEndSaveData -= value; } }
        public event Action OnClientEntered { add { event_OnClientEntered += value; } remove { event_OnClientEntered -= value; } }
        private Action event_OnSessionDisconnect;
        private Action event_OnSessionReconnect;
        private Action event_OnBeforeSaveData;
        private Action event_OnEndSaveData;
        private Action event_OnClientEntered;
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region Data

        protected virtual async Task<ServerRoleData> LoadRoleDataAsync()
        {
            var role = AutoDispose(GateServerManager.Mapping.CreateRoleDataMapping(this.RoleID, this));
            var snap = AutoDispose(GateServerManager.Mapping.CreateRoleSnapMapping(this.RoleID, this));
            await role.LoadDataAsync();
            await snap.LoadDataAsync();
            this.RoleMapping = role;
            this.SnapMapping = snap;
            var trans = DBAdapter.CreateExecutableObjectTransaction(this);
            try
            {
                role.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_ONLINE);
                role.SetField(nameof(ServerRoleData.last_login_time), DateTime.Now);
                role.BatchFlush(trans);
                snap.SetField(nameof(RoleSnap.last_login_time), role.Data.last_login_time);
                snap.SetField(nameof(RoleSnap.onlineState), RoleState.STATE_ONLINE);
                snap.SetField(nameof(RoleSnap.session_name), SessionName);
                snap.BatchFlush(trans);
            }
            finally
            {
                await trans.ExecuteAsync();
            }
            return RoleData;
        }

        private Task OnFlushDataTickAsync(object state)
        {
            return this.OnModulesSaveDataAsync();
        }
        protected virtual void OnSaveRoleData(IObjectTransaction trans)
        {
            this.RoleMapping.BatchFlush(trans);
            var data = RoleData;
            this.SnapMapping.SetField(nameof(RoleSnap.name), data.name);
            this.SnapMapping.SetField(nameof(RoleSnap.digitID), data.digitID);
            this.SnapMapping.SetField(nameof(RoleSnap.uuid), data.uuid);
            this.SnapMapping.SetField(nameof(RoleSnap.account_uuid), data.account_uuid);
            this.SnapMapping.SetField(nameof(RoleSnap.role_template_id), data.role_template_id);
            //this.SnapMapping.SetField(nameof(RoleSnap.unit_template_id), data.unit_template_id);
            this.SnapMapping.SetField(nameof(RoleSnap.server_id), data.server_id);
            this.SnapMapping.SetField(nameof(RoleSnap.level), data.Level);
            this.SnapMapping.BatchFlush(trans);
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region Modules

        public _dummy DummyModule { get; protected set; }
#if false
        public ChannelModule ChannelModule { get; protected set; }
#endif
        protected virtual void OnInitModules()
        {
            this.DummyModule = new _dummy(this);
#if false
            this.ChannelModule = new ChannelModule(this);
#endif
        }
        protected virtual void OnClearModules()
        {
            this.DummyModule = null;
#if false
            this.ChannelModule = null;
#endif
        }
        private async Task OnModulesSaveDataAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                event_OnBeforeSaveData?.Invoke();
                {
                    var watch_exe = CUtils.TickTimeMS;
                    var trans = DBAdapter.CreateExecutableObjectTransaction(this);
                    try
                    {
                        OnSaveRoleData(trans);
                        ForEachModules<ILogicModule>(module =>
                        {
                            var watch = CUtils.TickTimeMS;
                            try
                            {
                                module.OnSaveData(trans);
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                            finally
                            {
                                Statistics.LogTime($"{module.GetType().Name} : OnSaveData", CUtils.TickTimeMS - watch);
                            }
                        });
                        await trans.ExecuteAsync().ContinueWith(t =>
                        {
                            Statistics.LogTime($"{GetType().Name} : OnModulesSaveDataAsync", CUtils.TickTimeMS - watch_exe);
                        });
                    }
                    finally
                    {
                        await trans.DisposeAsync();
                    }
                }
            }
            finally
            {
                event_OnEndSaveData?.Invoke();
                stopwatch.Stop();
                {
                    if (stopwatch.ElapsedMilliseconds > SAVE_EXPECT_TIME_LIMIT)
                    {
                        log.Warn(" Warn LogicService : OnModulesSaveDataAsync Flush Time = " + stopwatch.Elapsed);
                    }
                    else
                    {
                        log.Debug("Debug LogicService : OnModulesSaveDataAsync Flush Time = " + stopwatch.Elapsed);
                    }
                }
            }
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 玩家断开连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionDisconnectNotify))]
        public virtual async Task rpc_session_Handle(SessionDisconnectNotify disconnect)
        {
            disconnect.roleID = this.RoleID;
            this.Disconnect = true;
            await ForEachModulesAsync<ILogicModule>(async module =>
            {
                try
                {
                    await module.OnSessionDisconnectAsync(disconnect);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            });
            this.event_OnSessionDisconnect?.Invoke();
        }
        /// <summary>
        /// 玩家重新连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionReconnectNotify))]
        public virtual async Task rpc_session_Handle(SessionReconnectNotify reconnect)
        {
            reconnect.roleID = this.RoleID;
            this.ClientInfo = GateClientInfo.LoadFrom(new DeepCore.Properties(reconnect.config));
            this.Disconnect = false;
            await ForEachModulesAsync<ILogicModule>(async module =>
            {
                try
                {
                    await module.OnSessionReconnectAsync(reconnect);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            });
            this.event_OnSessionReconnect?.Invoke();
        }

        [RpcHandler(typeof(ClientEnterGameRequest), typeof(ClientEnterGameResponse))]
        public void rpc_client_Handle(ClientEnterGameRequest enter, OnRpcReturn<ClientEnterGameResponse> cb)
        {
            cb(new ClientEnterGameResponse() { s2c_role = RoleData });
            IsClientEntered = true;
            event_OnClientEntered?.Invoke();
            Provider.Execute(async () =>
            {
                await ForEachModulesAsync<ILogicModule>(async module =>
                {
                    try
                    {
                        await module.OnClientEnterGameAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                });
            });
        }

        /// <summary>
        /// 测试客户端Ping，Pong
        /// </summary>
        [RpcHandler(typeof(ClientPing), typeof(ClientPong))]
        public virtual void rpc_client_Handle(ClientPing ping, OnRpcReturn<ClientPong> cb)
        {
#if TEST
                session.Invoke(new LogicTimeNotify() { index = 0, time = ping.time });
                session.Invoke(new LogicTimeNotify() { index = 1, time = ping.time });
                cb(new ClientPong()
                {
                    s2c_code = (ping.time.Millisecond % 2 == 0) ? Response.CODE_OK : Response.CODE_ERROR,
                    s2c_msg = DateTime.Now.ToString(),
                    time = ping.time
                });
                session.Invoke(new LogicTimeNotify() { index = 2, time = ping.time });
                session.Invoke(new LogicTimeNotify() { index = 3, time = ping.time });
#else
            cb(new ClientPong() { s2c_code = Response.CODE_OK, time = ping.time });
#endif
        }
        // 
        //         [RpcHandler(typeof(ServerGameEventNotify))]
        //         public virtual void rpc_event_notify(ServerGameEventNotify ntf)
        //         {
        //             if (EventMgr != null && (string.IsNullOrEmpty(ntf.ServerGroupID) || ntf.ServerGroupID == serverGroupID))
        //             {
        //                 EventMgr.OnReceiveMessage(EventMessage.FromBytes(ntf.EventMessageData));
        //             }
        //         }
        // 
        //         [RpcHandler(typeof(ClientGameEventNotify))]
        //         public virtual void client_rpc_notify(ClientGameEventNotify ntf)
        //         {
        //             var msg = EventMessage.FromBytes(ntf.EventMessageData);
        //             if (msg is NamedEventMessage nameMsg)
        //             {
        //                 nameMsg.From = EventMgr?.Address;
        //             }
        //             var address = EventManagerAddress.Parse(msg.From);
        //             address = new EventManagerAddress("Client", address.UUID);
        //             msg.From = address.Address;
        //             EventManager.MessageBroker.Publish(ntf.To, EventMgr, msg);
        //         }


        //---------------------------------------------------------------------------------------------------
    }

}
