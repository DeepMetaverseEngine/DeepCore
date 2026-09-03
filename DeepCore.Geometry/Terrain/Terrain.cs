using DeepCore.Astar;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry.Terrain
{
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainWorld : IDisposable
    {
        ITerrain Terrain { get; }
        ITerrainAstar PathFinder { get; }

        ITerrainAgent CreateAgent();
        ITerrainAgent CreateAgent(Vector3 pos);
        ITerrainAgent CreateAgent(ITerrainLayer pos);

        //         ITerrainBlock FindNearRandomMoveableNode(Random random, ITerrainAgent src, float radius);
        //         ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ITerrainAgent src, float width, float height);
        //         ITerrainBlock FindNearRandomMoveableNode(Random random, ITerrainBlock src, float radius);
        //         ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ITerrainBlock src, float width, float height);
        //         ITerrainBlock FindNearRandomMoveableNode(Random random, ref Vector3 src, float radius);
        //         ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ref Vector3 src, float width, float height);
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainSurface
    {
        int XCount { get; }
        int YCount { get; }
        float TotalSizeX { get; }
        float TotalSizeY { get; }
        bool TryGetVoxelLayerByPos(in Vector3 pos, out float upward, out float top);
        bool TryMoveSpellOnFloor(ref Geometry.Vector3 pos, float direction, float distance);
    }

    public interface ITerrain : IDisposable, ITerrainSurface
    {
        bool ITerrainSurface.TryGetVoxelLayerByPos(in Vector3 pos, out float upward, out float top)
        {
            var mCurrentLayer = GetVoxelLayerByPos(in pos);
            if (mCurrentLayer != null)
            {
                top = mCurrentLayer.Top;
                upward = mCurrentLayer.Upward;
                return true;
            }
            upward = 0;
            top = 10000;
            return false;
        }

        bool ITerrainSurface.TryMoveSpellOnFloor(ref Geometry.Vector3 pos, float direction, float distance)
        {
            if (TryGetVoxelLayerByPos(in pos, out var layer, true))
            {
                if (TryMoveSpellOnFloor(ref pos, ref layer, direction, distance))
                {
                    return true;
                }
            }
            return false;
        }

        BoundingBox AABB { get; }
        float GridCellSize { get; }
        float StepIntercept { get; }
        float ResourceStartX { get; }
        float ResourceStartY { get; }
        TerrainColor[] ColorPalette { get; }

        ITerrainLayer GetVoxelLayerByObject(ref Vector3 pos);
        ITerrainLayer GetVoxelLayerByPos(in Vector3 pos);

        bool TryUpdatePos(ref Vector3 pos, out ITerrainLayer layer);
        bool TryGetVoxelLayerByPos(in Vector3 pos, out ITerrainLayer layer, bool ground = false);
        bool TryGetVoxelLayerByObject(ref Vector3 vector, out ITerrainLayer layer);
        bool TryIntersectMapByPos(in Vector3 pos, out ITerrainLayer layer);
        bool TryTestInAirByPos(in Vector3 pos, out ITerrainLayer layer);

        bool TryMoveTo(ref Vector3 target, out ITerrainLayer touchLayer);
        /// <summary>
        /// 闇払い 百八式·暗勾手
        /// </summary>
        bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, float direction, float distance);
        /// <summary>
        /// 闇払い 百八式·暗勾手
        /// </summary>
        bool TryMoveSpellOnFloor(ref Vector3 pos, ref ITerrainLayer layer, in Vector2 target);

        bool RayCast(in Ray ray, out Vector3? hitPoint, out ITerrainLayer hitLayer);
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainLayer
    {
        Vector3 UpwardCenterPos { get; }
        float Top { get; }
        float Upward { get; }
        float Downward { get; }
        float Height { get; }
        bool IsPlane { get; }
        byte ColorIndex { get; }
        TerrainColor Color { get; }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainWayPoint : IWayPoint, IEnumerable<ITerrainWayPoint>
    {
        Vector3 Position { get; set; }
        new ITerrainWayPoint Next { get; }
        float TotalDistance { get; }
        bool PosEquals(ITerrainWayPoint o);
        void LinkNext(ITerrainWayPoint n);
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainMapNode
    {
        Vector3 Position { get; }
        //bool IsCross { get; }   
        float Height { get; }
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainAstar : IDisposable
    {
        int FindPathStepLimit { get; set; }

        ITerrainWayPoint FindPathByPos(Vector3 srcP, Vector3 dstP);
        ITerrainWayPoint FindPathByLayer(ITerrainLayer src, ITerrainLayer dst);
        ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, Vector3 dstP);
        ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, ITerrainLayer dst, Vector3 dstP);


        bool TestCross(IMapNode src, IMapNode dst);
        bool FillMapBlockByShape(IShape shape, bool block);
        bool GetMapBlockByPos(Vector3 srcP, out ITerrainMapNode mapnode);
        bool IsMapNodeBlock(ITerrainMapNode mapnode);
        IEnumerable<ITerrainMapNode> GetBlockMapNodes();
    }
    //----------------------------------------------------------------------------------------------------------------------------------------
    public enum AgentMoveResult : byte
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
    //----------------------------------------------------------------------------------------------------------------------------------------
    public interface ITerrainAgent
    {
        ITerrainWorld World { get; }
        ITerrain Terrain { get; }
        ITerrainLayer CurrentLayer { get; }
        Vector3 Position { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        /// <summary>
        /// 离地距离
        /// </summary>
        float? LandAirDistance { get; }
        /// <summary>
        /// 是否在空中
        /// </summary>
        bool IsInTheAir { get; }
        float Height { get; set; }
        float SpeedZ { get; set; }
        float Gravity { get; set; }
        bool MoveKeepInColor { get; set; }

        void Update(float intervalMS);
        ITerrainAgent Clone();
        void EnterWorld(ITerrainWorld world);
        void LeaveWorld();

        void Transport(ITerrainLayer layer);
        void Transport(in Vector3 pos);
        void Transport(in Vector3 pos, ITerrainLayer layer);

        void Jump(float speed);
        void Fly(float zoffset);
        void FlyTo(float dz);
        void MoveOffsetNoTouch(Vector2 offset);

        /// <summary>
        /// 目标移动
        /// </summary>
        /// <param name="path"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns></returns>
        AgentMoveResult TryMoveToPath(ref ITerrainWayPoint path, float step, bool land);
        /// <summary>
        /// 目标移动
        /// </summary>
        /// <param name="target"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns></returns>
        AgentMoveResult TryMoveTo(Vector3 target, float step, bool land);
        /// <summary>
        /// 指向移动
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="step"></param>
        /// <param name="land"></param>
        /// <returns>没有MoveArrived</returns>
        AgentMoveResult TryMoveLerp(float direction, float step, bool land);
        /// <summary>
        /// 偏移移动
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="land"></param>
        /// <returns>没有MoveArrived</returns>
        AgentMoveResult TryMoveOffset(Vector2 offset, bool land);

        /// <summary>
        /// 贴地闪现移动
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="touched"></param>
        /// <returns></returns>
        AgentMoveResult MoveLinearTo2D(Vector3 dst, out ITerrainLayer touched);




        /// <summary>
        /// 已切换体素
        /// </summary>
        event LayerChanged OnLayerChanged;
        /// <summary>
        /// 头撞到顶
        /// </summary>
        event BumpHead OnBumpHead;
        /// <summary>
        /// 摔落到地面
        /// </summary>
        event FallenDown OnFallenDown;

        delegate void LayerChanged(ITerrainAgent obj, ITerrainLayer src, ITerrainLayer dst);
        delegate void BumpHead(ITerrainAgent obj, float zspeed);
        delegate void FallenDown(ITerrainAgent obj, float zspeed);
    }
    //----------------------------------------------------------------------------------------------------------------------------------------


    public static class TerrainWorldExt
    {

        public static ITerrainLayer FindNearRandomMoveableNode(this ITerrainWorld world, Random random, ITerrainAgent src, float radius)
        {
            if (src != null)
            {
                float angle = (float)(random.NextDouble() * CMath.PI_MUL_2);
                float len = (float)(random.NextDouble() * radius);
                float x = src.X + (float)(Math.Cos(angle) * len);
                float y = src.Y + (float)(Math.Sin(angle) * len);
                var pos = new Vector3(x, y, src.Z);
                src.MoveLinearTo2D(pos, out var touched);
                return src.CurrentLayer;
            }
            return null;
        }
        public static ITerrainLayer FindNearRandomMoveableNodeRect(this ITerrainWorld world, Random random, ITerrainAgent src, float width, float height)
        {
            if (src != null)
            {
                float x = src.X + (float)((-width / 2f) + random.NextDouble() * width);
                float y = src.Y + (float)((-height / 2f) + random.NextDouble() * height);
                var pos = new Vector3(x, y, src.Z);
                src.MoveLinearTo2D(pos, out var touched);
                return src.CurrentLayer;
            }
            return null;
        }
        public static ITerrainLayer FindNearRandomMoveableNode(this ITerrainWorld world, Random random, ITerrainLayer src, float radius)
        {
            var obj = world.CreateAgent(src);
            obj.EnterWorld(world);
            return world.FindNearRandomMoveableNode(random, obj, radius);
        }
        public static ITerrainLayer FindNearRandomMoveableNodeRect(this ITerrainWorld world, Random random, ITerrainLayer src, float width, float height)
        {
            var obj = world.CreateAgent(src);
            obj.EnterWorld(world);
            return world.FindNearRandomMoveableNodeRect(random, obj, width, height);
        }
        public static ITerrainLayer FindNearRandomMoveableNode(this ITerrainWorld world, Random random, ref Vector3 src, float radius)
        {
            var obj = world.CreateAgent(src);
            obj.EnterWorld(world);
            var ret = world.FindNearRandomMoveableNode(random, obj, radius);
            src = obj.Position;
            return ret;
        }
        public static ITerrainLayer FindNearRandomMoveableNodeRect(this ITerrainWorld world, Random random, ref Vector3 src, float width, float height)
        {
            var obj = world.CreateAgent(src);
            obj.EnterWorld(world);
            var ret = world.FindNearRandomMoveableNodeRect(random, obj, width, height);
            src = obj.Position;
            return ret;
        }

    }

    //----------------------------------------------------------------------------------------------------------------------------------------
}

