namespace DeepCore.Event.Lua.EventSystem
{
    public static class LuaStringCode
    {
        public const string Code = @"
----------------------------------- global---------------------------
local PRINT_STACK = false

local function string_split(str, sep)
    local sep, fields = sep or ':', {}
    local pattern = string.format('([^%s]+)', sep)
    str:gsub(
        pattern,
        function(c)
            fields[#fields + 1] = c
        end
    )
    return fields
end

local function string_starts(String, Start)
    return string.sub(String, 1, string.len(Start)) == Start
end

local function string_ends(String, End)
    return End == '' or string.sub(String, -string.len(End)) == End
end

local function string_IsNullOrEmpty(str)
    return not str or str == ''
end

local function table_IsArray(t)
    if not t or type(t) ~= 'table' then
        return false
    end
    local i = 0
    for _ in pairs(t) do
        i = i + 1
        if t[i] == nil then
            return false
        end
    end
    return true
end

print('----------------GameEventManager-Main.Lua-----------------', _VERSION)
local function PrintTable(root)
    if not root then
        return 'nil'
    end
    local cache = {[root] = '.'}
    local function _dump(t, space, name)
        local temp = {}
        for k, v in pairs(t) do
            local key = tostring(k)
            if cache[v] then
                table.insert(temp, '+' .. key .. ' {' .. cache[v] .. '}')
            elseif type(v) == 'table' then
                local new_key = name .. '.' .. key
                cache[v] = new_key
                table.insert(temp, '+' .. key .. _dump(v, space .. (next(t, k) and '|' or ' ') .. string.rep(' ', #key), new_key))
            else
                table.insert(temp, '+' .. key .. ' [' .. tostring(v) .. ']')
            end
        end
        return table.concat(temp, '\n' .. space)
    end
    return '\n' .. _dump(root, '', '')
end

local function get_print_string(deep, ...)
    local p = {...}
    local ret = ''
    for k, v in ipairs(p) do
        local t = type(v)
        if t == 'table' then
            ret = deep and ret .. PrintTable(v) or ret .. tostring(v)
        else
            ret = ret .. tostring(v) .. '\t'
        end
    end
    return ret
end

local function dumpstack()
    if _VERSION == 'MoonSharp 2.0.0.0' then
        return ''
    end
    function vars(f)
        local dump = ''
        local func = debug.getinfo(f, 'f').func
        local i = 1
        local locals = {}
        -- get locals
        while true do
            local name, value = debug.getlocal(f, i)
            if not name then
                break
            end
            if string.sub(name, 1, 1) ~= '(' then
                dump = dump .. '    ' .. name .. '=' .. tostring(value) .. '\n'
            end
            i = i + 1
        end
        -- get varargs (these use negative indices)
        i = 1
        while true do
            local name, value = debug.getlocal(f, -i)
            -- `not name` should be enough, but LuaJIT 2.0.0 incorrectly reports `(*temporary)` names here
            if not name or name ~= '(*vararg)' then
                break
            end
            dump = dump .. '    ' .. name .. '=' .. tostring(value) .. '\n'
            i = i + 1
        end
        -- get upvalues
        i = 1
        while func do -- check for func as it may be nil for tail calls
            local name, value = debug.getupvalue(func, i)
            if not name then
                break
            end
            dump = dump .. '    ' .. name .. '=' .. tostring(value) .. '\n'
            i = i + 1
        end
        return dump
    end
    local dump = ''
    for i = 3, 100 do
        local source = debug.getinfo(i, 'S')
        if not source then
            break
        end
        dump = dump .. '- stack' .. tostring(i - 2) .. '\n'
        dump = dump .. vars(i + 1)
        if source.what == 'main' then
            break
        end
    end
    return dump
end

local inner_print = print
local function print(...)
    local ret = get_print_string(false, ...)
    CurrentManager:Log(ret)
    if PRINT_STACK then
        CurrentManager:Log(dumpstack())
    end
end

local function pprint(...)
    local ret = get_print_string(true, ...)
    print(ret)
end

local inner_error = error
local function error(msg, eventid)
    CurrentManager:LogError(msg .. '\n' .. debug.traceback() .. dumpstack())
    if eventid then
        CurrentManager:StopEvent(eventid, false, msg)
    end
end

local function copy_table(org)
    local function fold_t(func, z, t)
        -- print('fold',func,z,t)
        for name, val in pairs(t) do
            z = func(z, {key = name, val = val})
        end
        return z
    end
    local rtn = {}
    local func = function(z, i)
        if type(i.val) == 'table' then
            z[i.key] = copy_table(i.val)
        else
            z[i.key] = i.val
        end
        return z
    end
    fold_t(func, rtn, org)
    return rtn
end

----------------------------------------Sanbox----------------------------------
local Sanbox = {cache_sanbox = {}}
function Sanbox.getSandbox()
    local math = math
    local os = os
    local coroutine = coroutine
    local string = string
    local table = table
    local io = io
    local env = {}
    env.assert = assert
    env.ipairs = ipairs
    env.next = next
    env.pairs = pairs
    env.pcall = pcall
    env.print = print
    env.select = select
    env.tonumber = tonumber
    env.tostring = tostring
    env.type = type
    env.unpack = unpack
    env._VERSION = _VERSION
    --env.xpcall = xpcall

    env.coroutine = {}
    env.coroutine.create = coroutine.create
    env.coroutine.resume = coroutine.resume
    env.coroutine.running = coroutine.running
    env.coroutine.status = coroutine.status
    env.coroutine.wrap = coroutine.wrap
    env.coroutine.yield = coroutine.yield

    env.string = {}
    env.string.byte = string.byte
    env.string.char = string.char
    env.string.find = string.find
    env.string.format = string.format
    env.string.gmatch = string.gmatch
    env.string.gsub = string.gsub
    env.string.len = string.len
    env.string.lower = string.lower
    env.string.match = string.match
    env.string.rep = string.rep
    env.string.reverse = string.reverse
    env.string.sub = string.sub
    env.string.upper = string.upper
    env.string.split = string_split
    env.string.IsNullOrEmpty = string_IsNullOrEmpty

    env.table = {}
    env.table.insert = table.insert
    env.table.maxn = table.maxn
    env.table.remove = table.remove
    env.table.sort = table.sort
    env.table.concat = table.concat
    env.table.len = table.len
    env.math = {}
    env.math.abs = math.abs
    env.math.acos = math.acos
    env.math.asin = math.asin
    env.math.atan = math.atan
    env.math.atan2 = math.atan2
    env.math.cos = math.cos
    env.math.cosh = math.cosh
    env.math.deg = math.deg
    env.math.exp = math.exp
    env.math.floor = function(d)
        return math.floor(d + 0.000001)
    end
    env.math.ceil = function(d)
        return math.ceil(d - 0.000001)
    end
    env.math.fmod = math.fmod
    env.math.frexp = math.frexp
    env.math.huge = math.huge
    env.math.ldexp = math.ldexp
    env.math.log = math.log
    env.math.log10 = math.log10
    env.math.max = math.max
    env.math.min = math.min
    env.math.modf = math.modf
    env.math.pi = math.pi
    env.math.pow = math.pow
    env.math.rad = math.rad
    env.math.random = math.random
    env.math.randomseed = math.randomseed
    env.math.sin = math.sin
    env.math.sinh = math.sinh
    env.math.sqrt = math.sqrt
    env.math.tan = math.tan
    env.math.tanh = math.tanh

    env.io = {}
    env.io.read = io.read
    env.io.popen = io.popen
    env.io.write = io.write
    env.io.flush = io.flush
    env.io.type = io.type
    env.io.open = io.open

    env.os = {}
    env.os.clock = os.clock
    env.os.difftime = os.difftime
    env.os.time = os.time
    env.os.date = os.date
    env.os.execute = os.execute

    env.debug = debug
    return env
end

local functionScript = [[
    function main(fn,...)
        return fn(...)
    end
]]

function Sanbox.loadFunctionAndGetEnv(fn, otherEnv)
    local tSandbox = Sanbox.getSandbox()

    local fUntrusted, sMessage
    if _VERSION == 'Lua 5.1' then
        fUntrusted, sMessage = load(functionScript)
    else
        fUntrusted, sMessage = load(functionScript, 'bt', tSandbox)
    end
    if not fUntrusted then
        inner_error(sMessage)
    end

    local function process()
        if type(fUntrusted) ~= 'function' then
            return false, fUntrusted
        end
        local _ENV = tSandbox
        if setfenv then
            setfenv(fUntrusted, tSandbox)
        end
        return pcall(fUntrusted)
    end
    local ok, res = process()
    if not ok then
        return nil, res
    end

    if otherEnv then
        for k, v in pairs(otherEnv) do
            tSandbox[k] = v
        end
    end
    tSandbox.main = fn
    return tSandbox
end

function Sanbox.loadAndGetEnv(sFileName, otherEnv, use_cache)
    if use_cache and Sanbox.cache_sanbox[sFileName] then
        return Sanbox.cache_sanbox[sFileName]
    end
    local tSandbox = Sanbox.getSandbox()
    local fUntrusted, sMessage = loadfile(sFileName .. '.lua', 'bt', tSandbox)
    if not fUntrusted then
        inner_error(sMessage)
    end

    local function process()
        if type(fUntrusted) ~= 'function' then
            return false, fUntrusted
        end
        local _ENV = tSandbox
        if setfenv then
            setfenv(fUntrusted, tSandbox)
        end
        return pcall(fUntrusted)
    end
    local ok, res = process()
    if not ok then
        inner_error(res)
    end

    if otherEnv then
        for k, v in pairs(otherEnv) do
            tSandbox[k] = v
        end
    end
    Sanbox.cache_sanbox[sFileName] = tSandbox
    return tSandbox
end

function Sanbox.ClearCache()
    Sanbox.cache_sanbox = {}
end

function Sanbox.RemoveCache(scriptName)
    Sanbox.cache_sanbox = Sanbox.cache_sanbox or {}
    Sanbox.cache_sanbox[scriptName] = nil
end
------------------------------------------------------------------------------
-----------------------------BlackBoard---------------------------
local BlackBoard = {}
BlackBoard.__index = BlackBoard

function BlackBoard.Create()
    return setmetatable({NEXT_ID = 1, data = {}}, BlackBoard)
end

-- id有值时，按传入id作为key，id不存在时，使用内部id
function BlackBoard.Add(self, id, obj)
    if not obj then
        obj = id
        id = self.NEXT_ID
        self.NEXT_ID = self.NEXT_ID + 1
    else
        if type(id) == 'number' and not self[id] and self.NEXT_ID < id then
            self.NEXT_ID = id + 1
        end
    end
    if type(obj) ~= 'table' then
        error('type error')
    end
    self.data[id] = obj
    return id
end

function BlackBoard.Get(self, id)
    return self.data[id]
end

function BlackBoard.Remove(self, id)
    self.data[id] = nil
end

function BlackBoard.Find(self, find_iter)
    local function MatchInTable(a, b)
        for k, v in pairs(b or {}) do
            if type(v) == 'table' then
                if not MatchInTable(a[k], v) then
                    return false
                end
            elseif a[k] ~= v then
                return false
            end
        end
        return true
    end
    local str_type = type(find_iter)
    local ret = {}
    if str_type == 'table' then
        for k, v in pairs(self.data) do
            if MatchInTable(v, find_iter) then
                table.insert(ret, v)
            end
        end
    elseif str_type == 'function' then
        for k, v in pairs(self.data) do
            if find_iter(v) then
                table.insert(ret, v)
            end
        end
    end
end

function BlackBoard.GetAll(self)
    return self.data
end

local function DynamicToArgTable(...)
    local t = {len = select('#', ...), ...}
    return t
end

local function ArgTableToDynamic(arg)
    return unpack(arg, 1, arg.len)
end

----------------------------------------LuaEvent----------------------------------
local LuaEvent = {}
LuaEvent.__index = LuaEvent

function LuaEvent.Resume(self, ...)
    if coroutine.status(self.co) == 'suspended' then
        local ok, msg = coroutine.resume(self.co, ...)
        if not ok then
            error(self.ScriptDesc .. ' ' .. msg, self.ID)
        end
    else
        error('Resume error ' .. coroutine.status(self.co), self.ID)
    end
end
function LuaEvent.Yield(self, ...)
    coroutine.yield(...)
end

function LuaEvent.Start(self)
    local ok, msg = coroutine.resume(self.co, ArgTableToDynamic(self._params))
    if not ok then
        error((self.ScriptDesc or 'unknown') .. ' ' .. msg, self.ID)
    end
end

function LuaEvent.Stop(self, success)
    if self._stopFn then
        self:_stopFn(success)
    end
end
function LuaEvent.BeforeStop(self)
    if self._beforeStopFn then
        self:_beforeStopFn()
    end
end

function LuaEvent.Create(sanbox, callbacks, ...)
    local obj = setmetatable({}, LuaEvent)
    obj._params = DynamicToArgTable(...)
    obj._stopFn = callbacks.stop
    obj._beforeStopFn = callbacks.beforeStop
    local beforefn = callbacks.before
    local afterfn = callbacks.after
    obj._logicfn = sanbox.main
    obj.ScriptDesc = sanbox.ScriptDesc
    obj._errorfn = function(err)
        error(err, obj.ID)
    end

    local function node_func(...)
        sanbox.ID = obj.ID
        if beforefn then
            beforefn(obj)
        end
        local ret = {xpcall(obj._logicfn, obj._errorfn, ...)}
        local ok, isSuccess = ret[1], ret[2]
        local reason
        if not ok then
            isSuccess = false
            reason = ret[3]
        elseif isSuccess == nil then
            isSuccess = true
        end

        local params
        if isSuccess and afterfn then
            if #ret >= 3 then
                params = {}
                local p = 1
                for k, v in pairs(ret) do
                    if k ~= 1 and k ~= 2 then
                        params[p] = v
                        p = p + 1
                    end
                end
            end
        end
        afterfn(obj, isSuccess, reason, params)
    end
    obj.co = coroutine.create(node_func)
    sanbox.co = obj.co
    obj.script = sanbox
    return obj
end
------------------------------------------------------------------------------
_total_global = {
    _ids = {},
    _historyIds = {},
    blackBoard = BlackBoard.Create(),
    config = require(CurrentManager.RootPath .. CurrentManager.Config)
}

local config = _total_global.config.Managers[CurrentManager.Name]
local BaseApi = {Task = {}, Listen = {}}

if config.GenNameSpaceApi then
    print('------------GenNameSpaceApi--------------------')
    for _, v in ipairs(config.GenNameSpaceApi) do
        CurrentManager:GenNamespaceApi(v.NameSpace, CurrentManager.RootPath .. v.FileName, v.Group)
    end
    print('------------GenNameSpaceApi end----------------')
end
-----------------------------------------------------------------------------
local function AppendEventApi(sanbox)
    sanbox.Api = EventApi
    sanbox.print = EventApi.print
    sanbox.pprint = EventApi.pprint
    sanbox.log = EventApi.print
    sanbox.table.copy = EventApi.copy_table
    sanbox.error = inner_error
end

local function IsLocolManager(managerName, uuid)
    local isLocal = not managerName or (managerName == CurrentManager.Name and (not uuid or uuid == CurrentManager.UUID))
    return isLocal
end

function GetScriptPath(script_name, root)
    return CurrentManager.RootPath .. root .. script_name
end

function CreateLuaEvent(script_table, sanboxEnv, ...)
    local function before(obj)
        _total_global._ids = _total_global._ids or {}
        _total_global._ids[obj.ID] = obj.ScriptDesc
    end

    local function after(obj, success, reason, output)
        if success == nil then
            success = true
        end
        if not success then
            CurrentManager:StopEvent(obj.ID, success, reason)
        else
            CurrentManager:SetEventOutput(obj.ID, output)
            CurrentManager:StopEvent(obj.ID, true, 'main end')
        end
    end

    local function beforeStop(obj, ...)
        if obj.script.clean then
            obj.script.clean()
        end
    end

    local function stop(obj, success)
        _total_global._ids[obj.ID] = nil
        table.insert(_total_global._historyIds, {id = obj.ID, success = success})
        local len = #_total_global._historyIds
        if len > 10 then
            _total_global._historyIds = {_total_global._historyIds[len - 2], _total_global._historyIds[len - 1], _total_global._historyIds[len]}
        end
    end

    local fn_callback = {before = before, after = after, beforeStop = beforeStop, stop = stop}
    local ok, ret_sanbox
    if script_table.IsScript then
        local path = GetScriptPath(script_table.Desc, _total_global.config.ScriptRootPath)
        ok, ret_sanbox = pcall(Sanbox.loadAndGetEnv, path, sanboxEnv)
        if ok then
            ret_sanbox.ScriptDesc = script_table.Desc
        end
    elseif script_table.IsFunction then
        ok, ret_sanbox = pcall(Sanbox.loadFunctionAndGetEnv, script_table.Desc, sanboxEnv)
        if ok then
        -- ret_sanbox.ScriptDesc = 'CustomFuntion'
        end
    end

    if ok then
        AppendEventApi(ret_sanbox)
        local e = LuaEvent.Create(ret_sanbox, fn_callback, ...)
        return e
    end
end

local readonly_mt = {
    __index = t,
    __newindex = function(t, k, v)
        error('attempt to update a read-only table ')
    end
}

local function SplitParamsAndCallBack(...)
    local paramsLen = select('#', ...)
    local cb = select(paramsLen, ...)
    if type(cb) == 'function' then
        local src_params = {...}
        return DynamicToArgTable(unpack(src_params, 1, paramsLen - 1)), cb
    else
        return DynamicToArgTable(...)
    end
end

local function CreateNameApi(isRemoteApi, info, parent, k, v)
    local t = type(v)
    if not isRemoteApi and t == 'function' then
        return v
    end
    local fullApiName = table.concat(parent, '.') .. '.' .. k
    local lastParent = parent[#parent]
    return function(...)
        local nextInfo = copy_table(info)
        if t == 'string' and not isRemoteApi then
            nextInfo.Rpc = v
        else
            nextInfo.Rpc = fullApiName
        end
        local params
        if lastParent == 'Listen' then
            local cb
            params, cb = SplitParamsAndCallBack(...)
            if type(cb) == 'function' then
                nextInfo.CallBack = cb
            end
        elseif isRemoteApi and lastParent == 'Task' and k == 'StartEvent' then
            info.AllowStopByLocal = false
        end
        if not params then
            params = DynamicToArgTable(...)
        end
        nextInfo.Arg = params
        local eid = CurrentManager:CallSharpApi(nextInfo)
        if lastParent ~= 'Listen' and lastParent ~= 'Task' then
            if isRemoteApi then
                EventApi.Task.Wait(eid)
            end
            local out = CurrentManager:GetEventOutput(eid)
            if out.IsSuccess then
                if out.UnpackOutput then
                    return ArgTableToDynamic(out.Output)
                else
                    return out.Output
                end
            else
                inner_error(k .. ' not success')
            end
        end
        return eid
    end
end

local function AppendApiTo(src, target, info, parent)
    local isLocal = IsLocolManager(info.ManagerName, info.UUID)
    parent = parent or {}
    for k, v in pairs(src or {}) do
        if type(v) == 'table' then
            target[k] = target[k] or {}
            table.insert(parent, k)
            AppendApiTo(v, target[k], info, parent)
            table.remove(parent)
        else
            if isLocal then
                target[k] = CreateNameApi(not isLocal, info, parent, k, v)
            else
                target[k] = CreateNameApi(not isLocal, info, parent, k, v)
            end
        end
    end
end

local function CreateManagerApi(managerName, uuid)
    local ret = {}
    local info = {ManagerName = managerName, UUID = uuid}
    AppendApiTo(BaseApi, ret, info)
    local managerConfig = _total_global.config.Managers[managerName]
    local apiList = managerConfig.ApiList
    for _, v in ipairs(apiList) do
        local path = CurrentManager.RootPath .. v
        package.loaded[path] = nil
        local ok, t = pcall(require, path)
        if not ok then
            error('require api file error ' .. path)
        else
            print('AppendApi', v)
            AppendApiTo(t, ret, info)
        end
    end
    -- pprint('api list', ret)
    ret.RootPath = CurrentManager.RootPath .. _total_global.config.ScriptRootPath
    ret.pprint = pprint
    ret.log = print
    ret.print = print
    ret.string_IsNullOrEmpty = string_IsNullOrEmpty
    ret.string_split = string_split
    ret.PrintTable = PrintTable
    ret.copy_table = copy_table
    if ret.Task then
        setmetatable(ret.Task, readonly_mt)
    end
    setmetatable(ret, readonly_mt)
    return ret
end

------------------------------base api----------------------------
local function CreateEventTable(scriptName, ...)
    local t = type(scriptName)
    local ret
    if t == 'string' then
        ret = CreateLuaEvent({Desc = scriptName, IsScript = true}, _total_global.config.SanboxAppendEnv, ...)
    elseif t == 'function' then
        ret = CreateLuaEvent({Desc = scriptName, IsFunction = true}, _total_global.config.SanboxAppendEnv, ...)
    else
        error(string.format('argument error type:%s arg:%s', t, tostring(scriptName)))
    end
    return ret
end

function BaseApi.Task.DelaySec(sec)
    return CurrentManager:CallSharpApi({Rpc = 'DeepCore.GameEvent.Events.DelaySecEvent', Arg = {sec}})
end

function BaseApi.Listen.AddPeriodicSec(...)
    return EventApi.DoSharpApi('Listen', 'DeepCore.GameEvent.Events.PeriodicSecEvent', ...)
end

function BaseApi.ReStart()
    CurrentManager:ReStart()
end

function BaseApi.Task.StartWaitAlways()
    return CurrentManager:CallSharpApi({Rpc = 'DeepCore.GameEvent.Events.WaitAlwaysEvent', Arg = {sec}})
end

-- 所有和c#交互的api入口
function BaseApi.DoSharpApi(cate, rpc, ...)
    local info = {}
    info.Rpc = rpc
    if cate == 'Listen' then
        info.IsTriggerEvent = true
        local params, cb = SplitParamsAndCallBack(...)
        info.Arg = params
        if type(cb) == 'function' then
            -- info.CallBack = function(...)
            --     EventApi.Task.AddEvent(cb, ...)
            -- end
            info.CallBack = cb
        end
    else
        info.Arg = DynamicToArgTable(...)
    end

    local id = CurrentManager:CallSharpApi(info)
    if cate == 'Sync' then
        local out = CurrentManager:GetEventOutput(id)
        if out.IsSuccess then
            if out.UnpackOutput then
                return ArgTableToDynamic(out.Output)
            else
                return out.Output
            end
        else
            inner_error(rpc .. ' not success')
        end
    else
        return id
    end
end

--! @brief 监听一个事件的Trigger，触发时会添加一个新事件来执行此次触发
--! @param eid 事件ID
--! @param fn 监听方法
function BaseApi.ListenEvent(eid, fn)
    CurrentManager:ListenEvent(
        eid,
        function(...)
            EventApi.Task.AddEvent(fn, ...)
        end
    )
end

function BaseApi.Listen.RemoveEventListen(eid)
    CurrentManager:RemoveEventListen(eid)
end

function BaseApi.TriggerEvent(id, ...)
    CurrentManager:TriggerLuaEvent(id, DynamicToArgTable(...))
end

function BaseApi.Task.Wait(id)
    local waitResult
    if not id then
        waitResult = CurrentManager:WaitAll()
    else
        waitResult = CurrentManager:Wait(id)
    end
    if not waitResult then
        error('cannot use Wait or Sleep')
        return
    end
    local success, eventID = coroutine.yield()
    if eventID ~= 0 then
        local out = CurrentManager:GetEventOutput(eventID)
        if out.IsSuccess and out.UnpackOutput then
            return success, ArgTableToDynamic(out.Output)
        else
            return success, out.Output
        end
    else
        return success
    end
end

function BaseApi.Task.WaitSelect(...)
    local params = {...}
    local waitResult = CurrentManager:WaitSelect(params)
    if not waitResult then
        error('cannot use Wait or Sleep')
        return
    end
    local success, eventID = coroutine.yield()
    if eventID ~= 0 then
        local out = CurrentManager:GetEventOutput(eventID)
        if out.IsSuccess and out.UnpackOutput then
            return success, eventID, ArgTableToDynamic(out.Output)
        else
            return success, eventID, out.Output
        end
    else
        return success
    end
end

function BaseApi.IsEventStoped(id)
    return CurrentManager:IsEventStoped(id)
end

function BaseApi.IsEventSuccess(id)
    if CurrentManager:IsEventExists(id) then
        return CurrentManager:IsEventSuccess(id)
    end
    for i, v in ipairs(_total_global._historyIds) do
        if v.id == id then
            return v.success
        end
    end
    return false
end

function BaseApi.Task.Sleep(sec)
    EventApi.Task.Wait(EventApi.Task.DelaySec(sec))
end

function BaseApi.Task.ContinueWith(id, fn)
    return CurrentManager:ContinueWith(id, fn)
end

function BaseApi.AddCacheData(...)
    return _total_global.blackBoard:Add(...)
end
function BaseApi.GetCacheData(...)
    return _total_global.blackBoard:Get(...)
end
function BaseApi.RemoveCacheData(...)
    return _total_global.blackBoard:Remove(...)
end
function BaseApi.FindCacheData(...)
    return _total_global.blackBoard:Find(...)
end
function BaseApi.GetAllCacheData()
    return _total_global.blackBoard:GetAll()
end

function BaseApi.Task.AddEventTo(eid, scriptName, ...)
    local ret = CreateEventTable(scriptName, ...)
    if ret then
        return CurrentManager:AddLuaEventTo(eid, ret)
    end
end

function BaseApi.Task.AddEvent(scriptName, ...)
    local ret = CreateEventTable(scriptName, ...)
    if ret then
        return CurrentManager:AddLuaEvent(ret)
    end
end

function BaseApi.Task.StartEvent(scriptName, ...)
    local ret = CreateEventTable(scriptName, ...)
    if ret then
        return CurrentManager:StartLuaEvent(ret)
    end
end

function BaseApi.SetEventOutput(id, ...)
    CurrentManager:SetEventOutput(id, DynamicToArgTable(...))
end

function BaseApi.Task.StopEvent(id, result, reason)
    if result == nil then
        result = true
    end
    reason = reason or 'StopEvent'
    if type(id) == 'string' then
        local eids = {EventApi.GetEventID(id)}
        for _, v in ipairs(eids) do
            return CurrentManager:StopEvent(v, result, reason)
        end
    else
        return CurrentManager:StopEvent(id, result, reason)
    end
end
function BaseApi.GetEventID(scriptName)
    local ret = {}
    for id, v in pairs(_total_global._ids or {}) do
        if v == scriptName then
            table.insert(ret, id)
        end
    end
    if table_IsArray(ret) then
        return unpack(ret)
    else
        return ret
    end
end
function BaseApi.CreateRemoteApi(managerName, uuid)
    if IsLocolManager(managerName, uuid) then
        return EventApi
    else
        local ret = CreateManagerApi(managerName, uuid)
        return ret
    end
end

function BaseApi.ClearScriptCache()
    Sanbox.ClearCache()
end
function BaseApi.RemoveScriptCache(scriptName)
    Sanbox.RemoveCache(scriptName)
end

function BaseApi.ReStart()
    CurrentManager:ReStart()
end

function BaseApi.Task.StartTrunk(trunk, ...)
    trunk = 'return function(arg) ' .. trunk .. 'end'
    local fn = load(trunk)
    return EventApi.Task.StartEvent(fn, ...)
end

function BaseApi.IsLocolManager(managerName, uuid)
    return IsLocolManager(managerName, uuid)
end

function BaseApi.Task.WaitAlways()
    local id = EventApi.Task.StartWaitAlways()
    EventApi.Task.Wait(id)
end

function BaseApi.Stop(isSuccess, output, reason)
    return CurrentManager:CallSharpApi({Rpc = 'DeepCore.GameEvent.Events.StopParentEvent', Arg = {isSuccess, output, reason}})
end

function BaseApi.GetCurrentEventID()
    return CurrentManager:GetCurrentEventID()
end

function BaseApi.GetParentEventID(id)
    return CurrentManager:GetParentEventID(id)
end
------------------------------------------------------------------
-------------------------Use in csharp---------------------------
function CreateServerEventTable(apiName, arg)
    pprint('CreateServerEventTable', apiName, arg)
    local list = string_split(apiName, '.')
    local func = EventApi
    for k, v in ipairs(list) do
        if type(func) == 'table' then
            func = func[v]
        else
            break
        end
    end
    if type(func) ~= 'function' then
        error('call StartLuaApi error')
    else
        local function ServerEventLogic(...)
            if #list ~= 1 then
                local eid = func(...)
                EventApi.ListenEvent(
                    eid,
                    function(...)
                        local fatherID = EventApi.GetParentEventID(eid)
                        EventApi.TriggerEvent(fatherID, ...)
                    end
                )
                return EventApi.Task.Wait(eid)
            else
                return true, func(...)
            end
        end
        local eventTable = CreateEventTable(ServerEventLogic, ArgTableToDynamic(arg))
        if eventTable then
            return eventTable
        end
    end
end

EventApi = CreateManagerApi(CurrentManager.Name, CurrentManager.UUID, true)
----------------------------------------------------------------------

if config.InitScript then
    EventApi.Task.StartEvent(config.InitScript)
end




";
    }
}
