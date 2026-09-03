using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;


namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Follow
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 移动状态
        /// </summary>
        abstract public class StateFollow : State
        {
            //----------------------------------------------------------------------------------------------------------
            // 被追目标
            // 如果被单位碰撞，则向左或右挪动//
            protected MoveAI moveAI;
            // 检测切换走停的间隔时间 //
            protected readonly State<MoveState> state = new State<MoveState>(MoveState.Move, static (a, b) => a == b);
            protected bool begin_in_min_range;
            protected bool isCanNotPass;
            // 检测切换走停的间隔时间 //
            protected TimeInterval start_move_hold_time;
            //无路可走
            protected TimeExpire mNoWayHoldTime;
            protected OnNoWayAction mNoWayAction;
            //----------------------------------------------------------------------------------------------------------
            public StateFollow() { }
            protected StateFollow(InstanceUnit unit) : base(unit) { }
            protected virtual StateFollow Init(InstanceUnit unit, InstanceZoneObject target, bool beginInMinRange = true)
            {
                this.begin_in_min_range = beginInMinRange;
                this.mNoWayHoldTime = unit.AllocTimeExpire(0);
                this.moveAI = unit.CreateMoveAI();
                return this;
            }
            protected override void Disposing()
            {
                this.moveAI?.Dispose();
                this.moveAI = null;
                this.state.Update(MoveState.Move);
                this.begin_in_min_range = default;
                this.isCanNotPass = default;
                this.start_move_hold_time?.Dispose();
                this.start_move_hold_time = default;
                this.mNoWayHoldTime?.Dispose();
                this.mNoWayHoldTime = default;
                this.mNoWayAction = null;
            }
            //----------------------------------------------------------------------------------------------------------

            public enum MoveState
            {
                Hold,
                Move,
            }

            public delegate bool OnNoWayAction(InstanceUnit unit, IPositionObject target);

            public void SetNoWayAction(OnNoWayAction act)
            {
                mNoWayAction = act;
            }
            public bool IsActive { get => !IsNoWay && IsTargetActive; }
            public ITerrainWayPoint NextPath { get { return moveAI?.NextPath; } }
            public bool IsNoWay { get { if (moveAI != null) { return moveAI.IsNoWay; } return false; } }
            public bool IsCanNotPass { get => isCanNotPass; }
            public MoveState FollowState { get { return state.Value; } }
            public abstract bool IsTargetActive { get; }
            public abstract IPositionObject Target { get; }
            /// <summary>
            /// Hold到Move之间的检测间隔
            /// </summary>
            public float StartMoveHoldTimeMS
            {
                get { return (start_move_hold_time != null) ? start_move_hold_time.IntervalTimeMS : 0; }
                set
                {
                    if (value > 0 && value != StartMoveHoldTimeMS)
                    {
                        start_move_hold_time = new TimeInterval(value);
                    }
                }
            }



            public override bool OnBlock(State new_state)
            {
                isCanNotPass = false;
                return true;
            }

            protected override void OnStart()
            {
                isCanNotPass = false;
                if (IsTargetActive)
                {
                    if (cheekBeginInRange())
                    {
                        state.Update(MoveState.Hold);
                        unit.SetActionStatus(UnitActionStatus.Idle);
                    }
                    else
                    {
                        state.Update(MoveState.Move);
                        unit.SetActionStatus(unit.GetStartMoveStatus());
                        moveAI.FindPath(Target);
                    }
                    if (unit.IsInTheAir)
                    {
                        unit.SetActionStatus(UnitActionStatus.Jump);
                    }
                }
                else
                {
                    state.Update(MoveState.Hold);
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
            }

            protected override void OnUpdate()
            {
                isCanNotPass = false;
                if (!IsActive)
                {
                    if (IsTargetActive && onChangedToIdle(Target))
                    {
                    }
                    else
                    {
                        unit.DoSomething();
                    }
                    return;
                }
                switch (FollowState)
                {
                    case MoveState.Move:

                        if (moveAI.Target == null)
                        {
                            moveAI.FindPath(Target);
                        }
                        var result = moveAI.Update();
                        if (CheckTargetInMinRange() == true)
                        {
                            // 进入最小追踪距离 //
                            changeToHold();
                        }
                        else if (moveAI.IsNoWay)
                        {
                            if (CheckTargetInMaxRange() == true)
                            {
                                changeToHold();
                            }
                            else
                            {
                                //onChangedToIdle(target);
                                if (mNoWayHoldTime.IsEnd)
                                {
                                    mNoWayHoldTime.Reset(unit.CFG.AI_MOVE_NOWAY_HOLD_TIME_MS);
                                }
                                else if (mNoWayHoldTime.Update(zone.UpdateIntervalMS))
                                {
                                    if (mNoWayAction != null)
                                    {
                                        mNoWayAction.Invoke(unit, Target);
                                    }
                                    else
                                    {
                                        unit.DoSomething();
                                    }
                                }
                            }
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                        {
                            isCanNotPass = true;
                        }
                        break;
                    case MoveState.Hold:
                        // 超过最大追踪范围 //
                        if ((start_move_hold_time == null || start_move_hold_time.Update(zone.UpdateIntervalMS)))
                        {
                            if ((CheckTargetInMinRange()))
                            {
                                onUpdateFollowed(Target);
                            }
                            else
                            {
                                changeToMove();
                            }
                        }
                        else
                        {
                            onUpdateFollowed(Target);
                        }
                        break;
                }
                if (!IsNoWay)
                {
                    mNoWayHoldTime.Reset(0);
                }

            }

            protected override void OnStop()
            {
                isCanNotPass = false;
            }

            protected bool cheekBeginInRange()
            {
                if (begin_in_min_range)
                {
                    return CheckTargetInMinRange();
                }
                else
                {
                    return CheckTargetInMaxRange();
                }
            }
            protected void changeToMove()
            {
                state.Update(MoveState.Move);
                unit.SetActionStatus(unit.GetStartMoveStatus());
                moveAI.FindPath(Target);
                onInRangeChanged();
                onChangedToMove(Target);
            }
            protected void changeToHold()
            {
                state.Update(MoveState.Hold);
                unit.SetActionStatus(UnitActionStatus.Idle);
                moveAI.Pause();
                onInRangeChanged();
                onChangedToHold(Target);
            }

            /// <summary>
            /// 检查目标是否在跟随范围内。
            /// 检测为True，停止移动。
            /// </summary>
            protected abstract bool CheckTargetInMinRange();
            /// <summary>
            /// 检查目标是否在跟随范围内。
            /// 检测为False，开始移动。
            /// </summary>
            protected abstract bool CheckTargetInMaxRange();

            /// <summary>
            /// 行为改变
            /// </summary>
            protected virtual void onInRangeChanged()
            {

            }
            /// <summary>
            /// 目标已经被追踪到
            /// </summary>
            /// <param name="target"></param>
            protected virtual void onUpdateFollowed(IPositionObject target)
            {

            }
            /// <summary>
            /// 开始移动
            /// </summary>
            /// <param name="target"></param>
            protected virtual void onChangedToMove(IPositionObject target)
            {

            }
            /// <summary>
            /// changeToHold
            /// </summary>
            /// <param name="target"></param>
            protected virtual void onChangedToHold(IPositionObject target)
            {

            }
            protected virtual bool onChangedToIdle(IPositionObject target)
            {
                unit.DoSomething();
                return true;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public class StateFollowObject : StateFollow
        {
            private float distance;
            private InstanceZoneObject targetObj;
            public StateFollowObject() { }
            public StateFollowObject(InstanceUnit unit, InstanceZoneObject target) : base(unit)
            {
                this.Init(unit, target);
            }
            public static StateFollowObject Alloc(InstanceUnit unit, InstanceZoneObject target)
            {
                return unit.AllocState<StateFollowObject>().Init(unit, target);
            }
            protected virtual StateFollowObject Init(InstanceUnit unit, InstanceZoneObject target)
            {
                base.Init(unit, target);
                this.targetObj = target;
                this.distance = Target.BodySize + unit.BodyBlockSize * 2;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.targetObj = default;
                this.distance = default;
            }
            public override IPositionObject Target => targetObj;
            public override bool IsTargetActive { get { return targetObj.Enable; } }
            protected override bool CheckTargetInMaxRange()
            {
                //return CMath.includeRoundPoint(unit.X, unit.Y, distance * 2, Target.X, Target.Y);
                return Collider.Intersects(unit.Position, Target.Position, distance * 2);
            }
            protected override bool CheckTargetInMinRange()
            {
                //return CMath.includeRoundPoint(unit.X, unit.Y, distance, Target.X, Target.Y);
                return Collider.Intersects(unit.Position, Target.Position, distance);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public class StateFollowAndGuard : StateFollow
        {
            private float distance_min;
            private float distance_max;
            private InstanceUnit targetUnit;

            protected StateFollowAndGuard() { }
            public StateFollowAndGuard(InstanceUnit unit, InstanceUnit target, float minDistance, float maxDistance) : base(unit)
            {
                this.Init(unit, target, minDistance, maxDistance);
            }
            public static StateFollowAndGuard Alloc(InstanceUnit unit, InstanceUnit target, float minDistance, float maxDistance)
            {
                return unit.AllocState<StateFollowAndGuard>(static s => new StateFollowAndGuard()).Init(unit, target, minDistance, maxDistance);
            }
            protected virtual StateFollowAndGuard Init(InstanceUnit unit, InstanceUnit target, float minDistance, float maxDistance)
            {
                base.Init(unit, target);
                this.targetUnit = target;
                this.distance_min = Math.Max(minDistance, target.BodyBlockSize + unit.BodyBlockSize * 2);
                this.distance_max = Math.Max(maxDistance, distance_min);
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.distance_min = default;
                this.distance_max = default;
                this.targetUnit = default;
            }
            public override IPositionObject Target => targetUnit;
            public InstanceUnit TargetUnit { get { return targetUnit; } }
            public override bool IsTargetActive { get { return targetUnit.Enable; } }
            public bool IsOutRange { get => !CheckTargetInMaxRange(); }
            protected override bool CheckTargetInMinRange()
            {
                // return CMath.includeRoundPoint(unit.X, unit.Y, distance_min, Target.X, Target.Y);
                return Collider.Intersects(unit.Position, Target.Position, distance_min);
            }
            protected override bool CheckTargetInMaxRange()
            {
                // return CMath.includeRoundPoint(unit.X, unit.Y, distance_max, Target.X, Target.Y);
                return Collider.Intersects(unit.Position, Target.Position, distance_max);
            }
            //----------------------------------------------------------------------------------------------------------------------------------------


        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// xxxxx
        /// </summary>
        public class StateFollowAndAttack : StateFollow
        {
            protected InstanceUnit targetUnit;
            protected SkillTemplate.CastTarget expectTarget;
            protected TimeInterval checkAdjustLaunchSkillTime;
            protected EquipSkill launchSkill;
            private bool can_auto_focus_near_target;
            private bool can_random_skill = true;
            private bool can_attack = true;
            private float min_distance;
            private float max_distance;
            public bool CheckAutoLaunch = false;
            public StateFollowAndAttack() { }
            public StateFollowAndAttack(InstanceUnit unit, InstanceUnit target, SkillTemplate.CastTarget expectTarget = SkillTemplate.CastTarget.Enemy, bool autoFocusNearTarget = false) : base(unit)
            {
                this.Init(unit, target, expectTarget, autoFocusNearTarget);
            }
            public static StateFollowAndAttack Alloc(InstanceUnit unit, InstanceUnit target, SkillTemplate.CastTarget expectTarget = SkillTemplate.CastTarget.Enemy, bool autoFocusNearTarget = false)
            {
                return unit.AllocState<StateFollowAndAttack>().Init(unit, target, expectTarget, autoFocusNearTarget);
            }
            protected virtual StateFollowAndAttack Init(
                InstanceUnit unit,
                InstanceUnit target,
                SkillTemplate.CastTarget expectTarget = SkillTemplate.CastTarget.Enemy,
                bool autoFocusNearTarget = false)
            {
                base.Init(unit, target, false);
                unit.CurrentTargetID = target.ID;
                this.checkAdjustLaunchSkillTime = unit.AllocTimeInterval(zone.CFG.AI_FOLLOW_AND_ATTACK_HOLD_TIME_MS);
                this.targetUnit = target;
                this.expectTarget = expectTarget;
                this.can_auto_focus_near_target = autoFocusNearTarget;
                this.StartMoveHoldTimeMS = zone.CFG.AI_FOLLOW_AND_ATTACK_HOLD_TIME_MS;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.targetUnit = default;
                this.expectTarget = default;
                this.checkAdjustLaunchSkillTime?.Dispose();
                this.checkAdjustLaunchSkillTime = default;
                this.launchSkill = default;
                this.can_auto_focus_near_target = default;
                this.can_random_skill = true;
                this.can_attack = true;
                this.min_distance = default;
                this.max_distance = default;
                this.CheckAutoLaunch = false;

            }

            public InstanceUnit TargetUnit
            {
                get => targetUnit;
                set
                {
                    this.targetUnit = value;
                    this.isCanNotPass = false;
                }
            }

            /// <summary>
            /// 是否追踪
            /// </summary>
            /// <returns></returns>
            override public bool IsTargetActive => can_attack && targetUnit != null && targetUnit.IsActive;
            sealed public override IPositionObject Target => TargetUnit;
            public SkillTemplate.CastTarget ExpectTarget => expectTarget;

            public EquipSkill ExpectSkillState
            {
                get => launchSkill;
                set
                {
                    launchSkill = value;
                    if (value != null)
                    {
                        expectTarget = value.Data.ExpectTarget;
                    }
                    else if (unit.DefaultSkill != null)
                    {
                        expectTarget = unit.DefaultSkill.ExpectTarget;
                    }
                }
            }

            public SkillTemplate ExpectSkill => launchSkill == null ? unit.DefaultSkill : launchSkill.Data;

            public bool IsLaunchRandomSkill
            {
                get => can_random_skill;
                set => can_random_skill = value;
            }
            public bool IsAutoFocusNearTarget
            {
                get => can_auto_focus_near_target;
                set => can_auto_focus_near_target = value;
            }

            protected override void OnStart()
            {
                this.launchSkill = unit.GetRandomLaunchableExpectSkill(expectTarget, CheckAutoLaunch);
                if (IsTargetActive)
                {
                    resetMaxMinRange();
                }
                base.OnStart();
            }
            protected override void onUpdateFollowed(IPositionObject target)
            {
                this.can_attack = zone.Formula.IsAttackable(unit, targetUnit, expectTarget, AttackReason.Tracing, this.ExpectSkill);
                if (can_attack)
                {
                    if (base.IsNoWay)
                    {
                        can_attack = false;
                    }
                    unit.FaceTo(target.X, target.Y);
                    if (TryLaunchSkill() == null)
                    {
                        SkillTemplate expect_skill = ExpectSkill;
                        if (expect_skill != null && expect_skill.AttackKeepRange > 0)
                        {
                            if (checkAdjustLaunchSkillTime.IntervalTimeMS == 0 || checkAdjustLaunchSkillTime.Update(zone.UpdateIntervalMS))
                            {
                                if (CUtils.RandomPercent(zone.RandomN, zone.CFG.AI_FOLLOW_AND_ATTACK_ADJUST_ESCAPE_PCT))
                                {
                                    if (unit.StartAdjustLaunchSkill(expect_skill, targetUnit))
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                        unit.DoSomething();
                    }
                }
            }

            protected override void OnUpdate()
            {
                if (IsActive && targetUnit)
                {
                    resetMaxMinRange();
                }
                base.OnUpdate();
            }

            protected void resetMaxMinRange()
            {
                var expect_skill = ExpectSkillState;
                min_distance = unit.BodyBlockSize + targetUnit.BodyBlockSize;
                max_distance = Math.Max(min_distance, unit.BodyBlockSize + targetUnit.BodyHitSize);
                bool needCheck = true;
                if (expect_skill == null)
                {
                    var new_skill = unit.GetRandomLaunchableExpectSkill(targetUnit, expectTarget, AttackReason.Tracing, true, CheckAutoLaunch);
                    if (new_skill != null)
                    {
                        ExpectSkillState = new_skill;
                        expect_skill = new_skill;
                        needCheck = false;
                    }
                }

                if (expect_skill != null)
                {
                    if (can_random_skill)
                    {
                        if (needCheck == true && !expect_skill.CheckTargetRange(targetUnit))
                        {
                            var new_skill = unit.GetRandomLaunchableExpectSkill(targetUnit, expectTarget, AttackReason.Tracing, true, CheckAutoLaunch);
                            if (new_skill != null)
                            {
                                ExpectSkillState = new_skill;
                                expect_skill = new_skill;
                            }
                        }
                    }
                    unit.GetFollowRange(targetUnit, expect_skill.Data, out min_distance, out max_distance);
                }
                else
                {
                    if (unit.DefaultSkill != null)
                    {
                        unit.GetFollowRange(targetUnit, unit.DefaultSkill, out min_distance, out max_distance);
                    }
                }
                //max_distance = zone.RandomN.NextFloat(min_distance, max_distance);
            }

            protected override bool CheckTargetInMaxRange()
            {
                // return CMath.includeRoundPoint(unit.X, unit.Y, max_distance, targetUnit.X, targetUnit.Y);
                return Collider.Intersects(unit.Position, targetUnit.Position, max_distance);
            }
            protected override bool CheckTargetInMinRange()
            {
                // return CMath.includeRoundPoint(unit.X, unit.Y, min_distance, targetUnit.X, targetUnit.Y);
                return Collider.Intersects(unit.Position, targetUnit.Position, min_distance);
            }
            protected virtual bool OnEndAdjustKeepRange(StateMove m)
            {
                unit.FaceTo(Target.X, Target.Y);
                unit.ChangeState(this);
                return true;
            }
            protected virtual SkillTemplate TryLaunchSkill()
            {
                if (launchSkill == null)
                {
                    this.launchSkill = unit.GetRandomLaunchableExpectSkill(expectTarget, CheckAutoLaunch);
                }
                Geometry.Vector3? targetPos = null;
                if (launchSkill != null)
                {
                    if (unit.LaunchSkill(launchSkill, new InstanceUnit.TLaunchSkillParam(targetUnit.ID)
                    {
                        AutoFocusNearTarget = can_auto_focus_near_target,
                        SpellTargetPos = targetPos,
                    }) is EquipSkill sk)
                    {
                        return sk.Data;
                    }
                }
                if (can_random_skill)
                {
                    var ret = unit.LaunchRandomSkill(expectTarget, new InstanceUnit.TLaunchSkillParam(targetUnit.ID)
                    {
                        AutoFocusNearTarget = can_auto_focus_near_target,
                        SpellTargetPos = targetPos,
                    }, CheckAutoLaunch);
                    if (ret != null)
                    {
                        return ret.Data;
                    }
                }
                return null;
            }

            /// <summary>
            /// 寻路攻击目标
            /// </summary>
            /// <param name="src"></param>
            /// <param name="reason"></param>
            public static StateFollowAndAttack FollowAndAttack(InstanceUnit owner, InstanceUnit target, SkillTemplate.CastTarget cast, AttackReason reason, EquipSkill expectSkill = null)
            {
                if (owner.IsNoneSkill) return null;
                if ((target != null))
                {
                    if (owner.CurrentState is StateSkill skill && skill.IsDone == false)//还在释放技能
                    {
                        if (skill.TargetUnit == target)
                        {
                            return null;
                        }
                    }
                    if (owner.Parent.Formula.IsAttackable(owner, target, cast, reason, owner.Info))
                    {
                        var state = StateFollowAndAttack.Alloc(owner, target);
                        state.ExpectSkillState = expectSkill;
                        return state;
                    }
                }
                return null;
            }

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public class StateFollowAndPickObject : StateFollow
        {
            private InstanceZoneObject targetObject;
            private float pickTimeMS;
            private StatePickObject.OnPickDone doneEvent;
            private object status;
            public StateFollowAndPickObject() { }
            public StateFollowAndPickObject(InstanceUnit unit, InstanceZoneObject target, float timeMS, StatePickObject.OnPickDone done, object status = null) : base(unit)
            {
                this.Init(unit, target, timeMS, done, status);
            }
            public static StateFollowAndPickObject Alloc(InstanceUnit unit, InstanceZoneObject target, float timeMS, StatePickObject.OnPickDone done, object status = null)
            {
                return unit.AllocState<StateFollowAndPickObject>().Init(unit, target, timeMS, done, status);
            }
            protected virtual StateFollowAndPickObject Init(InstanceUnit unit, InstanceZoneObject target, float timeMS, StatePickObject.OnPickDone done, object status = null)
            {
                base.Init(unit, target, false);
                this.targetObject = target;
                this.pickTimeMS = timeMS;
                this.doneEvent = done;
                this.status = status;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.targetObject = default;
                this.pickTimeMS = default;
                this.doneEvent = default;
                this.status = default;
            }
            public override IPositionObject Target => targetObject;
            public override bool IsTargetActive
            {
                get { return targetObject.Enable; }
            }
            protected override bool CheckTargetInMinRange()
            {
                //return CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize, targetObject.X, targetObject.Y);
                return Collider.Intersects(unit.Position, Target.Position, (unit.BodyBlockSize + Target.BodySize) * 0.85f);
            }
            protected override bool CheckTargetInMaxRange()
            {
                //return CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize + targetObject.BodyBlockSize, targetObject.X, targetObject.Y);
                return Collider.Intersects(unit.Position, Target.Position, (unit.BodyBlockSize + targetObject.BodyBlockSize));
            }
            protected override void onUpdateFollowed(IPositionObject target)
            {
                unit.StartPickProgressObject(targetObject, pickTimeMS, doneEvent, status);
            }


            //----------------------------------------------------------------------------------------------------------------------------------------


        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public class StateFollowAndPickItem : StateFollow
        {
            private InstanceItem targetObject;
            public StateFollowAndPickItem() { }
            public StateFollowAndPickItem(InstanceUnit unit, InstanceItem target) : base(unit)
            {
                this.Init(unit, target);
            }
            public static StateFollowAndPickItem Alloc(InstanceUnit unit, InstanceItem target)
            {
                return unit.AllocState<StateFollowAndPickItem>().Init(unit, target);
            }
            protected virtual StateFollowAndPickItem Init(InstanceUnit unit, InstanceItem target)
            {
                base.Init(unit, target, false);
                this.targetObject = target;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                targetObject = null;
            }
            public override IPositionObject Target => targetObject;
            public InstanceItem TargetItem
            {
                get => targetObject;
                //set { targetUnit = value; }
            }
            public override bool IsTargetActive
            {
                get { return targetObject.Enable; }
            }
            protected override bool CheckTargetInMinRange()
            {
                // return CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize, targetObject.X, targetObject.Y);
                return Collider.Intersects(unit.Position, targetObject.Position, (unit.BodyBlockSize + targetObject.BodySize) * 0.75f);
            }
            protected override bool CheckTargetInMaxRange()
            {
                //   return CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize + targetObject.BodyBlockSize, targetObject.X, targetObject.Y);
                return Collider.Intersects(unit.Position, Target.Position, (unit.BodyBlockSize + targetObject.BodySize) * 0.95f);
            }
            protected override void onUpdateFollowed(IPositionObject target)
            {
                if (!targetObject.PickItem(unit))
                {
                    unit.DoSomething();
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------


        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion Follow
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 移动状态
        /// </summary>
        public class StateMove : State
        {
            private DeepCore.Geometry.Vector3 target;
            private int minStepCount;
            private bool moveEnd = false;
            private MoveBlockResult mLastMoveResult;
            public int MinStepCheckCount { get; set; }
            public bool StopOnTouchMap { get; set; }

            public static StateMove Alloc(InstanceUnit unit, DeepCore.Geometry.Vector3 pos)
            {
                var state = unit.AllocState<StateMove>();
                state.StopOnTouchMap = false;
                state.MinStepCheckCount = 10;
                state.target = pos;
                return state;
            }
            protected override void Disposing()
            {
                this.target = default;
                this.minStepCount = default;
                this.moveEnd = false;
                this.mLastMoveResult = default;
                this.MinStepCheckCount = default;
                this.StopOnTouchMap = default;
            }



            public DeepCore.Geometry.Vector3 Target { get { return target; } }
            public bool IsMoveEnd { get { return moveEnd; } }
            public MoveBlockResult LastMoveResult { get { return mLastMoveResult; } }


            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnUpdate()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                unit.FaceTo(target.X, target.Y);
                mLastMoveResult = unit.MoveBlockTo(target.X, target.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS);
                if ((mLastMoveResult.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0 && StopOnTouchMap)
                {
                    moveEnd = true;
                    unit.DoSomething();

                }
                else if ((mLastMoveResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    moveEnd = true;
                    unit.DoSomething();
                }
                else if ((mLastMoveResult.result & MoveResult.MOVE_RESULT_MIN_STEP) != 0)
                {
                    moveEnd = true;
                    minStepCount++;
                    if (minStepCount > MinStepCheckCount)
                    {
                        unit.DoSomething();
                    }
                }
                else
                {
                    minStepCount = 0;
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnStop()
            {

            }

            public static bool TryMoveScatterTarget(InstanceUnit owner, InstanceUnit target, out StateMove stateMove)
            {
                //只有单位为非碰撞时，才有这个需求//
                if (!owner.IntersectObj)
                {
                    if (CUtils.RandomPercent(owner.RandomN, owner.CFG.AI_NPC_ATTACK_IDLE_SCATTER_PCT))
                    {
                        InstanceUnit block = owner.Parent.GetNearBlockObject(owner);
                        if (block != null)
                        {
                            var pos = owner.Position;
                            float degree = MathVector.getDegree(pos.X, pos.Y, target.X, target.Y);
                            float distance = owner.BodyBlockSize + block.BodyBlockSize;
                            var turnL = new Vector2(pos.X, pos.Y);
                            var turnR = new Vector2(pos.X, pos.Y);
                            VectorHelper.MovePolar(ref turnL, degree + CMath.PI_DIV_2, distance);
                            VectorHelper.MovePolar(ref turnR, degree - CMath.PI_DIV_2, distance);
                            float dl = VectorHelper.GetDistanceSquare(turnL.X, turnL.Y, target.X, target.Y);
                            float dr = VectorHelper.GetDistanceSquare(turnR.X, turnR.Y, target.X, target.Y);
                            if (dl < dr)
                            {
                                stateMove = StateMove.Alloc(owner, new Geometry.Vector3(turnL.X, turnL.Y, owner.Z));
                                return true;
                            }
                            else
                            {
                                stateMove = StateMove.Alloc(owner, new Geometry.Vector3(turnR.X, turnR.Y, owner.Z));
                                return true;
                            }

                        }
                    }
                }
                stateMove = null;
                return false;
            }

            //----------------------------------------------------------------------------------------------------------------------------------------

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public class StateMoveAI : State
        {
            private Geometry.Vector3 target;
            private bool isEnd = false;
            private MoveAI moveAI;
            private ITerrainLayer targetLayer;
            public static StateMoveAI Alloc(InstanceUnit unit, Geometry.Vector3 tgt, bool beginFindPath = true)
            {
                var state = unit.AllocState<StateMoveAI>();
                state.target = tgt;
                state.moveAI = unit.CreateMoveAI();
                state.moveAI.IsFirstFindPath = beginFindPath;
                state.targetLayer = unit.Zone.Terrain3D.GetVoxelLayerByPos(tgt);
                return state;
            }
            protected override void Disposing()
            {
                this.target = default;
                this.isEnd = false;
                this.moveAI?.Dispose();
                this.moveAI = default;
            }



            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                this.moveAI.FindPath(target);
            }
            override protected void OnUpdate()
            {
                if (!isEnd)
                {
                    unit.FaceTo(target.X, target.Y);
                    MoveBlockResult result = moveAI.Update();
                    if ((result.result & MoveResult.MOVE_RESULT_NO_WAY) != 0)
                    {
                        isEnd = true;
                        unit.DoSomething();
                    }
                    else if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                    {
                        float r = Math.Max(zone.MinStep, unit.BodyBlockSize);
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, r, targetX, targetY))
                        if (moveAI.IsInRange(target, r))
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                        else if (targetLayer == unit.CurrentLayer)
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                    }
                    else
                    {
                        float r = Math.Max(zone.MinStep, unit.BodyBlockSize);
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, r, targetX, targetY))
                        if (moveAI.IsInRange(target, r))
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                    }
                }
                else
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {

            }

            //----------------------------------------------------------------------------------------------------------------------------------------


        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 浪
        /// </summary>
        public class StateIdleMove : State
        {
            private TimeExpire mExpire;
            private MoveAI mMoveAI;
            private Geometry.Vector3 mOrginPos;
            private float mRange;
            private MoveBlockResult mLastResult;


            public static StateIdleMove Alloc(InstanceUnit unit, Geometry.Vector3 orginPos, float timeMS, float range)
            {
                var alloc = unit.AllocState<StateIdleMove>();
                alloc.mExpire = unit.ObjectPool.AllocAutoRelease<TimeExpire>().Init(timeMS);
                alloc.mRange = Math.Abs(range);
                alloc.mOrginPos = orginPos;
                alloc.mLastResult = default;
                alloc.mMoveAI = unit.CreateMoveAI();
                return alloc;
            }
            protected override void Disposing()
            {
                this.mExpire?.Dispose();
                this.mExpire = null;
                this.mMoveAI?.Dispose();
                this.mMoveAI = null;
                this.mRange = 0;
                this.mOrginPos = default;
                this.mLastResult = default;
            }


            public MoveBlockResult LastMoveResult { get { return mLastResult; } }
            public override bool OnBlock(State new_state)
            {
                return true;
            }
            protected override void OnStart()
            {
                var target = this.FindTargetPos();
                this.mMoveAI.FindPath(target);
                unit.SetActionStatus(unit.GetStartMoveStatus());
            }
            protected override void OnStop()
            {
            }
            protected override void OnUpdate()
            {
                if (mExpire.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
                else
                {
                    mLastResult = mMoveAI.Update();
                    if ((mLastResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                    {
                        var target = this.FindTargetPos();
                        this.mMoveAI.FindPath(target);
                    }
                }
            }
            /// <summary>
            /// 搜索要去的地方
            /// </summary>
            /// <returns></returns>
            protected virtual ITerrainLayer FindTargetPos()
            {
                var p = this.mOrginPos;
                return zone.FindNearRandomMoveableNode(ref p, mRange);
                //                 if (node != null)
                //                 {
                //                     return new Vector2(node.PosX, node.PosY);
                //                 }
                //                 return new Vector2(mOrginPos.X, mOrginPos.Y);
            }



            //----------------------------------------------------------------------------------------------------------------------------------------
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public abstract class StateAttackTo : State
        {
            //---------------------------------------------------------------------------------------
            // 如果被地图碰撞，则寻路
            protected IPositionObject target;
            // 如果被单位碰撞，则向左或右挪动
            protected MoveAI moveAI;
            protected float holdTimeMS;
            protected MoveBlockResult lastMovingResult;
            //---------------------------------------------------------------------------------------
            public StateAttackTo() { }
            public StateAttackTo(InstanceUnit unit) : base(unit) { }
            protected virtual StateAttackTo Init(InstanceUnit unit, float move_ai_hold_time_ms = -1)
            {
                this.holdTimeMS = move_ai_hold_time_ms;
                this.moveAI = unit.CreateMoveAI(true, holdTimeMS);
                return this;
            }
            protected override void Disposing()
            {
                this.target = default;
                this.moveAI?.Dispose();
                this.moveAI = null;
                this.holdTimeMS = default;
                this.lastMovingResult = default;
            }
            //---------------------------------------------------------------------------------------

            public IPositionObject Target { get { return target; } }
            public MoveAI UnitMoveAI { get { return moveAI; } }
            public MoveBlockResult LastMovingResult { get => lastMovingResult; }
            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                if (target == null)
                {
                    target = PopNextPos();
                }
                if (target != null)
                {
                    this.moveAI.FindPath(target.Position);
                    unit.SetActionStatus(unit.GetStartMoveStatus());
                }
                else
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }

            override protected void OnUpdate()
            {
                if (target == null)
                {
                    unit.DoSomething();
                }
                else
                {
                    var prev = this.target;
                    lastMovingResult = this.moveAI.Update();
                    if (TestPopNext())
                    {
                        this.target = PopNextPos();
                        if (target != null)
                        {
                            this.OnPassPath(prev, target);
                            this.moveAI.FindPath(target);
                        }
                    }
                }
            }

            override protected void OnStop()
            {

            }
            abstract protected void OnPassPath(IPositionObject current, IPositionObject next);
            abstract protected bool HasNextPos();
            abstract protected IPositionObject PopNextPos();

            protected virtual bool TestPopNext()
            {
                //if (CMath.includeRoundPoint(unit.X, unit.Y, unit.Info.GuardRange, target.X, target.Y))
                if (unit.AGuard && Geometry.CollisionMath.SphereContainsPoint(unit.Position, unit.AGuard.GuardRange, target.Position))
                {
                    return true;
                }
                return false;
            }

            public virtual bool IsDone
            {
                get
                {
                    if (IsStarted)
                    {
                        if (target == null)
                        {
                            return true;
                        }
                        if (HasNextPos())
                        {
                            return false;
                        }
                        return TestPopNext();
                    }
                    return false;
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public class StateAttackToZoneWayPoint : StateAttackTo
        {
            // 如果被地图碰撞，则寻路
            protected InstanceFlag paths;
            protected InstanceFlag prev;
            protected TimeExpire hold_time;

            public static StateAttackToZoneWayPoint Alloc(InstanceUnit unit, InstanceFlag wps)
            {
                var ret = unit.AllocState<StateAttackToZoneWayPoint>();
                ret.Init(unit, wps);
                return ret;
            }
            public StateAttackToZoneWayPoint() { }
            public StateAttackToZoneWayPoint(InstanceUnit unit, InstanceFlag wps) : base(unit)
            {
                Init(unit, wps);
            }
            protected virtual StateAttackToZoneWayPoint Init(InstanceUnit unit, InstanceFlag wps)
            {
                base.Init(unit);
                this.paths = wps;
                return this;
            }
            protected override void Disposing()
            {
                this.paths = default;
                this.prev = default;
                this.hold_time?.Dispose();
                this.hold_time = null;
                base.Disposing();
            }
            protected InstanceFlag Paths { get { return paths; } }
            protected override void OnUpdate()
            {
                if (this.hold_time != null)
                {
                    if (this.hold_time.Update(zone.UpdateIntervalMS))
                    {
                        this.hold_time = null;
                    }
                    else
                    {
                        return;
                    }
                }
                base.OnUpdate();
            }
            protected bool Hold(InstanceFlag point)
            {
                if (point.InvokeTryPathHold(unit, out var hold))
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    this.hold_time = new TimeExpire(unit.RandomN.Next(hold.HoldMinTimeMS, hold.HoldMaxTimeMS));
                    return true;
                }
                return false;
            }
            protected override bool HasNextPos()
            {
                return paths != null;
            }
            protected override bool TestPopNext()
            {
                if (Target is WayPointRandomPoint rp)
                {
                    if (UnitMoveAI.IsNoWay || LastMovingResult.HasFlag(MoveResult.MOVE_RESULT_BLOCK_MAP))
                    {
                        rp.Reset();
                        UnitMoveAI.FindPath(rp);
                    }
                    if (LastMovingResult.touched != null)
                    {
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, unit.Info.GuardRange, Target.X, Target.Y))
                        if (unit.AGuard && Collider.Intersects(unit.Position, Target.Position, unit.AGuard.GuardRange))
                        {
                            Hold(rp.Point);
                            return true;
                        }
                    }
                    else
                    {
                        // if (CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize, Target.X, Target.Y))
                        if (Collider.Intersects(unit.Position, Target.Position, unit.BodyBlockSize))
                        {
                            Hold(rp.Point);
                            return true;
                        }
                    }
                }
                return false;
            }
            protected override IPositionObject PopNextPos()
            {
                if (paths != null)
                {
                    InstanceFlag ret = paths;
                    InstanceFlag prv = prev;
                    prev = paths;
                    paths = paths.PopRandomNext(prv);
                    if (ret != null)
                    {
                        return new WayPointRandomPoint(ret);
                    }
                }
                return null;
            }
            protected override void OnPassPath(IPositionObject current, IPositionObject next)
            {
                if (current is WayPointRandomPoint cp && next is WayPointRandomPoint np)
                {
                    cp.Point.InvokePathPass(unit, np.Point);
                }
            }
            protected class WayPointRandomPoint : IPositionObject
            {
                public Vector3 Target { get; private set; }
                public InstanceFlag Point { get; }
                public EditorScene Parent => Point.Parent;
                public float X => Target.X;
                public float Y => Target.Y;
                public float Z => Target.Z;
                public Vector3 Position => Target;
                public float Direction => Point.Direction;
                public float BodySize => Point.BodySize;
                public float BodyHeight => Point.BodyHeight;
                public IZone Zone => Parent;
                public bool Enable => true;

                public WayPointRandomPoint(InstanceFlag point)
                {
                    Point = point;
                    Target = point.GetRandomPos();
                }
                public void Reset()
                {
                    Target = Point.GetRandomPos();
                }

            }


            //----------------------------------------------------------------------------------------------------------------------------------------

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        public class StateGuardInPosition : State
        {
            //----------------------------------------------------------------------------------------------------
            // 如果被地图碰撞，则寻路
            protected Geometry.Vector3 target;
            // 如果被单位碰撞，则向左或右挪动
            protected MoveAI moveAI;
            protected MoveBlockResult lastMovingResult;
            public static StateGuardInPosition Alloc(InstanceUnit unit, Vector3 wps)
            {
                var ret = unit.AllocState<StateGuardInPosition>();
                ret.Init(unit, wps);
                return ret;
            }
            public StateGuardInPosition() { }
            public StateGuardInPosition(InstanceUnit unit, Vector3 wps) : base(unit)
            {
                Init(unit, wps);
            }
            protected virtual StateGuardInPosition Init(InstanceUnit unit, Geometry.Vector3 target)
            {
                this.target = target;
                this.moveAI = unit.CreateMoveAI();
                return this;
            }
            protected override void Disposing()
            {
                this.target = default;
                this.moveAI?.Dispose();
                this.moveAI = null;
                this.lastMovingResult = default;
            }
            //----------------------------------------------------------------------------------------------------
            public MoveBlockResult LastMovingResult { get => lastMovingResult; }
            public Geometry.Vector3 Target { get { return target; } }
            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                this.moveAI.FindPath(target);
                if (moveAI.IsNoWay)
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                else if (LastMovingResult.result == MoveResult.MOVE_RESULT_HOLD)
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                else
                {
                    unit.SetActionStatus(unit.GetStartMoveStatus());
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }

            override protected void OnUpdate()
            {
                this.lastMovingResult = this.moveAI.Update();
                if (LastMovingResult.result == MoveResult.MOVE_RESULT_HOLD)
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                else if ((LastMovingResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    if ((LastMovingResult.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                    {
                        moveAI.Pause();
                        unit.SetActionStatus(UnitActionStatus.Idle);
                    }
                    else if (unit.AGuard && moveAI.IsInRange(target, unit.AGuard.GuardRange))//if (CMath.includeRoundPoint(unit.X, unit.Y, unit.Info.GuardRange, target.x, target.y))
                    {
                        moveAI.Pause();
                        unit.SetActionStatus(UnitActionStatus.Idle);
                    }
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }

            override protected void OnStop()
            {

            }

            //----------------------------------------------------------------------------------------------------------------------------------------

        }

        /// <summary>
        /// 
        /// </summary>
        public class StateBackToPosition : State
        {
            //----------------------------------------------------------------------------------------------------
            protected MoveBlockResult lastMovingResult;
            // 如果被地图碰撞，则寻路
            protected Geometry.Vector3 target;
            // 如果被单位碰撞，则向左或右挪动
            protected MoveAI moveAI;
            protected bool isDone = false;
            public static StateBackToPosition Alloc(InstanceUnit unit, Vector3 wps)
            {
                var ret = unit.AllocState<StateBackToPosition>();
                ret.Init(unit, wps);
                return ret;
            }
            public StateBackToPosition() { }
            public StateBackToPosition(InstanceUnit unit, Vector3 wps) : base(unit)
            {
                Init(unit, wps);
            }
            protected virtual StateBackToPosition Init(InstanceUnit unit, Geometry.Vector3 target)
            {
                this.target = target;
                this.moveAI = unit.CreateMoveAI();
                return this;
            }
            protected override void Disposing()
            {
                this.isDone = false;
                this.target = default;
                this.moveAI?.Dispose();
                this.moveAI = null;
                this.lastMovingResult = default;
            }
            //----------------------------------------------------------------------------------------------------
            public MoveBlockResult LastMovingResult { get => lastMovingResult; }
            public Geometry.Vector3 Target { get { return target; } set => target = value; }
            public bool IsDone { get { return isDone; } }

            public override bool OnBlock(State new_state)
            {
                if (new_state is StateDamage)
                {
                    return true;
                }
                if (new_state is StateDead)
                {
                    return true;
                }
                return isDone;
            }
            protected override void OnStart()
            {
                this.moveAI.FindPath(target);
                if (moveAI.IsNoWay)
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    unit.Transport(target);
                }
                else
                {
                    unit.SetActionStatus(unit.GetStartMoveStatus());
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }

            protected override void OnUpdate()
            {
                this.lastMovingResult = this.moveAI.Update();
                if ((LastMovingResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    if (unit.AGuard && moveAI.IsInRange(target, unit.AGuard.GuardRange))
                    {
                        isDone = true;
                        unit.DoSomething();
                    }
                    else
                    {
                        moveAI.FindPath(target);
                    }
                }
            }

            protected override void OnStop()
            {

            }

            //----------------------------------------------------------------------------------------------------------------------------------------

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 移动状态
        /// </summary>
        public class StateMoveAway : State
        {
            private readonly VectorObject2 target = new VectorObject2();
            private float distance;
            private InstanceUnit other;
            public static StateMoveAway Alloc(InstanceUnit unit, InstanceUnit other, float distance, float angleOffset)
            {
                var ret = unit.AllocState<StateMoveAway>();
                ret.other = other;
                ret.target.X = (unit.X);
                ret.target.Y = (unit.Y);
                ret.distance = distance;
                float angle = MathVector.getDegree(other.X, other.Y, unit.X, unit.Y);
                angle += angleOffset;// (float)(-CMath.PI_DIV_2 + unit.RandomN.NextDouble() * CMath.PI_F);
                MathVector.movePolar(ret.target, angle, distance);
                return ret;
            }
            protected override void Disposing()
            {
                distance = 0;
                other = null;
            }


            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnUpdate()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
                unit.FaceTo(target.X, target.Y);
                MoveBlockResult result = unit.MoveBlockTo(target.X, target.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, true);
                if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop()
            {

            }
        }

        /// <summary>
        /// 优先大路点寻路再坐标寻路
        /// </summary>
        public class StateMoveFindPathWayPoint : State
        {
            /// <summary>
            /// 寻路AI
            /// </summary>
            private MoveAI mMoveAI;
            /// <summary>
            /// 目标点
            /// </summary>
            private Geometry.Vector3 mTargetPos;
            /// <summary>
            /// MOVEAI状态
            /// </summary>
            private MoveBlockResult mLastResult;
            /// <summary>
            /// 寻路点
            /// </summary>
            private List<Vector3> mPathPoints;
            /// <summary>
            /// 寻路路点下标
            /// </summary>
            private int mPathIndex = -1;
            /// <summary>
            /// 路点搜索范围
            /// </summary>
            private float mWayPointSearchRange = 0;
            /// <summary>
            /// 完成后的回调，有则不再执行dosomething
            /// </summary>
            private Action mOverrideEndCallBack = null;
            /// <summary>
            /// 是否结束
            /// </summary>
            private bool mIsEnd = false;

            public event Func<StateMoveFindPathWayPoint, ZoneWayPoint, bool> FlagSelector;

            public static StateMoveFindPathWayPoint Alloc(
                InstanceUnit unit,
                Geometry.Vector3 targetPos,
                float wayPointSearchRange = 10,
                Action endCallBack = null)
            {
                var ret = unit.AllocState<StateMoveFindPathWayPoint>();

                ret.mMoveAI = unit.CreateMoveAI();
                ret.mMoveAI.IsFirstFindPath = false; //不需要第一次寻路
                ret.mTargetPos = targetPos;
                ret.mWayPointSearchRange = wayPointSearchRange;
                ret.mOverrideEndCallBack = endCallBack;
                return ret;
            }

            public override bool OnBlock(State new_state)
            {
                return true;
            }

            protected override void Disposing()
            {
                mPathPoints?.Clear();
                mMoveAI?.Dispose();
                mMoveAI = null;
                mOverrideEndCallBack = null;
                FlagSelector = null;
            }

            protected override void OnStart()
            {
                if (mPathPoints == null)
                {
                    mPathPoints = new List<Vector3>();
                }
                else
                {
                    mPathPoints.Clear();
                }

                mPathIndex = -1;
                mIsEnd = false;
                var parent = unit.Parent;

                //找到离自己最近的路点
                var wpSrc = GetNearestWayPoint(unit.Position, unit.Parent, mWayPointSearchRange);

                //找到离目标点最近的路点
                var wpDst = GetNearestWayPoint(mTargetPos, unit.Parent, mWayPointSearchRange);


                if (wpSrc == null || wpDst == null)
                {
                    if (wpDst != null)
                    {
                        //log.Warn("StateMoveFindPathWayPoint Warnning:找不到离角色最近的路点");
                        mPathPoints.Add(wpDst.Position);
                    }
                    else if (wpSrc != null)
                    {
                        //log.Warn("StateMoveFindPathWayPoint Warnning:找不到离目标位置最近的路点");
                        mPathPoints.Add(wpSrc.Position);
                    }
                    //都没有直接寻路
                    mPathPoints.Add(mTargetPos);
                }
                else
                {
                    //大路点间寻路路径
                    var zoneComp = parent.Components.GetOrAddComponentAs<WayPointAstarZoneComponent>();
                    var flagAstar = zoneComp.FlagAstar;
                    var path = flagAstar.FindPath(wpSrc.Name, wpDst.Name);
                    if (path == null)
                    {
                        log.ErrorFormat("StateMoveFindPathWayPoint Error:{0}{1}没有可寻路路径", wpSrc.Name, wpDst.Name);
                    }
                    else
                    {
                        //大路点路径
                        mPathPoints.Add(path.Position);
                        while (path.Next != null)
                        {
                            path = path.Next;
                            mPathPoints.Add(path.Position);
                        }
                    }

                    mPathPoints.Add(mTargetPos);//最后终点
                }


                if (TryPopNextPoint(out var pos))
                {
                    mMoveAI.FindPath(pos);
                    unit.SetActionStatus(unit.GetStartMoveStatus());
                }
                else
                {
                    unit.DoSomething();
                }
            }

            protected override void OnStop()
            {
            }

            protected override void OnUpdate()
            {
                if (mIsEnd)
                {
                    unit.DoSomething();
                    return;
                }

                mLastResult = mMoveAI.Update();
                if ((mLastResult.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    if (TryPopNextPoint(out var target))
                    {
                        this.mMoveAI.FindPath(target);
                    }
                    else
                    {
                        if (mOverrideEndCallBack != null)
                        {
                            mOverrideEndCallBack.Invoke();
                        }
                        else
                        {
                            unit.DoSomething();
                        }

                        mIsEnd = true;
                    }
                }
            }

            private ZoneWayPoint GetNearestWayPoint(in Vector3 pos, InstanceZone zone, float wayPointSearchRange)
            {
                var allFlags = zone.AllFlags;
                var min = float.MaxValue;
                ZoneWayPoint targetPt = null;
                foreach (var flag in allFlags)
                {
                    if (flag is ZoneWayPoint pt)
                    {
                        if (FlagSelector == null || FlagSelector.Invoke(this, pt))
                        {
                            var dis = Vector3.DistanceSquared(pt.Position, pos);
                            if (dis < min)
                            {
                                min = dis;
                                targetPt = pt;
                            }
                        }
                    }
                }
                return targetPt;
            }

            private bool TryPopNextPoint(out Vector3 v3)
            {
                if (mPathIndex < mPathPoints.Count - 1)
                {
                    mPathIndex += 1;
                    v3 = mPathPoints[mPathIndex];
                    return true;
                }
                v3 = default;
                return false;
            }
        }
    }
}
//----------------------------------------------------------------------------------------------------------------------------------------

