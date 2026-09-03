using System;
using Code.System.AB;
using Code.System.Pool;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Code.System.Resource
{
    public sealed class TaskStepWrapAssetWebRequest<T> : TaskStepWrapAsset<T> where T : Object
    {
        
        public void Init(string url, AssetCompletedHandler<T> callback)
        {
            Url = url;
            Callback = callback;
        }

        public override void Start(bool bAsync = true)
        {
            var request = new UnityWebRequest(Url);
            var type = typeof(T);
            var wrap = ObjectPool<WrapAssetWebRequest<T>>.Get();
            if (type == typeof(Texture2D))
            {
                wrap.HandlerTexture = new DownloadHandlerTexture();
                request.downloadHandler = wrap.HandlerTexture;
            }
            else
            {
                wrap.HandlerBuffer = new DownloadHandlerBuffer();
                request.downloadHandler = wrap.HandlerBuffer;
            }
            var op = request.SendWebRequest();
            op.completed += operation =>
            {
                OnCompleted(wrap);
            };
        }

        protected override void Disposing()
        {
            ObjectPool<TaskStepWrapAssetWebRequest<T>>.Release(this);
        }
    }
}