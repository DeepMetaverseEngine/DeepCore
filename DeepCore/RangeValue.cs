using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{

    public class RangeValue
    {
        private int mMin = 0;
        private int mMax = 0;
        private int mValue = 0;

        public int Min { get { return mMin; } }
        public int Max { get { return mMax; } }
        public int Value { get { return mValue; } }

        public RangeValue(int value, int min, int max)
        {
            mMin = Math.Min(min, max);
            mMax = Math.Max(min, max);
            SetValue(value);
        }
        public bool SetMin(int min)
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
        public bool SetMax(int max, bool autoGenValue = false)
        {
            if (max != mMax && max >= mMin)
            {
                if (autoGenValue)
                {
                    if (mValue == 0) mValue = 1;
                    float addrat = (max / (float)mMax) - 1f;
                    mMax = max;
                    Add((int)((mValue * addrat) + 0.5f));
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
        public  delegate void Overflowhandle(int orgvalue, int newvalue);
        public event Overflowhandle OnOverflowHandle;

        public bool SetValue(int value)
        {
            if (value != mValue)
            {
                if (value > mMax)
                {
                    OnOverflowHandle?.Invoke(mValue,value);
                    value = mMax;
                }
                else if (value < mMin)
                {
                    OnOverflowHandle?.Invoke(mValue,value);
                    value = mMin;
                }
                if (value != mValue)
                {
                    mValue = value;
                    return true;
                }
            }
            return false;
        }
        public bool Add(int add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return false;
        }
        public void Dispose()
        {
            OnOverflowHandle = null;
        }
    }
    public class RangeValueF
    {
        private float mMin = 0;
        private float mMax = 0;
        private float mValue = 0;

        public float Min { get { return mMin; } }
        public float Max { get { return mMax; } }
        public float Value { get { return mValue; } }
        public  delegate void Overflowhandle(float orgvalue, float newvalue);
        public event Overflowhandle OnOverflowHandle;
        public RangeValueF(float value, float min, float max)
        {
            mMin = Math.Min(min, max);
            mMax = Math.Max(min, max);
            SetValue(value);
        }
        public bool SetMin(float min)
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
        public bool SetMax(float max, bool autoGenValue = false)
        {
            if (max != mMax && max >= mMin)
            {
                if (autoGenValue)
                {
                    float addrat = (max / mMax) - 1f;
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
        public bool SetValue(float value)
        {
            if (value != mValue)
            {
                if (value > mMax)
                {
                    OnOverflowHandle?.Invoke(mValue,value);
                    value = mMax;
                }
                else if (value < mMin)
                {
                    OnOverflowHandle?.Invoke(mValue,value);
                    value = mMin;
                }
                if (value != mValue)
                {
                    mValue = value;
                    return true;
                }
            }
            return false;
        }
        public void Dispose()
        {
            OnOverflowHandle = null;
        }
        public bool Add(float add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return false;
        }
    }

}
