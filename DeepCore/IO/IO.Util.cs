using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using DeepCore.Xml;
using System.IO.Compression;
using DeepCore.Reflection;
using System.Diagnostics;
using System.Buffers;
using System.Threading.Tasks;
using System.Collections;
using System.Numerics;

namespace DeepCore.IO
{

    public class EOFException : IOException
    {
        public EOFException(string message) : base(message) { }
        public EOFException(string message, Exception inner) : base(message, inner) { }
    }

    public static class IOUtil
    {
        //-------------------------------------------------------------------------------------------------------------
        #region FileHead
        public static void SaveFileHead(this IOutputStream output, string head)
        {
            foreach (var c in head)
            {
                output.PutUnicode(c);
            }
        }
        public static bool TryLoadFileHead(this IInputStream input, string head)
        {
            var ret = true;
            var buff = new char[head.Length];
            for (int i = 0; i < head.Length; i++)
            {
                buff[i] = input.GetUnicode();
                if (buff[i] != head[i]) { ret = false; }
            }
            return ret;
        }


        public static void SaveFileHead(this IOutputStream output, byte[] head)
        {
            output.PutRawBytes(head, 0, head.Length);
        }
        public static bool TryLoadFileHead(this IInputStream input, byte[] head)
        {
            var buff = new byte[head.Length];
            input.GetRawBytes(buff, 0, head.Length);
            for (int i = 0; i < head.Length; i++)
            {
                if (buff[i] != head[i]) { return false; }
            }
            return true;
        }


        public static void SaveFileHeadASCII(this IOutputStream output, string headASCII)
        {
            SaveFileHead(output, headASCII.ToCharArray().Convert1D((i, c) => (byte)c));
        }
        public static bool TryLoadFileHeadASCII(this IInputStream input, string headASCII)
        {
            var buff = new byte[headASCII.Length];
            if (TryLoadFileHead(input, buff))
            {
                var ascii = new string(buff.Convert1D((i, c) => (char)c));
                return headASCII == ascii;
            }
            return false;
        }



        public static bool TryPickFileHead(this IInputStream input, byte[] head)
        {
            var oldp = input.Position;
            try
            {
                return TryLoadFileHead(input, head);
            }
            finally
            {
                input.Position = oldp;
            }
        }
        public static bool TryPickFileHeadASCII(this IInputStream input, string headASCII)
        {
            return TryPickFileHead(input, headASCII.ToCharArray().Convert1D((i, c) => (byte)c));
        }
        public static byte[] GetFileHead(this IInputStream input, int bytes)
        {
            var head_trunk = new byte[bytes];
            input.GetRawBytes(head_trunk, 0, head_trunk.Length);
            return head_trunk;
        }
        public static string GetFileHeadASCII(this IInputStream input, int bytes)
        {
            var head_trunk = new byte[bytes];
            input.GetRawBytes(head_trunk, 0, head_trunk.Length);
            return new string(head_trunk.Convert1D((i, c) => (char)c));
        }
        public static bool TryValidateFileHead(this IInputStream input, byte[] head_start, out byte[] readed)
        {
            readed = GetFileHead(input, head_start.Length);
            return CUtils.ArraysEqual(readed, head_start);
        }
        public static bool TryValidateFileHeadASCII(this IInputStream input, string head_start, out string readed)
        {
            readed = GetFileHeadASCII(input, head_start.Length);
            return head_start == readed;
        }


        #endregion
        //-------------------------------------------------------------------------------------------------------------

