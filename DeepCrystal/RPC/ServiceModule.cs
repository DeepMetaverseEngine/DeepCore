using DeepCore;
using DeepCore.Log;
using DeepCrystal.ORM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    public abstract class IServiceModule : Disposable
    {
        public string VisibleName { get;  }
        public IService Service { get; private set; }
        public IServiceProvider Provider => Service.Provider;
        protected Logger log => Service.log;
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("RPC:ServiceModule");
        public IServiceModule(IService service)
        {
            this.VisibleName = $"{GetType().ToVisibleName()}@{service.GetType().ToVisibleName()}";
            Alloc.RecordConstructor(VisibleName);
            this.Service = service;
            this.Service.RegistModule(this);
        }
        ~IServiceModule()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(VisibleName);
            }
            Alloc.RecordDestructor(VisibleName);
        }
        protected sealed override void RecordDisposing()
        {
            Alloc.RecordDispose(VisibleName);
        }
        protected override void Disposing() { }
        public virtual Task OnStartAsync() { return Task.CompletedTask; }
        public virtual Task OnStartedAsync() { return Task.CompletedTask; }
        public virtual Task OnStopAsync() { return Task.CompletedTask; }
        public virtual Task OnStoppedAsync() { return Task.CompletedTask; }
    }
    public abstract class IServiceModule<T> : IServiceModule where T : IService
    {
        new public T Service { get => base.Service as T; }
        public IServiceModule(T service) : base(service)
        {
        }
    }

}
