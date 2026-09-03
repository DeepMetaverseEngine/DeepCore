using DeepCore.Meta.Channel.Data;
using DeepCore.Meta.Layout;

namespace DeepCore.Meta.Channel.Host
{
    partial class ChannelNode
    {
        public ObjectMap Agents { get => _agentsUUID; }
        private ObjectMap _agentsUUID;
        private HashMap<uint, ChannelAgent> _agents = new HashMap<uint, ChannelAgent>();
        private uint _agentIDIndexer = 0;

        public class ObjectMap : MetaObjectMap<string, ChannelAgent>
        {

        }
        internal uint GenChannelAgentID()
        {
            var id = ++_agentIDIndexer;
            while (_agents.ContainsKey(id) || id == 0)
            {
                id = ++_agentIDIndexer;
            }
            return id;
        }

        public bool TryGetChannelAgent(string uuid, out ChannelAgent obj)
        {
            return _agentsUUID.TryGetChild(uuid, out obj);
        }
        public bool TryGetChannelAgent(uint objectID, out ChannelAgent obj)
        {
            return _agents.TryGetValue(objectID, out obj);
        }
        public ChannelAgent GetChannelAgent(string uuid)
        {
            return _agentsUUID.GetChild(uuid);
        }
        public ChannelAgent GetChannelAgent(uint objectID)
        {
            return _agents.Get(objectID);
        }
        public ChannelAgent AddChannelAgent(AddChannelAgent add)
        {
            if (_agentsUUID.TryCreateOrGet(add.uuid, out var obj, u => CreateWorldAgent(add)))
            {
                _agents.Add(obj.ObjectID, obj);
                obj.InternalEnter(add);
                InvokeObjectEnter(obj, add);
                QueueChannelMessage(new ObjectEnterS2C()
                {
                    uuid = obj.UUID,
                    objectID = obj.ObjectID,
                    sync = obj.GetObjectStateS2C(),
                });
            }
            else
            {
                obj.InternalEnter(add);
            }
            obj.PostSessionS2C(new ActorEnterChannelS2C()
            {
                uuid = obj.UUID,
                channelID = this.ChannelID,
                objectID = obj.ObjectID,
                sync = obj.GetObjectStateS2C(),
            });
            return obj;
        }
        public ChannelAgent RemoveChannelAgent(string uuid)
        {
            if (_agentsUUID.TryRemoveChild(uuid, out var obj))
            {
                _agents.Remove(obj.ObjectID);
                obj.InternalLeave();
                InvokeObjectLeave(obj);
                obj.Dispose();
                QueueChannelMessage(new ObjectLeaveS2C()
                {
                    objectID = obj.ObjectID
                });
                obj.PostSessionS2C(new ActorLeaveChannelS2C()
                {
                    channelID = this.ChannelID,
                    objectID = obj.ObjectID,
                    uuid = obj.UUID,
                });
            }
            return obj;
        }

//         public void HandleObjectMessage(SessionPostChannelC2S post)
//         {
//             if (TryGetChannelAgent(post.playerUUID, out var agent))
//             {
//                 agent.HandleSessionPostChannelC2S(post);
//             }
//         }


    }


}
