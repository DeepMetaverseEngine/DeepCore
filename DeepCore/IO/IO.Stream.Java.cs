using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepCore.IO
{
    public class JavaInputStream : IInputStream
    {
        private Stream _stream;
        private byte[] _buff = new byte[8];
        public override long Position { get => _stream.Position; set { _stream.Position = value; } }
        public override long Length { get => _stream.Length; }
        public JavaInputStream(Stream stream, IExternalizableFactory factory) : base(factory)
        {
            _stream = stream;
        }
        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
        }
        public void SetStream(Stream stream)
        {
            _stream = stream;
        }
        public Stream GetStream()
        {
            return _stream;
        }

        public override bool GetBool()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 1);
            return _buff[0] != 0;
        }

        public override byte GetU8()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 1);
            return _buff[0];
        }

        public override sbyte GetS8()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 1);
            return (sbyte)_buff[0];
        }

        public override ushort GetU16()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            int ret = 0;
            ret |= (((int)_buff[0]));
            ret |= (((int)_buff[1]) << 8);
            return (ushort)ret;
        }

        public override short GetS16()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            int ret = 0;
            ret |= (((int)_buff[0]));
            ret |= (((int)_buff[1]) << 8);
            return (short)ret;
        }

        public override uint GetU32()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            int ret = 0;
            ret |= (((int)_buff[0]));
            ret |= (((int)_buff[1]) << 8);
            ret |= (((int)_buff[2]) << 16);
            ret |= (((int)_buff[3]) << 24);
            return (uint)ret;
        }

        public override int GetS32()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            int ret = 0;
            ret |= (((int)_buff[0]));
            ret |= (((int)_buff[1]) << 8);
            ret |= (((int)_buff[2]) << 16);
            ret |= (((int)_buff[3]) << 24);
            return ret;
        }

        public override ulong GetU64()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            ulong ret = 0;
            ret |= (((ulong)_buff[0]));
            ret |= (((ulong)_buff[1]) << 8);
            ret |= (((ulong)_buff[2]) << 16);
            ret |= (((ulong)_buff[3]) << 24);
            ret |= (((ulong)_buff[4]) << 32);
            ret |= (((ulong)_buff[5]) << 40);
            ret |= (((ulong)_buff[6]) << 48);
            ret |= (((ulong)_buff[7]) << 56);
            return ret;
        }

        public override long GetS64()
        {
            IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            long ret = 0;
            ret |= (((long)_buff[0]));
            ret |= (((long)_buff[1]) << 8);
            ret |= (((long)_buff[2]) << 16);
            ret |= (((long)_buff[3]) << 24);
            ret |= (((long)_buff[4]) << 32);
            ret |= (((long)_buff[5]) << 40);
            ret |= (((long)_buff[6]) << 48);
            ret |= (((long)_buff[7]) << 56);
            return ret;
        }

        public override float GetF32()
        {
            //IOUtil.ReadToEnd(_stream, _buff, 0, 4);
            //return System.BitConverter.ToSingle(_buff, 0);
            uint val = GetU32();
            unsafe
            {
                return *((float*)&val);
            }
        }
        public override double GetF64()
        {
            //             IOUtil.ReadToEnd(_stream, _buff, 0, 8);
            //             return System.BitConverter.ToDouble(_buff, 0);
            ulong val = GetU64();
            unsafe
            {
                return *((double*)&val);
            }
        }
        public override decimal GetDEC()
        {
            throw new NotImplementedException();
        }

        public override char GetUnicode()
        {
            //             IOUtil.ReadToEnd(_stream, _buff, 0, 2);
            //             return System.BitConverter.ToChar(_buff, 0);
            ushort val = GetU16();
            unsafe
            {
                return *((char*)&val);
            }
        }


        public override string GetUTF()
        {
            int len = GetU16();
            if (len > 0)
            {
                var buffer = new byte[len];
                {
                    DeepCore.IO.IOUtil.ReadToEnd(_stream, buffer, 0, len);
                    return UTF_ENCODING.GetString(buffer, 0, len);
                }
            }
            return "";
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
        public override void GetRawBytes(byte[] buff, int offset, int count)
        {
            IOUtil.ReadToEnd(_stream, buff, offset, count);
        }
        unsafe public override void GetRawBytes(byte* buff, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override int GetVS32()
        {
            throw new NotImplementedException();
        }

        public override uint GetVU32()
        {
            throw new NotImplementedException();
        }

        public override long GetVS64()
        {
            throw new NotImplementedException();
        }

        public override ulong GetVU64()
        {
            throw new NotImplementedException();
        }
        public override T GetStruct<T>()
        {
            throw new NotImplementedException();
        }
    }

    public class JavaOutputStream : IOutputStream
    {
        private Stream _stream;
        private byte[] _buff = new byte[8];
        public override long Position { get => _stream.Position; set { _stream.Position = value; } }
        public override long Length { get => _stream.Length; }
        public JavaOutputStream(Stream stream, IExternalizableFactory factory) : base(factory)
        {
            _stream = stream;
        }
        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
        }
        public void SetStream(Stream stream)
        {
            _stream = stream;
        }
        public Stream GetStream()
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
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
        }

        public override void PutS16(short value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
        }

        public override void PutU32(uint value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }

        public override void PutS32(int value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }

        public override void PutU64(ulong value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
            _stream.WriteByte((byte)(value >> 32));
            _stream.WriteByte((byte)(value >> 40));
            _stream.WriteByte((byte)(value >> 48));
            _stream.WriteByte((byte)(value >> 56));
        }

        public override void PutS64(long value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
            _stream.WriteByte((byte)(value >> 32));
            _stream.WriteByte((byte)(value >> 40));
            _stream.WriteByte((byte)(value >> 48));
            _stream.WriteByte((byte)(value >> 56));
        }

        public override void PutF32(float value)
        {
            //byte[] buff = BitConverter.GetBytes(value);
            unsafe
            {
                uint val = *((uint*)&value);
                PutU32(val);
            }
        }

        public override void PutF64(double value)
        {
            //byte[] buff = BitConverter.GetBytes(value);
            //_stream.Write(buff, 0, 8);
            unsafe
            {
                ulong val = *((ulong*)&value);
                PutU64(val);
            }
        }

        public override void PutDEC(decimal value)
        {
            throw new NotImplementedException();
        }

        public override void PutUnicode(char value)
        {
            //byte[] buff = BitConverter.GetBytes(value);
            //_stream.Write(buff, 0, 2);
            unsafe
            {
                ushort val = *((ushort*)&value);
                PutU16(val);
            }
        }

        public override void PutUTF(string str)
        {
            if (str == null)
            {
                PutU16(0);
            }
            else if (str.Length == 0)
            {
                PutU16(0);
            }
            else
            {
                int len = UTF_ENCODING.GetByteCount(str);
                if (len >= UInt16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + str + "\nSize=" + len);
                }
                var buffer = new byte[len];
                {
                    UTF_ENCODING.GetBytes(str, 0, str.Length, buffer, 0);
                    PutU16((UInt16)len);
                    _stream.Write(buffer, 0, len);
                }
            }
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
                _stream.Write(bytes, 0, length);
            }
        }
        public override void PutRawBytes(byte[] bytes, int offset, int count)
        {
            _stream.Write(bytes, offset, count);
        }
        public override unsafe void PutRawBytes(byte* bytes, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void PutVS32(int value)
        {
            throw new NotImplementedException();
        }

        public override void PutVU32(uint value)
        {
            throw new NotImplementedException();
        }

        public override void PutVS64(long value)
        {
            throw new NotImplementedException();
        }

        public override void PutVU64(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void PutStruct<T>(in T value)
        {
            throw new NotImplementedException();
        }
    }


}
