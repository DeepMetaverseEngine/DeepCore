using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.ZoneServer.Bot;
using DeepMetaGame.ZoneServer.Server;


namespace DeepMetaGame.ZoneServer
{
    public static class Program
    {
        public static FileInfo GetExeFile()
        {
            var file = typeof(Program).Assembly.Location;
            if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                file = file.Substring(0, file.Length - 4) + ".exe";
            }
            return new FileInfo(file);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            ReflectionUtil.LoadDlls();
            if (args.Length > 0 && args[0].Equals("bot", StringComparison.OrdinalIgnoreCase))
            {
                BotRunnerMain.Main(args);
            }
            else
            {
                ServerNodeMain.Main(args);
            }
        }
    }
}
