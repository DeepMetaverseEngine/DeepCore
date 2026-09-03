using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using System;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer
    {
        // private TimeInterval<int> mCheckGuard;
        private SyncMode mCurrentSyncMode = SyncMode.MoveByClient_PreSkillByClient;
        private readonly UnitSyncPos mRemoteSyncPos = new UnitSyncPos();
        //private UnitState mRemoteSyncState;

        private readonly RecyclableReference<UnitAxisAction> mSendingAxis = new RecyclableReference<UnitAxisAction>();
        private readonly RecyclableReference<UnitCustomAxisAction> mSendingCustomAxis = new RecyclableReference<UnitCustomAxisAction>();
        private readonly RecyclableReference<UnitUpdatePosAction> mSendingPos = new RecyclableReference<UnitUpdatePosAction>();
        private readonly RecyclableReference<UnitFaceToAction> mSendingFaceTo = new RecyclableReference<UnitFaceToAction>();
        private readonly RecyclableReference<UnitUpdatePosAction> mLastSendPos = new();

        private readonly float mMinStepSEC;
        private readonly float mElasticAngle;


        /*     //暂时关闭
             //上传位置间隔计时器
             private TimeInterval<int> _SendActionInterval = new TimeInterval<int>(UpdatePosIntervalMS);
             //上传位置间隔时间(毫秒)
             public static int UpdatePosIntervalMS = 330;*/
        //-------------------------------------------------------------------------------------------

        private void update_ai_Guard(float intervalMS)
        {
            base.UpdateAI();
        }

        // 服务端强同步 //
        private void update_ai_ForceByServer(float intervalMS)
        {
            if (mSendingAxis.HasValue)
            {
                //请求服务端移动//
                SendAction(mSendingAxis.Value);
            }
            else if (mSendingPos.HasValue)
            {
                SendAction(mSendingPos.Value);
            }
            UpdateMotion(intervalMS);
        }

        // 客户端本地处理移动，实时上传自己坐标位置，技能预先本地表现 //
        private void update_ai_MoveByClient_PreSkillByClient(float intervalMS)
        {
            //有摇杆动作//
            var currentPos = mLocalPos.Position;
            var inAir = IsInAir;
            var axis = mSendingAxis.Value;
            axis?.Retain();
            var subst = mSendingAxis.Value?.subState;
            var sendst = UnitActionStatus.NA;
            var sendpos = mSendingPos.Value;
            sendpos?.Retain();
            try
            {
                if (axis != null && axis.distanceRate != 0)
                {
                    #region Axis

                    // 处理技能以及控制位移 //
                    if (mCurrentSkillAction is PreSkillByClient)
                    {
                        var action = mCurrentSkillAction as PreSkillByClient;
                        //客户端预先技能动作//
                        if (action.IsDone)
                        {
                            base.mDirection.FinishUpdate();
                            clearSkillAction();
                            action.controlMoveTo(axis);
                            SendAction(axis);
                        }
                        else
                        {
                            action.controlMoveTo(axis);
                            if (action.IsDone || action.IsControlMoveable)
                            {
                                SendAction(axis);
                            }
                        }
                    }
                    else if (mCurrentSkillAction is PreSkillByServer)
                    {
                        if (IsCanControlMove)
                        {
                            PreAxisMove(axis, intervalMS);
                        }
                        if (IsCanControlFaceTo)
                        {
                            base.mDirection.SyncFace(axis.faceto);
                        }
                    }
                    else if (CurrentState == UnitActionStatus.ClientCustom)
                    {
                        sendst = CurrentState;
                        //if (!PreAxisZeroGravity(axis, intervalMS))
                        {
                            PreAxisMove(axis, intervalMS);
                        }
                        base.mDirection.SyncFace(axis.faceto);
                    }
                    else if (CurrentState == UnitActionStatus.Somersault)
                    {
                        sendst = CurrentState;
                        //if (!PreAxisZeroGravity(axis, intervalMS))
                        {
                            PreAxisMove(axis, intervalMS);
                        }
                        base.mDirection.SyncFace(axis.faceto);
                    }
                    else if (!HasSkillMove)
                    {
                        sendst = inAir /*&& !IsZeroGravityFly*/ ? UnitActionStatus.Jump : GetStartMoveStatus();
                        if (IsCanControlMove)
                        {
                            //if (!PreAxisZeroGravity(axis, intervalMS))
                            {
                                PreAxisMove(axis, intervalMS);
                            }
                            //客户端上传坐标//
                            this.PreSetCurrentMainState(sendst, null, null);
                        }
                        if (IsCanControlFaceTo)
                        {
                            base.mDirection.SyncFace(axis.faceto);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region NoneAxis
                    if (CurrentState == UnitActionStatus.ClientCustom)
                    {
                        sendst = CurrentState;
                        if (axis != null)
                        {
                            this.PreFaceTo(axis.faceto);
                            if (sendpos != null)
                            {
                                sendpos.direction = axis.faceto;
                            }
                        }
                    }
                    else if (CurrentState == UnitActionStatus.Somersault)
                    {
                        sendst = CurrentState;
                        if (axis != null)
                        {
                            this.PreFaceTo(axis.faceto);
                            if (sendpos != null)
                            {
                                sendpos.direction = axis.faceto;
                            }
                        }
                    }
                    else if (CurrentState == UnitActionStatus.Climb)
                    {
                        sendst = CurrentState;
                    }
                    else if (CurrentState == UnitActionStatus.Pick)
                    {
                        sendst = CurrentState;
                    }
                    else
                    {
                        sendst = inAir/* && !IsZeroGravityFly*/ ? UnitActionStatus.Jump : UnitActionStatus.Idle;
                        if (axis != null)
                        {
                            if (CurrentState.IsControllable())
                            {
                                //if (PreAxisZeroGravity(axis, intervalMS))
                                /*{
                                    sendst = UnitActionStatus.Move;
                                }*/
                                //客户端上传坐标//
                                this.PreSetCurrentMainState(sendst, subst, null);
                                this.PreFaceTo(axis.faceto);
                                if (sendpos != null)
                                {
                                    sendpos.direction = axis.faceto;
                                }
                            }
                        }
                        else if (sendpos != null)
                        {
                            if (sendpos.mainState.HasValue)
                            {
                                sendst = sendpos.mainState.Value;
                            }
                            subst = sendpos.subst;
                        }
                        else
                        {
                            if (CurrentState != UnitActionStatus.Jump || inAir)
                            {
                                sendst = CurrentState;
                            }
                        }
                    }
                    //停止技能移动//
                    if (mCurrentSkillAction is PreSkillByClient)
                    {
                        var action = mCurrentSkillAction as PreSkillByClient;
                        action.controlMoveTo(axis);
                    }
                    //处理UpdatePos移动//
                    else if (sendpos != null)
                    {
                        if (sendpos.mainState.HasValue)
                            sendst = sendpos.mainState.Value;
                        if (sendpos.subst != null)
                            subst = sendpos.subst;
                        if (IsCanControlFaceTo)
                        {
                            var sendDir = this.Direction;
                            var sendBodyDir = this.BodyDirection;
                            if (sendpos.direction.HasValue) sendDir = sendpos.direction.Value;
                            if (sendpos.bodyDirection.HasValue) sendDir = sendpos.bodyDirection.Value;
                            base.mDirection.ForceSync(sendDir, sendBodyDir);
                        }
                        if (IsCanControlMove)
                        {
                            if (mCurrentSkillAction == null)
                            {
                                if (sendpos.mainState.HasValue)
                                {
                                    this.PreSetCurrentMainState(sendpos.mainState.Value, sendpos.subst, null);
                                }
                            }
                            if (sendpos.pos.HasValue)
                            {
                                this.PreSetPos(sendpos.pos.Value);
                            }
                        }
                    }
                    else
                    {
                        if (CurrentState.IsControlMoveable())
                        {
                            PreSetCurrentMainState(sendst, subst, null);
                        }
                    }
                    #endregion
                }

                //客户端预先技能动作//
                if (mCurrentSkillAction is PreSkillByClient skillAction)
                {
                    skillAction.onUpdate(intervalMS);
                    if (skillAction.IsDone)
                    {
                        clearSkillAction();
                    }
                }
                UpdateMotion(intervalMS);
                UpdateSendPos(intervalMS, sendst, subst);
            }
            finally
            {
                axis?.Release();
                sendpos?.Release();
            }
        }
        //-------------------------------------------------------------------------------------------
        #region UpdateSendPos

        protected void UpdateSendPos(float intervalMS, UnitActionStatus sendst, string subst)
        {
            //有主动控制位移状态// 翻滚比较特殊，不能控制移动，本身有固定方向位移，但不需要强拉
            if (IsNeedFixRemotePos(out var rpos))
            {
                //FixPos(in rpos, intervalMS);
                mLocalPos.FixPos(in rpos, intervalMS, this.MoveSpeedSEC);
                if (mLastSendPos.HasValue)
                {
                    mLastSendPos.Value.pos = rpos;
                }
            }
            CheckAndUpdateSendPos(sendst, subst);
        }
        protected virtual void CheckAndUpdateSendPos(UnitActionStatus sendst, string subst)
        {
            //原版 GC 72B
            /* 
                  var mBeginSendPos = new UnitUpdatePosAction(
                  this.ObjectID,
                  mLocalPos.Position,
                  mDirection.Direction,
                  mDirection.BodyDirection,
                  sendst,
                  mSendPos?.subst);
           
                 if (IsNeedFixUpdatePos(mBeginSendPos, mLastSendPos))
                 {
                      SendAction(mBeginSendPos);
                 }
           */
            //优化 
            if (IsNeedFixUpdatePos(mLocalPos.Position, mDirection.Direction, sendst, subst, mLastSendPos))
            {
                var mBeginSendPos = ObjectPool.Alloc<UnitUpdatePosAction>().Init(
                    this.ObjectID,
                    mLocalPos.Position,
                    mDirection.Direction,
                    mDirection.BodyDirection,
                    sendst,
                    subst);
                SendAction(mBeginSendPos);
                mLastSendPos.Value = mBeginSendPos;
            }
        }
        protected virtual bool IsNeedFixRemotePos(out Geometry.Vector3 rpos)
        {
            if (!CurrentState.IsControllable() && !HasSkillMove)
            {
                rpos = mRemotePos.ToGeometry3();
                if (DeepCore.Geometry.Vector3.Distance(mLocalPos.Position, rpos) > Parent.AsyncUnitPosModifyMinRange)
                {
                    return true;
                }
            }
            rpos = Geometry.Vector3.Zero;
            return false;
        }
        //         protected virtual bool IsNeedFixUpdatePos(UnitUpdatePosAction sending, UnitUpdatePosAction lastSend)
        //         {
        //             if (lastSend == null) return true;
        //             float epsilon = MoveHelper.GetDistance(this.Parent.CurrentIntervalMS, mMinStepSEC);
        //             if (!UnitUpdatePosAction.VectorEqual(mLocalPos.Position, lastSend.pos, epsilon) ||
        //                 !UnitUpdatePosAction.FloatEqual(mDirection.Direction, lastSend.direction, epsilon) ||
        //                 sending.mainState != lastSend.mainState ||
        //                 mSendPos?.subst != lastSend.subst)
        //             {
        //                 return true;
        //             }
        //             return false;
        //         }
        protected virtual bool IsNeedFixUpdatePos(in Vector3 localPos, in float dir, UnitActionStatus status, string subst, UnitUpdatePosAction lastSend)
        {
            if (lastSend == null) return true;

            float epsilon = MoveHelper.GetDistance(this.Parent.CurrentIntervalMS, mMinStepSEC);

            if (lastSend.pos.HasValue && !UnitUpdatePosAction.VectorEqual(localPos, lastSend.pos.Value, epsilon))
            {
                return true;
            }
            if (lastSend.direction.HasValue && !UnitUpdatePosAction.FloatEqual(dir, lastSend.direction.Value, epsilon))
            {
                return true;
            }
            if (status != lastSend.mainState || subst != lastSend.subst)
            {
                return true;
            }
            return false;
        }

        #endregion
        //-------------------------------------------------------------------------------------------


        protected void ClearSendPos()
        {
            mLastSendPos.Value = null;
        }
        public void ClearSendAxis()
        {
            this.mSendingAxis.Value = null;
            this.mSendingPos.Value = null;
            this.mSendingCustomAxis.Value = null;
        }
        /// <summary>
        /// 每次 UpdateAI 结束时清空本次输入，子类可覆写以改变清空时机
        /// </summary>
        protected virtual void OnClearFrameInputs()
        {
            this.mSendingAxis.Value = null;
            this.mSendingPos.Value = null;
            this.mSendingCustomAxis.Value = null;
        }


        //-------------------------------------------------------------------------------------------

        protected virtual void DoPlayerForceSyncPosEvent(UnitForceSyncPosEvent e)
        {
            var updatePosAction = ObjectPool.Alloc<UnitUpdatePosAction>().Init(this.ObjectID, e.Position, e.Direction, e.BodyDirection, (UnitActionStatus)e.UnitMainState);
            this.SendAction(updatePosAction);
            this.ClearSendPos();
            this.ForceSyncPos(e.Position);

            mDirection.ForceSync(e.Direction, e.BodyDirection);
            mRemoteSyncPos.Direction = e.Direction;
            mRemoteSyncPos.BodyDirection = e.BodyDirection;

            this.mRemoteState.UnitMainState = (UnitActionStatus)e.UnitMainState;
            this.mRemoteState.UnitSubState = e.UnitSubState;
            this.LayerUpward = e.LayerUpward;
            this.ForceSyncCurrentState((UnitActionStatus)e.UnitMainState, e.UnitSubState, e);

            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                if (RemoteStatus != UnitActionStatus.Skill)
                {
                    clearSkillAction();
                }
            }
        }
        protected virtual void DoPlayerForceSyncStateEvent(UnitForceSyncStateEvent e)
        {
            this.SendAction(ObjectPool.Alloc<UnitUpdatePosAction>().Init(this.ObjectID, this.Position, this.Direction, this.BodyDirection, (UnitActionStatus)e.UnitMainState));
            this.mRemoteState.UnitMainState = (UnitActionStatus)e.UnitMainState;
            this.mRemoteState.UnitSubState = e.UnitSubState;
            this.ForceSyncCurrentState((UnitActionStatus)e.UnitMainState, e.UnitSubState, e);
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                if (RemoteStatus != UnitActionStatus.Skill)
                {
                    clearSkillAction();
                }
            }
        }
        protected virtual void DoPlayerGuardEvent(PlayerGuardEvent e)
        {
            if (e.guard) clearAgents();
            if (e.guard != this.IsGuard)
            {
                if (e.guard == false && Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
                {
                    var inAir = IsInAir;
                    if (CurrentState != UnitActionStatus.Jump)
                    {
                        if (Math.Abs(mLocalPos.Z - mLocalPos.Upward) <= Parent.Terrain3D.StepIntercept)
                        {
                            mLocalPos.SetPos(new Geometry.Vector3(mLocalPos.X, mLocalPos.Y, mLocalPos.Upward));
                        }
                    }
                }
            }
            this.IsGuard = e.guard;
        }


        public override void ForceSyncPos(in Geometry.Vector3 pos)
        {
            mRemotePos.X = pos.X;
            mRemotePos.Y = pos.Y;
            mRemotePos.Z = pos.Z;
            mLocalPos.SetPos(pos);
        }

        protected override void UpdatePos(float intervalMS)
        {
            if (HostObject is IZoneUnit hostUnit)
            {
                base.UpdatePos(intervalMS);               
            }
            else if ((IsGuard))
            {
                base.UpdatePos(intervalMS);
            }
            else if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
            {
                //if (!HasSkillMove)
                {
                    mLocalPos.Update(mRemotePos.ToGeometry3(), intervalMS);
                }
            }
        }

        public override void SyncPos(UnitSyncPos pos)
        {
            if (HostObject is IZoneUnit hostUnit)
            {
                this.mRemoteSyncPos.Sync(pos);
                return;
            }
            if (pos.HasModifer(UnitSyncModifer.LayerUpward))
            {
                this.LayerUpward = pos.LayerUpward;
            }
            this.mRemoteSyncPos.Sync(pos);
            if ((IsGuard))
            {
                if (pos.HasModifer(UnitSyncModifer.Direction))
                {
                    base.mDirection.SyncFace(pos.Direction, true);
                }
                if (pos.HasModifer(UnitSyncModifer.BodyRotation))
                {
                    base.mDirection.SyncBody(pos.BodyDirection, true);
                }
                base.SyncPos(pos);
            }
            else
            {
                if (pos.HasModifer(UnitSyncModifer.Posistion))
                {
                    base.mRemotePos.X = pos.X;
                    base.mRemotePos.Y = pos.Y;
                    base.mRemotePos.Z = pos.Z;
                }
                switch (Parent.ActorSyncMode)
                {
                    case SyncMode.ForceByServer:
                        sync_pos_ForceByServer(pos);
                        break;
                    case SyncMode.MoveByClient_PreSkillByClient:
                        sync_pos_MoveByClient_PreSkillByClient(pos);
                        break;
                }
                this.SyncState(pos);
            }
        }

        protected override void SyncState(UnitSyncPos st)
        {
            this.mRemoteState.Sync(st);
            if (HostObject is IZoneUnit hostUnit)
            {
                base.SyncState(st);
            }
            else if ((IsGuard))
            {
                this.SyncCurrentState(st);
            }
            else
            {
                base.SyncCurrentSubState(st);
                switch (Parent.ActorSyncMode)
                {
                    case SyncMode.ForceByServer:
                        sync_state_ForceByServer(st);
                        break;
                    case SyncMode.MoveByClient_PreSkillByClient:
                        sync_state_MoveByClient_PreSkillByClient(st);
                        break;
                }
            }
        }
        private void sync_state_ForceByServer(UnitSyncPos st)
        {
            this.SyncCurrentState(st);
        }
        private void sync_pos_ForceByServer(UnitSyncPos pos)
        {
            // 强同步 //
            if (pos.HasModifer(UnitSyncModifer.Posistion))
            {
                base.mLocalPos.SetPos(pos.X, pos.Y, pos.Z);
            }
            if (pos.HasModifer(UnitSyncModifer.Direction))
            {
                base.mDirection.SyncFace(pos.Direction);
            }
            if (pos.HasModifer(UnitSyncModifer.BodyRotation))
            {
                base.mDirection.SyncBody(pos.BodyDirection);
            }
        }

        private void sync_state_MoveByClient_PreSkillByClient(UnitSyncPos st)
        {
            if (CurrentState.IsControllable())
            {
                if (st.HasModifer(UnitSyncModifer.MainState))
                {
                    if (RemoteStatus == UnitActionStatus.Skill)
                    {
                        this.SyncCurrentState(st);
                    }
                    else if (RemoteStatus == UnitActionStatus.ClientCustom)
                    {
                        this.SyncCurrentState(st);
                    }
                    else if (RemoteStatus == UnitActionStatus.Climb)
                    {
                        this.SyncCurrentState(st);
                    }
                    else if (RemoteStatus.NotControllable())
                    {
                        this.SyncCurrentState(st);
                    }
                }
            }
            else
            {
                this.SyncCurrentState(st);
            }
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                if (RemoteStatus != UnitActionStatus.Skill)
                {
                    clearSkillAction();
                }
            }
        }
        private void sync_pos_MoveByClient_PreSkillByClient(UnitSyncPos pos)
        {
            if (CurrentState.IsControllable())
            {
                if (RemoteStatus == UnitActionStatus.Skill)
                {
                }
                else if (RemoteStatus == UnitActionStatus.Climb)
                {
                }
                else if (RemoteStatus.NotControllable())
                {
                    if (pos.HasModifer(UnitSyncModifer.Direction))
                    {
                        base.mDirection.SyncFace(pos.Direction);
                    }
                }
            }
            else
            {
                // 强同步 //
                if (RemoteStatus != UnitActionStatus.Skill && RemoteStatus.NotControllable())
                {
                    if (pos.HasModifer(UnitSyncModifer.Direction))
                    {
                        base.mDirection.SyncFace(pos.Direction);
                    }
                }
            }
            if (pos.HasModifer(UnitSyncModifer.BodyRotation))
            {
                base.mDirection.SyncBody(pos.BodyDirection);
            }
        }

        //--------------------------------------------------------------------------------------------------------
    }
}



