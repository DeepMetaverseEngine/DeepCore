using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class NpcAIComponent : UnitComponent
    {
        protected UnitGuardViewComponent mViewComponent;
        protected UnitHateComponent mHateComponent;
        protected Geometry.Vector3 mStartPos;
        private StateAttackTo _mRunningPath;
        private StateFollowAndAttack _mTracingTarget;
        private StateFollowAndGuard _mGuardTarget;
        private StateBackToPosition _mBackToPosition;

        public StateAttackTo StateRunningPath
        {
            get => _mRunningPath;
            protected set
            {
                _mRunningPath?.Dispose();
                _mRunningPath = value;
            }
        }
        public StateFollowAndAttack StateTracingTarget
        {
            get => _mTracingTarget;
            protected set
            {
                _mTracingTarget?.Dispose();
                _mTracingTarget = value;
            }
        }
        public StateFollowAndGuard StateGuardTarget
        {
            get => _mGuardTarget;
            protected set
            {
                _mGuardTarget?.Dispose();
                _mGuardTarget = value;
            }
        }
        public StateBackToPosition StateBackToOrgin
        {
            get => _mBackToPosition;
            protected set
            {
                _mBackToPosition?.Dispose();
                _mBackToPosition = value;
            }
        }
        public UnitGuardViewComponent ViewComponent => mViewComponent;
        public UnitHateComponent HateComponent => mHateComponent;
        public InstanceUnit TracingTarget => _mTracingTarget?.TargetUnit;
        public HateSystem HateSystem => mHateComponent?.HateSystem;
        public bool HasNearPlayer => Owner.SpaceUserTag.HasNearPlayer;
        public bool HasTracingTarget => (this.StateTracingTarget == null || !this.StateTracingTarget.IsActive);

        protected override void OnAdded()
        {
            base.OnAdded();
            this.mHateComponent = Owner.Components.GetOrAddComponentAs<UnitHateComponent>();
            this.mViewComponent = Owner.Components.GetOrAddComponentAs<UnitGuardViewComponent>();
            Owner.OnActivated += this.Owner_onUnitActivated;
            Owner.OnHandleResetAI += Owner_OnHandleResetAI;
            Owner.OnDead += Owner_OnDead;
            Owner.OnDamage += Owner_OnDamage; // 在HateComponent之后，才能有效确认伤害目标
            Owner.OnRefreshData += Owner_OnRefreshData;
            Owner.OnDoSomething += Owner_OnDoSomething;
            Owner.OnStateChanged += Owner_OnStateChanged;
            Owner.OnMoveBlockWithObject += Owner_OnMoveBlockWithObject;
            Owner.OnBlockOtherGetaway += Owner_OnBlockOtherGetaway;
            ViewComponent.OnObjectEnterView += ViewComponent_OnObjectEnterView;
            ViewComponent.OnInOriginRange += ViewComponent_OnInOriginRange;
            ViewComponent.NeedBackToOrigin += ViewComponent_NeedBackToOrigin;
        }
        protected override void OnRemoved()
        {
            base.OnRemoved();
            this.mViewComponent.Enable = false;
            this.mHateComponent.HateSystem.Clear();
            Owner.OnActivated -= this.Owner_onUnitActivated;
            Owner.OnHandleResetAI -= Owner_OnHandleResetAI;
            Owner.OnDead -= Owner_OnDead;
            Owner.OnDamage -= Owner_OnDamage;
            Owner.OnRefreshData -= Owner_OnRefreshData;
            Owner.OnDoSomething -= Owner_OnDoSomething;
            Owner.OnStateChanged -= Owner_OnStateChanged;
            Owner.OnMoveBlockWithObject -= Owner_OnMoveBlockWithObject;
            Owner.OnBlockOtherGetaway -= Owner_OnBlockOtherGetaway;
            ViewComponent.OnObjectEnterView -= ViewComponent_OnObjectEnterView;
            ViewComponent.OnInOriginRange -= ViewComponent_OnInOriginRange;
            ViewComponent.NeedBackToOrigin -= ViewComponent_NeedBackToOrigin;
        }
        protected override void OnDispose(InstanceZoneObject owner)
        {
            this.OnBackToOrigin = null;
            base.OnDispose(owner);
            this.StateRunningPath = null;
            this.StateTracingTarget = null;
            this.StateGuardTarget = null;
            this.StateBackToOrgin = null;
        }

        //----------------------------------------------------------------------------------------------------------------------------

        protected virtual void Owner_onUnitActivated(InstanceUnit unit)
        {
            this.mViewComponent.ResetViewTrigger();
            this.mViewComponent.OriginPosition = Owner.Position;
            this.mStartPos = Owner.Position;
        }
        protected virtual void Owner_OnHandleResetAI(InstanceUnit sender)
        {
            if (this.StateTracingTarget != null)
            {
                this.StateTracingTarget.TargetUnit = null;
            }
            HateSystem.Clear();
            if (Owner.CurrentState is StateSkill)
            {
                var target = TracingTarget;
                if (target != null)
                {
                    if (StartMoveScatterTarget(target)) { return; }
                }
            }
            Owner.DoSomething();
        }
        protected virtual void Owner_OnDead(InstanceUnit sender, InstanceUnit attacker)
        {
            mViewComponent.Enable = false;
        }
        protected virtual void Owner_OnRefreshData(InstanceUnit sender, UnitInfo data)
        {
            mViewComponent.ResetViewTrigger();
        }

        protected virtual bool Owner_OnDoSomething(InstanceUnit sender, bool handed)
        {
            if (Owner.CurrentState is StateSkill)
            {
                var target = TracingTarget;
                if (target != null)
                {
                    if (StartMoveScatterTarget(target))
                    {
                        return true;
                    }
                }
            }
            return StartGuard();
        }
        protected virtual void Owner_OnStateChanged(InstanceUnit sender, InstanceUnit.State old_state, InstanceUnit.State new_state)
        {
            if (new_state is StateIdle)
            {
                StartFollowAndAttack(HateSystem.GetHated(), AttackReason.Tracing);
            }
            if (old_state != null)
            {
                if (old_state == this.StateBackToOrgin)
                {
                    this.StateBackToOrgin = null;
                }
            }
        }

        protected virtual bool Owner_OnBlockOtherGetaway(InstanceUnit sender, InstanceUnit other)
        {
            if (other.Force == Owner.Force)
            {
                if (Owner.AMotion && Owner.AMotion.IsMoveImpact) { return false; }
                //给自己友军让路//
                var getaway = StateMoveAway.Alloc(Owner, other, 
                    (Owner.BodyBlockSize + other.BodyBlockSize) * Zone.CFG.AI_MOVE_AI_BYPASS_SCALE,
                    Owner.RandomN.RandomRadians(CMath.RADIANS_180));
                Owner.ChangeOrQueueState(getaway);
                return true;
            }
            return false;
        }

        protected virtual void Owner_OnMoveBlockWithObject(InstanceUnit sender, IEntityObject other)
        {
            if (Owner.IsNoneSkill)
            {
                return;
            }
            if (other is InstanceUnit otherUnit)
            {
                StartFollowAndAttack(otherUnit, AttackReason.MoveBlocked);
            }
        }
        protected virtual void Owner_OnDamage(InstanceUnit sender, InstanceUnit attacker, long hp, in TAttackSource source, in TAttackResult result)
        {
            if (Owner.IsNoneSkill)
            {
                return;
            }
            if (!Zone.Formula.IsAttackable(Owner, attacker, SkillTemplate.CastTarget.Enemy, AttackReason.Look))
            {
                return;
            }
            Owner.DoSomething();
        }

        //--------------------------------------------------------------------------------------------------------

        protected virtual void ViewComponent_OnObjectEnterView(UnitGuardViewComponent sender, InstanceUnit obj)
        {
            HateComponent.AddHateLook(obj);
            Owner.DoSomething();
        }
        private void ViewComponent_NeedBackToOrigin(UnitGuardViewComponent sender, Geometry.Vector3 origin, float limit)
        {
            StartBackToOrgin(mViewComponent.OriginPosition);
        }
        private void ViewComponent_OnInOriginRange(UnitGuardViewComponent sender, Geometry.Vector3 origin, float limit)
        {
            if (this.StateTracingTarget != null)
            {
                if (this.StateTracingTarget.IsActive)
                {
                    var r2 = limit + this.StateTracingTarget.TargetUnit.BodyHitSize;
                    if (!(Collider.Intersects(Owner.Position, this.StateTracingTarget.TargetUnit.Position, r2)))
                    {
                        StartBackToOrgin(mViewComponent.OriginPosition);
                        return;
                    }
                }
                else
                {
                    //FIX 单位在原点来回抽的问题 modify by UHA ((ノ｀Д)ノ)
                    //StartBackToOrgin(mViewComponent.OriginPosition);
                    return;
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 待机
        /// </summary>
        public virtual bool StartGuard()
        {
            if (StartFollowAndAttack(HateSystem.GetHated(), AttackReason.Tracing))
            {
                return true;
            }
            this.ViewComponent.Enable = (true);
            if (this.StateTracingTarget != null)
            {
                this.StateTracingTarget.TargetUnit = null;
            }
            if (this.StateGuardTarget != null && this.StateGuardTarget.IsActive)
            {
                return Owner.ChangeState(this.StateGuardTarget);
            }
            if (this.StateRunningPath != null && !this.StateRunningPath.IsDone)
            {
                return Owner.ChangeState(this.StateRunningPath);
            }
            if (HateSystem.Count == 0)
            {
                if (mViewComponent.OriginPosition.HasValue)
                {
                    var pos = mViewComponent.OriginPosition.Value;
                    return StartGuardInPosition(pos);
                }
            }
            return Owner.StartIdle();
        }

        /// <summary>
        /// 寻路攻击目标
        /// </summary>
        /// <param name="src"></param>
        /// <param name="reason"></param>
        public virtual bool StartFollowAndAttack(InstanceUnit src, AttackReason reason, EquipSkill expectSkill = null)
        {
            if (Owner.IsNoneSkill) return false;
            if ((src != null))
            {
                if (Owner.CurrentState is StateSkill skill && skill.IsDone == false)//还在释放技能
                {
                    if (skill.TargetUnit == src)
                    {
                        return true;
                    }
                }
                if (expectSkill != null && Zone.Formula.IsAttackableBySkill(Owner, src, expectSkill, reason))
                {
                    ViewComponent.Enable = (false);
                    HateSystem.Add(src, reason);
                    if (TracingTarget != src)
                    {
                        if (this.StateTracingTarget == null)
                        {
                            this.StateTracingTarget = new StateFollowAndAttack(Owner, src);
                        }
                        else
                        {
                            this.StateTracingTarget.TargetUnit = src;
                        }
                    }
                    this.StateTracingTarget.ExpectSkillState = expectSkill;
                    return Owner.ChangeState(this.StateTracingTarget);
                }
                else if (Zone.Formula.IsAttackable(Owner, src, SkillTemplate.CastTarget.Enemy, reason, Owner.Info))
                {
                    ViewComponent.Enable = (false);
                    HateSystem.Add(src, reason);
                    if (TracingTarget != src)
                    {
                        if (this.StateTracingTarget == null)
                        {
                            this.StateTracingTarget = new StateFollowAndAttack(Owner, src);
                        }
                        else
                        {
                            this.StateTracingTarget.TargetUnit = src;
                        }
                    }
                    this.StateTracingTarget.ExpectSkillState = expectSkill;
                    return Owner.ChangeState(this.StateTracingTarget);
                }
                else
                {
                    HateSystem.Remove(src);
                }
            }
            return false;
        }

        /// <summary>
        /// 寻路并一路警戒
        /// </summary>
        /// <param name="path"></param>
        public virtual bool StartAttackTo(InstanceFlag path)
        {
            this.StateRunningPath = new StateAttackToZoneWayPoint(Owner, path);
            return Owner.ChangeState(this.StateRunningPath);
        }

        /// <summary>
        /// 保护单位
        /// </summary>
        /// <param name="vip"></param>
        public virtual bool StartGuardUnit(InstanceUnit vip)
        {
            if (Owner.AGuard)
            {
                if (this.StateGuardTarget == null || !this.StateGuardTarget.IsActive || this.StateGuardTarget.TargetUnit != vip)
                {
                    this.StateGuardTarget = new StateFollowAndGuard(Owner, vip, Owner.BodyBlockSize * 2 + vip.BodyBlockSize, Owner.AGuard.GuardRange);
                }
            }
            return Owner.ChangeState(this.StateGuardTarget);
        }

        public virtual bool StartGuardInPosition(Geometry.Vector3? pos)
        {
            ViewComponent.ClearViewd();
            return Owner.ChangeState(StateGuardInPosition.Alloc(Owner, pos.Value));
        }

        /// <summary>
        /// 立刻开始返回原点
        /// </summary>
        public virtual bool StartBackToOrgin(Geometry.Vector3? mOrginPosition)
        {
            ViewComponent.OriginPosition = mOrginPosition;
            ViewComponent.ClearViewd();
            HateSystem.Clear();
            if (this.StateTracingTarget != null)
                this.StateTracingTarget.TargetUnit = null;

            if (this.StateGuardTarget != null && this.StateGuardTarget.IsActive)
            {
                if (Owner.ChangeState(this.StateGuardTarget))
                {
                    OnBackToOrigin?.Invoke(this);
                    return true;
                }
            }
            if (mViewComponent.OriginPosition.HasValue)
            {
                if (this.StateBackToOrgin == null)
                    this.StateBackToOrgin = new StateBackToPosition(Owner, mViewComponent.OriginPosition.Value);
                else
                    this.StateBackToOrgin.Target = mViewComponent.OriginPosition.Value;
                if (Owner.ChangeState(this.StateBackToOrgin))
                {
                    OnBackToOrigin?.Invoke(this);
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
        public virtual bool StartIdleMove(float timeMS, float range)
        {
            if (mViewComponent.OriginPosition.HasValue)
            {
                return Owner.ChangeState(StateIdleMove.Alloc(Owner, mViewComponent.OriginPosition.Value, timeMS, range));
            }
            else
            {
                return Owner.ChangeState(StateIdleMove.Alloc(Owner, Owner.Position, timeMS, range));
            }
        }
        public virtual bool StartMoveScatterTarget(InstanceUnit target)
        {
            //只有单位为非碰撞时，才有这个需求//
            if (StateMove.TryMoveScatterTarget(Owner, target, out var state))
            {
                return Owner.ChangeState(state);
            }
            return false;
        }
        //--------------------------------------------------------------------------------------------------------


        protected override void OnUpdate()
        {
            if (Active)
            {
                this.UpdateBlockMap();
                base.OnUpdate();
                UpdateTracingTarget();
                UpdateRunningPath();
                UpdateGuardTarget();
                UpdateBackToOrgin();
            }
            else
            {
                base.OnUpdate();
            }
        }

        protected virtual void UpdateBlockMap()
        {
            if (Owner.IntersectMap && Zone.TouchMap(Owner, out var layer))
            {
                if (mViewComponent.OriginPosition.HasValue)
                    Owner.Transport(mViewComponent.OriginPosition.Value, false);
                else
                    Owner.Transport(mStartPos, false);
            }
        }
        protected virtual void UpdateTracingTarget()
        {
            if (this.StateTracingTarget != null && this.StateTracingTarget.IsActive)
            {
                if (this.StateTracingTarget.IsCanNotPass)
                {
                    StartBackToOrgin(mViewComponent.OriginPosition);
                }
                else if (Zone.Formula.IsAttackable(Owner, this.StateTracingTarget.TargetUnit, SkillTemplate.CastTarget.Enemy, AttackReason.Tracing, Owner.Info))
                {
                    //有攻击目标//
                    if (Owner.CurrentState is StateSkill)
                    {
                        Owner.TryLaunchRandomSkillAndCancelCurrentSkill(TracingTarget, false);
                    }
                    else if (Owner.CurrentState is StateIdle)
                    {
                        StartMoveScatterTarget(TracingTarget);
                    }
                }
                else
                {
                    HateSystem.Remove(this.StateTracingTarget.TargetUnit);
                    if (this.StateTracingTarget == Owner.CurrentState)
                    {
                        Owner.DoSomething();
                    }
                    this.StateTracingTarget.TargetUnit = null;
                }
            }
        }
        protected virtual void UpdateRunningPath()
        {
            if (this.StateRunningPath != null)
            {
                if (Owner.CurrentState == this.StateRunningPath)
                {
                    if (this.StateRunningPath.IsDone)
                    {
                        this.StateRunningPath = null;
                    }
                    else if (this.StateRunningPath.Target != null)
                    {
                        mViewComponent.OriginPosition = Owner.Position;
                    }
                }
                else if (Owner.CurrentState is StateIdle)
                {
                    Owner.ChangeState(this.StateRunningPath);
                }
            }
        }
        protected virtual void UpdateGuardTarget()
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
                    mViewComponent.OriginPosition = this.StateGuardTarget.TargetUnit.Position;
                }
            }
        }
        protected virtual void UpdateBackToOrgin()
        {
            if (this.StateBackToOrgin != null)
            {
                if (this.StateBackToOrgin.IsDone)
                {
                    this.StateBackToOrgin = null;
                }
            }
        }


        //--------------------------------------------------------------------------------------------------------

        public delegate void BackToOriginHandler(NpcAIComponent sender);
        public event BackToOriginHandler OnBackToOrigin;

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("开关 NPC AI", "[游戏]/单位/[组件]/NPC_AI")]
        public class NpcAIComponentAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("开关")]
            public AbstractValue<bool> On = new ZoneBooleanValue.VALUE(true);
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    if (On.GetValueAs(api, args))
                    {
                        unit.Components.GetOrAddComponentAs<NpcAIComponent>();
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<NpcAIComponent>();
                    }
                }
                return null;
            }
        }
        [Desc("NPC AI 是否开启", "[游戏]/单位/[组件]/NPC_AI")]
        public class NpcAIIsON : ZoneBooleanValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    if (unit.Components.TryGetComponentAs<NpcAIComponent>(out var ai, true))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------

    }
}
