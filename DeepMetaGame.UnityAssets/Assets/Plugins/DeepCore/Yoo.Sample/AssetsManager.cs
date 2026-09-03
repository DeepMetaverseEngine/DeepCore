

#if YOOASSET_SAMPLE

using Cysharp.Threading.Tasks;
using DeepCore;
using DeepCore.IO;
using DeepCore.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

using YooAsset;
using Object = UnityEngine.Object;
namespace Yoo
{
    public class YooAssetParam
    {
        public static string YooVersion = "";
        public static string DefaultPackageName = "";
        public static string[] AllPackages;
    }
    /// <summary>
    ///     资源管理器
    /// </summary>
    public class AssetManager : UniSingleton<AssetManager>
    {
        private string[] AllPackageNames;
        private string YooVersion;
        private string PackageRoot;
        private string DefaultPackageName;
        //private string RawPackageName;
        //private EPlayMode playMode;

        private ResourcePackage DefaultPackage = null;
        //private ResourcePackage RawPackage = null;

        public bool EditorOnlyPackageSimulate { get; }
        public int TotalPackageCount => 1;

        public string FormatLocation(string location) => location.Replace('/', ';');

        protected override void Init()
        {
            AllPackageNames = YooAssetParam.AllPackages;
            YooVersion = YooAssetParam.YooVersion;
            DefaultPackageName = YooAssetParam.DefaultPackageName;

            DefaultPackage = YooAssets.GetPackage(DefaultPackageName);

            YooAssets.SetDefaultPackage(DefaultPackage);
        }
        /// <summary>
        ///     销毁所有资源和Package
        /// </summary>
        protected override void Disposing()
        {
            foreach (var packageName in AllPackageNames)
            {
                var package = YooAssets.GetPackage(packageName);
                YooAssets.RemovePackage(packageName);
                package?.DestroyAsync();
            }
        }

        #region API
        public bool IsValid(string path)
        {
            return DefaultPackage.CheckLocationValid(path) /*|| Instance.RawPackage.CheckLocationValid(path)*/;
        }

        public AssetInfo[] GetAssetInfosByTag(string tag)
        {
            AssetInfo[] result1 = DefaultPackage.GetAssetInfos(tag);
            //AssetInfo[] result2 = Instance.RawPackage.GetAssetInfos(tag);
            //result1.AddRange(result2);
            return result1;
        }

        #endregion

        #region Load API

        /// <summary>
        ///     异步加载场景
        /// </summary>
        /// <param name="location">场景名</param>
        /// <param name="sceneMode">附加模式</param>
        /// <param name="activeScene">是否设置为激活场景</param>
        /// <returns>场景加载句柄</returns>
        /// <exception cref="Exception">加载失败错误</exception>
        public async UniTask<SceneHandle> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activeScene = true)
        {
            var sceneHandle = LoadSceneAsyncInternal(location, sceneMode);
            await sceneHandle;
            if (sceneHandle.Status != EOperationStatus.Succeed) throw new Exception(sceneHandle.LastError);
            if (activeScene) SceneManager.SetActiveScene(sceneHandle.SceneObject);
            return sceneHandle;
        }
        public SceneHandle LoadScene(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activeScene = true)
        {
            var sceneHandle = LoadSceneInternal(location, sceneMode);
            if (sceneHandle.Status != EOperationStatus.Succeed) throw new Exception(sceneHandle.LastError);
            if (activeScene) SceneManager.SetActiveScene(sceneHandle.SceneObject);
            return sceneHandle;
        }

        /// <summary>
        ///异步卸载场景
        /// </summary>
        /// <param name="sceneHandle">场景句柄</param>
        /// <returns></returns>
        /// <exception cref="Exception">卸载失败错误</exception>
        public async UniTask UnloadSceneAsync(SceneHandle sceneHandle)
        {
            var unloadHandle = sceneHandle.UnloadAsync();
            await unloadHandle;
            if (unloadHandle.Status != EOperationStatus.Succeed) throw new Exception(unloadHandle.Error);
        }

