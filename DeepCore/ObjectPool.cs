using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static DeepCore.Colors;

namespace DeepCore
{

    public delegate object ObjectPoolOnCreate<ST>(ST st, ObjectPool pool);
    public delegate object ObjectPoolOnCreate(ObjectPool pool);


    public delegate T OnCreateInPool<T, ST>(ST st, ObjectPool pool);
    public delegate T OnCreateInPool<T>(ObjectPool pool);




    //-------------------------------------------------------------------------------------------------------------------------------



    public interface ObjectPool : IDisposable
    {
        AbstractCollectionPool Collection { get; }
        Type PoolType { get; }
        int StackCount { get; }
        bool TryGetOrNewObject<ST>(out object obj, ST st, ObjectPoolOnCreate<ST> create);
        public object GetObject<ST>(ST st, ObjectPoolOnCreate<ST> create)
        {
            TryGetOrNewObject<ST>(out var obj, st, create);
            return obj;
        }
        void ReleaseObject(object obj);
        void Clear();
    }
    public interface ObjectPool<T> : ObjectPool
    {
        void Release(T obj);
        bool TryGetOrNew<ST>(out T obj, ST st, OnCreateInPool<T, ST> create);
        public T Get<ST>(ST st, OnCreateInPool<T, ST> create)
        {
            TryGetOrNew<ST>(out T obj, st, create);
            return obj;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------


    //-------------------------------------------------------------------------------------------------------------------------------
    public abstract class ObjectPools : Disposable
    {
        #region Statistics
        class Tuple
        {
            internal long AllocCount = 0;
            internal long PoolCount = 0;
            internal long DropCount = 0;
            internal Tuple Clone()
            {
                return new Tuple()
                {
                    AllocCount = this.AllocCount,
                    PoolCount = this.PoolCount,
                    DropCount = this.DropCount
                };
            }
        }
        private static long s_total_alloc = 0;
        private static long s_total_pool = 0;
        private static long s_total_drop = 0;
        private static HashMap<Type, Tuple> s_statistics = new HashMap<Type, Tuple>();
        private static bool is_statistics = false;
        public static bool EnableObjectPool { protected get; set; } = false;

        public static int MaxObjectCount
        {
            get; set;
        }
        public static long TotalAllocCount
        {
            get { lock (s_statistics) return s_total_alloc; }
        }
        public static long TotalPoolCount
        {
            get { lock (s_statistics) return s_total_pool; }
        }
        public static long TotalDropCount
        {
            get { lock (s_statistics) return s_total_drop; }
        }
        public static bool EnableStatistics
        {
            get { return is_statistics; }
            set { is_statistics = value; }
        }
        private static Func<Type, Tuple> createAction = (t) => new Tuple();
        protected static void RecordAlloc(Type type)
        {
            if (is_statistics)
            {
                lock (s_statistics)
                {
                    s_total_alloc++;
                    var ac = s_statistics.GetOrAdd(type, static (e) => createAction(e));
                    ac.AllocCount++;
                }
            }
        }
        protected static void RecordInPool(Type type)
        {
            if (is_statistics)
            {
                lock (s_statistics)
                {
                    s_total_pool++;
                    var ac = s_statistics.GetOrAdd(type, static (e) => createAction(e));
                    ac.PoolCount++;
                }
            }
        }
        protected static void RecordOutPool(Type type)
        {
            if (is_statistics)
            {
                lock (s_statistics)
                {
                    s_total_pool--;
                    var ac = s_statistics.GetOrAdd(type, static (e) => createAction(e));
                    ac.PoolCount--;
                }
            }
        }
        protected static void RecordDropPool(Type type)
        {
            if (is_statistics)
            {
                lock (s_statistics)
                {
                    s_total_drop++;
                    var ac = s_statistics.GetOrAdd(type, static (e) => createAction(e));
                    ac.DropCount++;
                }
            }
        }
        public static void PrintStatus(TextWriter output, string prefix = "  ", int namePlaceHolder = 16, int totalPlaceHolder = 64)
        {
            if (is_statistics)
            {
                var map = new SortedDictionary<Type, Tuple>(new TypeComparer());
                long total_alloc;
                long total_pool;
                long total_drop;
                lock (s_statistics)
                {
                    total_alloc = s_total_alloc;
                    total_pool = s_total_pool;
                    total_drop = s_total_drop;
                    foreach (var e in s_statistics)
                    {
                        map.Add(e.Key, e.Value.Clone());
                    }
                }
                output.PrintTitle("Object Pool", "Alloc Infomation", prefix, namePlaceHolder);
                foreach (var e in map)
                {
                    if (e.Value.DropCount > 0)
                    {
                        output.PrintLine(string.Format($"{e.Value.PoolCount}(+{e.Value.DropCount}) / {e.Value.AllocCount}"), e.Key.ToVisibleName(), prefix, namePlaceHolder);
                    }
                    else
                    {
                        output.PrintLine(string.Format($"{e.Value.PoolCount} / {e.Value.AllocCount}"), e.Key.ToVisibleName(), prefix, namePlaceHolder);
                    }
                }
                output.PrintLine(string.Format($"{total_pool}(+{total_drop}) / {total_alloc}"), "[Total]", prefix, namePlaceHolder);
            }
        }
        public static void ClearPool()
        {
            if (is_statistics)
            {
                lock (s_statistics)
                {
                    s_total_alloc = 0;
                    s_total_pool = 0;
                    s_total_drop = 0;
                    s_statistics.Clear();
                }
            }
        }
        #endregion

        protected Logger log = new LazyLogger(typeof(ObjectPools));
    }


}
