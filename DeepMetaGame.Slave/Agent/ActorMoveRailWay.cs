using DeepCore.Game3D.Helper;
using DeepCore.GameData.Data;
using DeepCore.GameData.RTS;
using DeepCore.GameData.Zone;
using DeepCore.Vector;

namespace DeepCore.Game3D.Slave.Agent
{
    /// <summary>
    /// 按照策划预先设置好的路线走路
    /// </summary>
    public class ActorMoveRailWay : AbstractMoveAgent
    {
        private bool mFinish = false;
        public float EndDistance { get; set; }
        public UnitActionStatus MoveState { get; set; }
        public object UserData { get; set; }
        public override bool IsEnd { get { return way_points == null; } }
        public override bool IsDuplicate { get { return false; } }
        public override WayPoint WayPoints { get { return way_points; } }

        public override bool IsFinish => mFinish;

        private string start_point_name;
        private string end_point_name;
        private WayPoint way_points;
        private float cur_dir = 0;
        private bool auto_adjust;
        private Vector3 cur_pos = new Vector3();

        public ActorMoveRailWay(string startPointName, string endPointName, float endDistance = 0, UnitActionStatus st = UnitActionStatus.Move, bool autoAdjust = true, object ud = null)
        {
            this.start_point_name = startPointName;
            this.end_point_name = endPointName;
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
            var wp_path = Layer.FindPathWayPoint(start_point_name, end_point_name);
            if (wp_path != null)
            {
                this.way_points = Layer.PathFinder.GenWayPoint(wp_path.X, wp_path.Y);
                var wp = way_points;
                do
                {
                    if (wp_path.Next != null)
                    {
                        var next_wp = Layer.PathFinder.GenWayPoint(wp_path.Next.X, wp_path.Next.Y);
                        wp.LinkNext(next_wp);
                        wp_path = wp_path.Next;
                        wp = next_wp;
                        continue;
                    }
                    break;
                } while (wp_path != null);
            }
            else
            {
                this.way_points = null;
            }
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
