using DeepCore.Event.EventSystem;
using DeepCore.Event.EventSystem.Events;
using DeepCore.GameEvent.Events;
using DeepCore.GameEvent.Message;
using DeepCore.Lua;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.Event.Lua.EventSystem
{
    public class LuaEventManager : EventManager
    {
        public readonly ILuaAdapter LuaAdapter;
        private readonly Stack<LuaWorldEvent> mCurrentStack = new Stack<LuaWorldEvent>();

        public string Config { get; set; }

        public string RootPath { get; set; }

        public string CustomMainLua { get; set; }

        public ILuaSystem LuaSystem { get; private set; }

        public LuaEventManager(string name, string uid, ILuaAdapter adapter = null) : base(name, uid)
        {
            LuaAdapter = adapter ?? DefaultAdapter;
            RootPath = "";
        }

        public static ILuaAdapter DefaultAdapter { get; set; }

        protected override void Disposing()
        {
            base.Disposing();
            mCurrentStack.Clear();
        }

        private ILuaSystem CreateLuaSvr()
        {
            var ret = LuaAdapter.CreateLuaSystem(Log, LogError, typeof(LuaEventManager));
            var package = ret.CastToLuaTable(ret.GetGlobalValue("package"));
            package["path"] = $"?.lua;{RootPath}event_script/?.lua";
            return ret;
        }

        private void ClearLuaSvr()
        {
            LuaSystem.Dispose();
            LuaSystem = null;
        }

        public void PushLuaEvent(LuaWorldEvent e)
        {
            mCurrentStack.Push(e);
        }

        public void PopLuaEvent()
        {
            mCurrentStack.Pop();
        }

        private LuaWorldEvent PeekLuaEvent()
        {
            return mCurrentStack.Count > 0 ? mCurrentStack.Peek() : null;
        }

        private object InvokeLuaGlobalFunction(string func, params object[] objs)
        {
            var fn = LuaSystem.GetGlobalValue(func) as ILuaFunction;
            if (fn != null)
            {
                return SafeCallFunction(fn, objs);
            }

            return null;
        }

        protected override void OnStop()
        {
            base.OnStop();
            InvokeLuaGlobalFunction("OnEventManagerStop");
            ClearLuaSvr();
        }

        protected override void OnBeforeStop()
        {
            base.OnBeforeStop();
            InvokeLuaGlobalFunction("OnEventManagerBeforeStop");
        }

        protected override void OnStart(string reason)
        {
            base.OnStart(reason);
            LuaSystem = CreateLuaSvr();
            if (string.IsNullOrEmpty(CustomMainLua))
            {
                LuaSystem.DoString(LuaStringCode.Code);
            }
            else
            {
                LuaSystem.DoFile(RootPath + CustomMainLua);
            }

            InvokeLuaGlobalFunction("SetCurrentEventManager", this, reason);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            LuaSystem.Update();
        }

        public override void LogException(Exception e)
        {
            var err = LuaSystem != null ? LuaSystem.FormatException(e) : null;
            if (!string.IsNullOrEmpty(err))
            {
                LogError(err);
            }
            else
            {
                base.LogException(e);
            }
        }

        private BaseEvent CreateLuaEvent(object t)
        {
            if (t == null)
            {
                throw new Exception("t is null");
            }

            var ret = new LuaWorldEvent();
            ret.SetTable(LuaSystem.CastToLuaTable(t));
            return ret;
        }

        protected override void OnEventStop(BaseEvent e)
        {
            base.OnEventStop(e);
            if (e.Parent != null)
            {
                return;
            }

            InvokeLuaGlobalFunction("OnRootEventStop", e.ID);
        }


        #region 给Lua使用的Api

        public bool Wait(int id)
        {
            return mCurrentStack.Peek().Wait(id);
        }

        public bool IsBeforeStop()
        {
            return mCurrentStack.Peek().IsBeforeStop;
        }

        private int[] ParseWaitArray(object obj)
        {
            var arg = LuaSystem.InnerObjectToUnionValue(obj);
            var ids = new int[arg.Arr.Count];
            for (var i = 0; i < arg.Arr.Count; i++)
            {
                ids[i] = (int)arg.Arr[i];
            }

            return ids;
        }

        public bool WaitAny(object list)
        {
            return mCurrentStack.Peek().WaitAny(ParseWaitArray(list));
        }

        public bool WaitSelect(object selectlist)
        {
            return mCurrentStack.Peek().WaitSelect(ParseWaitArray(selectlist));
        }

        public bool WaitParallel(object sequencelist)
        {
            return mCurrentStack.Peek().WaitParallel(ParseWaitArray(sequencelist));
        }

        public void ListenEvent(int id, object cb)
        {
            var e = GetEvent(id);
            if (e == null)
            {
                LogError("not found event " + id);
                return;
            }

            var fn = LuaSystem.CastToLuaFunction(cb);
            if (fn != null)
            {
                mCurrentStack.Peek().BindTrigger(e, fn);
            }
            else
            {
                throw new Exception("cb == null");
            }
        }

        public void TriggerLuaEvent(int eid, object p)
        {
            var e = GetEvent(eid);
            if (e == null || e.IsStoped || e.IsBeforeStop)
            {
                return;
            }

            var arg = LuaSystem.InnerObjectToUnionValue(p);
            var v = arg.TryMapToArray(true, 1, true);
            v = v.IsArray ? v : arg[1];
            e.Trigger(v);
        }

        public bool WaitAll()
        {
            return mCurrentStack.Peek().WaitAll();
        }


        public int StartLuaEvent(object t)
        {
            try
            {
                return StartEvent(CreateLuaEvent(t)).ID;
            }
            catch (Exception e)
            {
                TryFixException(e);
                return 0;
            }
        }

        public int AddLuaEventTo(int id, object t)
        {
            try
            {
                var p = GetEvent(id);
                if (p == null || p.IsStoped)
                {
                    //已停止不允许添加事件
                    return 0;
                }

                var e = CreateLuaEvent(t);
                p.AddChild(e);
                return e.ID;
            }
            catch (Exception e)
            {
                TryFixException(e);
                return 0;
            }
        }

        public int AddLuaEvent(object t)
        {
            try
            {
                var parent = mCurrentStack.Peek();
                if (parent == null)
                {
                    LogError("current parent is null");
                    return 0;
                }

                if (parent.IsStoped)
                {
                    //已停止不允许添加事件
                    return 0;
                }

                var e = CreateLuaEvent(t);
                parent.Do(e);
                return e.ID;
            }
            catch (Exception e)
            {
                TryFixException(e);
                return 0;
            }
        }

        public bool IsCurrentManager(string managerName, string uuid)
        {
            return string.IsNullOrEmpty(managerName) || managerName == Name && uuid == UUID;
        }


        public int CallSharpApi(object p)
        {
            try
            {
                //todo xxxxx 反序列化/序列化 优化- 直接从LuaTable转换成Event的Arg
                var arg = LuaSystem.InnerObjectToUnionValue(p);
                var info = UnionValueSerializer.Deserialize<LuaRpcInfo>(arg);
                if (IsCurrentManager(info.ManagerName, info.UUID))
                {
                    BaseEvent peekEvent;
                    if (info.ParentEvent != 0)
                    {
                        peekEvent = GetEvent(info.ParentEvent);
                    }
                    else
                    {
                        peekEvent = mCurrentStack.Count > 0 ? mCurrentStack.Peek() : null;
                    }

                    var e = CreateEvent(info.Rpc);
                    var einfo = Decorator.Get(e.GetType());

                    if (einfo.IsSyncEvent)
                    {
                        peekEvent = null;
                    }
                    else if (peekEvent == null)
                    {
                        LogError("current parent is null");
                        return 0;
                    }

                    var argIndex = einfo.ArgIndex;
                    e.Arg = !argIndex ? info.Arg[1] : info.Arg.TryMapToArray(true, 1, true);

                    if (info.IsTriggerEvent && peekEvent != null)
                    {
                        if (info.CallBack != null)
                        {
                            if (peekEvent is LuaWorldEvent luaEvent)
                            {
                                luaEvent.BindTrigger(e, info.CallBack);
                            }
                            else
                            {
                                e.OnTrigger += (trigger, value) => SafeCallFunction(info.CallBack, value);
                            }
                        }
                        else
                        {
                            e.OnTrigger += (trigger, value) =>
                            {
                                e.Stop(true, "trigger once");
                                return UnionValue.Null;
                            };
                        }
                    }

                    if (peekEvent != null)
                    {
                        peekEvent.AddChild(e);
                    }
                    else
                    {
                        StartEvent(e, einfo.IsSyncEvent);
                    }

                    return e.ID;
                }


                //remote
                var msg = new StartEventMessage()
                {
                    Argument = info.Arg,
                    EventDesc = info.Rpc,
                    From = Address,
                    IsStartEvent = info.IsStartEvent,
                    To = !info.Broadcast ? GetAddress(info.ManagerName, info.UUID) : null
                };

                BaseEvent sharpEvent;
                if (info.Broadcast)
                {
                    sharpEvent = new RemoteMultiLocalEvent(info.ManagerName, msg, info.Config);
                }
                else
                {
                    sharpEvent = new RemoteLocalEvent(msg);
                }

                if (info.IsStartEvent)
                {
                    StartEvent(sharpEvent);
                }
                else
                {
                    BaseEvent peekEvent;
                    if (info.ParentEvent != 0)
                    {
                        peekEvent = GetEvent(info.ParentEvent);
                    }
                    else
                    {
                        peekEvent = mCurrentStack.Count > 0 ? mCurrentStack.Peek() : null;
                    }

                    if (peekEvent == null)
                    {
                        LogError("current parent is null");
                        return 0;
                    }

                    peekEvent.AddChild(sharpEvent);
                }

                return sharpEvent.ID;
            }
            catch (Exception e)
            {
                TryFixException(e);
                return 0;
            }
        }


        public object GetEventOutput(int id)
        {
            var e = GetEvent(id);
            if (e != null)
            {
                var v = UnionValue.NewMap;
                v["IsSuccess"] = e.IsSuccessed;
                var info = Decorator.Get(e.GetType());
                var needUnpack = info == null || info.OutputIndex;
                v["UnpackOutput"] = needUnpack;
                if (needUnpack && e.IsSuccessed)
                {
                    if (e.Output.IsArray)
                    {
                        var vv = e.Output.TryArrayToMap(1);
                        vv["len"] = e.Output.ElementCount;
                        v["Output"] = vv;
                    }
                    else
                    {
                        var vv = UnionValue.NewMap;
                        vv["len"] = 1;
                        vv[1] = e.Output;
                        v["Output"] = vv;
                    }
                }
                else
                {
                    v["Output"] = e.Output;
                }

                return LuaSystem.UnionValueToInnerObject(v);
            }

            return null;
        }

        public void StopEvent(int id, bool success, string reason)
        {
            var e = GetEvent(id);
            if (e != null)
            {
                e.Stop(success, reason);
            }
        }

        public int GetCurrentEventID()
        {
            var peek = PeekLuaEvent();
            return peek != null ? peek.ID : 0;
        }

        public int GetParentEventID(int id)
        {
            var e = GetEvent(id);
            var parentID = 0;
            if (e != null && e.Parent != null)
            {
                parentID = e.Parent.ID;
            }

            return parentID;
        }

        public int GetRootEventID(int id)
        {
            var e = GetEvent(id);
            return e != null ? e.RootEvent.ID : 0;
        }


        public bool IsEventStoped(int id)
        {
            var e = GetEvent(id);
            return e == null || e.IsStoped;
        }

        public bool IsEventSuccess(int id)
        {
            var e = GetEvent(id);
            return e != null && e.IsSuccessed;
        }

        public bool IsEventExists(int id)
        {
            var e = GetEvent(id);
            return e != null;
        }

        public void SetEventOutput(int id, object obj)
        {
            var e = GetEvent(id);
            if (e != null)
            {
                var info = Decorator.Get(e.GetType());
                var outIndex = info == null || info.OutputIndex;
                var output = LuaSystem.InnerObjectToUnionValue(obj);
                e.Output = !outIndex ? output[1] : output.TryMapToArray(true, 1, true);
            }
        }

        public int ContinueWith(int id, object fnObj)
        {
            var targetEvent = GetEvent(id);
            var act = new Action<BaseEvent>(e =>
            {
                var fn = LuaSystem.CastToLuaFunction(fnObj);
                SafeCallFunction(fn);
            });
            if (targetEvent == null || targetEvent.IsStoped)
            {
                var ve = BaseEvent.CreateActionEvent(act);
                ve.Output = targetEvent != null ? targetEvent.Output : UnionValue.Null;
                PeekLuaEvent().Do(ve);
                return ve.ID;
            }

            return targetEvent.ContinueWith(act).ID;
        }

        public void SetFileDirty(string fileName)
        {
            LuaAdapter.RemoveFileCache(fileName);
        }

        #endregion

        public object UnionValueToLuaObject(UnionValue v)
        {
            return LuaSystem.UnionValueToInnerObject(v);
        }

        protected override Type[] UnionValueKeepTypes
        {
            get { return LuaAdapter.GetInnerTypes(); }
        }

        public object GetEventSandbox(int id)
        {
            var e = GetEvent(id);
            if (e is LuaWorldEvent)
            {
                return ((LuaWorldEvent)e).GetSandbox();
            }

            return null;
        }

        public object CallFunction(string strFn, params object[] objs)
        {
            try
            {
                using (LockUpdating())
                {
                    var strs = strFn.Split('.');
                    ILuaFunction fn;
                    if (strs.Length == 1)
                    {
                        fn = (ILuaFunction)LuaSystem.GetGlobalValue(strFn);
                    }
                    else
                    {
                        var table = (ILuaTable)LuaSystem.GetGlobalValue(strs[0]);
                        for (var i = 1; i < strs.Length - 1; i++)
                        {
                            table = (ILuaTable)table[strs[i]];
                        }

                        fn = (ILuaFunction)table[strs[strs.Length - 1]];
                    }

                    using (fn)
                    {
                        return fn.Call(objs);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(string.Format("call lua api {0} error,LuaSystem: {1}", strFn, LuaSystem));
                TryFixException(e);
                return null;
            }
        }

        internal object SafeCallFunction(ILuaFunction fn, params object[] objs)
        {
            try
            {
                return fn?.Call(objs);
            }
            catch (Exception e)
            {
                //lua 报错lua负责打印
                throw new FixedException(e);
            }
        }

        internal UnionValue SafeCallFunction(ILuaFunction fn, UnionValue v)
        {
            try
            {
                object[] p;
                if (v.IsArray)
                {
                    p = new object[v.ElementCount];
                    for (var i = 0; i < v.ElementCount; i++)
                    {
                        p[i] = LuaSystem.UnionValueToInnerObject(v[i]);
                    }
                }
                else if (!v.IsNull)
                {
                    p = new object[] { LuaSystem.UnionValueToInnerObject(v) };
                }
                else
                {
                    p = new object[0];
                }

                var ret = fn?.Call(p);
                return ret != null ? LuaSystem.InnerObjectToUnionValue(ret) : UnionValue.Null;
            }
            catch (Exception e)
            {
                //lua 报错lua负责打印
                throw new FixedException(null);
            }
        }

        protected override BaseEvent CreateServerEntityEvent(string eType, UnionValue arg)
        {
            try
            {
                var innerObj = LuaSystem.UnionValueToInnerObject(arg);
                var ret = InvokeLuaGlobalFunction("CreateServerEventTable", eType, innerObj);
                return CreateLuaEvent(ret);
            }
            catch (Exception)
            {
                return null;
            }
        }


        public void GenNamespaceApi(object nameSpaceInfo, string targetFullPath, string group)
        {
            var ret = new StringBuilder();
            try
            {
                var uv = LuaSystem.InnerObjectToUnionValue(nameSpaceInfo);
                var nss = new List<string>();
                if (uv.IsArray)
                {
                    uv.ForEachElement((k, v) => nss.Add((string)v));
                }
                else
                {
                    nss.Add((string)nameSpaceInfo);
                }

                var all = ReflectionUtil.GetNoneVirtualSubTypes(typeof(BaseEvent));
                var zoneAll = new List<Type>();
                foreach (var type in all)
                {
                    if (nss.IndexOf(type.Namespace) >= 0)
                    {
                        zoneAll.Add(type);
                    }
                }
                var syncStr = new StringBuilder();
                var asyncStr = new StringBuilder();
                var listenStr = new StringBuilder();
                syncStr.AppendLine("local Api = {Task={},Listen={}}");
                syncStr.AppendLine("local Task = Api.Task");
                syncStr.AppendLine("local Listen = Api.Listen");
                var namespaceMap = new HashMap<string, bool>();
                foreach (var type in zoneAll)
                {
                    var attrs = type.GetCustomAttributes(typeof(EventAttribute), true);
                    if (attrs.Length == 0)
                    {
                        continue;
                    }

                    var attr = (EventAttribute)attrs[0];
                    string prefixName = null;
                    if (attr.Category.EndsWith("Listen"))
                    {
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Listen".Length);
                    }
                    else if (attr.Category.EndsWith("Sync"))
                    {
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Sync".Length);
                    }
                    else if (attr.Category.EndsWith("Async"))
                    {
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Async".Length);
                    }
                    else
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(prefixName) && !namespaceMap.Get(prefixName))
                    {
                        namespaceMap[prefixName] = true;
                        var cname = prefixName.Substring(0, prefixName.Length - 1);
                        syncStr.AppendLine("local " + cname + " = {Task ={},Listen={}}");
                        syncStr.AppendLine("Api." + cname + " = " + cname);
                    }
                }

                foreach (var type in zoneAll)
                {
                    var attrs = type.GetCustomAttributes(typeof(EventAttribute), true);
                    if (attrs.Length == 0)
                    {
                        continue;
                    }

                    var attr = (EventAttribute)attrs[0];
                    var eindex = type.Name.IndexOf("Event", StringComparison.Ordinal);
                    if (eindex < 0)
                    {
                        LogWarn(string.Format("{0} must ends with Event", type.Name));
                        continue;
                    }

                    var name = type.Name.Substring(0, eindex);
                    StringBuilder currentStrBuild = null;
                    var argStr = new StringBuilder();
                    string formatStr = null;
                    string prefixName = null;
                    if (attr.Category.EndsWith("Listen"))
                    {
                        currentStrBuild = listenStr;
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Listen".Length);
                        formatStr = "function " + prefixName + "Listen.{0}({1})";
                    }
                    else if (attr.Category.EndsWith("Sync"))
                    {
                        currentStrBuild = syncStr;
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Sync".Length);
                        if (string.IsNullOrEmpty(prefixName))
                        {
                            prefixName = "Api.";
                        }

                        formatStr = "function " + prefixName + "{0}({1})";
                    }
                    else if (attr.Category.EndsWith("Async"))
                    {
                        currentStrBuild = asyncStr;
                        prefixName = attr.Category.Substring(0, attr.Category.Length - "Async".Length);
                        formatStr = "function " + prefixName + "Task.{0}({1})";
                    }
                    else
                    {
                        LogError("Event Category error" + attr.Category);
                        continue;
                    }

                    currentStrBuild.AppendLine(string.Format("--! @brief {0}", attr.Desc));
                    var fs = Decorator.Get(type);
                    var argDocStr = new StringBuilder();
                    var outputStr = new StringBuilder();
                    if (fs != null)
                    {
                        if (!fs.ArgIndex && fs.Arg.Count > 0)
                        {
                            argStr.Append("argMap");
                            argDocStr.AppendLine("--! @param argMap");
                        }

                        for (var i = 0; i < fs.Arg.Count; i++)
                        {
                            var fName = fs.Arg[i].Value.Name;
                            if (fs.ArgIndex)
                            {
                                argDocStr.AppendLine(string.Format("--! @param {0} {1}", fName, fs.Arg[i].Value.Desc));
                                argStr.Append(fName);
                                if (i < fs.Arg.Count - 1)
                                {
                                    argStr.Append(',');
                                }
                            }
                            else
                            {
                                argDocStr.AppendLine(string.Format("--! - {0} {1}", fName, fs.Arg[i].Value.Desc));
                            }

                            var writeClassFieldd = new Action<string, Type>((start, t) =>
                            {
                                try
                                {
                                    if (UnionValue.IsNativeType(t))
                                    {
                                        return;
                                    }

                                    var allSubFields = t.GetFields();
                                    foreach (var subField in allSubFields)
                                    {
                                        var descAttr = subField.GetCustomAttributes(typeof(DescAttribute), true);
                                        var subFieldDesc = "";
                                        if (descAttr.Length > 0)
                                        {
                                            subFieldDesc = ((DescAttribute)descAttr[0]).Desc;
                                        }

                                        argDocStr.AppendLine(string.Format("{0}{1} {2}", start, subField.Name, subFieldDesc));
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogWarn("GenNamespaceApi " + e.Message + e.StackTrace);
                                }
                            });

                            var fType = fs.Arg[i].Key.FieldType;
                            if (fType.IsArray)
                            {
                                argDocStr.AppendLine("--! \t- 参数为一个Array []");
                                writeClassFieldd.Invoke("--! \t\t- ", fType.GetElementType());
                            }
                            else if (fType.GetInterface(typeof(IList).Name) != null)
                            {
                                argDocStr.AppendLine("--! \t- 参数为一个Array []");
                                writeClassFieldd.Invoke("--! \t\t- ", fType.GetGenericArguments()[0]);
                            }
                            else if (fType.GetInterface(typeof(IDictionary).Name) != null)
                            {
                                argDocStr.AppendLine("--! \t- 参数为一个Map ");
                                writeClassFieldd.Invoke("--! \t\t- Key:", fType.GetGenericArguments()[0]);
                                writeClassFieldd.Invoke("--! \t\t- Value:", fType.GetGenericArguments()[1]);
                            }
                            else if (fType.IsClass)
                            {
                                writeClassFieldd.Invoke("--! \t- ", fType);
                            }
                        }
                    }

                    if (attr.Category == "Listen")
                    {
                        if (!string.IsNullOrEmpty(argStr.ToString()))
                        {
                            argStr.Append(",");
                        }

                        argStr.Append("cb");
                    }

                    var argstring = argStr.ToString();

                    if (fs != null)
                    {
                        if (!fs.OutputIndex && fs.Output.Count > 0)
                        {
                            outputStr.AppendLine("--! @return ret ");
                        }

                        foreach (var outEntry in fs.Output)
                        {
                            if (fs.OutputIndex)
                            {
                                outputStr.AppendLine(string.Format("--! @return {0} {1}", outEntry.Value.Name, outEntry.Value.Desc));
                            }
                            else
                            {
                                outputStr.AppendLine(string.Format("--! - {0} {1}", outEntry.Value.Name, outEntry.Value.Desc));
                            }
                        }
                    }

                    currentStrBuild.Append(argDocStr);
                    currentStrBuild.Append(outputStr);


                    //参数还是用...
                    argstring = "...";
                    currentStrBuild.AppendLine(string.Format(formatStr, name, argstring));
                    if (string.IsNullOrEmpty(argstring))
                    {
                        currentStrBuild.AppendLine(string.Format("\treturn EventApi.DoSharpApi('{0}','{1}')", attr.Category, type.FullName));
                    }
                    else
                    {
                        currentStrBuild.AppendLine(string.Format("\treturn EventApi.DoSharpApi('{0}','{1}',{2})", attr.Category, type.FullName, argstring));
                    }

                    currentStrBuild.AppendLine("end");
                }

                ret.AppendLine(string.Format("--! @addtogroup {0}\n--! @{{", group));
                ret.Append(syncStr);
                ret.Append(asyncStr);
                ret.Append(listenStr);
                ret.AppendLine("return Api");
                ret.AppendLine("--! @}");
                File.WriteAllText(targetFullPath, ret.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                LogWarn("GenNamespaceApi targetFullPath::::" + targetFullPath + " ret.ToString():::: " + ret.ToString() + "   " + e.Message + e.StackTrace);
            }
        }
    }
}