using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DeepCore.Http
{
    public class WWWDownloaderStream : DownloaderStream
    {
        private readonly DeepCore.Http.WebClient www;

        public HttpResponse Response { get { return www.Response; } }
        public HttpRequest Request { get { return www.Request; } }
        public Exception WWWError { get { return www.Error; } }

        private WWWDownloaderStream(DeepCore.Http.WebClient www, Stream src, int bufferSize = 102400, bool leaveOpen = true, int downloadTrunkSize = 4096) : base(src, bufferSize, leaveOpen, downloadTrunkSize)
        {
            this.www = www;
        }
        /// <summary>
        /// 异步下载器
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="leaveOpen"></param>
        /// <param name="bufferSize"></param>
        /// <param name="downloadTrunkSize"></param>
        /// <returns></returns>
        public static WWWDownloaderStream CreateDownlader(Uri uri, bool leaveOpen = true, int bufferSize = 102400, int downloadTrunkSize = 4096)
        {
            var www = new DeepCore.Http.WebClient(uri);
//             if (uri.Scheme == "https")
//             {
//                 www.Request.SslProtocol = System.Security.Authentication.SslProtocols.Tls;
//             }
            var stream = www.Connect();
            var bufStream = new WWWDownloaderStream(www, stream, bufferSize, leaveOpen, downloadTrunkSize);
            return bufStream;
        }
    }


}
