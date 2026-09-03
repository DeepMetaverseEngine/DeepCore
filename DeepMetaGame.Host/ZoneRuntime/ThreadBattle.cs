using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Slave;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    public class ThreadBattle : InstanceBattle
    {
        protected readonly static Logger log = new LazyLogger(typeof(ThreadBattle));
        public override bool IsNet => true;
        public override long RecvPackages => 0;
        public override long SendPackages => 0;

        private BattleThread thread;
        private BattleCodec codec;
        private ObjectPool<MemoryStream> mempool = new ConcurrentObjectPool<MemoryStream>();
        private SceneData data;
        public virtual int FPS { get; }
        public ZoneHostFactory HostFactory { get; }
        public ThreadBattle(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData sd)
            : base(datas, slaveFactory)
        {
            this.HostFactory = hostFactory;
            this.Layer.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
            this.Layer.MessageReceived += Layer_MessageReceived;
            this.data = sd;
            this.FPS = datas.Templates.DefaultConfig.SYSTEM_FPS;
            if (sd.OverrideConfig != null)
            {
                this.FPS = sd.OverrideConfig.SYSTEM_FPS;
            }
            this.codec = new BattleCodec(datas.Templates, true);
            this.codec.ObjectPool = Layer.ObjectPool;
        }
        public override string ToString()
        {
            return $"ThreadBattle:{data}";
        }
        protected override void Disposing()
        {
            this.thread.Stop();
            this.Update();
            base.Disposing();
            this.OnStart = null;
            this.OnEnd = null;
            this.OnError = null;
            this.OnZoneStart = null;
            this.OnCrateZone = null;
            this.thread = null;
            this.codec.Dispose();
            this.mempool.Dispose();
        }
        public override void Start()
        {
            this.thread = new BattleThread(this, data);
            this.QueueTask(t =>
            {
                this.thread.Start();
            });
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
        public override void LowMemory()
        {
            if (IsDisposing) return;
            base.LowMemory();
            QueueZoneTask(static z =>
            {
                z.ObjectPool.LowMemory();
            });
        }
        public override bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            sdata = data;
            return true;
        }
        public override void BeginUpdate(float intervalMS)
        {
            if (TimeScale != 1)
            {
                intervalMS = Math.Max(1, (float)(intervalMS * TimeScale));
            }
            thread?.PostUpdate(intervalMS);
            base.BeginUpdate(intervalMS);
        }
        //-------------------------------------------------------------------------------------------------------
        protected virtual void b2c_OnStart(BattleThread zone)
        {
            OnZoneStart?.Invoke(this, zone.MainZone);
            QueueTask(() =>
            {
                OnStart?.Invoke(this);
            });
        }
        protected virtual void b2c_OnEnd(BattleThread zone)
        {
            QueueTask(() =>
            {
                OnEnd?.Invoke(this);
            });
        }
        protected virtual void b2c_OnError(BattleThread zone, Exception err)
        {
            log.Error(err);
            OnError?.Invoke(this, err);
        }
        // 从线程收到消息
        protected virtual void b2c_OnEvent(BattleThread zone, MemoryStream stream)
        {
            QueueTask((this, stream), static (b, st) =>
            {
                var battle = st.Item1;
                var stream = st.stream;
                try
                {
                    stream.Position = 0;
                    if (battle.codec.DoDecode(stream, out var msg))
                    {
                        if (msg is BattleNotify evt)
                        {
                            battle.Layer.QueueMessage(evt);
                            evt.Release();
                        }
                        else
                        {
                            // TODO other msg ?
                        }
                    }
                }
                finally
                {
                    battle.mempool.Release(stream);
                }
            });
        }
        protected virtual void b2c_DoPlayerEnter(BattleThread zone, InstancePlayer actor)
        {
            zone.b2c_sendToLayer(actor.AllocLockActorEvent(actor.Name,
                      zone.MainZone.CFG.CLIENT_SYNC_UNIT_MIN_RANGE,
                      zone.MainZone.CFG.CLIENT_SYNC_UNIT_MAX_RANGE,
                      1000 / FPS));
            zone.b2c_sendToLayer(actor.AllocSyncSkillActives());
            zone.b2c_sendToLayer(zone.MainZone.AllocSyncFlagsEvent());
            zone.b2c_sendToLayer(zone.MainZone.AllocSyncUnitsEvent(actor));
        }
        // 发送消息到线程
        public override void SendAction(BattleAction action)
        {
            var stream = mempool.Get(this, static (st, pool) => new MemoryStream());
            if (codec.DoEncode(stream, action))
            {
                this.thread.c2b_LayerAction(stream, Layer.ActorID);
            }
            else
            {
                mempool.Release(stream);
            }
        }
        //-------------------------------------------------------------------------------------------------------
        public override event BattleStart OnStart;
        public override event BattleEnd OnEnd;
        public override event BattleError OnError;
        public override event Action<InstanceBattle, InstanceZone> OnZoneStart;
        public override event Action<InstanceBattle, InstanceZone> OnCrateZone;
        //-------------------------------------------------------------------------------------------------------
        public void QueueZoneTask(Action<EditorScene> task)
        {
            if (IsDisposing) return;
            thread.task_queue.Enqueue(task);
        }
        public void QueueZoneTask<ST>(ST st, Action<EditorScene, ST> task)
        {
            if (IsDisposing) return;
            thread.task_queue.Enqueue(st, task);
        }
        //-------------------------------------------------------------------------------------------------------
        public struct PostUpdate
        {
            public float IntervalMS;
        }
        public class BattleThread : InstanceZoneListener
        {
            internal readonly Queue<PostUpdate> postUpdates = new();
            internal readonly MessageActionQueue<EditorScene> task_queue;
            private readonly ThreadBattle battle;
            private readonly SceneData scene;
            private readonly float fixedIntervalMS;
            private readonly Thread thread;
            private InstancePlayer Sender;
            private EditorScene Zone;
            private bool mIsRunning;
            private BattleCodec codec;
            public EditorScene MainZone { get => Zone; }
            public bool IsPause { get => battle.Pause; }
            public float TimeScale { get => battle.TimeScale; }
            public bool IsLocalBattle => false;
            public BattleThread(ThreadBattle battle, SceneData scene)
            {
                this.task_queue = new MessageActionQueue<EditorScene>();
                this.battle = battle;
                this.scene = scene;
                this.fixedIntervalMS = (float)(1000 / battle.FPS);
                this.thread = new Thread(main);
                this.thread.Name = $"{nameof(ThreadBattle)}:{scene}";
            }
            internal void PostUpdate(float intervalMS)
            {
                lock (postUpdates)
                {
                    postUpdates.Enqueue(new PostUpdate() { IntervalMS = intervalMS });
                    Monitor.Pulse(postUpdates);
                }
            }
            private bool TryGetUpdate(out PostUpdate update)
            {
                lock (postUpdates)
                {
                    if (postUpdates.Count == 0)
                    {
                        Monitor.Wait(postUpdates, 1000);
                        update = default;
                        return false;
                    }
                    update = postUpdates.Dequeue();
                }
                return true;
            }
            public void QueueTask(Action task)
            {
                task_queue.Enqueue(task);
            }
            internal void Start()
            {
                mIsRunning = true;
                thread.Start();
            }
            internal void Stop()
            {
                mIsRunning = false;
                PostUpdate(1);
                if (thread != null)
                {
                    thread.Join();
                }
            }
            void main()
            {
                try
                {
                    this.Zone = battle.HostFactory.CreateZone(this, battle.DataRoot, scene);
                    this.codec = new BattleCodec(battle.Templates, true);
                    this.codec.ObjectPool = Zone.ObjectPool;
                    {
                        var init = Zone.ObjectPool.Alloc<ClientEnterScene>().Init(
                                Zone.UUID,
                                Zone.Data.ID,
                                Zone.SpaceDivSizeW,
                                Zone.Gravity,
                                Zone.Terrain3D.StepIntercept,
                                battle.DataRoot.Templates.ResourceVersion,
                                Zone.GetLayerInitData());
                        this.b2c_sendToLayer(init);
                        this.battle.b2c_OnStart(this);
                        init.Release();
                    }
                    while (mIsRunning)
                    {
                        if (TryGetUpdate(out PostUpdate update))
                        {
                            var intervalMS = update.IntervalMS;
                            if (battle.Pause)
                            {
                                this.Zone.Update(0);
                                Thread.Sleep(1);
                                continue;
                            }
                            this.task_queue.ProcessMessages(Zone);
                            try
                            {
                                this.Zone.Update(intervalMS);
                            }
                            catch (Exception err)
                            {
                                battle.b2c_OnError(this, err);
                            }
                        }
                    }
                    this.task_queue.Dispose();
                    this.battle.b2c_OnEnd(this);
                    this.Zone.Dispose();
                    this.codec.Dispose();
                }
                catch (Exception e)
                {
                    battle.b2c_OnError(this, e);
                }
            }
            void InstanceZoneListener.OnCreateZone(InstanceZone zone)
            {
                battle.OnCrateZone?.Invoke(battle, zone);
            }
            void InstanceZoneListener.OnEventHandler(IReadOnlyList<BattleNotify> events)
            {
                b2c_sendToLayer(events);
            }
            internal void EnqueueAction(MemoryStream stream, uint senderID)
            {
                try
                {
                    if (senderID > 0)
                    {
                        if (Sender == null || Sender.ID != senderID)
                        {
                            Sender = Zone.GetUnit(senderID) as InstancePlayer;
                        }
                    }
                    stream.Position = 0;
                    if (codec.DoDecode(stream, out var message))
                    {
                        if (message is BattleAction action)
                        {
                            Zone.EnqueueAction(action, Sender);
                            action.Release();
                        }
                        else
                        {
                            // TODO other msg ?
                        }
                    }
                }
                finally
                {
                    battle.mempool.Release(stream);
                }
            }
            // 从主线程到战斗线程
            internal void c2b_LayerAction(MemoryStream action, uint senderID)
            {
                if (Zone != null)
                {
                    this.task_queue.Enqueue((this, action, senderID), static (st) =>
                    {
                        st.Item1.EnqueueAction(st.action, st.senderID);
                    });
                }
            }
            // 战斗线程发送给主线程
            internal void b2c_sendToLayer(IMessage msg)
            {
                var stream = battle.mempool.Get(this, static (st, pool) => new MemoryStream());
                if (codec.DoEncode(stream, msg))
                {
                    battle.b2c_OnEvent(this, stream);
                }
                else
                {
                    battle.mempool.Release(stream);
                }
            }
            // 战斗线程发送给主线程
            internal void b2c_sendToLayer(IEnumerable<IBattleMessage> events)
            {
                foreach (var e in events)
                {
                    b2c_sendToLayer((IMessage)e);
                }
            }


        }
    }

    public class ThreadBattleSinglePlay : ThreadBattle
    {
        private readonly int? force;
        private readonly int? actorTemplateID;
        public ThreadBattleSinglePlay(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData sd, int? force = null, int? actorTemplateID = null)
            : base(datas, hostFactory, slaveFactory, sd)
        {
            this.force = force;
            this.actorTemplateID = actorTemplateID;
        }
        protected override void b2c_OnStart(BattleThread zone)
        {
            base.b2c_OnStart(zone);
            var actor = zone.MainZone.InitPlayerStartRegions(force, actorTemplateID);
            if (actor != null)
            {
                b2c_DoPlayerEnter(zone, actor);
            }
        }
    }

}
