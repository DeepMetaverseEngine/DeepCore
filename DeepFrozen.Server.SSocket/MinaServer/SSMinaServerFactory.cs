using DeepCore.MinaClient;
using DeepCore.Protocol;
using DeepCrystal.Server;
using DeepCrystal.SharpMinaServer;

namespace DeepFrozen.Server.SSocket.NetServer
{
    public class SSMinaServerFactory : IMinaServerFactory
    {
        public static int SEND_BUFF_SIZE = 1024;

        public virtual IMinaServer CreateServer(ServerConfig sconfig,INetPackageCodec codec)
        {
            return new SSMinaServer(sconfig, codec, new MessageReceiveFilterFactory(codec));
        }
    }
}
