using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.Data;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.XCSV;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.ItemTemplateValue;

namespace DeepCore.Game3D.Host.Instance
{
    public class InstanceSpell : InstanceZoneObject, IZoneSpell
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
        //protected Geometry.Vector3 mStartPos;
        //protected float mLaunchDirection;
        //protected Polar3 mBindingOffset;
        // 被跟踪目标
        protected InstanceUnit m_Target;
        protected Geometry.Vector3? mTargetPos;
        protected Vector3 mStartPos;
        //protected Geometry.Vector3? mStartNormal;
        //protected TimeExpire mSeekingCooldownTime;
        protected SpellChainContext mChainInfo;

        private InstanceUnit.EquipSkill fromSkillTemplateID;
        private int mHittedCount = 0;
        private float mBaseSize;
        private float mDistance;

        private TimeExpire mNoTouchTime;
        //private float mSpeed;
        //private float mSpeedZ;
        //private float mDistanceSpeed;
        //private float mRotateSpeed;

        private ISpellMotion motion;

        private readonly PopupKeyFrames<SpellTemplate.KeyFrame> mKeyFrames = new PopupKeyFrames<SpellTemplate.KeyFrame>();
        private TimeExpire finishTime;
        private bool isFin;
        public bool IsFinish => isFin;

        private TimeInterval mHitIntervalTicker;
        private InstanceUnit.StateSkill mBindingSkill;

        private bool mClientVisible;
        private readonly VectorObject3 mPos = new VectorObject3();
        private Geometry.Vector3 mPrvePos;
        private bool mPrveMapTouch = false;
        public bool IsCloneTemplate { get; private set; } = false;
        //----------------------------------------------------------------------------------------------
        protected InstanceSpell() { }
        public static InstanceSpell Alloc(InstanceZone zone, TAddSpell add)
        {
            return zone.ObjectPool.AllocOrCreateAutoRelease<InstanceSpell>(static s => new InstanceSpell(), zone).Init(zone, add);
        }
        protected virtual InstanceSpell Init(InstanceZone zone, TAddSpell add)
        {
            this.isFin = false;
            this.IsHitted = false;
            this.mLaunchData = add.launch;
            this.mKeyFrames.Clear();
            this.mKeyFrames.AddRange(add.template.KeyFrames);
            this.mInfo = add.template;

            this.mLauncherUnit = add.launcher;
            this.mLauncherUnit.Retain();
            this.mSender = add.sender;
            this.mSender.Retain();

            this.mBaseSize = add.template.BodySize;
            this.mDistance = add.template.Distance;
            this.fromSkillTemplateID = add.FromSkillTemplateID;
            if (this.fromSkillTemplateID != null)
            {
                this.fromSkillTemplateID.Retain();
            }
            this.FaceTo(add.launcher.Direction);
            this.StartSpeed = mInfo.MSpeedSEC;
            //this.IsAffectNearChange = false;
            this.mSyncInfo = ObjectPool.Alloc<SyncSpellInfo>();
            this.mSyncInfo.TemplateID = mInfo.ID;
            this.mSyncInfo.Force = add.launcher.Force;
            this.mHittedUnits.Clear();
            this.mClientVisible = mInfo.ClientVisible;
            this.mNoTouchTime = mInfo.NoTouchTimeMS > 0 ? zone.AllocTimeExpire(add.template.NoTouchTimeMS) : null;
            this.mHitIntervalTicker = zone.AllocTimeInterval(add.template.HitIntervalMS);
            this.AttackRange = new AttackRangeHelper(add.launcher);
            //             this.mSpeed = mInfo.MSpeedSEC;
            //             this.mRotateSpeed = mInfo.RotateSpeedSEC;
            //             this.mDistanceSpeed = 0;
            this.IsCloneTemplate = add.cloneTemplate;
            this.motion = zone.ObjectPool.AllocOrCreateAutoRelease(this, static (st, pool) => ZoneDataFactory.Factory.CreateSpellMotion(st), this);
            this.motion.Init(this);
            return this;
        }
        protected override void Disposing()
        {
            try
            {
                this.mStartPos = default;

                this.finishTime?.Dispose();
                this.finishTime = null;
                this.isFin = false;

                this.IsHitted = false;
                this.mInfo = default;
                this.mSyncInfo?.Dispose();
                this.mSyncInfo = default;
                this.mLaunchData = default;
                // 释放者
                this.mLauncherUnit.Release();
                this.mLauncherUnit = default;
                // 发出者
                this.mSender.Release();
                this.mSender = default;

                this.AttackRange = default;
                //this.mStartPos = default;
                //this.mLaunchDirection = default;
                //protected Polar3 mBindingOffset;
                // 被跟踪目标
                this.m_Target?.Release();
                this.m_Target = default;

                this.mTargetPos = default;
                //this.mStartNormal = default;
                //this.mSeekingCooldownTime?.Dispose();
                //this.mSeekingCooldownTime = default;
                this.mChainInfo?.Release();
                this.mChainInfo = default;
                if (this.fromSkillTemplateID != null)
                {
                    this.fromSkillTemplateID.Release();
                }
                this.fromSkillTemplateID = default;
                this.mHittedCount = 0;
                this.mBaseSize = default;
                this.mDistance = default;
                this.StartSpeed = 0;
                //                 this.mSpeed = default;
                //                 this.mSpeedZ = default;
                //                 this.mRotateSpeed = default;

                this.mKeyFrames.Clear();
                this.mHittedUnits.Clear();

                this.mNoTouchTime?.Dispose();
                this.mNoTouchTime = null;

                this.mHitIntervalTicker?.Dispose();
                this.mHitIntervalTicker = default;
                this.mBindingSkill = default;

                this.mClientVisible = default;
                this.mPos.Value = default;
                //this.mPrvePos = null; ;
                this.mPrveMapTouch = false;
                this.IsCloneTemplate = default;

                this.motion?.Dispose();
                this.motion = null;
            }
            finally
            {
                base.Disposing();
            }
        }
        //----------------------------------------------------------------------------------------------
        #region IZoneSpell

