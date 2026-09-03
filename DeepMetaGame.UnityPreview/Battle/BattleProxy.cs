using DeepCore;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.OnGUI;
using DeepMetaGame.Unity.Preview;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Battle
{
    public class BattleProxy : UnityIPC
    {
        public static BattleProxy Proxy { get; private set; }

        public int SceneID = 999005;
        public int ActorTemplateID = 999999;
        public bool IsThreadBattle = true;
        [SerializeField] private Transform VoxelTemplateName;
        [SerializeField] private Transform SpellTemplateName;
        [SerializeField] private Transform UnitTemplateName;
        [SerializeField] private Transform SelectCursor;
        [SerializeField] public AudioSource BGMPlayer;

        public UnityZone Battle => battle;

        private UnityZone battle;
        //private UnityInterval interval;
        //private HPBar selectedHPBar;
        //---------------------------------------------------------------------------------------------

        protected override void Awake()
        {
            Proxy = this;
            base.Awake();
            //interval = new UnityInterval();
            //GUIFactory = new OnGUIFactory(EditorRootDir);
        }
        protected override void Start()
        {
            base.Start();


            var args = Environment.GetCommandLineArgs();
            var prop = Properties.ParseArgs(args);

            if (prop.TryGetAsInt("-actorID", out var _actorTemplateID))
            {
                ActorTemplateID = _actorTemplateID;
            }
            if (prop.TryGetAsInt("-mapID", out var _sceneID))
            {
                SceneID = _sceneID;
            }
            if (prop.TryGetAsBool("-thread", out var _thread))
            {
                IsThreadBattle = _thread;
            }
            var mapId = SceneID;
            var sceneData = Templates.LoadScene(mapId);
            if (!prop.TryGetAsInt("-actorForce", out var _force))
            {
                if (sceneData.TryGetStartTestRegion(out var region, out var start))
                {
                    _force = start.START_Force;
                }
            }
            this.battle = UnityBattleFactory.Instance.CreateBattle();
            AbstractBattle runtime = null;
            if (IsThreadBattle)
            {
                runtime = new ThreadBattleSinglePlay(Templates, UnityIPC.HostFactory, UnityIPC.SlaveFactory, sceneData, _force, ActorTemplateID);
            }
            else
            {
                runtime = new LocalBattleSinglePlay(Templates, UnityIPC.HostFactory, UnityIPC.SlaveFactory, sceneData, _force, ActorTemplateID);
            }
            var config = new UnityBattleConfig()
            {
                Root = this.transform,
                VoxelTemplateName = this.VoxelTemplateName,
                UnitTemplateName = this.UnitTemplateName,
                SpellTemplateName = this.SpellTemplateName,
                // = this.BGMPlayer,
                GameCamera = Camera.main,
                EffectLayerName = null,
                RayCastObjectLayerName = null,
                RayCastTerrainLayerName = null,
                UIRoot = null,
                RayCastMaxDistance = 0,
            };
            this.battle.Init(config, runtime);
            Debug.Log("Battle Init : " + this.battle);
            //interval.ResetTime();
            {
                var ongui = gameObject.AddComponent<UnityZoneOnGUIRuntime>();
                runtime.Layer.GUIRuntime = ongui;
            }
            battle.OnAddZoneObject += Battle_OnAddZoneObject;
        }

        private void Battle_OnAddZoneObject(UnityLayerObject go)
        {
            if (go is UnityZoneUnit zoneUnit)
            {
                RTG.AddEditorObject(zoneUnit.gameObject);
                if (battle.Actor == zoneUnit) {
                    RTG.LookAt(battle.Actor.transform);
                } 
            }
        }

        //         prote void Layer_LayerInit(DeepCore.Game3D.Slave.Layer.LayerZone layer)
        //         {
        //             var camera = Camera.main;
        //             if (camera != null)
        //             {
        //                 var free = camera.GetOrAddComponent<WowFreeCamera>();
        //                 var actor = camera.GetOrAddComponent<WowActorCamera>();
        //                 actor.enabled = false;
        //             }
        //         }

        protected override void OnUpdate(float deltaSEC)
        {
            var expectIntervalMS = 1000f / Templates.Templates.DefaultConfig.SYSTEM_FPS;
            if (battle != null)
            { 
                var totalMS = Time.deltaTime * 1000f; //interval.UpdateTime();
                while (totalMS > 0)
                {
                    var tick = Math.Min(totalMS, expectIntervalMS);
                    battle.battle.BeginUpdate(tick);
                    battle.battle.Update();
                    try
                    {
                        battle.Update(tick);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("update error " + e);
                    }
                    totalMS -= tick;
                }
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
        protected override void OnDestroy()
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
        private void OnDrawGizmos()
        {
            if (battle != null)
            {
                //                 Gizmos.color = Color.yellow;
                //                 battle.ForEachZoneObjects(z =>
                //                 {
                //                     if (z is UnityZoneUnit unit)
                //                     {
                //                         var upos = unit.ToUnityPosition(unit.layerUnit.RemotePos);
                //                         upos = unit.transform.parent.localToWorldMatrix.MultiplyPoint(upos);
                //                         Gizmos.DrawSphere(upos, 1f);
                //                     }
                //                 });
            }
        }

    }
}
