using System;
using System.Collections.Generic;

namespace Code.Utility
{
    public class LRUCache<K, V> : ICleanable, IPoolable
        where K : class, IComparable, IComparable<K>
        where V : class
    {
        private const int DefaultCapacity = 20;
        private readonly LinkedList<V> _list = new LinkedList<V>();

        private readonly Dictionary<V, LinkedListNode<LinkedListNode<V>>> _vSnap =
            new Dictionary<V, LinkedListNode<LinkedListNode<V>>>();

        private readonly Dictionary<K, LinkedList<LinkedListNode<V>>> _snap =
            new Dictionary<K, LinkedList<LinkedListNode<V>>>();

        public Action<V> OnRemove { get; set; }
        private int _capacity;

        public LRUCache()
        {
            _capacity = DefaultCapacity;
        }

        public LRUCache(Action<V> onRemove, int capacity = DefaultCapacity)
        {
            OnRemove = onRemove;
            _capacity = capacity;
        }

        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                var shrink = _list.Count - _capacity;
                while (shrink > 0)
                {
                    shrink--;
                    var v = _list.First.Value;
                    _list.RemoveFirst();
                    var node = _vSnap[v];
                    _vSnap.Remove(v);
                    node.List.Remove(node);
                    OnRemove(v);
                }
            }
        }

        public int Count => _list.Count;

        public V Get(K k)
        {
            if (!_snap.TryGetValue(k, out var list)) return null;
            if (list.Count <= 0) return null;
            var node = list.Last;
            list.RemoveLast();
            node.Value.List.Remove(node.Value);
            _vSnap.Remove(node.Value.Value);
            return node.Value.Value;
        }

        public void Release(K k, V v)
        {
            if (!Contain(v))
            {
                _list.AddLast(v);

                var node = _list.Last;
                if (!_snap.TryGetValue(k, out var list2))
                {
                    list2 = new LinkedList<LinkedListNode<V>>();
                    _snap.Add(k, list2);
                }

                list2.AddLast(node);
                _vSnap.Add(v, list2.Last);

                if (Count > Capacity)
                {
                    Capacity = Capacity;
                }
            }
        }

        public bool Contain(V v)
        {
            return _vSnap.ContainsKey(v);
        }

        public void Clear()
        {
            var node = _list.First;
            while (node != null)
            {
                OnRemove.Invoke(node.Value);
                node = node.Next;
            }

            _list.Clear();
            _vSnap.Clear();
            _snap.Clear();
        }

        public void Dispose()
        {
            Clear();
            ObjectPool<LRUCache<K, V>>.Release(this);
        }
    }
}
