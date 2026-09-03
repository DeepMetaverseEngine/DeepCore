using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace DeepCore
{
    public static class Parser
    {
        //--------------------------------------------------------------------------------------------------------------------
        #region Instance
        internal static Logger log = new LazyLogger(nameof(Parser));
        private static ArrayList<TypeParserAdapter> s_TypeParsers = new ArrayList<TypeParserAdapter>();
        private static HashMap<Type, TypeParserAdapter> s_TypeParsersMap = new HashMap<Type, TypeParserAdapter>();
        public static ParserAdapter Adapter { get; set; } = new BaseParserAdapter();
        public static string FloatFormat { get; set; } = "";
        public static string DateTimeFormat = "yyyyMMdd_HHmmss_fff";
        public static CultureInfo CultureInfo = CultureInfo.InvariantCulture;
        public static void RegistParser(TypeParserAdapter parser)
        {
            if (parser.IsAssignableFrom)
            {
                s_TypeParsers.Add(parser);
            }
            else
            {
                s_TypeParsersMap.Add(parser.ParserType, parser);
            }
        }
        public static TypeParserAdapter GetTypeAdapter(Type type)
        {
            if (s_TypeParsersMap.TryGetValue(type, out var ret))
            {
                return ret;
            }
            else
            {
                foreach (var p in s_TypeParsers)
                {
                    if (p.Accept(type)) { return p; }
                }
            }
            return null;
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------

        public static string ObjectToString(object obj)
        {
            return Adapter.ToString(obj);
        }
        public static bool TryStringToObject(string text, Type type, out object value)
        {
            return Adapter.TryParse(text, type, out value);
        }
        public static bool TryStringToObject<T>(string text, out T value)
        {
            try
            {
                if (Adapter.TryParse(text, typeof(T), out var ret))
                {
                    value = (T)ret;
                    return true;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            value = default(T);
            return false;
        }
        public static object StringToObject(string text, Type type)
        {
            if (Adapter.TryParse(text, type, out var ret))
            {
                return ret;
            }
            return null;
        }
        public static T StringToObject<T>(string text)
        {
            try
            {
                if (Adapter.TryParse(text, typeof(T), out var ret))
                {
                    return (T)ret;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return default(T);
        }
        //--------------------------------------------------------------------------------------------------------------------
        public static bool ParseBool(string text) => bool.Parse(text);
        public static sbyte ParseSByte(string text, NumberStyles style = NumberStyles.Any) => sbyte.Parse(text, style, Parser.CultureInfo);
        public static short ParseShort(string text, NumberStyles style = NumberStyles.Any) => short.Parse(text, style, Parser.CultureInfo);
        public static int ParseInt(string text, NumberStyles style = NumberStyles.Any) => int.Parse(text, style, Parser.CultureInfo);
        public static long ParseLong(string text, NumberStyles style = NumberStyles.Any) => long.Parse(text, style, Parser.CultureInfo);
        //--------------------------------------------------------------------------------------------------------------------
        public static byte ParseByte(string text, NumberStyles style = NumberStyles.Any) => byte.Parse(text, style, Parser.CultureInfo);
        public static ushort ParseUShort(string text, NumberStyles style = NumberStyles.Any) => ushort.Parse(text, style, Parser.CultureInfo);
        public static uint ParseUInt(string text, NumberStyles style = NumberStyles.Any) => uint.Parse(text, style, Parser.CultureInfo);
        public static ulong ParseULong(string text, NumberStyles style = NumberStyles.Any) => ulong.Parse(text, style, Parser.CultureInfo);
        //--------------------------------------------------------------------------------------------------------------------
        public static float ParseFloat(string text, NumberStyles style = NumberStyles.Any) => float.Parse(text, style, Parser.CultureInfo);
        public static double ParseDouble(string text, NumberStyles style = NumberStyles.Any) => double.Parse(text, style, Parser.CultureInfo);
        public static decimal ParseDecimal(string text, NumberStyles style = NumberStyles.Any) => decimal.Parse(text, style, Parser.CultureInfo);
        public static BigInteger ParseBigInteger(string text, NumberStyles style = NumberStyles.Any) => BigInteger.Parse(text, style, Parser.CultureInfo);
        //--------------------------------------------------------------------------------------------------------------------
        public static bool TryParseBool(string text, out bool result) => bool.TryParse(text, out result);
        public static bool TryParseSByte(string text, out sbyte result, NumberStyles style = NumberStyles.Any) => sbyte.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseShort(string text, out short result, NumberStyles style = NumberStyles.Any) => short.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseInt(string text, out int result, NumberStyles style = NumberStyles.Any) => int.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseLong(string text, out long result, NumberStyles style = NumberStyles.Any) => long.TryParse(text, style, Parser.CultureInfo, out result);
        //--------------------------------------------------------------------------------------------------------------------
        public static bool TryParseByte(string text, out byte result, NumberStyles style = NumberStyles.Any) => byte.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseUShort(string text, out ushort result, NumberStyles style = NumberStyles.Any) => ushort.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseUInt(string text, out uint result, NumberStyles style = NumberStyles.Any) => uint.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseULong(string text, out ulong result, NumberStyles style = NumberStyles.Any) => ulong.TryParse(text, style, Parser.CultureInfo, out result);
        //--------------------------------------------------------------------------------------------------------------------
        public static bool TryParseFloat(string text, out float result, NumberStyles style = NumberStyles.Any) => float.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseDouble(string text, out double result, NumberStyles style = NumberStyles.Any) => double.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseDecimal(string text, out decimal result, NumberStyles style = NumberStyles.Any) => decimal.TryParse(text, style, Parser.CultureInfo, out result);
        public static bool TryParseBigInteger(string text, out BigInteger result, NumberStyles style = NumberStyles.Any) => BigInteger.TryParse(text, style, Parser.CultureInfo, out result);
        //--------------------------------------------------------------------------------------------------------------------
    }

    //--------------------------------------------------------------------------------------------------------------------

    public interface ParserAdapter
    {
        bool TryParse(string text, Type type, out object value);
        string ToString(object obj);
    }

    //--------------------------------------------------------------------------------------------------------------------

    public interface TypeParserAdapter
    {
        /// <summary>
        /// 支持的的类型
        /// </summary>
        Type ParserType { get; }
        /// <summary>
        /// 是否兼容将基类解析为子类。
        /// </summary>
        bool IsAssignableFrom { get; }
        bool Accept(Type type);
        bool TryParse(string text, out object value);
        string ToString(object obj);
    }
    public abstract class TypeParserAdapter<T> : TypeParserAdapter
    {
        /// <summary>
        /// 支持的的类型
        /// </summary>
        public Type ParserType { get => typeof(T); }
        /// <summary>
        /// 是否兼容将基类解析为子类。
        /// </summary>
        public bool IsAssignableFrom { get => false; }
        public bool Accept(Type type) => type == typeof(T);
        public bool TryParse(string text, out object value)
        {
            var ret = this.TryParse(text, out T tvalue);
            value = tvalue;
            return ret;
        }
        public string ToString(object obj) => this.ToString((T)obj);
        public abstract bool TryParse(string text, out T value);
        public abstract string ToString(T obj);
    }
    //--------------------------------------------------------------------------------------------------------------------

    public class BaseParserAdapter : ParserAdapter
    {
        //--------------------------------------------------------------------------------------------------------------------
        public virtual bool TryParse(string text, Type type, out object ret)
        {
            try
            {
                if (typeof(string) == (type))
                {
                    ret = text;
                    return true;
                }
                else if (type.IsPrimitive)
                {
                    var style = NumberStyles.Number;
                    if (text.StartsWith("0x", CUtils.StringComparisonIgnoreCase))
                    {
                        style = NumberStyles.HexNumber;
                        text = text.Substring(2);
                    }
                    if (type == (typeof(int)))
                    {
                        var rst = int.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(float)))
                    {
                        var rst = float.TryParse(text, NumberStyles.Any, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(bool)))
                    {
                        if (bool.TryParse(text, out var retBool))
                        {
                            ret = retBool;
                            return true;
                        }
                        if (int.TryParse(text, style, Parser.CultureInfo, out var retInt))
                        {
                            ret = retInt != 0;
                            return true;
                        }
                        ret = false;
                        return false;
                    }
                    else if (type == (typeof(long)))
                    {
                        var rst = long.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(short)))
                    {
                        var rst = short.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(double)))
                    {
                        var rst = double.TryParse(text, NumberStyles.Any, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(uint)))
                    {
                        var rst = uint.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(ulong)))
                    {
                        var rst = ulong.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(ushort)))
                    {
                        var rst = ushort.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(byte)))
                    {
                        var rst = byte.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(sbyte)))
                    {
                        var rst = sbyte.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = retInt;
                        return rst;
                    }
                    else if (type == (typeof(char)))
                    {
                        var rst = int.TryParse(text, style, Parser.CultureInfo, out var retInt);
                        ret = Convert.ToChar(retInt);
                        return rst;
                    }
                    else
                    {
                        ret = null;
                        return false;
                    }
                }

                else if (typeof(byte[]) == (type))
                {
                    ret = CUtils.HexToBin(text);
                    return true;
                }
                else if (type == (typeof(decimal)))
                {
                    var rst = decimal.TryParse(text, NumberStyles.Any, Parser.CultureInfo, out var retInt);
                    ret = retInt;
                    return rst;
                }
                else if (type.IsEnum)
                {
                    try
                    {
                        try
                        {
                            if (text.IsNullOrEmpty())
                            {
                                ret = null;
                                return false;
                            }
                            if (Enum.TryParse(type, text, true, out var _ret))
                            {
                                ret = _ret;
                                return true;
                            }
                            else
                            {
                                var underType = Enum.GetUnderlyingType(type);
                                var underValue = Convert.ChangeType(text, underType);
                                var name = Enum.GetName(type, underValue);
                                ret = Enum.Parse(type, name, true);
                                return true;
                            }
                        }
                        catch
                        {
                            var underType = Enum.GetUnderlyingType(type);
                            var underValue = Convert.ChangeType(text, underType);
                            var name = Enum.GetName(type, underValue);
                            ret = Enum.Parse(type, name, true);
                            return true;
                        }
                    }
                    catch
                    {
                        ret = null;
                        return false;
                    }
                }
                else if (type == (typeof(DateTime)))
                {
                    var rst = DateTime.TryParseExact(text, Parser.DateTimeFormat, Parser.CultureInfo, DateTimeStyles.None, out var retDT);
                    ret = retDT;
                    return rst;
                }
                else if (type == (typeof(TimeSpan)))
                {
                    var rst = long.TryParse(text, out var ticks);
                    ret = TimeSpan.FromTicks(ticks);
                    return rst;
                }
                else if (string.IsNullOrEmpty(text))
                {
                    ret = null;
                    return false;
                }
                else
                {
                    TypeParserAdapter parser = Parser.GetTypeAdapter(type);
                    if (parser != null)
                    {
                        return parser.TryParse(text, out ret);
                    }
                    else if (type.IsArray && type.HasElementType)
                    {
                        var rst = DecodeArray(text, type.GetElementType(), type.GetArrayRank(), out var retArray);
                        ret = retArray;
                        return rst;
                    }
                    else if (type.GetInterface(typeof(IList).Name) != null)
                    {
                        var rst = DecodeList(text, type, out var retList);
                        ret = retList;
                        return rst;
                    }
                    else if (type.GetInterface(typeof(IDictionary).Name) != null)
                    {
                        var rst = DecodeDictionary(text, type, out var retMap);
                        ret = retMap;
                        return rst;
                    }
                    else if (DecodeFields(text, type, out var obj))
                    {
                        ret = obj;
                        return true;
                    }
                    else
                    {
                        ret = null;
                        return false;
                    }
                }
            }
            catch (Exception err)
            {
                Parser.log.Error(err.Message, err);
                ret = null;
                return false;
            }
        }
        public virtual string ToString(object obj)
        {
            try
            {
                if (obj == null)
                {
                    return string.Empty;
                }
                else if (obj is string)
                {
                    return (obj as string);
                }
                else if (obj is char ch)
                {
                    return Convert.ToInt32(ch).ToString();
                }
                else if (obj is byte[])
                {
                    return CUtils.BinToHex((byte[])obj);
                }
                else if (obj is float)
                {
                    return ((float)obj).ToString(Parser.FloatFormat);
                }
                else if (obj is double)
                {
                    return ((double)obj).ToString(Parser.FloatFormat);
                }
                else if (obj is decimal)
                {
                    return ((decimal)obj).ToString(Parser.FloatFormat);
                }
                else
                {
                    Type type = obj.GetType();
                    if (type.IsPrimitive)
                    {
                        return obj.ToString();
                    }
                    else if (type.IsEnum)
                    {
                        return Enum.GetName(type, obj);
                    }
                    else if (type == (typeof(DateTime)))
                    {
                        return ((DateTime)obj).ToString(Parser.DateTimeFormat);
                    }
                    else if (type == (typeof(TimeSpan)))
                    {
                        return ((TimeSpan)obj).Ticks.ToString();
                    }
                    else
                    {
                        TypeParserAdapter parser = Parser.GetTypeAdapter(type);
                        if (parser != null)
                        {
                            return parser.ToString(obj);
                        }
                        else if (type.IsArray)
                        {
                            if (EnocdeArray((Array)obj, out var text))
                            {
                                return text;
                            }
                        }
                        else if (type.GetInterface(typeof(IList).Name) != null)
                        {
                            if (EncodeList((IList)obj, out var text))
                            {
                                return text;
                            }
                        }
                        else if (type.GetInterface(typeof(IDictionary).Name) != null)
                        {
                            if (EncodeDictionary((IDictionary)obj, out var text))
                            {
                                return text;
                            }
                        }
                        else if (EncodeFields(obj, out var text))
                        {
                            return text;
                        }
                        return obj.ToString();
                    }
                }
            }
            catch (Exception err)
            {
                Parser.log.Error(err.Message, err);
                return null;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------
        protected virtual bool DecodeArray(string text, Type e_type, int rank, out Array array)
        {
            if (rank == 1)
            {
                var regions = new List<object>();
                {
                    var iter = new TextRegionIterator(text);
                    while (iter.MoveNext())
                    {
                        TryParse(iter.Current, e_type, out var e);
                        regions.Add(e);
                    }
                    array = Array.CreateInstance(e_type, regions.Count);
                    for (int i = 0; i < regions.Count; i++)
                    {
                        array.SetValue(regions[i], i);
                    }
                    return true;
                }
            }
            else if (rank == 2)
            {
                var regions = new List<Array>();
                {
                    int length_2 = 0;
                    var iter = new TextRegionIterator(text);
                    while (iter.MoveNext())
                    {
                        DecodeArray(iter.Current, e_type, 1, out var rank_2);
                        regions.Add(rank_2);
                        length_2 = Math.Max(length_2, rank_2.Length);
                    }
                    array = Array.CreateInstance(e_type, regions.Count, length_2);
                    for (int i = 0; i < regions.Count; i++)
                    {
                        for (int j = 0; j < regions[i].Length; j++)
                        {
                            array.SetValue(regions[i].GetValue(j), i, j);
                        }
                    }
                    return true;
                }
            }
            else
            {
                throw new Exception("Not Support Array Rank : " + rank);
            }
        }
        protected virtual bool EnocdeArray(Array array, out string ret)
        {
            var type = array.GetType();
            if (type.HasElementType)
            {
                Type e_type = type.GetElementType();
                var need_region = IsRegionElement(e_type);
                int rank = type.GetArrayRank();
                if (rank == 1)
                {
                    var sb = new StringBuilder();
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            if (need_region) sb.Append(REGION_L);
                            sb.Append(ToString(array.GetValue(i)));
                            if (need_region) sb.Append(REGION_R);
                            if (i < array.Length - 1) sb.Append(SEPARATOR);
                        }
                        ret = sb.ToString();
                        return true;
                    }
                }
                else if (rank == 2)
                {
                    {
                        var sb = new StringBuilder();
                        for (int i = 0; i < array.GetLength(0); i++)
                        {
                            sb.Append(REGION_L);
                            for (int j = 0; j < array.GetLength(1); j++)
                            {
                                if (need_region) sb.Append(REGION_L);
                                sb.Append(ToString(array.GetValue(i, j)));
                                if (need_region) sb.Append(REGION_R);
                                if (j < array.GetLength(1) - 1) sb.Append(SEPARATOR);
                            }
                            sb.Append(REGION_R);
                            if (i < array.GetLength(0) - 1) sb.Append(SEPARATOR);
                        }
                        ret = sb.ToString();
                        return true;
                    }
                }
                throw new Exception("Not Support Array Rank : " + rank);
            }
            throw new Exception("Not Support No ElementType Array : " + type);
        }
        //--------------------------------------------------------------------------------------------------------------------
        protected virtual bool DecodeList(string text, Type type, out IList ret)
        {
            if (type.IsGenericType)
            {
                Type e_type = type.GetGenericArguments()[0];
                var list = ReflectionUtil.CreateGenericArrayList(e_type);
                var iter = new TextRegionIterator(text);
                while (iter.MoveNext())
                {
                    TryParse(iter.Current, e_type, out var e);
                    list.Add(e);
                }
                ret = list;
                return true;
            }
            throw new Exception("Not Support No Generic List : " + type);
        }
        protected virtual bool EncodeList(IList list, out string ret)
        {
            var type = list.GetType();
            if (type.IsGenericType)
            {
                {
                    var sb = new StringBuilder();
                    Type subtype = type.GetGenericArguments()[0];
                    var need_region = IsRegionElement(subtype);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (need_region) sb.Append(REGION_L);
                        sb.Append(ToString(list[i]));
                        if (need_region) sb.Append(REGION_R);
                        if (i < list.Count - 1) sb.Append(SEPARATOR);
                    }
                    ret = sb.ToString();
                    return true;
                }
            }
            throw new Exception("Not Support No Generic List : " + type);
        }
        //--------------------------------------------------------------------------------------------------------------------
        protected virtual bool DecodeDictionary(string text, Type type, out IDictionary ret)
        {
            if (type.IsGenericType)
            {
                Type[] kv_type = type.GetGenericArguments();
                var map = ReflectionUtil.CreateGenericHashMap(kv_type);
                var iter = new TextRegionIterator(text);
                while (iter.MoveNext())
                {
                    var iter_kv = new TextRegionIterator(iter.Current);
                    object key = null, value = null;
                    if (iter_kv.MoveNext())
                    {
                        TryParse(iter_kv.Current, kv_type[0], out key);
                    }
                    if (iter_kv.MoveNext())
                    {
                        TryParse(iter_kv.Current, kv_type[1], out value);
                    }
                    if (key != null) map.Add(key, value);
                }
                ret = map;
                return true;
            }
            throw new Exception("Not Support No Generic Dictionary : " + type);
        }
        protected virtual bool EncodeDictionary(IDictionary map, out string ret)
        {
            var type = map.GetType();
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    Type[] kv_type = type.GetGenericArguments();
                    var need_region_k = IsRegionElement(kv_type[0]);
                    var need_region_v = IsRegionElement(kv_type[1]);
                    var map_entry = map.GetEnumerator();
                    var index = 0;
                    while (map_entry.MoveNext())
                    {
                        sb.Append(REGION_L);
                        {
                            if (need_region_k) sb.Append(REGION_L);
                            sb.Append(ToString(map_entry.Key));
                            if (need_region_k) sb.Append(REGION_R);
                        }
                        sb.Append(SEPARATOR);
                        {
                            if (need_region_v) sb.Append(REGION_L);
                            sb.Append(ToString(map_entry.Value));
                            if (need_region_v) sb.Append(REGION_R);
                        }
                        sb.Append(REGION_R);
                        index++;
                        if (index < map.Count) sb.Append(SEPARATOR);
                    }
                    ret = sb.ToString();
                    return true;
                }
            }
            throw new Exception("Not Support No Generic Dictionary : " + type);
        }
        //--------------------------------------------------------------------------------------------------------------------
        protected virtual bool DecodeFields(string text, Type type, out object obj)
        {
            obj = ReflectionUtil.CreateInstance(type);
            if (text.IndexOf(field_split[0]) >= 0)
            {
                var iter = new TextRegionIterator(text);
                while (iter.MoveNext())
                {
                    var kv = iter.Current.Split(field_split, 2);
                    var field = type.GetField(kv[0]);
                    if (field != null)
                    {
                        TryParse(kv[1], field.FieldType, out var value);
                        field.SetValue(obj, value);
                    }
                }
            }
            else
            {
                var iter = new TextRegionIterator(text);
                var lines = iter.ToArray();
                var fields = type.GetFieldsBySequence();
                for (int i = 0; i < lines.Count && i < fields.Length; i++)
                {
                    var field = fields[i];
                    TryParse(lines[i], field.FieldType, out var value);
                    field.SetValue(obj, value);
                }
            }
            return true;
        }
        protected virtual bool EncodeFields(object obj, out string text)
        {
            var sb = new StringBuilder();
            {
                var type = obj.GetType();
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    if (field.IsStatic == false && field.IsPublic)
                    {
                        var fd = field.GetValue(obj);
                        if (fd != null)
                        {
                            sb.Append(REGION_L);
                            sb.Append(field.Name).Append('=').Append(ToString(fd));
                            sb.Append(REGION_R);
                            sb.Append(SEPARATOR);
                        }
                    }
                }
                if (sb[sb.Length - 1] == SEPARATOR) sb.Remove(sb.Length - 1, 1);
                text = sb.ToString();
            }
            return true;
        }
        //--------------------------------------------------------------------------------------------------------------------
        #region RegionElement
        protected static readonly char[] field_split = { '=' };
        public static char REGION_L { get; set; } = '{';
        public static char REGION_R { get; set; } = '}';
        public static char SEPARATOR { get; set; } = ',';

        protected virtual bool IsRegionElement(Type type)
        {
            if (type.IsArray)
            {
                return true;
            }
            else if (type.GetInterface(typeof(IList).Name) != null)
            {
                return true;
            }
            else if (type.GetInterface(typeof(IDictionary).Name) != null)
            {
                return true;
            }
            else if (type.IsPrimitive)
            {
                return false;
            }
            else if (type.IsEnum)
            {
                return false;
            }
            else if (type == (typeof(DateTime)))
            {
                return false;
            }
            else if (type == (typeof(TimeSpan)))
            {
                return false;
            }
            else if (type.IsClass)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// ------------------------------
        /// 0-20 : {{a,b,c,},{a,b,c,},},{{a,b,c,},{a,b,c,},} -> {a,b,c,},{a,b,c,}
        /// 21-40 : {{a,b,c,},{a,b,c,},},{{a,b,c,},{a,b,c,},} -> {a,b,c,},{a,b,c,
        /// ------------------------------                                       
        /// 0-1 : a,b,c,{a,b,c,}                   -> a                          
        /// 2-3 : a,b,c,{a,b,c,}                   -> b                          
        /// 4-5 : a,b,c,{a,b,c,}                   -> c                          
        /// 6-13 : a,b,c,{a,b,c,}                   -> a,b,c,                    
        /// ------------------------------                                       
        /// 0-1 : a,b,c,a,b,c,                     -> a                          
        /// 2-3 : a,b,c,a,b,c,                     -> b                          
        /// 4-5 : a,b,c,a,b,c,                     -> c                          
        /// 6-7 : a,b,c,a,b,c,                     -> a                          
        /// 8-9 : a,b,c,a,b,c,                     -> b                          
        /// 10-11 : a,b,c,a,b,c,                     -> c                        
        /// ------------------------------                                       
        /// 0-8 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> a,b,c,                     
        /// 9-10 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> a                         
        /// 11-12 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> b                        
        /// 13-14 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> c                        
        /// 15-15 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  ->                          
        /// 16-24 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> a,b,c,                   
        /// 25-26 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> a                        
        /// 27-28 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> b                        
        /// 29-30 : {a,b,c,},a,b,c,,{a,b,c,},a,b,c,  -> c                        
        /// ------------------------------                                       
        /// 0-8 : {a,b,c,},{a,b,c,},{a,b,c,},{a,b,c,} -> a,b,c,                  
        /// 9-17 : {a,b,c,},{a,b,c,},{a,b,c,},{a,b,c,} -> a,b,c,                 
        /// 18-26 : {a,b,c,},{a,b,c,},{a,b,c,},{a,b,c,} -> a,b,c,                
        /// 27-34 : {a,b,c,},{a,b,c,},{a,b,c,},{a,b,c,} -> a,b,c,                
        /// ------------------------------                                       
        /// 0-8 : {{a},{b}}                        -> {a},{b}                    
        /// ------------------------------                                       
        /// 0-3 : {a},{b}                          -> a                          
        /// 4-6 : {a},{b}                          -> b                          
        /// ------------------------------                                       
        /// 0-9 : {{a},{c}},{{b},{c}}              -> {a},{c}                    
        /// 10-18 : {{a},{c}},{{b},{c}}              -> {b},{c}                  
        /// ------------------------------                                       
        /// 0-5 : },{b}                            -> },{b}                      
        /// ------------------------------                                       
        /// 0-2 : {a}}                             -> a                          
        /// 3-4 : {a}}                             -> }                          
        /// ------------------------------                                       
        /// 0-4 : {{a}                             -> {{a}                       
        /// ------------------------------                                            
        /// </summary>
        public struct TextRegionIterator : IEnumerator<string>
        {
            private string text;
            private int i;
            private int endIndex;
            private string current;
            public TextRegionIterator(string text, int startIndex, int endIndex)
            {
                this.text = text;
                this.i = startIndex;
                this.endIndex = endIndex;
                this.current = null;
            }
            public TextRegionIterator(string text)
            {
                this.text = text;
                this.i = 0;
                this.endIndex = text.Length;
                this.current = null;
            }
            public string Current => this.current;
            object IEnumerator.Current => this.current;
            public void Dispose()
            {
                i = 0;
                current = null;
                text = null;
                endIndex = 0;
            }
            public List<string> ToArray()
            {
                var array = new List<string>(); ;
                while (MoveNext()) { array.Add(current); }
                return array;
            }
            public bool MoveNext()
            {
                if (GetRegion(text, i, endIndex, out current, out i))
                {
                    i++;
                    return true;
                }
                return false;
            }
            public void Reset()
            {
                i = 0;
                current = null;
            }
            public static bool GetRegion(string text, int startIndex, int endIndex, out string region, out int lastIndex)
            {
                if (startIndex >= endIndex)
                {
                    region = null;
                    lastIndex = text.Length;
                    return false;
                }
                int mark = 0;
                int first_L = -1;
                int last_R = -1;
                int first_SP = -1;
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (text[i] == REGION_L)
                    {
                        if (first_L < 0) first_L = i;
                        mark++;
                    }
                    else if (text[i] == REGION_R)
                    {
                        last_R = i;
                        mark--;
                        if (mark <= 0) break;
                    }
                    else if (text[i] == SEPARATOR)
                    {
                        if (first_SP < 0) { first_SP = i; }
                        if (mark <= 0) break;
                    }
                }
                if (first_L >= startIndex && first_L < last_R && mark == 0)
                {
                    region = text.Substring(first_L + 1, last_R - first_L - 1);
                    first_SP = text.IndexOf(SEPARATOR, last_R + 1);
                    if (first_SP > last_R)
                    {
                        lastIndex = first_SP;
                        return true;
                    }
                    else
                    {
                        lastIndex = last_R;
                        return true;
                    }
                }
                else if (first_SP >= 0)
                {
                    region = text.Substring(startIndex, first_SP - startIndex);
                    lastIndex = first_SP;
                    return true;
                }
                else
                {
                    region = text.Substring(startIndex, endIndex - startIndex);
                    lastIndex = endIndex;
                    return true;
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------
    }

    //--------------------------------------------------------------------------------------------------------------------
#if FALSE
    //--------------------------------------------------------------------------------------------------------------------
    /*
    public class SerializerParserAdapter : ParserAdapter
    {
        public static char REGION_L { get; set; } = '{';
        public static char REGION_R { get; set; } = '}';
        public static char SEPARATOR { get; set; } = ',';

        public bool IsLookFeel { get; set; } = true;
        //--------------------------------------------------------------------------------------------------------------------
        public bool TryParse(string text, Type type, out object ret)
        {
            if (typeof(string) == (type))
            {
                if (text == null)
                {
                    ret = null;
                    return false;
                }
                var begin = text.IndexOf(':');
                if (begin > 0 && int.TryParse(text.Substring(0, begin), out var length))
                {
                    return text.Substring(begin + 1, length);
                }
            }
            return null;
        }
        public string ToString(object obj)
        {
            using (var sb = StringBuilderObjectPool.AllocAutoRelease())
            {
                EncodeObject(sb.Output, obj, 0);
                return sb.ToString();
            }
        }
        private void EncodeObject(StringBuilder sb, object obj, int indent)
        {
            try
            {
                if (obj == null)
                {
                    sb.Append("");
                }
                else if (obj is string)
                {
                    sb.Append(REGION_L);
                    sb.Append(obj as string);
                    sb.Append(REGION_R);
                }
                else if (obj is float)
                {
                    sb.Append(((float)obj).ToString(Parser.FloatFormat));
                }
                else if (obj is double)
                {
                    sb.Append(((double)obj).ToString(Parser.FloatFormat));
                }
                else
                {
                    Type type = obj.GetType();
                    if (type.IsPrimitive)
                    {
                        sb.Append(obj.ToString());
                    }
                    else if (type.IsEnum)
                    {
                        sb.Append(Enum.GetName(type, obj));
                    }
                    else if (type == (typeof(DateTime)))
                    {
                        sb.Append(REGION_L);
                        sb.Append(obj.ToString());
                        sb.Append(REGION_R);
                    }
                    else if (type == (typeof(TimeSpan)))
                    {
                        sb.Append(REGION_L);
                        sb.Append(obj.ToString());
                        sb.Append(REGION_R);
                    }
                    else if (type.IsArray)
                    {
                        EncodeArray(sb, (Array)obj, indent + 1);
                    }
                    else if (type.GetInterface(typeof(IList).Name) != null)
                    {
                        EncodeList(sb, (IList)obj, indent + 1);
                    }
                    else if (type.GetInterface(typeof(IDictionary).Name) != null)
                    {
                        EncodeDictionary(sb, (IDictionary)obj, indent + 1);
                    }
                    else
                    {
                        EncodeFields(sb, obj, indent + 1);
                    }
                }
            }
            catch (Exception err)
            {
                Parser.log.Error(err.Message, err);
            }
        }
        private void EncodeFields(StringBuilder sb, object data, int indent)
        {
            sb.Append(REGION_L);
            if (IsLookFeel) sb.AppendLine();
            Type type = data.GetType();
            //AppendAttribute(sb, ".type", type.FullName);
            foreach (var field in PropertyUtil.SortFields(type.GetFields()))
            {
                if (!field.IsStatic && field.IsPublic)
                {
                    object fd = field.GetValue(data);
                    if (fd != null)
                    {
                        sb.Append(field.Name).Append(':');
                        EncodeObject(sb, fd, indent + 1);
                        sb.Append(SEPARATOR);
                    }
                }
            }
            if (IsLookFeel) sb.AppendLine();
            sb.Append(REGION_R);
        }
        private void EncodeArray(StringBuilder sb, Array array, int indent)
        {
            sb.Append(REGION_L);
            if (IsLookFeel) sb.AppendLine();
            Type type = array.GetType();
            var rank = type.GetArrayRank();
            if (rank == 1)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    EncodeObject(sb, array.GetValue(i), indent + 1);
                    sb.Append(SEPARATOR);
                }
                if (IsLookFeel) sb.AppendLine();
            }
            else if (rank == 2)
            {
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    sb.Append(REGION_L);
                    if (IsLookFeel) sb.AppendLine();
                    for (int j = 0; j < array.GetLength(1); j++)
                    {
                        EncodeObject(sb, array.GetValue(i, j), indent + 2);
                        sb.Append(SEPARATOR);
                    }
                    if (IsLookFeel) sb.AppendLine();
                    sb.Append(REGION_R);
                    sb.Append(SEPARATOR);
                }
            }
            if (IsLookFeel) sb.AppendLine();
            sb.Append(REGION_R);
        }
        private void EncodeList(StringBuilder sb, IList list, int indent)
        {
            Type type = list.GetType();
            indent++;
            sb.Append(REGION_L);
            if (IsLookFeel) sb.AppendLine();
            for (int i = 0; i < list.Count; i++)
            {
                EncodeObject(sb, list[i], indent);
                sb.Append(SEPARATOR);
            }
            if (IsLookFeel) sb.AppendLine();
            sb.Append(REGION_R);
        }
        private void EncodeDictionary(StringBuilder sb, IDictionary map, int indent)
        {
            Type type = map.GetType();
            indent++;
            sb.Append(REGION_L);
            if (IsLookFeel) sb.AppendLine();
            using (var keys = new List.AllocAutoRelease(map.Keys))
            {
                keys.Sort();
                foreach (object k in keys)
                {
                    object v = map[k];
                    EncodeObject(sb, k, indent);
                    sb.Append(':');
                    EncodeObject(sb, v, indent);
                    sb.Append(SEPARATOR);
                }
            }
            if (IsLookFeel) sb.AppendLine();
            sb.Append(REGION_R);
        }

        public static StringBuilder AppendAttribute(StringBuilder sb, string key, string value)
        {
            return sb.AppendFormat("{0}:{1}", key, value);
        }
    }
    */
    //--------------------------------------------------------------------------------------------------------------------

    public class ListParser<T> : TypeParserAdapter
    {
        public Type ParserType { get => typeof(ArrayList<T>); }
        public bool IsAssignableFrom { get => true; }
        public char SplitChar { get; set; }
        public ListParser(char splitChar = ',')
        {
            this.SplitChar = splitChar;
        }
        public bool Accept(Type type)
        {
            //if (type.IsInterfaceOf(typeof(IList)) && type.IsGenericArgumentsOf(typeof(T)))
            if (type.IsAssignableFrom(ParserType))
            {
                return true;
            }
            return false;
        }
        public bool TryParse(string text, out object ret)
        {
            if (text == null)
            {
                ret = null;
                return false;
            }
            var ss = text.Split(SplitChar);
            var list = new ArrayList<T>();
            foreach (string s in ss)
            {
                list.Add(StringToElement(s));
            }
            ret = list;
            return true;
        }
        public string ToString(object obj)
        {
            var list = (IList<T>)obj;
            using (var auto = StringBuilderObjectPool.AllocAutoRelease())
            {
                var sb = auto.Output;
                int i = 0;
                foreach (T d in list)
                {
                    sb.Append(ElementToString(d));
                    if (i < list.Count - 1)
                    {
                        sb.Append(SplitChar);
                    }
                    i++;
                }
                return sb.ToString();
            }
        }
        public virtual T StringToElement(string text)
        {
            return (T)Parser.StringToObject(text, typeof(T));
        }
        public virtual string ElementToString(T obj)
        {
            return Parser.ObjectToString(obj);
        }
    }

    public static class SimpleTypeParser
    {
        public static string ToSimpleString(this Type type)
        {
            if (typeof(string) == (type)) /*      */ return "utf";
            if (typeof(bool) == (type))  /*       */ return "boo";
            if (typeof(int) == (type))  /*        */ return "s32";
            if (typeof(uint) == (type))  /*       */ return "u32";
            if (typeof(long) == (type))  /*       */ return "s64";
            if (typeof(ulong) == (type))  /*      */ return "u64";
            if (typeof(short) == (type))  /*      */ return "s16";
            if (typeof(ushort) == (type)) /*      */ return "u16";
            if (typeof(float) == (type))  /*      */ return "f32";
            if (typeof(double) == (type)) /*      */ return "f64";
            if (typeof(byte) == (type))  /*       */ return "ub8";
            if (typeof(sbyte) == (type))  /*      */ return "sb8";
            if (typeof(char) == (type))  /*       */ return "chr";
            if (typeof(decimal) == (type))  /*    */ return "dec";
            if (typeof(byte[]) == (type))  /*     */ return "bin";
            return "NaN";
        }
        public static bool TryParseType(string text, out Type type)
        {
            switch (text)
            {
                case "utf": type = typeof(string); return true;
                case "boo": type = typeof(bool); return true;
                case "s32": type = typeof(int); return true;
                case "u32": type = typeof(uint); return true;
                case "s64": type = typeof(long); return true;
                case "u64": type = typeof(ulong); return true;
                case "s16": type = typeof(short); return true;
                case "u16": type = typeof(ushort); return true;
                case "f32": type = typeof(float); return true;
                case "f64": type = typeof(double); return true;
                case "ub8": type = typeof(byte); return true;
                case "sb8": type = typeof(sbyte); return true;
                case "chr": type = typeof(char); return true;
                case "dec": type = typeof(decimal); return true;
                case "bin": type = typeof(byte[]); return true;
                case "NaN": type = null; return true;
                default:
                    type = null;
                    return false;
            }
        }

        private static System.Xml.XmlWriterSettings XmlSettings = new System.Xml.XmlWriterSettings() { Indent = false };

        public static void PutSimple(this IOutputStream output, object obj)
        {
            if (obj == null)
            {
                output.PutUTF(string.Empty);
            }
            else
            {
                var type = obj.GetType();
                var st = type.ToSimpleString();
                if (st == "NaN")
                {
                    output.PutUTF($"{st}:{XmlUtil.ObjectToXml(obj).ToXmlString(XmlSettings)}");
                }
                else
                {
                    output.PutUTF($"{st}:{Parser.ObjectToString(obj)}");
                }
            }
        }
        public static object GetSimple(this IInputStream input)
        {
            var st = input.GetUTF();
            if (st.Length >= 4)
            {
                if (TryParseType(st.Substring(0, 3), out var type))
                {
                    var content = st.Substring(4);
                    if (type == null)
                    {
                        return XmlUtil.XmlToObject(XmlUtil.FromString(content));
                    }
                    else
                    {
                        return Parser.StringToObject(content, type);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// IDictionary简单序列化
        /// </summary>
        public static void PutSimpleMap(this IOutputStream sb, IDictionary map)
        {
            if (map == null)
            {
                sb.PutS32(-1);
            }
            else
            {
                sb.PutS32(map.Count);
                map.ForEachDictionary((e) =>
                {
                    sb.PutSimple(e.Key);
                    sb.PutSimple(e.Value);
                });
            }
        }
        /// <summary>
        /// IDictionary简单序列化
        /// </summary>
        public static TMap GetSimpleMap<TMap>(this IInputStream sb, TMap map) where TMap : IDictionary
        {
            var count = sb.GetS32();
            for (int i = 0; i < count; i++)
            {
                var key = sb.GetSimple();
                var value = sb.GetSimple();
                map.Add(key, value);
            }
            return map;
        }
        public static TMap GetSimpleMap<TMap>(this IInputStream sb) where TMap : IDictionary, new()
        {
            return GetSimpleMap(sb, new TMap());
        }


        /// <summary>
        /// IList简单序列化
        /// </summary>
        public static void PutSimpleList(this IOutputStream sb, IList list)
        {
            if (list == null)
            {
                sb.PutS32(-1);
            }
            else
            {
                sb.PutS32(list.Count);
                foreach (var e in list)
                {
                    sb.PutSimple(e);
                }
            }
        }
        /// <summary>
        /// IList简单序列化
        /// </summary>
        public static TList GetSimpleList<TList>(this IInputStream sb, TList list) where TList : IList
        {
            var count = sb.GetS32();
            for (int i = 0; i < count; i++)
            {
                var e = sb.GetSimple();
                list.Add(e);
            }
            return list;
        }
        public static TList GetSimpleList<TList>(this IInputStream sb) where TList : IList, new()
        {
            return GetSimpleList(sb, new TList());
        }
        //--------------------------------------------------------------------------------------------------------------------

    }
#endif
    //--------------------------------------------------------------------------------------------------------------------
    [Reflectible]
    public interface ITextConverter
    {
        Type ParserType { get; }
        string Encode(Type type, object value);
        object Decode(Type type, string text);
    }

    public class TextConverters
    {
        protected readonly HashMap<Type, ITextConverter> converters = new HashMap<Type, ITextConverter>();
        protected readonly EnumConverter enumConverter = new EnumConverter();
        public TextConverters()
        {
            foreach (var ct in GetType().GetNestedTypes())
            {
                this.Regist((ITextConverter)DeepActivator.CreateInstance(ct));
            }
        }
        public void Regist(ITextConverter converter)
        {
            converters.Put(converter.ParserType, converter);
        }
        public virtual bool TryGetConverter(Type type, out ITextConverter converter)
        {
            if (type.IsEnum)
            {
                converter = enumConverter;
                return true;
            }
            if (converters.TryGetValue(type, out converter))
            {
                return true;
            }
            return false;
        }
        public object Decode(Type type, string text)
        {
            try
            {
                if (TryGetConverter(type, out var converter))
                {
                    return converter.Decode(type, text);
                }
            }
            catch (Exception err)
            {
                throw new Exception($"Can not convert '{text}' to '{type}' : {err.Message}", err);
            }
            return null;
        }
        public string Encode(Type type, object value)
        {
            if (TryGetConverter(value.GetType(), out var converter))
            {
                return converter.Encode(type, value);
            }
            return string.Empty;
        }


        public class EnumConverter : ITextConverter
        {
            public Type ParserType => typeof(Enum);
            public object Decode(Type type, string text) { return Enum.Parse(type, text, true); }
            public string Encode(Type type, object value) { return (Enum.GetName(value.GetType(), value)); }
        }
        public class StringConverter : ITextConverter
        {
            public Type ParserType => typeof(string);
            public object Decode(Type type, string text) { return text; }
            public string Encode(Type type, object value) { return value as string; }
        }
        public class CharConverter : ITextConverter
        {
            public Type ParserType => typeof(char);
            public object Decode(Type type, string text) { return Convert.ToChar(Parser.ParseInt(text)); }
            public string Encode(Type type, object value) { return Convert.ToInt32((char)value).ToString(); }
        }
        public class BytesConverter : ITextConverter
        {
            public Type ParserType => typeof(byte[]);
            public object Decode(Type type, string text) { return CUtils.HexToBin(text); }
            public string Encode(Type type, object value) { return CUtils.BinToHex((byte[])value); }
        }
        public class BoolConverter : ITextConverter
        {
            public Type ParserType => typeof(bool);
            public object Decode(Type type, string text) { return Parser.ParseBool(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class Int32Converter : ITextConverter
        {
            public Type ParserType => typeof(int);
            public object Decode(Type type, string text) { return Parser.ParseInt(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class UInt32Converter : ITextConverter
        {
            public Type ParserType => typeof(uint);
            public object Decode(Type type, string text) { return Parser.ParseUInt(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class Int64Converter : ITextConverter
        {
            public Type ParserType => typeof(long);
            public object Decode(Type type, string text) { return Parser.ParseLong(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class UInt64Converter : ITextConverter
        {
            public Type ParserType => typeof(ulong);
            public object Decode(Type type, string text) { return Parser.ParseULong(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class Int16Converter : ITextConverter
        {
            public Type ParserType => typeof(short);
            public object Decode(Type type, string text) { return Parser.ParseShort(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class UInt16Converter : ITextConverter
        {
            public Type ParserType => typeof(ushort);
            public object Decode(Type type, string text) { return Parser.ParseUShort(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class SByteConverter : ITextConverter
        {
            public Type ParserType => typeof(sbyte);
            public object Decode(Type type, string text) { return Parser.ParseSByte(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class ByteConverter : ITextConverter
        {
            public Type ParserType => typeof(byte);
            public object Decode(Type type, string text) { return Parser.ParseByte(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class FloatConverter : ITextConverter
        {
            public Type ParserType => typeof(float);
            public object Decode(Type type, string text) { return Parser.ParseFloat(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class DoubleConverter : ITextConverter
        {
            public Type ParserType => typeof(double);
            public object Decode(Type type, string text) { return Parser.ParseDouble(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class DecimalConverter : ITextConverter
        {
            public Type ParserType => typeof(decimal);
            public object Decode(Type type, string text) { return Parser.ParseDecimal(text); }
            public string Encode(Type type, object value) { return value.ToString(); }
        }
        public class DateTimeConverter : ITextConverter
        {
            public Type ParserType => typeof(DateTime);
            public object Decode(Type type, string text) { return DateTime.ParseExact(text, Parser.DateTimeFormat, Parser.CultureInfo); }
            public string Encode(Type type, object value) { return (((DateTime)value).ToString(Parser.DateTimeFormat)); }
        }
        public class TimeSpanConverter : ITextConverter
        {
            public Type ParserType => typeof(TimeSpan);
            public object Decode(Type type, string text) { return TimeSpan.FromTicks(Parser.ParseLong(text)); }
            public string Encode(Type type, object value) { return (((TimeSpan)value).Ticks.ToString()); }
        }
        public class BigIntegerConverter : ITextConverter
        {
            public Type ParserType => typeof(BigInteger);
            public object Decode(Type type, string text) { return Parser.ParseBigInteger(text); }
            public string Encode(Type type, object value) { return (((BigInteger)value).ToString()); }
        }
    }



    //--------------------------------------------------------------------------------------------------------------------

    public class SimpleTypeParser<T> : TypeParserAdapter
    {
        public delegate T DecodeFieldIndex(string text);
        public delegate string EncodeFieldIndex(T value);
        private DecodeFieldIndex decode;
        private EncodeFieldIndex encode;
        public Type ParserType => typeof(T);
        public bool IsAssignableFrom => false;
        public SimpleTypeParser(DecodeFieldIndex d, EncodeFieldIndex e)
        {
            encode = e;
            decode = d;
        }
        public bool Accept(Type type) { return type == ParserType; }
        public string ToString(object obj)
        {
            return encode((T)obj);
        }
        public bool TryParse(string text, out object value)
        {
            value = decode(text);
            return true;
        }
    }
}