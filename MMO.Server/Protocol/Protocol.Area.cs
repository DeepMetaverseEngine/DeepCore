using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using Gate.Data;
using System;
using System.Collections.Generic;
using System.Text;
namespace Gate.Server.Protocol
{
    public class RoleDataChangedNotify : Notify
    {
        public string roleID;
        public ISerializable roleData;
    }
    //---------------------------------------------------------------------------------

    //     [ProtocolRoute("Area", "Logic")]
    //     public class AreaSaveRoleDataNotify : Notify
    //     {
    //         public string roleID;
    //         public ISerializable roleData;
    //     }
    // 
    //     [ProtocolRoute("Area", "Logic")]
    //     public class AreaSaveRoleInfoNotify : Notify
    //     {
    //     }

    /// <summary>
    /// 当玩家踩到传送点，或者场景内其他传送事件 
    /// Area通知逻辑需要传送操作，一般是踩到场景传送点
    /// </summary>
    public class RoleNeedTransportNotify : Notify
    {
        public int nextMapID;
        public int nextZoneID;
        public string nextZoneFlagName;
        public string fromAreaName;
        public string fromAreaNode;
    }

    /// <summary>
    /// 角色所在场景Game Over后，推送给Logic
    /// 通常推送后，当前场景即将删除，
    /// Logic在收到后，将玩家传送到最后的公共场景或者主城
    /// </summary>
    public class AreaGameOverNotify : Notify
    {
        public int zoneTemplateID;
        public string zoneUUID;
        public byte winForce;
        public string message;
        // TODO some award
        public ISerializable expandData;
    }
    //---------------------------------------------------------------------------------

    /// <summary>
    /// 通知Session
    /// </summary>
    public class SessionBindAreaNotify : Notify
    {
        public string areaName;
        public string areaNode;
    }
    /// <summary>
    /// 通知Session
    /// </summary>
    public class SessionUnbindAreaNotify : Notify
    {
        public string areaName;
        public string areaNode;
    }

    /// <summary>
    /// 单位奖励信息.
    /// </summary>
    public class RoleBattleAwardNotify : Notify
    {
        public class AwardItem : ISerializable
        {
            public int ItemTemplateID;
            public int ItemCount;
        }

        public string RoleID;
        public int MonsterID;
        public List<AwardItem> Awards;
    }

    /// <summary>
    /// 角色穿越地图通知(无缝切图)
    /// </summary>
    public class RoleCrossMapNotify : Notify
    {
        public int NextSceneID;
        public ZonePosition NextScenePos;
    }


    //     /// <summary>
    //     /// 寻找一个Area
    //     /// </summary>
    //     [ProtocolRoute("*", "AreaManager")]
    //     public class LookingForAreaRequest : Request
    //     {
    //         /// <summary>
    //         /// 预期的Area服务地址
    //         /// </summary>
    //         public string expectAreaName;
    //         /// <summary>
    //         /// 预期的Area服务地址
    //         /// </summary>
    //         public string expectAreaNode;
    //         /// <summary>
    //         /// 预期的场景
    //         /// </summary>
    //         public int expectSceneTemplateID;
    //         /// <summary>
    //         /// 预期的具体战斗场景
    //         /// </summary>
    //         public string expectZoneUUID;
    //     }
    // 
    //     [ProtocolRoute("AreaManager", "*")]
    //     public class LookingForAreaResponse : Response
    //     {
    //         /// <summary>
    //         /// 返回Area服务地址
    //         /// </summary>
    //         public string areaName;
    //         /// <summary>
    //         /// 返回Area服务地址
    //         /// </summary>
    //         public string areaNode;
    //         /// <summary>
    //         /// 返回场景UUID
    //         /// </summary>
    //         public string zoneUUID;
    //     }
    //---------------------------------------------------------------------------------

    public class RegistAreaRequest : Request
    {
        public string areaName;
        public string areaNode;
    }


    public class RegistAreaResponse : Response { }

    public class AreaStateNotify : Notify
    {
        public string areaName;
        public string areaNode;
        public long memoryMB;
        public float cpuPercent;
        public int zoneCount;
        public int roleCount;

        public override string ToString()
        {
            return "AreaStateNotify:" +
                        "\n    areaName=" + areaName +
                        "\n    areaNode=" + areaNode +
                        "\n   roleCount=" + roleCount +
                        "\n   zoneCount=" + zoneCount +
                        "\n  cpuPercent=" + cpuPercent +
                        "\n    memoryMB=" + memoryMB;
        }
    }
    //---------------------------------------------------------------------------------

    public class RoleEnterZoneRequest : Request
    {
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string serverID;
        /// <summary>
        /// 服务器组ID.
        /// </summary>
        public string serverGroupID;
        /// <summary>
        /// 预期的Area服务地址
        /// </summary>
        public string expectAreaName;
        /// <summary>
        /// 预期的Area服务地址
        /// </summary>
        public string expectAreaNode;
        /// <summary>
        /// 预期的场景
        /// 如果为空，表示不知道在哪个场景，一般是第一次注册用户
        /// </summary>
        public int expectZoneTemplateID;
        /// <summary>
        /// 预期的具体战斗场景
        /// 如果为空，表示没有确定的实体场景
        /// </summary>
        public string expectZoneUUID;
        /// <summary>
        /// 自定义玩家groupKey，groupkey相同进同一场景
        /// </summary>
        public string roomKey;
        /// <summary>
        /// 角色
        /// </summary>
        public string roleUUID;
        public string roleSessionName;
        public string roleSessionNode;
        public string roleLogicName;
        public string roleLogicNode;
        public int roleForce;
        public int roleUnitTemplateID;
        public string roleDisplayName;
        public ZonePosition roleScenePos;
        public ISerializable LastZoneSaveData;
        public ISerializable roleData;


