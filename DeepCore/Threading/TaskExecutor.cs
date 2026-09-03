using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Threading
{
    /// <summary>
    /// 主线程执行，通常用于线程异步调用保证逻辑在主线程执行。
    /// </summary>
    public interface ITaskExecutor
    {
        public static ITaskExecutor Default=> SyncTaskExecutor.Instance;

        Task<TResult> FromResult<TResult>(TResult result);

        Task Execute(Action action);
        Task Execute<TInput>(Action<TInput> action, TInput state);
        Task<TResult> Execute<TResult>(Func<TResult> function);
        Task<TResult> Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state);

        Task Execute(Task task);
        Task<TResult> Execute<TResult>(Task<TResult> task);

        Task Execute(Func<Task> action);
        Task<TResult> Execute<TResult>(Func<Task<TResult>> function);
        Task Execute<TInput>(Func<TInput, Task> action, TInput state);
        Task<TResult> Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state);

        Task Delay(TimeSpan dueTime);
        Task Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime);
        Task<TResult> Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime);
    }


}
