using DeepCore.AI.LLM;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Log;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;
using static DeepMetaGame.Data.Template.SkillTemplate;

namespace DeepCore.Game3D.Host.Instance
{
    abstract public partial class InstanceUnit : InstanceZoneEntity, IZoneUnit
    {
        UnitInfo IZoneUnit.Template => Info;
        UnitSkillAbility IZoneUnit.ASkill => this.ASkill;


        private readonly UnitInfo mInfo;
        public readonly UnitRecoverAbility ARecover;
        public readonly UnitResourceAbility AResource;
        public readonly UnitMotionAbility AMotion;
        public readonly UnitDropItemAbility ADropItem;
        public readonly UnitInventoryAbility AInventory;
        public readonly UnitSkillAbility ASkill;
        public readonly UnitGuardAbility AGuard;

        private readonly DropItemGenerator mDropItems;

        /// <summary>
        /// 统计信息
        /// </summary>
        public IUnitStatistic Statistic { get; private set; }

        public override int TemplateID
        {
            get => Info.ID;
        }

        public UnitCartridge Cartridge { get; private set; }

        private readonly string mPlayerUUID;
        private readonly string mName;
        private string mDisplayName;
        private byte mForce = 0;
        private int mLevel = 0;
        private float mBodySize = 0;
        private long mMoney = 0;
        private long mExp = 0;
        private Lazy<LLMAgent> mAiAgent = new Lazy<LLMAgent>(static () => new LLMAgent(LLMEnvironment.Instance.CreateProxy()));

        // 当前HP
        private readonly RangeValueL __mCurrentHP;
        // 当前MP
        private readonly RangeValueL __mCurrentMP;
        // 当前SP
        private readonly RangeValueL __mCurrentSP;
        // 移动速度
        private float __mCurrentMoveSpeedSEC = 0;
        private int __InventorySize;
        // 当前移动速度加成
        private float __mFastMoveRate = 1f;
        // 当前技能速度加成
        private float __mFastActionRate = 1f;
        // 当前快速施法速度
        private float __mFastCastRate = 1f;

        private float __mPickRange = 1f;
        private float __mBodyScale = 1f;
        private float __mResScale = 1f;

        // 当前单位是否地图阻挡
        private bool mIntersectMap = false;
        // 当前单位是否单位阻挡
        private bool mIntersectObj = false;

        // 单位最后死亡时间
        private double mDeadTime = 0;

        // 自动恢复计数器 
        protected TimeInterval mRecoveryTime = null;

        public InstanceUnit Summoner { get; private set; }

        public InstanceUnitFormula Formula { get; }
        public string SpawnPointName { get; set; }
        public LLMAgent AiAgent { get => mAiAgent.Value; }
        public virtual string Prompt { get => string.Empty; }
        public virtual string PromptName { get => string.Empty; }
        public UnitType UType { get; }
        public int UTypeAsInt { get { return (int)UType; } }
        public TAddUnit Add { get; }

