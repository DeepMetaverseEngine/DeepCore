using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace DeepCore
{
    [Serializable]
    [KnownType(typeof(UnionValueMap))]
    [KnownType(typeof(UnionValueArray))]
    [KnownType(typeof(KeyValuePair<UnionValue, UnionValue>[]))]
    [KnownType(typeof(InnerType))]
    public struct UnionValue : IConvertible, IEquatable<UnionValue>, IComparable, IComparable<UnionValue>, ISerializable
    {
        [Serializable]
        public enum InnerType
        {
            Null = 0,
            Integer,
            Boolean,
            Float,
            DateTime,
            TimeSpan,
            Enum,
            Binary,
            String,
            UnionValueMap,
            UnionValueArray,
            External,
            Invalid
        }

        #region statics

        public static readonly UnionValue Null = new UnionValue(null);


        public static UnionValue NewMap => new UnionValue(new UnionValueMap());

        public static UnionValue NewArray => new UnionValue(new UnionValueArray());

        public static UnionValue NewConcurrentMap => new UnionValue(new UnionValueMap(new ConcurrentDictionary<UnionValue, UnionValue>()));

        public static UnionValue NewConcurrentArray
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal static UnionValue ToUnionValue(object obj)
        {
            if (IsNativeObj(obj))
            {
                return new UnionValue(obj);
            }

            return UnionValue.Null;
        }

        public static UnionValue Create(object obj)
        {
            var ret = ToUnionValue(obj);
            if (!ret.IsNull)
            {
                return ret;
            }

            return new UnionValue(obj);
        }

        public static UnionValue Create(PrimitiveData v)
        {
            return new UnionValue(v);
        }

        /// <summary>
        /// 判断obj 是否是元数据 IsPrimitive string Enum DateTime TimeSpan
        /// </summary>
        /// <returns></returns>
        public static bool IsNativeObj(object obj)
        {
            return obj == null || obj.GetType().IsPrimitive || obj is string || obj is Enum || obj is DateTime || obj is TimeSpan || obj is byte[];
        }

        /// <summary>
        /// 判断type 是否是元数据 IsPrimitive string Enum DateTime TimeSpan
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNativeType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || typeof(string).IsAssignableFrom(type) || typeof(DateTime).IsAssignableFrom(type) || typeof(TimeSpan).IsAssignableFrom(type) || typeof(byte[]).IsAssignableFrom(type);
        }

        #endregion

        private readonly object _innerValue;

        public readonly string Desc;
        public readonly PrimitiveData? PrimitiveValue;

        public object Value => PrimitiveValue?.Value ?? _innerValue;

        public struct PrimitiveData
        {
            public readonly double DoubleValue;
            public readonly long LongValue;
            public readonly PrimitiveType Type;

            public PrimitiveData(PrimitiveType t, double f, long l)
            {
                Type = t;
                DoubleValue = f;
                LongValue = l;
            }

            public enum PrimitiveType
            {
                None,
                Boolean,
                Float,
                Integer
            }

            public bool IsBoolean => Type == PrimitiveType.Boolean;
            public bool IsFloat => Type == PrimitiveType.Float;

            public bool IsInteger => Type == PrimitiveType.Integer;

            public object Value
            {
                get
                {
                    switch (Type)
                    {
                        case PrimitiveType.Boolean:
                            return LongValue == 1;
                        case PrimitiveType.Float:
                            return DoubleValue;
                        case PrimitiveType.Integer:
                            return LongValue;
                        default:
                            return null;
                    }
                }
            }

            public static explicit operator PrimitiveData(double value)
            {
                return new PrimitiveData(PrimitiveType.Float, value, 0);
            }

            public static explicit operator PrimitiveData(float value)
            {
                return new PrimitiveData(PrimitiveType.Float, value, 0);
            }

            public static explicit operator PrimitiveData(long value)
            {
                return new PrimitiveData(PrimitiveType.Integer, 0, value);
            }

            public static explicit operator PrimitiveData(int value)
            {
                return new PrimitiveData(PrimitiveType.Integer, 0, value);
            }

            public static explicit operator PrimitiveData(bool value)
            {
                return new PrimitiveData(PrimitiveType.Boolean, 0, value ? 1 : 0);
            }
        }

        internal InnerType InnerTypeCode
        {
            get
            {
                if (IsNull)
                {
                    return InnerType.Null;
                }

                if (IsMap)
                {
                    return InnerType.UnionValueMap;
                }

                if (IsArray)
                {
                    return InnerType.UnionValueArray;
                }

                if (IsExternal)
                {
                    return InnerType.External;
                }

                if (IsInteger)
                {
                    return InnerType.Integer;
                }

                if (IsBoolean)
                {
                    return InnerType.Boolean;
                }

                if (IsFloat)
                {
                    return InnerType.Float;
                }

                if (IsEnum)
                {
                    return InnerType.Enum;
                }

                if (IsString)
                {
                    return InnerType.String;
                }

                if (IsBinary)
                {
                    return InnerType.Binary;
                }

                if (IsDateTime)
                {
                    return InnerType.DateTime;
                }

                if (IsTimeSpan)
                {
                    return InnerType.TimeSpan;
                }

                throw new Exception("What the fuck?");
            }
        }

        internal UnionValue(object v, string desc = null)
        {
            _innerValue = v;
            Desc = desc;
            PrimitiveValue = null;
        }

        public UnionValue(PrimitiveData v, string desc = null)
        {
            _innerValue = null;
            Desc = desc;
            PrimitiveValue = v;
        }

        private static readonly HashSet<Type> sIntegerTypes = new HashSet<Type>
        {
            typeof(int), typeof(decimal),
            typeof(long), typeof(short), typeof(sbyte),
            typeof(byte), typeof(ulong), typeof(ushort),
            typeof(uint),
        };

        private static bool IsIntegerType(Type myType)
        {
            return myType != null && sIntegerTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (IsExternal)
            {
                return;
            }

            info.AddValue("TypeCode", (int) InnerTypeCode);
            if (IsEnum)
            {
                //枚举特殊处理
                info.AddValue("Value", Value.ToString());
                info.AddValue("Desc", Value.GetType().FullName);
            }
            else
            {
                info.AddValue("Value", Value, Value.GetType());
                info.AddValue("Desc", Desc);
            }
        }

        public UnionValue(SerializationInfo info, StreamingContext context)
        {
            var nt = (InnerType) info.GetInt32("TypeCode");
            Desc = info.GetString("Desc");
            _innerValue = null;
            PrimitiveValue = null;
            switch (nt)
            {
                case InnerType.Boolean:
                    _innerValue = info.GetBoolean("Value");
                    break;
                case InnerType.Integer:
                    _innerValue = info.GetInt64("Value");
                    break;
                case InnerType.Float:
                    _innerValue = info.GetDouble("Value");
                    break;
                case InnerType.DateTime:
                    _innerValue = info.GetDateTime("Value");
                    break;
                case InnerType.TimeSpan:
                    _innerValue = info.GetValue("Value", typeof(TimeSpan));
                    break;
                case InnerType.Enum:
                    var et = UnionValueSerializer.ReflectionGetType(Desc);
                    var sv = info.GetString("Value");
                    _innerValue = Enum.Parse(et, sv);
                    break;
                case InnerType.Binary:
                    _innerValue = info.GetValue("Value", typeof(byte[]));
                    break;
                case InnerType.String:
                    _innerValue = info.GetString("Value");
                    break;
                case InnerType.UnionValueMap:
                    _innerValue = info.GetValue("Value", typeof(UnionValueMap));
                    break;
                case InnerType.UnionValueArray:
                    _innerValue = info.GetValue("Value", typeof(UnionValueArray));
                    break;
                default:
                    throw new InvalidDataException();
            }
        }

        public bool IsInteger => IsIntegerType(_innerValue?.GetType()) || (PrimitiveValue?.IsInteger ?? false);

        public bool IsBoolean => _innerValue is bool || (PrimitiveValue?.IsBoolean ?? false);

        public bool IsFloat => _innerValue is double || _innerValue is float || (PrimitiveValue?.IsFloat ?? false);


        public bool IsBinary
        {
            get { return Value is byte[]; }
        }

        public bool IsPrimitive
        {
            get { return IsInteger || IsFloat || IsBoolean; }
        }

        public bool IsString
        {
            get { return Value is string; }
        }

        public bool IsEnum
        {
            get { return Value is Enum; }
        }

        public bool IsDateTime
        {
            get { return Value is DateTime; }
        }

        public bool IsTimeSpan
        {
            get { return Value is TimeSpan; }
        }

        public bool IsNative
        {
            get { return PrimitiveValue.HasValue || IsNativeObj(Value); }
        }

        public bool IsExternal
        {
            get { return !IsNative && !IsArray && !IsMap; }
        }

        public bool IsArray
        {
            get { return Value is UnionValueArray; }
        }

        public bool IsMap
        {
            get { return Value is UnionValueMap; }
        }

        public bool IsNull
        {
            get { return Value == null; }
        }

        public UnionValueMap Map => Value as UnionValueMap;

        public UnionValueArray Arr => Value as UnionValueArray;

        public override string ToString()
        {
            return (string) this;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (PrimitiveValue.HasValue)
                {
                    var vHash = PrimitiveValue.Value.IsInteger ? PrimitiveValue.Value.DoubleValue.GetHashCode() : PrimitiveValue.Value.LongValue.GetHashCode();
                    vHash *= 397;
                    return (vHash * 397) ^ (Desc != null ? Desc.GetHashCode() : 0);
                }

                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Desc != null ? Desc.GetHashCode() : 0);
            }
        }

        #region equals and compare

        public int CompareTo(object obj)
        {
            if (obj is UnionValue)
            {
                return CompareTo((UnionValue) obj);
            }

            var ret = ToUnionValue(obj);
            if (!ret.IsNull)
            {
                return CompareTo(ret);
            }

            return -1;
        }

        public int CompareTo(UnionValue other)
        {
            if (IsPrimitive && other.IsPrimitive)
            {
                return ((double) this).CompareTo((double) other);
            }

            if (IsEnum && other.IsEnum)
            {
                return ((Enum) this).CompareTo((Enum) other);
            }

            return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
        }

        public bool Equals(UnionValue other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is UnionValue && Equals((UnionValue) obj);
        }

        public bool DeepEquals(UnionValue other)
        {
            if (IsMap && other.IsMap && Map.Count == other.Map.Count)
            {
                using (var it = Map.GetEnumerator())
                using (var it2 = other.Map.GetEnumerator())
                {
                    while (it.MoveNext())
                    {
                        it2.MoveNext();
                        if (!it.Current.Key.DeepEquals(it2.Current.Key))
                        {
                            return false;
                        }

                        if (!it.Current.Value.DeepEquals(it2.Current.Value))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            if (IsArray && other.IsArray && Arr.Count == other.Arr.Count)
            {
                for (var i = 0; i < Arr.Count; i++)
                {
                    if (!Arr[i].DeepEquals(other.Arr[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return this == other;
        }

        #endregion

        #region element

        public int ElementCount
        {
            get
            {
                if (IsMap)
                {
                    return Map.Count;
                }

                if (IsArray)
                {
                    return Arr.Count;
                }

                return 0;
            }
        }

        public UnionValue this[UnionValue key]
        {
            get { return GetElement(key); }
            set { SetElement(key, value); }
        }

        public UnionValue GetElement(UnionValue key, UnionValue defaultValue)
        {
            if (IsMap)
            {
                UnionValue value;
                if (Map.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            else if (IsArray && key.IsInteger)
            {
                if ((int) key < Arr.Count)
                {
                    return Arr[(int) key];
                }
            }

            return defaultValue;
        }

        public UnionValue GetElement(UnionValue key)
        {
            return GetElement(key, UnionValue.Null);
        }

        public bool ContainsKey(UnionValue key)
        {
            return !GetElement(key).IsNull;
        }

        public bool ContainsValue(UnionValue value)
        {
            return !TrueForEachElement((k, v) => value != v);
        }

        public void SetElement(UnionValue key, UnionValue value)
        {
            if (IsMap)
            {
                Map[key] = value;
            }
            else if (IsArray && key.IsInteger)
            {
                if ((int) key >= Arr.Count)
                {
                    for (var i = (int) key; i > Arr.Count; i--)
                    {
                        Arr.Add(UnionValue.Null);
                    }

                    Arr.Add(value);
                }
                else
                {
                    Arr[(int) key] = value;
                }
            }
        }

        public void ForEachElement(Action<UnionValue, UnionValue> act)
        {
            if (IsArray)
            {
                for (var i = 0; i < Arr.Count; i++)
                {
                    act(i, Arr[i]);
                }
            }

            if (IsMap)
            {
                foreach (var entry in Map)
                {
                    act(entry.Key, entry.Value);
                }
            }
        }

        public bool TrueForEachElement(Func<UnionValue, UnionValue, bool> act)
        {
            if (IsArray)
            {
                for (var i = 0; i < Arr.Count; i++)
                {
                    if (!act(i, Arr[i]))
                    {
                        return false;
                    }
                }
            }

            if (IsMap)
            {
                foreach (var entry in Map)
                {
                    if (!act(entry.Key, entry.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public UnionValue TryArrayToMap(int startIndex = 0)
        {
            if (!IsArray)
            {
                return UnionValue.Null;
            }

            var ret = UnionValue.NewMap;
            for (var i = 0; i < Arr.Count; i++)
            {
                ret[startIndex + i] = Arr[i];
            }

            return ret;
        }

        public bool TryFloatToInt(out UnionValue ret)
        {
            if (!IsFloat)
            {
                ret = UnionValue.Null;
                return false;
            }

            var v = Convert.ToDouble(Value);
            var intV = (int) v;
            if (Math.Abs(v - intV) > 0.00001)
            {
                ret = UnionValue.Null;
            }
            else
            {
                ret = (int) this;
            }

            return !ret.IsNull;
        }

        private const int MaxConvertArraySize = ushort.MaxValue;

        //internal static CollectionPool Pool = new CollectionPool();

        public UnionValue TryMapToArray(bool force, int startIndex = 0, bool removeEmptyEnd = false)
        {
            if (!IsMap)
            {
                return UnionValue.Null;
            }

            var p = startIndex;
            //using (var ll = Pool.AllocList<UnionValue>())
            var arr = new UnionValueArray(ElementCount);
            foreach (var entry in Map)
            {
                if (!entry.Key.IsPrimitive)
                {
                    if (force)
                    {
                        continue;
                    }

                    return UnionValue.Null;
                }

                var index = (int) entry.Key;
                if (force)
                {
                    while (index != p++ && p < MaxConvertArraySize)
                    {
                        arr.Add(UnionValue.Null);
                    }
                }
                else if (index != p++)
                {
                    return UnionValue.Null;
                }

                arr.Add(entry.Value);
                if (p >= MaxConvertArraySize)
                {
                    break;
                }
            }


            if (removeEmptyEnd)
            {
                while (arr.Count > 0 && arr[arr.Count - 1].IsNull)
                {
                    arr.RemoveAt(arr.Count - 1);
                }
            }

            return new UnionValue(arr);
        }

        #endregion

        #region convert

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return (bool) this;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return (byte) this;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return (char) this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return (DateTime) this;
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return (decimal) this;
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return (double) this;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return (short) this;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return (int) this;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return (long) this;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return (sbyte) this;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return (float) this;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return (string) this;
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return (ushort) this;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return (uint) this;
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return (ulong) this;
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == null) throw new ArgumentNullException("conversionType is null");
            if (conversionType == typeof(byte[])) return (byte[]) this;
            if (conversionType == typeof(UnionValue)) return this;

            if (conversionType.IsInstanceOfType(Value))
            {
                return Value;
            }

            if (conversionType.IsEnum)
            {
                return Enum.Parse(conversionType, (string) this);
            }

            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.Boolean: return (bool) this;
                case TypeCode.Byte: return (byte) this;
                case TypeCode.Char: return (char) this;
                case TypeCode.DateTime: return (DateTime) this;
                case TypeCode.Decimal: return (decimal) this;
                case TypeCode.Double: return (double) this;
                case TypeCode.Int16: return (short) this;
                case TypeCode.Int32: return (int) this;
                case TypeCode.Int64: return (long) this;
                case TypeCode.SByte: return (sbyte) this;
                case TypeCode.Single: return (float) this;
                case TypeCode.String: return (string) this;
                case TypeCode.UInt16: return (ushort) this;
                case TypeCode.UInt32: return (uint) this;
                case TypeCode.UInt64: return (long) this;
                case TypeCode.Object: return this;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion

        #region operator 

        public static bool operator !=(UnionValue x, UnionValue y)
        {
            return !(x == y);
        }

        public static bool operator ==(UnionValue x, UnionValue y)
        {
            return Equals(x.Value, y.Value) && string.Equals(x.Desc, y.Desc);
        }


        public static implicit operator UnionValue(double value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(float value)
        {
            return (double) value;
        }

        public static implicit operator UnionValue(long value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(int value)
        {
            return (long) value;
        }

        public static implicit operator UnionValue(bool value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(string value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(Enum value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(DateTime value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(TimeSpan value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(byte[] value)
        {
            return new UnionValue(value);
        }

        public static implicit operator UnionValue(double? value)
        {
            return value == null ? Null : (UnionValue) value.GetValueOrDefault();
        }

        public static implicit operator UnionValue(long? value)
        {
            return value == null ? Null : (UnionValue) value.GetValueOrDefault();
        }

        public static implicit operator UnionValue(int? value)
        {
            return value == null ? Null : (UnionValue) value.GetValueOrDefault();
        }

        public static implicit operator UnionValue(bool? value)
        {
            return value == null ? Null : (UnionValue) value.GetValueOrDefault();
        }

        public static explicit operator double(UnionValue value)
        {
            if (!value.IsNull)
            {
                return Convert.ToDouble(value.Value);
            }

            return default(double);
        }

        public static explicit operator long(UnionValue value)
        {
            if (!value.IsNull)
            {
                return Convert.ToInt64(value.Value);
            }

            return default(long);
        }

        public static explicit operator int(UnionValue value)
        {
            return (int) (long) value;
        }

        public static explicit operator bool(UnionValue value)
        {
            return (bool) value.Value;
        }

        public static explicit operator string(UnionValue value)
        {
            return value.Value != null ? value.Value.ToString() : "null";
        }

        public static explicit operator Enum(UnionValue value)
        {
            if (value.IsEnum)
            {
                return (Enum) value.Value;
            }

            if (!string.IsNullOrEmpty(value.Desc))
            {
                var t = UnionValueSerializer.ReflectionGetType(value.Desc);
                return (Enum) Enum.Parse(t, (string) value);
            }

            throw new InvalidCastException();
        }

        public static explicit operator DateTime(UnionValue value)
        {
            if (value.IsDateTime)
            {
                return (DateTime) value.Value;
            }

            if (value.IsString)
            {
                return DateTime.Parse((string) value);
            }

            if (value.IsMap)
            {
                return new DateTime((int) value["Year"], (int) value["Month"], (int) value["Day"], (int) value["Hour"], (int) value["Minute"], (int) value["Second"]);
            }

            return default(DateTime);
        }

        public static explicit operator TimeSpan(UnionValue value)
        {
            return (TimeSpan) value.Value;
        }

        public static explicit operator byte[](UnionValue value)
        {
            return (byte[]) value.Value;
        }


        public static explicit operator float(UnionValue value)
        {
            if (!value.IsNull)
            {
                return Convert.ToSingle(value.Value);
            }

            return default(float);
        }

        public static explicit operator double?(UnionValue value)
        {
            return (double) value;
        }

        public static explicit operator long?(UnionValue value)
        {
            return (long) value;
        }

        public static explicit operator int?(UnionValue value)
        {
            return (int) value;
        }

        public static explicit operator bool?(UnionValue value)
        {
            return (bool) value;
        }

        #endregion
    }
}