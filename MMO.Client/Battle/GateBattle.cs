using DeepCore;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Log;
using DeepCore.NetClient;
using DeepCore.Protocol;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using Gate.Data.Protocol;
using System;

namespace Gate.Client.Battle
{
    public class GateBattle : AbstractBattle
    {
        protected readonly Logger log;
        protected readonly GateClient client;
        protected readonly INetClient game_client;
        protected readonly ClientEnterZoneNotify enter;
        private long sent_package = 0;
        private long recv_package = 0;
        private TimeInterval<int> ping_interval = new TimeInterval<int>(3000);
        private TimeInterval<int> post_interval;
        private PackAction post_queue = new PackAction();
        public GateClient Client { get => client; }
        public override bool IsNet { get { return true; } }
        public override long RecvPackages { get { return recv_package; } }
        public override long SendPackages { get { return sent_package; } }
        public float PingIntervalMS
        {
            get { return ping_interval.IntervalTimeMS; }
            set { ping_interval = new TimeInterval<int>(Math.Max(1000, value)); }
        }
        public string ZoneUUID { get => enter.s2c_ZoneUUID; }
        public ClientEnterZoneNotify Enter { get { return enter; } }

        public GateBattle(GateClient client, ZoneSlaveFactory slaveFactory, ClientEnterZoneNotify sd)
            : base(MMOClientManager.Instance.Battle.DataRoot, slaveFactory)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.client = client;
            this.game_client = client.GameSession;
            this.enter = sd;
            this.post_interval = new TimeInterval<int>(sd.s2c_ZoneUpdateIntervalMS);
            this.Layer.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
            this.QueueTask(t => OnStart?.Invoke(this));
        }
        protected override void Disposing()
        {
            this.OnEnd?.Invoke(this);
            base.Disposing();
            this.OnStart = null;
            this.OnEnd = null;
            this.OnError = null;
        }
        public override string ToString()
        {
            return string.Format("{1}({0})", enter.s2c_ZoneUUID, Layer.Data);
        }
        protected internal virtual void OnReceived(ClientBattleEvent notify)
        {
            try
            {
                if (MMOClientManager.Instance.Battle.Codec.DoDecode(new ArraySegment<byte>(notify.s2c_battleEvent), out var evt))
                {
                    recv_package++;
                    Layer.QueueMessage(evt as IBattleMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                OnError?.Invoke(this, err);
            }
        }
        protected virtual void SendToServer(object message)
        {
            try
            {
                if (MMOClientManager.Instance.Battle.Codec.DoEncode(message, out var bin))
                {
                    game_client.Notify(new ClientBattleAction() { c2s_battleAction = bin.Array });
                    sent_package++;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                OnError?.Invoke(this, err);
            }
        }
        public override bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            sdata = null;
            return false;
        }
        public override void SendAction(BattleAction action)
        {
            post_queue.actions.Add(action);
            action.Retain();
        }
        public override void BeginUpdate(float intervalMS)
        {
            base.BeginUpdate(intervalMS);
            if (post_interval.Update(intervalMS))
            {
                if (post_queue.actions.Count > 0)
                {
                    try
                    {
                        if (Actor != null)
                        {
                            post_queue.object_id = Actor.ObjectID;
                        }
                        SendToServer(post_queue);
                        foreach(var e in post_queue.actions)
                        {
                            e.Release();
                        }
                    }
                    finally
                    {
                        post_queue.actions.Clear();
                    }
                }
            }
        }
        public override void Start()
        {

        }
        public override void Update()
        {
            base.Update();
            if (ping_interval.Update(Layer.CurrentIntervalMS))
            {
                SendToServer(new Ping());
            }
        }
        public override event BattleStart OnStart;
        public override event BattleEnd OnEnd;
        public override event BattleError OnError;
    }
}
