using DeepCore;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.ItemTemplateValue;

namespace DeepCore.Game3D.Host.Instance.Triggers
{
    //-------------------------------------------------------------------------------------------
    #region BASE
    /// <summary>
    /// 监听单位进出观察范围
    /// </summary>
    public interface IViewTriggerListener<T> where T : InstanceZoneEntity
    {
        /// <summary>
        /// 当一个单位进入观察范围后触发
        /// </summary>
        /// <param name="src">观察者</param>
        /// <param name="obj">进入视野的单位</param>
        void OnObjectEnterView(ViewTrigger<T> src, T obj);

        /// <summary>
        /// 当一个单位离开观察范围后触发
        /// </summary>
        /// <param name="src">观察者</param>
        /// <param name="obj">离开视野范围的单位</param>
        void OnObjectLeaveView(ViewTrigger<T> src, T obj);

        /// <summary>
        /// 是否可见
        /// </summary>
        /// <param name="src"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool Select(ViewTrigger<T> src, T obj);
    }

    /// <summary>
    /// 观察一定范围的触发器
    /// </summary>
    abstract public class ViewTrigger<T> : Disposable where T : InstanceZoneEntity
    {
        public InstanceZone Zone { get; private set; }
        private bool mEnable = true;
        private int mMaxViewd = int.MaxValue;
        private IViewTriggerListener<T> mViewListener;
        private List<T> mViewd = new List<T>();
        private bool nearDirty = true;
        /// <summary>
        /// 检测频率
        /// </summary>
        private readonly TimeInterval<int> mCheckRate;

        public bool Enable
        {
            get { return mEnable; }
            set
            {
                if (mEnable != value)
                {
                    this.mEnable = value;
                    this.nearDirty = true;
                }
            }
        }

        public IEnumerable<T> InViewed
        {
            get { return mViewd; }
        }

        public ViewTrigger(InstanceZone zone)
        {
            this.Zone = zone;
            this.mCheckRate = new TimeInterval<int>(Zone.CFG.AI_VIEW_TRIGGER_CHECK_TIME_MS);
        }
        protected override void Disposing()
        {
            this.mEnable = false;
            this.mViewd.Clear();
        }

        public void ClearViewd()
        {
            for (int i = mViewd.Count - 1; i >= 0; --i)
            {
                onObjectLeaveView(mViewd[i]);
            }
            mViewd.Clear();
        }

        public void AddViewed(T obj)
        {
            mViewd.Add(obj);
        }
        public InstanceZoneObject GetRandomViewed(Random random)
        {
            return random.GetRandomInCollection<T>(mViewd);
        }
        public void GetRandomViewedList(Random random, List<T> ret)
        {
            ret.AddRange(mViewd);
            CUtils.RandomList<T>(random, ret);
        }
        public T ForEachViewd(BreakPredicate<T> action)
        {
            foreach (var obj in this.mViewd)
            {
                if (action(obj)) return obj;
            }
            return null;
        }


        /// <summary>
        /// 设置最大观察单位数量
        /// </summary>
        /// <param name="m"></param>
        public void SetMaxViewd(int m)
        {
            this.mMaxViewd = m;
        }

        /// <summary>
        /// 增加观察者监听器
        /// </summary>
        /// <param name="listener"></param>
        public void SetListener(IViewTriggerListener<T> listener)
        {
            mViewListener = listener;
        }

