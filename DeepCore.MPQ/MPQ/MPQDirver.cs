using DeepCore.Concurrent;
using DeepCore.Http;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.MPQ.Updater;
using DeepCore.Reflection;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DeepCore.MPQ
{
    //------------------------------------------------------------------------------------------------------------------------
    public static class MPQDriverFactory
    {
        //------------------------------------------------------------------------------------------------------------------------
        public delegate MPQDriver CreateDriverAction(DirectoryInfo saveRoot, DirectoryInfo bundleRoot);
        public delegate MPQUnziper CreateUnziperAction(DirectoryInfo dir);
        public delegate MPQDownloader CreateDownloaderAction(Uri remote_version_url);
        //------------------------------------------------------------------------------------------------------------------------

        public static CreateDriverAction CreateDriver = (DirectoryInfo saveRoot, DirectoryInfo bundleRoot) => new MPQDriver();

        public static CreateUnziperAction CreateUnziper = (DirectoryInfo dir) => new MPQUnziper();

        public static CreateDownloaderAction CreateDownloader = (Uri remote_version_url) =>
        {
            if (string.Equals(remote_version_url.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                return new MPQDownloaderHTTPS(remote_version_url);
            }
            else
            {
                return new MPQDownloaderHTTP(remote_version_url);
            }
        };
        //------------------------------------------------------------------------------------------------------------------------
    }

    //------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public class MPQDriver
    {
        protected readonly Logger log = LoggerFactory.GetLogger("MPQ");
        public MPQDriver()
        {
        }
        /// <summary>
        /// 获取存储空间可用容量
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual long GetAvaliableSpace(string path)
        {
            try
            {
                log.Info("GetAvaliableSpace : " + path);
                DriveInfo drive = new DriveInfo(Directory.GetDirectoryRoot(path));
                return drive.AvailableFreeSpace;
            }
            catch
            {
                log.Info("GetAvaliableSpace : failed return max");
                return long.MaxValue;
            }
        }

        /// <summary>
        /// 获取存储空间总容量
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual long GetTotalSpace(string path)
        {
            try
            {
                log.Info("GetTotalSpace : " + path);
                DriveInfo drive = new DriveInfo(Directory.GetDirectoryRoot(path));
                return drive.TotalSize;
            }
            catch
            {
                log.Info("GetTotalSpace : failed return max");
                return long.MaxValue;
            }
        }

        /// <summary>
        /// 自定义获取文件MD5方法.
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="md5"></param>
        /// <returns></returns>
        public virtual bool RunGetFileMD5(string fullname, out string md5)
        {
            using (FileStream fs = new FileStream(fullname, FileMode.Open, FileAccess.Read))
            {
                md5 = CMD5.CalculateMD5(fs);
            }
            return true;
        }

    }

    //------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public class MPQUnziper : Disposable
    {
        protected readonly Logger log = LoggerFactory.GetLogger("MPQ");
        public MPQUnziper()
        {
        }
        protected override void Disposing()
        {
        }
        /// <summary>
        /// 自定义解压缩方法
        /// </summary>
        /// <param name="updater"></param>
        /// <param name="zip">压缩文件</param>
        /// <param name="mpq">解压缩文件</param>
        /// <param name="process">解压进度，process+=readed</param>
        /// <returns></returns>
        public virtual bool RunUnzipSingle(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip, MPQUpdater.RemoteFileInfo mpq, AtomicLong process)
        {
            try
            {
                using (FileStream fis = new FileStream(zip.file.FullName, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fos = new FileStream(mpq.file.FullName, FileMode.Create, FileAccess.Write))
                    {
                        try
                        {
                            if (MPQUpdater.ZIP_EXT.ToLower().EndsWith(".z"))
                            {
                                var gstream = new System.IO.Compression.DeflateStream(fis, System.IO.Compression.CompressionMode.Decompress, true);
                                if (IOUtil.ReadTo(gstream, fos, mpq.size, (int readed) =>
                                {
                                    process += readed;
                                    return !updater.IsDisposing;
                                }, 1024 * 1024) == false)
                                { return false; }
                                gstream.Close();
                            }
                            return true;
                        }
                        finally
                        {
                            fos.Close();
                            fis.Close();
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                try
                {
                    var type = ReflectionUtil.GetType("DeepCore.SharpZipLib.Unzip");
                    if (type != null)
                    {
                        var method = type.GetMethod("SharpZipLib_RunUnzipMPQ");
                        return (bool)method.Invoke(null, new object[] { updater, zip, mpq, process });
                    }
                }
                catch (Exception err2)
                {
                    log.Error(err2.Message, err2);
                }

            }
            return false;
        }

    }
    //------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class MPQDownloader : Disposable
    {
        protected readonly Logger log = LoggerFactory.GetLogger("MPQ");
        protected override void Disposing()
        {
        }
        /// <summary>
        /// 自定义下载方法
        /// </summary>
        /// <param name="updater"></param>
        /// <param name="inf">要下载的文件</param>
        /// <param name="exist_size">已下载大小</param>
        /// <param name="need_bytes">需要的大小</param>
        /// <param name="process">下载进度，process+=readed</param>
        /// <returns></returns>
        public virtual bool RunDownloadSingle(MPQUpdater updater, MPQUpdater.RemoteFileInfo inf, long exist_size, long need_bytes, AtomicLong process)
        {
            byte[] io_buffer = new byte[1024 * 1024];
            using (FileStream fos = new FileStream(inf.file.FullName, FileMode.Append, FileAccess.Write))
            {
                RunDownloadBytes(updater, inf.key, exist_size, need_bytes, input =>
                {
                    try
                    {
                        long total_readed = 0;
                        while (total_readed < need_bytes)
                        {
                            if (updater.IsDisposing) return;
                            int readed = input.Read(io_buffer, 0, (int)Math.Min(io_buffer.Length, need_bytes - total_readed));
                            total_readed += readed;
                            process += readed;
                            fos.Write(io_buffer, 0, readed);
                        }
                        fos.Flush();
                    }
                    finally
                    {
                        fos.Close();
                    }
                }).Wait();
            }
            return true;
        }
        public abstract string DownloadString(Uri url);
        public abstract Task RunDownloadBytes(MPQUpdater updater, string key, long exist_size, long expect_length, Action<Stream> input);
    }

    //------------------------------------------------------------------------------------------------------------------------

    public class MPQDownloaderHTTP : MPQDownloader
    {
        public MPQDownloaderHTTP(Uri remote_version_url)
        {
        }
        public override string DownloadString(Uri url)
        {
            return WebClient.DownloadString(url);
        }
        public override Task RunDownloadBytes(MPQUpdater updater, string key, long exist_size, long expect_length, Action<Stream> input)
        {
            for (int i = 0; i < updater.UrlRoots.Length; i++)
            {
                string path = updater.UrlRoots[i % updater.UrlRoots.Length] + key;
                path = path.Replace('\\', '/');
                Uri url = new Uri(path);
                using (WebClient http = new WebClient(url))
                {
                    http.TimeoutMS = updater.DownloadTimeoutSEC * 1000;
                    if (exist_size > 0)
                    {
                        http.Request.Params["Range"] = ("bytes=" + exist_size + "-");
                    }
                    try
                    {
                        var stream = http.Connect();
                        if ((http.Response.ContentLength != expect_length))
                        {
                            throw new Exception("下载HTTP.ContentLength尺寸不匹配 : "
                                + http.Response.ContentLength + "\n"
                                + url.ToString() + "\n"
                                + http.Response.Status);
                        }
                        input(stream);
                        return Task.CompletedTask;
                    }
                    catch (Exception err)
                    {
                        log.Error("下载出错 : " + url + "\n  " + err.Message, err);
                        try
                        {
                            http.Dispose();
                        }
                        catch (Exception err2)
                        {
                            log.Error(err2.Message, err2);
                        }
                        continue;
                    }
                }
            }
            throw new Exception("Can not download : " + CUtils.ArrayToString(updater.UrlRoots) + key);
        }
    }

    //------------------------------------------------------------------------------------------------------------------------

    public class MPQDownloaderHTTPS : MPQDownloader
    {
        public MPQDownloaderHTTPS(Uri remote_version_url)
        {
        }


        protected virtual HttpClient CreateHttp()
        {
            var handler = new HttpClientHandler();
            //var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.UseCookies = true;
            //handler.CookieContainer = cookies;
            handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            handler.ServerCertificateCustomValidationCallback += (_HttpRequestMessage, _X509Certificate2, _X509Chain, _SslPolicyErrors) =>
            {
                return true;
            };
            var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(60);
            http.DefaultRequestHeaders.Accept.ParseAdd(HttpRequest.DEFAULT_ACCEPT);
            http.DefaultRequestHeaders.AcceptEncoding.ParseAdd(HttpRequest.DEFAULT_ACCEPT_ENCODING);
            http.DefaultRequestHeaders.Connection.ParseAdd(HttpRequest.DEFAULT_CONNECTION);
            //http.DefaultRequestHeaders.
            http.DefaultRequestHeaders.UserAgent.ParseAdd(HttpRequest.DEFAULT_USER_AGENT);
            return http;
        }


        public override string DownloadString(Uri url)
        {
            return Task.Run(async () =>
            {
                log.Info("get " + url);
                using (var client = CreateHttp())
                {
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsByteArrayAsync();
                        return CUtils.DecodeUTF8(responseBody);
                    }
                    else
                    {
                        return null;
                    }
                }
            }).WaitForResult();
        }

        public override async Task RunDownloadBytes(MPQUpdater updater, string key, long exist_size, long expect_length, Action<Stream> input)
        {
            for (int i = 0; i < updater.UrlRoots.Length; i++)
            {
                string path = updater.UrlRoots[i % updater.UrlRoots.Length] + key;
                path = path.Replace('\\', '/');
                var url = new Uri(Resource.FormatPath(path));
                log.Info("get " + url);
                using (var http = CreateHttp())
                {
                    http.Timeout = TimeSpan.FromSeconds(updater.DownloadTimeoutSEC);
                    if (exist_size > 0)
                    {
                        //http.Request.Params["Range"] = ("bytes=" + exist_size + "-");
                        http.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("Range", $"bytes={exist_size}-"));
                    }
                    try
                    {
                        //input = http.Connect();
                        var response = await http.GetAsync(url, HttpCompletionOption.ResponseContentRead);
                        if (response.IsSuccessStatusCode)
                        {
                            var content_length = response.Content.Headers.ContentLength;
                            if (content_length == expect_length)// ["Content-Length"];
                            {
                                var stream = await response.Content.ReadAsStreamAsync();
                                input(stream);
                                return;
                            }
                            else
                            {
                                throw new Exception("下载HTTP.ContentLength尺寸不匹配 : "
                                    + content_length + "\n"
                                    + url.ToString() + "\n"
                                    + response.StatusCode);
                            }
                        }
                        throw new Exception("Http 请求出错 : "
                            + url.ToString() + "\n"
                            + response.StatusCode);
                    }
                    catch (Exception err)
                    {
                        log.Error("下载出错 : " + url + "\n  " + err.Message, err);
                        try
                        {
                            http.Dispose();
                        }
                        catch (Exception err2)
                        {
                            log.Error(err2.Message, err2);
                        }
                        continue;
                    }
                }
            }
            throw new Exception("Can not connect to download root : " + CUtils.ArrayToString(updater.UrlRoots));
        }

    }

    //------------------------------------------------------------------------------------------------------------------------
}