        /// <summary>
        ///异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源名</param>
        /// <returns>卸载句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        public async UniTask<AssetHandle> LoadAssetAsync<T>(string location) where T : Object
        {
            var assetHandle = LoadAssetAsyncInternal<T>(location);
            await assetHandle;
            if (assetHandle.Status != EOperationStatus.Succeed) throw new Exception(assetHandle.LastError);
            return assetHandle;
        }

        /// <summary>
        ///异步加载资源
        /// </summary>
        /// <param name="location">资源名</param>
        /// <returns>资源句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        public async UniTask<AssetHandle> LoadAssetAsync(string location)
        {
            var assetHandle = LoadAssetAsyncInternal(location);
            await assetHandle;
            if (assetHandle.Status != EOperationStatus.Succeed) throw new Exception(assetHandle.LastError);
            return assetHandle;
        }

        /// <summary>
        ///同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源名</param>
        /// <returns>卸载句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        public AssetHandle LoadAsset<T>(string location) where T : Object
        {
            var assetHandle = LoadAssetSyncInternal<T>(location);
            if (assetHandle.Status != EOperationStatus.Succeed) throw new Exception(assetHandle.LastError);
            return assetHandle;
        }

        /// <summary>
        ///同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源名</param>
        /// <returns>卸载句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        public AssetHandle LoadAsset(string location, Type type)
        {
            var assetHandle = LoadAssetSyncInternal(location, type);
            if (assetHandle.Status != EOperationStatus.Succeed) throw new Exception(assetHandle.LastError);
            return assetHandle;
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public AllAssetsHandle LoadAllAssetsAsync(string location, int priority = 0)
        {
            var handle = Instance.DefaultPackage.LoadAllAssetsAsync<GameObject>(location);
            return handle;
        }


        /// <summary>
        /// 异步加载原生资源
        /// </summary>
        /// <param name="location">资源名</param>
        /// <returns>卸载句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        /*
                public async UniTask<RawFileHandle> LoadRawAssetAsync(string location)
                {
                    var assetHandle = Instance.RawPackage.LoadRawFileAsync(location);
                    await assetHandle;
                    if (assetHandle.Status != EOperationStatus.Succeed)
                        throw new Exception(assetHandle.LastError);

                    return assetHandle;
                }*/

        /// <summary>
        /// 同步加载原生资源
        /// </summary>
        /// <param name="location">资源名</param>
        /// <returns>卸载句柄</returns>
        /// <exception cref="Exception">加载失败的异常</exception>
        /*
                public RawFileHandle LoadRawAsset(string location)
                {
                    var assetHandle = Instance.RawPackage.LoadRawFileSync(location);
                    if (assetHandle.Status != EOperationStatus.Succeed) throw new Exception(assetHandle.LastError);
                    return assetHandle;
                }*/


        /// <summary>
        ///     加载实例 只能是GameObject或者Component,注意:这个方法会直接创建出实例对象
        /// </summary>
        /// <typeparam name="T">实例类型</typeparam>
        /// <param name="location">实例地址</param>
        public async UniTask<InstanceHandle<T>> LoadInstanceAsync<T>(string location) where T : Object
        {
            var assetHandle = await LoadAssetAsync<GameObject>(location);
            var instanceHandle = InstanceHandle<T>.pool.Get().Init(assetHandle);
            return instanceHandle;
        }

        public InstanceHandle<T> LoadInstanceSync<T>(string location) where T : Object
        {
            var assetHandle = LoadAsset<GameObject>(location);
            var instanceHandle = InstanceHandle<T>.pool.Get().Init(assetHandle);
            return instanceHandle;
        }

        /// <summary>
        ///卸载实例句柄
        /// </summary>
        public void UnloadInstanceHandle<T>(InstanceHandle<T> handle) where T : Object
        {
            handle?.Dispose();
        }

        /// <summary>
        ///卸载资源句柄
        /// </summary>
        /// <param name="assetHandle">卸载句柄</param>
        public void UnloadAssetHandle(AssetHandle assetHandle)
        {
            assetHandle.Release();
        }


        /// <summary>
        ///     卸载引用计数为0的资产
        /// </summary>
        public void UnloadUnusedAssets()
        {
            var op = DefaultPackage.UnloadUnusedAssetsAsync();
            op.WaitForAsyncComplete();
        }


        public void UnloadAsset(string location)
        {
            DefaultPackage.TryUnloadUnusedAsset(location);
        }

        #endregion

        #region Load Internal

        private AssetHandle LoadAssetAsyncInternal(string location)
        {
            return DefaultPackage.LoadAssetAsync((location));
        }

        private AssetHandle LoadAssetAsyncInternal<T>(string location) where T : Object
        {
            return DefaultPackage.LoadAssetAsync<T>((location));
        }

        private AssetHandle LoadAssetSyncInternal(string location, Type type)
        {

            return DefaultPackage.LoadAssetSync(location, type);
        }

        private AssetHandle LoadAssetSyncInternal<T>(string location) where T : Object
        {
            return DefaultPackage.LoadAssetSync<T>((location));
        }

        private SceneHandle LoadSceneAsyncInternal(
            string location,
            LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None,
            bool suspendLoad = false,
            uint priority = 0)
        {
            return DefaultPackage.LoadSceneAsync((location), sceneMode, physicsMode, suspendLoad, priority);
        }
        private SceneHandle LoadSceneInternal(
            string location,
            LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            return DefaultPackage.LoadSceneSync((location), sceneMode, physicsMode);
        }

        #endregion
    }
    internal interface IInstanceHandle : IDisposable
    {
        AssetHandle AssetHandle { get; }
    }

