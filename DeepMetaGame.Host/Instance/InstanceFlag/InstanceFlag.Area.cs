using DeepCore.Threading;
using DeepMetaGame.Data.ZoneEditor;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class ZoneArea : InstanceFlag
    {
        new public AreaData EditorData { get => base.EditorData as AreaData; }
        public override float BodySize => this.R;
        public override float Direction { get { return 0; } }
        // public int CurrentMapNodeValue { get; private set; }
        public Geometry.BoundingBox AABB { get; private set; }

        //public float Width { get { return W; } }
        private float W;
        private float H;
        private float R;

        public ZoneArea(InstanceZone zone, AreaData data)
            : base(zone, data)
        {
            this.W = data.W;
            this.H = data.H;
            this.R = data.W / 2;
            this.mEnterOnceList = new OnceInvokeList(zone.ObjectPool);
            this.mLeaveOnceList = new OnceInvokeList(zone.ObjectPool);

        }

        internal void BindMapBlock()
        {
            Parent.SpaceDiv.ClampPosition(
                      this.EditorData.X - this.EditorData.W / 2f,
                      this.EditorData.Y - this.EditorData.H / 2f,
                      this.EditorData.X + this.EditorData.W / 2f,
                      this.EditorData.Y + this.EditorData.H / 2f,
                      out var cx1, out var cy1, out var cx2, out var cy2);
            var cs = Parent.SpaceDivSizeW;
            this.AABB = new Geometry.BoundingBox(
                 new Geometry.Vector3(cx1 * cs, cy1 * cs, this.EditorData.Z - this.EditorData.Height),
                 new Geometry.Vector3(cx2 * cs + cs, cy2 * cs + cs, this.EditorData.Z + this.EditorData.Height));
//             Parent.SpaceDiv.ForEachSpaceCellNodes(cx1, cy1, cx2, cy2, this, static (area, cell) =>
//             {
//                 var sc = cell as ZoneSpaceDivision.ZoneSpaceCellNode;
//                 //sc.Area = area;
//             });
        }
        public ZoneArea TouchZ(float z)
        {
            if (Math.Abs(this.Z - z) <= this.BodyHeight)
            {
                return this;
            }
            return null;
        }
        public override Geometry.Vector3 GetRandomPos()
        {
            var random = Parent.RandomN;
            float x = X + (float)((-W / 2f) + random.NextDouble() * W);
            float y = Y + (float)((-H / 2f) + random.NextDouble() * H);
            return new Geometry.Vector3(x, y, Z);
        }

        internal void do_onUnitEnter(InstanceZoneObject obj)
        {
            if (obj is InstanceUnit u)
            {
                if (mOnUnitEnter != null) mOnUnitEnter.Invoke(this, obj as InstanceUnit);
                if (mEnterOnceList.Count > 0) mEnterOnceList.Invoke(this, obj as InstanceUnit);
                Zone.cb_OnUnitEnterArea(u, this);
            }
        }
        internal void do_onUnitLeave(InstanceZoneObject obj)
        {
            if (obj is InstanceUnit u)
            {
                if (mOnUnitLeave != null) mOnUnitLeave.Invoke(this, obj as InstanceUnit);
                if (mLeaveOnceList.Count > 0) mLeaveOnceList.Invoke(this, obj as InstanceUnit);
                Zone.cb_OnUnitLeaveArea(u, this);
            }
        }

        //----------------------------------------------------------------------------
        #region Delegate


        /// <summary>
        /// 某单位进入此区域
        /// </summary>
        /// <param name="area"></param>
        /// <param name="obj"></param>
        public delegate void UnitEnterHandler(ZoneArea area, InstanceUnit obj);

        /// <summary>
        /// 某单位离开此区域
        /// </summary>
        /// <param name="area"></param>
        /// <param name="obj"></param>
        public delegate void UnitLeaveHandler(ZoneArea area, InstanceUnit obj);


        private UnitEnterHandler mOnUnitEnter;
        private UnitLeaveHandler mOnUnitLeave;

        public event UnitEnterHandler OnUnitEnter { add { mOnUnitEnter += value; } remove { mOnUnitEnter -= value; } }
        public event UnitLeaveHandler OnUnitLeave { add { mOnUnitLeave += value; } remove { mOnUnitLeave -= value; } }


        //----------------------------------------------------------------------------
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
            public void Invoke(ZoneArea area)
            {
                if (enter_handler != null) this.enter_handler.Invoke(area, unit);
                if (leave_handler != null) this.leave_handler.Invoke(area, unit);
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
            public void Invoke(ZoneArea area, InstanceUnit unit)
            {
                var tuple = new ValueTuple<ZoneArea, InstanceUnit>(area, unit);
                base.Invoke(in tuple, static (st, e) =>
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

        //----------------------------------------------------------------------------

        protected override void clearEvents()
        {
            base.clearEvents();
            mEnterOnceList.Clear();
            mLeaveOnceList.Clear();
            mOnUnitEnter = null;
            mOnUnitLeave = null;
        }


        #endregion
        //----------------------------------------------------------------------------



        public AreaData Data { get => this.EditorData as AreaData; }
    }
}
