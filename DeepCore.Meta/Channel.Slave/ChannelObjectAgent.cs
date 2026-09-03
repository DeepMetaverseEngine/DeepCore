using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Slave
{

    //-------------------------------------------------------------------------------------------------------------------------------

    public class ChannelAgent : MetaObject
    {
        public string UUID { get; }
        public uint ObjectID { get; private set; }
        public ChannelNode Channel { get; private set; }

        public ChannelAgent(string uuid)
        {
            UUID = uuid;
        }
        internal void InternalEnter(ChannelNode ch, ObjectEnterS2C enter)
        {
            Channel = ch;
            ObjectID = enter.objectID;
            InternalHandleObjectMessage(enter.sync);
            OnEnter(ch, enter);
        }
        internal void InternalLeave(ChannelNode ch, ObjectLeaveS2C leave)
        {
            OnLeave(ch, leave);
        }
        internal bool InternalHandleObjectMessage(IObjectMessage msg)
        {
            return HandleObjectMessage(msg);
        }
        protected virtual void OnEnter(ChannelNode ch, ObjectEnterS2C enter) { }
        protected virtual void OnLeave(ChannelNode ch, ObjectLeaveS2C leave) { }
        protected virtual bool HandleObjectMessage(IObjectMessage msg)
        {
            return false;
        }

        //-----------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------

    }
}
