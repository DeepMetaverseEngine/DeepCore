using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Log;
using DeepCore.MinaClient.Sockets;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.ZoneServer.Client;
using DeepMetaGame.ZoneServer.Message;

namespace DeepMetaGame.ZoneServer.Bot
{

    public class BotPlayer : IDisposable
    {
        private static Random random = new Random();
        private readonly Logger log;
        private readonly string mName;
        private readonly NetTestClient mClient;
        private readonly int mFixedIntervalMS;

        private System.Threading.Timer mTimer;
        private double mLastUpdateTime;


        public string Name { get { return mName; } }

        public bool IsRunning { get { return mClient.Client.Session.IsConnected; } }

        public BotPlayer(string name, string roomID, UnitInfo unit, int force, EditorTemplates dataroot, string connectString, ZoneSlaveFactory slaveFactory)
        {
            this.log = LoggerFactory.GetLogger("Bot[" + name + "]");

            var ret = new CreateUnitInfoR2B();
            ret.UnitTemplateID = unit.ID;
            ret.Force = (byte)force;

            this.mFixedIntervalMS = (1000 / dataroot.Templates.DefaultConfig.SYSTEM_FPS);
            this.mName = name;
            this.mClient = new NetTestClient(name,
                roomID,
                connectString,
                mFixedIntervalMS,
                20, ret, false, null, dataroot, slaveFactory);
            this.mClient.Client.Layer.ActorAdded += Layer_ActorAdded;
            this.mClient.Client.Disconnectd += Client_Disconnectd;

        }

        public void Start()
        {
            this.mClient.Start();
            this.mTimer = new System.Threading.Timer(update, this, mFixedIntervalMS, mFixedIntervalMS);
        }
        public void Stop()
        {
            try
            {
                mClient.Stop();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public void SendLeaveRoom()
        {
            try
            {
                mClient.Client.SendLeaveRoom();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public void Dispose()
        {
            try
            {
                lock (this)
                {
                    mTimer.Dispose();
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            try
            {
                mClient.Stop();
                mClient.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }




        private void Client_Disconnectd(BattleClient bc)
        {
            this.Dispose();
        }

        private void Layer_ActorAdded(LayerZone layer, LayerPlayer actor)
        {
            actor.SendUnitGuard(true);
            startMoveToTarget();
        }

        private void update(object state)
        {
            lock (this)
            {
                var curTime = CUtils.TickTimeMS;
                if (mLastUpdateTime == 0)
                {
                    mLastUpdateTime = curTime;
                }
                var intervalMS = (float)(curTime - mLastUpdateTime);
                this.mLastUpdateTime = curTime;
                try
                {
                    updateInTarget(intervalMS);
                    this.mClient.Update(intervalMS);
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
            }
        }

        private TimeExpire<RegionData> target_expire;

        private void startMoveToTarget()
        {
            var list = new List<RegionData>(mClient.Client.Layer.Data.Regions);
            CUtils.RandomList(random, list);
            foreach (var rd in list)
            {
                var abs = rd.GetAbilities();
                if (abs != null)
                {
                    foreach (var ab in abs)
                    {
                        if (ab is SpawnUnitAbilityData)
                        {
                            var suab = ab as SpawnUnitAbilityData;
                            if (suab.Force != mClient.Client.Actor.Force)
                            {
                                target_expire = new TimeExpire<RegionData>().Init(random.Next(10000, 100000), rd);
                                mClient.Client.Actor.SendUnitAttackMoveTo(rd.Position, rd.Name, true);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void updateInTarget(float intervalMS)
        {
            if (target_expire != null && target_expire.Update(intervalMS))
            {
                var rg = target_expire.Tag;
                var actor = mClient.Client.Actor;
                startMoveToTarget();
            }
        }
    }
}
