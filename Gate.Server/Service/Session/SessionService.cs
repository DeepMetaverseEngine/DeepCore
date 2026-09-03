using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Protocol;
using DeepCrystal.ORM.Generic;
using DeepCrystal.ORM.Query;
using DeepCrystal.RPC;
using DeepCrystal.RPC.Protocol;
using Gate.Data;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Threading.Tasks;

namespace Gate.Server.Service.Session
{
    /// <summary>
    /// 单个链接服务
    /// </summary>
    public partial class SessionService : IService
    {
        public readonly string accountID;
        public bool AllowAutoKickExistLogic = true;
        protected ConnectServer.ViewSession session { get; private set; }
        protected ClientEnterServerRequest enter { get; private set; }
        protected ClientEnterGameRequest enter_game { get; private set; }

        protected IDisposable heartbeat_timer;
        protected string sessionToken;
        protected MappingReference<AccountData> accountSave;
        protected QueryMappingReference<RoleSnap> queryRoleSnap;
        protected bool mDisconnected = true;


        public override ServiceProperties Properties
        {
            get
            {
                var ret = base.Properties;
                ret.IsConcurrent = false;
                ret.IgnoreRequestError = true;
                ret.IgnoreResponseError = true;
                return ret;
            }
        }

        public SessionService(ServiceStartInfo start) : base(start)
        {
            this.accountID = start.Config["accountID"].ToString();
            this.Provider.OnWormholeTransported += this.rpc_AnyWormholeTransported;
        }

        protected override void OnDisposed()
        {
            this.accountSave.Dispose();
            this.queryRoleSnap.Dispose();
            this.session = null;
            this.enter = null;
            this.remote_logic_service = null;
            this.enter_game = null;
            this.heartbeat_timer = null;
            this.sessionToken = null;
            this.accountSave = null;
            this.queryRoleSnap = null;
        }

        protected override async Task OnStartAsync()
        {
            this.accountSave = new MappingReference<AccountData>(
                GateServerManager.Mapping.TYPE_ACCOUNT_DATA + accountID, this);
            this.queryRoleSnap = new QueryMappingReference<RoleSnap>(
                GateServerManager.Mapping.TYPE_ROLE_SNAP_DATA, this);

            this.Provider.AutoDispose(accountSave);
            this.Provider.AutoDispose(queryRoleSnap);

            this.heartbeat_timer = base.Provider.CreateTimer(CheckHeartbeat, this,
                TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionHeartbeatCheckInterval),
                TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionHeartbeatCheckInterval));

