using DeepCore.Game3D.Helper;
using DeepCore.GameData.RTS;
using DeepCore.GameData.Zone;
using DeepCore.Vector;
namespace DeepCore.Game3D.Slave.Agent
{
    public class ActorMoveAI : AbstractMoveAgent
    {
        private bool mFinish = false;
        public float EndDistance { get; set; }
        public override bool IsEnd { get { return moveAI == null; } }
        public override bool IsDuplicate { get { return false; } }
        public override WayPoint WayPoints { get { return moveAI != null ? moveAI.NextPath : null; } }

        public override bool IsFinish => mFinish;

        private MoveAI<ZoneActor, ZoneLayer> moveAI;
        private Vector2 target;

        public ActorMoveAI(float toX, float toY, float endDistance)
        {
            this.target = new Vector2(toX, toY);
            this.EndDistance = endDistance;
        }
        protected override void OnInit(ZoneActor actor)
        {
            this.moveAI = new MoveAI<ZoneActor, ZoneLayer>(actor);
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
            moveAI = null;
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
            float distance = MathVector.getDistance(Owner.X, Owner.Y, target.x, target.y);
            if (distance > EndDistance)
            {
                moveAI.FindPath(target.x, target.y);
            }
        }
        /// <summary>
        /// 外部打断寻路.
        /// </summary>
        public void Stop()
        {
            this.moveAI = null;
        }

        protected override void BeginUpdate(int intervalMS)
        {
            if (Owner.IsCanControlMove == false)
            {
                this.Stop();
            }
            if (moveAI != null)
            {
                var result = moveAI.Update();
                if ((result.result & MoveResult.RESULTS_MOVE_END) != 0)
                {
                    Stop();
                }
                else
                {
                    if (Owner.SendUpdatePos(Owner.X, Owner.Y, Owner.Z, Owner.Direction, moveAI.CurrentStatus))
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

        private void CheckEndDistance()
        {
            if (moveAI != null)
            {
                float distance = MathVector.getDistance(Owner.X, Owner.Y, target.X, target.Y);
                if (distance <= EndDistance)
                {
                    mFinish = true;
                    this.Stop();
                }
            }
        }
    }
}
