using DeepCore.Geometry;
using DeepCore.Space;
using DeepCore.Voxel.Data;
using DeepCore.VoxelWorld.Message;
using DeepCore.XCSV;
using System;
using System.Threading.Tasks;
using static DeepCore.Voxel.Extensions.MagicaVoxel.MagicaVoxelFile;

namespace DeepCore.VoxelWorld.VoxelClient
{

    public class SCVoxelAOI : Disposable
    {
        public SCVoxelWorld World { get; }
        public SingleThreadCollectionPool ObjectPool { get => World.ObjectPool; }
        public SCVoxelAOI(SCVoxelWorld world)
        {
            this.World = world;
            this.SetLookRange(4);

        }
        protected override void Disposing()
        {
            event_OnChunkEnterView = null;
            event_OnChunkLeaveView = null;
        }
        public void Update()
        {
            UpdateAOI();
        }
        //---------------------------------------------------------------------------------------------------
        private Vector3 currentPos;
        public Vector3 Position
        {
            get => currentPos;
        }
        public void Transport(in Geometry.Vector3 position)
        {
            this.currentPos = position;
        }
        public void Move(in Geometry.Vector3 offset)
        {
            this.currentPos += offset;
        }

        public BoundingBox CurrentLookMinRangeBox
        {
            get
            {
                var r = new Vector3(
                    inViewLookRange * World.WorldInfo.ChunkSize.X,
                    inViewLookRange * World.WorldInfo.ChunkSize.Y,
                    inViewLookRange * World.WorldInfo.ChunkSize.Z) / 2f;
                return new BoundingBox(currentPos - r, currentPos + r);
            }
        }
        public BoundingBox[] CurrentLookRanges
        {
            get
            {
                return this.inView3D.Convert1D((i, v) => v.ViewBounds);
            }
        }
        public ChunkMetaMap[] GetLODs()
        {
            return this.inView3D.Convert1D((i, v) => v.Meta as ChunkMetaMap);
        }
        //---------------------------------------------------------------------------------------------------
        #region SCVoxelViewAOI
        private IScrollView<SCVoxelMapChunk>[] inView3D;
        public class ChunkMetaMap : IScrollMap<SCVoxelMapChunk>
        {
            private readonly SCVoxelAOI aoi;
            public bool IsInfinity => true;
            public bool IsSycMap => false;
            public int LOD { get; }
            public int POW { get; }
            public Vector3 GridSize { get; }
            public Size3D Length { get; }
            public ChunkMetaMap(SCVoxelAOI aoi, int lod)
            {
                this.aoi = aoi;
                this.POW = (int)Math.Pow(2, lod);
                this.LOD = lod;
                this.GridSize = new Size3D(
                    aoi.World.WorldInfo.ChunkSize.X * (POW),
                    aoi.World.WorldInfo.ChunkSize.Y * (POW),
                    aoi.World.WorldInfo.ChunkSize.Z * (POW));
                this.Length = new Size3D(
                    aoi.World.WorldInfo.TotalSize.X,
                    aoi.World.WorldInfo.TotalSize.Y,
                    lod == 0 ? aoi.World.WorldInfo.TotalSize.Z : 1);
            }
            public SCVoxelMapChunk GetMetaData(int x, int y, int z)
            {
                return new SCVoxelMapChunk(aoi.World, this, new Location3D(
                    (x * (int)GridSize.X),
                    (y * (int)GridSize.Y),
                    (z * (int)GridSize.Z)));
            }
        }
        private int inViewLookRange = 0;
        private int inViewLookLOD = 1;
        private int[] inViewLookLODRanges;
        private bool inViewDirty = true;
        /// <summary>
        /// 每个LOD递进值
        /// 比如 2,2,2,2 有4层LOD，每一层递进视野范围2，则实际LOD距离为 2,4,6,8
        /// </summary>
        /// <param name="ranges"></param>
        public void SetLookRange(params int[] ranges)
        {
            if (ranges.Length == 0) throw new Exception("Look Range Cant Not Be Zero");
            this.inViewLookRange = ranges[0] = Math.Max(ranges[0], 2);
            for (int i = 1; i < ranges.Length; i++)
            {
                ranges[i] = ranges[i - 1] + ranges[i];
            }
            this.inViewLookLOD = ranges.Length;
            this.inViewLookLODRanges = ranges;
            this.inViewDirty = true;
        }
        void UpdateAOI()
        {
            if (inViewDirty)
            {
                this.inViewDirty = false;
                if (inView3D != null) { foreach (var aoi in inView3D) { aoi.Dispose(); } }
                inView3D = new LookRangeScroll<SCVoxelMapChunk>[inViewLookLOD];
                for (int lod = 0; lod < inViewLookLOD; lod++)
                {
                    inView3D[lod] = new LookRangeScroll<SCVoxelMapChunk>(
                        new ChunkMetaMap(this, lod),
                        new Vector3(
                        inViewLookLODRanges[lod] * World.WorldInfo.ChunkSize.X,
                        inViewLookLODRanges[lod] * World.WorldInfo.ChunkSize.Y,
                        lod == 0 ? (inViewLookLODRanges[lod] * World.WorldInfo.ChunkSize.Z) : 0),
                        2);
                    inView3D[lod].OnEnterView += InView3D_OnEnterView;
                    inView3D[lod].OnLeaveView += InView3D_OnLeaveView;
                }
            }
            for (int lod = 0; lod < inViewLookLOD; lod++)
            {
                var cpos = currentPos;
                var aoi = inView3D[lod];
                if (lod > 0)
                {
                    cpos.Z = 0;
                    aoi.SetViewPos(cpos);
                    var inner = inView3D[lod - 1];
                    var inbox = inner.ViewBounds;
                    inbox.Min.Z = -1;
                    inbox.Max.Z = 1;
                    aoi.ForEachBuffer((cell, x, y, z) =>
                    {
                        var outbox = cell.WorldBoundingBox;
                        outbox.Min.Z = 0;
                        outbox.Max.Z = 0;
                        cell.IsVisible = !(inbox.Contains(outbox) == ContainmentType.Contains);
                    });
                }
                else
                {
                    aoi.SetViewPos(cpos);
                }
            }
        }
        public bool ForEachInViewChunks(BreakPredicate<SCVoxelMapChunk> action)
        {
            return inView3D[0].Visit((inview, pos) =>
            {
                return action(inview);
            });
        }
        private async void InView3D_OnEnterView(IScrollView<SCVoxelMapChunk> sender, SCVoxelMapChunk newChunk, Location3D loc)
        {
            //Console.WriteLine($"InView3D_OnEnterView {x} {y} {z}");
            var meta = sender.Meta as ChunkMetaMap;
            var lod = meta.LOD;
            var chunkLocation = new Location3D(
                    loc.X * World.WorldInfo.ChunkSize.X * (lod + 1),
                    loc.Y * World.WorldInfo.ChunkSize.Y * (lod + 1),
                    loc.Z * World.WorldInfo.ChunkSize.Z * (lod + 1));
            var lcc = loc;
            var r = await World.Adapter.FetchMapChunkAsync(chunkLocation, lod);
            newChunk.Init(r);
            if (r.Chunk != null)
            {
                newChunk.InitTouch(r.Chunk);
                event_OnChunkEnterView?.Invoke(newChunk);
            }
            else
            {
                var chunk = await World.Adapter.FetchChunkByUUIDAsync(r.ChunkUUID);
                Task.Run(() =>
                {
                    newChunk.InitTouch(chunk.chunk);
                    World.Invoke(() =>
                    {
                        if (sender.IsInView(in lcc))
                        {
                            event_OnChunkEnterView?.Invoke(newChunk);
                        }
                    });
                }).NoWait();
            }
        }
        private void InView3D_OnLeaveView(IScrollView<SCVoxelMapChunk> sender, SCVoxelMapChunk oldChunk, Location3D loc)
        {
            if (oldChunk != null)
            {
                //Console.WriteLine($"InView3D_OnLeaveView {x} {y} {z}");
                event_OnChunkLeaveView?.Invoke(oldChunk);
            }
        }


