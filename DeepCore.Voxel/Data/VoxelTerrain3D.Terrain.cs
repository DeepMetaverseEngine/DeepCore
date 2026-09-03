using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using DeepCore.Voxel.Data.PathFinder;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DeepCore.Space.GridMatrix;

namespace DeepCore.Voxel.Data
{
    public class VoxelTerrain3D : Disposable, ITerrain
    {
        public static readonly string FILE_EXT = ".voxt";
        private static readonly byte[] FILE_HEAD = System.Text.Encoding.ASCII.GetBytes("VOXT");
        /// <summary>
        /// 网格尺寸
        /// </summary>
        public float GridCellSize { get; private set; }
        public float GridCellRadius { get; private set; }
        public VoxelBuildConfig BuildConfig { get => mCFG; }
        public VoxelPalette Palette { get; private set; } = new VoxelPalette();
        public int XCount { get; private set; }
        public int YCount { get; private set; }
        public float TotalSizeX { get; private set; }
        public float TotalSizeY { get; private set; }
        public float ResourceStartX { get; private set; }
        public float ResourceStartY { get; private set; }
        public int TotalLayerCount { get; internal set; }
        public int TotalCellCount { get; internal set; }
        public BoundingBox AABB { get => mAABB; }
        public bool IsGZip { get => VoxelStream.IsGZip(ref mFlags); private set => VoxelStream.SetGZip(ref mFlags, value); }
        private BitSet32 mFlags;
        private BoundingBox mAABB = new BoundingBox();
        private VoxelBuildConfig mCFG;
        internal VoxelCell[,] mCellGrid;
        #region Init
        public VoxelTerrain3D(VoxelTerrainData data, VoxelBuildConfig cfg, IRangeValue progress = null)
        {
            progress?.SetMax(data.XCount * data.YCount * 2);
            progress?.SetMin(0);
            progress?.SetValue(0);
            this.mCFG = cfg;
            {
                this.IsGZip = cfg.IsGZip;
            }
            this.GridCellSize = data.GridSize;
            this.GridCellRadius = GridCellSize / 2f;
            data.GetLength(out var xc, out var yc);
            this.XCount = xc;
            this.YCount = yc;
            this.TotalSizeX = xc * GridCellSize;
            this.TotalSizeY = yc * GridCellSize;
            this.ResourceStartX = data.MinX;
            this.ResourceStartY = data.MinY;
            this.mAABB.Min.X = 0;
            this.mAABB.Min.Y = 0;
            this.mAABB.Min.Z = float.MaxValue;
            this.mAABB.Max.X = TotalSizeX;
            this.mAABB.Max.Y = TotalSizeY;
            this.mAABB.Max.Z = float.MinValue;
            var aabb = mAABB;
            for (int x = xc - 1; x >= 0; --x)
            {
                for (int y = yc - 1; y >= 0; --y)
                {
                    var cdata = cfg.FlipY ? data.Grids[x, yc - 1 - y] : data.Grids[x, y];
                    if (!cdata.IsNullOrEmpty())
                    {
                        for (int z = 0; z < cdata.Length; z++)
                        {
                            var layer = cdata[z];
                            if (layer.Upward > aabb.Max.Z) { aabb.Max.Z = layer.Upward; }
                            if (layer.Downward < aabb.Min.Z) { aabb.Min.Z = layer.Downward; }
                        }
                    }
                }
            }
            this.Palette.Load(data, cfg);
            this.mCellGrid = new VoxelCell[xc, yc];
            var templayers = new List<VoxelNodeData>();
            for (int x = xc - 1; x >= 0; --x)
            {
                for (int y = yc - 1; y >= 0; --y)
                {
                    var cdata = cfg.FlipY ? data.Grids[x, yc - 1 - y] : data.Grids[x, y];
                    if (!cdata.IsNullOrEmpty())
                    {
                        templayers.Clear();
                        templayers.AddRange(cdata);
                        if (CombineLayers(templayers, in aabb))
                        {
                            var cell = new VoxelCell(this, (short)x, (short)y, templayers);
                            mCellGrid[x, y] = cell;
                        }
                    }
                    else
                    {
                        //Console.Error.WriteLine($"Cell[{x},{y}] is null");
                    }
                    progress?.Add(1);
                }
            }
            for (int x = xc - 1; x >= 0; --x)
            {
                for (int y = yc - 1; y >= 0; --y)
                {
                    var cell = mCellGrid[x, y];
                    if (cell != null)
                    {
                        cell.VoxelCellInit(this);
                        mAABB.Min.Z = Math.Min(mAABB.Min.Z, cell.GroundLayer.Downward);
                        mAABB.Max.Z = Math.Max(mAABB.Max.Z, cell.TopLayer.Upward);
                    }
                    progress?.Add(1);
                }
            }
        }
        public VoxelTerrain3D(InputStream inputT)
        {
            VoxelStream.Load(inputT, FILE_HEAD, out mFlags, input =>
            {
                this.mCFG = input.GetXmlObject<VoxelBuildConfig>();
                this.GridCellSize = input.GetF32();
                this.GridCellRadius = GridCellSize / 2f;
                this.XCount = input.GetS32();
                this.YCount = input.GetS32();
                this.TotalSizeX = XCount * GridCellSize;
                this.TotalSizeY = YCount * GridCellSize;
                this.TotalCellCount = input.GetS32();
                this.TotalLayerCount = input.GetS32();
                this.ResourceStartX = input.GetF32();
                this.ResourceStartY = input.GetF32();
                this.mAABB.Min.X = 0;
                this.mAABB.Min.Y = 0;
                this.mAABB.Max.X = TotalSizeX;
                this.mAABB.Max.Y = TotalSizeY;
                this.Palette.Load(input);
                this.mCellGrid = new VoxelCell[XCount, YCount];
                for (int x = XCount - 1; x >= 0; --x)
                {
                    for (int y = YCount - 1; y >= 0; --y)
                    {
                        if (input.GetBool())
                        {
                            var cell = new VoxelCell(this, (short)x, (short)y, input);
                            mCellGrid[x, y] = cell;
                        }
                    }
                }
                for (int x = XCount - 1; x >= 0; --x)
                {
                    for (int y = YCount - 1; y >= 0; --y)
                    {
                        var cell = mCellGrid[x, y];
                        if (cell != null)
                        {
                            cell.VoxelCellInit(this, input);
                            mAABB.Min.Z = Math.Min(mAABB.Min.Z, cell.GroundLayer.Downward);
                            mAABB.Max.Z = Math.Max(mAABB.Max.Z, cell.TopLayer.Upward);
                        }
                    }
                }
            });
        }
        public VoxelTerrain3D(VoxelTerrain3D src, Func<VoxelLayer, bool> selector, IRangeValue progress = null)
        {
            progress?.SetMax(src.XCount * src.YCount);
            progress?.SetMin(0);
            progress?.SetValue(0);
            this.mCFG = XmlUtil.CloneObject(src.mCFG);
            this.IsGZip = mCFG.IsGZip;
            this.GridCellSize = src.GridCellSize;
            this.GridCellRadius = src.GridCellRadius;
            this.XCount = src.XCount;
            this.YCount = src.YCount;
            this.TotalSizeX = src.TotalSizeX;
            this.TotalSizeY = src.TotalSizeY;
            this.ResourceStartX = src.ResourceStartX;
            this.ResourceStartY = src.ResourceStartY;
            this.mAABB = src.mAABB;
            this.mFlags = src.mFlags;
            this.Palette = src.Palette.Clone();
            this.mCellGrid = new VoxelCell[src.XCount, src.YCount];
            for (int x = src.XCount - 1; x >= 0; --x)
            {
                for (int y = src.YCount - 1; y >= 0; --y)
                {
                    var src_cell = src.GetVoxelCell(x, y);
                    if (src_cell != null)
                    {
                        var cell = new VoxelCell(this, src_cell, selector);
                        if (cell.LayerCount > 0)
                        {
                            this.mCellGrid[x, y] = cell;
                            this.TotalCellCount++;
                            this.TotalLayerCount += cell.LayerCount;
                        }
                    }
                    progress?.Add(1);
                }
            }
        }
        public void Save(OutputStream outputT)
        {
            VoxelStream.Save(outputT, FILE_HEAD, mFlags, output =>
            {
                output.PutXmlObject(this.mCFG);
                output.PutF32(this.GridCellSize);
                output.PutS32(this.XCount);
                output.PutS32(this.YCount);
                output.PutS32(this.TotalCellCount);
                output.PutS32(this.TotalLayerCount);
                output.PutF32(this.ResourceStartX);
                output.PutF32(this.ResourceStartY);
                this.Palette.Save(output);
                for (int x = XCount - 1; x >= 0; --x)
                {
                    for (int y = YCount - 1; y >= 0; --y)
                    {
                        output.PutBool(mCellGrid[x, y] != null);
                        if (mCellGrid[x, y] != null)
                        {
                            mCellGrid[x, y].Save(output); ;
                        }
                    }
                }
                for (int x = XCount - 1; x >= 0; --x)
                {
                    for (int y = YCount - 1; y >= 0; --y)
                    {
                        mCellGrid[x, y]?.SaveNext(output);
                    }
                }
            });
        }
        public override string ToString()
        {
            return $"Size={XCount},{YCount} : CellSize={GridCellSize} : TotalCell={TotalCellCount} : TotalLayer={TotalLayerCount} : {BuildConfig}";
        }
        protected bool CombineLayers(List<VoxelNodeData> layers, in BoundingBox aabb)
        {
            if (layers.Count == 1)
            {
                if (BuildConfig.IsIgnoreColor(layers[0].Color))
                {
                    return false;
                }
            }
            layers.Sort((a, b) => CMath.GetDirect(a.Upward - b.Upward));
            if (layers.Count > 1)
            {
                //按照上沿排序后，如果发现，下沿小于上沿，则将下沿设置为上沿
                for (var i = 0; i < layers.Count; i++)
                {
                    for (var j = i + 1; j < layers.Count; j++)
                    {
                        var down = layers[i];
                        var up = layers[j];
                        if (down != up)
                        {
                            if (up.Downward < down.Upward)
                            {
                                up.Downward = down.Upward;
                            }
                        }
                    }
                }
            }
            //切割地平线
            if (mCFG.ClipHorizon == VoxelClipHorizon.Combine)
            {
                var dz = mCFG.ClipHorizonAltitude;
                if (layers.Count > 0)
                {
                    for (int i = 0; i < layers.Count; i++)
                    {
                        var down = layers[i];
                        if (down.Upward < dz)
                        {
                            down.Upward = dz;
                        }
                        if (down.Downward < dz)
                        {
                            down.Downward = dz;
                        }
                    }
                }
            }
            else if (mCFG.ClipHorizon == VoxelClipHorizon.Drop)
            {
                var dz = mCFG.ClipHorizonAltitude;
                for (int i = layers.Count - 1; i >= 0; --i)
                {
                    var down = layers[i];
                    if (down.Upward < dz)
                    {
                        layers.RemoveAt(i);
                    }
                }
            }

            //处理过薄
            {
                var d = Math.Max(mCFG.VoxelMinHeight, 0);
                if (d > 0)
                {
                    for (int i = 0; i < layers.Count; i++)
                    {
                        var down = layers[i];
                        if (down.Downward > down.Upward - d)
                        {
                            down.Downward = down.Upward - d;
                        }
                    }
                }
            }

            //合并体素
            if (layers.Count > 1)
            {
                // combine
                if (mCFG.CombineDir == VoxelCombineDirection.Up2D)
                {
                    while (layers.Count > 1)
                    {
                        layers.Remove(0);
                    }
                }
                else if (mCFG.CombineDir == VoxelCombineDirection.Down2D)
                {
                    while (layers.Count > 1)
                    {
                        layers.Remove(layers.Count - 1);
                    }
                }
                else if (mCFG.CombineDir != VoxelCombineDirection.NA)
                {
                    while (pop_layers()) { }
                }
                bool pop_layers()
                {
                    var d = Math.Max(mCFG.VoxelMinDistance, 0);
                    for (int i = layers.Count - 1; i >= 1; --i)
                    {
                        var up = layers[i];
                        var down = layers[i - 1];
                        if (down.Upward + d >= up.Downward)
                        {
                            if (mCFG.CombineDir == VoxelCombineDirection.UpSameColor)
                            {
                                if (up.Color == down.Color)
                                {
                                    up.Downward = down.Downward;
                                    layers.Remove(down);
                                    return true;
                                }
                            }
                            else if (mCFG.CombineDir == VoxelCombineDirection.DownSameColor)
                            {
                                if (up.Color == down.Color)
                                {
                                    down.Upward = up.Upward;
                                    layers.Remove(up);
                                    return true;
                                }
                            }
                            else if (mCFG.CombineDir == VoxelCombineDirection.Up)
                            {
                                up.Downward = down.Downward;
                                layers.Remove(down);
                                return true;
                            }
                            else if (mCFG.CombineDir == VoxelCombineDirection.Down)
                            {
                                down.Upward = up.Upward;
                                layers.Remove(up);
                                return true;
                            }
                        }
                        else if (down.Upward > up.Downward)
                        {
                            throw new Exception($"层级错误[]");
                        }
                    }
                    return false;
                }
            }

            // clip color
            for (int i = layers.Count - 1; i >= 0; --i)
            {
                if (BuildConfig.IsIgnoreColor(layers[i].Color))
                {
                    layers.RemoveAt(i);
                }
            }

            if (mCFG.ClipHorizon == VoxelClipHorizon.AABB)
            {
                var dz = aabb.Min.Z;
                if (layers.Count > 0)
                {
                    var last = layers[0];
                    last.Downward = dz;
                }
            }

            return layers.Count > 0;
        }
        #endregion
        protected override void Disposing()
        {
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
            if (x >= XCount) { x = XCount - 1; ret = true; }
            if (y >= YCount) { y = YCount - 1; ret = true; }
            return ret;
        }
        public bool TryUpdatePos(ref Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell.GetLayerByAltitude(pos.Z);
                if (layer != null)
                {
                    var d = pos.Z - layer.Upward;
                    if (d >= 0)
                    {
                        return true;
                    }
                    else if (-d <= BuildConfig.StepIntercept)
                    {
                        pos.Z = layer.Upward;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            layer = null;
            return false;
        }

        public bool TryIntersectMapByPos(in Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell.GetLayerByAltitude(pos.Z);
                if (layer != null)
                {
                    return pos.Z < layer.Upward;
                }
            }
            layer = null;
            return false;
            //return path_finder.Terrain.TouchMapByPos(x, y);
        }


        public bool TryTestInAirByPos(in Vector3 pos, out VoxelLayer layer)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                layer = cell.GetLayerByAltitude(pos.Z);
                if (layer != null)
                {
                    return pos.Z > layer.Upward;
                }
                else
                {
                    layer = cell.GroundLayer;
                    return false;
                }
            }
            layer = null;
            return false;
            //return path_finder.Terrain.TouchMapByPos(x, y);
        }

