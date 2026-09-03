using System;
using System.Collections.Generic;

namespace DeepCore.Meta.Layout
{

    public class MetaObjectMap<K, V> : MetaObjectContainer<V> where V : MetaObject
    {
        protected HashMap<K, V> _childs = new HashMap<K, V>();

        public override int NumChildren => _childs.Count;
        public override IEnumerable<MetaObject> Children => _childs.Values;

        protected override void CollectionClearChildren()
        {
            _childs.Clear();
        }
        protected override bool CollectionRemoveChild(MetaObject c)
        {
            foreach (var e in _childs)
            {
                if (e.Value == c)
                {
                    return _childs.Remove(e.Key);
                }
            }
            return false;
        }

        public bool AddChild(K key, V value)
        {
            return InternalAddChild(value, c =>
            {
                if (!_childs.ContainsKey(key))
                {
                    _childs.Add(key, value);
                    return true;
                }
                return false;
            });
        }
        public bool ContainsChildKey(K key)
        {
            return _childs.ContainsKey(key);
        }
        public bool TryRemoveChild(K key, out V value, bool disposeChild = false)
        {
            if (_childs.TryRemove(key, out value))
            {
                InternalRemoveChild(value, c => true, disposeChild);
                return true;
            }
            return false;
        }
        public bool TryGetOrCreate(K key, out V value, Func<K, V> create)
        {
            if (_childs.TryGetValue(key, out value))
            {
                return true;
            }
            value = create(key);
            InternalAddChild(value, c =>
            {
                _childs.Add(key, c as V);
                return true;
            });
            return false;
        }
        public bool TryCreateOrGet(K key, out V value, Func<K, V> create)
        {
            if (_childs.TryGetValue(key, out value))
            {
                return false;
            }
            value = create(key);
            InternalAddChild(value, c =>
            {
                _childs.Add(key, c as V);
                return true;
            });
            return true;
        }
        public bool TryGetChild(K key, out V value)
        {
            return _childs.TryGetValue(key, out value);
        }

        public V GetChild(K key)
        {
            return _childs.Get(key);
        }
        public V PutChild(K k, V v, bool disposeChild = true)
        {
            if (_childs.TryGetValue(k, out var old))
            {
                if (v != old)
                {
                    InternalRemoveChild(old, c => _childs.Remove(k), disposeChild);
                }
            }
            else
            {
                AddChild(k, v);
            }
            return old;
        }
    }

}