        public delegate void ChunkEnterView(SCVoxelMapChunk chunk);
        public delegate void ChunkLeaveView(SCVoxelMapChunk chunk);
        public delegate void ChunkViewChanged();
        public event ChunkEnterView OnChunkEnterView { add { event_OnChunkEnterView += value; } remove { event_OnChunkEnterView -= value; } }
        public event ChunkLeaveView OnChunkLeaveView { add { event_OnChunkLeaveView += value; } remove { event_OnChunkLeaveView -= value; } }
        private ChunkEnterView event_OnChunkEnterView;
        private ChunkLeaveView event_OnChunkLeaveView;


        #endregion
        //---------------------------------------------------------------------------------------------------


        //---------------------------------------------------------------------------------------------------------
        #region SCVoxelTouchAOI


        public bool TryGetVoxelLayerByBody(ref Vector3 pos, float height, out SCVoxelLayer foot, out SCVoxelLayer head)
        {
            float z = pos.Z;
            foot = null;
            head = null;
            World.WorldPosToVoxel(in pos, out var loc);
            {
                if (TryGetVoxelCell(loc, out var cell))
                {
                    cell.TryRayCastUpward(z, height, out foot);
                    cell.TryRayCastDownward(z, height, out head);
                    if (foot == head)
                    {
                        head = foot?.UpLayer;
                    }
                }
            }
            if (foot == null)
            {
                var foot_loc = loc;
                foot_loc.Z -= World.ChunkSize.Z;
                if (TryGetVoxelCell(foot_loc, out var foot_cell))
                {
                    foot_cell.TryRayCastUpward(z, height, out foot);
                }
            }
            if (head == null)
            {
                var head_loc = loc;
                head_loc.Z += World.ChunkSize.Z;
                if (TryGetVoxelCell(head_loc, out var head_cell))
                {
                    head_cell.TryRayCastDownward(z, height, out head);
                }
            }
            if (foot == head)
            {
                head = foot?.UpLayer;
            }
            if (head != null)
            {
                if (z + height > head.WorldDownward) z = head.WorldDownward - height;
            }
            if (foot != null)
            {
                if (z < foot.WorldUpward) z = foot.WorldUpward;
            }
            if (head != null && foot != null)
            {
                if (head.WorldDownward < foot.WorldUpward)
                {
                    throw new Exception();
                }
                else if (head.WorldDownward - foot.WorldUpward < height)
                {
                    z = foot.WorldUpward;
                }
            }
            pos.Z = z;
            return foot != null;
        }
        public bool TryTouchMoveTo(in Location3D tloc, ref Vector3 pos, float height, float step, out SCVoxelCell next_cell, out SCVoxelLayer foot)
        {
            var z = pos.Z;
            if (TryGetVoxelCell(tloc, out next_cell))
            {
                if (next_cell.TryTouchMoveTo(ref z, step, out foot))
                {
                    if (next_cell.TryRayCastDownward(z, height, out var head))
                    {
                        if (z + height >= head.WorldDownward)
                        {
                            return false;
                        }
                    }
                    pos.Z = z;
                    return true;
                }
                return false;
            }
            else
            {
                foot = null;
                return true;
            }
        }
        public bool TryGetVoxelChunk(Location3D loc, out SCVoxelMapChunk chunk)
        {
            Vector3 vloc = loc;
            return inView3D[0].TryGetMapBuffByPos(vloc, out chunk);
        }
        public bool TryGetVoxelCell(Location3D loc, out SCVoxelCell cell)
        {
            Vector3 vloc = loc;
            if (inView3D[0].TryGetMapBuffByPos(vloc, out var chunk))
            {
                if (chunk != null && chunk.Chunk != null)
                {
                    if ((chunk.TryGetVoxelCell(loc, out var _cell)))
                    {
                        cell = _cell;
                        return true;
                    }
                }
            }
            cell = null;
            return false;
        }


