using Code.Utility;

namespace Code.System.Resource
{
    public interface ITaskStep : ICleanable
    {
        string Url { get; }
        bool IsCompleted { get; }
        void Start(bool bAsync = true);
        void Invoke(long serial);
    }
}