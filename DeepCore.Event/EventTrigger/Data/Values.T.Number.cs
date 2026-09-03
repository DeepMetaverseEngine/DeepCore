using DeepCore.Formula;
using DeepCore.Reflection;
using System;

namespace DeepCore.EventTrigger.Data
{

    [Desc("角度转弧度", "[基础]/转换")]
    public class AngleToRadiance : RealValue
    {
        [Desc("值")]
        public AbstractValue<double> Angle = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("ToRadiance({0})", Angle);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var value = Angle.GetValueAs(api, args);
            return value * Math.PI / 180.0;
        }
    }

    [Desc("弧度转角度", "[基础]/转换")]
    public class RadianceToAngle : RealValue
    {
        [Desc("值")]
        public AbstractValue<double> Radiance = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("ToAngle({0})", Radiance);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var value = Radiance.GetValueAs(api, args);
            return value * 180.0 / Math.PI;
        }
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
