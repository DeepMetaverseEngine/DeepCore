using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.Camera;
using DeepCore.Unity.ResourceViewer;
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
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.Simple;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Playables;
using static DeepGame3D.Unity.BattleView.UnityZoneUnit;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------
    public class SimpleAssetsLoaderComponent : AssetsLoaderComponent
    {
        public static SimpleAssetsLoaderComponent SimpleInstance { get; private set; }
        public SimpleAssetsLoaderComponent()
        {
            SimpleInstance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {
            ABSystem.RootPath = owner.RootPath;
        }
        //         public override void UnloadAssets(IAssetsTuple tuple)
        //         {
        //             if (tuple != null)
        //             {
        //                 GameObject.Destroy(tuple.template);
        //             }
        //         }
        public override void LoadAssets<ST>(in string file, ResourceType resType, ST st, LoadAssetsHandler<ST> cb)
        {
            var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(file);
            var wrap = ABSystem.GetWrapGO(file, name, null);
            if (wrap != null)
            {
                cb(st, new SimpleAssetsTuple(file, resType, wrap)
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
            var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(file);
            var wrap = ABSystem.GetWrapGO(file, name, null);
            if (wrap != null)
            {
                return new SimpleAssetsTuple(file, resType, wrap)
                {
                    resName = file,
                    resType = resType,
                };
            }
            return null;
        }
    }
    public class SimpleAssetsTuple : DefaultAssetsTuple
    {
        public readonly WrapGO wrap;
        public readonly GameObject go;
        public SimpleAssetsTuple(string name, ResourceType resType, WrapGO wrap)
        {
            this.wrap = wrap;
            this.wrap.gameObject.name = name;
            this.go = wrap.gameObject;
            this.resType = resType;
            this.resName = name;
            var wres = wrap.gameObject.GetOrAddComponent<ResourceInfo>();
            //             if (this.wrap.gameObject is GameObject go && go.TryGetComponent<CellSpriteController>(out var cell))
            //             {
            //                 this.cpj = cell;
            //             }
            wres.Refresh();
        }
        protected override void Disposing()
        {
            wrap.Dispose();
            GameObject.Destroy(template);
        }

        public override object handler => wrap;
        public override UnityEngine.Object template => go;
        //         public bool TryPlayAnim(string action, bool loop, float speed, GameObject go)
        //         {
        //             return false;
        //         }
        //         public bool TryGetDurationMS(out float ms)
        //         {
        //             ms = 0;
        //             return false;
        //         }
        public override bool TryListAnims(List<AnimInfo> actions, UnitInfo unit)
        {
            if (base.TryListAnims(actions, unit))
            {
                //                 if (cpj != null && cpj.Display != null)
                //                 {
                //                     foreach (var act in cpj.Meta.Animates)
                //                     {
                //                         actions.Add(new AnimInfo() { Name = $"{act.Name}", Action = act.Name });
                //                     }
                //                 }
                return true;
            }
            return false;
        }
    }
    //-----------------------------------------------------------------------------------------------------
    public class SimpleResourceComponent : ResourceComponent
    {
        public static SimpleResourceComponent SimpleInstance { get; private set; }
        public SimpleResourceComponent()
        {
            SimpleInstance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {
            base.Added(owner);
        }
        //-----------------------------------------------------------------------------------------------------
        #region Resources--------------------------------------------------------------------------------------
        #region Base
        public abstract class SimpleRes : BattleAutoRecycle
        {
            private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(SimpleObjectRes));
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
            public IWrapAssetsGO wrap { get; private set; }
            public UnityZone zone { get; private set; }
            public Transform transform { get; protected set; }
            public GameObject gameObject { get; protected set; }
            public virtual bool Active
            {
                get => wrap.gameObject.activeSelf;
                set => wrap.gameObject.SetActive(value);
            }
            public virtual SimpleZoneRes Init(UnityZone zone, IWrapAssetsGO wrap)
            {
                this.wrap = wrap;
                this.zone = zone;
                var layer = zone.layer;
                this.gameObject = wrap?.gameObject;
                this.transform = wrap?.gameObject?.transform;
                if (gameObject != null)
                {
                    if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
                    {
                        gameObject.SetLayer(zone.config.RayCastTerrainLayerName);
                    }
                    //                     if (wrap.transform.TryGetComponentInChildren<Light>(out var sceneLight))
                    //                     {
                    //                         if (zone.DefaultLight != null)
                    //                         {
                    //                             zone.DefaultLight.enabled = false;
                    //                         }
                    //                         zone.DefaultLight = sceneLight;
                    //                     }
                }
                return this;
            }
            protected override void Disposing()
            {
                gameObject = null;
                transform = null;
                wrap?.Dispose();
                wrap = null;
                zone = null;
            }
            public virtual void UpdateResource()
            {

            }
        }
        //-----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 所有资产的Transform是否都保证为Identity。
        /// </summary>
        public abstract class SimpleObjectRes : SimpleRes, IResourceObject
        {
            private GameObject go;
            private Transform tx;
            protected readonly List<ParticleSystem> pss = new List<ParticleSystem>();
            public Vector3 beginLocalPosition { get; set; } = Vector3.zero;
            public Vector3 beginLocalScale { get; set; } = Vector3.one;
            public Quaternion beginLocalRotation { get; set; } = Quaternion.identity;
            public IWrapAssetsGO assets { get; private set; }
            public int DelayDestoryTimeMS { get; set; }
            public Transform transform => tx;
            public GameObject gameObject => go;
            public SimpleObjectRes()
            {
                this.go = new GameObject("SimpleResObject");
                this.tx = this.go.transform;
            }
            protected override void Destructing()
            {
                if (this.go != null)
                {
                    GameObject.Destroy(go);
                    this.go = null;
                    this.tx = null;
                }
            }
            protected void InitWrap(IWrapAssetsGO res)
            {
                this.assets = res;
                this.pss.Clear();
                this.assets.transform.SetParent(this.transform, false);
                this.assets.gameObject.TryGetComponentsInChildren(pss);
                this.transform.localPosition = Vector3.zero;
                this.transform.localScale = Vector3.one;
                this.transform.localRotation = Quaternion.identity;
            }
            protected override void Disposing()
            {
                this.beginLocalPosition = Vector3.zero;
                this.beginLocalScale = Vector3.one;
                this.beginLocalRotation = Quaternion.identity;
                this.assets?.Dispose();
                this.assets = null;
                this.pss.Clear();
                this.DelayDestoryTimeMS = default;
            }
            public virtual bool TryGetEffectTimeMS(out float ms, out bool loop)
            {
                ms = 0;
                loop = false;
                if (assets == null) return false;
                if (pss.TryGetParticleDuration(out ms, out loop))
                {
                    ms *= 1000;
                    return true;
                }
                return false;
            }
            public virtual void SetLayer(int layer)
            {
                this.gameObject.SetLayer(layer);
            }
            public virtual void SetLayer(string layer)
            {
                this.gameObject.SetLayer(layer);
            }
            public virtual Transform FindDeep(string name)
            {
                return this.gameObject.FindDeep(name);
            }
            public virtual void UpdateResource(UnityPoolingObject obj)
            {
                if (this.transform.localPosition != beginLocalPosition)
                {
                    this.transform.localPosition = beginLocalPosition;
                }
                if (this.transform.localRotation != beginLocalRotation)
                {
                    this.transform.localRotation = beginLocalRotation;
                }
                if (this.transform.localScale != beginLocalScale)
                {
                    this.transform.localScale = beginLocalScale;
                }
            }
            public virtual void PlayAnim(UnitActionStatus main, string actionName, float speed, bool loop)
            {
                assets.AssetsTemplate.PlayAnim(assets, actionName, loop, speed);
            }
            public virtual void PlayAnim(UnityZoneUnit.UnityActionStatus state)
            {
                assets.AssetsTemplate.PlayAnim(assets, state.StateName, state.IsLoop, state.Speed);
            }
            public virtual void PlayAnim(string actionName, float speed, bool loop)
            {
                assets.AssetsTemplate.PlayAnim(assets, actionName, loop, speed);
            }
        }
        public abstract class SimpleObjectRes<T> : SimpleObjectRes, IResourceObject where T : UnityLayerObject
        {
            public T layerObj { get; private set; }
            protected void InitWrap(T obj, IWrapAssetsGO wrap)
            {
                this.layerObj = obj;
                this.InitWrap(wrap);
            }
            protected override void Disposing()
            {
                base.Disposing();
                layerObj = null;
            }
        }
        #endregion Base
        //-----------------------------------------------------------------------------------------------------
        public class SimpleUnitRes : SimpleObjectRes<UnityZoneUnit>, IUnitResourceObject
        {
            //protected IWrapAssetGO wrapDead { get; private set; }
            public Animator Animator { get; private set; }
            public Animation Animation { get; private set; }
            public ISpine Spine { get; private set; }
            public HashMap<string, PlayableDirector> Playables { get; } = new HashMap<string, PlayableDirector>(0);
            public UnityZoneUnit unit => base.layerObj;
            public virtual SimpleUnitRes Init(UnityZoneUnit unit, IWrapAssetsGO res)
            {
                this.InitWrap(unit, res);
                unit.layerUnit.OnUnitAvatarChanged += LayerUnit_OnUnitAvatarChanged;
                {
                    this.transform.localPosition = Vector3.zero;
                    this.transform.localScale = Vector3.one;
                    if (res.gameObject.TryGetComponentInChildren<Animator>(out var animator))
                    {
                        this.Animator = animator;
                        animator.applyRootMotion = false;
                    }
                    if (res.gameObject.TryGetComponentInChildren<Animation>(out var animation))
                    {
                        this.Animation = animation;
                        animation.wrapMode = WrapMode.Loop;
                    }
                    if (SimpleInstance.TryGetSpine(res.gameObject, out var _spine))
                    {
                        this.Spine = _spine;
                        Spine.playing = true;
                        Spine.initialSkinName = unit.layerUnit.Skin;
                        Spine.SetAvatar(unit.layerUnit.Avatar);
                    }
                    if (res.transform.TryGetComponentsInChildren<PlayableDirector>(out var playables, true))
                    {
                        foreach (var playable in playables)
                        {
                            this.Playables.Put(playable.name, playable);
                        }
                    }
                }
                return this;
            }
            protected virtual void LayerUnit_OnUnitAvatarChanged(DeepCore.Game3D.Slave.Layer.LayerUnit unit, string skin, string[] avatar)
            {
                if (this.Spine != null)
                {
                    Spine.initialSkinName = skin;
                    Spine.SetAvatar(avatar);
                }
            }
            protected override void Disposing()
            {
                base.Disposing();
                Playables.Clear();
                Spine = null;
                Animator = null;
                Animation = null;
            }
            protected virtual bool TryPlaySpine(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                if (this.Spine != null)
                {
                    Spine.playing = true;
                    Spine.loop = loop;
                    Spine.speed = speed;
                    if (!string.IsNullOrEmpty(StateName) && Spine.HasAnimation(StateName))
                    {
                        Spine.AnimationName = StateName;
                        return true;
                    }
                    else
                    {
                        //   Spine.playing = false;
                    }
                    return true;
                }
                return false;
            }
            protected virtual bool TryPlayPlayable(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                if (!string.IsNullOrEmpty(StateName) && Playables != null && Playables.TryGetValue(StateName, out var playable))
                {
                    foreach (var p in Playables.Values)
                    {
                        if (p != playable)
                        {
                            p.gameObject.SetActive(false);
                            p.Stop();
                        }
                    }
                    playable.gameObject.SetActive(true);
                    playable.Play();
                    return true;
                }
                return false;
            }
            protected virtual bool TryPlayAnimator(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                if (this.Animator)
                {
                    try
                    {
                        this.Animator.enabled = true;
                        this.Animator.speed = speed;
                        //if (status?.CurrentAction?.DefaultState != null)
                        {
                            string layerName = status?.LayerName;
                            float layerWeight = status?.LayerWeight ?? -1f;
                            var layer = -1;
                            if (!string.IsNullOrEmpty(layerName))
                            {
                                layer = this.Animator.GetLayerIndex(layerName);
                            }
                            if (layerWeight >= 0 && layer >= 0)
                            {
                                this.Animator.SetLayerWeight(layer, layerWeight);
                            }
                            if (!string.IsNullOrEmpty(StateName))
                            {
                                if (NormalizeTime > 0)
                                {
                                    this.Animator.CrossFade(StateName, NormalizeTime, layer);
                                }
                                else
                                {
                                    this.Animator.Play(StateName, layer);
                                }
                            }
                        }
                        if (status?.CurrentAction?.SubStates != null)
                        {
                            foreach (var action in status.CurrentAction.SubStates)
                            {
                                string layerName = action.LayerName;
                                float layerWeight = action.LayerWeight;
                                var layer = -1;
                                if (!string.IsNullOrEmpty(layerName))
                                {
                                    layer = this.Animator.GetLayerIndex(layerName);
                                }
                                if (layerWeight >= 0 && layer >= 0)
                                {
                                    this.Animator.SetLayerWeight(layer, layerWeight);
                                }
                                if (!string.IsNullOrEmpty(action.StateName))
                                {
                                    if (NormalizeTime > 0)
                                    {
                                        this.Animator.CrossFade(action.StateName, NormalizeTime, layer);
                                    }
                                    else
                                    {
                                        this.Animator.Play(action.StateName, layer);
                                    }
                                }
                            }
                        }
                        if (status?.CurrentAction?.ActionParams != null)
                        {
                            foreach (var param in status.CurrentAction.ActionParams)
                            {
                                if (string.IsNullOrEmpty(param.ParamName) == false)
                                {
                                    switch (param.ValueType)
                                    {
                                        case UnitActionDefinitionMap.UnitActionKeyFrame.ParamType.Boolean:
                                            Animator.SetBool(param.ParamName, param.BoolValue); break;
                                        case UnitActionDefinitionMap.UnitActionKeyFrame.ParamType.Float:
                                            Animator.SetFloat(param.ParamName, param.FloatValue); break;
                                        case UnitActionDefinitionMap.UnitActionKeyFrame.ParamType.Integer:
                                            Animator.SetInteger(param.ParamName, param.IntValue); break;
                                    }
                                }
                            }
                        }
                        if (status?.CurrentAction?.ActionTriggers != null)
                        {
                            foreach (var trigger in status.CurrentAction.ActionTriggers)
                            {
                                if (string.IsNullOrEmpty(trigger.TriggerName) == false)
                                {
                                    if (trigger.Enable)
                                    {
                                        this.Animator.SetTrigger(trigger.TriggerName);
                                    }
                                    else
                                    {
                                        this.Animator.ResetTrigger(trigger.TriggerName);
                                    }
                                }
                            }
                        }
                        //                         else
                        //                         {
                        //                             this.Animator.enabled = false;
                        //                         }
                    }
                    catch (Exception err)
                    {
                        Debug.LogWarning($"{StateName} : {err.Message}");
                    }
                    return true;
                }
                return false;
            }
            protected virtual bool TryPlayAnimation(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                if (this.Animation)
                {
                    if (!string.IsNullOrEmpty(StateName) && this.Animation[StateName] is AnimationState st)
                    {
                        try
                        {
                            this.Animation.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                            if (NormalizeTime > 0)
                            {
                                this.Animation.Play(StateName);
                                this.Animation.CrossFade(StateName, NormalizeTime);
                            }
                            else
                            {
                                this.Animation.Play(StateName);
                            }
                            st.speed = speed;
                            return true;
                        }
                        catch (Exception err)
                        {
                            Debug.LogWarning($"{StateName} : {err.Message}");
                        }
                    }
                    else
                    {
                        this.Animation.Stop();
                    }
                    return true;
                }
                return false;
            }
            protected virtual bool TryPlayAssets(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                if (assets.AssetsTemplate.PlayAnim(assets, StateName, loop, speed))
                {
                    return true;
                }
                return false;
            }
            protected virtual bool PlayInternal(UnityActionStatus status, string StateName, float speed, bool loop, float NormalizeTime)
            {
                try
                {
                    if (TryPlaySpine(status, StateName, speed, loop, NormalizeTime)) return true;
                    if (TryPlayPlayable(status, StateName, speed, loop, NormalizeTime)) return true;
                    if (TryPlayAnimator(status, StateName, speed, loop, NormalizeTime)) return true;
                    if (TryPlayAnimation(status, StateName, speed, loop, NormalizeTime)) return true;
                    if (TryPlayAssets(status, StateName, speed, loop, NormalizeTime)) return true;
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                }
                return false;
            }
            public override void PlayAnim(string actionName, float speed, bool loop)
            {
                PlayInternal(null, actionName, speed, loop, 0);
            }
            public override void PlayAnim(UnitActionStatus main, string StateName, float speed, bool loop)
            {
                PlayInternal(null, StateName, speed, loop, 0);
            }
            public override void PlayAnim(UnityActionStatus state)
            {
                PlayInternal(state, state.StateName, state.Speed, state.IsLoop, state.NormalizeTime);
            }
            public virtual void StopAnim()
            {
                try
                {
                    //                     if (this.Spine != null)
                    //                     {
                    //                         this.Spine.playing = false;
                    //                     }
                    if (this.Animator)
                    {
                        this.Animator.enabled = false;
                    }
                    if (this.Animation)
                    {
                        this.Animation.Stop();
                    }
                    if (Playables != null)
                    {
                        foreach (var p in Playables.Values)
                        {
                            p.gameObject.SetActive(false);
                            p.Stop();
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                }
            }
            public virtual void PauseAnim()
            {
                if (this.Animator)
                {
                    this.Animator.enabled = false;
                }
                if (this.Animation)
                {
                    this.Animation.enabled = false;
                }
            }
            public virtual void ResumeAnim()
            {
                if (this.Animator)
                {
                    this.Animator.enabled = true;
                }
                if (this.Animation)
                {
                    this.Animation.enabled = true;
                }
            }
            public virtual void SpeedChange(UnityActionStatus state)
            {
                try
                {
                    if (this.Animator)
                    {
                        this.Animator.speed = state.Speed;
                    }
                    if (this.Animation)
                    {
                        var st = this.Animation[state.StateName];
                        if (st != null)
                        {
                            st.speed = state.Speed;
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                }
            }
            public virtual bool TryGetAnimatorStateDuriationMS(string name, out float timeMS)
            {
                if (Animator)
                {
                    timeMS = Animator.GetAnimatorStateDuriationMS(name);
                    return true;
                }
                if (Animation)
                {
                    timeMS = Animation.GetAnimationStateDuriationMS(name);
                    return true;
                }
                timeMS = 0;
                return false;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleSpellRes : SimpleObjectRes<UnityZoneSpell>, ISpellResourceObject
        {
            public UnityZoneSpell spell { get => base.layerObj; }
            public Transform Bone1 { get; set; }
            public Transform Bone2 { get; set; }
            public virtual SimpleSpellRes Init(UnityZoneSpell spell, IWrapAssetsGO res)
            {
                this.InitWrap(spell, res);
                var layerSpell = spell.layerSpell;
                {
                    if (!string.IsNullOrEmpty(layerSpell.Info.BonesBegin))
                    {
                        Bone1 = res.transform.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == layerSpell.Info.BonesBegin));
                    }
                    if (!string.IsNullOrEmpty(layerSpell.Info.BonesEnd))
                    {
                        Bone2 = res.transform.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == layerSpell.Info.BonesEnd));
                    }
                    foreach (var p in pss)
                    {
                        p.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        p.Simulate(0, true, true);
                        p.Play();
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
                this.UpdateSpellBones();
                {
                    if (transform.localPosition != beginLocalPosition)
                    {
                        this.transform.localPosition = beginLocalPosition;
                    }
                    if (transform.localRotation != beginLocalRotation)
                    {
                        this.transform.localRotation = beginLocalRotation;
                    }
                    {
                        var scale = this.beginLocalScale;
                        if (layerSpell.ResourceScale != 1f && layerSpell.ResourceScale != 0)
                        {
                            scale *= layerSpell.ResourceScale;
                        }
                        if (layerSpell.Info.FileBodyScale != 1f && layerSpell.Info.FileBodyScale != 0)
                        {
                            scale *= layerSpell.Info.FileBodyScale;
                        }
                        if (this.transform.localScale != scale)
                        {
                            this.transform.localScale = scale;
                        }
                    }

                }
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleItemRes : SimpleObjectRes<UnityZoneItem>, IItemResourceObject
        {
            public UnityZoneItem item => base.layerObj;
            public virtual SimpleItemRes Init(UnityZoneItem item, IWrapAssetsGO wrap)
            {
                this.InitWrap(item, wrap);
                var ares = item.layerItem.AResource;
                {
                    var scale = item.layerItem.AResource.BodyScale;
                    var bodyH = item.layerObject.BodyHeight;
                    var va = ares.BodyVoxelAnchor;
                    var space3D = item.Space;
                    var v = space3D.BattleToUnityVoxelAnchorOffset(bodyH, va);
                    this.beginLocalPosition = v;
                    this.beginLocalScale = new Vector3(scale, scale, scale);
                }
                {
                    foreach (var p in pss)
                    {
                        p.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        p.Simulate(0, true, true);
                        p.Play();
                    }
                }
                return this;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleFlagRes : SimpleObjectRes<UnityZoneFlag>, IFlagResourceObject
        {
            public UnityZoneFlag flag => base.layerObj;
            public virtual SimpleFlagRes Init(UnityZoneFlag flag, IWrapAssetsGO wrap)
            {
                this.InitWrap(flag, wrap);
                {
                    if (transform.TryGetComponent<Animator>(out var animator))
                    {
                        animator.applyRootMotion = false;
                    }
                    if (transform.TryGetComponent<Animation>(out var animation))
                    {
                        animation.wrapMode = WrapMode.Loop;
                    }
                    {
                        foreach (var p in pss)
                        {
                            p.scalingMode = ParticleSystemScalingMode.Hierarchy;
                            p.Simulate(0, true, true);
                            p.Play();
                        }
                    }
                    this.beginLocalPosition += flag.ResourceOffset;
                }
                return this;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public class SimpleEffectRes : SimpleObjectRes, IEffectResourceObject
        {
            public UnityEffectPlay player { get; private set; }
            public virtual SimpleEffectRes Init(UnityEffectPlay effect, IWrapAssetsGO wrap)
            {
                this.InitWrap(wrap);
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
                    if (owner is UnityZoneUnit ownerUnit && !string.IsNullOrEmpty(effect.BindPartName))
                    {
                        if (!owner.IsDisposing)
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
                    if (effect.ResTransform != null)
                    {
                        var tx = effect.ResTransform;
                        this.beginLocalPosition = tx.localPosition.ToUnity();
                        this.beginLocalRotation = Quaternion.Euler(tx.localEuler.ToUnity());
                        this.beginLocalScale = tx.localScale.ToUnity();
                    }
                }
                InternalPlay();
            }
            public virtual void PlayAnim(LaunchEffect effect)
            {
                assets.AssetsTemplate.PlayAnim(assets, effect.AnimName, effect.IsLoop, effect.TimeScale);
            }
            protected virtual void InternalPlay()
            {
                pss.PlayParticle();
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

        #endregion --------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------
    }
    //-----------------------------------------------------------------------------------------------------
    public class SimpleBattleCamera : IBattleCamera
    {
        public Camera camera { get; }
        public Transform transform { get => camera.transform; }
        protected BaseCamera baseCamera;
        protected WowFreeCamera free;
        protected WowActorCamera actor;
        public SimpleBattleCamera(UnityZone zone, Camera camera)
        {
            this.camera = camera;
            this.free = camera.GetOrAddComponent<WowFreeCamera>();
            this.actor = camera.GetOrAddComponent<WowActorCamera>();
            this.actor.enabled = false;
            if (camera.TryGetComponent<BaseCamera>(out var baseC))
            {
                this.baseCamera = baseC;
            }
        }
        public virtual void UpdateCamera() { }
        public virtual void Cleanup()
        {

        }
        public virtual void MoveTo(Transform target)
        {
            camera.transform.position = target.position;
        }
        public virtual void LookAt(Transform target)
        {
            camera.transform.LookAt(target);
        }
        public virtual void BindActor(UnityZoneActor actor)
        {
            if (camera.TryGetComponent<WowActorCamera>(out var wowCamera))
            {
                wowCamera.enabled = true;
                wowCamera.BindActor(actor.parent);
                if (camera.TryGetComponent<WowFreeCamera>(out var freeCamera))
                {
                    freeCamera.enabled = false;
                }
            }
            else
            {
                if (camera.TryGetComponent<WowFreeCamera>(out var freeCamera))
                {
                    freeCamera.enabled = true;
                }
            }
        }
        public virtual void Focus(UnityLayerObject unit)
        {
        }
        public virtual void ResetFromTransform()
        {
            this.baseCamera.ResetFromTransform();
        }
        public virtual void Control(string name)
        {

        }
    }
    //-----------------------------------------------------------------------------------------------------
}
