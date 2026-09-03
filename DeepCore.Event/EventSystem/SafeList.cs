using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepCore.Event.EventSystem
{
    public class SafeList<TValue> : ReadWriteLockable<List<TValue>>
    {
        public SafeList() : base(new List<TValue>())
        {

        }

        public TValue this[int index]
        {
            get
            {
                EnterReadLock();
                try
                {
                    return InnerData[index];
                }
                finally
                {
                    ExitReadLock();
                }
            }

            set
            {
                EnterWriteLock();
                try
                {
                    InnerData[index] = value;
                }
                finally
                {
                    ExitWriteLock();
                }
            }
        }

        public int Count
        {
            get
            {
                EnterReadLock();
                try
                {
                    return InnerData.Count;
                }
                finally
                {
                    ExitReadLock();
                }
            }
        }

        public void RemoveAt(int index)
        {
            EnterWriteLock();
            try
            {
                InnerData.RemoveAt(index);
            }
            finally
            {
                ExitWriteLock();
            }
        }
        public void Clear()
        {
            EnterWriteLock();
            try
            {
                InnerData.Clear();
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public bool Remove(TValue value)
        {
            EnterWriteLock();
            try
            {
                return InnerData.Remove(value);
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public void Add(TValue value)
        {
            EnterWriteLock();
            try
            {
                InnerData.Add(value);
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public TValue[] ToArray()
        {
            EnterReadLock();
            try
            {
                return InnerData.ToArray();
            }
            finally
            {
                ExitReadLock();
            }
        }

        public void Foreach(Action<TValue> act)
        {
            EnterReadLock();
            try
            {
                InnerData.ForEach(act);
            }
            finally
            {
                ExitReadLock();
            }
        }
    }
}