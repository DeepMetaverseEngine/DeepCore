using DeepCore.Lua;
using SLua;
using System;
using System.Collections.Generic;
using System.Reflection;
using DeepCore.Reflection;
using System.Collections;

namespace DeepCore.Template.SLua
{
    public class SLuaTable : ILuaTable
    {
        internal readonly LuaTable Table;
        private Lazy<KeyValuePair<object, object>> mLazyFirst;

        public SLuaTable(ILuaSystem sys, LuaTable t)
        {
            System = sys;
            Table = t;
            mLazyFirst = new Lazy<KeyValuePair<object, object>>(() =>
            {
                var e = Table.GetEnumerator();
                if (e.MoveNext())
                {
                    var key = e.Current.key;
                    var value = ToValue(e.Current.value);
                    return new KeyValuePair<object, object>(key, value);
                }

                return new KeyValuePair<object, object>(null, null);
            });
        }

        public void Dispose()
        {
            Table.Dispose();
        }

        protected object ToValue(object obj)
        {
            if (obj is LuaTable)
            {
                return new SLuaTable(System, (LuaTable) obj);
            }

            if (obj is LuaFunction)
            {
                return new SLuaFunction(System, (LuaFunction) obj);
            }

            return obj;
        }

        public object this[object key]
        {
            get
            {
                object obj;
                if (key is int)
                {
                    obj = Table[(int) key];
                }
                else if (key is string)
                {
                    obj = Table[(string) key];
                }
                else
                {
                    throw new ArgumentException();
                }

                return ToValue(obj);
            }
            set
            {
                if (key is int)
                {
                    Table[(int) key] = value;
                }
                else if (key is string)
                {
                    Table[(string) key] = value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        public bool TryGetValue(object key, out object value)
        {
            if (key is int)
            {
                var obj = Table[(int) key];
                value = ToValue(obj);
                return true;
            }
            else if (key is string)
            {
                var obj = Table[(string) key];
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
                return Table[(int) key] != null;
            }
            else if (key is string)
            {
                return Table[(string) key] != null;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public object InnerTable
        {
            get { return Table; }
        }

        public ILuaSystem System { get; private set; }

        public int Length
        {
            get { return Table.length(); }
        }

        public int Count
        {
            get { return Table.length(); }
        }

        public KeyValuePair<object, object> First
        {
            get { return mLazyFirst.Value; }
        }

        public T ConvertTo<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<object, object>> Pairs
        {
            get
            {
                var ret = new List<KeyValuePair<object, object>>(this.Length);
                foreach (var e in Table)
                {
                    var key = e.key;
                    var value = ToValue(e.value);
                    ret.Add(new KeyValuePair<object, object>(key, value));
                }

                return ret;
            }
        }

        public IEnumerable<object> Keys
        {
            get
            {
                var ret = new List<object>(this.Length);
                foreach (var e in Table)
                {
                    ret.Add(e.key);
                }

                return ret;
            }
        }

        public IEnumerable<object> Values
        {
            get
            {
                var ret = new List<object>(this.Length);
                foreach (var e in Table)
                {
                    var value = ToValue(e.value);
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

        public UnionValue ToUnionValue()
        {
            var ret = UnionValue.NewMap;
            foreach (var entry in Table)
            {
                var key = System.InnerObjectToUnionValue(entry.key);
                if (key.IsFloat)
                {
                    key = (int) key;
                }

                var v = System.InnerObjectToUnionValue(entry.value);
                ret[key] = v;
            }

            var arr = ret.TryMapToArray(false, 1, true);
            return arr.IsArray ? arr : ret;
        }
    }

    public class SLuaSystem : ILuaSystem
    {
        internal readonly LuaState Svr;

        public SLuaSystem(LuaState svr)
        {
            Svr = svr;
        }

        public void Dispose()
        {
            Svr.Dispose(true);
        }

        public object DoString(string stringCode)
        {
            return Svr.doString(stringCode);
        }

        public object DoFile(string file)
        {
            return Svr.doFile(file);
        }


        public object ConvertToTargetInnerObject(object obj, ILuaSystem targetSystem)
        {
            if (targetSystem == this)
            {
                return obj;
            }

            if (!(targetSystem is SLuaSystem lua))
            {
                throw new NotSupportedException();
            }

            if (!(obj is LuaVar luaVar))
            {
                return obj;
            }

            if (luaVar.L == lua.Svr.L)
            {
                return obj;
            }

            if (obj is LuaFunction fn)
            {
                var swapFn = new LuaCSFunction(state =>
                {
                    var argc = LuaDLL.lua_gettop(state);
                    var args = new object[argc];
                    for (var i = 0; i < argc; i++)
                    {
                        LuaObject.checkType(state, 1, out args[i]);
                    }

                    for (var i = 0; i < argc; i++)
                    {
                        args[i] = lua.ConvertToTargetInnerObject(args[i], this);
                    }


                    if (!fn.TryCall(out var ret, args))
                    {
                        LuaDLL.lua_pushstring(state, "call error");
                        LuaDLL.lua_error(state);
                        return 0;
                    }

                    if (ret == null)
                    {
                        return 0;
                    }

                    var targetRet = ConvertToTargetInnerObject(ret, lua);
                    LuaDLL.lua_pushboolean(state, true);
                    LuaObject.pushVar(state, targetRet);
                    return 2;
                });
                return swapFn;
            }

            if (obj is LuaTable table)
            {
                var newTable = new LuaTable(lua.Svr);
                foreach (var entry in table)
                {
                    var v = ConvertToTargetInnerObject(entry.value, targetSystem);
                    if (entry.key.GetType().IsPrimitive)
                    {
                        newTable[Convert.ToInt32(entry.key)] = v;
                    }
                    else if (entry.key is string strKey)
                    {
                        newTable[strKey] = v;
                    }
                }
                return newTable;
            }

            throw new NotSupportedException();
        }

        public ILuaFunction CastToLuaFunction(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            if (obj is ILuaFunction function)
            {
                return function;
            }

            if (obj is LuaFunction fn)
            {
                if (fn.L == Svr.L)
                {
                    return new SLuaFunction(this, fn);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            return null;
        }

        public void SetGlobalValue(string key, object v)
        {
            Svr[key] = v;
        }

        public object GetGlobalValue(string key)
        {
            var obj = Svr[key];
            if (obj is LuaTable table)
            {
                return new SLuaTable(this, table);
            }

            if (obj is LuaFunction fn)
            {
                return new SLuaFunction(this, fn);
            }

            return obj;
        }


        public object UnionValueToInnerObject(UnionValue v)
        {
            if (v.IsNull)
            {
                return null;
            }

            if (v.IsNative)
            {
                return v.Value;
            }

            if (v.Value is LuaVar luaVar)
            {
                if (luaVar.L == Svr.L)
                {
                    return luaVar;
                }
                throw new NotSupportedException();
            }

            if (v.Value is Delegate)
            {
                return v.Value;
            }

            if (v.Value is ILuaFunction iFunc)
            {
                if (iFunc.InnerFunction is LuaFunction lFunc && lFunc.L == Svr.L)
                {
                    return lFunc;
                }

                throw new NotSupportedException();
            }

            
            if (v.Value is ILuaTable iObj)
            {
                v = iObj.ToUnionValue();
            }

            if (v.IsMap || v.IsArray)
            {
                var args = new LuaTable(Svr);
                v.ForEachElement((key, value) =>
                {
                    var ret = UnionValueToInnerObject(value);
                    if (key.IsString)
                    {
                        args[(string) key] = ret;
                    }
                    else if (key.IsPrimitive)
                    {
                        if (v.IsArray)
                        {
                            args[(int) key + 1] = ret;
                        }
                        else
                        {
                            args[(int) key] = ret;
                        }
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                });
                return args;
            }
            throw new NotSupportedException(v.Value.GetType().FullName);
        }

        public UnionValue InnerObjectToUnionValue(object obj)
        {
            if (obj == null)
            {
                return UnionValue.Null;
            }

            if (UnionValue.IsNativeObj(obj))
            {
                var native = UnionValueSerializer.Serialize(obj);
                if (native.IsFloat && float.IsNaN((float) native))
                {
                    return UnionValue.Null;
                }

                return native.TryFloatToInt(out var fNative) ? fNative : native;
            }

            if (obj is LuaFunction fn)
            {
                obj = CastToLuaFunction(fn);
            }

            if (obj is ILuaFunction iFunc)
            {
                return UnionValue.Create(iFunc);
            }

            if (obj is ILuaTable iObj)
            {
                obj = iObj.InnerTable;
            }

            var t = obj as LuaTable;
            if (t == null)
            {
                return UnionValue.Create(obj);
            }

            using (var tt = CastToLuaTable(t))
            {
                return tt.ToUnionValue();
            }
        }

        public object[] UnpackInnerArray(object t)
        {
            try
            {
                var table = t as LuaTable;
                if (table != null)
                {
                    var ret = new object[table.length()];
                    var p = 1;
                    foreach (var o in table)
                    {
                        if (Convert.ToInt32(o.key) == p)
                        {
                            ret[p - 1] = o.value;
                            p++;
                        }
                        else
                        {
                            return new[] {t};
                        }
                    }

                    return ret;
                }

                return new[] {t};
            }
            catch
            {
                return new[] {t};
            }
        }

        public object CLRToInnerObject(object obj)
        {
            throw new NotImplementedException();
        }

        public object InnerObjectToCLR(object innerObj)
        {
            throw new NotImplementedException();
        }

        public string FormatException(Exception e)
        {
            return null;
        }

        public ILuaTable CreateTable()
        {
            return new SLuaTable(this, new LuaTable(Svr));
        }

        public ILuaTable CastToLuaTable(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            if (obj is ILuaTable table)
            {
                return table;
            }

            if (obj is LuaTable luaTable)
            {
                return new SLuaTable(this, luaTable);
            }

            return null;
        }


        public void Update()
        {
            Svr.tick();
        }

    }

    public class SLuaFunction : ILuaFunction
    {
        internal LuaFunction Func;

        public SLuaFunction(ILuaSystem sys, LuaFunction fn)
        {
            System = sys;
            Func = fn;
        }

        public void Dispose()
        {
            Func.Dispose();
        }

        public object Call(params object[] args)
        {
            if (!Func.TryCall(out var ret, args))
            {
                throw new Exception("lua call error");
            }

            return ret;
        }

        public object InnerFunction => Func;

        public ILuaSystem System { get; private set; }
    }

    public class SLuaAdapter : ILuaAdapter
    {
        public static SLuaAdapter Instance { get; private set; }

        public SLuaAdapter()
        {
            Instance = this;
        }

        private static List<MethodInfo> sRegs = new List<MethodInfo>();
        private static bool sCollectRegs;

        private void OnInit(LuaState state)
        {
            state.openSluaLib();
            state.openExtLib();
            lock (sRegs)
            {
                if (!sCollectRegs)
                {
                    sCollectRegs = true;
                    var all = ReflectionUtil.GetNoneVirtualSubTypes(typeof(LuaObject));
                    foreach (var type in all)
                    {
                        if (type.Namespace != "SLua")
                        {
                            var m = type.GetMethod("reg");
                            if (m != null)
                            {
                                sRegs.Add(m);
                            }
                        }
                    }
                }

                foreach (var m in sRegs)
                {
                    m.Invoke(null, new object[] {state.L});
                }
            }
        }

        public override ILuaSystem CreateLuaSystem(Action<string> logHandler, Action<string> errorHandler, params Type[] types)
        {
            var ret = new LuaState();
            LuaObject.init(ret.L);
            OnInit(ret);
            if (logHandler != null)
            {
                ret.logDelegate = logHandler.Invoke;
                ret.warnDelegate = logHandler.Invoke;
            }

            if (errorHandler != null)
            {
                ret.errorDelegate = errorHandler.Invoke;
            }

            return new SLuaSystem(ret);
        }

        public override void ClearFileCache()
        {
            LuaState.fileMap.Clear();
        }

        public override void RemoveFileCache(string file)
        {
            byte[] ret;
            LuaState.fileMap.TryRemove(file, out ret);
        }

        private readonly Type[] mInnerTypes = new Type[] {typeof(LuaVar)};

        public override Type[] GetInnerTypes()
        {
            return mInnerTypes;
        }

        public override byte[] GetOrLoadFileBytes(string file)
        {
            try
            {
                var bytes = LuaState.fileMap.GetOrAdd(file, (key) =>
                {
                    var bs = System.IO.File.ReadAllBytes(key);
                    bs = LuaState.CleanUTF8Bom(bs);
                    return bs;
                });
                return bytes;
            }
            catch
            {
                return null;
            }
        }
    }
}