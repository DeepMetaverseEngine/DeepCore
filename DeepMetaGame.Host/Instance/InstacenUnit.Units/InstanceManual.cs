using DeepCore.Game3D.Host.Helper;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 纯手动控制AI，完全没有自动成分
    /// </summary>
    public partial class InstanceManual : InstanceUnit
    {
        //private StateFollowAndAttack mAttackTargget;
        private TimeExpire<Action> mWaitCommand;

        new public string PlayerUUID
        {
            get { return mSyncInfo.PlayerUUID; }
            set { mSyncInfo.PlayerUUID = value; }
        }

        public InstanceManual(InstanceZone zone, TAddUnit add)
            : base(zone, add)
        {
        }
        protected override void Disposing()
        {
            base.Disposing();
            mWaitCommand?.Dispose();
        }
        protected override void onUpdateAI()
        {
            base.onUpdateAI();

            if (mWaitCommand != null && mWaitCommand.Update(Parent.UpdateIntervalMS))
            {
                mWaitCommand.Tag.Invoke();
                mWaitCommand.Dispose();
                mWaitCommand = null;
            }
        }
        protected override void onAction(ObjectAction act)
        {
            if (IsDead)
            {
                return;
            }
            if (act is UnitStopMoveAction)
            {
                DoSomething();
            }
            //             else if (act is UnitMoveAction)
            //             {
            //                 UnitMoveAction move = act as UnitMoveAction;
            //                 startMoveTo(move.x, move.y);
            //             }
            else if (act is UnitAxisAction)
            {

            }
            else if (act is UnitLaunchSkillRequest)
            {
                UnitLaunchSkillRequest sk = act as UnitLaunchSkillRequest;
                LaunchSkill(sk.SkillID, new InstanceUnit.TLaunchSkillParam(sk.TargetObjID)
                {
                    SpellTargetPos = sk.SpellTargetPos,
                    AutoFocusNearTarget = sk.IsAutoFocusNearTarget,
                });
            }
            //             else if (act is UnitFaceToAction)
            //             {
            //                 UnitFaceToAction ufa = act as UnitFaceToAction;
            //                 this.faceTo(ufa.Direction);
            //             }
            //             else if (act is UnitSlipAction)
            //             {
            //                 // do nothing
            //             }
            else if (act is UnitGuardAction)
            {
                // do nothing
            }
            else if (act is UnitFocuseTargetAction)
            {
                // do nothing
            }
        }

        protected override void DoDefaultBehavior()
        {
            if (NextState == null)
                StartIdle();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------


        //-------------------------------------------------------------------------------------------------------------------------------------
        #region Status------------------------------------------------------------------------------------------------------------------------------------
        class QueueStateMoveTo : State
        {
            private Geometry.Vector3 target;
            private bool isEnd = false;
            private MoveAI moveAI;

            public static QueueStateMoveTo Alloc(InstanceUnit unit, Geometry.Vector3 pos)
            {
                var ret = unit.AllocState<QueueStateMoveTo>();
                ret.target = pos;
                ret.moveAI = unit.CreateMoveAI();
                return ret;
            }
            protected override void Disposing()
            {
                this.target = default;
                this.isEnd = false;
                this.moveAI?.Dispose();
                this.moveAI = null;
            }

            override public bool OnBlock(State new_state)
            {
                if (unit.IsDead) return true;
                return isEnd;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                this.moveAI.FindPath(target);
            }
            override protected void OnUpdate()
            {
                if (!isEnd)
                {
                    unit.FaceTo(target.X, target.Y);
                    MoveBlockResult result = moveAI.Update();
                    if ((result.result & MoveResult.MOVE_RESULT_NO_WAY) != 0)
                    {
                        isEnd = true;
                        unit.DoSomething();
                    }
                    else if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                    {
                        float r = Math.Max(zone.MinStep, unit.BodyBlockSize);
                        if (new Geometry.BoundingSphere(target, r).Contains(unit.Position) == Geometry.ContainmentType.Contains)
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                    }
                    else
                    {
                        float r = Math.Max(zone.MinStep, unit.BodyBlockSize);
                        if (new Geometry.BoundingSphere(target, r).Contains(unit.Position) == Geometry.ContainmentType.Contains)
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                    }
                }
            }

            override protected void OnStop()
            {

            }
        }

        class QueueStateLaunchSkill : State
        {
            private int SkillID;
            private bool IsRandom;
            private StateStopHandler SkillOver;
            private EquipSkill mSkillState;


            public static QueueStateLaunchSkill Alloc(InstanceUnit unit, int skillID, bool random, StateStopHandler over)
            {
                var ret = unit.AllocState<QueueStateLaunchSkill>();
                ret.SkillID = skillID;
                ret.IsRandom = random;
                ret.SkillOver = over;
                return ret;
            }
            protected override void Disposing()
            {
                this.SkillID = default;
                this.IsRandom = default;
                this.SkillOver = default;
                this.mSkillState = default;
            }


            public override bool OnBlock(State new_state)
            {
                if (unit.IsDead) return true;
                if (new_state is StateSkill)
                {
                    return true;
                }
                return mSkillState != null;
            }
            protected override void OnStart()
            {

            }
            protected override void OnUpdate()
            {
                if (IsRandom)
                {
                    mSkillState = unit.LaunchRandomSkillForAll(new InstanceUnit.TLaunchSkillParam());
                }
                else
                {
                    mSkillState = unit.LaunchSkill(SkillID, new InstanceUnit.TLaunchSkillParam());
                }
                if (SkillOver != null && unit.NextState is StateSkill sk)
                {
                    sk.OnStopOnce += (SkillOver);
                }
            }
            protected override void OnStop()
            {
                if (mSkillState == null && SkillOver != null)
                {
                    SkillOver.Invoke(unit, this);
                }
            }
        }

        class QueueStateIdleTime : State
        {
            protected TimeExpire mIdleTime;

            public static QueueStateIdleTime Alloc(InstanceUnit unit, float timeSEC)
            {
                var ret = unit.AllocState<QueueStateIdleTime>();
                ret.mIdleTime = unit.ObjectPool.AllocTimeExpire((timeSEC * 1000));
                return ret;
            }
            protected override void Disposing()
            {
                this.mIdleTime?.Dispose();
                this.mIdleTime = default;
            }

            override public bool OnBlock(State new_state)
            {
                if (unit == null) return false;
                if (unit.IsDead) return true;
                return true;
            }

            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.Idle);
            }

            override protected void OnUpdate()
            {
                if (mIdleTime.Update(zone.UpdateIntervalMS))
                {
                    unit.DoSomething();
                }
            }
            override protected void OnStop() { }
        }

        class QueueStateActionTime : QueueStateIdleTime
        {
            private string ActionName;
            public static QueueStateActionTime Alloc(InstanceUnit unit, float timeSEC, string actionName)
            {
                var ret = unit.AllocState<QueueStateActionTime>();
                ret.ActionName = actionName;
                ret.mIdleTime = unit.ObjectPool.AllocTimeExpire((timeSEC * 1000));
                return ret;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.ActionName = default;
            }

            override protected void OnStart()
            {
                unit.SetActionStatus(UnitActionStatus.ClientCustom);
                unit.SetActionSubState(ActionName, true);
                unit.PostEvent(ObjectPool.Alloc<UnitDoActionEvent>().Init (unit.ID, UnitActionStatus.ClientCustom,null, ActionName));
            }

        }

        public void FocuseAttack(InstanceUnit targget)
        {
            ChangeState(StateFollowAndAttack.Alloc(this, targget));
        }
        public void QueueIdle(float timeSEC, StateStopHandler over = null)
        {
            //mAttackTargget = null;
            var state = QueueStateIdleTime.Alloc(this, timeSEC);
            if (over != null)
            {
                state.OnStopOnce += (over);
            }
            QueueState(state);
        }
        public void QueueDoAction(float timeSEC, string actionName, StateStopHandler over = null)
        {
            //mAttackTargget = null;
            var state = QueueStateActionTime.Alloc(this, timeSEC, actionName);
            if (over != null)
            {
                state.OnStopOnce += (over);
            }
            QueueState(state);
        }
        public void DoAction(float timeSEC, string actionName, StateStopHandler over = null)
        {
            //mAttackTargget = null;
            var state = QueueStateActionTime.Alloc(this, timeSEC, actionName);
            if (over != null)
            {
                state.OnStopOnce += (over);
            }
            ChangeState(state);
        }
        public void QueueMoveTo(Geometry.Vector3 pos, StateStopHandler over = null)
        {
            //mAttackTargget = null;
            var state = QueueStateMoveTo.Alloc(this, pos);
            if (over != null)
            {
                state.OnStopOnce += (over);
            }
            QueueState(state);
        }
        public void QueueLaunchSkill(int skillID, bool random, StateStopHandler over = null)
        {
            //mAttackTargget = null;
            var state = QueueStateLaunchSkill.Alloc(this, skillID, random, over);
            QueueState(state);
        }
        public void Wait(float timeSEC, System.Action over = null)
        {
            // 强制中断前一个等待指令
            if (mWaitCommand != null)
            {
                mWaitCommand.Tag.Invoke();
                mWaitCommand.Dispose();
            }
            mWaitCommand = ObjectPool.AllocAutoRelease<TimeExpire<Action>>().Init((timeSEC * 1000), over);
        }
        #endregion Status------------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------
    }
}
