
using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.SQL;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Gate.Data
{

    [MessageType(Constants.DATA_START + 1)]
    public class ClientInfo : ISerializable, IObjectMapping
    {
        [PersistField] public string userAgent;
        [PersistField] public string network;
        [PersistField] public string deviceId;
        [PersistField] public string deviceType;
        [PersistField] public string deviceModel;
        [PersistField] public string region;
        [PersistField] public string channel;
        [PersistField] public string subChannel;
        [PersistField] public string clientVersion;
        [PersistField] public string sdkVersion;
        [PersistField] public string sdkName;
        [PersistField] public string userSource1;
        [PersistField] public string userSource2;
        [PersistField] public string platformAcount;
        [PersistField] public string walletAddress;
        [PersistField] public string invateWalletAddress;

        [PersistField] public byte[] rawData;
        [PersistField] public string[] args;
        [PersistField] public List<byte> rawDataList;
        [PersistField] public List<string> argsList;

        public static ClientInfo LoadFrom(Properties cfg)
        {
            return cfg.LoadInstance<ClientInfo>();
        }
        public Properties SaveTo()
        {
            var ret = new Properties();
            ret.SaveFields(this);
            return ret;
        }
    }

    [MessageType(Constants.DATA_START + 2)]
    public class ServerInfo : ISerializable
    {
        /// <summary>服务器id</summary>
        public string id;
        /// <summary>服务器名称</summary>
        public string name;
        /// <summary>服务器ip</summary>
        public string address;
        /// <summary>区id</summary>
        public string realm;
        /// <summary>服务器状态</summary>
        public string state;
        /// <summary>服务器状态文字</summary>
        public string state_text;
        /// <summary>逻辑服务器id</summary>
        public string group;
        /// <summary>逻辑服可分配的节点</summary>
        public string[] nodes;

        /// <summary>服务器类型</summary>
        public string type;
        /// <summary>是否对玩家可见</summary>
        public bool is_open;
        /// <summary>区名称图片id</summary>
        public string icon;
        /// <summary>服务器排序</summary>
        public int view_index;
        /// <summary>服务器排序</summary>
        public int view_realm_index;
        /// <summary>服务器排序</summary>
        public string view_realm_name;
        /// <summary>颜色值</summary>
        public int view_rgba;

        /// <summary>
        /// 是否强制排队等待
        /// </summary>
        public bool isForceQueueUp;
        /// <summary>
        /// 开服时间.
        /// </summary>
        public DateTime open_at;


        public override string ToString()
        {
            return $"id={id} name={name} realm={realm}";
        }
        public static bool TryParse(string text, out string id, out string name, out string realm)
        {
            var ret = true;
            var prop = Properties.ParseArgs(Regex.Split(text, @"\s"));
            if (!prop.TryGetValue("id", out id)) { ret = false; }
            if (!prop.TryGetValue("name", out name)) { ret = false; }
            if (!prop.TryGetValue("realm", out realm)) { ret = false; }
            return ret;
        }


        public ServerInfo Clone()
        {
            var ret = (ServerInfo)MemberwiseClone();
            return ret;
        }
        public static string DEFAULT_SERVER_LIST_XML =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<doc>
    <serverList>
        <server realm=""1"" id=""1"" group=""1"" name=""Local""  address=""127.0.0.1:19300""   state=""1:正常"" is_open=""1"" view_index=""0"" nodes=""LogicNode1"" />
        <server realm=""1"" id=""2"" group=""1"" name=""Lan""    address=""10.26.18.10:19300"" state=""1:正常"" is_open=""1"" view_index=""0"" nodes=""LogicNode1"" />
        <server realm=""1"" id=""3"" group=""1"" name=""US""     address=""44.44.44.44:8000""  state=""1:正常"" is_open=""1"" view_index=""0"" nodes=""LogicNode1"" />
    </serverList>
    <recomList>
        <serverId>1</serverId>
    </recomList>  
    <mappingList>
        <addressMapping src=""127.0.0.1:19300"" dst=""127.0.0.1:19300"" />
    </mappingList>
</doc>
";
        //--------------------------------------------------------------------------------------------------------------------
        public static void LoadServerList(
            string path,
            out HashMap<string, ServerInfo> serverList,
            out HashMap<string, List<ServerInfo>> groupList,
            out List<ServerInfo> recommendList,
            out HashMap<string, string> addressMapping,
            string realmID = null)
        {
            serverList = new HashMap<string, ServerInfo>();
            groupList = new HashMap<string, List<ServerInfo>>();
            recommendList = new List<ServerInfo>();
            addressMapping = new HashMap<string, string>();
            LoadServerList(path, serverList, groupList, recommendList, addressMapping, realmID);
        }
        public static void LoadServerList(
            XmlDocument xml,
            out HashMap<string, ServerInfo> serverList,
            out HashMap<string, List<ServerInfo>> groupList,
            out List<ServerInfo> recommendList,
            out HashMap<string, string> addressMapping,
            string realmID = null)
        {
            serverList = new HashMap<string, ServerInfo>();
            groupList = new HashMap<string, List<ServerInfo>>();
            recommendList = new List<ServerInfo>();
            addressMapping = new HashMap<string, string>();
            LoadServerList(xml, serverList, groupList, recommendList, addressMapping, realmID);
        }

        public static void LoadServerList(
            string path,
            HashMap<string, ServerInfo> serverList,
            HashMap<string, List<ServerInfo>> groupList,
            List<ServerInfo> recommendList,
            HashMap<string, string> addressMapping,
            string realmID = null)
        {
            try
            {
                var xml = XmlUtil.LoadXML(path);
                LoadServerList(xml, serverList, groupList, recommendList, addressMapping, realmID);
            }
            catch (Exception err)
            {
                throw new Exception("Load Server List Error From : " + path, err);
            }
        }
        public static void LoadServerList(
            XmlDocument xml,
            HashMap<string, ServerInfo> serverList,
            HashMap<string, List<ServerInfo>> groupList,
            List<ServerInfo> recommendList,
            HashMap<string, string> addressMapping,
            string realmID = null)
        {
            //加载服务器列表
            var serverListXml = xml.DocumentElement.FindChild<XmlElement>("serverList");
            if (serverListXml != null)
            {
                serverListXml.ForEachChilds<XmlElement>("server", (e) =>
                {
                    var serverProp = Properties.LoadFromXML(e, true);
                    var serverInfo = serverProp.LoadInstance<ServerInfo>();
                    if (string.IsNullOrEmpty(realmID) || realmID == serverInfo.realm)
                    {
                        serverList.Add(serverInfo.id, serverInfo);

                        var group = groupList.GetOrNew(serverInfo.group);
                        group.Add(serverInfo);

                    }
                });
            }
            //加载推荐服
            var recomListXml = xml.DocumentElement.FindChild<XmlElement>("recomList");
            if (recomListXml != null)
            {
                recomListXml.ForEachChilds<XmlElement>("serverId", (e) =>
                {
                    var serverID = e.GetXmlNodeText();
                    if (serverList.TryGetValue(serverID, out var serverInfo))
                    {
                        if (realmID == null || realmID == serverInfo.realm)
                        {
                            recommendList.Add(serverInfo);
                        }
                    }
                });
            }
            //加载地址隐射
            var addressMappingXml = xml.DocumentElement.FindChild<XmlElement>("mappingList");
            if (addressMappingXml != null)
            {
                addressMappingXml.ForEachChilds<XmlElement>("addressMapping", (e) =>
                {
                    var src = e.GetAttribute("src");
                    var dst = e.GetAttribute("dst");
                    if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(dst))
                    {
                        addressMapping[src] = dst;
                    }
                });
            }
        }
        //--------------------------------------------------------------------------------------------------------------------
    }

    [MessageType(Constants.DATA_START + 3)]
    public class AccountData : ISerializable, IObjectMapping
    {
        [PersistField]
        public string uuid;

        [PersistField]
        public string token;

        [PersistField]
        public string lastLoginRemoteAddress;

        [PersistField]
        public DateTime lastLoginTime;

        [PersistField]
        public string lastLoginConnectAddress;

        [PersistField]
        public string lastLoginServerID;
        [PersistField]
        public string lastLoginServerGroupID;

        [PersistField]
        public string lastLoginRoleID;

        [PersistField(PersistStrategy.SaveLoadImmediately)]
        public string lastLoginToken;

        [PersistField]
        public ClientInfo lastClientInfo;

        /// <summary>
        /// 用户权限
        /// </summary>
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;

        [PersistField]
        public HashMap<string, RoleIDSnap> roleList = new HashMap<string, RoleIDSnap>();

    }

    [MessageType(Constants.DATA_START + 4)]
    public class RoleIDSnap : ISerializable, IStructMapping
    {
        // 此处只定义静态数据
        public string roleUUID;
        public string serverID;
        public int lv;
        public byte pro;
        public byte gender;
        public string name;
    }


    /// <summary>
    /// 返回哪些指令是啥GM等级可以使用的
    /// </summary>
    public enum RolePrivilege
    {
        //----- 玩家级别 -----
        ///<summary>普通玩家                 [公共聊天室说话]</summary>
        User_Player,
        User_WhiteListPlayer,
        ///<summary>超级玩家，比如家人，老玩家 [目前没用]</summary>
        User_PowerPlayer,
        ///<summary>比如 版署，合作方, 员工  [可随时登入游戏]</summary>
        User_VIP,

        //----- 客服组级别 -----
        ///<summary>客服                    [可使用简单的客服指令]</summary>
        Gm_Wizard,
        ///<summary>资深客服                [可使用对玩家部分处罚和影响的客服指令]</summary>
        Gm_PowerWizard,
        ///<summary>客服主管                [可使用对玩家所有处罚和影响的客服指令]</summary>
        Gm_SuperWizard,

        // ----- 开发组级别 -----
        ///<summary>项目组策划开发人员</summary>
        Dev_Disgner,
        ///<summary>项目组程序开发人员       [同上，但可使用一些策划指令，如改npc]</summary>
        Dev_Programer,

        //----- 公司领导层级别 -----
        ///<summary>项目小队长              [可使用有一点rmb价值的指令]</summary>
        Admin_Leader,
        ///<summary>项目负责人              [可以使用较有rmb价值的指令]</summary>
        Admin_Manager,
        ///<summary>项目总负责人            [可以使用所有rmb价值的指令]</summary>
        Admin_SuperManager,
        ///<summary>总管理员                [所有]</summary>
        Admin_BigBoss,
    }

    /*
    [MessageType(Constants.DATA_START + 5)]
    public class ClientRoleData : ISerializable
    {
        //------------------------------------------------
        public string uuid;
        public string digitID;
        public string name;
        public string account_uuid;
        public int role_template_id;
        //public int unit_template_id;
        //------------------------------------------------
        public string local_code = "zh_CN";
        //------------------------------------------------
        public int level;
        public DateTime create_time;
        public DateTime last_login_time;
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string server_name;

        /// <summary>
        /// 用户权限
        /// </summary>
        public RolePrivilege privilege = RolePrivilege.User_Player;


        //         //------------------------------------------------
        //         #region area
        // 
        //         /// <summary>
        //         /// 最后场景服务地址
        //         /// </summary>
        //         public string last_area_name;
        //         public string last_area_node;
        //         /// <summary>
        //         /// 最后存在场景UUID
        //         /// </summary>
        //         public string last_zone_uuid;
        //         /// <summary>
        //         /// 最后存在场景模板
        //         /// </summary>
        //         public int last_map_template_id;
        //         /// <summary>
        //         /// 最后存在场景坐标
        //         /// </summary>
        //         //public ZonePosition last_zone_pos;
        //         #endregion
        //         //------------------------------------------------
    }
    */
    /// <summary>
    /// 角色快照数据
    /// </summary>
    [PersistType]
    [MessageType(Constants.DATA_START + 6)]
    public class RoleSnap : ISerializable, IObjectMapping
    {
        //------------------------------------------------
        [PersistField]
        public string uuid;
        [PersistField]
        public string digitID;
        [PersistField]
        public string name;
        [PersistField]
        public string session_name;
        [PersistField]
        public string account_uuid;
        [PersistField]
        public int role_template_id;
        //         [PersistField]
        //         public int unit_template_id;
        //------------------------------------------------
        /// <summary>
        /// 服务器ID.
        /// </summary>
        [PersistField]
        public string server_id;
        //------------------------------------------------
        [PersistField]
        public int level;
        [PersistField]
        public int vip_level;
        [PersistField]
        public DateTime create_time;
        [PersistField]
        public DateTime last_login_time;
        [PersistField]
        public int onlineState;
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;
        [PersistField]
        public bool isRecharge;
        [PersistField]
        public ISerializable extra_data;
    }

    [MessageType(Constants.DATA_START + 7)]
    public class PropertyStruct : ISerializable
    {
        public const int TYPE_NUMBER = 1;
        public const int TYPE_STRING = 2;

        public string key;
        public string value;
        public int type;

        public PropertyStruct(string key, string value, bool isNum)
        {
            this.key = key;
            this.value = value;
            type = isNum ? TYPE_NUMBER : TYPE_STRING;
        }

        public PropertyStruct()
        {
        }
    }

    /// <summary>
    /// 角色在线类型
    /// </summary>
    [MessageType(Constants.DATA_START + 8)]
    public class RoleState : ISerializable
    {
        public const int STATE_ONLINE = 1;
        public const int STATE_OFFLINE = 2;
    }


    //角色数据状态快照
    [PersistType]
    [MessageType(Constants.DATA_START + 9)]
    public class RoleDataStatusSnap : ISerializable, IObjectMapping
    {
        [PersistField]
        public string PlayerUUID;
        [PersistField]
        public string PlayerName;
        [PersistField]
        public DateTime SuspendDate;
        [PersistField]
        public string SuspendReason;
        [PersistField]
        public string OperatorID;
    }
    //---------------------------------------------------------------------------------
    [MessageType(Constants.DATA_START + 10)]
    public class EventStoreData : IObjectMapping
    {
        [PersistField]
        public byte[] Bytes;
    }

    [MessageType(Constants.DATA_START + 11)]
    public class ServerPassportData
    {
        public bool Verified;
        public RolePrivilege Privilege;
        public ServerPassportData(bool verified, RolePrivilege privilege)
        {
            Verified = verified;
            Privilege = privilege;
        }
    }
    [MessageType(Constants.DATA_START + 12)]
    public class ServerPassportEnterGame
    {
        public bool Verified;
        public string Message;
    }

    /// <summary>
    /// 角色完整数据
    /// </summary>
    [PersistType]
    [MessageType(Constants.DATA_START + 0x2001)]
    public class ServerRoleData : ISerializable, IObjectMapping
    {
        //------------------------------------------------
        /// <summary>
        /// 服务器ID.
        /// </summary>
        [PersistField]
        public string server_id;
        //------------------------------------------------
        [PersistField]
        public string uuid;
        [PersistField]
        public string digitID;
        [PersistField]
        public string name;
        [PersistField]
        public string account_uuid;
        [PersistField]
        public int role_template_id;
        //         [PersistField]
        //         public int unit_template_id;
        //------------------------------------------------
        /// <summary> zh_CN, zh_TW, en_US </summary>
        [PersistField]
        public string local_code = "zh_CN";
        //------------------------------------------------
        [PersistField]
        public int Level;
        [PersistField]
        public DateTime create_time;
        [PersistField]
        public DateTime last_login_time;
        [PersistField]
        public DateTime last_logout_time;
        //------------------------------------------------
        /// <summary>
        /// 用户权限
        /// </summary>
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;
        //------------------------------------------------

        //------------------------------------------------
        [PersistField]
        public int onlineState;
        /// <summary>
        /// 是否充值
        /// </summary>
        [PersistField]
        public bool isRecharge;

        public virtual RoleSnap ToSnap()
        {
            return new RoleSnap()
            {
                uuid = uuid,
                digitID = digitID,
                name = name,
                account_uuid = account_uuid,
                role_template_id = role_template_id,
                //unit_template_id = unit_template_id,
                level = Level,
                create_time = create_time,
                last_login_time = last_login_time,
                isRecharge = isRecharge,
            };
        }
        /*
        public virtual ClientRoleData ToClientRoleData()
        {
            var ret = new ClientRoleData();
            ret.uuid = uuid;
            ret.digitID = digitID;
            ret.name = name;
            ret.account_uuid = account_uuid;
            ret.role_template_id = role_template_id;
            //ret.unit_template_id = this.unit_template_id;
            ret.level = Level;
            ret.create_time = create_time;
            ret.last_login_time = last_login_time;
            ret.server_name = server_id;
            ret.privilege = privilege;
            //             ret.last_area_name = this.last_area_name;
            //             ret.last_area_node = this.last_area_node;
            //             ret.last_zone_uuid = this.last_zone_uuid;
            //             ret.last_map_template_id = this.last_map_template_id;
            //             ret.last_zone_pos = this.last_zone_pos;
            return ret;
        }*/
    }

    /// <summary>
    /// 角色完整数据
    /// </summary>
    [PersistType]
    [MessageType(Constants.DATA_START + 0x2002)]
    public class ServerRoleZoneData : ISerializable, IObjectMapping
    {
        [PersistField(PersistStrategy.Primary)]
        public string uuid;
        [PersistField]
        public int unit_template_id;
        /// <summary>
        /// 最后场景服务地址
        /// </summary>
        [PersistField]
        public string last_area_name;
        [PersistField]
        public string last_area_node;
        /// <summary>
        /// 最后存在场景UUID
        /// </summary>
        [PersistField]
        public string last_zone_uuid;
        /// <summary>
        /// 最后存在地图模板
        /// </summary>
        [PersistField]
        public int last_zone_template_id;
        /// <summary>
        /// 最后存在场景坐标
        /// </summary>
        [PersistField]
        public ZonePosition last_zone_pos;
        /// <summary>
        /// 最后存在场景存储数据，用于跨场景存储一些状态，比如BUFF
        /// </summary>
        [PersistField]
        public ISerializable last_zone_saved;
        /// <summary>
        /// 最近一次公共场景实例ID.
        /// </summary>
        [PersistField]
        public string last_public_area_uuid;
        /// <summary>
        /// 上一次公共场景地图ID.
        /// </summary>
        [PersistField]
        public int last_public_zone_ID;
        /// <summary>
        /// 上一次公共地图所在坐标.
        /// </summary>
        [PersistField]
        public ZonePosition last_public_zone_pos;



    }

    [MessageType(Constants.DATA_START + 0x2003)]
    public class GateClientInfo : ISerializable
    {
        public string SessionNode;
        public string SessionName;
        public string AccountID;
        public string RoleID;
        public string ServerID;
        public string ServerGroupID;

        public string OS;
        public string OSVersion;
        public string Network;
        public string AppVersion;
        public string AppVersionCode;
        public string SDKVersion;
        public string DeviceBrand;
        public string DeviceModel;
        public string DeviceType;
        public string DeviceScreen;
        public string DeviceMacAddress;
        public string Region;
        public string IMEI;
        public string UUID;
        public string PackageName;
        public string BuildNumber;
        public string Carrier;
        public string ICCID;
        public string IMSI;
        public string IDFA;
        public string ClientEndpoint;
        public string ClientVersion;
        public string DeviceID;
        public string Channel;
        public string SDKName;
        public string PlatformAccount;
        public string SubChannel;
        public string UserAgent;
        public string UserSource1;
        public string UserSource2;
        public string WalletAddress;
        public string InvateWalletAddress;

        public static GateClientInfo LoadFrom(Properties cfg)
        {
            return cfg.LoadInstance<GateClientInfo>();
        }
        public Properties SaveTo()
        {
            var ret = new Properties();
            ret.SaveFields(this);
            return ret;
        }
    }

}
