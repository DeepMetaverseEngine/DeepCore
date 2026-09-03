using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Reflection;
using System;

namespace DeepCore.PomeloClient
{
    public class PomeloClientFactory : NetClientFactory
    {
        public static bool USE_SYNC_CONNETION = false;
        public static bool USE_TCP_ASYNC_RW = true; // 是否使用异步读写
        public static PomeloClientFactory IOInstance { get; private set; } = new PomeloClientFactory();
        public PomeloClientFactory() { IOInstance = this; }
        public static PomeloClientConfig Config { get; set; } = new PomeloClientConfig()
        {
            MaxPackageSize = 1024 * 1024 * 4,
            BufferSize = 4 * 1024,
            NoDelay = true,
        };
        public override IClientAdapter CreateAdapter(INetClient client)
        {
            if (USE_SYNC_CONNETION)
                return new PomeloSYN(client);
            else
                return new PomeloTCP(client, USE_TCP_ASYNC_RW);
        }
        public virtual PomeloClient CreateClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000)
        {
            return new PomeloClient(codec,name, request_timer_tick_ms);
        }
    }

    public struct PomeloClientConfig
    {
        public int MaxPackageSize;
        public int BufferSize;
        public bool NoDelay;
    }



}
