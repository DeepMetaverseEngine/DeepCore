using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using DeepCore.Xml;
using System.IO.Compression;
using DeepCore.Reflection;
using System.Collections;
using DeepCore.Log;

namespace DeepCore.IO
{

    public enum DataType : byte
    {
        NA = 0,
        U8 = 1,
        S8 = 2,
        U16 = 3,
        S16 = 4,
        U32 = 5,
        S32 = 6,
        U64 = 7,
        S64 = 8,
        F32 = 9,
        F64 = 10,
        DEC = 210,
        UTF = 11,
        EXT = 12,
        UC = 13,
        ENUM = 14,
        BOOL = 15,
        BIN = 16,
        DATETIME = 17,
        TIMESPAN = 18,
        BIGINT = 19,
        OBJ = 100,
        ARRAY = 101,
        LIST = 102,
        MAP = 103,
        SER = 104,
        TYPE = 105,
    }
    public delegate T GetData<T>(IInputStream input);
    public delegate void PutData<T>(IOutputStream output, T v);

    [Reflectible]
    public interface ISerializable
    {
    }

    [Reflectible]
    public interface IAfterExternalizable : ISerializable
    {
        void AfterWrite(IOutputStream output);
        void AfterRead(IInputStream input);
    }
    [Reflectible]
    public interface IBeforeExternalizable : ISerializable
    {
        void BeforeWrite(IOutputStream output);
        void BeforeRead(IInputStream input);
    }

    [Reflectible]
    public interface IXmlAfterExternalizable
    {
        void AfterEncode(XmlElement e);
        void AfterDecode(XmlElement e);
    }
    [Reflectible]
    public interface IXmlBeforeExternalizable
    {
        void BeforeEncode(XmlElement e);
        void BeforeDecode(XmlElement e);
    }

    [Reflectible]
    public interface IWriteExternalizable
    {
        void WriteExternal(IOutputStream output);
    }
    [Reflectible]
    public interface IReadExternalizable
    {
        void ReadExternal(IInputStream input);
    }

    [Reflectible]
    public interface IExternalizable : ISerializable, IWriteExternalizable, IReadExternalizable
    {

    }

    [Reflectible]
    public interface ISerializerFactory
    {
        IEnumerable<TypeCodec> AllTypes { get; }
        TypeCodec GetCodec(int id);
        TypeCodec GetCodec(Type type);
        TypeCodec GetCodecByName(string name);
    }

    [Reflectible]
    public interface IExternalizableFactory : ISerializerFactory
    {
        string CodeHash { get; }
        int ArrayLimit { get; }
        int BytesLimit { get; }
        bool UseVLQ { get; }
        /// <summary>
        /// 保持Dictionary有序，保持存储文件一致性
        /// </summary>
        bool IsConsistency { get; }
        int GetTypeID(Type type);
        Type GetType(int id);
        Type GetTypeByName(string name);
    }
    public class WarpExternalizableFactory : IExternalizableFactory
    {
        public IExternalizableFactory Codec { get; }
        public WarpExternalizableFactory(IExternalizableFactory warp)
        {
            Codec = warp;
        }
        public string CodeHash => Codec.CodeHash;
        public int ArrayLimit { get => Codec.ArrayLimit; }
        public int BytesLimit { get => Codec.BytesLimit; }
        public bool UseVLQ { get; set; } = false;
        public bool IsConsistency { get; set; } = false;

        public IEnumerable<TypeCodec> AllTypes { get => Codec.AllTypes; }


        public TypeCodec GetCodec(int id) { return Codec.GetCodec(id); }
        public TypeCodec GetCodec(Type type) { return Codec.GetCodec(type); }
        public TypeCodec GetCodecByName(string name) { return Codec.GetCodecByName(name); }
        public int GetTypeID(Type type) { return Codec.GetTypeID(type); }
        public Type GetType(int id) { return Codec.GetType(id); }
        public Type GetTypeByName(string name) { return Codec.GetTypeByName(name); }
    }

