using DeepCore;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Reflection.Modeling;
using DeepMetaGame.Data;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static DeepCore.FuncData.FuncTableGroup;
using static DeepMetaGame.Data.Template.CardTemplate;

namespace DeepCore.FuncData
{

    [MessageType(BattleConstants.FUNC_TABLE_GROUP)]
    [Reflectible]
    public class FuncTableGroup : IFuncTableGroup, IExternalizable
    {
        public FuncTable[] Tables;
        public FuncTableGroup() { }
        public int TablesCount { get => Tables == null ? 0 : Tables.Length; }

        public bool TryGetFuncTable(int tableName, out FuncTable table)
        {
            table = null;
            if (Tables == null) return false;
            table = Array.Find(Tables, e => e.TableName == tableName);
            return table != null;
        }
        public FuncTable GetFuncTable(int tableName)
        {
            if (Tables == null) return null;
            return Array.Find(Tables, e => e.TableName == tableName);
        }

        public bool TryGetFuncField(int tableName, string fieldName, out FuncTable table, out FuncFieldIndex field)
        {
            field = null;
            if (TryGetFuncTable(tableName, out table) && table.TryGetField(fieldName, out field))
            {
                return true;
            }
            return false;
        }
        public bool TryGetFuncField(int tableName, string fieldName, string columnName, out FuncTable table, out FuncFieldIndex field)
        {
            field = null;
            if (TryGetFuncTable(tableName, out table) && table.TryGetField(fieldName, columnName, out field))
            {
                return true;
            }
            return false;
        }
        public bool TryGetFuncColumn(int tableName, string columnName, out FuncTable table, out FuncFieldIndex field)
        {
            field = null;
            if (TryGetFuncTable(tableName, out table) && table.TryGetColumn(columnName, out field))
            {
                return true;
            }
            return false;
        }

