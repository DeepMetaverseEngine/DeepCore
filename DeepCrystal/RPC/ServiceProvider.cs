using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeepCrystal.Threading;
using System.Linq;
using System.Dynamic;
using DeepCore.Threading;
using DeepCore;

namespace DeepCrystal.RPC
{
    //--------------------------------------------------------------------------------------------------------------------------------------
    public interface IServiceProvider : ITaskExecutor
    {
        /// <summary>
        /// 关闭当前服务
        /// </summary>
        void ShutdownSelf(string reason);
        /// <summary>
        /// 获取异步锁
        /// </summary>
        IAsyncLock CreateLock();

        /// <summary>
        /// 随服务卸载销毁
        /// </summary>
        /// <param name="disposable"></param>
        T AutoDispose<T>(T disposable) where T : IDisposable;
        void AutoDispose(IEnumerable<IDisposable> disposable);

        /// <summary>
        /// 注册模块监听
        /// </summary>
        /// <param name="module"></param>
        void RegistInvoker(object module);

        //-----------------------------------------------------------
        #region Threading

        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, object state, Action<ICornJobContext> callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, object state, Func<ICornJobContext, Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, Action<ICornJobContext> callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, Func<ICornJobContext, Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, Action callback, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        /// <summary>
        /// <see cref="CornJobExample"></see>
        /// </summary>
        Task<IDisposable> CreateCornJobAsync(string corn_expression, Func<Task> callbackAsync, CornJobMissFirePolicy missFire = CornJobMissFirePolicy.DoNothing);
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="state">一个包含回调方法要使用的信息的对象，或者为 null。</param>
        /// <param name="dueTime">System.TimeSpan，表示在 callback 参数调用它的方法之前延迟的时间量。 指定 -1 毫秒以防止启动计时器。 指定零 (0) 可立即启动计时器。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="dueTime">System.TimeSpan，表示在 callback 参数调用它的方法之前延迟的时间量。 指定 -1 毫秒以防止启动计时器。 指定零 (0) 可立即启动计时器。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns> 
        IDisposable CreateTimer(Action callback, TimeSpan dueTime, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="state">一个包含回调方法要使用的信息的对象，或者为 null。</param>
        /// <param name="dueTime">System.TimeSpan，表示在 callback 参数调用它的方法之前延迟的时间量。 指定 -1 毫秒以防止启动计时器。 指定零 (0) 可立即启动计时器。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Func<object, Task> callback, object state, TimeSpan dueTime, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="dueTime">System.TimeSpan，表示在 callback 参数调用它的方法之前延迟的时间量。 指定 -1 毫秒以防止启动计时器。 指定零 (0) 可立即启动计时器。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Func<Task> callback, TimeSpan dueTime, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="state">一个包含回调方法要使用的信息的对象，或者为 null。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Action<object> callback, object state, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Action callback, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="state">一个包含回调方法要使用的信息的对象，或者为 null。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Func<object, Task> callback, object state, TimeSpan period, bool missfire = true);
        /// <summary>
        /// 初始化 Timer 类的新实例，使用 System.TimeSpan 值来度量时间间隔。
        /// </summary>
        /// <param name="callback">一个 System.Action~1 委托，表示要执行的方法。</param>
        /// <param name="period">在调用 callback 所引用的方法之间的时间间隔。 指定 -1 毫秒可以禁用定期终止。</param>
        /// <param name="missfire">错过后执行</param>
        /// <returns></returns>
        IDisposable CreateTimer(Func<Task> callback, TimeSpan period, bool missfire = true);
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 创建TaskCompletionSource
        /// </summary>
        TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS);
        /// <summary>
        /// 创建TaskCompletionSource
        /// </summary>
        TaskCompletionSource<T> CreateTaskCompletionSource<T>(string name, TimeSpan timeoutMS, Action<TaskCompletionSource<T>> timeout);
        //-----------------------------------------------------------------------------------------------------------------
        #endregion
        //-----------------------------------------------------------
        #region GetService


