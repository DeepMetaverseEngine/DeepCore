using DeepCore;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Data;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Gate.Server.Service.Area
{
    public class AreaManager : IService
    {
        public AreaManager(ServiceStartInfo start) : base(start)
        {
            this.areas = new ValueSortedMap<string, ValueSortedMap<string, AreaInfo>>(AreaGroupComparison);
        }
        protected override void OnDisposed()
        {
        }
        protected override Task OnStartAsync()
        {
            return Task.FromResult(0);
        }
        protected override Task OnStopAsync()
        {
            return Task.FromResult(0);
        }

        public virtual void AreaSyncState(AreaStateNotify st)
        {
            if (areas.TryGetValue(st.areaNode, out var group))
            {
                areas.MarkSort();
                if (group.TryGetValue(st.areaName, out var area))
                {
                    group.MarkSort();
                    area.state = st;
                }
            }
        }
        public override bool GetState(TextWriter sb)
        {
            sb.WriteLine(CUtils.SequenceChar('-', 100));
            foreach (var group in areas.ToSortedArray())
            {
                foreach (var area in group.Value.ToSortedArray())
                {
                    area.Value.WriteState(sb);
                }
            }
            sb.WriteLine(CUtils.SequenceChar('-', 100));
            return true;
        }


        //------------------------------------------------------------------------------------------------------------------------------------
        #region rpc_from_AreaService
        /// <summary>
        /// 添加一个Area负载
        /// </summary>
        /// <returns></returns>
        [RpcHandler(typeof(RegistAreaRequest), typeof(RegistAreaResponse))]
        public virtual Task<RegistAreaResponse> area_rpc_RegistAreaAsync(RegistAreaRequest reg)
        {
            return AreaRegist(reg);
        }

        [RpcHandler(typeof(AreaStateNotify))]
        public virtual void area_rpc_AreaStateNotify(AreaStateNotify notify)
        {
            AreaSyncState(notify);
        }

        [RpcHandler(typeof(AreaZoneGameOverNotify))]
        public virtual void area_rpc_AreaGameOverHandle(AreaZoneGameOverNotify stop)
        {
            //log.Info("AreaMgr receive " + stop + " " + stop.zoneUUID);
            zones.SetZoneCloseFlag(stop.zoneUUID);
        }

        [RpcHandler(typeof(AreaZoneDestoryNotify))]
        public virtual void area_rpc_AreaGameOverHandle(AreaZoneDestoryNotify stop)
        {
            //TODO flag destory 
            //log.Info("AreaMgr recive " + stop + " " + stop.zoneUUID);
            DestoryZone(stop);
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------
        #region rpc_from_LogicService

        [RpcHandler(typeof(BatchCreateZoneLineRequest), typeof(BatchCreateZoneLineResponse))]
        public virtual Task<BatchCreateZoneLineResponse> logic_rpc_Handle(BatchCreateZoneLineRequest create)
        {
            //log.Info(create);
            return BatchCreateZoneLine(create);
        }

        [RpcHandler(typeof(CreateZoneNodeRequest), typeof(CreateZoneNodeResponse))]
        public virtual Task<CreateZoneNodeResponse> logic_rpc_Handle(CreateZoneNodeRequest create)
        {
            //log.Info(create);
            return CreateZone(create);
        }
        [RpcHandler(typeof(DestoryZoneNodeRequest), typeof(DestoryZoneNodeResponse))]
        public virtual Task<DestoryZoneNodeResponse> logic_rpc_Handle(DestoryZoneNodeRequest stop)
        {
            //log.Info(stop);
            return DestoryZone(stop);
        }
        [RpcHandler(typeof(RoleEnterZoneRequest), typeof(RoleEnterZoneResponse))]
        public virtual Task<RoleEnterZoneResponse> logic_rpc_Handle(RoleEnterZoneRequest req)
        {
            //log.Info(req);
            return RoleEnter(req);
        }
        [RpcHandler(typeof(RoleLeaveZoneRequest), typeof(RoleLeaveZoneResponse))]
        public virtual Task<RoleLeaveZoneResponse> logic_rpc_Handle(RoleLeaveZoneRequest req)
        {
            //log.Info(req);
            return RoleLeave(req);
        }


        [RpcHandler(typeof(RoleNameChangedNotify))]
        public virtual void logic_rpc_Handle(RoleNameChangedNotify ntf)
        {
            var role = GetRole(ntf.roleId);
            if (role != null)
            {
                role.enter.roleDisplayName = ntf.newName;
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(GetAllRoleRequest), typeof(GetAllRoleResponse))]
        public virtual void logic_rpc_Handle(GetAllRoleRequest req, OnRpcReturn<GetAllRoleResponse> cb)
        {
            var roleList = GetAllRoles();
            var uuidMap = new HashMap<string, OnlinePlayerData>();
            for (int i = 0; i < roleList.Length; ++i)
            {
                var rold = roleList[i];
                uuidMap.Add(rold.uuid, new OnlinePlayerData()
                {
                    name = rold.enter.roleDisplayName,
                    serverGroupId = rold.enter.serverGroupID
                });
            }
            cb(new GetAllRoleResponse() { uuidMap = uuidMap });
        }

        [RpcHandler(typeof(QueryZoneAreaNameRequest), typeof(QueryZoneAreaNameResponse))]
        public virtual Task<QueryZoneAreaNameResponse> logic_rpc_Handle(QueryZoneAreaNameRequest req)
        {
            var zone = GetZone(req.zoneUUID);
            return Task.FromResult(new QueryZoneAreaNameResponse
            {
                s2c_code = zone != null ? Response.CODE_OK : Response.CODE_ERROR,
                areaName = zone?.area.key
            });
        }


        [RpcHandler(typeof(GetZonesInfoRequest), typeof(GetZonesInfoResponse))]
        public virtual void logic_rpc_Handle(GetZonesInfoRequest req, OnRpcReturn<GetZonesInfoResponse> cb)
        {
            GetZonesInfoResponse rsp = new GetZonesInfoResponse();
            //指定场景的所有线.
            var lt = GetZoneList(req.servergroupID, req.mapID);
            if (lt != null)
            {
                List<ZoneInfoSnap> snaps = new List<ZoneInfoSnap>();
                rsp.snaps = snaps;
                for (int i = 0; i < lt.Count; i++)
                {
                    //获取所有线的信息.
                    var info = lt[i];
                    if (info != null && info.close == false)
                    {
                        snaps.Add(info.ToSnap());
                    }
                }
            }
            cb(rsp);
        }

        /// <summary>
        /// 批量获取场景分线
        /// </summary>
        /// <param name="req"></param>
        /// <param name="cb"></param>

        [RpcHandler(typeof(GetBatchZonesInfoRequest), typeof(GetBatchZonesInfoResponse))]
        public virtual void logic_rpc_Handle(GetBatchZonesInfoRequest req, OnRpcReturn<GetBatchZonesInfoResponse> cb)
        {
            GetBatchZonesInfoResponse rsp = new GetBatchZonesInfoResponse();
            rsp.snapDic = new HashMap<int, List<ZoneInfoSnap>>();

            foreach (var item in req.mapIDList)
            {
                var lt = GetZoneList(req.servergroupID, item);

                if (lt != null)
                {
                    ZoneInfoSnap snap = null;
                    ZoneInfo info = null;

                    List<ZoneInfoSnap> snaps = new List<ZoneInfoSnap>();

                    for (int i = 0; i < lt.Count; i++)
                    {
                        //获取所有线的信息.
                        info = lt[i];
                        if (info != null && info.close == false)
                        {
                            snaps.Add(info.ToSnap());
                        }
                    }
                    rsp.snapDic.Add(item, snaps);
                }
            }
            cb(rsp);
        }


        /// <summary>
        /// 接收到RPC请求的入口函数
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [RpcHandler(typeof(GetRolePositionRequest), typeof(GetRolePositionResponse))]
        public virtual async Task<GetRolePositionResponse> logic_rpc_Handle(GetRolePositionRequest req)
        {
            if (roles.ContainsKey(req.roleUUID) == false)
            {
                return new GetRolePositionResponse()
                {
                    s2c_code = GetRolePositionResponse.CODE_ROLE_NOT_EXIST
                };
            }

            var role = roles.Get(req.roleUUID);
            var zone = role.zone;
            if (zone == null)
            {
                return (new GetRolePositionResponse() { s2c_code = GetRolePositionResponse.CODE_ZONE_NOT_EXIST });
            }

            var resp = await zone.area.service.CallAsync<GetRolePositionResponse>(req);
            resp.line = zone.lineIndex;
            resp.zoneId = zone.templateID;
            resp.zoneUUID = zone.uuid;
            return resp;
        }


        #endregion
        //--------------------------------------------------

        //------------------------------------------------------------------------------------------------------------------------------------
        #region ZoneManager

        private readonly ValueSortedMap<string, ValueSortedMap<string, AreaInfo>> areas;
        private readonly HashMap<string, RoleInfo> roles = new HashMap<string, RoleInfo>();
        private readonly ZoneMap zones = new ZoneMap();

        public virtual async Task<RoleEnterZoneResponse> RoleEnter(RoleEnterZoneRequest req)
        {
            if (roles.ContainsKey(req.roleUUID))
            {
                return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ROLE_ALREADY_IN_ZONE });
            }
            var roleInfo = new RoleInfo(req.roleUUID, req);
            //先添加以防止二次进入//
            roles.Add(roleInfo.uuid, roleInfo);
            try
            {
                var zone = await this.LookingForExpectZone(req);
                if (zone == null || zone.close)
                {
                    roles.Remove(req.roleUUID);
                    return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ZONE_NOT_EXIST });
                }
                //--------------------------------------------------------------------------------
                //分配线.
                req.expectLineIndex = zone.lineIndex;
                req.expectZoneUUID = zone.uuid;
                //--------------------------------------------------------------------------------
                var rsp = await zone.area.service.CallAsync<RoleEnterZoneResponse>(req);
                if (Response.CheckSuccess(rsp))
                {
                    zone.currentRoleCount++;
                    zone.area.currentRoleCount++;
                    roleInfo.zone = zone;
                    roleInfo.response = rsp;
                }
                else
                {
                    roles.Remove(req.roleUUID);
                    log.Error(rsp);
                }
                return rsp;
            }
            catch (Exception err)
            {
                roles.Remove(req.roleUUID);
                return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }
        public virtual async Task<RoleLeaveZoneResponse> RoleLeave(RoleLeaveZoneRequest req)
        {
            var role = roles.RemoveByKey(req.roleID);
            if (role == null)
            {
                return (new RoleLeaveZoneResponse() { s2c_code = RoleLeaveZoneResponse.CODE_ROLE_NOT_EXIST });
            }
            var zone = role.zone;
            if (zone == null)
            {
                return (new RoleLeaveZoneResponse() { s2c_code = RoleLeaveZoneResponse.CODE_ZONE_NOT_EXIST });
            }
            //最后才删除//
            zone.currentRoleCount--;
            zone.area.currentRoleCount--;
            req.zoneUUID = zone.uuid;
            var rsp = await zone.area.service.CallAsync<RoleLeaveZoneResponse>(req);
            if (!Response.CheckSuccess(rsp))
            {
                log.Error(rsp);
            }
            return rsp;
        }

        public virtual async Task<ZoneInfo> LookingForExpectZone(RoleEnterZoneRequest req)
        {
            ZoneInfo zone = null;
            #region 返回上一个场景.

            //ROOMKEY.
            if (!string.IsNullOrEmpty(req.roomKey))
            {
                zone = GetRoomZone(req.roomKey);
                if (zone != null && !zone.close)
                {
                    return (zone);
                }
            }
            //返回上一次的场景，如果没有统一返回上一次的公共场景.
            if (!string.IsNullOrEmpty(req.expectZoneUUID))
            {
                //根据提供的UUID寻找场景//
                zone = GetZone(req.expectZoneUUID);
                if (zone != null && !zone.close)
                {
                    if (zone.IsMax == false)
                    {
                        return (zone);
                    }
                }

                return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicZoneID, req.lastPublicPos, req);
            }
            #endregion
            else
            {
                var temp = MMOServerManager.Battle.GetSceneAsCache(req.expectZoneTemplateID);
                if (temp == null)//场景不存在，往公共场景扔.
                {
                    zone = await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicZoneID, new ZonePosition(), req);
                    if (zone == null)
                    {
                        return await LookingForDefaultZone(req);
                    }
                }
                //根据EXPECT MAP来.
                if (temp.IsPublicMap)
                {
                    return await LookingForPublicZone(null, req.expectZoneTemplateID, req.roleScenePos, req);
                }
                else
                {
                    //新创建一个场景//
                    var rsp = await this.CreateZone(new CreateZoneNodeRequest()
                    {
                        //serverID = req.serverID,
                        serverGroupID = req.serverGroupID,
                        zoneTemplateID = temp.ID,
                        createRoleID = req.roleUUID,
                        reason = nameof(LookingForExpectZone),
                        roomKey = req.roomKey,
                        expectAreaNode = req.roleSessionNode,
                    });
                    if (Response.CheckSuccess(rsp))
                    {
                        return (GetZone(rsp.zoneUUID));
                    }
                    else
                    {
                        return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicZoneID, req.lastPublicPos, req);
                    }
                }
            }
        }
        protected virtual async Task<ZoneInfo> LookingForDefaultZone(RoleEnterZoneRequest req)
        {
            //找当前存在的公共场景.
            var temp = MMOServerManager.Battle.GetSceneAsCache(MMOServerManager.Battle.DataRoot.Templates.DefaultConfig.DEFAULT_SCENE);
            if (temp == null)
            {
                return (null);
            }
            //创建一个公共场景.
            var rsp = await this.CreateZone(new CreateZoneNodeRequest()
            {
                serverGroupID = req.serverGroupID,
                zoneTemplateID = temp.ID,
                reason = nameof(LookingForDefaultZone),
                roomKey = req.roomKey,
                expectAreaNode = req.roleSessionNode,
            });
            if (Response.CheckSuccess(rsp))
            {
                return (GetZone(rsp.zoneUUID));
            }
            else
            {
                return (null);
            }
        }
        protected virtual async Task<ZoneInfo> LookingForPublicZone(string publicmapUUID, int publicmapID, ZonePosition pos, RoleEnterZoneRequest req)
        {
            //没有统一返回上一次的公共场景.
            req.roleScenePos = pos;
            ZoneInfo zone = GetZone(publicmapUUID);
            if (zone != null && !zone.close)
            {
                if (zone.IsMax == false)
                {
                    return (zone);
                }
            }
            //找当前存在的公共场景.
            var temp = MMOServerManager.Battle.GetSceneAsCache(publicmapID);
            if (temp == null)
            {
                return (null);
            }
            zone = LookingForExpectServerGroupZone(req.serverGroupID, z =>
            {
                return (!z.IsFull) && (z.templateID == temp.ID);
            }, (a, b) =>
            {
                if (req.roleSessionNode == a.nodeName) return -1;
                if (req.roleSessionNode == b.nodeName) return 1;
                return 0;
            });
            if (zone != null && !zone.close)
            {
                return (zone);
            }
            //创建一个公共场景.
            var rsp = await this.CreateZone(new CreateZoneNodeRequest()
            {
                serverGroupID = req.serverGroupID,
                zoneTemplateID = temp.ID,
                reason = nameof(LookingForPublicZone),
                roomKey = req.roomKey,
                expectAreaNode = req.roleSessionNode,
            });
            if (Response.CheckSuccess(rsp))
            {
                return (GetZone(rsp.zoneUUID));
            }
            else
            {
                return (null);
            }
        }
        /// <summary>
        /// 根据地图ID，和Area名字选择合适Zone
        /// </summary>
        /// <param name="serverGroupID"></param>
        /// <param name="condition">必要条件</param>
        /// <param name="expect">可选条件</param>
        /// <returns></returns>
        protected ZoneInfo LookingForExpectServerGroupZone(string serverGroupID, Predicate<ZoneInfo> condition, Comparison<ZoneInfo> expect = null)
        {
            var map = zones.GetAllZones();
            if (map != null)
            {
                var zones = new List<ZoneInfo>(map);
                return LookingForExpectZone(zones, condition, expect);
            }
            return null;
        }
        /// <summary>
        /// 选取预期场景
        /// </summary>
        /// <param name="zones"></param>
        /// <param name="condition">必要条件</param>
        /// <param name="expect">可选条件</param>
        /// <returns></returns>
        protected virtual ZoneInfo LookingForExpectZone(List<ZoneInfo> zones, Predicate<ZoneInfo> condition, Comparison<ZoneInfo> expect = null)
        {
            for (int i = zones.Count - 1; i >= 0; --i)
            {
                var z = zones[i];
                if (z.close || !condition(z))
                {
                    zones.RemoveAt(i);
                }
            }
            if (zones.Count > 0)
            {
                if (expect != null)
                {
                    zones.Sort(expect);
                }
                return zones[0];
            }
            return null;
        }

        /// <summary>
        /// 负载均衡排序
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual int AreaComparison(AreaInfo a, AreaInfo b)
        {
            if (a.state == null) return -1;
            if (b.state == null) return 1;
            return a.state.roleCount - b.state.roleCount;
        }
        public virtual int AreaGroupComparison(ValueSortedMap<string, AreaInfo> a, ValueSortedMap<string, AreaInfo> b)
        {
            var ac = a.Values.Sum(area => area.currentRoleCount);
            var bc = b.Values.Sum(area => area.currentRoleCount);
            return ac - bc;
        }
        public virtual async Task<RegistAreaResponse> AreaRegist(RegistAreaRequest reg)
        {
            IRemoteService svc;
            try
            {
                svc = await base.Provider.GetAsync(new RemoteAddress(reg.areaName, reg.areaNode));
                if (svc != null)
                {
                    Console.WriteLine("   AreaManager areas 添加节点：" + svc.Address.ServiceNode + "   Name:" + svc.Address.ServiceName);
                    var node = areas.GetOrAdd(svc.Address.ServiceNode, (n) => new ValueSortedMap<string, AreaInfo>(AreaComparison));
                    node.TryAddOrUpdate(svc.Address.ServiceName, new AreaInfo(svc));
                    return new RegistAreaResponse();
                }
                else
                {
                    return new RegistAreaResponse() { s2c_code = RegistAreaResponse.CODE_ERROR };
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }
        /// <summary>
        /// 分配一个空闲的Area
        /// </summary>
        /// <param name="expectAreaName"></param>
        /// <returns></returns>
        public virtual AreaInfo AreaDispatch(string expectNodeName)
        {
            ////请求动态创建场景
            //if (areas.Count == 0)
            //{
            //    var getRemote = await ServerNames.GetOrCreateAreaServiceAsync(this, ServerGID);
            //    if (getRemote != null)
            //    {
            //        var node = areas.GetOrAdd(getRemote.Address.ServiceNode, (n) => new ValueSortedMap<string, AreaInfo>(AreaComparison));
            //        node.TryAddOrUpdate(getRemote.Address.ServiceName, new AreaInfo(getRemote));
            //    }
            //}
            
            if (areas.Count > 0)
            {
                if (expectNodeName != null && areas.TryGetValue(expectNodeName, out var group))
                {
                    if (group.Count > 0)
                    {
                        return group.First.Value;
                    }
                }
                group = areas.First.Value;
                if (group.Count > 0)
                {
                    return group.First.Value;
                }
            }

            throw new Exception("No Area !!!:::::");
        }
        /// <summary>
        /// 创建场景
        /// </summary>
        /// <param name="create"></param>
        /// <param name="cb"></param>
        public virtual async Task<CreateZoneNodeResponse> CreateZone(CreateZoneNodeRequest create)
        {
            //if (dungeon_scheduler.IsMapOpen(create.mapTemplateID))
            {
                var area = AreaDispatch(create.expectAreaNode);
                if (area != null)
                {
                    create.managerZoneUUID = (Guid.NewGuid().ToString());
                    //先添加以防止二次进入//
                    var scene_data = MMOServerManager.Battle.GetSceneAsCache(create.zoneTemplateID);
                    var info = new ZoneInfo(create.managerZoneUUID, area, scene_data, create.serverGroupID, create.expectAreaNode)
                    {
                        roomKey = create.roomKey
                    };
                    zones.AddZone(info);
                    area.currentZoneCount++;
                    var rsp = await area.service.CallAsync<CreateZoneNodeResponse>(create);
                    if (!Response.CheckSuccess(rsp))
                    {
                        zones.RemoveZone(create.managerZoneUUID);
                        area.currentZoneCount--;
                    }
                    else
                    {
                        //if (true)
                        {
                            log.InfoFormat("CreateZone: {0} : TotalZoneCount={1}", scene_data, zones.Count);
                        }
                    }
                    return rsp;
                }
                else
                {
                    return (new CreateZoneNodeResponse() { s2c_code = CreateZoneNodeResponse.CODE_ERROR, });
                }
            }
        }

        /// <summary>
        /// 销毁场景
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="cb"></param>
        public virtual async Task<DestoryZoneNodeResponse> DestoryZone(DestoryZoneNodeRequest stop)
        {
            ZoneInfo zone = zones.RemoveZone(stop.zoneUUID);
            if (zone != null)
            {
                //log.Log("DestoryZone: " + stop.zoneUUID + " " + stop);
                zones.RemoveZone(stop.zoneUUID);
                zone.area.currentZoneCount--;
                return await zone.area.service.CallAsync<DestoryZoneNodeResponse>(stop);
            }
            else
            {
                return (new DestoryZoneNodeResponse() { s2c_code = Response.CODE_ERROR, });
            }
        }

        /// <summary>
        /// 批量创建场景分线
        /// </summary>
        /// <param name="create"></param>
        /// <returns></returns>
        public virtual async Task<BatchCreateZoneLineResponse> BatchCreateZoneLine(BatchCreateZoneLineRequest create)
        {
            BatchCreateZoneLineResponse response = new BatchCreateZoneLineResponse();

            response.zoneList = new List<ZoneInfoSnap>();

            foreach (var item in create.zoneList)
            {
                var result = await CreateZone(item);
                ZoneInfoSnap zone = new ZoneInfoSnap();
                zone.lineIndex = GetZone(result.zoneUUID).lineIndex;
                zone.TemplateID = result.TemplateID;
                zone.uuid = result.zoneUUID;
                response.zoneList.Add(zone);
            }

            return response;
        }

        public virtual bool DestoryZone(AreaZoneDestoryNotify stop)
        {
            try
            {
                ZoneInfo zone = zones.RemoveZone(stop.zoneUUID);
                if (zone != null)
                {
                    //log.Log("DestoryZone: " + stop.zoneUUID + " " + stop);
                    zone.area.currentZoneCount--;
                    zone.area.service.Call<DestoryZoneNodeResponse>(new DestoryZoneNodeRequest()
                    {
                        reason = stop.reason,
                        zoneUUID = stop.zoneUUID
                    }, (rsp, err) =>
                    {
                        //log.Log("DestoryZone: " + stop.zoneUUID + " " + rsp);
                    });
                    return true;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw err;
            }
            return false;
        }
        public virtual RoleInfo GetRole(string roleID)
        {
            RoleInfo ret;
            if (roleID != null && roles.TryGetValue(roleID, out ret))
            {
                return ret;
            }
            return null;
        }
        public virtual ZoneInfo GetZone(string zoneUUID)
        {
            return zones.GetZone(zoneUUID);
        }

        public virtual ZoneInfo GetZone(int mapId, int lineId)
        {
            return zones.GetZone(mapId, lineId);
        }
        public ZoneInfo GetRoomZone(string roomKey)
        {
            return zones.GetRoomZone(roomKey);
        }
        public List<ZoneInfo> GetZoneList(string serverGroupID, int mapID)
        {
            return zones.GetZoneList(serverGroupID, mapID);
        }
        public RoleInfo[] GetAllRoles()
        {
            var ret = new List<RoleInfo>(roles.Values);
            return ret.ToArray();
        }
        public List<ZoneInfo> GetAllZones()
        {
            return zones.GetAllZones();
        }

        //------------------------------------------------------------------------------------------------------------------------------------
        public class AreaInfo
        {
            public readonly IRemoteService service;
            public readonly string key;
            public int currentRoleCount { get; internal set; }
            public int currentZoneCount { get; internal set; }
            public AreaStateNotify state { get; internal set; }
            public AreaInfo(IRemoteService svc)
            {
                this.service = svc;
                this.key = svc.Address.ServiceName;
                this.currentRoleCount = 0;
                this.currentZoneCount = 0;
            }
            public void WriteState(TextWriter sb)
            {
                sb.WriteLine("AreaState : " + service.Address.FullPath);
                sb.WriteLine("     role = " + currentRoleCount);
                sb.WriteLine("     zone = " + currentZoneCount);
                if (state != null)
                {
                    sb.WriteLine("      cpu = " + state.cpuPercent);
                    sb.WriteLine("   memory = " + state.memoryMB + "(MB)");
                }
            }
        }
        public class ZoneInfo
        {
            public readonly string uuid;
            public readonly AreaInfo area;
            public readonly SceneData scene_data;
            public int templateID { get => scene_data.ID; }
            public int currentRoleCount { get; internal set; }
            public string serverGroupID { get; internal set; }
            public int lineIndex { get; set; }
            public string roomKey { get; internal set; }
            public bool close { get; internal set; }
            public string nodeName { get; internal set; }
            public bool IsFull { get => (currentRoleCount >= scene_data.FullPlayer); }
            public bool IsMax { get => (currentRoleCount >= scene_data.MaxPlayer); }

            public ZoneInfo(string uuid, AreaInfo parent, SceneData sdata, string serverGroupID, string nodeName)
            {
                this.uuid = uuid;
                this.area = parent;
                this.scene_data = sdata;
                this.currentRoleCount = 0;
                this.serverGroupID = serverGroupID;
                this.lineIndex = 1;
                this.roomKey = roomKey;
                this.nodeName = nodeName;
            }
            public ZoneInfoSnap ToSnap()
            {
                var snap = new ZoneInfoSnap();
                snap.curPlayerCount = currentRoleCount;
                snap.playerMaxCount = scene_data.MaxPlayer;
                snap.playerFullCount = scene_data.FullPlayer;
                snap.lineIndex = lineIndex;
                snap.uuid = uuid;
                return snap;
            }
        }
        public class RoleInfo
        {
            public readonly string uuid;
            public readonly RoleEnterZoneRequest enter;
            public RoleEnterZoneResponse response;
            public ZoneInfo zone { get; internal set; }
            public RoleInfo(string uuid, RoleEnterZoneRequest req)
            {
                this.uuid = uuid;
                this.enter = req;
            }
        }
        public class ZoneMap
        {
            /// <summary>
            /// 所有场景信息<key:场景UID ,value:场景信息>
            /// </summary>
            private readonly Dictionary<string, ZoneInfo> zones = new Dictionary<string, ZoneInfo>();

            /// <summary>
            /// 所有场景信息<key:场景Id,value:<key:分线ID,value:分线信息>>
            /// </summary>
            private readonly Dictionary<int, Dictionary<int, ZoneInfo>> zonesLineMap = new Dictionary<int, Dictionary<int, ZoneInfo>>();

            /// <summary>
            /// 特殊场景信息 <key: 房间所有这生成房间ID，value：场景信息>>    
            /// </summary>
            private readonly Dictionary<string, ZoneInfo> roomZones = new Dictionary<string, ZoneInfo>();

            public int Count { get => zones.Count; }

            public void AddZone(ZoneInfo zone)
            {
                zones.Add(zone.uuid, zone);
                //---------------------------------------------------------------------------------------------
                if (zone.scene_data.IsPublicMap)
                {
                    Dictionary<int, ZoneInfo> lt = null;
                    if (!zonesLineMap.TryGetValue(zone.templateID, out lt))
                    {
                        lt = new Dictionary<int, ZoneInfo>();
                        zonesLineMap.Add(zone.templateID, lt);
                    }
                    AddLine(zone, lt);
                }
                //---------------------------------------------------------------------------------------------
                if (!string.IsNullOrEmpty(zone.roomKey))
                {
                    roomZones.Add(zone.roomKey, zone);
                }
            }

            public ZoneInfo GetRoomZone(string roomKey)
            {
                if (string.IsNullOrEmpty(roomKey))
                {
                    return null;
                }

                if (roomZones.TryGetValue(roomKey, out var ret))
                {
                    //if (ret.close != true)
                    return ret;
                }

                return null;
            }
            public ZoneInfo RemoveZone(string uuid)
            {
                if (string.IsNullOrEmpty(uuid))
                    return null;

                ZoneInfo info;
                if (zones.TryGetValue(uuid, out info))
                {
                    zones.Remove(uuid);
                    //---------------------------------------------------------------------------------------------
                    //分线表删除.
                    zonesLineMap.TryGetValue(info.templateID, out var lt);
                    RemoveLine(info.uuid, lt);
                    //---------------------------------------------------------------------------------------------
                    //公会场景表删除.
                    if (!string.IsNullOrEmpty(info.roomKey))
                    {
                        roomZones.Remove(info.roomKey);
                    }
                }
                return info;
            }
            public void Clear()
            {
                zones.Clear();

                zonesLineMap.Clear();
                roomZones.Clear();
            }
            public ZoneInfo GetZone(string uuid)
            {
                ZoneInfo ret = null;

                if (!string.IsNullOrEmpty(uuid) && zones.TryGetValue(uuid, out ret))
                {
                    return ret;
                }
                return null;
            }

            public ZoneInfo GetZone(int mapId, int lineId)
            {
                foreach (var item in zones)
                {
                    if (item.Value.templateID == mapId && item.Value.lineIndex == lineId)
                    {
                        return item.Value;
                    }
                }
                return null;
            }


            public bool TryGetValue(string uuid, out ZoneInfo zoneinfo)
            {
                return zones.TryGetValue(uuid, out zoneinfo);
            }
            public void SetZoneCloseFlag(string uuid)
            {
                var zone = GetZone(uuid);
                if (zone != null)
                    zone.close = true;
            }
            private void AddLine(ZoneInfo zone, Dictionary<int, ZoneInfo> lt)
            {

                if (lt == null)
                {
                    lt = new Dictionary<int, ZoneInfo>();
                    zone.lineIndex = 1;
                    lt.Add(zone.lineIndex, zone);//默认分线1
                    return;
                }
                else
                {
                    //遍历当前中所有分线信息
                    for (int i = 1; i <= lt.Count; i++)
                    {
                        //获取当前不存在的分线ID
                        if (!lt.ContainsKey(i))
                        {
                            zone.lineIndex = i;
                            lt.Add(zone.lineIndex, zone);
                            return;
                        }
                    }

                    //创建新的分线ID
                    zone.lineIndex = lt.Count + 1;
                    lt.Add(zone.lineIndex, zone);//默认分线1
                    return;

                    //bool insert = false;
                    //for (int i = 0; i < lt.Count; i++)
                    //{
                    //    if (lt[i] == null)
                    //    {
                    //        insert = true;
                    //        lt[i] = zone;
                    //        zone.lineIndex = i + 1;
                    //        break;
                    //    }
                    //}
                    //if (insert == false)
                    //{
                    //    lt.Add(zone.lineIndex, zone); 
                    //}
                }
            }
            private void RemoveLine(string uuid, Dictionary<int, ZoneInfo> lt)
            {
                if (lt == null || lt.Count == 0) return;
                foreach (var item in lt)
                {
                    if (item.Value.uuid == uuid)
                    {
                        lt.Remove(item.Key);
                        return;
                    }
                }
            }

            /// <summary>
            /// 获取当前Group内，指定场景的分线信息.
            /// </summary>
            /// <param name="serverGroupID"></param>
            /// <param name="mapID"></param>
            /// <returns></returns>
            public List<ZoneInfo> GetZoneList(string serverGroupID, int mapID)
            {

                Dictionary<int, ZoneInfo> lt;
                List<ZoneInfo> ret;
                if (zonesLineMap.TryGetValue(mapID, out lt))
                {
                    ret = new List<ZoneInfo>();
                    foreach (var item in lt)
                    {
                        ret.Add(item.Value);
                    }
                    return ret;
                }

                return null;
            }

            public List<ZoneInfo> GetZones(int templateID)
            {
                var all = new List<ZoneInfo>(this.zones.Values);
                var ret = new List<ZoneInfo>();
                foreach (var zoneInfo in all)
                {
                    if (zoneInfo.templateID == templateID /*&& zoneInfo.close != true*/)
                    {
                        ret.Add(zoneInfo);
                    }
                }
                return ret;
            }
            public List<ZoneInfo> GetAllZones()
            {
                return new List<ZoneInfo>(this.zones.Values);

                //var all = new List<ZoneInfo>(this.zones.Values);
                //var ret = new List<ZoneInfo>();
                //foreach (var zoneInfo in all)
                //{
                //    // if (zoneInfo.close != true)
                //    {
                //        ret.Add(zoneInfo);
                //    }
                //}
                //return ret;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------
    }
}
