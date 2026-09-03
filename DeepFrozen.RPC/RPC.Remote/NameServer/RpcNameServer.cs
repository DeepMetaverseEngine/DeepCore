using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Log;
using DeepCore.Threading;
using DeepCrystal.ORM;
using DeepCrystal.RPC;
using DeepCrystal.Threading.Dataflow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Remote.NameServer
{
    //-------------------------------------------------------------------------------------------------------------------
    public class RpcNameServer : Disposable
    {
        private ActionBlockExecutor actionBlock;
        private IOStreamPool codec_rpc;
        private IRpcApplication app;
        private IMappingAdapter db;
        private readonly NodeMap nodes;
        private readonly ServiceMap services;
        private readonly ServiceMap staticServices;
        private IRpcNameServerAdapter adapter;
        private bool isReady = false;

        public Logger log { get; private set; }
        public IRpcNameServerAdapter Adapter
        {
            get => adapter;
        }

        //----------------------------------------------------------------------------------------------------------------------
        public RpcNameServer(RpcNameConfig cfg, IRpcNameServerAdapter adapter)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.actionBlock = new ActionBlockExecutor();
            this.codec_rpc = new IOStreamPool(cfg.RpcCodec);
            this.db = ORMFactory.Instance.GetAdapter("0");
            {
                this.nodes = new("_NAME_SERVER:nodes", actionBlock, db);
                this.services = new("_NAME_SERVER:services", actionBlock, db);
                this.staticServices = new("_NAME_SERVER:staticServices", actionBlock, db);
            }
            this.adapter = adapter;
            this.adapter.Init(this);
            this.adapter.s2n_HandleRegistNodeAsync += this.s2n_HandleRegistNodeAsync;
            this.adapter.s2n_HandleUnregistNodeAsync += this.s2n_HandleUnregistNodeAsync;
            this.adapter.s2n_HandleUpdateNodeState += this.s2n_HandleUpdateNodeState;
            this.adapter.s2n_HandleGetOrCreateRemoteServiceAsync += this.s2n_HandleGetOrCreateRemoteServiceAsync;
            this.adapter.s2n_HandleDestoryRemoteServiceAsync += this.s2n_HandleDestoryRemoteServiceAsync;
            this.adapter.s2n_HandleGetServiceCountAsync += this.s2n_HandleGetServiceCountAsync;
            this.adapter.s2n_HandleGetRemoteServicesAsync += this.s2n_HandleGetRemoteServicesAsync;
            this.adapter.s2n_HandleGetRemoteServicesWithAddressPatternAsync += this.s2n_HandleGetRemoteServicesWithAddressPatternAsync;
            this.adapter.s2n_HandleGetRemoteServicesWithInfoLinqAsync += this.s2n_HandleGetRemoteServicesWithInfoLinqAsync;
            this.adapter.s2n_HandleGetStaticServicesAsync += this.s2n_HandleGetStaticServicesAsync;
            this.adapter.s2n_HandleGetStaticNodesAsync += this.s2n_HandleGetStaticNodesAsync;
            this.adapter.s2n_HandleServiceBroadcastMessage += this.s2n_HandleServiceBroadcastMessage;
            this.adapter.s2n_HandleAppBroadcastMessage += this.s2n_HandleAppBroadcastMessage;
            this.adapter.s2n_HandleAppBroadcastCommandAsync += this.s2n_HandleAppBroadcastCommandAsync;
        }
        protected override void Disposing()
        {
            //             event_OnNodeRegistered = null;
            //             event_OnNodeUnregistered = null;
            //             event_OnServiceStarted = null;
            //             event_OnServiceClosed = null;
        }
        //----------------------------------------------------------------------------------------------------------------------
        public async Task StartAsync()
        {
            await adapter.StartAsync(this);
        }
        public async Task StopAsync()
        {
            await CleanUpServicesAsync();
            this.actionBlock.Complete();
            await adapter.StopAsync(this);
        }
        /// <summary>
        /// 关闭所有服务
        /// </summary>
        /// <returns></returns>
        public Task ShutdownSerivceAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                var list = services.Values;
                list.Sort((a, b) => -(a.startTimeUTC.CompareTo(b.startTimeUTC)));
                return Task.Run(async () =>
                {
                    foreach (var svc in list)
                    {
                        await actionBlock.RunAsync(async () =>
                        {
                            try
                            {
                                if (services.ContainsKey(svc.ServiceName))
                                {
                                    RemoteAddress addr = svc.Address;
                                    await main_OnHandleDestoryRemoteServiceAsync(RemoteAddress.NULL, addr, "name server shutdown");
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                        });
                    }
                });
            });
        }

        public Task CleanUpServicesAsync()
        {
            return actionBlock.RunAsync(async () =>
            {
                var trans = db.CreateExecutableObjectTransaction(actionBlock);
                try
                {
                    nodes.Clear();
                    services.Clear();
                    staticServices.Clear();
                    nodes.BatchFlush(trans);
                    services.BatchFlush(trans);
                    staticServices.BatchFlush(trans);
                    await trans.ExecuteAsync();
                }
                finally
                {
                    await trans.DisposeAsync();
                }
            });
        }

        /// <summary>
        /// 恢复所有服务
        /// </summary>
        /// <returns></returns>
        public Task RecoverServicesAsync()
        {
            return actionBlock.RunAsync(async () =>
            {
                await nodes.LoadDataAsync();
                await services.LoadDataAsync();
                await staticServices.LoadDataAsync();
                foreach (var node in nodes.Values)
                {
                    log.Warn($"Reconver Node : {node.NodeName}");
                    try
                    {
                        //adapter.GetProxy(node.Data);
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                    }
                }
                foreach (var svc in services.Values)
                {
                    try
                    {
                        //adapter.GetProxy(svc.Data);
                        log.Warn($"Reconver Service : {svc.Address}");
                        await main_OnHandleGetRemoteServiceAsync(GetServiceOperation.GetOrCreate, RemoteAddress.NULL, svc.creater.ToAddress(), svc.info.Config, svc.isStatic);
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                    }
                }
            });
        }



        //----------------------------------------------------------------------------------------------------------------------
        #region IRpcNameServerAdapter

        Task<NodeInfo> s2n_HandleRegistNodeAsync(ServiceNodeStartInfo start) => actionBlock.RunAsync(main_OnHandleRegistNodeAsync, start);
        Task<NodeInfo> s2n_HandleUnregistNodeAsync(string nodeName) => actionBlock.RunAsync(main_OnHandleUnregistNodeAsync, nodeName);
        Task<NodeInfo[]> s2n_HandleGetStaticNodesAsync() => actionBlock.RunAsync(() => main_OnHandleGetStaticNodesAsync());
        Task s2n_HandleUpdateNodeState(ServiceNodeStateInfo state) => actionBlock.RunAsync(main_OnHandleUpdateNodeState, state);

        Task<ServiceInfo> s2n_HandleGetOrCreateRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config) => actionBlock.RunAsync(() => main_OnHandleGetRemoteServiceAsync(op, from, path, config, false));
        Task<ServiceInfo> s2n_HandleDestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason) => actionBlock.RunAsync(() => main_OnHandleDestoryRemoteServiceAsync(from, path, reason));
        Task<int> s2n_HandleGetServiceCountAsync(string serviceNode, string serviceType) => actionBlock.RunAsync(() => main_OnHandleGetServiceCountAsync(serviceNode, serviceType));
        Task<ServiceInfo[]> s2n_HandleGetRemoteServicesAsync(ICollection<string> paths) => actionBlock.RunAsync(() => main_OnHandleGetRemoteServicesAsync(paths));
        Task<ServiceInfo[]> s2n_HandleGetRemoteServicesWithAddressPatternAsync(string pattern) => actionBlock.RunAsync(() => main_OnHandleGetRemoteServicesWithPatternAsync(pattern));
        Task<ServiceInfo[]> s2n_HandleGetRemoteServicesWithInfoLinqAsync(string where, string orderBy) => actionBlock.RunAsync(() => main_OnHandleGetRemoteServicesWithLinqAsync(where, orderBy));
        Task<ServiceInfo[]> s2n_HandleGetStaticServicesAsync() => actionBlock.RunAsync(() => main_OnHandleGetStaticServicesAsync());
        Task s2n_HandleServiceBroadcastMessage(RemoteAddress from, BinaryMessage notify) => actionBlock.RunAsync(main_OnHandleBroadcast, from, notify);
        Task s2n_HandleAppBroadcastMessage(BinaryMessage notify) => actionBlock.RunAsync(main_OnHandleBroadcastApp, notify);
        Task<string> s2n_HandleAppBroadcastCommandAsync(string notify) => actionBlock.RunAsync(main_OnHandleAppCommandAsync, notify);

        #endregion
        //----------------------------------------------------------------------------------------------------------------------
        // 
        #region NameServer <-> Node

        private async Task<NodeInfo> main_OnHandleRegistNodeAsync(ServiceNodeStartInfo req)
        {
            try
            {
                if (nodes.TryAdd(req.NodeName, new NodeInfo(req), out var node))
                {
                    await nodes.FlushAsync();
                    //CreateProxy(node);
                    //event_OnNodeRegistered?.Invoke(node.NodeName);
                    return node.Data;
                }
                else
                {
                    //var p = CreateProxy(node);
                    log.Warn($"Regist Node Error : Service Node Already Exist : {req.NodeName}");
                    return node.Data;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<NodeInfo> main_OnHandleUnregistNodeAsync(string nodeName)
        {
            try
            {
                if (nodes.TryRemove(nodeName, out var node))
                {
                    await nodes.FlushAsync();
                    //event_OnNodeUnregistered?.Invoke(node.NodeName);
                    //RemoveProxy(node);
                    return node.Data;
                }
                else
                {
                    log.Warn($"Unregist Node Error : Service Node Not Exist : {nodeName}");
                    return null;
                }
                //                 else
                //                 {
                //                     throw new Exception("Service Node Not Exist : " + nodeName);
                //                 }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task main_OnHandleUpdateNodeState(ServiceNodeStateInfo st)
        {
            if (nodes.TryGetValue(st.NodeName, out var state))
            {
                state.state.Data = (st);
                await state.FlushAsync();
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------------

        #region NameServer <-> Services

        private async Task<ServiceInfo> main_OnHandleGetRemoteServiceAsync(GetServiceOperation op, RemoteAddress from, RemoteAddress path, Dictionary<string, string> config, bool isStatic)
        {
            try
            {
                var trans = db.CreateExecutableObjectTransaction(actionBlock);
                try
                {
                    NodeInfoMapping node = null;
                    ServiceInfoMapping svc = null;
                    bool isCreate = false;
                    switch (op)
                    {
                        case GetServiceOperation.GetOrCreate:
                            {
                                if (services.TryGetOrCreate(path.ServiceName, out svc, (name) =>
                                {
                                    node = DispatchNode(path);
                                    return new ServiceInfo(node.Data, from, path, config, isStatic);
                                }))
                                {
                                    node = nodes.Get(svc.info.Address.ServiceNode);
                                }
                                else
                                {
                                    isCreate = true;
                                }
                            }
                            break;
                        case GetServiceOperation.Create:
                            {
                                //svc = adapter.CreateServiceInfo(node, new ServiceInfo(node.Info, from, path, config, isStatic));
                                if (services.TryAdd(path.ServiceName, key =>
                                {
                                    node = DispatchNode(path);
                                    return new ServiceInfo(node.Data, from, path, config, isStatic);
                                }, out svc))
                                {
                                    isCreate = true;
                                }
                                else
                                {
                                    log.Warn($"Create Service Error : Service Already Exist : {path.ServiceName}");
                                }
                            }
                            break;
                        case GetServiceOperation.Get:
                            if (services.TryGetValue(path.ServiceName, out svc) == false)
                            {
                                //throw new Exception("Service Not Exist : " + path.ServiceName);
                                return null;
                            }
                            node = nodes.Get(svc.info.Address.ServiceNode);
                            break;
                    }
                    if (svc == null)
                    {
                        throw new Exception($"Service Not Exist :{path}  From={from}");
                        //throw new Exception("Service Not Exist : " + path);
                    }
                    if (isCreate)
                    {
                        if (svc.isStatic)
                        {
                            staticServices.Add(svc.ServiceName, svc.Data);
                        }
                        try
                        {
                            if (svc.status == ServiceStatus.NA)
                            {
                                node.serviceCount++;
                                try
                                {
                                    //var proxy = CreateProxy(node, svc);
                                    svc.status = ServiceStatus.Starting;
                                    svc.startTimeUTC = DateTime.UtcNow;
                                    if (await actionBlock.RunAsync(adapter.n2s_DispatchCreateServiceAsync(svc.creater.ToAddress(), node.Data, svc.Data)))
                                    {
                                        //event_OnServiceStarted?.Invoke(svc.Address);
                                        return svc.Data;
                                    }
                                }
                                finally
                                {
                                    svc.status = ServiceStatus.Started;
                                }
                            }
                            throw new Exception("Service Create Error : " + svc.Address);
                        }
                        catch
                        {
                            services.TryRemove(svc.ServiceName, out svc);
                            if (svc.isStatic)
                            {
                                staticServices.TryRemove(svc.ServiceName, out svc);
                            }
                            throw;
                        }
                    }
                    else
                    {
                        return svc.Data;
                    }
                }
                finally
                {
                    nodes.BatchFlush(trans);
                    services.BatchFlush(trans);
                    staticServices.BatchFlush(trans);
                    await trans.ExecuteAsync();
                    await trans.DisposeAsync();
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<ServiceInfo> main_OnHandleDestoryRemoteServiceAsync(RemoteAddress from, RemoteAddress path, string reason)
        {
            try
            {
                var trans = db.CreateExecutableObjectTransaction(actionBlock);
                try
                {
                    if (services.TryRemove(path.ServiceName, out var svc) == false)
                    {
                        throw new Exception("Destory Remote Service Not Exist : " + path.ServiceName);
                    }
                    if (svc.isStatic)
                    {
                        staticServices.TryRemove(svc.Address.ServiceName, out svc);
                    }
                    if (svc.status == ServiceStatus.Stopped || svc.status == ServiceStatus.Stopping)
                    {
                        throw new Exception("Service Already Destroyed : " + path.ServiceName);
                    }
                    else
                    {
                        await actionBlock.RunAsync(this.adapter.n2s_BroadcastRemoteDisposing(svc.Address));
                        {
                            // wait for status is started
                            // 防止一个SERVICE启动后立刻被销毁，等待启动结束
                            while (svc.status == ServiceStatus.Starting)
                            {
                                await actionBlock.RunAsync(Task.Delay(200));
                            }
                            if (svc.status == ServiceStatus.Started)
                            {
                                var node = nodes.Get(svc.info.Address.ServiceNode);
                                if (node != null)
                                {
                                    node.serviceCount--;
                                }
                                try
                                {
                                    svc.status = ServiceStatus.Stopping;
                                    if (await actionBlock.RunAsync(adapter.n2s_DispatchDestoryServiceAsync(from, node.Data, svc.Data, reason)))
                                    {
                                        //event_OnServiceClosed?.Invoke(svc.Address);
                                        //RemoveProxy(svc);
                                        await actionBlock.RunAsync(this.adapter.n2s_BroadcastRemoteDestoryed(svc.Address));
                                        return svc.Data;
                                    }
                                }
                                finally
                                {
                                    svc.status = ServiceStatus.Stopped;
                                }
                            }
                        }
                    }
                    return svc?.Data;
                }
                finally
                {
                    nodes.BatchFlush(trans);
                    services.BatchFlush(trans);
                    staticServices.BatchFlush(trans);
                    await trans.ExecuteAsync();
                    await trans.DisposeAsync();
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private Task<int> main_OnHandleGetServiceCountAsync(string serviceNode, string serviceType)
        {
            try
            {
                var in_services = (services.Values);
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

        private async Task<ServiceInfo[]> main_OnHandleGetRemoteServicesAsync(ICollection<string> paths)
        {
            try
            {
                var out_services = new List<ServiceInfo>();
                {
                    foreach (var path in paths)
                    {
                        if (services.TryGetValue(path, out var svc))
                        {
                            out_services.Add(svc.Data);
                        }
                    }
                    return (out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<ServiceInfo[]> main_OnHandleGetRemoteServicesWithPatternAsync(string pattern)
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
                    return (new ServiceInfo[0]);
                }
                var out_services = new List<ServiceInfo>();
                var in_services = services.Values;
                {
                    foreach (var svc in in_services)
                    {
                        if (regex.IsMatch(svc.Address.ToString()))
                        {
                            out_services.Add(svc);
                        }
                    }

                    return (out_services.ToArray());
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<ServiceInfo[]> main_OnHandleGetRemoteServicesWithLinqAsync(string where, string orderBy)
        {
            try
            {
                var in_services = services.Values;
                {
                    var queryableData = in_services.AsQueryable<IRemoteServiceInfo>();
                    var ret = System.Linq.Dynamic.DynamicQueryable.Where(queryableData, where);
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        ret = ret.OrderBy(orderBy);
                    }
                    return (Array.ConvertAll(ret.ToArray(), (a) => (a as ServiceInfo)));
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<ServiceInfo[]> main_OnHandleGetStaticServicesAsync()
        {
            try
            {
                if (isReady == false) throw new Exception("Static Services Not Ready !");
                var out_services = staticServices.Values;
                return (out_services.ToArray());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }

        private async Task<NodeInfo[]> main_OnHandleGetStaticNodesAsync()
        {
            try
            {
                var out_services = nodes.Values;
                return (out_services.ToArray());
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
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

        //         #region Events
        // 
        // //         private Action<string> event_OnNodeRegistered;
        // //         private Action<string> event_OnNodeUnregistered;
        // //         private Action<RemoteAddress> event_OnServiceStarted;
        // //         private Action<RemoteAddress> event_OnServiceClosed;
        // // 
        // //         public event Action<string> OnNodeRegistered { add { event_OnNodeRegistered += value; } remove { event_OnNodeRegistered -= value; } }
        // //         public event Action<string> OnNodeUnregistered { add { event_OnNodeUnregistered += value; } remove { event_OnNodeUnregistered -= value; } }
        // //         public event Action<RemoteAddress> OnServiceStarted { add { event_OnServiceStarted += value; } remove { event_OnServiceStarted -= value; } }
        // //         public event Action<RemoteAddress> OnServiceClosed { add { event_OnServiceClosed += value; } remove { event_OnServiceClosed -= value; } }
        // 
        // 
        //         #endregion

        //----------------------------------------------------------------------------------------------------------------------

        #region API

        //         public void ForEachNodes<T>(Action<T> action) where T : NodeInfoMapping
        //         {
        //             var list = new List<NodeInfoMapping>(nodes.Values);
        //             {
        //                 foreach (var node in list)
        //                 {
        //                     action(node as T);
        //                 }
        //             }
        //         }
        // 
        //         public void ForEachServices<T>(Action<T> action) where T : ServiceInfoMapping
        //         {
        //             var list = new List<ServiceInfoMapping>(services.Values);
        //             {
        //                 foreach (var svc in list)
        //                 {
        //                     action(svc as T);
        //                 }
        //             }
        //         }

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
                var svc = await main_OnHandleGetRemoteServiceAsync(GetServiceOperation.GetOrCreate, RemoteAddress.NULL, addr, config, true);
                if (svc != null)
                {
                    return svc.info;
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

        public Task<List<NodeInfo>> GetAllNodesAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                return nodes.Values;
            });
        }
        public Task<int> GetAllNodesCountAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                return services.Count;
            });
        }
        public Task<List<ServiceInfo>> GetAllServicesAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                return services.Values;
            });
        }
        public Task<int> GetAllServicesCountAsync()
        {
            return actionBlock.RunAsync(() =>
            {
                return services.Count;
            });
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

        #endregion

        //----------------------------------------------------------------------------------------------------------------------

        #region OP

        //         private NodeProxy CreateProxy(NodeInfoMapping node)
        //         {
        //             return nodeProxies.GetOrAdd(node.NodeName, n => adapter.CreateNodeProxy(node));
        //         }
        //         private ServiceProxy CreateProxy(NodeInfoMapping node, ServiceInfoMapping svc)
        //         {
        //             return serviceProxies.GetOrAdd(svc.ServiceName, n => adapter.CreateServiceProxy(CreateProxy(node), svc));
        //         }
        //         private NodeProxy RemoveProxy(NodeInfoMapping node)
        //         {
        //             nodeProxies.TryRemove(node.NodeName, out var ret);
        //             return ret;
        //         }
        //         private ServiceProxy RemoveProxy(ServiceInfoMapping svc)
        //         {
        //             serviceProxies.TryRemove(svc.ServiceName, out var ret);
        //             return ret;
        //         }

        //----------------------------------------------------------------------------------------------------------------------
        private NodeInfoMapping DispatchNode(RemoteAddress path)
        {
            if (path.ServiceNode != null)
            {
                if (nodes.TryGetValue(path.ServiceNode, out var node))
                {
                    if (node.Data.AcceptType(path.ServiceType))
                    {
                        return node;
                    }
                }
            }
            var list = new List<NodeInfoMapping>();
            {
                foreach (var e in nodes.MappingValues)
                {
                    if (e.Data.AcceptType(path.ServiceType))
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

        protected virtual NodeInfoMapping DispatchNode(List<NodeInfoMapping> list)
        {
            list.Sort((a, b) => { return (a.serviceCount - b.serviceCount); });
            return list[0];
        }




        #endregion

    }

}