using DeepCore.Http;
using DeepCore.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeepCore.Net.Http
{
    public class HttpResourceLoader : IResourceLoader
    {
        public HttpResourceLoader()
        {
            Resource.AddLoader(this);
        }
        public static bool TryGetPath(string path, out string suffix)
        {
            if (Resource.IsStartWith(path, Resource.PREFIX_HTTPS, out suffix))
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
        //-----------------------------------------------------------------------------------------------------
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
            return ExistDataAsync(path).WaitForResult();
        }
        public byte[] LoadData(string path)
        {
            return LoadDataAsync(path).WaitForResult();
        }
        public Stream OpenStream(string path)
        {
            return OpenStreamAsync(path).WaitForResult();
        }
        public async Task<bool> ExistDataAsync(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    using (var client = CreateHttp())
                    {
                        var response = await client.GetAsync(new Uri(path), HttpCompletionOption.ResponseHeadersRead);
                        return (response.IsSuccessStatusCode);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not ExistData Data : {path}");
            }
            return false;
        }
        public async Task<byte[]> LoadDataAsync(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
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
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not Load Data : {path}");
            }
            return null;

        }
        public async Task<Stream> OpenStreamAsync(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
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
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not OpenStream : {path}");
            }
            return null;
        }

        string[] IResourceLoader.ListFiles(string path, bool fullPath)
        {
            throw new NotImplementedException();
        }
        string[] IResourceLoader.ListDirectories(string path, bool fullPath)
        {
            throw new NotImplementedException();
        }
        Task<string[]> IResourceLoader.ListFilesAsync(string path, bool fullPath)
        {
            throw new NotImplementedException();
        }
        Task<string[]> IResourceLoader.ListDirectoriesAsync(string path, bool fullPath)
        {
            throw new NotImplementedException();
        }
    }

}
