using DeepCore.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DeepCore.Reflection;
using DeepCore.Log;
using System.Threading.Tasks;

namespace DeepCore.Lua
{
    public class LuaTemplateLoader : TemplateLoader
    {
        public static bool CHECK_TYPE_ERROR = false;
        public static string KEY_TEXT = "_key_";
        new public static LuaTemplateLoader Instance { get; private set; }
        protected ILuaSystem luaSystem;
        public override string FILE_SUFFIX => ".lua";
        public LuaTemplateLoader(bool instance, ILuaAdapter adapter) : base(instance)
        {
            if (instance)
            {
                LuaTemplateLoader.Instance = this;
            }
            //if (adapter != null)
            this.luaSystem = adapter.CreateLuaSystem(OnOutput, OnError);
            //             else
            //                 this.luaSystem = ILuaAdapter.Instance.CreateLuaSystem(OnOutput, OnError);
        }
        protected override void Disposing()
        {
            luaSystem.Dispose();
        }
        protected void OnOutput(string text)
        {
            log.Info(text);
        }
        protected void OnError(string text)
        {
            log.Error(text);
        }

        public override void GC()
        {
            luaSystem.Update();
        }

        protected override void LoadTemplatesImpl( TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            var count = 0;
            xlsFile = Resource.FormatPath(xlsFile);
            if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in Resource.ListFiles(xlsFile))
                {
                    if (sheetFile.EndsWith(".lua", CUtils.StringComparisonIgnoreCase))
                    {
                        count += LoadTemplatesImpl(center, xlsFile, sheetFile.Substring(0, sheetFile.Length - 4), dataType, keyField, keyType, onCreate, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    count += LoadTemplatesImpl(center, xlsFile, sheetName, dataType, keyField, keyType, onCreate, onLoad);
                }
            }
        }
        protected override async Task LoadTemplatesImplAsync(TemplateDataCenter center, string xlsFile, string[] sheets, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            var count = 0;
            xlsFile = Resource.FormatPath(xlsFile);
            if (sheets == null || sheets.Length == 0)//xleFile目录下按sheetname分了多个文件，没有指定就是读取所有
            {
                foreach (var sheetFile in Resource.ListFiles(xlsFile))
                {
                    if (sheetFile.EndsWith(".lua", CUtils.StringComparisonIgnoreCase))
                    {
                        count += await LoadTemplatesImplAsync(center, xlsFile, sheetFile.Substring(0, sheetFile.Length - 4), dataType, keyField, keyType, onCreate, onLoad);
                    }
                }
            }
            else
            {
                foreach (var sheetName in sheets)
                {
                    count += await LoadTemplatesImplAsync(center, xlsFile, sheetName, dataType, keyField, keyType, onCreate, onLoad);
                }
            }
        }


        protected virtual ValueTuple<ILuaTable, string> LoadLuaFile(string xlsFile, string sheetFile)
        {
            var luaPath = Resource.FormatPath(string.Format("{0}/{1}.lua", xlsFile, sheetFile));
            var txt = Resource.LoadAllText(luaPath);
            if (txt == null)
                throw new Exception("Template File Not Found : " + luaPath);
            try
            {
                var obj = luaSystem.DoString(txt);
                var ret = this.luaSystem.CastToLuaTable(obj);
                return (ret, luaPath);
            }
            catch (Exception e)
            {
                throw new Exception(luaPath + " :" + e.Message, e);
            }
        }
        protected virtual async Task<ValueTuple<ILuaTable, string>> LoadLuaFileAsync(string xlsFile, string sheetFile)
        {
            var luaPath = Resource.FormatPath(string.Format("{0}/{1}.lua", xlsFile, sheetFile));
            var txt = await Resource.LoadAllTextAsync(luaPath);
            if (txt == null)
                throw new Exception("Template File Not Found : " + luaPath);
            try
            {
                var obj = luaSystem.DoString(txt);
                var ret = this.luaSystem.CastToLuaTable(obj);
                return (ret, luaPath);
            }
            catch (Exception e)
            {
                throw new Exception(luaPath + " :" + e.Message, e);
            }
        }