        public InstanceUnit(InstanceZone zone, TAddUnit add, bool is_static_block = false)
            : base(zone, is_static_block)
        {
            this.Add = add;
            this.EnvironmentVarMap = new EnvironmentVarMap<InstanceUnit>(this);
            this.EnvironmentVarMap.OnEnvironmentVarChangeHandler += EnvironmentVarMap_OnEnvironmentVarChangeHandler;
            this.mForce = add.force;
            this.mLevel = add.level;
            this.mInfo = add.info;
            this.UType = add.info.UType;
            if (add.overrideType.HasValue && add.overrideType.Value != UnitType.TYPE_NA)
            {
                this.UType = add.overrideType.Value;
            }
            this.mName = string.IsNullOrEmpty(add.editor_name) ? string.Empty : add.editor_name;
            this.mPlayerUUID = string.IsNullOrEmpty(add.player_uuid) ? string.Empty : add.player_uuid;
            this.mDisplayName = string.IsNullOrEmpty(add.displayName) ? add.info.Name : add.displayName;
            this.Cartridge = zone.CreateCartridge(in add, this);
            {
                this.ARecover = mInfo.Abilities.GetComponentAs<UnitRecoverAbility>();
                this.AResource = mInfo.Abilities.GetComponentAs<UnitResourceAbility>();
                this.AMotion = mInfo.Abilities.GetComponentAs<UnitMotionAbility>();
                this.ADropItem = mInfo.Abilities.GetComponentAs<UnitDropItemAbility>();
                this.AInventory = mInfo.Abilities.GetComponentAs<UnitInventoryAbility>();
                this.ASkill = mInfo.Abilities.GetComponentAs<UnitSkillAbility>();
                this.AGuard = mInfo.Abilities.GetComponentAs<UnitGuardAbility>();
            }
            this.mBodySize = mInfo.BodySize;
            this.mIntersectMap = true;
            this.mIntersectObj = ResetIntersectObj();
            this.mSyncInfo = ObjectPool.Alloc<SyncUnitInfo>();
            this.mSyncInfo.Name = mName;
            this.mSyncInfo.UType = this.UType;
            this.mSyncInfo.Alias = add.alias;
            this.mSyncInfo.PlayerUUID = mPlayerUUID;
            this.mSyncInfo.TemplateID = mInfo.ID;
            this.mPos.Height = this.Info.BodyHeight;
            this.mPos.EnterWorld(zone.TerrainWorld);
            this.mPos.Transport(add.pos.Value);
            this.Summoner = add.summoner;
            this.__mCurrentHP = new RangeValueL(mInfo.HealthPoint, mInfo.HealthPoint);
            this.__mCurrentMP = new RangeValueL(mInfo.ManaPoint, mInfo.ManaPoint);
            this.__mCurrentSP = new RangeValueL(mInfo.StaminaPoint, mInfo.StaminaPoint);
            this.__mCurrentMoveSpeedSEC = AMotion ? AMotion.MoveSpeedSEC : 0;
            this.__mPickRange = mInfo.PickRange;
            this.mBuffs = new BuffMap(this);
            this.mBag = new InventoryBag(this);
            if (ARecover && ARecover.IdleRecover && ARecover.RecoveryIntervalMS > 0)
            {
                this.mRecoveryTime = new TimeInterval<int>(ARecover.RecoveryIntervalMS);
            }
            if (ADropItem)
            {
                this.mDropItems = new DropItemGenerator(ADropItem?.DropItemsSet);
            }
            if (AResource)
            {
                this.__mResScale = AResource.BodyScale;
                this.m_skin = AResource.SkinName;
                this.m_avatar = AResource.SkinAvatar;
            }
            this.FaceTo(add.direction);
            this.Formula = zone.CreateFormula(this);
            this.OnInitFormula(Formula, add);
            this.InitTimeLines();
            this.InitBagSlots();
            this.InitSkills();
            this.Statistic = this.CreateUnitStatistic();
            this.ResetGravity();
        }

        protected virtual void OnInitFormula(InstanceUnitFormula Formula, TAddUnit add)
        {
            this.Formula?.OnInit();
        }

        protected override void Disposing()
        {
            base.Disposing();
            this.Cartridge?.Dispose();
            this.cleanMultiTimeLines();
            this.cleanState();
            this.cleanPhysical();
            this.CleanBindEvents();
            this.cleanSkills();
            this.cleanBuffs();
            this.cleanAura();
            this.cleanItems();
            this.Formula.Dispose();
            this.mSyncInfo?.Dispose();
            this.mMultiTimeLineSync?.Dispose();
            this.mSyncFields?.Dispose();
            this.Cartridge = null;
        }

