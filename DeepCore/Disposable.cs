using System;
using System.Threading;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore
{
    abstract public class Disposable : IDisposable
    {
        public static bool EnableAlloc { get => s_alloc.Enable; set => s_alloc.Enable = value; }
        public static bool VerbosAlloc { get => s_alloc.Verbos; set => s_alloc.Verbos = value; }
        internal readonly static TypeAllocRecorder s_alloc = new TypeAllocRecorder(typeof(Disposable)) { Verbos = false };
        internal int disposed = 0;
        internal int disposing = 0;
        internal bool synchronized = false;
        protected Disposable()
        {
            s_alloc.RecordConstructor(GetType());
        }
        ~Disposable()
        {
            if (synchronized)
            {
                if (Interlocked.CompareExchange(ref disposing, 1, 0) == 0)
                {
                    s_alloc.RecordDispose(GetType());
                    disposed = 1;
                }
            }
            else
            {
                if (disposing == 0)
                {
                    disposing = 1;
                    s_alloc.RecordDispose(GetType());
                    disposed = 1;
                }
            }
            s_alloc.RecordDestructor(GetType());
        }
        protected virtual void RecordDisposing() { }
        protected abstract void Disposing();
        public bool IsDisposed { get => disposed != 0; }
        public bool IsDisposing { get => disposing != 0; }
        protected void AsSynchronizedDisposing()
        {
            this.synchronized = true;
        }
        public void Dispose()
        {
            if (synchronized)
            {
                if (Interlocked.CompareExchange(ref disposing, 1, 0) == 0)
                {
                    s_alloc.RecordDispose(GetType());
                    RecordDisposing();
                    Disposing();
                    disposed = 1;
                }
            }
            else
            {
                if (disposing == 0)
                {
                    disposing = 1;
                    s_alloc.RecordDispose(GetType());
                    RecordDisposing();
                    Disposing();
                    disposed = 1;
                }
            }
        }
    }

    abstract public class AsyncDisposable : Disposable, IAsyncDisposable
    {
        protected abstract ValueTask DisposingAsync();
        public async ValueTask DisposeAsync()
        {
            if (synchronized)
            {
                if (Interlocked.CompareExchange(ref disposing, 1, 0) == 0)
                {
                    s_alloc.RecordDispose(GetType());
                    RecordDisposing();
                    await DisposingAsync();
                    disposed = 1;
                }
            }
            else
            {
                if (disposing == 0)
                {
                    disposing = 1;
                    s_alloc.RecordDispose(GetType());
                    RecordDisposing();
                    await DisposingAsync();
                    disposed = 1;
                }
            }
        }

    }


    public interface IPoolingObject : IDisposable
    {
        /// <summary>
        /// 当对象从池中被重新使用时
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="create"></param>
        /// <param name="args"></param>
        void OnAlloc(ObjectPool pool, bool newObject, params object[] args);
        /// <summary>
        /// 从对象池销毁
        /// </summary>
        void OnDestory(ObjectPool pool);
    }

    /// <summary>
    /// 自释放可池化单位
    /// </summary>
    public interface IRecyclable : IPoolingObject
    {
        /// <summary>
        /// 可被释放
        /// </summary>
        bool CanDispose { get; }
        /// <summary>
        /// 可被回收利用
        /// </summary>
        bool CanRecycle { get; }
        /// <summary>
        /// 当某个对象正在用时，标记一次，该单位将不会被回池或释放。
        /// 被标记Retain的对象，无论调用多少次Dispose都不会被真正回收，直到Release后才可能被回收。
        /// </summary>
        void Retain(int count = 1);
        /// <summary>
        /// 当被标记对象使用完毕，解除标记一次，直到为0时则自动释放或回池
        /// </summary>
        bool Release();
    }


    public abstract class Recyclable : IRecyclable
    {
        public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(Recyclable)) { Verbos = false };
        private int retainCount = 0;
        private ObjectPool pool;
        public Recyclable()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~Recyclable()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        public void Retain(int count = 1)
        {
            if (count < 1) throw new ArgumentException("Retain count must be great than 0");
            retainCount += count;
        }
        public bool Release()
        {
            if (CanDispose)
            {
                this.Dispose();
                return true;
            }
            else
            {
                retainCount--;
                return false;
            }
        }
        public void Dispose()
        {
            if (CanDispose)
            {
                this.IsDisposing = true;
                if (!IsDisposed)
                {
                    try
                    {
                        Disposing();
                    }
                    finally
                    {
                        Alloc.RecordDispose(GetType());
                        RecordDisposing();
                        IsDisposed = true;
                        if (this.pool != null)
                        {
                            this.pool.ReleaseObject(this);
                            this.pool = null;
                        }
                    }
                }
            }
            else
            {
                retainCount--;
            }
        }
        void IPoolingObject.OnAlloc(ObjectPool pool, bool NEW, params object[] args)
        {
            if (this.pool != null)
            {
                throw new Exception("池对象已经在使用！");
            }
            this.IsDisposing = false;
            this.IsDisposed = false;
            this.pool = pool;
            this.retainCount = 0;
            if (!NEW)
            {
                Alloc.RecordReuse(GetType());
                RecordReuse();
            }
            OnAlloc(NEW, args);
        }
        void IPoolingObject.OnDestory(ObjectPool pool)
        {
            if (!IsDisposed)
            {
                this.IsDisposing = true;
                try
                {
                    Disposing();
                }
                finally
                {
                    Alloc.RecordDispose(GetType());
                    RecordDisposing();
                    IsDisposed = true;
                }
            }
            this.pool = null;
            Destructing();
            this.IsDestoryed = true;
        }
        public AbstractCollectionPool AllocPools { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsDisposing { get; private set; }
        public bool IsDestoryed { get; private set; }
        public bool IsOnUse { get => pool != null; }
        public bool CanRecycle { get => retainCount <= 0 && IsDisposed; }
        public bool CanDispose { get => retainCount <= 0; }

        protected abstract void Disposing();
        protected virtual void Destructing() { }

        protected virtual void OnAlloc(bool NEW, params object[] args) { }
        /// <summary>
        /// 用于记录GC
        /// </summary>
        protected virtual void RecordDisposing() { }
        /// <summary>
        /// 用于记录GC
        /// </summary>
        protected virtual void RecordReuse() { }
    }


    public class RecyclableReference<T> where T : IRecyclable
    {
        private T m_value;
        public T Value
        {
            get => m_value;
            set
            {
                if (this.m_value != null)
                {
                    this.m_value.Release();
                }
                this.m_value = value;
                if (this.m_value != null)
                {
                    this.m_value.Retain();
                }
            }
        }
        public bool HasValue => m_value != null;
        public bool TryGetValue(out T value)
        {
            if (HasValue)
            {
                value = this.Value;
                return true;
            }
            value = default;
            return false;
        }

        public static implicit operator T(RecyclableReference<T> value) => value.m_value;
    }

    public static class DisposableExtensions
    {
        public static bool CleanAndDispose(ref Disposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
                disposable = null;
                return true;
            }
            return false;
        }
        public static bool CleanAndDispose(ref IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
                disposable = null;
                return true;
            }
            return false;
        }
    }



}