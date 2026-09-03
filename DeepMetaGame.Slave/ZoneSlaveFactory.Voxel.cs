using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Space;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Slave
{
    //-------------------------------------------------------------------------------------------------------------------------
    public class VoxelClientTerrain3D : ILayerZoneTerrain
    {
        private ITerrainWorld world;
        private LayerZone zone;
        public float StepIntercept { get => world.Terrain.StepIntercept; }
        public ITerrainWorld World { get => world; }
        public LayerZone Zone { get => zone; }

        public VoxelClientTerrain3D(LayerZone zone, ITerrainWorld world)
        {
            this.world = world;
            this.zone = zone;
            this.world.PathFinder.FindPathStepLimit = zone.CFG.AI_FIND_PATH_STEP_LIMIT;
        }

        public int XCount => world.Terrain.XCount;
        public int YCount => world.Terrain.YCount;
        public float TotalSizeX => world.Terrain.TotalSizeX;
        public float TotalSizeY => world.Terrain.TotalSizeY;
        public float TotalWidth => world.Terrain.TotalSizeX;
        public float TotalHeight => world.Terrain.TotalSizeY;
        public float GridCellSize => world.Terrain.GridCellSize;
        public float ResourceStartX => world.Terrain.ResourceStartX;
        public float ResourceStartY => world.Terrain.ResourceStartY;

        public void Dispose()
        {
            this.world = null;
        }
        public bool TryGetVoxelUpRange(Vector3 pos, out float downward, out float upward, out float top)
        {
            var layer = world.Terrain.GetVoxelLayerByObject(ref pos);
            if (layer != null)
            {
                downward = layer.Downward;
                upward = layer.Upward;
                top = layer.Top;
                return true;
            }
            else
            {
                downward = 0;
                upward = 0;
                top = 0;
                return false;
            }
        }


        public ILayerWayPoint FindPath(Vector3 src, Vector3 dst)
        {
            var wp = world.PathFinder.FindPathByPos(src, dst);
            return VoxelClientWayPoint.CreateFromVoxel(wp);
        }

        public bool IsInAir(ref Vector3 pos)
        {
            var ret = world.Terrain.TryTestInAirByPos(pos, out var layer);
            if (ret == false)
            {
                pos.Z = layer.Upward;
            }
            return ret;
        }
        public bool TouchMapByPos(LayerUnit u, Vector3 pos)
        {
            return world.Terrain.TryIntersectMapByPos(pos, out var layer);
        }
        public bool TryMoveTo(ref Vector3 pos)
        {
            return world.Terrain.TryMoveTo(ref pos, out var layer);
        }
        public bool TryMoveSpellOnFloor(ref Geometry.Vector3 pos, float direction, float distance)
        {
            if (world.Terrain.TryGetVoxelLayerByPos(in pos, out var layer, true))
            {
                return world.Terrain.TryMoveSpellOnFloor(ref pos, ref layer, direction, distance);
            }
            return false;
        }
        public bool TryGetVoxelTopRange(Vector3 pos, out float top)
        {
            var layer = world.Terrain.GetVoxelLayerByObject(ref pos);
            if (layer != null)
            {
                top = layer.Top;
                return true;
            }
            else
            {
                top = 0;
                return false;
            }
        }

        public bool TryGetVoxelDownRange(Vector3 pos, out float downward)
        {
            var layer = world.Terrain.GetVoxelLayerByObject(ref pos);
            if (layer != null)
            {
                downward = layer.Downward;
                return true;
            }
            else
            {
                downward = 0;
                return false;
            }
        }

        public bool TryGetVoxelUpRange(Vector3 pos, out float upward)
        {
            var layer = world.Terrain.GetVoxelLayerByObject(ref pos);
            if (layer != null)
            {
                upward = layer.Upward;
                return true;
            }
            else
            {
                upward = 0;
                return false;
            }
        }

        public TryMoveToMapBorderResult TryMoveToMapBorder(LayerZoneObject obj, ref Vector3 pos, Vector2 offset)
        {
            return TryMoveToMapBorderResult.ARRIVE;
        }

        public ILayerUnitPosition CreateUnitPosition(LayerUnit unit)
        {
            if (unit is LayerPlayer player)
            {
                return new VoxelPlayerPosition(player);
            }
            else
            {
                return new VoxelUnitPosition(unit);
            }
        }

        public bool FillMapBlockByShape(IShape shape, bool block)
        {
            return this.World.PathFinder.FillMapBlockByShape(shape, block);
        }

        //--------------------------------------------------------------------------------------------------------
        class VoxelUnitPosition : ILayerUnitPosition
        {
            protected readonly LayerUnit unit;
            protected readonly ITerrainAgent vobj;
            protected readonly VoxelClientTerrain3D world;
            public VoxelUnitPosition(LayerUnit unit)
            {
                this.unit = unit;
                this.world = (unit.Parent.Terrain3D as VoxelClientTerrain3D);
                this.vobj = world.world.CreateAgent();
                this.vobj.Gravity = unit.Parent.Gravity;
                this.vobj.Height = unit.BodyHeight;
                this.vobj.EnterWorld(world.world);
            }

            public float X => vobj.X;
            public float Y => vobj.Y;
            public float Z => vobj.Z;
            public float Upward => vobj.CurrentLayer.Upward;
            public Vector3 Position => vobj.Position;

            public float SpeedZ
            {
                get => vobj.SpeedZ;
                set => vobj.SpeedZ = value;
            }
            public bool IsInAir => vobj.IsInTheAir;
            public float Gravity
            {
                get => vobj.Gravity;
                set => vobj.Gravity = value;
            }

            public void SetPos(float x, float y, float z)
            {
                var tempV = new Vector3(x, y, z);

                vobj.Transport(in tempV);
            }

            public void Fly(float zOffset)
            {
                vobj.Fly(zOffset);
            }
            public void SetPos(in Vector3 pos)
            {
                vobj.Transport(pos);
            }
            public void SetPos(Vector3 pos)
            {
                vobj.Transport(pos);
            }
            public void StartJump(float zspeed, float gravity)
            {
                vobj.Gravity = gravity;
                vobj.Jump(zspeed);
            }
            public virtual bool Update(in Vector3 remote_pos, float intervalMS)
            {
                vobj.Update(intervalMS);
                return true;
            }
            public virtual bool FixPos(in Vector3 remote_pos, float intervalMS,float speedSEC)
            {
                //var speedSEC = unit.MoveSpeedSEC;
                if (speedSEC == 0)
                {
                    this.SetPos(remote_pos);
                    return true;
                }
                var step = MoveHelper.GetDistance(intervalMS, speedSEC);
                float fdistance = Geometry.Vector3.Distance(this.Position, remote_pos);
                if (fdistance <= step)
                {
                    return FixLerp(in remote_pos, 1);
                }
                else if (fdistance >= unit.Parent.AsyncUnitPosModifyMaxRange)
                {
                    SetPos(remote_pos.X, remote_pos.Y, remote_pos.Z);
                    return true;
                }
                else if (fdistance > 0)
                {
                    var pos = this.Position;
                    VectorHelper.MoveTo3D(ref pos, in remote_pos, step);
                    FixLerp(in pos, 1);
                    SetPos(pos);
                    //FixLerp(in remote_pos, fdistance / unit.Parent.AsyncUnitPosModifyMinRange);
                    return remote_pos.X == this.X && remote_pos.Y == this.Y;
                }
                return true;
            }

            private bool FixLerp(in Vector3 remotePos, float amount)
            {
                var src = vobj.Position;

                //fix:导致Z轴不同步

                /*       var ret = Vector2.Lerp(src, remotePos, amount);
                       vobj.Transport(new Vector3(ret, src.Z));*/


                var ret = Vector3.Lerp(src, remotePos, amount);
                vobj.Transport(ret);

                return ret.X == src.X && ret.Y == src.Y;
            }

            private bool FixLerpIncludeZ(in Vector3 remotePos, float amount)
            {
                var src = vobj.Position;

                var ret = Vector3.Lerp(src, remotePos, amount);
                vobj.Transport(ret);

                return ret.X == src.X && ret.Y == src.Y && ret.Z == src.Z;
            }

            public void ForceSetPos(in Vector3 pos)
            {
                vobj.Transport(pos);
            }
            public void Move(float addX, float addY)
            {
                var src = vobj.Position;
                src.X += addX;
                src.Y += addY;
                vobj.Transport(src);
            }
            public TryMoveToMapBorderResult TryMoveToMapBorder(float addX, float addY)
            {
                switch (vobj.TryMoveOffset(new Vector2(addX, addY), true))
                {
                    case AgentMoveResult.MoveSmooth:
                    case AgentMoveResult.MoveCross:
                    case AgentMoveResult.MoveArrived:
                        return TryMoveToMapBorderResult.ARRIVE;
                    case AgentMoveResult.Blocked:
                        return TryMoveToMapBorderResult.BLOCK;
                    case AgentMoveResult.MoveTouchX:
                    case AgentMoveResult.MoveTouchY:
                        return TryMoveToMapBorderResult.TOUCH;
                }
                return TryMoveToMapBorderResult.ARRIVE;
            }
        }
        class VoxelPlayerPosition : VoxelUnitPosition, ILayerPlayerPosition
        {
            protected readonly LayerPlayer player;
            public VoxelPlayerPosition(LayerPlayer player) : base(player)
            {
                this.player = player;
                this.vobj.MoveKeepInColor = false;
            }
        }
        //--------------------------------------------------------------------------------------------------------
        class VoxelClientWayPoint : ILayerWayPoint
        {
            private ITerrainWayPoint p;
            public VoxelClientWayPoint(ITerrainWayPoint p)
            {
                this.p = p;
            }
            public static VoxelClientWayPoint CreateFromVoxel(ITerrainWayPoint p)
            {
                if (p == null)
                {
                    return null;
                }
                var start = new VoxelClientWayPoint(p);
                var wp = start;
                while (p.Next != null)
                {
                    p = p.Next;
                    var next = new VoxelClientWayPoint(p);
                    wp.Next = next;
                    next.Prev = wp;
                    wp = next;
                }
                return start;
            }
            public ILayerWayPoint Next { get; private set; }
            public ILayerWayPoint Prev { get; private set; }
            public ILayerWayPoint Tail { get { var wp = this as ILayerWayPoint; while (wp.Next != null) { wp = wp.Next; } return wp; } }
            public float X { get => p.Position.X; }
            public float Y { get => p.Position.Y; }
            public float Z { get => p.Position.Z; }
            public Geometry.Vector3 Position { get => p.Position; }
            public bool PosEquals(ILayerWayPoint w)
            {
                return p.PosEquals((w as VoxelClientWayPoint).p);
            }
            public float GetTotalDistance()
            {
                return p.TotalDistance;
            }
            //--------------------------------------------------------------------------------------------------------
        }
    }

}
