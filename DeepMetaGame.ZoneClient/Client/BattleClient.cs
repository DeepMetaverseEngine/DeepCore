using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.MinaClient;
using DeepCore.Net;
using DeepCore.Protocol;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Client
{
    public abstract class BattleClient : AbstractBattle
    {
        protected readonly Logger log;
        protected readonly PlayerWillConnectResponseB2R room;
        protected readonly PlayerWillConnectRequestR2B testToken;
        private KickedByServerNotifyB2C kicked;
        private TimeInterval<int> ping_task = new TimeInterval<int>(5000);
        public string token { get; private set; }
        public string data_root { get; private set; }
        public abstract IMinaClientSession Session { get; }
        public string PlayerUUID { get { return room.PlayerUUID; } }
        public MessageFactoryGenerator MessageFactory { get; private set; }
        public override long RecvPackages { get { return Session.TotalRecvPackages; } }
        public override long SendPackages { get { return Session.TotalSentPackages; } }
        public float PingIntervalMS
        {
            get
            {
                return ping_task.IntervalTimeMS;
            }
            set
            {
                ping_task = new TimeInterval<int>(Math.Max(1000, value));
            }
        }
        //         public override KickedByServerNotifyB2C KickMessage
        //         {
        //             get { return kicked; }
        //         }
        public override bool IsNet { get { return true; } }

        public BattleClient(
            EditorTemplates datas,
            ZoneSlaveFactory slaveFactory,
            MessageFactoryGenerator msgFactory,
            PlayerWillConnectResponseB2R room,
            PlayerWillConnectRequestR2B testToken,
            string token)
            : base(datas, slaveFactory)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
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
            this.OnStart = null;
            this.OnEnd = null;
            this.OnError = null;
        }
        //----------------------------------------------------------------------------------------------------

        abstract public void Stop();
        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// 发送单位控制命令
        /// </summary>
        /// <param name="action"></param>
        public override void SendAction(BattleAction action)
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

        protected virtual void callback_sessionOpened(IMinaClientSession session)
        {
            this.QueueTask((AbstractBattle client) =>
            {
                BattleClient bc = client as BattleClient;
                log.Info("sessionOpened : " + session);
                if (bc?.mConnected != null)
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

        protected virtual void callback_sessionClosed(IMinaClientSession session)
        {
            this.QueueTask((AbstractBattle client) =>
            {
                BattleClient bc = client as BattleClient;
                log.Info("sessionClosed : " + session);
                if (bc?.mDisconnectd != null)
                {
                    bc.mDisconnectd.Invoke(bc);
                }
            });
        }

        protected virtual void callback_messageReceived(IMinaClientSession session, object data)
        {
            if (data is KickedByServerNotifyB2C)
            {
                this.kicked = data as KickedByServerNotifyB2C;
            }
            if (data is EnterRoomResponseB2C)
            {
                EnterRoomResponseB2C enter = data as EnterRoomResponseB2C;
                if (enter.Result == EnterRoomResponseB2C.RESULT_OK)
                {
                    CreateRoomInfoR2B cr = enter.RoomData as CreateRoomInfoR2B;
                }
            }
            this.Layer.QueueMessage(data as IBattleMessage);
            if (mHandleMessage != null)
            {
                this.QueueTask((client) =>
                {
                    mHandleMessage?.Invoke(client as BattleClient, data as IBattleMessage);
                });
            }
        }

        protected virtual void callback_messageSent(IMinaClientSession session, object data)
        {

        }

        protected virtual void callback_onError(IMinaClientSession session, Exception err)
        {
            log.Error("onError : " + err.Message, err);
            this.QueueTask((client) =>
            {
                OnError?.Invoke(this, err);
            });
        }


        #region Delegate

        public delegate void OnHandleMessage(BattleClient bc, IBattleMessage msg);
        public delegate void OnDisconnectd(BattleClient bc);
        public delegate void OnConnected(BattleClient bc);

        public event OnHandleMessage HandleMessage { add { mHandleMessage += value; } remove { mHandleMessage -= value; } }
        public event OnDisconnectd Disconnectd { add { mDisconnectd += value; } remove { mDisconnectd -= value; } }
        public event OnConnected Connected { add { mConnected += value; } remove { mConnected -= value; } }

        public override event BattleStart OnStart;
        public override event BattleEnd OnEnd;
        public override event BattleError OnError;

        private OnHandleMessage mHandleMessage;
        private OnDisconnectd mDisconnectd;
        private OnConnected mConnected;

        #endregion


        public static BattleClient CreateBattleClient(
            string netAddress,
            string playerUUID,
            string roomID,
            int unitTemplateID,
            byte force,
            EditorTemplates data_root,
            ZoneSlaveFactory slaveFactory)
        {
            var netDirver = typeof(DeepCore.MinaClient.Sockets.MinaSocketSession).FullName;
            var connectString = netAddress;
            Console.WriteLine("     PlayerUUID = " + playerUUID);
            Console.WriteLine("         RoomID = " + roomID);
            Console.WriteLine("      NetDirver = " + netDirver);
            Console.WriteLine("  ConnectString = " + connectString);
            Console.WriteLine(" UnitTemplateID = " + unitTemplateID);
            Console.WriteLine("          Force = " + force);

            //UnitInfo temp = TemplateManager.Instance.getUnit(unit.UnitTemplateID);
            var factory = ZoneDataFactory.Factory.MessageCodec as MessageFactoryGenerator;
            var mDataRoot = data_root;

            var unit = new CreateUnitInfoR2B();
            unit.UnitTemplateID = unitTemplateID;
            unit.Force = (byte)force;

            if (unit.UnitPropData == null)
            {
                UnitInfo temp = mDataRoot.Templates.GetUnit(unit.UnitTemplateID);
                unit.UnitPropData = IOUtil.Clone(ZoneDataFactory.Factory.PersistCodec, temp.Properties);
            }

            PlayerWillConnectResponseB2R room = new PlayerWillConnectResponseB2R();
            room.PlayerUUID = playerUUID;
            room.Room = new RoomInfo();
            room.Room.ClientConnectString = connectString;
            room.Room.Dummy = 0;
            room.Room.RoomID = roomID;

            PlayerWillConnectRequestR2B enter = new PlayerWillConnectRequestR2B();
            {
                enter.PlayerUUID = playerUUID;
                enter.PlayerDisplayName = playerUUID;
                enter.Token = playerUUID;
                enter.TokenValidTimeSec = int.MaxValue;
                enter.RoomID = roomID;
                enter.Data = unit; //IOUtil.ObjectToBin(factory, unit);
            }
            return new BattleClientDirect(mDataRoot, slaveFactory, factory, room, enter, playerUUID);
        }


    }
}
