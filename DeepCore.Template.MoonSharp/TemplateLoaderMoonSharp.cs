using DeepCore.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MoonSharp.Interpreter;
using DeepCore.Reflection;

namespace DeepCore.Template.MoonSharp
{

#if TRUE

    public class TemplateLoaderMoonSharp
    {
        public TemplateLoaderMoonSharp()
        {
            new DeepCore.Lua.LuaTemplateLoader(new DeepCore.Template.MoonSharp.MoonSharpLuaAdapter());
        }
    }

#else
    public class TemplateLoaderMoonSharp : TemplateLoader
    {
        static internal DeepCore.Log.Logger log;
        public TemplateLoaderMoonSharp()
        {
        }
        public override ITemplateLoader GetLoader(string file)
        {
            return new LuaLoader(file);
        }
        public override void SetLogger(DeepCore.Log.Logger log)
        {
            TemplateLoaderMoonSharp.log = log;
            Script.DefaultOptions.DebugPrint = s => log.Debug(s);
        }

    }

    class LuaLoader : ITemplateLoader
    {
        public Script Svr { get; private set; }
        protected readonly string xlsFile;
        protected readonly int tableIndex;

        private static Script CreateLuaSvr()
        {
            var ret = new Script();
            return ret;
        }

        public LuaLoader(string xls_file, int tableIndex = 0)
        {
            this.Svr = CreateLuaSvr();

            this.xlsFile = xls_file;
            //             if (xlsFile.ToLower().EndsWith(".xls"))
            //             {
            //                 xlsFile = xlsFile.Substring(0, xlsFile.Length - 4);
            //             }
            //             else if (xlsFile.ToLower().EndsWith(".xlsx"))
            //             {
            //                 xlsFile = xlsFile.Substring(0, xlsFile.Length - 5);
            //             }
            this.tableIndex = tableIndex;
        }
        public virtual void Dispose()
        {
            //Svr.Dispose();
        }

