using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Host;
using Gate.Data;

namespace Gate.Server.Channel.World
{
    public class WorldChannel : ChannelNode
    {
        public int PendingSwapChannelMS { get; set; } = 3000;
        public MapChunk ChunkInfo { get; }
        public WorldChannel(ChannelInfo info, int channelID) : base(info, channelID)
        {
            this.ChunkInfo = GateServerManager.World.GetChunkByID(channelID);
            if (ChunkInfo == null)
            {
                throw new System.Exception();
            }
        }
        protected override ChannelAgent CreateWorldAgent(AddChannelAgent add)
        {
            return new WorldNodeAgent(add.uuid, this);
        }


    }
}
