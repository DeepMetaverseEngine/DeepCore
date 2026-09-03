using DeepCore;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Gate.Server.Service.Area
{
    public class AreaService : IService
    {
        public Random random { get; private set; } = new Random();
        public IDisposable sync_state_timer { get; private set; }
        public IRemoteService area_manager;
        private TypeCodec session_battle_codec;
        public BattleCodec BattleClientCodec { get; }


        public AreaService(ServiceStartInfo start) : base(start)
        {
            this.BattleClientCodec = new BattleCodec(MMOServerManager.Battle.DataRoot.Templates);
            this.session_battle_codec = base.ServerCodec.Factory.GetCodec(typeof(SessionBattleAction));
            this.Provider.OnWormholeTransported += this.client_rpc_OnWormholeTransported;

        }
        protected override void OnDisposed()
        {

        }
        protected override async Task OnStartAsync()
        {
            var manager_name = MMOServerManager.MMOServerName.GetAreaManagerService(this.SelfNode);
            this.area_manager = await Provider.GetAsync(manager_name);
            this.sync_state_timer = this.Provider.CreateTimer(timer_SyncState, this,
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(GateTimerConfig.timer_sec_AreaStateNotify));
            await area_manager.CallAsync<RegistAreaResponse>(new RegistAreaRequest()
            {
                areaName = SelfAddress.ServiceName,
                areaNode = SelfAddress.ServiceNode,
            });
        }
        protected override async Task OnStopAsync()
        {
            this.sync_state_timer.Dispose();
            foreach (var item in zoneNodes)
            {
                await item.Value.DoStopAsync();
            }
        }
        public override bool GetState(TextWriter sb)
        {
            ForEachZones(node =>
            {
                sb.WriteLine(CUtils.SequenceChar('-', 100));
                node.GetStatus(sb);
                sb.WriteLine(CUtils.SequenceChar('-', 100));
            });
            return true;
        }

        protected virtual void timer_SyncState(object obj)
        {
            this.area_manager.Invoke(new AreaStateNotify()
            {
                areaName = SelfAddress.ServiceName,
                areaNode = SelfAddress.ServiceNode,
                roleCount = this.PlayerCount,
                zoneCount = this.ZoneNodeCount,
                cpuPercent = 1, // TODO
                memoryMB = 1, // TODO
            });
        }
        //[RpcHandler(typeof(SystemStaticServicesStartedNotify))]
        //public virtual void rpc_HandleSystem(SystemStaticServicesStartedNotify shutdown)
        //{
        //    area_manager.CallAsync<RegistAreaResponse>(new RegistAreaRequest()
        //    {
        //        areaName = SelfAddress.ServiceName,
        //        areaNode = SelfAddress.ServiceNode,
        //    });
        //}

        //-----------------------------------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------------------------------
        #region __CreateObject__
        //--------------------------------------------------------------------------------------------------------------------------------
        protected virtual AreaZoneSession CreateZonePlayer(AreaZoneNode node, RoleEnterZoneRequest enter)
        {
            return new AreaZoneSession(this, node, enter);
        }
        protected virtual AreaZoneNode CreateZoneNode(CreateZoneNodeRequest create, SceneData map)
        {
            return new AreaZoneNode(this, create, map);
        }
        public virtual UnitInfo GetDefaultUnitTemplate()
        {
            var tempID = MMOServerManager.Battle.DataRoot.Templates.DefaultConfig.DEFAULT_UNIT;
            var temp = MMOServerManager.Battle.DataRoot.Templates.GetUnit(tempID);
            if (temp == null)
            {
                foreach (var u in MMOServerManager.Battle.DataRoot.Templates.AllUnits)
                {
                    if (u.UType == UnitType.TYPE_PLAYER)
                    {
                        temp = u;
                        break;
                    }
                }
            }
            return temp;
        }
        public virtual AreaZoneNode GetDefaultZoneNode()
        {
            return random.GetRandomInCollection(this.ZoneNodes);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region __ClientToArea__
        //--------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Client -> Session -> Area
        /// </summary>
        [RpcHandler(typeof(SessionBattleAction))]
        public virtual void client_rpc_Handle(SessionBattleAction action)
        {
            try
            {
                var roleID = action.roleID;
                var player = GetPlayer(roleID);
                if (player == null)
                {
                    return;
                }
                player.client_rpc_Handle(action);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
        }
        public virtual void client_rpc_OnWormholeTransported(RemoteAddress from, object message)
        {
            if (message is BinaryMessage bin)
            {
                message = this.ServerCodec.DecodeBinary(bin);
            }
            if (message is SessionBattleAction ser)
            {
                client_rpc_Handle(ser);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region __LogicToArea__
        //--------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 创建副本
        /// </summary>
        /// <param name="create"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(CreateZoneNodeRequest), typeof(CreateZoneNodeResponse))]
        public async Task<CreateZoneNodeResponse> area_manager_rpc_CreateZone(CreateZoneNodeRequest create)
        {
            try
            {
                var maptemp = MMOServerManager.Battle.GetSceneAsCache(create.zoneTemplateID);
                if (maptemp == null) throw new Exception("No MapTemplate : " + create.zoneTemplateID);
                var zoneUUID = create.managerZoneUUID;
                if (zoneNodes.ContainsKey(zoneUUID))
                {
                    throw new Exception(string.Format("node instance id ({0}) already exist!", zoneUUID));
                }
                AreaZoneNode node = this.CreateZoneNode(create, maptemp);
                zoneNodes.TryAdd(zoneUUID, node);
                var z = await node.DoStartAsync();
                // log.InfoFormat("Scene Started : {0} : {1}", z.UUID, z.Data);
                return (new CreateZoneNodeResponse()
                {
                    areaName = SelfAddress.ServiceName,
                    areaNode = SelfAddress.ServiceNode,
                    zoneUUID = zoneUUID,
                    TemplateID = maptemp.ID,
                });
            }
            catch (Exception e)
            {
                log.Error("CreateZone failed, error: " + e.Message, e);
                return (new CreateZoneNodeResponse()
                {
                    s2c_code = CreateZoneNodeResponse.CODE_ERROR,
                    s2c_msg = e.Message,
                });
            }
        }

        /// <summary>
        /// 销毁副本
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(DestoryZoneNodeRequest), typeof(DestoryZoneNodeResponse))]
        public async Task<DestoryZoneNodeResponse> area_manager_rpc_DestroyZone(DestoryZoneNodeRequest stop)
        {
            try
            {
                if (this.zoneNodes.TryRemove(stop.zoneUUID, out var node))
                {
                    //删除场景实例的所有玩家              
                    node.ZoneNode.ForEachPlayers((p) =>
                    {
                        try
                        {
                            using (var player = p.Session as AreaZoneSession)
                            {
                                log.Error("DestoryZoneNode Have Player : " + player.RoleUUID);
                                players.TryRemove(player.RoleUUID, out var pp);
                            }
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    });
                    await node.DoStopAsync();
                    return (new DestoryZoneNodeResponse() { s2c_msg = "done" });
                }
                else
                {
                    return (new DestoryZoneNodeResponse() { s2c_msg = "done" });
                }
            }
            catch (Exception e)
            {
                log.Error("DestroyZone failed, error: " + e.Message, e);
                return (new DestoryZoneNodeResponse() { s2c_code = Response.CODE_ERROR, s2c_msg = e.Message });
            }
        }

        /// <summary>
        /// 玩家进入副本
        /// </summary>
        /// <param name="enter"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(RoleEnterZoneRequest), typeof(RoleEnterZoneResponse))]
        public async Task<RoleEnterZoneResponse> area_manager_rpc_PlayerEnter(RoleEnterZoneRequest enter)
        {
            try
            {
                var node = this.FindZoneNode(enter);
                if (node == null)
                {
                    //TODO 分配一个默认场景
                    node = this.GetDefaultZoneNode();
                }
                //场景不存在//
                if (node == null)
                {
                    return new RoleEnterZoneResponse()
                    {
                        s2c_code = RoleEnterZoneResponse.CODE_ZONE_NOT_EXIST,
                        s2c_msg = $"PlayerEnter: ZoneNotExistException: roleID={enter.roleUUID} zone={enter.expectZoneUUID}"
                    };
                }
                if (this.players.TryGetOrCreate(enter.roleUUID, out var player, (uuid) => this.CreateZonePlayer(node, enter)))
                {
                    if (player.ZoneNode == node)
                    {
                        var replace = await node.DoPlayerEnterReplace(player, enter);
                        if (replace.IsSuccess) { return replace; }
                        this.players.TryGetOrCreate(enter.roleUUID, out player, (uuid) => this.CreateZonePlayer(node, enter));
                        return await node.DoPlayerEnterAsync(player, enter);
                    }
                    else
                    {
                        return new RoleEnterZoneResponse()
                        {
                            s2c_code = RoleEnterZoneResponse.CODE_ZONE_NOT_EXIST,
                            s2c_msg = $"PlayerEnter: ZoneNotExistException: roleID={enter.roleUUID} zone={enter.expectZoneUUID}"
                        };
                    }
                }
                else
                {
                    return await node.DoPlayerEnterAsync(player, enter);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                return new RoleEnterZoneResponse() { s2c_code = Response.CODE_ERROR, s2c_msg = e.Message };
            }
        }

        /// <summary>
        /// 玩家离开副本
        /// </summary>
        /// <param name="leave"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(RoleLeaveZoneRequest), typeof(RoleLeaveZoneResponse))]
        public async Task<RoleLeaveZoneResponse> area_manager_rpc_PlayerLeave(RoleLeaveZoneRequest leave)
        {
            try
            {
                if (this.players.TryRemove(leave.roleID, out var player))
                {
                    using (player)
                    {
                        AreaZoneNode node = player.ZoneNode;
                        if (node == null)
                        {
                            return (new RoleLeaveZoneResponse()
                            {
                                s2c_code = RoleLeaveZoneResponse.CODE_ROLE_NOT_EXIST,
                                s2c_msg = $"PlayerLeave: ZoneNotExistException: roleID={leave.roleID} zone={player.ZoneUUID}"
                            });
                        }
                        return await node.DoPlayerLeaveAsync(player, leave);
                    }
                }
                else
                {
                    return (new RoleLeaveZoneResponse()
                    {
                        s2c_code = RoleLeaveZoneResponse.CODE_ROLE_NOT_EXIST,
                        s2c_msg = $"PlayerLeave: PlayerNotExistException: roleID=roleID={leave.roleID} zone={leave.zoneUUID}"
                    });
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                return (new RoleLeaveZoneResponse() { s2c_code = Response.CODE_ERROR, s2c_msg = e.Message });
            }
        }

        [RpcHandler(typeof(GetRolePositionRequest), typeof(GetRolePositionResponse))]
        public async Task<GetRolePositionResponse> area_manager_rpc_GetRolePosition(GetRolePositionRequest req)
        {
            var resp = new GetRolePositionResponse();
            if (players.TryGetValue(req.roleUUID, out var role))
            {
                return await role.ZoneNode.DoGetPlayerPosition(role, req);
            }
            else
            {
                resp.s2c_code = GetRolePositionResponse.CODE_ROLE_NOT_EXIST;
                return resp;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 玩家数据改变
        /// </summary>
        /// <param name="changed"></param>
        [RpcHandler(typeof(RoleDataChangedNotify))]
        public void logic_rpc_PlayerNetStateChanged(RoleDataChangedNotify changed)
        {
            AreaZoneSession player;
            AreaZoneNode node;
            try
            {
                if (TryGetPlayer(changed.roleID, out player, out node))
                {
                    player.logic_rpc_Handle(changed.roleData);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
        }

        /// <summary>
        /// 玩家断开连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionDisconnectNotify))]
        public void logic_rpc_Handle(SessionDisconnectNotify disconnect)
        {
            AreaZoneSession player;
            AreaZoneNode node;
            try
            {
                if (TryGetPlayer(disconnect.roleID, out player, out node))
                {
                    node.DoPlayerDisconnect(player);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
        }
        /// <summary>
        /// 玩家重新连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionReconnectNotify))]
        public void logic_rpc_Handle(SessionReconnectNotify reconnect)
        {
            AreaZoneSession player;
            AreaZoneNode node;
            try
            {
                if (TryGetPlayer(reconnect.roleID, out player, out node))
                {
                    node.DoPlayerReconnect(player);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
        }

        [RpcHandler(typeof(SessionBeginLeaveRequest), typeof(SessionBeginLeaveResponse))]
        public async Task<SessionBeginLeaveResponse> logic_rpc_Handle(SessionBeginLeaveRequest leave)
        {
            try
            {
                if (this.players.TryGetValue(leave.roleID, out var player))
                {
                    var node = player.ZoneNode;
                    if (node != null)
                    {
                        await node.DoPlayerBeginLeaveAsync(player);
                        return (new SessionBeginLeaveResponse() { s2c_code = Response.CODE_OK });
                    }
                }
                return (new SessionBeginLeaveResponse() { s2c_code = Response.CODE_ERROR });
            }
            catch (Exception e)
            {
                log.Warn(e.Message, e);
                return (new SessionBeginLeaveResponse() { s2c_code = Response.CODE_ERROR, s2c_msg = e.Message });
            }
        }


        #endregion


        //-----------------------------------------------------------------------------------------------------------------------------
        #region __ZoneAndPlayer__
        //--------------------------------------------------------------------------------------------------------------------------------
        private ConcurrentDictionary<string, AreaZoneNode> zoneNodes = new ConcurrentDictionary<string, AreaZoneNode>();
        private ConcurrentDictionary<string, AreaZoneSession> players = new ConcurrentDictionary<string, AreaZoneSession>();

        public int PlayerCount
        {
            get { { return players.Count; } }
        }
        public int ZoneNodeCount
        {
            get { { return zoneNodes.Count; } }
        }
        public List<AreaZoneNode> ZoneNodes
        {
            get { { return new List<AreaZoneNode>(zoneNodes.Values); } }
        }
        public List<AreaZoneSession> ZonePlayers
        {
            get { { return new List<AreaZoneSession>(players.Values); } }
        }

        public void ForEachPlayers(Action<AreaZoneSession> action)
        {
            var list = new List<AreaZoneSession>();
            {
                { list.AddRange(players.Values); }
                foreach (var p in list) { action(p); }
            }
        }
        public void ForEachZones(Action<AreaZoneNode> action)
        {
            var list = new List<AreaZoneNode>();
            {
                { list.AddRange(zoneNodes.Values); }
                foreach (var z in list) { action(z); }
            }
        }

        public AreaZoneNode GetZoneNode(string zoneUUID)
        {
            if (zoneNodes.TryGetValue(zoneUUID, out var ret))
            {
                return ret;
            }
            return null;
        }

        public AreaZoneNode FindZoneNode(RoleEnterZoneRequest enter)
        {
            {
                if (enter.expectZoneUUID != null && zoneNodes.TryGetValue(enter.expectZoneUUID, out var node))
                {
                    return node;
                }
                if (enter.expectZoneTemplateID != 0)
                {
                    foreach (var e in zoneNodes.Values)
                    {
                        if (e.ZoneTemplateID == enter.expectZoneTemplateID)
                        {
                            return e;
                        }
                    }
                }
            }
            return null;
        }
        public AreaZoneSession GetPlayer(string roleID)
        {
            if (roleID != null && players.TryGetValue(roleID, out var ret))
            {
                return ret;
            }
            return null;
        }

        public bool TryGetPlayer(string roleID, out AreaZoneSession player, out AreaZoneNode node)
        {
            if (roleID != null && players.TryGetValue(roleID, out player))
            {
                node = player.ZoneNode;
                //                 if (node == null)
                //                 {
                //                     throw new Exception("PlayerLeave: InstanceNotExistException: " + player.ZoneUUID);
                //                 }
                return true;
            }
            player = null;
            node = null;
            //             else
            //             {
            //                 //throw new Exception("PlayerLeave: PlayerNotExistException roleID: " + roleID);
            //             }
            return false;
        }
        /// <summary>
        /// 异步执行战斗场景内交互代码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="zoneUUID"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task<T> QueueZoneTaskAsync<T>(string zoneUUID, Func<InstanceZone, T> func)
        {
            if (zoneNodes.TryGetValue(zoneUUID, out var node))
            {
                return node.ZoneNode.QueueSceneTaskAsync<T>(func);
            }
            return Task.FromResult<T>(func(null));
        }
        /// <summary>
        /// 异步执行战斗场景内交互代码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="roleUUID"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task<T> QueuePlayerTaskAsync<T>(string roleUUID, Func<InstancePlayer, T> func)
        {
            if (roleUUID != null && players.TryGetValue(roleUUID, out var player))
            {
                var node = player.ZoneNode;
                return node.ZoneNode.QueuePlayerTaskAsync<T>(roleUUID, func);
            }
            return Task.FromResult<T>(func(null));
        }
        /// <summary>
        /// 异步执行战斗场景内交互代码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="zoneUUID"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task<T> QueueZoneTaskAsync<T>(string zoneUUID, Func<AreaZoneNode, InstanceZone, T> func)
        {
            if (zoneNodes.TryGetValue(zoneUUID, out var node))
            {
                return node.ZoneNode.QueueSceneTaskAsync<T>(z => func(node, z));
            }
            return Task.FromResult<T>(func(null, null));
        }
        /// <summary>
        /// 异步执行战斗场景内交互代码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="roleUUID"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public Task<T> QueuePlayerTaskAsync<T>(string roleUUID, Func<AreaZoneSession, InstancePlayer, T> func)
        {
            if (roleUUID != null && players.TryGetValue(roleUUID, out var player))
            {
                var node = player.ZoneNode;
                return node.ZoneNode.QueuePlayerTaskAsync<T>(roleUUID, p => func(player, p));
            }
            return Task.FromResult<T>(func(null, null));
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------

    }
}