        public delegate bool RayTestVoxelLayer(in RayCast ray, SCVoxelLayer layer, out Vector3? touch);

        public bool RayTestLayerUpward(in RayCast _ray, SCVoxelLayer _layer, out Vector3? _ray_touch)
        {
            var chunk = _layer.OwnerChunk;
            var pp = _layer.WorldUpwardPos;
            var ray_touch = RayCast.RayPlaneIntersection(in _ray, new RayCast.Plane(pp, Vector3.UnitZ));
            _ray_touch = ray_touch;
            return CMath.IncludeRectPointW(pp.X, pp.Y, chunk.GridCellSize, chunk.GridCellSize, ray_touch.X, ray_touch.Y);
        }
        public bool RayTestLayerBounds(in RayCast _ray, SCVoxelLayer _layer, out Vector3? _ray_touch)
        {
            var box = _layer.WorldBoundingBox;
            _ray_touch = RayCast.RayBoundingBoxIntersection(in _ray, in box);
            return _ray_touch != null;
        }
        public SCVoxelLayer RayCastVoxelLayer(RayCast ray, RayTestVoxelLayer test, out Vector3? touch)
        {
            SCVoxelLayer toucn_layer = null;
            Vector3? touch_point = null;
            var target = ray.center + (ray.normal * ray.distance);
            //Console.WriteLine($"-------------------------------------------------------");
            inView3D[0].TryRayCastMap(this, World.ObjectPool, ray, ( chunk, loc, pos, st) =>
            {
                //Console.WriteLine($"TestChunk {chunk}");
                if (chunk.HasCube)
                {
                    var local_pos = ray.center - chunk.WorldPosition;
                    var local_target = target - chunk.WorldPosition;
                    var result_cell = chunk.ForEachCellsRayStepPloar(local_pos, local_target, (cell, cx, cy) =>
                    {
                        if (cell != null)
                        {
                            for (int i = cell.LayerCount - 1; i >= 0; --i)
                            {
                                var layer = cell.GetLayer(i);
                                if (test(in ray, layer, out var ray_touch))
                                {
                                    //Console.WriteLine($"Touch Layer {layer}");
                                    toucn_layer = layer;
                                    touch_point = ray_touch;
                                    return true;
                                }
                            }
                        }                       
                        return false;
                    }, false);
                    if (result_cell != null)
                    {
                        return true;
                    }
                }
                return false;
            }, out var rc);

            touch = touch_point;
            return toucn_layer;
        }
        public SCVoxelLayer RayCastVoxelLayerInBox(RayCast ray, in BoundingBox box, RayTestVoxelLayer test, out Vector3? touch)
        {
            SCVoxelLayer toucn_layer = null;
            Vector3? touch_point = null;
            var target = ray.center + (ray.normal * ray.distance);
            //Console.WriteLine($"-------------------------------------------------------");
            World.ChunkSize.FoeEachChunkLocation(in box, (location) =>
            {
                if (TryGetVoxelChunk(location, out var chunk) && chunk.HasCube)
                {
                    //Console.WriteLine($"TestChunk {chunk}");
                    var local_pos = ray.center - chunk.WorldPosition;
                    var local_target = target - chunk.WorldPosition;
                    var result_cell = chunk.ForEachCellsRayStepPloar(local_pos, local_target, (cell, cx, cy) =>
                    {
                        if (cell != null)
                        {
                            for (int i = cell.LayerCount - 1; i >= 0; --i)
                            {
                                var layer = cell.GetLayer(i);
                                if (test(in ray, layer, out var ray_touch))
                                {
                                    toucn_layer = layer;
                                    touch_point = ray_touch;
                                    return true;
                                }
                            }
                        }
                        return false;
                    }, false);
                    if (result_cell != null)
                    {
                        return true;
                    }
                }
                return false;
            });
            touch = touch_point;
            return toucn_layer;
        }
        public SCVoxelLayer RayCastVoxelLayerUpward(RayCast ray, out Vector3? touch)
        {
            return RayCastVoxelLayer(ray, RayTestLayerUpward, out touch);
        }
        public SCVoxelLayer RayCastVoxelLayerBounds(RayCast ray, out Vector3? touch)
        {
            return RayCastVoxelLayer(ray, RayTestLayerBounds, out touch);
        }

        #endregion // SCVoxelTouchAOI
        //---------------------------------------------------------------------------------------------------------
    }
}
