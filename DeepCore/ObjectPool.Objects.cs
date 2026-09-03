using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepCore
{
    //-------------------------------------------------------------------------------------------------------------------------------

    public struct AllocWrap<T> : IDisposable
    {
        private ObjectPool Pool;
        public T Value;
        internal AllocWrap(T value, ObjectPool pool)
        {
            this.Value = value;
            this.Pool = pool;
        }
        public void Dispose()
        {
            if (this.Pool != null)
            {
                this.Pool.ReleaseObject(Value);
                this.Pool = null;
            }
        }
        public static implicit operator T(in AllocWrap<T> value)
        {
            return value.Value;
        }
    }
    public class AllocRef<T> : IDisposable
    {
        private ObjectPool Pool;
        public T Value;
        internal AllocRef(ObjectPool pool)
        {
            this.Pool = pool;
        }
        public void Dispose()
        {
            if (this.Pool != null)
            {
                this.Pool.ReleaseObject(Value);
                this.Pool = null;
                this.Value = default;
            }
        }
        public static implicit operator T(in AllocRef<T> value)
        {
            return value.Value;
        }
    }

    public abstract class AbstractCollectionPool : Disposable
    {
        public abstract int StackCount { get; }
        protected abstract ObjectPool GetPool(Type type);
        protected abstract bool TryGetOrCreatePool(Type type, out ObjectPool pool);
        protected abstract bool TryGetOrCreatePool<T>(out ObjectPool<T> pool);
        public abstract void Clear();
        sealed protected override void Disposing() => Clear();

        //------------------------------------------------------------------------------------------------------------------
        protected T GetOrCreate<T, ST>(out ObjectPool<T> pool, ST st, OnCreateInPool<T, ST> create, params object[] args) where T : class
        {
            if (TryGetOrCreatePool<T>(out pool))
            {
                var exist = pool.TryGetOrNew<ST>(out var ret, st, create);
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, !exist, args);
                }
                return ret;
            }
            else if (create != null)
            {
                var ret = create(st, pool);
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, true, args);
                }
                return ret;
            }
            else
            {
                var ret = DeepActivator.CreateInstance<T>();
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, true, args);
                }
                return ret;
            }
        }
        protected object GetOrCreateObject<ST>(out ObjectPool pool, Type type, ST st, ObjectPoolOnCreate<ST> create, params object[] args)
        {
            if (TryGetOrCreatePool(type, out pool))
            {
                var exist = pool.TryGetOrNewObject<ST>(out var ret, st, create);
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, !exist, args);
                }
                return ret;
            }
            else if (create != null)
            {
                var ret = create(st, pool);
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, true, args);
                }
                return ret;
            }
            else
            {
                var ret = DeepActivator.CreateInstance(type);
                if (ret is IPoolingObject auto)
                {
                    auto.OnAlloc(pool, true, args);
                }
                return ret;
            }
        }
        //------------------------------------------------------------------------------------------------------------------
        public T Alloc<T>() where T : class, new()
        {
            return AllocOrCreate<T, Type>(typeof(T), static (st, pool) => new T());
        }
        public T AllocInit<T>(Action<T> init) where T : class, new()
        {
            var ret = AllocOrCreate<T, Type>(typeof(T), static (st, pool) => new T());
            init(ret);
            return ret;
        }
        public T AllocInit<T, ST>(ST st, Action<ST, T> init, T phototype = default) where T : class, new()
        {
            var ret = AllocOrCreate<T, Type>(typeof(T), static (st, pool) => new T());
            init(st, ret);
            return ret;
        }
        public T AllocOrCreate<T>(OnCreateInPool<T> create) where T : class
        {
            return AllocOrCreate<T, OnCreateInPool<T>>(create, static (t, p) => (T)t(p));
        }
        public T AllocOrCreate<T, ST>(ST st, OnCreateInPool<T, ST> create) where T : class
        {
            return GetOrCreate<T, ST>(out var pool, st, create);
        }
        public object Alloc(Type type)
        {
            return GetOrCreateObject(out var pool, type, type, null);
        }
        public object AllocOrCreate<ST>(Type type, ST st, ObjectPoolOnCreate<ST> create)
        {
            return GetOrCreateObject(out var pool, type, st, create);
        }
        //------------------------------------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------------------------------------
        public T AllocAutoRelease<T>(params object[] args) where T : class, IRecyclable, new()
        {
            return AllocOrCreateAutoRelease<T, Type>(typeof(T), static (st, pool) => new T(), args);
        }
        public T AllocOrCreateAutoRelease<T>(OnCreateInPool<T> create, params object[] args) where T : class, IRecyclable
        {
            return AllocOrCreateAutoRelease<T, OnCreateInPool<T>>(create, static (t, p) => (T)t(p), args);
        }
        public T AllocOrCreateAutoRelease<T, ST>(ST st, OnCreateInPool<T, ST> create, params object[] args) where T : class, IRecyclable
        {
            return GetOrCreate(out var pool, st, create, args);
        }
        //------------------------------------------------------------------------------------------------------------------
        public AllocWrap<T> AllocWrap<T>() where T : class, new()
        {
            var ret = GetOrCreateObject(out var pool, typeof(T), typeof(T), static (t, p) => new T()) as T;
            return new DeepCore.AllocWrap<T>(ret, pool);
        }
        public AllocRef<T> AllocRef<T>(T value)
        {
            var ret = GetOrCreateObject(out var pool, typeof(T), typeof(T), static (t, p) => new AllocRef<T>(p));
            ((AllocRef<T>)ret).Value = value;
            return ((AllocRef<T>)ret);
        }
        //------------------------------------------------------------------------------------------------------------------
        public void Release(Type type, object free)
        {
            var pool = GetPool(type);
            pool?.ReleaseObject(free);
        }
        public void Release(object free)
        {
            var pool = GetPool(free.GetType());
            pool?.ReleaseObject(free);
        }
        //--------------------------------------------------------------------
        #region ArrayList<T>
        //--------------------------------------------------------------------
        public class AutoReleaseList<T> : ArrayList<T>, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseList(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseList<T> AllocList<T>(Action<List<T>> add)
        {
            var ret = GetOrCreate<AutoReleaseList<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseList<T>(p));
            add.Invoke(ret);
            return ret;
        }
        public AutoReleaseList<T> AllocList<T>()
        {
            var ret = GetOrCreate<AutoReleaseList<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseList<T>(p));
            return ret;
        }
        public AutoReleaseList<T> AllocList<T>(IEnumerable<T> added)
        {
            var ret = GetOrCreate<AutoReleaseList<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseList<T>(p));
            if (added != null) { ret.AddRange(added); }
            return ret;
        }
        public AutoReleaseList<T> AllocList<T>(T[] added)
        {
            var ret = GetOrCreate<AutoReleaseList<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseList<T>(p));
            if (added != null) { ret.AddRange(added); }
            return ret;
        }
        public AutoReleaseList<T> AllocList<T>(int capacity)
        {
            var ret = GetOrCreate<AutoReleaseList<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseList<T>(p));
            if (capacity > 0) ret.Capacity = Math.Max(capacity, ret.Capacity);
            return ret;
        }
        //--------------------------------------------------------------------
        public class AutoReleaseSet<T> : HashSet<T>, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseSet(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseSet<T> AllocSet<T>()
        {
            var ret = GetOrCreate<AutoReleaseSet<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseSet<T>(p));
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------
        #region ArrayList
        public class AutoReleaseList : ArrayList, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseList(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseList AllocList(Action<IList> add)
        {
            var ret = GetOrCreate<AutoReleaseList, int>(out var pool, 0, static (t, p) => new AutoReleaseList(p));
            add.Invoke(ret);
            return ret;
        }
        public AutoReleaseList AllocList()
        {
            var ret = GetOrCreate<AutoReleaseList, int>(out var pool, 0, static (t, p) => new AutoReleaseList(p));
            return ret;
        }
        public AutoReleaseList AllocList(ICollection added)
        {
            var ret = GetOrCreate<AutoReleaseList, int>(out var pool, 0, static (t, p) => new AutoReleaseList(p));
            if (added != null) { ret.AddRange(added); }
            return ret;
        }
        public AutoReleaseList AllocList(Array added)
        {
            var ret = GetOrCreate<AutoReleaseList, int>(out var pool, 0, static (t, p) => new AutoReleaseList(p));
            if (added != null) { ret.AddRange(added); }
            return ret;
        }
        public AutoReleaseList AllocList(int capacity)
        {
            var ret = GetOrCreate<AutoReleaseList, int>(out var pool, 0, static (t, p) => new AutoReleaseList(p));
            if (capacity > 0) ret.Capacity = Math.Max(capacity, ret.Capacity);
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------
        #region AutoReleaseStack<T> 
        public class AutoReleaseStack<T> : Stack<T>, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseStack(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseStack<T> AllocStack<T>()
        {
            var ret = GetOrCreate<AutoReleaseStack<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseStack<T>(p));
            return ret;
        }
        public AutoReleaseStack<T> AllocStack<T>(IEnumerable<T> collection)
        {
            var ret = GetOrCreate<AutoReleaseStack<T>, int>(out var pool, 0, static (t, p) => new AutoReleaseStack<T>(p));
            foreach (var t in collection)
            {
                ret.Push(t);
            }
            return ret;
        }
        #endregion     
        //--------------------------------------------------------------------
        #region AutoReleaseMap<T> 
        public class AutoReleaseMap<K, T> : HashMap<K, T>, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseMap(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseMap<K, T> AllocMap<K, T>()
        {
            var ret = GetOrCreate<AutoReleaseMap<K, T>, int>(out var pool, 0, static (t, p) => new AutoReleaseMap<K, T>(p));
            return ret;
        }
        public AutoReleaseMap<K, T> AllocMap<K, T>(IDictionary<K, T> src)
        {
            var ret = GetOrCreate<AutoReleaseMap<K, T>, int>(out var pool, 0, static (t, p) => new AutoReleaseMap<K, T>(p));
            ret.PutAll(src);
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------
        #region AutoReleaseStack
        public class AutoReleaseStack : Stack, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseStack(ObjectPool owner)
            {
                this.owner = owner;
            }
            void IDisposable.Dispose()
            {
                this.Clear();
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                this.Clear();
            }
        }
        public AutoReleaseStack AllocStack()
        {
            var ret = GetOrCreate<AutoReleaseStack, int>(out var pool, 0, static (t, p) => new AutoReleaseStack(p));
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------
        #region StringBuilder
        public class AutoReleaseStringWriter : StringWriter, IPoolingObject
        {
            internal readonly ObjectPool owner;
            internal AutoReleaseStringWriter(ObjectPool owner) : base(new StringBuilder()) { this.owner = owner; }
            public override Encoding Encoding { get { return CUtils.UTF8; } }
            public StringBuilder Output { get { return GetStringBuilder(); } }
            public override string ToString()
            {
                return GetStringBuilder().ToString();
            }
            protected override void Dispose(bool disposing)
            {
                //base.Dispose(disposing);
                GetStringBuilder().Remove(0, GetStringBuilder().Length);
                owner?.ReleaseObject(this);
            }
            void IPoolingObject.OnAlloc(ObjectPool pool, bool newObject, params object[] args)
            {
            }
            void IPoolingObject.OnDestory(ObjectPool pool)
            {
                GetStringBuilder().Remove(0, GetStringBuilder().Length);
            }
            public AutoReleaseStringWriter Append(object obj)
            {
                Write(obj);
                return this;
            }
            public AutoReleaseStringWriter AppendLine(object obj)
            {
                WriteLine(obj);
                return this;
            }
            public AutoReleaseStringWriter AppendLine()
            {
                WriteLine();
                return this;
            }
        }
        public AutoReleaseStringWriter AllocStringWriter()
        {
            var ret = GetOrCreate<AutoReleaseStringWriter, int>(out var pool, 0, static (t, p) => new AutoReleaseStringWriter(p));
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------
        #region Timing
        public TimeInterval AllocTimeInterval(float intervalMS)
        {
            return AllocAutoRelease<TimeInterval>().Init(intervalMS);
        }
        public TimeExpire AllocTimeExpire(float delayMS)
        {
            return AllocAutoRelease<TimeExpire>().Init(delayMS);
        }
        #endregion
        //--------------------------------------------------------------------
    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public class CollectionPool : AbstractCollectionPool
    {
        private static CollectionPool s_shared = new CollectionPool();
        public static CollectionPool Shared { get => s_shared; }
        private HashMap<Type, ConcurrentObjectPool> pools = new HashMap<Type, ConcurrentObjectPool>();
        public override int StackCount { get => pools.Sum(e => e.Value.StackCount); }
        protected override bool TryGetOrCreatePool(Type type, out ObjectPool ret)
        {
            var pool = pools.Get(type);
            if (pool == null)
            {
                lock (pools)
                {
                    pool = pools.GetOrAdd(type, t => { return new ConcurrentObjectPool(type); });
                    pool.Collection = this;
                }
            }
            ret = pool;
            return ret != null;
        }
        protected override bool TryGetOrCreatePool<T>(out ObjectPool<T> ret)
        {
            var pool = pools.Get(typeof(T));
            if (pool == null)
            {
                lock (pools)
                {
                    pool = pools.GetOrAdd(typeof(T), t => { return new ConcurrentObjectPool<T>(); });
                    pool.Collection = this;
                }
            }
            ret = pool as ObjectPool<T>;
            return ret != null;
        }
        protected override ObjectPool GetPool(Type type)
        {
            return pools.Get(type);
        }
        public override void Clear()
        {
            lock (pools)
            {
                foreach (var p in pools.Values)
                {
                    p.Clear();
                }
                pools.Clear();
            }
        }
    }
    public class SingleThreadCollectionPool : AbstractCollectionPool
    {
        private HashMap<Type, SingleThreadObjectPool> pools = new HashMap<Type, SingleThreadObjectPool>();
        public override int StackCount { get => pools.Sum(e => e.Value.StackCount); }
        public SingleThreadCollectionPool() { }
        protected override bool TryGetOrCreatePool(Type type, out ObjectPool pool)
        {
            if (IsDisposing)
            {
                pool = null;
                return false;
            }
            if (pools.TryGetValue(type, out var spool))
            {
                pool = spool;
                return true;
            }
            else
            {
                spool = new SingleThreadObjectPool(type);
                spool.Collection = this;
                pools.Add(type, spool);
                pool = spool;
                return true;
            }
        }
        protected override bool TryGetOrCreatePool<T>(out ObjectPool<T> pool)
        {
            if (IsDisposing)
            {
                pool = null;
                return false;
            }
            var type = typeof(T);
            if (pools.TryGetValue(type, out var spool))
            {
                pool = spool as ObjectPool<T>;
                return pool != null;
            }
            else
            {
                spool = new SingleThreadObjectPool<T>();
                spool.Collection = this;
                pools.Add(type, spool);
                pool = spool as ObjectPool<T>;
                return true;
            }
        }
        protected override ObjectPool GetPool(Type type)
        {
            if (IsDisposing) { return null; }
            return pools.Get(type);
        }
        public override void Clear()
        {
            foreach (var p in pools.Values)
            {
                p.Clear();
            }
            pools.Clear();
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public class MemoryStreamObjectPool
    {
        private readonly ObjectPool<AutoRelease> s_Pool;
        public MemoryStreamObjectPool(bool singleThread)
        {
            if (singleThread)
                s_Pool = new SingleThreadObjectPool<AutoRelease>();
            else
                s_Pool = new ConcurrentObjectPool<AutoRelease>();
        }
        public void Dispose()
        {
            s_Pool?.Dispose();
        }
        public AutoRelease AllocAutoRelease()
        {
            AutoRelease ret = s_Pool.Get(this, static (t, p) => new AutoRelease(t)) as AutoRelease;
            return ret;
        }
        public AutoRelease AllocAutoRelease(byte[] buffer)
        {
            AutoRelease ret = s_Pool.Get(this, static (t, p) => new AutoRelease(t)) as AutoRelease;
            return ret.Init(buffer);
        }
        public AutoRelease AllocAutoRelease(byte[] buffer, int offset, int length)
        {
            AutoRelease ret = s_Pool.Get(this, static (t, p) => new AutoRelease(t)) as AutoRelease;
            return ret.Init(buffer, offset, length);
        }
        public AutoRelease AllocAutoRelease(ArraySegment<byte> buffer)
        {
            AutoRelease ret = s_Pool.Get(this, static (t, p) => new AutoRelease(t)) as AutoRelease;
            return ret.Init(buffer);
        }
        private void Release(AutoRelease toRelease)
        {
            s_Pool.Release(toRelease);
        }
        public class AutoRelease : DeepCore.IO.MemoryStream
        {
            private readonly MemoryStreamObjectPool pool;
            internal AutoRelease(MemoryStreamObjectPool pool) { this.pool = pool; }
            internal AutoRelease(MemoryStreamObjectPool pool, byte[] buffer) : base(buffer) { this.pool = pool; }
            internal AutoRelease(MemoryStreamObjectPool pool, byte[] buffer, int index, int count) : base(buffer, index, count) { this.pool = pool; }
            internal AutoRelease(MemoryStreamObjectPool pool, int capacity) : base(capacity) { this.pool = pool; }
            public AutoRelease Init(byte[] buffer)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Length, ret.Capacity);
                ret.SetLength(buffer.Length);
                Buffer.BlockCopy(buffer, 0, ret.GetBuffer(), 0, buffer.Length);
                ret.Position = 0;
                return ret;
            }
            public AutoRelease Init(byte[] buffer, int offset, int length)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Length, ret.Capacity);
                ret.SetLength(length);
                Buffer.BlockCopy(buffer, offset, ret.GetBuffer(), 0, length);
                ret.Position = 0;
                return ret;
            }
            public AutoRelease Init(ArraySegment<byte> buffer)
            {
                var ret = this;
                ret.Capacity = Math.Max(buffer.Count, ret.Capacity);
                ret.SetLength(buffer.Count);
                Buffer.BlockCopy(buffer.Array, buffer.Offset, ret.GetBuffer(), 0, buffer.Count);
                ret.Position = 0;
                return ret;
            }
            protected override void Dispose(bool disposing)
            {
                this.Position = 0;
                this.SetLength(0);
                pool?.Release(this);
            }
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------






}
