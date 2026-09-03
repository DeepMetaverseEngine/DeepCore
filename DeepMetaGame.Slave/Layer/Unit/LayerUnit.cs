using DeepCore.Components;
using DeepCore.EventTrigger;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using DeepCore.Geometry;
using DeepMetaGame.Data;

namespace DeepCore.Game3D.Slave.Layer
{
    public partial class LayerUnit : LayerZoneEntity, IZoneUnit, IEnvironmentObject
    {
        UnitInfo IZoneUnit.Template => Info;
        UnitSkillAbility IZoneUnit.ASkill => ASkill;
        bool IZoneUnit.IsControllable => IsActive && !IsStun && this.mCurrentMainState.Value != UnitActionStatus.Damage;

        public readonly SyncUnitInfo SyncInfo;
        public readonly UnitInfo Info;
        public readonly UnitGuardAbility AGuard;
        public readonly UnitResourceAbility AResource;
        public readonly UnitMotionAbility AUnitMotion;
        public readonly UnitDropItemAbility ADropItem;
        public readonly UnitInventoryAbility AInventory;
        public readonly UnitSkillAbility ASkill;

        // Status
        private bool mPaused;
        private long mHP;
        private long mMP;
        private long mSP;
        private long mMaxHP;
        private long mMaxMP;
        private long mMaxSP;
        private long mExp;
        private long mNextExp;
        private int mInventorySize;
        private int mLevel;
        private float mRemoteGravity;
        private float mMoveSpeedSEC;
        private float mFastMoveRate;
        private float mFastCastRate;
        private float mFastActionRate;
        private long mMoney;
        private string mName;
        private string mDisplayName;
        private float mBodySize;
        private IZoneShape mZoneShape;
        private float mPickRange;
        private float mBodyScale = 1;
        private float mResScale = 1;
        private System.Func<bool> mIsInAirCustomFunc;

        public string TemplateName { get { return Info.Name; } }
        public override int TemplateID { get { return Info.ID; } }
        public override string Name { get { return mName; } }
        public override string DisplayName { get { return mDisplayName; } }
        public float BodyHitSize { get { return (mBodySize + Info.BodySizeHitAppend) * BodyScale; } }
        sealed public override float BodyBlockSize { get { return (mBodySize) * BodyScale; } }
        sealed public override float BodyHeight { get { return (Info.BodyHeight) * BodyScale; } }
        public float BasePickRange => mPickRange;
        public float BodyScale => mBodyScale;
        public float ResScale => mResScale;
        public string Alias { get { return SyncInfo.Alias; } }
        public byte Force { get { return SyncInfo.Force; } }
        public bool IsPaused { get { return mPaused; } }
        public long HP { get { return mHP; } }
        public long MP { get { return mMP; } }
        public long SP { get { return mSP; } }
        public long MaxHP { get { return mMaxHP; } }
        public long MaxMP { get { return mMaxMP; } }
        public long MaxSP { get { return mMaxSP; } }

        public float HPAmount { get => mHP / (float)mMaxHP; }
        public float MPAmount { get => mMP / (float)mMaxMP; }
        public float SPAmount { get => mSP / (float)mMaxSP; }

        public float BaseMoveSpeedSEC { get { return mMoveSpeedSEC; } }
        public float MoveSpeedSEC { get { return mMoveSpeedSEC * mFastMoveRate; } }
        public float TurnFaceSpeedSEC { get { return mTurnFaceSpeedSEC; } }
        public float TurnBodySpeedSEC { get { return mTurnBodySpeedSEC; } }
        public float FastMoveRate { get { return mFastMoveRate; } }
        public float FastCastRate { get { return mFastCastRate; } }
        public float FastActionRate { get { return mFastActionRate; } }
        public long Money { get { return mMoney; } }
        public string PlayerUUID { get { return SyncInfo.PlayerUUID; } }
        public int Level { get { return mLevel; } }
        public long Exp { get { return mExp; } }
        public long NextExp { get { return mNextExp; } }
        public float ExpAmount { get; private set; }

        public int InventorySize { get { return mInventorySize; } }

        public uint DockingParentID { get; private set; }
        public DockingOffset DockingOffset { get; private set; }


