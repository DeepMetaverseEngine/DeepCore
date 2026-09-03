using Code.System.Pool;
using Code.Utility;
using Object = UnityEngine.Object;

namespace Code.System.AB
{
    public sealed class WrapAsset : ICleanable, IPoolable
    {
        public static implicit operator Object(WrapAsset wrap) => wrap?.Asset;
        
        public Object Asset => AssetLoader.Asset;
        internal LoaderAsset AssetLoader { get; set; }

        public void Dispose()
        {
            Clear();
            ObjectPool<WrapAsset>.Release(this);
        }

        public void Clear()
        {
            if (AssetLoader == null) return;
            ABSystemImpl.Inst.Release(AssetLoader);
            AssetLoader = null;
        }
    }
    
    public class WrapAsset<T> : ICleanable, IPoolable where T : Object
    {
        public static implicit operator T(WrapAsset<T> wrap) => wrap?.Asset;
        
        public virtual T Asset => AssetLoader.Asset as T;
        internal LoaderAsset AssetLoader { get; set; }
        
        public void Dispose()
        {
            Clear();
            Disposing();
        }

        protected virtual void Disposing()
        {
            ObjectPool<WrapAsset<T>>.Release(this);
        }

        public void Clear()
        {
            OnClear();
            if (AssetLoader == null) return;
            ABSystemImpl.Inst.Release(AssetLoader);
            AssetLoader = null;
        }

        protected virtual void OnClear()
        {
        }
    }
}
