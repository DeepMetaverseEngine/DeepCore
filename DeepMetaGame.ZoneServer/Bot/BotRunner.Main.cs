using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Log;
using DeepMetaGame.Data;
using System.Diagnostics;

namespace DeepMetaGame.ZoneServer.Bot
{
    public class BotRunnerMain
    {

        private static Logger log = LoggerFactory.GetLogger("BotRunnerMain");

        public static Process StartProcess(DirectoryInfo dataDir, string connectString)
        {
            return StartProcess(Program.GetExeFile(), dataDir, connectString);
        }
        public static Process StartProcess(FileInfo exefile, DirectoryInfo dataDir, string connectString)
        {
            Launcher.DataDir = dataDir;
            Launcher.ServerAddress = connectString;
            var args = Launcher.GetArguments();
            Launcher.SaveArguments(exefile, args, ".bot.properties");

            var p = new Process();
            p.StartInfo.FileName = exefile.FullName;
            p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            p.StartInfo.Arguments = "bot " + args;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            return p;
        }

        public static void StartMain(DirectoryInfo dataDir, string connectString)
        {
            Launcher.DataDir = dataDir;
            Launcher.ServerAddress = connectString;
            var args = Launcher.GetArguments();
            Main(Properties.SplitArgs(args));
        }



        public static int Main(string[] args)
        {
            try
            {
                var exefile = new FileInfo(Environment.ProcessPath);
                var prop = DeepCore.Properties.ParseArgs(args, "=");
                if (Launcher.TryLoadArguments(exefile, out var gargs, ".bot.properties"))
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
                Launcher.SaveArguments(exefile, Launcher.GetArguments(), ".bot.properties");

                BotRunner server = new BotRunner(
                    ZoneDataFactory.Factory,
                    Launcher.SlaveFactory,
                    Launcher.DataDir.FullName,
                    Launcher.ServerAddress);
                Console.Title = $"Bots host={Launcher.ServerAddress}";
                try
                {
                    int force = 2;
                    int count = 20;
                    if (prop.TryGetAsInt("force", out var _force))
                    {
                        force = _force;
                    }
                    if (prop.TryGetAsInt("count", out var _count))
                    {
                        count = _count;
                    }
                    BotRunner.Instance.AddBots(count, force);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
                new BotConsoleCommand(server).MainLoop();
            }
            catch (Exception err)
            {
                OnError(err);
                return -1;
            }
            return 0;
        }
        private static void OnError(Exception err)
        {
            log.Error(err.Message, err);
            log.Error("Press Any Key To Exit !!!");
            Console.In.Read();
            Environment.ExitCode = -1;
        }


    }
}
