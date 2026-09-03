using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.NetClient;
using DeepCore.PomeloClient;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{



    public class UnityIPC : PreviewBehavior
    {
        public static UnityIPC IPC { get; private set; }
        public static UnityRTG RTG { get => UnityRTG.RTG; }
        public static string EditorRootDir { get; set; }
        public static string BinaryRootDir { get; set; }
        public static IExternalizableFactory Codec { get; set; } //= $"Tiny.Client.GenClient.TinyBattleCodec";
        public static ZoneDataFactory DataFactory { get; set; } //= $"XTiny.Core.XTinyZoneDataFactory";
        public static ZoneHostFactory HostFactory { get; set; }
        public static ZoneSlaveFactory SlaveFactory { get; set; }
        public static EditorTemplates Templates { get; set; }
        public static INetClient Session { get; protected set; }
        public static TimeTaskQueue TimeTasks { get; protected set; }
        public static MessageActionQueue<UnityIPC> MainTasks { get; protected set; }

        public static SingleThreadCollectionPool ObjectPool { get; } = new SingleThreadCollectionPool();
        public static void PLog(object message)
        {
            if (message is Exception ex)
            {
                Debug.LogError(ex);
                Session?.Notify(new MsgPluginLog() { message = $"{ex.Message}\n{ex.StackTrace}" });
            }
            else if (message is string txt)
            {
                Debug.Log(txt);
                Session?.Notify(new MsgPluginLog() { message = txt });
            }
            else if (message is PreviewUpdate preview)
            {
                var msg = $"PreviewUpdate : {preview.Template} - {preview.Relation}";
                Debug.Log(msg);
                Session?.Notify(new MsgPluginLog() { message = msg });
            }
            else
            {
                Debug.Log(message);
                Session?.Notify(new MsgPluginLog() { message = $"{message}" });
            }
        }
        public static string GetResourceFullPath(string subName)
        {
            return EditorRootDir + Path.DirectorySeparatorChar + subName;
        }
        public virtual bool TryGetResourceProperties(string resName, out IResourceProperties resProp)
        {
            resProp = default;
            if (string.IsNullOrEmpty(resName)) return false;
            return UnityIPC.Templates.Templates.TryGetResourceProperties(resName, out resProp);
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        [SerializeField] public string Host = "127.0.0.1";
        [SerializeField] public int Port = 19900;
        //---------------------------------------------------------------------------------------------------------------------------------
        #region Parent HWND
        public Properties CommandArgs { get; private set; }
        private int parentHWND;
        private int keepHWND = 0;
        private IntPtr lastFocuse = IntPtr.Zero;
        public int ParentHWND { get => parentHWND; }
        public IntPtr CurrentHWND { get; private set; }
        public virtual string GetClipboardTransform(string name)
        {
            try
            {
                var root = EditorRootDir;
                return File.ReadAllText(Path.Combine(root, ".clipboard_transform", name));
            }
            catch
            {
                //err.PrintStackTrace();
            }
            return null;
        }
        private bool CheckParentWND()
        {
            try
            {
                if (keepHWND != 0)
                {
                    var ownerHWND = UnityApp.GetWindow(keepHWND);
                    if (ownerHWND == IntPtr.Zero)
                    {
                        if (!Application.isEditor)
                        {
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }
                        return false;
                    }
                }
            }
            catch (Exception err)
            {
                PLog(err);
                Debug.LogError(err);
                if (!Application.isEditor)
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                    //Environment.Exit(0);
                }
                return false;
            }
            return true;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------
        public float IntervalMS { get; private set; }
        public GUITextureManager Textures { get; protected set; } = new GUITextureManager();

        private double passTimeSEC;
        protected virtual void Awake()
        {
            IPC = this;
            this.CurrentHWND = UnityApp.GetCurrentWindowHandle();
            this.parentHWND = UnityApp.ParentHWND();
            this.keepHWND = parentHWND;
            Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
            Application.focusChanged += Application_focusChanged;
            //ABSystemImpl.RootPath = $"{EditorRootDir}";
            //UnityDriver.SetDirver();
            TimeTasks = new TimeTaskQueue(ObjectPool);
            MainTasks = new MessageActionQueue<UnityIPC>();
        }

        protected virtual void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
            {
                PLog($"{type} : {condition.TrimEnd()} : {stackTrace}");
            }
        }
        private void Application_focusChanged(bool obj)
        {
            CheckParentWND();
        }

        protected virtual void Start()
        {
            var args = Environment.GetCommandLineArgs();
            Debug.Log(CUtils.ArrayToString(args, "\n"));
            var prop = CommandArgs = Properties.ParseArgs(args);
            if (string.IsNullOrEmpty(EditorRootDir))
            {
                if (prop.TryGetValue("-editorRoot", out var root) && Directory.Exists(root))
                {
                    EditorRootDir = root;
                }
                else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("Data", "GameEditor"), out var editorRoot))
                {
                    EditorRootDir = editorRoot.FullName;
                }
            }
            if (string.IsNullOrEmpty(BinaryRootDir))
            {
                if (prop.TryGetValue("-binaryRoot", out var binRoot) && Directory.Exists(binRoot))
                {
                    BinaryRootDir = binRoot;
                }
                //                 else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("Data", "GameEditor", "bin"), out var binaryRoot))
                //                 {
                //                     BinaryRootDir = binaryRoot.FullName;
                //                 }
            }
            {
                if (prop.TryGetValue("-dataFactory", out var factory))
                {
                    var df = ReflectionUtil.CreateInterface<ZoneDataFactory>(factory);
                    if (df != null) { DataFactory = df; }
                }
                if (prop.TryGetValue("-hostFactory", out var hostFactory))
                {
                    var df = ReflectionUtil.CreateInterface<ZoneHostFactory>(hostFactory);
                    if (df != null) { HostFactory = df; }
                }
                if (prop.TryGetValue("-slaveFactory", out var slaveFactory))
                {
                    var df = ReflectionUtil.CreateInterface<ZoneSlaveFactory>(slaveFactory);
                    if (df != null) { SlaveFactory = df; }
                }
                if (prop.TryGetValue("-codec", out var codec))
                {
                    var df = ReflectionUtil.CreateInterface<IExternalizableFactory>(codec);
                    if (df != null) { Codec = df; }
                }
                if (prop.TryGetValue("-host", out var host))
                {
                    Host = host;
                }
                if (prop.TryGetAsInt("-port", out var port))
                {
                    Port = port;
                }
            }
            if (Directory.Exists(BinaryRootDir))
            {
                Debug.Log($"Assemby Dir = {BinaryRootDir}");
                ReflectionUtil.LoadDllsNoLock(new DirectoryInfo(BinaryRootDir), 0);
            }
            //             else
            //             {
            //                 var dir = new FileInfo(typeof(ReflectionUtil).Assembly.Location).Directory;
            //                 Debug.Log($"Assemby Dir = {dir}");
            //                 ReflectionUtil.LoadDllsNoLock(dir, 2);
            //             }
            try
            {

                Debug.Log(ZoneDataFactory.Factory.GetType());
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            try
            {
                if (Codec != null)
                {
                    ZoneDataFactory.Codec = Codec;
                }
                Debug.Log(ZoneDataFactory.Codec.GetType());
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
            DeepCore.Voxel.Data.VoxelWorldManager.Instance.ToString();
            //             Templates = ZoneDataFactory.Factory.CreateEditorTemplates(EditorRootDir + "/data", true);
            //             Templates.LoadAllTemplates();
            if (!string.IsNullOrEmpty(Host))
            {
                try
                {
                    Session = PomeloClientFactory.IOInstance.CreateClient(ZoneDataFactory.Factory.MessageCodec);
                    Session.MainHandleNotify += Session_MainHandleNotify;
                    Session.NetError += Session_NetError;
                    Session.OnDisconnected += Session_OnDisconnected;
                    Session.Connect(Host, Port, TimeSpan.FromSeconds(15), null, Session_Connected);
                }
                catch (Exception err)
                {
                    PLog(err);
                    Debug.LogError(err);
                }
            }
        }

        protected virtual void Update()
        {
            try
            {
                if (TimeTasks != null) TimeTasks.UpdatePassTime((passTimeSEC * 1000));
                if (MainTasks != null) MainTasks.ProcessMessages(this);
                if (Session != null) Session?.Update();
                OnUpdate(Time.deltaTime);
            }
            catch (Exception err)
            {
                PLog(err);
                Debug.LogError(err);
            }
            finally
            {
                this.passTimeSEC += Time.deltaTime;
            }
            CheckParentWND();
        }
        protected virtual void OnUpdate(float deltaSEC) { }
        protected virtual void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                //                  var current = UnityApp.GetFocus();
                //                  if (lastFocuse != current)
                //                  {
                //                      UnityApp.SetFocus(CurrentHWND);
                //                      lastFocuse = UnityApp.GetFocus();
                //                      Debug.Log($"SetFocus : current={current} CurrentHWND={CurrentHWND} lastFocuse={lastFocuse} parentHWND={parentHWND}");
                //                  }
                Session?.Notify(new MsgFocusHWND() { message = CurrentHWND });
            }
        }
        protected virtual void OnDestroy()
        {
            Textures.Dispose();
            Session?.Dispose();
            MainTasks?.Dispose();
            TimeTasks?.Dispose();
        }
        public T CreateDisplay<T>(string name) where T : DisplayObject
        {
            var go = new GameObject(name);
            var to = go.AddComponent<T>();
            go.transform.SetParent(this.transform, false);
            return to;
        }

        //---------------------------------------------------------------------------------------------------------------------------------
        #region _IPC_
        public void MainInvoke(Action action)
        {
            MainTasks?.Enqueue(action);
        }
        protected virtual void Session_NetError(Exception obj)
        {
            PLog(obj);
        }
        protected virtual void Session_Connected(Exception err, object message)
        {
            Session.Notify(new MsgUnityIsReady()
            {
                UnityHWND = CurrentHWND
            });
            if (message is ISerializable ser)
            {
                if (message is MsgUnityToken init)
                {
                    if (init.KeepHWND != IntPtr.Zero)
                    {
                        keepHWND = init.KeepHWND.ToInt32();
                    }
                    Session_Validate(init.State);
                }
                try
                {
                    HandleFromSession?.Invoke(ser);
                }
                catch (Exception e) { PLog(e); }
            }
        }
        protected virtual void Session_OnDisconnected(CloseReason arg1, string arg2)
        {
            CheckParentWND();
        }

        protected virtual void Session_Validate(ISerializable state)
        {

        }
        protected virtual void Session_MainHandleNotify(object message)
        {
            if (message is ISerializable ser)
            {
                try
                {
                    HandleFromSession?.Invoke(ser);
                }
                catch (Exception e) { PLog(e); }
            }
        }

        public event Action<ISerializable> HandleFromSession;
        public void SendToSession(ISerializable data)
        {
            //PLog($"< {data}");
            if (Session != null && Session.IsConnected)
                Session.Notify(data);
        }
        public void SendToSession(ISerializable data, Action<ISerializable> callback)
        {
            //PLog($"< {data}");
            if (Session != null && Session.IsConnected)
                Session.Request(data, (err, rsp) => callback(rsp));
            else
                callback(null);
        }
        public void SendToSession<RSP>(ISerializable data, Action<RSP> callback) where RSP : class, ISerializable
        {
            //PLog($"< {data}");
            if (Session != null && Session.IsConnected)
                Session.Request<RSP>(data, (err, rsp) => callback(rsp));
            else
                callback(null);
        }
        public void RefreshHWND()
        {
            Session?.Notify(new MsgRefreshHWND());
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------
    }


    public class UnityIPCLogger : DeepCore.Log.Logger
    {
        public UnityIPCLogger(object name) : base(LoggerFactory.CurrentFactory, name) { 
        
        }
        protected override void PrintText(string text, LoggerLevel level)
        {
            UnityIPC.PLog(text);
        }
    }
}
