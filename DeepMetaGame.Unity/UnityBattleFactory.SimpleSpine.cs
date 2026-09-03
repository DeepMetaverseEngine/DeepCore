using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static DeepGame3D.Unity.BattleView.UnityZoneUnit;
using static DeepMetaGame.Unity.UnityBattleFactory;

namespace DeepMetaGame.Unity
{
    public interface ISpine
    {
        //----------------------------------------------------------------
        bool playing { get; set; }
        bool loop { get; set; }
        float speed { get; set; }
        //----------------------------------------------------------------
        /// <summary>
        /// 当前Skin名字
        /// </summary>
        string initialSkinName { get; set; }
        /// <summary>
        /// 当前动画名字
        /// </summary>
        string AnimationName { get; set; }
        /// <summary>
        /// 附加Skin，用于武器或者Avatar
        /// </summary>
        /// <param name="skins"></param>
        void SetAvatar(params string[] skins);
        //----------------------------------------------------------------
        //当前动画时长
        float TotalDuration { get; }
        bool HasAnimation(string name);
        IEnumerable<string> Animations { get; }
        bool HasSkin(string name);
        IEnumerable<string> Skins { get; }
        //----------------------------------------------------------------
    }

    public abstract class SpineResourceComponent : ResourceComponent
    {
        public static SpineResourceComponent SpineInstance { get; private set; }
        public SpineResourceComponent()
        {
            SpineInstance = this;
        }

