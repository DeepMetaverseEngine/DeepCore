using DeepCore;
using DeepCore.IO;
using DeepCore.NetClient;
using DeepCore.Threading;
using DeepCrystal.NetServer;
using DeepCrystal.Server;
using DeepCrystal.Threading.Dataflow;
using DeepFrozen.Server.NetUV;
using NetUV.Core.Channels;
using NetUV.Core.Handles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PomeloServer.NetUV
{
    public class UVPomeloServer : UVAcceptor<UVPomeloSession>, IServer
    {
        private readonly ServerProtocolPool messagePool;
        private ActionBlockExecutor executor;
        private TaskCompletionSourcePool tpool;
        public IExternalizableFactory Codec { get; private set; }
        public ServerProtocolPool MessagePool { get => messagePool; }
        public TaskCompletionSourcePool TcsPool { get => tpool; }
        public bool EnableRequest { get; private set; } = false;
        public int RequestTimeoutMS { get; private set; } = 30000;
        //---------------------------------------------------------------------------------------------------------------------
        public UVPomeloServer(ServerConfig cfg, IExternalizableFactory codec) : this(cfg, codec, null) { }
        public UVPomeloServer(ServerConfig cfg, IExternalizableFactory codec, EventLoop eventLoop) : base(cfg.Config == null ? new Properties() : new Properties(cfg.Config), eventLoop)
        {
            this.Codec = codec;
            this.messagePool = new ServerProtocolPool(Codec);
            this.executor = new ActionBlockExecutor();
            this.tpool = new TimerTaskCompletionSourcePool(Name, CollectionPool.Shared, 1000);
            if (config.TryGetAsBool(nameof(EnableRequest), out var boolValue))
            {
                EnableRequest = boolValue;
            }
            if (config.TryGetAsInt(nameof(RequestTimeoutMS), out var intValue))
            {
                RequestTimeoutMS = intValue;
            }
            SetListenPort(cfg.Port);
        }
        public void SetEnableRequest(bool value)
        {
            config[nameof(EnableRequest)] = value.ToString();
            EnableRequest = value;
        }
        public void SetRequestTimeoutMS(int value)
        {
            config[nameof(RequestTimeoutMS)] = value.ToString();
            RequestTimeoutMS = value;
        }
        protected override void OnDisposing()
        {
            this.onDisposingEvents();
            this.tpool.Dispose();
            this.executor.Complete();
            this.executor.Completion.Wait();
            this.messagePool.Dispose();
        }
        protected override void OnDisposed()
        {
        }
        protected override void uv_OnStarting() { }
        protected override void uv_OnStarted() { }
        protected override void uv_OnClosing(string reason) { }
        protected override void uv_OnClosed(string reason) { }
        protected override void uv_OnError(Exception err)
        {
            log.Error(err.Message, err);
            uv_cb_OnServerError(err);
        }
        sealed protected override bool uv_OnConnection(UVAbstractSession client)
        {
            try
            {
                return uv_cb_OnSessionConnected(client as UVPomeloSession);
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            return false;
        }
        //---------------------------------------------------------------------------------
        #region Sessions
        public void Broadcast(ISerializable message)
        {
            var list = new List<UVAbstractSession>(sessions.Values);
            {
                foreach (UVPomeloSession e in list)
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
        public ISession GetSessionByID(string sessionID)
        {
            return base.GetSession(sessionID) as UVPomeloSession;
        }
        public int GetSessions(IList<ISession> ret)
        {
            int count = 0;
            var list = new List<UVAbstractSession>(sessions.Values);
            {
                foreach (UVPomeloSession s in list)
                {
                    count++;
                    ret.Add(s);
                }
            }
            return count;
        }
        public bool HasSession(ISession session)
        {
            return sessions.ContainsKey(session.ID);
        }
        protected override UVAbstractSession CreateSession(Tcp client)
        {
            var ret = new UVPomeloSession(this, client);
            OnCreateSession?.Invoke(ret);
            return ret;
        }
        #endregion
        //---------------------------------------------------------------------------------
        #region Event

        internal void uv_cb_OnServerError(Exception err)
        {
            this.log.Error(err.Message, err);
            this.executor.Post(() =>
            {
                try
                {
                    event_OnServerError?.Invoke(this, err);
                }
                catch (Exception err2)
                {
                    log.Error(err2.Message, err2);
                }
            });
        }

        internal bool uv_cb_OnSessionConnected(UVPomeloSession session)
        {
            try
            {
                this.executor.RunAsync(() =>
                {
                    try
                    {
                        event_OnSessionConnected?.Invoke(session);
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                    session.server_main_SessionReady();
                });
                return true;
            }
            catch (Exception err2)
            {
                log.Error(err2.Message, err2);
            }
            return false;
        }
        internal bool smain_NewMessageFilter(UVPomeloSession session, IRecvMessage message)
        {
            try
            {
                var handle = event_MessageFilter;
                if (handle != null) { return handle.Invoke(session, message); }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return false;
        }
        internal void smain_cb_OnSessionDisconnected(UVPomeloSession session)
        {
            this.executor.Post(() =>
            {
                try
                {
                    event_OnSessionDisconnected?.Invoke(session);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            });
        }
        internal void smain_cb_SessionReceived(UVPomeloSession session, IRecvMessage recv)
        {
            BinaryMessage bin = BinaryMessage.NULL;
            ISerializable ser = null;
            uint sendID = recv.MsgSendID;
            if (event_OnSessionReceivedBinary != null || event_OnSessionRequestBinaryAsync != null)
            {
                bin = recv.ReadBodyBinary();
            }
            if (event_OnSessionReceivedMessage != null || event_OnSessionRequestMessageAsync != null)
            {
                ser = recv.ReadBody();
            }
            this.executor.RunAsync(() =>
            {
                try
                {
                    event_OnSessionReceivedBinary?.Invoke(session, bin, sendID);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                try
                {
                    event_OnSessionReceivedMessage?.Invoke(session, ser, sendID);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                try
                {
                    if (event_OnSessionRequestMessageAsync != null)
                    {
                        event_OnSessionRequestMessageAsync.Invoke(session, ser).ContinueWith(t =>
                        {
                            try
                            {
                                var rsp = t.GetResultAs();
                                if (rsp != null)
                                {
                                    session.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                                }
                            }
                            catch (Exception err) { log.Error(err.Message, err); }
                        });
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                try
                {
                    if (event_OnSessionRequestBinaryAsync != null)
                    {
                        event_OnSessionRequestBinaryAsync.Invoke(session, bin).ContinueWith(t =>
                        {
                            try
                            {
                                var rsp = t.GetResultAs();
                                if (rsp.HasRoute)
                                {
                                    session.InternalSend(rsp, MessageType.MSG_RESPONSE_S2C, sendID);
                                }
                            }
                            catch (Exception err) { log.Error(err.Message, err); }
                        });
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            });
        }
        //         internal void smain_cb_SessionDisposing(UVSession session)
        //         {
        //             sessions.TryRemove(session.ID, out session);
        //         }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------

        internal SessionValidateAsyncHandler GetOnSessionValidateAsync
        {
            get { return event_OnSessionValidateAsync; }
        }

        private SessionMessageFilter event_MessageFilter;
        private ServerErrorHandler event_OnServerError;
        private SessionConnectedHandler event_OnSessionConnected;
        private SessionDisconnectedHandler event_OnSessionDisconnected;
        private SessionValidateAsyncHandler event_OnSessionValidateAsync;
        private SessionReceivedMessageHandler event_OnSessionReceivedMessage;
        private SessionReceivedBinaryHandler event_OnSessionReceivedBinary;
        private SessionReceivedRequestMessageHandler event_OnSessionRequestMessageAsync;
        private SessionReceivedRequestBinaryHandler event_OnSessionRequestBinaryAsync;

        public event SessionMessageFilter MessageFilter { add { event_MessageFilter += value; } remove { event_MessageFilter -= value; } }
        public event ServerErrorHandler OnServerError { add { event_OnServerError += value; } remove { event_OnServerError -= value; } }
        public event SessionConnectedHandler OnSessionConnected { add { event_OnSessionConnected += value; } remove { event_OnSessionConnected -= value; } }
        public event SessionDisconnectedHandler OnSessionDisconnected { add { event_OnSessionDisconnected += value; } remove { event_OnSessionDisconnected -= value; } }
        public event SessionValidateAsyncHandler OnSessionValidateAsync { add { event_OnSessionValidateAsync += value; } remove { event_OnSessionValidateAsync -= value; } }
        public event SessionReceivedMessageHandler OnSessionReceivedMessage { add { event_OnSessionReceivedMessage += value; } remove { event_OnSessionReceivedMessage -= value; } }
        public event SessionReceivedBinaryHandler OnSessionReceivedBinary { add { event_OnSessionReceivedBinary += value; } remove { event_OnSessionReceivedBinary -= value; } }
        public event SessionReceivedRequestMessageHandler OnSessionRequestMessageAsync { add { event_OnSessionRequestMessageAsync += value; } remove { event_OnSessionRequestMessageAsync -= value; } }
        public event SessionReceivedRequestBinaryHandler OnSessionRequestBinaryAsync { add { event_OnSessionRequestBinaryAsync += value; } remove { event_OnSessionRequestBinaryAsync -= value; } }
        public event SessionHandler OnCreateSession;

        private void onDisposingEvents()
        {
            event_MessageFilter = null;
            event_OnServerError = null;
            event_OnSessionConnected = null;
            event_OnSessionDisconnected = null;
            event_OnSessionValidateAsync = null;
            event_OnSessionReceivedMessage = null;
            event_OnSessionReceivedBinary = null;
            event_OnSessionRequestMessageAsync = null;
            event_OnSessionRequestBinaryAsync = null;
            OnCreateSession = null;
        }

        #endregion
        //---------------------------------------------------------------------------------
    }
    //---------------------------------------------------------------------------------------------------------------
}
