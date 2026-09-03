using DeepCore.EventTrigger;
using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer : LayerUnit
    {
        public override bool TouchObj
        {
            get
            {
                if (Parent.CFG.PLAYER_NONE_TOUCH)
                    return false;
                return base.TouchObj;
            }
        }

        /// <summary>
        /// 单位释放技能时，自动朝向锁定敌人
        /// </summary>
        public bool IsSkillAutoFocusTarget { get; set; }

        /// <summary>
        /// 当前是否为服务端托管
        /// </summary>
        public bool IsGuard { get; protected set; }

        /// <summary>
        /// 选中的目标单位
        /// </summary>
        public uint TargetUnitID => mLastFocusTarget;

        //public PlayerFocuseTargetEvent LastFocusTarget { get { return mFocusTarget; } }
        /// <summary>
        /// 进入场景推送过来的数据
        /// </summary>
        public LockActorEvent LoginData { get { return mLoginData; } }

        public bool IsReady { get; private set; } = false;
        private readonly LockActorEvent mLoginData;
        protected readonly ILayerPlayerPosition mPlayerLocalPos;
        protected readonly LayerEnvironmentMap mPlayerEnvironmentVarMap;
        public LayerEnvironmentMap PlayerEnvironmentVarMap => mPlayerEnvironmentVarMap;
        //private PlayerFocuseTargetEvent mFocusTarget;
        private uint mLastFocusTarget;
        public override Vector3 Position => mLocalPos.Position;
        public override float X { get { return mLocalPos.X; } }
        public override float Y { get { return mLocalPos.Y; } }
        public override float Z { get { return mLocalPos.Z; } }

        private Action<Agent.AbstractAgent> agentUpdateAction;
        private Action<Agent.AbstractAgent> agentUpdateAIAction;

        public LayerPlayer(UnitInfo unit, LockActorEvent info, LayerZone parent)
            : base(unit, info.UnitData, parent, null, info.sender)
        {
            this.mPlayerEnvironmentVarMap = new(parent);
            this.mPlayerLocalPos = base.mLocalPos as ILayerPlayerPosition;
            this.mMinStepSEC = CFG.OBJECT_MOVE_TO_MIN_STEP_SEC;
            this.mElasticAngle = CMath.AngleToRadian(CFG.OBJECT_MOVE_BLOCK_ELASTIC_ANGLE);
            this.mCurrentSyncMode = info.ClientSyncMode;
            this.mLoginData = info;
            this.ResetSkills();
            this.ResetItems();
            if (info.CurrentUnitVars != null)
            {
                foreach (var var in info.CurrentUnitVars)
                {
                    mEnvironmentVarMap.TrySet(var, out var k, out var v);
                }
            }
            if (info.CurrentPlayerVars != null)
            {
                foreach (var var in info.CurrentPlayerVars)
                {
                    mPlayerEnvironmentVarMap.TrySet(var, out var k, out var v);
                }
            }

            //this._SendActionInterval.FirstTimeEnable = false;

            agentUpdateAction = MAgents_OnEndUpdate;
            agentUpdateAIAction = MAgents_OnBeginUpdate;
        }
        protected override void Disposing()
        {
            this.clearAgents();
            this.clearSkillAction();
            base.Disposing();
            this.event_OnGuardFocusTarget = null;
            this.event_OnUnitLaunchSkill = null;
        }
        protected internal override void OnAdded()
        {
            base.OnAdded();
            if (mLoginData.Skills != null)
            {
                this.DoEvent(mLoginData.Skills);
            }
            InternalSyncObject(mLoginData.UnitData);
            SyncAuraStatus(mLoginData.UnitData.CurrentAuraStatus);
            SyncBuffStatus(mLoginData.UnitData.CurrentBuffStatus);
            SyncCardStatus(mLoginData.UnitData.CurrentCardStatus);
            SyncSkillStatus(mLoginData.CurrentSkillStatus);
            SyncItems(mLoginData.CurrentItemStatus);
            this.mCurrentSyncMode = Parent.ActorSyncMode;
            //Parent.SendAction(new UnitSetSyncModeAction(this.ObjectID, Parent.ActorSyncMode));
        }

        //-------------------------------------------------------------------------------------------

        /// <summary>
        /// 截获后端发送的事件
        /// </summary>
        /// <param name="e"></param>
        internal protected override void DoEvent(ObjectNotify e)
        {
            if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
            {
                //忽略Jump，客户端本地PreSkillClient已经处理位移//
                if (e is UnitJumpEvent)
                {
                    return;
                }
            }
            if (e is PlayerGuardEvent)
            {
                DoPlayerGuardEvent(e as PlayerGuardEvent);
            }
            else if (e is PlayerSkillStopEvent)
            {
                DoClearSkillActionEvent(e as PlayerSkillStopEvent);
            }
            else if (e is UnitForceSyncPosEvent)
            {
                DoPlayerForceSyncPosEvent(e as UnitForceSyncPosEvent);
            }
            else if (e is UnitForceSyncStateEvent)
            {
                DoPlayerForceSyncStateEvent(e as UnitForceSyncStateEvent);
            }
            else if (e is UnitSkillActionChangeEvent)
            {
                DoPlayerSkillActionChangeEvent(e as UnitSkillActionChangeEvent);
            }
            else if (e is UnitSyncInventoryItemEvent)
            {
                DoPlayerSyncItem(e as UnitSyncInventoryItemEvent);
            }
            else if (e is UnitUseItemEvent)
            {
                DoPlayerUseItem(e as UnitUseItemEvent);
            }
            else if (e is PlayerFocuseTargetEvent)
            {
                DoPlayerFocuseTargetEvent(e as PlayerFocuseTargetEvent);
            }
            else if (e is PlayerSyncEnvironmentVarEvent)
            {
                DoSyncPlayerVarEvent(e as PlayerSyncEnvironmentVarEvent);
            }
            base.DoEvent(e);
        }
        //-----------------------------------------------------------------------------------------------------------
        protected override void DoStopPick(UnitStopPickObjectEvent pick)
        {
            SendAction(ObjectPool.Alloc<UnitUpdatePosAction>().Init(this.ObjectID, this.mLocalPos.Position, Direction, BodyDirection, UnitActionStatus.Idle));
            this.mRemoteState.UnitMainState = UnitActionStatus.Idle;
            base.SyncCurrentState(mRemoteState);
            base.PreSetCurrentMainState(UnitActionStatus.Idle, null, pick);
            base.DoStopPick(pick);
        }
        protected override void DoDamage(UnitDamageEvent e)
        {
            //base.ForceSyncPos();
            base.DoDamage(e);
        }
        protected virtual void DoSyncPlayerVarEvent(PlayerSyncEnvironmentVarEvent e)
        {
            if (e.Var != null && mPlayerEnvironmentVarMap.TrySet(e.Var, out var k, out var v))
            {
                mOnEnvironmentVarChanged?.Invoke(this, k, v);
            }
        }
        //-------------------------------------------------------------------------------------------

        protected override void Update()
        {
            UpdateItems(Parent.CurrentIntervalMS);
            ForEachAgent(agentUpdateAction);
            base.Update();
        }

        //-------------------------------------------------------------------------------------------

        protected override void UpdateAI()
        {
            ForEachAgent(agentUpdateAIAction);
            var intervalMS = Parent.CurrentIntervalMS;
            if (IsGuard)
            {
                update_ai_Guard(intervalMS);
            }
            else
            {
                if (mCurrentSyncMode != Parent.ActorSyncMode)
                {
                    mCurrentSyncMode = Parent.ActorSyncMode;
                    SendAction(ObjectPool.Alloc<UnitSetSyncModeAction>().Init(this.ObjectID, Parent.ActorSyncMode));
                }
                switch (Parent.ActorSyncMode)
                {
                    case SyncMode.ForceByServer:
                        update_ai_ForceByServer(intervalMS);
                        break;
                    case SyncMode.MoveByClient_PreSkillByClient:
                        update_ai_MoveByClient_PreSkillByClient(intervalMS);
                        break;

                }
            }

            updateCustomAxisAction(intervalMS);
            updateSkillAction(intervalMS);
            OnClearFrameInputs();
        }

   

        //-----------------------------------------------------------------------------------------------------------


        public virtual void SendAction(ObjectAction act)
        {
            Parent.SendAction(act);
        }
        public virtual void SendRequest(ActorRequest req, LayerZone.OnResponseHandler handler, int timeOutMS = 15000)
        {
            Parent.SendRequest(req, handler, timeOutMS);
        }
        public virtual void SendActorRequest<TRsp>(ActorRequest req, LayerZone.OnResponseHandler<TRsp> handler, int timeOutMS = 15000) where TRsp : ActorResponse
        {
            if (handler != null)
            {
                Parent.SendRequest(req, (rsp) => { handler(rsp, rsp.ResponseMessage as TRsp); }, timeOutMS);
            }
            else
            {
                Parent.SendRequest(req, null);
            }
        }

        public virtual void SendReady()
        {
            if (!IsReady)
            {
                IsReady = true;
                SendAction(ObjectPool.Alloc<UnitReadyAction>().Init(base.ObjectID));
            }
        }

        public virtual void SendUnitAxisAngle(float angle, float distanceRate, float faceto)
        {
            this.mSendingPos.Value = null;
            if (IsGuard) return;
            if (IsLock) return;
            UnitAxisAction ma = ObjectPool.Alloc<UnitAxisAction>().Init(base.ObjectID);
            //ma.st = st;
            ma.angle = angle;
            ma.distanceRate = distanceRate;
            ma.faceto = faceto;
            this.mSendingAxis.Value = ma;
        }
        public virtual void SendUnitAxisAngle(UnitAxisAction ma)
        {
            this.mSendingPos.Value = null;
            if (IsGuard) return;
            if (IsLock) return;
            this.mSendingAxis.Value = ma;
        }
        public virtual bool SendCustomMovePos(float angle, float distanceRate, float faceto)
        {
            this.mSendingPos.Value = null;
            if (IsLock) return false;
            UnitCustomAxisAction ma = ObjectPool.Alloc<UnitCustomAxisAction>().Init(base.ObjectID);
            ma.angle = angle;
            ma.distanceRate = distanceRate;
            ma.faceto = faceto;
            this.mSendingCustomAxis.Value = ma;
            return true;
        }

        public virtual void SendUnit3DAxisAngle(float angle, float distanceRate, float faceto, float zspeed, float xyspeed = 0)
        {
            this.mSendingPos.Value = null;
            if (IsGuard) return;
            if (IsLock) return;
            var ma = ObjectPool.Alloc<UnitAxis3DAction>().Init3D(base.ObjectID);
            {
                ma.angle = angle;
                ma.distanceRate = distanceRate;
                ma.faceto = faceto;
                ma.ZControlSpeed = zspeed;
                ma.XYControlSpeed = xyspeed;
            }
            this.mSendingAxis.Value = ma;
        }

        public virtual bool SendUpdatePos(Geometry.Vector3? pos,
            float? direction = null,
            float? bodyDirection = null,
            UnitActionStatus? st = null,
            string subst = null)
        {
            this.mSendingAxis.Value = null;
            if (IsGuard) return false;
            if (IsLock) return false;
            this.mSendingPos.Value = ObjectPool.Alloc<UnitUpdatePosAction>().Init(this.ObjectID, pos, direction, bodyDirection, st, subst);
            return true;
        }



        public virtual void SendJump(float direction, float distanceRate)
        {
            if (IsLock) return;
            SendJump(direction, distanceRate, null);
        }

        public virtual void SendJump(float direction, float distanceRate, float? zspeed)
        {
            if (IsLock) return;
            if (AUnitMotion)
            {
                if (CurrentState.IsControllable())
                {
                    var jump = ObjectPool.Alloc<UnitJumpAction>().Init(ObjectID, direction, distanceRate, zspeed);
                    SendAction(jump);
                    if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
                    {
                        this.StartFly(zspeed.HasValue ? zspeed.Value : AUnitMotion.JumpZSpeed);
                    }
                    SendUnitAxisAngle(direction, distanceRate, direction);
                }
            }
        }
        public virtual void SendFall(float direction, float distanceRate)
        {
            if (IsLock) return;
            SendFall(direction, distanceRate, null);
        }

        public virtual void SendFall(float direction, float distanceRate, float? zspeed)
        {
            if (IsLock) return;
            if (AUnitMotion)
            {
                if (CurrentState.IsControllable())
                {
                    var jump = ObjectPool.Alloc<UnitJumpAction>().Init(ObjectID, direction, distanceRate, zspeed);
                    SendAction(jump);
                    if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
                    {
                        this.StartFly(
                          zspeed.HasValue ? zspeed.Value : -AUnitMotion.JumpZSpeed);
                    }
                    SendUnitAxisAngle(direction, distanceRate, direction);
                }
            }
        }

        public virtual void SendCustomAction(string custom_substate = null)
        {
            var ma = ObjectPool.Alloc<UnitClientCustomMoveAction> ();
            ma.SubState = custom_substate;
            SendAction(ma);
        }

        public virtual void SendClimbAction(Geometry.Vector3 position, Geometry.Quaternion rotation, float direction)
        {
            if (IsLock) return;
            var climb = ObjectPool.Alloc<UnitClimbAction>().Init(ObjectID, position, direction, rotation);
            SendAction(climb);
        }

        public virtual void SendSomersaultAction(float direction)
        {
            if (IsLock) return;
            if (CurrentState.IsControllable())
            {
                var a = ObjectPool.Alloc<UnitStartSomersaultAction>().Init(ObjectID, direction);
                SendAction(a);

                if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient)
                {
                    // Client Simulate
                }
            }

            //ClearClientLocalSkill();
        }

        public virtual void SendStopSomersaultAction()
        {
            var a = ObjectPool.Alloc<UnitStopSomersaultAction>().Init(ObjectID);
            SendAction(a);
        }

        public virtual void SendUnitFaceTo(float d)
        {
            this.mSendingFaceTo.Value = ObjectPool.Alloc<UnitFaceToAction>().Init(base.ObjectID, d);
            SendAction(mSendingFaceTo);
        }
        public virtual void SendUnitFaceTo(float wx, float wy)
        {
            float d = (float)Math.Atan2(wy - this.Y, wx - this.X);
            this.mSendingFaceTo.Value = ObjectPool.Alloc<UnitFaceToAction>().Init(base.ObjectID, d);
            SendAction(mSendingFaceTo);
        }
        /*
        public void SendUnitMove(float x, float y)
        {
            mSendAxis = null;
            mSendPos = null;
            if (!IsGuard) return;
            if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient) return;
            UnitMoveAction ma = new UnitMoveAction(base.ObjectID, x, y);
            Parent.SendAction(ma);
        }
        */

        public virtual void SendUnitStopMove()
        {
            ClearSendAxis();
            UnitStopMoveAction ma = ObjectPool.Alloc<UnitStopMoveAction>().Init(base.ObjectID);
            SendAction(ma);
        }

        /*
        public void SendUnitSlip(float x, float y)
        {
            mSendAxis = null;
            mSendPos = null;
            if (Parent.ActorSyncMode == SyncMode.MoveByClient_PreSkillByClient) return;
            UnitSlipAction ma = new UnitSlipAction(base.ObjectID, x, y);
            Parent.SendAction(ma);
        }*/
        public LayerUnit.SkillState SendUnitLaunchSkillByIndex(int skillIndex, LayerZoneObject target)
        {
            {
                var status = GetSkillStatus();
                if (skillIndex >= 0 && skillIndex < status.Count)
                {
                    var ss = status[skillIndex];
                    SendUnitLaunchSkill(ss.Data.ID, target.ObjectID);
                    return ss;
                }
            }
            return null;
        }
        public LayerUnit.SkillState SendUnitLaunchSkillByIndex(int skillIndex, Geometry.Vector3? spellTargetPos)
        {
            {
                var status = GetSkillStatus();
                if (skillIndex >= 0 && skillIndex < status.Count)
                {
                    var ss = status[skillIndex];
                    SendUnitLaunchSkill(ss.Data.ID, spellTargetPos);
                    return ss;
                }
            }
            return null;
        }
        public virtual void SendUnitLaunchSkill(UnitLaunchSkillRequest launch, LayerZone.OnResponseHandler<UnitLaunchSkillResponse> callback = null)
        {
            callback_OnUnitLaunchSkill(launch);
            var skill = this.GetSkillState(launch.SkillID);
            var stopMove = true;
            if (skill != null)
            {
                UnitActionData preActionData = null;
                if (CurrentSkillAction is PreSkillByClient)
                {
                    var pc = (PreSkillByClient)CurrentSkillAction;
                    var nextIndex = pc.CurrentActionIndex + 1;
                    if (pc.SkillData.ActionQueue.Count > nextIndex)
                    {
                        preActionData = pc.SkillData.ActionQueue[nextIndex];
                    }
                }
                else
                {
                    preActionData = skill.Data.ActionQueue.Count > 0 ? skill.Data.ActionQueue[0] : null;
                }
                if (preActionData != null && preActionData.IsControlMoveable)
                {
                    stopMove = false;
                }
                CheckAndUpdateSendPos(CurrentState, null);
                SendActorRequest<UnitLaunchSkillResponse>(launch, callback);
            }
            else
            {
                callback?.Invoke(null, null);
            }
            if (stopMove)
            {
                ClearSendAxis();
            }
        }
        public virtual void SendUnitLaunchSkill(int skillID, uint targetObjectID)
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, skillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            launch.TargetObjID = targetObjectID;
            SendUnitLaunchSkill(launch);
        }
        public virtual void SendUnitLaunchSkill(int skillID, uint targetObjectID, uint withObj)
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, skillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            launch.TargetObjID = targetObjectID;
            launch.RelatedPetId = withObj;
            SendUnitLaunchSkill(launch);
        }
        public virtual void SendUnitLaunchSkill(int skillID)
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, skillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            SendUnitLaunchSkill(launch);
        }
        public virtual void SendUnitLaunchSkill(int skillID, Geometry.Vector3? spellTargetPos)
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, skillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            launch.SpellTargetPos = spellTargetPos;
            SendUnitLaunchSkill(launch);
        }
        public virtual void SendUnitLaunchNormalAttack()
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, BaseSkillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            SendUnitLaunchSkill(launch);
        }
        public virtual void SendUnitLaunchNormalAttack(Geometry.Vector3? spellTargetPos)
        {
            UnitLaunchSkillRequest launch = new UnitLaunchSkillRequest(base.ObjectID, BaseSkillID);
            launch.IsAutoFocusNearTarget = IsSkillAutoFocusTarget;
            launch.SpellTargetPos = spellTargetPos;
            SendUnitLaunchSkill(launch);
        }

        public virtual void SendUnitGuard(bool auto)
        {
            ClearSendAxis();
            this.IsGuard = auto;
            SendAction(ObjectPool.Alloc<UnitGuardAction>().Init(ObjectID, auto));
        }
        public virtual void SendUnitGuard(bool auto, string reason)
        {
            this.mSendingAxis.Value = null;
            this.mSendingPos.Value = null;
            this.IsGuard = auto;
            SendAction(ObjectPool.Alloc<UnitGuardAction>().Init(ObjectID, auto, reason));
        }

        public virtual void SendUnitAttackMoveTo(Geometry.Vector3? target, string name, bool attack = false)
        {
            UnitAttackToAction ma = ObjectPool.Alloc<UnitAttackToAction>().Init(base.ObjectID, target, name, attack);
            SendAction(ma);
        }
        public virtual void SendUnitFocuseTarget(uint targetID)
        {
            var ma = ObjectPool.Alloc<UnitFocuseTargetAction>().Init(base.ObjectID, targetID);
            SendAction(ma);
        }
        public virtual void SendUnitFolloweTarget(uint targetID, bool attack = false, float minD = 0, float maxD = 0, float tpD = 0, int slot = 0)
        {
            var ma = ObjectPool.Alloc<UnitFollowTargetAction>().Init(base.ObjectID, targetID);
            ma.autoAttack = attack;
            ma.minDistance = minD;
            ma.maxDistance = maxD;
            ma.tpDistance = tpD;
            ma.slotIndex = slot;
            this.IsGuard = targetID != 0;
            SendAction(ma);
        }

        public virtual void SendUnitUseItem(int index, int count = 1)
        {
            UnitUseItemAction use = ObjectPool.Alloc<UnitUseItemAction>().Init(base.ObjectID, index, count);
            SendAction(use);
        }
        public LayerPlayer.ItemSlot SendUnitUseItemByIndex(int itemIndex)
        {
            {
                var items = GetItemSlots();
                if (itemIndex >= 0 && itemIndex < items.Count)
                {
                    var slot = items[itemIndex];
                    SendUnitUseItem(itemIndex);
                    return slot;
                }
            }
            return null;
        }

        public virtual void SendUnitPickObject(uint pickableID)
        {
            this.mSendingAxis.Value = null;
            this.mSendingPos.Value = null;
            UnitPickObjectAction act = ObjectPool.Alloc<UnitPickObjectAction>().Init (ObjectID, pickableID);
            SendAction(act);
        }
        public virtual void SendUnitStopPick(string reason)
        {
            this.mSendingAxis.Value = null;
            this.mSendingPos.Value = null;
            UnitStopPickObjectAction act = ObjectPool.Alloc<UnitStopPickObjectAction>().Init (ObjectID, reason);
            SendAction(act);
        }

        public virtual void SendCancelBuff(int buffID)
        {
            BuffState bs = GetBuff(buffID);
            if (bs != null)
            {
                UnitCancelBuffAction act = ObjectPool.Alloc<UnitCancelBuffAction>().Init (ObjectID, bs.Data.ID);
                SendAction(act);
            }
        }

        public virtual void SendSetSubState(string substate)
        {
            UnitSetSubStateAction use = ObjectPool.Alloc<UnitSetSubStateAction>().Init (base.ObjectID, substate);
            SendAction(use);
        }
        public virtual void SendPlayerSetEnvVar(string key, object value)
        {
            var use = ObjectPool.Alloc<PlayerSetEnvVarAction>().Init (base.ObjectID, key, value);
            SendAction(use);
        }
        //--------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获得当前单位服务端可同步环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetPlayerEnvironmentVar(string key)
        {
            return mPlayerEnvironmentVarMap.GetEnvironmentVar(key);
        }

        /// <summary>
        /// 获得当前单位服务端可同步环境变量列表
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<string> ListPlayerEnvironmentVars()
        {
            return mPlayerEnvironmentVarMap.Keys;
        }
        public IEnumerable<KeyValuePair<string, object>> ListPlayerEnvironmentValues()
        {
            return mPlayerEnvironmentVarMap.ListEnvironmentValues();
        }
        //--------------------------------------------------------------------------------------------------------
        #region Events




        /// <summary>
        /// 自动战斗锁定目标
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="target"></param>
        /// <param name="expect"></param>
        public delegate void OnGuardFocusTargetHandler(LayerPlayer actor, LayerZoneObject target, SkillTemplate.CastTarget expect);
        [EventTriggerDescAttribute("单位持有技能发生变化时触发")]
        public event OnGuardFocusTargetHandler OnGuardFocusTarget { add { event_OnGuardFocusTarget += value; } remove { event_OnGuardFocusTarget -= value; } }
        private OnGuardFocusTargetHandler event_OnGuardFocusTarget;
        protected virtual void DoPlayerFocuseTargetEvent(PlayerFocuseTargetEvent e)
        {
            if (this.TargetUnitID != e.targetUnitID)
            {
                this.mLastFocusTarget = e.targetUnitID;
                var target = Parent.GetObject(e.targetUnitID);
                if (event_OnGuardFocusTarget != null)
                {
                    event_OnGuardFocusTarget.Invoke(this, target, e.expectTarget);
                }
            }
            this.mLastFocusTarget = e.targetUnitID;
        }


        /// <summary>
        /// 自动战斗锁定目标
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="target"></param>
        /// <param name="expect"></param>
        public delegate void OnUnitLaunchSkillHandler(LayerPlayer actor, UnitLaunchSkillRequest launch);
        [EventTriggerDescAttribute("单位准备释放技能时触发")]
        public event OnUnitLaunchSkillHandler OnUnitLaunchSkill { add { event_OnUnitLaunchSkill += value; } remove { event_OnUnitLaunchSkill -= value; } }
        private OnUnitLaunchSkillHandler event_OnUnitLaunchSkill;
        protected void callback_OnUnitLaunchSkill(UnitLaunchSkillRequest e)
        {
            event_OnUnitLaunchSkill?.Invoke(this, e);
        }


        #endregion
        //--------------------------------------------------------------------------------------------------------
    }
}



