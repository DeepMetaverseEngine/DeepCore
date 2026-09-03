using DeepCore.IO;
using DeepCore.Meta.Channel.Data;

namespace DeepCore.Meta.Channel.Host
{
    public class ChannelAgent : ChannelObject
    {
        public AddChannelAgent Add { get; private set; }
        public uint ObjectID { get; }

        public ChannelAgent(string uuid, ChannelNode channel) : base(uuid, channel)
        {
            this.ObjectID = channel.GenChannelAgentID();
        }
        protected override void OnEnter(AddChannelObject add)
        {
            this.Add = add as AddChannelAgent;
            base.OnEnter(add);
            if (Add.update != null)
            {
                HandleSessionC2S(new SessionPostChannelC2S() { messages = Add.update?.messages });
            }
        }
        protected override void HandleSessionC2S(ISerializable post)
        {
            if (post is ClientPostChannelC2S a2s)
            {
                if (a2s.messages != null)
                {
                    foreach (var update in a2s.messages)
                    {
                        HandleChannelC2S(update);
                    }
                }
            }
            else
            {
                base.HandleSessionC2S(post);
            }
        }


    }


}
