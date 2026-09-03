using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCrystal;
using DeepCrystal.Server;
using DeepCrystal.SharpMinaServer;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Server
{
    //-------------------------------------------------------------------------------------------------

    public class ServerNode : Disposable, IMinaServerListener, IZoneNodeServer
    {
        protected readonly Logger log = LoggerFactory.GetLogger("ServerNode");
        private readonly IMinaServerFactory mServerFactory;
        private readonly EditorTemplates mDataRoot;
        private readonly int mSceneID;
        private readonly ServerCodec mCodec;
        private ZoneNode mNode;
        private IMinaServer mServer;
        public ServerNode(EditorTemplates dataroot, int sceneID, ServerConfig sconfig,ZoneHostFactory hostFactory)
        {

            this.mDataRoot = dataroot;
            this.mSceneID = sceneID;

            this.mCodec = new ServerCodec(dataroot.Templates);

            this.mServerFactory = new DeepFrozen.Server.NetUV.MinaServer.UVMinaServerFactory();
           // this.mServerFactory = new DeepFrozen.Server.SSocket.NetServer.SSMinaServerFactory();
            this.mServer = mServerFactory.CreateServer(sconfig, mCodec);

            this.mNode = hostFactory.CreateServerZoneNode(this, dataroot);
            this.mNode.OnZoneStart += MNode_OnZoneStart;
            this.mNode.OnZoneStop += MNode_OnZoneStop;
        }

        public Task<InstanceZone> StartAsync()
        {
            var data = mNode.DataRoot.LoadScene(mSceneID, true, false);
            return this.mNode.StartAsync(data);
        }
        public Task StopAsync()
        {
            mServer.Close();
            return mNode.StopAsync();
        }

        protected override void Disposing()
        {
            mServer.Close();
            mServer = null;
            mNode.Stop();
            mNode = null;
        }

        //-------------------------------------------------------------------------------------------------

        public ZoneNode Node
        {
            get { return mNode; }
        }
        public IMinaServer Server
        {
            get { return mServer; }
        }
        public EditorTemplates DataRoot
        {
            get { return mDataRoot; }
        }
        public bool Running
        {
            get { return mNode != null && mNode.IsRunning; }
        }
        public ServerCodec Codec
        {
            get { return mCodec; }
        }

        //-------------------------------------------------------------------------------------------------

        private void InternalOpen()
        {
            try
            {
                this.mServer.Open(this);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        private void MNode_OnZoneStop(InstanceZone zone)
        {
        }
        private void MNode_OnZoneStart(InstanceZone zone)
        {
            new Thread(InternalOpen).Start();
        }

        //-------------------------------------------------------------------------------------------------
        #region IServerListener

        void IMinaServerListener.OnInit(IMinaServer server)
        {
        }
        void IMinaServerListener.OnDestory()
        {
        }
        IMinaSessionListener IMinaServerListener.OnSessionConnected(IMinaSession session)
        {
            return new SessionPlayer(session, mNode);
        }

        #endregion
        //-------------------------------------------------------------------------------------------------
        #region IZoneNodeServer

        private GameServerCallHandler zone_rpc_call_handler;
        private GameServerMessageHandler zone_rpc_invoke_handler;
        void IZoneNodeServer.PostToGameServer(object msg)
        {
        }
        void IZoneNodeServer.PostToGameServer(object msg, Action<object, Exception> callback)
        {
        }
        event GameServerMessageHandler IZoneNodeServer.HandleGameServerInvoke
        {
            add { zone_rpc_invoke_handler += value; }
            remove { zone_rpc_invoke_handler -= value; }
        }
        event GameServerCallHandler IZoneNodeServer.HandleGameServerCall
        {
            add { zone_rpc_call_handler += value; }
            remove { zone_rpc_call_handler -= value; }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------




    }

    //-------------------------------------------------------------------------------------------------

    public class ServerCodec : BattleCodec
    {
        private InputStream mInputStream;
        private OutputStream mOutputStream;
        private DeepCore.IO.MemoryStream mBinBufferIn;
        private DeepCore.IO.MemoryStream mBinBufferOut;

        private HashMap<Type, Record> mTypeBytes = new HashMap<Type, Record>();

        public ServerCodec(TemplateManager templates)
            : base(templates)
        {
            this.mInputStream = new InputStream(null, Factory);
            this.mOutputStream = new OutputStream(null, Factory);
            this.mBinBufferIn = new DeepCore.IO.MemoryStream(1024);
            this.mBinBufferOut = new DeepCore.IO.MemoryStream(1024);
        }

        public override bool DoDecode(Stream input, out object message)
        {
            lock (mInputStream)
            {
                mInputStream.SetStream(input);
                return base.doDecode(mInputStream, out message);
            }
        }

        public override bool DoEncode(Stream output, object message)
        {
            bool ret;
            long len;
            lock (mOutputStream)
            {
                long pos = output.Position;
                mOutputStream.SetStream(output);
                ret = base.doEncode(mOutputStream, message);
                len = output.Position - pos;
            }
            if (ret)
            {
                var type = message.GetType();
                if (mTypeBytes.TryGetValue(type, out var rec))
                {
                    rec.Bytes += len;
                    rec.Count++;
                    mTypeBytes.Put(type, rec);
                }
                else
                {
                    rec.Bytes = len;
                    rec.Count = 1;
                    mTypeBytes.Put(type, rec);
                }
            }
            return ret;
        }

        public HashMap<Type, Record> GetSentTypeBytes()
        {
            lock (mOutputStream)
            {
                return new HashMap<Type, Record>(mTypeBytes);
            }
        }

        public struct Record
        {
            public long Bytes;
            public int Count;
        }
    }

    //-------------------------------------------------------------------------------------------------

    internal class SessionPlayer : IZoneNodeSession, IMinaSessionListener
    {
        private static Logger log = LoggerFactory.GetLogger("SessionPlayer");

        private readonly ZoneNode node;
        private readonly IMinaSession session;
        private readonly BattleCodec codec;

        private EnterRoomRequestC2B login_data;
        private string player_uuid;
        private string display_name;
        private int unit_template_id;
        private byte force;



        public SessionPlayer(IMinaSession session, ZoneNode node)
        {
            this.node = node;
            this.session = session;
            this.codec = new BattleCodec(node.Templates);
        }

        //----------------------------------------------------------------------------
        #region IZoneNodePlayer

        public string PlayerUUID { get { return player_uuid; } }
        public string DisplayName { get { return display_name; } }
        public ZoneNode.PlayerClient BindingPlayer { get; set; }
        private AtomicReference<PackNotify> send_queue = new AtomicReference<PackNotify>(null);
        void IZoneNodeSession.ClientSend(PlayerMessageEntry msg, bool immediately)
        {
            if (immediately)
            {
                session.Send(msg.message);
            }
            else
            {
                var queue = send_queue.GetOrCreate(() => { return new PackNotify(); });
                if (msg.buffer != null)
                    queue.events.Add(msg.buffer);
                else if (msg.message != null)
                    queue.events.Add(msg.message);
            }
        }
        void IZoneNodeSession.ClientFlush(BattleCodec codec)
        {
            var queue = send_queue.GetAndSet(null);
            if (queue != null)
            {
                session.Send(queue);
            }
        }
        void IZoneNodeSession.OnPlayerConnected(ZoneNode.PlayerClient binding)
        {
        }
        void IZoneNodeSession.OnPlayerDisconnect(ZoneNode.PlayerClient binding)
        {
            mHandleClientMessage = null;
        }
        void IZoneNodeSession.OnPlayerDisposed()
        {
        }
        void IZoneNodeSession.PostToGameServer(object msg)
        {
        }
        void IZoneNodeSession.PostToGameServer(object msg, Action<object, Exception> callback)
        {
        }


        private ClientMessageHandler mHandleClientMessage;
        private GameServerCallHandler mRpcHandleCall;
        private GameServerMessageHandler mRpcHandleInvoke;
        event ClientMessageHandler IZoneNodeSession.HandleClientMessage
        {
            add { mHandleClientMessage += value; }
            remove { mHandleClientMessage -= value; }
        }
        event GameServerMessageHandler IZoneNodeSession.HandleGameServerMessage
        {
            add { mRpcHandleInvoke += value; }
            remove { mRpcHandleInvoke -= value; }
        }
        event GameServerCallHandler IZoneNodeSession.HandleGameServerCall
        {
            add { mRpcHandleCall += value; }
            remove { mRpcHandleCall -= value; }
        }

        #endregion
        //----------------------------------------------------------------------------
        #region ISessionListener

        void IMinaSessionListener.OnConnected(IMinaSession session)
        {
            log.Info("OnConnected : " + session);
        }
        void IMinaSessionListener.OnDisconnected(IMinaSession session, string reason)
        {
            log.Info("OnDisconnected : " + session);
            if (node != null)
            {
                if (login_data != null)
                {
                    login_data = null;
                    node.PlayerLeave(this, (c, e) => { }, true);
                }
            }
        }
        void IMinaSessionListener.OnError(IMinaSession session, Exception err)
        {
            log.Error("OnError : " + session, err);
        }
        void IMinaSessionListener.OnSentMessage(IMinaSession session, object message)
        {
        }
        void IMinaSessionListener.OnReceivedMessage(IMinaSession session, object message)
        {
            try
            {
                if (message is EnterRoomRequestC2B)
                {
                    do_EnterRoomResponseB2C(message as EnterRoomRequestC2B);
                }
                else if (message is LeaveRoomRequestC2B)
                {
                    do_LeaveRoomRequestC2B(message as LeaveRoomRequestC2B);
                }
                else if (mHandleClientMessage != null)
                {
                    mHandleClientMessage(message as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        #endregion


        private void do_EnterRoomResponseB2C(EnterRoomRequestC2B req)
        {
            this.login_data = req;
            this.player_uuid = req.PlayerUUID;
            this.display_name = req.TestToken.PlayerDisplayName;
            this.unit_template_id = req.TestToken.Data.UnitTemplateID;
            this.force = req.TestToken.Data.Force;
            var temp = node.Templates.GetUnit(unit_template_id);
            var add = new TAddUnit()
            {
                info = temp,
                editor_name = "",
                player_uuid = req.PlayerUUID,
                force = force,
                level = 0,
                pos = null,
                direction = 0
            };
            node.PlayerEnter(this, add, (c, e) => { }, true);
        }

        private void do_LeaveRoomRequestC2B(LeaveRoomRequestC2B req)
        {
            if (login_data != null)
            {
                login_data = null;
                node.PlayerLeave(this, (c, e) => { }, false);
                this.session.Disconnect(false);
            }
        }

    }

}
