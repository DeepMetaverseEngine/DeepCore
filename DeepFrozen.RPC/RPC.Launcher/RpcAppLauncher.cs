using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Reflection;
using DeepCore.Statistics;
using DeepCore.Threading;
using DeepCore.Xml;
using DeepCrystal.Command;
using DeepCrystal.ORM;
using DeepCrystal.RPC;
using DeepCrystal.RPC.Protocol;
using DeepFrozen.RPC.Command;
using DeepFrozen.RPC.Invoker;
using DeepFrozen.RPC.Remote;
using DeepFrozen.RPC.Remote.InAppImpl;
using DeepFrozen.RPC.Remote.NameServer;
using DeepFrozen.RPC.Remote.ServiceNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DeepFrozen.RPC.Launcher
{
    [Reflectible]
    public abstract class RpcAppFactory
    {
        public static void DEBUG_ON(bool enable)
        {
            TypeAllocRecorder.ENABLE_STATISTICS = enable;
            TypeAllocRecorder.VERBOS = enable;
            ObjectPools.EnableStatistics = enable;
            TimeStatisticsRecoder.Enable = enable;
            ORMStatistics.EnableStatistics = enable;
            ORMFactory.IsTest = enable;
            RpcStatistics.Enable = enable;
        }
        static public RpcAppFactory Instance { get; private set; } = new InAppRpcAppFactory();
        public DeepCore.Log.Logger log { get; private set; }
        public RpcAppFactory()
        {
            Instance = this;
            this.log = DeepCore.Log.LoggerFactory.GetLogger(GetType().Name);
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        protected virtual void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            log.Error(sender, (Exception)e.ExceptionObject);
        }
        protected virtual void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            log.Error(sender, e.Exception);
        }
        public abstract ServiceNodeLauncher CreateServiceNodeApp();
        public abstract NameServerLauncher CreateNameServerApp();
        public virtual RpcAppConsoleCommandList CreateConsoleCommand(RpcAppLauncher app)
        {
            return new RpcAppConsoleCommandList(app);
        }
        //------------------------------------------------------------------------------------------

        public static bool ForEachServiceNodes(XmlDocument doc, BreakPredicate<XmlElement, RpcNodeConfig> action)
        {
            var nodes = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "ServiceNodes");
            if (nodes != null)
            {
                foreach (XmlNode e in nodes.ChildNodes.ToList())
                {
                    if (e is XmlElement xnode)
                    {
                        if (TryGetNodeConfig(xnode, out var nodeConfig))
                        {
                            if (action(xnode, nodeConfig))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static bool ForEachStartService(XmlDocument doc, BreakPredicate<XmlElement, RpcStartService> action)
        {
            var nodes = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "ServiceNodes");
            if (nodes != null)
            {
                foreach (XmlNode e in nodes.ChildNodes.ToList())
                {
                    if (e is XmlElement xnode)
                    {
                        var xstart = xnode["StartService"];
                        if (xstart != null)
                        {
                            var nodeName = xnode["RpcConfig"]["LocalNodeName"].GetXmlNodeText();
                            foreach (XmlNode e2 in xstart.ChildNodes.ToList())
                            {
                                if (e2 is XmlElement xsvc)
                                {
                                    var serviceName = xsvc["ServiceName"].GetXmlNodeText();
                                    var serviceType = xsvc["ServiceType"].GetXmlNodeText();
                                    var config = Properties.LoadFromXML(xsvc["Config"]);
                                    var startConfig = new RpcStartService()
                                    {
                                        Address = new RemoteAddress(serviceName, nodeName, serviceType),
                                        Config = config ?? new Properties(),
                                        IsStatic = true,
                                    };
                                    if (action(xsvc, startConfig))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static async Task<bool> ForEachServiceNodesAsync(XmlDocument doc, BreakPredicateAsync<XmlElement, RpcNodeConfig> action)
        {
            var nodes = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "ServiceNodes");
            if (nodes != null)
            {
                foreach (XmlNode e in nodes.ChildNodes.ToList())
                {
                    if (e is XmlElement xnode)
                    {
                        if (TryGetNodeConfig(xnode, out var nodeConfig))
                        {
                            if (await action(xnode, nodeConfig))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static async Task<bool> ForEachStartService(XmlDocument doc, BreakPredicateAsync<XmlElement, RpcStartService> action)
        {
            var nodes = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "ServiceNodes");
            if (nodes != null)
            {
                foreach (XmlNode e in nodes.ChildNodes.ToList())
                {
                    if (e is XmlElement xnode)
                    {
                        var xstart = xnode["StartService"];
                        if (xstart != null)
                        {
                            var nodeName = xnode["RpcConfig"]["LocalNodeName"].GetXmlNodeText();
                            foreach (XmlNode e2 in xstart.ChildNodes.ToList())
                            {
                                if (e2 is XmlElement xsvc)
                                {
                                    var serviceName = xsvc["ServiceName"].GetXmlNodeText();
                                    var serviceType = xsvc["ServiceType"].GetXmlNodeText();
                                    var config = Properties.LoadFromXML(xsvc["Config"]);
                                    var startConfig = new RpcStartService()
                                    {
                                        Address = new RemoteAddress(serviceName, nodeName, serviceType),
                                        Config = config ?? new Properties(),
                                        IsStatic = true,
                                    };
                                    if (await action(xsvc, startConfig))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool TryGetNodeConfig(XmlElement xnode, out RpcNodeConfig nodeConfig)
        {
            var rpc_config = xnode["RpcConfig"];
            if (rpc_config != null)
            {
                nodeConfig = new RpcNodeConfig()
                {
                    LocalEndPoint = rpc_config["LocalEndPoint"].GetXmlNodeText(),
                    LocalNodeName = rpc_config["LocalNodeName"].GetXmlNodeText(),
                    NameServerEndPoint = rpc_config["NameServerEndPoint"].GetXmlNodeText(),
                    RpcCodec = GetCodec(rpc_config["RpcCodec"]),
                    AcceptTypeMappings = Properties.LoadFromXML(rpc_config["AcceptTypeMappings"]),
                    RequestTickTimeMS = rpc_config["RequestTickTimeMS"].GetXmlNodeTextAs<int>(),
                    NetworkTimeoutMS = rpc_config["NetworkTimeoutMS"].GetXmlNodeTextAs<int>(),
                    DefaultTaskExecuteTimeout = rpc_config["DefaultTaskExecuteTimeout"].GetXmlNodeTextAs<int>(),
                };
                if (nodeConfig.RpcCodec == null)
                {
                    nodeConfig.RpcCodec = new DummyMessageCodec();
                }
                return true;
            }
            else
            {
                nodeConfig = default(RpcNodeConfig);
                return false;
            }
        }
        public static bool TryGetNameConfig(XmlDocument doc, out XmlElement nameRoot, out RpcNameConfig nameConfig)
        {
            nameRoot = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "NameServer");
            if (nameRoot != null)
            {
                var rpc_config = nameRoot["RpcConfig"];
                nameConfig = new RpcNameConfig()
                {
                    LocalEndPoint = rpc_config["LocalEndPoint"].GetXmlNodeText(),
                    NetworkTimeoutMS = rpc_config["NetworkTimeoutMS"].GetXmlNodeTextAs<int>(),
                    RpcCodec = GetCodec(rpc_config["RpcCodec"]),
                };
                return true;
            }
            else
            {
                nameConfig = default(RpcNameConfig);
                return false;
            }
        }
        public static bool TryGetGlobalConfig(XmlDocument doc, out XmlElement globalRoot, out Properties globalMap)
        {
            globalRoot = XmlUtil.FindChild<XmlElement>(doc.DocumentElement, "GlobalConfig");
            if (globalRoot != null)
            {
                globalMap = Properties.LoadFromXML(globalRoot);
                return true;
            }
            globalMap = null;
            return false;
        }
        public static IExternalizableFactory GetCodec(XmlElement e)
        {
            if (e == null || string.IsNullOrEmpty(e.GetXmlNodeText()))
                return new DummyMessageCodec();
            return ReflectionUtil.CreateInterface<IExternalizableFactory>(e.GetXmlNodeText());
        }

        //------------------------------------------------------------------------------------------


    }

    public class RpcAppConsoleCommandList : RPCServerConsoleCommandList
    {
        public RpcAppLauncher App { get; private set; }
        public DeepCore.Log.Logger log { get; private set; }
        public RpcAppConsoleCommandList(RpcAppLauncher app)
        {
            this.log = LoggerFactory.GetLogger(app.Name);
            this.OutputDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ".state", app.Name));
            RpcApplication.Instance.OnAppCommandAsync += OnHandleAppCommandAsync;
        }
        protected override bool TryReadLine(TextReader input, out string cmd)
        {
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                return base.TryReadLine(input, out cmd);
            }
            else
            {
                cmd = null;
                return false;
            }
        }
        protected virtual async Task<string> OnHandleAppCommandAsync(string command)
        {
            log.Info("Handle App Command : " + command);
            if (DoCommand(command, out var output))
            {
                await Task.CompletedTask;
            }
            else
            {
                output = "Unknow Command : " + command + Environment.NewLine + ListCommand();
            }
            log.Info(output);
            return $"{output}";
        }


        //         public void BindNameServer(NameServerLauncher app) : base(app)
        //         {
        //             this.app = app;
        //             //                 this.OnHandleUnknowCommand += TestServerConsoleCommand_OnHandleUnknowCommand;
        //             //                 this.OnHandleCommand += TestServerConsoleCommand_OnHandleCommand;
        //         }
        //             protected virtual void TestServerConsoleCommand_OnHandleCommand(string line, AbstractCommand cmd)
        //             {
        // //                 var result = box.NameServer.BroadcastCommandAsync(line).WaitForResult();
        // //                 box.log.Info(Environment.NewLine + result);
        //             }
        //             protected virtual bool TestServerConsoleCommand_OnHandleUnknowCommand(string line)
        //             {
        // //                 var result = box.NameServer.BroadcastCommandAsync(line).WaitForResult();
        // //                 box.log.Info(Environment.NewLine + result);
        //                 return true;
        //             }

        [Desc("Start All Services")]
        public class CMD_SSTART : AbstractCommand<RpcAppConsoleCommandList>
        {
            public override string Key { get { return "sstart"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (CmdList.App is NameServerLauncher app)
                {
                    var launched = app.StartStaticServicesAsync(app.ConfigRoot).WaitForResult();
                    app.log.Info(CUtils.SequenceChar('-', 100));
                    if (launched != null)
                    {
                        foreach (var prx in launched)
                        {
                            app.log.Info(prx?.ToString());
                        }
                        app.log.Info("Start Services Finish !!!");
                    }
                    else
                    {
                        app.log.Error("no name server");
                    }
                    app.log.Info(CUtils.SequenceChar('-', 100));
                }
            }
        }

        [Desc("Shutdown All Services")]
        public class CMD_SSTOP : AbstractCommand<RpcAppConsoleCommandList>
        {
            public override string Key { get { return "sstop"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (CmdList.App is NameServerLauncher app)
                {
                    if (!app.ShutdownSerivceAsync().WaitForResult())
                    {
                        app.log.Error("no name server");
                    }
                    else
                    {
                        app.log.Info("Stop Services Finish !!!");
                    }
                    app.log.Info(CUtils.SequenceChar('-', 100));
                }
            }
        }

        [Desc("Show All Services Count")]
        public class CMD_SCOUNT : AbstractCommand<RpcAppConsoleCommandList>
        {
            public override string Key { get { return "scount"; } }
            public override async void DoCommand(string arg, TextWriter output)
            {
                if (CmdList.App is NameServerLauncher app)
                {
                    var count = await app.NameServer.GetAllServicesCountAsync();
                    app.log.Info(Environment.NewLine + "Service Count = " + count);
                }
            }
        }

        [Desc("Broadcast Command To All Nodes")]
        public class CMD_BroadcastCommand : AbstractCommand<RpcAppConsoleCommandList>
        {
            public override string Key { get { return "bc"; } }
            public override string Help { get { return "Post Command To Services\nbc <command>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (CmdList.App is NameServerLauncher app)
                {
                    var result = app.BroadcastCommandAsync(arg).WaitForResult();
                    app.log.Info(Environment.NewLine + result);
                }
            }
        }

    }
    //-----------------------------------------------------------------------------------------------------------------------------------------------------------

    public abstract class RpcAppLauncher
    {
        public abstract string Name { get; }
        public DeepCore.Log.Logger log { get; protected set; }
        public RpcAppConsoleCommandList CreateConsoleCommand()
        {
            //   return new RpcAppConsoleCommandList(this);
            return RpcAppFactory.Instance.CreateConsoleCommand(this);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// NameServer启动器
    /// </summary>
    public abstract class NameServerLauncher : RpcAppLauncher
    {
        protected RpcNameServer nameServer;
        private bool started = false;
        public RpcNameServer NameServer { get => nameServer; }
        public XmlDocument ConfigRoot { get; private set; }
        public override string Name => "NameServer";
        protected abstract IRpcNameServerAdapter CreateAdapter(RpcNameConfig cfg);
        public virtual async Task<bool> StartAsync(XmlDocument doc)
        {
            this.ConfigRoot = doc;
            if (RpcAppFactory.TryGetNameConfig(doc, out var nameRoot, out var cfg))
            {
                try
                {
                    Console.WriteLine(CUtils.SequenceChar('-', 100));
                    Console.WriteLine("- Starting Name Server -");
                    Console.WriteLine(nameRoot.ToXmlString());
                    Console.WriteLine(CUtils.SequenceChar('-', 100));
                    this.log = LoggerFactory.GetLogger("NameServer");
                    this.nameServer = new RpcNameServer(cfg, CreateAdapter(cfg));
                    await this.nameServer.StartAsync();
                    return true;
                }
                catch (Exception err)
                {
                    log.Error(err);
                    throw;
                }
                finally
                {
                    Console.WriteLine(CUtils.SequenceChar('-', 100));
                }
            }
            return false;
        }
        public virtual async Task<RemoteProxyInfo> StartStaticServiceAsync(XmlElement xnode, RpcStartService start)
        {
            try
            {
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                Console.WriteLine(xnode.ToXmlString());
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                var prx = await nameServer.AddStaticServiceAsync(start.Address, start.Config);
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                return prx;
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public virtual async Task<RemoteProxyInfo[]> StartStaticServicesAsync(XmlDocument doc)
        {
            if (nameServer != null && started == false)
            {
                started = true;
                var ret = new List<RemoteProxyInfo>();
                var list = new List<KeyValuePair<XmlElement, RpcStartService>>();
                RpcAppFactory.ForEachStartService(doc, (xnode, start) =>
                 {
                     list.Add(new KeyValuePair<XmlElement, RpcStartService>(xnode, start));
                     return false;
                 });
                foreach (var e in list)
                {
                    var prx = await StartStaticServiceAsync(e.Key, e.Value);
                    ret.Add(prx);
                }
                if (await nameServer.SetStaticReadyAsync())
                {
                    this.Broadcast(new SystemStaticServicesStartedNotify());
                }
                return ret.ToArray();
            }
            return null;
        }
        public virtual async Task WaitForStaticNodesRegisteredAsync(XmlDocument doc)
        {
            var nodeNames = new HashMap<string, RpcNodeConfig>();
            RpcAppFactory.ForEachServiceNodes(doc, (xnode, nodeConfig) =>
            {
                nodeNames.Add(nodeConfig.LocalNodeName, nodeConfig);
                return false;
            });
            while (nodeNames.Count > 0)
            {
                var started = await nameServer.GetAllNodesAsync();
                foreach (var xn in started)
                {
                    nodeNames.Remove(xn.NodeName);
                }
                await Task.Delay(1000);
            }
        }
        public virtual async Task WaitForAllNodesUnregisteredAsync()
        {
            while (await nameServer.GetAllNodesCountAsync() > 0)
            {
                await Task.Delay(100);
            }
        }

        public virtual bool Broadcast(ISerializable msg)
        {
            if (nameServer != null)
            {
                nameServer.BroadcastSystemMessage(msg);
                return true;
            }
            return false;
        }
        public virtual Task<string> BroadcastCommandAsync(string msg)
        {
            if (nameServer != null)
            {
                return nameServer.BroadcastCommandAsync(msg);
            }
            return Task.FromResult(string.Empty);
        }
        public virtual async Task<bool> ShutdownSerivceAsync()
        {
            if (nameServer != null)
            {
                await nameServer.ShutdownSerivceAsync();
                return true;
            }
            return false;
        }
        public virtual async Task<bool> StopAsync()
        {
            if (nameServer != null)
            {
                try
                {
                    await nameServer.ShutdownSerivceAsync();
                    await nameServer.StopAsync();
                    nameServer.Dispose();
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

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// ServiceNode启动器
    /// </summary>
    public abstract class ServiceNodeLauncher : RpcAppLauncher
    {
        protected RpcServiceNode serviceNode;
        public RpcServiceNode ServiceNode { get => serviceNode; }
        public string NodeName { get; private set; }
        public override string Name => NodeName;
        public XmlElement ConfigElement { get; private set; }
        protected abstract IRpcServiceNodeAdapter CreateAdapter(RpcNodeConfig cfg);
        public virtual async Task<bool> StartAsync(XmlElement xnode)
        {
            this.ConfigElement = xnode;
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.WriteLine("- Starting Node Server -");
            Console.WriteLine(xnode.ToXmlString());
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            try
            {
                if (RpcAppFactory.TryGetNodeConfig(xnode, out var nodeConfig))
                {
                    this.NodeName = nodeConfig.LocalNodeName;
                    this.log = LoggerFactory.GetLogger(nodeConfig.LocalNodeName);
                    this.serviceNode = new RpcServiceNode(nodeConfig, CreateAdapter(nodeConfig));
                    return await this.serviceNode.StartAsync();
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
            finally
            {
                Console.WriteLine(CUtils.SequenceChar('-', 100));
            }
        }
        public virtual async Task<bool> ShutdownAsync()
        {
            if (serviceNode != null)
            {
                try
                {
                    await serviceNode.ShutdownAsync();
                }
                catch (System.Exception err)
                {
                    log.Error(err.Message, err);
                    throw;
                }
                return true;
            }
            return false;
        }
        public virtual async Task<bool> StopAsync()
        {
            if (serviceNode != null)
            {
                try
                {
                    await serviceNode.ShutdownAsync();
                }
                catch (System.Exception err)
                {
                    log.Error(err.Message, err);
                    throw;
                }
                try
                {
                    await serviceNode.StopAsync();
                }
                catch (System.Exception err)
                {
                    log.Error(err.Message, err);
                    throw;
                }
                finally
                {
                    serviceNode.Dispose();
                    serviceNode = null;
                }
                return true;
            }
            return false;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// ServiceNode组启动器，用于单进程启动所有节点
    /// </summary>
    public class ServiceNodeAppGroup
    {
        private List<ServiceNodeLauncher> nodes = new List<ServiceNodeLauncher>();
        private HashMap<string, ServiceNodeLauncher> nodeMap = new HashMap<string, ServiceNodeLauncher>();
        public XmlDocument ConfigRoot { get; private set; }

        public bool InitGlobalConfig(XmlDocument doc)
        {
            this.ConfigRoot = doc;
            if (RpcAppFactory.TryGetGlobalConfig(doc, out var globalRoot, out var globalMap))
            {
                IService.GlobalConfig = new Properties(globalMap);
                return true;
            }
            return false;
        }
        public async Task<int> StartNodesAsync(XmlDocument doc)
        {
            int ret = 0;
            var list = new List<KeyValuePair<XmlElement, RpcNodeConfig>>();
            RpcAppFactory.ForEachServiceNodes(doc, (xnode, nodeConfig) =>
            {
                list.Add(new KeyValuePair<XmlElement, RpcNodeConfig>(xnode, nodeConfig));
                return false;
            });
            foreach (var e in list)
            {
                try
                {
                    var node = RpcAppFactory.Instance.CreateServiceNodeApp();
                    await node.StartAsync(e.Key);
                    lock (nodeMap)
                    {
                        nodeMap.Add(node.NodeName, node);
                        nodes.Add(node);
                    }
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                    throw;
                }
                ret++;
            }
            return ret;
        }
        public async Task StopAsync()
        {
            using (var list = new ArrayList<ServiceNodeLauncher>())
            {
                lock (nodeMap)
                {
                    list.AddRange(nodes);
                    list.Reverse();
                }
                foreach (var node in list)
                {
                    try
                    {
                        await node.ShutdownAsync();
                    }
                    catch (Exception err)
                    {
                        Console.Error.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                    }
                }
                foreach (var node in list)
                {
                    try
                    {
                        await node.StopAsync();
                    }
                    catch (Exception err)
                    {
                        Console.Error.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                    }
                }
                lock (nodeMap)
                {
                    nodes.Clear();
                    nodeMap.Clear();
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------

}
