using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DeepCore.IO;

namespace DeepCore.IO
{
    public static class LittleEdian
    {
        //--------------------------------------------------------------------------------------
        #region BYTES

        public static bool GetBool(byte[] data, ref int pos)
        {
            return data[pos++] != 0;
        }
        public static byte GetU8(byte[] data, ref int pos)
        {
            return data[pos++];
        }
        public static sbyte GetS8(byte[] data, ref int pos)
        {
            return (sbyte)data[pos++];
        }
        public static ushort GetU16(byte[] data, ref int pos)
        {
            unchecked
            {
                int ret = data[pos++];
                ret |= (((int)data[pos++]) << 8);
                return (ushort)ret;
            }
        }
        public static short GetS16(byte[] data, ref int pos)
        {
            unchecked
            {
                int ret = data[pos++];
                ret |= (((int)data[pos++]) << 8);
                return (short)ret;
            }
        }
        public static uint GetU32(byte[] data, ref int pos)
        {
            unchecked
            {
                uint ret = data[pos++];
                ret |= (uint)(data[pos++] << 8);
                ret |= (uint)(data[pos++] << 16);
                ret |= (uint)(data[pos++] << 24);
                return ret;
            }
        }
        public static int GetS32(byte[] data, ref int pos)
        {
            unchecked
            {
                int ret = data[pos++];
                ret |= ((int)data[pos++] << 8);
                ret |= ((int)data[pos++] << 16);
                ret |= ((int)data[pos++] << 24);
                return ret;
            }
        }
        public static ulong GetU64(byte[] data, ref int pos)
        {
            unchecked
            {
                ulong ret = data[pos++];
                ret |= (((ulong)data[pos++]) << 8);
                ret |= (((ulong)data[pos++]) << 16);
                ret |= (((ulong)data[pos++]) << 24);
                ret |= (((ulong)data[pos++]) << 32);
                ret |= (((ulong)data[pos++]) << 40);
                ret |= (((ulong)data[pos++]) << 48);
                ret |= (((ulong)data[pos++]) << 56);
                return ret;
            }
        }
        public static long GetS64(byte[] data, ref int pos)
        {
            unchecked
            {
                long ret = data[pos++];
                ret |= (((long)data[pos++]) << 8);
                ret |= (((long)data[pos++]) << 16);
                ret |= (((long)data[pos++]) << 24);
                ret |= (((long)data[pos++]) << 32);
                ret |= (((long)data[pos++]) << 40);
                ret |= (((long)data[pos++]) << 48);
                ret |= (((long)data[pos++]) << 56);
                return ret;
            }
        }
        public static string GetUTF(byte[] data, ref int pos)
        {
            int len = GetU16(data, ref pos);
            if (len > 0)
            {
                string ret = System.Text.UTF8Encoding.UTF8.GetString(data, pos, len);
                pos += len;
                return ret;
            }
            return null;
        }
        public static byte[] GetBytes(byte[] data, ref int pos)
        {
            int len = GetU16(data, ref pos);
            byte[] ret = new byte[len];
            if (len > 0)
            {
                Array.Copy(data, pos, ret, 0, len);
                pos += len;
            }
            return ret;
        }

