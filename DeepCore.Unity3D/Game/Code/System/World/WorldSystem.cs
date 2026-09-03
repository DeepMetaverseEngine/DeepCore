using UnityEngine;

namespace Code.System.World
{
    public static class WorldSystem
    {
        private static WorldSystemImpl _inst;

        private static WorldSystemImpl Inst
        {
            get
            {
                if (_inst) return _inst;
                var go = new GameObject("[WorldSystem]");
                Object.DontDestroyOnLoad(go);
                _inst = go.AddComponent<WorldSystemImpl>();
                return _inst;
            }
        }

        public static long GenerateSerial()
        {
            return Inst.GenerateSerial();
        }

        public static T CreateSystem<T>() where T : BaseSystem, new()
        {
            return Inst.CreateSystem<T>();
        }

        public static T GetOrCreateSystem<T>() where T : BaseSystem, new()
        {
            return Inst.GetOrCreateSystem<T>();
        }

        internal static void AddSystem(BaseSystem system)
        {
            Inst.AddSystem(system);
        }

        public static void ReleaseSystem(BaseSystem system)
        {
            Inst.ReleaseSystem(system);
        }
    }
}