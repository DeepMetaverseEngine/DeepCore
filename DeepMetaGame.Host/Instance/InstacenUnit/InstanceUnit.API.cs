using DeepCore.Game3D.Host.FuncData;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {


        //-----------------------------------------------------------------------------------------------------------

        protected readonly SyncUnitInfo mSyncInfo;
        private UnitFieldChangedEvent mSyncFields;
        private uint m_currentTarget;
        private InstanceUnit lastLaunchSkillTarget;
        private bool m_paused = false;
        public override bool IsPaused
        {
            get => m_paused; set
            {
                if (m_paused != value)
                {
                    m_paused = value;
                    syncFields(UnitFieldMask.MASK_PAUSED, value);
                }
            }
        }

        public void SetMaxHP(long value, bool auto_update_hp = false)
        {
            if (__mCurrentHP.SetMax(value, auto_update_hp))
            {
                syncFields(UnitFieldMask.MASK_HP | UnitFieldMask.MASK_MAX_HP, value);
            }
        }
        public void SetMaxMP(long value, bool auto_update_mp = false)
        {
            if (__mCurrentMP.SetMax(value, auto_update_mp))
            {
                syncFields(UnitFieldMask.MASK_MP | UnitFieldMask.MASK_MAX_MP, value);
            }
        }
        public void SetMaxSP(long value, bool auto_update_mp = false)
        {
            if (__mCurrentSP.SetMax(value, auto_update_mp))
            {
                syncFields(UnitFieldMask.MASK_SP | UnitFieldMask.MASK_MAX_SP, value);
            }
        }

        [Desc("最大血量")]
        public long MaxHP
        {
            get { return __mCurrentHP.Max; }
            set
            {
                if (__mCurrentHP.SetMax(value, true))
                {
                    syncFields(UnitFieldMask.MASK_HP | UnitFieldMask.MASK_MAX_HP, value);
                }
            }
        }
        [Desc("最大能量")]
        public long MaxMP
        {
            get { return __mCurrentMP.Max; }
            set
            {
                if (__mCurrentMP.SetMax(value, true))
                {
                    syncFields(UnitFieldMask.MASK_MP | UnitFieldMask.MASK_MAX_MP, value);
                }
            }
        }
        [Desc("最大精力")]
        public long MaxSP
        {
            get { return __mCurrentSP.Max; }
            set
            {
                if (__mCurrentSP.SetMax(value, true))
                {
                    syncFields(UnitFieldMask.MASK_SP | UnitFieldMask.MASK_MAX_SP, value);
                }
            }
        }
        [Desc("当前血量")]
        public long CurrentHP
        {
            get { return __mCurrentHP.Value; }
            set
            {
                if (!IsDead && __mCurrentHP.SetValue(value))
                {
                    syncFields(UnitFieldMask.MASK_HP, value);
                }
            }
        }


        [Desc("当前能量")]
        public long CurrentMP
        {
            get { return __mCurrentMP.Value; }
            set
            {
                if (__mCurrentMP.SetValue(value))
                {
                    syncFields(UnitFieldMask.MASK_MP, value);
                }
            }
        }
        [Desc("当前精力")]
        public long CurrentSP
        {
            get { return __mCurrentSP.Value; }
            set
            {
                if (__mCurrentSP.SetValue(value))
                {
                    syncFields(UnitFieldMask.MASK_SP, value);
                }
            }
        }

        [Desc("当前血量百分比")]
        public float CurrentHP_Pct { get { return __mCurrentHP.Value * 100f / __mCurrentHP.Max; } }
        [Desc("当前能量百分比")]
        public float CurrentMP_Pct { get { return __mCurrentMP.Value * 100f / __mCurrentMP.Max; } }

        [Desc("当前金币")]
        public long CurrentMoney
        {
            get { return mMoney; }
        }
        [Desc("当前等级")]
        public int Level
        {
            get { return mLevel; }
            set
            {
                if (mLevel != value)
                {
                    mLevel = value;
                    syncFields(UnitFieldMask.MASK_LEVEL, value);
                }
            }
        }
        [Desc("当前经验")]
        public long Exp
        {
            get { return mExp; }
        }
        [Desc("捡取范围")]
        public float BasePickRange
        {
            get => __mPickRange;
            set
            {
                if (__mPickRange != value)
                {
                    __mPickRange = value;
                    syncFields(UnitFieldMask.MASK_PICK_RANGE, value);
                }
            }
        }
        [Desc("体型缩放")]
        public float BodyScale
        {
            get => __mBodyScale;
            set
            {
                if (__mBodyScale != value)
                {
                    __mBodyScale = value;
                    syncFields(UnitFieldMask.MASK_BODY_SCALE, value);
                }
            }
        }

        [Desc("资源缩放")]
        public float ResScale
        {
            get => __mResScale;
            set
            {
                if (__mResScale != value)
                {
                    __mResScale = value;
                    syncFields(UnitFieldMask.MASK_RES_SCALE, value);
                }
            }
        }

        [Desc("背包数量")]
        public int InventorySize
        {
            get { return __InventorySize; }
            set
            {
                if (__InventorySize != value)
                {
                    __InventorySize = value;
                    syncFields(UnitFieldMask.MASK_INVENTORY, value);
                    OnResetInventorySize();
                }
            }
        }

        [Desc("显示名称")]
        public string DisplayName
        {
            get { return mDisplayName; }
            set
            {
                if (mDisplayName != value)
                {
                    mDisplayName = value;
                    syncFields(UnitFieldMask.MASK_DISPLAY_NAME, value);
                }
            }
        }
        public void AddHP(long hp)
        {
            AddHP(hp, false);
        }

        public void AddHP(long hp, bool ignoreDead)
        {
            if (!IsDead || ignoreDead)
            {
                if (__mCurrentHP.Add(hp))
                {
                    syncFields(UnitFieldMask.MASK_HP, hp);
                }
            }
        }

        public void AddHP_Pct(float percent, bool ignoreDead)
        {
            AddHP((int)(percent / 100f * MaxHP), ignoreDead);
        }

        public void AddMP(long mp)
        {
            if (!IsDead)
            {
                if (__mCurrentMP.Add(mp))
                {
                    syncFields(UnitFieldMask.MASK_MP, mp);
                }
            }
        }
        public void AddMP_Pct(float percent)
        {
            AddMP((int)(percent / 100f * MaxMP));
        }
        public void AddSP(long st)
        {
            if (!IsDead)
            {
                if (__mCurrentSP.Add(st))
                {
                    syncFields(UnitFieldMask.MASK_SP, st);
                }
            }
        }
        public void AddSP_Pct(float percent)
        {
            AddSP((int)(percent / 100f * MaxSP));
        }


        public void AddHP(long hp, InstanceUnit sender)
        {
            if (hp != 0)
            {
                ReduceHP(-hp, sender);
            }
        }
        public void AddHP_Pct(float percent, InstanceUnit sender)
        {
            AddHP((long)(percent / 100f * MaxHP), sender);
        }


        public void AddHP_Pct(float percent, InstanceUnit sender, bool sendMsg)
        {
            AddHP((long)(percent / 100f * MaxHP), sender, sendMsg);
        }

        public void AddHP(long hp, InstanceUnit sender, bool sendMsg)
        {
            ReduceHP(-hp, sender, sendMsg);
        }

        public void AddHP_Pct(float percent, InstanceUnit sender, bool sendMsg, bool ignoreDead)
        {
            AddHP((int)(percent / 100f * MaxHP), sender, sendMsg, ignoreDead);
        }

        public void AddHP(long hp, InstanceUnit sender, bool sendMsg, bool ignoreDead)
        {
            ReduceHP(-hp, sender, sendMsg, null, ignoreDead);
        }

        public void SetExp(long v)
        {
            var add = v - mExp;
            AddExp(add);
        }
        public void AddExp(long add)
        {
            if (TryAddExp(ref add))
            {
                var oldExp = mExp;
                this.mExp = mExp + add;
                if (add > 0)
                {
                    if (Zone.Formula.TryLevelUP(this, oldExp, this.Exp))
                    {
                        doLevelUp();
                    }
                }
                syncFields(UnitFieldMask.MASK_EXP, mExp);
            }
        }
        protected virtual bool TryAddExp(ref long add)
        {
            return add != 0;
        }
        public void AddMoney(long add)
        {
            if (TryAddMoney(ref add))
            {
                mMoney += add;
                syncFields(UnitFieldMask.MASK_MONEY, mMoney);
                Parent.cb_unitGotMoneyCallBack(this, add);
            }
        }
        protected virtual bool TryAddMoney(ref long value)
        {
            return value != 0;
        }


        public void Rebirth(long hp, long mp)
        {
            if (IsDead)
            {
                doRebirth(hp, mp);
                DoSomething();
            }
        }

        public virtual EquipBuff LearnBuff(UnitCartridge cartridge, CardTemplate card, int buffID)
        {
            if (this.GetBuffByID(buffID) == null)
            {
                BuffTemplate buff = Cartridge.GetBuff(buffID, 0);
                if (buff != null)
                {
                    return this.AddBuff(buff, 0, this, null);
                }
            }
            return null;
        }
        public EquipBuff AddBuff(int buffID, int buffLevel = 0)
        {
            BuffTemplate buff = Cartridge?.GetBuff(buffID, buffLevel);
            if (buff != null)
            {
                return this.AddBuff(buff, buffLevel, this, null);
            }
            return null;
        }
        public EquipBuff AddBuff(int buffID, int buffLevel, InstanceUnit sender)
        {
            BuffTemplate buff = sender.Cartridge.GetBuff(buffID, buffLevel);
            if (buff != null)
            {
                return this.AddBuff(buff, buffLevel, sender, null);
            }
            return null;
        }

        /// <summary>
        /// 直接设置移动速度
        /// </summary>
        /// <param name="speed"></param>
        public void SetMoveSpeed(float speed)
        {
            if (speed != __mCurrentMoveSpeedSEC)
            {
                __mCurrentMoveSpeedSEC = speed;
                syncFields(UnitFieldMask.MASK_SPEED, speed);
            }
        }

        public void SetFastMoveRate(float rate)
        {
            if (rate <= 0)
            {
                rate = 0.1f;
            }
            if (__mFastMoveRate != rate)
            {
                __mFastMoveRate = rate;
                syncFields(UnitFieldMask.MASK_FMR, rate);
            }
        }
        public void SetFastActionRate(float rate)
        {
            if (rate <= 0)
            {
                rate = 0.1f;
            }
            if (rate != __mFastActionRate)
            {
                __mFastActionRate = rate;
                syncFields(UnitFieldMask.MASK_FAR, rate);
            }
        }
        public void SetFastCastRate(float rate)
        {
            if (rate <= 0)
            {
                rate = 0.001f;
            }
            if (__mFastCastRate != rate)
            {
                __mFastCastRate = rate;
                syncFields(UnitFieldMask.MASK_FCR, rate);
            }
        }
        public void MulFastMoveRate(float rate)
        {
            SetFastMoveRate(FastMoveRate * rate);
        }
        public void MulFastActionRate(float rate)
        {
            SetFastActionRate(FastActionRate * rate);
        }
        public void MulFastCastRate(float rate)
        {
            SetFastCastRate(FastCastRate * rate);
        }

        private int m_dummy_0, m_dummy_1, m_dummy_2, m_dummy_3, m_dummy_4, m_dummy_5;
        public int Dummy_0
        {
            get { return m_dummy_0; }
            set
            {
                if (m_dummy_0 != value)
                {
                    m_dummy_0 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_0, value);
                }
            }
        }
        public int Dummy_1
        {
            get { return m_dummy_1; }
            set
            {
                if (m_dummy_1 != value)
                {
                    m_dummy_1 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_1, value);
                }
            }
        }
        public int Dummy_2
        {
            get { return m_dummy_2; }
            set
            {
                if (m_dummy_2 != value)
                {
                    m_dummy_2 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_2, value);
                }
            }
        }
        public int Dummy_3
        {
            get { return m_dummy_3; }
            set
            {
                if (m_dummy_3 != value)
                {
                    m_dummy_3 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_3, value);
                }
            }
        }
        public int Dummy_4
        {
            get { return m_dummy_4; }
            set
            {
                if (m_dummy_4 != value)
                {
                    m_dummy_4 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_4, value);
                }
            }
        }
        public int Dummy_5
        {
            get { return m_dummy_5; }
            set
            {
                if (m_dummy_5 != value)
                {
                    m_dummy_5 = value;
                    syncFields(UnitFieldMask.MASK_DUMMY_5, value);
                }
            }
        }

        private string m_skin;
        private string[] m_avatar;
        public string Skin
        {
            get { return m_skin; }
            set
            {
                if (m_skin != value)
                {
                    m_skin = value;
                    syncFields(UnitFieldMask.MASK_SKIN, value);
                }
            }
        }
        public string[] Avatar
        {
            get { return m_avatar; }
            set
            {
                if (!CUtils.ArraysEqual(m_avatar, value))
                {
                    m_avatar = value;
                    syncFields(UnitFieldMask.MASK_AVATAR, value);
                }
            }
        }

        public override IZoneShape ZoneShape
        {
            get => base.ZoneShape;
            set
            {
                base.ZoneShape = value;
                syncFields(UnitFieldMask.MASK_ZONE_SHAPE, value);
            }
        }
        //         public int Dummy_3
        //         {
        //             get { return m_dummy_3; }
        //             set
        //             {
        //                 if (m_dummy_3 != value)
        //                 {
        //                     m_dummy_3 = value;
        //                     syncFields(FieldMask.MASK_DUMMY_3);
        //                 }
        //             }
        //         }
        public uint CurrentTargetID
        {
            get { return m_currentTarget; }
            set
            {
                if (m_currentTarget != value)
                {
                    m_currentTarget = value;
                    syncFields(UnitFieldMask.MASK_CURRENTTARGET, value);
                }
            }
        }
        public float Gravity
        {
            get { return mPos.Gravity; }
            set
            {
                if (mPos.Gravity != value)
                {
                    mPos.Gravity = value;
                    syncFields(UnitFieldMask.MASK_GRAVITY, value);
                }
            }
        }
        public void ResetGravity()
        {
            if (AMotion && AMotion.IsNoneGravity)
            {
                this.Gravity = 0;
            }
            else
            {
                this.Gravity = Parent.Gravity;
            }
        }
        private void syncFields(UnitFieldMask mask, object value)
        {
            if (mSyncFields == null)
            {
                mSyncFields = ObjectPool.Alloc<UnitFieldChangedEvent>();
            }
            mSyncFields.mask |= mask;
            OnFieldChange?.Invoke(this, mask, value);
        }
        private void updateSyncFields()
        {
            if (mSyncFields != null)
            {
                FillSyncFields(mSyncFields);
                PostEvent(mSyncFields);
                mSyncFields = null;
            }
        }
        public void FillSyncFields(UnitFieldChangedEvent sync)
        {
            sync.object_id = this.ObjectID;
            sync.paused = this.IsPaused;
            sync.currentHP = __mCurrentHP.Value;
            sync.currentMP = __mCurrentMP.Value;
            sync.currentSP = __mCurrentSP.Value;
            sync.maxHP = __mCurrentHP.Max;
            sync.maxMP = __mCurrentMP.Max;
            sync.maxSP = __mCurrentSP.Max;
            sync.currentSpeed = __mCurrentMoveSpeedSEC;
            sync.currentFCR = __mFastCastRate;
            sync.currentFAR = __mFastActionRate;
            sync.currentFMR = __mFastMoveRate;
            sync.currentMoney = mMoney;
            sync.level = mLevel;
            sync.exp = mExp;
            sync.zoneShape = this.ZoneShape;
            sync.currentGravity = this.Gravity;
            sync.currentTarget = this.m_currentTarget;
            sync.pickRange = this.__mPickRange;
            sync.inventorySize = this.__InventorySize;
            sync.displayName = this.mDisplayName;
            sync.dockingOffset = this.DockingOffset;
            sync.dockingObj = this.DockingParentID;
            sync.bodyScale = this.BodyScale;
            sync.resScale = this.ResScale;
            sync.dummy_0 = m_dummy_0;
            sync.dummy_1 = m_dummy_1;
            sync.dummy_2 = m_dummy_2;
            sync.dummy_3 = m_dummy_3;
            sync.dummy_4 = m_dummy_4;
            sync.dummy_5 = m_dummy_5;
            sync.skin = this.m_skin;
            sync.avatar = this.m_avatar;
        }
        public override SyncObjectInfo GenSyncInfo(bool net)
        {
            return GenSyncUnitInfo(net);
        }
        public SyncUnitInfo GenSyncUnitInfo(bool net = false)
        {
            mSyncInfo.status = (byte)CurrentActionStatus;
            mSyncInfo.sub_status = CurrentActionSubstate;
            mSyncInfo.Force = mForce;
            mSyncInfo.pos.X = X;
            mSyncInfo.pos.Y = Y;
            mSyncInfo.pos.Z = Z;
            mSyncInfo.direction = this.Direction;
            mSyncInfo.body_direction = this.BodyDirection;
            mSyncInfo.speed_z = this.ZSpeedSEC;
            mSyncInfo.Alias = this.Alias;
            mSyncInfo.PlayerUUID = this.PlayerUUID;
            mSyncInfo.TemplateID = this.TemplateID;
            mSyncInfo.VisibleInfo = this.VisibleInfo;
            mSyncInfo.Name = this.DisplayName;
            mSyncInfo.UType = this.UType;
            mSyncInfo.Level = this.Level;
            if (net)
            {
                this.GetCurrentBuffStatus(mSyncInfo.CurrentBuffStatus);
                this.GetCurrentAuraStatus(mSyncInfo.CurrentAuraStatus);
                this.Cartridge.GetCurrentCardStatus(mSyncInfo.CurrentCardStatus);
                mSyncInfo.fields.mask = UnitFieldMask.MASK_ALL;
                FillSyncFields(mSyncInfo.fields);
            }
            return mSyncInfo;
        }
        /// <summary>
        /// 设置单位显示信息
        /// </summary>
        /// <param name="visible_data"></param>
        public void SetVisibleInfo(IUnitVisibleData visible_data, bool post = false)
        {
            mSyncInfo.VisibleInfo = visible_data;
            if (post)
            {
                PostVisibleChangeEvent();
            }
        }
        public void SyncVisibleInfo()
        {
            PostVisibleChangeEvent();
        }

        protected virtual void PostVisibleChangeEvent()
        {
            PostEvent(ObjectPool.Alloc<UnitVisibleChangedEvent>().Init(this.ID, mSyncInfo.VisibleInfo));
        }

        //         public void SetBodySize(float size)
        //         {
        //             if (size > 0)
        //             {
        //                 mBodySize = size;
        //             }
        //         }

        public void UseItem(int itemTemplateID)
        {
            ItemTemplate item = Cartridge.GetItem(itemTemplateID);
            if (item != null)
            {
                UseItem(item);
            }
        }
        public bool HasNearPlayer => this.SpaceUserTag.HasNearPlayer;


        //-----------------------------------------------------------------------------------------------------------

    }
}