        public int Dummy_0 { get; private set; }
        public int Dummy_1 { get; private set; }
        public int Dummy_2 { get; private set; }
        public int Dummy_3 { get; private set; }
        public int Dummy_4 { get; private set; }
        public int Dummy_5 { get; private set; }
        public string Skin { get; private set; }
        public string[] Avatar { get; private set; }
        public float Gravity
        {
            get => this.mLocalPos.Gravity;
            private set
            {
                this.mRemoteGravity = value;
                this.mLocalPos.Gravity = value;
            }
        }
        public uint CurrentTarget { get; private set; }
        public float ServerDirection { get { return mDirection.ServerDirection; } }
        public override Geometry.Vector3 Position => mLocalPos.Position;
        public override float X { get { return mLocalPos.X; } }
        public override float Y { get { return mLocalPos.Y; } }
        public override float Z { get { return mLocalPos.Z; } }
        public virtual bool IsInAir { get { return mIsInAirCustomFunc != null ? mIsInAirCustomFunc() : mLocalPos.IsInAir; } }

        public float LayerUpward { get; protected set; }
        public override bool TouchObj { get { return (mTouchObj) && (CurrentState != UnitActionStatus.Dead) && (mHP > 0); } }
        public override bool TouchMap { get { return mTouchMap; } }
        virtual public bool IsActive { get { return (HP > 0); } }
        public override bool IsStaticBlock { get { return TouchObj && mStaticBlockable && IsActive && IsEnable; } }
        public override IZoneShape ZoneShape { get { return mZoneShape; } }
        public Geometry.VoxelCylinder VoxelHitBody { get => new Geometry.VoxelCylinder(this.Position, BodyHitSize, BodyHeight); }
        public UnitType UType { get; }
        public int UTypeAsInt { get { return (int)UType; } }
        public LayerEnvironmentMap EnvironmentVarMap => mEnvironmentVarMap;
        public object EventSender { get => _EventSender; }
        public IZoneUnit HostObject => this._EventSender as IZoneUnit;

        private bool mTouchObj;
        private bool mTouchMap;
        private bool mStaticBlockable;
        protected ILayerUnitPosition mLocalPos;
        protected float mTurnFaceSpeedSEC;
        protected float mTurnBodySpeedSEC;
        protected readonly LayerEnvironmentMap mEnvironmentVarMap;
        private object _EventSender;
        public LayerUnit(UnitInfo temp, SyncUnitInfo syn, LayerZone parent, AddUnitEvent add, object sender)
        {
            var info = syn.template ?? temp;
            this.mEnvironmentVarMap = new(parent);
            base.Init(syn.ObjectID, parent);
            this._EventSender = sender;
            this.SyncInfo = syn;
            this.mName = syn.Name;
            this.UType = syn.UType;
            this.mDisplayName = syn.fields.displayName;
            this.Info = info;
            {
                this.AGuard = info.Abilities.GetComponentAs<UnitGuardAbility>();
                this.AResource = info.Abilities.GetComponentAs<UnitResourceAbility>();
                this.AUnitMotion = info.Abilities.GetComponentAs<UnitMotionAbility>();
                this.ADropItem = info.Abilities.GetComponentAs<UnitDropItemAbility>();
                this.AInventory = info.Abilities.GetComponentAs<UnitInventoryAbility>();
                this.ASkill = info.Abilities.GetComponentAs<UnitSkillAbility>();
            }
            this.mRemotePos.X = syn.pos.X;
            this.mRemotePos.Y = syn.pos.Y;
            this.mRemotePos.Z = syn.pos.Z;
            this.mBodySize = info.BodySize;
            this.mPickRange = info.PickRange;
            this.mBodyScale = syn.fields.bodyScale;
            this.mResScale = syn.fields.resScale;
            if (AUnitMotion != null)
            {
                this.mMoveSpeedSEC = AUnitMotion.MoveSpeedSEC;
                this.mTurnFaceSpeedSEC = (float.IsNaN(AUnitMotion.TurnSpeedSEC) || AUnitMotion.TurnSpeedSEC == 0) ? Parent.CFG.UNIT_TURN_SPEED_SEC : AUnitMotion.TurnSpeedSEC;
                this.mTurnBodySpeedSEC = (float.IsNaN(AUnitMotion.BodyTurnSpeedSEC) || AUnitMotion.BodyTurnSpeedSEC == 0) ? Parent.CFG.UNIT_TURN_SPEED_SEC : AUnitMotion.BodyTurnSpeedSEC;
            }
            this.mFastCastRate = 0;
            this.mStaticBlockable = syn.StaticBlockable;
            this.mTouchObj = syn.IsTouchObj;
            this.mTouchMap = syn.IsTouchMap;
            this.mRemoteState.UnitMainState = (UnitActionStatus)syn.status;
            this.mRemoteState.UnitSubState = syn.sub_status;
            this.ForceSyncCurrentState((UnitActionStatus)syn.status, syn.sub_status, syn);
            switch (UType)
            {
                case UnitType.TYPE_PET:
                case UnitType.TYPE_ATTACHMENT:
                    mTouchObj = false;
                    break;
                case UnitType.TYPE_BUILDING:
                    mTouchObj = (BodyBlockSize > 0);
                    break;
            }
            var mpos = Parent.Terrain3D.CreateUnitPosition(this);
            mpos.SetPos(syn.pos);
            this.mLocalPos = mpos;
            this.mDirection.ForceSync(syn.direction, syn.body_direction);
            DoSyncFields(syn.fields);
            ResetSkills();
        }


