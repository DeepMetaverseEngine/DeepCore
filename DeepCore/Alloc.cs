using DeepCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace DeepCore
{
    //---------------------------------------------------------------------------------------------------------------------------------
    public class TypeAllocRecorder
    {
        //---------------------------------------------------------------------------------------------------------------------------------
        public string Name { get { return name; } }
        public int AllocCount { get { return m_alloc_count; } }
        public int ActiveCount { get { return m_active_count; } }
        private readonly string name;
        private int m_alloc_count = 0;
        private int m_active_count = 0;
        private bool enable = true;
        private bool verbos = true;
        private HashMap<string, Tuple> m_alloc_types = new HashMap<string, Tuple>();
        public bool Enable { get { return enable; } set { lock (m_alloc_types) enable = value; } }
        public bool Verbos { get { return verbos; } set { lock (m_alloc_types) verbos = value; } }
        public TypeAllocRecorder(Type baseType)
        {
            this.name = baseType.Name;
            lock (s_all) s_all.Add(this);
        }
        public TypeAllocRecorder(string name)
        {
            this.name = name;
            lock (s_all) s_all.Add(this);
        }
        public void RecordConstructor(Type type)
        {
            if (is_statistics && enable)
                RecordConstructor(type.ToVisibleName());
        }
        public void RecordDestructor(Type type)
        {
            if (is_statistics && enable)
                RecordDestructor(type.ToVisibleName());
        }
        public void RecordDispose(Type type)
        {
            if (is_statistics && enable)
                RecordDispose(type.ToVisibleName());
        }
        public void RecordReuse(Type type)
        {
            if (is_statistics && enable)
                RecordReuse(type.ToVisibleName());
        }
        public void RecordConstructor(string type)
        {
            if (is_statistics && enable)
            {
                lock (m_alloc_types)
                {
                    {
                        m_alloc_count++;
                        m_active_count++;
                        var tuple = get_tuple(type);
                        tuple.ActiveCount++;
                        tuple.AllocCount++;
                    }
                }
            }
        }
        public void RecordDestructor(string type)
        {
            if (is_statistics && enable)
            {
                lock (m_alloc_types)
                {
                    {
                        m_alloc_count--;
                        var tuple = get_tuple(type);
                        tuple.AllocCount--;
                    }
                }
            }
        }
        public void RecordDispose(string type)
        {
            if (is_statistics && enable)
            {
                lock (m_alloc_types)
                {
                    {
                        m_active_count--;
                        var tuple = get_tuple(type);
                        tuple.ActiveCount--;
                    }
                }
            }
        }
        public void RecordReuse(string type)
        {
            if (is_statistics && enable)
            {
                lock (m_alloc_types)
                {
                    {
                        m_active_count++;
                        var tuple = get_tuple(type);
                        tuple.ActiveCount++;
                    }
                }
            }
        }
        public void PrintStatus(TextWriter output, string prefix = "  ", int namePlaceHolder = 16)
        {
            var map = new SortedDictionary<string, Tuple>(StringComparer.CurrentCulture);
            int total_alloc;
            int total_active;
            lock (m_alloc_types)
            {
                if (!enable)
                {
                    return;
                }
                total_alloc = this.m_alloc_count;
                total_active = this.m_active_count;
                foreach (var e in m_alloc_types)
                {
                    map.Add(e.Key, new Tuple() { ActiveCount = e.Value.ActiveCount, AllocCount = e.Value.AllocCount, });
                }
            }
            output.PrintTitle(Name, "Alloc Infomation", prefix, namePlaceHolder);
            if (VERBOS && verbos)
            {
                foreach (var e in map)
                {
                    if (e.Value.ActiveCount > 0 || e.Value.AllocCount > 0)
                    {
                        output.PrintLine(string.Format($"{e.Value.ActiveCount} / {e.Value.AllocCount}"), e.Key, prefix, namePlaceHolder);
                    }
                }
            }
            output.PrintLine(string.Format($"{total_active} / {total_alloc}"), "[Total]", prefix, namePlaceHolder);
        }
        public bool IsEmpty()
        {
            lock (m_alloc_types)
            {
                return m_active_count <= 0 && m_alloc_count <= 0;
            }
        }
        public override string ToString()
        {
            var sb = new StringWriter();
            {
                this.PrintStatus(sb);
                return sb.ToString();
            }
        }
        private Tuple get_tuple(string type)
        {
            return m_alloc_types.GetOrAdd(type, static (t) =>
            {
                return new Tuple() { ActiveCount = 0, AllocCount = 0, };
            });
        }
        class Tuple
        {
            public int AllocCount;
            public int ActiveCount;
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        #region STATIC---------------------------------------------------------------------------------------------------------------------------------

        private static bool is_statistics = false;
        private static List<TypeAllocRecorder> s_all = new List<TypeAllocRecorder>();
        public static bool ENABLE_STATISTICS
        {
            get { return is_statistics; }
            set { is_statistics = value; }
        }
        public static bool VERBOS { get; set; } = false;

        //---------------------------------------------------------------------------------------------------------------------------------
        public static void AllVerbos(bool value)
        {
            VERBOS = value;
            lock (s_all)
            {
                foreach (var e in s_all)
                {
                    e.Verbos = value;
                }
            }
        }
        public static void AllEnable(bool value)
        {
            ENABLE_STATISTICS = value;
            lock (s_all)
            {
                foreach (var e in s_all)
                {
                    e.Enable = value;
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        public static void GetTotalCount(out long active, out long total)
        {
            total = 0L;
            active = 0L;
            var list = new List<TypeAllocRecorder>();
            lock (s_all) { list.AddRange(s_all); }
            foreach (var e in list)
            {
                active += e.ActiveCount;
                total += e.AllocCount;
            }
        }
        public static void PrintProcessStatus(TextWriter output, System.Diagnostics.Process proc, string prefix = "  ", int namePlaceHolder = 16)
        {
            var list = new List<PropertyInfo>(proc.GetType().GetProperties());
            list.Sort((a, b) => a.Name.CompareTo(b.Name));
            foreach (var field in list)
            {
                try
                {
                    if (field.Name.Contains("Memory") || field.Name.Contains("WorkingSet"))
                    {
                        var size = CUtils.ConvertTo<long>(field.GetValue(proc));
                        output.PrintLine(field.Name, CUtils.ToBytesSizeString(size), prefix, namePlaceHolder);
                    }
                    else
                    {
                        output.PrintLine(field.Name, field.GetValue(proc), prefix, namePlaceHolder);
                    }
                }
                catch (Exception err)
                {
                    output.PrintLine(field.Name, $"({err.Message})", prefix, namePlaceHolder);
                }
            }
        }

        public static void PrintMemoryStatus(TextWriter output, string prefix = "  ", int namePlaceHolder = 16, int totalPlaceHolder = 64)
        {
            var list = new List<TypeAllocRecorder>();
            lock (s_all) { list.AddRange(s_all); }
            list.Sort((a, b) => a.Name.CompareTo(b.Name));
            output.PrintLineSeparator(totalPlaceHolder);
            output.PrintLine("WorkingSet", CUtils.ToBytesSizeString(System.Environment.WorkingSet), prefix, namePlaceHolder);
            //output.PrintLine("GC.TotalMemory", CUtils.ToBytesSizeString(GC.GetTotalMemory(false)), prefix, namePlaceHolder);
            output.PrintLineSeparator(totalPlaceHolder);
            foreach (var e in list)
            {
                if (!e.IsEmpty())
                {
                    e.PrintStatus(output, prefix, namePlaceHolder);
                    output.PrintLineSeparator(totalPlaceHolder);
                }
            }
        }
        public static string GetMemoryStatus(string prefix = "  ", int namePlaceHolder = 16, int totalPlaceHolder = 64)
        {
            var sw = new StringWriter();
            PrintMemoryStatus(sw, prefix, namePlaceHolder, totalPlaceHolder);
            return sw.ToString();
        }

        #endregion ---------------------------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------------------------
    }





    //     public class AllocTestObject : Disposable
    //     {
    //         private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(nameof(AllocTestObject));
    //         private readonly string name;
    //         public AllocTestObject() : this(null) { }
    //         public AllocTestObject(string name)
    //         {
    //             this.name = name;
    //             Alloc.RecordConstructor(name ?? GetType().Name);
    //         }
    //         ~AllocTestObject()
    //         {
    //             Alloc.RecordDestructor(name ?? GetType().Name);
    //         }
    //         protected override void Disposing()
    //         {
    //             Alloc.RecordDispose(name ?? GetType().Name);
    //         }
    //     }
}
