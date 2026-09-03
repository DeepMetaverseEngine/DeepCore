using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DeepCore.IO
{
    public struct BinaryMessage
    {
        public static readonly BinaryMessage NULL = new BinaryMessage(0, new ArraySegment<byte>());
        private int route;
        private Type routeType;
        private ArraySegment<byte> buffer;
        public int Route { get => route; }
        public Type RouteType { get => routeType; }
        public byte[] Buffer { get => buffer.Array; }
        public int BufferOffset { get => buffer.Offset; }
        public int BufferLength { get => buffer.Count; }
        public ArraySegment<byte> DataSegment { get => buffer; }
        public bool IsNoRoute { get => Route == 0; }
        public bool HasRoute { get => Route != 0; }
        public bool IsNull { get => buffer.Array == null; }
        public bool HasData { get => buffer.Array != null; }
        private BinaryMessage(int route, ArraySegment<byte> data)
        {
            this.route = route;
            this.buffer = data;
        }
        public override string ToString()
        {
            return string.Format("Route={0}, RouteType={1}", Route, RouteType);
        }
        public byte[] ToArray()
        {
            if (buffer.Array == null)
            {
                return null;
            }
            if (buffer.Offset == 0 && buffer.Count == buffer.Array.Length)
            {
                return buffer.Array;
            }
            else
            {
                var ret = new byte[BufferLength];
                System.Buffer.BlockCopy(buffer.Array, buffer.Offset, ret, 0, buffer.Count);
                return ret;
            }
        }
        public void ReadExternal(IInputStream input)
        {
            this.route = input.GetS32();
            this.buffer = new ArraySegment<byte>(input.GetBytes());
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(this.route);
            output.PutBytes(this.buffer.Array, this.buffer.Offset, buffer.Count);
        }

        public static BinaryMessage FromRoute(int route)
        {
            return new BinaryMessage(route, new ArraySegment<byte>());
        }
        public static BinaryMessage FromSegment(int route, ArraySegment<byte> data)
        {
            return new BinaryMessage(route, data);
        }
        public static BinaryMessage FromBuffer(int route, Type routeType, MemoryStream buffer)
        {
            var ret = new BinaryMessage();
            ret.route = route;
            ret.routeType = routeType;
            ret.buffer = buffer.ToArraySegment(true);
            return ret;
        }
        public static BinaryMessage FromBuffer(int route, Type routeType, MemoryStream buffer, int offset, int length)
        {
            var ret = new BinaryMessage();
            ret.route = route;
            ret.routeType = routeType;
            ret.buffer = buffer.ToArraySegment(offset, length, true);
            return ret;
        }
        public static BinaryMessage CopyFrom(int route, Type routeType, ArraySegment<byte> data)
        {
            var ret = new BinaryMessage();
            ret.route = route;
            ret.routeType = routeType;
            if (data.Array != null)
            {
                ret.buffer = new ArraySegment<byte>(new byte[data.Count]);
                System.Buffer.BlockCopy(data.Array, data.Offset, ret.buffer.Array, 0, data.Count);
            }
            return ret;
        }

    }

    /// <summary>
    /// 自动反射字段序列化
    /// </summary>
    public abstract class AutoExternalizable : IExternalizable
    {
        public virtual void WriteExternal(IOutputStream output)
        {
            var fields = GetType().GetFields();
            foreach (var f in fields)
            {
                if (f.IsStatic == false && f.IsPublic)
                {
                    var fd = f.GetValue(this);
                    if (fd != null)
                    {
                        output.PutUTF(f.Name);
                        output.PutRawData(f.FieldType, fd);
                    }
                }
            }
            output.PutUTF(".");
        }
        public virtual void ReadExternal(IInputStream input)
        {
            do
            {
                var fname = input.GetUTF();
                if (fname == ".")
                {
                    break;
                }
                var f = GetType().GetField(fname);
                var fd = input.GetRawData(f.FieldType, out var dt);
                if (dt == DataType.NA || fd == null)
                {
                    throw new Exception(string.Format("Can not read field '{0}' in '{1}'", fname, this.GetType().FullName));
                }
                f.SetValue(this, fd);
            }
            while (true);

        }
    }

    /// <summary>
    /// 包装用于嵌套的可序列化Map
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public abstract class SerializableDictionary<K, V> : IExternalizable, IDictionary<K, V>
    {
        public HashMap<K, V> map;

        public SerializableDictionary(HashMap<K, V> src)
        {
            this.map = src;
        }
        public SerializableDictionary()
        {
            this.map = new HashMap<K, V>();
        }
        public abstract void WriteExternal(IOutputStream output);
        public abstract void ReadExternal(IInputStream input);

        #region Wrapper

        public V this[K key]
        {
            get { return map[key]; }
            set { map[key] = value; }
        }
        public int Count { get { return map.Count; } }
        public bool IsReadOnly { get { return ((IDictionary<K, V>)map).IsReadOnly; } }
        public ICollection<K> Keys { get { return map.Keys; } }
        public ICollection<V> Values { get { return map.Values; } }
        public void Add(K key, V value)
        {
            map.Add(key, value);
        }
        public void Clear()
        {
            map.Clear();
        }
        public bool ContainsKey(K key)
        {
            return map.ContainsKey(key);
        }
        public bool ContainsValue(V value)
        {
            return map.ContainsValue(value);
        }
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return map.GetEnumerator();
        }
        public bool Remove(K key)
        {
            return map.Remove(key);
        }
        public bool TryGetValue(K key, out V value)
        {
            return map.TryGetValue(key, out value);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.GetEnumerator();
        }
        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
        {
            map.Add(item.Key, item.Value);
        }
        bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
        {
            return map.ContainsKey(item.Key);
        }
        bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
        {
            return map.Remove(item.Key);
        }
        void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            ((IDictionary<K, V>)map).CopyTo(array, arrayIndex);
        }
        #endregion
    }

    /// <summary>
    /// 包装用于嵌套的可序列化List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SerializableList<T> : IExternalizable, IList<T>
    {
        public ArrayList<T> list;

        public SerializableList(ArrayList<T> src)
        {
            this.list = src;
        }
        public SerializableList()
        {
            this.list = new ArrayList<T>();
        }
        public abstract void WriteExternal(IOutputStream output);
        public abstract void ReadExternal(IInputStream input);

        #region Wrapper

        public int Count { get { return ((IList<T>)list).Count; } }
        public bool IsReadOnly { get { return ((IList<T>)list).IsReadOnly; } }
        public T this[int index]
        {
            get { return ((IList<T>)list)[index]; }
            set { ((IList<T>)list)[index] = value; }
        }
        public int IndexOf(T item)
        {
            return ((IList<T>)list).IndexOf(item);
        }
        public void Insert(int index, T item)
        {
            ((IList<T>)list).Insert(index, item);
        }
        public void RemoveAt(int index)
        {
            ((IList<T>)list).RemoveAt(index);
        }
        public void Add(T item)
        {
            ((IList<T>)list).Add(item);
        }
        public void Clear()
        {
            ((IList<T>)list).Clear();
        }
        public bool Contains(T item)
        {
            return ((IList<T>)list).Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            ((IList<T>)list).CopyTo(array, arrayIndex);
        }
        public bool Remove(T item)
        {
            return ((IList<T>)list).Remove(item);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }
        #endregion
    }
}