        public void DoSyncInfo(SyncUnitInfo syn)
        {
            this.mRemotePos.X = syn.pos.X;
            this.mRemotePos.Y = syn.pos.Y;
            this.mRemotePos.Z = syn.pos.Z;
            this.mStaticBlockable = syn.StaticBlockable;
            this.mTouchObj = syn.IsTouchObj;
            this.mTouchMap = syn.IsTouchMap;
            this.ForceSyncCurrentState((UnitActionStatus)syn.status, syn.sub_status, syn);
            this.mLocalPos.SetPos(syn.pos);
            this.mDirection.ForceSync(syn.direction, syn.body_direction);
            DoSyncFields(syn.fields);
            SyncAuraStatus(syn.CurrentAuraStatus);
            SyncBuffStatus(syn.CurrentBuffStatus);
            SyncCardStatus(syn.CurrentCardStatus);
        }


        protected override void Disposing()
        {
            this.ClearSkillMove();
            this.cleanStaticBlock();
            this.clearSkillAction();
            this.cleanAuras();
            this.ClearCards();
            base.Disposing();
            this.clearEvents();
            this.mBuffStatus.Clear();
            this.mChantingSkill = null;
            this.mDamageTime = null;
            this.mHitFlyState?.Dispose();
            this.mHitFlyState = null;
            this.mLastLaunchSkill = null;
            this.mPickEvent = null;
            this.mEnvironmentVarMap.Clear();
            this.mSkillStatus.Clear();
        }

        protected internal override void OnAdded()
        {
            //             if (mVirtual != null)
            //             {
            //                 mVirtual.OnInit(this);
            //             }
            base.OnAdded();
            this.Gravity = SyncInfo.fields.currentGravity;
            if (SyncInfo.speed_z != 0)
            {
                this.StartFly(SyncInfo.speed_z);
            }
            this.SyncAuraStatus(SyncInfo.CurrentAuraStatus);
            this.SyncBuffStatus(SyncInfo.CurrentBuffStatus);
            this.SyncCardStatus(SyncInfo.CurrentCardStatus);
        }

        //--------------------------------------------------------------------------------
        private IZoneShape blockZoneShape = null;
        private Vector3? originPos;
        protected virtual void cleanStaticBlock()
        {
            if (blockZoneShape != null)
            {
                originPos = null;
                onStaticBlockChanged(blockZoneShape, false);
                blockZoneShape = null;
            }
        }
        protected virtual void updateStaticBlock()
        {
            if (originPos != null && originPos.Value != this.Position)
            {
                cleanStaticBlock();
            }
            if (this.IsStaticBlock)
            {
                if (blockZoneShape == null && this.ZoneShape != null)
                {
                    originPos = this.Position;
                    blockZoneShape = this.ZoneShape;
                    onStaticBlockChanged(blockZoneShape, true);
                }
            }
            else
            {
                if (blockZoneShape != null)
                {
                    originPos = null;
                    onStaticBlockChanged(blockZoneShape, false);
                    blockZoneShape = null;
                }
            }
        }
        protected virtual void onStaticBlockChanged(IZoneShape zoneShape, bool enable)
        {
            var mmap = Parent.Terrain3D;
            mmap.FillMapBlockByShape(zoneShape, enable);
        }

        //--------------------------------------------------------------------------------

        protected override void UpdateAI()
        {
            float intervalMS = Parent.CurrentIntervalMS;
            UpdateMotion(intervalMS);
            updateSkillAction(intervalMS);
        }

