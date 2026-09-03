using DeepCore.Formula;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{

    [Desc("[基础]-布尔型")]
    public abstract class BooleanValue : AbstractValue<Boolean>
    {
        [Desc("值", "[基础]")]
        public class VALUE : BooleanValue
        {
            [Desc("值")]
            public bool Value;
            public VALUE() { this.Value = true; }
            public VALUE(bool value) { this.Value = value; }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append(Value).Append("</c>");
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                return Value;
            }
        }
        [Desc("返回值", "[基础]")]
        public class ReturnVALUE : BooleanValue
        {
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                try
                {
                    return Convert.ToBoolean(args.ReturnValue);
                }
                catch { return false; }
            }
        }
        [Desc("触发的值(Bool)", "[基础]")]
        public class TriggingValue : BooleanValue
        {
            public TriggingValue() { }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的值");
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                return args.TriggingBoolValue;
            }
        }

        [Desc("从字符串转换", "[基础]/转换")]
        public class ParseFromString : BooleanValue
        {
            [Desc("值")]
            public AbstractValue<string> Value = new StringValue.VALUE("1");

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("从{0}转换", Value);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                var value = Value.GetValueAs(api, args);
                if (bool.TryParse(value, out var ret))
                {
                    return ret;
                }
                return false;
            }
        }

        [Desc("百分比几率", "[基础]/几率")]
        public class RandomPercent : BooleanValue
        {
            [Desc("百分比几率")]
            public AbstractValue<double> Percent = new RealValue.VALUE(50);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}%几率", Percent);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                float pct = (float)Percent.GetValueAs(api, args);
                if (pct > 0)
                {
                    return CUtils.RandomPercent(api.API.RandomN, pct);
                }
                return false;
            }

        }

        [Desc("布尔运算组", "[基础]/比较")]
        public class BooleanOperatorGroup : BooleanValue
        {
            [Desc("运算符")]
            public BooleanOP Op = BooleanOP.EQUAL;

            [Desc("布尔集合")]
            [ListDescAttribute(typeof(AbstractValue<bool>))]
            public List<AbstractValue<bool>> Cases = new List<AbstractValue<bool>>();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("(");
                for (int i = 0; i < Cases.Count; i++)
                {
                    if (i > 0)
                    {
                        sw.Append(" <c color='" + sw.COLOR_KEYWORKD + "'>" + Op.ToString() + "</c> ");
                    }
                    sw.Append(Cases[i]);
                }
                sw.Append(")");
                //sw += CUtils.ListToString(Cases, ") " + Op.ToString() + " (", "(", ")");
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                if (Cases.Count > 1)
                {
                    var current = Cases[0];
                    bool value = current.GetValueAs(api, args);
                    for (int i = 1; i < Cases.Count; i++)
                    {
                        value = FormulaHelper.Calculate(value, Op, Cases[i].GetValueAs(api, args));
                    }
                    return value;
                }
                else if (Cases.Count == 1)
                {
                    return Cases[0].GetValueAs(api, args);
                }
                else
                {
                    return false;
                }
            }
        }

        [Desc("布尔运算", "[基础]/比较")]
        public class BooleanOperator : BooleanValue
        {
            [Desc("值1")]
            public AbstractValue<bool> Condition1 = new VALUE();
            [Desc("运算符")]
            public BooleanOP Op = BooleanOP.EQUAL;
            [Desc("值2")]
            public AbstractValue<bool> Condition2 = new VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Condition1, FormulaHelper.ToString(Op), Condition2);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                return FormulaHelper.Calculate(() => Condition1.GetValueAs(api, args), Op, () => Condition2.GetValueAs(api, args));
            }
        }

        [Desc("布尔比较", "[基础]/比较")]
        public class BooleanComparison : BooleanValue
        {
            [Desc("值1")]
            public AbstractValue<bool> Condition1 = new VALUE();
            [Desc("比较符")]
            public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
            [Desc("值2")]
            public AbstractValue<bool> Condition2 = new VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Condition1, FormulaHelper.ToString(Op), Condition2);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                return FormulaHelper.Compare(Condition1.GetValueAs(api, args), Op, Condition2.GetValueAs(api, args));
            }
        }



        [Desc("字符串比较", "[基础]/比较")]
        public class StringComparison : BooleanValue
        {
            [Desc("字符串1")]
            public AbstractValue<string> String1 = new StringValue.VALUE();
            [Desc("比较符")]
            public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
            [Desc("字符串2")]
            public AbstractValue<string> String2 = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", String1, FormulaHelper.ToString(Op), String2);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                string c1 = String1.GetValueAs(api, args);
                string c2 = String2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }
        [Desc("字符串是否包含", "[基础]/比较")]
        public class StringContains : BooleanValue
        {
            [Desc("字符串")]
            public AbstractValue<string> String = new StringValue.VALUE();
            [Desc("子字符串")]
            public AbstractValue<string> SubString = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).Contains({1})", String, SubString);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                string c1 = String.GetValueAs(api, args);
                string c2 = SubString.GetValueAs(api, args);
                return c1.Contains(c2);
            }
        }
        [Desc("字符串前缀比较", "[基础]/比较")]
        public class StringStartsWith : BooleanValue
        {
            [Desc("字符串")]
            public AbstractValue<string> String = new StringValue.VALUE();
            [Desc("前缀")]
            public AbstractValue<string> Prefix = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).StartsWith({1})", String, Prefix);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                string c1 = String.GetValueAs(api, args);
                string c2 = Prefix.GetValueAs(api, args);
                return c1.StartsWith(c2);
            }
        }
        [Desc("字符串后缀比较", "[基础]/比较")]
        public class StringEndsWith : BooleanValue
        {
            [Desc("字符串")]
            public AbstractValue<string> String = new StringValue.VALUE();
            [Desc("后缀")]
            public AbstractValue<string> Suffix = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}).EndsWith({1})", String, Suffix);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                string c1 = String.GetValueAs(api, args);
                string c2 = Suffix.GetValueAs(api, args);
                return c1.EndsWith(c2);
            }
        }

        [Desc("小数比较", "[基础]/比较")]
        public class RealComparison : BooleanValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new RealValue.VALUE();
            [Desc("比较符")]
            public NumericComparisonOP Op = NumericComparisonOP.EQUAL;
            [Desc("值2")]
            public AbstractValue<double> Value2 = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                var c1 = Value1.GetValueAs(api, args);
                var c2 = Value2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }

        [Desc("整数比较", "[基础]/比较")]
        public class IntegerComparison : BooleanValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new IntegerValue.VALUE();
            [Desc("比较符")]
            public NumericComparisonOP Op = NumericComparisonOP.EQUAL;
            [Desc("值2")]
            public AbstractValue<double> Value2 = new IntegerValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                var c1 = Value1.GetValueAs(api, args);
                var c2 = Value2.GetValueAs(api, args);
                return FormulaHelper.Compare(c1, Op, c2);
            }
        }




        [Desc("当前触发器是否已开启", "[基础]/触发器")]
        public class CurrentEventTriggerIsActive : BooleanValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("当前触发器是否已开启");
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                return api.IsActive;
            }
        }

        [Desc("指定触发器是否已开启", "[基础]/触发器")]
        public class SpecifyEventTriggerIsActive : BooleanValue
        {
            [Desc("事件触发器名字")]
            [EventIDAttribute]
            public string EventName;

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发器:({0})是否已开启", EventName);
            }
            protected override Boolean GetValue(EventExecutor api, IEventArguments args)
            {
                EventExecutor evt = api.Group.GetEditEvent(EventName);
                if (evt != null)
                {
                    return evt.IsActive;
                }
                return false;
            }
        }

        [Desc("值在范围内", "[基础]/数学")]
        public class ValueInRange : BooleanValue
        {
            [Desc("最小值")]
            public AbstractValue<double> Min = new IntegerValue.VALUE(1);
            [Desc("值")]
            public AbstractValue<double> Value = new IntegerValue.VALUE(1);
            [Desc("最大值")]
            public AbstractValue<double> Max = new IntegerValue.VALUE(100);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("值在范围内({0} 小于等于 {1} 小于等于 {2})", Min, Value, Max);
            }
            protected override bool GetValue(EventExecutor api, IEventArguments args)
            {
                var min = Min.GetValueAs(api, args);
                var max = Max.GetValueAs(api, args);
                var value = Value.GetValueAs(api, args);
                return max >= value && value >= min;
            }
        }
    }
    // 
    //     [Desc("[基础]-布尔数组", "数组")]
    //     public abstract class BooleanArrayValue : AbstractArrayValue<bool>
    //     {
    //         [Desc("布尔数组", "值")] public class VALUE : ArrayValue<AbstractValue<bool>, bool> { }
    //         [Desc("布尔数组索引", "数组")] public class INDEX : ArrayIndexValue<bool> { }
    //         [Desc("布尔数组随机", "数组")] public class RANDOM : ArrayRandomValue<bool> { }
    //         [Desc("迭代中的布尔值", "数组")] public class ITERATOR : ArrayIteratingValue<bool> { }
    //     }
    // 

    //     /// <summary>
    //     /// 行为树套用
    //     /// </summary>
    //     [Desc(Editable = false)]
    //     public class ConditionGroup : BooleanValue
    //     {
    // 
    //     }
}