        ISpellMotion IZoneSpell.Motion => this.motion;
        SpellTemplate IZoneSpell.Template => this.Info;
        LaunchSpell IZoneSpell.LaunchData => this.mLaunchData;
        IZoneObject IZoneSpell.Sender => this.Sender;
        IZoneUnit IZoneSpell.LauncherUnit => this.LauncherOwner;
        bool IZoneSpell.IsNextChain => (mChainInfo != null && mChainInfo.HasNextChain);
        double IZoneSpell.PassTimeMS => this.PassTimeMS;
        bool IZoneSpell.IsForceSync => true;
        public float StartSpeed { get; set; }
        Vector3 IZoneSpell.RemotePosition => this.Position;
        Vector3 IZoneSpell.PrevPos => this.mPrvePos;

        //---------------------------------------------------------------------
        public Vector3? StartNormal { get; set; }
        public Vector3? RayTouchPoint { get; set; }
        float IZoneSpell.SpellDistance { get => mDistance; set => mDistance = value; }
        float IZoneSpell.SpellSize { get => mBaseSize; set => mBaseSize = value; }
        float IZoneSpell.SpellDisplayDistance { get => mDistance; set => mDistance = value; }
        float IZoneSpell.SpellDisplaySize { get => mBaseSize; set => mBaseSize = value; }

