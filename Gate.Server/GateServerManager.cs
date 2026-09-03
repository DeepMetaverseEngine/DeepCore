using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.ORM;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepCore.Xml;
using DeepCrystal.NetServer;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.ORM.Query;
using DeepCrystal.RPC;
using DeepCrystal.Server;
using DeepFrozen.MySQL;
using Gate.Data;
using Gate.Data.Protocol;
using Gate.Server.Mail;
using Gate.Server.Service.Session;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ZstdSharp.Unsafe;

namespace Gate.Server
{
    [Reflectible]
    public class GateServerManager : Disposable
    {
        //-----------------------------------------------------------------------------------------------
        public static Logger log { get => Instance._log; }
        public static GateServerConfig Config { get; private set; }
        public static GateServerManager Instance { get; private set; }
        public static MySQLConnectPool MySQL { get; protected set; }
        public static ServerFactory ClientHostFactory { get; protected set; }
        public static IExternalizableFactory ClientCodec { get; protected set; }
        public static Type ServerRPCCodecType { get; protected set; }
        public static bool EnableProxyProtocolV2 { get; set; } = true;
        //-----------------------------------------------------------------------------------------------
        protected readonly Logger _log = new LazyLogger(nameof(GateServerManager));
        public GateServerManager(GateServerConfig cfg)
        {
            Config = cfg;
            Instance = this;
        }
        protected override void Disposing()
        {
        }
        public void Init()
        {
            try
            {
                ClientHostFactory = ReflectionUtil.CreateInterface<ServerFactory>(Config.ClientHostFactoryClass);
                Debug.Assert(ClientHostFactory != null, $"{nameof(GateServerConfig.ClientHostFactoryClass)} is null");
                ClientCodec = ReflectionUtil.CreateInterface<IExternalizableFactory>(Config.ClientCodecClass);
                Debug.Assert(ClientCodec != null, $"{nameof(GateServerConfig.ClientCodecClass)} is null");
                ServerRPCCodecType = ReflectionUtil.GetType(Config.ServerCodecClass);
                Debug.Assert(ServerRPCCodecType != null, $"{nameof(GateServerConfig.ServerCodecClass)} is null");
                if (!string.IsNullOrEmpty(Config.MySQLConnectorString))
                {
                    MySQL = new MySQLConnectPool(Config.MySQLConnectorString);
                }
                OnInitBegin();
                {
                    CreateServerName();
                    CreateServerList();
                    //CreateServerLanguage();
                    CreateMailBox();
                    CreateMapping();
                    CreatePassport();
#if false
                    CreateGridWorldData();
#endif
                }
                OnInit();
                OnInitEnd();
            }
            catch (Exception err)
            {
                log.Error(err);
                throw err;
            }
        }
        protected virtual void OnInitBegin() { }
        protected virtual void OnInit() { }
        protected virtual void OnInitEnd() { }
        protected virtual ServerNameManager CreateServerName() => new ServerNameManager();
        protected virtual ServerListManager CreateServerList() => new ServerListManager();
        //protected virtual ServerLanguageManager CreateServerLanguage() => new ServerLanguageManager();
        protected virtual MailBoxManager CreateMailBox() => new MailBoxManager();
        protected virtual MappingManager CreateMapping() => new MappingManager();
        protected virtual PassportManager CreatePassport() => new PassportManager();
#if false
        protected virtual GridWorldDataManager CreateGridWorldData() => new GridWorldDataManager();
#endif
        //-----------------------------------------------------------------------------------------------
        public virtual IServer CreateServer(ServerConfig config)
        {
            var client_codec = GateServerManager.ClientCodec;
            var factory = GateServerManager.ClientHostFactory;
            var acceptor = factory.CreateServer(config, client_codec);
            acceptor.OnCreateSession += Acceptor_OnCreateSession;
            return acceptor;
        }
        protected virtual void Acceptor_OnCreateSession(ISession session)
        {
            if (EnableProxyProtocolV2)
            {
                session.AppendDataFilter(new ProxyProtocolV2Filter());
            }
        }
        //-----------------------------------------------------------------------------------------------
        #region ServerName
        public static ServerNameManager ServerName { get; private set; }
        public class ServerNameManager
        {
            public const string GateServerType /*    */ = "GateServer";
            public const string ConnectServerType /* */ = "ConnectServer";
            public const string SessionServiceType /**/ = "SessionService";
            public const string LogicServiceType /*  */ = "LogicService";
#if false
            public const string WorldManagerType /*   */ = "WorldManager";
            public const string WorldChannelType /*   */ = "WorldChannel";
#endif
            public ServerNameManager()
            {
                ServerName = this;
            }

