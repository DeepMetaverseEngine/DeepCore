using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;


//----------------------------------------------------------------------------------------------------------------------------------------

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("单位朝目标点寻路移动", "[游戏]/单位/[状态机]")]
    public class StateMoveAIAction : UnitStateAction
    {
        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})寻路移动到{1};", Unit, Pos);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Pos.GetValueAs(api, args);
            if (pos.HasValue && api.ZoneAPI.Terrain3D.TryGetVoxelLayerByPos(pos.Value, out var layer))
            {
                if (unit.StartMoveAI(pos.Value))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }
    [Desc("尝试换个位置，攻击间歇避免怪物堆在一个点", "[游戏]/单位/[状态机]")]
    public class TryMoveScatterTargetAction : UnitStateAction
    {
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})尝试换个({1})攻击位置;", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var target = Target.GetValueAs(api, args);
            if (target != null)
            {
                if (unit.StartMoveScatterTarget(target))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }


    [Desc("在一定范围内浪", "[游戏]/单位/[状态机]")]
    public class StateIdleMoveAction : UnitStateAction
    {
        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();
        [Desc("浪多远")]
        public AbstractValue<double> Range = new ZoneRealValue.VALUE(10);
        [Desc("浪多久（毫秒）")]
        public AbstractValue<double> TimeMS = new IntegerValue.VALUE(3000);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})在{1}范围{2}内浪{3}毫秒;", Unit, Pos, Range, TimeMS);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Pos.GetValueAs(api, args);
            if (pos.HasValue && api.ZoneAPI.Terrain3D.TryGetVoxelLayerByPos(pos.Value, out var layer))
            {
                // var state = StateIdleMove.Alloc(unit, pos.Value, TimeMS.GetValueAs(api, args), Range.GetValueAs(api, args));
                if (unit.StartIdleMove(pos.Value, (int)TimeMS.GetValueAs(api, args), (float)Range.GetValueAs(api, args)))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }



    [Desc("跟随并保护单位", "[游戏]/单位/[状态机]")]
    public class StateFollowAndGuardAction : UnitStateAction
    {
        [Desc("跟随目标")]
        public AbstractValue<InstanceUnit> VIP = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})跟随并保护({1});", Unit, VIP);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            if (unit.AGuard)
            {
                var vip = VIP.GetValueAs(api, args);
                if (vip != null && vip.IsActive)
                {
                    if (unit.StartGuardUnit(vip))
                    {
                        return unit.NextState;
                    }
                }
            }
            return null;
        }
    }
    [Desc("跟随并攻击目标", "[游戏]/单位/[状态机]")]
    public class StateFollowAndAttackAction : UnitStateAction
    {
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Trigging();
        [Desc("目标类型")]
        public SkillTemplate.CastTarget CastTarget = SkillTemplate.CastTarget.Enemy;
        [Desc("攻击原因")]
        public AttackReason Reason = AttackReason.Attack;
        [Desc("优先技能（可选）")]
        public AbstractValue<SkillTemplate> ExpectSkill;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})跟随并攻击({1});", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            if (unit.AGuard)
            {
                var target = Target.GetValueAs(api, args);
                if (target != null && target.IsActive)
                {
                    var equipSkill = default(InstanceUnit.EquipSkill);
                    if (ExpectSkill != null && ExpectSkill.GetValueAs(api, args) is SkillTemplate sk)
                    {
                        equipSkill = unit.GetSkillState(sk.ID);
                    }
                    //var state = FollowAndAttack(unit, target, this.CastTarget, this.Reason, equipSkill);
                    if (unit.StartFollowAndAttack(target, this.Reason, this.CastTarget, equipSkill))
                    {
                        return unit.NextState;
                    }
                }
            }
            return null;
        }
    }


    [Desc("跟随并和目标交互", "[游戏]/单位/[状态机]")]
    public class StateFollowAndPickObjectAction : UnitStateAction
    {
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Trigging();
        [Desc("交互读条时间")]
        public AbstractValue<double> TimeMS = new IntegerValue.VALUE(1000);
        [Desc("完成交互后动作")]
        public AbstractAction OnPickDone = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})跟随并和({1})交互;", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var target = Target.GetValueAs(api, args);
            if (target != null && target.IsActive)
            {
                //                         var state = StateFollowAndPickObject.Alloc(unit, target, TimeMS.GetValueAs(api, args),
                //                             (unit, target, state) =>
                //                             {
                //                                 OnPickDone?.DoAction(api, args);
                //                                 return true;
                //                             }, OnPickDone);
                if (unit.StartFollowAndPickObject(target, (int)TimeMS.GetValueAs(api, args), (unit, cancel, target, state) =>
                    {
                        var arg2 = args;
                        arg2.TriggingUnit = unit;
                        arg2.TriggingCounterPart = target as InstanceUnit;
                        arg2.TriggingItem = target as InstanceItem;
                        OnPickDone?.Invoke(api, arg2);
                        return true;
                    }, OnPickDone))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("目标AS物品")] public InstanceItem TargetItem(EventArguments args) => args.TriggingItem;
        [TriggingArg("目标AS单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }



    [Desc("跟随并捡取道具", "[游戏]/单位/[状态机]")]
    public class StateFollowAndPickItemAction : UnitStateAction
    {
        [Desc("目标")]
        public AbstractValue<InstanceItem> Target = new ItemValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})跟随并捡取({1});", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var target = Target.GetValueAs(api, args);
            if (target != null)
            {
                //var state = StateFollowAndPickItem.Alloc(unit, target);
                if (unit.StartFollowAndPickItem(target))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }




    [Desc("沿路径A过去", "[游戏]/单位/[状态机]")]
    public class StateAttackToZoneWayPointAction : UnitStateAction
    {
        [Desc("目标路点")]
        public AbstractValue<InstanceFlag> WayPoint = new FlagValue.EditorPoint();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})沿路径({1})A过去;", Unit, WayPoint);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = WayPoint.GetValueAs(api, args);
            if (pos is ZoneWayPoint wp)
            {
                //var StateRunningPath = StateAttackToZoneWayPoint.Alloc(unit, wp);
                if (unit.StartAttackTo(wp))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }



    [Desc("在位置内警戒", "[游戏]/单位/[状态机]")]
    public class StateGuardInPositionAction : UnitStateAction
    {
        [Desc("警戒点")]
        public AbstractValue<Vector3?> Position = new PositionValue.PositionOfUnit() { };
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})在({1})内警戒;", Unit, Position);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            if (pos.HasValue)
            {
                //var state = StateGuardInPosition.Alloc(unit, pos.Value);
                if (unit.StartGuardInPosition(pos))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }



    [Desc("立刻开始返回警戒点", "[游戏]/单位/[状态机]")]
    public class StateBackToPositionAction : UnitStateAction
    {
        [Desc("警戒点")]
        public AbstractValue<Vector3?> Position = new PositionValue.PositionOfUnit() { };
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})立刻开始返回({1});", Unit, Position);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            if (pos.HasValue)
            {
                //var state = StateBackToPosition.Alloc(unit, pos.Value);
                if (unit.StartBackToOrgin(pos))
                {
                    return unit.NextState;
                }
            }
            return null;
        }
    }

    [Desc("NPC向路点进发", "[游戏]/单位/NPC单位-AI")]
    public class NpcAttackToWayPoint : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("路点入口")]
        public AbstractValue<InstanceFlag> WayPoint = new FlagValue.EditorPoint();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("NPC:{0}向{1}进发;", Unit, WayPoint);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var pos = WayPoint.GetValueAs(api, args) as ZoneWayPoint;
            if (unit != null && pos != null)
            {
                unit.StartAttackTo(pos);
            }
            return unit;
        }
    }
    [Desc("NPC巡逻", "[游戏]/单位/NPC单位-AI")]
    public class NpcPatrolWithWayPoint : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("路点入口")]
        public AbstractValue<InstanceFlag> WayPoint = new FlagValue.EditorPoint();

        //         [Desc("切换路点待机最小时间(秒)")]
        //         public AbstractValue<double> HoldMinTimeSEC = new RealValue.VALUE(0);
        //         [Desc("切换路点待机最大时间(秒)")]
        //         public AbstractValue<double> HoldMaxTimeSEC = new RealValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("NPC:{0}在{1}巡逻;", Unit, WayPoint);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var pos = WayPoint.GetValueAs(api, args) as ZoneWayPoint;
            if (unit != null && pos != null)
            {
                //                 int holdMinTimeMS = (int)(HoldMinTimeSEC.GetValueAs(api, args) * 1000);
                //                 int holdMaxTimeMS = (int)(HoldMaxTimeSEC.GetValueAs(api, args) * 1000);
                unit.StartAttackTo(pos);
            }
            return unit;
        }
    }


    [Desc("NPC跟随并警戒", "[游戏]/单位/NPC单位-AI")]
    public class NpcFollowAndGuardUnit : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("跟随的单位")]
        public AbstractValue<InstanceUnit> VIP = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("NPC:{0}跟随{1};", Unit, VIP);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var vip = VIP.GetValueAs(api, args);
            if (unit != null && vip != null)
            {
                unit.StartGuardUnit(vip);
            }
            return unit;
        }
    }

    [Desc("NPC追踪并战斗", "[游戏]/单位/NPC单位-AI")]
    public class NpcFollowAndAttackUnit : ZoneAbstractAction
    {
        [Desc("NPC单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("目标单位")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Editor();

        [Desc("攻击原因")]
        public AttackReason Reason = AttackReason.Tracing;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("NPC:{0}攻击{1};", Unit, Target);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var target = Target.GetValueAs(api, args);
            if (unit != null && target != null)
            {
                unit.StartFollowAndAttack(target, Reason);
            }
            return unit;
        }
    }

    [Desc("优先大路点寻路再坐标寻路", "[游戏]/单位/[状态机]")]
    public class StateMoveFindPathWayPointAction : UnitStateAction
    {
        [Desc("目标路点")]
        public PositionValue Target = new PositionValue.CenterOfFlag()
        {
            Flag = new FlagValue.EditorPoint()
        };
        [Desc("路点搜索范围")]
        public AbstractValue<double> FindRange = new ZoneRealValue.VALUE(10);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})大路点寻路再坐标寻路到({1});", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            if (unit.Moveable)
            {
                var pos = Target.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    unit.ChangeState(InstanceUnit.StateMoveFindPathWayPoint.Alloc(unit, pos.Value, (float)FindRange.GetValueAs(api, args)));
                }
            }
            return null;
        }
    }
}
//----------------------------------------------------------------------------------------------------------------------------------------

