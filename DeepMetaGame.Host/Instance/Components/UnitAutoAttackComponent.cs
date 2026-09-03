using DeepCore.EventTrigger.Data;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Reflection;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitAutoAttackComponent : UnitComponent
    {
        public bool IsIgnoreAutoLaunch { get; set; } = false;
        public bool IsLaunchAnyway { get; set; } = false;
        public bool IsFaceToTarget { get; set; } = true;
        public bool IsLockTarget { get; set; } = false;

        public InstanceUnit CurrentTarget { get; protected set; }
        public EquipSkill CurrentSkill { get; protected set; }


        private TimeInterval checkTarget;
        protected override void OnAdded()
        {
            base.OnAdded();
            this.checkTarget = Owner.AllocTimeInterval(Owner.CFG.AI_VIEW_TRIGGER_CHECK_TIME_MS);
        }
        protected override void OnRemoved()
        {
            base.OnRemoved();
            this.checkTarget?.Dispose();
        }
        protected override void OnUpdate()
        {
            if (!Active) return;
            base.OnUpdate();
            if (!Owner.IsPaused && !Owner.IsNoneSkill)
            {
                if (!(Owner.CurrentState is StateSkill skill))
                {
                    TryAutoLaunchSkill();
                }
            }
            if (checkTarget.Update(Zone.UpdateIntervalMS))
            {
                if (CurrentTarget != null && !Owner.IsInGuardRange(CurrentTarget))
                {
                    CurrentTarget = null;
                }
            }
        }

        public virtual bool TryAutoLaunchSkill()
        {
            var checkAutoLaunch = !IsIgnoreAutoLaunch;
            using (var list = Zone.ObjectPool.AllocList<EquipSkill>())
            {
                Owner.GetAvailableSkills(list, checkAutoLaunch);
                Zone.RandomN.RandomList(list);
                foreach (var skill in list)
                {
                    if (skill.CheckAutoLaunch(checkAutoLaunch))
                    {
                        if (IsLockTarget)
                        {
                            if (CurrentTarget != null && CurrentTarget.IsActive)
                            {
                                if (LaunchSkill(skill, CurrentTarget))
                                {
                                    FaceToTarget(CurrentSkill, CurrentTarget);
                                    CurrentSkill = skill;
                                    return true;
                                }
                            }
                        }
                        var target = SelectTarget(skill);
                        if (target != null)
                        {
                            FaceToTarget(skill, target);
                            if (LaunchSkill(skill, target))
                            {
                                CurrentSkill = skill;
                                CurrentTarget = target;
                                return true;
                            }
                        }
                    }
                }
            }
            if (IsLaunchAnyway)
            {
                var st = Owner.LaunchRandomSkillForAll(new InstanceUnit.TLaunchSkillParam(), checkAutoLaunch);
                if (st != null)
                {
                    CurrentSkill = st;
                    return true;
                }
            }
            return false;
        }
        public virtual InstanceUnit SelectTarget(EquipSkill skill)
        {
            if (Owner.AGuard && Owner.AGuard.GuardRange > 0)
            {
                if (skill != null)
                {
                    if (Zone.SeekRangedTarget(Owner, skill, skill.Data.AttackRange) is InstanceUnit tgt)
                    {
                        return tgt;
                    }
                }
                //return Zone.SeekUnitGuardTarget(Owner, skill, Owner.AGuard);
            }
            return null;
        }

        public virtual bool LaunchSkill(EquipSkill st, InstanceUnit target)
        {
            FaceToTarget(st, target);
            if (Owner.LaunchSkill(st.ID, new TLaunchSkillParam(target.ID) { SpellTargetPos = target.WaistPosition }) != null)
            {
                return true;
            }
            return false;
        }

        public virtual void FaceToTarget(EquipSkill st, InstanceUnit target)
        {
            if (target != null && IsFaceToTarget)
            {
                Owner.FaceTo(target.X, target.Y);
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("开关自动释放技能", "[游戏]/单位/[组件]/自动射击")]
        public class UnitAutoLaunchSkillAction : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("开关")]
            public AbstractValue<bool> On = new ZoneBooleanValue.VALUE(true);
            [Desc("面朝目标")]
            public AbstractValue<bool> FaceToTarget = new ZoneBooleanValue.VALUE(true);
            [Desc("允许空放")]
            public AbstractValue<bool> LaunchAnyway = new ZoneBooleanValue.VALUE(false);
            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    if (On.GetValueAs(api, args))
                    {
                        var comp = unit.Components.GetOrAddComponentAs<UnitAutoAttackComponent>();
                        comp.IsLaunchAnyway = this.LaunchAnyway.GetValueAs(api, args);
                        comp.IsFaceToTarget = this.FaceToTarget.GetValueAs(api, args);
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitAutoAttackComponent>();
                    }

                }
                return null;

            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }

    public class UnitAutoShootComponent : UnitComponent
    {
        public bool IsIgnoreAutoLaunch { get; set; } = false;
        public EquipSkill CurrentSkill { get; protected set; }


        protected override void OnAdded()
        {
            base.OnAdded();
        }
        protected override void OnRemoved()
        {
            base.OnRemoved();
        }
        protected override void OnUpdate()
        {
            if (!Active) return;
            base.OnUpdate();
            if (!Owner.IsPaused && !Owner.IsNoneSkill)
            {
                if (!(Owner.CurrentState is StateSkill skill))
                {
                    TryAutoLaunchSkill();
                }
            }
        }

        public virtual bool TryAutoLaunchSkill()
        {
            var checkAutoLaunch = !IsIgnoreAutoLaunch;
            var st = Owner.LaunchRandomSkillForAll(new InstanceUnit.TLaunchSkillParam(), checkAutoLaunch);
            if (st != null)
            {
                CurrentSkill = st;
                return true;
            }
            return false;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("开关自动发射技能(无目标)", "[游戏]/单位/[组件]/自动射击")]
        public class UnitAutoShootlAction : ZoneAbstractAction
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
                        var comp = unit.Components.GetOrAddComponentAs<UnitAutoShootComponent>();
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitAutoShootComponent>();
                    }

                }
                return null;

            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
    }
}
