using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Linq;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceUnit
    {
        private FallingDown mCurrentFallingDown;
        private HitMoveSpeed mCurrentStartMove;
        private MoveAI currentMoveAI;

        //---------------------------------------------------------------------------------------------------------

        public float ZSpeedSEC
        {
            get => mPos.SpeedZ;
            set { mPos.SpeedZ = value; }
        }

        public bool IsDamageFallingDown
        {
            get
            {
                StateDamage damage = (this.CurrentState as StateDamage);
                if (damage != null)
                {
                    return damage.IsFallingDown;
                }
                return false;
            }
        }
        public bool IsPhysicalMove
        {
            get
            {
                if (mCurrentStartMove != null)
                    return !mCurrentStartMove.IsEnd;
                return false;
            }
        }
        public MoveAI CurrentMoveAI { get => currentMoveAI; }
        public FallingDown CurrentFallingDown => mCurrentFallingDown;
        public UnitActionStatus GetStartMoveStatus() => ActionDefine.Instance.GetStartMoveStatus(this.Info, this.AMotion, this.MoveSpeedSEC);
        public UnitActionStatus GetStopMoveStatus() => UnitActionStatus.Idle;
        public float LayerUpward { get => mPrevLayerUpward; }
        public override Vector3 GetRandomPos()
        {
            var random = this.RandomN;
            float r = (float)(random.NextFloat() * this.BodySize);
            float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
            float x = (float)(this.X + Math.Cos(a) * r);
            float y = (float)(this.Y + Math.Sin(a) * r);
            return new Vector3(x, y, this.Z);
        }
        //-------------------------------------------------------------------------------------------------------------------------
        protected override void InternalFaceTo(float d)
        {
            if (d != Direction)
            {
                if (AMotion && AMotion.IsTurnable && !IsDockingSolidFace)
                {
                    base.InternalFaceTo(d);
                }
            }
        }
        protected override void InternalBodyFaceTo(float d)
        {
            if (d != BodyDirection)
            {
                if (AMotion && AMotion.IsTurnable && !IsDockingSolidFace)
                {
                    base.InternalBodyFaceTo(d);
                }
            }
        }
        protected override void updatePosBegin()
        {
            UpdateDocking();
            updateBodyDirection();
        }
        protected virtual void updateBodyDirection()
        {
            if (AMotion && AMotion.BodyTurnSpeedSEC != 0)
            {
                var prvPos = this.PrevFramePos;
                var curPos = this.Position;
                if (prvPos != curPos)
                {
                    var forward = curPos - prvPos;
                    var tgtlookAt = VectorHelper.GetDegree(forward);
                    var turnSpeed = AMotion.BodyTurnSpeedSEC;
                    if (turnSpeed > 0)
                    {
                        var lookAt = MoveHelper.DirectionChange(
                           BodyDirection,
                           tgtlookAt,
                           turnSpeed,
                           Zone.UpdateIntervalMS);
                        this.InternalBodyFaceTo(lookAt);
                    }
                    else
                    {
                        this.InternalBodyFaceTo(tgtlookAt);
                    }
                }
            }
            else
            {
                this.InternalBodyFaceTo(this.Direction);
            }
        }
        protected override void updatePosEnd(UnitSyncPos cache)
        {
            cache.UnitMainState = mCurrentActionMainState;
            cache.UnitSubState = mCurrentActionSubState;
            this.mPrevActionStatus = mCurrentActionMainState;
            this.mPrevActionSubstate = mCurrentActionSubState;
            var layer = this.mPos.CurrentLayer;
            if (layer != null && mPrevLayerUpward != layer.Upward)
            {
                this.mPrevLayerUpward = cache.LayerUpward = layer.Upward;
            }
            else
            {
                this.mPrevLayerUpward = 0;
            }
        }

        protected virtual void updatePhysical()
        {
            mPos.Update(Parent.UpdateIntervalMS);
            if (mCurrentFallingDown != null)
            {
                if (mCurrentFallingDown.Update(Parent.UpdateIntervalMS))
                {
                    mCurrentFallingDown.Dispose();
                    mCurrentFallingDown = null;
                }
            }
            if (mCurrentStartMove != null)
            {
                if (mCurrentStartMove.Update(Parent.UpdateIntervalMS))
                {
                    mCurrentStartMove.Dispose();
                    mCurrentStartMove = null;
                }
            }

        }
        protected virtual void cleanPhysical()
        {
            //this.currentMoveAI?.Dispose();
            this.mCurrentFallingDown?.Dispose();
            this.mCurrentStartMove?.Dispose();
        }

        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 在移动时被某个单位阻挡
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void onMoveBlockWithObject(IEntityObject obj)
        {
            cb_onMoveBlockWithObject(obj);
        }
        /// <summary>
        /// 阻挡到其他单位移动
        /// </summary>
        /// <param name="obj"></param>
        /// <returns >通知其他单位自己可以让开</returns>
        protected virtual bool onBlockOtherGetaway(InstanceUnit obj)
        {
            var ret = cb_OnBlockOtherGetaway(obj);
            return ret;
        }

        //---------------------------------------------------------------------------------------------------------
        public virtual MoveAI CreateMoveAI(bool overrideActionStatus = true, float holdTimeMS = 0)
        {
            currentMoveAI = MoveAI.Alloc(this, overrideActionStatus, holdTimeMS);
            if (AMotion)
            {
                currentMoveAI.IsMoveImpact = this.AMotion.IsMoveImpact;
            }
            return currentMoveAI;
        }

        public virtual bool ControlSetPos(in Vector3 pos)
        {
            if (IsLock) return false;
            return base.SetPos(pos, this.IntersectMap);
        }

        //---------------------------------------------------------------------------------------------------------
        #region MoveBlock
        public MoveBlockResult MoveBlockTo(ref ITerrainWayPoint path, float speedSEC, float intervalMS, bool land = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            var p = path.Position;
            if (mPos.X == p.X && mPos.Y == p.Y)
            {
                MoveBlockResult result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                path = path.Next;
                return result;
            }
            else
            {
                Geometry.Vector2 oldp = mPos.Position;
                var step = MoveHelper.GetDistance(intervalMS, speedSEC);
                var result = MoveBlockTo(p.X, p.Y, step, land);
                if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                {
                    var mlen = Geometry.Vector2.Distance(oldp, mPos.Position);
                    if (mlen < step)
                    {
                        path = path.Next;
                        if (path != null)
                        {
                            step = step - mlen;
                            p = path.Position;
                            result = MoveBlockTo(p.X, p.Y, step, land);
                        }
                        else
                        {

                        }
                    }
                }
                if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                {
                    path = null;
                }
                else if ((result.result & MoveResult.MOVE_RESULT_NO_WAY) != 0)
                {
                    path = null;
                }
                else if ((result.result & MoveResult.MOVE_RESULT_RESET_PATH) != 0)
                {
                    path = null;
                }
                return result;
            }
        }
        public MoveBlockResult MoveBlockTo(float x, float y, float speedSEC, float intervalMS, bool land = true)
        {
            return MoveBlockTo(x, y, MoveHelper.GetDistance(intervalMS, speedSEC), land);
        }
        public MoveBlockResult MoveBlockTo(float x, float y, float step, bool land = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            if (mPos.X == x && mPos.Y == y)
            {
                MoveBlockResult result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                return result;
            }
            else
            {
                var ddx = x - mPos.X;
                var ddy = y - mPos.Y;
                var dlen = CMath.GetDistance(ddx, ddy);
                if (dlen <= step)
                {
                    MoveBlockResult result = MoveBlock(ddx, ddy, land);
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
                    var distance = Math.Min(step, dlen);
                    float oldx = mPos.X;
                    float oldy = mPos.Y;
                    float direction = CMath.GetDegree(ddx, ddy);
                    float dx = (float)(Math.Cos(direction) * distance);
                    float dy = (float)(Math.Sin(direction) * distance);
                    MoveBlockResult result = MoveBlock(dx, dy, land);
                    ddx = Math.Abs(mPos.X - oldx);
                    ddy = Math.Abs(mPos.Y - oldy);
                    float minstep = Parent.MinStep;
                    if (ddx < minstep && ddy < minstep)
                    {
                        result.result |= MoveResult.MOVE_RESULT_MIN_STEP;
                    }
                    return result;
                }
            }
        }
        public MoveBlockResult MoveBlockToAngle(float angle, float speedSEC, float intervalMS, bool land = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            float dd = MoveHelper.GetDistance(intervalMS, speedSEC);
            float dx = (float)(Math.Cos(angle) * dd);
            float dy = (float)(Math.Sin(angle) * dd);
            return MoveBlock(dx, dy, land);
        }
        /// <summary>
        /// 尝试碰撞移动偏移量
        /// </summary>
        /// <param name="dx">offset X</param>
        /// <param name="dy">offset Y</param>
        /// <returns></returns>
        public MoveBlockResult MoveBlock(float dx, float dy, bool land = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            var oldv = mPos.CurrentLayer;
            var oldp = mPos.Position;
            MoveBlockResult result = new MoveBlockResult(0);
            if (IntersectMap)
            {
                //尝试地图碰撞移动//
                switch (mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), land))
                {
                    case AgentMoveResult.Blocked:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        return result;
                    case AgentMoveResult.MoveTouchX:
                    case AgentMoveResult.MoveTouchY:
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case AgentMoveResult.MoveArrived:
                    case AgentMoveResult.MoveSmooth:
                    case AgentMoveResult.MoveCross:
                        break;
                }
                //和建筑碰撞//
                var b = Parent.IntersectNearStaticBlockable(this);
                if (b != null)
                {
                    result.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                    result.touched = b;
                    if (Parent.TestMapCross(this, mPos.Position))
                    {
                        return result;
                    }
                    mPos.Transport(oldp, oldv);
                    if (Parent.TestMapCross(this, mPos.Position))
                    {
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ | MoveResult.MOVE_RESULT_RESET_PATH;
                        return result;
                    }
                    if (b is InstanceUnit u) onMoveBlockWithObject(u);
                    if (this.TryMoveToObjectBorder(b, dx, dy))
                    {
                        var b2 = Parent.IntersectNearStaticBlockable(this);
                        if (b2 != null && b2 != b)
                        {
                            mPos.Transport(oldp, oldv);
                            onMoveBlockWithObject(b2);
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            return result;
                        }
                        if (!Parent.TestMapCross(this, mPos.Position))
                        {
                            mPos.Transport(oldp, oldv);
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                            return result;
                        }
                        var dp = Vector3.DistanceSquared(oldp, mPos.Position);
                        if (dp <= Parent.MinStepSquare)
                        {
                            result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ | MoveResult.MOVE_RESULT_RESET_PATH;
                            return result;
                        }
                    }
                    else
                    {
                        result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ | MoveResult.MOVE_RESULT_RESET_PATH;
                        return result;
                    }
                }
            }
            else
            {
                mPos.MoveOffsetNoTouch(new Geometry.Vector2(dx, dy));
            }
            if (this.IntersectObj)
            {
                //和单位碰撞//
                var bu = Parent.IntersectNearUnit(this, this.IntersectMap);
                if (bu != null && bu.AoiStatus == AoiStatus)
                {
                    result.touched = bu;
                    bool moved = false;
                    mPos.Transport(oldp, oldv);
                    if (TryMoveToObjectBorder(bu, dx, dy))
                    {
                        var bu2 = Parent.IntersectNearUnit(this, false);
                        if (bu2 != null && bu2 != bu && bu.AoiStatus == bu2.AoiStatus)
                        {
                            mPos.Transport(oldp, oldv);
                            bu = bu2;
                            result.touched = bu;
                        }
                        else
                        {
                            moved = true;
                        }
                    }
                    if ((bu is InstanceUnit blockUnit) && blockUnit.onBlockOtherGetaway(this))
                    {
                        result.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY;
                    }
                    result.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                    if (Parent.TouchObject2(this, bu))
                    {
                        //恢复到原来位置也碰撞的话，就弹开//
                        if (ElasticOtherObject(bu) == false)
                        {
                            if (!moved)
                            {
                                mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), land);
                            }
                        }
                    }
                    onMoveBlockWithObject(bu);
                }
            }
            return result;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region MoveImpact
        public MoveBlockResult MoveImpactTo(ref ITerrainWayPoint path, float speedSEC, float intervalMS, bool land = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            var p = path.Position;
            if (mPos.X == p.X && mPos.Y == p.Y)
            {
                var result = new MoveBlockResult(MoveResult.MOVE_RESULT_ARRIVED);
                path = path.Next;
                return result;
            }
            else
            {
                Geometry.Vector2 oldp = mPos.Position;
                var step = MoveHelper.GetDistance(intervalMS, speedSEC);
                var result = MoveImpactTo(p.X, p.Y, step, land);
                if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                {
                    var mlen = Geometry.Vector2.Distance(oldp, mPos.Position);
                    if (mlen <= step)
                    {
                        path = path.Next;
                        if (path != null)
                        {
                            step = step - mlen;
                            p = path.Position;
                            result = MoveImpactTo(p.X, p.Y, step, land);
                        }
                    }
                }
                if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                {
                    path = null;
                }
                else if ((result.result & MoveResult.MOVE_RESULT_NO_WAY) != 0)
                {
                    path = null;
                }
                return result;
            }
        }


        /// <summary>
        /// 挤开对方的移动方式
        /// </summary>
        /// <param name="x">target</param>
        /// <param name="y">target</param>
        /// <param name="speedSEC"></param>
        /// <param name="intervalMS"></param>
        /// <returns>TRUE=无法移动</returns>
        public MoveBlockResult MoveImpactTo(float x, float y, float speedSEC, float intervalMS, bool land)
        {
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            return MoveImpactTo(x, y, distance, land);
        }
        public MoveBlockResult MoveImpactTo(float x, float y, float distance, bool land)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            var minstep = Parent.MinStep;
            var ddx = x - mPos.X;
            var ddy = y - mPos.Y;
            var dlen = CMath.GetDistance(ddx, ddy);
            var angle = MathVector.getDegree(ddx, ddy);
            if (dlen <= distance)
            {
                var result = MoveImpactInner(angle, dlen, 0, land);
                if (ddx < minstep && ddy < minstep)
                {
                    result.result |= MoveResult.MOVE_RESULT_MIN_STEP;
                }
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
                var result = MoveImpactInner(angle, distance, 0, land);
                if (ddx < minstep && ddy < minstep)
                {
                    result.result |= MoveResult.MOVE_RESULT_MIN_STEP;
                }
                return result;
            }
        }


        /// <summary>
        /// 挤开对方的移动方式
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="speedSEC"></param>
        /// <param name="intervalMS"></param>
        /// <returns>TRUE=无法移动</returns>
        public MoveBlockResult MoveImpact(float angle, float speedSEC, float intervalMS, bool land)
        {
            float minstep = Parent.MinStep;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            if (Math.Abs(distance) < minstep)
            {
                return new MoveBlockResult(MoveResult.MOVE_RESULT_MIN_STEP);
            }
            return MoveImpactInner(angle, distance, 0, land);
        }
        /// <summary>
        /// 挤开对方的移动方式
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public MoveBlockResult MoveImpactDistance(float angle, float distance, bool land)
        {
            float minstep = Parent.MinStep;
            if (Math.Abs(distance) < minstep)
            {
                return new MoveBlockResult(MoveResult.MOVE_RESULT_MIN_STEP);
            }
            return MoveImpactInner(angle, distance, 0, land);
        }
        class MoveImpactInput : ForEachInput<InstanceZoneEntity>
        {
            public InstanceUnit owner;
            public MoveBlockResult ret;
            public int depth;
            public int max_depth;
            public bool land;
            public ITerrainLayer oldv;
            public Geometry.Vector3 oldp;
        }
        protected MoveBlockResult MoveImpactInner(float angle, float distance, int depth, bool land)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            var ret = new MoveBlockResult(0);
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            var oldv = mPos.CurrentLayer;
            var oldp = mPos.Position;
            if (IntersectMap)
            {
                //尝试地图碰撞移动//
                switch (mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), land))
                {
                    case AgentMoveResult.Blocked:
                        ret.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        ret.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        return ret;
                    case AgentMoveResult.MoveTouchX:
                    case AgentMoveResult.MoveTouchY:
                        ret.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case AgentMoveResult.MoveArrived:
                    case AgentMoveResult.MoveSmooth:
                    case AgentMoveResult.MoveCross:
                        break;
                }
                var bu = Parent.IntersectNearStaticBlockable(this);
                if (bu != null)
                {
                    ret.touched = bu;
                    ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                    if (Parent.PathFinder.GetMapBlockByPos(mPos.Position, out var mapnode) && !Parent.PathFinder.IsMapNodeBlock(mapnode))
                    {
                        return ret;
                    }
                    mPos.Transport(oldp, oldv);
                    if (TryMoveToObjectBorder(bu, dx, dy))
                    {
                        var b2 = Parent.IntersectNearStaticBlockable(this);
                        if (b2 != null && b2 != bu)
                        {
                            mPos.Transport(oldp, oldv);
                            onMoveBlockWithObject(b2);
                            ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ | MoveResult.MOVE_RESULT_RESET_PATH;
                            return ret;
                        }
                        if (!Parent.TestMapCross(this, mPos.Position))
                        {
                            mPos.Transport(oldp, oldv);
                            ret.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                            return ret;
                        }
                    }
                    else
                    {
                        onMoveBlockWithObject(bu);
                        ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ | MoveResult.MOVE_RESULT_RESET_PATH;
                        return ret;
                    }
                }
            }
            else
            {
                mPos.MoveOffsetNoTouch(new Geometry.Vector2(dx, dy));
            }
            if (this.IntersectObj)
            {
                int max_depth = (int)CFG.GLOBAL_MOVE_IMPACT_DEPTH;
                using (var it = ObjectPool.AllocAutoRelease<MoveImpactInput>())
                {
                    it.depth = depth;
                    it.max_depth = max_depth;
                    it.owner = this;
                    it.ret = ret;
                    it.land = land;
                    it.oldv = oldv;
                    it.oldp = oldp;
                    Parent.ForEachNearObjects(mPos.X, mPos.Y, it, static (st) =>
                    {
                        if (st.Iterator is InstanceUnit o)
                        {
                            if ((o != st.owner) && o.IntersectObj && st.owner.Parent.TouchObject2(st.owner, o))
                            {
                                st.ret.touched = o;
                                st.ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                                if ((o is InstanceUnit blockUnit) && blockUnit.onBlockOtherGetaway(st.owner))
                                {
                                    //ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY;
                                }
                                if ((!o.IsStaticBlock) && (o.Weight <= st.owner.Weight))
                                {
                                    if (st.depth < st.max_depth)
                                    {
                                        float targetAngle = MathVector.getDegree(st.owner.X, st.owner.Y, o.X, o.Y);
                                        float ddr = MathVector.getDistance(o.mPos.X, o.mPos.Y, st.owner.mPos.X, st.owner.mPos.Y);
                                        float bdr = (st.owner.BodyBlockSize + o.BodyBlockSize);
                                        o.MoveImpactInner(targetAngle, (bdr - ddr), st.depth + 1, st.land);
                                    }
                                    else
                                    {
                                        st.Break = true;
                                        st.owner.mPos.Transport(st.oldp, st.oldv);
                                        st.owner.onMoveBlockWithObject(o);
                                        st.ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                                    }
                                }
                                else
                                {
                                    st.Break = true;
                                    st.owner.mPos.Transport(st.oldp, st.oldv);
                                    st.owner.onMoveBlockWithObject(o);
                                    st.ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                                    if (st.owner.Parent.TouchObject2(st.owner, o))
                                    {
                                        float targetAngle = MathVector.getDegree(o.X, o.Y, st.owner.X, st.owner.Y);
                                        float ddr = MathVector.getDistance(o.mPos.X, o.mPos.Y, st.owner.mPos.X, st.owner.mPos.Y);
                                        float bdr = (st.owner.BodyBlockSize + o.BodyBlockSize);
                                        //强制将自己移动到某处//
                                        st.owner.MoveLinearMap((bdr - ddr), targetAngle);
                                    }
                                }
                            }
                        }
                    });
                }
#if FALSE
                bool touched = Parent.ForEachNearObjects(mPos.X, mPos.Y, (InstanceUnit o, ref bool cancel) =>
                {
                    if ((o != this) && o.IntersectObj && Parent.TouchObject2(this, o))
                    {
                        ret.touched = o;
                        ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                        if ((o is InstanceUnit blockUnit) && blockUnit.onGetaway(this))
                        {
                            //ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY;
                        }
                        if ((!o.IsStaticBlock) && (o.Weight <= this.Weight))
                        {
                            if (depth < max_depth)
                            {
                                float targetAngle = MathVector.getDegree(this.X, this.Y, o.X, o.Y);
                                float ddr = MathVector.getDistance(o.mPos.X, o.mPos.Y, this.mPos.X, this.mPos.Y);
                                float bdr = (this.BodyBlockSize + o.BodyBlockSize);
                                o.MoveImpactInner(targetAngle, (bdr - ddr), depth + 1, land);
                            }
                            else
                            {
                                mPos.Transport(oldp, oldv);
                                onMoveBlockWithObject(o);
                                cancel = true;
                                ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            }
                        }
                        else
                        {
                            mPos.Transport(oldp, oldv);
                            onMoveBlockWithObject(o);
                            cancel = true;
                            ret.result |= MoveResult.MOVE_RESULT_BLOCK_OBJ;
                            if (Parent.TouchObject2(this, o))
                            {
                                float targetAngle = MathVector.getDegree(o.X, o.Y, this.X, this.Y);
                                float ddr = MathVector.getDistance(o.mPos.X, o.mPos.Y, this.mPos.X, this.mPos.Y);
                                float bdr = (this.BodyBlockSize + o.BodyBlockSize);
                                //强制将自己移动到某处//
                                this.MoveLinearMap((bdr - ddr), targetAngle);
                            }
                        }
                    }
                });
#endif
            }
            return ret;
        }
        #endregion

        /// <summary>
        /// 越过单位跳跃
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="speedSEC"></param>
        /// <param name="intervalMS"></param>
        /// <returns>是否到达终点</returns>
        public bool MoveAirTo(float direction, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                return false;
            }
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            if (IntersectMap)
            {
                var oldv = mPos.CurrentLayer;
                var oldp = mPos.Position;
                switch (mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), false))
                {
                    case AgentMoveResult.Blocked:
                        return true;
                }
                if (Parent.IntersectNearStaticBlockable(this) != null)
                {
                    mPos.Transport(oldp, oldv);
                    return true;
                }
            }
            else
            {
                mPos.MoveOffsetNoTouch(new Geometry.Vector2(dx, dy));
            }
            return false;
        }
        public bool MoveAirToTarget(float x, float y, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                return false;
            }
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float ddx = x - mPos.X;
            float ddy = y - mPos.Y;
            float direction = MathVector.getDegree(ddx, ddy);
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            bool minstep = false;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                dx = ddx;
                dy = ddy;
                minstep = true;
            }
            if (IntersectMap)
            {
                var oldv = mPos.CurrentLayer;
                var oldp = mPos.Position;
                switch (mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), false))
                {
                    case AgentMoveResult.Blocked:
                        if (IntersectObj) if (ElasticOtherObjects()) { SendForceSync(); }
                        return true;
                }
                if (Parent.IntersectNearStaticBlockable(this) != null)
                {
                    mPos.Transport(oldp, oldv);
                    if (IntersectObj) if (ElasticOtherObjects()) { SendForceSync(); }
                    return true;
                }

            }
            else
            {
                mPos.MoveOffsetNoTouch(new Geometry.Vector2(dx, dy));
            }
            return minstep;
        }

        //-------------------------------------------------------------------------------------------------------//

        /// <summary>
        /// 线性移动，只和地图碰撞
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="angle"></param>
        /// <returns>和地图碰撞</returns>
        public bool MoveLinearMap(float distance, float angle)
        {
            if (IsLock)
            {
                return false;
            }
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            return mPos.MoveLinearTo2D(new Geometry.Vector2(X + dx, Y + dy), out var touched) != AgentMoveResult.Blocked;
            //             if (Parent.RaycastMap2D(this, new Geometry.Vector2(dx, dy), out var touched, out var tdistance))
            //             {
            //                 MathVector.movePolar(ref touched.X, ref touched.Y, angle, -Parent.MinStep);
            //                 //                 mPos.x = touched.X;
            //                 //                 mPos.y = touched.Y;
            //                 mPos.Transport(touched);
            //                 return true;
            //             }
            //             mPos.MoveNoTouch(new Geometry.Vector2(dx, dy));
            //             mPos.x += dx;
            //             mPos.y += dy;
            //return false;
        }

        /// <summary>
        /// 线性移动，不会穿
        /// </summary>
        /// <returns></returns>
        public MoveBlockResult MoveLinear(float distance, float angle, bool ignore_map = false, bool ignore_obj = true)
        {
            if (IsLock)
            {
                return new MoveBlockResult() { result = MoveResult.MOVE_RESULT_ARRIVED };
            }
            MoveBlockResult ret = new MoveBlockResult();
            //             if (mPos.Z != z)
            //             {
            //                 mPos.FlyTo(z);
            //             }
            var srcP = mPos.Position;
            //if (!ignore_map)
            {
                float dst_x = mPos.X + (float)(Math.Cos(angle) * distance);
                float dst_y = mPos.Y + (float)(Math.Sin(angle) * distance);
                var oldLayer = CurrentLayer;
                switch (mPos.MoveLinearTo2D(new Geometry.Vector2(dst_x, dst_y), out var touched))
                {
                    case AgentMoveResult.MoveArrived:
                        ret.result |= MoveResult.MOVE_RESULT_ARRIVED;
                        break;
                    case AgentMoveResult.MoveCross:
                        ret.result |= MoveResult.MOVE_RESULT_TOUCH_MAP_ALL;
                        break;
                    case AgentMoveResult.Blocked:
                        ret.result |= MoveResult.MOVE_RESULT_BLOCK_MAP;
                        return ret;
                    default:
                        ret.result |= MoveResult.MOVE_SMOOTH;
                        break;
                }
            }
            using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
            {
                float dst_x = mPos.X;
                float dst_y = mPos.Y;
                var stripe = Geometry.VoxelStripe.InitFromPoint(srcP, new Geometry.Vector2(dst_x, dst_y), BodyBlockSize, BodyHeight);
                if (!ignore_obj)
                {
                    Parent.GetObjectsInStripe(this, static (InstanceUnit u, InstanceZoneObject o, in Geometry.VoxelStripe st) =>
                    {
                        var uo = o as InstanceZoneEntity;
                        if (uo.IntersectObj)
                        {
                            return Collider.Stripe_Touch_BlockBody(u, o, in st);
                        }
                        return false;
                    }, stripe, list);
                }
                else if (!ignore_map)
                {
                    Parent.GetObjectsInStripe(this, static (InstanceUnit u, InstanceZoneObject o, in Geometry.VoxelStripe st) =>
                    {
                        var uo = o as InstanceZoneEntity;
                        if (uo.IsStaticBlock && uo.IntersectObj)
                        {
                            return Collider.Stripe_Touch_BlockBody(u, o, in st);
                        }
                        return false;
                    }, stripe, list);
                }
                if (list.Count > 0)
                {
                    list.Sort(new ObjectBodySorterNearest<InstanceZoneEntity>(srcP, this.BodyBlockSize));
                    ret.result |= MoveResult.MOVE_RESULT_TOUCH_OBJ;
                    ret.result &= (~MoveResult.MOVE_RESULT_ARRIVED);
                    ret.touched = list[0];

                    distance = MathVector.getDistance(srcP.X, srcP.Y, ret.touched.X, ret.touched.Y) - this.BodyBlockSize - ret.touched.BodySize;
                    distance = Math.Max(distance, 0);
                    var dx = (float)(Math.Cos(angle) * distance);
                    var dy = (float)(Math.Sin(angle) * distance);
                    mPos.Transport(srcP);
                    mPos.TryMoveOffset(new Geometry.Vector2(dx, dy), false);
                }
            }
            return ret;
        }


        public bool MoveBlink(BlinkMove blink, Geometry.Vector3? targetPos, InstanceUnit targetUnit = null)
        {
            if (IsLock) return false;
            switch (blink.MType)
            {
                case BlinkMove.BlinkMoveType.MoveToForward:
                    MoveLinear(blink.Distance,
                            this.Direction + blink.DirectionOffset,
                            blink.NoneTouchMap || !IntersectMap,
                            blink.NoneTouchObj || !IntersectObj);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToBackward:
                    MoveLinear(-blink.Distance,
                            this.Direction + blink.DirectionOffset,
                            blink.NoneTouchMap || !IntersectMap,
                            blink.NoneTouchObj || !IntersectObj);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToTargetPos:
                    if (!targetPos.IsNullOrNaN())
                    {
                        var tp = targetPos.Value;
                        //if (CMath.includeRoundPoint(X, Y, blink.Distance, targetUnit.X, targetUnit.Y))
                        if (Geometry.Vector3.DistanceSquared(tp, mPos.Position) < blink.Distance * blink.Distance)
                        {
                            float angle = MathVector.getDegree(this.X, this.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(this.X, this.Y, tp.X, tp.Y), blink.Distance);
                            MoveLinear(distance,
                                angle + blink.DirectionOffset,
                                blink.NoneTouchMap || !IntersectMap,
                                blink.NoneTouchObj || !IntersectObj);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitFace:
                    if (targetUnit != null)
                    {
                        var tp = targetUnit.Position;
                        // if (CMath.includeRoundPoint(X, Y, blink.Distance, targetUnit.X, targetUnit.Y))
                        //if (CMath.includeRoundRound(X, Y, blink.Distance, targetUnit.X, targetUnit.Y, targetUnit.BodyHitSize))
                        var r = blink.Distance + targetUnit.BodyHitSize;
                        if (Geometry.Vector3.DistanceSquared(targetUnit.Position, mPos.Position) < r * r)
                        {
                            float angle = MathVector.getDegree(this.X, this.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(this.X, this.Y, tp.X, tp.Y), blink.Distance) - (targetUnit.BodyBlockSize + this.BodyBlockSize);
                            MoveLinear(distance,
                                angle + blink.DirectionOffset,
                                blink.NoneTouchMap || !IntersectMap,
                                blink.NoneTouchObj || !IntersectObj);
                            this.FaceTo(targetUnit.Position);
                            //                             mPos.x = targetUnit.X;
                            //                             mPos.y = targetUnit.Y;
                            //                             mPos.Transport(targetUnit.Position);
                            //                             this.FaceTo(targetUnit.Direction + CMath.PI_F + blink.DirectionOffset);
                            //                             MoveLinear(targetUnit.BodyBlockSize + this.BodyBlockSize,
                            //                                 targetUnit.Direction,
                            //                                 blink.NoneTouchMap || !IntersectMap,
                            //                                 blink.NoneTouchObj || !IntersectObj);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitBack:
                    if (targetUnit != null)
                    {
                        var tp = targetUnit.Position;
                        // if (CMath.includeRoundPoint(X, Y, blink.Distance, targetUnit.X, targetUnit.Y))
                        //if (CMath.includeRoundRound(X, Y, blink.Distance, targetUnit.X, targetUnit.Y, targetUnit.BodyHitSize))
                        var r = blink.Distance + targetUnit.BodyHitSize;
                        if (Geometry.Vector3.DistanceSquared(targetUnit.Position, mPos.Position) < r * r)
                        {
                            float angle = MathVector.getDegree(this.X, this.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(this.X, this.Y, tp.X, tp.Y), blink.Distance) + (targetUnit.BodyBlockSize + this.BodyBlockSize);
                            MoveLinear(distance,
                                angle + blink.DirectionOffset,
                                blink.NoneTouchMap || !IntersectMap,
                                blink.NoneTouchObj || !IntersectObj);
                            this.FaceTo(targetUnit.Position);
                            //                             mPos.Transport(targetUnit.Position);
                            //                             this.FaceTo(targetUnit.Direction + blink.DirectionOffset);
                            //                             MoveLinear(
                            //                                 -(targetUnit.BodyBlockSize + this.BodyBlockSize),
                            //                                 targetUnit.Direction,
                            //                                 blink.NoneTouchMap || !IntersectMap,
                            //                                 blink.NoneTouchObj || !IntersectObj);
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public bool Move3DNoneTouch(InstanceZonePosition target, float speedSEC, float intervalMS)
        {
            if (IsLock)
            {
                return false;
            }
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            var pos = mPos.Position;
            var src = pos;
            var ret = DeepCore.Geometry.VectorHelper.MoveTo3D(ref pos, target.Position, distance);
            mPos.Transport(pos);
            if (ret == false)
            {
                //如果没有移动到目标位置，说明目标位置和当前高度不一致
                var tgt = mPos.Position;
                if (tgt.Z != src.Z)
                {
                    return target.VoxelBody.Intersects(new VoxelCylinder(tgt, 0.01f, this.BodyHeight));
                }
            }
            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <returns>还剩多少没走完</returns>
        public float MoveLerpTo(Vector3 target, float distance)
        {
            if (IsLock)
            {
                return 0;
            }
            var current = mPos.Position;
            var ret = DeepCore.Geometry.VectorHelper.MoveLerpTo(ref current, target, distance);
            mPos.Transport(current);
            return ret;
        }

        /// <summary>
        /// 尝试从目标身边挤过去（沿着边界移动）
        /// </summary>
        public bool TryMoveToObjectBorder(IEntityObject touched, float addX, float addY)
        {
            if (IsLock)
            {
                return false;
            }
            ITerrainLayer touchLayer = null;
            var zone = this.Parent;
            if (touched.ZoneShape != null)
            {
                var pos = new Geometry.Vector2();
                pos.X = mPos.X;
                pos.Y = mPos.Y;
                if (touched.ZoneShape.MoveToBorder(ref pos, addX, addY, zone.MinStep))
                {
                    var tpos = new Geometry.Vector3(pos.X, pos.Y, mPos.Z);
                    if (!this.IntersectMap || zone.Terrain3D.TryMoveTo(ref tpos, out touchLayer))
                    {
                        if (touchLayer != null)
                        {
                            mPos.Transport(tpos, touchLayer);
                        }
                        return true;
                    }
                }
            }
            else if (touched is InstanceUnit)
            {
                var objT = touched as InstanceUnit;
                var dirD = MathVector.getDegree(addX, addY);
                var dirS = MathVector.getDegree(mPos.X, mPos.Y, touched.X, touched.Y);
                if (CMath.RadiansInRange(dirS, dirD - zone.ElasticAngle, zone.ElasticAngle * 2f) == false)
                {
                    var dir = MathVector.getDegree(touched.X, touched.Y, mPos.X + addX, mPos.Y + addY);
                    var pos = new Geometry.Vector3(touched.X, touched.Y, mPos.Z);
                    Geometry.VectorHelper.MovePolar(ref pos, dir, this.BodyBlockSize + objT.BodyBlockSize + zone.MinStep);
                    if (!this.IntersectMap || zone.Terrain3D.TryMoveTo(ref pos, out touchLayer))
                    {
                        if (touchLayer != null)
                        {
                            mPos.Transport(pos, touchLayer);
                        }
                        return true;
                    }
                }
            }
            else if (touched is InstanceFlag)
            {
                var objT = touched as InstanceFlag;
                if (objT.ZoneShape != null)
                {
                    var pos = new Geometry.Vector2();
                    pos.X = mPos.X;
                    pos.Y = mPos.Y;
                    if (objT.ZoneShape.MoveToBorder(ref pos, addX, addY, zone.MinStep))
                    {
                        var tpos = new Geometry.Vector3(pos.X, pos.Y, mPos.Z);
                        if (!this.IntersectMap || zone.Terrain3D.TryMoveTo(ref tpos, out touchLayer))
                        {
                            if (touchLayer != null)
                            {
                                mPos.Transport(tpos, touchLayer);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 挤开其他和自己重叠的单位，或者被Weight大于自己的人挤开
        /// </summary>
        /// <returns></returns>
        public bool ElasticOtherObjects(bool force = false)
        {
            if (force || this.IntersectObj)
            {
                using (var it = ObjectPool.AllocForEach3<InstanceZoneEntity, InstanceUnit, bool, bool>(this, force, false))
                {
                    Parent.ForEachNearObjects(X, Y, it, static (st) =>
                    {
                        if (st.Iterator is InstanceUnit o)
                        {
                            var _this = st.Arg1;
                            var _force = st.Arg2;
                            if ((o != _this) && (_force || o.IntersectObj) && o.Parent.TouchObject2(_this, o))
                            {
                                if (_this.ElasticOtherObject(o, _force))
                                {
                                    st.Arg3 = true;
                                }
                            }
                        }
                    });
                    return it.Arg3;
                }
            }
            return false;
        }
        /// <summary>
        /// 挤开其他和自己重叠的单位，或者被Weight大于自己的人挤开
        /// </summary>
        /// <param name="o"></param>
        /// <returns>自己发生位移</returns>
        public bool ElasticOtherObject(InstanceUnit o, bool force = false)
        {
            if (IsLock)
            {
                return false;
            }
            if (force || this.IntersectObj)
            {
                float targetAngle = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                float ddr = MathVector.getDistance(this.X, this.Y, o.X, o.Y);
                if (ddr > 0)
                {
                    targetAngle = MathVector.getDegree(this.X, this.Y, o.X, o.Y);
                }
                float bdr = (this.BodyBlockSize + o.BodyBlockSize);
                float d = (bdr - ddr);
                if (!o.Moveable)
                {
                    this.MoveImpactDistance(targetAngle, -d, true);
                    return true;
                }
                else if (o.Weight > this.Weight)
                {
                    this.MoveImpactDistance(targetAngle, -d, true);
                    return true;
                }
                else
                {
                    var rst = o.MoveImpactDistance(targetAngle, d, true);
                    if (rst.result == MoveResult.MOVE_SMOOTH || rst.result == MoveResult.MOVE_RESULT_MIN_STEP)
                    {
                        o.SendForceSync();
                    }
                    return false;
                }
            }
            return false;
        }

        //         public bool StartJumpState(float direction, float moveSpeed, float? speedZ = null, float? gravity = null)
        //         {
        //             return changeState(new StateJump(this, direction, moveSpeed, speedZ ?? (AMotion ? AMotion.JumpZSpeed : 0), gravity ?? Parent.Gravity));
        //         }
        //         public bool StartJumpState(float speedZ, float? gravity = null)
        //         {
        //             return changeState(new StateJump(this, this.Direction, this.MoveSpeedSEC, speedZ, gravity ?? Parent.Gravity));
        //         }

        /// <summary>
        /// 开始自由落体
        /// </summary>
        /// <param name="speedZ"></param>
        /// <param name="gravity"></param>
        /// <param name="zlimit"></param>
        /// <returns></returns>
        public virtual FallingDown StartJump(float? speedZ, float? gravity = null)
        {
            if (AMotion)
            {
                if (!speedZ.HasValue || float.IsNaN(speedZ.Value))
                {
                    speedZ = AMotion.JumpZSpeed;
                }
            }
            if (!gravity.HasValue || gravity.Value == 0)
            {
                gravity = this.Gravity;
            }
            mCurrentFallingDown?.Dispose();
            mCurrentFallingDown = FallingDown.Alloc(this, speedZ.Value, gravity.Value);
            PostEvent(ObjectPool.Alloc<UnitJumpEvent>().Init(this.ID, speedZ.Value, gravity.Value));
            return mCurrentFallingDown;
        }


        /// <summary>
        /// 开始复杂移动
        /// </summary>
        public virtual HitMoveSpeed StartHitMove(
            object launcher,
            float direction,
            float rotateSpeedSEC,
            float expectlTimeMS,
            float moveSpeedSEC,
            float moveSpeedAdd,
            float moveSpeedAcc,
            bool isNoneTouch)
        {
            if (mCurrentStartMove != null)
            {
                mCurrentStartMove.Stop();
                mCurrentStartMove.Dispose();
            }
            var move = HitMoveSpeed.Alloc(this, direction, rotateSpeedSEC, expectlTimeMS, moveSpeedSEC, moveSpeedAdd, moveSpeedAcc, isNoneTouch);
            mCurrentStartMove = move;
            return move;
        }
        public virtual HitMoveSpeed StartHitMove(object launcher, StartMove start_move)
        {
            if (mCurrentStartMove != null)
            {
                mCurrentStartMove.Stop();
                mCurrentStartMove.Dispose();
            }
            var move = HitMoveSpeed.Alloc(this, start_move);
            mCurrentStartMove = move;
            return move;
        }

        /// <summary>
        /// 落体运动
        /// </summary>
        public class FallingDown : InstanceStatus
        {
            private InstanceUnit unit;
            private float startZ;
            private float startSpeedZ;
            private float gravity;
            private bool isEnd = true;
            public event Action<FallingDown> OnFallDown;

            protected FallingDown() { }
            public static FallingDown Alloc(InstanceUnit unit, float zspeed, float gravity)
            {
                var ret = unit.ObjectPool.AllocOrCreateAutoRelease<FallingDown>(static s => new FallingDown());
                ret.Init(unit, zspeed, gravity);
                return ret;
            }
            protected void Init(InstanceUnit unit, float zspeed, float gravity)
            {
                this.unit = unit;
                this.gravity = gravity;
                this.startSpeedZ = zspeed;
                this.unit.mPos.Jump(zspeed);
                this.unit.mPos.Gravity = gravity;
                this.startZ = unit.Z;
                this.isEnd = false;
            }
            protected override void Disposing()
            {
                this.unit = null;
                this.gravity = 0;
                this.startZ = default;
                this.startSpeedZ = default;
                this.isEnd = true;
                this.OnFallDown = default;
            }


            public bool IsEnd { get => isEnd; }
            public InstanceUnit Unit { get => unit; }
            public float StartZ { get => startZ; }
            public float StartZSpeed { get => startSpeedZ; }
            public float Gravity { get => gravity; }

            public void End()
            {
                if (isEnd) { return; }
                isEnd = true;
                OnFallDown?.Invoke(this);
                OnFallDown = null;
                unit.ResetGravity();
            }

            public bool Update(float intervalMS)
            {
                if (isEnd)
                {
                    return true;
                }
                if (!unit.mPos.IsInTheAir)
                {
                    End();
                }
                return isEnd;
            }
        }


        public class HitMoveSpeed : InstanceStatus
        {
            private InstanceUnit mOwner;
            private float mStartDirection;
            private float mTotalTimeMS;
            private float mMoveSpeedAdd;
            private float mMoveSpeedAcc;
            private float mRotateSpeedSEC;

            private float mMoveSpeedSEC;
            private bool mIsNoneTouch = false;

            private TimeExpire hitMoveTime;
            private FallingDown hasFly;
            private InstanceZoneObject moveTarget;
            private bool moveTargetBody;
            private float moveTargetKeepRange;

            private InstanceZoneObject blockTarget;
            private float blockTargetKeepRange;
            private Geometry.Vector3 prevPos;

            private bool isEnd = true;

            protected HitMoveSpeed() { }
            public static HitMoveSpeed Alloc(
                InstanceUnit owner,
                float direction,
                float rotateSpeedSEC,
                float expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAccPct,
                bool isNoneTouch)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<HitMoveSpeed>(static s => new HitMoveSpeed());
                ret.Init(owner, direction, rotateSpeedSEC, expectlTimeMS, moveSpeedSEC, moveSpeedAdd, moveSpeedAccPct, isNoneTouch);
                return ret;
            }
            public static HitMoveSpeed Alloc(InstanceUnit owner, StartMove action_move)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<HitMoveSpeed>(static s => new HitMoveSpeed());
                ret.Init(owner, action_move);
                return ret;
            }
            protected void Init(
                InstanceUnit owner,
                float direction,
                float rotateSpeedSEC,
                float expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAccPct,
                bool isNoneTouch)
            {
                this.mOwner = owner;
                this.mStartDirection = direction;

                this.mMoveSpeedSEC = moveSpeedSEC;
                this.mMoveSpeedAdd = moveSpeedAdd;
                this.mMoveSpeedAcc = moveSpeedAccPct / 100f;
                this.mRotateSpeedSEC = rotateSpeedSEC;
                this.mIsNoneTouch = isNoneTouch;
                this.mTotalTimeMS = expectlTimeMS;
                this.hitMoveTime = owner.AllocTimeExpire(mTotalTimeMS);

                this.isEnd = false;
                this.prevPos = owner.Position;
            }
            protected void Init(InstanceUnit owner, StartMove action_move)
            {
                this.mOwner = owner;
                this.mStartDirection = owner.Direction + action_move.Direction;
                if (action_move.Direction != 0)
                {
                    owner.FaceTo(mStartDirection);
                }
                this.mMoveSpeedSEC = action_move.SpeedSEC;
                this.mMoveSpeedAdd = action_move.SpeedAdd;
                this.mMoveSpeedAcc = action_move.SpeedAcc / 100f;
                this.mRotateSpeedSEC = action_move.RotateSpeedSEC;
                this.mIsNoneTouch = action_move.IsNoneTouch;
                this.mTotalTimeMS = action_move.KeepTimeMS;
                this.hitMoveTime = owner.AllocTimeExpire(mTotalTimeMS);

                if (action_move.ZSpeedSEC != 0)
                {
                    this.SetFly(
                        action_move.ZSpeedSEC,
                        action_move.OverrideGravity);
                }
                this.isEnd = false;
                this.prevPos = owner.Position;
            }
            protected override void Disposing()
            {
                this.hasFly?.Release();
                this.hasFly = default;

                this.mOwner = default;
                this.mStartDirection = default;
                this.mTotalTimeMS = default;
                this.mMoveSpeedAdd = default;
                this.mMoveSpeedAcc = default;
                this.mRotateSpeedSEC = default;

                this.mMoveSpeedSEC = default;
                this.mIsNoneTouch = false;

                this.hitMoveTime?.Dispose();
                this.hitMoveTime = default;

                this.moveTarget?.Release();
                this.moveTarget = default;
                this.moveTargetBody = default;
                this.moveTargetKeepRange = default;

                this.blockTarget?.Release();
                this.blockTarget = default;
                this.blockTargetKeepRange = default;
                this.prevPos = default;

                this.isEnd = true;
            }

            public Geometry.Vector3 PrevPos { get => prevPos; }
            public bool IsFly { get { return hasFly != null; } }
            public float TotalTimeMS { get => mTotalTimeMS; }
            public bool IsEnd { get => isEnd; }
            public bool IsNoneTouch { get { return mIsNoneTouch; } set { this.mIsNoneTouch = value; } }
            public void SetFly(float moveZSpeed, float gravity)
            {
                if (moveZSpeed != 0)
                {
                    if (gravity == 0)
                    {
                        gravity = mOwner.Gravity;
                    }
                    this.hasFly = mOwner.StartJump(moveZSpeed, gravity);
                    this.hasFly.Retain();
                }
            }
            public void SetBlockTarget(InstanceZoneObject target, float bodyKeepRange = 0)
            {
                this.blockTarget?.Release();
                this.blockTarget = target;
                this.blockTarget.Retain();
                this.blockTargetKeepRange = bodyKeepRange;
            }
            public void SetMoveTarget(InstanceZoneObject target, bool targetBodyBlock, float bodyKeepRange = 0)
            {
                this.moveTarget?.Release();
                this.moveTarget = target;
                this.moveTarget.Retain();
                this.moveTargetBody = targetBodyBlock;
                this.moveTargetKeepRange = bodyKeepRange;
            }
            public UnitHitMoveEvent GetEvent()
            {
                var evt = mOwner.ObjectPool.Alloc<UnitHitMoveEvent>().Init(mOwner.ID,
                    this.mStartDirection,
                    this.mRotateSpeedSEC,
                    this.mTotalTimeMS,
                    this.mMoveSpeedSEC,
                    this.mMoveSpeedAdd,
                    this.mMoveSpeedAcc,
                    this.IsNoneTouch);
                if (hasFly != null)
                {
                    evt.SetFly(hasFly.StartZSpeed, hasFly.Gravity);
                }
                if (moveTarget is InstanceZoneObject target)
                {
                    evt.SetMoveTarget(target.ObjectID, moveTargetBody, moveTargetKeepRange);
                }
                return evt;
            }
            public void Stop()
            {
                this.isEnd = true;
            }

            public bool Update(float intervalMS)
            {
                if (mRotateSpeedSEC != 0)
                {
                    mOwner.Turn(MoveHelper.GetDistance(intervalMS, mRotateSpeedSEC));
                }
                this.prevPos = mOwner.Position;
                if (!testBlock(intervalMS))
                {
                    // 移动 //
                    if (moveTarget != null)
                    {
                        move(intervalMS, moveTarget);
                    }
                    else
                    {
                        move(intervalMS, mStartDirection);
                    }
                }
                // 递增 //
                {
                    //每秒递减速度绝对值//
                    mMoveSpeedSEC = MoveHelper.UpdateSpeed(intervalMS, mMoveSpeedSEC, mMoveSpeedAdd, mMoveSpeedAcc);
                }
                if (hitMoveTime.Update(intervalMS))
                {
                    if (hasFly != null)
                    {
                        this.isEnd = hasFly.IsEnd;
                    }
                    else
                    {
                        this.isEnd = true;
                    }
                }
                return IsEnd;
            }
            private bool testBlock(float intervalMS)
            {
                if (blockTarget != null)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, mMoveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (Collider.Intersects(mOwner.Position,
                        blockTarget.Position,
                        blockTarget.BodySize + mOwner.BodySize + distance + blockTargetKeepRange))
                    {
                        return true;
                    }
                }
                return false;
            }
            private void move(float intervalMS, IPositionObject target)
            {
                if (moveTargetBody)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, mMoveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (Collider.Intersects(mOwner.Position, target.Position, target.BodySize + mOwner.BodySize + distance + moveTargetKeepRange))
                    {
                        return;
                    }
                }
                if (hasFly != null)
                {
                    mOwner.MoveAirToTarget(target.X, target.Y, mMoveSpeedSEC, intervalMS);
                }
                else
                {
                    if (IsNoneTouch)
                    {
                        mOwner.MoveAirToTarget(target.X, target.Y, mMoveSpeedSEC, intervalMS);
                    }
                    else if (mMoveSpeedAdd != 0 || mMoveSpeedAcc != 0)
                    {
                        mOwner.MoveImpactTo(target.X, target.Y, mMoveSpeedSEC, intervalMS, false);
                    }
                    else
                    {
                        mOwner.MoveBlockTo(target.X, target.Y, mMoveSpeedSEC, intervalMS, false);
                    }
                }
            }

            private void move(float intervalMS, float direction)
            {
                if (hasFly != null)
                {
                    mOwner.MoveAirTo(direction, mMoveSpeedSEC, intervalMS);
                }
                else
                {
                    if (IsNoneTouch)
                    {
                        mOwner.MoveAirTo(direction, mMoveSpeedSEC, intervalMS);
                    }
                    else if (mMoveSpeedAdd != 0 || mMoveSpeedAcc != 0)
                    {
                        mOwner.MoveImpact(direction, mMoveSpeedSEC, intervalMS, false);
                    }
                    else
                    {
                        mOwner.MoveBlockToAngle(direction, mMoveSpeedSEC, intervalMS, false);
                    }
                }
            }

        }

    }
}
