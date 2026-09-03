
namespace Code.System.Resource
{
    public class TaskStepWrapBundle : ITaskStep
    {
        public string Url { get; }
        public bool IsCompleted { get; }
        
        public void Start(bool bAsync = true)
        {
        }

        public void Invoke(long serial)
        {
        }
        
        public void Clear()
        {
        }

        public void Dispose()
        {
        }
    }
}
