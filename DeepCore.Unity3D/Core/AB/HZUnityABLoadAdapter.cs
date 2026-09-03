using System.Collections.Generic;
using UnityEngine;


namespace DeepCore.Unity3D
{

    /// <summary>
    /// AssetBundle加载适配器.
    /// </summary>
    public class HZUnityABLoadAdapter
    {

        private static string mLoadRootPath;

        public static string LoadRootPath
        {
            get { return mLoadRootPath; }
            set
            {
                mLoadRootPath = value;
            }
        }

        public delegate string HZUnityABPathRemapper(string root,string res);
        public delegate void HZUnityLoadAdapterCallBack(HZUnityAssetBundle ab);
        public delegate void LoadFinishCallBack(HZUnityABLoadAdapter adapter);

        private static HZUnityABPathRemapper mPathRemapper  = null; // 资源路径重定向

        private LoadFinishCallBack OnFinshCallBack = null;
        private HZUnityLoadAdapterCallBack OnLoadCallBack = null;
        private HZUnityLoadIml mIml;
        private string mErrorLog = null;
        private bool mHasDone = false;
        public bool HasDone { get { return mHasDone; } }
        private bool mIsDispose = false;
        private string mURL = null;
        protected bool mLoadAsync = true;
        public bool LoadAsync { set { mLoadAsync = value; } get { return mLoadAsync; } }
        HZUnityAssetBundle mHZUnityAssetBundle = null;
        private HashSet<string> mChildABs = new HashSet<string>();

        private static int mVersionCount = 0;

        public static void SetPathRemapper(HZUnityABPathRemapper remapper)
        {
            mPathRemapper = remapper;
        }

        public HZUnityABLoadAdapter()
        {

        }
        public virtual void Load(string url, HZUnityLoadAdapterCallBack callBack)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.Log("HZUnityLoadAdapter Invalid args: " + url);
            }

            if (callBack != null)
            {
                OnLoadCallBack += callBack;
            }

            mURL = url;

            string loadPath;

            if (mPathRemapper != null)
            {
                loadPath = mPathRemapper(LoadRootPath,url);
                if (!string.IsNullOrEmpty(loadPath))
                {
                    StartLoad(loadPath);
                    return;
                }
            }

            if (!_ShortPathToLongPath.TryGetValue(url,out loadPath))
            {
                loadPath = LoadRootPath + url;
                _ShortPathToLongPath.Add(url,loadPath);
            }

            StartLoad(loadPath);
        }

        static Dictionary<string, string> _ShortPathToLongPath = new Dictionary<string, string>(100);

        public string GetURL()
        {
            return mURL;
        }
        public virtual void SetFinishCallBack(LoadFinishCallBack callBack, string childAB = null)
        {
            if (!string.IsNullOrEmpty(childAB))
            {
                mChildABs.Add(childAB);
            }
            OnFinshCallBack = callBack;
        }
        public virtual void AddCallBack(HZUnityLoadAdapterCallBack callBack, string childAB = null)
        {
            if (!string.IsNullOrEmpty(childAB))
            {
                mChildABs.Add(childAB);
            }
            OnLoadCallBack += callBack;
        }
        public virtual void RemoveCallBack(HZUnityLoadAdapterCallBack callBack)
        {
            OnLoadCallBack -= callBack;
        }
        protected virtual void StartLoad(string loadPath)
        {
            mIml = CreateLoadAdapter();
            mIml.LoadAsync = mLoadAsync;
            //Debug.LogError("[HZUnityABLoadAdapter]"+ loadPath + " " + mLoadAsync);
            mIml.Load(loadPath);
        }
        public bool OnAdapterUpdate()
        {
            if (mHasDone)
            {
                return true;
            }

            return OnUpdateLoad();
        }
        protected virtual bool OnUpdateLoad()
        {
            if (IsLoadFinish())
            {
                OnLoadFinish();
                return true;
            }

            return false;
        }
        public virtual void OnStartLoad()
        {

        }
        public virtual void OnStopLoad()
        {

        }
        public virtual void OnLoadFinish()
        {
            if (OnFinshCallBack != null)
            {
                OnFinshCallBack.Invoke(this);
                OnFinshCallBack = null;
            }

            mHasDone = true;

            if (mIml != null)
            {
                mIml.Dispose();
            }
        }
        public HZUnityAssetBundle GetHZUnityAssetBundle()
        {
            if (mHZUnityAssetBundle == null)
            {
                AssetBundle ab = mIml.GetAssetBundle();
                if(ab != null)
                {
                    mHZUnityAssetBundle = new HZUnityAssetBundle(mURL, ab, mVersionCount++, mChildABs,mIml.GetAssetBundleStream());
                }
            }
            return mHZUnityAssetBundle;
        }
        public virtual void DispatchCallBack()
        {
            if (mIml == null)
            {
                return;
            }

            if (OnLoadCallBack != null)
            {
                HZUnityAssetBundle mfab = GetHZUnityAssetBundle();
                OnLoadCallBack(mfab);
            }
        }
        public virtual float GetProgress()
        {
            if (mHasDone)
            {
                return 1;
            }

            if (mIml != null)
            {
                return mIml.GetProgress();
            }

            return 0;
        }
        public virtual void Dispose()
        {
            if (OnFinshCallBack != null)
            {
                Debug.Log("HZUnityABLoadAdapter dispose before finish!");
            }
            if (mIml != null)
            {
                mIml.Dispose();
                mIml = null;
            }
            mIsDispose = true;
            OnLoadCallBack = null;

        }
        public virtual AssetBundle GetAssetBundle()
        {
            if (mIml != null)
            {
                return mIml.GetAssetBundle();
            }

            return null;
        }
        protected bool IsLoadFinish()
        {
            if (mIsDispose)
            {
                return true;
            }


            if (mIml == null)
            {
                return false;
            }

            if (mIml.IsLoadError())
            {
                OnLoadError(mIml.GetErrorLog());
            }

            if (mIml.IsLoadFinish())
            {
                return true;
            }

            return false;
        }
        protected virtual void OnLoadError(string error)
        {
            mErrorLog = error;
            Debug.Log("HZUnityABLoadAdapter OnLoadError: " + mErrorLog);
        }
        private static HZUnityLoadIml CreateLoadAdapter()
        {
            //工厂类，后续有需求，再新增setFactory.
            return HZUnityLoadImlFactory.GetInstance().CreateLoadIml();
        }
    }
}