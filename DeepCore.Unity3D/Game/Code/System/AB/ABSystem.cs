using System;
using Object = UnityEngine.Object;

namespace Code.System.AB
{
    public static class ABSystem
    {
        public static string RootPath { set; get; }

        public static WrapAsset<T> GetAsset<T>(string bundleUrl, string assetName) where T : Object
        {
            return ABSystemImpl.Inst.GetAsset<T>(bundleUrl, assetName);
        }

        public static void GetAssetAsync<T>(string bundleUrl, string assetName, Action<WrapAsset<T>> callback) where T : Object
        {
            ABSystemImpl.Inst.GetAssetAsync(bundleUrl, assetName, callback);
        }
    }
}
