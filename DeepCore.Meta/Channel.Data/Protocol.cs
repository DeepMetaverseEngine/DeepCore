using DeepCore.IO;
using DeepCore.Protocol;
using System.Collections.Generic;

namespace DeepCore.Meta.Channel.Data
{
    public interface IObjectMessage : ISerializable
    {
        uint ObjectID { get; }
    }
    public interface IChannelMessage : ISerializable
    {
        int ChannelID { get; }
    }
    public interface IChannelS2C : ISerializable
    {

    }
    public interface IChannelC2S : ISerializable
    {

    }

    //--------------------------------------------------------------------------------------------------

    //--------------------------------------------------------------------------------------------------

    [MessageType(Constants.SLAVE_START + 0x11)]
    public class ActorEnterChannelS2C : Notify
    {
        public string uuid;
        public int channelID;
        public uint objectID;
        public IObjectMessage sync;
    }
    [MessageType(Constants.SLAVE_START + 0x12)]
    public class ActorLeaveChannelS2C : Notify
    {
        public string uuid;
        public int channelID;
        public uint objectID;
    }

    [MessageType(Constants.SLAVE_START + 0x13)]
    public class ClientPostChannelC2S : Notify
    {
        public List<IChannelC2S> messages;
    }

    [MessageType(Constants.SLAVE_START + 0x14)]
    public class SessionPostChannelC2S : Notify
    {
        public string playerUUID;
        public List<IChannelC2S> messages;
    }

    /// <summary>
    /// 进入频道
    /// </summary>
    [MessageType(Constants.SLAVE_START + 0x21)]
    public class ObserverEnterChannelS2C : Notify
    {
        public int channelID;
        public ChannelInfo channelInfo;
        public List<ObjectEnterS2C> objects;
    }
    /// <summary>
    /// 离开频道
    /// </summary>
    [MessageType(Constants.SLAVE_START + 0x22)]
    public class ObserverLeaveChannelS2C : Notify
    {
        public int channelID;
    }

    /// <summary>
    /// 频道状态更新
    /// </summary>
    [MessageType(Constants.SLAVE_START + 0x23)]
    public class ChannelPostObserverS2C : Notify, IChannelMessage
    {
        public int ChannelID { get => channelID; }
        public int channelID;
        public uint tick;
        public List<IChannelS2C> messages;
    }

    [MessageType(Constants.SLAVE_START + 0x24)]
    public class ObjectEnterS2C : Notify, IChannelS2C
    {
        public string uuid;
        public uint objectID;
        public IObjectMessage sync;
    }

    [MessageType(Constants.SLAVE_START + 0x25)]
    public class ObjectLeaveS2C : Notify, IChannelS2C
    {
        public uint objectID;
    }


    //--------------------------------------------------------------------------------------------------
    
    //--------------------------------------------------------------------------------------------------
}
