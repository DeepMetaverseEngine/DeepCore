using DeepCore.Components;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Slave.Layer
{
    public abstract partial class LayerZoneObject : LayerObject, IZoneObject
    {
        IZone IZoneObject.Zone => Parent;
        bool IZoneObject.Enable => IsEnable;
        float IZoneObject.BodySize => RadiusSize;


        /// <summary>
        /// 单位ID
        /// </summary>
        public uint ObjectID { get { return ObjID; } }
        public abstract int TemplateID { get; }
        /// <summary>
        /// 是否在场景里
        /// </summary>
        public bool IsEnable { get; private set; }
        public virtual bool TouchObj { get { return false; } }
        public virtual bool TouchMap { get { return false; } }
        sealed public override float Direction { get { return mDirection.Direction; } }
        sealed public override float BodyDirection { get { return mDirection.BodyDirection; } }
        //-------------------------------------------------------------------------------------

        private uint ObjID;
        private Geometry.Vector3 mLastPos = new Geometry.Vector3();
        private LayerObjectComponentCollection _components;
        protected ObjectDirection mDirection;

        protected virtual LayerZoneObject Init(uint objectID, LayerZone parent)
        {
            base.Init(parent);
            this.IsEnable = true;
            this.ObjID = objectID;
            //this._components = createComponents();
            return this;
        }
        internal void OnRemove()
        {
            IsEnable = false;
        }
        protected override void Disposing()
        {
            base.Disposing();
            IsEnable = false;
            mOnDoEvent = null;
            if (_components != null)
            {
                //_components.ForEach(0, static (st, c) => c.Dispose());
                _components.Dispose();
            }
        }
        //-------------------------------------------------------------------------------------
        public override string ToString()
        {
            return $"[{ObjID}]{DisplayName}";
        }

        protected internal virtual void OnAdded() { }


        public LayerObjectComponentCollection Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new(this, static (a, b) => a.Priority - b.Priority);
                }
                return _components;
            }
        }
        private void UpdateComponents(float intervalMS)
        {
            _components?.ForEach(intervalMS, static (st, c) => c.InternalUpdate(st));
        }

        //-------------------------------------------------------------------------------------
        protected abstract void UpdateAI();
        protected abstract void Update();
        protected virtual void UpdateEnd() { }

        internal void InternalBeginUpdate()
        {
            if (Parent!= null)
            {
                this.UpdateAI();
            }
        }
        internal void InternalUpdate()
        {
            this.UpdateComponents(Parent.CurrentIntervalMS);
            this.Update();
        }
        internal bool InternalEndUpdate()
        {
            if (Parent != null)
            {
                var curPos = this.Position;
                try
                {
                    if (this is ILayerZoneEntity block)
                    {
                        if (mLastPos != curPos)
                        {
                            Parent.SwapSpace(block, true);
                            return true;
                        }
                    }
                    return false;
                }
                finally
                {
                    UpdateEnd();
                    mLastPos = curPos;
                }
            }
            return false;
        }
        //-------------------------------------------------------------------------------------

        public override void ForceFaceTo(float dir, float body_dir)
        {
            mDirection.ForceSync(dir, body_dir);
        }
        public override void ForceSyncPos(in Vector3 pos)
        {
            base.ForceSyncPos(pos);
        }
        virtual public void SyncPos(UnitSyncPos pos)
        {
            if (pos.HasModifer(UnitSyncModifer.Posistion))
            {
                this.mRemotePos.X = pos.X;
                this.mRemotePos.Y = pos.Y;
                this.mRemotePos.Z = pos.Z;
            }
            if (pos.HasModifer(UnitSyncModifer.Direction))
            {
                this.mDirection.FaceTo(pos.Direction);
            }
            if (pos.HasModifer(UnitSyncModifer.BodyRotation))
            {
                this.mDirection.BodyTo(pos.BodyDirection);
            }

        }
        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------
    }


}
