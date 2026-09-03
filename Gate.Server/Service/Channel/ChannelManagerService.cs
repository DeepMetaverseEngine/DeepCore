using DeepCore;
using DeepCore.Meta.Channel.Data;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Server.Service.World
{
    public class ChannelManagerService : IService
    {
        public ChannelManagerService(ServiceStartInfo start) : base(start)
        {
        }
        protected override void OnDisposed()
        {
        }
        protected override async Task OnStartAsync()
        {
            await CreateChannelNodesAsync();
        }
        protected override async Task OnStopAsync()
        {
            channels.Clear();
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private SortedDictionary<int, ChannelNodeInfo> channels = new SortedDictionary<int, ChannelNodeInfo>();
        public class ChannelNodeInfo : Disposable
        {
            public readonly ChannelManagerService Manager;
            public readonly int ChannelID;
            public readonly IRemoteService Proxy;
            public readonly ChannelInfo Info;
            private int[] lookChannels;
            private HashMap<int, ChannelNodeInfo> nexts = new HashMap<int, ChannelNodeInfo>();
            public IEnumerable<ChannelNodeInfo> NextChannels { get => nexts.Values; }
            public int[] NextChannelsID { get => lookChannels; }
            public ChannelNodeInfo(ChannelManagerService manager, IRemoteService proxy, int channelID, ChannelInfo info)
            {
                this.Manager = manager;
                this.ChannelID = channelID;
                this.Proxy = proxy;
                this.Info = info;
            }
            public virtual void InitNexts(int[] nextsID)
            {
                this.lookChannels = new int[0];
                //                 this.lookChannels = nextsID ?? new int[0];
                //                 if (nextsID != null)
                //                 {
                //                     foreach (var nid in nextsID)
                //                     {
                //                         if (Manager.TryGetChannelInfo(nid, out var next))
                //                         {
                //                             this.nexts.Add(nid, next);
                //                         }
                //                     }
                //                 }
            }
            protected override void Disposing()
            {

            }
        }

        protected virtual async Task CreateChannelNodesAsync()
        {
            var chunks = GateServerManager.World.ListMapChunks();
            foreach (var chunkID in chunks)
            {
                var info = GateServerManager.World.GetChannelInfo(chunkID);
                try
                {
                    await CreateChannelAsync(chunkID, info, GateServerManager.ServerName.GetWorldChannelService(chunkID, SelfNode));
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            foreach (var ch in channels.Values)
            {
                ch.InitNexts(GateServerManager.World.ListNextChunks(ch.ChannelID));
            }
        }
        protected virtual async Task<ChannelNodeInfo> CreateChannelAsync(int channelID, ChannelInfo info, RemoteAddress address)
        {
            var svc = await Provider.GetOrCreateAsync(address, new
            {
                ChannelID = channelID,
                ChannelInfo = info
            });
            var cvd = new ChannelNodeInfo(this, svc, channelID, info);
            channels.Add(channelID, cvd);
            return cvd;
        }
        public bool TryGetChannelInfo(int channelID, out ChannelNodeInfo info)
        {
            return channels.TryGetValue(channelID, out info);
        }

        protected virtual async Task<ChannelNodeInfo> FindChannelAsync(FindChannelRequest enter)
        {
            if (TryGetChannelInfo(enter.actorChannelID, out var expectChannel))
            {
                return expectChannel;
            }
            //             if (TryGetAgentInfo(enter.playerUUID, out var agent) && agent.Channel != null)
            //             {
            //                 return agent.Channel;
            //             }
            foreach (var ch in channels.Values)
            {
                return ch;
            }
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        [RpcHandler]
        public async Task<FindChannelResponse> rpc_FindChannelRequest(FindChannelRequest find)
        {
            var channel = await FindChannelAsync(find);
            if (channel != null)
            {
                return new FindChannelResponse()
                {
                    actorChannelID = channel.ChannelID,
                    nextChannels = channel.NextChannelsID,
                };
            }
            else
            {
                return new FindChannelResponse() { s2c_code = Response.CODE_ERROR };
            }
        }



    }
}
