using System;
using System.Collections.Generic;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore
{
    public delegate void TimeSpanTickHandler(ITimeSpanTask task, TimeSpan tickTime);
    public interface ITimeSpanTask : IDisposable
    {
        public object UserData { get; set; }
        public bool IsRunning { get; }
        public bool IsExit { get; }
        public int TickCount { get; }
    }
    public class TimeSpanTaskQueue : IDisposable
    {
        private RecycleLinkList<TimeSpanTask> mTimeTasks = new RecycleLinkList<TimeSpanTask>();
        private int mTaskCount = 0;
        private AbstractCollectionPool mPool;
        private TimeSpan passTime = TimeSpan.Zero;
        public TimeSpan TotalTime
        {
            get
            {
                lock (mTimeTasks) { return passTime; }
            }
        }
        public TimeSpanTaskQueue() : this(CollectionPool.Shared)
        {
        }
        public TimeSpanTaskQueue(AbstractCollectionPool pool)
        {
            this.mPool = pool;
        }
        public void Dispose()
        {
            lock (mTimeTasks)
            {
                mTimeTasks.Clear();
                mTaskCount = 0;
            }
        }
        public void Update(in TimeSpan interval)
        {
            if (mTaskCount > 0)
            {
                using (var removing = mPool.AllocList<TimeSpanTask>())
                using (var invoking = mPool.AllocList<ValueTuple<TimeSpanTask, TimeSpan>>())
                {
                    lock (mTimeTasks)
                    {
                        this.passTime += interval;
                        for (var it = mTimeTasks.First; it != null;)
                        {
                            var t = it.Value;
                            t.Update(interval, invoking);
                            if (t.IsExit)
                            {
                                it = RemoveTask(t);
                                removing.Add(t);
                            }
                            else
                            {
                                it = it.Next;
                            }
                        }
                    }
                    if (invoking.Count > 0)
                    {
                        foreach (var e in invoking)
                        {
                            e.Item1.TryInvoke(e.Item2);
                        }
                    }
                    if (removing.Count > 0)
                    {
                        foreach (var e in removing)
                        {
                            e.DoExit();
                        }
                    }
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private LinkedListNode<TimeSpanTask> RemoveTask(TimeSpanTask time)
        {
            if (time.node != null)
            {
                var ret = time.node.Next;
                lock (mTimeTasks)
                {
                    mTimeTasks.Remove(time.node);
                    time.node = null;
                    mTaskCount--;
                }
                return ret;
            }
            return null;
        }
        private void AddTimeTaskInternal(TimeSpanTask time)
        {
            lock (mTimeTasks)
            {
                var node = mTimeTasks.SortedInsert(time, static (a, b) => a.CompareTo(b));
                time.node = node;
                mTaskCount++;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 增加时间任务
        /// </summary>
        public ITimeSpanTask AddTimeTask(TimeSpan interval, TimeSpan delay, int repeat, bool missfire, TimeSpanTickHandler handler)
        {
            var time = new TimeSpanTask(this, interval, delay, repeat, missfire, handler);
            AddTimeTaskInternal(time);
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加时间任务
        /// </summary>
        public ITimeSpanTask AddTimeTask(TimeSpan interval, TimeSpan delay, bool missfire, TimeSpanTickHandler handler)
        {
            var time = new TimeSpanTask(this, interval, delay, 0, missfire, handler);
            AddTimeTaskInternal(time);
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加延时回调方法
        /// </summary>
        public ITimeSpanTask AddTimeDelay(TimeSpan delay, bool missfire, TimeSpanTickHandler handler)
        {
            var time = new TimeSpanTask(this, TimeSpan.Zero, delay, 0, missfire, handler);
            AddTimeTaskInternal(time);
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加定时回调方法
        /// </summary>
        public ITimeSpanTask AddTimePeriodic(TimeSpan interval, bool missfire, TimeSpanTickHandler handler)
        {
            var time = new TimeSpanTask(this, interval, TimeSpan.Zero, 0, missfire, handler);
            AddTimeTaskInternal(time);
            time.Start();
            return time;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        public class TimeSpanTask : Disposable, IComparable<TimeSpanTask>, ITimeSpanTask
        {
            readonly private TimeSpan IntervalTime;
            readonly private TimeSpan DelayTime;
            readonly private int RepeatCount;
            readonly private bool MissFire;


            private TimeSpanTickHandler mHandler;
            private Action<TimeSpanTask> mOnExit;

            private TimeSpan mTotalTime = TimeSpan.Zero;
            private TimeSpan mPassTime = TimeSpan.Zero;
            private TimeSpan mDelayTime = TimeSpan.Zero;
            private int mRepeatTick = 0;
            private bool mRunning = false;
            private bool mExit = false;
            private int mTickCount = 0;

            internal readonly TimeSpanTaskQueue owner;
            internal LinkedListNode<TimeSpanTask> node;

            public object UserData { get; set; }
            public bool IsRunning { get { return mRunning; } }
            public bool IsExit { get { return mExit; } }
            public int TickCount { get => mTickCount; }


            internal TimeSpanTask(TimeSpanTaskQueue owner, TimeSpan interval, TimeSpan delay, int repeat, bool missfire, TimeSpanTickHandler handler)
            {
                this.AsSynchronizedDisposing();
                this.owner = owner;
                this.mHandler = handler;
                this.IntervalTime = interval;
                this.DelayTime = delay;
                this.RepeatCount = repeat;
                this.MissFire = missfire;
                this.Reset();
            }
            public override string ToString()
            {
                return $"d-{DelayTime} i-{IntervalTime} r-{RepeatCount}";
            }
            public int CompareTo(TimeSpanTask other)
            {
                return (int)(other.IntervalTime - this.IntervalTime).Ticks;
            }
            public void Reset()
            {
                if (IntervalTime.Ticks <= 0 && DelayTime.Ticks <= 0)
                {
                    this.mExit = true;
                    this.mRunning = false;
                    return;
                }
                this.mRunning = true;
                this.mPassTime = TimeSpan.Zero;
                this.mRepeatTick = 0;
                this.mDelayTime = DelayTime;
                this.mTickCount = 0;
            }
            protected override void Disposing()
            {
                this.mHandler = null;
                this.mExit = true;
                if (mOnExit == null)
                {
                    if (node != null)
                    {
                        owner.RemoveTask(this);
                    }
                    DoExit();
                }
            }
            internal void DoExit()
            {
                this.mOnExit?.Invoke(this);
                this.mOnExit = null;
                this.mHandler = null;
                this.UserData = null;
                this.node = null;
            }
            internal void Start()
            {
                this.mPassTime = TimeSpan.Zero;
                this.mRunning = true;
            }

            /// <summary>
            /// 更新
            /// </summary>
            internal void Update(in TimeSpan interval, List<ValueTuple<TimeSpanTask, TimeSpan>> invoking)
            {
                if (!mExit && mRunning)
                {
                    mTotalTime += interval;
                    mPassTime += interval;
                    if (mDelayTime.Ticks > 0)
                    {
                        var delta = mPassTime - mDelayTime;
                        if (delta.Ticks >= 0)
                        {
                            mPassTime -= mDelayTime;
                            invoking.Add((this, mTotalTime - delta));
                            mDelayTime = TimeSpan.Zero;
                            if (IntervalTime.Ticks <= 0)
                            {
                                mExit = true;
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (IntervalTime.Ticks > 0)
                    {
                        while (true)
                        {
                            var delta = mPassTime - IntervalTime;
                            if (delta.Ticks >= 0)
                            {
                                mPassTime -= IntervalTime;
                                invoking.Add((this, mTotalTime - delta));
                                if (RepeatCount > 0)
                                {
                                    mRepeatTick++;
                                    if (mRepeatTick >= RepeatCount)
                                    {
                                        mExit = true;
                                        break;
                                    }
                                }
                                if (MissFire)
                                {
                                    continue;
                                }
                                else
                                {
                                    mPassTime = TimeSpan.FromTicks(0);
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            internal void TryInvoke(in TimeSpan time)
            {
                try
                {
                    mHandler?.Invoke(this, time);
                }
                finally
                {
                    mTickCount++;
                }
            }
            public void Pause()
            {
                mRunning = false;
            }
            public void Resume()
            {
                mRunning = true;
            }

            public event Action<TimeSpanTask> OnExit
            {
                add { mOnExit += value; }
                remove { mOnExit -= value; }
            }
        }

    }

}
