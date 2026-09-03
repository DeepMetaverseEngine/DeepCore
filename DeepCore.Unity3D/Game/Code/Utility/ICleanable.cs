using System;

namespace Code.Utility
{
    public interface ICleanable : IDisposable
    {
        void Clear();
    }
}
