using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("手动控制", "[游戏]/单位/手动控制")]
    public abstract class ManualUnitAction : ZoneAbstractAction
    {
    }

    [Desc("手动控制单位待机", "[游戏]/单位/手动控制")]
    public class ManualUnitIdleAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [Desc("待机时间")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})待机{1}秒;", Unit, TimeSEC);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            if (unit != null)
            {
                unit.QueueIdle((float)TimeSEC.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("手动控制单位做动作", "[游戏]/单位/手动控制")]
    public class ManualUnitDoClientAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [ResourceID(ResourceType.Animation)]
        [Desc("动作名字")]
        public string ActionName;

        [Desc("动作持续时间(秒)")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})做动作{1};", Unit, ActionName);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            if (unit != null)
            {
                unit.QueueDoAction((float)TimeSEC.GetValueAs(api, args), ActionName);
            }
            return null;
        }
    }

    [Desc("手动控制单位移动", "[游戏]/单位/手动控制")]
    public class ManualUnitMoveAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})移动到{1};", Unit, Pos);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            var pos = Pos.GetValueAs(api, args);
            if (unit != null && pos != null)
            {
                unit.StartMoveTo(pos.Value);
            }
            return null;
        }
    }

    [Desc("手动控制单位改变朝向", "[游戏]/单位/手动控制")]
    public class ManualUnitFaceToAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [Desc("方向")]
        public AbstractValue<double> Direction = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})转向到{1};", Unit, Direction);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.FaceTo((float)Direction.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("手动控制单位释放技能", "[游戏]/单位/手动控制")]
    public class ManualUnitLaunchSkillAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [Desc("随机技能，如果为True，则SkillTemplateID无效")]
        public AbstractValue<bool> RandomSkill = new BooleanValue.VALUE();

        [TemplateIDAttribute(typeof(SkillTemplate))]
        [Desc("技能模板ID")]
        public int SkillTemplateID;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})释放技能{1};", Unit, SkillTemplateID);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            if (unit != null)
            {
                unit.LaunchSkill(SkillTemplateID, new TLaunchSkillParam());
            }
            return null;
        }
    }

    [Desc("手动控制开始攻击单位", "[游戏]/单位/手动控制")]
    public class ManualUnitFocuseAttackAction : ManualUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        [Desc("目标")]
        public AbstractValue<InstanceUnit> Target = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})开始攻击单位{1};", Unit, Target);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            var target = Target.GetValueAs(api, args);
            if (unit != null && target != null)
            {
                unit.FocuseAttack(target);
            }
            return null;
        }
    }




    [Desc("手动控制等待下一条指令(仅队列中有效)", "[游戏]/单位/手动控制")]
    public class ManualUnitWaitCommandAction : ManualUnitAction
    {
        [Desc("等待时间")]
        public AbstractValue<double> WaitTimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制等待指令{0}秒;", WaitTimeSEC);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            return null;
        }
    }

    [Desc("手动控制一系列动作", "[游戏]/单位/手动控制")]
    public class ManualUnitControlQueue : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        [Desc("动作序列")]
        [ListDescAttribute(typeof(ManualUnitAction))]
        public List<ManualUnitAction> Actions = new List<ManualUnitAction>();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("手动控制({0})执行序列", Unit).AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Actions);
            sw.IndentEnd("}");
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceManual;
            if (unit != null && Actions.Count > 0)
            {
                new ActionQueueExecuter(api, args, unit, Actions).doNextAction();
            }
            return null;
        }

        class ActionQueueExecuter
        {
            private IEventTriggerAdapter api;
            private EventArguments args;
            private InstanceManual unit;
            private Queue<ManualUnitAction> actions;

            public ActionQueueExecuter(IEventTriggerAdapter api, EventArguments args, InstanceManual manual, List<ManualUnitAction> actions)
            {
                this.api = api;
                this.args = args;
                this.unit = manual;
                this.actions = new Queue<ManualUnitAction>(actions);
            }

            public void doNextAction()
            {
                if (actions.Count > 0)
                {
                    ManualUnitAction act = actions.Dequeue();
                    if (act is ManualUnitIdleAction)
                    {
                        doIdle(act as ManualUnitIdleAction);
                    }
                    else if (act is ManualUnitMoveAction)
                    {
                        doMove(act as ManualUnitMoveAction);
                    }
                    else if (act is ManualUnitFaceToAction)
                    {
                        doFaceTo(act as ManualUnitFaceToAction);
                    }
                    else if (act is ManualUnitLaunchSkillAction)
                    {
                        doLaunchSkill(act as ManualUnitLaunchSkillAction);
                    }
                    else if (act is ManualUnitDoClientAction)
                    {
                        doClientAction(act as ManualUnitDoClientAction);
                    }
                    else if (act is ManualUnitFocuseAttackAction)
                    {
                        doAttackAction(act as ManualUnitFocuseAttackAction);
                    }
                    else if (act is ManualUnitWaitCommandAction)
                    {
                        doWaitCommand(act as ManualUnitWaitCommandAction);
                    }
                }
            }

            private void doIdle(ManualUnitIdleAction idle)
            {
                unit.QueueIdle((float)idle.TimeSEC.GetValueAs(api, args), (z,m) =>
                {
                    doNextAction();
                });
            }

            private void doMove(ManualUnitMoveAction move)
            {
                var pos = move.Pos.GetValueAs(api, args);
                if (pos != null)
                {
                    unit.QueueMoveTo(pos.Value, (z, m) =>
                    {
                        doNextAction();
                    });
                }
                else
                {
                    doNextAction();
                }
            }

            private void doFaceTo(ManualUnitFaceToAction faceTo)
            {
                unit.FaceTo((float)faceTo.Direction.GetValueAs(api, args));
                doNextAction();
            }

            private void doLaunchSkill(ManualUnitLaunchSkillAction skill)
            {
                unit.QueueLaunchSkill(skill.SkillTemplateID, skill.RandomSkill.GetValueAs(api, args), (z, m) =>
                {
                    doNextAction();
                });
            }

            private void doClientAction(ManualUnitDoClientAction skill)
            {
                unit.QueueDoAction((float)skill.TimeSEC.GetValueAs(api, args), skill.ActionName, (z, m) =>
                {
                    doNextAction();
                });
            }

            private void doAttackAction(ManualUnitFocuseAttackAction act)
            {
                var targget = act.Target.GetValueAs(api, args);
                if (targget != null)
                {
                    unit.FocuseAttack(targget);
                }
                doNextAction();
            }

            private void doWaitCommand(ManualUnitWaitCommandAction act)
            {
                unit.Wait((float)act.WaitTimeSEC.GetValueAs(api, args), () => { doNextAction(); });
            }
        }

    }
}
