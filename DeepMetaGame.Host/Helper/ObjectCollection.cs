using DeepCore.Game3D.Host.Instance;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Helper
{

    public static class DoAndRemoveCollection
    {
        public static void UpdateAndRemove<T>(this SingleThreadCollectionPool pool, LinkedList<T> list, Predicate<T> test)
        {
            using (var removed = pool.AllocList<LinkedListNode<T>>())
            {
                for (LinkedListNode<T> it = list.Last; it != null; it = it.Previous)
                {
                    T t = it.Value;
                    if (test(t))
                    {
                        removed.Add(it);
                    }
                }
                if (removed.Count > 0)
                {
                    foreach (LinkedListNode<T> it in removed)
                    {
                        list.Remove(it);
                    }
                }
            }
        }

        public static void UpdateAndRemove<T>(this SingleThreadCollectionPool pool, ICollection<T> list, Predicate<T> test)
        {
            using (var removed = pool.AllocList<T>())
            {
                foreach (T t in list)
                {
                    if (test(t))
                    {
                        removed.Add(t);
                    }
                }
                if (removed.Count > 0)
                {
                    for (int i = removed.Count - 1; i >= 0; --i)
                    {
                        list.Remove(removed[i]);
                    }
                }
            }
        }

        public static void UpdateAndRemove<T>(this SingleThreadCollectionPool pool, IList<T> list, Predicate<T> test)
        {
            using (var removed = pool.AllocList<T>())
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    T t = list[i];
                    if (test(t))
                    {
                        removed.Add(t);
                    }
                }
                if (removed.Count > 0)
                {
                    for (int i = removed.Count - 1; i >= 0; --i)
                    {
                        list.Remove(removed[i]);
                    }
                }
            }
        }
    }
}
