using DeepCore.AI.LLM;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using BattleAction = DeepMetaGame.Data.Message.BattleAction;
using ZoneNotify = DeepMetaGame.Data.Message.ZoneNotify;

namespace DeepCore.Game3D.Host.Instance
{
    public interface InstanceZoneListener
    {
        bool IsLocalBattle { get; }
        void OnCreateZone(InstanceZone zone);
        void OnEventHandler(IReadOnlyList<BattleNotify> e);
        void QueueTask(Action action);
    }
    public class InstanceObjectPool : BattleObjectPool<InstanceZone>
    {
        public InstanceObjectPool(InstanceZone owner) : base(owner) { }
    }
    /// <summary>
    /// 服务端场景
    /// </summary>
    [Reflectible]
    public partial class InstanceZone : InstanceAttributes, IHostZone, IEventRuntime
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(InstanceZone));
        new public static bool EnableAlloc
        {
            get => Alloc.Enable;
            set
            {
                InstanceStatus.EnableAlloc = value;
                InstanceComponent<InstanceZone>.EnableAlloc = value;
                InstanceComponent<InstanceZoneObject>.EnableAlloc = value;
                InstanceUnit.State.EnableAlloc = value;
                InstanceZone.Alloc.Enable = value;
                InstanceZone.HostGUIComponent.EnableAlloc = value;
                InstanceZonePosition.EnableAlloc = value;
            }
        }
        new public static bool VerbosAlloc
        {
            get => Alloc.Verbos;
            set
            {
                InstanceStatus.VerbosAlloc = value;
                InstanceComponent<InstanceZone>.VerbosAlloc = value;
                InstanceComponent<InstanceZoneObject>.VerbosAlloc = value;
                InstanceUnit.State.VerbosAlloc = value;
                InstanceZone.Alloc.Verbos = value;
                InstanceZone.HostGUIComponent.VerbosAlloc = value;
                InstanceZonePosition.VerbosAlloc = value;

            }
        }
        /// <summary>
        /// 分配实例数量
        /// </summary>
        public static int AllocZoneCount { get { return Alloc.AllocCount; } }
        /// <summary>
        /// 未释放实例数量
        /// </summary>
        public static int ActiveZoneCount { get { return Alloc.ActiveCount; } }
        //------------------------------------------------------------------------
        //------------------------------------------------------------------------
        // 基础数据
        private readonly InstanceObjectPool objectPool;

        private EditorTemplates mDataRoot;
        private TemplateManager mTemplates;
        private IQuestAdapter mQuestAdapter;
        private InstanceZoneListener mListener;
        public bool IsLocalBattle => mListener.IsLocalBattle;
        public InstanceZoneListener BattleListener => mListener;
        //------------------------------------------------------------------------
        private Queue<BattleNotify> mSendingEvents = new Queue<BattleNotify>();
        private MessageQueue<DeepMetaGame.Data.Message.BattleAction> mSyncActionQueue;
        private MessageActionQueue<InstanceZone> mTasks;
        private TimeTaskQueue mTimeTasks;
        //------------------------------------------------------------------------
        private InstanceZoneObjectMap mObjects;
        private List<InstanceZoneObject> mObjectsRemoving = new List<InstanceZoneObject>();
        private Lazy<LLMAgent> mAiAgent = new Lazy<LLMAgent>(static () => new LLMAgent(LLMEnvironment.Instance.CreateProxy()));
        //------------------------------------------------------------------------
        sealed public override BattleObjectPool ObjectPool { get => objectPool; }
        SingleThreadCollectionPool IZone.ObjectPool => ObjectPool;
        ITerrainSurface IZone.Terrain3D => this.Terrain3D;
        //------------------------------------------------------------------------
        public ZoneDataFactory DataFactory { get => HostFactory.DataFactory; }
        public ZoneHostFactory HostFactory { get; }
        public EditorTemplates DataRoot { get { return mDataRoot; } }
        public TemplateManager Templates { get { return mTemplates; } }
        public EditorDataCenter DataCenter { get => mTemplates.DataCenter; }
        public C DataCenterAs<C>() where C : EditorDataCenter => DataCenter as C;
        public Config CFG { get; }
        public ICommonConfig ExtCFG { get; }
        public T ExtCFGAs<T>() where T : class, ICommonConfig { return this.ExtCFG as T; }
        public Random RandomN { get { return random; } }
        public LLMAgent AiAgent { get => mAiAgent.Value; }
        public virtual string Prompt { get => string.Empty; }
        //-------------------------------------------------------------------------------------------------------
        public MessageActionQueue<InstanceZone> TaskQueue { get { return mTasks; } }
        public IQuestAdapter QuestAdapter { get { return mQuestAdapter; } }
        public float UpdateIntervalMS { get { return mLastInterval; } }
        //------------------------------------------------------------------------
        private readonly int mMaxUnitCount;
        private uint mTick = 0;
        private float mLastInterval = 0;
        private double mCurPassTimeMS = 0;
        private double mQueryPassTimeMS = 0;
        private double mQueryPassTimeSEC = 0;

        readonly private Random random;
        readonly private Logger log;

        public string UUID { get; set; } = string.Empty;
        //------------------------------------------------------------------------

        readonly private SceneData m_SceneData;
        readonly private ZoneSpaceDivision mSpaceDiv;
        readonly private IPostChannel mZoneChannel;
        private float mMinStep = 0;
        private float mMinStepSqr = 0;
        public InstanceZoneFormula Formula { get; }
        public SceneData SceneData { get { return m_SceneData; } }
        public ZoneSpaceDivision SpaceDiv { get { return mSpaceDiv; } }
        public IPostChannel ZoneChannel { get => mZoneChannel; }
        public float SpaceDivSizeW { get; private set; }
        [Desc("最大单位数量")]
        public int MaxUnitCount { get { return mMaxUnitCount; } }
        [Desc("场景中是否存在Area")]
        public bool HasArea { get { return mHasArea; } }

        public float Gravity { get; private set; }
        public float ElasticAngle { get; private set; }
        public float ElasticAngle2 { get; private set; }
        [Desc("单位当前帧位移的最小距离")]
        public float MinStep
        {
            get => mMinStep;
            private set
            {
                mMinStep = value;
                mMinStepSqr = value * value;
            }
        }
        [Desc("单位当前帧位移的最小距离")]
        public float MinStepSquare
        {
            get => mMinStepSqr;
        }
        public string ZoneName { get; }

        //------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="data">场景数据</param>
        /// <param name="spaceDivSize">空间分割参数</param>
        /// <param name="maxUnitCount">最大单位数</param>
        /// <param name="randomSeed">随机种子</param>
        internal InstanceZone(InstanceZoneListener listener, ZoneHostFactory hostFactory, EditorTemplates dataroot, SceneData data, int randomSeed)
        {
            this.HostFactory = hostFactory;
            this.objectPool = new InstanceObjectPool(this);
            this.EnvironmentVarMap = new EnvironmentVarMap<InstanceZone>(this);
            this.EnvironmentVarMap.OnEnvironmentVarChangeHandler += EnvironmentVarMap_OnEnvironmentVarChangeHandler1; ;
            this.mListener = listener;
            this.ZoneName = data.ToString();
            Alloc.RecordConstructor(GetType().ToVisibleName() + ":" + ZoneName);
            this.log = LoggerFactory.GetLogger(string.Format("Instance:({0})", data));
            this.random = new Random(randomSeed);// new Random(randomSeed);
            this.mDataRoot = dataroot;
            this.mTemplates = dataroot.Templates;
            this.CFG = dataroot.Templates.DefaultConfig;
            this.ExtCFG = dataroot.Templates.DefaultExtConfig;
            if (data.OverrideConfig != null)
            {
                this.CFG = data.OverrideConfig;
            }
            if (data.OverrideExtConfig != null)
            {
                this.ExtCFG = data.OverrideExtConfig;
            }
            this.ElasticAngle = CMath.AngleToRadian(CFG.OBJECT_MOVE_BLOCK_ELASTIC_ANGLE);
            this.ElasticAngle2 = ElasticAngle * 2f;
            this.Gravity = CFG.GLOBAL_GRAVITY;
            this.MinStep = MoveHelper.GetDistance(1000 / CFG.SYSTEM_FPS, CFG.OBJECT_MOVE_TO_MIN_STEP_SEC);
            if (data.SpaceDivW <= 1)
            {
                throw new Exception("SpaceDivSize must large than map 1 !");
            }
            this.mTasks = new MessageActionQueue<InstanceZone>();
            this.mTasks.OnError += this.cb_Error;
            this.mSyncActionQueue = new MessageQueue<BattleAction>(MainProcessAction);
            this.mTimeTasks = new TimeTaskQueue(this.objectPool);
            this.mMaxUnitCount = data.MaxUnit;
            this.m_SceneData = data;
            //this.m_TerrainSrc = data.Terrain.ZoneData as ZoneInfo;
            this.SpaceDivSizeW = data.SpaceDivW;
            this.mObjects = new InstanceZoneObjectMap();
            this.InitTerrain(data);
            this.mSpaceDiv = HostFactory.CreateSpaceDivision(this);
            this.mSpaceDiv.Init();
            this.mZoneChannel = HostFactory.CreateChannel(this);
            this.mQuestAdapter = CreateQuestAdapter();
            this.Formula = CreateFormula();
            this.OnInitFormula(Formula);
            this.InitGUI();
            listener.OnCreateZone(this);
        }


        protected virtual void OnInitFormula(InstanceZoneFormula Formula)
        {
            this.Formula?.OnInit();
        }
        // -----------------------------------------------------------------------------------
        ~InstanceZone()
        {
            Alloc.RecordDestructor(GetType().ToVisibleName() + ":" + ZoneName);
        }
        protected override void Disposing()
        {
            Alloc.RecordDispose(GetType().ToVisibleName() + ":" + ZoneName);
            base.Disposing();
            this.DateTimeAlarm.Dispose();
            this.PassTimeAlarm.Dispose();
            this.mTimeTasks.Dispose();
            this.mTasks.Dispose();
            this.ClearEvents();
            _components?.Dispose();
            foreach (var obj in mObjects.Objects)
            {
                obj.Dispose();
            }
            this.mObjects.Dispose();
            this.mObjects = null;
            foreach (var flg in mFlags.Values)
            {
                flg.Dispose();
            }
            this.mFlags.Clear();
            this.sync_pos_list.Clear();
            this.mQuestAdapter.Dispose();
            this.mQuestAdapter = null;
            this.mTemplates = null;
            this.mSyncActionQueue = null;
            this.mSendingEvents = null;
            this.EnvironmentVarMap.Clear();
            this.mFlags.Clear();
            this.Formula.Dispose();
            this.DisposeTerrain();
            this.mSpaceDiv.Dispose();
            this.objectPool.Dispose();
        }
        // -----------------------------------------------------------------------------------
        public Logger Log { get { return log; } }
        public float GridCell { get { return Terrain3D.GridCellSize; } }

        public uint Tick { get { return mTick; } }

        /// <summary>
        /// 是否发 SyncPosEvent 包
        /// </summary>
        public bool SyncPos
        {
            get { return sync_pos_list.Enable; }
            set { sync_pos_list.Enable = value; }
        }

        public DateTime StartDayTime { get; set; } = DateTime.Now.ToDayTime();

        public double PassTimeMS { get { return mQueryPassTimeMS; } }
        public double PassTimeSEC { get { return mQueryPassTimeSEC; } }

        public TimeSpan PassTime { get => TimeSpan.FromMilliseconds(mQueryPassTimeMS); }
        public DateTime DateTime { get => StartDayTime + PassTime; }

        public TimeSpanAlarm PassTimeAlarm { get; } = new TimeSpanAlarm();
        public DateTimeAlarm DateTimeAlarm { get; } = new DateTimeAlarm();
        //         public void Trace(string text)
        //         {
        //             log.Info(text);
        //         }

        //-----------------------------------------------------------------------------------

        public virtual DeepCore.IO.ISerializable GetLayerInitData() { return null; }
        // -----------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------
        protected internal virtual void cb_Error(Exception obj)
        {
            log.Error(obj.Message, obj);
        }
        //-----------------------------------------------------------------------------------
    

        //-----------------------------------------------------------------------------------
        private bool firstUpdate = false;
        protected virtual void FirstUpdate() { }
        protected virtual void BeginUpdate(float intervalMS) { }
        protected virtual void EndUpdate(float intervalMS) { }
        public void Update(float intervalMS)
        {
            try
            {
                if (firstUpdate == false)
                {
                    firstUpdate = true;
                    FirstUpdate();
                    OnFirstUpdate?.Invoke(this);
                }
                BeginUpdate(intervalMS);
                if (intervalMS > 0)
                {
                    this.mLastInterval = intervalMS;
                    BeginEventsRecord();
                    try
                    {
                        this.MinStep = MoveHelper.GetDistance(intervalMS, CFG.OBJECT_MOVE_TO_MIN_STEP_SEC);
                        if (mTick == 0)
                        {
                            mCurPassTimeMS = 0;
                            mQueryPassTimeMS = 0;
                            mQueryPassTimeSEC = 0;
                            foreach (InstanceFlag f in mFlags.Values)
                            {
                                f.OnStart();
                            }
                            if (event_OnInit != null)
                                event_OnInit.Invoke(this);
                        }
                        mTasks.ProcessMessages(this);
                        mSyncActionQueue.ProcessMessages();
                        UpdateComponents(intervalMS);
                        UpdateObjects();
                        if (event_OnUpdate != null)
                        {
                            event_OnUpdate.Invoke(this);
                        }
                        this.mTimeTasks.Update(intervalMS);
                        this.ProcessEvents();
                        this.mZoneChannel?.Flush(this);
                        this.mSpaceDiv.Flush(this);
                    }
                    finally
                    {
                        mSpaceDiv.ClearPosDirty();
                        mCurPassTimeMS += intervalMS;
                        mQueryPassTimeMS = mCurPassTimeMS;
                        mQueryPassTimeSEC = (float)(mCurPassTimeMS / 1000f);
                        mTick++;
                    }
                    this.DateTimeAlarm.Update(this.DateTime);
                    this.PassTimeAlarm.Update(this.PassTime);
                }
                else
                {
                    this.mLastInterval = 0;
                    this.mTasks.ProcessMessages(this);
                    this.mSyncActionQueue.ProcessMessages();
                    this.ProcessEvents();
                }
                EndUpdate(intervalMS);
            }
            finally
            {
                objectPool.UpdateRecycle();
            }
        }
        private void UpdateObjects()
        {
            mObjects.Refresh();
            {
                var objetes = mObjects.Objects;
                var cnt = objetes.Count;
                for (int i = 0; i < cnt; i++)
                {
                    var obj = objetes[i];
                    try
                    {
                        obj.onUpdate(this);
                    }
                    catch (Exception error)
                    {
                        log.Error(error);
                        OnObjectError?.Invoke(error, obj);
                    }
                }
                InstanceZoneObject u = null;
                for (int i = 0; i < cnt; i++)
                {
                    u = objetes[i];
                    if (u.Enable)
                    {
                        if (u.updatePos())
                        {
                            if (event_OnObjectPosChanged != null)
                            {
                                event_OnObjectPosChanged.Invoke(this, u);
                            }
                            sync_pos_list.Add(u);
                        }
                    }
                }
            }
            foreach (var f in mFlags.Values)
            {
                f.update();
            }
            if (mObjectsRemoving.Count > 0)
            {
                for (int i = 0; i < mObjectsRemoving.Count; i++)
                {
                    mObjectsRemoving[i].Dispose();
                }
                mObjectsRemoving.Clear();
            }
        }
        //-------------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------------------------//
        #region Environment

        private Action<string, object> event_OnEnvironmentVarChangeHandler;
        public event Action<string, object> OnEnvironmentVarChangeHandler
        {
            add { event_OnEnvironmentVarChangeHandler += value; }
            remove { event_OnEnvironmentVarChangeHandler -= value; }
        }
        public EnvironmentVarMap<InstanceZone> EnvironmentVarMap { get; }

        private void EnvironmentVarMap_OnEnvironmentVarChangeHandler1(InstanceZone st, string key, EnvironmentVar var, object value, bool syncToClient)
        {
            if (EnvironmentVar.ALWAYS_SYNC_ENVIRONMENT_VAR || var.SyncToClient || syncToClient)
            {
                PostEvent(ObjectPool.Alloc<SyncEnvironmentVarEvent>().Init(new ClientStruct.ZoneEnvironmentVar()
                {
                    Key = key,
                    Value = HostFactory.EncodeZoneVar(value),
                    SyncToClient = syncToClient
                }));
            }
            event_OnEnvironmentVarChangeHandler?.Invoke(key, value);
        }
        public void SetEnvironmentVar(string key, object value, bool syncToClient = true)
        {
            EnvironmentVarMap.SetEnvironmentVar(key, value, syncToClient);
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            return EnvironmentVarMap.GetEnvironmentVarAs<T>(key);
        }
        public bool TryGetEnvironmentVarAs<T>(string key, out T value)
        {
            return EnvironmentVarMap.TryGetEnvironmentVarAs<T>(key, out value);
        }
        public bool TryGetEnvironmentVar(string key, out object value)
        {
            return EnvironmentVarMap.TryGetEnvironmentVar(key, out value);
        }

        public int ListEnvironmentVars(List<EnvironmentVar> list)
        {
            return EnvironmentVarMap.ListEnvironmentVars(list);
        }

        public void GetCurrentZoneVars(IList<ClientStruct.ZoneEnvironmentVar> ret)
        {
            int i = 0;
            foreach (var var in EnvironmentVarMap.Values)
            {
                var o = new ClientStruct.ZoneEnvironmentVar();
                {
                    o.Key = var.Key;
                    o.SyncToClient = var.SyncToClient;
                    if (var.SyncToClient)
                    {
                        o.Value = var.Value;
                    }
                }
                ret.Add(o);
                i++;
            }
        }

        protected void BindZoneVar(ZoneVar var, BindValuesExecutor api)
        {
            if (string.IsNullOrEmpty(var.Key))
            {
                // Error
                return;
            }
            if (var.Value is IEventValue)
            {
                var evalue = (var.Value as IEventValue);
                SetEnvironmentVar(var.Key, evalue.GetEnvValue(api), var.SyncToClient);
            }
            else if (var.Value is Array)
            {
                Array array = var.Value as Array;
                for (int i = 0; i < array.Length; i++)
                {
                    object e = array.GetValue(i);
                    if (e is IEventValue)
                    {
                        string key = string.Format(var.Key + "[{0}]", i);
                        IEventValue evalue = (e as IEventValue);
                        SetEnvironmentVar(key, evalue.GetEnvValue(api), var.SyncToClient);
                    }
                }
            }
        }



        #endregion
        //-------------------------------------------------------------------------------------------------------//
        public LLMAgent CreateAiAgent() => new LLMAgent(LLMEnvironment.Instance.CreateProxy());
        //-------------------------------------------------------------------------------------------------------//

    }



}
