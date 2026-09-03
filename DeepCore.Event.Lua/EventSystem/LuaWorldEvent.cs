using DeepCore.Event.EventSystem.Events;
using DeepCore.Lua;
using System;
using System.Linq;

namespace DeepCore.Event.Lua.EventSystem
{
    //TODO lua中支持Listen 指定的LuaWorldEvent
    public class LuaWorldEvent : LuaBaseEvent
    {
        private ILuaTable mLuaTable;

        private ILuaFunction mResumeFn;
        private ILuaFunction mStartFn;
        private ILuaFunction mStopFn;
        private ILuaFunction mBeforeStopFn;

        private int mWaitID;
        private int[] mSelectWaitList;
        private int[] mWaitParallelList;
        private int[] mWaitAnyList;
        private bool mWaitAll;


        internal void SetTable(ILuaTable etable)
        {
            mLuaTable = etable;
            mLuaTable["ID"] = ID;
            var obj = mLuaTable["ScriptDesc"];
            if (obj != null)
            {
                Desc = obj.ToString();
            }
        }

        public object GetSandbox()
        {
            var t = (ILuaTable)mLuaTable["script"];
            return t.InnerTable;
        }

        public bool Wait(int id)
        {
            if (IsStoped || IsStopedState(NextState) || Mgr.IsEventStoped(id))
            {
                return false;
            }

            mWaitID = id;
            mWaitAll = false;
            return true;
        }

        public bool WaitSelect(int[] ids)
        {
            if (IsStoped || IsStopedState(NextState))
            {
                return false;
            }

            mSelectWaitList = ids;
            return true;
        }

        public bool WaitParallel(int[] ids)
        {
            if (IsStoped || IsStopedState(NextState))
            {
                return false;
            }

            mWaitParallelList = ids;
            return true;
        }

        public bool WaitAny(int[] ids)
        {
            if (IsStoped || IsStopedState(NextState))
            {
                return false;
            }

            mWaitAnyList = ids;
            return true;
        }

        public bool WaitAll()
        {
            if (IsStoped || IsStopedState(NextState))
            {
                return false;
            }

            mWaitAll = true;
            mWaitID = 0;
            return true;
        }

        protected override void OnBeforeStop()
        {
            Mgr.PushLuaEvent(this);
            try
            {
                mLuaTable["IsSuccessed"] = NextState == EventState.Successed;
                mLuaTable["ResultReason"] = ResultReason;
                base.OnBeforeStop();
                if (mBeforeStopFn != null)
                {
                    Mgr.SafeCallFunction(mBeforeStopFn, mLuaTable.InnerTable);
                }
            }
            finally
            {
                Mgr.PopLuaEvent();
            }
        }

        protected override void OnStart()
        {
            Mgr.PushLuaEvent(this);
            try
            {
                base.OnStart();

                mStartFn = mLuaTable["Start"] as ILuaFunction;
                mBeforeStopFn = mLuaTable["BeforeStop"] as ILuaFunction;
                mStopFn = mLuaTable["Stop"] as ILuaFunction;
                mResumeFn = mLuaTable["Resume"] as ILuaFunction;
                if (mStartFn != null)
                {
                    Mgr.SafeCallFunction(mStartFn, mLuaTable.InnerTable);
                }
            }
            finally
            {
                Mgr.PopLuaEvent();
            }
        }

        protected override void OnStop()
        {
            Mgr.PushLuaEvent(this);
            try
            {
                base.OnStop();
                if (mTriggerFns != null)
                {
                    foreach (var entry in mTriggerFns)
                    {
                        entry.Value.Dispose();
                    }

                    mTriggerFns.Clear();
                }

                if (mStopFn != null)
                {
                    Mgr.SafeCallFunction(mStopFn, mLuaTable.InnerTable, IsSuccessed);
                }

                mStartFn?.Dispose();
                mResumeFn?.Dispose();
                mBeforeStopFn?.Dispose();
                mStopFn?.Dispose();
                mLuaTable.Dispose();
            }
            finally
            {
                Mgr.PopLuaEvent();
            }
        }

        private HashMap<int, ILuaFunction> mTriggerFns;

        public void BindTrigger(BaseEvent e, ILuaFunction handler)
        {
            mTriggerFns = mTriggerFns ?? new HashMap<int, ILuaFunction>();
            mTriggerFns.Add(e.ID, handler);
            BindTrigger(e, LuaFunction_OnTrigger);
        }

        private UnionValue LuaFunction_OnTrigger(BaseEvent trigger, UnionValue value)
        {
            var fn = mTriggerFns.Get(trigger.ID);
            if (fn != null)
            {
                //压入null，不允许执行AddChild操作
                Mgr.PushLuaEvent(null);
                try
                {
                    return Mgr.SafeCallFunction(fn, value);
                }
                catch (Exception e)
                {
                    TryFixException(e);
                }
                finally
                {
                    Mgr.PopLuaEvent();
                }
            }

            return UnionValue.Null;
        }

