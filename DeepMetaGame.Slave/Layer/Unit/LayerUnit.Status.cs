using DeepCore.Game3D.Slave.Data;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using static DeepCore.Game3D.Slave.Layer.LayerPlayer;
using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Slave.Layer
{
    public partial class LayerUnit
    {
        //--------------------------------------------------------------------------------
        #region State
        /// <summary>
        /// 当前动做主状态
        /// </summary>
        public UnitActionStatus CurrentActionStatus => CurrentState;
        /// <summary>
        /// 当前动做子状态
        /// </summary>
        public string CurrentActionSubstate => CurrentSubState;

        public UnitActionStatus CurrentState
        {
            get { return mCurrentMainState.Value; }
        }
        public string CurrentSubState
        {
            get { return mCurrentSubState.Value; }
        }
        public UnitActionStatus RemoteStatus
        {
            get { return mRemoteState.UnitMainState; }
        }
        protected readonly UnitSyncPos mRemoteState = new UnitSyncPos();
        private State<UnitActionStatus> mCurrentMainState = new State<UnitActionStatus>(UnitActionStatus.NA, static (a, b) => a == b);
        private State<string> mCurrentSubState = new State<string>(null, static (a, b) => a == b);
        private bool mMarkMainStateDirty = true;
        private bool mMarkSubStateDirty = true;
        private UnitActionStatus mLastSendMainState = UnitActionStatus.NA;
        private string mLastSendSubState = null;
        private IRecyclable mLastSendStateMsg = null;
        protected void UpdateState()
        {
            var changed = false;
            {
                var cur = this.mCurrentMainState.Value;
                if (mMarkMainStateDirty || mLastSendMainState != cur)
                {
                    mMarkMainStateDirty = false;
                    mLastSendMainState = cur;
                    changed = true;
                }
            }
            {
                var cur = this.mCurrentSubState.Value;
                if (mMarkSubStateDirty || mLastSendSubState != cur)
                {
                    mMarkSubStateDirty = false;
                    mLastSendSubState = cur;
                    changed = true;
                }
            }
            if (changed)
            {
                mOnActionChanged?.Invoke(this, mLastSendMainState, mLastSendSubState, mLastSendStateMsg);
                mLastSendStateMsg?.Release();
                mLastSendStateMsg = null;
            }
        }

        protected void SyncCurrentState(UnitSyncPos pos)
        {
            if ((pos.HasModifer(UnitSyncModifer.MainState) && mCurrentMainState.Update(pos.UnitMainState)) ||
                (pos.HasModifer(UnitSyncModifer.SubState) && mCurrentSubState.Update(pos.UnitSubState)))
            {
                mMarkSubStateDirty = true;
                mLastSendStateMsg?.Release();
                mLastSendStateMsg = null;
            }
        }
        protected void SyncCurrentSubState(UnitSyncPos pos)
        {
            if (pos.HasModifer(UnitSyncModifer.SubState) && mCurrentSubState.Update(pos.UnitSubState))
            {
                mMarkSubStateDirty = true;
                mLastSendStateMsg?.Release();
                mLastSendStateMsg = null;
            }
        }

        protected void ForceSyncCurrentState(UnitActionStatus state, string sub, IRecyclable msg)
        {
            if (mCurrentMainState.Update(state) || mCurrentSubState.Update(sub))
            {
                mMarkMainStateDirty = true;
                mLastSendStateMsg?.Release();
                mLastSendStateMsg = msg;
                mLastSendStateMsg?.Retain();
            }
        }
        protected void PreSetCurrentMainState(UnitActionStatus state, string sub, IRecyclable msg)
        {
            if (mCurrentMainState.Update(state) || mCurrentSubState.Update(sub))
            {
                mMarkMainStateDirty = true;
                mLastSendStateMsg?.Release();
                mLastSendStateMsg = msg;
                mLastSendStateMsg?.Retain();
            }
            this.UpdateState();
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region DamageDeadFly


        private TimeExpire<UnitDamageEvent> mDamageTime;
        private TimeExpire<UnitDeadEvent> mDeadTime;


        protected virtual void DoDamage(UnitDamageEvent e)
        {
            if (e.HasMove)
            {
                var hitmove = e.HitMove;
                var move = this.PreSkillMove(
                    hitmove.Direction,
                    hitmove.RotateSpeedSEC,
                    hitmove.ExpectlTimeMS,
                    hitmove.MoveSpeedSEC,
                    hitmove.MoveSpeedAdd,
                    hitmove.MoveSpeedAcc,
                    hitmove.MoveZSpeed,
                    hitmove.Gravity,
                    hitmove.isNoneTouch);
                if (hitmove.hasTarget && Parent.GetObject(hitmove.TargetID) is LayerObject target)
                {
                    move.SetMoveTarget(target,
                        hitmove.TargetBodyBlock,
                        hitmove.TargetBodyKeepRange);
                }
            }
            mDamageTime?.Dispose();
            mDamageTime = ObjectPool.Alloc<TimeExpire<UnitDamageEvent>>().Init(e.DamageTimeMS, e);
            //             if (e.HasFly)
            //             {
            //                 //StartFly(e.ZSpeedSEC, e.ZGravity, e.ZLimit);
            //             }
            if (AResource && AResource.DamageEffect != null)
            {
                Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(this.ObjectID, AResource.DamageEffect));
            }
            PreSetCurrentMainState(UnitActionStatus.Damage, null, e);
            if (mOnDamage != null)
            {
                mOnDamage.Invoke(this, e.ToArgs());
            }

        }


        protected virtual void DoDead(UnitDeadEvent me)
        {
            if (me.DeadTimeMS > 0)
            {
                mDeadTime?.Dispose();
                mDeadTime = ObjectPool.Alloc<TimeExpire<UnitDeadEvent>>().Init(me.DeadTimeMS, me);
            }
            if (AResource && AResource.DeadEffect != null)
            {
                Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(this.ObjectID, AResource.DeadEffect));
            }
            if (me.Crushed)
            {
                if (AResource && AResource.CrushEffect != null)
                {
                    Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(this.ObjectID, AResource.CrushEffect));
                }
            }
            mOnDead?.Invoke(this, me.Crushed, me.attacker_id, me.DeadTimeMS);
        }



        protected void UpdateDamage(float intervalMS)
        {
            if (mDeadTime != null && mDeadTime.Update(intervalMS))
            {
                mDeadTime?.Dispose();
                mDeadTime = null;
            }
            if (mDamageTime != null && mDamageTime.Update(intervalMS))
            {
                mDamageTime?.Dispose();
                mDamageTime = null;
            }
        }

        #endregion
        //--------------------------------------------------------------------------------


        //-------------------------------------------------------------------------------------------
        #region Buff

        internal BuffMap mBuffStatus = new BuffMap();

        internal void SyncBuffStatus(IList<ClientStruct.UnitBuffStatus> buffs)
        {
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Count; i++)
                {
                    ClientStruct.UnitBuffStatus st = buffs[i];
                    TryAddBuff(st.BuffTemplateID, st.SenderID, st.IsEquip, st.TotalTime, st.BuffLevel, st.OverlayLevel, st.PassTime);
                }
            }
        }
        protected virtual void DoLaunchBuff(UnitLaunchBuffEvent me)
        {
            TryAddBuff(me.buffTemplateID, me.senderID, me.IsEquip, me.buffTimeMS, me.buffLevel, me.overlayLevel, me.passTimeMS);
        }
        protected virtual void DoStopBuff(UnitStopBuffEvent e)
        {
            TryRemoveBuff(e.buffTemplateID, e.senderID);
        }
        protected virtual void DoSyncBuff(UnitSyncBuffEvent e)
        {
            TryAddBuff(e.sync.BuffTemplateID, e.sync.SenderID, e.sync.IsEquip, e.sync.TotalTime, e.sync.BuffLevel, e.sync.OverlayLevel, e.sync.PassTime);
        }

        private void TryAddBuff(int buffTempalteID, uint senderID, bool isEquip, float totalTime, int bufflevel, int overlayLevel, float passTime = 0)
        {
            BuffTemplate buff = Templates.GetBuff(buffTempalteID);
            if (buff != null)
            {
                BuffState bs = mBuffStatus.Get(buffTempalteID, senderID);
                if (bs != null)
                {
                    bs.Sync(bufflevel, overlayLevel, totalTime, passTime);
                    if (mOnBuffChanged != null)
                    {
                        mOnBuffChanged.Invoke(this, bs);
                    }
                }
                else
                {
                    bs = SlaveFactory.AllocBuffState(buff, this, senderID, isEquip);
                    bs.Sync(bufflevel, overlayLevel, totalTime, passTime);
                    mBuffStatus.Put(bs);
                    if (mOnBuffAdded != null)
                    {
                        mOnBuffAdded.Invoke(this, bs);
                    }
                }
            }
        }
        private void TryRemoveBuff(int buffTempalteID, uint senderID)
        {
            if (mBuffStatus.RemoveByKey(buffTempalteID, senderID, out var bs))
            {
                try
                {
                    if (mOnBuffRemoved != null)
                    {
                        mOnBuffRemoved.Invoke(this, bs);
                    }
                }
                finally
                {
                    bs.Dispose();
                }
            }
        }
        protected void UpdateBuffs(float intervalMS)
        {
            mBuffStatus.ForEach(in intervalMS, static (intervalMS, bs) =>
            {
                bs.OnUpdate(intervalMS);
                return false;
            });
        }

        public class BuffMap
        {
            private HashMap<int, BuffList> Map = new HashMap<int, BuffList>();
            private List<BuffList> For = new List<BuffList>();
            public int TotalCount { get => For.Sum(b => b.Count); }

            public BuffState Get(int id, uint senderID)
            {
                var list = Map.Get(id);
                if (list != null)
                {
                    return list.Get(senderID);
                }
                return null;
            }
            internal void Put(BuffState state)
            {
                var list = Map.Get(state.BuffID);
                if (list == null)
                {
                    list = new BuffList(state.Data);
                    Map.Add(state.BuffID, list);
                    For.Add(list);
                }
                list.Add(state);
            }
            internal bool RemoveByKey(int id, uint senderID, out BuffState st)
            {
                var list = Map.Get(id);
                if (list != null)
                {
                    st = list.Remove(senderID);
                    if (st != null)
                    {
                        st.OnEnd();
                        if (list.Count == 0)
                        {
                            For.Remove(list);
                            Map.Remove(id);
                        }
                        return true;
                    }
                }
                st = null;
                return false;
            }
            internal void Clear()
            {
                ForEach(this, static (a, b) => { b.Dispose(); return false; });
                For.Clear();
                Map.Clear();
            }

            public bool ForEach<ST>(in ST input, ForEachPredicate<ST, BuffState> action)
            {
                for (int i = For.Count - 1; i >= 0; --i)
                {
                    if (For[i].ForEach(input, action))
                    {
                        return true;
                    }
                }
                return false;
            }

        }

        public class BuffList
        {
            public readonly BuffTemplate Data;
            private HashMap<uint, BuffState> Map = new HashMap<uint, BuffState>();
            private List<BuffState> For = new List<BuffState>();
            public int Count { get { return Map.Count; } }
            internal BuffList(BuffTemplate data)
            {
                this.Data = data;
            }
            internal BuffState Remove(uint senderID)
            {
                BuffState ret;
                if (Data.IsDuplicating)
                {
                    ret = Map.RemoveByKey(senderID);
                }
                else
                {
                    ret = Map.RemoveByKey(0);
                }
                if (ret != null)
                {
                    For.Remove(ret);
                }
                return ret;
            }
            internal void Add(BuffState state)
            {
                if (Data.IsDuplicating)
                {
                    Map.Add(state.SenderID, state);
                }
                else
                {
                    Map.Add(0, state);
                }
                For.Add(state);
            }
            internal BuffState Get(uint senderID)
            {
                if (Data.IsDuplicating)
                {
                    if (senderID == 0 && Map.Count > 0)
                    {
                        return Map.First().Value;
                    }
                    else
                    {
                        return Map.Get(senderID);
                    }
                }
                else
                {
                    return Map.Get(0);
                }
            }
            internal bool ForEach<ST>(in ST input, ForEachPredicate<ST, BuffState> action)
            {
                for (int i = For.Count - 1; i >= 0; --i)
                {
                    if (action(input, For[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public class BuffState : LayerStatus
        {
            private BuffTemplate _Data;
            private BuffAvatarChangeAbility _AvatarChange;
            private BuffEffectAbility _EffectAbility;
            private bool _isEquip;
            private LayerUnit _Owner;
            private uint _SenderID;
            private int buff_level;
            private int overlay_level;
            private float total_time;
            private float pass_time = 0;
            private float percent = 1f;
            private readonly TimeInterval interval = new();
            private readonly PopupKeyFrames<BuffTemplate.KeyFrame> keyframes = new();

            protected BuffState() { }
            public static BuffState Alloc(BuffTemplate data, LayerUnit owner, uint senderID, bool isEquip)
            {
                return owner.ObjectPool.AllocOrCreateAutoRelease<BuffState>(static s => new BuffState()).Init(data, owner, senderID, isEquip);
            }
            protected virtual BuffState Init(BuffTemplate data, LayerUnit owner, uint senderID, bool isEquip)
            {
                this._Data = data;
                this._isEquip = isEquip;
                this._Owner = owner;
                this._SenderID = senderID;
                this._AvatarChange = data.Abilities.GetComponentAs<BuffAvatarChangeAbility>();
                this._EffectAbility = data.Abilities.GetComponentAs<BuffEffectAbility>();
                return this;
            }
            protected override void Disposing()
            {
                this._Data = default;
                this._AvatarChange = default;
                this._EffectAbility = default;
                this._isEquip = default;
                this._Owner = default;
                this._SenderID = default;
                this.buff_level = default;
                this.overlay_level = default;
                this.total_time = default;
                this.pass_time = 0;
                this.percent = 1f;
                this.interval.Dispose();
                this.keyframes.Clear();
            }

            public BuffTemplate Data => _Data;
            public BuffAvatarChangeAbility AvatarChange => _AvatarChange;
            public BuffEffectAbility EffectAbility => _EffectAbility;
            public bool isEquip => _isEquip;
            public LayerUnit Owner => _Owner;
            public uint SenderID => _SenderID;
            internal void Sync(int bufflevel, int overlayLevel, float totalTime, float passTime)
            {
                this.buff_level = bufflevel;
                this.overlay_level = overlayLevel;
                this.total_time = totalTime;
                if (passTime < this.pass_time || this.pass_time == 0)
                {
                    this.interval.Init(Data.HitIntervalMS);
                    this.keyframes.Clear();
                    this.keyframes.AddRange(Data.KeyFrames);
                    this.pass_time = 0;
                }
                if (passTime > this.pass_time)
                {
                    this.interval.SetPassTime(passTime);
                    this.keyframes.PopKeyFrames(passTime, null);
                    this.pass_time = passTime;
                }
                this.percent = Math.Min(1, pass_time / (float)total_time);

                //                 if (AvatarChange)
                //                 {
                //                     if (AvatarChange.BodySize > 0)
                //                     {
                //                         Owner.SetBodySize(AvatarChange.BodySize);
                //                     }
                //                 }
            }

            internal void OnEnd()
            {
                //                 if (AvatarChange)
                //                 {
                //                     if (AvatarChange.BodySize > 0)
                //                     {
                //                         Owner.SetBodySize(Owner.Info.BodySize);
                //                     }
                //                 }
                if (Data.EndKeyFrame != null && Data.EndKeyFrame.Effect != null)
                {
                    Owner.Parent.PreQueueEvent(Owner.ObjectPool.Alloc<UnitEffectEvent>().Init(Owner.ObjectID, Data.EndKeyFrame.Effect));
                }
            }

            internal void OnUpdate(float intervalMS)
            {
                using (var kfs = Owner.ObjectPool.AllocList<BuffTemplate.KeyFrame>())
                {
                    if (keyframes.PopKeyFrames(pass_time, kfs) > 0)
                    {
                        for (int i = 0; i < kfs.Count; i++)
                        {
                            BuffTemplate.KeyFrame kf = kfs[i];
                            if (kf.Effect != null)
                            {
                                Owner.Parent.PreQueueEvent(Owner.ObjectPool.Alloc<UnitEffectEvent>().Init(Owner.ObjectID, kf.Effect));
                            }
                        }
                    }
                }
                if (interval.Update(intervalMS))
                {
                    if (Data.HitKeyFrame != null && Data.HitKeyFrame.Effect != null)
                    {
                        Owner.Parent.PreQueueEvent(Owner.ObjectPool.Alloc<UnitEffectEvent>().Init(Owner.ObjectID, Data.HitKeyFrame.Effect));
                    }
                }
                this.pass_time += intervalMS;
                if (percent < 1f)
                {
                    this.percent = Math.Min(1, pass_time / (float)total_time);
                }
            }

            public int BuffID { get { return Data.ID; } }
            public bool IsDone { get { return (isEquip) ? false : (percent >= 1f); } }
            public float CDAmount { get { return percent; } }
            public int OverlayLevel { get { return overlay_level; } }
            public int BuffLevel { get => buff_level; }
            public float ExpireTimeMS { get { return Math.Max(0, total_time - pass_time); } }
        }

        public int BuffTotalCount { get => mBuffStatus.TotalCount; }
        public bool ForEachBuffs<ST>(in ST input, ForEachPredicate<ST, BuffState> action)
        {
            return mBuffStatus.ForEach(in input, action);
        }
        [Obsolete("GC")]
        public List<BuffState> GetBuffStatus()
        {
            var list = new List<BuffState>();
            GetBuffStatus(list);
            return list;
        }
        public void GetBuffStatus(IList<BuffState> ret)
        {
            mBuffStatus.ForEach(in ret, static (RET, bs) => { RET.Add(bs); return false; });
        }
        public BuffState GetBuff(int tempID, uint senderID)
        {
            return mBuffStatus.Get(tempID, senderID);
        }
        public BuffState GetBuff(int tempID)
        {
            return mBuffStatus.Get(tempID, 0);
        }



        #endregion

        //--------------------------------------------------------------------------------

        #region Aura

        internal void SyncAuraStatus(IList<ClientStruct.UnitAuraStatus> auras)
        {
            if (auras != null)
            {
                for (int i = 0; i < auras.Count; i++)
                {
                    var st = auras[i];
                    var temp = Templates.GetAura(st.AuraTemplateID);
                    if (temp != null)
                    {
                        if (mAuraStatus.TryGetValue(temp.ID, out var old))
                        {
                            old.Sync(st.TotalTime, st.PassTime, st.Range);
                        }
                        else
                        {
                            var aura = SlaveFactory.AllocAuraState(this, temp);
                            aura.Sync(st.TotalTime, st.PassTime, st.Range);
                            mAuraStatus.Put(st.AuraTemplateID, aura);
                        }
                    }
                }
            }
        }
        protected virtual void DoLaunchAura(UnitLaunchAuraEvent me)
        {
            var temp = Templates.GetAura(me.auraTemplateID);
            if (temp != null)
            {
                if (mAuraStatus.TryGetValue(temp.ID, out var old))
                {
                    old.Sync(me.auraTimeMS, me.passTimeMS, me.range);
                }
                else
                {
                    var aura = SlaveFactory.AllocAuraState(this, temp);
                    aura.Sync(me.auraTimeMS, me.passTimeMS, me.range);
                    mAuraStatus.Put(temp.ID, aura);
                }
            }
        }
        protected virtual void DoStopAura(UnitStopAuraEvent e)
        {
            var ret = mAuraStatus.RemoveByKey(e.auraTemplateID);
            if (ret != null)
            {
                ret.Dispose();
            }
        }


        //         public List<AuraState> GetAuraStatus()
        //         {
        //             var list = new List<AuraState>();
        //             GetAuraStatus(list);
        //             return list;
        //         }
        public void GetAuraStatus(IList<AuraState> ret)
        {
            ret.AddRange(mAuraStatus.Values);
        }

        public AuraState GetAura(int tempID)
        {
            return mAuraStatus.Get(tempID);
        }
        private void cleanAuras()
        {
            foreach (var e in mAuraStatus.Values)
            {
                e.Dispose();
            }
            mAuraStatus.Clear();
        }

        private HashMap<int, AuraState> mAuraStatus = new HashMap<int, AuraState>();
        public class AuraState : LayerStatus
        {
            private AuraTemplate data;
            private float total_time;
            private float pass_time = 0;
            private float range;

            protected AuraState() { }
            public static AuraState Alloc(LayerUnit unit, AuraTemplate data)
            {
                var ret = unit.ObjectPool.AllocOrCreateAutoRelease<AuraState>(static s => new AuraState());
                ret.Init(data);
                return ret;
            }
            protected virtual void Init(AuraTemplate data)
            {
                this.data = data;
            }
            protected override void Disposing()
            {
                data = default;
                total_time = default;
                pass_time = 0;
                range = 0;
            }


            public AuraTemplate Data { get { return data; } }
            public int AuraID { get { return data.ID; } }
            public float Range { get => range; }
            internal void Sync(float totalTime, float passTime, float range)
            {
                this.total_time = totalTime;
                this.pass_time = passTime;
                this.range = range;
            }
        }


        #endregion

        //--------------------------------------------------------------------------------
        #region PickObject

        private TimeExpire<uint> mPickEvent;
        public TimeExpire<uint> PickEvent { get { return mPickEvent; } }

        protected virtual void DoStartPick(UnitStartPickObjectEvent pick)
        {
            mPickEvent = new TimeExpire<uint>().Init(pick.PickTimeMS, pick.PickObjectID);
            PreSetCurrentMainState(UnitActionStatus.Pick, null, pick);
            if (mOnStartPickObject != null)
            {
                mOnStartPickObject.Invoke(this, mPickEvent, pick.PickTimeMS, pick.PickObjectID, pick.PickStatus);
            }
        }
        protected virtual void DoStopPick(UnitStopPickObjectEvent pick)
        {
            PreSetCurrentMainState(UnitActionStatus.Idle, null, pick);
            if (mOnStopPickObject != null)
            {
                mOnStopPickObject.Invoke(this, mPickEvent, pick.StopReason);
            }
            if (mPickEvent != null)
            {
                mPickEvent.End();
                mPickEvent = null;
            }
        }

        protected void UpdatePickEvent(float intervalMS)
        {
            if (mPickEvent != null)
            {
                mPickEvent.Update(intervalMS);
            }
        }

        #endregion
        //-------------------------------------------------------------------------------- 

        #region MultiTimeLine


        /// <summary>
        /// 此单位是否霸体
        /// </summary>
        virtual public bool IsNoneBlock { get { return IsTimeLineEnable(0); } }
        /// <summary>
        /// 是否为眩晕
        /// </summary>
        virtual public bool IsStun { get { return IsTimeLineEnable(1); } }
        /// <summary>
        /// 是否可被发现
        /// </summary>
        virtual public bool IsVisible { get { return !IsTimeLineEnable(2); } }
        /// <summary>
        /// 是否无敌
        /// </summary>
        virtual public bool IsInvincible { get { return IsTimeLineEnable(3); } }
        /// <summary>
        /// 是否无伤
        /// </summary>
        virtual public bool IsNoDamage { get { return IsTimeLineEnable(4); } }
        /// <summary>
        /// 是否沉默
        /// </summary>
        virtual public bool IsSilent { get { return IsTimeLineEnable(5); } }
        /// <summary>
        /// 是否沉默
        /// </summary>
        virtual public bool IsLock { get { return IsTimeLineEnable(6); } }
        //--------------------------------------------------------------------------------

        private UnitSyncMultiTimeLine mMultiTimeLine;

        protected virtual void DoUnitSyncMultiTimeLine(UnitSyncMultiTimeLine e)
        {
            this.mMultiTimeLine = e;
        }

        /// <summary>
        /// 指定TimeLine是否还有任务
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsTimeLineEnable(int index)
        {
            if (mMultiTimeLine != null && mMultiTimeLine.timelines != null && index < mMultiTimeLine.timelines.Count && index >= 0)
            {
                return mMultiTimeLine.timelines[index];
            }
            return false;
        }

        public void GetMultiTimeLineStatus(List<bool> list)
        {
            if (mMultiTimeLine != null && mMultiTimeLine.timelines != null)
            {
                list.AddRange(mMultiTimeLine.timelines);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------
        #region Cards

        protected HashMap<int, CardSlot> ownerFuncs = new HashMap<int, CardSlot>();
        protected void ClearCards()
        {
            foreach (var e1 in ownerFuncs)
            {
                e1.Value.Dispose();
            }
            ownerFuncs.Clear();
        }
        protected void SyncCards(PlayerSyncCardsEvent e)
        {
            ClearCards();
            foreach (var ee in e.ownerFunctions)
            {
                var card = Templates.GetCard(ee.Key);
                if (card != null)
                {
                    ownerFuncs.Put(ee.Key, CardSlot.Alloc(this, card, ee.Value));
                }
            }
        }
        internal void SyncCardStatus(IList<ClientStruct.UnitCardStatus> cards)
        {
            ClearCards();
            foreach (var ee in cards)
            {
                var card = Templates.GetCard(ee.CardID);
                if (card != null)
                {
                    ownerFuncs.Put(ee.CardID, CardSlot.Alloc(this, card, ee.Level));
                }
            }
        }
        public virtual CardSlot GetCard(int id)
        {
            return ownerFuncs.Get(id);
        }
        public bool TryGetCard(int id, out CardSlot card)
        {
            card = ownerFuncs.Get(id);
            return card != null;
        }
        public void GetCards(IList<CardSlot> cards)
        {
            foreach (var e1 in ownerFuncs)
            {
                cards.Add(e1.Value);
            }
        }
        public class CardSlot : LayerStatus
        {
            private LayerUnit _Owner;
            private CardTemplate _Data;
            private int _Level;

            public CardTemplate Card => _Data;
            public int Level => _Level;
            public LayerUnit Owner => _Owner;

            protected CardSlot() { }
            public static CardSlot Alloc(LayerUnit owner, CardTemplate data, int level)
            {
                return owner.ObjectPool.AllocOrCreateAutoRelease<CardSlot>(static s => new CardSlot()).Init(owner, data, level);
            }
            protected CardSlot Init(LayerUnit owner, CardTemplate data, int level)
            {
                this._Data = data;
                this._Level = level;
                this._Owner = owner;
                return this;
            }
            protected override void Disposing()
            {
                this._Owner = null;
                this._Data = default;
                this._Level = default;
            }
        }
        #endregion
    }
}
