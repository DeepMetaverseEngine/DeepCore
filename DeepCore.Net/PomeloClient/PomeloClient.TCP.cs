using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static DeepCore.NetClient.MessagePool;

namespace DeepCore.PomeloClient
{
    public class PomeloTCP : IClientAdapter
    {
        protected Logger log => client.log;
        protected INetClient client;
        protected MessagePool msg_pool;


        private long total_recv_bytes;
        private long total_sent_bytes;
        private int current_ping;
        private bool is_handshake = false;
        private DateTime last_connect_time;
        private string host;

        private TcpClient tcp;

        private class ConnectingInfo : Disposable
        {
            public readonly PomeloTCP adapter;
            public readonly IPAddress[] _addrs;
            public readonly int _port;
            public readonly ISerializable _user;
            public readonly TcpClient _tcp;
            private readonly AtomicReference<Action<Exception, ISerializable>> _callback;

            public ConnectingInfo(
                PomeloTCP adapter,
                IPAddress[] addrs,
                int port,
                ISerializable user,
                TcpClient tcp,
                Action<Exception, ISerializable> callback)
            {
                this.adapter = adapter;
                this._addrs = addrs;
                this._port = port;
                this._user = user;
                this._tcp = tcp;
                this._callback = new(callback);
            }
            protected override void Disposing()
            {
                this._callback.Value = (null);
            }
            public override string ToString()
            {
                return string.Format("host={0} port={1}", CUtils.ArrayToString(_addrs), _port);
            }
            public void on_close(CloseReason reason, string message, Exception err)
            {
                var cb = this._callback.GetAndSet(null);
                if (cb != null)
                {
                    adapter.log.Warn("_run_close : _callback");
                    var exp = err != null ? new Exception($"{reason} : {message}", err) : new Exception($"{reason} : {message}");
                    adapter.client.TaskQueue.Enqueue(exp, (p, err) =>
                    {
                        adapter.log.Warn("_run_close : main _callback");
                        cb(err, null);
                    });
                }
            }
            public void on_finish(ISerializable token)
            {
                var cb = this._callback.GetAndSet(null);
                if (cb != null)
                {
                    adapter.client.TaskQueue.Enqueue((token, cb), static (st) => st.cb(null, st.token));
                }
            }
        }
        private ConnectingInfo conn_info = null;

        //--------------------------------------------------------------------------------------------------
        private readonly bool use_async_rw;
        public PomeloTCP(INetClient client, bool use_async_rw)
        {
            this.use_async_rw = use_async_rw;
            this.client = client;
            this.msg_pool = new MessagePool(client.Codec);
        }
        public long TotalRecvBytes
        {
            get { return total_recv_bytes; }
        }
        public long TotalSentBytes
        {
            get { return total_sent_bytes; }
        }
        public bool IsConnected
        {
            get { lock (this) { return tcp != null && tcp.Connected; } }
        }
        public bool IsHandshake
        {
            get { return is_handshake; }
        }
        public Socket Client
        {
            get { lock (this) { return tcp != null ? tcp.Client : null; } }
        }
        public int Ping
        {
            get { return current_ping; }
        }
        public DateTime ConnectTime
        {
            get { return last_connect_time; }
        }
        public TcpClient GetSocket()
        {
            lock (this) { return tcp; }
        }
        public override string ToString()
        {
            return "PomeloTCP:" + host;
        }
        //--------------------------------------------------------------------------------------------------
        #region --Implements--

        private Action<IRecvMessage> event_OnReceivedMessage;
        private Action<ISendMessage> event_OnSentMessage;
        private Action<Exception> event_OnError;
        private Action<CloseReason, string> event_OnDisconnected;
        private Action<SystemHandshakeAck, ISerializable> event_OnConnected;

        public event Action<IRecvMessage> OnReceivedMessage { add { event_OnReceivedMessage += value; } remove { event_OnReceivedMessage -= value; } }
        public event Action<ISendMessage> OnSentMessage { add { event_OnSentMessage += value; } remove { event_OnSentMessage -= value; } }
        public event Action<Exception> OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
        public event Action<CloseReason, string> OnDisconnected { add { event_OnDisconnected += value; } remove { event_OnDisconnected -= value; } }
        public event Action<SystemHandshakeAck, ISerializable> OnConnected { add { event_OnConnected += value; } remove { event_OnConnected -= value; } }

