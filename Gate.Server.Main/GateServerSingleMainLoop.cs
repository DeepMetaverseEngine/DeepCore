using DeepCore;
using DeepCore.Log;
using DeepCore.Lua;
using DeepCore.Reflection;
using DeepFrozen.RPC.Launcher;
using Gate.Client;
using Gate.Server.Protocol;
using System;
using System.Xml;

namespace Gate.Server.Launcher
{
    public class GateSingleNodeLauncher : SingleNodeLauncher
    {
        public GateSingleNodeLauncher()
        {
        }
        protected override bool StartNodes(XmlDocument xml)
        {
            if (base.StartNodes(xml))
            {
                this.NameServerLauncher.Broadcast(new SyncGateServerOpen() { status = true });
                return true;
            }
            return false;
        }
    }


    public class GateServerSingleMainLoop
    {
        //--------------------------------------------------------------------
        public Type RpcAppFactoryType;
        //public Type LuaAdapterType;
        //--------------------------------------------------------------------
        public string StartServiceName = nameof(GateLauncherService);
        public Type StartServiceType = typeof(GateLauncherService);
        public Properties MainConfig;
        public Properties StartServiceConfig = new Properties();
        public Properties ServiceMapping = new Properties();
        public Properties GlobalConfig = new Properties();
        public Properties GateServerConfig = new Properties();
        public Properties GateClientConfig = new Properties();
        //--------------------------------------------------------------------
        public Type GateServerManagerType = typeof(GateServerManager);
        public Type GateClientManagerType = typeof(GateClientManager);
        public GateServerConfig ServerConfig = new GateServerConfig();
        public GateClientConfig ClientConfig = new GateClientConfig();
        //--------------------------------------------------------------------
        public string RedisConnectionString;
        public string MySQLConnectionString;
        public TimeSpan RedisDumpMaintainceTime = TimeSpan.FromDays(7);
        //--------------------------------------------------------------------
        public GateSingleNodeLauncher App { get; }
        public Logger log => App.log;
        //--------------------------------------------------------------------
        public GateServerSingleMainLoop(GateSingleNodeLauncher app) { App = app; }
        public bool TryLoadMainConfig<T>(string key, out T _value)
        {
            if (MainConfig.TryGetAs<T>(nameof(RpcAppFactoryType), out _value))
            {
                log.Info($"Load Main Config : {key}");
                return true;
            }
            return false;
        }
        public bool TryLoadMainConfig(string key, out string _value)
        {
            log.Info($"Try Load Main Config : {key}");
            if (MainConfig.TryGetValue(nameof(RpcAppFactoryType), out _value))
            {
                return true;
            }
            return false;
        }
        public Properties LoadSubConfig(string prefix)
        {
            log.Info($"Load Sub Config : {prefix}.*");
            return MainConfig.SubProperties($"{prefix}.");
        }

