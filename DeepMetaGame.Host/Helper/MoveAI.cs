using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;

namespace DeepCore.Game3D.Host.Helper
{
    //-----------------------------------------------------------------------------------------------------------------
    public struct MoveBlockResult
    {
        public MoveResult result;
        public IEntityObject touched;

        public MoveBlockResult(MoveResult r)
        {
            this.result = r;
            this.touched = null;
        }
        public bool HasFlag(MoveResult flag)
        {
            return (result & flag) != 0;
        }
    }
    //-----------------------------------------------------------------------------------------------------------------
    public interface MoveTarget
    {
        Geometry.Vector3 Pos { get; }
        IPositionObject TargetObject { get; }
        ITerrainLayer TargetLayer { get; }
    }
    public struct MoveTargetEntity : MoveTarget
    {
        private readonly IEntityObject obj;
        public MoveTargetEntity(IEntityObject o)
        {
            this.obj = o;
        }
        public IPositionObject TargetObject => obj;
        public Geometry.Vector3 Pos => obj.Position;
        public ITerrainLayer TargetLayer => obj.CurrentLayer;
    }
    public struct MoveTargetPosition : MoveTarget
    {
        private readonly IPositionObject pos;
        private readonly InstanceZone zone;
        public MoveTargetPosition(IPositionObject pos, InstanceZone zone)
        {
            this.pos = pos;
            this.zone = zone;
        }
        public IPositionObject TargetObject => pos;
        public Geometry.Vector3 Pos => pos.Position;
        public ITerrainLayer TargetLayer => zone.Terrain3D.GetVoxelLayerByPos(pos.Position);
    }
    public struct MoveTargetStatic : MoveTarget
    {
        private readonly Geometry.Vector3 pos;
        private readonly ITerrainLayer layer;
        public MoveTargetStatic(Geometry.Vector3 pos, ITerrainLayer layer)
        {
            this.pos = pos;
            this.layer = layer;
        }
        public IPositionObject TargetObject => null;
        public Geometry.Vector3 Pos => pos;
        public ITerrainLayer TargetLayer => layer;
    }

    //-----------------------------------------------------------------------------------------------------------------
    //     public interface IZoneObject
    //     {
    //         IMoveableZone Parent { get; }
    //         Random RandomN { get; }
    //         float X { get; }
    //         float Y { get; }
    //         float Z { get; }
    //         float BodyBlockSize { get; }
    //     }
    //     public interface IZoneFlag : IZoneObject
    //     {
    //         IZoneShape ZoneShape { get; }
    //     }
    //     public interface IMoveableZoneObject : IZoneObject
    //     {
    //         uint ID { get; }
    //         bool TouchMap { get; }
    //         bool TouchObj { get; }
    //         float Direction { get; }
    //     }
    //     public interface IMoveableUnit : IMoveableZoneObject
    //     {
    //         GameData.Zone.UnitInfo Info { get; }
    //         float MoveSpeedSEC { get; }
    // 
    //         UnitActionStatus CurrentActionStatus { get; }
    //         byte CurrentActionSubstate { get; }
    // 
    //         void SetActionStatus(GameData.Data.UnitActionStatus st);
    //         void FaceTo(float targetX, float targetY);
    //         MoveBlockResult MoveBlockTo(float targetX, float targetY, float speedSEC, int intervalMS);
    //         MoveImpactResult MoveImpactTo(float targetX, float targetY, float speedSEC, int intervalMS);
    //     }
    // 
    //     public interface IMoveableZone
    //     {
    //         GameData.Zone.TemplateManager Templates { get; }
    //         Random RandomN { get; }
    //         int GridCell { get; }
    //         int UpdateIntervalMS { get; }
    //         float MinStep { get; }
    //         float ElasticAngle { get; }
    //         float ElasticAngle2 { get; }
    // 
    //         VoxelWayPoint FindPath(float sx, float sy, float dx, float dy);
    //         bool RaycastMap(IMoveableZoneObject u, float sx, float sy, float dx, float dy);
    //         bool TryTouchMap(IMoveableZoneObject u, float x, float y);
    //     }
    //-----------------------------------------------------------------------------------------------------------------
    public class MoveAI : InstanceStatus
    {
        //----------------------------------------------------------------------------------------------------------------
        private InstanceUnit unit;

