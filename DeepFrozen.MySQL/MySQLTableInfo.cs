using DeepCore.Log;
using DeepCore.Reflection;
using DeepFrozen.MySQL;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.SQL
{
    public static class MySQLTableInfo
    {
        private static Logger log = new LazyLogger("SQLTable");
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region CreateTable
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        public static void InitSQLTable(this MySqlConnection conn, params SQLTableInfo[] tables)
        {
            foreach (var table in tables)
            {
                TryInitSQLTable(table, conn);
                TryAlterSQLTable(table, conn);
            }
        }
        public static void InitSQLTable(this SQLTableInfo table, MySqlConnection conn)
        {
            TryInitSQLTable(table, conn);
            TryAlterSQLTable(table, conn);
        }
        public static bool TryInitSQLTable(this SQLTableInfo table, MySqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillCreateTableCommand(cmd);
                var result = cmd.ExecuteNonQuery();
                if (result != 1)
                {
                    //log.Warn($"Table Alread Exist : {table}");
                    return false;
                }
            }
            return true;
        }
        public static int TryAlterSQLTable(this SQLTableInfo table, MySqlConnection conn)
        {
            return conn.BeginTransactionCommand(cmd =>
            {
                var columns = GetTableColumnsInfo(table, cmd);
                var addList = new List<SQLFieldInfo>();
                var dropList = new List<string>();
                var modList = new List<SQLFieldInfo>();
                {
                    //SHOW FULL COLUMNS FROM 表名 //获取表结构的所有信息（含注释）
                    cmd.CommandText = $"SELECT * FROM {table.TableName} LIMIT 0;";
                    cmd.Prepare();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            if (table.TryGetField(fieldName, out var field))
                            {
                                if (TryModifyTableField(reader, schemaTable, columns, field, i))
                                {
                                    modList.Add(field);
                                }
                            }
                            else
                            {
                                dropList.Add(fieldName);
                            }
                        }
                        foreach (var field in table)
                        {
                            try
                            {
                                reader.GetOrdinal(field.FieldName);
                            }
                            catch
                            {
                                addList.Add(field);
                            }
                        }
                        if (MySQLDriver.Instance.TryGetSchemaPrimaryField(schemaTable, out var primaryKey))
                        {
                            if (table.PrimaryKey != null && primaryKey != table.PrimaryKey.FieldName)
                            {
                                //alter table tablename drop PRIMARY KEY
                                cmd.CommandText = $"ALTER TABLE `{table.TableName}` DROP PRIMARY KEY;";
                                cmd.Prepare();
                                log.Warn(cmd.CommandText);
                                cmd.ExecuteNonQuery();
                                if (!addList.TryFind(t => t.FieldAttr.PrimaryKey, out var addPrimary) && !modList.TryFind(t => t.FieldAttr.PrimaryKey, out var changePrimary))
                                {
                                    cmd.CommandText = $"ALTER TABLE `{table.TableName}` ADD PRIMARY KEY(`{table.PrimaryKey.FieldName}`);";
                                    cmd.Prepare();
                                    log.Warn(cmd.CommandText);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                int ret = addList.Count + dropList.Count + modList.Count;
                if (ret > 0)
                {
                    foreach (var add in addList)
                    {
                        cmd.CommandText = $"ALTER TABLE `{table.TableName}` ADD {add.GetFieldConstraint()};";
                        cmd.Prepare();
                        log.Warn(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var drop in dropList)
                    {
                        cmd.CommandText = $"ALTER TABLE `{table.TableName}` DROP COLUMN `{drop}`;";
                        cmd.Prepare();
                        log.Warn(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var change in modList)
                    {
                        cmd.CommandText = $"ALTER TABLE `{table.TableName}` MODIFY {change.GetFieldConstraint()};";
                        cmd.Prepare();
                        log.Warn(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                }
                return ret;
            });
        }

        public static bool TryModifyTableField(
            MySqlDataReader reader,
            DataTable schemaTable,
            Dictionary<string, ColumnInfo> columns,
            SQLFieldInfo field,
            int column)
        {
            //if (!field.FieldAttr.FieldType.ToString().StringEqualsIgnoreCase(sqlFieldTypeName) && (sqlFieldType != field.FieldType))
            var sqlFieldType = reader.GetFieldType(column);
            var sqlFieldTypeName = reader.GetDataTypeName(column);
            var srcFieldType = MySQLDriver.Instance.GetSQLType(field);
            var srcFieldTypeName = MySQLDriver.Instance.GetFieldTypeName(field, out var len, out var uns);
            if (!srcFieldTypeName.StringEqualsIgnoreCase(sqlFieldTypeName) && (sqlFieldType != srcFieldType))
            {
                return true;
            }
            if (MySQLDriver.Instance.TryGetFieldSchema(schemaTable, MySQLDriver.SchemaColumnKey.ColumnSize, column, out int columnSize))
            {
                if (len > 0)
                {
                    if (columnSize != len)
                    {
                        return true;
                    }
                }
            }
            if (columns.TryGetValue(field.FieldName, out var columnInfo))
            {
                if (columnInfo.IsUniqueKey != field.FieldAttr.UniqueKey)
                {
                    return true;
                }
            }
            return false;
        }
        public class ColumnInfo
        {
            public string Field;
            public string Type;
            public string Collation;
            public string Null;
            public string Key;
            public string Default;
            public string Extra;
            public string Privileges;
            public string Comment;
            public bool IsUniqueKey { get => Key == "UNI"; }
            public bool IsPrimaryKey { get => Key == "PRI"; }
            public bool NotNull { get => Null == "NO"; }
            public string TypeName
            {
                get
                {
                    if (Type.TryIndexOf('(', out var left))
                    {
                        return Type.Substring(0, left);
                    }
                    return Type;
                }
            }
            public int TypeLength
            {
                get
                {
                    var range = TypeLengthRange;
                    return range != null ? range[0] : 0;
                }
            }
            public int[] TypeLengthRange
            {
                get
                {
                    if (Type.TryIndexOf('(', out var left) && Type.TryIndexOf(')', out var right))
                    {
                        var length = Type.Substring(left + 1, right - left - 1);
                        return Array.ConvertAll(length.Split(','), len => Parser.ParseInt(len));
                    }
                    return null;
                }
            }
        }
        public static Dictionary<string, ColumnInfo> GetTableColumnsInfo(this SQLTableInfo table, MySqlCommand cmd)
        {
            cmd.CommandText = $"SHOW FULL COLUMNS FROM {table.TableName};";
            cmd.Prepare();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    var columns = new Dictionary<string, ColumnInfo>();
                    var type = typeof(ColumnInfo);
                    while (reader.Read())
                    {
                        var column = new ColumnInfo();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            var field = type.GetField(fieldName);
                            if (field != null && !reader.IsDBNull(i))
                            {
                                field.SetValue(column, reader.GetString(i));
                            }
                        }
                        columns.Add(column.Field, column);
                    }
                    return columns;
                }
            }
            return null;
        }
        public static string GetFieldConstraint(this SQLFieldInfo field)
        {
            var sb = new StringBuilder();
            var fieldAttr = field.FieldAttr;
            var typeName = MySQLDriver.Instance.GetFieldTypeName(field, out var length, out var unsigned);
            sb.Append($"`{field.FieldName}` {typeName}");
            if (length > 0)
            {
                sb.Append($"({length})");
            }
            if (unsigned)
            {
                sb.Append(" UNSIGNED");
            }
            if (fieldAttr.PrimaryKey)
            {
                sb.Append(" PRIMARY KEY");
            }
            if (fieldAttr.NotNull)
            {
                sb.Append(" NOT NULL");
            }
            if (fieldAttr.AutoIncrement)
            {
                sb.Append(" AUTO_INCREMENT");
            }
            if (fieldAttr.UniqueKey)
            {
                sb.Append(" UNIQUE KEY");
            }
            return sb.ToString();
        }

        public static void FillCreateTableCommand(this SQLTableInfo table, MySqlCommand cmd, bool if_not_exists = true, string default_charset = "utf8")
        {
            var ifNotExist = (if_not_exists ? "IF NOT EXISTS" : string.Empty);
            var defaultCharset = (default_charset != null ? $"DEFAULT CHARSET={default_charset}" : string.Empty);
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {ifNotExist} `{table.TableName}` ");
            sb.AppendLine($"( ");
            CUtils.ForEachLast(0,table.FieldCount, (st,i, last) =>
            {
                sb.Append($"    {table[i].GetFieldConstraint()}");
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($") ");
            if (!string.IsNullOrEmpty(defaultCharset))
            {
                sb.AppendLine($"DEFAULT CHARSET={default_charset}");
            }
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region InsertReplaceDelete
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static void FillInsertCommand(this SQLTableInfo table, MySqlCommand cmd, object data)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO {table.TableName} ");
            sb.AppendLine($"(");
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                sb.AppendLine($"`{field.Field.Name}`");
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
            });
            sb.AppendLine($") VALUES (");
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                sb.AppendLine($"@{i}");
                cmd.Parameters.AddWithValue($"@{i}", field.EncodeSQLValue(field.Field.GetValue(data)));
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($")");
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        public static void FillInsertCommand(this SQLTableInfo table, MySqlCommand cmd, object data, params string[] fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO {table.TableName} ");
            sb.AppendLine($"(");
            fields.ForEachLast(0, (_, i, ff, last) =>
            {
                var field = table.GetField(ff);
                sb.AppendLine($"`{field.Field.Name}`");
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
            });
            sb.AppendLine($") VALUES (");
            fields.ForEachLast(0, (_, i, ff, last) =>
            {
                var field = table.GetField(ff);
                sb.AppendLine($"@{i}");
                cmd.Parameters.AddWithValue($"@{i}", field.EncodeSQLValue(field.Field.GetValue(data)));
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($")");
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        public static void FillReplaceCommand(this SQLTableInfo table, MySqlCommand cmd, object data)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"REPLACE INTO {table.TableName} ");
            sb.AppendLine($"(");
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                sb.AppendLine($"`{field.Field.Name}`");
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($") VALUES (");
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                sb.AppendLine($"@{i}");
                cmd.Parameters.AddWithValue($"@{i}", field.EncodeSQLValue(field.Field.GetValue(data)));
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($")");
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static void FillDeleteCommand(this SQLTableInfo table, MySqlCommand cmd, object primaryKey)
        {
            cmd.CommandText = $"DELETE FROM {table.TableName} WHERE `{table.PrimaryKey.Field.Name}`=@key";
            cmd.Parameters.AddWithValue("@key", table.PrimaryKey.EncodeSQLValue(primaryKey));
            cmd.Prepare();
        }
        public static void FillDeleteCommand(this SQLTableInfo table, MySqlCommand cmd, params Where[] where)
        {
            var sb = new StringBuilder($"DELETE FROM {table.TableName} ");
            table.AppendWhere(sb, cmd, where);
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        public static int Insert(this SQLTableInfo table, MySqlConnection conn, object data)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillInsertCommand(cmd, data);
                return cmd.ExecuteNonQuery();
            }
        }
        public static async Task<int> InsertAsync(this SQLTableInfo table, MySqlConnection conn, object data)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillInsertCommand(cmd, data);
                return await cmd.ExecuteNonQueryAsync();
            }
        }
        public static int Replace(this SQLTableInfo table, MySqlConnection conn, object data)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillReplaceCommand(cmd, data);
                return cmd.ExecuteNonQuery();
            }
        }
        public static async Task<int> ReplaceAsync(this SQLTableInfo table, MySqlConnection conn, object data)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillReplaceCommand(cmd, data);
                return await cmd.ExecuteNonQueryAsync();
            }
        }
        public static int Delete(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillDeleteCommand(cmd, primaryKey);
                return cmd.ExecuteNonQuery();
            }
        }
        public static async Task<int> DeleteAsync(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillDeleteCommand(cmd, primaryKey);
                return await cmd.ExecuteNonQueryAsync();
            }
        }
        public static int Delete(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillDeleteCommand(cmd, where);
                return cmd.ExecuteNonQuery();
            }
        }
        public static async Task<int> DeleteAsync(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillDeleteCommand(cmd, where);
                return await cmd.ExecuteNonQueryAsync();
            }
        }


        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region Update
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static void FillUpdateCommand(this SQLTableInfo table, MySqlCommand cmd, object data)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"UPDATE {table.TableName} SET ");
            int primaryIndex = -1;
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                if (field.FieldAttr.PrimaryKey)
                {
                    primaryIndex = i;
                }
                sb.AppendLine($"`{field.FieldName}`=@{i}");
                cmd.Parameters.AddWithValue($"@{i}", field.EncodeSQLValue(field.Field.GetValue(data)));
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            sb.AppendLine($" WHERE {table.PrimaryKey.FieldName}=@{primaryIndex}");
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        public static void FillUpdateCommand(this SQLTableInfo table, MySqlCommand cmd, object data, params Where[] where)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"UPDATE {table.TableName} SET ");
            CUtils.ForEachLast(table.FieldCount, (i, last) =>
            {
                var field = table[i];
                sb.AppendLine($"`{field.FieldName}`=@i{i}");
                cmd.Parameters.AddWithValue($"@i{i}", field.EncodeSQLValue(field.Field.GetValue(data)));
                if (!last)
                {
                    sb.AppendLine($" , ");
                }
                else
                {
                    sb.AppendLine();
                }
            });
            table.AppendWhere(sb, cmd, where);
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
        }
        public static void FillUpdateFieldsCommand(this SQLTableInfo table, MySqlCommand cmd, object data, params string[] fields)
        {
            var sb = new StringBuilder();
            {
                var primaryKey = table.PrimaryKey;
                sb.AppendLine($"UPDATE {table.TableName} SET ");
                fields.ForEachLast(0,(_,i, field, last) =>
                {
                    var ff = table.GetField(field);
                    sb.AppendLine($"`{field}`=@{i}");
                    cmd.Parameters.AddWithValue($"@{i}", ff.EncodeSQLValue(ff.Field.GetValue(data)));
                    if (!last)
                    {
                        sb.AppendLine($" , ");
                    }
                });
                cmd.Parameters.AddWithValue("@primaryKey", primaryKey.EncodeSQLValue(primaryKey.Field.GetValue(data)));
                sb.AppendLine($" WHERE {table.PrimaryKey.FieldName}=@primaryKey");
                cmd.CommandText = sb.ToString();
                cmd.Prepare();
            }
        }
        public static void FillUpdateFieldsCommand(this SQLTableInfo table, MySqlCommand cmd, object primaryKey, params FieldEntity[] fields)
        {
            var sb = new StringBuilder();
            {
                sb.AppendLine($"UPDATE {table.TableName} SET ");
                fields.ForEachLast(0, (_, i, field, last) =>
                {
                    var ff = table.GetField(field.FieldName);
                    sb.AppendLine($"`{field}`=@{i}");
                    cmd.Parameters.AddWithValue($"@{i}", ff.EncodeSQLValue(field.FieldValue));
                    if (!last)
                    {
                        sb.AppendLine($" , ");
                    }
                });
                cmd.Parameters.AddWithValue("@primaryKey", table.PrimaryKey.EncodeSQLValue(primaryKey));
                sb.AppendLine($" WHERE {table.PrimaryKey.FieldName}=@primaryKey");
                cmd.CommandText = sb.ToString();
                cmd.Prepare();
            }
        }


        public static Task<int> UpdateAsync(this SQLTableInfo table, MySqlConnection conn, object data)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillUpdateCommand(cmd, data);
                return cmd.ExecuteNonQueryAsync();
            }
        }
        public static Task<int> UpdateAsync(this SQLTableInfo table, MySqlConnection conn, object data, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillUpdateCommand(cmd, data, where);
                return cmd.ExecuteNonQueryAsync();
            }
        }
        public static Task<int> UpdateFieldsAsync(this SQLTableInfo table, MySqlConnection conn, object data, params string[] fields)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillUpdateFieldsCommand(cmd, data, fields);
                return cmd.ExecuteNonQueryAsync();
            }
        }
        public static Task<int> UpdateFieldsAsync(this SQLTableInfo table, MySqlConnection conn, object primaryKey, params FieldEntity[] fields)
        {
            using (var cmd = conn.CreateCommand())
            {
                table.FillUpdateFieldsCommand(cmd, primaryKey, fields);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region Select
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        public static DataTable FillSelectCommand(this SQLTableInfo table, MySqlCommand cmd, object primaryKey)
        {
            cmd.CommandText = $"SELECT * FROM {table.TableName} WHERE `{table.PrimaryKey.Field.Name}`=@key";
            cmd.Parameters.AddWithValue("@key", table.PrimaryKey.EncodeSQLValue(primaryKey));
            var dataset = new DataTable();
            foreach (var field in table)
            {
                dataset.Columns.Add(field.FieldName, field.GetSQLType());
            }
            cmd.Prepare();
            return dataset;
        }
        public static DataTable FillSelectCommand(this SQLTableInfo table, MySqlCommand cmd, params Where[] where)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SELECT * FROM {table.TableName} ");
            table.AppendWhere(sb, cmd, where);
            cmd.CommandText = sb.ToString();
            var dataset = new DataTable();
            foreach (var field in table)
            {
                dataset.Columns.Add(field.FieldName, field.GetSQLType());
            }
            cmd.Prepare();
            return dataset;
        }
        public static DataTable FillSelectRowsCommand(this SQLTableInfo table, MySqlCommand cmd, int limit, int offset, params Where[] where)
        {
            var sb = new StringBuilder();
            sb.Append($"SELECT * FROM {table.TableName} ");
            if (where != null && where.Length > 0)
            {
                table.AppendWhere(sb, cmd, where);
            }
            if (limit > 0)
            {
                sb.Append($" LIMIT {limit}");
            }
            if (offset > 0)
            {
                sb.Append($" OFFSET {offset}");
            }
            sb.Append($";");
            cmd.CommandText = sb.ToString();
            var dataset = new DataTable();
            foreach (var field in table)
            {
                dataset.Columns.Add(field.FieldName, field.GetSQLType());
            }
            cmd.Prepare();
            return dataset;
        }
        public static DataTable FillSelectFieldsCommand(this SQLTableInfo table, SQLFieldInfo[] tfields, MySqlCommand cmd, int limit, int offset, params Where[] where)
        {
            var sb = new StringBuilder();
            var dataset = new DataTable();
            sb.Append($"SELECT ");
            CUtils.ForEachLast(tfields, 0, (_, index, field, last) =>
            {
                sb.Append($"{field.FieldName}");
                dataset.Columns.Add(field.FieldName, field.GetSQLType());
                if (!last) sb.Append($",");
            });
            sb.Append($" FROM {table.TableName} ");
            if (where != null && where.Length > 0)
            {
                table.AppendWhere(sb, cmd, where);
            }
            if (limit > 0)
            {
                sb.Append($" LIMIT {limit}");
            }
            if (offset > 0)
            {
                sb.Append($" OFFSET {offset}");
            }
            sb.Append($";");
            cmd.CommandText = sb.ToString();
            cmd.Prepare();
            return dataset;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static T[] FillDataTableRows<T>(this SQLTableInfo table, DataTable dataset)
        {
            var datas = new T[dataset.Rows.Count];
            for (int r = 0; r < dataset.Rows.Count; r++)
            {
                var row = dataset.Rows[r];
                var ret = (T)DeepActivator.CreateInstance(table.DataType);
                foreach (var field in table)
                {
                    var value = row[field.FieldName];
                    if (value == DBNull.Value)
                    {
                        field.Field.SetValue(ret, null);
                    }
                    else
                    {
                        field.Field.SetValue(ret, field.DecodeSQLValue(value));
                    }
                }
                datas[r] = ret;
            }
            return datas;
        }
        public static object[] FillDataTableRows(this SQLTableInfo table, DataTable dataset)
        {
            var datas = new object[dataset.Rows.Count];
            for (int r = 0; r < dataset.Rows.Count; r++)
            {
                var row = dataset.Rows[r];
                var ret = DeepActivator.CreateInstance(table.DataType);
                foreach (var field in table)
                {
                    var value = row[field.FieldName];
                    if (value == DBNull.Value)
                    {
                        field.Field.SetValue(ret, null);
                    }
                    else
                    {
                        field.Field.SetValue(ret, field.DecodeSQLValue(value));
                    }
                }
                datas[r] = ret;
            }
            return datas;
        }
        public static FieldEntity[][] FillDataFieldsRows(SQLFieldInfo[] fields, DataTable dataset)
        {
            var datas = new FieldEntity[dataset.Rows.Count][];
            for (int r = 0; r < dataset.Rows.Count; r++)
            {
                var row = dataset.Rows[r];
                var ret = datas[r] = new FieldEntity[fields.Length];
                CUtils.ForEachLast(fields, 0, (_, index, field, last) =>
                {
                    var value = row[field.FieldName];
                    ret[index] = new FieldEntity()
                    {
                        FieldName = field.FieldName
                    };
                    if (value == DBNull.Value)
                    {
                        ret[index].FieldValue = null;
                    }
                    else
                    {
                        ret[index].FieldValue = field.DecodeSQLValue(value);
                    }
                });
                datas[r] = ret;
            }
            return datas;
        }
        public static bool TryFillDataTable<T>(this SQLTableInfo table, DataTable dataset, out T data)
        {
            var result = FillDataTableRows<T>(table, dataset);
            if (result.Length > 0)
            {
                data = result[0];
                return true;
            }
            data = default(T);
            return false;
        }
        public static bool TryFillDataTable(this SQLTableInfo table, DataTable dataset, out object data)
        {
            var result = FillDataTableRows(table, dataset);
            if (result.Length > 0)
            {
                data = result[0];
                return true;
            }
            data = null;
            return false;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static FieldEntity[][] SelectFields(this SQLTableInfo table, MySqlConnection conn, string[] fields, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var tfields = table.GetFields(fields);
                var dataset = FillSelectFieldsCommand(table, tfields, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) >= 1)
                    {
                        return FillDataFieldsRows(tfields, dataset);
                    }
                }
            }
            return null;
        }
        public static async Task<FieldEntity[][]> SelectFieldsAsync(this SQLTableInfo table, MySqlConnection conn, string[] fields, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var tfields = table.GetFields(fields);
                var dataset = FillSelectFieldsCommand(table, tfields, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) >= 1)
                    {
                        return FillDataFieldsRows(tfields, dataset);
                    }
                }
            }
            return null;
        }
        public static FieldEntity[][] SelectFields(this SQLTableInfo table, MySqlConnection conn, string[] fields, params Where[] where)
        {
            return SelectFields(table, conn, fields, 0, 0, where);
        }
        public static Task<FieldEntity[][]> SelectFieldsAsync(this SQLTableInfo table, MySqlConnection conn, string[] fields, params Where[] where)
        {
            return SelectFieldsAsync(table, conn, fields, 0, 0, where);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        public static long SelectRowCount(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var sb = new StringBuilder($"SELECT COUNT(*) FROM {table.TableName} ");
                if (where != null && where.Length > 0)
                {
                    table.AppendWhere(sb, cmd, where);
                }
                sb.Append($";");
                cmd.Connection = conn;
                cmd.CommandText = sb.ToString();
                cmd.Prepare();
                var result = cmd.ExecuteScalar();
                var count = Convert.ToInt64(result);
                return count;
            }
        }
        public static async Task<long> SelectRowCountAsync(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var sb = new StringBuilder($"SELECT COUNT(*) FROM {table.TableName} ");
                if (where != null && where.Length > 0)
                {
                    table.AppendWhere(sb, cmd, where);
                }
                sb.Append($";");
                cmd.Connection = conn;
                cmd.CommandText = sb.ToString();
                cmd.Prepare();
                var result = await cmd.ExecuteScalarAsync();
                var count = Convert.ToInt64(result);
                return count;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static object[] SelectRows(this SQLTableInfo table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var dataset = FillSelectRowsCommand(table, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) >= 1)
                    {
                        return FillDataTableRows(table, dataset);
                    }
                }
            }
            return null;
        }
        public static T[] SelectRows<T>(this SQLTableInfo table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var dataset = FillSelectRowsCommand(table, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) >= 1)
                    {
                        return FillDataTableRows<T>(table, dataset);
                    }
                }
            }
            return null;
        }
        public static T[] SelectRows<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            return SelectRows<T>(table, conn, limit, offset, where);
        }
        public static async Task<object[]> SelectRowsAsync(this SQLTableInfo table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var dataset = FillSelectRowsCommand(table, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) >= 1)
                    {
                        return FillDataTableRows(table, dataset);
                    }
                }
            }
            return null;
        }
        public static async Task<T[]> SelectRowsAsync<T>(this SQLTableInfo table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                var dataset = FillSelectRowsCommand(table, cmd, limit, offset, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) >= 1)
                    {
                        return FillDataTableRows<T>(table, dataset);
                    }
                }
            }
            return null;
        }
        public static Task<T[]> SelectRowsAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, int limit, int offset, params Where[] where)
        {
            return table.SelectRowsAsync<T>(conn, limit, offset, where);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static object[] SelectRows(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            return SelectRows(table, conn, 0, 0, where);
        }
        public static T[] SelectRows<T>(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            return SelectRows<T>(table, conn, 0, 0, where);
        }
        public static T[] SelectRows<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, params Where[] where)
        {
            return SelectRows<T>(table, conn, where);
        }
        public static Task<object[]> SelectRowsAsync(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            return SelectRowsAsync(table, conn, 0, 0, where);
        }
        public static Task<T[]> SelectRowsAsync<T>(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            return SelectRowsAsync<T>(table, conn, 0, 0, where);
        }
        public static Task<T[]> SelectRowsAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, params Where[] where)
        {
            return table.SelectRowsAsync<T>(conn, where);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static object[] SelectRows(this SQLTableInfo table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRows(table, conn, limit, offset, null);
        }
        public static T[] SelectRows<T>(this SQLTableInfo table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRows<T>(table, conn, limit, offset, null);
        }
        public static T[] SelectRows<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRows<T>(table, conn, limit, offset, null);
        }
        public static Task<object[]> SelectRowsAsync(this SQLTableInfo table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRowsAsync(table, conn, limit, offset, null);
        }
        public static Task<T[]> SelectRowsAsync<T>(this SQLTableInfo table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRowsAsync<T>(table, conn, limit, offset, null);
        }
        public static Task<T[]> SelectRowsAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, int limit = 0, int offset = 0)
        {
            return MySQLTableInfo.SelectRowsAsync<T>(table, conn, limit, offset, null);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static object Select(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, primaryKey);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) == 1)
                    {
                        if (TryFillDataTable(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return null;
        }
        public static T Select<T>(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, primaryKey);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) == 1)
                    {
                        if (TryFillDataTable<T>(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return default(T);
        }
        public static T Select<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, K primaryKey)
        {
            return Select<T>(table, conn, primaryKey);
        }
        public static async Task<object> SelectAsync(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, primaryKey);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) == 1)
                    {
                        if (TryFillDataTable(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return null;
        }
        public static async Task<T> SelectAsync<T>(this SQLTableInfo table, MySqlConnection conn, object primaryKey)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, primaryKey);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) == 1)
                    {
                        if (TryFillDataTable<T>(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return default(T);
        }
        public static Task<T> SelectAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, K primaryKey)
        {
            return SelectAsync<T>(table, conn, primaryKey);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static object Select(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) == 1)
                    {
                        if (TryFillDataTable(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return null;
        }
        public static T Select<T>(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (adapter.Fill(dataset) == 1)
                    {
                        if (TryFillDataTable<T>(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return default(T);
        }
        public static T Select<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, params Where[] where)
        {
            return table.Select<T>(conn, where);
        }
        public static async Task<object> SelectAsync(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) == 1)
                    {
                        if (TryFillDataTable(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return null;
        }
        public static async Task<T> SelectAsync<T>(this SQLTableInfo table, MySqlConnection conn, params Where[] where)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Connection = conn;
                var dataset = MySQLTableInfo.FillSelectCommand(table, cmd, where);
                using (var adapter = new MySqlDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    if (await adapter.FillAsync(dataset) == 1)
                    {
                        if (TryFillDataTable<T>(table, dataset, out var data))
                        {
                            return data;
                        }
                    }
                }
            }
            return default(T);
        }
        public static Task<T> SelectAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, params Where[] where)
        {
            return SelectAsync<T>(table, conn, where);
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------

        public static Task<T> SelectForUpdateAsync<T>(this SQLTableInfo table, MySqlConnection conn, Func<T, Task<T>> update, params Where[] where)
        {
            return conn.BeginTransactionCommandAsync<T>(async cmd =>
            {
                T data = default(T);
                {
                    var dataset = FillSelectCommand(table, cmd, where);
                    cmd.CommandText += " FOR UPDATE;";
                    cmd.Prepare();
                    using (var adapter = new MySqlDataAdapter())
                    {
                        adapter.SelectCommand = cmd;
                        if (await adapter.FillAsync(dataset) >= 1)
                        {
                            if (TryFillDataTable<T>(table, dataset, out var row))
                            {
                                data = await update(row);
                            }
                        }
                    }
                }
                if (data != null)
                {
                    cmd.Parameters.Clear();
                    FillUpdateCommand(table, cmd, data, where);
                    await cmd.ExecuteNonQueryAsync();
                }
                return data;
            });
        }
        public static Task<T> SelectForUpdateAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, Func<T, Task<T>> update, params Where[] where)
        {
            return SelectForUpdateAsync<T>(table, conn, update, where);
        }



        public static Task<T> SelectForUpdateAsync<T>(this SQLTableInfo table, MySqlConnection conn, Func<T, Task<T>> update, object primaryKey)
        {
            return conn.BeginTransactionCommandAsync(async cmd =>
            {
                T data = default(T);
                {
                    var dataset = FillSelectCommand(table, cmd, primaryKey);
                    cmd.CommandText += " FOR UPDATE;";
                    cmd.Prepare();
                    using (var adapter = new MySqlDataAdapter())
                    {
                        adapter.SelectCommand = cmd;
                        if (await adapter.FillAsync(dataset) >= 1)
                        {
                            if (TryFillDataTable<T>(table, dataset, out var row))
                            {
                                data = await update(row);
                            }
                        }
                    }
                }
                if (data != null)
                {
                    cmd.Parameters.Clear();
                    FillUpdateCommand(table, cmd, data);
                    await cmd.ExecuteNonQueryAsync();
                }
                return data;
            });
        }
        public static Task<T> SelectForUpdateAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, Func<T, Task<T>> update, K primaryKey)
        {
            return SelectForUpdateAsync<T>(table, conn, update, primaryKey);
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static Task<long?> SelectForIncrementAsync(this SQLTableInfo table, MySqlConnection conn, string fieldName, long add, object primaryKey)
        {
            return SelectForIncrementAsync(table, conn, fieldName, add, (table.PrimaryKey.FieldName, primaryKey));
        }
        public static Task<long?> SelectForIncrementAsync(this SQLTableInfo table, MySqlConnection conn, string fieldName, long add, params Where[] where)
        {
            return conn.BeginTransactionCommandAsync(async cmd =>
            {
                long? value = null;
                {
                    var field = table.GetField(fieldName);
                    var sb = new StringBuilder();
                    {
                        sb.AppendLine($"SELECT {fieldName} FROM {table.TableName} ");
                        table.AppendWhere(sb, cmd, where);
                        sb.AppendLine($" FOR UPDATE;");
                        cmd.CommandText = sb.ToString();
                        cmd.Prepare();
                    }
                    var data = await cmd.ExecuteScalarAsync();
                    if (data != DBNull.Value)
                    {
                        value = Convert.ToInt64(data);
                        sb.Clear();
                        cmd.Parameters.Clear();
                        sb.AppendLine($"UPDATE {table.TableName} SET `{fieldName}`=@fieldName ");
                        cmd.Parameters.AddWithValue($"@fieldName", field.EncodeSQLValue(value + add));
                        table.AppendWhere(sb, cmd, where);
                        sb.AppendLine($";");
                        cmd.CommandText = sb.ToString();
                        cmd.Prepare();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return value;
            });
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public static Task<T> SelectOrInsertAsync<T>(this SQLTableInfo table, MySqlConnection conn, Func<Task<T>> create, params Where[] where)
        {
            return conn.BeginTransactionCommandAsync(async cmd =>
            {
                T data = default(T);
                {
                    var dataset = FillSelectCommand(table, cmd, where);
                    cmd.Prepare();
                    using (var adapter = new MySqlDataAdapter())
                    {
                        adapter.SelectCommand = cmd;
                        if (await adapter.FillAsync(dataset) >= 1)
                        {
                            TryFillDataTable(table, dataset, out data);
                        }
                    }
                }
                if (data == null)
                {
                    data = await create();
                    cmd.Parameters.Clear();
                    FillInsertCommand(table, cmd, data);
                    await cmd.ExecuteNonQueryAsync();
                }
                return data;
            });
        }
        public static Task<T> SelectOrInsertAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, Func<Task<T>> create, params Where[] where)
        {
            return SelectOrInsertAsync<T>(table, conn, create, where);
        }
        public static Task<T> SelectOrInsertAsync<T, K>(this SQLTableInfo<T, K> table, MySqlConnection conn, Func<K, Task<T>> create, K primaryKey)
        {
            return conn.BeginTransactionCommandAsync(async cmd =>
            {
                T data = default(T);
                {
                    var dataset = FillSelectCommand(table, cmd, primaryKey);
                    cmd.Prepare();
                    using (var adapter = new MySqlDataAdapter())
                    {
                        adapter.SelectCommand = cmd;
                        if (await adapter.FillAsync(dataset) >= 1)
                        {
                            TryFillDataTable<T>(table, dataset, out data);
                        }
                    }
                }
                if (data == null)
                {
                    data = await create(primaryKey);
                    cmd.Parameters.Clear();
                    FillInsertCommand(table, cmd, data);
                    await cmd.ExecuteNonQueryAsync();
                }
                return data;
            });
        }


        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        #region Where
        public static void AppendWhere(this SQLTableInfo table, StringBuilder sb, MySqlCommand cmd, params Where[] where)
        {
            if (where.Length > 0)
            {
                sb.AppendLine($" WHERE ");
                where.ForEachLast(0, (_, i, w, last) =>
                {
                    var ff = table.GetField(w.FieldName);
                    if (ff != null)
                    {
                        sb.AppendLine($"`{w.FieldName}`{w.Operation}@w{i}");
                        cmd.Parameters.AddWithValue($"@w{i}", ff.EncodeSQLValue(w.FieldValue));
                        if (!last)
                        {
                            sb.AppendLine($" AND ");
                        }
                    }
                    else
                    {
                        throw new Exception($"Can not find field :{w.FieldName}");
                    }
                });
            }
        }
        #endregion
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------
    public struct Where
    {
        public string FieldName;
        public string Operation;
        public object FieldValue;
        public Where()
        {
            FieldName = null;
            Operation = "=";
            FieldValue = null;
        }
        public Where(string name, object value)
        {
            FieldName = name;
            Operation = "=";
            FieldValue = value;
        }
        public Where(string name, string op, object value)
        {
            FieldName = name;
            Operation = op;
            FieldValue = value;
        }
        public override string ToString()
        {
            return $"`{FieldName}`{Operation}`{FieldValue}`";
        }

        public static implicit operator Where(in ValueTuple<string, object> value)
        {
            return new Where() { FieldName = value.Item1, FieldValue = value.Item2 };
        }
    }
    public struct FieldEntity
    {
        public string FieldName;
        public object FieldValue;
        public FieldEntity()
        {
            FieldName = null;
            FieldValue = null;
        }
        public FieldEntity(string name, object value)
        {
            FieldName = name;
            FieldValue = value;
        }
        public override string ToString()
        {
            return FieldName;
        }

        public static implicit operator FieldEntity(in ValueTuple<string, object> value)
        {
            return new FieldEntity() { FieldName = value.Item1, FieldValue = value.Item2 };
        }
        public static implicit operator FieldEntity(in KeyValuePair<string, object> value)
        {
            return new FieldEntity() { FieldName = value.Key, FieldValue = value.Value };
        }
    }


}