        public abstract class SimpleRes : BattleAutoRecycle
        {
            private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(SimpleResObject));
            new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
            new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
            public SimpleRes()
            {
                Alloc.RecordConstructor(GetType());
            }
            ~SimpleRes()
            {
                if (!IsDisposed)
                {
                    Alloc.RecordDispose(GetType());
                }
                Alloc.RecordDestructor(GetType());
            }
            protected sealed override void RecordDisposing()
            {
                Alloc.RecordDispose(GetType());
            }
            protected sealed override void RecordReuse()
            {
                Alloc.RecordReuse(GetType());
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleZoneRes : SimpleRes, IZoneResource
        {
            protected IWrapAssetsGO wrap { get; private set; }
            public UnityZone zone { get; private set; }
            public Transform transform => wrap?.gameObject?.transform;
            public bool Active
            {
                get => wrap.gameObject.activeSelf;
                set => wrap.gameObject.SetActive(value);
            }
            public virtual SimpleZoneRes Init(UnityZone zone, IWrapAssetsGO wrap)
            {
                this.wrap = wrap;
                this.zone = zone;
                var layer = zone.layer;
                {
                    if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
                    {
                        wrap?.gameObject.SetLayer(zone.config.RayCastTerrainLayerName);
                    }
                }
                return this;
            }
            protected override void Disposing()
            {
                wrap?.Dispose();
                wrap = null;
                zone = null;
            }
            public virtual void UpdateResource()
            {

            }
        }
        public abstract class SimpleResObject : SimpleRes, IResourceObject
        {
            public IWrapAssetsGO assets { get; private set; }
            public ISpine spine { get; private set; }
            public Transform transform => assets?.gameObject?.transform;
            public GameObject gameObject => assets?.gameObject;
            protected float ResScale = 1f;
            public virtual SimpleResObject Init(IWrapAssetsGO wrap)
            {
                this.assets = wrap;
                wrap.transform.localPosition = Vector3.zero;
                wrap.transform.localScale = Vector3.one;
                wrap.transform.localRotation = Quaternion.Euler(90, 0, 0);
                if (SpineInstance.TryGetSpine(wrap?.gameObject, out var _spine))
                {
                    this.spine = _spine;
                }
                return this;
            }
            protected override void Disposing()
            {
                assets?.Dispose();
                assets = null;
                spine = null;
                ResScale = 1f;
            }
            /// <summary>
            /// 获得动画时长
            /// </summary>
            /// <param name="ms"></param>
            /// <param name="loop"></param>
            /// <returns></returns>
            public virtual bool TryGetEffectTimeMS(out float ms, out bool loop)
            {
                ms = 0;
                loop = false;
                if (spine == null) return false;
                loop = spine.loop;
                var max = spine.TotalDuration;
                ms = max * 1000f;
                return true;
            }
            public virtual void SetLayer(int layer)
            {
                gameObject.SetLayer(layer);
            }
            public virtual void SetLayer(string layer)
            {
                gameObject.SetLayer(layer);
            }
            public virtual Transform FindDeep(string name)
            {
                return gameObject.FindDeep(name);
            }
            public virtual void PlayAnim(UnityActionStatus state)
            {
                if (spine != null)
                {
                    spine.AnimationName = state.StateName;
                }
            }
            public void PlayAnim(string actionName, float speed, bool loop)
            {
                if (spine != null)
                {
                    spine.AnimationName = actionName;
                }
            }
            public void PlayAnim(string actionName, float speed)
            {
                if (spine != null)
                {
                    spine.AnimationName = actionName;
                }
            }

            public void PlayAnim(UnitActionStatus main, string actionName, float speed)
            {
                if (spine != null)
                {
                    spine.AnimationName = actionName;
                }
            }
            public void PlayAnim(UnitActionStatus main, string actionName, float speed, bool loop)
            {
                if (spine != null)
                {
                    spine.AnimationName = actionName;
                }
            }


            public virtual void StopAnim()
            {
                if (spine != null)
                {
                }
            }
            public virtual void PauseAnim()
            {
                if (spine != null)
                {
                }
            }
            public virtual void ResumeAnim()
            {
                if (spine != null)
                {
                }
            }
            public virtual void SpeedChange(UnityActionStatus state)
            {
                if (spine != null)
                {
                }
            }
            public virtual bool TryGetAnimatorStateDuriationMS(string name, out float timeMS)
            {
                if (spine != null)
                {
                }
                timeMS = 0;
                return false;
            }
            public virtual void UpdateResource(UnityPoolingObject obj)
            {
                gameObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                if (obj is UnityLayerObject layerObj)
                {
                    var od = CMath.OpitimizeRadians(layerObj.layerObject.Direction);
                    if (od > CMath.RADIANS_90 && od < CMath.RADIANS_270)
                    {
                        gameObject.transform.localScale = new Vector3(-ResScale, ResScale, 1);
                    }
                    else
                    {
                        gameObject.transform.localScale = new Vector3(ResScale, ResScale, 1);
                    }
                }
                else
                {
                    gameObject.transform.localScale = new Vector3(ResScale, ResScale, 1);
                }
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public abstract class SimpleObjRes<T> : SimpleResObject, IResourceObject where T : UnityLayerObject
        {
            public T layerObj { get; private set; }
            public virtual SimpleObjRes<T> Init(T obj, IWrapAssetsGO wrap)
            {
                base.Init(wrap);
                this.layerObj = obj;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                layerObj = null;
            }
        }

        //-----------------------------------------------------------------------------------------------------
        public class SimpleUnitRes : SimpleObjRes<UnityZoneUnit>, IUnitResourceObject
        {
            public UnityZoneUnit unit => base.layerObj;
            new public virtual SimpleUnitRes Init(UnityZoneUnit unit, IWrapAssetsGO wrap)
            {
                base.Init(unit, wrap);
                return this;
            }

            protected override void Disposing()
            {
                base.Disposing();
            }

        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleSpellRes : SimpleObjRes<UnityZoneSpell>, ISpellResourceObject
        {
            public UnityZoneSpell spell { get => base.layerObj; }
            public Transform Bone1 { get; set; }
            public Transform Bone2 { get; set; }
            new public virtual SimpleSpellRes Init(UnityZoneSpell spell, IWrapAssetsGO wrap)
            {
                base.Init(spell, wrap);
                var layerSpell = spell.layerSpell;
                {
                    if (layerSpell.Info.FileBodyScale != 1f && layerSpell.Info.FileBodyScale != 0)
                    {
                        ResScale = layerSpell.Info.FileBodyScale;
                    }
                    if (!string.IsNullOrEmpty(layerSpell.Info.BonesBegin))
                    {
                        Bone1 = wrap.transform.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == layerSpell.Info.BonesBegin));
                    }
                    if (!string.IsNullOrEmpty(layerSpell.Info.BonesEnd))
                    {
                        Bone2 = wrap.transform.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == layerSpell.Info.BonesEnd));
                    }
                }
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                Bone1 = null;
                Bone2 = null;
            }
            public override void UpdateResource(UnityPoolingObject obj)
            {
                var parent = base.layerObj.parent;
                var layerSpell = base.layerObj.layerSpell;
                if (assets != null)
                {
                    ///如果是投射物，则根据方向翻转
                    if (layerSpell.Info.IsProjectile)
                    {
                        assets.gameObject.transform.localScale = new Vector3(ResScale, ResScale, 1);
                    }
                    else
                    {
                        base.UpdateResource(obj);
                    }
                }
                this.UpdateSpellBones();
                //                 switch (layerSpell.Info.BodyShape)
                //                 {
                //                     case SpellTemplate.Shape.LineToStart:
                //                         if (layerSpell.StartPos != null)
                //                         {
                //                             var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                //                             var p2 = parent.BattleToUnityWorldPosition(layerSpell.StartPos);
                //                             var t_z2w = parent.transform.localToWorldMatrix;
                //                             p1 = t_z2w.MultiplyPoint(p1);
                //                             p2 = t_z2w.MultiplyPoint(p2);
                //                             if (Bone1) { Bone1.position = p1; }
                //                             if (Bone2) { Bone2.position = p2; }
                //                         }
                //                         break;
                //                     case SpellTemplate.Shape.LineToTarget:
                //                         if (layerSpell.Target != null)
                //                         {
                //                             var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                //                             var p2 = parent.BattleToUnityWorldPosition(layerSpell.Target.Position);
                //                             var t_z2w = parent.transform.localToWorldMatrix;
                //                             p1 = t_z2w.MultiplyPoint(p1);
                //                             p2 = t_z2w.MultiplyPoint(p2);
                //                             if (Bone1) { Bone1.position = p1; }
                //                             if (Bone2) { Bone2.position = p2; }
                //                         }
                //                         else if (layerSpell.TargetPos != null)
                //                         {
                //                             var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                //                             var p2 = parent.BattleToUnityWorldPosition(layerSpell.TargetPos.Value);
                //                             var t_z2w = parent.transform.localToWorldMatrix;
                //                             p1 = t_z2w.MultiplyPoint(p1);
                //                             p2 = t_z2w.MultiplyPoint(p2);
                //                             if (Bone1) { Bone1.position = p1; }
                //                             if (Bone2) { Bone2.position = p2; }
                //                         }
                //                         break;
                //                     case SpellTemplate.Shape.LineToSender:
                //                         if (layerSpell.Sender != null)
                //                         {
                //                             var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                //                             var p2 = parent.BattleToUnityWorldPosition(layerSpell.Sender.Position);
                //                             var t_z2w = parent.transform.localToWorldMatrix;
                //                             p1 = t_z2w.MultiplyPoint(p1);
                //                             p2 = t_z2w.MultiplyPoint(p2);
                //                             if (Bone1) { Bone1.position = p1; }
                //                             if (Bone2) { Bone2.position = p2; }
                //                         }
                //                         break;
                //                     case SpellTemplate.Shape.RectStrip:
                //                     case SpellTemplate.Shape.RectStripRay:
                //                     case SpellTemplate.Shape.Strip:
                //                     case SpellTemplate.Shape.StripRay:
                //                     case SpellTemplate.Shape.StripRayTouchEnd:
                //                     case SpellTemplate.Shape.WideStrip:
                //                         {
                //                             var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                //                             var p2 = parent.BattleToUnityWorldPosition(layerSpell.DistancePos);
                //                             var t_z2w = parent.transform.localToWorldMatrix;
                //                             p1 = t_z2w.MultiplyPoint(p1);
                //                             p2 = t_z2w.MultiplyPoint(p2);
                //                             if (Bone1) { Bone1.position = p1; }
                //                             if (Bone2) { Bone2.position = p2; }
                //                         }
                //                         break;
                //                 }
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleItemRes : SimpleObjRes<UnityZoneItem>, IItemResourceObject
        {
            public UnityZoneItem item => base.layerObj;
            new public virtual SimpleItemRes Init(UnityZoneItem item, IWrapAssetsGO wrap)
            {
                base.Init(item, wrap);
                {
                    var ares = item.layerItem.AResource;
                    {
                        var scale = item.layerItem.AResource.BodyScale;
                        this.ResScale = scale;
                        var offset = item.Space.BattleToUnityVoxelAnchorOffset(item.layerObject.BodyHeight, ares.BodyVoxelAnchor);
                        wrap.transform.localPosition += (offset);
                    }
                }
                return this;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleFlagRes : SimpleObjRes<UnityZoneFlag>, IFlagResourceObject
        {
            public UnityZoneFlag flag => base.layerObj;
            new public virtual SimpleFlagRes Init(UnityZoneFlag flag, IWrapAssetsGO wrap)
            {
                base.Init(flag, wrap);
                return this;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleEffectRes : SimpleResObject, IEffectResourceObject
        {
            public UnityEffectPlay effect { get; private set; }
            public UnityEffectPlay player { get; private set; }
            public virtual SimpleEffectRes Init(UnityEffectPlay effect, IWrapAssetsGO wrap)
            {
                this.player = player;
                base.Init(wrap);
                this.effect = effect;
                InitEffect(effect.BindEffect, effect.BindOwner, effect.ForceBind);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.effect = null;
                this.player = null;
            }
            protected virtual void InitEffect(LaunchEffect effect, UnityLayerObject owner, bool forceBind = false)
            {
                if (!string.IsNullOrEmpty(this.effect.zone.config.EffectLayerName))
                {
                    SetLayer(LayerMask.NameToLayer(this.effect.zone.config.EffectLayerName));
                }
                if (effect != null)
                {
                    if (owner is UnityZoneUnit ownerUnit)
                    {
                        if (effect.BindBody || forceBind)
                        {
                            var part = ownerUnit.ModelWrap?.FindDeep(effect.BindPartName);
                            if (part)
                            {
                                this.transform.SetParent(part, false);
                            }
                            else
                            {
                                this.transform.SetParent(owner.transform, false);
                            }
                        }
                        else
                        {
                            this.transform.rotation = Quaternion.Euler(90, 0, 0);
                        }
                        //var offset = owner.Space.BattleToUnityVoxelAnchorOffset(owner.layerObject.BodyHeight, effect.BodyVoxelAnchor);
                        //transform.localPosition += (offset);
                    }
                    if (effect.ScaleToBodySize != 0)
                    {
                        this.ResScale = effect.ScaleToBodySize;
                    }
                }
                if (transform.TryGetComponentsInChildren<ParticleSystem>(out var pss))
                {
                    foreach (var p in pss)
                    {
                        p.Simulate(0, true, true);
                        p.Play();
                    }
                }
            }
            public virtual void PlayAnim(LaunchEffect effect)
            {
                assets.AssetsTemplate.PlayAnim(assets, effect.AnimName, effect.IsLoop, effect.TimeScale);
            }
            public override void UpdateResource(UnityPoolingObject obj)
            {
                base.UpdateResource(obj);
            }
        }
        //-----------------------------------------------------------------------------------------------------
        protected override IZoneResource CreateZoneRes(UnityZone zone, IWrapAssetsGO wrap) => new SimpleZoneRes().Init(zone, wrap);
        protected override IUnitResourceObject CreateUnitRes(UnityZoneUnit unit, IWrapAssetsGO wrap) => unit.objectPool.AllocAutoRelease<SimpleUnitRes>().Init(unit, wrap);
        protected override IItemResourceObject CreateItemRes(UnityZoneItem item, IWrapAssetsGO wrap) => item.objectPool.AllocAutoRelease<SimpleItemRes>().Init(item, wrap);
        protected override ISpellResourceObject CreateSpellRes(UnityZoneSpell spell, IWrapAssetsGO wrap) => spell.objectPool.AllocAutoRelease<SimpleSpellRes>().Init(spell, wrap);
        protected override IEffectResourceObject CreateEffectRes(UnityEffectPlay effect, IWrapAssetsGO wrap) => effect.objectPool.AllocAutoRelease<SimpleEffectRes>().Init(effect, wrap);
        protected override IFlagResourceObject CreateFlagRes(UnityZoneFlag flag, IWrapAssetsGO wrap) => flag.objectPool.AllocAutoRelease<SimpleFlagRes>().Init(flag, wrap);
        //-----------------------------------------------------------------------------------------------------

    }
}


