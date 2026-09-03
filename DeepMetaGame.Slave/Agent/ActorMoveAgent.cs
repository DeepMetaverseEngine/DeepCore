using DeepCore.Game3D.Helper;
using DeepCore.GameData.Data;
using DeepCore.GameData.RTS;
using DeepCore.GameData.Zone;
using DeepCore.Vector;
namespace DeepCore.Game3D.Slave.Agent
{
    public class ActorMoveAgent : AbstractMoveAgent
    {
        public float TargetX { get { return target.X; } }
        public float TargetY { get { return target.Y; } }
        public FindPathResult Result { get; private set; }
        public float EndDistance { get; set; }
        public UnitActionStatus MoveState { get; set; }
        public object UserData { get; set; }
        public override bool IsEnd { get { return way_points == null; } }
        public override bool IsDuplicate { get { return false; } }
        public override WayPoint WayPoints { get { return way_points; } }

        /// <summary>
        /// 是否到达终点
        /// </summary>
        public override bool IsFinish { get { return CMath.getDistanceSquare(Owner.X, Owner.Y, TargetX, TargetY) <= EndDistance * EndDistance; } }


        private WayPoint way_points;
        private Vector2 target;
        private float cur_dir = 0;
        private bool auto_adjust;
        private Vector3 cur_pos = new Vector3();

        public ActorMoveAgent(float toX, float toY, float endDistance, UnitActionStatus st = UnitActionStatus.Move, bool autoAdjust = true, object ud = null)
        {
            this.target = new Vector2(toX, toY);
            this.auto_adjust = autoAdjust;
            this.EndDistance = endDistance;
            this.MoveState = st;
            this.UserData = ud;
        }
        protected override void OnInit(ZoneActor actor)
        {
            this.Owner.OnDoEvent += Owner_OnDoEvent;
            this.OnEnd += ActorMoveAgent_OnEnd;
            this.Start();
        }
        private void ActorMoveAgent_OnEnd(AbstractAgent agent)
        {
            if (this.Owner != null)
            {
                this.Owner.OnDoEvent -= Owner_OnDoEvent;
            }

        }
        protected override void OnDispose()
        {
            this.Owner.OnDoEvent -= Owner_OnDoEvent;
            base.OnDispose();
            way_points = null;
        }


        private void Owner_OnDoEvent(ZoneObject obj, ObjectEvent e)
        {
            if (e is UnitForceSyncPosEvent)
            {
                this.Stop();
            }
        }

        /// <summary>
        /// 再次开始
        /// </summary>
        public void Start()
        {
            float distance = MathVector.getDistance(Owner.X, Owner.Y, TargetX, TargetY);
            if (distance > EndDistance)
            {
                Result = Owner.Parent.FindPathResult(Owner.X, Owner.Y, TargetX, TargetY, out way_points);
                if (auto_adjust && way_points == null)
                {
                    //从目标点找个能走的//
                    if (Owner.Parent.PathFinderTerrain.RayCast(TargetX, TargetY, Owner.X, Owner.Y, out target.x, out target.y, out distance))
                    {
                        MathVector.moveTo(target, Owner.X, Owner.Y, EndDistance);
                        var node = Owner.Parent.PathFinderTerrain.GetMapNodeByPos(target.x, target.y);
                        if (node != null)
                        {
                            Result = Owner.Parent.FindPathResult(Owner.X, Owner.Y, target.X, target.Y, out way_points);
                            if (way_points != null) { return; }
                        }
                    }
                    //从出发点找个能走的//
                    if (Owner.Parent.PathFinderTerrain.RayCast(Owner.X, Owner.Y, TargetX, TargetY, out target.x, out target.y, out distance))
                    {
                        MathVector.moveTo(target, Owner.X, Owner.Y, EndDistance);
                        var node = Owner.Parent.PathFinderTerrain.GetMapNodeByPos(target.x, target.y);
                        if (node != null)
                        {
                            Result = Owner.Parent.FindPathResult(Owner.X, Owner.Y, target.X, target.Y, out way_points);
                            if (way_points != null) { return; }
                        }
                    }
                }
            }
            else
            {
                Result = FindPathResult.Destination;
            }
        }
        /// <summary>
        /// 外部打断寻路.
        /// </summary>
        public void Stop(string reason = null)
        {
            this.way_points = null;
        }

        private void Turn(int intervalMS)
        {
            if (way_points != null)
            {
                float direction = MathVector.getDegree(cur_pos.x, cur_pos.y, way_points.PosX, way_points.PosY);
                this.cur_dir = MoveHelper.DirectionChange(
                           direction,
                           cur_dir,
                           Owner.TurnSpeedSEC,
                           intervalMS);
            }
        }
        private void CheckEndDistance()
        {
            if (way_points != null)
            {
                float distance = MathVector.getDistance(cur_pos.x, cur_pos.y, way_points.PosX, way_points.PosY);
                if (way_points.Next == null)
                {
                    if (distance <= EndDistance)
                    {
                        this.Stop();
                    }
                }
            }
        }

        protected override void BeginUpdate(int intervalMS)
        {
            //if (Owner.IsCanControlMove == false)
            //{
            //    this.Stop();
            //}
            if (way_points != null)
            {
                cur_pos.x = Owner.X;
                cur_pos.y = Owner.Y;
                Layer.TerrainSrc.TryGetHeightByPos(cur_pos.x, cur_pos.y, out var z);
                cur_pos.z = z;

                cur_dir = Owner.Direction;

                if (Owner.MoveSpeedSEC == 0)
                {
                    this.Stop();
                    return;
                }

                float length = MoveHelper.GetDistance(intervalMS, Owner.MoveSpeedSEC);
                float distance = MathVector.getDistance(cur_pos.x, cur_pos.y, way_points.PosX, way_points.PosY);
                if (MathVector.moveTo(cur_pos, way_points.PosX, way_points.PosY, length))
                {
                    this.way_points = way_points.Next as WayPoint;
                    if (distance < length && way_points != null)
                    {
                        MathVector.moveTo(cur_pos, way_points.PosX, way_points.PosY, length - distance);
                    }
                }
                else
                {
                    Turn(intervalMS);
                }
                if (Layer.TryTouchMap(Owner, cur_pos.x, cur_pos.y))
                {
                    this.Stop();
                }
                else
                {
                    if (Owner.SendUpdatePos(cur_pos.X, cur_pos.y, cur_pos.z, cur_dir, MoveState))
                    {
                        CheckEndDistance();
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        public bool TryStep()
        {
            if (way_points != null)
            {
                float px = Owner.X;
                float py = Owner.Y;
                int intervalMS = Layer.CurrentIntervalMS;
                float length = MoveHelper.GetDistance(intervalMS, Owner.MoveSpeedSEC);
                float distance = MathVector.getDistance(px, py, way_points.PosX, way_points.PosY);
                MathVector.moveTo(ref px, ref py, way_points.PosX, way_points.PosY, length);
                if (Layer.TryTouchMap(Owner, px, py))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
