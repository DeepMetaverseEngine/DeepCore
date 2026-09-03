using Code.System.AB;
using Code.System.Pool;
using Code.System.WrapGOWatch;
using UnityEngine;

namespace Code.System.Resource
{
    public sealed class WrapGO : IWrapGO, IPoolable
    {
        public static implicit operator GameObject(WrapGO wrap) => wrap?.GameObject;

        internal WrapAsset<GameObject> WrapAsset { get; private set; }
        internal WrapGOCache Cache { get; private set; }
        public GameObject GameObject { get; private set; }
        public string Name { get; private set; }
        public GameObject gameObject => GameObject;
        public Transform transform => Transform;
        public string name => Name;


        #region public Transform Transform { get; }

        private Transform _transform;

        public Transform Transform
        {
            get
            {
                if (!_transform && GameObject)
                {
                    _transform = GameObject.transform;
                }

                return _transform;
            }
            private set => _transform = value;
        }

        #endregion
        internal void SetCache(WrapGOCache cache)
        {
            this.Cache = cache;
        }
        public void Init(WrapAsset<GameObject> asset, Transform parent, WrapGOCache cache, string name)
        {
            this.WrapAsset = asset;
            this.Cache = cache;
            this.Name = name;
            if (WrapAsset != null)
            {
                GameObject = Object.Instantiate<GameObject>(WrapAsset, parent, false);
            }
        }

        public void Clear()
        {
            if (WrapAsset != null)
            {
                WrapAsset.Dispose();
                WrapAsset = null;
            }
            WrapGOWatchSystem.Remove(this);
            Cache = null;
            Object.DestroyImmediate(GameObject);
            GameObject = null;
            Name = null;
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<WrapGO>.Release(this);
        }

        public void CacheOrClear(float delaySec = 0f)
        {
            if (GameObject && Cache != null)
            {
                Cache.Release(this, delaySec);
            }
            else
            {
                Dispose();
            }
        }
    }
}