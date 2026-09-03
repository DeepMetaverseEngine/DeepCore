using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.IO;

namespace DeepCore.Event.EventSystem
{
    public class EventMessageCodec : IOStreamPool, INetPackageCodec, IBinaryPackageCodec
    {
        public static readonly ArraySegment<byte> ZERO_BUFF = new ArraySegment<byte>(new byte[0]);
        protected readonly Logger log;

        new public MessageFactoryGenerator Factory
        {
            get { return base.Factory as MessageFactoryGenerator; }
        }

        public EventMessageCodec() : base(new MessageFactoryGenerator())
        {
            log = LoggerFactory.GetLogger(GetType().Name);
        }

        public void RegisterMessage<T>(int id) where T : IExternalizable
        {
            var t = typeof(T);
            RegisterMessag(t, id);
        }

        public void RegisterMessag(Type t, int id)
        {
            (Factory as MessageFactoryGenerator).RegistExternalizable(t, id);
        }

        public byte[] GZIP_HEADER = new byte[] { 1, 3, 5, 7 };

        public virtual bool doDecode(IInputStream input, out object message)
        {
            int typeInt = input.GetS32();
            Type type = Factory.GetType(typeInt);
            if (type == null)
            {
                log.Error("Unknow Protocol : >>>" + typeInt + "<<<");
                message = null;
                return false;
            }
            else
            {
                IExternalizable nm = (IExternalizable)ReflectionUtil.CreateInstance(type);
                nm.ReadExternal(input);
                message = nm;
                return true;
            }
        }

        public virtual bool doEncode(IOutputStream output, object message)
        {
            IExternalizable nm = (IExternalizable)message;
            int typeInt = Factory.GetTypeID(message.GetType());
            if (typeInt == 0)
            {
                log.Error("Unknow Protocol : >>>" + typeInt + "<<< - " + message.GetType().FullName);
                return false;
            }

            output.PutS32(typeInt);
            nm.WriteExternal(output);
            return true;
        }

        public bool doDecode(Stream input, out object message)
        {
            using (var reader = AllocInputAutoRelease(input))
            {
                return doDecode(reader, out message);
            }
        }

        public bool doEncode(Stream output, object message)
        {
            using (var os = AllocOutputAutoRelease(output))
            {
                return doEncode(os, message);
            }
        }

        public bool doDecode(ArraySegment<byte> input, out object message)
        {
            var zipTest = 0;
            for (var i = 0; i < GZIP_HEADER.Length && i < input.Count; i++)
            {
                if (GZIP_HEADER[i] == input.Array[i + input.Offset])
                {
                    zipTest++;
                }
            }

            if (zipTest == GZIP_HEADER.Length)
            {
                var bytes = new byte[input.Count - GZIP_HEADER.Length];
                Buffer.BlockCopy(input.Array, input.Offset + GZIP_HEADER.Length, bytes, 0, bytes.Length);
                input = new ArraySegment<byte>(IOUtil.Unzip(bytes));
            }

            using (var buffer = MemoryStreamObjectPool.AllocAutoRelease(input))
            using (var stream = AllocInputAutoRelease(buffer))
            {
                stream.GetStream().Position = 0;
                return doDecode(stream, out message);
            }
        }

        public byte[] DoEncodeUnionValue(UnionValue v)
        {
            using (var buffer = MemoryStreamObjectPool.AllocAutoRelease())
            using (var stream = AllocOutputAutoRelease(buffer))
            {
                UnionValueSerializer.WriteToStream(stream, v);
                byte[] binary = new byte[buffer.Position];
                Buffer.BlockCopy(buffer.GetBuffer(), 0, binary, 0, binary.Length);
                return binary;
            }
        }

        public UnionValue DoDecodeUnionValue(byte[] data)
        {
            using (var buffer = MemoryStreamObjectPool.AllocAutoRelease(data))
            using (var stream = AllocInputAutoRelease(buffer))
            {
                return UnionValueSerializer.ReadFromStream(stream);
            }
        }


        //100k
        public const int ZipMinByte = 1024 * 100;

        public bool doEncode(object message, out ArraySegment<byte> output)
        {
            using (var buffer = MemoryStreamObjectPool.AllocAutoRelease())
            using (var stream = AllocOutputAutoRelease(buffer))
            {
                var ret = doEncode(stream, message);
                if (ret)
                {
                    if (buffer.Position > ZipMinByte)
                    {
                        var src = IOUtil.Zip(buffer.GetBuffer());
                        var binary = new byte[src.Length + GZIP_HEADER.Length];
                        Buffer.BlockCopy(GZIP_HEADER, 0, binary, 0, GZIP_HEADER.Length);
                        Buffer.BlockCopy(src, 0, binary, GZIP_HEADER.Length, src.Length);
                        output = new ArraySegment<byte>(binary);
                    }
                    else
                    {
                        var binary = new byte[buffer.Position];
                        Buffer.BlockCopy(buffer.GetBuffer(), 0, binary, 0, binary.Length);
                        output = new ArraySegment<byte>(binary);
                    }
                }
                else
                {
                    output = ZERO_BUFF;
                }

                return ret;
            }
        }
    }
}