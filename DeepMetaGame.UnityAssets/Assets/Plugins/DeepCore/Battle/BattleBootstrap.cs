
// 此代码为核心库自动复制到Unity
// 不要在Unity里改

using Cysharp.Threading.Tasks;
using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Client;
using System;
using System.IO;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{
    public delegate UniTask BootstrapHander();
    public abstract class BattleBootstrap : MonoBehaviour
    {
        [SerializeField]
        private EGameMode GameMode = RuntimeGameMode;
        public EGameMode SelfGameMode => GameMode;
        public static EGameMode RuntimeGameMode { get; protected set; } = EGameMode.GameClient;
        public static EditorTemplates Templates { get; protected set; }
        public static ZoneDataFactory DataFactory { get; protected set; }
        public static ZoneHostFactory HostFactory { get; protected set; }
        public static ZoneSlaveFactory SlaveFactory { get; protected set; }
        public static MMOClientManager ClientManager { get; protected set; }
        public static string EditorRootPath { get; protected set; }
        public static bool Finish { get; protected set; }

        protected static BootstrapHander event_OnFinish;
        public static void OnFinish(BootstrapHander value)
        {
            var done = false;
            lock (typeof(BattleBootstrap))
            {
                done = Finish;
            }
            if (done)
            {
                value.Invoke().Forget();
            }
            else
            {
                event_OnFinish += value;
            }
        }
        public static void OnFinish(Action action)
        {
            OnFinish(new BootstrapHander(() =>
            {
                action();
                return UniTask.CompletedTask;
            }));
        }
    }

    public abstract class BattleBootstrap<CODEC, DF, HF, SF, GM, UF
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        , PF
#endif
        > : BattleBootstrap
        where CODEC : class, IExternalizableFactory, new()
        //where TL : TemplateLoader
        where DF : ZoneDataFactory, new()
        where HF : ZoneHostFactory, new()
        where SF : ZoneSlaveFactory, new()
        where GM : MMOClientManager, new()
        where UF : UnityBattleFactory
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        where PF : UnityPreviewFactory, new()
#endif
    {
        private static bool starting = false;
        new public static DF DataFactory => BattleBootstrap.DataFactory as DF;
        new public static HF HostFactory => BattleBootstrap.HostFactory as HF;
        new public static SF SlaveFactory => BattleBootstrap.SlaveFactory as SF;
        new public static GM ClientManager => BattleBootstrap.ClientManager as GM;
        public static NetClientFactory ClientFactory { get; set; }
        protected abstract UF CreateUnityFactory(string root);
        private async UniTask GlobalInit()
        {
            lock (typeof(BattleBootstrap))
            {
                if (starting == true) { return; }
                starting = true;
            }
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Debug.Log($"--- {GetType().FullName}.GlobalInit() Start -----------------------------------");
                UnityDriver.SetDirver();
                var prop = UnityBattleFactory.CommandLineArgs;
                {
                    if (prop.TryGetValue("-editor", out var _))
                    {
                        if (prop.TryGetAsBool("-Preview", out var preview) && preview)
                        {
                            BattleBootstrap.RuntimeGameMode = EGameMode.Editor_Preview;
                        }
                        else if (prop.TryGetAsBool("-Resource", out var resource) && resource)
                        {
                            BattleBootstrap.RuntimeGameMode = EGameMode.Editor_Resource;
                        }
                        else if (prop.TryGetAsBool("-SceneEditor", out var sceneEditor) && sceneEditor)
                        {
                            BattleBootstrap.RuntimeGameMode = EGameMode.Editor_Scene;
                        }
                        else
                        {
                            BattleBootstrap.RuntimeGameMode = EGameMode.Editor_Preview;
                        }
                    }
                    else if (prop.TryGetValue("-battle", out var _))
                    {
                        BattleBootstrap.RuntimeGameMode = EGameMode.BattleTest;
                    }
                    else
                    {
                        BattleBootstrap.RuntimeGameMode = this.SelfGameMode;
                    }
                    Debug.Log("GameMode = " + RuntimeGameMode);
                    Debug.Log(prop.ToString());
                }
                {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    ClientFactory = new DeepCore.PomeloClient.PomeloClientFactory();
                    EditorRootPath = $"file://{Application.dataPath}/../../../Data/GameEditor";
                    if (prop.TryGetValue("-editorRoot", out var root) && Directory.Exists(root))
                    {
                        EditorRootPath = new DirectoryInfo(Path.GetFullPath(root)).FullName;
                    }
                    else if (Directory.Exists(EditorRootPath))
                    {
                    }
                    else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("Data", "GameEditor"), out var editorRoot))
                    {
                        EditorRootPath = editorRoot.FullName;
                    }
                    else
                    {
                        EditorRootPath = $"file://{Application.dataPath}/../../../Data/GameEditor";
                    }
#else
                    if (prop.TryGetValue("-editorRoot", out var root) && Directory.Exists(root))
                    {
                        EditorRootPath = new DirectoryInfo(Path.GetFullPath(root)).FullName;
                    }
#endif
                    DeepCore.IO.Resource.PathRoot = EditorRootPath;
                }
                await OnInitBegin();
                {
                    //CreateTemplateLoader(EditorRootPath);
                    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                        DeepCore.Voxel.Data.VoxelWorldManager.Instance.ToString();
                        TemplateDataCenter.ENABLE_BATCH_LOAD = true;
                        UnityIPC.Codec = ZoneDataFactory.Codec = new CODEC();
                        UnityIPC.DataFactory = BattleBootstrap.DataFactory = new DF();
                        UnityIPC.HostFactory = BattleBootstrap.HostFactory = new HF();
                        UnityIPC.SlaveFactory = BattleBootstrap.SlaveFactory = new SF();
                        ABSystem.RootPath = $"{EditorRootPath}";
#else
                        ZoneDataFactory.Codec = new CODEC();
                        BattleBootstrap.DataFactory = new DF();
                        BattleBootstrap.HostFactory = new HF();
                        BattleBootstrap.SlaveFactory = new SF();
#endif
                    }
                    if (RuntimeGameMode == EGameMode.GameClient)
                    {
                        BattleBootstrap.ClientManager = CreateClientManager();
                    }
                    CreateUnityFactory(EditorRootPath);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    new PF();
#endif
                }
                {
                    await OnLoadTemplatesAsync();
                }
                await OnInitFinish();
                lock (typeof(BattleBootstrap))
                {
                    Finish = true;
                }
                if (event_OnFinish != null)
                {
                    await event_OnFinish.Invoke();
                    event_OnFinish = null;
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
                OnError(err);
            }
            finally
            {
                stopwatch.Stop();
                Debug.Log($"--- {GetType().FullName}.GlobalInit() use {stopwatch.Elapsed} -----------------------------------");
            }
        }


        protected virtual void OnError(Exception err)
        {

        }
        protected virtual GM CreateClientManager()
        {
            var bm = new GM();
            return bm;
        }

        protected virtual async UniTask OnLoadTemplatesAsync(IRangeValue p = null)
        {
            var prop = UnityBattleFactory.CommandLineArgs;
            if (ClientManager != null)
            {
                var gateConfig = new Gate.Client.GateClientConfig
                {
                    ClientCodecClass = ZoneDataFactory.Codec.GetType().FullName,
                    BattleEditorDir = EditorRootPath,
                    BattleIsClientModel = false,
                };
                // Client Manager 会加载战斗模板
                await ClientManager.InitAsync(gateConfig, p).AsUniTask();
            }
            else
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                UnityIPC.Templates = BattleBootstrap.Templates = ZoneDataFactory.Factory.CreateEditorTemplates(EditorRootPath + "/data");
#else
                BattleBootstrap.Templates = ZoneDataFactory.Factory.CreateEditorTemplates(EditorRootPath + "/data");
#endif
                if (prop.TryGetAsBool("-meta", out var _meta) && _meta)
                {
                    // 强制Meta模式加载模板
                    await Templates.LoadAllTemplatesMetaAsync(p).AsUniTask();
                }
                else if (RuntimeGameMode == EGameMode.GameClient)
                {
                    // 游戏模式加载Meta
                    await Templates.LoadAllTemplatesMetaAsync(p).AsUniTask();
                }
                else
                {
                    // 编辑器模式，加载碎文件方便调试
                    await Templates.LoadAllTemplatesAsync(false, p).AsUniTask();
                }
            }
        }

        protected virtual UniTask OnInitFinish() => UniTask.CompletedTask;
        protected virtual UniTask OnInitBegin() => UniTask.CompletedTask;


        async void Start()
        {
            await GlobalInit();
        }
    }
    public enum EGameMode
    {
        Editor_Preview,
        Editor_Resource,
        Editor_Scene,
        BattleTest,
        GameClient,
    }
}
