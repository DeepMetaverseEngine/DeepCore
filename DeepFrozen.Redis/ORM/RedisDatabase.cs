using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeepCore.Threading;

namespace DeepCrystal.ORM.Redis
{
    //------------------------------------------------------------------------------------------------------------------
    public class RedisMappingAdapter : IMappingAdapter
    {
        public readonly IDatabase db;
        public readonly RedisDatabase redis_db;
        internal RedisMappingAdapter(IDatabase db) : base(db.Database.ToString())
        {
            this.db = db;
            this.redis_db = new RedisDatabase(this, db, db.Database);
        }
    }
    //------------------------------------------------------------------------------------------------------------------
    public class RedisDatabaseAsync : ORMObject, IMappingDatabaseAsync
    {
        public const string KEY_TOP_MAPPING_KEYS = ".top_mapping_keys";
        public const string KEY_TOP_DUMP_KEYS = ".top_dump_keys";
        public readonly int db_number;
        public readonly IDatabaseAsync db_async;
        public readonly IMappingAdapter adapter;
        internal RedisDatabaseAsync(IMappingAdapter adapter, IDatabaseAsync db, int db_num)
        {
            this.adapter = adapter;
            this.db_async = db;
            this.db_number = db_num;
        }
        protected override void Disposing()
        {
        }
        protected override ValueTask DisposingAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
        //----------------------------------------------------------
        #region Lock
        public Task<bool> LockTakeAsync(string key, string token, TimeSpan expire)
        {
            ORMStatistics.LogLoadCall(null);
            return db_async.LockTakeAsync(key, token, expire);
        }
        public Task<bool> LockReleaseAsync(string key, string token)
        {
            ORMStatistics.LogSaveCall(null);
            return db_async.LockReleaseAsync(key, token);
        }
        #endregion
        //----------------------------------------------------------
        #region Key

