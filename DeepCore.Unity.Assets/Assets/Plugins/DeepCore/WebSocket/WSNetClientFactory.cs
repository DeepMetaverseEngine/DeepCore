#if NATIVE_WEBSOCKET

using DeepCore.IO;
using DeepCore.NetClient;
using System.Collections.Generic;

namespace DeepCore.Net.WS
{
    public class WSNetClientFactory : NetClientFactory
    {
        static public WSNetClientFactory WSInstance { get; private set; } = new WSNetClientFactory();
        public WSNetClientFactory()
        {
            WSInstance = this;
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

#endif