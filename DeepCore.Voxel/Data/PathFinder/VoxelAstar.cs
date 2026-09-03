using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections.Generic;
using static DeepCore.Colors;
using static DeepCore.Voxel.Data.PathFinder.SpaceAstar;

namespace DeepCore.Voxel.Data.PathFinder
{
    //-----------------------------------------------------------------------------------------
    public class VoxelAstar : Astar<VoxelMapNode, VoxelWayPoint>, IVoxelAstarMap
    {
        public static readonly string FILE_EXT = ".voxp";
        public static readonly byte[] FILE_HEAD = System.Text.Encoding.ASCII.GetBytes("VOXP");
        private VoxelTerrain3D terrain3D;
        private VoxelSceneGraph graph;
        private List<int> close_area_count;
        private ITempMapNode[,][] nodes;
        private BitSet32 flags;
        public bool IsGZip { get => flags.Get(0); private set => flags.Set(0, value); }
        public VoxelTerrain3D Terrain3D { get => terrain3D; }
        new public IAstarGraph<VoxelMapNode> SceneGraph { get => graph; }
        //-------------------------------------------------------------------------------
        public VoxelAstar(VoxelTerrain3D meta, IRangeValue progress = null)
        {
            this.terrain3D = meta;
            progress?.Reset(meta.TotalLayerCount * 4);
            this.graph = new VoxelSceneGraph(this, meta, progress);
            this.close_area_count = base.InitCloseArea(this.graph, progress);
            this.InitMetas(progress);
            this.IsGZip = meta.IsGZip;
        }
        public VoxelAstar(VoxelTerrain3D meta, InputStream inputP)
        {
            this.terrain3D = meta;
            VoxelStream.Load(inputP, FILE_HEAD, out flags, input =>
            {
                this.close_area_count = input.GetList(
                    static input => input.GetS32());
                this.graph = new VoxelSceneGraph(this, meta, input);
            });
            this.InitMetas(null);
        }
        public void Save(OutputStream outputP)
        {
            VoxelStream.Save(outputP, FILE_HEAD, flags, output =>
            {
                output.PutList(close_area_count,
                 static (output, v) => output.PutS32(v));
                this.graph.Save(output);
            });
        }
        protected override void Disposing()
        {
            base.Disposing();
        }
        public void CombineMesh(Triangles trangles, float weight)
        {

        }
        public IVoxelAstar CreatePathFinder()
        {
            return new TerrainVoxelAstar(this);
        }

