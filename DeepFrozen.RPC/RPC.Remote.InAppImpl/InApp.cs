using DeepCore;
using DeepCore.Net;
using DeepFrozen.RPC.Launcher;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.InAppImpl
{
    public class InAppRpcAppFactory : RpcAppFactory
    {
        public InAppRpcAppFactory() { }
        public override NameServerLauncher CreateNameServerApp()
        {
            return new InAppNameServerLauncher();
        }
        public override ServiceNodeLauncher CreateServiceNodeApp()
        {
            return new InAppServiceNodeLauncher();
        }
        internal class InAppNameServerLauncher : NameServerLauncher
        {
            protected override IRpcNameServerAdapter CreateAdapter(RpcNameConfig cfg)
            {
                return new InAppNameServerNode();
            }
        }
        internal class InAppServiceNodeLauncher : ServiceNodeLauncher
        {
            protected override IRpcServiceNodeAdapter CreateAdapter(RpcNodeConfig cfg)
            {
                return new InAppRpcServiceNode();
            }
        }
    }
 

}
