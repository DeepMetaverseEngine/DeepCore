using DeepCore.IO;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Client
{
    public class NetTestClient : IDisposable
    {
        public BattleClient Client { get { return mClient; } }
        public EditorTemplates DataRoot { get { return mDataRoot; } }

        private readonly BattleClient mClient;
        private readonly EditorTemplates mDataRoot;

        public NetTestClient(
          string playerUUID,
          string roomID,
          string netDirver,
          string connectString,
          int intervalMS,
          int syncRange,
          CreateUnitInfoR2B unit,
          EditorTemplates data_root)
            : this(playerUUID, roomID, netDirver, connectString, intervalMS, syncRange, unit, false, null, data_root)
        { }

        public NetTestClient(
            string playerUUID,
            string roomID,
            string netDirver,
            string connectString,
            int intervalMS,
            int syncRange,
            CreateUnitInfoR2B unit,
            bool isProxy,
            string proxyConnectString,
            EditorTemplates data_root)
        {

            Console.WriteLine("     PlayerUUID = " + playerUUID);
            Console.WriteLine("         RoomID = " + roomID);
            Console.WriteLine("      NetDirver = " + netDirver);
            Console.WriteLine("  ConnectString = " + connectString);
            Console.WriteLine("     IntervalMS = " + intervalMS);
            Console.WriteLine("      SyncRange = " + syncRange);
            Console.WriteLine(" UnitTemplateID = " + unit.UnitTemplateID);
            Console.WriteLine("          Force = " + unit.Force);
            Console.WriteLine("        IsProxy = " + isProxy);

            //UnitInfo temp = TemplateManager.Instance.getUnit(unit.UnitTemplateID);
            MessageFactoryGenerator factory = TemplateManager.DataFactory.MessageCodec as MessageFactoryGenerator;
            this.mDataRoot = data_root;

            if (unit.UnitPropData == null)
            {
                UnitInfo temp = mDataRoot.Templates.GetUnit(unit.UnitTemplateID);
                unit.UnitPropData = IOUtil.Clone(TemplateManager.DataFactory.PersistCodec, temp.Properties);
            }

            PlayerWillConnectResponseB2R room = new PlayerWillConnectResponseB2R();
            room.PlayerUUID = playerUUID;
            room.Room = new RoomInfo();
            room.Room.ClientConnectString = connectString;
            room.Room.NetDriverString = netDirver;
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

            if (isProxy)
            {
                this.mClient = new BattleClientProxy(
                    new TestProxySession(),
                    proxyConnectString,
                    mDataRoot, factory, room, enter, playerUUID);
            }
            else
            {
                this.mClient = new BattleClientDirect(
                    mDataRoot, factory, room, enter, playerUUID);
            }
        }

        public void Start()
        {
            this.mClient.Start();
        }
        public void Stop()
        {
            this.mClient.Stop();
        }
        public void Dispose()
        {
            this.mClient.Dispose();
        }
        public void Update(int intervalMS)
        {
            this.mClient.BeginUpdate(intervalMS);
            this.mClient.Update();
        }

        //--------------------------------------------------------------------------------------------

        public class TestProxySession : BattleClientProxy.ProxyNetSession
        {
            public override bool IsMessageTypeGS(object msg)
            {
                return false;
            }
        }

    }
}
