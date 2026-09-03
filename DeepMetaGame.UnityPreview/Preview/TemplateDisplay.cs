using DeepCore.Geometry;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using UnityEngine;
using UnityEngine.UIElements;
using DeepMetaGame.Data.Helper;

namespace DeepMetaGame.Unity.Preview.Preview
{
    //---------------------------------------------------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------------------------------------------------
    public class ItemDisplay : PreviewObject<ItemTemplate>
    {
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(ItemTemplate item)
        {
            BodySize = item.BodySize;
            BodyHeight = item.BodyHeight;
            if (item.Abilities.TryGetComponentAs<ItemResource>(out var i_res))
            {
                var res = LoadRes(i_res.FileName, DeepMetaGame.Data.ResourceType.Object);
                if (res != null)
                {
                    if (res.gameObject.TryGetComponentsInChildren<ParticleSystem>(out var pss))
                    {
                        foreach (var p in pss)
                        {
                            p.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        }
                    }
                    res.transform.localScale = new UnityEngine.Vector3(i_res.BodyScale, i_res.BodyScale, i_res.BodyScale);
                    switch (i_res.BodyVoxelAnchor)
                    {
                        case VoxelAnchor.Floating:
                            res.transform.localPosition = new UnityEngine.Vector3(0, this.BodyHeight / 2f, 0);
                            break;
                        case VoxelAnchor.Flooring:
                            break;
                        case VoxelAnchor.Ceiling:
                            res.transform.localPosition = new UnityEngine.Vector3(0, this.BodyHeight, 0);
                            break;
                    }
                }
                if (i_res.BindingEffect != null)
                {
                     LoadEffect(i_res.BindingEffect);
                }
            }
            //RTG.LookAt(transform);
            RTG.TargetObject = gameObject;
            //LookAt(transform.position + new UnityEngine.Vector3(0, 0, -100));
        }
    }
    //---------------------------------------------------------------------------------------------------------------------------------
    public class BuffDisplay : PreviewObject<BuffTemplate>
    {
        private PopupKeyFrames<BuffTemplate.KeyFrame> current_frames = new PopupKeyFrames<BuffTemplate.KeyFrame>();
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(BuffTemplate buff)
        {
            if (buff.Abilities.TryGetComponentAs<BuffEffectAbility>(out var effects))
            {
                var res = LoadEffect(effects.BindingEffect);
                if (res != null)
                {
                    //RTG.LookAt(res.transform);
                    RTG.TargetObject = res.gameObject;
                }
                if (effects.BindingEffectList != null)
                {
                    foreach (var e in effects.BindingEffectList)
                    {
                        LoadEffect(e);
                    }
                }
            }
            //LookAt(transform.position + new UnityEngine.Vector3(0, 0, -100));
        }
        protected override void DoReplay()
        {
            current_frames.Clear();
            current_frames.AddRange(Data.KeyFrames);
        }
        protected override void DoUpdate()
        {
            using (var kfs = ObjectPool.AllocList<BuffTemplate.KeyFrame>())
            {
                int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                foreach (var kf in kfs)
                {
                    if (kf.Effect != null)
                    {
                        ShowEffect(kf.Effect);
                    }
                }
            }
        }
        protected override void DoDestory()
        {
            current_frames.Clear();
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------

    public class SkillDisplay : PreviewObject<SkillTemplate>
    {
        private PopupKeyFrames<UnitActionData.KeyFrame> current_frames = new PopupKeyFrames<UnitActionData.KeyFrame>();
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(SkillTemplate skill)
        {
            //LookAt(transform.position + new UnityEngine.Vector3(0, 0, -100));
        }
        protected override void DoReplay()
        {
            current_frames.Clear();
            foreach (var action in Data.ActionQueue)
            {
                current_frames.AddRange(action.KeyFrames);
            }
        }
        protected override void DoUpdate()
        {
            using (var kfs = ObjectPool.AllocList<UnitActionData.KeyFrame>())
            {
                int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                foreach (var kf in kfs)
                {
                    if (kf.Effect != null)
                    {
                        ShowEffect(kf.Effect);
                    }
                }
            }
        }
        protected override void DoDestory()
        {
            current_frames.Clear();
        }
    }
    //---------------------------------------------------------------------------------------------------------------------------------
    public class SpellDisplay : PreviewObject<SpellTemplate>
    {
        private float mBaseSize;
        private float mDistance;
        private PopupKeyFrames<SpellTemplate.KeyFrame> current_frames = new PopupKeyFrames<SpellTemplate.KeyFrame>();
        public float ResourceFitSize
        {
            get
            {
                switch (Data.BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        return 1f;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        return mDistance;
                    default:
                        return mBaseSize * 2;
                }
            }
        }
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(SpellTemplate spell)
        {
            mBaseSize = spell.BodySize;
            mDistance = spell.Distance;
            BodySize = spell.BodySize;
            BodyHeight = spell.BodyHeight;
            PlaySound(spell.SoundName, DeepMetaGame.Data.ResourceType.Sound_Effect);
            var res = LoadRes(spell.FileName, DeepMetaGame.Data.ResourceType.Effect);
            if (res != null)
            {
                //RTG.LookAt(res.transform);
                RTG.TargetObject = res.gameObject;
                if (spell.FitOwnerScale)
                {
                    res.transform.localScale = new UnityEngine.Vector3(ResourceFitSize, BodyHeight, ResourceFitSize);
                }
                else if (spell.FileBodyScale != 1f && spell.FileBodyScale != 0)
                {
                    res.transform.localScale = UnityEngine.Vector3.one * spell.FileBodyScale;
                }
                //Proxy.PlayEffect(res, spell.LifeTimeMS);
            }
            if (spell.BindingEffect != null)
            {
                LoadEffect(spell.BindingEffect);
            }
            //LookAt(transform.position + new UnityEngine.Vector3(0, 0, -100));
        }

        //         public override VoxelCylinder LocalBody
        //         {
        //             get
        //             {
        //                 var spell = Data;
        //                 var pos = DeepCore.Geometry.Vector3.Zero; 
        //                 var height = spell.BodyHeight;
        //                 pos = spell.AdjustVoxelAnchor(pos, ref height);
        //                 //                 var height = spell.BodyHeight;
        //                 //                 switch (spell.BodyShape)
        //                 //                 {
        //                 //                     case SpellTemplate.Shape.LineToStart:
        //                 //                     case SpellTemplate.Shape.LineToTarget:
        //                 //                         height = 0;
        //                 //                         break;
        //                 //                     case SpellTemplate.Shape.LineToSender:
        //                 //                         height = 0;
        //                 //                         break;
        //                 //                 }
        //                 //                 switch (spell.BodyHitVoxelAnchor)
        //                 //                 {
        //                 //                     case SpellTemplate.HitVoxelAnchor.NA:
        //                 //                         switch (spell.BodyVoxelAnchor)
        //                 //                         {
        //                 //                             case VoxelAnchor.Floating:
        //                 //                                 pos.Z -= height / 2f;
        //                 //                                 break;
        //                 //                             case VoxelAnchor.Flooring:
        //                 //                                 break;
        //                 //                             case VoxelAnchor.Ceiling:
        //                 //                                 pos.Z -= height;
        //                 //                                 break;
        //                 //                         }
        //                 //                         break;
        //                 //                     case SpellTemplate.HitVoxelAnchor.Up:
        //                 //                         break;
        //                 //                     case SpellTemplate.HitVoxelAnchor.Middle:
        //                 //                         pos.Z -= height / 2f;
        //                 //                         break;
        //                 //                     case SpellTemplate.HitVoxelAnchor.Down:
        //                 //                         pos.Z -= height;
        //                 //                         break;
        //                 //                 }
        //                 return new VoxelCylinder(pos, BodySize, height);
        //             }
        //         }
        public override VoxelCylinder LocalBody
        {
            get
            {
                var spell = Data;
                var height = BodyHeight;
                var pos = spell.AdjustVoxelAnchor(DeepCore.Geometry.Vector3.Zero, ref height);
                return new VoxelCylinder(pos, BodySize, height);
            }
        }
        protected override GameObject InitGizmos()
        {
            var spell = Data;
            var height = spell.BodyHeight;
            var pos = spell.AdjustVoxelAnchor(DeepCore.Geometry.Vector3.Zero, ref height);
            var shapeObject = AttackShapeGizmos.CreateAttackShape(
                  spell.AsBodyShape,
                  spell.BodySize,
                  height,
                  spell.Distance,
                  spell.FanAngle,
                  spell.RectWide,
                  this.transform,
                  this.transform,
                  this.transform);
            if (shapeObject != null)
            {
                shapeObject.transform.SetParent(transform, false);
                shapeObject.transform.localPosition = new UnityEngine.Vector3(pos.X, pos.Z, pos.Y);
                shapeObject.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
                if (RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    AttackShapeGizmos.SetMaterial(
                        shapeObject,
                        drender.material,
                        BodyColor);
                }
                return shapeObject;
            }
            return null;
        }
        protected override void UpdateGizmos(GameObject childGizmos)
        {
            var spell = Data;
            if (childGizmos != null)
            {
                var aoeFactor = 1f;
                switch (spell.BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        break;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        aoeFactor = mDistance / spell.Distance;
                        childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, 1f);
                        break;
                    default:
                        aoeFactor = mBaseSize / spell.BodySize;
                        childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, aoeFactor);
                        break;
                }
                if (childGizmos.activeSelf != PreviewConfig.IsShowGizmos)
                {
                    childGizmos.SetActive(PreviewConfig.IsShowGizmos);
                }
            }
        }
        protected override void DoReplay()
        {
            current_frames.Clear();
            current_frames.AddRange(Data.KeyFrames);
        }
        protected override void DoUpdate()
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                foreach (var kf in kfs)
                {
                    if (kf.Effect != null)
                    {
                        ShowEffect(kf.Effect);
                    }
                }
            }
        }
        protected override void DoDestory()
        {
            current_frames.Clear();
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    public class EffectPlayer : PreviewObject<ResInfo>
    {
        private double deadTime = 0;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override  void DoInit(ResInfo data)
        {
            BodyColor = Color.blue.SetAlpha(0.5f);
            BodyHeight = 0;
            var res = LoadRes(Data.ResName, DeepMetaGame.Data.ResourceType.Effect);
            if (res != null)
            {
                deadTime = Proxy.PlayEffect(res);
            }
            else
            {
                Dispose();
            }
        }
        protected override Collider InitEditCollider()
        {
            return null;
        }
        protected override GameObject InitGizmos()
        {
            return null;
        }
        protected override void DoReplay()
        {
        }
        protected override void DoUpdate()
        {
            if (MainRes != null && PassTimeMS > deadTime)
            {
                Dispose();
            }
        }
        protected override void DoDestory()
        {
        }
    }
    public class EffectDisplay : PreviewObject<ValueTuple<LaunchEffect, IViewResource>>
    {
        public LaunchEffect effect => Data.Item1;
        public IViewResource binding { get ; set; }
        private double deadTime = 0;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override  void DoInit(ValueTuple<LaunchEffect, IViewResource> tuple)
        {
            BodyColor = Color.blue.SetAlpha(0.5f);
            BodyHeight = 0;
            this.binding = tuple.Item2;
            if (!tuple.Item1.BindBody)
            {
                this.binding = null;
            }
            PlaySound(effect.SoundName, DeepMetaGame.Data.ResourceType.Sound_Effect);
            //RTG.PlaySound(effect.SoundName, this.gameObject, SoundType.Effect);
            var res =  LoadEffect(effect);
            if (res != null)
            {
                deadTime = Proxy.PlayEffect(res, effect, binding);
            }
            else
            {
                Dispose();
            }
        }
        protected override Collider InitEditCollider()
        {
            return null;
        }
        protected override GameObject InitGizmos()
        {
            // if (effect.Warning != null)
            // {
            //     var gizmos = AttackShapeGizmos.CreateWarningShape(effect);
            //     if (Proxy.TempGizmoz && Proxy.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
            //     {
            //         AttackShapeGizmos.SetMaterial(gizmos, drender.material, BodyColor);
            //     }
            //     gizmos.transform.SetParent(transform, false);
            //     gizmos.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
            //     return gizmos;
            // }
            if (effect.WarningShape != null)
            {
                var shape = effect.WarningShape;
                var shapeObject = new GameObject(shape.AShape.ToString());
                shapeObject.transform.SetParent(transform, false);
                shapeObject.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
                shapeObject.transform.localPosition = new UnityEngine.Vector3(0, 0.1f, 0);
                var gizmos = shapeObject.AddComponent<AttackShapeGizmos>().InitGizmos(
                      shape.AsShape,
                      BodySize + shape.AttackRange,
                      BodyHeight,
                      BodySize + shape.AttackRange,
                      shape.AttackAngle,
                      shape.StripWide,
                      null,
                      shape.OffsetRadius);
                if (RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    gizmos.SetMaterial(drender.material, Color.blue.SetAlpha(0.5f));
                }
            }
            return null;
        }
        protected override void UpdateGizmos(GameObject childGizmos)
        {

        }
        protected override void DoReplay()
        {
        }
        protected override void DoUpdate()
        {
            if (MainRes != null && PassTimeMS > deadTime)
            {
                Dispose();
            }
        }
        protected override void DoDestory()
        {
        }
    }

}
