using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Host
{
    public partial class ChannelNode : MetaStage
    {
        public readonly int ChannelID;
        public readonly ChannelInfo Info;
        public ChannelNode(ChannelInfo info, int channelID)
        {
            this.ChannelID = channelID;
            this.Info = info;
            this._observers = AddObject(new ObserverMap());
            this._agentsUUID = AddObject(new ObjectMap());
        }
        protected virtual ChannelAgent CreateWorldAgent(AddChannelAgent add)
        {
            return new ChannelAgent(add.uuid, this);
        }
        protected virtual ChannelObserver CreateWorldObserver(AddChannelObserver add)
        {
            return new ChannelObserver(add.uuid, this);
        }
        protected override void Disposing()
        {
            base.Disposing();
        }

        protected override void OnEndUpdate(float intervalMS)
        {
            base.OnEndUpdate(intervalMS);
            this.FlushChannelMessage();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        #region Events


        public delegate void ObjectEnter(ChannelObject obj, AddChannelAgent add);
        public delegate void ObjectLeave(ChannelObject obj);
        public delegate void ObserverEnter(ChannelObserver obj, AddChannelObserver add);
        public delegate void ObserverLeave(ChannelObserver obj);
        public event ObjectEnter OnObjectEnter { add { event_OnObjectEnter += value; } remove { event_OnObjectEnter -= value; } }
        public event ObjectLeave OnObjectLeave { add { event_OnObjectLeave += value; } remove { event_OnObjectLeave -= value; } }
        public event ObserverEnter OnObserverEnter { add { event_OnObserverEnter += value; } remove { event_OnObserverEnter -= value; } }
        public event ObserverLeave OnObserverLeave { add { event_OnObserverLeave += value; } remove { event_OnObserverLeave -= value; } }
        private ObjectEnter event_OnObjectEnter;
        private ObjectLeave event_OnObjectLeave;
        private ObserverEnter event_OnObserverEnter;
        private ObserverLeave event_OnObserverLeave;
        protected virtual void InvokeObjectEnter(ChannelObject obj, AddChannelAgent add) { event_OnObjectEnter?.Invoke(obj, add); }
        protected virtual void InvokeObjectLeave(ChannelObject obj) { event_OnObjectLeave?.Invoke(obj); }
        protected virtual void InvokeObserverEnter(ChannelObserver obj, AddChannelObserver add) { event_OnObserverEnter?.Invoke(obj, add); }
        protected virtual void InvokeObserverLeave(ChannelObserver obj) { event_OnObserverLeave?.Invoke(obj); }
        protected override void OnDisposingEvents()
        {
            base.OnDisposingEvents();
            this.event_OnObjectEnter = null;
            this.event_OnObjectLeave = null;
            this.event_OnObserverEnter = null;
            this.event_OnObserverLeave = null;
        }
        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------
    }


}
