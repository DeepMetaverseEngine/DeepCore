using DeepCore.Game3D.Slave.Layer;
using DeepMetaGame.Data;using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;using DeepMetaGame.Data.Misc;
using DeepCore.Space;

namespace DeepCore.Game3D.Slave.Helper
{
    public interface ILayerZoneEntity
    {
        LayerZone Parent { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        bool IsStaticBlock { get; }
        float RadiusSize { get; }
        float BodyHeight { get; }
        Geometry.Vector3 Position { get; }
        Geometry.VoxelCylinder VoxelBody { get; }
        LayerSpaceDivision.SpaceUserTag CurrentCellNode { get; }
        IZoneShape ZoneShape { get; }
    }

    public class LayerSpaceDivision : SpaceDivision<ILayerZoneEntity>
    {
        public LayerSpaceDivision(LayerZone zone) : base(
                zone.Terrain3D.TotalWidth,
                zone.Terrain3D.TotalHeight,
                zone.SpaceDivSizeW,
                zone.SpaceDivSizeW)
        {
        }
        protected override SpaceCellNode CreateSpaceCellNode(int cx, int cy)
        {
            return new ZoneSpaceCellNode(cx, cy);
        }
        public override SpaceUserTag CreateUserTag(ILayerZoneEntity obj)
        {
            return new ZoneUserCellNode(this, obj);
        }
        public virtual void SpaceUpdate(int intervalMS)
        {
        }
        public class ZoneUserCellNode : SpaceUserTag
        {
            public ZoneUserCellNode(LayerSpaceDivision div, ILayerZoneEntity obj) : base(div, obj)
            {
            }
            public ILayerZoneEntity Object { get { return base.UserTag as ILayerZoneEntity; } }
            new public ZoneUserCellNode Next { get { return (ZoneUserCellNode)base.Next; } }
            new public ZoneUserCellNode Prev { get { return (ZoneUserCellNode)base.Prev; } }
            new public ZoneSpaceCellNode SpaceCell { get { return (ZoneSpaceCellNode)base.SpaceCell; } }
        }
        public class ZoneSpaceCellNode : SpaceCellNode
        {
            public LayerEditorArea Area { get; internal set; }
            public ZoneSpaceCellNode(int six, int siy) : base(six, siy)
            {
            }
        }
    }
}
