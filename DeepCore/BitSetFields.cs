using System;
using DeepCore.IO;

namespace DeepCore
{
    public class BitSetFields
    {
        private readonly BitSetVector mMask;
        private object[] mFields;
        public int CurrentBitCount => mMask.CurrentBitCount;

        public BitSetFields()
        {
            mMask = new BitSetVector(1);
        }

        public bool IsEmpty => mMask.IsEmpty;
        // public bool IsOutStreamed { get; private set; }

        public T GetField<T>(int index)
        {
            TryGetField(index, out T ret);
            return ret;
        }

        public void Clear()
        {
            // IsOutStreamed = false;
            mMask.Clear();
            if (mFields != null)
            {
                for (var i = 0; i < mFields.Length; i++)
                {
                    mFields[i] = null;
                }
            }
        }

        public bool TryGetField<T>(int index, out T ret)
        {
            ret = default;
            if (!TryGetField(index, out var obj))
            {
                return false;
            }

            if (obj is T tObj)
            {
                ret = tObj;
            }

            return CUtils.TryConvertTo<T>(obj, out ret);
        }

        public virtual bool ExistsField(int index)
        {
            return mMask.Get(index);
        }

        public virtual bool SetField(int index, object val)
        {
            if (TryGetField(index, out var ret) && Equals(ret, val))
            {
                return false;
            }

            EnsureFieldArray(index);
            mMask.Set(index, true);
            mFields[index] = val;
            return true;
        }

        private void EnsureFieldArray(int index)
        {
            if (mFields == null)
            {
                mFields = new object[mMask.CurrentBitCount];
            }

            if (index >= mFields.Length)
            {
                var count = Math.Max(index << 1, mMask.CurrentBitCount);
                Array.Resize(ref mFields, count);
            }
        }

        public virtual bool TryGetField(int index, out object ret)
        {
            if (!ExistsField(index))
            {
                ret = null;
                return false;
            }

            ret = mFields[index];
            return true;
        }


        public void WriteExternal(IOutputStream output)
        {
            mMask.WriteExternal(output);
            if (mFields != null)
            {
                for (var i = 0; i < mFields.Length; i++)
                {
                    if (ExistsField(i))
                    {
                        output.PutRawData(mFields[i]);
                    }
                }
            }

            // IsOutStreamed = true;
        }

        public void ReadExternal(IInputStream input)
        {
            mMask.ReadExternal(input);
            for (var i = 0; i < mMask.CurrentBitCount; i++)
            {
                if (mMask.Get(i))
                {
                    EnsureFieldArray(i);
                    mFields[i] = input.GetRawData();
                }
            }
        }
    }

    public class DiffTrackingBitSetFields : BitSetFields
    {
        private bool mDirty;
        private readonly BitSetFields mChanged = new BitSetFields();

        public delegate bool CheckMergerHandler(int index, object obj);

        public delegate void OnFieldChangedHandler(int index);

        public override bool ExistsField(int index)
        {
            if (mDirty)
            {
                return mChanged.ExistsField(index) || base.ExistsField(index);
            }

            return base.ExistsField(index);
        }


        public override bool SetField(int index, object val)
        {
            if (!mDirty)
            {
                mChanged.Clear();// = new BitSetFields();
                // if (!mChanged.IsOutStreamed)
                // {
                //     mChanged = new BitSetFields();
                // }
                // else
                // {
                //     mChanged.Clear();
                // }
            }

            if (TryGetField(index, out var ret) && Equals(ret, val))
            {
                return false;
            }

            mDirty = true;
            mChanged.SetField(index, val);
            return true;
        }

        public override bool TryGetField(int index, out object ret)
        {
            if (mDirty)
            {
                return mChanged.TryGetField(index, out ret) || base.TryGetField(index, out ret);
            }

            return base.TryGetField(index, out ret);
        }

        /// <summary>
        /// return changed
        /// </summary>
        /// <returns></returns>
        public BitSetFields Flush()
        {
            if (!mDirty)
            {
                return null;
            }

            mDirty = false;

            var count = Math.Max(CurrentBitCount, mChanged.CurrentBitCount);
            for (var i = 0; i < count; i++)
            {
                if (!mChanged.TryGetField(i, out var obj))
                {
                    continue;
                }

                SetFieldInternal(i, obj);
            }

            return mChanged;
        }

        private bool SetFieldInternal(int index, object obj)
        {
            return base.SetField(index, obj);
        }


        public void SetAllDirty()
        {
            Flush();
            for (var i = 0; i < CurrentBitCount; i++)
            {
                if (TryGetField(i, out var ret))
                {
                    SetFieldInternal(i, null);
                    SetField(i, ret);
                }
            }
        }


        public void Merger(BitSetFields other, bool flushNow, CheckMergerHandler predicate = null, OnFieldChangedHandler changed = null)
        {
            for (var i = 0; i < other.CurrentBitCount; i++)
            {
                if (!other.TryGetField(i, out var obj) || (predicate != null && !predicate.Invoke(i, obj)))
                {
                    continue;
                }

                if (flushNow)
                {
                    SetFieldInternal(i, obj);
                    changed(i);
                }
                else
                {
                    SetField(i, obj);
                    changed(i);
                }
            }
        }
    }
}