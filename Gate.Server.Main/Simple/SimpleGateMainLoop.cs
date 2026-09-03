using DeepCore.IO;
using DeepCrystal.RPC;
using Gate.Client;
using Gate.Server.Launcher;
using Gate.Server.Service.Logic;
using Gate.Server.Service.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gate.Server.GateServerManager;

namespace Gate.Server.Main.Simple
{
    public class SimpleGateMainLoop<
        BattleCodec, ClientCodec, ServerCodec,
        ZoneDataFactory, ZoneHostFactory, ZoneSlaveFactory,
        GateServerManager, GateClientManager,
        LauncherService, LogicService, SessionService> : Launcher.GateMainLoop
        where BattleCodec : IExternalizableFactory, new()
        where ClientCodec : IExternalizableFactory, new()
        where ServerCodec : IExternalizableFactory, new()
        where ZoneDataFactory : DeepMetaGame.Data.ZoneDataFactory, new()
        where ZoneHostFactory : DeepCore.Game3D.Host.ZoneHostFactory, new()
        where ZoneSlaveFactory : DeepCore.Game3D.Slave.ZoneSlaveFactory, new()
        where GateServerManager : Gate.Server.GateServerManager, new()
        where GateClientManager : Gate.Client.GateClientManager, new()
        where LauncherService : Gate.Server.Launcher.GateLauncherService, new()
        where LogicService : Gate.Server.Service.Logic.LogicService, new()
        where SessionService : Gate.Server.Service.Session.SessionService, new()
    {
        public SimpleGateMainLoop()
        {
            new BattleCodec();
            new ZoneDataFactory();
            new ZoneHostFactory();
            new ZoneSlaveFactory();

            base.UseShellExecuteDB = false;
            base.StartServiceName /*        */= nameof(LauncherService);
            base.StartServiceType /*        */= typeof(LauncherService);
            base.GateServerManagerType /*   */= typeof(GateServerManager);
            base.GateClientManagerType /*   */= typeof(GateClientManager);
            base.BattleCodec = typeof(ClientCodec);
            base.ClientCodec = typeof(ClientCodec);
            base.ServerCodec = typeof(ServerCodec);
            base.GateListenPort = 19300;
            base.ServiceMapping.Put($"{ServerNameManager.LogicServiceType}", typeof(LogicService).FullName);
            base.ServiceMapping.Put($"{ServerNameManager.SessionServiceType}", typeof(SessionService).FullName);
        }
    }

    public class TinyGateMainLoop<
        BattleCodec, ClientCodec, ServerCodec,
        ZoneDataFactory, ZoneHostFactory, ZoneSlaveFactory> : GateMainLoop
        where BattleCodec : IExternalizableFactory, new()
        where ClientCodec : IExternalizableFactory, new()
        where ServerCodec : IExternalizableFactory, new()
        where ZoneDataFactory : DeepMetaGame.Data.ZoneDataFactory, new()
        where ZoneHostFactory : DeepCore.Game3D.Host.ZoneHostFactory, new()
        where ZoneSlaveFactory : DeepCore.Game3D.Slave.ZoneSlaveFactory, new()
    {
        public TinyGateMainLoop()
        {
            new BattleCodec();
            new ZoneDataFactory();
            new ZoneHostFactory();
            new ZoneSlaveFactory();

            base.UseShellExecuteDB = false;
            base.StartServiceName /*        */= nameof(GateLauncherService);
            base.StartServiceType /*        */= typeof(GateLauncherService);
            base.GateServerManagerType /*   */= typeof(GateServerManager);
            base.GateClientManagerType /*   */= typeof(GateClientManager);
            base.BattleCodec = typeof(ClientCodec);
            base.ClientCodec = typeof(ClientCodec);
            base.ServerCodec = typeof(ServerCodec);
            base.GateListenPort = 19300;
            base.ServiceMapping.Put($"{ServerNameManager.LogicServiceType}", typeof(LogicService).FullName);
            base.ServiceMapping.Put($"{ServerNameManager.SessionServiceType}", typeof(SessionService).FullName);
        }
    }
}
