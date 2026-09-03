using System;
using System.IO;
using UnityEngine;

namespace DeepCore.Unity3D.AB
{
    public class ABSystem
    {
        public static ABSystem Instance { get; private set; } = new ABSystem();
        public static string RootPath { set; get; }
        public static WrapGO GetWrapGO(string bundleName, string assetName, Transform parent)
        {
            return Instance.GetWrapGOImpl(bundleName, assetName, parent);
        }
        public static void GetWrapGOAsync(string bundleName, string assetName, Transform parent, Action<WrapGO> cb)
        {
            Instance.GetWrapGOAsyncImpl(bundleName, assetName, parent, cb);
        }
        public static void CleanUp()
        {
            Instance.CleanUpImpl();
        }

        public ABSystem() { Instance = this; }

        protected virtual WrapGO GetWrapGOImpl(string bundleName, string assetName, Transform parent)
        {
            if (TryGetFullPath(bundleName, out var fullPath))
            {
                if (TryLoadBundle(fullPath, out var bundle))
                {
                    var asset = bundle.GetAsset(assetName);
                    if (asset != null)
                    {
                        var go = UnityEngine.Object.Instantiate<GameObject>(asset, parent, false);
                        return new WrapGO(bundle, go);
                    }
                }
            }
            return null;
        }
        protected virtual void GetWrapGOAsyncImpl(string bundleName, string assetName, Transform parent, Action<WrapGO> cb)
        {
            if (TryGetFullPath(bundleName, out var fullPath))
            {
                if (TryLoadBundle(fullPath, out var bundle))
                {
                    bundle.GetAssetAsync(assetName, asset =>
                    {
                        if (asset != null)
                        {
                            var go = UnityEngine.Object.Instantiate<GameObject>(asset, parent, false);
                            cb(new WrapGO(bundle, go));
                            return;
                        }
                        cb(null);
                    });
                    return;
                }
            }
            cb(null);
        }
        protected virtual void CleanUpImpl()
        {
            bundlesMap.Clear();
        }

        protected virtual bool TryGetFullPath(string path, out string fullPath)
        {
            if (File.Exists(path))
            {
                fullPath = Path.GetFullPath(path.ToLower());
                return true;
            }
            path = $"{ABSystem.RootPath}{path}";
            if (File.Exists(path))
            {
                fullPath = Path.GetFullPath(path.ToLower());
                return true;
            }
            fullPath = null;
            return false;
        }


        private HashMap<string, WrapBundle> bundlesMap = new HashMap<string, WrapBundle>();
        private bool TryLoadBundle(string fullpath, out WrapBundle bundle)
        {
            if (bundlesMap.TryGetValue(fullpath, out bundle))
            {
                return true;
            }
            if (DeepCore.IO.Resource.TryLoadData(fullpath, out var data))
            {
                var b = AssetBundle.LoadFromMemory(data);
                if (b != null)
                {
                    //var all = b.LoadAllAssets();
//                     var fuckProp = null as DeepCore.Properties;
//                     if (ABSystem.Instance.TryGetFullPath(fullpath + ".fuck", out var fuckPath))
//                     {
//                         if (DeepCore.IO.Resource.TryLoadData(fuckPath, out var fuckData))
//                         {
//                             fuckProp = DeepCore.Properties.ParseText(DeepCore.CUtils.DecodeUTF8(fuckData));
//                             Debug.Log($"Load Fuck Data : {fuckPath}");
//                         }
//                     }
                    bundle = new WrapBundle(fullpath, b);
                    bundlesMap.Add(fullpath, bundle);
                    return true;
                }
            }
            return false;
        }

    }

    public class WrapBundle : Disposable
    {
        public string FullName { get; }
        public AssetBundle Bundle { get; }

