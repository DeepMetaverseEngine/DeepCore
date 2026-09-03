using DeepCore;
using DeepCore.GUI.Cell;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Cell;
using DeepCore.XCSV;
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
using System.IO;
using System.Linq;
using UnityEngine;
using static DeepGame3D.Unity.BattleView.UnityZoneUnit;
using static DeepMetaGame.Unity.UnityBattleFactory;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------
    public class CPJAssetsLoaderComponent : AssetsLoaderComponent
    {
        public UniCPJLoader Loader { get; set; } = new UniCPJLoader();
        protected override UnityEngine.Object Instantiate(UnityEngine.Object obj)
        {
            if (obj is GameObject go && go.TryGetComponent<CellSpriteController>(out var cell))
            {
                return cell.Display.Clone().gameObject;
            }
            return base.Instantiate(obj);
        }
        protected override void DestoryInstance(UnityEngine.Object obj)
        {
            if (obj is GameObject go && go.TryGetComponent<CellSpriteController>(out var cell))
            {
                cell.Display.Dispose();
                return;
            }
            base.DestoryInstance(obj);
        }
        //         public override void UnloadAssets(IAssetsTuple obj)
        //         {
        // //             if (obj is CPJAssetsTuple cpj && cpj.template is GameObject go && go.TryGetComponent<CellSpriteController>(out var cell))
        // //             {
        // //                 cell.Display.Dispose();
        // //                 return;
        // //             }
        //             //             if (handler is UniCPJFileResource cpj)
        //             //             {
        //             //                 cpj.Dispose();
        //             //             }
        //         }
        public override void LoadAssets<ST>(in string file, ResourceType resType, ST st, LoadAssetsHandler<ST> cb)
        {
            if (TryGetSetObject(file, out var cpj, out var go))
            {
                cb(st, new CPJAssetsTuple(cpj, go)
                {
                    resName = file,
                    resType = resType,
                });
            }
            else
            {
                cb(st, null);
            }
        }

        public override IAssetsTemplate LoadAssets(in string file, ResourceType resType)
        {
            TryGetSetObject(file, out var cpj, out var go);
            return new CPJAssetsTuple(cpj, go)
            {
                resName = file,
                resType = resType,
            };
        }

        protected virtual bool TryGetSetObject(string file, out UniCPJFileResource cpj, out GameObject go)
        {
            cpj = null;
            go = null;
            if (UniCPJLoader.TryGetOutputFile(Owner.RootPath + file, out var binPath, out var setName))
            {
                cpj = Loader.LoadFile(binPath);
                if (cpj != null)
                {
                    cpj.Load();
                    var spr = cpj.GetSprite(setName);
                    if (spr != null)
                    {
                        var display = CellSpriteObject.Create(spr);
                        go = display.gameObject;
                        return true;
                    }
                    var map = cpj.GetMap(setName);
                    if (map != null)
                    {
                        var display = new CellMapObject(map);
                        go = display.gameObject;
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public class CPJAssetsTuple : Disposable, IAssetsTemplate
    {
        public object handler { get => cpj; }
        public UnityEngine.Object template { get; }
        public ResourceType resType { get; set; }
        public string resName { get; set; }
        public readonly UniCPJFileResource cpj;
        public CPJAssetsTuple(UniCPJFileResource cpj, GameObject go)
        {
            this.cpj = cpj;
            this.template = go;
        }
        protected override void Disposing()
        {
            GameObject.Destroy(template);
            cpj.Dispose();
        }
        public bool TryListAnims(List<AnimInfo> actions, UnitInfo unit = null)
        {
            return false;
        }
        public bool TryGetDurationMS(out float ms, out bool loop)
        {
            ms = 0;
            loop = false;
            return false;
        }
        public bool PlayEffect(IWrapAssets wrap, string action, bool loop, float speed, Transform binding)
        {
            return false;
        }
        public bool PlayAnim(IWrapAssets wrap, string action, bool loop, float speed)
        {
            return false;
        }
        public bool PlayAction(IWrapAssets wrap, UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
            return false;
        }
        public bool PlaySound(IWrapAssets go, ResourceType? restype = null)
        {
            return false;
        }
    }
    //-----------------------------------------------------------------------------------------------------

    public class CPJResourceComponent : ResourceComponent
    {
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
            public CellSpriteController sprite { get; private set; }
            public Transform transform => assets?.gameObject?.transform;
            public GameObject gameObject => assets?.gameObject;
            public virtual SimpleResObject Init(IWrapAssetsGO wrap)
            {
                this.assets = assets;
                wrap.transform.localPosition = Vector3.zero;
                wrap.transform.localScale = Vector3.one;
                wrap.transform.localRotation = Quaternion.identity;
                this.sprite = wrap?.gameObject.GetComponentInChildren<CellSpriteController>();
                return this;
            }
            protected override void Disposing()
            {
                assets?.Dispose();
                assets = null;
                sprite = null;
            }
            public virtual bool TryGetEffectTimeMS(out float ms, out bool loop)
            {
                ms = 0;
                loop = false;
                if (sprite != null)
                {
                    ms = sprite.CurrentAnimateTotalTimeMS;
                    return true;
                }
                return false;
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
                return gameObject.transform;
            }
            public virtual void PlayAnim(string actionName, float speed, bool loop)
            {
                PlayAnim(actionName, speed);
            }
            public virtual void PlayAnim(UnitActionStatus main, string sub, float speed)
            {
                PlayAnim(main.ToString(), speed);
            }
            public void PlayAnim(UnitActionStatus main, string actionName, float speed, bool loop)
            {
                PlayAnim(actionName, speed, loop);
            }
            public virtual void PlayAnim(string StateName, float speed)
            {
                if (sprite != null)
                {
                    sprite.CurrentAnimateName = StateName;
                }
            }
            public virtual void PlayAnim(UnityActionStatus state)
            {
                if (sprite != null)
                {
                    sprite.CurrentAnimateName = state.StateName;
                }
            }
            public virtual void StopAnim()
            {
                if (sprite != null)
                {
                }
            }
            public virtual void PauseAnim()
            {
                if (sprite != null)
                {
                    sprite.IsPause = true;
                }
            }
            public virtual void ResumeAnim()
            {
                if (sprite != null)
                {
                    sprite.IsPause = false;
                }
            }
            public virtual void SpeedChange(UnityActionStatus state)
            {
                if (sprite != null)
                {
                }
            }
            public virtual bool TryGetAnimatorStateDuriationMS(string name, out float timeMS)
            {
                if (sprite != null && sprite.Display != null)
                {
                    if (sprite.Display.CellSprite.Meta.TryGetAnimateIndex(name, out var anim))
                    {
                        timeMS = sprite.Display.CellSprite.Meta.GetFrameCount(anim) * CPJEnviroment.GLOBAL_TICK_INTERVAL_MS;
                        return true;
                    }
                }
                timeMS = 0;
                return false;
            }
            public virtual void UpdateResource(UnityPoolingObject obj)
            {
                gameObject.transform.rotation = Quaternion.identity;
                if (obj is UnityLayerObject layerObj)
                {
                    var od = CMath.OpitimizeRadians(layerObj.layerObject.Direction);
                    if (od > CMath.RADIANS_90 && od < CMath.RADIANS_270)
                    {
                        gameObject.transform.localScale = new Vector3(-1, 1, 1);
                    }
                    else
                    {
                        gameObject.transform.localScale = new Vector3(1, 1, 1);
                    }
                }
                else
                {
                    gameObject.transform.localScale = new Vector3(1, 1, 1);
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
                        assets.gameObject.transform.localScale = new Vector3(1, 1, 1);
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
                //                         else if (layerSpell.Target != null)
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
                //                         //if (layerSpell.DistancePos.HasValue)
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
            public UnityEffectPlay player { get; private set; }
            public virtual SimpleEffectRes Init(UnityEffectPlay effect, IWrapAssetsGO wrap)
            {
                base.Init(wrap);
                this.player = effect;
                InitEffect(effect.BindEffect, effect.BindOwner, effect.ForceBind);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.player = null;
            }
            protected virtual void InitEffect(LaunchEffect effect, UnityLayerObject owner, bool forceBind = false)
            {
                if (!string.IsNullOrEmpty(this.player.zone.config.EffectLayerName))
                {
                    SetLayer(LayerMask.NameToLayer(this.player.zone.config.EffectLayerName));
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
                        }
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
        protected override IZoneResource CreateZoneRes(UnityZone zone, IWrapAssetsGO wrap) => wrap != null ? new SimpleZoneRes().Init(zone, wrap) : null;
        protected override IUnitResourceObject CreateUnitRes(UnityZoneUnit unit, IWrapAssetsGO wrap) => unit.objectPool.AllocAutoRelease<SimpleUnitRes>().Init(unit, wrap);
        protected override IItemResourceObject CreateItemRes(UnityZoneItem item, IWrapAssetsGO wrap) => item.objectPool.AllocAutoRelease<SimpleItemRes>().Init(item, wrap);
        protected override ISpellResourceObject CreateSpellRes(UnityZoneSpell spell, IWrapAssetsGO wrap) => spell.objectPool.AllocAutoRelease<SimpleSpellRes>().Init(spell, wrap);
        protected override IEffectResourceObject CreateEffectRes(UnityEffectPlay effect, IWrapAssetsGO wrap) => effect.objectPool.AllocAutoRelease<SimpleEffectRes>().Init(effect, wrap);
        protected override IFlagResourceObject CreateFlagRes(UnityZoneFlag flag, IWrapAssetsGO wrap) => flag.objectPool.AllocAutoRelease<SimpleFlagRes>().Init(flag, wrap);
        //-----------------------------------------------------------------------------------------------------
    }

}

