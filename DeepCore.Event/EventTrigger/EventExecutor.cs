using DeepCore.AI.LLM;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Debug;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Statistics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static DeepCore.Colors;

namespace DeepCore.EventTrigger
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 执行事件触发器的数据接口
    /// </summary>
    public abstract class EventExecutor : Recyclable
    {
        //-------------------------------------------------------------------------
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(EventExecutor));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public static TimeStatisticsRecoder Statistics { get; private set; } = new TimeStatisticsRecoder("EventStatistics");
        public static int EXPECT_TEST_AND_DO_ACTION_TIME_MS { get; set; } = 10;
        public static bool ENABLE_TRACE { get; set; } = false;
        //-------------------------------------------------------------------------
        private readonly Logger log;
        private IEventDataNode mData;
        private EventBehaviorExecutor mBehavior;
        private IEventExecutorCollection mGroup;
        private bool mActive = true;
        private readonly Lazy<LLMAgent> mAiAgent = new Lazy<LLMAgent>(static () => new LLMAgent(LLMEnvironment.Instance.CreateProxy()));
        private readonly List<TimeTaskMS> mTimes = new List<TimeTaskMS>();
        protected readonly HashMap<string, object> mAttributes = new HashMap<string, object>(1);
        protected readonly HashMap<string, object> mLocalVarMap = new HashMap<string, object>(1);
        public IEventRuntime Runtime { get; private set; }
        public ValueTypeNameSpace NameSpace { get; private set; }
        //-------------------------------------------------------------------------
        public EventExecutor()
        {
            Alloc.RecordConstructor(GetType());
            this.log = LoggerFactory.GetLogger(GetType().Name);
        }
        protected virtual EventExecutor Init(ValueTypeNameSpace @namespace, IEventDataNode evt, IEventExecutorCollection group, IEventRuntime runtime)
        {
            this.Runtime = runtime;
            this.NameSpace = @namespace;
            this.mData = evt;
            this.mGroup = group;
            this.mActive = evt != null && evt.EventIsActive;
            var bdata = mData?.GetRuntimeBehavior();
            if (bdata != null)
            {
                this.mBehavior = runtime.ObjectPool.Alloc<EventBehaviorExecutor>().InitExecutor(runtime.ObjectPool, bdata);
            }
            return this;
        }
        ~EventExecutor()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
            OnDisposed?.Invoke(this);
            {
                Main = null;
                OnDisposed = null;
                OnActionBegin = null;
                OnActionEnd = null;
                OnActiveChanged = null;
                Over = null;
            }
            mBehavior?.Dispose(this);
            mBehavior = null;
            if (mData != null)
            {
                mData.EventTriggers.ForEach(this, static (st, i, t) =>
                {
                    if (t != null) t.InvokeDispose(st);
                });
                mData.EventActions.ForEach(this, static (st, i, t) =>
                {
                    if (t != null) t.InvokeDispose(st);
                });
            }
            mTimes.ForEach(this, static (st, i, task) =>
            {
                task.Dispose();
            });
            mTimes.Clear();
            mData = null;
            mGroup = null;
            mActive = false;
            this.mAttributes.Clear();
            this.mLocalVarMap.Clear();
            this.Runtime = null;
            this.NameSpace = null;
        }
        //-------------------------------------------------------------------------
        public IEventDataNode Data { get => mData; }
        public Logger Log { get => log; }
        public string Name { get { return mData == null ? string.Empty : mData.EventName; } }
        public abstract IEventAPI API { get; }
        public LLMAgent AiAgent => mAiAgent.Value;
        public string EditorPath { get { return mData?.EditorPath; } }
        public IEventExecutorCollection Group { get { return mGroup; } }
        public AbstractCollectionPool ObjectPool => Runtime?.ObjectPool;
        public bool IsActive
        {
            get { return mActive; }
            set
            {
                if (mActive != value)
                {
                    mActive = value;
                    if (mActive)
                    {
                        foreach (TimeTaskMS task in mTimes)
                        {
                            task.Resume();
                        }
                        Main?.Invoke(this);
                    }
                    else
                    {
                        foreach (TimeTaskMS task in mTimes)
                        {
                            task.Pause();
                        }
                        Over?.Invoke(this);
                    }
                    OnActiveChanged?.Invoke(this);
                }
            }
        }
        public IEnumerable<string> TracingNodes
        {
            get
            {
                if (mBehavior != null) return mBehavior?.TracingNodes;
                return [string.Empty];
            }
        }
        //-------------------------------------------------------------------------
        #region __Runtime__
        //-------------------------------------------------------------------------
        public abstract void Invoke(Action action);
        public abstract void Invoke<T>(T t, Action<T> action);
        public void SetLocalVar(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                mLocalVarMap.Put(key, value);
            }
        }
        public T GetLocalVarAs<T>(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    //var converter = TypeDescriptor.GetConverter(typeof(T));
                    //T ret = (T)mLocalVarMap[key];// converter.ConvertTo(mLocalVarMap[key], typeof(T));
                    T ret;
                    if (mLocalVarMap.TryGetValue(key, out var retValue))
                    {
                        if (retValue is T t)
                        {
                            ret = t;
                        }
                        else if (retValue != null)
                        {
                            ret = CUtils.ConvertTo<T>(retValue);
                            //ret = (T)retValue;
                        }
                        else
                        {
                            ret = default(T);
                        }
                        return ret;
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
            return default(T);
        }

        public bool IsAttribute(string key)
        {
            return mAttributes.ContainsKey(key);
        }
        public void SetAttribute(string key, object value)
        {
            mAttributes.Put(key, value);
        }
        public object GetAttribute(string key)
        {
            return mAttributes.Get(key);
        }
        public void Start()
        {
            mBehavior?.Start(this);
            foreach (var t in mData.EventTriggers)
            {
                if (t != null)
                {
                    using (var args = API.AllocEventArguments(this, t, null))
                    {
                        t.StartListen(this, args);
                    }
                }
            }
            if (!mActive)
            {
                foreach (TimeTaskMS task in mTimes)
                {
                    task.Pause();
                }
            }
            else
            {
                Main?.Invoke(this);
            }
        }
        public void Stop()
        {
            if (mActive)
            {
                Over?.Invoke(this);
            }
        }
        //         public void RefreshData(IEventDataNode evt)
        //         {
        //             this.mData = evt;
        //             var bdata = mData?.GetRuntimeBehavior();
        //             if (bdata != null)
        //             {
        //                 this.mBehavior?.RefreshData(bdata);
        //             }
        //         }

        /// <summary>
        /// 测试并执行一次触发器
        /// </summary>
        /// <returns></returns>
        public bool TestAndDoAction(IEventArguments in_args)
        {
            if (IsDisposing) return false;
            try
            {
                using (var args = API.AllocEventArguments(this, in_args))
                {
                    if (IsActive)
                    {
                        if (args.Behavior != null)
                        {
                            var starttime = CUtils.TickTimeMS;
                            try
                            {
                                if (OnActionBegin != null)
                                {
                                    OnActionBegin.Invoke(this, args);
                                }
                                {
                                    args.Behavior.InvokeTrigging(this, args, args.Listener);
                                }
                                if (OnActionEnd != null)
                                {
                                    OnActionEnd.Invoke(this, args);
                                }
                            }
                            finally
                            {
                                PrintStopwatch(CUtils.TickTimeMS - starttime, "RunInternal");
                            }
                            return true;
                        }
                        else
                        {
                            mLocalVarMap.Clear();
                            foreach (EventLocalVar klv in mData.EventLocalVars)
                            {
                                object obj = klv.GetLocalVar(this, args);
                                SetLocalVar(klv.Key, obj);
                            }
                            foreach (AbstractCondition c in mData.EventConditions)
                            {
                                if (c != null && !c.DoTest(this, args))
                                {
                                    return false;
                                }
                            }
                            {
                                var starttime = CUtils.TickTimeMS;
                                try
                                {
                                    if (OnActionBegin != null)
                                    {
                                        OnActionBegin.Invoke(this, args);
                                    }
                                    //args.Listener.InvokeTrigging(this, args);
                                    foreach (AbstractAction a in mData.EventActions)
                                    {
                                        if (a != null)
                                        {
                                            a.Invoke(this, args);
                                        }
                                    }
                                    if (OnActionEnd != null)
                                    {
                                        OnActionEnd.Invoke(this, args);
                                    }
                                }
                                finally
                                {
                                    PrintStopwatch(CUtils.TickTimeMS - starttime, "RunEvents");
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error($"EventTrigger:\"{Name}\" Exception : {err.Message}", err);
            }
            return false;
        }
        internal object InvokeActionDone(AbstractAction action, EventExecutor api, IEventArguments args)
        {
            return this.mBehavior?.InvokeActionDone(this, args, action);
        }
        internal object GetReturnValue(AbstractAction action)
        {
            return mBehavior?.GetActionReturn(action);
        }
        /// <summary>
        /// 重置TaskTimer().
        /// </summary>
        public void ResetTimeTask()
        {
            foreach (TimeTaskMS task in mTimes)
            {
                task.Reset();
            }
        }

        private void PrintStopwatch(double elapsed, string function)
        {
            if (TimeStatisticsRecoder.Enable)
            {
                if (elapsed > EXPECT_TEST_AND_DO_ACTION_TIME_MS)
                {
                    string key = string.Format("scene event [{0}] at scene[{1}] -> event[{2}]", function, API?.ToString(), mData?.EventName);
                    Statistics.LogTime(key, (long)elapsed);
                    log.WarnFormat("{0} : stopwatch time {1} > {2}ms", key, elapsed, EXPECT_TEST_AND_DO_ACTION_TIME_MS);
                }
            }
        }
        internal void BeginTrace()
        {
            if (mBehavior != null)
            {
                mBehavior.BeginTrace();
            }
            Runtime.BeginTrace(this);
        }
        internal void Trace(EventExternalizable msg)
        {
            if (mBehavior != null)
            {
                mBehavior.Trace(msg);
            }
            Runtime.EventTrace(Group, this, msg);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------
        #region __Delegates__



        public delegate void EventExecutorHandler(EventExecutor trigger);
        public delegate void OnActionBeginHandler(EventExecutor trigger, IEventArguments args);
        public delegate void OnActionEndHandler(EventExecutor trigger, IEventArguments args);
        public delegate void OnDisposedHandler(EventExecutor trigger);
        public delegate void TimeTaskHandler(IEventArguments args);

        public event EventExecutorHandler Main;
        public event EventExecutorHandler OnActiveChanged;
        public event OnActionBeginHandler OnActionBegin;
        public event OnActionEndHandler OnActionEnd;
        public event EventExecutorHandler Over;
        public event OnDisposedHandler OnDisposed;

        #endregion
        //-----------------------------------------------------------------------------------------------
        #region __Listener__
        public void Listen(Action on, Action off)
        {
            on();
            this.OnDisposed += (e) => { off(); };
        }
        public void Listen<T, D>(T t, D d, Action<T, D> on, Action<T, D> off) where D : Delegate
        {
            on(t, d);
            this.OnDisposed += (e) => { off(t, d); };
        }
        //         public void Listen<D>(ref D t, D d) where D : Delegate
        //         {
        // //            t = (D)Delegate.Combine(t, d);
        // //             this.OnDisposed += (e) =>
        // //             {
        // //                 t = (D)Delegate.Remove(t, d);
        // //             };
        //         }

        //-----------------------------------------------------------------------------------------------
        public void AddTimeTaskSEC(IEventArguments args, float EveryTimeSEC, float DelayTimeSEC, int repeat, TimeTaskHandler handler)
        {
            // // args = args.Clone();
            var task = API.AddTimeTask((int)(EveryTimeSEC * 1000), (int)(DelayTimeSEC * 1000), repeat, (t) =>
            {
                handler(args);
            });
            task.OnExit += (t) => { mTimes.Remove(t); };
            this.mTimes.Add(task);
        }
        public void AddTimeDelaySEC(IEventArguments args, float TimeSEC, TimeTaskHandler handler)
        {
            // args = args.Clone();
            var task = API.AddTimeDelayMS((int)(TimeSEC * 1000), (t) =>
            {
                handler(args);
            });
            task.OnExit += (t) => { mTimes.Remove(t); };
            this.mTimes.Add(task);
        }
        public void AddTimePeriodicSEC(IEventArguments args, float EveryTimeSEC, TimeTaskHandler handler)
        {
            // // args = args.Clone();
            var task = API.AddTimePeriodicMS((int)(EveryTimeSEC * 1000), (t) =>
            {
                handler(args);
            });
            task.OnExit += (t) => { mTimes.Remove(t); };
            this.mTimes.Add(task);
        }
        //-----------------------------------------------------------------------------------------------
        public void listen_TimeTaskSEC(IEventArguments args, float EveryTimeSEC, float DelayTimeSEC, int repeat)
        {
            AddTimeTaskSEC(args, EveryTimeSEC, DelayTimeSEC, repeat, (args) =>
            {
                TestAndDoAction(args);
            });
        }
        public void listen_TimeDelaySEC(IEventArguments args, float TimeSEC)
        {
            AddTimeDelaySEC(args, TimeSEC, (args) =>
            {
                TestAndDoAction(args);
            });
        }
        public void listen_TimePeriodicSEC(IEventArguments args, float EveryTimeSEC)
        {
            AddTimePeriodicSEC(args, EveryTimeSEC, (args) =>
            {
                TestAndDoAction(args);
            });
        }
        //-----------------------------------------------------------------------------------------------
        public void listen_EventActiveChanged(IEventArguments args, EventExecutor adapter)
        {
            // args = args.Clone();
            var handler = new EventExecutorHandler((a) =>
            {
                args.TriggingBoolValue = a.IsActive;
                TestAndDoAction(args);
            });
            Listen(adapter, handler,
                static (adapter, handler) => adapter.OnActiveChanged += handler,
                static (adapter, handler) => adapter.OnActiveChanged -= handler);
            //Listen(adapter.Main, handler);
        }
        public void listen_EventActionMain(IEventArguments args, EventExecutor adapter)
        {
            // args = args.Clone();
            var handler = new EventExecutorHandler((a) =>
            {
                TestAndDoAction(args);
            });
            Listen(adapter, handler,
                static (adapter, handler) => adapter.Main += handler,
                static (adapter, handler) => adapter.Main -= handler);
            //Listen(adapter.Main, handler);
        }
        public void listen_EventActionOver(IEventArguments args, EventExecutor adapter)
        {
            // args = args.Clone();
            var handler = new EventExecutorHandler((a) =>
            {
                TestAndDoAction(args);
            });
            Listen(adapter, handler,
                static (adapter, handler) => adapter.Over += handler,
                static (adapter, handler) => adapter.Over -= handler);
            //Listen(adapter.Main, handler);
        }
        public void listen_EventActionBegin(IEventArguments args, EventExecutor adapter)
        {
            // args = args.Clone();
            OnActionBeginHandler handler = new OnActionBeginHandler((a, g) =>
            {
                TestAndDoAction(args);
            });
            Listen(adapter, handler,
                static (adapter, handler) => adapter.OnActionBegin += handler,
                static (adapter, handler) => adapter.OnActionBegin -= handler);
        }
        public void listen_EventActionEnd(IEventArguments args, EventExecutor adapter)
        {
            // args = args.Clone();
            OnActionEndHandler handler = new OnActionEndHandler((a, g) =>
            {
                TestAndDoAction(args);
            });
            Listen(adapter, handler,
                static (adapter, handler) => adapter.OnActionEnd += handler,
                static (adapter, handler) => adapter.OnActionEnd -= handler);
        }
        #endregion
        //-----------------------------------------------------------------------------------------------
    }



}
