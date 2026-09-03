using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static DeepCore.Game3D.Host.Instance.ZoneSpaceDivision;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Host.Instance
{
    public abstract partial class InstanceFlag : InstanceZonePosition
    {
        public string Name { get { return mName; } }
        public string EditName => this.Name;
        public string EditorPath => this.mPdata.SavePath;
        public string Alias { get { return mAlias; } }
        public override float X { get { return mPos.X; } }
        public override float Y { get { return mPos.Y; } }
        public override float Z { get { return mPos.Z; } }
        public override Geometry.Vector3 Position => mPos.ToGeometry3();
        public override float BodyDirection => this.Direction;
        public override float BodyHeight => mPdata.Height;
        public SceneObjectData EditorData { get => mPdata; }
        public IZoneShape ZoneShape { get; set; }
        public ZoneSpaceCellNode SpaceCellNode => mCurrentDivNode;

        private VectorObject3 mPos = new VectorObject3();
        private SceneObjectData mPdata;
        private readonly string mName;
        private readonly string mAlias;
        private readonly string mSrcTag;
        private bool mEnableTriggerd = false;
        private bool mEnable = true;
        private string mTag;
        private ZoneSpaceCellNode mCurrentDivNode;

        private ITerrainAgent mRandomPos;
        private ITerrainLayer mOrginLayer;
        public ITerrainLayer OrginLayer { get => mOrginLayer; }

        public InstanceFlag(InstanceZone zone, SceneObjectData data) : base(zone)
        {
            this.mName = data.Name;
            this.mAlias = data.Alias;
            this.mPos = new VectorObject3(data.X, data.Y, data.Z);
            this.mTag = data.Tag;
            this.mSrcTag = data.Tag;
            this.mPdata = data;
            this.BindAttributes(data.Attributes);
            this.mOrginLayer = Parent.Terrain3D.GetVoxelLayerByPos(this.Position);
            this.mRandomPos = Parent.TerrainWorld.CreateAgent();
            this.mRandomPos.EnterWorld(zone.TerrainWorld);
            if (mOrginLayer == null)
            {
                throw new Exception($"{GetType().Name} '{data.Name}' is in blackhole in MapId =" + zone.SceneData.ID);
            }
            this.mRandomPos.Transport(this.mOrginLayer);
        }
        public override string ToString()
        {
            return $"{mName}:{Enable}";
        }
        public bool Enable
        {
            get { return mEnable; }
            set
            {
                if (mEnable != value)
                {
                    Parent.PostEvent(ObjectPool.Alloc<FlagEnableChangedEvent>().Init (this.mName, value));
                }
                if (!mEnableTriggerd || mEnable != value)
                {
                    mEnableTriggerd = true;
                    mEnable = value;
                    cb_InvokeEnable(value);
                }
            }
        }
        public string Tag
        {
            get { return mTag; }
            set
            {
                if (mTag != value)
                {
                    mTag = value;
                    Parent.PostEvent(ObjectPool.Alloc<FlagTagChangedEvent>().Init (this.mName, this.mTag));
                }
            }
        }
        public string SrcTag
        {
            get { return mSrcTag; }
        }


        protected override void Disposing()
        {
            base.Disposing();
            this.clearEvents();
        }

        virtual internal void onAdded()
        {
            mCurrentDivNode = Parent.GetSpaceCellNode(this.X, this.Y);
            if (mOnInit != null)
            {
                mOnInit.Invoke(this);
            }
        }
        protected internal virtual void OnStart()
        {

        }

        internal void update()
        {
            var active = BeginUpdate();
            OnUpdate(active);
        }
        protected virtual void OnUpdate(bool active) { }
        protected virtual bool BeginUpdate()
        {
            return true;
        }
        public override Geometry.Vector3 GetRandomPos()
        {
            var pos = this.Position;
            var random = Parent.RandomN;
            float angle = (float)(random.NextFloat() * CMath.PI_MUL_2);
            float len = (float)(random.NextFloat() * BodySize);
            float x = X + (float)(Math.Cos(angle) * len);
            float y = Y + (float)(Math.Sin(angle) * len);
            pos = new Geometry.Vector3(x, y, Z);
            return pos;
        }
        public Geometry.Vector3 GetSpawnPos()
        {
            var pos = GetRandomPos();
            this.mRandomPos.Transport(this.mOrginLayer);
            this.mRandomPos.MoveLinearTo2D(pos, out var touched);
            return this.mRandomPos.Position;
        }
        //----------------------------------------------------------------------------

        /// <summary>
        /// Show Check Point
        /// </summary>
        /// <param name="target"></param>
        public void LookAtTarget(string target)
        {
            Parent.PostEvent(ObjectPool.Alloc<LookAtEvent>().Init (target, 0, 0));
        }

        //         #region Compnents
        // 
        //         private readonly ComponentCollection<InstanceObjectComponent> _components;
        //         public ComponentCollection<InstanceObjectComponent> Components { get => _components; }
        //         private ComponentCollection<InstanceObjectComponent> createComponents()
        //         {
        //             var ret = new ComponentCollection<InstanceObjectComponent>((a, b) => a.Priority - b.Priority);
        //             ret.OnAdded += (obj) => { obj.InternalAdded(this); };
        //             ret.OnRemoved += (obj) => { obj.InternalRemoved(this); };
        //             return ret;
        //         }
        //         private void updateComponents()
        //         {
        //             _components.ForEach(c => c.InternalUpdate());
        //         }
        // 
        //         #endregion
        //----------------------------------------------------------------------------
        #region Delegate


        /// <summary>
        /// 触发器开始
        /// </summary>
        /// <param name="flag"></param>
        public delegate void InitHandler(InstanceFlag flag);

        /// <summary>
        /// 触发器被开启
        /// </summary>
        /// <param name="flag"></param>
        public delegate void FlagEnabledHandler(InstanceFlag flag);

        /// <summary>
        /// 触发器被关闭
        /// </summary>
        /// <param name="flag"></param>
        public delegate void FlagDisabledHandler(InstanceFlag flag);

        private InitHandler mOnInit;
        private FlagEnabledHandler mOnFlagEnabled;
        private FlagDisabledHandler mOnFlagDisabled;

        public event InitHandler OnInit { add { mOnInit += value; } remove { mOnInit -= value; } }
        public event FlagEnabledHandler OnFlagEnabled { add { mOnFlagEnabled += value; } remove { mOnFlagEnabled -= value; } }
        public event FlagDisabledHandler OnFlagDisabled { add { mOnFlagDisabled += value; } remove { mOnFlagDisabled -= value; } }

        protected virtual void cb_InvokeEnable(bool value)
        {
            if (value)
            {
                mOnFlagEnabled?.Invoke(this);
                Zone.cb_OnFlagOn(this);
            }
            else
            {
                mOnFlagDisabled?.Invoke(this);
                Zone.cb_OnFlagOff(this);
            }
        }
        protected virtual void clearEvents()
        {
            mOnInit = null;
            mOnFlagEnabled = null;
            mOnFlagDisabled = null;
        }

        #endregion
        //----------------------------------------------------------------------------
        #region NextPoints
        private IList<InstanceFlag> mNexts = new List<InstanceFlag>();
        private WeightDropList<InstanceFlag> mNextPop = new WeightDropList<InstanceFlag>();
        internal void InitNexts()
        {
            if (EditorData is SceneVirtualObjectData sv)
            {
                foreach (string nextname in sv.NextNames)
                {
                    var nextpoint = Parent.GetFlag(nextname);
                    if (nextpoint != null)
                    {
                        this.AddNext(nextpoint);
                    }
                    else
                    {
                        Log.Warn("can not find next point : " + EditorData.Name + " -> " + nextname);
                    }
                }
            }
        }
        private void AddNext(InstanceFlag next)
        {
            mNexts.Add(next);
            float percent = 100;
            if (next is ZoneWayPoint nextP)
            {
                percent = nextP.Data.NextPercent;
            }
            mNextPop.AddItem(next, Math.Max(1, percent));
        }
        public bool TryPopRandomNext(out InstanceFlag next, InstanceFlag prev = null)
        {
            if (mNextPop.TryDropOnce(RandomN, out next, prev, static (prev, flag) => flag == prev))
            {
                return true;
            }
            return false;
        }
        public InstanceFlag PopRandomNext(InstanceFlag prev = null)
        {
            if (mNextPop.TryDropOnce(RandomN, out var next, prev, static (prev, flag) => flag == prev))
            {
                return next;
            }
            return null;
        }

        public delegate void OnUnitPathPassDelegate(InstanceFlag point, InstanceFlag next, InstanceUnit unit);
        public delegate void OnUnitPathHoldDelegate(InstanceFlag point, PointHoldAbility hold, InstanceUnit unit);

        public event OnUnitPathPassDelegate OnUnitPassPath;
        public event OnUnitPathHoldDelegate OnUnitHoldPath;

        public virtual void InvokePathPass(InstanceUnit unit, InstanceFlag next)
        {
            OnUnitPassPath?.Invoke(this, next, unit);
            Zone.cb_OnUnitPassPoint(unit, this, next);
        }
        public virtual bool InvokeTryPathHold(InstanceUnit unit, out PointHoldAbility ab)
        {
            if (this is ZoneWayPoint point)
            {
                if (point.Data.TryGetAbilityOf<PointHoldAbility>(out ab))
                {
                    OnUnitHoldPath?.Invoke(this, ab, unit);
                    Zone.cb_OnUnitHoldPoint(unit, this, ab);
                    return true;
                }
            }
            ab = null;
            return false;

        }
        #endregion
        //----------------------------------------------------------------------------
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------
    #region API

    #endregion
    //-------------------------------------------------------------------------------------------------------------------------------------------
}
