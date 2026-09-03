using DeepCore;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using System;

namespace DeepMetaGame.Data.ZoneGeometry.Terrain
{
    public class TerrainAgent : ITerrainAgent
    {
        private SceneTerrainWorld world;
        private TerrainLayer currentLayer;
        private Vector3 currentPos;
        private float zspeed = 0;
        private float height = 1.8f;
        private float gravity = 10f;

        public SceneTerrainWorld World { get => world; }
        public TerrainMap Terrain { get => world.map; }
        public TerrainLayer CurrentLayer { get => currentLayer; }
        public Vector3 Position { get => currentPos; }
        public float X { get => currentPos.X; }
        public float Y { get => currentPos.Y; }
        public float Z { get => currentPos.Z; }
        /// <summary>
        /// 离地距离
        /// </summary>
        public float? LandAirDistance
        {
            get
            {
                if (currentLayer != null) { return Z - currentLayer.Upward; }
                return null;
            }
        }
        /// <summary>
        /// 是否在空中
        /// </summary>
        public bool IsInTheAir
        {
            get
            {
                if (currentLayer != null) { return (Z - currentLayer.Upward) >= 0.1f; }
                return false;
            }
        }
        public float Height { get => height; set { height = value; } }
        public float SpeedZ { get => zspeed; set { zspeed = value; } }
        public float Gravity { get => this.gravity; set { this.gravity = value; } }
        public bool MoveKeepInColor { get; set; } = false;
        public bool IgnoreTop { get; set; } = true;

        public TerrainAgent()
        {
        }
        public TerrainAgent(Vector3 pos)
        {
            this.currentPos = pos;
        }
        public TerrainAgent(TerrainLayer pos)
        {
            this.currentLayer = pos;
            this.currentPos = pos.UpwardCenterPos;
        }
        public virtual void Update(float intervalMS)
        {
            ProcessGravity(intervalMS);
        }
        public TerrainAgent Clone()
        {
            return new TerrainAgent()
            {
                world = this.world,
                currentLayer = this.currentLayer,
                currentPos = this.currentPos,
                zspeed = this.zspeed,
                height = this.height,
                gravity = this.gravity,
            };
        }
        public void EnterWorld(SceneTerrainWorld world)
        {
            var oldLayer = currentLayer;
            this.world = world;
            this.currentLayer = Terrain.GetVoxelLayerByObject(ref currentPos) as TerrainLayer;
            if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
        }
        public void LeaveWorld()
        {
            var oldLayer = currentLayer;
            this.world = null;
            this.currentLayer = null;
            if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
        }
        public void Transport(TerrainLayer layer)
        {
            var oldLayer = currentLayer;
            this.currentPos = layer.UpwardCenterPos;
            this.currentLayer = layer;
            if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
        }
        public void Transport(in Vector3 pos)
        {
            var oldLayer = currentLayer;
            if (world != null)
            {
                Terrain.WorldPosToVoxel(currentPos.X, currentPos.Y, out var svx, out var svy);
                Terrain.WorldPosToVoxel(pos.X, pos.Y, out var dvx, out var dvy);
                this.currentPos = pos;
                if (svx != dvx || svy != dvy || currentLayer == null)
                {
                    this.currentLayer = Terrain.GetVoxelLayerByObject(ref currentPos) as TerrainLayer;
                    if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
                }
            }
            else
            {
                this.currentPos = pos;
            }
        }
        public void Transport(Vector3 pos, TerrainLayer layer)
        {
            var oldLayer = currentLayer;
            this.currentPos = pos;
            this.currentLayer = layer;
            if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
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

        /// <summary>
        /// 目标移动
        /// </summary>
        /// <param name="path"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns></returns>
        public AgentMoveResult TryMoveToPath(ref TerrainWayPoint path, float step, bool land)
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
            var target = new Vector2(
                currentPos.X + offset.X,
                currentPos.Y + offset.Y);
            return TryMoveToNext3D(target, land);
        }

        //-----------------------------------------------------------------------------------------------------

