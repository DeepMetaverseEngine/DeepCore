using DeepCore;
using DeepCore.Net;
using DeepCore.Reflection;
using DeepCrystal.Command;
using DeepFrozen.ICE.NameServer;
using DeepFrozen.ICE.ServiceNode;
using DeepFrozen.RPC.Launcher;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozen.RPC.Remote.ServiceNode;
using DeepFrozenIceImpl;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.ICE.Launcher
{

    //-------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Ice Service Node 启动器
    /// </summary>
    public class IceServiceNodeLauncher : ServiceNodeLauncher
    {
        protected readonly Ice.Communicator _communicator;
        protected Ice.ObjectAdapter _adapter;
        protected IceServiceNodeAdapter _serviceNode;
        private bool useIceGrid = false;
        public Ice.Communicator Communicator { get => _communicator; }
        public Ice.ObjectAdapter Adapter { get => _adapter; }
        public IceServiceNodeAdapter ServiceAdapter { get => _serviceNode; }
        public IceServiceNodeLauncher(Ice.Communicator communicator = null)
        {
            if (communicator == null)
            {
                communicator = Ice.Util.initialize();
            }
            else
            {
                useIceGrid = true;
            }
            this._communicator = communicator;
        }
        protected override IRpcServiceNodeAdapter CreateAdapter(RpcNodeConfig cfg)
        {
            if (!useIceGrid && IPUtil.TryParseHostPort(cfg.LocalEndPoint, out var localHost, out var localPort))
            {
                this._adapter = _communicator.createObjectAdapterWithEndpoints(cfg.LocalNodeName, string.Format("default -h {0} -p {1}", localHost, localPort));
            }
            else
            {
                cfg.NameServerEndPoint = "NameServer";
                this._adapter = _communicator.createObjectAdapter(cfg.LocalNodeName);
            }
            this._serviceNode = new IceServiceNodeAdapter(_communicator, cfg);
            this._adapter.add(_serviceNode.CreateServiceNodeI(log), Ice.Util.stringToIdentity(cfg.LocalNodeName));
            this._adapter.activate();
            return _serviceNode;
        }

        public override async Task<bool> StopAsync()
        {
            if (await base.StopAsync())
            {
                try
                {
                    _adapter.deactivate();
                    _communicator.shutdown();
                    _communicator.waitForShutdown();
                    return true;
                }
                catch (System.Exception err)
                {
                    log.Error(err.Message, err);
                    throw;
                }
            }
            return false;
        }

    }

}


