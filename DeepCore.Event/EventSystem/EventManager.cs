using DeepCore;
using DeepCore.Event.EventSystem.Events;
using DeepCore.Event.EventSystem.Message;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DeepCore.Event.EventSystem
{
    public class EventManager : Disposable
    {
        private static readonly TypeAllocRecorder Alloc = new TypeAllocRecorder(nameof(EventManager));
        public Logger Logger { get; private set; }
        public readonly Random Random = new Random();
        public string Name { get; }
        public string UUID { get; }

        public const char AddressSeparatorChar = '#';

        public int EventCount
        {
            get
            {
                lock (mEvents)
                {
                    return mEvents.Count;
                }
            }
        }

        public int CurrentRootEvent { get; internal set; }

        public bool IsRunning => !mPause;
        private int mStartedCount;

        public bool IsFirstStarting => mStartedCount == 0;

        public string Address => GetAddress(Name, UUID);

        public enum RemoteActionType
        {
            Doing,
            Success,
            Fail,
        }

        public RemoteActionType RemoteAction { get; set; }

        public static string GetAddress(string name, string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return name;
            }

            return name + AddressSeparatorChar.ToString() + uuid;
        }

        private readonly object mUpdateLocker = new object();

        private int mUpdateThreadID;
        public bool IsInUpdate => mUpdateThreadID == Thread.CurrentThread.ManagedThreadId;

        public void EnterUpdate()
        {
            mUpdateThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        private void ExitUpdate()
        {
            mUpdateThreadID = int.MinValue;
        }

        protected internal IDisposable LockUpdating()
        {
            return LockUpdating(false);
        }

        private IDisposable LockUpdating(bool setUpdateThread)
        {
            if (setUpdateThread)
            {
                var ret = new NormalLocker(mUpdateLocker, ExitUpdate);
                EnterUpdate();
                return ret;
            }

            return new NormalLocker(mUpdateLocker);
        }

        public static readonly MessageBroker MessageBroker = new MessageBroker();

        private readonly HashMap<int, BaseEvent> mEvents = new HashMap<int, BaseEvent>();
        private readonly HashMap<int, BaseEvent> mTempSaved = new HashMap<int, BaseEvent>();

        private readonly SafeDictionary<string, RemoteServerEvent> mRemoteStartMessage = new SafeDictionary<string, RemoteServerEvent>();
        private readonly SafeList<BaseEvent> mRootEvents = new SafeList<BaseEvent>();
        private readonly SafeDictionary<string, object> mCacheObjs = new SafeDictionary<string, object>();


        public string[] RootEventsDesc => mRootEvents.ToArray().Select(e => e.Desc).ToArray();
        public long LastUpdateTickMS { get; private set; }
        private TimeTaskQueue mTimeTasks = new TimeTaskQueue(CollectionPool.Shared);

        public event Action<NamedEventMessage> OnNamedMessage;


        private bool mPause = true;

        public object GetObject(string key)
        {
            return mCacheObjs.Get(key);
        }

        public T GetObject<T>(string key)
        {
            var o = mCacheObjs.Get(key);
            if (o == null)
            {
                return default;
            }

            return (T)o;
        }

        public void PutObject(string key, object obj)
        {
            if (obj == null)
            {
                mCacheObjs.Remove(key);
            }
            else
            {
                mCacheObjs[key] = obj;
            }
        }

        public EventManager(string name, string uid)
        {
            Name = name;
            UUID = uid;
            Alloc.RecordConstructor(GetType().ToVisibleName() + ":" + name);
            RemoteAction = RemoteActionType.Doing;
            MessageBroker.CreateChannel(Address);
            MessageBroker.Subscribe(Address, OnReceiveMessage);
            Logger = LoggerFactory.GetLogger(Address);
        }
#if DEBUG
        ~EventManager()
        {
            Alloc.RecordDestructor(GetType().ToVisibleName() + ":" + Name);
        }
#endif
        protected override void Disposing()
        {
            Alloc.RecordDispose(GetType().ToVisibleName() + ":" + Name);
            MessageBroker.CloseChannel(Address);
            EventManagerFactory.Instance.RemoveEventManager(Address);
            using (LockUpdating())
            {
                InnerStop("Dispose");
            }

            //Log("Dispose");
            lock (mEvents)
            {
                mEvents.Clear();
                mTempSaved.Clear();
            }

            mRemoteStartMessage.Dispose();
            mRootEvents.Dispose();
            mCacheObjs.Dispose();

            mTimeTasks.Dispose();
            mTimeTasks = null;
            OnNamedMessage = null;
        }


        private void OnReceiveMessage(IMessagePayload messagePayload)
        {
            if (messagePayload.WhatObject is EventMessage msg)
            {
                OnReceiveMessage(msg);
            }
            else
            {
                throw new ArgumentException("only support EventMessage");
            }
        }

        private bool IsEventRegistered(BaseEvent e)
        {
            lock (mEvents)
            {
                return mEvents.ContainsKey(e.ID);
            }
        }

        internal static void Init()
        {
            Decorator.Collect();
        }

        protected virtual Type[] UnionValueKeepTypes => null;

        protected internal void OnFixArgument(BaseEvent e)
        {
            var f = Decorator.Get(e.GetType());
            if (f == null || f.Arg.Count == 0 || e.Arg.IsNull)
            {
                return;
            }

            //UnionValue字段转换为[EventArgument]
            foreach (var fieldInfo in f.Arg)
            {
                UnionValue key;
                if (f.ArgIndex)
                {
                    key = fieldInfo.Value.Index;
                }
                else
                {
                    key = fieldInfo.Key.Name;
                }

                var subV = e.Arg[key];
                if (!subV.IsNull)
                {
                    var obj = UnionValueSerializer.Deserialize(subV, fieldInfo.Key.FieldType, UnionValueKeepTypes);
                    if (obj != null)
                    {
                        fieldInfo.Key.SetValue(e, obj);
                    }
                }
            }
        }

        protected internal void OnFixOutput(BaseEvent e)
        {
            var f = Decorator.Get(e.GetType());
            if (f == null || f.Output.Count == 0)
            {
                return;
            }

            //[EventOutput]的字段转换为UnionValue
            var v = f.OutputIndex ? UnionValue.NewArray : UnionValue.NewMap;
            foreach (var fieldInfo in f.Output)
            {
                var obj = fieldInfo.Key.GetValue(e);
                UnionValue key;
                if (f.OutputIndex)
                {
                    key = fieldInfo.Value.Index;
                }
                else
                {
                    key = fieldInfo.Key.Name;
                }

                v[key] = UnionValueSerializer.Serialize(obj, UnionValueKeepTypes);
            }

            e.Output = v;
        }

        public override string ToString()
        {
            return $"[{Name}-{UUID}]";
        }

        protected virtual string FormatLog(string msg)
        {
            return $"{DateTime.Now:hh:mm:ss}:{DateTime.Now.Millisecond}{this}{msg}";
        }

        public void Log(string msg)
        {
            Logger.Debug(FormatLog(msg));
        }

        public void LogWarn(string msg)
        {
            Logger.Warn(FormatLog(msg));
        }

        public void LogError(string msg)
        {
            Logger.Error(FormatLog(msg));
        }

        public void LogStackTrace(string msg)
        {
            var trace = new StackTrace();
            Logger.Warn(FormatLog(msg + "\n" + trace));
        }

        public virtual void LogException(Exception e)
        {
            if (e is FixedException)
            {
                e = ((FixedException)e).E;
            }

            if (e == null)
            {
                return;
            }

            LogError(e.Message + e.StackTrace);
            e = e.InnerException;
            while (e != null)
            {
                LogError("InnerException : " + e.Message + e.StackTrace);
                e = e.InnerException;
            }
        }

        private void StopAllEvents(bool force, string resultReason)
        {
            // Stop过程中可能有新增
            for (var i = 0; i < mRootEvents.Count; i++)
            {
                mRootEvents[i].Stop(false, resultReason);
            }

            if (force)
            {
                UpdateRootEvents();
            }
        }


        private void InnerStop(string reason)
        {
            OnBeforeStop();
            StopAllEvents(true, reason);
            OnStop();
            mPause = true;
        }

        public void Stop(string reason)
        {
            using (LockUpdating())
            {
                InnerStop(reason);
            }
        }

        private bool mStarting;

        public void Start(string reason = null)
        {
            if (IsDisposed)
            {
                LogError("IsDisposed");
                return;
            }

            mPause = true;
            try
            {
                mStarting = true;
                LastUpdateTickMS = CUtils.TickTimeMS;
                OnStart(reason);
                mPause = false;
                mStartedCount += 1;
            }
            catch (Exception e)
            {
                TryFixException(e);
                StopAllEvents(true, "start exception");
                OnStop();
            }

            mStarting = false;
            Update();
        }

        public void Pause()
        {
            mPause = true;
            Log("Pause");
            OnPause();
        }


        private bool mNeedRestart;

        public void ReStart()
        {
            mNeedRestart = true;
        }

        private void UpdateRootEvents()
        {
            var ms = (int)(CUtils.TickTimeMS - LastUpdateTickMS);
            var count = mRootEvents.Count - 1;
            for (var i = count; i >= 0; i--)
            {
                var root = mRootEvents[i];
                if (root.State == EventState.NotStart || IsEventRegistered(root))
                {
                    root.InternalUpdate(ms);
                }
                else
                {
                    mRootEvents.RemoveAt(i);
                }
            }
        }

        private const long LimitStopLifeMS = 5000;

        public void Update()
        {
            using (LockUpdating(true))
            {
                if (IsDisposed || mPause)
                {
                    return;
                }

                try
                {
                    // try restart
                    if (mNeedRestart)
                    {
                        mNeedRestart = false;
                        Log("Restart");
                        InnerStop("restart");
                        Start("restart");
                        return;
                    }

                    //update root events
                    var curTickMS = CUtils.TickTimeMS;
                    UpdateRootEvents();
                    mTimeTasks.Update((int)(curTickMS - LastUpdateTickMS));
                    LastUpdateTickMS = curTickMS;
                    // custom OnUpdate
                    OnUpdate();

                    //remove tempNodes
                    lock (mEvents)
                    {
                        using (var list = CollectionObjectPool<int>.AllocList())
                        {
                            foreach (var entry in mTempSaved)
                            {
                                if (entry.Value.StopedPassTimeMS > LimitStopLifeMS)
                                {
                                    list.Add(entry.Key);
                                }
                            }

                            foreach (var key in list)
                            {
                                mTempSaved.Remove(key);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TryFixException(e);
                }
            }
        }

        protected class FixedException : Exception
        {
            public readonly Exception E;

            public FixedException(Exception e)
            {
                E = e;
            }
        }

        protected void TryFixException(Exception e)
        {
            if (mStarting && !(e is FixedException))
            {
                throw new FixedException(e);
            }

            LogException(e);
        }

        public TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler)
        {
            return mTimeTasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }

        public TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler)
        {
            return mTimeTasks.AddTimeDelayMS(delayMS, handler);
        }

        public TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler)
        {
            return mTimeTasks.AddTimePeriodicMS(intervalMS, handler);
        }

        public BaseEvent StartEvent(BaseEvent e)
        {
            return StartEvent(e, false);
        }

        public void QueueAction(Action act)
        {
            StartEvent(new ActionEvent(act));
        }

        protected BaseEvent StartEvent(BaseEvent e, bool force)
        {
            try
            {
                RegisterEvent(e);
                e.Mgr = this;
                mRootEvents.Add(e);

                if (IsInUpdate)
                {
                    using (LockUpdating())
                    {
                        e.InternalUpdate(0);
                    }
                }
                else if (force)
                {
                    using (LockUpdating())
                    {
                        if (e.State == EventState.NotStart)
                        {
                            e.InternalUpdate(0);
                        }
                    }
                }

                return e;
            }
            catch (Exception ex)
            {
                TryFixException(ex);
                return null;
            }
        }


        public virtual BaseEvent CreateEvent(string eType)
        {
            var t = Decorator.GetType(eType);
            if (t == null)
            {
                throw new Exception($"not find {eType} ");
            }

            return CreateEvent(t);
        }

        public virtual BaseEvent CreateEvent(Type t)
        {
            var e = ReflectionUtil.CreateInstance(t) as BaseEvent;
            if (e == null)
            {
                throw new Exception($"not support type {t}");
            }

            return e;
        }

        /// <summary>
        /// 远程事件时，创建远端实际的逻辑事件
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected internal virtual BaseEvent CreateServerEntityEvent(string eType, UnionValue arg)
        {
            var e = CreateEvent(eType);
            e.Arg = arg;
            return e;
        }


        public BaseEvent GetEvent(int id)
        {
            lock (mEvents)
            {
                return mEvents.Get(id) ?? mTempSaved.Get(id);
            }
        }


        internal void RegisterEvent(BaseEvent e)
        {
            e.Mgr = this;
            lock (mEvents)
            {
                mEvents[e.ID] = e;
            }
        }

        internal void UnRegisterEvent(BaseEvent e)
        {
            lock (mEvents)
            {
                mEvents.Remove(e.ID);
                mTempSaved.Add(e.ID, e);
#if DEBUG
                BaseEvent.AllocRecorder.RecordDispose(e.GetType().ToVisibleName());
#endif
            }

            if (e is RemoteServerEvent remoteServerEvent)
            {
                mRemoteStartMessage.Remove(remoteServerEvent.Message.MessageID);
            }
        }


        #region virtual

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected virtual void OnBeforeStop()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnStart(string reason)
        {
        }

        protected virtual void OnEventCacheable(BaseEvent e)
        {
        }

        protected internal virtual void OnEventStop(BaseEvent e)
        {
        }

        protected internal virtual void OnEventStart(BaseEvent e)
        {
        }


        public virtual void OnReceiveMessage(EventMessage msg)
        {
            if (msg is StartEventMessage)
            {
                var e = new RemoteServerEvent((StartEventMessage)msg);
                mRemoteStartMessage.Add(e.Message.MessageID, e);
                StartEvent(e);
            }
            else if (msg is ExceptionStopEventMessage)
            {
                var eMsg = (ExceptionStopEventMessage)msg;
                var e = mRemoteStartMessage.Get(eMsg.MessageID);
                e?.Stop(false, eMsg.ResultReason);
            }
            else if (msg is TargetEventMessage)
            {
                var syncMsg = (TargetEventMessage)msg;
                var e = GetEvent(syncMsg.ToEvent);
                if (e != null)
                {
                    e.InternalReceiveMessage(syncMsg);
                }
                else
                {
                    LogWarn("not found message owner " + syncMsg.ToEvent);
                }
            }
            else if (msg is NamedEventMessage)
            {
                OnNamedMessage?.Invoke((NamedEventMessage)msg);
            }
        }

        #endregion
    }
}