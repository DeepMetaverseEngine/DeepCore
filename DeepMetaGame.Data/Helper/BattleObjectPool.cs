using DeepCore;
using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static DeepCore.Colors;

namespace DeepMetaGame.Data.Helper
{
    public abstract class BattleObjectPool : SingleThreadCollectionPool
    {
        private List<IAutoRecycle> mPendingDispose = new List<IAutoRecycle>();
        public override void Clear()
        {
            for (int i = 0; i < mPendingDispose.Count; i++)
            {
                var d = mPendingDispose[i];
                d.OnRecycle();
            }
            mPendingDispose.Clear();
            base.Clear();
        }
        public void UpdateRecycle()
        {
            for (int i = 0; i < mPendingDispose.Count; i++)
            {
                var d = mPendingDispose[i];
                if (d.CanRecycle)
                {
                    d.OnRecycle();
                }
            }
            mPendingDispose.Clear();
        }
        /// <summary>
        /// 排队等待回收
        /// </summary>
        /// <param name="release"></param>
        public void PostRecycle(IAutoRecycle release)
        {
            mPendingDispose.Add(release);
        }
        public void LowMemory()
        {
            base.Clear();
        }
    }
    public abstract class BattleObjectPool<T> : BattleObjectPool
    {
        public T Owner { get; private set; }
        public BattleObjectPool(T owner)
        {
            this.Owner = owner;
        }
    }
    public interface IAutoRecycle : IRecyclable
    {
        /// <summary>
        /// 丢进垃圾桶
        /// </summary>
        void OnRecycle();
    }
    /// <summary>
    /// 战斗对象池专用的自动回收对象，提供Retain/Release机制，允许在回收前保留对象，直到下一帧真正回收对象。
    /// </summary>
    public abstract class BattleAutoRecycle : IAutoRecycle
    {
        public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(BattleAutoRecycle)) { Verbos = false };
        private int retainCount = 0;
        private ObjectPool pool;
        public BattleAutoRecycle()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~BattleAutoRecycle()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
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
                    IsDisposed = true;
                    Alloc.RecordDispose(GetType());
                    RecordDisposing();
                }
            }
            Destructing();
            IsDestoryed = true;
        }
        void IPoolingObject.OnAlloc(ObjectPool pool, bool NEW, object[] args)
        {
            if (this.pool != null)
            {
                throw new Exception("池对象已经在使用！");
            }
            //this.trace = new StackTrace();
            this.IsDisposing = false;
            this.IsDisposed = false;
            this.retainCount = 0;
            this.pool = pool;
            if (!NEW)
            {
                Alloc.RecordReuse(GetType());
                RecordReuse();
            }
            OnAlloc(NEW, args);
        }
        void IAutoRecycle.OnRecycle()
        {
            try
            {
                if (!IsDisposed)
                {
                    try
                    {
                        Disposing();
                    }
                    finally
                    {
                        IsDisposed = true;
                        Alloc.RecordDispose(GetType());
                        RecordDisposing();
                    }
                }
            }
            finally
            {
                if (this.pool != null)
                {
                    this.pool.ReleaseObject(this);
                    this.pool = null;
                }
            }
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
                ((IDisposable)this).Dispose();
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
                if (this.IsDisposing == false)
                {
                    this.IsDisposing = true;
                    this.OnPostDisposing();
                    if (this.pool?.Collection is BattleObjectPool collection)
                    {
                        collection.PostRecycle(this);
                    }
                    else
                    {
                        ((IAutoRecycle)this).OnRecycle();
                    }
                }
            }
            else
            {
                retainCount--;
            }
        }

        protected virtual void OnPostDisposing() { }
        public bool IsDisposed { get; private set; }
        public bool IsDisposing { get; private set; }
        public bool IsDestoryed { get; private set; }
        public bool IsOnUse { get => pool != null; }
        public bool CanRecycle { get => retainCount <= 0 && IsDisposing; }
        public bool CanDispose { get => retainCount <= 0; }
        protected abstract void Disposing();
        protected virtual void Destructing() { }
        protected virtual void OnAlloc(bool NEW, object[] args) { }
        protected virtual void RecordDisposing() { }
        protected virtual void RecordReuse() { }
    }
}
