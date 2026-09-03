using DeepCore;
using DeepCore.EventTrigger.Data;
using DeepCore.Geometry;
using DeepCore.XCSV;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.GUI.Cell.SpriteSet;

namespace DeepMetaGame.Data.ZoneGeometry
{
    public class HostSpellMotion : ISpellMotion
    {
        protected Vector3 mStartPos;
        protected float mLaunchDirection;
        //protected float mBaseSize;
        //protected float mDistance;
        protected float mSpeed;
        protected float mSpeedZ;
        protected float mDistanceSpeed;
        protected float mRotateSpeed;
        //protected Vector3? mStartNormal;
        protected TimeExpire mSeekingCooldownTime;
        public override float CurrentSpeed { get => mSpeed; set => mSpeed = value; }
        //----------------------------------------------------------------------------------------------
        public override ISpellMotion Init(IZoneSpell spell)
        {
            base.Init(spell);
            this.mLaunchDirection = Spell.Direction;
            //             this.mBaseSize = Spell.Template.BodySize;
            //             this.mDistance = Spell.Template.Distance;
            this.mSpeed = spell.StartSpeed;
            this.mSpeedZ = 0;
            this.mDistanceSpeed = 0;
            this.mRotateSpeed = Info.RotateSpeedSEC;
            //this.mStartNormal = Spell.StartNormal;
            return this;
        }
        protected override void Disposing()
        {
            this.mStartPos = default;
            this.mLaunchDirection = default;

            //this.mStartNormal = default;
            this.mSeekingCooldownTime?.Dispose();
            this.mSeekingCooldownTime = default;

            //             this.mBaseSize = default;
            //             this.mDistance = default;
            this.mSpeed = default;
            this.mSpeedZ = default;
            this.mRotateSpeed = default;

            base.Disposing();
        }
        public override void OnAdded()
        {
            this.mSpeed = Spell.StartSpeed;
            this.mStartPos = Spell.Position;
            var mLaunchData = Spell.LaunchData;
            var mInfo = Spell.Template;
            this.mLaunchDirection = Spell.Direction;
            float startRadius = mLaunchData.LaunchSpellRadius;
            float startAngle = mLaunchData.LaunchSpellAngle;
            float startHeight = mLaunchData.LaunchSpellHeight;
            if (mLaunchData.FromUnitBody && (Spell.Sender is IZoneUnit su) && su.ASkill)
            {
                startAngle = su.ASkill.LaunchSpellAngle;
                startRadius = su.ASkill.LaunchSpellRadius * su.BodyScale;
                startHeight = su.ASkill.LaunchSpellHeight * su.BodyScale;
            }
            this.mStartPos += new Vector3(0, 0, startHeight);
            //----------------------------------------------------------------------------------------------------------
            switch (Info.MType)
            {
                #region Free
                case SpellTemplate.MotionType.Immovability:
                    VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (Spell.TargetUnit != null)
                    {
                        Spell.TargetPos = Spell.TargetUnit.Position;
                    }
                    if (Spell.TargetPos != null)
                    {
                        VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                        MoveHelper.CalculateSpellLaunchAngle(mInfo, in mStartPos, Spell.TargetPos.Value, Spell.Zone.CFG.GLOBAL_GRAVITY,
                            out var muzzleAngle,
                            out mLaunchDirection,
                            out mSpeed,
                            out mSpeedZ);
                    }
                    else
                    {
                        Spell.Finish();
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                case SpellTemplate.MotionType.StraightPingPong:
                case SpellTemplate.MotionType.Boomerang:
                    if (Spell.TargetPos == null && Spell.TargetUnit != null)
                    {
                        Spell.TargetPos = Spell.TargetUnit.WaistPosition;
                    }
                    if (Spell.TargetPos.HasValue)
                    {
                        VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                        var hroz = VectorHelper.Polar(mLaunchDirection + startAngle, 1);
                        hroz.Normalize();
                        var mStartNormal = Vector3.Normalize(Spell.TargetPos.Value - mStartPos);
                        mStartNormal = new Vector3(hroz.X, hroz.Y, mStartNormal.Value.Z);
                        if (Spell.LaunchData.FromSpellMagnitude)
                        {
                            if (Spell.Sender is IZoneSpell senderSpell && senderSpell.StartNormal.HasValue)
                            {
                                var srcNormal = senderSpell.StartNormal.Value * senderSpell.Motion.CurrentSpeed;
                                var dstNormal = mStartNormal * Spell.StartSpeed;
                                var normal = dstNormal + srcNormal;
                                mStartNormal = Vector3.Normalize(normal);//* senderSpell.Motion.CurrentSpeed;
                                mSpeed = normal.Length();
                                Spell.StartSpeed = mSpeed;
                            }
                        }
                        Spell.StartNormal = mStartNormal;
                    }
                    else
                    {
                        VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                        var hroz = VectorHelper.Polar(mLaunchDirection + startAngle, 1);
                        hroz.Normalize();
                        var mStartNormal = new Vector3(hroz.X, hroz.Y, 0);
                        if (Spell.LaunchData.FromSpellMagnitude)
                        {
                            if (Spell.Sender is IZoneSpell senderSpell && senderSpell.StartNormal.HasValue)
                            {
                                var srcNormal = senderSpell.StartNormal.Value * senderSpell.Motion.CurrentSpeed;
                                var dstNormal = mStartNormal * Spell.StartSpeed;
                                var normal = dstNormal + srcNormal;
                                mStartNormal = Vector3.Normalize(normal);//* senderSpell.Motion.CurrentSpeed;
                                mSpeed = normal.Length();
                                Spell.StartSpeed = mSpeed;
                            }
                        }
                        Spell.StartNormal = mStartNormal;
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    VectorHelper.MovePolar(ref mStartPos, Spell.Direction + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.Backward:
                    VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.AOE:
                    VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.Missile:
                    VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    VectorHelper.MovePolar(ref mStartPos, mLaunchDirection + startAngle, startRadius);
                    if (Info.SeekingCooldownMS > 0)
                    {
                        this.mSeekingCooldownTime = new TimeExpire(Info.SeekingCooldownMS);
                    }
                    else if (Spell.TargetUnit == null)
                    {
                        if (Spell.TrySeekAttackable(Info.SeekingRange, false, out var dst))
                        {
                            Spell.TargetUnit = dst;
                        }
                    }
                    break;
                #endregion
                //----------------------------------------------------------------------------------------------------------
                #region Bind
                case SpellTemplate.MotionType.SelectTarget:
                    if (Spell.TargetUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(Spell.TargetUnit);
                    }
                    break;
                case SpellTemplate.MotionType.SelectLauncher:
                    if (Spell.LauncherUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(Spell.LauncherUnit);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    if (Spell.TargetUnit == null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        if (Info.SeekingCooldownMS > 0)
                        {
                            this.mSeekingCooldownTime = Spell.Zone.AllocTimeExpire(Info.SeekingCooldownMS);
                        }
                        else
                        {
                            if (Spell.TrySeekAttackable(Info.SeekingRange, false, out var dst))
                            {
                                Spell.TargetUnit = dst;
                                mStartPos = GetBindingPos(Spell.TargetUnit);
                            }
                            //                             Spell.TargetUnit = Spell.SeekAttackable(Info.SeekingRange, false);
                            //                             if (Spell.TargetUnit != null)
                            //                             {
                            //                                 mStartPos = GetBindingPos(Spell.TargetUnit);
                            //                             }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.Binding:
                    if (Spell.Sender != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(Spell.Sender);
                    }
                    else if (Spell.LauncherUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(Spell.LauncherUnit);
                    }
                    break;

                case SpellTemplate.MotionType.AOE_BindingTarget:
                case SpellTemplate.MotionType.BindingTarget:
                    if (Spell.TargetUnit != null)
                    {
                        //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                        mStartPos = GetBindingPos(Spell.TargetUnit);
                    }
                    break;

                case SpellTemplate.MotionType.Chain:
                    if (Spell.Sender != null)
                    {
                        //                         if (Spell.IsNextChain)
                        //                         {
                        //                             mStartPos = GetBindingPos(Spell.Sender);
                        //                         }
                        //                         else
                        {
                            //mBindingOffset = new Polar3(startAngle, startRadius, startHeight);
                            mStartPos = GetBindingPos(Spell.Sender);
                        }
                    }
                    break;
                    #endregion
            }
            //----------------------------------------------------------------------------------------------------------
            if (Spell.Zone.Terrain3D.TryGetVoxelLayerByPos(in mStartPos, out var upward, out var top))
            {
                switch (Info.BodyVoxelAnchor)
                {
                    case VoxelAnchor.Ceiling:
                        mStartPos.Z = top;
                        break;
                    case VoxelAnchor.Flooring:
                        mStartPos.Z = upward;
                        break;
                }
            }
            Spell.SetPosition(mStartPos);
        }
        /// <summary>
        /// 更新移动行为
        /// </summary>
        public override void UpdateMotion(float intervalMS)
        {
            var mInfo = Spell.Template;
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Immovability:
                    updateImmovability(intervalMS, Spell.Sender);
                    break;
                case SpellTemplate.MotionType.SelectTarget:
                case SpellTemplate.MotionType.SelectLauncher:
                    break;

                case SpellTemplate.MotionType.Cannon:
                    if (Spell.TargetPos != null)
                    {
                        if (projectileToTarget(Spell.TargetPos.Value, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    else
                    {
                        Spell.Finish();
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                    if (Spell.StartNormal.HasValue)
                    {
                        if (moveLerp(Spell.StartNormal.Value, mSpeed, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    else
                    {
                        if (moveTo(mLaunchDirection, mSpeed, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    break;
                case SpellTemplate.MotionType.StraightPingPong:
                    if (Spell.IsHitted)
                    {
                        if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                        if (traceToTarget(Spell.Sender.WaistPosition, mSpeed, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    else
                    {
                        if (Spell.StartNormal.HasValue)
                        {
                            if (moveLerp(Spell.StartNormal.Value, mSpeed, intervalMS))
                            {
                                Spell.Finish();
                            }
                        }
                        else
                        {
                            if (moveTo(mLaunchDirection, mSpeed, intervalMS))
                            {
                                Spell.Finish();
                            }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Boomerang:
                    if (mSpeed < 0)
                    {
                        if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                        if (traceToTarget(Spell.Sender.WaistPosition, -mSpeed, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    else
                    {
                        if (Spell.StartNormal.HasValue)
                        {
                            if (moveLerp(Spell.StartNormal.Value, mSpeed, intervalMS))
                            {
                                Spell.Finish();
                            }
                        }
                        else
                        {
                            if (moveTo(mLaunchDirection, mSpeed, intervalMS))
                            {
                                Spell.Finish();
                            }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    if (moveTo(Spell.Direction, mSpeed, intervalMS))
                    {
                        Spell.Finish();
                    }
                    break;
                case SpellTemplate.MotionType.Backward:
                    {
                        if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                        if (traceToTarget(Spell.Sender.WaistPosition, mSpeed, intervalMS))
                        {
                            Spell.Finish();
                        }
                    }
                    break;

                case SpellTemplate.MotionType.AOE:
                    updateAOE(intervalMS);
                    break;
                case SpellTemplate.MotionType.AOE_Binding:
                    updateAOE(intervalMS);
                    if (Spell.Sender != null && Spell.Sender.Enable)
                    {
                        updateBinding(intervalMS, Spell.Sender);
                    }
                    else
                    {
                        Spell.Finish(true);
                    }
                    break;
                case SpellTemplate.MotionType.AOE_BindingTarget:
                    updateAOE(intervalMS);
                    if (Spell.TargetUnit != null && Spell.TargetUnit.IsActive)
                    {
                        updateBinding(intervalMS, Spell.TargetUnit);
                    }
                    else
                    {
                        Spell.Finish(true);
                    }
                    break;


                case SpellTemplate.MotionType.Binding:
                    if (Spell.Sender != null && Spell.Sender.Enable)
                    {
                        updateBinding(intervalMS, Spell.Sender);
                    }
                    else
                    {
                        Spell.Finish(true);
                    }
                    break;
                case SpellTemplate.MotionType.BindingTarget:
                    if (Spell.TargetUnit != null && Spell.TargetUnit.IsActive)
                    {
                        updateBinding(intervalMS, Spell.TargetUnit);
                    }
                    else
                    {
                        Spell.Finish(true);
                    }
                    break;

                case SpellTemplate.MotionType.Missile:
                    if (Spell.TargetUnit != null)
                    {
                        if (Info.SeekingTurningAngleSEC != 0)
                        {
                            traceToTargetTunning(Spell.TargetUnit.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                        }
                        else
                        {
                            if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.TargetUnit.Position);
                            traceToTarget(Spell.TargetUnit.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        moveTo(mLaunchDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    if (Spell.TargetUnit != null)
                    {
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(intervalMS))
                        {
                            if (Info.SeekingTurningAngleSEC != 0)
                            {
                                traceToTargetTunning(Spell.TargetUnit.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                            }
                            else
                            {
                                if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.TargetUnit.Position);
                                traceToTarget(Spell.TargetUnit.WaistPosition, mSpeed, intervalMS);
                            }
                        }
                        else
                        {
                            moveTo(mLaunchDirection, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        moveTo(mLaunchDirection, mSpeed, intervalMS);
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(intervalMS))
                        {
                            if (Spell.TrySeekAttackable(Info.SeekingRange, true, out var dst))
                            {
                                Spell.TargetUnit = dst;
                            }
                            //Spell.TargetUnit = Spell.SeekAttackable(Info.SeekingRange, true);
                            //if (Spell.TargetUnit != null)
                            //{
                            //Parent.PostObjectEvent(this, new SpellLockTargetEvent(ID, Spell.TargetUnit.ID, this.Position));
                            //}
                        }
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    if (Spell.TargetUnit == null)
                    {
                        if (mSeekingCooldownTime == null || mSeekingCooldownTime.Update(intervalMS))
                        {
                            if (Spell.TrySeekAttackable(Info.SeekingRange, true, out var dst))
                            {
                                Spell.TargetUnit = dst;
                                Spell.SetPosition(GetBindingPos(Spell.TargetUnit));
                            }
                            //                             Spell.TargetUnit = Spell.SeekAttackable(Info.SeekingRange, true);
                            //                             if (Spell.TargetUnit != null)
                            //                             {
                            //                                 Spell.SetPosition(GetBindingPos(Spell.TargetUnit));
                            //                                 // Parent.PostObjectEvent(this, new SpellLockTargetEvent(ID, Spell.TargetUnit.ID, this.Position));
                            //                             }
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (Spell.Sender != null && Spell.Sender.Enable && Spell.TargetUnit != null && Spell.TargetUnit.IsActive)
                    {
                        Spell.FaceTo(Spell.TargetUnit.Position);
                        updateBinding(intervalMS, Spell.Sender);
                        UpdateChain(intervalMS, Spell.Sender);
                    }
                    else
                    {
                        Spell.Finish(true);
                    }
                    break;
            }

            if (Info.BodyShape == SpellTemplate.Shape.LineToTarget)
            {
                if (Spell.TargetUnit != null)
                {
                    Spell.FaceTo(Spell.TargetUnit.Position);
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToTargetPos)
            {
                if (Spell.TargetPos != null)
                {
                    Spell.SpellDisplayDistance = Spell.SpellDistance = Vector3.Distance(Spell.TargetPos.Value, Spell.Position);
                    Spell.FaceTo(Spell.TargetPos.Value);
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                Spell.FaceTo(mStartPos);
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                Spell.FaceTo(Spell.Sender.Position);
            }
            else if (Info.RotateSpeedSEC != 0)
            {
                Spell.Turn(MoveHelper.GetDistance(intervalMS, mRotateSpeed));
            }

            if (Info.IsBindingOrbit)
            {
                mDistanceSpeed += MoveHelper.GetDistance(intervalMS, Info.MDistanceSpeedSEC);
                mDistanceSpeed += MoveHelper.UpdateSpeed(intervalMS, mDistanceSpeed, Info.MDistanceSpeedAdd,
                    Info.MDistanceSpeedAcc, Info.MDistanceSpeed_MIN, Info.MDistanceSpeed_MAX);
            }
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

        }

        private void updateAOE(float intervalMS)
        {
            switch (Info.BodyShape)
            {
                case SpellTemplate.Shape.LineToTarget:
                case SpellTemplate.Shape.LineToTargetPos:
                case SpellTemplate.Shape.LineToStart:
                case SpellTemplate.Shape.LineToSender:
                    break;
                case SpellTemplate.Shape.Strip:
                case SpellTemplate.Shape.StripRay:
                case SpellTemplate.Shape.StripRayTouchEnd:
                case SpellTemplate.Shape.RectStrip:
                case SpellTemplate.Shape.RectStripRay:
                case SpellTemplate.Shape.WideStrip:
                    Spell.SpellDistance = updateAoeMotion(intervalMS, Info.Distance, Spell.SpellDistance);
                    break;
                default:
                    Spell.SpellSize = updateAoeMotion(intervalMS, Info.BodySize, Spell.SpellSize);
                    break;
            }
        }

        private float updateAoeMotion(float intervalMS, float base_value, float value)
        {
            switch (Info.AOEMType)
            {
                case SpellTemplate.AoeMotionType.Sine:
                    value = (float)Math.Sin(CMath.PI_F * Spell.PassTimeMS / (float)Info.LifeTimeMS) * base_value;
                    break;
                case SpellTemplate.AoeMotionType.Linear:
                default:
                    value += MoveHelper.GetDistance(intervalMS, mSpeed);
                    break;
            }
            return value;
        }
        private void updateImmovability(float intervalMS, IZoneObject target)
        {
            if ((target is IZoneUnit target_unit))
            {
                if (Info.RemoveOnBindingUncontrollable)
                {
                    // 目标不可操控，停止法术 //
                    if (target_unit.IsControllable == false)
                    {
                        Spell.Finish(true);
                        return;
                    }
                }
                if (Info.RemoveOnBindingSkillOver)
                {
                    // 目标非技能状态，停止法术 //
                    if (Spell.CheckRemoveOnBindingSkillOver(target_unit))
                    {
                        Spell.Finish(true);
                        return;
                    }
                    //                     if (target_unit.CurrentState is InstanceUnit.StateSkill)
                    //                     {
                    //                         if (mBindingSkill == null)
                    //                         {
                    //                             mBindingSkill = target_unit.CurrentState as InstanceUnit.StateSkill;
                    //                         }
                    //                         else if (mBindingSkill != target_unit.CurrentState)
                    //                         {
                    //                             Spell.Finish(true);
                    //                             return;
                    //                         }
                    //                     }
                    //                     else
                    //                     {
                    //                         Spell.Finish(true);
                    //                         return;
                    //                     }
                }
            }
        }


        private void updateBinding(float intervalMS, IZoneObject target)
        {
            if (Spell.CheckBinding(target) == false)
            {
                Spell.Finish(true);
                return;
            }
            else if (target is IZoneUnit target_unit)
            {
                if (Info.RemoveOnBindingUncontrollable)
                {
                    // 目标不可操控，停止法术 //
                    if (target_unit.IsControllable == false)
                    {
                        Spell.Finish(true);
                        return;
                    }
                }
                if (Info.RemoveOnBindingSkillOver)
                {
                    // 目标非技能状态，停止法术 //
                    if (Spell.CheckRemoveOnBindingSkillOver(target_unit))
                    {
                        Spell.Finish(true);
                        return;
                    }
                    //                     if (target_unit.CurrentState is InstanceUnit.StateSkill)
                    //                     {
                    //                         if (mBindingSkill == null)
                    //                         {
                    //                             mBindingSkill = target_unit.CurrentState as InstanceUnit.StateSkill;
                    //                         }
                    //                         else if (mBindingSkill != target_unit.CurrentState)
                    //                         {
                    //                             Spell.Finish(true);
                    //                             return;
                    //                         }
                    //                     }
                    //                     else
                    //                     {
                    //                         Spell.Finish(true);
                    //                         return;
                    //                     }
                }
                if (Info.IsBinding)
                {
                    if (Info.IsBindingDirection)
                    {
                        Spell.FaceTo(target.Direction);
                    }
                    Spell.SetPosition(GetBindingPos(target));
                }
            }

        }

        private void UpdateChain(float intervalMS, IZoneObject target)
        {
            if (Spell.CheckBinding(target) == false)
            {
                Spell.Finish(true);
                return;
            }
        }

        //------------------------------------------------------------------------------------------------------

        public bool moveTo(float direction, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (Info.BodyVoxelAnchor == VoxelAnchor.Flooring)
            {
                //if (Zone.Terrain3D.TryGetVoxelLayerByPos(in pos, out var layer, true))
                {
                    if (Zone.Terrain3D.TryMoveSpellOnFloor(ref pos, direction, distance))
                    {
                        Spell.SetPosition(pos);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                VectorHelper.MovePolar(ref pos, direction, distance);
                Spell.SetPosition(pos);
                return false;
            }
        }
        public bool moveLerp(in Vector3 normal, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            pos = VectorHelper.MoveLerp(pos, normal, distance);
            Spell.SetPosition(pos);
            return false;
        }
        public bool traceToTarget(in Vector3 target, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            var ret = VectorHelper.MoveTo3D(ref pos, in target, distance);
            Spell.SetPosition(pos);
            return ret;
        }
        public void traceToTargetTunning(in Vector3 target, float speedSEC, float tunningSpeedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            var dir = Spell.Direction;
            MoveHelper.MoveToTargetTunning(ref pos, ref dir, target, speedSEC, tunningSpeedSEC, intervalMS);
            Spell.SetPosition(pos);
            Spell.FaceTo(dir);
        }
        public bool projectileToTarget(in Vector3 target, float intervalMS)
        {
            var start = mStartPos;
            var pos = Spell.Position;
            if (mSpeedZ < 0 && pos.Z <= target.Z) return true;
            var distance = MotionHelper.GetDistance(intervalMS, mSpeed);
            {
                var totalDistanceQ = VectorHelper.GetDistanceSquare(start.X, start.Y, target.X, target.Y);
                var targetDistanceQ = VectorHelper.GetDistanceSquare(start.X, start.Y, pos.X, pos.Y);
                if (targetDistanceQ >= totalDistanceQ)
                {
                    distance = 0;
                }
            }
            var offsetZ = MotionHelper.GetDistance(intervalMS, mSpeedZ);
            var gravity = Info.MCannonGravitySEC > 0 ? Info.MCannonGravitySEC : Spell.Zone.CFG.GLOBAL_GRAVITY;
            mSpeedZ -= MotionHelper.GetDistance(intervalMS, gravity);
            if (distance != 0)
            {
                VectorHelper.MovePolar(ref pos, mLaunchDirection, distance);
            }
            pos.Z += offsetZ;
            if (mSpeedZ < 0 && pos.Z < target.Z)
            {
                pos.Z = target.Z;
                Spell.SetPosition(pos);
                return true;
            }
            else
            {
                Spell.SetPosition(pos);
                return false;
            }
        }

        //----------------------------------------------------------------------------------------------

        public override Vector3 GetBindingPos(IZoneObject target)
        {
            var bindingP = target.WaistPosition;
            switch (Info.BodyVoxelAnchor)
            {
                case VoxelAnchor.Ceiling:
                    bindingP.Z = target.HeadPosition.Z + Info.BindingOffsetZ;
                    break;
                case VoxelAnchor.Floating:
                    bindingP.Z = target.WaistPosition.Z + Info.BindingOffsetZ;
                    break;
                case VoxelAnchor.Flooring:
                    bindingP.Z = target.Position.Z + Info.BindingOffsetZ;
                    break;
            }
            if (Info.BindingOffsetDistance != 0)
            {
                var offset = DeepCore.Geometry.VectorHelper.Polar(Spell.Direction + CMath.ToPI(Info.BindingOffsetAngle360), Info.BindingOffsetDistance);
                bindingP.X += offset.X;
                bindingP.Y += offset.Y;
            }
            if (Info.IsBindingOrbit)
            {
                if (Info.OrbitDistance != 0 || mDistanceSpeed != 0)
                {

                    var dadd = Info.OrbitDistance + mDistanceSpeed;
                    var dir = Spell.Direction;
                    var ox = (float)Math.Cos(dir) * dadd;
                    var oy = (float)Math.Sin(dir) * dadd;
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

        //------------------------------------------------------------------------------------------------------ }
    }
}