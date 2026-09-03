using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using DeepCore.XCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepCore.Geometry.Dummy
{
    public class DummyTerrainWorld : Disposable, ITerrainWorld
    {
        //----------------------------------------------------------------------------------------------------------------------------
        private float gridSize, baseLine, top;
        private int totalW, totalH;
        private DummyTerrain terrain;
        private DummyAstar astar;
        public ITerrain Terrain => terrain;
        public ITerrainAstar PathFinder => astar;
        public DummyTerrainWorld(float gridSize, int totalW, int totalH, float baseLine = 1, float top = 100)
        {
            this.baseLine = baseLine;
            this.gridSize = gridSize;
            this.totalW = totalW;
            this.totalH = totalH;
            this.top = top;
            this.astar = new DummyAstar(this);
            this.terrain = new DummyTerrain(this);
        }
        protected override void Disposing() { }
        public ITerrainAgent CreateAgent() => new DummyAgent();
        public ITerrainAgent CreateAgent(Vector3 pos) => new DummyAgent(pos);
        public ITerrainAgent CreateAgent(ITerrainLayer pos) => new DummyAgent((DummyLayer)pos);
        //----------------------------------------------------------------------------------------------------------------------------

        public class DummyLayer : ITerrainLayer
        {
            public float Top => float.MaxValue;
            public float Upward => world.baseLine;
            public float Downward => world.baseLine - 1;
            public float Height => 1f;
            public bool IsPlane => true;
            public byte ColorIndex => 0;
            public TerrainColor Color => TerrainColor.Zero;
            public Vector3 UpwardCenterPos => pos;

            public readonly DummyTerrainWorld world;
            public Vector3 pos;
            public int bx;
            public int by;
            public DummyLayer(DummyTerrainWorld world, int bx, int by)
            {
                this.world = world;
                this.pos = new Vector3(world.gridSize * bx, world.gridSize * by, world.baseLine);
                this.bx = bx;
                this.by = by;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public class DummyTerrain : Disposable, ITerrain
        {
            private readonly DummyTerrainWorld world;
            public float GridCellSize { get; }
            public float TotalSizeX { get; }
            public float TotalSizeY { get; }
            public float StepIntercept { get; }
            public float ResourceStartX { get; }
            public float ResourceStartY { get; }
            public TerrainColor[] ColorPalette { get; }
            public int XCount => world.totalW;
            public int YCount => world.totalH;
            public BoundingBox AABB { get; }
            private DummyLayer[,] matrix;
            public DummyTerrain(DummyTerrainWorld world)
            {
                this.world = world;
                this.GridCellSize = world.gridSize;
                this.TotalSizeX = world.gridSize * world.totalW;
                this.TotalSizeY = world.gridSize * world.totalH;
                this.StepIntercept = 1f;
                this.ResourceStartX = 0;
                this.ResourceStartY = 0;
                this.ColorPalette = new TerrainColor[] { };
                this.AABB = new BoundingBox(
                    new Vector3(0, 0, world.baseLine - 1),
                    new Vector3(TotalSizeX, TotalSizeY, world.top));
                this.matrix = new DummyLayer[world.totalW, world.totalH];
                CUtils.ForEach2D(world.totalW, world.totalH, (ix, iy) =>
                {
                    this.matrix[ix, iy] = new DummyLayer(world, ix, iy);
                });
            }
            protected override void Disposing()
            {
            }
            public void WorldPosToVoxel(float x, float y, out int bx, out int by)
            {
                bx = (int)(x / world.gridSize);
                by = (int)(y / world.gridSize);
            }
            public Vector3 WorldPosToUpwardCenterPos(Vector3 pos, out int bx, out int by)
            {
                bx = (int)(pos.X / world.gridSize);
                by = (int)(pos.Y / world.gridSize);
                return new Vector3(bx * world.gridSize + world.gridSize / 2, by * world.gridSize + world.gridSize / 2, world.baseLine);
            }
            public bool TryClampVoxelPos(ref int x, ref int y)
            {
                bool ret = false;
                if (x < 0) { x = 0; ret = true; }
                if (y < 0) { y = 0; ret = true; }
                if (x >= world.totalW) { x = world.totalW - 1; ret = true; }
                if (y >= world.totalH) { y = world.totalH - 1; ret = true; }
                return ret;
            }
            public bool TryGetVoxelCell(int x, int y, out DummyLayer cell)
            {
                if (x >= 0 && x < XCount && y >= 0 && y < YCount)
                {
                    cell = matrix[x, y];
                    return cell != null;
                }
                cell = default;
                return false;
            }
            public ITerrainLayer GetVoxelLayerByObject(ref Vector3 pos)
            {
                WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
                var fix = TryClampVoxelPos(ref x, ref y);
                if (TryGetVoxelCell(x, y, out var cell))
                {
                    if (fix)
                    {
                        var center = cell.pos;
                        pos.X = center.X;
                        pos.Y = center.Y;
                    }
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
            public bool RayCast(in Ray ray, out Vector3? hitPoint, out ITerrainLayer hitLayer)
            {
                var raycast = new RayCast()
                {
                    center = ray.Position,
                    normal = ray.Direction,
                    distance = this.TotalSizeY * this.TotalSizeX,
                };
                var ray_touch = DeepCore.Geometry.RayCast.RayPlaneIntersection(raycast.center, raycast.normal, Vector3.Zero, Vector3.UnitZ);
                if (CMath.IncludeRectPointW(
                    0, 0,
                    TotalSizeX,
                    TotalSizeY,
                    ray_touch.X,
                    ray_touch.Y))
                {
                    hitPoint = ray_touch;
                    hitLayer = GetVoxelLayerByPos(ray_touch);
                    return true;
                }
                hitPoint = null;
                hitLayer = null;
                return false;
            }
            public bool TryGetVoxelLayerByObject(ref Vector3 pos, out ITerrainLayer layer)
            {
                WorldPosToVoxel(pos.X, pos.Y, out var x, out var y);
                var fix = TryClampVoxelPos(ref x, ref y);
                if (TryGetVoxelCell(x, y, out var cell))
                {
                    if (fix)
                    {
                        var center = cell.pos;
                        pos.X = center.X;
                        pos.Y = center.Y;
                    }
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
                //return path_finder.Terrain.TouchMapByPos(x, y);
            }
            public bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, float direction, float distance)
            {
                VectorHelper.MovePolar(ref pos, direction, distance);
                if (pos.Z < layer.Upward) { pos.Z = layer.Upward; }
                return true;
            }
            public bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, in Vector2 target)
            {
                pos = target;
                if (pos.Z < layer.Upward) { pos.Z = layer.Upward; }
                return true;
            }
            public bool TryMoveTo(ref Vector3 target, out ITerrainLayer touchLayer)
            {
                touchLayer = null;
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
                    else if (-d <= 0.1f)
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
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public class DummyWayPoint : ITerrainWayPoint
        {
            public DummyWayPoint Next;
            public Vector3 Position { get; set; }
            public float TotalDistance
            {
                get
                {
                    if (Next != null)
                    {
                        return Vector3.Distance(Next.Position, Position);
                    }
                    return 0f;
                }
            }
            public DummyWayPoint(DummyTerrainWorld world, Vector3 pos)
            {
                this.Position = pos;
            }
            public IEnumerator<ITerrainWayPoint> GetEnumerator()
            {
                return new WayPointIterator<ITerrainWayPoint>(this);
            }
            public void LinkNext(ITerrainWayPoint n)
            {
                this.Next = n as DummyWayPoint;
            }
            public bool PosEquals(ITerrainWayPoint o)
            {
                if (o is DummyWayPoint od)
                {
                    return od.Position.Equals(Position);
                }
                return false;
            }
            IEnumerator IEnumerable.GetEnumerator() => new WayPointIterator<ITerrainWayPoint>(this);
            ITerrainWayPoint ITerrainWayPoint.Next => this.Next;
            IWayPoint IWayPoint.Next => this.Next;
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public class DummyAstar : ITerrainAstar
        {
            public readonly DummyTerrainWorld world;
            public int FindPathStepLimit { get; set; }
            public DummyAstar(DummyTerrainWorld world)
            {
                this.world = world;
            }
            public void Dispose()
            {
            }
            public bool FillMapBlockByShape(IShape shape, bool block)
            {
                return false;
            }
            public ITerrainWayPoint FindPathByLayer(ITerrainLayer src, ITerrainLayer dst)
            {
                if (src == null) return null;
                if (dst == null) return null;
                var srcP = new DummyWayPoint(world, src.UpwardCenterPos);
                var dstP = new DummyWayPoint(world, src.UpwardCenterPos);
                srcP.Next = dstP;
                return srcP;
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, Vector3 dstP)
            {
                var srcWP = new DummyWayPoint(world, srcP);
                var dstWP = new DummyWayPoint(world, dstP);
                srcWP.Next = dstWP;
                return srcWP;
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, ITerrainLayer dst, Vector3 dstP)
            {
                var srcWP = new DummyWayPoint(world, srcP);
                var dstWP = new DummyWayPoint(world, dstP);
                srcWP.Next = dstWP;
                return srcWP;
            }
            public ITerrainWayPoint FindPathByPos(Vector3 srcP, Vector3 dstP)
            {
                var srcWP = new DummyWayPoint(world, srcP);
                var dstWP = new DummyWayPoint(world, dstP);
                srcWP.Next = dstWP;
                return srcWP;
            }
            public IEnumerable<ITerrainMapNode> GetBlockMapNodes()
            {
                return [];
            }
            public bool GetMapBlockByPos(Vector3 srcP, out ITerrainMapNode mapnode)
            {
                mapnode = null;
                return false;
            }
            public bool IsMapNodeBlock(ITerrainMapNode mapnode)
            {
                return false;
            }
            public bool TestCross(IMapNode src, IMapNode dst)
            {
                return true;
            }
        }


        public class DummyAgent : ITerrainAgent
        {
            private DummyTerrainWorld world;
            private DummyLayer currentLayer;
            private Vector3 currentPos;
            private float zspeed = 0;
            private float height = 1.8f;
            private float gravity = 10f;
            public DummyTerrainWorld World { get => world; }
            public DummyLayer CurrentLayer { get => currentLayer; }
            public Vector3 Position { get => currentPos; }
            public float X { get => currentPos.X; }
            public float Y { get => currentPos.Y; }
            public float Z { get => currentPos.Z; }
            public float? LandAirDistance => Z - CurrentLayer.Upward;
            public bool IsInTheAir => (Z - CurrentLayer.Upward) >= 0.1f;
            public float Height { get => height; set { height = value; } }
            public float SpeedZ { get => zspeed; set { zspeed = value; } }
            public float Gravity { get => this.gravity; set { this.gravity = value; } }
            public bool MoveKeepInColor { get; set; } = false;
            public bool IgnoreTop { get; set; } = true;
            public DummyAgent()
            {
            }
            public DummyAgent(Vector3 pos)
            {
                this.currentPos = pos;
            }
            public DummyAgent(DummyLayer pos)
            {
                this.currentLayer = pos;
                this.currentPos = pos.UpwardCenterPos;
            }
            public void Update(float intervalMS)
            {
                if (currentLayer == null)
                {
                    return;
                }
                if (zspeed != 0)
                {
                    currentPos.Z += CMath.GetSpeedDistance(intervalMS, zspeed);
                }
                if (gravity != 0)
                {
                    if (currentPos.Z != currentLayer.Upward)
                    {
                        zspeed -= CMath.GetSpeedDistance(intervalMS, gravity);
                    }
                }
                else
                {
                    zspeed = 0;
                }
                if (currentPos.Z <= currentLayer.Upward)
                {
                    currentPos.Z = currentLayer.Upward;
                    if (zspeed != 0)
                    {
                        event_OnFallenDown?.Invoke(this, zspeed);
                        zspeed = 0;
                    }
                }
                if (currentPos.Z > currentLayer.Top - this.height)
                {
                    currentPos.Z = Math.Max(currentLayer.Top - this.height, currentLayer.Upward);
                    if (zspeed != 0)
                    {
                        event_OnBumpHead?.Invoke(this, zspeed);
                        zspeed = 0;
                    }
                }
            }
            public DummyAgent Clone()
            {
                return new DummyAgent()
                {
                    world = this.world,
                    currentLayer = this.currentLayer,
                    currentPos = this.currentPos,
                    zspeed = this.zspeed,
                    height = this.height,
                    gravity = this.gravity,
                };
            }
            public void EnterWorld(DummyTerrainWorld world)
            {
                var oldLayer = currentLayer;
                this.world = world;
                this.currentLayer = world.terrain.GetVoxelLayerByObject(ref currentPos) as DummyLayer;
                if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
            }
            public void LeaveWorld()
            {
                var oldLayer = currentLayer;
                this.world = null;
                this.currentLayer = null;
                if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
            }
            public void Transport(DummyLayer layer)
            {
                var oldLayer = currentLayer;
                this.currentPos = layer.UpwardCenterPos;
                this.currentLayer = layer;
                if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
            }
            public void Transport(Vector3 pos, DummyLayer layer)
            {
                var oldLayer = currentLayer;
                this.currentPos = pos;
                this.currentLayer = layer;
                if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
            }
            public void Transport(in Vector3 pos)
            {
                var oldLayer = currentLayer;
                if (world != null)
                {
                    world.terrain.WorldPosToVoxel(currentPos.X, currentPos.Y, out var svx, out var svy);
                    world.terrain.WorldPosToVoxel(pos.X, pos.Y, out var dvx, out var dvy);
                    this.currentPos = pos;
                    if (svx != dvx || svy != dvy || currentLayer == null)
                    {
                        this.currentLayer = world.terrain.GetVoxelLayerByObject(ref currentPos) as DummyLayer;
                        if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
                    }
                }
                else
                {
                    this.currentPos = pos;
                }
            }
            public void Jump(float speed)
            {
                this.zspeed = speed;
            }
            public void Fly(float zoffset)
            {
                FlyTo(this.Z + zoffset);
            }
            public void FlyTo(float dz)
            {
                if (currentLayer != null)
                {
                    dz = Math.Max(dz, currentLayer.Upward);
                    dz = Math.Min(dz, currentLayer.Top);
                }
                this.currentPos.Z = dz;
            }
            public AgentMoveResult TryMoveToPath(ref DummyWayPoint path, float step, bool land)
            {
                if (path == null) return AgentMoveResult.Blocked;
                var p = path.Position;
                var oldp = this.Position;
                var distance = Vector2.Distance(p, oldp);
                var ret = TryMoveTo(p, Math.Min(step, distance), land);
                if (ret == AgentMoveResult.MoveArrived)
                {
                    var mlen = Vector2.Distance(oldp, Position);
                    if (mlen < step)
                    {
                        path = path.Next;
                        if (path != null)
                        {
                            step = step - mlen;
                            p = path.Position;
                            ret = TryMoveTo(p, step, land);
                        }
                    }
                }
                if (ret == AgentMoveResult.Blocked)
                {
                    path = null;
                }
                return ret;
            }

            /// <summary>
            /// 目标移动
            /// </summary>
            /// <param name="target"></param>
            /// <param name="step"></param>
            /// <param name="land"></param>
            /// <returns></returns>
            public AgentMoveResult TryMoveTo(Vector3 target, float step, bool land)
            {
                var distance = Vector2.Distance(target, currentPos);
                var direction = CMath.GetDegree(currentPos.X, currentPos.Y, target.X, target.Y);
                var dx = target.X - currentPos.X;
                var dy = target.Y - currentPos.Y;
                var ox = (float)(Math.Cos(direction) * step);
                var oy = (float)(Math.Sin(direction) * step);
                if (distance <= step)
                {
                    var result = TryMoveOffset(new Vector2(dx, dy), land);
                    if (result == AgentMoveResult.MoveSmooth)
                    {
                        result = AgentMoveResult.MoveArrived;
                    }
                    return result;
                }
                return TryMoveOffset(new Vector2(ox, oy), land);
            }
            /// <summary>
            /// 指向移动
            /// </summary>
            /// <param name="direction"></param>
            /// <param name="step"></param>
            /// <param name="land"></param>
            /// <returns>没有MoveArrived</returns>
            public AgentMoveResult TryMoveLerp(float direction, float step, bool land)
            {
                float ox = (float)(Math.Cos(direction) * step);
                float oy = (float)(Math.Sin(direction) * step);
                return TryMoveToNext3D(new Vector2(currentPos.X + ox, currentPos.Y + oy), land);
            }
            /// <summary>
            /// 偏移移动
            /// </summary>
            /// <param name="offset"></param>
            /// <param name="land"></param>
            /// <returns>没有MoveArrived</returns>
            public AgentMoveResult TryMoveOffset(Vector2 offset, bool land)
            {
                var target = new Vector3(
                    currentPos.X + offset.X,
                    currentPos.Y + offset.Y,
                    currentPos.Z);
                return TryMoveToNext3D(target, land);
            }

            //-----------------------------------------------------------------------------------------------------

            protected AgentMoveResult TryMoveToNext3D(Vector3 target, bool land)
            {
                this.Transport(target);
                return AgentMoveResult.MoveSmooth;
            }

            //-----------------------------------------------------------------------------------------------------
            public void MoveOffsetNoTouch(Vector2 offset)
            {
                Vector3 target = new Vector3(currentPos.X + offset.X, currentPos.Y + offset.Y, currentPos.Z);
                this.Transport(target);
            }
            /// <summary>
            /// 贴地闪现移动
            /// </summary>
            /// <param name="dst"></param>
            /// <param name="touched"></param>
            /// <returns></returns>
            public AgentMoveResult MoveLinearTo2D(Vector3 dst, out DummyLayer touched)
            {
                this.Transport(dst);
                touched = CurrentLayer;
                return AgentMoveResult.MoveArrived;
            }
            //-----------------------------------------------------------------------------------------------------
            #region ITerrainAgent
            ITerrainWorld ITerrainAgent.World => this.World;
            ITerrain ITerrainAgent.Terrain => this.World.terrain;
            ITerrainLayer ITerrainAgent.CurrentLayer => this.CurrentLayer;
            ITerrainAgent ITerrainAgent.Clone()
            {
                return this.Clone();
            }
            void ITerrainAgent.EnterWorld(ITerrainWorld world)
            {
                this.EnterWorld(world as DummyTerrainWorld);
            }
            void ITerrainAgent.Transport(ITerrainLayer layer)
            {
                this.Transport((DummyLayer)layer);
            }
            void ITerrainAgent.Transport(in Vector3 pos, ITerrainLayer layer)
            {
                this.Transport(pos, (DummyLayer)layer);
            }
            AgentMoveResult ITerrainAgent.TryMoveToPath(ref ITerrainWayPoint path, float step, bool land)
            {
                var _path = path as DummyWayPoint;
                var ret = this.TryMoveToPath(ref _path, step, land);
                path = _path;
                return ret;
            }
            AgentMoveResult ITerrainAgent.TryMoveTo(Vector3 target, float step, bool land)
            {
                return this.TryMoveTo(target, step, land);
            }
            AgentMoveResult ITerrainAgent.TryMoveLerp(float direction, float step, bool land)
            {
                return this.TryMoveLerp(direction, step, land);
            }
            AgentMoveResult ITerrainAgent.TryMoveOffset(Vector2 offset, bool land)
            {
                return this.TryMoveOffset(offset, land);
            }
            AgentMoveResult ITerrainAgent.MoveLinearTo2D(Vector3 dst, out ITerrainLayer touched)
            {
                var ret = this.MoveLinearTo2D(dst, out var _touched);
                touched = _touched;
                return ret;
            }
            //-----------------------------------------------------------------------------------------------------
            event ITerrainAgent.LayerChanged ITerrainAgent.OnLayerChanged
            {
                add { event_OnLayerChanged += value; }
                remove { event_OnLayerChanged -= value; }
            }

            event ITerrainAgent.BumpHead ITerrainAgent.OnBumpHead
            {
                add { event_OnBumpHead += value; }
                remove { event_OnBumpHead -= value; }
            }

            event ITerrainAgent.FallenDown ITerrainAgent.OnFallenDown
            {
                add { event_OnFallenDown += value; }
                remove { event_OnFallenDown -= value; }
            }
            private ITerrainAgent.LayerChanged event_OnLayerChanged;
            private ITerrainAgent.BumpHead event_OnBumpHead;
            private ITerrainAgent.FallenDown event_OnFallenDown;
            #endregion
            //------------------------------------------------------------------------------------------------------------------------------------
        }

    }

    //     public class DummyTerrainFactory : TerrainFactory
    //     {
    //         public override ITerrainWorld GetOrCreateVoxelWorld(string path, object data)
    //         {
    //             throw new NotImplementedException();
    //         }
    // 
    //         public override Task<ITerrainWorld> GetOrCreateVoxelWorldAsync(string path, object data)
    //         {
    //             throw new NotImplementedException();
    //         }
    // 
    // 
    //     }
}
