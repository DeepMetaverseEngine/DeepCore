using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data;

namespace DeepCore.Game3D.Slave.Layer
{
    public class LayerSpell : LayerZoneObject, IZoneSpell
    {
        //----------------------------------------------------------------------------------------------------------------------------

        internal SpellTemplate _Info;
        internal LayerZoneObject _Sender;
        internal LayerUnit _Launcher;
        internal Geometry.Vector3? _TargetPos;
        private LayerUnit m_Target;
        private object _EventSender;
        private Geometry.Vector3 mStartPos = new Geometry.Vector3();
        private Geometry.Vector3? mDistancePos;
        private Geometry.Vector3? mStartNormal;
        //private Polar3 mBindingOffset;
        private float mSizeLimit, mDisplaySize;
        private float mDistanceLimit, mDisplayDistance;
        //private float mStartDirection;
        private float mStartSpeed;
        private double mPassTimeMS;
        //        private float mSpeed;
        //        private float mSpeedZ;
        //        private float mDistanceSpeed;
        //        private float mRotateSpeed;
        private AddSpellEvent mAddEvent;
        private readonly VectorObject3 mLocalPos = new VectorObject3();
        private Geometry.Vector3 mPrvePos = new Geometry.Vector3();
        private readonly PopupKeyFrames<SpellTemplate.KeyFrame> mKeyFrames = new();
        private readonly TimeInterval mHitIntervalTicker = new();
        private ISpellMotion motion;
        public LayerUnit Target
        {
            get => m_Target;
            set
            {
                if (m_Target != null) m_Target.Release();
                m_Target = value;
                if (m_Target != null) m_Target.Retain();
            }
        }
        public IZoneSpell HostObject => _EventSender as IZoneSpell;
        //----------------------------------------------------------------------------------------------------------------------------
        public static LayerSpell Alloc(SpellTemplate info, SyncSpellInfo syn, LayerZone parent, AddSpellEvent add)
        {
            return parent.ObjectPool.AllocAutoRelease<LayerSpell>().Init(info, syn, parent, add);
        }
        protected LayerSpell Init(SpellTemplate spell, SyncSpellInfo syn, LayerZone parent, AddSpellEvent add)
        {
            base.Init(syn.ObjectID, parent);
            this.IsHitted = false;
            this.IsFinish = false;
            this._EventSender = add?.sender;
            this._Info = add?.template ?? spell;
            this.mLocalPos.X = this.mStartPos.X = this.mRemotePos.X = syn.pos.X;
            this.mLocalPos.Y = this.mStartPos.Y = this.mRemotePos.Y = syn.pos.Y;
            this.mLocalPos.Z = this.mStartPos.Z = this.mRemotePos.Z = syn.pos.Z;
            //this.mStartDirection = syn.direction;
            this.mStartNormal = add?.normal;
            this.mDirection.ForceSync(syn.direction, syn.body_direction);
            this.mDistanceLimit = this.mDisplayDistance = this._Info.Distance;
            this.mSizeLimit = this.mDisplaySize = this._Info.BodySize;
            this.mPassTimeMS = 0;
            this.mAddEvent = add;
            this.mKeyFrames.Clear();
            this.mKeyFrames.AddRange(this._Info.KeyFrames);
            this.mHitIntervalTicker.Init(this._Info.HitIntervalMS);
            this.mStartSpeed = this._Info.MSpeedSEC;
            if (add != null)
            {
                this.mStartSpeed = add.startSpeed;
                this.IsFromSpellMagnitude = add.IsSpellMagnitude;
            }
            else if (syn.HasSpeed)
            {
                this.mStartSpeed = syn.CurSpeed;
            }
            this.motion = parent.ObjectPool.AllocOrCreateAutoRelease(this, static (st, pool) => ZoneDataFactory.Factory.CreateSpellMotion(st), this);
            this.motion.Init(this);
            if (add != null)
            {
                var e = add;
                this._Sender = parent.GetObject(e.sender_unit_id);
                this._Sender?.Retain();
                this._Launcher = parent.GetUnit(e.launcher_unit_id);
                this._Launcher?.Retain();
                this.Target = parent.GetUnit(e.target_obj_id);
                this._TargetPos = e.target_pos;
            }
            return this;
        }

