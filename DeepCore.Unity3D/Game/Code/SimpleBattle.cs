using Code.BattleView;
using Code.System.AB;
using DeepCore;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Slave;
using DeepCore.GameData;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using System;
using System.IO;
using UnityEngine;

namespace Code
{
    public class SimpleBattle : MonoBehaviour
    {
        public static SimpleBattle Instance { get; private set; }
        public EditorTemplates Templates
        {
            get => _templates;
        }
        // Start is called before the first frame update
        //---------------------------------------------------------------------------------------------
        public int SceneID = 10000;

        public string EditorRootPath;
        //---------------------------------------------------------------------------------------------
        private EditorTemplates _templates;
        private UnityBattle _battle;

        void Start()
        {
            EditorRootPath = $"file://{Application.dataPath}/../../../data";

            var args = Environment.GetCommandLineArgs();
            var prop = Properties.ParseArgs(args);
            if (prop.Count > 0)
            {
                if (prop.TryGetValue("-editorRoot", out var root) && Directory.Exists(root))
                {
                    EditorRootPath = Path.GetFullPath(root);
                }
            }

            ZoneDataFactory.Codec = new IOGame.Client.GenCodec.IOBattleCodec();
            new IOGame.Core.Battle.Data.IOBattleDataFactory();
            new IOGame.Core.Battle.Host.IOBattleHostFactory();
            new IOGame.Core.Battle.Slave.IOBattleSlaveFactory();
            ABSystemImpl.RootPath = $"{EditorRootPath}/res";
            ZoneHostFactory.GameEditorRoot = ZoneSlaveFactory.GameEditorRoot = $"{EditorRootPath}";
            _templates = TemplateManager.DataFactory.CreateEditorTemplates(EditorRootPath + "/data");
            _templates.LoadAllTemplates();
            
            var mapId = this.SceneID;

            var sceneData = Templates.LoadScene(mapId);
            // var battle = new CardsBattleLocalPlay(Templates, sceneData);
            _battle = new UnityBattle();
            // _battle.Init(this.gameObject, battle);
            _battle.Battle.Layer.LayerInit += Layer_LayerInit;

        }

        private void Layer_LayerInit(DeepCore.Game3D.Slave.Layer.LayerZone layer)
        {
            // init camera
            if (Camera.main)
            {
                var camera = Camera.main;
                if (camera.TryGetComponent<SimpleCameraController>(out var freeCamera))
                {
                    freeCamera.enabled = true;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            var ms = (int)(Time.deltaTime * 1000);
            _battle?.Update(ms);
            if (Input.GetMouseButtonDown(0))
            {
                _battle.RayCastVoxelLayer(out var hit);
            }
        }

        public void SummonUnit(int id, Action<bool> callback)
        {
            if (_battle != null && _battle.Actor != null)
            {
                if (_battle.RayCastVoxelLayer(out var hit))
                {
                    var launch = new UnitLaunchSkillRequest(_battle.Actor.ZonePlayer.ObjectID, 1);
                    launch.IsAutoFocusNearTarget = _battle.Actor.ZonePlayer.IsSkillAutoFocusTarget;
                    launch.SpellTargetPos = hit;
                    launch.SummonID = id;
                    _battle.Actor.ZonePlayer.SendUnitLaunchSkill(launch, (req, rsp) =>
                    {
                        callback?.Invoke(rsp != null && rsp.IsLaunched);
                    });
                    return;
                }
                /*
                //从摄像机发出到点击坐标的射线
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, 1000, 1 << 6))
                {
                    //划出射线，只有在scene视图中才能看到
                    Debug.DrawLine(ray.origin, hitInfo.point);

                    var launch = new UnitLaunchSkillRequest(_battle.Actor.ZonePlayer.ObjectID, 1);
                    launch.IsAutoFocusNearTarget = _battle.Actor.ZonePlayer.IsSkillAutoFocusTarget;
                    launch.SpellTargetPos = new Vector3(hitInfo.point.x, hitInfo.point.z, hitInfo.point.y);
                    launch.SummonID = id;
                    _battle.Actor.ZonePlayer.SendUnitLaunchSkill(launch);
                }*/
            }
            callback?.Invoke(false);
        }

        public int GetZoneLifetime()
        {
            var layer = _battle?.Battle?.Layer;
            if (layer == null || layer.IsLoaded == false)
                return 0;

            return (int)(layer.Data.TotalTimeLimitSEC - layer.ServerTimeMS / 1000);
        }
        private void OnDestroy()
        {
            _battle?.Dispose();
        }
    }
}
