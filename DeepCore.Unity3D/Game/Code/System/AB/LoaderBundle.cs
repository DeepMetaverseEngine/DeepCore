using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.System.Pool;
using DeepCore.Unity;
using DeepCore.Unity.ResourceViewer;
using UnityEngine;

namespace Code.System.AB
{
    internal sealed class LoaderBundle : Loader, IPoolable
    {
        public string Url { get; private set; }
        public Action<WrapBundle> Completed { get; set; }
        public AssetBundle Bundle { get; private set; }

     
        private List<LoaderBundle> _depLoaders = new List<LoaderBundle>();

        public LoaderBundle Init(string url)
        {
            if (Status != LoaderStatus.Invalid)
            {
                Debug.LogError($"invalid loader {url}");
                return null;
            }

            Url = url;
            Status = LoaderStatus.Inited;
            return this;
        }
        private void InitBundle(AssetBundle bundle)
        {
            this.Bundle = bundle;
        }

        public void LoadImmediate()
        {
            if (Status != LoaderStatus.Completed)
            {
                if (ABSystemImpl.Inst.TryOpenData(Url, out var stream))
                {
                    var Bundle = AssetBundle.LoadFromMemory(stream);
                    if (Bundle != null)
                    {
                        InitBundle(Bundle);
                    }
                }
                else
                {
                    throw new Exception($"Cannot load :: RootPath: {ABSystem.RootPath}, URL: {Url}");
                }
                Status = LoaderStatus.Completed;

                LoadDeps();

            }

            if (Completed != null)
            {
                Completed.Invoke(GetWrap());
                Completed = null;
            }
        }

        public WrapBundle GetWrap()
        {
            if (!Bundle) return null;
            var wrap = ObjectPool<WrapBundle>.Get();
            wrap.BundleLoader = this;
            wrap.FileName = Url;
            return wrap;
        }

        private void Start()
        {
            if (Status != LoaderStatus.Inited)
            {
                Debug.LogError("error status: " + Url);
            }
            else if (ABSystemImpl.Inst.TryOpenData(Url, out var stream))
            {
                Status = LoaderStatus.Started;
                LoadDeps();
                var op = AssetBundle.LoadFromMemoryAsync(stream);
                op.completed += OnCompleted;
            }
            else
            {
                Debug.LogError("error stream: " + Url);
            }
        }

        internal bool Update()
        {
            if (Status == LoaderStatus.Completed)
            {
                var depsCompleted = true;
                if (_depLoaders.Count > 0)
                {
                    depsCompleted = _depLoaders.All((loader => loader.Status == LoaderStatus.Completed));
                }

                if (depsCompleted && Completed != null)
                {
                    Completed.Invoke(GetWrap());
                    Completed = null;
                }
                return depsCompleted;
            }

            if (Status == LoaderStatus.Inited)
            {
                Start();
                Status = LoaderStatus.Started;
            }
            return false;
        }

        private void LoadDeps()
        {
            // Debug.Log($"start load deps: {Url}");
            var deps = ABSystemImpl.Inst.GetDeps(Url);
            if (deps.Length > 0)
            {
                foreach (var dep in deps)
                {
                    // Debug.Log($"load {Url} deps: {dep}");
                    var loader = ABSystemImpl.Inst.GetBundleLoader("/res/" + dep);
                    loader.Retain();
                    _depLoaders.Add(loader);
                    loader.LoadImmediate();

                    // if(bAsync) loader.Start();
                    // else loader.LoadImmediate();
                }
            }
        }

        private void OnCompleted(AsyncOperation op)
        {
            Status = LoaderStatus.Completed;
            if (op is AssetBundleCreateRequest request)
            {
                var Bundle = request.assetBundle;
                InitBundle(Bundle);
            }

            if (Completed != null)
            {
                Completed.Invoke(GetWrap());
                Completed = null;
            }
        }

        protected override void Disposing()
        {
            ObjectPool<LoaderBundle>.Release(this);
        }

        protected override void OnClear()
        {
            base.OnClear();

            if (_depLoaders.Count > 0)
            {
                foreach (var loader in _depLoaders)
                {
                    loader.Release();
                }
                _depLoaders.Clear();
            }

            if (Completed != null)
            {
                Completed.Invoke(null);
                Completed = null;
            }

            if (Bundle)
            {
                Bundle.Unload(true);
                Bundle = null;
            }

            Url = null;
        }
    }
}