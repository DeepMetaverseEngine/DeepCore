using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.MinaClient;
using DeepCore.Protocol;
using DeepCrystal.Server;
using DeepCrystal.SharpMinaServer;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DeepFrozen.Server.SSocket.NetServer
{

    public class SSMinaServer : Disposable, IMinaServer
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(SSMinaServer));
        private static Logger log = LoggerFactory.GetLogger("SSocket");
        private static ArraySegment<byte> _zero_segment = new ArraySegment<byte>(new byte[0]);
        private readonly INetPackageCodec _codec;
        private readonly IReceiveFilterFactory<BinaryRequestInfo> _filter;
        private EmulateLagging _emu_lagging;
        private AppServerImpl _server;
        private string _connect_string;
        internal long mTotalSentBytes;
        internal long mTotalRecvBytes;
        private DeepCrystal.Server.ServerConfig sconfig;

        public string ClientConnectString { get { return _connect_string; } }
        public INetPackageCodec PackageCodec { get { return _codec; } }
        public long TotalSentBytes { get { return mTotalSentBytes; } }
        public long TotalRecvBytes { get { return mTotalRecvBytes; } }
        public int SessionCount { get { return _server.SessionCount; } }
        public bool EmulateLag
        {
            get { return _emu_lagging != null && _emu_lagging.EmulateLag; }
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public SSMinaServer(DeepCrystal.Server.ServerConfig sconfig, INetPackageCodec codec, IReceiveFilterFactory<BinaryRequestInfo> filter)
        {
            Alloc.RecordConstructor(this.GetType());
            this.sconfig = sconfig;
            this._codec = codec;
            this._filter = filter;
            this._emu_lagging = new EmulateLagging();
        }
        ~SSMinaServer()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
            _emu_lagging.Dispose();
            _emu_lagging = null;
            _server?.Stop();
            _server?.Dispose();
            _server = null;
        }
        public void Close()
        {
            this.Dispose();
        }

        public void Open(string host, int port, IMinaServerListener listener)
        {
            this.sconfig = new DeepCrystal.Server.ServerConfig()
            {
                Host = host,
                Port = port,
            };
            this.Open(listener);
        }
        /// <summary>
        /// <pre>
        /// SuperSocket.HOST = 192.168.1.1
        /// SuperSocket.PORT = 9527
        /// SuperSocket.CLIENT_CONNECT_STRING = 192.168.1.1:9527
        /// </pre>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="listener"></param>
        public void Open(IMinaServerListener listener)
        {
            var config = sconfig.Config;
            var host = sconfig.Host;// config["SuperSocket.HOST"];
            var port = sconfig.Port;// config["SuperSocket.PORT"];
            if (host == null)
            {
                throw new Exception(@"Config Must Be Contains 'SuperSocket.HOST' and 'SuperSocket.PORT' fields !");
            }
            //            int dport = 0;
            //             if (!int.TryParse(port, out dport))
            //             {
            //                 throw new Exception(@"Config 'SuperSocket.PORT' must be a digit value !");
            //             }
            config["SuperSocket.HOST"] = host;
            config["SuperSocket.PORT"] = port.ToString();
            config["SuperSocket.CLIENT_CONNECT_STRING"] = host + ":" + port;
            this._connect_string = config["SuperSocket.CLIENT_CONNECT_STRING"];
            if (_connect_string == null)
            {
                throw new Exception(@"Config Must Be Contains 'SuperSocket.CLIENT_CONNECT_STRING' fields !");
            }
            var cfg = new SuperSocket.SocketBase.Config.ServerConfig();
            cfg.SyncSend = false;
            cfg.Port = port;
            cfg.Mode = SocketMode.Tcp;
            cfg.DisableSessionSnapshot = true;
            cfg.LogCommand = false;
            cfg.Name = "Server";
            cfg.ClearIdleSession = true;
            cfg.ClearIdleSessionInterval = 10;
            cfg.IdleSessionTimeOut = 10000;
            cfg.LogBasicSessionActivity = false;
            cfg.SendingQueueSize = 1000;
            cfg.SendTimeOut = 5000;
            cfg.ReceiveBufferSize = 16 * 1024;
            cfg.SendBufferSize = 16 * 1024;
            cfg.TextEncoding = "UTF-8";
            cfg.MaxConnectionNumber = 500;
            cfg.MaxRequestLength = 1024 * 100;
            foreach (string key in config.Keys)
            {
                try
                {
                    if (key.StartsWith("SuperSocket."))
                    {
                        string value = config[key];
                        string fname = key.Substring("SuperSocket.".Length);
                        FieldInfo finfo = typeof(SuperSocket.SocketBase.Config.ServerConfig).GetField(fname);
                        if (finfo != null)
                        {
                            finfo.SetValue(cfg, Parser.StringToObject(value.Trim(), finfo.FieldType));
                            log.Info(string.Format(" {0} = {1}", key, value));
                        }
                    }
                }
                catch { }
            }
            this.Open(cfg, listener);
        }
        /// <summary>
        /// <pre>
        /// cfg.name: 服务器实例的名称;
        /// cfg.serverType: 服务器实例的类型的完整名称;
        /// cfg.serverTypeName: 所选用的服务器类型在 serverTypes 节点的名字，配置节点 serverTypes 用于定义所有可用的服务器类型，我们将在后面再做详细介绍;
        /// cfg.ip: 服务器监听的ip地址。你可以设置具体的地址，也可以设置为下面的值 Any - 所有的IPv4地址 IPv6Any - 所有的IPv6地址
        /// cfg.port: 服务器监听的端口;
        /// cfg.listenBacklog: 监听队列的大小;
        /// cfg.mode: Socket服务器运行的模式, Tcp (默认) 或者 Udp;
        /// cfg.disabled: 服务器实例是否禁用了;
        /// cfg.startupOrder: 服务器实例启动顺序, bootstrap 将按照此值的顺序来启动多个服务器实例;
        /// cfg.sendTimeOut: 发送数据超时时间;
        /// cfg.sendingQueueSize: 发送队列最大长度, 默认值为5;
        /// cfg.maxConnectionNumber: 可允许连接的最大连接数;
        /// cfg.receiveBufferSize: 接收缓冲区大小;
        /// cfg.sendBufferSize: 发送缓冲区大小;
        /// cfg.syncSend: 是否启用同步发送模式, 默认值: false;
        /// cfg.logCommand: 是否记录命令执行的记录;
        /// cfg.logBasicSessionActivity: 是否记录session的基本活动，如连接和断开;
        /// cfg.clearIdleSession: true 或 false, 是否定时清空空闲会话，默认值是 false;
        /// cfg.clearIdleSessionInterval: 清空空闲会话的时间间隔, 默认值是120, 单位为秒;
        /// cfg.idleSessionTimeOut: 会话空闲超时时间; 当此会话空闲时间超过此值，同时clearIdleSession被配置成true时，此会话将会被关闭; 默认值为300，单位为秒;
        /// cfg.security: Empty, Tls, Ssl3. Socket服务器所采用的传输层加密协议，默认值为空;
        /// cfg.maxRequestLength: 最大允许的请求长度，默认值为1024;
        /// cfg.textEncoding: 文本的默认编码，默认值是 ASCII;
        /// cfg.defaultCulture: 此服务器实例的默认 thread culture, 只在.Net 4.5中可用而且在隔离级别为 'None' 时无效;
        /// cfg.disableSessionSnapshot: 是否禁用会话快照, 默认值为 false.
        /// cfg.sessionSnapshotInterval: 会话快照时间间隔, 默认值是 5, 单位为秒;
        /// cfg.keepAliveTime: 网络连接正常情况下的keep alive数据的发送间隔, 默认值为 600, 单位为秒;
        /// cfg.keepAliveInterval: Keep alive失败之后, keep alive探测包的发送间隔，默认值为 60, 单位为秒;
        /// </pre>
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="listener"></param>
        public void Open(SuperSocket.SocketBase.Config.ServerConfig cfg, IMinaServerListener listener)
        {
            this._server = new AppServerImpl(listener, this, _filter);
            if (_server.Setup(cfg))
            {
                this._emu_lagging.Start();
                this._server.Start();
            }
            else
            {
                throw new Exception("Open Server Failed !");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------

        public void Broadcast(object message)
        {
            foreach (IMinaSession session in _server.GetAllSessions())
            {
                session.Send(message);
            }
        }

        public bool HasSession(IMinaSession session)
        {
            return _server.GetSessionByID(session.ID) != null;
        }

        public IMinaSession GetSessionByID(string sessionID)
        {
            return _server.GetSessionByID(sessionID) as IMinaSession;
        }

        public IEnumerable<IMinaSession> GetSessions()
        {
            return _server.GetAllSessions();
        }

        //-------------------------------------------------------------------------------------------------------------------------

        internal void SendDelay(SSMniaSession session, DeepCore.IO.MemoryStream stream)
        {
            _emu_lagging.SendDelay(session, stream);
        }
        internal void RecvDelay(SSMniaSession session, object obj)
        {
            _emu_lagging.RecvDelay(session, obj);
        }


        public void SetEmulateLaggingMS(int min, int max)
        {
            _emu_lagging.SetEmulateLaggingMS(min, max);
        }
        public void GetEmulateLaggingMS(out int min, out int max)
        {
            _emu_lagging.GetEmulateLaggingMS(out min, out max);
        }

        //-------------------------------------------------------------------------------------------------------------------------

        protected internal virtual bool DoDecodeInternal(SSMniaSession session, BinaryRequestInfo bin, out int bytes, out object message)
        {
            bytes = bin.Body.Length + 4;
            using (var ms = new DeepCore.IO.MemoryStream(bin.Body))
            {
                object obj;
                if (_codec.DoDecode(ms, out obj))
                {
                    message = obj;
                    return true;
                }
            }
            message = null;
            return false;
        }
        protected internal virtual bool DoEncodeInternal(SSMniaSession session, object message, DeepCore.IO.MemoryStream ms)
        {
            LittleEdian.PutS32(ms, 0);
            if (_codec.DoEncode(ms, message))
            {
                int length = (int)ms.Position;
                int blen = length - 4;
                if (blen < 0)
                {
                    return false;
                }
                ms.Position = 0;
                LittleEdian.PutS32(ms, blen);
                ms.Position = length;
                return true;
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        class AppServerImpl : AppServer<SSMniaSession, BinaryRequestInfo>
        {
            internal SSMinaServer _owner;
            internal IMinaServerListener _listener;

            public AppServerImpl(IMinaServerListener listener, SSMinaServer owner, IReceiveFilterFactory<BinaryRequestInfo> filter)
                : base(filter)
            {
                this._owner = owner;
                this._listener = listener;
                this.NewRequestReceived += AppServerImpl_NewRequestReceived;
            }

            private void AppServerImpl_NewRequestReceived(SSMniaSession session, BinaryRequestInfo requestInfo)
            {
                int bytes;
                object msg;
                if (_owner.DoDecodeInternal(session, requestInfo, out bytes, out msg))
                {
                    _owner.mTotalRecvBytes += bytes;
                    session.NewRequestReceived(bytes, msg);
                }
            }

            protected override bool RegisterSession(string sessionID, SSMniaSession appSession)
            {
                if (base.RegisterSession(sessionID, appSession))
                {
                    appSession.NewSessionConnected(_owner, _listener.OnSessionConnected(appSession), _owner.PackageCodec);
                    return true;
                }
                return false;
            }

            protected override void OnNewSessionConnected(SSMniaSession session)
            {
                base.OnNewSessionConnected(session);
            }

            protected override void OnStarted()
            {
                base.OnStarted();
                _listener.OnInit(_owner);
            }

            protected override void OnStopped()
            {
                base.OnStopped();
                _listener.OnDestory();
            }

        }

    }
}