        private readonly MoveTurnPosObject turnLR;
        private readonly MoveTurnPosMapBlock turnMap;
        private ITerrainWayPoint next_path;
        private TimeExpire holdTime;
        private TimeInterval lookInterval;
        private bool mPause = true;
        private bool mOverrideActionStatus;

        private float hold_time;
        private float hold_max_time;
        private float hold_min_time;
        private float bypassSizeScale = 2f;

        public MoveTarget Target { get; private set; }
        public UnitActionStatus CurrentStatus { get; private set; }
        public bool IsBypass { get; set; }
        public bool IsMoveImpact { get; set; }
        public bool IsNoWay { get; private set; }
        public bool IsNoWayAutoFindNear { get; set; }
        public bool IsLandMove { get; set; } = true;
        public MoveBlockResult LastMoveResult { get; private set; }
        public bool IsFirstFindPath { get; set; }
        /// <summary>
        /// 无法寻路修正，默认为跳一下
        /// </summary>
        public Func<MoveAI, bool> OnFindPathNoWayAction;
        protected MoveAI()
        {
            this.turnLR = new MoveTurnPosObject(this);
            this.turnMap = new MoveTurnPosMapBlock(this);
        }
        public static MoveAI Alloc(InstanceUnit owner, bool overrideActionStatus = true, float holdTimeMS = 0)
        {
            return owner.ObjectPool.AllocOrCreateAutoRelease<MoveAI>(static s => new MoveAI()).Init(owner, overrideActionStatus, holdTimeMS);
        }
        protected virtual MoveAI Init(InstanceUnit owner, bool overrideActionStatus = true, float holdTimeMS = 0)
        {
            this.CurrentStatus = owner.GetStartMoveStatus();
            this.unit = owner;
            this.mOverrideActionStatus = overrideActionStatus;
            if (holdTimeMS > 1000)
            {
                hold_time = holdTimeMS;
            }
            else
            {
                hold_time = owner.CFG.AI_MOVE_AI_HOLD_TIME_MS;
            }
            this.bypassSizeScale = owner.CFG.AI_MOVE_AI_BYPASS_SCALE;
            this.holdTime = owner.AllocTimeExpire(hold_time);
            this.lookInterval = owner.AllocTimeInterval(owner.CFG.AI_VIEW_TRIGGER_CHECK_TIME_MS);
            this.holdTime.End();
            this.hold_max_time = hold_time * 2;
            this.hold_min_time = hold_time;
            this.IsNoWayAutoFindNear = true;
            this.IsFirstFindPath = Zone.CFG.AI_MOVE_FIRST_FIND_PATH;
            return this;
        }
        protected override void Disposing()
        {
            this.unit = null;
            this.turnLR.Clear();
            this.turnMap.Clear();
            this.next_path = default;
            this.holdTime?.Dispose();
            this.holdTime = default;
            this.lookInterval?.Dispose();
            this.lookInterval = null;
            this.mPause = true;
            this.mOverrideActionStatus = default;

            this.hold_time = default;
            this.hold_max_time = default;
            this.hold_min_time = default;
            this.bypassSizeScale = 2f;

            this.Target = default;
            this.CurrentStatus = default;
            this.IsBypass = default;
            this.IsMoveImpact = default;
            this.IsNoWay = default;
            this.IsNoWayAutoFindNear = default;
            this.IsLandMove = true;
            this.LastMoveResult = default;

            this.OnFindPathNoWayAction = null;
        }

