using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading
{



    public static class AutoLocker
    {
        struct Write : IDisposable
        {
            private ReaderWriterLockSlim locker;
            public Write(ReaderWriterLockSlim locker)
            {
                this.locker = locker;
                this.locker.EnterWriteLock();
            }
            public void Dispose()
            {
                this.locker.ExitWriteLock();
            }
        }
        struct Read : IDisposable
        {
            private ReaderWriterLockSlim locker;
            public Read(ReaderWriterLockSlim locker)
            {
                this.locker = locker;
                this.locker.EnterReadLock();
            }
            public void Dispose()
            {
                this.locker.ExitReadLock();
            }
        }
        struct UpgradeableRead : IDisposable
        {
            private ReaderWriterLockSlim locker;
            public UpgradeableRead(ReaderWriterLockSlim locker)
            {
                this.locker = locker;
                this.locker.EnterUpgradeableReadLock();
            }
            public void Dispose()
            {
                this.locker.ExitUpgradeableReadLock();
            }
        }

        public static IDisposable EnterWrite(this ReaderWriterLockSlim locker)
        {
            return new Write(locker);
        }
        public static IDisposable EnterRead(this ReaderWriterLockSlim locker)
        {
            return new Read(locker);
        }
        public static IDisposable EnterUpgradeableRead(this ReaderWriterLockSlim locker)
        {
            return new UpgradeableRead(locker);
        }


        struct SemaphoreSlimLocker : IDisposable
        {
            private SemaphoreSlim semaphore;
            public SemaphoreSlimLocker(SemaphoreSlim locker)
            {
                this.semaphore = locker;
            }
            public IDisposable Wait()
            {
                this.semaphore.Wait();
                return this;
            }
            public async Task<IDisposable> WaitAsync()
            {
                await this.semaphore.WaitAsync();
                return this;
            }
            void IDisposable.Dispose()
            {
                this.semaphore.Release();
            }
        }
        public static IDisposable EnterWait(this SemaphoreSlim locker)
        {
            return new SemaphoreSlimLocker(locker).Wait();
        }
        public static Task<IDisposable> EnterWaitAsync(this SemaphoreSlim locker)
        {
            return new SemaphoreSlimLocker(locker).WaitAsync();
        }
    }

}
