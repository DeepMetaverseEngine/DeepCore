using DeepCore.Components;
using DeepCore.Concurrent;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Slave.Data;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Data;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Helper;
using System.Threading.Tasks;


namespace DeepCore.Game3D.Slave.Layer
{

    public interface ILayerZoneListener
    {
        EditorTemplates DataRoot { get; }
        TemplateManager Templates { get; }
        LayerZone Layer { get; }
        bool Pause { get; set; }
        float TimeScale { get; set; }
        bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata);
        void QueueTask(Action task);
        void QueueTask(Func<Task> task);
        void SendAction(BattleAction action);
        void ReleaseMessage(IMessage message);
    }
    public class LayerObjectPool : BattleObjectPool<LayerZone>
    {
        public LayerObjectPool(LayerZone owner) : base(owner) { }
    }
    // ---------------------------------------------------------------------------------------------

    public partial class LayerZone : Disposable, IZone, IEnvironmentObject, IEnvironmentDecoder
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(LayerZone));
        new public static bool EnableAlloc
        {
            get => Alloc.Enable;
            set
            {
                LayerStatus.EnableAlloc = value;
                LayerComponent<LayerZone>.EnableAlloc = value;
                LayerComponent<LayerZoneObject>.VerbosAlloc = value;
                LayerZone.Alloc.Enable = value;
                LayerObject.EnableAlloc = value;
            }
        }
        new public static bool VerbosAlloc
        {
            get => Alloc.Verbos;
            set
            {
                LayerStatus.VerbosAlloc = value;
                LayerComponent<LayerZone>.VerbosAlloc = value;
                LayerComponent<LayerZoneObject>.VerbosAlloc = value;
                LayerZone.Alloc.Verbos = value;
                LayerObject.VerbosAlloc = value;
            }
        }
        ITerrainSurface IZone.Terrain3D => Terrain3D;
        SingleThreadCollectionPool IZone.ObjectPool => objectPool;
        public static int ActiveZoneLayerCount { get { return Alloc.ActiveCount; } }
        public static int AllocZoneLayerCount { get { return Alloc.AllocCount; } }

        public string ZoneUUID { get; private set; } = string.Empty;

        /// <summary>
        /// 超出此同步范围，立即将坐标修正
        /// </summary>
        public float AsyncUnitPosModifyMaxRange { get; private set; }
        public float AsyncUnitPosModifyMinRange { get; private set; }
        /// <summary>
        /// 是否主角做异步位置同步（即客户端先模拟假象）
        /// </summary>
        public SyncMode ActorSyncMode { get; set; }
        /// <summary>
        /// 服务器时间，从游戏开始到现在多少毫秒
        /// </summary>
        public double LastServerTimeMS { get { return mRemotePassTimeMS; } }
        public double LocalTimeMS { get { return mLocalPassTimeMS; } }
        /// <summary>
        /// 当前客户端更新Interval
        /// </summary>
        public float CurrentIntervalMS { get; private set; }
        /// <summary>
        /// 当前客户端Ping值
        /// </summary>
        public int CurrentPing { get; private set; } = 999;
        public int NetPing { get; private set; } = 999;
        public MessageActionQueue<LayerZone> TaskQueue { get { return mTasks; } }
        /// <summary>
        /// 计算对应的服务端时间
        /// </summary>
        /// <returns></returns>
        public double CurrentServerTimeMS
        {
            get
            {
                var now = mLocalPassTimeMS;
                return mRemotePassTimeMS + (now - mLastRemotePassClientTimeMS);
            }
        }
        /// <summary>
        /// 服务端资源版本号
        /// </summary>
        public string ServerResourceVersion { get; private set; }
        /// <summary>
        /// 本地资源版本号
        /// </summary>
        public string ClientResourceVersion { get { return Templates.ResourceVersion; } }

        public Random RandomN { get { return mRandom; } }
        public BattleObjectPool ObjectPool { get => objectPool; }
        public Logger Log => log;
        private readonly Logger log;
        private readonly LayerObjectPool objectPool;
        private double mLocalPassTimeMS = 0;
        private double mRemotePassTimeMS = 0;
        private double mLastRemotePassClientTimeMS = 0;
        private float mElasticAngle;
        private float mElasticAngle2;
        private float mMinStep;
        private float mMinStepSqr;
        private SceneData mData;
        private readonly Random mRandom;

        private MessageQueue<IBattleMessage> mSyncMessageQueue;
        private MessageActionQueue<LayerZone> mTasks;
        private TimeTaskQueue mTimeTasks;

        private HashMap<int, object> mListenRequests = new HashMap<int, object>();
        private TimeInterval<int> mListenRequestTimeout = new TimeInterval<int>(2000);
        private ServerStatusB2C mLastServerStatus;

        public LayerEnvironmentMap EnvironmentVarMap { get; }
        public CameraOffset CameraOffset;
        public ILayerZoneListener LayerClient { get; private set; }
        public object Sender { get; private set; }
        public TemplateManager Templates { get; private set; }
        public EditorTemplates DataRoot { get; private set; }
        public EditorDataCenter DataCenter { get => Templates.DataCenter; }
        public ZoneDataFactory DataFactory { get => SlaveFactory.DataFactory; }
        public ZoneSlaveFactory SlaveFactory { get; }
        public C DataCenterAs<C>() where C : EditorDataCenter => DataCenter as C;
        public Config CFG { get; private set; }
        public ICommonConfig ExtCFG { get; private set; }
        public T ExtCFGAs<T>() where T : class, ICommonConfig => ExtCFG as T;
        public bool IsLoaded { get; private set; }
        public ServerStatusB2C ServerStatus { get { return mLastServerStatus; } }
        public float ElasticAngle { get { return mElasticAngle; } }
        public float ElasticAngle2 { get { return mElasticAngle2; } }
        public float Gravity { get; private set; }
        public float StepHeight { get; private set; }
        public ISerializable LayerInitData { get; private set; }
        public float MinStepSquare { get => mMinStepSqr; }
        public float MinStep
        {
            get => mMinStep;
            private set
            {
                mMinStep = value;
                mMinStepSqr = value * value;
            }
        }

        public LayerZone(EditorTemplates dataroot, ZoneSlaveFactory slaveFactory, ILayerZoneListener client)
        {
            Alloc.RecordConstructor(GetType());
            this.SlaveFactory = slaveFactory;
            this.objectPool = new LayerObjectPool(this);
            this.log = LoggerFactory.GetLogger("ZoneClient");
            this.EnvironmentVarMap = new(this);
            this.mRandom = new Random();
            this.IsLoaded = false;
            this.DataRoot = dataroot;
            this.Templates = dataroot.Templates;
            this.LayerClient = client;
            this.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
            this.mSyncMessageQueue = new MessageQueue<IBattleMessage>(MainProcessMessage);
            this.mTasks = new MessageActionQueue<LayerZone>();
            this.mTasks.OnError += doError;
            this.mTimeTasks = new TimeTaskQueue(this.objectPool);
            this.CFG = Templates.DefaultConfig;
            this.ExtCFG = Templates.DefaultExtConfig;
            InitGUI();
        }
        //-------------------------------------------------------------------------------------------
        ~LayerZone()
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
            try { mOnDispose?.Invoke(this); } catch (Exception err) { err.PrintStackTrace(); }
            this.IsLoaded = false;
            this.ClearEvents();
            this.ClearGUIEvents();
            this.ClearGUI();
            this._components?.Dispose();
            this.mTasks?.Dispose();
            this.mTimeTasks?.Dispose();
            this.mSyncMessageQueue.Dispose();
            this.mSyncMessageQueue = null;
            this.mObjects.Dispose();
            this.mObjects = null;
            this.mActor = null;
            this.DataRoot = null;
            this.Templates = null;
            this.DisposeTerrain();
            this.mData = null;
            EnvironmentVarMap.Clear();
            mListenRequests.Clear();
            mLastServerStatus = null;
            objectPool.Dispose();
            LayerClient = null;
        }
        //-------------------------------------------------------------------------------------------
        private ComponentCollection<LayerZone, LayerZoneComponent> _components;
        public ComponentCollection<LayerZone, LayerZoneComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new ComponentCollection<LayerZone, LayerZoneComponent>(this, static (a, b) => a.Priority - b.Priority);
                }
                return _components;
            }
        }
        private void UpdateComponents(float intervalMS)
        {
            _components?.ForEach(intervalMS, static (st, c) => c.InternalUpdate(st));
        }
        //-------------------------------------------------------------------------------------------
        protected virtual bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata)
        {
            if (LayerClient.TryLoadSceneData(msg, out sdata))
            {
                return true;
            }
            sdata = DataRoot.LoadScene(msg.sceneID, false, true, false);
            return sdata != null;
        }
        protected virtual void InitSceneData(ClientEnterScene msg)
        {
            if (mData != null)
            {
                throw new Exception(string.Format("Layer already inited as id=[{0}] name=[{1}]", mData.ID, mData.Name));
            }
            if (!TryLoadSceneData(msg, out mData))
            {
                throw new Exception("Can not load scene data : " + msg.sceneID);
            }
            this.Sender = msg.sender;
            if (mData.OverrideConfig != null)
            {
                this.CFG = mData.OverrideConfig;
            }
            if (mData.OverrideExtConfig != null)
            {
                this.ExtCFG = mData.OverrideExtConfig;
            }
            {
                this.AsyncUnitPosModifyMaxRange = CFG.CLIENT_UNIT_MOVE_MODIFY_MAX_RANGE;
                this.AsyncUnitPosModifyMinRange = CFG.CLIENT_UNIT_MOVE_MODIFY_MIN_RANGE;
                this.MinStep = MoveHelper.GetDistance(1000 / CFG.SYSTEM_FPS, CFG.OBJECT_MOVE_TO_MIN_STEP_SEC);
                this.mElasticAngle = CMath.AngleToRadian(CFG.OBJECT_MOVE_BLOCK_ELASTIC_ANGLE);
                this.mElasticAngle2 = mElasticAngle * 2f;
            }
            this.ZoneUUID = msg.zoneUUID;
            this.Gravity = msg.gravity;
            this.StepHeight = msg.stepHeight;
            this.ServerResourceVersion = msg.resVersion;
            this.SpaceDivSizeW = msg.spaceDivW;
            this.LayerInitData = msg.initData;
            this.InitTerrain(msg);

            foreach (var dt in mData.Units)
            {
                var zea = new LayerEditorUnit(dt, this);
                mObjects.AddFlag(zea);
            }
            foreach (var dt in mData.Items)
            {
                var zea = new LayerEditorItem(dt, this);
                mObjects.AddFlag(zea);
            }
            foreach (var dt in mData.Regions)
            {
                var zea = new LayerEditorRegion(dt, this);
                mObjects.AddFlag(zea);
            }
            foreach (var dt in mData.Points)
            {
                var zea = new LayerEditorPoint(dt, this);
                mObjects.AddFlag(zea);
            }
            foreach (var dt in mData.Decorations)
            {
                var zed = new LayerEditorDecoration(dt, this);
                mObjects.AddFlag(zed);
            }
            foreach (var dt in mData.Areas)
            {
                var zea = new LayerEditorArea(dt, this);
                mObjects.AddFlag(zea);
            }

            mObjects.ForEachFlagsPredicate(this, static (st, flag) =>
            {
                flag.OnInit();
                return false;
            });
            if (this.mSpaceDiv != null)
            {
                foreach (var o in mObjects.Objects)
                {
                    if (o is ILayerZoneEntity et)
                    {
                        SwapSpace(et, true);
                    }
                }
            }

            IsLoaded = true;

            if (mLayerInit != null)
            {
                mLayerInit.Invoke(this);
            }
        }



        public SceneData Data
        {
            get { return mData; }
        }
        public int SceneID
        {
            get { return mData.ID; }
        }


        /// <summary>
        /// 获得当前服务端可同步环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetEnvironmentVar(string key)
        {
            return EnvironmentVarMap.GetEnvironmentVar(key);
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            return EnvironmentVarMap.GetEnvironmentVarAs<T>(key);
        }

        /// <summary>
        /// 获得当前服务端可同步环境变量列表
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<string> ListEnvironmentVars()
        {
            return EnvironmentVarMap.Keys;
        }
        public IEnumerable<KeyValuePair<string, object>> ListEnvironmentValues()
        {
            return EnvironmentVarMap.ListEnvironmentValues();
        }

        object IEnvironmentDecoder.DecodeZoneVar(object value)
        {
            return SlaveFactory.DecodeZoneVar(this, value);
        }


        public void BeginUpdate(float intervalMS)
        {
            if (IsDisposed) { return; }
            if (intervalMS > 0)
            {
                this.CurrentIntervalMS = intervalMS;
                this.mLocalPassTimeMS += intervalMS;
                mTasks.ProcessMessages(this);
                if (CFG != null)
                {
                    this.MinStep = MoveHelper.GetDistance(intervalMS, CFG.OBJECT_MOVE_TO_MIN_STEP_SEC);
                    this.mObjects.ForEachObjects(this, static (st, zo) =>
                    {
                        zo.InternalBeginUpdate();
                    });
                }
            }
            else
            {
                this.CurrentIntervalMS = 0;
                mTasks.ProcessMessages(this);
            }
        }

        public void Update()
        {
            try
            {
                if (IsDisposed) { return; }
                if (CurrentIntervalMS > 0)
                {
                    {
                        mSyncMessageQueue.ProcessMessages();
                        UpdateComponents(CurrentIntervalMS);
                        UpdateObjects();
                        mTimeTasks.Update(CurrentIntervalMS);
                    }
                    if (mListenRequestTimeout.Update(CurrentIntervalMS))
                    {
                        check_request_timeout();
                    }
                }
                else
                {
                    mSyncMessageQueue.ProcessMessages();
                }
            }
            finally
            {
                objectPool.UpdateRecycle();
            }
        }

        private void UpdateObjects()
        {
            this.mObjects.ForEachObjects(this, static (st, zo) =>
            {
                zo.InternalUpdate();
            });
            this.mObjects.ForEachObjects(this, static (st, zo) =>
            {
                zo.InternalEndUpdate();
            });
        }

        //-------------------------------------------------------------------------------------------
        public T CloneData<T>(T src) where T : ISerializable
        {
            return ObjectPool.Clone(ZoneDataFactory.Factory.PersistCodec, src);
        }

        //-------------------------------------------------------------------------------------------
        #region OBJECTS

        private bool addObj(LayerZoneObject obj)
        {
            if (mActor != null && mActor.ObjectID == obj.ObjectID)
            {
                obj.Dispose();
                return false;
            }
            if (obj is LayerPlayer actor)
            {
                mActor = actor;
            }
            var old = mObjects.RemoveObjectByKey(obj.ObjectID);
            if (old != null)
            {
                try
                {
                    if (old is ILayerZoneEntity ett && ett.CurrentCellNode != null)
                    {
                        ett.CurrentCellNode.Dispose();
                        //SwapSpace(ett, true);
                    }
                    if (mObjectLeave != null)
                    {
                        mObjectLeave.Invoke(this, old);
                    }
                }
                finally
                {
                    old.Dispose();
                }
            }
            mObjects.Add(obj);
            if (obj is ILayerZoneEntity et)
            {
                SwapSpace(et, true);
            }
            obj.OnAdded();
            if (mObjectEnter != null)
            {
                mObjectEnter.Invoke(this, obj);
            }
            if (obj == mActor)
            {
                if (mActorAdded != null)
                {
                    mActorAdded.Invoke(this, mActor);
                }
            }
            return true;
        }

        private void removeObj(uint objID)
        {
            LayerZoneObject ret = mObjects.RemoveObjectByKey(objID);
            if (ret != null)
            {
                try
                {
                    if (mObjectLeave != null)
                    {
                        mObjectLeave.Invoke(this, ret);
                    }
                    if (mActor == ret)
                    {
                        mActor = null;
                    }
                    if (ret is ILayerZoneEntity et && et.CurrentCellNode != null)
                    {
                        et.CurrentCellNode.Dispose();
                    }
                }
                finally
                {
                    ret.Dispose();
                }
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------

        #region TIMING_AND_TASK

        public void QueueTask(Action<LayerZone> task)
        {
            if (IsDisposing) return;
            mTasks.Enqueue(task);
        }
        public void QueueTask<ST>(ST st, Action<LayerZone, ST> task)
        {
            if (IsDisposing) return;
            mTasks.Enqueue(st, task);
        }
        //------------------------------------------------------------------------------------
        public TimeTaskMS AddTimeTask(float intervalMS, float delayMS, int repeat, TickHandler handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        public TimeTaskMS AddTimeDelayMS(float delayMS, TickHandler handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimeDelayMS(delayMS, handler);
        }
        public TimeTaskMS AddTimePeriodicMS(float intervalMS, TickHandler handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimePeriodicMS(intervalMS, handler);
        }
        //------------------------------------------------------------------------------------
        public TimeTaskMS<ST> AddTimeTask<ST>(ST st, float intervalMS, float delayMS, int repeat, TickHandler<ST> handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimeTask(intervalMS, delayMS, repeat, st, handler);
        }
        public TimeTaskMS<ST> AddTimeDelayMS<ST>(ST st, float delayMS, TickHandler<ST> handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimeDelayMS(delayMS, st, handler);
        }
        public TimeTaskMS<ST> AddTimePeriodicMS<ST>(ST st, float intervalMS, TickHandler<ST> handler)
        {
            if (IsDisposing) return null;
            return mTimeTasks.AddTimePeriodicMS(intervalMS, st, handler);
        }
        //------------------------------------------------------------------------------------
        #endregion
    }


}