        public HashMap<K, T> LoadTemplates<K, T>(string keyField) where T : new()
        {
            var ret = new HashMap<K, T>();
            int count = LoadTemplates<K, T>(keyField, (s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : " + xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        public List<T> LoadTemplatesAsList<K, T>(string keyField) where T : new()
        {
            var ret = new List<T>();
            int count = LoadTemplates<K, T>(keyField, (s, k, t) =>
            {
                ret.Add(t);
            });
            return ret;
        }
        public HashMap<K, T> LoadTemplates<K, T>(string keyField, string sheetName) where T : new()
        {
            var ret = new HashMap<K, T>();
            int count = LoadTemplates<K, T>(keyField, sheetName + ".lua", (s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : "+xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        public List<T> LoadTemplatesAsList<K, T>(string keyField, string sheetName) where T : new()
        {
            var ret = new List<T>();
            int count = LoadTemplates<K, T>(keyField, sheetName + ".lua", (s, k, t) => { ret.Add(t); });
            return ret;
        }

        protected int LoadTemplates<K, T>(string keyField, Action<string, K, T> action) where T : new()
        {
            var count = 0;
            foreach (var sheetName in Resource.ListFiles(xlsFile))
            {
                if (sheetName.ToLower().EndsWith(".lua"))
                {
                    count += LoadTemplates<K, T>(keyField, sheetName, action);
                }
            }
            return count;
        }
        protected int LoadTemplates<K, T>(string keyField, string sheetFile, Action<string, K, T> action) where T : new()
        {
            int count = 0;
            Table table = LoadLuaFile(sheetFile) as Table;
            Type data_type = typeof(T);
            HashMap<int, FieldInfo> head_map = null;
            {
                var keyVal = table["_key_"] as Table;
                if (keyVal == null) throw new Exception("No Key Field : Sheet=" + sheetFile + " File=" + xlsFile);
                head_map = new HashMap<int, FieldInfo>();
                foreach (var name in keyVal.Pairs)
                {
                    FieldInfo fi = data_type.GetField(name.Value.String);
                    if (fi != null)
                    {
                        head_map.Add(Convert.ToInt32(name.Key.Number), fi);
                    }
                }
            }
            foreach (var entry in table.Pairs)
            {
                if (entry.Key.String == "_key_")
                {
                    continue;
                }
                Table row = entry.Value.Table as Table;
                //扫内容并赋值.
                T data = new T();
                K keyValue = default(K);
                foreach (var cell in row.Pairs)
                {
                    FieldInfo fi = head_map.Get(Convert.ToInt32(cell.Key.Number));
                    if (fi != null && cell.Value != null)
                    {
                        var value = LuaValueToObject(fi.FieldType, cell.Value);
                        fi.SetValue(data, value);
                        if (fi.Name == keyField)
                        {
                            keyValue = (K)value;
                        }
                    }
                }
                action(sheetFile, keyValue, data);
                count++;

            }
            return count;
        }

        private object LuaValueToObject(Type valueType, DynValue cell)
        {
            if (cell.String != null)
            {
                return Parser.StringToObject(cell.String, valueType);
            }
            else if (cell.ToObject() is IConvertible)
            {
                return Convert.ChangeType(cell.ToObject(), valueType);
            }
            else if (cell.Table != null)
            {
                if (valueType.IsArray)
                {
                    return LoadWithArray(valueType, cell.Table);
                }
                else if (valueType.IsClass)
                {
                    if (valueType.GetInterface(typeof(IDictionary).Name) != null)
                    {
                        return LoadWithMap(valueType, cell.Table);
                    }
                    else if (valueType.GetInterface(typeof(IList).Name) != null)
                    {
                        return LoadWithList(valueType, cell.Table);
                    }
                    else
                    {
                        return LoadWithObject(valueType, cell.Table);
                    }
                }
            }
            throw new Exception("LuaLoader error:");
        }


        private object LoadWithObject(Type type, Table table)
        {
            var data = ReflectionUtil.CreateInstance(type);
            foreach (var cell in table.Pairs)
            {
                if (cell.Key.String != null && cell.Value != null)
                {
                    FieldInfo fi = type.GetField(cell.Key.String);
                    if (fi != null)
                    {
                        var value = LuaValueToObject(fi.FieldType, cell.Value);
                        fi.SetValue(data, value);
                    }
                }
            }
            return data;
        }
        private object LoadWithArray(Type type, Table table)
        {
            var etype = type.GetElementType();
            Array data = Array.CreateInstance(etype, table.Length);
            foreach (var cell in table.Pairs)
            {
                var i = Convert.ToInt32(cell.Key.Number) - 1;
                if (cell.Value != null)
                {
                    data.SetValue(LuaValueToObject(etype, cell.Value), i);
                }
                else
                {
                    data.SetValue(null, i);
                }
            }
            return data;
        }
        private object LoadWithMap(Type type, Table table)
        {
            IDictionary data = (IDictionary)ReflectionUtil.CreateInstance(type);
            var ktype = type.GetGenericArguments()[0];
            var vtype = type.GetGenericArguments()[1];
            foreach (var cell in table.Pairs)
            {
                if (cell.Key.String != null && cell.Value != null)
                {
                    var key = Parser.StringToObject(cell.Key.String, ktype);
                    var value = LuaValueToObject(vtype, cell.Value);
                    data[key] = value;
                }
            }
            return data;
        }
        private object LoadWithList(Type type, Table table)
        {
            IList data = (IList)ReflectionUtil.CreateInstance(type);
            var etype = type.GetGenericArguments()[0];
            foreach (var cell in table.Pairs)
            {
                var i = Convert.ToInt32(cell.Key.Number) - 1;
                if (cell.Value != null)
                {
                    data[i] = LuaValueToObject(etype, cell.Value);
                }
                else
                {
                    data[i] = null;
                }
            }
            return data;
        }

        protected object LoadLuaFile(string sheetFile)
        {
            string path = string.Format("{0}/{1}", xlsFile, sheetFile);
            path = Resource.FormatPath(path);
            var txt = Resource.LoadAllText(path);
            if (txt == null)
                throw new Exception("Template File Not Found : " + path);
            var ret = Svr.DoString(txt).Table;
            return ret;
        }
    }

#endif
}
