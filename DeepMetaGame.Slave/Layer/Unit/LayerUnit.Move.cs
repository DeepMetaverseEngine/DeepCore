using DeepCore.Game3D.Slave.Data;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneGeometry;
using System;

namespace DeepCore.Game3D.Slave.Layer
{
    public partial class LayerUnit
    {
        public virtual void PreSetPos(in Geometry.Vector3 pos)
        {
            //             mLocalPos.x = pos.X;
            //             mLocalPos.y = pos.Y;
            //             mLocalPos.z = pos.Z;
            mLocalPos.SetPos(pos);
        }
        protected virtual void PreAxisMove(UnitAxisAction axis, float intervalMS)
        {
            float ispeed = MoveHelper.GetDistance(intervalMS, this.MoveSpeedSEC);
            float direction = axis.angle;
            float addX = (float)(Math.Cos(direction) * ispeed);
            float addY = (float)(Math.Sin(direction) * ispeed);
            this.PreBlockMove(addX, addY);
        }

        public void PreFaceTo(float d)
        {
            this.mDirection.SyncFace(d);
        }
        public void PreFaceTo(float x, float y)
        {
            if (this.X == x && this.Y == y)
            {
                return;
            }
            float d = (float)(Math.Atan2(y - this.Y, x - this.X));
            PreFaceTo(d);
        }
        public void PreTurnFace(float add)
        {
            this.mDirection.TurnFace(add);
        }
        public void PreTurnFaceTo(Geometry.Vector2 target, float turnSpeed, float intervalMS)
        {
            var add = MoveHelper.DirectionChange(this.Position, this.Direction, target, turnSpeed, intervalMS);
            this.mDirection.TurnFace(add);
        }

        /// <summary>
        /// 被别的单位挤开
        /// </summary>
        /// <returns></returns>
        public virtual bool PreElasticOtherObjects()
        {
            if (IsLock)
            {
                return false;
            }
            if (this.TouchObj)
            {
                bool force_sync = false;
                Parent.ForEachNearObjectsPredicate<LayerZone, LayerUnit>(X, Y, Parent, (z, u) =>
                {
                    if ((u != this) && (u.TouchObj) && Parent.TouchObject2(this, u))
                    {
                        PreElasticOtherObject(u);
                        force_sync = true;
                    }
                    return false;
                });
                return force_sync;
            }
            return false;
        }

