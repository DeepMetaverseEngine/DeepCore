using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{
#if false
    public struct ffloat
    {
        public const long MaxValue = long.MaxValue;
        public const long MinValue = long.MinValue;

        public const long SPLIT_LONG = 100000;
        public const int SPLIT_INT = 100000;
        public const float SPLIT_FLOAT = 100000.0f;
        public const double SPLIT_DOUBLE = 100000.0;
        public long value;
        //----------------------------------------------------------------------
        public ffloat() { value = 0; }
        public ffloat(ffloat other) { this.value = other.value; }
        public ffloat(long value) { this.value = Parse(value); }
        public ffloat(double value) { this.value = Parse(value); }
        public ffloat(float value) { this.value = Parse(value); }
        public ffloat(int value) { this.value = Parse(value); }

        public static implicit operator ffloat(float value) { return new ffloat(value); }
        public static implicit operator ffloat(double value) { return new ffloat(value); }
        public static implicit operator ffloat(int value) { return new ffloat(value); }
        public static implicit operator ffloat(long value) { return new ffloat(value); }

        public static implicit operator float(ffloat value) { return ToFloat(value); }
        public static implicit operator double(ffloat value) { return ToDouble(value); }
        public static implicit operator int(ffloat value) { return ToInt32(value); }
        public static implicit operator long(ffloat value) { return ToInt64(value); }
        //----------------------------------------------------------------------
        public static long Parse(float v)
        {
            double tmp = v;
            return (long)(tmp * SPLIT_DOUBLE);
        }
        public static long Parse(double v)
        {
            double tmp = v;
            return (long)(tmp * SPLIT_DOUBLE);
        }
        public static long Parse(int v)
        {
            long tmp = v;
            return (long)(tmp * SPLIT_INT);
        }
        public static long Parse(long v)
        {
            long tmp = v;
            return (long)(tmp * SPLIT_LONG);
        }
        public static double ToDouble(ffloat v)
        {
            return ((double)v.value / SPLIT_DOUBLE);
        }
        public static float ToFloat(ffloat v)
        {
            return (float)((double)v.value / SPLIT_DOUBLE);
        }
        public static long ToInt64(ffloat v)
        {
            return (long)(v.value / SPLIT_LONG);
        }
        public static int ToInt32(ffloat v)
        {
            return (int)(v.value / SPLIT_LONG);
        }
        //----------------------------------------------------------------------
        public static ffloat operator *(ffloat x, ffloat y)
        {
            return new ffloat() { value = x.value * y.value / SPLIT_LONG };
        }
        public static ffloat operator /(ffloat x, ffloat y)
        {
            return new ffloat() { value = x.value * SPLIT_LONG / y.value  };
        }
    }

#endif
}
