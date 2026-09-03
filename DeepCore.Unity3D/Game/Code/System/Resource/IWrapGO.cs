using Code.Utility;
using UnityEngine;

namespace Code.System.Resource
{
    public interface IWrapGO : ICleanable
    {
        GameObject GameObject { get; }
        void CacheOrClear(float delaySec = 0);
    }
}