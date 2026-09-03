using Code.System.Pool;
using Code.System.WrapGOWatch;
using UnityEngine;

namespace Code.System.Resource
{
    public class WrapScene : IWrapGO, IPoolable
    {
        public string Url { get; set; }
        public UnityEngine.SceneManagement.Scene Scene;

        public void Clear()
        {
            WrapGOWatchSystem.Remove(this);
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<WrapScene>.Release(this);
        }

        public GameObject GameObject { get; internal set; }

        public void CacheOrClear(float delaySec = 0)
        {
            Dispose();
        }
    }
}