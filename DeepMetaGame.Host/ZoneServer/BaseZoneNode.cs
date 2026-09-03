using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepMetaGame.Data.Message;
using DeepCore.Threading;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System.Threading;
using DeepCore.Game3D.Host.ZoneRuntime;

namespace DeepCore.Game3D.Host.ZoneServer
{
    public class BaseZoneNode : InstanceZoneListener
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(BaseZoneNode));
        protected readonly object locker = new object();

        public bool IsLocalBattle => false;
        public ZoneHostFactory HostFactory { get; }

        private readonly IZoneNodeServer mServer;
        private readonly EditorTemplates mDataRoot;
        private readonly TemplateManager mTemplates;
        private Config mConfig;

        private SceneData mSceneData;
        private HashMap<string, SceneObjectData> mSceneObjMap;
        private EditorScene mZone;

        private float mFixedUpdateInterval;
        private double mLastUpdateTime;
        private double mLastUsedTime;

        private bool mStarted = false;
        private bool mShutDown = false;
        private bool mIsDisposed = false;
        private bool mIsRunning = false;
        //------------------------------------------------------------------------------------------------------------
        // 当前帧发送的消息队列 //
        private Queue<BattleNotify> mPostEvents = new Queue<BattleNotify>();
        // 内部主线程命令 //
        private MessageActionQueue<BaseZoneNode> mTasks;

        private bool EnableLog = false;
        private const float LogFrequencyMS = 1000 * 60 * 60;//1小时
        private TimeInterval _LogTimer = null;
        //------------------------------------------------------------------------------------------------------------

        public BaseZoneNode(IZoneNodeServer server, ZoneHostFactory hostFactory, EditorTemplates data_root)
        {
            Alloc.RecordConstructor(this.GetType());
            this.HostFactory = hostFactory;
            this.log = LoggerFactory.GetLogger("ZoneNode");
            this.mTasks = new MessageActionQueue<BaseZoneNode>();
            this.mTasks.OnError += this.OnError;
            this.mServer = server;
            this.mDataRoot = data_root;
            this.mTemplates = data_root.Templates;
            this.mConfig = mTemplates.DefaultConfig;
            if (EnableLog) this._LogTimer = new TimeInterval(LogFrequencyMS);
        }
        ~BaseZoneNode()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        public Logger log { get; }
        public IZoneNodeServer Server { get { return mServer; } }
        public string Name { get { return (mSceneData != null) ? mSceneData.ToString() : "null"; } }
        public SceneData SceneData { get { return mSceneData; } }
        public EditorTemplates DataRoot { get { return mDataRoot; } }
        public TemplateManager Templates { get { return mTemplates; } }
        public int SceneID { get { return mSceneData.ID; } }
        public bool IsStarted { get { return mStarted; } }
        public bool IsRunning { get { return mIsRunning; } }
        public bool IsShutDown { get => mShutDown; }
        public bool IsDisposed { get { lock (this) { return mIsDisposed; } } }
        public Config GameConfig { get => mConfig; }
        public float ServerUpdateIntervalMS
        {
            set
            {
                if (value != mFixedUpdateInterval)
                {
                    mFixedUpdateInterval = value;
                    OnTimerChanged(value);
                }
            }
            get => mFixedUpdateInterval;
        }
        public float ClientUpdateIntervalMS
        {
            get => 1000 / mConfig.SYSTEM_FPS;
        }
        public double LastElapsedTime => mLastUsedTime;
        public double LastUpdateTime => mLastUpdateTime;
        public float FixedUpdateInterval => mFixedUpdateInterval;
        public double LastUsedTime => mLastUsedTime;
        protected EditorScene Zone { get { return mZone; } }
        protected IEnumerable<BattleNotify> PostEvents { get { return mPostEvents; } }
        public MessageActionQueue<BaseZoneNode> TaskQueue { get { return mTasks; } }

        //------------------------------------------------------------------------------------------------------------

        public override string ToString()
        {
            if (mZone != null)
            {
                return string.Format("{0}({1}):{2}", mZone.Data.Name, mZone.Data.ID, mZone.UUID);
            }
            return "Unable to load scene";
        }


        /// <summary>
        /// 获得指定名称的路点坐标
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PointData GetScenePointData(string name)
        {
            foreach (PointData p in mSceneData.Points)
            {
                if (p.Name == name)
                {
                    return p;
                }
            }
            return null;
        }
        //------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 开始房间初始化
        /// </summary>
        public virtual void Start(SceneData data, System.Action<InstanceZone, Exception> started)
        {
            try
            {
                //this.log.SetLevelDisable(LoggerLevel.WARNNING);
                lock (locker)
                {
                    if (data.OverrideConfig != null)
                    {
                        mConfig = data.OverrideConfig;
                    }
                    this.mFixedUpdateInterval = ClientUpdateIntervalMS;
                    if (mStarted)
                    {
                        started(null, null);
                    }
                    this.mStarted = true;
                    // 解析游戏服创建房间信息 //
                    this.mSceneData = data;
                    this.mSceneObjMap = new HashMap<string, SceneObjectData>();
                    this.mSceneData.Points.Find((d) =>
                    {
                        mSceneObjMap.Add(d.Name, d);
                        return false;
                    });
                    this.mSceneData.Regions.Find((d) =>
                    {
                        mSceneObjMap.Add(d.Name, d);
                        return false;
                    });
                    this.mSceneData.Decorations.Find((d) =>
                    {
                        mSceneObjMap.Add(d.Name, d);
                        return false;
                    });
                    // 创建游戏主循环Timer //
                    this.mLastUpdateTime = CUtils.TickTimeMS;
                    this.mIsRunning = true;
                    this.mTasks.Insert(0, () =>
                    {
                        try
                        {  // 构造战斗场景 //
                            this.mZone = HostFactory.CreateZone(this, this.DataRoot, mSceneData);
                            this.OnZoneCreated(mZone);
                            this.OnStarted();
                            if (event_OnZoneStart != null)
                            {
                                event_OnZoneStart.Invoke(mZone);
                            }
                            started(this.mZone, null);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            this.mIsRunning = false;
                            started(null, err);
                        }
                    });
                    mServer.StartTimer(this);
                }
            }
            catch (Exception e)
            {
                started(null, e);
            }
        }
        /// <summary>
        /// 异步房间初始化
        /// </summary>
        public System.Threading.Tasks.Task<InstanceZone> StartAsync(SceneData data)
        {
            var tcs = new TaskCompletionSource<InstanceZone>();
            this.Start(data, (z, e) =>
            {
                if (e != null)
                    tcs.TrySetException(e);
                else
                    tcs.TrySetResult(z);
            });
            return tcs.Task;
        }

        public virtual void Stop()
        {
            lock (locker)
            {
                mShutDown = true;
            }
        }
        public void Stop(Action<InstanceZone> cb)
        {
            this.OnZoneStop += (z) =>
            {
                cb(z);
            };
            Stop();
        }
        public System.Threading.Tasks.Task<InstanceZone> StopAsync()
        {
            var tcs = new TaskCompletionSource<InstanceZone>();
            this.OnZoneStop += (z) =>
            {
                tcs.TrySetResult(z);
            };
            this.Stop();
            return tcs.Task;
        }
        public SceneObjectData FindSceneObjData(string name)
        {
            SceneObjectData ret;
            mSceneObjMap.TryGetValue(name, out ret);
            return ret;
        }
        //------------------------------------------------------------------------------------------------------------
        void InstanceZoneListener.OnCreateZone(InstanceZone zone)
        {
            OnCrateZone?.Invoke(this, zone);
        }
        protected virtual void OnZoneCreated(EditorScene zone)
        {
            // 非全屏同步，每个Client负责维护自己需要的队列 //
            this.mZone.SyncPos = false;
            // 半同步，场景不能大于255 //
        }
        protected virtual void OnStarted() { }
        protected virtual void OnStopped() { }
        protected virtual void OnZoneUpdate() { }
        protected virtual void OnBeginUpdate() { }
        protected virtual void OnEndUpdate() { }
        protected virtual void OnFinallUpdate() { }
        protected virtual void OnError(Exception err)
        {
            event_OnZoneError?.Invoke(Zone, err);
            log.Error(err);
        }
        protected virtual void OnDisposing() { }
        protected virtual void OnDisposed() { }


        [Desc("返回True表示发给游戏服，False表示发给客户端")]
        protected virtual bool FilterSendingZoneMessage(IZoneNodeServer server, IMessage e) { return false; }


        //------------------------------------------------------------------------------------------------------------
        #region Timer

        protected virtual void OnTimerChanged(float intervalMS)
        {
        }
        protected virtual void OnTimerExit()
        {
        }
        // #else
        //         private System.Threading.Timer mTimer;
        // 
        //         protected virtual void timer_change(int intervalMS)
        //         {
        //             this.mUpdateInterval = intervalMS;
        //             this.mTimer.Change(this.mUpdateInterval, this.mUpdateInterval);
        //         }
        //         protected virtual void timer_start(int intervalMS)
        //         {
        //             this.mUpdateInterval = intervalMS;
        //             if (IsAutoUpdate)
        //             {
        //                 this.mTimer = new System.Threading.Timer(timer_update, this, intervalMS, intervalMS);
        //             }
        //         }
        //         protected virtual void timer_exit()
        //         {
        //             if (this.mTimer != null)
        //             {
        //                 this.mTimer.Dispose();
        //                 this.mTimer = null;
        //             }
        //         }
        //         protected virtual void timer_update(object obj)
        //         {
        //             this.Update();
        //         }
        // #endif
        #endregion
        //------------------------------------------------------------------------------------------------------------
        public bool Update(double currentTime)
        {
            try
            {
                lock (locker)
                {
                    if (!mIsRunning) return false;
                    var curTime = currentTime;
                    if (mLastUpdateTime == 0)
                    {
                        mLastUpdateTime = curTime;
                    }
                    var intervalMS = (float)(curTime - mLastUpdateTime);
                    var intervalLimit = (mFixedUpdateInterval * 2);
                    this.mLastUpdateTime = curTime;
                    try
                    {
                        intervalMS = Math.Min(intervalMS, intervalLimit);
                        this.UpdateZone(intervalMS);
                        if (EnableLog)
                        {
                            if (mIsRunning && mZone != null && _LogTimer != null && _LogTimer.Update(intervalMS))//定时更新场景内信息
                            {
                                log.InfoFormat(" log info zone[{0}] : " +
                                                "\n\t units  count = {1}, " +
                                                "\n\t spells count = {2}," +
                                                "\n\t items  count = {3}, " +
                                                "\n\t player count = {4}",
                                              this,
                                              mZone.AllUnitsCount,
                                              mZone.AllSpellsCount,
                                              mZone.AllItemsCount,
                                              mZone.AllPlayersCount);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                        OnError(err);
                    }
                    this.mLastUsedTime = DeepCore.CUtils.TickTimeMS - curTime;
                    if (log.IsWarnEnabled)
                    {
                        if (mZone != null && mIsRunning && mLastUsedTime > intervalLimit)
                        {
                            const string info = "update time overload at zone[{0}] : stopwatch time {1} > update interval limit {2}" +
                                "\n\t units  count = {3}" +
                                "\n\t spells count = {4}" +
                                "\n\t items  count = {5}" +
                                "\n\t player count = {6}";
                            log.WarnFormat(info,
                                this,
                                mLastUsedTime,
                                intervalLimit,
                                mZone.AllUnitsCount,
                                mZone.AllSpellsCount,
                                mZone.AllItemsCount,
                                mZone.AllPlayersCount);
                        }
                    }
                    return mIsRunning;
                }
            }
            catch (Exception err)
            {
                this.OnError(err);
            }
            return false;
        }

        /// <summary>
        /// 战斗场景主逻辑更新//
        /// </summary>
        /// <param name="intervalMS"></param>
        protected virtual void UpdateZone(float intervalMS)
        {
            if (mIsRunning)
            {
                try
                {
                    foreach (var send in mPostEvents) { send.Release(); }
                    mPostEvents.Clear();
                    OnBeginUpdate();
                    if (event_OnBeginUpdate != null)
                    {
                        event_OnBeginUpdate.Invoke(mZone);
                    }
                    mTasks.ProcessMessages(this);
                    if (intervalMS > 0)
                    {
                        try
                        {
                            mZone.Update(intervalMS);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                            OnError(err);
                        }
                        finally
                        {
                            this.OnZoneUpdate();
                        }
                    }
                    if (event_OnZonePostEvents != null)
                    {
                        event_OnZonePostEvents.Invoke(mZone, mPostEvents);
                    }
                    OnEndUpdate();
                    if (event_OnEndUpdate != null)
                    {
                        event_OnEndUpdate.Invoke(mZone);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    OnError(err);
                }
                finally
                {
                    OnFinallUpdate();
                }
                if (mShutDown)
                {
                    this.mIsRunning = false;
                    try
                    {
                        this.mTasks.Dispose();
                        OnTimerExit();
                        OnStopped();
                        if (event_OnZoneStop != null)
                        {
                            event_OnZoneStop.Invoke(mZone);
                        }
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                        OnError(err);
                    }
                    try
                    {
                        this.OnDisposing();
                        if (mZone != null)
                        {
                            this.mZone.Dispose();
                            if (event_OnZoneDisposed != null)
                            {
                                event_OnZoneDisposed.Invoke(mZone);
                            }
                        }
                        this.OnDisposed();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                        OnError(err);
                    }
                    finally
                    {
                        this.OnDisposeEvents();
                        this.mIsDisposed = true;
                        Alloc.RecordDispose(this.GetType());
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------

        void InstanceZoneListener.OnEventHandler(IReadOnlyList<BattleNotify> events)
        {
            foreach (var e in events)
            {
                if (FilterSendingZoneMessage(Server, e))
                {
                    Server.PostToGameServer(e);
                }
                else
                {
                    e.Retain();
                    mPostEvents.Enqueue(e);
                }
            }
        }
        void InstanceZoneListener.QueueTask(Action task)
        {
            QueueTask(task);
        }

        /// <summary>
        /// 保证在Task内部执行的代码线程安全
        /// </summary>
        /// <param name="task"></param>
        public bool QueueTask(Action task)
        {
            return mTasks.Enqueue(task);
        }
        public bool QueueTask<ST>(ST st, Action<BaseZoneNode, ST> task)
        {
            return mTasks.Enqueue(st, task);
        }
        public bool QueueZoneTask<ST>(ST st, Action<InstanceZone, ST> task)
        {
            var tuple = new ValueTuple<ST, InstanceZone, Action<InstanceZone, ST>>(st, Zone, task);
            return mTasks.Enqueue(tuple, static (node, tuple) =>
            {
                tuple.Item3.Invoke(node.Zone, tuple.Item1);
            });
        }


        //---------------------------------------------------------------------------------------------------

        protected virtual void OnDisposeEvents()
        {
            this.event_OnZoneStart = null;
            this.event_OnZoneStop = null;
            this.event_OnZonePostEvents = null;
            this.event_OnZoneDisposed = null;
            this.event_OnBeginUpdate = null;
            this.event_OnEndUpdate = null;
        }

        private Action<InstanceZone> event_OnZoneStart;
        private Action<InstanceZone> event_OnZoneStop;
        private Action<InstanceZone, Exception> event_OnZoneError;
        private Action<InstanceZone, IEnumerable<BattleNotify>> event_OnZonePostEvents;
        private Action<InstanceZone> event_OnZoneDisposed;
        private Action<InstanceZone> event_OnBeginUpdate;
        private Action<InstanceZone> event_OnEndUpdate;

        public event Action<BaseZoneNode, InstanceZone> OnCrateZone;
        public event Action<InstanceZone> OnZoneStart { add { event_OnZoneStart += value; } remove { event_OnZoneStart -= value; } }
        public event Action<InstanceZone> OnZoneStop { add { event_OnZoneStop += value; } remove { event_OnZoneStop -= value; } }
        public event Action<InstanceZone, Exception> OnZoneError { add { event_OnZoneError += value; } remove { event_OnZoneError -= value; } }
        public event Action<InstanceZone, IEnumerable<BattleNotify>> OnZonePostEvents { add { event_OnZonePostEvents += value; } remove { event_OnZonePostEvents -= value; } }
        public event Action<InstanceZone> OnZoneDisposed { add { event_OnZoneDisposed += value; } remove { event_OnZoneDisposed -= value; } }
        public event Action<InstanceZone> OnZoneBeginUpdate { add { event_OnBeginUpdate += value; } remove { event_OnBeginUpdate -= value; } }
        public event Action<InstanceZone> OnZoneEndUpdate { add { event_OnEndUpdate += value; } remove { event_OnEndUpdate -= value; } }
        //---------------------------------------------------------------------------------------------------

    }


}
