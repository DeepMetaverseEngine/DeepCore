using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DeepCore
{
    public static class CollectionsExt
    {

        public static object First(this IEnumerable list)
        {
            var it = list.GetEnumerator();
            if (it.MoveNext()) { return it.Current; }
            return null;
        }
        public static T First<T>(this IEnumerable<T> list)
        {
            var it = list.GetEnumerator();
            if (it.MoveNext()) { return it.Current; }
            return default(T);
        }

        public static bool IsNullOrEmpty(this ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }
        public static bool IsNullOrEmpty(this Array collection)
        {
            return collection == null || collection.Length == 0;
        }
        public static bool IsNotEmpty(this ICollection collection)
        {
            return collection != null && collection.Count > 0;
        }
        public static bool IsNotEmpty(this Array src)
        {
            return src != null && src.Length > 0;
        }
        public static T Remove<T>(this IList<T> collection, int index)
        {
            if (collection == null) return default(T);
            var ret = collection[index];
            collection.RemoveAt(index);
            return ret;
        }
        public static bool TryRemoveAt<T>(this IList<T> collection, int index, out T ret)
        {
            ret = default(T);
            if (collection == null || index < 0 || index >= collection.Count) return false;
            ret = collection[index];
            collection.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 遍历过程中，可对原集合写入操作
        /// </summary>
        public static void WritableForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            var temp = new List<T>(list);
            {
                foreach (var o in list) { action(o); }
            }
        }
        /// <summary>
        /// 遍历过程中，可对原集合写入操作
        /// </summary>
        public static T WritableForEach<T>(this IEnumerable<T> list, BreakPredicate<T> action)
        {
            var temp = new List<T>(list);
            {
                foreach (var o in temp) { if (action(o)) { return o; } }
            }
            return default(T);
        }

        //--------------------------------------------------------------------------------------------------------------
        #region Queue

        public static bool TryDequeue<T>(this Queue<T> queue, out T item)
        {
            if (queue.Count > 0)
            {
                item = queue.Dequeue();
                return true;
            }
            item = default(T);
            return false;
        }
        public static bool SynchronizedTryDequeue<T>(this Queue<T> queue, out T item)
        {
            lock (queue) return TryDequeue<T>(queue, out item);
        }
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            if (items is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    queue.Enqueue(list[i]);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    queue.Enqueue(item);
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region Stack

#if NETSTANDARD
        public static void Clear<T>(this Stack<T> stack)
        {
            while (stack.Count > 0)
            {
                stack.Pop();
            }
        }
#endif
        public static bool TryPop<T>(this Stack<T> stack, out T item)
        {
            if (stack.Count > 0)
            {
                item = stack.Pop();
                return true;
            }
            item = default(T);
            return false;
        }
        public static bool SynchronizedTryPop<T>(this Stack<T> stack, out T item)
        {
            lock (stack) return TryPop<T>(stack, out item);
        }
        public static bool TryPeek<T>(this Stack<T> stack, out T item)
        {
            if (stack.Count > 0)
            {
                item = stack.Peek();
                return true;
            }
            item = default(T);
            return false;
        }
        public static bool SynchronizedTryPeek<T>(this Stack<T> stack, out T item)
        {
            lock (stack) return TryPeek<T>(stack, out item);
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region LinkedList

        public static bool TryPopFirst<T>(this LinkedList<T> list, out T value)
        {
            var first = list.First;
            if (first != null)
            {
                value = first.Value;
                list.RemoveFirst();
                return true;
            }
            value = default(T);
            return false;
        }
        public static bool TryPopLast<T>(this LinkedList<T> list, out T value)
        {
            var last = list.Last;
            if (last != null)
            {
                value = last.Value;
                list.RemoveLast();
                return true;
            }
            value = default(T);
            return false;
        }

        public static bool TryPeekFirst<T>(this LinkedList<T> list, out T value)
        {
            var first = list.First;
            if (first != null)
            {
                value = first.Value;
                return true;
            }
            value = default(T);
            return false;
        }
        public static bool TryPeekLast<T>(this LinkedList<T> list, out T value)
        {
            var last = list.Last;
            if (last != null)
            {
                value = last.Value;
                return true;
            }
            value = default(T);
            return false;
        }

        public static void SortedInsert<T>(this LinkedList<T> list, T value) where T : IComparable<T>
        {
            if (list.First == null || value.CompareTo(list.First.Value) <= 0)
            {
                list.AddFirst(value);
            }
            else if (list.Last != null && value.CompareTo(list.Last.Value) >= 0)
            {
                list.AddLast(value);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && next.Value.CompareTo(value) < 0)
                {
                    node = next;
                }

                list.AddAfter(node, value);
            }
        }

        public static void SortedInsert<T>(this LinkedList<T> list, LinkedListNode<T> insertNode) where T : IComparable<T>
        {
            if (list.First == null || insertNode.Value.CompareTo(list.First.Value) <= 0)
            {
                list.AddFirst(insertNode);
            }
            else if (list.Last != null && insertNode.Value.CompareTo(list.Last.Value) >= 0)
            {
                list.AddLast(insertNode);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && next.Value.CompareTo(insertNode.Value) < 0)
                {
                    node = next;
                }

                list.AddAfter(node, insertNode);
            }
        }
        public static void SortedInsert<T>(this LinkedList<T> list, T value, Comparison<T> compareTo)
        {
            if (list.First == null || compareTo(value, list.First.Value) <= 0)
            {
                list.AddFirst(value);
            }
            else if (list.Last != null && compareTo(value, list.Last.Value) >= 0)
            {
                list.AddLast(value);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && compareTo(next.Value, value) < 0)
                {
                    node = next;
                }

                list.AddAfter(node, value);
            }
        }
        public static void SortedInsert<T>(this LinkedList<T> list, LinkedListNode<T> insertNode, Comparison<T> compareTo)
        {
            if (list.First == null || compareTo(insertNode.Value, list.First.Value) <= 0)
            {
                list.AddFirst(insertNode);
            }
            else if (list.Last != null && compareTo(insertNode.Value, list.Last.Value) >= 0)
            {
                list.AddLast(insertNode);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && compareTo(next.Value, insertNode.Value) < 0)
                {
                    node = next;
                }

                list.AddAfter(node, insertNode);
            }
        }
        public static void Sort<T>(this LinkedList<T> list, Comparison<T> compareTo)
        {
            LinkedListNode<T> cNode;
            LinkedListNode<T> pNode;
            LinkedListNode<T> tNode;
            cNode = list.First;
            int result;
            bool IsSwitch;
            while (cNode != list.Last)
            {
                tNode = cNode;
                pNode = cNode;
                IsSwitch = false;
                do
                {
                    pNode = pNode.Next;

                    result = compareTo(tNode.Value, pNode.Value);
                    if (result > 0)
                    {
                        tNode = pNode;
                        IsSwitch = true;
                    }

                } while (pNode != list.Last);
                if (IsSwitch)
                {
                    list.Remove(tNode);
                    list.AddBefore(cNode, tNode);
                }
                cNode = tNode.Next;
            }
        }

        public struct LinkedListNodeEnumerator<T> : IEnumerator<LinkedListNode<T>>
        {
            private LinkedList<T> list;
            private LinkedListNode<T> node;
            public LinkedListNodeEnumerator(LinkedList<T> list)
            {
                this.list = list;
                this.node = list.First;
            }
            public LinkedListNode<T> Current => node;
            object IEnumerator.Current => this.Current;
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (node?.Next != null)
                {
                    node = node.Next;
                    return true;
                }
                return false;
            }
            public void Reset()
            {
                node = list.First;
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region IList

        public static ArrayList<V> ToGenericList<V>(this IList src)
        {
            var ret = new ArrayList<V>();
            foreach (var e in src) { ret.Add((V)e); }
            return ret;
        }
        public static bool TryGetOrCreateListData<ST, T>(this IList list, int index, out T data, ST state, Func<ST, T> create)
        {
            while (list.Count <= index)
            {
                list.Add(default);
            }
            if (list[index] is T member)
            {
                data = member;
                return true;
            }
            list[index] = data = create(state);
            return false;
        }

        public static bool Swap(this IList src, object a, object b)
        {
            var ia = src.IndexOf(a);
            var ib = src.IndexOf(b);
            if (ia >= 0 && ib >= 0)
            {
                src[ia] = b;
                src[ib] = a;
                return true;
            }
            return false;
        }
        public static bool Swap<T>(this IList<T> src, T a, T b)
        {
            var ia = src.IndexOf(a);
            var ib = src.IndexOf(b);
            if (ia >= 0 && ib >= 0)
            {
                src[ia] = b;
                src[ib] = a;
                return true;
            }
            return false;
        }
        public static bool Swap<T>(this T[] src, T a, T b)
        {
            var ia = Array.IndexOf(src, a);
            var ib = Array.IndexOf(src, b);
            if (ia >= 0 && ib >= 0)
            {
                src[ia] = b;
                src[ib] = a;
                return true;
            }
            return false;
        }

        public static bool TryIndexOf<T>(this IReadOnlyList<T> list, T exist, int start, out int index)
        {
            for (int i = start; i < list.Count; i++)
            {
                var e = list[i];
                if (Object.Equals(e, exist))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public static bool TryGet<T>(this T[] src, int index, out T value)
        {
            if (index >= 0 && index < src.Length)
            {
                value = src[index];
                return true;
            }
            value = default(T);
            return false;
        }

        public static bool TryGet<T>(this IReadOnlyList<T> src, int index, out T value)
        {
            if (index >= 0 && index < src.Count)
            {
                value = src[index];
                return true;
            }
            value = default(T);
            return false;
        }



        public static bool TryIndexOf<T>(this IReadOnlyList<T> list, T exist, out int index)
        {
            return TryIndexOf<T>(list, exist, 0, out index);
        }
        public static bool TryLastIndexOf<T>(this IReadOnlyList<T> list, T exist, int start, out int index)
        {
            for (int i = start; i >= 0; --i)
            {
                var e = list[i];
                if (Object.Equals(e, exist))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        public static bool TryLastIndexOf<T>(this IReadOnlyList<T> list, T exist, out int index)
        {
            return TryLastIndexOf<T>(list, exist, list.Count - 1, out index);
        }


        #endregion
        //--------------------------------------------------------------------------------------------------------------
        #region IDictionary

        public static void ForEachDictionary(this IDictionary map, Action<DictionaryEntry> action)
        {
            IDictionaryEnumerator map_e = map.GetEnumerator();
            int count = 0;
            while (map_e.MoveNext())
            {
                action(map_e.Entry);
                count++;
            }
        }
        public static void ForEachDictionary<ST>(this IDictionary map, ST st, Action<ST, DictionaryEntry> action)
        {
            IDictionaryEnumerator map_e = map.GetEnumerator();
            int count = 0;
            while (map_e.MoveNext())
            {
                action(st, map_e.Entry);
                count++;
            }
        }

        /// <summary>
        /// 遍历过程中，可对原集合写入操作
        /// </summary>
        public static void WritableForEachDictionary(this IDictionary map, Action<DictionaryEntry> action)
        {
            var temp = new List<DictionaryEntry>();
            {
                IDictionaryEnumerator map_e = map.GetEnumerator();
                while (map_e.MoveNext())
                {
                    temp.Add(map_e.Entry);
                }
                foreach (var o in temp) { action(o); }
            }
        }

        public static HashMap<K, V> ToGenericMap<K, V>(this IDictionary src)
        {
            var ret = new HashMap<K, V>();
            src.ForEachDictionary(ret, static (ret,e) =>
            {
                ret.Add((K)e.Key, (V)e.Value);
            });
            return ret;
        }

        public static void PutAll<K, V>(this IDictionary<K, V> src, IDictionary<K, V> map)
        {
            foreach (KeyValuePair<K, V> e in map)
            {
                src[e.Key] = e.Value;
            }
        }
        public static void AddAll<K, V>(this IDictionary<K, V> src, IDictionary<K, V> map)
        {
            foreach (KeyValuePair<K, V> e in map)
            {
                src.Add(e.Key, e.Value);
            }
        }

        public static void AddRange<V>(this IList<V> src, IList<V> list)
        {
            foreach (var e in list)
            {
                src.Add(e);
            }
        }
        public static bool TryGetOrCreate<K, V>(this ConcurrentDictionary<K, V> map, K key, out V value, Func<K, V> create)
        {
            bool exist = true;
            value = map.GetOrAdd(key, (k) =>
            {
                exist = false;
                return create(k);
            });
            return exist;
        }
        public static bool TryAdd<K, V>(this ConcurrentDictionary<K, V> map, K key, out V value, Func<K, V> create)
        {
            bool exist = true;
            value = map.GetOrAdd(key, (k) =>
            {
                exist = false;
                return create(k);
            });
            return !exist;
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------

    }

}