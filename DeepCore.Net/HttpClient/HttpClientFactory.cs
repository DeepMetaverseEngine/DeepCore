using DeepCore.IO;
using DeepCore.NetClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Net.HttpClient
{
        public class HttpClientFactory : NetClientFactory
        {
            static public HttpClientFactory HttpInstance { get; private set; } = new HttpClientFactory();
            public HttpClientFactory()
            {
            HttpInstance = this;
            }

            public static Dictionary<string, string> ClientHeader { get; set; }
            public static List<string> SubProtocol { get; set; }

            public virtual WSNetClient CreateClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000)
            {
                return new WSNetClient(codec, name, request_timer_tick_ms);
            }
            public override IClientAdapter CreateAdapter(INetClient client)
            {
                return new WSWebSocketAdapter(client);
            }
        }
    
}
