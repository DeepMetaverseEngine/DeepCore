using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepFrozen.MySQL;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using DeepCore.Threading;

namespace DeepCrystal.ORM.Redis
{


    public class RedisDump
    {
        private static Logger log = new LazyLogger("OrmService");
        private static MySQLConnectPool mysql;

        public static int BATCH_DUMP_COUNT = 1000;

        public static string TABLE_MAPPING_OBJECT = "mapping_object";
        public static string CMD_CREATE_MAPPING = @"
CREATE TABLE IF NOT EXISTS `mapping_object` (
  `key` VARCHAR(255) NOT NULL,
  `type` VARCHAR(255) DEFAULT NULL,
  `time` DATETIME DEFAULT NULL,
  `value` LONGTEXT NOT NULL,
  PRIMARY KEY (`key`)
) DEFAULT CHARSET=utf8";

        private static RedisMappingAdapter adapter;
        internal static void MySQLInit(RedisMappingAdapter re, MySQLConnectPool my)
        {
            mysql = my;
            adapter = re;
            using (var auto = mysql.Open())
            {
                try
                {
                    var conn = auto.Connection;
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = CMD_CREATE_MAPPING;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception err)
                {
                    log.Warn(err.Message);
                }
            }
        }

        private static async Task RecordDumpsAsync(DateTime now, params string[] keys)
        {
            try
            {
                var dumpKeys = Array.ConvertAll(keys, key =>
                {
                    return new HashEntry(key, now.ToString());
                });
                await adapter.db.HashSetAsync(RedisDatabase.KEY_TOP_DUMP_KEYS, dumpKeys);
            }
            catch (Exception err)
            {
                log.Warn(err.Message);
            }
        }
        private static async Task<bool> ExistDumpAsync(string key)
        {
            try
            {
                return await adapter.db.HashExistsAsync(RedisDatabase.KEY_TOP_DUMP_KEYS, key);
            }
            catch (Exception err)
            {
                log.Warn(err.Message);
            }
            return false;
        }
        private static async Task EraseDumpAsync(string key)
        {
            try
            {
                await adapter.db.HashDeleteAsync(RedisDatabase.KEY_TOP_DUMP_KEYS, key);
            }
            catch (Exception err)
            {
                log.Warn(err.Message);
            }
        }
        //----------------------------------------------------------------------------------------------------------------

