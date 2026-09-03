using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Host
{
    public abstract class ChannelObject : MetaObject
    {
        public string UUID { get; }
        public ChannelNode Channel { get; }
        public ISession Session { get; private set; }

        public ChannelObject(string uuid, ChannelNode channel)
        {
            this.UUID = uuid;
            this.Channel = channel;
        }
        internal void InternalEnter(AddChannelObject add) { OnEnter(add); }
        internal void InternalLeave() { OnLeave(); }
        //------------------------------------------------------------------------------------------------------------------
        protected virtual void OnEnter(AddChannelObject add)
        {
            this.BindSession(add.session);
        }
        protected virtual void OnLeave()
        {
        }
        protected override void OnEndUpdate(float intervalMS)
        {
            base.OnEndUpdate(intervalMS);
            this.Session?.Flush();
        }
        //------------------------------------------------------------------------------------------------------------------
        protected internal virtual IObjectMessage GetObjectStateS2C()
        {
            return null;
        }
        public void QueueChannelMessage(IChannelS2C post)
        {
            Channel.QueueChannelMessage(post);
        }
        public void PostSessionS2C(ISerializable update)
        {
            this.Session?.PostS2C(update);
        }
        protected virtual void BindSession(ISession session)
        {
            this.Session = session;
            this.Session.HandleC2S += HandleSessionC2S;
        }
        protected virtual void HandleSessionC2S(ISerializable post)
        {
            if (post is SessionPostChannelC2S c2s)
            {
                if (c2s.messages != null)
                {
                    foreach (var update in c2s.messages)
                    {
                        HandleChannelC2S(update);
                    }
                }
            }
        }
        protected virtual void HandleChannelC2S(IChannelC2S update)
        {
        }
        //------------------------------------------------------------------------------------------------------------------

    }


}
