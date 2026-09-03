using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
namespace System
{
    public static class TimeExt
    {

        public static DateTime ToDayTime(this DateTime dt)
        {
            return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }
    }


}

namespace DeepCore
{

    //-------------------------------------------------------------------------------------------------------------------
    public class TimeInterval : Recyclable
    {
        private Action mHandler;
        private float mIntervalTimeMS;
        private double mPassTimeMS = 0;
        private int mTickCount = 0;
        private bool mFirstTimeEnable = true;
        public object UserTag { get; set; }

        public TimeInterval()
        {
        }
        public TimeInterval(float intervalMS)
        {
            Init(intervalMS);
        }
        public TimeInterval Init(float intervalMS)
        {
            this.mIntervalTimeMS = intervalMS;
            return this;
        }
        protected override void Destructing()
        {

        }
        protected override void Disposing()
        {
            this.mHandler = default;
            this.mIntervalTimeMS = default;
            this.mPassTimeMS = 0;
            this.mTickCount = 0;
            this.mFirstTimeEnable = true;
            this.UserTag = default;
        }

        /// <summary>
        /// 间隔时间
        /// </summary>
        public float IntervalTimeMS { get { return mIntervalTimeMS; } }
        /// <summary>
        /// 触发过多少次
        /// </summary>
        public int TotalTickCount { get { return mTickCount; } }
        /// <summary>
        /// 总共经过时间
        /// </summary>
        public double TotalPassTimeMS { get { return mPassTimeMS; } }
        public bool FirstTimeEnable { set { mFirstTimeEnable = value; } }
        public event Action Handler { add { mHandler += value; } remove { mHandler -= value; } }

        public void SetPassTime(double passtimeMS)
        {
            mPassTimeMS = passtimeMS;
        }

        /// <summary>
        /// 记录归零
        /// </summary>
        public void Reset()
        {
            mPassTimeMS = 0;
            mTickCount = 0;
        }
        public void Reset(float intervalMS)
        {
            mIntervalTimeMS = intervalMS;
            mPassTimeMS = 0;
            mTickCount = 0;
        }

