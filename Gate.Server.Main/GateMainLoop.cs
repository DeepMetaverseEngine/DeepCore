using DeepCore;
// using DeepCore.Game3D.Host;
// using DeepCore.Game3D.Slave;
// using DeepCore.GameData;
// using DeepMetaGame.Data;
using DeepCore.Reflection;
using DeepCrystal;
using Gate.Client;

namespace Gate.Server.Launcher
{

    public class GateMainLoop : GateServerSingleMainLoop
    {
        public string CurrentDir = Environment.CurrentDirectory;
        //        public string BattleRoot;
        //         public Type BattleCodec = ZoneDataFactory.Codec?.GetType();
        //         public Type BattleDataFactory = ZoneDataFactory.Factory?.GetType();
        //         public Type BattleHostFactory = HostFactory?.GetType();
        //         public Type BattleSlaveFactory = SlaveFactory?.GetType();
        public int RedisPort = 16101;
        public int MysqlPort = 16102;
        public bool UseShellExecuteDB = true;
        public Type ClientCodec;
        public Type ServerCodec;
        public string GateListenHost = "127.0.0.1";
        public int GateListenPort = 19800;
        public GateMainLoop(GateSingleNodeLauncher app) : base(app)
        {
            base.RpcAppFactoryType /*       */= typeof(DeepFrozen.RPC.ICE.Launcher.IceRpcAppFactory);
            //base.LuaAdapterType /*          */= typeof(DeepCore.Template.MoonSharp.MoonSharpLuaAdapter);
//             try
//             {
//                 this.StartServiceName /*        */= ReflectionUtil.GetType("Gate.Sample.Main.Server.TestLauncherService")?.Name;
//                 this.StartServiceType /*        */= ReflectionUtil.GetType("Gate.Sample.Main.Server.TestLauncherService");
//                 this.GateServerManagerType /*   */= ReflectionUtil.GetType("Gate.Sample.Main.TestGateServerManager");
//                 this.GateClientManagerType /*   */= ReflectionUtil.GetType("Gate.Sample.Main.TestGateClientManager");
//                 this.ClientCodec /*             */= ReflectionUtil.GetType("Test.Codec.TestBattleCodec");
//                 this.ServerCodec /*             */= ReflectionUtil.GetType("Test.Codec.TestBattleCodec");
//             }
//             catch { }
//             {
//             }
        }
        public virtual void MainLoopGateTest(Properties pargs)
        {
            pargs.LoadFields(this);
            try
            {
                using (var redis = new RedisLauncher() { Port = RedisPort, UseShellExecute = UseShellExecuteDB }.Start_Redis_EXE(CurrentDir))
                using (var mysql = new MySQLLauncher() { Port = MysqlPort, UseShellExecute = UseShellExecuteDB }.Start_MySQL_EXE(CurrentDir))
                {
                    //this.RedisDumpMaintainceTime /* */= TimeSpan.FromDays(7);
                    this.RedisConnectionString /*   */= $"127.0.0.1:{RedisPort},allowAdmin=true,syncTimeout=30000,responseTimeout=30000,connectTimeout=30000;db=2";
                    this.MySQLConnectionString /*   */= $"Host=localhost;Port={MysqlPort};User ID={mysql.User};Password={mysql.Password};database=orm;";
                    this.ServerConfig.MySQLConnectorString = $"Host=localhost;Port={MysqlPort};User ID={mysql.User};Password={mysql.Password};database=gate;";
                    this.MainLoopWithProperties(pargs);
                }
                Console.WriteLine("done");
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }
        public override void MainLoopWithProperties(Properties _pargs)
        {
            _pargs.LoadFields(this);
            {
                //                Gate.Server.GateServerConfig.BattleEditorDir = BattleRoot;
                // Already init with properties
                //                 if (ZoneDataFactory.Codec == null)
                //                 {
                //                     Gate.Server.GateServerConfig.BattleCodec = BattleCodec.FullName;
                //                     Gate.Server.GateServerConfig.BattleDataFactory = BattleDataFactory.FullName;
                //                     Gate.Server.GateServerConfig.BattleHostFactory = BattleHostFactory.FullName;
                //                     Gate.Server.GateServerConfig.BattleSlaveFactory = BattleSlaveFactory.FullName;
                //                 }
//                 this.ServerConfig.RealmID = this.ServerConfig.RealmID ?? "1";
//                 this.ServerConfig.ServerListUrl = this.ServerConfig.ServerListUrl ?? new FileInfo(Path.Combine(CurrentDir, "serverlist.xml")).FullName;
                //this.ServerConfig.LanguageUrl = this.ServerConfig.LanguageUrl ?? new DirectoryInfo(Path.Combine(CurrentDir, "lang")).FullName;
                this.ServerConfig.ClientHostFactoryClass = this.ServerConfig.ClientHostFactoryClass ?? typeof(PomeloServer.NetUV.UVPomeloServerFactory).FullName;
                this.ServerConfig.ClientCodecClass = ClientCodec.FullName;
                this.ServerConfig.ServerCodecClass = ServerCodec.FullName;
            }
            {
//                Gate.Client.GateClientConfig.BattleEditorDir = Gate.Server.GateServerConfig.BattleEditorDir;
                // Already init with server factory
//                 if (ZoneDataFactory.Codec == null)
//                 {
//                     Gate.Client.GateClientConfig.BattleCodec = Gate.Server.GateServerConfig.BattleCodec;
//                     Gate.Client.GateClientConfig.BattleDataFactory = Gate.Server.GateServerConfig.BattleDataFactory;
//                     Gate.Client.GateClientConfig.BattleHostFactory = Gate.Server.GateServerConfig.BattleHostFactory;
//                     Gate.Client.GateClientConfig.BattleSlaveFactory = Gate.Server.GateServerConfig.BattleSlaveFactory;
//                 }
                //this.ClientConfig.LanguageUrl = ServerConfig.LanguageUrl;
                this.ClientConfig.ClientCodecClass = ServerConfig.ClientCodecClass;
                this.ClientConfig.ServerListUrl = ServerConfig.ServerListUrl;
            }
            {
                StartServiceConfig[nameof(GateLauncherService.GateListenHost)] = GateListenHost;
                StartServiceConfig[nameof(GateLauncherService.GateListenPort)] = GateListenPort.ToString();
            }
            base.MainLoopWithProperties(_pargs);
        }
        public void MainLoopGateTest(params string[] args)
        {
            var pargs = Properties.ParseArgs(args);
            MainLoopGateTest(pargs);
        }


    }
}