        public string reason;
        /// <summary>
        /// 预期线.
        /// </summary>
        public int expectLineIndex;
        /// <summary>
        /// 上一次的公共场景.
        /// </summary>
        public int lastPublicZoneID;
        /// <summary>
        /// 上一次的公共场景UUID.
        /// </summary>
        public string lastPublicMapUUID;
        /// <summary>
        /// 公共场景坐标.
        /// </summary>
        public ZonePosition lastPublicPos;
        /// <summary>
        /// 扩展数据.
        /// </summary>
        public HashMap<string, string> ext;
        /// <summary>
        /// 断线状态
        /// </summary>
        public bool IsDisconnect;
    }

    public class RoleEnterZoneResponse : Response
    {
        [MessageCode("重新进入")]
        public const int CODE_OK_REPLACE = CODE_OK + 1;
        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色已在场景中")]
        public const int CODE_ROLE_ALREADY_IN_ZONE = CODE_ERROR + 2;
        [MessageCode("场景未开放")]
        public const int CODE_ZONE_NOT_OPEN = CODE_ERROR + 3;
        [MessageCode("场景已关闭")]
        public const int CODE_ZONE_CLOSED = CODE_ERROR + 4;

        [DependOnProperty(nameof(IsSuccess))]
        public string zoneUUID;
        [DependOnProperty(nameof(IsSuccess))]
        public int zoneTemplateID;
        [DependOnProperty(nameof(IsSuccess))]
        public int roleUnitTemplateID;
        [DependOnProperty(nameof(IsSuccess))]
        public string roleDisplayName;
        [DependOnProperty(nameof(IsSuccess))]
        public ISerializable roleBattleData;
        [DependOnProperty(nameof(IsSuccess))]
        public ZonePosition roleScenePos;
        [DependOnProperty(nameof(IsSuccess))]
        public string areaName;
        [DependOnProperty(nameof(IsSuccess))]
        public string areaNode;
    }
    //---------------------------------------------------------------------------------

    public class RoleLeaveZoneRequest : Request
    {
        public string zoneUUID;
        public string roleID;
        public bool keepObject;
        public string reason;
    }

    public class RoleLeaveZoneResponse : Response
    {
        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色不存在")]
        public const int CODE_ROLE_NOT_EXIST = CODE_ERROR + 2;
        /// <summary>
        /// 最后存在场景坐标
        /// </summary>
        public ZonePosition lastScenePos;
        public ISerializable expandData;
        public ISerializable LeaveZoneSaveData;

        public int curHP;
        public int curMP;
    }

    //---------------------------------------------------------------------------------

    public class CreateZoneNodeRequest : Request
    {
        public string serverID;
        public string serverGroupID;
        public string expectAreaNode;

        public int zoneTemplateID;
        public string managerZoneUUID;
        public string reason;

        public string createRoleID;

        /// <summary>
        /// 自定义玩家roomKey，roomKey相同进同一场景
        /// </summary>
        public string roomKey;
        /// <summary>
        /// 扩展数据.
        /// </summary>
        public ISerializable expandData;
    }

    public class CreateZoneNodeResponse : Response
    {
        [MessageCode("地图尚未开放！")]
        public const int CODE_ERROR_MAP_NOT_OPEN = 501;
        public string zoneUUID;
        public string areaName;
        public string areaNode;
        public int TemplateID;
    }

    public class DestoryZoneNodeRequest : Request
    {
        public string zoneUUID;
        public string reason;
    }

    public class DestoryZoneNodeResponse : Response
    {

    }

    public class AreaZoneGameOverNotify : Notify
    {
        public string zoneUUID;
        public string reason;
    }


    public class AreaZoneDestoryNotify : Notify
    {
        public string zoneUUID;
        public string reason;
    }
    //---------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------

    public class GetAllRoleRequest : Request
    {

    }

    public class GetAllRoleResponse : Response
    {
        public HashMap<string, OnlinePlayerData> uuidMap;
    }


    public class QueryZoneAreaNameRequest : Request
    {
        public string zoneUUID;
    }


    public class QueryZoneAreaNameResponse : Response
    {
        public string areaName;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    public class GetZonesInfoRequest : Request
    {
        public string servergroupID;
        public int mapID;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    public class GetZonesInfoResponse : Response
    {
        public List<ZoneInfoSnap> snaps;
    }

    public class GetRolePositionRequest : Request
    {
        //        public string zoneUUID;
        public string roleUUID;
    }

    public class GetRolePositionResponse : Response
    {
        public int zoneId;
        public string zoneUUID;
        public float x;
        public float y;
        public float z;
        public int line;

        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色不存在")]
        public const int CODE_ROLE_NOT_EXIST = CODE_ERROR + 2;
    }


    public class RoleNameChangedNotify : Notify
    {
        public string roleId;
        public string newName;
    }


    public class BatchCreateZoneLineRequest : Request
    {
        public List<CreateZoneNodeRequest> zoneList;
    }

    public class BatchCreateZoneLineResponse : Response
    {
        public List<ZoneInfoSnap> zoneList;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    public class GetBatchZonesInfoRequest : Request
    {
        public string servergroupID;
        public List<int> mapIDList;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    public class GetBatchZonesInfoResponse : Response
    {
        public HashMap<int, List<ZoneInfoSnap>> snapDic;
    }

    //当前秘境玩法状态更新通知
    public class CurrentSecretPlacePlayTypeUpdateNotify : Notify
    {
        public string zoneUUID;
        public int update;
        public int playID; //玩法ID
    }

}