        public static void MaintainceAllDump(TimeSpan keepDuration)
        {
            if (mysql != null && adapter != null)
            {
                var group = BATCH_DUMP_COUNT;
                var keys = new List<string>();
                var total = adapter.db.HashLength(RedisDatabase.KEY_TOP_MAPPING_KEYS);
                var it = adapter.db.HashScan(RedisDatabase.KEY_TOP_MAPPING_KEYS);
                var now = DateTime.UtcNow;
                var progress = new AtomicRangeValue(0, 0, total);
                foreach (var entry in it)
                {
                    if (!entry.Value.IsNullOrEmpty)
                    {
                        var time_utc = RedisConverters.ToObject<DateTime>(entry.Value);
                        if (now - time_utc > keepDuration)
                        {
                            keys.Add(entry.Name.ToString());
                            if (keys.Count >= group)
                            {
                                flush();
                            }
                        }
                    }
                    progress.Add(1);
                }
                flush();
                void flush()
                {
                    MaintainceDumps(now, keepDuration, keys.ToArray());
                    keys.Clear();
                    log.Info($"Dump : {progress}");
                }
            }
        }
        public static Task<int> MaintainceDumpAsync(string key, TimeSpan keepDuration)
        {
            return Task.Run(() => MaintainceDumps(DateTime.UtcNow, keepDuration, key));
        }
        private static int MaintainceDumps(DateTime now, TimeSpan keepDuration, params string[] keys)
        {
            if (keys.Length == 0) return 0;
            if (mysql != null && adapter != null)
            {
                try
                {
                    var exe = ITaskExecutor.Default;
                    var dumps = new List<ValueTuple<MappingReference, DateTime>>();
                    foreach (var key in keys)
                    {
                        var tt = adapter.db.HashGet(key, new RedisValue[] { MappingReference.F_TYPE_FIELD_NAME, MappingReference.F_TIME_FIELD_NAME });
                        if (!tt[0].IsNullOrEmpty && !tt[1].IsNullOrEmpty)
                        {
                            var type_name = RedisConverters.ToObject<string>(tt[0]);
                            var time_utc = RedisConverters.ToObject<DateTime>(tt[1]);
                            if (now - time_utc > keepDuration)
                            {
                                var type = ReflectionUtil.GetType(type_name);
                                if (type != null)
                                {
                                    var mapping = new MappingReference(key, type, exe, adapter);
                                    var data = mapping.LoadDataAsync().WaitForResult();
                                    if (data != null)
                                    {
                                        dumps.Add((mapping, time_utc));
                                        log.Info($"Dump : {mapping.DataType.FullName} : {mapping.Key}");
                                    }
                                }
                            }
                        }
                    }
                    var count = PersistDumps(dumps.ConvertAll(e => (e.Item1.Key, e.Item1.Data)).ToArray());
                    if (count == dumps.Count)
                    {
                        using (var trans = adapter.CreateExecutableObjectTransaction(exe))
                        {
                            foreach (var dump in dumps)
                            {
                                MappingReference mapping = dump.Item1;
                                DateTime time_utc = dump.Item2;
                                if (now - time_utc > keepDuration)
                                {
                                    mapping.BatchSaveData(null, trans);
                                }
                            }
                            trans.ExecuteAsync().Wait();
                        }
                    }
                    return count;
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return 0;
        }


        public static void MaintainceLoadTest()
        {
            if (mysql != null && adapter != null)
            {
                var it = adapter.db.HashScan(RedisDatabase.KEY_TOP_MAPPING_KEYS);
                foreach (var entry in it)
                {
                    var key = entry.Name;
                    PersistRecoverAsync(key).ContinueWithCallback((obj, rst) =>
                    {
                        if (obj != null)
                        {
                            log.Info($"Test Recover : " + key);
                        }
                    });
                }
            }

        }

        //----------------------------------------------------------------------------------------------------------------
        public static async Task<int> PersistDumpsAsync(params ValueTuple<string, object>[] dumps)
        {
            int count = 0;
            if (mysql != null && adapter != null)
            {
                // Dump进MySQL的数据，标记为Dump
                await RecordDumpsAsync(DateTime.UtcNow, Array.ConvertAll(dumps, t => t.Item1));
                try
                {
                    using (var auto = mysql.Open())
                    {
                        var conn = auto.Connection;
                        using (DataTable table = new DataTable())
                        {
                            table.Columns.Add("key", typeof(string));
                            table.Columns.Add("type", typeof(string));
                            table.Columns.Add("time", typeof(DateTime));
                            table.Columns.Add("value", typeof(string));
                            foreach (var e in dumps)
                            {
                                var key = e.Item1;
                                var data = e.Item2;
                                var json = RedisConverters.PersistDumpToJson(data);
                                table.Rows.Add(new object[] { key, data.GetType().FullName, DateTime.UtcNow, json });
                            }
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"REPLACE INTO {TABLE_MAPPING_OBJECT}(`key`,`type`,`time`,`value`) VALUES (@key,@type,@time,@value)";
                                cmd.Parameters.Add("@key", MySqlDbType.VarChar);
                                cmd.Parameters.Add("@type", MySqlDbType.VarChar);
                                cmd.Parameters.Add("@time", MySqlDbType.DateTime);
                                cmd.Parameters.Add("@value", MySqlDbType.LongText);
                                cmd.Parameters["@key"].SourceColumn = "key";
                                cmd.Parameters["@type"].SourceColumn = "type";
                                cmd.Parameters["@time"].SourceColumn = "time";
                                cmd.Parameters["@value"].SourceColumn = "value";
                                using (var adapter = new MySqlDataAdapter())
                                {
                                    adapter.InsertCommand = cmd;
                                    adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
                                    count += await adapter.UpdateAsync(table);
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return count;
        }

        public static int PersistDumps(params ValueTuple<string, object>[] dumps)
        {
            int count = 0;
            if (mysql != null && adapter != null)
            {
                // Dump进MySQL的数据，标记为Dump
                RecordDumpsAsync(DateTime.UtcNow, Array.ConvertAll(dumps, t => t.Item1)).Wait();
                try
                {
                    using (var auto = mysql.Open())
                    {
                        var conn = auto.Connection;
                        using (DataTable table = new DataTable())
                        {
                            table.Columns.Add("key", typeof(string));
                            table.Columns.Add("type", typeof(string));
                            table.Columns.Add("time", typeof(DateTime));
                            table.Columns.Add("value", typeof(string));
                            foreach (var e in dumps)
                            {
                                var key = e.Item1;
                                var data = e.Item2;
                                var json = RedisConverters.PersistDumpToJson(data);
                                table.Rows.Add(new object[] { key, data.GetType().FullName, DateTime.UtcNow, json });
                            }
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"REPLACE INTO {TABLE_MAPPING_OBJECT}(`key`,`type`,`time`,`value`) VALUES(@key,@type,@time,@value)";
                                cmd.Parameters.Add("@key", MySqlDbType.VarChar);
                                cmd.Parameters.Add("@type", MySqlDbType.VarChar);
                                cmd.Parameters.Add("@time", MySqlDbType.DateTime);
                                cmd.Parameters.Add("@value", MySqlDbType.LongText);
                                cmd.Parameters["@key"].SourceColumn = "key";
                                cmd.Parameters["@type"].SourceColumn = "type";
                                cmd.Parameters["@time"].SourceColumn = "time";
                                cmd.Parameters["@value"].SourceColumn = "value";
                                using (var adapter = new MySqlDataAdapter())
                                {
                                    adapter.InsertCommand = cmd;
                                    adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
                                    count += adapter.Update(table);
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return count;
        }
        public static Task<bool> PersistDumpAsync(string key, object data)
        {
            return PersistDumpsAsync((key, data)).ContinueWith(t => t.Result == 1);
        }
        public static bool PersistDump(string key, object data)
        {
            return PersistDumps((key, data)) == 1;
        }

        //----------------------------------------------------------------------------------------------------------------

        public static Task<T> PersistRecoverAsync<T>(string key)
        {
            return PersistRecoverAsync(key).ContinueWith(t => (T)t.Result);
        }
        public static async Task<object> PersistRecoverAsync(string key)
        {
            if (mysql != null && adapter != null)
            {
                // 检测 Dump进MySQL的数据，标记为Dump
                if (await ExistDumpAsync(key))
                {
                    try
                    {

                        using (var auto = await mysql.OpenAsync())
                        {
                            var conn = auto.Connection;
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT * FROM {TABLE_MAPPING_OBJECT} WHERE `key`=@key";
                                cmd.Parameters.AddWithValue("@key", key);
                                using (var adapter = new MySqlDataAdapter())
                                {
                                    adapter.SelectCommand = cmd;
                                    using (DataTable table = new DataTable())
                                    {
                                        table.Columns.Add("key", typeof(string));
                                        table.Columns.Add("type", typeof(string));
                                        table.Columns.Add("time", typeof(DateTime));
                                        table.Columns.Add("value", typeof(string));
                                        if (await adapter.FillAsync(table) == 1)
                                        {
                                            var dataset = table.Rows[0];
                                            var type = dataset["type"].ToString();
                                            var json = dataset["value"].ToString();
                                            var data = RedisConverters.PersistRecoverFromJson(json);
                                            log.Info($"Recover : {type} : {key}");
                                            return (data);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                    }
                    finally
                    {
                        await EraseDumpAsync(key);
                    }
                }
            }
            return (null);
        }
        public static T PersistRecover<T>(string key)
        {
            return (T)PersistRecover(key);
        }
        public static object PersistRecover(string key)
        {
            if (mysql != null && adapter != null)
            {
                // 检测 Dump进MySQL的数据，标记为Dump
                if (ExistDumpAsync(key).WaitForResult())
                {
                    try
                    {
                        using (var auto = mysql.Open())
                        {
                            var conn = auto.Connection;
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT * FROM {TABLE_MAPPING_OBJECT} WHERE `key`=@key";
                                cmd.Parameters.AddWithValue("@key", key);
                                using (var adapter = new MySqlDataAdapter())
                                {
                                    adapter.SelectCommand = cmd;
                                    using (DataTable table = new DataTable())
                                    {
                                        table.Columns.Add("key", typeof(string));
                                        table.Columns.Add("type", typeof(string));
                                        table.Columns.Add("time", typeof(DateTime));
                                        table.Columns.Add("value", typeof(string));
                                        if (adapter.Fill(table) == 1)
                                        {
                                            var dataset = table.Rows[0];
                                            var type = dataset["type"].ToString();
                                            var json = dataset["value"].ToString();
                                            var data = RedisConverters.PersistRecoverFromJson(json);
                                            log.Info($"Recover : {type} : {key}");
                                            return (data);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error(err);
                    }
                    finally
                    {
                        EraseDumpAsync(key).Wait();
                    }
                }
            }
            return (null);
        }
    }
}
