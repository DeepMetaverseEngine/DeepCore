using System;
using System.Collections.Generic;
using Code.Utility;

namespace Code.System.Pool
{
    public class ObjectPoolImpl<T> : ICleanable where T : class
    {
        private LinkedList<T> _queue = new LinkedList<T>();
        public Func<T> CreateFunc { set; private get; }

        public T Get()
        {
            var node = _queue.First;
            if (node == null) return CreateFunc.Invoke();
            var value = node.Value;
            node.List.Remove(node);
            return value;
        }

        public void Release(T o)
        {
            _queue.AddLast(o);
        }

        public bool Contains(T o)
        {
            return _queue.Contains(o);
        }
        
        public void Clear()
        {
            _queue.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