        protected override void onAdded()
        {
            base.onAdded();
            this.mSyncInfo.ObjectID = base.ID;
            this.mSyncInfo.IsTouchObj = this.mIntersectObj;
            this.mSyncInfo.IsTouchMap = this.mIntersectMap;
            this.mSyncInfo.StaticBlockable = this.StaticBlockable;
            //             if (mInfo.SpawnTimeMS > 0)
            //             {
            //                 this.SetInvincibleTimeMS(mInfo.SpawnTimeMS);
            //             }
            this.Cartridge.InitMeta();
            this.Formula?.OnLatedInit();
            //this.OnLateAdded();

        }
        protected override void onRemoved()
        {
            OnRemoved?.Invoke(this);
            ClearDockingParent();
            ClearSummons();
            ClearAttachments();
            CleanBindEvents();
            if (AResource?.RemovedEffect != null)
            {
                Parent.PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ID, this.Position, Direction, AResource.RemovedEffect));
            }
            base.onRemoved();
        }
        //         public void Trace(string text)
        //         {
        //             log.Info(text);
        //         }


        //--------------------------------------------------------------------------

        //--------------------------------------------------------------------------
        #region Properties------------------------------------------------------------------------------------------------
        public virtual string PlayerUUID { get { return mPlayerUUID; } }
        public UnitInfo Info { get { return mInfo; } }
        public override string Name { get { return mName; } }

        public string Alias
        {
            get { return mSyncInfo.Alias; }
            set { if (value != null) { mSyncInfo.Alias = value; } }
        }
        public virtual byte Force
        {
            get { return mForce; }
            protected set { mForce = value; }
        }
        public float BaseMoveSpeedSEC { get => __mCurrentMoveSpeedSEC; }
        public float MoveSpeedSEC { get { return __mCurrentMoveSpeedSEC * __mFastMoveRate; } }
        public float FastMoveRate { get { return __mFastMoveRate; } set { SetFastMoveRate(value); } }
        public float FastCastRate { get { return __mFastCastRate; } set { SetFastCastRate(value); } }
        public float FastActionRate { get { return __mFastActionRate; } set { SetFastActionRate(value); } }
        public override float BodyBlockSize { get { return (mBodySize) * __mBodyScale; } }
        public override float BodyHitSize { get { return (mBodySize + mInfo.BodySizeHitAppend) * __mBodyScale; } }
        public override float BodyHeight { get { return (mPos.Height) * __mBodyScale; } }
        public override float Weight { get { return mInfo.Weight; } }
        public override bool IntersectMap { get { return mIntersectMap; } }
        public override bool IntersectObj { get { return mIntersectObj && IsVisible; } }
        public override bool IsStaticBlock { get { return StaticBlockable && IsVisible && IsActive; } }
        public override bool Moveable { get { return AMotion && this.AMotion.IsMoveable; } }
        public override bool ClientVisible { get { return true; } }
        public bool IsDead { get { return __mCurrentHP.Value <= 0; } }
        public int DeadCount { get { return Statistic.DeadCount; } }
        public double LastDeadTimeMS { get { return mDeadTime; } }
        virtual public bool IsPlayer { get { return false; } }
        /// <summary>
        /// 是否中立
        /// </summary>
        virtual public bool IsNature { get { return false; } }
        /// <summary>
        /// 此单位是否能被攻击并且活着
        /// </summary>
        virtual public bool IsActive { get { return (base.Enable) && (__mCurrentHP.Value > 0) /*&& !IsInvincible && IsVisible*/; } }
        /// <summary>
        /// 此单位是否可以被打到，包括鞭尸
        /// </summary>
        virtual public bool IsAttackable { get { return (base.Enable) /*&& !IsInvincible && IsVisible*/; } }
        /// <summary>
        /// 此单位是否无技能
        /// </summary>
        public bool IsNoneSkill { get { return mSkillStatus.Count == 0; } }
        /// <summary>
        /// 技能可产生位移，或者多段由服务器决定
        /// </summary>
        virtual public bool IsSkillControllableByServer { get { return true; } }
        /// <summary>
        /// 单位是否可控
        /// </summary>
        virtual public bool IsControllable { get { return IsActive && !IsStun && CurrentActionStatus != UnitActionStatus.Damage; } }
        /// <summary>
        /// 扩展属性
        /// </summary>
        public IUnitProperties ExtProp { get { return mInfo.Properties; } }
        /// <summary>
        /// 用于显示的，单位横向数据
        /// </summary>
        public IUnitVisibleData VisibleInfo { get { return mSyncInfo.VisibleInfo; } }
        /// <summary>
        /// 扩展数据
        /// </summary>
        public virtual IUnitProperties Properties { get { return mInfo.Properties; } }
        public T PropertiesAs<T>() where T : class, IUnitProperties { return this.Properties as T; }

        /// <summary>
        /// 复活时间.
        /// </summary>
        /// <returns></returns>
        public virtual int RebirthTimeMS { get => mInfo.RebirthTimeMS; }
        /// <summary>
        /// 复活时间.
        /// </summary>
        /// <returns></returns>
        public virtual int DeadTimeMS { get => mInfo.DeadTimeMS; }
        public virtual int SpawnTimeMS { get => mInfo.SpawnTimeMS; }


        #endregion
        //-----------------------------------------------------------------------------------------------------//
        #region Update------------------------------------------------------------------------------------------------

        private bool is_init = false;
        private bool last_active = false;
        private bool first_active = true;
        public bool IsInitialized => is_init;
        public bool IsFirstActive => first_active;

        private CustomUnitEventTriggerCollection _bindEvent;
        private void onInit()
        {
            syncFields(UnitFieldMask.MASK_ALL, this);
            OnInitUnitEvents();
            if (SpawnTimeMS > 0)
            {
                StartSpawn(SpawnTimeMS);
                //changeState(new StateSpawn(this, mInfo.SpawnTimeMS));
            }
            else
            {
                //changeState(new StateIdle(this));        
                DoSomething();
                doActivated();
            }
            var attachment = this.Info.Abilities.GetComponentAs<UnitAttachmentAbility>();
            if (attachment != null)
            {
                InitAttachments(attachment);
            }
        }
        /// <summary>
        /// 第一帧触发
        /// </summary>
        protected virtual void OnInitUnitEvents()
        {
            if (mInfo.CustomEvents != null)
            {
                this._bindEvent = this.BindCustomUnitEvent(mInfo);
            }
            if (mInfo.Events != null)
            {
                foreach (int evt_id in mInfo.Events)
                {
                    this.BindUnitEvent(evt_id);
                }
            }
        }
        sealed override protected void onUpdate()
        {
            if (!is_init)
            {
                onInit();
                is_init = true;
            }
            if (last_active != IsActive)
            {
                last_active = !last_active;
                cb_ActiveChanged(last_active);
            }
            if (checkUpdate())
            {
                updateAI();
                OnUpdateAI?.Invoke(this);
                updateState();
                if (!Enable)
                {
                    return;
                }
                updatePhysical();
                onUpdateRecover();
                updateSkills();
                updateAuras();
                updateBuffs();
                updateItems();
                OnUpdate?.Invoke(this);
            }
            updateSyncFields();
            updateSyncSkillActives();
        }


        protected virtual bool checkUpdate()
        {
            return !IsPaused;
        }

        protected virtual void onUpdateRecover()
        {
            //自动恢复  
            if (ARecover && (mRecoveryTime != null) && (!IsDead) && (CurrentActionStatus != UnitActionStatus.Damage))
            {
                if (mRecoveryTime.Update(Parent.UpdateIntervalMS))
                {
                    AddHP(ARecover.HealthRecoveryPoint);
                    AddMP(ARecover.ManaRecoveryPoint);
                    AddSP(ARecover.StaminaRecoveryPoint);
                }
            }
        }

        protected virtual void onUpdateAI()
        {
        }
        private void updateAI()
        {
            onUpdateAI();
            _Components?.ForEach(this, static (st, c) =>
            {
                if (c is UnitComponent uc)
                {
                    uc.InternalUpdateAI();
                }
            });
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------//
        //         protected virtual void OnLateAdded()
        //         {
        //             this.LateAddedComponents();
        //         }
        protected virtual bool ResetIntersectObj()
        {
            if (this.Moveable)
            {
                return (!CFG.OBJECT_NONE_TOUCH) && (BodyBlockSize > 0) && (!Info.NoTouch);
            }
            else
            {
                return (BodyBlockSize > 0) && (!Info.NoTouch);
            }
        }

        virtual protected internal void RefreshData(UnitInfo temp)
        {
            if (Info.Abilities.TryGetComponentAs<UnitSkillAbility>(out var skills))
            {
                foreach (var launchSkill in skills.Skills)
                {
                    var skillState = GetSkillState(launchSkill.SkillID);
                    if (skillState != null)
                    {
                        skillState.LaunchSkill = launchSkill;
                    }
                }
            }
            if (this._bindEvent != null)
            {
                this._bindEvent.RefreshData(temp);
            }
            cb_OnRefreshData(temp);
        }
        public void RemoveFromParent()
        {
            Parent.RemoveObjectByID(ID);
        }
        //-----------------------------------------------------------------------------------------------------//
        protected virtual IUnitStatistic CreateUnitStatistic()
        {
            return new UnitStatistic(this);
        }

        //-----------------------------------------------------------------------------------------------------//

        internal protected void doAction(ObjectAction act)
        {
            this.onAction(act);
            OnHandleAction?.Invoke(this, act);
        }

        public void doRebirth(long max_hp = 0, long max_mp = 0)
        {
            //this.ResetAI();
            //next_state_queue.Clear();        
            this.mIntersectObj = ResetIntersectObj();
            if (max_hp <= 0) max_hp = this.MaxHP;
            if (max_mp <= 0) max_mp = this.MaxMP;
            this.__mCurrentHP.SetValue(max_hp);
            this.__mCurrentMP.SetValue(max_mp);
            syncFields(UnitFieldMask.MASK_HP, max_hp);
            syncFields(UnitFieldMask.MASK_MP, max_mp);
            Parent.cb_unitRebirthCallBack(this);
            PostEvent(ObjectPool.Alloc<UnitRebirthEvent>().Init(ID));
        }

        internal void doActivated()
        {
            this.mIntersectObj = ResetIntersectObj();
            Parent.cb_unitActivatedCallBack(this);
            first_active = false;
        }

        internal void doDead(InstanceUnit killer)
        {
            killer.LogKill(this);
            this.LogDead(killer);
            this.mIntersectObj = false;
            this.ClearBuffs(UnitStopBuffEvent.EndResult_ByDead);
            //this.ClearAura();
            this.ClearAllSkillCD();
            this.ResetAI();
        }
        //------------------------------------------------------------------------------------------------------//

        //-----------------------------------------------------------------------------------------------------------------
        internal void doLevelUp()
        {
            if (AResource.LevelUpEffect) { PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, AResource.LevelUpEffect)); }
            this.cb_OnUnitLevelUp();
            Parent.cb_OnUnitLevelUp(this);
        }

        /// <summary>
        /// 收到协议
        /// </summary>
        /// <param name="act"></param>
        virtual protected void onAction(ObjectAction act)
        {
        }

        /// <summary>
        /// 状态已切换时回调
        /// </summary>
        /// <param name="old_state"></param>
        /// <param name="state"></param>
        virtual protected void onStateChanged(State old_state, State state)
        {
        }

        // 被攻击时回调
        virtual protected void onDamaged(InstanceUnit attacker, in TAttackSource source, in TAttackResult result, long reduceHP)
        {
        }

        virtual protected void onDead(InstanceUnit killer)
        {
        }
        protected void cbDamaged(InstanceUnit attacker, TAttackSource source, TAttackResult result, long reduceHP)
        {
            Parent.cb_unitDamageCallBack(this, attacker, reduceHP, source, result);
        }
        #region HitAttack------------------------------------------------------------------------------------------------
        public bool DoHitAttack(InstanceUnit attacker, TAttackSource source)
        {
            return ProcessHitAttack(attacker, source);
        }
        /// <summary>
        /// 攻击完成计算后处理
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="source"></param>
        /// <param name="result"></param>
        /// <param name="reduceHP"></param>
        public virtual void DoHitDamage(InstanceUnit attacker, TAttackSource source, TAttackResult result, long reduceHP)
        {
            onDamaged(attacker, source, result, reduceHP);
            cbDamaged(attacker, source, result, reduceHP);
            if (!IsDead)
            {
                if (result.OutIsDamage)
                {
                    ChangeState(StateDamage.Alloc(this, source, result, attacker));
                }
            }
        }
        /// <summary>
        /// 向客户端发送受击事件
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="source"></param>
        /// <param name="result"></param>
        /// <param name="reduceHP"></param>
        public virtual void DoSendHitEvent(InstanceUnit attacker, TAttackSource source, TAttackResult result, long reduceHP)
        {
            var evt = ObjectPool.Alloc<UnitHitEvent>().Init(ID);
            evt.SetAttacker(attacker.ID, attacker);
            evt.hp = reduceHP;
            evt.CustomData = result.CustomData;//自定义扩展数据
            evt.isDead = IsDead;
            evt.isCritical = result.OutIsCritical;
            evt.IsHitted = result.OutHitted;
            evt.effect = result.OutHitEffect;
            evt.SourceAttack = source.Attack;
            evt.client_state = result.OutClientState;
            evt.ExtendsResult = result.OutExtendsResult;
            PostEvent(evt);
        }
        /// <summary>
        /// 返回实际扣血
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="source"></param>
        /// <param name="reduceHP">跳字扣血</param>
        /// <returns></returns>
        virtual protected long DoHitAttackHP(InstanceUnit attacker, TAttackSource source, ref TAttackResult result, ref long reduceHP)
        {
            long oldHP = this.CurrentHP;
            this.AddHP(-reduceHP);//老方法
            //DoHitAttack_AddHP(attacker, source, -reduceHP);//新方法：可以拦截转移伤害
            return oldHP - this.CurrentHP;//伤害：是-X 加血是+X
        }
        /// <summary>
        /// 单位被攻击核心函数，里面处理受击状态，死亡状态
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="source"></param>
        virtual protected bool ProcessHitAttack(InstanceUnit attacker, TAttackSource source)
        {
            bool deadBeforeDmg = IsDead;
            var result = new TAttackResult(source, this);
            result.Tag = source.Tag;
            try
            {
                long reduceHP = Parent.Formula.OnHit(attacker, source, ref result, this);
                if (this.IsNoDamage && reduceHP > 0)
                {
                    reduceHP = 0;
                }
                if (IsDead && !result.OutCanWhiplashDeadBody)
                {
                    //不让鞭尸//
                    return false;
                }
                if (reduceHP > 0 && CurrentState is StateDamage sd && sd.IsDamageProtect)
                {
                    //受击保护//
                    return false;
                }
                result.OutReducedHP = DoHitAttackHP(attacker, source, ref result, ref reduceHP);
                // 统计 //
                attacker.LogAttack(this, result.OutReducedHP);
                this.LogDamage(attacker, result.OutReducedHP);

                // Parent.Formula.UnitHitEventOverride(attacker, source, this, -source.OutReducedHP, oldHP);
                AttackProp attack = source.Attack;
                // Post Event //
                if (result.OutSendEvent)
                {
                    DoSendHitEvent(attacker, source, result, reduceHP);
                }
                DoHitDamage(attacker, source, result, result.OutReducedHP);
                if (result.OutReducedHP > 0)
                {
                    if (IsDead)
                    {
                        if (IsDead != deadBeforeDmg)
                        {
                            mDeadTime = Parent.PassTimeMS;
                            onDead(attacker);
                            Parent.cb_unitDeadCallBack(this, attacker);
                            if (IsDead)
                                PostEvent(ObjectPool.Alloc<UnitDeadEvent>().Init(ID, attacker.ID, result.OutIsCrush, DeadTimeMS));
                        }

                        // 被击碎，秒杀 //
                        if (result.OutIsCrush)
                        {
                            Parent.PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ID, this.Position, Direction, attack.CrushEffect));
                            ChangeState(StateDead.Alloc(this, attacker, true));
                        }
                        else
                        {
                            if (!Moveable)
                            {
                                ChangeState(StateDead.Alloc(this, attacker));
                            }
                            else if (result.OutHasKnockDown || result.OutHasFly)
                            {
                                if (!ChangeState(StateDamage.Alloc(this, in source, in result, attacker)))
                                {
                                    ChangeState(StateDead.Alloc(this, attacker));
                                }
                            }
                            else
                            {
                                ChangeState(StateDead.Alloc(this, attacker));
                            }
                        }
                    }
                    return true;
                }
            }
            finally
            {
                doHitAttackEndEffect(attacker, source, result);
            }
            return false;
        }

        //         protected virtual void DoHitAttack_AddHP(InstanceUnit attacker, AttackSource source, int reduceHP)
        //         {
        //             AddHP(reduceHP);
        //         }
        /// <summary>
        /// 死亡后自爆或者触发法术
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="source"></param>
        private void doHitAttackEndEffect(InstanceUnit attacker, TAttackSource source, in TAttackResult result)
        {
            if (source.Attack.Buff != null)
            {
                InstanceUnit.EquipSkill skillId = null;
                if (source.FromSkillState != null)
                    skillId = source.FromSkillState;
                else if (source.FromSpellUnit != null)
                    skillId = source.FromSpellUnit.FromSkillTemplateID;

                AddBuff(source.Attack.Buff, attacker, skillId);
            }

            if (source.Attack.Spell != null)
            {
                Parent.AttackLaunchSpell(attacker, this, source, source.Attack.Spell);
            }
        }

        //-----------------------------------------------------------------------------------------------------//
        /// <summary>
        /// 有伤害源的扣血
        /// </summary>
        /// <param name="hp"></param>
        /// <param name="attacker"></param>
        /// <param name="sendHit"></param>
        /// <param name="hitMessage"></param>
        public void ReduceHP(long hp, InstanceUnit attacker, bool sendHit = true, UnitHitEvent hitMessage = null, bool ignoreDead = false)
        {
            if (IsDead && !ignoreDead)
            {
                return;
            }
            if (hp == 0) return;
            if (attacker == null)
            {
                attacker = this;
            }

            this.AddHP(-hp, ignoreDead);
            attacker.LogAttack(this, hp);
            this.LogDamage(attacker, hp);
            if (sendHit)
            {
                if (hitMessage == null)
                {
                    hitMessage = ObjectPool.Alloc<UnitHitEvent>();
                }
                hitMessage.object_id = this.ID;
                hitMessage.SetAttacker(attacker.ID, attacker);
                hitMessage.hp = hp;
                hitMessage.isDead = IsDead;
                hitMessage.IsHitted = true;
                PostEvent(hitMessage);
            }

            if (IsDead)
            {
                mDeadTime = Parent.PassTimeMS;
                onDead(attacker);
                Parent.cb_unitDeadCallBack(this, attacker);
                if (IsDead)
                {
                    PostEvent(ObjectPool.Alloc<UnitDeadEvent>().Init(ID, attacker.ID, false, DeadTimeMS));
                    ChangeState(StateDead.Alloc(this, attacker, false));
                }
            }
        }

        public void Kill(InstanceUnit killer = null, bool sendHit = false, UnitHitEvent hitMessage = null)
        {
            ReduceHP(this.CurrentHP, killer, sendHit, hitMessage);
        }
        protected internal virtual void deadDropItems(byte force)
        {
            if (mDropItems == null) return;

            foreach (KeyValuePair<ItemTemplate, DropItem> e in mDropItems.Drop(Parent.Templates, Parent.RandomN))
            {
                var dst = mDropItems.GetDropPos(Parent.TerrainWorld, RandomN, this.Position, e.Value.DropPosRange);
                var evt = new TAddItem();
                {
                    evt.template = e.Key;
                    evt.name = e.Key.Name;
                    evt.pos = dst;
                    evt.direction = CMath.RandomRadians(RandomN);
                    evt.force = force;
                    evt.creater = this;
                }
                Parent.AddItem(evt);
                //Parent.AddItem(e.Key, e.Key.Name, in dst, CMath.RandomRadians(RandomN), force, this);
            }
        }

        #endregion
        protected virtual void OnResetAI()
        {
            this.DoSomething();
        }
        public virtual void ResetAI()
        {
            next_state_queue.Clear();
            OnResetAI();
            callback_onResetAI();
        }

        public virtual void Transport(in Geometry.Vector3 pos, bool sendNtf = true)
        {
            var oldpos = this.Position;
            SetPos(pos, false);
            if (sendNtf)
            {
                SendForceSync();
            }
            Parent.cb_unitTransportCallBack(this, oldpos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void killDropItem(InstanceUnit killed, UnitDropItemAbility drop)
        {
            Zone.Formula.OnKillDropItem(this, killed, drop);
        }


        protected bool tryPickObject(InstanceUnit unit)
        {
            this.CurrentTargetID = unit != null ? unit.ID : 0;
            if (!Parent.Formula.IsVisibleAOI(this, unit))
            {
                return false;
            }

            bool ret = true;
            if (OnTryPickUnit != null)
            {
                foreach (OnTryPickUnitHandler trypick in OnTryPickUnit.GetInvocationList())
                {
                    if (!trypick.Invoke(this, unit))
                    {
                        ret = false;
                    }
                }
            }
            return ret;
        }
        public void PickUnit(InstanceUnit pickable)
        {
            if (tryPickObject(pickable))
            {
                Parent.cb_unitPickUnitCallBack(this, pickable);
                OnPickUnit?.Invoke(this, pickable);
            }
        }

        public void DoFinishStory(string storyName)
        {
            Parent.cb_unitFinishStory(this, storyName);
        }

        public bool IsInPickRange(InstanceZoneObject cylinder)
        {
            return new Geometry.VoxelCylinder(this.Position, this.GetPickRange(), this.BodyHeight).Intersects(cylinder.VoxelBody);
        }

        public float GetPickRange()
        {
            return this.BodyBlockSize + this.BasePickRange;
        }

        //-----------------------------------------------------------------------------------------------------//
        #region Log
        protected virtual void LogDamage(InstanceUnit attacker, long reduceHP)
        {
            this.Statistic.onDamage(attacker, reduceHP);
        }
        protected virtual void LogAttack(InstanceUnit target, long reduceHP)
        {
            this.Statistic.onAttack(target, reduceHP);
        }
        protected virtual void LogDead(InstanceUnit attacker)
        {
            this.Statistic.onDead(attacker);
        }
        protected virtual void LogKill(InstanceUnit target)
        {
            this.Statistic.onKill(target);
        }
        protected virtual void LogUseItem(ItemTemplate item)
        {
            this.Statistic.onUseItem(item);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------//
    }
}