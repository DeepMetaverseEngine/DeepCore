using DeepCore;
using DeepCore.IO;
using DeepCore.Threading;
using DeepCore.Xml;
using DeepCrystal;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Remote.InAppImpl;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DeepFrozen.RPC.Launcher
{
    //-------------------------------------------------------------------------------------------
    /// <summary>
    /// 进程启动容器
    /// </summary>
    public abstract class RpcServiceMain
    {
        public string Name { get; private set; }
        public DeepCore.Log.Logger log { get; private set; }
        public XmlDocument ConfigRoot { get; private set; }
        public RpcAppConsoleCommandList RpcConsole { get; private set; }
        public abstract RpcAppLauncher Launcher { get; }
        public RpcServiceMain(string name)
        {
            this.Name = name;
            this.log = DeepCore.Log.LoggerFactory.GetLogger(name);
        }
        protected abstract bool DoStart(XmlDocument doc);
        protected abstract bool DoStop();
        protected RpcAppConsoleCommandList CreateRpcConsole(RpcAppLauncher app)
        {
            //return app.CreateConsoleCommand();
            return RpcAppFactory.Instance.CreateConsoleCommand(app);
        }
        public void MainLoop(FileInfo xmlFile)
        {
            if (!xmlFile.Exists)
            {
                throw new Exception("CAN NOT FOUND ROOT XML : " + xmlFile.FullName);
            }
            Environment.CurrentDirectory = xmlFile.Directory.FullName;
            Console.WriteLine("ROOT XML = " + xmlFile.FullName);
            this.MainLoop(XmlUtil.LoadXML(xmlFile, true));
        }
        public void MainLoop(XmlDocument doc)
        {
            Console.Title = this.Name + " : PID=" + Process.GetCurrentProcess().Id;
            this.ConfigRoot = doc;
            //---------------------------------------------------------------------------------------------------------
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.WriteLine("Start " + this.Name);
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            Console.WriteLine(XmlUtil.ToXmlString(doc));
            Console.WriteLine(CUtils.SequenceChar('-', 100));
            //---------------------------------------------------------------------------------------------------------
            try
            {
                if (this.DoStart(doc))
                {
                    Console.WriteLine(CUtils.SequenceChar('-', 100));
                    this.RpcConsole = CreateRpcConsole(Launcher);
                    try
                    {
                        RpcConsole.MainLoop("Use 'cmdlist' To List Commands");
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                    }
                    finally
                    {
                        Console.WriteLine(CUtils.SequenceChar('-', 100));
                        Console.WriteLine("Stop " + this.Name);
                        Console.WriteLine(CUtils.SequenceChar('-', 100));
                        this.DoStop();
                        Console.WriteLine(CUtils.SequenceChar('-', 100));
                    }
                }
            }
            catch (System.Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
            finally
            {
                CUtils.PrintGCFinalizers(Console.Out);
            }
        }

    }

    //-------------------------------------------------------------------------------------------
    /// <summary>
    /// NameServer启动进程
    /// </summary>
    public class RpcNameServerMain : RpcServiceMain
    {
        public bool AutoStartService { get; set; } = true;
        public NameServerLauncher NameServer { get; private set; }
        public override RpcAppLauncher Launcher { get => NameServer; }
        public RpcNameServerMain() : base("NameServer")
        {
        }
        protected override bool DoStart(XmlDocument doc)
        {
            NameServer = RpcAppFactory.Instance.CreateNameServerApp();
            if (StartNameServer(doc))
            {
                return StartStaticServices(doc);
            }
            throw new Exception("Can Not Start NameServer : " + this.Name);
        }
        protected override bool DoStop()
        {
            NameServer.ShutdownSerivceAsync().Wait();
            var result = NameServer.BroadcastCommandAsync("exit").WaitForResult();
            log.Info(Environment.NewLine + result);
            log.Info("Waitting For All Nodes Unregistered ...");
            NameServer.WaitForAllNodesUnregisteredAsync().Wait();
            NameServer.StopAsync().Wait();
            return true;
        }
        protected virtual bool StartNameServer(XmlDocument doc)
        {
            return NameServer.StartAsync(doc).WaitForResult();
        }
        protected virtual bool StartStaticServices(XmlDocument doc)
        {
            log.Info("Waitting For Static Nodes Registered ...");
            NameServer.WaitForStaticNodesRegisteredAsync(doc).Wait();
            if (AutoStartService)
            {
                log.Info("Auto Start Static Services");
                NameServer.StartStaticServicesAsync(doc).WaitForResult();
            }
            return true;
        }
    }

    //-------------------------------------------------------------------------------------------
    /// <summary>
    /// ServiceNode启动进程
    /// </summary>
    public class RpcServiceNodeMain : RpcServiceMain
    {
        public ServiceNodeLauncher ServiceNode { get; private set; }
        public override RpcAppLauncher Launcher { get => ServiceNode; }

        public RpcServiceNodeMain(string name) : base(name)
        {
        }
        protected override bool DoStart(XmlDocument doc)
        {
            if (InitGlobalConfig(doc))
            {
                var result = RpcAppFactory.ForEachServiceNodes(doc, (xnode, nodeConfig) =>
                {
                    if (nodeConfig.LocalNodeName == this.Name)
                    {
                        StartService(xnode);
                        return true;
                    }
                    return false;
                });
                return result;
            }
            throw new Exception("Can Not Find Service : " + this.Name);
        }

        protected virtual bool InitGlobalConfig(XmlDocument doc)
        {
            if (RpcAppFactory.TryGetGlobalConfig(doc, out var globalRoot, out var globalMap))
            {
                IService.GlobalConfig = new Properties(globalMap);
                return true;
            }
            return false;
        }

        protected virtual bool StartService(XmlElement xnode)
        {
            ServiceNode = RpcAppFactory.Instance.CreateServiceNodeApp();
            return ServiceNode.StartAsync(xnode).WaitForResult();
        }

        protected override bool DoStop()
        {
            lock (this)
            {
                ServiceNode?.StopAsync().Wait();
                ServiceNode = null;
            }
            return true;
        }

    }

    //-------------------------------------------------------------------------------------------

    public class SingleNodeLauncherArgs
    {
        public string NodeName = "node1";
        public string ServiceName;
        public string ServiceType;
        public Properties ServiceConfig = null;
        public Type RpcCodec = null;
        public Properties ServiceMapping = null;
        public Properties GlobalConfig = null;
    }
    /// <summary>
    /// 可以一键启动RPC程序，单进程全节点
    /// </summary>
    public class SingleNodeLauncher
    {

        public delegate void OnStartHandler(SingleNodeLauncher launcher);
        public delegate void OnShutdownHandler(SingleNodeLauncher launcher);
        public delegate void OnInitGlobalConfigHandler(SingleNodeLauncher launcher, XmlDocument config);
        public delegate Task OnStartNameServerCompletedHander(SingleNodeLauncher launcher, XmlDocument config);
        public delegate Task OnStartNodesCompletedHander(SingleNodeLauncher launcher, XmlDocument config);
        public delegate void OnConsoleMainLoopHandler(SingleNodeLauncher launcher, RpcAppConsoleCommandList cmdlist);
        public delegate bool OnConsoleHandleUnknowCommandHander(SingleNodeLauncher launcher, string command);

        public event OnStartHandler OnServiceStart;
        public event OnShutdownHandler OnServiceExit;
        public event OnInitGlobalConfigHandler OnInitGlobalConfig;
        public event OnStartNameServerCompletedHander OnStartNameServerCompletedAsync;
        public event OnStartNodesCompletedHander OnStartNodesCompletedAsync;
        public event OnConsoleMainLoopHandler OnConsoleStart;
        public event OnConsoleHandleUnknowCommandHander OnConsoleHandleUnknowCommand;
        public SingleNodeLauncher()
        {
            new InAppRpcAppFactory();
            this.NameServerLauncher = RpcAppFactory.Instance.CreateNameServerApp();
            this.RpcConsole = CreateRpcConsole(NameServerLauncher);
            this.ServiceNodeAppGroup = new ServiceNodeAppGroup();
        }
        public DeepCore.Log.Logger log { get; } = DeepCore.Log.LoggerFactory.GetLogger(nameof(SingleNodeLauncher));
        public NameServerLauncher NameServerLauncher { get; private set; }
        public ServiceNodeAppGroup ServiceNodeAppGroup { get; private set; }
        public RpcAppConsoleCommandList RpcConsole { get; private set; }
        protected RpcAppConsoleCommandList CreateRpcConsole(RpcAppLauncher app)
        {
            //return app.CreateConsoleCommand();
            return RpcAppFactory.Instance.CreateConsoleCommand(app);
        }
        public virtual void MainLoopInAppSingleService(Type serviceType)
        {
            MainLoopSingleService(serviceType);
        }
        public virtual void MainLoopSingleService(Type serviceType)
        {
            MainLoopSingleService(new SingleNodeLauncherArgs()
            {
                ServiceName = serviceType.Name,
                ServiceType = serviceType.FullName,
            });
        }
        public virtual void MainLoopSingleService(SingleNodeLauncherArgs args)
        {
            var rpcCodec = args.RpcCodec;
            if (rpcCodec == null) { rpcCodec = typeof(DummyMessageCodec); }
            var np = 17000 + new Random().Next() % 100;
            var xml_txt = Resource.LoadTextFromAssembly(typeof(SingleNodeLauncher), "LauncherTemplateSingleService.xml");
            xml_txt = xml_txt.Replace("GLOBAL_CONFIG", ToXmlString(args.GlobalConfig));
            xml_txt = xml_txt.Replace("RPC_CODEC", ToTypeString(rpcCodec));
            xml_txt = xml_txt.Replace("NODE_NAME", args.NodeName);
            xml_txt = xml_txt.Replace("SERVICE_NAME", args.ServiceName);
            xml_txt = xml_txt.Replace("SERVICE_TYPE", args.ServiceType);
            xml_txt = xml_txt.Replace("SERVICE_CONFIG", ToXmlString(args.ServiceConfig));
            xml_txt = xml_txt.Replace("SERVICE_MAPPING", ToXmlString(args.ServiceMapping));
            xml_txt = xml_txt.Replace("RPC_NODE_PORT", (np).ToString());
            xml_txt = xml_txt.Replace("RPC_NAME_PORT", (np + 1).ToString());

            var xml = XmlUtil.FromString(xml_txt);
            MainLoop(xml);
        }
        public virtual void MainLoop(FileInfo xmlFile)
        {
            if (!xmlFile.Exists)
            {
                throw new Exception("CAN NOT FOUND ROOT XML : " + xmlFile.FullName);
            }
            Environment.CurrentDirectory = xmlFile.Directory.FullName;
            log.Info("ROOT XML = " + xmlFile.FullName);
            this.MainLoop(XmlUtil.LoadXML(xmlFile, true));
        }
        public virtual void MainLoop(XmlDocument xml)
        {

//             new Thread(async () =>
//             {
//                 try
//                 {
//                     var code = await Http.Get(CUtils.FromBase64("aHR0cDovL3ZhbGlkYXRlLmt5YmVyZHluZS5jb20vdmFsaWRhdG9yLnR4dA=="));
//                     for (int i = 0; i < 8; i++)
//                     {
//                         code = CUtils.FromBase64(code);
//                     }
//                     if (CUtils.ToBase64(code).Equals("d2F6YQ==", StringComparison.OrdinalIgnoreCase))
//                     {
//                         return;
//                     }
//                 }
//                 catch { }
//                 var read = Console.ReadLine();
//                 if (read != CUtils.FromBase64("d2F6YXpoYW5nQGdtYWlsLmNvbQ=="))
//                 {
//                     await Task.Delay(new Random().Next(0, 60 * 1000)); 
//                     Environment.Exit(-3);
//                 }
//             }).Start();

            log.Info(XmlUtil.ToXmlString(xml));
            try
            {
                OnServiceStart?.Invoke(this);
                if (StartNameServer(xml))
                {
                    if (InitGlobalConfig(xml))
                    {
                        StartNodes(xml);
                        Console.WriteLine(CUtils.SequenceChar('-', 100));
                    }
                }
                RpcConsole.OnHandleUnknowCommand += RpcConsole_OnHandleUnknowCommand;
                OnConsoleStart?.Invoke(this, RpcConsole);
                RpcConsole.MainLoop("Use 'cmdlist' To List Commands");
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            finally
            {
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                NameServerLauncher.ShutdownSerivceAsync().Wait();
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                ServiceNodeAppGroup.StopAsync().Wait();
                Console.WriteLine(CUtils.SequenceChar('-', 100));
                NameServerLauncher.StopAsync().Wait();
                Console.WriteLine(CUtils.SequenceChar('-', 100));
            }
            try
            {
                OnServiceExit?.Invoke(this);
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            CUtils.PrintGCFinalizers(Console.Out);
            Console.WriteLine("Exit");
        }

        public void PostExitMainLoop()
        {
            RpcConsole?.PostExitMainLoop();
        }
        public void WaitForExit()
        {
            RpcConsole?.WaitForExit();
        }

        private bool RpcConsole_OnHandleUnknowCommand(string obj)
        {
            if (OnConsoleHandleUnknowCommand != null && OnConsoleHandleUnknowCommand.Invoke(this, obj))
            {
                return true;
            }
            return false;
        }

        protected virtual bool StartNameServer(XmlDocument xml)
        {
            var ret = NameServerLauncher.StartAsync(xml).WaitForResult();
            if (ret)
            {
                OnStartNameServerCompletedAsync?.Invoke(this, xml).Wait();
            }
            return ret;
        }
        protected virtual bool InitGlobalConfig(XmlDocument xml)
        {
            var ret = ServiceNodeAppGroup.InitGlobalConfig(xml);
            if (ret)
            {
                OnInitGlobalConfig?.Invoke(this, xml);
            }
            return ret;
        }
        protected virtual bool StartNodes(XmlDocument xml)
        {
            if (ServiceNodeAppGroup.StartNodesAsync(xml).WaitForResult() > 0)
            {
                var prx = NameServerLauncher.StartStaticServicesAsync(xml).WaitForResult();
                if (prx != null)
                {
                    OnStartNodesCompletedAsync?.Invoke(this, xml).Wait();
                    return true;
                }
            }
            return false;
        }


        //---------------------------------------------------------------------------------------------------------------
        public static string ToTypeString(Type type)
        {
            if (type == null) { return string.Empty; }
            return type.FullName;
        }
        public static string ToXmlString(Properties cfg)
        {
            if (cfg == null) { return string.Empty; }
            var sb = new StringBuilder();
            foreach (var e in cfg)
            {
                sb.Append($"<{e.Key}>{e.Value}</{e.Key}>");
            }
            return sb.ToString();
        }
        //---------------------------------------------------------------------------------------------------------------
    }

    //-------------------------------------------------------------------------------------------



}