        public virtual bool Connect(string address, int timeout, ISerializable user, Action<Exception, ISerializable> callback)
        {
            this.host = address;
            AddressFamily family;
            IPHostEntry ips;
            TcpClient so;
            var cfg = PomeloClientFactory.Config;
            IPUtil.TryParseHostPort(address, out var host, out var port);
            var addrs = IPUtil.GetIPAddress(host, port, out family, out ips);
            lock (this)
            {
                if (tcp != null)
                {
                    log.Warn("the socket already connected!");
                    return false;
                }
                so = this.tcp = new TcpClient(family);
            }
            so.SendTimeout = timeout;
            so.ReceiveTimeout = timeout;
            so.NoDelay = cfg.NoDelay;
            so.ReceiveBufferSize = cfg.BufferSize;
            so.SendBufferSize = cfg.BufferSize;
            so.Client.Blocking = true;
            var conn = conn_info = new ConnectingInfo(this, addrs, port, user, so, callback);
            this._start_connect(conn, timeout);
            return true;
        }
        public virtual Task<ISerializable> ConnectAsync(string address, int timeout, ISerializable user)
        {
            var tcs = new TaskCompletionSource<ISerializable>();
            Connect(address, timeout, user, (err, rsp) =>
            {
                if (err != null)
                {
                    tcs.TrySetException(err);
                }
                else
                {
                    tcs.TrySetResult(rsp);
                }
            });
            return tcs.Task;
        }

        public virtual bool Disconnect(Action action)
        {
            if (_run_close(tcp, CloseReason.ClientClose, "the socket already disconnected!", null) == false)
            {
                action();
                return false;
            }
            else
            {
                action();
                return true;
            }
        }

