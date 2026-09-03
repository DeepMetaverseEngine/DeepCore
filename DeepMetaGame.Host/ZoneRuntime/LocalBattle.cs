using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Slave;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    public abstract class LocalBattle : InstanceBattle, InstanceZoneListener
    {
        private int mRecvPack = 0;
        private int mSendPack = 0;
        private InstanceUnit mSender;
        public EditorScene Zone { get; private set; }
        public override long RecvPackages { get { return mRecvPack; } }
        public override long SendPackages { get { return mSendPack; } }
        public override bool IsNet { get { return false; } }
        public bool IsLocalBattle => true;
        public SceneData SceneData { get { return Zone.Data; } }
        public ZoneHostFactory HostFactory { get; }
        public LocalBattle(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory) : base(datas, slaveFactory)
        {
            this.HostFactory = hostFactory;
            this.Layer.ActorSyncMode = SyncMode.ForceByServer;
            this.Layer.MessageReceived += Layer_MessageReceived;
        }
        protected override void Disposing()
        {
            this.OnEnd?.Invoke(this);
            {
                this.OnStart = null;
                this.OnEnd = null;
                this.OnError = null;
                this.OnCrateZone = null;
            }
            base.Disposing();
            this.mSender = null;
            this.Zone.Dispose();
            this.Zone = null;
        }
        public override void LowMemory()
        {
            if (IsDisposing) return;
            base.LowMemory();
            Zone?.ObjectPool.LowMemory();
        }
        protected abstract EditorScene CreateZone();
        public override void Start()
        {
            this.Zone = CreateZone();
            this.Zone.SyncPos = false;
            this.QueueTask(t =>
            {
                OnZoneStart?.Invoke(this, Zone);
                OnStart?.Invoke(this);
            });
            var enter = this.Layer.ObjectPool.Alloc<ClientEnterScene>().Init(
                Zone.UUID,
                SceneData.ID,
                Zone.SpaceDivSizeW,
                Zone.Gravity,
                Zone.Terrain3D.StepIntercept,
                DataRoot.Templates.ResourceVersion,
                Zone.GetLayerInitData());
            enter.sender = Zone;
            this.Layer.QueueMessage(enter);
        }

        protected virtual void Layer_MessageReceived(Slave.Layer.LayerZone layer, IBattleMessage msg)
        {
            if (msg is ZonePauseNotify pause)
            {
                if (pause.Pause.HasValue)
                {
                    this.Pause = pause.Pause.Value;
                }
                if (pause.TimeScale.HasValue)
                {
                    this.TimeScale = pause.TimeScale.Value;
                }
            }
        }
        public override void BeginUpdate(float intervalMS)
        {
            if (TimeScale != 1)
            {
                intervalMS = Math.Max(1, (float)(intervalMS * TimeScale));
            }
            base.BeginUpdate(intervalMS);
        }
        public override void Update()
        {
            if (this.Zone != null)
            {
                if (!Pause)
                {
                    this.Zone.Update(this.Layer.CurrentIntervalMS);
                }
                else
                {
                    this.Zone.Update(0);
                }
            }
            base.Update();
        }

        public override bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            sdata = Zone.SceneData;
            return true;
        }
        public override void SendAction(BattleAction action)
        {
            if (mSender == null && Layer.Actor != null)
            {
                mSender = Zone.GetUnit(Layer.Actor.ObjectID);
            }
            Zone.EnqueueAction(action, mSender);
            mSendPack++;
        }
        void InstanceZoneListener.OnCreateZone(InstanceZone zone)
        {
            OnCrateZone?.Invoke(this, zone);
        }
        public virtual void OnEventHandler(IReadOnlyList<BattleNotify> events)
        {
            for (var i = 0; i < events.Count; i++)
            {
                mRecvPack++;
                Layer.QueueMessage(events[i]);
            }
        }

        protected virtual void OnAddLocalPlayer(InstancePlayer actor)
        {
            var zone = actor.Parent;
            //actor.ClientSyncMode = SyncMode.ForceByServer;
            var loc = actor.AllocLockActorEvent(actor.Name,
                      zone.CFG.CLIENT_SYNC_UNIT_MIN_RANGE,
                      zone.CFG.CLIENT_SYNC_UNIT_MAX_RANGE,
                      1000 / zone.CFG.SYSTEM_FPS);
            Layer.QueueMessage(loc);
            //actor.SetSyncMode(SyncMode.ForceByServer);
            //Layer.ActorSyncMode = SyncMode.ForceByServer;
        }

        protected virtual void call_OnError(Exception err)
        {
            OnError?.Invoke(this, err);
        }

        public override event Action<InstanceBattle, InstanceZone> OnCrateZone;
        public override event Action<InstanceBattle, InstanceZone> OnZoneStart;
        public override event BattleStart OnStart;
        public override event BattleEnd OnEnd;
        public override event BattleError OnError;
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    public class LocalBattlePlay : LocalBattle
    {
        private SceneData scene;
        public LocalBattlePlay(EditorTemplates data_root, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData scene)
            : base(data_root, hostFactory, slaveFactory)
        {
            this.scene = scene;
        }
        public override string ToString()
        {
            return $"LocalBattle:{scene}";
        }
        protected override EditorScene CreateZone()
        {
            return HostFactory.CreateZone(this, DataRoot, scene);
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.scene = null;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    public class LocalBattleSinglePlay : LocalBattlePlay
    {
        private int? force = null;
        private int? actorTemplateID = null;
        public InstancePlayer ActorPlayer { get; protected set; }
        public LocalBattleSinglePlay(EditorTemplates data_root, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData scene, int? force = null, int? actorTemplateID = null)
            : base(data_root, hostFactory, slaveFactory, scene)
        {
            this.force = force;
            this.actorTemplateID = actorTemplateID;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.ActorPlayer = null;
        }
        public override void Start()
        {
            base.Start();
            this.Zone.QueueTask(this, static (t, z) =>
            {
                z.DoAddLocalPlayer(z.Zone);
            });
        }
        protected override void OnAddLocalPlayer(InstancePlayer actor)
        {
            this.ActorPlayer = actor;
            base.OnAddLocalPlayer(actor);
        }
        protected virtual InstancePlayer DoAddLocalPlayer(InstanceZone zone)
        {
            var actor = Zone.InitPlayerStartRegions(force, actorTemplateID);
            if (actor != null)
            {
                OnAddLocalPlayer(actor);
            }
            return actor;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------
}