            var data = await this.accountSave.LoadDataAsync();
        }

        protected override async Task OnStopAsync()
        {
            this.heartbeat_timer.Dispose();
            if (this.session != null)
            {
                this.session.socket.Disconnect(StopInfo.Reason);
            }

            await ShutdownLogicServiceAsync("session destroy");
            await this.accountSave.FlushAsync();
        }

        protected virtual void CheckHeartbeat(object state)
        {
            if (session == null)
            {
                this.ShutdownSelf("timeout");
            }
            else
            {
                if ((DateTime.UtcNow - session.socket.LastReceivedTimeUTC) > TimeSpan.FromSeconds(GateTimerConfig.timer_sec_SessionKeepTimeout))
                {
                    if (!session.socket.IsConnected)
                    {
                        session = null;
                        this.ShutdownSelf("timeout");
                    }
                    else
                    {
                        // socket 显示连接中但长时间无消息，发送一条协议验证连接是否真实存在
                        // 若连接已断开，发送操作会触发底层关闭逻辑
                        this.SocketSend(new ClientPong { time = DateTime.UtcNow });
                    }
                }
            }
        }

        public void SocketSend(ISerializable msg)
        {
            var session = this.session;
            if (session != null)
            {
                session.SocketSend(msg);
            }
        }
        public void SocketSend(BinaryMessage msg)
        {
            var session = this.session;
            if (session != null)
            {
                session.SocketSend(msg);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 从客户端收取的协议，需要转发给LogicService
        /// </summary>
        public virtual void call_connect_OnReceivedBinaryImmediately(TypeCodec route_codec, BinaryMessage binary, OnRpcReturnBinary cb = null)
        {
#if false
            if (IsChannelProtocol(route_codec))
            {
                SendToChannel(route_codec, binary);
            }
            else 
#endif
            if (typeof(IWormholeProtocol).IsAssignableFrom(route_codec.MessageType))
            {
                if (cb != null)
                {
                    this.remote_logic_service.WormholeTransportAsync(binary).ContinueWith(t =>
                    {
                        if (t.IsCompleted && t.Result != null)
                        {
                            cb((BinaryMessage)t.Result, t.Exception);
                        }
                        else
                        {
                            cb(BinaryMessage.NULL, t.Exception);
                        }
                    });
                }
                else
                {
                    this.remote_logic_service.WormholeTransport(binary);
                }
            }
            else
            {
                this.Provider.Execute(new Action(do_async_OnReceivedBinaryImmediately));
                void do_async_OnReceivedBinaryImmediately()
                {
                    SendToLogic(route_codec, binary, cb);
                }
            }
        }

        /// <summary>
        /// 由其他服务收到的协议，需要转发给客户端
        /// </summary>
        public virtual void rpc_AnyWormholeTransported(RemoteAddress from, object message)
        {
            var session = this.session;
            if (session != null)
            {
                if (message is BinaryMessage bin)
                {
                    session.socket.Send(bin);
                }
                else if (message is ISerializable ser)
                {
                    session.socket.Send(ser);
                }
            }
        }

        [RpcHandler(isBinary: true)]
        public virtual void rpc_AnyHandle(BinaryMessage msg)
        {
            if (mDisconnected) return;
            var session = this.session;
            if (session != null)
            {
                session.SocketSend(msg);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(SystemShutdownNotify))]
        public virtual void rpc_system_Handle(SystemShutdownNotify shutdown)
        {
            var logic = remote_logic_service;
            if (logic != null)
            {
                logic.Invoke(new SessionDisconnectNotify() { sessionName = SelfAddress.ServiceName, });
            }

            this.ShutdownSelf(shutdown.reason);
        }

        [RpcHandler(typeof(KickPlayerNotify))]
        public virtual void rpc_system_Handle(KickPlayerNotify notify)
        {
            if (session != null)
            {
                this.ShutdownSelf(notify.reason);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 首次连接或者重新连接
        /// </summary>
        /// <param name="bind"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(LocalBindSessionRequest), typeof(LocalBindSessionResponse))]
        public virtual async Task<LocalBindSessionResponse> rpc_connect_Handle(LocalBindSessionRequest bind)
        {
            if (!string.IsNullOrEmpty(bind.enter.c2s_session_token) && !string.IsNullOrEmpty(this.sessionToken) &&
                bind.enter.c2s_session_token != this.sessionToken)
            {
                this.sessionToken = null;
                return (new LocalBindSessionResponse() { s2c_code = Response.CODE_ERROR });
            }

            var savedLoginToken = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginToken));
            var savedServerGroup = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerGroupID));
            if (savedLoginToken != bind.enter.c2s_login_token)
            {
                this.sessionToken = null;
                return (new LocalBindSessionResponse() { s2c_code = Response.CODE_ERROR });
            }

            var old_session = this.session;
            if (old_session != null)
            {
                var disconnect = new SessionDisconnectNotify()
                {
                    socketID = old_session.socket.ID,
                    sessionName = SelfAddress.ServiceName,
                };
                if (enter_game != null)
                {
                    disconnect.roleID = enter_game.c2s_roleUUID;
                }

                //老Session暂停发包//
                var logic = this.remote_logic_service;
                if (logic != null)
                {
                    logic.Invoke(disconnect);
                }

                //log.Log("Reconnect");
                //老Session踢下线//
                old_session.socket.Disconnect("New Session Reconnect");
            }
            else
            {
                //log.Log("Connect");
            }

            this.session = bind.session;
            this.enter = bind.enter;
            //登录成功后，产生新的Token用于断线重连//
            this.sessionToken = Guid.NewGuid().ToString();

            return (new LocalBindSessionResponse()
            {
                session = this,
                sessionToken = sessionToken,
                serverGroupID = savedServerGroup,
            });
        }

        /// <summary>
        /// 用户断线
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionDisconnectNotify))]
        public virtual void rpc_disconnect_Handle(SessionDisconnectNotify disconnect)
        {
            //log.Log("Disconnect");
            disconnect.sessionName = SelfAddress.ServiceName;
            if (enter_game != null)
            {
                disconnect.roleID = enter_game.c2s_roleUUID;
            }

            //排除老Session踢下线导致的Disconnect//
            if (this.session == null || disconnect.socketID == this.session.socket.ID)
            {
                this.session = null;
                var logic = this.remote_logic_service;
                if (logic != null)
                {
                    mDisconnected = true;
                    logic.Invoke(disconnect);
                }
            }
        }

        /// <summary>
        /// 玩家进入游戏
        /// </summary>
        /// <param name="enter"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(ClientEnterGameRequest), typeof(ClientEnterGameResponse))]
        public virtual async Task<ClientEnterGameResponse> rpc_client_Handle(ClientEnterGameRequest enter)
        {
            //验证此角色UID是否在此账号列表中 
            var roleIDMap = accountSave.Data.roleList;
            if (roleIDMap == null || string.IsNullOrEmpty(enter.c2s_roleUUID) || !roleIDMap.ContainsKey(enter.c2s_roleUUID))
            {
                return new ClientEnterGameResponse()
                {
                    s2c_code = ClientEnterGameResponse.CODE_ROLEID_INVAILD,
                };
            }
            //第三方/一号通验证//
            var serverPassportResult = await GateServerManager.Passport.VerifyPassportEnterGameAsync(this.enter, enter);
            if (!serverPassportResult.Verified)
            {
                return new ClientEnterGameResponse()
                {
                    s2c_code = ClientEnterGameResponse.CODE_ROLE_SUSPEND,
                    s2c_msg = serverPassportResult.Message
                };
            }

            //log.Log("ClientEnterGameRequest");

            //-------------------------------------------------------------------------------------------------------

            this.enter_game = enter;
            bool reconnect = false;
            var rec = new SessionReconnectNotify();
            rec.sessionName = SelfAddress.ServiceName;
            rec.roleID = enter_game?.c2s_roleUUID;

            var logic = remote_logic_service;
            if (logic != null)
            {
                var oldRoleID = logic.Config[nameof(GateClientInfo.RoleID)].ToString();
                if (oldRoleID != enter.c2s_roleUUID)
                {
                    log.WarnFormat(string.Format("Role Already Login : Acc={0} : Role={1} -> {2}", accountID, oldRoleID, enter.c2s_roleUUID));
                    if (AllowAutoKickExistLogic)
                    {
                        await logic.ShutdownAsync("switch role");
                        logic = await this.CreateLogicServiceAsync(enter);
                    }
                    else
                    {
                        return new ClientEnterGameResponse() { s2c_code = ClientEnterGameResponse.CODE_LOGIC_ALREADY_LOGIN };
                    }
                }
                else
                {
                    //if (DeepCore.Log.Logger.SHOW_LOG)
                    {
                        log.InfoFormat(string.Format("Role Reconnect : Acc={0} : Role={1}", accountID, enter.c2s_roleUUID));
                    }
                    reconnect = true;
                    rec.config = (await CreateLogicConfig()).SaveTo();
                }
            }
            else
            {
                //if (DeepCore.Log.Logger.SHOW_LOG)
                {
                    log.InfoFormat(string.Format("Role Connect : Acc={0} : Role={1}", accountID, enter.c2s_roleUUID));
                }
                logic = await this.CreateLogicServiceAsync(enter);
            }

            if (logic != null)
            {
                accountSave.SetField(nameof(AccountData.lastLoginRoleID), enter.c2s_roleUUID);
                await accountSave.FlushAsync();
                try
                {
                    mDisconnected = false;
                    var ret = await logic.CallAsync<ClientEnterGameResponse>(enter);
                    //log.Log("ClientEnterGameResponse: " + ret.IsSuccess);
                    ret.s2c_reconnected = reconnect;
                    return ret;
                }
                finally
                {
                    if (reconnect)
                    {
                        logic.Invoke(rec);
                    }
                }
            }
            else
            {
                return (new ClientEnterGameResponse() { s2c_code = ClientEnterGameResponse.CODE_LOGIC_NOT_FOUND, });
            }
        }

        /// <summary>
        /// 玩家离开游戏
        /// </summary>
        /// <param name="enter"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(ClientExitGameRequest), typeof(ClientExitGameResponse))]
        public virtual async Task<ClientExitGameResponse> rpc_client_Handle(ClientExitGameRequest exit)
        {
            //log.Log("ClientExitGameRequest");
            await ShutdownLogicServiceAsync("player exit");
            return new ClientExitGameResponse();
            //return Task.FromResult(new ClientExitGameResponse());
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------


        //--------------------------------------------------------------------------------------------------------------------------------------------
#if false
        #region Channel
        //         [RpcHandler]
        //         public virtual async Task rpc_channel_Handle(RemoteAddress channel, ActorEnterChannelS2C bind)
        //         {
        //             this.remote_channel_service = await this.Provider.GetAsync(channel);
        //             SocketSend(bind);
        //         }
        // 
        //         [RpcHandler]
        //         public virtual void rpc_channel_Handle(RemoteAddress channel, ActorLeaveChannelS2C msg)
        //         {
        //             this.remote_channel_service = null;
        //             SocketSend(msg);
        //         }
        [RpcHandler]
        public virtual async Task rpc_channel_Handle(RemoteAddress channel, SessionBindChannelNotify msg)
        {
            this.remote_channel_service = await this.Provider.GetAsync(GateServerManager.ServerName.GetWorldChannelService(msg.actorChannelID));
        }
        protected IRemoteService remote_channel_service { get; private set; }
        protected readonly TypeCodec channel_c2s_codec = ConnectServer.ClientCodec.Factory.GetCodec(typeof(ClientPostChannelC2S));
        protected readonly TypeCodec channel_s2s_codec = ConnectServer.ClientCodec.Factory.GetCodec(typeof(SessionPostChannelC2S));
        protected virtual bool IsChannelProtocol(TypeCodec route_codec)
        {
            return channel_c2s_codec.MessageID == route_codec.MessageID;
        }
        public virtual void SendToChannel(TypeCodec route_codec, BinaryMessage action)
        {
            try
            {
                var channel = remote_channel_service;
                var enter = enter_game;
                if (channel != null && enter != null)
                {
                    //var c2s = this.ServerCodec.DecodeBinary(action);
                    using (var output = IOStreamObjectPool.AllocOutputAutoRelease(ConnectServer.ClientCodec.Factory))
                    {
                        output.PutUTF(enter.c2s_roleUUID);
                        output.PutRawBytes(action.Buffer, action.BufferOffset, action.BufferLength);
                        var to_channel = BinaryMessage.FromBuffer(channel_s2s_codec.MessageID, output.Buffer);
                        //var s2s = this.ServerCodec.DecodeBinary(to_channel);
                        channel.WormholeTransport(to_channel);

                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        #endregion
#endif
        //--------------------------------------------------------------------------------------------------------------------------------------------
        #region Logic
        protected IRemoteService remote_logic_service;
        /// <summary>
        /// 逻辑协议发往LogicService
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="callback"></param>
        public virtual void SendToLogic(TypeCodec route_codec, BinaryMessage msg, OnRpcReturnBinary callback = null)
        {
            var logic = remote_logic_service;
            if (logic != null)
            {
                if (callback != null)
                    logic.Call(msg, callback);
                else
                    logic.Invoke(msg);
            }
            else
            {
                log.Warn("SendToLogic Error : Logic Service Not Init : " + route_codec);
                if (callback != null) callback(BinaryMessage.NULL);
            }
        }
        protected virtual async Task<IRemoteService> CreateLogicServiceAsync(ClientEnterGameRequest enter_game)
        {
            var cfg = await CreateLogicConfig();
            var ret = await this.Provider.CreateAsync(
                GateServerManager.ServerName.GetLogicService(enter_game.c2s_roleUUID, this.SelfAddress.ServiceNode), cfg);
            this.remote_logic_service = ret;
            return ret;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        protected virtual async Task<GateClientInfo> CreateLogicConfig()
        {
            var cfg = new GateClientInfo();

            var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
            var serverGroupID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerGroupID));
            var ip = (this.session.socket.RemoteAddress as System.Net.IPEndPoint)?.Address?.ToString();
            var c2s_clientInfo = accountSave.Data.lastClientInfo;

            cfg.SessionNode = this.SelfAddress.ServiceNode;
            cfg.SessionName = this.SelfAddress.ServiceName;
            cfg.AccountID = enter.c2s_account;
            cfg.RoleID = enter_game.c2s_roleUUID;
            cfg.ServerID = serverID;
            cfg.ServerGroupID = serverGroupID;

            cfg.ClientEndpoint = ip;
            cfg.ClientVersion = c2s_clientInfo.clientVersion;
            cfg.DeviceID = c2s_clientInfo.deviceId;
            cfg.DeviceModel = c2s_clientInfo.deviceModel;
            cfg.DeviceType = c2s_clientInfo.deviceType;
            cfg.Network = c2s_clientInfo.network;
            cfg.Region = c2s_clientInfo.region;
            cfg.SDKName = c2s_clientInfo.sdkName;
            cfg.SDKVersion = c2s_clientInfo.sdkVersion;
            cfg.SubChannel = c2s_clientInfo.subChannel;
            cfg.UserAgent = c2s_clientInfo.userAgent;
            cfg.UserSource1 = c2s_clientInfo.userSource1;
            cfg.UserSource2 = c2s_clientInfo.userSource2;
            cfg.PlatformAccount = c2s_clientInfo.platformAcount;
            cfg.WalletAddress = c2s_clientInfo.walletAddress;
            cfg.InvateWalletAddress = c2s_clientInfo.invateWalletAddress;

            return cfg;
        }
        protected virtual async Task ShutdownLogicServiceAsync(string reason)
        {
            var logic = remote_logic_service;
            //var area = remote_area_service;
            remote_logic_service = null;
            if (logic != null)
            {
                if (await Provider.GetAsync(logic.Address) != null)
                {
                    //                     if (area != null)
                    //                     {
                    //                         try
                    //                         {
                    //                             await logic.CallAsync<SessionBeginLeaveResponse>(new SessionBeginLeaveRequest()
                    //                             {
                    //                                 sessionName = SelfAddress.ServiceName,
                    //                                 roleID = enter_game.c2s_roleUUID,
                    //                             });
                    //                         }
                    //                         catch (Exception err)
                    //                         {
                    //                             log.Error(err.Message, err);
                    //                         }
                    //                     }
                    try
                    {
                        var result = await logic.ShutdownAsync(reason);
                        log.Info("ShutdownAsync Complete : " + result);
                    }
                    catch (Exception err)
                    {
                        log.Error("ShutdownAsync Error : " + err.Message, err);
                    }
                }
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------
    }


    /// <summary>
    /// Connect 进程内，通知SessionService绑定ViewSession
    /// </summary>
    public class LocalBindSessionRequest : Request, IRpcNoneSerializable
    {
        public ConnectServer.ViewSession session;
        public ClientEnterServerRequest enter;
    }

    public class LocalBindSessionResponse : Response, IRpcNoneSerializable
    {
        public SessionService session;
        public string sessionToken;
        public string serverGroupID;
    }
}