        public virtual bool Send(ISerializable msg, MessageType msgType, uint send_id)
        {
            var so = GetSocket();
            if (so != null && so.Connected)
            {
                var send = msg_pool.AllocSend();
                try
                {
                    send.InitWithMessage(msgType, send_id, msg);
                    return _start_send(so, send as MessagePool.SendMessage);
                }
                catch
                {
                    send.Dispose();
                }
            }
            return false;
        }
        public virtual bool Send(BinaryMessage msg, MessageType msgType, uint send_id)
        {
            var so = GetSocket();
            if (so != null && so.Connected)
            {
                var send = msg_pool.AllocSend();
                try
                {
                    send.InitWithMessage(msgType, send_id, msg);
                    return _start_send(so, send as MessagePool.SendMessage);
                }
                catch
                {
                    send.Dispose();
                }
            }
            return false;
        }
        public virtual void Update()
        {
            _check_heartbeat();
        }
        public virtual void Dispose()
        {
            conn_info?.Dispose();
            _run_close(tcp, CloseReason.ClientClose, null, null);
            msg_pool?.Dispose();
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
        #region --Internal--

        private void _start_connect(ConnectingInfo info, int timeout)
        {
            this.is_handshake = false;
            var so = info._tcp;
            try
            {
                log.InfoFormat("begin connect : {0}", info);
                if (use_async_rw)
                {
                    var task = so.ConnectAsync(info._addrs, info._port).ContinueWith(static (t, state) =>
                    {
                        var info = state as ConnectingInfo;
                        var adapter = info.adapter;
                        adapter.log.InfoFormat("end connect : {0}", info);
                        var so = info._tcp;
                        try
                        {
                            if (t.IsFaulted)
                            {
                                adapter._run_close(so, CloseReason.Disconnect, t.Exception.Message, t.Exception);
                                return;
                            }
                            else if (t.IsCompleted)
                            {
                                if (so.Connected)
                                {
                                    adapter.last_connect_time = DateTime.Now;
                                    adapter._start_receive_head(so);
                                    adapter._send_handshake(so, info._user);
                                }
                                else
                                {
                                    adapter._run_close(so, CloseReason.TimeOut);
                                }
                            }
                            else
                            {
                                adapter._run_close(so, CloseReason.TimeOut);
                            }
                        }
                        catch (SocketException err)
                        {
                            if (err.SocketErrorCode == SocketError.TimedOut)
                            {
                                adapter._on_error(so, CloseReason.TimeOut, err);
                            }
                            else
                            {
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }
                        catch (Exception err)
                        {
                            adapter._on_error(so, CloseReason.Error, err);
                        }
                    }, info);
                    Task.Run(() =>
                    {
                        try
                        {
                            if (task.Wait(timeout))
                            {
                                Task.Delay(100).Wait(); // 确保连接完成
                                log.InfoFormat("begin connect WaitOne : {0}:{1}", info._addrs, info._port);
                                if (!so.Connected)
                                {
                                    log.Warn("WaitOne _run_close");
                                    _run_close(so, CloseReason.TimeOut, "Timeout");
                                }
                                else
                                {
                                    log.Info("begin connect WaitOne : success");
                                }
                            }
                            else
                            {
                                log.Warn("WaitOne false");
                                _run_close(so, CloseReason.TimeOut, "Timeout");
                            }
                        }
                        catch (Exception err)
                        {
                            _run_close(so, CloseReason.TimeOut, err.Message, err);
                        }
                    });
                }
                else
                {
                    var result = so.BeginConnect(info._addrs, info._port, static result =>
                    {
                        var info = result.AsyncState as ConnectingInfo;
                        var adapter = info.adapter;
                        adapter.log.InfoFormat("end connect : {0}", info);
                        var so = info._tcp;
                        try
                        {
                            so.EndConnect(result);
                            if (so.Connected)
                            {
                                adapter.last_connect_time = DateTime.Now;
                                adapter._start_receive_head(so);
                                adapter._send_handshake(so, info._user);
                            }
                            else
                            {
                                adapter._run_close(so, CloseReason.TimeOut);
                            }
                        }
                        catch (SocketException err)
                        {
                            if (err.SocketErrorCode == SocketError.TimedOut)
                            {
                                adapter._on_error(so, CloseReason.TimeOut, err);
                            }
                            else
                            {
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }
                        catch (Exception err)
                        {
                            adapter._on_error(so, CloseReason.Error, err);
                        }
                    }, info);
                    Task.Run(() =>
                    {
                        try
                        {
                            if (result.AsyncWaitHandle.WaitOne(timeout))
                            {
                                log.InfoFormat("begin connect WaitOne : {0}:{1}", info._addrs, info._port);
                                if (!so.Connected)
                                {
                                    log.Warn("WaitOne _run_close");
                                    _run_close(so, CloseReason.TimeOut, "Timeout");
                                }
                                else
                                {
                                    log.Info("begin connect WaitOne : success");
                                }
                            }
                            else
                            {
                                log.Warn("WaitOne false");
                                _run_close(so, CloseReason.TimeOut, "Timeout");
                            }
                        }
                        catch (Exception err)
                        {
                            _run_close(so, CloseReason.TimeOut, err.Message, err);
                        }
                    });
                }
            }
            catch (Exception err)
            {
                _on_error(so, CloseReason.Error, err);
            }
        }
        private bool _run_close(TcpClient s, CloseReason reason, string message = null, Exception err = null)
        {
            this.is_handshake = false;
            if (s != null)
            {
                conn_info?.on_close(reason, message, err);
                bool post_event = false;
                lock (this)
                {
                    if (s == tcp)
                    {
                        tcp = null;
                        post_event = true;
                    }
                }
                _stop_heartbeat();
                {
                    if (s.Client != null)
                    {
                        try { log.InfoFormat("closing : {0}", s.Client.RemoteEndPoint); } catch { }
                        try { s.Client.Shutdown(SocketShutdown.Both); } catch { }
                    }
                    try
                    {
                        s.Close();
                    }
                    catch { }
                    log.InfoFormat($"disconnected : {reason} : {message}");
                    if (post_event) event_OnDisconnected?.Invoke(reason, message);
                    return true;
                }
            }
            return false;
        }

        private void _on_error(TcpClient s, CloseReason reason, Exception err)
        {
            log.Error(err.Message + " : " + host, err);
            if (err.InnerException is SocketException && reason == CloseReason.Error)
            {
                reason = CloseReason.Disconnect;
            }
            _run_close(s, reason, err.Message, err);
            event_OnError?.Invoke(err);
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
            event_OnReceivedMessage?.Invoke(recv_object);
        }

        private void _received_message(MessagePool.RecvMessage recv_object)
        {
            recv_object.BeginBody();
            recv_object.BufferPosition = recv_object.BodyStartPistion;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
        #region --recv-and-send--

        private void _start_receive_head(TcpClient so, MessagePool.RecvMessage recv_object = null)
        {
            try
            {
                //log.Info("_start_receive_head");
                if (!so.Connected)
                {
                    if (recv_object != null) recv_object.Dispose();
                    _run_close(so, CloseReason.Disconnect);
                    return;
                }
                if (recv_object == null)
                {
                    recv_object = msg_pool.AllocRecv();
                    recv_object.token = so;
                    recv_object.adapter = this;
                }
                if (use_async_rw)
                {
                    so.GetStream().ReadAsync(
                        recv_object.Buffer,
                        recv_object.BufferPosition,
                        recv_object.BufferLength - recv_object.BufferPosition).ContinueWith(static (t, state) =>
                        {
                            var recv_object = state as MessagePool.RecvMessage;
                            var adapter = recv_object.adapter as PomeloTCP;
                            var so = recv_object.token as TcpClient;
                            try
                            {
                                if (t.IsFaulted)
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect, t.Exception.Message, t.Exception);
                                    return;
                                }
                                else if (t.IsCompleted)
                                {
                                    int length = t.Result;
                                    //adapter.log.Info("_end_receive_head");
                                    if (!so.Connected)
                                    {
                                        recv_object.Dispose();
                                        adapter._run_close(so, CloseReason.Disconnect);
                                        return;
                                    }
                                    if (length > 0)
                                    {
                                        adapter.total_recv_bytes += length;
                                        recv_object.BufferPosition += length;
                                        if (recv_object.BufferPosition == IRecvMessage.FIXED_HEAD_SIZE)
                                        {
                                            recv_object.ReadHead();
                                            if (recv_object.PkgLength > 0)
                                            {
                                                adapter._start_receive_body(recv_object);
                                            }
                                            else
                                            {
                                                adapter._received_package_(recv_object);
                                                recv_object.Dispose();
                                                recv_object = null;
                                                adapter._start_receive_head(so);
                                            }
                                        }
                                        else if (recv_object.BufferPosition > IRecvMessage.FIXED_HEAD_SIZE)
                                        {
                                            throw new Exception("endReceiveHead : Receive head overfollow");
                                        }
                                        else
                                        {
                                            adapter._start_receive_head(so, recv_object);
                                        }
                                    }
                                    else
                                    {
                                        recv_object.Dispose();
                                        adapter._run_close(so, CloseReason.Disconnect);
                                    }
                                }
                                else
                                {
                                    recv_object.Dispose();
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                if (recv_object != null) recv_object.Dispose();
                            }
                            catch (IOException e2)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                            }
                            catch (SocketException e3)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                            }
                            catch (Exception err)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }, recv_object);
                }
                else
                {
                    so.GetStream().BeginRead(
                        recv_object.Buffer,
                        recv_object.BufferPosition,
                        recv_object.BufferLength - recv_object.BufferPosition,
                        static (result) =>
                        {
                            var recv_object = result.AsyncState as MessagePool.RecvMessage;
                            var adapter = recv_object.adapter as PomeloTCP;
                            var so = recv_object.token as TcpClient;
                            try
                            {
                                int length = so.GetStream().EndRead(result);
                                //adapter.log.Info("_end_receive_head");
                                if (!so.Connected)
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect);
                                    return;
                                }
                                if (length > 0)
                                {
                                    adapter.total_recv_bytes += length;
                                    recv_object.BufferPosition += length;
                                    if (recv_object.BufferPosition == IRecvMessage.FIXED_HEAD_SIZE)
                                    {
                                        recv_object.ReadHead();
                                        if (recv_object.PkgLength > 0)
                                        {
                                            adapter._start_receive_body(recv_object);
                                        }
                                        else
                                        {
                                            adapter._received_package_(recv_object);
                                            recv_object.Dispose();
                                            recv_object = null;
                                            adapter._start_receive_head(so);
                                        }
                                    }
                                    else if (recv_object.BufferPosition > IRecvMessage.FIXED_HEAD_SIZE)
                                    {
                                        throw new Exception("endReceiveHead : Receive head overfollow");
                                    }
                                    else
                                    {
                                        adapter._start_receive_head(so, recv_object);
                                    }
                                }
                                else
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect);
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                if (recv_object != null) recv_object.Dispose();
                            }
                            catch (IOException e2)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                            }
                            catch (SocketException e3)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                            }
                            catch (Exception err)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }, recv_object);
                }
            }
            catch (Exception err)
            {
                if (recv_object != null) recv_object.Dispose();
                _on_error(so, CloseReason.Error, err);
            }
        }

        private void _start_receive_body(MessagePool.RecvMessage recv_object)
        {
            var so = recv_object.token as TcpClient;
            try
            {
                //log.Info("_start_receive_body");
                if (!so.Connected)
                {
                    recv_object.Dispose();
                    _run_close(so, CloseReason.Disconnect);
                    return;
                }
                if (use_async_rw)
                {
                    so.GetStream().ReadAsync(
                        recv_object.Buffer,
                        recv_object.BufferPosition,
                        recv_object.BufferLength - recv_object.BufferPosition).ContinueWith(static (t, state) =>
                        {
                            var recv_object = state as MessagePool.RecvMessage;
                            var adapter = recv_object.adapter as PomeloTCP;
                            var so = recv_object.token as TcpClient;
                            try
                            {
                                if (t.IsFaulted)
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect, t.Exception.Message, t.Exception);
                                    return;
                                }
                                else if (t.IsCompleted)
                                {
                                    int length = t.Result;
                                    //adapter.log.Info("_end_receive_body");
                                    if (!so.Connected)
                                    {
                                        recv_object.Dispose();
                                        adapter._run_close(so, CloseReason.Disconnect);
                                        return;
                                    }
                                    if (length > 0)
                                    {
                                        adapter.total_recv_bytes += length;
                                        recv_object.BufferPosition += length;
                                        if (recv_object.BufferPosition == recv_object.BufferLength)
                                        {
                                            adapter._received_package_(recv_object);
                                            recv_object.Dispose();
                                            recv_object = null;
                                            adapter._start_receive_head(so);
                                        }
                                        else if (recv_object.BufferPosition > recv_object.BufferLength)
                                        {
                                            throw new Exception("endReceiveBody : Receive body overfollow");
                                        }
                                        else
                                        {
                                            adapter._start_receive_body(recv_object);
                                        }
                                    }
                                    else
                                    {
                                        recv_object.Dispose();
                                        adapter._run_close(so, CloseReason.Disconnect);
                                    }
                                }
                                else
                                {
                                    recv_object.Dispose();
                                }
                            }
                            catch (IOException e2)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                            }
                            catch (SocketException e3)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                            }
                            catch (Exception err)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }, recv_object);
                }
                else
                {
                    so.GetStream().BeginRead(
                        recv_object.Buffer,
                        recv_object.BufferPosition,
                        recv_object.BufferLength - recv_object.BufferPosition,
                        static result =>
                        {
                            var recv_object = result.AsyncState as MessagePool.RecvMessage;
                            var adapter = recv_object.adapter as PomeloTCP;
                            var so = recv_object.token as TcpClient;
                            try
                            {
                                int length = so.GetStream().EndRead(result);
                                //adapter.log.Info("_end_receive_body");
                                if (!so.Connected)
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect);
                                    return;
                                }
                                if (length > 0)
                                {
                                    adapter.total_recv_bytes += length;
                                    recv_object.BufferPosition += length;
                                    if (recv_object.BufferPosition == recv_object.BufferLength)
                                    {
                                        adapter._received_package_(recv_object);
                                        recv_object.Dispose();
                                        recv_object = null;
                                        adapter._start_receive_head(so);
                                    }
                                    else if (recv_object.BufferPosition > recv_object.BufferLength)
                                    {
                                        throw new Exception("endReceiveBody : Receive body overfollow");
                                    }
                                    else
                                    {
                                        adapter._start_receive_body(recv_object);
                                    }
                                }
                                else
                                {
                                    recv_object.Dispose();
                                    adapter._run_close(so, CloseReason.Disconnect);
                                }
                            }
                            catch (IOException e2)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                            }
                            catch (SocketException e3)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                            }
                            catch (Exception err)
                            {
                                if (recv_object != null) recv_object.Dispose();
                                adapter._on_error(so, CloseReason.Error, err);
                            }
                        }, recv_object);
                }
            }
            catch (Exception err)
            {
                recv_object.Dispose();
                _on_error(so, CloseReason.Error, err);
            }
        }


        //--------------------------------------------------------------------------------------------------


        private bool _start_send(TcpClient so, SendMessage send_object, Action<SendMessage> cb = null)
        {
            try
            {
                if (send_object.PkgLength >= PomeloClientFactory.Config.MaxPackageSize)
                {
                    throw new Exception(string.Format("PkgLength:{0} out of limit:{1} {2}",
                        send_object.PkgLength,
                        PomeloClientFactory.Config.MaxPackageSize,
                        send_object));
                }
                send_object.token = so;
                send_object.adapter = this;
                send_object.callback = cb;
                int len = send_object.BufferLength;
                var stream = so.GetStream();
                if (use_async_rw)
                {
                    stream.WriteAsync(send_object.Buffer, 0, len).ContinueWith(static (t, state) =>
                    {
                        var send_object = state as SendMessage;
                        var adapter = send_object.adapter as PomeloTCP;
                        var cb = send_object.callback;
                        var so = send_object?.token as TcpClient;
                        try
                        {
                            if (t.IsFaulted)
                            {
                                adapter._run_close(so, CloseReason.Disconnect, t.Exception.Message, t.Exception);
                                return;
                            }
                            else if (t.IsCompleted)
                            {
                                int len = send_object.BufferLength;
                                adapter.total_sent_bytes += len;
                                cb?.Invoke(send_object);
                                adapter.event_OnSentMessage?.Invoke(send_object);
                            }
                            else
                            {

                            }
                        }
                        catch (IOException e2)
                        {
                            adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                        }
                        catch (SocketException e3)
                        {
                            adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                        }
                        catch (Exception err)
                        {
                            adapter._on_error(so, CloseReason.Error, err);
                        }
                        finally
                        {
                            send_object?.Dispose();
                        }
                    }, send_object);
                }
                else
                {
                    stream.BeginWrite(send_object.Buffer, 0, len, static (asyncSend) =>
                    {
                        var send_object = asyncSend.AsyncState as SendMessage;
                        var adapter = send_object.adapter as PomeloTCP;
                        var cb = send_object.callback;
                        var so = send_object?.token as TcpClient;
                        try
                        {
                            so.GetStream().EndWrite(asyncSend);
                            int len = send_object.BufferLength;
                            adapter.total_sent_bytes += len;
                            cb?.Invoke(send_object);
                            adapter.event_OnSentMessage?.Invoke(send_object);
                        }
                        catch (IOException e2)
                        {
                            adapter._run_close(so, CloseReason.Disconnect, e2.Message, e2);
                        }
                        catch (SocketException e3)
                        {
                            adapter._run_close(so, CloseReason.Disconnect, e3.Message, e3);
                        }
                        catch (Exception err)
                        {
                            adapter._on_error(so, CloseReason.Error, err);
                        }
                        finally
                        {
                            send_object?.Dispose();
                        }
                    }, send_object);
                }
                return true;
            }
            catch (Exception err)
            {
                send_object.Dispose();
                _on_error(so, CloseReason.Error, err);
            }
            return false;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
        #region --SystemMessage--

        private readonly AtomicReference<SystemTimeInterval<TcpClient>> heartbeat_timer = new AtomicReference<SystemTimeInterval<TcpClient>>(null);
        private double last_heartbeat_r2c = CUtils.TickTimeMS;
        private double last_heartbeat_c2r = CUtils.TickTimeMS;
        private double last_heartbeat_chk = CUtils.TickTimeMS;
        private int heartbeat_interval_ms = 3000;

        private void _send_handshake(TcpClient so, ISerializable user, Action<SendMessage> cb = null)
        {
            log.Info("send handshake!");
            var send_object = msg_pool.AllocSend();
            send_object.InitWithSystemMessage(new SystemHandshake() { user = user });
            _start_send(so, send_object, cb);
            so.GetStream().FlushAsync();
        }

        private void _received_handshake(MessagePool.RecvMessage recv_object)
        {
            log.Info("received handshake!");
            is_handshake = true;
            var sysmsg = recv_object.ReadBodySystemMessage();
            var so = recv_object.token as TcpClient;
            try
            {
                if (sysmsg is SystemHandshakeAck)
                {
                    var ack = sysmsg as SystemHandshakeAck;
                    var token = ack.token;
                    _init_heartbeat(so, ack.heartbeat_interval_ms);
                    event_OnConnected?.Invoke(ack, token);
                    conn_info?.on_finish(token);
                }
                else
                {
                    _run_close(so, CloseReason.KickByServer);
                }
            }
            finally
            {
            }
        }

        private void _init_heartbeat(TcpClient so, int intervalMS)
        {
            this.heartbeat_interval_ms = Math.Max(1000, intervalMS);
            this.last_heartbeat_r2c = CUtils.TickTimeMS;
            this.last_heartbeat_c2r = CUtils.TickTimeMS;
            this.last_heartbeat_chk = CUtils.TickTimeMS;
            if (intervalMS > 0)
            {
                log.InfoFormat("start heartbeat : {0} ms", intervalMS);
                this.heartbeat_timer.Value = new SystemTimeInterval<TcpClient>().Init(heartbeat_interval_ms, so);
            }
            else
            {
                this.heartbeat_timer.Value = null;
            }
        }
        private void _stop_heartbeat()
        {
            var timer = this.heartbeat_timer.GetAndSet(null);
            if (timer != null)
            {
                log.Info("stop heartbeat");
            }
        }
        private void _send_heartbeat(TcpClient so)
        {
            var send_object = msg_pool.AllocSend();
            var ctime = CUtils.TickTimeMS;
            last_heartbeat_c2r = ctime;
            send_object.InitWithSystemMessage(new SystemHeartbeat() { time = ctime });
            _start_send(so, send_object);
        }
        private void _received_heartbeat(MessagePool.RecvMessage recv_object)
        {
            //log.Debug("received heartbeat");
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemHeartbeat;
            var ctime = CUtils.TickTimeMS;
            if (sysmsg != null)
            {
                current_ping = (int)(ctime - sysmsg.time);
            }
            last_heartbeat_r2c = ctime;
        }
        private void _received_kick(MessagePool.RecvMessage recv_object)
        {
            var so = recv_object.token as TcpClient;
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemKick;
            _run_close(so, CloseReason.KickByServer, sysmsg?.reason);
        }
        private void _check_heartbeat()
        {
            var timer = this.heartbeat_timer.Value;
            if (timer != null && timer.Update())
            {
                var so = timer.Tag;
                if (so != null && so.Connected)
                {
                    //log.Debug("check heartbeat");
                    var curtime = CUtils.TickTimeMS;
                    int tick = (int)(curtime - last_heartbeat_chk);
                    last_heartbeat_chk = curtime;
                    if ((curtime - last_heartbeat_r2c) > heartbeat_interval_ms * 4)
                    {
                        _run_close(so, CloseReason.TimeOut);
                    }
                    else
                    {
                        _send_heartbeat(so);
                    }
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------

    }
}
