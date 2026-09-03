using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Server;
using System.Text;

namespace DeepMetaGame.ZoneServer
{
    public static class Launcher
    {
        public const string KEY_NAME = "name";
        public const string KEY_CODEC = "codec";
        public const string KEY_DATA_CLASS = "data_class";
        public const string KEY_HOST_CLASS = "host_class";
        public const string KEY_SLAVE_CLASS = "slave_class";
        public const string KEY_WORK_DIR = "work";

        public const string KEY_DATA_DIR = "dir";

        public const string KEY_CONNECT_STRING = "address";
        public const string KEY_PORT = "port";
        public const string KEY_SCENE_ID = "scene";

        //-----------------------------------------------------------------
        public static string ServerAddress { get; set; } = "127.0.0.1:14000";
        public static int Port { get; set; } = 14000;
        public static int SceneID { get; set; }
        public static EditorTemplates Templates { get; set; }
        public static DirectoryInfo DataDir { get; set; }
        public static ZoneHostFactory HostFactory { get; set; }
        public static ZoneSlaveFactory SlaveFactory { get; set; }
        //-----------------------------------------------------------------
        private static Logger log = LoggerFactory.GetLogger("Launcher");
        public static string GetArguments()
        {
            var sb = new StringBuilder();
            sb.Append($"{Launcher.KEY_NAME}=ZoneServer ");
            sb.Append($"{Launcher.KEY_CODEC}={ZoneDataFactory.Codec.GetType().FullName} ");
            sb.Append($"{Launcher.KEY_DATA_CLASS}={ZoneDataFactory.Factory.GetType().FullName} ");
            sb.Append($"{Launcher.KEY_HOST_CLASS}={HostFactory.GetType().FullName} ");
            sb.Append($"{Launcher.KEY_SLAVE_CLASS}={SlaveFactory.GetType().FullName} ");
            sb.Append($"{Launcher.KEY_WORK_DIR}=\"{Path.GetFullPath(Environment.CurrentDirectory)}\" ");
            sb.Append($"{Launcher.KEY_DATA_DIR}=\"{DataDir.FullName}\" ");
            sb.Append($"{Launcher.KEY_SCENE_ID}={SceneID} ");
            sb.Append($"{Launcher.KEY_CONNECT_STRING}={ServerAddress} ");
            sb.Append($"{Launcher.KEY_PORT}={Port} ");
            return sb.ToString();
        }
        public static void SaveArguments(FileInfo exefile, string args, string suffix = ".properties")
        {
            var startFile = exefile.FullName + suffix;
            if (exefile.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                startFile = exefile.FullName.Substring(0, exefile.FullName.Length - 4) + suffix;
            }
            if (exefile.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                startFile = exefile.FullName.Substring(0, exefile.FullName.Length - 4) + suffix;
            }
            File.WriteAllText(startFile, args);
        }
        public static bool TryLoadArguments(FileInfo exefile, out string args, string suffix = ".properties")
        {
            var startFile = exefile.FullName + suffix;
            if (exefile.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                startFile = exefile.FullName.Substring(0, exefile.FullName.Length - 4) + suffix;
            }
            if (exefile.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                startFile = exefile.FullName.Substring(0, exefile.FullName.Length - 4) + suffix;
            }
            if (File.Exists(startFile))
            {
                args = File.ReadAllText(startFile);
                return args != null;
            }
            args = null;
            return false;
        }
        //-----------------------------------------------------------------
        public static void InitFactory(Properties prop)
        {
            if (prop.TryGetValue(KEY_WORK_DIR, out var work))
            {
                work = Path.GetFullPath(work);
                log.Info($"{KEY_WORK_DIR} = {work}");
                Environment.CurrentDirectory = work;
            }
            try
            {
                if (ZoneDataFactory.Codec == null)
                {
                    string codec = prop[KEY_CODEC] + "";
                    ZoneDataFactory.Codec = ReflectionUtil.CreateInterface<IExternalizableFactory>(codec);
                }
                if (ZoneDataFactory.Factory == null)
                {
                    string data_class = prop[KEY_DATA_CLASS] + "";
                    var data_factory = ReflectionUtil.CreateInterface<ZoneDataFactory>(data_class);
                    if (data_factory == null) throw new Exception($"{nameof(ZoneDataFactory)} Not Exist : {data_class}");
                }
                if (HostFactory == null)
                {
                    string host_class = prop[KEY_HOST_CLASS] + "";
                    var host_factory = ReflectionUtil.CreateInterface<ZoneHostFactory>(host_class);
                    if (host_factory == null) throw new Exception($"{nameof(ZoneHostFactory)} Not Exist : {host_class}");
                    Launcher.HostFactory = host_factory;
                }
                if (SlaveFactory == null)
                {
                    string slave_class = prop[KEY_SLAVE_CLASS] + "";
                    var host_factory = ReflectionUtil.CreateInterface<ZoneSlaveFactory>(slave_class);
                    if (host_factory == null) throw new Exception($"{nameof(ZoneSlaveFactory)} Not Exist : {slave_class}");
                    Launcher.SlaveFactory = host_factory;
                }
                {
                    var dataDir = prop.Get(KEY_DATA_DIR);
                    DataDir = new DirectoryInfo(dataDir);
                    //Templates.EditorRoot = DataDir.Parent.FullName;

                    Templates = ZoneDataFactory.Factory.CreateEditorTemplates(dataDir);
                    Templates.LoadAllTemplates();
                    SceneID = Templates.Templates.DefaultConfig.DEFAULT_SCENE;

                }
            }
            catch (Exception err)
            {
                throw new Exception("编辑器插件初始化失败 : " + err.Message, err);
            }

            if (prop.TryGetValue(KEY_CONNECT_STRING, out var addr))
            {
                ServerAddress = addr;
            }
            if (prop.TryGetAsInt(KEY_PORT, out var port))
            {
                Port = port;
            }
            if (prop.TryGetAsInt(KEY_SCENE_ID, out var scendID))
            {
                SceneID = scendID;
            }
        }


    }
}
