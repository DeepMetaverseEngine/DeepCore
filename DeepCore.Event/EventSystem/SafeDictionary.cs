using System;
using System.Collections.Generic;

namespace DeepCore.Event.EventSystem
{
    public class SafeDictionary<TKey, TValue> : ReadWriteLockable<Dictionary<TKey, TValue>>
    {
        public SafeDictionary() : base(new Dictionary<TKey, TValue>())
        {
        }

        public TValue this[TKey key]
        {
            get
            {
                EnterReadLock();
                try
                {
                    return InnerData[key];
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
                    InnerData[key] = value;
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

        public ICollection<TValue> Values
        {
            get
            {
                EnterReadLock();
                try
                {
                    return new List<TValue>(InnerData.Values);
                }
                finally
                {
                    ExitReadLock();
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                EnterReadLock();
                try
                {
                    return new List<TKey>(InnerData.Keys);
                }
                finally
                {
                    ExitReadLock();
                }
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

        public void Add(TKey key, TValue value)
        {
            EnterWriteLock();
            try
            {
                InnerData.Add(key, value);
            }
            finally
            {
                ExitWriteLock();
            }
        }


        public TValue Get(TKey key)
        {
            EnterReadLock();
            try
            {
                InnerData.TryGetValue(key, out var ret);
                return ret;
            }
            finally
            {
                ExitReadLock();
            }
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            EnterUpgradeLock();
            try
            {
                if (!InnerData.TryGetValue(key, out var existValue))
                {
                    EnterWriteLock();
                    try
                    {
                        existValue = factory.Invoke(key);
                        InnerData.Add(key, existValue);
                    }
                    finally
                    {
                        ExitWriteLock();
                    }
                }

                return existValue;
            }
            finally
            {
                ExitUpgradeLock();
            }
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            EnterWriteLock();
            try
            {
                InnerData[key] = value;
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            EnterReadLock();
            try
            {
                return InnerData.TryGetValue(key, out value);
            }
            finally
            {
                ExitReadLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            EnterReadLock();
            try
            {
                return InnerData.ContainsKey(key);
            }
            finally
            {
                ExitReadLock();
            }
        }

        public TValue RemoveByKey(TKey key)
        {
            EnterWriteLock();
            try
            {
                InnerData.TryGetValue(key, out var value);
                InnerData.Remove(key);
                return value;
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public void Remove(TKey key)
        {
            EnterWriteLock();
            try
            {
                InnerData.Remove(key);
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public void Foreach(Action<TKey, TValue> act)
        {
            EnterReadLock();
            try
            {
                foreach (var entry in InnerData)
                {
                    act(entry.Key, entry.Value);
                }
            }
            finally
            {
                ExitReadLock();
            }
        }


        public void Remove(Predicate<KeyValuePair<TKey, TValue>> select)
        {
            EnterWriteLock();
            try
            {
                var listKeys = new List<TKey>();
                foreach (var entry in InnerData)
                {
                    if (select.Invoke(new KeyValuePair<TKey, TValue>(entry.Key, entry.Value)))
                    {
                        listKeys.Add(entry.Key);
                    }
                }

                foreach (var key in listKeys)
                {
                    InnerData.Remove(key);
                }
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            InnerData.Clear();
        }
    }
}