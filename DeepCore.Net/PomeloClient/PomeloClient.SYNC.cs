using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.NetClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.PomeloClient
{
    public class PomeloSYN : IClientAdapter
    {
        protected Logger log => client.log;
        protected INetClient client;
        protected  MessagePool msg_pool;
        private long total_recv_bytes;
        private long total_sent_bytes;
        private int current_ping;
        private bool is_handshake = false;
        private bool is_active = false;
        private DateTime last_connect_time;
        private string host;
        private TcpClient tcp;
        private ISerializable connecting_user;
        private Action<Exception, ISerializable> connecting_callback;

        public PomeloSYN(INetClient client)
        {
            this.client = client;
            this.msg_pool = new MessagePool(client.Codec);
        }
        public long TotalRecvBytes => total_recv_bytes;
        public long TotalSentBytes => total_sent_bytes;
        public bool IsConnected { get { lock (this) { return tcp != null && tcp.Connected; } } }
        public bool IsHandshake => is_handshake;
        public Socket Client { get { lock (this) { return tcp != null ? tcp.Client : null; } } }
        public int Ping => current_ping;
        public DateTime ConnectTime => last_connect_time;
        public TcpClient GetSocket() { lock (this) { return tcp; } }
        public override string ToString() { return "PomeloSYN:" + host; }

        //--------------------------------------------------------------------------------------------------------------
        #region ----Implements----

        public event Action<IRecvMessage> OnReceivedMessage;
        public event Action<ISendMessage> OnSentMessage;
        public event Action<Exception> OnError;
        public event Action<CloseReason, string> OnDisconnected;
        public event Action<SystemHandshakeAck, ISerializable> OnConnected;

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
                this.connecting_user = user;
                this.connecting_callback = callback;
                this.tcp = new TcpClient(family);
                this.is_active = true;
                this.is_handshake = false;
                so = tcp;
            }
            so.SendTimeout = timeout;
            so.ReceiveTimeout = timeout;
            so.NoDelay = cfg.NoDelay;
            so.ReceiveBufferSize = cfg.BufferSize;
            so.SendBufferSize = cfg.BufferSize;
            so.Client.Blocking = true;
            try
            {
                //lock (sending_queue) sending_queue.Clear();
                log.InfoFormat("begin connect : {0}:{1}", host, port);
                var result = so.BeginConnect(addrs, port, (IAsyncResult result) =>
                {
                    var info = result.AsyncState as PomeloSYN;
                    log.InfoFormat("end connect : {0}:{1}", host, port);
                    var so = info.tcp;
                    try
                    {
                        so.EndConnect(result);
                        if (so.Connected)
                        {
                            this.last_connect_time = DateTime.Now;
                            _send_handshake(info.connecting_user);
                            _run_start();
                        }
                        else
                        {
                            _run_close(CloseReason.TimeOut, null, new Exception("TimeOut"));
                        }
                    }
                    catch (SocketException err)
                    {
                        if (err.SocketErrorCode == SocketError.TimedOut)
                        {
                            _on_error(CloseReason.TimeOut, err);
                        }
                        else
                        {
                            _on_error(CloseReason.Error, err);
                        }
                    }
                    catch (Exception err)
                    {
                        _on_error(CloseReason.Error, err);
                    }
                }, this);
                Task.Run(() =>
                {
                    try
                    {
                        if (result.AsyncWaitHandle.WaitOne(timeout))
                        {
                            log.InfoFormat("begin connect WaitOne : {0}:{1}", addrs, port);
                            if (!so.Connected)
                            {
                                log.Warn("WaitOne _run_close");
                                _run_close(CloseReason.TimeOut, "Timeout", new Exception("Timeout"));
                            }
                            else
                            {
                                log.Info("begin connect WaitOne : success");
                            }
                        }
                        else
                        {
                            log.Warn("WaitOne false");
                            _run_close(CloseReason.TimeOut, "Timeout", new Exception("Timeout"));
                        }
                    }
                    catch (Exception err)
                    {
                        _run_close(CloseReason.TimeOut, err.Message, err);
                    }
                });
            }
            catch (Exception err)
            {
                _on_error(CloseReason.Error, err);
            }
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
            if (_run_close(CloseReason.ClientClose, "the socket already disconnected!", new Exception("Force Disconnect")) == false)
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
                    return _send_queue(send);
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
                    return _send_queue(send);
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
            _heartbeat_update();
        }
        public virtual void Dispose()
        {
            _run_close(CloseReason.ClientClose, string.Empty, new Exception("Dispose"));
            msg_pool?.Dispose();
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region ----INTERNAL----

        private void _on_error(CloseReason reason, Exception err)
        {
            log.Error(err.Message + " : " + host, err);
            if (err.InnerException is SocketException && reason == CloseReason.Error)
            {
                reason = CloseReason.Disconnect;
            }
            this.OnError?.Invoke(err);
            _run_close(reason, err.Message, err);
        }

        private void _run_start()
        {
            lock (this)
            {
                this.is_active = true;
            }
            _send_start();
            _receive_start();
        }

        private bool _run_close(CloseReason reason, string message, Exception err)
        {
            var cb = connecting_callback;
            if (cb != null)
            {
                connecting_callback = null;
                client.TaskQueue.Enqueue(err, (p, err) => cb(err, null));
            }
            this.is_handshake = false;
            _heartbeat_stop();
            var so = this.tcp;
            lock (this)
            {
                this.is_active = false;
                if (tcp == null)
                {
                    return false;
                }
                this.tcp = null;
            }
            try { log.InfoFormat("closing : {0}", so.Client.RemoteEndPoint); } catch { }
            try { so.Client.Shutdown(SocketShutdown.Both); } catch { }
            try { so.Close(); } catch { }
            try
            {
                if (thread_receive != null && Thread.CurrentThread.ManagedThreadId != thread_receive.ManagedThreadId)
                {
                    thread_receive.Join(1000);
                }
            }
            catch { }
            try
            {
                if (thread_send != null && Thread.CurrentThread.ManagedThreadId != thread_send.ManagedThreadId)
                {
                    thread_send.Join(1000);
                }
            }
            catch { }
            lock (async_queue) { async_queue.Clear(); Monitor.PulseAll(async_queue); }
            log.InfoFormat($"disconnected : {reason} : {message}");
            this.OnDisconnected?.Invoke(reason, message);
            return true;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region ----SEND----
        private Queue<MessagePool.SendMessage> async_queue = new Queue<MessagePool.SendMessage>();
        private System.Threading.Thread thread_send;
        private void _send_start()
        {
            thread_send = new System.Threading.Thread(_send_main);
            thread_send.Name = $"{nameof(PomeloSYN)}.{nameof(_send_main)}";
            thread_send.Start();
        }
        private void _send_main()
        {
            var sending = new List<MessagePool.SendMessage>();
            var tcp = this.tcp;
            while (is_active)
            {
                try
                {
                    if (!tcp.Connected) return;
                    lock (async_queue)
                    {
                        Monitor.Wait(async_queue, 100);
                        if (async_queue.Count > 0)
                        {
                            sending.AddRange(async_queue);
                            async_queue.Clear();
                        }
                    }
                    var stream = tcp.GetStream();
                    if (sending.Count > 0)
                    {
                        try
                        {
                            for (int i = 0; i < sending.Count; i++)
                            {
                                var send_object = sending[i];
                                int len = send_object.BufferLength;
                                stream.Write(send_object.Buffer, 0, len);
                                this.total_sent_bytes += len;
                            }
                            stream.Flush();
                        }
                        catch
                        {
                            is_active = false;
                        }
                        finally
                        {
                            for (int i = 0; i < sending.Count; i++)
                            {
                                OnSentMessage?.Invoke(sending[i]);
                                sending[i].Dispose();
                            }
                            sending.Clear();
                        }
                    }
                }
                catch
                {
                    is_active = false;
                }
            }
        }
        private bool _send_queue(MessagePool.SendMessage send_object)
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
                lock (async_queue)
                {
                    async_queue.Enqueue(send_object);
                    Monitor.PulseAll(async_queue);
                }
                return true;
            }
            catch (Exception err)
            {
                send_object.Dispose();
                _on_error(CloseReason.Error, err);
            }
            return false;
        }

        private void _send_handshake(ISerializable user)
        {
            log.Info("send handshake!");
            var send_object = msg_pool.AllocSend();
            send_object.InitWithSystemMessage(new SystemHandshake() { user = user });
            _send_queue(send_object);
        }

        private void _send_heartbeat(TcpClient so)
        {
            var send_object = msg_pool.AllocSend();
            var ctime = CUtils.TickTimeMS;
            last_heartbeat_c2r = ctime;
            send_object.InitWithSystemMessage(new SystemHeartbeat() { time = ctime });
            _send_queue(send_object);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region ----RECV----
        private System.Threading.Thread thread_receive;
        private void _receive_start()
        {
            thread_receive = new System.Threading.Thread(_receive_main);
            thread_receive.Name = $"{nameof(PomeloSYN)}.{nameof(_receive_main)}";
            thread_receive.Start();
        }
        private void _receive_main()
        {
            var tcp = this.tcp;
            while (is_active)
            {
                try
                {
                    if (!tcp.Connected)
                    {
                        _run_close(CloseReason.Disconnect, "Disconnected", new Exception($"Disconnected"));
                        return;
                    }
                    while (tcp.Available > 0)
                    {
                        try
                        {
                            if (!_receive_sync(tcp))
                            {
                                break;
                            }
                        }
                        catch (Exception err)
                        {
                            is_active = false;
                            _on_error(CloseReason.Error, err);
                        }
                    }
                    Thread.Sleep(1);
                }
                catch (Exception err)
                {
                    is_active = false;
                    _on_error(CloseReason.Error, err);
                }
            }
        }
        private int _receive_full(TcpClient so, byte[] buffer, in int pos, in int len)
        {
            var readed = 0;
            while (tcp.Available > 0 && readed < len)
            {
                var length = so.GetStream().Read(buffer, pos + readed, len - readed);
                if (length > 0)
                {
                    readed += length;
                    total_recv_bytes += length;
                    if (readed == len)
                    {
                        break;
                    }
                    else if (readed > len)
                    {
                        throw new IOException("Net Stream : overfollow");
                    }
                }
                else
                {
                    throw new IOException("Net Stream : EOF");
                }
            }
            return readed;
        }
        private bool _receive_sync(TcpClient so)
        {
            var recv_object = msg_pool.AllocRecv();
            try
            {
                var length = _receive_full(tcp, recv_object.Buffer, recv_object.BufferPosition, recv_object.BufferLength - recv_object.BufferPosition);
                recv_object.BufferPosition += length;
                if (recv_object.BufferPosition == IRecvMessage.FIXED_HEAD_SIZE)
                {
                    recv_object.ReadHead();
                    if (recv_object.PkgLength > 0)
                    {
                        length = _receive_full(so, recv_object.Buffer, recv_object.BufferPosition, recv_object.BufferLength - recv_object.BufferPosition);
                        recv_object.BufferPosition += length;
                        if (recv_object.BufferPosition == recv_object.BufferLength)
                        {
                            _received_package(recv_object);
                            return true;
                        }
                        else if (recv_object.BufferPosition > recv_object.BufferLength)
                        {
                            throw new IOException("endReceiveBody : Receive body overfollow");
                        }
                    }
                    else
                    {
                        _received_package(recv_object);
                        return true;
                    }
                }
                else if (recv_object.BufferPosition > IRecvMessage.FIXED_HEAD_SIZE)
                {
                    throw new IOException("endReceiveHead : Receive head overfollow");
                }
            }
            finally
            {
                recv_object.Dispose();
            }
            return false;
        }

        private void _received_package(MessagePool.RecvMessage recv_object)
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
            this.OnReceivedMessage?.Invoke(recv_object);
        }
        private void _received_message(MessagePool.RecvMessage recv_object)
        {
            recv_object.BeginBody();
            recv_object.BufferPosition = recv_object.BodyStartPistion;
        }

        private void _received_handshake(MessagePool.RecvMessage recv_object)
        {
            log.Info("received handshake!");
            is_handshake = true;
            var sysmsg = recv_object.ReadBodySystemMessage();
            if (sysmsg is SystemHandshakeAck)
            {
                var ack = sysmsg as SystemHandshakeAck;
                var token = ack.token;
                _heartbeat_start(ack.heartbeat_interval_ms);
                this.OnConnected?.Invoke(ack, token);
                var cb = connecting_callback;
                if (cb != null)
                {
                    connecting_callback = null;
                    client.TaskQueue.Enqueue((token, cb), static (st) => st.cb(null, st.token));
                }
            }
            else
            {
                _run_close(CloseReason.KickByServer, null, new Exception($"Handshake"));
            }
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
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemKick;
            _run_close(CloseReason.KickByServer, sysmsg?.reason, new Exception($"Server Kick"));
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------        
        #region ----HEART-BEAT----

        private readonly AtomicReference<SystemTimeInterval<TcpClient>> heartbeat_timer = new AtomicReference<SystemTimeInterval<TcpClient>>(null);
        private double last_heartbeat_r2c = CUtils.TickTimeMS;
        private double last_heartbeat_c2r = CUtils.TickTimeMS;
        private double last_heartbeat_chk = CUtils.TickTimeMS;
        private int heartbeat_interval_ms = 3000;

        private void _heartbeat_start(int intervalMS)
        {
            this.heartbeat_interval_ms = Math.Max(1000, intervalMS);
            this.last_heartbeat_r2c = CUtils.TickTimeMS;
            this.last_heartbeat_c2r = CUtils.TickTimeMS;
            this.last_heartbeat_chk = CUtils.TickTimeMS;
            if (intervalMS > 0)
            {
                log.InfoFormat("start heartbeat : {0} ms", intervalMS);
                this.heartbeat_timer.Value = new SystemTimeInterval<TcpClient>().Init(heartbeat_interval_ms, this.tcp);
            }
            else
            {
                this.heartbeat_timer.Value = null;
            }
        }
        private void _heartbeat_stop()
        {
            var timer = this.heartbeat_timer.GetAndSet(null);
            if (timer != null)
            {
                log.Info("stop heartbeat");
            }
        }
        private void _heartbeat_update()
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
                        _run_close(CloseReason.TimeOut, null, new Exception($"Heartbeat TimeOut"));
                    }
                    else
                    {
                        _send_heartbeat(so);
                    }
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------
    }
}
