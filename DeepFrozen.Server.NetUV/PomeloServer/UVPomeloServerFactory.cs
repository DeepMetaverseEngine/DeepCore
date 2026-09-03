using DeepCore.IO;
using DeepCrystal.NetServer;
using DeepCrystal.Server;

namespace PomeloServer.NetUV
{
    public class UVPomeloServerFactory : ServerFactory
    {
        private static UVPomeloServerFactory s_instance;
        public static UVPomeloServerFactory SuperInstance
        {
            get
            {
                if (s_instance == null) { s_instance = new UVPomeloServerFactory(); }
                return s_instance;
            }
        }

        //private EventLoop eventLoop;
        public UVPomeloServerFactory()
        {
            UVPomeloServerFactory.s_instance = this;
            //this.eventLoop = new EventLoop();
        }
        public override IServer CreateServer(ServerConfig config, IExternalizableFactory codec)
        {
            return new UVPomeloServer(config, codec);
        }
        public override void Shutdown()
        {
            //this.eventLoop.ShutdownGracefullyAsync().Wait();
        }
    }

}