        //         protected void LoadLuaTable(string xlsFile, Action<string, ILuaTable> onLoad)
        //         {
        //             LoadLuaTable(xlsFile, null, onLoad);
        //         }
        //         protected void LoadLuaTable(string xlsFile, string sheetName, Action<string, ILuaTable> onLoad)
        //         {
        //             try
        //             {
        //                 xlsFile = Resource.FormatPath(xlsFile);
        //                 string luaPath = null;
        //                 if (string.IsNullOrEmpty(sheetName))
        //                 {
        //                     foreach (var sheetFile in Resource.ListFiles(xlsFile))
        //                     {
        //                         if (sheetFile.EndsWith(".lua", CUtils.StringComparisonIgnoreCase))
        //                         {
        //                             try
        //                             {
        //                                 sheetName = sheetFile.Substring(0, sheetFile.Length - 4);
        //                                 var table = this.LoadLuaFile(xlsFile, sheetName);
        //                                 onLoad(sheetName, table.Item1);
        //                             }
        //                             catch (Exception err)
        //                             {
        //                                 throw new Exception($"sheetName={sheetName} file={luaPath} : {err.Message}", err);
        //                             }
        //                         }
        //                     }
        //                 }
        //                 else
        //                 {
        //                     try
        //                     {
        //                         var table = this.LoadLuaFile(xlsFile, sheetName);
        //                         onLoad(sheetName, table.Item1);
        //                     }
        //                     catch (Exception err)
        //                     {
        //                         throw new Exception($"sheetName={sheetName} file={luaPath} : {err.Message}", err);
        //                     }
        //                 }
        //             }
        //             catch (Exception err)
        //             {
        //                 throw new Exception($"Load Lua Error : {xlsFile} : {err.Message}", err);
        //             }
        //         }

        protected int LoadTemplatesImpl(TemplateDataCenter center, string xlsFile, string sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            var table = this.LoadLuaFile(xlsFile, sheetName);
            return LoadTemplatesImpl(center, table.Item1, xlsFile, table.Item2, sheetName, dataType, keyField, keyType, onCreate, onLoad);
        }
        protected async Task<int> LoadTemplatesImplAsync(TemplateDataCenter center, string xlsFile, string sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            var table = await this.LoadLuaFileAsync(xlsFile, sheetName);
            return LoadTemplatesImpl(center, table.Item1, xlsFile, table.Item2, sheetName, dataType, keyField, keyType, onCreate, onLoad);
        }

