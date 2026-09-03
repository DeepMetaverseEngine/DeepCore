using DeepCore;
using DeepCore.GameEvent;
using DeepCore.GameEvent.Lua;
using DeepCore.GameHost.Instance;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Template.MoonSharp;
using DeepCore.Xml;
using DeepCrystal.RPC;
using DeepFrozen.ICE;
using DeepFrozen.ICE.ServiceNode;
using DeepFrozen.RPC.Remote;
using DeepMMO.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ThreeLives.Server.Events;

public class TLServiceNodeBox : RpcServiceBox
{
    private IceServiceNode nodeServer;
    private Ice.ObjectAdapter adapter;

    public override void start(string nodeName, Ice.Communicator communicator, string[] args)
    {
        try
        {
            ReflectionUtil.LoadDlls(
                AppDomain.CurrentDomain, new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory),
                (domain, file) => System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(file.FullName),
                (domain, file, asmName) => System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName),
                0);
            base.start(nodeName, communicator, args);
            var nodeConfig = Properties.LoadFromXML(base.RpcConfig["ServiceNodes"][nodeName]);
            var nodeRpcConfig = Properties.LoadFromXML(base.RpcConfig["ServiceNodes"][nodeName]["RpcConfig"]);
            IService.GlobalConfig = Properties.LoadFromXML(base.RpcConfig["GlobalConfig"]);
            ThreeLives.Server.ThirdParty.FactoryManager.Init(false);
            IceServiceNode.DEFAULT_TASK_EXECUTE_TIMEOUT_MS = nodeRpcConfig.GetAsInt("DefaultTaskExecuteTimeout");
            IceServiceNode.REQUEST_TICK_TIME_MS = nodeRpcConfig.GetAsInt("RequestTickTimeMS");
            IceServiceNode.NETWORK_TIMEOUT_MS = nodeRpcConfig.GetAsInt("NetworkTimeoutMS");
            var typeMappings = nodeRpcConfig.SubProperties("AcceptTypeMappings.");
            nodeServer = new TLIceServiceNode(communicator, nodeName, nodeName, "NameServer", new TLServer.ServerCodec(), typeMappings);
            adapter = communicator.createObjectAdapter(nodeName);
            adapter.add(nodeServer.CreateServiceNodeI(), Ice.Util.stringToIdentity(nodeName));
            adapter.activate();
            nodeServer.StartAsync().Wait();
            ThreeLives.Server.ThirdParty.FactoryManager.InitOver();
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
            nodeServer.ShutdownAsync().Wait();
            nodeServer.StopAsync().Wait();
            adapter.deactivate();
            BILogger.BICustomLoggerFactory.Dispose();
        }
        catch (System.Exception err)
        {
            log.Error(err.Message, err);
            throw;
        }
    }

    class TLIceServiceNode : IceServiceNode
    {
        public TLIceServiceNode(Ice.Communicator com, string nodeName, string nodeEndPoint, string nameServerEndPoint, IExternalizableFactory rpcCodec, Dictionary<string, string> acceptTypeMappings)
            : base(com, nodeName, nodeEndPoint, nameServerEndPoint, rpcCodec, acceptTypeMappings)
        {
        }
        protected override void OnUpdateStatusTick(object state)
        {
            using (var sb = StringBuilderObjectPool.AllocAutoRelease())
            {
                this.Adapter.UpdateNodeState(new ServiceNodeStateInfo()
                {
                    NodeName = base.NodeName,
                    ServiceCount = base.ServiceCount,
                    MemoryUse = GC.GetTotalMemory(false),
                    MemoryTotal = Environment.WorkingSet,
                    CpuPercent = 0,
                    Info = sb.ToString(),
                });
            }
        }
    }
}


