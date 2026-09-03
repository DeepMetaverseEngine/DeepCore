using System;
using System.Collections.Generic;
using System.Threading;

namespace DeepCore.Event.EventSystem
{
    public interface ILocker : IDisposable
    {
        int ThreadID { get; }
    }

    public class CtrlLocker : IDisposable
    {
        private ILocker mLocker;
        public bool IsDisposed { get; private set; }

        public void SetLocker(ILocker locker)
        {
            mLocker?.Dispose();
            mLocker = locker;
        }

        public void Dispose()
        {
            mLocker?.Dispose();
            IsDisposed = true;
        }
    }

    public class NormalLocker : ILocker
    {
        public int ThreadID { get; }
        private readonly object mLockSlim;
        private readonly Action mDisposeAction;
        public void Dispose()
        {
            Monitor.Exit(mLockSlim);
            mDisposeAction?.Invoke();
        }

        public NormalLocker(object locker, Action act = null)
        {
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            mLockSlim = locker;
            mDisposeAction = act;
            Monitor.Enter(mLockSlim);
        }
    }

    public class NoneLocker : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class ReadLocker : ILocker
    {
        private readonly ReaderWriterLockSlim mLockSlim;
        public int ThreadID { get; }


        public ReadLocker(ReaderWriterLockSlim lockSlim)
        {
            mLockSlim = lockSlim;
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            mLockSlim.EnterReadLock();

        }

        public void Dispose()
        {
            mLockSlim.ExitReadLock();
        }
    }

    public class ReadLocker<T> : ILocker
    {
        public T Data => mData.InnerData;
        private readonly ReadWriteLockable<T> mData;
        private readonly ReaderWriterLockSlim mLockSlim;
        public int ThreadID { get; }

        public ReadLocker(ReaderWriterLockSlim lockSlim, ReadWriteLockable<T> t)
        {
            mLockSlim = lockSlim;
            mData = t;
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            mLockSlim.EnterReadLock();

        }

        public void Dispose()
        {
            mLockSlim.ExitReadLock();
        }
    }

    public class WriteLocker : ILocker
    {
        private readonly ReaderWriterLockSlim mLockSlim;
        public int ThreadID { get; }

        public WriteLocker(ReaderWriterLockSlim lockSlim)
        {
            mLockSlim = lockSlim;
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            mLockSlim.EnterWriteLock();
        }

        public void Dispose()
        {
            mLockSlim.ExitWriteLock();
        }
    }

    public class WriteLocker<T> : ILocker
    {
        public T Data => mData.InnerData;
        private readonly ReadWriteLockable<T> mData;
        private readonly ReaderWriterLockSlim mLockSlim;
        public int ThreadID { get; }
        public WriteLocker(ReaderWriterLockSlim lockSlim, ReadWriteLockable<T> t)
        {
            mLockSlim = lockSlim;
            ThreadID = Thread.CurrentThread.ManagedThreadId;
            mData = t;
            mLockSlim.EnterWriteLock();
        }

        public void Dispose()
        {
            mLockSlim.ExitWriteLock();
        }
    }


    public class ReadWriteLockable : IDisposable
    {
        /// <summary>
        /// ReaderWriterLockSlim 的LockRecursionPolicy在Unity无法使用
        /// </summary>
        protected readonly ReaderWriterLockSlim mLockSlim = new ReaderWriterLockSlim();

        public bool IsWriteLockHeld => mLockSlim.IsWriteLockHeld;
        public bool IsUpgradeableReadLockHeld => mLockSlim.IsUpgradeableReadLockHeld;
        public bool IsReadLockHeld => mLockSlim.IsReadLockHeld;

        protected void EnterReadLock()
        {
            mLockSlim.EnterReadLock();
        }

        protected void ExitReadLock()
        {
            mLockSlim.ExitReadLock();
        }

        protected void EnterWriteLock()
        {
            mLockSlim.EnterWriteLock();
        }

        protected void ExitWriteLock()
        {
            mLockSlim.ExitWriteLock();
        }

        protected void EnterUpgradeLock()
        {
            mLockSlim.EnterUpgradeableReadLock();
        }

        protected void ExitUpgradeLock()
        {
            mLockSlim.ExitUpgradeableReadLock();
        }

        public virtual void Dispose()
        {
            mLockSlim?.Dispose();
        }
    }

    public class ReadWriteLockable<T> : ReadWriteLockable
    {
        public readonly T InnerData;

        public ReadWriteLockable(T data)
        {
            InnerData = data;
        }

        public WriteLocker<T> LockWrite()
        {
            return new WriteLocker<T>(mLockSlim, this);
        }

        public ReadLocker<T> LockRead()
        {
            return new ReadLocker<T>(mLockSlim, this);
        }
    }
}