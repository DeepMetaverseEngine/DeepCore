using DeepCore.Game3D.ZoneServer;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Protocol;
using System;

namespace DeepCore.Game3D.Slave.Runtime
{
    public abstract class NetConnectorBattle : AbstractBattle
    {
        private static Logger log = LoggerFactory.GetLogger("BattleClient");
        protected readonly PlayerWillConnectResponseB2R room;
        protected readonly PlayerWillConnectRequestR2B testToken;
        //private KickedByServerNotifyB2C kicked;
        private TimeInterval<int> ping_task = new TimeInterval<int>(5000);
        //private int current_ping = 0;
        public string token { get; private set; }
        public string data_root { get; private set; }
        public abstract INetSession Session { get; }
        public string PlayerUUID { get { return room.PlayerUUID; } }
        public MessageFactoryGenerator MessageFactory { get; private set; }
        public override long RecvPackages { get { return Session.TotalRecvPackages; } }
        public override long SendPackages { get { return Session.TotalSentPackages; } }
        public int PingIntervalMS
        {
            get { return ping_task.IntervalTimeMS; }
            set { ping_task = new TimeInterval<int>(Math.Max(1000, value)); }
        }
//         public KickedByServerNotifyB2C KickMessage
//         {
//             get { return kicked; }
//         }
        public override bool IsNet { get { return true; } }

        public NetConnectorBattle(
            EditorTemplates datas,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(datas)
        {
            this.room = room;
            this.testToken = testToken;
            this.token = token;
            this.MessageFactory = msgFactory;
            this.Layer.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
        }


        protected override void Disposing()
        {
            base.Disposing();
            this.mHandleMessage = null;
            this.mDisconnectd = null;
            this.mConnected = null;
            this.mHandleError = null;
        }
        //----------------------------------------------------------------------------------------------------
        abstract public void Start();

        abstract public void Stop();
        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// 发送单位控制命令
        /// </summary>
        /// <param name="action"></param>
        public override void SendAction(GameData.Zone.Action action)
        {
            Session.Send(action);
        }
        /// <summary>
        /// 请求离开房间
        /// </summary>
        public void SendLeaveRoom()
        {
            LeaveRoomRequestC2B req = new LeaveRoomRequestC2B();
            Session.Send(req);
        }
        /// <summary>
        /// 发送聊天
        /// </summary>
        public void SendChatMessage(string message, ChatMessageType type = ChatMessageType.PlayerToForce)
        {
            ChatAction req = new ChatAction();
            req.Message = message;
            req.To = type;
            Session.Send(req);
        }

        //----------------------------------------------------------------------------------------------------
        public override void Update()
        {
            base.Update();
            if (ping_task.Update(Layer.CurrentIntervalMS))
            {
                Session.Send(new Ping());
            }
        }

        //----------------------------------------------------------------------------------------------------

        protected virtual void callback_sessionOpened(INetSession session)
        {
            this.QueueTask((AbstractBattle client) =>
            {
                var bc = client as NetConnectorBattle;
                log.Info("sessionOpened : " + session);
                if (bc.mConnected != null)
                {
                    bc.mConnected.Invoke(bc);
                }
                EnterRoomRequestC2B req = new EnterRoomRequestC2B();
                req.RoomID = bc.room.Room.RoomID;
                req.PlayerUUID = bc.room.PlayerUUID;
                req.Token = bc.token;
                req.TestToken = bc.testToken;
                session.Send(req);
            });
        }

        protected virtual void callback_sessionClosed(INetSession session)
        {
            this.QueueTask((AbstractBattle client) =>
            {
                var bc = client as NetConnectorBattle;
                log.Info("sessionClosed : " + session);
                if (bc.mDisconnectd != null)
                {
                    bc.mDisconnectd.Invoke(bc);
                }
            });
        }

        protected virtual void callback_messageReceived(INetSession session, object data)
        {
            //             if (data is Pong)
            //             {
            //                 uint ctime = (uint)CUtils.CurrentTimeMS;;
            //                 Pong pong = data as Pong;
            //                 this.current_ping = (int)(ctime - pong.ClientTimeDayOfMS);
            //             }
//             if (data is KickedByServerNotifyB2C)
//             {
//                 this.kicked = data as KickedByServerNotifyB2C;
//             }
            if (data is EnterRoomResponseB2C)
            {
                EnterRoomResponseB2C enter = data as EnterRoomResponseB2C;
                if (enter.Result == EnterRoomResponseB2C.RESULT_OK)
                {
                    CreateRoomInfoR2B cr = enter.RoomData as CreateRoomInfoR2B;
                }
            }

            this.Layer.ProcessMessage(data as IMessage);
            this.QueueTask((AbstractBattle client) =>
            {
                var bc = client as NetConnectorBattle;
                if (bc.mHandleMessage != null)
                {
                    bc.mHandleMessage.Invoke(bc, data as IMessage);
                }
            });
        }

        protected virtual void callback_messageSent(INetSession session, object data)
        {

        }

        protected virtual void callback_onError(INetSession session, Exception err)
        {
            log.Error("onError : " + err.Message, err);
            this.QueueTask((AbstractBattle client) =>
            {
                var bc = client as NetConnectorBattle;
                if (bc.mHandleError != null)
                {
                    bc.mHandleError.Invoke(bc, err);
                }
            });
        }
        #region Delegate

        public delegate void OnHandleMessage(NetConnectorBattle bc, IMessage msg);
        public delegate void OnDisconnectd(NetConnectorBattle bc);
        public delegate void OnConnected(NetConnectorBattle bc);
        public delegate void OnError(NetConnectorBattle bc, Exception err);

        public event OnHandleMessage HandleMessage { add { mHandleMessage += value; } remove { mHandleMessage -= value; } }
        public event OnDisconnectd Disconnectd { add { mDisconnectd += value; } remove { mDisconnectd -= value; } }
        public event OnConnected Connected { add { mConnected += value; } remove { mConnected -= value; } }
        public event OnError HandleError { add { mHandleError += value; } remove { mHandleError -= value; } }

        private OnHandleMessage mHandleMessage;
        private OnDisconnectd mDisconnectd;
        private OnConnected mConnected;
        private OnError mHandleError;

        #endregion

    }
}
