using DeepCore;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Launcher;
using Ice;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DeepFrozen.RPC.ICE.Launcher
{
    public abstract class IceRpcServiceBox : IceBox.Service
    {
        public string Name { get; private set; }
        public DeepCore.Log.Logger log { get; private set; }
        public XmlDocument RpcConfig { get; private set; }
        public void start(string name, Ice.Communicator communicator, string[] args)
        {
            try
            {
                this.Name = name;
                this.log = DeepCore.Log.LoggerFactory.GetLogger(name);
                Console.Title = name + " : PID=" + System.Diagnostics.Process.GetCurrentProcess().Id;
                var prop = new DeepCore.Properties(communicator.getProperties().getPropertiesForPrefix(""));
                //---------------------------------------------------------------------------------------------------------
                log.Log(CUtils.SequenceChar('-', 100));
                log.Log("Starting " + name);
                log.Log(CUtils.SequenceChar('-', 100));
                log.Log(prop.ToString());
                log.Log(CUtils.SequenceChar('-', 100));
                //---------------------------------------------------------------------------------------------------------
                var cfgFile = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + prop.Get("rpc_config"));
                Environment.CurrentDirectory = cfgFile.Directory.FullName;
                log.Info("ROOT = " + Environment.CurrentDirectory);
                log.Info("RPC_CONFIG = " + cfgFile.FullName);
                this.RpcConfig = XmlUtil.LoadXML(cfgFile, true);
                this.DoStartAsync(RpcConfig, communicator, args).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        log.Error(t.Exception);
                    }
                });
            }
            catch (System.Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }
        public void stop()
        {
            try
            {
                DoStopAsync().ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        log.Error(t.Exception);
                    }
                }).Wait();
            }
            catch (System.Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        protected abstract Task<bool> DoStartAsync(XmlDocument doc, Ice.Communicator communicator, string[] args);
        protected abstract Task<bool> DoStopAsync();
    }
    //--------------------------------------------------------------------------------------------------------------------------
    public class IceNameServerBox : IceRpcServiceBox
    {
        public IceNameServerLauncher NameServer { get; private set; }
        public bool AutoStartStaticServices { get; set; } = true;
        protected override async Task<bool> DoStartAsync(XmlDocument doc, Communicator communicator, string[] args)
        {
            NameServer = new IceNameServerLauncher(communicator);
            if (await NameServer.StartAsync(doc))
            {
                if (AutoStartStaticServices)
                {
                    log.Info("Waitting For Static Nodes Registered ...");
                    await NameServer.WaitForStaticNodesRegisteredAsync(doc);

                    log.Info("Auto Start Static Services");
                    await NameServer.StartStaticServicesAsync(doc);

                }
                return true;
            }
            return false;
        }

        protected override Task<bool> DoStopAsync()
        {
            return NameServer.StopAsync();
        }
    }
    //--------------------------------------------------------------------------------------------------------------------------
    public class IceServiceNodeBox : IceRpcServiceBox
    {
        public IceServiceNodeLauncher ServiceNode { get; private set; }

        protected override async Task<bool> DoStartAsync(XmlDocument doc, Communicator communicator, string[] args)
        {
            if (RpcAppFactory.TryGetGlobalConfig(doc, out var globalRoot, out var globalMap))
            {
                IService.GlobalConfig = new DeepCore.Properties(globalMap);
                var result = await RpcAppFactory.ForEachServiceNodesAsync(doc, async (xnode, nodeConfig) =>
                {
                    if (nodeConfig.LocalNodeName == this.Name)
                    {
                        ServiceNode = new IceServiceNodeLauncher(communicator);
                        if (await ServiceNode.StartAsync(xnode))
                        {
                            RpcAppFactory.Instance.CreateConsoleCommand(ServiceNode);
                            return true;
                        }
                    }
                    return false;
                });
                return result;
            }
            throw new System.Exception("Can Not Find Service : " + this.Name);
        }

        protected override Task<bool> DoStopAsync()
        {
            return ServiceNode.StopAsync();
        }
    }
    //--------------------------------------------------------------------------------------------------------------------------

}