        public void PostExitMainLoop()
        {
            App?.PostExitMainLoop();
        }
        public virtual void MainLoopWithProperties(Properties _pargs)
        {
            _pargs.LoadFields(this);
            MainConfig = _pargs;
            log.Info($"Load MainConfig : \n{MainConfig.ToString("  ")}");

            if (TryLoadMainConfig(nameof(RpcAppFactoryType), out var _value))
            {
                RpcAppFactoryType = ReflectionUtil.GetType(_value);
            }
//             if (TryLoadMainConfig(nameof(LuaAdapterType), out _value))
//             {
//                 LuaAdapterType = ReflectionUtil.GetType(_value);
//             }
            if (TryLoadMainConfig(nameof(StartServiceName), out _value))
            {
                StartServiceName = _value;
            }
            if (TryLoadMainConfig(nameof(StartServiceType), out _value))
            {
                StartServiceType = ReflectionUtil.GetType(_value);
            }
            if (TryLoadMainConfig(nameof(GateServerManagerType), out _value))
            {
                GateServerManagerType = ReflectionUtil.GetType(_value);
            }
            if (TryLoadMainConfig(nameof(GateClientManagerType), out _value))
            {
                GateClientManagerType = ReflectionUtil.GetType(_value);
            }
            if (TryLoadMainConfig(nameof(RedisConnectionString), out _value))
            {
                RedisConnectionString = _value;
            }
            if (TryLoadMainConfig(nameof(MySQLConnectionString), out _value))
            {
                MySQLConnectionString = _value;
            }
            if (TryLoadMainConfig(nameof(RedisDumpMaintainceTime), out _value))
            {
                RedisDumpMaintainceTime = TimeSpan.Parse(_value);
            }

            StartServiceConfig.PutAll(LoadSubConfig($"{nameof(StartServiceConfig)}"));
            ServiceMapping.PutAll(LoadSubConfig($"{nameof(ServiceMapping)}"));
            GlobalConfig.PutAll(LoadSubConfig($"{nameof(GlobalConfig)}"));
            {
                log.Info($"Default GateServerConfig : \n{Properties.GetDefaultFields(ServerConfig, $"  {nameof(GateServerConfig)}.")}");
                log.Info($"Default GateClientConfig : \n{Properties.GetDefaultFields(ClientConfig, $"  {nameof(GateClientConfig)}.")}");

                GateServerConfig.PutAll(LoadSubConfig($"{nameof(GateServerConfig)}"));
                GateClientConfig.PutAll(LoadSubConfig($"{nameof(GateClientConfig)}"));
            }
            MainLoopSingleService();
        }
        public virtual void MainLoopSingleService()
        {
            log.Info($"Connecting Redis ...");
            log.Info($"Connecting MySQL ...");
            using (var redis = new DeepCrystal.ORM.Redis.RedisORMFactory(RedisConnectionString, MySQLConnectionString))
            {
                //--------------------------------------------------------------------------------------------------------------
                //把冷数据落地到MySQL，默认1小时冷数据，线上可以设置为7天
                if (RedisDumpMaintainceTime.Ticks > 0)
                {
                    log.Info($"RedisDumpMaintaince ...");
                    DeepCrystal.ORM.Redis.RedisDump.MaintainceAllDump(RedisDumpMaintainceTime);
                }
                //--------------------------------------------------------------------------------------------------------------
                if (RpcAppFactoryType != null)
                {
                    log.Info($"Init RpcAppFactory : {RpcAppFactoryType}");
                    ReflectionUtil.CreateInterface<RpcAppFactory>(RpcAppFactoryType);
                }
//                 if (LuaAdapterType != null)
//                 {
//                     log.Info($"Init LuaAdapter : {LuaAdapterType}");
//                     ReflectionUtil.CreateInterface<ILuaAdapter>(LuaAdapterType);
//                 }
                //--------------------------------------------------------------------------------------------------------------
                //初始化Gate全局配置
                if (GateServerConfig != null)
                {
                    log.Info($"Load GateServerConfig :");
                    log.Info(GateServerConfig);
                    GateServerConfig.LoadFields(ServerConfig);
                }
                if (GateServerManagerType != null)
                {
                    if (GateServerManager.Instance == null)
                    {
                        log.Info($"Init GateServerManager : {GateServerManagerType}");
                        ReflectionUtil.CreateInterface<GateServerManager>(GateServerManagerType, ServerConfig).Init();
                    }
                }
                if (GateClientConfig != null)
                {
                    log.Info($"Load GateClientConfig : ");
                    log.Info(GateClientConfig);
                    GateClientConfig.LoadFields(ClientConfig);
                }
                if (GateClientManagerType != null)
                {
                    if (GateClientManager.Instance == null)
                    {
                        log.Info($"Init GateClientManager : {GateClientManagerType}");
                        ReflectionUtil.CreateInterface<GateClientManager>(GateClientManagerType).Init(ClientConfig);
                    }
                }
                //--------------------------------------------------------------------------------------------------------------
                //服务类型映射
                //                 if (this.GateServerNames != null)
                //                 {
                //                     log.Info($"Load GateServerNames : ");
                //                     log.Info(this.GateServerNames);
                //                     this.GateServerNames.LoadStaticFields(typeof(ServerNames));
                //                 }
                var mappings = new Properties();
                GateServerManager.ServerName.GetServiceMapping(mappings);
                mappings.Put(StartServiceName, StartServiceType.FullName);
                //                 mappings.Put(ServerNames.GateServerType, ServerNames.GateServerClassName);
                //                 mappings.Put(ServerNames.ConnectServerType, ServerNames.ConnectServerClassName);
                //                 mappings.Put(ServerNames.SessionServiceType, ServerNames.SessionServiceClassName);
                //                 mappings.Put(ServerNames.LogicServiceType, ServerNames.LogicServiceClassName);
                //                 mappings.Put(ServerNames.AreaServiceType, ServerNames.AreaServiceClassName);
                //                 mappings.Put(ServerNames.AreaManagerType, ServerNames.AreaManagerClassName);
                if (ServiceMapping != null)
                {
                    var mappingset = ServiceMapping.ToArray();
                    Array.Sort(mappingset, (a, b) => a.Key.CompareTo(b.Key));
                    foreach (var mapping in mappingset)
                    {
                        mappings.Put(mapping.Key, mapping.Value);
                        log.Info($"Service Mapping : {mapping.Key} = {mapping.Value} ");
                    }
                }
                //--------------------------------------------------------------------------------------------------------------
                //启动极简服务，只需要提供服务名和服务类型
                AppDomain.CurrentDomain.ProcessExit += (sender, evt) =>
                {
                    App.PostExitMainLoop();
                    App.WaitForExit();
                };
                App.MainLoopSingleService(new SingleNodeLauncherArgs()
                {
                    ServiceName = StartServiceName,
                    ServiceType = StartServiceName,
                    ServiceConfig = StartServiceConfig,
                    RpcCodec = GateServerManager.ServerRPCCodecType,
                    GlobalConfig = GlobalConfig,
                    ServiceMapping = mappings,
                });
            }
        }
    }

}
