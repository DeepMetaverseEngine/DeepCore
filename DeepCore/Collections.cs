using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeepCore
{
    public interface IHashMap<K, V> : IDictionary<K, V>
    {
        V Get(K key);

        void Put(K key, V val);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>added</returns>
        bool TryAddOrUpdate(K key, V val);
        bool TryAdd(K key, V val);
        bool TryGetOrCreate(K key, out V value, Func<K, V> create);
        V RemoveByKey(K key);
        void PutAll(IReadOnlyDictionary<K, V> map);
    }
    public interface IArrayList<T> : IList<T>
    {
        void AddRange(IEnumerable<T> collection);
    }

    public class HashMap<K, V> : System.Collections.Generic.Dictionary<K, V>, IHashMap<K, V>
    {
        public HashMap() { }
        public HashMap(int capacity) : base(capacity) { }
        public HashMap(IEqualityComparer<K> comparer) : base(comparer) { }
        public HashMap(int capacity, IEqualityComparer<K> comparer) : base(capacity, comparer) { }
        public HashMap(IDictionary<K, V> map) : base(map) { }
        public HashMap(IDictionary<K, V> map, IEqualityComparer<K> comparer) : base(map, comparer) { }
        public HashMap(IEnumerable<KeyValuePair<K, V>> map) : base(map) { }
        public HashMap(IEnumerable<KeyValuePair<K, V>> map, IEqualityComparer<K> comparer) : base(map, comparer) { }

        public V Get(K key)
        {
            V ret;
            if (base.TryGetValue(key, out ret))
            {
                return ret;
            }
            return default(V);
        }
        public void Put(K key, V val)
        {
            this[key] = val;
        }

        //         new public bool TryAdd(K key, V val)
        //         {
        //             if (!this.ContainsKey(key))
        //             {
        //                 this.Add(key, val);
        //                 return true;
        //             }
        //             return false;
        //         }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>true if not contained</returns>
        public bool TryAddOrUpdate(K key, V val)
        {
            if (!this.ContainsKey(key))
            {
                this.Add(key, val);
                return true;
            }
            this[key] = val;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ret"></param>
        /// <param name="create"></param>
        /// <returns>true if contained</returns>
        public bool TryGetOrCreate(K key, out V ret, Func<K, V> create)
        {
            if (base.TryGetValue(key, out ret))
            {
                return true;
            }
            ret = create(key);
            base.Add(key, ret);
            return false;
        }
        public bool TryGetOrCreate<ST>(K key, out V ret, ST st, Func<ST, K, V> create)
        {
            if (base.TryGetValue(key, out ret))
            {
                return true;
            }
            ret = create(st, key);
            base.Add(key, ret);
            return false;
        }
        public bool TryGetOrNew<VV>(K key, out V ret) where VV : V, new()
        {
            if (base.TryGetValue(key, out ret))
            {
                return true;
            }
            ret = new VV();
            base.Add(key, ret);
            return false;
        }
        public V RemoveByKey(K key)
        {
            V ret;
            if (base.TryGetValue(key, out ret))
            {
                base.Remove(key);
            }
            return ret;
        }
        public bool TryRemove(K key, out V value)
        {
            if (base.TryGetValue(key, out value))
            {
                base.Remove(key);
                return true;
            }
            return false;
        }

        public void PutAll(IReadOnlyDictionary<K, V> map)
        {
            foreach (KeyValuePair<K, V> e in map)
            {
                Put(e.Key, e.Value);
            }
        }
        public void AddAll(IReadOnlyDictionary<K, V> map)
        {
            foreach (KeyValuePair<K, V> e in map)
            {
                Add(e.Key, e.Value);
            }
        }
    }

    public class ArrayList<T> : System.Collections.Generic.List<T>, IArrayList<T>, IDisposable
    {
        public ArrayList() { }
        public ArrayList(IEnumerable<T> collection) : base(collection) { }
        public ArrayList(int capacity) : base(capacity) { }

        //         public static implicit operator ArrayList<T>(System.Collections.Generic.List<T> list)
        //         {
        //             return new ArrayList<T>(list);
        //         }
        public void Dispose()
        {
            this.Clear();
        }

        //---------------------------------------------------------------------------------------------------------
        #region Add
        public T AddComponent(T comp)
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return default(T);
        }
        public C AddComponent<C>(C comp) where C : T
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return default(C);
        }
        public T AddComponent(Type t)
        {
            if (TryAddComponent(t, out var comp))
            {
                return comp;
            }
            return default(T);
        }
        public bool TryAddComponent(T comp)
        {
            this.Add(comp);
            return true;
        }
        public bool TryAddComponent(Type ctype, out T comp)
        {
            comp = (T)ReflectionUtil.CreateInstance(ctype);
            this.Add(comp);
            return true;
        }
        public C AddComponentAs<C>() where C : T
        {
            if (TryAddComponentAs<C>(out var comp))
            {
                return comp;
            }
            return default(C);
        }
        public C GetOrAddComponentAs<C>() where C : T
        {
            if (TryGetOrCreateComponentAs<C>(out var comp))
            {
            }
            return comp;
        }
        public bool TryAddComponentAs<C>(out C comp) where C : T
        {
            var ctype = typeof(C);
            comp = (C)ReflectionUtil.CreateInstance(ctype);
            this.Add(comp);
            return true;
        }
        public bool TryAddComponentAs<C>(out C comp, Func<C> create) where C : T
        {
            comp = create();
            this.Add(comp);
            return true;
        }
        public bool AddOrUpdate(T comp)
        {
            if (TryGetComponent(comp.GetType(), out T value, out var index))
            {
                this[index] = comp;
                return false;
            }
            else
            {
                this.Add(comp);
                return true;
            }
        }
        public bool AddOrUpdate(Type type, T comp)
        {
            if (TryGetComponent(type, out T value, out var index))
            {
                this[index] = comp;
                return false;
            }
            else
            {
                this.Add(comp);
                return true;
            }
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Get
        public bool TryGetComponent(Type ctype, out T value, out int index, bool inherit = true)
        {
            value = default(T);
            for (int i = 0; i < this.Count; i++)
            {
                var comp = this[i];
                if (comp?.GetType() == ctype)
                {
                    index = i;
                    value = (T)comp;
                    return true;
                }
            }
            if (inherit)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    var comp = this[i];
                    if (ctype.IsAssignableFrom(comp.GetType()))
                    {
                        index = i;
                        value = (T)comp;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        public bool TryGetComponents(Type ctype, IList<T> outlist, bool inherit = true)
        {
            var cout = outlist.Count;
            for (int i = 0; i < this.Count; i++)
            {
                var comp = this[i];
                var etype = comp?.GetType();
                if (etype != null)
                {
                    if (inherit)
                    {
                        if (ctype.IsAssignableFrom(etype)) outlist.Add((T)comp);
                    }
                    else
                    {
                        if (ctype == etype) outlist.Add((T)comp);
                    }
                }
            }
            return cout != outlist.Count;
        }
        public bool TryGetComponentAs<C>(out C value, bool inherit = true) where C : T
        {
            value = default(C);
            if (TryGetComponent(typeof(C), out var tvalue, out var index, inherit))
            {
                value = (C)tvalue;
                return true;
            }
            return false;
        }
        public bool TryGetComponentsAs<C>(IList<C> outlist, bool inherit = true) where C : T
        {
            var ctype = typeof(C);
            var cout = outlist.Count;
            for (int i = 0; i < this.Count; i++)
            {
                var comp = this[i];
                if (inherit)
                {
                    if (comp is C c) outlist.Add(c);
                }
                else
                {
                    if (comp?.GetType() == ctype) outlist.Add((C)comp);
                }
            }
            return cout != outlist.Count;
        }
        private bool TryGetOrCreateComponentAs<C>(out C comp) where C : T
        {
            if (TryGetComponentAs<C>(out comp, true) == false)
            {
                comp = (C)ReflectionUtil.CreateInstance(typeof(C));
                AddComponent(comp);
                return false;
            }
            return true;
        }
        private bool TryGetOrCreateComponentAs<C>(out C comp, Func<C> create) where C : T
        {
            if (TryGetComponentAs<C>(out comp, true) == false)
            {
                comp = create();
                AddComponent(comp);
                return false;
            }
            return true;
        }

        public T GetComponent(Type ctype, bool inherit = true)
        {
            if (TryGetComponent(ctype, out var tvalue, out var index, inherit))
            {
                return tvalue;
            }
            return default(T);
        }
        public C GetComponentAs<C>(bool inherit = true) where C : T
        {
            if (TryGetComponentAs<C>(out var tvalue, inherit))
            {
                return tvalue;
            }
            return default(C);
        }
        public C GetOrCreateComponentAs<C>() where C : T
        {
            if (TryGetComponentAs<C>(out var comp, true) == false)
            {
                comp = (C)ReflectionUtil.CreateInstance(typeof(C));
                AddComponent(comp);
                return comp;
            }
            return comp;
        }
        public C GetOrCreateComponentAs<C>(Func<C> create) where C : T
        {
            if (TryGetComponentAs<C>(out var comp, true) == false)
            {
                comp = create();
                AddComponent(comp);
                return comp;
            }
            return comp;
        }


        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region ForEach

        public bool ForEach(BreakPredicate<T> predicate)
        {
            foreach (var c in this)
            {
                if (predicate(c))
                {
                    return true;
                }
            }
            return false;
        }
        public void ForEachAs<C>(Action<C> action) where C : T
        {
            foreach (var c in this)
            {
                if (c is C cc) action(cc);
            }
        }
        public bool ForEachAs<C>(BreakPredicate<C> predicate) where C : T
        {
            foreach (var c in this)
            {
                if (c is C cc && predicate(cc))
                {
                    return true;
                }
            }
            return false;
        }
        public bool TryGet<ST, R>(in ST state, TryGetPredicateResult<ST, T, R> predicate, out R ret)
        {
            foreach (var c in this)
            {
                if (predicate(state, c, out ret))
                {
                    return true;
                }
            }
            ret = default(R);
            return false;
        }

        public bool TryGetAs<ST, C, R>(in ST state, TryGetPredicateResult<ST, C, R> predicate, out R ret) where C : T
        {
            foreach (var c in this)
            {
                if (c is C cc && predicate(state, cc, out ret))
                {
                    return true;
                }
            }
            ret = default(R);
            return false;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
    }
    public class RecycleLinkList<T> : IEnumerable<T>
    {
        private LinkedList<T> list = new LinkedList<T>();
        private Stack<LinkedListNode<T>> recycle = new Stack<LinkedListNode<T>>();

        public LinkedListNode<T> SortedInsert(T value, Comparison<T> compareTo)
        {
            if (list.First == null || compareTo(value, list.First.Value) <= 0)
            {
                return this.AddFirst(value);
            }
            else if (list.Last != null && compareTo(value, list.Last.Value) >= 0)
            {
                return this.AddLast(value);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && compareTo(next.Value, value) < 0)
                {
                    node = next;
                }
                return this.AddAfter(node, value);
            }
        }
        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            if (recycle.TryPop(out var reuse))
            {
                reuse.Value = value;
                list.AddAfter(node, reuse);
                return reuse;
            }
            else
            {
                return list.AddAfter(node, value);
            }
        }
        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            if (recycle.TryPop(out var reuse))
            {
                reuse.Value = value;
                list.AddBefore(node, reuse);
                return reuse;
            }
            else
            {
                return list.AddBefore(node, value);
            }
        }
        public LinkedListNode<T> AddFirst(T value)
        {
            if (recycle.TryPop(out var reuse))
            {
                reuse.Value = value;
                list.AddFirst(reuse);
                return reuse;
            }
            else
            {
                return list.AddFirst(value);
            }
        }
        public LinkedListNode<T> AddLast(T value)
        {
            if (recycle.TryPop(out var reuse))
            {
                reuse.Value = value;
                list.AddLast(reuse);
                return reuse;
            }
            else
            {
                return list.AddLast(value);
            }
        }
        /// <summary>
        /// 返回下一个节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns>返回下一个节点</returns>
        public LinkedListNode<T> Remove(LinkedListNode<T> node)
        {
            var ret = node.Next;
            list.Remove(node);
            node.Value = default;
            recycle.Push(node);
            return ret;
        }
        public void Clear()
        {
            list.Clear();
            recycle.Clear();
        }
        public LinkedListNode<T> Last => list.Last;
        public LinkedListNode<T> First => list.First;
        public int Count => list.Count;
        public LinkedListNode<T> Find(T value) => list.Find(value);
        public LinkedListNode<T> FindLast(T value) => list.FindLast(value);
        public bool Contains(T value) => list.Contains(value);
        public void CopyTo(T[] array, int index) => list.CopyTo(array, index);
        public LinkedList<T>.Enumerator GetEnumerator() => list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

#if false
    public abstract class FastLinkNode
    {
        public FastLinkNode Prev { get { return m_prev; } }
        public FastLinkNode Next { get { return m_next; } }

        // 当前所在的列表
        internal object m_curList;
        internal FastLinkNode m_prev = null;
        internal FastLinkNode m_next = null;
    }
    public class FastLinkList<T> : ICollection<T> where T : FastLinkNode
    {
        private FastLinkNode head = null;
        private FastLinkNode last = null;
        private int count = 0;

        public T Head { get { return (T)head; } }
        public T Last { get { return (T)last; } }
        public bool IsEmpty { get { return count == 0; } }
        public bool IsReadOnly { get { return false; } }
        public int Count { get { return count; } }

        public void Add(T node)
        {
            if (node.m_curList == null)
            {
                if (last == null)
                {
                    head = last = node;
                }
                else
                {
                    last.m_next = node;
                    node.m_prev = last;
                    last = node;
                }
                node.m_curList = this;
                count++;
            }
            else
            {
                throw new Exception("Node is already in a List !");
            }
        }

        public bool Remove(T node)
        {
            if (node.m_curList == this)
            {
                if (head == node)
                {
                    head = node.m_next;
                }
                if (last == node)
                {
                    last = node.m_prev;
                }
                if (node.m_next != null)
                {
                    node.m_next.m_prev = node.m_prev;
                }
                if (node.m_prev != null)
                {
                    node.m_prev.m_next = node.m_next;
                }
                node.m_next = null;
                node.m_prev = null;
                node.m_curList = null;
                count--;
                return true;
            }
            else
            {
                throw new Exception("Node is not contains in this list !");
            }
        }

        public void Clear()
        {
            if (count > 0)
            {
                for (FastLinkNode i = head; i != null; i = i.m_next)
                {
                    i.m_curList = null;
                }
                FastLinkNode p = head;
                FastLinkNode q = null;
                do
                {
                    q = p.m_next;
                    p.m_next = null;
                    p = q;
                }
                while (p != null);
                p = last;
                do
                {
                    q = p.m_prev;
                    p.m_prev = null;
                    p = q;
                }
                while (p != null);
                head = null;
                last = null;
                count = 0;
            }
        }

        public bool Contains(T item)
        {
            return item.m_curList == this;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (FastLinkNode i = head; i != null; i = i.m_next)
            {
                array[arrayIndex] = (T)i;
                arrayIndex++;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private FastLinkList<T> list;
            private T current;
            public Enumerator(FastLinkList<T> list)
            {
                this.list = list;
                this.current = null;
            }
            public T Current
            {
                get { return current; }
            }
            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public void Dispose()
            {
                current = null;
            }

            public bool MoveNext()
            {
                if (current == null)
                {
                    this.current = (T)list.Head;
                }
                else
                {
                    this.current = (T)current.Next;
                }
                return current != null;
            }

            public void Reset()
            {
                this.current = null;
            }
        }
    }

#endif

    public class ListDictionary<K, V> : IHashMap<K, V>, IReadOnlyDictionary<K, V>
    {
        private List<K> keys = new List<K>();
        private HashMap<K, V> map = new HashMap<K, V>();

        public ListDictionary()
        {
            this.keys = new List<K>();
            this.map = new HashMap<K, V>();
        }
        public ListDictionary(int capacity)
        {
            this.keys = new List<K>(capacity);
            this.map = new HashMap<K, V>(capacity);
        }
        public ListDictionary(IDictionary<K, V> map)
        {
            this.map = new HashMap<K, V>(map);
            foreach (var kv in map.Keys) { keys.Add(kv); }
        }

        public ICollection<K> Keys => keys;

        public int Count => ((ICollection<KeyValuePair<K, V>>)map).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<K, V>>)map).IsReadOnly;

        public void Sort(IComparer<K> comparer)
        {
            keys.Sort(comparer);
        }
        public void Sort(Comparison<K> comparison)
        {
            keys.Sort(comparison);
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return ((ICollection<KeyValuePair<K, V>>)map).Contains(item);
        }

        public bool ContainsKey(K key)
        {
            return ((IDictionary<K, V>)map).ContainsKey(key);
        }

        public V Get(K key)
        {
            return ((IHashMap<K, V>)map).Get(key);
        }
        public bool TryGetValue(K key, out V value)
        {
            return map.TryGetValue(key, out value);
        }

        public V this[K key]
        {
            get => ((IDictionary<K, V>)map)[key];
            set => ((IDictionary<K, V>)map)[key] = value;
        }
        public ICollection<V> Values
        {
            get
            {
                var ret = new List<V>();
                foreach (var k in keys)
                {
                    ret.Add(this[k]);
                }
                return ret;
            }
        }
        public ICollection<V> HashValues { get => map.Values; }

        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => this.keys;

        IEnumerable<V> IReadOnlyDictionary<K, V>.Values => this.Values;

        public void Add(K key, V value)
        {
            ((IDictionary<K, V>)map).Add(key, value);
            keys.Add(key);
        }

        public void Add(KeyValuePair<K, V> item)
        {
            ((ICollection<KeyValuePair<K, V>>)map).Add(item);
            keys.Add(item.Key);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<K, V>>)map).Clear();
            keys.Clear();
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                array[arrayIndex + i] = new KeyValuePair<K, V>(keys[i], this[keys[i]]);
            }
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            var ret = new List<KeyValuePair<K, V>>();
            foreach (var k in keys)
            {
                ret.Add(new KeyValuePair<K, V>(k, this[k]));
            }
            return ret.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Put(K key, V val)
        {
            if (ContainsKey(key))
            {
                map.Put(key, val);
            }
            else
            {
                this.Add(key, val);
            }
        }

        public void PutAll(IReadOnlyDictionary<K, V> map)
        {
            foreach (var e in map)
            {
                this.Add(e.Key, e.Value);
            }
        }

        public bool Remove(K key)
        {
            if (map.Remove(key))
            {
                keys.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return this.Remove(item.Key);
        }

        public V RemoveByKey(K key)
        {
            if (map.TryGetValue(key, out var ret))
            {
                this.Remove(key);
            }
            return ret;
        }

        public bool TryAdd(K key, V val)
        {
            if (!this.ContainsKey(key))
            {
                this.Add(key, val);
                return true;
            }
            return false;
        }

        public bool TryAddOrUpdate(K key, V val)
        {
            if (!this.ContainsKey(key))
            {
                this.Add(key, val);
                return true;
            }
            this[key] = val;
            return false;
        }

        public bool TryGetOrCreate(K key, out V value, Func<K, V> create)
        {
            if (this.TryGetValue(key, out value))
            {
                return true;
            }
            value = create(key);
            this.Add(key, value);
            return false;
        }


    }

    /// <summary>
    /// 维持排序的字典
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class ValueSortedCollection<V> : ICollection<V>
    {
        private List<V> sort = new List<V>();
        private Comparison<V> comparer;
        private bool sortDirty = true;

        public ValueSortedCollection(Comparison<V> comparer)
        {
            this.comparer = comparer;
        }
        public int Compare(V a, V b)
        {
            return comparer(a, b);
        }
        public void MarkSort()
        {
            sortDirty = true;
        }
        private bool SortRefresh()
        {
            if (sortDirty)
            {
                sortDirty = false;
                sort.Sort(comparer);
                return true;
            }
            return false;
        }

        public int Count => sort.Count;
        public bool IsReadOnly => false;
        public bool Contains(V item)
        {
            return sort.Contains(item);
        }
        public void CopyTo(V[] array, int arrayIndex)
        {
            SortRefresh();
            sort.CopyTo(array, arrayIndex);
        }
        public IEnumerator<V> GetEnumerator()
        {
            SortRefresh();
            return ((ICollection<V>)sort).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            SortRefresh();
            return ((ICollection<V>)sort).GetEnumerator();
        }
        public void Add(V item)
        {
            sort.Add(item);
            MarkSort();
        }
        public bool Remove(V item)
        {
            var ret = sort.Remove(item);
            if (ret) { MarkSort(); }
            return ret;
        }
        public void Clear()
        {
            sort.Clear();
        }

    }


    /// <summary>
    /// 维持排序的字典
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class ValueSortedMap<K, V> : IHashMap<K, V>, IDictionary
    {
        private HashMap<K, V> map = new HashMap<K, V>();
        private List<KeyValuePair<K, V>> sort = new List<KeyValuePair<K, V>>();
        private Comparison<V> comparer;
        private bool sortDirty = true;

        public ValueSortedMap(Comparison<V> comparer)
        {
            this.comparer = comparer;
        }
        public int Compare(V a, V b)
        {
            return comparer(a, b);
        }
        public int Compare(KeyValuePair<K, V> a, KeyValuePair<K, V> b)
        {
            return Compare(a.Value, b.Value);
        }
        public void MarkSort()
        {
            sortDirty = true;
        }
        private bool SortRefresh()
        {
            if (sortDirty)
            {
                sortDirty = false;
                sort.Clear();
                sort.AddRange(map);
                sort.Sort(Compare);
                return true;
            }
            return false;
        }
        public void GetSortedList(IList<KeyValuePair<K, V>> list)
        {
            SortRefresh();
            list.AddRange(sort);
        }
        public IList<KeyValuePair<K, V>> GetSortedList()
        {
            SortRefresh();
            return sort.AsReadOnly();
        }
        public KeyValuePair<K, V>[] ToSortedArray()
        {
            SortRefresh();
            return sort.ToArray();
        }
        public void ForEachSorted<ST>(ST st, Action<ST, KeyValuePair<K, V>> action)
        {
            SortRefresh();
            var array = sort;
            for (int i = 0; i < array.Count; ++i)
            {
                action(st, array[i]);
            }
        }
        public void ForEachSortedReverse<ST>(ST st, Action<ST, KeyValuePair<K, V>> action)
        {
            SortRefresh();
            var array = sort;
            for (int i = array.Count - 1; i >= 0; --i)
            {
                action(st, array[i]);
            }
        }
        public bool TryGetFirst(out KeyValuePair<K, V> first)
        {
            return TryGetAt(0, out first);
        }
        public bool TryGetLast(out KeyValuePair<K, V> last)
        {
            return TryGetAt(sort.Count - 1, out last);
        }
        public bool TryGetAt(int index, out V value)
        {
            SortRefresh();
            if (index >= 0 && index < sort.Count)
            {
                value = sort[index].Value;
                return true;
            }
            value = default;
            return false;
        }
        public bool TryGetAt(int index, out KeyValuePair<K, V> value)
        {
            SortRefresh();
            if (index >= 0 && index < sort.Count)
            {
                value = sort[index];
                return true;
            }
            value = default;
            return false;
        }
        public V GetAt(int index)
        {
            SortRefresh();
            return sort[index].Value;
        }
        public KeyValuePair<K, V> First
        {
            get
            {
                SortRefresh();
                return (sort.Count > 0) ? sort[0] : default;
            }
        }
        public KeyValuePair<K, V> Last
        {
            get
            {
                SortRefresh();
                return (sort.Count > 0) ? sort[sort.Count - 1] : default;
            }
        }

        #region Write

        public bool TryPopFirst(out KeyValuePair<K, V> first)
        {
            return TryRemoveAt(0, out first);
        }
        public bool TryPopLast(out KeyValuePair<K, V> last)
        {
            return TryRemoveAt(sort.Count - 1, out last);
        }

        public bool TryRemoveAt(int index, out KeyValuePair<K, V> v)
        {
            SortRefresh();
            if (index >= 0 && index < sort.Count)
            {
                v = sort[index];
                sort.RemoveAt(index);
                map.Remove(v.Key);
                sortDirty = true;
                return true;
            }
            v = default;
            return false;
        }


        public V this[K key]
        {
            get => ((IHashMap<K, V>)map)[key];
            set
            {
                map[key] = value;
                sortDirty = true;
            }
        }
        public void Add(K key, V value)
        {
            ((IHashMap<K, V>)map).Add(key, value);
            sortDirty = true;
        }
        public void Add(KeyValuePair<K, V> item)
        {
            ((IHashMap<K, V>)map).Add(item);
            sortDirty = true;
        }

        public void Clear()
        {
            map.Clear();
            sort.Clear();
            sortDirty = false;
        }
        public void Put(K key, V val)
        {
            ((IHashMap<K, V>)map).Put(key, val);
            sortDirty = true;
        }
        public void PutAll(IReadOnlyDictionary<K, V> map)
        {
            ((IHashMap<K, V>)this.map).PutAll(map);
            sortDirty = true;
        }
        public bool Remove(K key)
        {
            if (((IHashMap<K, V>)map).Remove(key))
            {
                sortDirty = true;
                return true;
            }
            return false;
        }
        public bool Remove(KeyValuePair<K, V> item)
        {
            if (((IHashMap<K, V>)map).Remove(item))
            {
                sortDirty = true;
                return true;
            }
            return false;
        }
        public V RemoveByKey(K key)
        {
            sortDirty = true;
            return ((IHashMap<K, V>)map).RemoveByKey(key);
        }
        public bool TryRemove(K key, out V value)
        {
            if (TryGetValue(key, out value))
            {
                Remove(key);
                return true;
            }
            return false;
        }
        public bool TryAdd(K key, V val)
        {
            if (((IHashMap<K, V>)map).TryAdd(key, val))
            {
                sortDirty = true;
                return true;
            }
            return false;
        }
        public bool TryAddOrUpdate(K key, V val)
        {
            sortDirty = true;
            return ((IHashMap<K, V>)map).TryAddOrUpdate(key, val);
        }
        public bool TryGetOrCreate(K key, out V ret, Func<K, V> create)
        {
            if (map.TryGetValue(key, out ret))
            {
                return true;
            }
            ret = create(key);
            map.Add(key, ret);
            sortDirty = true;
            return false;
        }

        object IDictionary.this[object key]
        {
            get => ((IDictionary)map)[key];
            set
            {
                ((IDictionary)map)[key] = value;
                sortDirty = true;
            }
        }
        void IDictionary.Add(object key, object value)
        {
            ((IDictionary)map).Add(key, value);
            sortDirty = true;
        }
        void IDictionary.Remove(object key)
        {
            ((IDictionary)map).Remove(key);
            sortDirty = true;
        }

        #endregion

        #region Read
        public ICollection<K> Keys => ((IDictionary<K, V>)map).Keys;
        public ICollection<V> Values => ((IDictionary<K, V>)map).Values;
        ICollection IDictionary.Keys => ((IDictionary)map).Keys;
        ICollection IDictionary.Values => ((IDictionary)map).Values;
        public int Count => ((IDictionary)map).Count;
        public bool IsReadOnly => ((IDictionary)map).IsReadOnly;
        public bool IsFixedSize => ((IDictionary)map).IsFixedSize;
        public bool IsSynchronized => ((IDictionary)map).IsSynchronized;
        public object SyncRoot => ((IDictionary)map).SyncRoot;
        public bool Contains(KeyValuePair<K, V> item)
        {
            return ((IHashMap<K, V>)map).Contains(item);
        }
        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)map).Contains(key);
        }
        public bool ContainsKey(K key)
        {
            return ((IHashMap<K, V>)map).ContainsKey(key);
        }
        public V Get(K key)
        {
            return ((IHashMap<K, V>)map).Get(key);
        }
        public bool TryGetValue(K key, out V value)
        {
            return ((IHashMap<K, V>)map).TryGetValue(key, out value);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            SortRefresh();
            sort.CopyTo(array, arrayIndex);
        }
        void ICollection.CopyTo(Array array, int index)
        {
            SortRefresh();
            ((IList)sort).CopyTo(array, index);
        }
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            SortRefresh();
            return (sort).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            SortRefresh();
            return ((IList)sort).GetEnumerator();
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            SortRefresh();
            return ((IDictionary)sort).GetEnumerator();
        }
        #endregion
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class List2D<T>
    {
        private T[,] matrix;
        private int xcount = 0, ycount = 0;
        public int XCount { get => xcount; set { SetSize(value, ycount); } }
        public int YCount { get => ycount; set { SetSize(xcount, value); } }
        public int TotalCount { get => xcount * ycount; }
        public int XCapacity { get => matrix.GetLength(0); }
        public int YCapacity { get => matrix.GetLength(1); }
        public T this[int x, int y]
        {
            get { return Get(x, y); }
            set { Set(x, y, value); }
        }
        public List2D(int capacity)
        {
            this.matrix = new T[capacity, capacity];
            this.xcount = 0;
            this.ycount = 0;
        }
        public List2D() : this(10) { }
        public T[,] ToArray()
        {
            var temp = new T[xcount, ycount];
            for (int x = 0; x < xcount; ++x)
            {
                for (int y = 0; y < ycount; ++y)
                {
                    temp[x, y] = matrix[x, y];
                }
            }
            return temp;
        }
        public void TrimExcess()
        {
            if (xcount < XCapacity || ycount < YCapacity)
            {
                var temp = new T[xcount, ycount];
                for (int x = 0; x < xcount; ++x)
                {
                    for (int y = 0; y < ycount; ++y)
                    {
                        temp[x, y] = matrix[x, y];
                    }
                }
                matrix = temp;
            }
        }
        public void SetSize(int w, int h)
        {
            if (w > XCapacity && h > YCapacity)
            {
                var temp = new T[(int)Math.Ceiling(w * 1.4), (int)Math.Ceiling(h * 1.4)];
                for (int x = 0; x < xcount; ++x)
                {
                    for (int y = 0; y < ycount; ++y)
                    {
                        temp[x, y] = matrix[x, y];
                    }
                }
                matrix = temp;
            }
            else if (w > XCapacity)
            {
                var temp = new T[(int)Math.Ceiling(w * 1.4), YCapacity];
                for (int x = 0; x < xcount; ++x)
                {
                    for (int y = 0; y < ycount; ++y)
                    {
                        temp[x, y] = matrix[x, y];
                    }
                }
                matrix = temp;
            }
            else if (h > YCapacity)
            {
                var temp = new T[XCapacity, (int)Math.Ceiling(h * 1.4)];
                for (int x = 0; x < xcount; ++x)
                {
                    for (int y = 0; y < ycount; ++y)
                    {
                        temp[x, y] = matrix[x, y];
                    }
                }
                matrix = temp;
            }
            xcount = w;
            ycount = h;
        }
        public void Expand(int w, int h)
        {
            SetSize(Math.Max(w, xcount), Math.Max(h, ycount));
        }
        public void AppendLeft()
        {
            var temp = new T[xcount + 1, ycount];
            for (int x = 0; x < xcount; ++x)
            {
                for (int y = 0; y < ycount; ++y)
                {
                    temp[x + 1, y] = matrix[x, y];
                }
            }
            xcount++;
            matrix = temp;
        }
        public void AppendTop()
        {
            var temp = new T[xcount, ycount + 1];
            for (int x = 0; x < xcount; ++x)
            {
                for (int y = 0; y < ycount; ++y)
                {
                    temp[x, y + 1] = matrix[x, y];
                }
            }
            ycount++;
            matrix = temp;
        }
        public void AppendRight()
        {
            SetSize(xcount + 1, ycount);
        }
        public void AppendBottom()
        {
            SetSize(xcount, ycount + 1);
        }

        public bool TryGet(int x, int y, out T v)
        {
            if (x >= 0 && x < xcount && y >= 0 && y < ycount)
            {
                v = matrix[x, y];
                return true;
            }
            v = default(T);
            return false;
        }

        public bool TrySet(int x, int y, T v)
        {
            if (x >= 0 && x < xcount && y >= 0 && y < ycount)
            {
                matrix[x, y] = v;
                return true;
            }
            return false;
        }

        public T Get(int x, int y)
        {
            if (TryGet(x, y, out var ret))
            {
                return ret;
            }
            throw new IndexOutOfRangeException($"x={x} y={y} xcount={xcount} ycount={ycount}");
        }

        public void Set(int x, int y, T v)
        {
            if (!TrySet(x, y, v))
            {
                throw new IndexOutOfRangeException($"x={x} y={y} xcount={xcount} ycount={ycount}");
            }
        }
        public void Add(int x, int y, T v)
        {
            Expand(x + 1, y + 1);
            if (x >= 0 && x < xcount && y >= 0 && y < ycount)
            {
                matrix[x, y] = v;
            }
            else
            {
                throw new IndexOutOfRangeException($"x={x} y={y} xcount={xcount} ycount={ycount}");
            }
        }
        public void ForEach(Action action)
        {
            for (int x = 0; x < xcount; ++x)
            {
                for (int y = 0; y < ycount; ++y)
                {
                    action(x, y, matrix[x, y]);
                }
            }
        }
        public bool ForEach(Predicate action)
        {
            for (int x = 0; x < xcount; ++x)
            {
                for (int y = 0; y < ycount; ++y)
                {
                    if (action(x, y, matrix[x, y]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public delegate void Action(int x, int y, T v);
        public delegate bool Predicate(int x, int y, T v);
    }

    //-----------------------------------------------------------------------------------------------------------------------------
    public static class CollectionsUtil
    {
        public static void RunAndClear<T>(this ICollection<T> list, Action<T> action)
        {
            foreach (var e in list) { action(e); }
            list.Clear();
        }
        public static V GetOrAdd<K, V>(this IHashMap<K, V> map, K key, Func<K, V> create)
        {
            map.TryGetOrCreate(key, out var ret, create);
            return ret;
        }
        public static V GetOrNew<K, V>(this IHashMap<K, V> map, K key) where V : new()
        {
            map.TryGetOrCreate(key, out var ret, static k => new V());
            return ret;
        }
        public static SortedDictionary<K, V> ToSorted<K, V>(this IDictionary<K, V> map)
        {
            return new SortedDictionary<K, V>(map);
        }
        public static SortedDictionary<K, V> ToSorted<K, V>(this IDictionary<K, V> map, IComparer<K> comparer)
        {
            return new SortedDictionary<K, V>(map, comparer);
        }
        public static SortedDictionary<K, V> ToSorted<K, V>(this IDictionary<K, V> map, Comparison<K> comparison)
        {
            return new SortedDictionary<K, V>(map, Comparer<K>.Create(comparison));
        }

        public static void ConvertPutAll<K, V>(this IDictionary<K, V> map, IDictionary add)
        {
            foreach (DictionaryEntry e in add)
            {
                map[((K)e.Key)] = ((V)e.Value);
            }
        }
        public static void ConvertAddAll<K, V>(this IDictionary<K, V> map, IDictionary add)
        {
            foreach (DictionaryEntry e in add)
            {
                map.Add(((K)e.Key), ((V)e.Value));
            }
        }
        public static void ConvertPutAll<K, V, K2, V2>(this IDictionary<K2, V2> map, IDictionary add, Func<K, K2> c1, Func<V, V2> c2)
        {
            foreach (DictionaryEntry e in add)
            {
                map[c1((K)e.Key)] = c2((V)e.Value);
            }
        }
        public static void ConvertAddAll<K, V, K2, V2>(this IDictionary<K2, V2> map, IDictionary add, Func<K, K2> c1, Func<V, V2> c2)
        {
            foreach (DictionaryEntry e in add)
            {
                map.Add(c1((K)e.Key), c2((V)e.Value));
            }
        }
        public static HashMap<K2, V2> ConvertAll<K, V, K2, V2>(this IDictionary<K, V> map, Func<K, K2> c1, Func<V, V2> c2)
        {
            var ret = new HashMap<K2, V2>();
            foreach (var e in map)
            {
                ret.Add(c1(e.Key), c2(e.Value));
            }
            return ret;
        }
    }
}