        public Task<long> KeyDeleteAsync(string[] keys)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyDeleteAsync(Array.ConvertAll<string, RedisKey>(keys, e => e));
        }
        public Task<bool> KeyDeleteAsync(string key)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyDeleteAsync(key);
        }
        public Task<byte[]> KeyDumpAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.KeyDumpAsync(key);
        }
        public Task<bool> KeyExistsAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.KeyExistsAsync(key);
        }
        public Task<bool> KeyExpireAsync(string key, TimeSpan? expiry)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyExpireAsync(key, expiry);
        }
        public Task<bool> KeyExpireAsync(string key, DateTime? expiry)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyExpireAsync(key, expiry);
        }
        public Task<bool> KeyPersistAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.KeyPersistAsync(key);
        }
        public Task<string> KeyRandomAsync()
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.KeyRandomAsync().ContinueWith(t => t.GetResultToString());
        }
        public Task<bool> KeyRenameAsync(string key, string newKey, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyRenameAsync(key, newKey, (StackExchange.Redis.When)when);
        }
        public Task KeyRestoreAsync(string key, byte[] value, TimeSpan? expiry = null)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyRestoreAsync(key, value, expiry);
        }
        public Task<TimeSpan?> KeyTimeToLiveAsync(string key)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.KeyTimeToLiveAsync(key);
        }

        #endregion
        //----------------------------------------------------------
        #region Hash

        public Task<long> HashDecrementAsync(string key, string hashField, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.HashDecrementAsync(key, hashField, value));
        }
        public Task<double> HashDecrementAsync(string key, string hashField, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.HashDecrementAsync(key, hashField, value);
        }
        public Task<bool> HashDeleteAsync(string key, string hashField)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.HashDeleteAsync(key, hashField));
        }
        public Task<long> HashDeleteAsync(string key, string[] hashFields)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.HashDeleteAsync(key, Array.ConvertAll<string, RedisValue>(hashFields, e => e)));
        }
        public Task<bool> HashExistsAsync(string key, string hashField)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.HashExistsAsync(key, hashField));
        }
        public Task<HashQueryEntry[]> HashGetAllAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.HashGetAllAsync(key).ContinueWith((t) =>
            {
                var entries = t.GetResultAs();
                if (entries != null)
                {
                    return Array.ConvertAll<HashEntry, HashQueryEntry>(entries, (e) => new HashQueryEntry(e.Name, e.Value));
                }
                return null;
            }));
        }
        public Task<IConvertible> HashGetAsync(string key, string hashField)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.HashGetAsync(key, hashField).ContinueWith((t) => t.GetResultAs<RedisValue, IConvertible>()));
        }
        public Task<IConvertible[]> HashGetAsync(string key, string[] hashFields)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.HashGetAsync(key, Array.ConvertAll<string, RedisValue>(hashFields, (a) => (a))).ContinueWith((t) =>
            {
                var entries = t.GetResultAs();
                if (entries != null)
                {
                    return Array.ConvertAll<RedisValue, IConvertible>(entries, e => e);
                }
                return null;
            }));
        }
        public Task<double> HashIncrementAsync(string key, string hashField, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.HashIncrementAsync(key, hashField, value));
        }
        public Task<long> HashIncrementAsync(string key, string hashField, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.HashIncrementAsync(key, hashField, value));
        }
        public Task<string[]> HashKeysAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.HashKeysAsync(key).ContinueWith((t) =>
            {
                var entries = t.GetResultAs();
                if (entries != null)
                {
                    return Array.ConvertAll<RedisValue, string>(entries, e => e);
                }
                return null;
            }));
        }
        public Task<long> HashLengthAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.HashLengthAsync(key);
        }
        public Task HashSetAsync(string key, HashUpdateEntry[] hashFields)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.HashSetAsync(key, Array.ConvertAll(hashFields, e => new HashEntry(e.FieldName, RedisConverters.ToRedisValue(e.FieldValue))));
        }
        public Task<bool> HashSetAsync(string key, string hashField, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.HashSetAsync(key, hashField, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when);
        }
        public Task<IConvertible[]> HashValuesAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.HashValuesAsync(key).ContinueWith((t) =>
            {
                var entries = t.GetResultAs();
                if (entries != null)
                {
                    return Array.ConvertAll<RedisValue, IConvertible>(entries, e => e);
                }
                return null;
            });
        }
        public IAsyncEnumerable<HashQueryEntry> HashScanAsync(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
        {
            ORMStatistics.LogLoadCall(this);
            var it = db_async.HashScanAsync(key, pattern, pageSize, cursor, pageOffset);
            if (it != null)
            {
                return new HashScanAsyncEnumerable(it);
            }
            return null;
        }

        #endregion
        //----------------------------------------------------------
        #region String

        public Task<long> StringAppendAsync(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringAppendAsync(key, RedisConverters.ToRedisValue(value)));
        }
        public Task<long> StringBitCountAsync(string key, long start = 0, long end = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringBitCountAsync(key, start, end));
        }
        public Task<long> StringBitPositionAsync(string key, bool bit, long start = 0, long end = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringBitPositionAsync(key, bit, start, end));
        }
        public Task<long> StringDecrementAsync(string key, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringDecrementAsync(key, value));
        }
        public Task<double> StringDecrementAsync(string key, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringDecrementAsync(key, value));
        }
        public Task<bool> StringGetBitAsync(string key, long offset)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringGetBitAsync(key, offset));
        }
        public Task<IConvertible> StringGetRangeAsync(string key, long start, long end)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringGetRangeAsync(key, start, end).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<IConvertible> StringGetAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringGetAsync(key).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<IConvertible> StringGetSetAsync(string key, object value)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringGetSetAsync(key, RedisConverters.ToRedisValue(value)).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<long> StringIncrementAsync(string key, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringIncrementAsync(key, value));
        }
        public Task<double> StringIncrementAsync(string key, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringIncrementAsync(key, value));
        }
        public Task<long> StringLengthAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.StringLengthAsync(key));
        }
        public Task<bool> StringSetAsync(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringSetAsync(key, RedisConverters.ToRedisValue(value), null, (StackExchange.Redis.When)when, CommandFlags.None));
        }
        public Task<bool> StringSetBitAsync(string key, long offset, bool bit)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringSetBitAsync(key, offset, bit));
        }
        public Task<long> StringSetRangeAsync(string key, long offset, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.StringSetRangeAsync(key, offset, RedisConverters.ToRedisValue(value)).ContinueWith(t => t.GetResultAs<RedisValue, long>()));
        }

        #endregion
        //----------------------------------------------------------
        #region Set

        public Task<bool> SetAddAsync(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SetAddAsync(key, RedisConverters.ToRedisValue(value)));
        }
        public Task<long> SetAddAsync(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SetAddAsync(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public Task<bool> SetContainsAsync(string key, object value)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetContainsAsync(key, RedisConverters.ToRedisValue(value)));
        }
        public Task<long> SetLengthAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetLengthAsync(key));
        }
        public Task<IConvertible[]> SetMembersAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetMembersAsync(key).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => (IConvertible)e);
                }
                return null;
            }));
        }
        public Task<IConvertible> SetPopAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetPopAsync(key).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<IConvertible> SetRandomMemberAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetRandomMemberAsync(key).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<IConvertible[]> SetRandomMembersAsync(string key, long count)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SetRandomMembersAsync(key, count).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => (IConvertible)e);
                }
                return null;
            }));
        }
        public Task<bool> SetRemoveAsync(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SetRemoveAsync(key, RedisConverters.ToRedisValue(value)));
        }
        public Task<long> SetRemoveAsync(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SetRemoveAsync(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }

        #endregion
        //----------------------------------------------------------
        #region List

        public Task<IConvertible> ListGetByIndexAsync(string key, long index)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.ListGetByIndexAsync(key, index).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<long> ListInsertAfterAsync(string key, object pivot, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListInsertAfterAsync(key, RedisConverters.ToRedisValue(pivot), RedisConverters.ToRedisValue(value)));
        }
        public Task<long> ListInsertBeforeAsync(string key, object pivot, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListInsertBeforeAsync(key, RedisConverters.ToRedisValue(pivot), RedisConverters.ToRedisValue(value)));
        }
        public Task<IConvertible> ListLeftPopAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.ListLeftPopAsync(key).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<long> ListLeftPushAsync(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListLeftPushAsync(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public Task<long> ListLeftPushAsync(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListLeftPushAsync(key, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when));
        }
        public Task<long> ListLengthAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.ListLengthAsync(key));
        }
        public Task<IConvertible[]> ListRangeAsync(string key, long start = 0, long stop = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.ListRangeAsync(key, start, stop).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => (IConvertible)e);
                }
                return null;
            }));
        }
        public Task<long> ListRemoveAsync(string key, object value, long count = 0)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListRemoveAsync(key, RedisConverters.ToRedisValue(value), count));
        }
        public Task<IConvertible> ListRightPopAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.ListRightPopAsync(key).ContinueWith(t => (IConvertible)t.GetResultAs()));
        }
        public Task<long> ListRightPushAsync(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListRightPushAsync(key, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when));
        }
        public Task<long> ListRightPushAsync(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListRightPushAsync(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public Task ListSetByIndexAsync(string key, long index, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListSetByIndexAsync(key, index, RedisConverters.ToRedisValue(value)));
        }
        public Task ListTrimAsync(string key, long start, long stop)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.ListTrimAsync(key, start, stop));
        }

        #endregion
        //----------------------------------------------------------
        #region SortedSet

        public Task<bool> SortedSetAddAsync(string key, string member, double score, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetAddAsync(key, member, score, (StackExchange.Redis.When)when));
        }
        public Task<long> SortedSetAddAsync(string key, SortedEntry[] values, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetAddAsync(key, Array.ConvertAll(values, e => new SortedSetEntry(e.Member, e.Score)), (StackExchange.Redis.When)when));
        }
        public Task<double> SortedSetDecrementAsync(string key, string member, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetDecrementAsync(key, member, value));
        }
        public Task<double> SortedSetIncrementAsync(string key, string member, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetIncrementAsync(key, member, value));
        }
        public Task<long> SortedSetLengthAsync(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetLengthAsync(key, min, max, (Exclude)exclude));
        }
        public Task<long> SortedSetLengthByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetLengthByValueAsync(key, min, max, (Exclude)exclude));
        }
        public Task<string[]> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRangeByRankAsync(key, start, stop, (Order)order).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => e.ToString());
                }
                return null;
            }));
        }
        public Task<SortedEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRangeByRankWithScoresAsync(key, start, stop, (Order)order).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => new SortedEntry(e.Element, e.Score));
                }
                return null;
            }));
        }
        public Task<string[]> SortedSetRangeByScoreAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRangeByScoreAsync(key, start, stop, (Exclude)exclude, (Order)order, skip, take).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => e.ToString());
                }
                return null;
            }));
        }
        public Task<SortedEntry[]> SortedSetRangeByScoreWithScoresAsync(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRangeByScoreWithScoresAsync(key, start, stop, (Exclude)exclude, (Order)order, skip, take).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => new SortedEntry(e.Element, e.Score));
                }
                return null;
            }));
        }
        public Task<string[]> SortedSetRangeByValueAsync(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRangeByValueAsync(key, min, max, (Exclude)exclude, skip, take).ContinueWith(t =>
            {
                var members = t.GetResultAs();
                if (members != null)
                {
                    return Array.ConvertAll(members, e => e.ToString());
                }
                return null;
            }));
        }
        public Task<long?> SortedSetRankAsync(string key, string member, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetRankAsync(key, member, (Order)order));
        }
        public Task<bool> SortedSetRemoveAsync(string key, string member)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetRemoveAsync(key, member));
        }
        public Task<long> SortedSetRemoveAsync(string key, string[] members)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetRemoveAsync(key, Array.ConvertAll<string, RedisValue>(members, e => e)));
        }
        public Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetRemoveRangeByRankAsync(key, start, stop));
        }
        public Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetRemoveRangeByScoreAsync(key, start, stop, (Exclude)exclude));
        }
        public Task<long> SortedSetRemoveRangeByValueAsync(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_async.SortedSetRemoveRangeByValueAsync(key, min, max, (Exclude)exclude));
        }
        public Task<double?> SortedSetScoreAsync(string key, string member)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_async.SortedSetScoreAsync(key, member));
        }
        #endregion
        //----------------------------------------------------------
        #region ObjectHash

        public async Task<HashMap<string, ObjectQueryEntry[]>> ObjectBatchQueryAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            if (db_async is IDatabase db)
            {
                var result = await RedisORMFactory.RedisInstance.EvaluateObjectBatchQueryScriptAsync(db, key);
                var results = (RedisResult[])result;
                var table = new HashMap<string, ObjectQueryEntry[]>();
                var cur_entries = new List<ObjectQueryEntry>();
                var cur_key = string.Empty;
                for (int i = 0; i < results.Length; i += 2)
                {
                    var k = results[i];
                    var v = results[i + 1];
                    if (k.ToString() == "-")
                    {
                        if (!string.IsNullOrEmpty(cur_key))
                        {
                            table.Add(cur_key, cur_entries.ToArray());
                        }
                        cur_key = v.ToString();
                        cur_entries.Clear();
                    }
                    else
                    {
                        cur_entries.Add(new ObjectQueryEntry((RedisValue)k, (RedisValue)v));
                    }
                }
                if (!string.IsNullOrEmpty(cur_key))
                {
                    table.Add(cur_key, cur_entries.ToArray());
                }
                return table;
                //                 var server = RedisORMFactory.RedisInstance.RedisServer;
                //                 var keys = server.Keys(db_number, key + "*", 500);
                //                 var batch = db.CreateBatch();
                //                 var tasks = new List<Task<KeyValuePair<string, HashEntry[]>>>();
                // //                 tasks.Add(batch.HashGetAllAsync(key).ContinueWith(t =>
                // //                 {
                // //                     return new KeyValuePair<string, HashEntry[]>(key, t.GetResultAs());
                // //                 }));
                //                 foreach (var skey in keys)
                //                 {
                //                     tasks.Add(batch.HashGetAllAsync(skey).ContinueWith(t =>
                //                     {
                //                         return new KeyValuePair<string, HashEntry[]>(skey, t.GetResultAs());
                //                     }));
                //                 }
                //                 batch.Execute();
                //                 await Task.WhenAll(tasks);
                //                 var table = new HashMap<string, ObjectQueryEntry[]>();
                //                 foreach (var htask in tasks)
                //                 {
                //                     var hash = htask.GetResultAs();
                //                     table.Add(hash.Key, Array.ConvertAll<HashEntry, ObjectQueryEntry>(hash.Value, e => new ObjectQueryEntry(e.Name, e.Value)));
                //                 }
                //                 return table;
            }
            return null;
        }

        public Task<IConvertible[]> ObjectHashQueryFieldsAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.HashKeysAsync(key).ContinueWith(t =>
            {
                var redis_entries = t.GetResultAs();
                if (redis_entries != null) { return Array.ConvertAll(redis_entries, e => (IConvertible)e); }
                return null;
            });
        }
        public Task<ObjectQueryEntry[]> ObjectHashQueryEntriesAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.HashGetAllAsync(key).ContinueWith(t =>
            {
                var redis_entries = t.GetResultAs();
                if (redis_entries != null) { return Array.ConvertAll(redis_entries, e => new ObjectQueryEntry(e.Name, e.Value)); }
                return null;
            });
        }
        public Task<IConvertible> ObjectHashQueryEntryAsync(string key, string fieldName)
        {
            ORMStatistics.LogLoadCall(this);
            return db_async.HashGetAsync(key, fieldName).ContinueWith(t =>
            {
                var redis = t.GetResultAs<RedisValue>();
                return (IConvertible)redis;
            });
        }
        public Task<double> ObjectHashIncrementFieldAsync(string key, string field, double value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.HashIncrementAsync(key, field, value);
        }
        public Task<long> ObjectHashIncrementFieldAsync(string key, string field, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return db_async.HashIncrementAsync(key, field, value);
        }
        public Task<bool> ObjectHashUpdateAsync(string key, ObjectUpdateEntry e)
        {
            ORMStatistics.LogSaveCall(this);
            switch (e.Event)
            {
                case ExecuteEvent.DELETE_KEY:
                    return db_async.KeyDeleteAsync(key);
                case ExecuteEvent.DELETE_FIELD:
                    return db_async.HashDeleteAsync(key, e.FieldName);
                case ExecuteEvent.UPDATE_FIELD:
                    return db_async.HashSetAsync(key, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                case ExecuteEvent.UPDATE_TOP_KEY:
                    return db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                case ExecuteEvent.DELETE_TOP_KEY:
                    return db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString);
            }
            return Task.FromResult(false);
        }
        public async Task<int> ObjectHashBatchUpdateAsync(string key, ICollection<ObjectUpdateEntry> entries)
        {
            if (entries.Count == 0) return 0;
            ORMStatistics.LogSaveCall(this);
            int count = 0;
            {
                var updating = new List<HashEntry>();
                var removing = new List<RedisValue>();
                {
                    //遍历所有字段
                    foreach (var e in entries)
                    {
                        switch (e.Event)
                        {
                            case ExecuteEvent.DELETE_KEY:
                                await db_async.KeyDeleteAsync(key);
                                return count;
                            case ExecuteEvent.DELETE_FIELD:
                                removing.Add(e.FieldName);
                                break;
                            case ExecuteEvent.UPDATE_FIELD:
                                updating.Add(new HashEntry(e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                                break;
                            case ExecuteEvent.UPDATE_TOP_KEY:
                                await db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                                break;
                            case ExecuteEvent.DELETE_TOP_KEY:
                                await db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString);
                                break;
                        }
                    }
                    //删除空字段
                    if (removing.Count > 0)
                    {
                        await db_async.HashDeleteAsync(key, removing.ToArray());
                    }
                    //写入非Mapping数据
                    if (updating.Count > 0)
                    {
                        await db_async.HashSetAsync(key, updating.ToArray());
                    }
                }
            }
            return count;
        }
        public void EnqueueHashBatchUpdate(IObjectTransaction taskQueue, string key, ICollection<ObjectUpdateEntry> entries)
        {
            if (entries.Count == 0) return;
            ORMStatistics.LogSaveCall(this);
            //using (var updating = new List<HashEntry>())
            //using (var removing = new List<RedisValue>())
            {
                //遍历所有字段
                foreach (var e in entries)
                {
                    switch (e.Event)
                    {
                        case ExecuteEvent.DELETE_KEY:
                            if (string.IsNullOrEmpty(e.FieldName))
                            {
                                taskQueue.Enqueue(db_async.KeyDeleteAsync(key));
                                return;
                            }
                            else
                            {
                                taskQueue.Enqueue(db_async.KeyDeleteAsync(e.FieldName));
                            }
                            break;
                        case ExecuteEvent.RENAME_KEY:
                            taskQueue.Enqueue(db_async.KeyRenameAsync(e.FieldName, e.FieldValue.ToString()));
                            break;
                        case ExecuteEvent.DELETE_FIELD:
                            //removing.Add(e.FieldName);
                            taskQueue.Enqueue(db_async.HashDeleteAsync(key, e.FieldName));
                            break;
                        case ExecuteEvent.UPDATE_FIELD:
                            //updating.Add(new HashEntry(e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                            taskQueue.Enqueue(db_async.HashSetAsync(key, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                            break;
                        case ExecuteEvent.UPDATE_TOP_KEY:
                            taskQueue.Enqueue(db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                            break;
                        case ExecuteEvent.DELETE_TOP_KEY:
                            taskQueue.Enqueue(db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString));
                            break;
                    }
                }
                //删除空字段
                //                 if (removing.Count > 0)
                //                 {
                //                     taskQueue.Enqueue(db_async.HashDeleteAsync(key, removing.ToArray()));
                //                 }
                //写入非Mapping数据
                //                 if (updating.Count > 0)
                //                 {
                //                     taskQueue.Enqueue(db_async.HashSetAsync(key, updating.ToArray()));
                //                 }
            }
        }
        public void EnqueueHashUpdate(IObjectTransaction taskQueue, string key, ObjectUpdateEntry e)
        {
            ORMStatistics.LogSaveCall(this);
            switch (e.Event)
            {
                case ExecuteEvent.DELETE_KEY:
                    if (string.IsNullOrEmpty(e.FieldName))
                    {
                        taskQueue.Enqueue(db_async.KeyDeleteAsync(key));
                        return;
                    }
                    else
                    {
                        taskQueue.Enqueue(db_async.KeyDeleteAsync(e.FieldName));
                    }
                    break;
                case ExecuteEvent.RENAME_KEY:
                    taskQueue.Enqueue(db_async.KeyRenameAsync(e.FieldName, e.FieldValue.ToString()));
                    break;
                case ExecuteEvent.DELETE_FIELD:
                    taskQueue.Enqueue(db_async.HashDeleteAsync(key, e.FieldName));
                    break;
                case ExecuteEvent.UPDATE_FIELD:
                    taskQueue.Enqueue(db_async.HashSetAsync(key, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                    break;
                case ExecuteEvent.UPDATE_TOP_KEY:
                    taskQueue.Enqueue(db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                    break;
                case ExecuteEvent.DELETE_TOP_KEY:
                    taskQueue.Enqueue(db_async.HashSetAsync(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString));
                    break;
            }
        }


        #endregion
        //----------------------------------------------------------
        #region Persist

        public Task<object> PersistRecoverAsync(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return RedisDump.PersistRecoverAsync(key);
        }
        public Task<T> PersistRecoverAsync<T>(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return RedisDump.PersistRecoverAsync<T>(key);
        }

        public Task<bool> PersistDumpAsync(string key, object data)
        {
            ORMStatistics.LogSaveCall(this);
            return RedisDump.PersistDumpAsync(key, data);
        }

        public Task<int> PersistDumpsAsync(params ValueTuple<string, object>[] dumps)
        {
            ORMStatistics.LogSaveCall(this);
            return RedisDump.PersistDumpsAsync(dumps);
        }

        #endregion
        //----------------------------------------------------------
    }

    public class RedisDatabase : RedisDatabaseAsync, IMappingDatabase
    {
        public readonly IDatabase db_sync;
        internal RedisDatabase(IMappingAdapter adapter, IDatabase db, int db_num)
            : base(adapter, db, db_num)
        {
            this.db_sync = db;
        }
        //----------------------------------------------------------
        #region Lock
        public bool LockTake(string key, string token, TimeSpan expire)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.LockTake(key, token, expire);
        }
        public bool LockRelease(string key, string token)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.LockRelease(key, token);
        }
        #endregion
        //----------------------------------------------------------
        #region Key

        public long KeyDelete(string[] keys)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyDelete(Array.ConvertAll<string, RedisKey>(keys, e => e));
        }
        public bool KeyDelete(string key)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyDelete(key);
        }
        public byte[] KeyDump(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.KeyDump(key);
        }
        public bool KeyExists(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.KeyExists(key);
        }
        public bool KeyExpire(string key, TimeSpan? expiry)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyExpire(key, expiry);
        }
        public bool KeyExpire(string key, DateTime? expiry)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyExpire(key, expiry);
        }
        public bool KeyPersist(string key)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyPersist(key);
        }
        public string KeyRandom()
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.KeyRandom().ToString();
        }
        public bool KeyRename(string key, string newKey, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.KeyRename(key, newKey, (StackExchange.Redis.When)when);
        }
        public void KeyRestore(string key, byte[] value, TimeSpan? expiry = null)
        {
            ORMStatistics.LogSaveCall(this);
            db_sync.KeyRestore(key, value, expiry);
        }
        public TimeSpan? KeyTimeToLive(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.KeyTimeToLive(key);
        }

        #endregion
        //----------------------------------------------------------
        #region Hash

        public long HashDecrement(string key, string hashField, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.HashDecrement(key, hashField, value));
        }
        public double HashDecrement(string key, string hashField, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.HashDecrement(key, hashField, value);
        }
        public bool HashDelete(string key, string hashField)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.HashDelete(key, hashField));
        }
        public long HashDelete(string key, string[] hashFields)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.HashDelete(key, Array.ConvertAll<string, RedisValue>(hashFields, e => e)));
        }
        public bool HashExists(string key, string hashField)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.HashExists(key, hashField));
        }
        public HashQueryEntry[] HashGetAll(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var entries = db_sync.HashGetAll(key);
            if (entries != null)
            {
                return Array.ConvertAll(entries, (e) => new HashQueryEntry(e.Name, e.Value));
            }
            return null;
        }
        public IConvertible HashGet(string key, string hashField)
        {
            ORMStatistics.LogLoadCall(this);
            var ret = db_sync.HashGet(key, hashField);
            return ret;
        }
        public IConvertible[] HashGet(string key, string[] hashFields)
        {
            ORMStatistics.LogLoadCall(this);
            var entries = db_sync.HashGet(key, Array.ConvertAll<string, RedisValue>(hashFields, (a) => (a)));
            if (entries != null)
            {
                return Array.ConvertAll<RedisValue, IConvertible>(entries, e => e);
            }
            return null;
        }
        public double HashIncrement(string key, string hashField, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.HashIncrement(key, hashField, value));
        }
        public long HashIncrement(string key, string hashField, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.HashIncrement(key, hashField, value));
        }
        public string[] HashKeys(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var entries = db_sync.HashKeys(key);
            if (entries != null)
            {
                return Array.ConvertAll<RedisValue, string>(entries, e => e);
            }
            return null;
        }
        public long HashLength(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.HashLength(key);
        }
        public void HashSet(string key, HashUpdateEntry[] hashFields)
        {
            ORMStatistics.LogSaveCall(this);
            var fields = Array.ConvertAll(hashFields, e => new HashEntry(e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
            db_sync.HashSet(key, fields);
        }
        public bool HashSet(string key, string hashField, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.HashSet(key, hashField, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when);
        }
        public IConvertible[] HashValues(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var entries = db_sync.HashValues(key);
            if (entries != null)
            {
                return Array.ConvertAll<RedisValue, IConvertible>(entries, e => e);
            }
            return null;
        }
        public IEnumerable<HashQueryEntry> HashScan(string key, string pattern = default(string), int pageSize = 250, long cursor = 0L, int pageOffset = 0)
        {
            ORMStatistics.LogLoadCall(this);
            var it = db_sync.HashScan(key, pattern, pageSize, cursor, pageOffset);
            return new HashScanEnumerable(it);
        }


        #endregion
        //----------------------------------------------------------
        #region String

        public long StringAppend(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringAppend(key, RedisConverters.ToRedisValue(value)));
        }
        public long StringBitCount(string key, long start = 0, long end = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringBitCount(key, start, end));
        }
        public long StringBitPosition(string key, bool bit, long start = 0, long end = -1)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringBitPosition(key, bit, start, end));
        }
        public long StringDecrement(string key, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringDecrement(key, value));
        }
        public double StringDecrement(string key, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringDecrement(key, value));
        }
        public bool StringGetBit(string key, long offset)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringGetBit(key, offset));
        }
        public IConvertible StringGetRange(string key, long start, long end)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringGetRange(key, start, end));
        }
        public IConvertible StringGet(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringGet(key));
        }
        public IConvertible StringGetSet(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringGetSet(key, RedisConverters.ToRedisValue(value)));
        }
        public long StringIncrement(string key, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringIncrement(key, value));
        }
        public double StringIncrement(string key, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringIncrement(key, value));
        }
        public long StringLength(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.StringLength(key));
        }
        public bool StringSet(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringSet(key, RedisConverters.ToRedisValue(value), null, (StackExchange.Redis.When)when, CommandFlags.None));
        }
        public bool StringSetBit(string key, long offset, bool bit)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.StringSetBit(key, offset, bit));
        }
        public long StringSetRange(string key, long offset, object value)
        {
            ORMStatistics.LogSaveCall(this);
            var d = db_sync.StringSetRange(key, offset, RedisConverters.ToRedisValue(value));
            return Convert.ToInt64(d);
        }

        #endregion
        //----------------------------------------------------------
        #region Set

        public bool SetAdd(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SetAdd(key, RedisConverters.ToRedisValue(value)));
        }
        public long SetAdd(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SetAdd(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public bool SetContains(string key, object value)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SetContains(key, RedisConverters.ToRedisValue(value)));
        }
        public long SetLength(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SetLength(key));
        }
        public IConvertible[] SetMembers(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SetMembers(key);
            if (members != null)
            {
                return Array.ConvertAll(members, e => (IConvertible)e);
            }
            return null;
        }
        public IConvertible SetPop(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.SetPop(key);
        }
        public IConvertible SetRandomMember(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.SetRandomMember(key);
        }
        public IConvertible[] SetRandomMembers(string key, long count)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SetRandomMembers(key, count);
            if (members != null)
            {
                return Array.ConvertAll(members, e => (IConvertible)e);
            }
            return null;
        }
        public bool SetRemove(string key, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SetRemove(key, RedisConverters.ToRedisValue(value)));
        }
        public long SetRemove(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SetRemove(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }

        #endregion
        //----------------------------------------------------------
        #region List

        public IConvertible ListGetByIndex(string key, long index)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.ListGetByIndex(key, index);
        }
        public long ListInsertAfter(string key, object pivot, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListInsertAfter(key, RedisConverters.ToRedisValue(pivot), RedisConverters.ToRedisValue(value)));
        }
        public long ListInsertBefore(string key, object pivot, object value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListInsertBefore(key, RedisConverters.ToRedisValue(pivot), RedisConverters.ToRedisValue(value)));
        }
        public IConvertible ListLeftPop(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.ListLeftPop(key));
        }
        public long ListLeftPush(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListLeftPush(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public long ListLeftPush(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListLeftPush(key, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when));
        }
        public long ListLength(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.ListLength(key));
        }
        public IConvertible[] ListRange(string key, long start = 0, long stop = -1)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.ListRange(key, start, stop);
            if (members != null)
            {
                return Array.ConvertAll(members, e => (IConvertible)e);
            }
            return null;
        }
        public long ListRemove(string key, object value, long count = 0)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.ListRemove(key, RedisConverters.ToRedisValue(value), count);
        }
        public IConvertible ListRightPop(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.ListRightPop(key);
        }
        public long ListRightPush(string key, object value, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListRightPush(key, RedisConverters.ToRedisValue(value), (StackExchange.Redis.When)when));
        }
        public long ListRightPush(string key, object[] values)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.ListRightPush(key, Array.ConvertAll(values, e => RedisConverters.ToRedisValue(e))));
        }
        public void ListSetByIndex(string key, long index, object value)
        {
            ORMStatistics.LogSaveCall(this);
            db_sync.ListSetByIndex(key, index, RedisConverters.ToRedisValue(value));
        }
        public void ListTrim(string key, long start, long stop)
        {
            ORMStatistics.LogSaveCall(this);
            db_sync.ListTrim(key, start, stop);
        }

        #endregion
        //----------------------------------------------------------
        #region SortedSet

        public bool SortedSetAdd(string key, string member, double score, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetAdd(key, member, score, (StackExchange.Redis.When)when));
        }
        public long SortedSetAdd(string key, SortedEntry[] values, When when = When.Always)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetAdd(key, Array.ConvertAll(values, e => new SortedSetEntry(e.Member, e.Score)), (StackExchange.Redis.When)when));
        }
        public double SortedSetDecrement(string key, string member, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetDecrement(key, member, value));
        }
        public double SortedSetIncrement(string key, string member, double value)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetIncrement(key, member, value));
        }
        public long SortedSetLength(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SortedSetLength(key, min, max, (Exclude)exclude));
        }
        public long SortedSetLengthByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SortedSetLengthByValue(key, min, max, (Exclude)exclude));
        }
        public string[] SortedSetRangeByRank(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SortedSetRangeByRank(key, start, stop, (Order)order);
            if (members != null)
            {
                return Array.ConvertAll(members, e => e.ToString());
            }
            return null;
        }
        public SortedEntry[] SortedSetRangeByRankWithScores(string key, long start = 0, long stop = -1, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SortedSetRangeByRankWithScores(key, start, stop, (Order)order);
            if (members != null)
            {
                return Array.ConvertAll(members, e => new SortedEntry(e.Element, e.Score));
            }
            return null;
        }
        public string[] SortedSetRangeByScore(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SortedSetRangeByScore(key, start, stop, (Exclude)exclude, (Order)order, skip, take);
            if (members != null)
            {
                return Array.ConvertAll(members, e => e.ToString());
            }
            return null;
        }
        public SortedEntry[] SortedSetRangeByScoreWithScores(string key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, SortedExclude exclude = SortedExclude.None, SortedOrder order = SortedOrder.Ascending, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SortedSetRangeByScoreWithScores(key, start, stop, (Exclude)exclude, (Order)order, skip, take);
            if (members != null)
            {
                return Array.ConvertAll(members, e => new SortedEntry(e.Element, e.Score));
            }
            return null;
        }
        public string[] SortedSetRangeByValue(string key, string min = null, string max = null, SortedExclude exclude = SortedExclude.None, long skip = 0, long take = -1)
        {
            ORMStatistics.LogLoadCall(this);
            var members = db_sync.SortedSetRangeByValue(key, min, max, (Exclude)exclude, skip, take);
            if (members != null)
            {
                return Array.ConvertAll(members, e => e.ToString());
            }
            return null;
        }
        public long? SortedSetRank(string key, string member, SortedOrder order = SortedOrder.Ascending)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SortedSetRank(key, member, (Order)order));
        }
        public bool SortedSetRemove(string key, string member)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetRemove(key, member));
        }
        public long SortedSetRemove(string key, string[] members)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetRemove(key, Array.ConvertAll<string, RedisValue>(members, e => e)));
        }
        public long SortedSetRemoveRangeByRank(string key, long start, long stop)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetRemoveRangeByRank(key, start, stop));
        }
        public long SortedSetRemoveRangeByScore(string key, double start, double stop, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetRemoveRangeByScore(key, start, stop, (Exclude)exclude));
        }
        public long SortedSetRemoveRangeByValue(string key, string min, string max, SortedExclude exclude = SortedExclude.None)
        {
            ORMStatistics.LogSaveCall(this);
            return (db_sync.SortedSetRemoveRangeByValue(key, min, max, (Exclude)exclude));
        }
        public double? SortedSetScore(string key, string member)
        {
            ORMStatistics.LogLoadCall(this);
            return (db_sync.SortedSetScore(key, member));
        }
        #endregion
        //----------------------------------------------------------
        #region ObjectHash
        public HashMap<string, ObjectQueryEntry[]> ObjectBatchQuery(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var server = RedisORMFactory.RedisInstance.RedisServer;
            var keys = server.Keys(db_number, key + "*", 500);
            var batch = db_sync.CreateBatch();
            var tasks = new List<Task<KeyValuePair<string, HashEntry[]>>>();
            foreach (var skey in keys)
            {
                var task = batch.HashGetAllAsync(skey).ContinueWith(t =>
                {
                    return new KeyValuePair<string, HashEntry[]>(skey, t.GetResultAs());
                });
                tasks.Add(task);
            }
            batch.Execute();
            Task.WhenAll(tasks).Wait();
            var table = new HashMap<string, ObjectQueryEntry[]>();
            foreach (var htask in tasks)
            {
                var hash = htask.GetResultAs();
                table.Add(hash.Key, Array.ConvertAll<HashEntry, ObjectQueryEntry>(hash.Value, e => new ObjectQueryEntry(e.Name, e.Value)));
            }
            return table;
        }
        public IConvertible[] ObjectHashQueryFields(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var redis_entries = db_sync.HashKeys(key);
            if (redis_entries != null)
            {
                return Array.ConvertAll(redis_entries, e => (IConvertible)e);
            }
            return null;
        }
        public ObjectQueryEntry[] ObjectHashQueryEntries(string key)
        {
            ORMStatistics.LogLoadCall(this);
            var redis_entries = db_sync.HashGetAll(key);
            if (redis_entries != null)
            {
                return Array.ConvertAll(redis_entries, e => new ObjectQueryEntry(e.Name, e.Value));
            }
            return null;
        }
        public IConvertible ObjectHashQueryEntry(string key, string fieldName)
        {
            ORMStatistics.LogLoadCall(this);
            return db_sync.HashGet(key, fieldName);
        }
        public double ObjectHashIncrementField(string key, string field, double value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.HashIncrement(key, field, value);
        }
        public long ObjectHashIncrementField(string key, string field, long value = 1)
        {
            ORMStatistics.LogSaveCall(this);
            return db_sync.HashIncrement(key, field, value);
        }
        public bool ObjectHashUpdate(string key, ObjectUpdateEntry e)
        {
            ORMStatistics.LogSaveCall(this);
            switch (e.Event)
            {
                case ExecuteEvent.DELETE_KEY:
                    return db_sync.KeyDelete(key);
                case ExecuteEvent.DELETE_FIELD:
                    return db_sync.HashDelete(key, e.FieldName);
                case ExecuteEvent.UPDATE_FIELD:
                    return db_sync.HashSet(key, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                case ExecuteEvent.UPDATE_TOP_KEY:
                    return db_sync.HashSet(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                case ExecuteEvent.DELETE_TOP_KEY:
                    return db_sync.HashSet(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString);
            }
            return false;
        }
        public int ObjectHashBatchUpdate(string key, ICollection<ObjectUpdateEntry> entries)
        {
            ORMStatistics.LogSaveCall(this);
            if (entries.Count == 0) return 0;
            int count = 0;
            {
                var updating = new List<HashEntry>();
                var removing = new List<RedisValue>();
                {
                    //遍历所有字段
                    foreach (var e in entries)
                    {
                        switch (e.Event)
                        {
                            case ExecuteEvent.DELETE_KEY:
                                db_sync.KeyDelete(key);
                                return count;
                            case ExecuteEvent.DELETE_FIELD:
                                removing.Add(e.FieldName);
                                break;
                            case ExecuteEvent.UPDATE_FIELD:
                                updating.Add(new HashEntry(e.FieldName, RedisConverters.ToRedisValue(e.FieldValue)));
                                break;
                            case ExecuteEvent.UPDATE_TOP_KEY:
                                db_sync.HashSet(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisConverters.ToRedisValue(e.FieldValue));
                                break;
                            case ExecuteEvent.DELETE_TOP_KEY:
                                db_sync.HashSet(KEY_TOP_MAPPING_KEYS, e.FieldName, RedisValue.EmptyString);
                                break;
                        }
                    }
                    //删除空字段
                    if (removing.Count > 0)
                    {
                        db_sync.HashDelete(key, removing.ToArray());
                    }
                    //写入非Mapping数据
                    if (updating.Count > 0)
                    {
                        db_sync.HashSet(key, updating.ToArray());
                    }
                }
            }
            return count;
        }


        #endregion
        //----------------------------------------------------------
        #region Persist

        public object PersistRecover(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return RedisDump.PersistRecover(key);
        }
        public T PersistRecover<T>(string key)
        {
            ORMStatistics.LogLoadCall(this);
            return RedisDump.PersistRecover<T>(key);
        }
        public bool PersistDump(string key, object data)
        {
            ORMStatistics.LogSaveCall(this);
            return RedisDump.PersistDump(key, data);
        }
        public int PersistDumps(params ValueTuple<string, object>[] dumps)
        {
            ORMStatistics.LogSaveCall(this);
            return RedisDump.PersistDumps(dumps);
        }

        #endregion
        //----------------------------------------------------------


    }

    public class RedisTransactionDatabase : RedisBatchDatabase, ITransactionDatabase
    {
        public readonly ITransaction trans_db;
        internal RedisTransactionDatabase(RedisDatabaseAsync rd, ITransaction db)
            : base(rd, db)
        {
            this.trans_db = db;
        }
        public override void AddCondition(ICondition cond)
        {
            trans_db.AddCondition(((RedisCondition)cond).cond);
        }
        public override async Task<bool> ExecuteAsync()
        {
            ORMStatistics.LogSaveCall(null);
            var res = trans_db.Execute();
            await Task.WhenAll(tasksQueue);
            return res;
        }
    }

    public class RedisBatchDatabase : RedisDatabaseAsync, ITransactionDatabase
    {
        public readonly IBatch batch_db;
        protected readonly Queue<Task> tasksQueue = new Queue<Task>();
        public int BatchCount => tasksQueue.Count;
        internal RedisBatchDatabase(RedisDatabaseAsync rd, IBatch db)
            : base(rd.adapter, db, rd.db_number)
        {
            this.batch_db = db;
        }
        public virtual void AddCondition(ICondition cond)
        {
            throw new NotImplementedException();
        }
        public void Enqueue(Task task)
        {
            this.tasksQueue.Enqueue(task);
        }
        public void Enqueue(Func<Task> task)
        {
            this.tasksQueue.Enqueue(task());
        }
        public virtual Task<bool> ExecuteAsync()
        {
            ORMStatistics.LogSaveCall(null);
            batch_db.Execute();
            var task = Task.WhenAll(tasksQueue);
            return task.ContinueWith(t => this.Dispose()).ContinueWith(t => true);
        }
    }
}
