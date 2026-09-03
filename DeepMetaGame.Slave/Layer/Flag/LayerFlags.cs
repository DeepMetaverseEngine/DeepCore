using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepCore.Game3D.Slave.Layer
{
    //-----------------------------------------------------------------------------------------------------------

    public abstract class LayerFlag : LayerObject
    {
        protected readonly SceneObjectData data;
        public override string Name => data.Name;
        public override string DisplayName => data.Name;
        public bool Enable { get; internal set; }
        public string Tag { get; internal set; }
        public SceneObjectData EditorData { get { return data; } }
        public override float BodyHeight => data.Height;
        public virtual IZoneShape ZoneShape { get { return null; } }
        public override float BodyDirection => this.Direction;

        public LayerFlag(SceneObjectData data, LayerZone parent)
        {
            base.Init(parent);
            this.data = data;
            this.mRemotePos.X = data.X;
            this.mRemotePos.Y = data.Y;
            this.mRemotePos.Z = data.Z;
            this.Enable = data.Enable;
            this.Tag = data.Tag;
        }
        internal protected virtual void OnInit() { }
        public override string ToString()
        {
            return Name;
        }
        public override void ForceFaceTo(float dir, float body_dir)        {        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorUnit : LayerFlag
    {
        public readonly UnitData Data;
        public override float BodyBlockSize { get { return 0; } }
        public override float Direction => Data.Direction;
        public LayerEditorUnit(UnitData data, LayerZone parent)
            : base(data, parent)
        {
            this.Data = data;
        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorItem : LayerFlag
    {
        public readonly ItemData Data;
        public override float BodyBlockSize { get { return 0; } }
        public override float Direction => Data.Direction;
        public LayerEditorItem(ItemData data, LayerZone parent)
            : base(data, parent)
        {
            this.Data = data;
        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorRegion : LayerFlag
    {
        public readonly RegionData Data;
        public override float BodyBlockSize { get { return 0; } }
        public override float Direction => Data.Direction;
        public LayerEditorRegion(RegionData data, LayerZone parent)
            : base(data, parent)
        {
            this.Data = data;
        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorPoint : LayerFlag
    {
        public readonly PointData Data;
        public override float BodyBlockSize { get { return 0; } }
        public override float Direction => Data.Direction;
        public LayerEditorPoint(PointData data, LayerZone parent)
            : base(data, parent)
        {
            this.Data = data;
        }
    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorDecoration : LayerFlag, ILayerZoneEntity
    {
        public readonly DecorationData Data;
        public readonly float W;
        public readonly float H;
        //private readonly Rectangle BoundingBox;
        private readonly float blockSize;
        private readonly IZoneShape zoneShape;
        private readonly LayerSpaceDivision.SpaceUserTag mCurrentCellNode;

        public override float BodyBlockSize { get { return blockSize; } }
        public override float Direction => Data.Direction;
        public override IZoneShape ZoneShape { get { return zoneShape; } }

        public bool IsStaticBlock { get => this.Enable && this.Data.Blockable; }

        public LayerSpaceDivision.SpaceUserTag CurrentCellNode { get => mCurrentCellNode; }
        public VoxelCylinder VoxelBody { get => new VoxelCylinder(Position, BodyBlockSize, this.Data.Height); }

        public LayerEditorDecoration(DecorationData flag, LayerZone parent)
            : base(flag, parent)
        {
            this.Data = flag;
            this.W = flag.W;
            this.H = flag.H;
            this.zoneShape = flag.ToZoneShape();
            this.blockSize = flag.Radius;
            if (parent.SpaceDiv != null)
            {
                this.mCurrentCellNode = parent.SpaceDiv.CreateUserTag(this);
            }
        }
        protected internal void DecorationChanged()
        {
            if (Data.Blockable)
            {
                Parent.Terrain3D.FillMapBlockByShape(this.ZoneShape, IsStaticBlock);
            }
        }
        protected internal override void OnInit()
        {
            this.DecorationChanged();
            Parent.SwapSpace(this, true);
        }

        public virtual bool Touch(LayerUnit u)
        {
            if (CMath.IsIntersectW(u.Z, u.BodyHeight, this.Z, this.BodyHeight))
            {
                if (zoneShape != null)
                {
                    return zoneShape.Include(u.X, u.Y);
                }
            }
            return false;
        }

    }

    //-----------------------------------------------------------------------------------------------------------

    public class LayerEditorArea : LayerFlag
    {
        public readonly AreaData Data;
        public readonly float W;
        public readonly float H;
        //public int CurrentMapNodeValue { get; private set; }
        public override float BodyBlockSize { get { return 0; } }
        public override float Direction => Data.Direction;
        public Geometry.BoundingBox AABB { get; private set; }

        public LayerEditorArea(AreaData data, LayerZone parent)
            : base(data, parent)
        {
            this.Data = data;
            this.W = data.W;
            this.H = data.H;
        }

        protected internal override void OnInit()
        {
            Parent.SpaceDiv.ClampPosition(
                  this.Data.X - this.Data.W / 2f,
                  this.Data.Y - this.Data.H / 2f,
                  this.Data.X + this.Data.W / 2f,
                  this.Data.Y + this.Data.H / 2f,
                  out var cx1, out var cy1, out var cx2, out var cy2);
            var cs = Parent.SpaceDivSizeW;
            this.AABB = new Geometry.BoundingBox(
                 new Geometry.Vector3(cx1 * cs, cy1 * cs, this.Data.Z - this.Data.Height),
                 new Geometry.Vector3(cx2 * cs + cs, cy2 * cs + cs, this.Data.Z + this.Data.Height));
            Parent.SpaceDiv.ForEachSpaceCellNodes(cx1, cy1, cx2, cy2, this, static (st, cell) =>
            {
                var sc = cell as LayerSpaceDivision.ZoneSpaceCellNode;
                sc.Area = st;
            });
        }
    }

    //-----------------------------------------------------------------------------------------------------------
}
