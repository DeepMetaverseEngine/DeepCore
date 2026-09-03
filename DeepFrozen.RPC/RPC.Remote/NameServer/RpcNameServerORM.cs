using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Log;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.ORM.Utils;
using DeepCrystal.RPC;
using DeepCrystal.Threading.Dataflow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.NameServer
{
    //-------------------------------------------------------------------------------------------------------------------
    public class RpcNameServerORM : IRpcNameServerHandler
    {
        private ActionBlockExecutor actionBlock;
        private IOStreamPool codec_rpc;
        private IRpcApplication app;
        private IMappingAdapter db;
       
//         private readonly ConcurrentDictionary<string, NodeInfo> nodes = new ConcurrentDictionary<string, NodeInfo>();
//         private readonly ConcurrentDictionary<string, ServiceInfo> services = new ConcurrentDictionary<string, ServiceInfo>();
//         private readonly ConcurrentDictionary<string, ServiceInfo> staticServices = new ConcurrentDictionary<string, ServiceInfo>();

        private IRpcNameServerAdapter adapter;
        private bool isReady = false;

        public Logger log { get; private set; }
        public IRpcNameServerAdapter Adapter
        {
            get => adapter;
        }

        //----------------------------------------------------------------------------------------------------------------------
        protected virtual void Init(RpcNameConfig cfg, IRpcNameServerAdapter adapter)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.actionBlock = new ActionBlockExecutor();
            this.codec_rpc = new IOStreamPool(cfg.RpcCodec);
            this.db = ORMFactory.Instance.DefaultAdapter;
            this.adapter = adapter;
            this.adapter.Attach(this);
        }

        //----------------------------------------------------------------------------------------------------------------------
        public virtual Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task StopAsync()
        {
            this.actionBlock.Complete();
            return this.actionBlock.Completion;
        }

        /// <summary>
        /// 关闭所有服务
        /// </summary>
        /// <returns></returns>
        public virtual Task ShutdownSerivceAsync()
        {
            //var list = new List<ServiceInfo>(services.Values);
            services.GetAll();
            list.Sort((a, b) => -(a.StartTimeUTC.CompareTo(b.StartTimeUTC)));
            return Task.Run(async () =>
            {
                foreach (var svc in list)
                {
                    try
                    {
                        if (services.ContainsKey(svc.ServiceName))
                        {
                            RemoteAddress addr = svc.Address;
                            await actionBlock.RunAsync(main_OnHandleDestoryRemoteServiceAsync, RemoteAddress.NULL, addr, "name server shutdown");
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            });
        }

        /// <summary>
        /// 向所有服务广播消息
        /// </summary>
        /// <param name="notify"></param>
        public virtual void BroadcastSystemMessage(ISerializable notify)
        {
            var bin = codec_rpc.ToBinary(notify);
            adapter.n2s_AppBroadcastMessage(bin);
        }

        /// <summary>
        /// 向所有服务广播消息
        /// </summary>
        /// <param name="notify"></param>
        public virtual Task<string> BroadcastCommandAsync(string cmd)
        {
            return adapter.n2s_AppBroadcastCommandAsync(cmd);
        }

        //----------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 创建服务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public Task<RemoteProxyInfo> AddStaticServiceAsync(RemoteAddress addr, Properties config)
        {
            return actionBlock.RunAsync(async () =>
            {
                var svc = await main_OnHandleGetRemoteServiceAsync(GetServiceOperation.Create, RemoteAddress.NULL, addr, config, true);
                if (svc != null)
                {
                    return svc.Info;
                }
                return null;
            });
        }

        /// <summary>
        /// 创建服务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public Task<RemoteProxyInfo> AddGroupServiceAsync(RemoteAddress addr, Properties config)
        {
            return actionBlock.RunAsync(async () =>
            {
                var svc = await main_OnHandleGetRemoteServiceAsync(GetServiceOperation.Create, RemoteAddress.NULL, addr, config, false);
                if (svc != null)
                {
                    return svc.Info;
                }
                return null;
            });
        }

        /// <summary>
        /// 设置服务工作状态，静态服务全部初始化后调用
        /// </summary>
        /// <returns></returns>
        public Task<bool> SetStaticReadyAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                if (isReady) return false;
                else isReady = true;
                return true;
            });
        }

        //----------------------------------------------------------------------------------------------------------------------
        Task<NodeInfo> IRpcNameServerHandler.s2n_HandleRegistNodeAsync(ServiceNodeStartInfo start)
        {
            return actionBlock.RunAsync(main_OnHandleRegistNodeAsync, start);
        }

        Task<NodeInfo> IRpcNameServerHandler.s2n_HandleUnregistNodeAsync(string nodeName)
        {
            return actionBlock.RunAsync(main_OnHandleUnregistNodeAsync, nodeName);
        }

        void IRpcNameServerHandler.s2n_HandleUpdateNodeState(ServiceNodeStateInfo state)
        {
            actionBlock.Post(main_OnHandleUpdateNodeState, state);
        }

        Task<ServiceInfo> IRpcNameServerHandler.s2n_HandleGetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config)
        {
            return actionBlock.RunAsync(() =>
            {
                return main_OnHandleGetRemoteServiceAsync(op, from, path, config, false);
            });
        }

        Task<ServiceInfo> IRpcNameServerHandler.s2n_HandleDestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            return actionBlock.RunAsync(() => { return main_OnHandleDestoryRemoteServiceAsync(from, path, reason); });
        }

        //----------------------------------------------------------------------------------------------------------------------
        Task<int> IRpcNameServerHandler.s2n_HandleGetServiceCountAsync(string serviceNode, string serviceType)
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetServiceCountAsync(serviceNode, serviceType); });
        }

        Task<ServiceInfo[]> IRpcNameServerHandler.s2n_HandleGetRemoteServicesAsync(ICollection<string> paths)
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetRemoteServicesAsync(paths); });
        }

        Task<ServiceInfo[]> IRpcNameServerHandler.s2n_HandleGetRemoteServicesWithAddressPatternAsync(string pattern)
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetRemoteServicesWithPatternAsync(pattern); });
        }

        Task<ServiceInfo[]> IRpcNameServerHandler.s2n_HandleGetRemoteServicesWithInfoLinqAsync(string where, string orderBy)
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetRemoteServicesWithLinqAsync(where, orderBy); });
        }

        Task<ServiceInfo[]> IRpcNameServerHandler.s2n_HandleGetStaticServicesAsync()
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetStaticServicesAsync(); });
        }

        Task<NodeInfo[]> IRpcNameServerHandler.s2n_HandleGetStaticNodesAsync()
        {
            return actionBlock.RunAsync(() => { return main_OnHandleGetStaticNodesAsync(); });
        }

        //----------------------------------------------------------------------------------------------------------------------
        void IRpcNameServerHandler.s2n_HandleServiceBroadcastMessage(RemoteAddress from, BinaryMessage notify)
        {
            actionBlock.Post(main_OnHandleBroadcast, from, notify);
        }

        void IRpcNameServerHandler.s2n_HandleAppBroadcastMessage(BinaryMessage notify)
        {
            actionBlock.Post(main_OnHandleBroadcastApp, notify);
        }

        Task<string> IRpcNameServerHandler.s2n_HandleAppBroadcastCommandAsync(string notify)
        {
            return actionBlock.RunAsync(main_OnHandleAppCommandAsync, notify);
        }

        //----------------------------------------------------------------------------------------------------------------------

        private Action<string> event_OnNodeRegistered;
        private Action<string> event_OnNodeUnregistered;
        private Action<RemoteAddress> event_OnServiceStarted;
        private Action<RemoteAddress> event_OnServiceClosed;

        public event Action<string> OnNodeRegistered { add { event_OnNodeRegistered += value; } remove { event_OnNodeRegistered -= value; } }
        public event Action<string> OnNodeUnregistered { add { event_OnNodeUnregistered += value; } remove { event_OnNodeUnregistered -= value; } }
        public event Action<RemoteAddress> OnServiceStarted { add { event_OnServiceStarted += value; } remove { event_OnServiceStarted -= value; } }
        public event Action<RemoteAddress> OnServiceClosed { add { event_OnServiceClosed += value; } remove { event_OnServiceClosed -= value; } }

        #region NameServer <-> Node

        private NodeInfo main_OnHandleRegistNodeAsync(ServiceNodeStartInfo req)
        {
            try
            {
                var node = this.CreateNodeInfo(req);
                if (nodes.TryAdd(req.NodeName, node))
                {
                    event_OnNodeRegistered?.Invoke(node.NodeName);
                    return node;
                }
                else
                {
                    throw new Exception("Service Node Already Exist : " + req.NodeName);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private NodeInfo main_OnHandleUnregistNodeAsync(string nodeName)
        {
            try
            {
                if (nodes.TryRemove(nodeName, out var node))
                {
                    event_OnNodeUnregistered?.Invoke(node.NodeName);
                    return node;
                }
                else
                {
                    throw new Exception("Service Node Not Exist : " + nodeName);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private void main_OnHandleUpdateNodeState(ServiceNodeStateInfo st)
        {
            if (nodes.TryGetValue(st.NodeName, out var state))
            {
                state.UpdateState(st);
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------------

        #region NameServer <-> Services

        private async Task<ServiceInfo> main_OnHandleGetRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config, bool isStatic)
        {
            try
            {
                ServiceInfo svc = null;
                bool isCreate = false;
                switch (op)
                {
                    case GetServiceOperation.GetOrCreate:
                        svc = services.GetOrAdd(path.ServiceName, (name) =>
                        {
                            var ret = CreateServiceInfo(DispatchNode(ref path), from, path, config, isStatic);
                            isCreate = true;
                            return ret;
                        });
                        break;
                    case GetServiceOperation.Create:
                        svc = CreateServiceInfo(DispatchNode(ref path), from, path, config, isStatic);
                        if (services.TryAdd(path.ServiceName, svc))
                        {
                            isCreate = true;
                        }
                        else
                        {
                            throw new Exception("Service Already Exist : " + path.ServiceName);
                        }
                        break;
                    case GetServiceOperation.Get:
                        if (services.TryGetValue(path.ServiceName, out svc) == false)
                        {
                            //throw new Exception("Service Not Exist : " + path.ServiceName);
                            return null;
                        }
                        break;
                }

                if (svc == null)
                {
                    throw new Exception($"Service Not Exist :{path}  From={from}");
                    //throw new Exception("Service Not Exist : " + path);
                }
                else if (isCreate)
                {
                    if (svc.IsStatic)
                    {
                        staticServices.TryAdd(path.ServiceName, svc);
                    }
                    try
                    {
                        if (await svc.WaitForStarted())
                        {
                            event_OnServiceStarted?.Invoke(svc.Address);
                            return svc;
                        }
                        else
                        {
                            throw new Exception("Service Create Error : " + path);
                        }
                    }
                    catch
                    {
                        services.TryRemove(svc.ServiceName, out svc);
                        if (svc.IsStatic)
                        {
                            staticServices.TryRemove(path.ServiceName, out svc);
                        }
                        throw;
                    }
                }
                else
                {
                    return svc;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return await Task.FromException<ServiceInfo>(err);
            }
        }

        private async Task<ServiceInfo> main_OnHandleDestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            try
            {
                if (services.TryRemove(path.ServiceName, out var svc) == false)
                {
                    throw new Exception("Destory Remote Service Not Exist : " + path.ServiceName);
                }
                if (svc.IsStatic)
                {
                    staticServices.TryRemove(svc.Address.ServiceName, out svc);
                }
                if (svc.Status == ServiceStatus.Stopped || svc.Status == ServiceStatus.Stopping)
                {
                    throw new Exception("Service Already Destroyed : " + path.ServiceName);
                }
                else
                {
                    this.adapter.n2s_BroadcastRemoteDisposing(svc.Address);
                    var rst = await svc.WaitForStopped(from, reason);
                    if (rst)
                    {
                        event_OnServiceClosed?.Invoke(svc.Address);
                        this.adapter.n2s_BroadcastRemoteDestoryed(svc.Address);
                    }
                    return svc;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return await Task.FromException<ServiceInfo>(err);
            }
        }

        private Task<int> main_OnHandleGetServiceCountAsync(string serviceNode, string serviceType)
        {
            try
            {
                var in_services = new List<ServiceInfo>(services.Values);
                {
                    if (serviceNode != null && serviceType != null)
                    {
                        return Task.FromResult(in_services.Sum(e => (e.Address.ServiceNode == serviceNode && e.Address.ServiceType == serviceType) ? 1 : 0));
                    }
                    if (serviceNode != null)
                    {
                        return Task.FromResult(in_services.Sum(e => (e.Address.ServiceNode == serviceNode) ? 1 : 0));
                    }
                    if (serviceType != null)
                    {
                        return Task.FromResult(in_services.Sum(e => (e.Address.ServiceType == serviceType) ? 1 : 0));
                    }
                }
                return Task.FromResult(0);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<int>(err);
            }
        }

        private Task<ServiceInfo[]> main_OnHandleGetRemoteServicesAsync(ICollection<string> paths)
        {
            try
            {
                var out_services = new List<ServiceInfo>();
                {
                    foreach (var path in paths)
                    {
                        if (services.TryGetValue(path, out var svc))
                        {
                            out_services.Add(svc);
                        }
                    }
                    return Task.FromResult(out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<ServiceInfo[]>(err);
            }
        }

        private Task<ServiceInfo[]> main_OnHandleGetRemoteServicesWithPatternAsync(string pattern)
        {
            try
            {
                Regex regex;
                try
                {
                    regex = new Regex(pattern);
                }
                catch
                {
                    return Task.FromResult(new ServiceInfo[0]);
                }
                var out_services = new List<ServiceInfo>(); 
                var in_services = new List<ServiceInfo>(services.Values);
                {
                    foreach (var svc in in_services)
                    {
                        if (regex.IsMatch(svc.Address.ToString()))
                        {
                            out_services.Add(svc);
                        }
                    }

                    return Task.FromResult(out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<ServiceInfo[]>(err);
            }
        }

        private Task<ServiceInfo[]> main_OnHandleGetRemoteServicesWithLinqAsync(string where, string orderBy)
        {
            try
            {
                var out_services = new List<ServiceInfo>();
                var in_services = new List<ServiceInfo>(services.Values);
                {
                    var queryableData = in_services.AsQueryable<IRemoteServiceInfo>();
                    var ret = System.Linq.Dynamic.DynamicQueryable.Where(queryableData, where);
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        ret = ret.OrderBy(orderBy);
                    }

                    return Task.FromResult(Array.ConvertAll(ret.ToArray(), (a) => a as ServiceInfo));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<ServiceInfo[]>(err);
            }
        }

        private Task<ServiceInfo[]> main_OnHandleGetStaticServicesAsync()
        {
            try
            {
                if (isReady == false) throw new Exception("Static Services Not Ready !");
                var out_services = new List<ServiceInfo>();
                var in_services = new List<ServiceInfo>(staticServices.Values);
                {
                    foreach (var svc in in_services)
                    {
                        out_services.Add(svc);
                    }
                    return Task.FromResult(out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<ServiceInfo[]>(err);
            }
        }

        private Task<NodeInfo[]> main_OnHandleGetStaticNodesAsync()
        {
            try
            {
                var out_services = new List<NodeInfo>();
                {
                    out_services.AddRange(nodes.Values);
                    return Task.FromResult(out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return Task.FromException<NodeInfo[]>(err);
            }
        }

        private void main_OnHandleBroadcast(RemoteAddress from, BinaryMessage notify)
        {
            adapter.n2s_AppBroadcastMessage(notify);
        }

        private void main_OnHandleBroadcastApp(BinaryMessage notify)
        {
            adapter.n2s_AppBroadcastMessage(notify);
        }

        private Task<string> main_OnHandleAppCommandAsync(string notify)
        {
            return adapter.n2s_AppBroadcastCommandAsync(notify);
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------------
        public List<NodeInfo> GetAllNodes()
        {
            return new List<NodeInfo>(nodes.Values);
        }
        public int GetAllNodesCount()
        {
            return nodes.Count;
        }

        public List<ServiceInfo> GetAllServices()
        {
            return new List<ServiceInfo>(services.Values);
        }
        public int GetAllServicesCount()
        {
            return services.Count;
        }

        public void ForEachNodes<T>(Action<T> action) where T : NodeInfo
        {
            var list = new List<NodeInfo>(nodes.Values);
            {
                foreach (var node in list)
                {
                    action(node as T);
                }
            }
        }

        public void ForEachServices<T>(Action<T> action) where T : ServiceInfo
        {
            var list = new List<ServiceInfo>(services.Values);
            {
                foreach (var svc in list)
                {
                    action(svc as T);
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------
        private NodeInfo DispatchNode(ref RemoteAddress path)
        {
            if (path.ServiceNode != null)
            {
                if (nodes.TryGetValue(path.ServiceNode, out var node))
                {
                    if (node.AcceptType(path.ServiceType))
                    {
                        return node;
                    }
                }
            }
            var list = new List<NodeInfo>();
            {
                foreach (var e in GetAllNodes())
                {
                    if (e.AcceptType(path.ServiceType))
                    {
                        list.Add(e);
                    }
                }
                if (list.Count > 0)
                {
                    var ret = DispatchNode(list);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            throw new Exception($"Can Not Dispatch Service Node For : {path} : ServiceType Not Acceptable!!!");
        }

        protected virtual NodeInfo DispatchNode(List<NodeInfo> list)
        {
            list.Sort((a, b) => { return (a.ServiceCount - b.ServiceCount); });
            return list[0];
        }

        protected virtual NodeInfo CreateNodeInfo(ServiceNodeStartInfo info)
        {
            return new NodeInfo(this, info);
        }

        protected virtual ServiceInfo CreateServiceInfo(NodeInfo node, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config, bool isStatic)
        {
            return new ServiceInfo(node, from, path, config, isStatic);
        }
        //----------------------------------------------------------------------------------------------------------------------

        public virtual Task<string> GetNodeStatusAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                var sb = new StringWriter();
                {
                    {
                        var list = nodes.Values.ToArray();
                        Array.Sort(list, (a, b) => (a.NodeName.CompareTo(b.NodeName)));
                        foreach (var e in list)
                        {
                            sb.WriteLine(CUtils.SequenceChar('#', 100));
                            sb.WriteLine("####" + CUtils.FillPlaceHolder(e.NodeName, 100 - 8, ' ', 2) + "####");
                            sb.WriteLine(CUtils.SequenceChar('#', 100));
                            e.GetStatus(sb);
                        }
                    }
                    sb.WriteLine(CUtils.SequenceChar('#', 100));
                    {
                        var list = services.Values.ToArray();
                        Array.Sort(list, (a, b) => (a.Address.ServiceType.CompareTo(b.Address.ServiceType)));
                        var servicesTypes = new HashMap<string, AtomicInteger>();
                        foreach (var e in list)
                        {
                            servicesTypes.GetOrAdd(e.Address.ServiceType, static (t) => new AtomicInteger(0)).IncrementAndGet();
                        }
                        foreach (var e in new SortedDictionary<string, AtomicInteger>(servicesTypes))
                        {
                            sb.WriteLine(CUtils.FillPlaceHolder(e.Key, 50, ' ', 1) + " = " + e.Value);
                        }
                    }
                    sb.WriteLine(CUtils.SequenceChar('#', 100));
                    sb.WriteLine("####" + CUtils.FillPlaceHolder("Total NodeCount = " + nodes.Count, 100 - 8, ' ', 2) + "####");
                    sb.WriteLine("####" + CUtils.FillPlaceHolder("Total ServiceCount = " + services.Count, 100 - 8, ' ', 2) + "####");
                    sb.WriteLine(CUtils.SequenceChar('#', 100));
                    return sb.ToString();
                }
            });
        }

        public class NodeInfo
        {
            private readonly RpcNameServer nameServer;
            private readonly ServiceNodeStartInfo token;
            private ServiceNodeStateInfo state;
            private int serviceCount = 0;

            public RpcNameServer NameServer { get => nameServer; }
            public string NodeName { get => token.NodeName; }
            public string EndPoint { get => token.EndPoint; }
            public int ServiceCount { get { return serviceCount; } }
            public ServiceNodeStartInfo Token
            {
                get
                {
                    var ret = new ServiceNodeStartInfo();
                    ret.AcceptServiceType = new List<string>(token.AcceptServiceType);
                    ret.EndPoint = token.EndPoint;
                    ret.NodeName = token.NodeName;
                    return ret;
                }
            }

            public NodeInfo(RpcNameServer ns, ServiceNodeStartInfo req)
            {
                this.nameServer = ns;
                this.token = req;
            }
            public ServiceNodeStateInfo StateInfo
            {
                get => state;
            }

            public override string ToString()
            {
                return token.NodeName;
            }
            internal void UpdateState(ServiceNodeStateInfo state)
            {
                this.state = state;
            }

            internal void AddService()
            {
                serviceCount++;
            }

            internal void RemoveService()
            {
                serviceCount--;
            }

            public bool AcceptType(string serviceType)
            {
                return token.AcceptServiceType.Contains(serviceType);
            }

            public virtual void GetStatus(TextWriter output)
            {
                output.WriteLine("                  NodeName = " + NodeName);
                output.WriteLine("                  EndPoint = " + EndPoint);
                output.WriteLine("              ServiceCount = " + ServiceCount);
                output.WriteLine("         AcceptServiceType = " + CUtils.ListToString(token.AcceptServiceType, " "));
                if (state == null) return;
                output.WriteLine("                CpuPercent = " + state.CpuPercent);
                output.WriteLine("                 MemoryUse = " + CUtils.ToBytesSizeString(state.MemoryUse));
                output.WriteLine("               MemoryTotal = " + CUtils.ToBytesSizeString(state.MemoryTotal));
                output.WriteLine("           ServiceBoxCount = " + state.ServiceCount);
                output.Write(state.Info);
            }
        }

        public class ServiceInfo : IRemoteServiceInfo
        {
            private readonly RemoteAddress creater;
            private readonly RemoteProxyInfo info;
            private readonly NodeInfo node;
            private readonly bool isStatic;
            private ServiceStatus status;

            public NodeInfo Node { get => node; }
            public string ServiceName { get => info.Address.ServiceName; }
            public RpcNameServer NameServer { get => node.NameServer; }
            public RemoteAddress Address { get => info.Address; }
            public RemoteProxyInfo Info { get => info.Clone(); }
            public DateTime StartTimeUTC { get => info.StartTimeUTC; }
            public ServiceStatus Status { get => status; }
            public bool IsStatic { get => isStatic; }
            string IRemoteServiceInfo.ServiceName => info.Address.ServiceName;

            RemoteAddress IRemoteServiceInfo.Address => info.Address;
            Properties IRemoteServiceInfo.Config => info.Config;
            DateTime IRemoteServiceInfo.StartTimeUTC => info.StartTimeUTC;
            bool IRemoteServiceInfo.IsStatic => isStatic;

            public ServiceInfo(NodeInfo node, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config, bool isStatic)
            {
                this.node = node;
                this.creater = from;
                this.isStatic = isStatic;
                this.info = new RemoteProxyInfo()
                {
                    Address = new RemoteAddress(path.ServiceName, this.node.NodeName, path.ServiceType),
                    Config = new Properties(config),
                    EndPoint = node.EndPoint,
                    IsStatic = isStatic,
                };
                this.status = ServiceStatus.NA;
            }

            public override string ToString()
            {
                return info.Address.ToString();
            }

            internal async Task<bool> WaitForStarted()
            {
                if (this.status == ServiceStatus.NA)
                {
                    this.node.AddService();
                    try
                    {
                        this.status = ServiceStatus.Starting;
                        this.info.StartTimeUTC = DateTime.UtcNow;
                        return await NameServer.adapter.n2s_DispatchCreateServiceAsync(this.creater, this, info.Config, isStatic);
                    }
                    finally
                    {
                        this.status = ServiceStatus.Started;
                    }
                }

                return false;
            }

            internal async Task<bool> WaitForStopped(RemoteAddress from, string reason)
            {
                // wait for status is started
                // 防止一个SERVICE启动后立刻被销毁，等待启动结束
                while (this.status == ServiceStatus.Starting)
                {
                    await Task.Delay(100);
                }

                if (this.status == ServiceStatus.Started)
                {
                    this.node.RemoveService();
                    try
                    {
                        this.status = ServiceStatus.Stopping;
                        var rst = await NameServer.adapter.n2s_DispatchDestoryServiceAsync(from, this, reason);
                        return rst;
                    }
                    finally
                    {
                        this.status = ServiceStatus.Stopped;
                    }
                }

                return false;
            }
        }

        public enum ServiceStatus
        {
            NA,
            Starting,
            Started,
            Stopping,
            Stopped,
        }
    }
}