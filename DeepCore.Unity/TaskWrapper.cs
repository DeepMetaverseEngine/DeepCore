
#if false
using DeepCore.Unity.AwaitHelper;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;
namespace DeepCore.Unity
{
    [AsyncMethodBuilder(typeof(WrapperAsyncTaskMethodBuilder))]
    public interface IWrapperTask
    {
        IWrapperAwaiter GetAwaiter();
    }
    [AsyncMethodBuilder(typeof(WrapperAsyncTaskMethodBuilder<>))]
    public interface IWrapperTask<T> : IWrapperTask
    {
    }
    public interface IWrapperAwaiter : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
    public interface IWrapperAwaiter<T> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }


    public interface WrapperTaskCompletionSource
    {
        IWrapperTask Task { get; }
        bool TrySetResult();
        bool TrySetCanceled(CancellationToken cancellationToken = default);
        bool TrySetException(Exception exception);
    }
    public interface WrapperTaskCompletionSource<T>
    {
        IWrapperTask<T> Task { get; }
        bool TrySetResult(T result);
        bool TrySetCanceled(CancellationToken cancellationToken = default);
        bool TrySetException(Exception exception);
    }

    public abstract class WrapperTaskFactory
    {
        public static WrapperTaskFactory Factory { get; private set; }
        public WrapperTaskFactory()
        {
            Factory = this;
        }
        public abstract IWrapperTask CompletedTask { get; }

        public abstract WrapperTaskCompletionSource CreateTaskCompletionSource();
        public abstract WrapperTaskCompletionSource<T> CreateTaskCompletionSource<T>();

        public abstract IWrapperTask FromResult();
        public abstract IWrapperTask<T> FromResult<T>(T t);

        public abstract IWrapperTask FromException(Exception err);
        public abstract IWrapperTask<T> FromException<T>(Exception err);


        internal protected abstract void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine;
        internal protected abstract void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine;
        internal protected abstract void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine;
        internal protected abstract void SetStateMachine(IAsyncStateMachine stateMachine);
    }

    [StructLayout(LayoutKind.Auto)]
    public struct WrapperAsyncTaskMethodBuilder
    {
        public static WrapperAsyncTaskMethodBuilder Create() => default;
        private IWrapperTask m_task;
        public IWrapperTask Task => m_task ?? WrapperTaskFactory.Factory.FromResult();
        public void SetResult() => m_task = WrapperTaskFactory.Factory.FromResult();
        public void SetException(Exception exception) => m_task = WrapperTaskFactory.Factory.FromException(exception);
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.Start(ref stateMachine);
        }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            WrapperTaskFactory.Factory.SetStateMachine(stateMachine);
        }
    }
    [StructLayout(LayoutKind.Auto)]
    public struct WrapperAsyncTaskMethodBuilder<T>
    {
        public static WrapperAsyncTaskMethodBuilder Create() => default;
        private IWrapperTask<T> m_task;
        public IWrapperTask<T> Task => m_task ?? WrapperTaskFactory.Factory.FromResult<T>(default(T));
        public void SetResult(T result) => m_task = WrapperTaskFactory.Factory.FromResult<T>(result);
        public void SetException(Exception exception) => m_task = WrapperTaskFactory.Factory.FromException<T>(exception);
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.Start(ref stateMachine);
        }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            WrapperTaskFactory.Factory.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            WrapperTaskFactory.Factory.SetStateMachine(stateMachine);
        }
    }
}
#endif