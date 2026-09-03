using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepFrozen.ICE;
using DeepFrozen.ICE.NameServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ThreeLives;

public class TLNameServerBox : RpcServiceBox
{
    private Ice.ObjectAdapter adapter;
    private IceRpcNameServer nameServer;

    public override void start(string name, Ice.Communicator communicator, string[] args)
    {
        try
        {
            ReflectionUtil.LoadDlls(AppDomain.CurrentDomain, new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory),
                (domain, file) => System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(file.FullName),
                (domain, file, asmName) => System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName),
                0);
            base.start(name, communicator, args);
            //---------------------------------------------------------------------------------------------------------
            nameServer = new IceRpcNameServer(communicator, new TLServer.ServerCodec());
            adapter = communicator.createObjectAdapter("NameServer");
            adapter.add(nameServer.CreateNameServerI(), Ice.Util.stringToIdentity("NameServer"));
            adapter.add(new NameHostI(this), Ice.Util.stringToIdentity("NameServerConsole"));
            adapter.activate();
            nameServer.StartAsync().Wait();
            log.Info(CUtils.SequenceChar('-', 100));
            log.Info("NameServer Started !");
        }
        catch (System.Exception err)
        {
            log.Error(err.Message, err);
            throw;
        }
        finally
        {
            log.Info(CUtils.SequenceChar('-', 100));
        }
    }
    public override void stop()
    {
        try
        {
            nameServer.ShutdownAsync().Wait();
            nameServer.StopAsync().Wait();
            adapter.deactivate();
            BILogger.BICustomLoggerFactory.Dispose();
        }
        catch (System.Exception err)
        {
            log.Error(err.Message, err);
            throw;
        }
    }

    public async Task<DeepFrozen.RPC.Remote.RemoteProxyInfo> StartService(XmlElement xml, string serviceNode)
    {
        try
        {
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.WriteLine("- Starting Service : " + xml.Name);
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.WriteLine(xml.ToXmlString());
            var serviceName = xml["ServiceName"].GetXmlNodeText();
            var serviceType = xml["ServiceType"].GetXmlNodeText();
            var config = Properties.LoadFromXML(xml["Config"]);
            var ret = await nameServer.AddStaticServiceAsync(new DeepCrystal.RPC.RemoteAddress(serviceName, serviceNode, serviceType), config);
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            return ret;
        }
        catch (Exception err)
        {
            await Console.Error.WriteLineAsync(err.Message + Environment.NewLine + err.StackTrace);
            throw;
        }
    }

    class NameHostI : IRpcNameServerConsoleDisp_
    {
        private TLNameServerBox box;

        public NameHostI(TLNameServerBox nameServer)
        {
            this.box = nameServer;
        }
        public override async Task<string> DoStartAsync(Ice.Current current = null)
        {
            Console.WriteLine("DoStartAsync");
            using (var sb = StringBuilderObjectPool.AllocAutoRelease())
            {
                var nodes = box.RpcConfig["ServiceNodes"];
                if (nodes != null)
                {
                    foreach (var node in nodes.ChildNodes)
                    {
                        if (node is XmlElement nodeInfo)
                        {
                            var start = nodeInfo["StartService"];
                            foreach (var svc in start.ChildNodes)
                            {
                                if (svc is XmlElement svcInfo)
                                {
                                    var info = await box.StartService(svcInfo, nodeInfo.Name);
                                    if (info != null)
                                    {
                                        await sb.WriteLineAsync(info.Address.ToString());
                                    }
                                }
                            }
                        }
                    }
                    await box.nameServer.SetStaticReadyAsync();
                    box.nameServer.BroadcastSystemMessage(new DeepMMO.Server.SystemMessage.SystemStaticServicesStartedNotify());
                }
                return sb.ToString();
            }
        }
        public override async Task<string> DoCloseAsync(Ice.Current current = null)
        {
            Console.WriteLine("DoCloseAsync");
            await box.nameServer.ShutdownAsync();
            return "close finish";
        }
        public override Task<string> DoStatAsync(Ice.Current current = null)
        {
            Console.WriteLine("DoStatAsync");
            return box.nameServer.GetNodeStatusAsync();
        }
        public override Task<string> DoCommandAsync(string cmd, Ice.Current current = null)
        {
            Console.WriteLine("AppCommandAsync");
            return this.box.nameServer.Adapter.AppCommandAsync(cmd);
        }
        public override Task<long> PingAsync(long time, Ice.Current current = null)
        {
            Console.WriteLine("PingAsync");
            var rand = new Random();
            var delay = rand.Next() % 10000;
            Console.WriteLine("Handle Ping : " + time + " Delay : " + delay);
            return Task.Delay(delay).ContinueWith((t) =>
            {
                Console.WriteLine("Handle Ping Over : " + time);
                return time;
            });
        }
    }
}