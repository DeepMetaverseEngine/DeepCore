using DeepCore;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Space;
using DeepCore.Voxel.Data.PathFinder;
using DeepCore.Voxel.Extensions.MagicaVoxel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore.Voxel.Data
{
    //--------------------------------------------------------------------------------------------------
    public class VoxelLayer : ITerrainLayer
    {
        //private VoxelLayer[,] mNextMoveable = new VoxelLayer[3, 3];
        private static readonly NextFlag[][] NEXT_MOVEABLE_INDEX = new NextFlag[3][] {
            new NextFlag[3]{ NextFlag.LT, NextFlag.CT, NextFlag.RT },
            new NextFlag[3]{ NextFlag.LC, /*      */0, NextFlag.RC },
            new NextFlag[3]{ NextFlag.LB, NextFlag.CB, NextFlag.RB },
        };
        private static readonly int[][] NEXT_INDEX_TABLE = new int[][] {
            new int[]{ -1,-1, (int)NextFlag.LT}, new int[]{ 0,-1, (int)NextFlag.CT}, new int[]{ 1,-1, (int)NextFlag.RT},
            new int[]{ -1, 0, (int)NextFlag.LC},/*new int[]{0,0,                  */ new int[]{ 1, 0, (int)NextFlag.RC},
            new int[]{ -1, 1, (int)NextFlag.LB}, new int[]{ 0, 1, (int)NextFlag.CB}, new int[]{ 1, 1, (int)NextFlag.RB},};
        private static readonly int[][] NEXT_CROSS_INDEX_TABLE = new int[][] {
            new int[]{ 0,-1 },
            new int[]{ -1, 0 },
            new int[]{ 1, 0 },
            new int[]{ 0, 1 },};
        public enum NextFlag : byte
        {
            LT = 0x01,
            CT = 0x02,
            RT = 0x04,
            LC = 0x08,
            /*CC*/
            RC = 0x10,
            LB = 0x20,
            CB = 0x40,
            RB = 0x80,
        }
        internal byte mNextFlag;
        public readonly byte Layer;
        public readonly byte ColorIndex;
        public readonly float Upward;
        public readonly float Downward;
        //public IVoxelMapNode MapNode;

        public TerrainColor Color { get => OwnerCell.Terrain.Palette.Colors[ColorIndex]; }
        public bool HasNext { get => mNextFlag != 0; }
        public float Height { get => Upward - Downward; }
        public bool IsPlane { get => Upward == Downward; }


        float ITerrainLayer.Upward => this.Upward;
        float ITerrainLayer.Downward => this.Downward;
        TerrainColor ITerrainLayer.Color { get => OwnerCell.Terrain.Palette.Colors[ColorIndex]; }
        byte ITerrainLayer.ColorIndex { get => this.ColorIndex; }


        public readonly VoxelCell OwnerCell;
        public VoxelLayer UpLayer { get { OwnerCell.TryGetLayer(Layer + 1, out var up); return up; } }
        public VoxelLayer DownLayer { get { OwnerCell.TryGetLayer(Layer - 1, out var down); return down; } }
        public short X { get => OwnerCell.X; }
        public short Y { get => OwnerCell.Y; }
        public Vector3 UpwardCenterPos
        {
            get
            {
                var t = OwnerCell.Terrain;
                return new Vector3(
                    OwnerCell.X * t.GridCellSize + t.GridCellRadius,
                    OwnerCell.Y * t.GridCellSize + t.GridCellRadius,
                    Upward);
            }
        }
        public Vector3 UpwardTopLeft
        {
            get
            {
                var t = OwnerCell.Terrain;
                return new Vector3(
                    OwnerCell.X * t.GridCellSize,
                    OwnerCell.Y * t.GridCellSize,
                    Upward);
            }
        }
        public float Top
        {
            get
            {
                if (OwnerCell.TryGetLayer(this.Layer + 1, out var up))
                {
                    return up.Downward;
                }
                return float.MaxValue;
            }
        }


        #region Init
        //-------------------------------------------------------------------------------------
        internal VoxelLayer(VoxelTerrain3D terrain, VoxelCell owner, int layer, VoxelNodeData data)
        {
            terrain.TotalLayerCount++;
            this.OwnerCell = owner;
            this.Layer = (byte)layer;
            if (terrain.BuildConfig.FloatPrecision > 1)
            {
                this.Upward = ((int)Math.Ceiling(data.Upward * terrain.BuildConfig.FloatPrecision)) / ((float)terrain.BuildConfig.FloatPrecision);
                this.Downward = ((int)Math.Ceiling(data.Downward * terrain.BuildConfig.FloatPrecision)) / ((float)terrain.BuildConfig.FloatPrecision);
            }
            else
            {
                this.Upward = data.Upward;
                this.Downward = data.Downward;
            }
            this.ColorIndex = terrain.Palette.IndexOfColor(data.Color, out var _);
        }
        internal void VoxelLayerInitNext(VoxelTerrain3D terrain)
        {
            mNextFlag = 0;
            foreach (var index in NEXT_INDEX_TABLE)
            {
                int ox = index[0];
                int oy = index[1];
                var nf = (byte)index[2];
                if (terrain.TryGetNextStep(this, ox, oy, out var next))
                {
                    mNextFlag |= nf;
                }
            }
        }
        //-------------------------------------------------------------------------------------
        internal VoxelLayer(VoxelTerrain3D terrain, VoxelCell owner, int layer, VoxelLayer src)
        {
            this.OwnerCell = owner;
            this.Layer = (byte)layer;
            this.Upward = src.Upward;
            this.Downward = src.Downward;
            this.ColorIndex = src.ColorIndex;
            this.mNextFlag = src.mNextFlag;
        }
        //-------------------------------------------------------------------------------------
        internal VoxelLayer(VoxelCell owner, byte layer, IInputStream input)
        {
            this.OwnerCell = owner;
            this.Layer = (byte)layer;
            this.Upward = input.GetF32();
            this.Downward = input.GetF32();
            this.ColorIndex = input.GetU8();
        }
        internal void VoxelLayerInitNext(VoxelTerrain3D terrain, IInputStream input)
        {
            this.mNextFlag = input.GetU8();
        }
        //-------------------------------------------------------------------------------------
        internal void Save(IOutputStream output)
        {
            output.PutF32(Upward);
            output.PutF32(Downward);
            output.PutU8(ColorIndex);
        }
        internal void SaveNext(IOutputStream output)
        {
            output.PutU8(this.mNextFlag);
        }
        #endregion

        public BoundingBox GetBlockBoundingBox()
        {
            var terrain = OwnerCell.Terrain;
            var min = new Vector3(OwnerCell.X * terrain.GridCellSize, OwnerCell.Y * terrain.GridCellSize, Downward);
            var max = new Vector3(min.X + terrain.GridCellSize, min.Y + terrain.GridCellSize, Upward);
            return new BoundingBox(min, max);
        }
        public BoundingBox GetFullBoundingBox()
        {
            var terrain = OwnerCell.Terrain;
            var min = new Vector3(OwnerCell.X * terrain.GridCellSize, OwnerCell.Y * terrain.GridCellSize, Downward);
            var max = new Vector3(min.X + terrain.GridCellSize, min.Y + terrain.GridCellSize, Top);
            return new BoundingBox(min, max);
        }
        public bool TryStandOn(ref Vector3 pos, float height, float step)
        {
            if (Top - Upward > height)
            {
                if (pos.Z < this.Upward)
                {
                    if (pos.Z + step >= this.Upward)
                    {
                        pos.Z = Upward;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var bt = this.Top - height;
                    if (pos.Z - step < bt)
                    {
                        pos.Z = Math.Min(pos.Z, bt);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }
        public bool TryStandOn(ref Vector3 pos, float step)
        {
            if (pos.Z < this.Upward)
            {
                if (pos.Z + step >= this.Upward)
                {
                    pos.Z = Upward;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (pos.Z - step < this.Top)
                {
                    pos.Z = Math.Min(pos.Z, this.Top);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public int GetNextNodeCount()
        {
            int ret = 0;
            for (int i = 0; i < 8; i++)
            {
                if (((mNextFlag >> i) & 0x01) != 0) ret++;
            }
            return ret;
        }
        public VoxelLayer GetNextNode(int ox, int oy)
        {
            TryGetNextNode(ox, oy, out var next);
            return next;
        }
        public bool TryGetNextNode(int ox, int oy, out VoxelLayer next)
        {
            var na = (byte)NEXT_MOVEABLE_INDEX[oy + 1][ox + 1];
            if ((mNextFlag & na) != 0)
            {
                var dx = OwnerCell.X + ox;
                var dy = OwnerCell.Y + oy;
                var nc = OwnerCell.Terrain.mCellGrid[dx, dy];
                if (OwnerCell.Terrain.TryGetNextStep(this, nc, out next))
                {
                    return true;
                }
                if (nc.LayerCount == 1)
                {
                    next = nc.GroundLayer;
                    return true;
                }
            }
            next = null;
            return false;
        }
        public int GetMaxDistance(VoxelLayer next)
        {
            var ox = next.X - this.X;
            var oy = next.Y - this.Y;
            return Math.Max(Math.Abs(ox), Math.Abs(oy));
        }
        public bool ContainsNextNode(VoxelLayer next)
        {
            var ox = next.X - this.X;
            var oy = next.Y - this.Y;
            if (Math.Abs(ox) <= 1 && Math.Abs(oy) <= 1)
            {
                return GetNextNode(ox, oy) == next;
            }
            return false;
        }
        /// <summary>
        /// 上下左右斜方向
        /// </summary>
        /// <param name="action"></param>
        public void ForEachNextNodes<ST>(ST st, Action<VoxelLayer, ST> action)
        {
            foreach (var index in NEXT_INDEX_TABLE)
            {
                int ox = index[0];
                int oy = index[1];
                if (TryGetNextNode(ox, oy, out var next))
                {
                    action(next, st);
                }
            }
        }
        /// <summary>
        /// 上下左右
        /// </summary>
        /// <param name="action"></param>
        public void ForEachNextCrossNodes<ST>(ST st, Action<VoxelLayer, ST> action)
        {
            foreach (var index in NEXT_CROSS_INDEX_TABLE)
            {
                int ox = index[0];
                int oy = index[1];
                if (TryGetNextNode(ox, oy, out var next))
                {
                    action(next, st);
                }
            }
        }
        public void ForEachNearCell<ST>(ST st, Action<int, int, VoxelCell, ST> action)
        {
            var terrain = OwnerCell.Terrain;
            foreach (var index in NEXT_INDEX_TABLE)
            {
                int ox = index[0];
                int oy = index[1];
                if (terrain.TryGetVoxelCell(this.X + ox, this.Y + oy, out var nextCell))
                {
                    action(ox, oy, nextCell, st);
                }
            }
        }
        //--------------------------------------------------------------------------------------------------
        #region WIN32_EDITOR

        /// <summary>
        /// 编辑器调用，修改连接网格
        /// </summary>
        public bool TryLinkNextNode(VoxelLayer next)
        {
            if (TestLinkNextNode(next))
            {
                int ox = next.X - this.X;
                int oy = next.Y - this.Y;
                if (Math.Abs(ox) <= 1 && Math.Abs(oy) <= 1)
                {
                    var OF1 = this.mNextFlag;
                    var OF2 = next.mNextFlag;
                    var nf = (byte)NEXT_MOVEABLE_INDEX[1 + oy][1 + ox];
                    var pf = (byte)NEXT_MOVEABLE_INDEX[1 - oy][1 - ox];
                    this.mNextFlag |= (byte)(nf);
                    next.mNextFlag |= (byte)(pf);
                    if (OF1 != this.mNextFlag || OF2 != next.mNextFlag)
                    {
                        return true;
                    }
                }
                //                 this.mNextMoveable[1 + ox, 1 + oy] = next;
                //                 next.mNextMoveable[1 - ox, 1 - oy] = this;
                //                     this.mNextMoveableI[1 + ox, 1 + oy] = next.Layer;
                //                 next.mNextMoveableI[1 - ox, 1 - oy] = this.Layer;
            }
            return false;
        }
        /// <summary>
        /// 编辑器调用，修改连接网格
        /// </summary>
        public bool TryUnlinkNextNode(VoxelLayer next)
        {
            if (TestUnlinkNextNode(next))
            {
                int ox = next.X - this.X;
                int oy = next.Y - this.Y;
                if (Math.Abs(ox) <= 1 && Math.Abs(oy) <= 1)
                {
                    var OF1 = this.mNextFlag;
                    var OF2 = next.mNextFlag;
                    var nf = (byte)NEXT_MOVEABLE_INDEX[1 + oy][1 + ox];
                    var pf = (byte)NEXT_MOVEABLE_INDEX[1 - oy][1 - ox];
                    this.mNextFlag &= (byte)(~nf);
                    next.mNextFlag &= (byte)(~pf);
                    if (OF1 != this.mNextFlag || OF2 != next.mNextFlag)
                    {
                        return true;
                    }
                }
                //                 this.mNextMoveableI[1 + ox, 1 + oy] = -1;
                //                 next.mNextMoveableI[1 - ox, 1 - oy] = -1;
            }
            return false;
        }
        /// <summary>
        /// 编辑器调用，修改连接网格
        /// </summary>
        public bool TestLinkNextNode(VoxelLayer next)
        {
            if (next == this) return false;
            int ox = next.X - this.X;
            int oy = next.Y - this.Y;
            if (Math.Abs(ox) <= 1 && Math.Abs(oy) <= 1)
            {
                if (this.GetNextNode(ox, oy) == null && next.GetNextNode(-ox, -oy) == null)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 编辑器调用，修改连接网格
        /// </summary>
        public bool TestUnlinkNextNode(VoxelLayer next)
        {
            if (next == this) return false;
            int ox = next.X - this.X;
            int oy = next.Y - this.Y;
            if (Math.Abs(ox) <= 1 && Math.Abs(oy) <= 1)
            {
                if (this.GetNextNode(ox, oy) == next || next.GetNextNode(-ox, -oy) == this)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
        //--------------------------------------------------------------------------------------------------
    }
    //--------------------------------------------------------------------------------------------------
    public class VoxelCell
    {
        public readonly VoxelTerrain3D Terrain;
        internal VoxelLayer[] mLayers;
        /// <summary>
        /// 网格位置
        /// </summary>
        public readonly short X;
        /// <summary>
        /// 网格位置
        /// </summary>
        public readonly short Y;
        /// <summary>
        /// 最顶层节点
        /// </summary>
        public VoxelLayer TopLayer { get => mLayers[mLayers.Length - 1]; }
        /// <summary>
        /// 地表层节点
        /// </summary>
        public VoxelLayer GroundLayer { get => mLayers[0]; }
        /// <summary>
        /// 层数量
        /// </summary>
        public int LayerCount { get => mLayers.Length; }

        #region Init
        internal VoxelCell(VoxelTerrain3D owner, short x, short y, List<VoxelNodeData> layers)
        {
            this.Terrain = owner;
            owner.TotalCellCount++;
            this.X = x;
            this.Y = y;
            this.mLayers = new VoxelLayer[layers.Count];
            for (int i = 0; i < layers.Count; i++)
            {
                mLayers[i] = new VoxelLayer(owner, this, i, layers[i]);
            }
        }
        internal void VoxelCellInit(VoxelTerrain3D terrain)
        {
            foreach (var layer in mLayers)
            {
                layer.VoxelLayerInitNext(terrain);
            }
        }
        internal VoxelCell(VoxelTerrain3D owner, VoxelCell src, Func<VoxelLayer, bool> selector)
        {
            this.Terrain = owner;
            this.X = src.X;
            this.Y = src.Y;
            var list = new List<VoxelLayer>();
            foreach (var src_layer in src.mLayers)
            {
                if (selector(src_layer))
                {
                    list.Add(new VoxelLayer(owner, this, list.Count, src_layer));
                }
            }
            this.mLayers = list.ToArray();
        }
        internal VoxelCell(VoxelTerrain3D owner, short x, short y, IInputStream input)
        {
            this.Terrain = owner;
            this.X = x;
            this.Y = y;
            this.mLayers = new VoxelLayer[input.GetS32()];
            if (mLayers.Length > byte.MaxValue) { throw new Exception($"Layer too much : {x}{y} : layers={mLayers.Length} "); }
            for (int i = 0; i < mLayers.Length; i++)
            {
                mLayers[i] = new VoxelLayer(this, (byte)i, input);
            }
        }
        internal void VoxelCellInit(VoxelTerrain3D terrain, IInputStream input)
        {
            foreach (var layer in mLayers)
            {
                layer.VoxelLayerInitNext(terrain, input);
            }
        }




        internal void Save(IOutputStream output)
        {
            output.PutS32(LayerCount);
            foreach (var layer in mLayers)
            {
                layer.Save(output);
            }
        }
        internal void SaveNext(IOutputStream output)
        {
            foreach (var layer in mLayers)
            {
                layer.SaveNext(output);
            }
        }
        #endregion

        public VoxelLayer GetLayer(int layer)
        {
            return mLayers[layer];
        }
        public bool TryGetLayer(int layer, out VoxelLayer node)
        {
            if (layer >= 0 && layer < mLayers.Length)
            {
                node = mLayers[layer];
                return true;
            }
            else
            {
                node = null;
                return false;
            }
        }
        /// <summary>
        /// 根据海拔找到Layer
        /// </summary>
        public VoxelLayer GetLayerByAltitude(float z)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                var node = mLayers[i];
                if (z >= node.Downward && z <= node.Top)
                {
                    return node;
                }
            }
            return null;
        }
        /// <summary>
        /// 根据水平移动找到Layer
        /// </summary>
        public VoxelLayer GetLayerByStep(float z, float step)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                var node = mLayers[i];
                if (z >= node.Upward - step && z <= node.Upward + step)
                {
                    return node;
                }
            }
            return null;
        }
        /// <summary>
        /// 根据单位坐标，获取当前体素
        /// </summary>
        public VoxelLayer GetLayerAndStandOn(ref Vector3 vector)
        {
            for (int i = mLayers.Length - 1; i >= 0; --i)
            {
                var node = mLayers[i];
                if (vector.Z >= node.Downward && vector.Z <= node.Top)
                {
                    if (vector.Z < node.Upward) { vector.Z = node.Upward; }
                    return node;
                }
            }
            if (vector.Z < GroundLayer.Upward) { vector.Z = GroundLayer.Upward; }
            return this.GroundLayer;
        }

        /// <summary>
        /// 按照海拔高度，找到可移动到的层
        /// </summary>
        public bool TryStandOn(ref Vector3 pos, float height, float step, out VoxelLayer layer)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                var node = mLayers[i];
                if (node.TryStandOn(ref pos, height, step))
                {
                    layer = node;
                    return true;
                }
            }
            layer = null;
            return false;
        }
        public bool TryStandOn(ref Vector3 pos, float step, out VoxelLayer layer)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                var node = mLayers[i];
                if (node.TryStandOn(ref pos, step))
                {
                    layer = node;
                    return true;
                }
            }
            layer = null;
            return false;
        }


        public VoxelLayer FindNearAltitude(float z, float distance = float.MaxValue)
        {
            VoxelLayer min = null;
            for (int i = 0; i < mLayers.Length; i++)
            {
                var layer = mLayers[i];
                if (min == null) { min = layer; }
                else
                {
                    var md = Math.Abs(z - min.Upward);
                    var cd = Math.Abs(z - layer.Upward);
                    if (cd < md)
                    {
                        min = layer;
                    }
                }
            }
            if (Math.Abs(min.Upward - z) <= distance)
            {
                return min;
            }
            return null;
        }


        public void ForEachLayers<ST>(ST st, Action<VoxelLayer, ST> action)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                action(mLayers[i], st);
            }
        }

        internal Vector2 GetCenterPos()
        {
            var terrain = Terrain;
            return new Vector2(
                this.X * terrain.GridCellSize + terrain.GridCellRadius,
                this.Y * terrain.GridCellSize + terrain.GridCellRadius);
        }
    }
    //--------------------------------------------------------------------------------------------------


}
