using DeepCore.Lua;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DeepCore.Template.MoonSharp
{
    public class MoonSharpLuaTable : ILuaTable
    {
        internal readonly Table Table;
        public object InnerTable => Table;
        public UnionValue ToUnionValue()
        {
            throw new NotImplementedException();
        }

        public ILuaSystem System { get; }

        public int Length
        {
            get { return Table.Length; }
        }
        public KeyValuePair<object, object> First { get; }
        public T ConvertTo<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<object, object>> Pairs
        {
            get
            {
                var ret = new List<KeyValuePair<object, object>>(this.Length);
                foreach (var e in Table.Pairs)
                {
                    var key = e.Key.ToObject();
                    var value = ToValue(e.Value);
                    ret.Add(new KeyValuePair<object, object>(key, value));
                }
                return ret;
            }
        }

        public void Dispose()
        {
            Table.Clear();
        }

        public MoonSharpLuaTable(ILuaSystem sys, Table t)
        {
            System = sys;
            Table = t;
            {
                var e = t.Pairs.GetEnumerator();
                if (e.MoveNext())
                {
                    var key = e.Current.Key.ToObject();
                    var value = ToValue(e.Current.Value);
                    this.First = new KeyValuePair<object, object>(key, value);
                }
            }
        }

        public object this[object key]
        {
            get
            {
                var obj = Table[key];
                return ToValue(obj);
            }
            set => Table[key] = value;
        }

        protected object ToValue(object obj)
        {
            switch (obj)
            {
                case Table t:
                    return new MoonSharpLuaTable(System, t);
                case Closure s:
                    return new MoonSharpLuaFunction(System, s);
                case DynValue v:
                    return ToValue(v.ToObject());
                default:
                    return obj;
            }
        }


        //-----------------------------------------------------------------------------
       
    
        public bool TryGetValue(object key, out object value)
        {
            if (key is int)
            {
                var obj = Table[(int)key];
                value = ToValue(obj);
                return true;
            }
            else if (key is string)
            {
                var obj = Table[(string)key];
                value = ToValue(obj);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
        public bool ContainsKey(object key)
        {
            if (key is int)
            {
                return Table[(int)key] != null;
            }
            else if (key is string)
            {
                return Table[(string)key] != null;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public int Count
        {
            get { return this.Length; }
        }

        public IEnumerable<object> Keys
        {
            get
            {
                var ret = new List<object>(this.Length);
                foreach (var e in Table.Keys)
                {
                    ret.Add(ToValue(e));
                }
                return ret;
            }
        }
        public IEnumerable<object> Values
        {
            get
            {
                var ret = new List<object>(this.Length);
                foreach (var e in Table.Values)
                {
                    var value = ToValue(e);
                    ret.Add(value);
                }
                return ret;
            }
        }
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return this.Pairs.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class MoonSharpLuaFunction : ILuaFunction
    {
        internal readonly Closure Closure;
        public object InnerFunction => Closure;
        public ILuaSystem System { get; }

        public MoonSharpLuaFunction(ILuaSystem sys, Closure s)
        {
            System = sys;
            Closure = s;
        }

        public void Dispose()
        {

        }

        public object Call(params object[] args)
        {
            return Closure.Call(args);
        }
    }

    public class MoonSharpLuaSystem : ILuaSystem
    {
        internal readonly Script Script;
        //private static ConcurrentDictionary<string, string> mFiles = new ConcurrentDictionary<string, string>();

        public MoonSharpLuaSystem(Script s)
        {
            Script = s;
        }
        
        public void Dispose()
        {
        }

        public object DoString(string stringCode)
        {
            return Script.DoString(stringCode)?.ToObject();
        }

        public object DoFile(string file)
        {
            return Script.DoFile(file)?.ToObject();
        }

        public ILuaTable CreateTable()
        {
            return new MoonSharpLuaTable(this, new Table(Script));
        }

        public ILuaTable CastToLuaTable(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            Table t;
            if (obj is DynValue value)
            {
                t = value.Table;
            }
            else
            {
                t = (Table)obj;
            }
            return new MoonSharpLuaTable(this, t);
        }

        public ILuaFunction CastToLuaFunction(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            return new MoonSharpLuaFunction(this, (Closure)obj);
        }

        public void SetGlobalValue(string key, object v)
        {
            Script.Globals[key] = v;
        }

        public object GetGlobalValue(string key)
        {
            var obj = Script.Globals[key];
            switch (obj)
            {
                case Table t:
                    return new MoonSharpLuaTable(this, t);
                case Closure s:
                    return new MoonSharpLuaFunction(this, s);
                case DynValue v:
                    return v.ToObject();
                default:
                    return obj;
            }
        }

        public object UnionValueToInnerObject(UnionValue v)
        {
            if (v.IsNull)
            {
                return null;
            }
            if (v.IsNative)
            {
                if (v.IsDateTime || v.IsTimeSpan)
                {
                    return DynValue.FromObject(this.Script, v.ToString()).ToObject();
                }
                return DynValue.FromObject(this.Script, v.Value).ToObject();
            }
            var args = new Table(this.Script);
            v.ForEachElement((key, value) =>
            {
                object ret = UnionValueToInnerObject(value);
                if (key.IsString)
                {
                    args[(string)key] = ret;
                }
                else if (key.IsPrimitive)
                {
                    if (v.IsArray)
                    {
                        args[(int)key + 1] = ret;
                    }
                    else
                    {
                        args[(int)key] = ret;
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            });
            //disposeList.Add(msvr.CastToLuaTable(args));
            return args;
        }

        public UnionValue InnerObjectToUnionValue(object obj, bool _)
        {
            return InnerObjectToUnionValue(obj);
        }

        public UnionValue InnerObjectToUnionValue(object obj)
        {
            if (obj == null)
            {
                return UnionValue.Null;
            }
            if (obj is DynValue)
            {
                obj = ((DynValue)obj).ToObject();
            }

            if (UnionValue.IsNativeObj(obj))
            {
                return UnionValueSerializer.Serialize(obj);
            }

            if (obj is Closure)
            {
                return UnionValue.Create(CastToLuaFunction(obj));
            }
            
            if (!(obj is Table t))
            {
                return UnionValue.Create(obj);
            }
            //disposeList.Add(msvr.CastToLuaTable(t));
            var ret = UnionValue.NewMap;
            foreach (var entry in t.Pairs)
            {
                var key = InnerObjectToUnionValue(entry.Key.ToObject(), true);
                if (key.IsFloat)
                {
                    key = (int)key;
                }
                var v = InnerObjectToUnionValue(entry.Value.ToObject(), true);
                ret[key] = v;
            }

            var arr = ret.TryMapToArray(false, 1, true);

            return arr.IsArray ? arr : ret;
        }

        public object CLRToInnerObject(object obj)
        {
            throw new NotImplementedException();
        }

        public object InnerObjectToCLR(object innerObj)
        {
            throw new NotImplementedException();
        }

        public object[] UnpackInnerArray(object t)
        {
            if (t is Table table)
            {
                var ret = new List<object>();
                foreach (var o in table.Pairs)
                {
                    if (o.Key.Type != DataType.Number && o.Key.Type != DataType.Nil)
                    {
                        return new[] { t };
                    }
                    var index = o.Key.ToObject<int>();
                    while (ret.Count < index - 1)
                    {
                        ret.Add(null);
                    }
                    ret.Add(o.Value);
                }
                return ret.ToArray();
            }
            return new[] { t };
        }

        public string FormatException(Exception e)
        {
            if (e is InterpreterException ie)
            {
                var err = ie.DecoratedMessage ?? e.Message;
                if (ie.CallStack != null)
                {
                    err += "\n";
                    foreach (var item in ie.CallStack)
                    {
                        err += item + "\n";
                    }
                }
                return err;
            }
            return null;
        }

        public void Reload()
        {
            if (Script.Options.ScriptLoader is MoonSharpLuaAdapter.ScriptLoader adp)
            {
                MoonSharpLuaAdapter.ScriptLoader.Reload();
            }
        }

        public void Reload(string file)
        {
            if (Script.Options.ScriptLoader is MoonSharpLuaAdapter.ScriptLoader adp)
            {
                MoonSharpLuaAdapter.ScriptLoader.Reload(file);
            }
        }

        public void Update()
        {

        }

        public void DisposeNext(IDisposable obj)
        {
            
        }

        public object ConvertToTargetInnerObject(object obj, ILuaSystem targetSystem)
        {
            throw new NotImplementedException();
        }
    }

    public class MoonSharpLuaAdapter : ILuaAdapter
    {
        public static MoonSharpLuaAdapter Instance { get; private set; }

        private static HashMap<string, string> mFiles = new HashMap<string, string>();

        //查询文件是否存在的缓存，减少文件IO
        private static HashMap<string, bool> mFilesExists = new HashMap<string, bool>();

        public MoonSharpLuaAdapter()
        {
            MoonSharpLuaAdapter.Instance = this;
            // new LuaTemplateLoader(this);
        }

        public class ScriptLoader : FileSystemScriptLoader
        {
            public override bool ScriptFileExists(string name)
            {
                lock (mFiles)
                {
                    string _;
                    if (mFiles.TryGetValue(name, out _))
                    {
                        return true;
                    }
                    bool rtn = mFilesExists.GetOrAdd(name, (file_name) =>
                    {
                        var is_exists = base.ScriptFileExists(file_name);
                        mFilesExists[file_name] = is_exists;
                        return is_exists;
                    });
                    return rtn;
                }
            }

            public override object LoadFile(string file, Table globalContext)
            {
                lock (mFiles)
                {
                    //return base.LoadFile(file, globalContext);
                    var str = mFiles.GetOrAdd(file, (name) =>
                {
                    var ret = File.ReadAllText(name);
                    mFilesExists[name] = true;
                    return ret;
                });
                    return str;
                }
            }

            public static void Reload()
            {
                lock (mFiles)
                {
                    mFiles.Clear();
                    mFilesExists.Clear();
                }
            }

            public static void Reload(string fileName)
            {
                lock (mFiles)
                {
                    mFilesExists.Remove(fileName);
                    mFiles.Remove(fileName);
                }
            }
        }



        public override ILuaSystem CreateLuaSystem(Action<string> logHandler, Action<string> errorHandler, params Type[] types)
        {
            var ret = new Script(CoreModules.Preset_Complete);
            foreach (var type in types)
            {
                UserData.RegisterType(type);
            }
            ret.Options.ScriptLoader = new ScriptLoader();
            ((ScriptLoaderBase)ret.Options.ScriptLoader).ModulePaths = ScriptLoaderBase.UnpackStringPaths("./?;./?.lua;?.lua");
            ret.Options.DebugPrint = logHandler;
            ret.Options.UseLuaErrorLocations = true;
            ret.Options.CheckThreadAccess = false;
            return new MoonSharpLuaSystem(ret);
        }

        public override void ClearFileCache()
        {
            ScriptLoader.Reload();
        }

        public override void RemoveFileCache(string file)
        {
            ScriptLoader.Reload(file);
        }

    }
}