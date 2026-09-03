using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.MinaClient;
using DeepCore.MinaClient.Sockets;
using DeepCore.Net;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Client
{
    public class BattleClientDirect : BattleClient, IMinaClientSessionListener
    {
        private IMinaClientSession session;
        private BattleCodec codec;

        public override IMinaClientSession Session { get { return session; } }
        public BattleClientDirect(
            EditorTemplates data_root,
            ZoneSlaveFactory slaveFactory,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(data_root, slaveFactory, msgFactory, room, testToken, token)
        {
            var netSession = new MinaSocketSession();
            this.codec = new BattleCodec(base.DataRoot.Templates);
            this.session = netSession;
        }
        protected override void Disposing()
        {
            this.Stop();
            base.Disposing();
        }
        public override void Start()
        {
            session.Open(room.Room.ClientConnectString, codec, this);
        }

        public override void Stop()
        {
            session.Close();
        }

        //----------------------------------------------------------------------------------------------------

        public override bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            sdata = null;
            return false;
        }

        void IMinaClientSessionListener.OnSessionOpened(IMinaClientSession session)
        {
            base.callback_sessionOpened(session);
        }
        void IMinaClientSessionListener.OnSessionClosed(IMinaClientSession session)
        {
            base.callback_sessionClosed(session);
        }
        void IMinaClientSessionListener.OnMessageReceived(IMinaClientSession session, object data)
        {
            base.callback_messageReceived(session, data);

        }
        void IMinaClientSessionListener.OnMessageSent(IMinaClientSession session, object data)
        {
            base.callback_messageSent(session, data);
        }
        void IMinaClientSessionListener.OnError(IMinaClientSession session, Exception err)
        {
            base.callback_onError(session, err);
        }

    }
}
