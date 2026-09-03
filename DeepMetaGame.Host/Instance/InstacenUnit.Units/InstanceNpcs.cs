using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;

namespace DeepCore.Game3D.Host.Instance
{

    //--------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 所有自动电脑AI
    /// </summary>
    public partial class InstanceGuard : InstanceUnit, IViewTriggerListener<InstanceUnit>
    {
        protected ViewTrigger<InstanceUnit> mViewTrigger;
        protected UnitHateComponent mHate;

        protected Geometry.Vector3 mStartPos;
        protected Geometry.Vector3? mOrginPosition;
        protected TimeInterval mCheckInGuardLimit;

        private StateAttackTo _mRunningPath;
        private StateFollowAndAttack _mTracingTarget;
        private StateFollowAndGuard _mGuardTarget;
        private StateBackToPosition _mBackToPosition;

        public InstanceUnit TracingTarget
        {
            get
            {
                if (_mTracingTarget != null) return _mTracingTarget.TargetUnit;
                return null;
            }
        }
        public HateSystem HateSystem
        {
            get => mHate?.HateSystem;
        }

        public InstanceGuard(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
            this.mCheckInGuardLimit = new TimeInterval(CFG.AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS);
            this.OnActivated += this.onUnitActivated;
        }

        protected override void onAdded()
        {
            this.mHate = Components.AddComponentAs<UnitHateComponent>();
            this.mOrginPosition = new Geometry.Vector3(X, Y, Z);
            this.mStartPos = new Geometry.Vector3(X, Y, Z);
            base.onAdded();
        }

        protected override void Disposing()
        {
            base.Disposing();
            this.mViewTrigger?.Dispose(); 
            this.mViewTrigger = null;

            this.StateRunningPath?.Dispose();
            this.StateRunningPath = null;

            this.StateTracingTarget?.Dispose();
            this.StateTracingTarget = null;

            this.StateGuardTarget?.Dispose();
            this.StateGuardTarget = null;

            this.StateBackToOrgin?.Dispose();
            this.StateBackToOrgin = null;
        }
        override protected void OnResetAI()
        {
            if (this.StateTracingTarget != null)
                this.StateTracingTarget.TargetUnit = null;
            HateSystem.Clear();
            DoSomething();
        }

        protected override void onDead(InstanceUnit killer)
        {
            base.onDead(killer);
            SetEnableView(false);
        }

        protected internal override void RefreshData(UnitInfo temp)
        {
            base.RefreshData(temp);
            initViewTrigger();
        }

        protected StateAttackTo StateRunningPath
        {
            get => _mRunningPath;
            set
            {
                _mRunningPath?.Dispose();
                _mRunningPath = value;
            }
        }
        protected StateFollowAndAttack StateTracingTarget
        {
            get => _mTracingTarget;
            set
            {
                _mTracingTarget?.Dispose();
                _mTracingTarget = value;
            }
        }
        protected StateFollowAndGuard StateGuardTarget
        {
            get => _mGuardTarget;
            set
            {
                _mGuardTarget?.Dispose();
                _mGuardTarget = value;
            }
        }
        protected StateBackToPosition StateBackToOrgin
        {
            get => _mBackToPosition;
            set
            {
                _mBackToPosition?.Dispose();
                _mBackToPosition = value;
            }
        }

        //--------------------------------------------------------------------------------------------------------
        #region View

        public virtual void SetEnableView(bool view)
        {
            if (mViewTrigger != null)
            {
                this.mViewTrigger.Enable = view && !IsNature && !IsNoneSkill;
            }
        }

        private void initViewTrigger()
        {
            if (mViewTrigger != null)
            {
                this.mViewTrigger.Dispose();
            }
            this.mViewTrigger = CreateViewTrigger(Parent);
            if (mViewTrigger != null)
            {
                this.mViewTrigger.SetListener(this);
            }
        }

        protected virtual ViewTrigger<InstanceUnit> CreateViewTrigger(InstanceZone zone)
        {
            if (AGuard && AGuard.GuardRange > 0)
            {
                return new ViewTriggerSphereBody<InstanceUnit>(zone, this.Position, AGuard.GuardRange/*, this.BodyHeight*/);
            }
            else
            {
                return new ViewTriggerBlind<InstanceUnit>(zone);
            }
        }

