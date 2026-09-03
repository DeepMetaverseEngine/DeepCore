using DeepCore.AI.LLM;
using DeepCore.Components;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    //-------------------------------------------------------------------------------------------------------//
    public abstract partial class InstanceZoneObject : InstanceZonePosition, IZoneObject
    {
        public static implicit operator bool(in InstanceZoneObject value) { return value != null; }
        //---------------------------------------------------------------------------------------
        private Geometry.Vector3 mPrevFramePos = new Geometry.Vector3();
        private float mPrevFrameDirection = 0;
        private float mPrevFrameBodyDirection = 0;
        private float mDirection = 0;
        private float mBodyDirection = 0;
        private readonly UnitSyncPos m_SyncPosCache = new UnitSyncPos();
        private bool mAdded = false;
        private uint mID = 0;
        private bool mMarkRemoved = false;
        private string mUnitTag = string.Empty;
        private ObjectAoiStatus mAoiStatus;
        private TimeExpire mPauseTime = null;
        private double mCurPassTimeMS = 0;
        private ZoneSpaceDivision.ZoneSpaceCellNode mCurrentSpace;
        private int mCurrentZoneInfoMatrixColor;
        private bool ignoreLookRange = false;
        private List<TimeTaskMS> timetasks = new List<TimeTaskMS>();

        private InstanceObjectComponentCollection _components;
        protected InstanceObjectComponentCollection _Components => _components;
        //---------------------------------------------------------------------------------------
        protected InstanceZoneObject() { }
        protected InstanceZoneObject(InstanceZone zone) : base(zone) { }
        protected override void Disposing()
        {
            try
            {
                this.IsPaused = false;
                if (this is IEntityObject po)
                {
                    Parent.clearSpace(po);
                }
                this.ClearEvents();
                this.ClearComponents();
                this.mPrevFramePos = default;
                this.mPrevFrameDirection = 0;
                this.mPrevFrameBodyDirection = 0;
                this.mDirection = 0;
                this.mBodyDirection = 0;
                this.m_SyncPosCache.Clean();
                this.mAdded = false;
                //this.mMarkRemoved = false;
                this.mUnitTag = string.Empty;
                this.mAoiStatus?.Dispose();
                this.mAoiStatus = default;
                this.mPauseTime?.Dispose();
                this.mPauseTime = null;
                this.mCurPassTimeMS = 0;
                this.mCurrentSpace = default;
                this.mCurrentZoneInfoMatrixColor = default;
                this.ignoreLookRange = false;
                for (int i = 0; i < timetasks.Count; i++)
                {
                    timetasks[i].Dispose();
                }
                this.timetasks.Clear();
            }
            finally
            {
                base.Disposing();
            }
        }
        //---------------------------------------------------------------------------------------

        IZone IZoneObject.Zone => Parent;
        //---------------------------------------------------------------------------------------
        public abstract int TemplateID { get; }
        public uint ID { get { return mID; } }
        public abstract string Name { get; }
        /// <summary>
        /// 是否已被添加到场景
        /// </summary>
        public bool IsInZone { get { return mAdded; } }
        /// <summary>
        /// 是否未被从场景移除
        /// </summary>
        public bool Enable { get { return !mMarkRemoved && !IsDisposing; } }
        /// <summary>
        /// 朝向
        /// </summary>
        sealed public override float Direction { get { return mDirection; } }
        sealed public override float BodyDirection { get { return mBodyDirection; } }
        sealed public override float BodySize => this.BodyBlockSize;
        /// <summary>
        /// 总共存活了多久
        /// </summary>
        public double PassTimeMS { get { return mCurPassTimeMS; } }
        /// <summary>
        /// 从编辑器继承过来的参数
        /// </summary>
        public string UnitTag { get { return mUnitTag; } set { this.mUnitTag = value; } }
        /// <summary>
        /// 是否被标记为暂停逻辑
        /// </summary>
        public virtual bool IsPaused { get; set; } = false;
        /// <summary>
        /// IAO标记
        /// </summary>
        public ObjectAoiStatus AoiStatus { get { return mAoiStatus; } set { SetAoiStatus(value); } }
        [Desc("无论多远都能看到")] public bool IgnoreLookRange { get => ignoreLookRange; set => ignoreLookRange = value; }

        public IPostChannel CurrentChannel { get => mAoiStatus != null ? mAoiStatus.Channel : mCurrentSpace?.Channel; }
        public int CurrentZoneInfoFlagget => mCurrentZoneInfoMatrixColor;
        /// <summary>
        /// 是否可以动
        /// </summary>
        abstract public bool Moveable { get; }
        /// <summary>
        /// 碰撞半径
        /// </summary>
        abstract public float BodyBlockSize { get; }
        /// <summary>
        /// 受击半径
        /// </summary>
        abstract public float BodyHitSize { get; }
        /// <summary>
        /// 是否同步客户端
        /// </summary>
        abstract public bool ClientVisible { get; }

        abstract public float Weight { get; }
        public Geometry.VoxelCylinder VoxelBodyHit { get { return new Geometry.VoxelCylinder(Position, BodyHitSize, BodyHeight); } }
        public uint ObjectID => this.ID;
        public Geometry.Vector3 PrevFramePos { get => mPrevFramePos; }
        public float PrevFrameDirection { get => mPrevFrameDirection; }
        public float PrevFrameBodyDirection { get => mPrevFrameBodyDirection; }

        public virtual void SetPassTimeMS(double passTimeMS)
        {
            this.mCurPassTimeMS = passTimeMS;
        }
        internal void cb_OnSwapZoneInfoMatrixColor(int oldColor, int newColor)
        {
            OnObjectSwapZoneInfoFlag?.Invoke(Parent, this, oldColor, newColor);
            Parent.cb_OnUnitSwapZoneInfoFlag(this, oldColor, newColor);
        }
        internal protected virtual void onSwapSpace(ZoneSpaceDivision.ZoneSpaceCellNode src, ZoneSpaceDivision.ZoneSpaceCellNode dst)
        {
            //mCurrentCellArea = dst.Area;
            mCurrentSpace = dst;
            //             if (mAoiStatus == null)
            //             {
            // 
            //             }
        }
        protected virtual void onAoiChanged(ObjectAoiStatus _old, ObjectAoiStatus _new)
        {
            //             if (_old != null)
            //             {
            //                 _old.Channel.OnLeave(this);
            //             }
            //             if (_new != null)
            //             {
            //                 if (_old == null)
            //                 {
            //                     mCurrentSpace.Channel.OnLeave(this);
            //                 }
            //                 _new.Channel.OnLeave(this);
            //             }
            //             else
            //             {
            //                 mCurrentSpace.Channel.OnEnter(this);
            //             }
        }

        protected virtual bool onTryAdd(ref Vector3 pos, float direction)
        {
            return true;
        }

        // 返回结果表示当前是否可以添加
        internal bool tryAdd(Vector3 pos, float direction)
        {
            if (mAdded == false && onTryAdd(ref pos, direction))
            {
                this.m_SyncPosCache.AddModifer(UnitSyncModifer.All);
                this.mAdded = true;
                this.mMarkRemoved = false;
                this.mID = Parent.genObjectID();
                this.mPrevFramePos = pos;
                this.mDirection = mPrevFrameDirection = direction;
                this.mBodyDirection = mPrevFrameBodyDirection = direction;
                {
                    this.m_SyncPosCache.Position = pos;
                    this.m_SyncPosCache.Direction = this.Direction;
                    this.m_SyncPosCache.BodyDirection = this.BodyDirection;
                }
                this.EnterWorld(mPrevFramePos);
                if (this is IEntityObject po)
                {
                    Parent.swapSpace(po);
                }
                return true;
            }
            return false;
        }

        // 更新坐标
        internal bool updatePos()
        {
            if (mMarkRemoved) return false;
            updatePosBegin();
            m_SyncPosCache.Begin(this.ID);
            if (mPrevFramePos != this.Position)
            {
                m_SyncPosCache.Position = this.Position;
                if (this is IEntityObject po)
                {
                    Parent.swapSpace(po);
                }
            }
            if (mPrevFrameDirection != this.Direction)
            {
                m_SyncPosCache.Direction = this.Direction;
            }
            if (mPrevFrameBodyDirection != this.BodyDirection)
            {
                m_SyncPosCache.BodyDirection = this.BodyDirection;
            }
            if (Zone.HasArea)
            {
                this.updateArea();
            }
            this.updatePosEnd(m_SyncPosCache);
            mPrevFramePos = this.Position;
            mPrevFrameDirection = this.Direction;
            mPrevFrameBodyDirection = this.BodyDirection;
            if (m_SyncPosCache.HasModifer(UnitSyncModifer.Posistion))
            {
                if (this is IEntityObject po)
                {
                    if (po.SpaceUserTag != null)
                    {
                        po.SpaceUserTag.MarkPosDirty();
                    }
                }
            }
            return m_SyncPosCache.IsDirty;
        }
        private void updateArea()
        {
            //ZoneArea old_area = mCurrentTouchedArea;
            //ZoneArea new_area = mCurrentCellArea?.TouchZ(this.Z);
            //             if (new_area != old_area)
            //             {
            //                 mCurrentTouchedArea = new_area;
            //                 Parent.swapArea(this, old_area, new_area);
            //                 onAreaChanged(old_area, new_area);
            //                 OnObjectAreaChanged?.Invoke(this, old_area, new_area);
            //             }
            var oldFlag = mCurrentZoneInfoMatrixColor;
            if (Parent.ZoneInfoMatrix.TryGetFlagByPos(X, Y, out var flag))
            {
                if (mCurrentZoneInfoMatrixColor != flag)
                {
                    mCurrentZoneInfoMatrixColor = flag;
                    cb_OnSwapZoneInfoMatrixColor(oldFlag, flag);
                }
            }
            else
            {
                if (mCurrentZoneInfoMatrixColor != 0)
                {
                    mCurrentZoneInfoMatrixColor = 0;
                    cb_OnSwapZoneInfoMatrixColor(oldFlag, 0);
                }
            }
        }

        protected virtual void updatePosBegin()
        {

        }
        protected virtual void updatePosEnd(UnitSyncPos cache)
        {

        }

        public virtual bool TryGetSyncPos(out UnitSyncPos pos)
        {
            if (m_SyncPosCache.IsDirty)
            {
                pos = m_SyncPosCache;
                return true;
            }
            else
            {
                pos = null;
                return false;
            }
        }

        //----------------------------------------------------------------------------------------

        internal void onAdded(InstanceZone zone)
        {
            this.onAdded();
            OnObjectAdded?.Invoke(this);
        }
        internal void onRemoved(InstanceZone zone)
        {
            OnObjectRemoved?.Invoke(this);
            this.mAdded = false;
            this.mMarkRemoved = true;
            //this.mCurrentCellArea = null;
            if (this is IEntityObject po)
            {
                zone.clearSpace(po);
            }
            this.SetAoiStatus(null);
            this.onRemoved();
        }

        // 返回结果表示是否位移
        internal void onUpdate(InstanceZone zone)
        {
            this.onUpdate();
            this.UpdateComponents();

            if (!IsPaused)
            {
                mCurPassTimeMS += zone.UpdateIntervalMS;
            }
            else if (mPauseTime != null)
            {
                if (mPauseTime.Update(zone.UpdateIntervalMS))
                {
                    mPauseTime.Dispose();
                    mPauseTime = null;
                    IsPaused = false;
                }
            }
        }

        internal void onSendingEvent(ref ObjectNotify evt)
        {
            OnObjectSendingEvent?.Invoke(this, ref evt);
        }


        //---------------------------------------------------------------------------------------------------------

        abstract protected void onUpdate();

        abstract protected void onAdded();
        abstract protected void onRemoved();

        //---------------------------------------------------------------------------------------------------------



        //protected virtual void onAreaChanged(ZoneArea old_area, ZoneArea new_area) { }



        //----------------------------------------------------------------------------------------
        /// <summary>
        /// 暂停逻辑
        /// </summary>
        /// <param name="pause"></param>
        /// <param name="timeMS"></param>
        public void Pause(bool pause, int timeMS = 0)
        {
            if (IsPaused != pause)
            {
                this.IsPaused = pause;
                this.mPauseTime?.Dispose();
                if (pause && timeMS > 0)
                {
                    this.mPauseTime = ObjectPool.AllocTimeExpire(timeMS);
                }
                else
                {
                    this.mPauseTime = null;
                }
            }
        }

        /*
        /// <summary>
        /// 被攻击到
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="attack"></param>
        /// <returns></returns>
        virtual internal bool onHitAttack(InstanceUnit sender, AttackProp attack) { return false; }
        */
        /// <summary>
        /// 获取同步信息
        /// </summary>
        abstract public SyncObjectInfo GenSyncInfo(bool network);

        //---------------------------------------------------------------------------------------
        /// <summary>
        /// 直接转向
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void FaceTo(float x, float y)
        {
            if (CMath.GetDistance(x, y, X, Y) < this.Parent.MinStep) { return; }
            this.FaceTo((float)(Math.Atan2(y - this.Y, x - this.X)));
        }
        public void FaceTo(Geometry.Vector2 pos)
        {
            this.FaceTo(pos.X, pos.Y);
        }
        public void FaceTo(float d)
        {
            InternalFaceTo(d);
        }
        public void Turn(float add)
        {
            InternalFaceTo(mDirection + add);
        }
        public void TurnTo(Geometry.Vector2 target, float turnSpeed, float intervalMS)
        {
            var d = MoveHelper.DirectionChange(this.Position, this.Direction, target, turnSpeed, intervalMS);
            InternalFaceTo(d);
        }

        protected virtual void InternalFaceTo(float d)
        {
            this.mDirection = d;
        }
        protected virtual void InternalBodyFaceTo(float d)
        {
            this.mBodyDirection = d;
        }


        protected bool SetPos(float x, float y, float z, bool touchMap = false)
        {
            return this.SetPos(new Geometry.Vector3(x, y, z), touchMap);
        }
        protected bool SetPos(Geometry.Vector3 pos, bool touchMap = false)
        {
            if (mMarkRemoved) return false;
            if (touchMap)
            {
                if (Parent.TryUpdatePos(this, ref pos, out var layer))
                {
                    this.InternalSetPos(pos);
                    if (this is IEntityObject po)
                    {
                        Parent.swapSpace(po);
                    }
                    return true;
                }
                return false;
            }
            else
            {
                this.InternalSetPos(pos);
                if (this is IEntityObject po)
                {
                    Parent.swapSpace(po);
                }
                return true;
            }
        }


        protected abstract void EnterWorld(Geometry.Vector3 pos);

        protected abstract void InternalSetPos(Geometry.Vector3 pos);
        //---------------------------------------------------------------------------------------
        public virtual void SetAoiStatus(ObjectAoiStatus aoi)
        {
            if (this.mAoiStatus != aoi)
            {
                var _old = this.mAoiStatus;
                if (_old != null)
                {
                    _old.RemoveObject(this);
                }
                this.mAoiStatus = aoi;
                var _new = this.mAoiStatus;
                if (_new != null)
                {
                    _new.AddObject(this);
                }
                this.onAoiChanged(_old, _new);
                _old?.Release();
                _new?.Retain();
            }
        }

        public virtual void SendForceSync()
        {
            ObjectForceSyncPosEvent mForceSync = ObjectPool.Alloc<ObjectForceSyncPosEvent>();
            mForceSync.object_id = this.ID;
            mForceSync.Pos = this.Position;
            mForceSync.Direction = this.Direction;
            mForceSync.BodyDirection = this.BodyDirection;
            Parent.PostObjectEvent(this, mForceSync);
        }
        public virtual void SendForceFaceSync()
        {
            ObjectForceSyncFaceEvent mForceFaceSync = ObjectPool.Alloc<ObjectForceSyncFaceEvent>();
            mForceFaceSync.object_id = this.ID;
            mForceFaceSync.Direction = this.Direction;
            mForceFaceSync.BodyDirection = this.BodyDirection;
            Parent.PostObjectEvent(this, mForceFaceSync);
        }
        public LLMAgent CreateAiAgent() => new LLMAgent(LLMEnvironment.Instance.CreateProxy());

        //----------------------------------------------------------------------------------------
        #region TimeTasks
        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeTask(float intervalMS, float delayMS, int repeat, TickHandler handler)
        {
            var t = Zone.AddTimeTask(intervalMS, delayMS, repeat, handler);
            timetasks.Add(t);
            return t;
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeDelayMS(float delayMS, TickHandler handler)
        {
            var t = Zone.AddTimeDelayMS(delayMS, handler);
            timetasks.Add(t);
            return t;
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimePeriodicMS(float intervalMS, TickHandler handler)
        {
            var t = Zone.AddTimePeriodicMS(intervalMS, handler);
            timetasks.Add(t);
            return t;
        }
        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeTask<ST>(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler)
        {
            var t = Zone.AddTimeTask<ST>(intervalMS, delayMS, repeat, st, handler);
            timetasks.Add(t);
            return t;
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimeDelayMS<ST>(float delayMS, ST st, TickHandler<ST> handler)
        {
            var t = Zone.AddTimeDelayMS<ST>(delayMS, st, handler);
            timetasks.Add(t);
            return t;
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS<ST> AddTimePeriodicMS<ST>(float intervalMS, ST st, TickHandler<ST> handler)
        {
            var t = Zone.AddTimePeriodicMS<ST>(intervalMS, st, handler);
            timetasks.Add(t);
            return t;
        }
        #endregion
        //----------------------------------------------------------------------------------------
        #region Compnents
        public InstanceObjectComponentCollection Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new(this, static (a, b) => a.Priority - b.Priority);
                }
                return _components;
            }
        }
        private void UpdateComponents()
        {
            _components?.ForEach(this, static (st, c) => c.InternalUpdate());
        }
        private void ClearComponents()
        {
            this._components?.Dispose();
            this._components = null;
        }

        #endregion
        //----------------------------------------------------------------------------------------
        #region DELEGATE

        public delegate void ObjectAddedHandler(InstanceZoneObject obj);
        public delegate void ObjectLateAddedHandler(InstanceZoneObject obj);
        public delegate void ObjectRemovedHandler(InstanceZoneObject obj);
        public delegate void ObjectSwapZoneInfoFlagHandler(InstanceZone zone, InstanceZoneObject unit, int oldFLag, int newFlag);
        public delegate void ObjectSendingEventHandler(InstanceZoneObject obj, ref ObjectNotify evt);

        [Desc("单位添加到场景")] public event ObjectAddedHandler OnObjectAdded;
        [Desc("单位被移除触发")] public event ObjectRemovedHandler OnObjectRemoved;
        [Desc("单位地图Flag颜色改变触发")] public event ObjectSwapZoneInfoFlagHandler OnObjectSwapZoneInfoFlag;
        [Desc("单位发送事件时触发")] public event ObjectSendingEventHandler OnObjectSendingEvent;

        protected virtual void ClearEvents()
        {
            OnObjectAdded = null;
            OnObjectRemoved = null;
            OnObjectSendingEvent = null;
            OnObjectSwapZoneInfoFlag = null;
        }
        #endregion
        //----------------------------------------------------------------------------------------
    }

}
