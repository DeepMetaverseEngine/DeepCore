using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Protocol;

namespace Gate.Server.Protocol
{
    public interface IChannelLogicMessage : ISerializable { }


    public class FindChannelRequest : Request, IChannelLogicMessage
    {
        public string playerUUID;
        public int actorChannelID = -1;
    }
    public class FindChannelResponse : Response, IChannelLogicMessage
    {
        public int actorChannelID;
        public int[] nextChannels;
    }
    public class SessionBindChannelNotify : Notify
    {
        public int actorChannelID;
    }

    public class ActorEnterChannelRequest : Request, IChannelLogicMessage
    {
        public string playerUUID;
        public string logicServiceName;
        public string sessionServiceName;
        public ClientPostChannelC2S update;
        public int channelID;
    }
    public class ActorEnterChannelResponse : Response, IChannelLogicMessage
    {
        public string channelServiceName;
    }
    public class ActorLeaveChannelRequest : Request, IChannelLogicMessage
    {
        public string playerUUID;
    }
    public class ActorLeaveChannelResponse : Response, IChannelLogicMessage
    {

    }

    public class ObserverEnterChannelRequest : Request, IChannelLogicMessage
    {
        public string playerUUID;
        public string logicServiceName;
        public string sessionServiceName;
    }
    public class ObserverEnterChannelResponse : Response, IChannelLogicMessage
    {
        public string channelServiceName;
    }
    public class ObserverLeaveChannelRequest : Request, IChannelLogicMessage
    {
        public string playerUUID;
    }
    public class ObserverLeaveChannelResponse : Response, IChannelLogicMessage
    {
    }


    /// <summary>
    /// 当玩家踩到传送点，或者场景内其他传送事件 
    /// Area通知逻辑需要传送操作，一般是踩到场景传送点
    /// </summary>
    public class PlayerNeedTransportNotify : Notify, IChannelLogicMessage
    {
        public int fromChannelID;
        public int nextChannelID;
        public ClientPostChannelC2S objectState;
    }


}
