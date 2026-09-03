using DeepCore.FuncData;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepMetaGame.Data
{
    public class BattleCodec : IOStreamPool, INetPackageCodec
    {
        protected readonly Logger log;
        public static readonly ArraySegment<byte> ZERO_BUFF = new ArraySegment<byte>(new byte[0]);
        protected readonly int FIXED_HEADER_SIZE = 4;
        protected readonly int DEFAULT_BUFFER_SIZE = 1024;
        private readonly TemplateManager templates = null;
        public BattleCodec(TemplateManager templates, bool singleThread = false)
            : base(templates.DataFactory.MessageCodec, singleThread)
        {
            log = LoggerFactory.GetLogger(GetType().Name);
            this.templates = templates;
        }
        public TemplateManager Templates
        {
            get { return templates; }
        }
        public virtual bool doDecode(IInputStream input, out object message)
        {
            message = input.GetObjAny();
            if (message is IBattleMessage bm)
            {
                bm.EndRead(templates);
            }
            return message != null;
        }
        public virtual bool doEncode(IOutputStream output, object message)
        {
            if (message is IBattleMessage bm)
            {
                bm.BeforeWrite(templates);
            }
            output.PutObj(message);
            return message != null;
        }

        public virtual bool DoDecode(Stream input, out object message)
        {
            using (var reader = AllocInputAutoRelease(input))
            {
                return doDecode(reader, out message);
            }
        }
        public virtual bool DoEncode(Stream output, object message)
        {
            using (var os = AllocOutputAutoRelease(output))
            {
                return doEncode(os, message);
            }
        }
        public virtual bool DoDecode(ArraySegment<byte> input, out object message)
        {
            using (var buffer = AllocStream(input))
            using (var stream = AllocInputAutoRelease(buffer))
            {
                return doDecode(stream, out message);
            }
        }
        public virtual bool DoEncode(object message, out ArraySegment<byte> output)
        {
            using (var buffer = AllocStream())
            using (var stream = AllocOutputAutoRelease(buffer))
            {
                var ret = doEncode(stream, message);
                if (ret)
                {
                    output = new ArraySegment<byte>(buffer.ToArray());
                }
                else
                {
                    output = ZERO_BUFF;
                }
                return ret;
            }
        }

        public bool doEncodeTo(object message, out byte[] bytes)
        {
            using (var buffer = AllocStream())
            using (var stream = AllocOutputAutoRelease(buffer))
            {
                var mstream = stream.GetStream() as DeepCore.IO.MemoryStream;
                stream.PutS32(0);
                var ret = doEncode(stream, message);
                if (ret)
                {
                    int len = (int)mstream.Position;
                    mstream.Position = 0;
                    stream.PutS32(len - 4);
                    bytes = buffer.ToArray();
                }
                else
                {
                    bytes = null;
                }
                return ret;
            }
        }
        public bool doEncodeTo(object message, Stream buffer)
        {
            using (var stream = AllocOutputAutoRelease(buffer))
            {
                var mstream = stream.GetStream() as DeepCore.IO.MemoryStream;
                var oldp = (int)mstream.Position;
                stream.PutS32(0);
                var ret = doEncode(stream, message);
                if (ret)
                {
                    int len = (int)(mstream.Position - oldp);
                    mstream.Position = oldp;
                    stream.PutS32(len - 4);
                }
                mstream.Position = oldp;
                return ret;
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------------------------------------
        public T Clone<T>(T src)
        {
            using (var stream = AllocStream())
            {
                DoEncode(stream, src);
                stream.Position = 0;
                DoDecode(stream, out var dst);
                return (T)dst;
            }
        }
        public int DecodeAll<T>(Stream stream, IList<T> retlist)
        {
            int count = 0;
            using (var reader = AllocInputAutoRelease(stream))
            {
                while (stream.Position < stream.Length)
                {
                    if (doDecode(reader, out var c))
                    {
                        retlist.Add((T)c);
                        count++;
                    }
                }
            }
            return count;
        }
        public T[] DecodeAll<T>(byte[] data)
        {
            var objs = new List<T>();
            using (var stream = AllocStream(data))
            {
                DecodeAll(stream, objs);
            }
            return objs.ToArray();
        }
        public T DecodeOne<T>(Stream stream)
        {
            using (var reader = AllocInputAutoRelease(stream))
            {
                while (stream.Position < stream.Length)
                {
                    if (doDecode(reader, out var c))
                    {
                        return (T)c;
                    }
                }
            }
            return default;
        }
        public T DecodeOne<T>(byte[] data)
        {
            using (var stream = AllocStream(data))
            {
                return DecodeOne<T>(stream);
            }
        }

        public int EncodeTo<T>(Stream stream, IEnumerable<T> array)
        {
            int count = 0;
            using (var writer = AllocOutputAutoRelease(stream))
            {
                foreach (var obj in array)
                {
                    if (doEncode(writer, obj))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        public int EncodeTo<T>(Stream stream, params T[] array)
        {
            return EncodeTo(stream, (IEnumerable<T>)array);
        }
        public byte[] Encode<T>(IEnumerable<T> array)
        {
            using (var stream = AllocStream())
            {
                EncodeTo(stream, array);
                stream.Flush();
                return stream.ToArray();
            }
        }
        public byte[] Encode<T>(params T[] array)
        {
            using (var stream = AllocStream())
            {
                EncodeTo(stream, array);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }

    public static class PositionCodec
    {
        //----------------------------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------------------------------
        public static void WritePos(this IOutputStream output, float x)
        {
            output.PutF32(x);
        }
        public static void WritePos(this IOutputStream output, float x, float y, float z)
        {
            output.PutF32(x);
            output.PutF32(y);
            output.PutF32(z);
        }
        public static void WritePos(this IOutputStream output, in Vector3 v)
        {
            output.PutF32(v.X);
            output.PutF32(v.Y);
            output.PutF32(v.Z);
        }
        public static void WritePos(this IOutputStream output, Vector3 v)
        {
            output.PutF32(v.X);
            output.PutF32(v.Y);
            output.PutF32(v.Z);
        }
        public static void WriteDirection(this IOutputStream output, float direction)
        {
            //output.PutU8(ToDirectionD8(direction));
            output.PutF32(direction);
        }
        public static void WriteRotation(this IOutputStream output, Quaternion rotation)
        {
            output.PutF32(rotation.X);
            output.PutF32(rotation.Y);
            output.PutF32(rotation.Z);
            output.PutF32(rotation.W);
        }
        public static void WritePosAndDirection(this IOutputStream output, in Vector3 v, float direction)
        {
            output.PutF32(v.X);
            output.PutF32(v.Y);
            output.PutF32(v.Z);
            // output.PutU8(ToDirectionD8(direction));
            output.PutF32(direction);
        }
        public static void WritePosAndDirection(this IOutputStream output, Vector3 v, float direction)
        {
            output.PutF32(v.X);
            output.PutF32(v.Y);
            output.PutF32(v.Z);
            //  output.PutU8(ToDirectionD8(direction)); 
            output.PutF32(direction);
        }
        //----------------------------------------------------------------------------------------------------------------------
        public static float ReadPos1D(this IInputStream input)
        {
            return input.GetF32();
        }
        public static Vector3 ReadPos3D(this IInputStream input)
        {
            return new Vector3(
                input.GetF32(),
                input.GetF32(),
                input.GetF32());
        }
        public static float ReadDirection(this IInputStream input)
        {
            //return ToDirectionF32(input.GetU8());
            return input.GetF32();
        }

        public static void ReadPos(this IInputStream input, out float x)
        {
            x = input.GetF32();
        }
        public static void ReadPos(this IInputStream input, out Vector3 v)
        {
            v = new Vector3(
                input.GetF32(),
                input.GetF32(),
                input.GetF32());
        }
        public static void ReadPos(this IInputStream input, out float x, out float y, out float z)
        {
            x = input.GetF32();
            y = input.GetF32();
            z = input.GetF32();
        }
        public static void ReadDirection(this IInputStream input, out float direction)
        {
            //  direction = ToDirectionF32(input.GetU8());
            direction = input.GetF32();
        }
        public static void ReadRotation(this IInputStream input, out Quaternion rotation)
        {
            rotation = new Quaternion(
                input.GetF32(),
                input.GetF32(),
                input.GetF32(),
                input.GetF32());
        }
        public static void ReadPosAndDirection(this IInputStream input, out Vector3 v, out float direction)
        {
            v = new Vector3(
                input.GetF32(),
                input.GetF32(),
                input.GetF32());
            //   direction = ToDirectionF32(input.GetU8());
            direction = input.GetF32();
        }
    }

    //     public class IInputStream : IOStreamPool.WrapperIn
    //     {
    //         public bool IsHalf { get; }
    //         public IInputStream(IOStreamPool pool, IExternalizableFactory factory) : base(pool, factory)
    //         {
    //         }
    //     }
    //     public class IOutputStream : IOStreamPool.WrapperOut
    //     {
    //         public bool IsHalf { get; }
    //         public IOutputStream(IOStreamPool pool, IExternalizableFactory factory) : base(pool, factory)
    //         {
    //         }
    //     }
}
