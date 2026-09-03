using System;
using System.Collections.Generic;
using System.Linq;
using DeepCore.Event.EventSystem.Message;

namespace DeepCore.Event.EventSystem.Events
{
    public class RemoteLocalEvent : CustomEvent
    {
        private readonly StartEventMessage mMessage;

        private bool mStopByRemote;
        public int RemoteEventID { get; private set; }

        private int mPingPongTime;
        public const int PingPongTimeoutMS = 30000;
        public const int PingPongIntervalMS = 10000;

        private bool mWaitPong;

        public RemoteLocalEvent(StartEventMessage msg)
        {
            mMessage = msg;
            mMessage.FromEvent = ID;
        }

        protected override void OnStart()
        {
            base.OnStart();
            EventManager.MessageBroker.Publish(mMessage.To, Mgr, mMessage);
            if (mMessage.IsStartEvent)
            {
                Stop(true);
            }
        }

        private void SendSyncMessage()
        {
            var syncMsg = new SyncEventStateMessage
            {
                State = State,
                From = mMessage.From,
                To = mMessage.To,
                FromEvent = ID,
                ToEvent = RemoteEventID,
                ResultReason = ResultReason
            };
            EventManager.MessageBroker.Publish(syncMsg.To, Mgr, syncMsg);
        }

        private void SendPing()
        {
            mWaitPong = true;
            var ping = new PingPongMessage
            {
                From = mMessage.From,
                To = mMessage.To,
                FromEvent = ID,
                ToEvent = RemoteEventID
            };
            //Mgr.Log($"Send ping {ID} {RemoteEventID} {ping.From}=>{ping.To}");
            EventManager.MessageBroker.Publish(ping.To, Mgr, ping);
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);
            if (mMessage.IsStartEvent)
            {
                return;
            }

            mPingPongTime = mPingPongTime + ms;
            if (RemoteEventID != 0 && !mWaitPong && mPingPongTime > PingPongIntervalMS)
            {
                SendPing();
            }
            else if (mPingPongTime > PingPongTimeoutMS)
            {
                Stop(false, "ping pong timeout");
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (!mStopByRemote && !mMessage.IsStartEvent)
            {
                if (RemoteEventID != 0)
                {
                    SendSyncMessage();
                }
                else
                {
                    var syncMsg = new ExceptionStopEventMessage
                    {
                        From = mMessage.From,
                        To = mMessage.To,
                        MessageID = mMessage.MessageID,
                        FromEvent = ID,
                        ResultReason = "lose message"
                    };
                    EventManager.MessageBroker.Publish(syncMsg.To, Mgr, syncMsg);
                }
            }
        }

        protected override void OnReceiveMessage(EventMessage msg)
        {
            base.OnReceiveMessage(msg);
            switch (msg)
            {
                case SyncEventStateMessage syncMsg:
                    {
                        //RemoteServerEvent的同步消息
                        RemoteEventID = syncMsg.FromEvent;
                        if (syncMsg.IsTrigger)
                        {
                            Trigger(syncMsg.Content);
                        }
                        else if (syncMsg.State == EventState.Failed || syncMsg.State == EventState.Successed)
                        {
                            Output = syncMsg.Content;
                            mStopByRemote = true;
                            // 延迟一帧保证Trigger能被触发
                            Stop(syncMsg.State == EventState.Successed, syncMsg.ResultReason, IsNextInvokeTrigger);
                        }

                        break;
                    }
                case PingPongMessage pong:
                    mWaitPong = false;
                    mPingPongTime = 0;
                    break;
            }
        }
    }


    /// <summary>
    /// 管理一对多，不处理lose message
    /// todo 优化， 可视情况不做心跳包逻辑
    /// </summary>
    public class RemoteMultiLocalEvent : CustomEvent
    {
        private bool mStopByRemote;
        public const int PingPongTimeoutMS = 60000;
        public const int PingPongIntervalMS = 20000;

        private class ServerEventInfo
        {
            public string RemoteAddress;
            public int EventID;
            public int PingPassTime;
            public bool WaitPong;
            public EventState State;
            public string ResultReason;
        }

        private HashMap<int, ServerEventInfo> mEvents = new HashMap<int, ServerEventInfo>();

        private StartEventMessage mMessage;
        private readonly string mTargetManagerName;

        private readonly UnionValue mConfig;

        public RemoteMultiLocalEvent(string managerName, StartEventMessage message, UnionValue config)
        {
            mMessage = message;
            mMessage.FromEvent = ID;
            mTargetManagerName = managerName;
            mConfig = config;
        }

        protected override void OnStart()
        {
            base.OnStart();
            EventManagerFactory.Instance.BroadcastMessage(mTargetManagerName, mMessage, mConfig, Mgr);
        }

        private void SendPing(ServerEventInfo to)
        {
            to.WaitPong = true;
            var ping = new PingPongMessage
            {
                From = Mgr.Address,
                To = to.RemoteAddress,
                FromEvent = ID,
                ToEvent = to.EventID
            };
            EventManager.MessageBroker.Publish(ping.To, Mgr, ping);
        }

        protected override void OnUpdate(int ms)
        {
            base.OnUpdate(ms);

            if (mMessage.IsStartEvent)
            {
                return;
            }

            var needCheckStop = false;
            foreach (var entry in mEvents)
            {
                var info = entry.Value;
                info.PingPassTime = info.PingPassTime + ms;
                if (!info.WaitPong && info.PingPassTime > PingPongIntervalMS)
                {
                    SendPing(info);
                }
                else if (info.PingPassTime > PingPongTimeoutMS)
                {
                    info.State = EventState.Failed;
                    info.ResultReason = "timeout";
                    needCheckStop = true;
                }
            }

            if (needCheckStop)
            {
                CheckStop();
            }
        }

