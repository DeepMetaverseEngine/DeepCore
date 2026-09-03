using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DeepCore.IO
{

    //--------------------------------------------------------------------------------------------------------------------

    public class InputStream : IInputStream
    {
        protected Stream _stream;
        protected readonly byte[] _buff = new byte[16];
        public override long Position { get => _stream.Position; set { _stream.Position = value; } }
        public override long Length { get => _stream.Length; }
        public InputStream() : this(null, null) { }
        public InputStream(Stream stream, IExternalizableFactory factory = null) : base(factory)
        {
            _stream = stream;
        }
        public InputStream(IExternalizableFactory factory) : this(null, factory)
        {
        }
        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
        }
        public virtual void SetStream(Stream stream)
        {
            _stream = stream;
        }
        public virtual Stream GetStream()
        {
            return _stream;
        }

        public override bool GetBool()
        {
            return _stream.ReadByte() != 0;
        }
        public override byte GetU8()
        {
            return (byte)_stream.ReadByte();
        }
        public override sbyte GetS8()
        {
            return (sbyte)_stream.ReadByte();
        }

        public override ushort GetU16()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return (ushort)Marshal.ReadInt16((IntPtr)pByte);
                }
            }
        }

        public override short GetS16()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return Marshal.ReadInt16((IntPtr)pByte);
                }
            }
        }

        public override uint GetU32()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return (uint)Marshal.ReadInt32((IntPtr)pByte);
                }
            }
        }

        public override int GetS32()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return Marshal.ReadInt32((IntPtr)pByte);
                }
            }
        }
        public override ulong GetU64()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return (ulong)Marshal.ReadInt64((IntPtr)pByte);
                }
            }
        }

        public override long GetS64()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return Marshal.ReadInt64((IntPtr)pByte);
                }
            }
        }

        public override float GetF32()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            int val;
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    val = Marshal.ReadInt32((IntPtr)pByte);
                }
                return *((float*)&val);
            }
        }
        public override double GetF64()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            long val;
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    val = Marshal.ReadInt64((IntPtr)pByte);
                }
                return *((double*)&val);
            }
        }
        public override decimal GetDEC()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 16);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return Marshal.PtrToStructure<decimal>((IntPtr)pByte);
                }
            }
        }

        public override char GetUnicode()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    return (char)Marshal.ReadInt16((IntPtr)pByte);
                }
            }
        }

        private const int STACK_SIZE = 1024;

        public override string GetUTF()
        {
            int str_len = GetS16();
            if (str_len < 0)
            {
                return null;
            }
            if (str_len == 0)
            {
                return string.Empty;
            }
            int len = GetS16();// str_len << 1;
            if (this._stream is System.IO.MemoryStream ms)
            {
                var offset = (int)ms.Position;
                ms.Position += len;
                unsafe
                {
                    fixed (byte* pBuff = ms.GetBuffer())
                    {
                        byte* pData = &pBuff[offset];
                        return new string((char*)pData, 0, str_len); //Marshal.PtrToStringUni((IntPtr)pData, str_len);
                    }
                }
            }
            else
            {
                //IOUtil.ReadToEnd(_stream, buff, 0, len);
                Span<byte> buff = len < STACK_SIZE ? stackalloc byte[len] : new byte[len];
                IOUtil.ReadToEnd(_stream, buff);
                unsafe
                {
                    fixed (byte* pBuff = buff)
                    {
                        char* pData = (char*)pBuff;
                        return new string(pData, 0, str_len); //Marshal.PtrToStringUni((IntPtr)pData, str_len);
                    }
                }
            }
        }

        public override T GetStruct<T>()
        {
            int len = GetS16();
            if (len <= 0)
            {
                return default(T);
            }
            if (this._stream is System.IO.MemoryStream ms)
            {
                var offset = (int)ms.Position;
                ms.Position += len;
                unsafe
                {
                    fixed (byte* pBuff = ms.GetBuffer())
                    {
                        byte* pData = &pBuff[offset];
                        return Marshal.PtrToStructure<T>((IntPtr)pData);
                    }
                }
            }
            else
            {
                //                     byte[] buff = new byte[len];
                //                     IOUtil.ReadToEnd(_stream, buff, 0, len);
                Span<byte> buff = len < STACK_SIZE ? stackalloc byte[len] : new byte[len];
                IOUtil.ReadToEnd(_stream, buff);
                unsafe
                {
                    fixed (void* pBuff = buff)
                    {
                        return Marshal.PtrToStructure<T>((IntPtr)pBuff);
                    }
                }
            }
        }
        public override unsafe void GetRawBytes(byte* buff, int offset, int count)
        {
            Span<byte> bytes = new Span<byte>(buff + offset, count);
            IOUtil.ReadToEnd(_stream, bytes);
        }
        public override void GetRawBytes(byte[] buff, int offset, int count)
        {
            IOUtil.ReadToEnd(_stream, buff, offset, count);
        }

        public override byte[] GetBytes()
        {
            int len = GetS32();
            if (len > BYTES_LIMIT) { throw new IOException("Bytes overflow : " + len); }
            if (len < 0) return null;
            if (len == 0) return new byte[0];
            byte[] ret = new byte[len];
            IOUtil.ReadToEnd(_stream, ret, 0, len);
            return ret;
        }

        public override Int32 GetVS32()
        {
            if (USE_VLQ) return (Int32)GetVS64();
            else return GetS32();
        }
        public override UInt32 GetVU32()
        {
            if (USE_VLQ) return (UInt32)GetVU64();
            else return GetU32();
        }
        public override Int64 GetVS64()
        {
            if (USE_VLQ)
            {
                ulong m = ReadVLQ();
                long v = (long)(m >> 1);
                return (m % 2 == 1) ? -v : v;
            }
            else
            {
                return GetS64();
            }
        }
        public override UInt64 GetVU64()
        {
            if (USE_VLQ)
            {
                return ReadVLQ();
            }
            else
            {
                return GetU64();
            }
        }
        protected ulong ReadVLQ()
        {
            ulong value = 0;
            ulong b = 0;
            for (int i = 0; i <= 70; i += 7)
            {
                b = (ulong)_stream.ReadByte();
                value |= ((b & 0x7F) << i);
                if ((b & 0x80) == 0)
                    break;
            }
            return value;
        }
    }

    public class OutputStream : IOutputStream
    {
        protected Stream _stream;
        protected readonly byte[] _buff = new byte[16];
        public override long Position { get => _stream.Position; set { _stream.Position = value; } }
        public override long Length { get => _stream.Length; }
        public OutputStream() : this(null, null) { }
        public OutputStream(Stream stream, IExternalizableFactory factory = null) : base(factory)
        {
            _stream = stream;
        }
        public OutputStream(IExternalizableFactory factory) : this(null, factory)
        {
        }
        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
        }
        public virtual void SetStream(Stream stream)
        {
            _stream = stream;
        }
        public virtual Stream GetStream()
        {
            return _stream;
        }

        public override void PutBool(bool value)
        {
            _stream.WriteByte((byte)(value ? 1 : 0));
        }

        public override void PutU8(byte value)
        {
            _stream.WriteByte(value);
        }

        public override void PutS8(sbyte value)
        {
            _stream.WriteByte((byte)value);
        }

        public override void PutU16(ushort value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt16((IntPtr)pByte, (short)value);
                }
                _stream.Write(_buff, 0, 2);
            }
        }

        public override void PutS16(short value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt16((IntPtr)pByte, value);
                }
                _stream.Write(_buff, 0, 2);
            }
        }

        public override void PutU32(uint value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt32((IntPtr)pByte, (int)value);
                }
                _stream.Write(_buff, 0, 4);
            }
        }

        public override void PutS32(int value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt32((IntPtr)pByte, value);
                }
                _stream.Write(_buff, 0, 4);
            }
        }

        public override void PutU64(ulong value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt64((IntPtr)pByte, (long)value);
                }
                _stream.Write(_buff, 0, 8);
            }
        }

        public override void PutS64(long value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt64((IntPtr)pByte, value);
                }
                _stream.Write(_buff, 0, 8);
            }
        }

        public override void PutF32(float value)
        {
            unsafe
            {
                int val = *((int*)&value);
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt32((IntPtr)pByte, val);
                }
                _stream.Write(_buff, 0, 4);
            }
        }

        public override void PutF64(double value)
        {
            unsafe
            {
                long val = *((long*)&value);
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt64((IntPtr)pByte, val);
                }
                _stream.Write(_buff, 0, 8);
            }
        }
        public override void PutDEC(decimal value)
        {
            unsafe
            {
                fixed (byte* pByte = _buff)
                {
                    Marshal.StructureToPtr(value, (IntPtr)pByte, false);
                }
                _stream.Write(_buff, 0, 16);
            }
        }

        public override void PutUnicode(char value)
        {
            unsafe
            {
                short val = *((short*)&value);
                fixed (byte* pByte = _buff)
                {
                    Marshal.WriteInt16((IntPtr)pByte, val);
                }
                _stream.Write(_buff, 0, 2);
            }
        }

        public override void PutUTF(string str)
        {
            if (str == null)
            {
                PutS16(NULL_MESSAGE_CODE);
                return;
            }
            int str_len = str.Length;
            if (str_len == 0)
            {
                PutS16(0);
            }
            else
            {
                if (str_len >= Int16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + str + "\nSize=" + str_len);
                }
                PutS16((short)str_len);
                int len = System.Text.Encoding.Unicode.GetByteCount(str);//str.Length << 1;
                PutS16((short)len);
                if (this._stream is System.IO.MemoryStream ms)
                {
                    if (ms.Length < ms.Position + len) ms.SetLength(Math.Max(ms.Length, ms.Position + len));
                    unsafe
                    {
                        fixed (void* pStr = str)
                        {
                            Marshal.Copy(new IntPtr(pStr), ms.GetBuffer(), (int)ms.Position, len);
                        }
                    }
                    ms.Position += len;
                }
                else
                {
                    unsafe
                    {
                        fixed (char* pStr = str)
                        {
                            byte* pByte = (byte*)pStr;
                            var buff = new Span<byte>(pStr, len);
                            _stream.Write(buff);
                        }
                    }
                }
            }
        }
        public override void PutStruct<T>(in T value)
        {
            var len = Marshal.SizeOf(value);
            if (len >= Int16.MaxValue)
            {
                throw new IOException($"PutStruct overflow : {typeof(T)} len={len}");
            }
            PutS16((short)len);
            if (this._stream is System.IO.MemoryStream ms)
            {
                if (ms.Length < ms.Position + len) ms.SetLength(Math.Max(ms.Length, ms.Position + len));
                unsafe
                {
                    IntPtr bufferIntPtr = Marshal.AllocHGlobal(len);
                    try
                    {
                        Marshal.StructureToPtr(value, bufferIntPtr, true);
                        Marshal.Copy(bufferIntPtr, ms.GetBuffer(), (int)ms.Position, len);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(bufferIntPtr);
                    }
                }
                ms.Position += len;
            }
            else
            {
                unsafe
                {
                    IntPtr bufferIntPtr = Marshal.AllocHGlobal(len);
                    try
                    {
                        Marshal.StructureToPtr(value, bufferIntPtr, true);
                        var buff = new Span<byte>(bufferIntPtr.ToPointer(), len);
                        _stream.Write(buff);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(bufferIntPtr);
                    }
                }
            }
        }
        public override unsafe void PutRawBytes(byte* buff, int offset, int count)
        {
            Span<byte> bytes = new Span<byte>(buff + offset, count);
            _stream.Write(bytes);
        }
        public override void PutRawBytes(byte[] buff, int offset, int count)
        {
            _stream.Write(buff, offset, count);
        }

        public override void PutBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                PutS32(NULL_MESSAGE_CODE);
            }
            else if (bytes.Length == 0)
            {
                PutS32(0);
            }
            else
            {
                int len = bytes.Length;
                if (len > BYTES_LIMIT) { throw new IOException("Bytes overflow : " + len); }
                PutS32(bytes.Length);
                _stream.Write(bytes, 0, bytes.Length);
            }
        }
        public override void PutBytes(byte[] bytes, int offset, int length)
        {
            if (bytes == null)
            {
                PutS32(NULL_MESSAGE_CODE);
            }
            else if (length == 0)
            {
                PutS32(0);
            }
            else
            {
                if (length > BYTES_LIMIT) { throw new IOException("Bytes overflow : " + length); }
                PutS32(length);
                _stream.Write(bytes, offset, length);
            }
        }

        public override void PutVU32(uint value)
        {
            if (USE_VLQ) PutVU64(value);
            else PutU32(value);
        }
        public override void PutVS32(int value)
        {
            if (USE_VLQ) PutVS64(value);
            else PutS32(value);
        }
        public override void PutVS64(long value)
        {
            if (USE_VLQ)
            {
                ulong m;
                if (value < 0)
                {
                    m = (ulong)(((-value) << 1) | 1);
                }
                else
                {
                    m = (ulong)(((value) << 1));
                }
                WriteVLQ(m);
            }
            else
            {
                PutS64(value);

            }
        }
        public override void PutVU64(ulong value)
        {
            if (USE_VLQ)
            {
                WriteVLQ(value);
            }
            else
            {
                PutU64(value);
            }
        }

        protected void WriteVLQ(ulong value)
        {
            do
            {
                byte b = (byte)(value & 0x7F);
                value = (value >> 7);
                if (value != 0)
                {
                    b = (byte)(b | 0x80);
                }
                _stream.WriteByte(b);
            }
            while (value != 0);
        }
    }

    //--------------------------------------------------------------------------------------------------------------------

    public class MemoryOutputStream : OutputStream
    {
        private readonly bool autoDispose;
        public DeepCore.IO.MemoryStream Buffer { get { return GetStream() as DeepCore.IO.MemoryStream; } }
        protected override ArraySegment<byte> GetBufferSegment(int offset, int count)
        {
            return new ArraySegment<byte>(this.Buffer.GetBuffer(), offset, count);
        }
        public MemoryOutputStream() : this(null) { }
        public MemoryOutputStream(IExternalizableFactory factory) : base(new DeepCore.IO.MemoryStream(), factory)
        {
            this.autoDispose = true;
        }
        public MemoryOutputStream(DeepCore.IO.MemoryStream stream, IExternalizableFactory factory) : base(stream, factory)
        {
            this.autoDispose = false;
        }
        new public DeepCore.IO.MemoryStream GetStream()
        {
            return base.GetStream() as DeepCore.IO.MemoryStream;
        }
        public void Reset()
        {
            Buffer.Position = 0;
            Buffer.SetLength(0);
        }
        protected override void Dispose(bool disposing)
        {
            if (autoDispose) Buffer.Dispose();
        }
    }

    public class MemoryInputStream : InputStream
    {
        private readonly bool autoDispose;
        public DeepCore.IO.MemoryStream Buffer { get { return GetStream() as DeepCore.IO.MemoryStream; } }
        protected override ArraySegment<byte> GetBufferSegment(int offset, int count)
        {
            return new ArraySegment<byte>(this.Buffer.GetBuffer(), offset, count);
        }
        public MemoryInputStream() : this(null) { }
        public MemoryInputStream(IExternalizableFactory factory) : base(new DeepCore.IO.MemoryStream(), factory)
        {
            this.autoDispose = true;
        }
        public MemoryInputStream(DeepCore.IO.MemoryStream stream, IExternalizableFactory factory) : base(stream, factory)
        {
            this.autoDispose = false;
        }
        new public DeepCore.IO.MemoryStream GetStream()
        {
            return base.GetStream() as DeepCore.IO.MemoryStream;
        }
        public void Reset()
        {
            Buffer.Position = 0;
        }
        protected override void Dispose(bool disposing)
        {
            if (autoDispose) Buffer.Dispose();
        }
    }
    //--------------------------------------------------------------------------------------------------------------------
    public class AutoMemoryStream : Disposable
    {
        private MemoryStream buffer;
        private MemoryInputStream input;
        private MemoryOutputStream output;
        public MemoryStream Buffer { get => buffer; }
        public MemoryInputStream Input { get => input; }
        public MemoryOutputStream Output { get => output; }
        public AutoMemoryStream() : this(null) { }
        public AutoMemoryStream(IExternalizableFactory factory = null)
        {
            this.buffer = new MemoryStream();
            this.input = new MemoryInputStream(buffer, factory);
            this.output = new MemoryOutputStream(buffer, factory);
        }
        public void SetFactory(IExternalizableFactory factory)
        {
            input.SetFactory(factory);
            output.SetFactory(factory);
        }
        public void Flip()
        {
            buffer.Position = 0;
        }
        protected override void Disposing()
        {
            output.Dispose();
            input.Dispose();
            buffer.Dispose();
        }
        public T Clone<T>(T src) where T : ISerializable
        {
            this.Flip();
            this.Output.PutObj(src);
            this.Flip();
            src = this.Input.GetObj<T>();
            return src;
        }
    }
    //--------------------------------------------------------------------------------------------------------------------

}
