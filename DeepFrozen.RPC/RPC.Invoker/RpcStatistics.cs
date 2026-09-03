using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal.RPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DeepFrozen.RPC.Invoker
{
    public static class RpcStatistics
    {
        //--------------------------------------------------------------------------------------------------------
        private static ConcurrentDictionary<Type, MessageInfo> statistics = new ConcurrentDictionary<Type, MessageInfo>();
        private static AtomicLong s_req_count = new AtomicLong(0);
        private static AtomicLong s_rsp_count = new AtomicLong(0);
        private static Logger s_log = new LazyLogger(nameof(RpcStatistics)); 
        public static bool Enable { get; set; }
#if RELEASE
= false;
#else
= true;
#endif
        private static IExternalizableFactory Factory;
        //--------------------------------------------------------------------------------------------------------
        public static void Init(IExternalizableFactory factory, bool enable)
        {
            Enable = enable;
            Factory = factory;
        }
        public static StackTrace AllocTrace()
        {
            if (Enable) { return new StackTrace(); }
            return null;
        }
        internal class MessageInfo
        {
            private readonly Type msgType;
            private readonly bool haveReturn;
            private int sendCount = 0;
            private int recvCount = 0;
            private float avgUseTime = 0;
            private float maxUseTime = 0;
            private float minUseTime = 0;
            private double totalUseTime = 0;
            //private LinkedList<Delegate> waittingRequests = new LinkedList<Delegate>();
            public Type MessageType { get => msgType; }
            public int SendCount { get { lock (this) { return sendCount; } } }
            public int RecvCount { get { lock (this) { return recvCount; } } }
            public float AvgUseTime { get { lock (this) { return avgUseTime; } } }
            public float MaxUseTime { get { lock (this) { return maxUseTime; } } }
            public float MinUseTime { get { lock (this) { return minUseTime; } } }
            internal MessageInfo(Type type, bool haveReturn)
            {
                this.msgType = type;
                this.haveReturn = haveReturn;
            }
            private void LogTime(double used)
            {
                lock (this)
                {
                    this.recvCount++;
                    this.totalUseTime += used;
                    this.maxUseTime = (float)Math.Max(used, this.maxUseTime);
                    this.minUseTime = (float)Math.Min(used, this.minUseTime);
                    this.avgUseTime = (float)(totalUseTime / recvCount);
                }
            }
            internal void LogCount()
            {
                lock (this) { this.sendCount++; }
            }
            internal void LogTime(ref OnRpcReturn<ISerializable> callback)
            {
                s_req_count++;
                var watch = CUtils.TickTimeMS;
                var src_callback = callback;
                lock (this)
                {
                    this.sendCount++;
                    //var waitting = waittingRequests.AddLast(src_callback);
                    callback = new OnRpcReturn<ISerializable>((rsp, err) =>
                    {
                        s_rsp_count++;
                        //lock (this) waittingRequests.Remove(waitting);
                        try
                        {
                            src_callback(rsp, err);
                        }
                        finally
                        {
                            LogTime(CUtils.TickTimeMS - watch);
                        }
                    });
                }

            }
            internal void LogTime(ref OnRpcReturnBinary callback)
            {
                s_req_count++;
                var watch = CUtils.TickTimeMS;
                var src_callback = callback;
                lock (this)
                {
                    this.sendCount++;
                    //var waitting = waittingRequests.AddLast(src_callback);
                    callback = new OnRpcReturnBinary((rsp, err) =>
                    {
                        s_rsp_count++;
                        //lock (this) waittingRequests.Remove(waitting);
                        try
                        {
                            src_callback(rsp, err);
                        }
                        finally
                        {
                            LogTime(CUtils.TickTimeMS - watch);
                        }
                    });
                }

            }
            public string GetStatus()
            {
                lock (this)
                {
                    var rcount = haveReturn ? recvCount.ToString() : "N/A";
                    var min_t = haveReturn ? minUseTime.ToString() : "N/A";
                    var max_t = haveReturn ? maxUseTime.ToString() : "N/A";
                    var avg_t = haveReturn ? avgUseTime.ToString() : "N/A";
                    var ret = string.Format("REQ={0} RSP={1} MIN={2} MAX={3} AVG={4}",
                    CUtils.FillPlaceHolder(sendCount.ToString(), 8, ' ', 0),
                    CUtils.FillPlaceHolder(rcount, 8, ' ', 0),
                    CUtils.FillPlaceHolder(min_t.ToString(), 6, ' ', 0),
                    CUtils.FillPlaceHolder(max_t.ToString(), 6, ' ', 0),
                    CUtils.FillPlaceHolder(avg_t.ToString(), 6, ' ', 0));
                    return ret;
                }
            }
            public void PrintWaitting(TextWriter output, string prefix = "    ", int namePlaceHolder = 30)
            {
                lock (this)
                {
                    if (haveReturn && (sendCount != recvCount))
                    {
                        output.PrintLine(msgType.FullName, sendCount + "/" + recvCount, prefix, namePlaceHolder);
                        //                         foreach (var e in waittingRequests)
                        //                         {
                        //                             output.PrintLine("    " + e.Target, e, prefix, namePlaceHolder);
                        //                         }
                    }
                }
            }
        }
        public enum SortField
        {
            NAME, REQ, RSP, MIN, MAX, AVG,
            rNAME, rREQ, rRSP, rMIN, rMAX, rAVG,
        }
        public static void PrintStatus(TextWriter output, SortField sort = SortField.MAX, string prefix = "    ", int namePlaceHolder = 30, int totalPlaceHolder = 100)
        {
            using (var list = new ArrayList<MessageInfo>(statistics.Values))
            {
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintTitle("RpcStatistics", "Total=" + list.Count, prefix, namePlaceHolder);
                output.PrintLineSeparator(totalPlaceHolder);
                switch (sort)
                {
                    case SortField.NAME: list.Sort((a, b) => a.MessageType.FullName.CompareTo(b.MessageType.FullName)); break;
                    case SortField.REQ: list.Sort((a, b) => a.SendCount - b.SendCount); break;
                    case SortField.RSP: list.Sort((a, b) => a.RecvCount - b.RecvCount); break;
                    case SortField.MIN: list.Sort((a, b) => (int)(a.MinUseTime - b.MinUseTime)); break;
                    case SortField.MAX: list.Sort((a, b) => (int)(a.MaxUseTime - b.MaxUseTime)); break;
                    case SortField.AVG: list.Sort((a, b) => (int)(a.AvgUseTime - b.AvgUseTime)); break;

                    case SortField.rNAME: list.Sort((b, a) => a.MessageType.FullName.CompareTo(b.MessageType.FullName)); break;
                    case SortField.rREQ: list.Sort((b, a) => a.SendCount - b.SendCount); break;
                    case SortField.rRSP: list.Sort((b, a) => a.RecvCount - b.RecvCount); break;
                    case SortField.rMIN: list.Sort((b, a) => (int)(a.MinUseTime - b.MinUseTime)); break;
                    case SortField.rMAX: list.Sort((b, a) => (int)(a.MaxUseTime - b.MaxUseTime)); break;
                    case SortField.rAVG: list.Sort((b, a) => (int)(a.AvgUseTime - b.AvgUseTime)); break;
                }
                foreach (var e in list)
                {
                    output.PrintLine(e.GetStatus(), e.MessageType.FullName, prefix, namePlaceHolder);
                }
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintTitle("WaittingRequests", "", prefix, namePlaceHolder);
                foreach (var e in list)
                {
                    e.PrintWaitting(output, prefix, namePlaceHolder);
                }
                output.PrintLineSeparator(totalPlaceHolder);
                output.PrintLine("TotalRequest", s_req_count.Value, prefix, namePlaceHolder);
                output.PrintLine("TotalResponse", s_rsp_count.Value, prefix, namePlaceHolder);
                output.PrintLineSeparator(totalPlaceHolder);
            }
        }
        //--------------------------------------------------------------------------------------------------------
        internal static void LogRpcNotify(Type msgType)
        {
            if (Enable)
            {
                statistics.GetOrAdd(msgType, t => new MessageInfo(t, false)).LogCount();
            }
        }
        internal static void LogRpcNotify(int msgID)
        {
            if (Enable && Factory != null)
            {
                var msgType = Factory.GetType(msgID);
                if (msgType != null)
                {
                    statistics.GetOrAdd(msgType, t => new MessageInfo(t, false)).LogCount();
                }
                else
                {
                    s_log.Error("can not find msgID " + msgID);
                }
            }
        }
        internal static void LogRpcNotify(ICollection<ISerializable> list)
        {
            if (Enable)
            {
                foreach (var msg in list) { if (msg != null) LogRpcNotify(msg.GetType()); }
            }
        }
        internal static void LogRpcNotify(ICollection<BinaryMessage> list)
        {
            if (Enable)
            {
                foreach (var msg in list) { LogRpcNotify(msg.Route); }
            }
        }
        //--------------------------------------------------------------------------------------------------------
        internal static void LogRpcRequest(Type msgType, ref OnRpcReturn<ISerializable> callback)
        {
            if (Enable)
            {
                statistics.GetOrAdd(msgType, t => new MessageInfo(t, true)).LogTime(ref callback);
            }
        }
        internal static void LogRpcRequest(int msgID, ref OnRpcReturnBinary callback)
        {
            if (Enable && Factory!=null)
            {
                var type = Factory.GetType(msgID);
                if (type != null)
                {
                    statistics.GetOrAdd(type, t => new MessageInfo(t, true)).LogTime(ref callback);
                }
                else
                {
                    s_log.Error("can not find msgID " + msgID);
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------


    }
}
