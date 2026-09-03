using DeepCore.Components;
using DeepCore.Log;
using DeepCore.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Meta.Layout
{
    public class MetaStage : MetaObjectContainer
    {
        internal protected readonly Logger log;
        internal readonly SingleThreadCollectionPool objectPool;
        private readonly MessageActionQueue<MetaStage> tasks;
        private readonly TimeTaskQueue timer_tasks;
        private readonly List<MetaObject> children = new List<MetaObject>();

        public float CurrentIntervalSEC { get; private set; }
        public float CurrentIntervalMS { get; private set; }

        public MetaStage()
        {
            parent = null;
            root = this;
            objectPool = new SingleThreadCollectionPool();
            log = LoggerFactory.GetLogger(GetType().Name);
            tasks = new MessageActionQueue<MetaStage>();
            tasks.OnError += InvokeError;
            timer_tasks = new TimeTaskQueue(objectPool);
            timer_tasks.OnError += InvokeError;
        }
        protected override void Disposing()
        {
            try
            {
                tasks.Dispose();
                timer_tasks.Dispose();
                base.Disposing();
                ObjectPool.Dispose();
            }
            catch (Exception err)
            {
                InvokeError(err);
            }
        }

        public override int NumChildren => children.Count;
        public override IEnumerable<MetaObject> Children => children;
        protected override void CollectionClearChildren()
        {
            children.Clear();
        }
        protected override bool CollectionRemoveChild(MetaObject c)
        {
            return children.Remove(c);
        }

        public V AddObject<V>(V v) where V : MetaObject
        {
            if (InternalAddChild(v, c =>
            {
                if (children.Contains(c))
                    return false;
                children.Add(c);
                return true;
            }))
            {
                return v;
            }
            else
            {
                return null;
            }
        }
        public bool RemoveObject<V>(V v, bool dispose = false) where V : MetaObject
        {
            return (InternalRemoveChild(v, c => children.Remove(v), dispose));
        }

        private void MainDoTask(Action act)
        {
            try
            {
                act.Invoke();
            }
            catch (Exception err)
            {
                InvokeError(err);
            }
        }

        public void MainUpdate(float intervalMS)
        {
            this.CurrentIntervalSEC = 1000f / intervalMS;
            this.CurrentIntervalMS = intervalMS;
            this.tasks.ProcessMessages(this);
            try
            {
                this.timer_tasks.Update(intervalMS);
            }
            catch (Exception err)
            {
                InvokeError(err);
            }
            try
            {
                this.InternalUpdate(intervalMS);
            }
            catch (Exception err)
            {
                InvokeError(err);
            }
        }

        //---------------------------------------------------------------------------------------------------------
        public void QueueTask(Action action)
        {
            tasks.Enqueue(action);
        }

        public Task<O> QueueTaskAsync<I, O>(Func<I, O> action, I state)
        {
            var tcs = new TaskCompletionSource<O>();
            tasks.Enqueue(() =>
            {
                try
                {
                    tcs.TrySetResult(action(state));
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }
        public Task<O> QueueTaskAsync<O>(Func<O> action)
        {
            var tcs = new TaskCompletionSource<O>();
            tasks.Enqueue(() =>
            {
                try
                {
                    tcs.TrySetResult(action());
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }
        public Task QueueTaskAsync<I>(Action<I> action, I state)
        {
            var tcs = new TaskCompletionSource<int>();
            tasks.Enqueue(() =>
            {
                try
                {
                    action(state);
                    tcs.TrySetResult(1);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }
        public Task QueueTaskAsync(Action action)
        {
            var tcs = new TaskCompletionSource<int>();
            tasks.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(1);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            });
            return tcs.Task;
        }
        public TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler)
        {
            return timer_tasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        public TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler)
        {
            return timer_tasks.AddTimeDelayMS(delayMS, handler);
        }
        public TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler)
        {
            return timer_tasks.AddTimePeriodicMS(intervalMS, handler);
        }
        //---------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------
    }



}
