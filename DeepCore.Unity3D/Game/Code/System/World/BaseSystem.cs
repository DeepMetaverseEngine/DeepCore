using DeepCore;

namespace Code.System.World
{
    public abstract class BaseSystem : Disposable
    {

        public BaseSystem()
        {
            WorldSystem.AddSystem(this);
        }

        internal void Create()
        {
            OnCreate();
        }
        
        protected virtual void OnCreate()
        {
            
        }

        internal void Update(float deltaTime)
        {
            if (IsDisposed) return;
            OnUpdate(deltaTime);
        }

        protected virtual void OnUpdate(float deltaTime)
        {
            
        }

        protected override void Disposing()
        {
            WorldSystem.ReleaseSystem(this);
        }
    }
}
