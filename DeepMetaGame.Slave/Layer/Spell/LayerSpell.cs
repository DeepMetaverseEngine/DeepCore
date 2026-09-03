using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Slave.Layer
{
    public class LayerSpell : LayerZoneObject
    {
        //----------------------------------------------------------------------------------------------------------------------------

        internal SpellTemplate _Info;
        internal LayerZoneObject _Sender;
        internal LayerUnit _Launcher;
        internal Geometry.Vector3? _TargetPos;
        internal LayerUnit _Target;
        private object _EventSender;
        private Geometry.Vector3 mStartPos = new Geometry.Vector3();
        private Geometry.Vector3 mDistancePos = new Geometry.Vector3();
        private Geometry.Vector3? mStartNormal;
        //private Polar3 mBindingOffset;
        private float mSizeLimit, mDisplaySize;
        private float mDistanceLimit, mDisplayDistance;
        private float mStartDirection;
        private float mPassTimeMS;
        private float mSpeed;
        private float mSpeedZ;
        private float mDistanceSpeed;
        private float mRotateSpeed;
        private AddSpellEvent mAddEvent;
        private readonly VectorObject3 mLocalPos = new VectorObject3();
        private readonly PopupKeyFrames<SpellTemplate.KeyFrame> mKeyFrames = new();
        private readonly TimeInterval mHitIntervalTicker = new();

        public static LayerSpell Alloc(SpellTemplate info, SyncSpellInfo syn, LayerZone parent, AddSpellEvent add)
        {
            return parent.ObjectPool.AllocAutoRelease<LayerSpell>().Init(info, syn, parent, add);
        }
        protected LayerSpell Init(SpellTemplate spell, SyncSpellInfo syn, LayerZone parent, AddSpellEvent add)
        {
            base.Init(syn.ObjectID, parent);
            this._EventSender = add?.sender;
            this._Info = add?.template ?? spell;
            this.mLocalPos.X = this.mStartPos.X = this.mRemotePos.X = syn.pos.X;
            this.mLocalPos.Y = this.mStartPos.Y = this.mRemotePos.Y = syn.pos.Y;
            this.mLocalPos.Z = this.mStartPos.Z = this.mRemotePos.Z = syn.pos.Z;
            this.mStartDirection = syn.direction;
            this.mStartNormal = add?.normal;
            this.mDirection.ForceSync(syn.direction, syn.body_direction);
            this.mDistanceLimit = this.mDisplayDistance = this._Info.Distance;
            this.mSizeLimit = this.mDisplaySize = this._Info.BodySize;
            this.mPassTimeMS = 0;
            this.mAddEvent = add;
            this.mKeyFrames.Clear();
            this.mKeyFrames.AddRange(this._Info.KeyFrames);
            this.mHitIntervalTicker.Init(this._Info.HitIntervalMS);
            this.mSpeed = this._Info.MSpeedSEC;
            this.mDistanceSpeed = 0;
            this.mRotateSpeed = this._Info.RotateSpeedSEC;
            if (syn.HasSpeed)
            {
                this.mSpeed = syn.CurSpeed;
            }
            return this;
        }

        protected override void Disposing()
        {
            base.Disposing();
            this._Info = default;
            this._Sender = default;
            this._Launcher = default;
            this._TargetPos = default;
            this._Target = default;
            this._EventSender = default;
            this.mStartPos = default;
            this.mDistancePos = default;
            this.mStartNormal = default;
            this.mLocalPos.Value = default;
            this.mSizeLimit = default;
            this.mDisplaySize = default;
            this.mDistanceLimit = default;
            this.mDisplayDistance = default;
            this.mStartDirection = default;
            this.mPassTimeMS = default;
            this.mSpeed = default;
            this.mSpeedZ = default;
            this.mAddEvent = default;
            this.mKeyFrames.Clear();
            this.mHitIntervalTicker.Dispose();
        }


        //----------------------------------------------------------------------------------------------------------------------------

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
        public Geometry.Vector3 DistancePos { get { return mDistancePos; } }
        public override float WaistZ { get => this.Z; }
        public override float TopZ { get => this.Z; }
        public override Geometry.Vector3 WaistPosition { get => this.Position; }
        public float BodySize { get { return mDisplaySize; } }
        public float Distance { get { return mDisplayDistance; } }
        public float PassTimeMS { get { return mPassTimeMS; } }
        public LayerZoneObject Sender => _Sender;
        public LayerUnit Launcher => _Launcher;
        public LayerUnit Target => _Target;
        public Geometry.Vector3? TargetPos => _TargetPos;
        public object EventSender { get => _EventSender; }

        protected internal override void OnAdded()
        {
            base.OnAdded();
            if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            {
                {
                    float radius = 0;
                    float angle = 0;
                    float height = 0;
                    if ((Sender is LayerUnit sender) && sender.ASkill)
                    {
                        radius = sender.ASkill.LaunchSpellRadius;
                        angle = sender.ASkill.LaunchSpellAngle;
                        height = sender.ASkill.LaunchSpellHeight;
                    }
                    if (mAddEvent != null && !mAddEvent.LaunchData.FromUnitBody)
                    {
                        radius = mAddEvent.LaunchData.LaunchSpellRadius;
                        angle = mAddEvent.LaunchData.LaunchSpellAngle;
                        height = mAddEvent.LaunchData.LaunchSpellHeight;
                    }
                }
                switch (Info.MType)
                {
                    case SpellTemplate.MotionType.Straight:
                        if (_TargetPos == null && Target != null)
                        {
                            _TargetPos = Target.WaistPosition;
                        }
                        //                         if (TargetPos.HasValue)
                        //                         {
                        //                             var hroz = Geometry.VectorHelper.Polar(mStartDirection, 1);
                        //                             hroz.Normalize();
                        //                             this.mStartNormal = Vector3.Normalize(TargetPos.Value - mStartPos);
                        //                             this.mStartNormal = new Vector3(hroz.X, hroz.Y, mStartNormal.Value.Z);
                        //                         }
                        break;
                    case SpellTemplate.MotionType.Cannon:
                        if (_TargetPos.HasValue)
                        {
                            MoveHelper.CalculateSpellLaunchAngle(Info, in mStartPos, TargetPos.Value, CFG.GLOBAL_GRAVITY,
                                out var muzzleAngle,
                                out mStartDirection,
                                out mSpeed,
                                out mSpeedZ);
                        }
                        break;
                    case SpellTemplate.MotionType.Chain:
                        if (Sender != null)
                        {
                            if (mAddEvent != null && mAddEvent.senderChain)
                            {
                                mStartPos = GetBindingPos(Sender);
                            }
                            else
                            {
                                //mBindingOffset = new Polar3(angle, radius, height);
                                mStartPos = GetBindingPos(Sender);
                            }
                        }
                        break;
                    case SpellTemplate.MotionType.AOE_Binding:
                    case SpellTemplate.MotionType.Binding:
                        if (Sender != null)
                        {
                            //mBindingOffset = new Polar3(angle, radius, height);
                            mStartPos = GetBindingPos(Sender);
                        }
                        break;
                    case SpellTemplate.MotionType.AOE_BindingTarget:
                    case SpellTemplate.MotionType.BindingTarget:
                        if (Target != null)
                        {
                            //mBindingOffset = new Polar3(angle, radius, height);
                            mStartPos = GetBindingPos(Target);
                        }
                        break;
                }
                this.mLocalPos.X = this.mRemotePos.X = this.mStartPos.X;
                this.mLocalPos.Y = this.mRemotePos.Y = this.mStartPos.Y;
                this.mLocalPos.Z = this.mRemotePos.Z = this.mStartPos.Z;
                base.mDirection.ForceSync(this.mStartDirection, this.mStartDirection);
            }
            if (Info.TargetEffect != null)
            {
                if (Target != null)
                {
                    Parent.PreQueueEvent(new UnitEffectEvent(Target.ObjectID, Info.TargetEffect));
                }
                else if (TargetPos != null)
                {
                    Parent.PreQueueEvent(new AddEffectEvent(ObjectID, TargetPos.Value, base.mDirection.Direction, Info.TargetEffect));
                }
            }
        }

        public Geometry.Vector3 GetBindingPos(LayerObject target)
        {
            var bindingP = target.WaistPosition;
            switch (Info.BodyVoxelAnchor)
            {
                case VoxelAnchor.Ceiling:
                    bindingP.Z = target.TopZ + Info.BindingOffsetZ;
                    break;
                case VoxelAnchor.Floating:
                    bindingP.Z = target.WaistZ + Info.BindingOffsetZ;
                    break;
                case VoxelAnchor.Flooring:
                    bindingP.Z = target.Z + Info.BindingOffsetZ;
                    break;
            }
            if (Info.IsBindingOrbit)
            {
                if (Info.OrbitDistance != 0 || mDistanceSpeed != 0)
                {
                    float dadd = Info.OrbitDistance + mDistanceSpeed;
                    float ox = (float)Math.Cos(Direction) * dadd;
                    float oy = (float)Math.Sin(Direction) * dadd;
                    bindingP.X += ox;
                    bindingP.Y += oy;
                }
            }
            //             if (mBindingOffset != null)
            //             {
            //                 if (mBindingOffset.distance != 0)
            //                 {
            //                     float dadd = mBindingOffset.distance;
            //                     float ox = (float)Math.Cos(target.Direction + mBindingOffset.direction) * dadd;
            //                     float oy = (float)Math.Sin(target.Direction + mBindingOffset.direction) * dadd;
            //                     bindingP.X += ox;
            //                     bindingP.Y += oy;
            //                 }
            //                 bindingP.Z += mBindingOffset.height;
            //             }
            return bindingP;
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
            else if (e is ObjectForceSyncPosEvent)
            {
                var oe = e as ObjectForceSyncPosEvent;
                this.InternalSyncObject(oe);
            }
        }
        private void doSpellLockTargetEvent(SpellLockTargetEvent e)
        {
            this._Target = Parent.GetUnit(e.target_obj_id);
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
        protected override void UpdateAI()
        {

        }
        protected override void Update()
        {
            var intervalMS = Parent.CurrentIntervalMS;
            mPassTimeMS += intervalMS;
            updateMotion(intervalMS, (Parent.ActorSyncMode != SyncMode.ForceByServer));
            if (Info.IsMoveable)
            {
                mSpeed = MoveHelper.UpdateSpeed(intervalMS, mSpeed,
                    Info.MSpeedAdd,
                    Info.MSpeedAcc,
                    Info.MSpeed_MIN,
                    Info.MSpeed_MAX);
            }
            if (Info.RotateSpeedSEC != 0)
            {
                mRotateSpeed = MoveHelper.UpdateSpeed(intervalMS, mRotateSpeed,
                    Info.RotateSpeedAdd,
                    Info.RotateSpeedAcc);
            }
            updateLength();
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.AOE:
                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.AOE_BindingTarget:
                    updateAOE(intervalMS);
                    break;
            }
            switch (Info.BodyShape)
            {
                case SpellTemplate.Shape.StripRayTouchEnd:
                    updateRayTouchEnd();
                    break;
                case SpellTemplate.Shape.LineToStart:
                case SpellTemplate.Shape.LineToTarget:
                case SpellTemplate.Shape.LineToSender:
                    updateLineToTarget();
                    break;
            }
            updateKeyFrames();
        }

        public override void SyncPos(UnitSyncPos pos)
        {
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

        //-----------------------------------------------------------------------
        #region _UpdateMotions_

        /// <summary>
        /// 更新移动行为
        /// </summary>
        private void updateMotion(float intervalMS, bool clientMove)
        {
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.SelectLauncher:
                case SpellTemplate.MotionType.SelectTarget:
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (TargetPos != null)
                    {
                        if (clientMove) PreProjectileToTarget(TargetPos.Value, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                    if (this.mStartNormal.HasValue)
                    {
                        if (clientMove) PreMoveLerp(mStartNormal.Value, mSpeed, intervalMS);
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    if (clientMove) PreMoveTo(Direction, mSpeed, intervalMS);
                    break;

                case SpellTemplate.MotionType.Immovability:
                case SpellTemplate.MotionType.AOE:
                    if (clientMove)
                    {
                        mLocalPos.X = mRemotePos.X;
                        mLocalPos.Y = mRemotePos.Y;
                        mLocalPos.Z = mRemotePos.Z;
                    }
                    break;


                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.Binding:
                    if (Sender != null)
                    {
                        updateBinding(Sender);
                    }
                    else
                    {
                        if (clientMove) adjustPos(MoveHelper.GetDistance(intervalMS, mSpeed));
                    }
                    break;
                case SpellTemplate.MotionType.AOE_BindingTarget:
                case SpellTemplate.MotionType.BindingTarget:
                    if (Target != null)
                    {
                        updateBinding(Target);
                    }
                    else
                    {
                        if (clientMove) adjustPos(MoveHelper.GetDistance(intervalMS, mSpeed));
                    }
                    break;

                case SpellTemplate.MotionType.Missile:
                    if (Target != null)
                    {
                        if (Info.SeekingTurningAngleSEC != 0)
                        {
                            if (clientMove) PreTraceToTargetTunning(Target.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                        }
                        else
                        {
                            if (Info.RotateSpeedSEC == 0) PreFaceTo(Target.X, Target.Y);
                            if (clientMove) PreTraceToTarget(Target.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    if (Target != null)
                    {
                        if (Info.SeekingTurningAngleSEC != 0)
                        {
                            if (clientMove) PreTraceToTargetTunning(Target.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                        }
                        else
                        {
                            if (Info.RotateSpeedSEC == 0) PreFaceTo(Target.X, Target.Y);
                            if (clientMove) PreTraceToTarget(Target.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (Sender != null && Sender.IsEnable && Target != null && Target.IsActive)
                    {
                        updateBinding(Sender);
                        PreFaceTo(Target.X, Target.Y);
                    }
                    break;
            }
            if (Info.BodyShape == SpellTemplate.Shape.LineToTarget)
            {
                if (Target != null)
                {
                    PreFaceTo(Target.X, Target.Y);
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                PreFaceTo(StartPos.X, StartPos.Y);
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                if (Sender != null)
                {
                    PreFaceTo(Sender.X, Sender.Y);
                }
            }
            else if (Info.RotateSpeedSEC != 0)
            {
                base.mDirection.TurnFace(MoveHelper.GetDistance(intervalMS, mRotateSpeed));
            }
            if (Info.IsBindingOrbit)
            {
                mDistanceSpeed += MoveHelper.GetDistance(intervalMS, Info.MDistanceSpeedSEC);
                mDistanceSpeed = MoveHelper.UpdateSpeed(intervalMS, mDistanceSpeed, Info.MDistanceSpeedAdd, Info.MDistanceSpeedAcc, Info.MDistanceSpeed_MIN, Info.MDistanceSpeed_MAX);
            }
        }
        private void updateBinding(LayerObject binding)
        {
            if (Info.IsBindingDirection)
            {
                base.mDirection.ForceSync(binding.Direction, binding.BodyDirection);
            }
            if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            {
                var pos = GetBindingPos(binding);
                mLocalPos.X = pos.X;
                mLocalPos.Y = pos.Y;
                mLocalPos.Z = pos.Z;
            }
        }



        //---------------------------------------------------------------------------------------------------
        private void adjustPos(float min_distance)
        {
            float fdistance = MathVector.getDistance(mLocalPos, mRemotePos);
            if (fdistance < min_distance)
            {
                MathVector.moveTo(mLocalPos, mRemotePos.X, mRemotePos.Y, min_distance);
            }
            else
            {
                MathVector.moveTo(mLocalPos, mRemotePos.X, mRemotePos.Y, fdistance / 2f);
            }
        }


        private void PreFaceTo(float x, float y)
        {
            if (this.X == x && this.Y == y)
            {
                return;
            }
            var d = (float)(Math.Atan2(y - this.Y, x - this.X));
            base.mDirection.SyncFace(d);
        }
        private void PreTurnTo(float add)
        {
            base.mDirection.TurnFace(add);
        }
        private void PreMoveTo(float direction, float speedSEC, float intervalMS)
        {
            var pos = mLocalPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (Info.BodyVoxelAnchor == VoxelAnchor.Flooring)
            {
                if (Parent.Terrain3D.TryMoveSpellOnFloor(ref pos, direction, distance))
                {
                    mLocalPos.FromGeometry3(pos);
                }
            }
            else
            {
                Geometry.VectorHelper.MovePolar(ref pos, direction, distance);
                mLocalPos.FromGeometry3(pos);
            }
        }
        private void PreMoveLerp(in Geometry.Vector3 normal, float speedSEC, float intervalMS)
        {
            var pos = mLocalPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            pos = VectorHelper.MoveLerp(pos, normal, distance);
            mLocalPos.FromGeometry3(pos);
        }
        private bool PreProjectileToTarget(in Geometry.Vector3 target, float intervalMS)
        {
            var pos = mLocalPos.ToGeometry3();
            if (mSpeedZ < 0 && pos.Z <= target.Z) return true;
            var distance = Geometry.MotionHelper.GetDistance(intervalMS, mSpeed);
            {
                var totalDistanceQ = VectorHelper.GetDistanceSquare(StartPos.X, StartPos.Y, target.X, target.Y);
                var targetDistanceQ = VectorHelper.GetDistanceSquare(StartPos.X, StartPos.Y, pos.X, pos.Y);
                if (targetDistanceQ >= totalDistanceQ)
                {
                    distance = 0;
                }
            }
            var offsetZ = Geometry.MotionHelper.GetDistance(intervalMS, mSpeedZ);
            var gravity = Info.MCannonGravitySEC > 0 ? Info.MCannonGravitySEC : CFG.GLOBAL_GRAVITY;
            mSpeedZ -= Geometry.MotionHelper.GetDistance(intervalMS, gravity);
            if (distance != 0)
            {
                Geometry.VectorHelper.MovePolar(ref pos, mStartDirection, distance);
            }
            pos.Z += offsetZ;
            if (mSpeedZ < 0 && pos.Z < target.Z)
            {
                pos.Z = target.Z;
                mLocalPos.FromGeometry3(pos);
                return true;
            }
            else
            {
                mLocalPos.FromGeometry3(pos);
                return false;
            }
        }
        private void PreTraceToTarget(Geometry.Vector3 target, float speedSEC, float intervalMS)
        {
            var pos = mLocalPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            Geometry.VectorHelper.MoveTo3D(ref pos, in target, distance);
            mLocalPos.FromGeometry3(pos);
        }
        public void PreTraceToTargetTunning(Geometry.Vector3 target, float speedSEC, float tunningSpeedSEC, float intervalMS)
        {
            var pos = mLocalPos.ToGeometry3();
            var dir = base.mDirection.Direction;
            MoveHelper.MoveToTargetTunning(ref pos, ref dir, target, speedSEC, tunningSpeedSEC, intervalMS);
            mLocalPos.FromGeometry3(pos);
            base.mDirection.SyncFace(dir);
        }
        #endregion

        //---------------------------------------------------------------------------------------------------
        #region _UpdateShape_

        private void updateLength()
        {
            mDistancePos.Value = mLocalPos.Value;
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Chain:
                    if (Sender != null && Sender.IsEnable && Target != null && Target.IsActive)
                    {
                        mDistancePos.Value = Target.WaistPosition.Value;
                    }
                    break;
            }
        }
        public float ResourceFitSize
        {
            get
            {
                switch (Info.BodyShape)
                {
                    case SpellTemplate.Shape.LineToTarget:
                    case SpellTemplate.Shape.LineToStart:
                    case SpellTemplate.Shape.LineToSender:
                        return 1f;
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.StripRayTouchEnd:
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.WideStrip:
                        return mDisplayDistance;
                    default:
                        return mDisplaySize * 2;
                }
            }
        }
        private void updateAOE(float intervalMS)
        {
            switch (Info.BodyShape)
            {
                case SpellTemplate.Shape.LineToTarget:
                case SpellTemplate.Shape.LineToStart:
                case SpellTemplate.Shape.LineToSender:
                    break;
                case SpellTemplate.Shape.Strip:
                case SpellTemplate.Shape.StripRay:
                case SpellTemplate.Shape.StripRayTouchEnd:
                case SpellTemplate.Shape.RectStrip:
                case SpellTemplate.Shape.RectStripRay:
                case SpellTemplate.Shape.WideStrip:
                    updateAoeMotion(intervalMS, Info.Distance, ref mDistanceLimit);
                    mDisplayDistance = mDistanceLimit;
                    break;
                default:
                    updateAoeMotion(intervalMS, Info.BodySize, ref mSizeLimit);
                    mDisplaySize = mSizeLimit;
                    break;
            }
        }
        private void updateAoeMotion(float intervalMS, float base_value, ref float value)
        {
            switch (Info.AOEMType)
            {
                case SpellTemplate.AoeMotionType.Sine:
                    value = (float)Math.Sin(CMath.PI_F * mPassTimeMS / (float)Info.LifeTimeMS) * base_value;
                    break;
                case SpellTemplate.AoeMotionType.Linear:
                default:
                    value += MoveHelper.GetDistance(intervalMS, mSpeed);
                    break;
            }
        }

        private void updateRayTouchEnd()
        {
            if (Launcher != null)
            {
                var ray = Geometry.VoxelStripe.InitFromRay(this.Position, this.Direction, Info.RectWide, this.mDistanceLimit, this.BodyHeight);
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
                        this.mDisplayDistance = CMath.GetDistance(this.X, this.Y, list[0].X, list[0].Y);
                        this.mDisplayDistance = Math.Min(mDisplayDistance, this.mDistanceLimit);
                        this.mDistancePos.Value = new System.Numerics.Vector3(list[0].X, list[0].Y, this.Z);
                    }
                    else
                    {
                        this.mDisplayDistance = this.mDistanceLimit;
                        this.mDistancePos.Value = new System.Numerics.Vector3(ray.LineQ.X, ray.LineQ.Y, this.Z);
                    }
                }
            }
        }

        private void updateLineToTarget()
        {
            if (Info.BodyShape == SpellTemplate.Shape.LineToTarget)
            {
                if (Target != null)
                {
                    this.mDisplayDistance = Geometry.Vector3.Distance(this.Position, Target.Position);
                    this.mDisplayDistance = Math.Min(mDisplayDistance, this.mDistanceLimit);
                    this.mDistancePos.Value = Target.WaistPosition.Value;
                    PreFaceTo(Target.X, Target.Y);
                }
                else
                {
                    this.mDisplayDistance = 0;
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                this.mDisplayDistance = Geometry.Vector3.Distance(this.Position, mStartPos);
                this.mDisplayDistance = Math.Min(mDisplayDistance, this.mDistanceLimit);
                this.mDistancePos.Value = mStartPos.Value;
                PreFaceTo(mStartPos.X, mStartPos.Y);
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                if (Sender != null)
                {
                    this.mDisplayDistance = Geometry.Vector3.Distance(this.Position, Sender.Position);
                    this.mDisplayDistance = Math.Min(mDisplayDistance, this.mDistanceLimit);
                    this.mDistancePos.Value = Sender.WaistPosition.Value;
                    PreFaceTo(Sender.X, Sender.Y);
                }
                else
                {
                    this.mDisplayDistance = 0;
                }
            }
            else
            {
                this.mDisplayDistance = 0;
            }
        }

        #endregion
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
                    if (Info.BodyShape == SpellTemplate.Shape.LineToTarget ||
                        Info.BodyShape == SpellTemplate.Shape.LineToSender ||
                        Info.BodyShape == SpellTemplate.Shape.LineToStart)
                    {

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
                            Parent.PreQueueEvent(new UnitEffectEvent(ObjectID, kfs[i].Effect));
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
                        Parent.PreQueueEvent(new UnitEffectEvent(ObjectID, Info.HitIntervalKeyFrame.Effect));
                    }
                }
            }
        }

        #endregion
    }
}
