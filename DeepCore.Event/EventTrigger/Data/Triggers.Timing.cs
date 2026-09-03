using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{

    [Desc("时间逝去", "[基础]/时间")]
    public class TimeElapsed : AbstractTrigger
    {
        [Desc("时间(秒)")]
        public float TimeSEC = 5.0f;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}秒之后", TimeSEC);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimeDelaySEC(args, TimeSEC);
        }
    }

    [Desc("时间间隔", "[基础]/时间")]
    public class TimePeriodic : AbstractTrigger
    {
        [Desc("时间(秒)")]
        public float EveryTimeSEC = 5.0f;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("每隔{0}秒", EveryTimeSEC);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimePeriodicSEC(args, EveryTimeSEC);
        }
    }

    [Desc("时间间隔按次数", "[基础]/时间")]
    public class TimeTask : AbstractTrigger
    {
        [Desc("延时时间(秒)")]
        public float DelayTimeSEC = 0f;
        [Desc("间隔时间(秒)")]
        public float EveryTimeSEC = 5.0f;
        [Desc("重复次数")]
        public int RepeatCount = 0;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("延时{0}秒，每隔{1}秒，执行{2}次", DelayTimeSEC, EveryTimeSEC, RepeatCount);

        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimeTaskSEC(args, EveryTimeSEC, DelayTimeSEC, RepeatCount);
        }
    }



    [Desc("（变量）时间逝去", "[基础]/时间")]
    public class ValuedTimeElapsed : AbstractTrigger
    {
        [Desc("时间(秒)")]
        public AbstractValue<double> TimeSEC = new RealValue.VALUE(5);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}秒之后", TimeSEC);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimeDelaySEC(args, (float)TimeSEC.GetValueAs(api, args));
        }
    }

    [Desc("（变量）时间间隔", "[基础]/时间")]
    public class ValuedTimePeriodic : AbstractTrigger
    {
        [Desc("时间(秒)")]
        public AbstractValue<double> EveryTimeSEC = new RealValue.VALUE(5);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("每隔{0}秒", EveryTimeSEC);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimePeriodicSEC(args, (float)EveryTimeSEC.GetValueAs(api, args));
        }
    }

    [Desc("（变量）时间间隔按次数", "[基础]/时间")]
    public class ValuedTimeTask : AbstractTrigger
    {
        [Desc("延时时间(秒)")]
        public AbstractValue<double> DelayTimeSEC = new RealValue.VALUE(5);
        [Desc("间隔时间(秒)")]
        public AbstractValue<double> EveryTimeSEC = new RealValue.VALUE(5);
        [Desc("重复次数")]
        public AbstractValue<double> RepeatCount = new IntegerValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("延时{0}秒，每隔{1}秒，执行{2}次", DelayTimeSEC, EveryTimeSEC, RepeatCount);

        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_TimeTaskSEC(args,
                (float)EveryTimeSEC.GetValueAs(api, args),
                (float)DelayTimeSEC.GetValueAs(api, args),
                (int)RepeatCount.GetValueAs(api, args));
        }
    }




    [Desc("经过时间整分报时", "[基础]/时间")]
    public class OnTimeMIN : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new TimeSpanAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.PassTimeAlarm, hander,
                static (t, d) => t.OnMinAlarm += d,
                static (t, d) => t.OnMinAlarm -= d);
        }
    }
    [Desc("经过时间整时报时", "[基础]/时间")]
    public class OnTimeHOR : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new TimeSpanAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.PassTimeAlarm, hander,
                static (t, d) => t.OnHourAlarm += d,
                static (t, d) => t.OnHourAlarm -= d);
        }
    }
    [Desc("经过时间整天报时", "[基础]/时间")]
    public class OnTimeDAY : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new TimeSpanAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.PassTimeAlarm, hander,
                static (t, d) => t.OnDayAlarm += d,
                static (t, d) => t.OnDayAlarm -= d);
        }
    }


    [Desc("当前时间整分报时", "[基础]/时间")]
    public class OnDateTimeMIN : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new DateTimeAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.DateTimeAlarm, hander,
                static (t, d) => t.OnMinAlarm += d,
                static (t, d) => t.OnMinAlarm -= d);
        }
    }
    [Desc("当前时间整时报时", "[基础]/时间")]
    public class OnDateTimeHOR : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new DateTimeAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.DateTimeAlarm, hander,
                static (t, d) => t.OnHourAlarm += d,
                static (t, d) => t.OnHourAlarm -= d);
        }
    }
    [Desc("当前时间整天报时", "[基础]/时间")]
    public class OnDateTimeDAY : AbstractTrigger
    {
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            var hander = new DateTimeAlarmHandler(time => api.TestAndDoAction(args));
            api.Listen(api.API.DateTimeAlarm, hander,
                static (t, d) => t.OnDayAlarm += d,
                static (t, d) => t.OnDayAlarm -= d);
        }
    }

}
