using DeepCore;
using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Host;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using Gate.Server.Channel.World;
using Gate.Server.Protocol;
using System;
using System.Threading.Tasks;

namespace Gate.Server.Service.World
{
    public class ChannelService : IService
    {
        protected readonly int ChannelID;
        protected readonly ThreadUpdateable<ChannelNode> update;
        protected readonly ChannelInfo channelInfo;
        protected readonly ChannelNode channel;

        public ChannelService(ServiceStartInfo start) : base(start)
        {
            this.ChannelID = start.Config.GetAsInt(nameof(ChannelID));
            this.channelInfo = start.Config.GetAs<ChannelInfo>(nameof(ChannelInfo));
            this.channel = new WorldChannel(channelInfo, ChannelID);
            this.update = new ThreadUpdateable<ChannelNode>(channel);
            this.update.OnUpdate += Update_OnUpdate;
            this.Provider.OnWormholeTransported += rpc_PlayerUpdateWormholeC2S;
        }
        protected override void OnDisposed()
        {

        }
        protected override Task OnStartAsync()
        {
            this.update.Start();
            return Task.CompletedTask;
        }
        protected override Task OnStopAsync()
        {
            this.update.Stop();
            return Task.CompletedTask;
        }
        private void Update_OnUpdate(ChannelNode state, float intervalMS)
        {
            channel.MainUpdate(intervalMS);
        }


        public void rpc_PlayerUpdateWormholeC2S(RemoteAddress from, object message)
        {
            if (message is BinaryMessage binary)
            {
                message = this.ServerCodec.DecodeBinary(binary);
            }
            if (message is SessionPostChannelC2S update)
            {
                rpc_PlayerUpdateC2S(from, update);
            }
        }

        [RpcHandler]
        public void rpc_PlayerUpdateC2S(RemoteAddress from, SessionPostChannelC2S update)
        {
            channel.QueueTask(() =>
            {
                if (channel.TryGetChannelAgent(update.playerUUID, out var agent) && agent.Session is AgentSession session)
                {
                    session.rpc_Handle(update);
                }
            });
        }

        [RpcHandler]
        public async Task<ActorEnterChannelResponse> rpc_PlayerEnterChannelRequest(ActorEnterChannelRequest enter)
        {
            var session = await Provider.GetAsync(enter.sessionServiceName);
            var logic = await Provider.GetAsync(enter.logicServiceName);
            return await (channel.QueueTaskAsync(i =>
            {
                var obj = channel.AddChannelAgent(new AddChannelAgent()
                {
                    uuid = enter.playerUUID,
                    from = enter,
                    session = new AgentSession(session, logic),
                    update = enter.update,
                });
                if (obj != null)
                {
                    return new ActorEnterChannelResponse()
                    {
                        s2c_code = Response.CODE_OK,
                        channelServiceName = SelfAddress.ServiceName
                    };
                }
                else
                {
                    return new ActorEnterChannelResponse() { s2c_code = Response.CODE_ERROR, };
                }
            }, enter));
        }

        [RpcHandler]
        public async Task<ActorLeaveChannelResponse> rpc_PlayerLeaveChannelRequest(ActorLeaveChannelRequest leave)
        {
            return await (channel.QueueTaskAsync(i =>
            {
                var obj = channel.RemoveChannelAgent(leave.playerUUID);
                if (obj != null)
                {
                    return new ActorLeaveChannelResponse() { s2c_code = Response.CODE_OK };
                }
                else
                {
                    return new ActorLeaveChannelResponse() { s2c_code = Response.CODE_ERROR, };
                }
            }, leave));
        }

        [RpcHandler]
        public async Task<ObserverEnterChannelResponse> rpc_ObserverEnterChannelRequest(ObserverEnterChannelRequest enter)
        {
            var session = await Provider.GetAsync(enter.sessionServiceName);
            var logic = await Provider.GetAsync(enter.logicServiceName);
            return await (channel.QueueTaskAsync(i =>
            {
                var obj = channel.AddObserver(new AddChannelObserver()
                {
                    uuid = enter.playerUUID,
                    from = enter,
                    session = new AgentSession(session, logic),
                });
                return new ObserverEnterChannelResponse()
                {
                    s2c_code = Response.CODE_OK,
                    channelServiceName = SelfAddress.ServiceName
                };
            }, enter));
        }

        [RpcHandler]
        public async Task<ObserverLeaveChannelResponse> rpc_ObserverLeaveChannelRequest(ObserverLeaveChannelRequest leave)
        {
            return await (channel.QueueTaskAsync(i =>
            {
                if (channel.RemoveWorldObserver(leave.playerUUID) is ChannelObserver obj)
                {
                    return new ObserverLeaveChannelResponse() { s2c_code = Response.CODE_OK };
                }
                return new ObserverLeaveChannelResponse() { s2c_code = Response.CODE_ERROR };
            }, leave));
        }

        public class AgentSession : ISession
        {
            public readonly IRemoteService session;
            public readonly IRemoteService logic;
            public AgentSession(IRemoteService session, IRemoteService logic)
            {
                this.session = session;
                this.logic = logic;
            }
            public event Action<ISerializable> HandleC2S;
            public void rpc_Handle(ISerializable msg)
            {
                HandleC2S?.Invoke(msg);
            }
            public void PostS2C(ISerializable msg)
            {
                if (msg is IChannelLogicMessage)
                {
                    logic.Invoke(msg);
                }
                else
                {
                    session.WormholeTransport(msg);
                }
                //else if (msg is ChannelPostObserverS2C)
                //                 else
                //                 {
                //                     session.Invoke(msg);
                //                 }
            }
            public void Flush()
            {
            }
        }
    }
}
