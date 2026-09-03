using DeepCore;
using DeepCore.ORM;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{

    //-----------------------------------------------------------------------------------------------------------------------------

    public abstract class IWrapper : Disposable, IMappingNode
    {
        /// <summary>
        /// 用户临时数据
        /// </summary>
        public int UserTag { get; set; }
        /// <summary>
        /// 用户临时数据
        /// </summary>
        public object UserObject { get; set; }
        //private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("ORM:IWrapper");
        private IMappingNode _owner;
        protected bool enableEventChanged { get; private set; } = true;
        private Action<IWrapper> event_OnDataChanged;
        protected IWrapper()
        {
            AsSynchronizedDisposing();
            //Alloc.RecordConstructor(GetType());
        }
        protected override void Disposing()
        {
            event_OnDataChanged = null;
            _owner = null;
        }
        //         ~IWrapper()
        //         {
        //             Alloc.RecordDispose(GetType());
        //             Alloc.RecordDestructor(GetType());
        //         }
        public bool HasValue { get => SourceObject != null; }
        /// <summary> 自动刷新 </summary>
        protected abstract object SourceObject { get; set; }
        public object Data { get => SourceObject; set => SourceObject = value; }
        public IMappingNode Parent { get => _owner; }
        public event Action<IWrapper> OnDataChanged
        {
            add { event_OnDataChanged += value; }
            remove { event_OnDataChanged -= value; }
        }
        public void RunWithNoEvent(Action action)
        {
            enableEventChanged = false;
            try
            {
                action();
            }
            finally
            {
                enableEventChanged = true;
            }
        }
        /// <summary> 标记刷新整个数据 </summary>
        public void FireDirty()
        {
            event_OnDataChanged?.Invoke(this);
        }
        //------------------------------------------------------------------------------------------
        #region Internal
        internal void setParent(IMappingNode parent)
        {
            if (parent is IWrapper p)
            {
                p.attachChild(this);
            }
            else
            {
                this._owner = parent;
            }
        }
        internal void invokeDataChange()
        {
            if (enableEventChanged) event_OnDataChanged?.Invoke(this);
        }
        internal void attachChild(IWrapper sub)
        {
            if (sub._owner == null)
            {
                sub._owner = this;
                sub.event_OnDataChanged += onChildDataChanged;
            }
            else if (sub._owner != this)
            {
                throw new Exception();
            }
        }
        internal void detachChild(IWrapper sub)
        {
            if (sub._owner == this)
            {
                sub.event_OnDataChanged -= onChildDataChanged;
                sub._owner = null;
            }
        }
        internal void replaceChild(IWrapper old, IWrapper value)
        {
            if (!object.ReferenceEquals(old, value))
            {
                detachChild(old);
                attachChild(value);
                invokeDataChange();
            }
        }
        internal void attachChilds(IEnumerable<IWrapper> sub)
        {
            foreach (var s in sub) { attachChild(s); }
        }
        internal void detachChilds(IEnumerable<IWrapper> sub)
        {
            foreach (var s in sub) { detachChild(s); }
        }
        private void onChildDataChanged(IWrapper sub)
        {
            if (enableEventChanged && sub.Parent == this)
            {
                event_OnDataChanged?.Invoke(sub);
                event_OnDataChanged?.Invoke(this);
            }
        }
        #endregion
        //------------------------------------------------------------------------------------------
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class WrapperStruct : IWrapper
    {
        private readonly HashMap<string, FieldWrapper> subWrappers;
        private Type srcType;
        private object src;
        public WrapperStruct(Type srcType)
        {
            this.subWrappers = new HashMap<string, FieldWrapper>();
            this.TrySetDataType(srcType);
        }

        public WrapperStruct(object src) : this(src.GetType())
        {
            this.src = src;
            this.SetSubWrapperFields(src);
        }
        protected override void Disposing()
        {
            base.Disposing(); subWrappers.Clear();
        }
        public Type SourceType
        {
            get { return srcType; }
        }
        /// <summary> 自动刷新 </summary>
        protected override object SourceObject
        {
            get => src;
            set
            {
                if (!object.ReferenceEquals(src, value))
                {
                    this.src = value;
                    this.RunWithNoEvent(() =>
                    {
                        this.SetSubWrapperFields(value);
                    });
                    this.invokeDataChange();
                }
            }
        }
        private bool TrySetDataType(Type type)
        {
            if (type == null)
                return false;
            if (type != srcType)
            {
                this.srcType = type;
                var dataFields = DynamicMethodTypeFactory.Instance.GetTypeInfo(type);
                this.subWrappers.Values.WritableForEach((wrapper) =>
                {
                    if (dataFields.GetField(wrapper.field.Name) == null)
                    {
                        subWrappers.Remove(wrapper.field.Name);
                        wrapper.Dispose();
                        this.detachChild(wrapper.wrapper);
                    }
                });
                foreach (var field in dataFields.GetFields())
                {
                    if (MappingConverter.Instance.IsWrapperType(field.Field.FieldType) && !subWrappers.ContainsKey(field.Name))
                    {
                        var subWrapper = CreateSubWrapper(field);
                        if (subWrapper != null)
                        {
                            subWrappers.Add(field.Name, new FieldWrapper(this, field, subWrapper));
                        }
                        else
                        {
                            throw new Exception($"Can't Create Sub Wrapper : '{field.Name}' : {type.FullName}");
                        }
                    }
                }
                this.OnDataTypeChanged(type);
                return true;
            }
            return false;
        }
        private void SetSubWrapperFields(object data)
        {
            if (data != null)
            {
                this.TrySetDataType(data.GetType());
                foreach (var sub in this.subWrappers.Values)
                {
                    var sub_value = sub.field.GetValue(data);
                    sub.wrapper.Data = sub_value;
                }
            }
            else
            {
                foreach (var sub in this.subWrappers.Values)
                {
                    sub.wrapper.Data = null;
                }
            }
        }
        protected virtual void OnDataTypeChanged(Type type) { }
        protected virtual IWrapper CreateSubWrapper(IDynamicFieldInfo field)
        {
            return MappingConverter.Instance.CreateWrapper(field.Field.FieldType, this);
        }
        public IWrapper GetWrapperField(string fieldName)
        {
            if (subWrappers.TryGetValue(fieldName, out var ret))
            {
                return ret.wrapper;
            }
            return null;
        }
        private class FieldWrapper
        {
            public readonly WrapperStruct owner;
            public readonly IDynamicFieldInfo field;
            public readonly IWrapper wrapper;
            public FieldWrapper(WrapperStruct owner, IDynamicFieldInfo field, IWrapper wrapper)
            {
                this.owner = owner;
                this.field = field;
                this.wrapper = wrapper;
                wrapper.OnDataChanged += Sub_OnDataChanged;
            }
            private void Sub_OnDataChanged(IWrapper obj)
            {
                if (owner.src != null)
                {
                    if (obj == wrapper)
                    {
                        field.SetValue(owner.src, obj.Data);
                    }
                }
            }
            public void Dispose()
            {
                wrapper.OnDataChanged -= Sub_OnDataChanged;
            }
        }
    }

    public class WrapperStruct<T> : WrapperStruct , IMappingNode<T> where T : IStructMapping
    {
        public WrapperStruct() : base(typeof(T))
        {
            if (!base.SourceType.IsAbstract)
            {
                base.SourceObject = DeepActivator.CreateInstance(typeof(T));
            }
        }
        public WrapperStruct(T data) : base(data)
        {
        }
        new public T Data
        {
            get => (T)base.SourceObject;
            set { base.SourceObject = value; }
        }
        public static implicit operator WrapperStruct<T>(in T src)
        {
            return new WrapperStruct<T>(src);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class WrapperList<T, TRef> : IWrapper, IArrayList<TRef>, IList, IMappingNode<ArrayList<T>> where TRef : IWrapper
    {
        private readonly ArrayList<TRef> mapping;
        private ArrayList<T> src;

        public WrapperList()
        {
            this.mapping = new ArrayList<TRef>();
            this.src = new ArrayList<T>();
        }
        protected override void Disposing()
        {
            base.Disposing(); mapping.Clear();
        }

        /// <summary> 自动刷新 </summary>
        protected override object SourceObject
        {
            get { return src; }
            set
            {
                if (!object.ReferenceEquals(src, value))
                {
                    if (value == null)
                    {
                        this.src.Clear();
                    }
                    else if (value is ArrayList<T> list)
                    {
                        this.src = list;
                    }
                    else if (value.GetType().IsInterfaceOf(typeof(IList)))
                    {
                        this.src.Clear();
                        var slist = value as IList;
                        foreach (var v in slist) { this.src.Add((T)v); }
                    }
                    this.RunWithNoEvent(() =>
                    {
                        this.mapping.Clear();
                        foreach (var e in src)
                        {
                            var item = (TRef)MappingConverter.Instance.CreateWrapper(typeof(T), this, e);
                            this.attachChild(item);
                            this.mapping.Add(item);
                        }
                    });
                    this.invokeDataChange();
                }
            }
        }
        new public ArrayList<T> Data
        {
            get => this.src;
            set { this.SourceObject = value; }
        }

        #region Write
        /// <summary> 自动刷新 </summary>
        public TRef this[int index]
        {
            get => mapping[index];
            set
            {
                base.detachChild(mapping[index]);
                base.attachChild(value);
                mapping[index] = value;
                src[index] = (T)value.Data;
                this.invokeDataChange();
            }
        }
        /// <summary> 自动刷新 </summary>
        object IList.this[int index]
        {
            get => ((IList)mapping)[index];
            set
            {
                if (value is TRef tref)
                {
                    base.detachChild(mapping[index]);
                    base.attachChild(tref);
                    ((IList)mapping)[index] = tref;
                    src[index] = (T)tref.Data;
                    this.invokeDataChange();
                }
            }
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            base.detachChilds(mapping);
            this.mapping.Clear();
            this.src.Clear();
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void AddRange(IEnumerable<TRef> list)
        {
            base.attachChilds(list);
            int startIndex = this.mapping.Count;
            this.mapping.AddRange(list);
            for (int i = startIndex; i < mapping.Count; i++)
            {
                this.src.Add((T)this.mapping[i].Data);
            }
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Add(TRef item)
        {
            base.attachChild(item);
            this.mapping.Add(item);
            this.src.Add((T)item.Data);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        int IList.Add(object value)
        {
            if (value is TRef tref)
            {
                base.attachChild(tref);
                var ret = ((IList)mapping).Add(tref);
                this.src.Add((T)tref.Data);
                this.invokeDataChange();
                return ret;
            }
            return -1;
        }
        /// <summary> 自动刷新 </summary>
        public void Insert(int index, TRef item)
        {
            base.attachChild(item);
            this.mapping.Insert(index, item);
            this.src.Insert(index, (T)item.Data);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(TRef item)
        {
            var index = mapping.IndexOf(item);
            if (index >= 0)
            {
                mapping.RemoveAt(index);
                src.RemoveAt(index);
                base.detachChild(item);
                this.invokeDataChange();
                return true;
            }
            return false;
        }
        /// <summary> 自动刷新 </summary>
        public void RemoveAt(int index)
        {
            base.detachChild(mapping[index]);
            mapping.RemoveAt(index);
            src.RemoveAt(index);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        void IList.Insert(int index, object value) { this.Insert(index, (TRef)value); }
        /// <summary> 自动刷新 </summary>
        void IList.Remove(object value) { this.Remove((TRef)value); }
        #endregion

        #region Read
        public int Count => mapping.Count;
        public bool IsReadOnly => false;
        public bool Contains(TRef item) { return mapping.Contains(item); }
        public void CopyTo(TRef[] array, int arrayIndex) { mapping.CopyTo(array, arrayIndex); }
        public IEnumerator<TRef> GetEnumerator() { return mapping.GetEnumerator(); }
        public int IndexOf(TRef item) { return mapping.IndexOf(item); }
        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable)mapping).GetEnumerator(); }
        bool ICollection.IsSynchronized => ((IList)mapping).IsSynchronized;
        object ICollection.SyncRoot => ((IList)mapping).SyncRoot;
        void ICollection.CopyTo(Array array, int index) { ((IList)mapping).CopyTo(array, index); }
        bool IList.IsFixedSize => ((IList)mapping).IsFixedSize;
        bool IList.Contains(object value) { return ((IList)mapping).Contains(value); }
        int IList.IndexOf(object value) { return ((IList)mapping).IndexOf(value); }
        #endregion
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class WrapperHashMap<K, V, TRef> : IWrapper, IHashMap<K, TRef>, IDictionary, IMappingNode<HashMap<K, V>> where TRef : IWrapper
    {
        private readonly HashMap<K, TRef> mapping;
        private HashMap<K, V> src;

        public WrapperHashMap()
        {
            this.mapping = new HashMap<K, TRef>();
            this.src = new HashMap<K, V>();
        }
        protected override void Disposing()
        {
            base.Disposing(); mapping.Clear();
        }
        /// <summary> 自动刷新 </summary>
        protected override object SourceObject
        {
            get { return this.src; }
            set
            {
                if (!object.ReferenceEquals(src, value))
                {
                    if (value == null)
                    {
                        this.src.Clear();
                    }
                    else if (value is HashMap<K, V> map)
                    {
                        this.src = map;
                    }
                    else if (value.GetType().IsInterfaceOf(typeof(IDictionary)))
                    {
                        this.src.Clear();
                        var smap = value as IDictionary;
                        smap.ForEachDictionary((e) =>
                        {
                            src.Add((K)e.Key, (V)e.Value);
                        });
                    }
                    this.RunWithNoEvent(() =>
                    {
                        mapping.Clear();
                        src.ForEachDictionary(e =>
                        {
                            var item = (TRef)MappingConverter.Instance.CreateWrapper(typeof(V), this, e.Value);
                            this.attachChild(item);
                            mapping.Add((K)e.Key, item);
                        });
                    });
                    this.invokeDataChange();
                }
            }
        }
        new public HashMap<K, V> Data
        {
            get => this.src;
            set { this.SourceObject = value; }
        }



        #region Write
        /// <summary> 自动刷新 </summary>
        public TRef this[K key]
        {
            get => mapping[key];
            set
            {
                base.detachChild(mapping.Get(key));
                base.attachChild(value);
                mapping[key] = value;
                src[key] = (V)value.Data;
                this.invokeDataChange();
            }
        }
        /// <summary> 自动刷新 </summary>
        object IDictionary.this[object key]
        {
            get => ((IDictionary)mapping)[key];
            set
            {
                if (value is TRef tref)
                {
                    base.detachChild(mapping.Get((K)key));
                    base.attachChild(tref);
                    ((IDictionary)mapping)[key] = tref;
                    src[(K)key] = (V)tref.Data;
                    this.invokeDataChange();
                }
            }
        }
        /// <summary> 自动刷新 </summary>
        public void Add(K key, TRef value)
        {
            base.attachChild(value);
            mapping.Add(key, value);
            src.Add(key, (V)value.Data);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            base.detachChilds(mapping.Values);
            this.mapping.Clear();
            this.src.Clear();
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Put(K key, TRef val)
        {
            base.detachChild(mapping.Get(key));
            base.attachChild(val);
            mapping.Put(key, val);
            src.Put(key, (V)val.Data);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void PutAll(IReadOnlyDictionary<K, TRef> map)
        {
            base.attachChilds(map.Values);
            foreach (var e in map)
            {
                mapping.Put(e.Key, e.Value);
                src.Put(e.Key, (V)e.Value.Data);
            }
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public TRef RemoveByKey(K key)
        {
            var ret = mapping.RemoveByKey(key);
            src.Remove(key);
            base.detachChild(ret);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAdd(K key, TRef val)
        {
            var ret = mapping.TryAdd(key, val);
            if (ret)
            {
                this.src.TryAdd(key, (V)val.Data);
                base.attachChild(val);
                this.invokeDataChange();
            }
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAddOrUpdate(K key, TRef val)
        {
            base.attachChild(val);
            var ret = mapping.TryAddOrUpdate(key, val);
            this.src.TryAddOrUpdate(key, (V)val.Data);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryGetOrCreate(K key, out TRef value, Func<K, TRef> create)
        {
            var ret = mapping.TryGetOrCreate(key, out value, create);
            if (!ret)
            {
                this.src.TryAdd(key, (V)value.Data);
                base.attachChild((TRef)value);
                this.invokeDataChange();
            }
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(K key) { return this.RemoveByKey(key) != null; }
        /// <summary> 自动刷新 </summary>
        public bool Remove(KeyValuePair<K, TRef> item) { return this.Remove(item.Key); }
        /// <summary> 自动刷新 </summary>
        public void Add(KeyValuePair<K, TRef> item) { this.Add(item.Key, item.Value); }
        /// <summary> 自动刷新 </summary>
        void IDictionary.Remove(object key) { this.Remove((K)key); }
        /// <summary> 自动刷新 </summary>
        void IDictionary.Add(object key, object value) { this.Add((K)key, (TRef)value); }
        #endregion

        #region Read
        public ICollection<K> Keys => mapping.Keys;
        public ICollection<TRef> Values => mapping.Values;
        public int Count => mapping.Count;
        public bool IsReadOnly => false;
        public bool Contains(KeyValuePair<K, TRef> item) { return ((IHashMap<K, TRef>)mapping).Contains(item); }
        public bool ContainsKey(K key) { return mapping.ContainsKey(key); }
        public void CopyTo(KeyValuePair<K, TRef>[] array, int arrayIndex) { ((IHashMap<K, TRef>)mapping).CopyTo(array, arrayIndex); }
        public TRef Get(K key) { return mapping.Get(key); }
        public bool TryGetValue(K key, out TRef value) { return mapping.TryGetValue(key, out value); }
        public IEnumerator<KeyValuePair<K, TRef>> GetEnumerator() { return mapping.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable)mapping).GetEnumerator(); }
        bool ICollection.IsSynchronized => ((IDictionary)mapping).IsSynchronized;
        object ICollection.SyncRoot => ((IDictionary)mapping).SyncRoot;
        void ICollection.CopyTo(Array array, int index) { ((IDictionary)mapping).CopyTo(array, index); }
        bool IDictionary.IsFixedSize => ((IDictionary)mapping).IsFixedSize;
        ICollection IDictionary.Keys => ((IDictionary)mapping).Keys;
        ICollection IDictionary.Values => ((IDictionary)mapping).Values;
        bool IDictionary.Contains(object key) { return ((IDictionary)mapping).Contains(key); }
        IDictionaryEnumerator IDictionary.GetEnumerator() { return ((IDictionary)mapping).GetEnumerator(); }
        #endregion

    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class SimpleWrapperList<T> : IWrapper, IArrayList<T>, IList, IMappingNode<ArrayList<T>>
    {
        private ArrayList<T> src;

        public SimpleWrapperList()
        {
            this.src = new ArrayList<T>();
        }
        protected override void Disposing()
        {
            base.Disposing(); src.Clear();
        }

        /// <summary> 自动刷新 </summary>
        protected override object SourceObject
        {
            get => src;
            set
            {
                if (!object.ReferenceEquals(src, value))
                {
                    if (value == null)
                    {
                        this.src.Clear();
                    }
                    else if (value is ArrayList<T> list)
                    {
                        this.src = list;
                    }
                    else if (value.GetType().IsInterfaceOf(typeof(IList)))
                    {
                        var slist = value as IList;
                        this.src = new ArrayList<T>(slist.Count);
                        foreach (var v in slist) { this.src.Add((T)v); }
                    }
                    this.invokeDataChange();
                }
            }
        }
        new public ArrayList<T> Data
        {
            get => src;
            set { this.SourceObject = value; }
        }
        /// <summary> 自动刷新 </summary>
        public T this[int index]
        {
            get => src[index];
            set
            {
                src[index] = value;
                this.invokeDataChange();
            }
        }
        /// <summary> 自动刷新 </summary>
        object IList.this[int index]
        {
            get => ((IList)src)[index];
            set
            {
                ((IList)src)[index] = value;
                this.invokeDataChange();
            }
        }

        #region Write
        /// <summary> 自动刷新 </summary>
        public void Add(T item)
        {
            src.Add(item);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public int Add(object value)
        {
            var ret = ((IList)src).Add(value);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            ((IArrayList<T>)src).AddRange(collection);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            src.Clear();
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Insert(int index, T item)
        {
            src.Insert(index, item);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Insert(int index, object value)
        {
            ((IList)src).Insert(index, value);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(T item)
        {
            var ret = src.Remove(item);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public void Remove(object value)
        {
            ((IList)src).Remove(value);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void RemoveAt(int index)
        {
            src.RemoveAt(index);
            this.invokeDataChange();
        }
        public void Sort()
        {
            src.Sort();
            this.invokeDataChange();
        }
        public void Sort(Comparison<T> comparison)
        {
            src.Sort(comparison);
            this.invokeDataChange();
        }
        public void Sort(IComparer<T> comparison)
        {
            src.Sort(comparison);
            this.invokeDataChange();
        }
        #endregion

        #region Read
        public int Count => src.Count;
        public bool IsReadOnly => ((IArrayList<T>)src).IsReadOnly;
        public bool IsFixedSize => ((IList)src).IsFixedSize;
        public bool IsSynchronized => ((IList)src).IsSynchronized;
        public object SyncRoot => ((IList)src).SyncRoot;
        public bool Contains(T item) { return src.Contains(item); }
        public bool Contains(object value) { return ((IList)src).Contains(value); }
        public void CopyTo(T[] array, int arrayIndex) { src.CopyTo(array, arrayIndex); }
        public void CopyTo(Array array, int index) { ((IList)src).CopyTo(array, index); }
        public IEnumerator<T> GetEnumerator() { return ((IArrayList<T>)src).GetEnumerator(); }
        public int IndexOf(T item) { return src.IndexOf(item); }
        public int IndexOf(object value) { return ((IList)src).IndexOf(value); }
        IEnumerator IEnumerable.GetEnumerator() { return ((IArrayList<T>)src).GetEnumerator(); }
        #endregion
    }

    //-----------------------------------------------------------------------------------------------------------------------------

    public class SimpleWrapperHashMap<K, V> : IWrapper, IHashMap<K, V>, IDictionary, IMappingNode<HashMap<K, V>>
    {
        private HashMap<K, V> src;

        public SimpleWrapperHashMap()
        {
            this.src = new HashMap<K, V>();
        }
        protected override void Disposing()
        {
            base.Disposing(); src.Clear();
        }
        /// <summary> 自动刷新 </summary>
        protected override object SourceObject
        {
            get => src;
            set
            {
                if (!object.ReferenceEquals(src, value))
                {
                    if (value == null)
                    {
                        this.src.Clear();
                    }
                    else if (value is HashMap<K, V> map)
                    {
                        this.src = map;
                    }
                    else if (value.GetType().IsInterfaceOf(typeof(IDictionary)))
                    {
                        var smap = value as IDictionary;
                        this.src = new HashMap<K, V>(smap.Count);
                        smap.ForEachDictionary((e) =>
                        {
                            src.Add((K)e.Key, (V)e.Value);
                        });
                    }
                    this.invokeDataChange();
                }
            }
        }
        new public HashMap<K, V> Data
        {
            get => src;
            set { this.SourceObject = value; }
        }
        /// <summary> 自动刷新 </summary>
        public V this[K key]
        {
            get => ((IHashMap<K, V>)src)[key];
            set
            {
                src[key] = value;
                this.invokeDataChange();
            }
        }
        /// <summary> 自动刷新 </summary>
        public object this[object key]
        {
            get => ((IDictionary)src)[key];
            set
            {
                src[(K)key] = (V)value;
                this.invokeDataChange();
            }
        }

        #region Write
        /// <summary> 自动刷新 </summary>
        public void Add(K key, V value)
        {
            ((IHashMap<K, V>)src).Add(key, value);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Add(KeyValuePair<K, V> item)
        {
            ((IHashMap<K, V>)src).Add(item);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Add(object key, object value)
        {
            ((IDictionary)src).Add(key, value);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            ((IHashMap<K, V>)src).Clear();
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void Put(K key, V val)
        {
            ((IHashMap<K, V>)src).Put(key, val); this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public void PutAll(IReadOnlyDictionary<K, V> map)
        {
            ((IHashMap<K, V>)src).PutAll(map);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(K key)
        {
            var ret = ((IHashMap<K, V>)src).Remove(key);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(KeyValuePair<K, V> item)
        {
            var ret = ((IHashMap<K, V>)src).Remove(item);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public void Remove(object key)
        {
            ((IDictionary)src).Remove(key);
            this.invokeDataChange();
        }
        /// <summary> 自动刷新 </summary>
        public V RemoveByKey(K key)
        {
            var ret = ((IHashMap<K, V>)src).RemoveByKey(key);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAdd(K key, V val)
        {
            var ret = ((IHashMap<K, V>)src).TryAdd(key, val);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAddOrUpdate(K key, V val)
        {
            var ret = ((IHashMap<K, V>)src).TryAddOrUpdate(key, val);
            this.invokeDataChange();
            return ret;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryGetOrCreate(K key, out V value, Func<K, V> create)
        {
            var ret = ((IHashMap<K, V>)src).TryGetOrCreate(key, out value, create);
            if (ret) this.invokeDataChange();
            return ret;
        }
        public V GetOrAdd(K key, Func<K, V> create)
        {
            var ret = ((IHashMap<K, V>)src).TryGetOrCreate(key, out var value, create);
            if (ret) this.invokeDataChange();
            return value;
        }
        #endregion

        #region Read
        public ICollection<K> Keys => ((IHashMap<K, V>)src).Keys;
        public ICollection<V> Values => ((IHashMap<K, V>)src).Values;
        public int Count => ((IHashMap<K, V>)src).Count;
        public bool IsReadOnly => ((IHashMap<K, V>)src).IsReadOnly;
        public bool IsFixedSize => ((IDictionary)src).IsFixedSize;
        public bool IsSynchronized => ((IDictionary)src).IsSynchronized;
        public object SyncRoot => ((IDictionary)src).SyncRoot;
        ICollection IDictionary.Keys => ((IDictionary)src).Keys;
        ICollection IDictionary.Values => ((IDictionary)src).Values;
        public bool Contains(KeyValuePair<K, V> item) { return ((IHashMap<K, V>)src).Contains(item); }
        public bool Contains(object key) { return ((IDictionary)src).Contains(key); }
        public bool ContainsKey(K key) { return ((IHashMap<K, V>)src).ContainsKey(key); }
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) { ((IHashMap<K, V>)src).CopyTo(array, arrayIndex); }
        public void CopyTo(Array array, int index) { ((IDictionary)src).CopyTo(array, index); }
        public V Get(K key) { return ((IHashMap<K, V>)src).Get(key); }
        public bool TryGetValue(K key, out V value) { return ((IHashMap<K, V>)src).TryGetValue(key, out value); }
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() { return ((IHashMap<K, V>)src).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return ((IHashMap<K, V>)src).GetEnumerator(); }
        IDictionaryEnumerator IDictionary.GetEnumerator() { return ((IDictionary)src).GetEnumerator(); }
        #endregion

    }

    //-----------------------------------------------------------------------------------------------------------------------------

}