        public WrapBundle(string fullName, AssetBundle bundle)
        {
            this.FullName = fullName;
            this.Bundle = bundle;
            //this.Names = Bundle.GetAllAssetNames();
        }
        private GameObject FindGameObject(GameObject[] assets, string assetName)
        {
            for (int i = assets.Length - 1; i >= 0; --i)
            {
                if (string.Equals(assetName, assets[i].name, StringComparison.OrdinalIgnoreCase))
                {
                    return assets[i];
                }
            }
            return assets[0];
        }
        public UnityEngine.GameObject GetAsset(string assetName)
        {
            var asset = Bundle.LoadAssetWithSubAssets<GameObject>(assetName);
            if (asset != null && asset.Length > 0)
            {
                var go = FindGameObject(asset, assetName);
                //                 if (FuckData != null)
                //                 {
                //                     if (go.TryGetComponentsInChildren<MeshRenderer>(out var renders, true))
                //                     {
                //                         foreach (var render in renders)
                //                         {
                //                             if (FuckData.TryGetValue(render.gameObject.name, out var mname))
                //                             {
                //                                 try
                //                                 {
                //                                     //var m = this.Bundle.LoadAsset<Material>(mname);
                //                                     //render.material = m;
                //                                 }
                //                                 catch (Exception err)
                //                                 {
                //                                     Debug.LogError(err);
                //                                 }
                //                             }
                //                         }
                //                     }
                //                     if (go.TryGetComponentsInChildren<Animation>(out var animations, true))
                //                     {
                // 
                //                     }
                //                     if (go.TryGetComponentsInChildren<Animator>(out var animators, true))
                //                     {
                // 
                //                     }
                //                 }
                return go;
            }
            return null;
        }
        public void GetAssetAsync(string assetName, Action<UnityEngine.GameObject> cb)
        {
            Bundle.LoadAssetUniAsync<GameObject>(assetName, asset =>
            {
                if (asset is GameObject go)
                {
                    cb(go);
                    return;
                }
                cb(null);
            });
        }
        protected override void Disposing()
        {
            Bundle.Unload(true);
        }
    }

    public class WrapGO : Disposable
    {
        private WrapBundle Owner { get; }
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public WrapGO(WrapBundle owner, GameObject gameObject)
        {
            this.Owner = owner;
            this.gameObject = gameObject;
            this.transform = gameObject.transform;
        }
        protected override void Disposing()
        {
            this.Owner.Dispose();
            GameObject.Destroy(gameObject);
        }
    }

    public static class BundleExt
    {
        public static void LoadAssetUniAsync(this AssetBundle bundle, string name, Type type, Action<UnityEngine.Object> cb)
        {
            var req = bundle.LoadAssetAsync(name, type);
            req.completed += (op) => { cb(req.asset); };
        }
        public static void LoadAssetUniAsync(this AssetBundle bundle, string name, Action<UnityEngine.Object> cb)
        {
            LoadAssetUniAsync(bundle, name, typeof(UnityEngine.GameObject), cb);
        }
        public static void LoadAssetUniAsync<T>(this AssetBundle bundle, string name, Action<T> cb) where T : UnityEngine.Object
        {
            LoadAssetUniAsync(bundle, name, typeof(T), t => cb(t as T));
        }
        public static void LoadAssetWithSubAssetsUniAsync(this AssetBundle bundle, string name, Type type, Action<UnityEngine.Object[]> cb)
        {
            var req = bundle.LoadAssetWithSubAssetsAsync(name, type);
            req.completed += (op) => { cb(req.allAssets); };
        }
        public static void LoadAssetWithSubAssetsUniAsync(this AssetBundle bundle, string name, Action<UnityEngine.Object[]> cb)
        {
            LoadAssetWithSubAssetsUniAsync(bundle, name, typeof(UnityEngine.GameObject), cb);
        }
        public static void LoadAssetWithSubAssetsUniAsync<T>(this AssetBundle bundle, string name, Action<T[]> cb) where T : UnityEngine.Object
        {
            LoadAssetWithSubAssetsUniAsync(bundle, name, typeof(T), t => cb(ConvertObjects<T>(t)));
        }
        public static void LoadAllAssetsUniAsync(this AssetBundle bundle, Type type, Action<UnityEngine.Object[]> cb)
        {
            var req = bundle.LoadAllAssetsAsync(type);
            req.completed += (op) => { cb(req.allAssets); };
        }
        public static void LoadAllAssetsUniAsync(this AssetBundle bundle, Action<UnityEngine.Object[]> cb)
        {
            LoadAllAssetsUniAsync(bundle, typeof(UnityEngine.GameObject), cb);
        }
        public static void LoadAllAssetsUniAsync<T>(this AssetBundle bundle, string name, Action<T[]> cb) where T : UnityEngine.Object
        {
            LoadAllAssetsUniAsync(bundle, typeof(T), t => cb(ConvertObjects<T>(t)));
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
