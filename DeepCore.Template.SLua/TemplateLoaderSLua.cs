using DeepCore.IO;
using DeepCore.Reflection;
using SLua;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DeepCore.Template.SLua
{
    public class TemplateLoaderSLua : TemplateLoader
    {
        internal DeepCore.Log.Logger log;
        public TemplateLoaderSLua()
        {
        }
        public override ITemplateLoader GetLoader(string file)
        {
            return new LuaLoader(file, log);
        }
        public override void SetLogger(DeepCore.Log.Logger log)
        {
            this.log = log;
        }
    }

    class LuaLoader : ITemplateLoader
    {
        public LuaState Svr { get; private set; }
        protected readonly string xlsFile;
        protected readonly int tableIndex;

        protected static void doinit(LuaState L)
        {
            L.openSluaLib();
            L.openExtLib();

        }

        private static LuaState CreateLuaSvr(DeepCore.Log.Logger log)
        {
            var ret = new LuaState();
            IntPtr L = ret.L;
            LuaObject.init(L);
            doinit(ret);
            ret.logDelegate = log.Log;
            ret.warnDelegate = log.Warn;
            ret.errorDelegate = log.Error;
            return ret;
        }

        public LuaLoader(string xls_file, DeepCore.Log.Logger log, int tableIndex = 0)
        {
            this.Svr = CreateLuaSvr(log);

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
            Svr.Dispose();
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
            int count = LoadTemplates<K, T>(keyField, (s, k, t) => { ret.Add(t); });
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
        public HashMap<K, T> LoadTemplates<K, T>(string keyField, string sheetName) where T : new()
        {
            var ret = new HashMap<K, T>();
            int count = LoadTemplates<K, T>(keyField, sheetName + ".lua", (s, k, t) => { ret.Add(k, t); });
            return ret;
        }
        public List<T> LoadTemplatesAsList<K, T>(string keyField, string sheetName) where T : new()
        {
            var ret = new List<T>();
            int count = LoadTemplates<K, T>(keyField, sheetName + ".lua", (s, k, t) => { ret.Add(t); });
            return ret;
        }
        protected int LoadTemplates<K, T>(string keyField, string sheetFile, Action<string, K, T> action) where T : new()
        {
            int count = 0;
            LuaTable table = LoadLuaFile(sheetFile) as LuaTable;
            Type data_type = typeof(T);
            HashMap<int, FieldInfo> head_map = null;
            var keyVal = table["_key_"] as LuaTable;
            if (keyVal == null) throw new Exception("No Key Field : Sheet=" + sheetFile + " File=" + xlsFile);
            bool is_array = false;
            if (keyVal == null)
            {
                foreach (var row in table)
                {
                    keyVal = row.value as LuaTable;
                    break;
                }
                is_array = true;
            }
            head_map = new HashMap<int, FieldInfo>();
            foreach (var name in keyVal)
            {
                FieldInfo fi = data_type.GetField(name.value.ToString());
                if (fi != null)
                {
                    head_map.Add(Convert.ToInt32(name.key), fi);
                }
            }

            foreach (var entry in table)
            {
                string key = entry.key.ToString();
                if (key == "_key_")
                {
                    continue;
                }
                LuaTable row = entry.value as LuaTable;
                //扫内容并赋值.
                T data = new T();
                K keyValue = default(K);
                foreach (var cell in row)
                {
                    FieldInfo fi = head_map.Get(Convert.ToInt32(cell.key));
                    if (fi != null && cell.value != null)
                    {
                        object value;
                        if (cell.value is string)
                        {
                            value = Parser.StringToObject(cell.value.ToString(), fi.FieldType);
                            fi.SetValue(data, value);
                        }
                        else if (cell.value is IConvertible)
                        {
                            value = Convert.ChangeType(cell.value, fi.FieldType);
                            fi.SetValue(data, value);
                        }
                        else
                        {
                            throw new Exception("LuaLoader error:");
                        }

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

        private object LuaValueToObject(Type valueType, object cell)
        {
            if (cell is string)
            {
                return Parser.StringToObject(cell as string, valueType);
            }
            else if (cell is IConvertible)
            {
                return Convert.ChangeType(cell, valueType);
            }
            else if (cell is LuaTable)
            {
                if (valueType.IsArray)
                {
                    return LoadWithArray(valueType, cell as LuaTable);
                }
                else if (valueType.IsClass)
                {
                    if (valueType.GetInterface(typeof(IDictionary).Name) != null)
                    {
                        return LoadWithMap(valueType, cell as LuaTable);
                    }
                    else if (valueType.GetInterface(typeof(IList).Name) != null)
                    {
                        return LoadWithList(valueType, cell as LuaTable);
                    }
                    else
                    {
                        return LoadWithObject(valueType, cell as LuaTable);
                    }
                }
            }
            throw new Exception("LuaLoader error:");
        }


        private object LoadWithObject(Type type, LuaTable table)
        {
            var data = ReflectionUtil.CreateInstance(type);
            foreach (var cell in table)
            {
                if (cell.key != null && cell.value != null)
                {
                    FieldInfo fi = type.GetField(cell.key.ToString());
                    if (fi != null)
                    {
                        var value = LuaValueToObject(fi.FieldType, cell.value);
                        fi.SetValue(data, value);
                    }
                }
            }
            return data;
        }
        private object LoadWithArray(Type type, LuaTable table)
        {
            var etype = type.GetElementType();
            Array data = Array.CreateInstance(etype, table.length());
            foreach (var cell in table)
            {
                var i = Convert.ToInt32(cell.key) - 1;
                if (cell.value != null)
                {
                    data.SetValue(LuaValueToObject(etype, cell.value), i);
                }
                else
                {
                    data.SetValue(null, i);
                }
            }
            return data;
        }
        private object LoadWithMap(Type type, LuaTable table)
        {
            IDictionary data = (IDictionary)ReflectionUtil.CreateInstance(type);
            var ktype = type.GetGenericArguments()[0];
            var vtype = type.GetGenericArguments()[1];
            foreach (var cell in table)
            {
                if (cell.key != null && cell.value != null)
                {
                    var key = Parser.StringToObject(cell.key.ToString(), ktype);
                    var value = LuaValueToObject(vtype, cell.value);
                    data[key] = value;
                }
            }
            return data;
        }
        private object LoadWithList(Type type, LuaTable table)
        {
            IList data = (IList)ReflectionUtil.CreateInstance(type);
            var etype = type.GetGenericArguments()[0];
            foreach (var cell in table)
            {
                var i = Convert.ToInt32(cell.key) - 1;
                if (cell.value != null)
                {
                    data[i] = LuaValueToObject(etype, cell.value);
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
            //TemplateLoaderSLua.doLog(Logger.Level.Debug, path);
            var ret = Svr.doString(txt);
            return ret;
        }

    }
}
