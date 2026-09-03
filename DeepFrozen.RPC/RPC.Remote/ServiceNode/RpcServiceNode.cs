using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepCrystal.RPC;
using DeepCrystal.Threading;
using DeepCrystal.Threading.Timer;
using DeepFrozen.RPC.Invoker;
using DeepFrozen.Schedule;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static DeepFrozen.RPC.Remote.ServiceNode.IRpcServiceNodeAdapter;

namespace DeepFrozen.RPC.Remote.ServiceNode
{
    public struct RpcNodeConfig
    {
        public string LocalNodeName;
        public string LocalEndPoint;
        public string NameServerEndPoint;
        public int RequestTickTimeMS;
        public int NetworkTimeoutMS;
        public int DefaultTaskExecuteTimeout;
        public IExternalizableFactory RpcCodec;
        public Properties AcceptTypeMappings;
        public TaskScheduler TaskScheduler;
    }
    public struct RpcStartService
    {
        public RemoteAddress Address;
        public Properties Config;
        public bool IsStatic;
    }

    /// <summary>
    /// 服务节点，管理多个Service
    /// </summary>
    public class RpcServiceNode : Disposable
    {
        public static int TASK_COMPLETE_SOURCE_POOL_TICK_MS = 1000;
        public static int DEFAULT_TASK_EXECUTE_TIMEOUT_MS = 10000;
        public static int NETWORK_TIMEOUT_MS = 30000;
        public static int REQUEST_TICK_TIME_MS = 3000;
        public static int UPDATE_NODE_STATUS_TICK_MS = 3000;
        public static int TIMER_SENSITIVITY = 1000;

        private static void CheckStatic()
        {
            CheckStatic(nameof(REQUEST_TICK_TIME_MS), REQUEST_TICK_TIME_MS, 1000);
            CheckStatic(nameof(DEFAULT_TASK_EXECUTE_TIMEOUT_MS), DEFAULT_TASK_EXECUTE_TIMEOUT_MS, 10000);
            CheckStatic(nameof(TASK_COMPLETE_SOURCE_POOL_TICK_MS), TASK_COMPLETE_SOURCE_POOL_TICK_MS, 1000);
            CheckStatic(nameof(NETWORK_TIMEOUT_MS), NETWORK_TIMEOUT_MS, 3000);
            CheckStatic(nameof(UPDATE_NODE_STATUS_TICK_MS), UPDATE_NODE_STATUS_TICK_MS, 3000);
            CheckStatic(nameof(TIMER_SENSITIVITY), TIMER_SENSITIVITY, 500);
        }
        private static void CheckStatic(string key, int currentValue, int minValue)
        {
            if (currentValue < minValue)
                throw new Exception("\"" + key + "\" Min is " + minValue + ", current value is " + currentValue);
        }
        //-------------------------------------------------------------------------------------------------------------------
        private SharedMemory sharedMemory;
        private string localEndPoint;
        private IRpcApplication app;
        private string nodeName;
        private IOStreamPool rpcCodec;
        private RpcInvokerManager invokeManager;
        private DeepTimerGroup timers;
        private IRpcServiceNodeAdapter adapter;
        private TaskScheduler taskScheduler;
        private IRemoteServiceInfo[] staticServicesCache = null;
        private ConcurrentDictionary<string, IRemoteServiceInfo> staticServicesCacheMap = new ConcurrentDictionary<string, IRemoteServiceInfo>();
        private ConcurrentDictionary<string, RemoteProxyInfo> remoteServicesCacheMap = new ConcurrentDictionary<string, RemoteProxyInfo>();
        private ConcurrentDictionary<string, IRemoteNodeInfo> staticNodesCacheMap = new ConcurrentDictionary<string, IRemoteNodeInfo>();
        private IRemoteNodeInfo[] staticNodesCache = null;
        private TaskCompletionSourcePool taskCompletionSourcePool;
        private readonly HashMap<string, Type> typeMapping = new HashMap<string, Type>();
        private TimeSpan taskExecuteTimeout;
        //-------------------------------------------------------------------------------------------------------------------
        private readonly LocalServiceMap localServices = new LocalServiceMap();
        //-------------------------------------------------------------------------------------------------------------------
        public string NodeName { get => this.nodeName; }
        public string LocalEndPoint { get => this.localEndPoint; }
        public IOStreamPool RpcCodec { get => this.rpcCodec; }
        public RpcInvokerManager InvokeManager { get => this.invokeManager; }
        public TaskScheduler TaskScheduler { get => this.taskScheduler; }
        public int ServiceCount { get => localServices.Count; }
        public LocalServiceMap LocalServices { get => localServices; }
        public TimeSpan TaskExecuteTimeout { get => taskExecuteTimeout; }
        public int TaskExecuteTimeoutMS { get => (int)taskExecuteTimeout.TotalMilliseconds; }
        public DeepTimerGroup RpcTimers { get => timers; }
        public IRpcServiceNodeAdapter Adapter { get => adapter; }
        public IRpcApplication Application { get => app; }
        public ISharedMemory SharedMemory => sharedMemory;
        public Logger log { get; private set; }
        public ServiceNodeStartInfo NodeInfo
        {
            get
            {
                var info = new ServiceNodeStartInfo();
                info.NodeName = this.NodeName;
                info.AcceptServiceType = new List<string>();
                info.EndPoint = localEndPoint;
                foreach (var type in typeMapping.Keys)
                {
                    info.AcceptServiceType.Add(type);
                }
                return info;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------
        public RpcServiceNode(RpcNodeConfig cfg, IRpcServiceNodeAdapter proxy)
        {
            this.nodeName = cfg.LocalNodeName;
            this.log = LoggerFactory.GetLogger(nodeName);
            {
                RpcServiceNode.REQUEST_TICK_TIME_MS = cfg.RequestTickTimeMS;
                RpcServiceNode.NETWORK_TIMEOUT_MS = cfg.NetworkTimeoutMS;
                RpcServiceNode.DEFAULT_TASK_EXECUTE_TIMEOUT_MS = cfg.DefaultTaskExecuteTimeout;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
                if (QuartzScheduleFactory.Factory == null)
                {
                    new QuartzScheduleFactory(TimeZoneInfo.Local);
                }
            }
            CheckStatic();
            this.localEndPoint = cfg.LocalEndPoint;
            this.rpcCodec = new IOStreamPool(cfg.RpcCodec);
            this.taskCompletionSourcePool = new TimerTaskCompletionSourcePool(nameof(RpcServiceNode), CollectionPool.Shared, TASK_COMPLETE_SOURCE_POOL_TICK_MS);
            this.invokeManager = new RpcInvokerManager(rpcCodec);
            this.timers = new DeepTimerGroup(TIMER_SENSITIVITY);
            this.taskExecuteTimeout = TimeSpan.FromMilliseconds(DEFAULT_TASK_EXECUTE_TIMEOUT_MS);
            this.adapter = proxy;
            this.taskScheduler = cfg.TaskScheduler;
            this.adapter.Attach(this);
            {
                this.adapter.n2s_HandleCreateLocalServiceAsync += this.n2s_HandleCreateLocalServiceAsync;
                this.adapter.n2s_HandleDestoryLocalServiceAsync += this.n2s_HandleDestoryLocalServiceAsync;
                this.adapter.n2s_HandleRemoteDisposing += this.n2s_HandleRemoteDisposing;
                this.adapter.n2s_HandleRemoteDestoryed += this.n2s_HandleRemoteDestoryed;
                this.adapter.n2s_HandleAppMessage += this.n2s_HandleAppMessage;
                this.adapter.n2s_HandleAppCommandAsync += this.n2s_HandleAppCommandAsync;
                this.adapter.r2s_HandleRemoteRpcRequest += this.r2s_HandleRemoteRpcRequest;
                this.adapter.r2s_HandleRemoteRpcNotify += this.r2s_HandleRemoteRpcNotify;
                this.adapter.r2s_HandleRemoteRpcBatchNotify += this.r2s_HandleRemoteRpcBatchNotify;
                this.adapter.r2s_HandleRemoteRpcNotifyWithType += this.r2s_HandleRemoteRpcNotifyWithType;
                this.adapter.r2s_HandleRemoteRpcWormhole += this.r2s_HandleRemoteRpcWormhole;
                this.adapter.r2s_HandleRemoteRpcWormholeAsync += this.r2s_HandleRemoteRpcWormholeAsync;
                this.adapter.r2s_HandleRemoteRpcWormholeWithType += this.r2s_HandleRemoteRpcWormholeWithType;
            }
            this.sharedMemory = new SharedMemory(this);
            this.sharedMemory.SetSyncPeriod(TimeSpan.FromSeconds(5));
            RpcApplication.Instance.Bind(this);
            this.app = RpcApplication.Instance;
            if (cfg.AcceptTypeMappings != null && cfg.AcceptTypeMappings.Count > 0)
            {
                foreach (var mapping in cfg.AcceptTypeMappings)
                {
                    AddTypeMapping(mapping.Key, ReflectionUtil.GetType(mapping.Value));
                }
            }
            else
            {
                var alltype = ReflectionUtil.GetNoneVirtualSubTypes(typeof(IService));
                foreach (var type in alltype)
                {
                    AddTypeMapping(type.FullName, type);
                }
            }
        }
        protected override void Disposing()
        {
            event_HandleRemoteDestoryed.Clear();
        }
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.Error.WriteLine("TaskScheduler_UnobservedTaskException");
            log.Error(sender, e.Exception);
        }
        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine("AppDomain_UnhandledException");
            if (e.ExceptionObject is Exception err)
            {
                log.Error(sender, err);
            }
            else
            {
                log.Error(e.ToString());
            }
        }

