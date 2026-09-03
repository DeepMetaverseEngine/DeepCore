using DeepCore.Log;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Threading
{
    public class TaskCompletionSourcePool : Disposable
    {
        private readonly Logger log;
        private readonly TimeTaskQueue mTimeTasks;
        private double mLastUpdateTime = 0;

        public TaskCompletionSourcePool(string name, AbstractCollectionPool cpool)
        {
            this.log = LoggerFactory.GetLogger(name);
            this.mTimeTasks = new TimeTaskQueue(cpool);
        }
        protected override void Disposing()
        {
            mTimeTasks.CleanInvoke();
            mTimeTasks.Dispose();
        }
        public TimeTaskMS CreateTimeout(TimeSpan delayMS, TickHandler handler, object state)
        {
            var ttask = mTimeTasks.AddTimeDelayMS((int)delayMS.TotalMilliseconds, handler);
            ttask.UserData = state;
            return ttask;
        }
        public TimeTaskMS CreateTimer(TimeSpan intervalMS, TickHandler handler, object state)
        {
            var ttask = mTimeTasks.AddTimePeriodicMS((int)intervalMS.TotalMilliseconds, handler);
            ttask.UserData = state;
            return ttask;
        }
        public void Update()
        {
            var curTime = DeepCore.CUtils.TickTimeMS;
            if (mLastUpdateTime == 0)
            {
                mLastUpdateTime = curTime;
            }
            var intervalMS = (float)(curTime - mLastUpdateTime);
            mLastUpdateTime = curTime;
            mTimeTasks.Update(intervalMS);
        }
        public virtual TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, StackTrace stack, TimeSpan timeoutMS)
        {
            var tcs = new TaskCompletionSource<T>();
            if (timeoutMS != Timeout.InfiniteTimeSpan)
            {
                var delay = this.CreateTimeout(timeoutMS, (t) =>
                {
                    if (tcs.TrySetCanceled())
                    {
                        log.Warn(name + " : Async Task Timeout, Canceled!!!" + Environment.NewLine + stack);
                    }
                }, tcs);
                tcs.Task.ContinueWith(t => { delay.Dispose(); });
            }
            return tcs;
        }
        public virtual TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, StackTrace stack, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout)
        {
            var tcs = new TaskCompletionSource<T>();
            if (timeoutMS != Timeout.InfiniteTimeSpan)
            {
                var delay = this.CreateTimeout(timeoutMS, (t) =>
                {
                    if (tcs.TrySetCanceled())
                    {
                        log.Warn(name + " : Async Task Timeout, Canceled!!!" + Environment.NewLine + stack);
                    }
                    timeout(tcs);
                }, tcs);
                tcs.Task.ContinueWith(t => { delay.Dispose(); });
            }
            return tcs;
        }
        public virtual TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, StackTrace stack, TaskCreationOptions options, TimeSpan timeoutMS)
        {
            var tcs = new TaskCompletionSource<T>(options);
            if (timeoutMS != Timeout.InfiniteTimeSpan)
            {
                var delay = this.CreateTimeout(timeoutMS, (t) =>
                {
                    if (tcs.TrySetCanceled())
                    {
                        log.Warn(name + " : Async Task Timeout, Canceled!!!" + Environment.NewLine + stack);
                    }
                }, tcs);
                tcs.Task.ContinueWith(t => { delay.Dispose(); });
            }
            return tcs;
        }
        public virtual TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, StackTrace stack, TaskCreationOptions options, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout)
        {
            var tcs = new TaskCompletionSource<T>(options);
            if (timeoutMS != Timeout.InfiniteTimeSpan)
            {
                var delay = this.CreateTimeout(timeoutMS, (t) =>
                {
                    if (tcs.TrySetCanceled())
                    {
                        log.Warn(name + " : Async Task Timeout, Canceled!!!" + Environment.NewLine + stack);
                    }
                    timeout(tcs);
                }, tcs);
                tcs.Task.ContinueWith(t => { delay.Dispose(); });
            }
            return tcs;
        }
    }

    public class TimerTaskCompletionSourcePool : TaskCompletionSourcePool
    {
        private readonly System.Threading.Timer mTimer;
        public TimerTaskCompletionSourcePool(string name, AbstractCollectionPool cpool, int intervalMS = 100) : base(name, cpool)
        {
            this.mTimer = new System.Threading.Timer(OnTick, this, 0, intervalMS);
        }
        protected override void Disposing()
        {
            mTimer.Dispose();
            base.Disposing();
        }
        protected void OnTick(object state)
        {
            base.Update();
        }

    }
}
