using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Threading
{
    public class SyncTaskExecutor : ITaskExecutor
    {
        public static SyncTaskExecutor Instance { get; private set; } = new SyncTaskExecutor();
        private SyncTaskExecutor() { }
        public Task<TResult> FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }
        public Task Execute(Action action)
        {
            action();
            return Task.CompletedTask;
        }
        public Task Execute(Func<Task> action)
        {
            return action();
        }
        public Task<TResult> Execute<TResult>(Func<TResult> function)
        {
            var result = function();
            return Task.FromResult(result);
        }
        public Task<TResult> Execute<TResult>(Func<Task<TResult>> function)
        {
            return function();
        }
        public Task Execute<TInput>(Action<TInput> action, TInput state)
        {
            action(state);
            return Task.CompletedTask;
        }
        public Task Execute<TInput>(Func<TInput, Task> action, TInput state)
        {
            return action(state);
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            var rst = function(state);
            return Task.FromResult(rst);
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return function(state);
        }
        public Task Execute(Task task)
        {
            return task;
        }
        public Task<TResult> Execute<TResult>(Task<TResult> task)
        {
            return task;
        }
        public Task Delay(TimeSpan dueTime)
        {
            return Task.Delay((int)dueTime.TotalMilliseconds);
        }
        public Task Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime)
        {
            return Task.Delay((int)dueTime.TotalMilliseconds).ContinueWith(t => callback(state));
        }
        public Task<TResult> Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime)
        {
            return Task.Delay((int)dueTime.TotalMilliseconds).ContinueWith(t => callback(state));
        }
    }



}
