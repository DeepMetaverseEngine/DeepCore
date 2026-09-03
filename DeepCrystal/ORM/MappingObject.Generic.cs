using DeepCore;
using DeepCore.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.ORM.Generic
{
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public class MappingReference<T> : MappingReference, IMappingNode<T>
        where T : DeepCore.ORM.IObjectMapping, new()
    {
        public MappingReference(string key, ITaskExecutor exe = null, IMappingAdapter db = null) : base(null, key, typeof(T), exe, db) { }
        public MappingReference(string typeName, string key, ITaskExecutor exe = null, IMappingAdapter db = null) : base(typeName, key, typeof(T), exe, db)
        {
        }
        //         public void SetData(T data)
        //         {
        //             base.InternalSetData(data, true);
        //         }
        //[Obsolete("尽量直接使用ORM对象做逻辑操作，避免整个数据拿出，整个数据写入")]
        /// <summary> 自动刷新 </summary>
        new public T Data
        {
            get { return (T)base.Data; }
            set { base.InternalSetData(value, true); }
        }
        //         protected T Source
        //         {
        //             get { return (T)base.Data; }
        //             set { base.InternalSetData(value, true); }
        //         }
        new public Task<T> LoadDataAsync()
        {
            return base.LoadDataAsync().ContinueWith(t => t.GetResultAs<T>());
        }
        public Task<T> LoadOrCreateDataAsync()
        {
            return LoadOrCreateDataAsync(static () => new T());
        }
        public async Task<T> LoadOrCreateDataAsync(Func<T> create)
        {
            var db = this.StartDatabase();
            var entries = await db.ObjectBatchQueryAsync(Key);
            var data = await this.InternalLoadDataAsync(db, entries, true);
            if (data == null)
            {
                data = create();
                var trans = StartTransaction(t =>
                {
                    InternalSaveData(t, data, true);
                });
                await trans.ExecuteAsync();
            }
            return await executor.FromResult((T)data);
        }
        public Task SaveDataAsync(T data)
        {
            return base.SaveDataAsync(data);
        }

        public bool TryGetField<V>(string fieldName, out V value)
        {
            var field = base.InternalGetSubField(fieldName);
            if (field != null)
            {
                value = (V)field.Field.GetValue(base.Data);
                return true;
            }
            value = default(V);
            return false;
        }
        public Task<long> IncrementFieldAsync(string fieldName, long value = 1)
        {
            var field = base.InternalGetSubField(fieldName);
            if (field != null)
            {
                var db = StartDatabase();
                return executor.Execute(db.ObjectHashIncrementFieldAsync(base.Key, fieldName, value).ContinueWith(t =>
                {
                    var fv = t.GetResultAs<long>();
                    var fieldValue = Convert.ChangeType(fv, field.Field.Field.FieldType);
                    if (base.Data != null)
                    {
                        field.Field.SetValue(base.Data, fieldValue);
                    }
                    return fv;
                }));
            }
            return Task.FromResult<long>(0);
        }
        public Task<double> IncrementFieldAsync(string fieldName, double value = 1)
        {
            var field = base.InternalGetSubField(fieldName);
            if (field != null)
            {
                var db = StartDatabase();
                return executor.Execute(db.ObjectHashIncrementFieldAsync(base.Key, fieldName, value).ContinueWith(t =>
                {
                    var fv = t.GetResultAs<double>();
                    var fieldValue = Convert.ChangeType(fv, field.Field.Field.FieldType);
                    if (base.Data != null)
                    {
                        field.Field.SetValue(base.Data, fieldValue);
                    }
                    return fv;
                }));
            }
            return Task.FromResult<double>(0);
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    public class MappingDictionary<K, V, TRef> : MappingDictionary, IHashMap<K, TRef>, IMappingNode<HashMap<K, V>>
        where TRef : IMappingNode<V>
    {
        public MappingDictionary(string key, ITaskExecutor exe, IMappingAdapter db) : base(key, typeof(HashMap<K, V>), typeof(K), typeof(V), exe, db)
        {
        }
        new public Task<HashMap<K, V>> LoadDataAsync()
        {
            return base.LoadDataAsync().ContinueWith(t => this.Source);
        }
        public Task SaveDataAsync(IDictionary<K, V> map)
        {
            this.Source = new HashMap<K, V>(map);
            return base.SaveDataAsync();
        }
        public Task<V> LoadFieldAsync(K key)
        {
            var db = StartDatabase();
            return executor.Execute(base.InternalLoadFieldAsync(db, key, null).ContinueWith(t => t.GetResultAs<V>()));
        }
        public Task SaveFieldAsync(K key, V fieldValue)
        {
            base.MapData[key] = fieldValue;
            return executor.Execute(StartTransaction(db =>
            {
                base.InternalSavePutField(db, key, fieldValue);
            }).ExecuteAsync());
        }
        public Task RemoveFieldAsync(K key)
        {
            var ret = this.Source.Remove(key);
            return executor.Execute(StartTransaction(db =>
            {
                base.InternalSaveRemoveField(db, key);
            }).ExecuteAsync());
        }
        //[Obsolete("尽量直接使用ORM对象做逻辑操作，避免整个数据拿出，整个数据写入")]
        /// <summary> 自动刷新 </summary>
        new public HashMap<K, V> Data
        {
            get { return (HashMap<K, V>)base.MapData; }
            set { base.InternalSetData(value, true); }
        }
        protected HashMap<K, V> Source
        {
            get { return (HashMap<K, V>)base.MapData; }
            set { base.InternalSetData(value, true); }
        }
        public int Count
        {
            get { return base.MapCount; }
        }
        public TRef this[K key]
        {
            get { return (TRef)(base.InternalGetMappingField(key)); }
        }
        public ICollection<K> Keys
        {
            get { return ((IDictionary<K, V>)Source).Keys; }
        }
        public ICollection<TRef> Values
        {
            get
            {
                var ret = new List<TRef>();
                InternalForEachSubFields(e =>
                {
                    ret.Add((TRef)(e.MappingNode));
                });
                return ret;
            }
        }
        public override bool IsReadOnly
        {
            get => false;
        }
        public TRef Get(K key)
        {
            return (TRef)base.InternalGetMappingField(key);
        }
        /// <summary> 自动刷新 </summary>
        public TRef Put(K key, V value)
        {
            return (TRef)base.InternalCacheAndPutField(key, value);
        }
        /// <summary> 自动刷新 </summary>
        public TRef Add(K key, V value)
        {
            return (TRef)base.InternalCacheAndAddField(key, value);
        }
        /// <summary> 自动刷新 </summary>
        public TRef AddOrUpdate(K key, V value)
        {
            var mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return mapping;
            }
            else
            {
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return mapping;
            }
        }
        /// <summary> 自动刷新 </summary>
        public TRef GetOrAdd(K key, Func<K, V> create)
        {
            var mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                var value = create(key);
                mapping = (TRef)InternalCacheAndPutField(key, value);
            }
            return mapping;
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAddOrUpdate(K key, V value, out TRef mapping)
        {
            mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return true;
            }
            else
            {
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return false;
            }
        }
        /// <summary> 自动刷新 </summary>
        public bool TryAdd(K key, V value, out TRef mapping)
        {
            mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool TryAdd(K key, Func<K, V> value, out TRef mapping)
        {
            mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                mapping = (TRef)InternalCacheAndPutField(key, value(key));
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary> 自动刷新 </summary>
        public bool TryGetOrCreate(K key, out TRef mapping, Func<K, V> create)
        {
            mapping = (TRef)InternalGetMappingField(key);
            if (mapping == null)
            {
                var value = create(key);
                mapping = (TRef)InternalCacheAndPutField(key, value);
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary> 自动刷新 </summary>
        public bool Remove(K key)
        {
            return base.InternalCacheAndRemove(key);
        }
        public TRef RemoveByKey(K key)
        {
            TRef ret = (TRef)base.InternalGetMappingField(key);
            if (ret != null)
            {
                base.InternalCacheAndRemove(key);
            }
            return ret;
        }
        public bool TryRemove(K key, out TRef ret)
        {
            ret = (TRef)base.InternalGetMappingField(key);
            if (ret != null)
            {
                base.InternalCacheAndRemove(key);
            }
            return ret != null;
        }
        public bool ContainsKey(K key)
        {
            return Source.ContainsKey(key);
        }
        public bool TryGetValue(K key, out TRef value)
        {
            value = Get(key);
            return value != null;
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            base.InternalCacheAndClear();
        }
        public KeyValuePair<K, TRef>[] ToArray()
        {
            var ret = new KeyValuePair<K, TRef>[Count];
            int i = 0;
            foreach (var e in this)
            {
                ret[i] = e;
                i++;
            }
            return ret;
        }
        public IEnumerator<KeyValuePair<K, TRef>> GetEnumerator()
        {
            var ret = new List<KeyValuePair<K, TRef>>();
            foreach (var e in Source)
            {
                ret.Add(new KeyValuePair<K, TRef>(e.Key, (TRef)InternalGetMappingField(e.Key)));
            }
            return ret.GetEnumerator();
        }
        public void CopyTo(KeyValuePair<K, TRef>[] array, int arrayIndex)
        {
            var ret = ToArray();
            Array.Copy(ret, 0, array, arrayIndex, ret.Length);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        bool IDictionary<K, TRef>.Remove(K key)
        {
            return this.Remove(key);
        }
        bool ICollection<KeyValuePair<K, TRef>>.Remove(KeyValuePair<K, TRef> item)
        {
            return this.Remove(item.Key);
        }
        bool ICollection<KeyValuePair<K, TRef>>.Contains(KeyValuePair<K, TRef> item)
        {
            return this.ContainsKey(item.Key);
        }
        TRef IHashMap<K, TRef>.Get(K key)
        {
            return Get(key);
        }

        #region NotImplemented
        TRef IDictionary<K, TRef>.this[K key] { get => this.Get(key); set => throw new NotImplementedException(); }
        void IDictionary<K, TRef>.Add(K key, TRef value) { throw new NotImplementedException(); }
        void ICollection<KeyValuePair<K, TRef>>.Add(KeyValuePair<K, TRef> item) { throw new NotImplementedException(); }
        bool IHashMap<K, TRef>.TryGetOrCreate(K key, out TRef ret, Func<K, TRef> create) { throw new NotImplementedException(); }
        bool IHashMap<K, TRef>.TryAddOrUpdate(K key, TRef val) { throw new NotImplementedException(); }
        bool IHashMap<K, TRef>.TryAdd(K key, TRef val) { throw new NotImplementedException(); }
        void IHashMap<K, TRef>.Put(K key, TRef val) { throw new NotImplementedException(); }
        void IHashMap<K, TRef>.PutAll(IReadOnlyDictionary<K, TRef> map) { throw new NotImplementedException(); }
        #endregion

    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    public class MappingList<T, TRef> : MappingList, IList<TRef>, IMappingNode<List<T>>
        where TRef : IMappingNode<T>
    {
        protected internal MappingList(string key, ITaskExecutor exe, IMappingAdapter db) : base(key, typeof(List<T>), typeof(T), exe, db)
        {
        }
        new public Task<List<T>> LoadDataAsync()
        {
            return base.LoadDataAsync().ContinueWith(t => this.Source);
        }
        public Task SaveDataAsync(T[] array)
        {
            this.Source = new List<T>(array);
            return base.SaveDataAsync();
        }
        public Task SaveDataAsync(IList<T> list)
        {
            this.Source = new List<T>(list);
            return base.SaveDataAsync();
        }
        public Task<T> LoadFieldAsync(int index)
        {
            var db = StartDatabase();
            return executor.Execute(base.InternalLoadFieldAsync(db, index, null).ContinueWith(t => t.GetResultAs<T>()));
        }
        public Task SaveFieldAsync(int index, T fieldValue)
        {
            base.ListData[index] = fieldValue;
            return executor.Execute(StartTransaction(db =>
            {
                var entry = base.InternalSaveField(db, index);
                db.Enqueue(db.Database.ObjectHashUpdateAsync(base.Key, entry));
            }).ExecuteAsync());
        }
        //[Obsolete("尽量直接使用ORM对象做逻辑操作，避免整个数据拿出，整个数据写入")]
        /// <summary> 自动刷新 </summary>
        new public List<T> Data
        {
            get { return (List<T>)base.ListData; }
            set { base.InternalSetData(value, true); }
        }
        protected List<T> Source
        {
            get { return (List<T>)base.ListData; }
            set { base.InternalSetData(value, true); }
        }
        public int Count
        {
            get { return base.ListCount; }
        }
        public override bool IsReadOnly
        {
            get => false;
        }
        /// <summary> 自动刷新 </summary>
        public TRef this[int index]
        {
            get { return (TRef)(base.InternalGetMappingField(index)); }
            set { throw new NotImplementedException(); }
        }
        public TRef Get(int index)
        {
            return (TRef)base.InternalGetMappingField(index);
        }
        /// <summary> 自动刷新 </summary>
        public TRef SetField(int index, T fieldValue)
        {
            return (TRef)base.InternalCacheAndSetField(index, fieldValue);
        }
        /// <summary> 自动刷新 </summary>
        public TRef Add(T data)
        {
            return (TRef)InternalCacheAndAdd(data);
        }
        /// <summary> 自动刷新 </summary>
        public bool TryRemoveLast(out TRef last)
        {
            last = (TRef)InternalCacheAndRemove(this.Count - 1);
            return last != null;
        }
        /// <summary> 自动刷新 </summary>
        public bool RemoveLast()
        {
            return TryRemoveLast(out TRef last);
        }
        public int IndexOf(T item)
        {
            return Source.IndexOf(item);
        }
        public bool Contains(T item)
        {
            return Source.Contains(item);
        }
        /// <summary> 自动刷新 </summary>
        public void Clear()
        {
            base.InternalCacheAndClear();
        }
        /// <summary> 自动刷新 </summary>
        [Obsolete("性能极差", false)]
        public Task InsertAsync(int index, T data)
        {
            if (this.Data == null)
            {
                this.Data = new List<T>();
            }
            if (index == this.Data.Count)
            {
                base.InternalCacheAndAdd(data);
                return base.FlushAsync();
            }
            else
            {
                this.Data.Insert(index, data);
                return base.SaveDataAsync();
            }
        }
        /// <summary> 自动刷新 </summary>
        [Obsolete("性能极差", false)]
        public Task RemoveAtAsync(int index)
        {
            if (this.Data != null)
            {
                if (index == this.Data.Count - 1)
                {
                    base.InternalCacheAndRemove(index);
                    return base.FlushAsync();
                }
                else
                {
                    this.Data.RemoveAt(index);
                    return base.SaveDataAsync();
                }
            }
            return Task.CompletedTask;
        }
        /// <summary> 自动刷新 </summary>
        [Obsolete("性能极差", false)]
        public Task<bool> RemoveAsync(T item)
        {
            if (this.Data != null)
            {
                var index = Data.IndexOf(item);
                if (index >= 0)
                {
                    return RemoveAtAsync(index).ContinueWith(t => true);
                }
            }
            return Task.FromResult(false);
        }

        public void CopyTo(TRef[] array, int arrayIndex)
        {
            var ret = new List<TRef>(this.Count);
            InternalForEachSubFields(e => ret.Add((TRef)e.MappingNode));
            ret.CopyTo(array, arrayIndex);
        }
        public IEnumerator<TRef> GetEnumerator()
        {
            var ret = new List<TRef>(this.Count);
            InternalForEachSubFields(e => ret.Add((TRef)e.MappingNode));
            return ret.GetEnumerator();
        }

        void IList<TRef>.RemoveAt(int index)
        {
            base.InternalCacheAndRemove(index);
        }
        bool ICollection<TRef>.Remove(TRef item)
        {
            var index = ((IList<TRef>)this).IndexOf(item);
            if (index >= 0)
            {
                ((IList<TRef>)this).RemoveAt(index);
                return true;
            }
            return false;
        }
        int IList<TRef>.IndexOf(TRef item)
        {
            var ret = new List<TRef>(this.Count);
            InternalForEachSubFields(e => ret.Add((TRef)e.MappingNode));
            return ret.IndexOf(item);
        }
        bool ICollection<TRef>.Contains(TRef item)
        {
            var ret = new List<TRef>(this.Count);
            InternalForEachSubFields(e => ret.Add((TRef)e.MappingNode));
            return ret.Contains(item);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region NotImplemented
        void IList<TRef>.Insert(int index, TRef item) { throw new NotImplementedException(); }
        void ICollection<TRef>.Add(TRef item) { throw new NotImplementedException(); }
        #endregion
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
#if ARRAY_MAPPING
    
    public class MappingArray<T, TRef> : MappingArray, IReadOnlyList<TRef>       
        where TRef : MappingObject
    {
        public MappingArray(string key, ITaskExecutor exe, IMappingAdapter db) : base(key, typeof(T[]), exe, db)
        {
        }
        new public Task<T[]> LoadDataAsync()
        {
            var db = StartDatabase();
            return executor.Execute(base.InternalLoadDataAsync(db, true).ContinueWith(t => (T[])base.ArrayData));
        }
        public Task SaveDataAsync(T[] array)
        {
            return executor.Execute(StartTransaction(db =>
            {
                base.InternalSaveData(db, array, true);
            }).ExecuteAsync());
        }
        public Task<T> LoadFieldAsync(int index)
        {
            var db = StartDatabase();
            return executor.Execute(base.InternalLoadFieldAsync(db, index).ContinueWith(t => t.GetResultAs<T>()));
        }
        public Task SaveFieldAsync(int index, T fieldValue)
        {
            base.ListData[index] = fieldValue;
            return executor.Execute(StartTransaction(db =>
            {
                var entry = base.InternalSaveField(db, index);
                db.Enqueue(db.Database.ObjectHashUpdateAnsync(base.Key, entry));
            }).ExecuteAsync());
        }
        [Obsolete("尽量直接使用ORM对象做逻辑操作，避免整个数据拿出，整个数据写入")]
        new public T[] Data
        {
            get { return (T[])base.ArrayData; }
            set { base.InternalSetData(value, true); }
        }
        protected T[] Source
        {
            get { return this.Data; }
            set { this.Data = value; }
        }
        public int Count
        {
            get { return base.ListCount; }
        }
        public TRef this[int index]
        {
            get { return (TRef)(base.InternalGetMappingField(index)); }
        }
        public TRef GetMappingField(int index)
        {
            return (TRef)base.InternalGetMappingField(index);
        }
        public T GetValue(int index)
        {
            return (T)base.ListData[index];
        }
        public TRef SetValue(int index, T fieldValue)
        {
            return (TRef)base.InternalCacheAndSetField(index, fieldValue);
        }
        public IEnumerator<TRef> GetEnumerator()
        {
            var ret = new List<TRef>(this.Count);
            InternalForEachSubMapping(e => ret.Add((TRef)e));
            return ret.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

#endif
    //----------------------------------------------------------------------------------------------------------------------------------------------------------



}
