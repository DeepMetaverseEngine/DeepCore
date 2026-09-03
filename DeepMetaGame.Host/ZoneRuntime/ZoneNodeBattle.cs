using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DeepCore.Game3D.Host.ZoneRuntime
{
    public class ZoneNodeBattle : InstanceBattle, IZoneNodeServer, IZoneNodeSession
    {
        protected readonly static Logger log = new LazyLogger(typeof(ThreadBattle));
        public override bool IsNet => true;
        public override long RecvPackages => 0;
        public override long SendPackages => 0;
        protected ZoneNode Node { get => node; }
        public ZoneHostFactory HostFactory { get; }

        private ZoneNode node;
        private BattleCodec codec;
        private SceneData data;
        private readonly Ping ping = new Ping();
        public ZoneNodeBattle(EditorTemplates datas, ZoneHostFactory hostFactory, ZoneSlaveFactory slaveFactory, SceneData sd)
            : base(datas, slaveFactory)
        {
            this.ping.Retain();
            this.HostFactory = hostFactory;
            this.Layer.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
            this.data = sd;
            this.codec = new BattleCodec(datas.Templates);
            this.node = hostFactory.CreateServerZoneNode(this, datas);
            this.node.OnCrateZone += Node_OnCrateZone;
            this.node.OnZoneError += (z, err) =>
            {
                QueueTask((b) => { cb_OnError(err); });
            };
            this.node.OnZoneStart += (z) =>
            {
                OnZoneStart?.Invoke(this, z);
                QueueTask((b) => { cb_OnStart(z); });
            };
            this.node.OnZoneStop += (z) =>
            {
                QueueTask((b) => { cb_OnEnd(); });
            };
        }
        public override string ToString()
        {
            return $"ZoneNodeBattle:{data}";
        }
        public override void Start()
        {
            this.node.StartAsync(data).Wait();
            this.node.QueueZoneTask(this, (z, st) =>
            {
                this.cb_StartEnter(z);
            });
        }
        protected override void Disposing()
        {
            this.node.StopAsync().Wait();
            base.Update();
            base.Disposing();
            this.OnStart = null;
            this.OnEnd = null;
            this.OnError = null;
            this.OnZoneStart = null;
            this.OnCrateZone = null;
        }
        public override void LowMemory()
        {
            if (IsDisposing) return;
            base.LowMemory();
            node.QueueZoneTask(this, static (z, st) =>
            {
                z.ObjectPool.LowMemory();
            });
        }
        public override bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            sdata = data;
            return true;
        }
        public override void SendAction(BattleAction action)
        {
            // 多线程交给GC吧
            action.Retain();
            push_Action(action);
        }
        public virtual void DoPlayerEnter(in TAddUnit add)
        {
            this.node.PlayerEnter(this, add, (c, err) => { }, true);
        }
        public override void BeginUpdate(float intervalMS)
        {
            base.BeginUpdate(intervalMS);
            this.EnqueueUpdate(intervalMS);
            this.UpdatePing();
        }
        public override event BattleStart OnStart;
        public override event BattleEnd OnEnd;
        public override event BattleError OnError;
        public override event Action<InstanceBattle, InstanceZone> OnZoneStart;
        public override event Action<InstanceBattle, InstanceZone> OnCrateZone;
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        private void Node_OnCrateZone(BaseZoneNode arg1, InstanceZone arg2)
        {
            OnCrateZone?.Invoke(this, arg2);
        }

        protected virtual void cb_OnStart(InstanceZone zone)
        {
            OnStart?.Invoke(this);
        }
        protected virtual void cb_OnEnd()
        {
            OnEnd?.Invoke(this);
        }
        protected virtual void cb_OnError(Exception err)
        {
            log.Error(err);
            OnError?.Invoke(this, err);
        }
        protected virtual void cb_StartEnter(InstanceZone zone)
        {
            zone.Data.ForEachStartRegions(this, (battle, region, start) =>
            {
                var add = zone.TryAddPlayer(region, start);
                if (add != null)
                {
                    this.DoPlayerEnter(add.Value);
                    return true;
                }
                return false;
            });
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region Ping
        public const float DefaultPingIntervalMS = 5000;
        private TimeInterval ping_task = new TimeInterval(DefaultPingIntervalMS);
        public float PingIntervalMS
        {
            get { return ping_task.IntervalTimeMS; }
            set
            {
                if (ping_task.IntervalTimeMS != value)
                {
                    ping_task = new TimeInterval(Math.Max(DefaultPingIntervalMS, value));
                }
            }
        }
        private void UpdatePing()
        {
            if (ping_task.Update(Layer.CurrentIntervalMS))
            {
                ping.UpdateTime();
                push_Action(ping);
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region Timer Thread
        void IZoneNodeServer.StartTimer(BaseZoneNode node)
        {
            if (mainThread == null)
            {
                mainThread = new Thread(thread_main);
                mainThread.Name = node.Name;
                mainThread.Start();
            }
        }
        private double currentTimeMS = 0;
        private readonly Queue<PostUpdate> postUpdates = new();
        private Thread mainThread;
        private void thread_main()
        {
            try
            {
                while (node.Update(currentTimeMS))
                {
                    if (DequeueUpdate(out PostUpdate update))
                    {
                        this.currentTimeMS += update.IntervalMS;
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }
        private bool DequeueUpdate(out PostUpdate update)
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
        private void EnqueueUpdate(float intervalMS)
        {
            lock (postUpdates)
            {
                postUpdates.Enqueue(new PostUpdate() { IntervalMS = intervalMS });
                Monitor.Pulse(postUpdates);
            }
        }
        struct PostUpdate
        {
            public float IntervalMS;
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region IZoneNodeServer
        void IZoneNodeServer.PostToGameServer(object msg) { }
        void IZoneNodeServer.PostToGameServer(object msg, Action<object, Exception> callback) { }
        event GameServerMessageHandler IZoneNodeServer.HandleGameServerInvoke { add { } remove { } }
        event GameServerCallHandler IZoneNodeServer.HandleGameServerCall { add { } remove { } }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        #region IZoneNodeSession

        protected ClientMessageHandler mHandleClientMessage;
        protected PackNotify mSendingQueue = new PackNotify();

        protected virtual void push_Action(BattleAction action)
        {
            //log.Info($"push_Action {action}");
            mHandleClientMessage?.Invoke(action);
        }
        protected virtual void pop_Event(InstanceZone zone, IMessage e)
        {
            using (var client_events = this.Layer.ObjectPool.AllocList<BattleNotify>())
            {
                try
                {
                    if (codec.DoEncode(e, out var bin))
                    {
                        if (codec.DoDecode(bin, out var msg))
                        {
                            if (msg is BattleNotify evt)
                            {
                                this.Layer.QueueMessage(evt);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    cb_OnError(err);
                }
            }
        }

        public virtual string PlayerUUID { get => "ACTOR"; }
        public virtual string DisplayName { get => "ACTOR"; }

        ZoneNode.PlayerClient IZoneNodeSession.BindingPlayer { get; set; }

        event ClientMessageHandler IZoneNodeSession.HandleClientMessage
        {
            add { mHandleClientMessage += value; }
            remove { mHandleClientMessage -= value; }
        }
        event GameServerMessageHandler IZoneNodeSession.HandleGameServerMessage { add { } remove { } }
        event GameServerCallHandler IZoneNodeSession.HandleGameServerCall { add { } remove { } }

        void IZoneNodeSession.OnPlayerConnected(ZoneNode.PlayerClient binding) { }
        void IZoneNodeSession.OnPlayerDisconnect(ZoneNode.PlayerClient binding) { }
        void IZoneNodeSession.OnPlayerDisposed() { }
        void IZoneNodeSession.ClientSend(PlayerMessageEntry msg, bool immediately)
        {
            var c = ((IZoneNodeSession)this).BindingPlayer;
            if (immediately)
            {
                pop_Event(c.Zone, msg.message);
            }
            else
            {
                var queue = mSendingQueue;
                if (msg.buffer != null)
                    queue.events.Add(msg.buffer);
                else if (msg.message != null)
                    queue.events.Add(msg.message);
            }
        }
        void IZoneNodeSession.ClientFlush(BattleCodec codec)
        {
            var c = ((IZoneNodeSession)this).BindingPlayer;
            if (mSendingQueue.events.Count > 0)
            {
                try
                {
                    var node = c.Node;
                    if (node != null)
                    {
                        mSendingQueue.sequenceNo = node.ZoneTick;
                        pop_Event(c.Zone, mSendingQueue);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                finally
                {
                    mSendingQueue.events.Clear();
                }
            }
        }
        void IZoneNodeSession.PostToGameServer(object msg) { }
        void IZoneNodeSession.PostToGameServer(object msg, Action<object, Exception> callback) { }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------------------------

    }

    //     public class ThreadNodeBattleSinglePlay : ThreadNodeBattle
    //     {
    //         private readonly int force;
    //         private readonly int actorTemplateID;
    //         public ThreadNodeBattleSinglePlay(EditorTemplates datas, SceneData sd, int force = 0, int actorTemplateID = 0) : base(datas, sd)
    //         {
    //             this.force = force;
    //             this.actorTemplateID = actorTemplateID;
    //         }
    //         protected override void thread_OnStart(InstanceZone zone)
    //         {
    //             var actor = zone.InitPlayerStartRegions(force, actorTemplateID);
    //             if (actor != null)
    //             {
    //                 DoPlayerEnter(actor);
    //             }
    //         }
    //     }

}
