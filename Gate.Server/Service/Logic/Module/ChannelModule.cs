using DeepCore;
using DeepCore.Meta.Channel.Data;
using DeepCrystal.ORM;
using DeepCrystal.RPC;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Google.Protobuf.WellKnownTypes.Field.Types;

namespace Gate.Server.Service.Logic.Module
{
    public class ChannelModule : IServiceModule<LogicService>, ILogicModule
    {
        private IRemoteService worldManager;
        public ChannelModule(LogicService service) : base(service)
        {
            //this.Service.Provider.OnWormholeTransported += Provider_OnWormholeTransported;
        }
        public override async Task OnStartAsync()
        {
            this.worldManager = await Service.Provider.GetAsync(GateServerManager.ServerName.GetWorldManagerService(Service.SelfNode));
        }
        public override async Task OnStopAsync()
        {
            if (actorChannel != null)
            {
                await actorChannel.proxy.CallAsync<ActorLeaveChannelResponse>(new ActorLeaveChannelRequest()
                {
                    playerUUID = this.Service.RoleID,
                });
            }
            await ClearObserverAsync();
            await base.OnStopAsync();
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        public Task OnClientEnterGameAsync()
        {
            return Task.CompletedTask;
        }
        public Task OnSessionDisconnectAsync(SessionDisconnectNotify notify)
        {
            return ClearObserverAsync();
        }
        public Task OnSessionReconnectAsync(SessionReconnectNotify notify)
        {
            //return ClearObserverAsync();
            return Task.CompletedTask;
        }
        public void OnSaveData(IObjectTransaction trans)
        {
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        #region LookingChannels

        public class ChannelSnap
        {
            public readonly IRemoteService proxy;
            public readonly int channelID;
            public ChannelSnap(int channelID, IRemoteService proxy)
            {
                this.proxy = proxy;
                this.channelID = channelID;
            }
        }

        private HashMap<int, ChannelSnap> lookingChannels = new HashMap<int, ChannelSnap>();
        private ChannelSnap actorChannel;
        public async Task ClearObserverAsync()
        {
            var close = lookingChannels.Values.ToArray();
            lookingChannels.Clear();
            foreach (var channel in close)
            {
                await channel.proxy.CallAsync<ObserverLeaveChannelResponse>(new ObserverLeaveChannelRequest()
                {
                    playerUUID = this.Service.RoleID,
                });
            }
        }
        public async Task<ChannelSnap> ResetObserverAsync(FindChannelResponse find)
        {
            var lookChannels = new HashSet<int>();
            {
                lookChannels.Add(find.actorChannelID);
                lookChannels.AddRange(find.nextChannels);
            }
            foreach (var exist in new HashMap<int, ChannelSnap>(lookingChannels).Values)
            {
                if (!lookChannels.Contains(exist.channelID))
                {
                    lookingChannels.Remove(exist.channelID);
                    await exist.proxy.CallAsync<ObserverLeaveChannelResponse>(new ObserverLeaveChannelRequest()
                    {
                        playerUUID = this.Service.RoleID,
                    });
                    log.Info("ObserverLeaveChannelResponse " + exist.channelID);
                }
            }
            foreach (var look in lookChannels)
            {
                if (!lookingChannels.ContainsKey(look))
                {
                    var lookS = await Service.Provider.GetAsync(GateServerManager.ServerName.GetWorldChannelService(look));
                    var lookR = await lookS.CallAsync<ObserverEnterChannelResponse>(new ObserverEnterChannelRequest()
                    {
                        logicServiceName = this.Service.Name,
                        sessionServiceName = this.Service.SessionName,
                        playerUUID = this.Service.RoleID,
                    });
                    log.Info("ObserverEnterChannelResponse " + look);
                    if (lookR.IsSuccess)
                    {
                        lookingChannels.Add(look, new ChannelSnap(look, lookS));
                    }
                }
            }
            actorChannel = lookingChannels.Get(find.actorChannelID);
            return actorChannel;
        }
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler]
        public async Task<ClientEnterChannelResponse> rpc_PlayerEnterWorldRequest(ClientEnterChannelRequest req)
        {
            var find = await worldManager.CallAsync<FindChannelResponse>(new FindChannelRequest()
            {
                playerUUID = this.Service.RoleID,
            });
            await Service.Session.InvokeAsync(new SessionBindChannelNotify()
            {
                actorChannelID = find.actorChannelID
            });
            Service.Session.WormholeTransport(new ClientEnterChannelNotify()
            {
                actorChannelID = find.actorChannelID,
                nextChannels = find.nextChannels
            });
            await ResetObserverAsync(find);
            var rsp = await actorChannel.proxy.CallAsync<ActorEnterChannelResponse>(new ActorEnterChannelRequest()
            {
                playerUUID = this.Service.RoleID,
                logicServiceName = this.Service.Name,
                sessionServiceName = this.Service.SessionName,
                channelID = find.actorChannelID,
                update = req.update,
            });
            return new ClientEnterChannelResponse() { s2c_code = rsp.s2c_code };
        }

        [RpcHandler]
        public async Task<ClientLeaveChannelResponse> rpc_PlayerLeaveWorldRequest(ClientLeaveChannelRequest req)
        {
            if (actorChannel != null)
            {
                await actorChannel.proxy.CallAsync<ActorLeaveChannelResponse>(new ActorLeaveChannelRequest()
                {
                    playerUUID = this.Service.RoleID,
                });
            }
            await ClearObserverAsync();
            Service.Session.WormholeTransport(new ClientLeaveChannelNotify()
            {
                channelID = actorChannel.channelID
            });
            return new ClientLeaveChannelResponse() { };
        }


        [RpcHandler]
        public async Task rpc_PlayerNeedTransportNotify(PlayerNeedTransportNotify notify)
        {
            if (actorChannel != null)
            {
                await actorChannel.proxy.CallAsync<ActorLeaveChannelResponse>(new ActorLeaveChannelRequest()
                {
                    playerUUID = this.Service.RoleID,
                });
            }
            var find = await worldManager.CallAsync<FindChannelResponse>(new FindChannelRequest()
            {
                playerUUID = this.Service.RoleID,
                actorChannelID = notify.nextChannelID,
            });
            await Service.Session.InvokeAsync(new SessionBindChannelNotify()
            {
                actorChannelID = find.actorChannelID
            });
            await ResetObserverAsync(find);
            await actorChannel.proxy.CallAsync<ActorEnterChannelResponse>(new ActorEnterChannelRequest()
            {
                playerUUID = this.Service.RoleID,
                logicServiceName = this.Service.Name,
                sessionServiceName = this.Service.SessionName,
                update = notify.objectState,
                channelID = notify.nextChannelID,
            });
        }

        //------------------------------------------------------------------------------------------------------------------------------------
    }
}
