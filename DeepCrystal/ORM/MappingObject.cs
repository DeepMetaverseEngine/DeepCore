using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.ORM;
using DeepCore.Reflection;
using DeepCore.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{
    [Reflectible]
    public interface IMappingNode : IDisposable
    {
        IMappingNode Parent { get; }
        bool HasValue { get; }
        /// <summary>
        /// 用户临时数据
        /// </summary>
        public int UserTag { get; set; }
        /// <summary>
        /// 用户临时数据
        /// </summary>
        public object UserObject { get; set; }
        object Data { get; }
    }
    public interface IMappingNode<T> : IMappingNode
    {
        new T Data { get; }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract class MappingObject : AsyncDisposable, IMappingNode
    {
        public const string F_KEY_SUBMAPPING = "*";
        private readonly LinkedList<ObjectUpdateEntry> saveBatch = new LinkedList<ObjectUpdateEntry>();
        //protected const string ck_ParentFieldName = ".parent";
        protected readonly ITaskExecutor executor;
        protected readonly IMappingAdapter adapter;
        protected static Logger log = new LazyLogger("MappingObject");
        private readonly string displayName;
        private IMappingLocker locker;
        private string key;
        private IMappingNode parent;
        public string DisplayName { get => displayName; }
        public virtual bool IsReadOnly { get; internal set; } = true;
        public string Key { get => key; }
        public IMappingAdapter Adapter { get => adapter; }
        public ITaskExecutor Executor { get => executor; }
        //public bool IsDirty { get => saveBatch.Count > 0; }
        public bool IsTopMapping { get => parent == null; }
        public bool HasValue { get => Data != null; }
        public IMappingNode Parent { get => parent; }
        public int UserTag { get; set; }
        public object UserObject { get; set; }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------
        public abstract object Data { get; }

        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder("ORM:MappingObject");
        public MappingObject(string key, string displayName, ITaskExecutor exe, IMappingAdapter db)
        {
            if (this is MappingReference) Alloc.RecordConstructor(GetType());
            this.key = key;
            this.displayName = displayName;
            this.executor = exe ?? ITaskExecutor.Default;
            this.adapter = db ?? ORMFactory.Instance.DefaultAdapter;
            this.Init();
        }
        ~MappingObject()
        {
            try
            {
                this.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            finally
            {
                if (this is MappingReference) Alloc.RecordDestructor(GetType());
            }
        }
        sealed protected override void RecordDisposing()
        {
            if (this is MappingReference) Alloc.RecordDispose(GetType());
        }
        sealed protected override void Disposing()
        {
            CleanUp();
        }
        sealed protected override ValueTask DisposingAsync()
        {
            CleanUp();
            return new ValueTask(Task.CompletedTask);
        }
        protected virtual void CleanUp()
        {
            this.event_OnDataChanged = null;
            this.InternalForEachSubFields(f => f.CleanUp());
            this.InternalClearBatch();
        }
        internal void SetParent(MappingObject parent)
        {
            this.parent = parent;
            this.IsReadOnly = parent.IsReadOnly;
        }
        protected virtual void Init() { }
        protected virtual MappingObject CreateSubMapping(string fieldName, Type fieldType)
        {
            return MappingConverter.Instance.CreateSubMapping(this, fieldName, fieldType);
        }
        protected virtual string GetSubMappingName(string fieldName, Type fieldType)
        {
            return MappingConverter.Instance.GetSubMappingName(this.Key, fieldName, fieldType);
        }
        protected virtual IWrapper CreateSubWrapper(string fieldName, Type fieldType)
        {
            return MappingConverter.Instance.CreateWrapper(fieldType, this);
        }
        protected abstract object CreateInstance();
        //--------------------------------------------------------------------------------------------
        protected virtual IMappingDatabase StartDatabase()
        {
            return this.adapter.CreateDatabase();
        }
        protected virtual IMappingDatabase StartDatabase(Action<IMappingDatabase> action)
        {
            var db = this.adapter.CreateDatabase();
            action(db);
            return db;
        }
        protected IObjectTransaction StartTransaction(Action<IObjectTransaction> action, params ICondition[] conditions)
        {
            var trans = new ObjectTransaction(this.adapter, conditions);
            action(trans);
            return trans;
        }
        protected IObjectTransaction StartTransaction(Action<IObjectTransaction> action)
        {
            var trans = new ObjectTransaction(this.adapter);
            action(trans);
            return trans;
        }
        //--------------------------------------------------------------------------------------------
        public Task<bool> EnterLockAsync(out string token)
        {
            if (this.locker == null)
            {
                this.locker = this.adapter.CreateExecutablLocker(key + "._lock", this.executor);
            }
            return locker.EnterLockAsync(out token);
        }
        public Task<bool> ExitLockAsync(string token)
        {
            return locker.ExitLockAsync(token);
        }
        public async Task<TResult> LockCourseAsync<TResult>(Func<Task<TResult>> action)
        {
            var ret = default(TResult);
            if (await this.EnterLockAsync(out var token))
            {
                try
                {
                    ret = await action();
                }
                finally
                {
                    await this.ExitLockAsync(token);
                }
            }
            return ret;
        }
        public async Task LockCourseAsync(Func<Task> action)
        {
            if (await this.EnterLockAsync(out var token))
            {
                try
                {
                    await action();
                }
                finally
                {
                    await this.ExitLockAsync(token);
                }
            }
        }
        //--------------------------------------------------------------------------------------------

        public int BatchFlush(IObjectTransaction trans)
        {
            trans.DebugBeginMappingObject(this);
            return InternalFlush(trans);
        }
        public void BatchSaveData(IObjectTransaction trans)
        {
            trans.DebugBeginMappingObject(this);
            InternalSaveData(trans, this.Data, true);
        }
        public void BatchSaveData(object data, IObjectTransaction trans)
        {
            trans.DebugBeginMappingObject(this);
            InternalSaveData(trans, data, true);
        }
        public Task FlushAsync()
        {
            return executor.Execute(StartTransaction(db =>
            {
                InternalFlush(db);
            }).ExecuteAsync());
        }
        public Task FlushAsync(ICondition condition)
        {
            return executor.Execute(StartTransaction(db =>
            {
                InternalFlush(db);
            }, condition).ExecuteAsync());
        }
        public Task SaveDataAsync()
        {
            return executor.Execute(StartTransaction(db =>
            {
                InternalSaveData(db, this.Data, true);
            }).ExecuteAsync());
        }
        public async Task SaveDataAsync(object data)
        {
            if (this.Data == null && data == null)
            {
                var db = this.StartDatabase();
                var entries = await db.ObjectBatchQueryAsync(this.key);
                var dt = await this.InternalLoadDataAsync(db, entries, true);
                if (dt == null) { return; }
            }
            await executor.Execute(StartTransaction(db =>
            {
                InternalSaveData(db, data, true);
            }).ExecuteAsync());
        }
        public async Task SaveDataAsync(object data, ICondition condition)
        {
            if (this.Data == null && data == null)
            {
                var db = this.StartDatabase();
                var entries = await db.ObjectBatchQueryAsync(this.key);
                var dt = await this.InternalLoadDataAsync(db, entries, true);
                if (dt == null) { return; }
            }
            await executor.Execute(StartTransaction(db =>
            {
                InternalSaveData(db, data, true);
            }, condition).ExecuteAsync());
        }
        public async Task<object> LoadDataAsync()
        {
            var db = this.StartDatabase();
            var entries = await db.ObjectBatchQueryAsync(this.key);
            var data = await this.InternalLoadDataAsync(db, entries, true);
            return await executor.FromResult(data);
        }
        //--------------------------------------------------------------------------------------------



        //--------------------------------------------------------------------------------------------
        internal void InternalRename(string newKey)
        {
            var oldKey = key;
            this.key = newKey;
            this.saveBatch.AddFirst(new ObjectUpdateEntry(ExecuteEvent.RENAME_KEY, oldKey, newKey));
        }
        internal void InternalEnqueueBatch(LinkedListNode<ObjectUpdateEntry> entry)
        {
            if (entry.List != null) { saveBatch.Remove(entry); }
            saveBatch.AddLast(entry);
        }
        internal void InternalEnqueueBatch(ObjectUpdateEntry entry)
        {
            saveBatch.AddLast(entry);
        }
        internal void InternalInsertBatch(LinkedListNode<ObjectUpdateEntry> entry)
        {
            if (entry.List != null) { saveBatch.Remove(entry); }
            saveBatch.AddFirst(entry);
        }
        internal void InternalInsertBatch(ObjectUpdateEntry entry)
        {
            saveBatch.AddFirst(entry);
        }
        internal void InternalClearBatch()
        {
            saveBatch.Clear();
        }
        //--------------------------------------------------------------------------------------------
        internal virtual int InternalFlush(IObjectTransaction db)
        {
            var events = new List<ObjectUpdateEntry>(saveBatch);
            {
                saveBatch.Clear();
                if (InternalFlushBegin(db, events))
                {
                    var ret = events.Count;
                    db.Database.EnqueueHashBatchUpdate(db, this.Key, events);
                    //遍历所有字段
                    this.InternalForEachSubFields((sub) =>
                    {
                        if (sub.Mapping != null)
                        {
                            ret += sub.Mapping.InternalFlush(db);
                        }
                    });
                    InternalFlushEnd(db);
                    return ret;
                }
            }
            return 0;
        }
        protected virtual bool InternalFlushBegin(IObjectTransaction db, IList<ObjectUpdateEntry> events)
        {
            return false;
        }
        protected virtual void InternalFlushEnd(IObjectTransaction db)
        {
        }
        //--------------------------------------------------------------------------------------------
        internal abstract void InternalSaveData(IObjectTransaction db, object data, bool fireEvent);
        internal abstract void InternalSetData(object data, bool fireEvent);
        internal abstract Task<object> InternalLoadDataAsync(IMappingDatabase db, HashMap<string, ObjectQueryEntry[]> batch, bool fireEvent);
        //--------------------------------------------------------------------------------------------
        internal abstract void InternalForEachSubFields(Action<FieldMapping> action);
        //--------------------------------------------------------------------------------------------
        //         public ICondition ConditionFieldNotEqual(string fieldName, object fieldValue)
        //         {
        //             return ORMFactory.Instance.Conditions.HashNotEqual(this.key, fieldName, fieldValue);
        //         }
        //         public ICondition ConditionFieldEqual(string fieldName, object fieldValue)
        //         {
        //             return ORMFactory.Instance.Conditions.HashEqual(this.key, fieldName, fieldValue);
        //         }
        //         public ICondition ConditionFieldExists(string fieldName)
        //         {
        //             return ORMFactory.Instance.Conditions.HashExists(this.key, fieldName);
        //         }
        //         public ICondition ConditionFieldNotExists(string fieldName)
        //         {
        //             return ORMFactory.Instance.Conditions.HashNotExists(this.key, fieldName);
        //         }
        //--------------------------------------------------------------------------------------------
        protected virtual void InternalFireDataChanged(object data, bool fireEvent)
        {
            if (fireEvent)
            {
                event_OnDataChanged?.Invoke(data);
            }
        }
        private Action<object> event_OnDataChanged;

        #region FieldMapping
        public abstract class FieldMapping
        {
            virtual public object Key { get; private set; }
            public string HashName { get; private set; }
            public string FieldName { get; private set; }
            public IMappingNode MappingNode { get; }
            public MappingObject Mapping { get; }
            public IWrapper Wrapper { get; }
            internal LinkedListNode<ObjectUpdateEntry> Batch { get; private set; }
            public FieldMapping(object key, string fieldName, MappingObject mapping, IWrapper wrapper)
            {
                this.Key = key;
                this.FieldName = fieldName;
                this.HashName = mapping == null ? fieldName : $"{F_KEY_SUBMAPPING}{fieldName}";
                this.Mapping = mapping;
                this.Wrapper = wrapper;
                this.Batch = new LinkedListNode<ObjectUpdateEntry>(new ObjectUpdateEntry());
                if (mapping != null)
                {
                    this.MappingNode = mapping;
                }
                else if (wrapper != null)
                {
                    this.MappingNode = wrapper;
                }
            }
            internal virtual void CleanUp()
            {
                Batch = null;
                MappingNode?.Dispose();
            }
            protected void ReIndex(object field, string fieldName)
            {
                this.Key = field;
                this.FieldName = fieldName;
                this.HashName = Mapping == null ? fieldName : $"{F_KEY_SUBMAPPING}{fieldName}";
            }
            internal virtual void RemoveFromParent(IMappingNode parent)
            {
                //                 if (Wrapper != null)
                //                 {
                // 
                //                 }
                //                 else if (Mapping != null)
                //                 {
                // 
                //                 }
            }
        }
        public class FieldMapping<K> : FieldMapping
        {
            public K Field { get; private set; }
            private Action<FieldMapping<K>, object> onMappingDataChanged;
            private Action<FieldMapping<K>, IWrapper> onWrapperDataChanged;
            public FieldMapping(K field, string fieldName, MappingObject mapping, Action<FieldMapping<K>, object> onMappingDataChanged)
                : this(field, fieldName, mapping, onMappingDataChanged, null, null) { }
            public FieldMapping(K field, string fieldName, IWrapper wrapper, Action<FieldMapping<K>, IWrapper> onWrapperDataChanged)
                : this(field, fieldName, null, null, wrapper, onWrapperDataChanged) { }
            protected FieldMapping(K field, string fieldName, MappingObject mapping, Action<FieldMapping<K>, object> onMappingDataChanged, IWrapper wrapper, Action<FieldMapping<K>, IWrapper> onWrapperDataChanged)
                : base(field, fieldName, mapping, wrapper)
            {
                this.Field = field;
                if (mapping != null)
                {
                    this.onMappingDataChanged = onMappingDataChanged;
                    mapping.event_OnDataChanged += OnFieldChanged;
                }
                if (wrapper != null)
                {
                    this.onWrapperDataChanged = onWrapperDataChanged;
                    wrapper.OnDataChanged += OnWrapperChanged;
                }
            }
            internal override void CleanUp()
            {
                this.onMappingDataChanged = null;
                this.onWrapperDataChanged = null;
                base.CleanUp();
            }

            internal override void RemoveFromParent(IMappingNode parent)
            {
                this.onMappingDataChanged = null;
                this.onWrapperDataChanged = null;
                if (Mapping != null)
                {
                    Mapping.event_OnDataChanged -= OnFieldChanged;
                }
                else if (Wrapper != null)
                {
                    Wrapper.OnDataChanged -= OnWrapperChanged;
                }
            }
            internal void ReIndex(K field, string fieldName)
            {
                this.Field = field;
                base.ReIndex(field, fieldName);
            }
            private void OnFieldChanged(object value)
            {
                onMappingDataChanged(this, value);
            }
            private void OnWrapperChanged(IWrapper wrapper)
            {
                onWrapperDataChanged(this, wrapper);
            }
        }
        public class FieldMappingMap<K, V> where V : FieldMapping
        {
            private readonly MappingObject owner;
            private readonly HashMap<K, V> map = new HashMap<K, V>();
            public ICollection<V> Values { get => map.Values; }
            public ICollection<K> Keys { get => map.Keys; }
            public int Count { get => map.Count; }
            public FieldMappingMap(MappingObject owner)
            {
                this.owner = owner;
            }
            public bool Contains(K key)
            {
                return map.ContainsKey(key);
            }
            public V[] ToArray()
            {
                return map.Values.ToArray();
            }
            public V Get(K key)
            {
                return map.Get(key);
            }
            public V GetOrAdd(K key, Func<K, V> create)
            {
                return map.GetOrAdd(key, create);
            }
            public bool TryGetValue(K key, out V value)
            {
                return map.TryGetValue(key, out value);
            }
            public bool TryGetOrCreate(K key, out V value, Func<K, V> create)
            {
                return map.TryGetOrCreate(key, out value, create);
            }
            public void Add(V field)
            {
                map.Add((K)field.Key, field);
            }
            public void Remove(V field)
            {
                field.RemoveFromParent(owner);
                map.Remove((K)field.Key);
            }
            public V RemoveByKey(K key)
            {
                if (TryGetValue(key, out var value))
                {
                    this.Remove(value);
                    return value;
                }
                return null;
            }
            public void Clear()
            {
                foreach (var f in map.Values)
                {
                    f.RemoveFromParent(owner);
                }
                map.Clear();
            }
        }
        public class FieldMappingList<V> where V : FieldMapping
        {
            private readonly MappingObject owner;
            private readonly List<V> list = new List<V>();
            public ICollection<V> Values { get => list; }
            public int Count { get => list.Count; }
            public V this[int index]
            {
                get { return list[index]; }
                set
                {
                    var old = list[index];
                    if (!object.ReferenceEquals(old, value))
                    {
                        old.RemoveFromParent(owner);
                        list[index] = value;
                    }
                }
            }
            public FieldMappingList(MappingObject owner)
            {
                this.owner = owner;
            }
            public void Remove(int index)
            {
                var old = list[index];
                if (old != null)
                {
                    old.RemoveFromParent(owner);
                }
            }
            public void Insert(int index, V value)
            {
                this.list.Insert(index, value);
            }
            public void SetSize(int count, Func<int, V> create)
            {
                CUtils.SetListSize(list, count, create);
            }
            public void Clear()
            {
                foreach (var f in list)
                {
                    f.RemoveFromParent(owner);
                }
                list.Clear();
            }
        }
        #endregion
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    public class MappingReference : MappingObject
    {
        public const string F_TYPE_FIELD_NAME = ".type";
        public const string F_TIME_FIELD_NAME = ".time";
        private Type currentDataType;
        private readonly FieldMappingMap<string, DynamicFieldMapping> fieldMappings;
        private DateTime updateTimeUTC = DateTime.UtcNow;
        private object data;
        public override object Data { get => data; }
        public DateTime UpdateTimeUTC { get => updateTimeUTC; }
        public Type DataType { get => currentDataType; }
        public MappingReference(string key, Type type, ITaskExecutor exe, IMappingAdapter db)
         : base(key, (string.Format("{0}:{1}", type.Name, key)), exe, db)
        {
            this.fieldMappings = new FieldMappingMap<string, DynamicFieldMapping>(this);
            this.TrySetDataType(type);
        }
        public MappingReference(string prefix, string key, Type type, ITaskExecutor exe, IMappingAdapter db)
            : base(prefix != null ? (prefix + key) : key, (string.Format("{0}:{1}", type.Name, key)), exe, db)
        {
            this.fieldMappings = new FieldMappingMap<string, DynamicFieldMapping>(this);
            this.TrySetDataType(type);
        }
        protected override void CleanUp()
        {
            base.CleanUp();
            this.fieldMappings.Clear();
        }
        protected override object CreateInstance()
        {
            this.data = DeepActivator.CreateInstance(currentDataType);
            return data;
        }
        private bool TrySetDataType(Type type)
        {
            base.InternalClearBatch();
            if (type == null)
                return false;
            if (type != currentDataType)
            {
                this.currentDataType = type;
                var dataFields = DynamicMethodTypeFactory.Instance.GetTypeInfo(type);
                this.fieldMappings.Values.WritableForEach((field) =>
                {
                    if (dataFields.GetField(field.FieldName) == null)
                    {
                        fieldMappings.Remove(field);
                    }
                });
                foreach (var field in dataFields.GetFields())
                {
                    if (MappingConverter.Instance.IsPersistField(field.Field) && !fieldMappings.Contains(field.Field.Name))
                    {
                        fieldMappings.Add(InternalCreateFieldMapping(field as IDynamicFieldInfo));
                    }
                }
                this.OnDataTypeChanged(type);
                return true;
            }
            return false;
        }
        protected virtual void OnDataTypeChanged(Type type) { }
        protected virtual DynamicFieldMapping InternalCreateFieldMapping(IDynamicFieldInfo field)
        {
            MappingObject sub_mapping = null;
            IWrapper sub_wrapper = null;
            if (MappingConverter.Instance.IsMappingObject(field.Field.FieldType))
            {
                sub_mapping = this.CreateSubMapping(field.Name, field.Field.FieldType);
            }
            else if (MappingConverter.Instance.IsWrapperType(field.Field.FieldType))
            {
                sub_wrapper = this.CreateSubWrapper(field.Name, field.Field.FieldType);
            }
            return new DynamicFieldMapping(field, field.Name, sub_mapping, InternalFieldChanged, sub_wrapper, InternalWrapperChanged);
        }
        protected virtual void InternalFieldChanged(FieldMapping<IDynamicFieldInfo> field, object fieldValue)
        {
            InternalSetSubField(field as DynamicFieldMapping, fieldValue);
            if (fieldValue != null)
            {
                if (field.Mapping != null)
                    field.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, field.Mapping.Key);
                else
                    field.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue);
            }
            else
            {
                field.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName);
            }
            base.InternalEnqueueBatch(field.Batch);
        }
        protected virtual void InternalWrapperChanged(FieldMapping<IDynamicFieldInfo> field, IWrapper wrapper)
        {
            if (field.Wrapper == wrapper)
            {
                var sub_wrapper = field.Wrapper;
                //获取当前外键索引
                if (data != null)
                {
                    var fieldValue = sub_wrapper.Data;
                    field.Field.SetValue(data, fieldValue);
                    //添加到预提交列表
                    if (fieldValue != null)
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue));
                    else
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                    base.InternalEnqueueBatch(field.Batch);
                }
            }
        }
        protected virtual void InternalUpdateWrapper(DynamicFieldMapping field, object fieldValue)
        {
            if (field.Wrapper != null)
            {
                field.Wrapper.RunWithNoEvent(() =>
                {
                    field.Wrapper.Data = fieldValue;
                });
            }
        }
        private void InternalSetSubField(DynamicFieldMapping field, object fieldValue)
        {
            if (field.NotNull && fieldValue == null)
            {
                fieldValue = DeepActivator.CreateInstance(field.Field.Field.FieldType);
            }
            if (data != null)
            {
                field.Field.SetValue(this.data, fieldValue);
            }
            InternalUpdateWrapper(field, fieldValue);
        }
        internal override void InternalForEachSubFields(Action<FieldMapping> action)
        {
            foreach (var sub in fieldMappings.Values)
            {
                action(sub);
            }
        }
        protected DynamicFieldMapping InternalGetSubField(string fieldName)
        {
            return fieldMappings.Get(fieldName);
        }

        internal override async Task<object> InternalLoadDataAsync(IMappingDatabase db, HashMap<string, ObjectQueryEntry[]> batch, bool change)
        {
            ORMStatistics.LogLoad(GetType());
            InternalClearBatch();
            ObjectQueryEntry[] entries = null;
            if (batch == null || !batch.TryGetValue(this.Key, out entries))
            {
                //获取所有Entry
                entries = await db.ObjectHashQueryEntriesAsync(this.Key);
            }
            if (entries != null && entries.Length > 0)
            {
                //读取对象实际类型
                //                 var real_type = Array.Find(entries, e => e.FieldName.ToString() == F_TYPE_FIELD_NAME);
                //                 if (real_type.FieldValue != null)
                if (entries.TryFind(e => e.FieldName.ToString() == F_TYPE_FIELD_NAME, out var real_type))
                {
                    var type = ReflectionUtil.GetType(real_type.FieldValue.ToString());
                    //尝试重新设置对象实际类型（声明父类，实际存储为子类）
                    this.TrySetDataType(type);
                    this.CreateInstance();
                    this.InternalFireDataChanged(data, change);
                    var updating = default(List<ObjectUpdateEntry>);
                    {
                        if (IsTopMapping)
                        {
                            updating = updating ?? new List<ObjectUpdateEntry>();
                            this.updateTimeUTC = DateTime.UtcNow;
                            updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_TIME_FIELD_NAME, this.updateTimeUTC));
                            updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_TOP_KEY, this.Key, this.updateTimeUTC));
                        }
                        foreach (var e in entries)
                        {
                            var fname = e.FieldName.ToString();
                            if (!fname.StartsWith("."))
                            {
                                if (fname.StartsWith(F_KEY_SUBMAPPING)) { fname = fname.Substring(F_KEY_SUBMAPPING.Length); }
                                var field = fieldMappings.Get(fname);
                                if (field != null)
                                {
                                    await InternalLoadFieldAsync(db, field, e.FieldValue, batch);
                                }
                                else if (!IsReadOnly)
                                {
                                    log.WarnFormat("{0} : Delete Not Exist Field : {1}", this.DisplayName, e.FieldName);
                                    updating = updating ?? new List<ObjectUpdateEntry>();
                                    updating.Add(new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, e.FieldName.ToString()));
                                }
                            }
                        }
                        if (updating != null && updating.Count > 0)
                        {
                            await db.ObjectHashBatchUpdateAsync(this.Key, updating);
                        }
                    }
                    return data;
                }
            }
            if (IsTopMapping)
            {
                //从Dump数据里加载为热数据//
                var dump = await db.PersistRecoverAsync(this.Key);
                this.data = dump;
                if (dump != null)
                {
                    //恢复写入Redis缓存//
                    await StartTransaction(trans =>
                    {
                        InternalSaveData(trans, data, true);
                    }).ExecuteAsync();
                }
                this.InternalFireDataChanged(data, change);
            }
            return data;
        }
        internal async Task InternalLoadFieldAsync(IMappingDatabase db, DynamicFieldMapping field, IConvertible fieldValue, HashMap<string, ObjectQueryEntry[]> batch)
        {
            if (fieldValue != null)
            {
                //TODO 如果是复合类型, fieldValue 为外键索引
                var sub_mapping = field.Mapping;
                if (sub_mapping != null)
                {
                    if (!string.IsNullOrEmpty(fieldValue.ToString()))
                    {
                        var sub_data = await sub_mapping.InternalLoadDataAsync(db, batch, false);
                        InternalSetSubField(field, sub_data);
                    }
                    else
                    {
                        //field.Field.Set(data, null);
                        InternalSetSubField(field, null);
                    }
                }
                else
                {
                    var fvalue = ORMFactory.Instance.DecodeObject(fieldValue, field.Field.Field.FieldType);
                    InternalSetSubField(field, fvalue);
                }
            }
            else
            {
                InternalSetSubField(field, null);
            }
        }
        internal async Task<object> InternalLoadFieldAsync(IMappingDatabase db, DynamicFieldMapping field, HashMap<string, ObjectQueryEntry[]> batch)
        {
            if (data != null)
            {
                var sub_mapping = field.Mapping;
                if (sub_mapping != null)
                {
                    var fieldValue = await sub_mapping.InternalLoadDataAsync(db, batch, false);
                    InternalSetSubField(field, fieldValue);
                    return fieldValue;
                }
                else
                {
                    var fieldValue = await db.ObjectHashQueryEntryAsync(this.Key, field.HashName);
                    var ret = ORMFactory.Instance.DecodeObject(fieldValue, field.Field.Field.FieldType);
                    InternalSetSubField(field, ret);
                    return ret;
                }
            }
            else
            {
                return null;
            }
        }

        internal override void InternalSaveData(IObjectTransaction db, object data, bool updateChange)
        {
            ORMStatistics.LogSave(GetType());
            if (this.data == null && data == null)
            {
                //原本没有加载过，如果直接删除Key，会导致子链断裂//
                //throw new Exception("Mapping Data Is Null : " + this.Key);
                return;
            }
            this.InternalClearBatch();
            this.data = data;
            this.InternalFireDataChanged(this.data, updateChange);
            if (data != null)
            {
                var updating = new List<ObjectUpdateEntry>(fieldMappings.Count + 1);
                {
                    //尝试重新设置对象实际类型（声明父类，实际存储为子类）
                    TrySetDataType(data.GetType());
                    if (IsTopMapping)
                    {
                        this.updateTimeUTC = DateTime.UtcNow;
                        updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_TOP_KEY, this.Key, this.updateTimeUTC));
                        updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_TIME_FIELD_NAME, this.updateTimeUTC));
                    }
                    updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_TYPE_FIELD_NAME, currentDataType.FullName));
                    //遍历所有字段
                    foreach (var sub in fieldMappings.Values)
                    {
                        var fieldValue = sub.Field.GetValue(data);
                        InternalUpdateWrapper(sub, fieldValue);
                        updating.Add(InternalSaveMappingField(db, sub, fieldValue, false));
                    }
                    db.Database.EnqueueHashBatchUpdate(db, this.Key, updating);
                }
            }
            else
            {
                if (IsTopMapping)
                {
                    db.Database.EnqueueHashUpdate(db, this.Key, new ObjectUpdateEntry(ExecuteEvent.DELETE_TOP_KEY, this.Key));
                }
                //清理数据，删除所有外键
                foreach (var sub in fieldMappings.Values)
                {
                    InternalUpdateWrapper(sub, null);
                    if (sub.Mapping != null)
                    {
                        sub.Mapping.InternalSaveData(db, null, false);
                    }
                }
                db.Enqueue(db.Database.ObjectHashUpdateAsync(this.Key, new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY, this.Key)));
            }

        }
        internal ObjectUpdateEntry InternalSaveMappingField(IObjectTransaction db, DynamicFieldMapping field, object fieldValue, bool change)
        {
            //获取当前外键索引
            var sub_mapping = field.Mapping;
            if (sub_mapping != null)
            {
                //存储外键
                sub_mapping.InternalSaveData(db, fieldValue, change);
                if (fieldValue != null)
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, sub_mapping.Key);
                }
                else
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName);
                }
            }
            else
            {
                //添加到预提交列表
                if (fieldValue != null)
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue);
                else
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName);
            }
        }
        internal Task InternalSaveFieldsAsync(List<DynamicFieldMapping> fields)
        {
            return StartTransaction(db =>
            {
                var updating = new List<ObjectUpdateEntry>(fields.Count + 1);
                {
                    foreach (var sub in fields)
                    {
                        var fieldValue = sub.Field.GetValue(data);
                        InternalUpdateWrapper(sub, fieldValue);
                        updating.Add(InternalSaveMappingField(db, sub, fieldValue, false));
                    }
                    db.Database.EnqueueHashBatchUpdate(db, this.Key, updating);
                }
            }).ExecuteAsync();
        }

        protected override bool InternalFlushBegin(IObjectTransaction taskQueue, IList<ObjectUpdateEntry> events)
        {
            ORMStatistics.LogSave(GetType());
            //if (data != null) events.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, typeFieldName, dataType.FullName));
            return true;
        }

        internal override void InternalSetData(object data, bool change)
        {
            base.InternalClearBatch();
            this.data = data;
            this.InternalFireDataChanged(this.data, change);
            if (data != null)
            {
                //尝试重新设置对象实际类型（声明父类，实际存储为子类）
                this.TrySetDataType(data.GetType());
                if (IsTopMapping)
                {
                    this.updateTimeUTC = DateTime.UtcNow;
                    base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.UPDATE_TOP_KEY, this.Key, this.updateTimeUTC));
                    base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_TIME_FIELD_NAME, this.updateTimeUTC));
                }
                base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_TYPE_FIELD_NAME, currentDataType.FullName));
                //遍历所有字段
                foreach (var field in fieldMappings.Values)
                {
                    this.InternalSetMappingField(field, field.Field.GetValue(data), false);
                }
            }
            else
            {
                if (IsTopMapping)
                {
                    base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.DELETE_TOP_KEY, this.Key));
                }
                //遍历所有字段
                foreach (var field in fieldMappings.Values)
                {
                    this.InternalSetMappingField(field, null, false);
                }
                // Null Null 表示干掉自己
                base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY));
            }
        }
        protected void InternalSetMappingField(DynamicFieldMapping field, object fieldValue, bool change)
        {
            //获取当前外键索引
            var sub_mapping = field.Mapping;
            if (data != null)
            {
                InternalUpdateWrapper(field, fieldValue);
                if (sub_mapping != null)
                {
                    //存储外键
                    sub_mapping.InternalSetData(fieldValue, change);
                    if (fieldValue != null)
                    {
                        // fieldValue 为外键索引，添加到预提交列表
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, sub_mapping.Key));
                    }
                    else
                    {
                        // fieldValue 为外键索引，添加到预提交列表
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                    }
                }
                else
                {
                    //添加到预提交列表
                    if (fieldValue != null)
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue));
                    else
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                }
                base.InternalEnqueueBatch(field.Batch);
            }
            else
            {
                InternalUpdateWrapper(field, null);
                //清理外键
                if (sub_mapping != null)
                {
                    sub_mapping.InternalSetData(null, change);
                }
            }
        }
        //----------------------------------------------------------------------------------------
        public void SetData(object data)
        {
            this.InternalSetData(data, true);
        }
        public bool TryGetField(string fieldName, out object value)
        {
            var field = this.InternalGetSubField(fieldName);
            if (field != null)
            {
                value = field.Field.GetValue(this.Data);
                return true;
            }
            value = null;
            return false;
        }
        /// <summary> 自动刷新 </summary>
        public void SetField(string fieldName, object fieldValue)
        {
            var field = this.InternalGetSubField(fieldName);
            if (field != null)
            {
                field.Field.SetValue(this.Data, fieldValue);
                this.InternalSetMappingField(field, fieldValue, false);
            }
        }
        public MappingObject GetMappingField(string fieldName)
        {
            if (fieldMappings.TryGetValue(fieldName, out var ret))
            {
                return ret.Mapping;
            }
            return null;
        }
        public IWrapper GetWrapperField(string fieldName)
        {
            if (fieldMappings.TryGetValue(fieldName, out var ret))
            {
                return ret.Wrapper;
            }
            return null;
        }
        /// <summary> 自动刷新 </summary>
        public DynamicFieldMapping SetMappingField(string fieldName, object fieldValue)
        {
            var field = InternalGetSubField(fieldName);
            if (field != null)
            {
                field.Field.SetValue(this.data, fieldValue);
                InternalSetMappingField(field, fieldValue, false);
                return field;
            }
            return null;
        }
        /// <summary> 自动刷新 </summary>
        public DynamicFieldMapping SetFieldDirty(string fieldName)
        {
            var field = InternalGetSubField(fieldName);
            if (field != null)
            {
                var fieldValue = field.Field.GetValue(data);
                //获取当前外键索引
                var sub_mapping = field.Mapping;
                if (data != null)
                {
                    if (sub_mapping != null)
                    {
                        //存储外键
                        sub_mapping.InternalSetData(fieldValue, false);
                        if (fieldValue != null)
                        {
                            // fieldValue 为外键索引，添加到预提交列表
                            field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, sub_mapping.Key));
                        }
                        else
                        {
                            // fieldValue 为外键索引，添加到预提交列表
                            field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                        }
                    }
                    else
                    {
                        //添加到预提交列表
                        if (fieldValue != null)
                            field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue));
                        else
                            field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                    }
                    base.InternalEnqueueBatch(field.Batch);
                }
                else
                {
                    //清理外键
                    if (sub_mapping != null)
                    {
                        sub_mapping.InternalSetData(null, false);
                    }
                }
                return field;
            }
            return null;
        }
        /// <summary> 自动刷新 </summary>
        public void SetFieldDirty(DynamicFieldMapping field)
        {
            InternalSetMappingField(field, field.Field.GetValue(data), false);
        }

        public Task<V> LoadFieldAsync<V>(string fieldName)
        {
            var field = this.InternalGetSubField(fieldName);
            if (field != null)
            {
                var db = StartDatabase();
                return executor.Execute(this.InternalLoadFieldAsync(db, field, null).ContinueWith(t => t.GetResultAs<V>()));
            }
            return Task.FromResult<V>(default(V));
        }
        public Task<V> LoadFieldAsync<V>(DynamicFieldMapping field)
        {
            var db = StartDatabase();
            return executor.Execute(this.InternalLoadFieldAsync(db, field, null).ContinueWith(t => t.GetResultAs<V>()));
        }

        public Task SaveFieldAsync(string fieldName, object fieldValue)
        {
            var field = this.InternalGetSubField(fieldName);
            if (field != null)
            {
                InternalSetSubField(field, fieldValue);
                return executor.Execute(StartTransaction(db =>
                {
                    var entry = this.InternalSaveMappingField(db, field, fieldValue, false);
                    db.Enqueue(db.Database.ObjectHashUpdateAsync(this.Key, entry));
                }).ExecuteAsync());
            }
            return Task.CompletedTask;
        }
        public Task SaveFieldAsync(DynamicFieldMapping field, object fieldValue)
        {
            InternalSetSubField(field, fieldValue);
            return executor.Execute(StartTransaction(db =>
            {
                var entry = this.InternalSaveMappingField(db, field, fieldValue, false);
                db.Enqueue(db.Database.ObjectHashUpdateAsync(this.Key, entry));
            }).ExecuteAsync());
        }
        public async Task SaveFieldsAsync(params string[] fields)
        {
            var list = new List<DynamicFieldMapping>();
            {
                foreach (var fieldName in fields)
                {
                    var field = this.InternalGetSubField(fieldName);
                    if (field != null)
                    {
                        list.Add(field);
                    }
                }
                await this.executor.Execute(this.InternalSaveFieldsAsync(list));
            }
        }
        //----------------------------------------------------------------------------------------

        public class DynamicFieldMapping : FieldMapping<IDynamicFieldInfo>
        {
            public override object Key => Field.Name;
            public bool NotNull { get; private set; }
            public DynamicFieldMapping(IDynamicFieldInfo field, string fieldName, MappingObject mapping, Action<FieldMapping<IDynamicFieldInfo>, object> onMappingDataChanged, IWrapper wrapper, Action<FieldMapping<IDynamicFieldInfo>, IWrapper> onWrapperDataChanged)
                : base(field, fieldName, mapping, onMappingDataChanged, wrapper, onWrapperDataChanged)
            {
                this.NotNull = field.Field.GetAttribute<PersistNotNullAttribute>() != null;
            }
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    public class MappingDictionary : MappingObject
    {
        public const string F_KEYS_FIELD_NAME = ".keys";
        private bool flushDirty = false;
        private readonly Type mapType;
        private readonly Type keyType;
        private readonly Type valueType;
        private readonly bool isSubMapping;
        private readonly FieldMappingMap<object, FieldMapping<object>> subMapping;
        private IDictionary map;

        public override object Data { get => map; }
        public IDictionary MapData { get => map; }
        public int MapCount { get => map != null ? map.Count : 0; }
        public Type MapType { get => mapType; }
        public Type KeyType { get => keyType; }
        public Type ValueType { get => valueType; }
        //   public bool IsValueMapping { get => isValueMapping; }

        protected internal MappingDictionary(string key, Type mapType, Type keyType, Type valueType, ITaskExecutor exe, IMappingAdapter db)
            : base(key, (string.Format("Map<{0},{1}>:{2}", keyType.Name, valueType, key)), exe, db)
        {
            this.mapType = mapType;
            this.keyType = keyType;
            this.valueType = valueType;
            this.isSubMapping = MappingConverter.Instance.IsMappingObject(valueType);
            this.subMapping = new FieldMappingMap<object, FieldMapping<object>>(this);
        }
        protected override void CleanUp()
        {
            base.CleanUp();
            this.subMapping.Clear();
        }
        protected override object CreateInstance()
        {
            this.map = ReflectionUtil.CreateGenericHashMap(mapType.GetGenericArguments());
            return map;
        }
        internal override void InternalForEachSubFields(Action<FieldMapping> action)
        {
            foreach (var sub in subMapping.Values)
            {
                action(sub);
            }
        }
        protected virtual FieldMapping<object> InternalCreateFieldMapping(object fieldKey)
        {
            var fieldName = InternalObjectKeyToString(fieldKey);
            if (isSubMapping)
            {
                return new FieldMapping<object>(fieldKey, fieldName, this.CreateSubMapping(fieldName, valueType), InternalFieldChanged);
            }
            else
            {
                return new FieldMapping<object>(fieldKey, fieldName, this.CreateSubWrapper(fieldName, valueType), InternalWrapperChanged);
            }
        }
        private void InternalFieldChanged(FieldMapping<object> field, object fieldValue)
        {
            if (map == null)
            {
                this.flushDirty = true;
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.map, true);
            }
            map[field.Field] = fieldValue;
        }
        private void InternalWrapperChanged(FieldMapping<object> field, IWrapper wrapper)
        {
            if (field.Wrapper == wrapper)
            {
                var sub_wrapper = field.Wrapper;
                if (map == null || map.Count == 0)
                {
                    this.flushDirty = true;
                    base.InternalClearBatch();
                    if (map == null) this.CreateInstance();
                    this.InternalFireDataChanged(this.map, true);
                }
                else
                {
                    var fieldValue = sub_wrapper.Data;
                    map[field.Field] = fieldValue;
                    if (fieldValue != null)
                    {
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue));
                    }
                    else
                    {
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                    }
                    base.InternalEnqueueBatch(field.Batch);
                }
            }
        }
        private void InternalUpdateWrapper(FieldMapping<object> field, object fieldValue)
        {
            if (field.Wrapper != null)
            {
                field.Wrapper.RunWithNoEvent(() =>
                {
                    field.Wrapper.Data = fieldValue;
                });
            }
        }
        protected object InternalKeyFromString(string key)
        {
            return ORMFactory.Instance.DecodeKey(key, keyType);
        }
        protected string InternalObjectKeyToString(object key)
        {
            return ORMFactory.Instance.EncodeKey(key);
        }
        protected HashSet<string> InternalEntryToKeys(ObjectQueryEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.FieldValue.ToString()))
            {
                var list = ORMFactory.Instance.DecodeObject<string[]>(entry.FieldValue);
                return new HashSet<string>(list);
            }
            return null;
        }
        protected ObjectUpdateEntry InternalObjectKeysToEntry()
        {
            if (map != null)
            {
                var list = new List<string>(map.Count);
                {
                    foreach (var e in map.Keys) { list.Add(InternalObjectKeyToString(e)); }
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_KEYS_FIELD_NAME, list.ToArray());
                }
            }
            else
            {
                return new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY);
            }
        }


        internal override async Task<object> InternalLoadDataAsync(IMappingDatabase db, HashMap<string, ObjectQueryEntry[]> batch, bool changeEvent)
        {
            base.InternalClearBatch();
            this.flushDirty = false;
            ObjectQueryEntry[] entries = null;
            if (batch == null || !batch.TryGetValue(this.Key, out entries))
            {
                //获取所有Entry
                entries = await db.ObjectHashQueryEntriesAsync(this.Key);
            }
            if (entries != null && entries.Length > 0)
            {
                var txt_keys = await InternalLoadTrimExcessAsync(db, entries);
                var new_map = (IDictionary)this.CreateInstance();
                this.InternalFireDataChanged(new_map, changeEvent);
                foreach (var e in entries)
                {
                    var fname = e.FieldName.ToString();
                    if (!fname.StartsWith("."))
                    {
                        if (fname.StartsWith(F_KEY_SUBMAPPING)) { fname = fname.Substring(F_KEY_SUBMAPPING.Length); }
                        if (txt_keys.Contains(fname))
                        {
                            await InternalLoadFieldAsync(db, new_map, fname, e.FieldValue, batch);
                        }
                    }
                }
                return map;
            }
            else
            {
                this.map = null;
                this.InternalFireDataChanged(this.map, changeEvent);
                this.subMapping.Clear();
                return map;
            }
        }
        internal async Task<HashSet<string>> InternalLoadTrimExcessAsync(IMappingDatabase db, ObjectQueryEntry[] entries)
        {
            //删除多余的数组元素
            var e_keys = Array.Find(entries, e => e.FieldName.ToString() == F_KEYS_FIELD_NAME);
            var txt_keys = InternalEntryToKeys(e_keys);
            if (txt_keys != null)
            {
                if (!IsReadOnly)
                {
                    var updating = new List<ObjectUpdateEntry>();
                    {
                        foreach (var hash_key in entries)
                        {
                            var hashName = hash_key.FieldName.ToString();
                            var fname = hashName;
                            if (!fname.StartsWith("."))
                            {
                                if (fname.StartsWith(F_KEY_SUBMAPPING)) { fname = fname.Substring(F_KEY_SUBMAPPING.Length); }
                                if (!txt_keys.Contains(fname))
                                {
                                    log.WarnFormat("{0} : Delete Not Exist Dictionary Field : {1}", this.DisplayName, hashName);
                                    updating.Add(new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, hashName));
                                }
                            }
                        }
                        if (updating.Count > 0)
                        {
                            await db.ObjectHashBatchUpdateAsync(base.Key, updating);
                        }
                    }
                }
                foreach (var sub in subMapping.ToArray())
                {
                    if (!txt_keys.Contains(sub.FieldName))
                    {
                        subMapping.Remove(sub);
                    }
                }
            }
            else
            {
                subMapping.Clear();
            }
            return txt_keys;
        }
        internal async Task InternalLoadFieldAsync(IMappingDatabase db, IDictionary map, string key, IConvertible fieldValue, HashMap<string, ObjectQueryEntry[]> batch)
        {
            var vkey = InternalKeyFromString(key);
            if (vkey != null)
            {
                // fieldValue 为外键索引
                var sub = subMapping.GetOrAdd(vkey, _ => InternalCreateFieldMapping(vkey));
                if (isSubMapping)
                {
                    //TODO 如果是复合类型，则创建新的Mapping
                    if (fieldValue != null && !string.IsNullOrEmpty(fieldValue.ToString()))
                    {
                        var sub_data = await sub.Mapping.InternalLoadDataAsync(db, batch, false);
                        map[vkey] = sub_data;
                    }
                    else
                    {
                        map[vkey] = null;
                    }
                }
                else
                {
                    var sub_value = ORMFactory.Instance.DecodeObject(fieldValue, valueType);
                    InternalUpdateWrapper(sub, sub_value);
                    map[vkey] = sub_value;
                }
            }
        }
        internal async Task<object> InternalLoadFieldAsync(IMappingDatabase db, object key, HashMap<string, ObjectQueryEntry[]> batch)
        {
            if (map != null)
            {
                var sub = subMapping.Get(key);
                if (sub != null)
                {
                    if (isSubMapping)
                    {
                        var fieldValue = await sub.Mapping.InternalLoadDataAsync(db, batch, false);
                        map[key] = fieldValue;
                        return fieldValue;
                    }
                    else
                    {
                        IConvertible fieldValue;
                        if (batch != null && batch.TryGetValue(this.Key, out var entries) && entries.TryFind(e => e.FieldName.ToString() == sub.HashName, out var entry))
                        {
                            fieldValue = entry.FieldValue;
                        }
                        else
                        {
                            fieldValue = await db.ObjectHashQueryEntryAsync(this.Key, sub.HashName);
                        }
                        var ret = ORMFactory.Instance.DecodeObject(fieldValue, valueType);
                        InternalUpdateWrapper(sub, ret);
                        map[key] = ret;
                        return ret;
                    }
                }
            }
            return null;
        }

        internal override void InternalSaveData(IObjectTransaction db, object data, bool changeEvent)
        {
            if (this.map == null && data == null)
            {
                //原本没有加载过，如果直接删除Key，会导致子链断裂//
                return;
            }
            this.InternalClearBatch();
            this.flushDirty = false;
            if (data != null)
            {
                this.map = (IDictionary)data;
                this.InternalFireDataChanged(this.map, changeEvent);
                var updating = new List<ObjectUpdateEntry>(map.Count + 1);
                {
                    //清理无用外键
                    var keys = InternalSaveTrimExcess(db);
                    updating.Add(keys);
                    //遍历所有字段
                    map.WritableForEachDictionary((map_e) =>
                    {
                        updating.Add(InternalSaveField(db, map_e.Key, map_e.Value, false, out var modifyKeys));
                    });
                    db.Database.EnqueueHashBatchUpdate(db, this.Key, updating);
                }
            }
            else
            {
                //清理数据，删除所有外键
                this.map = null;
                this.InternalFireDataChanged(this.map, changeEvent);
                this.InternalSaveTrimExcess(db);
                db.Enqueue(db.Database.ObjectHashUpdateAsync(this.Key, new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY)));
            }
        }
        private ObjectUpdateEntry InternalSaveTrimExcess(IObjectTransaction db)
        {
            if (map != null)
            {
                //连同删除外键
                foreach (var sub in subMapping.ToArray())
                {
                    if (!map.Contains(sub.Field))
                    {
                        subMapping.Remove(sub);
                        if (sub.Mapping != null)
                        {
                            sub.Mapping.InternalSaveData(db, null, false);
                        }
                    }
                }
            }
            else
            {
                //清理外键
                foreach (var sub in subMapping.Values)
                {
                    if (sub.Mapping != null)
                    {
                        sub.Mapping.InternalSaveData(db, null, false);
                    }
                }
                subMapping.Clear();
            }
            return InternalObjectKeysToEntry();
        }
        private ObjectUpdateEntry InternalSaveField(IObjectTransaction db, object fieldKey, object fieldValue, bool change, out bool modifiKeys)
        {
            if (fieldValue != null)
            {
                //获取当前外键索引
                modifiKeys = !subMapping.TryGetOrCreate(fieldKey, out var sub, _ =>
                {
                    return InternalCreateFieldMapping(fieldKey); //new FieldMapping<object>(fieldKey, base.CreateSubMapping(save_key, fieldValue.GetType()), InternalOnFieldDataChanged);
                });
                if (isSubMapping)
                {
                    //存储外键
                    sub.Mapping.InternalSaveData(db, fieldValue, change);
                    // value 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, sub.Mapping.Key);
                }
                else
                {
                    InternalUpdateWrapper(sub, fieldValue);
                    // value 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, fieldValue);
                }
            }
            else
            {
                var sub = subMapping.RemoveByKey(fieldKey);
                if (sub != null)
                {
                    modifiKeys = true;
                    if (sub.Mapping != null)
                    {
                        //存储外键
                        sub.Mapping.InternalSaveData(db, fieldValue, change);
                    }
                    else
                    {
                        InternalUpdateWrapper(sub, fieldValue);
                    }
                    // value 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                }
                else
                {
                    modifiKeys = false;
                    var save_key = InternalObjectKeyToString(fieldKey);
                    if (isSubMapping) { save_key = $"{F_KEY_SUBMAPPING}{save_key}"; }
                    // value 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, save_key);
                }
            }
        }
        private ObjectUpdateEntry InternalSaveRemoveField(IObjectTransaction db, object fieldKey, out bool modifiKeys)
        {
            var sub = subMapping.RemoveByKey(fieldKey);
            if (sub != null)
            {
                modifiKeys = true;
                if (sub.Mapping != null)
                {
                    //存储外键
                    sub.Mapping.InternalSaveData(db, null, false);
                }
                //                 else
                //                 {
                //                     InternalUpdateWrapper(sub, null);
                //                 }
                return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
            }
            else
            {
                modifiKeys = false;
                var save_key = InternalObjectKeyToString(fieldKey);
                if (isSubMapping) { save_key = $"{F_KEY_SUBMAPPING}{save_key}"; }
                return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, save_key);
            }
        }
        internal void InternalSavePutField(IObjectTransaction db, object fieldKey, object fieldValue)
        {
            var entry = InternalSaveField(db, fieldKey, fieldValue, false, out var modifyKeys);
            db.Enqueue(db.Database.ObjectHashUpdateAsync(base.Key, entry));
            if (modifyKeys)
            {
                db.Enqueue(db.Database.ObjectHashUpdateAsync(base.Key, InternalObjectKeysToEntry()));
            }
        }
        internal void InternalSaveRemoveField(IObjectTransaction db, object fieldKey)
        {
            var entry = InternalSaveRemoveField(db, fieldKey, out var modifyKeys);
            db.Enqueue(db.Database.ObjectHashUpdateAsync(base.Key, entry));
            if (modifyKeys)
            {
                db.Enqueue(db.Database.ObjectHashUpdateAsync(base.Key, InternalObjectKeysToEntry()));
            }
        }

        protected override bool InternalFlushBegin(IObjectTransaction db, IList<ObjectUpdateEntry> events)
        {
            if (flushDirty)
            {
                flushDirty = false;
                events.Insert(0, InternalObjectKeysToEntry());
            }
            return true;
        }

        internal override void InternalSetData(object data, bool changeEvent)
        {
            base.InternalClearBatch();
            this.flushDirty = true;
            if (data != null)
            {
                this.map = (IDictionary)data;
                this.InternalFireDataChanged(this.map, changeEvent);
                //清理无用外键
                this.InternalCacheAndTrimExcess();
                //遍历所有字段
                map.WritableForEachDictionary((map_e) =>
                {
                    InternalSetMappingField(map_e.Key, map_e.Value, false);
                });
            }
            else
            {
                //清理数据，删除所有外键
                this.map = null;
                this.InternalFireDataChanged(this.map, changeEvent);
                this.InternalCacheAndTrimExcess();
                // Null Null 表示干掉自己
                base.InternalEnqueueBatch(new LinkedListNode<ObjectUpdateEntry>(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY)));
            }
        }
        protected IMappingNode InternalGetMappingField(object fieldKey)
        {
            if (subMapping.TryGetValue(fieldKey, out var sub))
            {
                return sub.MappingNode;
            }
            return null;
        }
        protected IMappingNode InternalSetMappingField(object fieldKey, object fieldValue, bool changeEvent)
        {
            this.flushDirty = true;
            if (fieldValue != null)
            {
                //获取当前外键索引
                var sub = subMapping.GetOrAdd(fieldKey, _ =>
                {
                    return InternalCreateFieldMapping(fieldKey); //new FieldMapping<object>(fieldKey, base.CreateSubMapping(save_key, fieldValue.GetType()), InternalOnFieldDataChanged);
                });
                if (isSubMapping)
                {
                    //存储外键
                    sub.Mapping.InternalSetData(fieldValue, changeEvent);
                    // value 为外键索引，添加到预提交列表
                    sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, sub.Mapping.Key);
                }
                else
                {
                    InternalUpdateWrapper(sub, fieldValue);
                    sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, fieldValue);
                }
                base.InternalEnqueueBatch(sub.Batch);
                return sub.MappingNode;
            }
            else
            {
                var sub = subMapping.RemoveByKey(fieldKey);
                if (sub != null)
                {
                    if (isSubMapping)
                    {
                        //存储外键
                        //sub.Mapping.InternalSetData(null, changeEvent);
                        base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY, sub.Mapping.Key));
                    }
                    else
                    {
                        InternalUpdateWrapper(sub, fieldValue);
                    }
                    // value 为外键索引，添加到预提交列表
                    sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                    base.InternalEnqueueBatch(sub.Batch);
                }
                return null;
            }
        }
        protected bool InternalRemoveMappingField(object fieldKey, bool changeEvent)
        {
            this.flushDirty = true;
            //获取当前外键索引
            var sub = subMapping.RemoveByKey(fieldKey);
            if (sub != null)
            {
                if (isSubMapping)
                {
                    //存储外键
                    //sub.Mapping.InternalSetData(null, changeEvent);
                    base.InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY, sub.Mapping.Key));
                }
                //                 else
                //                 {
                //                     //InternalUpdateWrapper(sub, null);
                //                 }
                // value 为外键索引，添加到预提交列表
                sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName));
                base.InternalEnqueueBatch(sub.Batch);
                return true;
            }
            else
            {
                //var save_key = InternalObjectKeyToString(fieldKey);
                //sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, save_key));
                //base.InternalEnqueueBatch(sub.Batch);
                return false;
            }
        }
        protected IMappingNode InternalCacheAndPutField(object fieldKey, object fieldValue)
        {
            this.flushDirty = true;
            if (map == null)
            {
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.map, true);
            }
            this.map[fieldKey] = fieldValue;
            return InternalSetMappingField(fieldKey, fieldValue, false);
        }
        protected IMappingNode InternalCacheAndAddField(object fieldKey, object fieldValue)
        {
            this.flushDirty = true;
            if (map == null)
            {
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.map, true);
            }
            this.map.Add(fieldKey, fieldValue);
            return InternalSetMappingField(fieldKey, fieldValue, false);
        }
        protected bool InternalCacheAndRemove(object fieldKey)
        {
            this.flushDirty = true;
            if (map != null)
            {
                map.Remove(fieldKey);
                return InternalRemoveMappingField(fieldKey, false);
            }
            return false;
        }
        protected void InternalCacheAndTrimExcess()
        {
            this.flushDirty = true;
            if (map != null && map.Count > 0)
            {
                //连同删除外键
                var sub_keys = new List<object>(subMapping.Keys);
                {
                    foreach (var vkey in sub_keys)
                    {
                        if (!map.Contains(vkey))
                        {
                            InternalRemoveMappingField(vkey, false);
                        }
                    }
                }
            }
            else
            {
                //清理外键
                foreach (var sub in subMapping.Values)
                {
                    if (sub.Mapping != null)
                    {
                        //sub.Mapping.InternalSetData(null, false);
                        InternalEnqueueBatch(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY, sub.Mapping.Key));
                    }
                }
                subMapping.Clear();
            }
        }
        protected void InternalCacheAndClear()
        {
            this.flushDirty = true;
            InternalClearBatch();
            if (this.map != null)
            {
                map.Clear();
            }
            //清理无用外键
            this.InternalCacheAndTrimExcess();
        }

        public IMappingNode SetFieldDirty(object key)
        {
            if (MapData != null)
            {
                return InternalSetMappingField(key, MapData[key], false);
            }
            return null;
        }
    }

    public class MappingList : MappingObject
    {
        public const string F_COUNT_FIELD_NAME = ".count";
        private bool flushDirty = false;
        private int currentCount = 0;
        private readonly Type listType;
        private readonly Type elementType;
        private readonly bool isSubMapping;
        private readonly FieldMappingList<FieldMapping<int>> fieldMappings;
        private IList list;

        public override object Data { get => list; }
        public Type ListType { get => listType; }
        public Type ElementType { get => elementType; }
        public IList ListData { get => list; }
        public int ListCount { get => list != null ? list.Count : 0; }
        //public bool IsElementMapping { get => isElementMapping; }

        protected internal MappingList(string key, Type listType, Type elementType, ITaskExecutor exe, IMappingAdapter db)
            : this(key, listType, elementType, (string.Format("List<{0}>:{1}", elementType.Name, key)), exe, db)
        {
        }
        protected MappingList(string key, Type listType, Type elementType, string log, ITaskExecutor exe, IMappingAdapter db)
           : base(key, log, exe, db)
        {
            this.listType = listType;
            this.elementType = elementType;
            this.isSubMapping = MappingConverter.Instance.IsMappingObject(elementType);
            this.fieldMappings = new FieldMappingList<FieldMapping<int>>(this);
        }
        protected override void CleanUp()
        {
            base.CleanUp();
            this.fieldMappings.Clear();
        }
        protected override object CreateInstance()
        {
            this.list = ReflectionUtil.CreateGenericArrayList(listType.GetGenericArguments());
            return list;
        }
        protected virtual FieldMapping<int> InternalCreateFieldMapping(int index)
        {
            if (isSubMapping)
            {
                return new FieldMapping<int>(index, index.ToString(), this.CreateSubMapping(index.ToString(), elementType), InternalFieldChanged);
            }
            else
            {
                return new FieldMapping<int>(index, index.ToString(), this.CreateSubWrapper(index.ToString(), elementType), InternalWrapperChanged);
            }
        }
        private void InternalFieldChanged(FieldMapping<int> field, object fieldValue)
        {
            if (this.list == null)
            {
                this.flushDirty = true;
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.list, true);
            }
            list[field.Field] = fieldValue;
        }
        private void InternalWrapperChanged(FieldMapping<int> field, IWrapper wrapper)
        {
            if (field.Wrapper == wrapper)
            {
                var sub_wrapper = field.Wrapper;
                if (this.list == null || this.list.Count == 0)
                {
                    this.flushDirty = true;
                    base.InternalClearBatch();
                    if (this.list == null) this.CreateInstance();
                    this.InternalFireDataChanged(this.list, true);
                }
                else
                {
                    var fieldValue = sub_wrapper.Data;
                    list[field.Field] = fieldValue;
                    if (fieldValue != null)
                    {
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, field.HashName, fieldValue));
                    }
                    else
                    {
                        field.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, field.HashName));
                    }
                    base.InternalEnqueueBatch(field.Batch);
                }
            }
        }
        private void InternalUpdateWrapper(FieldMapping<int> field, object fieldValue)
        {
            if (field.Wrapper != null)
            {
                field.Wrapper.RunWithNoEvent(() =>
                {
                    field.Wrapper.Data = fieldValue;
                });
            }
        }
        internal override void InternalForEachSubFields(Action<FieldMapping> action)
        {
            foreach (var sub in fieldMappings.Values)
            {
                action(sub);
            }
        }
        internal override async Task<object> InternalLoadDataAsync(IMappingDatabase db, HashMap<string, ObjectQueryEntry[]> batch, bool changeEvent)
        {
            base.InternalClearBatch();
            this.flushDirty = false;
            ObjectQueryEntry[] entries = null;
            if (batch == null || !batch.TryGetValue(this.Key, out entries))
            {
                //获取所有Entry
                entries = await db.ObjectHashQueryEntriesAsync(this.Key);
            }
            //var entries = await db.ObjectHashQueryEntriesAsync(this.Key);
            if (entries != null && entries.Length > 0 && entries.TryFind(e => e.FieldName.ToString() == F_COUNT_FIELD_NAME, out var e_count))
            {
                var count = ORMFactory.Instance.DecodeObject<int>(e_count.FieldValue);
                this.CreateInstance();
                this.InternalFireDataChanged(this.list, changeEvent);
                this.InternalListSetSize(count);
                List<ObjectUpdateEntry> updating = null;
                {
                    foreach (var e in entries)
                    {
                        var fname = e.FieldName.ToString();
                        if (!fname.StartsWith("."))
                        {
                            if (fname.StartsWith(F_KEY_SUBMAPPING)) { fname = fname.Substring(F_KEY_SUBMAPPING.Length); }
                            int index = Parser.ParseInt(fname);
                            if (index < count)
                            {
                                await InternalLoadFieldAsync(db, index, e.FieldValue, batch);
                            }
                            else if (!IsReadOnly)
                            {
                                log.WarnFormat("{0} : Delete Not Exist List Index : {0}", this.DisplayName, e.FieldName);
                                if (updating == null) updating = new List<ObjectUpdateEntry>();
                                updating.Add(new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, e.FieldName.ToString()));
                            }
                        }
                    }
                    //清除不存在元素
                    if (updating != null)
                    {
                        await db.ObjectHashBatchUpdateAsync(this.Key, updating);
                    }
                }
                return list;
            }
            else
            {
                list = null;
                this.InternalFireDataChanged(this.list, changeEvent);
                InternalListSetSize(0);
            }
            return null;
        }
        internal async Task InternalLoadFieldAsync(IMappingDatabase db, int index, IConvertible fieldValue, HashMap<string, ObjectQueryEntry[]> batch)
        {
            var sub = fieldMappings[index];
            if (isSubMapping)
            {
                // fieldValue 为外键索引
                if (!string.IsNullOrEmpty(fieldValue.ToString()))
                {
                    var sub_data = await sub.Mapping.InternalLoadDataAsync(db, batch, false);
                    list[index] = sub_data;
                }
                else
                {
                    // fieldValue 为空
                    list[index] = null;
                }
            }
            else
            {
                var sub_data = ORMFactory.Instance.DecodeObject(fieldValue, elementType);
                InternalUpdateWrapper(sub, sub_data);
                list[index] = sub_data;
            }
        }
        internal async Task<object> InternalLoadFieldAsync(IMappingDatabase db, int index, HashMap<string, ObjectQueryEntry[]> batch)
        {
            if (list != null)
            {
                var sub = fieldMappings[index];
                if (isSubMapping)
                {
                    var fieldValue = await sub.Mapping.InternalLoadDataAsync(db, batch, false);
                    list[index] = fieldValue;
                    return fieldValue;
                }
                else
                {
                    IConvertible fieldValue;
                    if (batch != null && batch.TryGetValue(this.Key, out var entries) && entries.TryFind(e => e.FieldName.ToString() == sub.HashName, out var entry))
                    {
                        fieldValue = entry.FieldValue;
                    }
                    else
                    {
                        fieldValue = await db.ObjectHashQueryEntryAsync(this.Key, sub.HashName);
                    }
                    //var fieldValue = await db.ObjectHashQueryEntryAsync(this.Key, sub.Field.ToString());
                    var sub_data = ORMFactory.Instance.DecodeObject(fieldValue, elementType);
                    InternalUpdateWrapper(sub, sub_data);
                    list[index] = sub_data;
                    return sub_data;
                }
            }
            return null;
        }

        internal override void InternalSaveData(IObjectTransaction db, object data, bool updateChange)
        {
            if (this.list == null && data == null)
            {
                //原本没有加载过，如果直接删除Key，会导致子链断裂//
                return;
            }
            this.InternalClearBatch();
            this.flushDirty = false;
            if (data != null)
            {
                this.list = (IList)data;
                this.InternalFireDataChanged(this.list, updateChange);
                this.InternalSaveTrimExcess(db, list.Count);
                this.InternalListSetSize(list.Count);
                var updating = new List<ObjectUpdateEntry>(list.Count + 1);
                {
                    updating.Add(new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_COUNT_FIELD_NAME, list.Count));
                    //遍历所有字段
                    for (int i = 0; i < list.Count; i++)
                    {
                        updating.Add(InternalSaveField(db, i));
                    }
                    db.Database.EnqueueHashBatchUpdate(db, this.Key, updating);
                }
            }
            else
            {
                //清理数据，删除所有外键
                this.list = null;
                this.InternalFireDataChanged(this.list, updateChange);
                this.InternalSaveTrimExcess(db, 0);
                this.InternalListSetSize(0);
                db.Enqueue(db.Database.ObjectHashUpdateAsync(this.Key, new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY)));
            }
        }
        internal ObjectUpdateEntry InternalSaveField(IObjectTransaction db, int index)
        {
            var fieldValue = list[index];
            //获取当前外键索引
            var sub = fieldMappings[index];
            if (isSubMapping)
            {
                //存储外键
                sub.Mapping.InternalSaveData(db, fieldValue, false);
                if (fieldValue != null)
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, sub.Mapping.Key);
                }
                else
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                }
            }
            else
            {
                InternalUpdateWrapper(sub, fieldValue);
                if (fieldValue != null)
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, fieldValue);
                }
                else
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    return new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                }
            }
        }
        protected override bool InternalFlushBegin(IObjectTransaction db, IList<ObjectUpdateEntry> events)
        {
            if (flushDirty)
            {
                flushDirty = false;
                if (Data == null)
                {
                    events.Insert(0, new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY));
                }
                else
                {
                    events.Insert(0, new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, F_COUNT_FIELD_NAME, currentCount));
                }
            }
            return true;
        }
        internal void InternalSaveTrimExcess(IObjectTransaction db, int count)
        {
            //删除多余的外键内容
            if (isSubMapping)
            {
                for (int i = count; i < fieldMappings.Count; ++i)
                {
                    var subkey = fieldMappings[i];
                    subkey.Mapping.InternalSaveData(db, null, false);
                }
            }
            else
            {
                for (int i = count; i < fieldMappings.Count; ++i)
                {
                    var subkey = fieldMappings[i];
                    InternalUpdateWrapper(subkey, null);
                }
            }
        }

        internal override void InternalSetData(object data, bool changeEvent)
        {
            base.InternalClearBatch();
            this.flushDirty = true;
            if (data != null)
            {
                this.list = (IList)data;
                this.InternalFireDataChanged(this.list, changeEvent);
                this.InternalCacheAndTrimExcess(list.Count);
                //遍历所有字段
                for (int i = 0; i < list.Count; i++)
                {
                    InternalSetMappingField(i, list[i], false);
                }
            }
            else
            {
                this.list = null;
                this.InternalFireDataChanged(this.list, changeEvent);
                this.InternalCacheAndTrimExcess(0);
                // Null Null 表示干掉自己
                base.InternalEnqueueBatch(new LinkedListNode<ObjectUpdateEntry>(new ObjectUpdateEntry(ExecuteEvent.DELETE_KEY)));
            }
        }
        protected IMappingNode InternalGetMappingField(int index)
        {
            return fieldMappings[index].Mapping;
        }
        protected IMappingNode InternalSetMappingField(int index, object fieldValue, bool changeEvent)
        {
            //获取当前外键索引
            var sub = fieldMappings[index];
            if (isSubMapping)
            {
                sub.Mapping.InternalSetData(fieldValue, changeEvent);
                if (fieldValue != null)
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, sub.Mapping.Key));
                }
                else
                {
                    // fieldValue 为外键索引，添加到预提交列表
                    sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName));
                }
            }
            else
            {
                InternalUpdateWrapper(sub, fieldValue);
                if (fieldValue != null)
                {
                    sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, fieldValue));
                }
                else
                {
                    sub.Batch.Value = (new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName));
                }
            }
            base.InternalEnqueueBatch(sub.Batch);
            return sub.MappingNode;
        }
        protected IMappingNode InternalCacheAndSetField(int index, object fieldValue)
        {
            if (list != null && index < list.Count)
            {
                list[index] = fieldValue;
                return this.InternalSetMappingField(index, fieldValue, false);
            }
            return null;
        }
        protected bool InternalCacheAndTrimExcess(int count)
        {
            //删除多余的外键内容
            if (isSubMapping)
            {
                for (int i = count; i < fieldMappings.Count; ++i)
                {
                    var sub = fieldMappings[i];
                    sub.Mapping.InternalSetData(null, false);
                    sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                    base.InternalEnqueueBatch(sub.Batch);
                }
            }
            else
            {
                for (int i = count; i < fieldMappings.Count; ++i)
                {
                    var sub = fieldMappings[i];
                    InternalUpdateWrapper(sub, null);
                    sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, sub.HashName);
                    base.InternalEnqueueBatch(sub.Batch);
                }
            }
            if (this.InternalListSetSize(count))
            {
                return true;
            }
            return false;
        }
        protected IMappingNode InternalCacheAndAdd(object fieldValue)
        {
            int index = InternalListAdd(fieldValue);
            return InternalCacheAndSetField(index, fieldValue);
        }
        protected IMappingNode InternalCacheAndInsert(int index, object fieldValue)
        {
            InternalListInsert(index, fieldValue);
            return InternalCacheAndSetField(index, fieldValue);
        }
        protected IMappingNode InternalCacheAndRemove(int index)
        {
            if (InternalListRemove(index, out var fieldValue, out var sub))
            {
                if (isSubMapping)
                {
                    sub.Mapping.InternalSetData(null, false);
                }
                else
                {
                    InternalUpdateWrapper(sub, null);
                }
                //sub.batch.Value = new ObjectUpdateEntry(ExecuteEvent.DELETE_FIELD, index.ToString());
                // fieldValue 为外键索引，添加到预提交列表
                //base.InternalEnqueueBatch(sub.batch);
                return sub.MappingNode;
            }
            return null;
        }
        protected void InternalCacheAndClear()
        {
            currentCount = 0;
            InternalClearBatch();
            if (list != null)
            {
                list.Clear();
                InternalCacheAndTrimExcess(list.Count);
            }
        }
        protected int InternalListAdd(object element)
        {
            this.flushDirty = true;
            if (this.list == null)
            {
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.list, true);
            }
            int i = list.Count;
            InternalListInsert(i, element);
            return i;
        }

        private bool InternalListSetSize(int count)
        {
            this.flushDirty = true;
            var ret = count != currentCount;
            currentCount = count;
            if (list != null && list.Count != count)
            {
                this.flushDirty = true;
                CUtils.SetListLength(list, count, i => elementType.IsClass ? null : DeepActivator.CreateInstance(elementType));
            }
            if (fieldMappings.Count != count)
            {
                fieldMappings.SetSize(count, i => InternalCreateFieldMapping(i));
            }
            return ret;
        }

        private bool InternalListInsert(int i, object element)
        {
            this.flushDirty = true;
            if (list == null)
            {
                base.InternalClearBatch();
                this.CreateInstance();
                this.InternalFireDataChanged(this.list, true);
                this.currentCount = list.Count;
            }
            if (i <= list.Count)
            {
                list.Insert(i, element);
                var sub = InternalCreateFieldMapping(i);// new FieldMapping<int>(i, isElementMapping ? base.CreateSubMapping(i.ToString(), elementType) : null, InternalSetSubField);
                fieldMappings.Insert(i, sub);
                currentCount = list.Count;
                if (i < (list.Count - 1))
                {
                    InternalReIndexFrom(i + 1);
                }
                return true;
            }
            return false;
        }
        private bool InternalListRemove(int index, out object fieldValue, out FieldMapping<int> subMapping)
        {
            if (list != null && index < list.Count)
            {
                this.flushDirty = true;
                var reIndex = index < list.Count - 1;
                fieldValue = list[index];
                subMapping = this.fieldMappings[index];
                list.RemoveAt(index);
                this.fieldMappings.Remove(index);
                currentCount = list.Count;
                if (reIndex)
                {
                    InternalReIndexFrom(index);
                }
                return true;
            }
            else
            {
                fieldValue = null;
                subMapping = null;
                currentCount = 0;
                return false;
            }
        }
        private void InternalReIndexFrom(int index)
        {
            for (int i = index; i < fieldMappings.Count; i++)
            {
                var sub = fieldMappings[i];
                sub.ReIndex(i, i.ToString());
                if (sub.Mapping != null)
                {
                    sub.Mapping.InternalRename(base.GetSubMappingName(i.ToString(), elementType));
                }
                var oldb = sub.Batch.Value;
                sub.Batch.Value = new ObjectUpdateEntry(ExecuteEvent.UPDATE_FIELD, sub.HashName, oldb.FieldValue);
                base.InternalEnqueueBatch(sub.Batch);
            }
        }

        public IMappingNode SetFieldDirty(int index)
        {
            if (ListData != null)
            {
                return InternalSetMappingField(index, ListData[index], false);
            }
            return null;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
#if ARRAY_MAPPING

    public class MappingArray : MappingList
    {
        private readonly Type arrayType;
        private Array array;

        public override object Data
        {
            get => InternalGenArrayData(true);
        }
        public Array ArrayData
        {
            get { return InternalGenArrayData(true); }
        }
        public int ArrayLength { get => base.ListCount; }
        public Type ArrayType { get => arrayType; }

        public MappingArray(string key, Type arrayType, ITaskExecutor exe, IMappingAdapter db)
            : base(key, typeof(ArrayList), arrayType.GetElementType(), LoggerFactory.GetLogger(string.Format("{0}[]:{1}", arrayType.GetElementType().Name, key)), exe, db)
        {
            this.arrayType = arrayType;
        }

        internal override Task<object> InternalLoadDataAsync(IMappingDatabase db, bool fireEvent)
        {
            return base.InternalLoadDataAsync(db, false).ContinueWith(t =>
            {
                return (object)InternalGenArrayData(fireEvent);
            });
        }

        internal override void InternalSaveData(IObjectTransaction db, object data, bool fireEvent)
        {
            this.array = (Array)data;
            this.InternalFireDataChanged(array, fireEvent);
            if (data != null)
            {
                var list = new ArrayList();
                list.AddRange(array);
                base.InternalSaveData(db, list, false);
            }
            else
            {
                base.InternalSaveData(db, null, false);
            }
        }

        internal override void InternalSetData(object data, bool fireEvent)
        {
            this.array = (Array)data;
            this.InternalFireDataChanged(array, fireEvent);
            if (data != null)
            {
                var list = new ArrayList();
                list.AddRange(array);
                base.InternalSetData(list, false);
            }
            else
            {
                base.InternalSetData(null, false);
            }
        }
        private Array InternalGenArrayData(bool fireEvent)
        {
            if (base.ListData == null)
            {
                this.array = null;
                this.InternalFireDataChanged(null, fireEvent);
                return null;
            }
            else if (array == null || array.Length != base.ListCount)
            {
                this.array = (Array)Array.CreateInstance(ElementType, base.ListCount);
                this.InternalFireDataChanged(array, fireEvent);
            }
            base.ListData.CopyTo(array, 0);
            return array;
        }
    }
#endif
    //----------------------------------------------------------------------------------------------------------------------------------------------------------



    //----------------------------------------------------------------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------------------------------------------------------------

}
