using DeepCore;
using DeepCore.Components;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Reflection;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------------------
    [Reflectible]
    public partial class UnityBattleFactory
    {
        public static Properties CommandLineArgs { get; } = new Properties();
        static UnityBattleFactory()
        {
            var args = Environment.GetCommandLineArgs();
            UnityBattleFactory.CommandLineArgs = Properties.ParseArgs(args);
        }
        //-----------------------------------------------------------------------------------------------------------------

        private ComponentCollection<UnityBattleFactory, BattleFactoryComponent> components;
        //private SingleThreadCollectionPool resource_pool = new();
        public static UnityBattleFactory Instance { get; private set; }
        public static SingleThreadCollectionPool ObjectPool => Instance.loading_task_queue;
        //public static SingleThreadCollectionPool ObjectPool => Instance.resource_pool;
        private SingleThreadCollectionPool loading_task_queue = new SingleThreadCollectionPool();
        public ComponentCollection<UnityBattleFactory, BattleFactoryComponent> Components => components;
        public string RootPath { get; set; } = Application.dataPath;
        public UnityBattleFactory(string root)
        {
            Instance = this;
            this.RootPath = root;
            this.components = new ComponentCollection<UnityBattleFactory, BattleFactoryComponent>(this);
            this.InitializeComponents();
            this.OnInit();
        }
        public void CleanAssets()
        {
            OnCleanAssets();
            components.ForEach(this, static (st, c) => c.CleanAssets());
            this.loading_task_queue.Clear();
        }
        public void LowMemory()
        {
            OnLowMemory();
            components.ForEach(this, static (st, c) => c.LowMemory());
            this.loading_task_queue.Clear();
        }
        protected virtual void OnInit() { }
        protected virtual void OnCleanAssets() { }
        protected virtual void OnLowMemory() { }
        //-----------------------------------------------------------------------------------------------------------------
        #region BattleObject

        public virtual UnityZone CreateBattle() { return new UnityZone(); }
        public virtual void BeginBattle(UnityZone zone, SceneData sceneData) { }
        public virtual void EndBattle(UnityZone zone) { }

        public virtual UnityZoneObject CreateZoneObject(UnityZone zone, LayerZoneObject obj)
        {
            if (obj is LayerPlayer player)
            {
                return new UnityZoneActor(zone);
            }
            else if (obj is LayerUnit unit)
            {
                return new UnityZoneUnit(zone);
            }
            else if (obj is LayerSpell spell)
            {
                return new UnityZoneSpell(zone);
            }
            else if (obj is LayerItem item)
            {
                return new UnityZoneItem(zone);
            }
            throw new System.Exception($"Unknow layer object : {obj.GetType()}");
        }
        public virtual UnityZoneFlag CreateZoneFlag(UnityZone zone, LayerFlag obj)
        {
            if (obj is LayerEditorDecoration deco)
            {
                return new UnityLayerDecoration(zone);
            }
            if (obj is LayerEditorRegion region)
            {
                return new UnityLayerRegion(zone);
            }
            if (obj is LayerEditorPoint point)
            {
                return new UnityLayerPoint(zone);
            }
            return null;
        }
        public virtual CapsuleCollider CreateUnitCollider(UnityZoneUnit unit)
        {
            return unit.gameObject.AddComponent<CapsuleCollider>();
        }
        public virtual Collider CreateFlagCollider(UnityZoneFlag flag)
        {
            if (flag is UnityLayerRegion rg || flag is UnityLayerDecoration dc)
            {
                var c = flag.gameObject.AddComponent<CapsuleCollider>();
                {
                    var data = flag.layerFlag.EditorData;
                    c.height = data.Height;
                    c.radius = data.Radius;
                    c.center = new Vector3(0, c.height * 0.5f, 0);
                }
                return c;
            }
            return null;
        }

        public virtual HUDUnitHPBar CreateHUDUnitHPBar(UnityLayerObject obj)
        {
            return null;
        }
        public virtual HUDDamageText CreateHUDDamageText(UnityZone zone)
        {
            return null;
        }
        public virtual UnityZoneSpaceTransverter CreateZoneSpaceTransverter() => new UnityZoneSpace3D();
        public virtual IBattleCamera CreateBattleCamera(UnityZone zone, Camera camera)
        {
            if (zone.Space is UnityZoneSpace2D)
            {
                return new SimpleBattleCamera2D(zone, camera);
            }
            else
            {
                return new SimpleBattleCamera(zone, camera);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------
    }
    //-----------------------------------------------------------------------------------------------------------------

    //-----------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 加载战斗资源
    /// </summary>
    /// <typeparam name="ST"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="st"></param>
    /// <param name="res"></param>
    /// <param name="error"></param>
    public delegate void BattleResourceLoaderHandler<ST, T>(ST st, T res = default, System.Exception error = null);

    /// <summary>
    /// 加载资产，内部已经池化复用
    /// </summary>
    /// <typeparam name="ST"></typeparam>
    /// <param name="st"></param>
    /// <param name="wrap"></param>
    public delegate void LoadWrapAssetHandler<ST, T>(ST st, T wrap) where T : IWrapAssets;
    public delegate void LoadWrapAssetHandler<ST>(ST st, IWrapAssets wrap);

    /// <summary>
    /// 加载资产
    /// </summary>
    /// <typeparam name="ST"></typeparam>
    /// <param name="st"></param>
    /// <param name="assets"></param>

    public delegate void LoadAssetsHandler<ST>(ST st, IAssetsTemplate assets);

    //-----------------------------------------------------------------------------------------------------------------
    public class TerrainInfo
    {
        public float TerrainH;
        public float TerrainW;
        public float ResX;
        public float ResY;
    }
    public struct AnimInfo
    {
        public string Name;
        public string Action;
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return Action;
            }
            return $"{Name}";
        }
    }
    public interface IAssetsTemplate : IDisposable
    {
        object handler { get; }
        UnityEngine.Object template { get; }
        ResourceType resType { get; set; }
        string resName { get; set; }

        bool PlayEffect(IWrapAssets wrap, string action, bool loop, float speed, Transform binding);
        bool PlayAnim(IWrapAssets wrap, string action, bool loop, float speed);
        bool PlayAction(IWrapAssets wrap, UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action);
        bool PlaySound(IWrapAssets go, ResourceType? restype = null);
        bool TryGetDurationMS(out float ms, out bool loop);
        bool TryListAnims(List<AnimInfo> actions, UnitInfo unit = null);

        //         UnityEngine.Object MakeInstantiate();
        //         void DestoryInstance(UnityEngine.Object ins);

        //         public bool PlayEffect(IWrapAsset wrap, string action, bool loop, float speed, Transform binding)
        //         {
        //             if (wrap.instance is GameObject go)
        //             {
        //                 go.SetParticleEmission(true);
        //                 go.PlayParticle();
        //                 return true;
        //             }
        //             return false;
        //         }
        //         public bool PlayAnim(IWrapAsset wrap, string action, bool loop, float speed)
        //         {
        //             try
        //             {
        //                 if (wrap.instance is GameObject go)
        //                 {
        //                     if (go.TryGetComponentInChildren<Animator>(out var animator))
        //                     {
        //                         if (!string.IsNullOrEmpty(action))
        //                         {
        //                             animator.enabled = true;
        //                             animator.speed = speed;
        //                             try
        //                             {
        //                                 animator.Play(action);
        //                                 return true;
        //                             }
        //                             catch
        //                             {
        //                             }
        //                         }
        //                         else
        //                         {
        //                             animator.enabled = false;
        //                         }
        //                     }
        //                     else if (go.TryGetComponentInChildren<Animation>(out var animation))
        //                     {
        //                         if (!string.IsNullOrEmpty(action))
        //                         {
        //                             try
        //                             {
        //                                 animation.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        //                                 animation.Play(action);
        //                                 var st = animation[action];
        //                                 if (st != null)
        //                                 {
        //                                     st.speed = speed;
        //                                 }
        //                                 return true;
        //                             }
        //                             catch
        //                             {
        //                             }
        //                         }
        //                         else
        //                         {
        //                             animation.Stop();
        //                         }
        //                     }
        //                 }
        //             }
        //             catch (System.Exception err)
        //             {
        //                 Debug.LogError(err);
        //             }
        //             return false;
        //         }
        //         public bool PlayAction(IWrapAsset wrap, UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        //         {
        //             try
        //             {
        //                 if (wrap.instance is GameObject go)
        //                 {
        //                     if (go.TryGetComponentInChildren<Animator>(out var animator))
        //                     {
        //                         if (!string.IsNullOrEmpty(action.ActionName))
        //                         {
        //                             animator.enabled = true;
        //                             animator.speed = action.Speed;
        //                             try
        //                             {
        //                                 if (action.CrossFadeTimeMS > 0)
        //                                 {
        //                                     animator.CrossFade(action.ActionName, action.CrossFadeTimeMS);
        //                                     animator.Play(action.ActionName);
        //                                 }
        //                                 else
        //                                 {
        //                                     animator.Play(action.ActionName);
        //                                 }
        //                                 return true;
        //                             }
        //                             catch
        //                             {
        //                             }
        //                         }
        //                         else
        //                         {
        //                             animator.enabled = false;
        //                         }
        //                     }
        //                     else if (go.TryGetComponentInChildren<Animation>(out var animation))
        //                     {
        //                         if (!string.IsNullOrEmpty(action.ActionName))
        //                         {
        //                             try
        //                             {
        //                                 animation.wrapMode = action.Cycle ? WrapMode.Loop : WrapMode.Once;
        //                                 if (action.CrossFadeTimeMS > 0)
        //                                 {
        //                                     animation.Play(action.ActionName);
        //                                     animation.CrossFade(action.ActionName, action.CrossFadeTimeMS);
        //                                 }
        //                                 else
        //                                 {
        //                                     animation.Play(action.ActionName);
        //                                 }
        //                                 var st = animation[action.ActionName];
        //                                 if (st != null)
        //                                 {
        //                                     st.speed = action.Speed;
        //                                 }
        //                                 return true;
        //                             }
        //                             catch
        //                             {
        //                             }
        //                         }
        //                         else
        //                         {
        //                             animation.Stop();
        //                         }
        //                     }
        //                 }
        //             }
        //             catch (System.Exception err)
        //             {
        //                 Debug.LogError(err);
        //             }
        //             return false;
        //         }
        //         public bool PlaySound(IWrapAsset go, ResourceType? restype = null)
        //         {
        //             System.Console.Beep(1000, 200);
        //             return false;
        //         }
        //         public bool TryGetDurationMS(out float ms, out bool loop)
        //         {
        //             if (template is GameObject res)
        //             {
        //                 return res.TryGetParticleDurationMS(out ms, out loop);
        //             }
        //             ms = 0f;
        //             loop = false;
        //             return false;
        //         }
        //         public void ListAnims(List<AnimInfo> actions, UnitInfo unit = null)
        //         {
        //             var actionMap = default(UnitActionDefinitionMap);//PreviewProxy.TemplateDatas?.DefaultUnitActionDefinitions?.ActionMap;
        //             if (unit != null)
        //             {
        //                 if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
        //                 {
        //                     if (u_res.OverrideActionMap != null)
        //                     {
        //                         actionMap = u_res.OverrideActionMap;
        //                     }
        //                 }
        //                 if (actionMap?.ActionMap != null)
        //                 {
        //                     foreach (var act in actionMap.ActionMap)
        //                     {
        //                         if (act.FirstKeyFrame is UnitActionDefinitionMap.UnitActionKeyFrame kf)
        //                         {
        //                             actions.Add(new AnimInfo() { Name = $"{act.Action}", Action = kf.ActionName });
        //                         }
        //                     }
        //                 }
        //             }
        //             if (template is GameObject res)
        //             {
        //                 if (res.TryGetComponentInChildren<Animation>(out var animation))
        //                 {
        //                     foreach (AnimationState state in animation)
        //                     {
        //                         actions.Add(new AnimInfo() { Name = $"{state.name}", Action = state.name });
        //                     }
        //                 }
        //                 if (res.TryGetComponentInChildren<Animator>(out var animator))
        //                 {
        //                     if (animator?.runtimeAnimatorController?.animationClips != null)
        //                     {
        //                         foreach (var clip in animator.runtimeAnimatorController.animationClips)
        //                         {
        //                             actions.Add(new AnimInfo() { Name = $"{clip.name}", Action = clip.name });
        //                         }
        //                     }
        //                 }
        //             }
        //         }

    }

    //-----------------------------------------------------------------------------------------------------------------

    public class IWrapAssets : Disposable
    {
        internal readonly AssetsLoaderComponent.IWrapAssetsPool owner;
        /// <summary>
        /// 当前资源实例
        /// </summary>
        public UnityEngine.Object instance { get; }
        /// <summary>
        /// 当前资源原始实例
        /// </summary>
        public UnityEngine.Object TemplateObject => owner.AssetsTemplate?.template;
        /// <summary>
        /// 当前资源数据源
        /// </summary>
        public IAssetsTemplate AssetsTemplate => owner.AssetsTemplate;
        public UnityBattleFactory Factory => owner.Factory;
        internal IWrapAssets(AssetsLoaderComponent.IWrapAssetsPool owner, UnityEngine.Object instance)
        {
            this.owner = owner;
            this.instance = instance;
        }
        protected override void Disposing()
        {
            this.owner.Recycle(instance);
        }
    }
    public class IWrapAssetsGO : IWrapAssets
    {
        /// <summary>
        /// 当前资源实例
        /// </summary>
        public GameObject gameObject => instance as GameObject;
        /// <summary>
        /// 当前资源实例
        /// </summary>
        public Transform transform => gameObject?.transform;
        internal IWrapAssetsGO(AssetsLoaderComponent.IWrapAssetsPool owner, GameObject instance) : base(owner, instance)
        {
            var template = base.TemplateObject as GameObject;
            if (template != null && template && instance != null && instance)
            {
                instance.transform.localPosition = template.transform.localPosition;
                instance.transform.localRotation = template.transform.localRotation;
                instance.transform.localScale = template.transform.localScale;
                instance.SetActive(true);
            }
        }
        protected override void Disposing()
        {
            var w = this.gameObject;
            if (w != null && w)
            {
                w.SetActive(false);
                if (!UnityBattleFactory.AssetsLoader.TryStashNode(w, "instance"))
                {
                    w.transform.SetParent(null, false);
                    return;
                }
            }
            base.Disposing();
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    public interface IZoneResource : System.IDisposable
    {
        UnityZone zone { get; }
        void UpdateResource();
        bool Active { get; set; }
    }
    public interface IResourceObject : IAutoRecycle
    {
        IWrapAssetsGO assets { get; }
        Transform transform { get; }
        GameObject gameObject { get; }
        bool TryGetEffectTimeMS(out float ms, out bool loop);
        void SetLayer(int layer);
        void SetLayer(string layer);
        void UpdateResource(UnityPoolingObject obj);
        Transform FindDeep(string name);
        void PlayAnim(string actionName, float speed, bool loop);
    }
    public interface IUnitResourceObject : IResourceObject
    {
        UnityZoneUnit unit { get; }
        void PlayAnim(UnitActionStatus main, string actionName, float speed, bool loop);
        void PlayAnim(UnityZoneUnit.UnityActionStatus state);
        void StopAnim();
        void PauseAnim();
        void ResumeAnim();
        void SpeedChange(UnityZoneUnit.UnityActionStatus state);
        bool TryGetAnimatorStateDuriationMS(string name, out float timeMS);
    }
    public interface ISpellResourceObject : IResourceObject
    {
        UnityZoneSpell spell { get; }
        Transform Bone1 { get; }
        Transform Bone2 { get; }
    }
    public interface IItemResourceObject : IResourceObject
    {
        UnityZoneItem item { get; }
    }
    public interface IFlagResourceObject : IResourceObject
    {
        UnityZoneFlag flag { get; }
    }
    public interface IEffectResourceObject : IResourceObject
    {
        UnityEffectPlay player { get; }
        void PlayAnim(LaunchEffect effect);
    }

    //-----------------------------------------------------------------------------------------------------------------

}
