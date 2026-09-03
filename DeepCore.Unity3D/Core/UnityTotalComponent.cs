using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D
{
    [RequireComponent(typeof(UnityDelayInvoke))]
    [RequireComponent(typeof(UnityMainThreadDispatcher))]
    class UnityTotalComponent : MonoBehaviour
    {
        public UnityDelayInvoke DelayInvoke;
        public UnityMainThreadDispatcher MainThreadDispatcher;

        private void Awake()
        {
            DelayInvoke = GetComponent<UnityDelayInvoke>();
            MainThreadDispatcher = GetComponent<UnityMainThreadDispatcher>();
        }

    }
}
