using System;
using System.Collections.Concurrent;
using System.Threading;
using DeepCore;
using DeepCore.Log;
using DeepCrystal.RPC;

namespace DeepCrystal.Threading.Timer
{
    public class DeepTimer : Disposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(nameof(DeepTimer));
        private ITimeSpanTask mTimeTaskMS;
        private readonly string name;
        internal object state;
        internal TimerCallback callback;
        internal DeepTimerGroup group;
        internal DeepTimer(ITimeSpanTask tt)
        {
            this.name = tt.ToString();
            this.mTimeTaskMS = tt;
            Alloc.RecordConstructor(name);
        }
        ~DeepTimer()
        {
            Alloc.RecordDestructor(name);
        }
        protected override void RecordDisposing()
        {
            Alloc.RecordDispose(name);
        }
        protected override void Disposing()
        {
            if (mTimeTaskMS != null)
            {
                mTimeTaskMS.Dispose();
                mTimeTaskMS = null;
            }
            state = null;
            callback = null;
            group = null;
        }
    }

    public class DeepTimerGroup : Disposable
    {
        private readonly static TimeSpan TIME_LIMIT = TimeSpan.FromDays(1);
        private readonly Logger log = LoggerFactory.GetLogger(nameof(DeepTimerGroup));
        private readonly ConcurrentDictionary<string, TimeList> mTimerLists = new ConcurrentDictionary<string, TimeList>();
        private readonly int sensitivity;
        /// <summary>
        /// 时钟以sensitivity的间隔精度执行。
        /// 比如sensitivity==1000，则每个任务以sensitivity倍率执行。
        /// </summary>
        /// <param name="sensitivity"></param>
        public DeepTimerGroup(int sensitivity)
        {
            this.AsSynchronizedDisposing();
            this.sensitivity = sensitivity;
        }
        protected override void Disposing()
        {
            var timers = mTimerLists.ToArray();
            mTimerLists.Clear();
            foreach (var list in timers)
            {
                list.Value.Dispose();
            }
        }
        public DeepTimer CreateTimer(TimeSpan dueTime, TimeSpan period, bool missfire, object state, TimerCallback callback)
        {
            if (dueTime > TIME_LIMIT || period > TIME_LIMIT)
            {
                throw new ArgumentOutOfRangeException($"Timer duration exceeds limit: '{dueTime}' or '{period}', TIME_LIMIT is '{TIME_LIMIT}'");
            }
            if (dueTime.Ticks == 0)
            {
                callback(state);
            }
            var delayMS = (int)(((long)(Math.Max(dueTime.TotalMilliseconds, 0) / sensitivity)) * sensitivity);
            var periodMS = (int)(((long)(period.TotalMilliseconds / sensitivity)) * sensitivity);
            if (periodMS == 0)
            {
                throw new Exception($"Can Not Create ZERO period timer >>>{period}<<< , Sensitivity is {sensitivity}ms !");
            }
            if (dueTime.Ticks > 0 && delayMS == 0)
            {
                throw new Exception($"Can Not Create ZERO dueTime timer >>>{period}<<< , Sensitivity is {sensitivity}ms !");
            }
            var tl = mTimerLists.GetOrAdd($"{delayMS}-{periodMS}", it => new TimeList(delayMS, periodMS));
            var ret = tl.AddTimeTask(missfire, static (t, now) =>
            {
                if (t.IsExit == false)
                {
                    if (t.UserData is DeepTimer dt)
                    {
                        try
                        {
                            dt.callback(dt.state);
                        }
                        catch (Exception err)
                        {
                            var handle = dt.group.event_OnError;
                            if (handle != null) handle(dt.state, err);
                            else dt.group.log.Error(err.Message, err);
                        }
                    }
                }
            });
            {
                ret.state = state;
                ret.callback = callback;
                ret.group = this;
            }
            return ret;
        }

        private Action<object, Exception> event_OnError;
        public event Action<object, Exception> OnError { add { event_OnError += value; } remove { event_OnError -= value; } }

        private class TimeList : Disposable
        {
            private readonly int mDelayMS;
            private readonly int mIntervalMS;
            private readonly TimeSpanTaskQueue mTimeTasks;
            private readonly System.Threading.Timer mTimer;
            private DateTime mLastUpdateTime;
            public TimeList(int delay, int fixedInterval)
            {
                this.AsSynchronizedDisposing();
                mIntervalMS = fixedInterval;
                mDelayMS = delay;
                mTimeTasks = new TimeSpanTaskQueue(CollectionPool.Shared);
                mLastUpdateTime = DateTime.Now;
                mTimer = new System.Threading.Timer(OnTick, this, mDelayMS, mIntervalMS);
            }
            protected override void Disposing()
            {
                try { mTimer.Dispose(); } catch { }
                try { mTimeTasks.Dispose(); } catch { }
            }
            private void OnTick(object state)
            {
                TimeSpan intervalMS;
                lock (this)
                {
                    var curTime = DateTime.Now;
                    intervalMS = (curTime - mLastUpdateTime);
                    mLastUpdateTime = curTime;
                }
                mTimeTasks.Update(intervalMS);
            }
            internal DeepTimer AddTimeTask(bool missfire, TimeSpanTickHandler handler)
            {
                var ttask = mTimeTasks.AddTimeTask(
                    TimeSpan.FromMilliseconds(mIntervalMS),
                    TimeSpan.FromMilliseconds(mDelayMS),
                    0,
                    missfire,
                    handler
                );
                var ret = new DeepTimer(ttask);
                ttask.UserData = ret;
                return ret;
            }
        }

    }
}