        private void CheckStop()
        {
            var success = true;
            var reason = string.Empty;
            foreach (var entry in mEvents)
            {
                if (!IsStopedState(entry.Value.State))
                {
                    return;
                }

                if (entry.Value.State != EventState.Successed)
                {
                    success = false;
                    reason = entry.Value.ResultReason;
                }
            }

            mStopByRemote = true;
            Stop(success, reason, IsNextInvokeTrigger);
        }

        private void SendSyncMessage(ServerEventInfo info)
        {
            var syncMsg = new SyncEventStateMessage
            {
                State = State,
                From = Mgr.Address,
                To = info.RemoteAddress,
                FromEvent = ID,
                ToEvent = info.EventID,
                ResultReason = ResultReason
            };
            EventManager.MessageBroker.Publish(syncMsg.To, Mgr, syncMsg);
        }


        protected override void OnStop()
        {
            base.OnStop();
            if (!mStopByRemote && !mMessage.IsStartEvent)
            {
                foreach (var entry in mEvents)
                {
                    if (!IsStopedState(entry.Value.State))
                    {
                        SendSyncMessage(entry.Value);
                    }
                }

                //todo ExceptionStopEventMessage
                //
            }
        }

        protected override void OnReceiveMessage(EventMessage msg)
        {
            base.OnReceiveMessage(msg);
            var info = mEvents.Get(msg.FromEvent);
            switch (msg)
            {
                case SyncEventStateMessage syncMsg:
                    {
                        //RemoteServerEvent的同步消息
                        if (info == null)
                        {
                            info = new ServerEventInfo { EventID = syncMsg.FromEvent, RemoteAddress = syncMsg.From, State = EventState.Running };
                            mEvents[syncMsg.FromEvent] = info;
                        }

                        if (syncMsg.IsTrigger)
                        {
                            Trigger(syncMsg.Content);
                        }
                        else if (IsStopedState(syncMsg.State))
                        {
                            info.State = syncMsg.State;
                            CheckStop();
                        }

                        break;
                    }
                case PingPongMessage pong:
                    info.PingPassTime = 0;
                    info.WaitPong = false;
                    break;
            }
        }
    }

    public class RemoteServerEvent : CustomEvent
    {
        public readonly StartEventMessage Message;

        private bool mStopByLocal;

        private BaseEvent mSubEvent;

        public RemoteServerEvent(StartEventMessage msg)
        {
            Message = msg;
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (Mgr.RemoteAction == EventManager.RemoteActionType.Success)
            {
                Stop(true);
            }
            else if (Mgr.RemoteAction == EventManager.RemoteActionType.Fail)
            {
                Stop(false, "RemoteActionType.Fail");
            }
            else
            {
                mSubEvent = Mgr.CreateServerEntityEvent(Message.EventDesc, Message.Argument);
                if (mSubEvent == null)
                {
                    Stop(false, "invalid message");
                    return;
                }

                mSubEvent.OnEventStop += OnSubEventStop;
                BindTrigger(mSubEvent, OnSubEventTrigger);
                Do(mSubEvent);
                if (!IsStoped)
                {
                    //同步事件ID
                    SendSyncMessage(UnionValue.Null, false, ResultReason);
                }
            }
        }

        private void SendSyncMessage(UnionValue v, bool isTrigger = false, string reason = null)
        {
            if (string.IsNullOrEmpty(Message.From))
            {
                return;
            }

            var msg = new SyncEventStateMessage
            {
                State = State,
                FromEvent = ID,
                ToEvent = Message.FromEvent,
                Content = v,
                From = Mgr.Address,
                To = Message.From,
                ResultReason = reason,
                IsTrigger = isTrigger
            };
            try
            {
                EventManager.MessageBroker.Publish(msg.To, Mgr, msg);
            }
            catch (Exception e)
            {
                TryFixException(e);
            }
        }

        private UnionValue OnSubEventTrigger(BaseEvent trigger, UnionValue unionValue)
        {
            //StartEvent 不支持Trigger
            if (!Message.IsStartEvent)
            {
                SendSyncMessage(unionValue, true);
            }

            return UnionValue.Null;
        }

        private void OnSubEventStop(BaseEvent obj)
        {
            Stop(mSubEvent.State == EventState.Successed, obj.ResultReason);
        }

        private void SendPong()
        {
            var pong = new PingPongMessage
            {
                From = Mgr.Address,
                To = Message.From,
                FromEvent = ID,
                ToEvent = Message.FromEvent
            };
            EventManager.MessageBroker.Publish(pong.To, Mgr, pong);
            //Mgr.Log($"Send pong {ID} {mMessage.FromEvent} {pong.From}=>{pong.To}");
        }

        protected override void OnReceiveMessage(EventMessage msg)
        {
            base.OnReceiveMessage(msg);
            switch (msg)
            {
                case SyncEventStateMessage syncMsg:
                    {
                        if (syncMsg.State == EventState.Failed || syncMsg.State == EventState.Successed)
                        {
                            //stop by local
                            mStopByLocal = true;
                            mSubEvent.Stop(syncMsg.State == EventState.Successed, syncMsg.ResultReason);
                        }

                        break;
                    }
                case PingPongMessage ping:
                    SendPong();
                    break;
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (!mStopByLocal)
            {
                if (Mgr.RemoteAction != EventManager.RemoteActionType.Doing)
                {
                    SendSyncMessage(UnionValue.Null, false, ResultReason);
                }
                else if (mSubEvent != null)
                {
                    SendSyncMessage(mSubEvent.Output, false, ResultReason);
                }
                else
                {
                    SendSyncMessage(UnionValue.Null, false, "invalid message");
                }
            }
        }
    }
}