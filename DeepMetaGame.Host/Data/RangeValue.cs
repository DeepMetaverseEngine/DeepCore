using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.Data
{
    public class RangeValueL
    {
        private long mMax = 0;
        private long mValue = 0;
        public long Max { get { return mMax; } }
        public long Value { get { return mValue; } }
        public RangeValueL(long value, long max)
        {
            mMax = max;
            SetValue(value);
        }
        public bool SetMax(long max, bool autoGenValue = false)
        {
            if (max != mMax)
            {
                if (autoGenValue)
                {
                    if (mValue == 0) mValue = 1;
                    var addrat = (max / (double)mMax) - 1f;
                    mMax = max;
                    Add((long)((mValue * addrat) + 0.5f));
                }
                else
                {
                    mMax = max;
                    mValue = Math.Min(mValue, mMax);
                }
                return true;
            }
            return false;
        }

        public bool SetValue(long value)
        {
            if (value != mValue)
            {
                if (value > mMax)
                {
                    value = mMax;
                }
                if (value != mValue)
                {
                    mValue = value;
                    return true;
                }
            }
            return false;
        }
        public bool Add(long add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return false;
        }
    }
}
