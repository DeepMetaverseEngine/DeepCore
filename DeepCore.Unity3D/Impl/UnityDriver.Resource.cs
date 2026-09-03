using DeepCore.IO;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{

    public partial class UnityDriver
    {
        static private string TestDataPath = String.Empty;
        static public void SetTestDataPath(string path)
        {
            TestDataPath = path;
        }
        private static T LoadFromResources<T>(string path) where T : UnityEngine.Object
        {
            int index = path.LastIndexOf(".");

            if (index < 0) { return null; }
            // Unity TextAsset
            string assetpath = path.Substring(0, index);
            while (assetpath.StartsWith("/"))
            {
                assetpath = assetpath.Substring(1);
            }

            try
            {
                T ta = UnityEngine.Resources.Load<T>(assetpath);
                //Debug.Log("LoadFromAsserts ==========> " + assetpath + " --- " + ta);
                return ta;
            }
            catch
            {
                return default(T);
            }

        }
        private static ResourceRequest LoadFromResourcesAsync<T>(string path) where T : UnityEngine.Object
        {
            int index = path.LastIndexOf(".");

            if (index < 0) { return null; }
            // Unity TextAsset
            string assetpath = path.Substring(0, index);
            while (assetpath.StartsWith("/"))
            {
                assetpath = assetpath.Substring(1);
            }

            try
            {
                var ta = UnityEngine.Resources.LoadAsync<T>(assetpath);
                //Debug.Log("LoadFromAsserts ==========> " + assetpath + " --- " + ta);
                return ta;
            }
            catch
            {
                return null;
            }

        }

        private static object LoadObjectFromResources(string path)
        {
            // Unity TextAsset
            string assetpath = path.Substring(0, path.LastIndexOf("."));
            while (assetpath.StartsWith("/"))
            {
                assetpath = assetpath.Substring(1);
            }
            return UnityEngine.Resources.Load(assetpath);
        }
        //-----------------------------------------------------------------------------------------------------------------
        /*
        public static bool LOAD_ASSETBUNDLE_USE_STREAM = true;

        public virtual AssetBundleCreateRequest LoadAssetBundle(string path, out int size)
        {
            size = 0; try
            {
                if (LOAD_ASSETBUNDLE_USE_STREAM)
                {
                    var stream = mDefaultLoader.OpenStream(path);
                    if (stream != null)
                    {
                        size = (int)stream.Length;
                        var request = AssetBundle.LoadFromStreamAsync(stream, 0, 128 * 1024);
                        request.completed += (e) => { stream.Dispose(); };
                        return request;
                    }
                }
                else
                {
                    var bin = mDefaultLoader.LoadData(path);
                    if (bin != null)
                    {
                        size = bin.Length;
                        var request = AssetBundle.LoadFromMemoryAsync(bin);
                        return request;
                    }
                }
            }
            catch (Exception err)
            {
                Assert(false, "LoadAssetBundle : Error " + path + "\n" + err.Message);
            }
            return null;
        }

        public virtual AssetBundleCreateRequest LoadAssetBundle(string path)
        {
            try
            {
                if (LOAD_ASSETBUNDLE_USE_STREAM)
                {
                    var stream = mDefaultLoader.OpenStream(path);
                    if (stream != null)
                    {
                        var request = AssetBundle.LoadFromStreamAsync(stream, 0, 128 * 1024);
                        request.completed += (e) => { stream.Dispose(); };
                        return request;
                    }
                }
                else
                {
                    var bin = mDefaultLoader.LoadData(path);
                    if (bin != null)
                    {
                        var request = AssetBundle.LoadFromMemoryAsync(bin);
                        return request;
                    }
                }
            }
            catch (Exception err)
            {
                Assert(false, "LoadAssetBundle : Error " + path + "\n" + err.Message);
            }
            return null;
        }

        public virtual AssetBundle LoadAssetBundleImmediate(string path)
        {
            try
            {
                if (LOAD_ASSETBUNDLE_USE_STREAM)
                {
                    var stream = mDefaultLoader.OpenStream(path);
                    if (stream != null)
                    {
                        var request = AssetBundle.LoadFromStream(stream);
                        return request;
                    }
                }
                else
                {
                    var bin = mDefaultLoader.LoadData(path);
                    if (bin != null)
                    {
                        return AssetBundle.LoadFromMemory(bin);
                    }
                }
            }
            catch (Exception err)
            {
                Assert(false, "LoadAssetBundleImmediate : Error " + path + "\n" + err.Message);
            }
            return null;
        }

        public virtual void CreateAssetObject(string abpath, string a, Action<GameObject> onFinish)
        {
            throw new NotImplementedException();
        }
        */
        //-----------------------------------------------------------------------------------------------------------------
        /*
        public class UnityResourceLoader : IResourceLoader
        {
            //------------------------------------------------------------------------------------------------------------------------
            private string dataRoot;
            public UnityResourceLoader(string root)
            {
                dataRoot = root;
            }
            public bool TryGetPath(string path, out string suffix)
            {
                if (Resource.IsStartWith(path, TestDataPath, out suffix))
                {
                    return true;
                }
                if (Resource.IsStartWith(path, Resource.PREFIX_RES, out suffix))
                {
                    return true;
                }
                else if (Resource.IsStartWith(path, Resource.PREFIX_JAR, out suffix))
                {
                    return true;
                }
                else if (Resource.IsStartWith(path, Resource.PREFIX_HTTP, out suffix))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public bool IsStartWith(string path)
            {
                return TryGetPath(path, out _);
            }
            public string GetParent(string path)
            {
                path = Resource.FormatPath(path);
                if (path.TryLastIndexOf('/', out var indexR))
                {
                    return path.Substring(indexR);
                }
                if (path.TryLastIndexOf('\\', out var indexL))
                {
                    return path.Substring(indexL);
                }
                return path;
            }

            private bool _TryLoadFromTest(string path, ref byte[] ret)
            {
                string fullpath = TestDataPath + "/" + path;
                try
                {
                    fullpath = System.IO.Path.GetFullPath(fullpath);
                    if (System.IO.File.Exists(fullpath))
                    {
                        ret = File.ReadAllBytes(fullpath);
                        if (ret != null)
                        {
                            if (IsDebug)
                            {
                                Debug.Log("Load Data From Test Path : " + fullpath + " -> " + ret.Length + " (bytes)");
                            }
                            return true;
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err.Message);
                    Debug.LogError("Load Data From Test Path : " + fullpath + " -> " + ret.Length + " (bytes)");
                }
                return false;
            }
            private bool _TryLoadFromResources(string path, ref byte[] ret)
            {
                TextAsset data = LoadFromResources<TextAsset>(path);
                if (data != null)
                {
                    ret = data.bytes;
                    if (IsDebug)
                    {
                        Debug.Log("Load Data From Unity Resources : " + path + " -> " + ret.Length + " (bytes)");
                    }
                    return true;
                }
                return false;
            }
            private bool _TryLoadFromJAR(string path, ref byte[] ret)
            {
                var data = WWWHelper.Instance.getJavaData(path);
                //yield return data;
                if (data != null)
                {
                    ret = data;
                    if (IsDebug)
                    {
                        Debug.Log("Load Data From JAR : " + path + " -> " + ret.Length + " (bytes)");
                    }
                    return true;
                }
                return false;
            }
            private bool _TryLoadFromHttp(string path, ref byte[] ret)
            {
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);
                www.SendWebRequest();
                while (!www.isDone) { System.Threading.Thread.Sleep(1); }
                if (www.error != null)
                {
                    return false;
                }
                else
                {
                    ret = www.downloadHandler.data;
                    return true;
                }
            }


            private async Task<byte[]> _TryLoadFromTestAsync(string path)
            {
                string fullpath = TestDataPath + "/" + path;
                try
                {
                    fullpath = System.IO.Path.GetFullPath(fullpath);
                    if (System.IO.File.Exists(fullpath))
                    {
                        var data = await File.ReadAllBytesAsync(fullpath);
                        if (data != null)
                        {
                            if (IsDebug)
                            {
                                Debug.Log("Load Data From Test Path : " + fullpath + " -> " + data.Length + " (bytes)");
                            }
                        }
                        return data;
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err.Message);
                    Debug.LogError("Load Data From Test Path : " + fullpath + " -> (bytes)");
                }
                return (null);
            }
            private Task<byte[]> _TryLoadFromResourcesAsync(string path)
            {
                var req = LoadFromResourcesAsync<TextAsset>(path);
                if (req != null)
                {
                    var tcs = new TaskCompletionSource<byte[]>();
                    req.completed += (op) =>
                    {
                        try
                        {
                            var ta = req.asset as TextAsset;
                            if (ta != null)
                            {
                                var data = ta.bytes;
                                if (IsDebug)
                                {
                                    Debug.Log("Load Data From Unity Resources : " + path + " -> " + ta.bytes.Length + " (bytes)");
                                }
                                tcs.TrySetResult(data);
                            }
                            else
                            {
                                Debug.LogError($"Load Data From Unity Resources Not Found : {path}");
                                tcs.TrySetResult(null);
                            }
                        }
                        catch (Exception err)
                        {
                            Debug.LogError($"Load Data From Unity Resources Error : {path}");
                            tcs.TrySetException(err);
                        }
                    };
                    return tcs.Task;
                }
                return Task.FromResult<byte[]>(null);
            }
            private Task<byte[]> _TryLoadFromJARAsync(string path)
            {
                if (WWWHelper.Instance.isFileExists(path))
                {
                    return Task.Run(() =>
                    {
                        var data = WWWHelper.Instance.getJavaData(path);
                        if (data != null)
                        {
                            if (IsDebug)
                            {
                                Debug.Log("Load Data From JAR : " + path + " -> " + data.Length + " (bytes)");
                            }
                        }
                        return data;
                    });
                }
                else
                {
                    return Task.FromResult<byte[]>(null);
                }
            }
            private Task<byte[]> _TryLoadFromHttpAsync(string path)
            {
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);
                www.SendWebRequest();
                try
                {
                    return Task.Run(() =>
                    {
                        while (!www.isDone) { System.Threading.Thread.Sleep(1); }
                        if (www.error != null)
                        {
                            return null;
                        }
                        else
                        {
                            return www.downloadHandler.data;
                        }
                    });
                }
                catch (Exception err)
                {
                    return Task.FromResult<byte[]>(null);
                }
            }

            //------------------------------------------------------------------------------------------------------------------------


            private bool _TryOpenFromTest(string path, ref Stream ret)
            {
                string fullpath = TestDataPath + "/" + path;
                try
                {
                    fullpath = System.IO.Path.GetFullPath(fullpath);
                    if (System.IO.File.Exists(fullpath))
                    {
                        ret = new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);// File.ReadAllBytes(fullpath);
                        if (ret != null)
                        {
                            if (IsDebug)
                            {
                                Debug.Log("Load Data From Test Path : " + fullpath + " -> " + ret.Length + " (bytes)");
                            }
                            return true;
                        }
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError(err.Message);
                    Debug.LogError("Load Data From Test Path : " + fullpath + " -> " + ret.Length + " (bytes)");
                }
                return false;
            }
            private bool _TryOpenFromResources(string path, ref Stream ret)
            {
                byte[] data = null;
                if (_TryLoadFromResources(path, ref data))
                {
                    ret = new DeepCore.IO.MemoryStream(data);
                    return true;
                }
                return false;
            }
            private bool _TryOpenFromJAR(string path, ref Stream ret)
            {
                byte[] data = null;
                if (_TryLoadFromJAR(path, ref data))
                {
                    ret = new DeepCore.IO.MemoryStream(data);
                    return true;
                }
                return false;
            }
            private bool _TryOpenFromHttp(string path, ref Stream ret)
            {
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);
                www.SendWebRequest();
                while (!www.isDone) { }
                if (www.error != null)
                {
                    return false;
                }
                else
                {
                    byte[] bs = System.Text.Encoding.UTF8.GetBytes(www.downloadHandler.text);
                    ret = new DeepCore.IO.MemoryStream(bs);
                    return true;
                }
            }

            //------------------------------------------------------------------------------------------------------------------------

            private bool _TryExistDataFromTestPath(string path)
            {
                if (Directory.Exists(TestDataPath))
                {
                    string fullpath = TestDataPath + "/" + path;
                    try
                    {
                        fullpath = System.IO.Path.GetFullPath(fullpath);
                        if (System.IO.File.Exists(fullpath))
                        {
                            return true;
                        }
                    }
                    catch (Exception err)
                    {
                        Debug.LogError("ExitDataFromTestPath Error:" + err.ToString() + path.ToString());
                    }
                }
                return false;
            }
            private bool _TryExistDataFromJAR(string path)
            {
                return WWWHelper.Instance.isFileExists(path);
            }
            private bool _TryExistDataFromHttp(string path)
            {
                return base._TryExistWithHttp(path);
            }

            //"jar:file://" + Application.dataPath + "!/assets/";
            //------------------------------------------------------------------------------------------------------------------------
            public override bool ExistData(string path)
            {
                if (IsStartWith(path, Resource.PREFIX_JAR, out var prefix))
                {
                    return _TryExistDataFromJAR(prefix);
                }
                if (IsAndroid && IsStartWith(path, Application.streamingAssetsPath, out prefix))
                {
                    return _TryExistDataFromJAR(prefix);
                }
                return base.ExistData(path);
            }
            public override Task<bool> ExistDataAsync(string path)
            {
                return Task.FromResult(ExistData(path));
            }
            public override Stream OpenStream(string path)
            {
                Stream stream = null;
                if (IsStartWith(path, PREFIX_RES, out var prefix))
                {
                    _TryOpenFromResources(prefix, ref stream);
                    return stream;
                }
                if (IsStartWith(path, PREFIX_JAR, out prefix))
                {
                    _TryOpenFromJAR(prefix, ref stream);
                    return stream;
                }
                if (IsAndroid && IsStartWith(path, Application.streamingAssetsPath, out prefix))
                {
                    _TryOpenFromJAR(prefix, ref stream);
                    return stream;
                }
                return base.OpenStream(path);
            }
            public override Task<Stream> OpenStreamAsync(string path)
            {
                return Task.FromResult(OpenStream(path));
            }
            public override byte[] LoadData(string path)
            {
                byte[] ret = null;
                if (IsStartWith(path, PREFIX_RES, out var prefix))
                {
                    _TryLoadFromResources(prefix, ref ret);
                    return ret;
                }
                if (IsStartWith(path, PREFIX_JAR, out prefix))
                {
                    _TryLoadFromJAR(prefix, ref ret);
                    return ret;
                }
                if (IsAndroid && IsStartWith(path, Application.streamingAssetsPath, out prefix))
                {
                    _TryLoadFromJAR(prefix, ref ret);
                    return ret;
                }
                return base.LoadData(path);
            }
            public override async Task<byte[]> LoadDataAsync(string path)
            {
                if (IsStartWith(path, PREFIX_RES, out var prefix))
                {
                    return await _TryLoadFromResourcesAsync(prefix);
                }
                if (IsStartWith(path, PREFIX_JAR, out prefix))
                {
                    return await _TryLoadFromJARAsync(prefix);
                }
                if (IsAndroid && IsStartWith(path, Application.streamingAssetsPath, out prefix))
                {
                    return await _TryLoadFromJARAsync(prefix);
                }
                return await base.LoadDataAsync(path);
            }
            //             public override string[] ListFiles(string path, bool fullPath = false)
            //             {
            //                 return base.ListFiles(path, fullPath);
            //             }
            //             public override string[] ListDirectories(string path, bool fullPath = false)
            //             {
            //                 return base.ListDirectories(path, fullPath);
            //             }
            //             public override Task<string[]> ListFilesAsync(string path, bool fullPath = false)
            //             {
            //                 return base.ListFilesAsync(path, fullPath);
            //             }
            //             public override Task<string[]> ListDirectoriesAsync(string path, bool fullPath = false)
            //             {
            //                 return base.ListDirectoriesAsync(path, fullPath);
            //             }
            //             public override string GetParent(string path)
            //             {
            //                 return base.GetParent(path);
            //             }

        }
        */
        // ---------------------------------------------------------------------------------
#if MPQ
        public class MPQAdapterFactory //: MPQDriverFactory
        {
            public MPQAdapterFactory()
            {
                MPQDriverFactory.CreateDriver = (saveRoot, bundleRoot) => new MPQAdapter();
            }
        }
        public class MPQAdapter : MPQDriver
        {
            public MPQAdapter()
            {
            }
            public override long GetAvaliableSpace(string path)
            {
                return long.MaxValue;
            }
            public override long GetTotalSpace(string path)
            {
                return long.MaxValue;
            }
            //             public override bool RunGetFileMD5(string fullname, out string md5)
            //             {
            //                 return sPlatform.NativeGetFileMD5(fullname, out md5);
            //             }
            //             public override bool RunUnzipSingle(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip, MPQUpdater.RemoteFileInfo mpq, AtomicLong process)
            //             {
            //                 return sPlatform.NativeDecompressFile(updater, zip, mpq, process);
            //             }
        }


        // ---------------------------------------------------------------------------------
#endif
        public class WWWHelper
        {
            public static WWWHelper Instance { get; private set; } = new WWWHelper();
            public WWWHelper() { Instance = this; }
            public virtual bool isFileExists(string path) { return false; }
            public virtual byte[] getJavaData(string path) { return null; }
        }
    }


}
