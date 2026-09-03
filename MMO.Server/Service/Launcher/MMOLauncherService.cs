using DeepCrystal.RPC;
using Quartz.Impl.AdoJobStore.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Launcher
{
    public class MMOLauncherService : GateLauncherService
    {
        public MMOLauncherService(ServiceStartInfo start) : base(start)
        {

        }
        protected override async Task OnStartAsync()
        {
            await Provider.CreateAsync(MMOServerManager.MMOServerName.GetAreaManagerService(SelfAddress.ServiceNode));
            await Provider.CreateAsync(MMOServerManager.MMOServerName.GetAreaService("", SelfAddress.ServiceNode));

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

    }
}
