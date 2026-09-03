using DeepCore;
using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Channel.Host;
using DeepCore.Meta.Layout;
using System.Collections.Generic;

namespace DeepCore.Meta.Channel.Host
{
    partial class ChannelNode
    {
        public ObserverMap Observers { get => _observers; }
        private ObserverMap _observers;
        private ArrayList<IChannelS2C> _pendingChannelMessage = new ArrayList<IChannelS2C>();
        public class ObserverMap : MetaObjectMap<string, ChannelObserver>
        {

        }


        public ChannelObserver AddObserver(AddChannelObserver add)
        {
            var create = _observers.TryCreateOrGet(add.uuid, out var obj, uuid => CreateWorldObserver(add));
            obj.InternalEnter(add);
            if (create)
            {
                InvokeObserverEnter(obj, add);
            }
            var enter = new ObserverEnterChannelS2C();
            enter.channelID = this.ChannelID;
            enter.channelInfo = this.Info;
            enter.objects = new List<ObjectEnterS2C>(_agents.Count);
            _agentsUUID.ForEachChildren(a =>
            {
                enter.objects.Add(new ObjectEnterS2C()
                {
                    uuid = a.UUID,
                    objectID = a.ObjectID,
                    sync = a.GetObjectStateS2C(),
                });
            });
            obj.PostSessionS2C(enter);
            return obj;
        }
        public ChannelObserver RemoveWorldObserver(string uuid)
        {
            if (_observers.TryRemoveChild(uuid, out var obj))
            {
                obj.InternalLeave();
                InvokeObserverLeave(obj);
                obj.PostSessionS2C(new ObserverLeaveChannelS2C() { channelID = this.ChannelID });
                return obj;
            }
            return null;
        }

        public void Broadcast(ISerializable msg)
        {
            _observers.ForEachChildren(obj => obj.PostSessionS2C(msg));
        }
        public void QueueChannelMessage(IChannelS2C sync)
        {
            _pendingChannelMessage.Add(sync);
        }
        private void FlushChannelMessage()
        {
            if (_pendingChannelMessage.Count > 0)
            {
                var update = new ChannelPostObserverS2C();
                update.channelID = this.ChannelID;
                update.messages = new List<IChannelS2C>();
                foreach (var c in _pendingChannelMessage)
                {
                    update.messages.Add(c);
                }
                _pendingChannelMessage.Clear();
                _observers.ForEachChildren(obj => obj.PostSessionS2C(update));
            }
        }




    }


}
