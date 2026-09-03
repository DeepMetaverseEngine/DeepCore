using System;
using System.Collections.Concurrent;
using System.Threading;

//using SLua;
using DeepCore.Log;

namespace DeepCore.Lua
{
    public class LuaDataCenter : Disposable
    {
        public static LuaDataCenter Instance { get; private set; }

        private const string LuaDataCenterCode = @"
local readonly_mt = {}
readonly_mt.__index = readonly_mt
readonly_mt.__newindex = function(self, k, v)
    if self:HasWriteAccess(k) then
        rawset(self, k, v)
    else
        error('attempt to update a read-only table . you must call EnableWriteAccess first')
    end
end

function readonly_mt.HasWriteAccess(self, k)
    if self._readwrite_access_ then
        return true
    end
    if not __globalhooks_datacenter.enable_readonly then
        print('try set readonly field', k)
        return true
    end
    local cache_root = __globalhooks_datacenter.cache
    local p = cache_root[self._mt_parent_path]
    if p and p.contents then
        return p.contents._readwrite_access_
    end
    return false
end

function readonly_mt.EnableWriteAccess(self, val)
    rawset(self, '_readwrite_access_', val)
end

local function gen_table(tb, arr, header)
    local ret = {}
    header = header or tb._key_ or tb[1]
    --pprint(arr)
    local indexed
    for i, v in ipairs(arr) do
        local key = header[i]
        if key then
            ret[key] = v
        end
        indexed = i
    end
    if indexed < #arr then
        --参数数量不一致，检查
        local p = 1
        for k, _ in pairs(arr) do
            if k ~= p then
                local key = header[p]
                error(string.format('%s -- %s is nil', k, key))
            end
            p = p + 1
        end
    end
    return ret
end

local function make_refs(tb, cache_path)
    local contents = {}
    local header = tb._key_ or tb[1]
    for k, v in pairs(tb) do
        if (tb._key_ and k ~= '_key_') or (not tb._key_ and k > 1) then
            local t = gen_table(tb, v, header)
            t._mt_parent_path = cache_path
            setmetatable(t, readonly_mt)
            -- t.__index = t
            contents[k] = t
        end
    end
    local refs = {}
    local clone_key = {}
    for i, k in ipairs(header) do
        clone_key[i] = k
        local r = {}
        for _, v in pairs(contents) do
            local current = v[k]
            r[current] = r[current] or {}
            table.insert(r[current], v)
        end
        refs[k] = r
    end
    setmetatable(contents, readonly_mt)
    return {contents = contents, refs = refs, _key_ = clone_key}
end

local function src_table_find(tb, find_key, limit_count)
    if not find_key then
        return
    end

    local header = tb._key_
    if not header then
        if type(find_key) == 'table' and not next(find_key) then
            return tb
        elseif type(find_key) == 'string' then
            return tb[find_key]
        else
            -- todo 暂未实现
            error('todo 暂未实现')
        end
    end

    if type(find_key) == 'table' then
        if not next(find_key) then
            return tb.contents
        end
        local source = {}
        for k, v in pairs(find_key) do
            local ref = tb.refs[k]
            if ref then
                local refs
                if type(v) == 'function' then
                    refs = {}
                    for key, vv in pairs(ref) do
                        local check_ok = v(key)
                        if check_ok then
                            for _, vvv in ipairs(vv) do
                                table.insert(refs, vvv)
                            end
                        end
                    end
                else
                    refs = ref[v]
                end
                table.insert(source, refs or {})
            else
                print('key not found', k)
            end
        end
        if #source == 0 then
            return {}
        end
        local ret = {}
        for _, v in ipairs(source[1]) do
            table.insert(ret, v)
        end
        table.remove(source, 1)
        -- 取交集
        for i = #ret, 1, -1 do
            local v = ret[i]
            local check_ok = true
            for _, results in ipairs(source) do
                local sub_check_ok = false
                for __, vv in ipairs(results) do
                    if vv == v then
                        sub_check_ok = true
                        break
                    end
                end
                check_ok = check_ok and sub_check_ok
                if not check_ok then
                    break
                end
            end
            if not check_ok then
                table.remove(ret, i)
            end
        end
        if limit_count and #ret > limit_count then
            return {unpack(ret, 1, limit_count)}
        else
            return ret
        end
    elseif type(find_key) == 'function' then
        local ret = {}
        for _, v in pairs(tb.contents) do
            local check_ok = find_key(v)
            if check_ok then
                table.insert(ret, v)
                if limit_count and limit_count <= #ret then
                    break
                end
            end
        end
        return ret
    elseif tb.contents[find_key] then
        local ret = tb.contents[find_key]
        return ret
    else
        return nil
    end
end

local function ensure_table_cache(cache_path)
    local path = __globalhooks_datacenter.path_prefix .. cache_path
    local cache_root = __globalhooks_datacenter.cache
    if not cache_root[cache_path] then
        local ok, ret = pcall(require, path)
        if ok then
            cache_root[cache_path] = make_refs(ret, cache_path)
            if __globalhooks_datacenter.store_source then
                __globalhooks_datacenter.cache_source[cache_path] = ret
            else
                package.loaded[path] = nil
            end
        else
            print('require error', ret)
        end
    end
    return cache_root[cache_path]
end

-- 复用__globalhooks_datacenter
if not __globalhooks_datacenter then
    print('create __globalhooks_datacenter')
    __globalhooks_datacenter = {cache = {}, cache_source = {}, path_prefix = ''}
    function __globalhooks_datacenter.find(path, find_key, limit_count)
        local tb = ensure_table_cache(path)
        return src_table_find(tb, find_key, limit_count)
    end

