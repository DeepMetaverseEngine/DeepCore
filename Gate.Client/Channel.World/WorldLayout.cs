using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Slave;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using World.Slave.Layer;

namespace Gate.Client.Channel.World
{
    public class WorldLayout : ChannelLayout
    {
        public WorldActor Actor { get; private set; }
        public WorldLayout(IClientAdapter adapter) : base(adapter)
        {
            adapter.Listen<ClientEnterChannelNotify>(InvokeClientEnter);
            adapter.Listen<ClientLeaveChannelNotify>(InvokeClientLeave);
        }
        protected override void InvokeActorEnter(ChannelActor obj)
        {
            this.Actor = obj as WorldActor;
            base.InvokeActorEnter(obj);
        }
        protected override ChannelAgent CreateChannelObject(ChannelNode channel, ObjectEnterS2C enter)
        {
            return new WorldAgent(enter.uuid);
        }
        protected override ChannelActor CreateActor(ActorEnterChannelS2C enter)
        {
            return new WorldActor(enter.uuid);
        }
        //--------------------------------------------------------------------------------------------------
        #region Events

        public delegate void ClientEnterHandler(ChannelLayout layout);
        public delegate void ClientLeaveHandler(ChannelLayout layout);
        public event ClientEnterHandler OnClientEnter { add { event_OnChannelEnter += value; } remove { event_OnChannelEnter -= value; } }
        public event ClientLeaveHandler OnClientLeave { add { event_OnChannelLeave += value; } remove { event_OnChannelLeave -= value; } }
        private ClientEnterHandler event_OnChannelEnter;
        private ClientLeaveHandler event_OnChannelLeave;
        protected virtual void InvokeClientEnter(ClientEnterChannelNotify enter)
        {
            event_OnChannelEnter?.Invoke(this);
        }
        protected virtual void InvokeClientLeave(ClientLeaveChannelNotify leave)
        {
            event_OnChannelLeave?.Invoke(this);
        }
        protected override void OnDisposingEvents()
        {
            base.OnDisposingEvents();
            event_OnChannelEnter = null;
            event_OnChannelLeave = null;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------
    }
}
