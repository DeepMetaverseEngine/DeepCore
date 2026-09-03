using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Threading;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;
using static DeepCore.Game3D.Host.Instance.Abilities.SpawnUnitAbility;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class ZoneRegion : InstanceFlag, IViewTriggerListener<InstanceUnit>, ISpawnContainer
    {
        new public InstanceZone Zone { get => Parent; }

        new public RegionData EditorData { get => base.EditorData as RegionData; }
        public RegionData.Shape RegionType { get { return mShape; } }
        public RegionData Data => this.EditorData as RegionData;
        /// <summary>
        /// Center of the position
        /// </summary>
        public override float BodySize { get { return Data.Radius; } }
        public override float Direction { get { return Data.Direction; } }
        public float Radius { get { return Data.Radius; } }
        readonly private RegionData.Shape mShape;
        //readonly private float W;
        //readonly private float H;
        //readonly private float R;

        private ViewTrigger<InstanceUnit> mViewTrigger;

        private SpawnCollection mSpawnTriggers;

        public ZoneRegion(InstanceZone zone, RegionData data)
            : base(zone, data)
        {
            this.mShape = data.RegionType;
            //             this.W = data.W;
            //             this.H = data.H;
            //             this.R = data.Radius;
            this.mEnterOnceList = new OnceInvokeList(zone.ObjectPool);
            this.mLeaveOnceList = new OnceInvokeList(zone.ObjectPool);
            switch (mShape)
            {
                case RegionData.Shape.RECTANGLE:
                    var rw = data.W / 2f;
                    var rh = data.H / 2f;
                    this.mViewTrigger = new ViewTriggerBoxCenter<InstanceUnit>(Parent, base.Position, data.W, data.H, BodyHeight);
                    break;
                case RegionData.Shape.STRIP:
                    this.mViewTrigger = new ViewTriggerRectStripCenter<InstanceUnit>(Parent, base.Position, data.Direction, data.W, data.H, BodyHeight);
                    break;
                case RegionData.Shape.ROUND:
                default:
                    this.mViewTrigger = new ViewTriggerCylinderCenter<InstanceUnit>(Parent, base.Position, data.R, BodyHeight);
                    break;
            }
            this.mViewTrigger.SetListener(this);
            this.mSpawnTriggers = new SpawnCollection(this);
        }

        protected override void Disposing()
        {
            mOnUnitEnter = null;
            mOnUnitLeave = null;
            mOnZoneUpdate = null;
            OnSpawnEnabled = null;
            OnSpawnDisabled = null;
            base.Disposing();
            mViewTrigger.Dispose();
            mSpawnTriggers.Dispose();
        }
        protected override void OnUpdate(bool active)
        {
            base.OnUpdate(active);
            if (active && Enable && (mOnUnitEnter != null || mOnUnitLeave != null || mEnterOnceList.Count > 0 || mLeaveOnceList.Count > 0))
            {
                mViewTrigger.Enable = true;
                mViewTrigger.LookUpdate(this.Position);
            }
            else
            {
                mViewTrigger.Enable = false;
            }
            if (active && Enable && mOnZoneUpdate != null)
            {
                mOnZoneUpdate.Invoke(this);
            }
        }
        sealed public override Geometry.Vector3 GetRandomPos()
        {
            var pos = this.Position;
            var random = Parent.RandomN;
            if (mShape == RegionData.Shape.ROUND)
            {
                float angle = (float)(random.NextFloat() * CMath.PI_MUL_2);
                float len = (float)(random.NextFloat() * Data.R);
                float x = X + (float)(Math.Cos(angle) * len);
                float y = Y + (float)(Math.Sin(angle) * len);
                pos = new Geometry.Vector3(x, y, Z);
            }
            else if (mShape == RegionData.Shape.STRIP)
            {
                float x =  (float)((-Data.W / 2f) + random.NextFloat() * Data.W);
                float y =  (float)((-Data.H / 2f) + random.NextFloat() * Data.H);
                pos = new Geometry.Vector3(x, y, Z);
                VectorHelper.Rotate(ref pos, this.Direction + CMath.RADIANS_90);
                pos.X += this.X;
                pos.Y += this.Y;
            }
            else
            {
                float x = X + (float)((-Data.W / 2f) + random.NextFloat() * Data.W);
                float y = Y + (float)((-Data.H / 2f) + random.NextFloat() * Data.H);
                pos = new Geometry.Vector3(x, y, Z);
            }
            return pos;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region Spawn
        public SpawnCollection SpawnCollection { get => mSpawnTriggers; }

        public void BeginSpawnOnce(AbstractSpawnAbility spawn)
        {

        }
        public void KeepInSpawnRegion(AbstractSpawnAbility spawn, ref Geometry.Vector3 pos)
        {
            switch (mShape)
            {
                case RegionData.Shape.RECTANGLE:
                    {
                        var box = (mViewTrigger as ViewTriggerBoxCenter<InstanceUnit>).Box;
                        pos.X = CMath.Clamp(pos.X, box.Min.X, box.Max.X);
                        pos.Y = CMath.Clamp(pos.Y, box.Min.Y, box.Max.Y);
                    }
                    break;
                case RegionData.Shape.STRIP:
                    {
                        var r = this.Radius;
                        float d = MathVector.getDistance(pos.X, pos.Y, this.X, this.Y);
                        if (d > r)
                        {
                            float a = MathVector.getDegree(pos.X, pos.Y, this.X, this.Y);
                            Geometry.VectorHelper.MovePolar(ref pos, a, d - r);
                        }
                    }
                    break;
                case RegionData.Shape.ROUND:
                    {
                        float d = MathVector.getDistance(pos.X, pos.Y, this.X, this.Y);
                        if (d > Data.R)
                        {
                            float a = MathVector.getDegree(pos.X, pos.Y, this.X, this.Y);
                            Geometry.VectorHelper.MovePolar(ref pos, a, d - Data.R);
                        }
                    }
                    break;
            }
        }

        public Vector3 GetSpawnPos(AbstractSpawnAbility spawn)
        {
            return this.GetRandomPos();
        }

        protected override void cb_InvokeEnable(bool value)
        {
            base.cb_InvokeEnable(value);
            if (value)
            {
                OnSpawnEnabled?.Invoke(this);
            }
            else
            {
                OnSpawnDisabled?.Invoke(this);
            }
        }

        public event Action<ISpawnContainer> OnSpawnEnabled;
        public event Action<ISpawnContainer> OnSpawnDisabled;

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region ViewTrigger

        void IViewTriggerListener<InstanceUnit>.OnObjectEnterView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            //             var cylinder = (mViewTrigger as ViewTriggerCylinderCenter<InstanceUnit>).Cylinder;
            //             var a1 = Collider.Cylinder_Touch_BlockBody(obj, in cylinder);
            //             var a2 = isInRegion(obj);
            if (mOnUnitEnter != null)
            {
                mOnUnitEnter.Invoke(this, obj as InstanceUnit);
            }
            mEnterOnceList.Invoke(this, obj as InstanceUnit);
            Zone.cb_OnUnitEnterRegion(obj, this);
        }

        void IViewTriggerListener<InstanceUnit>.OnObjectLeaveView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            if (mOnUnitLeave != null)
            {
                mOnUnitLeave.Invoke(this, obj as InstanceUnit);
            }
            mLeaveOnceList.Invoke(this, obj as InstanceUnit);
            Zone.cb_OnUnitLeaveRegion(obj, this);
        }
        bool IViewTriggerListener<InstanceUnit>.Select(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            return true;
        }

        public void addInRegionViewed(InstanceUnit unit)
        {
            if (mViewTrigger != null)
            {
                mViewTrigger.AddViewed(unit);
            }
        }

        public bool isInRegion(Geometry.Vector3 pos)
        {
            if (this.RegionType == RegionData.Shape.RECTANGLE)
            {
                var box = (mViewTrigger as ViewTriggerBoxCenter<InstanceUnit>).Box;
                return box.Contains(pos) != Geometry.ContainmentType.Disjoint;
            }
            else if (this.RegionType == RegionData.Shape.STRIP)
            {
                var rect = (mViewTrigger as ViewTriggerRectStripCenter<InstanceUnit>).Rect;
                return rect.Contains(pos);
            }
            else
            {
                var cylinder = (mViewTrigger as ViewTriggerCylinderCenter<InstanceUnit>).Cylinder;
                return cylinder.Intersects(in pos);
            }
        }

        public bool isInRegion(InstanceZoneObject obj)
        {
            if (this.RegionType == RegionData.Shape.RECTANGLE)
            {
                var box = (mViewTrigger as ViewTriggerBoxCenter<InstanceUnit>).Box;
                return Collider.Box_Touch_BlockBody<ZoneRegion>(this, obj, in box);
            }
            else if (this.RegionType == RegionData.Shape.STRIP)
            {
                var rect = (mViewTrigger as ViewTriggerRectStripCenter<InstanceUnit>).Rect;
                return Collider.RectStripe_Touch_BlockBodye<ZoneRegion>(this, obj, in rect);
            }
            else
            {
                var cylinder = (mViewTrigger as ViewTriggerCylinderCenter<InstanceUnit>).Cylinder;
                return Collider.Cylinder_Touch_BlockBody<ZoneRegion>(this, obj, in cylinder);
            }
        }

        public int GetObjectsInRegion<T>(List<T> list) where T : InstanceZoneEntity
        {
            if (this.RegionType == RegionData.Shape.RECTANGLE)
            {
                var box = (mViewTrigger as ViewTriggerBoxCenter<InstanceUnit>).Box;
                return Parent.GetObjectsInBox<ZoneRegion, T>(this,
                     static (ZoneRegion state, InstanceZoneObject o, in BoundingBox shape) => Collider.Box_Touch_BlockBody<ZoneRegion>(state, o, in shape),
                     box, list);
            }
            else if (this.RegionType == RegionData.Shape.STRIP)
            {
                var rect = (mViewTrigger as ViewTriggerRectStripCenter<InstanceUnit>).Rect;
                return Parent.GetObjectsInRectStripe<ZoneRegion, T>(this,
                    static (ZoneRegion state, InstanceZoneObject o, in VoxelRectStripe shape) => Collider.RectStripe_Touch_BlockBodye<ZoneRegion>(state, o, in shape),
                    rect, list);
            }
            else
            {
                var cylinder = (mViewTrigger as ViewTriggerCylinderCenter<InstanceUnit>).Cylinder;
                return Parent.GetObjectsInCylinder<ZoneRegion, T>(this,
                    static (ZoneRegion state, InstanceZoneObject o, in VoxelCylinder shape) => Collider.Cylinder_Touch_BlockBody(state, o, in shape), cylinder, list);
            }
        }
        public int GetObjectsCountInRegion<T>() where T : InstanceZoneEntity
        {
            using (var list = Parent.ObjectPool.AllocList<T>())
            {
                return GetObjectsInRegion(list);
            }

        }
        public int GetObjectsCountInRegion<T>(Predicate<T> select) where T : InstanceZoneEntity
        {
            int ret = 0;
            using (var list = Parent.ObjectPool.AllocList<T>())
            {
                GetObjectsInRegion<T>(list);
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (select.Invoke(list[i]))
                    {
                        ret++;
                    }
                }
            }
            return ret;
        }

        //--------------------------------------------------------------------------------------------------------------

        public int InRegionUnitCount
        {
            get
            {
                using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
                {
                    this.GetObjectsInRegion(list);
                    return list.Count;
                }
            }
        }

        public bool IsInRegion(Geometry.Vector3 pos)
        {
            return this.isInRegion(pos);
        }

        public bool IsInRegion(InstanceZoneObject o)
        {
            return this.isInRegion((InstanceZoneObject)o);
        }

        public T ForEachObjectsInRegion<T>(BreakPredicate<T> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
            {
                this.GetObjectsInRegion(list);
                foreach (var o in list)
                {
                    if (o is T t && indexer(t)) return t;
                }
            }
            return default(T);
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
        #region Delegate

        /// <summary>
        /// 某单位进入此区域
        /// </summary>
        /// <param name="region"></param>
        /// <param name="obj"></param>
        public delegate void UnitEnterHandler(ZoneRegion region, InstanceUnit obj);

        /// <summary>
        /// 某单位离开此区域
        /// </summary>
        /// <param name="region"></param>
        /// <param name="obj"></param>
        public delegate void UnitLeaveHandler(ZoneRegion region, InstanceUnit obj);

        /// <summary>
        /// 区域更新
        /// </summary>
        /// <param name="region"></param>
        public delegate void ZoneUpdateHandler(ZoneRegion region);


        private UnitEnterHandler mOnUnitEnter;
        private UnitLeaveHandler mOnUnitLeave;
        private ZoneUpdateHandler mOnZoneUpdate;

        public event UnitEnterHandler OnUnitEnter { add { mOnUnitEnter += value; } remove { mOnUnitEnter -= value; } }
        public event UnitLeaveHandler OnUnitLeave { add { mOnUnitLeave += value; } remove { mOnUnitLeave -= value; } }
        public event ZoneUpdateHandler OnZoneUpdate { add { mOnZoneUpdate += value; } remove { mOnZoneUpdate -= value; } }

        //--------------------------------------------------------------------------------------------------------------
        private struct OnceEvent : IOnceInvoke
        {
            public InstanceUnit unit { get; private set; }
            public bool IsDone { get { return done || !unit.Enable; } }
            private UnitEnterHandler enter_handler;
            private UnitLeaveHandler leave_handler;
            private bool done;

            public OnceEvent(InstanceUnit unit, UnitEnterHandler enter, UnitLeaveHandler leave)
            {
                this.unit = unit;
                this.enter_handler = enter;
                this.leave_handler = leave;
                this.done = false;
            }
            public void Invoke(ZoneRegion region)
            {
                if (enter_handler != null) this.enter_handler.Invoke(region, unit);
                if (leave_handler != null) this.leave_handler.Invoke(region, unit);
                this.enter_handler = null;
                this.leave_handler = null;
                this.done = true;
            }
        }
        private class OnceInvokeList : OnceInvokeList<OnceEvent>
        {
            public OnceInvokeList(SingleThreadCollectionPool pool) : base(pool)
            {
            }
            public void Invoke(ZoneRegion region, InstanceUnit unit)
            {
                var tuple = new ValueTuple<ZoneRegion, InstanceUnit>(region, unit);
                base.Invoke(tuple, static (st, e) =>
                {
                    if (e.unit == st.Item2)
                    {
                        e.Invoke(st.Item1);
                    }
                });
            }
        }
        private OnceInvokeList mEnterOnceList;
        private OnceInvokeList mLeaveOnceList;


        /// <summary>
        /// 监听单位进入一次，触发器只触发一次
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="handler"></param>
        public void ListenUnitEnterOnce(InstanceUnit unit, UnitEnterHandler handler)
        {
            mEnterOnceList.Add(new OnceEvent(unit, handler, null));
        }
        /// <summary>
        /// 监听单位离开一次，触发器只触发一次
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="handler"></param>
        public void ListenUnitLeaveOnce(InstanceUnit unit, UnitLeaveHandler handler)
        {
            mLeaveOnceList.Add(new OnceEvent(unit, null, handler));
        }

        //--------------------------------------------------------------------------------------------------------------

        protected override void clearEvents()
        {
            base.clearEvents();
            mEnterOnceList.Clear();
            mLeaveOnceList.Clear();
            mOnUnitEnter = null;
            mOnUnitLeave = null;
            mOnZoneUpdate = null;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------------------------
    }


}
