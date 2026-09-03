using DeepCore.IO;
using DeepCore.Meta.Channel.Data;

namespace DeepCore.Meta.Channel.Host
{

    public class AddChannelObject
    {
        public string uuid;
        public ISerializable from;
        public ISession session;
    }

    public class AddChannelAgent: AddChannelObject
    {
        public ClientPostChannelC2S update;
    }


    public class AddChannelObserver: AddChannelObject
    {
    }
}
