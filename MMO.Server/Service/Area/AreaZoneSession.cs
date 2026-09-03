//#define SHOW_SEND_MESSAGE

using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.GameData;
using DeepCore.GameData.Zone;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using Gate.Data.Protocol;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Gate.Server.Service.Area
{
    public class AreaZoneSession : Disposable, IZoneNodeSession
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(AreaZoneSession));
        protected readonly Logger log;
        protected readonly AreaService service;
        protected readonly AreaZoneNode node;
        protected readonly int client_event_route;
        protected IRemoteService remote_session;
        protected IRemoteService remote_logic;
        public readonly RoleEnterZoneRequest enter;
        private HashMap<string, object> mAttributes = new HashMap<string, object>();
        private bool pause_client = false;
        private bool pause_logic = false;

        public AreaZoneNode ZoneNode { get { return node; } }
        public string RoleUUID { get { return enter.roleUUID; } }
        public string RoleSessionName { get { return enter.roleSessionName; } }
        public string ZoneUUID { get { return node.ZoneUUID; } }
        protected InstancePlayer Actor { get { return mBinding.Value.Actor; } }

        public AreaZoneSession(AreaService svc, AreaZoneNode node, RoleEnterZoneRequest enter)
        {
            Alloc.RecordConstructor(this.GetType());
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.service = svc;
            this.node = node;
            this.enter = enter;
            this.pause_client = enter.IsDisconnect;
            this.pause_logic = enter.IsDisconnect;
            this.client_event_route = TypeCodec.GetAttributeRoute(typeof(ClientBattleEvent));
        }
        ~AreaZoneSession()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
        }
        internal async Task<bool> OnEnterAsync()
        {
            this.remote_session = await service.Provider.GetAsync(new RemoteAddress(enter.roleSessionName, enter.roleSessionNode));
            this.remote_logic = await service.Provider.GetAsync(new RemoteAddress(enter.roleLogicName, enter.roleLogicNode));
            return remote_session != null && remote_logic != null;
        }
        public virtual void SessionDisconnect()
        {
            this.remote_session.WormholeTransport(new ClientLeaveZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
            });
            this.remote_session.Invoke(new SessionUnbindAreaNotify()
            {
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode
            });
            this.pause_client = true;
        }
        public virtual void SessionBeginLeave()
        {
            this.remote_session.WormholeTransport(new ClientLeaveZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
            });
            this.pause_logic = true;
        }

        public virtual void SessionReconnect()
        {
            this.pause_client = false;
            this.pause_logic = false;
            this.remote_session?.WormholeTransport(new ClientEnterZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
                s2c_ZoneTemplateID = node.ZoneTemplateID,
                s2c_RoleDisplayName = enter.roleDisplayName,
                s2c_RoleUnitTemplateID = enter.roleUnitTemplateID,
                s2c_SceneLineIndex = enter.expectLineIndex,
                s2c_ZoneUpdateIntervalMS = ZoneNode.ZoneNode.ClientUpdateIntervalMS,
                s2c_Ext = enter.ext,
            });
            this.remote_session?.Invoke(new SessionBindAreaNotify()
            {
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode
            });
        }
        public virtual void SendClientEnterZoneNotify()
        {
            if (pause_client) return;
            this.remote_session?.WormholeTransport(new ClientEnterZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
                s2c_ZoneTemplateID = node.ZoneTemplateID,
                s2c_RoleDisplayName = enter.roleDisplayName,
                s2c_RoleUnitTemplateID = enter.roleUnitTemplateID,
                s2c_SceneLineIndex = enter.expectLineIndex,
                s2c_ZoneUpdateIntervalMS = ZoneNode.ZoneNode.ClientUpdateIntervalMS,
                s2c_Ext = enter.ext,
            });
            this.remote_session?.Invoke(new SessionBindAreaNotify()
            {
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode
            });
        }
        public virtual void DoGameOver(GameOverEvent evt)
        {
            this.remote_logic?.Invoke(new AreaGameOverNotify()
            {
                zoneUUID = node.ZoneUUID,
                zoneTemplateID = node.ZoneTemplateID,
                winForce = evt.WinForce,
                message = evt.message,
            });
        }

        public virtual void SendToSession(ISerializable msg)
        {
            this.remote_session?.Invoke(msg);
        }

        //---------------------------------------------------------------------------------------------
        #region ZoneEvents

        protected virtual void Actor_OnTransportScene(InstancePlayer player, InstanceFlag flag, int nextSceneID, string nextScenePosition)
        {
            this.remote_logic?.Invoke(new RoleNeedTransportNotify()
            {
                fromAreaName = service.SelfAddress.ServiceName,
                fromAreaNode = service.SelfAddress.ServiceNode,
                nextZoneID = nextSceneID,
                nextMapID = nextSceneID,
                nextZoneFlagName = nextScenePosition,
            });
        }


        #endregion
        //---------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------
        #region RPC

        //         /// <summary>
        //         /// ClientBattleAction
        //         /// </summary>
        //         /// <param name="msg"></param>
        //         public virtual void client_rpc_Handle(Stream clientBattleAction)
        //         {
        //             try
        //             {
        //                 object action;
        //                 // Drop 4 for head message id //
        //                 clientBattleAction.Position += 4;
        //                 if (service.BattleClientCodec.doDecode(clientBattleAction, out action))
        //                 {
        //                     OnHandleClientMessage(action as IMessage);
        //                 }
        //             }
        //             catch (Exception err)
        //             {
        //                 log.Error(err.Message, err);
        //             }
        //         }
        //         public virtual void client_rpc_Handle(ArraySegment<byte> clientBattleAction)
        //         {
        //             try
        //             {
        //                 object action;
        //                 // Drop 4 for head message id //
        //                 //clientBattleAction.Position += 4;
        //                 if (service.BattleClientCodec.doDecode(new ArraySegment<byte>(clientBattleAction.Array, clientBattleAction.Offset + 4, clientBattleAction.Count - 4), out action))
        //                 {
        //                     OnHandleClientMessage(action as IMessage);
        //                 }
        //             }
        //             catch (Exception err)
        //             {
        //                 log.Error(err.Message, err);
        //             }
        //         }
        public virtual void client_rpc_Handle(SessionBattleAction msg)
        {
            try
            {
                if (service.BattleClientCodec.DoDecode(new ArraySegment<byte>(msg.clientBattleAction), out var action))
                {
                    client_rpc_OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public virtual void client_rpc_Handle(ClientBattleAction msg)
        {
            try
            {
                if (service.BattleClientCodec.DoDecode(new ArraySegment<byte>(msg.c2s_battleAction), out var action))
                {
                    client_rpc_OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        private void client_rpc_OnHandleClientMessage(IMessage action)
        {
            if (mHandleClientMessage != null)
            {
                mHandleClientMessage.Invoke(action);
            }
            else
            {
                node.ZoneNode.QueueSceneTask((z, err) =>
                {
                    if (err != null)
                    {
                        log.Error(err.Message, err);
                    }
                    else if (mHandleClientMessage != null)
                    {
                        mHandleClientMessage.Invoke(action);
                    }
                });
            }
        }

        public virtual void logic_rpc_Handle(ISerializable msg)
        {
            if (mRpcHandleInvoke != null)
                mRpcHandleInvoke(msg);
        }
        public virtual void logic_rpc_Handle(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
            if (mRpcHandleCall != null)
                mRpcHandleCall(msg, (rsp, err) => { cb(rsp as ISerializable, err); });
        }
        public virtual void logic_rpc_Invoke(ISerializable msg)
        {
            if (pause_logic) return;
            lock (mLogicSendingQueue) { mLogicSendingQueue.Add(msg); }
        }
        public virtual void logic_rpc_Call(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
            this.remote_logic?.Call<ISerializable>(msg, cb);
        }

        #endregion
        //---------------------------------------------------------------------------------------------
        #region  IZoneNodeSession
        //---------------------------------------------------------------------------------------------

        private AtomicReference<ZoneNode.PlayerClient> mBinding = new AtomicReference<ZoneNode.PlayerClient>(null);
        private PackNotify mSendingQueue = new PackNotify();
        private List<ISerializable> mLogicSendingQueue = new List<ISerializable>();


        string IZoneNodeSession.PlayerUUID { get { return enter.roleUUID; } }
        string IZoneNodeSession.DisplayName { get { return enter.roleDisplayName; } }
        ZoneNode.PlayerClient IZoneNodeSession.BindingPlayer
        {
            get { return mBinding.Value; }
            set { mBinding.Value = value; }
        }


        void IZoneNodeSession.ClientSend(PlayerMessageEntry msg, bool imm)
        {
            if (pause_client) return;
            if (msg.buffer != null)
            {
                mSendingQueue.events.Add(msg.buffer);
            }
            else
            {
                mSendingQueue.events.Add(msg.message);
            }
        }
        void IZoneNodeSession.ClientFlush(BattleCodec codec)
        {
            try
            {
                if (pause_logic == false)
                {
                    lock (mLogicSendingQueue)
                    {
                        if (mLogicSendingQueue.Count > 0)
                        {
                            remote_logic?.BatchInvoke(mLogicSendingQueue);
                            mLogicSendingQueue.Clear();
                        }
                    }
                }
                if (pause_client == false)
                {
                    if (mSendingQueue.events.Count > 0)
                    {
                        mSendingQueue.sequenceNo = node.ZoneNode.ZoneTick;
                        using (var buffer = new DeepCore.IO.MemoryStream())
                        {
                            try
                            {
                                if (codec.doEncodeTo(mSendingQueue, buffer))
                                {
                                    var notify = BinaryMessage.FromBuffer(client_event_route, typeof(ClientBattleEvent), buffer);
                                    remote_session?.WormholeTransport(notify);
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                            finally
                            {
                                mSendingQueue.events.Clear();
                            }
                        }

                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        void IZoneNodeSession.OnPlayerConnected(ZoneNode.PlayerClient binding)
        {
            //pause_client = false;
            mBinding.Value = binding;
            binding.Actor.OnTransportScene += Actor_OnTransportScene;
        }

        void IZoneNodeSession.OnPlayerDisconnect(ZoneNode.PlayerClient binding)
        {
            mBinding.Value = null;
        }
        void IZoneNodeSession.OnPlayerDisposed()
        {
            this.pause_client = true;
            this.pause_logic = true;
        }
        void IZoneNodeSession.PostToGameServer(object msg)
        {
            this.logic_rpc_Invoke(msg as ISerializable);
        }
        void IZoneNodeSession.PostToGameServer(object msg, Action<object, Exception> callback)
        {
            this.logic_rpc_Call(msg as ISerializable, (rsp, err) => { callback(rsp, err); });
        }

        private ClientMessageHandler mHandleClientMessage;
        private GameServerCallHandler mRpcHandleCall;
        private GameServerMessageHandler mRpcHandleInvoke;
        event ClientMessageHandler IZoneNodeSession.HandleClientMessage
        {
            add { mHandleClientMessage += value; }
            remove { mHandleClientMessage -= value; }
        }
        event GameServerMessageHandler IZoneNodeSession.HandleGameServerMessage
        {
            add { mRpcHandleInvoke += value; }
            remove { mRpcHandleInvoke -= value; }
        }
        event GameServerCallHandler IZoneNodeSession.HandleGameServerCall
        {
            add { mRpcHandleCall += value; }
            remove { mRpcHandleCall -= value; }
        }
        #endregion
        //---------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------
    }

}
