using DeepCore.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DeepCore.Concurrent
{
    public class ThreadExecutor : Executor
    {
        private AtomicLong idgen = new AtomicLong(0);
        private Logger log;
        private Thread[] processors;
        private Queue<WorkTask> workQueue = new Queue<WorkTask>();
        private bool shutdown = false;

        public ThreadExecutor(int processorCount)
        {
            this.log = LoggerFactory.GetLogger(string.Format("ThreadExecutor"));
            this.processors = new Thread[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                this.processors[i] = new Thread(internal_run);
                this.processors[i].IsBackground = true;
                this.processors[i].Start();
            }
        }

        public void Shutdown()
        {
            log.Debug("processors shutting down ...");
            shutdown = true;
            lock (workQueue)
            {
                foreach (WorkTask task in workQueue)
                {
                    task.Cancel();
                }
            }
            for (int i = 0; i < processors.Length; i++)
            {
                this.processors[i].Join();
            }
            log.Debug("processors shutdown !");
        }

        private void internal_run()
        {
            try
            {
                while (!shutdown)
                {
                    WorkTask task = null;
                    int taskCount;
                    lock (workQueue)
                    {
                        taskCount = workQueue.Count;
                        if (taskCount > 0)
                        {
                            task = workQueue.Dequeue();
                        }
                        else
                        {
                            Monitor.Wait(workQueue);
                            continue;
                        }
                    }
                    try
                    {
                        task.Run();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
            catch (Exception err)
            {
                log.Fatal(err.Message, err);
            }
        }

        private void internal_queue_task(WorkTask task)
        {
            lock (workQueue)
            {
                workQueue.Enqueue(task);
                Monitor.Pulse(workQueue);
            }
        }

        public Future Execute(Action command)
        {
            WorkTask task = new WorkTask(this, command);
            internal_queue_task(task);
            return task;
        }

        public Future Schedule(Action r, int delay)
        {
            return new ScheduledWorkTask(this, r, delay, System.Threading.Timeout.Infinite);
        }
        public Future ScheduleAtFixedRate(Action r, int initial, int period)
        {
            return new ScheduledWorkTask(this, r, initial, period);
        }

        private class WorkTask : Future
        {
            internal readonly ThreadExecutor exec;
            internal readonly long id;
            internal readonly Action action;
            internal bool is_cancel = false;
            public WorkTask(ThreadExecutor exec, Action action)
            {
                this.exec = exec;
                this.id = exec.idgen.IncrementAndGet();
                this.action = action;
            }
            public bool Cancel()
            {
                lock (this)
                {
                    this.is_cancel = true;
                }
                return is_cancel;
            }
            public long ID { get { return id; } }
            public bool IsCancelled { get { lock (this) { return is_cancel; } } }

            internal virtual void Run()
            {
                lock (this)
                {
                    if (is_cancel) { return; }
                }
                action();
            }
        }

        private class ScheduledWorkTask : WorkTask
        {
            internal readonly int initial;
            internal readonly int period;
            internal Timer timer;

            public ScheduledWorkTask(ThreadExecutor exec, Action command, int initial, int period)
                : base(exec, command)
            {
                this.initial = initial;
                this.period = period;
                this.timer = new Timer(timeout, command, initial, period);
            }
            private void timeout(object state)
            {
                lock (this)
                {
                    if (is_cancel)
                    {
                        this.timer.Dispose();
                        return;
                    }
                    if (period == Timeout.Infinite)
                    {
                        this.timer.Dispose();
                    }
                }
                exec.internal_queue_task(this);
            }
        }
    }



    public class SingleThreadExecutor : ThreadExecutor
    {
        public SingleThreadExecutor() : base(1)
        {

        }
    }


}
