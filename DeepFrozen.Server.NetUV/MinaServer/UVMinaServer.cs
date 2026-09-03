using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Protocol;
using DeepCrystal.Server;
using DeepCrystal.SharpMinaServer;
using NetUV.Core.Buffers;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DeepFrozen.Server.NetUV.MinaServer
{
    //---------------------------------------------------------------------------------------------------------------

    public class UVMinaServerFactory : IMinaServerFactory
    {
        public virtual IMinaServer CreateServer(ServerConfig cfg, INetPackageCodec codec)
        {
            return new UVMinaServer(codec, cfg);
        }
    }

    //---------------------------------------------------------------------------------------------------------------

    public class UVMinaServer : UVAcceptor<UVMinaSession>, IMinaServer
    {
        internal readonly INetPackageCodec codec;
        internal readonly string clientConnectString;
        public UVMinaServer(INetPackageCodec codec, ServerConfig cfg, EventLoop eventLoop = null) : base(cfg.Config, eventLoop)
        {
            this.codec = codec;
            this.clientConnectString = $"127.0.0.1:{cfg.Port}";
            this.SetListenPort(cfg.Port);
        }
        protected override bool uv_OnConnection(UVAbstractSession client)
        {
            try
            {
                var ss = client as UVMinaSession;
                var sl = listener.OnSessionConnected(ss);
                ss.OnConnected(sl);
                return true;
            }
            catch
            {
                return (false);
            }
        }
        protected override UVAbstractSession CreateSession(Tcp client)
        {
            return new UVMinaSession(this, client);
        }
        protected override void uv_OnStarting()
        {
        }
        protected override void uv_OnStarted()
        {
            listener?.OnInit(this);
        }
        protected override void uv_OnClosing(string reason)
        {
        }
        protected override void uv_OnClosed(string reason)
        {
        }
        protected override void OnDisposing()
        {
        }
        protected override void OnDisposed()
        {
            listener?.OnDestory();
        }
        #region -------------------Mina--------------------
        private IMinaServerListener listener;
        string IMinaServer.ClientConnectString => clientConnectString;
        INetPackageCodec IMinaServer.PackageCodec => codec;
        void IMinaServer.Open(IMinaServerListener listener)
        {
            this.listener = listener;
            this.StartAsync().Wait();
        }
        void IMinaServer.Close()
        {
            this.StopAsync("server close").Wait();
        }
        public void Broadcast(object message)
        {
            var list = new List<UVAbstractSession>(sessions.Values);
            {
                foreach (IMinaSession e in list)
                {
                    try
                    {
                        e.Send(message);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        public IMinaSession GetSessionByID(string sessionID)
        {
            return base.GetSession(sessionID);
        }
        public IEnumerable<IMinaSession> GetSessions()
        {
            var list = new List<IMinaSession>();
            ForEachSession(list, static (list, session) =>
            {
                list.Add(session);
            });
            return list;
        }
        public bool HasSession(IMinaSession session)
        {
            return base.HasSession(session as UVMinaSession);
        }
        void IMinaServer.SetEmulateLaggingMS(int min, int max) { }
        void IMinaServer.GetEmulateLaggingMS(out int min, out int max) { min = 0; max = 0; }

        #endregion
    }

    //---------------------------------------------------------------------------------------------------------------

    public class UVMinaSession : UVAbstractSession, IMinaSession
    {
        private const int FIXED_HEAD_SIZE = 4;
        private FixedHeadProtocolDecoding recv_buffer = new FixedHeadProtocolDecoding(FIXED_HEAD_SIZE);
        private MemoryStream send_buffer = new MemoryStream();
        public UVMinaServer Server => base.server as UVMinaServer;
        public IMinaSessionListener Listener => this.listener;
        public UVMinaSession(UVMinaServer server, Tcp tcp) : base(server, tcp)
        {
            recv_buffer.OnDecodeHead += uv_buffer_OnDecodeHead;
            recv_buffer.OnDecodeBody += uv_buffer_OnDecodeBody;
        }

        protected override void OnDisposeEvents()
        {
        }
        protected override void OnDisposeListening()
        {
        }
        protected override Task OnDisposingAsync()
        {
            return Task.CompletedTask;
        }
        protected override void OnError(Exception err)
        {
            listener.OnError(this, err);
        }
        protected override void uv_OnDisconnected(string reason, Action complete)
        {
            this.listener.OnDisconnected(this, reason);
        }
        protected override void uv_OnDisconnecting(string reason, Action<StreamHandle, Exception> complete)
        {

        }
        protected override void uv_OnDataReceived(ReadableBuffer data)
        {
            int count = data.Count;
            var buffer = new ArraySegment<byte>(new byte[count]);
            data.ReadBytes(buffer.Array, count);
            recv_buffer.OnReceived(buffer);
        }
        private void uv_buffer_OnDecodeBody(System.IO.Stream stream, int pkgLength)
        {
            if (Server.codec.DoDecode(stream, out var message))
            {
                listener.OnReceivedMessage(this, message);
            }
            else
            {
                this.uv_InternalStop("Decode Error");
            }
        }
        private void uv_buffer_OnDecodeHead(System.IO.Stream stream, out int pkgLength)
        {
            pkgLength = LittleEdian.GetS32(stream);
        }
        private bool encode(object message, out ArraySegment<byte> output)
        {
            lock (send_buffer)
            {
                send_buffer.SetLength(4);
                send_buffer.Position = 4;
                if (Server.codec.DoEncode(send_buffer, message))
                {
                    var pos = send_buffer.Position;
                    var len = (int)(send_buffer.Position - 4);
                    send_buffer.Position = 0;
                    LittleEdian.PutS32(send_buffer, len);
                    send_buffer.Position = pos;
                    var buffer = send_buffer.ToArray();
                    output = buffer;
                    return true;
                }
                output = ArraySegment<byte>.Empty;
                return false;
            }
        }
        #region -------------------Mina--------------------
        private IMinaSessionListener listener;
        internal void OnConnected(IMinaSessionListener listener)
        {
            this.listener = listener;
            this.listener.OnConnected(this);
        }
        bool IMinaSession.Disconnect(bool force)
        {
            return this.DisconnectAsync($"force={force}").WaitForResult();
        }
        IPEndPoint IMinaSession.GetRemoteAddress()
        {
            return base.client?.GetPeerEndPoint();
        }
        bool IMinaSession.Send(object message)
        {
            if (encode(message, out var buffer))
            {
                base.InternalSend(buffer, (ok) =>
                {
                    listener.OnSentMessage(this, message);
                });
                return true;
            }
            return false;
        }
        #endregion
    }

    //---------------------------------------------------------------------------------------------------------------
}
