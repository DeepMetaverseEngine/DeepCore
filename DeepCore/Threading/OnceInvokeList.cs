using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Threading
{   
    
    /*
    public class SyncMessageQueue<T> where T : class
    {
        private readonly SingleThreadCollectionPool pool;
        private readonly List<T> adding = new List<T>();
        private readonly Action<T> mDoAction;
        private int count = 0;
        public event Action<Exception> OnError;

        public SyncMessageQueue(SingleThreadCollectionPool pool, Action<T> action)
        {
            this.pool = pool;
            this.mDoAction = action;
        }

        /// <summary>
        /// 添加一个消息到队列
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            lock (adding)
            {
                this.adding.Add(item);
                this.count++;
            }
        }
        public void Enqueue(IEnumerable<T> item)
        {
            lock (adding)
            {
                this.adding.AddRange(item);
                this.count++;
            }
        }

        public void Insert(int index, T item)
        {
            lock (adding)
            {
                this.adding.Insert(index, item);
                this.count++;
            }
        }

        /// <summary>
        /// 尝试处理队列中所有消息
        /// </summary>
        /// <param name="action">处理函数</param>
        public void ProcessMessages()
        {
            if (count > 0)
            {
                using (var queue = pool.AllocList<T>())
                {
                    lock (adding)
                    {
                        queue.AddRange(adding);
                        this.adding.Clear();
                        this.count = 0;
                    }
                    for (int i = 0; i < queue.Count; i++)
                    {
                        try
                        {
                            mDoAction(queue[i]);
                        }
                        catch (Exception err)
                        {
                            OnError?.Invoke(err);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            lock (adding)
            {
                this.adding.Clear();
                this.count = 0;
            }
        }
    }

    public class SingleThreadMessageQueue<T> where T : class
    {
        private readonly SingleThreadCollectionPool pool;
        private readonly List<T> adding = new List<T>();
        private readonly Action<T> mDoAction;
        private int count = 0;
        private Action<Exception> mOnError;
        public event Action<Exception> OnError
        {
            add { mOnError += value; }
            remove { mOnError -= value; }
        }


        public SingleThreadMessageQueue(SingleThreadCollectionPool pool, Action<T> action)
        {
            this.pool = pool;
            this.mDoAction = action;
        }

        /// <summary>
        /// 添加一个消息到队列
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            lock (adding)
            {
                this.adding.Add(item);
                this.count++;
            }
        }

        public void Insert(int index, T item)
        {
            lock (adding)
            {
                this.adding.Insert(index, item);
                this.count++;
            }
        }

        /// <summary>
        /// 尝试处理队列中所有消息
        /// </summary>
        /// <param name="action">处理函数</param>
        public void ProcessMessages()
        {
            try
            {
                if (count > 0)
                {
                    using (var queue = pool.AllocList<T>())
                    {
                        lock (adding)
                        {
                            queue.AddRange(adding);
                            this.adding.Clear();
                            this.count = 0;
                        }
                        for (int i = 0; i < queue.Count; i++)
                        {
                            try
                            {
                                mDoAction(queue[i]);
                            }
                            catch (Exception err)
                            {
                                mOnError?.Invoke(err);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                mOnError?.Invoke(err);
            }
        }

        public void Clear()
        {
            lock (adding)
            {
                this.adding.Clear();
                this.count = 0;
            }
        }
    }
    */

    public interface IOnceInvoke
    {
        bool IsDone { get; }
    }

    public class OnceInvokeList<T>
        where T : IOnceInvoke
    {
        private readonly SingleThreadCollectionPool pool;
        private readonly List<T> mInvokeList = new List<T>();
        public OnceInvokeList(SingleThreadCollectionPool pool)
        {
            this.pool = pool;
        }
        public int Count { get { return mInvokeList.Count; } }
        public void Add(T e) { mInvokeList.Add(e); }
        public void Invoke<ST>(in ST state, Action<ST, T> on_invoke)
        {
            if (mInvokeList.Count > 0)
            {
                using (var list = pool.AllocList<T>(mInvokeList))
                {
                    foreach (var e in list)
                    {
                        on_invoke(state, e);
                    }
                }
                for (int i = mInvokeList.Count - 1; i >= 0; --i)
                {
                    var e = mInvokeList[i];
                    if (e.IsDone)
                    {
                        mInvokeList.RemoveAt(i);
                    }
                }
            }
        }
        public void Clear()
        {
            mInvokeList.Clear();
        }
    }
}
