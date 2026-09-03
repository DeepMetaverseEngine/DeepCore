using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    public abstract class UnitStateAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("状态执行后")]
        public AbstractAction OnStateDone;
        protected override void GetEndText(EventStringBuilder sw)
        {
            if (!OnStateDone.IsNullOrEmpty())
            {
                sw.Append("\n状态完成后:").Append(OnStateDone);
            }
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api, args) is InstanceUnit unit)
            {
                var state = this.RunState(unit, api, args);
                if (state != null)
                {
                    state.OnStopOnce += ((unit, st) =>
                    {
                        OnStateDone?.Invoke(api, args);
                    });
                }
                else
                {
                    OnStateDone?.Invoke(api, args);
                }
            }
            return null;
        }
        protected abstract InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args);
    }



    [Desc("控制单位移动", "[游戏]/单位/单位控制")]
    public class ControlUnitMoveAction : UnitStateAction
    {
        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})移动到{1};", Unit, Pos);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Pos.GetValueAs(api, args);
            if (pos.HasValue && api.ZoneAPI.Terrain3D.TryGetVoxelLayerByPos(pos.Value, out var layer))
            {
                var state = StateMoveAI.Alloc(unit, pos.Value);
                if (unit.ChangeState(state))
                {
                    return state;
                }
            }
            return null;
        }
    }

    [Desc("控制单位释放技能", "[游戏]/单位/单位控制")]
    public class ControlUnitLaunchSkillAction : UnitStateAction
    {
        [Desc("随机技能，如果为True，则SkillTemplateID无效")]
        public AbstractValue<bool> RandomSkill = new BooleanValue.VALUE();
        [Desc("无法释放指定技能时是否随便放个技能")]
        public AbstractValue<bool> SkillForAll = new BooleanValue.VALUE();
        [TemplateIDAttribute(typeof(SkillTemplate))]
        [Desc("技能模板ID")]
        public int SkillTemplateID;
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})释放技能{1};", Unit, SkillTemplateID);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var state = null as EquipSkill;
            var target = Target.GetValueAs(api, args);
            var targetID = target == null ? 0 : target.ID;
            var param = new InstanceUnit.TLaunchSkillParam()
            {
                TargetUnitID = targetID
            };
            if (RandomSkill.GetValueAs(api, args))
            {
                state = unit.LaunchRandomSkillForAll(param);
            }
            else
            {
                state = unit.LaunchSkill(SkillTemplateID, param);
            }
            if (state == null && SkillForAll.GetValueAs(api, args))
            {
                state = unit.LaunchRandomSkillForAll(param);
            }
            if (state != null)
            {
                return unit.NextState as StateSkill;
            }
            return null;
        }
    }

    [Desc("单位定义动作", "[游戏]/单位/单位控制")]
    public class UnitStateDefinedAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("主状态")] public UnitActionStatus MainStatus = UnitActionStatus.Idle;
        [Desc("子状态")] public AbstractValue<string> SubStatus = new StringValue.VALUE("");

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})单位定义动作({1}:{2});", Unit, MainStatus, SubStatus);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.ChangeState(InstanceUnit.StateDefinedAction.Alloc(unit, MainStatus, SubStatus?.GetValueAs(api, args)));
            }
            return unit;
        }
    }

    [Desc("控制单位状态", "[游戏]/单位/单位控制")]
    public class ControlUnitIdleAction : UnitStateAction
    {
        [Desc("待机时间")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();
        [Desc("强制无法被中断")]
        public AbstractValue<bool> Force = new BooleanValue.VALUE(false);
        [Desc("待机动作")]
        public UnitActionStatus ActionStatus = UnitActionStatus.Idle;
        [Desc("待机子动作")]
        public string SubState;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})状态{1}{2}秒;", Unit, ActionStatus, TimeSEC);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var state = StateIdleTime.Alloc(unit, 
                (float)TimeSEC.GetValueAs(api, args), 
                Force.GetValueAs(api,args),
                ActionStatus,
                SubState);
            if (unit.ChangeState(state))
            {
                return state;
            }
            return null;
        }
    }

    [Desc("控制单位做特定动作", "[游戏]/单位/单位控制")]
    public class ControUnitDoClientAction : UnitStateAction
    {
        [Desc("动作状态")]
        public UnitActionStatus ActionStatus = UnitActionStatus.ClientCustom;
        [Desc("子状态")]
        public string Sub;
        [ResourceID(ResourceType.Animation)]
        [Desc("动作名字")]
        public string ActionName;
        [Desc("动作持续时间(秒)")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})做特定动作{1};", Unit, ActionName);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var state = StateClientAction.Alloc(unit, ActionStatus, Sub, ActionName, (float)TimeSEC.GetValueAs(api, args));
            if (unit.ChangeState(state))
            {
                return state;
            }
            return null;
        }
    }


    [Desc("控制单位改变朝向", "[游戏]/单位/单位控制")]
    public class ControUnitFaceToAction : UnitStateAction
    {
        [Desc("方向")]
        public AbstractValue<double> Direction = new RealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})转向到{1};", Unit, Direction);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            unit.FaceTo((float)Direction.GetValueAs(api, args));
            return null;
        }
    }

    [Desc("控制单位攻击目标", "[游戏]/单位/单位控制")]
    public class ControUnitFocuseAttackAction : UnitStateAction
    {
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制({0})开始攻击{1};", Unit, Target);
        }
        protected override InstanceUnit.State RunState(InstanceUnit unit, IEventTriggerAdapter api, EventArguments args)
        {
            var target = Target.GetValueAs(api, args);
            if (target != null)
            {
                var state = StateFollowAndAttack.Alloc(unit, target);
                if (unit.ChangeState(state))
                {
                    return state;
                }
            }
            return null;
        }
    }
}

