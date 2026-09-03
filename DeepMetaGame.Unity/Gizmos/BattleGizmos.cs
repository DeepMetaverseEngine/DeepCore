using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity3D;
using DeepCore.Unity3D.Voxel;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.Preview.Preview;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.XR;

namespace DeepMetaGame.Unity
{
    public static class BattleGizmos
    {
        //------------------------------------------------------------------------------------------------------
        public static GameObject CreateGizmos(UnitInfo unitInfo, float bodySize, float bodyHeight)
        {
            switch (unitInfo.FillZoneShape)
            {
                case UnitInfo.Shape.RECTANGLE:
                    {
                        var rect = VoxelGizmos.CreateVoxelRect(bodySize, bodySize, bodyHeight);
                        rect.name = $"Gizmos-{unitInfo}";
                        return rect;
                    }
                case UnitInfo.Shape.ROUND:
                default:
                    {
                        var rect = VoxelGizmos.CreateVoxelCylinder(bodySize, bodyHeight);
                        rect.name = $"Gizmos-{unitInfo}";
                        return rect;
                    }
            }
        }
        //         public static void UpdateGizmos(GameObject childGizmos, UnitInfo data, float currentBodyScale)
        //         {
        //         }
        public static GameObject CreateGizmos(ItemTemplate data)
        {
            var rect = VoxelGizmos.CreateVoxelCylinder(data.BodySize, data.BodyHeight);
            rect.name = $"Gizmos-{data}";
            return rect;
        }
        public static GameObject CreateGizmos(SpellTemplate spell, Transform launcher, Transform sender, Transform target)
        {
            var height = spell.BodyHeight;
            var pos = spell.AdjustVoxelAnchor(DeepCore.Geometry.Vector3.Zero, ref height);
            var shapeObject = AttackShapeGizmos.CreateAttackShape(
                  spell.AsBodyShape,
                  spell.BodySize,
                  height,
                  spell.Distance,
                  spell.FanAngle,
                  spell.RectWide,
                  target,
                  sender,
                  launcher);
            if (shapeObject != null)
            {
                shapeObject.name = $"Gizmos-{spell}";
                shapeObject.transform.localPosition = new UnityEngine.Vector3(pos.X, pos.Z, pos.Y);
                shapeObject.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
                return shapeObject;
            }
            return shapeObject;
        }
        public static GameObject CreateGizmos(RegionData data)
        {
            if (data.RegionType == RegionData.Shape.RECTANGLE)
            {
                var rect = VoxelGizmos.CreateVoxelRect(data.W, data.H, data.Height);
                rect.name = $"Gizmos-{data}";
                return rect;
            }
            else if (data.RegionType == RegionData.Shape.STRIP)
            {
                var strip = VoxelGizmos.CreateVoxelRectStrip(data.W, data.H, data.Height);
                strip.name = $"Gizmos-{data}";
                return strip;
            }
            else
            {
                var rect = VoxelGizmos.CreateVoxelCylinder(data.Radius, data.Height);
                rect.name = $"Gizmos-{data}";
                return rect;
            }
        }
        public static GameObject CreateGizmos(DecorationData data)
        {
            if (data.RegionType == DecorationData.Shape.RECTANGLE)
            {
                var rect = VoxelGizmos.CreateVoxelRect(data.W, data.H, data.Height);
                rect.name = $"Gizmos-{data}";
                return rect;
            }
            else if (data.RegionType == DecorationData.Shape.STRIP)
            {
                var strip = VoxelGizmos.CreateVoxelRectStrip(data.W, data.H, data.Height);
                strip.name = $"Gizmos-{data}";
                return strip;
            }
            else
            {
                var cylinder = VoxelGizmos.CreateVoxelCylinder(data.Radius, data.Height);
                cylinder.name = $"Gizmos-{data}";
                return cylinder;
            }
        }
        public static GameObject CreateGizmos(PointData Data)
        {
            var cylinder = VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            cylinder.name = $"Gizmos-{Data}";
            return cylinder;
        }
        public static GameObject CreateGizmos(AreaData Data)
        {
            var rect = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Data.Height);
            rect.name = $"Gizmos-{Data}";
            return rect;
        }
    }
    //------------------------------------------------------------------------------------------------------
    public abstract class ObjectGizmos : MonoBehaviour
    {
        public UnityLayerObject BindingObject { get; private set; }
        private GameObject gizmos;
        protected virtual bool ENABLE => UnityBattleConfig.ENABLE_BATTLE_GIZMOS;
        public GameObject Init(UnityLayerObject obj)
        {
            BindingObject = obj;
            this.gizmos = OnInit(obj);
            if (this.gizmos != null)
            {
                this.gizmos.transform.SetParent(obj.transform, false);
                this.gizmos.gameObject.SetActive(ENABLE);
            }
            return gizmos;
        }
        protected virtual void LateUpdate()
        {
            if (gizmos)
            {
                OnUpdate(BindingObject, gizmos);
                if (gizmos.gameObject.activeSelf != ENABLE)
                {
                    gizmos.gameObject.SetActive(ENABLE);
                }
            }
        }
        protected abstract GameObject OnInit(UnityLayerObject obj);
        protected abstract void OnUpdate(UnityLayerObject obj, GameObject gizmos);
    }
    public abstract class ObjectGizmos<T> : ObjectGizmos where T : UnityLayerObject
    {
        new public T BindingObject => base.BindingObject as T;
        sealed protected override GameObject OnInit(UnityLayerObject obj)
        {
            return this.OnInit(obj as T);
        }
        sealed protected override void OnUpdate(UnityLayerObject obj, GameObject gizmos)
        {
            this.OnUpdate(obj as T, gizmos);
        }
        protected abstract GameObject OnInit(T obj);
        protected abstract void OnUpdate(T obj, GameObject gizmos);
    }
    //------------------------------------------------------------------------------------------------------
    public class UnitGizmos : ObjectGizmos<UnityZoneUnit>
    {
        protected override GameObject OnInit(UnityZoneUnit unit)
        {
            var voxel = VoxelComponent.Instance;
            var zone = unit.zone;
            var ret = BattleGizmos.CreateGizmos(unit.info, unit.info.BodySize, unit.info.BodyHeight);
            {
                var meshrender = ret.GetComponent<MeshRenderer>();
                if (meshrender != null && zone.config.UnitTemplateName != null)
                {
                    var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        Colors.DecodeARGB(unit.info.ColorARGB, out float r, out float g, out float b, out float a);
                        meshrender.material = GameObject.Instantiate(cell_meshr.material);
                        meshrender.material.SetColor("_Color", a == 0 ? voxel.DEFAULT_UNIT_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f));
                    }
                }
            }
            {
                var hit = BattleGizmos.CreateGizmos(unit.info, unit.info.BodySize + unit.info.BodySizeHitAppend, unit.info.BodyHeight / 2f);
                var meshrender = hit.GetComponent<MeshRenderer>();
                if (meshrender != null && zone.config.UnitTemplateName != null)
                {
                    var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        meshrender.material = GameObject.Instantiate(cell_meshr.material);
                        meshrender.material.SetColor("_Color", voxel.DEFAULT_UNIT_HIT_GIZMOS_COLOR.SetAlpha(0.5f));
                    }
                }
                hit.transform.SetParent(ret.transform, false);
            }
            return ret;
        }
        protected override void OnUpdate(UnityZoneUnit obj, GameObject gizmos)
        {
            //BattleGizmos.UpdateGizmos(gizmos, obj.info, 1f);
            //gizmos.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
    public class ItemGizmos : ObjectGizmos<UnityZoneItem>
    {
        protected override GameObject OnInit(UnityZoneItem item)
        {
            var voxel = VoxelComponent.Instance;
            var zone = item.zone;
            var ret = BattleGizmos.CreateGizmos(item.info);
            var meshrender = ret.GetComponent<MeshRenderer>();
            if (meshrender != null && zone.config.UnitTemplateName != null)
            {
                var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                if (cell_meshr)
                {
                    Colors.DecodeARGB(item.info.ColorARGB, out float r, out float g, out float b, out float a);
                    meshrender.material = GameObject.Instantiate(cell_meshr.material);
                    meshrender.material.SetColor("_Color", a == 0 ? voxel.DEFAULT_ITEM_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f));
                }
            }
            return ret;
        }
        protected override void OnUpdate(UnityZoneItem obj, GameObject gizmos)
        {

        }
    }
    public class SpellGizmos : ObjectGizmos<UnityZoneSpell>
    {
        protected override GameObject OnInit(UnityZoneSpell spell)
        {
            var voxel = VoxelComponent.Instance;
            var zone = spell.zone;
            var ret = BattleGizmos.CreateGizmos(spell.info,
                   spell.parent.GetObject(spell.layerSpell.Launcher?.ObjectID)?.transform,
                   spell.parent.GetObject(spell.layerSpell.Sender?.ObjectID)?.transform,
                   spell.parent.GetObject(spell.layerSpell.Target?.ObjectID)?.transform);
            if (zone.config.SpellTemplateName != null)
            {
                var cell_meshr = zone.config.SpellTemplateName.GetComponentInChildren<MeshRenderer>();
                if (cell_meshr)
                {
                    Colors.DecodeARGB(spell.info.ColorARGB, out float r, out float g, out float b, out float a);
                    var material = GameObject.Instantiate(cell_meshr.material);
                    var color = a == 0 ? voxel.DEFAULT_SPELL_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f);
                    material.SetColor("_Color", color);
                    AttackShapeGizmos.SetMaterial(ret, material, color);
                }
            }
            return ret;
        }
        protected override void OnUpdate(UnityZoneSpell spell, GameObject gizmos)
        {
            //BattleGizmos.UpdateGizmos(gizmos, obj.info, obj.layerSpell.Distance, obj.layerSpell.BodySize);
            var childGizmos = gizmos;
            if (childGizmos != null)
            {
                var aoeFactor = 1f;
                switch (spell.info.BodyShape)
                {
                    case SpellTemplate.Shape.LineToTargetPos:
                        if (spell.layerSpell.TargetPos.HasValue)
                        {
                            if (gizmos.TryGetComponent<LineToTarget>(out var line))
                            {
                                line.targetPos = spell.zone.BattleToUnityWorldPosition(spell.layerSpell.TargetPos.Value);
                            }
                        }
                        break;
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        break;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        aoeFactor = spell.layerSpell.Distance / spell.info.Distance;
                        childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, 1f);
                        break;
                    default:
                        aoeFactor = spell.layerSpell.BodySize / spell.info.BodySize;
                        childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, aoeFactor);
                        break;
                }
            }
        }
    }
    //------------------------------------------------------------------------------------------------------
    public abstract class FlagGizmos<T> : ObjectGizmos<T> where T : UnityZoneFlag
    {
        protected override bool ENABLE => UnityBattleConfig.ENABLE_BATTLE_GIZMOS_FLAGS;
    }
    public class RegionGizmos : FlagGizmos<UnityLayerRegion>
    {
        protected override GameObject OnInit(UnityLayerRegion flag)
        {
            var voxel = VoxelComponent.Instance;
            var zone = flag.zone;
            var Data = flag.Data;
            var ret = default(GameObject);
            if (Data.RegionType == RegionData.Shape.RECTANGLE)
            {
                ret = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Data.Height);
            }
            else if (Data.RegionType == RegionData.Shape.STRIP)
            {
                ret = VoxelGizmos.CreateVoxelRectStrip(Data.W, Data.H, Data.Height);
            }
            else
            {
                ret = VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            }
            //             var ret = (Data.RegionType == RegionData.Shape.RECTANGLE) ?
            //                 VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Data.Height) :
            //                 VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            {
                var meshrender = ret.GetComponent<MeshRenderer>();
                if (meshrender != null && zone.config.UnitTemplateName != null)
                {
                    var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        Colors.DecodeARGB(Data.Color, out float r, out float g, out float b, out float a);
                        meshrender.material = GameObject.Instantiate(cell_meshr.material);
                        meshrender.material.SetColor("_Color", a == 0 ? voxel.DEFAULT_FLAG_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f));
                    }
                }
            }
            return ret;
        }
        protected override void OnUpdate(UnityLayerRegion obj, GameObject gizmos)
        {

        }
    }
    public class DecorationGizmos : FlagGizmos<UnityLayerDecoration>
    {
        protected override GameObject OnInit(UnityLayerDecoration flag)
        {
            var voxel = VoxelComponent.Instance;
            var zone = flag.zone;
            var Data = flag.Data;
            var ret = default(GameObject);
            if (Data.RegionType == DecorationData.Shape.RECTANGLE)
            {
                ret = VoxelGizmos.CreateVoxelRect(Data.W, Data.H, Data.Height);
            }
            else if (Data.RegionType == DecorationData.Shape.STRIP)
            {
                ret = VoxelGizmos.CreateVoxelRectStrip(Data.W, Data.H, Data.Height);
            }
            else
            {
                ret = VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            }
            {
                var meshrender = ret.GetComponent<MeshRenderer>();
                if (meshrender != null && zone.config.UnitTemplateName != null)
                {
                    var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        Colors.DecodeARGB(Data.Color, out float r, out float g, out float b, out float a);
                        meshrender.material = GameObject.Instantiate(cell_meshr.material);
                        meshrender.material.SetColor("_Color", a == 0 ? voxel.DEFAULT_FLAG_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f));
                    }
                }
            }
            return ret;
        }
        protected override void OnUpdate(UnityLayerDecoration obj, GameObject gizmos)
        {

        }
    }
    public class PointGizmos : FlagGizmos<UnityLayerPoint>
    {
        protected override GameObject OnInit(UnityLayerPoint flag)
        {
            var voxel = VoxelComponent.Instance;
            var zone = flag.zone;
            var Data = flag.Data;
            var ret = VoxelGizmos.CreateVoxelCylinder(Data.Radius, Data.Height);
            {
                var meshrender = ret.GetComponent<MeshRenderer>();
                if (meshrender != null && zone.config.UnitTemplateName != null)
                {
                    var cell_meshr = zone.config.UnitTemplateName.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        Colors.DecodeARGB(Data.Color, out float r, out float g, out float b, out float a);
                        meshrender.material = GameObject.Instantiate(cell_meshr.material);
                        meshrender.material.SetColor("_Color", a == 0 ? voxel.DEFAULT_FLAG_GIZMOS_COLOR.SetAlpha(0.5f) : new Color(r, g, b, 0.5f));
                    }
                }
            }
            return ret;
        }
        protected override void OnUpdate(UnityLayerPoint obj, GameObject gizmos)
        {

        }
    }
    //------------------------------------------------------------------------------------------------------
}
