using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

// This can be used as a way to return to the main unity thread when using multiple threads
// with async methods

namespace DeepCore.Unity.AwaitHelper;

// ReSharper disable once CheckNamespace
public class WaitForUpdate : CustomYieldInstruction
{
    public override bool keepWaiting => false;
}

public class WaitForBackgroundThread
{
    public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
    {
        return Task.Run(() => {}).ConfigureAwait(false).GetAwaiter();
    }
}