using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace DeepCore.Unity3D
{
    public class HZUnityAssetBundleLoader : HZUnityLoadIml
    {

        private static Action<string> mOnBeginLoadData;

        //public static event Action<string> OnBegionLoadData { add { mOnBeginLoadData += value; } remove { mOnBeginLoadData -= value; } }

        public enum HZUnityLoadType
        {
            MPQ,
            WWW
        }

        public static string UNITY_RES_SUFFIXS
        {
            get
            {
                string ret = "";
                foreach (var elem in gUnityResSuffixs)
                {
                    ret += elem + ";";
                }
                return ret;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    string [] suffix = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    gUnityResSuffixs.Clear();
                    foreach (var elem in suffix)
                    {
                        gUnityResSuffixs.Add(value.ToLower());
                    }
                }
            }
        }

        private static HashSet<string> gUnityResSuffixs = new HashSet<string>();
        public static HZUnityLoadType DefaultLoadType { get; set; }
        private HZUnityLoadType mLoadType;
        private AssetBundle mAB = null;
        private System.IO.Stream BundleStream = null;
        private string mErrorLog = null;
        private bool mHasDone = false;
		//System.Diagnostics.Stopwatch sw;

        #region WWW

        private WWW m3W = null;
		private string url;

        #endregion

        #region MPQ
    
        private AssetBundleCreateRequest mABRequest = null;

        #endregion


        public HZUnityAssetBundleLoader()
        {
            mLoadType = DefaultLoadType;
        }

        public HZUnityAssetBundleLoader(HZUnityLoadType loadType)
        {
            mLoadType = loadType;
        }

        public override float GetProgress()
        {
            if(mHasDone)
            {
                return 1;
            }


            if (mAB != null)
            {
                return 1;
            }
            if (m3W != null)
            {
                return m3W.progress;
            }

            if (mABRequest != null)
            {
                return mABRequest.progress;
            }
            return 0;
        }
        public override bool IsLoadFinish()
        {
            if(mHasDone) { return mHasDone; }

            if (m3W != null && m3W.isDone)
            {
                OnLoadFinish();
                return true;
            }

            if (mABRequest != null && mABRequest.isDone)
            {
                OnLoadFinish();
                return true;
            }

            return false;
        }
        public override void Load(string url)
        {
            //string suffix = System.IO.Path.GetExtension(url);
            //if (gUnityResSuffixs.Contains(suffix))
            //{
            //    mLoadType = HZUnityLoadType.WWW;
            //}
			//sw = System.Diagnostics.Stopwatch.StartNew();
			this.url = url;
            //StringBuilder sb = new StringBuilder();
            //sb.Length = 0;
            //sb.Append("mpq://");
            //sb.Append(url);

            //if (mOnBeginLoadData != null)
            //{
            //    mOnBeginLoadData.Invoke(sb.ToString());
            //}
            switch(mLoadType)
            {
                case HZUnityLoadType.WWW:
                    if (!mLoadAsync)
                    {
                        mAB = UnityDriver.UnityInstance.LoadAssetBundleImmediate(url,out BundleStream);
                        mHasDone = true;
                        if (mAB == null) { mErrorLog = "LoadAssetBundleImmediate Error" + url; }
                    }
                    else
                    {
                        // m3W = new WWW(url);
                        mABRequest = AssetBundle.LoadFromFileAsync(url);
                    }
                    break;
                case HZUnityLoadType.MPQ:

                    //url = sb.ToString();

                    if(!mLoadAsync)
                    {
                        if (mAB == null)
                        {
                            mErrorLog = "LoadAssetBundleImmediate Error" + url;
                        }
                        mAB = UnityDriver.UnityInstance.LoadAssetBundleImmediate(url, out BundleStream);
                        mHasDone = true;
                       
                    }
                    else
                    {
                        if (UnityDriver.LOAD_ASSETBUNDLE_USE_STREAM)
                        {
                            if (UnityDriver.UnityInstance.TryOpenStream(url, out BundleStream))
                            {
                                if (null == BundleStream)
                                {
                                    Debugger.LogError("Stream is null please check path:" + url);
                                }
                                try
                                {
                                    var request = AssetBundle.LoadFromStreamAsync(BundleStream, 0, 128 * 1024);
                                    mABRequest = request;
                                }
                                catch (Exception e)
                                {
                                    mErrorLog = e.ToString() + "LoadAssetBundle Error: " + url;
                                    mHasDone = true;
                                }
                            }
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem((obj) =>
                            {
                                if (UnityDriver.UnityInstance.TryLoadData(url, out var bin))
                                {
                                    UnityHelper.MainThreadInvoke(() =>
                                    {
                                        mABRequest = AssetBundle.LoadFromMemoryAsync(bin);
                                        if (mABRequest == null)
                                        {
                                            mErrorLog = "LoadAssetBundle Error: " + url;
                                            mHasDone = true;
                                        }
                                    });
                                }
                            });
                        }
                    }

                   
                    break;
            }
        }
        public override string GetErrorLog()
        {
            return mErrorLog;
        }
        public override bool IsLoadError()
        {
            if(!string.IsNullOrEmpty(mErrorLog))
            {
                return true;
            }

            if (m3W?.error != null)
            {
                mErrorLog = m3W.error.ToString() + m3W.url;
                mHasDone = true;
            }
            return !string.IsNullOrEmpty(mErrorLog);
        }
        public override AssetBundle GetAssetBundle()
        {
            return mAB;
        }
        public override void Dispose()
        {
            mHasDone = true;
            if(m3W != null)
            {
                m3W.Dispose();
                m3W = null;
            }

            mABRequest = null;
            mAB = null;
        }
        protected virtual void OnLoadFinish()
        {
            if(!string.IsNullOrEmpty(mErrorLog))
            {
                mAB = null;
                m3W?.Dispose();
            }
            else
            {
                if (m3W != null)
                {
                    mAB = m3W.assetBundle;
                    m3W.Dispose();
                }

                if (mABRequest != null)
                {
                    mAB = mABRequest.assetBundle;
                    mABRequest = null;
                }
            }

			//sw.Stop();
			//Debug.LogError("[yyyyyyyyyyyyyyyy] " + url.ToString() + " "+sw.ElapsedMilliseconds/1000f);
            mHasDone = true;
        }

        public override Stream GetAssetBundleStream()
        {
            return BundleStream;  
        }
    }
}