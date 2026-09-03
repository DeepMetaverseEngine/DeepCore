using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;
using DeepCore.Protocol;
using System;

namespace DeepCore.Meta.Channel.Slave
{
    public class ChannelLayout : MetaStage
    {
        public IClientAdapter Adapter { get; }
        public ChannelLayout(IClientAdapter adapter)
        {
            this.Adapter = adapter;
            this._worldChannels = this.AddObject(new MetaObjectMap<int, ChannelNode>());
            this._worldActors = this.AddObject(new MetaObjectMap<string, ChannelActor>());
            adapter.Listen<ActorEnterChannelS2C>(handle_ActorEnterWorldS2C);
            adapter.Listen<ActorLeaveChannelS2C>(handle_ActorLeaveWorldS2C);
            adapter.Listen<ObserverEnterChannelS2C>(handle_ObserverEnterChannelS2C);
            adapter.Listen<ObserverLeaveChannelS2C>(handle_ObserverLeaveChannelS2C);
            adapter.Listen<ChannelPostObserverS2C>(handle_ChannelPostObserverS2C);
        }
        protected override void Disposing()
        {
            base.Disposing();
        }
        protected override void OnUpdate(float intervalMS)
        {
            base.OnUpdate(intervalMS);
        }
        

        //--------------------------------------------------------------------------------------------------
        protected virtual ChannelNode CreateChannelNode(ObserverEnterChannelS2C enter)
        {
            return new ChannelNode(enter);
        }
        protected virtual ChannelAgent CreateChannelObject(ChannelNode channel, ObjectEnterS2C enter)
        {
            return new ChannelAgent(enter.uuid);
        }
        protected virtual ChannelActor CreateActor(ActorEnterChannelS2C enter)
        {
            return new ChannelActor(enter.uuid);
        }

        //--------------------------------------------------------------------------------------------------
        #region NetMessage
      
        protected virtual void handle_ActorEnterWorldS2C(ActorEnterChannelS2C ntf)
        {
            DoActorEnterChannelS2C(ntf);
        }
        protected virtual void handle_ActorLeaveWorldS2C(ActorLeaveChannelS2C ntf)
        {
            DoActorLeaveChannelS2C(ntf);
        }
        protected virtual void handle_ObserverEnterChannelS2C(ObserverEnterChannelS2C ntf)
        {
            DoObserverEnterChannelS2C(ntf);
        }
        protected virtual void handle_ObserverLeaveChannelS2C(ObserverLeaveChannelS2C ntf)
        {
            DoObserverLeaveChannelS2C(ntf);
        }
        protected virtual void handle_ChannelPostObserverS2C(ChannelPostObserverS2C update)
        {
            if (TryGetChannel(update.ChannelID, out var _channel))
            {
                if (update.messages != null)
                {
                    foreach (var msg in update.messages)
                    {
                        handle_ChannelUpdate(_channel, msg);
                    }
                }
            }
            else
            {
                log.Warn($"Can Not Find Channel:{update.ChannelID}");
            }
        }
        protected virtual void handle_ChannelUpdate(ChannelNode channel, IChannelS2C notify)
        {
            if (notify is IObjectMessage obj)
            {
                if (channel.TryGetChild(obj.ObjectID, out var _object))
                {
                    handle_AgentMessage(_object, obj);
                }
                else
                {
                    log.Warn($"Can Not Find Object:{obj.ObjectID} Handler Type:{notify.GetType()} Message:{notify}");
                }
            }
            else if (notify is ObjectEnterS2C enter)
            {
                DoObjectEnterS2C(channel, enter);
            }
            else if (notify is ObjectLeaveS2C leave)
            {
                DoObjectLeaveS2C(channel, leave);
            }
            else
            {
                log.Warn($"No Handler Mesage :{notify}");
            }
        }
        protected virtual void handle_ChannelMessage(ChannelNode channel, IChannelMessage notify)
        {
            channel.InternalHandleChannelMessage(notify);
        }
        protected virtual void handle_AgentMessage(ChannelAgent agent, IObjectMessage notify)
        {
            if (!agent.InternalHandleObjectMessage(notify))
            {
                log.Warn($"No Handler Object Mesage :{notify}");
            }
        }


        #endregion
        //--------------------------------------------------------------------------------------------------
        #region Channels

        protected MetaObjectMap<int, ChannelNode> _worldChannels;

