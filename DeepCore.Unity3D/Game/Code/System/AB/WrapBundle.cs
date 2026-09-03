using Code.System.Pool;
using Code.Utility;
using UnityEngine;

namespace Code.System.AB
{
    public sealed class WrapBundle : ICleanable, IPoolable
    {
        public static implicit operator AssetBundle(WrapBundle wrap) => wrap?.Bundle;
        
        public AssetBundle Bundle => BundleLoader.Bundle;
        
        internal LoaderBundle BundleLoader { get; set; }
        public string FileName { get; internal set; }
        public void Dispose()
        {
            Clear();
            ObjectPool<WrapBundle>.Release(this);
        }

        public void Clear()
        {
            if (BundleLoader == null) return;
            ABSystemImpl.Inst.Release(BundleLoader);
            BundleLoader = null;
        }
    }
}
