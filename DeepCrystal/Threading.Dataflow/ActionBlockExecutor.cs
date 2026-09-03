using DeepCore.Threading;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepCrystal.Threading.Dataflow
{
    public class ActionBlockExecutor : ITaskExecutor
    {
        private readonly ActionBlock<Action> actionBlock;

        public ActionBlockExecutor()
        {
            this.actionBlock = new ActionBlock<Action>(new Action<Action>(actionBlockMain));
        }
        public ActionBlockExecutor(ActionBlock<Action> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            this.actionBlock = new ActionBlock<Action>(new Action<Action>(actionBlockMain), dataflowBlockOptions);
        }
        public void Complete()
        {
            this.actionBlock.Complete();
        }
        public Task Completion
        {
            get => this.actionBlock.Completion;
        }
        public int InputCount
        {
            get => this.actionBlock.InputCount;
        }

        private void actionBlockMain(Action msg)
        {
            msg.Invoke();
        }


        public bool Post(Action action)
        {
            return actionBlock.Post(action);
        }
        public bool Post<T>(Action<T> action, T arg)
        {
            return actionBlock.Post(() => action(arg));
        }
        public bool Post<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return actionBlock.Post(() => action(arg1, arg2));
        }
        public bool Post<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return actionBlock.Post(() => action(arg1, arg2, arg3));
        }

        public Task RunAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action();
                    tcs.TrySetResult(false);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task RunAsync<T>(Action<T> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task RunAsync<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task RunAsync<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task<TResult> RunAsync<TResult>(Func<TResult> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    var ret = action();
                    tcs.TrySetResult(ret);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task<TResult> RunAsync<T, TResult>(Func<T, TResult> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task<TResult> RunAsync<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task<TResult> RunAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task<TResult> RunAsync<TResult>(Func<Task<TResult>> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        if (actionBlock.Post(() => { tcs.TrySetCompletionFrom(t); }) == false)
                        {
                            tcs.TrySetCanceled();
                        }
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }

        public Task<TResult> RunAsync<T, TResult>(Func<T, Task<TResult>> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task<TResult> RunAsync<T1, T2, TResult>(Func<T1, T2, Task<TResult>> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task<TResult> RunAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task RunAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<int>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        if (actionBlock.Post(() => {
                            tcs.TrySetCompletionFrom(t, 1);
                        }) == false)
                        {
                            tcs.TrySetCanceled();
                        }
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task RunAsync<T>(Func<T, Task> action, T arg)
        {
            return RunAsync(() => action(arg));
        }

        public Task<TResult> RunAsync<TResult>(Task<TResult> task)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                task.ContinueWith(t =>
                {
                    if (actionBlock.Post(run) == false)
                    {
                        tcs.TrySetCanceled();
                    }
                    void run()
                    {
                        tcs.TrySetCompletionFrom(t);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }
        public Task RunAsync(Task task)
        {
            var tcs = new TaskCompletionSource<int>();
            try
            {
                task.ContinueWith(t =>
                {
                    if (actionBlock.Post(run) == false)
                    {
                        tcs.TrySetCanceled();
                    }
                    void run()
                    {
                        tcs.TrySetCompletionFrom(t, 1);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        tcs.TrySetCompletionFrom(t);
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T, TResult>(Func<T, Task<TResult>> action, T arg)
        {
            return PostAsync(() => action(arg));
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T1, T2, TResult>(Func<T1, T2, Task<TResult>> action, T1 arg1, T2 arg2)
        {
            return PostAsync(() => action(arg1, arg2));
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return PostAsync(() => action(arg1, arg2, arg3));
        }
        //---------------------------------------------------------------------------------------------------------------------------------------


        Task ITaskExecutor.Execute<TInput>(Action<TInput> callback, TInput state)
        {
            return this.RunAsync<TInput>(callback, state);
        }
        Task ITaskExecutor.Execute(Action callback)
        {
            return this.RunAsync(callback);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return this.RunAsync<TInput, TResult>(function, state);
        }
        Task ITaskExecutor.Execute<TInput>(Func<TInput, Task> function, TInput state)
        {
            return this.RunAsync(function, state);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return this.RunAsync<TInput, TResult>(function, state);
        }
        Task ITaskExecutor.Execute(Func<Task> function)
        {
            return this.RunAsync(function);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<TResult> function)
        {
            return this.RunAsync<TResult>(function);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<Task<TResult>> function)
        {
            return this.RunAsync<TResult>(function);
        }
        Task ITaskExecutor.Execute(Task task)
        {
            return this.RunAsync(task);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Task<TResult> task)
        {
            return this.RunAsync<TResult>(task);
        }
        Task<TResult> ITaskExecutor.FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }
        Task ITaskExecutor.Delay(TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            return this.RunAsync(Task.Delay(delayMS));
        }
        async Task ITaskExecutor.Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            await this.RunAsync(callback, state);
        }
        async Task<TResult> ITaskExecutor.Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            return await this.RunAsync(callback, state);
        }
    }


    public class SingleTaskExecutor : ITaskExecutor
    {
        private readonly ActionBlock<Action> actionBlock;
        private SemaphoreSlim accLock = new SemaphoreSlim(1, 1);
        public SingleTaskExecutor()
        {
            this.actionBlock = new ActionBlock<Action>(new Action<Action>(actionBlockMain));
        }
        public SingleTaskExecutor(ActionBlock<Action> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            this.actionBlock = new ActionBlock<Action>(new Action<Action>(actionBlockMain), dataflowBlockOptions);
        }
        public void Complete()
        {
            this.actionBlock.Complete();
        }
        public Task Completion
        {
            get => this.actionBlock.Completion;
        }
        public int InputCount
        {
            get => this.actionBlock.InputCount;
        }

        private void actionBlockMain(Action msg)
        {
            msg.Invoke();
        }


        public bool Post(Action action)
        {
            return actionBlock.Post(action);
        }
        public bool Post<T>(Action<T> action, T arg)
        {
            return actionBlock.Post(() => action(arg));
        }
        public bool Post<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return actionBlock.Post(() => action(arg1, arg2));
        }
        public bool Post<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return actionBlock.Post(() => action(arg1, arg2, arg3));
        }

        public Task RunAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action();
                    tcs.TrySetResult(false);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task RunAsync<T>(Action<T> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task RunAsync<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task RunAsync<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task<TResult> RunAsync<TResult>(Func<TResult> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    var ret = action();
                    tcs.TrySetResult(ret);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task<TResult> RunAsync<T, TResult>(Func<T, TResult> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task<TResult> RunAsync<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task<TResult> RunAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task<TResult> RunAsync<TResult>(Func<Task<TResult>> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        if (actionBlock.Post(() => { tcs.TrySetCompletionFrom(t); }) == false)
                        {
                            tcs.TrySetCanceled();
                        }
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }

        public Task<TResult> RunAsync<T, TResult>(Func<T, Task<TResult>> action, T arg)
        {
            return RunAsync(() => action(arg));
        }
        public Task<TResult> RunAsync<T1, T2, TResult>(Func<T1, T2, Task<TResult>> action, T1 arg1, T2 arg2)
        {
            return RunAsync(() => action(arg1, arg2));
        }
        public Task<TResult> RunAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunAsync(() => action(arg1, arg2, arg3));
        }

        public Task RunAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<int>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        if (actionBlock.Post(() =>
                        {
                            tcs.TrySetCompletionFrom(t, 1);
                        }) == false)
                        {
                            tcs.TrySetCanceled();
                        }
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        public Task RunAsync<T>(Func<T, Task> action, T arg)
        {
            return RunAsync(() => action(arg));
        }

        public Task<TResult> RunAsync<TResult>(Task<TResult> task)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                task.ContinueWith(t =>
                {
                    if (actionBlock.Post(run) == false)
                    {
                        tcs.TrySetCanceled();
                    }
                    void run()
                    {
                        tcs.TrySetCompletionFrom(t);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }
        public Task RunAsync(Task task)
        {
            var tcs = new TaskCompletionSource<int>();
            try
            {
                task.ContinueWith(t =>
                {
                    if (actionBlock.Post(run) == false)
                    {
                        tcs.TrySetCanceled();
                    }
                    void run()
                    {
                        tcs.TrySetCompletionFrom(t, 1);
                    }
                });
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            return tcs.Task;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (actionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    action().ContinueWith(t =>
                    {
                        tcs.TrySetCompletionFrom(t);
                    });
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T, TResult>(Func<T, Task<TResult>> action, T arg)
        {
            return PostAsync(() => action(arg));
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T1, T2, TResult>(Func<T1, T2, Task<TResult>> action, T1 arg1, T2 arg2)
        {
            return PostAsync(() => action(arg1, arg2));
        }
        /// <summary>
        /// Result Task 本身不在ActionBlock内运行
        /// </summary>
        public Task<TResult> PostAsync<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return PostAsync(() => action(arg1, arg2, arg3));
        }
        //---------------------------------------------------------------------------------------------------------------------------------------


        Task ITaskExecutor.Execute<TInput>(Action<TInput> callback, TInput state)
        {
            return this.RunAsync<TInput>(callback, state);
        }
        Task ITaskExecutor.Execute(Action callback)
        {
            return this.RunAsync(callback);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return this.RunAsync<TInput, TResult>(function, state);
        }
        Task ITaskExecutor.Execute<TInput>(Func<TInput, Task> function, TInput state)
        {
            return this.RunAsync(function, state);
        }
        Task<TResult> ITaskExecutor.Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return this.RunAsync<TInput, TResult>(function, state);
        }
        Task ITaskExecutor.Execute(Func<Task> function)
        {
            return this.RunAsync(function);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<TResult> function)
        {
            return this.RunAsync<TResult>(function);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Func<Task<TResult>> function)
        {
            return this.RunAsync<TResult>(function);
        }
        Task ITaskExecutor.Execute(Task task)
        {
            return this.RunAsync(task);
        }
        Task<TResult> ITaskExecutor.Execute<TResult>(Task<TResult> task)
        {
            return this.RunAsync<TResult>(task);
        }
        Task<TResult> ITaskExecutor.FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }
        Task ITaskExecutor.Delay(TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            return this.RunAsync(Task.Delay(delayMS));
        }
        async Task ITaskExecutor.Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            await this.RunAsync(callback, state);
        }
        async Task<TResult> ITaskExecutor.Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime)
        {
            var delayMS = (int)dueTime.TotalMilliseconds;
            await Task.Delay(delayMS);
            return await this.RunAsync(callback, state);
        }
    }
}
