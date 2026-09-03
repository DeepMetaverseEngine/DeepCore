using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Voxel.Data
{
    public interface IVoxelAstarMap
    {
        void CombineMesh(DeepCore.Geometry.Triangles trangles, float weight);
        void Save(OutputStream outputP);
        IVoxelAstar CreatePathFinder();
    }

    public interface IVoxelAstar : ITerrainAstar
    {
        void ForEachNodes<ST>(ST st, Action<IVoxelMapNode, ST> action);
        IVoxelMapNode GetMapNode(VoxelLayer src);
        IVoxelWayPoint GenWayPoint(IVoxelMapNode node);
        new IVoxelWayPoint FindPathByPos(Vector3 srcP, Vector3 dstP);
        IVoxelWayPoint FindPathByLayer(VoxelLayer src, VoxelLayer dst);
        IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP);
        IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP);
    }
    public interface IVoxelMapNode : ITerrainMapNode
    {
        int CloseAreaIndex { get; }
        Rectangle Range { get; }
        void ForEachNextLinks<ST>(ST st, Action<IVoxelMapNode, VoxelLayer, VoxelLayer, ST> action);
        bool HasWeight { get; }
        float Weight { get; }
    }

    public interface IVoxelWayPoint : ITerrainWayPoint
    {
        IVoxelMapNode MapNode { get; }
        new IVoxelWayPoint Next { get; }
    }
    //-----------------------------------------------------------------------------------------
}
