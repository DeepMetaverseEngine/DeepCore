using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepCore.Unity3D
{
    public partial class FuckAssetLoader : CustomYieldInstruction
    {
        private static readonly HashMap<int, FuckAssetLoader> sAllLoader = new HashMap<int, FuckAssetLoader>();

        public static void GetAllLoader()
        {
            Debug.LogError(sAllLoader.Count);
        }
        public static FuckAssetLoader GetLoader(int id)
        {
            FuckAssetLoader fuckAssetLoader = null;
            sAllLoader.TryGetValue(id,out fuckAssetLoader);
            return fuckAssetLoader;
        }

        private FuckAssetLoader()
        {
            ID = UnityHelper.GenIntID();
            sAllLoader.Add(ID, this);
        }

        public int ID { get; private set; }

        public static int MaxAsyncLoadingLoader = 5;

        public object UserData;

        private int _Priority = 0;

        public bool DonotLoadAsset { get; set; }

        public override bool keepWaiting
        {
            get { return !IsDone; }
        }

        public override string ToString()
        {
            return string.Format("[{0} {1}] ", BundleName, IsSuccess);
        }

        public bool IsGameObject
        {
            get { return AssetObject is GameObject; }
        }

        public bool IsAudioClip
        {
            get { return AssetObject is AudioClip; }
        }

        public bool IsDiscard { get; private set; }

        public void Discard()
        {
            IsDiscard = true;
            CancelLoad(ID);
        }

        /// <summary>
        /// 表明该资源是一个通过link方式加载进来的
        /// </summary>
        public bool IsLinkedLoad { get; private set; }


        public bool IsSuccess
        {
            get
            {
                if (DonotLoadAsset && Bundle != null)
                {
                    return true;
                }
                return AssetObject;
            }
        }

        public Object AssetObject { get; private set; }

        public bool IsDone { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool ActualImmediate { get; private set; }

        public HZUnityAssetBundle Bundle { get; private set; }

        #region 启动参数

        public string BundleName { get; private set; }
        public string AssetName { get; private set; }
        public bool Async { get; private set; }

        #endregion

        private Queue<FuckAssetLoader> mLinkedLoader;
        private int mNextDepIndex;
        private List<string> mDeps;
        private Action<FuckAssetLoader> mLoadAction;

        private static readonly HashMap<string, FuckAssetLoader> sLoadingLoader = new HashMap<string, FuckAssetLoader>();
        //private static readonly Queue<FuckAssetLoader> sPausingLoader = new Queue<FuckAssetLoader>(5);
        private static readonly LinkedList<FuckAssetLoader> sPausingLoader = new LinkedList<FuckAssetLoader>();

        public string[] GetDeps()
        {
            if (mDeps != null)
            {
                return mDeps.ToArray();
            }

            return new string[0];
        }

        private static void TryStartPausingLoader()
        {
            while (MaxAsyncLoadingLoader - sLoadingLoader.Count > 0 && sPausingLoader.Count > 0)
            {
                var cur = sPausingLoader.First.Value;
                sPausingLoader.RemoveFirst();
                if (sLoadingLoader.TryGetValue(cur.BundleName, out var loader))
                {
                    loader.AddLinkedLoader(cur);
                }
                else
                {
                    cur.StartLoading();
                }
            }
        }

        public static FuckAssetLoader Load(string bundleName, Action<FuckAssetLoader> cb)
        {
            var ret = Load(bundleName, null, true, cb);
            return ret;
        }

        public static FuckAssetLoader Load(string bundleName, string assetName, Action<FuckAssetLoader> cb, bool isDonotLoadAsset = false, int priority = 0)
        {
            var ret = Load(bundleName, assetName, true, cb, isDonotLoadAsset, priority);
            return ret;
        }

        static FuckAssetLoader Load(string bundleName, string assetName, bool async, Action<FuckAssetLoader> cb, bool isDonotLoadAsset = false, int priority = 0)
        {
            UnityEngine.Profiling.Profiler.BeginSample("--new FuckAssetLoader--");
            FuckAssetLoader mAssetLoader = null;
#if UNITY_2017_2_OR_NEWER
            mAssetLoader = Get(bundleName, assetName, async, cb, isDonotLoadAsset, priority);
#endif
            UnityEngine.Profiling.Profiler.EndSample();

            if (isDonotLoadAsset)
            {
                mAssetLoader.DonotLoadAsset = true;
            }
            else
            {
                mAssetLoader.DonotLoadAsset = bundleName.EndsWith(".unity3d");
            }

            return mAssetLoader.StartLoading();
        }

        private static Dictionary<string, string> nameDict = new Dictionary<string, string>();

        public static string GetAssetNameFromBundleName(string bundleName)
        {
            string rtn;
            if (!nameDict.TryGetValue(bundleName, out rtn))
            {
                rtn = Path.GetFileNameWithoutExtension(bundleName);
                nameDict[bundleName] = rtn;
            }

            return rtn;
        }

        public FuckAssetLoader SetComplete(Object obj)
        {
            OnAssetLoadCallBack(AssetName, obj, null, obj);
            return this;
        }

#if UNITY_EDITOR
        [SLua.DoNotToLua]
#endif
        public FuckAssetLoader(string bundleName,
            string assetName = null,
            bool async = true,
            Action<FuckAssetLoader> cb = null, int priority = 0) : this()
        {
            Async = async;
            mLoadAction = cb;
            BundleName = bundleName;
            AssetName = assetName;
            _Priority = priority;

#if ((UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE) && !OPEN_STACK_TRACE
            if (BundleName != null && BundleName.ToLower() != BundleName)
            {
                Debug.LogError("[FuckAssetLoader] assetbundle name must be lower!" + BundleName);
            }
#endif
            //BundleName = BundleName.ToLower();

            if (BundleName != null && string.IsNullOrEmpty(AssetName))
            {
                AssetName = GetAssetNameFromBundleName(BundleName);
            }
        }

        public FuckAssetLoader StartLoading()
        {
            UnityEngine.Profiling.Profiler.BeginSample("--FuckAssetLoader StartLoading--");
            if (string.IsNullOrEmpty(BundleName))
            {
                OnAssetLoadCallBack(null, null, null, false);
                return this;
            }

            if (!Async)
            {
                InternalStartLoading();
                return this;
            }

            FuckAssetLoader loader;
            if (sLoadingLoader.TryGetValue(BundleName, out loader))
            {
                loader.AddLinkedLoader(this);
            }
            else
            {
                if (sLoadingLoader.Count > MaxAsyncLoadingLoader)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("--InsertWithPriority--");
                    InsertWithPriority(sPausingLoader, this);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
                else
                {
                    UnityEngine.Profiling.Profiler.BeginSample("--InternalStartLoading--");
                    InternalStartLoading();
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return this;
        }

#if UNITY_EDITOR
        [SLua.DoNotToLua]
#endif
        public void AddRef()
        {
            HZUnityAssetBundleManager.GetInstance().AddRefWithDeps(BundleName);
        }
#if UNITY_EDITOR
        [SLua.DoNotToLua]
#endif
        public void Unload()
        {
            sAllLoader.TryRemove(this.ID,out _);
            if (IsDone)
            {
                HZUnityAssetBundleManager.GetInstance().RemoveRefWithDeps(BundleName);
#if UNITY_2017_2_OR_NEWER
                Recycle();
#endif
            }
            else
            {
                Discard();
            }
        }

        bool CancelLoad(int loaderID)
        {
            var node = sPausingLoader.First;
            LinkedListNode<FuckAssetLoader> targetNode;
            while (null != node)
            {
                if (node.Value.ID == loaderID)
                {
                    targetNode = node;
                    break;
                }
                node = node.Next;
            }

            if (null != node)
            {
                sPausingLoader.Remove(node);
                return true;
            }


            return false;
        }

        // <summary>
        /// 找到同一优先级中的最后一个
        // <summary>
        void InsertWithPriority(LinkedList<FuckAssetLoader> linkedList, FuckAssetLoader fuckAssetLoader)
        {
            if (linkedList.Count <= 0)
            {
                linkedList.AddFirst(fuckAssetLoader);
                return;
            }

            var node = linkedList.First;
            int priority = fuckAssetLoader._Priority;
            if (node.Value._Priority < priority)//大于所有的
            {
                linkedList.AddFirst(fuckAssetLoader);
                return;
            }
            else if (linkedList.Last.Value._Priority >= priority)//小于等与所有的
            {
                linkedList.AddLast(fuckAssetLoader);
                return;
            }

            //有同一优先级：找到同一优先级的最后一个
            //无同一优先级：找到下一优先级的第一个并插，如果当前优先级为所有优先级的最后一个，则找到队尾
            while (null != node)
            {
                if (node.Value._Priority >= priority
                    && null != node.Next
                    && node.Next.Value._Priority < priority)
                {
                    linkedList.AddAfter(node, fuckAssetLoader);
                    return;
                }
                node = node.Next;
            }

            linkedList.AddLast(fuckAssetLoader);
        }

        private void OnAssetLoadCallBack(string name, Object o, object userdata, bool isLoadOk)
        {
            if (mInStartLoading && Async)
            {
                ActualImmediate = true;
            }

            if (!isLoadOk)
            {
                OnLoadError("Asset Load Error");
            }
            else
            {
                OnLoadSuccess(o);
            }
        }

        private void OnBundleLoadCallBack(HZUnityAssetBundle ab)
        {
            if (ab == null)
            {
                OnLoadError("AssetBundle Load Error ");
            }
            else
            {
                Bundle = ab;
                if (!DonotLoadAsset)
                {
                    ab.GetAsset(AssetName, Async, OnAssetLoadCallBack, null);
                }
                else
                {
                    if (mInStartLoading && Async)
                    {
                        ActualImmediate = true;
                    }

                    OnLoadFinish();
                }
            }
        }

        Dictionary<string, int> _AllDeps = new Dictionary<string, int>();
        void LoadAllDeps()
        {
            _AllDeps.Clear();
            foreach (var va in mDeps)
            {
                _AllDeps.Add(va, 0);
            }
            if (_AllDeps.Count <= 0)
            {
                HZUnityAssetBundleManager.GetInstance().GetAssetBundle(BundleName, OnBundleLoadCallBack, Async);
            }
            else
            {
                foreach (var va in mDeps)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("--LoadDep--");
                    HZUnityAssetBundleManager.GetInstance().LoadDep(va, Async, Async, LoadDepDone, null, BundleName);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        void LoadDepDone(string bundlePath, Object o, object userdata, bool isLoadOk)
        {
            if (!isLoadOk)
            {
                Debug.LogError(this + "LoadDep " + bundlePath);
            }
            _AllDeps.Remove(bundlePath);
            bool isAllLoadDone = _AllDeps.Count <= 0;
            if (isAllLoadDone)
            {
                HZUnityAssetBundleManager.GetInstance().GetAssetBundle(BundleName, OnBundleLoadCallBack, Async);
            }
        }
        private bool mInStartLoading;

        private void InternalStartLoading()
        {
            mInStartLoading = true;
#if ((UNITY_EDITOR && !UNITY_ANDROID) || UNITY_STANDALONE) && !OPEN_STACK_TRACE
            Debug.Log(this + "StartLoading " + sLoadingLoader.Count);
#endif
            sLoadingLoader.Add(BundleName, this);
            mNextDepIndex = 0;
            mDeps = HZUnityAssetBundleManager.GetInstance().GetDepList(BundleName);
            UnityEngine.Profiling.Profiler.BeginSample("--LoadAllDeps--");
            LoadAllDeps();
            UnityEngine.Profiling.Profiler.EndSample();
            mInStartLoading = false;
        }

        /// <summary>
        /// 链接一个新的FuckAssetLoader, 加载完毕后调用该loader的OnBundleLoadCallBack
        /// </summary>
        /// <returns></returns>
        private void AddLinkedLoader(FuckAssetLoader loader)
        {
            if (mLinkedLoader == null)
            {
                mLinkedLoader = new Queue<FuckAssetLoader>(2);
            }

            mLinkedLoader.Enqueue(loader);
        }

        private void OnLoadError(string errMessage)
        {
            ErrorMessage = errMessage;
            if (!string.IsNullOrEmpty(BundleName))
            {
                Debug.LogError(this + errMessage);
            }

            OnLoadFinish();
        }


        private void OnLoadSuccess(Object obj)
        {
            AssetObject = obj;
            OnLoadFinish();
        }

        private FuckAssetLoader mFirstLinkedLoader;

        /// <summary>
        /// 加载完毕（无论成功或失败）
        /// </summary>
        private void OnLoadFinish()
        {
            if (!IsSuccess && !string.IsNullOrEmpty(BundleName))
            {
                Debug.LogError(this + "loadError");
            }

            if (BundleName != null)
            {
                sLoadingLoader.Remove(BundleName);
            }

            IsDone = true;
            if (!IsDiscard && mLoadAction != null)
            {
                mLoadAction.Invoke(this);
            }

            mLoadAction = null;
            if (mLinkedLoader != null)
            {
                while (mLinkedLoader.Count > 0)
                {
                    var loader = mLinkedLoader.Dequeue();
                    if (!loader.IsDiscard)
                    {
                        loader.IsLinkedLoad = true;
                        loader.Bundle = Bundle;
                        loader.OnAssetLoadCallBack(loader.AssetName, AssetObject, null, IsSuccess);
                        mFirstLinkedLoader = loader;
                    }
                }
            }

            if (IsDiscard)
            {
                if (mFirstLinkedLoader != null)
                {
                    mFirstLinkedLoader.IsLinkedLoad = false;
                }
                else
                {
                    if (AssetObject is AudioClip)
                    {
                        Resources.UnloadAsset(AssetObject);
                    }
                    else
                    {
                        Unload();
                    }
                }
            }

            TryStartPausingLoader();
        }
    }
}