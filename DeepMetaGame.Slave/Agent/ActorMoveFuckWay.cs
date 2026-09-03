using DeepCore.Game3D.Helper;
using DeepCore.GameData.Data;
using DeepCore.GameData.RTS;
using DeepCore.GameData.Zone;
using DeepCore.Vector;
using System;

namespace DeepCore.Game3D.Slave.Agent
{
    /// <summary>
    /// 按照策划预先设置好的路线走路
    /// </summary>
    public class ActorMoveFuckWay : AbstractMoveAgent
    {
        private bool mFinish = false;
        public float EndDistance { get; set; }
        public UnitActionStatus MoveState { get; set; }
        public object UserData { get; set; }
        public override bool IsEnd { get { return way_points == null; } }
        public override bool IsDuplicate { get { return false; } }
        public override WayPoint WayPoints { get { return way_points; } }

        public override bool IsFinish => mFinish;

        private ZoneEditorPoint start_point;
        private Predicate<ZoneEditorPoint> select;
        private WayPoint way_points;
        private float cur_dir = 0;
        private bool auto_adjust;
        private Vector2 target_pos;
        private Vector3 cur_pos = new Vector3();

        public ActorMoveFuckWay(
            float targetX, float targetY,
            float endDistance = 0,
            Predicate<ZoneEditorPoint> select = null,
            UnitActionStatus st = UnitActionStatus.Move,
            bool autoAdjust = true,
            object ud = null)
            : this(null, targetX, targetY, endDistance, select, st, autoAdjust, ud)
        {
        }
        public ActorMoveFuckWay(
            ZoneEditorPoint start_point,
            float targetX, float targetY,
            float endDistance = 0,
            Predicate<ZoneEditorPoint> select = null,
            UnitActionStatus st = UnitActionStatus.Move,
            bool autoAdjust = true,
            object ud = null)
        {
            this.start_point = start_point;
            this.auto_adjust = autoAdjust;
            this.EndDistance = endDistance;
            this.MoveState = st;
            this.UserData = ud;
            this.target_pos = new Vector2(targetX, targetY);
            this.select = select;
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
            if (Layer == null)
            {
                return;
            }
            //开始段
            if (start_point == null)
            {
                start_point = Layer.GetNearZoneFlag<ZoneEditorPoint>(Owner.X, Owner.Y, select);
            }
            if (start_point == null)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            //结尾段
            var end_point = Layer.GetNearZoneFlag<ZoneEditorPoint>(target_pos.x, target_pos.y, select);
            if (end_point == null)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            //开始结尾一致
            if (start_point == end_point)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            //Flag链接点
            var wp_path = Layer.FindPathWayPointAsPathFinder(start_point.Name, end_point.Name);
            if (wp_path == null)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            var begin_path = Layer.FindPath(Owner.X, Owner.Y, start_point.X, start_point.Y);
            if (begin_path == null)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            var end_path = Layer.FindPath(end_point.X, end_point.Y, target_pos.X, target_pos.Y);
            if (end_path == null)
            {
                this.way_points = Layer.FindPath(Owner.X, Owner.Y, target_pos.x, target_pos.y);
                return;
            }
            begin_path.Tail.LinkNext(wp_path);
            wp_path.Tail.LinkNext(end_path);
            begin_path.OptimizePath(Layer.PathFinder);
            this.way_points = begin_path;
        }

        /// <summary>
        /// 外部打断寻路.
        /// </summary>
        public void Stop()
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
                        mFinish = true;
                        this.Stop();
                    }
                }
            }
        }

        protected override void BeginUpdate(int intervalMS)
        {
            if (way_points != null)
            {
                cur_pos.x = Owner.X;
                cur_pos.y = Owner.Y; 
                cur_pos.z = Layer.TerrainSrc.GetHeightByPos(cur_pos.x, cur_pos.y);
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
