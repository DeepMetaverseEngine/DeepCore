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
    //-------------------------------------------------------------------------------------------------------------------------------

   
    public abstract class AbstractObjectPool : ObjectPools, ObjectPool
    {
        public Type PoolType { get; }
        public AbstractCollectionPool Collection { get; internal set; }
        public abstract int StackCount { get; }
        protected AbstractObjectPool(Type type) { PoolType = type; }
        public object CreateNew<ST>(ST st, ObjectPoolOnCreate<ST> m_ActionOnCreate)
        {
            if (m_ActionOnCreate != null)
            {
                return m_ActionOnCreate(st, this);
            }
            else
            {
                return DeepActivator.CreateInstance(PoolType);
            }
        }
        public abstract bool TryGetOrNewObject<ST>(out object obj, ST st, ObjectPoolOnCreate<ST> create);
        public abstract void ReleaseObject(object obj);
        public abstract void Clear();
        protected override void Disposing()
        {
            Clear();
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public class ConcurrentObjectPool : AbstractObjectPool
    {
        private readonly ConcurrentQueue<object> m_Stack = new();
        public override int StackCount => m_Stack.Count;
        public ConcurrentObjectPool(Type type) : base(type)
        {
            this.AsSynchronizedDisposing();
        }
        sealed public override bool TryGetOrNewObject<ST>(out object element, ST st, ObjectPoolOnCreate<ST> create)
        {
            if (EnableObjectPool)
            {
                if (this.TryPopInternal(out element))
                {
                    RecordOutPool(element.GetType());
                    return true;
                }
                else
                {
                    element = CreateNew(st, create);
                    RecordAlloc(element.GetType());
                    return false;
                }
            }
            else
            {
                element = CreateNew(st, create);
                return false;
            }
        }
        sealed public override void ReleaseObject(object element)
        {
            if (EnableObjectPool)
            {
                if (EnableStatistics)
                {
                    if (this.ContainsInternal(element))
                    {
                        throw new Exception("Internal error. Trying to destroy object that is already released to pool.");
                    }
                }
                if (MaxObjectCount > 0 && this.StackCount >= MaxObjectCount)
                {
                    RecordDropPool(element.GetType());
                    return;
                }
                this.PushInternal(element);
                RecordInPool(element.GetType());
            }
        }
        sealed public override void Clear()
        {
            while (m_Stack.TryDequeue(out var e))
            {
                if (e is IPoolingObject p) { p.OnDestory(this); }
            }
        }
        private bool TryPopInternal(out object ret)
        {
            if (m_Stack.TryDequeue(out ret))
            {
                if (ret is IRecyclable auto && auto.CanRecycle == false)
                {
                    log.Warn($"Object {ret.GetType()} cannot be recycled, returning to pool instead of recycling it.");
                    m_Stack.Enqueue(auto);                    
                    return false;
                }
                return true;
            }
            return false;
        }
        private void PushInternal(object element)
        {
            this.m_Stack.Enqueue(element);
        }
        private bool ContainsInternal(object ret)
        {
            return (m_Stack.Contains(ret));
        }
    }
    public class ConcurrentObjectPool<T> : ConcurrentObjectPool, ObjectPool<T>
    {
        public ConcurrentObjectPool() : base(typeof(T)) { }
        public bool TryGetOrNew<ST>(out T element, ST st, OnCreateInPool<T, ST> create)
        {
            var ret = base.TryGetOrNewObject(out var obj, (st, create), static (st, p) => st.create(st.st, p));
            element = (T)obj;
            return ret;
        }
        public void Release(T obj)
        {
            base.ReleaseObject(obj);
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------
    public class SingleThreadObjectPool : AbstractObjectPool, IDisposable
    {
        private readonly Queue<object> m_Stack = new();
        public override int StackCount { get => m_Stack.Count; }
        public SingleThreadObjectPool(Type type) : base(type) { }
        sealed public override bool TryGetOrNewObject<ST>(out object element, ST st, ObjectPoolOnCreate<ST> create)
        {
            if (TryPopInternal(out element))
            {
                return true;
            }
            else
            {
                element = CreateNew(st, create);
                return false;
            }
        }
        sealed public override void ReleaseObject(object element)
        {
            PushInternal(element);
        }
        sealed public override void Clear()
        {
            while (m_Stack.TryDequeue(out var e))
            {
                if (e is IPoolingObject p) { p.OnDestory(this); }
            }
        }
        private bool TryPopInternal(out object ret)
        {
            if (m_Stack.TryDequeue(out ret))
            {
                if (ret is IRecyclable auto && auto.CanRecycle == false)
                {
                    log.Warn($"Object {ret.GetType()} cannot be recycled, returning to pool instead of recycling it.");
                    m_Stack.Enqueue(auto);
                    return false;
                }
                return true;
            }
            return false;
        }
        private void PushInternal(object element)
        {
            this.m_Stack.Enqueue(element);
        }
    }
    public class SingleThreadObjectPool<T> : SingleThreadObjectPool, ObjectPool<T>
    {
        public SingleThreadObjectPool() : base(typeof(T)) { }
        public bool TryGetOrNew<ST>(out T element, ST st, OnCreateInPool<T, ST> create)
        {
            var ret = base.TryGetOrNewObject(out var obj, (st, create), static (st, p) => st.create(st.st, p));
            element = (T)obj;
            return ret;
        }
        public void Release(T obj)
        {
            base.ReleaseObject(obj);
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------------------------------------------------






}
