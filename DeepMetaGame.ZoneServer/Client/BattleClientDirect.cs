using DeepCore.IO;
using DeepCore.Net;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Client
{
    public class BattleClientDirect : BattleClient, INetSessionListener
    {
        private INetSession session;
        private BattleCodec codec;

        public override INetSession Session { get { return session; } }

        public BattleClientDirect(
            EditorTemplates data_root,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(data_root, msgFactory, room, testToken, token)
        {
            INetSession netSession = ReflectionUtil.CreateInterface<INetSession>(room.Room.NetDriverString);
            if (netSession == null)
            {
                throw new Exception("Invalid NetDriver : " + room.Room.NetDriverString);
            }
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


        void INetSessionListener.sessionOpened(INetSession session)
        {
            base.callback_sessionOpened(session);
        }
        void INetSessionListener.sessionClosed(INetSession session)
        {
            base.callback_sessionClosed(session);
        }
        void INetSessionListener.messageReceived(INetSession session, object data)
        {
            base.callback_messageReceived(session, data);

        }
        void INetSessionListener.messageSent(INetSession session, object data)
        {
            base.callback_messageSent(session, data);
        }
        void INetSessionListener.onError(INetSession session, Exception err)
        {
            base.callback_onError(session, err);
        }

    }
}
