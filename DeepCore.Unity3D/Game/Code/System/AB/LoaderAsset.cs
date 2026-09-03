using System;
using System.Collections.Generic;
using Code.System.Pool;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity;
using UnityEngine;
using Object = UnityEngine.Object;
using DeepCore.Unity.AB;
using DeepCore.IO;
using System.IO;
using DeepCore.Unity3D.Impl;

namespace Code.System.AB
{
    internal sealed class LoaderAsset : Loader, IPoolable
    {
        public string BundleUrl { get; private set; }
        public string AssetName { get; private set; }
        public Type Type { get; private set; }
        public string Key => ToKey(BundleUrl, AssetName);
        public WrapBundle BundleWrap { get; private set; }
        public Action<LoaderAsset> Completed { get; set; }
        public Object Asset { get; private set; }

        public static string ToKey(string bundleUrl, string assetName)
        {
            return $"{bundleUrl}__{assetName}";
        }
        public LoaderAsset Init(string bundleUrl, string assetName, Type type)
        {
            if (Status != LoaderStatus.Invalid)
            {
                Debug.LogError("invalid loader");
                return null;
            }

            BundleUrl = bundleUrl;
            AssetName = assetName;
            Type = type;

            Status = LoaderStatus.Inited;
            return this;
        }
        private void InitAsset(Object asset, Object[] assets)
        {
            Asset = asset;
            try
            {
                if (DeepCore.IO.Resource.TryLoadData(BundleWrap.FileName + ".fuck", out var fuckData))
                {
                    if (asset is GameObject go && go.TryGetComponentsInChildren<MeshRenderer>(out var renders, true))
                    {
                        var fuckText = File.ReadAllText(DeepCore.CUtils.DecodeUTF8(fuckData));
                        var fuckProp = DeepCore.Properties.ParseText(fuckText);
                        foreach (var render in renders)
                        {
                            if (fuckProp.TryGetValue(render.gameObject.name, out var mname))
                            {
                                try
                                {
                                    var m = this.BundleWrap.Bundle.LoadAsset<Material>(mname);
                                    render.material = m;
                                }
                                catch (Exception err)
                                {
                                    Debug.LogError(err);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err);
            }
        }
        public void LoadImmediate()
        {
            if (Status != LoaderStatus.Completed)
            {
                BundleWrap = ABSystemImpl.Inst.GetBundle(BundleUrl);
                if (BundleWrap != null)
                {
                    var assets = BundleWrap.Bundle.LoadAssetWithSubAssets(AssetName, Type);
                    if (assets.Length > 0)
                    {
                        InitAsset(assets[0], assets);
                    }
                }
                Status = LoaderStatus.Completed;
            }
            if (!Asset)
            {
                Debug.LogWarning($"LoadImmediate Error : AssetName={AssetName} BundleUrl={BundleUrl}");
            }
            if (Completed != null)
            {
                Completed.Invoke(this);
                Completed = null;
            }
        }

        public WrapAsset GetWrap()
        {
            if (!Asset) return null;
            var wrap = ObjectPool<WrapAsset>.Get();
            wrap.AssetLoader = this;
            return wrap;
        }

        public WrapAsset<T> GetWrap<T>() where T : Object
        {
            if (!Asset) return null;
            var wrap = ObjectPool<WrapAsset<T>>.Get();
            wrap.AssetLoader = this;
            return wrap;
        }

        private void Start()
        {
            if (Status != LoaderStatus.Inited)
            {
                Debug.LogError("error status");
            }
            else
            {
                Status = LoaderStatus.Started;
                ABSystemImpl.Inst.GetBundleAsync(BundleUrl, OnBundleCompleted);
            }
        }

        public bool Update()
        {
            if (Status == LoaderStatus.Completed)
            {
                if (Completed != null)
                {
                    Completed.Invoke(this);
                    Completed = null;
                }
                return true;
            }
            if (Status == LoaderStatus.Inited)
            {
                Start();
            }
            return false;

        }
        private void OnBundleCompleted(WrapBundle wrap)
        {
            BundleWrap = wrap;
            if (BundleWrap != null && BundleWrap.Bundle)
            {
                var op = BundleWrap.Bundle.LoadAssetAsync(AssetName, Type);
                op.completed += OnAssetCompleted;
            }
            else
            {
                OnAssetCompleted(null);
            }
        }

        private void OnAssetCompleted(AsyncOperation op)
        {
            Status = LoaderStatus.Completed;
            if (op is AssetBundleRequest request)
            {
                InitAsset(request.asset, request.allAssets);
            }


            if (Completed != null)
            {
                Completed.Invoke(this);
                Completed = null;
            }
        }

        protected override void Disposing()
        {
            ObjectPool<LoaderAsset>.Release(this);
        }

        protected override void OnClear()
        {
            base.OnClear();

            if (BundleWrap != null)
            {
                BundleWrap.Dispose();
                BundleWrap = null;
            }

            if (Asset)
            {
                if (!(Asset is GameObject))
                {
                    Resources.UnloadAsset(Asset);
                }
                Asset = null;
            }

            if (Completed != null)
            {
                Completed.Invoke(this);
                Completed = null;
            }

            BundleUrl = null;
            AssetName = null;
            Type = null;
        }
    }
}