        //--------------------------------------------------------------------------------------------------------------
        public bool TryGetVoxelCell(int x, int y, out VoxelCell cell)
        {
            if (x >= 0 && x < XCount && y >= 0 && y < YCount)
            {
                cell = mCellGrid[x, y];
                return cell != null;
            }
            cell = null;
            return false;
        }
        public bool TryGetVoxelLayer(int x, int y, int layerIndex, out VoxelCell cell, out VoxelLayer layer)
        {
            if (x >= 0 && x < XCount && y >= 0 && y < YCount)
            {
                cell = mCellGrid[x, y];
                if (cell != null)
                {
                    layer = cell.GetLayer(layerIndex);
                    return layer != null;
                }
            }
            cell = null;
            layer = null;
            return false;
        }
        public bool TryGetVoxelLayerByPos(in Vector3 pos, out VoxelCell cell, out VoxelLayer layer, bool ground = false)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out cell))
            {
                layer = cell.GetLayerByAltitude(pos.Z);
                if (ground && layer == null)
                {
                    layer = cell.GroundLayer;
                }
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryGetVoxelLayerByAltitude(int x, int y, float z, out VoxelCell cell, out VoxelLayer layer)
        {
            if (TryGetVoxelCell(x, y, out cell))
            {
                layer = cell.GetLayerByAltitude(z);
                if (layer == null)
                {
                    layer = cell.GroundLayer;
                }
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryGetVoxelLayerByObject(ref Vector3 vector, out VoxelCell cell, out VoxelLayer layer)
        {
            WorldPosToVoxel(vector.X, vector.Y, out var x, out var y);
            var fix = TryClampVoxelPos(ref x, ref y);
            if (TryGetVoxelCell(x, y, out cell))
            {
                if (fix)
                {
                    var center = cell.GetCenterPos();
                    vector.X = center.X;
                    vector.Y = center.Y;
                }
                layer = cell.GetLayerAndStandOn(ref vector);
                return layer != null;
            }
            layer = null;
            return false;
        }
        public bool TryGetNextStep(VoxelLayer current, int ox, int oy, out VoxelLayer next)
        {
            int dx = current.OwnerCell.X + ox;
            int dy = current.OwnerCell.Y + oy;
            if (TryGetVoxelCell(dx, dy, out var target))
            {
                var step = target.GetLayerByStep(current.Upward, mCFG.StepIntercept);
                next = step;
                return step != null;
            }
            next = null;
            return false;
        }
        public bool TryGetNextStep(VoxelLayer current, VoxelCell target, out VoxelLayer next)
        {
            var step = target.GetLayerByStep(current.Upward, mCFG.StepIntercept);
            next = step;
            return step != null;
        }

        //--------------------------------------------------------------------------------------------------------------
        public VoxelCell GetVoxelCell(int x, int y)
        {
            return mCellGrid[x, y];
        }
        public VoxelLayer GetVoxelLayer(int x, int y, int layer)
        {
            var cell = mCellGrid[x, y];
            return cell.GetLayer(layer);
        }
        public VoxelLayer GetVoxelLayerByPos(in Vector3 pos, bool ground = false)
        {
            WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                var layer = cell.GetLayerByAltitude(pos.Z);
                if (ground && layer == null)
                {
                    return cell.GroundLayer;
                }
                return layer;
            }
            return null;
        }
        public VoxelLayer GetVoxelLayerByAltitude(int x, int y, float z)
        {
            var cell = mCellGrid[x, y];
            return cell.GetLayerByAltitude(z);
        }
        /// <summary>
        /// 一定能返回一个体素坐标。
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public VoxelLayer GetVoxelLayerByObject(ref Vector3 vector)
        {
            WorldPosToVoxel(vector.X, vector.Y, out var x, out var y);
            var fix = TryClampVoxelPos(ref x, ref y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                if (fix)
                {
                    var center = cell.GetCenterPos();
                    vector.X = center.X;
                    vector.Y = center.Y;
                }
                return cell.GetLayerAndStandOn(ref vector);
            }
            return null;
        }
        //--------------------------------------------------------------------------------------------------
        /// <summary>
        /// 体素是否可行走
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public virtual bool IsWalkable(VoxelLayer layer)
        {
            //if (layer.GetNextNodeCount() <= 0) return false;
            if (BuildConfig.IsWalkableColor(layer.Color))
            {
                return true;
            }
            return false;
        }
        //--------------------------------------------------------------------------------------------------
        #region Utils
        //--------------------------------------------------------------------------------------------------------------
        public VoxelCell GetRandomCell(Random random)
        {
            int sx = random.Next() % XCount;
            int sy = random.Next() % YCount;
            for (int x = XCount - 1; x >= 0; --x)
            {
                for (int y = YCount - 1; y >= 0; --y)
                {
                    var cell = GetVoxelCell(CMath.CycNum(x + sx, XCount), CMath.CycNum(y + sy, YCount));
                    if (cell != null) return cell;
                }
            }
            return null;
        }
        public VoxelLayer GetRandomLayer(Random random)
        {
            int sx = random.Next() % XCount;
            int sy = random.Next() % YCount;
            for (int x = XCount - 1; x >= 0; --x)
            {
                for (int y = YCount - 1; y >= 0; --y)
                {
                    var cell = GetVoxelCell(CMath.CycNum(x + sx, XCount), CMath.CycNum(y + sy, YCount));
                    if (cell != null)
                    {
                        var la = random.Next() % cell.LayerCount;
                        return cell.GetLayer(la);
                    }
                }
            }
            return null;
        }
        public VoxelLayer GetRandomLayer(Random random, int x, int y, int distance)
        {
            int sx = random.Next() % distance;
            int sy = random.Next() % distance;
            for (int ix = distance - 1; ix >= 0; --ix)
            {
                for (int iy = distance - 1; iy >= 0; --iy)
                {
                    if (TryGetVoxelCell(
                        x + CMath.CycNum(sx + ix, distance),
                        y + CMath.CycNum(sy + iy, distance),
                        out var cell))
                    {
                        var la = random.Next() % cell.LayerCount;
                        return cell.GetLayer(la);
                    }
                }
            }
            return null;
        }
        public VoxelLayer GetRandomLayerByPos(Random random, in Vector3 pos, float radius)
        {
            var x = (int)(pos.X / GridCellSize);
            var y = (int)(pos.Y / GridCellSize);
            int distance = (int)(radius / GridCellSize);
            int sx = random.Next() % distance;
            int sy = random.Next() % distance;
            for (int ix = distance - 1; ix >= 0; --ix)
            {
                for (int iy = distance - 1; iy >= 0; --iy)
                {
                    if (TryGetVoxelCell(
                        x + CMath.CycNum(sx + ix, distance),
                        y + CMath.CycNum(sy + iy, distance),
                        out var cell))
                    {
                        var ret = cell.GetLayerByAltitude(pos.Z);
                        if (ret != null)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }
        public Vector3 GetUpwardRandomPos(VoxelLayer src, Random random)
        {
            return new Vector3(
               (float)(src.OwnerCell.X * GridCellSize + GridCellSize * random.NextDouble()),
               (float)(src.OwnerCell.Y * GridCellSize + GridCellSize * random.NextDouble()),
               src.Upward);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------
        #region ForEachCells
        public delegate bool VoxelLayerBreakPredicate<ST>(VoxelLayer layer, ST st);
        public VoxelLayer ForEachLayers<ST>(ST st, VoxelLayerBreakPredicate<ST> action)
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
                            if (action(layer, st))
                            {
                                return layer;
                            }
                        }
                    }
                }
            }
            return null;
        }
        public delegate void VoxelLayerChunkAction<ST>(List<VoxelLayer> layers, Location2D location, ST st);
        public void ForEachChunkLayers<ST>(ST st, VoxelLayerChunkAction<ST> action, int slice)
        {
            var totalSize = new Size2D(XCount, YCount);
            totalSize.FoeEachSlice(slice, sloc =>
            {
                var layers = new List<VoxelLayer>();
                for (int x = slice - 1; x >= 0; --x)
                {
                    for (int y = slice - 1; y >= 0; --y)
                    {
                        if (TryGetVoxelCell(sloc.X + x, sloc.Y + y, out var cell))
                        {
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                var layer = cell.GetLayer(i);
                                layers.Add(layer);
                            }
                        }
                    }
                }
                action(layers, sloc, st);
            });
        }
        /// <summary>
        /// 遍历Layer平面，通常为一个矩形平台
        /// </summary>
        /// <returns></returns>
        public VoxelLayer[,] GetVoxelLayersPlane(VoxelLayer start, int w, int h)
        {
            if (w == 1 && h == 1) return new VoxelLayer[1, 1] { { start } };
            var matrix = new VoxelLayer[w, h];
            matrix[0, 0] = start;
            {
                var n = start;
                for (int y = 1; y < h; y++)
                {
                    n = matrix[0, y] = n.GetNextNode(0, 1);
                    if (n == null)
                    {
                        return null;
                    }
                }
            }
            for (int y = 0; y < h; y++)
            {
                var n = matrix[0, y];
                for (int x = 1; x < w; x++)
                {
                    n = matrix[x, y] = n.GetNextNode(1, 0);
                    if (n == null)
                    {
                        return null;
                    }
                }
            }
            return matrix;
        }


        public VoxelCell ForEachCells<ST>(ST st, BreakPredicate<VoxelCell, ST> action)
        {
            for (int x = this.XCount - 1; x >= 0; --x)
            {
                for (int y = this.YCount - 1; y >= 0; --y)
                {
                    var cell = mCellGrid[x, y];
                    if (cell != null)
                    {
                        if (action(cell, st))
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }
        public VoxelCell ForEachCellsRect<ST>(ST st, int x1, int y1, int x2, int y2, BreakPredicate<VoxelCell, ST> action)
        {
            x1 = Math.Max(x1, 0);
            y1 = Math.Max(y1, 0);
            x2 = Math.Min(x2, this.XCount - 1);
            y2 = Math.Min(y2, this.YCount - 1);
            for (int ix = x1; ix <= x2; ix++)
            {
                for (int iy = y1; iy <= y2; iy++)
                {
                    var cell = mCellGrid[ix, iy];
                    if (cell != null)
                    {
                        if (action(cell, st))
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }
        public VoxelCell ForEachCellsRectF<ST>(ST st, float x1, float y1, float x2, float y2, BreakPredicate<VoxelCell, ST> action)
        {
            WorldPosToVoxel(x1, y1, out var bx1, out var by1);
            WorldPosToVoxel(x2, y2, out var bx2, out var by2);
            return ForEachCellsRect(st, bx1, by1, bx2, by2, action);
        }

        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        public void ForEachCellsRayStep<ST>(ref ST input, ref Vector2 pos, Vector2 target, ForEachCellsRayStepPredicate<VoxelCell, ST> action, bool breakOutBounds = true)
        {
            mCellGrid.ForEachCellsRayStepPloar(ref input, ref pos, target, GridCellSize, action, null, breakOutBounds);
        }
        public void ForEachCellsRayStep<ST>(ref ST input, ref Vector2 pos, Vector2 target, BreakPredicate<VoxelCell, int, int, ST> action, bool breakOutBounds = true)
        {
            var tuple = (input, action);
            mCellGrid.ForEachCellsRayStepPloar(
                ref tuple,
                ref pos, target, GridCellSize,
                static (st, cell, cx, cy, current) => st.action(cell, cx, cy, st.input), null,
                breakOutBounds);
        }

        /// <summary>
        /// 线性延伸，扫描线段经过的所有节点，不进行碰撞检测
        /// </summary>
        public void ForEachCellsRayStepPloar<ST>(ref ST input, ref Vector2 pos, float dir, float len, ForEachCellsRayStepPredicate<VoxelCell, ST> action, bool breakOutBounds = true)
        {
            mCellGrid.ForEachCellsRayStepPloar(ref input, ref pos, dir, len, GridCellSize, action, null, breakOutBounds);
        }
        public void ForEachCellsRayStepPloar<ST>(ST input, ref Vector2 pos, float dir, float len, BreakPredicate<VoxelCell, int, int, ST> action, bool breakOutBounds = true)
        {
            var tuple = (input, action);
            mCellGrid.ForEachCellsRayStepPloar(ref tuple, ref pos, dir, len, GridCellSize,
                static (st, cell, cx, cy, current) => st.action(cell, cx, cy, st.input), null,
                breakOutBounds);
        }

        /// <summary>
        /// 贴地闪现移动
        /// </summary>
        /// <returns>阻挡</returns>
        public bool TryBlinkToTarget2D(VoxelLayer src, VoxelLayer dst, out VoxelLayer currentLayer, out Vector3 currentPos, float height = 0)
        {
            return TryBlinkToTarget2D(src, src.UpwardCenterPos, dst, dst.UpwardCenterPos, out currentLayer, out currentPos, height);
        }

        /// <summary>
        /// 贴地闪现移动
        /// </summary>
        /// <returns>阻挡</returns>
        public bool TryBlinkToTarget2D(
            VoxelLayer src, in Vector3 srcP,
            VoxelLayer dst, in Vector3 dstP,
            out VoxelLayer currentLayer,
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
                step = BuildConfig.StepIntercept
            };
            mCellGrid.ForEachCellsRayStepPloar(ref tuple, ref pos, target, GridCellSize,
                static (tuple, cell, cx, cy, current) =>
                {
                    if (cell == null)
                    {
                        return true;
                    }
                    Vector3 p = current;
                    p.Z = tuple.currentPos.Z;
                    if (cell.TryStandOn(ref p, tuple.height, tuple.step, out var cross))
                    {
                        return false;
                    }
                    return true;
                },
                static (ref BlinkState tuple, Vector2 current) =>
                {
                    tuple.currentPos.X = current.X;
                    tuple.currentPos.Y = current.Y;
                    if (tuple.terrain.TryGetVoxelLayerByObject(ref tuple.currentPos, out var cell, out var layer))
                    {
                        tuple.currentPos.Z = layer.Upward;
                        tuple.currentLayer = layer;
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
        private struct BlinkState
        {
            public VoxelTerrain3D terrain;
            public VoxelLayer currentLayer;
            public Vector3 currentPos;
            public float height;
            public float step;
        }

        //--------------------------------------------------------------------------------------------------

        public bool ForEachByShape<ST>(IShape shape, ST st, ForEachPredicate<ST, VoxelLayer> action)
        {
            var mmap = new GridTerrain()
            {
                GridSize = GridCellSize,
                XCount = XCount,
                YCount = YCount,
                include = true,
            };
            var tuple = new ValueTuple<VoxelTerrain3D, ST, ForEachPredicate<ST, VoxelLayer>, IShape>(this, st, action, shape);
            return mmap.ForEachByShape(shape, tuple, static (tuple, bx, by) =>
            {
                var astar = tuple.Item1;
                var st = tuple.Item2;
                var action = tuple.Item3;
                var shape = tuple.Item4;
                var cell = astar.GetVoxelCell(bx, by);
                if (cell != null)
                {
                    var zpos = shape.Position;
                    var layer = cell.GetLayerByAltitude(zpos.Z);
                    if (layer != null)
                    {
                        //var mapnode = astar.GetMapNode(layer);
                        //if (mapnode != null)
                        {
                            return action(st, layer);
                        }
                    }
                }
                return false;
            });
        }

        #endregion

        //--------------------------------------------------------------------------------------------------
        #region Motion
        public bool TryMoveTo(ref Vector3 target, out VoxelLayer touchLayer)
        {
            touchLayer = null;
            var step = BuildConfig.StepIntercept;
            WorldPosToVoxel(target.X, target.Y, out var x, out var y);
            if (TryGetVoxelCell(x, y, out var cell))
            {
                touchLayer = cell.GetLayerByAltitude(target.Z);
                if (touchLayer != null)
                {
                    if (target.Z >= touchLayer.Upward - step)
                    {
                        if (target.Z < touchLayer.Upward)
                        {
                            target.Z = touchLayer.Upward;
                        }
                        return true;
                    }
                }
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
            var step = BuildConfig.StepIntercept;
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
                    if (nextX != null && nextX.TryStandOn(ref pos, step))
                    {
                        layer = nextX;
                        pos.X = target.X;
                        pos.Z = nextX.Upward;
                        return true;
                    }
                    //尝试单边//
                    var nextY = layer.GetNextNode(0, dy);
                    if (nextY != null && nextY.TryStandOn(ref pos, step))
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
                if (TryGetVoxelCell(tx, ty, out var next_cell))
                {
                    //下格无碰撞//
                    if (next_cell.TryStandOn(ref pos, step, out var next))
                    {
                        layer = next;
                        pos.X = target.X;
                        pos.Y = target.Y;
                        pos.Z = next.Upward;
                        return true;
                    }
                }
            }
            //不可行走面//
            return false;
        }
        #endregion
        //--------------------------------------------------------------------------------------------------
        #region ITerrain
        float ITerrain.StepIntercept => BuildConfig.StepIntercept;
        TerrainColor[] ITerrain.ColorPalette => Palette.Colors;
        ITerrainLayer ITerrain.GetVoxelLayerByObject(ref Vector3 pos)
        {
            return this.GetVoxelLayerByObject(ref pos);
        }
        ITerrainLayer ITerrain.GetVoxelLayerByPos(in Vector3 pos)
        {
            return this.GetVoxelLayerByPos(in pos);
        }
        bool ITerrain.TryUpdatePos(ref Vector3 pos, out ITerrainLayer layer)
        {
            var ret = this.TryUpdatePos(ref pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryGetVoxelLayerByPos(in Vector3 pos, out ITerrainLayer layer, bool ground)
        {
            var ret = this.TryGetVoxelLayerByPos(in pos, out var _cell, out var _layer, ground);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryGetVoxelLayerByObject(ref Vector3 vector, out ITerrainLayer layer)
        {
            var ret = this.TryGetVoxelLayerByObject(ref vector, out var _cell, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryIntersectMapByPos(in Vector3 pos, out ITerrainLayer layer)
        {
            var ret = this.TryIntersectMapByPos(in pos, out var _layer);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveTo(ref Vector3 target, out ITerrainLayer touchLayer)
        {
            var ret = this.TryMoveTo(ref target, out var _layer);
            touchLayer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, float direction, float distance)
        {
            var _layer = layer as VoxelLayer;
            var ret = this.TryMoveSpellOnFloor(ref pos, ref _layer, direction, distance);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, in Vector2 target)
        {
            var _layer = layer as VoxelLayer;
            var ret = this.TryMoveSpellOnFloor(ref pos, ref _layer, target);
            layer = _layer;
            return ret;
        }
        bool ITerrain.TryTestInAirByPos(in Vector3 pos, out ITerrainLayer layer)
        {
            var ret = this.TryTestInAirByPos(in pos, out var _layer);
            layer = _layer;
            return ret;
        }

        bool ITerrain.RayCast(in Ray ray, out Vector3? hitPoint, out ITerrainLayer hitLayer)
        {
            hitLayer = VoxelRayCast.RayCastVoxel(this, new RayCast()
            {
                center = ray.Position,
                normal = ray.Direction,
                distance = this.TotalSizeY * this.TotalSizeX,
            }, out var _hitPoint);

            if (hitLayer != null)
            {
                hitPoint = _hitPoint;
                return true;
            }
            hitPoint = null;
            return false;
        }
        #endregion
    }
    //--------------------------------------------------------------------------------------------------


}
