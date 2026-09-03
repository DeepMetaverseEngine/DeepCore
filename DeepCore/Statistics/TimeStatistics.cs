using DeepCore.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.Statistics
{
    public class TimeStatisticsRecoder
    {
        public enum SortField
        {
            NAME, COUNT, MIN, MAX, AVG,
            rNAME, rCOUNT, rMIN, rMAX, rAVG,
        }
        private static List<TimeStatisticsRecoder> s_allocs = new List<TimeStatisticsRecoder>();
        public string Name { get; private set; }
        public TimeStatisticsRecoder(string name)
        {
            this.Name = name;
            s_allocs.Add(this);
        }
        public static void PrintAllStatus(TextWriter output, SortField sort = SortField.NAME, string prefix = "    ", int namePlaceHolder = 30, int totalPlaceHolder = 100)
        {
            if (Enable)
            {
                foreach (var st in s_allocs)
                {
                    st.PrintStatus(output, sort, prefix, namePlaceHolder, totalPlaceHolder);
                }
            }
        }
        public static bool Enable { get; set; }
#if RELEASE
= false;
#else
= true;
#endif
        //--------------------------------------------------------------------------------------------------------
        private System.Collections.Concurrent.ConcurrentDictionary<string, MessageInfo> statistics =
            new System.Collections.Concurrent.ConcurrentDictionary<string, MessageInfo>();
        //--------------------------------------------------------------------------------------------------------
        private class MessageInfo
        {
            private readonly string id;
            private int count = 0;
            private float avgUseTime = 0;
            private float maxUseTime = 0;
            private float minUseTime = 0;
            private double totalUseTime = 0;
            public string ID { get => id; }
            public int Count { get { lock (this) { return count; } } }
            public float AvgUseTime { get { lock (this) { return avgUseTime; } } }
            public float MaxUseTime { get { lock (this) { return maxUseTime; } } }
            public float MinUseTime { get { lock (this) { return minUseTime; } } }
            internal MessageInfo(string id)
            {
                this.id = id;
            }
            internal void LogTime(double used)
            {
                lock (this)
                {
                    this.count++;
                    this.totalUseTime += used;
                    this.maxUseTime = (float)Math.Max(used, this.maxUseTime);
                    this.minUseTime = (float)Math.Min(used, this.minUseTime);
                    this.avgUseTime = (float)(totalUseTime / count);
                }
            }
            public string GetStatus()
            {
                lock (this)
                {
                    var rcount = count.ToString();
                    var min_t = minUseTime.ToString();
                    var max_t = maxUseTime.ToString();
                    var avg_t = avgUseTime.ToString();
                    var ret = string.Format("COUNT={0} MIN={1} MAX={2} AVG={3}",
                    CUtils.FillPlaceHolder(rcount, 8, ' ', 0),
                    CUtils.FillPlaceHolder(min_t, 6, ' ', 0),
                    CUtils.FillPlaceHolder(max_t, 6, ' ', 0),
                    CUtils.FillPlaceHolder(avg_t, 6, ' ', 0));
                    return ret;
                }
            }
        }
        public void PrintStatus(TextWriter output, SortField sort = SortField.MAX, string prefix = "    ", int namePlaceHolder = 30, int totalPlaceHolder = 100)
        {
            var list = new List<MessageInfo>(statistics.Values);
            {
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintTitle(Name, "Total=" + list.Count, prefix, namePlaceHolder);
                output.PrintLineSeparator(totalPlaceHolder);
                switch (sort)
                {
                    case SortField.NAME: list.Sort((a, b) => a.ID.CompareTo(b.ID)); break;
                    case SortField.COUNT: list.Sort((a, b) => a.Count - b.Count); break;
                    case SortField.MIN: list.Sort((a, b) => (int)(a.MinUseTime - b.MinUseTime)); break;
                    case SortField.MAX: list.Sort((a, b) => (int)(a.MaxUseTime - b.MaxUseTime)); break;
                    case SortField.AVG: list.Sort((a, b) => (int)(a.AvgUseTime - b.AvgUseTime)); break;

                    case SortField.rNAME: list.Sort((b, a) => a.ID.CompareTo(b.ID)); break;
                    case SortField.rCOUNT: list.Sort((b, a) => a.Count - b.Count); break;
                    case SortField.rMIN: list.Sort((b, a) => (int)(a.MinUseTime - b.MinUseTime)); break;
                    case SortField.rMAX: list.Sort((b, a) => (int)(a.MaxUseTime - b.MaxUseTime)); break;
                    case SortField.rAVG: list.Sort((b, a) => (int)(a.AvgUseTime - b.AvgUseTime)); break;
                }
                foreach (var e in list)
                {
                    output.PrintLine(e.GetStatus(), e.ID, prefix, namePlaceHolder);
                }
                output.PrintLineSeparator(totalPlaceHolder);
            }
        }
        //--------------------------------------------------------------------------------------------------------
        public void LogTime(string msgType, double useTime)
        {
            if (Enable)
            {
                statistics.GetOrAdd(msgType, t => new MessageInfo(msgType)).LogTime(useTime);
            }
        }
    }


}
