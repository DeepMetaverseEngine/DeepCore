using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    //--------------------------------------------------------------------------------

    //--------------------------------------------------------------------------------


    //--------------------------------------------------------------------------------
    #region __强制动作__





    [Desc("强制动作", "[游戏]/单位/-强制动作")]
    public abstract class ForceUnitAction : ZoneAbstractAction
    {
    }

    [Desc("强制单位待机", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitIdleAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("待机时间")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("强制动作({0})待机{1}秒;", Unit, TimeSEC);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.QueueUnitAction(api, args, this);
                // unit.queueCurrentState(new DeepCore.GameHost.Instance.InstanceUnit.ForceStateIdleTime(unit, TimeSEC.GetValueAs(api, args)));
            }
            return unit;
        }
    }

    [Desc("强制单位做动作", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitDoAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [ResourceID(ResourceType.Animation)]
        [Desc("动作名字")]
        public string ActionName;

        [Desc("动作持续时间(秒)")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("强制动作({0})做动作{1};", Unit, ActionName);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                // unit.queueCurrentState(new DeepCore.GameHost.Instance.InstanceUnit.ForceStateActionTime(unit, TimeSEC.GetValueAs(api, args), ActionName));
                unit.QueueUnitAction(api, args, this);
            }
            return unit;
        }
    }

    [Desc("强制单位移动", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitMoveAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("位置")]
        public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("强制动作({0})移动到{1};", Unit, Pos);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceUnit;
            var pos = Pos.GetValueAs(api, args);
            if (unit != null && pos != null)
            {
                unit.QueueUnitAction(api, args, this);
                //unit.queueCurrentState(new DeepCore.GameHost.Instance.InstanceUnit.ForceStateMoveTo(unit, pos.x, pos.y));
            }
            return unit;
        }
    }
    
    
    [Desc("强制单位指定朝向移动并在指定区域内停止", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitDirMoveAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("方向(角度)")]
        public float Angle = -90;
        [Desc("移动停止区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat($"{Unit}强制朝向{Angle}移动直到{Region}");
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                unit.QueueUnitAction(api, args, this);
                //unit.queueCurrentState(new DeepCore.GameHost.Instance.InstanceUnit.ForceStateMoveTo(unit, pos.x, pos.y));
            }
            return unit;
        }
    }

    [Desc("强制单位改变朝向", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitFaceToAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("方向")]
        public AbstractValue<double> Direction = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("强制动作({0})转向到{1};", Unit, Direction);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                unit.QueueUnitAction(api, args, this);
                //unit.FaceTo(Direction.GetValueAs(api, args));
            }
            return unit;
        }
    }

    [Desc("强制单位释放技能", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitLaunchSkillAction : ForceUnitAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("随机技能，如果为True，则SkillTemplateID无效")]
        public AbstractValue<bool> RandomSkill = new BooleanValue.VALUE();

        [TemplateIDAttribute(typeof(SkillTemplate))]
        [Desc("技能模板ID")]
        public int SkillTemplateID;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("强制动作({0})释放技能{1};", Unit, SkillTemplateID);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                unit.QueueUnitAction(api, args, this);
                //unit.queueCurrentState(new DeepCore.GameHost.Instance.InstanceUnit.ForceStateLaunchSkill(unit, SkillTemplateID, RandomSkill.GetValueAs(api, args)));
            }
            return unit;
        }
    }

    [Desc("强制一系列动作", "[游戏]/单位/剧情-强制动作")]
    public class ForceUnitControlQueue : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("动作序列")]
        [ListDescAttribute(typeof(ForceUnitAction))]
        public List<ForceUnitAction> Actions = new List<ForceUnitAction>();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})强制执行序列", Unit).AppendLine();
            sw.IndentBegin("{");
            sw.AppendLine(Actions);
            sw.IndentEnd("}");
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null && Actions.Count > 0)
            {
                new ActionQueueExecuter(api, args, unit, Actions).doNextAction();
            }
            return unit;
        }

        class ActionQueueExecuter
        {
            private IEventTriggerAdapter api;
            private EventArguments args;
            private InstanceUnit unit;
            private Queue<ForceUnitAction> actions;

            public ActionQueueExecuter(IEventTriggerAdapter api, EventArguments args, InstanceUnit manual, List<ForceUnitAction> actions)
            {
                this.api = api;
                this.args = args;
                this.unit = manual;
                this.actions = new Queue<ForceUnitAction>(actions);
            }

            public void doNextAction()
            {
                if (actions.Count > 0)
                {
                    ForceUnitAction act = actions.Dequeue();
                    unit.QueueUnitAction(api, args, act, () => { doNextAction(); });
                    //                     if (act is ForceUnitIdleAction)
                    //                     {
                    //                         doIdle(act as ForceUnitIdleAction);
                    //                     }
                    //                     else if (act is ForceUnitMoveAction)
                    //                     {
                    //                         doMove(act as ForceUnitMoveAction);
                    //                     }
                    //                     else if (act is ForceUnitFaceToAction)
                    //                     {
                    //                         doFaceTo(act as ForceUnitFaceToAction);
                    //                     }
                    //                     else if (act is ForceUnitLaunchSkillAction)
                    //                     {
                    //                         doLaunchSkill(act as ForceUnitLaunchSkillAction);
                    //                     }
                    //                     else if (act is ForceUnitDoAction)
                    //                     {
                    //                         doClientAction(act as ForceUnitDoAction);
                    //                     }
                }
            }
            // 
            //             private void doIdle(ForceUnitIdleAction idle)
            //             {
            //                 //                 InstanceUnit.State state = new InstanceUnit.ForceStateIdleTime(unit, idle.TimeSEC.GetValueAs(api, args));
            //                 //                 state.AddStopOnce((InstanceUnit u, InstanceUnit.State os) =>
            //                 //                {
            //                 //                    doNextAction();
            //                 //                });
            //                 //                 unit.queueCurrentState(state);
            //                 unit.QueueUnitAction(api, args, idle, ()=> { doNextAction(); });
            // 
            //             }
            // 
            //             private void doMove(ForceUnitMoveAction move)
            //             {
            //                 unit.QueueUnitAction(api, args, move, () => { doNextAction(); });
            //                 //                 Vector2 pos = move.Pos.GetValueAs(api, args);
            //                 //                 if (pos != null)
            //                 //                 {
            //                 //                     InstanceUnit.State state = new InstanceUnit.ForceStateMoveTo(unit, pos.x, pos.y);
            //                 //                     state.AddStopOnce((InstanceUnit u, InstanceUnit.State os) =>
            //                 //                    {
            //                 //                        doNextAction();
            //                 //                    });
            //                 //                     unit.queueCurrentState(state);
            //                 //                 }
            //                 //                 else
            //                 //                 {
            //                 //                     doNextAction();
            //                 //                 }
            //             }
            // 
            //             private void doFaceTo(ForceUnitFaceToAction faceTo)
            //             {
            //                 unit.faceTo(faceTo.Direction.GetValueAs(api, args));
            //                 doNextAction();
            //             }
            // 
            //             private void doLaunchSkill(ForceUnitLaunchSkillAction skill)
            //             {
            //                 InstanceUnit.State state = new InstanceUnit.ForceStateLaunchSkill(
            //                     unit,
            //                     skill.SkillTemplateID,
            //                     skill.RandomSkill.GetValueAs(api, args),
            //                     (InstanceUnit u, InstanceUnit.State os) =>
            //                 {
            //                     doNextAction();
            //                 });
            //                 unit.queueCurrentState(state);
            //             }
            // 
            //             private void doClientAction(ForceUnitDoAction skill)
            //             {
            //                 InstanceUnit.State state = new InstanceUnit.ForceStateActionTime(unit, skill.TimeSEC.GetValueAs(api, args), skill.ActionName);
            //                 state.AddStopOnce((InstanceUnit u, InstanceUnit.State os) =>
            //                {
            //                    doNextAction();
            //                });
            //                 unit.queueCurrentState(state);
            //             }

        }

    }

    #endregion

    //--------------------------------------------------------------------------------

}

