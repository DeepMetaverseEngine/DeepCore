using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;


namespace DeepCore.Voxel.Data.PathFinder
{
    class DummyAstar : Disposable, IVoxelAstar
    {
        public bool IsGZip { get; set; }
        public int FindPathStepLimit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override void Disposing() { }
        public IVoxelWayPoint FindPathByLayer(VoxelLayer src, VoxelLayer dst) { return null; }
        public IVoxelWayPoint FindPathByPos(Vector3 src, Vector3 dst) { return null; }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP) { return null; }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP) { return null; }
        public void ForEachNodes<ST>(ST st, Action<IVoxelMapNode, ST> action) { }
        public IVoxelWayPoint GenWayPoint(IVoxelMapNode node) { return new DummyWayPoint(); }
        public IVoxelMapNode GetMapNode(VoxelLayer src) { return null; }
        public void CombineMesh(Triangles trangles, float weight) { }
        public void Save(OutputStream outputP) { }
        ITerrainWayPoint ITerrainAstar.FindPathByPos(Vector3 srcP, Vector3 dstP) { return null; }
        public ITerrainWayPoint FindPathByLayer(ITerrainLayer src, ITerrainLayer dst) { return null; }
        public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, Vector3 dstP) { return null; }
        public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, ITerrainLayer dst, Vector3 dstP) { return null; }
        public bool TestCross(IMapNode src, IMapNode dst) { return false; }
        public bool FillMapBlockByShape(IShape shape, bool block) { return false; }
        public bool GetMapBlockByPos(Vector3 srcP, out ITerrainMapNode mapnode) { mapnode = null; return false; }
        public bool IsMapNodeBlock(ITerrainMapNode mapnode) { return false; }
        public IEnumerable<ITerrainMapNode> GetBlockMapNodes() { return null; }
    }
    class DummyMapNode : IVoxelMapNode
    {
        public float Height { get => 0; }
        public bool HasWeight { get => false; }
        public float Weight { get => 1; }
        public int CloseAreaIndex { get; protected set; }
        public Vector3 Position { get; }
        public bool IsCross { get => false; }
        public IVoxelMapNode[] Nexts => new IVoxelMapNode[0];
        public Rectangle Range => new Rectangle();
        public VoxelLayer Layer => null;
        public void FillCross(bool canCross) { }
        public void ForEachNextLinks<ST>(ST st, Action<IVoxelMapNode, VoxelLayer, VoxelLayer, ST> action) { }
    }
    class DummyWayPoint : IVoxelWayPoint
    {
        public Vector3 Position { get; set; }
        public float TotalDistance => 0;
        public IVoxelWayPoint Next => null;
        IWayPoint IWayPoint.Next => null;
        public void LinkNext(IVoxelWayPoint n) { }
        public bool PosEquals(IVoxelWayPoint o) { return false; }
        public IEnumerator<IVoxelWayPoint> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
        public bool PosEquals(ITerrainWayPoint o) { return false; }
        public void LinkNext(ITerrainWayPoint n) { }
        IEnumerator<ITerrainWayPoint> IEnumerable<ITerrainWayPoint>.GetEnumerator() { return null; }
        public IVoxelMapNode MapNode => null;
        public Rectangle Range => MapNode.Range;
        ITerrainWayPoint ITerrainWayPoint.Next => this.Next;
    }
}