        Vector3? IZoneSpell.TargetPos { get => mTargetPos; set => mTargetPos = value; }
        IZoneUnit IZoneSpell.TargetUnit { get => m_Target; set => this.setTarget(value as InstanceUnit, true); }
        void IZoneSpell.FaceTo(float dir) => this.FaceTo(dir);
        void IZoneSpell.FaceTo(Vector3 dir) => this.FaceTo(dir);
        void IZoneSpell.Turn(float dir) => this.Turn(dir);
        void IZoneSpell.SetPosition(Vector3 position) => this.SetPos(position);
        //---------------------------------------------------------------------
        bool IZoneSpell.TrySeekAttackable(float range, bool postEvent, out IZoneUnit target)
        {
            target = seekAttackable(range);
            if (postEvent && target != null)
            {
                setTarget(target as InstanceUnit, postEvent);
            }
            return target != null;
        }
        bool IZoneSpell.TryRayCastTouchEndUnit(VoxelStripe ray, out IZoneUnit target)
        {
            target = null;
            return false;
        }
        bool IZoneSpell.CheckBinding(IZoneObject target) => CheckBinding(target as InstanceZoneObject);
        bool IZoneSpell.CheckRemoveOnBindingSkillOver(IZoneUnit target)
        {
            if (target is InstanceUnit target_unit)
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
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        public void Finish(bool destoryImmediately)
        {
            if (destoryImmediately)
            {
                Parent.RemoveObject(this);
            }
            else
            {
                this.isFin = true;
                if (Info.DestoryTimeMS > 0)
                {
                    this.finishTime = Zone.AllocTimeExpire(Info.DestoryTimeMS);
                    this.PostForceSync();
                }
            }
        }
        public void PostForceSync()
        {
            var hit = ObjectPool.Alloc<SpellSyncEvent>().Init(ID, this.Position, this.Direction, IsHitted, IsFinish, PassTimeMS, motion.CurrentSpeed);
            Parent.PostObjectEvent(this, hit);
        }
        public override void SetPassTimeMS(double passTimeMS)
        {
            base.SetPassTimeMS(passTimeMS);
            PostForceSync();
        }
        #endregion
        //----------------------------------------------------------------------------------------------
        #region Properties
        public override int TemplateID { get => mInfo.ID; }
        //private ITerrainBlock mCurrentLayer;
        public InstanceUnit.EquipSkill FromSkillTemplateID { get => fromSkillTemplateID; }
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
        public InstanceUnit Target { get { return m_Target; } }
        //         public override bool IntersectMap { get { return false; } }
        //         public override bool IntersectObj { get { return false; } }
        public override bool Moveable { get { return true; } }
        public override float BodyBlockSize { get { return mBaseSize; } }
        public override float BodyHitSize { get { return mBaseSize; } }
        public override float BodyHeight { get { return mInfo != null ? mInfo.BodyHeight : 0; } }
        public override float Weight { get { return 0; } }
        public override bool ClientVisible { get { return mClientVisible; } }
        public bool IsHitted { get; private set; }
        /// <summary>
        /// 连锁等级
        /// </summary>
        public int ChainLevel { get { return (mChainInfo != null) ? mChainInfo.Level : 0; } }
        public SpellChainContext ChainInfo { get { return mChainInfo; } }
        public uint SenderID { get { return (mSender != null) ? mSender.ID : 0; } }
        public uint TargetID { get { return (m_Target != null) ? m_Target.ID : 0; } }
        public Geometry.Vector3? TargetPos => mTargetPos;
        public override float X { get => mPos.X; }
        public override float Y { get => mPos.Y; }
        public override float Z { get => mPos.Z; }
        public override float WaistZ { get => this.Z; }
        public override float TopZ { get => this.Z; }
        public override Geometry.Vector3 WaistPosition { get => this.Position; }
        public override Geometry.Vector3 Position { get => mPos.ToGeometry3(); }

        public bool IsFromSpellMagnitude
        {
            get
            {
                if (LaunchData.FromSpellMagnitude)
                {
                    if (Sender is IZoneSpell senderSpell && senderSpell.StartNormal.HasValue)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion
        //----------------------------------------------------------------------------------------------
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
            mSyncInfo.HasSpeed = mInfo.IsMoveable && motion.CurrentSpeed != mInfo.MSpeedSEC;
            mSyncInfo.CurSpeed = motion.CurrentSpeed;
            return mSyncInfo;
        }

        // 如果是导弹类型，则需要目标
        public void setTarget(InstanceUnit target, bool postEvent = false)
        {
            if (m_Target != target)
            {
                this.m_Target?.Release();
                this.m_Target = target;
                this.m_Target?.Retain();
                if (postEvent)
                {
                    if (m_Target != null)
                    {
                        Parent.PostObjectEvent(this, ObjectPool.Alloc<SpellLockTargetEvent>().Init(ID, m_Target.ID, this.Position));
                    }
                    else
                    {
                        Parent.PostObjectEvent(this, ObjectPool.Alloc<SpellLockTargetEvent>().Init(ID, 0, this.Position));
                    }
                }
            }
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
        public void setChainInfo(SpellChainContext c)
        {
            this.mChainInfo?.Release();
            this.mChainInfo = c;
            this.mChainInfo?.Retain();
        }
        private InstanceUnit resetSeeekingTarget(FocusTarget ResetSeekingTarget, bool postEvent)
        {
            if (ResetSeekingTarget != null)
            {
                var result = Parent.SeekSpellAttackable(
                        this.LauncherOwner,
                        this.Info,
                        this.Position,
                        ResetSeekingTarget,
                        mChainInfo);
                var newTarget = result.Item1;
                var targetPos = result.Item2;
                if (newTarget != null)
                {
                    this.setTarget(newTarget, postEvent);
                    if (ResetSeekingTarget.ChangeDirection)
                    {
                        FaceTo(this.Target.Position);
                    }
                    if (ResetSeekingTarget.ChangeTargetPos)
                    {
                        mTargetPos = (targetPos);
                    }
                }
                if (targetPos != null)
                {
                    if (ResetSeekingTarget.ChangeStartPos)
                    {
                        mPos.FromGeometry3(targetPos.Value);
                    }
                }
                return newTarget;
            }
            return null;
        }
        public void SetSpeedRate(float rate)
        {
            if (rate > 0 && rate != 1)
            {
                this.motion.CurrentSpeed = this.motion.CurrentSpeed * rate;
                PostForceSync();
            }
        }
        //------------------------------------------------------------------------------------------------------
        protected override void onAdded()
        {
            this.mStartPos = this.Position;
            this.mPrvePos = mPos.ToGeometry3();
            if (Parent.TryTouchSpell(this, out var layer, out var newDir))
            {
                this.mPrveMapTouch = true;
            }
            else
            {
                this.mPrveMapTouch = false;
            }
            this.mSyncInfo.ObjectID = base.ID;
            if (mLaunchData.ResetSeekingTarget != null)
            {
                resetSeeekingTarget(mLaunchData.ResetSeekingTarget, false);
            }
            if (mLaunchData.IsAutoSeekingTarget && this.Target == null)
            {
                var result = Parent.SeekSpellAttackable(
                    this.LauncherOwner,
                    this.Info,
                    this.Position,
                    mLaunchData.SeekingTargetRange,
                    Info.ExpectTarget,
                    mLaunchData.SeekingTargetExpect,
                    mLaunchData.SeekingIgnoreInChain,
                    mChainInfo,
                    SeekingTargetAnchor.Waist,
                    this);
                var newTarget = result.Item1;
                var targetPos = result.Item2;
                if (newTarget != null)
                {
                    FaceTo(newTarget.Position);
                }
                this.setTarget(newTarget, false);
            }
            else if (!mLaunchData.IsAutoSeekingTarget && this.Target != null)
            {
                switch (mInfo.MType)
                {
                    case SpellTemplate.MotionType.SeekerSelectTarget:
                    case SpellTemplate.MotionType.SeekerMissile:
                        setTarget(null, false);
                        break;
                }
            }
            switch (mInfo.BodyShape)
            {
                case SpellTemplate.Shape.LineToTargetPos:
                    if (TargetPos == null && Target != null)
                    {
                        this.setTargetPos(Target.Position);
                    }
                    if (TargetPos != null)
                    {
                        var tpos = TargetPos.Value;
                        tpos.Z = this.Position.Z;
                        this.setTargetPos(tpos);
                    }
                    break;
            }
            this.motion.OnAdded();
            this.mStartPos = this.Position;
        }
        protected override void onRemoved()
        {
            if (mInfo.StopBindingSkillOnRemoved)
            {
                if (this.LauncherOwner != null && this.LauncherOwner.CurrentState is InstanceUnit.StateSkill skillState)
                {
                    skillState.block();
                    this.LauncherOwner.DoSomething();
                }
            }
            Parent.cb_removeSpell(this);
        }

        override protected void onUpdate()
        {
            if (IsPaused) { return; }
            if (Info.IsNeedTarget)
            {
                if (Target == null || Target.IsActive == false)
                {
                    if (Info.ResetSeekingTarget != null)
                    {
                        this.resetSeeekingTarget(Info.ResetSeekingTarget, true);
                    }
                }
            }
            this.mPrvePos = mPos.ToGeometry3();
            //             this.mPrveMapTouch = false;
            //             if (mInfo.MapBlockExplosion)
            //             {
            //                 var pos = this.Position;
            //                 if (Parent.TryTouchSpell(this, out var layer, out var newDir))
            //                 {
            //                     beforMapTouch = true;
            //                 }
            //             }
            this.motion.UpdateMotion(Parent.UpdateIntervalMS);
            if (!IsFinish)
            {
                updateKeyFrames();
                if (mInfo.MaxHitCount > 0 && mHittedCount >= mInfo.MaxHitCount)
                {
                    Finish(false);
                }
                else if (mInfo.LifeTimeMS <= 0)
                {
                    if (Sender == null || !Sender.Enable)
                    {
                        Finish(false);
                    }
                }
                else if (PassTimeMS >= mInfo.LifeTimeMS)
                {
                    Finish(false);
                }
                else if (mInfo.MapBlockExplosion)
                {
                    var pos = this.Position;
                    if (Parent.TryTouchSpell(this, out var layer, out var newDir))
                    {
                        if (!mPrveMapTouch)
                        {
                            affectToMap(mInfo.HitOnExplosionKeyFrame, true, layer, newDir);
                            Finish(false);
                        }
                        this.mPrveMapTouch = true;
                    }
                    else
                    {
                        this.mPrveMapTouch = false;
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
            else
            {
                switch (Info.MType)
                {
                    case SpellTemplate.MotionType.Cannon:
                        if (IsHitted == false)
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
                        }
                        break;
                }
                if (this.finishTime != null)
                {
                    if (this.finishTime.Update(Zone.UpdateIntervalMS))
                    {
                        affectToDummy(mInfo.LastKeyFrame, true);
                        Parent.RemoveObject(this);
                    }
                }
                else
                {
                    affectToDummy(mInfo.LastKeyFrame, true);
                    Parent.RemoveObject(this);
                }
            }
        }
        /// <summary>
        /// 更新范围检测以及关键帧
        /// </summary>
        private void updateKeyFrames()
        {
            if (mNoTouchTime != null)
            {
                if (mNoTouchTime.Update(Zone.UpdateIntervalMS))
                {
                    mNoTouchTime.Dispose();
                    mNoTouchTime = null;
                }
                else
                {
                    return;
                }
            }
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Missile:
                case SpellTemplate.MotionType.SeekerMissile:
                    if (this.Target != null)
                    {
                        var tpos = this.Target.Position;
                        if (mInfo.BodyVoxelAnchor != VoxelAnchor.Flooring)
                        {
                            tpos.Z += this.Target.BodyHeight * 0.5f;
                        }
                        //【战斗】Missile类技能，在目标消失后失效//
                        if (this.Target != null && Collider.Intersects(this.Position, tpos, this.Target.BodyHitSize + this.BodyHitSize))
                        {
                            if (this.Target.IsActive)
                            {
                                affectToSingle(this.Target, mInfo.HitOnExplosionKeyFrame);
                            }
                            Finish(false);
                        }
                        else
                        {
                            updateKeyFramesToDummy();
                        }
                    }
                    else
                    {
                        updateKeyFramesToDummy();
                    }
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (IsFinish)
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
                        Finish(false);
                    }
                    else
                    {
                        updateKeyFramesToDummy();
                    }
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (this.Target != null)
                    {
                        var tpos = this.Target.Position;
                        if (mInfo.BodyVoxelAnchor == VoxelAnchor.Floating)
                        {
                            tpos.Z += this.Target.BodyHeight * 0.5f;
                        }
                        // var sp = new Geometry.BoundingSphere(this.Position, this.mDistance);
                        //if (Collider.Sphere_Touch_Position(mTarget, ref sp))
                        if (Collider.Intersects(this.Position, tpos, this.mDistance))
                        {
                            updateKeyFrameSingleTarget(Target, true);
                        }
                        else
                        {
                            Finish(false);
                        }
                    }
                    else
                    {
                        Finish(false);
                    }
                    break;
                default:
                    if (Info.BodyShape == SpellTemplate.Shape.LineToTargetPos)
                    {
                        if (TargetPos != null)
                        {
                            updateKeyFramesRanged();
                        }
                    }
                    else if (
                        Info.BodyShape == SpellTemplate.Shape.LineToTarget ||
                        Info.BodyShape == SpellTemplate.Shape.LineToStart ||
                        Info.BodyShape == SpellTemplate.Shape.LineToSender)
                    {
                        if (Target != null)
                        {
                            var tpos = Target.Position;
                            if (mInfo.BodyVoxelAnchor == VoxelAnchor.Floating)
                            {
                                tpos.Z += Target.BodyHeight * 0.5f;
                            }
                            //var sp = new Geometry.BoundingSphere(this.Position, this.mDistance);
                            if (Collider.Intersects(this.Position, tpos, this.mDistance))
                            {
                                updateKeyFrameSingleTarget(Target, true);
                            }
                            else
                            {
                                updateKeyFrameSingleTarget(Target, false);
                            }
                        }
                        else if (TargetPos != null)
                        {
                            updateKeyFramesRanged();
                        }
                        else
                        {
                            Finish(false);
                        }
                    }
                    else
                    {
                        updateKeyFramesRanged();
                    }
                    break;
            }
        }

        private void updateKeyFrameSingleTarget(InstanceUnit enemy, bool affect)
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames(PassTimeMS, kfs);
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
                            Finish(false);
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
                else
                {
                    if (kfs_count > 0)
                    {
                        for (int i = 0; i < kfs.Count; i++)
                        {
                            affectToDummy(kfs[i], true);
                        }
                    }
                }
            }
        }

        private void updateKeyFramesToDummy()
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames(PassTimeMS, kfs);
                {
                    if (kfs_count > 0)
                    {
                        for (int i = 0; i < kfs.Count; i++)
                        {
                            affectToDummy(kfs[i], true);
                        }
                    }
                }
            }
        }