        public virtual async Task<bool> StartAsync()
        {
            if (await adapter.StartAsync(this))
            {
                if (await adapter.node2name_RegistNodeAsync(this.NodeInfo))
                {
                    this.timers.CreateTimer(
                        TimeSpan.FromMilliseconds(UPDATE_NODE_STATUS_TICK_MS),
                        TimeSpan.FromMilliseconds(UPDATE_NODE_STATUS_TICK_MS),
                        false,
                        this,
                        OnUpdateStatusTick);
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 关闭所有服务
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> ShutdownAsync()
        {
            try
            {
                var ret = 0;
                var closing = new List<RpcServiceBox>();
                {
                    localServices.GetAllLocalServices(closing);
                    closing.Sort((a, b) => -(a.StartTimeUTC.CompareTo(b.StartTimeUTC)));
                    foreach (var svc in closing)
                    {
                        try
                        {
                            await s2n2r_RpcShutdownSelfAsync(svc.Address, "node closing");
                            ret++;
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    }
                    localServices.Clear();
                    staticServicesCacheMap.Clear();
                    staticServicesCache = null;
                    staticNodesCache = null;
                }
                return ret;
            }
            finally
            {
                await adapter.ShutdownAsync(this);
            }
        }
        /// <summary>
        /// 卸载当前节点
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> StopAsync()
        {
            taskCompletionSourcePool.Dispose();
            timers.Dispose();
            localServices.Dispose();
            try
            {
                var ret = await adapter.node2name_UnregistNodeAsync(this.NodeName);
                return ret;
            }
            finally
            {
                await adapter.StopAsync(this);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 本节点创建服务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public Task<RemoteProxyInfo> AddLocalServiceAsync(string serviceName, string serviceType, Properties config)
        {
            if (typeMapping.TryGetValue(serviceType, out var svcType))
            {
                var path = new RemoteAddress(serviceName, this.nodeName, serviceType);
                return adapter.s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation.Create, path, path, config);
            }
            else
            {
                throw new Exception("Service Type Not Exist : " + serviceType);
            }
        }
        protected virtual void OnUpdateStatusTick(object state)
        {
            using (var sb = new StringWriter())
            {
                this.adapter.node2name_UpdateNodeState(new ServiceNodeStateInfo()
                {
                    NodeName = this.NodeName,
                    ServiceCount = this.localServices.Count,
                    //MemoryUse = GC.GetTotalMemory(false),
                    MemoryTotal = Environment.WorkingSet,
                    CpuPercent = 0,
                });
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 添加类型映射
        /// </summary>
        public void AddTypeMapping(string name, Type type)
        {
            if (type == null)
            {
                throw new Exception("Can not resolve type name : " + name);
            }
            log.WarnFormat("AddTypeMapping: {0} = {1}", name, type.FullName);
            this.typeMapping.Add(name, type);
        }

        public TaskCompletionSource<T> CreateDefaultTaskCompletionSource<T>(string name)
        {
            return taskCompletionSourcePool.CreateTaskCompletionSource<T>(name, null, this.taskExecuteTimeout);
        }

        /// <summary>
        /// 创建TaskCompletionSource
        /// </summary>
        public TaskCompletionSource<T> CreateAsyncCompletionSource<T>(string name, TimeSpan timeoutMS, StackTrace trace)
        {
            if (timeoutMS == Timeout.InfiniteTimeSpan)
                return taskCompletionSourcePool.CreateTaskCompletionSource<T>(name, trace, Timeout.InfiniteTimeSpan);
            else
                return taskCompletionSourcePool.CreateTaskCompletionSource<T>(name, trace, this.taskExecuteTimeout + timeoutMS);
        }
        public TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout, StackTrace trace)
        {
            if (timeoutMS == Timeout.InfiniteTimeSpan)
                return taskCompletionSourcePool.CreateTaskCompletionSource<T>(name, trace, Timeout.InfiniteTimeSpan, timeout);
            else
                return taskCompletionSourcePool.CreateTaskCompletionSource<T>(name, trace, this.taskExecuteTimeout + timeoutMS, timeout);
        }
        public virtual IDisposable CreateTimeout(TimeSpan dueTime, TickHandler handler, object state = null)
        {
            return taskCompletionSourcePool.CreateTimeout(dueTime, handler, state);
        }
        public virtual IDisposable CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period, bool missfire)
        {
            return timers.CreateTimer(dueTime, period, missfire, state, callback);
        }
        public virtual Task<Disposable> CreateCornJobAsync(RemoteAddress svc, string corn_expression, object state, Action<ICornJobContext> callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing)
        {
            return QuartzScheduleFactory.Factory.CreateCornJobAsync(svc, corn_expression, missFire, state, callback);
        }
        //-------------------------------------------------------------------------------------------------------------------
        #region LocalService
        protected virtual RpcServiceBox CreateLocalService(RemoteAddress from, RemoteAddress address, IDictionary<string, string> config, bool isStatic)
        {
            if (typeMapping.TryGetValue(address.ServiceType, out var svcType))
            {
                return new RpcServiceBox(address, config, svcType, this, from, isStatic);
            }
            else
            {
                throw new Exception($"Service Not Exist : From={from} To={address}");
            }
        }
        protected virtual DeepCrystal.RPC.IRemoteService CreateProxy(RpcServiceBox from, IRemoteServiceInfo info)
        {
            if (localServices.TryGetLocalService(info.Address, out var remote))
            {
                var ret = new RpcLocalProxy(from, remote);
                return ret;
            }
            else if (info is RemoteProxyInfo remoteInfo)
            {
                var ret = new RpcRemoteProxy(from, remoteInfo.Clone());
                if (info.Address.ServiceNode != this.NodeName)
                {
                    remoteServicesCacheMap.TryAdd(info.Address.ServiceName, remoteInfo);
                }
                return ret;
            }
            else
            {
                throw new Exception("IRemoteServiceInfo is not RemoteProxyInfo : " + info);
            }
        }
        protected virtual bool TryGetCacheProxy(RpcServiceBox from, RemoteAddress path, out DeepCrystal.RPC.IRemoteService cache)
        {
            if (localServices.TryGetLocalService(path, out var remote))
            {
                cache = new RpcLocalProxy(from, remote);
                return true;
            }
            if (staticServicesCacheMap.TryGetValue(path.ServiceName, out var info))
            {
                cache = new RpcRemoteProxy(from, (info as RemoteProxyInfo).Clone());
                return true;
            }
            if (remoteServicesCacheMap.TryGetValue(path.ServiceName, out var remoteInfo))
            {
                cache = new RpcRemoteProxy(from, remoteInfo.Clone());
                return true;
            }
            cache = null;
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------
        public class LocalServiceMap : Disposable
        {
            private ReaderWriterLockSlim lock_rw = new ReaderWriterLockSlim();
            private readonly HashMap<string, RpcServiceBox> nameMap = new HashMap<string, RpcServiceBox>();
            private readonly HashMap<string, HashMap<string, RpcServiceBox>> typeMap = new HashMap<string, HashMap<string, RpcServiceBox>>();

            protected override void Disposing()
            {
                lock_rw.Dispose();
            }
            public int Count
            {
                get
                {
                    using (lock_rw.EnterRead())
                    {
                        return nameMap.Count;
                    }
                }
            }
            public bool TryGetLocalService(RemoteAddress addr, out RpcServiceBox svc)
            {
                using (lock_rw.EnterRead())
                {
                    if (!string.IsNullOrEmpty(addr.ServiceType) && typeMap.TryGetValue(addr.ServiceType, out var group))
                    {
                        if (group.TryGetValue(addr.ServiceName, out svc))
                        {
                            return true;
                        }
                    }
                    return nameMap.TryGetValue(addr.ServiceName, out svc);
                }
            }
            public bool TryGetLocalServiceByName(string serviceName, out RpcServiceBox svc)
            {
                using (lock_rw.EnterRead())
                {
                    return nameMap.TryGetValue(serviceName, out svc);
                }
            }
            public void GetAllLocalServices(List<RpcServiceBox> list)
            {
                using (lock_rw.EnterRead())
                {
                    list.AddRange(nameMap.Values);
                }
            }
            public void GetAllLocalServicesByType(string serviceType, List<RpcServiceBox> list)
            {
                using (lock_rw.EnterRead())
                {
                    if (typeMap.TryGetValue(serviceType, out var group))
                    {
                        list.AddRange(group.Values);
                    }
                }
            }
            internal void Add(RpcServiceBox svc)
            {
                using (lock_rw.EnterWrite())
                {
                    nameMap.Add(svc.Address.ServiceName, svc);
                    var group = typeMap.GetOrAdd(svc.Address.ServiceType, static (t) => new HashMap<string, RpcServiceBox>());
                    group.Add(svc.Address.ServiceName, svc);
                }
            }
            internal bool TryRemove(string serviceName, out RpcServiceBox svc)
            {
                using (lock_rw.EnterWrite())
                {
                    svc = nameMap.RemoveByKey(serviceName);
                    if (svc != null && typeMap.TryGetValue(svc.Address.ServiceType, out var group))
                    {
                        group.Remove(svc.Address.ServiceName);
                    }
                    return svc != null;
                }
            }
            internal void Clear()
            {
                using (lock_rw.EnterWrite())
                {
                    nameMap.Clear();
                    typeMap.Clear();
                }
            }
        }

        public List<IRemoteServiceInfo> GetAllLocalServicesInfo()
        {
            using (var list = new ArrayList<RpcServiceBox>())
            {
                localServices.GetAllLocalServices(list);
                return list.ConvertAll(e => e.LocalServiceInfo);
            }
        }

        public void ForEachLocalServices(Action<RpcServiceBox> action)
        {
            using (var list = new ArrayList<RpcServiceBox>())
            {
                localServices.GetAllLocalServices(list);
                foreach (var svc in list) { action(svc); }
            }
        }
        protected bool TryEncodeWormholeMessage(object message, out BinaryMessage bin)
        {
            if (message is BinaryMessage)
            {
                bin = (BinaryMessage)message;
                return true;
            }
            if (message is ISerializable req)
            {
                bin = rpcCodec.ToBinary(req);
                return true;
            }
            bin = BinaryMessage.NULL;
            return false;
        }
        protected bool TryDecodeWormholeMessage(BinaryMessage bin, out object message)
        {
            message = rpcCodec.ToSerializable(bin);
            return true;
        }
        public void ForEachLocalServicesByType(string serviceType, Action<RpcServiceBox> action)
        {
            using (var list = new ArrayList<RpcServiceBox>())
            {
                localServices.GetAllLocalServicesByType(serviceType, list);
                foreach (var svc in list) { action(svc); }
            }
        }

        protected void local_HandleWormholeWithType(RemoteAddress from, string serviceType, object message)
        {
            if (string.IsNullOrEmpty(serviceType))
            {
                this.ForEachLocalServices(svc => svc.r2s_PushWormhole(from, message));
            }
            else
            {
                this.ForEachLocalServicesByType(serviceType, svc => svc.r2s_PushWormhole(from, message));
            }
        }
        protected void local_HandleRpcNotifyWithType(RemoteAddress from, string serviceType, ISerializable msg)
        {
            if (string.IsNullOrEmpty(serviceType))
            {
                this.ForEachLocalServices(svc => svc.r2s_PushNotify(from, rpcCodec.CloneSerializable(msg)));
            }
            else
            {
                this.ForEachLocalServicesByType(serviceType, svc => svc.r2s_PushNotify(from, rpcCodec.CloneSerializable(msg)));
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------

        async Task<bool> n2s_HandleCreateLocalServiceAsync(RemoteAddress from, RemoteAddress addr, Dictionary<string, string> config, bool isStatic)
        {
            try
            {
                var svc = CreateLocalService(
                                 new RemoteAddress(from.ServiceName, from.ServiceNode, from.ServiceType),
                                 new RemoteAddress(addr.ServiceName, this.NodeName, addr.ServiceType),
                                 config,
                                 isStatic);
                localServices.Add(svc);
                try
                {
                    return await svc.n2s_HandleStartAsync(from);
                }
                catch
                {
                    localServices.TryRemove(addr.ServiceName, out var svc2);
                    throw;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return false;
            }
        }
        async Task<bool> n2s_HandleDestoryLocalServiceAsync(RemoteAddress from, RemoteAddress addr, string reason)
        {
            try
            {
                if (localServices.TryRemove(addr.ServiceName, out var svc))
                {
                    return await svc.n2s_HandleDestoryAsync(from, reason);
                }
                log.Warn($"Destory Local Service Not Exist : From={from} To={addr}");
                return false;
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                return false;
            }
        }
        void n2s_HandleRemoteDisposing(RemoteAddress addr)
        {
            remoteServicesCacheMap.TryRemove(addr.ServiceName, out var prx);
        }
        void n2s_HandleRemoteDestoryed(RemoteAddress addr)
        {
            this.InvokeRemoteDestoryed(addr);
        }

        void n2s_HandleAppMessage(BinaryMessage notify)
        {
            if (n2s_HandleSharedMemory(notify))
            {
                return;
            }
            RpcApplication.Instance.HandleAppMessage(notify);
            ForEachLocalServices(svc => svc.r2s_PushNotify(RemoteAddress.NULL, notify));
        }

        Task<string> n2s_HandleAppCommandAsync(string notify)
        {
            return RpcApplication.Instance.HandleAppCommandAsync(notify);
        }

        void r2s_HandleRemoteRpcRequest(RemoteAddress from, RemoteAddress to, BinaryMessage msg, OnRpcReturnBinary callback)
        {
            try
            {
                if (localServices.TryGetLocalService(to, out var svc))
                {
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, to);
                    {
                        evt.State = msg;
                        evt.SetCallbackRsp(callback);
                    }
                    try
                    {
                        svc.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        evt.Dispose();
                        callback(BinaryMessage.NULL, err);
                    }
                }
                else
                {
                    callback(BinaryMessage.NULL, new Exception($"Service Not Exist : From={from} To={to}"));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        void r2s_HandleRemoteRpcNotify(RemoteAddress from, RemoteAddress to, BinaryMessage msg)
        {
            if (localServices.TryGetLocalService(to, out var svc))
            {
                svc.r2s_PushNotify(from, msg);
            }
        }

        void r2s_HandleRemoteRpcBatchNotify(RemoteAddress from, RemoteAddress to, ICollection<BinaryMessage> msg)
        {
            if (localServices.TryGetLocalService(to, out var svc))
            {
                svc.r2s_PushBatchNotify(from, msg);
            }
        }

        void r2s_HandleRemoteRpcNotifyWithType(RemoteAddress from, string serviceType, BinaryMessage msg)
        {
            if (string.IsNullOrEmpty(serviceType))
            {
                ForEachLocalServices(svc => svc.r2s_PushNotify(from, msg));
            }
            else
            {
                ForEachLocalServicesByType(serviceType, svc => svc.r2s_PushNotify(from, msg));
            }
        }

        void r2s_HandleRemoteRpcWormhole(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        {
            if (localServices.TryGetLocalService(to, out var svc))
            {
                if (srcIsBin)
                {
                    svc.r2s_PushWormhole(from, msg);
                }
                else
                {
                    svc.r2s_PushWormhole(from, rpcCodec.ToSerializable(msg));
                }
            }
        }
        async Task<BinaryMessage> r2s_HandleRemoteRpcWormholeAsync(RemoteAddress from, RemoteAddress to, BinaryMessage msg, bool srcIsBin)
        {
            if (localServices.TryGetLocalService(to, out var svc))
            {
                object ret;
                if (srcIsBin)
                {
                    ret = await svc.r2s_PushWormholeAsync(from, msg);
                }
                else
                {
                    ret = await svc.r2s_PushWormholeAsync(from, rpcCodec.ToSerializable(msg));
                }
                if (ret is BinaryMessage rbin)
                {
                    return rbin;
                }
                else if (ret is ISerializable ntf)
                {
                    return rpcCodec.ToBinary(ntf);
                }
            }
            return BinaryMessage.NULL;
        }


        void r2s_HandleRemoteRpcWormholeWithType(RemoteAddress from, string serviceType, BinaryMessage msg, bool srcIsBin)
        {
            if (srcIsBin)
            {
                if (string.IsNullOrEmpty(serviceType))
                {
                    ForEachLocalServices(svc => svc.r2s_PushWormhole(from, msg));
                }
                else
                {
                    ForEachLocalServicesByType(serviceType, svc => svc.r2s_PushWormhole(from, msg));
                }
            }
            else
            {
                var ntf = rpcCodec.ToSerializable(msg);
                if (string.IsNullOrEmpty(serviceType))
                {
                    ForEachLocalServices(svc => svc.r2s_PushWormhole(from, ntf));
                }
                else
                {
                    ForEachLocalServicesByType(serviceType, svc => svc.r2s_PushWormhole(from, ntf));
                }
            }
        }

        private bool n2s_HandleSharedMemory(BinaryMessage notify)
        {
            if (sharedMemory.IsSyncRoute(notify.Route))
            {
                sharedMemory.HandleSyncMessage(RpcCodec.ToSerializable(notify));
                return true;
            }

            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------
        #region Rpc : Local -> Remote
        protected internal virtual void s2r_RpcNotify(RemoteAddress from, IRemoteService proxy, ISerializable req)
        {
            RpcStatistics.LogRpcNotify(req.GetType());
            if (proxy is RpcLocalProxy to_local)
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_OBJ, from, proxy.Address);
                try
                {
                    evt.State = (rpcCodec.CloneSerializable(req));
                    to_local.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to_local.IsIgnoreError)
                        log.Warn(err.Message, err);
                }
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_OBJ, from, proxy.Address);
                try
                {
                    evt.State = (rpcCodec.CloneSerializable(req));
                    to.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to.serviceProperties.IgnoreRequestError)
                        log.Warn(err.Message, err);
                }
            }
            else
            {
                try
                {
                    var bin = rpcCodec.ToBinary(req);
                    adapter.s2r_RpcNotify(from, proxy.Address, bin);
                }
                catch (Exception err)
                {
                    log.Warn(err.Message, err);
                }
            }
        }
        protected internal virtual void s2r_RpcNotify(RemoteAddress from, IRemoteService proxy, BinaryMessage req)
        {
            RpcStatistics.LogRpcNotify(req.Route);
            if (proxy is RpcLocalProxy to_local)
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_BIN, from, proxy.Address);
                try
                {
                    evt.State = req;
                    to_local.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to_local.IsIgnoreError)
                        log.Warn(err.Message, err);
                }
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_NOTIFY_BIN, from, proxy.Address);
                try
                {
                    evt.State = req;
                    to.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to.serviceProperties.IgnoreRequestError)
                        log.Warn(err.Message, err);
                }
            }
            else
            {
                try
                {
                    adapter.s2r_RpcNotify(from, proxy.Address, req);
                }
                catch (Exception err)
                {
                    log.Warn(err.Message, err);
                }
            }
        }

        protected internal virtual void s2r_RpcBatchNotify(RemoteAddress from, IRemoteService proxy, ICollection<ISerializable> req)
        {
            RpcStatistics.LogRpcNotify(req);
            if (proxy is RpcLocalProxy to_local)
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_OBJ, from, proxy.Address);
                try
                {
                    var array = new ISerializable[req.Count];
                    req.CopyTo(array, 0);
                    for (int i = array.Length - 1; i >= 0; --i)
                    {
                        array[i] = (rpcCodec.CloneSerializable(array[i]));
                    }
                    evt.State = (array);
                    to_local.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to_local.IsIgnoreError)
                        log.Warn(err.Message, err);
                }
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_OBJ, from, proxy.Address);
                try
                {
                    var array = new ISerializable[req.Count];
                    req.CopyTo(array, 0);
                    for (int i = array.Length - 1; i >= 0; --i)
                    {
                        array[i] = (rpcCodec.CloneSerializable(array[i]));
                    }
                    evt.State = (array);
                    to.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to.serviceProperties.IgnoreRequestError)
                        log.Warn(err.Message, err);
                }
            }
            else
            {
                var bin = new List<BinaryMessage>(req.Count);
                {
                    foreach (var e in req)
                    {
                        bin.Add(rpcCodec.ToBinary(e));
                    }
                    try
                    {
                        adapter.s2r_RpcBatchNotify(from, proxy.Address, bin);
                    }
                    catch (Exception err)
                    {
                        log.Warn(err.Message, err);
                    }
                }
            }
        }
        protected internal virtual void s2r_RpcBatchNotify(RemoteAddress from, IRemoteService proxy, ICollection<BinaryMessage> req)
        {
            RpcStatistics.LogRpcNotify(req);
            if (proxy is RpcLocalProxy to_local)
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_BIN, from, proxy.Address);
                try
                {
                    evt.State = req.ToArray();
                    to_local.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to_local.IsIgnoreError)
                        log.Warn(err.Message, err);
                }
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BATCH_NOTIFY_BIN, from, proxy.Address);
                try
                {
                    evt.State = req.ToArray();
                    to.PostRequest(evt);
                }
                catch (Exception err)
                {
                    evt.Dispose();
                    if (!to.serviceProperties.IgnoreRequestError)
                        log.Warn(err.Message, err);
                }
            }
            else
            {
                try
                {
                    adapter.s2r_RpcBatchNotify(from, proxy.Address, req);
                }
                catch (Exception err)
                {
                    log.Warn(err.Message, err);
                }
            }
        }

        protected internal virtual void s2r_RpcRequest(RemoteAddress from, IRemoteService proxy, ISerializable req, OnRpcReturn<ISerializable> callback)
        {
            RpcStatistics.LogRpcRequest(req.GetType(), ref callback);
            try
            {
                bool responsed = false;
                IDisposable timeout = null;
                void callback_timeout(TimeTaskMS time)
                {
                    callback_msg(null, new Exception("A Task Timeout Exception!"));
                }
                void callback_msg(ISerializable rsp, Exception rsp_err)
                {
                    timeout.Dispose();
                    if (!responsed)
                    {
                        responsed = true;
                        callback(rpcCodec.CloneSerializable(rsp), rsp_err);
                    }
                }
                if (proxy is RpcLocalProxy to_local)
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, from, proxy.Address);
                    try
                    {
                        evt.State = rpcCodec.CloneSerializable(req);
                        evt.SetCallbackRsp<ISerializable>(callback_msg);
                        to_local.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(null, err);
                    }
                }
                else if (localServices.TryGetLocalService(proxy.Address, out var to))
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, from, proxy.Address);
                    try
                    {
                        evt.State = rpcCodec.CloneSerializable(req);
                        evt.SetCallbackRsp<ISerializable>(callback_msg);
                        to.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(null, err);
                    }
                }
                else
                {
                    try
                    {
                        var req_bin = rpcCodec.ToBinary(req);
                        adapter.s2r_RpcRequest(from, proxy.Address, req_bin, callback_bin);
                        void callback_bin(BinaryMessage rsp_bin, Exception rsp_err)
                        {
                            if (rsp_err != null)
                            {
                                callback(null, rsp_err);
                            }
                            else
                            {
                                if (rsp_bin.HasRoute)
                                {
                                    ISerializable rsp;
                                    try
                                    {
                                        rsp = rpcCodec.ToSerializable(rsp_bin);
                                    }
                                    catch (Exception err)
                                    {
                                        callback(null, err);
                                        return;
                                    }
                                    callback(rsp, null);
                                }
                                else
                                {
                                    callback(null, new Exception(string.Format("Response Is Null : Msg={0} From={1} To={2}", req.GetType().FullName, from.ServiceName, proxy.ServiceName)));
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        callback(null, err);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        protected internal virtual void s2r_RpcRequest(RemoteAddress from, IRemoteService proxy, BinaryMessage req, OnRpcReturnBinary callback)
        {
            RpcStatistics.LogRpcRequest(req.Route, ref callback);
            try
            {
                bool responsed = false;
                IDisposable timeout = null;
                void callback_timeout(TimeTaskMS time)
                {
                    callback_binary(BinaryMessage.NULL, new Exception("A Task Timeout Exception!"));
                }
                void callback_binary(BinaryMessage rsp, Exception rsp_err)
                {
                    timeout.Dispose();
                    if (!responsed)
                    {
                        responsed = true;
                        callback(rsp, rsp_err);
                    }
                }
                if (proxy is RpcLocalProxy to_local)
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, proxy.Address);
                    try
                    {
                        evt.State = req;
                        evt.SetCallbackRsp(callback_binary);
                        to_local.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(BinaryMessage.NULL, err);
                    }
                }
                else if (localServices.TryGetLocalService(proxy.Address, out var to))
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, proxy.Address);
                    try
                    {
                        evt.State = req;
                        evt.SetCallbackRsp(callback_binary);
                        to.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(BinaryMessage.NULL, err);
                    }
                }
                else
                {
                    try
                    {
                        adapter.s2r_RpcRequest(from, proxy.Address, req, callback);
                    }
                    catch (Exception err)
                    {
                        callback(BinaryMessage.NULL, err);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        protected internal virtual void s2r_RpcRequest(RemoteAddress from, IRemoteService proxy, ISerializable req, OnRpcReturnVoid callback)
        {
            try
            {
                bool responsed = false;
                IDisposable timeout = null;
                void callback_timeout(TimeTaskMS time)
                {
                    callback_msg(new Exception("A Task Timeout Exception!"));
                }
                void callback_msg(Exception rsp_err)
                {
                    timeout.Dispose();
                    if (!responsed)
                    {
                        responsed = true;
                        callback(rsp_err);
                    }
                }
                if (proxy is RpcLocalProxy to_local)
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, from, proxy.Address);
                    try
                    {
                        evt.State = rpcCodec.CloneSerializable(req);
                        evt.SetCallbackRsp(callback_msg);
                        to_local.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(err);
                    }
                }
                else if (localServices.TryGetLocalService(proxy.Address, out var to))
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_OBJ, from, proxy.Address);
                    try
                    {
                        evt.State = rpcCodec.CloneSerializable(req);
                        evt.SetCallbackRsp(callback_msg);
                        to.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(err);
                    }
                }
                else
                {
                    try
                    {
                        var req_bin = rpcCodec.ToBinary(req);
                        adapter.s2r_RpcRequest(from, proxy.Address, req_bin, callback_bin);
                        void callback_bin(Exception rsp_err)
                        {
                            callback(rsp_err);
                        }
                    }
                    catch (Exception err)
                    {
                        callback(err);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        protected internal virtual void s2r_RpcRequest(RemoteAddress from, IRemoteService proxy, BinaryMessage req, OnRpcReturnVoid callback)
        {
            try
            {
                bool responsed = false;
                IDisposable timeout = null;
                void callback_timeout(TimeTaskMS time)
                {
                    callback_void(new Exception("A Task Timeout Exception!"));
                }
                void callback_void(Exception rsp_err)
                {
                    timeout.Dispose();
                    if (!responsed)
                    {
                        responsed = true;
                        callback(rsp_err);
                    }
                }
                if (proxy is RpcLocalProxy to_local)
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, proxy.Address);
                    try
                    {
                        evt.State = req;
                        evt.SetCallbackRsp(callback_void);
                        to_local.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(err);
                    }
                }
                else if (localServices.TryGetLocalService(proxy.Address, out var to))
                {
                    timeout = this.CreateTimeout(TimeSpan.FromMilliseconds(RpcServiceNode.NETWORK_TIMEOUT_MS + this.TaskExecuteTimeoutMS), callback_timeout);
                    var evt = RpcMessage.AllocAutoRelease(RpcEvent.REQUEST_BIN, from, proxy.Address);
                    try
                    {
                        evt.State = req;
                        evt.SetCallbackRsp(callback_void);
                        to.PostRequest(evt);
                    }
                    catch (Exception err)
                    {
                        timeout.Dispose();
                        evt.Dispose();
                        callback(err);
                    }
                }
                else
                {
                    try
                    {
                        adapter.s2r_RpcRequest(from, proxy.Address, req, callback);
                    }
                    catch (Exception err)
                    {
                        callback(err);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        protected internal void s2r_RpcWormholeTransport(RemoteAddress from, IRemoteService proxy, object message)
        {
            if (message is BinaryMessage bin)
            {
                RpcStatistics.LogRpcNotify(bin.Route);
            }
            else if (message is ISerializable req)
            {
                RpcStatistics.LogRpcNotify(req.GetType());
            }
            if (proxy is RpcLocalProxy to_local)
            {
                to_local.remote.r2s_PushWormhole(from, message);
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                to.r2s_PushWormhole(from, message);
            }
            else if (TryEncodeWormholeMessage(message, out var to_bin))
            {
                try
                {
                    adapter.s2r_RpcWormhole(from, proxy.Address, to_bin, message is BinaryMessage);
                }
                catch (Exception err)
                {
                    log.Warn(err.Message, err);
                }
            }
            else
            {
                log.Warn($"Can Not Encode Wormhole Message : From={from} To={proxy} Message={message}");
            }
        }
        protected internal async Task<object> s2r_RpcWormholeTransportAsync(RemoteAddress from, IRemoteService proxy, object message)
        {
            if (message is BinaryMessage bin)
            {
                RpcStatistics.LogRpcNotify(bin.Route);
            }
            else if (message is ISerializable req)
            {
                RpcStatistics.LogRpcNotify(req.GetType());
            }
            if (proxy is RpcLocalProxy to_local)
            {
                var ret = await to_local.remote.r2s_PushWormholeAsync(from, message);

                return ret;
            }
            else if (localServices.TryGetLocalService(proxy.Address, out var to))
            {
                var ret = await to.r2s_PushWormholeAsync(from, message);

                return ret;
            }
            else if (TryEncodeWormholeMessage(message, out var to_bin))
            {
                try
                {
                    var srcIsBin = message is BinaryMessage;
                    var rbin = await adapter.s2r_RpcWormholeAsync(from, proxy.Address, to_bin, srcIsBin);
                    if (srcIsBin)
                    {
                        return rbin;
                    }
                    if (TryDecodeWormholeMessage(rbin, out var ret))
                    {
                        return ret;
                    }
                }
                catch (Exception err)
                {
                    log.Warn(err.Message, err);
                }
            }
            else
            {
                log.Warn($"Can Not Encode Wormhole Message : From={from} To={proxy} Message={message}");
            }
            return Task.FromResult<object>(null);
        }
        //-------------------------------------------------------------------------------------------------------------------
        protected internal void s2r_RpcBroadcast(RemoteAddress from, ISerializable notify)
        {
            RpcStatistics.LogRpcNotify(notify.GetType());
            try
            {
                var bin = rpcCodec.ToBinary(notify);
                adapter.s2n_BroadcastServiceMessage(from, bin);
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
            }
        }
        protected internal void s2r_RpcBroadcastWithName(RemoteAddress from, ICollection<string> servicesName, ISerializable notify)
        {
            RpcStatistics.LogRpcNotify(notify.GetType());
            try
            {
                var list = new ArrayList<string>(servicesName);
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (localServices.TryGetLocalServiceByName(list[i], out var to))
                    {
                        to.r2s_PushNotify(from, rpcCodec.CloneSerializable(notify));
                        list.RemoveAt(i);
                    }
                }
                if (list.Count > 0)
                {
                    adapter.s2n_GetRemoteServicesAsync(list).ContinueWith(task =>
                    {
                        var remotes = task.GetResultAs();
                        if (remotes != null)
                        {
                            var bin = rpcCodec.ToBinary(notify);
                            {
                                foreach (var svc in remotes)
                                {
                                    adapter.s2r_RpcNotify(from, svc.Address.ToAddress(), bin);
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
            }
        }
        protected internal void s2r_RpcBroadcastWithNodeAndType(RemoteAddress from, string serviceNode, string serviceType, ISerializable notify)
        {
            RpcStatistics.LogRpcNotify(notify.GetType());
            try
            {
                if (serviceNode == this.nodeName)
                {
                    local_HandleRpcNotifyWithType(from, serviceType, notify);
                }
                else
                {
                    var nodesCache = staticNodesCache;
                    if (nodesCache != null)
                    {
                        s2r_RpcBroadcastWithNodeAndType(from, serviceNode, serviceType, notify, nodesCache);
                    }
                    else
                    {
                        s2n_GetStaticNodesInfoAsync().ContinueWith(t =>
                        {
                            try
                            {
                                s2r_RpcBroadcastWithNodeAndType(from, serviceNode, serviceType, notify, t.GetResultAs());
                            }
                            catch (Exception err)
                            {
                                log.Warn(err.Message, err);
                            }
                        });
                    }
                }
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
            }
        }
        protected internal void s2r_RpcWormholeBroadcastWithNodeAndType(RemoteAddress from, string serviceNode, string serviceType, object message)
        {
            if (message is BinaryMessage bin)
            {
                RpcStatistics.LogRpcNotify(bin.Route);
            }
            if (message is ISerializable req)
            {
                RpcStatistics.LogRpcNotify(req.GetType());
            }
            try
            {
                if (serviceNode == this.nodeName)
                {
                    local_HandleWormholeWithType(from, serviceType, message);
                }
                else
                {
                    var nodesCache = staticNodesCache;
                    if (nodesCache != null)
                    {
                        s2r_RpcWormholeBroadcastWithNodeAndType(from, serviceNode, serviceType, message, nodesCache);
                    }
                    else
                    {
                        s2n_GetStaticNodesInfoAsync().ContinueWith(t =>
                        {
                            try
                            {
                                s2r_RpcWormholeBroadcastWithNodeAndType(from, serviceNode, serviceType, message, t.GetResultAs());
                            }
                            catch (Exception err)
                            {
                                log.Warn(err.Message, err);
                            }
                        });
                    }
                }
            }
            catch (Exception err)
            {
                log.Warn(err.Message, err);
            }
        }
        protected void s2r_RpcBroadcastWithNodeAndType(RemoteAddress from, string serviceNode, string serviceType, ISerializable notify, IRemoteNodeInfo[] nodesCache)
        {
            if (nodesCache == null)
            {
                return;
            }
            if (nodesCache.Length == 1)
            {
                local_HandleRpcNotifyWithType(from, serviceType, notify);
            }
            else if (!string.IsNullOrEmpty(serviceNode))
            {
                var node = Array.Find(nodesCache, n => n.NodeName == serviceNode && n.AcceptServiceType.Contains(serviceType));
                if (node != null)
                {
                    var bin = rpcCodec.ToBinary(notify);
                    adapter.s2r_RpcNotifyWithType(from, node.NodeName, serviceType, bin);
                }
            }
            else if (!string.IsNullOrEmpty(serviceType))
            {
                var bin = rpcCodec.ToBinary(notify);
                foreach (var node in nodesCache)
                {
                    if (node.AcceptServiceType.Contains(serviceType))
                    {
                        if (node.NodeName == this.NodeName)
                        {
                            local_HandleRpcNotifyWithType(from, serviceType, notify);
                        }
                        else
                        {
                            adapter.s2r_RpcNotifyWithType(from, node.NodeName, serviceType, bin);
                        }
                    }
                }
            }
        }
        protected void s2r_RpcWormholeBroadcastWithNodeAndType(RemoteAddress from, string serviceNode, string serviceType, object message, IRemoteNodeInfo[] nodesCache)
        {
            if (nodesCache == null)
            {
                return;
            }
            if (nodesCache.Length == 1)
            {
                local_HandleWormholeWithType(from, serviceType, message);
            }
            else if (!string.IsNullOrEmpty(serviceNode))
            {
                var node = Array.Find(nodesCache, n => n.NodeName == serviceNode && n.AcceptServiceType.Contains(serviceType));
                if (node != null && TryEncodeWormholeMessage(message, out var bin))
                {
                    // adapter.s2r_RpcNotifyWithType(from, node.NodeName, serviceType, bin);
                    adapter.s2r_RpcWormholeWithType(from, node.NodeName, serviceType, bin, message is BinaryMessage);
                }
            }
            else if (!string.IsNullOrEmpty(serviceType))
            {
                var is_bin = TryEncodeWormholeMessage(message, out var bin);
                foreach (var node in nodesCache)
                {
                    if (node.AcceptServiceType.Contains(serviceType))
                    {
                        if (node.NodeName == this.NodeName)
                        {
                            local_HandleWormholeWithType(from, serviceType, message);
                        }
                        else if (is_bin)
                        {
                            adapter.s2r_RpcWormholeWithType(from, node.NodeName, serviceType, bin, message is BinaryMessage);
                        }
                        else
                        {
                            log.Warn($"Can Not Encode Wormhole Message : From={from} Message={message}");
                        }
                    }
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        protected internal virtual void s2n2r_RpcShutdown(RemoteAddress from, RemoteAddress target, string reason, Action<object, Exception> callback)
        {
            adapter.s2n_DestoryRemoteServiceAsync(from, target, reason).ContinueWith(task =>
            {
                try
                {
                    callback(task.GetResultAs(), task.Exception);
                }
                catch (Exception err)
                {
                    callback(null, err);
                }
            });
        }
        protected internal virtual Task<bool> s2n2r_RpcShutdownAsync(RemoteAddress from, RemoteAddress target, string reason)
        {
            return adapter.s2n_DestoryRemoteServiceAsync(from, target, reason);
        }
        protected internal virtual Task<bool> s2n2r_RpcShutdownSelfAsync(RemoteAddress svc, string reason)
        {
            return adapter.s2n_DestoryRemoteServiceAsync(svc, svc, reason);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------
        #region Rpc : Local -> NameServer

        internal Task<IRemoteService> s2n_CreateProxyAsync(RpcServiceBox from, RemoteAddress path, Properties cfg)
        {
            try
            {
                if (path.ServiceName == null)
                {
                    return Task.FromResult<IRemoteService>(null);
                }
                if (staticServicesCacheMap.TryGetValue(path.ServiceName, out var cache))
                {
                    return Task.FromResult<IRemoteService>(null);
                }
                if (localServices.TryGetLocalService(path, out var local))
                {
                    return Task.FromResult<IRemoteService>(null);
                }
                return adapter.s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation.Create, from.Address, path, cfg).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        log.Error(t.Exception.Message, t.Exception);
                    }
                    var info = t.GetResultAs();
                    if (info != null)
                    {
                        if (info.IsStatic) staticServicesCacheMap.TryAdd(info.Address.ServiceName, info);
                        return CreateProxy(from, info);
                    }
                    else
                    {
                        log.ErrorFormat("Create Service Error : {0}", path);
                    }
                    return null;
                });
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal Task<IRemoteService> s2n_GetOrCreateProxyAsync(RpcServiceBox from, RemoteAddress path, Properties cfg)
        {
            try
            {
                if (path.ServiceName == null)
                {
                    return Task.FromResult<IRemoteService>(null);
                }
                if (TryGetCacheProxy(from, path, out var cache))
                {
                    return Task.FromResult<IRemoteService>(cache);
                }
                return adapter.s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation.GetOrCreate, from.Address, path, cfg).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        log.Error(t.Exception.Message, t.Exception);
                    }
                    var info = t.GetResultAs();
                    if (info != null)
                    {
                        if (info.IsStatic) staticServicesCacheMap.TryAdd(info.Address.ServiceName, info);
                        return CreateProxy(from, info);
                    }
                    else
                    {
                        log.ErrorFormat("Service Not Exist : {0}", path);
                    }
                    return null;
                });
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return Task.FromResult<IRemoteService>(null);
        }
        internal Task<IRemoteService> s2n_GetProxyAsync(RpcServiceBox from, RemoteAddress path)
        {
            try
            {
                if (path.ServiceName == null)
                {
                    return Task.FromResult<IRemoteService>(null);
                }
                if (TryGetCacheProxy(from, path, out var cache))
                {
                    return Task.FromResult<IRemoteService>(cache);
                }
                return adapter.s2n_GetOrCreateRemoteServiceAsync(GetServiceOperation.Get, from.Address, path, new Dictionary<string, string>()).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        log.Error(t.Exception.Message, t.Exception);
                    }
                    var info = t.GetResultAs();
                    if (info != null)
                    {
                        if (info.IsStatic) staticServicesCacheMap.TryAdd(info.Address.ServiceName, info);
                        return CreateProxy(from, info);
                    }
                    else
                    {
                        log.ErrorFormat("Service Not Exist : {0}", path);
                    }
                    return null;
                });
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return Task.FromResult<IRemoteService>(null);
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal Task<int> s2n_GetServiceCountAsync(string serviceNode, string serviceType)
        {
            try
            {
                return adapter.s2n_GetServiceCountAsync(serviceNode, serviceType);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return Task.FromResult(-1);
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal async Task<IRemoteService[]> s2n_GetServicesAsync(RpcServiceBox from, ICollection<string> servicesName)
        {
            try
            {
                var list = new List<string>(servicesName);
                var ret = new List<IRemoteServiceInfo>();
                {
                    foreach (var name in servicesName)
                    {
                        if (staticServicesCacheMap.TryGetValue(name, out var cache))
                        {
                            list.Remove(name);
                            ret.Add(cache);
                        }
                    }
                    if (list.Count > 0)
                    {
                        var remotes = await adapter.s2n_GetRemoteServicesAsync(list);
                        if (remotes != null)
                        {
                            foreach (var info in remotes)
                            {
                                if (info.IsStatic) staticServicesCacheMap.TryAdd(info.Address.ServiceName, info);
                            }
                            ret.AddRange(remotes);
                        }
                    }
                    return ret.ConvertAll((info) => CreateProxy(from, info)).ToArray();
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteService[]> s2n_FindServicesWithAddressPatternAsync(RpcServiceBox from, string pattern)
        {
            try
            {
                var remotes = await adapter.s2n_GetRemoteServicesWithAddressPatternAsync(pattern);
                if (remotes != null)
                {
                    return Array.ConvertAll<RemoteProxyInfo, IRemoteService>(remotes, (info) => CreateProxy(from, info));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteService[]> s2n_FindServicesWithInfoLinqAsync(RpcServiceBox from, string where, string orderBy)
        {
            try
            {
                var remotes = await adapter.s2n_GetRemoteServicesWithInfoLinqAsync(where, orderBy);
                if (remotes != null)
                {
                    return Array.ConvertAll<RemoteProxyInfo, IRemoteService>(remotes, (info) => CreateProxy(from, info));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal async Task<IRemoteServiceInfo[]> s2n_GetStaticServicesInfoAsync()
        {
            //if (from.isStatic) throw new Exception("Invoker Service Is Static : " + from.Address);
            var buff = staticServicesCache;
            if (buff != null) { return buff; }
            try
            {
                buff = await adapter.s2n_GetStaticServicesAsync();
                if (buff != null)
                {
                    lock (staticServicesCacheMap)
                    {
                        foreach (var info in buff)
                        {
                            staticServicesCacheMap.TryAdd(info.ServiceName, info);
                        }
                        staticServicesCache = buff;
                    }
                    return buff;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteNodeInfo[]> s2n_GetStaticNodesInfoAsync()
        {
            var buff = staticNodesCache;
            if (buff != null) { return buff; }
            try
            {
                buff = await adapter.s2n_GetStaticNodesInfoAsync();
                if (buff != null)
                {
                    lock (staticNodesCacheMap)
                    {
                        if (staticNodesCache == null)
                        {
                            foreach (var info in buff)
                            {
                                staticNodesCacheMap.TryAdd(info.NodeName, info);
                            }
                            staticNodesCache = buff;
                        }
                    }
                    return buff;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteService> s2n_GetStaticServiceAsync(RpcServiceBox from, RemoteAddress path)
        {
            try
            {
                var buff = await s2n_GetStaticServicesInfoAsync();
                if (buff != null)
                {
                    var cache = Array.Find(buff, e => e.ServiceName == path.ServiceName);
                    if (cache != null)
                    {
                        return CreateProxy(from, cache);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteService[]> s2n_GetStaticServicesAsync(RpcServiceBox from)
        {
            try
            {
                var buff = await s2n_GetStaticServicesInfoAsync();
                if (buff != null)
                {
                    return Array.ConvertAll<IRemoteServiceInfo, IRemoteService>(buff, (info) => CreateProxy(from, info));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }
        internal async Task<IRemoteService> s2n_FindStaticServiceAsync(RpcServiceBox from, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            var buff = await this.s2n_GetStaticServicesInfoAsync();
            if (buff != null)
            {
                var info = select(buff);
                if (info != null)
                {
                    return CreateProxy(from, info);
                }
            }
            return null;
        }
        internal async Task<IRemoteService> s2n_FindStaticServiceWithTypeAsync(RpcServiceBox from, string serviceType, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            var buff = await this.s2n_GetStaticServicesInfoAsync();
            if (buff != null)
            {
                var info = select(Array.FindAll(buff, e => e.Address.ServiceType == serviceType));
                if (info != null)
                {
                    return CreateProxy(from, info);
                }
            }
            return null;
        }
        internal async Task<IRemoteService> s2n_FindStaticServiceWithNodeAsync(RpcServiceBox from, string serviceNode, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select)
        {
            var buff = await this.s2n_GetStaticServicesInfoAsync();
            if (buff != null)
            {
                var info = select(Array.FindAll(buff, e => e.Address.ServiceNode == serviceNode));
                if (info != null)
                {
                    return CreateProxy(from, info);
                }
            }
            return null;
        }
        //-------------------------------------------------------------------------------------------------------------------
        #endregion
        //-------------------------------------------------------------------------------------------------------------------
        #region Events

        private HashSet<Action<RemoteAddress>> event_HandleRemoteDestoryed = new HashSet<Action<RemoteAddress>>();

        private void InvokeRemoteDestoryed(RemoteAddress addr)
        {
            var list = new List<Action<RemoteAddress>>();
            {
                lock (event_HandleRemoteDestoryed)
                {
                    list.AddRange(event_HandleRemoteDestoryed);
                }
                foreach (var handler in list)
                {
                    try { handler(addr); } catch (Exception err) { log.Error(err.Message, err); }
                }
            }
        }

        internal event Action<RemoteAddress> OnHandleRemoteDestoryed
        {
            add
            {
                lock (event_HandleRemoteDestoryed)
                {
                    event_HandleRemoteDestoryed.Add(value);
                }
            }
            remove
            {
                lock (event_HandleRemoteDestoryed)
                {
                    event_HandleRemoteDestoryed.Remove(value);
                }
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------
    }

}
