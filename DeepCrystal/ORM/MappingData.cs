using DeepCore.Reflection;
using DeepCore.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DeepCrystal.ORM
{

    [Reflectible]
    public abstract class IMappingData : ORMObject
    {
        protected readonly string key;
        protected readonly IMappingAdapter adapter;
        protected readonly IMappingDatabase database;
        protected readonly ITaskExecutor executor;
        private IMappingLocker locker;
        public string Key { get => key; }
        public IMappingAdapter Adapter { get => adapter; }
        public IMappingDatabase Database { get => database; }
        public IMappingData(string key, IMappingAdapter ad, ITaskExecutor exe)
        {
            this.key = key;
            this.adapter = ad;
            this.database = ad.CreateDatabase();
            this.executor = exe ?? ITaskExecutor.Default;
        }
        public Task<bool> EnterLockAsync(out string token)
        {
            if (this.locker == null)
            {
                this.locker = adapter.CreateExecutablLocker(key + "._lock", executor);
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
#if ORM_SYNC
        public bool EnterLock(out string token)
        {
            if (this.locker == null)
            {
                this.locker = adapter.CreateExecutablLocker(key + "._lock", executor);
            }
            return locker.EnterLock(out token);
        }
        public bool ExitLock(string token)
        {
            return locker.ExitLock(token);
        }
        public TResult LockCourse<TResult>(Func<TResult> action)
        {
            var ret = default(TResult);
            if (this.EnterLock(out var token))
            {
                try
                {
                    ret = action();
                }
                finally
                {
                    this.ExitLock(token);
                }
            }
            return ret;
        }
        public void LockCourse(Func<Task> action)
        {
            if (this.EnterLock(out var token))
            {
                try
                {
                    action();
                }
                finally
                {
                    this.ExitLock(token);
                }
            }
        }
#endif
    }

    public class IMappingHash : IMappingData
    {
        public IMappingHash(string key, IMappingAdapter db, ITaskExecutor exe) : base(key, db, exe)
        {
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        #region Async
        public Task<long> DecrementAsync(string hashField, long value = 1)
        {
            return executor.Execute(database.HashDecrementAsync(base.key, hashField, value));
        }
        public Task<double> DecrementAsync(string hashField, double value)
        {
            return executor.Execute(database.HashDecrementAsync(base.key, hashField, value));
        }
        public Task<bool> DeleteAsync(string hashField)
        {
            return executor.Execute(database.HashDeleteAsync(base.key, hashField));
        }
        public Task<long> DeleteAsync(string[] hashFields)
        {
            return executor.Execute(database.HashDeleteAsync(base.key, hashFields));
        }
        public Task<bool> ExistsAsync(string hashField)
        {
            return executor.Execute(database.HashExistsAsync(base.key, hashField));
        }
        public Task<HashQueryEntry[]> GetAllAsync()
        {
            return executor.Execute(database.HashGetAllAsync(base.key));
        }
        public Task<IConvertible> GetAsync(string hashField)
        {
            return executor.Execute(database.HashGetAsync(base.key, hashField));
        }
        public Task<IConvertible[]> GetAsync(string[] hashFields)
        {
            return executor.Execute(database.HashGetAsync(base.key, hashFields));
        }
        public Task<double> IncrementAsync(string hashField, double value)
        {
            return executor.Execute(database.HashIncrementAsync(base.key, hashField, value));
        }
        public Task<long> IncrementAsync(string hashField, long value = 1)
        {
            return executor.Execute(database.HashIncrementAsync(base.key, hashField, value));
        }
        public Task<string[]> KeysAsync()
        {
            return executor.Execute(database.HashKeysAsync(base.key));
        }
        public Task<long> LengthAsync()
        {
            return executor.Execute(database.HashLengthAsync(base.key));
        }
        public Task SetAsync(HashUpdateEntry[] hashFields)
        {
            return executor.Execute(database.HashSetAsync(base.key, hashFields));
        }
        public Task<bool> SetAsync(string hashField, object value, When when = When.Always)
        {
            return executor.Execute(database.HashSetAsync(base.key, hashField, value, when));
        }
        public Task<IConvertible[]> ValuesAsync()
        {
            return executor.Execute(database.HashValuesAsync(base.key));
        }
        public IAsyncEnumerable<HashQueryEntry> ScanAsync(string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
        {
            return database.HashScanAsync(base.key, pattern, pageSize, cursor, pageOffset);
        }

        //         public async Task<HashQueryEntry<T>[]> GetAllAsync<T>()
        //         {
        //             var all = await GetAllAsync();
        //             if (all != null)
        //             {
        //                 return Array.ConvertAll(all, item => new HashQueryEntry<T>(item.FieldName, ORMFactory.Instance.DecodeObject<T>(item.FieldValue)));
        //             }
        //             return [];
        //         }
        //         public async Task<T> GetAsync<T>(string hashField)
        //         {
        //             var kv = await GetAsync(hashField);
        //             if (kv != null)
        //             {
        //                 return ORMFactory.Instance.DecodeObject<T>(kv);
        //             }
        //             return default;
        //         }
        //         public async Task<T[]> GetAsync<T>(string[] hashFields)
        //         {
        //             var all = await GetAsync(hashFields);
        //             if (all != null)
        //             {
        //                 return Array.ConvertAll(all, item => ORMFactory.Instance.DecodeObject<T>(item));
        //             }
        //             return [];
        //         }

        #endregion
#if ORM_SYNC
        #region Sync
        public long Decrement(string hashField, long value = 1)
        {
            return database.HashDecrement(base.key, hashField, value);
        }
        public double Decrement(string hashField, double value)
        {
            return database.HashDecrement(base.key, hashField, value);
        }
        public bool Delete(string hashField)
        {
            return database.HashDelete(base.key, hashField);
        }
        public long Delete(string[] hashFields)
        {
            return database.HashDelete(base.key, hashFields);
        }
        public bool Exists(string hashField)
        {
            return database.HashExists(base.key, hashField);
        }
        public HashQueryEntry[] GetAll()
        {
            return database.HashGetAll(base.key);
        }
        public IConvertible Get(string hashField)
        {
            return database.HashGet(base.key, hashField);
        }
        public IConvertible[] Get(string[] hashFields)
        {
            return database.HashGet(base.key, hashFields);
        }
        public double Increment(string hashField, double value)
        {
            return database.HashIncrement(base.key, hashField, value);
        }
        public long Increment(string hashField, long value = 1)
        {
            return database.HashIncrement(base.key, hashField, value);
        }
        public string[] Keys()
        {
            return database.HashKeys(base.key);
        }
        public long Length()
        {
            return database.HashLength(base.key);
        }
        public void Set(HashUpdateEntry[] hashFields)
        {
            database.HashSet(base.key, hashFields);
        }
        public bool Set(string hashField, object value, When when = When.Always)
        {
            return database.HashSet(base.key, hashField, value, when);
        }
        public IConvertible[] Values()
        {
            return database.HashValues(base.key);
        }
        public IEnumerable<HashQueryEntry> Scan(string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
        {
            return database.HashScan(base.key, pattern, pageSize, cursor, pageOffset);
        }

        //         public HashQueryEntry<T>[] GetAll<T>()
        //         {
        //             var all = GetAll();
        //             if (all != null)
        //             {
        //                 return Array.ConvertAll(all, item => new HashQueryEntry<T>(item.FieldName, ORMFactory.Instance.DecodeObject<T>(item.FieldValue)));
        //             }
        //             return [];
        //         }
        //         public T Get<T>(string hashField)
        //         {
        //             var kv = Get(hashField);
        //             if (kv != null)
        //             {
        //                 return ORMFactory.Instance.DecodeObject<T>(kv);
        //             }
        //             return default;
        //         }
        //         public T[] Get<T>(string[] hashFields)
        //         {
        //             var all = Get(hashFields);
        //             if (all != null)
        //             {
        //                 return Array.ConvertAll(all, item => ORMFactory.Instance.DecodeObject<T>(item));
        //             }
        //             return [];
        //         }

        #endregion
#endif
    }

    public class IMappingString : IMappingData
    {
        public IMappingString(string key, IMappingAdapter db, ITaskExecutor exe) : base(key, db, exe)
        {
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        #region Async
        public Task<long> AppendAsync(object value)
        {
            return executor.Execute(database.StringAppendAsync(key, value));
        }
        public Task<long> BitCountAsync(long start = 0, long end = -1)
        {
            return executor.Execute(database.StringBitCountAsync(key, start, end));
        }
        public Task<long> BitPositionAsync(bool bit, long start = 0, long end = -1)
        {
            return executor.Execute(database.StringBitPositionAsync(key, bit, start, end));
        }
        public Task<long> DecrementAsync(long value = 1)
        {
            return executor.Execute(database.StringDecrementAsync(key, value));
        }
        public Task<double> DecrementAsync(double value)
        {
            return executor.Execute(database.StringDecrementAsync(key, value));
        }
        public Task<bool> GetBitAsync(long offset)
        {
            return executor.Execute(database.StringGetBitAsync(key, offset));
        }
        public Task<IConvertible> GetRangeAsync(long start, long end)
        {
            return executor.Execute(database.StringGetRangeAsync(key, start, end));
        }
        public Task<IConvertible> GetAsync()
        {
            return executor.Execute(database.StringGetAsync(key));
        }
        public Task<IConvertible> GetSetAsync(object value)
        {
            return executor.Execute(database.StringGetSetAsync(key, value));
        }
        public Task<long> IncrementAsync(long value = 1)
        {
            return executor.Execute(database.StringIncrementAsync(key, value));
        }
        public Task<double> IncrementAsync(double value)
        {
            return executor.Execute(database.StringIncrementAsync(key, value));
        }
        public Task<long> LengthAsync()
        {
            return executor.Execute(database.StringLengthAsync(key));
        }
        public Task<bool> SetAsync(object value, When when = When.Always)
        {
            return executor.Execute(database.StringSetAsync(key, value, when));
        }
        public Task<bool> SetBitAsync(long offset, bool bit)
        {
            return executor.Execute(database.StringSetBitAsync(key, offset, bit));
        }
        public Task<long> SetRangeAsync(long offset, object value)
        {
            return executor.Execute(database.StringSetRangeAsync(key, offset, value));
        }
        #endregion
#if ORM_SYNC
        #region Sync
        public long Append(object value)
        {
            return database.StringAppend(key, value);
        }
        public long BitCount(long start = 0, long end = -1)
        {
            return database.StringBitCount(key, start, end);
        }
        public long BitPosition(bool bit, long start = 0, long end = -1)
        {
            return database.StringBitPosition(key, bit, start, end);
        }
        public long Decrement(long value = 1)
        {
            return database.StringDecrement(key, value);
        }
        public double Decrement(double value)
        {
            return database.StringDecrement(key, value);
        }
        public bool GetBit(long offset)
        {
            return database.StringGetBit(key, offset);
        }
        public IConvertible GetRange(long start, long end)
        {
            return database.StringGetRange(key, start, end);
        }
        public IConvertible Get()
        {
            return database.StringGet(key);
        }
        public IConvertible GetSet(object value)
        {
            return database.StringGetSet(key, value);
        }
        public long Increment(long value = 1)
        {
            return database.StringIncrement(key, value);
        }
        public double Increment(double value)
        {
            return database.StringIncrement(key, value);
        }
        public long Length()
        {
            return database.StringLength(key);
        }
        public bool Set(object value, When when = When.Always)
        {
            return database.StringSet(key, value, when);
        }
        public bool SetBit(long offset, bool bit)
        {
            return database.StringSetBit(key, offset, bit);
        }
        public long SetRange(long offset, object value)
        {
            return database.StringSetRange(key, offset, value);
        }
        #endregion
#endif
    }

    public class IMappingSet : IMappingData
    {
        public IMappingSet(string key, IMappingAdapter db, ITaskExecutor exe) : base(key, db, exe)
        {
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        #region Async
        public Task<bool> AddAsync(object value)
        {
            return executor.Execute(database.SetAddAsync(key, value));
        }
        public Task<long> AddAsync(object[] values)
        {
            return executor.Execute(database.SetAddAsync(key, values));
        }
        public Task<bool> ContainsAsync(object value)
        {
            return executor.Execute(database.SetContainsAsync(key, value));
        }
        public Task<long> LengthAsync()
        {
            return executor.Execute(database.SetLengthAsync(key));
        }
        public Task<IConvertible[]> MembersAsync()
        {
            return executor.Execute(database.SetMembersAsync(key));
        }
        public Task<IConvertible> PopAsync()
        {
            return executor.Execute(database.SetPopAsync(key));
        }
        public Task<IConvertible> RandomMemberAsync()
        {
            return executor.Execute(database.SetRandomMemberAsync(key));
        }
        public Task<IConvertible[]> RandomMembersAsync(long count)
        {
            return executor.Execute(database.SetRandomMembersAsync(key, count));
        }
        public Task<bool> RemoveAsync(object value)
        {
            return executor.Execute(database.SetRemoveAsync(key, value));
        }
        public Task<long> RemoveAsync(object[] values)
        {
            return executor.Execute(database.SetRemoveAsync(key, values));
        }
        #endregion
#if ORM_SYNC
        #region Sync
        public bool Add(object value)
        {
            return database.SetAdd(key, value);
        }
        public long Add(object[] values)
        {
            return database.SetAdd(key, values);
        }
        public bool Contains(object value)
        {
            return database.SetContains(key, value);
        }
        public long Length()
        {
            return database.SetLength(key);
        }
        public IConvertible[] Members()
        {
            return database.SetMembers(key);
        }
        public IConvertible Pop()
        {
            return database.SetPop(key);
        }
        public IConvertible RandomMember()
        {
            return database.SetRandomMember(key);
        }
        public IConvertible[] RandomMembers(long count)
        {
            return database.SetRandomMembers(key, count);
        }
        public bool Remove(object value)
        {
            return database.SetRemove(key, value);
        }
        public long Remove(object[] values)
        {
            return database.SetRemove(key, values);
        }
        #endregion
#endif
    }

    public class IMappingList : IMappingData
    {
        public IMappingList(string key, IMappingAdapter db, ITaskExecutor exe) : base(key, db, exe)
        {
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        #region Async
        public Task<IConvertible> GetByIndexAsync(long index)
        {
            return executor.Execute(database.ListGetByIndexAsync(key, index));
        }
        public Task<long> InsertAfterAsync(object pivot, object value)
        {
            return executor.Execute(database.ListInsertAfterAsync(key, pivot, value));
        }
        public Task<long> InsertBeforeAsync(object pivot, object value)
        {
            return executor.Execute(database.ListInsertBeforeAsync(key, pivot, value));
        }
        public Task<IConvertible> LeftPopAsync()
        {
            return executor.Execute(database.ListLeftPopAsync(key));
        }
        public Task<long> LeftPushAsync(object[] values)
        {
            return executor.Execute(database.ListLeftPushAsync(key, values));
        }
        public Task<long> LeftPushAsync(object value, When when = When.Always)
        {
            return executor.Execute(database.ListLeftPushAsync(key, value, when));
        }
        public Task<long> LengthAsync()
        {
            return executor.Execute(database.ListLengthAsync(key));
        }
        public Task<IConvertible[]> RangeAsync(long start = 0, long stop = -1)
        {
            return executor.Execute(database.ListRangeAsync(key, start, stop));
        }
        public Task<long> RemoveAsync(object value, long count = 0)
        {
            return executor.Execute(database.ListRemoveAsync(key, value, count));
        }
        public Task<IConvertible> RightPopAsync()
        {
            return executor.Execute(database.ListRightPopAsync(key));
        }
        public Task<long> RightPushAsync(object value, When when = When.Always)
        {
            return executor.Execute(database.ListRightPushAsync(key, value, when));
        }
        public Task<long> RightPushAsync(object[] values)
        {
            return executor.Execute(database.ListRightPushAsync(key, values));
        }
        public Task SetByIndexAsync(long index, object value)
        {
            return executor.Execute(database.ListSetByIndexAsync(key, index, value));
        }
        public Task TrimAsync(long start, long stop)
        {
            return executor.Execute(database.ListTrimAsync(key, start, stop));
        }
        #endregion
#if ORM_SYNC
        #region Sync
        public IConvertible GetByIndex(long index)
        {
            return database.ListGetByIndex(key, index);
        }
        public long InsertAfter(object pivot, object value)
        {
            return database.ListInsertAfter(key, pivot, value);
        }
        public long InsertBefore(object pivot, object value)
        {
            return database.ListInsertBefore(key, pivot, value);
        }
        public IConvertible LeftPop()
        {
            return database.ListLeftPop(key);
        }
        public long LeftPush(object[] values)
        {
            return database.ListLeftPush(key, values);
        }
        public long LeftPush(object value, When when = When.Always)
        {
            return database.ListLeftPush(key, value, when);
        }
        public long Length()
        {
            return database.ListLength(key);
        }
        public IConvertible[] Range(long start = 0, long stop = -1)
        {
            return database.ListRange(key, start, stop);
        }
        public long Remove(object value, long count = 0)
        {
            return database.ListRemove(key, value, count);
        }
        public IConvertible RightPop()
        {
            return database.ListRightPop(key);
        }
        public long RightPush(object value, When when = When.Always)
        {
            return database.ListRightPush(key, value, when);
        }
        public long RightPush(object[] values)
        {
            return database.ListRightPush(key, values);
        }
        public void SetByIndex(long index, object value)
        {
            database.ListSetByIndex(key, index, value);
        }
        public void Trim(long start, long stop)
        {
            database.ListTrim(key, start, stop);
        }
        #endregion
#endif
    }

    public class IMappingSortedSet : IMappingData
    {
        public IMappingSortedSet(string key, IMappingAdapter db, ITaskExecutor exe) : base(key, db, exe)
        {
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        #region Async
        public Task<bool> AddAsync(string member, double score, When when = When.Always)
        {
            return executor.Execute(database.SortedSetAddAsync(key, member, score, when));
        }
        public Task<long> AddAsync(SortedEntry[] values, When when = When.Always)
        {
            return executor.Execute(database.SortedSetAddAsync(key, values, when));
        }
        public Task<double> DecrementAsync(string member, double value)
        {
            return executor.Execute(database.SortedSetDecrementAsync(key, member, value));
        }
        public Task<double> IncrementAsync(string member, double value)
        {
            return executor.Execute(database.SortedSetIncrementAsync(key, member, value));
        }
        public Task<long> LengthAsync(double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
        {
            return executor.Execute(database.SortedSetLengthAsync(key, min, max, exclude));
        }
        public Task<long> LengthByValueAsync(string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            return executor.Execute(database.SortedSetLengthByValueAsync(key, min, max, exclude));
        }
        public Task<string[]> RangeByRankAsync(long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            return executor.Execute(database.SortedSetRangeByRankAsync(key, start, stop, order));
        }
        public Task<SortedEntry[]> RangeByRankWithScoresAsync(long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            return executor.Execute(database.SortedSetRangeByRankWithScoresAsync(key, start, stop, order));
        }
        public Task<string[]> RangeByScoreAsync(double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            return executor.Execute(database.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take));
        }
        public Task<SortedEntry[]> RangeByScoreWithScoresAsync(double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            return executor.Execute(database.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take));
        }
        public Task<string[]> RangeByValueAsync(string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
        {
            return executor.Execute(database.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take));
        }
        public Task<long?> RankAsync(string member, SortedOrder order = SortedOrder.Ascending)
        {
            return executor.Execute(database.SortedSetRankAsync(key, member, order));
        }
        public Task<bool> RemoveAsync(string member)
        {
            return executor.Execute(database.SortedSetRemoveAsync(key, member));
        }
        public Task<long> RemoveAsync(string[] members)
        {
            return executor.Execute(database.SortedSetRemoveAsync(key, members));
        }
        public Task<long> RemoveRangeByRankAsync(long start, long stop)
        {
            return executor.Execute(database.SortedSetRemoveRangeByRankAsync(key, start, stop));
        }
        public Task<long> RemoveRangeByScoreAsync(double start, double stop, SortedExclude exclude = SortedExclude.None)
        {
            return executor.Execute(database.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude));
        }
        public Task<long> RemoveRangeByValueAsync(string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            return executor.Execute(database.SortedSetRemoveRangeByValueAsync(key, min, max, exclude));
        }
        public Task<double?> ScoreAsync(string member)
        {
            return executor.Execute(database.SortedSetScoreAsync(key, member));
        }
        #endregion
#if ORM_SYNC
        #region Sync

        public bool Add(string member, double score, When when = When.Always)
        {
            return database.SortedSetAdd(key, member, score, when);
        }
        public long Add(SortedEntry[] values, When when = When.Always)
        {
            return database.SortedSetAdd(key, values, when);
        }
        public double Decrement(string member, double value)
        {
            return database.SortedSetDecrement(key, member, value);
        }
        public double Increment(string member, double value)
        {
            return database.SortedSetIncrement(key, member, value);
        }
        public long Length(double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
        {
            return database.SortedSetLength(key, min, max, exclude);
        }
        public long LengthByValue(string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            return database.SortedSetLengthByValue(key, min, max, exclude);
        }
        public string[] RangeByRank(long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            return database.SortedSetRangeByRank(key, start, stop, order);
        }
        public SortedEntry[] RangeByRankWithScores(long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            return database.SortedSetRangeByRankWithScores(key, start, stop, order);
        }
        public string[] RangeByScore(double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            return database.SortedSetRangeByScore(key, start, stop, exclude, order, skip, take);
        }
        public SortedEntry[] RangeByScoreWithScores(double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            return database.SortedSetRangeByScoreWithScores(key, start, stop, exclude, order, skip, take);
        }
        public string[] RangeByValue(string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
        {
            return database.SortedSetRangeByValue(key, min, max, exclude, skip, take);
        }
        public long? Rank(string member, SortedOrder order = SortedOrder.Ascending)
        {
            return database.SortedSetRank(key, member, order);
        }
        public bool Remove(string member)
        {
            return database.SortedSetRemove(key, member);
        }
        public long Remove(string[] members)
        {
            return database.SortedSetRemove(key, members);
        }
        public long RemoveRangeByRank(long start, long stop)
        {
            return database.SortedSetRemoveRangeByRank(key, start, stop);
        }
        public long RemoveRangeByScore(double start, double stop, SortedExclude exclude = SortedExclude.None)
        {
            return database.SortedSetRemoveRangeByScore(key, start, stop, exclude);
        }
        public long RemoveRangeByValue(string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            return database.SortedSetRemoveRangeByValue(key, min, max, exclude);
        }
        public double? Score(string member)
        {
            return database.SortedSetScore(key, member);
        }
        #endregion
#endif
    }



}
