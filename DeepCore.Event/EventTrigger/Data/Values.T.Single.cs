using DeepCore.Formula;
using DeepCore.Reflection;
using System;

namespace DeepCore.EventTrigger.Data
{

    [Desc("[基础]-小数点型")]
    public abstract class RealValue : AbstractValue<double>
    {
        [Desc("值", "[基础]")]
        public class VALUE : RealValue
        {
            [Desc("值")]
            public float Value = 20.00f;
            public VALUE() { }
            public VALUE(float value) { this.Value = value; }
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
        public class ReturnVALUE : RealValue
        {
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                try
                {
                    return Convert.ToSingle(args.ReturnValue);
                }
                catch { return 0; }
            }
        }
        [Desc("触发的值(Number)", "[基础]")]
        public class TriggingValue : RealValue
        {
            public TriggingValue() { }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的值");
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return args.TriggingNumberValue;
            }
        }
        //------------------------------------------------------------------------------------------------------------


        [Desc("小数计算", "[基础]/数学")]
        public class RealOP : RealValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new RealValue.VALUE();
            [Desc("运算符")]
            public NumericOP OP = NumericOP.ADD;
            [Desc("值2")]
            public AbstractValue<double> Value2 = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}) {1} ({2})", Value1, FormulaHelper.ToString(OP), Value2);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var ret = FormulaHelper.Calculate(Value1.GetValueAs(api, args), OP, Value2.GetValueAs(api, args));
                return ret;
            }
        }

        [Desc("函数-最小值", "[基础]/数学")]
        public class MinFunction : RealValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new RealValue.VALUE();
            [Desc("值2")]
            public AbstractValue<double> Value2 = new RealValue.VALUE();

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
        public class MaxFunction : RealValue
        {
            [Desc("值1")]
            public AbstractValue<double> Value1 = new RealValue.VALUE();
            [Desc("值2")]
            public AbstractValue<double> Value2 = new RealValue.VALUE();

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

        [Desc("随机小数", "[基础]/数学")]
        public class RandomReal : RealValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("介于0.0和1.0之间的随机数");
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return api.API.RandomN.NextFloat();
            }
        }

        [Desc("随机小数(Min Max)", "[基础]/数学")]
        public class RandomRealMinMax : RealValue
        {
            [Desc("Min")]
            public AbstractValue<double> Min = new RealValue.VALUE();
            [Desc("Max")]
            public AbstractValue<double> Max = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("介于{0}和{1}之间的随机数", Min, Max);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                return api.API.RandomN.NextDouble(Min.GetValueAs(api, args), Max.GetValueAs(api, args));
            }
        }


        [Desc("从整数转换", "[基础]/转换")]
        public class ConvertFromInteger : RealValue
        {
            [Desc("值")]
            public AbstractValue<double> Value = new IntegerValue.VALUE();

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("IntToFloat({0})", Value);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var value = Value.GetValueAs(api, args);
                return value;
            }
        }







        [Desc("从字符串转换", "[基础]/转换")]
        public class ParseFromString : RealValue
        {
            [Desc("值")]
            public AbstractValue<string> Value = new StringValue.VALUE("1");

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("StringToFloat({0})", Value);
            }
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                string value = Value.GetValueAs(api, args);

                if (Parser.TryParseFloat(value, out var ret))
                {
                    return ret;
                }
                return 0f;
            }
        }

    }

//     [Desc("[基础]-小数数组", "数组")]
//     public abstract class RealArrayValue : AbstractArrayValue<float>
//     {
//         [Desc("小数数组", "值")] public class VALUE : ArrayValue<AbstractValue<double>, float> { }
//         [Desc("小数数组索引", "数组")] public class INDEX : ArrayIndexValue<float> { }
//         [Desc("小数数组随机", "数组")] public class RANDOM : ArrayRandomValue<float> { }
//         [Desc("迭代中的小数", "数组")] public class ITERATOR : ArrayIteratingValue<float> { }
//     }
}