        private void updateKeyFramesRanged()
        {
            using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
            {
                int kfs_count = mKeyFrames.PopKeyFrames(PassTimeMS, kfs);
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
                                Finish(false);
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

        private void OnSpellHitted(InstanceUnit hitted, TAttackSource attack)
        {
            if (!IsHitted)
            {
                this.IsHitted = true;
                LauncherOwner.cb_unitSpellFirstHitted(this, hitted, attack);
                PostForceSync();
            }
            LauncherOwner.cb_unitSpellHitted(this, hitted, attack);
        }


        private readonly HashMap<InstanceUnit, double> mHittedUnits = new();

        private void affectToMap(SpellTemplate.KeyFrame kf, bool effect, ITerrainLayer layer, float newDirection)
        {
            if (kf == null) return;

            if (effect && kf.Effect != null)
            {
                Parent.PostObjectEvent(this, ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, kf.Effect));
            }

            // 法术产生法术
            if (kf.Spell != null)
            {
                Parent.SpellLaunchSpell(this, kf.Spell, newDirection);
            }

            // 召唤
            if (kf.Summon != null)
            {
                Parent.SpellSummonUnit(this, kf.Summon);
            }
        }
        private void affectToDummy(SpellTemplate.KeyFrame kf, bool effect)
        {
            if (kf == null) return;

            if (effect && kf.Effect != null)
            {
                Parent.PostObjectEvent(this, ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, kf.Effect));
            }

            // 法术产生法术
            if (kf.Spell != null)
            {
                Parent.SpellLaunchSpell(this, kf.Spell, this.Direction);
            }

            // 召唤
            if (kf.Summon != null)
            {
                Parent.SpellSummonUnit(this, kf.Summon);
            }
        }