        //----------------------------------------------------------------------------------------------------------------
        public InstanceUnit Unit => unit;
        public InstanceZone Zone { get => unit?.Parent; }
        public TemplateManager templates { get => unit?.Templates; }
        public ITerrainWayPoint NextPath { get { return next_path; } }
        public bool IsTurnLR { get { return turnLR != null; } }
        public Vector3? NextStepTarget
        {
            get
            {
                if (unit != null)
                {
                    if (!turnMap.IsEnd)
                    {
                        return new Vector3(turnMap.TargetX, turnMap.TargetY, this.unit.Z);
                    }
                    else if (!turnLR.IsEnd)
                    {
                        return new Vector3(turnLR.TargetX, turnLR.TargetY, this.unit.Z);
                    }
                    else if (next_path != null)
                    {
                        return next_path.Position;
                    }
                    if (Target != null)
                    {
                        return Target.Pos;
                    }
                    return unit.Position;
                }
                return null;
            }
        }
        public bool IsInRange(Geometry.Vector3 target, float range)
        {
            return (new Geometry.BoundingSphere(target, range).Contains(unit.Position) != Geometry.ContainmentType.Disjoint);
        }
        public void FindPath(IPositionObject target)
        {
            if (target is IEntityObject)
            {
                this.FindPath(target as IEntityObject);
            }
            else
            {
                this.FindPath(new MoveTargetPosition(target, Zone));
            }
        }
        public void FindPath(IEntityObject target)
        {
            this.FindPath(new MoveTargetEntity(target));
        }
        public void FindPath(Geometry.Vector3 target)
        {
            var layer = unit.Parent.Terrain3D.GetVoxelLayerByPos(target);
            if (layer != null)
            {
                this.FindPath(new MoveTargetStatic(target, layer));
            }
        }
        public void FindPath(ITerrainLayer target)
        {
            this.FindPath(new MoveTargetStatic(target.UpwardCenterPos, target));
        }
        public void FindPath(MoveTarget target)
        {
            if (target != null)
            {
                this.IsNoWay = false;
                if (Collider.Intersects(this.unit.Position, target.Pos, Zone.MinStep))
                {
                    return;
                }
                this.Target = target;
                this.mPause = false;
                if (this.IsFirstFindPath)
                {
                    FindPathInternal();
                }
            }
        }
        private bool FindPathInternal()
        {
            if (Collider.Intersects(this.unit.Position, Target.Pos, Zone.MinStep))
            {
                IsNoWay = false;
                return true;
            }
            if (!unit.Moveable) return false;
            next_path = Zone.FindPathByLayer(unit, Target.TargetLayer);
            if (next_path == null)
            {
                //if (!unit.IsZeroGravityFlyStarted)
                {
                    Hold();
                    IsNoWay = true;
                }
                return false;
            }
            else
            {
                IsNoWay = false;
                return true;
            }
        }
        public void Pause()
        {
            this.next_path = null;
            this.turnLR.Stop();
            this.turnMap.Stop();
            this.holdTime.End();
            this.mPause = true;
        }
        public virtual bool IsDirectLookTarget()
        {
            if ((Target.TargetObject is InstanceUnit t))
            {
                var keep = unit.GetKeepRange(t);
                if (Collider.Intersects(unit.Position, t.Position, keep / 2f))
                {
                    return true;
                }
            }
            return false;
        }
        protected void Hold(float timeMS = 0)
        {
            if (timeMS <= 0)
            {
                timeMS = unit.RandomN.NextFloat(hold_min_time, hold_max_time);
            }
            holdTime.Reset(timeMS);
            SetUnitActionStatus(UnitActionStatus.Idle);
        }
        protected void SetUnitActionStatus(UnitActionStatus st)
        {
            this.CurrentStatus = st;
            if (mOverrideActionStatus)
            {
                //if (unit.IsZeroGravityFlyStarted)
                //                 {
                //                     unit.SetActionStatus(st);
                //                 }
                if (unit.IsInTheAir)
                {
                    unit.SetActionStatus(UnitActionStatus.Jump);
                }
                else
                {
                    unit.SetActionStatus(st);
                }
            }
        }

