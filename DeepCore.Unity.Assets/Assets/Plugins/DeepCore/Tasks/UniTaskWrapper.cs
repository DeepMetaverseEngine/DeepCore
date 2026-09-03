using Cysharp.Threading.Tasks;
using DeepCore.Unity;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#if false
namespace DeepCore.Unity3D.Tasks
{
    public class UniWrapperTaskFactory : WrapperTaskFactory
    {
        public override IWrapperTask CompletedTask { get; }

        public override WrapperTaskCompletionSource CreateTaskCompletionSource() => new UniWrapperTaskCompletionSource();
        public override WrapperTaskCompletionSource<T> CreateTaskCompletionSource<T>() => new UniWrapperTaskCompletionSource<T>();

        public override IWrapperTask FromResult() => new UniWrapperTask(UniTask.FromResult(0));
        public override IWrapperTask<T> FromResult<T>(T t) => new UniWrapperTask<T>(UniTask.FromResult<T>(t));

        public override IWrapperTask FromException(Exception err) => new UniWrapperTask(UniTask.FromException(err));
        public override IWrapperTask<T> FromException<T>(Exception err) => new UniWrapperTask<T>(UniTask.FromException<T>(err));


        protected override void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            throw new NotImplementedException();
        }
        protected override void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            throw new NotImplementedException();
        }
        protected override void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        {
            throw new NotImplementedException();
        }
        protected override void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        {
            throw new NotImplementedException();
        }



        struct UniWrapperTask : IWrapperTask
        {
            UniTask t;
            public UniWrapperTask(UniTask t) { this.t = t; }
            public IWrapperAwaiter GetAwaiter() => new UniAwaiterWrapper(t.GetAwaiter());
        }
        struct UniWrapperTask<T> : IWrapperTask<T>
        {
            UniTask<T> t;
            public UniWrapperTask(UniTask<T> t) { this.t = t; }
            public IWrapperAwaiter GetAwaiter() => new UniAwaiterWrapper<T>(t.GetAwaiter());
        }
        struct UniAwaiterWrapper : IWrapperAwaiter
        {
            UniTask.Awaiter awaiter;
            public UniAwaiterWrapper(UniTask.Awaiter awaiter) { this.awaiter = awaiter; }
            public bool IsCompleted => awaiter.IsCompleted;
            public void GetResult() => awaiter.GetResult();
            public void OnCompleted(Action continuation) => awaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => awaiter.UnsafeOnCompleted(continuation);
        }
        struct UniAwaiterWrapper<T> : IWrapperAwaiter
        {
            UniTask<T>.Awaiter awaiter;
            public UniAwaiterWrapper(UniTask<T>.Awaiter awaiter) { this.awaiter = awaiter; }
            public bool IsCompleted => awaiter.IsCompleted;
            public void GetResult() => awaiter.GetResult();
            public void OnCompleted(Action continuation) => awaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => awaiter.UnsafeOnCompleted(continuation);
        }
        struct UniWrapperTaskCompletionSource : WrapperTaskCompletionSource
        {
            UniTaskCompletionSource tcs;
            public UniWrapperTaskCompletionSource() { tcs = new UniTaskCompletionSource(); }
            public IWrapperTask Task => new UniWrapperTask(tcs.Task);
            public bool TrySetResult() => tcs.TrySetResult();
            public bool TrySetCanceled(CancellationToken cancellationToken = default) => tcs.TrySetCanceled(cancellationToken);
            public bool TrySetException(Exception exception) => tcs.TrySetException(exception);
        }
        struct UniWrapperTaskCompletionSource<T> : WrapperTaskCompletionSource<T>
        {
            UniTaskCompletionSource<T> tcs;
            public UniWrapperTaskCompletionSource() { tcs = new UniTaskCompletionSource<T>(); }
            public IWrapperTask<T> Task => new UniWrapperTask<T>(tcs.Task);
            public bool TrySetResult(T res) => tcs.TrySetResult(res);
            public bool TrySetCanceled(CancellationToken cancellationToken = default) => tcs.TrySetCanceled(cancellationToken);
            public bool TrySetException(Exception exception) => tcs.TrySetException(exception);
        }
    }

}

#endif