        public bool ForEachFields<ST>(ST st, ForEachPredicate<ST, FuncTable, FuncFieldIndex> action)
        {
            if (Tables != null)
            {
                foreach (var table in Tables)
                {
                    if (table.Fields != null)
                    {
                        foreach (var field in table.Fields)
                        {
                            if (action(st, table, field))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return CUtils.ArrayToString(Tables);
        }
        public void ReadExternal(IInputStream input)
        {
            this.Tables = input.GetExtArrayNoHead<FuncTable>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutExtArrayNoHead(this.Tables);
        }
        public class FuncTable : IReadExternalizable, IWriteExternalizable
        {
            [TemplateID(typeof(CardTemplate))]
            public int TableName;
            public FuncFieldIndex[] Fields;

            public int FieldsCount { get => Fields == null ? 0 : Fields.Length; }
            public bool TryGetOrCreate(string fieldName, string columnName, out FuncFieldIndex index)
            {
                if (Fields == null)
                {
                    Fields = new FuncFieldIndex[] {
                        new FuncFieldIndex() {
                        FieldName = fieldName,
                        ColumnName = columnName } };
                    index = Fields[0];
                    return false;
                }
                else if (TryGetField(fieldName, out index))
                {
                    index.ColumnName = columnName;
                    return true;
                }
                else
                {
                    index = new FuncFieldIndex()
                    {
                        FieldName = fieldName,
                        ColumnName = columnName
                    };
                    Fields = Fields.ArrayAppend(index);
                    return false;
                }
            }
            public bool ForEachFields<ST>(ST st, ForEachPredicate<ST, FuncFieldIndex> action)
            {
                if (Fields != null)
                {
                    foreach (var e in Fields)
                    {
                        if (action(st, e))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            public bool TryGetColumn(string columnName, out FuncFieldIndex index)
            {
                if (Fields != null)
                {
                    foreach (var e in Fields)
                    {
                        if (e.ColumnName == columnName)
                        {
                            index = e;
                            return true;
                        }
                    }
                }
                index = null;
                return false;
            }
            public bool TryGetField(string fieldName, out FuncFieldIndex index)
            {
                if (Fields != null)
                {
                    foreach (var e in Fields)
                    {
                        if (e.FieldName == fieldName)
                        {
                            index = e;
                            return true;
                        }
                    }
                }
                index = null;
                return false;
            }
            public bool TryGetField(string fieldName, string columnName, out FuncFieldIndex index)
            {
                if (Fields != null)
                {
                    foreach (var e in Fields)
                    {
                        if (e.ColumnName == columnName && e.FieldName == fieldName)
                        {
                            index = e;
                            return true;
                        }
                    }
                }
                index = null;
                return false;
            }
            public bool TryRemove(FuncFieldIndex index)
            {
                if (Fields != null && Fields.TryIndexOf(index, out var idx))
                {
                    Fields = CUtils.ArrayRemove(Fields, idx);
                    return true;
                }
                return false;
            }
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(TableName);
                if (Fields != null && Fields.Length > 0)
                {
                    sb.Append('(');
                    int index = 0;
                    foreach (var e in Fields)
                    {
                        sb.Append(e.FieldName).Append('[').Append(e.ColumnName).Append(']');
                        index++;
                        if (index < Fields.Length) { sb.Append(','); }
                    }
                    sb.Append(')');
                }
                return sb.ToString();
            }
            public void ReadExternal(IInputStream input)
            {
                TableName = input.GetS32();
                Fields = input.GetArray(static (s) => new FuncFieldIndex() { FieldName = s.GetUTF(), ColumnName = s.GetUTF(), });
            }
            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(TableName);
                output.PutArray(Fields, static (s, o) => { s.PutUTF(o.FieldName); s.PutUTF(o.ColumnName); });
            }
        }
        public class FuncFieldIndex
        {
            public string FieldName;
            public string ColumnName;
            public override string ToString()
            {
                return $"{FieldName}:{ColumnName}";
            }
        }

    }


    [Desc("字段操作符")]
    public enum FieldOperation : byte
    {
        [Desc("=")] SET,
        [Desc("+=")] ADD,
        [Desc("-=")] SUB,
        [Desc("*=")] MUL,
        [Desc("/=")] DIV,
        [Desc("=null")] DEL,
    }

    /// <summary>
    /// 
    /// </summary>
    [MessageType(BattleConstants.FUNC_CARD_AFFECTS)]
    public class CardAffectBindingTemplates : ISerializable
    {
        ///<summary> TemplatesToCard[TempType][TempID] => CardList </summary>
        public HashMap<Type, HashMap<int, List<int>>> TemplatesToCard = new();
        ///<summary> CardToTemplates[CardID][TempType] => TempList </summary>
        public HashMap<int, HashMap<Type, List<int>>> CardToTemplates = new();
    }



    public static class FuncTableUtil
    {
        public static string ToShortString(this FieldOperation op)
        {
            return PropertyUtil.GetEnumDescriptionText(op);
        }
        public static bool HasFuncID(this IFuncData func, out FuncTableGroup ggroup)
        {
            if (func != null && func.Tables is FuncTableGroup group && group.Tables != null)
            {
                ggroup = group;
                return true;
            }
            ggroup = null;
            return false;
        }
        public static bool TryGetFirstFuncFields(this IFuncData func, out FuncTableGroup.FuncTable firstField)
        {
            if (func != null && func.Tables is FuncTableGroup group && group.Tables != null && group.Tables.Length > 0)
            {
                firstField = group.Tables[0];
                return true;
            }
            firstField = null;
            return false;
        }
        public static void WriteFuncID(this IOutputStream output, FuncTableGroup func)
        {
            output.PutBool(func != null);
            if (func != null)
            {
                func.WriteExternal(output);
            }
        }
        public static FuncTableGroup ReadFuncID(this IInputStream input)
        {
            if (input.GetBool())
            {
                var func = new FuncTableGroup();
                func.ReadExternal(input);
                return func;
            }
            return null;
        }

        public static bool ForEachFillFromFuncIDAttribute(object owner, BreakPredicate<IFuncData> action)
        {
            if (owner.GetType().IsGenericList())
            {
                var list = (IEnumerable)owner;
                foreach (var data in list)
                {
                    if (data is IFuncData fundata && fundata.Tables != null)
                    {
                        if (action(fundata)) { return true; }
                    }
                }
            }
            else if (typeof(IFuncData).IsAssignableFrom(owner.GetType()))
            {
                if (owner is IFuncData fundata && fundata.Tables != null)
                {
                    if (action(fundata)) { return true; }
                }
            }
            return false;
        }
        public static bool ForEachFillFromFuncIDAttribute(object owner, BreakPredicate<IFuncData, FuncTableGroup.FuncTable> action)
        {
            if (owner.GetType().IsGenericList())
            {
                var list = (IEnumerable)owner;
                foreach (var data in list)
                {
                    if (data is IFuncData fundata)
                    {
                        if (fundata.HasFuncID(out var group))
                        {
                            foreach (var fields in group.Tables)
                            {
                                if (action(fundata, fields)) { return true; }
                            }
                        }
                    }
                }
            }
            else if (typeof(IFuncData).IsAssignableFrom(owner.GetType()))
            {
                if (owner is IFuncData fundata)
                {
                    if (fundata.HasFuncID(out var group))
                    {
                        foreach (var fields in group.Tables)
                        {
                            if (action(fundata, fields)) { return true; }
                        }
                    }
                }
            }
            return false;
        }
        public static string ToFuncVisibleName(this Type type)
        {
            return type.ToTypeDefineFullName();
        }
        public static bool IsPrimitiveFuncType(this Type objType)
        {
            if (objType.IsPrimitive) return true;
            if (objType == typeof(string)) return true;
            if (objType == typeof(DateTime)) return true;
            if (objType == typeof(TimeSpan)) return true;
            if (objType == typeof(BigInteger)) return true;
            if (objType.IsEnum) return true;
            return objType.IsValueType;
        }
        public static object NewPrimitiveValue(this Type type)
        {
            if (type == null) return null;
            else if (type == (typeof(bool))) return true;
            else if (type == (typeof(byte))) return (byte)0;
            else if (type == (typeof(sbyte))) return (sbyte)0;
            else if (type == (typeof(ushort))) return (ushort)0;
            else if (type == (typeof(short))) return (short)0;
            else if (type == (typeof(uint))) return (uint)0U;
            else if (type == (typeof(int))) return (int)0;
            else if (type == (typeof(ulong))) return (ulong)0UL;
            else if (type == (typeof(long))) return (long)0L;
            else if (type == (typeof(float))) return (float)0f;
            else if (type == (typeof(double))) return (double)0d;
            else if (type == (typeof(decimal))) return (decimal)0m;
            else if (type == (typeof(char))) return (char)'c';
            else if (type == (typeof(string))) return (string)"text";
            else if (type == (typeof(DateTime))) return (DateTime)DateTime.Now;
            else if (type == (typeof(TimeSpan))) return (TimeSpan)TimeSpan.Zero;
            else if (type == (typeof(BigInteger))) return (BigInteger)new BigInteger(0);
            else if (type.IsEnum) Enum.GetValues(type).GetValue(0);
            return null;
        }
        public static readonly Type[] PrimitiveTypes = new Type[] {
            (typeof(bool)),
            (typeof(byte)),
            (typeof(sbyte)),
            (typeof(ushort)),
            (typeof(short)),
            (typeof(uint)),
            (typeof(int)),
            (typeof(ulong)),
            (typeof(long)),
            (typeof(float)),
            (typeof(double)),
            (typeof(decimal)),
            (typeof(char)),
            (typeof(string)),
            (typeof(DateTime)),
            (typeof(TimeSpan)),
            (typeof(BigInteger)),
            (typeof(Enum)),
        };


    }

}
