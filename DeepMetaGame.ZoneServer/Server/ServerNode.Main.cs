using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Slave;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.Command;
using DeepCrystal.Server;
using System.Diagnostics;

namespace DeepMetaGame.ZoneServer.Server
{
    /// <summary>
    /// 独立进程
    /// </summary>
    public static class ServerNodeMain
    {
        public static string BeginProcess(FileInfo exefile, DirectoryInfo dataDir, int sceneID, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory)
        {
            Launcher.DataDir = dataDir;
            Launcher.SceneID = sceneID;
            Launcher.HostFactory = hostFactory;
            Launcher.SlaveFactory = slaveFactory;
            var args = Launcher.GetArguments();
            Launcher.SaveArguments(exefile, args, ".server.properties");
            return args;
        }

        public static Process StartProcess(DirectoryInfo dataDir, int sceneID, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory)
        {
            return StartProcess(Program.GetExeFile(), dataDir, sceneID, hostFactory, slaveFactory);
        }
        public static Process StartProcess(FileInfo exefile, DirectoryInfo dataDir, int sceneID, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory)
        {
            var args = BeginProcess(exefile, dataDir, sceneID, hostFactory, slaveFactory);
            var p = new Process();
            p.StartInfo.FileName = exefile.FullName;
            p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.LoadUserProfile = true;
            p.Start();
            return p;
        }

        public static ServerNode StartMain(string args)
        {
            return StartMain(Properties.SplitArgs(args));
        }
        public static ServerNode StartMain(DirectoryInfo dataDir, int sceneID)
        {
            Launcher.DataDir = dataDir;
            Launcher.SceneID = sceneID;
            Launcher.Port = 14000;
            var args = Launcher.GetArguments();
            return ServerNodeMain.StartMain(args);
        }
        public static ServerNode StartMain(string[] args)
        {
            var exefile = new FileInfo(Environment.ProcessPath);
            var prop = DeepCore.Properties.ParseArgs(args, "=");
            if (Launcher.TryLoadArguments(exefile, out var gargs, ".server.properties"))
            {
                var saved = Properties.ParseArgs(gargs);
                foreach (var arg in saved)
                {
                    if (!prop.ContainsKey(arg.Key))
                    {
                        prop.Add(arg.Key, arg.Value);
                    }
                }
            }
            Launcher.InitFactory(prop);
            var sceneID = Launcher.SceneID;
            var port = Launcher.Port;
            Launcher.HostFactory.BindLogger(log);
            var templates = Launcher.Templates;
            var server = new ServerNode(templates, sceneID, new ServerConfig() { Host = "127.0.0.1", Port = port }, Launcher.HostFactory);
            try
            {
                Console.Title = $"ServerNode port={port}";
            }
            catch { }
            Launcher.SaveArguments(exefile, Launcher.GetArguments(), ".server.properties");

            return server;
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public static string Usage
        {
            get
            {
                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"{Launcher.KEY_DATA_CLASS}=ZoneDataFactory");
                sb.AppendLine($"{Launcher.KEY_HOST_CLASS}=ZoneHostFactory");
                sb.AppendLine($"{Launcher.KEY_NAME}={nameof(ServerNodeMain)}");
                sb.AppendLine($"{Launcher.KEY_WORK_DIR}=Work Dir");
                sb.AppendLine($"{Launcher.KEY_DATA_DIR}=Data Dir");
                sb.AppendLine($"{Launcher.KEY_SCENE_ID}=scene ID");
                sb.AppendLine($"{Launcher.KEY_PORT}=listen Port");

                return sb.ToString();
            }
        }

        private static Logger log = LoggerFactory.GetLogger("ServerNodeMain");


        public static void Main(string[] args)
        {
            try
            {
                var server = StartMain(args);
                var zone = server.StartAsync().WaitForResult();
                var console = new NodeConsoleCommand();
                console.MainLoop();
                server.StopAsync().Wait();
            }
            catch (Exception err)
            {
                log.Info(Usage);
                OnError(err);
            }
        }

        private static void OnError(Exception err)
        {
            log.Error(err.Message, err);
            log.Error("Press Any Key To Exit !!!");
            Console.In.Read();
            Environment.ExitCode = -1;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        class NodeConsoleCommand : ConsoleCommandList
        {
            [Desc("进程查看")]
            public class CMD_PI : AbstractCommand
            {
                public override string Key { get { return "pi"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    TypeAllocRecorder.PrintProcessStatus(output, System.Diagnostics.Process.GetCurrentProcess(), " ", 32);
                }
            }
            [Desc("内存查看")]
            public class CMD_AC : AbstractCommand
            {
                public override string Key { get { return "ac"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    output.PrintLineSeparator();
                    TypeAllocRecorder.PrintMemoryStatus(output);
                    output.PrintLineSeparator();
                }
            }
            [Desc("内存清理")]
            public class CMD_GC : AbstractCommand
            {
                public override string Key { get { return "gc"; } }
                public override string Help { get { return "gc <generation>"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    if (Parser.TryParseInt(arg, out var gen)) { GC.Collect(gen); }
                    else { GC.Collect(); }
                    output.PrintLineSeparator();
                    TypeAllocRecorder.PrintMemoryStatus(output);
                    output.PrintLineSeparator();
                }
            }
            [Desc("缓存查看")]
            public class CMD_POOL : AbstractCommand
            {
                public override string Key { get { return "pool"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    output.PrintLineSeparator();
                    ObjectPools.PrintStatus(output);
                    output.PrintLineSeparator();
                }
            }
            [Desc("缓存清理")]
            public class CMD_POOL_CLEAR : AbstractCommand
            {
                public override string Key { get { return "poolc"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    ObjectPools.ClearPool();
                    output.PrintLineSeparator();
                    ObjectPools.PrintStatus(output);
                    output.PrintLineSeparator();
                }
            }
            [Desc("RPC协议统计")]
            public class CMD_ST : AbstractCommand
            {
                private Type etype = typeof(DeepFrozen.RPC.Invoker.RpcStatistics.SortField);
                public override string Key { get { return "st"; } }
                public override string Help
                {
                    get
                    {
                        return "st <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
                    }
                }
                public override void DoCommand(string arg, TextWriter output)
                {
                    var sort = DeepFrozen.RPC.Invoker.RpcStatistics.SortField.NAME;
                    if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
                    {
                        sort = (DeepFrozen.RPC.Invoker.RpcStatistics.SortField)sortobj;
                    }
                    DeepFrozen.RPC.Invoker.RpcStatistics.PrintStatus(output, sort, " ", 64, 150);
                }
            }
            [Desc("时间耗时统计")]
            public class CMD_STT : AbstractCommand
            {
                private Type etype = typeof(DeepCore.Statistics.TimeStatisticsRecoder.SortField);
                public override string Key { get { return "stt"; } }
                public override string Help
                {
                    get
                    {
                        return "stt <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
                    }
                }
                public override void DoCommand(string arg, TextWriter output)
                {
                    var sort = DeepCore.Statistics.TimeStatisticsRecoder.SortField.NAME;
                    if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
                    {
                        sort = (DeepCore.Statistics.TimeStatisticsRecoder.SortField)sortobj;
                    }
                    DeepCore.Statistics.TimeStatisticsRecoder.PrintAllStatus(output, sort, " ", 64, 150);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
    }

}