        protected override void Disposing()
        {
            base.Disposing();
            this.IsHitted = false;
            this.IsFinish = false;
            this._Info = default;
            this._Sender?.Release();
            this._Sender = default;
            this._Launcher?.Release();
            this._Launcher = default;
            this.m_Target?.Release();
            this.m_Target = default;
            this._TargetPos = default;
            this._EventSender = default;
            this.mStartPos = default;
            this.mDistancePos = default;
            this.mStartNormal = default;
            this.mLocalPos.Value = default;
            this.mSizeLimit = default;
            this.mDisplaySize = default;
            this.mDistanceLimit = default;
            this.mDisplayDistance = default;
            //this.mStartDirection = default;
            this.mPassTimeMS = default;
            //             this.mSpeed = default;
            //             this.mSpeedZ = default;
            this.mAddEvent = default;
            this.mKeyFrames.Clear();
            this.mHitIntervalTicker.Dispose();

            this.motion?.Dispose();
            this.motion = null;
        }
        public bool IsFromSpellMagnitude { get; private set; }
        //----------------------------------------------------------------------------------------------
        #region IZoneSpell

        ISpellMotion IZoneSpell.Motion => this.motion;
        SpellTemplate IZoneSpell.Template => this.Info;
        LaunchSpell IZoneSpell.LaunchData => mAddEvent?.LaunchData;
        IZoneObject IZoneSpell.Sender => this.Sender;
        IZoneUnit IZoneSpell.LauncherUnit => this._Launcher;
        bool IZoneSpell.IsNextChain => (mAddEvent != null && mAddEvent.senderChain);
        double IZoneSpell.PassTimeMS => this.PassTimeMS;
        bool IZoneSpell.IsForceSync => (Parent.ActorSyncMode == SyncMode.ForceByServer);
        Vector3 IZoneSpell.RemotePosition => this.RemotePos;
        Vector3 IZoneSpell.PrevPos => this.mPrvePos;

        //---------------------------------------------------------------------
        Vector3? IZoneSpell.StartNormal { get => mStartNormal; set => mStartNormal = value; }
        float IZoneSpell.StartSpeed { get => mStartSpeed; set => mStartSpeed = value; }
        Vector3? IZoneSpell.RayTouchPoint { get => mDistancePos; set => mDistancePos = value; }
        float IZoneSpell.SpellDistance { get => mDistanceLimit; set => mDistanceLimit = value; }
        float IZoneSpell.SpellSize { get => mSizeLimit; set => mSizeLimit = value; }
        float IZoneSpell.SpellDisplayDistance { get => mDisplayDistance; set => mDisplayDistance = value; }
        float IZoneSpell.SpellDisplaySize { get => mDisplaySize; set => mDisplaySize = value; }

