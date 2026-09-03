using DeepCore;
using DeepCore.Reflection;
using DeepCore.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{
    [Reflectible]
    public class IMappingAdapter
    {
        public string DatabaseNumber { get; private set; }

        protected IMappingAdapter(string num)
        {
            this.DatabaseNumber = num;
        }
        public virtual IMappingDatabase CreateDatabase()
        {
            return ORMFactory.Instance.CreateDatabase(DatabaseNumber);
        }
        public virtual ITransactionDatabase CreateTransaction()
        {
            return ORMFactory.Instance.CreateTransaction(CreateDatabase());
        }
        public virtual ITransactionDatabase CreateTransaction(ICondition condition)
        {
            return ORMFactory.Instance.CreateTransaction(CreateDatabase(), condition);
        }
        public virtual ITransactionDatabase CreateTransaction(ICondition[] conditions)
        {
            return ORMFactory.Instance.CreateTransaction(CreateDatabase(), conditions);
        }
        //-----------------------------------------------------------------------------------------------------------
        #region DataMapping

        public virtual IMappingHash GetHash(string key, ITaskExecutor exe)
        {
            return new IMappingHash(key, this, exe);
        }
        public virtual IMappingString GetString(string key, ITaskExecutor exe)
        {
            return new IMappingString(key, this, exe);
        }
        public virtual IMappingSet GetSet(string key, ITaskExecutor exe)
        {
            return new IMappingSet(key, this, exe);
        }
        public virtual IMappingList GetList(string key, ITaskExecutor exe)
        {
            return new IMappingList(key, this, exe);
        }
        public virtual IMappingSortedSet GetSortedSet(string key, ITaskExecutor exe)
        {
            return new IMappingSortedSet(key, this, exe);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------
        #region ExecutableInterface

        public virtual IObjectTransaction CreateExecutableObjectTransaction(ITaskExecutor exe)
        {
            return new ExecutableObjectTransaction(this, exe);
        }
        public virtual IObjectTransaction CreateExecutableObjectTransaction(ITaskExecutor exe, ICondition condition)
        {
            return new ExecutableObjectTransaction(this, exe, condition);
        }
        public virtual IObjectTransaction CreateExecutableObjectTransaction(ITaskExecutor exe, ICondition[] conditions)
        {
            return new ExecutableObjectTransaction(this, exe, conditions);
        }
        public virtual IMappingDatabase CreateExecutableDatabase(ITaskExecutor exe)
        {
            return new IExecutableDatabase(CreateDatabase(), exe);
        }
        public virtual IMappingLocker CreateExecutablLocker(string key, ITaskExecutor exe)
        {
            return new ExecutableMappingLocker(CreateDatabase(), key, exe);
        }

        internal class IExecutableDatabase : ORMObject, IMappingDatabase
        {
            private ITaskExecutor exe;
            private IMappingDatabase db;
            public IExecutableDatabase(IMappingDatabase db, ITaskExecutor exe)
            {
                this.db = db;
                this.exe = exe ?? ITaskExecutor.Default;
            }
            protected override void Disposing()
            {
                this.db.Dispose();
                this.db = null;
                this.exe = null;
            }
            protected override async ValueTask DisposingAsync()
            {
                await this.db.DisposeAsync();
                this.db = null;
                this.exe = null;
            }
            #region Async
            //----------------------------------------------------------
            #region LockAndPersist
            public Task<bool> LockTakeAsync(string key, string token, TimeSpan expire)
            {
                return exe.Execute(db.LockTakeAsync(key, token, expire));
            }
            public Task<bool> LockReleaseAsync(string key, string token)
            {
                return exe.Execute(db.LockReleaseAsync(key, token));
            }
            public Task<object> PersistRecoverAsync(string key)
            {
                return exe.Execute(db.PersistRecoverAsync(key));
            }
            public Task<T> PersistRecoverAsync<T>(string key)
            {
                return exe.Execute(db.PersistRecoverAsync<T>(key));
            }
            public Task<bool> PersistDumpAsync(string key, object value)
            {
                return exe.Execute(db.PersistDumpAsync(key, value));
            }
            public Task<int> PersistDumpsAsync(params ValueTuple<string, object>[] dumps)
            {
                return exe.Execute(db.PersistDumpsAsync(dumps));
            }
            #endregion
            //----------------------------------------------------------
            #region Key

            public Task<long> KeyDeleteAsync(string[] keys)
            {
                return exe.Execute(db.KeyDeleteAsync(keys));
            }
            public Task<bool> KeyDeleteAsync(string key)
            {
                return exe.Execute(db.KeyDeleteAsync(key));
            }
            public Task<byte[]> KeyDumpAsync(string key)
            {
                return exe.Execute(db.KeyDumpAsync(key));
            }
            public Task<bool> KeyExistsAsync(string key)
            {
                return exe.Execute(db.KeyExistsAsync(key));
            }
            public Task<bool> KeyExpireAsync(string key, TimeSpan? expiry)
            {
                return exe.Execute(db.KeyExpireAsync(key, expiry));
            }
            public Task<bool> KeyExpireAsync(string key, DateTime? expiry)
            {
                return exe.Execute(db.KeyExpireAsync(key, expiry));
            }
            public Task<bool> KeyPersistAsync(string key)
            {
                return exe.Execute(db.KeyPersistAsync(key));
            }
            public Task<string> KeyRandomAsync()
            {
                return exe.Execute(db.KeyRandomAsync());
            }
            public Task<bool> KeyRenameAsync(string key, string newKey, When when = When.Always)
            {
                return exe.Execute(db.KeyRenameAsync(key, newKey, when));
            }
            public Task KeyRestoreAsync(string key, byte[] value, TimeSpan? expiry = null)
            {
                return exe.Execute(db.KeyRestoreAsync(key, value, expiry));
            }
            public Task<TimeSpan?> KeyTimeToLiveAsync(string key)
            {
                return exe.Execute(db.KeyTimeToLiveAsync(key));
            }

            #endregion
            //----------------------------------------------------------
            #region Hash

            public Task<long> HashDecrementAsync(string key, string hashField, long value = 1)
            {
                return exe.Execute(db.HashDecrementAsync(key, hashField, value));
            }
            public Task<double> HashDecrementAsync(string key, string hashField, double value)
            {
                return exe.Execute(db.HashDecrementAsync(key, hashField, value));
            }
            public Task<bool> HashDeleteAsync(string key, string hashField)
            {
                return exe.Execute(db.HashDeleteAsync(key, hashField));
            }
            public Task<long> HashDeleteAsync(string key, string[] hashFields)
            {
                return exe.Execute(db.HashDeleteAsync(key, hashFields));
            }
            public Task<bool> HashExistsAsync(string key, string hashField)
            {
                return exe.Execute(db.HashExistsAsync(key, hashField));
            }
            public Task<HashQueryEntry[]> HashGetAllAsync(string key)
            {
                return exe.Execute(db.HashGetAllAsync(key));
            }
            public Task<IConvertible> HashGetAsync(string key, string hashField)
            {
                return exe.Execute(db.HashGetAsync(key, hashField));
            }
            public Task<IConvertible[]> HashGetAsync(string key, string[] hashFields)
            {
                return exe.Execute(db.HashGetAsync(key, hashFields));
            }
            public Task<double> HashIncrementAsync(string key, string hashField, double value)
            {
                return exe.Execute(db.HashIncrementAsync(key, hashField, value));
            }
            public Task<long> HashIncrementAsync(string key, string hashField, long value = 1)
            {
                return exe.Execute(db.HashIncrementAsync(key, hashField, value));
            }
            public Task<string[]> HashKeysAsync(string key)
            {
                return exe.Execute(db.HashKeysAsync(key));
            }
            public Task<long> HashLengthAsync(string key)
            {
                return exe.Execute(db.HashLengthAsync(key));
            }
            public Task HashSetAsync(string key, HashUpdateEntry[] hashFields)
            {
                return exe.Execute(db.HashSetAsync(key, hashFields));
            }
            public Task<bool> HashSetAsync(string key, string hashField, object value, When when = When.Always)
            {
                return exe.Execute(db.HashSetAsync(key, hashField, value, when));
            }
            public Task<IConvertible[]> HashValuesAsync(string key)
            {
                return exe.Execute(db.HashValuesAsync(key));
            }
            public IAsyncEnumerable<HashQueryEntry> HashScanAsync(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
            {
                return db.HashScanAsync(key, pattern, pageSize, cursor, pageOffset);
            }
            #endregion
            //----------------------------------------------------------
            #region String

            public Task<long> StringAppendAsync(string key, object value)
            {
                return exe.Execute(db.StringAppendAsync(key, value));
            }
            public Task<long> StringBitCountAsync(string key, long start = 0, long end = -1)
            {
                return exe.Execute(db.StringBitCountAsync(key, start, end));
            }
            public Task<long> StringBitPositionAsync(string key, bool bit, long start = 0, long end = -1)
            {
                return exe.Execute(db.StringBitPositionAsync(key, bit, start, end));
            }
            public Task<long> StringDecrementAsync(string key, long value = 1)
            {
                return exe.Execute(db.StringDecrementAsync(key, value));
            }
            public Task<double> StringDecrementAsync(string key, double value)
            {
                return exe.Execute(db.StringDecrementAsync(key, value));
            }
            public Task<bool> StringGetBitAsync(string key, long offset)
            {
                return exe.Execute(db.StringGetBitAsync(key, offset));
            }
            public Task<IConvertible> StringGetRangeAsync(string key, long start, long end)
            {
                return exe.Execute(db.StringGetRangeAsync(key, start, end));
            }
            public Task<IConvertible> StringGetAsync(string key)
            {
                return exe.Execute(db.StringGetAsync(key));
            }
            public Task<IConvertible> StringGetSetAsync(string key, object value)
            {
                return exe.Execute(db.StringGetSetAsync(key, value));
            }
            public Task<long> StringIncrementAsync(string key, long value = 1)
            {
                return exe.Execute(db.StringIncrementAsync(key, value));
            }
            public Task<double> StringIncrementAsync(string key, double value)
            {
                return exe.Execute(db.StringIncrementAsync(key, value));
            }
            public Task<long> StringLengthAsync(string key)
            {
                return exe.Execute(db.StringLengthAsync(key));
            }
            public Task<bool> StringSetAsync(string key, object value, When when = When.Always)
            {
                return exe.Execute(db.StringSetAsync(key, value, when));
            }
            public Task<bool> StringSetBitAsync(string key, long offset, bool bit)
            {
                return exe.Execute(db.StringSetBitAsync(key, offset, bit));
            }
            public Task<long> StringSetRangeAsync(string key, long offset, object value)
            {
                return exe.Execute(db.StringSetRangeAsync(key, offset, value));
            }

            #endregion
            //----------------------------------------------------------
            #region Set

            public Task<bool> SetAddAsync(string key, object value)
            {
                return exe.Execute(db.SetAddAsync(key, value));
            }
            public Task<long> SetAddAsync(string key, object[] values)
            {
                return exe.Execute(db.SetAddAsync(key, values));
            }
            public Task<bool> SetContainsAsync(string key, object value)
            {
                return exe.Execute(db.SetContainsAsync(key, value));
            }
            public Task<long> SetLengthAsync(string key)
            {
                return exe.Execute(db.SetLengthAsync(key));
            }
            public Task<IConvertible[]> SetMembersAsync(string key)
            {
                return exe.Execute(db.SetMembersAsync(key));
            }
            public Task<IConvertible> SetPopAsync(string key)
            {
                return exe.Execute(db.SetPopAsync(key));
            }
            public Task<IConvertible> SetRandomMemberAsync(string key)
            {
                return exe.Execute(db.SetRandomMemberAsync(key));
            }
            public Task<IConvertible[]> SetRandomMembersAsync(string key, long count)
            {
                return exe.Execute(db.SetRandomMembersAsync(key, count));
            }
            public Task<bool> SetRemoveAsync(string key, object value)
            {
                return exe.Execute(db.SetRemoveAsync(key, value));
            }
            public Task<long> SetRemoveAsync(string key, object[] values)
            {
                return exe.Execute(db.SetRemoveAsync(key, values));
            }

            #endregion
            //----------------------------------------------------------
            #region List

            public Task<IConvertible> ListGetByIndexAsync(string key, long index)
            {
                return exe.Execute(db.ListGetByIndexAsync(key, index));
            }
            public Task<long> ListInsertAfterAsync(string key, object pivot, object value)
            {
                return exe.Execute(db.ListInsertAfterAsync(key, pivot, value));
            }
            public Task<long> ListInsertBeforeAsync(string key, object pivot, object value)
            {
                return exe.Execute(db.ListInsertBeforeAsync(key, pivot, value));
            }
            public Task<IConvertible> ListLeftPopAsync(string key)
            {
                return exe.Execute(db.ListLeftPopAsync(key));
            }
            public Task<long> ListLeftPushAsync(string key, object[] values)
            {
                return exe.Execute(db.ListLeftPushAsync(key, values));
            }
            public Task<long> ListLeftPushAsync(string key, object value, When when = When.Always)
            {
                return exe.Execute(db.ListLeftPushAsync(key, value, when));
            }
            public Task<long> ListLengthAsync(string key)
            {
                return exe.Execute(db.ListLengthAsync(key));
            }
            public Task<IConvertible[]> ListRangeAsync(string key, long start = 0, long stop = -1)
            {
                return exe.Execute(db.ListRangeAsync(key, start, stop));
            }
            public Task<long> ListRemoveAsync(string key, object value, long count = 0)
            {
                return exe.Execute(db.ListRemoveAsync(key, value, count));
            }
            public Task<IConvertible> ListRightPopAsync(string key)
            {
                return exe.Execute(db.ListRightPopAsync(key));
            }
            public Task<long> ListRightPushAsync(string key, object value, When when = When.Always)
            {
                return exe.Execute(db.ListRightPushAsync(key, value, when));
            }
            public Task<long> ListRightPushAsync(string key, object[] values)
            {
                return exe.Execute(db.ListRightPushAsync(key, values));
            }
            public Task ListSetByIndexAsync(string key, long index, object value)
            {
                return exe.Execute(db.ListSetByIndexAsync(key, index, value));
            }
            public Task ListTrimAsync(string key, long start, long stop)
            {
                return exe.Execute(db.ListTrimAsync(key, start, stop));
            }

            #endregion
            //----------------------------------------------------------
            #region SortedSet

            public Task<bool> SortedSetAddAsync(string key, string member, double score, When when = When.Always)
            {
                return exe.Execute(db.SortedSetAddAsync(key, member, score, when));
            }
            public Task<long> SortedSetAddAsync(string key, SortedEntry[] values, When when = When.Always)
            {
                return exe.Execute(db.SortedSetAddAsync(key, values, when));
            }
            public Task<double> SortedSetDecrementAsync(string key, string member, double value)
            {
                return exe.Execute(db.SortedSetDecrementAsync(key, member, value));
            }
            public Task<double> SortedSetIncrementAsync(string key, string member, double value)
            {
                return exe.Execute(db.SortedSetIncrementAsync(key, member, value));
            }
            public Task<long> SortedSetLengthAsync(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
            {
                return exe.Execute(db.SortedSetLengthAsync(key, min, max, exclude));
            }
            public Task<long> SortedSetLengthByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
            {
                return exe.Execute(db.SortedSetLengthByValueAsync(key, min, max, exclude));
            }
            public Task<string[]> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
            {
                return exe.Execute(db.SortedSetRangeByRankAsync(key, start, stop, order));
            }
            public Task<SortedEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
            {
                return exe.Execute(db.SortedSetRangeByRankWithScoresAsync(key, start, stop, order));
            }
            public Task<string[]> SortedSetRangeByScoreAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
            {
                return exe.Execute(db.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take));
            }
            public Task<SortedEntry[]> SortedSetRangeByScoreWithScoresAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
            {
                return exe.Execute(db.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take));
            }
            public Task<string[]> SortedSetRangeByValueAsync(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
            {
                return exe.Execute(db.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take));
            }
            public Task<long?> SortedSetRankAsync(string key, string member, SortedOrder order = SortedOrder.Ascending)
            {
                return exe.Execute(db.SortedSetRankAsync(key, member, order));
            }
            public Task<bool> SortedSetRemoveAsync(string key, string member)
            {
                return exe.Execute(db.SortedSetRemoveAsync(key, member));
            }
            public Task<long> SortedSetRemoveAsync(string key, string[] members)
            {
                return exe.Execute(db.SortedSetRemoveAsync(key, members));
            }
            public Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop)
            {
                return exe.Execute(db.SortedSetRemoveRangeByRankAsync(key, start, stop));
            }
            public Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop, SortedExclude exclude = SortedExclude.None)
            {
                return exe.Execute(db.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude));
            }
            public Task<long> SortedSetRemoveRangeByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
            {
                return exe.Execute(db.SortedSetRemoveRangeByValueAsync(key, min, max, exclude));
            }
            public Task<double?> SortedSetScoreAsync(string key, string member)
            {
                return exe.Execute(db.SortedSetScoreAsync(key, member));
            }
            #endregion
            //----------------------------------------------------------
            #region ObjectHash

            public Task<HashMap<string, ObjectQueryEntry[]>> ObjectBatchQueryAsync(string pattern)
            {
                return exe.Execute(db.ObjectBatchQueryAsync(pattern));
            }

            public Task<double> ObjectHashIncrementFieldAsync(string key, string field, double value = 1)
            {
                return exe.Execute(db.ObjectHashIncrementFieldAsync(key, field, value));
            }
            public Task<long> ObjectHashIncrementFieldAsync(string key, string field, long value = 1)
            {
                return exe.Execute(db.ObjectHashIncrementFieldAsync(key, field, value));
            }

            public Task<IConvertible[]> ObjectHashQueryFieldsAsync(string key)
            {
                return exe.Execute(db.ObjectHashQueryFieldsAsync(key));
            }
            public Task<ObjectQueryEntry[]> ObjectHashQueryEntriesAsync(string key)
            {
                return exe.Execute(db.ObjectHashQueryEntriesAsync(key));
            }
            public Task<IConvertible> ObjectHashQueryEntryAsync(string key, string fieldName)
            {
                return exe.Execute(db.ObjectHashQueryEntryAsync(key, fieldName));
            }
            public Task<bool> ObjectHashUpdateAsync(string key, ObjectUpdateEntry entry)
            {
                return exe.Execute(db.ObjectHashUpdateAsync(key, entry));
            }

            public Task<int> ObjectHashBatchUpdateAsync(string key, ICollection<ObjectUpdateEntry> entries)
            {
                return exe.Execute(db.ObjectHashBatchUpdateAsync(key, entries));
            }

            public void EnqueueHashBatchUpdate(IObjectTransaction taskQueue, string key, ICollection<ObjectUpdateEntry> entries)
            {
                db.EnqueueHashBatchUpdate(taskQueue, key, entries);
            }

            public void EnqueueHashUpdate(IObjectTransaction taskQueue, string key, ObjectUpdateEntry entry)
            {
                db.EnqueueHashUpdate(taskQueue, key, entry);
            }

            #endregion
            //----------------------------------------------------------
            #endregion
#if ORM_SYNC
            #region Sync
            public long KeyDelete(string[] keys)
            {
                return db.KeyDelete(keys);
            }

            public bool KeyDelete(string key)
            {
                return db.KeyDelete(key);
            }

            public byte[] KeyDump(string key)
            {
                return db.KeyDump(key);
            }

            public bool KeyExists(string key)
            {
                return db.KeyExists(key);
            }

            public bool KeyExpire(string key, TimeSpan? expiry)
            {
                return db.KeyExpire(key, expiry);
            }

            public bool KeyExpire(string key, DateTime? expiry)
            {
                return db.KeyExpire(key, expiry);
            }

            public bool KeyPersist(string key)
            {
                return db.KeyPersist(key);
            }

            public string KeyRandom()
            {
                return db.KeyRandom();
            }

            public bool KeyRename(string key, string newKey, When when = When.Always)
            {
                return db.KeyRename(key, newKey, when);
            }

            public void KeyRestore(string key, byte[] value, TimeSpan? expiry = null)
            {
                db.KeyRestore(key, value, expiry);
            }

            public TimeSpan? KeyTimeToLive(string key)
            {
                return db.KeyTimeToLive(key);
            }

            public bool LockTake(string key, string token, TimeSpan expire)
            {
                return db.LockTake(key, token, expire);
            }

            public bool LockRelease(string key, string token)
            {
                return db.LockRelease(key, token);
            }

            public long HashDecrement(string key, string hashField, long value = 1)
            {
                return db.HashDecrement(key, hashField, value);
            }

            public double HashDecrement(string key, string hashField, double value)
            {
                return db.HashDecrement(key, hashField, value);
            }

            public bool HashDelete(string key, string hashField)
            {
                return db.HashDelete(key, hashField);
            }

            public long HashDelete(string key, string[] hashFields)
            {
                return db.HashDelete(key, hashFields);
            }

            public bool HashExists(string key, string hashField)
            {
                return db.HashExists(key, hashField);
            }

            public HashQueryEntry[] HashGetAll(string key)
            {
                return db.HashGetAll(key);
            }

            public IConvertible HashGet(string key, string hashField)
            {
                return db.HashGet(key, hashField);
            }

            public IConvertible[] HashGet(string key, string[] hashFields)
            {
                return db.HashGet(key, hashFields);
            }

            public double HashIncrement(string key, string hashField, double value)
            {
                return db.HashIncrement(key, hashField, value);
            }

            public long HashIncrement(string key, string hashField, long value = 1)
            {
                return db.HashIncrement(key, hashField, value);
            }

            public string[] HashKeys(string key)
            {
                return db.HashKeys(key);
            }

            public long HashLength(string key)
            {
                return db.HashLength(key);
            }

            public void HashSet(string key, HashUpdateEntry[] hashFields)
            {
                db.HashSet(key, hashFields);
            }

            public bool HashSet(string key, string hashField, object value, When when = When.Always)
            {
                return db.HashSet(key, hashField, value, when);
            }

            public IConvertible[] HashValues(string key)
            {
                return db.HashValues(key);
            }
            public IEnumerable<HashQueryEntry> HashScan(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
            {
                return db.HashScan(key, pattern, pageSize, cursor, pageOffset);
            }

            public long StringAppend(string key, object value)
            {
                return db.StringAppend(key, value);
            }

            public long StringBitCount(string key, long start = 0, long end = -1)
            {
                return db.StringBitCount(key, start, end);
            }

            public long StringBitPosition(string key, bool bit, long start = 0, long end = -1)
            {
                return db.StringBitPosition(key, bit, start, end);
            }

            public long StringDecrement(string key, long value = 1)
            {
                return db.StringDecrement(key, value);
            }

            public double StringDecrement(string key, double value)
            {
                return db.StringDecrement(key, value);
            }

            public bool StringGetBit(string key, long offset)
            {
                return db.StringGetBit(key, offset);
            }

            public IConvertible StringGetRange(string key, long start, long end)
            {
                return db.StringGetRange(key, start, end);
            }

            public IConvertible StringGet(string key)
            {
                return db.StringGet(key);
            }

            public IConvertible StringGetSet(string key, object value)
            {
                return db.StringGetSet(key, value);
            }

            public long StringIncrement(string key, long value = 1)
            {
                return db.StringIncrement(key, value);
            }

            public double StringIncrement(string key, double value)
            {
                return db.StringIncrement(key, value);
            }

            public long StringLength(string key)
            {
                return db.StringLength(key);
            }

            public bool StringSet(string key, object value, When when = When.Always)
            {
                return db.StringSet(key, value, when);
            }

            public bool StringSetBit(string key, long offset, bool bit)
            {
                return db.StringSetBit(key, offset, bit);
            }

            public long StringSetRange(string key, long offset, object value)
            {
                return db.StringSetRange(key, offset, value);
            }

            public bool SetAdd(string key, object value)
            {
                return db.SetAdd(key, value);
            }

            public long SetAdd(string key, object[] values)
            {
                return db.SetAdd(key, values);
            }

            public bool SetContains(string key, object value)
            {
                return db.SetContains(key, value);
            }

            public long SetLength(string key)
            {
                return db.SetLength(key);
            }

            public IConvertible[] SetMembers(string key)
            {
                return db.SetMembers(key);
            }

            public IConvertible SetPop(string key)
            {
                return db.SetPop(key);
            }

            public IConvertible SetRandomMember(string key)
            {
                return db.SetRandomMember(key);
            }

            public IConvertible[] SetRandomMembers(string key, long count)
            {
                return db.SetRandomMembers(key, count);
            }

            public bool SetRemove(string key, object value)
            {
                return db.SetRemove(key, value);
            }

            public long SetRemove(string key, object[] values)
            {
                return db.SetRemove(key, values);
            }

            public IConvertible ListGetByIndex(string key, long index)
            {
                return db.ListGetByIndex(key, index);
            }

            public long ListInsertAfter(string key, object pivot, object value)
            {
                return db.ListInsertAfter(key, pivot, value);
            }

            public long ListInsertBefore(string key, object pivot, object value)
            {
                return db.ListInsertBefore(key, pivot, value);
            }

            public IConvertible ListLeftPop(string key)
            {
                return db.ListLeftPop(key);
            }

            public long ListLeftPush(string key, object[] values)
            {
                return db.ListLeftPush(key, values);
            }

            public long ListLeftPush(string key, object value, When when = When.Always)
            {
                return db.ListLeftPush(key, value, when);
            }

            public long ListLength(string key)
            {
                return db.ListLength(key);
            }

            public IConvertible[] ListRange(string key, long start = 0, long stop = -1)
            {
                return db.ListRange(key, start, stop);
            }

            public long ListRemove(string key, object value, long count = 0)
            {
                return db.ListRemove(key, value, count);
            }

            public IConvertible ListRightPop(string key)
            {
                return db.ListRightPop(key);
            }

            public long ListRightPush(string key, object value, When when = When.Always)
            {
                return db.ListRightPush(key, value, when);
            }

            public long ListRightPush(string key, object[] values)
            {
                return db.ListRightPush(key, values);
            }

            public void ListSetByIndex(string key, long index, object value)
            {
                db.ListSetByIndex(key, index, value);
            }

            public void ListTrim(string key, long start, long stop)
            {
                db.ListTrim(key, start, stop);
            }

            public bool SortedSetAdd(string key, string member, double score, When when = When.Always)
            {
                return db.SortedSetAdd(key, member, score, when);
            }

            public long SortedSetAdd(string key, SortedEntry[] values, When when = When.Always)
            {
                return db.SortedSetAdd(key, values, when);
            }

            public double SortedSetDecrement(string key, string member, double value)
            {
                return db.SortedSetDecrement(key, member, value);
            }

            public double SortedSetIncrement(string key, string member, double value)
            {
                return db.SortedSetIncrement(key, member, value);
            }

            public long SortedSetLength(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
            {
                return db.SortedSetLength(key, min, max, exclude);
            }

            public long SortedSetLengthByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
            {
                return db.SortedSetLengthByValue(key, min, max, exclude);
            }

            public string[] SortedSetRangeByRank(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
            {
                return db.SortedSetRangeByRank(key, start, stop, order);
            }

            public SortedEntry[] SortedSetRangeByRankWithScores(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
            {
                return db.SortedSetRangeByRankWithScores(key, start, stop, order);
            }

            public string[] SortedSetRangeByScore(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
            {
                return db.SortedSetRangeByScore(key, start, stop, exclude, order, skip, take);
            }

            public SortedEntry[] SortedSetRangeByScoreWithScores(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
            {
                return db.SortedSetRangeByScoreWithScores(key, start, stop, exclude, order, skip, take);
            }

            public string[] SortedSetRangeByValue(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
            {
                return db.SortedSetRangeByValue(key, min, max, exclude, skip, take);
            }

            public long? SortedSetRank(string key, string member, SortedOrder order = SortedOrder.Ascending)
            {
                return db.SortedSetRank(key, member, order);
            }

            public bool SortedSetRemove(string key, string member)
            {
                return db.SortedSetRemove(key, member);
            }

            public long SortedSetRemove(string key, string[] members)
            {
                return db.SortedSetRemove(key, members);
            }

            public long SortedSetRemoveRangeByRank(string key, long start, long stop)
            {
                return db.SortedSetRemoveRangeByRank(key, start, stop);
            }

            public long SortedSetRemoveRangeByScore(string key, double start, double stop, SortedExclude exclude = SortedExclude.None)
            {
                return db.SortedSetRemoveRangeByScore(key, start, stop, exclude);
            }

            public long SortedSetRemoveRangeByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
            {
                return db.SortedSetRemoveRangeByValue(key, min, max, exclude);
            }

            public double? SortedSetScore(string key, string member)
            {
                return db.SortedSetScore(key, member);
            }


            public HashMap<string, ObjectQueryEntry[]> ObjectBatchQuery(string pattern)
            {
                return db.ObjectBatchQuery(pattern);
            }

            public IConvertible[] ObjectHashQueryFields(string key)
            {
                return db.ObjectHashQueryFields(key);
            }

            public ObjectQueryEntry[] ObjectHashQueryEntries(string key)
            {
                return db.ObjectHashQueryEntries(key);
            }

            public IConvertible ObjectHashQueryEntry(string key, string fieldName)
            {
                return db.ObjectHashQueryEntry(key, fieldName);
            }

            public double ObjectHashIncrementField(string key, string field, double value = 1)
            {
                return db.ObjectHashIncrementField(key, field, value);
            }

            public long ObjectHashIncrementField(string key, string field, long value = 1)
            {
                return db.ObjectHashIncrementField(key, field, value);
            }

            public bool ObjectHashUpdate(string key, ObjectUpdateEntry entry)
            {
                return db.ObjectHashUpdate(key, entry);
            }

            public int ObjectHashBatchUpdate(string key, ICollection<ObjectUpdateEntry> entries)
            {
                return db.ObjectHashBatchUpdate(key, entries);
            }

            public object PersistRecover(string key)
            {
                return db.PersistRecover(key);
            }
            public T PersistRecover<T>(string key)
            {
                return db.PersistRecover<T>(key);
            }
            public bool PersistDump(string key, object value)
            {
                return db.PersistDump(key, value);
            }
            public int PersistDumps(params ValueTuple<string, object>[] dumps)
            {
                return db.PersistDumps(dumps);
            }
            #endregion
#endif
        }

        internal class ExecutableObjectTransaction : ObjectTransaction
        {
            private readonly ITaskExecutor exe;
            public ExecutableObjectTransaction(IMappingAdapter adapter, ITaskExecutor exe, params ICondition[] conditions) : base(adapter, conditions)
            {
                this.exe = exe ?? ITaskExecutor.Default;
            }
            public ExecutableObjectTransaction(IMappingAdapter adapter, ITaskExecutor exe) : base(adapter)
            {
                this.exe = exe ?? ITaskExecutor.Default;
            }
            public override Task<bool> ExecuteAsync()
            {
                return exe.Execute(base.ExecuteAsync());
            }
        }

        internal class ExecutableMappingLocker : ORMObject, IMappingLocker
        {
            private IMappingDatabase db;
            private ITaskExecutor exe;
            private readonly string lock_key;
            private string lock_value;
            private int indexer = 0;
            public ExecutableMappingLocker(IMappingDatabase db, string key, ITaskExecutor exe)
            {
                this.db = db;
                this.exe = exe ?? ITaskExecutor.Default;
                this.lock_key = key;
            }
            protected override void Disposing()
            {
                this.db = null;
                this.exe = null;
            }
            protected override ValueTask DisposingAsync()
            {
                this.db = null;
                this.exe = null;
                return new ValueTask(Task.CompletedTask);
            }
            public Task<bool> EnterLockAsync(out string token)
            {
                if (lock_value == null)
                {
                    this.lock_value = Guid.NewGuid().ToString();
                }
                string _token = string.Format("{0}:{1}", lock_value, (indexer++));
                token = _token;
                return exe.Execute(async () =>
                {
                    int retry_count = 600;
                    while (await db.LockTakeAsync(lock_key, _token, TimeSpan.FromSeconds(10)) == false)
                    {
                        if (base.IsDisposed)
                        {
                            //throw new ThreadInterruptedException(string.Format("RedisLock [{0}] Disposed !!!", lock_key));
                            return false;
                        }
                        if (--retry_count < 0) { return false; }
                        await Task.Delay(100);
                    }
                    return true;
                });
            }
            public Task<bool> ExitLockAsync(string token)
            {
                return exe.Execute(db.LockReleaseAsync(lock_key, token));
            }
#if ORM_SYNC
            public bool EnterLock(out string token)
            {
                if (lock_value == null)
                {
                    this.lock_value = Guid.NewGuid().ToString();
                }
                string _token = string.Format("{0}:{1}", lock_value, (indexer++));
                token = _token;
                int retry_count = 600;
                while (db.LockTake(lock_key, _token, TimeSpan.FromSeconds(10)) == false)
                {
                    if (base.IsDisposed)
                    {
                        //throw new ThreadInterruptedException(string.Format("RedisLock [{0}] Disposed !!!", lock_key));
                        return false;
                    }
                    if (--retry_count < 0) { return false; }
                    System.Threading.Thread.Sleep(100);
                }
                return true;

            }
            public bool ExitLock(string token)
            {
                return db.LockRelease(lock_key, token);
            }
#endif
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------
    }

    [Reflectible]
    public interface IMappingDatabaseAsync : IAsyncDisposable, IDisposable
    {
        #region Async
        Task<long> KeyDeleteAsync(string[] keys);
        Task<bool> KeyDeleteAsync(string key);
        Task<byte[]> KeyDumpAsync(string key);
        Task<bool> KeyExistsAsync(string key);
        Task<bool> KeyExpireAsync(string key, TimeSpan? expiry);
        Task<bool> KeyExpireAsync(string key, DateTime? expiry);
        Task<bool> KeyPersistAsync(string key);
        Task<string> KeyRandomAsync();
        Task<bool> KeyRenameAsync(string key, string newKey, When when = When.Always);
        Task KeyRestoreAsync(string key, byte[] value, TimeSpan? expiry = null);
        Task<TimeSpan?> KeyTimeToLiveAsync(string key);

        Task<bool> LockTakeAsync(string key, string token, TimeSpan expire);
        Task<bool> LockReleaseAsync(string key, string token);

        Task<long> HashDecrementAsync(string key, string hashField, long value = 1);
        Task<double> HashDecrementAsync(string key, string hashField, double value);
        Task<bool> HashDeleteAsync(string key, string hashField);
        Task<long> HashDeleteAsync(string key, string[] hashFields);
        Task<bool> HashExistsAsync(string key, string hashField);
        Task<HashQueryEntry[]> HashGetAllAsync(string key);
        Task<IConvertible> HashGetAsync(string key, string hashField);
        Task<IConvertible[]> HashGetAsync(string key, string[] hashFields);
        Task<double> HashIncrementAsync(string key, string hashField, double value);
        Task<long> HashIncrementAsync(string key, string hashField, long value = 1);
        Task<string[]> HashKeysAsync(string key);
        Task<long> HashLengthAsync(string key);
        Task HashSetAsync(string key, HashUpdateEntry[] hashFields);
        Task<bool> HashSetAsync(string key, string hashField, object value, When when = When.Always);
        Task<IConvertible[]> HashValuesAsync(string key);
        IAsyncEnumerable<HashQueryEntry> HashScanAsync(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0);

        Task<long> StringAppendAsync(string key, object value);
        Task<long> StringBitCountAsync(string key, long start = 0, long end = -1);
        Task<long> StringBitPositionAsync(string key, bool bit, long start = 0, long end = -1);
        Task<long> StringDecrementAsync(string key, long value = 1);
        Task<double> StringDecrementAsync(string key, double value);
        Task<bool> StringGetBitAsync(string key, long offset);
        Task<IConvertible> StringGetRangeAsync(string key, long start, long end);
        Task<IConvertible> StringGetAsync(string key);
        Task<IConvertible> StringGetSetAsync(string key, object value);
        Task<long> StringIncrementAsync(string key, long value = 1);
        Task<double> StringIncrementAsync(string key, double value);
        Task<long> StringLengthAsync(string key);
        Task<bool> StringSetAsync(string key, object value, When when = When.Always);
        Task<bool> StringSetBitAsync(string key, long offset, bool bit);
        Task<long> StringSetRangeAsync(string key, long offset, object value);

        Task<bool> SetAddAsync(string key, object value);
        Task<long> SetAddAsync(string key, object[] values);
        Task<bool> SetContainsAsync(string key, object value);
        Task<long> SetLengthAsync(string key);
        Task<IConvertible[]> SetMembersAsync(string key);
        Task<IConvertible> SetPopAsync(string key);
        Task<IConvertible> SetRandomMemberAsync(string key);
        Task<IConvertible[]> SetRandomMembersAsync(string key, long count);
        Task<bool> SetRemoveAsync(string key, object value);
        Task<long> SetRemoveAsync(string key, object[] values);

        Task<IConvertible> ListGetByIndexAsync(string key, long index);
        Task<long> ListInsertAfterAsync(string key, object pivot, object value);
        Task<long> ListInsertBeforeAsync(string key, object pivot, object value);
        Task<IConvertible> ListLeftPopAsync(string key);
        Task<long> ListLeftPushAsync(string key, object[] values);
        Task<long> ListLeftPushAsync(string key, object value, When when = When.Always);
        Task<long> ListLengthAsync(string key);
        Task<IConvertible[]> ListRangeAsync(string key, long start = 0, long stop = -1);
        Task<long> ListRemoveAsync(string key, object value, long count = 0);
        Task<IConvertible> ListRightPopAsync(string key);
        Task<long> ListRightPushAsync(string key, object value, When when = When.Always);
        Task<long> ListRightPushAsync(string key, object[] values);
        Task ListSetByIndexAsync(string key, long index, object value);
        Task ListTrimAsync(string key, long start, long stop);

        Task<bool> SortedSetAddAsync(string key, string member, double score, When when = When.Always);
        Task<long> SortedSetAddAsync(string key, SortedEntry[] values, When when = When.Always);
        Task<double> SortedSetDecrementAsync(string key, string member, double value);
        Task<double> SortedSetIncrementAsync(string key, string member, double value);
        Task<long> SortedSetLengthAsync(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None);
        Task<long> SortedSetLengthByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None);
        Task<string[]> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending);
        Task<SortedEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending);
        Task<string[]> SortedSetRangeByScoreAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1);
        Task<SortedEntry[]> SortedSetRangeByScoreWithScoresAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1);
        Task<string[]> SortedSetRangeByValueAsync(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1);
        Task<long?> SortedSetRankAsync(string key, string member, SortedOrder order = SortedOrder.Ascending);
        Task<bool> SortedSetRemoveAsync(string key, string member);
        Task<long> SortedSetRemoveAsync(string key, string[] members);
        Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop);
        Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop, SortedExclude exclude = SortedExclude.None);
        Task<long> SortedSetRemoveRangeByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None);
        Task<double?> SortedSetScoreAsync(string key, string member);

        Task<HashMap<string, ObjectQueryEntry[]>> ObjectBatchQueryAsync(string key);
        Task<IConvertible[]> ObjectHashQueryFieldsAsync(string key);
        Task<ObjectQueryEntry[]> ObjectHashQueryEntriesAsync(string key);
        Task<IConvertible> ObjectHashQueryEntryAsync(string key, string fieldName);
        Task<double> ObjectHashIncrementFieldAsync(string key, string field, double value = 1.0f);
        Task<long> ObjectHashIncrementFieldAsync(string key, string field, long value = 1);
        Task<bool> ObjectHashUpdateAsync(string key, ObjectUpdateEntry entry);
        Task<int> ObjectHashBatchUpdateAsync(string key, ICollection<ObjectUpdateEntry> entries);

        void EnqueueHashBatchUpdate(IObjectTransaction taskQueue, string key, ICollection<ObjectUpdateEntry> entries);
        void EnqueueHashUpdate(IObjectTransaction taskQueue, string key, ObjectUpdateEntry entry);

        /// <summary>
        /// 从持久化恢复
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<object> PersistRecoverAsync(string key);
        Task<T> PersistRecoverAsync<T>(string key);

        Task<bool> PersistDumpAsync(string key, object data);

        Task<int> PersistDumpsAsync(params ValueTuple<string, object>[] dumps);

        #endregion
    }
    [Reflectible]
    public interface IMappingDatabase : IMappingDatabaseAsync
    {
#if ORM_SYNC
        #region Sync

        long KeyDelete(string[] keys);
        bool KeyDelete(string key);
        byte[] KeyDump(string key);
        bool KeyExists(string key);
        bool KeyExpire(string key, TimeSpan? expiry);
        bool KeyExpire(string key, DateTime? expiry);
        bool KeyPersist(string key);
        string KeyRandom();
        bool KeyRename(string key, string newKey, When when = When.Always);
        void KeyRestore(string key, byte[] value, TimeSpan? expiry = null);
        TimeSpan? KeyTimeToLive(string key);

        bool LockTake(string key, string token, TimeSpan expire);
        bool LockRelease(string key, string token);

        long HashDecrement(string key, string hashField, long value = 1);
        double HashDecrement(string key, string hashField, double value);
        bool HashDelete(string key, string hashField);
        long HashDelete(string key, string[] hashFields);
        bool HashExists(string key, string hashField);
        HashQueryEntry[] HashGetAll(string key);
        IConvertible HashGet(string key, string hashField);
        IConvertible[] HashGet(string key, string[] hashFields);
        double HashIncrement(string key, string hashField, double value);
        long HashIncrement(string key, string hashField, long value = 1);
        string[] HashKeys(string key);
        long HashLength(string key);
        void HashSet(string key, HashUpdateEntry[] hashFields);
        bool HashSet(string key, string hashField, object value, When when = When.Always);
        IConvertible[] HashValues(string key);
        IEnumerable<HashQueryEntry> HashScan(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0);

        long StringAppend(string key, object value);
        long StringBitCount(string key, long start = 0, long end = -1);
        long StringBitPosition(string key, bool bit, long start = 0, long end = -1);
        long StringDecrement(string key, long value = 1);
        double StringDecrement(string key, double value);
        bool StringGetBit(string key, long offset);
        IConvertible StringGetRange(string key, long start, long end);
        IConvertible StringGet(string key);
        IConvertible StringGetSet(string key, object value);
        long StringIncrement(string key, long value = 1);
        double StringIncrement(string key, double value);
        long StringLength(string key);
        bool StringSet(string key, object value, When when = When.Always);
        bool StringSetBit(string key, long offset, bool bit);
        long StringSetRange(string key, long offset, object value);

        bool SetAdd(string key, object value);
        long SetAdd(string key, object[] values);
        bool SetContains(string key, object value);
        long SetLength(string key);
        IConvertible[] SetMembers(string key);
        IConvertible SetPop(string key);
        IConvertible SetRandomMember(string key);
        IConvertible[] SetRandomMembers(string key, long count);
        bool SetRemove(string key, object value);
        long SetRemove(string key, object[] values);

        IConvertible ListGetByIndex(string key, long index);
        long ListInsertAfter(string key, object pivot, object value);
        long ListInsertBefore(string key, object pivot, object value);
        IConvertible ListLeftPop(string key);
        long ListLeftPush(string key, object[] values);
        long ListLeftPush(string key, object value, When when = When.Always);
        long ListLength(string key);
        IConvertible[] ListRange(string key, long start = 0, long stop = -1);
        long ListRemove(string key, object value, long count = 0);
        IConvertible ListRightPop(string key);
        long ListRightPush(string key, object value, When when = When.Always);
        long ListRightPush(string key, object[] values);
        void ListSetByIndex(string key, long index, object value);
        void ListTrim(string key, long start, long stop);

        bool SortedSetAdd(string key, string member, double score, When when = When.Always);
        long SortedSetAdd(string key, SortedEntry[] values, When when = When.Always);
        double SortedSetDecrement(string key, string member, double value);
        double SortedSetIncrement(string key, string member, double value);
        long SortedSetLength(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None);
        long SortedSetLengthByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None);
        string[] SortedSetRangeByRank(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending);
        SortedEntry[] SortedSetRangeByRankWithScores(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending);
        string[] SortedSetRangeByScore(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1);
        SortedEntry[] SortedSetRangeByScoreWithScores(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1);
        string[] SortedSetRangeByValue(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1);
        long? SortedSetRank(string key, string member, SortedOrder order = SortedOrder.Ascending);
        bool SortedSetRemove(string key, string member);
        long SortedSetRemove(string key, string[] members);
        long SortedSetRemoveRangeByRank(string key, long start, long stop);
        long SortedSetRemoveRangeByScore(string key, double start, double stop, SortedExclude exclude = SortedExclude.None);
        long SortedSetRemoveRangeByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None);
        double? SortedSetScore(string key, string member);

        HashMap<string, ObjectQueryEntry[]> ObjectBatchQuery(string key);
        IConvertible[] ObjectHashQueryFields(string key);
        ObjectQueryEntry[] ObjectHashQueryEntries(string key);
        IConvertible ObjectHashQueryEntry(string key, string fieldName);
        double ObjectHashIncrementField(string key, string field, double value = 1.0f);
        long ObjectHashIncrementField(string key, string field, long value = 1);
        bool ObjectHashUpdate(string key, ObjectUpdateEntry entry);
        int ObjectHashBatchUpdate(string key, ICollection<ObjectUpdateEntry> entries);

        /// <summary>
        /// 从持久化恢复
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object PersistRecover(string key);
        T PersistRecover<T>(string key);

        bool PersistDump(string key, object data);
        int PersistDumps(params ValueTuple<string, object>[] dumps);

        #endregion
