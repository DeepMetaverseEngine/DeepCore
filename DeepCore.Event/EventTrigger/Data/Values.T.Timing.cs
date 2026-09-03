using DeepCore.Formula;
using DeepCore.Reflection;
using System;

namespace DeepCore.EventTrigger.Data
{



    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 时间
    [Desc("经过时间Ticks", "[基础]/时间")]
    public class PassTimeTicks : AbstractValue<double>
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => api.API.PassTime.Ticks;
    }
    [Desc("经过时间Ticks缩放", "[基础]/时间")]
    public class PassTimeScaleTicks : AbstractValue<double>
    {
        public AbstractValue<double> TimeScale = new RealValue.VALUE(1);
        protected override double GetValue(EventExecutor api, IEventArguments args) => api.API.PassTime.Ticks * TimeScale.GetValueAs(api, args);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("经过时间Ticks缩放{0}倍", TimeScale);
        }
    }

    [Desc("系统时间Ticks", "[基础]/时间")]
    public class SystemTimeTicks : AbstractValue<double>
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => System.DateTime.Now.Ticks;
    }
    [Desc("系统时间Ticks缩放", "[基础]/时间")]
    public class SystemTimeScaleTicks : AbstractValue<double>
    {
        public AbstractValue<double> TimeScale = new RealValue.VALUE(1);
        protected override double GetValue(EventExecutor api, IEventArguments args) => System.DateTime.Now.Ticks * TimeScale.GetValueAs(api, args);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("系统时间Ticks缩放{0}倍", TimeScale);
        }
    }

    [Desc("当前时间Ticks", "[基础]/时间")]
    public class DateTimeTicks : AbstractValue<double>
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => api.API.DateTime.Ticks;
    }
    [Desc("当前时间Ticks缩放", "[基础]/时间")]
    public class DateTimeScaleTicks : AbstractValue<double>
    {
        public AbstractValue<double> TimeScale = new RealValue.VALUE(1);
        protected override double GetValue(EventExecutor api, IEventArguments args) => api.API.DateTime.Ticks * TimeScale.GetValueAs(api, args);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当前时间Ticks缩放{0}倍", TimeScale);
        }
    }

    [Desc("在系统时间范围内", "[基础]/时间")]
    public class InSystemTimeRange : BooleanValue
    {
        public DateTime Start = DateTime.Now;
        public DateTime End = DateTime.Now + TimeSpan.FromMinutes(10);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("在系统时间范围[{0}-{1}]内", Start, End);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            var time = DateTime.Now;
            return (time >= Start && time <= End);
        }
    }
    [Desc("在经过时间范围内", "[基础]/时间")]
    public class InPassTimeRange : BooleanValue
    {
        public DateTime Start = DateTime.MinValue;
        public DateTime End = DateTime.MinValue + TimeSpan.FromMinutes(10);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("在经过时间范围[{0}-{1}]内", Start, End);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            var time = new DateTime(api.API.PassTime.Ticks);
            return (time >= Start && time <= End);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 经过时间
    [Desc("经过时间毫秒", "[基础]/时间")]
    public class PassTimeMS : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.Milliseconds);
    }
    [Desc("经过时间秒", "[基础]/时间")]
    public class PassTimeSEC : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.Seconds);
    }
    [Desc("经过时间分钟", "[基础]/时间")]
    public class PassTimeMIN : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.Minutes);
    }
    [Desc("经过时间小时", "[基础]/时间")]
    public class PassTimeHOR : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.Hours);
    }
    [Desc("经过时间天", "[基础]/时间")]
    public class PassTimeDAY : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.Days);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 指定经过时间
    [Desc("指定经过时间毫秒", "[基础]/时间")]
    public class VPassTimeMS : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).Milliseconds);
    }
    [Desc("指定经过时间秒", "[基础]/时间")]
    public class VPassTimeSEC : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).Seconds);
    }
    [Desc("指定经过时间分钟", "[基础]/时间")]
    public class VPassTimeMIN : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).Minutes);
    }
    [Desc("指定经过时间小时", "[基础]/时间")]
    public class VPassTimeHOR : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).Hours);
    }
    [Desc("指定经过时间天", "[基础]/时间")]
    public class VPassTimeDAY : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).Days);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 经过总共
    [Desc("经过时间总共毫秒", "[基础]/时间")]
    public class PassTimeTotalMS : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.TotalMilliseconds);
    }
    [Desc("经过时间总共秒", "[基础]/时间")]
    public class PassTimeTotalSEC : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.TotalSeconds);
    }
    [Desc("经过时间总共分钟", "[基础]/时间")]
    public class PassTimeTotalMIN : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.TotalMinutes);
    }
    [Desc("经过时间总共小时", "[基础]/时间")]
    public class PassTimeTotalHOR : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.TotalHours);
    }
    [Desc("经过时间总共天", "[基础]/时间")]
    public class PassTimeTotalDAY : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.PassTime.TotalDays);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 指定经过指总共
    [Desc("指定经过时间总共毫秒", "[基础]/时间")]
    public class VPassTimeTotalMS : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).TotalMilliseconds);
    }
    [Desc("指定经过时间总共秒", "[基础]/时间")]
    public class VPassTimeTotalSEC : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).TotalSeconds);
    }
    [Desc("指定经过时间总共分钟", "[基础]/时间")]
    public class VPassTimeTotalMIN : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).TotalMinutes);
    }
    [Desc("指定经过时间总共小时", "[基础]/时间")]
    public class VPassTimeTotalHOR : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).TotalHours);
    }
    [Desc("指定经过时间总共天", "[基础]/时间")]
    public class VPassTimeTotalDAY : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(TimeSpan.FromTicks((long)Ticks.GetValueAs(api, args)).TotalDays);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 当前时间
    [Desc("当前时间毫秒", "[基础]/时间")]
    public class DateTimeMS : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.DateTime.Millisecond);
    }
    [Desc("当前时间秒", "[基础]/时间")]
    public class DateTimeSEC : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.DateTime.Second);
    }
    [Desc("当前时间分钟", "[基础]/时间")]
    public class DateTimeMIN : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.DateTime.Minute);
    }
    [Desc("当前时间小时", "[基础]/时间")]
    public class DateTimeHOR : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.DateTime.Hour);
    }
    [Desc("当前时间天", "[基础]/时间")]
    public class DateTimeDAY : IntegerValue
    {
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(api.API.DateTime.Day);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
    #region 指定当前时间
    [Desc("指定当前时间毫秒", "[基础]/时间")]
    public class VDateTimeMS : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(new DateTime((long)Ticks.GetValueAs(api, args)).Millisecond);
    }
    [Desc("指定当前时间秒", "[基础]/时间")]
    public class VDateTimeSEC : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(new DateTime((long)Ticks.GetValueAs(api, args)).Second);
    }
    [Desc("指定当前时间分钟", "[基础]/时间")]
    public class VDateTimeMIN : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(new DateTime((long)Ticks.GetValueAs(api, args)).Minute);
    }
    [Desc("指定当前时间小时", "[基础]/时间")]
    public class VDateTimeHOR : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(new DateTime((long)Ticks.GetValueAs(api, args)).Hour);
    }
    [Desc("指定当前时间天", "[基础]/时间")]
    public class VDateTimeDAY : IntegerValue
    {
        public AbstractValue<double> Ticks = new PassTimeTicks();
        protected override double GetValue(EventExecutor api, IEventArguments args) => Convert.ToInt32(new DateTime((long)Ticks.GetValueAs(api, args)).Day);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------
}
