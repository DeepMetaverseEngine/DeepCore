using DeepCore.EventTrigger.Data;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Host.EventTrigger.UI;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public class UnitManualAttackComponent : UnitComponent
    {
        public bool IsLaunchAnyway { get; set; } = false;
        public bool IsFaceToTarget { get; set; } = true;
        public EquipSkill CurrentSkill { get; private set; }
        public InstanceUnit TargetUnit { get; set; }
        public Vector3? TargetPos { get; set; }
        public float RandomRadius { get; set; }

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
            using (var list = Zone.ObjectPool.AllocList<EquipSkill>())
            {
                Owner.GetAvailableSkills(list);
                foreach (var skill in list)
                {
                    if (skill.CheckAutoLaunch())
                    {
                        if (TargetUnit != null && TargetUnit.IsActive)
                        {
                            if (LaunchSkill(skill))
                            {
                                FaceToTarget(skill, TargetUnit.Position);
                                return true;
                            }
                        }
                        if (TargetPos != null)
                        {
                            if (LaunchSkill(skill))
                            {
                                FaceToTarget(skill, TargetPos.Value);
                                return true;
                            }
                        }
                    }
                }
            }
            if (IsLaunchAnyway)
            {
                if (LaunchRandomSkill())
                {
                    return true;
                }
            }
            return false;
        }

        public virtual TLaunchSkillParam ToSkillParam()
        {
            if (TargetUnit != null && TargetUnit.IsActive)
            {
                return new InstanceUnit.TLaunchSkillParam()
                {
                    TargetUnitID = TargetUnit.ObjectID,
                };
            }
            if (TargetPos.HasValue)
            {
                var p = TargetPos.Value;
                CMath.RandomPosInRound(Owner.RandomN, p, RandomRadius, out p);
                return new InstanceUnit.TLaunchSkillParam()
                {
                    SpellTargetPos = p,
                };
            }
            return new TLaunchSkillParam();
        }

        public virtual bool LaunchSkill(EquipSkill st)
        {
            if (Owner.LaunchSkill(st.ID, ToSkillParam()) != null)
            {
                CurrentSkill = st;
                return true;
            }
            return false;
        }
        public virtual bool LaunchRandomSkill()
        {
            var st = Owner.LaunchRandomSkillForAll(ToSkillParam());
            if (st != null)
            {
                CurrentSkill = st;
                return true;
            }
            return false;
        }

        public virtual void FaceToTarget(EquipSkill st, in Geometry.Vector3 target)
        {
            if (target != null && IsFaceToTarget)
            {
                Owner.FaceTo(target.X, target.Y);
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region EventAPI

        [Desc("开关手动释放技能", "[游戏]/单位/[组件]/手动释放技能")]
        public class UnitManualLaunchSkillAction : ZoneAbstractAction
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
                        var comp = unit.Components.GetOrAddComponentAs<UnitManualAttackComponent>();
                        comp.IsLaunchAnyway = this.LaunchAnyway.GetValueAs(api, args);
                        comp.IsFaceToTarget = this.FaceToTarget.GetValueAs(api, args);
                    }
                    else
                    {
                        unit.Components.RemoveComponentAs<UnitManualAttackComponent>();
                    }

                }
                return null;
            }
        }

        [Desc("设置手动释放技能坐标", "[游戏]/单位/[组件]/手动释放技能")]
        public class SetUnitManualTarget : ZoneAbstractAction
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
            [Desc("坐标")]
            public AbstractValue<Vector3?> TargetPos = new RaycastHitPos();
            [Desc("散射半径")]
            public AbstractValue<double> RandomRadius = new ZoneRealValue.VALUE(0);

            protected override object Run(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs(api, args);
                if (unit != null && unit.Components.TryGetComponentAs<UnitManualAttackComponent>(out var comp))
                {
                    comp.TargetPos = TargetPos.GetValueAs(api, args);
                    comp.RandomRadius = (float)RandomRadius.GetValueAs(api, args);
                }
                return null;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------
 

    }
}
