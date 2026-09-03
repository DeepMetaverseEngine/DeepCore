using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 可拾取的道具
    /// </summary>
    public class InstanceItem : InstanceZoneEntity, IViewTriggerListener<InstanceUnit>, IZoneItem
    {
        public string Alias { get { return mSyncInfo.Alias; } set { mSyncInfo.Alias = value; } }
        public override float Weight { get { return 0; } }
        public override int TemplateID { get => Info.ID; }
        [Desc("是否连续采集")] public virtual bool ContinuousPick { get => APickable && APickable.ContinuousPick; }
        public ItemTemplate TemplateData => Info;
        public ItemTemplate Template => Info;
        public override bool IsStaticBlock => false;
        public int PickTimes { get { return this.mSyncInfo.PickTimes; } set { this.mSyncInfo.PickTimes = value; } }
        public override string Name { get => mName; }

        private string mName;

        private readonly ItemTemplate mData;
        private readonly bool mClientVisible;
        private readonly InstanceUnit mItemCreater;
        protected SyncItemInfo mSyncInfo;
        protected ViewTrigger<InstanceUnit> mViewTrigger;
        protected TimeExpire<int> mViewTriggerTimer;
        protected TimeExpire<int> mRemovedExpire;
        protected InstanceUnit mMoveToTarget;

        public readonly ItemBuyable ABuyable;
        public readonly ItemInventory AInventory;
        public readonly ItemUseable AUseable;
        public readonly ItemEquip AEquip;
        public readonly ItemPickable APickable;
        public readonly ItemResource AResource;
        public readonly ItemMotion AMotion;
        public InstanceItem(InstanceZone zone, TAddItem add)
            : base(zone, false)
        {
            this.mName = (add.name == null) ? add.template.Name : add.name;
            this.mData = add.template;
            {
                this.ABuyable = mData.Abilities.GetComponentAs<ItemBuyable>();
                this.AInventory = mData.Abilities.GetComponentAs<ItemInventory>();
                this.AUseable = mData.Abilities.GetComponentAs<ItemUseable>();
                this.AEquip = mData.Abilities.GetComponentAs<ItemEquip>();
                this.APickable = mData.Abilities.GetComponentAs<ItemPickable>();
                this.AResource = mData.Abilities.GetComponentAs<ItemResource>();
                this.AMotion = mData.Abilities.GetComponentAs<ItemMotion>();
            }
            this.mSyncInfo = ObjectPool.Alloc<SyncItemInfo>();
            this.mSyncInfo.TemplateID = mData.ID;
            this.mSyncInfo.Force = add.force;
            if (APickable)
            {
                this.mViewTrigger = new ViewTriggerItemPickRange<InstanceUnit>(zone,
                    this.Position,
                    add.template.BodySize);
                this.mViewTrigger.SetListener(this);
                this.mViewTrigger.Enable = false;
                this.mViewTriggerTimer = new TimeExpire<int>(APickable.GotCoolDownTimeMS);
                this.mSyncInfo.PickTimes = APickable.PickTimes;
            }
            this.mSyncInfo.ItemTotalTimeMS = Info.LifeTimeMS;
            if (Info.LifeTimeMS > 0)
            {
                this.mRemovedExpire = new TimeExpire<int>(Info.LifeTimeMS);
            }
            this.Alias = add.alias;
            this.Force = add.force;
            this.mClientVisible = add.template.ClientVisible;
            this.mItemCreater = add.creater;
        }

        protected override void Disposing()
        {
            base.Disposing();
            mViewTrigger?.Dispose();
            mMoveToTarget = null;
            this.mSyncInfo?.Dispose();
            this.mSyncInfo = null;
        }

        public ItemTemplate Info
        {
            get { return mData; }
        }

        public virtual byte Force { get; protected set; }

        public override bool IntersectMap
        {
            get { return false; }
        }

        public override bool IntersectObj
        {
            get { return false; }
        }

        public override bool Moveable
        {
            get { return false; }
        }

        public override float BodyBlockSize
        {
            get { return mData.BodySize; }
        }

        override public float BodyHitSize
        {
            get { return mData.BodySize; }
        }

        public override float BodyHeight
        {
            get { return mData.BodyHeight; }
        }

        public override bool ClientVisible
        {
            get { return mClientVisible; }
        }

        public InstanceUnit ItemCreater
        {
            get { return mItemCreater; }
        }

        public virtual float ExpireTimeMS
        {
            get
            {
                //                 if (Info.NoLifeTime)
                //                 {
                //                     return 0;
                //                 }
                if (mRemovedExpire != null)
                {
                    return (float)(mRemovedExpire.TotalTimeMS - mRemovedExpire.PassTimeMS);
                }
                return 0;
            }
        }

        public double LifeTimeMS
        {
            get
            {
                return this.mRemovedExpire == null ? 0 : this.mRemovedExpire.TotalTimeMS;
            }
            set
            {
                if (value > 0)
                {
                    this.mRemovedExpire?.Dispose();
                    this.mRemovedExpire = new TimeExpire<int>(value);
                }
                else
                {
                    this.mRemovedExpire?.Dispose();
                    this.mRemovedExpire = null;
                }
            }
        }
        public bool IsMoveToTarget { get => mMoveToTarget; }
        public bool IsPickingUnit
        {
            get
            {
                return (
                    currentPicking != null
                     && currentPicking.IsActive
                     && currentPicking.CurrentState is InstanceUnit.StatePickObject pick
                     && pick.Target == this
                    );
            }
        }

        public override SyncObjectInfo GenSyncInfo(bool net)
        {
            return GenSyncItemInfo(net);
        }

        public SyncItemInfo GenSyncItemInfo(bool net = false)
        {
            mSyncInfo.ItemExpireTimeMS = this.ExpireTimeMS;
            mSyncInfo.Alias = this.Alias;
            return mSyncInfo;
        }


        protected override void onAdded()
        {
            this.mSyncInfo.pos = this.Position;
            this.mSyncInfo.ObjectID = base.ID;
            this.mSyncInfo.direction = Direction;
            this.mSyncInfo.body_direction = BodyDirection;
            this.mSyncInfo.Alias = this.Alias;
            this.mSyncInfo.Name = this.Name;
            this.mSyncInfo.PickTimes = this.PickTimes;
            this.mPos.Gravity = Parent.Gravity;
        }

        protected override void onRemoved()
        {
            mMoveToTarget = null;
        }

        protected override void onUpdate()
        {
            if (mMoveToTarget != null)
            {
                mViewTrigger.Enable = false;
                var pos = this.mPos.Position;
                var distance = MotionHelper.GetDistance(Zone.UpdateIntervalMS, AMotion.MotionSpeedSEC);
                if (VectorHelper.MoveTo3D(ref pos, mMoveToTarget.Position, distance))
                {
                    if (AMotion.MoveFinishEffect != null)
                    {
                        Parent.PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ID, this.Position, Direction, AMotion.MoveFinishEffect));
                    }
                    if (AMotion.MoveTargetEffect != null)
                    {
                        mMoveToTarget.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, AMotion.MoveTargetEffect));
                    }
                    Parent.RemoveObject(this);
                    return;
                }
                else
                {
                    this.mPos.Transport(in pos);
                }
                return;
            }
            if (checkUpdate())
            {
                if (IsPaused)
                {
                    return;
                }
                // 长时间没人捡取
                if (mRemovedExpire != null && mRemovedExpire.Update(Parent.UpdateIntervalMS))
                {
                    Parent.RemoveObject(this);
                    return;
                }

                // 拾取触发器：附近无玩家时跳过，避免无效的空间扫描
                if (HasNearPlayer)
                {
                    if (mViewTriggerTimer != null && mViewTriggerTimer.Update(Parent.UpdateIntervalMS))
                    {
                        mViewTrigger.Enable = true;
                        mViewTrigger.LookUpdate(this.Position);
                    }
                }
            }
            mPos.Update(Parent.UpdateIntervalMS);
        }
        protected virtual bool checkUpdate()
        {
            return !IsPaused;
        }
        protected virtual void BeginRemove(InstanceUnit picker)
        {
            mViewTrigger.Enable = false;
            if (AMotion != null)
            {
                this.mMoveToTarget = picker;
            }
            else
            {
                Parent.RemoveObject(this);
            }
        }
        public override Vector3 GetRandomPos()
        {
            var random = this.RandomN;
            float r = (float)(random.NextFloat() * this.BodySize);
            float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
            float x = (float)(this.X + Math.Cos(a) * r);
            float y = (float)(this.Y + Math.Sin(a) * r);
            return new Vector3(x, y, this.Z);
        }
        public virtual bool IsPickable(InstanceUnit u)
        {
            if (!APickable)
            {
                return false;
            }
            if (mViewTriggerTimer != null && !mViewTriggerTimer.IsEnd)
            {
                return false;
            }

            if (!HasPickTimes(APickable))
                return false;
            if (!APickable.TogetherPicking)
            {
                if (IsPickingUnit)
                {
                    return false;
                }
                else
                {
                    currentPicking = null;
                }
            }
            if (APickable.DropAcceptUnitTypes != null)
            {
                foreach (var accept in APickable.DropAcceptUnitTypes)
                {
                    if (accept != UnitType.TYPE_NA && u.UType != accept)
                    {
                        return false;
                    }
                }
            }
            if (APickable.DropDenyUnitTypes != null)
            {
                foreach (var deny in APickable.DropDenyUnitTypes)
                {
                    if (deny != UnitType.TYPE_NA && u.UType == deny)
                    {
                        return false;
                    }
                }
            }

            if ((APickable.PlayerOnly) && (!u.IsPlayer))
            {
                return false;
            }

            if (!this.Enable)
            {
                return false;
            }

            if ((APickable.DropForAll) || Force == u.Force)
            {
                if (tryPickItem(u) && Parent.cb_unitTryPickItem(u, this))
                {
                    return true;
                }
            }

            return false;
        }


        protected virtual bool Cb_unitTryPickItem(InstanceUnit u, InstanceItem item)
        {
            return Parent.cb_unitTryPickItem(u, item);
        }

        protected virtual void Cb_unitFinishPickItem(InstanceUnit u, InstanceItem item)
        {
            Parent.cb_unitFinishPickItem(u, item);
        }

        protected virtual bool DoGotInstanceItem(InstanceUnit u, InstanceItem item)
        {
            return u.doGotInstanceItem(item);
        }

        private InstanceUnit currentPicking = null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>是否有新状态</returns>
        public virtual bool PickItem(InstanceUnit u)
        {
            u.CurrentTargetID = this.ID;
            if (IsPickable(u))
            {
                if (!APickable.CheckItemBodySize || Parent.TouchObject2(u, this))
                {
                    if (tryPickItem(u) && Parent.cb_unitTryPickItem(u, this))
                    {
                        OnPickBegin(u);
                        if (APickable.PickTimeMS > 0)
                        {
                            if (u.StartPickProgressObject(this, APickable.PickTimeMS, static (unit, cancel, pickable, status) =>
                            {
                                if (pickable is InstanceItem item)
                                {
                                    item.currentPicking = null;
                                    if (!cancel)
                                    {
                                        item.FinishPickItem(unit, item);
                                    }
                                }
                                return true;
                            }, this))
                            {
                                if (Info.PickingEffect != null)
                                {
                                    u.PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(u.ID, Info.PickingEffect));
                                }
                                currentPicking = u;
                                return true;
                            }
                        }
                        else
                        {
                            FinishPickItem(u, this);
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        private bool FinishPickItem(InstanceUnit u, InstanceZoneObject obj)
        {
            bool done = true;
            if (this.Enable)
            {
                if (u.doGotInstanceItem(this) && ReducePickTimes(APickable))
                {
                    PickFinish(u);
                    Parent.cb_unitFinishPickItem(u, this);
                    if (APickable.GotEffectSelf != null)
                    {
                        Parent.PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ID, this.Position, Direction, APickable.GotEffectSelf));
                    }

                    u.PostEvent(ObjectPool.Alloc<UnitGotZoneItemEvent>().Init(u.ID, this.ID));

                    if (!HasPickTimes(APickable))
                    {
                        mViewTrigger.Enable = false;
                        Parent.RemoveObject(this);
                    }
                }
                //扔有次数时继续拾取.
                if (this.PickTimes > 0 && ContinuousPick)
                {
                    done = false;
                }
            }
            return done;
        }

        public virtual bool DirectPickItem(InstanceUnit u)
        {
            if (IsPickable(u))
            {
                if (u.doGotInstanceItem(this) && ReducePickTimes(APickable))
                {
                    PickFinish(u);
                    Parent.cb_unitFinishPickItem(u, this);
                    if (APickable && APickable.GotEffectSelf != null)
                    {
                        Parent.PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ID, this.Position, Direction, APickable.GotEffectSelf));
                    }
                    u.PostEvent(ObjectPool.Alloc<UnitGotZoneItemEvent>().Init(u.ID, this.ID));
                    if (!HasPickTimes(APickable))
                    {
                        BeginRemove(u);
                    }
                }
                return true;
            }
            return false;
        }

        protected void AutoPickItem(InstanceUnit obj)
        {
            if (APickable && APickable.PickTimeMS <= 0)
            {
                DirectPickItem(obj);
            }
        }


        protected bool tryPickItem(InstanceUnit unit)
        {
            if (!Parent.Formula.IsVisibleAOI(unit, this))
            {
                return false;
            }

            bool ret = true;
            if (mTryPickItem != null)
            {
                foreach (TryPickItem trypick in mTryPickItem.GetInvocationList())
                {
                    if (!trypick.Invoke(this, unit))
                    {
                        ret = false;
                    }
                }
            }

            return ret;
        }

        public bool HasPickTimes(ItemPickable drop)
        {
            if (!drop.RemoveOnFinishPick) return true;

            if (PickTimes == 0)
                return false;

            return true;
        }

        public bool RemoveOnFinish()
        {
            if (!APickable || !APickable.RemoveOnFinishPick) return false;
            return !HasPickTimes(APickable);
        }

        protected void PickFinish(InstanceUnit u)
        {
            OnPickEnd(u);
        }

        protected bool ReducePickTimes(ItemPickable drop)
        {
            if (!drop.RemoveOnFinishPick) return true;
            if (PickTimes > 0)
            {
                PickTimes--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 拾取开始
        /// </summary>
        /// <param name="u"></param>
        protected virtual void OnPickBegin(InstanceUnit u)
        {
        }

        /// <summary>
        /// 拾取结束
        /// </summary>
        /// <param name="u"></param>
        protected virtual void OnPickEnd(InstanceUnit u)
        {
        }
        public virtual bool HasNearPlayer => this.SpaceUserTag.HasNearPlayer;


        #region _ViewTrigger_

        public class ViewTriggerItemPickRange<T> : ViewTriggerSphereBody<T> where T : InstanceUnit
        {
            public ViewTriggerItemPickRange(InstanceZone zone, Vector3 pos, float r)
                : base(zone, pos, r)
            {
            }

            protected override bool TestInView(T o)
            {
                return new Geometry.BoundingSphere(o.Position, o.GetPickRange()).Intersects(in sphere);
            }
        }
        bool IViewTriggerListener<InstanceUnit>.Select(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            return TrySelect(obj);
        }
        void IViewTriggerListener<InstanceUnit>.OnObjectEnterView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            OnObjectEnterItemView(src, obj);
        }
        void IViewTriggerListener<InstanceUnit>.OnObjectLeaveView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            OnObjectLeaveItemView(src, obj);
        }



        protected virtual void OnObjectEnterItemView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {
            AutoPickItem(obj);
        }

        protected virtual void OnObjectLeaveItemView(ViewTrigger<InstanceUnit> src, InstanceUnit obj)
        {

        }

        protected virtual bool TrySelect(InstanceUnit obj)
        {
            return true;
        }

        #endregion


        #region _Delegate_

        /// <summary>
        /// 单位尝试检取道具监听，
        /// 返回False禁止检取
        /// </summary>
        /// <returns></returns>
        public delegate bool TryPickItem(InstanceItem item, InstanceUnit unit);

        private TryPickItem mTryPickItem;

        public event TryPickItem OnTryPickItem
        {
            add { mTryPickItem += value; }
            remove { mTryPickItem -= value; }
        }

        protected override void ClearEvents()
        {
            base.ClearEvents();
            this.mTryPickItem = null;
        }

        #endregion
    }
}