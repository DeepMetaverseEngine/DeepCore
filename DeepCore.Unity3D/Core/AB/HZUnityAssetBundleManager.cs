using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D
{
//     public class HZUnityAssetBundleManager<ShaderCollection> : HZUnityAssetBundleManager where ShaderCollection : UnityEngine.Object
//     {
//         public ShaderCollection shaderCollection = null;
//         protected override void OnLoadShaderCollection()
//         {
//             var sc = shadervariants.LoadAsset<ShaderCollection>("ShaderCollection.asset");
//             if (sc != null)
//             {
//                 shaderCollection = sc;
//             }
//         }
//         protected override bool TryGetInShaderCollection(string shaderName, out Shader s)
//         {
//             if (shaderCollection != null)
//             {
//                 dynamic list = shaderCollection;
//                 foreach (var item in list.nameToFile)
//                 {
//                     if (shaderName.Equals(item.name, StringComparison.Ordinal))
//                     {
//                         s = shadervariants.LoadAsset<Shader>(item.path);
//                         if (s != null)
//                         {
//                             ShaderList.Add(s);
//                             return true;
//                         }
//                         break;
//                     }
//                 }
//             }
//             s = null;
//             return false;
//         }
//     }

    /// <summary>
    /// HZUnityAssetBundle加载管理器.
    /// </summary>
    abstract public class HZUnityAssetBundleManager : MonoBehaviour
    {
        protected static HZUnityAssetBundleManager mInstance = null;
        protected Dictionary<string, HZUnityAssetBundle> mABMap = null;
        protected Dictionary<string, HZUnityABLoadAdapter> mLoadTaskMap = null;
        public AssetBundleManifest Manifest { get; private set; }
        public List<Shader> ShaderList { get; private set; }
        //         public Type shaderCollectionType;
        //         protected UnityEngine.Object shaderCollection = null;
        protected Dictionary<string, List<string>> mDepsList = new Dictionary<string, List<string>>();
        public int PrefabCapacity = 50;
        public delegate void ShaderListLoadOverHandler(AssetBundle ab);
        public static ShaderListLoadOverHandler OnShaderListLoadOverCallBack;
        public static Action<UnityEngine.Object> OnPostProcessingLoadOverHandlerCallBack;

        protected string shaderAB = "/res/shadervariants.assetbundles";
        protected string ppsAB = "/res/postprocessresources.assetbundles";
        protected AssetBundle shadervariants = null;
        protected AssetBundle postprocessresources = null;

        //private HZUnityAssetBundleManager()
        //{
        //    mInstance = this;
        //    Init();
        //}
        //private List<HZUnityABLoadAdapter> mTempLoadTask = null;
        protected AsyncOperation mUnloadOp = null;
        protected bool mNeedUnload = false;
        void Awake()
        {
            mInstance = this;
            Init();
        }

        void Start()
        {

        }

        void Update()
        {
            if (!IsInit && HZUnityABLoadAdapter.LoadRootPath != null)
            {
                Init();
            }

            if (!IsInit)
            {
                return;
            }

            //检查加载任务
            //mTempLoadTask = new List<HZUnityABLoadAdapter>(mLoadTaskMap.Values);
            //var iter = mTempLoadTask.GetEnumerator();
            //while (iter.MoveNext())
            //{
            //    if(iter.Current.OnAdapterUpdate())
            //    {
            //        iter.Current.Dispose();
            //        mLoadTaskMap.Remove(iter.Current.GetURL());
            //
            //    }
            //}
            //mTempLoadTask.Clear();
            using (var list = CollectionObjectPool<HZUnityABLoadAdapter>.AllocList(mLoadTaskMap.Values))
            using (var iter = list.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    if (iter.Current.OnAdapterUpdate())
                    {
                        iter.Current.Dispose();
                        mLoadTaskMap.Remove(iter.Current.GetURL());
                    }
                }
            }

            //是否存在加载任务
            bool isLoading = false;
            if (mLoadTaskMap.Count == 0)
            {
                using (var item = mABMap.GetEnumerator())
                {
                    while (item.MoveNext())
                    {
                        if (item.Current.Value.BundleStatus == HZUnityAssetBundle.Status.LoadAsset || item.Current.Value.BundleStatus == HZUnityAssetBundle.Status.LoadDep)
                        {
                            isLoading = true;
                            break;
                        }
                    }
                }

            }
            else
            {
                isLoading = true;
            }

            //检查unload进度
            if (mUnloadOp != null && mUnloadOp.isDone)
            {
                mUnloadOp = null;
            }
            //在合适的时候执行unload
            if (!isLoading && mNeedUnload)
            {
                mUnloadOp = Resources.UnloadUnusedAssets();
                //Debug.LogError("[Resources.UnloadUnusedAssets()]");
                mNeedUnload = false;
            }
        }

        public bool IsInit => mABMap != null;
        public void Init()
        {
            if (IsInit || HZUnityABLoadAdapter.LoadRootPath == null)
            {
                return;
            }
            mABMap = new Dictionary<string, HZUnityAssetBundle>();
            mLoadTaskMap = new Dictionary<string, HZUnityABLoadAdapter>();
            //mTempLoadTask = new List<HZUnityABLoadAdapter>();
            ShaderList = new List<Shader>();
            
            StartLoadManifest();
            
            StartLoadShaderList();
            
            StartLoadPostProcessing();
        }
        void StartLoadManifest()
        {
            HZUnityABLoadAdapter load = new HZUnityABLoadAdapter();
            load.LoadAsync = false;
            load.SetFinishCallBack(OnManifestFinish);
            load.Load("/res/" + GetPlatformForAssetBundles(), null);
            StartCoroutine(CheckLoadManifest(load));
        }
        IEnumerator CheckLoadManifest(HZUnityABLoadAdapter load)
        {
            while (!load.HasDone)
            {
                load.OnAdapterUpdate();
                yield return null;
            }
        }

        public ComputeShader GetComputeShader(string shaderName)
        {
            ComputeShader s = null;
            if (TryGetInShaderCollection(shaderName, out s))
            {
                return s;
            }

            return null;
        }

        public Shader GetShader(string shaderName)
        {
            Shader s = null;

            if (ShaderList.Count > 0)
            {
                foreach (var item in ShaderList)
                {
                    if (shaderName.Equals(item.name, StringComparison.Ordinal))
                    {
                        return item;
                    }
                }
            }
            if (TryGetInShaderCollection(shaderName, out s))
            {
                return s;
            }
            if (s == null)
            {
                s = Shader.Find(shaderName);
            }
            if (s == null)
            {
                Debug.LogError(string.Format("[GetShader]Can't Find: {0}", shaderName));
            }
            return s;
        }
        abstract protected bool TryGetInShaderCollection(string shaderName, out Shader s);
        abstract public bool TryGetInShaderCollection(string shaderName, out ComputeShader s);
        private void OnManifestFinish(HZUnityABLoadAdapter adapter)
        {
            if (adapter.GetAssetBundle() != null)
            {
                Manifest = adapter.GetAssetBundle().LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }
            else
            {
                Debug.LogError("HZUnityAssetBundleManager can not find Manifest");
            }

        }
        void StartLoadShaderList()
        {
            //HZUnityABLoadAdapter load = new HZUnityABLoadAdapter();
            //load.LoadAsync = false;
            //load.SetFinishCallBack(OnShaderListFinish);
            //load.Load("/res/shaderslist.assetbundles", null);
            //StartCoroutine(CheckLoadShaderList(load));

            var mDeps = GetDepList(shaderAB);
            foreach (var mDep in mDeps)
            {
                GetAssetBundle(mDep, null, false);
            }
            
            HZUnityABLoadAdapter shaderload = new HZUnityABLoadAdapter();
            shaderload.LoadAsync = false;
            shaderload.SetFinishCallBack(OnShaderVariantsFinish);
            AddAssetRef(shaderAB);
            shaderload.Load(shaderAB, null);
            StartCoroutine(CheckLoadShaderVariants(shaderload));
        }

        void StartLoadPostProcessing()
        {
            HZUnityABLoadAdapter postProcessingLoad = new HZUnityABLoadAdapter();
            postProcessingLoad.LoadAsync = false;
            postProcessingLoad.SetFinishCallBack(LoadDone);
            AddAssetRef(ppsAB);
            postProcessingLoad.Load(ppsAB, null);
            StartCoroutine(CheckLoadLoadPostProcessing(postProcessingLoad));
        }

        void LoadDone(HZUnityABLoadAdapter adapter)
        {
            var bundle = adapter.GetAssetBundle();
            //mABMap.Add(adapter.GetURL(), adapter.GetHZUnityAssetBundle());
            postprocessresources = adapter.GetAssetBundle();
            UnityEngine.Object postProcess = null;
            if (null != bundle)
            {
                postProcess = bundle.LoadAsset("postprocessresources");
            }
            OnPostProcessingLoadOverHandlerCallBack?.Invoke(postProcess);
        }

        IEnumerator CheckLoadLoadPostProcessing(HZUnityABLoadAdapter load)
        {
            while (!load.HasDone)
            {
                load.OnAdapterUpdate();
                yield return null;
            }
        }

        IEnumerator CheckLoadShaderVariants(HZUnityABLoadAdapter load)
        {
            while (!load.HasDone)
            {
                load.OnAdapterUpdate();
                yield return null;
            }
        }
        private void OnShaderVariantsFinish(HZUnityABLoadAdapter adapter)
        {
            if (adapter.GetAssetBundle() != null)
            {
                //mABMap.Add(adapter.GetURL(), adapter.GetHZUnityAssetBundle());
                shadervariants = adapter.GetAssetBundle();
                //var shaderobject = ab.LoadAllAssets();
                //foreach (var sb in shaderobject)
                //{
                //    var item = sb as Shader;
                //    if (item != null)
                //    {
                //        ShaderList.Add(item);
                //    }
                //}
                //ShaderVariantCollection svc = ab.LoadAsset<ShaderVariantCollection>("ShaderVariants");
                //if (svc == null)
                //{
                //    Debug.LogError("ShaderVariantCollection can not be Found ");
                //    return;
                //}
                //svc.WarmUp();
                this.OnLoadShaderCollection();

                if (OnShaderListLoadOverCallBack != null)
                {
                    OnShaderListLoadOverCallBack.Invoke(shadervariants);
                }
            }
            else
            {
                Debug.LogWarning("HZUnityAssetBundleManager can not find ShaderVariant");
            }

        }

        protected abstract void OnLoadShaderCollection();

        IEnumerator CheckLoadShaderList(HZUnityABLoadAdapter load)
        {
            while (!load.HasDone)
            {
                load.OnAdapterUpdate();
                yield return null;
            }
        }

        public static HZUnityAssetBundleManager GetInstance()
        {
            return mInstance;
        }
        public void UnloadUnusedAssets()
        {
            if (mUnloadOp == null)
            {
                mNeedUnload = true;
            }
        }
        protected static FastString fs = new FastString();
        public List<string> GetDepList(string name)
        {
            UnityEngine.Profiling.Profiler.BeginSample("--HzAssetBundleManager.GetDepList--");
#if ((UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE) && !OPEN_STACK_TRACE
            if (name.ToLower() != name)
            {
                Debug.LogError("[HZUnityAssetBundleManager]assetbundle name must be lower!" + name);
            }
#endif

            UnityEngine.Profiling.Profiler.BeginSample("--HzAssetBundleManager.SetAndReplace--");
            string dep_key = ResNameToNoResName(name);
            UnityEngine.Profiling.Profiler.EndSample();
            if (!mDepsList.ContainsKey(name))
            {
                string[] deps = null;

                if (Manifest != null)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("--GetAllDependencies--");
                    deps = Manifest.GetAllDependencies(dep_key);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
                else
                {
                    deps = new string[0];
                }

                List<string> ds = new List<string>(deps.Length);
                for (int i = 0; i < deps.Length; i++)
                {
                    if (IsAddToDeps(deps[i]))
                    {
                        UnityEngine.Profiling.Profiler.BeginSample("--HzAssetBundleManager.SetAndReplace1--");
                        string key = NoResNameToResName(deps[i]);
                        UnityEngine.Profiling.Profiler.EndSample();
                        ds.Add(key);
                    }
                }
                mDepsList[name] = (ds);
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return mDepsList[name];
        }

        protected Dictionary<string, string> _ResNameToNoResName = new Dictionary<string, string>();
        protected Dictionary<string, string> _NoResNameToResName = new Dictionary<string, string>();
        string ResNameToNoResName(string content)
        {
            string key = null;
            if (!_ResNameToNoResName.TryGetValue(content, out key))
            {
                fs.Set(content);
                fs.Replace("/res/", "");
                key = fs.ToString();
                _ResNameToNoResName.Add(content, key);
            }
            return key;
        }

        string NoResNameToResName(string content)
        {
            string key = null;
            if (!_NoResNameToResName.TryGetValue(content, out key))
            {
                fs.Set("/res/");
                fs.Append(content);
                key = fs.ToString();
                _NoResNameToResName.Add(content, key);
            }
            return key;
        }

        bool IsAddToDeps(string dep)
        {
            if (dep != "shaderslist.assetbundles"
                && dep != "shadervariants.assetbundles"
                && dep != "postprocessresources.assetbundles")
            {
                return true;
            }
            return false;
        }
        public void GetAssetBundle(string name, HZUnityABLoadAdapter.HZUnityLoadAdapterCallBack callBack, bool async = true, string childAB = null)
        {
#if ((UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE)&&!OPEN_STACK_TRACE
            if (name.ToLower() != name)
            {
                Debug.LogError("[HZUnityAssetBundleManager]assetbundle name must be lower!" + name);
            }
#endif
            UnityEngine.Profiling.Profiler.BeginSample("--HZUnityAssetBundleManager.GetAssetBundle--");
            HZUnityAssetBundle ab = null;
            HZUnityABLoadAdapter adapter = null;
            if (mABMap.TryGetValue(name, out ab))   //从AB中寻找AssetBundle.
            {
                if (!string.IsNullOrEmpty(childAB))
                {
                    ab.MarkAsDeps(childAB);
                }
                if (callBack != null)
                {
                    callBack.Invoke(ab);
                    //return null;
                }
            }
            else if (async && mLoadTaskMap.TryGetValue(name, out adapter)) //从正在加载的map中寻找.
            {
                if (callBack != null)
                {
                    adapter.AddCallBack(callBack, childAB);
                }
                //return null;
            }
            else //创建加载器.
            {
                UnityEngine.Profiling.Profiler.BeginSample("--new HZUnityABLoadAdapter--");
                HZUnityABLoadAdapter load = new HZUnityABLoadAdapter();
                UnityEngine.Profiling.Profiler.EndSample();
                load.LoadAsync = async;
                load.SetFinishCallBack(OnAdapterFinish, childAB);
                load.Load(name, callBack);
                if (async)
                {
                    mLoadTaskMap.Add(name, load);
                }
                else
                {
                    load.OnAdapterUpdate();
                }
                //return load;
            }
            UnityEngine.Profiling.Profiler.EndSample();
            //如果是依赖包的话，应该在加载之处就标记是谁要求加载这个依赖包的，这样才不会在加载完之前未标记的时候被unload掉


            //return null;
        }
        public AssetBundle GetAssetBundle(string name)
        {
#if (UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE
            if(name.ToLower() != name)
            {
                Debug.LogError("[HZUnityAssetBundleManager]assetbundle name must be lower!" + name);
            }
#endif
            HZUnityAssetBundle ret = null;
            mABMap.TryGetValue(name, out ret);
            return ret.AssetBundle;
        }
        public HZUnityAssetBundle GetHZUnityAssetBundle(string name)
        {
#if (UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE
            if(name.ToLower() != name)
            {
                Debug.LogError("[HZUnityAssetBundleManager]assetbundle name must be lower!" + name);
            }
#endif
            HZUnityAssetBundle ret = null;
            mABMap.TryGetValue(name, out ret);
            if (ret == null)
            {
                return null;
            }
            return ret;
        }
        public bool AddAssetBundle(string name, HZUnityAssetBundle ab)
        {
            if (!string.IsNullOrEmpty(name) && ab != null)
            {
                mABMap.Add(name, ab);
                return true;
            }

            Debug.Log("HZUnityAssetBundleManager AddAssetBundle Error: Invaild Data");

            return false;
        }
        
        public void UnloadAssetBundleImmediate(string name, bool isUnloadAll, bool force = false)
        {
            // 内部自动处理
            HZUnityAssetBundle ab = null;
            if (mABMap.TryGetValue(name, out ab))
            {
                if (ab != null)
                {
                    //TODO 销毁HZUnityAssetBundle，主要是缓存和prefab
                    if (ab.Unload(isUnloadAll, force))
                    {
                        mABMap.Remove(name);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[策划，测试请无视]UnloadAssetBundleImmediate Error, bundle not exists: " + name);
            }
        }
        public void LoadDep(string name, bool loadABASync, bool loadAssetASync, HZUnityAssetBundle.LoadResCallBack callback, object userdata, string childAB)
        {
            HZUnityAssetBundleManager.GetInstance().GetAssetBundle(name, (HZUnityAssetBundle mfab) =>
            {
                //if (mfab != null)
                //{
                //    mfab.MarkAsDeps(childAB);
                //}
                callback(name, null, userdata, mfab != null);
            }, loadABASync, childAB);
        }
        public void LoadAsset(string name, string asset, bool loadABASync, bool loadAssetASync, HZUnityAssetBundle.LoadResCallBack callback, object userdata, System.Type type = null)
        {
            GetAssetBundle(name, (HZUnityAssetBundle mfab) =>
            {
                if (mfab != null)
                {
                    mfab.GetAsset(asset, loadAssetASync, callback, userdata, type);
                }
                else
                {
                    callback(asset, null, userdata, false);
                }
            }, loadABASync);
        }
        public void LoadAsset(string name, bool loadABASync, bool loadAssetASync, HZUnityAssetBundle.LoadResCallBack callback, object userdata, System.Type type = null)
        {
#if (UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE
            if(name.ToLower() != name)
            {
                Debug.LogError("[HZUnityAssetBundleManager]assetbundle name must be lower!" + name);
            }
#endif
            int starIndex = name.LastIndexOf('/') + 1;
            int length = name.Length - starIndex - ".assetbundles".Length;
            string objectName = name.Substring(starIndex, length);
            LoadAsset(name, objectName, loadABASync, loadAssetASync, callback, userdata, type);
        }

        private readonly Dictionary<string, int> assetRef = new Dictionary<string, int>();

        public void AddAssetRef(string refName)
        {
            int i;
            if (assetRef.TryGetValue(refName, out i))
            {
                assetRef[refName] = i + 1;
            }
            else
            {
                assetRef[refName] = 1;
            }
        }

        public void RemoveAssetRef(string refName)
        {
            int refCount;  
            if (assetRef.TryGetValue(refName, out refCount))
            {
                if (refCount >= 0)
                {
                    var newCount = refCount - 1;
                    assetRef[refName] = newCount;
                    if (newCount == 0)
                    {
                        UnloadAssetBundleImmediate(refName, false);
                    }
                }
                else
                {
                    Debug.LogError("RefCount error:" + refName + " refCount:"+ refCount);
                }
            }
            else
            {
                Debug.LogError("RemoveRef error: " + refName);
            }
        }

        public int GetRef(string refName)
        {
            int refCount = 0;
            if (!assetRef.TryGetValue(refName, out refCount))
            {
                Debug.LogError("not found ref of refName:" + refName);
            }
            return refCount;
        }

        public void AddRefWithDeps(string refName)
        {
            var deps = GetDepList(refName);
            AddAssetRef(refName);
            foreach (var va in deps)
            {
                AddAssetRef(va);
            }
        }

        public void RemoveRefWithDeps(string refName)
        {
            var deps = GetDepList(refName);
            RemoveAssetRef(refName);
            foreach (var va in deps)
            {
                RemoveAssetRef(va);
            }
        }

#if UNITY_EDITOR && RES_ANLYZE
        public Dictionary<string, HZUnityAssetBundle> GetAllLoadedBundles()
        {
            return mABMap;
        }

        public int GetRefCount(string name)
        {
            int refCount = 0;
            foreach (var va in assetRef)
            {
                if (va.Key.Contains(name))
                {
                    return va.Value;
                }
                else if(name.Contains(va.Key))
                {
                    return va.Value;
                }
            }

            return refCount;
        }

        public string GetRefInfo()
        {
            string content = "ResInfo:\n";
            foreach (var va in assetRef)
            {
                content += string.Format("name:{0} ref:{1} \n",va.Key,va.Value);
            }
            return content;
        }
#endif
        private void OnAdapterFinish(HZUnityABLoadAdapter adapter)
        {
            HZUnityAssetBundle mfab = adapter.GetHZUnityAssetBundle();
            if (mfab != null)
            {
                mABMap.Add(adapter.GetURL(), mfab); 
            }

            if (mLoadTaskMap.ContainsKey(adapter.GetURL()))
            {
                mLoadTaskMap.Remove(adapter.GetURL());
            }
            else if (adapter.LoadAsync)
            {
                Debug.Log("HZUnityAssetBundleManager can not find Adapter");
            }

            adapter.DispatchCallBack();
        }
        public bool ContainsBundle(string name) { return mABMap.ContainsKey(name); }

        public static string GetPlatformForAssetBundles()
        {
            RuntimePlatform platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WebGLPlayer:
                    return "webgl";
                //                 case RuntimePlatform.OSXWebPlayer:
                //                 case RuntimePlatform.WindowsWebPlayer:
                //                     return "standalonewindows";
                case RuntimePlatform.WindowsEditor:
#if UNITY_ANDROID
                return "android";
#elif UNITY_IOS
                    return "ios";
#else
                    return "standalonewindows";
#endif
                case RuntimePlatform.WindowsPlayer:
                    return "standalonewindows";
                case RuntimePlatform.OSXEditor:
#if UNITY_IOS
				return "ios";
#else
                    return "standalonewindows";
#endif
                case RuntimePlatform.OSXPlayer:
                    return "osx";
                default:
                    return null;
            }
        }
    }
}