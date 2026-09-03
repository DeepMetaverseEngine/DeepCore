using System;
using System.Collections.Generic;
using Code.Utility;
using UnityEngine;

namespace Code.System.Pool
{
    public static class PoolSystem
    {
        private static Dictionary<Type, ICleanable> _pools = new Dictionary<Type, ICleanable>();

        static PoolSystem()
        {
            Application.lowMemory += ApplicationOnLowMemory;
        }

        private static void ApplicationOnLowMemory()
        {
            Clear();
        }

        public static T GetOrCreate<T>() where T : class, ICleanable, new()
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new T();
                _pools.Add(type, pool);
            }

            return pool as T;
        }

        public static void Clear()
        {
            foreach (var pair in _pools)
            {
                pair.Value.Clear();
            }
        }
    }
}

