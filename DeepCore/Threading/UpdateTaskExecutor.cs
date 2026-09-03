using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;
using Barrier = System.Threading.Barrier;
using Monitor = System.Threading.Monitor;
using IDisposable = System.IDisposable;
using TaskEnum = System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task>;
using TaskQueue = System.Collections.Generic.Queue<System.Threading.Tasks.Task>;
using Enumerable = System.Linq.Enumerable;
using ObjectDisposedException = System.ObjectDisposedException;
using System.Threading;

namespace DeepCore.Threading
{
    /// <summary>
    /// 保证所有任务在主线程执行
    /// </summary>
    public class UpdateTaskExecutor : Disposable, ITaskExecutor
    {
        private CancellationToken cancellationToken;
        private TaskCreationOptions creationOptions = TaskCreationOptions.PreferFairness;
        private TaskContinuationOptions continuationOptions = TaskContinuationOptions.HideScheduler;

        private Queue<Action> actionBlock;
        private SingleThreadCollectionPool collectionPool;
        private UpdateTaskScheduler scheduler;
        private TaskFactory factory;

        public SingleThreadCollectionPool ObjectPool
        {
            get => collectionPool;
        }
        public TaskFactory TaskFactory
        {
            get => factory;
        }
        public UpdateTaskExecutor()
        {
            this.cancellationToken = new CancellationToken();
            this.actionBlock = new Queue<Action>();
            this.collectionPool = new SingleThreadCollectionPool();
            this.scheduler = new UpdateTaskScheduler(collectionPool);
            this.factory = new TaskFactory(cancellationToken, creationOptions, continuationOptions, scheduler);
        }

        public void Update()
        {
            scheduler.Update();
            using (var list = collectionPool.AllocList<Action>())
            {
                lock (actionBlock)
                {
                    list.AddRange(actionBlock);
                    actionBlock.Clear();
                }
                foreach (var run in list)
                {
                    run.Invoke();
                }
            }
        }
        public void QueueTask(Action action)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
            lock (actionBlock)
            {
                actionBlock.Enqueue(action);
            }
        }

        protected override void Disposing()
        {
            using (var list = collectionPool.AllocList<Action>())
            {
                lock (actionBlock)
                {
                    list.AddRange(actionBlock);
                }
                foreach (var run in list)
                {
                    run.Invoke();
                }
            }
            collectionPool.Dispose();
        }
        //--------------------------------------------------------------------------------------------------------------------------
        #region Executor
        protected virtual Task<TResult> PostExecuteAsync<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return factory.StartNew((st) => function((TInput)st), state);
        }
        protected virtual Task<TResult> PostExecuteAsync<TResult>(Func<TResult> function)
        {
            return factory.StartNew(function);
        }
        protected virtual Task PostExecuteAsync<TInput>(Action<TInput> function, TInput state)
        {
            return factory.StartNew((st) => function((TInput)st), state);
        }
        protected virtual Task PostExecuteAsync(Action function)
        {
            return factory.StartNew(function);
        }

        protected virtual Task<TResult> PostExecuteAsync<TResult>(Task<TResult> task)
        {
            return task.ContinueWith(t => t.GetResultAs(), this.cancellationToken, this.continuationOptions, this.scheduler);
        }
        protected virtual Task PostExecuteAsync(Task task)
        {
            return task.ContinueWith(t => { }, this.cancellationToken, this.continuationOptions, this.scheduler);
        }
        protected virtual async Task<TResult> PostExecuteAsync<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            var task1 = await factory.StartNew((st) => function((TInput)st), state);
            var result = await task1.ContinueWith(t => t.GetResultAs(), this.cancellationToken, this.continuationOptions, this.scheduler);
            return result;
        }
        protected virtual async Task<TResult> PostExecuteAsync<TResult>(Func<Task<TResult>> function)
        {
            var task1 = await factory.StartNew(function);
            var result = await task1.ContinueWith(t => t.GetResultAs(), this.cancellationToken, this.continuationOptions, this.scheduler);
            return result;
        }
        protected virtual async Task PostExecuteAsync<TInput>(Func<TInput, Task> function, TInput state)
        {
            var task1 = await factory.StartNew(st => function((TInput)st), state);
            await task1.ContinueWith(t => { }, this.cancellationToken, this.continuationOptions, this.scheduler);
        }
        protected virtual async Task PostExecuteAsync(Func<Task> function)
        {
            var task1 = await factory.StartNew(function);
            await task1.ContinueWith(t => { }, this.cancellationToken, this.continuationOptions, this.scheduler);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        public Task<TResult> FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }
        public Task Execute(Action action)
        {
            return this.PostExecuteAsync(action);
        }
        public Task Execute(Func<Task> action)
        {
            return this.PostExecuteAsync(action);
        }
        public Task<TResult> Execute<TResult>(Func<TResult> function)
        {
            return this.PostExecuteAsync(function);
        }
        public Task<TResult> Execute<TResult>(Func<Task<TResult>> function)
        {
            return this.PostExecuteAsync(function);
        }
        public Task Execute<TInput>(Action<TInput> action, TInput state)
        {
            return this.PostExecuteAsync(action, state);
        }
        public Task Execute<TInput>(Func<TInput, Task> action, TInput state)
        {
            return this.PostExecuteAsync(action, state);
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return this.PostExecuteAsync(function, state);
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return this.PostExecuteAsync(function, state);
        }
        public Task Execute(Task task)
        {
            return this.PostExecuteAsync(task);
        }
        public Task<TResult> Execute<TResult>(Task<TResult> task)
        {
            return this.PostExecuteAsync(task);
        }
        public Task Delay(TimeSpan dueTime)
        {
            return this.PostExecuteAsync(Task.Delay((int)dueTime.TotalMilliseconds));
        }
        public async Task Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime)
        {
            await Task.Delay((int)dueTime.TotalMilliseconds);
            await this.PostExecuteAsync(callback, state);
        }
        public async Task<TResult> Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime)
        {
            await Task.Delay((int)dueTime.TotalMilliseconds);
            return await this.PostExecuteAsync(callback, state);
        }







    }



}
