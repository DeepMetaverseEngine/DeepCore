using DeepCore;
using DeepCore.IO;
using DeepCore.SQL;
using DeepCrystal.Json;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;

namespace DeepFrozen.MySQL
{

    /*
    ————————————————
    bigint	            long
    bigint unsigned	    ulong
    int	                int
    int unsigned	    uint
    smallint	        short
    smallint unsigned	ushort
    guid	            Guid
    smalldatetime	    DateTime
    date	            DateTime
    datetime	        DateTime
    timestamp	        DateTime
    float	            float
    double	            double
    numeric	            decimal
    smallmoney	        decimal
    decimal	            decimal
    money	            decimal
    bit	                bool
    bool	            bool
    boolean	            bool
    tinyint	            byte
    tinyint unsigned	sbyte
    image	            byte[]
    binary	            byte[]
    blob	            byte[]
    mediumblob	        byte[]
    longblob	        byte[]
    varbinary	        byte[]
    ————————————————

    */
    public class MySQLDriver
    {
        public static MySQLDriver Instance { get; private set; } = new MySQLDriver();

        protected FastJsonParser fastJson = new FastJsonParser();
        protected IOStreamPool binaryCodec;

        public MySQLDriver() { Instance = this; }
        public virtual void SetCodec(IExternalizableFactory codec)
        {
            binaryCodec = new IOStreamPool(codec, false);
        }
        public virtual string GetFieldTypeName(SQLFieldInfo field, out int length, out bool unsigned)
        {
            length = field.FieldAttr.Length;
            unsigned = false;
            var type = field.FieldType;
            if (field.FieldValueType == SQLValueType.Primitive)
            {
                //---------------------------------------------------------------------------
                if (type == (typeof(bool))) { length = 0; return "BOOL"; }
                else if (type == (typeof(byte))) { length = 0; unsigned = true; return "TINYINT"; }
                else if (type == (typeof(sbyte))) { length = 0; unsigned = false; return "TINYINT"; }
                else if (type == (typeof(ushort))) { length = 0; unsigned = true; return "SMALLINT"; }
                else if (type == (typeof(short))) { length = 0; unsigned = false; return "SMALLINT"; }
                else if (type == (typeof(uint))) { length = 0; unsigned = true; return "INT"; }
                else if (type == (typeof(int))) { length = 0; unsigned = false; return "INT"; }
                else if (type == (typeof(ulong))) { length = 0; unsigned = true; return "BIGINT"; }
                else if (type == (typeof(long))) { length = 0; unsigned = false; return "BIGINT"; }
                else if (type == (typeof(char))) { length = 4; return "VARCHAR"; }
                else if (type == (typeof(float))) { length = 0; return "FLOAT"; }
                else if (type == (typeof(double))) { length = 0; return "DOUBLE"; }
                else if (type == (typeof(decimal))) { length = 0; return "DECIMAL(65,30)"; }
                else if (type == (typeof(DateTime))) { length = 0; return "DATETIME(6)"; }
                else if (type == (typeof(TimeSpan))) { length = 0; return "DATETIME(6)"; }
                else if (type.IsEnum) { length = 0; return "INT"; }
                //---------------------------------------------------------------------------
                else if (type == (typeof(string)))
                {
                    return (length > 0) ? "VARCHAR" : "TEXT";
                }
                else if (type == (typeof(byte[])))
                {
                    if (length == 0) { length = 0; return "LONGBLOB"; }
                    if (length < 65536) { length = 0; return "BLOB"; }
                    length = 0; return "LONGBLOB";
                }
                //---------------------------------------------------------------------------
                else if (type == (typeof(BigInteger)))
                {
                    return "BLOB";
                }
                else if (type.IsArray)
                {
                    return (length > 0) ? "VARCHAR" : "LONGTEXT";
                }
                else
                {
                    return (length > 0) ? "VARCHAR" : "TEXT";
                }
            }
            else if (field.FieldValueType == SQLValueType.BinaryObject)
            {
                return "LONGBLOB";
            }
            else if (field.FieldValueType == SQLValueType.JsonObject)
            {
                return "LONGTEXT";
            }
            else if (type.IsClass)
            {
                return "LONGTEXT";
            }
            else
            {
                return "TEXT";
            }
        }
        public virtual Type GetSQLType(SQLFieldInfo field)
        {
            var type = field.FieldType;
            if (field.FieldValueType == SQLValueType.Primitive)
            {
                if (type == (typeof(BigInteger)))
                {
                    return typeof(byte[]);
                }
                else if (type == typeof(byte[]))
                {
                    return typeof(byte[]);
                }
                else if (type == (typeof(TimeSpan)))
                {
                    return typeof(DateTime);
                }
                else if (type.IsArray)
                {
                    return typeof(string);
                }
                return type;
            }
            else if (field.FieldValueType == SQLValueType.BinaryObject)
            {
                return typeof(byte[]);
            }
            else if (field.FieldValueType == SQLValueType.JsonObject)
            {
                return typeof(string);
            }
            else
            {
                return type;
            }
        }
        public virtual object DecodeSQLValue(SQLFieldInfo field, object data)
        {
            if (data == DBNull.Value)
            {
                return null;
            }
            var type = field.FieldType;
            if (field.FieldValueType == SQLValueType.Primitive)
            {
                if (type == (typeof(BigInteger)))
                {
                    if (data != null)
                    {
                        return new BigInteger((byte[])data);
                    }
                    else
                    {
                        return new BigInteger(0);
                    }
                }
                else if (type == typeof(byte[]))
                {
                    return data;
                }
                else if (type == (typeof(TimeSpan)))
                {
                    return new TimeSpan(((DateTime)data).Ticks);
                }
                else if (type.IsArray)
                {
                    return fastJson.DecodeObject(data as string, field.FieldType);
                }
                return data;
            }
            else if (field.FieldValueType == SQLValueType.BinaryObject)
            {
                return binaryCodec.ToBinaryNoHead(data as ISerializable);
            }
            else if (field.FieldValueType == SQLValueType.JsonObject)
            {
                return fastJson.DecodeObject(data as string, field.FieldType);
            }
            else if (type.IsClass)
            {
                return fastJson.DecodeObject(data as string, field.FieldType);
            }
            else
            {
                return data;
            }
        }
        public virtual object EncodeSQLValue(SQLFieldInfo field, object data)
        {
            var type = field.FieldType;
            if (field.FieldValueType == SQLValueType.Primitive)
            {
                if (type == (typeof(BigInteger)))
                {
                    return ((BigInteger)data).ToByteArray();
                }
                else if (type == typeof(byte[]))
                {
                    return data;
                }
                else if (type == (typeof(TimeSpan)))
                {
                    return new DateTime(((TimeSpan)data).Ticks);
                }
                else if (type.IsArray)
                {
                    return fastJson.EncodeObject(data, field.FieldType);
                }
                return data;
            }
            else if (field.FieldValueType == SQLValueType.BinaryObject)
            {
                return binaryCodec.FromBinaryNoHead((byte[])data);
            }
            else if (field.FieldValueType == SQLValueType.JsonObject)
            {
                return fastJson.EncodeObject(data, field.FieldType);
            }
            else if (type.IsClass)
            {
                return fastJson.EncodeObject(data, field.FieldType);
            }
            else
            {
                return data;
            }
        }


