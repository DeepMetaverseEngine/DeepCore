using DeepCore;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Space;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using static DeepCore.Geometry.Dummy.DummyTerrainWorld;

namespace DeepMetaGame.Data.ZoneGeometry.Terrain
{
    public class TerrainMap : ITerrain
    {
        public static TerrainPalette BuildPalette(SceneData data)
        {
            var palette = new TerrainPalette();
            var root = TerrainPaletteOctreeQuantizer.Node.CreateRoot();
            var matrix = data.ZoneData;
            matrix.ForEach(root, (root, x, y, flag) =>
            {
                root.AddColor2Root(flag, 8);

            });
            palette.Colors = TerrainPaletteOctreeQuantizer.Node.MergeColors(root, 256);
            return palette;
        }
        //----------------------------------------------------------------------------------
        public readonly SceneData data;
        public readonly ZoneInfo zone;
        private TerrainLayer[,] matrix;
        public TerrainLayer[,] Matrix { get => matrix; }
        public TerrainPalette Palette { get; }
        public int XCount => zone.XCount;
        public int YCount => zone.YCount;
        public BoundingBox AABB { get; }
        //----------------------------------------------------------------------------------
        public TerrainMap(SceneData data, TemplateManager templates)
        {
            this.data = data;
            this.zone = data.ZoneData;
            this.Palette = BuildPalette(data);
            this.matrix = new TerrainLayer[zone.XCount, zone.YCount];
            this.AABB = new BoundingBox(
                    new Vector3(0, 0, -1),
                    new Vector3(TotalSizeX, TotalSizeY, Math.Min(TotalSizeX, TotalSizeY)));
            var tdefine = data.OverrideTerrainDefinition;
            if (tdefine == null)
            {
                tdefine = templates.DefaultTerrainDefinition;
            }
            zone.ForEach(this, (st, x, y, flag) =>
            {
                if (tdefine != null)
                {
                    var brush = tdefine.GetMapBlockBrush(flag);
                    if (brush == null || !brush.IsBlock)
                    {
                        this.matrix[x, y] = new TerrainLayer(x, y, flag, this);
                    }
                }
                else
                {
                    this.matrix[x, y] = new TerrainLayer(x, y, flag, this);
                }
            });
        }
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
            if (x >= zone.XCount) { x = zone.XCount - 1; ret = true; }
            if (y >= zone.YCount) { y = zone.YCount - 1; ret = true; }
            return ret;
        }
        public TerrainLayer GetVoxelCell(int x, int y)
        {
            return matrix[x, y];
        }
        public bool TryGetVoxelCell(int x, int y, out TerrainLayer cell)
        {
            if (x >= 0 && x < zone.XCount && y >= 0 && y < zone.YCount)
            {
                cell = matrix[x, y];
                return cell != null;
            }
            cell = default;
            return false;
        }
        private struct BlinkState
        {
            public TerrainMap terrain;
            public TerrainLayer currentLayer;
            public Vector3 currentPos;
            public float height;
            public float step;
        }
        /// <summary>
        /// 贴地闪现移动
        /// </summary>
        /// <returns>阻挡</returns>
        public bool TryBlinkToTarget2D(
            TerrainLayer src, in Vector3 srcP,
            TerrainLayer dst, in Vector3 dstP,
            out TerrainLayer currentLayer,
            out Vector3 currentPos,
            float height = 0)
        {
            Vector2 pos = srcP;
            Vector2 target = dstP;
            var tuple = new BlinkState()
            {
                terrain = this,
                currentLayer = src,
                currentPos = srcP,
                height = height,
                step = 1
            };
            matrix.ForEachCellsRayStepPloar(ref tuple, ref pos, target, GridCellSize,
                static (tuple, cell, cx, cy, current) =>
                {
                    if (cell == null)
                    {
                        return true;
                    }
                    Vector3 p = current;
                    p.Z = tuple.currentPos.Z;
                    if (cell.TryStandOn(ref p, tuple.height, tuple.step))
                    {
                        return false;
                    }
                    return true;
                },
                static (ref BlinkState tuple, Vector2 current) =>
                {
                    tuple.currentPos.X = current.X;
                    tuple.currentPos.Y = current.Y;
                    if (tuple.terrain.TryGetVoxelLayerByObject(ref tuple.currentPos, out var layer))
                    {
                        tuple.currentPos.Z = layer.Upward;
                        tuple.currentLayer = layer as TerrainLayer;
                    }
                });
            currentLayer = tuple.currentLayer;
            currentPos = tuple.currentPos;
            if (currentLayer == dst)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ForEachByShape<ST>(IShape shape, ST st, ForEachPredicate<ST, TerrainLayer> action)
        {
            var mmap = new GridTerrain()
            {
                GridSize = GridCellSize,
                XCount = XCount,
                YCount = YCount,
                include = true,
            };
            return mmap.ForEachByShape(shape, (this, st, action, shape), static (tuple, bx, by) =>
            {
                var astar = tuple.Item1;
                var st = tuple.st;
                var action = tuple.action;
                var shape = tuple.shape;
                var layer = astar.GetVoxelCell(bx, by);
                if (layer != null)
                {
                    return action(st, layer);
                }
                return false;
            });
        }
        //----------------------------------------------------------------------------------
        #region Impl
        public float GridCellSize => zone.GridCellH;
        public float StepIntercept => 1;
        public float TotalSizeX => zone.TotalWidth;
        public float TotalSizeY => zone.TotalHeight;
        public float ResourceStartX => 0;
        public float ResourceStartY => 0;
        public TerrainColor[] ColorPalette => Palette.Colors;
        public void Dispose()
        {

        }
        public ITerrainLayer GetVoxelLayerByObject(ref Vector3 vector)
        {
            WorldPosToVoxel(vector.X, vector.Y, out var x, out var y);
            var fix = TryClampVoxelPos(ref x, ref y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                if (fix)
                {
                    var center = cell.UpwardCenterPos;
                    vector.X = center.X;
                    vector.Y = center.Y;
                }
                cell.GetLayerAndStandOn(ref vector);
                return cell;
            }
            return null;
        }
        public ITerrainLayer GetVoxelLayerByPos(in Vector3 pos)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                return cell;
            }
            return null;
        }
        public bool TryGetVoxelLayerByObject(ref Vector3 vector, out ITerrainLayer layer)
        {
            WorldPosToVoxel(vector.X, vector.Y, out var x, out var y);
            var fix = TryClampVoxelPos(ref x, ref y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                if (fix)
                {
                    var center = cell.UpwardCenterPos;
                    vector.X = center.X;
                    vector.Y = center.Y;
                }
                cell.GetLayerAndStandOn(ref vector);
                layer = cell;
                return true;
            }
            layer = null;
            return false;
        }

        public bool TryGetVoxelLayerByPos(in Vector3 pos, out ITerrainLayer layer, bool ground = false)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell;
                return true;
            }
            layer = null;
            return false;
        }
        public bool TryUpdatePos(ref Vector3 pos, out ITerrainLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell;
                var d = pos.Z - layer.Upward;
                if (d >= 0)
                {
                    return true;
                }
                else if (-d <= 1)
                {
                    pos.Z = layer.Upward;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            layer = null;
            return false;
        }

        public bool TryIntersectMapByPos(in Vector3 pos, out ITerrainLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell;
                return pos.Z < layer.Upward;
            }
            layer = null;
            return false;
        }

        public bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, float direction, float distance)
        {
            Vector2 target = pos;
            VectorHelper.MovePolar(ref target, direction, distance);
            return TryMoveSpellOnFloor(ref pos, ref layer, target);
        }

        public bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer _layer, in Vector2 target)
        {
            WorldPosToVoxel(target.X, target.Y, out var tx, out var ty);
            var layer = _layer as TerrainLayer;
            //没跨格//
            if (layer.X == tx && layer.Y == ty)
            {
                pos.X = target.X;
                pos.Y = target.Y;
                return true;
            }
            var step = 1;
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
                    if (next != null && next.TryStandOn(ref pos, step))
                    {
                        _layer = next;
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
                    if (nextX != null && nextX.TryStandOn(ref pos, step))
                    {
                        _layer = nextX;
                        pos.X = target.X;
                        pos.Z = nextX.Upward;
                        return true;
                    }
                    //尝试单边//
                    var nextY = layer.GetNextNode(0, dy);
                    if (nextY != null && nextY.TryStandOn(ref pos, step))
                    {
                        _layer = nextY;
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
                if (TryGetVoxelCell(tx, ty, out var next_cell))
                {
                    //下格无碰撞//
                    if (next_cell.TryStandOn(ref pos, step))
                    {
                        _layer = next_cell;
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

        public bool TryMoveTo(ref Vector3 target, out ITerrainLayer touchLayer)
        {
            WorldPosToVoxel(target.X, target.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                touchLayer = cell;
                if (target.Z < touchLayer.Upward)
                {
                    target.Z = touchLayer.Upward;
                }
                return true;
            }
            touchLayer = null;
            return false;
        }

        public bool TryTestInAirByPos(in Vector3 pos, out ITerrainLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell;
                return pos.Z > layer.Upward;
            }
            layer = null;
            return false;
        }

        public bool RayCast(in Ray ray, out Vector3? hitPoint, out ITerrainLayer hitLayer)
        {
            var ray_touch = DeepCore.Geometry.RayCast.RayPlaneIntersection(ray.Position, ray.Direction, Vector3.Zero, Vector3.UnitZ);
            if (TryGetVoxelLayerByPos(ray_touch, out hitLayer))
            {
                hitPoint = ray_touch;
                return true;
            }
            hitPoint = null;
            return false;
        }
        #endregion
    }
    //------------------------------------------------------------------------------------------------------------------------------------
    public class TerrainLayer : ITerrainLayer
    {
        //-----------------------------------------------------------------------
        public float Top => float.MaxValue;
        public float Upward => 0;
        public float Downward => -1;
        public float Height => 1;
        public bool IsPlane => true;
        //-----------------------------------------------------------------------
        public Vector3 UpwardCenterPos { get; }
        public byte ColorIndex { get; }
        public TerrainColor Color { get; }
        readonly public TerrainMap Terrain;
        readonly public short X;
        readonly public short Y;
        //-----------------------------------------------------------------------
        public TerrainLayer(int x, int y, int color, TerrainMap terrain)
        {
            this.ColorIndex = terrain.Palette.IndexOfColor(color, out var tcolor);
            this.Color = tcolor;
            this.Terrain = terrain;
            this.X = (short)x;
            this.Y = (short)y;
            this.UpwardCenterPos = new Vector3(
                X * terrain.GridCellSize + terrain.GridCellSize / 2f,
                Y * terrain.GridCellSize + terrain.GridCellSize / 2f,
                0);
        }
        public TerrainLayer GetLayerAndStandOn(ref Vector3 vector)
        {
            if (vector.Z < this.Upward)
            {
                vector.Z = this.Upward;
            }
            return this;
        }
        public TerrainLayer GetNextNode(int ox, int oy)
        {
            TryGetNextNode(ox, oy, out var next);
            return next;
        }
        public bool TryGetNextNode(int ox, int oy, out TerrainLayer next)
        {
            {
                var dx = X + ox;
                var dy = Y + oy;
                next = Terrain.Matrix[dx, dy];
                if (next != null)
                {
                    return true;
                }
            }
            next = null;
            return false;
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
        private static readonly int[][] NEXT_INDEX_TABLE = new int[][] {
            new int[]{ -1,-1}, new int[]{ 0,-1 }, new int[]{ 1,-1},
            new int[]{ -1, 0},/*new int[]{0,0,}*/ new int[]{ 1, 0},
            new int[]{ -1, 1}, new int[]{ 0, 1},  new int[]{ 1, 1} };
        /// <summary>
        /// 上下左右斜方向
        /// </summary>
        /// <param name="action"></param>
        public void ForEachNextNodes<ST>(ST st, Action<TerrainLayer, ST> action)
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
        public int GetNextNodeCount()
        {
            int ret = 0;
            foreach (var index in NEXT_INDEX_TABLE)
            {
                int ox = index[0];
                int oy = index[1];
                if (TryGetNextNode(ox, oy, out var next))
                {
                    ret++;
                }
            }
            return ret;
        }
    }
}