        private void ResumeLuaFunction(bool result, int eventID = 0)
        {
            mWaitID = 0;
            mWaitAll = false;
            mSelectWaitList = null;
            mWaitParallelList = null;
            mWaitAnyList = null;
            Mgr.SafeCallFunction(mResumeFn, mLuaTable.InnerTable, result, eventID);
        }

        protected override bool IsCachable()
        {
            mStartFn = null;
            mResumeFn = null;
            mBeforeStopFn = null;
            mStopFn = null;
            mLuaTable = null;
            mWaitID = 0;
            mSelectWaitList = null;
            mWaitParallelList = null;
            mWaitAnyList = null;
            mWaitAll = false;
            return true;
        }

        private void CheckStopEvent(int eID)
        {
            if (mWaitID == eID)
            {
                var e = Mgr.GetEvent(eID);
                ResumeLuaFunction(e.IsSuccessed, eID);
            }
            else if (mWaitAll && IsChildrenStoped)
            {
                ResumeLuaFunction(true);
            }
            else if (mSelectWaitList != null && mSelectWaitList.Length > 0)
            {
                if (Array.IndexOf(mSelectWaitList, eID) >= 0)
                {
                    var list = mSelectWaitList;
                    var e = Mgr.GetEvent(eID);
                    ResumeLuaFunction(e.IsSuccessed, eID);
                    foreach (var id in list)
                    {
                        Mgr.StopEvent(id, false, "SelectWaitList");
                    }
                }
            }
            else if (mWaitParallelList != null && mWaitParallelList.Length > 0)
            {
                if (Array.IndexOf(mWaitParallelList, eID) >= 0)
                {
                    var failedEventID = 0;
                    var stopCount = 0;
                    foreach (var id in mWaitParallelList)
                    {
                        var e = Mgr.GetEvent(id);
                        if (e == null || e.IsStoped)
                        {
                            stopCount++;
                        }

                        if (e != null && !e.IsSuccessed)
                        {
                            failedEventID = e.ID;
                        }
                    }

                    if (stopCount == mWaitParallelList.Length)
                    {
                        ResumeLuaFunction(failedEventID == 0, failedEventID);
                    }
                }
            }
            else if (mWaitAnyList != null && mWaitAnyList.Length > 0)
            {
                if (Array.IndexOf(mWaitAnyList, eID) >= 0)
                {
                    var e = Mgr.GetEvent(eID);
                    ResumeLuaFunction(e.IsSuccessed, eID);
                }
            }
        }

        protected override void OnChildStop(BaseEvent e)
        {
            base.OnChildStop(e);
            if (IsStoped || IsStopedState(NextState))
            {
                return;
            }

            Mgr.PushLuaEvent(this);
            try
            {
                CheckStopEvent(e.ID);
            }
            finally
            {
                Mgr.PopLuaEvent();
            }
        }

        protected override void OnUpdate(int ms)
        {
            Mgr.PushLuaEvent(this);
            try
            {
                base.OnUpdate(ms);
                if (mWaitID != 0)
                {
                    var e = Mgr.GetEvent(mWaitID);
                    if (e != null)
                    {
                        if (e.IsStoped)
                        {
                            ResumeLuaFunction(e.IsSuccessed, mWaitID);
                        }
                    }
                    else
                    {
                        ResumeLuaFunction(true, mWaitID);
                    }
                }
                else if (mWaitAll && IsChildrenStoped)
                {
                    ResumeLuaFunction(true);
                }
                else if (mSelectWaitList != null && mSelectWaitList.Length > 0)
                {
                    var stopEventId = (from id in mSelectWaitList let e = Mgr.GetEvent(id) where e == null || e.IsStoped select id).FirstOrDefault();
                    if (stopEventId != 0)
                    {
                        foreach (var id in mSelectWaitList)
                        {
                            if (id != stopEventId)
                            {
                                Mgr.StopEvent(id, false, "SelectWaitList");
                            }
                        }

                        var e = Mgr.GetEvent(stopEventId);
                        ResumeLuaFunction(e == null || e.IsSuccessed, stopEventId);
                    }
                }
                else if (mWaitParallelList != null && mWaitParallelList.Length > 0)
                {
                    var failedEventID = 0;
                    var stopCount = 0;
                    foreach (var id in mWaitParallelList)
                    {
                        var e = Mgr.GetEvent(id);

                        if (e == null || e.IsStoped)
                        {
                            stopCount++;
                        }

                        if (e != null && !e.IsSuccessed)
                        {
                            failedEventID = e.ID;
                        }
                    }

                    if (stopCount == mWaitParallelList.Length)
                    {
                        ResumeLuaFunction(failedEventID == 0, failedEventID);
                    }
                }
                else if (mWaitAnyList != null && mWaitAnyList.Length > 0)
                {
                    foreach (var id in mWaitAnyList)
                    {
                        var e = Mgr.GetEvent(id);
                        if (e == null || e.IsStoped)
                        {
                            ResumeLuaFunction(e == null || e.IsSuccessed, id);
                            break;
                        }
                    }
                }
            }
            finally
            {
                Mgr.PopLuaEvent();
            }
        }
    }
}