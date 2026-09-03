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
    /// Ice Name Server 启动器
    /// </summary>
    public class IceNameServerLauncher : NameServerLauncher
    {
        protected readonly Ice.Communicator _communicator;
        protected Ice.ObjectAdapter _adapter;
        protected IceNameServerAdapter _nameServer;
        private bool useIceGrid = false;
        public Ice.Communicator Communicator { get => _communicator; }
        public Ice.ObjectAdapter Adapter { get => _adapter; }
        new public IceNameServerAdapter NameServer { get => _nameServer; }
        public IceNameServerLauncher(Ice.Communicator communicator = null)
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
        protected override Remote.NameServer.IRpcNameServerAdapter CreateAdapter(RpcNameConfig cfg)
        {
            try
            {
                if (!useIceGrid && IPUtil.TryParseHostPort(cfg.LocalEndPoint, out var localHost, out var localPort))
                {
                    this._adapter = _communicator.createObjectAdapterWithEndpoints("NameServer", string.Format("default -h {0} -p {1}", localHost, localPort));
                }
                else
                {
                    this._adapter = _communicator.createObjectAdapter("NameServer");
                }
                this._nameServer = new IceNameServerAdapter(_communicator, cfg);
                this._adapter.add(_nameServer.CreateNameServerI(log), Ice.Util.stringToIdentity("NameServer"));
                this._adapter.add(new NameServerConsoleDispaciter(this), Ice.Util.stringToIdentity("NameServerConsole"));
                this._adapter.activate();
                return _nameServer;
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

        class NameServerConsoleDispaciter : IRpcNameServerConsoleDisp_
        {
            private IceNameServerLauncher box;

            public NameServerConsoleDispaciter(IceNameServerLauncher box)
            {
                this.box = box;
            }
            public override async Task<string> DoStartAsync(Ice.Current current = null)
            {
                box.log.Info("DoStartAsync");
                var list = await box.StartStaticServicesAsync(box.ConfigRoot);
                if (list != null)
                {
                    return CUtils.ArrayToString(list, "\n");
                }
                return "error";
            }
            public override async Task<string> DoCloseAsync(Ice.Current current = null)
            {
                box.log.Info("DoCloseAsync");
                await box.ShutdownSerivceAsync();
                return "close finish";
            }
            public override Task<string> DoStatAsync(Ice.Current current = null)
            {
                box.log.Info("DoStatAsync");
                return box.nameServer.GetNodeStatusAsync();
            }
            public override Task<string> DoCommandAsync(string cmd, Ice.Current current = null)
            {
                box.log.Info("AppCommandAsync");
                return box.BroadcastCommandAsync(cmd);
            }
            public override Task<long> PingAsync(long time, Ice.Current current = null)
            {
                box.log.Info("PingAsync");
                return Task.FromResult(time);
            }
        }
    }
}
