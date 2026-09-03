using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepFrozen.RPC.Invoker;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.RpcTest
{


    public class RpcTest
    {
        private readonly Logger log = LoggerFactory.GetLogger("RpcTest");
        private readonly HashMap<string, ServiceContainer> services = new HashMap<string, ServiceContainer>();
        private readonly HashMap<string, Type> name_mapping = new HashMap<string, Type>();
        internal readonly IOStreamPool codec;
        internal readonly RpcInvokerManager invoke_manager;
        internal readonly Timers timers;
        internal readonly TaskScheduler taskScheduler;

        public RpcTest(IOStreamPool pool, TaskScheduler taskScheduler = null)
        {
            this.codec = pool;
            this.taskScheduler = taskScheduler;
            this.invoke_manager = new RpcInvokerManager(pool);
            this.timers = new Timers();
        }
        public RpcTest(IExternalizableFactory factory, TaskScheduler taskScheduler = null)
            : this(new IOStreamPool(factory), taskScheduler)
        {
        }
        public void AddServiceType(string name, Type type)
        {
            this.name_mapping.Add(name, type);
        }
        //-------------------------------------------------------------------------------------------------------------------
        public Task DisposeAsync()
        {
            timers.Dispose();
            var list = this.GetAllServices();
            list.Sort((a, b) => { return -(a.info.StartTimeUTC.CompareTo(b.info.StartTimeUTC)); });
            List<Task> tasks = new List<Task>();
            foreach (var svc in list)
            {
                var task = svc.DestoryAsync(svc.info.Address, "system shutdown");
                tasks.Add(task);
            }
            return Task.WhenAll(tasks);
        }
        //-------------------------------------------------------------------------------------------------------------------
        public Task<ServiceContainer> AddService(IDictionary<string, string> cfg)
        {
            var config = new Properties();
            cfg.TryGetValue("ServiceNode", out var node);
            cfg.TryGetValue("ServiceType", out var type);
            foreach (var e in cfg)
            {
                config.Add(e.Key, e.Value);
            }
            var info = new ServiceInfo()
            {
                Config = config,
                Address = new RemoteAddress(cfg["ServiceName"], node, type),
                StartTimeUTC = DateTime.UtcNow
            };
            ServiceContainer svc;
            return this.AddService(null, info, out svc);
        }
        internal Task<ServiceContainer> AddService(RemoteAddress from, ServiceInfo info, out ServiceContainer svc)
        {
            var svcType = name_mapping[info.Address.ServiceType];
            if (svcType == null)
                throw new Exception("Unknow Service Type : " + info.Address.ServiceType);
            lock (services)
            {
                if (services.ContainsKey(info.Address.ServiceName))
                {
                    throw new Exception("Service Already Exist : " + info.Address.ServiceName);
                }
                else
                {
                    svc = new ServiceContainer(info, svcType, this);
                    services.Add(svc.Key, svc);
                    ServiceContainer ret = svc;
                    var task = svc.StartAsync(from);
                    return task.ContinueWith((rst) => { return ret; });
                }
            }
        }
        internal Task<ServiceContainer> TryAddService(RemoteAddress from, ServiceInfo info, out ServiceContainer svc)
        {
            var svcType = name_mapping[info.Address.ServiceType];
            if (svcType == null)
                throw new Exception("Unknow Service Type : " + info.Address.ServiceType);
            lock (services)
            {
                svc = services.Get(info.Address.ServiceName);
                if (svc == null)
                {
                    svc = new ServiceContainer(info, svcType, this);
                    services.Add(svc.Key, svc);
                    var task = svc.StartAsync(from);
                    ServiceContainer ret = svc;
                    return task.ContinueWith((rst) => { return ret; });
                }
                else
                {
                    ServiceContainer ret = svc;
                    return Task.FromResult(ret);
                }
            }
        }
        internal ServiceContainer RemoveService(RemoteAddress addr)
        {
            lock (services)
            {
                return services.RemoveByKey(addr.ServiceName);
            }
        }
        internal ServiceContainer GetService(RemoteAddress addr)
        {
            lock (services)
            {
                return services.Get(addr.ServiceName);
            }
        }
        internal Task<ServiceContainer> GetServiceAsync(RemoteAddress addr)
        {
            ServiceContainer ret = GetService(addr);
            return Task.FromResult(ret);
        }
        internal List<ServiceContainer> GetAllServices()
        {
            lock (services)
            {
                return new List<ServiceContainer>(services.Values);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal Task<ServiceContainer> GetOrCreateAsync(RemoteAddress from, RemoteAddress path, IDictionary<string, string> cfg)
        {
            var info = new ServiceInfo()
            {
                Config = new Properties(cfg),
                Address = path,
                Creator = from,
                StartTimeUTC = DateTime.UtcNow
            };
            return TryAddService(from, info, out var svc);
        }
        internal Task<ServiceContainer> CreateAsync(RemoteAddress from, RemoteAddress path, IDictionary<string, string> cfg)
        {
            var info = new ServiceInfo()
            {
                Config = new Properties(cfg),
                Address = path,
                Creator = from,
                StartTimeUTC = DateTime.UtcNow
            };
            return AddService(from, info, out var svc);
        }
        internal ServiceContainer GetOrCreate(RemoteAddress from, RemoteAddress path, IDictionary<string, string> cfg)
        {
            var info = new ServiceInfo()
            {
                Config = new Properties(cfg),
                Address = path,
                Creator = from,
                StartTimeUTC = DateTime.UtcNow
            };
            TryAddService(from, info, out var svc).Wait();
            return svc;
        }
        internal ServiceContainer Create(RemoteAddress from, RemoteAddress path, IDictionary<string, string> cfg)
        {
            var info = new ServiceInfo()
            {
                Config = new Properties(cfg),
                Address = path,
                Creator = from,
                StartTimeUTC = DateTime.UtcNow
            };
            AddService(from, info, out var svc).Wait();
            return svc;
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal void PostRequest(RpcMessage rpc)
        {
            var to = this.GetService(rpc.to);
            if (to == null)
            {
                throw new Exception(string.Format("Post Target Remote Service Not Exist : To={0}", rpc.to));
            }
            if (rpc.callback_async)
            {
                if (rpc.from == rpc.to)
                {
                    throw new Exception(string.Format("Async Post Not Support : From == To : {0}", rpc.to));
                }
                var from = this.GetService(rpc.from);
                if (from == null)
                {
                    throw new Exception(string.Format("Wait From Remote Service Not Exist : From={0}", rpc.from));
                }
            }
            to.PostRequest(rpc);
        }
        internal void PostResponse(RpcMessage rpc)
        {
            var to = this.GetService(rpc.to);
            if (to == null)
            {
                throw new Exception(string.Format("Post Target Remote Service Not Exist : {0}", rpc.to));
            }
            to.PostResponse(rpc);
        }
        //-------------------------------------------------------------------------------------------------------------------
        internal TaskCompletionSource<T> CreateAsyncCompletionSource<T>(string name, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<T>();
            if (timeoutMs != Timeout.Infinite)
            {
                var ct = new CancellationTokenSource(timeoutMs);
                ct.Token.Register(() =>
                {
                    if (tcs.TrySetCanceled())
                    {
                        log.Warn(name + " : Async Task Timeout, Canceled!!!");
                    }
                }, useSynchronizationContext: false);
            }
            return tcs;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------

}
