using Code.System.AB;
using Code.System.Pool;
using Code.System.WrapGOWatch;
using Code.Utility;
using DeepCore.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Code.System.Resource
{
    public sealed class TaskStepWrapGO : ITaskStep, IPoolable
    {
        #region private static Transform TempRoot { get; }

        private static Transform _tempRoot;

        private static Transform TempRoot
        {
            get
            {
                if (_tempRoot) return _tempRoot;
                _tempRoot = new GameObject("[TempRoot]").transform.SetActive(false);
                Object.DontDestroyOnLoad(_tempRoot.gameObject);
                return _tempRoot;
            }
        }

        #endregion

        public static string ToKey(string bundleUrl, string assetName)
        {
            return $"{bundleUrl}__{assetName}";
        }

        public string Url { get; private set; }
        public string Name { get; private set; }
        public string Key => ToKey(Url, Name);
        public GameObjectCompletedHandler Callback { get; private set;}
        public WrapGOCache Cache { get; private set; }
        public Transform Parent { get; private set; }
        public WrapGO Wrap { get; private set;}
        public bool IsCompleted { get; private set; }

        public void Init(string url, string name, GameObjectCompletedHandler callback
            , WrapGOCache cache = null, Transform parent = null)
        {
            Url = url;
            Name = name;
            Callback = callback;
            Cache = cache;
            Parent = parent;
        }
        
        public void Start(bool bAsync = true)
        {
            Wrap = Cache?.Get(Key);
            if (Wrap != null)
            {
                OnCompleted(Wrap);
            }
            else if (bAsync)
            {
                ABSystem.GetAssetAsync<GameObject>(Url, Name, asset =>
                {
                    var root = Parent ? Parent : TempRoot;
                    Wrap = ObjectPool<WrapGO>.Get();
                    Wrap.Init(asset, root, Cache, Name);
                    OnCompleted(Wrap);
                });
            }
            else
            {
                var root = Parent ? Parent : TempRoot;
                Wrap = ObjectPool<WrapGO>.Get();
                var asset = ABSystem.GetAsset<GameObject>(Url, Name);
                Wrap.Init(asset, root, Cache, Name);
            }
        }

        private void OnCompleted(WrapGO wrap)
        {
            IsCompleted = true;
            WrapGOWatchSystem.Add(wrap);
        }

        public void Invoke(long serial)
        {
            if (Callback == null) return;
            if (Wrap?.GameObject && !Parent)
            {
                SceneManager.MoveGameObjectToScene(Wrap.GameObject.Parent(null), SceneManager.GetActiveScene());
            }

            Callback.Invoke(serial, Wrap);
            Callback = null;
            Wrap = null;
        }

        public void Clear()
        {
            IsCompleted = false;

            if (Wrap != null)
            {
                if (Wrap.GameObject)
                {
                    Wrap.CacheOrClear();
                }
                else
                {
                    Wrap.Dispose();
                }

                Wrap = null;
            }

            if (Callback != null)
            {
                Callback.Invoke(0, null);
                Callback = null;
            }
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<TaskStepWrapGO>.Release(this);
        }
    }
}