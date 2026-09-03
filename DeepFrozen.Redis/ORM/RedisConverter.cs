using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepCrystal.Json;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.ORM.Redis
{
    public interface IRedisConverter
    {
        Type ParserType { get; }
        RedisValue Encode(object value);
        object Decode(Type type, RedisValue text);
    }
    public class RedisConverters
    {
        //--------------------------------------------------------------------------------------------------------------------------------
        private static string DateTimeFormat = "yyyy-MM-dd HH:mm:ss:fff";
        private static PropertiesFormat PropertiesFormat = new PropertiesFormat() { Separator = " = " };
        private static FastJsonParser fastJson = new FastJsonParser();
        private static HashMap<Type, IRedisConverter> converters = new HashMap<Type, IRedisConverter>();
        private static EnumConverter enumConverter = new EnumConverter();
        //--------------------------------------------------------------------------------------------------------------------------------
        static RedisConverters()
        {
            foreach (var ct in typeof(RedisConverters).GetNestedTypes())
            {
                Regist((IRedisConverter)DeepActivator.CreateInstance(ct));
            }
        }
        public static void Regist(IRedisConverter converter)
        {
            converters.Put(converter.ParserType, converter);
        }
        public static bool TryGetConverter(Type type, out IRedisConverter converter)
        {
            if (converters.TryGetValue(type, out converter))
            {
                return true;
            }
            if (type.IsEnum)
            {
                converter = enumConverter;
                return true;
            }
            return false;
        }
        //--------------------------------------------------------------------------------------------------------------------------------
        public static RedisValue ToRedisValue(object obj)
        {
            return ToRedisValue(obj, typeof(object));
        }
        public static RedisValue ToRedisValue(object obj, Type decleardType)
        {
            if (obj == null) return RedisValue.Null;
            Type type = obj.GetType();
            if (TryGetConverter(type, out var converter))
            {
                return converter.Encode(obj);
            }
            if (obj is IBinaryStructMapping binExt)
            {
                using (var io = IOStreamObjectPool.AllocAutoRelease(ORMFactory.Instance.StructFactory))
                {
                    var output = io.Output;
                    output.PutExt(binExt);
                    return output.Buffer.ToArray();
                }
            }
            if (obj is ITextStructMapping txtExt)
            {
                using (var output = new TextOutputStream(new StringWriter(), ORMFactory.Instance.StructFactory))
                {
                    output.PutExt(txtExt);
                    return output.ToString();
                }
            }
            return fastJson.EncodeObject(obj, decleardType);
        }
        public static object ToObject(RedisValue obj, Type type)
        {
            try
            {
                if (obj.IsNull) return null;
                if (TryGetConverter(type, out var converter))
                {
                    return converter.Decode(type, obj);
                }
                if (type.IsInterfaceOf(typeof(IBinaryStructMapping)))
                {
                    using (var io = IOStreamObjectPool.AllocAutoRelease(ORMFactory.Instance.StructFactory, (byte[])obj))
                    {
                        var input = io.Input;
                        return input.GetExtAny();
                    }
                }
                if (type.IsInterfaceOf(typeof(ITextStructMapping)))
                {
                    using (var input = new TextInputStream(new StringReader((string)obj), ORMFactory.Instance.StructFactory))
                    {
                        return input.GetExtAny();
                    }
                }
                return fastJson.DecodeObject(obj, type);
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }
        public static T ToObject<T>(RedisValue obj)
        {
            return (T)ToObject(obj, typeof(T));
        }
        public static string PersistDumpToJson(object obj)
        {
            return fastJson.EncodeObject(obj, null);
        }
        public static object PersistRecoverFromJson(string json)
        {
            return fastJson.DecodeObject(json, null);
        }
        //--------------------------------------------------------------------------------------------------------------------------------
        #region Converters
        public class RedisValueConverter : IRedisConverter
        {
            public Type ParserType => typeof(RedisValue);
            public object Decode(Type type, RedisValue text) { return text; }
            public RedisValue Encode(object value) { return (RedisValue)value; }
        }
        public class EnumConverter : IRedisConverter
        {
            public Type ParserType => typeof(Enum);
            public object Decode(Type type, RedisValue text) { return Enum.Parse(type, text, true); }
            public RedisValue Encode(object value) { return (Enum.GetName(value.GetType(), value)); }
        }
        public class StringConverter : IRedisConverter
        {
            public Type ParserType => typeof(string);
            public object Decode(Type type, RedisValue text) { return (string)text; }
            public RedisValue Encode(object value) { return (string)value; }
        }
        public class CharConverter : IRedisConverter
        {
            public Type ParserType => typeof(char);
            public object Decode(Type type, RedisValue text) { return ((string)text)[0]; }
            public RedisValue Encode(object value) { return value.ToString(); }
        }
        public class BytesConverter : IRedisConverter
        {
            public Type ParserType => typeof(byte[]);
            public object Decode(Type type, RedisValue text) { return (byte[])text; }
            public RedisValue Encode(object value) { return (byte[])value; }
        }
        public class BoolConverter : IRedisConverter
        {
            public Type ParserType => typeof(bool);
            public object Decode(Type type, RedisValue text) { return (bool)text; }
            public RedisValue Encode(object value) { return (bool)value; }
        }
        public class Int32Converter : IRedisConverter
        {
            public Type ParserType => typeof(int);
            public object Decode(Type type, RedisValue text) { return (int)text; }
            public RedisValue Encode(object value) { return (int)value; }
        }
        public class UInt32Converter : IRedisConverter
        {
            public Type ParserType => typeof(uint);
            public object Decode(Type type, RedisValue text) { return (uint)text; }
            public RedisValue Encode(object value) { return (uint)value; }
        }
        public class Int64Converter : IRedisConverter
        {
            public Type ParserType => typeof(long);
            public object Decode(Type type, RedisValue text) { return (long)text; }
            public RedisValue Encode(object value) { return (long)value; }
        }
        public class UInt64Converter : IRedisConverter
        {
            public Type ParserType => typeof(ulong);
            public object Decode(Type type, RedisValue text) { return (ulong)text; }
            public RedisValue Encode(object value) { return (ulong)value; }
        }
        public class Int16Converter : IRedisConverter
        {
            public Type ParserType => typeof(short);
            public object Decode(Type type, RedisValue text) { return (short)text; }
            public RedisValue Encode(object value) { return (short)value; }
        }
        public class UInt16Converter : IRedisConverter
        {
            public Type ParserType => typeof(ushort);
            public object Decode(Type type, RedisValue text) { return (ushort)((uint)text); }
            public RedisValue Encode(object value) { return Convert.ToUInt32(value); }
        }
        public class SByteConverter : IRedisConverter
        {
            public Type ParserType => typeof(sbyte);
            public object Decode(Type type, RedisValue text) { return (sbyte)text; }
            public RedisValue Encode(object value) { return (sbyte)value; }
        }
        public class ByteConverter : IRedisConverter
        {
            public Type ParserType => typeof(byte);
            public object Decode(Type type, RedisValue text) { return (byte)((uint)text); }
            public RedisValue Encode(object value) { return Convert.ToUInt32(value); }
        }
        public class FloatConverter : IRedisConverter
        {
            public Type ParserType => typeof(float);
            public object Decode(Type type, RedisValue text) { return (float)text; }
            public RedisValue Encode(object value) { return (float)value; }
        }
        public class DoubleConverter : IRedisConverter
        {
            public Type ParserType => typeof(double);
            public object Decode(Type type, RedisValue text) { return (double)text; }
            public RedisValue Encode(object value) { return (double)value; }
        }
        public class DateTimeConverter : IRedisConverter
        {
            public Type ParserType => typeof(DateTime);
            public object Decode(Type type, RedisValue text) { return DateTime.ParseExact((string)text, DateTimeFormat, Parser.CultureInfo); }
            public RedisValue Encode(object value) { return ((DateTime)value).ToString(DateTimeFormat, Parser.CultureInfo); }
        }
        public class TimeSpanConverter : IRedisConverter
        {
            public Type ParserType => typeof(TimeSpan);
            public object Decode(Type type, RedisValue text) { return TimeSpan.FromTicks((long)text); }
            public RedisValue Encode(object value) { return ((TimeSpan)value).Ticks; }
        }
        public class BigIntegerConverter : IRedisConverter
        {
            public Type ParserType => typeof(BigInteger);
            public object Decode(Type type, RedisValue text) { return new BigInteger((byte[])text); }
            public RedisValue Encode(object value) { return ((BigInteger)value).ToByteArray(); }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
    }



    public struct HashScanAsyncEnumerable : IAsyncEnumerable<HashQueryEntry>
    {
        private IAsyncEnumerable<HashEntry> it;
        public HashScanAsyncEnumerable(IAsyncEnumerable<HashEntry> it) { this.it = it; }
        public IAsyncEnumerator<HashQueryEntry> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new HashScanAsyncEnumerator(it.GetAsyncEnumerator(cancellationToken));
        }
        public struct HashScanAsyncEnumerator : IAsyncEnumerator<HashQueryEntry>
        {
            private IAsyncEnumerator<HashEntry> it;
            public HashScanAsyncEnumerator(IAsyncEnumerator<HashEntry> it) { this.it = it; }
            public HashQueryEntry Current
            {
                get
                {
                    var e = it.Current;
                    return new HashQueryEntry(e.Name, e.Value);
                }
            }
            public ValueTask DisposeAsync()
            {
                return it.DisposeAsync();
            }
            public ValueTask<bool> MoveNextAsync()
            {
                return it.MoveNextAsync();
            }
        }
    }
    public struct HashScanEnumerable : IEnumerable<HashQueryEntry>
    {
        private IEnumerable<HashEntry> it;
        public HashScanEnumerable(IEnumerable<HashEntry> it) { this.it = it; }
        public IEnumerator<HashQueryEntry> GetEnumerator()
        {
            return new HashScanEnumerator(it.GetEnumerator());
        }
        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        public struct HashScanEnumerator : IEnumerator<HashQueryEntry>
        {
            private IEnumerator<HashEntry> it;
            public HashScanEnumerator(IEnumerator<HashEntry> it) { this.it = it; }
            public HashQueryEntry Current
            {
                get
                {
                    var e = it.Current;
                    return new HashQueryEntry(e.Name, e.Value);
                }
            }
            object IEnumerator.Current => this.Current;
            public void Dispose()
            {
                it.Dispose();
            }
            public bool MoveNext()
            {
                return it.MoveNext();
            }
            public void Reset()
            {
                it.Reset();
            }
        }
    }
}
