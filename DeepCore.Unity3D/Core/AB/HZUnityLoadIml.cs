using UnityEngine;
namespace DeepCore.Unity3D
{
    public abstract class HZUnityLoadIml
    {
        protected bool mLoadAsync = true;
        public bool LoadAsync { set { mLoadAsync = value; } get { return mLoadAsync; } }
        public abstract float GetProgress();
        public abstract bool IsLoadFinish();
        public abstract void Load(string url);
        public abstract string GetErrorLog();
        public abstract bool IsLoadError();
        public abstract AssetBundle GetAssetBundle();
        public abstract System.IO.Stream GetAssetBundleStream();
        public abstract void Dispose();
    }
}