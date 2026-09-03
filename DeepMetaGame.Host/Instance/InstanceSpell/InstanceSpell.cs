using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.Data;
using DeepCore.Geometry;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    public class InstanceSpell : InstanceZoneObject
    {
        //----------------------------------------------------------------------------------------------
        protected SpellTemplate mInfo;
        protected SyncSpellInfo mSyncInfo;
        protected LaunchSpell mLaunchData;
        // 释放者
        protected InstanceUnit mLauncherUnit;
        // 发出者
        protected InstanceZoneObject mSender;
        protected AttackRangeHelper AttackRange;
        protected Geometry.Vector3 mStartPos;
        protected float mLaunchDirection;
        //protected Polar3 mBindingOffset;
        // 被跟踪目标
        protected InstanceUnit mTarget;
        protected Geometry.Vector3? mTargetPos;
        protected Geometry.Vector3? mStartNormal;
        protected TimeExpire mSeekingCooldownTime;
        protected SpellChainLevelInfo mChainInfo;

        private int? fromSkillTemplateID;
        private int mHittedCount = 0;
        private float mBaseSize;
        private float mDistance;
        private float mSpeed;
        private float mSpeedZ;
        private float mDistanceSpeed;
        private float mRotateSpeed;

        private readonly PopupKeyFrames<SpellTemplate.KeyFrame> mKeyFrames = new PopupKeyFrames<SpellTemplate.KeyFrame>();
        private bool Finish = false;

        private TimeInterval mHitIntervalTicker;
        private InstanceUnit.StateSkill mBindingSkill;

        private bool mClientVisible;
        private readonly VectorObject3 mPos = new VectorObject3();
        private Geometry.Vector3 mPrvePos = new Geometry.Vector3();
        public bool IsCloneTemplate { get; private set; } = false;
        //----------------------------------------------------------------------------------------------
        protected InstanceSpell() { }
        public static InstanceSpell Alloc(InstanceZone zone, TAddSpell add)
        {
            return zone.ObjectPool.AllocAutoRelease<InstanceSpell>(static s => new InstanceSpell(), zone).Init(zone, add);
        }
        protected virtual InstanceSpell Init(InstanceZone zone, TAddSpell add)
        {
            this.mLaunchData = add.launch;
            this.mKeyFrames.Clear();
            this.mKeyFrames.AddRange(add.template.KeyFrames);
            this.mInfo = add.template;
            this.mLauncherUnit = add.launcher;
            this.mSender = add.sender;
            this.mBaseSize = add.template.BodySize;
            this.mDistance = add.template.Distance;
            this.fromSkillTemplateID = add.FromSkillTemplateID;
            this.FaceTo(add.launcher.Direction);
            //this.IsAffectNearChange = false;
            this.mSyncInfo = new SyncSpellInfo();
            this.mSyncInfo.TemplateID = mInfo.ID;
            this.mSyncInfo.Force = add.launcher.Force;
            this.mHittedUnits.Clear();
            this.mClientVisible = mInfo.ClientVisible;
            this.mHitIntervalTicker = zone.AllocTimeInterval(add.template.HitIntervalMS);
            this.AttackRange = new AttackRangeHelper(add.launcher);
            this.mSpeed = mInfo.MSpeedSEC;
            this.mRotateSpeed = mInfo.RotateSpeedSEC;
            this.mDistanceSpeed = 0;
            this.IsCloneTemplate = add.cloneTemplate;
            return this;
        }
        protected override void Disposing()
        {
            try
            {
                this.mInfo = default;
                this.mSyncInfo = default;
                this.mLaunchData = default;
                // 释放者
                this.mLauncherUnit = default;
                // 发出者
                this.mSender = default;
                this.AttackRange = default;
                this.mStartPos = default;
                this.mLaunchDirection = default;
                //protected Polar3 mBindingOffset;
                // 被跟踪目标
                this.mTarget = default;
                this.mTargetPos = default;
                this.mStartNormal = default;
                this.mSeekingCooldownTime?.Dispose();
                this.mSeekingCooldownTime = default;
                this.mChainInfo = default;

                this.fromSkillTemplateID = default;
                this.mHittedCount = 0;
                this.mBaseSize = default;
                this.mDistance = default;
                this.mSpeed = default;
                this.mSpeedZ = default;
                this.mRotateSpeed = default;

                this.mKeyFrames.Clear();
                this.mHittedUnits.Clear();
                this.Finish = false;

                this.mHitIntervalTicker?.Dispose();
                this.mHitIntervalTicker = default;
                this.mBindingSkill = default;

                this.mClientVisible = default;
                this.mPos.Value = default;
                this.mPrvePos = default;

                this.IsCloneTemplate = default;
            }
            finally
            {
                base.Disposing();
            }
        }
        //----------------------------------------------------------------------------------------------
        public override int TemplateID { get => mInfo.ID; }
        //private ITerrainBlock mCurrentLayer;
        public int? FromSkillTemplateID { get => fromSkillTemplateID; }
        public SpellTemplate Info { get { return mInfo; } }
        public LaunchSpell LaunchData { get { return mLaunchData; } }
        public int TemplageID => mInfo.ID;
        public SpellTemplate TemplateData => mInfo;
        public override string Name => null;
        /// <summary>
        /// 最先技能的发起者
        /// </summary>
        public InstanceUnit LauncherOwner { get { return mLauncherUnit; } }
        public uint LauncherID { get { return mLauncherUnit.ID; } }
        /// <summary>
        /// 技能的出口，比如技能触发技能，则Sender就是一个Spell
        /// </summary>
        public InstanceZoneObject Sender { get { return mSender; } }
        public InstanceUnit Target { get { return mTarget; } }
        //         public override bool IntersectMap { get { return false; } }
        //         public override bool IntersectObj { get { return false; } }
        public override bool Moveable { get { return true; } }
        public override float BodyBlockSize { get { return mBaseSize; } }
        public override float BodyHitSize { get { return mBaseSize; } }
        public override float BodyHeight { get { return mInfo.BodyHeight; } }
        public override float Weight { get { return 0; } }
        public override bool ClientVisible { get { return mClientVisible; } }
        /// <summary>
        /// 连锁等级
        /// </summary>
        public int ChainLevel { get { return (mChainInfo != null) ? mChainInfo.Level : 0; } }
        public SpellChainLevelInfo ChainInfo { get { return mChainInfo; } }
        public uint SenderID { get { return (mSender != null) ? mSender.ID : 0; } }
        public uint TargetID { get { return (mTarget != null) ? mTarget.ID : 0; } }
        public Geometry.Vector3? TargetPos { get { if (mTargetPos.HasValue) { return mTargetPos.Value; } return null; } }
        public override float X { get => mPos.X; }
        public override float Y { get => mPos.Y; }
        public override float Z { get => mPos.Z; }
        public override float WaistZ { get => this.Z; }
        public override float TopZ { get => this.Z; }
        public override Geometry.Vector3 WaistPosition { get => this.Position; }
        public override Geometry.Vector3 Position { get => mPos.ToGeometry3(); }
        public Geometry.Vector3? StartNormal => mStartNormal;

        protected override void InternalSetPos(Geometry.Vector3 pos)
        {
            this.mPos.X = pos.X;
            this.mPos.Y = pos.Y;
            this.mPos.Z = pos.Z;
        }
        protected override void EnterWorld(Geometry.Vector3 pos)
        {
            this.mPos.X = pos.X;
            this.mPos.Y = pos.Y;
            this.mPos.Z = pos.Z;
        }
        public override SyncObjectInfo GenSyncInfo(bool net)
        {
            return GenSyncSpellInfo(net);
        }
        public SyncSpellInfo GenSyncSpellInfo(bool net = false)
        {
            mSyncInfo.pos = this.Position;
            mSyncInfo.direction = this.Direction;
            mSyncInfo.body_direction = this.BodyDirection;
            mSyncInfo.HasSpeed = mInfo.IsMoveable && mSpeed != mInfo.MSpeedSEC;
            mSyncInfo.CurSpeed = mSpeed;
            return mSyncInfo;
        }

        // 如果是导弹类型，则需要目标
        public void setTarget(InstanceUnit target)
        {
            this.mTarget = target;
        }
        public void addHittedUnit(InstanceUnit unit)
        {
            if (unit != null && !mHittedUnits.ContainsKey(unit))
            {
                mHittedUnits.Add(unit, Zone.PassTimeMS);
            }
        }
        public void setTargetPos(Geometry.Vector3? targetPos)
        {
            this.mTargetPos = targetPos;
        }
        public void setChainInfo(SpellChainLevelInfo c)
        {
            this.mChainInfo = c;
        }
        public Geometry.Vector3 GetBindingPos(InstanceZoneObject target)
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
            if (mInfo.IsBindingOrbit)
            {
                if (mInfo.OrbitDistance != 0 || mDistanceSpeed != 0)
                {

                    float dadd = mInfo.OrbitDistance + mDistanceSpeed;
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

        protected override void onAdded()
        {
            if (mLaunchData.ResetSeekingTarget != null)
            {
                var result = Parent.SeekSpellAttackable(
                        this.LauncherOwner,
                        this.Info,
                        this.Position,
                        mLaunchData.ResetSeekingTarget,
                        mChainInfo);
                var newTarget = result.Item1;
                var targetPos = result.Item2;
                if (newTarget != null)
                {
                    this.mTarget = newTarget;
                    if (mLaunchData.ResetSeekingTarget.ChangeDirection)
                    {
                        FaceTo(mTarget.Position);
                    }
                    if (mLaunchData.ResetSeekingTarget.ChangeTargetPos)
                    {
                        mTargetPos = (targetPos);
                    }
                    if (mLaunchData.ResetSeekingTarget.ChangeStartPos)
                    {
                        mPos.FromGeometry3(targetPos.Value);
                    }
                }
            }
            if (mLaunchData.IsAutoSeekingTarget && mTarget == null)
            {
                var result = Parent.SeekSpellAttackable(
                    this.LauncherOwner,
                    this.Info,
                    this.Position,
                    mLaunchData.SeekingTargetRange,
                    Info.ExpectTarget,
                    mLaunchData.SeekingTargetExpect,
                    mChainInfo,
                    SeekingTargetAnchor.Waist,
                    this);
                var newTarget = result.Item1;
                var targetPos = result.Item2;
                this.mTarget = newTarget;
            }
            else if (!mLaunchData.IsAutoSeekingTarget && mTarget != null)
            {
                switch (mInfo.MType)
                {
                    case SpellTemplate.MotionType.SeekerSelectTarget:
                    case SpellTemplate.MotionType.SeekerMissile:
                        mTarget = null;
                        break;
                }
            }

            this.mSyncInfo.ObjectID = base.ID;
            this.mLaunchDirection = this.Direction;
            float startRadius = mLaunchData.LaunchSpellRadius;
            float startAngle = mLaunchData.LaunchSpellAngle;
            float startHeight = mLaunchData.LaunchSpellHeight;
            if (mLaunchData.FromUnitBody && (Sender is InstanceUnit su) && su.ASkill)
            {
                startAngle = su.ASkill.LaunchSpellAngle;
                startRadius = su.ASkill.LaunchSpellRadius * su.BodyScale;
                startHeight = su.ASkill.LaunchSpellHeight * su.BodyScale;
            }
            this.mStartPos = new Vector3(X, Y, Z + startHeight);
            switch (mInfo.MType)
            {
                //----------------------------------------------------------------------------------------------------------
                #region Free
                case SpellTemplate.MotionType.Immovability:
                    Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (mTargetPos == null && mTarget != null)
                    {
                        mTargetPos = mTarget.Position;
                    }
                    if (mTargetPos != null)
                    {
                        Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                        MoveHelper.CalculateSpellLaunchAngle(mInfo, in mStartPos, mTargetPos.Value, CFG.GLOBAL_GRAVITY,
                            out var muzzleAngle,
                            out mLaunchDirection,
                            out mSpeed,
                            out mSpeedZ);
                    }
                    else
                    {
                        this.Finish = true;
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                    if (mTargetPos == null && mTarget != null)
                    {
                        mTargetPos = mTarget.WaistPosition;
                    }
                    Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    if (mTargetPos.HasValue)
                    {
                        var hroz = Geometry.VectorHelper.Polar(mLaunchDirection + startAngle, 1);
                        hroz.Normalize();
                        this.mStartNormal = Vector3.Normalize(mTargetPos.Value - mStartPos);
                        this.mStartNormal = new Vector3(hroz.X, hroz.Y, mStartNormal.Value.Z);
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    Geometry.VectorHelper.MovePolar(ref mStartPos, Direction + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.AOE:
                    Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.Missile:
                    Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    Geometry.VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    if (mInfo.SeekingCooldownMS > 0)
                    {
                        this.mSeekingCooldownTime = Zone.AllocTimeExpire(mInfo.SeekingCooldownMS);
                    }
                    else if (mTarget == null)
                    {
                        mTarget = seekAttackable(mInfo.SeekingRange);
                    }
                    break;
                #endregion
                //----------------------------------------------------------------------------------------------------------
                #region Bind
                case SpellTemplate.MotionType.SelectTarget:
                    if (mTarget != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(mTarget);
                    }
                    break;
                case SpellTemplate.MotionType.SelectLauncher:
                    if (mLauncherUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(mLauncherUnit);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    if (mTarget == null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        if (mInfo.SeekingCooldownMS > 0)
                        {
                            this.mSeekingCooldownTime = Zone.AllocTimeExpire(mInfo.SeekingCooldownMS);
                        }
                        else
                        {
                            mTarget = seekAttackable(mInfo.SeekingRange);
                            if (mTarget != null)
                            {
                                mStartPos = GetBindingPos(mTarget);
                            }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.Binding:
                    if (mSender != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(mSender);
                    }
                    else if (mLauncherUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(mLauncherUnit);
                    }
                    break;

                case SpellTemplate.MotionType.AOE_BindingTarget:
                case SpellTemplate.MotionType.BindingTarget:
                    if (mTarget != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(mTarget);
                    }
                    break;

                case SpellTemplate.MotionType.Chain:
                    if (mSender != null)
                    {
                        if (mChainInfo != null && mChainInfo.IsNextChain)
                        {
                            mStartPos = GetBindingPos(mSender);
                        }
                        else
                        {
                            //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                            mStartPos = GetBindingPos(mSender);
                        }
                    }
                    break;
                    #endregion
                    //----------------------------------------------------------------------------------------------------------
            }
            var mCurrentLayer = Parent.Terrain3D.GetVoxelLayerByPos(in mStartPos);
            if (mCurrentLayer != null)
            {
                switch (mInfo.BodyVoxelAnchor)
                {
                    case VoxelAnchor.Ceiling:
                        mStartPos.Z = mCurrentLayer.Top;
                        break;
                    case VoxelAnchor.Flooring:
                        mStartPos.Z = mCurrentLayer.Upward;
                        break;
                }
            }
            SetPos(mStartPos.X, mStartPos.Y, mStartPos.Z);
        }
        protected override void onRemoved()
        {
            if (mInfo.StopBindingSkillOnRemoved)
            {
                if (this.LauncherOwner != null && this.LauncherOwner.CurrentState is InstanceUnit.StateSkill)
                {
                    (this.LauncherOwner.CurrentState as InstanceUnit.StateSkill).block();
                    this.LauncherOwner.DoSomething();
                }
            }
            Parent.cb_removeSpell(this);
        }

        override protected void onUpdate()
        {
            if (IsPaused) { return; }
            updateMotion();
            if (mInfo.IsMoveable)
            {
                mSpeed = MoveHelper.UpdateSpeed(Parent.UpdateIntervalMS, mSpeed,
                    mInfo.MSpeedAdd,
                    mInfo.MSpeedAcc,
                    mInfo.MSpeed_MIN,
                    mInfo.MSpeed_MAX);
            }
            if (mInfo.RotateSpeedSEC != 0)
            {
                mRotateSpeed = MoveHelper.UpdateSpeed(Parent.UpdateIntervalMS, mRotateSpeed,
                    mInfo.RotateSpeedAdd,
                    mInfo.RotateSpeedAcc);
            }
            updateKeyFrames();
            if (mInfo.MaxHitCount > 0 && mHittedCount >= mInfo.MaxHitCount)
            {
                Parent.RemoveObjectByID(ID);
            }
            else if (PassTimeMS >= mInfo.LifeTimeMS)
            {
                Parent.RemoveObjectByID(ID);
            }
            else if (mInfo.MapBlockExplosion)
            {
                var pos = this.Position;
                if (Parent.IntersectMapByPos(pos, out var layer))
                {
                    affectToBlank(mInfo.HitOnExplosionKeyFrame, true);
                    Parent.RemoveObjectByID(ID);
                }
            }
            if (mHittedUnits.Count > 0 && Info.CleanHitIntervalMS > 0)
            {
                using (var list = Zone.ObjectPool.AllocMap(mHittedUnits))
                {
                    var curTime = Zone.PassTimeMS;
                    foreach (var kv in list)
                    {
                        if (kv.Value + Info.CleanHitIntervalMS < curTime)
                        {
                            mHittedUnits.Remove(kv.Key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新移动行为
        /// </summary>
        private void updateMotion()
        {
            this.mPrvePos.X = X;
            this.mPrvePos.Y = Y;
            this.mPrvePos.Z = Z;
            switch (mInfo.MType)
            {
                case SpellTemplate.MotionType.Immovability:
                    updateImmovability(mSender);
                    break;
                case SpellTemplate.MotionType.SelectTarget:
                case SpellTemplate.MotionType.SelectLauncher:
                    break;

                case SpellTemplate.MotionType.Cannon:
                    if (mTargetPos != null)
                    {
                        if (projectileToTarget(mTargetPos.Value, Parent.UpdateIntervalMS))
                        {
                            this.Finish = true;
                        }
                    }
                    else
                    {
                        this.Finish = true;
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                    if (this.mStartNormal.HasValue)
                    {
                        if (moveLerp(mStartNormal.Value, mSpeed, Parent.UpdateIntervalMS))
                        {
                            this.Finish = true;
                        }
                    }
                    else
                    {
                        if (moveTo(mLaunchDirection, mSpeed, Parent.UpdateIntervalMS))
                        {
                            this.Finish = true;
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    if (moveTo(Direction, mSpeed, Parent.UpdateIntervalMS))
                    {
                        this.Finish = true;
                    }
                    break;

                case SpellTemplate.MotionType.AOE:
                    updateAOE();
                    break;
                case SpellTemplate.MotionType.AOE_Binding:
                    updateAOE();
                    if (mSender != null && mSender.Enable)
                    {
                        updateBinding(mSender);
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                    }
                    break;
                case SpellTemplate.MotionType.AOE_BindingTarget:
                    updateAOE();
                    if (mTarget != null && mTarget.IsActive)
                    {
                        updateBinding(mTarget);
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                    }
                    break;


                case SpellTemplate.MotionType.Binding:
                    if (mSender != null && mSender.Enable)
                    {
                        updateBinding(mSender);
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                    }
                    break;
                case SpellTemplate.MotionType.BindingTarget:
                    if (mTarget != null && mTarget.IsActive)
                    {
                        updateBinding(mTarget);
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                    }
                    break;

                case SpellTemplate.MotionType.Missile:
                    if (mTarget != null)
                    {
                        if (mInfo.SeekingTurningAngleSEC != 0)
                        {
                            traceToTargetTunning(mTarget.WaistPosition, mSpeed, mInfo.SeekingTurningAngleSEC, Parent.UpdateIntervalMS);
                        }
                        else
                        {
                            if (mInfo.RotateSpeedSEC == 0) FaceTo(mTarget.X, mTarget.Y);
                            traceToTarget(mTarget.WaistPosition, mSpeed, Parent.UpdateIntervalMS);
                        }
                    }
                    else
                    {
                        moveTo(mLaunchDirection, mSpeed, Parent.UpdateIntervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    if (mTarget != null)
                    {
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(Parent.UpdateIntervalMS))
                        {
                            if (mInfo.SeekingTurningAngleSEC != 0)
                            {
                                traceToTargetTunning(mTarget.WaistPosition, mSpeed, mInfo.SeekingTurningAngleSEC, Parent.UpdateIntervalMS);
                            }
                            else
                            {
                                if (mInfo.RotateSpeedSEC == 0) FaceTo(mTarget.X, mTarget.Y);
                                traceToTarget(mTarget.WaistPosition, mSpeed, Parent.UpdateIntervalMS);
                            }
                        }
                        else
                        {
                            moveTo(mLaunchDirection, mSpeed, Parent.UpdateIntervalMS);
                        }
                    }
                    else
                    {
                        moveTo(mLaunchDirection, mSpeed, Parent.UpdateIntervalMS);
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(Parent.UpdateIntervalMS))
                        {
                            mTarget = seekAttackable(mInfo.SeekingRange);
                            if (mTarget != null)
                            {
                                Parent.PostObjectEvent(this, new SpellLockTargetEvent(ID, mTarget.ID, this.Position));
                            }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    if (mTarget == null)
                    {
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(Parent.UpdateIntervalMS))
                        {
                            mTarget = seekAttackable(mInfo.SeekingRange);
                            if (mTarget != null)
                            {
                                this.SetPos(GetBindingPos(mTarget));
                                Parent.PostObjectEvent(this, new SpellLockTargetEvent(ID, mTarget.ID, this.Position));
                            }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (mSender != null && mSender.Enable && mTarget != null && mTarget.IsActive)
                    {
                        FaceTo(mTarget.X, mTarget.Y);
                        updateBinding(mSender);
                        UpdateChain(mSender);
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                    }
                    break;
            }

            if (mInfo.BodyShape == SpellTemplate.Shape.LineToTarget)
            {
                if (mTarget != null)
                {
                    this.FaceTo(mTarget.X, mTarget.Y);
                }
            }
            else if (mInfo.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                this.FaceTo(mStartPos.X, mStartPos.Y);
            }
            else if (mInfo.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                this.FaceTo(Sender.X, Sender.Y);
            }
            else if (mInfo.RotateSpeedSEC != 0)
            {
                this.Turn(MoveHelper.GetDistance(Parent.UpdateIntervalMS, mRotateSpeed));
            }

            if (Info.IsBindingOrbit)
            {
                mDistanceSpeed += MoveHelper.GetDistance(Parent.UpdateIntervalMS, mInfo.MDistanceSpeedSEC);
                mDistanceSpeed += MoveHelper.UpdateSpeed(Parent.UpdateIntervalMS, mDistanceSpeed, mInfo.MDistanceSpeedAdd,
                    mInfo.MDistanceSpeedAcc, mInfo.MDistanceSpeed_MIN, mInfo.MDistanceSpeed_MAX);
            }
        }



        private void updateAOE()
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
                    updateAoeMotion(Info.Distance, ref mDistance);
                    break;
                default:
                    updateAoeMotion(Info.BodySize, ref mBaseSize);
                    break;
            }
        }

        private void updateAoeMotion(float base_value, ref float value)
        {
            switch (Info.AOEMType)
            {
                case SpellTemplate.AoeMotionType.Sine:
                    value = (float)Math.Sin(CMath.PI_F * PassTimeMS / (float)Info.LifeTimeMS) * base_value;
                    break;
                case SpellTemplate.AoeMotionType.Linear:
                default:
                    value += MoveHelper.GetDistance(Parent.UpdateIntervalMS, mSpeed);
                    break;
            }
        }
        private void updateImmovability(InstanceZoneObject target)
        {
            if ((target is InstanceUnit))
            {
                var target_unit = target as InstanceUnit;
                if (mInfo.RemoveOnBindingUncontrollable)
                {
                    // 目标不可操控，停止法术 //
                    if (target_unit.IsControllable == false)
                    {
                        Parent.RemoveObject(this);
                        return;
                    }
                }
                if (mInfo.RemoveOnBindingSkillOver)
                {
                    // 目标非技能状态，停止法术 //
                    if (target_unit.CurrentState is InstanceUnit.StateSkill)
                    {
                        if (mBindingSkill == null)
                        {
                            mBindingSkill = target_unit.CurrentState as InstanceUnit.StateSkill;
                        }
                        else if (mBindingSkill != target_unit.CurrentState)
                        {
                            Parent.RemoveObject(this);
                            return;
                        }
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                        return;
                    }
                }
            }
        }


        private void updateBinding(InstanceZoneObject target)
        {
            if (CheckBinding(target) == false)
            {
                Parent.RemoveObject(this);
                return;
            }
            else if (target is InstanceUnit target_unit)
            {
                if (mInfo.RemoveOnBindingUncontrollable)
                {
                    // 目标不可操控，停止法术 //
                    if (target_unit.IsControllable == false)
                    {
                        Parent.RemoveObject(this);
                        return;
                    }
                }
                if (mInfo.RemoveOnBindingSkillOver)
                {
                    // 目标非技能状态，停止法术 //
                    if (target_unit.CurrentState is InstanceUnit.StateSkill)
                    {
                        if (mBindingSkill == null)
                        {
                            mBindingSkill = target_unit.CurrentState as InstanceUnit.StateSkill;
                        }
                        else if (mBindingSkill != target_unit.CurrentState)
                        {
                            Parent.RemoveObject(this);
                            return;
                        }
                    }
                    else
                    {
                        Parent.RemoveObject(this);
                        return;
                    }
                }
            }
            if (mInfo.IsBinding)
            {
                if (mInfo.IsBindingDirection)
                {
                    this.FaceTo(target.Direction);
                }
                this.SetPos(GetBindingPos(target));
            }
        }

        private void UpdateChain(InstanceZoneObject target)
        {
            if (CheckBinding(target) == false)
            {
                Parent.RemoveObject(this);
                return;
            }
        }


        /// <summary>
        /// 更新范围检测以及关键帧
        /// </summary>
        private void updateKeyFrames()
        {
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Missile:
                case SpellTemplate.MotionType.SeekerMissile:
                    if (mTarget != null)
                    {
                        var tpos = mTarget.Position;
                        if (mInfo.BodyVoxelAnchor != VoxelAnchor.Flooring)
                        {
                            tpos.Z += mTarget.BodyHeight * 0.5f;
                        }
                        //【战斗】Missile类技能，在目标消失后失效//
                        if (mTarget != null && Collider.Intersects(this.Position, tpos, mTarget.BodyHitSize + this.BodyHitSize))
                        {
                            if (mTarget.IsActive)
                            {
                                affectToSingle(mTarget, mInfo.HitOnExplosionKeyFrame);
                            }
                            Parent.RemoveObject(this);
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (Finish)
                    {
                        SpellTemplate.KeyFrame kf = mInfo.HitOnExplosionKeyFrame;
                        if (kf != null)
                        {
                            using (var list = ObjectPool.AllocList<InstanceUnit>())
                            {
                                getShapeAttackable(list, AttackReason.Attack);
                                affectToMulti(list, kf, true);
                            }
                        }
                        Parent.RemoveObject(this);
                    }
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (mTarget != null)
                    {
                        var tpos = mTarget.Position;
                        if (mInfo.BodyVoxelAnchor == VoxelAnchor.Floating)
                        {
                            tpos.Z += mTarget.BodyHeight * 0.5f;
                        }
                        // var sp = new Geometry.BoundingSphere(this.Position, this.mDistance);
                        //if (Collider.Sphere_Touch_Position(mTarget, ref sp))
                        if (Collider.Intersects(this.Position, tpos, this.mDistance))
                        {
                            updateKeyFrameSingleTarget(mTarget, true);
                        }
                        else
                        {
                            Parent.RemoveObject(this);
                        }
                    }
                    break;
                default:
                    if (Info.BodyShape == SpellTemplate.Shape.LineToTarget ||
                        Info.BodyShape == SpellTemplate.Shape.LineToStart ||
                        Info.BodyShape == SpellTemplate.Shape.LineToSender)
                    {
                        var tpos = mTarget.Position;
                        if (mInfo.BodyVoxelAnchor == VoxelAnchor.Floating)
                        {
                            tpos.Z += mTarget.BodyHeight * 0.5f;
                        }
                        //var sp = new Geometry.BoundingSphere(this.Position, this.mDistance);
                        if (mTarget != null && Collider.Intersects(this.Position, tpos, this.mDistance))
                        {
                            updateKeyFrameSingleTarget(mTarget, true);
                        }
                        else
                        {
                            updateKeyFrameSingleTarget(mTarget, false);
                        }
                    }
                    else
                    {
                        updateKeyFramesRanged();
                    }
                    if (Finish)
                    {
                        Parent.RemoveObject(this);
                    }
                    break;
            }
        }

        private void updateKeyFrameSingleTarget(InstanceUnit enemy, bool affect)
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames((int)PassTimeMS, kfs);
                bool is_interval_test = mHitIntervalTicker.Update(Parent.UpdateIntervalMS);
                if (affect)
                {
                    if (kfs_count > 0 || is_interval_test || mInfo.HitIntervalMS == 0)
                    {
                        if (kfs_count > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                affectToSingle(enemy, kfs[i]);
                            }
                        }
                        if (mInfo.HitOnExplosion)
                        {
                            // 击中后爆炸
                            affectToSingle(enemy, mInfo.HitOnExplosionKeyFrame);
                            Parent.RemoveObject(this);
                        }
                        else if (mInfo.HitIntervalKeyFrame != null)
                        {
                            if (mInfo.HitIntervalMS == 0)
                            {
                                // 只在接触后第一次产生效果
                                if (!mHittedUnits.ContainsKey(enemy))
                                {
                                    affectToSingle(enemy, mInfo.HitIntervalKeyFrame);
                                }
                            }
                            else if (is_interval_test)
                            {
                                // 间隔产生效果
                                affectToSingle(enemy, mInfo.HitIntervalKeyFrame);
                            }
                        }
                    }
                }
            }
        }

        private void updateKeyFramesRanged()
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames((int)PassTimeMS, kfs);
                bool is_interval_test = mHitIntervalTicker.Update(Parent.UpdateIntervalMS);

                if (kfs_count > 0 || is_interval_test || mInfo.HitIntervalMS == 0)
                {
                    using (var enemy_list = ObjectPool.AllocList<InstanceUnit>())
                    {
                        getShapeAttackable(enemy_list, AttackReason.Attack);

                        if (kfs_count > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                affectToMulti(enemy_list, kfs[i], false);
                            }
                        }

                        if (mInfo.HitOnExplosion)
                        {
                            if (this.LaunchData.InheritDamageTargetList)
                            {
                                if (enemy_list.Count > 0)
                                {
                                    foreach (InstanceUnit u in mHittedUnits.Keys)
                                    {
                                        enemy_list.Remove(u);
                                    }
                                }
                            }
                          
                            if (enemy_list.Count > 0)
                            {
                                // 击中后爆炸
                                affectToMulti(enemy_list, mInfo.HitOnExplosionKeyFrame, true);
                                Parent.RemoveObject(this);
                            }
                        }
                        else if (mInfo.HitIntervalMS == 0)
                        {
                            // 只在接触后第一次产生效果
                            if (enemy_list.Count > 0)
                            {
                                foreach (InstanceUnit u in mHittedUnits.Keys)
                                {
                                    enemy_list.Remove(u);
                                }
                            }
                            if (enemy_list.Count > 0)
                            {
                                // 只在接触后第一次产生效果
                                affectToMulti(enemy_list, mInfo.HitIntervalKeyFrame, true);
                            }
                        }
                        else if (is_interval_test)
                        {
                            // 间隔产生效果
                            affectToMulti(enemy_list, mInfo.HitIntervalKeyFrame, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查Binding效果是否有效，否则终止法术
        /// </summary>
        /// <param name="target"></param>
        /// <returns>False=终止法术</returns>
        protected virtual bool CheckBinding(InstanceZoneObject target)
        {
            return true;
        }

        //------------------------------------------------------------------------------------------------------
        #region __命中相关__


        private readonly HashMap<InstanceUnit, double> mHittedUnits = new();

        private void affectToBlank(SpellTemplate.KeyFrame kf, bool effect)
        {
            if (kf == null) return;

            if (effect && kf.Effect != null)
            {
                Parent.PostObjectEvent(this, new UnitEffectEvent(this.ID, kf.Effect));
            }

            // 法术产生法术
            if (kf.Spell != null)
            {
                Parent.SpellLaunchSpell(this, kf.Spell, this.Position);
            }

            // 召唤
            if (kf.Summon != null)
            {
                Parent.SpellSummonUnit(this, kf.Summon);
            }
        }

        private void affectToSingle(InstanceUnit target, SpellTemplate.KeyFrame kf)
        {
            mHittedUnits.Put(target, Zone.PassTimeMS);
            if (kf == null) return;
            if (mInfo.MaxHitCount <= 0 || mHittedCount < mInfo.MaxHitCount)
            {
                if (kf.Effect != null)
                {
                    Parent.PostObjectEvent(this, new UnitEffectEvent(this.ID, kf.Effect));
                }
                // 法术造成伤害
                if (kf.Attack != null)
                {
                    if (Parent.Formula.IsAttackable(mLauncherUnit, target, mInfo.ExpectTarget, AttackReason.Attack, mInfo))
                    {
                        var attack = TAttackSource.Alloc(this, kf.Attack);
                        target.DoHitAttack(mLauncherUnit, in attack);
                    }
                    //                     if (Parent.UnitAttackSingle(mLauncherUnit, TAttackSource.Alloc(this, kf.Attack), target, mInfo.ExpectTarget))
                    //                     {
                    //                     }
                }
                // 法术产生法术
                if (kf.Spell != null)
                {
                    Parent.SpellLaunchSpell(this, kf.Spell, this.Position, target.ID);
                }
                // 召唤
                if (kf.Summon != null)
                {
                    Parent.SpellSummonUnit(this, kf.Summon);
                }
                this.mHittedCount++;
            }
        }

        private void affectToMulti(List<InstanceUnit> list, SpellTemplate.KeyFrame kf, bool effect)
        {
            if (list.Count > 0)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    mHittedUnits.Put(list[i], Zone.PassTimeMS);
                }
            }
            if (kf == null) return;

            if (effect && kf.Effect != null)
            {
                Parent.PostObjectEvent(this, new UnitEffectEvent(this.ID, kf.Effect));
            }
            if (mInfo.MaxHitCount > 0)
            {
                var expect = mInfo.MaxHitCount - mHittedCount;
                if (list.Count > expect)
                {
                    list.RemoveRange(expect, list.Count - expect);
                }
            }
            if (list.Count > 0)
            {
                // 法术造成伤害
                if (kf.Attack != null)
                {
                    var hitted = Parent.UnitAttackDirect(mLauncherUnit, TAttackSource.Alloc(this, kf.Attack), list);
                }
            }
            // 法术产生法术
            if (kf.Spell != null)
            {
                Parent.SpellLaunchSpell(this, kf.Spell, this.Position);
            }

            // 召唤
            if (kf.Summon != null)
            {
                Parent.SpellSummonUnit(this, kf.Summon);
            }
            mHittedCount += list.Count;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------
        #region __检测范围内目标__

        // 跟踪导弹查找附近单位 //
        public InstanceUnit seekAttackable(float range)
        {
            return Parent.SeekSpellAttackable(this.LauncherOwner,
                this.Info,
                this.Position,
                range,
                mInfo.ExpectTarget,
                mInfo.SeekingExpectTarget,
                mChainInfo,
                SeekingTargetAnchor.Waist,
                this).Item1;
        }

        // 范围伤害，碰撞检测 //
        private void getShapeAttackable(List<InstanceUnit> ret, AttackReason reason)
        {
            AttackRange.Shape = (AttackShape)mInfo.BodyShape;
            AttackRange.AttackRange = this.mBaseSize;
            AttackRange.Direction = this.Direction;
            AttackRange.Distance = this.mDistance;
            AttackRange.ExpectTarget = mInfo.ExpectTarget;
            AttackRange.FanAngle = this.mInfo.FanAngle;
            AttackRange.StripWide = this.mInfo.RectWide;
            var pos = this.Position;
            var prv_pos = pos;
            switch (mInfo.MType)
            {
                case SpellTemplate.MotionType.Straight:
                case SpellTemplate.MotionType.Forward:
                    prv_pos = mPrvePos;
                    break;
            }
            var height = this.mInfo.BodyHeight;
            pos = mInfo.AdjustVoxelAnchor(pos, ref height);
            prv_pos = mInfo.AdjustVoxelAnchor(prv_pos, ref height);
            AttackRange.Height = height;

            //             switch (mInfo.BodyHitVoxelAnchor)
            //             {
            //                 case SpellTemplate.HitVoxelAnchor.NA:
            //                     switch (mInfo.BodyVoxelAnchor)
            //                     {
            //                         case VoxelAnchor.Floating:
            //                             pos.Z -= mInfo.BodyHeight / 2;
            //                             prv_pos.Z -= mInfo.BodyHeight / 2;
            //                             break;
            //                         case VoxelAnchor.Flooring:
            //                             break;
            //                         case VoxelAnchor.Ceiling:
            //                             pos.Z -= mInfo.BodyHeight;
            //                             prv_pos.Z -= mInfo.BodyHeight;
            //                             break;
            //                     }
            //                     break;
            //                 case SpellTemplate.HitVoxelAnchor.Up:
            //                     break;
            //                 case SpellTemplate.HitVoxelAnchor.Middle:
            //                     pos.Z -= mInfo.BodyHeight / 2;
            //                     prv_pos.Z -= mInfo.BodyHeight / 2;
            //                     break;
            //                 case SpellTemplate.HitVoxelAnchor.Down:
            //                     pos.Z -= mInfo.BodyHeight;
            //                     prv_pos.Z -= mInfo.BodyHeight;
            //                     break;
            //             }

            AttackRange.GetShapeAttackable(ret, reason, this.Info, pos, prv_pos);

            //移除发送者无法看到的单位
            for (int i = ret.Count - 1; i >= 0; i--)
            {
                if (!Parent.Formula.IsVisibleAOI(mLauncherUnit, ret[i]))
                {
                    ret.RemoveAt(i);
                }
            }

            if (mChainInfo != null && TemplageID == mChainInfo.SpellID)
            {
                switch (Info.SeekingExpectTarget)
                {
                    case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                    case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                    case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                        for (int i = ret.Count - 1; i >= 0; i--)
                        {
                            if (mChainInfo.ContainsTarget(ret[i]))
                            {
                                ret.RemoveAt(i);
                            }
                        }
                        break;
                }
            }

            // 最大攻击数量 //
            if (mInfo.MaxAffectUnit > 0 && ret.Count > mInfo.MaxAffectUnit)
            {
                FilterAttackableMaxAffect(ret, mInfo.MaxAffectUnit);
            }
        }

        /// <summary>
        /// 过滤最大影响单位
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="maxAffect"></param>
        protected virtual void FilterAttackableMaxAffect(List<InstanceUnit> ret, int maxAffect)
        {
            CUtils.RandomList(Parent.RandomN, ret);
            while (ret.Count > mInfo.MaxAffectUnit)
            {
                ret.RemoveAt(ret.Count - 1);
            }
        }

        #endregion

        //------------------------------------------------------------------------------------------------------
        #region __Motion__

        public bool moveTo(float direction, float speedSEC, float intervalMS)
        {
            var pos = mPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (mInfo.BodyVoxelAnchor == VoxelAnchor.Flooring)
            {
                if (Parent.Terrain3D.TryGetVoxelLayerByPos(in pos, out var layer, true))
                {
                    if (Parent.Terrain3D.TryMoveSpellOnFloor(ref pos, ref layer, direction, distance))
                    {
                        mPos.FromGeometry3(pos);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                Geometry.VectorHelper.MovePolar(ref pos, direction, distance);
                mPos.FromGeometry3(pos);
                return false;
            }
        }
        public bool moveLerp(in Geometry.Vector3 normal, float speedSEC, float intervalMS)
        {
            var pos = mPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            pos = VectorHelper.MoveLerp(pos, normal, distance);
            mPos.FromGeometry3(pos);
            return false;
        }
        public bool traceToTarget(in Geometry.Vector3 target, float speedSEC, float intervalMS)
        {
            var pos = mPos.ToGeometry3();
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            var ret = Geometry.VectorHelper.MoveTo3D(ref pos, in target, distance);
            mPos.FromGeometry3(pos);
            return ret;
        }
        public void traceToTargetTunning(in Geometry.Vector3 target, float speedSEC, float tunningSpeedSEC, float intervalMS)
        {
            var pos = mPos.ToGeometry3();
            var dir = Direction;
            MoveHelper.MoveToTargetTunning(ref pos, ref dir, target, speedSEC, tunningSpeedSEC, intervalMS);
            mPos.FromGeometry3(pos);
            this.FaceTo(dir);
        }
        public bool projectileToTarget(in Geometry.Vector3 target, float intervalMS)
        {
            var start = mStartPos;
            var pos = mPos.ToGeometry3();
            if (mSpeedZ < 0 && pos.Z <= target.Z) return true;
            var distance = Geometry.MotionHelper.GetDistance(intervalMS, mSpeed);
            {
                var totalDistanceQ = VectorHelper.GetDistanceSquare(start.X, start.Y, target.X, target.Y);
                var targetDistanceQ = VectorHelper.GetDistanceSquare(start.X, start.Y, pos.X, pos.Y);
                if (targetDistanceQ >= totalDistanceQ)
                {
                    distance = 0;
                }
            }
            var offsetZ = Geometry.MotionHelper.GetDistance(intervalMS, mSpeedZ);
            var gravity = mInfo.MCannonGravitySEC > 0 ? mInfo.MCannonGravitySEC : CFG.GLOBAL_GRAVITY;
            mSpeedZ -= Geometry.MotionHelper.GetDistance(intervalMS, gravity);
            if (distance != 0)
            {
                Geometry.VectorHelper.MovePolar(ref pos, mLaunchDirection, distance);
            }
            pos.Z += offsetZ;
            if (mSpeedZ < 0 && pos.Z < target.Z)
            {
                pos.Z = target.Z;
                mPos.FromGeometry3(pos);
                return true;
            }
            else
            {
                mPos.FromGeometry3(pos);
                return false;
            }
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
    }
}