        protected void onObjectEnterView(T o)
        {
            mViewListener?.OnObjectEnterView(this, o);
            event_OnObjectEnterView?.Invoke(this, o);
        }
        protected void onObjectLeaveView(T o)
        {
            mViewListener?.OnObjectLeaveView(this, o);
            event_OnObjectLeaveView?.Invoke(this, o);
        }
        protected bool onSelect(T o)
        {
            if (mViewListener != null && mViewListener.Select(this, o) == false)
            {
                return false;
            }
            if (event_OnSelect != null && event_OnSelect.Invoke(this, o) == false)
            {
                return false;
            }
            return true;
        }
        public virtual void LookUpdate(Geometry.Vector3 pos)
        {
            if (mEnable)
            {
                //                 if (IsNearChanged())
                //                 {
                //                     this.nearDirty = true;
                //                 }
                if (this.nearDirty || mCheckRate.Update(Zone.UpdateIntervalMS))
                {
                    this.nearDirty = false;
                    this.Check();
                    //                     if (nearDirty)
                    //                     {
                    //                         Console.WriteLine("递归了");
                    //                     }
                    this.nearDirty = false;
                }
            }
            else if (mViewd.Count > 0)
            {
                ClearViewd();
            }
        }

        protected virtual void Check()
        {
            // 检索已看到的消失
            for (int i = mViewd.Count - 1; i >= 0; --i)
            {
                var o = mViewd[i];
                if (!o.Enable || !onSelect(o) || !TestInView(o))
                {
                    mViewd.RemoveAt(i);
                    onObjectLeaveView(o);
                }
            }
            using (var for1 = Zone.ObjectPool.AllocForEach1<T, ViewTrigger<T>>(this))
            {
                this.ForEachNearObjects(for1, static (st) =>
                {
                    var o = st.Iterator;
                    var owner = st.Arg1;
                    if (o.Enable && !owner.mViewd.Contains(o) && owner.onSelect(o) && owner.TestInView(o))
                    {
                        owner.mViewd.Add(o);
                        owner.onObjectEnterView(o);
                    }
                });
            }
        }

        abstract protected bool IsNearChanged();
        abstract protected bool TestInView(T o);
        abstract protected void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer) where ST : ForEachInput<T>;


        private ObjectEnterViewHandler event_OnObjectEnterView;
        private ObjectLeaveViewHandler event_OnObjectLeaveView;
        private SelectHandler event_OnSelect;

        public event ObjectEnterViewHandler OnObjectEnterView { add { event_OnObjectEnterView += value; } remove { event_OnObjectEnterView -= value; } }
        public event ObjectLeaveViewHandler OnObjectLeaveView { add { event_OnObjectLeaveView += value; } remove { event_OnObjectLeaveView -= value; } }
        public event SelectHandler OnSelect { add { event_OnSelect += value; } remove { event_OnSelect -= value; } }

        /// <summary>
        /// 当一个单位进入观察范围后触发
        /// </summary>
        /// <param name="src">观察者</param>
        /// <param name="obj">进入视野的单位</param>
        public delegate void ObjectEnterViewHandler(ViewTrigger<T> src, T obj);

        /// <summary>
        /// 当一个单位离开观察范围后触发
        /// </summary>
        /// <param name="src">观察者</param>
        /// <param name="obj">离开视野范围的单位</param>
        public delegate void ObjectLeaveViewHandler(ViewTrigger<T> src, T obj);