            public virtual void GetServiceMapping(Properties mappings)
            {
                mappings.Put(ServerNameManager.GateServerType, typeof(Gate.Server.Service.Gate.GateServer).FullName);
                mappings.Put(ServerNameManager.ConnectServerType, typeof(Gate.Server.Service.Session.ConnectServer).FullName);
                mappings.Put(ServerNameManager.SessionServiceType, typeof(Gate.Server.Service.Session.SessionService).FullName);
                mappings.Put(ServerNameManager.LogicServiceType, typeof(Gate.Server.Service.Logic.LogicService).FullName);
#if false
                mappings.Put(ServerNameManager.WorldManagerType, typeof(Gate.Server.Service.World.ChannelManagerService).FullName);
                mappings.Put(ServerNameManager.WorldChannelType, typeof(Gate.Server.Service.World.ChannelService).FullName);
#endif
            }


            public virtual RemoteAddress GetGateService(string svcNode = null)
            {
                return new RemoteAddress("GateServer", svcNode, GateServerType);
            }
            public virtual RemoteAddress GetConnectService(string connectNumber, string svcNode = null)
            {
                return new RemoteAddress($"Connect:{connectNumber}", svcNode, ConnectServerType);
            }
            public virtual RemoteAddress GetSessionService(string accountID, string svcNode = null)
            {
                return new RemoteAddress($"Session:{accountID}", svcNode, SessionServiceType);
            }
            public virtual RemoteAddress GetLogicService(string roleID, string svcNode = null)
            {
                return new RemoteAddress($"Logic:{roleID}", svcNode, LogicServiceType);
            }

#if false
            public virtual RemoteAddress GetWorldManagerService(string svcNode = null)
            {
                return new RemoteAddress("WorldManager", svcNode, WorldManagerType);
            }
            public virtual RemoteAddress GetWorldChannelService(int chunkID, string svcNode = null)
            {
                return new RemoteAddress("WorldChannel:" + chunkID, svcNode, WorldChannelType);
            }
#endif
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        //         #region Language
        //         public static ServerLanguageManager Language { get; private set; }
        //         public class ServerLanguageManager
        //         {
        //             private HashMap<string, LanguageManager> _languages = new HashMap<string, LanguageManager>();
        // 
        //             public ServerLanguageManager()
        //             {
        //                 Language = this;
        //                 LoadLanguage(Config.LanguageRootDir);
        //             }
        //             public virtual void LoadLanguage(string path)
        //             {
        //                 _languages = LanguageManager.LoadLanguages(path);
        //             }
        //             /// <summary>
        //             /// 
        //             /// </summary>
        //             /// <param name="local_code">zh_CN, zh_TW, en_US</param>
        //             /// <returns></returns>
        //             public virtual bool TryGetLanguage(string local_code, out LanguageManager lang)
        //             {
        //                 if (_languages.TryGetValue(local_code.ToLower(), out lang))
        //                 {
        //                     return true;
        //                 }
        //                 return false;
        //             }
        // 
        //         }
        //         #endregion
        //-----------------------------------------------------------------------------------------------
        #region ServerList
        public static ServerListManager ServerList { get; private set; }
        public class ServerListManager
        {
            private ReaderWriterLockSlim serverListLock = new ReaderWriterLockSlim();
            private HashMap<string, ServerInfo> serverList = new HashMap<string, ServerInfo>();
            private HashMap<string, List<ServerInfo>> groupList = new HashMap<string, List<ServerInfo>>();
            private List<ServerInfo> recommendList = new List<ServerInfo>();
            /*
                <?xml version="1.0" encoding="utf-8"?>
                    <doc>
                      <serverList>
                        <server realm="1" id="1"     group="1"  name="外网测试服"        address="103.242.169.212:38000"   state ="1:正常"  is_open="1"  view_index="1"  />
                        <server realm="1" id="2"     group="1"  name="内网策划服"        address="192.168.1.231:19001"     state ="1:正常"  is_open="1"  view_index="2"  />
                        <server realm="1" id="3"     group="1"  name="内网测试服"        address="192.168.1.226:19001"     state ="1:正常"  is_open="1"  view_index="3"  />
                        <server realm="1" id="4"     group="1"  name="审核服"            address="192.168.1.226:29001"     state ="1:正常"  is_open="1"  view_index="4"  />
                        <server realm="1" id="5"     group="1"  name="六七"              address="192.168.1.19:19001"      state ="1:正常"  is_open="1"  view_index="5"  />
                        <server realm="1" id="6"     group="1"  name="从现在开始"        address="192.168.1.102:19001"     state ="1:正常"  is_open="1"  view_index="6"  />
                        <server realm="1" id="7"     group="1"  name="从现在开始-外网"   address="103.242.169.212:48001"   state ="1:正常"  is_open="1"  view_index="7"  />
                        <server realm="1" id="2000"  group="1"  name="(VS)尼霸霸"        address="192.168.1.20:19001"      state ="1:正常"  is_open="1"  view_index="8"  />
                        <server realm="1" id="3000"  group="1"  name="(VS)luo"           address="192.168.1.21:19001"      state ="1:正常"  is_open="1"  view_index="9"  />
                        <server realm="1" id="4000"  group="1"  name="(VS)吴"            address="192.168.1.11:19001"      state ="1:正常"  is_open="1"  view_index="10" />
                        <server realm="1" id="5000"  group="1"  name="(VS)老蔡"          address="192.168.1.12:19001"      state ="1:正常"  is_open="1"  view_index="11" />
                        <server realm="1" id="6000"  group="1"  name="(VS)老Q"           address="192.168.1.17:19001"      state ="1:正常"  is_open="1"  view_index="12" />
                        <server realm="1" id="7000"  group="1"  name="(VS)飞哥"          address="192.168.1.22:19001"      state ="1:正常"  is_open="1"  view_index="13" />
                        <server realm="1" id="9001"  group="1"  name="(VS)本地开发服G1"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode1"/>
                        <server realm="1" id="9002"  group="2"  name="(VS)本地开发服G2"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2"/>
                        <server realm="1" id="9003"  group="3"  name="(VS)本地开发服G3"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2,LogicNode1"/>
                        <server realm="1" id="9003"  group="4"  name="(VS)本地开发服G4"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2,LogicNode1"/>
                      </serverList>
                      <recomList>
                        <serverId>3</serverId>
                        <serverId>2</serverId>
                      </recomList>
                    </doc> 
             */
            public ServerListManager()
            {
                ServerList = this;
                ReloadServerList();
            }

