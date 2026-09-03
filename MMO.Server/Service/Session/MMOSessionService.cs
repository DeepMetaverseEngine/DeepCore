using DeepCore.IO;
using DeepCrystal.RPC;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Threading.Tasks;

namespace Gate.Server.Service.Session
{
    /// <summary>
    /// 单个链接服务
    /// </summary>
    public partial class MMOSessionService : SessionService
    {

        public MMOSessionService(ServiceStartInfo start) : base(start)
        {
        }
        protected override void OnDisposed()
        {
            base.OnDisposed();
            this.remote_area_service = null;
        }

        [RpcHandler(typeof(SessionDisconnectNotify))]
        public override void rpc_disconnect_Handle(SessionDisconnectNotify disconnect)
        {
            var area = remote_area_service;
            if (area != null)
            {
                area.Invoke(disconnect);
            }
            base.rpc_disconnect_Handle(disconnect);
        }

        [RpcHandler(typeof(ClientExitGameRequest), typeof(ClientExitGameResponse))]
        public override async Task<ClientExitGameResponse> rpc_client_Handle(ClientExitGameRequest exit)
        {
            var ret = await base.rpc_client_Handle(exit);
            this.remote_area_service = null;
            return ret;
        }
        public override void call_connect_OnReceivedBinaryImmediately(TypeCodec route_codec, BinaryMessage binary, OnRpcReturnBinary cb = null)
        {
            if (IsAreaProtocol(route_codec))
            {
                SendToArea(route_codec, binary);
                return;
            }
            base.call_connect_OnReceivedBinaryImmediately(route_codec, binary, cb);
        }

        protected override async Task ShutdownLogicServiceAsync(string reason)
        {
            var logic = remote_logic_service;
            var area = remote_area_service;
            remote_logic_service = null;
            if (logic != null)
            {
                if (await Provider.GetAsync(logic.Address) != null)
                {
                    if (area != null)
                    {
                        try
                        {
                            await logic.CallAsync<SessionBeginLeaveResponse>(new SessionBeginLeaveRequest()
                            {
                                sessionName = SelfAddress.ServiceName,
                                roleID = enter_game.c2s_roleUUID,
                            });
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    }
                    try
                    {
                        var result = await logic.ShutdownAsync(reason);
                        log.Info("ShutdownAsync Complete : " + result);
                    }
                    catch (Exception err)
                    {
                        log.Error("ShutdownAsync Error : " + err.Message, err);
                    }
                }
            }
        }
        #region Area
        //--------------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(SessionBindAreaNotify))]
        public virtual async Task rpc_area_Handle(RemoteAddress area, SessionBindAreaNotify bind)
        {
            this.remote_area_service = await this.Provider.GetAsync(new RemoteAddress(bind.areaName, bind.areaNode));
        }
        [RpcHandler(typeof(SessionUnbindAreaNotify))]
        public virtual void rpc_area_Handle(RemoteAddress area, SessionUnbindAreaNotify msg)
        {
            this.remote_area_service = null;
        }

        protected IRemoteService remote_area_service { get; private set; }
        protected readonly TypeCodec area_c2s_codec = ConnectServer.ClientCodec.Factory.GetCodec(typeof(ClientBattleAction));
        protected readonly TypeCodec area_s2s_codec = ConnectServer.ClientCodec.Factory.GetCodec(typeof(SessionBattleAction));

        protected virtual bool IsAreaProtocol(TypeCodec route_codec)
        {
            return area_c2s_codec.MessageID == route_codec.MessageID;
        }
        /// <summary>
        /// 战斗协议 ClientBattleAction 直接发往AreaService
        /// </summary>
        /// <param name="action"></param>
        public virtual void SendToArea(TypeCodec route_codec, BinaryMessage action)
        {
            try
            {
                var area = remote_area_service;
                var enter = enter_game;
                if (area != null && enter != null)
                {
                    //var c2s = this.ServerCodec.DecodeBinary(action);
                    using (var output = IOStreamObjectPool.AllocOutputAutoRelease(ConnectServer.ClientCodec.Factory))
                    {
                        output.PutUTF(enter.c2s_roleUUID);
                        output.PutRawBytes(action.Buffer, action.BufferOffset, action.BufferLength);
                        var to_area = BinaryMessage.FromBuffer(area_s2s_codec.MessageID, area_s2s_codec.MessageType, output.Buffer);
                        //var s2s = this.ServerCodec.DecodeBinary(to_area);
                        area.WormholeTransport(to_area);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
        }
        #endregion
    }

}