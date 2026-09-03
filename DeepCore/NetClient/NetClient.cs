using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.NetClient
{

    public class INetClient : IDisposable, INetSession
    {
        //---------------------------------------------------------------------------------------------------------------------
        public Logger log { get; }
        protected internal readonly IExternalizableFactory codec;
        protected readonly MessageActionQueue<INetClient> tasks;
        protected readonly SystemTimeInterval<TimeSpan> request_timer;
        private bool disposed = false;
        private IClientAdapter adapter;
        //---------------------------------------------------------------------------------------------------------------------
        public MessageActionQueue<INetClient> TaskQueue { get { return tasks; } }
        public IDictionary<string, object> Attributes { get; } = new Dictionary<string, object>();
        public string Name { get; }
        public IExternalizableFactory Codec { get => codec; }
        public bool IsDisposed { get { return disposed; } }
        public bool IsConnected { get { return adapter == null ? false : adapter.IsConnected; } }
        public bool IsHandshake { get { return adapter == null ? false : adapter.IsHandshake; } }
        public int CurrentPing { get { return adapter == null ? 0 : adapter.Ping; } }
        public long TotalRecvBytes { get { return adapter == null ? 0 : adapter.TotalRecvBytes; } }
        public long TotalSentBytes { get { return adapter == null ? 0 : adapter.TotalSentBytes; } }
        public DateTime ConnectTime { get { return adapter == null ? DateTime.MinValue : adapter.ConnectTime; } }
        public TimeSpan RequestTimeout { get { return request_timer.Tag; } }
        public int RequestTimerTickMS { get { return (int)request_timer.IntervalTimeMS; } }
        public Logger Log { get { return log; } }
        public string ConnectAddress { get; protected set; }
        public TimeSpan ConnectTimeout { get; protected set; }
        public ISerializable ConnectUser { get; protected set; }
        public ISerializable ConnectedToken { get; protected set; }
        public bool Connected { get; protected set; }
        public object UserTag { get; set; }
        bool INetSession.IsConnected => adapter == null ? false : adapter.IsConnected;
        long INetSession.TotalSentBytes => adapter == null ? 0 : adapter.TotalSentBytes;
        long INetSession.TotalRecvBytes => adapter == null ? 0 : adapter.TotalRecvBytes;
        protected IClientAdapter Adapter => adapter;
        //---------------------------------------------------------------------------------------------------------------------
        public INetClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000)
        {
            this.ConnectTimeout = TimeSpan.FromSeconds(30);
            this.tasks = new MessageActionQueue<INetClient>();
            this.tasks.OnError += onError;
            this.Name = name;
            this.log = LoggerFactory.GetLogger((name == null ? GetType().Name : name));
            this.request_timer = new SystemTimeInterval<TimeSpan>().Init(request_timer_tick_ms);
            this.request_timer.Tag = ConnectTimeout;
            this.codec = codec;
        }
        protected virtual IClientAdapter CreateAdapter(string address) => NetClientFactory.Instance.CreateAdapter(this);
        protected IClientAdapter InitAdapter(string address)
        {
            this.adapter = CreateAdapter(address);
            this.adapter.OnReceivedMessage += adapter_OnReceivedMessage;
            this.adapter.OnSentMessage += adapter_OnSentMessage;
            this.adapter.OnError += adapter_OnError;
            this.adapter.OnDisconnected += adapter_OnDisconnected;
            this.adapter.OnConnected += adapter_OnConnected;
            return adapter;
        }
        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposing_events();
            this.Disposing();
            this.disposed = true;
        }
        public override string ToString()
        {
            return $"{Name}@{adapter?.ToString()}";
        }
        protected void UpdateTask()
        {
            tasks.ProcessMessages(this);
        }
        //---------------------------------------------------------------------------------------------------------------------
        public virtual bool Connect(string address, TimeSpan timeout, ISerializable user = null, Action<Exception, ISerializable> callback = null)
        {
            NetClientFactory.TryAddressMapping(address, out address);
            this.adapter = InitAdapter(address);
            this.ConnectTimeout = timeout;
            this.request_timer.Tag = timeout;
            this.ConnectAddress = address;
            this.ConnectUser = user;
            return this.adapter.Connect(address, (int)timeout.TotalMilliseconds, user, callback);
        }
        public virtual Task<ISerializable> ConnectAsync(string address, TimeSpan timeout, ISerializable user = null)
        {
            NetClientFactory.TryAddressMapping(address, out address);
            this.adapter = InitAdapter(address);
            this.ConnectTimeout = timeout;
            this.request_timer.Tag = timeout;
            this.ConnectAddress = address;
            this.ConnectUser = user;
            return this.adapter.ConnectAsync(address, (int)timeout.TotalMilliseconds, user);
        }

        /// <summary>
        /// 开始异步链接服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <param name="user"></param>
        /// <param name="callback"></param>
        public bool Connect(string host, int port, TimeSpan timeout, ISerializable user = null, Action<Exception, ISerializable> callback = null)
        {
            return Connect($"{host}:{port}", timeout, user, callback);
        }
        /// <summary>
        /// 同步链接服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <param name="user"></param>
        public Task<ISerializable> ConnectAsync(string host, int port, TimeSpan timeout, ISerializable user = null)
        {
            return ConnectAsync($"{host}:{port}", timeout, user);
        }
        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 主动断开连接
        /// </summary>
        public bool Disconnect()
        {
            try
            {
                if (adapter != null)
                {
                    var ret = adapter.Disconnect(() =>
                    {
                        this.clear_response(false);
                    });
                    ;
                    return ret;
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
            return false;
        }

        protected virtual void Disposing()
        {
            try
            {
                this.adapter?.Disconnect(() => { });
                this.clear_request();
                this.clear_response(false);
                this.clear_push();
                this.ClearLastResponse();
                this.adapter?.Dispose();
                this.tasks.Dispose();
            }
            catch { }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 心跳，客户端一般是一帧调用一次
        /// </summary>
        public virtual void Update()
        {
            this.adapter?.Update();
            this.UpdateTask();
            this.main_check_request_timeout();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 发送请求消息
        /// </summary>
        /// <param name="data">发送的请求</param>
        /// <param name="action">监听返回</param>
        protected bool InternalRequestBinary(BinaryMessage data, Action<Exception, BinaryMessage> action, bool infinity)
        {
            uint id = req_id_gen.GetAndIncrement();
            this.listen_response_binary(data.Route.ToString(), id, infinity, action);
            try
            {
                return this.send(data, MessageType.MSG_REQUEST_C2S, id);
            }
            catch (Exception e)
            {
                onError(e);
                return false;
            }
        }

        /// <summary>
        /// 发送请求消息
        /// </summary>
        /// <param name="req">发送的请求</param>
        /// <param name="cb">监听返回</param>
        /// <param name="state">监听状态</param>
        protected bool InternalRequest(ISerializable req, Action<Exception, ISerializable> cb, bool infinity, object state)
        {
            var c = codec.GetCodec(req.GetType());
            if (c == null)
            {
                throw new Exception($"Can not found type '{req.GetType()}' in codec!!!");
            }
            var route = c.MessageType.FullName;
            if (!this.onBeforeRequest(route, req, state))
            {
                return false;
            }
            this.onRequestStart(route, req, state);
            uint id = req_id_gen.GetAndIncrement();
            this.listen_response(route, id, infinity, (err, response) =>
            {
                if (err != null) log.Error($"Request Error : {err.Message}", err);
                this.onRequestEnd(route, err, response, state);
                cb(err, response);
            });
            try
            {
                return this.send(req, MessageType.MSG_REQUEST_C2S, id);
            }
            catch (Exception e)
            {
                onError(e);
            }
            return false;
        }
        protected virtual TaskCompletionSource<T> CreateTCS<T>(string message, bool infinity)
        {
            return new TaskCompletionSource<T>();
        }
        //---------------------------------------------------------------------------------------------------------------------
        public bool RequestBinary(BinaryMessage data, Action<Exception, BinaryMessage> action)
        {
            return InternalRequestBinary(data, action, false);
        }
        //-----------------------------------------------------------------------------------------------------------------
        public bool Request(ISerializable req, Action<Exception, ISerializable> cb, object state = null)
        {
            return this.InternalRequest(req, cb, false, state);
        }
        public bool Request<RSP>(ISerializable req, Action<Exception, RSP> cb, object state = null) where RSP : ISerializable
        {
            return this.InternalRequest(req, (err, rsp) => { cb(err, (RSP)rsp); }, false, state);
        }
        //-----------------------------------------------------------------------------------------------------------------
        public bool Request(ISerializable req, Action<TResponse<ISerializable>> cb, object state = null)
        {
            return this.InternalRequest(req, (err, rsp) => { cb(new TResponse<ISerializable>() { err = err, rsp = rsp, state = state }); }, false, state);
        }
        public bool Request<RSP>(ISerializable req, Action<TResponse<RSP>> cb, object state = null) where RSP : ISerializable
        {
            return this.Request<RSP>(req, (err, rsp) => { cb(new TResponse<RSP>() { err = err, rsp = rsp, state = state }); }, state);
        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public bool DemandBinary(BinaryMessage data, Action<Exception, BinaryMessage> action)
        {
            return InternalRequestBinary(data, action, true);
        }
        public bool Demand(ISerializable req, Action<Exception, ISerializable> cb, object state = null)
        {
            return this.InternalRequest(req, cb, true, state);
        }
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public bool Demand<RSP>(ISerializable req, Action<Exception, RSP> cb, object state = null) where RSP : ISerializable
        {
            return this.InternalRequest(req, (err, rsp) => { cb(err, (RSP)rsp); }, true, state);
        }
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public bool Demand(ISerializable req, Action<TResponse<ISerializable>> cb, object state = null)
        {
            return this.InternalRequest(req, (err, rsp) => { cb(new TResponse<ISerializable>() { err = err, rsp = rsp, state = state }); }, true, state);
        }
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public bool Demand<RSP>(ISerializable req, Action<TResponse<RSP>> cb, object state = null) where RSP : ISerializable
        {
            return this.Demand<RSP>(req, (err, rsp) => { cb(new TResponse<RSP>() { err = err, rsp = rsp, state = state }); }, state);
        }
        //-----------------------------------------------------------------------------------------------------------------
        public Task<BinaryMessage> RequestBinaryAsync(BinaryMessage data)
        {
            var tcs = CreateTCS<BinaryMessage>("RequestBinaryAsync", false);
            this.InternalRequestBinary(data, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, false);
            return tcs.Task;
        }
        public Task<ISerializable> RequestAsync(ISerializable req, object state = null)
        {
            var tcs = CreateTCS<ISerializable>("RequestAsync", false);
            this.InternalRequest(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, false, state);
            return tcs.Task;
        }
        public Task<RSP> RequestAsync<RSP>(ISerializable req, object state = null) where RSP : ISerializable
        {
            var tcs = CreateTCS<RSP>("RequestAsync", false);
            this.Request<RSP>(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, state);
            return tcs.Task;
        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public Task<BinaryMessage> DemandBinaryAsync(BinaryMessage data)
        {
            var tcs = CreateTCS<BinaryMessage>("DemandBinaryAsync", true);
            this.InternalRequestBinary(data, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, true);
            return tcs.Task;
        }
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public Task<ISerializable> DemandAsync(ISerializable req, object state = null)
        {
            var tcs = CreateTCS<ISerializable>("DemandAsync", true);
            this.InternalRequest(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, true, state);
            return tcs.Task;
        }
        /// <summary>
        /// 永久等待返回
        /// </summary>
        public Task<RSP> DemandAsync<RSP>(ISerializable req, object state = null) where RSP : ISerializable
        {
            var tcs = CreateTCS<RSP>("DemandAsync", true);
            this.Demand<RSP>(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, state);
            return tcs.Task;
        }
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 发送通知消息
        /// </summary>
        public void Notify(ISerializable msg)
        {
            this.send(msg, MessageType.MSG_NOTIFY, 0);
        }

        /// <summary>
        /// 发送通知消息
        /// </summary>
        public void NotifyBinary(BinaryMessage data)
        {
            this.send(data, MessageType.MSG_NOTIFY, 0);
        }

        //-----------------------------------------------------------------------------------------------------------------
        public IPushHandler Listen<T>(Action<IPushHandler, T> action, bool recursion_base_type = true) where T : ISerializable
        {
            var c = codec.GetCodec(typeof(T));
            if (c != null)
            {
                return listen_push(c.MessageID, (handler, push) => { action(handler, (T)push); }, null, recursion_base_type);
            }
            else
            {
                throw new Exception($"Can not found type '{typeof(T)}' in codec!!!");
            }
        }
        public IPushHandler Listen(Type type, Action<IPushHandler, ISerializable> action, bool recursion_base_type = true)
        {
            var c = codec.GetCodec(type);
            if (c != null)
            {
                return listen_push(c.MessageID, action, null, recursion_base_type);
            }
            else
            {
                throw new Exception($"Can not found type '{type}' in codec!!!");
            }
        }
        public IPushHandler ListenBinary(int route, Action<IPushHandler, BinaryMessage> action, bool recursion_base_type = true)
        {
            return listen_push(route, null, action, recursion_base_type);
        }

        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <typeparam name="T">监听类型</typeparam>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        public IPushHandler Listen<T>(Action<T> action, bool recursion_base_type = true) where T : ISerializable
        {
            var c = codec.GetCodec(typeof(T));
            if (c != null)
            {
                return listen_push(c.MessageID, (handler, push) => { action((T)push); }, null, recursion_base_type);
            }
            else
            {
                throw new Exception($"Can not found type '{typeof(T)}' in codec!!!");
            }
        }
        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <param name="type">监听类型</param>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        public IPushHandler Listen(Type type, Action<ISerializable> action, bool recursion_base_type = true)
        {
            var c = codec.GetCodec(type);
            if (c != null)
            {
                return listen_push(c.MessageID, (handler, push) => { action(push); }, null, recursion_base_type);
            }
            else
            {
                throw new Exception($"Can not found type '{type}' in codec!!!");
            }
        }
        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <param name="route">监听类型</param>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        public IPushHandler ListenBinary(int route, Action<BinaryMessage> action, bool recursion_base_type = true)
        {
            return listen_push(route, null, (handler, push) => { action(push); }, recursion_base_type);
        }

        public void ListenOnce<T>(Action<T> action, bool recursion_base_type = true) where T : ISerializable
        {
            var func = new Action<IPushHandler, T>((handler, evt) => handler.Dispose());
            Listen<T>(func, recursion_base_type);
        }
        public void ListenOnce(Type type, Action<ISerializable> action, bool recursion_base_type = true)
        {
            var func = new Action<IPushHandler, ISerializable>((handler, evt) => handler.Dispose());
            Listen(type, func, recursion_base_type);
        }
        public void ListenBinaryOnce(int route, Action<BinaryMessage> action, bool recursion_base_type = true)
        {
            var func = new Action<IPushHandler, BinaryMessage>((handler, evt) => handler.Dispose());
            ListenBinary(route, func, recursion_base_type);
        }

        //-----------------------------------------------------------------------------------------------------------------
        public IRpcRequestHandler HandleRpcRequest(Func<ISerializable, Action<ISerializable>, bool> handle)
        {
            return this.listen_request(handle, null);
        }
        public IRpcRequestHandler HandleRpcRequest<REQ>(Func<REQ, Action<ISerializable>, bool> handle) where REQ : ISerializable
        {
            return this.listen_request((req, cb) =>
            {
                if (req is REQ) { return handle((REQ)req, cb); }
                return false;
            }, null);
        }
        public IRpcRequestHandler HandleRpcRequest(Func<BinaryMessage, Action<BinaryMessage>, bool> handle)
        {
            return this.listen_request(null, handle);
        }

        //-----------------------------------------------------------------------------------------------------------------
        #region ProcessMessages


        protected internal bool send(ISerializable msg, MessageType msgType, uint send_id)
        {
            try
            {
                if (adapter != null)
                {
                    return adapter.Send(msg, msgType, send_id);
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
            return false;
        }

        protected internal bool send(BinaryMessage msg, MessageType msgType, uint send_id)
        {
            try
            {
                if (adapter != null)
                {
                    return adapter.Send(msg, msgType, send_id);
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
            return false;
        }

        protected virtual void net_process_message(IRecvMessage msg)
        {
            if (async_event_HandleMessageImmediately != null)
            {
                if (async_event_HandleMessageImmediately(msg))
                {
                    return;
                }
            }
            try
            {
                switch (msg.MsgType)
                {
                    case MessageType.MSG_RESPONSE_S2C:
                        net_process_response(msg);
                        break;
                    case MessageType.MSG_NOTIFY:
                        net_process_push(msg);
                        break;
                    case MessageType.MSG_RPC_REQUEST_S2C:
                        net_process_request(msg);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception err)
            {
                onError(err);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------
        #endregion
        //-----------------------------------------------------------------------------------------------
        #region process_request_response

        private HashMap<uint, RequestHandler> response_map = new HashMap<uint, RequestHandler>();
        private readonly AtomicUInt req_id_gen = new AtomicUInt(1);
        private List<RequestHandler> response_removing = new(1);
        private List<KeyValuePair<uint, RequestHandler>> response_list = new(1);
        private void main_check_request_timeout()
        {
            if (request_timer.Update())
            {
                try
                {
                    response_list.Clear();
                    response_removing.Clear();
                    var cur_time = CUtils.TickTimeMS;
                    lock (response_map)
                    {
                        if (response_map.Count > 0)
                        {
                            response_list.AddRange(response_map);
                            foreach (var req in response_list)
                            {
                                if (req.Value.CheckTimeout(RequestTimeout.TotalMilliseconds, cur_time))
                                {
                                    response_map.Remove(req.Key);
                                    response_removing.Add(req.Value);
                                }
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (response_removing.Count > 0)
                    {
                        foreach (var r in response_removing)
                        {
                            var exp = new NetException($"Request Timeout : Route={r.Route} SendID={r.SendID}");
                            exp.Timeout = true;
                            r.Invoke(exp);
                        }
                    }
                }
                finally
                {
                    response_list.Clear();
                    response_removing.Clear();
                }
            }

        }
        private void clear_response(bool async)
        {
            List<RequestHandler> cbs = null;
            {
                lock (response_map)
                {
                    if (response_map.Count > 0)
                    {
                        if (cbs == null) cbs = new();
                        cbs.AddRange(response_map.Values);
                        response_map.Clear();
                    }
                    else
                    {
                        return;
                    }
                }
                if (cbs != null)
                {
                    foreach (var cb in cbs)
                    {
                        var err = new NetException("closed");
                        if (async)
                        {
                            TaskQueue.Enqueue(() =>
                            {
                                cb.Invoke(err);
                            });
                        }
                        else
                        {
                            cb.Invoke(err);
                        }
                    }
                }
            }
        }
        private void listen_response(string route, uint send_id, bool infinity, Action<NetException, ISerializable> cb)
        {
            if (send_id > 0 && cb != null)
            {
                lock (response_map)
                {
                    this.response_map.Add(send_id, new RequestHandler(this, route, send_id, infinity, cb, null));
                }
            }
        }
        private void listen_response_binary(string route, uint send_id, bool infinity, Action<NetException, BinaryMessage> cb)
        {
            if (send_id > 0 && cb != null)
            {
                lock (response_map)
                {
                    this.response_map.Add(send_id, new RequestHandler(this, route, send_id, infinity, null, cb));
                }
            }
        }

        private void process_Response(ISerializable rsp, RequestHandler cb)
        {

            cb.Invoke(rsp);
        }
        private void process_Response(BinaryMessage response, RequestHandler cb)
        {
            cb.InvokeBin(response);
        }
        protected virtual void net_process_response(IRecvMessage msg)
        {
            async_event_HandleResponseImmediately?.Invoke(msg);
            RequestHandler cb;
            lock (response_map)
            {
                if (!response_map.TryGetValue(msg.MsgSendID, out cb))
                {
                    msg.ReadBody();
                    log.WarnFormat("Ignore response message : {0}", msg);
                    return;
                }
                response_map.Remove(msg.MsgSendID);
            }
            try
            {
                //if (cb.callback != null)
                {
                    if (cb.IsBinary)
                    {
                        var response = msg.ReadBodyBinary();
                        TaskQueue.Enqueue((this, cb, response), static (st) =>
                        {
                            st.Item1.process_Response(st.response, st.cb);
                        });
                    }
                    else
                    {
                        object response = msg.ReadBody();
                        if (response is ISerializable rsp)
                        {
                            this.addLastResponse(rsp);
                            TaskQueue.Enqueue((this, cb, rsp), static (st) =>
                            {
                                st.Item1.process_Response(st.rsp, st.cb);
                            });
                        }
                        else
                        {
                            throw new Exception("Deserialize response error : " + msg);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TaskQueue.Enqueue(() =>
                {
                    var exp = new NetException(e.Message, e);
                    cb.Invoke(exp);
                });
                onError(e);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------
        #region process_push

        public event MainHandleNotifyHandler MainHandleNotify;

        private HashMap<int, List<PushHandler>> push_handler = new HashMap<int, List<PushHandler>>();
        private PushHandler listen_push(int route, Action<IPushHandler, ISerializable> cb, Action<IPushHandler, BinaryMessage> cbb, bool recursion_base_type)
        {
            var ret = new PushHandler(this, route, cb, cbb, recursion_base_type);
            lock (push_handler)
            {
                {
                    var act = push_handler.GetOrAdd(route, static _ => new List<PushHandler>());
                    act.Add(ret);
                }
                if (recursion_base_type)
                {
                    var sub_types = new List<TypeCodec>(1);
                    {
                        IOUtil.GetAllSubTypes(codec, codec.GetCodec(route).MessageType, sub_types);
                        foreach (var sub_codec in sub_types)
                        {
                            var sub_act = push_handler.GetOrAdd(sub_codec.MessageID, static _ => new List<PushHandler>());
                            sub_act.Add(ret);
                        }
                    }
                }
            }
            return ret;
        }
        internal void remove_push(PushHandler handler)
        {
            lock (push_handler)
            {
                handler.Dispose();
                var act = push_handler.Get(handler.Route);
                if (act != null)
                {
                    act.Remove(handler);
                }
                if (handler.IsRecursion)
                {
                    var sub_types = new List<TypeCodec>(1);
                    {
                        IOUtil.GetAllSubTypes(codec, codec.GetCodec(handler.Route).MessageType, sub_types);
                        foreach (var sub_codec in sub_types)
                        {
                            var sub_act = push_handler.Get(sub_codec.MessageID);
                            if (sub_act != null)
                            {
                                sub_act.Remove(handler);
                            }
                        }
                    }
                }
            }
        }
        private void get_push_handler(int msg_route, List<PushHandler> all)
        {
            lock (push_handler)
            {
                var list = push_handler.Get(msg_route);
                if (list != null) { all.AddRange(list); }
            }
        }
        private void clear_push()
        {
            lock (push_handler)
            {
                push_handler.Clear();
            }
        }

        private List<PushHandler> invoking_PushHandler = new List<PushHandler>();

        private void process_MainHandleNotify(ISerializable push, MainHandleNotifyHandler handler)
        {
            try
            {
                handler.Invoke(push);
            }
            catch (Exception e)
            {
                log.Error($"process_MainHandleNotify Error : MsgType={push?.GetType()}", e);
            }
        }
        private void process_PushHandler(ISerializable push, PushHandler handler)
        {
            handler.Invoke(push);
        }
        private void process_PushHandler(BinaryMessage push, PushHandler handler)
        {
            handler.InvokeBin(push);
        }

        protected virtual void net_process_push(IRecvMessage msg)
        {
            try
            {
                {
                    var handler = MainHandleNotify;
                    if (handler != null)
                    {
                        var push = msg.ReadBody();
                        TaskQueue.Enqueue((this, push, handler), static (st) =>
                        {
                            st.Item1.process_MainHandleNotify(st.push, st.handler);
                        });
                    }
                }
                lock (invoking_PushHandler)
                {
                    invoking_PushHandler.Clear();
                    try
                    {
                        get_push_handler(msg.MsgRoute, invoking_PushHandler);
                        if (invoking_PushHandler.Count > 0)
                        {
                            for (int i = 0; i < invoking_PushHandler.Count; i++)
                            {
                                var handler = invoking_PushHandler[i];
                                if (handler.IsBinary)
                                {
                                    var push_bin = msg.ReadBodyBinary();
                                    TaskQueue.Enqueue((this, push_bin, handler), static (st) =>
                                    {
                                        st.Item1.process_PushHandler(st.push_bin, st.handler);
                                    });
                                }
                                else
                                {
                                    var push = msg.ReadBody();
                                    if (push is ISerializable ntf)
                                    {
                                        this.addLastResponse(ntf);
                                        TaskQueue.Enqueue((this, ntf, handler), static (st) =>
                                        {
                                            st.Item1.process_PushHandler(st.ntf, st.handler);
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (async_event_HandleNoListeningPush != null)
                            {
                                async_event_HandleNoListeningPush.Invoke(msg);
                            }
                        }
                    }
                    finally
                    {
                        invoking_PushHandler.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                onError(e);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------
        #region process_rpc_request S->C

        private readonly List<RpcRequestHandler> rpc_request_handler = new List<RpcRequestHandler>();
        private RpcRequestHandler listen_request(Func<ISerializable, Action<ISerializable>, bool> cb, Func<BinaryMessage, Action<BinaryMessage>, bool> cbb)
        {
            var ret = new RpcRequestHandler(this, cb, cbb);
            lock (push_handler)
            {
                rpc_request_handler.Add(ret);
            }
            return ret;
        }
        internal void remove_request(RpcRequestHandler handler)
        {
            lock (rpc_request_handler)
            {
                rpc_request_handler.Remove(handler);
            }
        }
        private void clear_request()
        {
            lock (rpc_request_handler)
            {
                rpc_request_handler.Clear();
            }
        }

        private void process_Request(List<RpcRequestHandler> invoking, BinaryMessage req_bin, ISerializable req_msg, uint sendID)
        {
            foreach (var handler in invoking)
            {
                if (handler.IsBinary)
                {
                    if (handler.InvokeBin(req_bin, sendID)) { return; }
                }
                else
                {
                    if (handler.Invoke(req_msg, sendID)) { return; }
                }
            }
        }
        protected virtual void net_process_request(IRecvMessage msg)
        {
            ISerializable req_msg = null;
            BinaryMessage req_bin = BinaryMessage.NULL;
            uint sendID = msg.MsgSendID;
            List<RpcRequestHandler> invoking = null;
            try
            {
                lock (rpc_request_handler)
                {
                    if (rpc_request_handler.Count > 0)
                    {
                        if (invoking == null) invoking = new();
                        invoking.AddRange(rpc_request_handler);
                    }
                }
                if (invoking != null)
                {
                    foreach (var handler in invoking)
                    {
                        if (handler.IsBinary)
                        {
                            if (req_bin.IsNoRoute)
                            {
                                req_bin = msg.ReadBodyBinary();
                            }
                        }
                        else
                        {
                            if (req_msg == null)
                            {
                                req_msg = msg.ReadBody();
                                this.addLastResponse(req_msg);
                            }
                        }
                    }
                    TaskQueue.Enqueue((this, invoking, req_bin, req_msg, sendID), static (st) =>
                    {
                        st.Item1.process_Request(st.invoking, st.req_bin, st.req_msg, st.sendID);
                    });
                }
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------
        #region LastResponse

        private bool enable_last_response = true;
        private readonly HashMap<Type, object> last_response = new HashMap<Type, object>();

        public bool IsSaveResponse
        {
            get { return enable_last_response; }
            set
            {
                enable_last_response = value;
                if (value == false)
                {
                    lock (last_response)
                    {
                        last_response.Clear();
                    }
                }
            }
        }

        internal void addLastResponse(object push)
        {
            async_event_HandleBodyImmediately?.Invoke(push);
            if (push != null && enable_last_response)
            {
                lock (last_response)
                {
                    last_response.Put(push.GetType(), push);
                }
            }
        }
        public T GetLastResponse<T>() where T : class
        {
            lock (last_response)
            {
                return last_response.Get(typeof(T)) as T;
            }
        }
        public object GetLastResponse(Type type)
        {
            lock (last_response)
            {
                return last_response.Get(type);
            }
        }
        public void ClearLastResponse()
        {
            lock (last_response)
            {
                last_response.Clear();
            }
        }
        #endregion

        //-----------------------------------------------------------------------------------------------------------------
        #region Events

        //-----------------------------------------------------------------------------------------------------------------

        internal void onError(Exception err)
        {
            if (async_event_OnError != null) { async_event_OnError.Invoke(err); }
        }
        private bool onBeforeRequest(string route, ISerializable req, object option)
        {
            if (event_OnBeforeRequest != null)
            {
                bool ret = true;
                foreach (BeforeRequestHandler handler in event_OnBeforeRequest.GetInvocationList())
                {
                    if (!handler.Invoke(route, req, option))
                    {
                        ret = false;
                    }
                }
                return ret;
            }
            return true;
        }
        private void onRequestStart(string route, ISerializable req, object option)
        {
            event_RequestStartEvent?.Invoke(route, req, option);
        }
        private void onRequestEnd(string route, NetException excep, ISerializable response, object option)
        {
            event_RequestEndEvent?.Invoke(route, excep, response, option);
        }
        private void adapter_OnConnected(SystemHandshakeAck ack, ISerializable token)
        {
            this.Connected = true;
            this.ConnectedToken = token;
            this.TaskQueue.Enqueue(() =>
            {
                event_OnConnected?.Invoke(token);
            });
        }
        private void adapter_OnDisconnected(CloseReason reason, string err)
        {
            this.Connected = false;
            this.clear_response(true);
            this.TaskQueue.Enqueue(() =>
            {
                if (event_OnDisconnected != null)
                {
                    event_OnDisconnected.Invoke(reason, err);
                }
            });
        }
        private void adapter_OnError(Exception err)
        {
            this.onError(err);
        }
        private void adapter_OnReceivedMessage(IRecvMessage msg)
        {
            this.net_process_message(msg);
        }
        private void adapter_OnSentMessage(ISendMessage obj)
        {
            var sent = OnMessageSent;
            if (sent != null && obj.BodyObject is ISerializable msg)
            {
                this.TaskQueue.Enqueue((sent, msg), static (t, st) =>
                {
                    st.sent.Invoke(t, st.msg);
                });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 网络线程立即回调收到消息，返回True拦截消息
        /// </summary>
        public event HandleMessageImmediately NetHandleMessageImmediately
        {
            add { lock (this) async_event_HandleMessageImmediately += value; }
            remove { lock (this) async_event_HandleMessageImmediately -= value; }
        }
        /// <summary>
        /// 网络线程立即回调收到消息
        /// </summary>
        public event HandleResponseImmediately NetHandleResponseImmediately
        {
            add { lock (this) async_event_HandleResponseImmediately += value; }
            remove { lock (this) async_event_HandleResponseImmediately -= value; }
        }
        /// <summary>
        /// 网络线程立即回调未处理消息
        /// </summary>
        public event HandleNoListeningPush NetHandleNoListeningPush
        {
            add { lock (this) async_event_HandleNoListeningPush += value; }
            remove { lock (this) async_event_HandleNoListeningPush -= value; }
        }
        /// <summary>
        /// 网络线程立即回调收到非二进制消息
        /// </summary>
        public event HandleBodyImmediately NetHandleBodyImmediately
        {
            add { lock (this) async_event_HandleBodyImmediately += value; }
            remove { lock (this) async_event_HandleBodyImmediately -= value; }
        }
        /// <summary>
        /// 网络线程错误回调
        /// </summary>
        public event Action<Exception> NetError
        {
            add { lock (this) async_event_OnError += value; }
            remove { lock (this) async_event_OnError -= value; }
        }

        //-----------------------------------------------------------------------------------------------------------------


        public event MessageSentHandler OnMessageSent;

        //-----------------------------------------------------------------------------------------------------------------

        public event BeforeRequestHandler OnBeforeRequest { add { event_OnBeforeRequest += value; } remove { event_OnBeforeRequest -= value; } }
        /// <summary>
        /// 请求开始事件
        /// </summary>
        public event RequestStartHandler OnRequestStart { add { event_RequestStartEvent += value; } remove { event_RequestStartEvent -= value; } }
        /// <summary>
        /// 请求返回事件
        /// </summary>
        public event RequestEndHandler OnRequestEnd { add { event_RequestEndEvent += value; } remove { event_RequestEndEvent -= value; } }
        /// <summary>
        /// 已连线
        /// </summary>
        public event Action<ISerializable> OnConnected { add { event_OnConnected += value; } remove { event_OnConnected -= value; } }
        /// <summary>
        /// 已断线
        /// </summary>
        public event Action<CloseReason, string> OnDisconnected { add { event_OnDisconnected += value; } remove { event_OnDisconnected -= value; } }

        //-----------------------------------------------------------------------------------------------------------------
        private HandleMessageImmediately async_event_HandleMessageImmediately;
        private HandleResponseImmediately async_event_HandleResponseImmediately;
        private HandleBodyImmediately async_event_HandleBodyImmediately;
        private HandleNoListeningPush async_event_HandleNoListeningPush;
        private Action<Exception> async_event_OnError;

        private BeforeRequestHandler event_OnBeforeRequest;
        private RequestStartHandler event_RequestStartEvent;
        private RequestEndHandler event_RequestEndEvent;
        private Action<ISerializable> event_OnConnected;
        private Action<CloseReason, string> event_OnDisconnected;
        //-----------------------------------------------------------------------------------------------------------------
        protected virtual void disposing_events()
        {
            OnMessageSent = null;

            async_event_HandleMessageImmediately = null;
            async_event_HandleResponseImmediately = null;
            async_event_HandleBodyImmediately = null;
            async_event_HandleNoListeningPush = null;
            async_event_OnError = null;

            event_RequestStartEvent = null;
            event_RequestEndEvent = null;
            event_OnConnected = null;
            event_OnDisconnected = null;
            event_OnBeforeRequest = null;
        }
        //-----------------------------------------------------------------------------------------------------------------
        #endregion

    }


    public delegate void MainHandleNotifyHandler(ISerializable ntf);

    /// <summary>
    /// 网络线程立即回调收到消息，返回True拦截消息
    /// </summary>
    /// <param name="protocol"></param>
    /// <returns>eat this message</returns>
    public delegate bool HandleMessageImmediately(IRecvMessage protocol);
    public delegate void HandleResponseImmediately(IRecvMessage protocol);
    public delegate void HandleNoListeningPush(IRecvMessage protocol);
    public delegate void HandleBodyImmediately(object message);


    public delegate void MessageSentHandler(INetClient sender, ISerializable msg);
    public delegate bool BeforeRequestHandler(string route, ISerializable request, object option);
    public delegate void RequestStartHandler(string route, ISerializable request, object option);
    public delegate void RequestEndHandler(string route, Exception error, ISerializable response, object option);

}