        protected MoveBlockResult MoveTo(float x, float y, bool impact = false)
        {
            if (IsMoveImpact || impact)
            {
                return unit.MoveImpactTo(x, y, unit.MoveSpeedSEC, Zone.UpdateIntervalMS, IsLandMove);
            }
            else
            {
                return unit.MoveBlockTo(x, y, unit.MoveSpeedSEC, Zone.UpdateIntervalMS, IsLandMove);
            }
        }
        protected MoveBlockResult MoveTo(ref ITerrainWayPoint path, bool impact = false)
        {
            if (IsMoveImpact || impact)
            {
                return unit.MoveImpactTo(ref path, unit.MoveSpeedSEC, Zone.UpdateIntervalMS, IsLandMove);
            }
            else
            {
                return unit.MoveBlockTo(ref path, unit.MoveSpeedSEC, Zone.UpdateIntervalMS, IsLandMove);
            }
        }
        public MoveBlockResult Update()
        {
            this.LastMoveResult = UpdateInternal();
            return LastMoveResult;
        }
        protected virtual MoveBlockResult UpdateInternal()
        {
            var result = new MoveBlockResult();
            if (!unit.Moveable)
            {
                result.result = MoveResult.MOVE_RESULT_NO_WAY;
                SetUnitActionStatus(UnitActionStatus.Idle);
                return result;
            }
            if (unit.MoveSpeedSEC == 0)
            {
                result.result = MoveResult.RESULTS_MOVE_END;
                SetUnitActionStatus(UnitActionStatus.Idle);
                return result;
            }
            if (Target == null)
            {
                result.result = MoveResult.MOVE_RESULT_NO_WAY;
                SetUnitActionStatus(UnitActionStatus.Idle);
                //result.result = MoveResult.MOVE_RESULT_HOLD;
                return result;
            }
            if (mPause)
            {
                result.result = MoveResult.MOVE_RESULT_HOLD;
                SetUnitActionStatus(UnitActionStatus.Idle);
                return result;
            }
            //             if (unit.IsZeroGravityFlyStarted)
            //             {
            //                 if (!holdTime.Update(zone.UpdateIntervalMS))
            //                 {
            //                     CurrentStatus = UnitActionStatus.Idle;
            //                     result.result = MoveResult.MOVE_RESULT_HOLD;
            //                     SetUnitActionStatus(CurrentStatus);
            //                 }
            //                 else
            //                 {
            //                     result = unit.ZeroGravityFly.MoveBlockTo(Target.Pos.X, Target.Pos.Y, Target.Pos.Z, unit.MoveSpeedSEC, zone.UpdateIntervalMS);
            //                     IsNoWay = result.HasFlag(MoveResult.MOVE_RESULT_NO_WAY);
            //                     if (IsNoWay)
            //                     {
            //                         Hold();
            //                     }
            //                 }
            //                 return result;
            //             }

            if (!holdTime.Update(Zone.UpdateIntervalMS))
            {
                result.result = MoveResult.MOVE_RESULT_HOLD;
                SetUnitActionStatus(UnitActionStatus.Idle);
                return result;
            }
            else
            {
                SetUnitActionStatus(unit.GetStartMoveStatus());

                if (!turnMap.IsEnd)
                {
                    // 如果向目标移动过程中被地块阻挡，则向左或右调整一段距离 //
                    if (turnMap.UpdateMove(out result))
                    {
                        return result;
                    }
                }
                else if (!turnLR.IsEnd)
                {
                    // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                    if (turnLR.UpdateMove(out result))
                    {
                        if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                        {
                            result.result ^= MoveResult.MOVE_RESULT_ARRIVED;
                        }
                        if ((result.result & MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY) != 0)
                        {
                            Hold();
                            return result;
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                        {
                            Hold();
                        }
                    }
                }
                else
                {
                    if (IsNoWay)
                    {
                        if (IsNoWayAutoFindNear)
                        {
                            FindPathInternal();
                        }
                        if (IsNoWay)
                        {
                            unit.FaceTo(Target.Pos.X, Target.Pos.Y);
                            //                         result = unit.MoveImpactTo(Target.Pos.X, Target.Pos.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, IsLandMove);
                            //                         result = unit.MoveBlockTo(Target.Pos.X, Target.Pos.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, IsLandMove);
                            result = this.MoveTo(Target.Pos.X, Target.Pos.Y);
                            if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                            {
                                if (OnFindPathNoWayAction == null || !OnFindPathNoWayAction.Invoke(this))
                                {
                                    SetUnitActionStatus(UnitActionStatus.Jump);
                                    unit.StartJump(unit.AMotion.JumpZSpeed);
                                }
                                else
                                {
                                    Hold();
                                }
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_OBJ) != 0)
                            {
                                // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                                this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                            }
                            result.result = result.result | MoveResult.MOVE_RESULT_NO_WAY;
                            return result;
                        }
                    }
                    if (next_path != null)
                    {
                        if (lookInterval.Update(Zone.UpdateIntervalMS) && IsDirectLookTarget())
                        {
                            // 向目标移动 //
                            unit.FaceTo(Target.Pos.X, Target.Pos.Y);
                            // result = unit.MoveBlockTo(Target.Pos.X, Target.Pos.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, IsLandMove);
                            result = this.MoveTo(Target.Pos.X, Target.Pos.Y);
                            if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                            {
                                next_path = null;
                            }
                            else if (result.HasFlag(MoveResult.MOVE_RESULT_TOUCH_MAP_ALL) && result.HasFlag(MoveResult.MOVE_RESULT_MIN_STEP))
                            {
                                turnMap.Start(Target.Pos.X, Target.Pos.Y);
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                            {
                            }
                            else
                            {
                                next_path = null;
                            }
                        }
                        else
                        {
                            // 如果向目标移动过程中被地图阻挡，则寻路 //
                            var p = next_path.Position;
                            //result = unit.MoveBlockTo(ref next_path, unit.MoveSpeedSEC, zone.UpdateIntervalMS, IsLandMove);
                            result = this.MoveTo(ref next_path);
                            if ((result.result & MoveResult.MOVE_RESULT_MIN_STEP) != 0 ||
                                (result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                            {
                            }
                            else if (next_path != null)
                            {
                                var np = next_path.Position;
                                unit.FaceTo(np.X, np.Y);
                            }
                            if ((result.result & MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY) != 0)
                            {
                                //阻挡单位同意让开//
                                if ((this.turnLR.TouchCount > 0) && (unit.RandomN.Next() % 2 == 0))
                                {
                                    // 发呆一段时间 //
                                    Hold();
                                }
                                else
                                {
                                    // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                                    this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                                }
                                return result;
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_RESET_PATH) != 0)
                            {
                                if (FindPathInternal() == false)
                                {
                                    // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                                    this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                                }
                                return result;
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                            {
                                if ((result.result & MoveResult.MOVE_RESULT_MIN_STEP) != 0)
                                {
                                    next_path = null;
                                    turnMap.Start(p.X, p.Y);
                                }
                                else
                                {
                                    Hold();
                                    next_path = null;
                                }
                                return result;
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_OBJ) != 0)
                            {
                                if ((this.turnLR.TouchCount > 0) && (unit.RandomN.Next() % 2 == 0))
                                {
                                    // 发呆一段时间 //
                                    Hold();
                                }
                                else
                                {
                                    // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                                    this.turnLR.Start(p.X, p.Y, result.touched);
                                }
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                            {
                                // next_path = next_path.Next;
                                if (next_path != null)
                                {
                                    result.result ^= MoveResult.MOVE_RESULT_ARRIVED;
                                }
                                else
                                {

                                }
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_TOUCH_MAP_ALL) != 0)
                            {
                                if ((result.result & MoveResult.MOVE_RESULT_MIN_STEP) != 0)
                                {
                                    turnMap.Start(p.X, p.Y);
                                }
                            }
                            else if ((result.result & MoveResult.MOVE_RESULT_NO_WAY) != 0)
                            {
                                next_path = null;
                            }
                        }
                    }
                    else // mp way point
                    {
                        // 向目标移动 //
                        unit.FaceTo(Target.Pos.X, Target.Pos.Y);
                        //result = unit.MoveBlockTo(Target.Pos.X, Target.Pos.Y, unit.MoveSpeedSEC, zone.UpdateIntervalMS, IsLandMove);
                        result = this.MoveTo(Target.Pos.X, Target.Pos.Y);
                        if ((result.result & MoveResult.MOVE_RESULT_ARRIVED) != 0)
                        {
                            //result.result ^= MoveResult.MOVE_RESULT_ARRIVED;
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_TOUCH_OBJ_GETAWAY) != 0)
                        {
                            //阻挡单位同意让开//
                            if ((this.turnLR.TouchCount > 0) && (unit.RandomN.Next() % 2 == 0))
                            {
                                // 发呆一段时间 //
                                Hold();
                            }
                            else
                            {
                                // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                                this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                            }
                            return result;
                        }
                        else if (result.HasFlag(MoveResult.MOVE_RESULT_TOUCH_MAP_ALL) && result.HasFlag(MoveResult.MOVE_RESULT_MIN_STEP))
                        {
                            // 如果向目标移动过程中被地图阻挡，则寻路//
                            if (!FindPathInternal())
                            {
                                this.turnMap.Start(Target.Pos.X, Target.Pos.Y);
                            }
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_RESET_PATH) != 0)
                        {
                            // 如果向目标移动过程中被地图阻挡，则寻路//
                            if (!FindPathInternal())
                            {
                                this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                            }
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_MAP) != 0)
                        {
                            // 如果向目标移动过程中被地图阻挡，则寻路//
                            if (!FindPathInternal())
                            {
                                this.turnMap.Start(Target.Pos.X, Target.Pos.Y);
                            }
                        }
                        else if ((result.result & MoveResult.MOVE_RESULT_BLOCK_OBJ) != 0)
                        {
                            // 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离 //
                            this.turnLR.Start(Target.Pos.X, Target.Pos.Y, result.touched);
                        }
                    }
                }
            }
            return result;
        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离
        /// </summary>
        private class MoveTurnPosObject
        {
            public MoveAI Move { get; }
            public bool IsEnd { get; private set; }
            public IEntityObject Touched { get; private set; }
            public float TargetX { get; private set; }
            public float TargetY { get; private set; }
            public bool FaceTo { get; private set; }
            private Vector2 TargetPos;
            private readonly ArrayList<IEntityObject> touched_list = new ArrayList<IEntityObject>();

            public MoveTurnPosObject(MoveAI owner)
            {
                this.Move = owner;
                this.IsEnd = true;
            }
            public void Clear()
            {
                this.IsEnd = true;
                this.Touched = default;
                this.TargetX = default;
                this.TargetY = default;
                this.FaceTo = default;
                this.TargetPos = default;
                this.touched_list.Clear();
            }


            public InstanceUnit Owner { get => Move.unit; }
            public int TouchCount { get { return touched_list != null ? touched_list.Count : 0; } }


            public void Start(float targetX, float targetY, IEntityObject touched)
            {
                this.Touched = touched;
                this.IsEnd = false;
                this.TargetPos = new Vector2(targetX, targetY);
                this.touched_list.Clear();
                this.TurnNearBypass();
            }


            public void Stop()
            {
                this.IsEnd = true;
                this.touched_list.Clear();
            }

            /// <summary>
            /// 绕过某个单位移动
            /// </summary>
            /// <param name="speedSEC"></param>
            /// <param name="intervalMS"></param>
            /// <param name="result"></param>
            /// <returns>是否移动结束</returns>
            public bool UpdateMove(out MoveBlockResult result)
            {
                Owner.FaceTo(TargetX, TargetY);
                result = Move.MoveTo(TargetX, TargetY, true);
                if (result.touched != null)
                {
                    Touched = result.touched;
                }
                if ((result.result & (MoveResult.MOVE_RESULT_BLOCK_MAP)) != 0)
                {
                    this.IsEnd = true;
                    return true;
                }
                //                 else if ((result.result & (MoveResult.MOVE_RESULT_RESET_PATH)) != 0)
                //                 {
                //                     this.IsEnd = true;
                //                     return true;
                //                 }
                else if ((result.result & (MoveResult.MOVE_RESULT_ARRIVED)) != 0)
                {
                    if (this.TargetX == Owner.X && this.TargetY == Owner.Y)
                    {
                        this.TargetX = TargetPos.X;
                        this.TargetY = TargetPos.Y;
                        this.IsEnd = true;
                        return true;
                    }
                }
                if (result.touched != null)
                {
                    //换一种躲避方法//
                    if (touched_list.Count == 0)
                    {
                        TurnNearBypass();
                        touched_list.Add(result.touched);
                    }
                    else if (touched_list.Contains(result.touched))
                    {
                        TurnNearBypass(Owner.RandomN.NextFloat() * CMath.RADIANS_45);
                    }
                    else
                    {
                        TurnNearBypass();
                        touched_list.Add(result.touched);
                    }
                }
                return false;
            }

            /// <summary>
            /// 从最近点绕过去
            /// </summary>
            private void TurnNearBypass(float angleOffset = 0f)
            {
                float bodysize = Math.Max(Touched.BodySize, Owner.BodyBlockSize) * Move.bypassSizeScale; // 1~2个身位 //
                float distance = (float)(Owner.RandomN.NextFloat() * bodysize);
                float angle = MathVector.getDegree(Touched.X, Touched.Y, Owner.X, Owner.Y);
                var turnL = new Vector2(Owner.X, Owner.Y);
                var turnR = new Vector2(Owner.X, Owner.Y);
                VectorHelper.MovePolar(ref turnL, angle + CMath.PI_DIV_2 + angleOffset, distance);
                VectorHelper.MovePolar(ref turnR, angle - CMath.PI_DIV_2 - angleOffset, distance);
                float dl = VectorHelper.GetDistanceSquare(turnL, TargetPos.Value);
                float dr = VectorHelper.GetDistanceSquare(turnR, TargetPos.Value);
                if (dl < dr)
                {
                    this.TargetX = turnL.X;
                    this.TargetY = turnL.Y;
                }
                else
                {
                    this.TargetX = turnR.X;
                    this.TargetY = turnR.Y;
                }
            }




        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 如果向目标移动过程中被单位阻挡，则向左或右调整一段距离
        /// </summary>
        private class MoveTurnPosMapBlock
        {
            public MoveAI Move { get; }
            private float tx;
            private float ty;
            public bool IsEnd { get; private set; }

            public MoveTurnPosMapBlock(MoveAI owner)
            {
                this.Move = owner;
                this.IsEnd = true;
            }
            public void Clear()
            {
                this.IsEnd = true;
                this.tx = default;
                this.ty = default;
            }

            public float TargetX { get => tx; }
            public float TargetY { get => ty; }
            public InstanceUnit Owner { get => Move.unit; }

            public void Start(float target_x, float target_y)
            {
                float tdx = (target_x - Owner.X);
                float tdy = (target_y - Owner.Y);
                int ddx = (int)CMath.GetDirect(tdx);
                int ddy = (int)CMath.GetDirect(tdy);
                if (Math.Abs(tdx) < Math.Abs(tdy))
                {
                    float tw = ddx * (float)(Owner.Parent.GridCell + (Owner.RandomN.NextFloat() * Owner.Parent.GridCell) * Move.bypassSizeScale);
                    tx = Owner.X + tw;
                    ty = Owner.Y;
                }
                else
                {
                    float th = ddy * (float)(Owner.Parent.GridCell + (Owner.RandomN.NextFloat() * Owner.Parent.GridCell) * Move.bypassSizeScale);
                    tx = Owner.X;
                    ty = Owner.Y + th;
                }
                this.IsEnd = false;
            }

            public void Stop()
            {
                this.IsEnd = true;
            }

            /// <summary>
            /// 绕过某个单位移动
            /// </summary>
            /// <param name="speedSEC"></param>
            /// <param name="intervalMS"></param>
            /// <param name="result"></param>
            /// <returns>是否移动结束</returns>
            public bool UpdateMove(out MoveBlockResult result)
            {
                Owner.FaceTo(tx, ty);
                result = Move.MoveTo(tx, ty, true);
                if ((result.result & MoveResult.RESULTS_BLOCK_ANY) != 0)
                {
                    this.IsEnd = true;
                    return true;
                }
                if ((result.result & (MoveResult.MOVE_RESULT_RESET_PATH)) != 0)
                {
                    this.IsEnd = true;
                    return true;
                }
                if ((result.result & (MoveResult.MOVE_RESULT_ARRIVED)) != 0)
                {
                    this.IsEnd = true;
                    return true;
                }
                //                 if ((result.result & MoveResult.MOVE_RESULT_RESET_PATH) != 0)
                //                 {
                //                     this.IsEnd = true;
                //                     return true;
                //                 }
                return false;
            }
        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 计算单位击退方向
        /// </summary>
        /// <param name="damage">受击者</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="mtype"></param>
        /// <returns></returns>
        public static float CalculateHitMoveDirection(InstanceUnit damage, InstanceZoneObject attacker, AttackProp.HitMoveType mtype)
        {
            switch (mtype)
            {
                case AttackProp.HitMoveType.BySenderPosition:
                    return MathVector.getDegree(attacker.X, attacker.Y, damage.X, damage.Y);
                case AttackProp.HitMoveType.BySenderDirection:
                    return attacker.Direction;
                case AttackProp.HitMoveType.BySenderLeftRight:
                    float fx = attacker.X;
                    float fy = attacker.Y;
                    MathVector.movePolar(ref fx, ref fy, attacker.Direction, 10);
                    if (CMath.PointOnLine(attacker.X, attacker.Y, fx, fy, damage.X, damage.Y) == CMath.PointOnLineResult.Left)
                    {
                        return attacker.Direction - CMath.PI_DIV_2;
                    }
                    else
                    {
                        return attacker.Direction + CMath.PI_DIV_2;
                    }
                case AttackProp.HitMoveType.ToSenderCenter:
                case AttackProp.HitMoveType.ToSenderBodySize:
                    return attacker.Direction + CMath.PI_F;
            }
            return damage.Direction + CMath.PI_F;
        }

    }
    //-----------------------------------------------------------------------------------------------------------------


}