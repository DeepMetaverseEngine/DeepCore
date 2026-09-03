using Code.System.Resource;
using Code.System.Tick;
using Code.Utility;
using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.IO;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Code.System;
using IOGame.Core.Battle.Data;
using UnityEngine;
using ZoneEvent = DeepCore.GameData.Zone.ZoneEvent;

namespace Code.BattleView
{
    public partial class UnityBattle : IDisposable
    {
        public GameObject GameObject { get; private set; }

        public GameObject ObjectsNode { get; private set; }
        public GameObject FlagsNode { get; private set; }
        public GameObject TerrainNode { get; private set; }
        public GameObject EffectsNode { get; private set; }
        public GameObject SceneNode { get; private set; }
        public GameObject AudioNode { get; private set; }
        public GameObject DragNode { get; private set; }
        public GameObject UndragNode { get; private set; }

        public AbstractBattle Battle { get; private set; }
        public WrapGO ModelWrap { get; private set; }
        public UnityBattleActor Actor { get; private set; }

        public string VoxelTemplateName { get; set; } = "VoxelTemplate";
        public string RayCastLayerName { get; set; } = "RayCast";

        private readonly HashMap<uint, UnityBattleObject> _battleObjects = new HashMap<uint, UnityBattleObject>();
        private readonly HashMap<string, UnityBattleFlag> _battleFlags = new HashMap<string, UnityBattleFlag>();
        private float _terrainH = 0;

        public UnityBattle Init(GameObject go, AbstractBattle battle)
        {
            this.GameObject = go;

            this.ObjectsNode = new GameObject("objects");
            this.FlagsNode = new GameObject("flags");
            this.TerrainNode = new GameObject("terrain");
            this.EffectsNode = new GameObject("effects");
            this.SceneNode = new GameObject("scene");
            this.AudioNode = new GameObject("audio");
            this.AudioNode.AddComponent<AudioManager>();

            this.ObjectsNode.transform.SetParent(go.transform, false);
            this.FlagsNode.transform.SetParent(go.transform, false);
            this.TerrainNode.transform.SetParent(go.transform, false);
            this.EffectsNode.transform.SetParent(go.transform, false);
            this.SceneNode.transform.SetParent(go.transform, false);
            this.AudioNode.transform.SetParent(go.transform, false);

            this.Battle = battle;
            this.Battle.Layer.LayerInit += Layer_LayerInit;
            this.Battle.Layer.ObjectEnter += Layer_ObjectEnter;
            this.Battle.Layer.ObjectLeave += Layer_ObjectLeave;
            this.Battle.Layer.MessageReceived += Layer_MessageReceived;
            this.Battle.Layer.GameOver += Layer_GameOver;
            this.RegistAllZoneEvent();

            return this;
        }

        public void Dispose()
        {
            Battle.Layer.GameOver -= Layer_GameOver;
            Battle.Layer.LayerInit -= Layer_LayerInit;
            Battle.Layer.ObjectEnter -= Layer_ObjectEnter;
            Battle.Layer.ObjectLeave -= Layer_ObjectLeave;
            Battle.Layer.MessageReceived -= Layer_MessageReceived;

            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }
            if (_effectSerials.Count > 0)
            {
                foreach (var serial in _effectSerials)
                {
                    TickSystem.TickCancel(serial);
                }
                _effectSerials.Clear();
            }
            foreach (var bv in _battleObjects.Values)
            {
                bv.Dispose();
            }
            _battleObjects.Clear();
            Battle.Dispose();
            UnityEngine.Object.Destroy(AudioNode);
            UnityEngine.Object.Destroy(GameObject);
        }

        public void Update(int deltaTimeMS)
        {
            Battle?.BeginUpdate(deltaTimeMS);
            Battle?.Update();
            foreach (var bv in _battleObjects.Values)
            {
                bv.Update(deltaTimeMS);
            }
        }

        public UnityBattleObject GetObject(uint objID)
        {
            return _battleObjects.Get(objID);
        }

        protected virtual void Layer_LayerInit(LayerZone layer)
        {
            _terrainH = layer.Terrain3D.TotalHeight;
            // init scene
            {
                var url = layer.Data.FileName;
                if (!string.IsNullOrEmpty(url))
                {
                    var name = Resource.GetFileNameWithoutExtension(url);
                    ModelWrap = ResourceSystem.GetWrapGO(url, name, null, SceneNode.transform);
                }
            }
            // init terrain
            if (layer.Terrain3D is VoxelClientTerrain3D voxel)
            {
                new UnityBattleVoxelTerrain(this, TerrainNode, voxel);
            }
            // init flags
            layer.ForEachFlags<LayerFlag>(flag =>
            {
                var viewflag = CreateBattleFlag(flag, this.FlagsNode);
                if (viewflag != null)
                {
                    _battleFlags.Add(flag.Name, viewflag);
                }
            });
        }

        private void InitProperties(IOSceneProperties prop, byte force)
        {
            if (prop == null)
                return;


        }



        private void SetRegionTransform(GameObject node, RegionData region)
        {
            node.transform.localScale = new Vector3(region.W, 0.01f, region.H);
            node.transform.localPosition = BattleToUnityPosition(new DeepCore.Vector.Vector3(region.X, region.Y, region.Z));
        }

