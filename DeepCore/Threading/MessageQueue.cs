using System;
using System.Collections.Generic;

namespace DeepCore.Threading
{
    public class MessageQueue<T> : Disposable where T : class
    {
        private readonly Action<T> doAction;
        private readonly List<T> adding = new List<T>();
        private readonly List<T> forlist = new List<T>();
        private int count = 0;
        public event Action<Exception> OnError;

        public MessageQueue(Action<T> action)
        {
            this.doAction = action;
        }
        protected override void Disposing()
        {
            lock (adding)
            {
                adding.Clear();
            }
        }
        public void Enqueue(T item)
        {
            lock (adding)
            {
                adding.Add(item);
                count++;
            }
        }
        public void Enqueue(IEnumerable<T> item)
        {
            lock (adding)
            {
                adding.AddRange(item);
                count++;
            }
        }
        public void Insert(int index, T item)
        {
            lock (adding)
            {
                adding.Insert(index, item);
                count++;
            }
        }
        public void ProcessMessages()
        {
            if (count > 0)
            {
                forlist.Clear();
                try
                {
                    lock (adding)
                    {
                        forlist.AddRange(adding);
                        adding.Clear();
                        count = 0;
                    }
                    for (int i = 0; i < forlist.Count; i++)
                    {
                        try
                        {
                            doAction(forlist[i]);
                        }
                        catch (Exception err)
                        {
                            if (OnError != null)
                            {
                                OnError.Invoke(err);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                finally
                {
                    forlist.Clear();
                }
            }
        }

    }

    public class ActionQueue<T> : Disposable where T : class
    {
        private readonly List<Action<T>> adding = new();
        private readonly List<Action<T>> forlist = new();
        private int count = 0;
        public event Action<Exception> OnError;
        public ActionQueue()
        {
        }
        protected override void Disposing()
        {
            lock (adding)
            {
                adding.Clear();
            }
        }
        public void Enqueue(Action<T> item)
        {
            lock (adding)
            {
                adding.Add(item);
                count++;
            }
        }
        public void Insert(int index, Action<T> item)
        {
            lock (adding)
            {
                adding.Insert(index, item);
                count++;
            }
        }
        public void ProcessMessages(T arg)
        {
            if (count > 0)
            {
                forlist.Clear();
                try
                {
                    lock (adding)
                    {
                        forlist.AddRange(adding);
                        adding.Clear();
                        count = 0;
                    }
                    for (int i = 0; i < forlist.Count; i++)
                    {
                        try
                        {
                            forlist[i](arg);
                        }
                        catch (Exception err)
                        {
                            if (OnError != null)
                            {
                                OnError.Invoke(err);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                finally
                {
                    forlist.Clear();
                }
            }
        }
    }



    public class MessageActionQueue<T> : Disposable
    {
        private HashMap<Type, Stack<MessageTuple>> pool = new();
        private List<MessageTuple> adding = new();
        private List<MessageTuple> forlist = new();
        private int count = 0;
        public event Action<Exception> OnError;

        public MessageActionQueue()
        {

        }
        protected override void Disposing()
        {
            this.pool.Clear();
            this.pool = null;
            this.adding.Clear();
            this.adding = null;
            this.forlist.Clear();
            this.forlist = null;
        }
        //-------------------------------------------------------------------------------
        class MessageTuple
        {
            internal MessageActionQueue<T> queue;
            internal Delegate Invoker;
            internal Action<T, MessageTuple> Processor;
            internal virtual void Cleanup()
            {
                Processor = null;
                Invoker = null;
                queue.Release(this);
            }
            public void Process(T v)
            {
                try
                {
                    Processor?.Invoke(v, this);
                }
                finally
                {
                    Cleanup();
                }
            }
        }
        class MessageTuple<ST> : MessageTuple
        {
            internal ST State;
            internal override void Cleanup()
            {
                State = default(ST);
                base.Cleanup();
            }
        }
        //-------------------------------------------------------------------------------
        private void Release(MessageTuple tuple)
        {
            lock (pool)
            {
                var list = pool.GetOrNew(tuple.GetType());
                list.Push(tuple);
            }
        }
        private bool TryPopTuple<ST>(out MessageTuple<ST> item)
        {
            lock (pool)
            {
                var type = typeof(MessageTuple<ST>);
                if (pool.TryGetValue(type, out var group) && group.TryPop(out var _item))
                {
                    item = _item as MessageTuple<ST>;
                    return true;
                }
            }
            item = null;
            return false;
        }
        private MessageTuple AllocTuple<ST>(ST state, Action<T, ST> invoker)
        {
            if (TryPopTuple<ST>(out var wrap))
            {
                wrap.queue = this;
                wrap.State = state;
                wrap.Invoker = invoker;
                wrap.Processor = static (t, w) =>
                {
                    ((Action<T, ST>)w.Invoker).Invoke(t, ((MessageTuple<ST>)w).State);
                };
                return wrap;
            }
            return new MessageTuple<ST>()
            {
                queue = this,
                State = state,
                Invoker = invoker,
                Processor = static (t, w) =>
                {
                    ((Action<T, ST>)w.Invoker).Invoke(t, ((MessageTuple<ST>)w).State);
                },
            };
        }
        //-------------------------------------------------------------------------------
        public bool Enqueue(Action action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(action, static (t, st) => st.Invoke());
            return EnqueueInternal(wrap);
        }
        public bool Enqueue(Action<T> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(action, static (t, st) => st.Invoke(t));
            return EnqueueInternal(wrap);
        }
        public bool Enqueue<ST>(ST state, Action<T, ST> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(state, action);
            return EnqueueInternal(wrap);
        }
        public bool Enqueue<ST>(ST state, Action<ST> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple((state, action), static (t, st) => st.action.Invoke(st.state));
            return EnqueueInternal(wrap);
        }
        //-------------------------------------------------------------------------------
        public bool Insert(int index, Action action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(action, static (t, st) => st.Invoke());
            return InsertInternal(index, wrap);
        }
        public bool Insert(int index, Action<T> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(action, static (t, st) => st.Invoke(t));
            return InsertInternal(index, wrap);
        }
        public bool Insert<ST>(int index, ST state, Action<T, ST> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple(state, action);
            return InsertInternal(index, wrap);
        }
        public bool Insert<ST>(int index, ST state, Action<ST> action)
        {
            if (IsDisposing) { return false; }
            var wrap = AllocTuple((state, action), static (t, st) => st.action?.Invoke(st.state));
            return InsertInternal(index, wrap);
        }
        //-------------------------------------------------------------------------------
        bool EnqueueInternal(MessageTuple tuple)
        {
            if (IsDisposing) { return false; }
            lock (adding)
            {
                adding.Add(tuple);
                count++;
            }
            return true;
        }
        bool InsertInternal(int index, MessageTuple tuple)
        {
            if (IsDisposing) { return false; }
            lock (adding)
            {
                adding.Insert(index, tuple);
                count++;
            }
            return true;
        }
        //-------------------------------------------------------------------------------
        public void ProcessMessages(T t)
        {
            if (count > 0)
            {
                var list = forlist;
                if (list != null)
                {
                    list.Clear();
                    try
                    {
                        lock (adding)
                        {
                            list.AddRange(adding);
                            adding.Clear();
                            count = 0;
                        }
                        for (int i = 0; i < list.Count; i++)
                        {
                            var e = list[i];
                            try
                            {
                                e.Process(t);
                            }
                            catch (Exception err)
                            {
                                if (OnError != null)
                                {
                                    OnError.Invoke(err);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    finally
                    {
                        list.Clear();
                    }
                }
            }
        }

    }

}