        //-------------------------------------------------------------------------------
        #region FindPathByLayer
        internal int GetCloseAreaCount(int closeAreaIndex)
        {
            return close_area_count[closeAreaIndex];
        }
        public VoxelMapNode GetMapNode(VoxelLayer src)
        {
            return graph.GetMapNode(src.X, src.Y, src.Layer);
        }
        public override VoxelWayPoint GenWayPoint(VoxelMapNode node)
        {
            return new VoxelWayPoint(node);
        }
        //-------------------------------------------------------------------------------
        public IVoxelWayPoint FindPathByPos(Vector3 src, Vector3 dst, FindPathParams args = null)
        {
            var srcL = terrain3D.GetVoxelLayerByPos(src);
            var dstL = terrain3D.GetVoxelLayerByPos(dst);
            if (srcL != null && dstL != null)
            {
                return FindPathByLayer(srcL, dstL, args);
            }
            return null;
        }
        public IVoxelWayPoint FindPathByLayer(VoxelLayer src, VoxelLayer dst, FindPathParams args = null)
        {
            if (src == null) return null;
            if (dst == null) return null;
            var srcN = graph.GetMapNode(src.X, src.Y, src.Layer);
            if (srcN == null) return null;
            var dstN = graph.GetMapNode(dst.X, dst.Y, dst.Layer);
            if (dstN == null) return null;
            return base.FindPath(srcN, dstN, args);
        }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP, FindPathParams args = null)
        {
            return FindPathByPos(srcP, dstP, args);
        }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP, FindPathParams args = null)
        {
            return FindPathByLayer(src, dst, args);
        }
        protected override VoxelWayPoint FindPathInternal(ITempMapNode src_node, ITempMapNode dst_node, FindPathParams args)
        {
            var srcN = src_node.MapNode as VoxelMapNode;
            var dstN = dst_node.MapNode as VoxelMapNode;
            if (srcN.Layer.ContainsNextNode(dstN.Layer))
            {
                var head = GenWayPoint(srcN);
                var tail = GenWayPoint(dstN);
                head.LinkNext(tail);
                return head;
            }
            lock (this)
            {
                return base.FindPathInternal(src_node, dst_node, args);
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// 共享MapNode内存，只缓存TempMapNode
        /// </summary>
        private void InitMetas(IRangeValue progress)
        {
            this.nodes = new ITempMapNode[terrain3D.XCount, terrain3D.YCount][];
            for (int x = terrain3D.XCount - 1; x >= 0; --x)
            {
                for (int y = terrain3D.YCount - 1; y >= 0; --y)
                {
                    var cell = terrain3D.GetVoxelCell(x, y);
                    if (cell != null)
                    {
                        nodes[x, y] = new ITempMapNode[cell.LayerCount];
                        progress?.Add(cell.LayerCount);
                    }
                }
            }
            base.InitGraph(graph);
        }
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            var layer = (node as VoxelMapNode).Layer;
            nodes[layer.X, layer.Y][layer.Layer] = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            var layer = (node as VoxelMapNode).Layer;
            return nodes[layer.X, layer.Y][layer.Layer];
        }
        #endregion
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        class VoxelSceneGraph : IAstarGraph<VoxelMapNode>
        {
            private readonly VoxelAstar astar;
            private readonly VoxelTerrain3D terrain;
            private VoxelMapNode[,][] nodes;
            public VoxelTerrain3D Terrain3D { get => terrain; }
            public int TotalNodeCount { get; private set; }
            public VoxelSceneGraph(VoxelAstar astar, VoxelTerrain3D terrain, IRangeValue progress)
            {
                this.astar = astar;
                this.terrain = terrain;
                this.Init(LoadNode, progress);
            }
            internal VoxelSceneGraph(VoxelAstar astar, VoxelTerrain3D terrain, IInputStream input)
            {
                this.astar = astar;
                this.terrain = terrain;
                this.Init((la) => LoadNode(la, input), null);
            }
            internal void Init(Func<VoxelLayer, VoxelMapNode> genNode, IRangeValue progress)
            {
                this.TotalNodeCount = 0;
                this.nodes = new VoxelMapNode[terrain.XCount, terrain.YCount][];
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var cell = terrain.GetVoxelCell(x, y);
                        if (cell != null)
                        {
                            var layers = nodes[x, y] = new VoxelMapNode[cell.LayerCount];
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                var layer = cell.GetLayer(i);
                                layers[i] = genNode(layer);
                                progress?.Add(1);
                                if (layers[i] != null) TotalNodeCount++;
                            }
                        }
                    }
                }
                {
                    var list = new List<VoxelMapNode>(8);
                    this.ForEachNodes((this, list, progress, Terrain3D), static (e, st) =>
                    {
                        st.list.Clear();
                        if (st.Terrain3D.BuildConfig.LinkDir == VoxelLinkDirection.Cross)
                        {
                            e.Layer.ForEachNextCrossNodes(st, static (layer, st) =>
                            {
                                var tn = st.Item1.GetMapNode(layer.X, layer.Y, layer.Layer);
                                if (tn != null)
                                {
                                    st.list.Add(tn);
                                }
                            });
                        }
                        else
                        {
                            e.Layer.ForEachNextNodes(st, static (layer, st) =>
                            {
                                var tn = st.Item1.GetMapNode(layer.X, layer.Y, layer.Layer);
                                if (tn != null)
                                {
                                    st.list.Add(tn);
                                }
                            });
                        }
                        e.nexts = st.list.ToArray();
                        st.progress?.Add(1);
                    });
                }
            }
            internal VoxelMapNode LoadNode(VoxelLayer layer)
            {
                if (astar.Terrain3D.IsWalkable(layer))
                {
                    var nc = layer.GetNextNodeCount();
                    layer.ForEachNextNodes((this, nc), static (next, st) =>
                    {
                        st.nc += next.GetNextNodeCount() - 1;
                    });
                    return new VoxelMapNode(terrain, layer, TotalNodeCount, nc);
                }
                return null;
            }
            internal VoxelMapNode LoadNode(VoxelLayer layer, IInputStream input)
            {
                if (input.GetBool())
                {
                    var index = input.GetS32();
                    var weight = input.GetF32();
                    var ret = new VoxelMapNode(terrain, layer, index, weight);
                    ret.areaLinkValue = input.GetS32();
                    return ret;
                }
                return null;
            }
            internal void Save(OutputStream output)
            {
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var cell = nodes[x, y];
                        if (cell != null)
                        {
                            for (int i = 0; i < cell.Length; ++i)
                            {
                                var layer = cell[i];
                                output.PutBool(layer != null);
                                if (layer != null)
                                {
                                    output.PutS32(layer.nodeIndex);
                                    output.PutF32(layer.weight);
                                    output.PutS32(layer.CloseAreaIndex);
                                }
                            }
                        }
                    }
                }
            }
            public void Dispose()
            {
                ForEachNodes(this, static (e, st) => e.Dispose());
            }
            public VoxelMapNode GetMapNode(int x, int y, int layer)
            {
                return nodes[x, y][layer];
            }
            public void ForEachNodes<ST>(ST st, Action<VoxelMapNode, ST> action)
            {
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var layers = nodes[x, y];
                        if (layers != null)
                        {
                            foreach (var layer in layers)
                            {
                                if (layer != null)
                                {
                                    action(layer, st);
                                }
                            }
                        }
                    }
                }
            }

        }
        //-----------------------------------------------------------------------------------------
        public class TerrainVoxelAstar : IVoxelAstar
        {
            public readonly VoxelAstar astar;
            private FindPathParams args;
            private HashSet<VoxelMapNode> blocks = new HashSet<VoxelMapNode>();
            int ITerrainAstar.FindPathStepLimit { get; set; }
            public TerrainVoxelAstar(VoxelAstar astar)
            {
                this.astar = astar;
                this.args = new FindPathParams();
                this.args.TestCross = this.TestCross;
            }
            public void ForEachNodes<ST>(ST st, Action<IVoxelMapNode, ST> action)
            {
                astar.graph.ForEachNodes((st, action), static (e, st) => st.action(e, st.st));
            }
            public IVoxelMapNode GetMapNode(VoxelLayer src)
            {
                return astar.GetMapNode(src);
            }
            public IVoxelWayPoint GenWayPoint(IVoxelMapNode node)
            {
                return astar.GenWayPoint(node as VoxelMapNode);
            }
            public ITerrainWayPoint FindPathByPos(Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByPos(srcP, dstP, args);
            }
            public ITerrainWayPoint FindPathByLayer(ITerrainLayer src, ITerrainLayer dst)
            {
                return astar.FindPathByLayer(src as VoxelLayer, dst as VoxelLayer, args);
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src as VoxelLayer, srcP, dstP, args);
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, ITerrainLayer dst, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src as VoxelLayer, srcP, dst as VoxelLayer, dstP, args);
            }
            IVoxelWayPoint IVoxelAstar.FindPathByPos(Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByPos(srcP, dstP, args);
            }
            public IVoxelWayPoint FindPathByLayer(VoxelLayer src, VoxelLayer dst)
            {
                return astar.FindPathByLayer(src, dst, args);
            }
            IVoxelWayPoint IVoxelAstar.FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src, srcP, dstP, args);
            }
            public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src, srcP, dst, dstP, args);
            }

            public bool FillMapBlockByShape(IShape shape, bool block)
            {
                if (block)
                {
                    return astar.terrain3D.ForEachByShape(shape, this, static (st, layer) =>
                    {
                        var mapnode = st.astar.GetMapNode(layer);
                        if (mapnode != null)
                        {
                            st.blocks.Add(mapnode);
                        }
                        return false;
                    });
                }
                else
                {
                    return astar.terrain3D.ForEachByShape(shape, this, static (st, layer) =>
                    {
                        var mapnode = st.astar.GetMapNode(layer);
                        if (mapnode != null)
                        {
                            st.blocks.Remove(mapnode);
                        }
                        return false;
                    });
                }
            }
            public IEnumerable<ITerrainMapNode> GetBlockMapNodes()
            {
                return blocks;
            }
            public bool GetMapBlockByPos(Vector3 srcP, out ITerrainMapNode mapnode)
            {
                var srcL = astar.terrain3D.GetVoxelLayerByPos(srcP);
                if (srcL != null)
                {
                    mapnode = astar.GetMapNode(srcL);
                    if (mapnode is VoxelMapNode vnode)
                    {
                        return true;
                    }
                }
                mapnode = null;
                return false;
            }
            public bool IsMapNodeBlock(ITerrainMapNode mapnode)
            {
                if (mapnode is VoxelMapNode vnode)
                {
                    return blocks.Contains(vnode);
                }
                return false;
            }
            public bool TestCross(IMapNode src, IMapNode dst)
            {
                if (src is VoxelMapNode snode)
                {
                    if (blocks.Contains(snode)) { return false; }
                }
                if (dst is VoxelMapNode dnode)
                {
                    if (blocks.Contains(dnode)) { return false; }
                }
                return true;
            }
            public void Dispose()
            {
                blocks.Clear();
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    public class VoxelMapNode : IMapNode, IVoxelMapNode
    {
        public readonly VoxelLayer Layer;
        internal readonly int nodeIndex;
        internal int areaLinkValue;
        internal VoxelMapNode[] nexts;
        internal float weight = 1;
        public float Height { get => Layer.Height; }
        public Vector3 Position { get => Layer.UpwardCenterPos; }
        public override int NextCount => nexts.Length;
        //public override IMapNode[] Nexts { get { return nexts; } }
        public override int CloseAreaIndex
        {
            get => this.areaLinkValue;
            protected set { areaLinkValue = value; }
        }
        public override object Tag
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public float Weight
        {
            get => weight;
            set { this.weight = value; }
        }
        public bool HasWeight { get => weight > 1; }
        public Rectangle Range => new Rectangle(Layer.X, Layer.Y, 1, 1);

        public VoxelMapNode(VoxelTerrain3D terrain, VoxelLayer layer, int index, float weight)
        {
            this.nodeIndex = index;
            this.Layer = layer;
            this.weight = weight;
        }
        public VoxelMapNode(VoxelTerrain3D terrain, VoxelMapNode input)
        {
            this.nodeIndex = input.nodeIndex;
            this.Layer = input.Layer;
            this.areaLinkValue = input.areaLinkValue;
            this.weight = input.weight;
        }
        public override void Dispose() { }
        public override bool TestCross(IMapNode other)
        {
            return true;
        }
        public override float GetFatherG(IMapNode target)
        {
            var tt = target as VoxelMapNode;
            var d = Vector3.Distance(Position, tt.Position);
            return d / weight;
        }
        public override float GetTargetH(IMapNode father)
        {
            var ft = father as VoxelMapNode;
            var d = Vector3.Distance(Position, ft.Position);
            return d / weight;
        }
        public override bool ForEachNext<ST>(ST st, BreakPredicate<IMapNode, ST> action)
        {
            foreach (var node in nexts)
            {
                if (action(node, st)) return true;
            }
            return false;
        }
        public void ForEachNextLinks<ST>(ST st, Action<IVoxelMapNode, VoxelLayer, VoxelLayer, ST> action)
        {
            foreach (var next in nexts)
            {
                action(next, this.Layer, next.Layer, st);
            }
        }

        int IVoxelMapNode.CloseAreaIndex => this.CloseAreaIndex;
        Vector3 ITerrainMapNode.Position => this.Position;

    }

    //-----------------------------------------------------------------------------------------
    public class VoxelWayPoint : IWayPoint<VoxelMapNode, VoxelWayPoint>, IVoxelWayPoint
    {
        public Vector3 Position { get; set; }
        public VoxelLayer Layer { get => Node.Layer; }
        public Rectangle Range { get => Node.Range; }

        internal VoxelWayPoint(VoxelMapNode map_node) : base(map_node)
        {
            this.Position = base.Node.Position;
        }
        public override bool PosEquals(VoxelWayPoint w)
        {
            return this.Position == w.Position;
        }
        public virtual float GetTotalDistance()
        {
            float ret = 0;
            var cur = this;
            while (cur != null)
            {
                var nex = cur.Next;
                if (cur != null && nex != null)
                {
                    ret += Vector3.Distance(cur.Position, nex.Position);
                }
                cur = nex;
            }
            return ret;
        }

        IVoxelMapNode IVoxelWayPoint.MapNode => this.Node;
        IVoxelWayPoint IVoxelWayPoint.Next => this.Next;
        ITerrainWayPoint ITerrainWayPoint.Next => this.Next;
        float ITerrainWayPoint.TotalDistance => this.GetTotalDistance();
        bool ITerrainWayPoint.PosEquals(ITerrainWayPoint o)
        {
            return this.PosEquals(o as VoxelWayPoint);
        }
        void ITerrainWayPoint.LinkNext(ITerrainWayPoint n)
        {
            this.LinkNext(n as VoxelWayPoint);
        }
        IEnumerator<ITerrainWayPoint> IEnumerable<ITerrainWayPoint>.GetEnumerator()
        {
            return new WayPointIterator<ITerrainWayPoint>(this);
        }

    }
    //-----------------------------------------------------------------------------------------
}
