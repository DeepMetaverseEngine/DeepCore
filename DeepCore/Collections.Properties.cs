using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Xml;
using System.IO;
using System.Dynamic;
using DeepCore.Json;
using DeepCore.ORM;
using System.Threading.Tasks;

namespace DeepCore
{

    public struct PropertiesFormat
    {
        public string Separator = "=";
        public string LinkNextLine = "\\";
        public string NextLine = "\n";
        public string Comment = "#";
        public bool EmptyValue = false;
        public Func<string, string> KeyToString;
        public Func<string, string> KeyFromString;
        public Func<string, string> ValueToString;
        public Func<string, string> ValueFromString;
        public PropertiesFormat() { }
        public static readonly PropertiesFormat Default = new PropertiesFormat();
        public static readonly PropertiesFormat HTTP = new PropertiesFormat()
        {
            Comment = null,
            Separator = ":",
            LinkNextLine = null,
        };
    }
    public class Properties : HashMap<string, string>, IPrimitiveWrapper
    {
        public Properties() { }
        public Properties(int capacity) : base(capacity) { }
        public Properties(IDictionary<string, string> map) : base(map) { }

        public static Properties LoadFromXML(XmlElement xml, string sub_split = ".")
        {
            return LoadFromXML(xml, false, sub_split);
        }
        public static Properties LoadFromXML(XmlElement xml, bool includeAttribute, string sub_split = ".")
        {
            if (xml != null)
            {
                var ret = new Properties();
                if (includeAttribute)
                {
                    foreach (XmlAttribute attr in xml.Attributes)
                    {
                        ret.Put(attr.Name, attr.Value);
                    }
                }
                foreach (var e in xml.ChildNodes)
                {
                    if (e is XmlElement sub)
                    {
                        ret.Put(sub.Name, sub.InnerText);
                        if (sub.ChildNodes.Count > 0)
                        {
                            var spp = LoadFromXML(sub, includeAttribute, sub_split);
                            ret.PutSub(sub.Name + sub_split, spp);
                        }
                    }
                }
                return ret;
            }
            return null;
        }
        public static Task<Properties> LoadFromResourceAsync(string path)
        {
            return LoadFromResourceAsync(path, PropertiesFormat.Default);
        }
        public static async Task<Properties> LoadFromResourceAsync(string path, PropertiesFormat format)
        {
            string text = await Resource.LoadAllTextAsync(path);
            if (text != null)
            {
                return ParseText(text, format);
            }
            else
            {
                return null;
            }
        }
        public static Properties LoadFromResource(string path)
        {
            return LoadFromResource(path, PropertiesFormat.Default);
        }
        public static Properties LoadFromResource(string path, PropertiesFormat format)
        {
            string text = Resource.LoadAllText(path);
            if (text != null)
            {
                return ParseText(text, format);
            }
            else
            {
                return null;
            }
        }
        public static Properties LoadFromCodeBase(Assembly asm, string extension = ".properties")
        {
            return LoadFromCodeBase(asm, PropertiesFormat.Default, extension);
        }
        public static Properties LoadFromCodeBase(Assembly asm, PropertiesFormat format, string extension = ".properties")
        {
            var exe_file = new System.IO.FileInfo(asm.Location);
            var config_file = new System.IO.FileInfo(exe_file.Directory + "/" + exe_file.Name.Substring(0, exe_file.Name.Length - exe_file.Extension.Length) + extension);
            var lines = System.IO.File.ReadAllLines(config_file.FullName, CUtils.UTF8);
            return ParseLines(lines, format);
        }

