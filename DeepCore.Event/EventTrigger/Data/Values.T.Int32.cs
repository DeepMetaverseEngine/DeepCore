using DeepCore.Formula;
using DeepCore.Reflection;
using System;

namespace DeepCore.EventTrigger.Data
{
    [Desc("[基础]-整形")]
    public abstract class IntegerValue : AbstractValue<double>
    {
        [Desc("值", "[基础]")]
        public class VALUE : IntegerValue
        {
            [Desc("值")]
            public int Value = 10;
            public VALUE() { }
            public VALUE(int v)
            {
                this.Value = v;
            }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append(Value).Append("</c>");
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return Value;
            }
        }
        [Desc("返回值", "[基础]")]
        public class ReturnVALUE : IntegerValue
        {
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                try
                {
                    return Convert.ToInt32(args.ReturnValue);
                }
                catch { return 0; }
            }
        }
        [Desc("触发的值(强转一次int)", "[基础]")]
        public class TriggingValue : IntegerValue
        {
            public TriggingValue() { }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的值");
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return (int)args.TriggingNumberValue;
            }
        }

        [Desc("从字符串转换", "[基础]/转换")]
        public class ParseFromString : IntegerValue
        {
            [Desc("值")]
            public AbstractValue<string> Value = new StringValue.VALUE("1");

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("StringToInt({0})", Value);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                string value = Value.GetValueAs(api, args);
                if (Parser.TryParseInt(value, out var ret))
                {
                    return ret;
                }
                return 0;
            }
        }

        [Desc("整形计算", "[基础]/数学")]
        public class IntegerOP : IntegerValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new IntegerValue.VALUE();
            [Desc("运算符")]
            public NumericOP OP = NumericOP.ADD;
            [Desc("值2")]
            public AbstractValue<double> Value2 = new IntegerValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}) {1} ({2})", Value1, FormulaHelper.ToString(OP), Value2);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var ret = (int)FormulaHelper.Calculate(Value1.GetValueAs(api, args), OP, Value2.GetValueAs(api, args));
                return ret;
            }
        }

        [Desc("函数-最小值", "[基础]/数学")]
        public class MinFunction : IntegerValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new IntegerValue.VALUE();
            [Desc("值2")]
            public AbstractValue<double> Value2 = new IntegerValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Min({0}, {1})", Value1, Value2);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var v1 = Value1.GetValueAs(api, args);
                var v2 = Value2.GetValueAs(api, args);
                return Math.Min(v1, v2);
            }
        }

        [Desc("函数-最大值", "[基础]/数学")]
        public class MaxFunction : IntegerValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new IntegerValue.VALUE();
            [Desc("值2")]
            public AbstractValue<double> Value2 = new IntegerValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Max({0}, {1})", Value1, Value2);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var v1 = Value1.GetValueAs(api, args);
                var v2 = Value2.GetValueAs(api, args);
                return Math.Max(v1, v2);
            }
        }

        [Desc("随机整形", "[基础]/数学")]
        public class RandomInt : IntegerValue
        {
            [Desc("最小值")]
            public AbstractValue<double> Min = new IntegerValue.VALUE(0);
            [Desc("最大值(小于)")]
            public AbstractValue<double> Max = new IntegerValue.VALUE(10);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("Random({0}, {1}(不包括))", Min, Max);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var max = (int)Max.GetValueAs(api, args);
                var min = (int)Min.GetValueAs(api, args);
                return api.API.RandomN.Next(min, max);
            }
        }


        [Desc("从小数转换", "[基础]/转换")]
        public class ConvertFromReal : IntegerValue
        {
            [Desc("值")]
            public AbstractValue<double> Value = new RealValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("FloatToInt({0})", Value);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var value = (int)Value.GetValueAs(api, args);
                return value;
            }
        }

        [Desc("迭代中的整形", "[基础]/循环迭代")]
        public class PickingIteratorInt32 : IntegerValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("迭代中的整形");
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return args.IteratingInt32;
            }
        }

        //----------------------------------------------------------------------------------------------

        [Desc("枚举值", "[基础]")]
        public class EnumValueInt32 : IntegerValue
        {
            [Desc("枚举值")]
            public EnumValue EnumValue;
            protected override void GetText(EventStringBuilder sw)
            {
                if (EnumValue != null)
                {
                    sw.AppendFormat("枚举值:{0}", EnumValue);
                }
                else
                {
                    sw.Append("0");
                }
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                if (EnumValue != null)
                {
                    return EnumValue.Value;
                }
                return 0;
            }
        }


        //----------------------------------------------------------------------------------------------

    }


//     [Desc("[基础]-整形数组", "数组")]
//     public abstract class IntegerArrayValue : AbstractArrayValue<int>
//     {
//         [Desc("整形数组", "值")] public class VALUE : ArrayValue<AbstractValue<double>, int> { }
//         [Desc("整形数组索引", "数组")] public class INDEX : ArrayIndexValue<int> { }
//         [Desc("整形数组随机", "数组")] public class RANDOM : ArrayRandomValue<int> { }
//         [Desc("迭代中的整形", "数组")] public class ITERATOR : ArrayIteratingValue<int> { }
//     }
}
