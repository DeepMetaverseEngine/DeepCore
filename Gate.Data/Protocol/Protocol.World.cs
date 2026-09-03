using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Protocol;

namespace Gate.Data.Protocol
{
    [MessageType(Constants.WORLD_PROTOCOL_START + 0x01)]
    public class ClientEnterChannelRequest : Request
    {
        public ClientPostChannelC2S update;
    }
    [MessageType(Constants.WORLD_PROTOCOL_START + 0x02)]
    public class ClientEnterChannelResponse : Response
    {
    }
    [MessageType(Constants.WORLD_PROTOCOL_START + 0x03)]
    public class ClientEnterChannelNotify : Notify
    {
        public int actorChannelID;
        public int[] nextChannels;
    }


    [MessageType(Constants.WORLD_PROTOCOL_START + 0x04)]
    public class ClientLeaveChannelRequest : Request
    {
    }
    [MessageType(Constants.WORLD_PROTOCOL_START + 0x05)]
    public class ClientLeaveChannelResponse : Response
    {
    }
    [MessageType(Constants.WORLD_PROTOCOL_START + 0x06)]
    public class ClientLeaveChannelNotify : Notify
    {
        public int channelID;
    }
}