            public void ReloadServerList()
            {
                using (serverListLock.EnterWrite())
                {
                    this.serverList.Clear();
                    this.groupList.Clear();
                    this.recommendList.Clear();
                    LoadServerList(serverList, groupList, recommendList, new HashMap<string, string>());
                    foreach (var e in serverList)
                    {
                        log.Warn($"{e.Value} reload");
                    }
                }
            }

            protected virtual void LoadServerList(
                HashMap<string, ServerInfo> serverList,
                HashMap<string, List<ServerInfo>> groupList,
                List<ServerInfo> recommendList,
                HashMap<string, string> addressMapping)
            {
                var serverListPath = Config.ServerListUrl;
                if (!string.IsNullOrEmpty(serverListPath))
                {
                    try
                    {
                        log.Log($"LoadServerList : {serverListPath}");
                        //log.Warn("Load Server From : " + serverListPath + "\n" + Resource.LoadAllText(GlobalConfig.ServerListUrl));
                        if (Resource.ExistData(serverListPath))
                        {
                            var xml = XmlUtil.LoadXML(serverListPath);
                            if (xml != null)
                            {
                                log.Warn("LoadServerList From : " + serverListPath);
                                ServerInfo.LoadServerList(xml, serverList, groupList, recommendList, addressMapping, Config.RealmID);
                                return;
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error("LoadServerList Error From : " + serverListPath, err);
                    }
                }
                {
                    log.Warn("LoadServerList From DEFAULT_SERVER_LIST_XML");
                    var xml = XmlUtil.FromString(ServerInfo.DEFAULT_SERVER_LIST_XML);
                    if (xml != null)
                    {
                        ServerInfo.LoadServerList(xml, serverList, groupList, recommendList, addressMapping, Config.RealmID);
                    }
                }
            }

            public virtual string GetServerGroupID(string serverID)
            {
                using (serverListLock.EnterRead())
                {
                    if (serverList.TryGetValue(serverID, out var info))
                    {
                        return info.group;
                    }
                }

                return null;
            }

            public virtual List<string> GetAllServerGroupIdList()
            {
                var groupIds = groupList.Keys.ToGenericList<string>();
                return groupIds;
            }
            public virtual ServerInfo[] GetRecommendList()
            {
                return recommendList.ToArray();
            }

            /// <summary>
            ///组对应的服务器ID.
            /// </summary>
            /// <param name="serverGroupID"></param>
            /// <returns></returns>
            public virtual List<string> GetServersID(string serverGroupID)
            {
                using (serverListLock.EnterRead())
                {
                    if (groupList.TryGetValue(serverGroupID, out var list))
                    {
                        return list.ConvertAll(e => e.id);
                    }
                }
                return null;
            }
            public virtual List<ServerInfo> GetServersInfo(string serverGroupID)
            {
                using (serverListLock.EnterRead())
                {
                    if (groupList.TryGetValue(serverGroupID, out var list))
                    {
                        return list;
                    }
                }
                return null;
            }

            public virtual List<string> GetAllServerGroupID()
            {
                using (serverListLock.EnterRead())
                {
                    return new List<string>(groupList.Keys);
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="serverID"></param>
            /// <returns></returns>
            public virtual bool ServerIsOpen(string serverID)
            {
                using (serverListLock.EnterRead())
                {
                    if (serverList.TryGetValue(serverID, out var info))
                    {
                        return info.is_open;
                    }
                }
                return false;
            }

            /// <summary>
            /// 获取所有服务器配置
            /// </summary>
            /// <returns></returns>
            public virtual List<ServerInfo> GetAllServers()
            {
                using (serverListLock.EnterRead())
                {
                    return new List<ServerInfo>(serverList.Values);
                }
            }

            /// <summary>
            /// 开服时间,非UTC时间.
            /// </summary>
            /// <param name="serverID"></param>
            /// <returns></returns>
            public virtual DateTime GetServerOpenTime(string serverID)
            {
                using (serverListLock.EnterRead())
                {
                    if (serverList.TryGetValue(serverID, out var info))
                    {
                        return info.open_at;
                    }
                }
                return new DateTime();
            }

            /// <summary>
            /// 根据group获得开服时间,合服的话以最早开的为主
            /// </summary>
            /// <param name="groupid"></param>
            /// <returns></returns>
            public DateTime GetOpenTimeByGroupID(string groupid)
            {
                DateTime openTime = DateTime.MaxValue;
                using (serverListLock.EnterRead())
                {
                    if (groupList.TryGetValue(groupid, out var list))
                    {
                        foreach (var server in list)
                        {
                            if (DateTime.Compare(openTime, server.open_at) > 0)
                            {
                                openTime = server.open_at;
                            }
                        }
                    }
                }
                return openTime;
            }

            public virtual HashMap<string, DateTime> GetAllServerOpenTime()
            {
                HashMap<string, DateTime> dateTimes = new HashMap<string, DateTime>();
                using (serverListLock.EnterRead())
                {
                    foreach (var data in serverList)
                    {
                        dateTimes.Add(data.Key, data.Value.open_at);
                    }
                }
                return dateTimes;
            }

            // H.Q.Cai 添加代码开始
            public virtual void PostGroupServerNumber(string serverGroupID, int number)
            {
            }
            // H.Q.Cai 添加代码结束
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Passport
        public static PassportManager Passport { get; protected set; }
        public class PassportManager
        {
            public PassportManager()
            {
                GateServerManager.Passport = this;
            }
            public virtual Task<ServerPassportData> VerifyPassportAsync(ClientEnterGateRequest req)
            {
                return Task.FromResult(new ServerPassportData(true, 0));
            }
            public virtual Task<ServerPassportEnterGame> VerifyPassportEnterGameAsync(ClientEnterServerRequest server, ClientEnterGameRequest req)
            {
                return Task.FromResult(new ServerPassportEnterGame() { Verified = true });
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Mapping
        public static MappingManager Mapping { get; private set; }
        public class MappingManager : Disposable
        {
            public string TYPE_ACCOUNT_DATA = "Account:";
            public string TYPE_ROLE_DATA = "Role:";
            public string TYPE_ROLE_ZONE_DATA = "RoleZone:";
            public string TYPE_ROLE_SNAP_DATA = "RoleSnap:";


            private DeepCrystal.ORM.IMappingHash mappingWalletToAccount;
            private DeepCrystal.ORM.IMappingHash mappingAccountToWallet;
            private DeepCrystal.ORM.IMappingHash mappingNameToUUID;
            private DeepCrystal.ORM.IMappingHash mappingUUIDToName;
            private DeepCrystal.ORM.IMappingHash mappingDigitToUUID;
            private DeepCrystal.ORM.IMappingHash mappingUUIDToDigit;
            public DateTime ServerInitTimeUTC { get; private set; }

            public MappingManager()
            {
                Mapping = this;
                using (var start = ORMFactory.Instance.DefaultAdapter.GetHash("SERVER_INIT", null))
                {
                    var now = DateTime.UtcNow;
                    if (start.SetAsync(nameof(ServerInitTimeUTC), now, When.NotExists).WaitForResult())
                    {
                        ServerInitTimeUTC = now;
                    }
                    else
                    {
                        ServerInitTimeUTC = start.GetAsync<DateTime>(nameof(ServerInitTimeUTC)).WaitForResult();
                    }
                }
                this.mappingWalletToAccount = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:WalletToAccount", null);
                this.mappingAccountToWallet = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:AccountToWallet", null);
                this.mappingNameToUUID = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:NameToUUID", null);
                this.mappingUUIDToName = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:UUIDToName", null);
                this.mappingDigitToUUID = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:DigitToUUID", null);
                this.mappingUUIDToDigit = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:UUIDToDigit", null);
            }
            protected override void Disposing()
            {
                mappingWalletToAccount?.Dispose();
                mappingAccountToWallet?.Dispose();
                mappingNameToUUID?.Dispose();
                mappingUUIDToName?.Dispose();
                mappingDigitToUUID?.Dispose();
                mappingUUIDToDigit?.Dispose();
                this.mappingWalletToAccount = null;
                this.mappingAccountToWallet = null;
                this.mappingNameToUUID = null;
                this.mappingUUIDToName = null;
                this.mappingDigitToUUID = null;
                this.mappingUUIDToDigit = null;
            }

            /// <summary>
            /// 是否在屏蔽字库内.
            /// </summary>
            /// <param name="word"></param>
            /// <returns></returns>
            public virtual bool IsRoleNameBlackWord(string word)
            {
                return false;
            }

            public virtual int GetRoleMaxCount()
            {
                //TODO.
                return 5;
            }

            public virtual ServerRoleData CreateRoleData(ClientCreateRoleRequest req, string accountID, string serverid)
            {
                var roleID = (Guid.NewGuid().ToString());
                //玩家角色信息.
                ServerRoleData srd = new ServerRoleData();
                //创建UUID.
                srd.uuid = roleID;
                srd.account_uuid = accountID;
                srd.name = req.c2s_name;
                srd.server_id = serverid;
                srd.role_template_id = req.c2s_template_id;
                //srd.unit_template_id = req.c2s_template_id;
                srd.create_time = DateTime.UtcNow;
                return srd;
            }
            public virtual RoleSnap CreateRoleSnap(ServerRoleData data)
            {
                return new RoleSnap()
                {
                    uuid = data.uuid,
                    digitID = data.digitID,
                    name = data.name,
                    account_uuid = data.account_uuid,
                    role_template_id = data.role_template_id,
                    //unit_template_id = data.unit_template_id,
                    level = data.Level,
                    create_time = data.create_time,
                    last_login_time = data.last_login_time,
                    server_id = data.server_id,
                    privilege = data.privilege,
                };
            }
            public virtual RoleIDSnap CreateRoleIDSnapData(ServerRoleData roleData)
            {
                var ret = new RoleIDSnap()
                {
                    roleUUID = roleData.uuid,
                    serverID = roleData.server_id,
                    name = roleData.name,
                    lv = roleData.Level,
                };
                return ret;
            }

            public virtual MappingReference<ServerRoleData> CreateRoleDataMapping(string roleID, ITaskExecutor svc)
            {
                var role = (new MappingReference<ServerRoleData>(GateServerManager.Mapping.TYPE_ROLE_DATA, roleID, svc));
                //var snap = AutoDispose(new MappingReference<RoleSnap>(GateServerManager.Mapping.TYPE_ROLE_SNAP_DATA, roleID, this));
                return role;
            }
            public virtual MappingReference<RoleSnap> CreateRoleSnapMapping(string roleID, ITaskExecutor svc)
            {
                var role = (new MappingReference<RoleSnap>(GateServerManager.Mapping.TYPE_ROLE_SNAP_DATA, roleID, svc));
                //var snap = AutoDispose(new MappingReference<RoleSnap>(GateServerManager.Mapping.TYPE_ROLE_SNAP_DATA, roleID, this));
                return role;
            }

            /// <summary>
            /// 创建角色，由Session.Roled调用ORM.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="data"></param>
            /// <returns></returns>
            public virtual async Task<RoleSnap> CreateRoleAndSnapAsync(ServerRoleData data, ITaskExecutor svc)
            {
                var roleMapping = CreateRoleDataMapping(data.uuid, svc);
                await roleMapping.SaveDataAsync(data);
                var snapData = CreateRoleSnap(data);
                // Snap数据映射
                var snapMapping = CreateRoleSnapMapping(data.uuid, svc);
                await snapMapping.SaveDataAsync(snapData);
                return snapData;
            }

            public virtual async Task<ServerRoleZoneData> CreateRoleZoneDataAsync(MappingReference<ServerRoleZoneData> mapping, ServerRoleData role)
            {
                var ret = await mapping.LoadOrCreateDataAsync(() =>
                {
                    return new ServerRoleZoneData()
                    {
                        uuid = role.uuid,
                        unit_template_id = role.role_template_id,
                    };
                });
                var trans = mapping.Adapter.CreateExecutableObjectTransaction(mapping.Executor);
                try
                {

                    mapping.BatchFlush(trans);
                }
                finally
                {
                    await trans.ExecuteAsync();
                }
                return ret;
            }

            public virtual async Task DeleteRoleDataAsync(string c2s_role_uuid, ITaskExecutor svc)
            {
                var snapMapping = new MappingReference<RoleSnap>(TYPE_ROLE_SNAP_DATA, c2s_role_uuid, svc);
                var roleSnap = await snapMapping.LoadDataAsync();
                // TODO 
            }
            public class AccountDataInfo
            {
                public AccountData Account;
                public bool IsNewAccount;
            }
            public virtual async Task<AccountDataInfo> GetOrCreateAccountDataAsync(MappingReference<AccountData> saveAcc, string accountName, string accountToken)
            {
                if (await saveAcc.EnterLockAsync(out var token))
                {
                    try
                    {
                        var isNewAccount = false;
                        var accountData = await saveAcc.LoadOrCreateDataAsync(() =>
                        {
                            var ret = new AccountData();
                            ret.uuid = accountName;
                            ret.token = accountToken;
                            isNewAccount = true;
                            return ret;
                        });
                        return new AccountDataInfo()
                        {
                            Account = accountData,
                            IsNewAccount = isNewAccount
                        };
                    }
                    finally
                    {
                        await saveAcc.ExitLockAsync(token);
                    }
                }

                return null;
            }

            public virtual QueryMappingReference<T> GetQueryReference<T>(string typeName, ITaskExecutor svc, IMappingAdapter db = null) where T : IObjectMapping, new()
            {
                /* TEST
                Task.Run(async () => 
                {
                    var trans = ORMFactory.Instance.CreateTransaction(ORMFactory.Instance.DefaultAdapter);
                    trans.AddCondition(ORMFactory.Instance.Conditions.HashEqual("key", "fieldA", 12345));
                    trans.AddCondition(ORMFactory.Instance.Conditions.HashNotEqual("key", "fieldB", 12345));
                    using (var save = trans.GetHash("key", svc))
                    {
                        await save.SetAsync("fieldA", 1);
                        await save.SetAsync("fieldB", 2);
                        await save.SetAsync("fieldC", "ccc");
                        await trans.ExecuteAsync(svc);
                    }
                });
                */
                return new QueryMappingReference<T>(typeName, svc, db);
            }



            #region RoleNameMapping
            //-------------------------------------------------------------------------------------------------------------------------- 
            public virtual Task<bool> TryRegistAccountWalletAsync(string account, string walletAddress, ITaskExecutor svc)
            {
                return svc.Execute(async () =>
                {
                    if (await mappingAccountToWallet.SetAsync(account, walletAddress, When.NotExists))
                    {
                        if (await mappingWalletToAccount.SetAsync(walletAddress, account, When.NotExists))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }
            public virtual Task<string> GetAccountByWalletAsync(string walletAddress, ITaskExecutor svc)
            {
                return svc.Execute(mappingWalletToAccount.GetAsync(walletAddress).ContinueWith<string>(t => t.GetResultToString()));
            }
            public virtual Task<string> GetWalletByAccountAsync(string account, ITaskExecutor svc)
            {
                return svc.Execute(mappingAccountToWallet.GetAsync(account).ContinueWith<string>(t => t.GetResultToString()));
            }



            protected virtual async Task<string> GenDigitIDAsync(string roleUUID, ITaskExecutor svc)
            {
                var duration = DateTime.UtcNow - ServerInitTimeUTC;
                var prifix = ((long)duration.TotalMilliseconds);
                var suffix = ((int)roleUUID[0]) % 10;
                return $"{prifix}{suffix}";
            }
            public virtual Task<string> TryRegistRoleNameMappingAsync(string serverID, string roleUUID, string roleName, ITaskExecutor svc)
            {
                return svc.Execute(async () =>
                {
                    if (await mappingNameToUUID.SetAsync(roleName, roleUUID, When.NotExists))
                    {
                        await mappingUUIDToName.SetAsync(roleUUID, roleName);
                        var digitID = await GenDigitIDAsync(roleUUID, svc);
                        if (await mappingDigitToUUID.SetAsync(digitID, roleUUID, When.NotExists) == false)
                        {
                            var exist = await mappingDigitToUUID.GetAsync(digitID);
                            await mappingDigitToUUID.SetAsync(digitID, $"{exist},{roleUUID}");
                        }
                        await mappingUUIDToDigit.SetAsync(roleUUID, digitID);
                        return digitID;
                    }
                    return null;
                });
            }
            public virtual Task<bool> RoleChangeNameMappingAsync(string serverID, string roleUUID, string newName, string curName, ITaskExecutor svc)
            {
                return svc.Execute(async () =>
                {

                    if (await mappingNameToUUID.SetAsync(newName, roleUUID, When.NotExists))
                    {
                        //删除旧的名字.
                        await mappingNameToUUID.DeleteAsync(curName);
                        //删除旧的UUID关联.
                        await mappingUUIDToName.DeleteAsync(roleUUID);
                        //设置新的UUID关联.
                        await mappingUUIDToName.SetAsync(roleUUID, newName);
                        return true;
                    }
                    return false;
                });
            }
            public virtual Task<string> GetRoleNameByUUIDAsync(string roleUUID, ITaskExecutor svc)
            {
                return svc.Execute(mappingUUIDToName.GetAsync(roleUUID).ContinueWith<string>(t => t.GetResultToString()));
            }
            public virtual Task<IConvertible[]> GetRoleNameByUUIDAsync(string[] roleUUID, ITaskExecutor svc)
            {
                return svc.Execute(mappingUUIDToName.GetAsync(roleUUID));
            }
            public virtual Task<string> GetRoleUUIDByNameAsync(string serverID, string roleName, ITaskExecutor svc)
            {
                return svc.Execute(mappingNameToUUID.GetAsync(roleName).ContinueWith<string>(t => t.GetResultToString()));
            }
            public virtual Task<string> GetRoleDigitByUUIDAsync(string roleUUID, ITaskExecutor svc)
            {
                return svc.Execute(mappingUUIDToDigit.GetAsync(roleUUID).ContinueWith<string>(t => t.GetResultToString()));
            }
            public virtual Task<string[]> GetRoleUUIDByDigitAsync(string digit, ITaskExecutor svc)
            {
                return svc.Execute(mappingDigitToUUID.GetAsync(digit).ContinueWith<string[]>(t =>
                {
                    if (t.IsCompleted)
                    {
                        var exist = t.GetResultToString();
                        if (exist != null)
                        {
                            return exist.Split(',');
                        }
                    }
                    return null;
                }));
            }
            public virtual Task<bool> RoleNameExistAsync(string serverID, string roleName, ITaskExecutor svc)
            {
                return svc.Execute(mappingNameToUUID.ExistsAsync(roleName));
            }

            #endregion
            //--------------------------------------------------------------------------------------------------------------------------
            #region ServerRoleIDMapping.

            public class ServerRoleIDMappingSet
            {
                private const string TYPE_SERVER_ROLEID_DATA = "ServerID:{0}:RoleID:";
                private readonly DeepCrystal.ORM.IMappingSet mappingSet;

                public ServerRoleIDMappingSet(ITaskExecutor svc, string serverID)
                {
                    string key = string.Format(TYPE_SERVER_ROLEID_DATA, serverID);
                    this.mappingSet = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetSet(key, svc);
                }

                public Task AddRoleIDAsync(string playerUUID)
                {
                    return mappingSet.AddAsync(playerUUID);
                }

                public Task<string[]> GetRoleIDsAsync()
                {
                    return mappingSet.MembersAsync().ContinueWith(t =>
                    {
                        var rst = t.GetResultAs();
                        if (rst != null) return Array.ConvertAll(rst, (s) => s.ToString());
                        return null;
                    });
                }
            }

            public virtual ServerRoleIDMappingSet GetServerRoleIDMappingSet(ITaskExecutor svc, string serverid)
            {
                return new ServerRoleIDMappingSet(svc, serverid);
            }
            #endregion
            //--------------------------------------------------------------------------------------------------------------------------
            #region NameChecking
            //匹配中文，英文字母和数字及_: 
            private Regex roleNamePattern = new Regex(@"^[\u4e00-\u9fa5_a-zA-Z0-9]+$");
            /// <summary>
            /// 检查角色名是否合法
            /// </summary>
            /// <param name="roleName"></param>
            /// <returns></returns>
            public virtual bool CheckRoleName(string roleName)
            {
                //                 if (roleNamePattern.IsMatch(roleName))
                //                 {
                //                     return true;
                //                 }
                return true;
            }

            #endregion
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region MailBox
        public static MailBoxManager MailBox { get; private set; }
        public class MailBoxManager
        {
            public IMappingAdapter MappingAdapter { get; protected set; }

            protected HashMap<string, MailDomain> mailDomains = new HashMap<string, MailDomain>();

            public MailBoxManager()
            {
                MailBox = this;
                Init();
            }
            protected virtual void Init()
            {
                this.MappingAdapter = ORMFactory.Instance.DefaultAdapter;
            }

            public virtual async Task<MailDomain> GetDomainAsync(string mailDomain, string mailAccount)
            {
                MailDomain domain;
                lock (mailDomains)
                {
                    domain = mailDomains.GetOrAdd(mailDomain, d => new MailDomain(d));
                }
                await domain.RegistAsync(mailAccount);
                return domain;
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Channel

        #endregion
        //-----------------------------------------------------------------------------------------------------------
        #region World
#if false
        public static WorldDataManager World { get; private set; }
        public abstract class WorldDataManager
        {
            public WorldDataManager()
            {
                World = this;
            }
            abstract public int[] ListMapChunks();
            abstract public int[] ListNextChunks(int chunkID);
            public abstract MapChunk GetChunkByID(int chunkID);
            public abstract MapChunk GetChunkByPos(Vector3 pos);
            public abstract ChannelInfo GetChannelInfo(int chunkID);
            public virtual bool TrySwapChunk(MapChunk chunk, Vector3 pos, out MapChunk nextChunk)
            {
                if (chunk.AABB.Contains(pos) == ContainmentType.Disjoint)
                {
                    var nextNode = GetChunkByPos(pos);
                    if (nextNode != null)
                    {
                        nextChunk = nextNode;
                        return true;
                    }
                }
                nextChunk = null;
                return false;
            }
            public virtual bool TestInclude(MapChunk chunk, Vector3 pos)
            {
                if (chunk.AABB.Contains(pos) == ContainmentType.Disjoint)
                {
                    return false;
                }
                return true;
            }

        }
        public class GridWorldDataManager : WorldDataManager
        {
            protected readonly int totalW, totalH, split, xcount, ycount;
            protected readonly int[] chunksID;
            protected readonly MapChunk[,] mapMatrix;
            protected readonly HashMap<int, MapChunk> mapChunks;
            private static readonly int[][] NEXT_INDEX_TABLE = new int[][] {
            new int[]{ -1,-1 }, new int[]{ 0,-1 }, new int[]{ 1,-1 },
            new int[]{ -1, 0 },/*new int[]{0,0},*/ new int[]{ 1, 0 },
            new int[]{ -1, 1 }, new int[]{ 0, 1 }, new int[]{ 1, 1 },};

            public GridWorldDataManager(int totalW = 10000, int totalH = 10000, int split = 1000)
            {
                this.totalW = totalW;
                this.totalH = totalH;
                this.split = split;
                this.xcount = CMath.RoundMod(totalW, split);
                this.ycount = CMath.RoundMod(totalH, split);
                this.mapMatrix = new MapChunk[xcount, ycount];
                this.mapChunks = new HashMap<int, MapChunk>(xcount * ycount);
                this.chunksID = new int[xcount * ycount];
                for (int cx = 0; cx < xcount; cx++)
                {
                    for (int cy = 0; cy < ycount; cy++)
                    {
                        var index = cx * xcount + cy;
                        var chunk = this.mapMatrix[cx, cy] = CreateChunk(
                            index + 1,
                            new Location3D(cx * split, cy * split, 0),
                            new Size3D(split, split, split));
                        mapMatrix[cx, cy] = chunk;
                        chunksID[index] = chunk.ChunkID;
                        mapChunks.Add(chunk.ChunkID, chunk);
                    }
                }
                for (int cx = 0; cx < xcount; cx++)
                {
                    for (int cy = 0; cy < ycount; cy++)
                    {
                        var chunk = this.mapMatrix[cx, cy];
                        foreach (var next in NEXT_INDEX_TABLE)
                        {
                            if (TryGetChunkByGrid(cx + next[0], cy + next[1], out var nextChunk))
                            {
                                chunk.Nexts.Add(nextChunk);
                            }
                        }
                    }
                }
            }
            public virtual MapChunk CreateChunk(int chunkID, Location3D chunkLocation, Size3D chunkSize)
            {
                return new MapChunk(chunkID, chunkLocation, chunkSize);
            }
            public bool TryGetChunkByGrid(int cx, int cy, out MapChunk next)
            {
                if (cx >= 0 && cx < xcount && cy >= 0 && cy < ycount)
                {
                    next = mapMatrix[cx, cy];
                    return true;
                }
                next = null;
                return false;
            }
            public override MapChunk GetChunkByPos(Vector3 pos)
            {
                var cx = CMath.RoundMod(pos.X, split);
                var cy = CMath.RoundMod(pos.Y, split);
                if (cx >= 0 && cx < xcount)
                {
                    if (cy >= 0 && cy < ycount)
                    {
                        return this.mapMatrix[cx, cy];
                    }
                }
                return null;
            }
            public override int[] ListMapChunks()
            {
                return chunksID;
            }
            public override int[] ListNextChunks(int chunkID)
            {
                if (mapChunks.TryGetValue(chunkID, out var node))
                {
                    return node.Nexts.ToArray().Convert1D((i, v) => v.ChunkID);
                }
                return null;
            }
            public override MapChunk GetChunkByID(int chunkID)
            {
                if (mapChunks.TryGetValue(chunkID, out var node))
                {
                    return node;
                }
                return null;
            }
            public override ChannelInfo GetChannelInfo(int chunkID)
            {
                return new ChannelInfo()
                {
                    Name = $"Chunk:{chunkID}",
                    UUID = $"ChannelChunk:{chunkID}",
                    Data = null,
                };
            }
        }
#endif
        #endregion
        //-----------------------------------------------------------------------------------------------------------
    }

}
