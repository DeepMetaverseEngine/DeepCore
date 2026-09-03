using DeepCore.Components;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Slave.Layer
{
    partial class LayerZoneObject
    {

        internal protected abstract void DoEvent(ObjectNotify e);

        internal void InternalSyncObject(SyncObjectInfo sync)
        {
            this.ForceFaceTo(sync.direction, sync.body_direction);
            this.ForceSyncPos(sync.pos);
        }
        internal void InternalSyncObject(ObjectForceSyncPosEvent sync)
        {
            this.ForceFaceTo(sync.Direction, sync.BodyDirection);
            this.ForceSyncPos(sync.Pos);
        }
        internal void InternalSyncObject(ObjectForceSyncFaceEvent sync)
        {
            this.ForceFaceTo(sync.Direction, sync.BodyDirection);
        }

        protected virtual void DoForceSyncFaceEvent(ObjectForceSyncFaceEvent e)
        {
            this.mDirection.FaceTo(e.Direction);
            this.mDirection.BodyTo(e.BodyDirection);
        }
        protected virtual void DoForceSyncPosEvent(ObjectForceSyncPosEvent e)
        {
            this.mRemotePos.X = e.Pos.X;
            this.mRemotePos.Y = e.Pos.Y;
            this.mRemotePos.Z = e.Pos.Z;
            this.mDirection.FaceTo(e.Direction);
            this.mDirection.BodyTo(e.BodyDirection);
        }

        //-------------------------------------------------------------------------------------

        private OnDoEventHandler mOnDoEvent;
        public delegate void OnDoEventHandler(LayerZoneObject obj, ObjectNotify e);
        [EventTriggerDescAttribute("单位接收到服务端事件")]
        public event OnDoEventHandler OnMessageReceived { add { mOnDoEvent += value; } remove { mOnDoEvent -= value; } }
        internal void cb_OnDoEvent(ObjectNotify e)
        {
            mOnDoEvent?.Invoke(this, e);
        }
        //-------------------------------------------------------------------------------------
    }


}
