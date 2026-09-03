namespace DeepCore.Meta.Channel.Host
{
    public class ChannelObserver : ChannelObject
    {
        public AddChannelObserver Add { get; private set; }
        public ChannelObserver(string uuid, ChannelNode channel) : base(uuid, channel)
        {
        }




    }
}