        protected virtual void Layer_ObjectEnter(LayerZone layer, LayerZoneObject obj)
        {
            if (obj is LayerPlayer player)
            {
                Actor = CreateBattleActor(player, this.ObjectsNode);
                _battleObjects.Add(obj.ObjectID, Actor);
                OnActorCreate?.Invoke(Actor);
            }
            else if (obj is LayerUnit unit)
            {
                _battleObjects.Add(obj.ObjectID, CreateBattleUnit(unit, this.ObjectsNode));
            }
            else if (obj is LayerSpell spell)
            {
                _battleObjects.Add(obj.ObjectID, CreateBattleSpell(spell, this.ObjectsNode));
            }
            else if (obj is LayerItem item)
            {
                _battleObjects.Add(obj.ObjectID, CreateBattleItem(item, this.ObjectsNode));
            }
            else
            {
                Debug.LogError("~");
            }
        }

        protected virtual void Layer_ObjectLeave(LayerZone layer, LayerZoneObject obj)
        {
            if (_battleObjects.TryRemove(obj.ObjectID, out var body))
            {
                body.Dispose();
            }
        }

        protected virtual void Layer_MessageReceived(LayerZone layer, IMessage msg)
        {
            if (msg is ZoneEvent)
            {
                Action<ZoneEvent> action = null;
                if (_zoneEvens.TryGetValue(msg.GetType(), out action))
                {
                    action(msg as ZoneEvent);
                }
            }

        }

        protected virtual UnityBattleActor CreateBattleActor(LayerUnit obj, GameObject parent)
        {
            var actor = System.Pool.ObjectPool<UnityBattleActor>.Get();
            actor.Init(this, obj, parent);
            return actor;
        }

        protected virtual UnityBattleObject CreateBattleUnit(LayerUnit obj, GameObject parent)
        {
            var unit = System.Pool.ObjectPool<UnityBattleUnit>.Get();
            unit.Init(this, obj, parent);
            return unit;
        }

        protected virtual UnityBattleObject CreateBattleSpell(LayerSpell obj, GameObject parent)
        {
            var spell = System.Pool.ObjectPool<UnityBattleSpell>.Get();
            spell.Init(this, obj, parent);
            return spell;
        }

        protected virtual UnityBattleObject CreateBattleItem(LayerItem obj, GameObject parent)
        {
            var item = System.Pool.ObjectPool<UnityBattleItem>.Get();
            item.Init(this, obj, parent);
            return item;
        }

        protected virtual UnityBattleFlag CreateBattleFlag(LayerFlag obj, GameObject parent)
        {
            if (obj is LayerEditorDecoration deco && !string.IsNullOrEmpty(deco.Data.ResourceID))
            {
                var item = new UnityBattleDecoration();
                item.Init(this, obj, parent);
                return item;
            }
            if (obj is LayerEditorRegion region && !string.IsNullOrEmpty(region.Data.ResourceID))
            {
                var item = new UnityBattleRegion();
                item.Init(this, obj, parent);
                return item;
            }
            if (obj is LayerEditorPoint point && !string.IsNullOrEmpty(point.Data.ResourceID))
            {
                var item = new UnityBattlePoint();
                item.Init(this, obj, parent);
                return item;
            }
            return null;
        }


        public bool RayCastVoxelLayer(out DeepCore.Geometry.Vector3 hit)
        {
            var hitLayer = LayerMask.NameToLayer(RayCastLayerName);
            //从摄像机发出到点击坐标的射线
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo, 1000, hitLayer)) // Voxel Mask 6
            {
                var target = hitInfo.point;
                //划出射线，只有在scene视图中才能看到
                //Debug.DrawLine(ray.origin, target);
                //hit = new DeepCore.Geometry.Vector3(target.x, _terrainH - target.z, target.y);
                hit = UnityToBattlePosition(target);
                Debug.Log("### Ray Cast Voxel " + hit);
                /*
                var launch = new UnitLaunchSkillRequest(Actor.ZonePlayer.ObjectID, 1);
                launch.IsAutoFocusNearTarget = _battle.Actor.ZonePlayer.IsSkillAutoFocusTarget;
                launch.SpellTargetPos = new Vector3(hitInfo.point.x, hitInfo.point.z, hitInfo.point.y);
                launch.SummonID = id;
                _battle.Actor.ZonePlayer.SendUnitLaunchSkill(launch);
                */
                return true;
            }
            hit = DeepCore.Geometry.Vector3.NaN;
            Debug.Log("### Ray Cast Voxel NaN ");
            return false;
        }
        public DeepCore.Geometry.Vector3 UnityToBattlePosition(UnityEngine.Vector3 Pos)
        {
            return new DeepCore.Geometry.Vector3(Pos.x, _terrainH - Pos.z, Pos.y);
            //Debug.Log("### Ray Cast Voxel 2" + hit);
            //return true;
        }
        public Vector3 BattleToUnityPosition(in DeepCore.Vector.Vector3 p)
        {
            return new Vector3(p.X, p.Z, _terrainH - p.Y);
        }
        public Quaternion BattleToUnityRotation(in float direction)
        {
            //x为正方向
            var radians = direction;
            return Quaternion.AngleAxis(radians * Mathf.Rad2Deg + 90, Vector3.up);
        }


    }
}