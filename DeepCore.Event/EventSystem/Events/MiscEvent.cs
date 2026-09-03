using System;
using System.Collections.Generic;
using System.Linq;
using DeepCore;
using DeepCore.Event.EventSystem.Message;

namespace DeepCore.Event.EventSystem.Events
{
    [Event("延迟", "Async")]
    public class DelaySecEvent : CustomEvent
    {
        [EventArgument(null, 0)] public float Sec;

        public DelaySecEvent(float sec)
        {
            Sec = sec;
        }

        public DelaySecEvent()
        {
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            if (RunningTimeMS > Sec * 1000)
            {
                Stop(true);
            }
        }
    }

    [Event("所有EventManager地址", "Async")]
    public class GetAllEventManagerAddressEvent : CustomEvent
    {
        [EventOutput(null, 0)] public string[] Addresses;
        protected override void OnStart()
        {
            base.OnStart();
            Addresses = EventManagerFactory.Instance.AllEventManager.Select(e => e.Address).ToArray();
            Stop(true);
        }
    }

    [Event("延迟一帧", "Async")]
    public class NextFrameEvent : CustomEvent
    {
        private int mPass = 0;

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            mPass += 1;
            if (mPass > 1)
            {
                Stop(true);
            }
        }
    }

    [Event("延迟一帧", "Sync")]
    public class ActionEvent : CustomEvent
    {
        private Action mAction;

        public ActionEvent()
        {
        }

        public ActionEvent(Action act)
        {
            mAction = act;
        }

        protected override void OnStart()
        {
            base.OnStart();
            mAction?.Invoke();
            Stop(true);
        }

        protected override void OnStop()
        {
            base.OnStop();
            mAction = null;
        }
    }

    [Event("永远等待", "Async")]
    public class WaitAlwaysEvent : CustomEvent
    {
    }

    [Event("停止父事件", "Sync")]
    public class StopParentEvent : CustomEvent
    {
        [EventArgument(null, 0)] public bool Success;
        [EventArgument(null, 1)] public UnionValue ParentOutput;
        [EventArgument(null, 2)] public string Reason;

        protected override void OnStart()
        {
            base.OnStart();
            Stop(true);
            Parent.Output = ParentOutput;
            Parent.Stop(Success, Reason);
        }
    }

    [Event("按概率随机", "Sync")]
    public class RandomPercentEvent : CustomEvent
    {
        [EventArgument(null, 0)] public int Percent;
        [EventOutput("结果", 0)] public bool Value;


        protected override void OnStart()
        {
            base.OnStart();
            Value = Mgr.Random.Next(0, 100) < Percent;
            Stop(true);
        }

        public RandomPercentEvent(int percent)
        {
            Percent = percent;
        }

        public RandomPercentEvent()
        {
        }
    }

    [Event("在2个数字之间随机", "Sync")]
    public class RandomIntegerEvent : CustomEvent
    {
        [EventArgument("min", 0)] public int Min;
        [EventArgument("max", 1)] public int Max;
        [EventOutput("随机值", 0)] public int Value;

        protected override void OnStart()
        {
            base.OnStart();
            Value = Mgr.Random.Next(Min, Max);
            Stop(true);
        }
    }

    [Event("随机0-1之间的浮点数", "Sync")]
    public class RandomSingleEvent : CustomEvent
    {
        [EventOutput("随机值", 0)] public float Value;

        protected override void OnStart()
        {
            base.OnStart();
            Value = Convert.ToSingle(Mgr.Random.NextDouble());
            Stop(true);
        }
    }

    [Event("间隔时长触发", "Listen")]
    public class PeriodicSecEvent : CustomEvent
    {
        [EventArgument("间隔时长", 0)] public float PeriodSec;
        [EventArgument("最大持续时间", 1)] public float MaxSec = float.MaxValue;

        private long mLastRunningMS;
        private int mPeriodMS;

        protected override void OnStart()
        {
            base.OnStart();
            mPeriodMS = Convert.ToInt32(PeriodSec * 1000);
            mLastRunningMS = 0;
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            if (RunningTimeMS - mLastRunningMS >= mPeriodMS)
            {
                mLastRunningMS = RunningTimeMS;
                var sec = RunningTimeMS / 1000f;
                sec = Math.Min(sec, MaxSec);
                Trigger(sec);
                if (sec >= MaxSec)
                {
                    Stop(true);
                }
            }
        }
    }

    [Event("监听某事件的消息", "Listen")]
    public class NamedMessageEvent : CustomEvent
    {
        [EventArgument("消息名", 0)] public string MessageName;

        protected override void OnStart()
        {
            base.OnStart();
            Mgr.OnNamedMessage += OnNamedMessage;
        }

        protected override void OnStop()
        {
            base.OnStop();
            Mgr.OnNamedMessage -= OnNamedMessage;
        }

        private readonly List<NamedEventMessage> mMessages = new List<NamedEventMessage>();

        private bool mHasMessage = false;
        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            List<NamedEventMessage> messages = null;

            if (mHasMessage)
            {
                lock (mMessages)
                {
                    messages = new List<NamedEventMessage>(mMessages);
                    mMessages.Clear();
                    mHasMessage = false;
                }
            }

            if (messages != null)
            {
                foreach (var obj in messages)
                {
                    var address = EventManagerAddress.Parse(obj.From);
                    Trigger(UnionValueSerializer.Serialize(new[] { obj.Name, obj.Content, address.Name, address.UUID }));
                }
            }
        }

        private void OnNamedMessage(NamedEventMessage obj)
        {
            if (string.IsNullOrEmpty(MessageName) || obj.Name == MessageName)
            {
                lock (mMessages)
                {
                    mHasMessage = true;
                    mMessages.Add(obj);
                }
            }
        }
    }

    [Event("发送带消息名称的消息", "Sync")]
    public class SendNamedMessageEvent : CustomEvent
    {
        [EventArgument("需要发送到的EventManager名称", 0)]
        public string ManagerName;

        [EventArgument("需要发送到的EventManager的UUID", 1)]
        public string UUID;

        [EventArgument("消息名", 2)] public string MessageName;
        [EventArgument("消息内容", 3)] public UnionValue Content;

        protected override void OnStart()
        {
            base.OnStart();
            if (string.IsNullOrEmpty(ManagerName))
            {
                Stop(false, "ManagerName null");
                return;
            }

            var msg = new NamedEventMessage
            {
                Content = Content,
                From = Mgr.Address,
                FromEvent = ID,
                Name = MessageName,
                To = EventManager.GetAddress(ManagerName, UUID)
            };
            EventManager.MessageBroker.Publish(msg.To, Mgr, msg);
            Stop(true);
        }
    }

    [Event("指定的DateTime触发-参数{秒，分，时}", "Listen")]
    public class TodayTimeEvent : BaseEvent
    {
        private DateTime mLastDateTime = DateTime.MinValue;
        public const int AccuracySec = 2;

        public class TodayTime
        {
            public int Hour = -1;
            public int Minute = -1;
            public int Second = 0;
        }


        //秒，分，时
        [EventArgument("时间间隔", 0)] public TodayTime[] DateTimes;

        protected override void OnStart()
        {
            base.OnStart();
            if (DateTimes == null || DateTimes.Length == 0)
            {
                Stop(false, "DateTimes?.Length == 0");
            }
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);

            if (Math.Abs(DateTime.Now.Second - mLastDateTime.Second) < AccuracySec + 0.5)
            {
                return;
            }

            mLastDateTime = DateTime.Now;

            foreach (var dateTime in DateTimes)
            {
                if (dateTime.Hour >= 0 && mLastDateTime.Hour != dateTime.Hour)
                {
                    continue;
                }

                if (dateTime.Minute >= 0 && mLastDateTime.Minute != dateTime.Minute)
                {
                    continue;
                }

                if (dateTime.Second >= 0 && Math.Abs(mLastDateTime.Second - dateTime.Second) > ms / 1000f + AccuracySec)
                {
                    continue;
                }

                Trigger(UnionValueSerializer.Serialize(dateTime));
            }
        }
    }

    [Event("新的一天开始了", "Listen")]
    public class NewDayEvent : CustomEvent
    {
        public const int AccuracySec = 10;

        private DateTime mLastDateTime = DateTime.MinValue;
        private DateTime mDayStartTime;

        protected override void OnStart()
        {
            base.OnStart();
            mDayStartTime = DateTime.Now;
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            if (DateTime.Now.Second - mLastDateTime.Second < AccuracySec + 0.5)
            {
                return;
            }

            mLastDateTime = DateTime.Now;

            if (mDayStartTime.Day != mLastDateTime.Day)
            {
                mDayStartTime = mLastDateTime;
                Trigger(mDayStartTime.Year, mDayStartTime.Month, mDayStartTime.Day, (int)mDayStartTime.DayOfWeek);
            }
        }
    }
}