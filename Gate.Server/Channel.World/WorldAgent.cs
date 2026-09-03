using DeepCore;
using DeepCore.Geometry;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Host;
using DeepCore.Meta.Layout;
using Gate.Data;
using Gate.Server.Protocol;

namespace Gate.Server.Channel.World
{
    public class WorldNodeAgent : ChannelAgent
    {
        new public WorldChannel Channel { get => base.Channel as WorldChannel; }
        public WorldNodeAgent(string uuid, WorldChannel channel) : base(uuid, channel)
        {
        }

        protected override void OnUpdate(float intervalMS)
        {
            base.OnUpdate(intervalMS);
            this.UpdateState();
        }
        //-----------------------------------------------------------------------------------------------------------

        #region State

        protected Vector3 currentPosition;
        protected float currentDirection;
        protected float currentMoveSpeedSEC;
        protected int currentState;
        protected string currentStateAction;
        protected ObjectSyncState currentSync = new ObjectSyncState();

        public Vector3 Position { get => currentPosition; }
        public float Direction { get => currentDirection; }
        public float MoveSpeedSEC { get => currentMoveSpeedSEC; }
        public int State { get => currentState; }
        public string StateAction { get => currentStateAction; }

        protected override IObjectMessage GetObjectStateS2C()
        {
            var sync = new ObjectSyncState();
            sync.TryUpdate(ObjectID,
                   currentPosition,
                   currentDirection,
                   currentMoveSpeedSEC,
                   currentState,
                   currentStateAction);
            sync.SetMask(0xff);
            return sync;
        }
        protected virtual void HandleObjectSyncStateC2S(ObjectSyncState sync)
        {
            sync.TrySync(
                ref currentPosition,
                ref currentDirection,
                ref currentMoveSpeedSEC,
                ref currentState,
                ref currentStateAction);
        }
        protected virtual void UpdateState()
        {
            if (0 != this.currentSync.TryUpdate(ObjectID,
                currentPosition,
                currentDirection,
                currentMoveSpeedSEC,
                currentState,
                currentStateAction))
            {
                this.QueueChannelMessage(currentSync);
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------
        #region Message 

        private TimeTaskMS _pendingSwapChannel;

        protected override void HandleChannelC2S(IChannelC2S update)
        {
            if (update is ObjectSyncState sync)
            {
                HandleObjectSyncStateC2S(sync);
            }
            if (_pendingSwapChannel == null)
            {
                if (GateServerManager.World.TrySwapChunk(Channel.ChunkInfo, Position, out var next))
                {
                    _pendingSwapChannel = Channel.AddTimeDelayMS(Channel.PendingSwapChannelMS, DoSwapZoneRequest);
                    _pendingSwapChannel.UserData = next;
                }
            }
            else
            {
                var next = _pendingSwapChannel.UserData as MapChunk;
                if (!GateServerManager.World.TestInclude(next, Position))
                {
                    _pendingSwapChannel.Dispose();
                    _pendingSwapChannel = null;
                }
            }
        }

        protected virtual void DoSwapZoneRequest(TimeTaskMS timer)
        {
            var next = timer.UserData as MapChunk;
            if (!InvokePlayerNeedTransport(Channel.ChannelID, next.ChunkID))
            {
                var state = new ClientPostChannelC2S() { messages = new System.Collections.Generic.List<IChannelC2S>() };
                state.messages.Add(currentSync);
                PostSessionS2C(new PlayerNeedTransportNotify()
                {
                    fromChannelID = Channel.ChannelID,
                    nextChannelID = next.ChunkID,
                    objectState = state,
                });
            }
        }


        #endregion

        //-----------------------------------------------------------------------------------------------------------
        #region Events

        public delegate bool PlayerNeedTransportHandler(MetaObject sender, int fromChannel, int toChannel);
        public event PlayerNeedTransportHandler PlayerNeedTransport { add { event_OnPlayerNeedTransport += value; } remove { event_OnPlayerNeedTransport -= value; } }
        private PlayerNeedTransportHandler event_OnPlayerNeedTransport;
        protected virtual bool InvokePlayerNeedTransport(int fromChannel, int toChannel)
        {
            return (event_OnPlayerNeedTransport != null && event_OnPlayerNeedTransport.Invoke(this, fromChannel, toChannel));
        }
        protected override void OnDisposingEvents()
        {
            base.OnDisposingEvents();
            event_OnPlayerNeedTransport = null;
        }

        #endregion
    }


}
