using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepMetaGame.Data.Template;
using DeepCore.IO;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using DeepMetaGame.Data.Message;
using DeepCore.Reflection;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public partial class ZoneNode
    {
        [Desc("允许预编码客户端协议")]
        public bool EnablePreEncodeMessageEntries { get; set; } = true;

        [Desc("允许客户端非实时同步AOI")]
        public bool EnableClientIntervalSyncAOI { get; set; } = true;

        private List<DeepCore.IO.MemoryStream> messageEntries = new List<DeepCore.IO.MemoryStream>();
        /// <summary>
        /// 预编码战斗消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual PlayerMessageEntry AllocPlayerEntry(IMessage msg)
        {
            if (EnablePreEncodeMessageEntries)
            {
                var buffer = Codec.AllocStream();
                try
                {
                    using (var output = Codec.AllocOutputAutoRelease(buffer))
                    {
                        output.Statistics = true;
                        if (Codec.doEncode(output, msg))
                        {
                            messageEntries.Add(buffer);
                            return new PlayerMessageEntry() { message = msg, buffer = buffer };
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                buffer.Dispose();
            }
            return new PlayerMessageEntry()
            {
                message = msg
            };
        }

        protected virtual void ReleasePlayerEntries()
        {
            if (EnablePreEncodeMessageEntries)
            {
                foreach (var e in messageEntries)
                {
                    e.Dispose();
                }
                messageEntries.Clear();
            }
        }

        //------------------------------------------------------------------------------------------------------------
        /// <summary> 
        /// 存储玩家UUID和场景内单位的对应关系，如果玩家掉线重连，优先从此表内获取单位信息。
        /// </summary>
        private PlayerClientMap mPlayerObjectMap = new PlayerClientMap();
        private bool mEnableAOI = true;

        protected virtual PlayerClient CreatePlayerClient(IZoneNodeSession client, InstancePlayer actor)
        {
            return new PlayerClient(client, actor, this, GameConfig.CLIENT_SYNC_UNIT_MIN_RANGE, GameConfig.CLIENT_SYNC_UNIT_MAX_RANGE);
        }

        protected virtual void ReconnectPlayerClient(IZoneNodeSession client, InstancePlayer actor)
        {
        }

        public class PlayerClient
        {
            private readonly IZoneNodeSession mSession;
            private readonly ZoneNode mNode;
            private readonly EditorScene mZone;
            private readonly InstancePlayer mActor;
            private readonly float mSyncObjectInRange;
            private readonly float mSyncObjectOutRange;
            private bool mDisposed = false;
            // 视野范围内的单位 //
            private readonly HashMap<uint, InstanceZoneObject> mInViewList = new HashMap<uint, InstanceZoneObject>();
            private readonly SyncPosEvent mSyncPosLocal = new SyncPosEvent();
            private readonly TimeInterval<int> check_out_timer;
            private readonly TimeInterval<int> check_in_timer;
            private double mLastRecvTimeMS = CUtils.TickTimeMS;

            public float SyncObjectRange { get { return mSyncObjectInRange; } }
            public float SyncObjectOutRange { get { return mSyncObjectOutRange; } }
            public ZoneNode Node { get { return mNode; } }
            public EditorScene Zone { get { return mZone; } }
            public InstancePlayer Actor { get { return mActor; } }
            public IZoneNodeSession Session { get { return mSession; } }
            public string PlayerUUID { get { return mSession.PlayerUUID; } }
            public IEnumerable<InstanceZoneObject> InViewList { get => mInViewList.Values; }
            public ISerializable LastZoneSaveData
            {
                get;
                internal set;
            }
            public PlayerClient(IZoneNodeSession client, InstancePlayer actor, ZoneNode node, float look_in_range, float look_out_range)
            {
                this.mActor = actor;
                this.mSession = client;
                this.mNode = node;
                this.mZone = node.Zone;
                this.mSyncObjectInRange = Math.Min(look_in_range, look_out_range);
                this.mSyncObjectOutRange = Math.Max(look_in_range, look_out_range);
                if ((mSyncObjectOutRange / mZone.SpaceDivSizeW) <= (mSyncObjectInRange / mZone.SpaceDivSizeW))
                {
                    this.mSyncObjectOutRange = mSyncObjectInRange + mZone.SpaceDivSizeW;
                }
                if (node.EnableClientIntervalSyncAOI)
                {
                    this.check_out_timer = new TimeInterval<int>(node.GameConfig.CLIENT_UPDATE_LOOK_OUT_INTERVAL_MS);
                    this.check_in_timer = new TimeInterval<int>(node.GameConfig.CLIENT_UPDATE_LOOK_IN_INTERVAL_MS);
                }
                this.mInViewList.Add(mActor.ID, mActor);
                this.mSession.BindingPlayer = this;
                this.mSession.HandleClientMessage += OnClientHandle;
                this.mSession.HandleGameServerMessage += OnServerRpcInvoke;
                this.mSession.HandleGameServerCall += OnServerRpcCall;
            }

            internal void Dispose()
            {
                if (mDisposed) { return; }
                try
                {
                    this.mSession.HandleClientMessage -= OnClientHandle;
                    this.mSession.HandleGameServerMessage -= OnServerRpcInvoke;
                    this.mSession.HandleGameServerCall -= OnServerRpcCall;
                    this.DisposingEvents();
                    this.Disposing();
                    this.mSession.OnPlayerDisconnect(this);
                    this.mSession.BindingPlayer = null;
                    this.mSession.OnPlayerDisposed();
                }
                catch (Exception err)
                {
                    mNode.log.Error(err);
                }
                finally
                {
                    this.mDisposed = true;
                }
            }

            protected virtual void Disposing() { }
            protected virtual void OnStart() { }
            protected virtual void OnBeginUpdate() { }
            protected virtual void OnEndUpdate() { }


            internal void Start()
            {
                OnStart();
                BeginUpdate();
            }

            /// <summary>
            /// 定时更新Client
            /// </summary>
            internal void BeginUpdate()
            {
                OnBeginUpdate();
                if (mNode.mEnableAOI)
                {
                    UpdateLookInRange();
                }
            }
            internal void EndUpdate()
            {
                if (mNode.mEnableAOI)
                {
                    UpdateLookOutRange();
                    // 一直同步周围单位
                    SendSyncPosEvent();
                }
                OnEndUpdate();
            }


            /// <summary>
            /// 排队发送消息
            /// </summary>
            /// <param name="msg"></param>
            /// <param name="immediately"></param>
            public void Send(PlayerMessageEntry msg, bool immediately = false)
            {
                if (mNode.mEnableAOI)
                {
                    if (immediately)
                    {
                        this.mSession.ClientSend(msg);
                    }
                    else if (msg.message is AddSpellEvent add_spell)
                    {
                        if (OnLookSpell(add_spell))
                        {
                            mSession.ClientSend(msg);
                        }
                    }
                    else if (msg.message is RemoveObjectEvent remove_obj)
                    {
                        if (RemoveInView(remove_obj))
                        {
                            mSession.ClientSend(msg);
                        }
                    }
                    else if (mNode.FilterSendingClientMessage(this, msg.message))
                    {
                        this.mSession.ClientSend(msg);
                    }
                }
                else
                {
                    this.mSession.ClientSend(msg);
                }
            }
            public void Send(IMessage msg, bool immediately = false)
            {
                Send(new PlayerMessageEntry() { message = msg }, immediately);
            }

            protected bool RemoveInView(RemoveObjectEvent em)
            {
                var obj = em.sender as InstanceZoneObject;
                if (mInViewList.Remove(obj.ID))
                {
                    OnLeaveView(obj);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// 从客户端接收消息
            /// </summary>
            /// <param name="message"></param>
            private void OnClientHandle(object message)
            {
                if (message is Ping ping)
                {
                    this.mSession.ClientSend(new PlayerMessageEntry() { message = new NetPong().Init(ping) }, true);
                }
                mLastRecvTimeMS = CUtils.TickTimeMS;
                mNode.QueueTask(new ValueTuple<PlayerClient, object>(this, message), static (z, tuple) =>
                {
                    var player = tuple.Item1;
                    var message = tuple.Item2;
                    if (message is PackAction pack)
                    {
                        foreach (var act in pack.actions)
                        {
                            player.DoClientHandleMessage(act);
                        }
                    }
                    else
                    {
                        player.DoClientHandleMessage(message);
                    }
                });
            }

            private void DoClientHandleMessage(object message)
            {
                if (!mNode.OnPlayerClientMessageReceived(this, message))
                {
                    if (message is Ping ping)
                    {
                        this.mSession.ClientSend(new PlayerMessageEntry() { message = new Pong().Init(ping) });
                    }
                    else if (mActor.Enable)
                    {
                        if (message is ObjectAction oa)
                        {
                            oa.object_id = mActor.ID;
                            oa.sender = mActor;
                            mZone.EnqueueAction(oa, mActor);
                        }
                        else if (message is DeepMetaGame.Data.Message.BattleAction act)
                        {
                            act.sender = mActor;
                            mZone.EnqueueAction(act, mActor);
                        }
                    }
                }
            }

            private void OnServerRpcInvoke(object e)
            {
                mNode.QueueTask(() => { mNode.OnPlayerRpcInvoke(this, e); });
            }
            private void OnServerRpcCall(object e, Action<object, Exception> callback)
            {
                mNode.QueueTask(() => { mNode.OnPlayerRpcCall(this, e, callback); });
            }

            //------------------------------------------------------------------------------------------------------------

            //------------------------------------------------------------------------------------------------------------
            #region _AOI_

            /// <summary>
            /// 获取当前场景内所有单位，用于同步现有场景中单位 
            /// </summary>
            /// <returns></returns>
            public SyncObjectsEvent AllocSyncObjectsEvent()
            {
                if (mNode.mEnableAOI)
                {
                    return mZone.AllocSyncObjectsEvent(mInViewList.Values);
                }
                else
                {
                    return mZone.AllocSyncUnitsEvent(mActor);
                }
            }
            public void SendSyncPosEvent()
            {
                if (mZone.TryGetSyncPosEvent(mSyncPosLocal, mInViewList.Values))
                {
                    if (!mSyncPosLocal.IsEmpty)
                    {
                        mSession.ClientSend(mNode.AllocPlayerEntry(mSyncPosLocal));
                    }
                }
            }
            public bool IsInView(InstanceZoneObject obj)
            {
                if (mNode.mEnableAOI)
                {
                    if (obj != null)
                    {
                        return mInViewList.ContainsKey(obj.ID);
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            public bool IsLookInRange(Geometry.Vector3 pos)
            {
                return IsLookInRange(pos.X, pos.Y);
            }
            public virtual bool IsLookInRange(float x, float y)
            {
                if (mNode.mEnableAOI)
                    return CMath.IncludeRoundPoint(mActor.X, mActor.Y, mSyncObjectInRange, x, y);
                else
                    return true;
            }
            public bool ContainsInViewList(uint objID)
            {
                return mInViewList.ContainsKey(objID);
            }

            //--------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// 判断单位是否进入视野
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>True，进入视野</returns>
            public virtual bool IsLookInRange(InstanceZoneObject obj)
            {
                if (obj.IgnoreLookRange) return true;
                if (this.Actor.IgnoreLookRange) return true;
                return CMath.IncludeRoundPoint(mActor.X, mActor.Y, mSyncObjectInRange, obj.X, obj.Y);
            }
            /// <summary>
            /// 判断单位是否超出视野
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>True，超出视野</returns>
            public virtual bool IsLookOutRange(InstanceZoneObject obj)
            {
                if (obj.IgnoreLookRange) return false;
                if (this.Actor.IgnoreLookRange) return false;
                return !CMath.IncludeRoundPoint(mActor.X, mActor.Y, mSyncObjectOutRange, obj.X, obj.Y);
            }

            public bool TryLookInRange(InstanceZoneEntity o)
            {
                if ((o != mActor))
                {
                    if (o.Enable && o.ClientVisible && mZone.Formula.IsVisibleAOI(mActor, o) && IsLookInRange(o))
                    {
                        if (!mInViewList.ContainsKey(o.ID))
                        {
                            mInViewList.Put(o.ID, o);
                            OnEnterView(o);
                            return false;
                        }
                    }
                }
                return false;
            }
            protected void UpdateLookInRange()
            {
                if (check_in_timer == null || check_in_timer.Update(Zone.UpdateIntervalMS))
                {
                    mZone.ForEachNearObjectsPredicate(mActor.X, mActor.Y, mSyncObjectInRange, this,
                        static (PlayerClient v, InstanceZoneEntity o) => v.TryLookInRange(o));
                }
            }
            protected void UpdateLookOutRange()
            {
                if (check_out_timer == null || check_out_timer.Update(Zone.UpdateIntervalMS))
                {
                    using (var removing = Zone.ObjectPool.AllocList<InstanceZoneObject>())
                    {
                        foreach (InstanceZoneObject o in mInViewList.Values)
                        {
                            if (o != mActor)
                            {
                                if (!o.Enable || !mZone.Formula.IsVisibleAOI(mActor, o) || IsLookOutRange(o))
                                {
                                    removing.Add(o);
                                }
                            }
                        }
                        if (removing.Count > 0)
                        {
                            foreach (var o in removing)
                            {
                                mInViewList.Remove(o.ID);
                                OnLeaveView(o);
                            }
                        }
                    }
                }
            }


            //--------------------------------------------------------------------------------------------------------------------
            public void ForceAddObjectInView(InstanceZoneObject obj)
            {
                if (mInViewList.TryAdd(obj.ID, obj))
                {
                    OnEnterView(obj);
                }
            }

            protected virtual bool OnLookSpell(AddSpellEvent em)
            {
                // 过滤不在自己感兴趣范围内的消息
                if (em.sender is InstanceSpell)
                {
                    var sp = em.sender as InstanceSpell;
                    //作用于自己或者自己发射的//
                    if (sp.LauncherOwner == mActor || sp.Target == mActor)
                    {
                        mInViewList.Put(sp.ID, sp);
                        return true;
                    }
                    if (mZone.Formula.IsVisibleAOI(mActor, sp))
                    {
                        if (IsLookInRange(em.spell_pos.X, em.spell_pos.Y))
                        {
                            mInViewList.Put(sp.ID, sp);
                            return true;
                        }
                    }
                }
                return false;
            }
            private void OnEnterView(InstanceZoneObject obj)
            {
                event_OnObjectEnterView?.Invoke(this, obj);
                if (obj is InstanceUnit unit)
                {
                    var sync = unit.GenSyncUnitInfo(true);
                    var add = Zone.ObjectPool.Alloc<AddUnitEvent>().Init(sync, unit);
                    mSession.ClientSend(new PlayerMessageEntry() { message = add });
                }
                else if (obj is InstanceItem item)
                {
                    var sync = item.GenSyncItemInfo(true);
                    var add = Zone.ObjectPool.Alloc<AddItemEvent>().Init(sync,item);
                    add.sender = item;
                    mSession.ClientSend(new PlayerMessageEntry() { message = add });
                }
            }
            private void OnLeaveView(InstanceZoneObject obj)
            {
                event_OnObjectLeaveView?.Invoke(this, obj);
                RemoveObjectEvent remove = Zone.ObjectPool.Alloc<RemoveObjectEvent>().Init(obj.ID);
                remove.sender = obj;
                mSession.ClientSend(new PlayerMessageEntry() { message = remove });
            }

            public void ForEachInViewList(Action<InstanceZoneObject> action)
            {
                var list = new List<InstanceZoneObject>(mInViewList.Values);
                {
                    foreach (var obj in list) { action(obj); }
                }
            }

            #endregion
            //------------------------------------------------------------------------------------------------------------
            #region Events
            protected void DisposingEvents()
            {
                event_OnObjectEnterView = null;
                event_OnObjectLeaveView = null;
            }
            private OnObjectEnterViewHandler event_OnObjectEnterView;
            private OnObjectLeaveViewHandler event_OnObjectLeaveView;
            public delegate void OnObjectEnterViewHandler(PlayerClient sender, InstanceZoneObject obj);
            public delegate void OnObjectLeaveViewHandler(PlayerClient sender, InstanceZoneObject obj);
            public event OnObjectEnterViewHandler OnObjectEnterView { add { event_OnObjectEnterView += value; } remove { event_OnObjectEnterView -= value; } }
            public event OnObjectLeaveViewHandler OnObjectLeaveView { add { event_OnObjectLeaveView += value; } remove { event_OnObjectLeaveView -= value; } }
            #endregion
        }

        private class PlayerClientMap : Disposable
        {
            private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
            private HashMap<string, InstancePlayer> mPlayerObjectMap = new HashMap<string, InstancePlayer>();
            private HashMap<string, PlayerClient> mPlayerClientMap = new HashMap<string, PlayerClient>();

            protected override void Disposing()
            {
                using (locker.EnterWrite())
                {
                    foreach (var p in mPlayerClientMap.Values.ToArray())
                    {
                        p.Dispose();
                    }
                    mPlayerObjectMap.Clear();
                    mPlayerClientMap.Clear();
                }
                locker.Dispose();
            }

            public int Count
            {
                get
                {
                    using (locker.EnterRead())
                    {
                        return mPlayerClientMap.Count;
                    }
                }
            }
            public InstancePlayer[] Players
            {
                get
                {
                    using (locker.EnterRead())
                    {
                        return mPlayerObjectMap.Values.ToArray();
                    }
                }
            }
            public void PutPlayer(PlayerClient client)
            {
                using (locker.EnterWrite())
                {
                    mPlayerObjectMap.Put(client.PlayerUUID, client.Actor);
                    mPlayerClientMap.Put(client.PlayerUUID, client);
                }
            }
            public InstancePlayer GetPlayer(string uuid)
            {
                using (locker.EnterRead())
                {
                    return mPlayerObjectMap.Get(uuid);
                }
            }
            public PlayerClient GetClient(string uuid)
            {
                using (locker.EnterRead())
                {
                    return mPlayerClientMap.Get(uuid);
                }
            }
            public bool TryGet(string uuid, out InstancePlayer player, out PlayerClient client)
            {
                using (locker.EnterRead())
                {
                    player = mPlayerObjectMap.Get(uuid);
                    client = mPlayerClientMap.Get(uuid);
                }
                return client != null;
            }
            public bool RemoveByKey(string uuid, out InstancePlayer player, out PlayerClient client)
            {
                using (locker.EnterWrite())
                {
                    player = mPlayerObjectMap.RemoveByKey(uuid);
                    client = mPlayerClientMap.RemoveByKey(uuid);
                }
                return client != null;
            }
            public bool ContainsKey(string uuid)
            {
                using (locker.EnterRead())
                {
                    return mPlayerObjectMap.ContainsKey(uuid);
                }
            }
            public void ForEachPlayers(Action<PlayerClient> action)
            {
                var list = new List<PlayerClient>();
                {
                    using (locker.EnterRead())
                    {
                        list.AddRange(mPlayerClientMap.Values);
                    }
                    foreach (var c in list)
                    {
                        action(c);
                    }
                }
            }
            public void GetPlayers(List<PlayerClient> list)
            {
                using (locker.EnterRead())
                {
                    list.AddRange(mPlayerClientMap.Values);
                }
            }
        }

        //------------------------------------------------------------------------------------------------------------


    }
}
