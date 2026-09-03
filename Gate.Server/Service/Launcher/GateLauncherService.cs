using DeepCrystal.RPC;
using Quartz.Impl.AdoJobStore.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Launcher
{
    public class GateLauncherService : IService
    {
        public string GateListenHost = "127.0.0.1";
        public int GateListenPort = 19800;
        public GateLauncherService(ServiceStartInfo start) : base(start)
        {
            if (start.Config.TryGetValue(nameof(GateListenHost), out var host))
            {
                GateListenHost = host;
            }
            if (start.Config.TryGetAsInt(nameof(GateListenPort), out var port))
            {
                GateListenPort = port;
            }
        }
        protected override void OnDisposed()
        {
        }
        protected override async Task OnStartAsync()
        {
#if false
            await Provider.CreateAsync(GateServerManager.ServerName.GetWorldManagerService(SelfNode));
#endif
            var port = GateListenPort;
            await Provider.CreateAsync(GateServerManager.ServerName.GetGateService(SelfNode), new
            {
                Host = GateListenHost,
                Port = port++,
                NetCodec = GateServerManager.Config.ClientCodecClass,
            });
            await Provider.CreateAsync(GateServerManager.ServerName.GetConnectService("1", SelfNode), new
            {
                Host = GateListenHost,
                Port = port++,
                NetCodec = GateServerManager.Config.ClientCodecClass,
            });
            await Provider.CreateAsync(GateServerManager.ServerName.GetConnectService("2", SelfNode), new
            {
                Host = GateListenHost,
                Port = port++,
                NetCodec = GateServerManager.Config.ClientCodecClass,
            });
        }
        protected override async Task OnStopAsync()
        {
        }

    }
}
