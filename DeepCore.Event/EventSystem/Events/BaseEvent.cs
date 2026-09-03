//#define TIMER_DATETIME
using DeepCore.Event.EventSystem.Message;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DeepCore.Event.EventSystem.Events
{
    [Flags]
    public enum EventState
    {
        None,
        NotStart = 1,
        Running = 1 << 1,
        Successed = 1 << 2,
        Failed = 1 << 3,
    }

    public delegate UnionValue TriggerHandler(BaseEvent trigger, UnionValue v);

    /// <summary>
    /// 所有事件的基类
    /// </summary>
    public class BaseEvent
    {
        private static int _genID;

        /// <summary>
        /// ID永远不为0
        /// </summary>
        /// <returns></returns>
        internal static int GenID()
        {
            return Interlocked.Increment(ref _genID);
        }

#if DEBUG
        internal static readonly TypeAllocRecorder AllocRecorder = new TypeAllocRecorder(nameof(BaseEvent));
        private static readonly HashMap<int, WeakReference> sTotalEvents = new HashMap<int, WeakReference>();
        public static ICollection<WeakReference> STotalEvents
        {
            get
            {
                GC.Collect();
                lock (sTotalEvents)
                {
                    var listKeys = new List<int>();
                    foreach (var entry in sTotalEvents)
                    {
                        if (!entry.Value.IsAlive)
                        {
                            listKeys.Add(entry.Key);
                        }
                    }

                    foreach (var key in listKeys)
                    {
                        sTotalEvents.Remove(key);
                    }

                    return sTotalEvents.Values;
                }
            }
        }
        ~BaseEvent()
        {
            AllocRecorder.RecordDestructor(GetType().ToVisibleName());
            lock (sTotalEvents)
            {
                sTotalEvents.Remove(ID);
            }
            //Console.WriteLine("~" + GetType().FullName + ID);
        }
#endif


        protected BaseEvent()
        {
            ID = GenID();
            if (ID == 0)
            {
                ID = GenID();
            }
#if DEBUG
            AllocRecorder.RecordConstructor(GetType().ToVisibleName());
            lock (sTotalEvents)
            {
                sTotalEvents.Add(ID, new WeakReference(this));
            }
#endif
        }

        public event Action<BaseEvent> OnEventStop;
        public event Action<BaseEvent> OnEventStart;

        public event TriggerHandler OnTrigger;

        //public event Action<EventMessage> OnEventReciveMessage;
        public string Desc { get; protected set; }
        public UnionValue Arg;
        public UnionValue Output;
        public UnionValue UserTag;

        public EventManager Mgr { get; internal set; }
        public int ID { get; private set; }
        public bool IsStoped => IsStopedState(State);
        public bool IsBeforeStop => IsStopedState(NextState);
        public bool IsSuccessed => State == EventState.Successed;
        public bool IsRunning => State == EventState.Running;

        public long FrameIndex { get; private set; }
#if TIMER_DATETIME
        private DateTime mStartDateTime;
        private DateTime mStopDateTime;
        public long RunningTimeMS => State >= EventState.Running ? Convert.ToInt64((DateTime.Now - mStartDateTime).TotalMilliseconds) : 0;
        public long StopedPassTimeMS => IsStoped ? Convert.ToInt64((DateTime.Now - mStopDateTime).TotalMilliseconds) : -1;
#else
        private long mStartTickMS;
        private long mStopTickMS;
        public long RunningTimeMS => State >= EventState.Running ? CUtils.TickTimeMS - mStartTickMS : 0;
        public long StopedPassTimeMS => IsStoped ? CUtils.TickTimeMS - mStopTickMS : -1;
#endif

        public BaseEvent Parent => InternalParent;

        public EventState State { get; private set; } = EventState.NotStart;
        public EventState NextState { get; private set; } = EventState.None;
        public string ResultReason { get; private set; }


        public static bool IsStopedState(EventState s)
        {
            return s == EventState.Successed || s == EventState.Failed;
        }

        public BaseEvent RootEvent
        {
            get
            {
                var p = this;
                while (p.InternalParent != null)
                {
                    p = p.InternalParent;
                }

                return p;
            }
        }

        private EventState mNextFrameState = EventState.None;

        private HashMap<int, TriggerHandler> mTriggers;
        private bool mOnlyContinue;
        private readonly UnionValueArray mNextTriggers = new UnionValueArray();
        private readonly List<BaseEvent> _children = new List<BaseEvent>();
        private event Action QueueActions;


        internal BaseEvent InternalParent;

        public int ChildCount
        {
            get
            {
                lock (_children)
                {
                    return _children.Count;
                }
            }
        }

        public bool IsNextInvokeTrigger
        {
            get
            {
                lock (mNextTriggers)
                {
                    return mNextTriggers.Count > 0;
                }
            }
        }

        public bool IsChildrenStoped => GetChildCount(EventState.Running | EventState.NotStart) == 0;
        public bool IsChildrenSuccess => SuccessChildCount == ChildCount;
        public int SuccessChildCount => GetChildCount(EventState.Successed);

        private readonly LinkedList<int> mDependEvents = new LinkedList<int>();
        private readonly LinkedList<int> mLinkedEvents = new LinkedList<int>();

        private bool CheckHasDepend()
        {
            lock (mDependEvents)
            {
                while (mDependEvents.Count > 0)
                {
                    var e = Mgr.GetEvent(mDependEvents.First.Value);
                    if (e == null || e.IsStoped)
                    {
                        mDependEvents.RemoveFirst();
                    }
                    else
                    {
                        break;
                    }
                }

                return mDependEvents.Count == 0;
            }
        }

        private void TryStartChild(BaseEvent child)
        {
            if (State != EventState.Running || child.State != EventState.NotStart)
            {
                return;
            }

            try
            {
                if (!child.CheckHasDepend() || !OnTryStartChild(child))
                {
                    return;
                }

                child.SetState(EventState.Running);
            }
            catch (Exception e)
            {
                TryFixException(e);
            }
        }

        private void UpdateChildren(int ms)
        {
            lock (_children)
            {
                //不能使用foreach,因为事件执行过程中,可能有事件新增
                var removeChildIndex = -1;
                for (var i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];
                    if (child.Mgr == null)
                    {
                        Mgr.RegisterEvent(child);
                    }

                    TryStartChild(child);
                    child.InternalUpdate(ms);
                    if (removeChildIndex < 0 && child.IsStoped)
                    {
                        removeChildIndex = i;
                    }
                }

                if (removeChildIndex >= 0)
                {
                    _children[removeChildIndex].UnRegisterMgr();
                    _children.RemoveAt(removeChildIndex);
                }
            }
        }


        private void StopChildren(EventState s)
        {
            lock (_children)
            {
                //不能使用foreach,因为事件执行过程中,可能有事件新增
                for (var i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];
                    if (child.State == EventState.Running)
                    {
                        child.Stop(s == EventState.Successed, mException != null ? $"event({ID}) exception" : ResultReason);
                    }
                }
            }
        }

        private void ClearEvents()
        {
            if (mTriggers != null)
            {
                foreach (var entry in mTriggers)
                {
                    var e = Mgr.GetEvent(entry.Key);
                    if (e != null)
                    {
                        e.OnTrigger -= entry.Value;
                    }
                }

                mTriggers.Clear();
            }

            QueueActions = null;
            OnTrigger = null;
            OnEventStart = null;
            OnEventStop = null;
            //OnEventReciveMessage = null;
        }


        /// <summary>
        /// 保证SetState在EventManager的Update的流程中
        /// 事件结束流程 Stop -> SetState -> StopChildren (可引发二次Stop，此时使用mNextState进行限制）-> 
        /// OnBeforeStop（可能引发子事件新增）-> 事件正式结束，此时无法新增子事件 -> 第二次执行StopChildren-> OnStop
        /// </summary>
        /// <param name="value"></param>
        private void SetState(EventState value)
        {
            if (State == value || IsStoped || IsStopedState(NextState))
            {
                return;
            }

            try
            {
                NextState = value;
                if (IsStopedState(value))
                {
                    StopChildren(value);
                    try
                    {
                        OnBeforeStop();
                    }
                    catch (Exception e)
                    {
                        TryFixException(e);
                    }
                }

                State = value;
                lock (mLinkedEvents)
                {
                    foreach (var eId in mLinkedEvents)
                    {
                        var e = Mgr.GetEvent(eId);
                        if (e != null)
                        {
                            e.ResultReason = ResultReason;
                            e.SetState(State);
                        }
                    }
                }

                if (Desc != null && mException == null)
                {
                    Mgr.Log(IsStoped ? $"{this} {value} {ResultReason}. Time:{RunningTimeMS}ms" : $"{this} {value} ");
                }

                if (State == EventState.Running)
                {
#if TIMER_DATETIME
                    mStartDateTime = DateTime.Now;
#else
                    mStartTickMS = CUtils.TickTimeMS;
#endif
                    Mgr.OnFixArgument(this);
                    OnStart();
                    OnEventStart?.Invoke(this);
                    Mgr.OnEventStart(this);
                }
                else if (IsStoped)
                {
                    Mgr.OnFixOutput(this);
                    if (State == EventState.Failed)
                    {
                        Output = ResultReason;
                    }

                    //stop beforestop 添加的子事件
                    StopChildren(State);
                    //Mgr.Log($"running ms datetime: {(DateTime.Now - mStartDateTime).TotalMilliseconds} tick:{RunningTimeMS}");
                    OnStop();
#if TIMER_DATETIME
                    mStopDateTime = DateTime.Now;
#else
                    mStopTickMS = CUtils.TickTimeMS;
#endif

                    OnEventStop?.Invoke(this);
                    if (IsRunning)
                    {
                        Parent?.OnChildStop(this);
                    }

                    Mgr.OnEventStop(this);
                    ClearEvents();
                }
            }
            catch (Exception e)
            {
                TryFixException(e);
            }
            finally
            {
                NextState = EventState.None;
            }
        }


        private void UnRegisterMgr()
        {
            ForeachChild<BaseEvent>(e => { e.UnRegisterMgr(); });
            Mgr.UnRegisterEvent(this);
            lock (_children)
            {
                _children.Clear();
            }

            InternalParent = null;
            //Mgr = null;
        }

        internal bool PushToObjectPool()
        {
            if (IsCachable())
            {
                //重新生成ID
                Mgr = null;
                ID = GenID();
                return true;
            }

            return false;
        }

        private UnionValue InvokeTrigger(UnionValue v)
        {
            Output = v;
            try
            {
                var ret = OnTrigger?.Invoke(this, v) ?? UnionValue.Null;
                OnTriggered(v);
                return ret;
            }
            catch (Exception e)
            {
                TryFixException(e);
                return UnionValue.Null;
            }
        }

        private void InternalTrigger()
        {
            lock (mNextTriggers)
            {
                for (var i = 0; i < mNextTriggers.Count; i++)
                {
                    InvokeTrigger(mNextTriggers[i]);
                }
                mNextTriggers.Clear();
            }
        }


        /// <summary>
        /// 提供给外部调用的Update，必须保证处于EventManager的update中
        /// </summary>
        /// <param name="ms"></param>
        protected internal virtual void InternalUpdate(int ms)
        {
            try
            {
                if (Parent == null)
                {
                    Mgr.CurrentRootEvent = ID;
                }
                FrameIndex++;
                QueueActions?.Invoke();
                QueueActions = null;
                if (Parent == null && State == EventState.NotStart)
                {
                    SetState(EventState.Running);
                }

                if (mNextFrameState != EventState.None)
                {
                    if (IsStopedState(mNextFrameState))
                    {
                        InternalTrigger();
                    }

                    SetState(mNextFrameState);
                    mNextFrameState = EventState.None;
                }

                UpdateChildren(ms);

                if (State == EventState.Running)
                {
                    if (FrameIndex == 1)
                    {
                        OnFirstUpdate(ms);
                    }

                    InternalTrigger();
                    OnUpdate(ms);
                }

                //root event
                if (Parent == null && IsStoped)
                {
                    UnRegisterMgr();
                }
            }
            catch (Exception e)
            {
                TryFixException(e);
            }
            finally
            {
                if (Parent == null)
                {
                    Mgr.CurrentRootEvent = 0;
                }
            }
        }

        private Exception mException;

        protected void TryFixException(Exception e)
        {
            mException = e;
            Mgr.LogException(e);
            Stop(false, e.Message);
        }

        protected virtual bool OnTryStartChild(BaseEvent e)
        {
            return true;
        }

        protected virtual void OnUpdate(int ms)
        {
        }

        protected virtual void OnFirstUpdate(int ms)
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnBeforeStop()
        {
        }

        protected virtual void OnChildStop(BaseEvent e)
        {
        }

        protected virtual void OnReceiveMessage(EventMessage msg)
        {
        }


        protected virtual void OnStop()
        {
        }

        protected virtual void OnTriggered(UnionValue eventValue)
        {
        }

        /// <summary>
        /// 返回true表示支持缓存
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsCachable()
        {
            return false;
        }


        internal void InternalReceiveMessage(EventMessage msg)
        {
            OnReceiveMessage(msg);
        }


        public override string ToString()
        {
            return $"{Desc}({ID})";
        }

        public BaseEvent ContinueWith(BaseEvent e)
        {
            try
            {
                if (mOnlyContinue)
                {
                    var last = GetLastChild();
                    e.AddDependEvent(last.ID);
                    last.RemoveResultLink(ID);
                    e.AddResultLink(ID);
                    AddChild(e);
                    return this;
                }

                //本事件执行完毕才执行e
                e.AddDependEvent(ID);

                //生成的事件和e保持状态同步
                var ret = new BaseEvent
                {
                    mOnlyContinue = true,
                };
                e.AddResultLink(ret.ID);
                if (Parent == null)
                {
                    ret.AddChild(this);
                }
                else if (IsRunning)
                {
                    Mgr.StartEvent(ret);
                }

                if (e.Parent == null)
                {
                    ret.AddChild(e);
                }

                return ret;
            }
            catch (Exception ex)
            {
                TryFixException(ex);
                return null;
            }
        }

        public BaseEvent ContinueWith(Action<BaseEvent> act)
        {
            return ContinueWith<BaseEvent>(act);
        }

        public BaseEvent ContinueWith(Action act)
        {
            return ContinueWith<BaseEvent>(e => act.Invoke());
        }

        public BaseEvent ContinueWith<T>(Action<T> act) where T : BaseEvent
        {
            var e = new BaseEvent();
            var handler = new Action<BaseEvent>(ee =>
            {
                act.Invoke(this as T);
                ee.Output = Output;
                ee.Stop(IsSuccessed, ResultReason);
            });
            e.OnEventStart += handler;
            return ContinueWith(e);
        }

        public static BaseEvent CreateActionEvent(Action<BaseEvent> act)
        {
            var e = new BaseEvent();
            var handler = new Action<BaseEvent>(ee =>
            {
                act.Invoke(ee);
                ee.Stop(true);
            });
            e.OnEventStart += handler;
            return e;
        }

        public void QueueAction(Action act)
        {
            QueueActions += act;
        }

        public void AddDependEvent(int eId)
        {
            lock (mDependEvents)
            {
                mDependEvents.AddLast(eId);
            }
        }

        public void RemoveDependEvent(int eId)
        {
            lock (mDependEvents)
            {
                mDependEvents.Remove(eId);
            }
        }

        public void AddResultLink(int eId)
        {
            lock (mLinkedEvents)
            {
                mLinkedEvents.AddLast(eId);
            }
        }

        public void RemoveResultLink(int eId)
        {
            lock (mLinkedEvents)
            {
                mLinkedEvents.Remove(eId);
            }
        }

        public void TriggerNextFrame(params UnionValue[] args)
        {
            UnionValue v;
            if (args.Length > 0)
            {
                v = args.Length == 1 ? args[0] : UnionValueSerializer.Serialize(args);
            }
            else
            {
                v = UnionValue.Null;
            }

            lock (mNextTriggers)
            {
                mNextTriggers.Add(v);
            }
        }

        public void Trigger(params UnionValue[] args)
        {
            UnionValue v;
            if (args.Length > 0)
            {
                v = args.Length == 1 ? args[0] : UnionValueSerializer.Serialize(args);
            }
            else
            {
                v = UnionValue.Null;
            }

            if (Mgr != null && Mgr.IsInUpdate)
            {
                using (Mgr.LockUpdating())
                {
                    InvokeTrigger(v);
                }
            }
            else
            {
                lock (mNextTriggers)
                {
                    mNextTriggers.Add(v);
                }
            }
        }

        public UnionValue TriggerNow(params UnionValue[] args)
        {
            using (Mgr.LockUpdating())
            {
                UnionValue v;
                if (args.Length > 0)
                {
                    v = args.Length == 1 ? args[0] : UnionValueSerializer.Serialize(args);
                }
                else
                {
                    v = UnionValue.Null;
                }

                return InvokeTrigger(v);
            }
        }

        public void BindTrigger(BaseEvent trigger, TriggerHandler handler)
        {
            trigger.OnTrigger += handler;
            if (mTriggers == null)
            {
                mTriggers = new HashMap<int, TriggerHandler>() { { trigger.ID, handler } };
            }
            else
            {
                mTriggers.Add(trigger.ID, handler);
            }
        }

        public void RemoveBindTrigger(BaseEvent trigger)
        {
            var handler = mTriggers.RemoveByKey(trigger.ID);
            if (handler != null)
            {
                trigger.OnTrigger -= handler;
            }
        }

        public int GetChildCount(EventState s)
        {
            var count = 0;
            lock (_children)
            {
                foreach (var child in _children)
                {
                    if ((s & child.State) > 0)
                    {
                        count += 1;
                    }
                }
            }

            return count;
        }


        public void ForeachChild<T>(Action<T> act) where T : BaseEvent
        {
            lock (_children)
            {
                _children.ForEach(e => { act(e as T); });
            }
        }

        public BaseEvent GetLastChild()
        {
            lock (_children)
            {
                return _children.Count > 0 ? _children[_children.Count - 1] : null;
            }
        }

        public void AddChild(ICollection<BaseEvent> events)
        {
            if (IsStoped)
            {
                return;
            }

            foreach (var e in events)
            {
                AddChild(e);
            }
        }


        public void Stop(bool success, string resultReason = null, bool forceNextFrame = false)
        {
            if (IsStoped || IsStopedState(NextState))
            {
                return;
            }

            var s = success ? EventState.Successed : EventState.Failed;
            ResultReason = resultReason;
            if (Mgr != null && Mgr.IsInUpdate && !forceNextFrame)
            {
                using (Mgr.LockUpdating())
                {
                    SetState(s);
                }
            }
            else
            {
                mNextFrameState = s;
            }
        }

        public void StopNow(bool success, string resultReason = null)
        {
            var s = success ? EventState.Successed : EventState.Failed;
            ResultReason = resultReason;
            using (Mgr.LockUpdating())
            {
                SetState(s);
            }
        }

        public void AddChild(BaseEvent e)
        {
            if (e == null || IsStoped)
            {
                return;
            }

            lock (_children)
            {
                _children.Add(e);
            }

            e.InternalParent = this;

            if (e.Mgr == null)
            {
                Mgr?.RegisterEvent(e);
            }

            if (Mgr != null && Mgr.IsInUpdate)
            {
                using (Mgr.LockUpdating())
                {
                    TryStartChild(e);
                }
            }
        }
    }
}