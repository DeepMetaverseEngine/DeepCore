using Code.System.AB;
using Code.System.Pool;
using UnityEngine;
using UnityEngine.Networking;

namespace Code.System.Resource
{
    public class WrapAssetWebRequest<T> : WrapAsset<T> where T : Object
    {
        public override T Asset
        {
            get
            {
                var type = typeof(T);
                if (type == typeof(Texture2D))
                {
                    return HandlerTexture.texture as T;
                }

                return null;
            }
        }
        internal DownloadHandlerTexture HandlerTexture { get; set; }
        internal DownloadHandlerBuffer HandlerBuffer { get; set; }

        
        protected override void Disposing()
        {
            ObjectPool<WrapAssetWebRequest<T>>.Release(this);
        }

        protected override void OnClear()
        {
            base.OnClear();
            if (HandlerTexture != null)
            {
                HandlerTexture.Dispose();
                HandlerTexture = null;
            }
        }
    }
}
