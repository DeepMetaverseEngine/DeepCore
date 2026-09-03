using System;
using System.Runtime.Serialization;

namespace DeepCore
{
    [Serializable]
    public class UnionValueArray : ISerializable
    {
        private const int DefaultCapacity = 4;
        private const int MaxArrayLength = 0X7FEFFFFF;

        internal UnionValue[] InnerArray;

        private int mContentSize;

        public ref UnionValue this[int index] => ref InnerArray[index];

        public void Set(int index, UnionValue v)
        {
            if (index < 0 || index >= mContentSize)
            {
                throw new ArgumentOutOfRangeException();
            }

            InnerArray[index] = v;
        }

        public int Count => mContentSize;

        public int Capacity
        {
            get => InnerArray.Length;
            private set
            {
                if (value < mContentSize)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != InnerArray.Length)
                {
                    if (value > 0)
                    {
                        var newItems = new UnionValue[value];
                        if (mContentSize > 0)
                        {
                            Array.Copy(InnerArray, 0, newItems, 0, mContentSize);
                        }

                        InnerArray = newItems;
                    }
                    else
                    {
                        InnerArray = new UnionValue[DefaultCapacity];
                    }
                }
            }
        }

        private void EnsureCapacity(int min)
        {
            if (InnerArray.Length >= min)
            {
                return;
            }

            var newCapacity = InnerArray.Length == 0 ? DefaultCapacity : InnerArray.Length * 2;

            if ((uint) newCapacity > MaxArrayLength)
            {
                newCapacity = MaxArrayLength;
            }

            if (newCapacity < min) newCapacity = min;
            Capacity = newCapacity;
        }

        public override string ToString()
        {
            return string.Concat(InnerArray, ",");
        }


        public UnionValueArray(UnionValue[] collection = null)
        {
            Init(collection);
        }

        public UnionValueArray(int capacity)
        {
            InnerArray = new UnionValue[capacity];
        }

        private void Init(UnionValue[] collection)
        {
            if (collection != null)
            {
                InnerArray = collection;
                mContentSize = collection.Length;
            }
            else
            {
                InnerArray = new UnionValue[DefaultCapacity];
                mContentSize = 0;
            }
        }

        public int Add(UnionValue value)
        {
            if (mContentSize == InnerArray.Length)
            {
                EnsureCapacity(mContentSize + 1);
            }

            InnerArray[mContentSize] = value;
            return mContentSize++;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= mContentSize)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            mContentSize--;
            if (index < mContentSize)
            {
                Array.Copy(InnerArray, index + 1, InnerArray, index, mContentSize - index);
            }

            InnerArray[mContentSize] = UnionValue.Null;
        }

        public void Clear()
        {
            if (mContentSize > 0)
            {
                Array.Clear(InnerArray, 0, mContentSize);
                mContentSize = 0;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(InnerArray), InnerArray);
        }

        public UnionValueArray(SerializationInfo info, StreamingContext context)
        {
            var collection = info.GetValue(nameof(InnerArray), typeof(UnionValue[])) as UnionValue[];
            Init(collection);
        }
    }
}