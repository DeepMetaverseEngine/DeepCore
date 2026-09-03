using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore
{

    public static class CUtils
    {

        public static readonly Encoding UTF8 = new UTF8Encoding(false, false);
        public static readonly Encoding UTF8_BOM = new UTF8Encoding(true, false);
        private static System.Diagnostics.Stopwatch start_time = System.Diagnostics.Stopwatch.StartNew();

        public const int BYTES_1K = 1024;
        public const int BYTES_1M = 1024 * 1024;
        public const int BYTES_1G = 1024 * 1024 * 1024;
        public const long BYTES_1T = 1024L * 1024 * 1024 * 1024;

        public static double TickTimeMS
        {
            get { return start_time.Elapsed.TotalMilliseconds; }
        }
        public static Random Random { get; private set; } = new Random();

        public static T GetOrCreate<T>(T obj, Func<T> create) where T : class
        {
            if (obj == null)
            {
                obj = create();
            }
            return obj;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T ta = a;
            a = b;
            b = ta;
        }

        public static Span<byte> PointerToSpan(this IntPtr ptr, int size)
        {
            unsafe
            {
                return new Span<byte>(ptr.ToPointer(), size);
            }
        }

        public static IntPtr ToBytesPtr(this byte[] bytes)
        {
            unsafe
            {
                fixed (byte* p = bytes)
                {
                    IntPtr ptr = (IntPtr)p;
                    return ptr;
                }
            }
        }
        public static IntPtr ToIntPtr(object obj)
        {
            if (obj == null)
            {
                return new IntPtr(0);
            }
            GCHandle hObject = GCHandle.Alloc(obj, GCHandleType.Pinned);
            IntPtr pObject = hObject.AddrOfPinnedObject();
            if (hObject.IsAllocated)
                hObject.Free();
            return pObject;
        }

        public static bool TryConvertTo(object src, Type targetType)
        {
            object target;
            return TryConvertTo(src, targetType, out target);
        }
        public static bool TryConvertTo(object src, Type targetType, out object target)
        {
            if (targetType.IsInstanceOfType(src))
            {
                target = src;
                return true;
            }
            try
            {
                target = Convert.ChangeType(src, targetType);
                if (targetType.IsInstanceOfType(target))
                {
                    return true;
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            target = null;
            return false;
        }
        public static bool TryConvertTo<T>(object src, out T target)
        {
            if (src is T t)
            {
                target = t;
                return true;
            }
            try
            {
                target = (T)Convert.ChangeType(src, typeof(T));
                if (target is T)
                {
                    return true;
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            target = default;
            return false;
        }
        public static object ConvertTo(object src, Type targetType)
        {
            if (TryConvertTo(src, targetType, out var ret))
            {
                return ret;
            }
            return default;
        }
        public static T ConvertTo<T>(object src)
        {
            if (TryConvertTo<T>(src, out var ret))
            {
                return ret;
            }
            return default;
        }


        public static void PrintGCFinalizers()
        {
            PrintGCFinalizers(Console.Out);
        }
        public static void PrintGCFinalizers(TextWriter output)
        {
            GC.Collect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCApproach();
            GC.WaitForFullGCComplete();
            output.PrintLineSeparator();
            TypeAllocRecorder.PrintMemoryStatus(output);
            output.PrintLineSeparator();
        }

        //----------------------------------------------------------------------------------------------------------
        #region RANDOM

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }
        public static float NextFloat(this Random random, float min, float max)
        {
            var d = max - min;
            return min + (float)(random.NextDouble() * d);
        }
        public static double NextDouble(this Random random, double min, double max)
        {
            var d = max - min;
            return min + (random.NextDouble() * d);
        }

        public static bool RandomPercent(this Random random, float percent)
        {
            //大于等于 0.0 并且小于 1.0 的双精度浮点数。//
            if (random.NextDouble() * 100f < percent)
            {
                return true;
            }
            return false;
        }

        public static float RandomFactor(this Random random, float value, float factor)
        {
            if (factor == 0)
            {
                return value;
            }
            return value + value * (float)(factor / 2f + random.NextDouble() * factor);
        }

        public static T RandomEnumValue<T>(this Random random)
        {
            Array values = Enum.GetValues(typeof(T));
            int count = values.Length;
            if (count > 0)
            {
                var ret = values.GetValue(random.Next(0, count));
                return (T)ret;
            }
            return default(T);
        }

        public static void RandomList<T>(this Random random, IList<T> src)
        {
            for (int i = src.Count - 1; i >= 0; i--)
            {
                int r = random.Next(0, src.Count);
                T t = src[r];
                src[r] = src[i];
                src[i] = t;
            }
        }

        public static void RandomArray<T>(this Random random, T[] src)
        {
            for (int i = src.Length - 1; i >= 0; i--)
            {
                int r = random.Next(0, src.Length);
                T t = src[r];
                src[r] = src[i];
                src[i] = t;
            }
        }

        public static T GetRandomInCollection<T>(this Random random, ICollection<T> list)
        {
            if (list.Count == 0) return default(T);
            if (list is IReadOnlyList<T> array)
            {
                return GetRandomInList(random, array);
            }
            else
            {
                return GetRandomInList(random, new List<T>(list));
            }
        }
        public static T GetRandomInCollection<T>(this Random random, IEnumerable<T> list)
        {
            if (list is IReadOnlyList<T> array)
            {
                return GetRandomInList(random, array);
            }
            else
            {
                return GetRandomInList(random, new List<T>(list));
            }
        }

        public static T GetRandomInList<T>(this Random random, IReadOnlyList<T> list)
        {
            if (list.Count == 0) return default(T);
            int rd = random.Next(list.Count);
            return list[rd];
        }

        public static T GetRandomInArray<T>(this Random random, T[] list)
        {
            if (list.Length == 0) return default(T);
            int rd = random.Next(list.Length);
            return list[rd];
        }

        public static object GetRandomInArray(this Random random, Array list)
        {
            if (list.Length == 0) { return null; }
            int rd = random.Next(list.Length);
            return list.GetValue(rd);
        }


        public static T GetRandomInArray<T>(T[] arr, int[] weight = null, Random random = null)
        {
            var ret = GetRandomInArray(arr, 1, weight, random);
            if (ret != null)
            {
                return ret[0];
            }
            return default(T);
        }

        /// <summary>
        /// 简单随机算法，可指定每项权重
        /// </summary>
        public static T[] GetRandomInArray<T>(T[] arr, int count, int[] weight = null, Random ran = null)
        {
            if (arr == null || arr.Length == 0)
            {
                return null;
            }
            var totalWeight = 0;
            int[] numArr = null;
            if (weight != null && weight.Length == arr.Length)
            {
                var currentIndex = 0;
                numArr = new int[weight.Length];
                for (var i = 0; i < weight.Length; i++)
                {
                    totalWeight += weight[i];
                    if (weight[i] == 0)
                    {
                        numArr[i] = 0;
                    }
                    else
                    {
                        numArr[i] = currentIndex + weight[i];
                        currentIndex = numArr[i];
                    }
                }
            }
            if (ran == null)
            {
                ran = new Random();
            }
            var ret = new T[count];
            for (var i = 0; i < count; i++)
            {
                if (totalWeight > 0)
                {
                    var num = ran.Next(0, totalWeight);
                    for (var j = 0; j < numArr.Length; j++)
                    {
                        if (num < numArr[j])
                        {
                            ret[i] = arr[j];
                            break;
                        }
                    }
                }
                else
                {
                    var index = ran.Next(0, arr.Length);
                    ret[i] = arr[index];
                }
            }

            return ret;
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region CLONE
        public static object[] CloneArray(object[] src)
        {
            if (src == null) return null;
            var ret = new object[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] is ICloneable c)
                {
                    ret[i] = c.Clone();
                }
                else
                {
                    ret[i] = ret[i];
                }
            }
            return ret;
        }

        public static T TryClone<T>(T src) where T : class, ICloneable
        {
            if (src == null) return null;
            return (T)src.Clone();
        }

        public static T[] CloneArray<T>(T[] src) where T : class, ICloneable
        {
            if (src == null) return null;
            T[] ret = new T[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] != null)
                {
                    ret[i] = (T)src[i].Clone();
                }
                else
                {
                    ret[i] = null;
                }
            }
            return ret;
        }

        public static ArrayList<T> CloneList<T>(IList<T> src) where T : class, ICloneable
        {
            if (src == null) return null;
            var ret = new ArrayList<T>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                if (src[i] != null)
                {
                    ret.Add((T)src[i].Clone());
                }
                else
                {
                    ret.Add(null);
                }
            }
            return ret;
        }

        public static HashMap<K, V> CloneMap<K, V>(IDictionary<K, V> map) where V : ICloneable
        {
            if (map == null) return null;
            var ret = new HashMap<K, V>(map.Count);
            foreach (K k in map.Keys)
            {
                ret.Add(k, (V)map[k]?.Clone());
            }
            return ret;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------

        #region STRING

        public static StringComparison StringComparisonIgnoreCase = StringComparison.OrdinalIgnoreCase;
        public static Regex SplitWhiteSpace = new Regex(@"\s+");

        public static bool NotNullOrEmpty(this string str) => !string.IsNullOrEmpty(str);
        public static bool NotNullOrWhiteSpace(this string str) => !string.IsNullOrEmpty(str);
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static string[] StringToLines(string src)
        {
            return src?.Split('\n');
        }

        public static string StringFromLines(params string[] lines)
        {
            if (lines == null) return null;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(lines[i]);
                if (i < lines.Length - 1)
                {
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public static string[] StringSplitWhiteSpace(this string a, int count)
        {
            if (a == null) return null;
            return SplitWhiteSpace.Split(a, count);
        }
        public static string[] StringSplitWhiteSpace(this string a)
        {
            if (a == null) return null;
            return SplitWhiteSpace.Split(a);
        }


        public static bool StringEqualsIgnoreCase(this string a, string b)
        {
            return a.Equals(b, StringComparisonIgnoreCase);
        }
        public static bool StringStartWithIgnoreCase(this string a, string b)
        {
            return a.StartsWith(b, StringComparisonIgnoreCase);
        }

        public static string FromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return string.Empty;
            var bytes = System.Convert.FromBase64String(base64);
            return DecodeUTF8(bytes);
        }
        public static string ToBase64(string text)
        {
            var bytes = UTF8.GetBytes(text);
            var base64 = System.Convert.ToBase64String(bytes);
            return base64;
        }

        public static string DecodeUTF8(byte[] data)
        {
            if (data.Length > 3)
            {
                if ((data[0] == 0xEF) && (data[1] == 0xBB) && (data[2] == 0xBF))
                {
                    return UTF8_BOM.GetString(data, 3, data.Length - 3);
                }
            }
            return UTF8.GetString(data);
        }
        public static string DecodeUTF8(byte[] data, out Encoding encoding)
        {
            if (data.Length > 3)
            {
                if ((data[0] == 0xEF) && (data[1] == 0xBB) && (data[2] == 0xBF))
                {
                    encoding = UTF8_BOM;
                    return UTF8_BOM.GetString(data, 3, data.Length - 3);
                }
            }
            encoding = UTF8;
            return UTF8.GetString(data);
        }
        public static byte[] EncodeUTF8(string src)
        {
            return UTF8.GetBytes(src);
        }

        public static BigInteger ToBigInteger(this Guid guid)
        {
            return new BigInteger(guid.ToByteArray());
        }

        //         public static string ReadAllText(string path)
        //         {
        //             var bin = Resource.LoadAllText(path);
        //             return DecodeUTF8(bin);
        //         }

        public static string BinToHex(byte[] bin, int offset, int length, bool prefix = false)
        {
            if (bin == null)
            {
                return null;
            }
            var sb = new StringBuilder();
            {
                if (prefix)
                {
                    sb.Append("0x");
                }
                for (int i = 0; i < length; i++)
                {
                    var b = bin[i + offset];
                    var hex = b.ToString("X2");
                    if (hex.Length < 2)
                    {
                        sb.Append("0" + hex);
                    }
                    else if (hex.Length == 2)
                    {
                        sb.Append(hex);
                    }
                    else
                    {
                        throw new Exception($"BinToHex: {b} -> {hex}");
                    }
                }
                return sb.ToString();
            }
        }
        public static string BinToHex(this byte[] value, bool prefix = false)
        {
            return string.Concat(prefix ? "0x" : "", string.Concat(value.Select((byte b) => b.ToString("x2")).ToArray()));
        }
        public static byte[] HexToBin(string hex)
        {
            if (hex == null)
            {
                return null;
            }
            if (hex.StringStartWithIgnoreCase("0x"))
            {
                hex = hex.Substring(2, hex.Length - 2);
            }
            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex;
            }
            int count = hex.Length;
            byte[] os = new byte[count / 2];
            for (int i = 0; i < count; i += 2)
            {
                string hch = hex.Substring(i, 2);
                byte read = byte.Parse(hch, NumberStyles.HexNumber);
                os[i / 2] = read;
            }
            return os;
        }
        public static string ToBytesString(this long bytes, string format = "#.##", bool upper = true)
        {
            double b = bytes;
            double kb = b / 1024f;
            double mb = kb / 1024f;
            double gb = mb / 1024f;
            double tb = gb / 1024f;
            if (tb > 10)
            {
                return $"{tb.ToString(format)}{(upper ? 'T' : 't')}";
            }
            if (gb > 10)
            {
                return $"{gb.ToString(format)}{(upper ? 'G' : 'g')}";
            }
            if (mb > 10)
            {
                return $"{mb.ToString(format)}{(upper ? 'M' : 'm')}";
            }
            if (kb > 10)
            {
                return $"{kb.ToString(format)}{(upper ? 'K' : 'k')}";
            }
            return $"{b.ToString(format)}{(upper ? 'B' : 'b')}";

        }
        public static string ToBytesSizeString(long bytes)
        {
            long b = bytes;
            long kb = b >> 10;
            long mb = kb >> 10;
            long gb = mb >> 10;
            long tb = gb >> 10;
            if (tb > 10)
            {
                return string.Format("{0}.{1}t", tb, gb % 1024);
            }
            if (gb > 10)
            {
                return string.Format("{0}.{1}g", gb, mb % 1024);
            }
            if (mb > 10)
            {
                return string.Format("{0}.{1}m", mb, kb % 1024);
            }
            if (kb > 10)
            {
                return string.Format("{0}.{1}k", kb, b % 1024);
            }
            return string.Format("{0}b", b);

        }



        public static void ForEachLast(int count, ForEachLastAction tostring)
        {
            var lastIndex = count - 1;
            for (int i = 0; i < count; i++)
            {
                tostring(i, i == lastIndex);
            }
        }
        public static void ForEachLast<ST>(ST st, int count, ForEachLastAction<ST> tostring)
        {
            var lastIndex = count - 1;
            for (int i = 0; i < count; i++)
            {
                tostring(st, i, i == lastIndex);
            }
        }
        public static void ForEachLast<T, ST>(this T[] list, ST st, ForEachLastAction<T, ST> tostring)
        {
            if (list == null) return;
            var lastIndex = list.Length - 1;
            for (int i = 0; i < list.Length; i++)
            {
                tostring(st, i, list[i], i == lastIndex);
            }
        }
        public static void ForEachLast<T, ST>(this IReadOnlyList<T> list, ST st, ForEachLastAction<T, ST> tostring)
        {
            if (list == null) return;
            var lastIndex = list.Count - 1;
            for (int i = 0; i < list.Count; i++)
            {
                tostring(st, i, list[i], i == lastIndex);
            }
        }

        public delegate void ForEachBuildCommandAction(StringBuilder sb, int index, bool last);
        public static void ForEachBuildCommand(this StringBuilder sb, int count, ForEachBuildCommandAction tostring, string split = ",")
        {
            var lastIndex = count - 1;
            ForEachLast((sb), count, (sb, i, end) =>
            {
                tostring(sb, i, end);
                if (!end)
                {
                    sb.AppendLine(split);
                }
            });
        }



        public static string ArrayToString<T>(this T[] list, Func<T, string> tostring, string split = ", ", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                var lastIndex = list.Length - 1;
                for (int i = 0; i < list.Length; i++)
                {
                    T obj = list[i];
                    sb.Append(prefix + tostring(obj) + suffix);
                    if (i < lastIndex)
                    {
                        sb.Append(split);
                    }
                }
                return sb.ToString();
            }
        }
        public static string ListToString<T>(this IReadOnlyList<T> list, Func<T, string> tostring, string split = ", ", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                var lastIndex = list.Count - 1;
                for (int i = 0; i < list.Count; i++)
                {
                    T obj = list[i];
                    sb.Append(prefix + tostring(obj) + suffix);
                    if (i < lastIndex)
                    {
                        sb.Append(split);
                    }
                }
                return sb.ToString();
            }
        }
        public static string MapToString<K, V>(this IReadOnlyDictionary<K, V> list, Func<K, V, string> tostring, string kv_split = "=", string line_split = "\n", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                int i = 0;
                foreach (var k in list.Keys)
                {
                    var v = list[k];
                    sb.Append(prefix + tostring(k, v) + suffix);
                    if (i < list.Count - 1)
                    {
                        sb.Append(line_split);
                    }
                    i++;
                }
                return sb.ToString();
            }
        }


        public static string ArrayToString(this Array list, string split = ", ", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                for (int i = 0; i < list.Length; i++)
                {
                    object obj = list.GetValue(i);
                    sb.Append(prefix + obj + suffix);
                    if (i < list.Length - 1)
                    {
                        sb.Append(split);
                    }
                }
                return sb.ToString();
            }
        }
        public static string ListToString(this IList list, string split = ", ", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                for (int i = 0; i < list.Count; i++)
                {
                    object obj = list[i];
                    sb.Append(prefix + obj + suffix);
                    if (i < list.Count - 1)
                    {
                        sb.Append(split);
                    }
                }
                return sb.ToString();
            }
        }
        public static string MapToString(this IDictionary list, string kv_split = "=", string line_split = "\n", string prefix = "", string suffix = "")
        {
            if (list == null) return string.Empty;
            var sb = new StringBuilder();
            {
                int i = 0;
                foreach (object key in list.Keys)
                {
                    object obj = list[key];
                    sb.Append(prefix + key + kv_split + obj + suffix);
                    if (i < list.Count - 1)
                    {
                        sb.Append(line_split);
                    }
                    i++;
                }
                return sb.ToString();
            }
        }

        public static T[] GetRange<T>(this T[] src, int index, int count)
        {
            T[] ret = new T[count];
            Array.Copy(src, index, ret, 0, count);
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="format">YYMMDD_hhmmss</param>
        /// <returns></returns>
        public static string FormatTime(DateTime time, string format = "YYYYMMDD_hhmmss")
        {
            format = format.Replace("YYYY", time.Year.ToString("d4"));
            format = format.Replace("MM", time.Month.ToString("d2"));
            format = format.Replace("DD", time.Day.ToString("d2"));
            format = format.Replace("hh", time.Hour.ToString("d2"));
            format = format.Replace("mm", time.Minute.ToString("d2"));
            format = format.Replace("ss", time.Second.ToString("d2"));
            return format;
        }
        public static bool TryParseTime(string txt, out DateTime time, DateTimeKind kind = DateTimeKind.Local, string format = "YYYYMMDD_hhmmss")
        {
            try
            {
                if (txt.Length == format.Length)
                {
                    int YYYY = 0;
                    int MM = 0;
                    int DD = 0;
                    int hh = 0;
                    int mm = 0;
                    int ss = 0;
                    if (format.TryIndexOf("YYYY", out var index) && int.TryParse(txt.Substring(index, 4), out YYYY))
                    {

                    }
                    if (format.TryIndexOf("MM", out index) && int.TryParse(txt.Substring(index, 2), out MM))
                    {

                    }
                    if (format.TryIndexOf("DD", out index) && int.TryParse(txt.Substring(index, 2), out DD))
                    {

                    }
                    if (format.TryIndexOf("hh", out index) && int.TryParse(txt.Substring(index, 2), out hh))
                    {

                    }
                    if (format.TryIndexOf("mm", out index) && int.TryParse(txt.Substring(index, 2), out mm))
                    {

                    }
                    if (format.TryIndexOf("ss", out index) && int.TryParse(txt.Substring(index, 2), out ss))
                    {

                    }
                    time = new DateTime(YYYY, MM, DD, hh, mm, ss, kind); // 默认当前时间
                    return true;
                }
            }
            catch { }
            time = DateTime.MinValue;
            return false;
        }

        public static string FormatBlockTableString(string info)
        {
            var sb = new StringBuilder();
            {
                var list = info.Split('\n');
                int max_len = 0;
                foreach (var line in list)
                {
                    max_len = Math.Max(max_len, line.Length);
                }
                sb.Append('┌').Append('─', max_len).Append('┐').AppendLine();
                foreach (var line in list)
                {
                    sb.Append('│').Append(line).Append(' ', max_len - line.Length).Append('│').AppendLine();
                }
                sb.Append('└').Append('─', max_len).Append('┘').AppendLine();
                return sb.ToString();
            }
        }

        public static string SequenceChar(char ch, int count)
        {
            var sb = new StringBuilder();
            {
                sb.Append(ch, count);
                return sb.ToString();
            }
        }
        public static string SequenceString(string ch, int count)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                sb.Append(ch);
            }
            return sb.ToString();
        }
        public static void FillString(this StringBuilder sb, string ch, int count)
        {
            for (var i = 0; i < count; i++)
            {
                sb.Append(ch);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="count"></param>
        /// <param name="placeholder"></param>
        /// <param name="anchor">0=left, 1=right, 2=center</param>
        /// <returns></returns>
        public static string FillPlaceHolder(string src, int count, char placeholder = ' ', int anchor = 0)
        {
            if (src.Length >= count) { return src; }
            var sb = new StringBuilder();
            {
                if (anchor == 2)
                {
                    var left_space = (count - src.Length) >> 1;
                    var right_space = count - src.Length - left_space;
                    for (int i = 0; i < left_space; ++i)
                    {
                        sb.Append(placeholder);
                    }
                    sb.Append(src);
                    for (int i = 0; i < right_space; ++i)
                    {
                        sb.Append(placeholder);
                    }
                }
                else
                {
                    var space = count - src.Length;
                    if (anchor == 0) { sb.Append(src); }
                    for (int i = 0; i < space; ++i)
                    {
                        sb.Append(placeholder);
                    }
                    if (anchor == 1) { sb.Append(src); }
                }
                return sb.ToString();
            }
        }


        public static bool GetTextRegion(string text, string begin, string end, out string region, bool include = false)
        {
            int i_begin = text.IndexOf(begin);
            int i_end = text.LastIndexOf(end);
            if (i_begin >= 0 && i_end > i_begin)
            {
                if (include)
                {
                    region = text.Substring(i_begin, i_end - i_begin + end.Length);
                }
                else
                {
                    region = text.Substring(i_begin + begin.Length, i_end - i_begin - begin.Length);
                }
                return true;
            }
            region = null;
            return false;
        }
        public static bool GetTextRegion(string text, char begin, char end, out string region, bool include = false)
        {
            int i_begin = text.IndexOf(begin);
            int i_end = text.LastIndexOf(end);
            if (i_begin >= 0 && i_end > i_begin)
            {
                if (include)
                {
                    region = text.Substring(i_begin, i_end - i_begin + 1);
                }
                else
                {
                    region = text.Substring(i_begin + 1, i_end - i_begin - 1);
                }
                return true;
            }
            region = null;
            return false;
        }




        public delegate string TransformText(string src);

        public static string ProcessAllLines(string text, TransformText action)
        {
            if (text.EndsWith("\r"))
            {
                text = text.Substring(0, text.Length - 1);
            }
            if (text.EndsWith("\n"))
            {
                text = text.Substring(0, text.Length - 1);
            }
            string[] lines = text.Split('\n');
            var sb = new StringBuilder();
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.EndsWith("\r"))
                    {
                        line = line.Substring(0, line.Length - 1);
                    }
                    if (i < lines.Length - 1)
                    {
                        sb.AppendLine(action(line));
                    }
                    else
                    {
                        sb.Append(action(line));
                    }
                }
                return sb.ToString();
            }
        }


        /// <summary>
        /// input "{1,2,3,4},{5,6,7,8}"
        /// return "1,2,3,4", "5,6,7,8"
        /// </summary>
        public static string[] GetArray2D(string text, char ltc = '{', char rtc = '}')
        {
            var list = new List<string>();
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int lt = text.IndexOf(ltc, i);
                    int rt = text.IndexOf(rtc, i);
                    if (lt >= 0 && rt > lt)
                    {
                        list.Add(text.Substring(lt + 1, rt - lt - 1).Trim());
                    }
                    else
                    {
                        break;
                    }
                    i = rt;
                }
                return list.ToArray();
            }
        }
        public static T[][] GetArray2D<T>(string text, Func<string, T> parse, char ltc = '{', char rtc = '}', char split = ',')
        {
            var list = new List<T[]>();
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int lt = text.IndexOf(ltc, i);
                    int rt = text.IndexOf(rtc, i);
                    if (lt >= 0 && rt > lt)
                    {
                        var sub = new List<T>();
                        foreach (var item in text.Substring(lt + 1, rt - lt - 1).Split(split))
                        {
                            sub.Add(parse.Invoke(item));
                        }
                        list.Add(sub.ToArray());
                    }
                    else
                    {
                        break;
                    }
                    i = rt;
                }
            }
            return list.ToArray();

        }

        public static string ReplaceAll(this string str, string src, string dst)
        {
            if (src == dst) return str;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + dst + str.Substring(index + src.Length);
            }
            return str;
        }
        public static string ReplaceAll(this string str, char src, char dst)
        {
            if (src == dst) return str;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + dst + str.Substring(index + 1);
            }
            return str;
        }
        public static string ReplaceAll<ST>(this string str, ST st, string src, Func<ST, int, string> tostring)
        {
            int count = 0;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + tostring(st, count) + str.Substring(index + src.Length);
                count++;
            }
            return str;
        }

        public static bool TryReplace(ref string str, char src, char dst, out int index)
        {
            index = -1;
            if (src == dst) return false;
            if (str.TryIndexOf(src, out index))
            {
                str = str.Substring(0, index) + dst + str.Substring(index + 1);
                return true;
            }
            return false;
        }

        public static int TryReplaceAll(ref string str, string src, string dst)
        {
            if (src == dst) return 0;
            int count = 0;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + dst + str.Substring(index + src.Length);
                count++;
            }
            return count;
        }
        public static int TryReplaceAll(ref string str, char src, char dst)
        {
            if (src == dst) return 0;
            int count = 0;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + dst + str.Substring(index + 1);
                count++;
            }
            return count;
        }
        public static int TryReplaceAll<ST>(ref string str, ST st, string src, Func<ST, int, string> tostring)
        {
            int count = 0;
            while (str.TryIndexOf(src, out var index))
            {
                str = str.Substring(0, index) + tostring(st, count) + str.Substring(index + src.Length);
                count++;
            }
            return count;
        }


        public static bool TryIndexOf(this string str, char ch, out int index, int start, int count)
        {
            index = str.IndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, char ch, out int index, int start, int count)
        {
            index = str.LastIndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryIndexOf(this string str, char ch, out int index, int start)
        {
            index = str.IndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, char ch, out int index, int start)
        {
            index = str.LastIndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryIndexOf(this string str, char ch, out int index)
        {
            index = str.IndexOf(ch);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, char ch, out int index)
        {
            index = str.LastIndexOf(ch);
            return index >= 0;
        }
        public static bool TryIndexOf(this string str, string ch, out int index, int start, int count)
        {
            index = str.IndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, string ch, out int index, int start, int count)
        {
            index = str.LastIndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryIndexOf(this string str, string ch, out int index, int start)
        {
            index = str.IndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, string ch, out int index, int start)
        {
            index = str.LastIndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryIndexOf(this string str, string ch, out int index)
        {
            index = str.IndexOf(ch);
            return index >= 0;
        }
        public static bool TryLastIndexOf(this string str, string ch, out int index)
        {
            index = str.LastIndexOf(ch);
            return index >= 0;
        }


        public static string Indent(this string txt, string indent = "    ")
        {
            var sb = new StringBuilder();
            var lines = txt.Split('\n');
            foreach (var line in lines)
            {
                sb.AppendLine(indent + line);
            }
            return sb.ToString();
        }
        public static string Indent(this string txt, int indent, char indentChar = ' ', int indentCharCount = 4)
        {
            var sb = new StringBuilder();
            var lines = txt.Split('\n');
            foreach (var line in lines)
            {
                sb.AppendLine(SequenceChar(indentChar, indent * indentCharCount) + line);
            }
            return sb.ToString();
        }

        public static string SubStringRange(this string txt, int beginIndex, int endIndex)
        {
            return txt.Substring(beginIndex, endIndex - beginIndex);
        }

        private static Regex REG_NUM = new Regex("\\d+");
        /// <summary>
        /// 检测数字开头的字段
        /// </summary>
        /// <param name="text"></param>
        /// <param name="digit"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public static bool TryStartsWithDigit(string text, out string digit, out string word)
        {
            var m = REG_NUM.Match(text);
            if (m.Success && m.Index == 0)
            {
                digit = text.Substring(m.Index, m.Length);
                word = text.Substring(m.Index + m.Length);
                return true;
            }
            word = null;
            digit = null;
            return false;
        }
        /// <summary>
        /// 检测数字结尾的字段
        /// </summary>
        /// <param name="text"></param>
        /// <param name="word"></param>
        /// <param name="digit"></param>
        /// <returns></returns>
        public static bool TryEndsWithDigit(string text, out string word, out string digit)
        {
            var ms = REG_NUM.Matches(text);
            if (ms.Count > 0)
            {
                var m = ms[ms.Count - 1];
                if (m.Success && m.Index + m.Length == text.Length)
                {
                    word = text.Substring(0, m.Index);
                    digit = text.Substring(m.Index);
                    return true;
                }
            }
            word = null;
            digit = null;
            return false;
        }
        public static bool TryIndexOfDigit(string text, out int digit_begin, out int digit_end, out string digit)
        {
            var ms = REG_NUM.Matches(text);
            if (ms.Count > 0)
            {
                var m = ms[ms.Count - 1];
                if (m.Success)
                {
                    digit_begin = m.Index;
                    digit_end = m.Index + m.Length;
                    digit = text.Substring(m.Index, m.Length);
                    return true;
                }
            }
            digit = null;
            digit_begin = -1;
            digit_end = -1;
            return false;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region ARRAY_AND_COLLECTIONS


        public static void ForEach<ST, T>(this IList<T> list, ST st, Action<ST, int, T> action)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    action(st, i, list[i]);
                }
            }
        }

        public delegate void ForEachLastAction(int index, bool last);
        public delegate void ForEachLastAction<ST>(ST st, int index, bool last);
        public delegate void ForEachLastAction<T, ST>(ST st, int index, T value, bool last);

        public static T[] SetArrayLength<T, ST>(this T[] src, ST st, int len, Func<ST, int, T> append)
        {
            if (src == null)
            {
                var dst = new T[len];
                for (int i = 0; i < len; i++)
                {
                    dst[i] = append(st, i);
                }
                return dst;
            }
            else if (len > src.Length)
            {
                var dst = new T[len];
                Array.Copy(src, dst, src.Length);
                for (int i = src.Length; i < len; i++)
                {
                    dst[i] = append(st, i);
                }
                return dst;
            }
            else if (len < src.Length)
            {
                var dst = new T[len];
                Array.Copy(src, dst, len);
                return dst;
            }
            else
            {
                return src;
            }
        }
        public static T[] SubArray<T>(this T[] src, int start)
        {
            return SubArray(src, start, src.Length - start);
        }
        public static T[] SubArray<T>(this T[] src, int start, int count)
        {
            var dst = new T[count];
            Array.Copy(src, start, dst, 0, count);
            return dst;
        }
        public static T[] ArrayCopy<T>(this T[] src)
        {
            var dst = new T[src.Length];
            Array.Copy(src, dst, src.Length);
            return dst;
        }
        public static T[] ArrayAppend<T>(this T[] src, params T[] append)
        {
            if (src == null)
            {
                var dst = new T[append.Length];
                Array.Copy(append, dst, append.Length);
                return dst;
            }
            else
            {
                var dst = new T[src.Length + append.Length];
                Array.Copy(src, dst, src.Length);
                Array.Copy(append, 0, dst, src.Length, append.Length);
                return dst;
            }
        }
        public static bool ArrayContains<T>(this T[] src, T value)
        {
            if (src != null)
            {
                foreach (var item in src)
                {
                    if (item.Equals(value)) return true;
                }
            }
            return false;
        }
        public static T[] ArrayRemove<T>(this T[] src, int index)
        {
            var dst = new T[src.Length - 1];
            Array.Copy(src, dst, index);
            Array.Copy(src, index + 1, dst, index, src.Length - index - 1);
            return dst;
        }
        public static T[] ArrayRemove<T>(this T[] src, T obj)
        {
            if (src.TryIndexOf(obj, out var index))
            {
                return ArrayRemove(src, index);
            }
            return src;
        }

        public static void ForEach2D(int w, int h, Action<int, int> action)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    action(x, y);
                }
            }
        }
        public static void ForEach3D<ST>(ST st, int xlen, int ylen, int zlen, Action<ST, int, int, int> action)
        {
            for (int x = 0; x < xlen; x++)
            {
                for (int y = 0; y < ylen; y++)
                {
                    for (int z = 0; z < zlen; z++)
                    {
                        action(st, x, y, z);
                    }
                }
            }
        }

        public static bool ForEach2D<ST>(ST st, int w, int h, BreakPredicate<ST, int, int> action)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (action(st, x, y)) { return true; }
                }
            }
            return false;
        }
        public static bool ForEach3D<ST>(ST st, int xlen, int ylen, int zlen, BreakPredicate<ST, int, int, int> action)
        {
            for (int x = 0; x < xlen; x++)
            {
                for (int y = 0; y < ylen; y++)
                {
                    for (int z = 0; z < zlen; z++)
                    {
                        if (action(st, x, y, z)) { return true; }
                    }
                }
            }
            return false;
        }

        public static void ForEach2D<ST>(ST st, int sx, int sy, int w, int h, int ax, int ay, Action<ST, int, int> action)
        {
            for (int x = sx; x < w; x += ax)
            {
                for (int y = sy; y < h; y += ay)
                {
                    action(st, x, y);
                }
            }
        }
        public static void ForEach3D<ST>(ST st, int sx, int sy, int sz, int xlen, int ylen, int zlen, int ax, int ay, int az, Action<ST, int, int, int> action)
        {
            for (int x = sx; x < xlen; x += ax)
            {
                for (int y = sy; y < ylen; y += ay)
                {
                    for (int z = sz; z < zlen; z += az)
                    {
                        action(st, x, y, z);
                    }
                }
            }
        }

        public static void InitArray2D<T, ST>(this T[,] array2d, ST st, Func<ST, int, int, T> action)
        {
            if (array2d != null)
            {
                int w = array2d.GetLength(0);
                int h = array2d.GetLength(1);
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        array2d[x, y] = action(st, x, y);
                    }
                }
            }
        }
        public static void InitArray3D<T, ST>(this T[,,] matrix3D, ST st, Func<ST, int, int, int, T> action)
        {
            if (matrix3D != null)
            {
                var xlen = matrix3D.GetLength(0);
                var ylen = matrix3D.GetLength(1);
                var zlen = matrix3D.GetLength(2);
                for (int x = 0; x < xlen; x++)
                {
                    for (int y = 0; y < ylen; y++)
                    {
                        for (int z = 0; z < zlen; z++)
                        {
                            matrix3D[x, y, z] = action(st, x, y, z);
                        }
                    }
                }
            }
        }

        public static void ForEachArray2D<T, ST>(this T[,] array2d, ST st, Action<ST, T, int, int> action)
        {
            if (array2d != null)
            {
                int w = array2d.GetLength(0);
                int h = array2d.GetLength(1);
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        action(st, array2d[x, y], x, y);
                    }
                }
            }
        }
        public static void ForEachArray3D<T, ST>(this T[,,] matrix3D, ST st, Action<ST, T, int, int, int> action)
        {
            if (matrix3D != null)
            {
                var xlen = matrix3D.GetLength(0);
                var ylen = matrix3D.GetLength(1);
                var zlen = matrix3D.GetLength(2);
                for (int x = 0; x < xlen; x++)
                {
                    for (int y = 0; y < ylen; y++)
                    {
                        for (int z = 0; z < zlen; z++)
                        {
                            action(st, matrix3D[x, y, z], x, y, z);
                        }
                    }
                }
            }
        }

        public static void ForEachArray2D<T, ST>(this T[,] array2d,
            ST st,
            int sx, int sy,
            int xlen, int ylen,
            int ax, int ay, Action<ST, T, int, int> action)
        {
            if (array2d != null)
            {
                for (int x = sx; x < xlen; x += ax)
                {
                    for (int y = sy; y < ylen; y += ay)
                    {
                        action(st, array2d[x, y], x, y);
                    }
                }
            }
        }
        public static void ForEachArray3D<T, ST>(this T[,,] matrix3D,
            ST st,
            int sx, int sy, int sz,
            int xlen, int ylen, int zlen,
            int ax, int ay, int az, Action<ST, T, int, int, int> action)
        {
            if (matrix3D != null)
            {
                for (int x = sx; x < xlen; x += ax)
                {
                    for (int y = sy; y < ylen; y += ay)
                    {
                        for (int z = sz; z < zlen; z += az)
                        {
                            action(st, matrix3D[x, y, z], x, y, z);
                        }
                    }
                }
            }
        }

        public static bool ForEachArray2D<T, ST>(this T[,] array2d, ST st, BreakPredicate<ST, T, int, int> action)
        {
            if (array2d != null)
            {
                int w = array2d.GetLength(0);
                int h = array2d.GetLength(1);
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (action(st, array2d[x, y], x, y))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static bool ForEachArray3D<T, ST>(this T[,,] matrix3D, ST st, BreakPredicate<ST, T, int, int, int> action)
        {
            if (matrix3D != null)
            {
                var xlen = matrix3D.GetLength(0);
                var ylen = matrix3D.GetLength(1);
                var zlen = matrix3D.GetLength(2);
                for (int x = 0; x < xlen; x++)
                {
                    for (int y = 0; y < ylen; y++)
                    {
                        for (int z = 0; z < zlen; z++)
                        {
                            if (action(st, matrix3D[x, y, z], x, y, z))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 将size按照slice切分
        /// </summary>
        /// <param name="size"></param>
        /// <param name="slice"></param>
        /// <param name="action"></param>
        public static void SliceArray2D<T, ST>(this T[,] array2d, ST st, int slice, Action<int, int> begin, Action<ST, T, int, int> action, Action<int, int> end)
        {
            var xlen = array2d.GetLength(0);
            var ylen = array2d.GetLength(1);
            int dbx = (xlen / slice);
            int dby = (ylen / slice);
            for (int bx = 0; bx <= dbx; bx++)
            {
                for (int by = 0; by <= dby; by++)
                {
                    var cx = bx * slice;
                    var cy = by * slice;
                    begin(cx, cy);
                    for (int x = cx; x < cx + slice && x < xlen; x++)
                    {
                        for (int y = cy; y < cy + slice && y < ylen; y++)
                        {
                            action(st, array2d[x, y], x, y);
                        }
                    }
                    end(cx, cy);
                }
            }
        }

        /// <summary>
        /// 将size按照slice切分
        /// </summary>
        /// <param name="size"></param>
        /// <param name="slice"></param>
        /// <param name="action"></param>
        public static void SliceArray3D<T, ST>(this T[,,] matrix3D, ST st, int slice, Func<int, int, int, Action<ST, T, int, int, int>> action)
        {
            var xlen = matrix3D.GetLength(0);
            var ylen = matrix3D.GetLength(1);
            var zlen = matrix3D.GetLength(2);
            int dbx = (xlen / slice);
            int dby = (ylen / slice);
            int dbz = (zlen / slice);
            for (int bx = 0; bx <= dbx; bx++)
            {
                for (int by = 0; by <= dby; by++)
                {
                    for (int bz = 0; bz <= dbz; bz++)
                    {
                        var cx = bx * slice;
                        var cy = by * slice;
                        var cz = bz * slice;
                        var _continue = action(cx, cy, cz);
                        for (int x = cx; x < cx + slice && x < xlen; x++)
                        {
                            for (int y = cy; y < cy + slice && y < ylen; y++)
                            {
                                for (int z = cz; z < cz + slice && z < zlen; z++)
                                {
                                    _continue(st, matrix3D[x, y, z], x, y, z);
                                }
                            }
                        }
                    }

                }
            }
        }

        public static IList<T> RemoveAll<T>(IList<T> src, ICollection<T> list)
        {
            if (list.Count > 0)
            {
                for (int i = src.Count - 1; i >= 0; i--)
                {
                    T e = src[i];
                    if (e != null && list.Contains(e))
                    {
                        src.RemoveAt(i);
                    }
                }
            }
            return src;
        }

        public static int ArrayTotalLength<T>(T[] src, params T[] append)
        {
            var len =
                (src != null ? src.Length : 0) +
                (append != null ? append.Length : 0);
            return len;
        }
        public static T[] ArrayLink<T>(T[] src, params T[] append)
        {
            var dst = new T[ArrayTotalLength<T>(src, append)];
            if (src != null) Array.Copy(src, 0, dst, 0, src.Length);
            if (append != null) Array.Copy(append, 0, dst, src.Length, append.Length);
            return dst;
        }
        public static int ArrayTotalLength<T>(T[] src, params T[][] append)
        {
            var len = src != null ? src.Length : 0;
            foreach (var a in append)
            {
                if (a == null) continue;
                len += a.Length;
            }
            return len;
        }
        public static T[] ArrayLink<T>(T[] src, params T[][] append)
        {
            var dst = new T[ArrayTotalLength<T>(src, append)];
            if (src != null) Array.Copy(src, 0, dst, 0, src.Length);
            var offset = src != null ? src.Length : 0;
            foreach (var a in append)
            {
                if (a == null) continue;
                Array.Copy(a, 0, dst, offset, a.Length);
                offset += a.Length;
            }
            return dst;
        }

        public static void ArrayCopy<T>(ICollection<T> src, Queue<T> dst)
        {
            foreach (T t in src)
            {
                dst.Enqueue(t);
            }
        }
        public static void ArrayCopy<T>(ICollection<T> src, ICollection<T> dst)
        {
            foreach (T t in src)
            {
                dst.Add(t);
            }
        }
        public static void ArrayCopy2D<T>(T[,] src, int sx, int sy, T[,] dst, int dx, int dy, int wc, int hc)
        {
            for (int x = 0; x < wc; x++)
            {
                for (int y = 0; y < hc; y++)
                {
                    dst[dx + x, dy + y] = src[sx + x, sy + y];
                }
            }
        }

        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }
        public static bool ListEqual<T>(IReadOnlyList<T> a1, IReadOnlyList<T> a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Count; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        public static bool ArraysEqual<TA, TB>(TA[] a1, TB[] a2, Func<TA, TB, bool> comparer)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer(a1[i], a2[i])) return false;
            }
            return true;
        }
        public static bool ListEqual<TA, TB>(IReadOnlyList<TA> a1, IReadOnlyList<TB> a2, Func<TA, TB, bool> comparer)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            for (int i = 0; i < a1.Count; i++)
            {
                if (!comparer(a1[i], a2[i])) return false;
            }
            return true;
        }

        public static Array ToArrayConvert(this IList list, Type elementType)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                array.SetValue(list[i], i);
            }
            return array;
        }

        public static int[] GetArrayRanges(this Array array)
        {
            Type type = array.GetType();
            int rank = type.GetArrayRank();
            int[] ranges = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                ranges[i] = array.GetLength(i);
            }
            return ranges;
        }
        public static int GetArrayTotalCount(this Array array)
        {
            int ret = 0;
            foreach (var k in array)
            {
                ret++;
            }
            return ret;
        }


        public static int[] GetArrayRankIndex(int[] ranks, int total_index)
        {
            int[] ret = new int[ranks.Length];
            if (ranks.Length == 1)
            {
                ret[0] = total_index;
                return ret;
            }
            if (ranks.Length == 2)
            {
                ret[0] = total_index / ranks[1];
                ret[1] = total_index % ranks[1];
                return ret;
            }
            if (ranks.Length == 3)
            {
                ret[0] = total_index / ranks[2] / ranks[1];
                ret[1] = total_index / ranks[2] % ranks[1];
                ret[2] = total_index % ranks[2];
                return ret;
            }
            if (ranks.Length == 4)
            {
                ret[0] = total_index / ranks[3] / ranks[2] / ranks[1];
                ret[1] = total_index / ranks[3] / ranks[2] % ranks[1];
                ret[2] = total_index / ranks[3] % ranks[2];
                ret[3] = total_index % ranks[3];
                return ret;
            }
            return GetArrayIndex(ranks, total_index);
        }

        public static int GetArrayTotalIndex(int[] ranks, params int[] indices)
        {
            int total_index = 0;
            if (ranks.Length == 1)
            {
                total_index = indices[0];
                return total_index;
            }
            if (ranks.Length == 2)
            {
                total_index += indices[0] * ranks[1];
                total_index += indices[1];
                return total_index;
            }
            if (ranks.Length == 3)
            {
                total_index += indices[0] * ranks[2] * ranks[1];
                total_index += indices[1] * ranks[2];
                total_index += indices[2];
                return total_index;
            }
            if (ranks.Length == 4)
            {
                total_index += indices[0] * ranks[3] * ranks[2] * ranks[1];
                total_index += indices[1] * ranks[3] * ranks[2];
                total_index += indices[2] * ranks[3];
                total_index += indices[3];
                return total_index;
            }
            return GetArrayIndex(ranks, indices);
        }


        private static int[] GetArrayIndex(int[] arrayStruct, int index)
        {
            int[] valueArray = new int[arrayStruct.Length];
            int[] tempArray = new int[arrayStruct.Length];

            int[] outIndex = new int[arrayStruct.Length];

            valueArray[arrayStruct.Length - 1] = 1;
            for (int i = arrayStruct.Length - 1 - 1; i >= 0; --i)
            {
                valueArray[i] = arrayStruct[i + 1] * valueArray[i + 1];
            }

            if (index < 0 || index > valueArray[0] * arrayStruct[0])
                throw new Exception(" Array Out of index " + index);

            outIndex[0] = index / valueArray[0];
            tempArray[0] = outIndex[0] * valueArray[0];

            for (int i = 1; i < arrayStruct.Length; ++i)
            {
                outIndex[i] = (index - tempArray[i - 1]) / valueArray[i];
                tempArray[i] = tempArray[i - 1] + outIndex[i] * valueArray[i];
            }

            return outIndex;
        }

        private static int GetArrayIndex(int[] arrayStruct, int[] arrayIndex)
        {
            int index = 0;

            int[] valueArray = new int[arrayStruct.Length];

            valueArray[arrayStruct.Length - 1] = 1;
            for (int i = arrayStruct.Length - 1 - 1; i >= 0; --i)
            {
                valueArray[i] = arrayStruct[i + 1] * valueArray[i + 1];
            }

            for (int i = 0; i < arrayStruct.Length; ++i)
            {
                index += valueArray[i] * arrayIndex[i];
            }

            return index;
        }

        public static T GetMinOrMax<T>(T[] array, int index)
        {
            if (array.Length > 0)
            {
                if (index < 0)
                {
                    return array[0];
                }
                if (index >= array.Length)
                {
                    return array[array.Length - 1];
                }
                return array[index];
            }
            return default(T);
        }

        public static T GetMinOrMax<T>(IList<T> array, int index)
        {
            if (array.Count > 0)
            {
                if (index < 0)
                {
                    return array[0];
                }
                if (index >= array.Count)
                {
                    return array[array.Count - 1];
                }
                return array[index];
            }
            return default(T);
        }



        public delegate bool TestRemove<T>(T data);

        public static void RemoveAll<T>(LinkedList<T> list, TestRemove<T> test)
        {
            if (list.Count > 0)
            {
                List<LinkedListNode<T>> removed = null;
                for (LinkedListNode<T> it = list.Last; it != null; it = it.Previous)
                {
                    T t = it.Value;
                    if (test(t))
                    {
                        if (removed == null)
                        {
                            removed = new List<LinkedListNode<T>>(2);
                        }
                        removed.Add(it);
                    }
                }
                if (removed != null)
                {
                    foreach (LinkedListNode<T> it in removed)
                    {
                        list.Remove(it);
                    }
                }
            }
        }

        public static void RemoveAll<T>(ICollection<T> list, TestRemove<T> test)
        {
            if (list.Count > 0)
            {
                List<T> removed = null;
                foreach (T t in list)
                {
                    if (test(t))
                    {
                        if (removed == null)
                        {
                            removed = new List<T>(2);
                        }
                        removed.Add(t);
                    }
                }
                if (removed != null)
                {
                    for (int i = removed.Count - 1; i >= 0; --i)
                    {
                        list.Remove(removed[i]);
                    }
                }
            }
        }

        public static void RemoveAll<T>(IList<T> list, TestRemove<T> test)
        {
            if (list.Count > 0)
            {
                List<T> removed = null;
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    T t = list[i];
                    if (test(t))
                    {
                        if (removed == null)
                        {
                            removed = new List<T>(2);
                        }
                        removed.Add(t);
                    }
                }
                if (removed != null)
                {
                    for (int i = removed.Count - 1; i >= 0; --i)
                    {
                        list.Remove(removed[i]);
                    }
                }
            }
        }
        public static B[] Convert1D<A, B>(this A[] src, Func<int, A, B> converter)
        {
            var dst = new B[src.Length];
            for (int i = 0; i < src.Length; ++i)
            {
                dst[i] = converter(i, src[i]);
            }
            return dst;
        }
        public static B[,] Convert2D<A, B>(this A[,] src, Func<int, int, A, B> converter)
        {
            var w = src.GetLength(0);
            var h = src.GetLength(1);
            var dst = new B[w, h];
            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    dst[x, y] = converter(x, y, src[x, y]);
                }
            }
            return dst;
        }
        public static ArrayList<B> ConvertAll<A, B>(this IEnumerable src, Func<A, B> converter)
        {
            var ret = new ArrayList<B>();
            ConvertTo(src, ret, converter);
            return ret;
        }
        public static ArrayList<B> ConvertAll<A, B>(this ICollection src, Func<A, B> converter)
        {
            var ret = new ArrayList<B>(src.Count);
            ConvertTo(src, ret, converter);
            return ret;
        }
        public static ArrayList<B> ConvertAll<A, B>(this IEnumerable<A> src, Func<A, B> converter)
        {
            var ret = new ArrayList<B>();
            ConvertTo(src, ret, converter);
            return ret;
        }
        public static ArrayList<B> ConvertAll<A, B>(this ICollection<A> src, Func<A, B> converter)
        {
            var ret = new ArrayList<B>(src.Count);
            ConvertTo(src, ret, converter);
            return ret;
        }
        public static HashMap<K2, V2> ConvertAll<K1, V1, K2, V2>(
            this IDictionary<K1, V1> src,
            Func<KeyValuePair<K1, V1>, KeyValuePair<K2, V2>> converter)
        {
            var dst = new HashMap<K2, V2>(src.Count);
            ConvertTo(src, dst, converter);
            return dst;
        }

        public static void ConvertTo<A, B>(this IEnumerable src, ICollection<B> dst, Func<A, B> converter)
        {
            foreach (object obj in src)
            {
                dst.Add(converter((A)obj));
            }
        }
        public static void ConvertTo<K1, V1, K2, V2>(
            this IDictionary<K1, V1> src,
            IDictionary<K2, V2> dst,
            Func<KeyValuePair<K1, V1>, KeyValuePair<K2, V2>> converter)
        {
            foreach (var e in src)
            {
                dst.Add(converter(e));
            }
        }


        public static ArrayList<T> ToGenericList<T>(this IEnumerable list, int capacity = 0)
        {
            var ret = (capacity > 0) ? new ArrayList<T>(capacity) : new ArrayList<T>();
            foreach (object obj in list)
            {
                ret.Add((T)obj);
            }
            return ret;
        }

        public static ArrayList<T> ArrayGetComponents<T>(this ICollection list)
        {
            if (list == null) return null;
            var ret = new ArrayList<T>(list.Count);
            foreach (var e in list)
            {
                if (e is T t)
                {
                    ret.Add(t);
                }
            }
            return ret;
        }
        public static object[] ToArray(this Array list)
        {
            var ret = new object[list.Length];
            list.CopyTo(ret, 0);
            return ret;
        }

        public static T[] ToArray<T>(this ICollection list)
        {
            T[] ret = new T[list.Count];
            list.CopyTo(ret, 0);
            return ret;
        }

        public static T[] ToArray<T>(this ICollection<T> list)
        {
            if (list.GetType().IsArray)
            {
                return (T[])list;
            }
            T[] ret = new T[list.Count];
            list.CopyTo(ret, 0);
            return ret;
        }
        public static Array ToArray(this ICollection list, Type etype)
        {
            Array ret = Array.CreateInstance(etype, list.Count);
            list.CopyTo(ret, 0);
            return ret;
        }

        public static void SetListValueAutoExpand<T>(IList<T> list, int index, T value)
        {
            if (list.Count - 1 < index)
            {
                SetListSize(list, index + 1);
            }
            list[index] = value;
        }

        public static void SetListSize<T>(IList<T> list, int length, Func<int, T> create = null)
        {
            int d = length - list.Count;
            if (d < 0)
            {
                RemoveRange(list, length, -d);
            }
            else if (d > 0)
            {
                for (int i = 0; i < d; i++)
                {
                    if (create == null) list.Add(default(T));
                    else list.Add(create(list.Count));
                }
            }
        }
        public static void RemoveRange<T>(IList<T> list, int index, int count)
        {
            for (int i = index + count - 1; i >= index; --i)
            {
                list.RemoveAt(i);
            }
        }

        public static void SetListLength<T>(IList<T> list, int length, Func<int, T> create = null)
        {
            int d = length - list.Count;
            if (d < 0)
            {
                RemoveRange(list, length, -d);
            }
            else if (d > 0)
            {
                for (int i = 0; i < d; i++)
                {
                    if (create != null)
                    {
                        list.Add(create(list.Count));
                    }
                    else
                    {
                        list.Add(default(T));
                    }
                }
            }
        }
        public static void SetListLength(IList list, int length, Func<int, object> create = null)
        {
            int d = length - list.Count;
            if (d < 0)
            {
                RemoveRange(list, length, -d);
            }
            else if (d > 0)
            {
                for (int i = 0; i < d; i++)
                {
                    if (create != null)
                    {
                        list.Add(create(list.Count));
                    }
                    else
                    {
                        list.Add(null);
                    }
                }
            }
        }
        public static void RemoveRange(IList list, int index, int count)
        {
            for (int i = index + count - 1; i >= index; --i)
            {
                list.RemoveAt(i);
            }
        }


        public static void SwapInList<T>(IList<T> list, int i, int j)
        {
            T oi = list[i];
            list[i] = list[j];
            list[j] = oi;
        }

        public static void SwapInArray<T>(T[] array, int i, int j)
        {
            T oi = array[i];
            array[i] = array[j];
            array[j] = oi;
        }

        public static void AddRange<D, S>(IList<D> list, IList<S> add) where S : D
        {
            //GC
            /* foreach (var a in add)
               {
                   list.Add(a);
               }
            */

            for (int i = 0; i < add.Count; i++)
            {
                list.Add(add[i]);
            }

        }
        public static void AddRange<D, S>(this IList<D> list, Span<S> add) where S : D
        {
            /*   foreach (var a in add)
               {
                   list.Add(a);
               }*/

            for (int i = 0; i < add.Length; i++)
            {
                list.Add(add[i]);
            }
        }

        public static void SyncToDstList<LIST>(IEnumerable src, LIST dst, Func<object, object, bool> equal, Action<LIST, object> doAdd, Action<LIST, object> doRemove) where LIST : IEnumerable
        {
            var removing = new List<object>();
            var adding = new List<object>();
            {
                {
                    foreach (var s in src) { adding.Add(s); }
                    foreach (var d in dst)
                    {
                        //预添加列表里是否有Dst元素//
                        var exist = adding.Find((add) => { return equal(add, d); });
                        if (exist != null) { adding.Remove(exist); }
                        else { removing.Add(d); }
                    }
                    foreach (var remove in removing)
                    {
                        doRemove(dst, remove);
                    }
                    foreach (var add in adding)
                    {
                        doAdd(dst, add);
                    }
                }
            }
        }
        public static void SyncToDstList<SRC, DST, DST_LIST>(IEnumerable<SRC> src, DST_LIST dst, Func<SRC, DST, bool> equal, Action<DST_LIST, SRC> doAdd, Action<DST_LIST, DST> doRemove) where DST_LIST : IEnumerable
        {
            SyncToDstList<DST_LIST>(src, dst,
                (object a, object b) => { return equal((SRC)a, (DST)b); },
                (DST_LIST list, object add) => { doAdd(list, (SRC)add); },
                (DST_LIST list, object remove) => { doRemove(list, (DST)remove); });
        }
        public static void SyncToDstList<SRC, DST>(IEnumerable<SRC> src, ICollection<DST> dst, Func<SRC, DST, bool> equal, Action<ICollection<DST>, SRC> doAdd, Action<ICollection<DST>, DST> doRemove)
        {
            SyncToDstList<ICollection<DST>>(src, dst,
                (object a, object b) => { return equal((SRC)a, (DST)b); },
                (ICollection<DST> list, object add) => { doAdd(list, (SRC)add); },
                (ICollection<DST> list, object remove) => { doRemove(list, (DST)remove); });
        }
        public static void SyncToDstList<T>(IList<T> src, IList<T> dst)
        {
            SyncToDstList(src, dst, (a, b) => { return a.Equals(b); }, (dlist, e) => { dlist.Add(e); }, (dlist, e) => { dlist.Remove(e); });
        }

        public static void AddRange<T>(this ICollection<T> src, IEnumerable<T> add)
        {
            foreach (var e in add)
            {
                src.Add(e);
            }
        }
        public static int AddRange<T>(this ICollection<T> src, IList<T> add, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pos = index + i;
                if (pos < add.Count)
                {
                    src.Add(add[pos]);
                }
                else
                {
                    return i;
                }
            }
            return count;
        }

        public static T[][] SplitArray<T>(this T[] src, int everyCount)
        {
            ArrayList<T> temp = new ArrayList<T>(src);
            ArrayList<T[]> ret = new ArrayList<T[]>();
            while (temp.Count > 0)
            {
                var now = new ArrayList<T>(everyCount);
                var added = now.AddRange(temp, 0, everyCount);
                if (added > 0)
                {
                    temp.RemoveRange(0, added);
                    ret.Add(now.ToArray());
                }
                else
                {
                    break;
                }
            }
            return ret.ToArray();
        }
        public static ArrayList<T>[] SplitList<T>(this IList<T> src, int everyCount)
        {
            ArrayList<T> temp = new ArrayList<T>(src);
            ArrayList<ArrayList<T>> ret = new ArrayList<ArrayList<T>>();
            while (temp.Count > 0)
            {
                var now = new ArrayList<T>(everyCount);
                var added = now.AddRange(temp, 0, everyCount);
                if (added > 0)
                {
                    temp.RemoveRange(0, added);
                    ret.Add(now);
                }
                else
                {
                    break;
                }
            }
            return ret.ToArray();
        }

        public static void AddRangeNotNull<T>(this IList<T> src, IEnumerable<T> dst) where T : class
        {
            if (dst != null)
            {
                foreach (var e in dst)
                {
                    if (e != null)
                    {
                        src.Add(e);
                    }
                }
            }
        }


        public static bool TryIndexOf<T>(this T[] str, T ch, out int index, int start, int count)
        {
            index = Array.IndexOf(str, ch, start, count);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this T[] str, T ch, out int index, int start, int count)
        {
            index = Array.LastIndexOf(str, ch, start, count);
            return index >= 0;
        }
        public static bool TryIndexOf<T>(this T[] str, T ch, out int index, int start)
        {
            index = Array.IndexOf(str, ch, start);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this T[] str, T ch, out int index, int start)
        {
            index = Array.LastIndexOf(str, ch, start);
            return index >= 0;
        }
        public static bool TryIndexOf<T>(this T[] str, T ch, out int index)
        {
            index = Array.IndexOf(str, ch);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this T[] str, T ch, out int index)
        {
            index = Array.LastIndexOf(str, ch);
            return index >= 0;
        }

        public static bool TryFind<T>(this T[] str, Predicate<T> ch, out T value)
        {
            if (str != null && str.Length > 0)
            {
                var ret = false;
                value = Array.Find(str, v =>
                {
                    if (ch(v))
                    {
                        ret = true;
                        return true;
                    }
                    return false;
                });
                return ret;
            }
            else { }
            value = default;
            return false;
        }

        public static bool TryFindAs<T, D>(this T[] str, Predicate<D> ch, out D value)
            where T : class
            where D : class
        {
            var ret = false;
            value = Array.Find<T>(str, v =>
            {
                if ((v is D d) && ch(d))
                {
                    ret = true;
                    return true;
                }
                return false;
            }) as D;
            return ret;
        }
        public static bool TryFindTypes<T, D>(this T[] str, Predicate<D> ch, out D[] value)
            where T : class
            where D : class
        {
            var lit = new List<D>();
            foreach (var v in str)
            {
                if ((v is D d) && ch(d))
                {
                    lit.Add(d);
                }
            }
            value = lit.ToArray();
            return lit.Count > 0;
        }

        public static bool TryIndexOf<T>(this List<T> str, T ch, out int index, int start, int count)
        {
            index = str.IndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this List<T> str, T ch, out int index, int start, int count)
        {
            index = str.LastIndexOf(ch, start, count);
            return index >= 0;
        }
        public static bool TryIndexOf<T>(this List<T> str, T ch, out int index, int start)
        {
            index = str.IndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this List<T> str, T ch, out int index, int start)
        {
            index = str.LastIndexOf(ch, start);
            return index >= 0;
        }
        public static bool TryIndexOf<T>(this List<T> str, T ch, out int index)
        {
            index = str.IndexOf(ch);
            return index >= 0;
        }
        public static bool TryLastIndexOf<T>(this List<T> str, T ch, out int index)
        {
            index = str.LastIndexOf(ch);
            return index >= 0;
        }

        public static bool TryFind<T>(this List<T> str, Predicate<T> ch, out T value)
        {
            var ret = false;
            value = str.Find(v =>
            {
                if (ch(v))
                {
                    ret = true;
                    return true;
                }
                return false;
            });
            return ret;
        }
        public static bool TryFindIndex<T>(this List<T> str, Predicate<T> ch, out T value, out int index)
        {
            value = default(T);
            index = -1;
            for (int i = 0; i < str.Count; i++)
            {
                var v = str[i];
                if (ch(v))
                {
                    value = v;
                    index = i;
                    return true;
                }
            }
            return false;
        }
        public static bool TryFindIndex<T, ST>(this List<T> str, ST st, ForEachPredicate<ST, T> ch, out T value, out int index)
        {
            value = default(T);
            index = -1;
            for (int i = 0; i < str.Count; i++)
            {
                var v = str[i];
                if (ch(st, v))
                {
                    value = v;
                    index = i;
                    return true;
                }
            }
            return false;
        }


        public static T[] ArrayExcludeNull<T>(this T[] src) where T : class
        {
            var auto = new List<T>(src.Length);
            {
                for (int i = 0; i < src.Length; i++)
                {
                    if (src[i] != null)
                    {
                        auto.Add(src[i]);
                    }
                }
                return auto.ToArray();
            }
        }

        /// <summary>
        /// 移除所有相同的元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="equals"></param>
        public static void RemoveAllDuplicate<T>(this ICollection<T> collection, Func<T, T, bool> equals)
        {
            var exists = new List<T>();
            foreach (var item in collection.ToArray())
            {
                foreach (var e in exists)
                {
                    if (equals(e, item))
                    {
                        collection.Remove(item);
                        break;
                    }
                }
                exists.Add(item);
            }
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------

        public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            try
            {
                var ret = System.Enum.Parse(typeof(TEnum), value);
                if (ret != null)
                {
                    result = (TEnum)ret;
                    return true;
                }
            }
            catch { }
            result = default(TEnum);
            return false;
        }
        public static bool TryParseEnum<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
            try
            {
                var ret = System.Enum.Parse(typeof(TEnum), value, ignoreCase);
                if (ret != null)
                {
                    result = (TEnum)ret;
                    return true;
                }
            }
            catch { }
            result = default(TEnum);
            return false;
        }
        public static bool TryParseEnum(Type type, string value, out object result)
        {
            try
            {
                var ret = System.Enum.Parse(type, value);
                if (ret != null)
                {
                    result = ret;
                    return true;
                }
            }
            catch { }
            result = null;
            return false;
        }
        public static bool TryParseEnum(Type type, string value, bool ignoreCase, out object result)
        {
            try
            {
                var ret = System.Enum.Parse(type, value, ignoreCase);
                if (ret != null)
                {
                    result = ret;
                    return true;
                }
            }
            catch { }
            result = null;
            return false;
        }

        public static string GetObjectFieldsInfo<T>(T t)
        {
            if (t == null)
            {
                return "";
            }
            string reValue = "";
            int tLen = 8;

            BindingFlags param = param = BindingFlags.Instance | BindingFlags.Public;
            /*
            PropertyInfo[] props = t.GetType().GetProperties(param);
            if(props.Length == 0)
            {
                return "";
            }
            foreach(PropertyInfo prop in props)
            {
                string name = prop.Name;
                object value = prop.GetValue(t, null);
                string desc = ((DescriptionAttribute)Attribute.GetCustomAttribute(prop, typeof(DescriptionAttribute)))?.Description;
                if(prop.PropertyType.IsValueType || prop.PropertyType.Name.StartsWith("String"))
                {
                    Console.WriteLine(string.Format("{0}:{1}:{2}", name, value, desc));
                }
                else
                {
                    PrintProperties(value);
                }
            }
            */

            int maxKeyLen = 0;
            int maxValLen = 0;
            List<string> keyList = new List<string>();
            HashMap<string, string> keyValueMap = new HashMap<string, string>();
            HashMap<string, string> keyDescMap = new HashMap<string, string>();

            FieldInfo[] fields = t.GetType().GetFields(param);
            foreach (FieldInfo field in fields)
            {
                var objValue = field.GetValue(t);
                if (objValue == null)
                {
                    continue;
                }
                var key = field.Name;
                var value = objValue.ToString();
                string desc = ((DescAttribute)Attribute.GetCustomAttribute(field, typeof(DescAttribute)))?.Desc;
                keyList.Add(key);
                keyValueMap[key] = value;
                keyDescMap[key] = desc;

                maxKeyLen = Math.Max(maxKeyLen, key.Length);
                maxValLen = Math.Max(maxValLen, value.Length);
            }
            keyList.Sort();
            maxKeyLen = (maxKeyLen + tLen - 1) / tLen * tLen;
            maxValLen = (maxValLen + tLen - 1) / tLen * tLen;
            for (var i = 0; i < keyList.Count; i++)
            {
                if (i > 0)
                {
                    reValue += "\n";
                }
                var key = keyList[i];
                var value = keyValueMap[key];
                var desc = keyDescMap[key];
                //方法1
                var str = string.Format("{0}{1}{2}", key.PadRight(maxKeyLen), value.PadRight(maxValLen), desc);
                //方法2
                //var str = string.Format("{0,-" + maxKeyLen + "}{1,-" + maxValLen + "}{2}", key, value, desc);
                //方法3
                /*
                var keyTabCount = Math.Ceiling((double)(maxKeyLen - key.Length) / tLen);
                var keyTabStr = "";
                while (keyTabCount > 0)
                {
                    keyTabStr += "\t";
                    keyTabCount--;
                }
                var valTabCount = Math.Ceiling((double)(maxValLen - value.Length) / tLen);
                var valTabStr = "";
                while (valTabCount > 0)
                {
                    valTabStr += "\t";
                    valTabCount--;
                }
                var str = string.Format("{0}{1}{2}{3}{4}", key, keyTabStr, value, valTabStr, desc);
                */
                reValue += str;
            }
            return reValue;
        }

        //返回堆栈信息
        public static string GetStackInfo()
        {
            StackTrace st = new StackTrace(true);
            var frames = st.GetFrames();
            string strRe = "";
            for (var i = 0; i < frames.Length; i++)
            {
                if (i > 0)
                {
                    strRe += "\n";
                }
                strRe += i + "\t" + frames[i].GetFileName() + ":" + frames[i].GetFileLineNumber();
            }
            return strRe;
        }
    }

}