    public class InstanceHandle<T> : IDisposable where T : UnityEngine.Object
    {
        public void Dispose()
        {
            if (!bindAssetHandle)
                return;

            if (Instance)
            {
                if (Instance is Component comp)
                {
                    UnityEngine.Object.Destroy(comp.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(Instance);
                }
            }

            Instance = null;
            AssetHandle?.Dispose();
            AssetHandle = null;
            pool.Release(this);
            bindAssetHandle = false;

        }

        internal static UnityEngine.Pool.ObjectPool<InstanceHandle<T>> pool = new(() => new InstanceHandle<T>());


        private AssetHandle AssetHandle;
        public T AssetObject => AssetHandle.GetAssetObject<T>();
        public T Instance { get; private set; }

        bool bindAssetHandle;

        internal InstanceHandle<T> Init(AssetHandle handle)
        {
            if (bindAssetHandle)
                return this;

            AssetHandle = handle;
            var go = AssetHandle.GetAssetObject<GameObject>();
            if (go == null)
            {
                Debug.LogError("AssetHandle does not reference a GameObject.");
                bindAssetHandle = true;
                return this;
            }

            T inst = GetInstance(go);

            if (inst == null)
            {
                Debug.LogError($"InstanceHandle: Expected type {typeof(T)}, got {AssetHandle.AssetObject.GetType()} instead.");
            }
            else
            {
                Instance = UnityEngine.Object.Instantiate(inst);
            }

            bindAssetHandle = true;
            return this;
        }


        private T GetInstance(GameObject go)
        {
            if (typeof(T) == typeof(GameObject))
            {
                return go as T;
            }
            return go.GetComponent<T>();
        }
    }


    /// <summary>
    /// 缓存一组资产句柄，只需要关注资产在哪里销毁而不需要关注每个对象的释放
    /// </summary>
    public class AssetsLoaderGroup : Disposable
    {
        private readonly Dictionary<string, AssetHandle> handles = new();
        //private readonly Dictionary<string, UnityEngine.Object> instances = new();
        //private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        protected override void Disposing()
        {
            //semaphore.Wait(1000);
//             foreach (var go in instances.Values)
//             {
//                 Object.Destroy(go);
//             }

            foreach (var kv in handles)
            {
                kv.Value?.Dispose();
            }

            handles.Clear();
            //instances.Clear();
        }

        private string GetUniqueId(string location, Type type)
        {
            return string.Concat(location, type.FullName);
        }