        public enum SQLFieldType
        {
            TINYINT = 0x01,
            SMALLINT = 0x02,
            MEDIUMINT = 0x03,
            INT = 0x04, INTEGER = 0x04,
            BIGINT = 0x05,
            FLOAT = 0x06,
            DOUBLE = 0x07,
            DECIMAL = 0x08,

            BOOL = 0x09, BOOLEAN = 0x09,

            DATE = 0x11,
            TIME = 0x12,
            YEAR = 0x13,
            DATETIME = 0x14,
            TIMESTAMP = 0x15,

            CHAR = 0x21,
            VARCHAR = 0x22,
            TINYBLOB = 0x23,
            TINYTEXT = 0x24,
            BLOB = 0x25,
            TEXT = 0x26,
            MEDIUMBLOB = 0x27,
            MEDIUMTEXT = 0x28,
            LONGBLOB = 0x29,
            LONGTEXT = 0x2A,

            BINARY = 0x41,
        }
        public enum SchemaColumnKey
        {
            ColumnName = 0,
            ColumnOrdinal,
            ColumnSize,
            NumericPrecision,
            NumericScale,
            IsUnique,
            IsKey,

            BaseCatalogName,
            BaseColumnName,
            BaseSchemaName,
            BaseTableName,
            DataType,
            AllowDBNull,
            ProviderType,
            IsAliased,
            IsExpression,
            IsIdentity,
            IsAutoIncrement,
            IsRowVersion,
            IsHidden,
            IsLong,
            IsReadOnly,
        }
        public virtual bool TryGetFieldSchema<T>(DataTable schemaTable, SchemaColumnKey key, int column, out T value)
        {
            try
            {
                var schemaField = schemaTable.Rows[column];
                value = (T)schemaField[key.ToString()];
                return true;
            }
            catch { }
            value = default(T);
            return false;
        }
        public virtual bool TryGetSchemaPrimaryField(DataTable schemaTable, out string primaryKey)
        {
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                if (TryGetFieldSchema(schemaTable, SchemaColumnKey.IsKey, i, out bool isKey))
                {
                    if (isKey == true)
                    {
                        primaryKey = schemaTable.Rows[i][SchemaColumnKey.ColumnName.ToString()].ToString();
                        return true;
                    }
                }
            }
            primaryKey = null;
            return false;
        }
    }

    public static class MySQLHelper
    {
        public static object DecodeSQLValue(this SQLFieldInfo field, object data)
        {
            return MySQLDriver.Instance.DecodeSQLValue(field, data);
        }
        public static object EncodeSQLValue(this SQLFieldInfo field, object data)
        {
            return MySQLDriver.Instance.EncodeSQLValue(field, data);
        }
        public static Type GetSQLType(this SQLFieldInfo field)
        {
            return MySQLDriver.Instance.GetSQLType(field);
        }
    }
}
