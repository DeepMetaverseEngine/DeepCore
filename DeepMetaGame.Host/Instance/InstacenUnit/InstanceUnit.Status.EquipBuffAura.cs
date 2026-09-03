using DeepCore.Components;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using static DeepCore.Game3D.Host.FuncData.UnitCartridge;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 所有常态状态（Buff，技能，被动系）
    /// </summary>
    partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//

        //-----------------------------------------------------------------------------------------------
        public AbstractCollectionPool.AutoReleaseList<EquipBuff> AllocBuffsList()
        {
            var ret = this.ObjectPool.AllocList<EquipBuff>();
            this.ForEachBuffStatus(ret, static (ret, sk) => { ret.Add(sk); return false; });
            return ret;
        }
        public AbstractCollectionPool.AutoReleaseList<UnitCardSlot> AllocCardsList()
        {
            var ret = this.ObjectPool.AllocList<UnitCardSlot>();
            ret.AddRange(this.Cartridge.OwnerCards);
            return ret;
        }
        //----------------------------------------------------------------------------------------------------------
        #region EquipBuff

        public class EquipBuff : InstanceStatus
        {
            private bool End = false;
            private byte End_result;
            private bool End_replace = false;

            private TAddBuff add;
            private BuffTemplate mData;
            private InstanceUnit mOwner;
            private InstanceUnit sender;

            private BuffStateChangeAbility mStateChange;
            private BuffAvatarChangeAbility mAvatarChange;
            private BuffSpeedChangeAbility mSpeedChange;
            private BuffOverlayAbility mOverlay;

            private TimeExpire st_stun;
            private TimeExpire st_invisible;
            private TimeExpire st_invincible;
            private TimeExpire st_no_damage;
            private TimeExpire st_silent;
            private TimeExpire st_lock;

            private TimeInterval interval;
            private float total_time;
            private float passtime;
            private int overlay_level;
            private bool mRemoveOnSkillDeactivated;
            private InstanceUnit.EquipSkill mFromSkillID;
            private readonly PopupKeyFrames<BuffTemplate.KeyFrame> keyframes = new PopupKeyFrames<BuffTemplate.KeyFrame>();

            private BuffComponentCollection _components;
            public BuffComponentCollection Components
            {
                get
                {
                    if (_components == null)
                    {
                        _components = new(this, static (a, b) => a.Priority - b.Priority);
                    }
                    return _components;
                }
            }

            //是否为永久buff
            public bool IsEquip { get; protected set; }
            public int BuffLevel { get; protected set; }
            public object Tag { get; set; }
            public InstanceUnit.EquipSkill FromSkillID => mFromSkillID;
            public byte EndResult => End_result;
            protected EquipBuff() { }
            public static EquipBuff Alloc(InstanceUnit unit, TAddBuff add)
            {
                return unit.ObjectPool.AllocOrCreateAutoRelease<EquipBuff>(static s => new EquipBuff()).Init(unit, add);
            }
            protected virtual EquipBuff Init(InstanceUnit unit, TAddBuff add)
            {
                this.add = add;
                this.mData = add.template;
                this.mOwner = unit;
                this.mOwner.Retain();
                this.sender = add.sender;
                this.sender.Retain();
                this.mStateChange = mData.Abilities.GetComponentAs<BuffStateChangeAbility>();
                this.mAvatarChange = mData.Abilities.GetComponentAs<BuffAvatarChangeAbility>();
                this.mSpeedChange = mData.Abilities.GetComponentAs<BuffSpeedChangeAbility>();
                this.mOverlay = mData.Abilities.GetComponentAs<BuffOverlayAbility>();

                this.BuffLevel = add.buffLevel;
                this.IsEquip = OverrideIsEquip(add);
                this.overlay_level = add.overLayLevel;
                this.passtime = add.passTimeMS;

                this.interval = unit.AllocTimeInterval(mData.HitIntervalMS);
                this.interval.FirstTimeEnable = mData.FirstTimeEnable;

                this.keyframes.Clear();
                this.keyframes.AddRange(mData.KeyFrames);
                if (mData.IsEquip)
                    this.total_time = int.MaxValue;
                else if (add.lifeTimeMS > 0)
                    this.total_time = add.lifeTimeMS;
                else
                    this.total_time = mData.LifeTimeMS;

                this.mRemoveOnSkillDeactivated = mData.IsRemoveOnSkillDeactivated;
                this.mFromSkillID = add.FromSkillID;
                if (this.mFromSkillID != null)
                {
                    this.mFromSkillID.Retain();
                }
                this._components = BuffComponentCollection.Create(mOwner, this, sender);

                return this;
            }
            protected override void Disposing()
            {
                if (bindEvent != null)
                {
                    mOwner.RemoveCustomEvent(bindEvent);
                }
                this.bindEvent = null;

                this._components?.Clear();

                this.End = false;
                this.End_result = 0;
                this.End_replace = false;

                this.add = default;
                this.mData = default;
                this.mOwner.Release();
                this.mOwner = default;
                this.sender.Release();
                this.sender = default;

                this.mStateChange = default;
                this.mAvatarChange = default;
                this.mSpeedChange = default;
                this.mOverlay = default;

                this.st_stun?.Dispose();
                this.st_invisible?.Dispose();
                this.st_invincible?.Dispose();
                this.st_no_damage?.Dispose();
                this.st_silent?.Dispose();
                this.st_lock?.Dispose();

                this.st_stun = default;
                this.st_invisible = default;
                this.st_invincible = default;
                this.st_no_damage = default;
                this.st_silent = default;
                this.st_lock = default;

                this.interval?.Dispose();
                this.interval = default;
                this.total_time = default;
                this.passtime = default;
                this.overlay_level = default;
                this.mRemoveOnSkillDeactivated = default;
                if (this.mFromSkillID != null)
                {
                    this.mFromSkillID.Release();
                }
                this.mFromSkillID = default;
                this.keyframes.Clear();
                this.IsEquip = false;
                this.BuffLevel = 0;
                this.Tag = null;

            }

            public InstanceZone Zone { get { return mOwner.Parent; } }
            public InstanceUnit Owner { get { return mOwner; } }
            public InstanceUnit Sender { get { return sender; } }
            public uint SenderID { get { return sender.ID; } }
            public BuffTemplate Data { get { return mData; } }
            public TAddBuff Add { get => add; }
            public int ID { get { return mData.ID; } }
            public int OverlayLevel { get { return overlay_level; } }
            public float PassTimeMS { get { return passtime; } }
            public float ExpireMS { get => total_time - passtime; }
            public float ProgressAmount => IsEquip ? 0 : (total_time > 0 ? passtime / total_time : 0);
            public bool IsEnd { get => End; }
            public float LifeTimeMS { get { return total_time; } }
            public int TotalTickCount { get => interval.TotalTickCount; }
            public BuffStateChangeAbility StateChange => mStateChange;
            public BuffAvatarChangeAbility AvatarChange => mAvatarChange;
            public BuffSpeedChangeAbility SpeedChange => mSpeedChange;
            public BuffOverlayAbility Overlay => mOverlay;
            private CustomUnitEventTriggerCollection bindEvent;
            protected virtual bool OverrideIsEquip(TAddBuff add)
            {
                if (add.isEquip.HasValue)
                {
                    return add.isEquip.Value;
                }
                return mData.IsEquip;
            }

            protected virtual void OnBuffStart() { }
            protected virtual void OnBuffEnd(byte result) { }
            protected virtual void OnBuffTick() { }
            protected virtual void OnBuffUpdate() { }

            protected void SetIsEquip(bool value)
            {
                IsEquip = value;
            }
            internal void OnStart()
            {
                bindEvent = mOwner.BindCustomUnitEvent(mData);

                Zone.Formula.OnBuffBegin(mOwner, this, sender);
                OnBuffStart();
                _components?.ForEach(this, static (st, c) => c.InternalStart());

                if (StateChange)
                {
                    if (StateChange.MakeStun)
                    {
                        this.st_stun = mOwner.SetStunTimeMS(total_time);
                    }
                    if (StateChange.IsInvisible)
                    {
                        this.st_invisible = mOwner.SetInvisibleTimeMS(total_time);
                    }
                    if (StateChange.IsInvincible)
                    {
                        this.st_invincible = mOwner.SetInvincibleTimeMS(total_time);
                    }
                    if (StateChange.IsNoDamage)
                    {
                        this.st_no_damage = mOwner.SetNoDamageTimeMS(total_time);
                    }
                    if (StateChange.IsSilent)
                    {
                        this.st_silent = mOwner.SetSilentTimeMS(total_time);
                    }
                    if (StateChange.IsLockMotion)
                    {
                        this.st_lock = mOwner.SetLockTimeMS(total_time);
                    }
                    if (StateChange.LockMainStateAction != UnitActionStatus.NA)
                    {
                        Owner.ChangeState(StateBuffAction.Alloc(Owner, this, StateChange));
                    }
                }
                if (AvatarChange)
                {
                    if (AvatarChange.BodyScaleAppend > 0)//碰撞体积变更.
                    {
                        mOwner.BodyScale += AvatarChange.BodyScaleAppend;
                        //mOwner.SetBodySize(AvatarChange.BodySize);
                    }
                    if (AvatarChange.MakeAvatar)
                    {
                        if (!string.IsNullOrEmpty(AvatarChange.SkinName))
                        {
                            mOwner.Skin = AvatarChange.SkinName;
                        }
                        if (AvatarChange.SkinAvatar != null)
                        {
                            mOwner.Avatar = AvatarChange.SkinAvatar;
                        }
                    }
                    if (AvatarChange.UnitChangeSkills)
                    {
                        // mOwner.BuffActiveSkill(true, mData.UnitSkills, mData.UnitKeepSkillsID);

                        if (AvatarChange.UnitKeepSkillsID != null && AvatarChange.UnitKeepSkillsID.Count > 0)
                        {
                            using (var keeps = Owner.ObjectPool.AllocList<LaunchSkill>())
                            {
                                mOwner.GetKeepSkills(AvatarChange.UnitKeepSkillsID, keeps);

                                keeps.AddRange(AvatarChange.UnitSkills);
                                mOwner.InitSkills(AvatarChange.UnitBaseSkillID, keeps);
                            }
                        }
                        else
                        {
                            mOwner.InitSkills(AvatarChange.UnitBaseSkillID, AvatarChange.UnitSkills);
                        }

                    }
                }
                if (SpeedChange)
                {
                    if (SpeedChange.FastMoveRate != 1f)
                        Owner.MulFastMoveRate(SpeedChange.FastMoveRate);
                    if (SpeedChange.FastCastRate != 1f)
                        Owner.MulFastCastRate(SpeedChange.FastCastRate);
                    if (SpeedChange.FastActionRate != 1f)
                        Owner.MulFastActionRate(SpeedChange.FastActionRate);
                }

                mOwner.doGotBuff(this);

                if (mData.ClientVisible)
                {
                    var post = mOwner.ObjectPool.Alloc<UnitLaunchBuffEvent>().Init(mOwner.ID,
                        this.ID, this.SenderID, this.LifeTimeMS, this.IsEquip,
                        this.BuffLevel, overlay_level, passtime);
                    if (add.isDuplicate || Zone.IsLocalBattle)
                    {
                        post.template = mData;
                    }
                    mOwner.PostEvent(post);
                }
            }
            internal void OnEnd(byte result, bool replace = false)
            {
                if (End) return;
                this.End = true;
                this.End_result = result;
                this.End_replace = replace;

                this.passtime = total_time;

                if (this.st_stun != null)
                {
                    mOwner.mStunTimeMS.Remove(st_stun);
                    st_stun = null;
                }
                if (this.st_invisible != null)
                {
                    mOwner.mInvisibleTimeMS.Remove(st_invisible);
                    st_invisible = null;
                }
                if (this.st_invincible != null)
                {
                    mOwner.mInvincibleTimeMS.Remove(st_invincible);
                    st_invincible = null;
                }
                if (this.st_no_damage != null)
                {
                    mOwner.mNoDamageTimeMS.Remove(st_no_damage);
                    st_no_damage = null;
                }
                if (this.st_silent != null)
                {
                    mOwner.mSilentTimeMS.Remove(st_silent);
                    st_silent = null;
                }
                if (this.st_lock != null)
                {
                    mOwner.mLockTimeMS.Remove(st_lock);
                    st_lock = null;
                }
                if (AvatarChange)
                {
                    if (AvatarChange.MakeAvatar)
                    {
                        if (!string.IsNullOrEmpty(AvatarChange.SkinName))
                        {
                            mOwner.Skin = mOwner.AResource?.SkinName;
                        }
                        if (AvatarChange.SkinAvatar != null)
                        {
                            mOwner.Avatar = mOwner.AResource?.SkinAvatar;
                        }
                    }
                    if (AvatarChange.BodyScaleAppend > 0)//碰撞体积变更.
                    {
                        mOwner.BodyScale -= AvatarChange.BodyScaleAppend;// (mOwner.mInfo.BodySize);
                    }
                    if (AvatarChange.UnitChangeSkills)
                    {
                        mOwner.ResetSkills();
                        //mOwner.BuffActiveSkill(false, mData.UnitSkills, mData.UnitKeepSkillsID);
                    }
                }
                if (SpeedChange)
                {
                    if (SpeedChange.FastMoveRate != 1f)
                        Owner.MulFastMoveRate(1 / SpeedChange.FastMoveRate);
                    if (SpeedChange.FastCastRate != 1f)
                        Owner.MulFastCastRate(1 / SpeedChange.FastCastRate);
                    if (SpeedChange.FastActionRate != 1f)
                        Owner.MulFastActionRate(1 / SpeedChange.FastActionRate);
                }
                if (mData.EndKeyFrame != null)
                {
                    doKeyFrame(mData.EndKeyFrame);
                }

                OnBuffEnd(End_result);

                Zone.Formula.OnBuffEnd(mOwner, this, End_result);

                if (!replace)
                {
                    Zone.cb_unitRemoveBuffCallBack(mOwner, this);
                }

                _components?.ForEach(this, static (st, c) => c.InternalEnd(st.End_result));
                _components?.Clear();

                if (bindEvent != null)
                {
                    mOwner.RemoveCustomEvent(bindEvent);
                }
                bindEvent = null;

                if (this.Data.ClientVisible)
                {
                    if (!End_replace)
                    {
                        mOwner.PostEvent(mOwner.ObjectPool.Alloc<UnitStopBuffEvent>().Init(mOwner.ID, this.ID, SenderID, End_result));
                    }
                }
            }

            internal bool OnUpdate(InstanceZone zone)
            {
                if (End) return false;

                passtime += zone.UpdateIntervalMS;

                //有GC
                //keyframes.DoKeyFrames(passtime, doKeyFrame);

                //大明星
                using (var kfs = mOwner.ObjectPool.AllocList<BuffTemplate.KeyFrame>())
                {
                    var count = keyframes.PopKeyFrames(passtime, kfs);
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            doKeyFrame(kfs[i]);
                        }

                    }
                }

                if (interval.Update(zone.UpdateIntervalMS))
                {
                    doKeyFrame(mData.HitKeyFrame);
                    OnBuffTick();
                    Zone.Formula.OnBuffTick(mOwner, this, interval.TotalTickCount);
                    _components?.ForEach(this, static (st, c) => c.InternalTick());
                }

                OnBuffUpdate();

                _components?.ForEach(this, static (st, c) => c.InternalUpdate());

                return IsTimeEnd();
            }

            private void doKeyFrame(BuffTemplate.KeyFrame kf)
            {
                if (kf != null)
                {
                    if (kf.Attack != null)
                    {
                        using (var atksrc = TAttackSource.AllocWithBuff(this, kf.Attack))
                        {
                            mOwner.DoHitAttack(sender, atksrc);
                        }
                    }
                    if (kf.Spell != null)
                    {
                        mOwner.Parent.BuffLaunchSpell(sender, mOwner, this, kf.Spell, mOwner);
                    }
                    if (kf.Item != null)
                    {
                        mOwner.UseItem(kf.Item.ItemTemplateID);
                    }
                }
            }

            private bool IsTimeEnd()
            {
                if (Data.IsRemoveOnSenderRemoved)
                {
                    if (!sender.Enable) return true;
                }

                if (Data.IsRemoveOnOwnerDead)
                {
                    if (!Owner.IsActive)
                    {
                        return true;
                    }
                }
                if (IsEquip)
                {
                    return false;//!sender.IsActive && Data.IsRemoveOnOwnerDead;
                }

                if (mRemoveOnSkillDeactivated && mFromSkillID != null)
                {
                    if (sender != null)
                    {
                        var ss = mFromSkillID;
                        if (ss == null || !ss.IsActive)
                        {
                            return true;
                        }
                    }
                }


                return (passtime >= total_time);

            }

            public void AddLifeTimeMS(float timeMS, bool synctoclient = true)
            {
                this.total_time += timeMS;
                if (synctoclient)
                {
                    SyncBuffChange();
                }
            }

            public void SetPassTimeMS(float timeMS, bool synctoclient = true)
            {
                this.passtime = timeMS;
                if (synctoclient)
                {
                    SyncBuffChange();
                }

            }

            protected void SyncBuffChange()
            {
                if (!mData.ClientVisible) return;
                var evt = Zone.ObjectPool.Alloc<UnitSyncBuffEvent>();
                evt.sync.OverlayLevel = this.overlay_level;
                evt.sync.BuffTemplateID = this.mData.ID;
                evt.sync.SenderID = this.SenderID;
                evt.sync.PassTime = this.passtime;
                evt.sync.TotalTime = this.total_time;
                evt.sync.BuffLevel = this.BuffLevel;
                mOwner.PostEvent(evt);
            }

        }
        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region BuffList
        private class BuffMap : Disposable
        {
            class BuffList : IEnumerable<EquipBuff>
            {
                readonly public BuffTemplate bufft;
                private HashMap<uint, EquipBuff> map;
                private EquipBuff last;

                public BuffList(BuffTemplate bufft)
                {
                    this.bufft = bufft;
                    this.map = new HashMap<uint, EquipBuff>(1);
                }
                public void Add(EquipBuff bs)
                {
                    if (bs.Data.IsDuplicating)
                    {
                        map.Add(bs.SenderID, bs);
                    }
                    else
                    {
                        map.Add(0, bs);
                    }
                    last = bs;
                }
                public EquipBuff Remove(uint unit_id)
                {
                    if (bufft.IsDuplicating)
                    {
                        var bs = map.RemoveByKey(unit_id);
                        if (bs != null && bs == last)
                        {
                            if (map.Count > 0)
                            {
                                foreach (var bf in map.Values)
                                {
                                    last = bf;
                                    break;
                                }
                            }
                            else
                            {
                                last = null;
                            }
                        }
                        return bs;
                    }
                    else
                    {
                        last = null;
                        return map.RemoveByKey(0);
                    }
                }
                public InstanceUnit.EquipBuff Get(uint unit_id)
                {
                    if (map == null)
                    {
                        return null;
                    }
                    if (bufft.IsDuplicating)
                    {
                        return map.Get(unit_id);
                    }
                    else
                    {
                        return map.Get(0);
                    }
                }
                public int Count { get { return map.Count; } }
                public InstanceUnit.EquipBuff Last { get { return last; } }
                public Dictionary<uint, EquipBuff>.ValueCollection.Enumerator GetEnumerator()
                {
                    return map.Values.GetEnumerator();
                }
                IEnumerator<EquipBuff> IEnumerable<EquipBuff>.GetEnumerator()
                {
                    return map.Values.GetEnumerator();
                }
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return map.Values.GetEnumerator();
                }
            }
            //-----------------------------------------------------------------------
            private HashMap<int, BuffList> mObjects = new HashMap<int, BuffList>();
            internal readonly InstanceUnit owner;
            public BuffMap(InstanceUnit owner) { this.owner = owner; }
            public int TotalCount
            {
                get
                {
                    int ret = 0;
                    foreach (var list in mObjects.Values)
                    {
                        ret += list.Count;
                    }
                    ret += mDisposingList.Count;
                    return ret;
                }
            }
            protected override void Disposing()
            {
                disposeBuffs();
                foreach (var list in mObjects.Values)
                {
                    foreach (var buff in list)
                    {
                        buff.Dispose();
                    }
                }
                mObjects.Clear();
            }
            private void disposeBuffs()
            {
                try
                {
                    var list = mDisposingList;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var bs = list[i];
                        bs.Dispose();
                    }
                }
                finally { mDisposingList.Clear(); }
            }
            //-----------------------------------------------------------------------
            // read
            //-----------------------------------------------------------------------
            public InstanceUnit.EquipBuff GetObject(int id)
            {
                BuffList list;
                if (mObjects.TryGetValue(id, out list))
                {
                    return list.Last;
                }
                return null;
            }
            public void GetObjects(int id, List<InstanceUnit.EquipBuff> ret)
            {
                BuffList list;
                if (mObjects.TryGetValue(id, out list))
                {
                    ret.AddRange(list);
                }
            }
            public InstanceUnit.EquipBuff GetObject(int buff_id, uint unit_id)
            {
                BuffList list;
                if (mObjects.TryGetValue(buff_id, out list))
                {
                    return list.Get(unit_id);
                }
                return null;
            }
            //-----------------------------------------------------------------------
            // 
            //-----------------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            /// <param name="action">返回true终止迭代!</param>
            public EquipBuff ForEachRead<ST>(ST st, ForEachPredicate<ST, InstanceUnit.EquipBuff> action)
            {
                foreach (BuffList list in mObjects.Values)
                {
                    foreach (InstanceUnit.EquipBuff buff in list)
                    {
                        if (action(st, buff))
                        {
                            return buff;
                        }
                    }
                }
                return null;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="action">返回true终止迭代!</param>
            public EquipBuff ForEachWrite<ST>(ST st, ForEachPredicate<ST, InstanceUnit.EquipBuff> action)
            {
                using (var list = owner.ObjectPool.AllocList<EquipBuff>())
                {
                    ToList(list);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (action(st, list[i]))
                        {
                            return list[i];
                        }
                    }
                }
                return null;
            }

            //-----------------------------------------------------------------------
            // write
            //-----------------------------------------------------------------------
            public bool RemoveObject(InstanceUnit.EquipBuff bs, byte result, bool replace = false)
            {
                if (mObjects.TryGetValue(bs.ID, out var list))
                {
                    var exe = list.Remove(bs.SenderID);
                    if (exe == bs)
                    {
                        bs.OnEnd(result, replace);
                        mDisposingList.Add(bs);
                        return true;
                    }
                    else if (exe != null)
                    {
                        bs.OnEnd(result, replace);
                        mDisposingList.Add(bs);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            //             public InstanceUnit.EquipBuff RemoveObject(int buffID, uint unit_id)
            //             {
            //                 BuffList list;
            //                 if (mObjects.TryGetValue(buffID, out list))
            //                 {
            //                     dirty = true;
            //                     return list.Remove(unit_id);
            //                 }
            //                 return null;
            //             }

            public void AddObject(InstanceUnit.EquipBuff obj)
            {
                BuffList list = mObjects.Get(obj.ID);
                if (list == null)
                {
                    list = new BuffList(obj.Data);
                    mObjects.Add(obj.ID, list);
                }
                list.Add(obj);
                obj.OnStart();
            }

            private void ToList(List<EquipBuff> ret)
            {
                if (mObjects.Count == 0) return;

                foreach (BuffList list in mObjects.Values)
                {
                    if (list.Count == 0) continue;

                    foreach (InstanceUnit.EquipBuff buff in list)
                    {
                        ret.Add(buff);
                    }
                }
            }

            private List<EquipBuff> mDisposingList = new List<EquipBuff>();
            public void Update(InstanceZone parent)
            {
                if (mObjects.Count == 0) return;

                using (var list = parent.ObjectPool.AllocList<EquipBuff>())
                {
                    ToList(list);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var bs = list[i];
                        if (!bs.IsEnd)
                        {
                            if (bs.OnUpdate(parent))
                            {
                                this.RemoveObject(bs, UnitStopBuffEvent.EndResult_ByTimeUp);
                            }
                        }
                    }
                }
                if (mDisposingList.Count > 0)
                {
                    disposeBuffs();
                }
            }
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------

        private readonly BuffMap mBuffs;
        public int TotalBuffCount => mBuffs.TotalCount;
        // 单位获取道具 
        internal void doGotBuff(InstanceUnit.EquipBuff buff)
        {
            Parent.cb_unitGotBuffCallBack(this, buff);
        }

        internal void doRemoveBuff(InstanceUnit.EquipBuff buff)
        {
            Parent.cb_unitRemoveBuffCallBack(this, buff);
        }
        private void updateBuffs()
        {
            mBuffs.Update(Parent);
        }
        private void cleanBuffs()
        {
            mBuffs.Dispose();
        }

        /// <summary>
        /// 是否存在技能变身BUFF
        /// </summary>
        public bool IsBuffChangeSkills
        {
            get
            {
                return GetChangeSkillBuff() != null;
            }
        }
        public void GetCurrentBuffStatus(IList<ClientStruct.UnitBuffStatus> ret)
        {
            {
                mBuffs.ForEachRead(ret, static (ret, buff) =>
                {
                    if (buff.Data.ClientVisible)
                    {
                        var bf = new ClientStruct.UnitBuffStatus();
                        bf.BuffTemplateID = (buff.Data.ID);
                        bf.SenderID = buff.SenderID;
                        bf.IsEquip = buff.IsEquip;
                        bf.TotalTime = buff.LifeTimeMS;
                        bf.PassTime = (buff.PassTimeMS);
                        bf.OverlayLevel = buff.OverlayLevel;
                        bf.BuffLevel = buff.BuffLevel;
                        ret.Add(bf);
                    }
                    return false;
                });
            }
        }
        public void GetAllBuffStatus(List<EquipBuff> ret)
        {
            mBuffs.ForEachRead(ret, static (ret, bf) =>
            {
                ret.Add(bf);
                return false;
            });
        }
        public EquipBuff ForEachBuffStatus<ST>(ST st, ForEachPredicate<ST, EquipBuff> actionT)
        {
            return mBuffs.ForEachRead(st, actionT);
        }
        public void ForEachBuffStatus<ST>(ST st, ForEachAction<ST, EquipBuff> actionT)
        {
            mBuffs.ForEachRead((st, actionT), static (st, buff) =>
            {
                st.actionT(st.st, buff);
                return false;
            });
        }
        public void RefreshBuffData(BuffTemplate temp)
        {
            using (var list = ObjectPool.AllocList<EquipBuff>())
            {
                this.mBuffs.GetObjects(temp.ID, list);
                foreach (var buff in list)
                {
                    if (buff.ID == temp.ID && buff.Sender == this)
                    {
                        RemoveBuff(buff, UnitStopBuffEvent.EndResult_ByReplaced);

                        var add = buff.Add;
                        add.template = temp;
                        var addbuff = AddBuff(add);
                        addbuff.SetPassTimeMS(buff.PassTimeMS);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 获得技能变身BUFF
        /// </summary>
        public EquipBuff GetChangeSkillBuff()
        {
            var ret = mBuffs.ForEachRead(this, static (owner, bs) =>
            {
                if (bs.AvatarChange && bs.AvatarChange.UnitChangeSkills && !bs.IsEnd)
                {
                    return true;
                }
                return false;
            });
            return ret;
        }
        public EquipBuff GetBuffByID(int buffTemplateID)
        {
            return mBuffs.GetObject(buffTemplateID);
        }

        public void GetBuffByIDs(int buffTemplateID, List<EquipBuff> ret)
        {
            mBuffs.GetObjects(buffTemplateID, ret);
        }
        public bool TryGetBuffByComponent<T>(out T component) where T : BuffComponent
        {
            using (var st = Zone.ObjectPool.AllocForEach1<EquipBuff, T>(default(T)))
            {
                mBuffs.ForEachRead(st, static (st, bf) =>
                {
                    if (bf.Components.TryGetComponentAs<T>(out var comp))
                    {
                        st.Arg1 = comp;
                        return true;
                    }
                    return false;
                });
                component = st.Arg1;
                return component != null;
            }
        }
        public EquipBuff GetBuffByIDAndSender(int buffTemplateID, uint senderUnitID)
        {
            return mBuffs.GetObject(buffTemplateID, senderUnitID);
        }
        public EquipBuff AddBuff(int buffTemplateID, int BuffLevel, InstanceUnit sender, InstanceUnit.EquipSkill fromSkillID = null)
        {
            var buff = sender.Cartridge.GetBuff(buffTemplateID, BuffLevel);
            if (buff != null)
            {
                return AddBuff(buff, BuffLevel, sender, fromSkillID);
            }
            return null;
        }
        public EquipBuff AddBuff(LaunchBuff buff, InstanceUnit sender, InstanceUnit.EquipSkill fromSkillID = null)
        {
            if (CUtils.RandomPercent(RandomN, buff.LaunchPercent))
            {
                var bt = sender.Cartridge.GetBuff(buff.BuffID, buff.BuffLevel);
                if (bt != null)
                {
                    return this.AddBuff(new TAddBuff()
                    {
                        buffLevel = buff.BuffLevel,
                        template = bt,
                        sender = sender,
                        FromSkillID = fromSkillID,
                    });
                }
            }
            return null;
        }
        public EquipBuff AddBuff(BuffTemplate buff, int BuffLevel, InstanceUnit sender, InstanceUnit.EquipSkill fromSkillID = null)
        {
            var add = new Instance.TAddBuff
            {
                buffLevel = BuffLevel,
                template = buff,
                sender = sender,
                FromSkillID = fromSkillID,
            };
            return AddBuff(add);
        }
        public EquipBuff AddBuff(TAddBuff add)
        {
            if (add.template == null) { return null; }

            add.unit = this;

            if (Parent.Formula.TryAddBuff(ref add))
            {
                if (!add.template.IsEquip && add.template.LifeTimeMS <= 0 && add.lifeTimeMS == 0)
                {
                    return null;
                }
                if (!TryRemoveExclusiveBuff(add))
                {
                    return null;
                }
                var old = mBuffs.GetObject(add.template.ID, add.sender.ID);
                if (old != null)
                {
                    if (old.Overlay)
                    {
                        add.overLayLevel = (Math.Min(old.OverlayLevel + 1, old.Overlay.MaxOverlay));
                    }
                    mBuffs.RemoveObject(old, UnitStopBuffEvent.EndResult_ByReplaced, true);
                    add.removed = old;
                }
                var newbs = Zone.CreateUnitBuffState(this, add);
                newbs.Tag = add.tag;
                mBuffs.AddObject(newbs);
                return newbs;
            }
            return null;
        }

        /// <summary>
        /// 移除互斥技能，根据优先级
        /// </summary>
        protected virtual bool TryRemoveExclusiveBuff(in TAddBuff add)
        {
            var buffID = add.template.ID;
            var catgory = add.template.ExclusiveCatgory;
            var priority = add.template.ExclusivePriority;

            // 检测当时有更高优先级 //
            if (mBuffs.ForEachRead(add, static (add, bs) =>
            {
                var buffID = add.template.ID;
                var catgory = add.template.ExclusiveCatgory;
                var priority = add.template.ExclusivePriority;
                //判断是否可重复//
                if (bs.ID == buffID)
                {
                    if (!add.template.IsDuplicating || bs.SenderID == add.sender.ID)
                    {
                        if (add.template.ExclusiveLevel && bs.BuffLevel > add.buffLevel)
                        {
                            return true;
                        }
                    }
                }
                else if (catgory != 0)
                {
                    if ((bs.Data.ExclusiveCatgory == catgory) && (bs.Data.ExclusivePriority > priority))
                    {
                        return true;
                    }
                }
                return false;
            }) != null)
            {
                return false;
            }
            if (catgory != 0)
            {
                // 移除低优先级 //
                mBuffs.ForEachWrite((mBuffs, add, catgory, buffID), static (st, bs) =>
                {
                    if ((bs.ID != st.buffID) && (bs.Data.ExclusiveCatgory == st.catgory))
                    {
                        st.mBuffs.RemoveObject(bs, UnitStopBuffEvent.EndResult_ByCatgoryExclusive);
                    }
                    return false;
                });
            }
            return true;
        }
        public bool RemoveBuff(EquipBuff bs, byte result = UnitStopBuffEvent.EndResult_ByCode)
        {
            if (Parent.Formula.TryRemoveBuff(bs, result) && mBuffs.RemoveObject(bs, result))
            {
                return true;
            }
            return false;
        }
        public bool RemoveBuff(int buffID, int level, byte result = UnitStopBuffEvent.EndResult_ByCode)
        {
            using (var bss = ObjectPool.AllocList<EquipBuff>())
            {
                mBuffs.GetObjects(buffID, bss);
                if (bss.Count > 0)
                {
                    foreach (EquipBuff bs in bss)
                    {
                        if (Parent.Formula.TryRemoveBuff(bs, level, result) && mBuffs.RemoveObject(bs, result))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        public bool RemoveBuff(int buffID, byte result = UnitStopBuffEvent.EndResult_ByCode)
        {
            using (var bss = ObjectPool.AllocList<EquipBuff>())
            {
                mBuffs.GetObjects(buffID, bss);
                if (bss.Count > 0)
                {
                    foreach (EquipBuff bs in bss)
                    {
                        if (Parent.Formula.TryRemoveBuff(bs, result) && mBuffs.RemoveObject(bs, result))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool RemoveBuffBySender(int buffID, uint senderUnitID, byte result = UnitStopBuffEvent.EndResult_ByCode)
        {
            var bs = GetBuffByIDAndSender(buffID, senderUnitID);
            if (bs != null)
            {
                if (Parent.Formula.TryRemoveBuff(bs, result) && mBuffs.RemoveObject(bs, result))
                {
                    return true;
                }
            }

            return false;
        }
        public void ClearBuffs(byte result = UnitStopBuffEvent.EndResult_ByCode)
        {
            mBuffs.ForEachWrite((result, mBuffs, Parent), static (st, bs) =>
            {
                if (!bs.Data.IsPassive && !bs.IsEquip)
                {
                    if (!bs.Data.IsRemoveOnOwnerDead && st.result == UnitStopBuffEvent.EndResult_ByDead)
                    {
                        //DO NOTHING
                    }
                    else
                    {
                        if (st.Parent.Formula.TryRemoveBuff(bs, st.result) && st.mBuffs.RemoveObject(bs, st.result))
                        {

                        }
                    }
                }
                return false;
            });
        }

        protected EquipBuff AddAuraBuff(LaunchBuff buff, EquipAura aura)
        {
            var sender = aura.Owner;
            var bt = sender.Cartridge.GetBuff(buff.BuffID, buff.BuffLevel);
            if (bt != null)
            {
                return this.AddBuff(new TAddBuff()
                {
                    buffLevel = buff.BuffLevel,
                    template = bt,
                    sender = sender,
                    FromSkillID = aura.FromSkillTemplateID,
                    isEquip = true,
                });
            }
            return null;
        }
        protected bool RemoveAuraBuff(int buffID, EquipAura aura)
        {
            return this.RemoveBuffBySender(buffID, aura.Owner.ObjectID, UnitStopBuffEvent.EndResult_OutAura);
        }
        //---------------------------------------------------------------------------------------------------------------


        private readonly HashMap<int, EquipAura> mBindingAuras = new HashMap<int, EquipAura>();

        public class EquipAura : InstanceStatus, IViewTriggerListener<InstanceUnit>
        {
            private ViewTriggerSphereCenter<InstanceUnit> mViewTrigger;
            private bool mIsActive;
            private TimeExpire mExpire;
            private InstanceUnit.EquipSkill fromSkillTemplateID;
            private AuraTemplate data;
            private int id;
            private int level;
            private InstanceUnit owner;
            private object tag;

            protected EquipAura() { }
            public static EquipAura Alloc(InstanceUnit unit, AuraTemplate aura, int level, InstanceUnit.EquipSkill fromSkillTemplateId)
            {
                return unit.ObjectPool.AllocOrCreateAutoRelease<EquipAura>(static s => new EquipAura()).Init(unit, aura, level, fromSkillTemplateId);
            }
            protected virtual EquipAura Init(InstanceUnit unit, AuraTemplate aura, int level, InstanceUnit.EquipSkill fromSkillTemplateId)
            {
                this.data = aura;
                this.id = aura.ID;
                this.owner = unit;
                this.level = level;
                this.mViewTrigger = new ViewTriggerSphereCenter<InstanceUnit>(unit.Parent, unit.Position, aura.Range);
                this.mViewTrigger.SetListener(this);
                this.mIsActive = true;
                this.mExpire = aura.LifeTimeMS > 0 ? unit.AllocTimeExpire(aura.LifeTimeMS) : null;
                this.fromSkillTemplateID = fromSkillTemplateId;
                return this;
            }
            protected override void Disposing()
            {
                this.mViewTrigger?.Dispose();
                this.mViewTrigger = null;
                this.mIsActive = default;
                this.mExpire?.Dispose();
                this.mExpire = default;
                this.fromSkillTemplateID = default;
                this.data = default;
                this.id = default;
                this.level = default;
                this.owner = default;
                this.tag = default;
            }

            public AuraTemplate Data { get => data; }
            public int ID { get => id; }
            public int Level { get => level; }
            public InstanceUnit Owner { get => owner; }
            public bool IsActive { get => mIsActive; }
            public object Tag { get => tag; set => tag = value; }
            public InstanceUnit.EquipSkill FromSkillTemplateID { get => fromSkillTemplateID; }
            public void RefreshData(AuraTemplate aura)
            {
                this.data = aura;
                this.mViewTrigger?.Dispose();
                this.mViewTrigger = new ViewTriggerSphereCenter<InstanceUnit>(Owner.Parent, Owner.Position, aura.Range);
                this.mViewTrigger.SetListener(this);
            }
            public bool Remove()
            {
                return owner.RemoveAura(this.id);
            }
            internal void Stop()
            {
                this.mIsActive = false;
                using (var list = Owner.ObjectPool.AllocList<InstanceUnit>())
                {
                    this.mViewTrigger.ForEachViewd(obj =>
                    {
                        list.Add(obj);
                        foreach (var buff in Data.BindingBuffs)
                        {
                            obj.RemoveBuff(buff.BuffID);
                        }
                        return false;
                    });
                    foreach (var obj in list)
                    {
                        Owner.Parent.cb_OnUnitLeaveAura(obj, this);
                    }
                }
                this.mViewTrigger.Dispose();
            }
            internal bool Update()
            {
                //源技能停用时,光环同步移除效果
                if (Data.RemoveOnSkillDeactivated && FromSkillTemplateID != null)
                {
                    var ss = FromSkillTemplateID;
                    if (ss == null || ss.IsActive == false)
                        return true;
                }


                if (this.mExpire != null && this.mExpire.Update(Owner.Parent.UpdateIntervalMS))
                {
                    return true;
                }
                else
                {
                    this.mViewTrigger.LookUpdate(Owner.Position);
                    return false;
                }
            }
            void IViewTriggerListener<InstanceUnit>.OnObjectEnterView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
            {
                foreach (var buff in Data.BindingBuffs)
                {
                    obj.AddAuraBuff(buff, this);
                }
                Owner.Parent.cb_OnUnitEnterAura(obj, this);
            }
            void IViewTriggerListener<InstanceUnit>.OnObjectLeaveView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
            {
                foreach (var buff in Data.BindingBuffs)
                {
                    obj.RemoveAuraBuff(buff.BuffID, this);
                }
                Owner.Parent.cb_OnUnitLeaveAura(obj, this);
            }
            bool IViewTriggerListener<InstanceUnit>.Select(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
            {
                if (!obj.IsActive) return false;
                return Owner.Parent.Formula.IsExpectTarget(Owner, obj, this.Data.ExpectTarget);
            }
            internal ClientStruct.UnitAuraStatus GetStatus()
            {
                return new ClientStruct.UnitAuraStatus()
                {
                    AuraTemplateID = this.ID,
                    Range = this.Data.Range,
                    TotalTime = (float)(mExpire != null ? mExpire.TotalTimeMS : 0),
                    PassTime = (float)(mExpire != null ? mExpire.PassTimeMS : 0),
                };
            }
        }
        public void GetCurrentAuraStatus(IList<ClientStruct.UnitAuraStatus> ret)
        {
            {
                foreach (var e in mBindingAuras.Values)
                {
                    ret.Add(e.GetStatus());
                }
            }
        }
        public virtual EquipAura LearnAura(UnitCartridge cartridge, CardTemplate card, int auraID)
        {
            return LaunchAura(auraID);
        }
        public EquipAura LaunchAura(int auraID, int level = 0, InstanceUnit.EquipSkill fromSkillID = null)
        {
            var aura = Cartridge.GetAura(auraID);
            if (aura != null) { return LaunchAura(aura, level, fromSkillID); }
            return null;
        }
        public EquipAura LaunchAura(LaunchAura aura, InstanceUnit.EquipSkill fromSkillID = null)
        {
            if (RandomN.RandomPercent(aura.LaunchPercent))
            {
                return LaunchAura(aura.AuraID, aura.AuraLevel, fromSkillID);
            }
            return null;
        }
        public EquipAura LaunchAura(AuraTemplate aura, int level, InstanceUnit.EquipSkill fromSkillID = null)
        {
            if (mBindingAuras.TryGetValue(aura.ID, out var exist))
            {
                if (exist.Level < level)
                {
                    if (Parent.Formula.TryLaunchAura(this, aura, level))
                    {
                        exist.Stop();
                        exist.Dispose();
                        var st = Zone.CreateUnitAuraState(this, aura, level, fromSkillID);
                        mBindingAuras.Put(aura.ID, st);
                        Parent.cb_OnUnitLaunchAura(this, st);
                        this.PostEvent(ObjectPool.Alloc<UnitLaunchAuraEvent>().Init(this.ID, aura.ID, aura.LifeTimeMS, 0, aura.Range));
                        return st;
                    }
                }
            }
            else
            {
                if (Parent.Formula.TryLaunchAura(this, aura, level))
                {
                    var st = Zone.CreateUnitAuraState(this, aura, level, fromSkillID);
                    mBindingAuras.Add(aura.ID, st);
                    Parent.cb_OnUnitLaunchAura(this, st);
                    this.PostEvent(ObjectPool.Alloc<UnitLaunchAuraEvent>().Init(this.ID, aura.ID, aura.LifeTimeMS, 0, aura.Range));
                    return st;
                }
            }
            return null;
        }
        public bool RemoveAura(int templateID)
        {
            if (mBindingAuras.TryRemove(templateID, out var aura))
            {
                this.PostEvent(ObjectPool.Alloc<UnitStopAuraEvent>().Init(this.ID, aura.ID));
                aura.Stop();
                aura.Dispose();
                return true;
            }
            return false;
        }
        private void cleanAura()
        {
            foreach (var aura in mBindingAuras.Values)
            {
                aura.Dispose();
            }
            mBindingAuras.Clear();
        }
        public bool HasAura(int templateID)
        {
            return mBindingAuras.ContainsKey(templateID);
        }
        public EquipAura GetAura(int templateID)
        {
            return mBindingAuras.Get(templateID);
        }

        private void updateAuras()
        {
            //提前判断，ObjectPool.AllocList耗
            if (mBindingAuras == null || mBindingAuras.Count == 0) return;

            using (var removed = ObjectPool.AllocList<EquipAura>())
            {
                foreach (var aura in mBindingAuras.Values)
                {
                    if (aura.Update())
                    {
                        removed.Add(aura);
                    }
                }
                if (removed.Count > 0)
                {
                    foreach (var r in removed)
                    {
                        RemoveAura(r.ID);
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------

    }
}