        protected virtual int LoadTemplatesImpl(TemplateDataCenter center, ILuaTable table, string xlsFile, string luaPath, string sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> onCreate, OnLoadTempaletData onLoad)
        {
            int count = 0;
            //var table = this.LoadLuaFile(xlsFile, sheetName, out var luaPath);
            var dtype = DynamicTypeFactory.Instance.GetTypeInfo(dataType);
            var head_map = new HashMap<int, IDynamicFieldInfo>();
            {
                var keyVal = table[KEY_TEXT] as ILuaTable;
                if (keyVal == null) throw new Exception($"No Key Field '{KEY_TEXT}' : Sheet={sheetName} File={xlsFile}");
                foreach (var entry in keyVal.Pairs)
                {
                    var fname = entry.Value.ToString();
                    var fi = dtype.GetField(fname);
                    if (fi != null)
                    {
                        try
                        {
                            head_map.Add(this.LuaValueToObject<int>(entry.Key), fi);
                        }
                        catch (Exception err)
                        {
                            throw new Exception($"Load Lua Head Error : dataType={dataType.FullName} field={fi.Name} entry={entry.Key} value={entry.Value} file={luaPath} : {err.Message}", err);
                        }
                    }
                    else
                    {
                        log.Warn($"Field not found in C# class '{dataType.FullName}.{fname}' : Sheet={sheetName} File={xlsFile}");
                    }
                }
            }
            foreach (var entry in table.Pairs)
            {
                if (entry.Key.ToString() == KEY_TEXT)
                {
                    continue;
                }
                var row = entry.Value as ILuaTable;
                //扫内容并赋值.
                object data = onCreate(dataType);
                object keyValue = null;
                foreach (var cell in row.Pairs)
                {
                    var fi = head_map.Get(this.LuaValueToObject<int>(cell.Key));
                    if (fi != null && cell.Value != null)
                    {
                        try
                        {
                            var value = this.LuaValueToFieldValue(cell.Value, fi);
                            if (CHECK_TYPE_ERROR || value != null)
                            {
                                fi.SetValue(data, value);
                                if (fi.Name == keyField)
                                {
                                    keyValue = value;
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            throw new Exception($"Load Lua Field Error : dataType={dataType.FullName} field={fi.Name} entry={entry.Key} cell={cell.Key} value={cell.Value} file={luaPath} : {err.Message}", err);
                        }
                    }
                }
                onLoad(xlsFile, sheetName, keyValue, data);
                count++;

            }
            return count;
        }

        #region Decode
        public virtual object LuaValueToObject(object luaValue)
        {
            if (luaValue == null)
            {
                return null;
            }
            else if (luaValue is string)
            {
                return luaValue.ToString();
            }
            else if (luaValue is IConvertible)
            {
                return Convert.ChangeType(luaValue, typeof(decimal));
            }
            else if (luaValue is ILuaTable luaTable)
            {
                var map = new HashMap<string, object>(luaTable.Count);
                foreach (var pair in luaTable)
                {
                    map.Add(pair.Key.ToString(), LuaValueToObject(pair.Value));
                }
                return map;
            }
            else
            {
                return luaValue;
            }
        }
        public T LuaValueToObject<T>(object luaValue)
        {
            if (luaValue == null) return default(T);
            return (T)LuaValueToObject(luaValue, typeof(T));
        }
        public virtual object LuaValueToObject(object luaValue, Type objType)
        {
            if (luaValue == null)
            {
                return null;
            }
            else if (objType == typeof(string))
            {
                return luaValue.ToString();
            }
            else if (luaValue is ILuaTable luaTable)
            {
                if (objType.IsArray)
                {
                    return LuaValueToArray(luaTable, objType);
                }
                if (objType.IsInterfaceOf(typeof(IDictionary)))
                {
                    return LuaValueToMap(luaTable, objType);
                }
                if (objType.IsInterfaceOf(typeof(IList)))
                {
                    return LuaValueToList(luaTable, objType);
                }
                if (objType.IsClass)
                {
                    return LuaValueToClass(luaTable, objType, null);
                }
                if (TryConvertToPrimitive(luaTable.Values.First(), objType, out var ret))
                {
                    return ret;
                }
            }
            else if (luaValue is LuaTableMeta.ValueTable valueTable)
            {
                if (objType.IsArray)
                {
                    return LuaValueToArray(valueTable, objType);
                }
                if (objType.IsInterfaceOf(typeof(IList)))
                {
                    return LuaValueToList(valueTable, objType);
                }
            }
            else
            {
                if (TryConvertToPrimitive(luaValue, objType, out var ret))
                {
                    return ret;
                }
            }
            throw new Exception("LuaLoader error:");
        }
        protected virtual bool TryConvertToPrimitive(object luaValue, Type objType, out object ret)
        {
            if (objType.IsEnum)
            {
                try
                {
                    ret = PropertyUtil.GetEnumFromDescription(objType, luaValue.ToString());
                    return true;
                }
                catch
                {
                    var underType = Enum.GetUnderlyingType(objType);
                    var underValue = Convert.ChangeType(luaValue, underType);
                    var name = Enum.GetName(objType, underValue);
                    if (!string.IsNullOrEmpty(name))
                    {
                        ret = Enum.Parse(objType, name);
                        return true;
                    }
                }
            }
            if (luaValue is string strValue)
            {
                if (objType.IsClass && string.IsNullOrEmpty(strValue))
                {
                    ret = null;
                    return true;
                }
                if (Parser.TryStringToObject(strValue, objType, out ret))
                {
                    return true;
                }
            }
            if (luaValue is IConvertible)
            {
                ret = Convert.ChangeType(luaValue, objType);
                return true;
            }
            if (objType.IsPrimitive)
            {
                ret = Convert.ChangeType(luaValue, objType);
                return true;
            }
            ret = null;
            return false;
        }
        public virtual object LuaValueToFieldValue(object luaValue, IDynamicFieldInfo dfield)
        {
            var objType = dfield.Field.FieldType;
            if (dfield.IsDynamicFieldType && luaValue is ILuaTable luaTable)
            {
                return LuaValueToClass(luaTable, objType, dfield.DynamicType);
            }
            return LuaValueToObject(luaValue, objType);
        }
        protected virtual object LuaValueToClass(ILuaTable table, Type decleardType, IDynamicTypeInfo dtype)
        {
            dtype = dtype ?? DynamicTypeFactory.Instance.GetTypeInfo(decleardType);
            var data = dtype.CreateInstance();
            foreach (var cell in table.Pairs)
            {
                if (cell.Key is string cellKey)
                {
                    if (cell.Value != null)
                    {
                        var fi = dtype.GetField(cellKey);
                        if (fi != null)
                        {
                            var value = LuaValueToFieldValue(cell.Value, fi);
                            fi.SetValue(data, value);
                        }
                    }
                }
            }
            return data;
        }
        protected virtual object LuaValueToArray(ILuaTable table, Type decleardType)
        {
            var etype = decleardType.GetElementType();
            var data = Array.CreateInstance(etype, table.Length);
            foreach (var cell in table.Pairs)
            {
                var i = LuaValueToObject<int>(cell.Key) - 1;
                if (cell.Value != null)
                {
                    var evalue = LuaValueToObject(cell.Value, etype);
                    data.SetValue(evalue, i);
                }
                else
                {
                    data.SetValue(null, i);
                }
            }
            return data;
        }
        protected virtual object LuaValueToArray(LuaTableMeta.ValueTable table, Type decleardType)
        {
            var etype = decleardType.GetElementType();
            var data = Array.CreateInstance(etype, table.Count);
            foreach (var cell in table)
            {
                var i = LuaValueToObject<int>(cell.Key) - 1;
                if (cell.Value != null)
                {
                    var evalue = LuaValueToObject(cell.Value, etype);
                    data.SetValue(evalue, i);
                }
                else
                {
                    data.SetValue(null, i);
                }
            }
            return data;
        }
        protected virtual object LuaValueToList(ILuaTable table, Type decleardType)
        {
            var data = (IList)ReflectionUtil.CreateInstance(decleardType);
            var etype = decleardType.GetGenericArguments()[0];
            try
            {
                for (int index = 0; index < table.Count; index++)
                {
                    data.Add(DeepActivator.CreateInstance(etype));
                }
            }
            catch { }
            foreach (var cell in table.Pairs)
            {
                var i = LuaValueToObject<int>(cell.Key) - 1;
                if (cell.Value != null)
                {
                    data[i] = LuaValueToObject(cell.Value, etype);
                }
                else
                {
                    data[i] = null;
                }
            }
            return data;
        }
        protected virtual object LuaValueToList(LuaTableMeta.ValueTable table, Type decleardType)
        {
            var data = (IList)ReflectionUtil.CreateInstance(decleardType);
            var etype = decleardType.GetGenericArguments()[0];
            try
            {
                for (int index = 0; index < table.Count; index++)
                {
                    data.Add(DeepActivator.CreateInstance(etype));
                }
            }
            catch { }
            foreach (var cell in table)
            {
                var i = LuaValueToObject<int>(cell.Key) - 1;
                if (cell.Value != null)
                {
                    data[i] = LuaValueToObject(cell.Value, etype);
                }
                else
                {
                    data[i] = null;
                }
            }
            return data;
        }
        protected virtual object LuaValueToMap(ILuaTable table, Type decleardType)
        {
            var data = (IDictionary)ReflectionUtil.CreateInstance(decleardType);
            var ktype = decleardType.GetGenericArguments()[0];
            var vtype = decleardType.GetGenericArguments()[1];
            foreach (var cell in table.Pairs)
            {
                if (cell.Key is string cellKey)
                {
                    if (cell.Value != null)
                    {
                        var key = Parser.StringToObject(cell.Key.ToString(), ktype);
                        var value = LuaValueToObject(cell.Value, vtype);
                        data[key] = value;
                    }
                }
            }
            return data;
        }

        #endregion
    }

    public class LuaTableMeta
    {
        public LuaTemplateLoader LuaLoader { get; }
        public IReadOnlyDictionary<string, Row> Rows { get => rows; }
        public IReadOnlyDictionary<int, string> HeadIndex { get => head; }

        private readonly HashMap<string, Row> rows;
        private readonly HashMap<int, string> head;

        public LuaTableMeta(LuaTemplateLoader loader, ILuaTable table)
        {
            this.LuaLoader = loader;
            this.rows = new HashMap<string, Row>(table.Count);
            this.head = new HashMap<int, string>();
            {
                var keyVal = table[LuaTemplateLoader.KEY_TEXT] as ILuaTable;
                if (keyVal == null) throw new Exception($"No Key Field '{LuaTemplateLoader.KEY_TEXT}'");
                foreach (var entry in keyVal.Pairs)
                {
                    var column = Convert.ToInt32(entry.Key);
                    var fname = entry.Value.ToString();
                    head.Add(column, fname);
                }
            }
            foreach (var entry in table.Pairs)
            {
                if (entry.Key.ToString() == LuaTemplateLoader.KEY_TEXT)
                {
                    continue;
                }
                this.rows.Add(entry.Key.ToString(), new Row(this, entry.Value as ILuaTable));
            }
        }

        public class Row : IReadOnlyDictionary<string, object>
        {
            private readonly LuaTableMeta parent;
            private readonly HashMap<string, object> cells;
            internal Row(LuaTableMeta meta, ILuaTable row)
            {
                this.parent = meta;
                this.cells = new HashMap<string, object>(row.Count);
                foreach (var cell in row.Pairs)
                {
                    var column = Convert.ToInt32(cell.Key);
                    var fname = meta.head.Get(column);
                    if (cell.Value is ILuaTable cellTable)
                    {
                        cells.Add(fname, new ValueTable(cellTable));
                    }
                    else
                    {
                        cells.Add(fname, cell.Value);
                    }
                }
            }
            public object Get(string fname)
            {
                return cells.Get(fname);
            }
            public object this[string key] => cells[key];
            public int Count => cells.Count;
            public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, object>)cells).Keys;
            public IEnumerable<object> Values => ((IReadOnlyDictionary<string, object>)cells).Values;
            public bool ContainsKey(string key) { return cells.ContainsKey(key); }
            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() { return ((IReadOnlyDictionary<string, object>)cells).GetEnumerator(); }
            public bool TryGetValue(string key, out object value) { return cells.TryGetValue(key, out value); }
            IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyDictionary<string, object>)cells).GetEnumerator(); }
        }
        public class ValueTable : IReadOnlyDictionary<object, object>
        {
            private HashMap<object, object> values;
            private object firstValue;
            internal ValueTable(ILuaTable arrayValue)
            {
                this.values = new HashMap<object, object>(arrayValue.Count);
                foreach (var e in arrayValue)
                {
                    var key = e.Key;
                    if (key.GetType().IsValueType)
                    {
                        key = Convert.ToInt32(key);
                    }
                    else if (key is string)
                    {
                        key = (string)key;
                    }
                    else
                    {
                        throw new Exception($"Unexpect key : {key} type={key.GetType().FullName}");
                    }
                    if (e.Value is ILuaTable vt)
                    {
                        this.values[key] = new ValueTable(vt);
                    }
                    else
                    {
                        this.values[key] = e.Value;
                    }
                    if (firstValue == null)
                    {
                        firstValue = values[key];
                    }
                }
            }
            public object FirstValue { get => firstValue; }
            public object this[object key]
            {
                get
                {
                    return values[key];
                }
            }
            public bool TryGetValue(object key, out object value)
            {
                return values.TryGetValue(key, out value);
            }
            public bool ContainsKey(object key)
            {
                return values.ContainsKey(key);
            }
            public int Count => values.Count;
            public IEnumerable<object> Keys => ((IReadOnlyDictionary<object, object>)values).Keys;
            public IEnumerable<object> Values => ((IReadOnlyDictionary<object, object>)values).Values;
            public IEnumerator<KeyValuePair<object, object>> GetEnumerator() { return ((IReadOnlyDictionary<object, object>)values).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return ((IReadOnlyDictionary<object, object>)values).GetEnumerator(); }
        }
    }
}
