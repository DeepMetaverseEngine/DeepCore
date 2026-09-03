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
    public class IceRpcAppFactory : RpcAppFactory
    {
        public override NameServerLauncher CreateNameServerApp()
        {
            return new IceNameServerLauncher();
        }
        public override ServiceNodeLauncher CreateServiceNodeApp()
        {
            return new IceServiceNodeLauncher();
        }
    }

  
    //-------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 用于连接NameServer控制台
    /// </summary>
    public class IceNameServerConsoleApplication : Ice.Application
    {
        public string ConfigIce = "tlserver_ice_console.client";
        public string ProxyPropName = "NameServerConsole.Proxy";
        public string RealmNameQuery = "ThreeLivesRealm/Query";
        public string NameServerConsoleType = "::DeepFrozen::NameServerConsole";
        public override int run(string[] args)
        {
            var prop = new DeepCore.Properties(communicator().getProperties().getPropertiesForPrefix(""));
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.Write(prop.ToString());
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            IRpcNameServerConsolePrx nameProxy;
            try
            {
                nameProxy = IRpcNameServerConsolePrxHelper.checkedCast(communicator().propertyToProxy(ProxyPropName));
            }
            catch (Ice.NotRegisteredException)
            {
                var query = IceGrid.QueryPrxHelper.checkedCast(communicator().stringToProxy(RealmNameQuery));
                nameProxy = IRpcNameServerConsolePrxHelper.checkedCast(query.findObjectByType(NameServerConsoleType));
            }
            //var hello = IRpcNameServerConsolePrxHelper.checkedCast(communicator.stringToProxy(string.Format("NameServerConsole:default -h {0} -p {1}", args[0], args[1])));
            if (nameProxy == null)
            {
                Console.Error.WriteLine("NameServerConsole.Proxy not found");
                return 1;
            }
            Console.Title = "IceGridConsole: " + nameProxy.ice_getIdentity();
            var console = new TestServerConsoleCommand(nameProxy);
            console.MainLoop("使用cmdlist列出所有指令");
            return 0;
        }

        public int Main(string[] args)
        {
            if (args.Length > 0)
            {
                ConfigIce = args[0];
            }
            if (args.Length > 1)
            {
                RealmNameQuery = args[1];
            }
            if (args.Length > 2)
            {
                NameServerConsoleType = args[2];
            }
            var app = new IceNameServerConsoleApplication();
            return app.main(args, ConfigIce);
        }

        //---------------------------------------------------------------------------------------------------------
        public class TestServerConsoleCommand : ConsoleCommandList
        {
            private static IRpcNameServerConsolePrx nameServer;

            public TestServerConsoleCommand(IRpcNameServerConsolePrx name)
            {
                nameServer = name;
                this.OnHandleCommand += TestServerConsoleCommand_OnHandleCommand;
                this.OnHandleUnknowCommand += TestServerConsoleCommand_OnHandleUnknowCommand;
            }
            private void TestServerConsoleCommand_OnHandleCommand(string line, AbstractCommand cmd)
            {
                if (cmd is CMD_LIST)
                {
                    var result = nameServer.DoCommand(line);
                    Console.WriteLine(result);
                }
            }
            private bool TestServerConsoleCommand_OnHandleUnknowCommand(string line)
            {
                var result = nameServer.DoCommand(line);
                Console.WriteLine(result);
                return true;
            }
            [Desc("NameServer开始服务")]
            public class CMD_START : AbstractCommand
            {
                public override string Key { get { return "start"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    nameServer.DoStart();
                }
            }
            [Desc("NameServer卸载服务")]
            public class CMD_CLOSE : AbstractCommand
            {
                public override string Key { get { return "close"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    nameServer.DoClose();
                }
            }
            [Desc("NameServer状态")]
            public class CMD_STAT : AbstractCommand
            {
                public override string Key { get { return "stat"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    var rst = nameServer.DoStat();
                    output.WriteLine(rst);
                }
            }
            [Desc("Test Ping")]
            public class CMD_TEST_PING : AbstractCommand
            {
                public override string Key { get { return "ping"; } }
                public override string Help { get { return "ping [count]"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    long time = 0;
                    int count = 1;
                    if (Parser.TryParseInt(arg, out count) == false)
                    {
                        count = 1;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        output.WriteLine("Send Ping : " + time);
                        nameServer.PingAsync(time).ContinueWith(t => { output.WriteLine("Send Ping Done : " + t.Result); });
                        time++;
                    }
                }
            }
            [Desc("Test Ping")]
            public class CMD_TEST_PINGS : AbstractCommand
            {
                public override string Key { get { return "pings"; } }
                public override string Help { get { return "pings [count]"; } }
                public override void DoCommand(string arg, TextWriter output)
                {
                    Task.Run(async () =>
                    {
                        long time = 0;
                        int count = 1;
                        if (Parser.TryParseInt(arg, out count) == false)
                        {
                            count = 1;
                        }
                        for (int i = 0; i < count; i++)
                        {
                            output.WriteLine("Send Pings : " + time);
                            var rst = await nameServer.PingAsync(time);
                            output.WriteLine("Send Pings Done : " + rst);
                            time++;
                        }
                    });
                }
            }
        }

    }

}


