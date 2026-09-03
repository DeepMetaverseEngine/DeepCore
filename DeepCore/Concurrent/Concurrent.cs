using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore.Concurrent
{
    public class AtomicInteger
    {
        private int mValue;

        public AtomicInteger(int v)
        {
            this.mValue = v;
        }

        public int Value
        {
            get
            {
                lock (this)
                {
                    return mValue;
                }
            }
            set
            {
                lock (this)
                {
                    mValue = value;
                }
            }
        }


        public int GetAndSet(int val)
        {
            lock (this)
            {
                int ret = mValue;
                mValue = val;
                return ret;
            }
        }

        public int GetAndIncrement()
        {
            lock (this)
            {
                int ret = mValue;
                mValue++;
                return ret;
            }
        }

        public int GetAndDecrement()
        {
            lock (this)
            {
                int ret = mValue;
                mValue--;
                return ret;
            }
        }

        public int GetAndAdd(int delta)
        {
            lock (this)
            {
                int ret = mValue;
                mValue += delta;
                return ret;
            }
        }

        public int IncrementAndGet()
        {
            lock (this)
            {
                mValue++;
                return mValue;
            }
        }

        public int DecrementAndGet()
        {
            lock (this)
            {
                mValue--;
                return mValue;
            }
        }

        public int AddAndGet(int delta)
        {
            lock (this)
            {
                mValue += delta;
                return mValue;
            }
        }


        public bool CompareAndSet(int expect, int update)
        {
            lock (this)
            {
                if (expect == mValue)
                {
                    mValue = update;
                    return true;
                }
                return false;
            }
        }


        public static AtomicInteger operator +(AtomicInteger value1, int value2)
        {
            lock (value1)
            {
                value1.Value += value2;
            }
            return value1;
        }
        public static AtomicInteger operator -(AtomicInteger value1, int value2)
        {
            lock (value1)
            {
                value1.Value -= value2;
            }
            return value1;
        }
        public static AtomicInteger operator *(AtomicInteger value1, int value2)
        {
            lock (value1)
            {
                value1.Value *= value2;
            }
            return value1;
        }
        public static AtomicInteger operator /(AtomicInteger value1, int value2)
        {
            lock (value1)
            {
                value1.Value /= value2;
            }
            return value1;
        }

        public static AtomicInteger operator ++(AtomicInteger value1)
        {
            lock (value1)
            {
                value1.Value += 1;
            }
            return value1;
        }
        public static AtomicInteger operator --(AtomicInteger value1)
        {
            lock (value1)
            {
                value1.Value -= 1;
            }
            return value1;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }

    }



    public class AtomicLong
    {
        private long mValue;

        public AtomicLong(long v)
        {
            this.mValue = v;
        }

        public long Value
        {
            get
            {
                lock (this)
                {
                    return mValue;
                }
            }
            set
            {
                lock (this)
                {
                    mValue = value;
                }
            }
        }


        public long GetAndSet(long val)
        {
            lock (this)
            {
                long ret = mValue;
                mValue = val;
                return ret;
            }
        }

        public long GetAndIncrement()
        {
            lock (this)
            {
                long ret = mValue;
                mValue++;
                return ret;
            }
        }

        public long GetAndDecrement()
        {
            lock (this)
            {
                long ret = mValue;
                mValue--;
                return ret;
            }
        }

        public long GetAndAdd(long delta)
        {
            lock (this)
            {
                long ret = mValue;
                mValue += delta;
                return ret;
            }
        }

        public long IncrementAndGet()
        {
            lock (this)
            {
                mValue++;
                return mValue;
            }
        }

        public long DecrementAndGet()
        {
            lock (this)
            {
                mValue--;
                return mValue;
            }
        }

        public long AddAndGet(long delta)
        {
            lock (this)
            {
                mValue += delta;
                return mValue;
            }
        }


        public bool CompareAndSet(long expect, long update)
        {
            lock (this)
            {
                if (expect == mValue)
                {
                    mValue = update;
                    return true;
                }
                return false;
            }
        }




        public static AtomicLong operator +(AtomicLong value1, long value2)
        {
            lock (value1)
            {
                value1.Value += value2;
            }
            return value1;
        }
        public static AtomicLong operator -(AtomicLong value1, long value2)
        {
            lock (value1)
            {
                value1.Value -= value2;
            }
            return value1;
        }
        public static AtomicLong operator *(AtomicLong value1, long value2)
        {
            lock (value1)
            {
                value1.Value *= value2;
            }
            return value1;
        }
        public static AtomicLong operator /(AtomicLong value1, long value2)
        {
            lock (value1)
            {
                value1.Value /= value2;
            }
            return value1;
        }
        public static AtomicLong operator ++(AtomicLong value1)
        {
            lock (value1)
            {
                value1.Value += 1;
            }
            return value1;
        }
        public static AtomicLong operator --(AtomicLong value1)
        {
            lock (value1)
            {
                value1.Value -= 1;
            }
            return value1;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }

    }


    public class AtomicUInt
    {
        private uint mValue;

        public AtomicUInt(uint v)
        {
            this.mValue = v;
        }

        public uint Value
        {
            get
            {
                lock (this)
                {
                    return mValue;
                }
            }
            set
            {
                lock (this)
                {
                    mValue = value;
                }
            }
        }


        public uint GetAndSet(uint val)
        {
            lock (this)
            {
                uint ret = mValue;
                mValue = val;
                return ret;
            }
        }

        public uint GetAndIncrement()
        {
            lock (this)
            {
                uint ret = mValue;
                mValue++;
                return ret;
            }
        }

        public uint GetAndDecrement()
        {
            lock (this)
            {
                uint ret = mValue;
                mValue--;
                return ret;
            }
        }

        public uint GetAndAdd(uint delta)
        {
            lock (this)
            {
                uint ret = mValue;
                mValue += delta;
                return ret;
            }
        }

        public uint IncrementAndGet()
        {
            lock (this)
            {
                mValue++;
                return mValue;
            }
        }

        public uint DecrementAndGet()
        {
            lock (this)
            {
                mValue--;
                return mValue;
            }
        }

        public uint AddAndGet(uint delta)
        {
            lock (this)
            {
                mValue += delta;
                return mValue;
            }
        }


        public bool CompareAndSet(uint expect, uint update)
        {
            lock (this)
            {
                if (expect == mValue)
                {
                    mValue = update;
                    return true;
                }
                return false;
            }
        }



        public static AtomicUInt operator +(AtomicUInt value1, uint value2)
        {
            lock (value1)
            {
                value1.Value += value2;
            }
            return value1;
        }
        public static AtomicUInt operator -(AtomicUInt value1, uint value2)
        {
            lock (value1)
            {
                value1.Value -= value2;
            }
            return value1;
        }
        public static AtomicUInt operator *(AtomicUInt value1, uint value2)
        {
            lock (value1)
            {
                value1.Value *= value2;
            }
            return value1;
        }
        public static AtomicUInt operator /(AtomicUInt value1, uint value2)
        {
            lock (value1)
            {
                value1.Value /= value2;
            }
            return value1;
        }
        public static AtomicUInt operator ++(AtomicUInt value1)
        {
            lock (value1)
            {
                value1.Value += 1;
            }
            return value1;
        }
        public static AtomicUInt operator --(AtomicUInt value1)
        {
            lock (value1)
            {
                value1.Value -= 1;
            }
            return value1;
        }
        public override string ToString()
        {
            return mValue.ToString();
        }

    }

    public class AtomicFloat
    {
        private float mValue;

        public AtomicFloat(float v)
        {
            this.mValue = v;
        }

        public float Value
        {
            get
            {
                lock (this)
                {
                    return mValue;
                }
            }
            set
            {
                lock (this)
                {
                    mValue = value;
                }
            }
        }


        public float GetAndSet(float val)
        {
            lock (this)
            {
                float ret = mValue;
                mValue = val;
                return ret;
            }
        }

        public float GetAndIncrement()
        {
            lock (this)
            {
                float ret = mValue;
                mValue++;
                return ret;
            }
        }

        public float GetAndDecrement()
        {
            lock (this)
            {
                float ret = mValue;
                mValue--;
                return ret;
            }
        }

        public float GetAndAdd(float delta)
        {
            lock (this)
            {
                float ret = mValue;
                mValue += delta;
                return ret;
            }
        }

        public float AddAndGet(float delta)
        {
            lock (this)
            {
                mValue += delta;
                return mValue;
            }
        }

        public bool CompareAndSet(float expect, float update)
        {
            lock (this)
            {
                if (expect == mValue)
                {
                    mValue = update;
                    return true;
                }
                return false;
            }
        }


        public static AtomicFloat operator +(AtomicFloat value1, float value2)
        {
            lock (value1)
            {
                value1.Value += value2;
            }
            return value1;
        }
        public static AtomicFloat operator -(AtomicFloat value1, float value2)
        {
            lock (value1)
            {
                value1.Value -= value2;
            }
            return value1;
        }
        public static AtomicFloat operator *(AtomicFloat value1, float value2)
        {
            lock (value1)
            {
                value1.Value *= value2;
            }
            return value1;
        }
        public static AtomicFloat operator /(AtomicFloat value1, float value2)
        {
            lock (value1)
            {
                value1.Value /= value2;
            }
            return value1;
        }
        public static AtomicFloat operator ++(AtomicFloat value1)
        {
            lock (value1)
            {
                value1.Value += 1;
            }
            return value1;
        }
        public static AtomicFloat operator --(AtomicFloat value1)
        {
            lock (value1)
            {
                value1.Value -= 1;
            }
            return value1;
        }
        public override string ToString()
        {
            return mValue.ToString();
        }

    }

    public class AtomicReference<T>
    {
        private T mData;

        public AtomicReference(T data)
        {
            this.mData = data;
        }

        public T Value
        {
            get
            {
                lock (this)
                {
                    return mData;
                }
            }
            set
            {
                lock (this)
                {
                    mData = value;
                }
            }
        }

        public bool Update(T data)
        {
            lock (this)
            {
                if (!object.Equals(mData, data))
                {
                    mData = data;
                    return true;
                }
                return false;
            }
        }
        public T GetOrCreate(Func<T> create)
        {
            lock (this)
            {
                if (mData == null)
                {
                    mData = create();
                }
                return mData;
            }
        }
        public T GetOrCreate(out bool exist, Func<T> create)
        {
            lock (this)
            {
                if (mData == null)
                {
                    exist = false;
                    mData = create();
                }
                else
                {
                    exist = true;
                }
                return mData;
            }
        }
        public T GetAndSet(T data)
        {
            lock (this)
            {
                T ret = mData;
                mData = data;
                return ret;
            }
        }
        public T TryUpdate(Func<T, T> func)
        {
            lock (this)
            {
                var old = mData;
                var update = func(old);
                mData = update;
                return mData;
            }
        }
        public bool CompareAndSet(Predicate<T> expect, out T exist, T update)
        {
            lock (this)
            {
                if (expect(mData))
                {
                    exist = mData;
                    mData = update;
                    return true;
                }
            }
            exist = default(T);
            return false;
        }
        public bool CompareAndSet(Predicate<T> expect, T update)
        {
            lock (this)
            {
                if (expect(mData))
                {
                    mData = update;
                    return true;
                }
                return false;
            }
        }
        public bool CompareAndSet(T expect, T update)
        {
            lock (this)
            {
                if (expect.Equals(mData))
                {
                    mData = update;
                    return true;
                }
                return false;
            }
        }
        public bool CompareNotAndSet(T expect, T update)
        {
            lock (this)
            {
                if (!expect.Equals(mData))
                {
                    mData = update;
                    return true;
                }
                return false;
            }
        }
        public override string ToString()
        {
            return mData + "";
        }

    }

    public class LazyReference<T>
    {
        private bool inited = false;
        private Func<T> init;
        private T value;
        public LazyReference(Func<T> func)
        {
            this.init = func;
        }
        public T Value
        {
            get
            {
                if (!inited)
                {
                    lock (this)
                    {
                        if (!inited)
                        {
                            this.value = init();
                            this.inited = true;
                        }
                    }
                }
                return value;
            }
        }
    }

    public class IDGenerator
    {
        private uint indexer = 0;

        public uint NextID()
        {
            lock (this)
            {
                indexer++;
                uint ret = indexer;
                return ret;
            }
        }

        public uint Regist(uint value)
        {
            lock (this)
            {
                indexer = Math.Max(indexer, value);
            }
            return value;
        }
    }


    public interface IRangeValue
    {
        public static bool ENABLE_SHOW_LOAD_PROGRESS = true;
        string Text { get; set; }
        long Min { get; }
        long Max { get; }
        long Value { get; }
        float Rate { get; }

        IRangeValue Update();
        IRangeValue Reset(long max);
        IRangeValue SetRange(long min, long max, long value);
        IRangeValue SetMin(long min);
        IRangeValue SetMax(long max, bool autoGenValue = false);
        IRangeValue SetText(string txt);
        IRangeValue SetValue(long value);
        IRangeValue Add(long add);
    }

    public class AtomicRangeValue : IRangeValue
    {
        private long mMin = 0;
        private long mMax = 0;
        private long mValue = 0;
        public bool Break = false;
        private string text;
        public long Min { get { return mMin; } }
        public long Max { get { return mMax; } }
        public long Value { get { return mValue; } }
        public float Rate { get { { return (float)((mMax == mMin) ? 1 : (mValue - mMin) / (double)(mMax - mMin)); } } }
        public string Text { get => text; set => SetText(value); }
        public AtomicRangeValue() : this(0, 0, 1) { }
        public AtomicRangeValue(long value, long min, long max)
        {
            mMin = Math.Min(min, max);
            mMax = Math.Max(min, max);
            SetValue(value);
        }
        public virtual IRangeValue Update() { return this; }
        public void SetMax() { SetValue(Max); }
        public void SetZero() { SetValue(Min); }
        public void SetIdentity() { SetRange(0, 1, 0); }
        public override string ToString()
        {
            lock (this)
            {
                var p = mValue - mMin;
                var len = mMax - mMin;
                return $"{p}/{len}";
            }
        }
        public string ToStringPercent()
        {
            lock (this)
            {
                var p = mValue - mMin;
                var len = mMax - mMin;
                return $"{(100 * p / len)}%";
            }
        }
        public virtual IRangeValue Reset(long max)
        {
            lock (this)
            {
                mMin = 0;
                mMax = max;
                mValue = 0;
            }
            return this;
        }
        public virtual IRangeValue SetRange(long min, long max, long value)
        {
            lock (this)
            {
                mMin = Math.Min(min, max);
                mMax = Math.Max(min, max);
                mValue = value;
                mValue = Math.Min(mValue, mMax);
                mValue = Math.Max(mValue, mMin);
            }
            return this;
        }
        public virtual IRangeValue SetMin(long min)
        {
            lock (this)
            {
                if (min != mMin && min <= mMax)
                {
                    mMin = min;
                    mValue = Math.Min(mValue, mMax);
                    mValue = Math.Max(mValue, mMin);
                }
            }
            return this;
        }
        public virtual IRangeValue SetMax(long max, bool autoGenValue = false)
        {
            lock (this)
            {
                if (max != mMax && max >= mMin)
                {
                    if (autoGenValue)
                    {
                        double addrat = (max / (double)mMax) - 1f;
                        mMax = max;
                        Add((int)(mValue * addrat));
                    }
                    else
                    {
                        mMax = max;
                        mValue = Math.Min(mValue, mMax);
                        mValue = Math.Max(mValue, mMin);
                    }
                }
            }
            return this;
        }
        public virtual IRangeValue SetValue(long value)
        {
            lock (this)
            {
                if (value != mValue)
                {
                    mValue = value;
                    return this;
                }
            }
            return this;
        }
        public virtual IRangeValue Add(long add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return this;
        }
        public virtual IRangeValue SetText(string txt)
        {
            if (IRangeValue.ENABLE_SHOW_LOAD_PROGRESS)
            {
                lock (this) { text = txt; }
            }
            return this;
        }
    }

    public class AtomicRangeValueF
    {
        private double mMin = 0;
        private double mMax = 0;
        private double mValue = 0;

        public double Min { get { return mMin; } }
        public double Max { get { return mMax; } }
        public double Value { get { return mValue; } }
        public double Rate { get { { return (mMax == mMin) ? 1 : (mValue - mMin) / (mMax - mMin); } } }

        public AtomicRangeValueF() : this(0, 0, 1) { }
        public AtomicRangeValueF(double value, double min, double max)
        {
            mMin = Math.Min(min, max);
            mMax = Math.Max(min, max);
            SetValue(value);
        }
        public void Reset(double max)
        {
            lock (this)
            {
                mMin = 0;
                mMax = max;
                mValue = 0;
            }
        }
        public bool SetMin(double min)
        {
            lock (this)
            {
                if (min != mMin && min <= mMax)
                {
                    mMin = min;
                    mValue = Math.Min(mValue, mMax);
                    mValue = Math.Max(mValue, mMin);
                    return true;
                }
                return false;
            }
        }
        public bool SetMax(double max, bool autoGenValue = false)
        {
            lock (this)
            {
                if (max != mMax && max >= mMin)
                {
                    if (autoGenValue)
                    {
                        double addrat = (max / mMax) - 1f;
                        mMax = max;
                        Add(mValue * addrat);
                    }
                    else
                    {
                        mMax = max;
                        mValue = Math.Min(mValue, mMax);
                        mValue = Math.Max(mValue, mMin);
                    }
                    return true;
                }
                return false;
            }
        }
        public bool SetValue(double value)
        {
            lock (this)
            {
                if (value != mValue)
                {
                    mValue = value;
                    return true;
                }
            }
            return false;
        }
        public bool Add(double add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return false;
        }
    }




    //     public class AtomicProgress : AtomicRangeValue
    //     {
    //         private string text;
    //         public string Text { get => text; set { lock (this) { text = value; } } }
    //     }
}
