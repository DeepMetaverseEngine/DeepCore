using Code.Utility;

namespace Code.System.AB
{
    public abstract class Reference : ICleanable
    {
        public int RefCount { get; private set; }

        public void Retain()
        {
            RefCount++;
        }

        public void Release()
        {
            RefCount--;
        }

        public void Dispose()
        {
            Clear();
            Disposing();
        }

        protected abstract void Disposing();

        public void Clear()
        {
            OnClear();
            RefCount = 0;
        }

        protected abstract void OnClear();
    }
}
