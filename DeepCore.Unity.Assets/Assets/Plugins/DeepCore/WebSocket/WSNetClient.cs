#if NATIVE_WEBSOCKET
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.NetClient;
using NativeWebSocket;
using System;
using System.Threading.Tasks;
using static DeepCore.NetClient.MessagePool;

namespace DeepCore.Net.WS
{
    public class WSNetClient : INetClient
    {
        public WSNetClient(IExternalizableFactory codec, string name = null, int request_timer_tick_ms = 5000) : base(codec, name, request_timer_tick_ms)
        {
        }
        protected override IClientAdapter CreateAdapter(string addr) { return new WSWebSocketAdapter(this); }
    }

    public class WSWebSocketAdapter : Disposable, IClientAdapter
    {
        public static bool ENABLE_SENDING_POOL = true;

        protected INetClient client;
        protected MessagePool msg_pool;

        private IWebSocket websocket;
        protected Logger log => client.log;
        public bool IsConnected => websocket == null ? false : websocket.State == WebSocketState.Open;
        public bool IsHandshake { get; private set; } = false;
        public long TotalRecvBytes { get; private set; }
        public long TotalSentBytes { get; private set; }
        public int Ping { get; private set; }
        public DateTime ConnectTime { get; private set; }
        private int connect_timeout;
        public WSWebSocketAdapter(INetClient client)
        {
            this.client = client;
            this.msg_pool = new MessagePool(client.Codec, true);
        }
        public bool Disconnect(Action action)
        {
            return _run_close(CloseReason.Disconnect, action);
        }
        protected override void Disposing()
        {
            this.OnReceivedMessage = null;
            this.OnSentMessage = null;
            this.OnError = null;
            this.OnDisconnected = null;
            this.OnConnected = null;
            this.Disconnect(null);
            this.websocket?.Dispose();
            this.websocket = null;
            this.msg_pool?.Dispose();
            this.msg_pool = null;
        }
        public void Update()
        {
            this.websocket?.DispatchMessageQueue();
            _check_heartbeat();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        public bool Connect(string address, int timeout, ISerializable user, Action<Exception, ISerializable> callback)
        {
            if (websocket == null)
            {
                connect_timeout = timeout;
                ConnectTime = DateTime.UtcNow;
                var uri = new Uri(address);
                websocket = new WebSocket(uri.ToString(), WSNetClientFactory.SubProtocol, WSNetClientFactory.ClientHeader);
                _connect_async(user).ContinueWith(t =>
                {
                    client.TaskQueue.Enqueue(() =>
                    {
                        if (t.IsCanceled)
                        {
                            callback(null, null);
                        }
                        else if (t.IsFaulted)
                        {
                            callback(t.Exception, null);
                        }
                        else if (t.IsCompleted)
                        {
                            callback(null, t.Result);
                        }
                        else
                        {
                            callback(null, null);
                        }
                    });
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            return false;
        }

        public async Task<ISerializable> ConnectAsync(string address, int timeout, ISerializable user)
        {
            if (websocket == null)
            {
                connect_timeout = timeout;
                ConnectTime = DateTime.UtcNow;
                var uri = new Uri(address);
                websocket = new WebSocket(uri.ToString(), WSNetClientFactory.SubProtocol, WSNetClientFactory.ClientHeader);
                return await _connect_async(user);
            }
            return default;
        }
        public bool Send(ISerializable msg, MessageType msgType, uint send_id)
        {
            if (!IsConnected) return false;
            _start_send(msg, msgType, send_id).Forget();
            return true;
        }
        public bool Send(BinaryMessage msg, MessageType msgType, uint send_id)
        {
            if (!IsConnected) return false;
            _start_send(msg, msgType, send_id).Forget();
            return true;
        }
        public event Action<IRecvMessage> OnReceivedMessage;
        public event Action<ISendMessage> OnSentMessage;
        public event Action<Exception> OnError;
        public event Action<CloseReason, string> OnDisconnected;
        public event Action<SystemHandshakeAck, ISerializable> OnConnected;
        //-------------------------------------------------------------------------------------------------------------------------------
        private void Websocket_OnOpen()
        {
            log.Info("Websocket_OnOpen");
            _send_handshake(connect_user).Forget();
        }
        private void Websocket_OnClose(WebSocketCloseCode closeCode)
        {
            log.Info("Websocket_OnClose:" + closeCode);
            _run_close(CloseReason.Disconnect, null, $"{closeCode}");
        }
        private void Websocket_OnError(string errorMsg)
        {
            log.Info("Websocket_OnClose:" + errorMsg);
            _run_close(CloseReason.Error, null, $"{errorMsg}", new Exception(errorMsg));
        }
        private void Websocket_OnMessage(byte[] data)
        {
            this.TotalRecvBytes += data.Length;
            try
            {
                if (data.Length > 0)
                {
                    var recv_object = msg_pool.AllocRecv();
                    try
                    {
                        recv_object.adapter = this;
                        recv_object.FillBuffer(data, 0, data.Length);
                        recv_object.ReadHead();
                        if (recv_object.PkgLength > 0)
                        {
                            _received_package_(recv_object);
                        }
                        else
                        {
                            _received_package_(recv_object);
                        }
                    }
                    finally
                    {
                        client.TaskQueue.Enqueue(recv_object, static (r) => { r.Dispose(); });
                    }
                }
            }
            catch (Exception err)
            {
                _run_close(CloseReason.Error, null, $"{err.Message}", err);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private TaskCompletionSource<ISerializable> connect_complete_tcs;
        private ISerializable connect_user;

        private async Task<ISerializable> _connect_async(ISerializable user)
        {
            this.connect_user = user;
            this.connect_complete_tcs = new TaskCompletionSource<ISerializable>();
            try
            {
                var task = this.connect_complete_tcs.Task;
                try
                {
                    this.websocket.OnError += Websocket_OnError;
                    this.websocket.OnMessage += Websocket_OnMessage;
                    this.websocket.OnClose += Websocket_OnClose;
                    this.websocket.OnOpen += Websocket_OnOpen;
                    websocket.Connect().Forget();
                }
                catch (Exception err)
                {
                    _run_close(CloseReason.Error, null, err.ToString(), err);
                }
                if (connect_timeout > 0)
                {
                    var completed = await Task.WhenAny(task, Task.Delay(connect_timeout));
                    if (completed != task && !task.IsCompleted)
                    {
                        var timeoutEx = new TimeoutException($"websocket connect timeout after {connect_timeout}ms");
                        _run_close(CloseReason.Error, null, timeoutEx.Message, timeoutEx);
                    }
                }
                return await task;
            }
            finally
            {
                connect_complete_tcs = null;
            }
        }
        private bool _run_close(CloseReason reason, Action done = null, string message = null, Exception err = null)
        {
            log.Error($"[WebSocket] RunClose  reason={reason}, message={message ?? "<null>"}, " +
                     $"state={websocket?.State}, handshake={IsHandshake}, " +
                     $"totalRecvBytes={TotalRecvBytes}, totalSentBytes={TotalSentBytes}");
            _stop_heartbeat();
            if (connect_complete_tcs != null)
            {
                try
                {
                    if (!connect_complete_tcs.Task.IsCompleted)
                    {
                        var ws = this.websocket;
                        this.websocket = null;
                        if (ws != null)
                        {
                            ws.OnError -= Websocket_OnError;
                            ws.OnMessage -= Websocket_OnMessage;
                            ws.OnClose -= Websocket_OnClose;
                            ws.OnOpen -= Websocket_OnOpen;
                            try
                            {
                                ws.CancelConnection();
                            }
                            catch { }
                            try
                            {
                                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting)
                                {
                                    ws.Close();
                                }
                            }
                            catch { }
                        }
                        if (err != null)
                        {
                            connect_complete_tcs.TrySetException(err);
                        }
                        else
                        {
                            connect_complete_tcs.TrySetCanceled();
                        }
                    }
                }
                finally
                {
                    connect_complete_tcs = null;
                    try { done?.Invoke(); }
                    catch (Exception err3) { err3.PrintStackTrace(); }
                }
                return false;
            }
            if (err != null)
            {
                this.OnError?.Invoke(err);
            }
            {
                var ws = this.websocket;
                this.websocket = null;
                if (ws != null)
                {
                    try
                    {
                        ws.OnError -= Websocket_OnError;
                        ws.OnMessage -= Websocket_OnMessage;
                        ws.OnClose -= Websocket_OnClose;
                        ws.OnOpen -= Websocket_OnOpen;
                        try
                        {
                            ws.CancelConnection();
                        }
                        catch { }
                        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting)
                        {
                            try
                            {
                                ws.Close().ContinueWith(t =>
                                {
                                    try { done?.Invoke(); }
                                    catch (Exception err3) { err3.PrintStackTrace(); }
                                    try { OnDisconnected?.Invoke(reason, message); }
                                    catch (Exception err3) { err3.PrintStackTrace(); }
                                });
                            }
                            catch (Exception err2)
                            {
                                err2.PrintStackTrace();
                                try { done?.Invoke(); }
                                catch (Exception err3) { err3.PrintStackTrace(); }
                                try { OnDisconnected?.Invoke(reason, message); }
                                catch (Exception err3) { err3.PrintStackTrace(); }
                            }
                        }
                        else
                        {
                            try { done?.Invoke(); }
                            catch (Exception err3) { err3.PrintStackTrace(); }
                            try { OnDisconnected?.Invoke(reason, message); }
                            catch (Exception err3) { err3.PrintStackTrace(); }
                        }
                    }
                    catch
                    {
                        try { done?.Invoke(); }
                        catch (Exception err3) { err3.PrintStackTrace(); }
                        try { OnDisconnected?.Invoke(reason, message); }
                        catch (Exception err3) { err3.PrintStackTrace(); }
                    }
                    finally
                    {
                        try
                        {
                            ws.Dispose();
                        }
                        catch { }
                    }
                    return true;
                }
                else
                {
                    try { done?.Invoke(); }
                    catch (Exception err3) { err3.PrintStackTrace(); }
                }
            }
            return false;
        }

        private async Task _start_send(ISerializable msg, MessageType msgType, uint send_id)
        {
            var send_object = msg_pool.AllocSend();
            try
            {
                send_object.InitWithMessage(msgType, send_id, msg);
                await _start_send(send_object);
            }
            finally
            {
                if (ENABLE_SENDING_POOL) client.TaskQueue.Enqueue(send_object, static (r) => { r.Dispose(); });
            }
        }
        private async Task _start_send(BinaryMessage msg, MessageType msgType, uint send_id)
        {
            var send_object = msg_pool.AllocSend();
            try
            {
                send_object.InitWithMessage(msgType, send_id, msg);
                await _start_send(send_object);
            }
            finally
            {
                if (ENABLE_SENDING_POOL) client.TaskQueue.Enqueue(send_object, static (r) => { r.Dispose(); });
            }
        }

        private async Task _start_send(SendMessage send_object)
        {
            int len = send_object.BufferLength;
            await websocket.Send(new ArraySegment<byte>(send_object.Buffer, 0, len));
            this.OnSentMessage?.Invoke(send_object);
            this.TotalSentBytes += len;
        }

        private void _received_package_(MessagePool.RecvMessage recv_object)
        {
            switch (recv_object.PkgType)
            {
                case PackageType.PKG_HANDSHAKE_ACK:
                    _received_handshake(recv_object);
                    break;
                case PackageType.PKG_HEARTBEAT:
                    _received_heartbeat(recv_object);
                    break;
                case PackageType.PKG_MESSAGE:
                    _received_message(recv_object);
                    break;
                case PackageType.PKG_KICK:
                    _received_kick(recv_object);
                    break;
            }
            OnReceivedMessage?.Invoke(recv_object);
        }
        private void _received_message(MessagePool.RecvMessage recv_object)
        {
            recv_object.BeginBody();
            recv_object.BufferPosition = recv_object.BodyStartPistion;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private SystemTimeInterval<WSWebSocketAdapter> heartbeat_timer;
        private double last_heartbeat_r2c = CUtils.TickTimeMS;
        private double last_heartbeat_c2r = CUtils.TickTimeMS;
        private double last_heartbeat_chk = CUtils.TickTimeMS;
        private int heartbeat_interval_ms = 3000;
        private async Task _send_handshake(ISerializable user)
        {
            log.Info("send handshake!");
            var send_object = msg_pool.AllocSend();
            try
            {
                send_object.InitWithSystemMessage(new SystemHandshake() { user = user });
                await _start_send(send_object);
            }
            finally
            {
                if (ENABLE_SENDING_POOL) client.TaskQueue.Enqueue(send_object, static (r) => { r.Dispose(); });
            }
        }
        private void _received_handshake(MessagePool.RecvMessage recv_object)
        {
            log.Info("received handshake!");
            IsHandshake = true;
            var sysmsg = recv_object.ReadBodySystemMessage();
            if (sysmsg is SystemHandshakeAck)
            {
                var ack = sysmsg as SystemHandshakeAck;
                var token = ack.token;
                _init_heartbeat(ack.heartbeat_interval_ms);
                OnConnected?.Invoke(ack, token);
                if (connect_complete_tcs != null)
                {
                    connect_complete_tcs.TrySetResult(token);
                }
            }
            else
            {
                _run_close(CloseReason.KickByServer);
            }
        }
        private void _init_heartbeat(int intervalMS)
        {
            this.heartbeat_interval_ms = Math.Max(1000, intervalMS);
            this.last_heartbeat_r2c = CUtils.TickTimeMS;
            this.last_heartbeat_c2r = CUtils.TickTimeMS;
            this.last_heartbeat_chk = CUtils.TickTimeMS;
            if (intervalMS > 0)
            {
                log.InfoFormat("start heartbeat : {0} ms", intervalMS);
                this.heartbeat_timer = new SystemTimeInterval<WSWebSocketAdapter>().Init(heartbeat_interval_ms, this);
            }
            else
            {
                this.heartbeat_timer = null;
            }
        }
        private async Task _send_heartbeat()
        {
            var send_object = msg_pool.AllocSend();
            try
            {
                log.Info("send heartbeat");
                var ctime = CUtils.TickTimeMS;
                last_heartbeat_c2r = ctime;
                send_object.InitWithSystemMessage(new SystemHeartbeat() { time = ctime });
                await _start_send(send_object);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
            finally
            {
                if (ENABLE_SENDING_POOL) client.TaskQueue.Enqueue(send_object, static (r) => { r.Dispose(); });
            }
        }
        private void _received_heartbeat(MessagePool.RecvMessage recv_object)
        {
            log.Info("received heartbeat");
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemHeartbeat;
            var ctime = CUtils.TickTimeMS;
            if (sysmsg != null)
            {
                this.Ping = (int)(ctime - sysmsg.time);
            }
            last_heartbeat_r2c = ctime;
        }
        private void _stop_heartbeat()
        {
            var timer = this.heartbeat_timer;
            this.heartbeat_timer = null;
            if (timer != null)
            {
                log.Info("stop heartbeat");
            }
        }
        private void _check_heartbeat()
        {
            var timer = this.heartbeat_timer;
            if (timer != null && timer.Update())
            {
                //var so = websocket;
                //if (so != null && so.State == WebSocketState.Open)
                {
                    log.Info($"check heartbeat : ws state {websocket?.State}");
                    var curtime = CUtils.TickTimeMS;
                    int tick = (int)(curtime - last_heartbeat_chk);
                    last_heartbeat_chk = curtime;
                    //                     if ((curtime - last_heartbeat_r2c) > heartbeat_interval_ms * 4)
                    //                     {
                    //                         _run_close(CloseReason.TimeOut);
                    //                     }
                    //                     else                 
                    _send_heartbeat().Forget();                  
                }
            }
        }
        private void _received_kick(MessagePool.RecvMessage recv_object)
        {
            log.Info("received kick!");
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemKick;
            _run_close(CloseReason.KickByServer, null, sysmsg?.reason);
        }
        //-------------------------------------------------------------------------------------------------------------------------------

    }
}
#endif
