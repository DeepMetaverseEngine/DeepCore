namespace Code.System.World
{
    public abstract class SingleSystem<T> : BaseSystem where T : BaseSystem, new()
    {
        private static T _inst;
        
        public static T Inst
        {
            get 
            {
                if (_inst == null)
                {
                    _inst = WorldSystem.GetOrCreateSystem<T>();
                }
                return _inst;
            }
        }
    }
}
