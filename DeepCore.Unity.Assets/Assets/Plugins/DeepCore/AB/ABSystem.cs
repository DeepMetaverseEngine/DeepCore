using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using DeepCore.Unity3D.Impl;
using DeepCore.Unity;
using Cysharp.Threading.Tasks;

namespace DeepCore.Unity3D.AB
{
    public static class BundleExt
    {
        public static UniTask<AsyncOperation> RequestAsync(this AssetBundleRequest req)
        {
            var tcs = new UniTaskCompletionSource<AsyncOperation>();
            req.completed += (op) => { tcs.TrySetResult(op); };
            return tcs.Task;
        }


        public static async UniTask<UnityEngine.Object> LoadAssetUniAsync(this AssetBundle bundle, string name, Type type)
        {
            var req = bundle.LoadAssetAsync(name, type);
            var op = await RequestAsync(req);
            return req.asset;
        }
        public static async UniTask<UnityEngine.Object> LoadAssetUniAsync(this AssetBundle bundle, string name)
        {
            return await LoadAssetUniAsync(bundle, name, typeof(UnityEngine.GameObject));
        }
        public static async UniTask<T> LoadAssetUniAsync<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
        {
            var rsp = await LoadAssetUniAsync(bundle, name, typeof(T));
            return rsp as T;
        }



        public static async UniTask<UnityEngine.Object[]> LoadAssetWithSubAssetsUniAsync(this AssetBundle bundle, string name, Type type)
        {
            var req = bundle.LoadAssetWithSubAssetsAsync(name, type);
            var op = await RequestAsync(req);
            return req.allAssets;
        }
        public static async UniTask<UnityEngine.Object[]> LoadAssetWithSubAssetsUniAsync(this AssetBundle bundle, string name)
        {
            return await LoadAssetWithSubAssetsUniAsync(bundle, name, typeof(UnityEngine.GameObject));
        }
        public static async UniTask<T[]> LoadAssetWithSubAssetsUniAsync<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
        {
            var rsp = await LoadAssetWithSubAssetsUniAsync(bundle, name, typeof(T));
            return ConvertObjects<T>(rsp);
        }



        public static async UniTask<UnityEngine.Object[]> LoadAllAssetsUniAsync(this AssetBundle bundle, Type type)
        {
            var req = bundle.LoadAllAssetsAsync(type);
            var op = await RequestAsync(req);
            return req.allAssets;
        }
        public static async UniTask<UnityEngine.Object[]> LoadAllAssetsUniAsync(this AssetBundle bundle)
        {
            return await LoadAllAssetsUniAsync(bundle, typeof(UnityEngine.GameObject));
        }
        public static async UniTask<T[]> LoadAllAssetsUniAsync<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
        {
            var rsp = await LoadAllAssetsUniAsync(bundle, typeof(T));
            return ConvertObjects<T>(rsp);
        }




        public static T[] ConvertObjects<T>(UnityEngine.Object[] rawObjects) where T : UnityEngine.Object
        {
            if (rawObjects == null)
            {
                return null;
            }
            T[] array = new T[rawObjects.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (T)rawObjects[i];
            }

            return array;
        }


    }
}
