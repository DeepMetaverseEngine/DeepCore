using DeepCore.Protocol;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using Gate.Data;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Server.Service.Logic.Module
{
    /// <summary>
    /// MMO 战斗场景交互模块
    /// </summary>
    public class AreaModule : MMOLogicModule<MMOLogicService>, ILogicModule
    {
        public IRemoteService areaManager { get; private set; }
        public IRemoteService currentArea { get; protected set; }
        public string currentAreaName { get { return currentArea != null ? currentArea.Address.ServiceName : null; } }
        //-------------------------------------------------------------------------------------------------------------
        protected List<ZoneInfoSnap> mLastZoneInfoSnaps;
        protected bool mZoneInfoDirtyFlag = true;
        protected DateTime mLastGetZoneInfoTimeStamp;
        public MappingReference<ServerRoleZoneData> RoleZoneMapping { get; protected set; }
        //-------------------------------------------------------------------------------------------------------------
        public AreaModule(MMOLogicService service) : base(service)
        {

        }
        protected override void Disposing()
        {
        }
        public override async Task OnStartAsync()
        {
            this.areaManager = await Service.Provider.GetAsync(MMOServerManager.MMOServerName.GetAreaManagerService(this.Service.SelfNode));
            if (areaManager == null)
            {
                throw new Exception("Cant Find AreaManager Service");
            }
            await LoadRoleZoneDataAsync();
        }
        public override Task OnStartedAsync()
        {
            //Service.Execute(RequestEnterDefaultZoneAsync());
            return Task.CompletedTask;
        }
        public async override Task OnStopAsync()
        {
            this.DisposingEvents();
            await this.RequestLeaveZoneAsync();
        }
        //-------------------------------------------------------------------------------------------------------------
        void ILogicModule.OnSaveData(IObjectTransaction trans)
        {
        }
        Task ILogicModule.OnClientEnterGameAsync()
        {
            return Task.CompletedTask;
        }
        Task ILogicModule.OnSessionDisconnectAsync(SessionDisconnectNotify notify)
        {
            var area = currentArea;
            if (area != null)
            {
                area.Invoke(notify);
            }
            return Task.CompletedTask;
        }
        Task ILogicModule.OnSessionReconnectAsync(SessionReconnectNotify notify)
        {
            var area = currentArea;
            if (area != null)
            {
                area.Invoke(notify);
            }
            return Task.CompletedTask;
        }
        //-------------------------------------------------------------------------------------------------------------
        protected virtual async Task LoadRoleZoneDataAsync()
        {
            this.RoleZoneMapping = Service.AutoDispose(new MappingReference<ServerRoleZoneData>(
                GateServerManager.Mapping.TYPE_ROLE_ZONE_DATA,
                Service.RoleID,
                Service));
            await GateServerManager.Mapping.CreateRoleZoneDataAsync(RoleZoneMapping, Service.RoleData);
        }
        protected virtual void SaveEnterZoneInfo(RoleEnterZoneResponse rsp)
        {
            //如果记录野外场景的uuid和MapID.//
            //解决公共场景切线以后进入副本出来以后不会原来线的问题.//
            var sd = MMOServerManager.Battle.GetSceneAsCache(rsp.zoneTemplateID);
            if (sd != null && sd.IsPublicMap)
            {
                this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_public_zone_ID), rsp.zoneTemplateID);
                this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_public_area_uuid), rsp.zoneUUID);
                this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_public_zone_pos), rsp.roleScenePos);
            }
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_zone_template_id), rsp.zoneTemplateID);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_zone_uuid), rsp.zoneUUID);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_area_name), rsp.areaName);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_area_node), rsp.areaNode);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_zone_pos), rsp.roleScenePos);
        }
        public virtual void SaveLeaveZoneInfo(RoleLeaveZoneResponse pos)
        {
            var rd = RoleZoneMapping.Data;
            var sd = MMOServerManager.Battle.GetSceneAsCache(rd.last_zone_template_id);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_zone_saved), pos.LeaveZoneSaveData);
            this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_zone_pos), pos.lastScenePos);
            if (sd != null && sd.IsPublicMap)
            {
                this.RoleZoneMapping.SetField(nameof(ServerRoleZoneData.last_public_zone_pos), pos.lastScenePos);
            }
        }
        //-------------------------------------------------------------------------------------------------------------

        public virtual void BeginEnterZone(RoleEnterZoneRequest req)
        {
            var rd = RoleZoneMapping.Data;
            var rrd = Service.RoleData;
            req.serverID = Service.ServerID;
            req.serverGroupID = Service.ServerGroupID;
            req.roleUUID = Service.RoleID;
            req.roleSessionName = Service.SessionName;
            req.roleSessionNode = Service.SessionNode;
            req.roleLogicName = Service.SelfAddress.ServiceName;
            req.roleLogicNode = Service.SelfAddress.ServiceNode;
            req.roleDisplayName = rrd.name;
            req.roleUnitTemplateID = rd.unit_template_id;
            //req.roleData = ToRoleBattleData(req);//< ---战斗相关数据
            req.LastZoneSaveData = rd.last_zone_saved;
            req.lastPublicZoneID = rd.last_public_zone_ID;
            req.lastPublicMapUUID = rd.last_public_area_uuid;
            req.lastPublicPos = rd.last_public_zone_pos;
            req.expectAreaNode = Service.SelfAddress.ServiceNode;
            req.IsDisconnect = Service.Disconnect;
        }


        protected virtual Task<RoleEnterZoneResponse> RequestEnterDefaultZoneAsync()
        {
            // 寻找一个场景 //
            var rd = RoleZoneMapping.Data;
            if (rd != null && rd.last_zone_template_id == 0)
            {
                return this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
                {
                    expectZoneTemplateID = MMOServerManager.Battle.DataRoot.Templates.DefaultConfig.DEFAULT_SCENE,
                });
            }
            else
            {
                return this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
                {
                    expectZoneTemplateID = rd.last_zone_template_id,
                    expectZoneUUID = rd.last_zone_uuid,
                    roleScenePos = rd.last_zone_pos,
                });
            }
        }
        /// <summary>
        /// 请求进入场景
        /// </summary>
        protected virtual async Task<RoleEnterZoneResponse> RequestEnterZoneAsync(RoleEnterZoneRequest req)
        {
            BeginEnterZone(req);
            var result = await this.areaManager.CallAsync<RoleEnterZoneResponse>(req);
            if (RoleEnterZoneResponse.CheckSuccess(result))
            {
                this.currentArea = await Service.Provider.GetAsync(new RemoteAddress(result.areaName, result.areaNode));
                //变更过场景，场景线缓存失效.
                this.mZoneInfoDirtyFlag = true;
                this.SaveEnterZoneInfo(result);
                event_OnEnterZone?.Invoke(req, result);
            }
            else
            {
                log.Error("Enter Zone Error : " + result);
            }
            return result;
        }

        protected virtual async Task<RoleLeaveZoneResponse> RequestLeaveZoneAsync()
        {
            var rd = RoleZoneMapping.Data;
            var request = new RoleLeaveZoneRequest()
            {
                zoneUUID = rd.last_zone_uuid,
                roleID = rd.uuid,
                keepObject = false,
            };
            var result = await this.areaManager.CallAsync<RoleLeaveZoneResponse>(request);
            if (result != null && RoleLeaveZoneResponse.CheckSuccess(result))
            {
                this.SaveLeaveZoneInfo(result);
                event_OnLeaveZone?.Invoke(request, result);
            }
            return result;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------
        #region RPC

        /// <summary>
        /// Area通知逻辑需要传送操作，一般是踩到场景传送点
        /// </summary>
        /// <param name="tp"></param>
        [RpcHandler(typeof(RoleNeedTransportNotify))]
        public virtual async Task area_rpc_Handle(RoleNeedTransportNotify tp)
        {
            var leave_result = await RequestLeaveZoneAsync();
            if (leave_result == null || Response.CheckSuccess(leave_result) == false)
            {
                return;
            }
            var rd = RoleZoneMapping.Data;
            await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                serverGroupID = Service.ServerGroupID,
                serverID = Service.ServerID,
                expectZoneTemplateID = tp.nextMapID,
                roleScenePos = new ZonePosition() { flagName = tp.nextZoneFlagName },
            });
        }

        /// <summary>
        /// Area通知逻辑服无缝切场景
        /// </summary>
        /// <param name="tp"></param>
        [RpcHandler(typeof(RoleCrossMapNotify))]
        public virtual async Task area_rpc_Handle(RoleCrossMapNotify notify)
        {
            var leave_result = await RequestLeaveZoneAsync();
            if (leave_result == null || Response.CheckSuccess(leave_result) == false)
            {
                return;
            }
            var rd = RoleZoneMapping.Data;
            await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                serverGroupID = Service.ServerGroupID,
                serverID = Service.ServerID,
                expectZoneTemplateID = notify.NextSceneID,
                roleScenePos = new ZonePosition()
                {
                    x = notify.NextScenePos.x,
                    y = notify.NextScenePos.y,
                    z = notify.NextScenePos.z
                },
            });
        }

        /// <summary>
        /// Area通知逻辑服游戏结束
        /// </summary>
        /// <param name="notify"></param>
        /// <returns></returns>
        [RpcHandler(typeof(AreaGameOverNotify))]
        public virtual Task area_rpc_Handle(AreaGameOverNotify notify)
        {
            return Task.CompletedTask;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler]
        public virtual async Task<ClientEnterZoneResponse> client_rpc_Handle(ClientEnterZoneRequest rpc)
        {

            var svc = await RequestEnterDefaultZoneAsync();
            if (svc != null)
            {
                return new ClientEnterZoneResponse() { };
            }
            else
            {
                return new ClientEnterZoneResponse() { s2c_code = Response.CODE_ERROR };
            }
        }


        [RpcHandler(typeof(ClientGetZoneInfoSnapRequest), typeof(ClientGetZoneInfoSnapResponse))]
        public virtual async Task<ClientGetZoneInfoSnapResponse> client_rpc_Handle(ClientGetZoneInfoSnapRequest req)
        {
            var rsp = new ClientGetZoneInfoSnapResponse();
            var rd = RoleZoneMapping.Data;
            rsp.s2c_curZoneUUID = rd.last_zone_uuid;
            //同场景同线内使用缓存的线数据.
            if (mZoneInfoDirtyFlag == true || (DateTime.UtcNow - mLastGetZoneInfoTimeStamp).TotalMilliseconds > 0)
            {
                var ret = await areaManager.CallAsync<GetZonesInfoResponse>(new GetZonesInfoRequest()
                {
                    servergroupID = Service.ServerGroupID,
                    mapID = rd.last_zone_template_id,
                });
                rsp.s2c_snaps = ret.snaps;
                mLastZoneInfoSnaps = ret.snaps;
                mZoneInfoDirtyFlag = false;
                mLastGetZoneInfoTimeStamp = DateTime.UtcNow.AddMinutes(2);
            }
            else
            {
                rsp.s2c_snaps = mLastZoneInfoSnaps;
            }

            return rsp;
        }

        [RpcHandler(typeof(ClientChangeZoneLineRequest), typeof(ClientChangeZoneLineResponse))]
        public virtual async Task<ClientChangeZoneLineResponse> client_rpc_Handle(ClientChangeZoneLineRequest req)
        {
            var rsp = new ClientChangeZoneLineResponse();
            var rd = RoleZoneMapping.Data;
            //var mapData = GateServerManager.Battle.GetSceneAsCache(rd.last_map_template_id);
            if (mLastZoneInfoSnaps == null || rd.last_zone_uuid == req.c2s_zoneuuid)
            {
                rsp.s2c_code = ClientChangeZoneLineResponse.CODE_ERROR;
                return rsp;
            }
            for (int i = 0; i < mLastZoneInfoSnaps.Count; i++)
            {
                var snap = mLastZoneInfoSnaps[i];
                if (req.c2s_zoneuuid == snap.uuid)
                {
                    if (snap.curPlayerCount >= snap.playerMaxCount)
                    {
                        rsp.s2c_code = ClientChangeZoneLineResponse.CODE_LINE_BUSY;
                        return rsp;
                    }
                    else
                    {
                        var rsp2 = await RequestLeaveZoneAsync();
                        if (rsp2.IsSuccess)
                        {
                            // 寻找一个场景 //
                            var rsp3 = await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
                            {
                                expectZoneUUID = req.c2s_zoneuuid,
                                expectZoneTemplateID = rd.last_zone_template_id,
                                roleScenePos = rsp2.lastScenePos,
                            });
                            if (rsp3.IsSuccess)
                            {
                                return rsp;
                            }
                        }
                    }
                }
            }
            rsp.s2c_code = ClientChangeZoneLineResponse.CODE_NOT_EXIST;
            return rsp;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(SessionBeginLeaveRequest), typeof(SessionBeginLeaveResponse))]
        public virtual void session_rpc_Handle(SessionBeginLeaveRequest disconnect, OnRpcReturn<SessionBeginLeaveResponse> cb)
        {
            var area = currentArea;
            if (area != null)
            {
                area.Call(disconnect, cb);
            }
            else
            {
                cb(new SessionBeginLeaveResponse() { s2c_code = Response.CODE_ERROR });
            }
        }

        #endregion

        //--------------------------------------------------------------------------------------------------------------------------------------



        //---------------------------------------------------------------------------------------------------------------------------------------
        #region Events

        public delegate void PlayerEnterZoneHandler(RoleEnterZoneRequest req, RoleEnterZoneResponse result);
        public delegate void PlayerLeaveZoneHandler(RoleLeaveZoneRequest req, RoleLeaveZoneResponse result);
        public event PlayerEnterZoneHandler OnEnterZone { add { event_OnEnterZone += value; } remove { event_OnEnterZone += value; } }
        public event PlayerLeaveZoneHandler OnLeaveZone { add { event_OnLeaveZone += value; } remove { event_OnLeaveZone += value; } }
        private PlayerEnterZoneHandler event_OnEnterZone;
        private PlayerLeaveZoneHandler event_OnLeaveZone;
        protected virtual void DisposingEvents()
        {
            event_OnEnterZone = null;
            event_OnLeaveZone = null;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------
    }
}
