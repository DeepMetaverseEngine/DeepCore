using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Http
{
    public delegate void HttpConnectHandler(WebClient www);
    public delegate void HttpPostHandler(string result);
    public delegate void HttpGetHandler(byte[] result);

    public class HttpRequest
    {
        public const string METHOD_GET = "GET";
        public const string METHOD_POST = "POST";

        public const string CONTENT_TYPE_OCTET_STREAM = "application/octet-stream";
        public const string CONTENT_TYPE_WWW_FORM_URLENCODED = "application/x-www-form-urlencoded";
        public const string CONTENT_TYPE_TEXT_XML = "text/xml";

        public static string DEFAULT_ACCEPT = "text/html,application/x-compress,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
        public static string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0";
        public static string DEFAULT_ACCEPT_LANGUAGE = "zh-CN,zh;q=0.9";
        public static string DEFAULT_ACCEPT_ENCODING = "identity";
        public static string DEFAULT_CONNECTION = "keep-alive";
        public static string DEFAULT_CACHE_CONTROL = "no-cache";

        public string Method = METHOD_GET;
        public string ContentType = CONTENT_TYPE_OCTET_STREAM;
        public string Referer;
        public string Accept = DEFAULT_ACCEPT;
        public string AcceptLanguage = DEFAULT_ACCEPT_LANGUAGE;
        public string AcceptEncoding = DEFAULT_ACCEPT_ENCODING;
        public string UserAgent = DEFAULT_USER_AGENT;
        public string Connection = DEFAULT_CONNECTION;
        public string CacheControl = DEFAULT_CACHE_CONTROL;

        public SslProtocols SslProtocol = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        public X509CertificateCollection Certificates = null;
        public RemoteCertificateValidationCallback OnRemoteCertificateValidation = WebClient.ValidateServerCertificate;
        public LocalCertificateSelectionCallback OnLocalCertificateValidation = null;



        private Properties _params;
        public Properties Params
        {
            get
            {
                if (_params == null)
                {
                    _params = new Properties();
                }
                return _params;
            }
        }

        public byte[] Content;

    }

    public class HttpResponse
    {
        public Properties Params { get; internal set; }
        public string Status { get; internal set; }
        public string ContentType { get; internal set; }
        public long ContentLength { get; internal set; }
        public string Location { get; internal set; }
        public bool IsGzip { get; internal set; }
        public bool IsChunk { get; internal set; }

        private Stream input;
        public Stream InputStream
        {
            get { return input; }
            internal set
            {
                if (this.IsChunk)
                {
                    this.input = new ChunkInputStream(this, value);
                }
                else
                {
                    this.input = new ResponseInputStream(this, value);
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Status);
            if (Params != null)
            {
                foreach (var e in Params)
                {
                    sb.Append(e.Key + " : " + e.Value + WebClient.BR);
                }
            }
            return sb.ToString();
        }

        public byte[] ReadContentToEnd()
        {
            if (IsChunk)
            {
                byte[] data = IOUtil.ReadToEnd(input);
                return data;
            }
            else
            {
                byte[] data = new byte[ContentLength];
                IOUtil.ReadToEnd(input, data, 0, data.Length);
                return data;
            }
        }
        public async Task<byte[]> ReadContentToEndAsync()
        {
            if (IsChunk)
            {
                byte[] data = await IOUtil.ReadToEndAsync(input);
                return data;
            }
            else
            {
                byte[] data = new byte[ContentLength];
                await IOUtil.ReadToEndAsync(input, data, 0, data.Length);
                return data;
            }
        }

        internal class ResponseInputStream : Stream
        {
            private readonly HttpResponse response;
            private readonly Stream baseStream;
            private int total_readed = 0;
            public override bool CanRead { get { return baseStream.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return 0; } }
            public override long Position { get { return 0; } set { } }


            internal ResponseInputStream(HttpResponse rsp, Stream s)
            {
                this.response = rsp;
                this.baseStream = s;
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (total_readed < response.ContentLength)
                {
                    int readed = baseStream.Read(buffer, offset, count);
                    if (readed > 0)
                    {
                        total_readed += readed;
                    }
                    return readed;
                }
                else
                {
                    return 0;
                }
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }
            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
            public override void Flush()
            {
                throw new NotImplementedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
        internal class ChunkInputStream : Stream
        {
            private readonly HttpResponse response;
            private readonly Stream baseStream;
            private int current_chunk_pos = 0;
            private int current_chunk_size = -1;

            public override bool CanRead { get { return baseStream.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return 0; } }
            public override long Position { get { return 0; } set { } }


            internal ChunkInputStream(HttpResponse rsp, Stream s)
            {
                this.response = rsp;
                this.baseStream = s;
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (current_chunk_pos >= current_chunk_size)
                {
                    try
                    {
                        var line = WebClient.ReadLine(baseStream);
                        current_chunk_size = Convert.ToInt32(line.Trim(), 16);
                        current_chunk_pos = 0;
                    }
                    catch (Exception err)
                    {
                        throw new Exception("Maybe exists 'chunk-ext' field.", err);
                    }
                }
                if (current_chunk_size == 0)
                {
                    return 0;
                }
                int total = current_chunk_size - current_chunk_pos;
                count = Math.Min(total, count);
                int readed = baseStream.Read(buffer, offset, count);
                if (readed > 0)
                {
                    current_chunk_pos += readed;
                    if (current_chunk_pos >= current_chunk_size)
                    {
                        WebClient.ReadLine(baseStream);
                    }
                }
                return readed;
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }
            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
            public override void Flush()
            {
                throw new NotImplementedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class WebClient : IDisposable
    {
        private static Logger log = new LazyLogger(nameof(WebClient));

        private TcpClient mSocket = null;
        private Uri url;

        public HttpRequest Request = new HttpRequest();
        public HttpResponse Response { get; private set; }
        public Exception Error { get; private set; }
        public int TimeoutMS { get; set; }
        public int BufferSize { get; set; } = 32 * 1024;
        public bool IsExist { get => Response != null && Response.Status != null && Response.Status.Contains("200"); }

        public WebClient(Uri url)
        {
            this.url = url;
            this.TimeoutMS = 30000;
        }
        public void Dispose()
        {
            try
            {
                if (mSocket != null)
                {
                    mSocket.Close();
                }
            }
            catch (System.Exception e) { log.Error(e.Message, e); }
        }
        public bool Exist()
        {
            return _connect(null);
        }
        public Stream Connect()
        {
            _connect(null);
            if (Response != null)
            {
                return Response.InputStream;
            }
            return null;
        }
        public Task<Stream> ConnectAsync()
        {
            return new Task<Stream>(() => { return this.Connect(); });
        }
        public Task<bool> ConnectAsync(HttpConnectHandler handler)
        {
            return Task.Run(() => { return this._connect(handler); });
        }
    

        //-----------------------------------------------------------------------------------------------------------------

        #region Internal

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forceIPv6">强制IPv6</param>
        /// <param name="location"></param>
        /// <param name="timeoutMS"></param>
        private static TcpClient _connect_remote(bool forceIPv6, Uri location, int timeoutMS, int bufferSize)
        {
            if (location.HostNameType == UriHostNameType.Dns)
            {
                Console.WriteLine("dns : " + location + "  " + location.HostNameType.ToString());
                IPHostEntry ips;//= Dns.GetHostEntry(location.Host);
                // 如果只包含IPv6地址，表示当前环境IPv6 only
                AddressFamily family;//= IPUtil.IsOnlyIPv6(ips.AddressList) ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                var addrs = IPUtil.GetIPAddress(location.Host, location.Port, out family, out ips);
                var mSocket = new TcpClient(forceIPv6 ? AddressFamily.InterNetworkV6 : family);
                mSocket.SendTimeout = timeoutMS;
                mSocket.ReceiveTimeout = timeoutMS;
                mSocket.ReceiveBufferSize = bufferSize;
                mSocket.SendBufferSize = bufferSize;
                if (family != AddressFamily.InterNetworkV6 && forceIPv6)
                {
                    if (ips == null)
                    {
                        ips = Dns.GetHostEntry(location.Host);
                    }
                    //首次是IPV6地址，优先选择V6地址//
                    foreach (var ip in ips.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            mSocket.Connect(new IPAddress[] { ip }, location.Port);
                            return mSocket;
                        }
                    }
                    //强转V4地址到V6地址//
                    foreach (var ip in ips.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var ipv6 = IPUtil.MapToIPv6(ip);
                            Console.WriteLine("ipv4 to ipv6 : " + ip + " - " + ipv6);
                            mSocket.Connect(new IPAddress[] { ipv6 }, location.Port);
                            return mSocket;
                        }
                    }
                    mSocket.Connect(addrs, location.Port);
                }
                else
                {
                    mSocket.Connect(addrs, location.Port);
                }
                return mSocket;
            }
            else
            {
                Console.WriteLine("ip : " + location + "  " + location.HostNameType.ToString());
                var mSocket = new TcpClient(forceIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
                mSocket.SendTimeout = timeoutMS;
                mSocket.ReceiveTimeout = timeoutMS;
                mSocket.ReceiveBufferSize = bufferSize;
                mSocket.SendBufferSize = bufferSize;
                if (location.HostNameType != UriHostNameType.IPv6 && forceIPv6)
                {
                    var ipv6 = IPUtil.MapToIPv6(location.Host);
                    Console.WriteLine("ipv4 to ipv6 : " + location.Host + " - " + ipv6);
                    mSocket.Connect(new IPAddress[] { ipv6 }, location.Port);
                }
                else
                {
                    mSocket.Connect(location.Host, location.Port);
                }
                return mSocket;
            }
        }

        private bool _connect(HttpConnectHandler handler)
        {
            try
            {
                bool isIpV6 = false;
                Uri location = url;
                do
                {
                    mSocket = _connect_remote(isIpV6, location, TimeoutMS, BufferSize);
                    if (!mSocket.Connected)
                    {
                        return false;
                    }
                    Console.WriteLine("RemoteEndPoint AddressFamily : " + mSocket.Client.RemoteEndPoint.AddressFamily);
                    if (mSocket.Client.RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        isIpV6 = true;
                    }
                    Stream stream = null;
                    if (location.Scheme == "http")
                    {
                        stream = mSocket.GetStream();
                    }
                    else if (location.Scheme == "https")
                    {
                        var sslStream = new SslStream(mSocket.GetStream(), false,
                            Request.OnRemoteCertificateValidation,
                            Request.OnLocalCertificateValidation);
                        sslStream.ReadTimeout = TimeoutMS;
                        sslStream.WriteTimeout = TimeoutMS;
                        var certificates = Request.Certificates;
                        //                         if (certificates == null)
                        //                         {
                        //                             //var store = new X509Store(StoreName.My);
                        //                             certificates = GetCertificateFromStore();
                        //                         }
                        sslStream.AuthenticateAsClient(location.Host, certificates, Request.SslProtocol, false);
                        //sslStream.AuthenticateAsClient(location.Host, certificates, Request.SslProtocol, true);
                        if (!sslStream.IsAuthenticated)
                        {
                            return false;
                        }
                        stream = sslStream;
                    }
                    else
                    {
                        throw new ArgumentException("url must start with HTTP or HTTPS.", "url");
                    }
                    log.Trace("send request : " + location);
                    this.Response = _send_request(stream, location, Request.Referer, Request);
                    log.Trace("response : " + Response.Status);
                    // redirect 302
                    if (!string.IsNullOrEmpty(this.Response.Location))
                    {
                        if (mSocket != null)
                        {
                            try { stream.Close(); } catch (System.Exception e) { log.Error(e.Message, e); }
                            try { mSocket.Close(); } catch (System.Exception e) { log.Error(e.Message, e); }
                        }
                        log.Trace(" redirect to : " + this.Response.Location);
                        location = new Uri(this.Response.Location);
                        continue;
                    }
                    else
                    {
                        this.Response.InputStream = stream;
                        break;
                    }
                }
                while (true);
                return this.IsExist;
            }
            catch (System.Exception e)
            {
                log.Error(e.Message, e);
                this.Response = null;
                this.Error = e;
                return false;
            }
            finally
            {
                if (handler != null)
                {
                    handler(this);
                }
            }
        }

        private static HttpResponse _send_request(Stream stream, Uri url, string referer, HttpRequest request)
        {
            // Send
            if (request.Method.Equals(HttpRequest.METHOD_GET))
            {
                StringBuilder header_text = new StringBuilder();
                header_text.Append(request.Method + " " + url.PathAndQuery + " HTTP/1.1" + BR);
                header_text.Append("Accept: " + request.Accept + BR);
                header_text.Append("Referer: " + referer + BR);
                header_text.Append("Accept-Language: " + request.AcceptLanguage + BR);
                header_text.Append("Accept-Encoding: " + request.AcceptEncoding + BR);
                header_text.Append("User-Agent: " + request.UserAgent + BR);
                header_text.Append("Host: " + url.Host + BR);
                header_text.Append("Connection: " + request.Connection + BR);
                header_text.Append("Cache-Control: " + request.CacheControl + BR);
                foreach (KeyValuePair<string, string> e in request.Params)
                {
                    header_text.Append(e.Key + ": " + e.Value + BR);
                }
                header_text.Append(BR);
                // send HTTP GET //
                string req = header_text.ToString();
                byte[] header_bytes = Encoding.UTF8.GetBytes(req);
                IOUtil.WriteToEnd(stream, header_bytes, 0, header_bytes.Length);
            }
            else if (request.Method.Equals(HttpRequest.METHOD_POST))
            {
                byte[] body = _get_post_data(url, request);
                // send HTTP Post Head //
                StringBuilder header_text = new StringBuilder();
                header_text.Append(request.Method + " " + url.AbsolutePath + " HTTP/1.1" + BR);
                header_text.Append("Host: " + url.Host + BR);
                header_text.Append("Referer: " + referer + BR);
                header_text.Append("Content-Length: " + body.Length + BR);
                header_text.Append("Content-Type: " + request.ContentType + BR);
                header_text.Append("User-Agent: " + request.UserAgent + BR);
                header_text.Append("Connection: " + request.Connection + BR);
                header_text.Append("Cache-Control: " + request.CacheControl + BR);
                header_text.Append("Accept: " + request.Accept + BR);
                foreach (KeyValuePair<string, string> e in request.Params)
                {
                    header_text.Append(e.Key + ": " + e.Value + BR);
                }
                header_text.Append(BR);
                string req = header_text.ToString();
                byte[] header_bytes = Encoding.UTF8.GetBytes(req);
                IOUtil.WriteToEnd(stream, header_bytes, 0, header_bytes.Length);
                IOUtil.WriteToEnd(stream, body, 0, body.Length);
            }

            HttpResponse response = new HttpResponse();
            response.Params = new Properties();
            //                 response.Params.CommentChar = null;
            //                 response.Params.SeparatorChar = ":";
            String line = null;
            while ((line = ReadLine(stream)) != null)
            {
                if (line.Length == 0)
                {
                    break;
                }
                else if (response.Status == null && line.ToUpper().StartsWith("HTTP"))
                {
                    response.Status = line;
                }
                else if (response.Params.TryParseLine(line, PropertiesFormat.HTTP))
                {

                }
                string len = null;
                if (TryGetResponseValue(line, "Content-Length", out len))
                {
                    response.ContentLength = Parser.ParseLong(len);
                }
                else if (TryGetResponseValue(line, "Content-Type", out len))
                {
                    response.ContentType = len;
                }
                else if (TryGetResponseValue(line, "Location", out len))
                {
                    response.Location = len;
                }
                else if (TryGetResponseValue(line, "Content-Encoding", out len))
                {
                    if (len.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        response.IsGzip = true;
                    }
                }
                else if (TryGetResponseValue(line, "Transfer-Encoding", out len))
                {
                    if (len.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        response.IsChunk = true;
                    }
                }
            }
            return response;
        }

        private static byte[] _get_post_data(Uri url, HttpRequest request)
        {
            if (request.Content == null)
            {
                string body = url.Query;
                if (body.StartsWith("?"))
                {
                    body = body.Substring(1);
                }
                int body_length = Encoding.UTF8.GetByteCount(body);
                byte[] body_bytes = Encoding.UTF8.GetBytes(body);
                return body_bytes;
            }
            else
            {
                return request.Content;
            }
        }


        #endregion

        //-----------------------------------------------------------------------------------------------------------------

        #region STATIC
        //         /// <summary>
        //         /// 从证书库中获取证书
        //         /// </summary>
        //         /// <param name="subjectName">证书名字</param>
        //         /// <returns></returns>
        //         public X509Certificate2 GetCertificateFromStore(string subjectName)
        //         {
        //             try
        //             {
        //                 subjectName = "CN=" + subjectName;
        //                 using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        //                 {
        //                     store.Open(OpenFlags.ReadWrite);
        //                     X509Certificate2Collection storecollection = (X509Certificate2Collection)store.Certificates;
        //                     foreach (X509Certificate2 x509 in storecollection)
        //                     {
        //                         if (x509.Subject == subjectName)
        //                         {
        //                             return x509;
        //                         }
        //                     }
        //                     store.Close();
        //                     return null;
        //                 }
        //             }
        //             catch (Exception)
        //             {
        //                 throw;
        //             }
        //         } 
        //         /// <summary>
        //            /// 从证书库中获取证书
        //            /// </summary>
        //            /// <param name="subjectName">证书名字</param>
        //            /// <returns></returns>
        //         public X509Certificate2Collection GetCertificateFromStore()
        //         {
        //             using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
        //             {
        //                 store.Open(OpenFlags.ReadWrite);
        //                 return (X509Certificate2Collection)store.Certificates;
        //             }
        //         }
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return false;
        }

        public static string DownloadString(Uri url)
        {
            using (WebClient client = new WebClient(url))
            {
                client.Request.Method = HttpRequest.METHOD_GET;
                client.Request.Referer = url.AbsoluteUri;
                client.Connect();
                if (client.Error != null)
                {
                    throw client.Error;
                }
                byte[] data = client.Response.ReadContentToEnd();
                string ret = CUtils.DecodeUTF8(data);
                return ret;
            }
        }

        public static byte[] Get(Uri url)
        {
            using (WebClient client = new WebClient(url))
            {
                client.Request.Method = HttpRequest.METHOD_GET;
                client.Request.Referer = url.AbsoluteUri;
                client.Connect();
                if (client.Error != null)
                {
                    throw client.Error;
                }
                byte[] data = client.Response.ReadContentToEnd();
                return data;
            }
        }
        public static bool Exist(Uri url)
        {
            using (WebClient client = new WebClient(url))
            {
                client.Request.Method = HttpRequest.METHOD_GET;
                client.Request.Referer = url.AbsoluteUri;
                client.Connect();
                if (client.Error != null)
                {
                    client.Error.PrintStackTrace();
                    return false;
                }
                if (client.IsExist)
                {
                    return true;
                }
                return false;
            }
        }

        public static void GetAsync(Uri url, HttpGetHandler handler)
        {
            WebClient client = new WebClient(url);
            client.Request.Method = HttpRequest.METHOD_GET;
            client.Request.Referer = url.AbsoluteUri;
            client.ConnectAsync((www) =>
            {
                try
                {
                    if (www.Response != null)
                    {
                        byte[] data = client.Response.ReadContentToEnd();
                        handler.Invoke(data);
                    }
                    else
                    {
                        handler.Invoke(null);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    handler.Invoke(null);
                }
                finally
                {
                    www.Dispose();
                }
            });
        }

        public static string Post(Uri url, string referer = null, Encoding enc = null)
        {
            if (enc == null)
            {
                enc = CUtils.UTF8;
            }
            if (referer == null)
            {
                referer = url.AbsoluteUri;
            }
            using (WebClient client = new WebClient(url))
            {
                client.Request.Method = HttpRequest.METHOD_POST;
                client.Request.ContentType = "application/x-www-form-urlencoded";
                client.Request.Referer = referer;
                client.Connect();
                byte[] data = client.Response.ReadContentToEnd();
                string ret = enc.GetString(data);
                return ret;
            }
        }
        public static void PostAsync(Uri url, string referer, Encoding enc, HttpPostHandler handler)
        {
            if (enc == null)
            {
                enc = CUtils.UTF8;
            }
            if (referer == null)
            {
                referer = url.AbsoluteUri;
            }
            WebClient client = new WebClient(url);
            client.Request.Method = HttpRequest.METHOD_POST;
            client.Request.ContentType = "application/x-www-form-urlencoded";
            client.Request.Referer = referer;
            client.ConnectAsync((www) =>
            {
                try
                {
                    if (www.Response != null)
                    {
                        byte[] data = client.Response.ReadContentToEnd();
                        string ret = enc.GetString(data);
                        handler.Invoke(ret);
                    }
                    else
                    {
                        handler.Invoke(null);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    handler.Invoke(null);
                }
                finally
                {
                    www.Dispose();
                }
            });
        }
        public static void PostAsync(Uri url, Encoding enc, HttpPostHandler handler)
        {
            PostAsync(url, null, CUtils.UTF8, handler);
        }
        public static void PostAsync(Uri url, HttpPostHandler handler)
        {
            PostAsync(url, CUtils.UTF8, handler);
        }

        #endregion

        //----------------------------------------------------------------------------------------------

        #region UTILS

        public static readonly string BR = "\r\n";
        public static readonly char[] SPLIT = new char[] { ':' };

        public static string FormatPath(string path)
        {
            path = path.Replace('\\', '/');
            return path;
        }

        private static bool TryGetResponseValue(string line, string key, out string value)
        {
            if (line.ToLower().StartsWith(key.ToLower()))
            {
                string[] kv = line.Split(SPLIT, 2);
                value = kv[1].Trim();
                return true;
            }
            value = null;
            return false;
        }

        public static string ReadLine(Stream input)
        {
            byte _r = (byte)'\r';
            byte _n = (byte)'\n';
            try
            {
                using (var ms = new DeepCore.IO.MemoryStream())
                {
                    int a0 = 0;
                    while (a0 >= 0)
                    {
                        int a1 = input.ReadByte();
                        if (a1 < 0)
                        {
                            break;
                        }
                        ms.WriteByte((byte)a1);
                        if (a0 == _r && a1 == _n)
                        {
                            break;
                        }
                        a0 = a1;
                    }
                    ms.Flush();
                    if (ms.Length >= 2)
                    {
                        return Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)(ms.Length - 2));
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion



    }


}
