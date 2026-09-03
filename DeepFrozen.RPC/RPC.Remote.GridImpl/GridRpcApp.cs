using DeepCore;
using DeepCore.Net;
using DeepFrozen.RPC.Launcher;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.GridImpl
{
    public class GridRpcAppFactory : RpcAppFactory
    {
        public GridRpcAppFactory() { }
        public override NameServerLauncher CreateNameServerApp()
        {
            return new UVNameServerLauncher();
        }
        public override ServiceNodeLauncher CreateServiceNodeApp()
        {
            return new UVServiceNodeLauncher();
        }
    }

    public class UVNameServerLauncher : NameServerLauncher
    {
        protected override IRpcNameServerAdapter CreateAdapter(RpcNameConfig cfg)
        {
            IPUtil.TryParseHostPort(cfg.LocalEndPoint, out var localHost, out var localPort);
            var hostConfig = new Properties();
            {
                hostConfig["Name"] = "UVNameServer";
                hostConfig["Host"] = localHost;
                hostConfig["Listen"] = localPort.ToString();
            }
            try
            {
                var uv = new GridNameServerUVAdapter(hostConfig);
                return uv;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
            finally
            {
                Console.WriteLine(CUtils.SequenceChar('-', 100));
            }
        }
    }

    public class UVServiceNodeLauncher : ServiceNodeLauncher
    {
        protected override IRpcServiceNodeAdapter CreateAdapter(RpcNodeConfig cfg)
        {
            IPUtil.TryParseHostPort(cfg.LocalEndPoint, out var host, out var port);
            var hostConfig = new Properties();
            {
                hostConfig["Name"] = cfg.LocalNodeName;
                hostConfig["Host"] = host;
                hostConfig["Listen"] = port.ToString();
                hostConfig["MaxConnections"] = 1000.ToString();
            }
            return new GridServiceNodeUVAdapter(cfg, hostConfig);
        }
    }

}
