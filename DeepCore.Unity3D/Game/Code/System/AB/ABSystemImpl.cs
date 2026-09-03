using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Code.System.Pool;
using Code.System.World;
using DeepCore.IO;
using DeepCore.MPQ;
using DeepCore.Unity3D.Impl;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Code.System.AB
{
    internal class ABSystemImpl : SingleSystem<ABSystemImpl>
    {
        public const string PREFIX_FILE = "file://";
        public const string PREFIX_MPQ = "mpq://";

        public static string CDN { get; set; } = string.Empty;
        public IResourceLoader DefaultLoader { get => UnityDriver.UnityInstance; }

        private WrapAsset<AssetBundleManifest> _manifest;
        private readonly Dictionary<string, string[]> _depsSnap = new Dictionary<string, string[]>();
        private readonly Dictionary<string, LoaderBundle> _bundleSnap = new Dictionary<string, LoaderBundle>();
        private readonly Dictionary<string, LoaderAsset> _assetSnap = new Dictionary<string, LoaderAsset>();
        private readonly LinkedList<LoaderBundle> _bundleLoadings = new LinkedList<LoaderBundle>();
        private readonly LinkedList<LoaderAsset> _assetLoadings = new LinkedList<LoaderAsset>();
        private readonly LinkedList<LoaderBundle> _bundleUnloadings = new LinkedList<LoaderBundle>();
        private readonly LinkedList<LoaderAsset> _assetUnloadings = new LinkedList<LoaderAsset>();

        public void Samples()
        {
            var url = "/res/unit/heroaxe.assetbundles";
            var wrap = GetBundle(url);
            wrap.Dispose();
            GetBundleAsync(url, bundle =>
            {
                wrap = bundle;
            });
            var asset = GetAsset<GameObject>(url, "HeroAxe");
            GetAssetAsync<GameObject>(url, "HeroAxe", wrapAsset =>
            {
                asset = wrapAsset;
            });
        }

        public void SetManifestBundleName(string file, string name)
        {
            try
            {
                _manifest = GetAsset<AssetBundleManifest>(file, name);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
        protected override void OnCreate()
        {
        }

        internal protected virtual bool TryGetFullPath(string path, out string fullPath)
        {
            if (File.Exists(path))
            {
                fullPath = path;
                return true;
            }
            path = $"{ABSystem.RootPath}{path}";
            if (File.Exists(path))
            {
                fullPath = path;
                return true;
            }
            fullPath = null;
            return false;
        }
        internal protected virtual bool TryOpenData(string fullPath, out byte[] stream)
        {
            stream = UnityDriver.UnityInstance.LoadData(fullPath);
            return stream != null;
        }

        internal string[] GetDeps(string url)
        {
            if (_depsSnap.TryGetValue(url, out var deps)) return deps;
            if (_manifest == null) return Array.Empty<string>();
            deps = _manifest.Asset.GetDirectDependencies(url.Substring(5));
            _depsSnap.Add(url, deps);
            return deps;
        }

        internal LoaderBundle GetBundleLoader(string url)
        {
            if (_bundleSnap.TryGetValue(url, out var loader)) return loader;
            loader = ObjectPool<LoaderBundle>.Get();
            _bundleSnap.Add(url, loader);
            loader.Init(url);
            loader.IsInLoadingQueue = true;
            _bundleLoadings.AddLast(loader);

            return loader;
        }

        public WrapBundle GetBundle(string url)
        {
            //url = url.ToLower();
            var loader = GetBundleLoader(url);
            loader.Retain();
            loader.LoadImmediate();
            return loader.GetWrap();
        }

        public void GetBundleAsync(string url, Action<WrapBundle> callback)
        {
            //url = url.ToLower();
            var loader = GetBundleLoader(url);
            loader.Retain();
            if (!loader.IsInLoadingQueue)
            {
                loader.IsInLoadingQueue = true;
                _bundleLoadings.AddLast(loader);
            }

            loader.Completed += callback;
        }

        internal void Release(LoaderBundle loader)
        {
            loader.Release();
            if (loader.RefCount <= 0)
            {
                if (loader.RefCount < 0)
                {
                    Debug.LogError("error release count !");
                }

                if (loader.IsInLoadingQueue)
                {
                    loader.IsInLoadingQueue = false;
                    _bundleLoadings.Remove(loader);
                }

                if (!loader.IsInUnloadingQueue)
                {
                    loader.IsInUnloadingQueue = true;
                    _bundleUnloadings.AddLast(loader);
                }
            }
        }


        private LoaderAsset GetAssetLoader(string bundleUrl, string assetName, Type type)
        {
            var key = LoaderAsset.ToKey(bundleUrl, assetName);
            if (_assetSnap.TryGetValue(key, out var loader)) return loader;

            loader = ObjectPool<LoaderAsset>.Get();
            loader.Init(bundleUrl, assetName, type);
            _assetSnap.Add(loader.Key, loader);
            loader.IsInLoadingQueue = true;
            _assetLoadings.AddLast(loader);

            return loader;
        }

        public WrapAsset<T> GetAsset<T>(string bundleUrl, string assetName) where T : Object
        {
            if (TryGetFullPath(bundleUrl, out var fullPath))
            {
                var type = typeof(T);
                var loader = GetAssetLoader(fullPath, assetName, type);
                loader.Retain();
                loader.LoadImmediate();
                return loader.GetWrap<T>();
            }
            return null;
        }

        public void GetAssetAsync<T>(string bundleUrl, string assetName, Action<WrapAsset<T>> callback) where T : Object
        {
            if (TryGetFullPath(bundleUrl, out var fullPath))
            {
                var type = typeof(T);
                var loader = GetAssetLoader(bundleUrl, assetName, type);
                loader.Retain();
                if (!loader.IsInLoadingQueue)
                {
                    loader.IsInLoadingQueue = true;
                    _assetLoadings.AddLast(loader);
                }

                loader.Completed += (assetLoader) => { callback(assetLoader.GetWrap<T>()); };
            }
            else
            {
                callback(null);
            }
        }

        internal void Release(LoaderAsset loader)
        {
            if (!_assetSnap.ContainsKey(loader.Key)) return;
            loader.Release();
            if (loader.RefCount <= 0)
            {
                if (loader.IsInLoadingQueue)
                {
                    _assetLoadings.Remove(loader);
                }
                _assetUnloadings.AddLast(loader);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_bundleUnloadings.Count > 0)
            {
                var head = _bundleUnloadings.First;
                while (head != null)
                {
                    var node = head;
                    head = head.Next;

                    var value = node.Value;
                    value.IsInUnloadingQueue = false;
                    _bundleUnloadings.Remove(node);
                    if (value.RefCount == 0)
                    {
                        _bundleSnap.Remove(value.Url);
                        if (value.IsInLoadingQueue)
                        {
                            _bundleLoadings.Remove(value);
                        }
                        value.Dispose();
                    }
                }
            }

            if (_bundleLoadings.Count > 0)
            {
                var head = _bundleLoadings.First;
                while (head != null)
                {
                    var node = head;
                    head = head.Next;

                    var value = node.Value;
                    if (value.Update())
                    {
                        value.IsInLoadingQueue = false;
                        _bundleLoadings.Remove(node);
                    }
                }
            }

            if (_assetUnloadings.Count > 0)
            {
                var head = _assetUnloadings.First;
                while (head != null)
                {
                    var node = head;
                    head = head.Next;

                    var value = node.Value;
                    value.IsInUnloadingQueue = false;
                    _assetUnloadings.Remove(node);
                    if (value.RefCount == 0)
                    {
                        _assetSnap.Remove(value.Key);
                        if (value.IsInLoadingQueue)
                        {
                            _assetLoadings.Remove(value);
                        }
                        value.Dispose();
                    }
                }
            }

            if (_assetLoadings.Count > 0)
            {
                var head = _assetLoadings.First;
                while (head != null)
                {
                    var node = head;
                    head = head.Next;

                    var value = node.Value;
                    if (value.Update())
                    {
                        value.IsInLoadingQueue = false;
                        _assetLoadings.Remove(node);
                    }
                }
            }
        }

        public string GetAvatarUrl(string uuid)
        {

            return CDN + uuid;
        }


        public string GetResUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (url.StartsWith("/res/") && url.EndsWith(".png"))
            {
                url = $"{ABSystem.RootPath}{url}";
            }
            return url;
        }
    }
}