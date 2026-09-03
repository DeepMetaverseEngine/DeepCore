using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DeepCore.IO
{
    //--------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class IOStream : IDisposable
    {
        public static System.Text.UTF8Encoding UTF_ENCODING = new System.Text.UTF8Encoding(false);
        public const int NULL_MESSAGE_CODE = -1;
        public const int INVALID_MESSAGE_CODE = 0;
        public static int DEFAULT_ARRAY_LIMIT = UInt16.MaxValue;
        public static int DEFAULT_BYTES_LIMIT = 10 * 1024 * 1024;
        public int ARRAY_LIMIT;
        public int BYTES_LIMIT;
        public bool USE_VLQ { get; private set; } = false;
        public IExternalizableFactory Factory { get; private set; }
        public bool Statistics { get; set; } = false;
        public abstract long Position { get; set; }
        public abstract long Length { get; }
        public IOStream(IExternalizableFactory factory)
        {
            this.SetFactory(factory);
        }
        public void SetFactory(IExternalizableFactory factory)
        {
            this.Factory = factory;
            if (factory != null)
            {
                this.ARRAY_LIMIT = factory.ArrayLimit;
                this.BYTES_LIMIT = factory.BytesLimit;
                this.USE_VLQ = factory.UseVLQ;
            }
            else
            {
                this.ARRAY_LIMIT = DEFAULT_ARRAY_LIMIT;
                this.BYTES_LIMIT = DEFAULT_BYTES_LIMIT;
                this.USE_VLQ = false;
            }
        }
        protected virtual ArraySegment<byte> GetBufferSegment(int offset, int count)
        {
            throw new NotImplementedException();
        }
        #region IDisposable Support
        private bool disposedValue = false;
        protected abstract void Dispose(bool disposing);
        public void Dispose()
        {
            if (!disposedValue)
            {
                Dispose(true);
                disposedValue = true;
            }
        }
        #endregion
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public abstract class IOutputStream : IOStream
    {
        public IOutputStream(IExternalizableFactory factory) : base(factory)
        {

        }

        #region _Abstract_
        public abstract void PutBytes(byte[] bytes);
        public abstract void PutBytes(byte[] bytes, int offset, int length);
        public abstract void PutRawBytes(byte[] buff, int offset, int count);
        unsafe public abstract void PutRawBytes(byte* buff, int offset, int count);
        public abstract void PutBool(bool value);
        public abstract void PutU8(byte value);
        public abstract void PutS8(sbyte value);
        public abstract void PutU16(ushort value);
        public abstract void PutS16(short value);
        public abstract void PutU32(uint value);
        public abstract void PutS32(int value);
        public abstract void PutU64(ulong value);
        public abstract void PutS64(long value);
        public abstract void PutF32(float value);
        public abstract void PutF64(double value);
        public abstract void PutDEC(decimal value);
        public abstract void PutUnicode(char value);
        public abstract void PutUTF(string str);
        public abstract void PutVS32(int value);
        public abstract void PutVU32(uint value);
        public abstract void PutVS64(long value);
        public abstract void PutVU64(ulong value);

        public abstract void PutStruct<T>(in T value) where T : unmanaged;

        #endregion

        #region _Primitive_
        public virtual void PutFlag(bool flag, Action<IOutputStream> func)
        {
            PutBool(flag);
            if (flag) func(this);
        }
        public virtual void PutDateTime(DateTime time)
        {
            PutVS64(time.ToBinary());
        }

        public virtual void PutTimeSpan(TimeSpan time)
        {
            PutVU64((ulong)time.TotalMilliseconds);
        }
        public virtual void PutEnum8(ValueType enum8)
        {
            PutU8((byte)enum8);
        }
        public virtual void PutEnum32(ValueType enum32)
        {
            PutS32((int)enum32);
        }
        public virtual void PutEnum<T>(T enum8) where T : unmanaged
        {
            unsafe
            {
                switch (sizeof(T))
                {
                    case sizeof(byte):
                        PutU8(Convert.ToByte(enum8));
                        break;
                    case sizeof(short):
                        PutS16(Convert.ToInt16(enum8));
                        break;
                    case sizeof(int):
                        PutS32(Convert.ToInt32(enum8));
                        break;
                    case sizeof(long):
                        PutS64(Convert.ToInt64(enum8));
                        break;
                    default:
                        PutS32(Convert.ToInt32(enum8));
                        break;
                }
            }
        }
        public virtual void PutEnum8<T>(T enum8) where T : unmanaged
        {
            PutU8(Convert.ToByte(enum8));
        }
        public virtual void PutEnum32<T>(T enum32) where T : unmanaged
        {
            PutS32(Convert.ToInt32(enum32));
        }
        public virtual void PutBigInt(BigInteger big)
        {
            PutBytes(big.ToByteArray());
        }
        public virtual void PutValueType(Type type)
        {
            PutUTF(type?.FullName);
        }

        #endregion

        #region _ISerializable_  
        public void PutXmlObject(object value)
        {
            if (value == null)
            {
                PutS32(-1);
            }
            else
            {
                var utf = XmlUtil.ObjectToXmlString(value, "root");
                var bytes = CUtils.UTF8.GetBytes(utf);
                PutS32(bytes.Length);
                PutRawBytes(bytes, 0, bytes.Length);
            }
        }
        protected virtual bool ResolveHead(object value, out TypeCodec codec)
        {
            if (value != null)
            {
                var type = value.GetType();
                codec = Factory.GetCodec(type);
                if (codec != null)
                {
                    PutS32(codec.MessageID);
                    if (codec.MessageID == INVALID_MESSAGE_CODE)
                    {
                        PutUTF(type.FullName);
                        //throw new IOException("Can Not Encode Message : " + type);
                    }
                    return true;
                }
                else if (value is ISerializable)
                {
                    PutS32(INVALID_MESSAGE_CODE);
                    PutUTF(type.FullName);
                    return true;
                }
                else
                {
                    throw new IOException("Can Not Encode Message : " + type);
                }
            }
            else
            {
                codec = null;
                PutS32(NULL_MESSAGE_CODE);
                return false;
            }
        }
        public void Encode(TypeCodec codec, ISerializable value)
        {
            if (value is IWriteExternalizable ext)
            {
                EncodeExternalizable(in ext);
            }
            else if (value != null)
            {
                EncodeSerializable(codec, value);
            }
        }
        public void PutExt(IExternalizable value)
        {
            if (ResolveHead(value, out var codec))
            {
                EncodeExternalizable(value);
            }
        }
        public void PutSer(ISerializable value)
        {
            PutObj(value);
        }
        public virtual void PutObj(object value)
        {
            if (ResolveHead(value, out var codec))
            {
                if (value is IWriteExternalizable ext)
                {
                    EncodeExternalizable(in ext);
                }
                else if (codec != null)
                {
                    EncodeSerializable(codec, value);
                }
                else
                {
                    EncodeFields(value);
                }
            }
        }
        public void EncodeExternalizable(in IWriteExternalizable value)
        {
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeWrite(this);
                }
                value.WriteExternal(this);
                if (value is IAfterExternalizable after)
                {
                    after.AfterWrite(this);
                }
            }
        }
        public void EncodeSerializable(TypeCodec codec, in object value)
        {
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeWrite(this);
                }
                codec.DoWrite(this, value);
                if (value is IAfterExternalizable after)
                {
                    after.AfterWrite(this);
                }
            }
        }
        public void EncodeBinaryMessage(in BinaryMessage value)
        {
            PutRawBytes(value.Buffer, value.BufferOffset, value.BufferLength);
        }

        #endregion

        #region _Reflection_

        public virtual void TryPutStruct<T>(T? value) where T : unmanaged
        {
            if (!value.HasValue)
            {
                PutBool(false);
            }
            else
            {
                PutBool(true);
                PutStruct(value.Value);
            }
        }

        public virtual void TryPutObj<T>(T value, Action<IOutputStream, T> write)
        {
            if (value == null)
            {
                PutBool(false);
            }
            else
            {
                PutBool(true);
                write(this, value);
            }
        }
        public virtual void Encode<T>(T value, Action<T, IOutputStream> encode)
        {
            if (value == null)
            {
                PutBool(false);
            }
            else
            {
                PutBool(true);
                encode(value, this);
            }
        }
        public virtual void EncodeList<T>(ICollection<T> list, Action<T, IOutputStream> encode)
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                foreach (var value in list)
                {
                    Encode(value, encode);
                }
            }
        }


        /*
        public virtual void PutObj2Xml(object obj)
        {
            if (obj != null)
            {
                var xml = XmlUtil.ObjectToXml(obj);
                var utf = XmlUtil.ToString(xml);
                var len = UTF_ENCODING.GetByteCount(utf);
                using (var ms = MemoryStreamObjectPool.AllocAutoRelease(len))
                {
                    UTF_ENCODING.GetBytes(utf, 0, utf.Length, ms.GetBuffer(), 0);
                    PutVS32(len);
                    PutRawData(ms.GetBuffer(), 0, len);
                }
            }
            else
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
        }
        */
        public DataType PutRawData<T>(T value)
        {
            if (value == null)
            {
                PutU8((byte)DataType.NA);
                return DataType.NA;
            }
            return PutRawData(value.GetType(), value);
        }
        public DataType PutRawData(object value)
        {
            if (value == null)
            {
                PutU8((byte)DataType.NA);
                return DataType.NA;
            }
            return PutRawData(value.GetType(), value);
        }

        public virtual DataType PutRawData(Type type, object value)
        {
            if (value != null) { type = value.GetType(); }

            if (type == (typeof(bool)))
            {
                PutU8((byte)DataType.BOOL);
                PutBool((bool)value);
                return DataType.BOOL;
            }
            else if (type == (typeof(byte)))
            {
                PutU8((byte)DataType.U8);
                PutU8((byte)value);
                return DataType.U8;
            }
            else if (type == (typeof(sbyte)))
            {
                PutU8((byte)DataType.S8);
                PutS8((sbyte)value);
                return DataType.S8;
            }
            else if (type == (typeof(ushort)))
            {
                PutU8((byte)DataType.U16);
                PutU16((ushort)value);
                return DataType.U16;
            }
            else if (type == (typeof(short)))
            {
                PutU8((byte)DataType.S16);
                PutS16((short)value);
                return DataType.S16;
            }
            else if (type == (typeof(uint)))
            {
                PutU8((byte)DataType.U32);
                PutU32((uint)value);
                return DataType.U32;
            }
            else if (type == (typeof(int)))
            {
                PutU8((byte)DataType.S32);
                PutS32((int)value);
                return DataType.S32;
            }
            else if (type == (typeof(ulong)))
            {
                PutU8((byte)DataType.U64);
                PutU64((ulong)value);
                return DataType.U64;
            }
            else if (type == (typeof(long)))
            {
                PutU8((byte)DataType.S64);
                PutS64((long)value);
                return DataType.S64;
            }
            else if (type == (typeof(float)))
            {
                PutU8((byte)DataType.F32);
                PutF32((float)value);
                return DataType.F32;
            }
            else if (type == (typeof(double)))
            {
                PutU8((byte)DataType.F64);
                PutF64((double)value);
                return DataType.F64;
            }
            else if (type == (typeof(decimal)))
            {
                PutU8((byte)DataType.DEC);
                PutDEC((decimal)value);
                return DataType.F64;
            }
            else if (type == (typeof(char)))
            {
                PutU8((byte)DataType.UC);
                PutUnicode((char)value);
                return DataType.UC;
            }
            else if (type == (typeof(string)))
            {
                PutU8((byte)DataType.UTF);
                PutUTF((string)value);
                return DataType.UTF;
            }
            else if (type == (typeof(byte[])))
            {
                PutU8((byte)DataType.BIN);
                PutBytes((byte[])value);
                return DataType.BIN;
            }
            else if (type == (typeof(DateTime)))
            {
                PutU8((byte)DataType.DATETIME);
                PutDateTime((DateTime)value);
                return DataType.DATETIME;
            }
            else if (type == (typeof(TimeSpan)))
            {
                PutU8((byte)DataType.TIMESPAN);
                PutTimeSpan((TimeSpan)value);
                return DataType.TIMESPAN;
            }
            else if (type == (typeof(BigInteger)))
            {
                PutU8((byte)DataType.BIGINT);
                PutBigInt((BigInteger)value);
                return DataType.BIGINT;
            }
            else if (type.IsEnum)
            {
                PutU8((byte)DataType.ENUM);
                if (!Enum.IsDefined(type, value))
                {
                    throw new Exception($"Enum Value Not Defined : {type.FullName} = {value}");
                }
                PutEnumData(type, value);
                return DataType.ENUM;
            }
            else if (type.IsArray)
            {
                PutU8((byte)DataType.ARRAY);
                PutRawDataArray(type, value);
                return DataType.ARRAY;
            }
            else if (type.IsInterfaceOf(typeof(IExternalizable)))
            {
                PutU8((byte)DataType.EXT);
                PutExt((IExternalizable)value);
                return DataType.EXT;
            }
            else if (type.IsInterfaceOf(typeof(ISerializable)))
            {
                PutU8((byte)DataType.SER);
                PutObj((ISerializable)value);
                return DataType.SER;
            }
            else if (type.IsInterfaceOf(typeof(IList)))
            {
                PutU8((byte)DataType.LIST);
                PutRawDataList(type, value);
                return DataType.LIST;
            }
            else if (type.IsInterfaceOf(typeof(IDictionary)))
            {
                PutU8((byte)DataType.MAP);
                PutRawDataMap(type, value);
                return DataType.MAP;
            }
            else if (value is Type)
            {
                PutU8((byte)DataType.TYPE);
                PutValueType((Type)value);
                return DataType.TYPE;
            }
            else if (type.IsClass)
            {
                PutU8((byte)DataType.OBJ);
                PutRawDataFields(type, value);
                return DataType.OBJ;
            }
            else
            {
                PutU8((byte)DataType.NA);
                return DataType.NA;
            }
        }
        protected virtual void PutEnumData(Type type, object value)
        {
            PutValueType(value.GetType());
            var name = Enum.GetName(value.GetType(), value);
            PutUTF(name);
        }
        protected virtual void PutRawDataArray(Type type, object value)
        {
            var array = (Array)value;
            var rank = type.GetArrayRank();
            var ranges = new int[rank];
            var etype = type.GetElementType();
            int total_count = CUtils.GetArrayTotalCount(array);
            for (int i = 0; i < rank; i++)
            {
                ranges[i] = array.GetLength(i);
            }
            PutUTF(type.FullName);
            PutUTF(etype.FullName);
            PutVS32(total_count);
            PutArray(ranges, static (t, v) => t.PutVS32(v));
            foreach (var k in array)
            {
                PutRawData(etype, k);
            }
        }
        protected virtual void PutRawDataList(Type type, object value)
        {
            var list = (IList)value;
            var etype = (type.IsGenericType) ? type.GetGenericArguments()[0] : typeof(object);
            PutUTF(type.FullName);
            PutUTF(etype.FullName);
            PutVS32(list.Count);
            foreach (var k in list)
            {
                PutRawData(etype, k);
            }
        }
        protected virtual void PutRawDataMap(Type type, object value)
        {
            var map = (IDictionary)value;
            var ktype = typeof(object);
            var vtype = typeof(object);
            if (type.IsGenericType)
            {
                ktype = type.GetGenericArguments()[0];
                vtype = type.GetGenericArguments()[1];
            }
            PutUTF(type.FullName);
            PutUTF(ktype.FullName);
            PutUTF(vtype.FullName);
            PutVS32(map.Count);
            if (Factory.IsConsistency)
            {
                var keys = map.Keys.ToArray<object>();
                Array.Sort(keys, (a, b) => $"{a}".CompareTo($"{b}"));
                foreach (var eKey in keys)
                {
                    if (eKey == null)
                        throw new IOException("Map Key Can Not Be Null : " + type.FullName);
                    var eValue = map[eKey];
                    PutRawData(ktype, eKey);
                    PutRawData(vtype, eValue);
                }
            }
            else
            {
                foreach (DictionaryEntry e in map)
                {
                    if (e.Key == null)
                        throw new IOException("Map Key Can Not Be Null : " + type.FullName);
                    PutRawData(ktype, e.Key);
                    PutRawData(vtype, e.Value);
                }
            }
        }
        protected virtual void PutRawDataFields(Type type, object value)
        {
            PutUTF(type.FullName);
            EncodeFields(value);
        }
        protected virtual void EncodeFields(object value)
        {
            var type = value.GetType();
            var fields = type.GetFields();
            if (Factory.IsConsistency)
            {
                Array.Sort(fields, (a, b) => $"{a.Name}".CompareTo($"{b.Name}"));
            }
            foreach (var f in fields)
            {
                if (f.IsStatic == false && f.IsPublic)
                {
                    var fd = f.GetValue(value);
                    if (fd != null)
                    {
                        PutUTF(f.Name);
                        PutRawData(f.FieldType, fd);
                    }
                }
            }
            PutUTF(".");
        }

        #endregion

        #region _Collection_

        public void PutUTFArray(string[] array)
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    this.PutUTF(array[i]);
                }
            }
        }
        public void PutUTFList(IList<string> list)
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    this.PutUTF(list[i]);
                }
            }
        }
        public void PutNullable<T>(T? value, PutData<T> write) where T : struct
        {
            if (!value.HasValue)
            {
                PutBool(false);
            }
            else
            {
                PutBool(true);
                write(this, value.Value);
            }
        }
        public void PutArray<T>(T[] array, PutData<T> action)
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(this, array[i]);
                }
            }
        }
        public void PutArray<T>(T[,] array, PutData<T> action)
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int lenX = array.GetLength(0);
                int lenY = array.GetLength(1);
                if (lenX > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenX + " > " + ARRAY_LIMIT); }
                if (lenY > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenY + " > " + ARRAY_LIMIT); }
                PutVS32(lenX);
                PutVS32(lenY);
                for (int x = 0; x < lenX; x++)
                {
                    for (int y = 0; y < lenY; y++)
                    {
                        action.Invoke(this, array[x, y]);
                    }
                }
            }
        }
        public void PutArray<T>(T[,,] array, PutData<T> action)
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
                PutVS32(NULL_MESSAGE_CODE);
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int lenX = array.GetLength(0);
                int lenY = array.GetLength(1);
                int lenZ = array.GetLength(2);
                if (lenX > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenX + " > " + ARRAY_LIMIT); }
                if (lenY > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenY + " > " + ARRAY_LIMIT); }
                if (lenZ > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenZ + " > " + ARRAY_LIMIT); }
                PutVS32(lenX);
                PutVS32(lenY);
                PutVS32(lenZ);
                for (int x = 0; x < lenX; x++)
                {
                    for (int y = 0; y < lenY; y++)
                    {
                        for (int z = 0; z < lenZ; z++)
                        {
                            action.Invoke(this, array[x, y, z]);
                        }
                    }
                }
            }
        }

        public void PutArrayAny<T>(T[] array) where T : IExternalizable
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutExt(array[i]);
                }
            }
        }
        public void PutStructArray<T>(T[] list) where T : unmanaged
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutStruct(list[i]);
                }
            }
        }
        public void PutStructList<T>(IList<T> list) where T : unmanaged
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutStruct(list[i]);
                }
            }
        }

        public void PutList<T>(IList<T> list, PutData<T> action)
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(this, list[i]);
                }
            }
        }
        public void PutListAny<T>(IList<T> list) where T : IExternalizable
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutExt(list[i]);
                }
            }
        }
        public void PutMap<K, V>(IDictionary<K, V> map, PutData<K> k_action, PutData<V> v_action)
        {
            if (map == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = map.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(len);
                if (Factory.IsConsistency)
                {
                    var keys = map.Keys.ToArray();
                    Array.Sort(keys, (a, b) => $"{a}".CompareTo($"{b}"));
                    foreach (var eKey in keys)
                    {
                        if (eKey == null)
                            throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                        var eValue = map[eKey];
                        k_action.Invoke(this, eKey);
                        v_action.Invoke(this, eValue);
                    }
                }
                else
                {
                    foreach (var kv in map)
                    {
                        if (kv.Key == null)
                            throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                        k_action.Invoke(this, kv.Key);
                        v_action.Invoke(this, kv.Value);
                    }
                }

            }
        }


        public void PutRawDataArray(Array array)
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutRawData(array.GetValue(i));
                }
            }
        }
        public void PutRawDataList(IList list)
        {
            if (list == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = list.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                PutVS32(len);
                for (int i = 0; i < len; i++)
                {
                    PutRawData(list[i]);
                }
            }
        }
        public void PutRawDataMap(IDictionary map)
        {
            if (map == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = map.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(len);
                if (Factory.IsConsistency)
                {
                    var keys = map.Keys.ToArray<object>();
                    Array.Sort(keys, (a, b) => $"{a}".CompareTo($"{b}"));
                    foreach (var eKey in keys)
                    {
                        if (eKey == null)
                            throw new IOException("Map Key Can Not Be Null : " + map.GetType().FullName);
                        var eValue = map[eKey];
                        PutRawData(eKey);
                        PutRawData(eValue);
                    }
                }
                else
                {
                    foreach (DictionaryEntry kv in map)
                    {
                        if (kv.Key == null)
                            throw new IOException("Map Key Can Not Be Null : " + map.GetType().FullName);
                        PutRawData(kv.Key);
                        PutRawData(kv.Value);
                    }
                }
            }
        }



        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public void PutExtArrayNoHead<T>(T[] array) where T : IWriteExternalizable
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    EncodeExternalizable(array[i]);
                }
            }
        }
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public void PutExtListNoHead<T>(IList<T> array) where T : IWriteExternalizable
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    EncodeExternalizable(array[i]);
                }
            }
        }
        /*
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public void PutExtArrayNoHead<T>(T[] array) where T : struct, IExternalizable
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Length;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    EncodeExternalizable(array[i]);
                }
            }
        }
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public void PutExtListNoHead<T>(IList<T> array) where T : struct, IExternalizable
        {
            if (array == null)
            {
                PutVS32(NULL_MESSAGE_CODE);
            }
            else
            {
                int len = array.Count;
                if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
                this.PutVS32(array.Count);
                for (int i = 0; i < array.Count; i++)
                {
                    EncodeExternalizable(array[i]);
                }
            }
        }
        */
        #endregion

    }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public abstract class IInputStream : IOStream
    {
        public IInputStream(IExternalizableFactory factory) : base(factory)
        {

        }

        #region _Abstract_
        public abstract byte[] GetBytes();
        public abstract void GetRawBytes(byte[] buff, int offset, int count);
        unsafe public abstract void GetRawBytes(byte* buff, int offset, int count);
        public abstract bool GetBool();
        public abstract byte GetU8();
        public abstract sbyte GetS8();
        public abstract ushort GetU16();
        public abstract short GetS16();
        public abstract uint GetU32();
        public abstract int GetS32();
        public abstract ulong GetU64();
        public abstract long GetS64();
        public abstract float GetF32();
        public abstract double GetF64();
        public abstract decimal GetDEC();
        public abstract char GetUnicode();
        public abstract string GetUTF();
        public abstract Int32 GetVS32();
        public abstract UInt32 GetVU32();
        public abstract Int64 GetVS64();
        public abstract UInt64 GetVU64();
        public abstract T GetStruct<T>() where T : unmanaged;

        #endregion

        #region _Primitive_

        public virtual bool GetFlag(Action<IInputStream> func)
        {
            var flag = GetBool();
            if (flag) func(this);
            return flag;
        }
        public virtual DateTime GetDateTime()
        {
            long ms = GetVS64();
            return DateTime.FromBinary(ms);
        }
        public virtual TimeSpan GetTimeSpan()
        {
            ulong ms = GetVU64();
            return TimeSpan.FromMilliseconds(ms);
        }
        public virtual T GetEnum<T>() where T : unmanaged
        {
            unsafe
            {
                switch (sizeof(T))
                {
                    case sizeof(byte):
                        return (T)Enum.ToObject(typeof(T), Convert.ChangeType(GetU8(), Enum.GetUnderlyingType(typeof(T))));
                    case sizeof(short):
                        return (T)Enum.ToObject(typeof(T), Convert.ChangeType(GetS16(), Enum.GetUnderlyingType(typeof(T))));
                    case sizeof(int):
                        return (T)Enum.ToObject(typeof(T), Convert.ChangeType(GetS32(), Enum.GetUnderlyingType(typeof(T))));
                    case sizeof(long):
                        return (T)Enum.ToObject(typeof(T), Convert.ChangeType(GetS64(), Enum.GetUnderlyingType(typeof(T))));
                    default:
                        return (T)Enum.ToObject(typeof(T), Convert.ChangeType(GetS32(), Enum.GetUnderlyingType(typeof(T))));
                }
            }
        }
        public virtual void GetEnum<T>(out T ret) where T : unmanaged
        {
            ret = GetEnum<T>();
        }
        public virtual T GetEnum8<T>() where T : unmanaged
        {
            byte u8 = GetU8();
            var cov = Convert.ChangeType(u8, Enum.GetUnderlyingType(typeof(T)));
            return (T)Enum.ToObject(typeof(T), cov);
        }
        public virtual T GetEnum32<T>() where T : unmanaged
        {
            int s32 = GetS32();
            var cov = Convert.ChangeType(s32, Enum.GetUnderlyingType(typeof(T)));
            return (T)Enum.ToObject(typeof(T), cov);
        }
        public virtual BigInteger GetBigInt()
        {
            var bytes = GetBytes();
            return new BigInteger(bytes);
        }
        public virtual Type GetValueType()
        {
            var txt = GetUTF();
            return ReflectionUtil.GetType(txt);
        }

        #endregion

        #region _ISerializable_
        unsafe public T GetXmlObject<T>()
        {
            var count = GetS32();
            if (count > 0)
            {
                var bytes = stackalloc byte[count];
                GetRawBytes(bytes, 0, count);
                var xml = CUtils.UTF8.GetString(bytes, count);
                return XmlUtil.XmlTextToObject<T>(xml);
            }
            else
            {
                return default(T);
            }
        }

        public AbstractCollectionPool ObjectPool { get; set; }
        private string LastResolvedHead;

        protected object CreateInstance(TypeCodec codec, Type type)
        {
            if (ObjectPool != null)
            {
                return ObjectPool.AllocOrCreate(type, (codec, type), static (st, pool) =>
                {
                    if (st.codec != null) return st.codec.DoCreate(st.type);
                    return DeepActivator.CreateInstance(st.type);
                });
            }
            else
            {
                if (codec != null) return codec.DoCreate(type);
                return DeepActivator.CreateInstance(type);
            }
        }
        protected object MakeInstance(TypeCodec codec, object instance, Type type, string typeName, Type genericType)
        {
            if (type == null)
            {
                if (typeName != null)
                {
                    type = ReflectionUtil.GetType(typeName);
                }
                else if (genericType != null)
                {
                    type = genericType;
                    if (instance != null)
                    {
                        if (type.IsInstanceOfType(instance))
                        {
                            return instance;
                        }
                    }
                    return CreateInstance(codec, type);
                }
                else
                {
                    throw new Exception("Can Not Resolve Type Name : " + typeName);
                }
            }
            if (instance != null)
            {
                if (type.IsInstanceOfType(instance))
                {
                    return instance;
                }
            }
            if (genericType != null && genericType.IsGenericType && genericType.GetGenericTypeDefinition() == type)
            {
                return CreateInstance(null, genericType);
            }
            return CreateInstance(codec, type);
        }
        protected virtual bool ResolveHead(object instance, Type genericType, out object value, out TypeCodec codec)
        {
            var typeID = GetS32();
            if (typeID == NULL_MESSAGE_CODE)
            {
                value = null;
                codec = null;
                return false;
            }
            else if (typeID != INVALID_MESSAGE_CODE)
            {
                codec = Factory.GetCodec(typeID);
                if (codec != null)
                {
                    LastResolvedHead = codec.MessageType.FullName;
                    value = MakeInstance(codec, instance, codec.MessageType, null, genericType);
                    return true;
                }
                throw new IOException("Can Not Resolve Message : 0x" + typeID.ToString("X") + " : Last Resolved = " + LastResolvedHead);
            }
            else
            {
                var typeName = GetUTF();
                try
                {
                    codec = Factory.GetCodecByName(typeName);
                    if (codec != null)
                    {
                        value = MakeInstance(codec, instance, codec.MessageType, null, genericType);
                        LastResolvedHead = typeName;
                        return true;
                    }
                    else
                    {
                        value = MakeInstance(codec, instance, null, typeName, genericType);
                        LastResolvedHead = typeName;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    throw new IOException("Can Not Resolve Message : 0x" + typeID.ToString("X") + " : Type Name = " + typeName, e);
                }
            }
        }
        public ISerializable Decode(TypeCodec codec, ISerializable value)
        {
            if (value is IExternalizable)
            {
                return (ISerializable)DecodeExternalizable((IExternalizable)value);
            }
            else
            {
                return (ISerializable)DecodeSerializable(codec, value);
            }
        }
        public T GetExt<T>() where T : IExternalizable
        {
            if (ResolveHead(null, typeof(T), out var value, out var codec))
            {
                var any = (T)value;
                DecodeExternalizable(in any);
                return any;
            }
            return default(T);
        }
        public T GetExt<T>(T ret) where T : IExternalizable
        {
            if (ResolveHead(ret, typeof(T), out var value, out var codec))
            {
                var any = (T)value;
                DecodeExternalizable(in any);
                return any;
            }
            return default(T);
        }
        public IExternalizable GetExtAny()
        {
            if (ResolveHead(null, null, out var value, out var codec))
            {
                var any = (IExternalizable)value;
                DecodeExternalizable(in any);
                return any;
            }
            return null;
        }
        public ISerializable GetSer()
        {
            return GetObjAny() as ISerializable;
        }
        public virtual T GetObj<T>()
        {
            return GetObjAs<T>();
        }
        public T GetObj<T>(T ret)
        {
            if (ResolveHead(ret, typeof(T), out var value, out var codec))
            {
                var any = (T)value;
                if (any is IExternalizable ext)
                {
                    DecodeExternalizable(in ext);
                }
                else if (codec != null)
                {
                    DecodeSerializable(codec, in any);
                }
                else
                {
                    DecodeFields(any);
                }
                return any;
            }
            return default(T);
        }
        public virtual T GetObjAs<T>()
        {
            if (ResolveHead(null, typeof(T), out var value, out var codec))
            {
                var any = (T)value;
                if (any is IExternalizable ext)
                {
                    DecodeExternalizable(in ext);
                }
                else if (codec != null)
                {
                    DecodeSerializable(codec, in any);
                }
                else
                {
                    DecodeFields(any);
                }
                return any;
            }
            return default(T);
        }
        public virtual object GetObjAny()
        {
            if (ResolveHead(null, null, out var value, out var codec))
            {
                if (value is IExternalizable ext)
                {
                    DecodeExternalizable(in ext);
                }
                else if (codec != null)
                {
                    DecodeSerializable(codec, in value);
                }
                else
                {
                    DecodeFields(value);
                }
                return value;
            }
            return null;
        }

        public virtual IReadExternalizable DecodeExternalizable(in IReadExternalizable value)
        {
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeRead(this);
                }
                value.ReadExternal(this);
                if (value is IAfterExternalizable after)
                {
                    after.AfterRead(this);
                }
            }
            return value;
        }
        public virtual T DecodeExternalizable<T>(in T value) where T : IReadExternalizable
        {
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeRead(this);
                }
                value.ReadExternal(this);
                if (value is IAfterExternalizable after)
                {
                    after.AfterRead(this);
                }
            }
            return value;
        }
        public virtual object DecodeSerializable(TypeCodec codec, in object value)
        {
            LastResolvedHead = codec.MessageType.FullName;
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeRead(this);
                }
                codec.DoRead(this, value);
                if (value is IAfterExternalizable after)
                {
                    after.AfterRead(this);
                }
            }
            return value;
        }
        public virtual T DecodeSerializable<T>(TypeCodec codec, in T value)
        {
            LastResolvedHead = codec.MessageType.FullName;
            {
                if (value is IBeforeExternalizable before)
                {
                    before.BeforeRead(this);
                }
                codec.DoRead(this, value);
                if (value is IAfterExternalizable after)
                {
                    after.AfterRead(this);
                }
            }
            return value;
        }
        public virtual BinaryMessage DecodeBinaryMessage(int route, Type routeType, int offset, int count)
        {
            return BinaryMessage.CopyFrom(route, routeType, GetBufferSegment(offset, count));
        }

        #endregion

        #region _Reflection_

        public virtual bool TryGetStruct<T>(out T value) where T : unmanaged
        {
            if (GetBool())
            {
                value = GetStruct<T>();
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
        public virtual bool TryGetObj<T>(Func<IInputStream, T> read, out T value)
        {
            if (GetBool())
            {
                value = read(this);
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
        public virtual bool TryGetObj<T>(Action<IInputStream, T> read, out T value) where T : new()
        {
            if (GetBool())
            {
                value = new T();
                read(this, value);
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
        public T Decode<T>(Action<T, IInputStream> decode) where T : new()
        {
            var not_null = GetBool();
            if (not_null)
            {
                var ret = new T();
                decode(ret, this);
                return ret;
            }
            else
            {
                return default(T);
            }
        }
        public T Decode<T>(T value, Action<T, IInputStream> decode)
        {
            var not_null = GetBool();
            if (not_null)
            {
                decode(value, this);
                return value;
            }
            else
            {
                return default(T);
            }
        }
        public List<T> DecodeList<T>(Action<T, IInputStream> decode) where T : new()
        {
            var count = this.GetVS32();
            if (count == NULL_MESSAGE_CODE) return null;
            if (count > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + count + " > " + ARRAY_LIMIT); }
            var ret = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                ret.Add(this.Decode<T>(decode));
            }
            return ret;
        }
        public List<T> DecodeList<T>(Func<T> create, Action<T, IInputStream> decode)
        {
            var count = this.GetVS32();
            if (count == NULL_MESSAGE_CODE) return null;
            if (count > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + count + " > " + ARRAY_LIMIT); }
            var ret = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                ret.Add(this.Decode<T>(create(), decode));
            }
            return ret;
        }
        /*
        public T GetXml2Obj<T>(Action<Exception> error = null)
        {
            var ret = GetXml2Obj(error);
            if (ret != null)
            {
                return (T)ret;
            }
            else
            {
                return default(T);
            }
        }
        public virtual object GetXml2Obj(Action<Exception> error = null)
        {
            var len = GetVS32();
            if (len == NULL_MESSAGE_CODE || len == INVALID_MESSAGE_CODE)
            {
                return null;
            }
            else
            {
                using (var ms = MemoryStreamObjectPool.AllocAutoRelease(len))
                {
                    GetRawData(ms.GetBuffer(), 0, len);
                    var utf = UTF_ENCODING.GetString(ms.GetBuffer(), 0, len);
                    var xml = XmlUtil.FromString(utf);
                    var ser = new XmlSerializer();
                    if (error != null)
                    {
                        ser.OnError += error;
                    }
                    return ser.XmlToObject(xml);
                }
            }
        }
        */
        public object GetRawData()
        {
            DataType dt;
            return GetRawData(null, out dt);
        }
        public object GetRawData(out DataType type)
        {
            return GetRawData(null, out type);
        }
        public virtual object GetRawData(Type expect, out DataType type)
        {
            byte dt = GetU8();
            if (Enum.IsDefined(typeof(DataType), dt))
            {
                type = (DataType)dt;
                switch (type)
                {
                    case DataType.BOOL: return GetBool();
                    case DataType.U8: return GetU8();
                    case DataType.S8: return GetS8();
                    case DataType.U16: return GetU16();
                    case DataType.S16: return GetS16();
                    case DataType.U32: return GetU32();
                    case DataType.S32: return GetS32();
                    case DataType.U64: return GetU64();
                    case DataType.S64: return GetS64();
                    case DataType.F32: return GetF32();
                    case DataType.F64: return GetF64();
                    case DataType.DEC: return GetDEC();
                    case DataType.UC: return GetUnicode();
                    case DataType.UTF: return GetUTF();
                    case DataType.TYPE: return GetValueType();
                    case DataType.EXT: return GetExtAny();
                    case DataType.SER: return GetObjAny();
                    case DataType.BIN: return GetBytes();
                    case DataType.DATETIME: return GetDateTime();
                    case DataType.TIMESPAN: return GetTimeSpan();
                    case DataType.BIGINT: return GetBigInt();
                    case DataType.ENUM: return GetEnumData(expect);
                    case DataType.ARRAY: return GetArrayData(expect);
                    case DataType.LIST: return GetListData(expect);
                    case DataType.MAP: return GetMapData(expect);
                    case DataType.OBJ: return GetFieldsData(expect);
                }
            }
            type = DataType.NA;
            return null;
        }
        public T GetRawData<T>()
        {
            return (T)GetRawData(typeof(T), out var dt);
        }

        protected virtual object GetEnumData(Type expect)
        {
            var tname = GetValueType();
            var ename = GetUTF();
            try
            {
                if (tname == null) { throw new Exception("Can not resolve type : " + tname); }
                if (expect != tname && tname != null) { expect = tname; }
                return Enum.Parse(tname, ename, true);
            }
            catch { }
            return null;
        }
        protected virtual Array GetArrayData(Type expect)
        {
            var ctype_s = GetUTF();
            var etype_s = GetUTF();

            var ctype = expect != null ? expect : ReflectionUtil.GetType(ctype_s);
            if (ctype == null) { throw new Exception("Can not resolve type : " + ctype_s); }

            var etype = ctype.GetElementType() != null ? ctype.GetElementType() : ReflectionUtil.GetType(etype_s);
            if (etype == null) { throw new Exception("Can not resolve type : " + etype_s); }

            int total_count = GetVS32();
            int[] ranges = GetArray(static t => t.GetVS32());
            Array array = Array.CreateInstance(etype, ranges);
            int total_index = 0;
            for (int i = 0; i < total_count; i++)
            {
                DataType edt;
                object fd = GetRawData(etype, out edt);
                int[] indices = CUtils.GetArrayRankIndex(ranges, total_index);
                array.SetValue(fd, indices);
                total_index++;
            }
            return array;
        }
        protected virtual IList GetListData(Type expect)
        {
            var ctype_s = GetUTF();
            var etype_s = GetUTF();


            var ctype = expect != null ? expect : ReflectionUtil.GetType(ctype_s);
            Type etype = null;
            if (ctype == null)
            {
                var objType = typeof(List<>);
                etype = ReflectionUtil.GetType(etype_s);
                if (etype != null)
                {
                    ctype = objType.MakeGenericType(etype);
                }
            }
            else
            {
                etype = (ctype.IsGenericType) ? ctype.GetGenericArguments()[0] : ReflectionUtil.GetType(etype_s);
            }

            if (ctype == null)
            {
                throw new Exception("Can not resolve type : " + ctype_s);
            }

            if (etype == null)
            {
                throw new Exception("Can not resolve type : " + etype_s);
            }

            int count = GetVS32();
            IList list = null;
            if (expect != null && !expect.IsAbstract && !expect.IsInterface)
            {
                list = (IList)DeepActivator.CreateInstance(expect);
            }
            else
            {
                list = ReflectionUtil.CreateGenericArrayList(etype);
            }
            for (int i = 0; i < count; i++)
            {
                DataType edt;
                object fd = GetRawData(etype, out edt);
                list.Add(fd);
            }
            //             if (list is IList alist)
            //             {
            //                 alist.
            //             }
            return list;
        }
        protected virtual IDictionary GetMapData(Type expect)
        {
            var ctype_s = GetUTF();
            var ktype_s = GetUTF();
            var vtype_s = GetUTF();

            var ctype = expect != null ? expect : ReflectionUtil.GetType(ctype_s);
            if (ctype == null) { throw new Exception("Can not resolve type : " + ctype_s); }

            var ktype = (ctype.IsGenericType) ? ctype.GetGenericArguments()[0] : ReflectionUtil.GetType(ktype_s);
            if (ktype == null) { throw new Exception("Can not resolve type : " + ktype_s); }

            var vtype = (ctype.IsGenericType) ? ctype.GetGenericArguments()[1] : ReflectionUtil.GetType(vtype_s);
            if (vtype == null) { throw new Exception("Can not resolve type : " + vtype_s); }

            int count = GetVS32();
            IDictionary map = null;
            if (expect != null && !expect.IsAbstract && !expect.IsInterface)
            {
                map = (IDictionary)DeepActivator.CreateInstance(expect);
            }
            else
            {
                map = ReflectionUtil.CreateGenericHashMap(ktype, vtype);
            }
            //var map = ReflectionUtil.CreateGenericHashMap(ktype, vtype);
            for (int i = 0; i < count; i++)
            {
                DataType kdt;
                DataType vdt;
                object k = GetRawData(ktype, out kdt);
                object v = GetRawData(vtype, out vdt);
                map.Add(k, v);
            }
            return map;
        }
        protected virtual object GetFieldsData(Type expect)
        {
            var ctype_s = GetUTF();
            var ctype = expect != null ? expect : ReflectionUtil.GetType(ctype_s);
            if (ctype == null) { throw new Exception("Can not resolve type : " + ctype_s); }
            if (ctype.IsPrimitive) { throw new Exception("OBJ type is IsPrimitive : " + ctype_s); }
            var obj = DeepActivator.CreateInstance(ctype);
            DecodeFields(obj);
            return obj;
        }
        protected virtual object DecodeFields(object obj)
        {
            var ctype = obj.GetType();
            do
            {
                var fname = GetUTF();
                if (fname == ".")
                {
                    break;
                }
                var f = ctype.GetField(fname);
                if (f == null)
                {
                    throw new Exception(string.Format("Can not read field '{0}' in '{1}'", fname, this.GetType().FullName));
                }
                var fd = GetRawData(f.FieldType, out var fdt);
                if (fdt == DataType.NA || fd == null)
                {
                    throw new Exception(string.Format("Can not read field '{0}' in '{1}'", fname, this.GetType().FullName));
                }
                f.SetValue(obj, fd);
            }
            while (true);
            return obj;
        }

        #endregion
        //--------------------------------------------------------------------------------------------
        #region _Collection_
        #region UTF
        public string[] GetUTFArray()
        {
            int len = GetVS32();
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (len == NULL_MESSAGE_CODE) return null;
            string[] ret = new string[len];
            for (int i = 0; i < len; i++)
            {
                ret[i] = GetUTF();
            }
            return ret;
        }
        public int GetUTFList(IList<string> src)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            for (int i = 0; i < len; i++)
            {
                string d = GetUTF();
                src.Add(d);
            }
            if (src is List<string> list)
            {
                list.TrimExcess();
            }
            return len;
        }
        public List<string> GetUTFList(List<string> src = null)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (src != null) src.Clear();
            var ret = src ?? new List<string>(len);
            for (int i = 0; i < len; i++)
            {
                string d = GetUTF();
                ret.Add(d);
            }
            return ret;
        }
        //--------------------------------------------------------------------------------------------

        public L GetGenericUTFList<L>(L src = null)
            where L : class, IList<string>, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (src != null) src.Clear();
            var ret = src ?? new L();
            for (int i = 0; i < len; i++)
            {
                string d = GetUTF();
                ret.Add(d);
            }
            if (ret is List<string> list)
            {
                list.TrimExcess();
            }
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------------------------------
        #region Action
        public Nullable<T> GetNullable<T>(GetData<T> read) where T : struct
        {
            if (GetBool())
            {
                return read(this);
            }
            return null;
        }
        public T[] GetArray<T>(GetData<T> action)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            T[] ret = new T[len];
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(this);
                ret[i] = d;
            }
            return ret;
        }
        public T[] GetArray<T>(GetData<T> action, T[] phototype)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            T[] ret = new T[len];
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(this);
                ret[i] = d;
            }
            return ret;
        }
        public T[,] GetArray<T>(GetData<T> action, T[,] phototype)
        {
            int lenX = GetVS32();
            int lenY = GetVS32();
            if (lenX == NULL_MESSAGE_CODE) return null;
            if (lenY == NULL_MESSAGE_CODE) return null;
            if (lenX > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenX + " > " + ARRAY_LIMIT); }
            if (lenY > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenY + " > " + ARRAY_LIMIT); }
            var ret = new T[lenX, lenY];
            for (int x = 0; x < lenX; x++)
            {
                for (int y = 0; y < lenY; y++)
                {
                    var d = action.Invoke(this);
                    ret[x, y] = d;
                }
            }
            return ret;
        }
        public T[,,] GetArray<T>(GetData<T> action, T[,,] phototype)
        {
            int lenX = GetVS32();
            int lenY = GetVS32();
            int lenZ = GetVS32();
            if (lenX == NULL_MESSAGE_CODE) return null;
            if (lenY == NULL_MESSAGE_CODE) return null;
            if (lenZ == NULL_MESSAGE_CODE) return null;
            if (lenX > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenX + " > " + ARRAY_LIMIT); }
            if (lenY > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenY + " > " + ARRAY_LIMIT); }
            if (lenZ > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + lenZ + " > " + ARRAY_LIMIT); }
            var ret = new T[lenX, lenY, lenZ];
            for (int x = 0; x < lenX; x++)
            {
                for (int y = 0; y < lenY; y++)
                {
                    for (int z = 0; z < lenZ; z++)
                    {
                        var d = action.Invoke(this);
                        ret[x, y, z] = d;
                    }
                }
            }
            return ret;
        }

        public List<T> GetList<T>(GetData<T> action, List<T> ret = null)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new List<T>(len);
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(this);
                ret.Add(d);
            }
            return ret;
        }
        public int GetList<T>(GetData<T> action, IList<T> src)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            src.Clear();
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(this);
                src.Add(d);
            }
            if (src is List<T> list)
            {
                list.TrimExcess();
            }
            return len;
        }
        public L GetList<L, T>(GetData<T> action, L ret = null)
            where L : class, IList<T>, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new L();
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(this);
                ret.Add(d);
            }
            if (ret is List<T> list)
            {
                list.TrimExcess();
            }
            return ret;
        }
        //         public L GetList<L, T>(GetData<T> action, L ret = null)
        //             where L : class, IList<T>, new()
        //         {
        //             int len = GetVS32();
        //             if (len == NULL_MESSAGE_CODE) return null;
        //             if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
        //             if (ret == null) ret = new L();
        //             else ret.Clear();
        //             for (int i = 0; i < len; i++)
        //             {
        //                 T d = action.Invoke(this);
        //                 ret.Add(d);
        //             }
        //             if (ret is List<T> list)
        //             {
        //                 list.TrimExcess();
        //             }
        //             return ret;
        //         }

        //--------------------------------------------------------------------------------------------

        /// <summary>
        /// 为了减小泛型尺寸
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="action"></param>
        /// <param name="phototype"></param>
        /// <returns></returns>
        public Array GetObjArrayAny(Type elementType, GetData<ISerializable> action, Func<int, Array> newarray)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            var src = newarray(len);
            for (int i = 0; i < len; i++)
            {
                var d = action.Invoke(this);
                src.SetValue(d, i);
            }
            return src;
        }
        /// <summary>
        /// 为了减小泛型尺寸
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="action"></param>
        /// <param name="phototype"></param>
        /// <returns></returns>
        public IList GetObjListAny(Type elementType, GetData<ISerializable> action, Func<int, IList> newarray)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            var ret = newarray(len);
            for (int i = 0; i < len; i++)
            {
                var d = action.Invoke(this);
                ret.Add(d);
            }
            return ret;
        }

        //--------------------------------------------------------------------------------------------

        public HashMap<K, V> GetMap<K, V>(GetData<K> k_action, GetData<V> v_action, HashMap<K, V> ret = null)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new HashMap<K, V>(len);
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                K k = k_action.Invoke(this);
                V v = v_action.Invoke(this);
                if (k == null) 
                    throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                ret[k] = v;
            }
            return ret;
        }
        public int GetMap<K, V>(GetData<K> k_action, GetData<V> v_action, IDictionary<K, V> src)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            src.Clear();
            for (int i = 0; i < len; i++)
            {
                K k = k_action.Invoke(this);
                V v = v_action.Invoke(this);
                if (k == null) 
                    throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                src[k] = v;
            }
            return len;
        }
        public M GetMap<M, K, V>(GetData<K> k_action, GetData<V> v_action, M ret = null)
            where M : class, IDictionary<K, V>, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new M();
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                K k = k_action.Invoke(this);
                V v = v_action.Invoke(this);
                if (k == null)
                    throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                ret[k] = v;
            }
            return ret;
        }
        //         public M GetMap<M, K, V>(GetData<K> k_action, GetData<V> v_action)
        //             where M : class, IDictionary<K, V>, new()
        //         {
        //             int len = GetVS32();
        //             if (len == NULL_MESSAGE_CODE) return null;
        //             if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
        //             var ret = new M();
        //             for (int i = 0; i < len; i++)
        //             {
        //                 K k = k_action.Invoke(this);
        //                 V v = v_action.Invoke(this);
        //                 ret[k] = v;
        //             }
        //             return ret;
        //         }
        #endregion
        //--------------------------------------------------------------------------------------------
        #region Raw
        public object[] GetRawDataArray()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            var ret = new object[len];
            for (int i = 0; i < len; i++)
            {
                var d = GetRawData();
                ret[i] = d;
            }
            return ret;
        }

        public List<object> GetRawDataList(List<object> ret = null)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new List<object>(len);
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                var d = GetRawData();
                ret.Add(d);
            }
            return ret;
        }
        public int GetRawDataList(IList src)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            for (int i = 0; i < len; i++)
            {
                var d = GetRawData();
                src.Add(d);
            }
            return len;
        }
        public HashMap<object, object> GetRawDataMap<K, V>(HashMap<object, object> ret = null)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new HashMap<object, object>(len);
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                var k = GetRawData();
                var v = GetRawData();
                if (k == null)
                    throw new IOException("Map Key Can Not Be Null : " + typeof(K).FullName);
                ret[k] = v;
            }
            return ret;
        }
        public int GetRawDataMap(IDictionary src)
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            for (int i = 0; i < len; i++)
            {
                var k = GetRawData();
                var v = GetRawData();
                if (k == null)
                    throw new IOException("Map Key Can Not Be Null : " + src.GetType().FullName);
                src[k] = v;
            }
            return len;
        }
        #endregion
        //--------------------------------------------------------------------------------------------
        #region Ext
        public List<T> GetListAny<T>(List<T> src = null) where T : ISerializable
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (src != null)
            {
                src.Clear();
            }
            var ret = src ?? new List<T>(len);
            for (int i = 0; i < len; i++)
            {
                T ext = (T)GetObjAny();
                ret.Add(ext);
            }
            return ret;
        }
        public int GetListAny<T>(IList<T> src) where T : ISerializable
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            src.Clear();
            for (int i = 0; i < len; i++)
            {
                T ext = (T)GetObjAny();
                src.Add(ext);
            }
            if (src is List<T> list)
            {
                list.TrimExcess();
            }
            return len;
        }

        public T[] GetArrayAny<T>()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            var ret = new T[len];
            for (int i = 0; i < len; i++)
            {
                var ext = (T)GetObjAny();
                ret[i] = (ext);
            }
            return ret;
        }

        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public T[] GetExtArrayNoHead<T>() where T : IReadExternalizable, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            T[] array = new T[len];
            for (int i = 0; i < len; i++)
            {
                array[i] = new T();
                DecodeExternalizable(in array[i]);
            }
            return array;
        }

        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public List<T> GetExtListNoHead<T>(List<T> src = null) where T : IReadExternalizable, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (src != null)
            {
                src.Clear();
            }
            var ret = src ?? new List<T>(len);
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                ret.Add(item);
            }
            return ret;
        }

        public int GetExtListNoHead<T>(IList<T> src) where T : IReadExternalizable, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            src.Clear();
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                src.Add(item);
            }
            if (src is List<T> list)
            {
                list.TrimExcess();
            }
            return len;
        }
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public L GetExtListNoHead<L, T>(L ret)
             where T : IReadExternalizable, new()
             where L : class, IList<T>, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new L();
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                ret.Add(item);
            }
            if (ret is List<T> list)
            {
                list.TrimExcess();
            }
            return ret;
        }

        /*
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public T[] GetExtArrayNoHead<T>()            where T : struct, IExternalizable
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            T[] array = new T[len];
            for (int i = 0; i < len; i++)
            {
                DecodeExternalizable(in array[i]);
            }
            return array;
        }
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public List<T> GetExtListNoHead<T>() where T : struct, IExternalizable
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            List<T> ret = new List<T>(len);
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                ret.Add(item);
            }
            return ret;
        }
        public int GetExtListNoHead<T>(IList<T> src) where T : struct, IExternalizable
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return len;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                src.Add(item);
            }
            return len;
        }
        /// <summary>
        /// 不包括消息头，列表中元素必须保证类型一致
        /// </summary>
        public L GetGenericExtListNoHead<L, T>()
            where T : struct, IExternalizable
            where L : class, IList<T>, new()
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            var list = new L();
            for (int i = 0; i < len; i++)
            {
                T item = new T();
                DecodeExternalizable(in item);
                list.Add(item);
            }
            return list;
        }
        */
        #endregion
        //--------------------------------------------------------------------------------------------
        #region Struct
        public T[] GetStructArray<T>() where T : unmanaged
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            T[] array = new T[len];
            for (int i = 0; i < len; i++)
            {
                array[i] = GetStruct<T>();
            }
            return array;
        }
        public List<T> GetStructList<T>(List<T> ret = null) where T : unmanaged
        {
            int len = GetVS32();
            if (len == NULL_MESSAGE_CODE) return null;
            if (len > ARRAY_LIMIT) { throw new IOException("Collection overflow : " + len + " > " + ARRAY_LIMIT); }
            if (ret == null) ret = new List<T>(len);
            else ret.Clear();
            for (int i = 0; i < len; i++)
            {
                ret.Add(GetStruct<T>());
            }
            return ret;
        }
        #endregion
        //--------------------------------------------------------------------------------------------
        #endregion

    }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
}