#endif
    }

    [Reflectible]
    public interface ITransactionDatabase : IMappingDatabaseAsync
    {
        int BatchCount { get; }
        void Enqueue(Task task);
        void Enqueue(Func<Task> task);
        Task<bool> ExecuteAsync();
        //void Execute();
    }

    [Reflectible]
    public interface IObjectTransaction : IAsyncDisposable, IDisposable
    {
        int BatchCount { get; }
        ITransactionDatabase Database { get; }
        void Enqueue(Task task);
        void Enqueue(Func<Task> task);
        Task<bool> ExecuteAsync();
        //void Execute();

        void DebugBeginMappingObject(MappingObject mapping);
        void DebugForEachMappingObject(Action<MappingObject> action);
    }

    [Reflectible]
    public interface IMappingLocker
    {
        Task<bool> EnterLockAsync(out string token);
        Task<bool> ExitLockAsync(string token);
#if ORM_SYNC
        bool EnterLock(out string token);
        bool ExitLock(string token);
#endif
    }

    [Reflectible]
    public interface ICondition
    {
    }
    [Reflectible]
    public interface IConditions
    {
        ICondition HashEqual(string key, string hashField, object value);
        ICondition HashExists(string key, string hashField);

        ICondition KeyExists(string key);
        ICondition KeyNotExists(string key);

        ICondition HashNotEqual(string key, string hashField, object value);
        ICondition HashNotExists(string key, string hashField);

        ICondition SetLengthEqual(string key, long length);
        ICondition SetLengthGreaterThan(string key, long length);
        ICondition SetLengthLessThan(string key, long length);

        ICondition ListIndexEqual(string key, long index, object value);
        ICondition ListIndexExists(string key, long index);
        ICondition ListIndexNotEqual(string key, long index, object value);
        ICondition ListIndexNotExists(string key, long index);
        ICondition ListLengthEqual(string key, long length);
        ICondition ListLengthGreaterThan(string key, long length);
        ICondition ListLengthLessThan(string key, long length);

        ICondition SortedSetLengthEqual(string key, long length);
        ICondition SortedSetLengthGreaterThan(string key, long length);
        ICondition SortedSetLengthLessThan(string key, long length);

        ICondition StringEqual(string key, object value);
        ICondition StringLengthEqual(string key, long length);
        ICondition StringLengthGreaterThan(string key, long length);
        ICondition StringLengthLessThan(string key, long length);
        ICondition StringNotEqual(string key, object value);
    }

    public enum When
    {
        Always = 0,
        Exists = 1,
        NotExists = 2
    }

    public enum ExecuteEvent
    {
        UPDATE_FIELD,
        DELETE_FIELD,
        DELETE_KEY,
        RENAME_KEY,
        UPDATE_TOP_KEY,
        DELETE_TOP_KEY,
    }

    public struct ObjectUpdateEntry
    {
        public ExecuteEvent Event;
        public string FieldName;
        public object FieldValue;
        public ObjectUpdateEntry(ExecuteEvent evt, string name, object value)
        {
            this.Event = evt;
            this.FieldName = name;
            this.FieldValue = value;
        }
        public ObjectUpdateEntry(ExecuteEvent evt, string name)
        {
            this.Event = evt;
            this.FieldName = name;
            this.FieldValue = null;
        }
        public ObjectUpdateEntry(ExecuteEvent evt)
        {
            this.Event = evt;
            this.FieldName = null;
            this.FieldValue = null;
        }
        public override string ToString()
        {
            return string.Format("{0}:{1}", Event, FieldName);
        }
    }

    public struct ObjectQueryEntry
    {
        public IConvertible FieldName;
        public IConvertible FieldValue;
        public ObjectQueryEntry(IConvertible name, IConvertible value)
        {
            this.FieldName = name;
            this.FieldValue = value;
        }
        public override string ToString()
        {
            return string.Format("{0}", FieldName);
        }
    }

    public struct HashQueryEntry
    {
        public string FieldName;
        public IConvertible FieldValue;
        public HashQueryEntry(string name, IConvertible value)
        {
            this.FieldName = name;
            this.FieldValue = value;
        }
        public override string ToString()
        {
            return string.Format("{0}", FieldName);
        }
    }
    public struct HashQueryEntry<T>
    {
        public string FieldName;
        public T FieldValue;
        public HashQueryEntry(string name, T value)
        {
            this.FieldName = name;
            this.FieldValue = value;
        }
    }
    public struct HashUpdateEntry
    {
        public string FieldName;
        public object FieldValue;
    }

    public enum SortedOrder
    {
        Ascending = 0,
        Descending = 1
    }
    public enum SortedExclude
    {
        //     Both start and stop are inclusive
        None = 0,
        //     Start is exclusive, stop is inclusive
        Start = 1,
        //     Start is inclusive, stop is exclusive
        Stop = 2,
        //     Both start and stop are exclusive
        Both = 3
    }
    public struct SortedEntry
    {
        public string Member;
        public double Score;
        public SortedEntry(string member, double score)
        {
            this.Member = member;
            this.Score = score;
        }
    }

}

