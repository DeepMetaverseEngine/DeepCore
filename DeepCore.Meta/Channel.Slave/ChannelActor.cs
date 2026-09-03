using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Slave
{
    public class ChannelActor : MetaObject
    {
        public string UUID { get; }
        public ChannelLayout Layout { get; private set; }
        public ChannelAgent Agent { get; private set; }

        public ChannelActor(string uuid)
        {
            this.UUID = uuid;
        }

        internal void InternalBindAgent(ChannelAgent agent)
        {
            this.Agent = agent;
            this.OnAgentChange(agent);
        }
        internal void InternalEnter(ChannelLayout ch, ActorEnterChannelS2C enter)
        {
            this.Layout = ch;
            this.OnEnter(ch, enter);
            this.OnAgentChange(Layout.GetAgent(enter.uuid));
        }
        internal void InternalLeave(ChannelLayout ch, ActorLeaveChannelS2C leave)
        {
            this.OnLeave(ch, leave);
        }
        protected virtual void OnAgentChange(ChannelAgent agent) { }
        protected virtual void OnEnter(ChannelLayout ch, ActorEnterChannelS2C enter) { }
        protected virtual void OnLeave(ChannelLayout ch, ActorLeaveChannelS2C enter) { }
        protected override void OnEndUpdate(float intervalMS)
        {
            base.OnEndUpdate(intervalMS);
            this.FlushClientC2S();
        }

        private ClientPostChannelC2S pendingC2S = new ClientPostChannelC2S()
        {
            messages = new System.Collections.Generic.List<IChannelC2S>(),
        };

        public void QueueClientC2S(IChannelC2S post)
        {
            pendingC2S.messages.Add(post);
        }
        private void FlushClientC2S()
        {
            if (pendingC2S.messages.Count > 0)
            {
                this.Layout.Adapter.Send(pendingC2S);
                this.pendingC2S.messages.Clear();
            }
        }

    }
}
