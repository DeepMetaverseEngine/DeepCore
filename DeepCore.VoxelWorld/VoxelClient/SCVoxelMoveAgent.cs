using DeepCore.Geometry;
using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.VoxelWorld.VoxelClient
{
    public class SCVoxelMoveAgent : Disposable
    {
        private SCVoxelWorld world;
        private SCVoxelAOI aoi;
        private Vector3 currentPos;
        private SCVoxelLayer currentLayer;
        private float zspeed = 0;
        private float height = 1.8f;
        private float gravity = 9.8f;

        public SCVoxelWorld World { get => world; }
        public SCVoxelAOI AOI { get => aoi; }
        public Vector3 Position { get => currentPos; }
        public float X { get => currentPos.X; }
        public float Y { get => currentPos.Y; }
        public float Z { get => currentPos.Z; }
        public SCVoxelLayer CurrentLayer { get => currentLayer; }
        public SCVoxelCell CurrentCell { get => currentLayer?.OwnerCell; }
        public SCVoxelMapChunk CurrentChunk { get => currentLayer?.OwnerCell?.OwnerChunk; }
        public float Height { get => height; set { height = value; } }
        public float SpeedZ { get => zspeed; set { zspeed = value; } }
        public float Gravity { get => this.gravity; set { this.gravity = value; } }
        public float StepHeight { get; set; } = 1f;
        /// <summary>
        /// 离地距离
        /// </summary>
        public float? MidAir
        {
            get
            {
                if (currentLayer != null) { return Z - currentLayer.WorldUpward - world.WorldInfo.GridCellSize; }
                return null;
            }
        }
        /// <summary>
        /// 是否在空中
        /// </summary>
        public bool IsMidair
        {
            get
            {
                if (currentLayer != null) { return Z > currentLayer.WorldUpward - world.WorldInfo.GridCellSize; }
                return false;
            }
        }
        public SCVoxelMoveAgent()
        {
        }
        protected override void Disposing()
        {
            event_OnLayerChanged = null;
            event_OnBumpHead = null;
            event_OnFallenDown = null;
        }
        public void EnterWorld(SCVoxelWorld world, SCVoxelAOI aoi)
        {
            var oldLayer = currentLayer;
            this.aoi = aoi;
            this.world = world;
            Transport(currentPos);
        }
        public void LeaveWorld()
        {
            var oldLayer = currentLayer;
            this.aoi = null;
            this.world = null;
            this.currentLayer = null;
            if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
        }
        public void Update()
        {
            if (world != null)
            {
                var oldLayer = currentLayer;
                aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head);
                if (oldLayer != currentLayer) { this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer); }
                if (gravity > 0)
                {
                    var intervalMS = world.LastUpdateIntervalMS;
                    if (currentLayer == null)
                    {
                        currentPos.Z += CMath.GetSpeedDistance(intervalMS, zspeed);
                        zspeed -= CMath.GetSpeedDistance(intervalMS, gravity);
                        return;
                    }
                    if (zspeed != 0 || currentPos.Z > currentLayer.WorldUpward)
                    {
                        currentPos.Z += CMath.GetSpeedDistance(intervalMS, zspeed);
                        if (currentPos.Z <= currentLayer.WorldUpward)
                        {
                            currentPos.Z = currentLayer.WorldUpward;
                            if (zspeed != 0)
                            {
                                event_OnFallenDown?.Invoke(this, currentLayer, zspeed);
                            }
                            zspeed = 0;
                            return;
                        }
                        else if (head != null && currentPos.Z + height > head.WorldDownward)
                        {
                            currentPos.Z = Math.Max(currentLayer.WorldUpward, head.WorldDownward - height);
                            if (zspeed != 0)
                            {
                                event_OnBumpHead?.Invoke(this, currentLayer.UpLayer, zspeed);
                            }
                            zspeed = 0;
                            return;
                        }
                        else
                        {
                            zspeed -= CMath.GetSpeedDistance(intervalMS, gravity);
                        }
                    }
                    else if (currentPos.Z < currentLayer.WorldUpward)
                    {
                        currentPos.Z = currentLayer.WorldUpward;
                        zspeed = 0;
                    }
                    else 
                    {
                        zspeed = 0;
                    }
                }
            }
        }
        public void Transport(Vector3 pos)
        {
            if (world != null)
            {
                var oldLayer = currentLayer;
                this.currentPos = pos;
                aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head);
                if (oldLayer != currentLayer) this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
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
            if (world != null)
            {
                var oldLayer = currentLayer;
                this.currentPos.Z = dz;
                aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head);
                if (oldLayer != currentLayer) this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
            }
            else
            {
                this.currentPos.Z = dz;
            }
        }
        protected MoveResult TryMoveToNext2D(Vector2 target)
        {
            world.WorldPosToVoxel(Position, out var cloc);
            world.WorldPosToVoxel(target, out var tloc);
            tloc.Z = cloc.Z;
            //没跨格//
            if (cloc.X == tloc.X && cloc.Y == tloc.Y)
            {
                this.currentPos.X = target.X;
                this.currentPos.Y = target.Y;
                return MoveResult.MoveSmooth;
            }
            //向上跳过程中，不检测StepIntercept，否则上浮过程中会被强拉
            var step = zspeed > 0 ? 0f : StepHeight;
            var oldLayer = currentLayer;
            if (aoi.TryTouchMoveTo(in tloc, ref currentPos, height, step, out var next_cell, out var next))
            {
                currentPos.X = target.X;
                currentPos.Y = target.Y;
                if (next == null) { aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head); }
                this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                return MoveResult.MoveSmooth;
            }
            //尝试单边//
            if (aoi.TryTouchMoveTo(new Location3D(tloc.X, cloc.Y, cloc.Z), ref currentPos, height, step, out var next_cell_x, out next))
            {
                currentPos.X = target.X;
                if (next == null) { aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head); }
                this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                return MoveResult.MoveTouchY;
            }
            //尝试单边//
            if (aoi.TryTouchMoveTo(new Location3D(cloc.X, tloc.Y, cloc.Z), ref currentPos, height, step, out var next_cell_y, out next))
            {
                currentPos.Y = target.Y;
                if (next == null) { aoi.TryGetVoxelLayerByBody(ref currentPos, height, out currentLayer, out var head); }
                this.event_OnLayerChanged?.Invoke(this, oldLayer, currentLayer);
                return MoveResult.MoveTouchY;
            }
            //不可行走面//
            return MoveResult.Blocked;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        public enum MoveResult : byte
        {
            /// <summary>
            /// 完成移动
            /// </summary>
            MoveSmooth,
            /// <summary>
            /// 碰到墙被修正
            /// </summary>
            MoveTouchX,
            /// <summary>
            /// 碰到墙被修正
            /// </summary>
            MoveTouchY,
            /// <summary>
            /// 穿格移动，速度过快
            /// </summary>
            MoveCross,
            /// <summary>
            /// 移动到目的地
            /// </summary>
            MoveArrived,
            /// <summary>
            /// 被阻挡
            /// </summary>
            Blocked,
        }

        /// <summary>
        /// 目标移动
        /// </summary>
        /// <param name="target"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns></returns>
        public MoveResult TryMoveTo2D(Vector2 target, float step)
        {
            var distance = Vector2.Distance(target, currentPos);
            var direction = CMath.GetDegree(currentPos.X, currentPos.Y, target.X, target.Y);
            var dx = target.X - currentPos.X;
            var dy = target.Y - currentPos.Y;
            var ox = (float)(Math.Cos(direction) * step);
            var oy = (float)(Math.Sin(direction) * step);
            if (distance <= step)
            {
                var result = TryMoveOffset2D(new Vector2(dx, dy));
                if (result == MoveResult.MoveSmooth)
                {
                    result = MoveResult.MoveArrived;
                }
                return result;
            }
            return TryMoveOffset2D(new Vector2(ox, oy));
        }
        /// <summary>
        /// 指向移动
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns>没有MoveArrived</returns>
        public MoveResult TryMoveLerp2D(float direction, float step)
        {
            float ox = (float)(Math.Cos(direction) * step);
            float oy = (float)(Math.Sin(direction) * step);
            return TryMoveToNext2D(new Vector2(currentPos.X + ox, currentPos.Y + oy));
        }
        /// <summary>
        /// 偏移移动
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="land"></param>
        /// <returns>没有MoveArrived</returns>
        public MoveResult TryMoveOffset2D(Vector2 offset)
        {
            var target = new Vector2(
                currentPos.X + offset.X,
                currentPos.Y + offset.Y);
            return TryMoveToNext2D(target);
        }

        //-----------------------------------------------------------------------------------------------------


        //-----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 已切换体素
        /// </summary>
        public event LayerChanged OnLayerChanged { add { event_OnLayerChanged += value; } remove { event_OnLayerChanged -= value; } }
        /// <summary>
        /// 头撞到顶
        /// </summary>
        public event BumpHead OnBumpHead { add { event_OnBumpHead += value; } remove { event_OnBumpHead -= value; } }
        /// <summary>
        /// 摔落到地面
        /// </summary>
        public event FallenDown OnFallenDown { add { event_OnFallenDown += value; } remove { event_OnFallenDown -= value; } }
        private LayerChanged event_OnLayerChanged;
        private BumpHead event_OnBumpHead;
        private FallenDown event_OnFallenDown;
        public delegate void LayerChanged(SCVoxelMoveAgent obj, SCVoxelLayer src, SCVoxelLayer dst);
        public delegate void BumpHead(SCVoxelMoveAgent obj, SCVoxelLayer upLayer, float zspeed);
        public delegate void FallenDown(SCVoxelMoveAgent obj, SCVoxelLayer downLayer, float zspeed);
    }
}
