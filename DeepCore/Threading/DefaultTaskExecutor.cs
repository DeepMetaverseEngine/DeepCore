using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Threading
{
    public struct DefaultTaskExecutor : ITaskExecutor
    {
        public Task<TResult> FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }
        public Task Execute(Action action)
        {
            return Task.Run(action);
        }
        public Task Execute(Func<Task> action)
        {
            return Task.Run(action);
        }
        public Task<TResult> Execute<TResult>(Func<TResult> function)
        {
            return Task.Run(function);
        }
        public Task<TResult> Execute<TResult>(Func<Task<TResult>> function)
        {
            return Task.Run(function);
        }
        public Task Execute<TInput>(Action<TInput> action, TInput state)
        {
            return Task.Run(() => { action(state); });
        }
        public Task Execute<TInput>(Func<TInput, Task> action, TInput state)
        {
            return Task.Run(() => { return action(state); });
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state)
        {
            return Task.Run(() => { return function(state); });
        }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state)
        {
            return Task.Run(() => { return function(state); });
        }
        public Task Execute(Task task)
        {
            return Task.Run(() => task);
        }
        public Task<TResult> Execute<TResult>(Task<TResult> task)
        {
            return Task.Run(() => task);
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