        /// <summary>
        /// 获取远程服务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config">创建服务代入参数，可以在Service构造的时候取到</param>
        /// <returns></returns>
        Task<IRemoteService> GetOrCreateAsync(RemoteAddress path, IDictionary<string, string> config);
        Task<IRemoteService> GetOrCreateAsync(RemoteAddress path, object config = null);
        /// <summary>
        /// 获取远程服务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config">创建服务代入参数，可以在Service构造的时候取到</param>
        /// <returns></returns>
        Task<IRemoteService> CreateAsync(RemoteAddress path, IDictionary<string, string> config);
        Task<IRemoteService> CreateAsync(RemoteAddress path, object config = null);
        /// <summary>
        /// 获取远程服务
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<IRemoteService> GetAsync(RemoteAddress path);
        /// <summary>
        /// 获取远程服务
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<IRemoteService> GetStaticAsync(RemoteAddress path);

        /// <summary>
        /// 获取当前Node所有服务
        /// </summary>
        /// <returns></returns>
        List<IRemoteServiceInfo> GetLocalServices();

        /// <summary>
        /// 获取远程服务数量
        /// </summary>
        Task<int> GetServiceCountAsync(string serviceNode, string serviceType);
        /// <summary>
        /// 获取远程服务数量
        /// </summary>
        Task<int> GetServiceCountWithNodeAsync(string serviceNode);
        /// <summary>
        /// 获取远程服务数量
        /// </summary>
        Task<int> GetServiceCountWithTypeAsync(string serviceType);

        /// <summary>
        /// 获取一组远程服务
        /// </summary>
        Task<IRemoteService[]> GetServicesAsync(ICollection<string> servicesName);
        /// <summary>
        /// 根据正则表达式获取服务
        /// PatternFor "ServiceName@ServiceNode@ServiceType"
        /// eg: \w+@\w+@ServiceType
        /// </summary>
        Task<IRemoteService[]> GetServicesWithAddressPatternAsync(string pattern);
        /// <summary>
        /// 获取远端服务组
        /// Where("Address.ServiceType='LogicService'").OrderBy("Address.ServiceNode");
        /// </summary>
        Task<IRemoteService[]> GetServicesWithInfoLinqAsync(string where, string orderBy = null);
        /// <summary>
        /// 获取所有静态服务
        /// </summary>
        Task<IRemoteService[]> GetStaticServicesAsync();
        /// <summary>
        /// 获取静态服务
        /// </summary>
        Task<IRemoteService> FindStaticServiceAsync(Func<IRemoteServiceInfo[], IRemoteServiceInfo> select);
        /// <summary>
        /// 获取静态服务
        /// </summary>
        Task<IRemoteService> FindStaticServiceWithTypeAsync(string serviceType, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select);
        /// <summary>
        /// 获取静态服务
        /// </summary>
        Task<IRemoteService> FindStaticServiceWithNodeAsync(string serviceNode, Func<IRemoteServiceInfo[], IRemoteServiceInfo> select);

        /// <summary>
        /// 获取静态节点信息
        /// </summary>
        /// <returns></returns>
        Task<IRemoteNodeInfo[]> GetStaticNodesInfoAsync();

        #endregion
        //-----------------------------------------------------------
        #region ListenPush

        /// <summary>
        /// 注册监听所有事件回调
        /// </summary>
        IPushHandlerBinary ListenBinary(Action<BinaryMessage> action);
        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <typeparam name="T">监听类型</typeparam>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        IPushHandler Listen<T>(Action<T> action, bool recursion_base_type = true) where T : ISerializable;
        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <param name="type">监听类型</param>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        IPushHandler Listen(Type type, Action<ISerializable> action, bool recursion_base_type = true);
        /// <summary>
        /// 注册监听所有事件回调
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IPushHandler Listen(Action<ISerializable> action);
        /// <summary>
        /// 注册监听事件回调
        /// </summary>
        /// <param name="route">监听类型</param>
        /// <param name="action">监听回调</param>
        /// <param name="recursion_base_type">一并监听所有子类型</param>
        /// <returns></returns>
        IPushHandlerBinary ListenBinary(int route, Action<BinaryMessage> action, bool recursion_base_type = true);

        #endregion
        //-----------------------------------------------------------
        #region Broadcast

