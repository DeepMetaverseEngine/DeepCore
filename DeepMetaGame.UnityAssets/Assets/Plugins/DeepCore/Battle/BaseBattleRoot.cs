using Cysharp.Threading.Tasks;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Net;
using DeepCore.Unity;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.OnGUI;
using System;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{

    public abstract class BaseBattleRoot : MonoBehaviour
    {
        public static BaseBattleRoot Instance { get; private set; }
        public EditorTemplates Templates => BattleBootstrap.Templates;
        public ZoneHostFactory HostFactory => BattleBootstrap.HostFactory;
        public ZoneSlaveFactory SlaveFactory => BattleBootstrap.SlaveFactory;
        // Start is called before the first frame update
        //---------------------------------------------------------------------------------------------
        public int SceneID = 3000799;
        public int ActorTemplateID = 1000001;
        public int ActorForce = 2;
        //public string EditorRootPath;
        public string BattleHost = "";
        public bool IsThreadBattle = true;
        [SerializeField] protected Transform VoxelTemplateName;
        [SerializeField] protected Transform SpellTemplateName;
        [SerializeField] protected Transform UnitTemplateName;
        [SerializeField] protected Canvas UGUICanvas;
        [SerializeField] protected Transform SelectCursor;
        [SerializeField] protected AudioSource BGMPlayer;
        [SerializeField] protected Camera MainCamera;
        //---------------------------------------------------------------------------------------------
        //private EditorTemplates templates;
        protected UnityZone battle;
        protected UnityInterval interval;
        //private HPBar selectedHPBar;
        //---------------------------------------------------------------------------------------------
        protected virtual void Awake()
        {
            //         UnityDriver.SetDirver();
            //         ReflectionUtil.LoadDlls();
            this.interval = new UnityInterval();
            BattleBootstrap.OnFinish(DoStartBattle);
        }
        protected virtual UniTask DoStartBattle()
        {
            //         await YooAssetManager.InitYooAsset(null);
            //         await YooAssetManager.InitPackage();
            //         var args = Environment.GetCommandLineArgs();
            //         var prop = Properties.ParseArgs(args);
            //         if (prop.TryGetValue("-editorRoot", out var _root) && Directory.Exists(_root))
            //         {
            //             EditorRootPath = Path.GetFullPath(_root);
            //         }
            //         else if (new DirectoryInfo(Application.dataPath).TryFindParentDirectory(Path.Combine("Data", "GameEditor"), out var _editorRoot))
            //         {
            //             EditorRootPath = _editorRoot.FullName;
            //         }
            //         else
            //         {
            //             EditorRootPath = $"file://{Application.dataPath}/../../../Data/GameEditor";
            //         }
            var prop = UnityBattleFactory.CommandLineArgs;
            if (prop.TryGetAsInt("-ActorTemplateID", out var _actorTemplateID))
            {
                ActorTemplateID = _actorTemplateID;
            }
            if (prop.TryGetAsInt("-SceneID", out var _sceneID))
            {
                SceneID = _sceneID;
            }
            //         if (prop.TryGetValue("-Host", out var _host))
            //         {
            //             BattleHost = _host;
            //         }
            //        Debug.Log("EditorRootPath : " + EditorRootPath);
            //       ZoneDataFactory.GameEditorRoot = $"{EditorRootPath}";
            // 
            //         ZoneDataFactory.Codec = new GameBattleCodec();
            //
            //ABSystem.RootPath = $"{EditorRootPath}";
            //         DeepCore.Voxel.Data.VoxelWorldManager.Instance.ToString();
            //         TemplateDataCenter.ENABLE_LOAD_FROM_BIN = true;
            //         EditorTemplates.DEFAULT_LOAD_FROM_BIN = true;
            //         new NewtonJsonTemplateLoader(true);
            //         new RogueZoneDataFactory();
            //         new RogueZoneHostFactory();
            //         new RogueZoneSlaveFactory();
            //         new RogueUnityBattleFactory(EditorRootPath);
            //         templates = ZoneDataFactory.Factory.CreateEditorTemplates(EditorRootPath + "/data");
            //         templates.LoadAllTemplates();
            //new UnityLiveFactory(Templates, this.gameObject);
            var mapId = SceneID;
            var sceneData = Templates.LoadScene(mapId);
            if (!prop.TryGetAsInt("-ActorForce", out var _force))
            {
                if (sceneData.TryGetStartTestRegion(out var region, out var start))
                {
                    _force = start.START_Force;
                }
            }
            AudioComponent.Instance.SoundSource = this.BGMPlayer;
            this.battle = UnityBattleFactory.Instance.CreateBattle();
            var runtime = CreateBattleRuntime(sceneData);
            var config = new UnityBattleConfig()
            {
                EffectLayerName = null,
                RayCastObjectLayerName = null,
                RayCastTerrainLayerName = null,

                Root = gameObject.transform,
                VoxelTemplateName = this.VoxelTemplateName,
                UnitTemplateName = this.UnitTemplateName,
                SpellTemplateName = this.SpellTemplateName,
                GameCamera = MainCamera,
            };
            this.battle.Init(config, runtime);
            this.battle.battle.Layer.LayerInit += Layer_LayerInit;
            this.battle.battle.Layer.ActorAdded += Layer_ActorAdded;
            this.battle.battle.Layer.MessageReceived += Layer_MessageReceived;
            //         if (gameObject.transform.parent.gameObject.TryGetComponentInChildren<BattleHUD>(out var hud))
            //         {
            //             hud.SetBattle(battle);
            //         }
            Debug.Log("Battle Init : " + this.battle);
            interval.ResetTime();
            if (gameObject.TryGetComponent<ConsoleLogOutput>(out var console))
            {

            }
            {
                //UnityZoneOnGUIRuntime.Init(Templates.Templates);
                var ongui = gameObject.AddComponent<UnityZoneOnGUIRuntime>();
                runtime.Layer.GUIRuntime = ongui;
            }
//             if (runtime is BattleClient netRuntime)
//             {
//                 netRuntime.Start();
//             }
            return UniTask.CompletedTask;
        }

        protected virtual AbstractBattle CreateBattleRuntime(SceneData sceneData)
        {            
            //             if (IPUtil.TryParseHostPort(BattleHost, out var host, out var port))
            //             {
            //                 runtime = BattleClient.CreateBattleClient(
            //                     netAddress: BattleHost,
            //                     playerUUID: $"{Guid.NewGuid()}",
            //                     roomID: $"{mapId}",
            //                     unitTemplateID: ActorTemplateID,
            //                     force: (byte)_force,
            //                     data_root: Templates,
            //                     slaveFactory: SlaveFactory);
            //             }
            //             else 
            if (IsThreadBattle)
            {
                return new ThreadBattleSinglePlay(
                    Templates,
                    HostFactory,
                    SlaveFactory,
                    sceneData,
                    ActorForce,
                    ActorTemplateID);
            }
            else
            {
                return new LocalBattleSinglePlay(
                    Templates,
                    HostFactory,
                    SlaveFactory,
                    sceneData,
                    ActorForce,
                    ActorTemplateID);
            }
        }
        //---------------------------------------------------------------------------------------------

        // Update is called once per frame
        protected virtual void Update()
        {
            if (battle != null)
            {
                var ms = interval.UpdateTime();
                battle.battle.BeginUpdate(ms);
                battle.battle.Update();
                battle.Update(ms);
                if (SelectCursor)
                {
                    if (battle.SelectedObject is UnityZoneUnit selected)
                    {
                        SelectCursor.SetActive(true);
                        SelectCursor.transform.position = selected.gameObject.transform.position;
                        SelectCursor.localScale = Vector3.one * selected.layerUnit.BodyBlockSize;
                    }
                    else
                    {
                        SelectCursor.SetActive(false);
                    }
                }
            }
        }
        protected virtual void OnDrawGizmos()
        {
            if (battle != null)
            {
                //battle.OnDrawGizmos();
                //             Gizmos.color = Color.yellow;
                //             battle.ForEachZoneObjects(z =>
                //             {
                //                 if (z is UnityZoneUnit unit)
                //                 {
                //                     var upos = unit.ZonePosition.ToUnityWorldPosition(unit, unit.layerUnit.RemotePos);
                //                     upos = unit.transform.parent.localToWorldMatrix.MultiplyPoint(upos);
                //                     Gizmos.DrawSphere(upos, 1f);
                //                 }
                //             });
            }
        }

        protected virtual void OnDestroy()
        {
            try
            {
                battle?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        protected virtual void OnGUI()
        {

        }
        protected virtual void Layer_LayerInit(DeepCore.Game3D.Slave.Layer.LayerZone layer)
        {
        }
        protected virtual void Layer_MessageReceived(DeepCore.Game3D.Slave.Layer.LayerZone layer, IBattleMessage msg)
        {
        }
        protected virtual void Layer_ActorAdded(DeepCore.Game3D.Slave.Layer.LayerZone layer, DeepCore.Game3D.Slave.Layer.LayerPlayer actor)
        {
            Debug.Log("Layer_ActorAdded : ");

        }

    }
}



