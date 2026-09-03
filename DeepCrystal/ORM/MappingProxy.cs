using DeepCore;
using DeepCrystal.ORM.Generic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{
    public abstract class ProxyMap<K, V, TRef> where TRef : IMappingNode<V>
    {
        private AsyncLock locker = new AsyncLock();
        protected abstract MappingDictionary<K, V, TRef> map { get; }

        //-------------------------------------------------------------------------------------------

        public int Count
        {
            get
            {
                using (locker.Lock())
                {
                    return this.map.Count;
                }
            }
        }
        public List<V> Values
        {
            get
            {
                using (locker.Lock())
                {
                    return new List<V>(this.map.Values.ConvertAll(t => t.Data));
                }
            }
        }
        public List<TRef> MappingValues
        {
            get
            {
                using (locker.Lock())
                {
                    return new List<TRef>(this.map.Values);
                }
            }
        }

        //-------------------------------------------------------------------------------------------

        public TRef Add(K name, V node)
        {
            using (locker.Lock())
            {
                return map.Add(name, node);
            }
        }
        public bool TryAdd(K name, V node, out TRef _exist)
        {
            using (locker.Lock())
            {
                return map.TryAdd(name, node, out _exist);
            }
        }
        public bool TryAdd(K name, Func<K, V> create, out TRef value)
        {
            using (locker.Lock())
            {
                return map.TryAdd(name, create, out value);
            }
        }
        public bool TryGetOrCreate(K name, out TRef value, Func<K, V> create)
        {
            using (locker.Lock())
            {
                return map.TryGetOrCreate(name, out value, create);
            }
        }
        public bool TryRemove(K key, out TRef _exist)
        {
            using (locker.Lock())
            {
                return this.map.TryRemove(key, out _exist);
            }
        }
        public void Clear()
        {
            using (locker.Lock())
            {
                this.map.Clear();
            }
        }

        //-------------------------------------------------------------------------------------------

        public bool ContainsKey(K key)
        {
            using (locker.Lock())
            {
                return this.map.ContainsKey(key);
            }
        }
        public TRef Get(K key)
        {
            using (locker.Lock())
            {
                return this.map.Get(key);
            }
        }
        public bool TryGetValue(K key, out TRef _exist)
        {
            using (locker.Lock())
            {
                return this.map.TryGetValue(key, out _exist);
            }
        }

        //-------------------------------------------------------------------------------------------

        public void BatchFlush(IObjectTransaction tx)
        {
            using (locker.Lock())
            {
                this.map.BatchFlush(tx);
            }
        }
        public async Task FlushAsync()
        {
            using (await locker.LockAsync())
            {
                await this.map.FlushAsync();
            }
        }
        public async Task LoadDataAsync()
        {
            using (await locker.LockAsync())
            {
                await this.map.LoadDataAsync();
            }
        }

        //-------------------------------------------------------------------------------------------
    }
}
