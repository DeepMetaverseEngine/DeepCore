using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Log;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System.Collections.Generic;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance.Components
{

    public class PlayerAIComponent : UnitComponent
    {
        protected Logger log;

        protected UnitHateComponent mHateComponent;

        protected StateFollowAndAttack mFocusTarget;
        protected StateFollowTarget mFollowTarget;
        protected StateFollowAndPickItem mFocusPickItem;
        protected StatePlayerAttackTo mAttackTo;
        protected TimeInterval mCheckGuard;
        protected bool IsFollowTarget
        {
            get { return (mFollowTarget != null && mFollowTarget.IsActive && Owner.CurrentState == mFollowTarget); }
        }
        protected bool IsFocusPickItem
        {
            get { return (mFocusPickItem != null && mFocusPickItem.IsActive && Owner.CurrentState == mFocusPickItem); }
        }
        protected bool IsFocusTarget
        {
            get { return (mFocusTarget != null && mFocusTarget.IsActive && Owner.CurrentState == mFocusTarget); }
        }
        protected bool IsAttackTo
        {
            get { return (mAttackTo != null && !mAttackTo.IsDone && Owner.CurrentState == mAttackTo); }
        }

        public float DefaultHoldMinTimeMS = 2000;
        public float DefaultHoldMaxTimeMS = 4000;
        public bool DefaultAttackToWayPoint = false;
        public bool DefaultDoSomething = true;

        public PlayerAIComponent()
        {
            this.log = LoggerFactory.GetLogger(GetType());
            this.log.Color = System.ConsoleColor.Magenta;
        }
        protected override void OnDispose(InstanceZoneObject owner)
        {
            CleanAll();
            base.OnDispose(owner);
        }
        protected virtual void CleanFocus()
        {
            if (mFocusPickItem != null)
            {
                mFocusPickItem.Dispose();
                mFocusPickItem = null;
            }
            if (mFocusTarget != null)
            {
                mFocusTarget.Dispose();
                mFocusTarget = null;
            }
        }
        protected virtual void CleanAll()
        {
            if (mFocusPickItem != null)
            {
                mFocusPickItem.Dispose();
                mFocusPickItem = null;
            }
            if (mFocusTarget != null)
            {
                mFocusTarget.Dispose();
                mFocusTarget = null;
            }
            if (mFollowTarget != null)
            {
                mFollowTarget.Dispose();
                mFollowTarget = null;
            }
            if (mAttackTo != null)
            {
                mAttackTo.Dispose();
                mAttackTo = null;
            }
        }

        protected override void OnAdded()
        {
            this.mCheckGuard = new TimeInterval(Owner.CFG.AI_VIEW_TRIGGER_CHECK_TIME_MS);
            base.OnAdded();
            this.Owner.OnDamage += Owner_OnDamage;
            this.Owner.OnUpdateAI += Owner_OnUpdateAI;
            this.Owner.OnHandleAction += Owner_OnHandleAction;
            this.Owner.OnHandleResetAI += Owner_OnHandleResetAI;
            this.Owner.OnDoSomething += Owner_OnDoSomething;
        }
        protected override void OnRemoved()
        {
            CleanAll();
            base.OnRemoved();
            this.Owner.OnDamage -= Owner_OnDamage;
            this.Owner.OnUpdateAI -= Owner_OnUpdateAI;
            this.Owner.OnHandleAction -= Owner_OnHandleAction;
            this.Owner.OnHandleResetAI -= Owner_OnHandleResetAI;
            this.Owner.OnDoSomething -= Owner_OnDoSomething;
        }

        protected virtual void Owner_OnUpdateAI(InstanceUnit unit)
        {
            if (TryCheckIsOver())
            {

            }
            if (TryContinueFollowTarget())
            {
                return;
            }
            {
                UpdateCancelFocusTargetSkill();
                UpdateCheckNewState();
            }
        }

        protected virtual void Owner_OnHandleAction(InstanceUnit unit, ObjectAction act)
        {
            switch (act)
            {
                case UnitStopMoveAction stopMove:
                    CleanAll();
                    break;
                case UnitAttackToAction attackTo:
                    DoAttackTo(attackTo);
                    break;
                case UnitFollowTargetAction followTarget:
                    DoFollowTarget(followTarget);
                    break;
                case UnitFocuseTargetAction focusTarget:
                    DoFocusTarget(focusTarget);
                    break;
            }
        }
        protected virtual void Owner_OnDamage(InstanceUnit obj, InstanceUnit attacker, long hp, in TAttackSource attack, in TAttackResult result)
        {
            if (mAttackTo != null && !mAttackTo.IsAttack) { return; }
            if (Zone.Formula.IsAttackable(Owner, attacker, SkillTemplate.CastTarget.Enemy, AttackReason.Damaged))
            {
                if (mHateComponent == null)
                {
                    this.mHateComponent = Owner.Components.GetComponentAs<UnitHateComponent>();
                }
                if (mHateComponent != null)
                {
                    mHateComponent.HateSystem.OnHitted(attacker, in attack, in result, hp);
                }
                CleanFocus();
                StartFocusAttack(attacker, SkillTemplate.CastTarget.Enemy);
            }
        }
        protected virtual void Owner_OnHandleResetAI(InstanceUnit obj)
        {
            CleanAll();
            DoSomething();
        }
        protected virtual bool Owner_OnDoSomething(InstanceUnit obj, bool handed)
        {
            return DoSomething();
        }

        public virtual bool DoSomething()
        {
            if (TryCheckContinue()) { return true; }
            if (TryFindNewTarget()) { return true; }
            if (TryDoSomting()) { return true; }
            return false;
        }

        // 旋风斩贴近目标 //
        protected virtual void UpdateCancelFocusTargetSkill()
        {
            if (Owner.CurrentState is StateSkill skill)
            {
                if ((mFocusTarget != null))
                {
                    var d = MathVector.getDistance(Owner.X, Owner.Y, mFocusTarget.Target.X, mFocusTarget.Target.Y) - mFocusTarget.TargetUnit.BodyHitSize;
                    var range = Owner.GetSkillAttackRange(skill.SkillData);
                    if (d > range)//超出范围靠近.
                    {
                        skill.controlMoveTo(mFocusTarget.Target.X, mFocusTarget.Target.Y, 1f);
                    }
                    else
                    {
                        skill.controlFaceTo(mFocusTarget.Target.X, mFocusTarget.Target.Y);
                    }
                    Owner.TryLaunchRandomSkillAndCancelCurrentSkill(mFocusTarget.TargetUnit, true);
                }
            }
        }
        // 没目标定期检测目标 //
        protected virtual void UpdateCheckNewState()
        {
            if (Owner.CurrentState is StateSkill || Owner.CurrentState is StatePickObject || Owner.CurrentState is StateMoveAway)
            {

            }
            else if (mCheckGuard.Update(base.Owner.Parent.UpdateIntervalMS))
            {
                if ((!IsFocusTarget) && (!IsFocusPickItem) && (!IsFollowTarget))
                {
                    if (!TryFindNewTarget())
                    {
                        if (!TryCheckContinue())
                        {
                            TryDoSomting();
                        }
                    }
                }
                else
                {
                    if (!TryCheckContinue())
                    {
                        TryDoSomting();
                    }
                }
            }
        }

        protected virtual bool TryCheckIsOver()
        {
            bool changed = false;
            if (mFocusTarget != null && !mFocusTarget.IsActive)
            {
                changed = true;
                mFocusTarget = null;
            }
            if (mFocusPickItem != null && !mFocusPickItem.IsActive)
            {
                changed = true;
                mFocusPickItem = null;
            }
            if (mFollowTarget != null && !mFollowTarget.IsActive)
            {
                changed = true;
                mFollowTarget = null;
            }
            if (mAttackTo != null)
            {
                if (mAttackTo.IsDone)
                {
                    changed = true;
                    mAttackTo = null;
                }
                else if (!mAttackTo.IsAttack)
                {
                    changed = true;
                    CleanFocus();
                }
            }
            return changed;
        }
        protected virtual bool TryFindNewTarget()
        {
            if ((mAttackTo == null || mAttackTo.IsAttack))
            {
                {
                    // 优先捡取
                    var item = FindFocusRangedPickItem();
                    if (item != null)
                    {
                        if (StartFocusPickItem(item) != null)
                        {
                            return true;
                        }
                    }
                }
                {
                    // 先找仇恨
                    var hated = FindFocusHateTarget(SkillTemplate.CastTarget.Enemy, AttackReason.Look);
                    if (hated != null)
                    {
                        if (StartFocusAttack(hated, SkillTemplate.CastTarget.Enemy))
                        {
                            return true;
                        }
                    }
                    // 再找范围内敌人
                    var enemy = FindFocusRangedAttackTarget(SkillTemplate.CastTarget.Enemy, AttackReason.Look);
                    if (enemy != null)
                    {
                        if (StartFocusAttack(enemy, SkillTemplate.CastTarget.Enemy))
                        {
                            return true;
                        }
                    }
                    // 再找范围内友军
                    var alias = FindFocusRangedAttackTarget(SkillTemplate.CastTarget.AlliesIncludeSelf, AttackReason.Look);
                    if (alias != null)
                    {
                        if (StartFocusAttack(alias, SkillTemplate.CastTarget.AlliesIncludeSelf))
                        {
                            return true;
                        }
                    }
                    // 再找范围内可用的技能
                    var avaliable = Unit.GetAvailableSkill();
                    if (avaliable != null)
                    {
                        var anyone = FindFocusRangedAttackTarget(avaliable.Data.ExpectTarget, AttackReason.Look);
                        if (anyone != null)
                        {
                            if (StartFocusAttack(anyone, avaliable.Data.ExpectTarget))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }



        protected virtual bool TryDoSomting()
        {
            if (DefaultDoSomething)
            {
                var walkto = FindAutoAttackTo();
                if (walkto != null)
                {
                    if (StartAttackTo(walkto.UpwardCenterPos, null, true) != null)
                    {
                        return true;
                    }
                }
                Owner.StartIdle();
            }
            return false;
        }

        protected virtual bool TryCheckContinue()
        {
            if (TryContinueFollowTarget()) { return true; }
            if (TryContinueFocusPickItem()) { return true; }
            if (TryContinueFocusTarget()) { return true; }
            if (TryContinueAttackTo()) { return true; }
            return false;
        }
        protected virtual bool TryContinueFollowTarget()
        {
            if (mFollowTarget != null)
            {
                if (!mFollowTarget.IsActive)
                {
                    mFollowTarget = null;
                    return false;
                }
                if (Owner.CurrentState == mFollowTarget)
                {
                    if (mFollowTarget.IsNoWay)
                    {
                        return true;
                    }
                    if (mFollowTarget.FollowState == StateFollow.MoveState.Move)
                    {
                        return true;
                    }
                    if (mFollowTarget.Action.autoAttack == false)
                    {
                        mAttackTo = null;
                        CleanFocus();
                        return true;
                    }
                }
                else
                {
                    if (mFollowTarget.IsOutRange)
                    {
                        CleanFocus();
                        //if (Owner.CurrentState is not StateFollowTarget) { log.Info($"TryContinueFollowTarget : {mFollowTarget.Target}"); }
                        Owner.ChangeState(mFollowTarget);
                        return true;
                    }
                }
            }
            return false;
        }
        protected virtual bool TryContinueFocusPickItem()
        {
            if (mFocusPickItem != null)
            {
                if (mFocusPickItem.IsActive)
                {
                    if (mFocusPickItem.IsNoWay)
                    {
                        mFocusPickItem = null;
                    }
                    else if (!mFocusPickItem.TargetItem.IsPickable(Owner))
                    {
                        mFocusPickItem = null;
                    }
                    else
                    {
                        //if (Owner.CurrentState is not StateFollowAndPickItem && Owner.CurrentState is not StatePickObject) log.Info($"TryContinuePickItem : {mFocusPickItem.Target}");
                        //Owner.changeState(mFocusPickItem);
                        return StartFocusPickItem(mFocusPickItem.TargetItem) != null;
                    }
                }
                else
                {
                    mFocusPickItem = null;
                }
            }
            return false;
        }
        protected virtual bool TryContinueFocusTarget()
        {
            if (mFocusTarget != null)
            {
                if (mFocusTarget.IsActive)
                {
                    //                     var skill = Owner.GetAvailableSkill(mFocusTarget.ExpectTarget);
                    //                     if (skill==null)
                    //                     {
                    //                         mFocusTarget = null;
                    //                         return false;
                    //                     }
                    if ((mFocusTarget.ExpectTarget & SkillTemplate.CastTarget.AlliesIncludeSelf) != 0)
                    {
                        mFocusTarget = null;
                        return false;
                    }
                    else if (mFocusTarget.IsNoWay)
                    {
                        mFocusTarget = null;
                        return false;
                    }
                    else if (!Owner.Parent.Formula.IsAttackable(Owner, mFocusTarget.TargetUnit, mFocusTarget.ExpectTarget, AttackReason.Attack, Owner.Info))
                    {
                        mFocusTarget = null;
                        return false;
                    }
                    else
                    {
                        //if (Owner.CurrentState is not StateFollowAndAttack && Owner.CurrentState is not StateSkill) log.Info($"TryContinueFocusTarget : {mFocusTarget.Target}");
                        //Owner.changeState(mFocusTarget);
                        return StartFocusAttack(mFocusTarget.TargetUnit);
                    }
                }
                else
                {
                    mFocusTarget = null;
                }
            }
            return false;
        }
        protected virtual bool TryContinueAttackTo()
        {
            if (mAttackTo != null && !mAttackTo.IsDone)
            {
                if (Owner.CurrentState != mAttackTo)
                {
                    Owner.ChangeState(mAttackTo);
                }
                //if (Owner.CurrentState is not StatePlayerAttackTo) log.Info($"TryContinueAttackTo");
                return true;
            }
            return false;
        }




        protected virtual InstanceUnit FindFocusHateTarget(SkillTemplate.CastTarget expect = SkillTemplate.CastTarget.Enemy, AttackReason reason = AttackReason.Look)
        {
            InstanceUnit min = null;
            if (Owner.AGuard)
            {
                var skill = Owner.GetAvailableSkill(expect);
                if (skill != null)
                {
                    if (mHateComponent == null)
                    {
                        this.mHateComponent = Owner.Components.GetComponentAs<UnitHateComponent>();
                    }
                    if (mHateComponent != null && mHateComponent.HateSystem.TryGetHated(out var hated))
                    {
                        if (Zone.Formula.IsAttackableBySkill(Unit, hated, skill, reason))
                        {
                            return hated;
                        }
                    }
                }
            }
            return min;
        }
        protected virtual InstanceUnit FindFocusRangedAttackTarget(SkillTemplate.CastTarget expect = SkillTemplate.CastTarget.Enemy, AttackReason reason = AttackReason.Look)
        {
            InstanceUnit min = null;
            if (Owner.AGuard)
            {
                var skill = Owner.GetAvailableSkill(expect);
                if (skill != null)
                {
                    var min_len = float.MaxValue;
                    using (var st = Owner.ObjectPool.AllocForEach3((Owner, reason, skill), min, min_len, default(InstanceUnit)))
                    {
                        Owner.Parent.ForEachNearObjectsPredicate(Owner.X, Owner.Y, Owner.AGuard.GuardRange, st, static (input, u) =>
                        {
                            var st = input.Arg1;
                            if (st.Owner.Parent.Formula.IsAttackableBySkill(st.Owner, u, st.skill, st.reason))
                            {
                                if (st.Owner.IsInAttackRange(st.skill.Data, u))
                                {
                                    input.Arg2 = u;
                                    return true;
                                }
                                if (st.Owner.IsInGuardLimit(u))
                                {
                                    float len = MathVector.getDistanceSquare(u.X, u.Y, st.Owner.X, st.Owner.Y);
                                    if (input.Arg3 > len)
                                    {
                                        input.Arg3 = len;
                                        input.Arg2 = u;
                                    }
                                }
                            }
                            return false;
                        }, default(InstanceUnit));
                        min = st.Arg2;
                    }

                }
            }
            return min;
        }


        protected virtual InstanceItem FindFocusRangedPickItem()
        {
            if (Owner.AGuard)
            {
                using (var input = Owner.ObjectPool.AllocForEach3(Owner, default(InstanceItem), float.MaxValue, default(InstanceItem)))
                {
                    Owner.Parent.ForEachNearObjectsPredicate(Owner.X, Owner.Y, Owner.AGuard.GuardRange, input, static (input, u) =>
                    {
                        var Owner = input.Arg1;
                        if (u.IsPickable(Owner))
                        {
                            if (Owner.IsInPickRange(u))
                            {
                                input.Arg2 = u;
                                return true;
                            }
                            if (Owner.IsInGuardLimit(u))
                            {
                                float len = MathVector.getDistanceSquare(u.X, u.Y, Owner.X, Owner.Y);
                                if (input.Arg3 > len)
                                {
                                    input.Arg3 = len;
                                    input.Arg2 = u;
                                }
                            }
                        }
                        return false;
                    }, default(InstanceItem));
                    return input.Arg2;
                }
            }
            return null;
        }
        protected virtual ITerrainLayer FindAutoAttackTo()
        {
            if (Owner.AGuard && Owner.CurrentLayer != null)
            {
                return Owner.Parent.FindNearRandomMoveableNode(Owner.CurrentLayer, Owner.AGuard.GuardRange);
            }
            return null;
        }

        protected virtual EquipSkill StartLaunchSkill(InstanceUnit target, SkillTemplate.CastTarget expectTarget = SkillTemplate.CastTarget.Enemy)
        {
            if (Owner.CurrentState is StateSkill skill && !skill.IsDone)
            {
                return skill.Skill;
            }
            var launchSkill = Owner.GetRandomLaunchableExpectSkill(expectTarget);
            if (launchSkill != null)
            {
                if (launchSkill != null && Owner.IsInAttackRange(launchSkill.Data, target) && launchSkill.CheckTargetRange(target))
                {
                    var expect_skill = launchSkill.Data;
                    if (expect_skill != null && expect_skill.AttackKeepRange > 0)
                    {
                        if (CUtils.RandomPercent(Zone.RandomN, Zone.CFG.AI_FOLLOW_AND_ATTACK_ADJUST_ESCAPE_PCT))
                        {
                            if (Owner.StartAdjustLaunchSkill(expect_skill, target))
                            {
                                return launchSkill;
                            }
                        }
                    }
                    var ret = Owner.LaunchSkill(launchSkill, new TLaunchSkillParam(target.ID)
                    {
                        AutoFocusNearTarget = false,
                    });
                    if (ret != null)
                    {
                        Owner.FaceTo(target.Position);
                        //log.Info($"StartLaunchSkill : {target}");
                        return ret;
                    }
                }
            }
            return null;
        }
        protected virtual bool StartFocusAttack(InstanceUnit target, SkillTemplate.CastTarget expect_target = SkillTemplate.CastTarget.Enemy)
        {
            if (target != null && Owner.Parent.Formula.IsAttackable(Owner, target, expect_target, AttackReason.Tracing, Owner.Info))
            {
                var skill = StartLaunchSkill(target, expect_target);
                if (skill != null)
                {
                    if (mFocusTarget == null || mFocusTarget.TargetUnit != target)
                    {
                        mFocusTarget?.Dispose();
                        mFocusTarget = new StateFollowAndAttack(Owner, target, expect_target, false);
                    }
                    return true;
                }
                if (Owner.CurrentState is StateSkill skill1 && skill1.TargetUnit == target)
                {
                    if (mFocusTarget == null || mFocusTarget.TargetUnit != target)
                    {
                        mFocusTarget?.Dispose();
                        mFocusTarget = new StateFollowAndAttack(Owner, target, expect_target, false);
                    }
                    return true;
                }
                if (mFocusTarget != null)
                {
                    if (Owner.CurrentState == mFocusTarget && mFocusTarget.IsActive)
                    {
                        return mFocusTarget != null;
                    }
                    if (mFocusTarget.TargetUnit != target)
                    {
                        mFocusTarget?.Dispose();
                        mFocusTarget = new StateFollowAndAttack(Owner, target, expect_target, false);
                        Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerFocuseTargetEvent>().Init (Owner.ID, target.ID, expect_target));
                        Owner.ChangeState(mFocusTarget);
                    }
                    else if (Owner.CurrentState != mFocusTarget)
                    {
                        if (Owner.CurrentActionStatus != UnitActionStatus.Skill)
                        {
                            Owner.ChangeState(mFocusTarget);
                        }
                    }
                }
                else
                {
                    mFocusTarget?.Dispose();
                    mFocusTarget = new StateFollowAndAttack(Owner, target, expect_target, false);
                    Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerFocuseTargetEvent>().Init (Owner.ID, target.ID, expect_target));
                    Owner.ChangeState(mFocusTarget);
                }
                //log.Info($"StartFocusAttack : {mFocusTarget.Target}");
            }
            else
            {
                mFocusTarget = null;
            }
            return mFocusTarget != null;
        }
        protected virtual State StartPickItem(InstanceItem target)
        {
            if (Owner.CurrentState is StatePickObject pick && !pick.IsDone)
            {
                return pick;
            }
            if (target.PickItem(Owner))
            {
                //log.Info($"StartPickItem : {target}");
                return Owner.NextState;
            }
            return null;
        }
        protected virtual State StartFocusPickItem(InstanceItem target)
        {
            if (target != null)
            {
                var pick = StartPickItem(target);
                if (pick != null)
                {
                    if (mFocusPickItem == null || mFocusPickItem.TargetItem != target)
                    {
                        mFocusPickItem?.Dispose();
                        mFocusPickItem = new StateFollowAndPickItem(Owner, target);
                    }
                    return pick;
                }
                if (Owner.CurrentState is StatePickObject pick1 && pick1.Target == target)
                {
                    if (mFocusPickItem == null || mFocusPickItem.TargetItem != target)
                    {
                        mFocusPickItem?.Dispose();
                        mFocusPickItem = new StateFollowAndPickItem(Owner, target);
                    }
                    return pick1;
                }
                if (mFocusPickItem != null)
                {
                    if (Owner.CurrentState == mFocusPickItem && mFocusPickItem.IsActive)
                    {
                        return mFocusPickItem;
                    }
                    if (mFocusPickItem.TargetItem != target)
                    {
                        mFocusPickItem?.Dispose();
                        mFocusPickItem = new StateFollowAndPickItem(Owner, target);
                        if (Owner.ChangeState(mFocusPickItem))
                        {
                            Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerFocuseTargetEvent>().Init (Owner.ID, target.ID, SkillTemplate.CastTarget.NA));
                        }
                    }
                    else if (Owner.CurrentState != mFocusPickItem)
                    {
                        if (Owner.CurrentActionStatus != UnitActionStatus.Pick)
                        {
                            Owner.ChangeState(mFocusPickItem);
                        }
                    }
                }
                else
                {
                    mFocusPickItem?.Dispose();
                    mFocusPickItem = new StateFollowAndPickItem(Owner, target);
                    if (Owner.ChangeState(mFocusPickItem))
                    {
                        Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerFocuseTargetEvent>().Init (Owner.ID, target.ID, SkillTemplate.CastTarget.NA));
                    }
                }
                //log.Info($"StartFocusPickItem : {mFocusPickItem.Target}");
            }
            else
            {
                mFocusPickItem = null;
            }
            return mFocusPickItem;
        }
        protected virtual State StartAttackTo(Geometry.Vector3? target, string name, bool autoAttack)
        {
            //log.Info($"StartAttackTo : {target} : {autoAttack}");
            var flag = Zone.GetFlag(name);
            mAttackTo?.Dispose();
            mAttackTo = new StatePlayerAttackTo(Owner, target, flag, autoAttack,
                DefaultAttackToWayPoint,
                Owner.RandomN.NextFloat(DefaultHoldMinTimeMS, DefaultHoldMaxTimeMS));
            Owner.ChangeState(mAttackTo);
            return mAttackTo;
        }


        protected virtual void DoFollowTarget(UnitFollowTargetAction focus)
        {
            CleanAll();
            {
                var src = Owner.Parent.GetObject<InstanceZoneObject>(focus.targetUnitID);
                //log.Info($"DoFollowTarget : {src}");
                if (src is InstanceUnit target && target != Owner)
                {
                    if (focus.minDistance <= 0)
                    {
                        focus.minDistance = target.BodyBlockSize + Owner.BodyBlockSize;
                    }
                    if (focus.maxDistance <= focus.minDistance)
                    {
                        focus.maxDistance = focus.autoAttack && Owner.AGuard ? Owner.AGuard.GuardRange : target.BodyBlockSize + Owner.BodyBlockSize * 2;
                    }
                    if (focus.tpDistance > 0 && focus.tpDistance <= focus.maxDistance)
                    {
                        focus.tpDistance = focus.maxDistance * 2;
                    }
                    if (focus.slotIndex > 0)
                    {
                        var step = focus.minDistance * focus.slotIndex;
                        focus.minDistance += step;
                        focus.maxDistance += step;
                    }
                    this.mFollowTarget?.Dispose();
                    this.mFollowTarget = new StateFollowTarget(Owner, target, focus);
                    //Owner.SetGuard(true, true);
                    //this.mIsSkillControlByServer = IsGuard || (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient);
                }
                else
                {
                    this.mFollowTarget?.Dispose();
                    this.mFollowTarget = null;
                    //this.SetGuard(false, true);
                    //this.mIsSkillControlByServer = IsGuard || (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient);
                }
            }
            Owner.ResetAI();
        }
        protected virtual void DoFocusTarget(UnitFocuseTargetAction focus)
        {
            CleanAll();
            var src = Owner.Parent.GetObject<InstanceZoneObject>(focus.targetUnitID);
            //log.Info($"DoFocusTarget : {src}");
            if ((src is InstanceUnit))
            {
                if (!Owner.IsNoneSkill && ((src as InstanceUnit).IsActive))
                {
                    StartFocusAttack(src as InstanceUnit);
                }
            }
            else if ((src is InstanceItem))
            {
                if (src.Enable)
                {
                    StartFocusPickItem(src as InstanceItem);
                }
            }
        }
        protected virtual void DoAttackTo(UnitAttackToAction act)
        {
            CleanAll();
            //log.Info($"DoFocusTarget : {act}");
            StartAttackTo(act.target, act.name, act.attack);
        }

        //--------------------------------------------------------------------------------------
        public class StatePlayerAttackTo : State
        {
            private TimeExpire holdTime;
            private TargetPos target;
            private MoveAI moveAI;
            private bool is_attack;
            private bool is_waypoint;

            private float mWayPointSearchRange;
            private readonly Stack<Vector3> mPathPoints = new Stack<Vector3>();

            public bool IsAttack { get { return is_attack; } }
            public bool IsDone { get { return target == null || holdTime.IsEnd; } }

            public StatePlayerAttackTo(InstanceUnit unit, Geometry.Vector3? pos, InstanceFlag flag, bool attack, bool useWayPoints, float holdMS) : base(unit)
            {
                this.mWayPointSearchRange = unit.AGuard ? (unit.AGuard.GuardRange + unit.AGuard.GuardRangeLimitAppend) : unit.Zone.CFG.CLIENT_SYNC_UNIT_MAX_RANGE;
                this.mPathPoints.Clear();
                if (flag != null)
                {
                    this.target = new TargetPos(unit.Parent, flag.GetRandomPos());
                }
                else if (pos.HasValue)
                {
                    this.target = new TargetPos(unit.Parent, pos.Value);
                }
                this.is_attack = attack;
                this.is_waypoint = useWayPoints;
                this.holdTime = unit.AllocTimeExpire(holdMS);
                this.moveAI = unit.CreateMoveAI(true, 0);
                this.moveAI.IsNoWayAutoFindNear = false; ;
            }
            protected override void Disposing()
            {
                this.holdTime?.Dispose();
                this.holdTime = null;
                this.moveAI?.Dispose();
                this.moveAI = null;
                this.mPathPoints.Clear();
            }
            public override bool OnBlock(State new_state)
            {
                return true;
            }

            override protected void OnStart()
            {
                if (target != null)
                {
                    mPathPoints.Push(target.Position);//最后终点
                    if (this.is_waypoint)
                    {
                        //找到离自己最近的路点
                        var wpSrc = GetNearestWayPoint(unit.Position, unit.Parent, mWayPointSearchRange);
                        //找到离目标点最近的路点
                        var wpDst = GetNearestWayPoint(target.Position, unit.Parent, mWayPointSearchRange);
                        if (wpSrc == null || wpDst == null)
                        {
                            if (wpDst != null)
                            {
                                //log.Warn("StateMoveFindPathWayPoint Warnning:找不到离角色最近的路点");
                                mPathPoints.Push(wpDst.Position);
                            }
                            if (wpSrc != null)
                            {
                                //log.Warn("StateMoveFindPathWayPoint Warnning:找不到离目标位置最近的路点");
                                mPathPoints.Push(wpSrc.Position);
                            }
                        }
                        else
                        {
                            //大路点间寻路路径
                            var zoneComp = zone.Components.GetOrAddComponentAs<WayPointAstarZoneComponent>();
                            var flagAstar = zoneComp.FlagAstar;
                            var path = flagAstar.FindPath(wpDst.Name, wpSrc.Name);
                            if (path != null)
                            {
                                //大路点路径
                                mPathPoints.Push(path.Position);
                                while (path.Next != null)
                                {
                                    path = path.Next;
                                    mPathPoints.Push(path.Position);
                                }
                            }
                        }
                    }
                }
                if (TryPopNextPoint(out var pos))
                {
                    this.moveAI.FindPath(pos);
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
            protected override void OnStop()
            {
            }

            override protected void OnUpdate()
            {
                if (target == null)
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                    if (holdTime.Update(zone.UpdateIntervalMS))
                    {
                        unit.DoSomething();
                    }
                }
                else
                {
                    var result = this.moveAI.Update();
                    if (result.HasFlag(MoveResult.MOVE_RESULT_BLOCK_OBJ))
                    {
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, unit.BodyBlockSize, target.X, target.Y))
                        if (Collider.Intersects(unit.Position, target.Position, unit.BodyBlockSize))
                        {
                            do_stop();
                        }
                        //else if (result.touched != null && CMath.includeRoundPoint(result.touched.X, result.touched.Y, result.touched.RadiusSize, target.X, target.Y))
                        else if (result.touched != null && Collider.Intersects(result.touched.Position, target.Position, result.touched.BodySize))
                        {
                            do_stop();
                        }
                    }
                    else if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                    {
                        if (TryPopNextPoint(out var pos))
                        {
                            this.moveAI.FindPath(pos);
                        }
                        else
                        {
                            do_stop();
                        }
                    }
                    else if (result.HasFlag(MoveResult.MOVE_RESULT_MIN_STEP) && result.HasFlag(MoveResult.MOVE_RESULT_NO_WAY))
                    {
                        if (TryPopNextPoint(out var pos))
                        {
                            this.moveAI.FindPath(pos);
                        }
                        else
                        {
                            do_stop();
                        }
                    }
                }
            }

            private void do_stop()
            {
                target = null;
            }

            private bool TryPopNextPoint(out Vector3 v3)
            {
                return mPathPoints.TryPop(out v3);
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
                        var dis = Vector3.DistanceSquared(pt.Position, pos);
                        if (dis < min)
                        {
                            min = dis;
                            targetPt = pt;
                        }
                    }
                }
                return targetPt;
            }
            class TargetPos : IPositionObject
            {
                public IZone Zone => Parent;
                public bool Enable => true;
                public EditorScene Parent { get; }
                public float Direction { get; }
                public float BodySize { get; }
                public float BodyHeight { get => 0; }
                public float X { get => Position.X; }
                public float Y { get => Position.Y; }
                public float Z { get => Position.Z; }
                public Geometry.Vector3 Position { get; private set; }
                public ITerrainLayer CurrentLayer { get; private set; }
                public VoxelCylinder VoxelBody { get; private set; }
                public TargetPos(InstanceZone zone, Geometry.Vector3 pos)
                {
                    this.Parent = zone as EditorScene;
                    this.Position = pos;
                    this.CurrentLayer = zone.Terrain3D.GetVoxelLayerByPos(pos);
                    this.VoxelBody = new VoxelCylinder(pos, BodySize, 1f);
                }
            }
        }
        //--------------------------------------------------------------------------------------
        public class StateFollowTarget : StateFollowAndGuard, IStateNoneControllable
        {
            public InstanceUnit Player { get => unit as InstanceUnit; }
            public UnitFollowTargetAction Action { get; private set; }
            public StateFollowTarget(InstanceUnit unit, InstanceUnit target, UnitFollowTargetAction act)
                : base(unit, target, act.minDistance, act.maxDistance)
            {
                this.Action = act;
                this.Action.Retain();
                this.StartMoveHoldTimeMS = zone.CFG.AI_FOLLOW_AND_ATTACK_HOLD_TIME_MS;
                TargetUnit.OnTransport += Player_OnTransport;
            }
            protected override void Disposing()
            {
                Action?.Release();
                Action = null;
                TargetUnit.OnTransport -= Player_OnTransport;
            }

            private void Player_OnTransport(InstanceUnit obj, Geometry.Vector3 oldpos)
            {
                var destPos = obj.Parent.FindNearRandomMoveablePos(obj.Position, Action.minDistance);
                if (destPos != null)
                {
                    this.unit.Transport(destPos.Value);
                }
                this.unit.FaceTo(Target.X, Target.Y);
                this.unit.StartJump(0);
            }
            protected override bool onChangedToIdle(IPositionObject target)
            {
                if (Action.tpDistance > 0)
                {
                    if (Target.Distance(this.unit) > Action.tpDistance)
                    {
                        this.unit.Transport(target.Position);
                        this.unit.FaceTo(target.X, target.Y);
                        this.unit.StartJump(0);
                        return true;
                    }
                }
                return base.onChangedToIdle(target);
            }
            protected override void onChangedToHold(IPositionObject target)
            {
                base.onChangedToHold(target);
                if (Action.autoAttack)
                {
                    Player.DoSomething();
                }
            }
            protected override bool CheckTargetInMaxRange()
            {
                if (TargetUnit.Enable == false)
                {
                    return Collider.Intersects(unit.Position, Target.Position, 0.1f);
                }
                return base.CheckTargetInMaxRange();
            }
            protected override bool CheckTargetInMinRange()
            {
                if (TargetUnit.Enable == false)
                {
                    return Collider.Intersects(unit.Position, Target.Position, 0.01f);
                }
                return base.CheckTargetInMinRange();
            }

            public State AsState()
            {
                return this;
            }
        }

        //--------------------------------------------------------------------------------------

        public class StateWait : State
        {
            private readonly TimeExpire mIdleTime;
            public bool IsDone { get => mIdleTime.IsEnd; }
            public StateWait(InstanceUnit unit, float timeMS) : base(unit)
            {
                mIdleTime = new TimeExpire(timeMS);
            }
            protected override void Disposing()
            {

            }
            override public bool OnBlock(State new_state)
            {
                return true;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Idle);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }
            override protected void OnStop()
            {
            }
            override protected void OnUpdate()
            {
                unit.SetActionStatus(UnitActionStatus.Idle);
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
                if (mIdleTime.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
            }
        }

        //--------------------------------------------------------------------------------------
    }
}
