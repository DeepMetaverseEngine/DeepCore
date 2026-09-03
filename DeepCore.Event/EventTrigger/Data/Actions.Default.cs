using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using static DeepCore.Colors;
using static DeepCore.EventTrigger.Data.SwitchCaseInteger;

namespace DeepCore.EventTrigger.Data
{
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    [Desc("什么都不做", "[基础]")]
    public class DoNoting : AbstractAction
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("什么都不做;");
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            return null;
        }
    }
    [Desc("注释", "[基础]")]
    public class CommentAction : AbstractAction
    {
        [Desc("注释")] public string Comment = "注释";

        protected override void GetText(EventStringBuilder sw)
        {
            //"<![CDATA[]]>"
            sw.AppendFormat("<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Comment);
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            return null;
        }
    }
    [Desc("Print", "[基础]")]
    public class Print : AbstractAction
    {
        [Desc("Level")] public LoggerLevel Level = LoggerLevel.WARNNING;
        [Desc("Text")] public AbstractValue<string> Text = new StringValue.VALUE("Hellow world!");
        protected override void GetText(EventStringBuilder sw)
        {
            //"<![CDATA[]]>"
            sw.AppendFormat("Print:<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Text);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            api.Log.LogLevel(Level, Text.GetValueAs(api, args));
            return null;
        }
    }
    //-------------------------------------------------------------------
    [Desc("GC", "[基础]")]
    public class SystemGC : AbstractAction
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("GC;");
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            System.GC.Collect();
            return null;
        }
    }
    [Desc("模拟卡顿", "[基础]")]
    public class Wait : AbstractAction<double>
    {
        [Desc("延时毫秒")] public AbstractValue<double> DelayTimeMS = new IntegerValue.VALUE(1000);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("模拟卡顿{0}(毫秒);", DelayTimeMS).AppendLine();
        }
        protected override double RunAs(EventExecutor api, IEventArguments args)
        {
            int delayMS = (int)DelayTimeMS.GetValueAs(api, args);
            Task.Delay(delayMS).Wait();
            return delayMS;
        }
    }
    [Desc("延时执行一个动作", "[基础]")]
    public class DelayedAction : AbstractAction
    {
        [Desc("延时毫秒")] public AbstractValue<double> DelayTimeMS = new IntegerValue.VALUE(1000);

        [Desc("延时动作")] public AbstractAction DelayAction = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("延时{0}(毫秒);", DelayTimeMS).AppendLine();
            if (!DelayAction.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendLine(DelayAction);
                sw.IndentEnd("}");
            }
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            int delayMS = (int)DelayTimeMS.GetValueAs(api, args);
            api.AddTimeDelaySEC(args, delayMS / 1000f, (args2) =>
            {
                DelayAction?.Invoke(api, args2);
            });
            return null;
        }
    }
    //-------------------------------------------------------------------
    [Desc("条件执行(IF)", "[基础]")]
    public class ConditionAction : AbstractAction
    {
        [Desc("IF 条件")] public AbstractValue<bool> Condition = new BooleanValue.BooleanComparison();

        [Desc("THEN 动作")] public AbstractAction Action = new DoNoting();

        [Desc("ELSE 动作")] public AbstractAction ElseAction = null;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_KEYWORKD + "'>IF</c> ({0}) <c color='" + sw.COLOR_KEYWORKD + "'>THEN</c>", Condition).AppendLine();
            if (!Action.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendLine(Action);
                sw.IndentEnd("}");
            }
            if (!ElseAction.IsNullOrEmpty())
            {
                sw.AppendLine();
                sw.AppendLine("<c color='" + sw.COLOR_KEYWORKD + "'>ELSE</c>");
                sw.IndentBegin("{");
                sw.AppendLine(ElseAction);
                sw.IndentEnd("}");
            }
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            if (Condition.GetValueAs(api, args))
            {
                Action?.Invoke(api, args);
                return true;
            }
            else
            {
                ElseAction?.Invoke(api, args);
                return false;
            }
        }

    }

    [Desc("条件执行(IF NOT)", "[基础]")]
    public class ConditionNotAction : AbstractAction
    {
        [Desc("IF NOT 条件")] public AbstractValue<bool> NotCondition = new BooleanValue.BooleanComparison();

        [Desc("THEN 动作")] public AbstractAction Action = new DoNoting();

        [Desc("ELSE 动作")] public AbstractAction ElseAction = null;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_KEYWORKD + "'>IF NOT</c> ({0}) <c color='" + sw.COLOR_KEYWORKD + "'>THEN</c>", NotCondition).AppendLine();
            if (!Action.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendLine(Action);
                sw.IndentEnd("}");
            }
            if (!ElseAction.IsNullOrEmpty())
            {
                sw.AppendLine();
                sw.AppendLine("<c color='" + sw.COLOR_KEYWORKD + "'>ELSE</c>");
                sw.IndentBegin("{");
                sw.AppendLine(ElseAction);
                sw.IndentEnd("}");
            }
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            if (!NotCondition.GetValueAs(api, args))
            {
                Action?.Invoke(api, args);
                return true;
            }
            else
            {
                ElseAction?.Invoke(api, args);
                return false;
            }
        }
    }
    //-------------------------------------------------------------------

    [Desc("FOR (Int32)循环执行一个动作", "[基础]")]
    public class IteratorForInt32Action : AbstractAction
    {
        [Desc("起始值")] public AbstractValue<double> BeginIndex = new IntegerValue.VALUE(0);
        [Desc("递增")] public AbstractValue<double> Step = new IntegerValue.VALUE(1);
        [Desc("结束值(不包括)")] public AbstractValue<double> EndIndex = new IntegerValue.VALUE(10);

        [Desc("动作")] public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat($"<c color='{sw.COLOR_KEYWORKD}'>FOR</c> (<c color='{sw.COLOR_KEYWORKD}'>VAR</c> i={BeginIndex}; i&lt;{EndIndex} ;i+={Step})").AppendLine();
            if (!Action.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendLine(Action);
                sw.IndentEnd("}");
            }
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            int start = (int)BeginIndex.GetValueAs(api, args);
            int step = (int)Step.GetValueAs(api, args);
            int end = (int)EndIndex.GetValueAs(api, args);
            //args = args.Clone();
            if (Action != null)
            {
                for (int i = start; i < end; i += step)
                {
                    args.IteratingInt32 = (i);
                    Action.Invoke(api, args);
                    args.IteratingInt32 = (0);
                }
            }

            return null;
        }
        [TriggingArg("迭代值")] public int Iterating(IEventArguments args) => args.IteratingInt32;
    }

    [Desc("执行多条指令2", "[基础]")]
    public class DoAction2 : AbstractAction
    {
        [Desc("动作1")] public AbstractAction Action1 = new DoNoting();
        [Desc("动作2")] public AbstractAction Action2 = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendLine(Action1);
            sw.Append(Action2);
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            if (Action1 != null)
            {
                Action1.Invoke(api, args);
            }

            if (Action2 != null)
            {
                Action2.Invoke(api, args);
            }

            return null;
        }
    }


    [Desc("执行多条指令3", "[基础]")]
    public class DoAction3 : AbstractAction
    {
        [Desc("动作1")] public AbstractAction Action1 = new DoNoting();
        [Desc("动作2")] public AbstractAction Action2 = new DoNoting();
        [Desc("动作3")] public AbstractAction Action3 = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendLine(Action1);
            sw.AppendLine(Action2);
            sw.Append(Action3);
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            if (Action1 != null)
            {
                Action1.Invoke(api, args);
            }

            if (Action2 != null)
            {
                Action2.Invoke(api, args);
            }

            if (Action3 != null)
            {
                Action3.Invoke(api, args);
            }

            return null;
        }
    }

    [Desc("按顺序执行动作(执行多条指令)", "[基础]")]
    public class DoActionQueue : AbstractAction
    {
        [Desc("动作序列")]
        [ListDescAttribute(typeof(AbstractAction))]
        public List<AbstractAction> ActionQueue = new List<AbstractAction>();

        protected override void GetText(EventStringBuilder sw)
        {
            for (int i = 0; i < ActionQueue.Count; i++)
            {
                if (i < ActionQueue.Count - 1)
                {
                    sw.AppendLine(ActionQueue[i]);
                }
                else
                {
                    sw.Append(ActionQueue[i]);
                }
            }
        }

        protected override object Run(EventExecutor api, IEventArguments args)
        {
            foreach (AbstractAction ca in ActionQueue)
            {
                if (ca != null)
                {
                    ca.Invoke(api, args);
                }
            }

            return null;
        }
    }
    //-------------------------------------------------------------------
    public abstract class AbstractSwitchCase : AbstractAction
    {
        protected abstract AbstractValue Switch { get; }
        protected abstract IEnumerable<AbstractAction> CaseList { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_KEYWORKD + "'>SWITCH</c> ({0})", Switch).AppendLine();
            sw.IndentBegin("{");
            foreach (var ca in CaseList)
            {
                sw.Append(ca);
                sw.AppendLine();
            }
            sw.IndentEnd("}");
        }
    }
    public abstract class AbstractCaseAction : AbstractAction, IStereoOption
    {
        public abstract EventExternalizable Input { get; set; }
        public abstract EventExternalizable Output { get; set; }
        public virtual string InputName { get => "Case"; }
        public virtual string OutputName { get => "Action"; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>CASE</c> ").Append(Input).Append(" :");
            sw.IndentBegin();
            sw.Append(Output);
            sw.IndentEnd();
        }
    }
    //-------------------------------------------------------------------
    [Desc("Switch ... Case (Int32)", "[基础]")]
    public class SwitchCaseInteger : AbstractSwitchCase
    {
        [Desc("Switch值")] public AbstractValue<double> SwitchValue = new IntegerValue.RandomInt();
        [Desc("Case集合")]
        [ListDescAttribute(typeof(CaseActionInt32))]
        public List<CaseActionInt32> Cases = new List<CaseActionInt32>();
        protected override AbstractValue Switch => SwitchValue;
        protected override IEnumerable<AbstractAction> CaseList => Cases;
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            int sv = (int)SwitchValue.GetValueAs(api, args);
            foreach (CaseActionInt32 ca in Cases)
            {
                if (ca.CaseValue != null && ca.Action != null)
                {
                    int cv = (int)ca.CaseValue.GetValueAs(api, args);
                    if (cv == sv)
                    {
                        ca.Invoke(api, args);
                    }
                }
            }

            return null;
        }

        [Desc("Case动作", Editable = false)]
        [Expandable]
        [StereoOption(typeof(AbstractValue<double>), "Case", typeof(AbstractAction), "Action")]
        public class CaseActionInt32 : AbstractCaseAction, IStereoOption
        {
            [Desc("Case值")] public AbstractValue<double> CaseValue = new IntegerValue.VALUE();
            [Desc("Case动作")] public AbstractAction Action = new DoNoting();
            public override EventExternalizable Input
            {
                get => CaseValue;
                set => CaseValue = (AbstractValue<double>)value;
            }

            public override EventExternalizable Output
            {
                get => Action;
                set => Action = (AbstractAction)value;
            }
            protected override object Run(EventExecutor api, IEventArguments args)
            {
                return Action.Invoke(api, args);
            }

        }
    }
    //-------------------------------------------------------------------
    [Desc("Switch ... Case (string)", "[基础]")]
    public class SwitchCaseString : AbstractSwitchCase
    {
        [Desc("Switch值")] public AbstractValue<string> SwitchValue = new StringValue.VALUE();
        [Desc("Case集合")]
        [ListDescAttribute(typeof(CaseActionString))]
        public List<CaseActionString> Cases = new List<CaseActionString>();
        protected override AbstractValue Switch => SwitchValue;
        protected override IEnumerable<AbstractAction> CaseList => Cases;
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            string sv = SwitchValue.GetValueAs(api, args);
            foreach (CaseActionString ca in Cases)
            {
                if (ca.CaseValue != null && ca.Action != null)
                {
                    string cv = ca.CaseValue.GetValueAs(api, args);
                    if (cv == sv)
                    {
                        ca.Invoke(api, args);
                    }
                }
            }
            return null;
        }
    }
    [Desc("Case动作", Editable = false)]
    [Expandable]
    [StereoOption(typeof(AbstractValue<string>), "Case", typeof(AbstractAction), "Action")]
    public class CaseActionString : AbstractCaseAction, IStereoOption
    {
        [Desc("Case值")] public AbstractValue<string> CaseValue = new StringValue.VALUE();
        [Desc("Case动作")] public AbstractAction Action = new DoNoting();
        public override EventExternalizable Input
        {
            get => CaseValue;
            set => CaseValue = (AbstractValue<string>)value;
        }
        public override EventExternalizable Output
        {
            get => Action;
            set => Action = (AbstractAction)value;
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            return Action.Invoke(api, args);
        }
    }
    //-------------------------------------------------------------------
    [Desc("概率执行(万分比)", "[基础]")]
    public class RandomAction : AbstractAction<double>
    {
        [Desc("IF 条件")]
        public AbstractValue<double> Value = new IntegerValue.VALUE(1000);

        [Desc("THEN 动作")]
        public AbstractAction Action = new DoNoting();

        [Desc("ELSE 动作")]
        public AbstractAction ElseAction = null;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_KEYWORKD + "'>IF</c> ({0}/10000概率) <c color='" + sw.COLOR_KEYWORKD + "'>THEN</c>", Value).AppendLine();
            if (!Action.IsNullOrEmpty())
            {
                sw.IndentBegin("{");
                sw.AppendLine(Action);
                sw.IndentEnd("}");
            }
            if (ElseAction != null)
            {
                sw.AppendLine();
                sw.AppendLine("<c color='" + sw.COLOR_KEYWORKD + "'>ELSE</c>");
                sw.IndentBegin("{");
                sw.AppendLine(ElseAction);
                sw.IndentEnd("}");
            }
        }
        protected override double RunAs(EventExecutor api, IEventArguments args)
        {
            int v = api.API.RandomN.Next(0, 10000);
            if (v < Value.GetValueAs(api, args))
            {
                Action?.Invoke(api, args);
                return v;
            }
            else
            {
                ElseAction?.Invoke(api, args);
                return v;
            }
        }
    }
    //-------------------------------------------------------------------
//     [Desc("开始监听", "[基础]")]
//     public class StartTriggerAction : AbstractAction
//     {
//         [Desc("触发器")] public AbstractTrigger Trigger;
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.AppendFormat("<c color='" + sw.COLOR_KEYWORKD + "'>开始监听:</c>{0}", Trigger);
//         }
//         protected override object Run(EventExecutor api, IEventArguments args)
//         {
//             if (Trigger != null)
//             {
//                 Trigger.StartListen(api, args);
//             }
//             return null;
//         }
//     }
    //-------------------------------------------------------------------
}