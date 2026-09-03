using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using DeepCore.Threading;
using System.Threading.Tasks;
using DeepCore.Geometry;
using DeepCore.VoxelWorld.Message;

namespace DeepCore.VoxelWorld.VoxelClient
{
    public partial class SCVoxelWorld
    {
        private HashMap<string, SCVoxelObject> m_Objects = new HashMap<string, SCVoxelObject>();
        protected virtual void InitListenObjects()
        {
            this.Adapter.OnActorEnter += InvokeWrapAction<ActorEnter>(VoxelWorld_OnActorEnter);
            this.Adapter.OnActorLeave += InvokeWrapAction<ActorLeave>(VoxelWorld_OnActorLeave);
            this.Adapter.OnObjectEnter += InvokeWrapAction<ObjectEnter>(VoxelWorld_OnObjectEnter);
            this.Adapter.OnObjectLeave += InvokeWrapAction<ObjectLeave>(VoxelWorld_OnObjectLeave);
        }
        protected void UpdateObjects()
        {
            foreach (var o in this.m_Objects.Values)
            {
                o.Update();
            }
        }
        private void VoxelWorld_OnObjectEnter(ObjectEnter obj)
        {
            var scobj = new SCVoxelObject(this, obj);
            if (m_Objects.TryGetOrCreate(obj.OID, out var old, oid => scobj))
            {
                event_OnObjectLeave?.Invoke(scobj);
            }
            event_OnObjectEnter?.Invoke(scobj);
        }
        private void VoxelWorld_OnObjectLeave(ObjectLeave obj)
        {
            var scobj = m_Objects.RemoveByKey(obj.OID);
            if (scobj != null)
            {
                event_OnObjectLeave?.Invoke(scobj);
            }
        }
        private void VoxelWorld_OnActorEnter(ActorEnter obj)
        {
        }
        private void VoxelWorld_OnActorLeave(ActorLeave obj)
        {
        }

        public delegate void SCObjectEnter(SCVoxelObject obj);
        public delegate void SCObjectLeave(SCVoxelObject obj);
        public event SCObjectEnter OnObjectEnter { add { event_OnObjectEnter += value; } remove { event_OnObjectEnter -= value; } }
        public event SCObjectLeave OnObjectLeave { add { event_OnObjectLeave += value; } remove { event_OnObjectLeave -= value; } }
        private SCObjectEnter event_OnObjectEnter;
        private SCObjectLeave event_OnObjectLeave;

    }
}
