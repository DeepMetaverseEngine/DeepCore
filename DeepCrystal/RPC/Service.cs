using DeepCore;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    //--------------------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class IService : IServiceStop, IServiceStart, ITaskExecutor, IDisposable
    {
        public static Properties GlobalConfig { get; set; } = new Properties();
        //-----------------------------------------------------------------------------------------------
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("RPC:Service");
        public static int ActiveCount { get { return Alloc.ActiveCount; } }
        public static int AllocCount { get { return Alloc.AllocCount; } }
        //-----------------------------------------------------------------------------------------------
        public RemoteAddress SelfAddress { get; }
        public string Name { get => SelfAddress.ServiceName; }
        public string SelfNode { get => SelfAddress.ServiceNode; }
        public string SelfType { get => SelfAddress.ServiceType; }
        public RemoteAddress CreatorAddress { get; }
        public IOStreamPool ServerCodec { get; }
        public bool IsStatic { get; }
        public bool IsStarted { get { return is_started; } }
        public bool IsStopped { get { return is_stopped; } }
        public bool IsDisposed { get { return is_disposed; } }
        public Properties StartConfig { get; private set; }
        public ISharedMemory SharedMemory { get; private set; }
        public IServiceProvider Provider { get; private set; }
        public IRpcApplication CurrentApplication { get; private set; }
        public ServiceStopInfo StopInfo { get; private set; }
        public virtual ServiceProperties Properties
        {
            get
            {
                return new ServiceProperties()
                {
                    IsConcurrent = true,
                    IgnoreRequestError = false,
                    IgnoreResponseError = true,
                };
            }
        }

        //-----------------------------------------------------------------------------------------------

        private bool is_started = false;
        private bool is_stopped = false;
        private bool is_disposed = false;
        public Logger log { get; protected set; }

        /// <summary>
        /// 初始化服务
        /// </summary>
        public IService(ServiceStartInfo start)
        {
            Alloc.RecordConstructor(GetType());
            this.log = LoggerFactory.GetLogger(start.Address.ServiceName);
            this.CurrentApplication = start.Application;
            this.SelfAddress = start.Address;
            this.Provider = start.Provider;
            this.StartConfig = start.Config;
            this.ServerCodec = start.ServerCodec;
            this.CreatorAddress = start.CreatorAddress;
            this.IsStatic = start.IsStatic;
            this.SharedMemory = start.SharedMemory;
        }
        ~IService()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        void IDisposable.Dispose()
        {
            try
            {
                if (is_disposed) throw new Exception("Service Already Disposed : " + SelfAddress);
                is_disposed = true;
                this.OnModulesDisposing();
                this.OnDisposed();
            }
            finally
            {
                this.Provider = null;
                this.CurrentApplication = null;
                this.StopInfo = null;
                this.StartConfig = null;
                this.SharedMemory = null;
                Alloc.RecordDispose(GetType());
            }
        }
        public override string ToString()
        {
            return this.SelfAddress.ToString();
        }
        /// <summary>
        /// 获取当前服务状态
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual bool GetState(TextWriter output)
        {
            return false;
        }

        async Task IServiceStart.StartAsync()
        {
            if (is_started) { throw new Exception("Service Already Started : " + SelfAddress); }
            is_started = true;
            try
            {
                await this.OnStartAsync();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            await OnModulesStartAsync();
            try
            {
                await this.OnStartedAsync();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            await OnModulesStartedAsync();
        }
        async Task IServiceStop.StopAsync(ServiceStopInfo stop)
        {
            if (is_stopped) { throw new Exception("Service Already Stopped : " + SelfAddress); }
            this.StopInfo = stop;
            this.is_stopped = true;
            try
            {
                await this.OnStopAsync();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            await OnModulesStopAsync();
            try
            {
                await this.OnStoppedAsync();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            await OnModulesStopedAsync();
        }
        public void ShutdownSelf(string reason)
        {
            this.Provider.ShutdownSelf(reason);
        }

        //-----------------------------------------------------------------------------------------------


        /// <summary>
        /// 服务已被清理,一般和构造函数匹配（非主线程）
        /// </summary>
        protected abstract void OnDisposed();
        /// <summary>
        /// 开始服务（主线程）
        /// </summary>
        protected abstract Task OnStartAsync();
        /// <summary>
        /// 停止服务（主线程）
        /// </summary>
        protected abstract Task OnStopAsync();

        protected virtual Task OnStartedAsync() { return Task.CompletedTask; }
        protected virtual Task OnStoppedAsync() { return Task.CompletedTask; }


        //-----------------------------------------------------------------------------------------------
        #region ITaskExecutor

        /// <summary>
        /// 随服务卸载销毁
        /// </summary>
        /// <param name="disposable"></param>
        public T AutoDispose<T>(T disposable) where T : IDisposable { this.Provider.AutoDispose(disposable); return disposable; }
        public IDisposable AutoDispose(IDisposable disposable) { this.Provider.AutoDispose(disposable); return disposable; }
        public Task<TResult> FromResult<TResult>(TResult result) { return Provider.FromResult(result); }

        public void Run(Action action) { Provider.Execute(action); }
        public void Run(Func<Task> action) { Provider.Execute(action); }

        public Task Execute(Action action) { return Provider.Execute(action); }
        public Task Execute(Func<Task> action) { return Provider.Execute(action); }
        public Task<TResult> Execute<TResult>(Func<TResult> function) { return Provider.Execute(function); }
        public Task<TResult> Execute<TResult>(Func<Task<TResult>> function) { return Provider.Execute(function); }
        public Task Execute<TInput>(Action<TInput> action, TInput state) { return Provider.Execute(action, state); }
        public Task Execute<TInput>(Func<TInput, Task> action, TInput state) { return Provider.Execute(action, state); }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, TResult> function, TInput state) { return Provider.Execute(function, state); }
        public Task<TResult> Execute<TInput, TResult>(Func<TInput, Task<TResult>> function, TInput state) { return Provider.Execute(function, state); }
        public Task Execute(Task task) { return Provider.Execute(task); }
        public Task<TResult> Execute<TResult>(Task<TResult> task) { return Provider.Execute(task); }
        public Task Delay(TimeSpan dueTime) { return Provider.Delay(dueTime); }
        public Task Delay<TInput>(Action<TInput> callback, TInput state, TimeSpan dueTime) { return Provider.Delay(callback, state, dueTime); }
        public Task<TResult> Delay<TInput, TResult>(Func<TInput, TResult> callback, TInput state, TimeSpan dueTime) { return Provider.Delay(callback, state, dueTime); }

        #endregion
        //-----------------------------------------------------------------------------------------------
        #region Modules

        private List<IServiceModule> modules = new List<IServiceModule>(1);
        internal void RegistModule(IServiceModule module)
        {
            if (modules.Contains(module)) throw new Exception($"Module '{module.GetType()}' Already Registed!!!");
            modules.Add(module);
            Provider.RegistInvoker(module);
        }
        public M GetModel<M>(Predicate<M> predicate = null) where M : IServiceModule
        {
            foreach (var m in modules)
            {
                if (m is M model && (predicate == null || predicate.Invoke(model)))
                {
                    return model;
                }
            }
            return null;
        }
        public bool TryGetModel<M>(out M ret, Predicate<M> predicate = null) where M : IServiceModule
        {
            foreach (var m in modules)
            {
                if (m is M model && (predicate == null || predicate.Invoke(model)))
                {
                    ret = model;
                    return true;
                }
            }
            ret = null;
            return false;
        }

        public void ForEachModules(Action<IServiceModule> action)
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    action(module);
                }
            }
        }
        public void ForEachModules<M>(Action<M> action)
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    if (module is M m) action(m);
                }
            }
        }
        public async Task ForEachModulesAsync(Func<IServiceModule, Task> action)
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    await action(module);
                }
            }
        }
        public async Task ForEachModulesAsync<M>(Func<M, Task> action)
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    if (module is M m) await action(m);
                }
            }
        }

        private void OnModulesDisposing()
        {
            var list = new List<IServiceModule>(modules);
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        module.Dispose();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
            modules.Clear();
        }
        private async Task OnModulesStartAsync()
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStartAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                        throw;
                    }
                }
            }
        }
        private async Task OnModulesStartedAsync()
        {
            var list = new List<IServiceModule>(modules);
            {
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStartedAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        private async Task OnModulesStopAsync()
        {
            var list = new List<IServiceModule>(modules);
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStopAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        private async Task OnModulesStopedAsync()
        {
            var list = new List<IServiceModule>(modules);
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStoppedAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }






        #endregion
        //-----------------------------------------------------------------------------------------------
    }
    //--------------------------------------------------------------------------------------------------------------------------------------
    public class ServiceStartInfo
    {
        public readonly IRpcApplication Application;
        public readonly IServiceProvider Provider;
        public readonly RemoteAddress Address;
        public readonly RemoteAddress CreatorAddress;
        public readonly Properties Config;
        public readonly IOStreamPool ServerCodec;
        public readonly bool IsStatic;
        public readonly ISharedMemory SharedMemory;
        public ServiceStartInfo(IRpcApplication app, IServiceProvider provider, RemoteAddress address, IDictionary<string, string> config, IOStreamPool codec, RemoteAddress creator, bool isStatic, ISharedMemory sharedMemory)
        {
            this.Application = app;
            this.Provider = provider;
            this.Address = address;
            this.CreatorAddress = creator;
            this.Config = new Properties(config);
            this.ServerCodec = codec;
            this.IsStatic = isStatic;
            this.SharedMemory = sharedMemory;
        }
    }
    public class ServiceStopInfo
    {
        public enum ShutdownEvent
        {
            START_ERROR,
            RPC_SHUTDOWN,
            SELF_SHUTDOWN,
        }
        public readonly ShutdownEvent Event;
        public readonly RemoteAddress FromAddress;
        public readonly Exception LacunchError;
        public readonly string Reason;
        public ServiceStopInfo(ShutdownEvent @event, RemoteAddress remoteAddress, Exception lacunchError, string reason)
        {
            this.Event = @event;
            this.FromAddress = remoteAddress;
            this.LacunchError = lacunchError;
            this.Reason = reason;
        }
    }
    public struct ServiceProperties
    {
        public bool IsConcurrent;
        public bool IgnoreRequestError;
        public bool IgnoreResponseError;
    }
    //--------------------------------------------------------------------------------------------------------------------------------------
    public interface IServiceStart
    {
        Task StartAsync();
    }
    public interface IServiceStop
    {
        Task StopAsync(ServiceStopInfo stop);
    }
}
