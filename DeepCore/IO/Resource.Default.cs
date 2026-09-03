using DeepCore.Log;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeepCore.IO
{
#if false
    //-----------------------------------------------------------------------------------------------------------------------
    public class DefaultResourceLoader : IResourceLoader
    {
        //-----------------------------------------------------------------------------------------------------
        #region Init
        protected readonly Logger log = LoggerFactory.GetLogger("res");
        protected static string mRoot;
        //protected static MPQFileSystem mMPQ;
        public string PathRoot { get => mRoot; set { SetRoot(value); } }
        //         public static void SetMPQ(MPQFileSystem fs)
        //         {
        //             mMPQ = fs;
        //         }
        public static void SetRoot(string root)
        {
            mRoot = root;
        }
        public DefaultResourceLoader() { }
        public DefaultResourceLoader(string root)
        {
            mRoot = root;
        }
        //         public DefaultResourceLoader(MPQFileSystem fs)
        //         {
        //             mRoot = PREFIX_MPQ;
        //             mMPQ = fs;
        //         }
        #endregion
        //-----------------------------------------------------------------------------------------------------
        public static bool IsStartWith(string path, string prefix, out string suffix)
        {
            if (path.StringStartWithIgnoreCase(prefix))
            {
                suffix = path.Substring(prefix.Length);
                return true;
            }
            suffix = null;
            return false;
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual string GetParent(string path)
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
        public virtual bool ExistPath(string path)
        {
            try
            {
                if (ExistData(path))
                {
                    return true;
                }
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    return (_TryExistDirWithFileSystem(suffix));
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    return (_TryExistDirWithMPQ(suffix));
                }
                else
                {
                    //--------------------------------------------------
                    {
                        if (_TryExistDirWithFileSystem(path)) { return true; }
                    }
                    {
                        if (_TryExistDirWithMPQ(path)) { return true; }
                    }
                    //--------------------------------------------------
                    if (!string.IsNullOrEmpty(mRoot))
                    {
                        if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                        {
                            return (_TryExistDirWithFileSystem(suffix + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                        {
                            return (_TryExistDirWithMPQ(suffix + path));
                        }
                        else
                        {
                            return (_TryExistDirWithFileSystem(mRoot + path));
                        }
                    }
                    //--------------------------------------------------
                }
            }
            catch (Exception err)
            {
                log.Error("ExistPath From : " + path);
                log.Error(err.Message, err);
            }
            return false;
        }
        public virtual bool ExistData(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    return (_TryExistWithFileSystem(suffix));
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    return (_TryExistWithMPQ(suffix));
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    return (_TryExistWithHttp(path));
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    return (_TryExistWithHttp(path));
                }
                else
                {
                    //--------------------------------------------------
                    {
                        if (_TryExistWithFileSystem(path)) { return true; }
                    }
                    {
                        if (_TryExistWithMPQ(path)) { return true; }
                    }
                    //--------------------------------------------------
                    if (!string.IsNullOrEmpty(mRoot))
                    {
                        if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                        {
                            return (_TryExistWithFileSystem(suffix + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                        {
                            return (_TryExistWithMPQ(suffix + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                        {
                            return (_TryExistWithHttp(mRoot + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                        {
                            return (_TryExistWithHttp(mRoot + path));
                        }
                        else
                        {
                            return (_TryExistWithFileSystem(mRoot + path));
                        }
                    }
                    //--------------------------------------------------
                }
            }
            catch (Exception err)
            {
                log.Error("ExistData From : " + path);
                log.Error(err.Message, err);
            }
            return false;
        }
        public virtual Task<bool> ExistDataAsync(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    return Task.FromResult(_TryExistWithFileSystem(suffix));
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    return Task.FromResult(_TryExistWithMPQ(suffix));
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    return Task.FromResult(_TryExistWithHttp(path));
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    return Task.FromResult(_TryExistWithHttp(path));
                }
                else
                {
                    //--------------------------------------------------
                    {
                        if (_TryExistWithFileSystem(path)) { return Task.FromResult(true); }
                    }
                    {
                        if (_TryExistWithMPQ(path)) { return Task.FromResult(true); }
                    }
                    //--------------------------------------------------
                    if (!string.IsNullOrEmpty(mRoot))
                    {
                        if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                        {
                            return Task.FromResult(_TryExistWithFileSystem(suffix + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                        {
                            return Task.FromResult(_TryExistWithMPQ(suffix + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                        {
                            return Task.FromResult(_TryExistWithHttp(mRoot + path));
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                        {
                            return Task.FromResult(_TryExistWithHttp(mRoot + path));
                        }
                        else
                        {
                            return Task.FromResult(_TryExistWithFileSystem(mRoot + path));
                        }
                    }
                    //--------------------------------------------------
                }
            }
            catch (Exception err)
            {
                log.Error("ExistData From : " + path);
                log.Error(err.Message, err);
            }
            return Task.FromResult(false);
        }
        public virtual byte[] LoadData(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryLoadFromFileSystem(suffix, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryLoadFromMPQ(suffix, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    if (_TryLoadFromHttp(path, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    if (_TryLoadFromHttp(path, out var ret)) { return ret; }
                }
                else
                {
                    //--------------------------------------------------
                    {
                        if (_TryLoadFromFileSystem(path, out var ret)) { return ret; }
                    }
                    {
                        if (_TryLoadFromMPQ(path, out var ret)) { return ret; }
                    }
                    //--------------------------------------------------
                    if (!string.IsNullOrEmpty(mRoot))
                    {
                        if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                        {
                            if (_TryLoadFromFileSystem(suffix + path, out var ret)) { return ret; }
                        }
                        else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                        {
                            if (_TryLoadFromMPQ(suffix + path, out var ret)) { return ret; }
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                        {
                            if (_TryLoadFromHttp(mRoot + path, out var ret)) { return ret; }
                        }
                        else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                        {
                            if (_TryLoadFromHttp(mRoot + path, out var ret)) { return ret; }
                        }
                        else
                        {
                            if (_TryLoadFromFileSystem(mRoot + path, out var ret)) { return ret; }
                        }
                    }
                    //--------------------------------------------------
                }
            }
            catch (Exception err)
            {
                log.Error("TryLoadData From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not Load Data : {path}");
            return null;
        }
        public virtual Task<byte[]> LoadDataAsync(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryLoadFromFileSystem(suffix, out var ret)) { return Task.FromResult(ret); }
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryLoadFromMPQ(suffix, out var ret)) { return Task.FromResult(ret); }
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    return _LoadFromHttpAsync(path);
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    return _LoadFromHttpAsync(path);
                }
                //--------------------------------------------------
                {
                    {
                        if (_TryLoadFromFileSystem(path, out var ret)) { return Task.FromResult(ret); }
                    }
                    {
                        if (_TryLoadFromMPQ(path, out var ret)) { return Task.FromResult(ret); }
                    }
                }
                //--------------------------------------------------
                if (!string.IsNullOrEmpty(mRoot))
                {
                    if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                    {
                        if (_TryLoadFromFileSystem(suffix + path, out var ret)) { return Task.FromResult(ret); }
                    }
                    else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                    {
                        if (_TryLoadFromMPQ(suffix + path, out var ret)) { return Task.FromResult(ret); }
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                    {
                        return _LoadFromHttpAsync(mRoot + path);
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                    {
                        return _LoadFromHttpAsync(mRoot + path);
                    }
                    else
                    {
                        if (_TryLoadFromFileSystem(mRoot + path, out var ret)) { return Task.FromResult(ret); }
                    }
                }
                //-----------------------------------   ---------------
            }
            catch (Exception err)
            {
                log.Error("LoadDataAsync From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not Load Data : {path}");
            return Task.FromResult<byte[]>(null);

        }
        public virtual Stream OpenStream(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryOpenFromFileSystem(suffix, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryOpenFromMPQ(suffix, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    if (_TryOpenFromHttp(path, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    if (_TryOpenFromHttp(path, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                {
                    if (_TryOpenFromFileSystem(path, out var ret)) { return ret; }
                }
                {
                    if (_TryOpenFromMPQ(path, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                if (!string.IsNullOrEmpty(mRoot))
                {
                    if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                    {
                        if (_TryOpenFromFileSystem(suffix + path, out var ret)) { return ret; }
                    }
                    else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                    {
                        if (_TryOpenFromMPQ(suffix + path, out var ret)) { return ret; }
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                    {
                        if (_TryOpenFromHttp(mRoot + path, out var ret)) { return ret; }
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                    {
                        if (_TryOpenFromHttp(mRoot + path, out var ret)) { return ret; }
                    }
                    else
                    {
                        if (_TryOpenFromFileSystem(mRoot + path, out var ret)) { return ret; }
                    }
                }
                //--------------------------------------------------
            }
            catch (Exception err)
            {
                log.Error("TryOpenStream From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not Open Stream : {path}");
            return null;
        }
        public virtual Task<Stream> OpenStreamAsync(string path)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryOpenFromFileSystem(suffix, out var ret)) { return Task.FromResult(ret); }
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryOpenFromMPQ(suffix, out var ret)) { return Task.FromResult(ret); }
                }
                else if (IsStartWith(path, PREFIX_HTTPS, out suffix))
                {
                    return _OpenFromHttpAsync(path);
                }
                else if (IsStartWith(path, PREFIX_HTTP, out suffix))
                {
                    return _OpenFromHttpAsync(path);
                }
                //--------------------------------------------------
                {
                    if (_TryOpenFromFileSystem(path, out var ret)) { return Task.FromResult(ret); }
                }
                {
                    if (_TryOpenFromMPQ(path, out var ret)) { return Task.FromResult(ret); }
                }
                //--------------------------------------------------
                if (!string.IsNullOrEmpty(mRoot))
                {
                    if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                    {
                        if (_TryOpenFromFileSystem(suffix + path, out var ret)) { return Task.FromResult(ret); }
                    }
                    else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                    {
                        if (_TryOpenFromMPQ(suffix + path, out var ret)) { return Task.FromResult(ret); }
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTPS, out suffix))
                    {
                        return _OpenFromHttpAsync(mRoot + path);
                    }
                    else if (IsStartWith(mRoot, PREFIX_HTTP, out suffix))
                    {
                        return _OpenFromHttpAsync(mRoot + path);
                    }
                    else
                    {
                        if (_TryOpenFromFileSystem(mRoot + path, out var ret)) { return Task.FromResult(ret); }
                    }
                }
                //--------------------------------------------------
            }
            catch (Exception err)
            {
                log.Error("TryOpenStream From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not Open Stream : {path}");
            return null;
        }
        public virtual string[] ListFiles(string path, bool fullPath)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryListFileSystem(suffix, fullPath, out var ret)) { return ret; }
                }
                else if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryListMPQ(suffix, fullPath, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                {
                    if (_TryListFileSystem(path, fullPath, out var ret)) { return ret; }
                }
                {
                    if (_TryListMPQ(path, fullPath, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                if (!string.IsNullOrEmpty(mRoot))
                {
                    if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                    {
                        if (_TryListFileSystem(suffix + path, fullPath, out var ret)) { return ret; }
                    }
                    else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                    {
                        if (_TryListMPQ(suffix + path, fullPath, out var ret)) { return ret; }
                    }
                }
                //--------------------------------------------------
            }
            catch (Exception err)
            {
                log.Error("TryListFiles From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not List Files : {path}");
            return new string[0];
        }

        public virtual string[] ListDirectories(string path, bool fullPath)
        {
            try
            {
                //--------------------------------------------------
                if (IsStartWith(path, PREFIX_FILE, out var suffix))
                {
                    if (_TryListDirFileSystem(suffix, fullPath, out var ret)) { return ret; }
                }
                if (IsStartWith(path, PREFIX_MPQ, out suffix))
                {
                    if (_TryListDirMPQ(suffix, fullPath, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                {
                    if (_TryListDirFileSystem(path, fullPath, out var ret)) { return ret; }
                }
                {
                    if (_TryListDirMPQ(path, fullPath, out var ret)) { return ret; }
                }
                //--------------------------------------------------
                if (!string.IsNullOrEmpty(mRoot))
                {
                    if (IsStartWith(mRoot, PREFIX_FILE, out suffix))
                    {
                        if (_TryListDirFileSystem(suffix + path, fullPath, out var ret)) { return ret; }
                    }
                    else if (IsStartWith(mRoot, PREFIX_MPQ, out suffix))
                    {
                        if (_TryListDirMPQ(suffix + path, fullPath, out var ret)) { return ret; }
                    }
                }
                //--------------------------------------------------
            }
            catch (Exception err)
            {
                log.Error("TryListDirectories From : " + path);
                log.Error(err.Message, err);
            }
            log.Error($"Can Not List Directories : {path}");
            return new string[0];
        }
        public virtual Task<string[]> ListFilesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListFiles(path, fullPath));
        }
        public virtual Task<string[]> ListDirectoriesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListDirectories(path, fullPath));
        }
        //-----------------------------------------------------------------------------------------------------
        #region FileSystem


        public virtual bool _TryLoadFromFileSystem(string path, out byte[] ret)
        {
            if (_TryExistWithFileSystem(path))
            {
                ret = System.IO.File.ReadAllBytes(path);
                return ret != null;
            }
            ret = null;
            return false;
        }
        public virtual bool _TryOpenFromFileSystem(string path, out Stream ret)
        {
            if (_TryExistWithFileSystem(path))
            {
                ret = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return ret != null;
            }
            ret = null;
            return false;
        }
        public virtual bool _TryListFileSystem(string path, bool fullPath, out string[] ret)
        {
            if (Directory.Exists(path))
            {
                ret = Array.ConvertAll(new DirectoryInfo(path).GetFiles(), file => fullPath ? file.FullName : file.Name);
                return ret != null;
            }
            ret = null;
            return false;
        }
        public virtual bool _TryListDirFileSystem(string path, bool fullPath, out string[] ret)
        {
            if (Directory.Exists(path))
            {
                ret = Array.ConvertAll(new DirectoryInfo(path).GetDirectories(), file => fullPath ? file.FullName : file.Name);
                return ret != null;
            }
            ret = null;
            return false;
        }
        public virtual bool _TryExistWithFileSystem(string path)
        {
            if (System.IO.File.Exists(path))
            {
                return true;
            }
            return false;
        }
        public virtual bool _TryExistDirWithFileSystem(string dir)
        {
            if (System.IO.Directory.Exists(dir))
            {
                return true;
            }
            return false;
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------
        #region MPQ

        public virtual bool _TryLoadFromMPQ(string path, out byte[] ret)
        {
#if false
            if (_TryExistWithMPQ(path))
            {
                ret = mMPQ.GetData(path);
                return ret != null;
            }
#endif
            ret = null;
            return false;
        }
        public virtual bool _TryOpenFromMPQ(string path, out Stream ret)
        {
#if false
            if (_TryExistWithMPQ(path))
            {
                ret = mMPQ.OpenStream(path);
                return ret != null;
            }
#endif
            ret = null;
            return false;
        }
        public virtual bool _TryExistWithMPQ(string path)
        {
#if false
            if (mMPQ != null)
            {
                if (mMPQ.FindEntry(path) != null)
                {
                    return true;
                }
                return false;
            }
#endif
            return false;
        }
        public virtual bool _TryExistDirWithMPQ(string dir)
        {
#if false
            if (mMPQ != null)
            {
                if (mMPQ.GetDirectory(dir) != null)
                {
                    return true;
                }
                return false;
            }
#endif
            return false;
        }

        public virtual bool _TryListMPQ(string path, bool fullPath, out string[] ret)
        {
#if false
            if (mMPQ != null)
            {
                var dir = mMPQ.GetDirectory(path);
                if (dir != null)
                {
                    ret = Array.ConvertAll(dir.GetFiles(), e => fullPath ? e.FullPath : e.Name);
                    return ret != null;
                }
            }
#endif
            ret = null;
            return false;
        }
        public virtual bool _TryListDirMPQ(string path, bool fullPath, out string[] ret)
        {
#if false
            if (mMPQ != null)
            {
                var dir = mMPQ.GetDirectory(path);
                if (dir != null)
                {
                    ret = Array.ConvertAll(dir.GetDirectories(), e => fullPath ? e.FullPath : e.Name);
                    return ret != null;
                }
            }
#endif
            ret = null;
            return false;
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------
        #region Http
#if false
        protected virtual HttpClient CreateHttp()
        {
            var handler = new HttpClientHandler();
            //var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.UseCookies = true;
            //handler.CookieContainer = cookies;
            handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            handler.ServerCertificateCustomValidationCallback += (_HttpRequestMessage, _X509Certificate2, _X509Chain, _SslPolicyErrors) => true;
            var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(60);
            http.DefaultRequestHeaders.Accept.ParseAdd(HttpRequest.DEFAULT_ACCEPT);
            http.DefaultRequestHeaders.AcceptEncoding.ParseAdd(HttpRequest.DEFAULT_ACCEPT_ENCODING);
            http.DefaultRequestHeaders.Connection.ParseAdd(HttpRequest.DEFAULT_CONNECTION);
            //http.DefaultRequestHeaders.
            http.DefaultRequestHeaders.UserAgent.ParseAdd(HttpRequest.DEFAULT_USER_AGENT);
            return http;
        }
#endif
        //-----------------------------------------------------------------------------------------------------
        public virtual bool _TryLoadFromHttp(string path, out byte[] ret)
        {
#if false
            try
            {
                log.Info("get " + path);
                using (var client = CreateHttp())
                {
                    var response = client.GetAsync(new Uri(path), HttpCompletionOption.ResponseContentRead).WaitForResult();
                    if (response.IsSuccessStatusCode)
                    {
                        ret = response.Content.ReadAsByteArrayAsync().WaitForResult();
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
#endif
            ret = null;
            return false;
        }

        public virtual bool _TryOpenFromHttp(string path, out Stream ret)
        {
#if false
            try
            {
                log.Info("open " + path);
                using (var client = CreateHttp())
                {
                    var response = client.GetAsync(new Uri(path), HttpCompletionOption.ResponseContentRead).WaitForResult();
                    if (response.IsSuccessStatusCode)
                    {
                        ret = response.Content.ReadAsStreamAsync().WaitForResult();
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
#endif
            ret = null;
            return false;
        }
        public virtual bool _TryExistWithHttp(string path)
        {
#if false
            try
            {
                log.Info("exist " + path);
                using (var client = CreateHttp())
                {
                    var response = client.GetAsync(new Uri(path), HttpCompletionOption.ResponseHeadersRead).WaitForResult();
                    return (response.IsSuccessStatusCode);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
#endif
            return false;
        }
        public virtual async Task<byte[]> _LoadFromHttpAsync(string path)
        {
#if false
            try
            {
                log.Info("get " + path);
                using (var client = CreateHttp())
                {
                    var response = await client.GetAsync(new Uri(path), HttpCompletionOption.ResponseContentRead);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsByteArrayAsync();
                        return responseBody;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
#endif
            return null;
        }
        public virtual async Task<Stream> _OpenFromHttpAsync(string path)
        {
#if false
            try
            {
                log.Info("open " + path);
                using (var client = CreateHttp())
                {
                    var response = await client.GetAsync(new Uri(path), HttpCompletionOption.ResponseContentRead);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStreamAsync();
                        return responseBody;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
#endif
            return null;
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------

    }

    //-----------------------------------------------------------------------------------------------------------------------
#endif

    //-----------------------------------------------------------------------------------------------------------------------
    public class FileResourceLoader : IResourceLoader
    {
        public static bool TryGetPath(string path, out string suffix)
        {
            if (Resource.IsStartWith(path, Resource.PREFIX_FILE, out suffix))
            {
                return File.Exists(suffix) || Directory.Exists(suffix);
            }
            else
            {
                suffix = path;
                return File.Exists(path) || Directory.Exists(path);
            }
        }
        public bool IsStartWith(string path)
        {
            return TryGetPath(path, out _);
        }
        //-----------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------
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
        public bool ExistData(string path)
        {
            if (TryGetPath(path, out var suffix))
            {
                if (System.IO.File.Exists(suffix))
                {
                    return true;
                }
            }
            return false;
        }
        public Task<bool> ExistDataAsync(string path)
        {
            return Task.FromResult(ExistData(path));
        }
        public byte[] LoadData(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return System.IO.File.ReadAllBytes(suffix);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not Load Data : {path}");
            }
            return null;
        }
        public async Task<byte[]> LoadDataAsync(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return await System.IO.File.ReadAllBytesAsync(suffix);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not Load Data : {path}");
            }
            return null;

        }
        public Stream OpenStream(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return new FileStream(suffix, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not OpenStream : {path}");
            }
            return null;
        }
        public Task<Stream> OpenStreamAsync(string path)
        {
            return Task.FromResult(OpenStream(path));
        }
        public string[] ListFiles(string path, bool fullPath)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    if (Directory.Exists(suffix))
                    {
                        return Array.ConvertAll(new DirectoryInfo(suffix).GetFiles(), file => fullPath ? file.FullName : file.Name);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not ListFiles : {path}");
            }
            return null;
        }

        public string[] ListDirectories(string path, bool fullPath)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    if (Directory.Exists(suffix))
                    {
                        return Array.ConvertAll(new DirectoryInfo(suffix).GetDirectories(), file => fullPath ? file.FullName : file.Name);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not ListDirectories : {path}");
            }
            return null;
        }
        public Task<string[]> ListFilesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListFiles(path, fullPath));
        }
        public Task<string[]> ListDirectoriesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListDirectories(path, fullPath));
        }
        //-----------------------------------------------------------------------------------------------------

    }

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------
   

}
