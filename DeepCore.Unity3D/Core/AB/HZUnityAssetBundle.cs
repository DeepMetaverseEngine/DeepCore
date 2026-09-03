using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeepCore.Unity3D
{
    //依赖在外部维护比较好
    //卸载依赖的逻辑需要用计数器
    public class HZUnityAssetBundle
    {
        public string Name { get; private set; }
        private AssetBundle mAssetBundle = null;
        Stream BundleStream;
        public AssetBundle AssetBundle { get { return mAssetBundle; } }
        Stack<Object> mCache = new Stack<Object>();
        HashSet<string> mChildAB = new HashSet<string>();
        Dictionary<string, Object> mPrefabs = new Dictionary<string, Object>();
        private Status mBundleStatus = Status.NONE;
        public Status BundleStatus { get { return mBundleStatus; } }
        public int Version { get; private set; }

        private AssetBundleRequest Request = null;
        //System.Diagnostics.Stopwatch sw;

        public delegate void LoadResCallBack(string name, Object o, object userdata, bool isLoadOK);
        public class ABCallBackInfo
        {
            public string Name { get; set; }
            public LoadResCallBack CallBack { get; set; }
            public object UserData { get; set; }

            public ABCallBackInfo(string name, LoadResCallBack callback, object userdata)
            {
                this.Name = name;
                this.CallBack = callback;
                this.UserData = userdata;
            }
        }
        List<ABCallBackInfo> mLoadingAssetTask = new List<ABCallBackInfo>();

        public enum Status
        {
            NONE,
            LoadDep,
            LoadDepDone,
            LoadAssetBundle,
            LoadAssetBundleDone,
            LoadAsset,
            LoadAssetDone,
            Unloaded,
        }

        public HZUnityAssetBundle(string name)
        {
            Name = name;
        }

        public HZUnityAssetBundle(string name, AssetBundle ab, int version, HashSet<string> chiladAB = null,Stream stream = null)
        {
            Name = name;
            mAssetBundle = ab;
            BundleStream = stream;
            mBundleStatus = Status.LoadAssetBundleDone;
            Version = version;
            if (chiladAB != null)
            {
                mChildAB = new HashSet<string>(chiladAB);
            }
        }

        bool AddChild(string name)
        {
            return mChildAB.Add(name);
        }

        bool RemoveChild(string name)
        {
            return mChildAB.Remove(name);
        }

        bool GetFromCache(out Object obj)
        {
            if (mCache.Count > 0)
            {
                obj = mCache.Pop();
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        void PushCache(Object obj)
        {
            mCache.Push(obj);
        }

        public Object GetAsset(string name)
        {
            Object obj = null;
            mPrefabs.TryGetValue(name, out obj);
            return obj;
        }

        //public void LoadAsset(string asset, bool isAsync)
        //{
        //    Object obj = null;
        //    //缓存里没有现成的
        //    if (!GetFromCache(out obj))
        //    {

        //    }
        //}
        public void GetAsset(string asset, bool isAsync, LoadResCallBack callback, object userdata, System.Type type = null)
        {
            Object o = null;
            if (mPrefabs.TryGetValue(asset, out o))
            {
                callback(asset, o, userdata, true);
            }
            else
            {
                LoadAsset(asset, isAsync, callback, userdata, type);
            }
        }

        //public void LoadAsset(string asset, bool isAsync)
        //{
        //    if (mBundleStatus == Status.LoadAsset || mBundleStatus == Status.LoadAssetDone)
        //    {
        //        Debug.Log("LoadAsset Done");
        //        return;
        //    }
        //    if (isAsync)
        //    {
        //        Request = mAssetBundle.LoadAssetAsync<Object>(asset);
        //        mBundleStatus = Status.LoadAsset;
        //        HZUnityAssetBundleManager.GetInstance().StartCoroutine(CheckRequestAsset(asset));
        //    }
        //    else
        //    {
        //        mPrefabs[asset] = mAssetBundle.LoadAsset<Object>(asset);
        //        mBundleStatus = Status.LoadAssetDone;
        //    }
        //}

#if UNITY_EDITOR && RES_ANLYZE
        public HashSet<string> GetChildAB()
        {
            return mChildAB;
        }
#endif

        public void LoadAsset(string asset, bool isAsync, LoadResCallBack callback, object userdata, System.Type type = null)
        {
            //TODO 先按照之前的代码只支持一个AB1个物件，以后改成多个
            if (mBundleStatus == Status.LoadAsset || mBundleStatus == Status.LoadAssetDone)
            {
                //Debug.Log("LoadAsset Done");
                AddLoadingAssetTask(asset, callback, userdata);
                return;
            }
            if (type == null)
            {
                type = typeof(Object);
            }
            if (isAsync)
            {
                //sw = System.Diagnostics.Stopwatch.StartNew();
                Request = mAssetBundle.LoadAssetAsync(asset, type);
                mBundleStatus = Status.LoadAsset;
                AddLoadingAssetTask(asset, callback, userdata);
                HZUnityAssetBundleManager.GetInstance().StartCoroutine(CheckRequestAsset(asset));
            }
            else
            {
                //sw = System.Diagnostics.Stopwatch.StartNew();
                if (mAssetBundle == null)
                {
                    int i = 0;
                }
                Object o = mAssetBundle.LoadAsset(asset, type);
                if (o != null)
                {
                    mPrefabs[asset] = o;
                    mBundleStatus = Status.LoadAssetDone;
                    callback(asset, o, userdata, true);
                }
                else
                {
                    mBundleStatus = Status.LoadAssetDone;
                    callback(asset, o, userdata, false);
                }
            }
        }

        void AddLoadingAssetTask(string asset, LoadResCallBack callback, object userdata)
        {
            ABCallBackInfo info = new ABCallBackInfo(asset, callback, userdata);
            mLoadingAssetTask.Add(info);
        }

        void DoLoadAssetCallback(Object asset, bool isLoadOK)
        {
            var iter = mLoadingAssetTask.GetEnumerator();
            while (iter.MoveNext())
            {
                iter.Current.CallBack(iter.Current.Name, asset, iter.Current.UserData, isLoadOK);
            }
            mLoadingAssetTask.Clear();
        }

        public void LoadDeps(bool isAsync, LoadResCallBack callback, object userdata, string childAB)
        {
            //TODO 处理依赖关系
            if (childAB != null)
            {
                mChildAB.Add(childAB);
            }
            if (mBundleStatus == Status.LoadDep)
            {
                AddLoadingAssetTask(null, callback, userdata);
            }
            else if (mBundleStatus == Status.LoadDepDone)
            {
                //Debug.Log("LoadDeps Done");
                callback(null, null, userdata, true);
            }
            else
            {
                //sw = System.Diagnostics.Stopwatch.StartNew();
                if (isAsync)
                {
                    Request = mAssetBundle.LoadAllAssetsAsync();
                    mBundleStatus = Status.LoadDep;
                    AddLoadingAssetTask(null, callback, userdata);
                    HZUnityAssetBundleManager.GetInstance().StartCoroutine(CheckRequestDeps());
                }
                else
                {
                    mAssetBundle.LoadAllAssets();
                    mBundleStatus = Status.LoadDepDone;
                    //sw.Stop();
                    //Debug.LogError("[zzzzzzzzzzzzzzzz] " + Name.ToString() + " "+sw.ElapsedMilliseconds/1000f);
                    callback(null, null, userdata, true);
                }
            }
        }

        public void MarkAsDeps(string childAB)
        {
            //TODO 处理依赖关系
            if (childAB != null)
            {
                mChildAB.Add(childAB);
            }
            mBundleStatus = Status.LoadDepDone;
        }

        bool IsRefed()
        {
            int refCount = HZUnityAssetBundleManager.GetInstance().GetRef(Name);
            return refCount > 0;
        }

        public bool Unload(bool all, bool force = false)
        {
            //TODO 被其他包引用的包不能被Unload
            if (mBundleStatus == Status.NONE || mBundleStatus == Status.LoadAssetBundle || mBundleStatus == Status.LoadDep || mBundleStatus == Status.LoadAsset)
            {
                Debug.LogError("[策划测试请无视]HZUnityAssetBundle Unload Error! Status: " + mBundleStatus);
                return false;
            }
            if (force)
            {
                if (IsRefed())
                {
                    Debug.LogWarning("[策划测试请无视]HZUnityAssetBundle Force Unload Error! Child Referenced :" + Name + " " + mChildAB.Count);
                }
            }
            else
            {
                if (IsRefed())
                {
                    Debug.LogWarning("[策划测试请无视]HZUnityAssetBundle Unload Error! Child Referenced :" + Name + " " + mChildAB.Count);
                    return false;
                }
            }
            //Debug.LogError("[Unload]" + Name + " " + all);
            mBundleStatus = Status.Unloaded;
            mPrefabs.Clear();
            mAssetBundle.Unload(true);
            BundleStream?.Dispose();
            mAssetBundle = null;
            mChildAB.Clear();
            return true;
        }

        IEnumerator<YieldInstruction> CheckRequestDeps()
        {
            //if (Request != null)
            {
                while (!Request.isDone)
                {
                    yield return null;
                }

                mBundleStatus = Status.LoadDepDone;
                Request = null;
                //sw.Stop();
                //Debug.LogError("[zzzzzzzzzzzzzzzz] " + Name.ToString() + " "+sw.ElapsedMilliseconds/1000f);
                DoLoadAssetCallback(null, true);
            }
        }

        IEnumerator<YieldInstruction> CheckRequestAsset(string asset)
        {
            //if (Request != null)
            {
                while (!Request.isDone)
                {
                    yield return null;
                }

                Object o = Request.asset;
                //sw.Stop();
                //Debug.LogError("[zzzzzzzzzzzzzzzz] " + Name.ToString() + " "+sw.ElapsedMilliseconds/1000f);
                if (o != null)
                {
                    mPrefabs[asset] = o;
                    mBundleStatus = Status.LoadAssetDone;
                    Request = null;
                    DoLoadAssetCallback(o, true);
                }
                else
                {
                    mBundleStatus = Status.LoadAssetDone;
                    Request = null;
                    DoLoadAssetCallback(null, false);
                }
            }
        }
    }

}