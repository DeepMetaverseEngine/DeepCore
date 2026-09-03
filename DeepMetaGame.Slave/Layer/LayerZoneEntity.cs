using DeepCore.Game3D.Slave.Helper;
using DeepCore.Space;
using DeepMetaGame.Data.Misc;

namespace DeepCore.Game3D.Slave.Layer
{

    public abstract class LayerZoneEntity : LayerZoneObject, ILayerZoneEntity
    {
        private LayerSpaceDivision.ZoneUserCellNode mCurrentCellNode;
        protected override LayerZoneObject Init(uint objectID, LayerZone parent)
        {
            base.Init(objectID, parent);
            if (parent.SpaceDiv != null)
            {
                this.mCurrentCellNode = parent.SpaceDiv.CreateUserTag(this) as LayerSpaceDivision.ZoneUserCellNode;
            }
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.mCurrentCellNode?.Dispose();
            this.mCurrentCellNode = null;
        }
        public Geometry.VoxelCylinder VoxelBody { get => new Geometry.VoxelCylinder(this.Position, this.BodyBlockSize, this.BodyHeight); }
        public SpaceDivision<ILayerZoneEntity>.SpaceUserTag CurrentCellNode => mCurrentCellNode;
        public LayerSpaceDivision.ZoneUserCellNode CurrentUserCellNode => mCurrentCellNode;
        public LayerSpaceDivision.ZoneSpaceCellNode CurrentSpaceCellNode => mCurrentCellNode?.SpaceCell;
        public abstract bool IsStaticBlock { get; }
        public abstract IZoneShape ZoneShape { get; }
    }

}