        public async UniTask<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            var unique = GetUniqueId(location, typeof(T));
            //AssetHandle handle = null;
            //await semaphore.WaitAsync();
            try
            {
//                 if (instances.TryGetValue(unique, out UnityEngine.Object result))
//                     return result as T;

                if (!handles.TryGetValue(unique, out var handle))
                {
                    handle = await AssetManager.Instance.LoadAssetAsync<T>(location);
                    handles.Add(unique, handle);
                }
                return handle.GetAssetObject<T>();
                //                 var op = handle.InstantiateAsync();
                //                 await op;
                //                 instances.Add(unique, op.Result);
                //                 return op.Result as T;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                //semaphore.Release();
            }
            return default(T);
        }

        public T LoadAsset<T>(string location) where T : UnityEngine.Object
        {
            var unique = GetUniqueId(location, typeof(T));
            if (!handles.TryGetValue(unique, out var handle))
            {
                handle = AssetManager.Instance.LoadAsset<T>(location);
                handles.Add(unique, handle);
            }
            return handle.GetAssetObject<T>();
        }

        public T LoadAsset<T>(string location, Type type) where T : UnityEngine.Object
        {
            var unique = GetUniqueId(location, type);
            if (!handles.TryGetValue(unique, out var handle))
            {
                handle = AssetManager.Instance.LoadAsset(location, type);
                handles.Add(unique, handle);
            }
            return handle.GetAssetObject<T>();
        }

        public bool Contains<T>(string location)
        {
            var key = GetUniqueId(location, typeof(T));
            return handles.ContainsKey(key);
        }
    }

    [Serializable]
    public class YooAssetData
    {
        public EPlayMode PlayMode;

        public string HostURL;
        public string FallbackURL;
        public string DefaultPackageName;
        public string[] AllPackages;

        public int DownloadingMaxCount = 10;
        public int ReTryTimes = 10;

        public Action<PatchStep, Exception> OnError;
        public Action<float, string> OnProgress;
        public Action<PatchStep, string> OnStatus;
    }

    public enum PatchStep
    {
        Error,
        AssetInit,
        DownloadPackages,
        CleanCache,
        Complete
    }

    public class YooAssetManager
    {
        public static string YooAssetRoot { get; set; }
        public static string DefaultUnityPackage { get; set; } = "DefaultPackage";
        public static string DefaultYooVersion { get; set; } = string.Empty;
        public static EPlayMode DefaultPlayMode { get; set; } = EPlayMode.EditorSimulateMode;


        public static async UniTask InitYooAsset(YooAssetData data)
        {
            YooAssetParam.YooVersion = DefaultYooVersion;

            YooAssets.Initialize();
            YooAssets.SetOperationSystemMaxTimeSlice(100);

            if (data != null)
            {
                if (data.DefaultPackageName.IsNullOrEmpty())
                {
                  throw new Exception("YooAsset Default package name is empty.");
                }
                YooAssetParam.DefaultPackageName = data.DefaultPackageName;
                if (data.AllPackages != null)
                {
                    foreach (var packageName in data.AllPackages)
                    {
                        var package = YooAssets.TryGetPackage(packageName);
                        if (package == null)
                            package = YooAssets.CreatePackage(packageName);
                        await InitPackage(package, data);
                    }
                }
                else
                {
                    var package = YooAssets.TryGetPackage(YooAssetParam.DefaultPackageName);
                    if (package == null)
                        package = YooAssets.CreatePackage(YooAssetParam.DefaultPackageName);
                    await InitPackage(package, data);
                }
               
            }
            else
            {
                string defaultPackage = DefaultUnityPackage;
                YooAssetParam.DefaultPackageName = defaultPackage;

                var package = YooAssets.TryGetPackage(defaultPackage);
                if (package == null)
                    package = YooAssets.CreatePackage(defaultPackage);

                await InitPackage(package, data);
            }
        }