        public static DataType GetRawDataType(Type type)
        {
            if (type == (typeof(bool))) return DataType.BOOL;
            else if (type == (typeof(byte))) return DataType.U8;
            else if (type == (typeof(sbyte))) return DataType.S8;
            else if (type == (typeof(ushort))) return DataType.U16;
            else if (type == (typeof(short))) return DataType.S16;
            else if (type == (typeof(uint))) return DataType.U32;
            else if (type == (typeof(int))) return DataType.S32;
            else if (type == (typeof(ulong))) return DataType.U64;
            else if (type == (typeof(long))) return DataType.S64;
            else if (type == (typeof(float))) return DataType.F32;
            else if (type == (typeof(double))) return DataType.F64;
            else if (type == (typeof(decimal))) return DataType.F64;
            else if (type == (typeof(char))) return DataType.UC;
            else if (type == (typeof(string))) return DataType.UTF;
            else if (type == (typeof(byte[]))) return DataType.BIN;
            else if (type == (typeof(DateTime))) return DataType.DATETIME;
            else if (type == (typeof(TimeSpan))) return DataType.TIMESPAN;
            else if (type == (typeof(BigInteger))) return DataType.BIGINT;
            else if (type.IsEnum) return DataType.ENUM;
            else if (type.IsArray) return DataType.ARRAY;
            else if (type.IsInterfaceOf(typeof(IExternalizable))) return DataType.EXT;
            else if (type.IsInterfaceOf(typeof(ISerializable))) return DataType.SER;
            else if (type.IsInterfaceOf(typeof(IList))) return DataType.LIST;
            else if (type.IsInterfaceOf(typeof(IDictionary))) return DataType.MAP;
            else if (type.IsClass) return DataType.OBJ;
            else return DataType.NA;
        }
        public static Type GetRawType(this DataType type)
        {
            switch (type)
            {
                case DataType.BOOL: return typeof(bool);
                case DataType.U8: return typeof(byte);
                case DataType.S8: return typeof(sbyte);
                case DataType.U16: return typeof(ushort);
                case DataType.S16: return typeof(short);
                case DataType.U32: return typeof(uint);
                case DataType.S32: return typeof(int);
                case DataType.U64: return typeof(ulong);
                case DataType.S64: return typeof(long);
                case DataType.F32: return typeof(float);
                case DataType.F64: return typeof(double);
                case DataType.DEC: return typeof(decimal);
                case DataType.UC: return typeof(char);
                case DataType.UTF: return typeof(string);
                case DataType.BIN: return typeof(byte[]);
                case DataType.DATETIME: return typeof(DateTime);
                case DataType.TIMESPAN: return typeof(TimeSpan);
                case DataType.BIGINT: return typeof(BigInteger);
                case DataType.ENUM: return typeof(Enum);
                case DataType.ARRAY: return typeof(Array);
                case DataType.EXT: return typeof(IExternalizable);
                case DataType.SER: return typeof(ISerializable);
                case DataType.LIST: return typeof(IList);
                case DataType.MAP: return typeof(IDictionary);
                case DataType.OBJ: return typeof(object);
                default:
                    return null;
            }
        }




        public static readonly ArraySegment<byte> ZERO_BYTES = new ArraySegment<byte>(new byte[0]);

        public static string ReadAllText(Stream input, Encoding encoding = null)
        {
            if (encoding == null) { encoding = CUtils.UTF8; }
            byte[] data = ReadToEnd(input);
            return encoding.GetString(data);
        }
        public static byte[] ReadAllBytes(Stream input)
        {
            return ReadToEnd(input);
        }


        public static ArraySegment<byte> ToArraySegment(this MemoryStream stream, bool copy)
        {
            if (copy)
            {
                return new ArraySegment<byte>(stream.ToArray());
            }
            return new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length);
        }
        public static ArraySegment<byte> ToArraySegment(this MemoryStream stream, int offset, int length, bool copy)
        {
            if (copy)
            {
                var ret = new ArraySegment<byte>(new byte[length]);
                Buffer.BlockCopy(stream.GetBuffer(), offset, ret.Array, 0, length);
                return ret;
            }
            return new ArraySegment<byte>(stream.GetBuffer(), offset, length);
        }

        public static byte TryReadByte(Stream data)
        {
            int b = data.ReadByte();
            if (b < 0)
            {
                throw new EOFException("EOF of stream");
            }
            return (byte)b;
        }
        public static void GetAllSubTypes(ISerializerFactory codec, Type base_type, List<TypeCodec> sub_types)
        {
            foreach (var type in codec.AllTypes)
            {
                if (type.MessageType.IsSubclassOf(base_type))
                {
                    sub_types.Add(type);
                }
            }
        }