        public bool Update(float intervalMS)
        {
            var ret = false;
            if (mIntervalTimeMS > 0)
            {
                if (mPassTimeMS == 0 && mFirstTimeEnable)
                {
                    this.mTickCount++;
                    this.mHandler?.Invoke();
                    ret = true;
                }
                this.mPassTimeMS += intervalMS;
                while (mPassTimeMS >= mIntervalTimeMS)
                {
                    this.mPassTimeMS -= mIntervalTimeMS;
                    this.mTickCount++;
                    this.mHandler?.Invoke();
                    ret = true;
                }
            }
            return ret;
        }
    }
    public class TimeInterval<T> : TimeInterval
    {
        public T Tag { get; set; }
        public TimeInterval() { }
        public TimeInterval(float intervalMS)
        {
            this.Init(intervalMS);
        }
        public TimeInterval<T> Init(float intervalMS, T tag = default(T))
        {
            base.Init(intervalMS);
            this.Tag = tag;
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.Tag = default(T);
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    public class TimeExpire : Recyclable
    {
        private double mTotalTimeMS;
        private double mPassTimeMS;
        public object UserTag { get; set; }

        public TimeExpire() { }
        public TimeExpire(double totalMS)
        {
            this.Init(totalMS);
        }
        public TimeExpire Init(double totalMS, object tag = null)
        {
            this.mTotalTimeMS = totalMS;
            this.mPassTimeMS = 0;
            this.UserTag = tag;
            return this;
        }
        protected override void Destructing()
        {

        }
        protected override void Disposing()
        {
            mTotalTimeMS = 0;
            mPassTimeMS = 0;
            UserTag = null;
        }

        public double TotalTimeMS { get { return mTotalTimeMS; } }
        public double PassTimeMS { get { return mPassTimeMS; } }
        public double ExpireTimeMS { get { return Math.Max(mTotalTimeMS - mPassTimeMS, 0); } }
        /// <summary>
        /// 记录归零
        /// </summary>
        public void Reset()
        {
            this.mPassTimeMS = 0;
        }
        public void Reset(double totalTimeMS)
        {
            this.mPassTimeMS = 0;
            this.mTotalTimeMS = totalTimeMS;
        }

        public void End()
        {
            this.mPassTimeMS = mTotalTimeMS;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="intervalMS">间隔时间</param>
        /// <returns>是否到期</returns>
        public bool Update(float intervalMS)
        {
            this.mPassTimeMS += intervalMS;
            if (mPassTimeMS >= mTotalTimeMS)
            {
                this.mPassTimeMS = mTotalTimeMS;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否结束
        /// </summary>
        public bool IsEnd { get { return mPassTimeMS >= mTotalTimeMS; } }

        /// <summary>
        /// 剩余百分比
        /// </summary>
        public float Amount
        {
            get
            {
                return mTotalTimeMS == 0 ? 1 : Math.Min((float)(mPassTimeMS / mTotalTimeMS), 1);
            }
        }
    }
    public class TimeExpire<T> : TimeExpire
    {
        public T Tag { get; set; }
        public TimeExpire() { }
        public TimeExpire(double totalMS)
        {
            this.Init(totalMS);
        }
        public TimeExpire<T> Init(double totalMS, T tag = default(T))
        {
            base.Init(totalMS, tag);
            this.Tag = tag;
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.Tag = default(T);
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    #region TimeTasks

    public delegate void TickHandler<ST>(ST st, TimeTaskMS task);
    public delegate void TickHandler(TimeTaskMS task);
    public class TimeTaskMS : Disposable
    {
        readonly private float IntervalTimeMS;
        readonly private float DelayTimeMS;
        readonly private int RepeatCount;

        public object UserData;

        private bool mRunning = false;
        private bool mExit = false;
        private TickHandler mInvoke;
        private double mPassTimeMS = 0;
        private float mDelayMS = 0;
        private int mRepeatTick = 0;
        private int mTickCount = 0;
        private int mInvokingCount = 0;
        private Action<TimeTaskMS> mOnExit;

        public bool IsRunning { get { return mRunning; } }
        public bool IsExit { get { return mExit; } }
        public int TickCount { get => mTickCount; }

        internal TimeTaskMS(float intervalMS, float delayMS, int repeat, TickHandler invoke)
        {
            this.mInvoke = invoke;
            this.IntervalTimeMS = intervalMS;
            this.DelayTimeMS = delayMS;
            this.RepeatCount = repeat;
            this.Reset();
        }
        public void Cancel()
        {
            this.mExit = true;
            this.mRunning = false;
            this.mOnExit = null;
            this.CleanInvoke();
        }
        public void Reset()
        {
            if (IntervalTimeMS <= 0 && DelayTimeMS <= 0)
            {
                this.mExit = true;
                this.mRunning = false;
                return;
            }
            this.mRunning = true;
            this.mPassTimeMS = 0;
            this.mRepeatTick = 0;
            this.mDelayMS = DelayTimeMS;
            this.mTickCount = 0;
        }
        protected override void Disposing()
        {
            this.mExit = true;
            this.mOnExit = null;
            this.CleanInvoke();
        }
        internal void Start()
        {
            this.mPassTimeMS = 0;
            this.mRunning = true;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="intervalMS">间隔时间</param>
        internal bool Update(float intervalMS)
        {
            if (!mExit && mRunning)
            {
                mPassTimeMS += intervalMS;
                if (mDelayMS > 0)
                {
                    if (mPassTimeMS >= mDelayMS)
                    {
                        mPassTimeMS -= mDelayMS;
                        mInvokingCount++;
                        mRepeatTick++;
                        mDelayMS = 0;
                        if (IntervalTimeMS <= 0)
                        {
                            mExit = true;
                            return mInvokingCount > 0;
                        }
                        else if (RepeatCount > 0 && mRepeatTick >= RepeatCount)
                        {
                            mExit = true;
                            return mInvokingCount > 0;
                        }
                        else
                        {
                            mPassTimeMS -= IntervalTimeMS;
                        }
                    }
                    else
                    {
                        return mInvokingCount > 0;
                    }
                }
                if (IntervalTimeMS > 0)
                {
                    while (mPassTimeMS >= 0)
                    {
                        mPassTimeMS -= IntervalTimeMS;
                        mInvokingCount++;
                        mRepeatTick++;
                        if (RepeatCount > 0 && mRepeatTick >= RepeatCount)
                        {
                            mExit = true;
                            break;
                        }
                    }
                }
                return mInvokingCount > 0;
            }
            return false;
        }
        internal void TryInvoke(Action<Exception> onerror)
        {
            while (mInvokingCount > 0)
            {
                mInvokingCount--;
                try
                {
                    OnInvoke();
                }
                catch (Exception err)
                {
                    if (onerror != null)
                    {
                        onerror.Invoke(err);
                    }
                    else
                    {
                        throw;
                    }
                }
                mTickCount++;
            }
        }
        internal void DoExit()
        {
            this.mOnExit?.Invoke(this);
            this.mOnExit = null;
            this.CleanInvoke();
            this.UserData = null;
        }
        public void Pause()
        {
            mRunning = false;
        }
        public void Resume()
        {
            mRunning = true;
        }
        public event Action<TimeTaskMS> OnExit
        {
            add { mOnExit += value; }
            remove { mOnExit -= value; }
        }
        protected virtual void OnInvoke()
        {
            mInvoke?.Invoke(this);
        }
        protected virtual void CleanInvoke()
        {
            mInvoke = null;
        }
    }
    public class TimeTaskMS<ST> : TimeTaskMS
    {
        private ST mState;
        private TickHandler<ST> mHandler;
        public TimeTaskMS(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler) : base(intervalMS, delayMS, repeat, null)
        {
            this.mState = st;
            this.mHandler = handler;
        }
        protected override void CleanInvoke()
        {
            base.CleanInvoke();
            this.mHandler = null;
            this.mState = default(ST);
        }
        protected override void OnInvoke()
        {
            base.OnInvoke();
            this.mHandler.Invoke(mState, this);
        }
    }
    public class TimeTaskQueue : IDisposable
    {
        private RecycleLinkList<TimeTaskMS> mTimeTasks = new RecycleLinkList<TimeTaskMS>();
        private int mTaskCount = 0;
        private AbstractCollectionPool mPool;
        private Action<Exception> mOnError;
        private double lastUpdateTimeMS;
        public event Action<Exception> OnError
        {
            add { mOnError += value; }
            remove { mOnError -= value; }
        }

        public TimeTaskQueue(AbstractCollectionPool pool)
        {
            this.mPool = pool;
        }
        public void Dispose()
        {
            lock (mTimeTasks)
            {
                for (var it = mTimeTasks.First; it != null; it = it.Next)
                {
                    TimeTaskMS t = it.Value;
                    t.Cancel();
                }
                mTimeTasks.Clear();
                mTaskCount = 0;
            }
        }
        public void CleanInvoke()
        {
            lock (mTimeTasks)
            {
                for (var it = mTimeTasks.First; it != null; it = it.Next)
                {
                    TimeTaskMS t = it.Value;
                    t.TryInvoke(mOnError);
                }
                mTimeTasks.Clear();
                mTaskCount = 0;
            }
        }
        public void Update(float intervalMS)
        {
            if (mTaskCount > 0)
            {
                using (var removing = mPool.AllocList<TimeTaskMS>())
                using (var invoking = mPool.AllocList<TimeTaskMS>())
                {
                    lock (mTimeTasks)
                    {
                        for (var it = mTimeTasks.First; it != null;)
                        {
                            var t = it.Value;
                            if (t.Update(intervalMS))
                            {
                                invoking.Add(t);
                            }
                            if (t.IsExit)
                            {
                                it = mTimeTasks.Remove(it);
                                removing.Add(t);
                                mTaskCount--;
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
                            e.TryInvoke(mOnError);
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
        public void UpdatePassTime(double passTimeMS)
        {
            float interval = (float)(passTimeMS - lastUpdateTimeMS);
            lastUpdateTimeMS = passTimeMS;
            this.Update(interval);
        }
        //-------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeTask(float intervalMS, float delayMS, int repeat, TickHandler handler)
        {
            TimeTaskMS time = new TimeTaskMS(intervalMS, delayMS, repeat, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeDelayMS(float delayMS, TickHandler handler)
        {
            TimeTaskMS time = new TimeTaskMS(0, delayMS, 0, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimePeriodicMS(float intervalMS, TickHandler handler)
        {
            TimeTaskMS time = new TimeTaskMS(intervalMS, 0, 0, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }

        //-------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeTask<ST>(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler)
        {
            var time = new TimeTaskMS<ST>(intervalMS, delayMS, repeat, st, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeDelayMS<ST>(float delayMS, ST st, TickHandler<ST> handler)
        {
            var time = new TimeTaskMS<ST>(0, delayMS, 0, st, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }
        /// <summary>
        /// 增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimePeriodicMS<ST>(float intervalMS, ST st, TickHandler<ST> handler)
        {
            var time = new TimeTaskMS<ST>(intervalMS, 0, 0, st, handler);
            lock (mTimeTasks)
            {
                mTimeTasks.AddLast(time);
                mTaskCount++;
            }
            time.Start();
            return time;
        }

        //-------------------------------------------------------------------------------------------------------------------
    }

    #endregion
    //-------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 同时执行多个Timer
    /// </summary>
    public class MultiTimeLine
    {
        private readonly AbstractCollectionPool pool;
        private readonly List<TimeExpire> Times = new List<TimeExpire>();

        public MultiTimeLine(AbstractCollectionPool pool)
        {
            this.pool = pool;
        }
        public void Clear()
        {
            for (int i = Times.Count - 1; i >= 0; --i)
            {
                var task = Times[i];
                task.Release();
            }
            Times.Clear();
        }
        public TimeExpire Add(float timeMS)
        {
            var ret = pool.AllocTimeExpire(timeMS);
            Times.Add(ret);
            return ret;
        }
        public bool Remove(TimeExpire task)
        {
            if (Times.Remove(task))
            {
                task.End();
                task.Release();
                return true;
            }
            return false;
        }
        public bool Update(float intervalMS)
        {
            var ret = false;
            for (int i = Times.Count - 1; i >= 0; --i)
            {
                var task = Times[i];
                if (task.Update(intervalMS))
                {
                    Times.RemoveAt(i);
                    task.Release();
                    ret = true;
                }
            }
            return ret;
        }
        public bool Enable { get { return Times.Count > 0; } }
    }


    //--------------------------------------------------------------------------------------------
    #region SystemTime


    public class SystemTimeInterval<T> : TimeInterval<T>
    {
        private double? lastUpdateTime;
        public SystemTimeInterval() { }
        public SystemTimeInterval(float intervalMS, T tag = default(T))
        {
            this.Init(intervalMS, tag);
        }
        new public SystemTimeInterval<T> Init(float intervalMS, T tag = default(T))
        {
            lastUpdateTime = null;
            base.Init(intervalMS, tag);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            lastUpdateTime = null;
        }
        public bool Update()
        {
            var curTime = CUtils.TickTimeMS;
            if (lastUpdateTime.HasValue)
            {
                var interval = (float)(curTime - lastUpdateTime.Value);
                lastUpdateTime = curTime;
                return base.Update(interval);
            }
            else
            {
                lastUpdateTime = curTime;
                return base.Update(0);
            }
        }
    }

    public class SystemTimeExpire<T> : TimeExpire<T>
    {
        private double? lastUpdateTime;
        public SystemTimeExpire() { }
        public SystemTimeExpire(double timeMS, T tag = default)
        {
            this.Init(timeMS, tag);
        }
        new public SystemTimeExpire<T> Init(double intervalMS, T tag = default(T))
        {
            lastUpdateTime = null;
            base.Init(intervalMS, tag);
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            lastUpdateTime = null;
        }
        public bool Update()
        {
            var curTime = CUtils.TickTimeMS;
            if (lastUpdateTime.HasValue)
            {
                var interval = (float)(curTime - lastUpdateTime.Value);
                lastUpdateTime = curTime;
                return base.Update(interval);
            }
            else
            {
                lastUpdateTime = curTime;
                return base.Update(0);
            }
        }
    }

    //--------------------------------------------------------------------------------------------
    public class SystemTimeTaskQueue : TimeTaskQueue
    {
        private double lastUpdateTime = CUtils.TickTimeMS;
        public SystemTimeTaskQueue(AbstractCollectionPool pool) : base(pool)
        {
        }
        public void Update()
        {
            var curTime = CUtils.TickTimeMS;
            var interval = (float)(curTime - lastUpdateTime);
            lastUpdateTime = curTime;
            base.Update(interval);
        }
    }

    public class SystemMultiTimeLine : MultiTimeLine
    {
        private double lastUpdateTime = CUtils.TickTimeMS;
        public SystemMultiTimeLine(AbstractCollectionPool pool) : base(pool) { }
        public void Update()
        {
            var curTime = CUtils.TickTimeMS;
            var interval = (float)(curTime - lastUpdateTime);
            lastUpdateTime = curTime;
            base.Update(interval);
        }
    }

    #endregion
    //--------------------------------------------------------------------------------------------


    public class SystemTimeRecoder
    {
        private double last_update_time = CUtils.TickTimeMS;
        private float current_interval;

        public double LastUpdateTimeMS { get { return last_update_time; } }
        public float CurrentIntervalMS { get { return current_interval; } }

        public SystemTimeRecoder()
        {
            this.last_update_time = CUtils.TickTimeMS;
            this.current_interval = 0;
        }

        public void Reset()
        {
            this.last_update_time = CUtils.TickTimeMS;
            this.current_interval = 0;
        }

        public float Update()
        {
            var curtime = CUtils.TickTimeMS;
            this.current_interval = (float)(curtime - last_update_time);
            this.last_update_time = curtime;
            return current_interval;
        }

    }
    public class ThreadUpdateable<T>
    {
        private readonly T state;
        private int _running = 0;
        private Thread _thread;
        private double _lastUpdateTime = 0;
        private float _lastUsedTime = 0;
        private float _fixedUpdateInterval = 30;
        public bool IsRunning { get => _running > 0; }
        public float FixedUpdateIntervalMS
        {
            get => _fixedUpdateInterval;
            set => _fixedUpdateInterval = value;
        }
        public float LastUsedTimeMS
        {
            get => _lastUsedTime;
        }

        public ThreadUpdateable(T state)
        {
            this.state = state;
        }

        public bool Start()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
            {
                _lastUpdateTime = 0;
                _lastUsedTime = 0;
                _thread = new Thread(ThreadMain);
                _thread.Start();
                return true;
            }
            return false;
        }
        public bool Stop()
        {
            if (Interlocked.CompareExchange(ref _running, 0, 1) == 1)
            {
                _thread.Join();
                return true;
            }
            return false;
        }

        private void ThreadMain()
        {
            try
            {
                try
                {
                    event_OnStart?.Invoke(state);
                }
                catch (Exception err)
                {
                    event_OnError?.Invoke(state, err);
                }
                var watch = Stopwatch.StartNew();
                while (_running > 0)
                {
                    var curTime = watch.Elapsed.TotalMilliseconds;
                    if (_lastUpdateTime == 0)
                    {
                        _lastUpdateTime = curTime;
                    }
                    var intervalMS = (float)(curTime - _lastUpdateTime);
                    _lastUpdateTime = curTime;
                    try
                    {
                        event_OnUpdate?.Invoke(state, intervalMS);
                    }
                    catch (Exception err)
                    {
                        event_OnError?.Invoke(state, err);
                    }
                    finally
                    {
                        _lastUsedTime = (float)(watch.Elapsed.TotalMilliseconds - curTime);
                        var delay = _fixedUpdateInterval - _lastUsedTime;
                        if (delay > 0)
                        {
                            Thread.Sleep((int)delay);
                        }
                    }
                }
                watch.Stop();
                try
                {
                    event_OnStop?.Invoke(state);
                }
                catch (Exception err)
                {
                    event_OnError?.Invoke(state, err);
                }
            }
            finally
            {
                event_OnStart = null;
                event_OnStop = null;
                event_OnUpdate = null;
                event_OnError = null;
            }
        }
        public delegate void StartHandler(T state);
        public delegate void StopHandler(T state);
        public delegate void UpdateHandler(T state, float intervalMS);
        public delegate void ErrorHandler(T state, Exception err);
        public event StartHandler OnStart { add { event_OnStart += value; } remove { event_OnStart -= value; } }
        public event StopHandler OnStop { add { event_OnStop += value; } remove { event_OnStop -= value; } }
        public event UpdateHandler OnUpdate { add { event_OnUpdate += value; } remove { event_OnUpdate -= value; } }
        public event ErrorHandler OnError { add { event_OnError += value; } remove { event_OnError -= value; } }
        private StartHandler event_OnStart;
        private StopHandler event_OnStop;
        private UpdateHandler event_OnUpdate;
        private ErrorHandler event_OnError;

    }

    public delegate void TimeSpanAlarmHandler(TimeSpan time);
    public class TimeSpanAlarm : Disposable
    {
        public event TimeSpanAlarmHandler OnMinAlarm;
        public event TimeSpanAlarmHandler OnHourAlarm;
        public event TimeSpanAlarmHandler OnDayAlarm;

        private List<TimeSpanAlarmHandler> OnMinAlarms = new();
        private List<TimeSpanAlarmHandler> OnHourAlarms = new();
        private List<TimeSpanAlarmHandler> OnDayAlarms = new();

        private int? lastMIN;
        private int? lastHOR;
        private int? lastDAY;
        public TimeSpanAlarm(TimeSpan time)
        {
            this.lastMIN = time.Minutes;
            this.lastHOR = time.Hours;
            this.lastDAY = time.Days;
        }
        public TimeSpanAlarm()
        {
        }
        protected override void Disposing()
        {
            OnMinAlarm = null;
            OnHourAlarm = null;
            OnDayAlarm = null;
            OnMinAlarms.Clear();
            OnHourAlarms.Clear();
            OnDayAlarms.Clear();
        }
        public bool Update(in TimeSpan time)
        {
            if (this.lastMIN.HasValue && this.lastMIN.Value != time.Minutes)
            {
                SaveInvoke(in time, OnMinAlarm);
                SaveInvokes(in time, OnMinAlarms);
            }
            if (this.lastHOR.HasValue && this.lastHOR.Value != time.Hours)
            {
                SaveInvoke(in time, OnHourAlarm);
                SaveInvokes(in time, OnHourAlarms);
            }
            if (this.lastDAY.HasValue && this.lastDAY.Value != time.Days)
            {
                SaveInvoke(in time, OnDayAlarm);
                SaveInvokes(in time, OnDayAlarms);
            }
            this.lastMIN = time.Minutes;
            this.lastHOR = time.Hours;
            this.lastDAY = time.Days;
            return true;
        }
        private void SaveInvokes(in TimeSpan time, List<TimeSpanAlarmHandler> list)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                var h = list[i];
                SaveInvoke(time, h);
                list.RemoveAt(i);
            }
        }
        private void SaveInvoke(in TimeSpan time, TimeSpanAlarmHandler h)
        {
            try
            {
                h?.Invoke(time);
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
        }
    }


    public delegate void DateTimeAlarmHandler(DateTime time);
    public class DateTimeAlarm : Disposable
    {
        public event DateTimeAlarmHandler OnMinAlarm;
        public event DateTimeAlarmHandler OnHourAlarm;
        public event DateTimeAlarmHandler OnDayAlarm;

        private List<DateTimeAlarmHandler> OnMinAlarms = new();
        private List<DateTimeAlarmHandler> OnHourAlarms = new();
        private List<DateTimeAlarmHandler> OnDayAlarms = new();

        private int? lastMIN;
        private int? lastHOR;
        private int? lastDAY;
        public DateTimeAlarm(DateTime time)
        {
            this.lastMIN = time.Minute;
            this.lastHOR = time.Hour;
            this.lastDAY = time.Day;
        }
        public DateTimeAlarm()
        {
        }
        protected override void Disposing()
        {
            OnMinAlarm = null;
            OnHourAlarm = null;
            OnDayAlarm = null;
            OnMinAlarms.Clear();
            OnHourAlarms.Clear();
            OnDayAlarms.Clear();
        }
        public bool Update(in DateTime time)
        {
            if (this.lastMIN.HasValue && this.lastMIN.Value != time.Minute)
            {
                SaveInvoke(in time, OnMinAlarm);
                SaveInvokes(in time, OnMinAlarms);
            }
            if (this.lastHOR.HasValue && this.lastHOR.Value != time.Hour)
            {
                SaveInvoke(in time, OnHourAlarm);
                SaveInvokes(in time, OnHourAlarms);
            }
            if (this.lastDAY.HasValue && this.lastDAY.Value != time.Day)
            {
                SaveInvoke(in time, OnDayAlarm);
                SaveInvokes(in time, OnDayAlarms);
            }
            this.lastMIN = time.Minute;
            this.lastHOR = time.Hour;
            this.lastDAY = time.Day;
            return true;
        }
        private void SaveInvokes(in DateTime time, List<DateTimeAlarmHandler> list)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                var h = list[i];
                SaveInvoke(time, h);
                list.RemoveAt(i);
            }
        }
        private void SaveInvoke(in DateTime time, DateTimeAlarmHandler h)
        {
            try
            {
                h?.Invoke(time);
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
        }
    }
}