        Vector3? IZoneSpell.TargetPos { get => _TargetPos; set => _TargetPos = value; }
        IZoneUnit IZoneSpell.TargetUnit { get => Target; set => Target = value as LayerUnit; }
        void IZoneSpell.FaceTo(float dir) => base.mDirection.SyncFace(dir);
        void IZoneSpell.FaceTo(Vector3 t)
        {
            if (this.X == t.X && this.Y == t.Y) return;
            var d = (float)(Math.Atan2(t.Y - this.Y, t.X - this.X));
            base.mDirection.SyncFace(d);
        }
        void IZoneSpell.Turn(float dir) => base.mDirection.TurnFace(dir);
        void IZoneSpell.SetPosition(Vector3 position) => mLocalPos.FromGeometry3(position);
        //---------------------------------------------------------------------
        bool IZoneSpell.TrySeekAttackable(float range, bool postEvent, out IZoneUnit target)
        {
            target = null;
            return target != null;
        }
        bool IZoneSpell.TryRayCastTouchEndUnit(VoxelStripe ray, out IZoneUnit target)
        {
            using (var list = ObjectPool.AllocList<LayerUnit>())
            {
                Parent.ForEachNearObjectsRectPredicate(ray.LineP.X, ray.LineP.Y, ray.LineQ.X, ray.LineQ.Y, Parent, (LayerZone st, LayerUnit zu) =>
                {
                    if (Parent.IsAttackable(Launcher, zu, Info.ExpectTarget))
                    {
                        //if ((CMath.intersectLineRound(this.X, this.Y, p1.X, p1.Y, zu.X, zu.Y, d_width + zu.BodyHitSize)))
                        if (zu.VoxelBody.Intersects(in ray))
                        {
                            list.Add(zu);
                        }
                    }
                    return false;
                });
                if (list.Count > 0)
                {
                    list.Sort(new Helper.ObjectBodySorterNearest<ILayerZoneEntity>(this.Position, 0));
                    target = list[0];
                    return true;
                }
            }
            target = null;
            return false;
        }
        bool IZoneSpell.CheckBinding(IZoneObject target) => true;
        bool IZoneSpell.CheckRemoveOnBindingSkillOver(IZoneUnit target) => false;
        void IZoneSpell.Finish(bool destoryImmediately) { }
        public bool IsHitted { get; private set; }
        public bool IsFinish { get; private set; }
        #endregion
        //----------------------------------------------------------------------------------------------       
        public SpellTemplate Info => _Info;
        public override int TemplateID => Info.ID;
        public override string Name { get; } = string.Empty;
        public override string DisplayName { get { return Info.Name; } }
        public override Geometry.Vector3 Position => mLocalPos.ToGeometry3();
        public override float X { get { return mLocalPos.X; } }
        public override float Y { get { return mLocalPos.Y; } }
        public override float Z { get { return mLocalPos.Z; } }
        public override float BodyBlockSize { get { return mDisplaySize; } }
        public override float BodyHeight { get { return Info.BodyHeight; } }
        public Geometry.Vector3 StartPos { get { return mStartPos; } }
        public Geometry.Vector3 DistancePos { get { return mDistancePos.HasValue ? mDistancePos.Value : this.Position; } }
        public override float WaistZ { get => this.Z; }
        public override float TopZ { get => this.Z; }
        public override Geometry.Vector3 WaistPosition { get => this.Position; }
        public float BodySize { get { return mDisplaySize; } }
        public float Distance { get { return mDisplayDistance; } }
        public double PassTimeMS { get { return mPassTimeMS; } protected set { mPassTimeMS = value; } }
        public LayerZoneObject Sender => _Sender;
        public LayerUnit Launcher => _Launcher;
        public Geometry.Vector3? TargetPos => _TargetPos;
        public object EventSender { get => _EventSender; }
        public float ResourceScale
        {
            get
            {
                switch (Info.BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToTargetPos:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        return 1f;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        return Distance / Info.Distance;
                    default:
                        return BodySize / Info.BodySize;
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        protected internal override void OnAdded()
        {
            this.mPrvePos = mLocalPos.ToGeometry3();
            base.OnAdded();
            if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            {
                this.motion.OnAdded();
                this.mStartPos = mLocalPos.ToGeometry3();
            }
            if (Info.TargetEffect != null)
            {
                if (Target != null)
                {
                    Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(Target.ObjectID, Info.TargetEffect));
                }
                else if (TargetPos != null)
                {
                    Parent.PreQueueEvent(ObjectPool.Alloc<AddEffectEvent>().Init(ObjectID, TargetPos.Value, base.mDirection.Direction, Info.TargetEffect));
                }
            }
        }

        public override void ForceSyncPos(in Geometry.Vector3 pos)
        {
            this.mLocalPos.X = this.mRemotePos.X = pos.X;
            this.mLocalPos.Y = this.mRemotePos.Y = pos.Y;
            this.mLocalPos.Z = this.mRemotePos.Z = pos.Z;
        }
        internal protected override void DoEvent(ObjectNotify e)
        {
            if (e is SpellLockTargetEvent)
            {
                doSpellLockTargetEvent(e as SpellLockTargetEvent);
            }
            else if (e is SpellSyncEvent)
            {
                doSpellSyncEvent(e as SpellSyncEvent);
            }
            else if (e is ObjectForceSyncPosEvent)
            {
                var oe = e as ObjectForceSyncPosEvent;
                this.InternalSyncObject(oe);
            }
        }
        private void doSpellLockTargetEvent(SpellLockTargetEvent e)
        {
            this.Target = Parent.GetUnit(e.target_obj_id);
            if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            {
                switch (Info.MType)
                {
                    case SpellTemplate.MotionType.SeekerSelectTarget:
                        if (Target != null)
                        {
                            this.mLocalPos.X = Target.X;
                            this.mLocalPos.Y = Target.Y;
                            this.mLocalPos.Z = Target.Z;
                        }
                        break;
                }
            }
        }
        private void doSpellSyncEvent(SpellSyncEvent e)
        {
            this.IsHitted = e.IsHit;
            this.IsFinish = e.IsFin;
            this.mLocalPos.Value = this.mRemotePos.Value = e.pos.Value;
            this.PassTimeMS = e.passTimeMS;
            this.motion.CurrentSpeed = e.speed;
            base.mDirection.SyncFace(e.dir);
        }
        protected override void UpdateAI()
        {

        }

        protected override void Update()
        {
            this.mPrvePos = mLocalPos.ToGeometry3();
            var intervalMS = Parent.CurrentIntervalMS;
            mPassTimeMS += intervalMS;
            if (HostObject is IZoneSpell hostSpell)
            {
                this.mLocalPos.Value = this.mRemotePos.Value = hostSpell.Position.Value;
                this.mDirection.ForceSync(hostSpell.Direction, hostSpell.BodyDirection);

                this.mStartNormal = hostSpell.StartNormal;
                this.mStartSpeed = hostSpell.StartSpeed;
                this.mDistancePos = hostSpell.RayTouchPoint;
                this.mDistanceLimit = hostSpell.SpellDistance;
                this.mSizeLimit = hostSpell.SpellSize;
                this.mDisplayDistance = hostSpell.SpellDisplayDistance;
                this.mDisplaySize = hostSpell.SpellDisplaySize;
            }
            else
            {
                this.motion.UpdateMotion(intervalMS);
            }
            updateKeyFrames();
        }

        public override void SyncPos(UnitSyncPos pos)
        {
            if (HostObject is IZoneSpell hostSpell)
            {
                return;
            }
            base.SyncPos(pos);
            if (Parent.ActorSyncMode == SyncMode.ForceByServer)
            {
                if (pos.HasModifer(UnitSyncModifer.Posistion))
                {
                    mLocalPos.X = pos.X;
                    mLocalPos.Y = pos.Y;
                    mLocalPos.Z = pos.Z;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------



        #region _UpdateKeyFrames_


        /// <summary>
        /// 更新范围检测以及关键帧
        /// </summary>
        private void updateKeyFrames()
        {
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Missile:
                case SpellTemplate.MotionType.SeekerMissile:
                case SpellTemplate.MotionType.Cannon:
                case SpellTemplate.MotionType.Chain:
                    break;
                default:
                    if (Info.BodyShape == SpellTemplate.Shape.LineToTargetPos)
                    {
                        if (TargetPos != null)
                        {
                            updateKeyFramesRanged();
                        }
                    }
                    else if (Info.BodyShape == SpellTemplate.Shape.LineToTarget ||
                        Info.BodyShape == SpellTemplate.Shape.LineToSender ||
                        Info.BodyShape == SpellTemplate.Shape.LineToStart)
                    {
                        if (Target != null)
                        {

                        }
                        else if (TargetPos != null)
                        {
                            updateKeyFramesRanged();
                        }
                    }
                    else
                    {
                        updateKeyFramesRanged();
                    }
                    break;
            }
        }

        private void updateKeyFramesRanged()
        {
            using (var kfs = this.ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames(PassTimeMS, kfs);
                bool is_interval_test = mHitIntervalTicker.Update(Parent.CurrentIntervalMS);
                if (kfs_count > 0)
                {
                    for (int i = 0; i < kfs.Count; i++)
                    {
                        if (kfs[i].Effect != null)
                        {
                            Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(ObjectID, kfs[i].Effect));
                        }
                    }
                }
                if (Info.HitOnExplosion)
                {
                }
                else if (Info.HitIntervalMS == 0)
                {
                }
                else if (is_interval_test)
                {
                    if (Info.HitIntervalKeyFrame != null && Info.HitIntervalKeyFrame.Effect != null)
                    {
                        Parent.PreQueueEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(ObjectID, Info.HitIntervalKeyFrame.Effect));
                    }
                }
            }
        }

        #endregion

        //---------------------------------------------------------------------------------------------------
    }
}
