using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Cysharp.Threading.Tasks
{
    public static class Fake
    {
        public static AsyncOperationAwaiter GetAwaiter(this AsyncOperation asyncOperation) => throw new NotImplementedException();
        public static UniTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken) => throw new NotImplementedException();
        public static UniTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately) => throw new NotImplementedException();
        public static UniTask ToUniTask(this AsyncOperation asyncOperation) => throw new NotImplementedException();
        public struct AsyncOperationAwaiter : ICriticalNotifyCompletion
        {
            public AsyncOperationAwaiter(AsyncOperation asyncOperation)
            {
            }
            public bool IsCompleted =>            throw new NotImplementedException();
            public void GetResult() => throw new NotImplementedException();
            public void OnCompleted(Action continuation) => throw new NotImplementedException();
            public void UnsafeOnCompleted(Action continuation) => throw new NotImplementedException();
        }

        public static UniTask RunOnThreadPool(Func<UniTask> action, bool configureAwait = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public static UniTask SwitchToMainThread(Func<UniTask> action, bool configureAwait = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();


    }

}
