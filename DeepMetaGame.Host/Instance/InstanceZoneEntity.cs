using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Misc;

namespace DeepCore.Game3D.Host.Instance
{
    //-------------------------------------------------------------------------------------------------------//
    public abstract class InstanceZoneEntity : InstanceZoneObject, IEntityObject
    {
        // 体素坐标
        protected readonly ITerrainAgent mPos;
        // 空间分割节点
        private readonly ZoneSpaceDivision.ZoneSpaceUserTag mCurCellNode;
        private IZoneShape mZoneShape;
        //---------------------------------------------------------------------------------------

        public InstanceZoneEntity(InstanceZone zone, bool is_static_block) : base(zone)
        {
            this.StaticBlockable = is_static_block;
            this.mCurCellNode = Parent.SpaceDiv.CreateUserTag(this) as ZoneSpaceDivision.ZoneSpaceUserTag;
            this.mPos = zone.TerrainWorld.CreateAgent();
        }
        public override string ToString()
        {
            return $"{GetType().Name}({ObjectID})";
        }
        protected override void onAdded()
        {
            this.mPos.EnterWorld(Parent.TerrainWorld);
        }
        protected override void onRemoved()
        {
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.mPos.LeaveWorld();
        }
        /// <summary>
        /// 获得当前空间分割节点
        /// </summary>
        public ZoneSpaceDivision.ZoneSpaceUserTag SpaceUserTag { get { return mCurCellNode; } }
        public ZoneSpaceDivision.ZoneSpaceCellNode CurrentSpaceCell { get { return mCurCellNode.SpaceCell; } }

        /// <summary>
        /// 当前空间布局已改变
        /// </summary>
        public bool PosDirty { get { return mCurCellNode.IsPosDirty; } }
        /// <summary>
        /// 是否为静态阻挡物
        /// </summary>
        public bool StaticBlockable { get; }
        /// <summary>
        /// 地图阻挡 ?
        /// </summary>
        public abstract bool IntersectMap { get; }
        /// <summary>
        /// 单位阻挡
        /// </summary>
        public abstract bool IntersectObj { get; }
        /// <summary>
        /// 当前为静态阻挡
        /// </summary>
        public abstract bool IsStaticBlock { get; }
        /// <summary>
        /// 获取当前体素层
        /// </summary>
        public ITerrainLayer CurrentLayer { get => mPos.CurrentLayer; }

        public Vector3 CurrentLayerCenterPos => mPos.CurrentLayer.UpwardCenterPos;
        public override float X { get => mPos.X; }
        public override float Y { get => mPos.Y; }
        public override float Z { get => mPos.Z; }
        public override Geometry.Vector3 Position { get => mPos.Position; }
        public bool IsInTheAir { get => mPos.IsInTheAir; }
        public virtual IZoneShape ZoneShape
        {
            get { return mZoneShape; }
            set { mZoneShape = value; }
        }
        protected override void InternalSetPos(Geometry.Vector3 pos)
        {
            this.mPos.Transport(pos);
        }
        protected override void EnterWorld(Vector3 pos)
        {
            mPos.EnterWorld(Parent.TerrainWorld);
            this.mPos.Transport(pos);
        }
        public ITerrainLayer FindNearRandomMoveableNode(float range)
        {
            return Parent.TerrainWorld.FindNearRandomMoveableNode(Parent.RandomN, mPos.Clone(), range);
        }
    }

}
