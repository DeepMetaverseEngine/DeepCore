using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.SQL;
using DeepCrystal;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepFrozen.MySQL.Service
{
    [SQLTable]
    public abstract class TransferRecord
    {
        public abstract object PrimaryKey { get; }
        [SQLField()]
        public int Approved = 0;
        [SQLField()]
        public DateTime Time;
        [SQLField(Length = 256)]
        public string JsonType;
        [SQLField()]
        public string JsonText;
        public object JsonObject;
    }
    public struct QueryRecord<T, K, JSON> where T : TransferRecord
    {
        public T Record;
        public K PrimaryKey { get => (K)Record?.PrimaryKey; }
        public JSON JsonObject { get => (JSON)Record?.JsonObject; }
        public bool HasValue { get => Record != null; }
        public QueryRecord(T r)
        {
            this.Record = r;
        }
    }
    public struct ConsumeRecord<T>
    {
        public MySqlTransaction Transaction;
        public T Record;
        public bool HasValue { get => Record != null; }
        public ConsumeRecord(MySqlTransaction batch, T r)
        {
            this.Transaction = batch;
            this.Record = r;
        }
    }
    public struct ConsumeRecord<T, K, JSON> where T : TransferRecord
    {
        public MySqlTransaction Transaction;
        public T Record;
        public K PrimaryKey { get => (K)Record?.PrimaryKey; }
        public JSON JsonObject { get => (JSON)Record?.JsonObject; }
        public bool HasValue { get => Record != null; }
        public ConsumeRecord(MySqlTransaction batch, T r)
        {
            this.Transaction = batch;
            this.Record = r;
        }
        public static implicit operator ConsumeRecord<T>(in ConsumeRecord<T, K, JSON> r)
        {
            return new ConsumeRecord<T>(r.Transaction, r.Record);
        }
        public static implicit operator ConsumeRecord<T, K, JSON>(in ConsumeRecord<T> r)
        {
            return new ConsumeRecord<T, K, JSON>(r.Transaction, r.Record);
        }
    }
    //---------------------------------------------------------------------------------------------------------------
    public class TransferService<T, K> : Disposable where T : TransferRecord
    {
        public Logger log { get; }
        public MySQLConnectPool MySQL { get; }
        public SQLTableInfo<T, K> TablePending { get; }
        public SQLTableInfo<T, K> TableProcessed { get; }
        public int ConcurrentCount { get; }
        public TransferService(MySQLConnectPool mysql, string table_name, int concurrentCount)
        {
            this.log = LoggerFactory.GetLogger(GetType());
            this.MySQL = mysql;
            this.TablePending = new SQLTableInfo<T, K>(table_name + "_pending");
            this.TableProcessed = new SQLTableInfo<T, K>(table_name + "_processed");
            this.ConcurrentCount = concurrentCount;
            using (var auto = MySQL.Open())
            {
                var conn = auto.Connection;
                conn.InitSQLTable(TablePending, TableProcessed);
            }
        }
        protected override void Disposing()
        {
            _Productor?.Dispose();
            _Approver?.Dispose();
            _Consumer?.Dispose();
        }
        //---------------------------------------------------------------------------------------------------------------
        private readonly HashMap<string, Type> typeMappingS2C = new HashMap<string, Type>();
        public void RegistJsonTypeAlias(string alias, Type type)
        {
            if (!typeMappingS2C.TryGetValue(alias, out var old))
            {
                lock (typeMappingS2C)
                {
                    if (!typeMappingS2C.TryGetValue(alias, out old))
                    {
                        typeMappingS2C.Add(alias, type);
                    }
                }
            }
            else
            {
                if (type != old) throw new Exception($"alias already mapping a type : {alias} = {type}");
            }
        }
        public bool TryGetAliasType(string alias, out Type type)
        {
            if (!typeMappingS2C.TryGetValue(alias, out type))
            {
                lock (typeMappingS2C)
                {
                    if (!typeMappingS2C.TryGetValue(alias, out type))
                    {
                        type = ReflectionUtil.GetType(alias);
                        typeMappingS2C.Add(alias, type);
                    }
                }
            }
            return true;
        }
        //---------------------------------------------------------------------------------------------------------------
        public Where[] WhereApproved(params Where[] where)
        {
            return where.ArrayAppend(WhereApproved());
        }
        public Where[] WhereApproved(Where where)
        {
            return new Where[] { where, WhereApproved() };
        }
        public virtual Where WhereApproved()
        {
            return new Where(nameof(TransferRecord.Approved), 1);
        }
        public virtual FieldEntity[] UpdateApproved()
        {
            return new FieldEntity[] { new FieldEntity(nameof(TransferRecord.Approved), 1) };
        }
        public virtual bool IsApproved(T record)
        {
            return (record.Approved == 1);
        }
        protected virtual Task<bool> TryConsumeAsync(T record)
        {
            return Task.FromResult(true);
        }
        //---------------------------------------------------------------------------------------------------------------
        public virtual object DeserializeJsonObject(T record)
        {
            if (record != null && !string.IsNullOrEmpty(record.JsonText))
            {
                try
                {
                    TryGetAliasType(record.JsonType, out var type);
                    record.JsonObject = JSON.Deserialize(record.JsonText, type);
                    return record.JsonObject;
                }
                catch (Exception err) { log.Error(err); }
            }
            return null;
        }
        public virtual string SerializeJsonObject(T record)
        {
            if (record.JsonObject != null)
            {
                record.JsonText = JSON.Serialize(record.JsonObject);
            }
            return record.JsonText;
        }
        //---------------------------------------------------------------------------------------------------------------
        public async Task<bool> QueryPendingExistAsync(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                return await TablePending.SelectRowCountAsync(conn, where) > 0;
            }
        }
        public async Task<long> QueryPendingCountAsync(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                return await TablePending.SelectRowCountAsync(conn, where);
            }
        }
        public async Task<QueryRecord<T, K, JSON>[]> QueryPendingRowsAsync<JSON>(int limit, int offset, params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var results = await TablePending.SelectRowsAsync<T>(conn, limit, offset, where);
                if (results.IsNotEmpty())
                {
                    return Array.ConvertAll(results, t =>
                    {
                        DeserializeJsonObject(t);
                        return new QueryRecord<T, K, JSON>(t);
                    });
                }
                return new QueryRecord<T, K, JSON>[0];
            }
        }
        public async Task<QueryRecord<T, K, JSON>> QueryPendingAsync<JSON>(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var result = await TablePending.SelectAsync<T>(conn, where);
                if (result != null)
                {
                    DeserializeJsonObject(result);
                }
                return new QueryRecord<T, K, JSON>(result);
            }
        }
        public async Task<QueryRecord<T, K, JSON>> QueryPendingAsync<JSON>(K primaryKey)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var result = await TablePending.SelectAsync<T>(conn, primaryKey);
                if (result != null)
                {
                    DeserializeJsonObject(result);
                }
                return new QueryRecord<T, K, JSON>(result);
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public async Task<bool> QueryProcessedExistAsync(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                return await TableProcessed.SelectRowCountAsync(conn, where) > 0;
            }
        }
        public async Task<long> QueryProcessedCountAsync(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                return await TableProcessed.SelectRowCountAsync(conn, where);
            }
        }
        public async Task<QueryRecord<T, K, JSON>[]> QueryProcessedRowsAsync<JSON>(int limit, int offset, params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var results = await TableProcessed.SelectRowsAsync<T>(conn, limit, offset, where);
                if (results.IsNotEmpty())
                {
                    return Array.ConvertAll(results, t =>
                    {
                        DeserializeJsonObject(t);
                        return new QueryRecord<T, K, JSON>() { Record = t };
                    });
                }
                return new QueryRecord<T, K, JSON>[0];
            }
        }
        public async Task<QueryRecord<T, K, JSON>> QueryProcessedAsync<JSON>(params Where[] where)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var result = await TableProcessed.SelectAsync<T>(conn, where);
                if (result != null)
                {
                    DeserializeJsonObject(result);
                }
                return new QueryRecord<T, K, JSON>(result);
            }
        }
        public async Task<QueryRecord<T, K, JSON>> QueryProcessedAsync<JSON>(K primaryKey)
        {
            using (var conn = await MySQL.OpenAsync())
            {
                var result = await TableProcessed.SelectAsync<T>(conn, primaryKey);
                if (result != null)
                {
                    DeserializeJsonObject(result);
                }
                return new QueryRecord<T, K, JSON>(result);
            }
        }

        //---------------------------------------------------------------------------------------------------------------
        public async Task<QueryRecord<T, K, JSON>> UpdateAsync<JSON>(SQLTableInfo<T, K> table, Func<ConsumeRecord<T, K, JSON>, Task<bool>> handler, params Where[] where)
        {
            using (var auto = await MySQL.OpenAsync())
            {
                using (var batch = auto.Connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = auto.Connection.CreateCommand())
                        {
                            cmd.Connection = auto.Connection;
                            cmd.Transaction = batch;
                            var dataset = table.FillSelectCommand(cmd, where);
                            using (var adapter = new MySqlDataAdapter())
                            {
                                adapter.SelectCommand = cmd;
                                if (await adapter.FillAsync(dataset) == 1)
                                {
                                    if (table.TryFillDataTable<T>(dataset, out var result))
                                    {
                                        DeserializeJsonObject(result);
                                        if (await handler.Invoke(new ConsumeRecord<T, K, JSON>(batch, result)))
                                        {
                                            SerializeJsonObject(result);
                                            cmd.Parameters.Clear();
                                            table.FillUpdateCommand(cmd, result);
                                            await cmd.ExecuteNonQueryAsync();
                                            batch.Commit();
                                        }
                                        else
                                        {
                                            batch.Rollback();
                                        }
                                        return new QueryRecord<T, K, JSON>(result);
                                    }
                                }
                            }
                        }
                        batch.Rollback();
                    }
                    catch
                    {
                        batch.Rollback();
                        throw;
                    }
                }
            }
            return new QueryRecord<T, K, JSON>(null);
        }


        //---------------------------------------------------------------------------------------------------------------
        public abstract class AService : Disposable
        {
            public TransferService<T, K> Service { get; }
            public Logger log { get => Service.log; }
            public MySQLConnectPool MySQL { get => Service.MySQL; }
            public SQLTableInfo<T, K> TablePending { get => Service.TablePending; }
            public SQLTableInfo<T, K> TableProcessed { get => Service.TableProcessed; }
            public AService(TransferService<T, K> svc)
            {
                this.Service = svc;
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class ProductionService : AService
        {
            public ProductionService(TransferService<T, K> svc) : base(svc)
            {
                svc._Productor = this;
            }
            protected override void Disposing()
            { }
            public Task<long> PushProductAsync(T record)
            {
                var alias = record.JsonObject.GetType().FullName;
                return PushProductAsync(alias, record);
            }
            public async Task<long> PushProductAsync(string alias, T record)
            {
                if (await Service.QueryProcessedExistAsync(new Where(Service.TablePending.PrimaryKey.FieldName, record.PrimaryKey)))
                {
                    throw new Exception($"Record already processed : {record.PrimaryKey} : {record}");
                }
                Service.RegistJsonTypeAlias(alias, record.JsonObject.GetType());
                record.Time = DateTime.UtcNow;
                record.JsonType = alias;
                record.JsonText = Service.SerializeJsonObject(record);
                return await Service.MySQL.RunConnectionAsync(async conn =>
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Connection = conn;
                        MySQLTableInfo.FillInsertCommand(Service.TablePending, cmd, record);
                        cmd.Prepare();
                        return await cmd.ExecuteNonQueryAsync();
                    }
                });
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class ApproveService : AService
        {
            public ApproveService(TransferService<T, K> svc) : base(svc)
            {
                svc._Approver = this;
            }
            protected override void Disposing()
            { }
            public virtual async Task<int> ApproveAsync(K primaryKey)
            {
                using (var conn = await Service.MySQL.OpenAsync())
                {
                    return await Service.TablePending.UpdateFieldsAsync(conn, primaryKey, Service.UpdateApproved());
                }
            }
            public virtual async Task<int> ApproveAsync(K primaryKey, params FieldEntity[] fields)
            {
                using (var conn = await Service.MySQL.OpenAsync())
                {
                    fields = CUtils.ArrayAppend(fields, Service.UpdateApproved());
                    return await Service.TablePending.UpdateFieldsAsync(conn, primaryKey, fields);
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class ConsumeService : AService
        {
            public ConsumeService(TransferService<T, K> svc) : base(svc)
            {
                svc._Consumer = this;
            }
            protected override void Disposing()
            {
            }
            protected virtual async Task<int> ConsumeAllInternalAsync(Func<ConsumeRecord<T>, Task<bool>> handler, params Where[] where)
            {
                var processed = 0;
                try
                {
                    if (IsDisposing) { return processed; }
                    using (var conn = await Service.MySQL.OpenAsync())
                    {
                        var limit = Service.ConcurrentCount;
                        var total = await Service.TablePending.SelectRowCountAsync(conn, where);
                        for (int offset = 0; offset < total; offset += limit)
                        {
                            if (IsDisposing) { return processed; }
                            var records = await Service.TablePending.SelectRowsAsync<T>(conn.Connection, limit, offset, where);
                            if (records != null && records.Length > 0)
                            {
                                foreach (var r in records)
                                {
                                    if (IsDisposing) { return processed; }
                                    if (await this.ConsumeInternalAsync(r, handler))
                                    {
                                        processed++;
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
                return processed;
            }
            protected virtual async Task<bool> ConsumeInternalAsync(T record, Func<ConsumeRecord<T>, Task<bool>> handler)
            {
                try
                {
                    if (Service.IsApproved(record))
                    {
                        if (await Service.TryConsumeAsync(record) == false)
                        {
                            return false;
                        }
                        var primary = Service.TableProcessed.PrimaryKey;
                        var primaryKey = primary.DecodeSQLValue(primary.Field.GetValue(record));
                        using (var auto = await Service.MySQL.OpenAsync())
                        {
                            var conn = auto.Connection;
                            using (var batch = conn.BeginTransaction())
                            {
                                try
                                {
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.Transaction = batch;
                                        Service.TableProcessed.FillInsertCommand(cmd, record, Service.TableProcessed.PrimaryKey.FieldName);
                                        var tr1 = await cmd.ExecuteNonQueryAsync();
                                        cmd.Parameters.Clear();
                                        Service.TablePending.FillDeleteCommand(cmd, primaryKey);
                                        var tr2 = await cmd.ExecuteNonQueryAsync();
                                        cmd.Parameters.Clear();
                                        if (tr1 > 0 && tr2 > 0)
                                        {
                                            Service.log.Info($"ConsumeRecordAsync { primaryKey}");
                                            if (record.JsonObject == null)
                                            {
                                                record.JsonObject = Service.DeserializeJsonObject(record);
                                            }
                                            if (await handler.Invoke(new ConsumeRecord<T>(batch, record)))
                                            {
                                                Service.TableProcessed.FillUpdateCommand(cmd, record);
                                                var tr3 = await cmd.ExecuteNonQueryAsync();
                                                cmd.Parameters.Clear();
                                                batch.Commit();
                                                return true;
                                            }
                                        }
                                    }
                                    batch.Rollback();
                                }
                                catch
                                {
                                    batch.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                }
                catch (MySqlException sql_err)
                {
                    if (sql_err.Number != (int)MySqlErrorCode.DuplicateKeyEntry)
                    {
                        Service.log.Error(sql_err);
                    }
                }
                catch (Exception err)
                {
                    Service.log.Error(err);
                }
                return false;
            }

            public Task<int> ConsumeAllAsync<JSON>(Func<ConsumeRecord<T, K, JSON>, Task<bool>> handler, params Where[] where) where JSON : class
            {
                where = where.ArrayAppend(Service.WhereApproved());
                return ConsumeAllInternalAsync((t) => handler(new ConsumeRecord<T, K, JSON>(t.Transaction, t.Record)), where);
            }
            public async Task<JSON> ConsumeAsync<JSON>(Func<ConsumeRecord<T, K, JSON>, Task<bool>> handler, K primaryKey) where JSON : class
            {
                using (var conn = await MySQL.OpenAsync())
                {
                    var result = await TablePending.SelectAsync<T>(conn, primaryKey);
                    if (result != null)
                    {
                        if (await ConsumeInternalAsync(result, (t) => handler(new ConsumeRecord<T, K, JSON>(t.Transaction, t.Record))))
                        {
                            return (JSON)result.JsonObject;
                        }
                    }
                    return null;
                }
            }
            public async Task<JSON> ConsumeAsync<JSON>(Func<ConsumeRecord<T, K, JSON>, Task<bool>> handler, params Where[] where) where JSON : class
            {
                using (var conn = await MySQL.OpenAsync())
                {
                    var result = await TablePending.SelectAsync<T>(conn, where);
                    if (result != null)
                    {
                        if (await ConsumeInternalAsync(result, (t) => handler(new ConsumeRecord<T, K, JSON>(t.Transaction, t.Record))))
                        {
                            return (JSON)result.JsonObject;
                        }
                    }
                    return null;
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class SubscriberConsumeService : ConsumeService
        {
            public SubscriberConsumeService(TransferService<T, K> svc) : base(svc)
            {
                this.thread = new Thread(ConsumeMainThread);
                this.thread.Start();
            }
            protected override void Disposing()
            {
                try { this.thread.Join(); } catch { }
                this.type_txComplete.Clear();
            }
            //---------------------------------------------------------------------------------------------------------------
            private HashMap<string, ConsumeAction> type_txComplete = new HashMap<string, ConsumeAction>();
            public delegate Task<bool> ConsumeAction(ConsumeRecord<T> record);
            public delegate Task<bool> ConsumeAction<JSON>(ConsumeRecord<T, K, JSON> record);
            public void ListenConsumeComplete<JSON>(string alias, ConsumeAction<JSON> action) where JSON : class
            {
                this.ListenConsumeComplete(alias, typeof(JSON), (t) => action(new ConsumeRecord<T, K, JSON>(t.Transaction, t.Record)));
            }
            public void ListenConsumeComplete(string alias, Type statsType, ConsumeAction action)
            {
                Service.RegistJsonTypeAlias(alias, statsType);
                type_txComplete.Add(alias, action);
            }
            public void ListenConsumeComplete<JSON>(ConsumeAction<JSON> action) where JSON : class
            {
                var alias = typeof(JSON).FullName;
                this.ListenConsumeComplete(alias, action);
            }
            public void ListenConsumeComplete(Type statsType, ConsumeAction action)
            {
                var alias = statsType.FullName;
                this.ListenConsumeComplete(alias, statsType, action);
            }
            //---------------------------------------------------------------------------------------------------------------

            //-----------------------------------------------------------------------------------------------------------------------------------
            #region MainThread
            //---------------------------------------------------------------------------------------------------------------
            private Thread thread;
            private void ConsumeMainThread(object state)
            {
                log.Info("Main Thread Start");
                try
                {
                    var where = Service.WhereApproved();
                    while (IsDisposing == false)
                    {
                        try
                        {
                            this.ConsumeAllInternalAsync((t) => Task.FromResult(!IsDisposing), where).Wait();
                        }
                        catch (Exception err)
                        {
                            log.Error(err);
                        }
                        finally
                        {
                            Thread.Sleep(1000);
                            Thread.Yield();
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
                finally
                {
                    log.Info("Main Thread Over");
                }
            }
            protected override Task<int> ConsumeAllInternalAsync(Func<ConsumeRecord<T>, Task<bool>> handler, params Where[] where)
            {
                return base.ConsumeAllInternalAsync(handler, where);
            }
            protected override async Task<bool> ConsumeInternalAsync(T record, Func<ConsumeRecord<T>, Task<bool>> handler)
            {
                if (type_txComplete.TryGetValue(record.JsonType, out var _handler))
                {
                    return await base.ConsumeInternalAsync(record, async (r) =>
                    {
                        if (await handler(r) && await _handler.Invoke(r))
                        {
                            return true;
                        }
                        return false;
                    });
                }
                return false;
            }
            /*

            protected virtual T[] ConsumeSelectPendingRows(MySqlConnection conn, int limit, int offset, params Where[] where)
            {
                return TablePending.SelectRows<T>(conn, where, limit, offset);
            }
            protected virtual bool ConsumeAcceptRecord(T record)
            {
                return record.Approved == 0;
            }

            private void ConsumeMainThread(object state)
            {
                log.Info("Main Thread Start");
                try
                {
                    while (isMainExit == false)
                    {
                        try
                        {
                            using (var conn = MySQL.Open())
                            {
                                var total = TablePending.SelectRowCount(conn);
                                for (int offset = 0; offset < total; offset += concurrentCount)
                                {
                                    var records = this.ConsumeSelectPendingRows(conn, concurrentCount, offset);
                                    if (records != null && records.Length > 0)
                                    {
                                        foreach (var r in records)
                                        {
                                            if (type_txComplete.TryGetValue(r.JsonType, out var handler))
                                            {
                                                if (this.ConsumeAcceptRecord(r))
                                                {
                                                    base.ConsumeRecordAsync(r, (json) =>
                                                    {
                                                        try
                                                        {
                                                            handler.Invoke(json);
                                                        }
                                                        catch (Exception err)
                                                        {
                                                            log.Error(err);
                                                        }
                                                        return Task.CompletedTask;
                                                    }).Wait();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
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
                            Thread.Sleep(1000);
                            Thread.Yield();
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
                finally
                {
                    log.Info("Main Thread Over");
                }
            }



            */

            #endregion

        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        private ProductionService _Productor = null;
        private ApproveService _Approver = null;
        private ConsumeService _Consumer = null;
        public ProductionService Productor
        {
            get { return _Productor; }
        }
        public ApproveService Approver
        {
            get { return _Approver; }
        }
        public ConsumeService Consumer
        {
            get { return _Consumer; }
        }
        public virtual ProductionService CreateProductor()
        {
            return new ProductionService(this);
        }
        public virtual ApproveService CreateApprover()
        {
            return new ApproveService(this);
        }
        public virtual ConsumeService CreateConsumer()
        {
            return new ConsumeService(this);
        }
        public virtual SubscriberConsumeService CreateSubscribeConsumer()
        {
            return new SubscriberConsumeService(this);
        }

        //---------------------------------------------------------------------------------------------------------------
    }



}
