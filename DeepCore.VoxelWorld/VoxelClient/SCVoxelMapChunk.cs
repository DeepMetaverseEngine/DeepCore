using DeepCore.Geometry;
using DeepCore.Space;
using DeepCore.Voxel.Data;
using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static DeepCore.VoxelWorld.VoxelClient.SCVoxelAOI;

namespace DeepCore.VoxelWorld.VoxelClient
{
    public class SCVoxelMapChunk
    {
        public SCVoxelWorld World { get; }
        public ChunkMetaMap LOD { get; }
        public Location3D WorldLocation { get; }
        public Vector3 WorldPosition { get; }
        public Message.FetchMapChunkResponse Fetch { get; private set; }
        public StreamingChunk Chunk { get; private set; }
        public float GridCellSize { get; private set; }
        public float GridCellRadius { get; private set; }
        public bool HasCube { get; private set; }
        public Size3D ChunkSize { get; private set; }
        public int XCount { get; private set; }
        public int YCount { get; private set; }
        public BoundingBox WorldBoundingBox { get; private set; }
        public Vector3 WorldSize { get; private set; }
        public bool IsVisible { get; internal set; } = true;

        private SCVoxelCell[,] mCellGrid;

        public SCVoxelMapChunk(SCVoxelWorld world, ChunkMetaMap meta, Location3D worldLocation)
        {
            this.World = world;
            this.LOD = meta;
            this.ChunkSize = world.ChunkSize;

            this.WorldLocation = worldLocation;
            this.WorldPosition = new Vector3(
                WorldLocation.X * world.GridCellSize,
                WorldLocation.Y * world.GridCellSize,
                WorldLocation.Z * world.GridCellSize);
            this.WorldSize = new Vector3(
                  ChunkSize.X * world.GridCellSize,
                  ChunkSize.Y * world.GridCellSize,
                  ChunkSize.Z * world.GridCellSize);
        }
        public override string ToString()
        {
            return $"Chunk:{WorldLocation}";
        }
        public void Init(Message.FetchMapChunkResponse fetch)
        {
            if (this.Fetch == null)
            {
                this.Fetch = fetch;
                this.GridCellSize = fetch.ChunkGridCellSize;
                this.GridCellRadius = fetch.ChunkGridCellSize / 2f;
                this.ChunkSize = fetch.ChunkSize;
                this.XCount = fetch.ChunkSize.X;
                this.YCount = fetch.ChunkSize.Y;
                this.WorldSize = new Vector3(
                    ChunkSize.X * GridCellSize,
                    ChunkSize.Y * GridCellSize,
                    ChunkSize.Z * GridCellSize);
                this.WorldBoundingBox = new BoundingBox(WorldPosition, WorldPosition + WorldSize);
            }
        }
        public void InitTouch(StreamingChunk chunk)
        {
            this.Chunk = chunk;
            this.HasCube = chunk?.Cubes != null;
            if (chunk != null && chunk.TouchGrids != null)
            {
                var grid = new SCVoxelCell[chunk.ChunkSize.X, chunk.ChunkSize.Y];
                grid.InitArray2D(this, (st, x, y) =>
                {
                    if (chunk.TouchGrids[x, y] != null && chunk.TouchGrids[x, y].Length > 0)
                    {
                        var cell = new SCVoxelCell(this, (byte)x, (byte)y, chunk.TouchGrids[x, y]);
                        return cell;
                    }
                    return null;
                });
                this.mCellGrid = grid;
            }
        }
        public void ReleaseTouch()
        {
            mCellGrid = null;
        }
        //--------------------------------------------------------------------------------------------------------------
        public bool IntersectLocation(in Location3D loc)
        {
            var min = WorldLocation;
            var max = new Location3D(
                min.X + World.ChunkSize.X,
                min.Y + World.ChunkSize.Y,
                min.Z + World.ChunkSize.Z);
            if (loc.X >= min.X && loc.X < max.X)
            {
                if (loc.Y >= min.Y && loc.Y < max.Y)
                {
                    if (loc.Z >= min.Z && loc.Z < max.Z)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void WorldPosToChunkVoxel(in Vector3 pos, out Location3D loc)
        {
            World.WorldPosToVoxel(pos, out loc);
            loc.X -= this.WorldLocation.X;
            loc.Y -= this.WorldLocation.Y;
            loc.Z -= this.WorldLocation.Z;
        }
        public void WorldLocToChunkVoxel(in Location3D pos, out Location3D loc)
        {
            loc.X = pos.X - this.WorldLocation.X;
            loc.Y = pos.Y - this.WorldLocation.Y;
            loc.Z = pos.Z - this.WorldLocation.Z;
        }
        public BoundingBox GetCubeWorldBoundingBox(StreamingCube cube)
        {
            var min = new Vector3(
                cube.X * GridCellSize,
                cube.Y * GridCellSize,
                cube.Z * GridCellSize);
            var max = min + new Vector3(
                GridCellSize,
                GridCellSize,
                GridCellSize);
            return new BoundingBox(WorldPosition + min, WorldPosition + max);
        }
        public Vector3 GetCubeWorldPosition(StreamingCube cube)
        {
            var min = new Vector3(
                cube.X * GridCellSize,
                cube.Y * GridCellSize,
                cube.Z * GridCellSize);
            return min;
        }
        //--------------------------------------------------------------------------------------------------------------
        public bool TryGetVoxelCell(in Location3D loc, out SCVoxelCell cell)
        {
            if (mCellGrid != null)
            {
                WorldLocToChunkVoxel(in loc, out var cloc);
                if (cloc.X >= 0 && cloc.X < XCount && cloc.Y >= 0 && cloc.Y < YCount)
                {
                    cell = mCellGrid[cloc.X, cloc.Y];
                    return cell != null;
                }
            }
            cell = null;
            return false;
        }
        //--------------------------------------------------------------------------------------------------------------
        public SCVoxelLayer ForEachLayers(BreakPredicate<SCVoxelLayer> action)
        {
            if (mCellGrid != null)
            {
                for (int x = this.XCount - 1; x >= 0; --x)
                {
                    for (int y = this.YCount - 1; y >= 0; --y)
                    {
                        var cell = mCellGrid[x, y];
                        if (cell != null)
                        {
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                var layer = cell.GetLayer(i);
                                if (action(layer))
                                {
                                    return layer;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public SCVoxelCell ForEachCells(BreakPredicate<SCVoxelCell> action)
        {
            if (mCellGrid != null)
            {
                for (int x = this.XCount - 1; x >= 0; --x)
                {
                    for (int y = this.YCount - 1; y >= 0; --y)
                    {
                        var cell = mCellGrid[x, y];
                        if (cell != null)
                        {
                            if (action(cell))
                            {
                                return cell;
                            }
                        }
                    }
                }
            }
            return null;
        }
        //--------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        public SCVoxelCell ForEachCellsRayStepPloar(Vector2 center, Vector2 target, BreakPredicate<SCVoxelCell, int, int> action, bool breakOutBounds = true)
        {
            if (mCellGrid != null)
            {
                var rrr = this;
                var pos = center;
                return mCellGrid.ForEachCellsRayStepPloar(ref rrr, ref pos, target, GridCellSize, (chunk, cell, cx, cy, cur) =>
                {
                    return action(cell, cx, cy);
                }, null, breakOutBounds);
            }
            return null;
        }

    }
    public class SCVoxelCell
    {
        /// <summary>
        /// 最顶层节点
        /// </summary>
        public SCVoxelLayer TopLayer { get => mLayers[mLayers.Length - 1]; }
        /// <summary>
        /// 地表层节点
        /// </summary>
        public SCVoxelLayer GroundLayer { get => mLayers[0]; }
        /// <summary>
        /// 层数量
        /// </summary>
        public int LayerCount { get => mLayers.Length; }
        public readonly SCVoxelMapChunk OwnerChunk;
        /// <summary>
        /// 网格位置
        /// </summary>
        public readonly byte CubeX;
        /// <summary>
        /// 网格位置
        /// </summary>
        public readonly byte CubeY;
        private SCVoxelLayer[] mLayers;

        internal SCVoxelCell(SCVoxelMapChunk owner, byte x, byte y, StreamingTouchLayer[] layers)
        {
            this.OwnerChunk = owner;
            this.CubeX = x;
            this.CubeY = y;
            this.mLayers = new SCVoxelLayer[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                mLayers[i] = new SCVoxelLayer(this, (byte)i,
                    layers[i].Upward + owner.WorldPosition.Z,
                    layers[i].Downward + owner.WorldPosition.Z);
            }
        }
        public SCVoxelLayer GetLayer(int layer)
        {
            return mLayers[layer];
        }
        public bool TryGetLayer(int layer, out SCVoxelLayer node)
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
        public SCVoxelLayer RayCastUpward(float z, float height)
        {
            TryRayCastUpward(z, height, out var layer);
            return layer;
        }
        public SCVoxelLayer RayCastDownward(float z, float height)
        {
            TryRayCastDownward(z, height, out var layer);
            return layer;
        }
        public bool TryRayCastUpward(float z, float height, out SCVoxelLayer layer)
        {
            for (int i = mLayers.Length - 1; i >= 0; --i)
            {
                var node = mLayers[i];
                if (z >= node.WorldDownward)
                {
                    layer = node;
                    return true;
                }
            }
            layer = null;
            return false;
        }
        public bool TryRayCastDownward(float z, float height, out SCVoxelLayer layer)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                var node = mLayers[i];
                if (z < node.WorldUpward)
                {
                    layer = node;
                    return true;
                }
            }
            layer = null;
            return false;
        }
        public bool TryTouchMoveTo(ref float z, float step, out SCVoxelLayer layer)
        {
            for (int i = mLayers.Length - 1; i >= 0; --i)
            {
                var node = mLayers[i];
                if (node.TryTouchMoveTo(ref z, step))
                {
                    layer = node;
                    return true;
                }
                if (node.TestBlockZ(z))
                {
                    layer = null;
                    return false;
                }
            }
            layer = null;
            return true;
        }
        public void ForEachLayers(Action<SCVoxelLayer> action)
        {
            for (int i = 0; i < mLayers.Length; i++)
            {
                action(mLayers[i]);
            }
        }
    }
    public class SCVoxelLayer
    {
        public SCVoxelMapChunk OwnerChunk { get => OwnerCell?.OwnerChunk; }
        /// <summary>
        /// 行走上沿
        /// </summary>
        public float WorldUpward => upward;
        /// <summary>
        /// 高度下沿
        /// </summary>
        public float WorldDownward => downward;
        /// <summary>
        /// 上链接节点
        /// </summary>
        public SCVoxelLayer UpLayer { get { OwnerCell.TryGetLayer(Layer + 1, out var up); return up; } }
        /// <summary>
        /// 下链接节点
        /// </summary>
        public SCVoxelLayer DownLayer { get { OwnerCell.TryGetLayer(Layer - 1, out var down); return down; } }
        /// <summary>
        /// 高度
        /// </summary>
        public float Height { get => WorldUpward - WorldDownward; }
        /// <summary>
        /// 是否高度为0
        /// </summary>
        public bool IsPlane { get => WorldUpward == WorldDownward; }
        /// <summary>
        /// 网格位置
        /// </summary>
        public byte CubeX { get => OwnerCell.CubeX; }
        /// <summary>
        /// 网格位置
        /// </summary>
        public byte CubeY { get => OwnerCell.CubeY; }
        /// <summary>
        /// 中心点
        /// </summary>
        public Vector3 WorldUpwardCenterPos
        {
            get
            {
                var t = OwnerChunk;
                return new Vector3(
                    (OwnerCell.OwnerChunk.WorldLocation.X + OwnerCell.CubeX) * t.GridCellSize + t.GridCellRadius,
                    (OwnerCell.OwnerChunk.WorldLocation.Y + OwnerCell.CubeY) * t.GridCellSize + t.GridCellRadius,
                    WorldUpward);
            }
        }
        public Vector3 WorldDownwardPos
        {
            get
            {
                var t = OwnerChunk;
                return t.WorldPosition + new Vector3(
                    OwnerCell.CubeX * t.GridCellSize,
                    OwnerCell.CubeY * t.GridCellSize,
                    WorldDownward);
            }
        }
        public Vector3 WorldUpwardPos
        {
            get
            {
                var t = OwnerChunk;
                return t.WorldPosition + new Vector3(
                    OwnerCell.CubeX * t.GridCellSize,
                    OwnerCell.CubeY * t.GridCellSize,
                    WorldUpward);
            }
        }
        public BoundingBox WorldBoundingBox
        {
            get
            {
                var t = OwnerChunk;
                var min = WorldDownwardPos;
                var max = min + new Vector3(t.GridCellSize, t.GridCellSize, 0);
                max.Z = WorldUpward;
                return new BoundingBox(min, max);
            }
        }
        public BoundingBox WorldFullBoundingBox
        {
            get
            {
                var t = OwnerChunk;
                var min = WorldDownwardPos;
                var max = min + new Vector3(t.GridCellSize, t.GridCellSize, 0);
                max.Z = t.WorldBoundingBox.Max.Z;
                return new BoundingBox(min, max);
            }
        }
        /// <summary>
        /// 节点所在层数
        /// </summary>
        public readonly byte Layer;
        public readonly SCVoxelCell OwnerCell;
        private float upward;
        private float downward;

        internal SCVoxelLayer(SCVoxelCell owner, byte layer, float upward, float downward)
        {
            this.OwnerCell = owner;
            this.Layer = layer;
            this.upward = upward;
            this.downward = downward;
        }
        public override string ToString()
        {
            return $"[{OwnerCell.CubeX} {OwnerCell.CubeY}][{Layer}]";
        }
        internal void ExpandDown(SCVoxelLayer down)
        {
            this.downward = down.downward;
        }
        public bool TryTouchMoveTo(ref float z, float step)
        {
            if (z < this.WorldUpward)
            {
                if (z + step >= this.WorldUpward)
                {
                    z = WorldUpward;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        public bool TryTouchBumpHead(ref Vector3 pos, float height)
        {
            var head_z = pos.Z + height;
            if (head_z > downward)
            {
                pos.Z = downward - height;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool TestBlockZ(float z)
        {
            if (z > downward && z < upward)
            {
                return true;
            }
            return false;
        }
    }

}