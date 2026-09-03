using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.ClientFocusAction;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 可自动战斗的可操作单位
    /// </summary>
    public partial class InstancePlayer
    {
        protected UnitLaunchSkillRequest mWaitingSkill;
        protected StatePlayerControlMove mControlMove;
        protected StatePlayerCustomControlMove mCustomControlMove;
        //protected IStateControllable mControlJump;
        protected StatePlayerUpdateMove mControlUpdateMove;
        protected StatePlayerCustomAction mCustomAction;
        protected StatePlayerClimb mClimb;

        protected virtual ActorResponse onRequest(ActorRequest req)
        {
            switch (req)
            {
                case UnitGetStatisticRequest req_st:
                    return doGetStatisticRequest(req_st);
                case UnitLaunchSkillRequest req_sk:
                    return doLaunchSkill(req_sk);
            }
            return null;
        }
        protected override void onAction(ObjectAction act)
        {
            if (!this.Enable) { return; }


            if (act is ActorRequest req)
            {
                var rsp = onRequest(req);
                if (rsp != null)
                {
                    Zone.PostActorResponse(this, req, rsp);
                }
            }
            if (CheckUnitStatusBeforeDoAction())
            {
                if (act is UnitUpdatePosAction actPos)
                {
                    if (actPos.pos.HasValue)
                    {
                        var pos = actPos.pos.Value;
                        Parent.TryUpdatePos(this, ref pos, out var layer);
                        actPos.pos = pos;
                    }
                }
                this.mCustomAction.OnControlAction(act);
                this.mControlMove.OnControlAction(act);
                this.mCustomControlMove.OnControlAction(act);
                this.mControlUpdateMove.OnControlAction(act);
                //this.mControlJump.OnControlAction(act);
                this.mClimb.OnControlAction(act);

                switch (act)
                {
                    case UnitUpdatePosAction updatePos:
                        doUnitUpdatePos(updatePos);
                        break;
                    case UnitAxisAction axis:
                        doAxis(axis);
                        break;
                    case UnitClientCustomMoveAction custom:
                        doCustomMove(custom);
                        break;
                    case UnitFaceToAction faceTo:
                        doFaceTo(faceTo);
                        break;
                    case UnitJumpAction jump:
                        doJump(jump);
                        break;
                    case UnitClimbAction climb:
                        doClimb(climb);
                        break;
                    case UnitStopMoveAction stopMove:
                        doStopMove(stopMove);
                        break;
                    case UnitCancelSkillRequest cancelSkill:
                        doCancelSkill(cancelSkill);
                        break;
                    case UnitUseItemAction useItem:
                        doUseItem(useItem);
                        break;
                    case UnitPickObjectAction pickObj:
                        doPickObject(pickObj);
                        break;
                    case UnitStopPickObjectAction stopPickObj:
                        doStopPickObject(stopPickObj);
                        break;
                    case UnitCancelBuffAction cancelBuff:
                        doCancelBuff(cancelBuff);
                        break;
                    case UnitSetSubStateAction subState:
                        doUnitSetSubStateAction(subState);
                        break;
                }
            }
            switch (act)
            {
                case UnitReadyAction ready:
                    doReady(ready);
                    break;

                case UnitGuardAction guard:
                    doGuard(guard);
                    break;
                case UnitAttackToAction attackTo:
                    //mGuard?.DoAttackTo(attackTo);
                    break;
                case UnitFollowTargetAction followTarget:
                    //mGuard?.DoFollowTarget(followTarget);
                    break;
                case UnitFocuseTargetAction focusTarget:
                    CurrentTargetID = focusTarget.targetUnitID;
                    //mGuard?.DoFocusTarget(focusTarget);
                    break;

                case ChatAction chat:
                    doChat(chat);
                    break;
                case UnitSetSyncModeAction syncMode:
                    doSetSyncMode(syncMode);
                    break;
                case UnitGetStatisticRequest statistic:
                    doGetStatisticRequest(statistic);
                    break;
                case ComponentFieldChangeAction compFields:
                    //        Components.SyncComponentFields(compFields.ComponentTag, compFields.Fields, false, false);
                    break;
                case PlayerSetEnvVarAction setVar:
                    doPlayerSetEnvVarAction(setVar);
                    break;
            }
        }

        protected virtual bool CheckUnitStatusBeforeDoAction()
        {
            return !IsDead;
        }

        protected virtual void doReady(UnitReadyAction act)
        {
            Parent.QueueTask(this, (z, st) =>
            {
                if (st.IsReady == false)
                {
                    st.IsReady = true;
                    //st.Components.ForceSyncAllComponentFields();
                    st.Parent.cb_playerReady(st, act.info);
                }
            });
        }
        /*
        protected virtual void doMove(UnitMoveAction ma)
        {
            if (CurrentState is StateSkill)
            {
                StateSkill ss = CurrentState as StateSkill;
                ss.setMoveTo(ma.x, ma.y);
            }
            else
            {
                cleanFocus();
                startMoveTo(ma.x, ma.y);
            }
        }
        */
        protected virtual void doAxis(UnitAxisAction ma)
        {
            if (IsGuard)
            {
                return;
            }
            if (CurrentState is StateSkill)
            {
                StateSkill ss = CurrentState as StateSkill;
                if (ma.distanceRate != 0 && ss.IsCancelableByMove)
                {
                    if (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient)
                    {
                        ss.block(mControlMove);
                    }
                    else
                    {
                        ss.block(mControlUpdateMove);
                    }
                }
                else
                {
                    ss.controlMoveTo(ma);
                }
            }
            else if (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient)
            {
                //                 if (CurrentState is StateControlJump)
                //                 {
                // 
                //                 }
                //                 else 
                if (ma.distanceRate != 0 /*|| (IsZeroGravityFlyStarted && ma is UnitAxis3DAction axis3D && axis3D.ZControlSpeed != 0)*/)
                {
                    ChangeState(mControlMove);
                }
                else
                {
                    FaceTo(ma.faceto);
                    DoSomething();
                }
            }
            //             else
            //             {
            //                 cleanFocus();
            //                 if (CurrentState is StateControlJump)
            //                 {
            //                 }
            //                 else if (ma.st == UnitActionStatus.Move)
            //                 {
            //                     changeState(mControlMove);
            //                 }
            //                 else
            //                 {
            //                     FaceTo(ma.faceto);
            //                     doSomething();
            //                 }
            //             }
        }
        protected virtual void doUnitUpdatePos(UnitUpdatePosAction act)
        {
            if (IsGuard)
            {
                return;
            }
            //             if (mCurrentSyncMode == SyncMode.MoveByClient_PreSkillByClient)
            //             {
            //                 if (mLastForceSyncPos != null)
            //                 {
            //                     var distance = MathVector.getDistance(mLastForceSyncPos.X, mLastForceSyncPos.Y, act.pos.X, act.pos.Y);
            //                     var move_len = Game3D.Helper.MoveHelper.GetDistance(Parent.UpdateIntervalMS, this.MoveSpeedSEC);
            //                     if (distance > move_len)
            //                     {
            //                         return;
            //                     }
            //                     mLastForceSyncPos = null;
            //                 }
            if (CurrentState is StateSkill)
            {
                var ss = CurrentState as StateSkill;
                ss.doUpdatePosByClient(act);
            }
            else if (CurrentState is StatePickObject || NextState is StatePickObject)
            {
                if (act.mainState.HasValue)
                {
                    if (act.mainState.Value.IsMoving())
                    {
                        if (act.direction.HasValue)
                        {
                            this.FaceTo(act.direction.Value);
                        }
                        if (act.pos.HasValue)
                        {
                            this.ControlSetPos(act.pos.Value);
                        }
                        //打断捡取
                        this.ChangeState(mControlUpdateMove);
                    }
                }
            }
            else if (CurrentState is StateDamage || CurrentState is StateDead || CurrentState is IStateNoneControllable)
            {

            }
            else if (CurrentState is StatePlayerClimb)
            {
                //在攀爬状态中收到移动位置的消息时
                if (act.direction.HasValue) this.FaceTo(act.direction.Value);
                if (act.pos.HasValue) this.ControlSetPos(act.pos.Value);
            }
            else if (CurrentActionStatus.IsControllable())
            {
                if (act.direction.HasValue) this.FaceTo(act.direction.Value);
                if (act.pos.HasValue) this.ControlSetPos(act.pos.Value);
                //打断
                this.ChangeState(mControlUpdateMove);
                //Console.WriteLine("st=" + act.st);
            }
            //             }
            //             else
            //             {
            //                 this.ControlSetPos(act.pos);
            //             }
        }

        protected virtual void doCustomMove(UnitClientCustomMoveAction act)
        {
            if (IsGuard)
            {
                return;
            }
            ChangeState(mCustomAction);
        }

        protected virtual void doClimb(UnitClimbAction act)
        {
            if (mCurrentSyncMode == SyncMode.MoveByClient_PreSkillByClient)
            {
                if (CurrentState is StatePlayerClimb)
                {
                    //在攀爬状态中收到移动位置的消息时
                    if (!float.IsNaN(act.direction)) this.FaceTo(act.direction);
                    //this.SetRotation(act.rotation);
                    this.ControlSetPos(act.position);
                }
            }
            ChangeState(mClimb);
        }

        protected virtual void doStopMove(UnitStopMoveAction act)
        {
            //mGuard?.CleanFocus();
            if (CurrentState is StateSkill)
            {
                StateSkill ss = CurrentState as StateSkill;
                ss.controlMoveTo(null);
                SendForceSync();
            }
            else if (CurrentState is StatePlayerControlMove || CurrentState is StatePlayerUpdateMove)
            {
                base.SetActionStatus(UnitActionStatus.Idle);
                DoSomething();
                SendForceSyncState();
            }
            else if (CurrentState is StatePlayerCustomAction)
            {
                base.SetActionStatus(UnitActionStatus.Idle);
                DoSomething();
                SendForceSyncState();
            }
            else if (CurrentState is StatePlayerClimb)
            {
                SetActionStatus(UnitActionStatus.Idle);
                DoSomething();
                SendForceSyncState();
            }
        }
        protected virtual void doJump(UnitJumpAction act)
        {
            //             if (mCurrentSyncMode == SyncMode.MoveByClient_PreSkillByClient)
            //             {
            // 
            //             }
            //             else
            {
                this.StartJump(act.ZSpeed);
                //changeState(mControlJump.AsState());
            }
            //             StartJumpState(act.Direction, CMath.getDirect(act.MoveSpeed) * Info.JumpMoveSpeed,
            //                 float.IsNaN(act.ZSpeed) ? null : new Nullable<float>(act.ZSpeed),
            //                 float.IsNaN(act.Gravity) ? null : new Nullable<float>(act.Gravity));
        }
        protected virtual void doFaceTo(UnitFaceToAction ma)
        {
            if (CurrentActionStatus.IsControllable())
            {
                FaceTo(ma.Direction);
            }
        }

        protected virtual void doCancelSkill(UnitCancelSkillRequest sk)
        {
            CancelSkill(sk.SkillID);
        }

        protected virtual UnitLaunchSkillResponse doLaunchSkill(UnitLaunchSkillRequest sk)
        {
            if (sk == mWaitingSkill) { mWaitingSkill = null; }
            var launched = LaunchSkill(sk.SkillID, new TLaunchSkillParam(sk.TargetObjID)
            {
                SpellTargetPos = sk.SpellTargetPos,
                AutoFocusNearTarget = sk.IsAutoFocusNearTarget,
                SummonID = sk.SummonID,
                LaunchArgs = sk.LaunchArgs,
                LaunchTag = sk.LaunchTag,
                LaunchTimeMS = sk.LaunchTimeMS,
                RelatedPetId = sk.RelatedPetId,
                SkillID = sk.SkillID,
                TargetUnitID = sk.TargetObjID,
                //SkillLv = sk.SkillLv,
            });
            if (launched == null)
            {
                if (CurrentState is StateSkill)
                {
                    //缓存释放技能指令//
                    mWaitingSkill = sk;
                }
                return new UnitLaunchSkillResponse(this.ID, false);
            }
            else
            {
                //一旦释放成功，释放指令//
                mWaitingSkill = null;
                return new UnitLaunchSkillResponse(this.ID, true);
            }
        }

        protected virtual UnitGetStatisticResponse doGetStatisticRequest(UnitGetStatisticRequest req)
        {
            UnitGetStatisticResponse resp = new UnitGetStatisticResponse();
            if (req.RequestObjectsID != null)
            {
                for (int i = 0; i < req.RequestObjectsID.Count; i++)
                {
                    InstanceUnit u = Parent.GetUnit(req.RequestObjectsID[i]);
                    if (u != null)
                    {
                        var data = u.Statistic.ToUnitStatisticData();
                        resp.Statistics.Put(req.RequestObjectsID[i], data);
                    }
                }
            }
            return resp;
        }

        protected virtual void doUseItem(UnitUseItemAction use)
        {
            Bag.UseInventoryItem(use.Index, use.Count);
        }

        protected virtual void doPickObject(UnitPickObjectAction pick)
        {
            this.CurrentTargetID = pick.PickableObjectID;
            InstanceZoneObject obj = Parent.GetObject<InstanceZoneObject>(pick.PickableObjectID);
            if (obj is InstanceItem)
            {
                InstanceItem item = obj as InstanceItem;
                item.PickItem(this);
            }
            else if (obj is InstanceUnit)
            {
                InstanceUnit unit = obj as InstanceUnit;
                this.PickUnit(unit);
            }
        }
        protected virtual void doStopPickObject(UnitStopPickObjectAction pick)
        {
            if (CurrentState is StatePickObject c)
            {
                c.Stop(pick.reason);
                this.DoSomething();
            }
        }
        protected virtual void doChat(ChatAction chat)
        {
            ChatNotify send = ObjectPool.Alloc<ChatNotify>().Init(chat.To);
            send.FromPlayerUUID = this.PlayerUUID;
            send.Message = chat.Message;
            switch (chat.To)
            {
                case ChatMessageType.PlayerToPlayer:
                    InstancePlayer target = Parent.GetPlayerByUUID(chat.TargetPlayerUUID);
                    if (target != null)
                    {
                        send.ToPlayerUUID = target.PlayerUUID;
                        Parent.PostEvent(send);
                    }
                    break;
                case ChatMessageType.PlayerToForce:
                    send.Force = this.Force;
                    Parent.PostEvent(send);
                    break;
                case ChatMessageType.PlayerToAll:
                    Parent.PostEvent(send);
                    break;
            }
        }

        protected virtual void doGuard(UnitGuardAction act)
        {
            if (this.IsGuard != act.guard)
            {
                this.mIsSkillControlByServer = act.guard || (mCurrentSyncMode != SyncMode.MoveByClient_PreSkillByClient);
                this.SetGuard(act.guard);
            }
        }
        protected virtual void doSetSyncMode(UnitSetSyncModeAction act)
        {
            SetSyncMode(act.Mode);
        }
        protected virtual void doCancelBuff(UnitCancelBuffAction act)
        {
            EquipBuff bs = this.GetBuffByID(act.BuffID);
            if (bs != null && bs.Data.IsCancelBySelf)
            {
                this.RemoveBuff(act.BuffID, UnitStopBuffEvent.EndResult_ByClientRemoved);
            }
        }

        protected virtual void doUnitSetSubStateAction(UnitSetSubStateAction act)
        {
            base.SetActionSubState(act.UnitSubState, true);
        }
        protected virtual void doPlayerSetEnvVarAction(PlayerSetEnvVarAction act)
        {
            this.SetPlayerEnvironmentVar(act.key, act.value, true);
        }

        //--------------------------------------------------------------------------------------
        /// <summary>
        /// 更新坐标移动状态
        /// </summary>
        public class StatePlayerUpdateMove : State, IStateControllable
        {
            private readonly RecyclableReference<UnitUpdatePosAction> current_act = new RecyclableReference<UnitUpdatePosAction>();
            //             private UnitUpdatePosAction last_act;
            //             private float last_dir;
            public bool IsPosChanged
            {
                get
                {
                    if (current_act.HasValue && current_act.Value.pos.HasValue)
                    {
                        return current_act.Value.pos.Value != unit.Position;
                    }
                    return false;
                }
            }
            public bool IsMove
            {
                get { return current_act.HasValue && current_act.Value.mainState.HasValue && current_act.Value.mainState.Value.IsMoving(); }
            }

            public bool IsIdle
            {
                get { return current_act.HasValue && current_act.Value.mainState.HasValue && current_act.Value.mainState.Value == UnitActionStatus.Idle; }
            }

            public StatePlayerUpdateMove(InstancePlayer unit) : base(unit)
            {
            }
            protected override void Disposing()
            {
                this.current_act.Value = null;
            }
            public bool OnControlAction(ObjectAction ma)
            {
                if (ma is UnitUpdatePosAction act)
                {
                    // TODO 检测作弊
                    if (act.mainState != UnitActionStatus.Idle &&
                        act.mainState != UnitActionStatus.Move &&
                        act.mainState != UnitActionStatus.Walk &&
                        act.mainState != UnitActionStatus.Jump)
                    {
                        act.mainState = UnitActionStatus.Idle;
                    }
                    this.current_act.Value = act;
                    //                     if (last_act != null)
                    //                     {
                    //                         this.last_dir = VectorHelper.GetDegree(last_act.pos, current_act.pos);
                    //                     }
                    //                     this.last_act = act;
                }
                return false;
            }
            public void OnReconnected()
            {
                unit.SetActionStatus(unit.CurrentActionStatus);
            }
            public override bool OnBlock(State new_state)
            {
                return true;
            }
            protected override void OnStart()
            {
                if (current_act.Value?.mainState != null)
                    unit.SetActionStatus(current_act.Value.mainState.Value);
                else
                    unit.SetActionStatus(unit.GetStartMoveStatus());
            }
            protected override void OnUpdate()
            {
                try
                {
                    if (current_act.Value?.mainState != null)
                    {
                        unit.SetActionStatus(current_act.Value.mainState.Value);
                        //当前帧没有坐标过来，需要模拟向前走
                        //                         if (current_act == null)
                        //                         {
                        //                             if (last_act.st == UnitActionStatus.Move)
                        //                             {
                        //                                 //unit.MoveBlockTo(last_dir, unit.MoveSpeedSEC, zone.UpdateIntervalMS);
                        //                                 //var step = MoveHelper.GetDistance(zone.UpdateIntervalMS,unit.MoveSpeedSEC);
                        //                                 //var dir = VectorHelper.GetDegree(last_act.pos, current_act.pos);
                        //                                 //var pos = unit.Position;
                        //                                 //VectorHelper.MovePolar(ref pos, dir, step);
                        //                                 //unit.move
                        //                             }
                        //                         }
                    }
                    else
                    {
                        unit.SetActionStatus(unit.GetStartMoveStatus());
                    }
                }
                finally
                {
                    // this.current_act = null;
                }
            }
            protected override void OnStop()
            {
                current_act.Value = null;
            }

        }
        //--------------------------------------------------------------------------------------
        /// <summary>
        /// 摇杆控制移动状态
        /// </summary>
        public class StatePlayerControlMove : State, IStateControllable
        {
            // private float mDirection;
            private readonly RecyclableReference<UnitAxisAction> mAction = new();
            public UnitAxisAction Action
            {
                get { return mAction.Value; }
            }

            public virtual float MoveSpeedSEC => unit.MoveSpeedSEC;
            public virtual float ZControlSpeedSEC => unit.MoveSpeedSEC;

            public bool IsMove { get { return Action != null && Action.distanceRate != 0; } }
            public StatePlayerControlMove(InstancePlayer unit) : base(unit)
            {

            }
            protected override void Disposing()
            {
                this.mAction.Value = null;
            }
            public virtual bool OnControlAction(ObjectAction ma)
            {
                if (ma is UnitAxisAction axis)
                {
                    this.mAction.Value = axis;
                }
                return false;
            }
            public void OnReconnected()
            {
                unit.SetActionStatus(unit.CurrentActionStatus);
            }

            public override bool OnBlock(State new_state)
            {
                return true;
            }

            protected override void OnStart()
            {
                if (Action != null && Action.distanceRate != 0)
                {
                    unit.SetActionStatus(unit.GetStartMoveStatus());
                }
                else
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }


            protected override void OnUpdate()
            {
                if (Action != null)
                {
                    unit.FaceTo(Action.faceto);
                    //                     if (unit.PreAxisZeroGravity(mAction, zone.UpdateIntervalMS))
                    //                     {
                    //                         unit.SetActionStatus(unit.GetStartMoveStatus());
                    //                     }
                    if (Action.distanceRate != 0)
                    {
                        unit.MoveBlockToAngle(Action.angle, Action.distanceRate * MoveSpeedSEC, zone.UpdateIntervalMS);
                        unit.SetActionStatus(unit.GetStartMoveStatus());
                    }
                    else
                    {
                        unit.SetActionStatus(UnitActionStatus.Idle);
                    }
                }
                else
                {
                    unit.SetActionStatus(UnitActionStatus.Idle);
                }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
            }

            protected override void OnStop()
            {
            }
        }

        /// <summary>
        /// 纯移动不发生任何状态切换
        /// </summary>
        public class StatePlayerCustomControlMove : State, IStateControllable
        {
            // private float mDirection;
            private readonly RecyclableReference<UnitCustomAxisAction> mAction = new();
            public UnitCustomAxisAction Action
            {
                get { return mAction; }
            }

            public virtual float MoveSpeedSEC => unit.MoveSpeedSEC;

            public bool IsMove { get { return Action != null && Action.distanceRate != 0; } }
            public StatePlayerCustomControlMove(InstancePlayer unit) : base(unit)
            {

            }
            protected override void Disposing()
            {
                this.mAction.Value = null;
            }
            public virtual bool OnControlAction(ObjectAction ma)
            {
                if (ma is UnitCustomAxisAction axis)
                {
                    this.mAction.Value = axis;
                }
                return false;
            }
            public void OnReconnected()
            {
                unit.SetActionStatus(unit.CurrentActionStatus);
            }

            public override bool OnBlock(State new_state)
            {
                return true;
            }

            protected override void OnStart()
            {

            }


            protected override void OnUpdate()
            {
                if (mAction.HasValue)
                {
                    if (Action.distanceRate != 0)
                    {
                        unit.MoveBlockToAngle(Action.angle, Action.distanceRate * MoveSpeedSEC, zone.UpdateIntervalMS);
                    }
                    mAction.Value = null;
                }

            }

            protected override void OnStop()
            {
            }
        }
        //--------------------------------------------------------------------------------------
        //         public class StateControlJump : State, IStateControllable
        //         {
        //             private float direction;
        //             private float moveSpeed;
        //             private float speedz;
        //             private float gravity;
        //             private FallingDown falldown;
        // 
        //             public StateControlJump(InstanceUnit unit)
        //                 : base(unit)
        //             {
        //             }
        //             public State AsState() { return this; }
        //             public virtual bool OnControlAction(ObjectAction ma)
        //             {
        //                 if (ma is UnitAxisAction axis)
        //                 {
        //                     this.direction = axis.angle;
        //                     this.moveSpeed = CMath.GetDirect(axis.distance) * unit.MoveSpeedSEC;
        //                     if (unit.CurrentState == this)
        //                     {
        //                         this.unit.FaceTo(axis.faceto);
        //                     }
        //                 }
        //                 else if (ma is UnitJumpAction jump)
        //                 {
        //                     this.direction = jump.Direction;
        //                     this.moveSpeed = CMath.GetDirect(jump.MoveSpeed) * unit.MoveSpeedSEC;
        //                     this.speedz = float.IsNaN(jump.ZSpeed) && unit.AMotion ? unit.AMotion.JumpZSpeed : jump.ZSpeed;
        //                     this.gravity = float.IsNaN(jump.Gravity) ? unit.Parent.Gravity : jump.Gravity;
        //                     // TODO continue jump
        //                     if (unit.CurrentState == this)
        //                     {
        //                         unit.SetActionStatus(UnitActionStatus.Jump);
        //                         this.falldown = unit.StartJump(speedz, gravity);
        //                     }
        //                 }
        // 
        // 
        //                 return false;
        //             }
        //             public void OnReconnected()
        //             {
        // 
        //             }
        //             override public bool onBlock(State new_state)
        //             {
        //                 if (falldown == null || falldown.IsEnd) return true;
        //                 if (new_state == this) { return true; }
        //                 if (new_state is IStateControllable) { return false; }
        //                 if (new_state is IStateNoneControllable) { return true; }
        //                 return false;
        //             }
        //             override protected void onStart()
        //             {
        //                 unit.SetActionStatus(UnitActionStatus.Jump);
        //                 this.falldown = unit.StartJump(speedz, gravity);
        //             }
        //             override protected void onUpdate()
        //             {
        //                 unit.SetActionStatus(UnitActionStatus.Jump);
        //                 if (moveSpeed != 0)
        //                 {
        //                     unit.MoveAirTo(direction, moveSpeed, zone.UpdateIntervalMS);
        //                 }
        //                 if (falldown.IsEnd)
        //                 {
        //                     unit.DoSomething();
        //                 }
        //             }
        //             override protected void onStop()
        //             {
        //             }
        //         }

        //--------------------------------------------------------------------------------------

        /// <summary>
        /// 攀爬状态
        /// </summary>
        public class StatePlayerClimb : State, IStateControllable
        {
            private float defaultMoveSpeed;

            public StatePlayerClimb(InstancePlayer unit) : base(unit)
            {
            }
            protected override void Disposing()
            {

            }


            public override bool OnBlock(State newState)
            {
                if (unit.CurrentActionStatus != UnitActionStatus.Climb) return true;
                if (newState is IStateControllable) return false;
                if (newState is IStateNoneControllable) return true;
                return false;
            }

            public bool OnControlAction(ObjectAction ma)
            {
                if (unit.CurrentState == this)
                {
                    if (ma is UnitUpdatePosAction pos)
                    {
                        if (pos.direction.HasValue) unit.FaceTo(pos.direction.Value);
                        if (pos.pos.HasValue) unit.ControlSetPos(pos.pos.Value);
                        unit.SetActionSubState(pos.subst, false);
                    }
                    else if (ma is UnitStopMoveAction stop)
                    {
                        unit.SetActionStatus(UnitActionStatus.Idle);
                        unit.SetActionSubState(null, true);
                    }
                    else if (ma is UnitClimbAction climb)
                    {
                        unit.FaceTo(climb.direction);
                        //unit.SetRotation(climb.rotation);
                        unit.ControlSetPos(climb.position);
                    }
                }
                return false;
            }

            public void OnReconnected()
            {
                unit.SetActionStatus(unit.CurrentActionStatus);
            }

            protected override void OnStart()
            {
                defaultMoveSpeed = unit.MoveSpeedSEC;
                unit.Gravity = 0;
                unit.ZSpeedSEC = 0;
                unit.SetActionStatus(UnitActionStatus.Climb);
            }

            protected override void OnUpdate()
            {
            }

            protected override void OnStop()
            {
                unit.ResetGravity();
                unit.ZSpeedSEC = 0;
                unit.SetMoveSpeed(defaultMoveSpeed);
            }
        }

        public class StatePlayerCustomAction : State, IStateControllable
        {
            private float defaultMoveSpeed;
            private string subState;
            public StatePlayerCustomAction(InstancePlayer unit) : base(unit)
            {
            }
            protected override void Disposing()
            {

            }
            public State AsState() { return this; }
            public override bool OnBlock(State new_state)
            {
                if (unit.CurrentActionStatus != UnitActionStatus.ClientCustom) return true;
                if (new_state is IStateControllable) return false;
                if (new_state is IStateNoneControllable) return true;
                return false;
            }
            public bool OnControlAction(ObjectAction ma)
            {
                if (unit.CurrentState == this)
                {
                    if (ma is UnitJumpAction jump)
                    {
                        if (jump.ZSpeed.HasValue)
                        {
                            unit.ZSpeedSEC = jump.ZSpeed.Value;
                        }
                        unit.SetMoveSpeed(jump.MoveSpeed * unit.FastMoveRate);
                    }
                    else if (ma is UnitUpdatePosAction pos)
                    {
                        if (pos.direction.HasValue) unit.FaceTo(pos.direction.Value);
                        if (pos.pos.HasValue) unit.ControlSetPos(pos.pos.Value);
                        unit.SetActionSubState(pos.subst, false);
                    }
                    else if (ma is UnitStopMoveAction stop)
                    {
                        unit.SetActionStatus(UnitActionStatus.Idle);
                        unit.SetActionSubState(null, true);
                        unit.ResetGravity();
                        unit.ZSpeedSEC = 0;
                        unit.SetMoveSpeed(defaultMoveSpeed * unit.FastMoveRate);
                    }
                }
                else
                {
                    if (ma is UnitClientCustomMoveAction custom)
                    {
                        subState = custom.SubState;
                        return true;
                    }
                }
                return false;
            }
            public void OnReconnected()
            {
                unit.SetActionStatus(unit.CurrentActionStatus);
            }
            protected override void OnStart()
            {
                defaultMoveSpeed = unit.BaseMoveSpeedSEC;

                unit.SetActionStatus(UnitActionStatus.ClientCustom);
                unit.SetActionSubState(subState, false);
                unit.Gravity = 0;
                unit.ZSpeedSEC = 0;
            }
            protected override void OnStop()
            {
                unit.ResetGravity();
                unit.ZSpeedSEC = 0;
                unit.SetMoveSpeed(defaultMoveSpeed * unit.FastMoveRate);
                subState = null;
            }
            protected override void OnUpdate()
            {

            }
        }
    }

}