        /// <summary>
        /// 从输入流复制到输出流
        /// </summary>
        /// <param name="input">输入流</param>
        /// <param name="output">输出流</param>
        /// <param name="total_bytes">总共复制多少字节</param>
        /// <param name="progress">进度回调，返回False表示终止进程</param>
        /// <param name="buffer_size">缓冲区大小</param>
        /// <returns></returns>
        public static bool ReadTo(Stream input, Stream output, long total_bytes, Predicate<int> progress, int buffer_size = 16384)
        {
            byte[] io_buffer = new byte[buffer_size];
            long total_readed = 0;
            while (total_readed < total_bytes)
            {
                if (!progress(0)) return false;
                int expect = (int)Math.Min(io_buffer.Length, total_bytes - total_readed);
                int readed = input.Read(io_buffer, 0, expect);
                total_readed += readed;
                if (!progress(readed)) return false;
                output.Write(io_buffer, 0, readed);
            }
            output.Flush();
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        public static byte[] ReadExpect(Stream input, int count)
        {
            if (count == 0) return new byte[0];
            byte[] data = new byte[count];
            ReadToEnd(input, data, 0, count);
            return data;
        }
        public static void ReadToEnd(Stream input, byte[] data, int offset, int count)
        {
            if (count == 0) return;
            int readed = input.Read(data, offset, count);
            if (readed <= 0)
                throw new EOFException("EOF of stream");
            while (readed < count)
            {
                int bytes = input.Read(data, offset + readed, count - readed);
                if (bytes <= 0)
                {
                    throw new EOFException("EOF of stream");
                }
                readed += bytes;
            }
        }
        public static byte[] ReadToEnd(Stream src)
        {
            using (MemoryStream dst = new DeepCore.IO.MemoryStream())
            {
                src.CopyTo(dst);
                return dst.ToArray();
            }
        }

        public static async Task<byte[]> ReadExpectAsync(Stream input, int count)
        {
            if (count == 0) return new byte[0];
            byte[] data = new byte[count];
            await ReadToEndAsync(input, data, 0, count);
            return data;
        }
        public static async Task ReadToEndAsync(Stream input, byte[] data, int offset, int count)
        {
            if (count == 0) return;
            int readed = await input.ReadAsync(data, offset, count);
            if (readed <= 0)
                throw new EOFException("EOF of stream");
            while (readed < count)
            {
                int bytes = await input.ReadAsync(data, offset + readed, count - readed);
                if (bytes <= 0)
                {
                    throw new EOFException("EOF of stream");
                }
                readed += bytes;
            }
        }
        public static async Task<byte[]> ReadToEndAsync(Stream src)
        {
            using (MemoryStream dst = new DeepCore.IO.MemoryStream())
            {
                await src.CopyToAsync(dst);
                return dst.ToArray();
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        public static void ReadToEnd(Stream input, Span<byte> data)
        {
            if (data.Length == 0) return;
            var count = data.Length;
            int readed = input.Read(data);
            if (readed <= 0)
                throw new EOFException("EOF of stream");
            data = data.Slice(readed);
            while (readed < count)
            {
                int bytes = input.Read(data);
                if (bytes <= 0)
                {
                    throw new EOFException("EOF of stream");
                }
                data = data.Slice(bytes);
                readed += bytes;
            }
        }
        public static void WriteToEnd(Stream output, byte[] data, int offset, int count)
        {
            output.Write(data, offset, count);
        }
        public static void WriteToEnd(Stream output, byte[] data)
        {
            output.Write(data, 0, data.Length);
        }

        //-------------------------------------------------------------------------------------------------------------------------

        public static T CloneObject<T>(IExternalizableFactory decode, T src) where T : ISerializable, new()
        {
            return decode.Clone(src);
        }
        public static T Clone<T>(IExternalizableFactory decode, T src) where T : ISerializable
        {
            return decode.Clone(src);
        }

        public static byte[] ObjectToBin(this IExternalizableFactory decode, ISerializable ext)
        {
            using (var auto = IOStreamObjectPool.AllocAutoRelease(decode))
            {
                auto.Output.PutObj(ext);
                byte[] buffer = auto.Buffer.ToArray();
                return buffer;
            }
        }

        public static T BinToObject<T>(this IExternalizableFactory decode, byte[] data) where T : ISerializable
        {
            if (data == null || data.Length == 0) return default(T);
            using (var auto = IOStreamObjectPool.AllocAutoRelease(decode, data))
            {
                return auto.Input.GetObj<T>();
            }
        }
        public static object BinToObjectAny(this IExternalizableFactory decode, byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            using (var auto = IOStreamObjectPool.AllocAutoRelease(decode, data))
            {
                return auto.Input.GetObjAny();
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 项目命名空间.资源文件所在文件夹名.资源文件名
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static byte[] LoadFromAssembly(Assembly assembly, string resource)
        {
            resource = resource.Replace('/', '.');
            var reses = assembly.GetManifestResourceNames();
            foreach (var res in reses)
            {
                if (res.EndsWith(resource))
                {
                    Stream stream = assembly.GetManifestResourceStream(res);
                    byte[] ret = new byte[stream.Length];
                    /*stream.Read(ret, 0, ret.Length);*/
                    IOUtil.ReadToEnd(stream, ret, 0, ret.Length);
                    return ret;
                }
            }
            return null;
        }

        /// <summary>
        /// .资源文件所在文件夹名.资源文件名
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static byte[] LoadFromAssembly(Type type, string resource)
        {
            return LoadFromAssembly(type.Assembly, resource);
        }


        public static void Zip(Stream src, Stream dst)
        {
            int blen = (int)Math.Min(1024, src.Length);
            //using (MemoryStream buff = MemoryStreamObjectPool.AllocAutoRelease(blen))
            var buffer = new byte[blen];
            {
                GZipStream zipStream = null;
                try
                {
                    zipStream = new GZipStream(dst, CompressionMode.Compress, true);
                    while (true)
                    {
                        int bytesRead = src.Read(buffer, 0, blen);
                        if (bytesRead == 0)
                            break;
                        zipStream.Write(buffer, 0, bytesRead);
                    }
                    zipStream.Flush();
                    dst.Flush();
                }
                finally
                {
                    if (zipStream != null) zipStream.Close();
                }
            }
        }

        public static void Unzip(Stream src, Stream dst)
        {
            int blen = 1024;
            //using (MemoryStream buff = MemoryStreamObjectPool.AllocAutoRelease(blen))
            var buffer = new byte[blen];
            {
                GZipStream zipStream = null;
                try
                {
                    // Create a compression stream pointing to the destiantion stream
                    zipStream = new GZipStream(src, CompressionMode.Decompress, true);
                    // Read the footer to determine the length of the destiantion file
                    // Read the compressed data into the buffer
                    while (true)
                    {
                        int bytesRead = zipStream.Read(buffer, 0, blen);
                        if (bytesRead == 0)
                            break;
                        dst.Write(buffer, 0, bytesRead);
                    }
                    dst.Flush();
                }
                finally
                {
                    if (zipStream != null) zipStream.Close();
                }
            }
        }

        public static byte[] Zip(byte[] data)
        {
            // Read in the compressed source stream
            using (MemoryStream src = new DeepCore.IO.MemoryStream(data))
            using (MemoryStream dst = new DeepCore.IO.MemoryStream(data.Length * 2))
            {
                try
                {
                    Zip(src, dst);
                    byte[] ret = new byte[dst.Position];
                    Array.Copy(dst.GetBuffer(), 0, ret, 0, ret.Length);
                    return ret;
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    return null;
                }
            }
        }

        public static byte[] Unzip(byte[] data)
        {
            // Read in the compressed source stream
            using (MemoryStream src = new DeepCore.IO.MemoryStream(data))
            using (MemoryStream dst = new DeepCore.IO.MemoryStream(data.Length))
            {
                try
                {
                    Unzip(src, dst);
                    byte[] ret = new byte[dst.Position];
                    Array.Copy(dst.GetBuffer(), 0, ret, 0, ret.Length);
                    return ret;
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    return null;
                }
            }

        }

        //---------------------------------------------------------------------------------------------------

        public static TextWriter Append(this TextWriter o, object any)
        {
            o.Write(any);
            return o;
        }
        public static TextWriter AppendLine(this TextWriter o, object any)
        {
            o.WriteLine(any);
            return o;
        }
        public static TextWriter AppendLine(this TextWriter o)
        {
            o.WriteLine();
            return o;
        }


        //---------------------------------------------------------------------------------------------------

        struct ValidateBodySizeAction : IDisposable
        {
            readonly IInputStream stream;
            readonly long pos1;
            readonly long expect;
            public ValidateBodySizeAction(IInputStream os, long expect)
            {
                this.stream = os;
                this.pos1 = os.Position;
                this.expect = expect;
            }
            public void Dispose()
            {
                var len = stream.Position - pos1;
                if (len != expect)
                {
                    throw new Exception($"Validate BodySize Error : expect:{expect} != readed:{len}");
                }
            }
        }
        public static IDisposable BeginValidateBodySize(this IInputStream input, long expect)
        {
            return new ValidateBodySizeAction(input, expect);
        }


        //---------------------------------------------------------------------------------------------------
        public static BeginSavePosition BeginSavePosition(this OutputStream os)
        {
            return new BeginSavePosition(os.GetStream());
        }
        public static BeginLoadPosition BeginLoadPosition(this InputStream input)
        {
            return new BeginLoadPosition(input.GetStream());
        }
        public static BeginSavePosition BeginSavePosition(this Stream os)
        {
            return new BeginSavePosition(os);
        }
        public static BeginLoadPosition BeginLoadPosition(this Stream input)
        {
            return new BeginLoadPosition(input);
        }

        //---------------------------------------------------------------------------------------------------

        public static void WriteAllBytes<ST>(this IExternalizableFactory factory, string path, ST st, Action<ST, OutputStream> encode)
        {
            CFiles.CreateFile(path);
            using (var ms = new MemoryStream())
            using (var os = new OutputStream(ms, factory))
            {
                encode(st, os);
                var bytes = ms.ToArray();
                File.WriteAllBytes(path, bytes);
            }
        }
        public static void ReadAllBytes<ST>(this IExternalizableFactory factory, string path, ST st, Action<ST, InputStream> decode)
        {
            using (var ms = new MemoryStream(Resource.LoadData(path)))
            {
                using (var os = new InputStream(ms, factory))
                {
                    decode(st, os);
                }
            }
        }
        public static async Task ReadAllBytesAsync<ST>(this IExternalizableFactory factory, string path, ST st, Func<ST, InputStream, Task> decode)
        {
            using (var ms = new MemoryStream(await Resource.LoadDataAsync(path)))
            {
                using (var os = new InputStream(ms, factory))
                {
                    await decode(st, os);
                }
            }
        }

        public static T ReadAllBytes<ST, T>(this IExternalizableFactory factory, string path, ST st, Func<ST, InputStream, T> decode)
        {
            using (var ms = new MemoryStream(Resource.LoadData(path)))
            {
                using (var os = new InputStream(ms, factory))
                {
                    return decode(st, os);
                }
            }
        }
        public static async Task<T> ReadAllBytesAsync<ST, T>(this IExternalizableFactory factory, string path, ST st, Func<ST, InputStream, Task<T>> decode)
        {
            using (var ms = new MemoryStream(await Resource.LoadDataAsync(path)))
            {
                using (var os = new InputStream(ms, factory))
                {
                    return await decode(st, os);
                }
            }
        }

    }




    public struct BeginSavePosition : IDisposable
    {
        private Stream _stream;
        private long _position;
        public int Length { get => (int)(_stream.Position - _position); }
        public long LongLength { get => (_stream.Position - _position); }
        public BeginSavePosition(Stream s)
        {
            this._stream = s;
            LittleEdian.PutS64(_stream, 0);
            this._position = s.Position;
        }
        public void Dispose()
        {
            var newp = _stream.Position;
            _stream.Position = _position - sizeof(long);
            LittleEdian.PutS64(_stream, newp - _position);
            _stream.Position = newp;
        }
    }
    public struct BeginLoadPosition : IDisposable
    {
        private Stream _stream;
        private long _position;
        private long _length;
        public int Length { get => (int)_length; }
        public long LongLength { get => _length; }
        public BeginLoadPosition(Stream s)
        {
            this._stream = s;
            this._length = LittleEdian.GetS64(s);
            this._position = s.Position;
        }
        public void Dispose()
        {


        }
    }
}

namespace System.IO
{
    public static class StreamExt
    {
#if NETSTANDARD2_0 || NET_STANDARD_2_0 || NET462
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int numRead = stream.Read(sharedBuffer, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException("SR.IO_StreamTooLong");
                }
                new Span<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally { ArrayPool<byte>.Shared.Return(sharedBuffer); }
        }
        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(sharedBuffer);
                stream.Write(sharedBuffer, 0, buffer.Length);
            }
            finally { ArrayPool<byte>.Shared.Return(sharedBuffer); }
        }
#endif

    }
}