        private int affectToSingle(InstanceUnit target, SpellTemplate.KeyFrame kf)
        {
            var count = 0;
            if (kf == null) return count;
            if (mInfo.MaxHitCount <= 0 || mHittedCount < mInfo.MaxHitCount)
            {
                if (kf.Effect != null)
                {
                    Parent.PostObjectEvent(this, ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, kf.Effect));
                }
                // 法术造成伤害
                if (kf.Attack != null)
                {
                    if (Parent.Formula.IsAttackable(mLauncherUnit, target, mInfo.ExpectTarget, AttackReason.Attack, mInfo))
                    {
                        using (var attack = TAttackSource.AllocWithSpell(this, kf.Attack))
                        {
                            if (target.DoHitAttack(mLauncherUnit, attack))
                            {
                                mHittedUnits.Put(target, Zone.PassTimeMS);
                                this.mHittedCount++;
                                count++;
                                OnSpellHitted(target, attack);
                            }
                        }
                    }
                    //                     if (Parent.UnitAttackSingle(mLauncherUnit, TAttackSource.Alloc(this, kf.Attack), target, mInfo.ExpectTarget))
                    //                     {
                    //                     }
                }
                // 法术产生法术
                if (kf.Spell != null)
                {
                    var reflectAngle = VectorHelper.GetDegree(this.Position, target.Position) + CMath.RADIANS_180;
                    Parent.SpellLaunchSpell(this, kf.Spell, reflectAngle, target);
                }
                // 召唤
                if (kf.Summon != null)
                {
                    Parent.SpellSummonUnit(this, kf.Summon);
                }
            }
            return count;
        }

        private int affectToMulti(List<InstanceUnit> list, SpellTemplate.KeyFrame kf, bool effect)
        {
            var count = 0;
            if (kf == null) return count;

            if (effect && kf.Effect != null)
            {
                Parent.PostObjectEvent(this, ObjectPool.Alloc<UnitEffectEvent>().Init(this.ID, kf.Effect));
            }
            // 法术造成伤害
            if (kf.Attack != null)
            {
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
                    using (var attack = TAttackSource.AllocWithSpell(this, kf.Attack))
                    {
                        var hitted = Parent.UnitAttackDirect(mLauncherUnit, attack, list);
                        mHittedCount += hitted;
                        count += hitted;
                        for (int i = list.Count - 1; i >= 0; --i)
                        {
                            mHittedUnits.Put(list[i], Zone.PassTimeMS);
                            OnSpellHitted(list[i], attack);
                        }
                    }
                }
            }
            // 法术产生法术
            if (kf.Spell != null)
            {
                var reflectAngle = this.Direction + CMath.RADIANS_180;
                if (list.Count > 0)
                {
                    var target = list[0];
                    reflectAngle = VectorHelper.GetDegree(this.Position, target.Position) + CMath.RADIANS_180;
                }
                Parent.SpellLaunchSpell(this, kf.Spell, reflectAngle, this.Target, this.TargetPos);
            }

            // 召唤
            if (kf.Summon != null)
            {
                Parent.SpellSummonUnit(this, kf.Summon);
            }
            return count;
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
        #region __检测范围内目标__
        public override Vector3 GetRandomPos()
        {
            var random = this.RandomN;
            var shape = (AttackShape)mInfo.BodyShape;
            switch (shape)
            {
                case AttackShape.Circle:
                    {
                        float r = this.BodySize;
                        float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                        float x = (float)(this.X + Math.Cos(a) * r);
                        float y = (float)(this.Y + Math.Sin(a) * r);
                        return new Vector3(x, y, this.Z);
                    }
                case AttackShape.Fan:
                    {
                        float r = (float)(random.NextFloat() * this.BodySize);
                        float a = (float)(this.Direction + Info.FanAngle / 2 + random.NextFloat() * Info.FanAngle);
                        float x = (float)(this.X + Math.Cos(a) * r);
                        float y = (float)(this.Y + Math.Sin(a) * r);
                        return new Vector3(x, y, this.Z);
                    }
                case AttackShape.Strip:
                    {
                        return VoxelStripe.RandomPos(random, this.Position, this.Direction, this.mInfo.RectWide, mDistance);
                    }
                case AttackShape.StripRay:
                    {
                        return VoxelStripe.RandomPos(random, this.Position, this.Direction, this.mInfo.RectWide, mDistance);
                    }
                case AttackShape.StripRayTouchEnd:
                    {
                        return VoxelStripe.RandomPos(random, this.Position, this.Direction, this.mInfo.RectWide, mDistance);
                    }
                case AttackShape.RectStrip:
                    {
                        return VoxelRectStripe.RandomPos(random, this.Position, this.Direction, this.mInfo.RectWide, mDistance);
                    }
                case AttackShape.RectStripRay:
                    {
                        return VoxelRectStripe.RandomPos(random, this.Position, this.Direction, this.mInfo.RectWide, mDistance);
                    }
                case AttackShape.WideStrip:
                    {
                        return VoxelStripe.RandomPos(random, this.Position, this.Direction, mDistance, this.mInfo.RectWide);
                    }
                case AttackShape.LineToTarget:
                    if (this.Target != null)
                    {
                        var src = this.Position;
                        var dst = Target.Position;
                        VectorHelper.MoveLerpTo(ref src, dst, random.NextFloat() * Vector3.Distance(src, dst));
                        return src;
                    }
                    break;
                case AttackShape.LineToTargetPos:
                    if (this.TargetPos != null)
                    {
                        var src = this.Position;
                        var dst = TargetPos.Value;
                        VectorHelper.MoveLerpTo(ref src, dst, random.NextFloat() * Vector3.Distance(src, dst));
                        return src;
                    }
                    break;
                case AttackShape.LineToStart:
                    {
                        var src = this.Position;
                        var dst = mStartPos;
                        VectorHelper.MoveLerpTo(ref src, dst, random.NextFloat() * Vector3.Distance(src, dst));
                        return src;
                    }
                case AttackShape.LineToSender:
                    if (this.Sender != null)
                    {
                        var src = this.Position;
                        var dst = Sender.Position;
                        VectorHelper.MoveLerpTo(ref src, dst, random.NextFloat() * Vector3.Distance(src, dst));
                        return src;
                    }
                    break;
            }
            {
                float r = (float)(random.NextFloat() * this.BodySize);
                float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                float x = (float)(this.X + Math.Cos(a) * r);
                float y = (float)(this.Y + Math.Sin(a) * r);
                return new Vector3(x, y, this.Z);
            }
        }


        // 跟踪导弹查找附近单位 //
        public InstanceUnit seekAttackable(float range)
        {
            return Parent.SeekSpellAttackable(this.LauncherOwner,
                this.Info,
                this.Position,
                range,
                mInfo.ExpectTarget,
                mInfo.SeekingExpectTarget,
                mInfo.SeekingIgnoreInChain,
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
                case SpellTemplate.MotionType.StraightPingPong:
                case SpellTemplate.MotionType.Boomerang:
                case SpellTemplate.MotionType.Forward:
                case SpellTemplate.MotionType.Backward:
                    prv_pos = mPrvePos.Value;
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
                if (Info.SeekingIgnoreInChain)
                {
                    for (int i = ret.Count - 1; i >= 0; i--)
                    {
                        if (mChainInfo.ContainsTarget(ret[i]))
                        {
                            ret.RemoveAt(i);
                        }
                    }
                }
                //                 switch (Info.SeekingExpectTarget)
                //                 {
                //                     case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                //                     case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                //                     case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                //                         for (int i = ret.Count - 1; i >= 0; i--)
                //                         {
                //                             if (mChainInfo.ContainsTarget(ret[i]))
                //                             {
                //                                 ret.RemoveAt(i);
                //                             }
                //                         }
                //                         break;
                //                 }
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
            Zone.Formula.SortSeekingTarget(Zone.RandomN, this.Info, this.Position, this.Info.FilterAffect, ret);

            while (ret.Count > mInfo.MaxAffectUnit)
            {
                ret.RemoveAt(ret.Count - 1);
            }
        }

        #endregion

        //------------------------------------------------------------------------------------------------------
#if false
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
#endif
        //------------------------------------------------------------------------------------------------------
    }
}