        protected AgentMoveResult TryMoveToNext3D(Vector2 target, bool land)
        {
            Terrain.WorldPosToVoxel(target.X, target.Y, out var tx, out var ty);
            if (currentLayer == null)
            {
                this.currentPos = target;
                return AgentMoveResult.MoveSmooth;
            }
            //没跨格//
            if (currentLayer.X == tx && currentLayer.Y == ty)
            {
                this.currentPos.X = target.X;
                this.currentPos.Y = target.Y;
                return AgentMoveResult.MoveSmooth;
            }
            //向上跳过程中，不检测StepIntercept，否则上浮过程中会被强拉
            var step = zspeed > 0 ? 0f : 1;
            var inAir = currentPos.Z > currentLayer.Upward;
            if (inAir) { land = false; }
            var oldLayer = currentLayer;
            var dx = tx - currentLayer.X;
            var dy = ty - currentLayer.Y;
            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);
            if (adx <= 1 && ady <= 1)
            {
                {
                    var next = currentLayer.GetNextNode(dx, dy);
                    //跨格已连接//
                    if (next != null && next.TryStandOn(ref currentPos, IgnoreTop ? 0 : height, step))
                    {
                        this.currentLayer = next;
                        this.currentPos.X = target.X;
                        this.currentPos.Y = target.Y;
                        if (land)
                        {
                            this.currentPos.Z = currentLayer.Upward;
                        }
                        this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                        return AgentMoveResult.MoveSmooth;
                    }
                }
                if (adx == ady)
                {
                    //尝试单边//
                    var nextX = currentLayer.GetNextNode(dx, 0);
                    if (nextX != null && nextX.TryStandOn(ref currentPos, IgnoreTop ? 0 : height, step))
                    {
                        this.currentLayer = nextX;
                        this.currentPos.X = target.X;
                        if (land)
                        {
                            this.currentPos.Z = currentLayer.Upward;
                        }
                        this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                        return AgentMoveResult.MoveTouchY;
                    }
                    //尝试单边//
                    var nextY = currentLayer.GetNextNode(0, dy);
                    if (nextY != null && nextY.TryStandOn(ref currentPos, IgnoreTop ? 0 : height, step))
                    {
                        this.currentLayer = nextY;
                        this.currentPos.Y = target.Y;
                        if (land)
                        {
                            this.currentPos.Z = currentLayer.Upward;
                        }
                        this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                        return AgentMoveResult.MoveTouchX;
                    }
                }
                if (currentLayer.Y == ty)
                {
                    this.currentPos.Y = target.Y;
                }
                if (currentLayer.X == tx)
                {
                    this.currentPos.X = target.X;
                }
                //跨格无连接//
                if (Terrain.TryGetVoxelCell(tx, ty, out var next_cell))
                {
                    //下格无碰撞//
                    if (next_cell.TryStandOn(ref currentPos, IgnoreTop ? 0 : height, step))
                    {
                        //同色地块或者在空中//
                        if ((!MoveKeepInColor || currentLayer.Color == next_cell.Color) || land == false)
                        {
                            currentLayer = next_cell;
                            currentPos.X = target.X;
                            currentPos.Y = target.Y;
                            this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                            return AgentMoveResult.MoveCross;
                        }
                    }
                }
            }
            else
            {
                //跨格无连接//
                if (Terrain.TryGetVoxelCell(tx, ty, out var next_cell))
                {
                    //下格无碰撞//
                    if (next_cell.TryStandOn(ref currentPos, IgnoreTop ? 0 : height, step))
                    {
                        currentLayer = next_cell;
                        currentPos.X = target.X;
                        currentPos.Y = target.Y;
                        this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                        return AgentMoveResult.MoveCross;
                    }
                }
            }
            //不可行走面//
            return AgentMoveResult.Blocked;
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
        public AgentMoveResult MoveLinearTo2D(Vector3 dst, out TerrainLayer touched)
        {
            var oldLayer = currentLayer;
            //             var step = world.Terrain.BuildConfig.StepIntercept;
            //             var touchCell = default(VoxelCell);
            //             var tuple = new ValueTuple<VoxelLayer, VoxelCell, Vector2>();
            //             world.Terrain.ForEachCellsRayStep(currentPos.X, currentPos.Y, dst.X, dst.Y, ref tuple, static (ref ValueTuple<VoxelLayer, VoxelCell, Vector2> tuple, VoxelCell cell, int cx, int cy, float ox, float oy) =>
            //             {
            //                 if (cell == null)
            //                 {
            //                     tuple.Item2 = tuple.Item1.OwnerCell;
            //                     return true;
            //                 }
            //                 if (cell.TryTouchMoveTo(ref currentPos, height, step, out var cross))
            //                 {
            //                     this.currentLayer = cross;
            //                     this.currentPos.X = ox;
            //                     this.currentPos.Y = oy;
            //                     if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
            //                     return false;
            //                 }
            //                 touchCell = cell;
            //                 return true;
            //             });
            Terrain.TryGetVoxelLayerByPos(dst, out var dstLayer);
            Terrain.TryBlinkToTarget2D(currentLayer, currentPos, dstLayer as TerrainLayer, dst, out currentLayer, out currentPos);
            if (dstLayer == null || currentLayer == oldLayer)
            {
                touched = currentLayer;// touchCell.GetLayerByAltitude(currentPos.Z);
                return AgentMoveResult.Blocked;
            }
            else if (dstLayer == currentLayer)
            {
                touched = null;
                return AgentMoveResult.MoveArrived;
            }
            else
            {
                touched = null;
                return AgentMoveResult.MoveCross;
            }
        }
        //-----------------------------------------------------------------------------------------------------
        protected virtual void ProcessGravity(float intervalMS)
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
        //-------------------------------------------------------------------------------------------------------------------------------------------
        #region ITerrainAgent

        ITerrainWorld ITerrainAgent.World => this.World;
        ITerrain ITerrainAgent.Terrain => this.Terrain;
        ITerrainLayer ITerrainAgent.CurrentLayer => this.CurrentLayer;

        ITerrainAgent ITerrainAgent.Clone()
        {
            return this.Clone();
        }
        void ITerrainAgent.EnterWorld(ITerrainWorld world)
        {
            this.EnterWorld(world as SceneTerrainWorld);
        }
        void ITerrainAgent.Transport(ITerrainLayer layer)
        {
            this.Transport(layer as TerrainLayer);
        }
        void ITerrainAgent.Transport(in Vector3 pos, ITerrainLayer layer)
        {
            this.Transport(pos, layer as TerrainLayer);
        }
        AgentMoveResult ITerrainAgent.TryMoveToPath(ref ITerrainWayPoint path, float step, bool land)
        {
            var _path = path as TerrainWayPoint;
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
