using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore
{
    public class DropList<T> : Disposable
    {
        struct Drop
        {
            public double start;
            public double length;
            public T item;
        }
        private double totalWeight = 0;
        private List<Drop> items = new();
        private List<Drop> some = new();
        public T this[int index] => items[index].item;
        public int Count => items.Count;
        protected override void Disposing()
        {
            Clear();
        }
        public void Clear()
        {
            totalWeight = 0;
            some.Clear();
            items.Clear();
            items.TrimExcess();
        }
        public void TrimExcess()
        {
            items.TrimExcess();
        }
        public void Add(T item, float weight)
        {
            items.Add(new Drop() { item = item, start = totalWeight, length = weight, });
            totalWeight += weight;
        }
        private void Sort(List<Drop> some, out double totalWeight)
        {
            some.Clear();
            totalWeight = 0;
            foreach (var d in items)
            {
                some.Add(new Drop() { item = d.item, start = totalWeight, length = d.length, });
                totalWeight += d.length;
            }
        }
        public bool TryDropOnce(Random random, out T item)
        {
            var rd = random.NextDouble() * totalWeight;
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i];
                if (rd >= d.start && rd <= d.start + d.length)
                {
                    item = d.item;
                    return true;
                }
            }
            item = default(T);
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="onceCount">一次几个</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public int DropOnce(Random random, int onceCount, List<T> result)
        {
            onceCount = Math.Min(items.Count, onceCount);
            if (onceCount >= items.Count)
            {
                foreach (var item in items)
                {
                    result.Add(item.item);
                }
                return onceCount;
            }
            else if (onceCount == 1)
            {
                if (TryDropOnce(random, out var drop))
                {
                    result.Add(drop);
                    return onceCount;
                }
                return 0;
            }
            else
            {
                int total = 0;
                try
                {
                    while (onceCount > 0)
                    {
                        Sort(some, out var totalWeight);
                        var rd = random.NextDouble() * totalWeight;
                        for (int i = 0; i < some.Count; i++)
                        {
                            var d = items[i];
                            if (rd >= d.start && rd <= d.start + d.length)
                            {
                                result.Add(d.item);
                                total++;
                                onceCount--;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    some.Clear();
                }
                return total;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="onceCount">一次几个</param>
        /// <param name="dropCount">调用次数</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public int DropSome(Random random, int onceCount, int dropCount, List<T> result)
        {
            int total = 0;
            for (int i = 0; i < dropCount; i++)
            {
                total += DropOnce(random, onceCount, result);
            }
            return total;
        }
    }

}