        /// <summary>
        /// 被别的单位挤开
        /// </summary>
        /// <param name="o"></param>
        public virtual void PreElasticOtherObject(LayerUnit o)
        {
            if (IsLock)
            {
                return;
            }
            float targetAngle = MathVector.getDegree(this.X, this.Y, o.X, o.Y);
            float ddr = MathVector.getDistance(this.X, this.Y, o.X, o.Y);
            float bdr = (this.BodyBlockSize + o.BodyBlockSize);
            float distance = -(bdr - ddr);
            float dx = (float)(Math.Cos(targetAngle) * distance);
            float dy = (float)(Math.Sin(targetAngle) * distance);
            float oldx = mLocalPos.X;
            float oldy = mLocalPos.Y;
            if (this.TouchMap)
            {
                mLocalPos.Move(dx, 0);
                if (Parent.TouchMap(this))
                {
                    mLocalPos.Move(-dx, 0);
                }
                mLocalPos.Move(0, dy);
                if (Parent.TouchMap(this))
                {
                    mLocalPos.Move(0, -dy);
                }
            }
            else
            {
                mLocalPos.Move(dx, dy);
            }
        }
        /// <summary>
        /// 尝试客户端移动
        /// </summary>
        /// <returns>TRUE=无法移动</returns>
        public MoveBlockResult PreMoveToTarget(float x, float y, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                return new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
            }
            if (mLocalPos.X == x && mLocalPos.Y == y)
            {
                MoveBlockResult result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                return result;
            }
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float ddx = x - mLocalPos.X;
            float ddy = y - mLocalPos.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                MoveBlockResult result = PreBlockMoveInternal(ddx, ddy);
                if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                {

                }
                else
                {
                    result.result |= MoveResult.MOVE_RESULT_ARRIVED;
                }
                return result;
            }
            else
            {
                float oldx = mLocalPos.X;
                float oldy = mLocalPos.Y;
                float direction = (float)(Math.Atan2(ddy, ddx));
                float dx = (float)(Math.Cos(direction) * distance);
                float dy = (float)(Math.Sin(direction) * distance);
                MoveBlockResult result = PreBlockMoveInternal(dx, dy);
                ddx = Math.Abs(mLocalPos.X - oldx);
                ddy = Math.Abs(mLocalPos.Y - oldy);
                float minstep = Parent.MinStep;
                if (ddx < minstep && ddy < minstep)
                {
                    result.result |= MoveResult.MOVE_RESULT_MIN_STEP;
                }
                return result;
            }
        }

        /// <summary>
        /// 尝试客户端移动
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="speedSEC"></param>
        /// <param name="intervalMS"></param>
        /// <returns>TRUE=无法移动</returns>
        public MoveBlockResult PreMoveTo(float angle, float speedSEC, float intervalMS)
        {
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            return PreBlockMoveInternal(dx, dy);
        }

        /// <summary>
        /// 和所有单位（包括地图）做碰撞检测，是否阻挡
        /// </summary>
        /// <param name="addX"></param>
        /// <param name="addY"></param>
        /// <returns>阻挡</returns>
        public virtual MoveBlockResult PreBlockMove(float addX, float addY)
        {
            return PreBlockMoveInternal(addX, addY);
        }

        public virtual MoveBlockResult PreBlockMoveInternal(float addX, float addY)
        {
            if (IsLock)
            {
                return new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
            }
            var oldpos = mLocalPos.Position;
            MoveBlockResult result = new MoveBlockResult(0);
            if (this.TouchMap)
            {
                //尝试地图碰撞移动//
                switch (mLocalPos.TryMoveToMapBorder(addX, addY))
                {
                    case TryMoveToMapBorderResult.BLOCK:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        return result;
                    case TryMoveToMapBorderResult.TOUCH:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case TryMoveToMapBorderResult.ARRIVE:
                        break;
                }
                //和建筑碰撞//
                var zu = Parent.TouchStaticBlock(this);
                if (zu != null)
                {
                    result.touched = zu;
                    PreSetPos(oldpos);
                    if (TryMoveToObjectBorder(zu, oldpos, addX, addY))
                    {
                        var b2 = Parent.TouchStaticBlock(this);
                        if (b2 != null && b2 != zu)
                        {
                            PreSetPos(oldpos);
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return result;
                        }
                        if (Parent.Terrain3D.TouchMapByPos(this, this.Position))
                        {
                            PreSetPos(oldpos);
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                            return result;
                        }
                        var dp = Vector3.DistanceSquared(oldpos, this.Position);
                        if (dp <= Parent.MinStepSquare)
                        {
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return result;
                        }
                        if (oldpos == mLocalPos.Position)
                        {
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return result;
                        }
                    }
                    else
                    {
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                        return result;
                    }
                }
            }
            else
            {
                mLocalPos.Move(addX, addY);
            }
            if (this.TouchObj)
            {
                LayerUnit u = Parent.TouchUnit(this);
                if (u != null)
                {
                    PreSetPos(oldpos);
                    result.touched = u;
                    if (TryMoveToObjectBorder(u, oldpos, addX, addY))
                    {
                        var u2 = Parent.TryTouchUnit(this, Position);
                        if (u2 != null && u2 != u)
                        {
                            PreSetPos(oldpos);
                            result.touched = u2;
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                        }
                    }
                    else
                    {
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                    }
                    return result;
                }
            }
            return result;
        }
        public MoveBlockResult PreJumpToTarget(float tx, float ty, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                MoveBlockResult result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                return result;
            }
            //             float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            //             distance = Math.Min(distance, Geometry.VectorHelper.GetDistance(X, Y, tx, ty));
            if (mLocalPos.X == tx && mLocalPos.Y == ty)
            {
                MoveBlockResult result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                return result;
            }
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            var dir = Geometry.VectorHelper.GetDegree(X, Y, tx, ty);
            return PreJumpTo(dir, distance);
        }
        public MoveBlockResult PreJumpTo(float angle, float speedSEC, float intervalMS)
        {
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            return PreJumpTo(angle, distance);
        }
        public MoveBlockResult PreJumpTo(float angle, float distance)
        {
            if (IsLock)
            {
                return new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
            }
            float addX = (float)(Math.Cos(angle) * distance);
            float addY = (float)(Math.Sin(angle) * distance);
            var oldpos = mLocalPos.Position;
            MoveBlockResult result = new MoveBlockResult(0);
            if (this.TouchMap)
            {
                //尝试地图碰撞移动//
                switch (mLocalPos.TryMoveToMapBorder(addX, addY))
                {
                    case TryMoveToMapBorderResult.BLOCK:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        return result;
                    case TryMoveToMapBorderResult.TOUCH:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case TryMoveToMapBorderResult.ARRIVE:
                        break;
                }
                //和建筑碰撞//
                var zu = Parent.TouchStaticBlock(this);
                if (zu != null)
                {
                    result.touched = zu;
                    PreSetPos(oldpos);
                    if (TryMoveToObjectBorder(zu, oldpos, addX, addY))
                    {
                        var u2 = Parent.TouchStaticBlock(this);
                        if (u2 != null && u2 != zu)
                        {
                            PreSetPos(oldpos);
                            zu = u2;
                            result.touched = zu;
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return result;
                        }
                    }
                    else
                    {
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                        return result;
                    }
                }
            }
            else
            {
                mLocalPos.Move(addX, addY);
            }
            return result;
        }
        public virtual MoveResult PreMoveImpactTo(float x, float y, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                return (MoveResult.MOVE_RESULT_ARRIVED);
            }
            float minstep = Parent.MinStep;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (Math.Abs(distance) < minstep)
            {
                return MoveResult.MOVE_RESULT_MIN_STEP;
            }
            float ddx = x - mLocalPos.X;
            float ddy = y - mLocalPos.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                return MoveResult.MOVE_RESULT_MIN_STEP;
            }
            float angle = MathVector.getDegree(ddx, ddy);
            return PreMoveImpactInternal(angle, distance);
        }

        protected virtual MoveResult PreMoveImpactInternal(float angle, float distance)
        {
            if (IsLock)
            {
                return (MoveResult.MOVE_RESULT_ARRIVED);
            }
            MoveResult ret = MoveResult.MOVE_SMOOTH;
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            var oldpos = mLocalPos.Position;
            if (TouchMap)
            {
                switch (mLocalPos.TryMoveToMapBorder(dx, dy))
                {
                    case TryMoveToMapBorderResult.BLOCK:
                        ret |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        ret |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        return ret;
                    case TryMoveToMapBorderResult.TOUCH:
                        ret |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case TryMoveToMapBorderResult.ARRIVE:
                        break;
                }
                var zu = Parent.TouchStaticBlock(this);
                if (zu != null)
                {
                    PreSetPos(oldpos);
                    if (TryMoveToObjectBorder(zu, oldpos, dx, dy))
                    {
                        var u2 = Parent.TouchStaticBlock(this);
                        if (u2 != null && u2 != zu)
                        {
                            PreSetPos(oldpos);
                            ret |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return ret;
                        }
                    }
                    else
                    {
                        ret |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                        return ret;
                    }
                }
            }
            else
            {
                this.mLocalPos.Move(dx, dy);
            }
            if (this.TouchObj)
            {
                LayerUnit u = Parent.TouchUnit(this);
                if (u != null)
                {
                    PreSetPos(oldpos);
                    ret |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                    if (TryMoveToObjectBorder(u, oldpos, dx, dy))
                    {
                        var u2 = Parent.TouchUnit(this);
                        if (u2 != null && u2 != u)
                        {
                            PreSetPos(oldpos);
                            ret |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                        }
                    }
                    else
                    {
                        ret |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                    }
                }
            }
            return ret;
        }

        public bool TryMoveToObjectBorder(ILayerZoneEntity touched, Geometry.Vector3 mPos, float addX, float addY)
        {
            if (IsLock)
            {
                return (false);
            }
            var zone = this.Parent;
            if (touched.ZoneShape != null)
            {
                Geometry.Vector2 pos = mPos;
                if (touched.ZoneShape.MoveToBorder(ref pos, addX, addY, zone.MinStep))
                {
                    var tpos = new Geometry.Vector3(pos.X, pos.Y, mPos.Z);
                    if (!this.TouchMap || zone.Terrain3D.TryMoveTo(ref tpos))
                    {
                        PreSetPos(tpos);
                        return true;
                    }
                }
            }
            else if (touched is LayerUnit)
            {
                var objT = touched as LayerUnit;
                var dirD = MathVector.getDegree(addX, addY);
                var dirS = MathVector.getDegree(mPos.X, mPos.Y, touched.X, touched.Y);
                if (CMath.RadiansInRange(dirS, dirD - zone.ElasticAngle, zone.ElasticAngle * 2f) == false)
                {
                    var dir = MathVector.getDegree(touched.X, touched.Y, mPos.X + addX, mPos.Y + addY);
                    var pos = new Geometry.Vector3(touched.X, touched.Y, mPos.Z);
                    Geometry.VectorHelper.MovePolar(ref pos, dir, this.BodyBlockSize + objT.BodyBlockSize + zone.MinStep);
                    if (!this.TouchMap || zone.Terrain3D.TryMoveTo(ref pos))
                    {
                        PreSetPos(pos);
                        return true;
                    }
                }
            }
            else if (touched is LayerEditorDecoration)
            {
                var objT = touched as LayerEditorDecoration;
                if (objT.ZoneShape != null)
                {
                    Geometry.Vector2 pos = mPos;
                    if (objT.ZoneShape.MoveToBorder(ref pos, addX, addY, zone.MinStep))
                    {
                        var tpos = new Geometry.Vector3(pos.X, pos.Y, mPos.Z);
                        if (!this.TouchMap || zone.Terrain3D.TryMoveTo(ref tpos))
                        {
                            PreSetPos(tpos);
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------



        public bool HasSkillMove { get => mSkillMove != null && !mSkillMove.IsEnd; }

        protected PreSkillStartMove mSkillMove = null;

        public virtual PreSkillStartMove PreSkillMove(
                float direction,
                float rotateSpeedSEC,
                float expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAcc,
                float moveZSpeed,
                float zgravity,
                bool isNoneTouch)
        {
            ClearSkillMove();
            PreSkillStartMove move = PreSkillStartMove.Alloc(this, direction, rotateSpeedSEC, expectlTimeMS, moveSpeedSEC, moveSpeedAdd, moveSpeedAcc, moveZSpeed, zgravity, isNoneTouch);
            mSkillMove = move;
            return move;
        }
        private void ClearSkillMove()
        {
            if (mSkillMove != null)
            {
                mSkillMove.Stop();
                mSkillMove.Dispose();
                mSkillMove = null;
            }
        }
        public class PreSkillStartMove : LayerStatus
        {
            private LayerUnit owner;
            private float startDirection;
            private float moveSpeedAdd;
            private float moveSpeedAcc;
            private float RotateSpeedSEC;
            private float moveSpeedSEC;
            private FallingDown hasFly;
            private TimeExpire<int> hitMoveTime;
            private bool isNoneTouch;
            private Geometry.Vector3 startPos;
            private LayerObject moveTarget;
            private bool moveTargetBody;
            private float moveTargetKeepRange;
            private LayerObject blockTarget;
            private float blockTargetKeepRange;

            protected PreSkillStartMove() { }
            public static PreSkillStartMove Alloc(
                LayerUnit owner,
                float direction,
                float rotateSpeedSEC,
                float expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAcc,
                float moveZSpeed,
                float zgravity,
                bool isNoneTouch)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<PreSkillStartMove>(static s => new PreSkillStartMove());
                ret.Init(owner,
                     direction,
                     rotateSpeedSEC,
                     expectlTimeMS,
                     moveSpeedSEC,
                     moveSpeedAdd,
                     moveSpeedAcc,
                     moveZSpeed,
                     zgravity,
                     isNoneTouch);
                return ret;
            }
            protected void Init(
                LayerUnit owner,
                float direction,
                float rotateSpeedSEC,
                float expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAcc,
                float moveZSpeed,
                float zgravity,
                bool isNoneTouch)
            {
                this.RotateSpeedSEC = rotateSpeedSEC;

                this.moveSpeedSEC = moveSpeedSEC;
                this.moveSpeedAdd = moveSpeedAdd;
                this.moveSpeedAcc = moveSpeedAcc / 100f;

                this.owner = owner;
                this.startDirection = direction;
                this.isNoneTouch = isNoneTouch;

                this.TotalTimeMS = expectlTimeMS;
                if (moveZSpeed != 0)
                {
                    if (zgravity == 0)
                    {
                        zgravity = owner.Gravity;
                    }
                    //owner.Parent.Terrain3D.TryGetVoxelUpRange(owner.Position, out var down, out var up, out var top);
                    this.hasFly = owner.StartFly(moveZSpeed, zgravity);
                    this.hasFly.Retain();
                    //this.TotalTimeMS = hasFly.ExpectTimeMS;
                }
                //                 else
                //                 {
                //                     this.TotalTimeMS = expectlTimeMS;
                //                 }
                this.startPos = owner.mLocalPos.Position;
                this.hitMoveTime = new TimeExpire<int>(TotalTimeMS);
                this.IsEnd = false;
            }
            protected override void Disposing()
            {
                this.IsEnd = true;
                this.hasFly?.Release();
                this.hasFly = null;
                this.owner = null;
                this.startDirection = 0;
                this.moveSpeedAdd = 0;
                this.moveSpeedAcc = 0;
                this.RotateSpeedSEC = 0;
                this.moveSpeedSEC = 0;
                this.hitMoveTime = null;
                this.isNoneTouch = false;
                this.startPos = Vector3.Zero;
                this.moveTarget = null;
                this.moveTargetBody = false;
                this.moveTargetKeepRange = 0;
                this.blockTarget = null;
                this.blockTargetKeepRange = 0;
            }

            public float StartDirection { get => startDirection; }
            public void SetBlockTarget(LayerObject target, float bodyKeepRange = 0)
            {
                blockTarget = target;
                blockTargetKeepRange = bodyKeepRange;
            }
            public void SetMoveTarget(LayerObject target, bool targetBodyBlock, float targetKeepRange)
            {
                moveTarget = target;
                moveTargetBody = targetBodyBlock;
                moveTargetKeepRange = targetKeepRange;
            }
            public void Stop()
            {
                this.IsEnd = true;
            }

            public bool Update(float intervalMS)
            {
                if (RotateSpeedSEC != 0)
                {
                    float add = MoveHelper.GetDistance(intervalMS, RotateSpeedSEC);
                    owner.PreTurnFace(add);
                }
                if (!testBlock(intervalMS))
                {
                    // 移动 //
                    if (moveTarget != null)
                    {
                        move(intervalMS, moveTarget);
                    }
                    else
                    {
                        move(intervalMS, startDirection);
                    }
                }
                // 递增 //
                {
                    moveSpeedSEC = MoveHelper.UpdateSpeed(intervalMS, moveSpeedSEC, moveSpeedAdd, moveSpeedAcc);
                }
                if (hitMoveTime.Update(intervalMS))
                {
                    if (hasFly != null)
                    {
                        this.IsEnd = hasFly.IsEnd;
                    }
                    else
                    {
                        IsEnd = true;
                    }
                }
                this.TotalDistance = Geometry.Vector3.Distance(startPos, owner.mLocalPos.Position);
                return IsEnd;
            }
            private bool testBlock(float intervalMS)
            {
                if (blockTarget != null)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, moveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (Intersects(owner.Position,
                    blockTarget.Position,
                    blockTarget.BodyBlockSize + owner.BodyBlockSize + distance + blockTargetKeepRange))
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool Intersects(Geometry.Vector3 p1, in Geometry.Vector3 p2, float distance)
            {
                Vector3.DistanceSquared(in p1, in p2, out var pd);
                return pd <= distance * distance;
            }

            //             public bool Update(int intervalMS)
            //             {
            //                 if (RotateSpeedSEC != 0)
            //                 {
            //                     float add = MoveHelper.GetDistance(intervalMS, RotateSpeedSEC);
            //                     owner.PreTurn(add);
            //                 }
            //                 if (moveTargetBody && moveTargetKeepRange > 0 && moveTarget != null)
            //                 {
            //                     float distance = MoveHelper.GetDistance(intervalMS, moveSpeedSEC);
            //                     //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
            //                     var d = moveTarget.BodyBlockSize + owner.BodyBlockSize + distance + moveTargetKeepRange;
            //                     if (Geometry.Vector3.DistanceSquared(owner.Position, moveTarget.Position) <= d * d)
            //                     {
            //                         return true;
            //                     }
            //                 }
            //                 // 落下 //
            //                 if (hasFly != null)
            //                 {
            //                     owner.PreJumpTo(startDirection, moveSpeedSEC, intervalMS);
            //                 }
            //                 else
            //                 {
            //                     if (IsNoneTouch)
            //                     {
            //                         owner.PreJumpTo(startDirection, moveSpeedSEC, intervalMS);
            //                     }
            //                     else
            //                     {
            //                         owner.PreMoveTo(startDirection, moveSpeedSEC, intervalMS);
            //                     }
            //                 }
            //                 // 后退 //
            //                 {
            //                     MoveHelper.UpdateSpeed(intervalMS, ref moveSpeedSEC, moveSpeedAdd, moveSpeedAcc);
            //                 }
            //                 if (hitMoveTime.Update(intervalMS))
            //                 {
            //                     Stop();
            //                 }
            //                 this.TotalDistance = Geometry.Vector3.Distance(startPos, owner.mLocalPos.Position);
            //                 return IsEnd;
            //             }



            private void move(float intervalMS, LayerObject target)
            {
                if (moveTargetBody)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, moveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (LayerZone.Intersects(owner.Position, target.Position, target.BodyBlockSize + owner.BodyBlockSize + distance + moveTargetKeepRange))
                    {
                        return;
                    }
                }
                if (hasFly != null)
                {
                    owner.PreJumpToTarget(target.X, target.Y, moveSpeedSEC, intervalMS);
                }
                else
                {
                    if (IsNoneTouch)
                    {
                        owner.PreJumpToTarget(target.X, target.Y, moveSpeedSEC, intervalMS);
                    }
                    else
                    {
                        owner.PreMoveToTarget(target.X, target.Y, moveSpeedSEC, intervalMS);
                    }
                }
            }
            private void move(float intervalMS, float direction)
            {
                if (hasFly != null)
                {
                    owner.PreJumpTo(startDirection, moveSpeedSEC, intervalMS);
                }
                else
                {
                    if (IsNoneTouch)
                    {
                        owner.PreJumpTo(startDirection, moveSpeedSEC, intervalMS);
                    }
                    else
                    {
                        owner.PreMoveTo(startDirection, moveSpeedSEC, intervalMS);
                    }
                }
            }



            public bool IsEnd { get; private set; }
            public float TotalTimeMS { get; private set; }
            public float TotalDistance { get; private set; }
            public bool IsNoneTouch { get { return isNoneTouch; } set { isNoneTouch = value; } }
            public float MoveSpeed { get { return moveSpeedSEC; } }
        }

        //-----------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------

        /// <summary>
        /// 落体运动
        /// </summary>
        public class FallingDown : LayerStatus
        {
            private LayerUnit unit;
            //private readonly float gravity;
            // private readonly float z_limit;
            // private float z_speed;
            protected FallingDown() { }
            public static FallingDown Alloc(LayerUnit unit)
            {
                var mHitFlyState = unit.ObjectPool.AllocOrCreateAutoRelease<FallingDown>(static s => new FallingDown());
                mHitFlyState.Init(unit);
                return mHitFlyState;
            }
            private void Init(LayerUnit unit)
            {
                this.unit = unit;
                this.isEnd = false;
            }
            internal void Start(float zspeed, float zgravity)
            {
                if (zgravity == 0)
                {
                    zgravity = unit.Gravity;
                }
                this.unit.mLocalPos.StartJump(zspeed, zgravity);
                this.isEnd = false;
            }
            protected override void Disposing()
            {
                unit = null;
                isEnd = true;
            }

            internal bool Update(float intervalMS)
            {
                //var pos = unit.Position;
                //向上受到ZLimit限制//
                //                 if (unit.Parent.ActorSyncMode != SyncMode.ForceByServer)
                //                 {
                //                     //                     unit.mLocalPos.Z += MoveHelper.GetDistance(intervalMS, z_speed);
                //                     //                     if (!unit.Parent.Terrain3D.IsInAir(ref pos))
                //                     //                     {
                //                     //                         unit.mLocalPos.Z = pos.Z;
                //                     //                         this.IsEnd = true;
                //                     //                     }
                //                     //                     this.z_speed -= MoveHelper.GetDistance(intervalMS, gravity);
                //                     //                     //骤停//
                //                     //                     if (z_speed > 0 && unit.Z > z_limit)
                //                     //                     {
                //                     //                         z_speed = 0;
                //                     //                     }
                //                     //                 }
                //                     //                 else
                //                     //                 {
                //                     //                     if (!unit.Parent.Terrain3D.IsInAir(ref pos))
                //                     //                     {
                //                     //                         unit.mLocalPos.Z = pos.Z;
                //                     //                         this.IsEnd = true;
                //                     //                     }
                //                 }
                if (this.isEnd)
                {
                    return true;
                }
                if (!unit.IsInAir)
                {
                    End();
                }
                return isEnd;
            }

            internal void End()
            {
                unit.ResetGravity();
                isEnd = true;
            }
            private bool isEnd;
            public bool IsEnd { get => isEnd; }
            //             public float ZSpeedSEC { get { return z_speed; } }
            //             public float ZLimit { get { return z_limit; } }
        }

        private FallingDown mHitFlyState;

        protected virtual void DoJump(UnitJumpEvent e)
        {
            StartFly(e.ZSpeed, e.Gravity);
        }


        public bool IsFallingDown
        {
            get => mHitFlyState != null && !mHitFlyState.IsEnd;
        }


        protected FallingDown StartFly(float z_speed, float z_gravity = 0)
        {
            if (z_speed != 0)
            {
                if (mHitFlyState == null)
                {
                    mHitFlyState = FallingDown.Alloc(this);
                }
                mHitFlyState.Start(z_speed, z_gravity);
                return mHitFlyState;
            }
            return null;
        }

        protected virtual void UpdateMotion(float intervalMS)
        {
            if (mHitFlyState != null && mHitFlyState.Update(intervalMS))
            {
                mHitFlyState.End();
                mHitFlyState.Dispose();
                mHitFlyState = null;
            }
            //客户端预先技能动作//
            if (mSkillMove != null)
            {
                if (mSkillMove.Update(intervalMS))
                {
                    ClearSkillMove();
                }
            }
        }


        protected void ResetGravity()
        {
            this.Gravity = mRemoteGravity;
        }


    }
}