        public static async UniTask InitPackage()
        {
            var packages = YooAssets.GetAllPackages();

            foreach (var package in packages)
            {
                var requestVersionOp = package.RequestPackageVersionAsync();
                await requestVersionOp;

                if (requestVersionOp.Status is not EOperationStatus.Succeed)
                {
                    Debug.LogError($"InitPackage RequestPackageVersion  Error : {requestVersionOp.Error}");
                }
                else
                {
                    var version = requestVersionOp.PackageVersion;
                    Debug.Log($"Request Package[{package.PackageName}] Version: {version}. ");

                    YooAssetParam.YooVersion += version.Split('-').LastOrDefault();

                    var updateManifestOp = package.UpdatePackageManifestAsync(version);
                    await updateManifestOp;
                    if (updateManifestOp.Status is not EOperationStatus.Succeed)
                    {
                        Debug.LogError($"InitPackage UpdatePackageManifest  Error :{updateManifestOp.Error}");
                    }
                }
            }
        }


        private static async UniTask InitPackage(ResourcePackage package, YooAssetData data)
        {
            var mode = DefaultPlayMode;
            //string path = null;
            if (data != null)
            {
                mode = data.PlayMode;
            }
//             else if (string.IsNullOrEmpty(YooAssetRoot))
//             {
//                 string editorRoot = "";
//                 if (CFiles.TryFindParentDirectory(Environment.CurrentDirectory, Path.Combine("Data", "GameEditor", "res", "yoo"), out editorRoot))
//                 {
//                     path = editorRoot;
//                 }
//             }
//             else
//             {
//                 path = YooAssetRoot;
//             }

            InitializeParameters initParams = mode switch
            {
                EPlayMode.EditorSimulateMode => BuildEditorMode(package.PackageName),
                EPlayMode.OfflinePlayMode => BuildOfflineMode(package.PackageName),
                EPlayMode.HostPlayMode => BuildHostMode(data, package.PackageName),
#if UNITY_WEBGL && WEIXINMINIGAME
                EPlayMode.WebPlayMode => BuildWebMode(data, package.PackageName),
#endif
                _ => throw new ArgumentException("Invalid PlayMode: " + mode + "not support!")
            };

            var initOperation = package.InitializeAsync(initParams);
            await initOperation;
            if (initOperation.Status is not EOperationStatus.Succeed)
            {
                Debug.LogError($"YooAsset  package Init  Failed : Package-Name({package.PackageName})");
                data.OnError?.Invoke(PatchStep.Error, new Exception($"Package {package.PackageName} Initialize Failed!"));
            }
        }



        public static async UniTask InitAsset()
        {
            //YooAsset.load
        }

        private static InitializeParameters BuildEditorMode(string packageName)
        {
            var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            return new EditorSimulateModeParameters
            {
                BundleLoadingMaxConcurrency = 20,
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
            };
        }

        private static InitializeParameters BuildOfflineMode(string packageName)
        {
            var root = "";
            if (!string.IsNullOrEmpty(YooAssetRoot))
            {
                root = PathUtility.Combine(YooAssetRoot, packageName);
            }
            else if (CFiles.TryFindParentDirectory(Environment.CurrentDirectory, Path.Combine("Data", "GameEditor", "res", "yoo"), out var editorRoot))
            {
                root = editorRoot + "/" + packageName;
            }
            else
            {
                root = YooAssetRoot + "/" + packageName;
            }

            Debug.Log($"YooAsset  BuildOfflineMode path :{root}");

            var createParameters = new OfflinePlayModeParameters
            {
                BundleLoadingMaxConcurrency = 20,

                BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(null, root)
            };
            createParameters
                .BuildinFileSystemParameters
                .AddParameter(FileSystemParametersDefine.DISABLE_CATALOG_FILE, true);
            return createParameters;

        }

