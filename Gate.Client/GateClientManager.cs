using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.NetClient;
using DeepCore.Protocol;
using DeepCore.Reflection;
using DeepCore.Xml;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace Gate.Client
{
    [Reflectible]
    public class GateClientManager : Disposable
    {
        public static GateClientManager Instance { get; private set; }
        public static Logger Log => Instance?.log;
        public GateClientConfig Config { get; private set; }
        protected readonly Logger log = new LazyLogger(nameof(GateClientManager));
        public GateClientManager()
        {
            Instance = this;
        }
        public void Init(GateClientConfig cfg, IRangeValue range = null)
        {
            InitAsync(cfg, range).Wait();
        }
        public async Task InitAsync(GateClientConfig cfg, IRangeValue range = null)
        {
            Config = cfg;
            try
            {
                log.Info($"Get Client Code : {Config.ClientCodecClass}");
                this.ClientCodec = ReflectionUtil.CreateInterface<IExternalizableFactory>(Config.ClientCodecClass);
                Debug.Assert(ClientCodec != null, $"{nameof(GateClientConfig.ClientCodecClass)} is null");
                log.Info("OnInitBeginAsync");
                await OnInitBeginAsync(range);
                {
                    log.Info("LoadLangPropertiesAsync");
                    await LoadLangPropertiesAsync();
                    log.Info("CreateServerListManager");
                    this.ServerList = await CreateServerListManager(range);
                    log.Info("LoadServerListAsync");
                    await this.ServerList.LoadServerListAsync();
                    log.Info("CreateMessageCodeManager");
                    this.ErrorCodeManager = await CreateMessageCodeManager(range);
                    if (this.ErrorCodeManager != null && LangProperties != null)
                    {
                        this.ErrorCodeManager.Load(LangProperties.SubProperties($"ErrorCode."));
                    }
                    log.Info("CreateClientPassportManager");
                    this.Passport = await CreateClientPassportManager(range);
#if false
                    log.Info("CreateChannelManager");
                    this.Channel = await CreateChannelManager(range);
#endif
                }
                log.Info("OnInitAsync");
                await OnInitAsync(range);
                log.Info("OnInitEndAsync");
                await OnInitEndAsync(range);
            }
            catch (Exception err)
            {
                log.Error(err);
                throw err;
            }
        }
        protected override void Disposing()
        {
            OnLoadServerListAsync = null;

        }

        protected virtual Task OnInitBeginAsync(IRangeValue p) { return Task.CompletedTask; }
        protected virtual Task OnInitAsync(IRangeValue p) { return Task.CompletedTask; }
        protected virtual Task OnInitEndAsync(IRangeValue p) { return Task.CompletedTask; }

        protected virtual Task<ServerListManager> CreateServerListManager(IRangeValue range)
        {
            return Task.FromResult(new ServerListManager());
        }
        protected virtual Task<MessageCodeManager> CreateMessageCodeManager(IRangeValue range)
        {
            var codeManager = new MessageCodeManager(ClientCodec);
            return Task.FromResult(codeManager);
        }
        protected virtual Task<ClientPassportManager> CreateClientPassportManager(IRangeValue range)
        {
            return Task.FromResult(new ClientPassportManager());
        }
#if false
        protected virtual Task<ChannelManager> CreateChannelManager(IRangeValue range)
        {
            return Task.FromResult(new ChannelManager());
        }
#endif
        //-----------------------------------------------------------------------------------------------
        public IExternalizableFactory ClientCodec { get; protected set; }
        public ClientInfo ClientInfo { get; set; } = new ClientInfo() { };
        public MessageCodeManager ErrorCodeManager { get; protected set; }
        //-----------------------------------------------------------------------------------------------
        public virtual GateClient CreateGateClient() { return new GateClient(); }
        public virtual GateNetClient CreateNetClient(string name) { return new GateNetClient(ClientCodec, name); }
        public virtual IClientAdapter CreateNetClientAdapter(string address, GateNetClient client) => NetClientFactory.Instance.CreateAdapter(client);

        //-----------------------------------------------------------------------------------------------
        #region ServerList

        public delegate Task<XmlDocument> LoadServerListHandlerAsync(string path);
        public event LoadServerListHandlerAsync OnLoadServerListAsync;

        public ServerListManager ServerList { get; protected set; }
        public class ServerListManager
        {
            private List<ServerInfo> recommendList = new List<ServerInfo>();
            private HashMap<string, List<ServerInfo>> groupList = new HashMap<string, List<ServerInfo>>();
            private HashMap<string, ServerInfo> serverList = new HashMap<string, ServerInfo>();
            private HashMap<string, string> addressMapping = new HashMap<string, string>();
            public virtual List<ServerInfo> RecommendServers => new List<ServerInfo>(recommendList);
            public virtual List<string> GroupList => new List<string>(groupList.Keys);
            public virtual ServerInfo RecommendServer => recommendList.Count > 0 ? recommendList[0] : null;
            public ServerListManager()
            {
            }
            public async Task LoadServerListAsync()
            {
                try
                {
                    string serverListPath = Instance.Config.ServerListUrl;
                    if (!string.IsNullOrEmpty(serverListPath))
                    {
                        Instance.log.Log($"LoadServerList : {serverListPath}");
                        try
                        {
                            if (Instance.OnLoadServerListAsync != null)
                            {
                                var xml = await Instance.OnLoadServerListAsync(serverListPath);
                                if (xml != null)
                                {
                                    ServerInfo.LoadServerList(xml, serverList, groupList, recommendList, addressMapping);
                                    return;
                                }
                            }
                            {
                                var xml = await LoadServerListXmlAsync(serverListPath);
                                if (xml != null)
                                {
                                    ServerInfo.LoadServerList(xml, serverList, groupList, recommendList, addressMapping);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Instance.log.Error($"LoadServerList failed: {serverListPath}", ex);
                        }
                    }
                    {
                        Instance.log.Warn("LoadServerList From DEFAULT_SERVER_LIST_XML");
                        var xml = XmlUtil.FromString(ServerInfo.DEFAULT_SERVER_LIST_XML);
                        if (xml != null)
                        {
                            ServerInfo.LoadServerList(xml, serverList, groupList, recommendList, addressMapping);
                        }
                    }
                }
                finally
                {
                    NetClientFactory.SetAddressMapping(addressMapping);
                }
            }
            protected virtual async Task<XmlDocument> LoadServerListXmlAsync(string serverListPath)
            {
                if (await Resource.ExistDataAsync(serverListPath))
                {
                    try
                    {
                        return await XmlUtil.LoadXMLAsync(serverListPath);
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
                return null;
            }
            public virtual List<ServerInfo> GetAllServers()
            {
                return new List<ServerInfo>(serverList.Values);
            }
            public virtual ServerInfo GetServer(string id)
            {
                return serverList.Get(id);
            }

        }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Language
        public Properties LangProperties { get; private set; } = new Properties();
        public async Task LoadLangPropertiesAsync()
        {
            var langPath = $"{Config.LanguageUrl}";
            log.Info($"Load : {langPath}");
            try
            {
                this.LangProperties = await LoadLangPropertiesAsync(langPath);
            }
            catch (Exception err)
            {
                log.Error("Load Language Error : " + langPath, err);
            }
        }
        protected virtual async Task<Properties> LoadLangPropertiesAsync(string langPath)
        {
            if (await Resource.ExistDataAsync(langPath))
            {
                return await Properties.LoadFromResourceAsync(langPath);
            }
            return null;
        }
        //         public static ClientLanguageManager Language { get; protected set; }
        //         public class ClientLanguageManager
        //         {
        //             private HashMap<string, LanguageManager> _languages = new HashMap<string, LanguageManager>();
        // 
        //             public ClientLanguageManager()
        //             {
        //                 LoadLanguage(Config.LanguageRootDir);
        //             }
        // 
        //             public virtual void LoadLanguage(string path)
        //             {
        //                 _languages = LanguageManager.LoadLanguages(path);
        //             }
        // 
        //             /// <summary>
        //             /// 
        //             /// </summary>
        //             /// <param name="local_code">zh_CN, zh_TW, en_US</param>
        //             /// <returns></returns>
        //             public virtual LanguageManager GetLanguage(string local_code)
        //             {
        //                 LanguageManager ret;
        //                 if (!_languages.TryGetValue(local_code, out ret))
        //                 {
        //                     throw new Exception("语言类型不存在: " + local_code);
        //                 }
        //                 return ret;
        //             }
        // 
        //         }
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Passport
        public ClientPassportManager Passport { get; protected set; }
        public class ClientPassportManager
        {
            public virtual ClientEnterGateRequest SignPassportData(ClientEnterGateRequest req)
            {
                req.c2s_token = CMD5.CalculateMD5(req.c2s_token);
                return req;
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------
#if false
        #region Channel
        public ChannelManager Channel { get; protected set; }
        public class ChannelManager
        {
            public virtual WorldLayout CreateWorldLayout(IClientAdapter client)
            {
                return new WorldLayout(client);
            }
            //             public virtual ChannelNode CreateChannelNode(EnterChannelS2C enter)
            //             {
            //                 return new WorldChannel(enter);
            //             }
            //             public virtual ChannelObject CreateChannelObject(ChannelNode channel, ObjectEnterS2C enter)
            //             {
            //                 return new WorldAgent(enter.uuid);
            //             }
        }
        #endregion
#endif
        //-----------------------------------------------------------------------------------------------
    }
}
