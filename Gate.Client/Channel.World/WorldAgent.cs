using DeepCore.Geometry;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Slave;
using DeepCore.Meta.Layout;
using Gate.Data;
using Gate.Data.Protocol;

namespace World.Slave.Layer
{

    //-------------------------------------------------------------------------------------------------------------------------------

    public class WorldAgent : ChannelAgent
    {
        public WorldAgent(string uuid) : base(uuid)
        {
        }
        protected override void OnEnter(ChannelNode ch, ObjectEnterS2C enter)
        {
            base.OnEnter(ch, enter);
            this.UpdateState();
        }
        protected override void OnLeave(ChannelNode ch, ObjectLeaveS2C leave)
        {
            base.OnLeave(ch, leave);
        }
        protected override void OnUpdate(float intervalMS)
        {
            base.OnUpdate(intervalMS);
            this.UpdateState();
        }
        protected override bool HandleObjectMessage(IObjectMessage msg)
        {
            if (base.HandleObjectMessage(msg))
            {
                return true;
            }
            else if (msg is ObjectSyncState sync)
            {
                HandleObjectSyncState(sync);
                return true;
            }
            else
            {
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------
        #region State
        protected Vector3 remotePosition;
        protected float remoteDirection;
        protected float remoteMoveSpeedSEC;
        protected int remoteState;
        protected string remoteStateAction;

        public Vector3 Position { get; protected set; }
        public float Direction { get; protected set; }
        public float MoveSpeedSEC { get; protected set; }
        public int State { get; protected set; }
        public string StateAction { get; protected set; }

        protected virtual void HandleObjectSyncState(ObjectSyncState sync)
        {
            sync.TrySync(
                ref remotePosition,
                ref remoteDirection, 
                ref remoteMoveSpeedSEC,
                ref remoteState,
                ref remoteStateAction);
        }

        protected virtual void UpdateState()
        {
            if (Position != remotePosition)
            {
                Position = remotePosition;
            }
            if (Direction != remoteDirection)
            {
                Direction = remoteDirection;
            }
            if (MoveSpeedSEC != remoteMoveSpeedSEC)
            {
                MoveSpeedSEC = remoteMoveSpeedSEC;
            }
            if (State != remoteState)
            {
                State = remoteState;
            }
            if (StateAction != remoteStateAction)
            {
                StateAction = remoteStateAction;
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------

    }
}
