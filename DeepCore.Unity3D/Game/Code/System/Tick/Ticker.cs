using Code.System.Pool;
using Code.Utility;

namespace Code.System.Tick
{
    public class Ticker : ICleanable, IPoolable
    {
        public long Serial;
        public float IntervalTime;
        public TickSystem.TickHandler Callback;
        public int Count;
        public float PastTime;
        public int Index;
        
        public void Clear()
        {
            Callback = null;
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<Ticker>.Release(this);
        }
    }
}