    /// <summary>
    /// 标记协议为服务内部通信，用于交换内存，不需要序列化。
    /// </summary>
    public interface IRpcNoneSerializable
    {

    }

    public class TypeCodec
    {
        public readonly Type MessageType;
        public readonly int MessageID;
        public readonly Action<IInputStream, object> DoRead;
        public readonly Action<IOutputStream, object> DoWrite;
        public readonly Func<Type, object> DoCreate;
        public TypeCodec(Type type, int msg_id,
            Func<Type, object> do_create,
            Action<IInputStream, object> do_read,
            Action<IOutputStream, object> do_write)
        {
            if (do_create == null)
                throw new NotImplementedException($"{type.FullName} :  do_create");
            if (do_read == null)
                throw new NotImplementedException($"{type.FullName} :  do_read");
            if (do_write == null)
                throw new NotImplementedException($"{type.FullName} :  do_write");
            this.MessageType = type;
            this.MessageID = msg_id;
            this.DoCreate = do_create;
            this.DoRead = do_read;
            this.DoWrite = do_write;
        }
        public override string ToString()
        {
            return string.Format("TypeCodec : {0} ({1})", MessageType.FullName, MessageID);
        }
        public static int GetAttributeRoute(Type type)
        {
            var attr = PropertyUtil.GetAttribute<MessageTypeAttribute>(type);
            if (attr != null) return attr.MessageTypeID;
            return IInputStream.INVALID_MESSAGE_CODE;
        }
    }


    public class BinaryEncoder : Disposable
    {
        private MemoryStream stream;
        private OutputStream output;
        private IExternalizableFactory codec;
        public IExternalizableFactory Codec { get => codec; }
        public BinaryEncoder(IExternalizableFactory codec)
        {
            this.stream = new MemoryStream();
            this.output = new OutputStream(stream, codec);
            this.codec = codec;
        }
        protected override void Disposing()
        {
            output.Dispose();
        }
        public byte[] Encode(params object[] data)
        {
            Begin();
            EncodeNext(data);
            return ToArray();
        }
        public byte[] Encode(IEnumerable data)
        {
            Begin();
            EncodeNext(data);
            return ToArray();
        }

        public BinaryEncoder Begin()
        {
            stream.Position = 0;
            stream.SetLength(0);
            return this;
        }
        public void EncodeNext(params object[] data)
        {
            foreach (var c in data)
            {
                output.PutObj(c);
            }
        }
        public void EncodeNext(IEnumerable data)
        {
            foreach (var c in data)
            {
                output.PutObj(c);
            }
        }
        public byte[] ToArray()
        {
            return stream.ToArray();
        }
    }
    public class BinaryDecoder : Disposable
    {
        private MemoryStream stream;
        private InputStream input;
        private IExternalizableFactory codec;
        public IExternalizableFactory Codec { get => codec; }
        public BinaryDecoder(IExternalizableFactory codec)
        {
            this.stream = new MemoryStream();
            this.input = new InputStream(stream, codec);
            this.codec = codec;
        }
        protected override void Disposing()
        {
            input.Dispose();
        }
        public T DecodeOne<T>(byte[] data)
        {
            Begin(data);
            return DecodeNextOne<T>();
        }
        public T[] DecodeAll<T>(byte[] data)
        {
            Begin(data);
            return DecodeNextAll<T>();
        }
        public BinaryDecoder Begin(byte[] data)
        {
            stream.Position = 0;
            stream.SetLength(data.Length);
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            return this;
        }
        public T DecodeNextOne<T>()
        {
            return input.GetObj<T>();
        }
        public T[] DecodeNextAll<T>()
        {
            var retlist = new ArrayList<T>();
            while (stream.Position < stream.Length)
            {
                var c = input.GetObj<T>();
                retlist.Add(c);
            }
            return retlist.ToArray();
        }
    }
}

