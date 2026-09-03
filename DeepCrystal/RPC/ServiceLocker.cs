using DeepCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    public class ServiceAsyncLock : IAsyncLock
    {
        private IServiceProvider svc;
        private SemaphoreSlim accLock = new SemaphoreSlim(1, 1);

        public ServiceAsyncLock(IServiceProvider svc)
        {
            this.svc = svc;
        }
        public async Task<IDisposable> LockAsync()
        {
            await accLock.WaitAsync();
            return await svc.Execute(Task.FromResult(new LockImpl(accLock)));
        }
        public async Task<IDisposable> LockAsync(CancellationToken ct)
        {
            await accLock.WaitAsync(ct);
            return await svc.Execute(Task.FromResult(new LockImpl(accLock)));
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

    public class AsyncLockDictionary<K> : Disposable
    {
        private readonly IServiceProvider svc;
        private readonly HashMap<K, IAsyncLock> lockMap;
        public AsyncLockDictionary(IServiceProvider service)
        {
            this.svc = service;
            this.lockMap = new HashMap<K, IAsyncLock>();
        }
        protected override void Disposing()
        {
            foreach (var e in lockMap.Values)
            {
                e.Dispose();
            }
            lockMap.Clear();
        }
        public Task<IDisposable> LockAsync(K key)
        {
            var locker = lockMap.GetOrAdd(key, k => svc.CreateLock());
            return locker.LockAsync();
        }
        public Task<IDisposable> LockAsync(K key, CancellationToken ct)
        {
            var locker = lockMap.GetOrAdd(key, k => svc.CreateLock());
            return locker.LockAsync(ct);
        }
    }

}
