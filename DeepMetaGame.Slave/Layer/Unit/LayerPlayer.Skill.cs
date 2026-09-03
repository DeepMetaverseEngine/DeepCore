using DeepCore.Game3D.Slave.Data;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer
    {
        /// <summary>
        /// 是否客户端位移模式
        /// </summary>
        public bool IsClientControlMove
        {
            get
            {
                if (IsGuard) return false;
                if (Parent.ActorSyncMode != SyncMode.MoveByClient_PreSkillByClient) return false;
                return true;
            }
        }

        public IActorSkillAction CurrentActorSkillAction
        {
            get { return CurrentSkillAction as IActorSkillAction; }
        }
        public SkillState CurrentSkillState
        {
            get
            {
                if (CurrentSkillAction is IActorSkillAction a)
                {
                    return a.State;
                }
                return null;
            }
        }

        protected override void doLaunchSkillAction(SkillState ss, UnitLaunchSkillEvent me)
        {
            clearSkillAction();
            PreSetCurrentMainState(UnitActionStatus.Skill, null, me);
            var action = SlaveFactory.AllocSkillAction(this, ss);
            action.onLaunch(me);
            this.mCurrentSkillAction?.Dispose();
            this.mCurrentSkillAction = action;
            //             if (IsClientControlMove)
            //             {
            //                 PreSkillByClient action = PreSkillByClient.Alloc(this, ss);
            //                 action.onLaunch(me);
            //                 this.mCurrentSkillAction = action;
            //             }
            //             else
            //             {
            //                 PreSkillByServer action = PreSkillByServer.Alloc(this, ss);
            //                 action.onLaunch(me);
            //                 this.mCurrentSkillAction = action;
            //             }
            invokeLaunchSkill(ss, action);
            invokeSkillActionStart(action);
        }
        protected override void updateSkillAction(float intervalMS)
        {
            //base.updateSkillAction(intervalMS);
            if (CurrentSkillAction != null)
            {
                if (CurrentSkillAction is PreSkillByClient)
                {
                    //mCurrentSkillAction.onUpdate(intervalMS);、、放到移动逻辑预处理
                }
                else
                {
                    CurrentSkillAction.onUpdate(intervalMS);
                }
                if (CurrentSkillAction.IsDone)
                {
                    clearSkillAction();
                }
            }
        }
        //         protected override void clearSkillAction()
        //         {
        //             base.clearSkillAction();
        //         }
        private void DoClearSkillActionEvent(PlayerSkillStopEvent evt)
        {
            clearSkillAction();
        }
        private void DoPlayerSkillActionChangeEvent(UnitSkillActionChangeEvent evt)
        {
            if (mCurrentSkillAction is ISkillAction skillAction)
            {
                skillAction.onUnitSkillActionChangeEvent(evt);
            }
        }

        //-------------------------------------------------------------------------------------------------------

        public interface IActorSkillAction
        {
            SkillState State { get; }
        }
        //-------------------------------------------------------------------------------------------------------
        public class PreSkillByServer : UnitPreSkillAction, IActorSkillAction
        {
            private LayerPlayer ownerPlayer;
            public LayerPlayer owner => ownerPlayer;
            public static PreSkillByServer Alloc(LayerPlayer actor, SkillState skill)
            {
                var ret = actor.ObjectPool.AllocAutoRelease<PreSkillByServer>();
                ret.Init(actor, skill);
                return ret;
            }
            protected void Init(LayerPlayer actor, SkillState state)
            {
                base.Init(actor, state);
                this.ownerPlayer = actor;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.ownerPlayer = null;
            }
        }
        //-------------------------------------------------------------------------------------------------------
        public class PreSkillByClient : UnitPreSkillAction, IActorSkillAction
        {
            //--------------------------------------------------------
            public LayerPlayer ownerPlayer => base.ownerUnit as LayerPlayer;

            //--------------------------------------------------------

            public static PreSkillByClient Alloc(LayerPlayer actor, SkillState skill)
            {
                var ret = actor.ObjectPool.AllocAutoRelease<PreSkillByClient>();
                ret.Init(actor, skill);
                return ret;
            }

            public void controlMoveTo(UnitAxisAction axis)
            {
                if (axis == null)
                {
                    this.move_to = null;
                    return;
                }
                if (axis.distanceRate != 0)
                {
                    if (this.IsCancelableByMove)
                    {
                        is_done = true;
                        total_pass_time = TotalTimeMS;
                    }
                    float degree = axis.angle;
                    if (IsControlMoveable)
                    {
                        var pos = ownerUnit.Position;
                        pos.Z = axis.distanceRate;
                        VectorHelper.MovePolar(ref pos, degree, MoveHelper.GetDistance(this.TotalTimeMS, ownerUnit.MoveSpeedSEC));
                        move_to = pos;
                    }
                }
                else
                {
                    this.move_to = null;
                }
                if (IsControlFaceable)
                {
                    ownerUnit.PreFaceTo(axis.faceto);
                }
                this.StopFaceTo = axis;// new Vector2(pos.x, pos.y);
                this.StopFaceTo.Retain();
            }
        }
        //-------------------------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------------------------
    }
}