        void Broadcast(ISerializable notify);
        void BroadcastWithName(ICollection<string> servicesName, ISerializable notify);
        void BroadcastWithNode(string serviceNode, ISerializable notify);
        void BroadcastWithType(string serviceType, ISerializable notify);
        void BroadcastWithNodeAndType(string serviceNode, string serviceType, ISerializable notify);

        void WormholeBroadcastWithNode(string serviceNode, object message);
        void WormholeBroadcastWithType(string serviceType, object message);
        void WormholeBroadcastWithNodeAndType(string serviceNode, string serviceType, object message);

        #endregion
        //-----------------------------------------------------------
        #region Remote

        /// <summary>
        /// 直接根据服务名调用RPC（带返回值）
        /// </summary>
        void RemoteCall<RSP>(string serviceName, ISerializable req, OnRpcReturn<RSP> callback) where RSP : ISerializable;
        /// <summary>
        /// 直接根据服务名调用RPC（带返回值）
        /// </summary>
        void RemoteCall(string serviceName, BinaryMessage req, OnRpcReturnBinary callback);
        /// <summary>
        /// 直接根据服务名调用RPC（无返回值）
        /// </summary>
        void RemoteInvoke(string serviceName, ISerializable msg);
        /// <summary>
        /// 直接根据服务名调用RPC（无返回值）
        /// </summary>
        void RemoteInvoke(string serviceName, BinaryMessage msg);
        /// <summary>
        /// 直接根据服务名一次批量调用RPC（无返回值）
        /// </summary>
        void RemoteBatchInvoke(string serviceName, ICollection<ISerializable> batch);
        /// <summary>
        /// 直接根据服务名一次批量调用RPC（无返回值）
        /// </summary>
        void RemoteBatchInvoke(string serviceName, ICollection<BinaryMessage> batch);
        /// <summary>
        /// 直接根据服务名跨进程直接将消息传送过去，线程不安全。
        /// </summary>
        void RemoteWormholeTransport(string serviceName, object message);
        /// <summary>
        /// 直接根据服务名调用RPC（带返回值）
        /// </summary>
        Task<RSP> RemoteCallAsync<RSP>(string serviceName, ISerializable req) where RSP : ISerializable;
        /// <summary>
        /// 直接根据服务名调用RPC（带返回值）
        /// </summary>
        Task<BinaryMessage> RemoteCallAsync(string serviceName, BinaryMessage req);

        #endregion
        //-----------------------------------------------------------

        event OnWormholeTransportedHandler OnWormholeTransported;
        event OnWormholeAsyncTransportedHandler OnWormholeTransportedAsync;

        //-----------------------------------------------------------
    }


    /// <summary>
    /// 跨进程直接将消息传送过去，线程不安全。
    /// </summary>
    /// <param name="binary"></param>
    public delegate void OnWormholeTransportedHandler(RemoteAddress from, object message);
    /// <summary>
    /// 跨进程直接将消息传送过去，线程不安全。
    /// </summary>
    /// <param name="binary"></param>
    public delegate Task<object> OnWormholeAsyncTransportedHandler(RemoteAddress from, object message);

    //--------------------------------------------------------------------------------------------------------------------------------------
    public interface IPushHandler : IDisposable
    {
        bool IsDisposed { get; }
        bool IsRecursion { get; }
        Type Route { get; }
    }
    public interface IPushHandlerBinary : IDisposable
    {
        bool IsDisposed { get; }
        bool IsRecursion { get; }
        int Route { get; }
    }
    public interface IAsyncLock : IDisposable
    {
        Task<IDisposable> LockAsync();
        Task<IDisposable> LockAsync(CancellationToken ct);
    }
    interface IJobDetail
    {
        /// <summary>
        /// 本次：实际执行时间
        /// </summary>
        DateTimeOffset? FireTimeUtc { get; }
        /// <summary>
        /// 本次：计划执行时间
        /// </summary>
        DateTimeOffset? ScheduledFireTimeUtc { get; }
        /// <summary>
        /// 计划下次执行时间
        /// </summary>
        DateTimeOffset? NextFireTimeUtc { get; }
        /// <summary>
        /// 计划上次执行时间
        /// </summary>
        DateTimeOffset? PreviousFireTimeUtc { get; }
        object State { get; }
    }
    //--------------------------------------------------------------------------------------------------------------------------------------


}