        public static void PutBool(byte[] data, ref int pos, bool value)
        {
            data[pos++] = (byte)(value ? 1 : 0);
        }
        public static void PutU8(byte[] data, ref int pos, byte value)
        {
            data[pos++] = value;
        }
        public static void PutS8(byte[] data, ref int pos, sbyte value)
        {
            data[pos++] = (byte)value;
        }
        public static void PutU16(byte[] data, ref int pos, ushort value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
        }
        public static void PutS16(byte[] data, ref int pos, short value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
        }
        public static void PutU32(byte[] data, ref int pos, uint value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 24);
        }
        public static void PutS32(byte[] data, ref int pos, int value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 24);
        }
        public static void PutU64(byte[] data, ref int pos, ulong value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 32);
            data[pos++] = (byte)(value >> 40);
            data[pos++] = (byte)(value >> 48);
            data[pos++] = (byte)(value >> 56);
        }
        public static void PutS64(byte[] data, ref int pos, long value)
        {
            data[pos++] = (byte)(value);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 32);
            data[pos++] = (byte)(value >> 40);
            data[pos++] = (byte)(value >> 48);
            data[pos++] = (byte)(value >> 56);
        }
        public static void PutUTF(byte[] data, ref int pos, string value)
        {
            if (value == null || value.Length == 0)
            {
                PutU16(data, ref pos, 0);
            }
            else
            {
                byte[] buff = System.Text.UTF8Encoding.UTF8.GetBytes(value);
                if (buff.Length > UInt16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + value + "\nSize=" + buff.Length);
                }
                PutU16(data, ref pos, (ushort)buff.Length);
                Put(data, ref pos, buff, 0, buff.Length);
            }
        }
        public static void PutBytes(byte[] data, ref int pos, byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                PutS32(data, ref pos, 0);
            }
            else
            {
                PutS32(data, ref pos, value.Length);
                Put(data, ref pos, value, 0, value.Length);
            }
        }
        public static void Get(byte[] data, ref int pos, byte[] value, int offset, int length)
        {
            Array.Copy(data, pos, value, offset, length);
            pos += length;
        }
        public static void Put(byte[] data, ref int pos, byte[] value, int offset, int length)
        {
            Array.Copy(value, offset, data, pos, length);
            pos += length;
        }

        #endregion

        //--------------------------------------------------------------------------------------
        #region STREAM

        public static bool GetBool(Stream data)
        {
            return IOUtil.TryReadByte(data) != 0;
        }
        public static void PutBool(Stream data, bool value)
        {
            data.WriteByte((byte)(value ? 1 : 0));
        }

        public static byte GetU8(Stream data)
        {
            return (byte)IOUtil.TryReadByte(data);
        }
        public static void PutU8(Stream data, byte value)
        {
            data.WriteByte(value);
        }

        public static sbyte GetS8(Stream data)
        {
            return (sbyte)IOUtil.TryReadByte(data);
        }
        public static void PutS8(Stream data, sbyte value)
        {
            data.WriteByte((byte)value);
        }

        public static ushort GetU16(Stream data)
        {
            unchecked
            {
                int ret = (IOUtil.TryReadByte(data));
                ret |= (((int)IOUtil.TryReadByte(data)) << 8);
                return (ushort)ret;
            }
        }
        public static void PutU16(Stream data, ushort value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
        }

        public static short GetS16(Stream data)
        {
            unchecked
            {
                int ret = (IOUtil.TryReadByte(data));
                ret |= (((int)IOUtil.TryReadByte(data)) << 8);
                return (short)ret;
            }
        }
        //------------------------------------------------------------------------------------------

        public static void PutS16(Stream data, short value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
        }
        public static uint GetU32(Stream data)            
        {
            unchecked
            {
                uint ret = IOUtil.TryReadByte(data);
                ret |= (((uint)IOUtil.TryReadByte(data)) << 8);
                ret |= (((uint)IOUtil.TryReadByte(data)) << 16);
                ret |= (((uint)IOUtil.TryReadByte(data)) << 24);
                return (uint)ret;
            }
        }
        public static void PutU32(Stream data, uint value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 24));
        }
        public static int GetS32(Stream data)
        {
            unchecked
            {
                int ret = IOUtil.TryReadByte(data);
                ret |= (((int)IOUtil.TryReadByte(data)) << 8);
                ret |= (((int)IOUtil.TryReadByte(data)) << 16);
                ret |= (((int)IOUtil.TryReadByte(data)) << 24);
                return ret;
            }
        }
        public static void PutS32(Stream data, int value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 24));
        }
        public static ulong GetU64(Stream data)
        {
            unchecked
            {
                ulong ret = (ulong)(IOUtil.TryReadByte(data));
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 8);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 16);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 24);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 32);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 40);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 48);
                ret |= (((ulong)IOUtil.TryReadByte(data)) << 56);
                return ret;
            }
        }
        public static void PutU64(Stream data, ulong value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 32));
            data.WriteByte((byte)(value >> 40));
            data.WriteByte((byte)(value >> 48));
            data.WriteByte((byte)(value >> 56));
        }
        public static long GetS64(Stream data)
        {
            unchecked
            {
                long ret = IOUtil.TryReadByte(data);
                ret |= (((long)IOUtil.TryReadByte(data)) << 8);
                ret |= (((long)IOUtil.TryReadByte(data)) << 16);
                ret |= (((long)IOUtil.TryReadByte(data)) << 24);
                ret |= (((long)IOUtil.TryReadByte(data)) << 32);
                ret |= (((long)IOUtil.TryReadByte(data)) << 40);
                ret |= (((long)IOUtil.TryReadByte(data)) << 48);
                ret |= (((long)IOUtil.TryReadByte(data)) << 56);
                return ret;
            }
        }

        public static void PutS64(Stream data, long value)
        {
            data.WriteByte((byte)(value));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 32));
            data.WriteByte((byte)(value >> 40));
            data.WriteByte((byte)(value >> 48));
            data.WriteByte((byte)(value >> 56));
        }

        //--------------------------------------------------------------------------------------

        public static float GetF32(Stream data)
        {
            byte[] buff = IOUtil.ReadExpect(data, 4);
            return System.BitConverter.ToSingle(buff, 0);
        }
        public static void PutF32(Stream data, float value)
        {
            byte[] buff = BitConverter.GetBytes(value);
            data.Write(buff, 0, 4);
        }
        public static double GetF64(Stream data)
        {
            byte[] buff = IOUtil.ReadExpect(data, 4);
            return System.BitConverter.ToDouble(buff, 0);
        }
        public static void PutF64(Stream data, double value)
        {
            byte[] buff = BitConverter.GetBytes(value);
            data.Write(buff, 0, 8);
        }

        //--------------------------------------------------------------------------------------

        public static string GetUTF(Stream data)
        {
            int len = GetU16(data);
            if (len > UInt16.MaxValue)
            {
                throw new IOException("GetUTF overflow : Size=" + len);
            }
            if (len > 0)
            {
                byte[] buff = IOUtil.ReadExpect(data, len);
                return System.Text.UTF8Encoding.UTF8.GetString(buff, 0, len);
            }
            return null;
        }
        public static void PutUTF(Stream data, string str)
        {
            if (str == null || str.Length == 0)
            {
                PutU16(data, 0);
            }
            else
            {
                byte[] buff = System.Text.UTF8Encoding.UTF8.GetBytes(str);
                if (buff.Length > UInt16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + str + "\nSize=" + buff.Length);
                }
                PutU16(data, (ushort)buff.Length);
                data.Write(buff, 0, buff.Length);
            }
        }

        public static byte[] GetBytes(Stream data)
        {
            int len = GetS32(data);
            if (len < 0) return null;
            if (len == 0) return new byte[0];
            byte[] buff = IOUtil.ReadExpect(data, len);
            return buff;
        }
        public static void PutBytes(Stream data, byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                PutS32(data, 0);
            }
            else
            {
                PutS32(data, value.Length);
                data.Write(value, 0, value.Length);
            }
        }


        //--------------------------------------------------------------------------------------

        public static T GetEnum8<T>(Stream data, Type enumType)
        {
            return (T)Enum.ToObject(enumType, IOUtil.TryReadByte(data));
        }

        public static void PutEnum8(Stream data, object enumData)
        {
            byte b = (byte)(enumData);
            data.WriteByte(b);
        }
        //--------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------

        public delegate T GetData<T>(Stream data);
        public delegate void PutData<T>(Stream data, T v);

        public static T[] GetArray<T>(Stream data, GetData<T> action) where T : new()
        {
            int len = GetU16(data);
            if (len > UInt16.MaxValue)
            {
                throw new IOException("GetArray overflow : Size=" + len);
            }
            T[] ret = new T[len];
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(data);
                ret[i] = d;
            }
            return ret;
        }
        public static void PutArray<T>(Stream data, T[] array, PutData<T> action)
        {
            if (array == null)
            {
                PutU16(data, 0);
            }
            else
            {
                int len = array.Length;
                if (len > UInt16.MaxValue)
                {
                    throw new IOException("PutArray overflow : " + array + "\nSize=" + len);
                }
                PutU16(data, (ushort)len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(data, array[i]);
                }
            }
        }

        public static List<T> GetList<T>(Stream data, GetData<T> action) where T : new()
        {
            int len = GetU16(data);
            if (len > UInt16.MaxValue)
            {
                throw new IOException("GetList overflow : Size=" + len);
            }
            ArrayList<T> ret = new ArrayList<T>(len);
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(data);
                ret.Add(d);
            }
            return ret;
        }

        public static void PutList<T>(Stream data, IList<T> list, PutData<T> action)
        {
            if (list == null)
            {
                PutU16(data, 0);
            }
            else
            {
                int len = list.Count;
                if (len > UInt16.MaxValue)
                {
                    throw new IOException("PutList overflow : " + list + "\nSize=" + len);
                }
                PutU16(data, (ushort)len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(data, list[i]);
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------

    }


    public static class BigEdian
    {
        //--------------------------------------------------------------------------------------
        #region BYTES

        public static bool GetBool(byte[] data, ref int pos)
        {
            return data[pos++] != 0;
        }
        public static byte GetU8(byte[] data, ref int pos)
        {
            return data[pos++];
        }
        public static sbyte GetS8(byte[] data, ref int pos)
        {
            return (sbyte)data[pos++];
        }
        public static ushort GetU16(byte[] data, ref int pos)
        {
            int ret = 0;
            ret |= (data[pos++] << 8);
            ret |= (data[pos++]);
            return (ushort)ret;
        }
        public static short GetS16(byte[] data, ref int pos)
        {
            int ret = 0;
            ret |= (data[pos++] << 8);
            ret |= (data[pos++]);
            return (short)ret;
        }
        public static uint GetU32(byte[] data, ref int pos)
        {
            uint ret = 0;
            ret |= (uint)(data[pos++] << 24);
            ret |= (uint)(data[pos++] << 16);
            ret |= (uint)(data[pos++] << 8);
            ret |= (uint)(data[pos++]);
            return ret;
        }
        public static int GetS32(byte[] data, ref int pos)
        {
            int ret = 0;
            ret |= ((int)data[pos++] << 24);
            ret |= ((int)data[pos++] << 16);
            ret |= ((int)data[pos++] << 8);
            ret |= ((int)data[pos++]);
            return ret;
        }
        public static ulong GetU64(byte[] data, ref int pos)
        {
            ulong ret = 0;
            ret |= (((ulong)data[pos++]) << 56);
            ret |= (((ulong)data[pos++]) << 48);
            ret |= (((ulong)data[pos++]) << 40);
            ret |= (((ulong)data[pos++]) << 32);
            ret |= (((ulong)data[pos++]) << 24);
            ret |= (((ulong)data[pos++]) << 16);
            ret |= (((ulong)data[pos++]) << 8);
            ret |= (((ulong)data[pos++]));
            return ret;
        }
        public static long GetS64(byte[] data, ref int pos)
        {
            long ret = 0;
            ret |= (((long)data[pos++]) << 56);
            ret |= (((long)data[pos++]) << 48);
            ret |= (((long)data[pos++]) << 40);
            ret |= (((long)data[pos++]) << 32);
            ret |= (((long)data[pos++]) << 24);
            ret |= (((long)data[pos++]) << 16);
            ret |= (((long)data[pos++]) << 8);
            ret |= (((long)data[pos++]));
            return ret;
        }
        public static string GetUTF(byte[] data, ref int pos)
        {
            int len = GetU16(data, ref pos);
            if (len > 0)
            {
                string ret = System.Text.UTF8Encoding.UTF8.GetString(data, pos, len);
                pos += len;
                return ret;
            }
            return null;
        }
        public static byte[] GetBytes(byte[] data, ref int pos)
        {
            int len = GetU16(data, ref pos);
            byte[] ret = new byte[len];
            if (len > 0)
            {
                Array.Copy(data, pos, ret, 0, len);
                pos += len;
            }
            return ret;
        }

        public static void PutBool(byte[] data, ref int pos, bool value)
        {
            data[pos++] = (byte)(value ? 1 : 0);
        }
        public static void PutU8(byte[] data, ref int pos, byte value)
        {
            data[pos++] = value;
        }
        public static void PutS8(byte[] data, ref int pos, sbyte value)
        {
            data[pos++] = (byte)value;
        }
        public static void PutU16(byte[] data, ref int pos, ushort value)
        {
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutS16(byte[] data, ref int pos, short value)
        {
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutU32(byte[] data, ref int pos, uint value)
        {
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutS32(byte[] data, ref int pos, int value)
        {
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutU64(byte[] data, ref int pos, ulong value)
        {
            data[pos++] = (byte)(value >> 56);
            data[pos++] = (byte)(value >> 48);
            data[pos++] = (byte)(value >> 40);
            data[pos++] = (byte)(value >> 32);
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutS64(byte[] data, ref int pos, long value)
        {
            data[pos++] = (byte)(value >> 56);
            data[pos++] = (byte)(value >> 48);
            data[pos++] = (byte)(value >> 40);
            data[pos++] = (byte)(value >> 32);
            data[pos++] = (byte)(value >> 24);
            data[pos++] = (byte)(value >> 16);
            data[pos++] = (byte)(value >> 8);
            data[pos++] = (byte)(value);
        }
        public static void PutUTF(byte[] data, ref int pos, string value)
        {
            if (value == null || value.Length == 0)
            {
                PutU16(data, ref pos, 0);
            }
            else
            {
                byte[] buff = System.Text.UTF8Encoding.UTF8.GetBytes(value);
                if (buff.Length > UInt16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + value + "\nSize=" + buff.Length);
                }
                PutU16(data, ref pos, (ushort)buff.Length);
                Put(data, ref pos, buff, 0, buff.Length);
            }
        }
        public static void PutBytes(byte[] data, ref int pos, byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                PutS32(data, ref pos, 0);
            }
            else
            {
                PutS32(data, ref pos, value.Length);
                Put(data, ref pos, value, 0, value.Length);
            }
        }
        public static void Get(byte[] data, ref int pos, byte[] value, int offset, int length)
        {
            Array.Copy(data, pos, value, offset, length);
            pos += length;
        }
        public static void Put(byte[] data, ref int pos, byte[] value, int offset, int length)
        {
            Array.Copy(value, offset, data, pos, length);
            pos += length;
        }

        #endregion

        //--------------------------------------------------------------------------------------
        #region STREAM

        public static bool GetBool(Stream data)
        {
            return IOUtil.TryReadByte(data) != 0;
        }
        public static void PutBool(Stream data, bool value)
        {
            data.WriteByte((byte)(value ? 1 : 0));
        }

        public static byte GetU8(Stream data)
        {
            return (byte)IOUtil.TryReadByte(data);
        }
        public static void PutU8(Stream data, byte value)
        {
            data.WriteByte(value);
        }

        public static sbyte GetS8(Stream data)
        {
            return (sbyte)IOUtil.TryReadByte(data);
        }
        public static void PutS8(Stream data, sbyte value)
        {
            data.WriteByte((byte)value);
        }

        public static ushort GetU16(Stream data)
        {
            int ret = 0;
            ret |= (IOUtil.TryReadByte(data) << 8);
            ret |= (IOUtil.TryReadByte(data));
            return (ushort)ret;
        }
        public static void PutU16(Stream data, ushort value)
        {
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }

        public static short GetS16(Stream data)
        {
            int ret = 0;
            ret |= (IOUtil.TryReadByte(data) << 8);
            ret |= (IOUtil.TryReadByte(data));
            return (short)ret;
        }
        public static void PutS16(Stream data, short value)
        {
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }

        public static uint GetU32(Stream data)
        {
            int ret = 0;
            ret |= (IOUtil.TryReadByte(data) << 24);
            ret |= (IOUtil.TryReadByte(data) << 16);
            ret |= (IOUtil.TryReadByte(data) << 8);
            ret |= (IOUtil.TryReadByte(data));
            return (uint)ret;
        }
        public static void PutU32(Stream data, uint value)
        {
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }
        public static int GetS32(Stream data)
        {
            int ret = 0;
            ret |= (IOUtil.TryReadByte(data) << 24);
            ret |= (IOUtil.TryReadByte(data) << 16);
            ret |= (IOUtil.TryReadByte(data) << 8);
            ret |= (IOUtil.TryReadByte(data));
            return ret;
        }
        public static void PutS32(Stream data, int value)
        {
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }
        public static ulong GetU64(Stream data)
        {
            ulong ret = 0;
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 56);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 48);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 40);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 32);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 24);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 16);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 8);
            ret |= (((ulong)IOUtil.TryReadByte(data)) << 0);
            return ret;
        }
        public static void PutU64(Stream data, ulong value)
        {
            data.WriteByte((byte)(value >> 56));
            data.WriteByte((byte)(value >> 48));
            data.WriteByte((byte)(value >> 40));
            data.WriteByte((byte)(value >> 32));
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }
        public static long GetS64(Stream data)
        {
            long ret = 0;
            ret |= (((long)IOUtil.TryReadByte(data)) << 56);
            ret |= (((long)IOUtil.TryReadByte(data)) << 48);
            ret |= (((long)IOUtil.TryReadByte(data)) << 40);
            ret |= (((long)IOUtil.TryReadByte(data)) << 32);
            ret |= (((long)IOUtil.TryReadByte(data)) << 24);
            ret |= (((long)IOUtil.TryReadByte(data)) << 16);
            ret |= (((long)IOUtil.TryReadByte(data)) << 8);
            ret |= (((long)IOUtil.TryReadByte(data)) << 0);
            return ret;
        }

        public static void PutS64(Stream data, long value)
        {
            data.WriteByte((byte)(value >> 56));
            data.WriteByte((byte)(value >> 48));
            data.WriteByte((byte)(value >> 40));
            data.WriteByte((byte)(value >> 32));
            data.WriteByte((byte)(value >> 24));
            data.WriteByte((byte)(value >> 16));
            data.WriteByte((byte)(value >> 8));
            data.WriteByte((byte)(value));
        }

        //--------------------------------------------------------------------------------------

        public static float GetF32(Stream data)
        {
            byte[] buff = IOUtil.ReadExpect(data, 4);
            return System.BitConverter.ToSingle(buff, 0);
        }
        public static void PutF32(Stream data, float value)
        {
            byte[] buff = BitConverter.GetBytes(value);
            data.Write(buff, 0, 4);
        }
        public static double GetF64(Stream data)
        {
            byte[] buff = IOUtil.ReadExpect(data, 8);
            return System.BitConverter.ToDouble(buff, 0);
        }
        public static void PutF64(Stream data, double value)
        {
            byte[] buff = BitConverter.GetBytes(value);
            data.Write(buff, 0, 8);
        }

        //--------------------------------------------------------------------------------------

        public static string GetUTF(Stream data)
        {
            int len = GetU16(data);
            if (len > 0)
            {
                byte[] buff = IOUtil.ReadExpect(data, len);
                return System.Text.UTF8Encoding.UTF8.GetString(buff, 0, len);
            }
            return null;
        }
        public static void PutUTF(Stream data, string str)
        {
            if (str == null || str.Length == 0)
            {
                PutU16(data, 0);
            }
            else
            {
                byte[] buff = System.Text.UTF8Encoding.UTF8.GetBytes(str);
                if (buff.Length > UInt16.MaxValue)
                {
                    throw new IOException("PutUTF overflow : " + str + "\nSize=" + buff.Length);
                }
                PutU16(data, (ushort)buff.Length);
                data.Write(buff, 0, buff.Length);
            }
        }

        //--------------------------------------------------------------------------------------

        public static T GetEnum8<T>(Stream data, Type enumType)
        {
            return (T)Enum.ToObject(enumType, IOUtil.TryReadByte(data));
        }

        public static void PutEnum8(Stream data, object enumData)
        {
            byte b = (byte)(enumData);
            data.WriteByte(b);
        }

        //--------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------

        public delegate T GetData<T>(Stream data);
        public delegate void PutData<T>(Stream data, T v);

        public static T[] GetArray<T>(Stream data, GetData<T> action) where T : new()
        {
            int len = GetU16(data);
            T[] ret = new T[len];
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(data);
                ret[i] = d;
            }
            return ret;
        }
        public static void PutArray<T>(Stream data, T[] array, PutData<T> action)
        {
            if (array == null)
            {
                PutU16(data, 0);
            }
            else
            {
                int len = array.Length;
                if (len > UInt16.MaxValue)
                {
                    throw new IOException("PutArray overflow : " + array + "\nSize=" + len);
                }
                PutU16(data, (ushort)len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(data, array[i]);
                }
            }
        }

        public static ArrayList<T> GetList<T>(Stream data, GetData<T> action) where T : new()
        {
            int len = GetU16(data);
            ArrayList<T> ret = new ArrayList<T>(len);
            for (int i = 0; i < len; i++)
            {
                T d = action.Invoke(data);
                ret.Add(d);
            }
            return ret;
        }

        public static void PutList<T>(Stream data, IList<T> list, PutData<T> action)
        {
            if (list == null)
            {
                PutU16(data, 0);
            }
            else
            {
                int len = list.Count;
                if (len > UInt16.MaxValue)
                {
                    throw new IOException("PutList overflow : " + list + "\nSize=" + len);
                }
                PutU16(data, (ushort)len);
                for (int i = 0; i < len; i++)
                {
                    action.Invoke(data, list[i]);
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------

    }


    /// <summary>
    /// VLQ Int取值范围21位
    /// </summary>
    public static class VLQEdian
    {
        public static int GetVLQSize(ulong value)
        {
            var len = 0;
            do
            {
                value = (value >> 7);
                len++;
            }
            while (value != 0);
            return len;
        }
        public static int GetVLQSize(long value)
        {
            return GetVLQSize((ulong) value);
        }

        public static void PutVU32(Stream stream, uint value)
        {
            PutVU64(stream, value);
        }
        public static void PutVS32(Stream stream, int value)
        {
            PutVS64(stream, value);
        }
        public static void PutVS64(Stream stream, long value)
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
            PutVU64(stream, m);
        }
        public static void PutVU64(Stream stream, ulong value)
        {
            do
            {
                byte b = (byte)(value & 0x7F);
                value = (value >> 7);
                if (value != 0)
                {
                    b = (byte)(b | 0x80);
                }
                stream.WriteByte(b);
            }
            while (value != 0);
        }
        
        public static UInt32 GetVU32(Stream stream)
        {
            return (UInt32)GetVU64(stream);
        }
        public static Int32 GetVS32(Stream stream)
        {
            return (Int32)GetVS64(stream);
        }
        public static Int64 GetVS64(Stream stream)
        {
            ulong m = GetVU64(stream);
            long v = (long)(m >> 1);
            if (m % 2 == 1)
            {
                return -v;
            }
            else
            {
                return v;
            }
        }
        public static UInt64 GetVU64(Stream stream)
        {
            UInt64 value = 0;
            UInt64 b = 0;
            int u8 = 0;
            for (int i = 0; i <= 70; i += 7)
            {
                u8 = stream.ReadByte();
                if (u8 < 0) break;
                b = (UInt64)u8;
                value |= ((b & 0x7F) << i);
                if ((b & 0x80) == 0)
                    break;
            }
            return value;
        }

        public static void EncodeInt32(this Stream writer, int value)
        {
            do
            {
                byte lower7bits = (byte)(value & 0x7f);
                value >>= 7;
                if (value > 0)
                    lower7bits |= 128;
                writer.WriteByte(lower7bits);
            } while (value > 0);
        }
        public static int DecodeInt32(this Stream reader)
        {
            bool more = true;
            int value = 0;
            int shift = 0;
            while (more)
            {
                int lower7bits = reader.ReadByte();
                more = (lower7bits & 128) != 0;
                value |= (lower7bits & 0x7f) << shift;
                shift += 7;
            }
            return value;
        }

    }



}
