using Code.System.Pool;
using Code.System.WrapGOWatch;
using Code.Utility;
using DeepCore.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.System.Resource
{
    public class WrapGOCache : ICleanable, IPoolable
    {
        #region private LRUCache<string, WrapGO> Cache

        private LRUCache<string, WrapGO> _cache;

        private LRUCache<string, WrapGO> Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new LRUCache<string, WrapGO>(OnCacheRemove);
                }

                return _cache;
            }
        }

        #endregion

        #region private Transform RootTrans

        private Transform _rootTrans;

        private Transform RootTrans
        {
            get
            {
                if (_rootTrans) return _rootTrans;
                _rootTrans = new GameObject($"[{Name}]").transform.SetActive(false);
                Object.DontDestroyOnLoad(_rootTrans.gameObject);
                return _rootTrans;
            }
        }

        #endregion

        public string Name { get; set; }

        public int Capacity
        {
            get => Cache.Capacity;
            set => Cache.Capacity = value;
        }

        public WrapGOCache()
        {
#if _DEBUG_
            Name = "WrapGOCache";
#endif
        }

        public void Clear()
        {
            if (_cache != null)
            {
                _cache.Clear();
                _cache = null;
            }

            if (_rootTrans)
            {
                Object.Destroy(_rootTrans.gameObject);
            }
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<WrapGOCache>.Release(this);
        }

        public WrapGO Get(string url)
        {
#if _DEBUG_
            if (ObjectPool<WrapGOCache>.Contains(this))
            {
                Debug.LogWarning("WrapGOCache has released!");
            }
#endif
            var wrap = Cache.Get(url);
#if _DEBUG_
            if (wrap != null && !wrap.GameObject)
            {
                Debug.LogWarning("go in cache has destroyed!");
                wrap.Dispose();
                return null;
            }
#endif
            if (wrap == null)
            {
                return null;
            }

            wrap.SetCache(this) ;
            SceneManager.MoveGameObjectToScene(wrap.GameObject.Parent(null), SceneManager.GetActiveScene());
            return wrap;
        }

        public void Release(WrapGO wrap, float delaySec = 0f)
        {
#if _DEBUG_
            if (ObjectPool<WrapGOCache>.Contains(this))
            {
                Debug.LogWarning("WrapGOCache has released!");
            }
            
            if (wrap == null)
            {
                Debug.LogWarning("wrap is null!");
                return;
            }

            if (!wrap.GameObject)
            {
                Debug.LogWarning("go is destroyed!");
                return;
            }
#endif
            Tick.TickSystem.Tick(delaySec, (serial, index) =>
            {
#if _DEBUG_
                if (ObjectPool<WrapGOCache>.Contains(this))
                {
                    Debug.LogWarning("WrapGOCache has released!");
                    wrap.Dispose();
                    return;
                }
#endif
                if (!wrap.GameObject) return;

                wrap.SetCache(null);
                if (_cache == null)
                {
                    wrap.Dispose();
                }
                //````````````````````````````````
                // wrap.GameObject.Parent(RootTrans);
                // _cache.Release(wrap.Url, wrap);
            });
        }

        private void OnCacheRemove(WrapGO wrap)
        {
#if _DEBUG_
            RootTrans.name = $"[{Name}]_{_cache.Count}/{_cache.Capacity}";
#endif
            WrapGOWatchSystem.Remove(wrap);
            wrap.Dispose();
        }
    }
}
