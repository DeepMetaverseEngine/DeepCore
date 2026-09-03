using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DeepCore
{
    [Serializable]
    public class UnionValueMap : ISerializable, IEnumerable<KeyValuePair<UnionValue, UnionValue>>
    {
        internal IDictionary<UnionValue, UnionValue> InnerMap;

        public UnionValueMap(IDictionary<UnionValue, UnionValue> collection = null)
        {
            InnerMap = collection ?? new Dictionary<UnionValue, UnionValue>();
        }

        public UnionValueMap(SerializationInfo info, StreamingContext context)
        {
            var all = (ICollection) info.GetValue(nameof(InnerMap), typeof(ICollection));
            foreach (KeyValuePair<UnionValue, UnionValue> o in all)
            {
                InnerMap.Add(o.Key, o.Value);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var array = new KeyValuePair<UnionValue, UnionValue>[Count];
            var index = 0;
            foreach (var v in InnerMap)
            {
                array[index++] = new KeyValuePair<UnionValue, UnionValue>(v.Key, v.Value);
            }

            info.AddValue(nameof(InnerMap), array);
        }

        public IEnumerator<KeyValuePair<UnionValue, UnionValue>> GetEnumerator()
        {
            return InnerMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => InnerMap.Count;

        public UnionValue this[UnionValue key]
        {
            get => InnerMap[key];
            set => InnerMap[key] = value;
        }

        public bool TryGetValue(UnionValue key, out UnionValue value)
        {
            return InnerMap.TryGetValue(key, out value);
        }

        public override string ToString()
        {
            return InnerMap.ToString();
        }
    }
}