        protected override void Update()
        {
            float intervalMS = Parent.CurrentIntervalMS;
            UpdatePos(intervalMS);
            UpdateDamage(intervalMS);
            UpdateSkills(intervalMS);
            UpdateBuffs(intervalMS);
            UpdatePickEvent(intervalMS);
            UpdateState();
            updateStaticBlock();
        }
        protected virtual void UpdatePos(float intervalMS)
        {
            if (HostObject is IZoneUnit hostUnit)
            {
                this.mLocalPos.ForceSetPos(this.mRemotePos.Value = hostUnit.Position.Value);
                //this.mDirection.ForceSync(hostUnit.Direction, hostUnit.BodyDirection);
                this.mDirection.SyncFace(hostUnit.Direction, true);
                this.mDirection.SyncBody(hostUnit.BodyDirection, true);
                var trunFaceSpeed = mTurnFaceSpeedSEC * FastMoveRate;
                var trunBodySpeed = mTurnBodySpeedSEC * FastMoveRate;
                mDirection.Update(intervalMS, trunFaceSpeed, trunBodySpeed);
                this.LayerUpward = hostUnit.LayerUpward; 
                this.ForceSyncCurrentState(hostUnit.CurrentActionStatus, hostUnit.CurrentActionSubstate, null);
            }
            else
            {
                var trunFaceSpeed = mTurnFaceSpeedSEC * FastMoveRate;
                var trunBodySpeed = mTurnBodySpeedSEC * FastMoveRate;
                mDirection.Update(intervalMS, trunFaceSpeed, trunBodySpeed);
                if (Parent.ActorSyncMode != SyncMode.ForceByServer)
                {
                    mLocalPos.Update(mRemotePos.ToGeometry3(), intervalMS);
                    if (mLocalPos.FixPos(mRemotePos.ToGeometry3(), intervalMS, this.MoveSpeedSEC))
                    {
                        if (CurrentState.IsControlMoveable() && !mRemoteState.UnitMainState.IsControlMoveable())
                        {
                            this.SyncCurrentState(mRemoteState);
                        }
                    }
                }
            }

        }

        protected override void UpdateEnd()
        {
            base.UpdateEnd();
            UpdateDocking();
        }
        protected virtual void UpdateDocking()
        {
            if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            {
                if (GetDockingParent() is LayerZoneObject docking)
                {
                    if (DockingOffset is DockingOffset offset && offset.TailsCount == null)//如果拖尾模式，还是走Host坐标
                    {
                        var pos = docking.Position;
                        if (offset.Radius != 0)
                        {
                            if (offset.BindBodyRotation)
                            {
                                Geometry.VectorHelper.MovePolar(ref pos, docking.BodyDirection + offset.Angle, offset.Radius);
                            }
                            else
                            {
                                Geometry.VectorHelper.MovePolar(ref pos, docking.Direction + offset.Angle, offset.Radius);
                            }
                        }
                        pos.Z += offset.Z;
                        if (offset.SolidFaceAngle.HasValue)
                        {
                            var d = docking.Direction + offset.SolidFaceAngle.Value;
                            base.mDirection.ForceSync(d, d);
                        }
                        this.mLocalPos.SetPos(pos);
                        this.mRemotePos.Value = pos.Value;
                    }
                }
            }
        }
        public LayerZoneObject GetDockingParent()
        {
            if (DockingParentID != 0)
            {
                return Parent.GetObject(DockingParentID);
            }
            return null;
        }

