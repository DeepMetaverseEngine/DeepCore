using DeepCore;
using DeepCore.Components;
using DeepCore.Unity;
using DeepCore.Unity.ResourceViewer;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{
    public  class UnityPreviewFactory
    {
        public static UnityPreviewFactory Instance { get; private set; } = new UnityPreviewFactory();
        public UnityPreviewFactory()
        {
            Instance = this;
        }
        public virtual IViewResource LoadViewResource(object sender, string resName, ResourceType resType, IResourceProperties resProp)
        {
            if (string.IsNullOrEmpty(resName))
            {
                return null;
            }
            var wrap = UnityBattleFactory.AssetsLoader.GetAssetObject(resName, resType);
            if (wrap != null)
            {
                if (wrap is IWrapAssetsGO go)
                {
                    return new DefaultViewRes(sender, resName, go, resType);
                }
                else
                {
                    wrap.Dispose();
                }
            }
            return null;
        }
    }
    public interface IViewResource : System.IDisposable
    {
        GameObject gameObject { get; }
        Transform transform { get; }
        bool IsVisible { get; set; }
        ResourceType resType { get; }
        string resName { get; }
        void PlayEffect(string action, bool loop, float speed, Transform binding);
        void PlayAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action);
        void StopAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action);
        void PlaySound(ResourceType? restype = null);
        bool BindBody(IViewResource parent, string partName = null);
        void UpdateResource(GameObject obj);
        void PlayAnim(string action = null);
        bool TryGetDurationMS(out float duration, out bool loop);
        bool TryListAnims(List<AnimInfo> actions, UnitInfo unit = null);

    }
    public class DummyViewRes : Disposable, IViewResource
    {
        protected readonly GameObject go;
        public GameObject gameObject => go;
        public Transform transform => go.transform;
        public virtual ResourceType resType { get; }
        public virtual string resName { get; }
        public bool IsVisible
        {
            get => go.activeSelf;
            set => go.SetActive(value);
        }
        public DummyViewRes(object sender, string name, ResourceType resType)
        {
            this.resName = name;
            this.resType = resType;
            this.go = new GameObject(name);
            if (sender?.AsTransform() is Transform parent)
            {
                this.go.transform.SetParent(parent.transform, false);
            }
        }
        protected override void Disposing()
        {
            GameObject.Destroy(go);
        }
        public virtual bool BindBody(IViewResource parent, string partName)
        {
            if (!string.IsNullOrEmpty(partName))
            {
                var part = parent.transform.FindDeep(partName);
                if (part)
                {
                    this.transform.SetParent(part, false);
                    return true;
                }
            }
            this.transform.SetParent(parent.transform, false);
            return false;
        }
        public virtual bool TryGetDurationMS(out float duration, out bool loop)
        {
            duration = 0f;
            loop = false;
            return false;
        }
        public virtual bool TryListAnims(List<AnimInfo> actions, UnitInfo unit)
        {
            return false;
        }
        public virtual void PlayEffect(string action, bool loop, float speed, Transform binding)
        {
        }
        public virtual void PlayAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
        }
        public virtual void StopAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
        }
        public virtual void PlayAnim(string action)
        {
        }
        public virtual void PlaySound(ResourceType? restype)
        {
        }
        public virtual void UpdateResource(GameObject obj)
        {

        }
    }
    public class DefaultViewRes : DummyViewRes
    {
        protected readonly IWrapAssetsGO assets;
        protected readonly List<ParticleSystem> pss = new List<ParticleSystem>();
        public DefaultViewRes(object sender, string name, IWrapAssetsGO wrap, ResourceType resType) : base(sender, name, resType)
        {
            this.assets = wrap;
            this.assets.gameObject.name = name;
            this.assets.gameObject.transform.SetParent(this.go.transform, false);
            var wres = wrap.gameObject.GetOrAddComponent<ResourceInfo>();
            wres.Refresh();
        }
        protected virtual bool PlayInternal(string StateName, float speed, bool loop, float NormalizeTime)
        {
            try
            {
                if (gameObject)
                {
                    if (assets.AssetsTemplate.PlayAnim(assets, StateName, loop, speed))
                    {
                        return true;
                    }
                    else if (DefaultAssetsTuple.PLAY_PREVIEW(assets, StateName, speed, loop, NormalizeTime))
                    {
                        return true;
                    }
                }
            }
            catch (System.Exception err)
            {
                Debug.LogError(err);
            }
            return false;
        }
        protected override void Disposing()
        {
            assets?.Dispose();
            base.Disposing();
        }

        public override bool TryGetDurationMS(out float duration, out bool loop)
        {
            return assets.AssetsTemplate.TryGetDurationMS(out duration, out loop);
        }
        public override bool TryListAnims(List<AnimInfo> actions, UnitInfo unit)
        {
            return assets.AssetsTemplate.TryListAnims(actions, unit);
        }
        public override void PlayEffect(string action, bool loop, float speed, Transform binding)
        {
            assets.AssetsTemplate.PlayAnim(assets, action, loop, speed);
            assets.gameObject.SetParticleEmission(true);
            assets.gameObject.PlayParticle();
        }
        public override void PlayAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
            PlayInternal(action.ActionName, action.Speed, action.Cycle, action.CrossFadeTimeMS);
        }
        public override void StopAction(UnitActionStatus status, UnitActionDefinitionMap.UnitActionKeyFrame action)
        {
            //PlayInternal(action.ActionName, action.Speed, action.Cycle, action.CrossFadeTimeMS);
        }
        public override void PlayAnim(string action)
        {
            PlayInternal(action, 1, true, 0);
        }
        public override void PlaySound(ResourceType? restype)
        {
            if (assets.AssetsTemplate.PlaySound(assets, restype))
            {

            }
        }
        public override void UpdateResource(GameObject obj)
        {

        }
    }
}
