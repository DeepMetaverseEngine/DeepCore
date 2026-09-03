using DeepCore;
using DeepMetaGame.Data.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DeepMetaGame.Data.Helper
{
    /// <summary>
    /// 基于权重的掉落列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeightDropList<T> where T : class
    {
        private double totalWeight;
        private List<Item> items = new();
        public int Count { get => items.Count; }
        public void Clear()
        {
            totalWeight = 0;
            items.Clear();
        }
        public void AddItem(T item, float weight)
        {
            if (weight > 0)
            {
                if (items.Count == 0)
                {
                    items.Add(new Item()
                    {
                        start = 0,
                        end = weight,
                        item = item,
                    });
                    totalWeight = weight;
                }
                else
                {
                    var last = items[items.Count - 1];
                    var add = new Item()
                    {
                        start = last.end,
                        end = last.end + weight,
                        item = item,
                    };
                    items.Add(add);
                    totalWeight = add.end;
                }
            }
        }

        public bool TryDropOnce(Random random, out T ret) => TryDropOnce(random, out ret, random, null);
        public bool TryDropOnce<ST>(Random random, out T ret, ST st, TryGetPredicate<ST, T> exclude) => try_drop_once(random, out ret, null, st, exclude);

        public int TryDropCount(Random random, int count, ICollection<T> outlist) => TryDropCount(random, count, outlist, random, null);
        public int TryDropCount<ST>(Random random, int count, ICollection<T> outlist, ST st, TryGetPredicate<ST, T> exclude)
        {
            int retCount = 0;
            for (int dropI = 0; dropI < count; dropI++)
            {
                if (try_drop_once<ST>(random, out var ret, outlist, st, exclude))
                {
                    outlist.Add(ret);
                    retCount++;
                }
                else
                {
                    break;
                }
            }
            return retCount;
        }

        private bool try_drop_once<ST>(Random random, out T ret, ICollection<T> excludeList, ST st, TryGetPredicate<ST, T> exclude = null)
        {
            var seed = random.NextDouble() * totalWeight;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (seed >= item.start && seed < item.end)
                {
                    // 判断是否在排除列表里
                    if (is_exclude(item.item, excludeList, st, exclude))
                    {
                        for (int j = 0; j < items.Count; j++)
                        {
                            int idx = CMath.CycNum(i, j, items.Count);
                            var nextItem = items[idx].item;
                            if (!is_exclude(nextItem, excludeList, st, exclude))
                            {
                                ret = nextItem;
                                return true;
                            }
                        }
                        ret = default(T);
                        return false;
                    }
                    else
                    {
                        ret = item.item;
                        return true;
                    }
                }
            }
            ret = default(T);
            return false;
        }
        private bool is_exclude<ST>(T item, ICollection<T> excludeList, ST st, TryGetPredicate<ST, T> exclude)
        {
            if (exclude != null && exclude(st, item)) return true;
            if (excludeList != null && excludeList.Contains(item)) return true;
            return false;
        }
        private struct Item
        {
            public double start;
            public double end;
            public T item;
        }
    }

}
