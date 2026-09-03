using Code.System.Pool;
using Code.Utility;

namespace Code.BattleView.MaterialActions
{
    public abstract class MaterialAction : ICleanable, IPoolable
    {
        public bool IsDone { get; protected set; }
        public void Update(int deltaMS)
        {
            OnUpdate(deltaMS);
        }

        protected abstract void OnUpdate(int deltaMS);
            
        public void Dispose()
        {
            Clear();
            Disposing();
        }

        protected abstract void Disposing();

        public void Clear()
        {
            OnClear();
            IsDone = false;
        }

        protected abstract void OnClear();
    }
}
