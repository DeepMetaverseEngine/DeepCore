using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal
{
    public class AsyncLock : IAsyncLock
    {
        private SemaphoreSlim accLock = new SemaphoreSlim(1, 1);
        public AsyncLock()
        {
        }
        public async Task<IDisposable> LockAsync()
        {
            await accLock.WaitAsync();
            return new LockImpl(accLock);
        }
        public async Task<IDisposable> LockAsync(CancellationToken ct)
        {
            await accLock.WaitAsync(ct);
            return new LockImpl(accLock);
        }
        public IDisposable Lock()
        {
            accLock.Wait();
            return new LockImpl(accLock);
        }
        public IDisposable Lock(CancellationToken ct)
        {
            accLock.Wait(ct);
            return new LockImpl(accLock);
        }
        public void Dispose()
        {
            accLock?.Dispose();
            accLock = null;
        }
        struct LockImpl : IDisposable
        {
            SemaphoreSlim accLock;
            public LockImpl(SemaphoreSlim acc)
            {
                this.accLock = acc;
            }
            public void Dispose()
            {
                accLock.Release();
            }
        }
    }
}
