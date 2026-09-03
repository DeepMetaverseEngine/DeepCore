using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DeepCore.Threading
{
    public class UpdateTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();
        private SingleThreadCollectionPool _collectionPool;
        private bool disposedValue;
        public sealed override int MaximumConcurrencyLevel { get { return 1; } }
        public  SingleThreadCollectionPool ObjectPool
        {
            get => _collectionPool;
        }

        public UpdateTaskScheduler(SingleThreadCollectionPool pool)
        {
            this._collectionPool = pool;
        }
        public void Update()
        {
            using (var list = _collectionPool.AllocList<Task>())
            {
                lock (_tasks)
                {
                    list.AddRange(_tasks);
                    _tasks.Clear();
                }
                foreach (var run in list)
                {
                    base.TryExecuteTask(run);
                }
            }
        }
        protected sealed override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
            }
        }

        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued)
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Update();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~UpdateTaskScheduler()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }
    }
}
