using DeepCore;
using DeepCore.IO;
using DeepCore.Net;
using DeepCore.Net.Sockets;
using DeepCore.Protocol;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Client
{
    public class BattleClientProxy : BattleClient
    {
        private INetSession proxy_session;
        private enum Status : int
        {
            ON_LINE, OFF_LINE,
        }
        private AtomicState<Status> status = new AtomicState<Status>(Status.OFF_LINE);

        public override INetSession Session
        {
            get { return proxy_session; }
        }

        public BattleClientProxy(
            INetSession netSession,
            EditorTemplates data_root,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(data_root, msgFactory, room, testToken, token)
        {
            this.proxy_session = netSession;
        }
        public BattleClientProxy(
            ProxyNetSession proxySession,
            string proxySessionConnectString,
            EditorTemplates data_root,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(data_root, msgFactory, room, testToken, token)
        {
            this.proxy_session = proxySession;
            this.proxy_session.Open(proxySessionConnectString, new BattleCodec(DataRoot.Templates), new DirectProxyListener(this));
        }
        protected override void Disposing()
        {
            this.Stop();
            base.Disposing();
        }
        public override void Start()
        {
            if (status.Update(Status.ON_LINE))
            {
                proxy_session.OnMessageReceived += proxy_session_OnMessageReceived;
                proxy_session.OnMessageSent += proxy_session_OnMessageSent;
                proxy_session.OnError += proxy_session_OnError;
                proxy_session.Send(new ConnectToProxy(room.Room.ClientConnectString));
                base.callback_sessionOpened(proxy_session);
            }
        }
        public override void Stop()
        {
            if (status.Update(Status.OFF_LINE))
            {
                proxy_session.OnMessageReceived -= proxy_session_OnMessageReceived;
                proxy_session.OnMessageSent -= proxy_session_OnMessageSent;
                proxy_session.OnError -= proxy_session_OnError;
                base.callback_sessionClosed(proxy_session);
            }
        }
        //----------------------------------------------------------------------------------------------------

        private void proxy_session_OnMessageReceived(INetSession session, object data)
        {
            if (data is DisconnectFromProxy)
            {
                Stop();
            }
            else
            {
                base.callback_messageReceived(session, data);
            }
        }
        private void proxy_session_OnMessageSent(INetSession session, object data)
        {
            base.callback_messageSent(session, data);
        }
        private void proxy_session_OnError(INetSession session, Exception err)
        {
            base.callback_onError(session, err);
        }

        //----------------------------------------------------------------------------------------------------
        class DirectProxyListener : INetSessionListener
        {
            private BattleClientProxy mClient;
            public DirectProxyListener(BattleClientProxy client)
            {
                this.mClient = client;
            }
            public void sessionOpened(INetSession session)
            {
            }
            public void sessionClosed(INetSession session)
            {
            }
            public void messageReceived(INetSession session, object data)
            {
            }
            public void messageSent(INetSession session, object data)
            {
            }
            public void onError(INetSession session, Exception err)
            {
            }
        }

        //----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 将游戏服和战斗服合并成一个连接，需要连接代理服务器
        /// </summary>
        public abstract class ProxyNetSession : NetSession
        {
            public readonly byte TYPE_GS = 1;
            public readonly byte TYPE_BS = 2;

            public static int DEFAULT_BUFFER_SIZE = 1024;

            public ProxyNetSession(byte type_gs_header = 1, byte type_bs_header = 2)
            {
                this.TYPE_GS = type_gs_header;
                this.TYPE_BS = type_bs_header;
            }

            protected override bool doEncode(Stream output, object message, out int writeBytes)
            {
                if (IsMessageTypeGS(message))
                {
                    return doEncodeGS(output, message, out writeBytes);
                }
                else if (message is IMessage)
                {
                    return doEncodeBS(output, message as IMessage, out  writeBytes);
                }
                writeBytes = 0;
                return false;
            }
            protected override bool doDecode(Stream input, out object message, out int readBytes)
            {
                int b0 = input.ReadByte();
                int b1 = input.ReadByte();
                int b2 = input.ReadByte();
                int ServerID = input.ReadByte();
                int BodySize = (int)(b0 | (b1 << 8) | (b2 << 16));
                bool ret = false;
                object msg = null;
                if (ServerID == TYPE_BS)
                {
                    ret = doDecodeBS(input, BodySize, out msg);
                }
                else if (ServerID == TYPE_GS)
                {
                    ret = doDecodeGS(input, BodySize, out msg);
                }
                message = msg;
                readBytes = BodySize + 4;
                return ret;
            }
            protected virtual bool doDecodeBS(Stream input, int bodysize, out object message)
            {
                byte[] h_body = IOUtil.ReadExpect(input, bodysize);
                using (DeepCore.IO.MemoryStream ms = new DeepCore.IO.MemoryStream(h_body))
                {
                    if (mCodec.doDecode(ms, out message))
                    {
                        return true;
                    }
                }
                return false;
            }
            protected virtual bool doEncodeBS(Stream output, IMessage send_msg, out int sendBytes)
            {
                sendBytes = 0;
                using (DeepCore.IO.MemoryStream ms = new DeepCore.IO.MemoryStream(DEFAULT_BUFFER_SIZE))
                {
                    if (mCodec.doEncode(ms, send_msg))
                    {
                        byte[] h_body = ms.GetBuffer();
                        int length = (int)ms.Position;
                        {
                            int BodySize = (int)(length);
                            if (length > 0x00FFFFFF)
                            {
                                throw new Exception("Size Overflow");
                            }
                            int b0 = ((BodySize & 0x000000FF));
                            int b1 = ((BodySize & 0x0000FF00) >> 8);
                            int b2 = ((BodySize & 0x00FF0000) >> 16);
                            output.WriteByte((byte)b0);
                            output.WriteByte((byte)b1);
                            output.WriteByte((byte)b2);
                            output.WriteByte((byte)TYPE_BS);
                        }
                        output.Write(h_body, 0, length);
                        sendBytes = length + 4;
                        return true;
                    }
                }
                return false;
            }


            public abstract bool IsMessageTypeGS(object msg);
            protected virtual bool doDecodeGS(Stream input, int bodysize, out object message)
            {
                message = null;
                return false;
            }
            protected virtual bool doEncodeGS(Stream output, object protocol, out int writeBytes)
            {
                writeBytes = 0;
                return false;
            }
        }
    }
}