        private static InitializeParameters BuildHostMode(YooAssetData data, string packageName)
        {
            var host = PathUtility.Combine(data.HostURL, packageName);
            var fallback = PathUtility.Combine(data.FallbackURL, packageName);
            var remoteServices = new RemoteService(host, fallback);
            var builtinRoot = PathUtility.Combine(Application.streamingAssetsPath, $"yoo/{packageName}");

            return new HostPlayModeParameters
            {
                BundleLoadingMaxConcurrency = 20,

                // 必须有首包资源才能初始化BuildinFileSystem，否则找不到对应版本号
                BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(null, builtinRoot),

                CacheFileSystemParameters =
                    FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices),
            };
        }
#if UNITY_WEBGL && WEIXINMINIGAME
        //过滤端口后的所有内容
        private static string GetHostURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;
            // 匹配 http(s)://host:port/path，去掉 :port，保留路径
            var match = System.Text.RegularExpressions.Regex.Match(url, @"^(https?://[^/:]+)(:\d+)?(.*)$");
            if (match.Success)
                return match.Groups[1].Value + match.Groups[3].Value;
            return url;
        }
        private static InitializeParameters BuildWebMode(YooAssetData data, string packageName)
        {
           
            var host = GetHostURL(data.HostURL);
            var fallback = GetHostURL(data.FallbackURL);
            var remoteServices = new RemoteService(host, fallback);
            // 小游戏缓存根目录
            // 注意：此处代码根据微信插件配置来填写！
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE/yoo/{packageName}";
            //string pacakgeRoot = $"{WeChatWASM.WX.PluginCachePath}/yoo";
            Debug.LogWarning($"packageRoot{packageRoot}");
            // 创建初始化参数
            var createParameters = new WebPlayModeParameters();
            createParameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null);
            
            return createParameters;
        }
#endif

        public static class PathUtility
        {
            public static string RegularLinuxPath(string path)
            {
                return path.Replace('\\', '/').Replace("\\", "/");
            }

            public static string Combine(string path1, string path2)
            {
                return StringUtility.Format("{0}/{1}", path1, path2);
            }

            public static string Combine(string path1, string path2, string path3)
            {
                return StringUtility.Format("{0}/{1}/{2}", path1, path2, path3);
            }

            public static string Combine(string path1, string path2, string path3, string path4)
            {
                return StringUtility.Format("{0}/{1}/{2}/{3}", path1, path2, path3, path4);
            }

            public static string GetPlatform()
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android: return "android";
                    case RuntimePlatform.IPhonePlayer: return "ios";
                    case RuntimePlatform.WindowsPlayer: return "pc";
                    case RuntimePlatform.OSXPlayer: return "mac";
                    default: return "pc";
                }
            }
        }
        public static class StringUtility
        {
            [ThreadStatic]
            private static StringBuilder _cacheBuilder = new(2048);

            public static string Format(string format, object arg0)
            {
                if (string.IsNullOrEmpty(format))
                    throw new ArgumentNullException();

                _cacheBuilder.Length = 0;
                _cacheBuilder.AppendFormat(format, arg0);
                return _cacheBuilder.ToString();
            }
            public static string Format(string format, object arg0, object arg1)
            {
                if (string.IsNullOrEmpty(format))
                    throw new ArgumentNullException();

                _cacheBuilder.Length = 0;
                _cacheBuilder.AppendFormat(format, arg0, arg1);
                return _cacheBuilder.ToString();
            }
            public static string Format(string format, object arg0, object arg1, object arg2)
            {
                if (string.IsNullOrEmpty(format))
                    throw new ArgumentNullException();

                _cacheBuilder.Length = 0;
                _cacheBuilder.AppendFormat(format, arg0, arg1, arg2);
                return _cacheBuilder.ToString();
            }
            public static string Format(string format, params object[] args)
            {
                if (string.IsNullOrEmpty(format))
                    throw new ArgumentNullException();

                if (args == null)
                    throw new ArgumentNullException();

                _cacheBuilder.Length = 0;
                _cacheBuilder.AppendFormat(format, args);
                return _cacheBuilder.ToString();
            }
        }
        public class RemoteService : IRemoteServices
        {
            private readonly string defHost;
            private readonly string fallbackHost;

            public RemoteService(string cdn, string fallbackCDN)
            {
                defHost = cdn;
                fallbackHost = fallbackCDN;
            }

            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return PathUtility.RegularLinuxPath($"{defHost}/{fileName}");
            }

            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return PathUtility.RegularLinuxPath($"{defHost}/{fileName}");
            }
        }
    }
}

#endif