        void IViewTriggerListener<InstanceUnit>.OnObjectEnterView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            onAddHateLook(obj as InstanceUnit);
            onAddHateGroup(obj as InstanceUnit);
            DoSomething();
        }
        void IViewTriggerListener<InstanceUnit>.OnObjectLeaveView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {

        }
        bool IViewTriggerListener<InstanceUnit>.Select(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            if (obj == this)
            {
                return false;
            }
            else
            {
                if (!obj.IsNature && Parent.Formula.IsAttackable(this, obj, SkillTemplate.CastTarget.Enemy, AttackReason.Look, Info))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------
        #region Action

        protected override void DoDefaultBehavior()
        {
            if (CurrentState is StateSkill)
            {
                var target = TracingTarget;
                if (target != null)
                {
                    if (tryMoveScatterTarget(target)) { return; }
                }
            }
            guard();
        }
        public override bool StartAttackTo(InstanceFlag start)
        {
            this.StateRunningPath = new StateAttackToZoneWayPoint(this, start);
            return ChangeState(this.StateRunningPath);
        }
        public override bool StartFollowAndAttack(InstanceUnit target, AttackReason reason, SkillTemplate.CastTarget castTarget = SkillTemplate.CastTarget.Enemy, EquipSkill expectSkill = null)
        {
            if (IsNoneSkill) return false;
            if ((target != null))
            {
                if (CurrentState is StateSkill skill && skill.IsDone == false)//还在释放技能
                {
                    if (skill.TargetUnit == target)
                    {
                        return true;
                    }
                }
                if (Parent.Formula.IsAttackable(this, target, SkillTemplate.CastTarget.Enemy, reason, Info))
                {
                    this.SetEnableView(false);
                    HateSystem.Add(target, reason);
                    if (TracingTarget != target)
                    {
                        if (this.StateTracingTarget == null)
                        {
                            this.StateTracingTarget = new StateFollowAndAttack(this, target);
                        }
                        else
                        {
                            this.StateTracingTarget.TargetUnit = target;
                        }
                    }
                    this.StateTracingTarget.ExpectSkillState = expectSkill;
                    ChangeState(this.StateTracingTarget);
                    return true;
                }
                else
                {
                    HateSystem.Remove(target);
                }
            }
            return false;
        }
        public override bool StartGuardUnit(InstanceUnit vip)
        {
            if (AGuard)
            {
                if (this.StateGuardTarget == null || !this.StateGuardTarget.IsActive || this.StateGuardTarget.TargetUnit != vip)
                {
                    this.StateGuardTarget = new StateFollowAndGuard(this, vip, this.BodyBlockSize * 2 + vip.BodyBlockSize, AGuard.GuardRange);
                }
            }
            return ChangeState(this.StateGuardTarget);
        }
        public override bool StartGuardInPosition(Geometry.Vector3? pos)
        {
            mViewTrigger?.ClearViewd();
            if (pos.HasValue)
            {
                return ChangeState(StateGuardInPosition.Alloc(this, pos.Value));
            }
            return false;
        }
        public override bool StartBackToOrgin(Vector3? mOrginPosition)
        {
            mViewTrigger?.ClearViewd();
            HateSystem.Clear();
            if (this.StateTracingTarget != null)
                this.StateTracingTarget.TargetUnit = null;

            if (this.StateGuardTarget != null && this.StateGuardTarget.IsActive)
            {
                if (ChangeState(this.StateGuardTarget))
                {
                    OnBackToOrgin();
                    return true;
                }
            }
            if (mOrginPosition.HasValue)
            {
                if (this.StateBackToOrgin == null)
                {
                    this.StateBackToOrgin = new StateBackToPosition(this, mOrginPosition.Value);
                }
                else
                    this.StateBackToOrgin.Target = mOrginPosition.Value;
                if (ChangeState(this.StateBackToOrgin))
                {
                    OnBackToOrgin();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在一定范围内浪
        /// </summary>
        /// <param name="timeMS">浪多久</param>
        /// <param name="range">浪多远</param>
        public override bool StartIdleMove(Vector3 pos, float timeMS, float range)
        {
            return ChangeState(StateIdleMove.Alloc(this, pos, timeMS, range));
        }
        public virtual bool StartIdleMove(float timeMS, float range)
        {
            if (mOrginPosition.HasValue)
            {
                return StartIdleMove(mOrginPosition.Value, timeMS, range);
            }
            else
            {
                return StartIdleMove(this.Position, timeMS, range);
            }
        }
        public virtual void SetOrginPosition(Geometry.Vector3? pos)
        {
            this.mOrginPosition = pos;
        }
        public virtual Geometry.Vector3? GetOrginPosition()
        {
            return mOrginPosition;
        }

        /// <summary>
        /// 待机
        /// </summary>
        public virtual void guard()
        {
            if (StartFollowAndAttack(HateSystem.GetHated(), AttackReason.Tracing))
            {
                return;
            }
            if (this.StateTracingTarget != null)
                this.StateTracingTarget.TargetUnit = null;
            this.SetEnableView(true);
            if (this.StateGuardTarget != null && this.StateGuardTarget.IsActive)
            {
                ChangeState(this.StateGuardTarget);
                return;
            }
            if (this.StateRunningPath != null && !this.StateRunningPath.IsDone)
            {
                ChangeState(this.StateRunningPath);
                return;
            }
            if (HateSystem.Count == 0)
            {
                StartGuardInPosition(mOrginPosition);
                return;
            }
            base.StartIdle();
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------
        #region Update

        protected override void onUpdateRecover()
        {
            if (this.StateTracingTarget == null || !this.StateTracingTarget.IsActive)
            {
                base.onUpdateRecover();
            }
        }

        protected override void onUpdateAI()
        {
            this.updateBlockMap();
            base.onUpdateAI();
            updateTracingTarget();
            updateRunningPath();
            updateGuardTarget();
            updateBackToOrgin();
            updateView();
        }
        protected virtual void updateBlockMap()
        {
            if (IntersectMap && Parent.TouchMap(this, out var layer))
            {
                if (mOrginPosition.HasValue)
                    this.SetPos(mOrginPosition.Value);
                else
                    this.SetPos(mStartPos);
            }
        }
        protected virtual void updateTracingTarget()
        {
            if (this.StateTracingTarget != null && this.StateTracingTarget.IsActive)
            {
                if (this.StateTracingTarget.IsCanNotPass)
                {
                    StartBackToOrgin(mOrginPosition);
                }
                else if (Parent.Formula.IsAttackable(this, this.StateTracingTarget.TargetUnit, SkillTemplate.CastTarget.Enemy, AttackReason.Tracing, Info))
                {
                    //有攻击目标//
                    if ((CurrentState is StateSkill))
                    {
                        TryLaunchRandomSkillAndCancelCurrentSkill(TracingTarget, false);
                    }
                    else if (CurrentState is StateIdle)
                    {
                        tryMoveScatterTarget(TracingTarget);
                    }
                }
                else
                {
                    HateSystem.Remove(this.StateTracingTarget.TargetUnit);
                    if (this.StateTracingTarget == CurrentState)
                    {
                        DoSomething();
                    }

                    this.StateTracingTarget.TargetUnit = null;
                }
            }
        }
        protected virtual void updateRunningPath()
        {
            if (this.StateRunningPath != null)
            {
                if (CurrentState == this.StateRunningPath)
                {
                    if (this.StateRunningPath.IsDone)
                    {
                        this.StateRunningPath = null;
                    }
                    else if (this.StateRunningPath.Target != null)
                    {
                        mOrginPosition = this.Position;
                    }
                }
                else if (CurrentState is StateIdle)
                {
                    ChangeState(this.StateRunningPath);
                }
            }
        }
        protected virtual void updateGuardTarget()
        {
            //如果在寻路或保护，则实时更新OrginPosition//
            if (this.StateGuardTarget != null)
            {
                if (!this.StateGuardTarget.IsActive)
                {
                    this.StateGuardTarget = null;
                }
                else
                {
                    mOrginPosition = this.StateGuardTarget.TargetUnit.Position;
                }
            }
        }
        protected virtual void updateBackToOrgin()
        {
            if (this.StateBackToOrgin != null)
            {
                if (this.StateBackToOrgin.IsDone)
                {
                    this.StateBackToOrgin = null;
                }
            }
            if (mOrginPosition.HasValue)
            {
                if (AGuard && AGuard.GuardRangeLimitAppend > 0)
                {
                    if (mCheckInGuardLimit.Update(Parent.UpdateIntervalMS))
                    {
                        var limit = AGuard.GuardRange + AGuard.GuardRangeLimitAppend;
                        //if (!CMath.includeRoundPoint(X, Y, Info.GuardRangeLimit, mOrginPosition.X, mOrginPosition.Y))
                        if (!Collider.Intersects(mOrginPosition.Value, this.Position, limit))
                        {
                            StartBackToOrgin(mOrginPosition);
                            return;
                        }
                        else if (this.StateTracingTarget != null && this.StateTracingTarget.IsActive)
                        {
                            var r2 = limit + this.StateTracingTarget.TargetUnit.BodyHitSize;
                            if (!(Collider.Intersects(this.Position, this.StateTracingTarget.TargetUnit.Position, r2)))
                            {
                                StartBackToOrgin(mOrginPosition);
                                return;
                            }
                        }
//                         else
//                         {
//                             StartBackToOrgin(mOrginPosition);
//                             return;
//                         }
                    }
                }
            }
        }
        protected virtual void updateView()
        {
            if (mViewTrigger != null)
            {
                mViewTrigger.LookUpdate(this.Position);
            }
        }
        //         protected virtual void updateHate()
        //         {
        //             HateSystem.Update();
        //         }
        /// <summary>
        /// 攻击间歇，尝试换个位置，避免怪物堆在一个点
        /// </summary>
        protected virtual bool tryMoveScatterTarget(InstanceUnit target)
        {
            //只有单位为非碰撞时，才有这个需求//
            if (StateMove.TryMoveScatterTarget(this, target, out var state))
            {
                this.ChangeState(state);
            }
            return false;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------
        #region InternalEvents

        protected virtual void onUnitActivated(InstanceUnit unit)
        {
            initViewTrigger();
        }

        protected override void onStateChanged(State old_state, State state)
        {
            if (state is StateIdle)
            {
                StartFollowAndAttack(HateSystem.GetHated(), AttackReason.Tracing);
            }
            if (old_state != null)
            {
                if (old_state == this.StateBackToOrgin)
                {
                    this.StateBackToOrgin = null;
                }
                //if (old_state == mTracingTarget)
                //{
                //    if (mTracingTarget.IsNoWay)
                //    {
                //        mHateSystem?.Remove(mTracingTarget.TargetUnit);
                //        mTracingTarget = null;
                //    }
                //}
            }
        }

        protected override void onMoveBlockWithObject(IEntityObject obj)
        {
            base.onMoveBlockWithObject(obj);
            if (IsNoneSkill)
            {
                return;
            }
            if (obj is InstanceUnit)
            {
                StartFollowAndAttack(obj as InstanceUnit, AttackReason.MoveBlocked);
            }
        }
        protected override bool onBlockOtherGetaway(InstanceUnit obj)
        {
            var ret = base.onBlockOtherGetaway(obj);
            if (obj.Force == this.Force)
            {
                if (AMotion && AMotion.IsMoveImpact) { return false; }
                //给自己友军让路//
                var getaway = StateMoveAway.Alloc(this, obj,
                    (this.BodyBlockSize + obj.BodyBlockSize) * Zone.CFG.AI_MOVE_AI_BYPASS_SCALE,
                    this.RandomN.RandomRadians(CMath.RADIANS_180));
                ChangeOrQueueState(getaway);
                return true;
            }
            return ret;
        }

        protected override void onDamaged(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            if (IsNoneSkill)
            {
                return;
            }
            if (!Zone.Formula.IsAttackable(this, attacker, SkillTemplate.CastTarget.Enemy, AttackReason.Damaged))
            {
                return;
            }
            onAddHateDamage(attacker, in attack, in result, reduceHP);
            onAddHateGroup(attacker);
            DoSomething();
        }


        protected virtual void onAddHateDamage(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            // 被攻击转火
            HateSystem.OnHitted(attacker, in attack, in result, reduceHP);
            //             bool attackTo = false;
            //             if (mOnEnemyAdded != null)
            //             {
            //                 mOnEnemyAdded.Invoke(this, attacker, AttackReason.Damaged, ref attackTo);
            //             }
            //             if (attackTo)
            //             {
            //                 followAndAttack(mHateSystem.GetHated(), AttackReason.Damaged);
            //             }
            //             return attackTo;
        }
        protected virtual void onAddHateLook(InstanceUnit target)
        {
            HateSystem.Add(target, AttackReason.Look, CFG.AI_HATE_SYSTEM_ENTER_VIEW_HATE_VALUE);
            /*       bool attack = false;
                   if (mOnEnemyAdded != null)
                   {
                       mOnEnemyAdded.Invoke(this, target, AttackReason.Look, ref attack);
                   }
                   if (attack)
                   {
                       mHateSystem.Add(target, AttackReason.Look, CFG.AI_HATE_SYSTEM_ENTER_VIEW_HATE_VALUE);
                       followAndAttack(mHateSystem.GetHated(), AttackReason.Look);
                   }
                   return attack;*/
        }

        protected virtual void onAddHateGroup(InstanceUnit target)
        {
            if (AGuard && AGuard.GuardRangeGroup > 0)
            {
                using (var for1 = ObjectPool.AllocForEach2<InstanceZoneEntity, InstanceGuard, InstanceUnit>(this, target))
                {
                    Parent.ForEachNearObjects(X, Y, AGuard.GuardRangeGroup, for1, static (st) =>
                    {
                        if (st.Iterator is InstanceGuard o)
                        {
                            var _this = st.Arg1;
                            var _target = st.Arg2;
                            if ((o != _this))
                            {
                                //精确过滤.
                                if (Collider.Intersects(_this.mPos.Position, o.Position, _this.AGuard.GuardRangeGroup))
                                {
                                    if (o.AGuard && o.Force == _this.Force && _this.Parent.Formula.IsAttackable(o, _target, SkillTemplate.CastTarget.Enemy, AttackReason.Look, _this.Info))
                                    {
                                        var limit = o.AGuard.GuardRange + o.AGuard.GuardRangeLimitAppend;
                                        if (Collider.Intersects(o.Position, _target.Position, limit))
                                        {
                                            o.onAddHateLook(_target);
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }
        }



        protected virtual void OnBackToOrgin()
        {

        }


        #endregion
        //--------------------------------------------------------------------------------------------------------
        #region Events

        protected override void ClearEvents()
        {
            base.ClearEvents();
            //mOnEnemyAdded = null;
        }

        //         public delegate void EnemyAdded(InstanceUnit unit, InstanceUnit enemy, AttackReason reason, ref bool attack);
        // 
        //         private EnemyAdded mOnEnemyAdded;
        // 
        //         [EventTriggerDescAttribute("Add敌人")]
        //         public event EnemyAdded OnEnemyAdded { add { mOnEnemyAdded += value; } remove { mOnEnemyAdded -= value; } }

        #endregion
        //--------------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------------------

        //         public void SetOrginPosition(Geometry.Vector3? pos)
        //         {
        //             this.setOrginPosition(pos);
        //         }
        //         public void AttackTo(ZoneWayPoint target)
        //         {
        //             this.attackTo(target as ZoneWayPoint);
        //         }
        //         public void GuardUnit(InstanceUnit vip)
        //         {
        //             this.guardUnit(vip as InstanceUnit);
        //         }
        //         public void FollowAndAttack(InstanceUnit target, AttackReason reason)
        //         {
        //             this.followAndAttack(target as InstanceUnit, reason);
        //         }
    }

    //--------------------------------------------------------------------------------------------------------

    public class InstanceSummon : InstanceGuard
    {
        public InstanceUnit SummonerUnit { get => this.Summoner; }

        public InstanceSummon(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
        }

        override protected void onUpdateAI()
        {
            base.onUpdateAI();
            if ((PassTimeMS >= Info.LifeTimeMS) || (SummonerUnit != null && SummonerUnit.IsDead))
            {
                Kill();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------


}


namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("设置NPC警戒位置", "[游戏]/单位/NPC单位-AI")]
    public class NpcSetGuardPosition : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("警戒位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置NPC:{0}警戒在{1};", Unit, Pos);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceGuard;
            var pos = Pos.GetValueAs(api, args);
            if (unit != null && pos != null)
            {
                unit.SetOrginPosition(pos);
            }
            return unit;
        }
    }
    [Desc("清除NPC警戒位置", "[游戏]/单位/NPC单位-AI")]
    public class NpcClearGuardPosition : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("清除NPC:{0}警戒位置;", Unit);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceGuard;
            if (unit != null)
            {
                unit.SetOrginPosition(null);
            }
            return unit;
        }
    }


}
