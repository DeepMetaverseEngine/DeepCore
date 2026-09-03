using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        public abstract class ForceState : State
        {
            override public bool OnBlock(State new_state)
            {
                if (new_state is StateDead) return true;
                if (new_state is StateRebirth) return true;
                if (new_state is StateStun) return true;
                return unit.IsDead;
            }
        }

        /// <summary>
        /// 强制移动到目标点，一般用于剧情动画
        /// </summary>
        public class ForceStateMoveTo : ForceState
        {
            private Geometry.Vector3 target;
            private bool isEnd = false;
            private MoveAI moveAI;
            public static ForceStateMoveTo Alloc(InstanceUnit unit, Geometry.Vector3 tgt)
            {
                var ret = unit.AllocState<ForceStateMoveTo>();
                ret.Init(unit, tgt);
                return ret;
            }
            protected ForceStateMoveTo Init(InstanceUnit unit, Geometry.Vector3 tgt)
            {
                this.target = tgt;
                this.moveAI = unit.CreateMoveAI();
                return this;
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
                if (base.OnBlock(new_state)) return true;
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
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, r, targetX, targetY))
                        if (moveAI.IsInRange(target, r))
                        {
                            isEnd = true;
                            unit.DoSomething();
                        }
                    }
                    else
                    {
                        float r = Math.Max(zone.MinStep, unit.BodyBlockSize);
                        //if (CMath.includeRoundPoint(unit.X, unit.Y, r, targetX, targetY))
                        if (moveAI.IsInRange(target, r))
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
        
        
        /// <summary>
        /// 强制移动到目标区域
        /// </summary>
        public class ForceStateMoveToZoneRegion : ForceState
        {
            
            private bool IsEnd = false;
            private ZoneRegion EndRegion;
            private float Dir = 0;
            public static ForceStateMoveToZoneRegion Alloc(InstanceUnit unit, float dir,ZoneRegion region)
            {
                var ret = unit.AllocState<ForceStateMoveToZoneRegion>();
                ret.Init(unit,dir, region);
                return ret;
            }
            protected ForceStateMoveToZoneRegion Init(InstanceUnit unit, float dir, ZoneRegion region)
            {
                this.Dir = dir;
                this.EndRegion = region;
                if (unit is InstancePlayer player)
                {
                    player.SetGuard(true,true);
                }
                return this;
            }

            private void UnitOnUpdate(InstanceUnit instanceUnit)
            {
                if (!IsEnd)
                {
                    unit.FaceTo(Dir);
                    var pos = unit.Position;
                    VectorHelper.MovePolar(ref pos, Dir,  unit.MoveSpeedSEC);
                    unit.MoveImpactTo(pos.Value.X, pos.Value.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, false);
                }
            }

            protected override void Disposing()
            {
                this.IsEnd = false;
            }

            override public bool OnBlock(State new_state)
            {
                // if (base.OnBlock(new_state)) return true;
                return IsEnd;
            }
            override protected void OnStart()
            {
                unit.SetActionStatus(unit.GetStartMoveStatus());
                unit.OnUpdate += UnitOnUpdate;
                EndRegion.ListenUnitEnterOnce(unit, (r, u) =>
                {
                    IsEnd = true;
                    u.ChangeState(u.AllocState<StateIdle>());
                });
                
            }
            override protected void OnUpdate()
            {
              
            }

            override protected void OnStop()
            {
                unit.OnUpdate -= UnitOnUpdate;
            }
        }


        public class ForceStateLaunchSkill : ForceState
        {
            private int SkillID;
            private bool IsRandom;
            private StateStopHandler SkillOver;
            private EquipSkill mSkillState;

            public static ForceStateLaunchSkill Alloc(InstanceUnit unit, int skillID, bool random, StateStopHandler over = null)
            {
                var ret = unit.AllocState<ForceStateLaunchSkill>();
                ret.Init(unit, skillID, random, over);
                return ret;
            }
            protected ForceStateLaunchSkill Init(InstanceUnit unit, int skillID, bool random, StateStopHandler over = null)
            {
                this.SkillID = skillID;
                this.IsRandom = random;
                this.SkillOver = over;
                return this;
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
                if (base.OnBlock(new_state)) return true;
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
                if (mSkillState == null)
                {
                    mSkillState = unit.LaunchRandomSkillForAll(new InstanceUnit.TLaunchSkillParam());
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

        public class ForceStateIdleTime : ForceState
        {
            private TimeExpire mIdleTime;
            public static ForceStateIdleTime Alloc(InstanceUnit unit, float timeSEC)
            {
                var ret = unit.AllocState<ForceStateIdleTime>();
                ret.Init(unit, timeSEC);
                return ret;
            }
            protected ForceStateIdleTime Init(InstanceUnit unit, float timeSEC)
            {
                mIdleTime = unit.AllocTimeExpire((int)(timeSEC * 1000));
                return this;
            }
            protected override void Disposing()
            {
                mIdleTime?.Dispose();
                mIdleTime = null;
            }

            override public bool OnBlock(State new_state)
            {
                if (base.OnBlock(new_state)) return true;
                return mIdleTime.IsEnd;
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

        public class ForceStateActionTime : ForceStateIdleTime
        {
            private string ActionName;
            public static ForceStateActionTime Alloc(InstanceUnit unit, float timeSEC, string actionName)
            {
                var ret = unit.AllocState<ForceStateActionTime>();
                ret.Init(unit, timeSEC, actionName);
                return ret;
            }
            protected ForceStateActionTime Init(InstanceUnit unit, float timeSEC, string actionName)
            {
                base.Init(unit, timeSEC);
                this.ActionName = actionName;
                return this;
            }
            protected override void Disposing()
            {
                base.Disposing();
                this.ActionName = null;
            }
            override protected void OnStart()
            {
                unit.PostEvent(ObjectPool.Alloc<UnitDoActionEvent>().Init (unit.ID, UnitActionStatus.ClientCustom, null, ActionName));
            }
        }


    }
}