        /// <summary>
        /// 是否可见
        /// </summary>
        /// <param name="src"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public delegate bool SelectHandler(ViewTrigger<T> src, T obj);

    }

    //-------------------------------------------------------------------------------------------

    /// <summary>
    /// 瞎子
    /// </summary>
    public class ViewTriggerBlind<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        public ViewTriggerBlind(InstanceZone zone) : base(zone)
        {

        }
        public override void LookUpdate(Vector3 pos)
        {

        }
        protected override void Check()
        {
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
        }
        protected override bool IsNearChanged()
        {
            return false;
        }
        protected override bool TestInView(T o)
        {
            return false;
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------
    #region Sphere
    /// <summary>
    /// 观察目标坐标是否在圆形范围内
    /// </summary>
    public class ViewTriggerSphereCenter<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        protected BoundingSphere sphere;
        public BoundingSphere Sphere { get => sphere; }
        public ViewTriggerSphereCenter(InstanceZone zone, Vector3 pos, float r)
            : base(zone)
        {
            this.sphere = new BoundingSphere(pos, r);
        }
        public override void LookUpdate(Vector3 pos)
        {
            this.sphere.Center = pos;
            base.LookUpdate(pos);
        }
        protected override bool IsNearChanged()
        {
            return Zone.IsNearChanged(sphere.Center.X, sphere.Center.Y, sphere.Radius);
        }
        protected override bool TestInView(T o)
        {
            return Collider.Sphere_Touch_BlockBody(this, o, in sphere);
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
            Zone.ForEachNearObjects<ST, T>(sphere.Center.X, sphere.Center.Y, sphere.Radius, input, indexer);
        }
    }

    /// <summary>
    /// 观察目标BlockBody是否在圆形范围内
    /// </summary>
    public class ViewTriggerSphereBody<T> : ViewTriggerSphereCenter<T> where T : InstanceZoneEntity
    {
        public ViewTriggerSphereBody(InstanceZone zone, Vector3 pos, float r)
            : base(zone, pos, r)
        {

        }
        protected override bool TestInView(T o)
        {
            return Collider.Sphere_Touch_HitBody(this, o, in sphere);
        }
    }

    #endregion
    //-------------------------------------------------------------------------------------------
    #region Cylinder
    /// <summary>
    /// 观察目标坐标是否在圆形范围内
    /// </summary>
    public class ViewTriggerCylinderCenter<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        protected VoxelCylinder cylinder;
        public VoxelCylinder Cylinder { get => cylinder; }
        public ViewTriggerCylinderCenter(InstanceZone zone, Vector3 pos, float r, float height)
            : base(zone)
        {
            this.cylinder = new VoxelCylinder(pos, r, height);
        }
        public override void LookUpdate(Vector3 pos)
        {
            this.cylinder.Center = pos;
            base.LookUpdate(pos);
        }
        protected override bool IsNearChanged()
        {
            return Zone.IsNearChanged(cylinder.Center.X, cylinder.Center.Y, cylinder.Radius);
        }
        protected override bool TestInView(T o)
        {
            return Collider.Cylinder_Touch_BlockBody(this, o, in cylinder);
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
            Zone.ForEachNearObjects<ST, T>(cylinder.Center.X, cylinder.Center.Y, cylinder.Radius, input, indexer);
        }
    }

    /// <summary>
    /// 观察目标BlockBody是否在圆形范围内
    /// </summary>
    public class ViewTriggerCylinderBody<T> : ViewTriggerCylinderCenter<T> where T : InstanceZoneEntity
    {
        public ViewTriggerCylinderBody(InstanceZone zone, Vector3 pos, float r, float height)
            : base(zone, pos, r, height)
        {

        }
        protected override bool TestInView(T o)
        {
            return Collider.Cylinder_Touch_HitBody(this, o, in cylinder);
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------
    #region Box AABB
    /// <summary>
    /// 观察目标坐标是否在矩形范围内
    /// </summary>
    public class ViewTriggerBoxCenter<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        private float mSizeW;
        private float mSizeH;
        private float mHeight;
        private BoundingBox box;
        public BoundingBox Box { get => box; }
        public ViewTriggerBoxCenter(InstanceZone zone, Vector3 pos, float w, float h, float height)
            : base(zone)
        {
            this.mSizeW = w;
            this.mSizeH = h;
            this.mHeight = height;
            box.Min.X = pos.X - mSizeW / 2f;
            box.Min.Y = pos.Y - mSizeH / 2f;
            box.Min.Z = pos.Z;
            box.Max.X = box.Min.X + mSizeW;
            box.Max.Y = box.Min.Y + mSizeH;
            box.Max.Z = pos.Z + mHeight;
        }
        public override void LookUpdate(Vector3 pos)
        {
            box.Min.X = pos.X - mSizeW / 2f;
            box.Min.Y = pos.Y - mSizeH / 2f;
            box.Min.Z = pos.Z;
            box.Max.X = box.Min.X + mSizeW;
            box.Max.Y = box.Min.Y + mSizeH;
            box.Max.Z = pos.Z + mHeight;
            base.LookUpdate(pos);
        }
        protected override bool IsNearChanged()
        {
            return Zone.IsNearChanged(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y);
        }
        protected override bool TestInView(T o)
        {
            return Collider.Box_Touch_BlockBody(this, o, in box);
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
            Zone.ForEachNearObjectsRect<ST, T>(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y, input, indexer);
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------
    #region Fan
    /// <summary>
    /// 观察目标坐标是否在扇形范围内
    /// </summary>
    public class ViewTriggerFanCenter<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        protected VoxelFan fan;
        protected float angleRange;
        public float Direction { get; set; }
        public VoxelFan Fan { get => fan; }

        public ViewTriggerFanCenter(InstanceZone zone, Vector3 pos, float range, float angle, float height)
            : base(zone)
        {
            this.angleRange = angle;
            this.fan.Radius = range;
            this.fan.Height = height;
            this.fan.Center = pos;
            this.fan.StartAngle = Direction - angleRange;
            this.fan.EndAngle = Direction + angleRange;
        }
        public void SetLookRange(float r)
        {
            this.fan.Radius = r;
        }
        public override void LookUpdate(Vector3 pos)
        {
            fan.Center = pos;
            fan.StartAngle = Direction - angleRange;
            fan.EndAngle = Direction + angleRange;
            base.LookUpdate(pos);
        }
        protected override bool IsNearChanged()
        {
            return Zone.IsNearChanged(fan.Center.X, fan.Center.Y, fan.Radius);
        }
        protected override bool TestInView(T o)
        {
            return Collider.Fan_Touch_Position(this, o, in fan);
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
            Zone.ForEachNearObjects<ST, T>(fan.Center.X, fan.Center.Y, fan.Radius, input, indexer);
        }
    }

    /// <summary>
    /// 观察目标身体是否在扇形范围内
    /// </summary>
    public class ViewTriggerFanBody<T> : ViewTriggerFanCenter<T> where T : InstanceZoneEntity
    {
        public ViewTriggerFanBody(InstanceZone zone, Vector3 pos, float range, float angle, float height)
            : base(zone, pos, range, angle, height)
        {
        }
        protected override bool TestInView(T o)
        {
            return Collider.Fan_Touch_BlockBody(this, o, in fan);
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------
    #region Strip
    /// <summary>
    /// 观察目标坐标是否在矩形范围内
    /// </summary>
    public class ViewTriggerRectStripCenter<T> : ViewTrigger<T> where T : InstanceZoneEntity
    {
        private Vector3 center;
        private float r;
        private VoxelRectStripe rect;
        public VoxelRectStripe Rect { get => rect; }
        public ViewTriggerRectStripCenter(InstanceZone zone, Vector3 pos, float dir, float w, float h, float height)
            : base(zone)
        {
            this.center = pos;
            this.r = Math.Max(w, h) / 2f;
            this.rect = VoxelRectStripe.InitFromCenter(pos, dir, w, h, height);
        }
        public override void LookUpdate(Vector3 pos)
        {
            base.LookUpdate(pos);
        }
        protected override bool IsNearChanged()
        {
            return Zone.IsNearChanged(center.X, center.Y, r);
        }
        protected override bool TestInView(T o)
        {
            return Collider.RectStripe_Touch_BlockBodye(this, o, in rect);
        }
        protected override void ForEachNearObjects<ST>(ST input, ForEachAction<ST> indexer)
        {
            Zone.ForEachNearObjectsRectWide<ST, T>(rect.LineP.X, rect.LineP.Y, rect.LineQ.X, rect.LineQ.Y, rect.LineRaidus, input, indexer);
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------
}
