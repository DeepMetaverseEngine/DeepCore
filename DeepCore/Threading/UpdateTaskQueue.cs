using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.Threading
{
    /*
    public class UpdateTaskQueue : Disposable
    {
        private Queue<Func<Task>> invoking = new Queue<Func<Task>>();
        private Task running;
        public void Clear()
        {
            invoking.Clear();
        }
        protected override void Disposing()
        {
            while (invoking.TryDequeue(out var func))
            {
                try
                {
                    func();
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
            running = null;
        }
        public void Add(Func<Task> invoke)
        {
            invoking.Enqueue(async () =>
            {
                await invoke();
            });
        }
        public void Add(Action invoke)
        {
            invoking.Enqueue(() =>
            {
                invoke();
                return Task.CompletedTask;
            });
        }
        public void Update()
        {
            if (!IsDisposing)
            {
                while (IsDone(running) && invoking.TryDequeue(out var func))
                {
                    try
                    {
                        running = func();
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }
        }
        static bool IsDone(Task task)
        {
            if (task == null) return true;
            return task.IsCompleted || task.IsFaulted || task.IsCanceled || task.IsCompletedSuccessfully;
        }
    }
    */
    public class UpdateTaskQueue<T> : Disposable
    {
        private T t;
        private Queue<Func<T, Task>> invoking = new();
        private Task running;
        public UpdateTaskQueue(T t)
        {
            this.t = t;
        }
        public void Clear()
        {
            invoking.Clear();
        }
        protected override void Disposing()
        {
            while (invoking.TryDequeue(out var func))
            {
                try
                {
                    func(t);
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
            running = null;
        }
        public void Add(Func<T, Task> invoke)
        {
            invoking.Enqueue(async (t) =>
            {
                await invoke(t);
            });
        }
        public void Add(Action<T> invoke)
        {
            invoking.Enqueue((t) =>
            {
                invoke(t);
                return Task.CompletedTask;
            });
        }
        public void Update()
        {
            if (!IsDisposing)
            {
                while (IsDone(running) && invoking.TryDequeue(out var func))
                {
                    try
                    {
                        running = func(t);
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace();
                    }
                }
            }
        }
        static bool IsDone(Task task)
        {
            if (task == null) return true;
            return task.IsCompleted || task.IsFaulted || task.IsCanceled || task.IsCompletedSuccessfully;
        }
    }
}
