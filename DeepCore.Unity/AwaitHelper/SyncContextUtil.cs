using System.Threading;
using UnityEngine;

namespace DeepCore.Unity.AwaitHelper;

public static class SyncContextUtil
{

    public static int UnityThreadID  { get; private set; }

    public static SynchronizationContext UnitySynchronizationContext { get; private set; }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        UnitySynchronizationContext = SynchronizationContext.Current;
        UnityThreadID = Thread.CurrentThread.ManagedThreadId;
    }

}