using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DeepCore.GUI.Cell
{
    
    //-------------------------------------------------------------------------------------------------------------------
    public struct CPJTerrainColor : ITerrainColor
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;
        public uint ARGB
        {
            get
            {
                uint rgb = 0;
                rgb |= ((uint)(A)) << 24;
                rgb |= ((uint)(R)) << 16;
                rgb |= ((uint)(G)) << 8;
                rgb |= ((uint)(B));
                return rgb;
            }
        }
        byte ITerrainColor.R => R;
        byte ITerrainColor.G => G;
        byte ITerrainColor.B => B;
        byte ITerrainColor.A => A;
        public override string ToString()
        {
            return ARGB.ToString("X8");
        }
        public static CPJTerrainColor Zero = new CPJTerrainColor();
    }
    //-------------------------------------------------------------------------------------------------------------------
    public class CPJTerrain : Disposable, ITerrain
    {
        public MapSet Map { get; }
        public Vector2 TotalSize { get; }
        public int XCount => Map.XCount;
        public int YCount => Map.YCount;
        public float GridCellSize => Map.CellW;
        public float GridCellHalf { get; }
        public float TotalSizeX => TotalSize.X;
        public float TotalSizeY => TotalSize.Y;
        public float StepIntercept => 1f;
        public float ResourceStartX => 0;
        public float ResourceStartY => 0;
        public ITerrainColor[] ColorPalette => pallette;
        private ITerrainColor[] pallette;
        private VoxelLayer[,] matrix;
        public CPJTerrain(MapSet map)
        {
            this.Map = map;
            this.GridCellHalf = map.CellW / 2f;
            this.TotalSize = new Vector2(map.XCount * map.CellW, map.YCount * map.CellH);
            this.pallette = new ITerrainColor[this.Map.Blocks.Length];
            this.matrix = new VoxelLayer[map.XCount, map.YCount];
        }
        protected override void Disposing()
        {
        }
        //------------------------------------------------------------------------
        public void WorldPosToVoxel(float x, float y, out int bx, out int by)
        {
            bx = (int)(x / GridCellSize);
            by = (int)(y / GridCellSize);
        }
        public void WorldSizeToVoxel(float radius, out int size)
        {
            size = (int)Math.Ceiling(radius / GridCellSize);
        }
        public bool TryClampVoxelPos(ref int x, ref int y)
        {
            bool ret = false;
            if (x < 0) { x = 0; ret = true; }
            if (y < 0) { y = 0; ret = true; }
            if (x >= XCount) { x = XCount - 1; ret = true; }
            if (y >= YCount) { y = YCount - 1; ret = true; }
            return ret;
        }
        internal VoxelLayer GetVoxelLayer(int x, int y)
        {
            return matrix[x, y];
        }
        public bool TryGetVoxelLayer(int x, int y, out VoxelLayer layer)
        {
            if (x >= 0 && x < XCount && y >= 0 && y < YCount)
            {
                layer = matrix[x, y];
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryGetVoxelLayerByPos(in Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelLayer(x, y, out layer))
            {
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryGetVoxelLayerByObject(ref Vector3 vector, out VoxelLayer layer)
        {
            WorldPosToVoxel(vector.X, vector.Y, out var x, out var y);
            var fix = TryClampVoxelPos(ref x, ref y);
            if (TryGetVoxelLayer(x, y, out layer))
            {
                if (fix)
                {
                    var center = layer.UpwardCenterPos;
                    vector.X = center.X;
                    vector.Y = center.Y;
                }
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryUpdatePos(ref Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelLayer(x, y, out layer))
            {
                if (layer != null)
                {
                    return true;
                }
            }
            layer = null;
            return false;
        }
        public bool TryIntersectMapByPos(in Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelLayer(x, y, out layer))
            {
                return layer.IsBlock;
            }
            layer = null;
            return false;
        }
        public bool TryTestInAirByPos(in Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelLayer(x, y, out layer))
            {
                return false;

            }
            layer = null;
            return false;
            //return path_finder.Terrain.TouchMapByPos(x, y);
        }
        public bool TryMoveTo(ref Vector3 target, out VoxelLayer touchLayer)
        {
            touchLayer = null;
            WorldPosToVoxel(target.X, target.Y, out var x, out var y);
            if (TryGetVoxelLayer(x, y, out touchLayer))
            {
                return true;
            }
            return false;

        }

        /// <summary>
        /// 闇払い 百八式·暗勾手
        /// </summary>
        public bool TryMoveSpellOnFloor(ref Vector3 pos, ref VoxelLayer layer, float direction, float distance)
        {
            Vector2 target = pos;
            VectorHelper.MovePolar(ref target, direction, distance);
            return TryMoveSpellOnFloor(ref pos, ref layer, target);
        }
        /// <summary>
        /// 闇払い 百八式·暗勾手
        /// </summary>
        public bool TryMoveSpellOnFloor(ref Vector3 pos, ref VoxelLayer layer, Vector2 target)
        {
            WorldPosToVoxel(target.X, target.Y, out var tx, out var ty);
            //没跨格//
            if (layer.X == tx && layer.Y == ty)
            {
                pos.X = target.X;
                pos.Y = target.Y;
                return true;
            }
            var inAir = pos.Z > layer.Upward;
            var oldLayer = layer;
            var dx = tx - layer.X;
            var dy = ty - layer.Y;
            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);
            if (adx <= 1 && ady <= 1)
            {
                {
                    var next = layer.GetNextNode(dx, dy);
                    //跨格已连接//
                    if (next != null && next.TryStandOn(ref pos))
                    {
                        layer = next;
                        pos.X = target.X;
                        pos.Y = target.Y;
                        pos.Z = next.Upward;
                        return true;
                    }
                }
                if (adx == ady)
                {
                    //尝试单边//
                    var nextX = layer.GetNextNode(dx, 0);
                    if (nextX != null && nextX.TryStandOn(ref pos))
                    {
                        layer = nextX;
                        pos.X = target.X;
                        pos.Z = nextX.Upward;
                        return true;
                    }
                    //尝试单边//
                    var nextY = layer.GetNextNode(0, dy);
                    if (nextY != null && nextY.TryStandOn(ref pos))
                    {
                        layer = nextY;
                        pos.Y = target.Y;
                        pos.Z = nextY.Upward;
                        return true;
                    }
                }
                return false;
                //                 if (layer.Y == ty)
                //                 {
                //                     pos.Y = target.Y;
                //                 }
                //                 if (layer.X == tx)
                //                 {
                //                     pos.X = target.X;
                //                 }
            }
            else
            {
                //跨格无连接//
                if (TryGetVoxelLayer(tx, ty, out var next_cell))
                {
                    //下格无碰撞//
                    if (next_cell.TryStandOn(ref pos))
                    {
                        layer = next_cell;
                        pos.X = target.X;
                        pos.Y = target.Y;
                        pos.Z = next_cell.Upward;
                        return true;
                    }
                }
            }
            //不可行走面//
            return false;
        }

        //------------------------------------------------------------------------

        ITerrainBlock ITerrain.GetVoxelLayerByObject(ref Vector3 pos)
        {
            TryGetVoxelLayerByObject(ref pos, out var layer);
            return layer;
        }
        ITerrainBlock ITerrain.GetVoxelLayerByPos(in Vector3 pos)
        {
            TryGetVoxelLayerByPos(in pos, out var layer);
            return layer;
        }
        bool ITerrain.TryUpdatePos(ref Vector3 pos, out ITerrainBlock layer)
        {
            var ret = TryUpdatePos(ref pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryGetVoxelLayerByPos(in Vector3 pos, out ITerrainBlock layer, bool ground)
        {
            var ret = TryGetVoxelLayerByPos(in pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryGetVoxelLayerByObject(ref Vector3 vector, out ITerrainBlock layer)
        {
            var ret = TryGetVoxelLayerByObject(ref vector, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryIntersectMapByPos(in Vector3 pos, out ITerrainBlock layer)
        {
            var ret = TryIntersectMapByPos(in pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryTestInAirByPos(in Vector3 pos, out ITerrainBlock layer)
        {
            var ret = TryTestInAirByPos(in pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveTo(ref Vector3 target, out ITerrainBlock touchLayer)
        {
            var ret = TryMoveTo(ref target, out var _layer);
            touchLayer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainBlock layer, float direction, float distance)
        {
            var _layer = layer as VoxelLayer;
            var ret = this.TryMoveSpellOnFloor(ref pos, ref _layer, direction, distance);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainBlock layer, in Vector2 target)
        {
            var _layer = layer as VoxelLayer;
            var ret = this.TryMoveSpellOnFloor(ref pos, ref _layer, target);
            layer = _layer;
            return ret;
        }
        bool ITerrain.RayCast(in Ray ray, out Vector3? hitPoint, out ITerrainBlock hitLayer)
        {
            hitPoint = default;
            hitLayer = default;
            var ret = TryGetVoxelLayerByPos(in ray.Position, out var _layer);
            if (ret)
            {
                if (!_layer.IsBlock)
                {
                    var p = ray.Position;
                    p.Z = _layer.UpwardCenterPos.Z;
                    hitPoint = p;
                    hitLayer = _layer;
                    return ret;
                }
            }
            return false;
        }

        public class VoxelLayer : ITerrainBlock
        {
            public readonly CPJTerrain terrain;
            public readonly short X;
            public readonly short Y;
            public bool IsBlock => terrain.Map.Terrain[0, Y, X].TerrainFlag != 0;
            public VoxelLayer(CPJTerrain t, short x, short y)
            {
                X = x; Y = y;
            }
            public Vector3 UpwardCenterPos => new Vector3(
                X * terrain.GridCellSize + terrain.GridCellHalf,
                Y * terrain.GridCellSize + terrain.GridCellHalf,
                0);
            public float Top => 1000;
            public float Upward => 0;
            public float Downward => 1;
            public float Height => 1;
            public bool IsPlane => true;
            public byte ColorIndex => 0;
            public ITerrainColor Color => CPJTerrainColor.Zero;
            public VoxelLayer GetNextNode(int dx, int dy)
            {
                var nx = X + dx;
                var ny = Y + dy;
                if (terrain.TryGetVoxelLayer(nx, ny, out var next))
                {
                    return next;
                }
                return null;
            }
            public bool TryStandOn(ref Vector3 pos)
            {
                return !IsBlock;
            }
        }
    }
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
}
