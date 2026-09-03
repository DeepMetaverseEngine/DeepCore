using DeepCore.Formula;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using static DeepCore.Colors;

namespace DeepCore.EventTrigger.Data
{

    //-------------------------------------------------------------------


    [Desc("事件条件")]
    [Expandable]
    public abstract class AbstractCondition : AbstractValue<bool>
    {
        public bool DoTest(EventExecutor api, IEventArguments args)
        {
            if (EventExecutor.ENABLE_TRACE) api.Trace(this);
            return GetValueAs(api, args);
        }
    }


    [Desc("总是可以", "[基础]/布尔")]
    public class AlwaysTrue : AbstractCondition
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("总是可以");
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            return true;
        }
    }

    [Desc("布尔条件", "[基础]/布尔")]
    public class BooleanCondition : AbstractCondition
    {
        [Desc("布尔值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append(Value);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }

    [Desc("注释", "[基础]/注释")]
    public class CommentCondition : AbstractCondition
    {
        [Desc("注释")]
        public string Comment = "注释";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Comment);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            return true;
        }
    }



    public abstract class IFAction<T> : AbstractAction
    {
        [Desc("THEN 动作")] public AbstractAction Action = new DoNoting();
        [Desc("ELSE 动作")] public AbstractAction ElseAction = null;   
        protected abstract bool Compare(EventExecutor api, IEventArguments args);
        protected abstract void GetCompareText(EventStringBuilder sw);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_KEYWORKD + "'>IF</c>");
            GetCompareText(sw);
            sw.AppendLine("<c color='" + sw.COLOR_KEYWORKD + "'>THEN</c>");
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
            if (Compare(api, args))
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

    [Desc("IF Number 比较", "[基础]")]
    public class IFNumberAction : IFAction<double>
    {
        [Desc("值1")]
        public AbstractValue<double> Value1 = new RealValue.VALUE();
        [Desc("比较符")]
        public NumericComparisonOP Op = NumericComparisonOP.EQUAL;
        [Desc("值2")]
        public AbstractValue<double> Value2 = new RealValue.VALUE();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
        }
        protected override Boolean Compare(EventExecutor api, IEventArguments args)
        {
            var c1 = Value1.GetValueAs(api, args);
            var c2 = Value2.GetValueAs(api, args);
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }

    [Desc("IF Bool 比较", "[基础]")]
    public class IFBoolAction : IFAction<bool>
    {
        [Desc("值1")]
        public AbstractValue<bool> Condition1 = new BooleanValue.VALUE();
        [Desc("运算符")]
        public BooleanOP Op = BooleanOP.EQUAL;
        [Desc("值2")]
        public AbstractValue<bool> Condition2 = new BooleanValue.VALUE();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Condition1, FormulaHelper.ToString(Op), Condition2);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            return FormulaHelper.Calculate(() => Condition1.GetValueAs(api, args), Op, () => Condition2.GetValueAs(api, args));
        }
    }

    [Desc("IF String 比较", "[基础]")]
    public class IFStringAction : IFAction<string>
    {
        [Desc("字符串1")]
        public AbstractValue<string> String1 = new StringValue.VALUE();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("字符串2")]
        public AbstractValue<string> String2 = new StringValue.VALUE();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", String1, FormulaHelper.ToString(Op), String2);
        }
        protected override Boolean Compare(EventExecutor api, IEventArguments args)
        {
            string c1 = String1.GetValueAs(api, args);
            string c2 = String2.GetValueAs(api, args);
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }

}
