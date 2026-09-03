using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore
{
    public struct BitSet8
    {
        private byte mMask;
        public byte Mask { get { return mMask; } set { mMask = value; } }
        public BitSet8(byte mask) { mMask = mask; }
        public void Clear()
        {
            mMask = 0;
        }
        public bool Get(int i)
        {
            return BitMask.BitGetMask(mMask, i);
        }
        public bool Set(int i, bool value)
        {
            BitMask.BitSetMask(ref mMask, i, value);
            return value;
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU8(mMask);
        }
        public void ReadExternal(IInputStream input)
        {
            this.mMask = input.GetU8();
        }
    }
    public struct BitSet16
    {
        private short mMask;
        public short Mask { get { return mMask; } set { mMask = value; } }
        public BitSet16(short mask) { mMask = mask; }
        public void Clear()
        {
            mMask = 0;
        }
        public bool Get(int i)
        {
            return BitMask.BitGetMask(mMask, i);
        }
        public void Set(int i, bool value)
        {
            BitMask.BitSetMask(ref mMask, i, value);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutS16(mMask);
        }
        public void ReadExternal(IInputStream input)
        {
            this.mMask = input.GetS16();
        }
    }

    public struct BitSet32
    {
        private int mMask;
        public int Mask { get { return mMask; } set { mMask = value; } }
        public BitSet32(int mask) { mMask = mask; }
        public void Clear()
        {
            mMask = 0;
        }
        public bool Get(int i)
        {
            return BitMask.BitGetMask(mMask, i);
        }
        public void Set(int i, bool value)
        {
            BitMask.BitSetMask(ref mMask, i, value);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(mMask);
        }
        public void ReadExternal(IInputStream input)
        {
            this.mMask = input.GetS32();
        }
    }

    public struct BitSet64
    {
        private long mMask;
        public long Mask { get { return mMask; } set { mMask = value; } }
        public BitSet64(long mask) { mMask = mask; }
        public void Clear()
        {
            mMask = 0;
        }
        public bool Get(int i)
        {
            return BitMask.BitGetMask(mMask, i);
        }
        public void Set(int i, bool value)
        {
            BitMask.BitSetMask(ref mMask, i, value);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutS64(mMask);
        }
        public void ReadExternal(IInputStream input)
        {
            this.mMask = input.GetS64();
        }
    }

    public class BitSetVector
    {
        private List<byte> mMasks;
        public BitSetVector(int capacity)
        {
            mMasks = new List<byte>(capacity);
        }

        public BitSetVector()
        {
            mMasks = new List<byte>(1);
        }
        public bool Get(int i)
        {
            var d = i / 8;
            if (mMasks.Count <= d) { return false; }
            var mask = mMasks[d];
            return BitMask.BitGetMask(mask, i % 8);
        }

        public bool IsEmpty => mMasks.All(m => m == 0);
        public void Clear()
        {
            for (var i = 0; i < mMasks.Count; i++)
            {
                mMasks[i] = 0;
            }
        }

        public int CurrentBitCount => mMasks.Capacity * 8;
        public void Set(int i, bool value)
        {
            var d = i / 8;
            if (mMasks.Count <= d) { CUtils.SetListLength<byte>(mMasks, d + 1); }
            var mask = mMasks[d];
            BitMask.BitSetMask(ref mask, i % 8, value);
            mMasks[d] = mask;
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutList(mMasks, static (t, v) => t.PutU8(v));
        }
        public void ReadExternal(IInputStream input)
        {
            this.mMasks = input.GetList(static t => t.GetU8());
        }
    }


    public static class BitMask
    {
        //         public static void BitSetMask<E>(ref E mask, int i, bool value) where E : unmanaged
        //         {
        //             unsafe
        //             {
        //                 switch (sizeof(E))
        //                 {
        //                     case sizeof(byte):
        //                         byte _byte = Convert.ToByte(mask);
        //                         BitSetMask(ref _byte,  i,  value);
        //                         mask = (E)_byte;
        //                         break;
        //                     case sizeof(short):
        //                         if (value)
        //                         {
        //                             mask |= (short)(1 << i);
        //                         }
        //                         else
        //                         {
        //                             mask &= (short)(~(1 << i));
        //                         }
        //                         break;
        //                     case sizeof(int):
        //                         if (value)
        //                         {
        //                             mask |= (1 << i);
        //                         }
        //                         else
        //                         {
        //                             mask &= (~(1 << i));
        //                         }
        //                         break;
        //                     case sizeof(long):
        //                         long m = (1L << i);
        //                         if (value)
        //                         {
        //                             mask |= m;
        //                         }
        //                         else
        //                         {
        //                             mask &= ~m;
        //                         }
        //                         break;
        //                 }
        //             }
        //         }

        public static void BitSetMask(ref byte mask, int i, bool value)
        {
            if (value)
            {
                mask |= (byte)(1 << i);
            }
            else
            {
                mask &= (byte)(~(1 << i));
            }
        }
        public static bool BitGetMask(byte mask, int i)
        {
            return (mask & (1 << i)) != 0;
        }

        public static void BitSetMask(ref short mask, int i, bool value)
        {
            if (value)
            {
                mask |= (short)(1 << i);
            }
            else
            {
                mask &= (short)(~(1 << i));
            }
        }
        public static bool BitGetMask(short mask, int i)
        {
            return (mask & (1 << i)) != 0;
        }

        public static void BitSetMask(ref int mask, int i, bool value)
        {
            if (value)
            {
                mask |= (1 << i);
            }
            else
            {
                mask &= (~(1 << i));
            }
        }
        public static bool BitGetMask(int mask, int i)
        {
            return (mask & (1 << i)) != 0;
        }

        public static void BitSetMask(ref long mask, int i, bool value)
        {
            long m = (1L << i);
            if (value)
            {
                mask |= m;
            }
            else
            {
                mask &= ~m;
            }
        }
        public static bool BitGetMask(long mask, int i)
        {
            long m = (1L << i);
            return (mask & m) != 0;
        }



    }

    public static class EnumMask
    {
        public static void SetMask<T>(ref T enumValue, T mask, bool value) where T : Enum
        {
            var v = (Convert.ToInt64(enumValue));
            var m = (Convert.ToInt64(mask));
            if (value)
            {
                v |= m;
            }
            else
            {
                v &= ~m;
            }
            enumValue = (T)Enum.ToObject(typeof(T), v);
        }
        public static bool GetMask<T>(T enumValue, T mask) where T : Enum
        {
            return enumValue.HasFlag(mask);
        }
        public static bool HasMask<T>(this T enumValue, T mask) where T : Enum
        {
            return enumValue.HasFlag(mask);
        }
        public static bool AnyFlag<T>(this T type, T resType) where T : Enum
        {
            return (Convert.ToInt64(type) & Convert.ToInt64(resType)) != 0;
        }
        public static bool AnyFlag<T>(this T type, params T[] resType) where T : Enum
        {
            foreach (var res in resType)
            {
                if ((Convert.ToInt64(type) & Convert.ToInt64(res)) != 0)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
