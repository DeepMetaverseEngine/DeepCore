using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Message;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class ZoneDecoration : InstanceFlag, IEntityObject
    {
        new public DecorationData EditorData { get => base.EditorData as DecorationData; }
        public override float BodySize { get { return Data.Radius; } }
        public override float Direction { get { return this.Data.Direction; } }
        public bool StaticBlockable { get; }
        public ITerrainLayer CurrentLayer { get; private set; }
        public override Geometry.VoxelCylinder VoxelBody { get;  }
        public bool IsStaticBlock => StaticBlockable && Enable;
        public ZoneSpaceDivision.ZoneSpaceUserTag SpaceUserTag => mCurrentSpaceTag;

        private readonly DecorationData.Shape mShape;
        private int BlockValue;
        internal readonly ZoneSpaceDivision.ZoneSpaceUserTag mCurrentSpaceTag;

        public ZoneDecoration(InstanceZone zone, DecorationData data)
            : base(zone, data)
        {
            this.mShape = data.RegionType;
            this.StaticBlockable = data.Blockable;
            this.BlockValue = data.BlockValue;
//             this.OnFlagEnabled += this.OnEnabled;
//             this.OnFlagDisabled += this.OnDisabled;
            if (StaticBlockable)
            {
                this.OnFlagEnabled += this.OnTerrainEnabled;
                this.OnFlagDisabled += this.OnTerrainDisabled;
            }
            this.VoxelBody = new Geometry.VoxelCylinder(this.Position, this.BodySize, this.BodyHeight);
            this.ZoneShape = data.ToZoneShape();
            if (StaticBlockable)
            {
                this.mCurrentSpaceTag = Parent.SpaceDiv.CreateUserTag(this) as ZoneSpaceDivision.ZoneSpaceUserTag;
            }
        }

        internal override void onAdded()
        {
            this.CurrentLayer = Parent.Terrain3D.GetVoxelLayerByPos(this.Position);
            base.onAdded();
            Parent.swapSpace(this);
        }


        public override Geometry.Vector3 GetRandomPos()
        {
            var pos = this.Position;
            var random = Parent.RandomN;
            switch (mShape)
            {
                case DecorationData.Shape.ROUND:
                    {
                        float angle = (float)(random.NextDouble() * CMath.PI_MUL_2);
                        float len = (float)(random.NextDouble() * Data.R);
                        float x = X + (float)(Math.Cos(angle) * len);
                        float y = Y + (float)(Math.Sin(angle) * len);
                        return new Geometry.Vector3(x, y, Z);
                    }
                case DecorationData.Shape.RECTANGLE:
                    {
                        float x = X + (float)((-Data.W / 2f) + random.NextDouble() * Data.W);
                        float y = Y + (float)((-Data.H / 2f) + random.NextDouble() * Data.H);
                        return new Geometry.Vector3(x, y, Z);
                    }
                case DecorationData.Shape.STRIP:
                    {
                        float x = X + (float)((-Data.W / 2f) + random.NextDouble() * Data.W);
                        float y = Y + (float)((-Data.H / 2f) + random.NextDouble() * Data.H);
                        DeepCore.Geometry.VectorHelper.Rotate(ref x, ref y, pos.X, pos.Y, Direction);
                        return new Geometry.Vector3(x, y, Z);
                    }
            }
            return pos;
        }
//         private void OnEnabled(InstanceFlag flag)
//         {
//             //Parent.PostEvent(new DecorationChangedEvent(Name, true));
//         }
//         private void OnDisabled(InstanceFlag flag)
//         {
//             //Parent.PostEvent(new DecorationChangedEvent(Name, false));
//         }

        public virtual bool Touch(InstanceZoneObject u)
        {
            if (CMath.IsIntersectW(u.Z, u.BodyHeight, this.Z, this.BodyHeight))
            {
                if (ZoneShape != null)
                {
                    return ZoneShape.Include(u.X, u.Y);
                }
            }
            return false;
        }

        // TODO FILL TERRAIN
        private void OnTerrainEnabled(InstanceFlag flag)
        {
            this.FillTerrainCross(!IsStaticBlock);
        }
        private void OnTerrainDisabled(InstanceFlag flag)
        {
            this.FillTerrainCross(true);
        }
        protected void FillTerrainCross(bool cross)
        {
            var mmap = Parent.TerrainWorld.PathFinder;
            //             mmap.ForEachByShape(this.ZoneShape, cross, static (cross, mapnode) =>
            //             {
            //                 mapnode.FillCross(cross);
            //                 return false;
            //             });
            mmap.FillMapBlockByShape(this.ZoneShape, !cross);
        }

        public DecorationData Data { get => this.EditorData as DecorationData; }
    }
}
