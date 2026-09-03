using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Slave
{

    public class ChannelNode : MetaObjectMap<uint, ChannelAgent>
    {
        public ChannelLayout Layer { get => Root as ChannelLayout; }
        public int ChannelID { get; }
        public ChannelInfo Info { get; }
        public string UUID { get => Info?.UUID; }

        public ChannelNode(ObserverEnterChannelS2C enter)
        {
            ChannelID = enter.channelID;
            Info = enter.channelInfo;
            Name = Info.Name;
        }
        internal bool DoObjectEnterS2C(ObjectEnterS2C enter, ChannelAgent agent)
        {
            var ret = AddChild(enter.objectID, agent);
            if (ret)
            {
                agent.InternalEnter(this, enter);
            }
            return ret;
        }
        internal bool DoObjectLeaveS2C(ObjectLeaveS2C leave, out ChannelAgent agent)
        {
            if (TryRemoveChild(leave.objectID, out agent, true))
            {
                agent.InternalLeave(this, leave);
                return true;
            }
            return false;
        }
        internal void InternalHandleChannelMessage(IChannelMessage msg)
        {
            HandleChannelMessage(msg);
        }
        protected virtual bool HandleChannelMessage(IChannelMessage msg)
        {
            return false;
        }

    }

}