        public override void ForceSyncPos(in Geometry.Vector3 pos)
        {
            this.mRemotePos.Value = pos.Value;
            mLocalPos.SetPos(in pos);
        }
        public override void ForceFaceTo(float dir, float body_dir)
        {
            mDirection.ForceSync(dir, body_dir);
        }
        public override void SyncPos(UnitSyncPos pos)
        {
            if (HostObject is IZoneUnit hostUnit)
            {
                return;
            }
            if (pos.HasModifer(UnitSyncModifer.LayerUpward))
            {
                this.LayerUpward = pos.LayerUpward;
            }
            if (pos.HasModifer(UnitSyncModifer.Posistion))
            {
                base.mRemotePos.X = pos.X;
                base.mRemotePos.Y = pos.Y;
                base.mRemotePos.Z = pos.Z;
                if (Parent.ActorSyncMode == SyncMode.ForceByServer)
                {
                    mLocalPos.SetPos(pos.X, pos.Y, pos.Z);
                }
            }
            base.SyncPos(pos);
            //             if (pos.HasModifer(UnitSyncModifer.Direction))
            //             {
            //                 this.mDirectionChange = pos.Direction;
            //                 this.mDirection = MoveHelper.DirectionChange(
            //                     mDirection,
            //                     mDirectionChange,
            //                     mTurnSpeedSEC * FastMoveRate,
            //                     Parent.CurrentIntervalMS);
            //             }
            //             if (pos.HasModifer(UnitSyncModifer.BodyRotation))
            //             {
            //                 this.mBodyDirectionChange = pos.BodyDirection;
            //                 this.mBodyDirection = MoveHelper.DirectionChange(
            //                     mBodyDirection,
            //                     mBodyDirectionChange,
            //                     mTurnSpeedSEC * FastMoveRate,
            //                     Parent.CurrentIntervalMS);
            //             }
            this.SyncState(pos);
        }
        protected virtual void SyncState(UnitSyncPos pos)
        {
//             if (HostObject is IZoneUnit hostUnit)
//             {
//                 this.ForceSyncCurrentState(hostUnit.CurrentActionStatus, hostUnit.CurrentActionSubstate, null);
//             }
//             else
            {
                this.mRemoteState.Sync(pos);
                if (Parent.ActorSyncMode == SyncMode.ForceByServer)
                {
                    this.SyncCurrentState(pos);
                }
                else
                {
                    if (CurrentState.IsControlMoveable() && mRemoteState.UnitMainState == UnitActionStatus.Idle)
                    {
                        if (IsNeedFixPos(mLocalPos, mRemotePos))
                        {
                            pos.UnitMainState = CurrentState;
                        }
                    }
                    this.SyncCurrentState(pos);
                }
            }      
        }
        //         /// <summary>
        //         /// 修正本地坐标//
        //         /// </summary>
        //         public virtual bool FixPos(in DeepCore.Geometry.Vector3 remote_pos, int intervalMS)
        //         {
        //             ILayerUnitPosition local_pos = mLocalPos;
        //             if (MoveSpeedSEC == 0)
        //             {
        //                 local_pos.SetPos(remote_pos.X, remote_pos.Y, remote_pos.Z);
        //                 return true;
        //             }
        //             float fdistance = Geometry.Vector3.Distance(local_pos.Position, remote_pos);
        //             if (fdistance > 0)
        //             {
        //                 float dspeed = MoveHelper.GetDistance(intervalMS, MoveSpeedSEC);
        //                 if (fdistance >= Parent.AsyncUnitPosModifyMaxRange)
        //                 {
        //                     local_pos.SetPos(remote_pos.X, remote_pos.Y, remote_pos.Z);
        //                     return true;
        //                 }
        //                 else
        //                 {
        //                     if (CurrentState.IsControlMoveable())
        //                     {
        //                         return local_pos.FixLerp(in remote_pos, Math.Min(dspeed / fdistance, 1f));
        //                         // MathVector.moveTo(, remote_pos.X, remote_pos.Y, db);
        //                     }
        //                     else
        //                     {
        //                         return local_pos.FixLerp(in remote_pos, 1);
        //                         //MathVector.moveTo(local_pos, remote_pos.X, remote_pos.Y, db);
        //                     }
        //                 }
        //             }
        //             return true;
        //         }
        /// <summary>
        /// 是否需要修正坐标
        /// </summary>
        /// <returns></returns>
        public bool IsNeedFixPos(ILayerUnitPosition local_pos, in IVector3 remote_pos)
        {
            float fdistance = Geometry.Vector3.Distance(local_pos.Position, remote_pos.ToGeometry3());
            return fdistance >= Parent.MinStep;
        }

        /// <summary>
        /// 由外部设置Func:单位是否浮空
        /// </summary>
        public void SetIsInAirFunc(System.Func<bool> func)
        {
            mIsInAirCustomFunc = func;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="smooth">是否慢慢转身</param>
        public void SetDirection(float direction, bool smooth = true)
        {
            mDirection.FaceTo(direction, smooth);
        }

        public float GetDeadTimeCD()
        {
            if (mDeadTime != null)
            {
                return mDeadTime.Amount;
            }
            return 0;
        }

        //         public void SetBodySize(float size)
        //         {
        //             if (size > 0)
        //             {
        //                 mBodySize = size;
        //             }
        //         }

        public void SetLocalPosSpeedZ(float v) { mLocalPos.SpeedZ = v; }
        public float GetLocalPosSpeedZ() { return mLocalPos.SpeedZ; }

        public UnitActionStatus GetStartMoveStatus() => ActionDefine.Instance.GetStartMoveStatus(this.Info, this.AUnitMotion, this.MoveSpeedSEC);
        public UnitActionStatus GetStopMoveStatus() => UnitActionStatus.Idle;
        //--------------------------------------------------------------------------------

        /// <summary>
        /// 获得当前单位服务端可同步环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetEnvironmentVar(string key)
        {
            return mEnvironmentVarMap.GetEnvironmentVar(key);
        }
        /// <summary>
        /// 获得当前单位服务端可同步环境变量列表
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<string> ListEnvironmentVars()
        {
            return mEnvironmentVarMap.Keys;
        }
        public IEnumerable<KeyValuePair<string, object>> ListEnvironmentValues()
        {
            return mEnvironmentVarMap.ListEnvironmentValues();
        }
        //--------------------------------------------------------------------------------



    }



}
