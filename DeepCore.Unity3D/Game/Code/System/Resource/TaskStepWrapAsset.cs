using Code.System.AB;
using Code.System.Pool;
using Object = UnityEngine.Object;

namespace Code.System.Resource
{
    public class TaskStepWrapAsset<T> : ITaskStep, IPoolable where T : Object
    {
        public string Url { get; protected set; }
        public string Name { get; private set; }
        public AssetCompletedHandler<T> Callback { get; protected set; }
        public WrapAsset<T> WrapAsset { get; private set; }
        public bool IsCompleted { get; private set; }

        public void Init(string url, string name, AssetCompletedHandler<T> callback)
        {
            Url = url;
            Name = name;
            Callback = callback;
        }
        
        public virtual void Start(bool bAsync = true)
        {
            if (bAsync)
            {
                ABSystem.GetAssetAsync<T>(Url, Name, OnCompleted);
            }
            else
            {
                OnCompleted(ABSystem.GetAsset<T>(Url, Name));
            }
        }

        protected void OnCompleted(WrapAsset<T> wrap)
        {
            IsCompleted = true;
            WrapAsset = wrap;
        }

        public void Invoke(long serial)
        {
            Callback.Invoke(serial, WrapAsset);
            Callback = null;
            WrapAsset = null;
        }

        public void Clear()
        {
            OnClear();
            IsCompleted = false;
            
            if (WrapAsset != null)
            {
                WrapAsset.Dispose();
                WrapAsset = null;
            }

            if (Callback != null)
            {
                Callback.Invoke(0, null);
                Callback = null;
            }
        }

        protected virtual void OnClear()
        {
            
        }

        public void Dispose()
        {
            Clear();
            Disposing();
        }

        protected virtual void Disposing()
        {
            ObjectPool<TaskStepWrapAsset<T>>.Release(this);
        }
    }
}