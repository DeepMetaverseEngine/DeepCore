using DeepCore.Geometry;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Slave;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Client.Channel.World
{
    public class WorldActor : ChannelActor
    {
        public WorldActor(string uuid) : base(uuid)
        {
        }
        protected override void OnEnter(ChannelLayout ch, ActorEnterChannelS2C enter)
        {
            base.OnEnter(ch, enter);
            if (enter.sync is ObjectSyncState sync)
            {
                HandleObjectSyncState(sync);
            }
        }
        protected override void OnUpdate(float intervalMS)
        {
            base.OnUpdate(intervalMS);
        }
        protected override void OnEndUpdate(float intervalMS)
        {
            this.UpdateState();
            base.OnEndUpdate(intervalMS);
        }

        #region State
        private ObjectSyncState pendingState;
        protected Vector3 localPosition;
        protected float localDirection;
        protected float localMoveSpeedSEC;
        protected int localState;
        protected string localStateAction;
        public Vector3 Position { get => localPosition; set => localPosition = value; }
        public float Direction { get => localDirection; set => localDirection = value; }
        public float MoveSpeedSEC { get => localMoveSpeedSEC; set => localMoveSpeedSEC = value; }
        public int State { get => localState; set => localState = value; }
        public string StateAction { get => localStateAction; set => localStateAction = value; }
        protected virtual void HandleObjectSyncState(ObjectSyncState sync)
        {
            sync.TrySync(
                ref localPosition,
                ref localDirection,
                ref localMoveSpeedSEC,
                ref localState,
                ref localStateAction);
            pendingState = sync;
        }
        protected virtual void UpdateState()
        {
            if (Agent != null)
            {
                if (0 != this.pendingState.TryUpdate(Agent.ObjectID,
                    localPosition,
                    localDirection,
                    localMoveSpeedSEC,
                    localState,
                    localStateAction))
                {
                    this.QueueClientC2S(pendingState);
                }
            }
        }
        #endregion
    }
}
