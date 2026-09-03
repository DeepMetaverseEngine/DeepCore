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
    public class SlaveSpellMotion : ISpellMotion
    {
        protected Vector3 mStartPos;
        //        protected Vector3 mDistancePos;
        //protected Vector3? mStartNormal;
        //         protected float mSizeLimit, mDisplaySize;
        //         protected float mDistanceLimit, mDisplayDistance;
        protected float mStartDirection;
        protected float mPassTimeMS;
        protected float mSpeed;
        protected float mSpeedZ;
        protected float mDistanceSpeed;
        protected float mRotateSpeed;
        public override float CurrentSpeed {  get { return mSpeed; } set { mSpeed = value; } }
        //----------------------------------------------------------------------------------------------
        public override ISpellMotion Init(IZoneSpell spell)
        {
            base.Init(spell);
            this.mStartPos = spell.Position;
            this.mStartDirection = spell.Direction;
            //this.mStartNormal = spell.StartNormal;
            //             this.mDistanceLimit = this.mDisplayDistance = Info.Distance;
            //             this.mSizeLimit = this.mDisplaySize = Info.BodySize;
            this.mPassTimeMS = 0;
            this.mSpeed = spell.StartSpeed;
            this.mDistanceSpeed = 0;
            this.mRotateSpeed = Info.RotateSpeedSEC;
            //if (syn.HasSpeed)
            {
                //    this.mSpeed = Spell.StartSpeed;
            }
            //             if (spell.IsFromSpellMagnitude)
            //             {
            //                 if (Spell.Sender is IZoneSpell senderSpell && senderSpell.StartNormal.HasValue)
            //                 {
            //                     var srcNormal = senderSpell.StartNormal.Value * senderSpell.Motion.CurrentSpeed;
            //                     var dstNormal = mStartNormal * Spell.StartSpeed;
            //                     var normal = dstNormal + srcNormal;
            //                     mStartNormal = Vector3.Normalize(normal);//* senderSpell.Motion.CurrentSpeed;
            //                     mSpeed = normal.Length();
            //                 }
            //             }
            return this;
        }
        protected override void Disposing()
        {
            //
            this.mStartPos = default;
            //            this.mDistancePos = default;
            //this.mStartNormal = default;
            //             this.mSizeLimit = default;
            //             this.mDisplaySize = default;
            //             this.mDistanceLimit = default;
            //             this.mDisplayDistance = default;
            this.mStartDirection = default;
            this.mPassTimeMS = default;
            this.mSpeed = default;
            this.mSpeedZ = default;
            base.Disposing();
        }
        public override void OnAdded()
        {
            this.mSpeed = Spell.StartSpeed;
            var mLaunchData = Spell.LaunchData;
            var mInfo = Spell.Template;
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Straight:
                case SpellTemplate.MotionType.StraightPingPong:
                case SpellTemplate.MotionType.Boomerang:
                    if (Spell.TargetPos == null && Spell.TargetUnit != null)
                    {
                        Spell.TargetPos = Spell.TargetUnit.WaistPosition;
                    }
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (Spell.TargetPos.HasValue)
                    {
                        MoveHelper.CalculateSpellLaunchAngle(Info, in mStartPos, Spell.TargetPos.Value, Zone.CFG.GLOBAL_GRAVITY,
                            out var muzzleAngle,
                            out mStartDirection,
                            out mSpeed,
                            out mSpeedZ);
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
                            mStartPos = GetBindingPos(Spell.Sender);
                        }
                    }
                    break;
                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.Binding:
                    if (Spell.Sender != null)
                    {
                        mStartPos = GetBindingPos(Spell.Sender);
                    }
                    break;
                case SpellTemplate.MotionType.AOE_BindingTarget:
                case SpellTemplate.MotionType.BindingTarget:
                    if (Spell.TargetUnit != null)
                    {
                        mStartPos = GetBindingPos(Spell.TargetUnit);
                    }
                    break;
                case SpellTemplate.MotionType.AOE:

                    break;
            }
            Spell.SetPosition(this.mStartPos);
            Spell.FaceTo(this.mStartDirection);
        }
        /// <summary>
        /// 更新移动行为
        /// </summary>
        public override void UpdateMotion(float intervalMS)
        {
            updateMotion(intervalMS, !Spell.IsForceSync);
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
            updateLength();
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.AOE:
                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.AOE_BindingTarget:
                    updateAOE(intervalMS);
                    break;
            }
            switch (Info.BodyShape)
            {
                case SpellTemplate.Shape.StripRayTouchEnd:
                    updateRayTouchEnd();
                    break;
                case SpellTemplate.Shape.LineToStart:
                case SpellTemplate.Shape.LineToTarget:
                case SpellTemplate.Shape.LineToTargetPos:
                case SpellTemplate.Shape.LineToSender:
                    updateLineToTarget();
                    break;
            }
        }



        //-----------------------------------------------------------------------
        #region _UpdateMotions_

        /// <summary>
        /// 更新移动行为
        /// </summary>
        private void updateMotion(float intervalMS, bool clientMove)
        {
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.SelectLauncher:
                case SpellTemplate.MotionType.SelectTarget:
                    break;
                case SpellTemplate.MotionType.Cannon:
                    if (Spell.TargetPos != null)
                    {
                        if (clientMove) PreProjectileToTarget(Spell.TargetPos.Value, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.Straight:
                    if (Spell.StartNormal.HasValue)
                    {
                        if (clientMove) PreMoveLerp(Spell.StartNormal.Value, mSpeed, intervalMS);
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.StraightPingPong:
                    if (Spell.IsHitted)
                    {
                        if (Spell.Sender != null)
                        {
                            if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                            if (clientMove)
                            {
                                if (clientMove) PreTraceToTarget(Spell.Sender.WaistPosition, mSpeed, intervalMS);
                            }
                        }
                    }
                    else
                    {
                        if (Spell.StartNormal.HasValue)
                        {
                            if (clientMove) PreMoveLerp(Spell.StartNormal.Value, mSpeed, intervalMS);
                        }
                        else
                        {
                            if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Boomerang:
                    if (mSpeed < 0)
                    {
                        if (Spell.Sender != null)
                        {
                            if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                            if (clientMove)
                            {
                                if (clientMove) PreTraceToTarget(Spell.Sender.WaistPosition, -mSpeed, intervalMS);
                            }
                        }
                    }
                    else
                    {
                        if (Spell.StartNormal.HasValue)
                        {
                            if (clientMove) PreMoveLerp(Spell.StartNormal.Value, mSpeed, intervalMS);
                        }
                        else
                        {
                            if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                        }
                    }
                    break;
                case SpellTemplate.MotionType.Forward:
                    if (clientMove) PreMoveTo(Spell.Direction, mSpeed, intervalMS);
                    break;
                case SpellTemplate.MotionType.Backward:
                    if (Spell.Sender != null)
                    {
                        if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.Sender.Position);
                        if (clientMove)
                        {
                            if (clientMove) PreTraceToTarget(Spell.Sender.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                   
                    break;

                case SpellTemplate.MotionType.Immovability:
                case SpellTemplate.MotionType.AOE:
                    if (clientMove)
                    {
                        Spell.SetPosition(Spell.RemotePosition);
                    }
                    break;


                case SpellTemplate.MotionType.AOE_Binding:
                case SpellTemplate.MotionType.Binding:
                    if (Spell.Sender != null)
                    {
                        updateBinding(Spell.Sender);
                    }
                    else
                    {
                        if (clientMove) adjustPos(MoveHelper.GetDistance(intervalMS, mSpeed));
                    }
                    break;
                case SpellTemplate.MotionType.AOE_BindingTarget:
                case SpellTemplate.MotionType.BindingTarget:
                    if (Spell.TargetUnit != null)
                    {
                        updateBinding(Spell.TargetUnit);
                    }
                    else
                    {
                        if (clientMove) adjustPos(MoveHelper.GetDistance(intervalMS, mSpeed));
                    }
                    break;

                case SpellTemplate.MotionType.Missile:
                    if (Spell.TargetUnit != null)
                    {
                        if (Info.SeekingTurningAngleSEC != 0)
                        {
                            if (clientMove) PreTraceToTargetTunning(Spell.TargetUnit.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                        }
                        else
                        {
                            if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.TargetUnit.Position);
                            if (clientMove) PreTraceToTarget(Spell.TargetUnit.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerMissile:
                    if (Spell.TargetUnit != null)
                    {
                        if (Info.SeekingTurningAngleSEC != 0)
                        {
                            if (clientMove) PreTraceToTargetTunning(Spell.TargetUnit.WaistPosition, mSpeed, Info.SeekingTurningAngleSEC, intervalMS);
                        }
                        else
                        {
                            if (Info.RotateSpeedSEC == 0) Spell.FaceTo(Spell.TargetUnit.Position);
                            if (clientMove) PreTraceToTarget(Spell.TargetUnit.WaistPosition, mSpeed, intervalMS);
                        }
                    }
                    else
                    {
                        if (clientMove) PreMoveTo(mStartDirection, mSpeed, intervalMS);
                    }
                    break;
                case SpellTemplate.MotionType.SeekerSelectTarget:
                    break;
                case SpellTemplate.MotionType.Chain:
                    if (Spell.Sender != null && Spell.Sender.Enable && Spell.TargetUnit != null && Spell.TargetUnit.IsActive)
                    {
                        updateBinding(Spell.Sender);
                        Spell.FaceTo(Spell.TargetUnit.Position);
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
                    Spell.FaceTo(Spell.TargetPos.Value);
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                Spell.FaceTo(mStartPos);
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                if (Spell.Sender != null)
                {
                    Spell.FaceTo(Spell.Sender.Position);
                }
            }
            else if (Info.RotateSpeedSEC != 0)
            {
                Spell.Turn(MoveHelper.GetDistance(intervalMS, mRotateSpeed));
            }
            if (Info.IsBindingOrbit)
            {
                mDistanceSpeed += MoveHelper.GetDistance(intervalMS, Info.MDistanceSpeedSEC);
                mDistanceSpeed = MoveHelper.UpdateSpeed(intervalMS, mDistanceSpeed,
                    Info.MDistanceSpeedAdd,
                    Info.MDistanceSpeedAcc,
                    Info.MDistanceSpeed_MIN,
                    Info.MDistanceSpeed_MAX);
            }
        }
        private void updateBinding(IZoneObject binding)
        {
            if (Info.IsBindingDirection)
            {
                Spell.FaceTo(binding.Direction);
            }
            //if (Parent.ActorSyncMode != SyncMode.ForceByServer)
            if (!Spell.IsForceSync)
            {
                var pos = GetBindingPos(binding);
                Spell.SetPosition(pos);
            }
        }



        //---------------------------------------------------------------------------------------------------
        private void adjustPos(float min_distance)
        {
            var lpos = Spell.Position;
            var rpos = Spell.RemotePosition;
            float fdistance = VectorHelper.GetDistance(lpos, rpos);
            if (fdistance < min_distance)
            {
                VectorHelper.MoveTo3D(ref lpos, rpos, min_distance);
            }
            else
            {
                VectorHelper.MoveTo3D(ref lpos, rpos, fdistance / 2f);
            }
            Spell.SetPosition(lpos);
        }

        private void PreMoveTo(float direction, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (Info.BodyVoxelAnchor == VoxelAnchor.Flooring)
            {
                if (Zone.Terrain3D.TryMoveSpellOnFloor(ref pos, direction, distance))
                {
                    Spell.SetPosition(pos);
                }
            }
            else
            {
                VectorHelper.MovePolar(ref pos, direction, distance);
                Spell.SetPosition(pos);
            }
        }
        private void PreMoveLerp(in Vector3 normal, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            pos = VectorHelper.MoveLerp(pos, normal, distance);
            Spell.SetPosition(pos);
        }
        private bool PreProjectileToTarget(in Vector3 target, float intervalMS)
        {
            var pos = Spell.Position;
            if (mSpeedZ < 0 && pos.Z <= target.Z) return true;
            var distance = MotionHelper.GetDistance(intervalMS, mSpeed);
            {
                var totalDistanceQ = VectorHelper.GetDistanceSquare(mStartPos.X, mStartPos.Y, target.X, target.Y);
                var targetDistanceQ = VectorHelper.GetDistanceSquare(mStartPos.X, mStartPos.Y, pos.X, pos.Y);
                if (targetDistanceQ >= totalDistanceQ)
                {
                    distance = 0;
                }
            }
            var offsetZ = MotionHelper.GetDistance(intervalMS, mSpeedZ);
            var gravity = Info.MCannonGravitySEC > 0 ? Info.MCannonGravitySEC : Zone.CFG.GLOBAL_GRAVITY;
            mSpeedZ -= MotionHelper.GetDistance(intervalMS, gravity);
            if (distance != 0)
            {
                VectorHelper.MovePolar(ref pos, mStartDirection, distance);
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
        private void PreTraceToTarget(Vector3 target, float speedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            VectorHelper.MoveTo3D(ref pos, in target, distance);
            Spell.SetPosition(pos);
        }
        public void PreTraceToTargetTunning(Vector3 target, float speedSEC, float tunningSpeedSEC, float intervalMS)
        {
            var pos = Spell.Position;
            var dir = Spell.Direction;
            MoveHelper.MoveToTargetTunning(ref pos, ref dir, target, speedSEC, tunningSpeedSEC, intervalMS);
            Spell.SetPosition(pos);
            Spell.FaceTo(dir);
        }
        #endregion

        //---------------------------------------------------------------------------------------------------
        #region _UpdateShape_

        private void updateLength()
        {
            Spell.RayTouchPoint = Spell.Position;
            switch (Info.MType)
            {
                case SpellTemplate.MotionType.Chain:
                    if (Spell.Sender != null && Spell.Sender.Enable && Spell.TargetUnit != null && Spell.TargetUnit.IsActive)
                    {
                        Spell.RayTouchPoint = Spell.TargetUnit.WaistPosition;
                    }
                    break;
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
                    Spell.SpellDisplayDistance = Spell.SpellDistance = updateAoeMotion(intervalMS, Info.Distance, Spell.SpellDistance);
                    break;
                default:
                    Spell.SpellDisplaySize = Spell.SpellSize = updateAoeMotion(intervalMS, Info.BodySize, Spell.SpellSize);
                    break;
            }
        }
        private float updateAoeMotion(float intervalMS, float base_value, float value)
        {
            switch (Info.AOEMType)
            {
                case SpellTemplate.AoeMotionType.Sine:
                    value = (float)Math.Sin(CMath.PI_F * mPassTimeMS / (float)Info.LifeTimeMS) * base_value;
                    break;
                case SpellTemplate.AoeMotionType.Linear:
                default:
                    value += MoveHelper.GetDistance(intervalMS, mSpeed);
                    break;
            }
            return value;
        }

        private void updateRayTouchEnd()
        {
            if (Spell.LauncherUnit != null)
            {
                var ray = VoxelStripe.InitFromRay(Spell.Position, Spell.Direction, Info.RectWide, Spell.SpellDistance, Spell.BodyHeight);
                if (Spell.TryRayCastTouchEndUnit(ray, out var target))
                {
                    var src = Spell.Position;
                    var dst = target.Position;
                    Spell.SpellDisplayDistance = CMath.GetDistance(src.X, src.Y, dst.X, dst.Y);
                    Spell.SpellDisplayDistance = Math.Min(Spell.SpellDisplayDistance, Spell.SpellDistance);
                    Spell.RayTouchPoint = new Vector3(dst.X, dst.Y, src.Z);
                }
                else
                {
                    var src = Spell.Position;
                    Spell.SpellDisplayDistance = Spell.SpellDistance;
                    Spell.RayTouchPoint = new Vector3(ray.LineQ.X, ray.LineQ.Y, src.Z);
                }
                //                 using (var list = ObjectPool.AllocList<LayerUnit>())
                //                 {
                //                     Parent.ForEachNearObjectsRectPredicate(ray.LineP.X, ray.LineP.Y, ray.LineQ.X, ray.LineQ.Y, Parent, (LayerZone st, LayerUnit zu) =>
                //                     {
                //                         if (Parent.IsAttackable(Launcher, zu, Info.ExpectTarget))
                //                         {
                //                             //if ((CMath.intersectLineRound(this.X, this.Y, p1.X, p1.Y, zu.X, zu.Y, d_width + zu.BodyHitSize)))
                //                             if (zu.VoxelBody.Intersects(in ray))
                //                             {
                //                                 list.Add(zu);
                //                             }
                //                         }
                //                         return false;
                //                     });
                //                     if (list.Count > 0)
                //                     {
                //                         list.Sort(new Helper.ObjectBodySorterNearest<ILayerZoneEntity>(this.Position, 0));
                //                         this.mDisplayDistance = CMath.GetDistance(this.X, this.Y, list[0].X, list[0].Y);
                //                         this.mDisplayDistance = Math.Min(mDisplayDistance, this.mDistanceLimit);
                //                         this.mDistancePos.Value = new System.Numerics.Vector3(list[0].X, list[0].Y, this.Z);
                //                     }
                //                     else
                //                     {
                //                         this.mDisplayDistance = this.mDistanceLimit;
                //                         this.mDistancePos.Value = new System.Numerics.Vector3(ray.LineQ.X, ray.LineQ.Y, this.Z);
                //                     }
                //                 }
            }
        }

        private void updateLineToTarget()
        {
            if (Info.BodyShape == SpellTemplate.Shape.LineToTarget)
            {
                if (Spell.TargetUnit != null)
                {
                    var distance = Math.Min(Vector3.Distance(Spell.Position, Spell.TargetUnit.Position), Spell.SpellDistance);
                    Spell.SpellDisplayDistance = distance;
                    Spell.RayTouchPoint = Spell.TargetUnit.WaistPosition.Value;
                    Spell.FaceTo(Spell.TargetUnit.Position);
                }
                else
                {
                    Spell.SpellDisplayDistance = 0;
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToTargetPos)
            {
                if (Spell.TargetPos != null)
                {
                    var distance = Math.Min(Vector3.Distance(Spell.Position, Spell.TargetPos.Value), Spell.SpellDistance);
                    Spell.SpellDisplayDistance = distance;
                    Spell.RayTouchPoint = Spell.TargetPos.Value;
                    Spell.FaceTo(Spell.TargetPos.Value);
                }
                else
                {
                    Spell.SpellDisplayDistance = 0;
                }
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToStart)
            {
                var distance = Math.Min(Vector3.Distance(Spell.Position, mStartPos), Spell.SpellDistance);
                Spell.SpellDisplayDistance = distance;
                Spell.RayTouchPoint = mStartPos.Value;
                Spell.FaceTo(mStartPos);
            }
            else if (Info.BodyShape == SpellTemplate.Shape.LineToSender)
            {
                if (Spell.Sender != null)
                {
                    var distance = Math.Min(Vector3.Distance(Spell.Position, Spell.Sender.Position), Spell.SpellDistance);
                    Spell.SpellDisplayDistance = distance;
                    Spell.RayTouchPoint = Spell.Sender.WaistPosition.Value;
                    Spell.FaceTo(Spell.Sender.Position);
                }
                else
                {
                    Spell.SpellDisplayDistance = 0;
                }
            }
            else
            {
                Spell.SpellDisplayDistance = 0;
            }
        }

        #endregion
        //------------------------------------------------------------------------------------------------------

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
                    float dadd = Info.OrbitDistance + mDistanceSpeed;
                    float ox = (float)Math.Cos(Spell.Direction) * dadd;
                    float oy = (float)Math.Sin(Spell.Direction) * dadd;
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
    }



}