        public static Properties FromDictionary(IDictionary map)
        {
            var ret = new Properties(map.Count);
            var e = map.GetEnumerator();
            while (e.MoveNext())
            {
                ret.Add(e.Key.ToString(), e.Value.ToString());
            }
            return ret;
        }
        //----------------------------------------------------------------
        #region Format
        public override string ToString()
        {
            return ToString(new PropertiesFormat() { });
        }
        public string ToString(string prefix = "")
        {
            return ToString(new PropertiesFormat() { }, prefix);
        }
        public string ToString(PropertiesFormat format, string prefix = "")
        {
            var sb = new StringWriter();
            {
                foreach (var e in this)
                {
                    sb.Append(prefix);
                    if (format.KeyToString != null)
                    {
                        sb.Append(format.KeyToString(e.Key));
                    }
                    else
                    {
                        sb.Append(e.Key);
                    }
                    sb.Append(format.Separator);
                    if (format.ValueToString != null)
                    {
                        sb.Append(format.ValueToString(e.Value));
                    }
                    else
                    {
                        sb.Append(e.Value);
                    }
                    sb.Append(prefix);
                    sb.Append(format.NextLine);
                }
                return sb.ToString();
            }
        }

        public static string[] SplitArgs(string args)
        {
            try
            {
                var list = new List<string>();
                var current = new StringBuilder();
                var begin = -1;
                for (int i = 0; i < args.Length; i++)
                {
                    var ch = args[i];
                    if (ch == '"')
                    {
                        //找 “
                        if (begin < 0)
                        {
                            begin = i;
                        }
                        else
                        {
                            list.Add(current.ToString());
                            current.Clear();
                            begin = -1;
                        }
                    }
                    else
                    {
                        if (begin < 0)
                        {
                            if (char.IsWhiteSpace(ch))
                            {
                                if (current.Length != 0)
                                {
                                    list.Add(current.ToString());
                                    current.Clear();
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                current.Append(ch);
                            }
                        }
                        else
                        {
                            current.Append(ch);
                        }
                    }
                }
                if (current.Length > 0)
                {
                    list.Add(current.ToString());
                    current.Clear();
                }
                return list.ToArray();
            }
            catch
            {
                return null;
            }
        }
        public static Properties ParseCommandLineArgs(string separator = "=")
        {
            var format = new PropertiesFormat() { Separator = separator, LinkNextLine = null, Comment = null };
            return ParseLines(Environment.GetCommandLineArgs(), format);
        }
        public static Properties ParseEnvironmentVariables(EnvironmentVariableTarget target = EnvironmentVariableTarget.User)
        {
            var args = Environment.GetEnvironmentVariables(target);
            var ret = new Properties();
            var e = args.GetEnumerator();
            while (e.MoveNext())
            {
                ret.Put($"{e.Key}", $"{e.Value}");
            }
            return ret;
        }
        public void SaveEnvironmentVariables(EnvironmentVariableTarget target = EnvironmentVariableTarget.User)
        {
            foreach (var e in this)
            {
                Environment.SetEnvironmentVariable(e.Key, e.Value, target);
            }
        }
        public static Properties ParseArgs(string args, string separator = "=")
        {
            var format = new PropertiesFormat() { Separator = separator, LinkNextLine = null, Comment = null, EmptyValue = true };
            return ParseLines(SplitArgs(args), format);
        }

        public static Properties ParseArgs(string[] args, string separator = "=")
        {
            var format = new PropertiesFormat() { Separator = separator, LinkNextLine = null, Comment = null, EmptyValue = true };
            return ParseLines(args, format);
        }


        public static Properties ParseText(string text)
        {
            return ParseText(text, PropertiesFormat.Default);
        }
        public static Properties ParseText(string text, PropertiesFormat format)
        {
            var ret = new Properties();
            ret.TryParseText(text, format);
            return ret;
        }
        public static Properties ParseLines(string[] lines)
        {
            return ParseLines(lines, PropertiesFormat.Default);
        }
        public static Properties ParseLines(string[] lines, int index, int count)
        {
            return ParseLines(lines, index, count, PropertiesFormat.Default);
        }
        public static Properties ParseLines(string[] lines, PropertiesFormat format)
        {
            return ParseLines(lines, 0, lines.Length, format);
        }
        public static Properties ParseLines(string[] lines, int index, int count, PropertiesFormat format)
        {
            var ret = new Properties();
            ret.TryParseLines(lines, index, count, format);
            return ret;
        }
        public int TryParseText(string text)
        {
            return TryParseText(text, PropertiesFormat.Default);
        }
        public int TryParseText(string text, PropertiesFormat format)
        {
            int count = 0;
            string line = null;
            for (int ci = 0; ci < text.Length; ci++)
            {
                string temp_line = null;
                bool temp_done = false;
                var line_index = text.IndexOf(format.NextLine, ci);
                if (line_index >= 0)
                {
                    temp_line = text.Substring(ci, line_index - ci);
                }
                else
                {
                    temp_line = text.Substring(ci);
                    temp_done = true;
                }
                if (line != null)
                {
                    line += temp_line;
                }
                else
                {
                    line = temp_line;
                }
                if (!string.IsNullOrEmpty(format.LinkNextLine) && line.EndsWith(format.LinkNextLine))
                {
                    line = line.Substring(0, line.Length - 1);
                    if (temp_done)
                    {
                        if (TryParseLine(line, format))
                        {
                            count++;
                        }
                        break;
                    }
                    else
                    {
                        ci = line_index;
                        continue;
                    }
                }
                else if (TryParseLine(line, format))
                {
                    count++;
                }
                line = null;
                if (temp_done) break;
                else ci = line_index;
            }
            return count;
        }
        public int TryParseLines(string[] lines, int index, int count)
        {
            return TryParseLines(lines, index, count, PropertiesFormat.Default);
        }
        public int TryParseLines(string[] lines, int index, int count, PropertiesFormat format)
        {
            int ret = 0;
            int length = index + count;
            string line = null;
            for (int i = index; i < length; i++)
            {
                if (line != null)
                {
                    line += lines[i];
                }
                else
                {
                    line = lines[i];
                }
                if (format.LinkNextLine != null && line.EndsWith(format.LinkNextLine))
                {
                    if ((i + 1) >= length)
                    {
                        TryParseLine(line, format);
                    }
                    else
                    {
                        line = line.Substring(0, line.Length - 1);
                        continue;
                    }
                }
                else
                {
                    TryParseLine(line, format);
                }
                line = null;
            }
            return ret;
        }
        public bool TryParseLine(string line)
        {
            return TryParseLine(line, PropertiesFormat.Default);
        }
        public bool TryParseLine(string line, PropertiesFormat format)
        {
            line = line.Trim();
            if (format.Comment != null && line.StartsWith(format.Comment))
            {
                return false;
            }
            int index = line.IndexOf(format.Separator);
            if (index >= 0)
            {
                string key = line.Substring(0, index).Trim();
                string val = line.Substring(index + 1).Trim();
                if (format.KeyFromString != null)
                {
                    key = format.KeyFromString(key);
                }
                if (format.ValueFromString != null)
                {
                    val = format.ValueFromString(val);
                }
                this[key] = val;
                return true;
            }
            else if (format.EmptyValue)
            {
                string key = line.Trim();
                if (format.KeyFromString != null)
                {
                    key = format.KeyFromString(key);
                }
                this[line] = string.Empty;
                return true;
            }
            return false;
        }

        #endregion
        //----------------------------------------------------------------
        #region SubProperties

        public Properties PutAll(IDictionary<string, string> config, string prefix = null)
        {
            if (prefix == null)
            {
                foreach (string key in config.Keys)
                {
                    this[key] = config[key];
                }
            }
            else
            {
                foreach (string key in config.Keys)
                {
                    this[prefix + key] = config[key];
                }
            }
            return this;
        }
        public Properties PutAll(NameValueCollection config, string prefix = null)
        {
            if (prefix == null)
            {
                foreach (string key in config.AllKeys)
                {
                    this[key] = config[key];
                }
            }
            else
            {
                foreach (string key in config.AllKeys)
                {
                    this[prefix + key] = config[key];
                }
            }
            return this;
        }

        public void PutSub(string prefix, IDictionary<string, string> sub)
        {
            foreach (var e in sub)
            {
                this.Put(prefix + e.Key, e.Value);
            }
        }
        public void AddSub(string prefix, IDictionary<string, string> sub)
        {
            foreach (var e in sub)
            {
                this.Add(prefix + e.Key, e.Value);
            }
        }
        public Properties Indent(string prefix)
        {
            var ret = new Properties();
            foreach (var e in this)
            {
                ret.Put(prefix + e.Key, e.Value);
            }
            return ret;
        }
        public Properties SubProperties(string prefix)
        {
            Properties ret = new Properties();
            foreach (var e in this)
            {
                if (e.Key.StartsWith(prefix))
                {
                    var fname = e.Key.Substring(prefix.Length);
                    ret[fname] = e.Value;
                }
            }
            return ret;
        }
        public Properties SubProperties(string prefix, string split)
        {
            return SubProperties(prefix + split);
        }
        public HashMap<string, Properties> SplitSubProperties(string split)
        {
            var ret = new HashMap<string, Properties>();
            foreach (var e in this)
            {
                var index = e.Key.IndexOf(split);
                if (index >= 0)
                {
                    var prifix = e.Key.Substring(0, index + split.Length);
                    var map = ret.GetOrAdd(prifix, static p => new Properties());
                    var fname = e.Key.Substring(prifix.Length);
                    map[fname] = e.Value;
                }
            }
            return ret;
        }

        #endregion
        //----------------------------------------------------------------
        #region Converter
        public void PutAs<T>(string key, T data)
        {
            this.Put(key, ObjectToString(data));
        }

        public bool GetAsBool(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<bool>(v);
            }
            return false;
        }
        public char GetAsChar(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<char>(v);
            }
            return (char)0;
        }
        public byte GetAsByte(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<byte>(v);
            }
            return 0;
        }
        public sbyte GetAsSByte(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<sbyte>(v);
            }
            return 0;
        }
        public short GetAsShort(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<short>(v);
            }
            return 0;
        }
        public ushort GetAsUShort(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<ushort>(v);
            }
            return 0;
        }
        public int GetAsInt(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<int>(v);
            }
            return 0;
        }
        public uint GetAsUInt(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<uint>(v);
            }
            return 0;
        }
        public long GetAsLong(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<long>(v);
            }
            return 0;
        }
        public ulong GetAsULong(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<ulong>(v);
            }
            return 0;
        }

        public float GetAsFloat(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<float>(v);
            }
            return 0;
        }
        public double GetAsDouble(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<double>(v);
            }
            return 0;
        }
        public T GetAsEnum<T>(string key) where T : unmanaged
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<T>(v);
            }
            return default(T);
        }
        public T GetAs<T>(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return StringToObject<T>(v);
            }
            return default(T);
        }
        public Nullable<T> GetStructAs<T>(string key) where T : struct
        {
            if (TryGetValue(key, out var v))
            {
                return (StringToObject<T>(v));
            }
            return default;
        }



        public bool TryGetAsBool(string key, out bool ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<bool>(v, out ret);
            }
            ret = false;
            return false;
        }
        public bool TryGetAsChar(string key, out char ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<char>(v, out ret);
            }
            ret = (char)0;
            return false;
        }
        public bool TryGetAsByte(string key, out byte ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<byte>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsSByte(string key, out sbyte ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<sbyte>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsShort(string key, out short ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<short>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsUShort(string key, out ushort ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<ushort>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsInt(string key, out int ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<int>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsUInt(string key, out uint ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<uint>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsLong(string key, out long ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<long>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsULong(string key, out ulong ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<ulong>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsFloat(string key, out float ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<float>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsDouble(string key, out double ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<double>(v, out ret);
            }
            ret = 0;
            return false;
        }
        public bool TryGetAsEnum<T>(string key, out T ret) where T : unmanaged
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<T>(v, out ret);
            }
            ret = default(T);
            return false;
        }
        public bool TryGetAs<T>(string key, out T ret)
        {
            if (TryGetValue(key, out var v))
            {
                return TryStringToObject<T>(v, out ret);
            }
            ret = default(T);
            return false;
        }
        public bool TryGetAction<T>(string key, Action<T> action)
        {
            if (TryGetValue(key, out var v))
            {
                var r = TryStringToObject<T>(v, out var ret);
                action(ret);
                return r;
            }
            return false;
        }

        #endregion

        //----------------------------------------------------------------
        #region Object
        public void PutObject(string key, object data)
        {
            this.Put(key, ObjectToString(data));
        }
        protected T StringToObject<T>(string v)
        {
            if (TryStringToObject<T>(v, out var value))
            {
                return value;
            }
            return default(T);
        }
        protected object StringToObject(string v, Type type)
        {
            if (TryStringToObject(v, type, out var value))
            {
                return value;
            }
            return null;
        }
        protected bool TryStringToObject<T>(string v, out T ret)
        {
            if (TryStringToObject(v, typeof(T), out var _ret))
            {
                ret = (T)_ret;
                return true;
            }
            ret = default(T);
            return false;
        }

        protected virtual bool TryStringToObject(string v, Type type, out object ret)
        {
            //             if (JsonUtil.TryDecodeObject(v, type, out ret))
            //             {
            //                 return true;
            //             }
            if (Parser.TryStringToObject(v, type, out ret))
            {
                return true;
            }
            return false;
        }
        protected virtual string ObjectToString(object data)
        {
            return Parser.ObjectToString(data);
        }

        public void PutWithJson(string key, object data)
        {
            this.Put(key, JsonUtil.EncodeObject(data));
        }

        public T GetWithJson<T>(string key)
        {
            if (TryGetValue(key, out var v))
            {
                return JsonUtil.DecodeObject<T>(v);
            }
            return default(T);
        }
        public bool TryGetWithJson<T>(string key, out T ret)
        {
            if (TryGetValue(key, out var v))
            {
                ret = JsonUtil.DecodeObject<T>(v);
                return true;
            }
            ret = default(T);
            return false;
        }

        #endregion
        //----------------------------------------------------------------
        #region Config

        public T LoadInstance<T>() where T : new()
        {
            var ret = new T();
            LoadFields(ret);
            return ret;
        }
        public static Properties SaveInstance(object data)
        {
            var p = new Properties();
            p.SaveFields(data);
            return p;
        }

        /// <summary>
        /// 通常载入配置文件
        /// </summary>
        /// <param name="cfg"></param>
        public void LoadFields(object cfg, Action<FieldInfo> notExist = null)
        {
            Type type = cfg.GetType();
            foreach (FieldInfo fi in type.GetFields())
            {
                if (!fi.IsStatic)
                {
                    if (ContainsKey(fi.Name))
                    {
                        string value = this.Get(fi.Name);
                        object vo = this.StringToObject(value, fi.FieldType);
                        fi.SetValue(cfg, vo);
                    }
                    else
                    {
                        notExist?.Invoke(fi);
                    }
                }
            }
        }
        public void SaveFields(object cfg)
        {
            Type type = cfg.GetType();
            foreach (var fi in type.GetFields())
            {
                if (!fi.IsStatic)
                {
                    this.Put(fi.Name, this.ObjectToString(fi.GetValue(cfg)));
                }
            }
        }

        public void LoadStaticFields(Type type, Action<FieldInfo> notExist = null)
        {
            foreach (FieldInfo fi in type.GetFields())
            {
                if (fi.IsStatic)
                {
                    if (ContainsKey(fi.Name))
                    {
                        string value = this.Get(fi.Name);
                        object vo = this.StringToObject(value, fi.FieldType);
                        fi.SetValue(null, vo);
                    }
                    else
                    {
                        notExist?.Invoke(fi);
                    }
                }
            }
        }
        public void SaveStaticFields(Type type)
        {
            foreach (FieldInfo fi in type.GetFields())
            {
                if (fi.IsStatic)
                {
                    object vo = fi.GetValue(null);
                    this.Put(fi.Name, this.ObjectToString(vo));
                }
            }
        }

        public static Properties GetDefaultFields(object data, string prefix = "")
        {
            var type = data.GetType();
            var ret = new Properties();
            foreach (FieldInfo fi in type.GetFields())
            {
                //if (fi.IsStatic)
                {
                    object vo = fi.GetValue(data);
                    ret.Put(prefix + fi.Name, ret.ObjectToString(vo));
                }
            }
            return ret;
        }
        public static Properties GetDefaultStaticFields(Type type, string prefix = "")
        {
            var ret = new Properties();
            foreach (FieldInfo fi in type.GetFields())
            {
                if (fi.IsStatic)
                {
                    object vo = fi.GetValue(null);
                    ret.Put(prefix + fi.Name, ret.ObjectToString(vo));
                }
            }
            return ret;
        }

        public static Properties SaveStaticFieldsToFile(FileInfo file, Type type)
        {
            var prop = new DeepCore.Properties();
            prop.SaveStaticFields(type);
            System.IO.File.WriteAllText(file.FullName, prop.ToString(), CUtils.UTF8);
            return prop;
        }
        public static Properties LoadStaticFieldsFromFile(FileInfo file, Type type, Action<FieldInfo> notExist = null)
        {
            return LoadStaticFieldsFromFile(file, type, PropertiesFormat.Default, notExist);
        }
        public static Properties LoadStaticFieldsFromFile(FileInfo file, Type type, PropertiesFormat format, Action<FieldInfo> notExist = null)
        {
            if (file.Exists)
            {
                var text = System.IO.File.ReadAllText(file.FullName, CUtils.UTF8);
                if (text != null)
                {
                    var prop = new DeepCore.Properties();
                    prop.TryParseText(text, format);
                    prop.LoadStaticFields(type, notExist);
                    return prop;
                }
            }
            return null;
        }

        public static Properties SaveStaticFieldsToFile(string file, Type type)
        {
            return SaveStaticFieldsToFile(new FileInfo(file), type);
        }
        public static Properties LoadStaticFieldsFromFile(string file, Type type, Action<FieldInfo> notExist = null)
        {
            return LoadStaticFieldsFromFile(new FileInfo(file), type, notExist);
        }
        public static Properties LoadStaticFieldsFromFile(string file, Type type, PropertiesFormat format, Action<FieldInfo> notExist = null)
        {
            return LoadStaticFieldsFromFile(new FileInfo(file), type, format, notExist);
        }

        public static Properties LoadStaticFieldsFromPath(string path, Type type, PropertiesFormat format, Action<FieldInfo> notExist = null)
        {
            if (Resource.ExistData(path))
            {
                var text = Resource.LoadAllText(path);
                if (text != null)
                {
                    var prop = new DeepCore.Properties();
                    prop.TryParseText(text, format);
                    prop.LoadStaticFields(type, notExist);
                    return prop;
                }
            }
            return null;
        }
        public static Properties LoadStaticFieldsFromPath(string path, Type type, Action<FieldInfo> notExist = null)
        {
            return LoadStaticFieldsFromPath(path, type, PropertiesFormat.Default, notExist);
        }


        #endregion
        //----------------------------------------------------------------
        public static void ReadExternal(Properties prop, IInputStream input)
        {
            prop.ReadExternal(input);
        }
        public static void WriteExternal(Properties prop, IOutputStream output)
        {
            prop.WriteExternal(output);
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutVS32(this.Count);
            foreach (var e in this)
            {
                output.PutUTF(e.Key);
                output.PutUTF(e.Value);
            }
        }
        public void ReadExternal(IInputStream input)
        {
            var count = input.GetVS32();
            for (int i = 0; i < count; i++)
            {
                this.Put(input.GetUTF(), input.GetUTF());
            }
        }
    }

}
