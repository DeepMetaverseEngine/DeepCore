using DeepCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCrystal.ORM
{
    public static class ORMStatistics
    {
        //--------------------------------------------------------------------------------------------------------
        private static ConcurrentDictionary<Type, StatisticsMessageInfo> statistics = new ConcurrentDictionary<Type, StatisticsMessageInfo>();
        private static int totalSaveCount = 0;
        private static int totalLoadCount = 0;
        public static bool EnableStatistics { get; set; }
#if RELEASE
= false;
#else
= true;
#endif
        //--------------------------------------------------------------------------------------------------------
        internal class StatisticsMessageInfo
        {
            private readonly Type msgType;
            private int saveCount = 0;
            private int loadCount = 0;

            public Type MessageType { get => msgType; }
            public int SendCount { get { lock (this) { return saveCount; } } }
            public int RecvCount { get { lock (this) { return loadCount; } } }

            internal StatisticsMessageInfo(Type type)
            {
                this.msgType = type;
            }
            internal void LogSave()
            {
                lock (this)
                {
                    this.saveCount++;
                }
            }
            internal void LogLoad()
            {
                lock (this)
                {
                    this.loadCount++;
                }
            }
            public string GetStatus()
            {
                lock (this)
                {
                    var ret = string.Format("SAVE={0} LOAD={1}",
                    CUtils.FillPlaceHolder(saveCount.ToString(), 10, ' ', 0),
                    CUtils.FillPlaceHolder(loadCount.ToString(), 10, ' ', 0));
                    return ret;
                }
            }
        }
        public enum StatisticsSortField
        {
            NAME, SAVE, LOAD,
            rNAME, rSAVE, rLOAD,
        }
        public static void PrintStatisticsStatus(TextWriter output, StatisticsSortField sort = StatisticsSortField.SAVE, string prefix = "    ", int namePlaceHolder = 30, int totalPlaceHolder = 100)
        {
            var list = new List<StatisticsMessageInfo>(statistics.Values);
            {
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintTitle("ORM Statistics", "Total=" + list.Count, prefix, namePlaceHolder);
                output.PrintLineSeparator(totalPlaceHolder);
                switch (sort)
                {
                    case StatisticsSortField.NAME: list.Sort((a, b) => a.MessageType.FullName.CompareTo(b.MessageType.FullName)); break;
                    case StatisticsSortField.SAVE: list.Sort((a, b) => a.SendCount - b.SendCount); break;
                    case StatisticsSortField.LOAD: list.Sort((a, b) => a.RecvCount - b.RecvCount); break;

                    case StatisticsSortField.rNAME: list.Sort((b, a) => a.MessageType.FullName.CompareTo(b.MessageType.FullName)); break;
                    case StatisticsSortField.rSAVE: list.Sort((b, a) => a.SendCount - b.SendCount); break;
                    case StatisticsSortField.rLOAD: list.Sort((b, a) => a.RecvCount - b.RecvCount); break;
                }
                foreach (var e in list)
                {
                    output.PrintLine(e.GetStatus(), e.MessageType.FullName, prefix, namePlaceHolder);
                }
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintLine("TotalLoadCount", totalLoadCount, prefix, namePlaceHolder);
                output.PrintLine("TotalSaveCount", totalSaveCount, prefix, namePlaceHolder);
                output.PrintLineSeparator(totalPlaceHolder);
            }
        }
        //--------------------------------------------------------------------------------------------------------

        public static void LogSave(Type msgType)
        {
            if (EnableStatistics) { if (msgType != null) statistics.GetOrAdd(msgType, t => new StatisticsMessageInfo(t)).LogSave(); }
        }
        public static void LogLoad(Type msgType)
        {
            if (EnableStatistics) { if (msgType != null) statistics.GetOrAdd(msgType, t => new StatisticsMessageInfo(t)).LogLoad(); }
        }
        public static void LogSaveCall(IMappingDatabaseAsync db)
        {
            if (EnableStatistics)
            {
                if (db is ITransactionDatabase) { return; }
                lock (typeof(ORMStatistics))
                {
                    totalSaveCount++;
                }
            }
        }
        public static void LogLoadCall(IMappingDatabaseAsync db)
        {
            if (EnableStatistics)
            {
                if (db is ITransactionDatabase) { return; }
                lock (typeof(ORMStatistics))
                {
                    totalLoadCount++;
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------

    }
}
