using DeepCore;
using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    public static class TaskExt
    {
        private static Logger log = new LazyLogger(nameof(Task));
        public static void NoWait(this Task task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
        }
        public static void Forget(this Task task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
        }
        public static T WaitForResult<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }
        public static T WaitForResult<T>(this Task<T> task, int millisecondsTimeout)
        {
            task.Wait(millisecondsTimeout);
            return task.Result;
        }
        public static void ContinueWithCallback<T>(this Task<T> task, Action<T, Exception> callback)
        {
            task.ContinueWith(t =>
            {
                if (task.IsCanceled)
                {
                    callback(default(T), t.Exception);
                }
                if (t.IsCompleted)
                {
                    callback(t.Result, t.Exception);
                }
                else
                {
                    callback(default(T), t.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        public static void OnComplete<T>(this Task<T> task, Action<Task<T>> callback)
        {
            task.ContinueWith(callback, TaskContinuationOptions.ExecuteSynchronously);
        }
        public static void OnComplete(this Task task, Action<Task> callback)
        {
            task.ContinueWith(callback, TaskContinuationOptions.ExecuteSynchronously);
        }
        public static T GetResultAs<T>(this Task<T> task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
            if (task.IsCanceled)
            {
                return default(T);
            }
            if (task.IsFaulted)
            {
                return default(T);
            }
            if (task.IsCompleted)
            {
                return task.Result;
            }
            return default(T);
        }
        public static string GetResultToString<T>(this Task<T> task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
            if (task.IsCanceled)
            {
                return null;
            }
            if (task.IsFaulted)
            {
                return null;
            }
            if (task.IsCompleted)
            {
                var rst = (task.Result);
                return rst.ToString();
            }
            return null;
        }
        public static R GetResultAs<T, R>(this Task<T> task) where T : IConvertible
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
            if (task.IsCanceled)
            {
                return default(R);
            }
            if (task.IsFaulted)
            {
                return default(R);
            }
            if (task.IsCompleted)
            {
                var rst = (task.Result);
                return CUtils.ConvertTo<R>(rst);
            }
            return default(R);
        }
        public static R GetResultAs<R>(this Task<object> task)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception.Message, task.Exception);
            }
            if (task.IsCanceled)
            {
                return default(R);
            }
            if (task.IsFaulted)
            {
                return default(R);
            }
            if (task.IsCompleted)
            {
                var rst = (task.Result);
                return CUtils.ConvertTo<R>(rst);//(R)Convert.ChangeType(rst, typeof(R));
            }
            return default(R);
        }




        public static void TrySetCompletionFrom<T>(this TaskCompletionSource<T> source, Task<T> task)
        {
            try
            {
                if (task.IsCanceled)
                {
                    source.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    source.TrySetException(task.Exception);
                }
                else
                {
                    if (task.Exception != null)
                    {
                        source.TrySetException(task.Exception);
                    }
                    else
                    {
                        source.TrySetResult(task.Result);
                    }
                }
            }
            catch (Exception err)
            {
                source.TrySetException(err);
            }
        }

        public static void TrySetCompletionFrom<T>(this TaskCompletionSource<T> source, Task task, T result)
        {
            try
            {
                if (task.IsCanceled)
                {
                    source.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    source.TrySetException(task.Exception);
                }
                else
                {
                    if (task.Exception != null)
                    {
                        source.TrySetException(task.Exception);
                    }
                    else
                    {
                        source.TrySetResult(result);
                    }
                }
            }
            catch (Exception err)
            {
                source.TrySetException(err);
            }
        }
    }
}