        protected virtual void DoObserverEnterChannelS2C(ObserverEnterChannelS2C enter)
        {
            var channel = CreateChannelNode(enter);
            if (_worldChannels.AddChild(enter.channelID, channel))
            {
                if (enter.objects != null)
                {
                    foreach (var obj in enter.objects)
                    {
                        this.DoObjectEnterS2C(channel, obj);
                    }
                }
            }
            else
            {
                throw new Exception();
            }
        }
        protected virtual void DoObserverLeaveChannelS2C(ObserverLeaveChannelS2C leave)
        {
            if (_worldChannels.TryRemoveChild(leave.channelID, out var channel))
            {
                channel.ForEachChildren<ChannelAgent>(a =>
                {
                    RemoveAgent(a);
                    a.Dispose();
                });
                channel.Dispose();
            }
            else
            {
                throw new Exception();
            }
        }
        public bool TryGetChannel(int channelID, out ChannelNode obj)
        {
            return _worldChannels.TryGetChild(channelID, out obj);
        }
        public void ForEachChannel(Action<ChannelNode> action)
        {
            _worldChannels.ForEachChildren(action);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------
        #region ObjectsUUID

        protected HashMap<string, ChannelAgent> _worldObjects = new HashMap<string, ChannelAgent>();
        protected virtual void DoObjectEnterS2C(ChannelNode channel, ObjectEnterS2C enter)
        {
            var agent = CreateChannelObject(channel, enter);
            if (channel.DoObjectEnterS2C(enter, agent))
            {
                AddAgent(agent);
            }
            else
            {
                log.Warn($"Can Not Add Object: {enter.uuid}");
            }
        }
        protected virtual void DoObjectLeaveS2C(ChannelNode channel, ObjectLeaveS2C leave)
        {
            if (channel.DoObjectLeaveS2C(leave, out var agent))
            {
                RemoveAgent(agent);
            }
            else
            {
                log.Warn($"Can Not Find Object:{leave.objectID} Handler Type:{leave.GetType()} Message:{leave}");
            }
        }
        protected void AddAgent(ChannelAgent agent)
        {
            _worldObjects.Add(agent.UUID, agent);
            InvokeObjectEnter(agent);
            if (TryGetActor(agent.UUID, out var actor))
            {
                actor.InternalBindAgent(agent);
            }
        }
        protected bool RemoveAgent(ChannelAgent agent)
        {
            if (_worldObjects.Remove(agent.UUID))
            {
                InvokeObjectLeave(agent);
                if (TryGetActor(agent.UUID, out var actor))
                {
                    actor.InternalBindAgent(agent);
                }
                return true;
            }
            return false;
        }
        public bool TryGetAgent(string uuid, out ChannelAgent agent)
        {
            return _worldObjects.TryGetValue(uuid, out agent);
        }
        public ChannelAgent GetAgent(string uuid)
        {
            return _worldObjects.Get(uuid);
        }
        public void ForEachWorldObject(Action<ChannelAgent> action)
        {
            using (var alloc = ObjectPool.AllocList<ChannelAgent>(_worldObjects.Values))
            {
                foreach (var obj in alloc)
                {
                    action(obj);
                }
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
        #region Actor
        protected MetaObjectMap<string, ChannelActor> _worldActors;

        protected void DoActorEnterChannelS2C(ActorEnterChannelS2C enter)
        {
            if (_worldActors.TryGetChild(enter.uuid, out var old))
            {
                old.RemoveFromParent();
                old.Dispose();
            }
            var actor = CreateActor(enter);
            if (_worldActors.AddChild(enter.uuid, actor))
            {
                actor.InternalEnter(this, enter);
                InvokeActorEnter(actor);
            }
        }
        protected void DoActorLeaveChannelS2C(ActorLeaveChannelS2C leave)
        {
            if (_worldActors.TryGetChild(leave.uuid, out var old))
            {
                old.InternalLeave(this, leave);
                InvokeActorLeave(old);
                old.RemoveFromParent();
                old.Dispose();
            }
        }
        public bool TryGetActor(string uuid, out ChannelActor obj)
        {
            return _worldActors.TryGetChild(uuid, out obj);
        }
        public void ForEachActor(Action<ChannelActor> action)
        {
            _worldActors.ForEachChildren(action);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------
        #region Events




        public delegate void ObjectEnterHandler(ChannelAgent agent);
        public delegate void ObjectLeaveHandler(ChannelAgent agent);
        public delegate void ActorEnterHandler(ChannelActor actor);
        public delegate void ActorLeaveHandler(ChannelActor actor);
        public event ObjectEnterHandler OnObjectEnter { add { event_OnObjectEnter += value; } remove { event_OnObjectEnter -= value; } }
        public event ObjectLeaveHandler OnObjectLeave { add { event_OnObjectLeave += value; } remove { event_OnObjectLeave -= value; } }
        public event ActorEnterHandler OnActorEnter { add { event_OnActorEnter += value; } remove { event_OnActorEnter -= value; } }
        public event ActorLeaveHandler OnActorLeave { add { event_OnActorLeave += value; } remove { event_OnActorLeave -= value; } }

        private ObjectEnterHandler event_OnObjectEnter;
        private ObjectLeaveHandler event_OnObjectLeave;
        private ActorEnterHandler event_OnActorEnter;
        private ActorLeaveHandler event_OnActorLeave;

        protected virtual void InvokeObjectEnter(ChannelAgent obj)
        {
            event_OnObjectEnter?.Invoke(obj);
        }
        protected virtual void InvokeObjectLeave(ChannelAgent obj)
        {
            event_OnObjectLeave?.Invoke(obj);
        }
        protected virtual void InvokeActorEnter(ChannelActor obj)
        {
            event_OnActorEnter?.Invoke(obj);
        }
        protected virtual void InvokeActorLeave(ChannelActor obj)
        {
            event_OnActorLeave?.Invoke(obj);
        }
        protected override void OnDisposingEvents()
        {
            base.OnDisposingEvents();
            event_OnObjectLeave = null;
            event_OnObjectEnter = null;
            event_OnActorEnter = null;
            event_OnActorLeave = null;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
    }





}
