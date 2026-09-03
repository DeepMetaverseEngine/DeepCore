using System.Collections.Generic;
using Code.Utility;

namespace Code.System.Pool
{
    public interface IPoolable
    {

    }

    public static class ObjectPool<T> where T : class, IPoolable, new()
    {
        private static ObjectPoolImpl<T> _pool;
        
        static ObjectPool()
        {
            _pool = PoolSystem.GetOrCreate<ObjectPoolImpl<T>>();
            _pool.CreateFunc = OnCreate;
        }

        private static T OnCreate()
        {
            return new T();
        }

        public static T Get()
        {
            return _pool.Get();
        }

        public static void Release(T o)
        {
            if (_pool.Contains(o))
            {
                UnityEngine.Debug.LogError("The object has been released!");
                return;
            }
            _pool.Release(o);
        }

        public static bool Contains(T o)
        {
            return _pool.Contains(o);
        }
    }

    public static class LinkedListNodePool<T>
    {
        private static ObjectPoolImpl<LinkedListNode<T>> _g_pool;
        private static ObjectPoolImpl<LinkedListNode<T>> Pool
        {
            get
            {
                if (_g_pool == null)
                {
                    _g_pool = PoolSystem.GetOrCreate<ObjectPoolImpl<LinkedListNode<T>>>();
                    _g_pool.CreateFunc = OnCreate;
                }
                return _g_pool;
            }
        }

        private static LinkedListNode<T> OnCreate()
        {
            return new LinkedListNode<T>(default(T));
        }

        public static LinkedListNode<T> Get()
        {
            return Pool.Get();
        }
        
        public static void Release(LinkedListNode<T> node)
        {
            node.Value = default(T);
            Pool.Release(node);
        }
    }

    public class ListPool<T> : List<T>, ICleanable, IPoolable where T : class, new()
    {
        public static ListPool<T> Get()
        {
            return ObjectPool<ListPool<T>>.Get();
        }
        public void Dispose()
        {
            Clear();
            ObjectPool<ListPool<T>>.Release(this);
        }
    }
}