    function __globalhooks_datacenter.remove_cache(path)
        __globalhooks_datacenter.cache[path] = nil
    end

    function __globalhooks_datacenter.clear_cache()
        __globalhooks_datacenter.cache = {}
        __globalhooks_datacenter.cache_source = {}
    end

    function __globalhooks_datacenter.get_source(path)
        if not __globalhooks_datacenter.cache_source[path] then
            ensure_table_cache(path)
        end
        return __globalhooks_datacenter.cache_source[path]
    end

    function __globalhooks_datacenter.pre_load(path)
        ensure_table_cache(path)
    end
else
    print('__globalhooks_datacenter already exists')
end
";


        public struct NormalLocker : IDisposable
        {
            private readonly object mLockSlim;
            private readonly Action mDisposeAction;

            public void Dispose()
            {
                Monitor.Exit(mLockSlim);
                mDisposeAction?.Invoke();
            }

            public NormalLocker(object locker, Action act = null)
            {
                mLockSlim = locker;
                mDisposeAction = act;
                Monitor.Enter(mLockSlim);
            }
        }

        private IDisposable TakeLuaSystem(out ILuaSystem system, out ILuaTable hooks, out ILuaFunction finder)
        {
            EnsureLuaSystem();
            hooks = mHooks;
            finder = mFindHandler;
            system = mLuaSystem;
            return new NormalLocker(mLuaLocker);
        }

        private object CallLuaFunction(string key, params object[] args)
        {
            using (TakeLuaSystem(out var system, out _, out _))
            {
                var fn = system.CastToLuaFunction(mHooks[key]);
                return fn.Call(args);
            }
        }

        private ILuaSystem mLuaSystem;
        private ILuaTable mHooks;
        private ILuaFunction mFindHandler;

        private static readonly Logger sLog = new LazyLogger(nameof(LuaDataCenter));
        private const string VirtualDir = "__virtual/";
        private readonly ILuaAdapter mAdapter;
        private readonly object mLuaLocker = new object();


        public string RootPath { get; }

        public LuaDataCenter(ILuaAdapter adapter, string root)
        {
            mAdapter = adapter;
            RootPath = root;
            Instance = this;
        }

        public void ResetLuaSystem()
        {
            lock (mLuaLocker)
            {
                ClearCache();
                mFindHandler.Dispose();
                mHooks.Dispose();
                mLuaSystem.Dispose();
                mLuaSystem = null;
                mFindHandler = null;
                mHooks = null;
            }
        }


        private void EnsureLuaSystem()
        {
            if (mLuaSystem != null)
            {
                return;
            }

            lock (mLuaLocker)
            {
                mLuaSystem = mAdapter.CreateLuaSystem(OnOutput, OnError);
                mLuaSystem.DoString(LuaDataCenterCode);
                mHooks = mLuaSystem.CastToLuaTable(mLuaSystem.GetGlobalValue("__globalhooks_datacenter"));
                mHooks["path_prefix"] = RootPath;
                mHooks["store_source"] = true;
                mFindHandler = mLuaSystem.CastToLuaFunction(mHooks["find"]);
            }
        }


        public void RemoveCache(string key)
        {
            CallLuaFunction("remove_cache", key);
        }

        public void ClearCache()
        {
            CallLuaFunction("clear_cache");
        }

        protected override void Disposing()
        {
            ResetLuaSystem();
        }


        private void OnOutput(string text)
        {
            sLog.Info(text);
        }

        private void OnError(string text)
        {
            sLog.Error(text);
        }

        public void PreLoad(string key)
        {
            CallLuaFunction("pre_load", key);
        }


        public delegate bool CheckDataValid(UnionValue data);


        public UnionValue GetCLRData(string tbKey, UnionValue args, int limitCount = 10000)
        {
            using (TakeLuaSystem(out var luaSystem, out var hooks, out var handler))
            {
                var ret = handler.Call(tbKey, luaSystem.UnionValueToInnerObject(args), limitCount);
                return luaSystem.InnerObjectToUnionValue(ret);
            }
        }

        public void SetVirtualData(string tbKey, UnionValue vKey, UnionValue vValue)
        {
            throw new NotImplementedException();
        }

        public object GetSourceTable(ILuaSystem lua, string key)
        {
            using (TakeLuaSystem(out var luaSystem, out var hooks, out _))
            {
                var fn = luaSystem.CastToLuaFunction(hooks["get_source"]);
                var ret = fn.Call(key);
                if (lua == luaSystem)
                {
                    return ret;
                }

                return luaSystem.ConvertToTargetInnerObject(ret, lua);
            }
        }

        public object GetSourceTable(string key)
        {
            return CallLuaFunction("get_source", key);
        }

        public byte[] GetFileBytes(string key)
        {
            if (!key.EndsWith(".lua"))
            {
                key = key + ".lua";
            }

            return mAdapter.GetOrLoadFileBytes(RootPath + key);
        }

        public object GetData(string tbKey, object id, int limitCount = 10000)
        {
            using (TakeLuaSystem(out _, out _, out var handler))
            {
                return handler.Call(tbKey, id, limitCount);
            }
        }


        public object GetData(ILuaSystem lua, string tbKey, object id, int limitCount = 10000)
        {
            using (TakeLuaSystem(out var system, out _, out var handler))
            {
                if (lua == system)
                {
                    return handler.Call(tbKey, id, limitCount);
                }

                id = lua.ConvertToTargetInnerObject(id, system);
                var ret = handler.Call(tbKey, id, limitCount);
                return system.ConvertToTargetInnerObject(ret, lua);
            }
        }
    }
}