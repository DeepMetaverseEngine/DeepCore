using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Net;
using DeepCore.NetClient;
using DeepCrystal.NetServer;
using DeepFrozen.Server.NetUV;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using System;
using System.Threading.Tasks;

namespace PomeloClient.NetUV
{
    public class UVClientTCP : UVConnector, IClientAdapter
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(UVClientTCP));
        protected readonly INetClient client;
        private int current_ping;
        private bool is_handshake = false;
        private DateTime last_connect_time;
        private string host;
        private Action<Exception, ISerializable> connect_callback;
        //--------------------------------------------------------------------------------------------------
        public UVClientTCP(INetClient client, Properties cfg, ServerProtocolPool ppool, EventLoop eventLoop = null) : base(cfg, ppool, eventLoop)
        {
            this.client = client;
        }
        public bool IsHandshake { get { return is_handshake; } }
        public int Ping { get { return current_ping; } }
        public DateTime ConnectTime { get { return last_connect_time; } }
        public override string ToString()
        {
            return "UVClientTCP:" + host;
        }
        //--------------------------------------------------------------------------------------------------
        public bool Connect(string address, int timeout, ISerializable user, Action<Exception, ISerializable> callback)
        {
            this.host = address;
            this.last_connect_time = DateTime.Now;
            IPUtil.TryParseHostPort(address, out var host, out var port);
            var addrs = IPUtil.GetIPEndPoints(host, port, out var family, out var ips);
            this.ConnectAsync(addrs[0]).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    callback?.Invoke(t.Exception, null);
                }
                else if (t.GetResultAs())
                {
                    this.connect_callback = callback;
                    SendHandshake(user);
                }
                else
                {
                    callback?.Invoke(t.Exception, null);
                }
            });
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
        public bool Disconnect(Action action)
        {
            CloseAsync(CloseReason.CloseByUser).ContinueWith(t => { action(); });
            return true;
        }
        public bool Send(ISerializable msg, MessageType msgType, uint send_id)
        {
            return base.Send(msg, msgType, send_id);
        }
        public bool Send(BinaryMessage msg, MessageType msgType, uint send_id)
        {
            return base.Send(msg, msgType, send_id);
        }
        public void Update()
        {
            CheckHeartbeat();
        }
        //--------------------------------------------------------------------------------------------------
        protected override void uv_OnConnected(Tcp client, Exception exception)
        {
            base.uv_OnConnected(client, exception);
        }
        protected override void uv_InternalClose(CloseReason reason, string msg, Action<bool, Exception> done = null)
        {
            is_handshake = false;
            StopHeartbeat();
            base.uv_InternalClose(reason, msg, done);
        }
        protected override void uv_onSentComplete(SendMessage send)
        {
            event_OnSentMessage?.Invoke(send);
        }
        protected override void uv_onClosed(CloseReason reason, string msg)
        {
            var cb = connect_callback;
            if (cb != null)
            {
                connect_callback = null;
                cb(new Exception($"{reason}"), null);
            }
            is_handshake = false;
            base.uv_onClosed(reason, msg);
            event_OnDisconnected?.Invoke(ToFuckReason(reason), msg);
        }
        protected override void uv_onError(Tcp tcp, Exception err)
        {
            event_OnError?.Invoke(err);
            base.uv_onError(tcp, err);
        }
        //--------------------------------------------------------------------------------------------------
        protected override void main_onError(Exception err)
        {
            event_OnError?.Invoke(err);
            base.main_onError(err);
        }
        protected override void main_onProtocolReceived(RecvMessage recv, Exception error)
        {
            switch (recv.PkgType)
            {
                case PackageType.PKG_HANDSHAKE_ACK:
                    main_received_handshake(recv);
                    break;
                case PackageType.PKG_HEARTBEAT:
                    main_received_heartbeat(recv);
                    break;
                case PackageType.PKG_MESSAGE:
                    main_received_message(recv);
                    break;
                case PackageType.PKG_KICK:
                    main_received_kick(recv);
                    break;
            }
            event_OnReceivedMessage?.Invoke(recv);
        }

        private void main_received_handshake(RecvMessage recv_object)
        {
            log.Debug("received handshake!");
            is_handshake = true;
            var sysmsg = recv_object.ReadBodySystemMessage();
            if (sysmsg is SystemHandshakeAck)
            {
                var ack = sysmsg as SystemHandshakeAck;
                InitHeartbeat(ack.heartbeat_interval_ms);
                event_OnConnected?.Invoke(ack, ack.token);
                var cb = connect_callback;
                if (cb != null)
                {
                    connect_callback = null;
                    cb(null, ack.token);
                }
            }
            else
            {
                CloseAsync();
            }
        }

        private void main_received_heartbeat(RecvMessage recv_object)
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
        private void main_received_kick(RecvMessage recv_object)
        {
            var sysmsg = recv_object.ReadBodySystemMessage() as SystemKick;
            log.Debug("received kick!");
            CloseAsync(CloseReason.CloseByKickByServer);
        }


        private void main_received_message(RecvMessage recv_object)
        {
            recv_object.BeginBody();
            recv_object.BufferPosition = recv_object.BodyStartPistion;
        }

        private readonly AtomicReference<SystemTimeInterval<Tcp>> heartbeat_timer = new AtomicReference<SystemTimeInterval<Tcp>>(null);
        private double last_heartbeat_r2c = CUtils.TickTimeMS;
        private double last_heartbeat_c2r = CUtils.TickTimeMS;
        private double last_heartbeat_chk = CUtils.TickTimeMS;
        private int heartbeat_interval_ms = 3000;

        protected virtual void SendHandshake(ISerializable user)
        {
            log.Debug("send handshake!");
            base.SendSystemMessage(new SystemHandshake() { user = user });
        }
        protected virtual void SendHeartbeat()
        {
            var ctime = CUtils.TickTimeMS;
            SendSystemMessage(new SystemHeartbeat() { time = ctime });
        }
        protected virtual void InitHeartbeat(int intervalMS)
        {
            this.heartbeat_interval_ms = Math.Max(1000, intervalMS);
            this.last_heartbeat_r2c = CUtils.TickTimeMS;
            this.last_heartbeat_c2r = CUtils.TickTimeMS;
            this.last_heartbeat_chk = CUtils.TickTimeMS;
            if (intervalMS > 0)
            {
                log.DebugFormat("start heartbeat : {0} ms", intervalMS);
                this.heartbeat_timer.Value = new SystemTimeInterval<Tcp>().Init(heartbeat_interval_ms, tcp);
            }
            else
            {
                this.heartbeat_timer.Value = null;
            }
        }
        protected virtual void StopHeartbeat()
        {
            var timer = this.heartbeat_timer.GetAndSet(null);
            if (timer != null)
            {
                log.Debug("stop heartbeat");
            }
        }

        private void CheckHeartbeat()
        {
            var timer = this.heartbeat_timer.Value;
            if (timer != null && timer.Update())
            {
                if (IsConnected)
                {
                    //log.Debug("check heartbeat");
                    var curtime = CUtils.TickTimeMS;
                    var tick = (float)(curtime - last_heartbeat_chk);
                    last_heartbeat_chk = curtime;
                    if ((curtime - last_heartbeat_r2c) > heartbeat_interval_ms * 4)
                    {
                        CloseAsync(CloseReason.CloseByTimeout);
                    }
                    else
                    {
                        SendHeartbeat();
                    }
                }
            }
        }



        //--------------------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------------------

        private Action<ISendMessage> event_OnSentMessage;
        private Action<IRecvMessage> event_OnReceivedMessage;
        private Action<Exception> event_OnError;
        private Action<DeepCore.NetClient.CloseReason, string> event_OnDisconnected;
        private Action<SystemHandshakeAck, ISerializable> event_OnConnected;

        public event Action<ISendMessage> OnSentMessage { add { event_OnSentMessage += value; } remove { event_OnSentMessage -= value; } }
        public event Action<IRecvMessage> OnReceivedMessage { add { event_OnReceivedMessage += value; } remove { event_OnReceivedMessage -= value; } }
        public event Action<Exception> OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
        public event Action<DeepCore.NetClient.CloseReason, string> OnDisconnected { add { event_OnDisconnected += value; } remove { event_OnDisconnected -= value; } }
        public event Action<SystemHandshakeAck, ISerializable> OnConnected { add { event_OnConnected += value; } remove { event_OnConnected -= value; } }

        //--------------------------------------------------------------------------------------------------
        public static DeepCore.NetClient.CloseReason ToFuckReason(CloseReason reason)
        {
            switch (reason)
            {
                case CloseReason.Unknow:
                    return DeepCore.NetClient.CloseReason.Unknown;
                case CloseReason.CloseByUser:
                    return DeepCore.NetClient.CloseReason.ClientClose;
                case CloseReason.CloseByComplete:
                    return DeepCore.NetClient.CloseReason.Unknown;
                case CloseReason.CloseByException:
                    return DeepCore.NetClient.CloseReason.Error;
                case CloseReason.CloseByKickByServer:
                    return DeepCore.NetClient.CloseReason.KickByServer;
                case CloseReason.CloseByTimeout:
                    return DeepCore.NetClient.CloseReason.TimeOut;
                default:
                    return DeepCore.NetClient.CloseReason.Unknown;
            }
        }
        //--------------------------------------------------------------------------------------------------

    }
}
