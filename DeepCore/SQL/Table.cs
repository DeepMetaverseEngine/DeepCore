using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCore.SQL
{

    public class SQLFieldInfo
    {
        public FieldInfo Field { get; internal set; }
        public SQLFieldAttribute FieldAttr { get; internal set; }
        public string FieldName { get => Field.Name; }
        public Type FieldType { get => Field.FieldType; }
        public SQLValueType FieldValueType { get => FieldAttr.ValueType; }
        public override string ToString()
        {
            return this.FieldName;
        }
    }

    public class SQLTableInfo : IEnumerable<SQLFieldInfo>
    {
        public Type DataType { get; private set; }
        public string TableName { get; private set; }
        public SQLTableAttribute TableAttr { get; private set; }
        public SQLFieldInfo PrimaryKey { get; private set; }

        protected List<SQLFieldInfo> fieldsList;
        protected HashMap<string, SQLFieldInfo> fieldsMap;

        public SQLTableInfo(Type type, string table_name)
        {
            this.DataType = type;
            this.TableName = table_name;
            this.TableAttr = PropertyUtil.GetAttribute<SQLTableAttribute>(type);
            var fields = PropertyUtil.GetFieldsWithAttribute<SQLFieldAttribute>(type);
            this.fieldsList = new List<SQLFieldInfo>(Array.ConvertAll(fields, item => new SQLFieldInfo() { Field = item.Item1, FieldAttr = item.Item2 }));
            this.fieldsMap = new HashMap<string, SQLFieldInfo>();
            foreach (var f in fieldsList)
            {
                if (f.FieldAttr.PrimaryKey) { this.PrimaryKey = f; }
                fieldsMap.Add(f.Field.Name, f);
            }
        }
        public override string ToString()
        {
            return TableName;
        }
        public int FieldCount { get => fieldsList.Count; }
        public SQLFieldInfo this[int fieldIndex] { get => this.fieldsList[fieldIndex]; }
        public SQLFieldInfo this[string fieldName] { get => this.fieldsMap[fieldName]; }
        public bool TryGetField(string fieldName, out SQLFieldInfo field)
        {
            return this.fieldsMap.TryGetValue(fieldName, out field);
        }
        public SQLFieldInfo GetField(string fieldName)
        {
            return this.fieldsMap.Get(fieldName);
        }
        public SQLFieldInfo GetField(int fieldIndex)
        {
            return this.fieldsList[fieldIndex];
        }
        public IEnumerator<SQLFieldInfo> GetEnumerator()
        {
            return fieldsList.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return fieldsList.GetEnumerator();
        }
        public SQLFieldInfo[] GetFields(params string[] fields)
        {
            var ret = new SQLFieldInfo[fields.Length];
            CUtils.ForEachLast(fields, this, (st, index, field, last) =>
            {
                if (TryGetField(field, out var tfield))
                {
                    ret[index] = tfield;
                }
                else
                {
                    throw new Exception($"Can not find field `{field}` in `{this}`!");
                }
            });
            return ret;
        }
    }
    public class SQLTableInfo<T, K> : SQLTableInfo
    {
        public Type PrimaryKeyType { get; private set; }
        public SQLTableInfo(string table_name) : base(typeof(T), table_name)
        {
            this.PrimaryKeyType = typeof(K);
        }
    }


    public abstract class SQLFactory
    {
        public static SQLFactory Instance { get; private set; }
        public SQLFactory() { Instance = this; }

        public abstract string ToSQLFieldTypeName(SQLFieldInfo field);

    }
}
