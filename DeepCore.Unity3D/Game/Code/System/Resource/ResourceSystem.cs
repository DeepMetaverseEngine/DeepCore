using System;
using System.Collections;
using System.Threading.Tasks;
using Code.System.AB;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.System.Resource
{
    public delegate void BundleCompletedHandler(long serial, WrapBundle wrap);
    public delegate void AssetCompletedHandler<T>(long serial, WrapAsset<T> wrap) where T : Object;
    public delegate void GameObjectCompletedHandler(long serial, WrapGO wrap);
    public delegate void SceneCompletedHandler(long serial, WrapScene wrap);

    public static class ResourceSystem
    {
#if UNITY_EDITOR
        public static Action<string> AssetCollectNotify;
#endif

        public static long Task(bool top = false, short discardMS = 0)
        {
            return ResourceSystemImpl.Inst.Task(top, discardMS);
        }

        public static bool ContainsTask(long serial)
        {
            return ResourceSystemImpl.Inst.ContainsTask(serial);
        }
        
        public static async Task<bool> TaskAsync(long serial)
        {
            return await ResourceSystemImpl.Inst.TaskAsync(serial);
        }

        public static IEnumerator TaskCoroutine(long serial)
        {
            yield return ResourceSystemImpl.Inst.TaskCoroutine(serial);
        }

        public static void TaskCancel(long serial)
        {
            ResourceSystemImpl.Inst.TaskCancel(serial);
        }
        
        public static void TaskStepWrapBundle(long serial, string url, BundleCompletedHandler callback)
        {
            ResourceSystemImpl.Inst.TaskStepWrapBundle(serial, url, callback);
        }

        public static void TaskStepWrapAsset<T>(long serial, string url, string name_without_ext, AssetCompletedHandler<T> callback)
            where T : Object
        {
            ResourceSystemImpl.Inst.TaskStepWrapAsset(serial, url, name_without_ext, callback);
        }

        public static void TaskStepWrapGO(long serial, string url, string name_without_ext, GameObjectCompletedHandler callback,
            WrapGOCache cache = null, Transform parent = null)
        {
            ResourceSystemImpl.Inst.TaskStepWrapGO(serial, url, name_without_ext, callback, cache, parent);
        }

        public static void TaskStepWrapScene(long serial, string url, bool additive, SceneCompletedHandler callback)
        {
            ResourceSystemImpl.Inst.TaskStepWrapScene(serial, url, additive, callback);
        }

        public static async Task<WrapScene> GetWrapSceneAsync(string url, bool additive, bool top = false, short discardMS = 0)
        {
            return await ResourceSystemImpl.Inst.GetWrapSceneAsync(url, additive, top, discardMS);
        }



        public static async Task<WrapAsset<T>> GetWrapAssetAsync<T>(string url, string name_without_ext, bool top = false) where T : Object
        {
            if (url.EndsWith(".png") && (!url.StartsWith("http://") || !url.StartsWith("https://")))
            {
                url = $"file://{ABSystem.RootPath}{url}";
            }
            return await ResourceSystemImpl.Inst.GetWrapAssetAsync<T>(url, name_without_ext, top);
        }
        public static WrapAsset<T> GetWrapAsset<T>(string url, string name_without_ext) where T : Object
        {
            if (url.EndsWith(".png") && (!url.StartsWith("http://") || !url.StartsWith("https://")))
            {
                url = $"{ABSystem.RootPath}{url}";
            }
            return ResourceSystemImpl.Inst.GetWrapAsset<T>(url, name_without_ext);
        }
        
        public static WrapAsset<T> GetWrapAssetWebRequest<T>(string url, string name_without_ext) where T : Object
        {
            if (url.EndsWith(".png") && (!url.StartsWith("http://") || !url.StartsWith("https://")))
            {
                url = $"{ABSystem.RootPath}{url}";
            }
            return ResourceSystemImpl.Inst.GetWrapAssetWebRequest<T>(url, name_without_ext);
        }

        public static async Task<WrapGO> GetWrapGOAsync(string url, string name_without_ext, bool top = false, short discardMS = 0,
            WrapGOCache cache = null, Transform parent = null)
        {
            return await ResourceSystemImpl.Inst.GetWrapGOAsync(url, name_without_ext, top, discardMS, cache, parent);
        }
        public static void GetWrapGOInvoke(Action<WrapGO> cb, string url, string name_without_ext, bool top = false, short discardMS = 0,
            WrapGOCache cache = null, Transform parent = null)
        {
            ResourceSystemImpl.Inst.GetWrapGOInvoke(cb, url, name_without_ext, top, discardMS, cache, parent);
        }

        public static WrapGO GetWrapGO(string url, string name_without_ext, WrapGOCache cache = null, Transform parent = null)
        {
            return ResourceSystemImpl.Inst.GetWrapGO(url, name_without_ext, cache, parent);
        }


    }
}