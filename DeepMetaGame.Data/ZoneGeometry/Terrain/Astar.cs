using DeepCore;
using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using DeepCore.XCSV;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepMetaGame.Data.ZoneGeometry.Terrain
{
    //-----------------------------------------------------------------------------------------
    public class TerrainAstar : Astar<TerrainMapNode, TerrainWayPoint>
    {
        public readonly TerrainMap terrain3D;
        public readonly TerrainSceneGraph graph;
        private List<int> close_area_count;
        private ITempMapNode[,] nodes;
        new public IAstarGraph<TerrainMapNode> SceneGraph { get => graph; }
        //-------------------------------------------------------------------------------
        public TerrainAstar(TerrainMap meta)
        {
            this.terrain3D = meta;
            this.graph = new TerrainSceneGraph(this, meta);
            this.close_area_count = base.InitCloseArea(this.graph, null);
            this.InitMetas();
        }
        protected override void Disposing()
        {
            base.Disposing();
        }
        public void CombineMesh(Triangles trangles, float weight)
        {

        }
        public ITerrainAstar CreatePathFinder()
        {
            return new TerrainTerrainAstar(this);
        }

        //-------------------------------------------------------------------------------
        #region FindPathByLayer
        internal int GetCloseAreaCount(int closeAreaIndex)
        {
            return close_area_count[closeAreaIndex];
        }
        public TerrainMapNode GetMapNode(TerrainLayer src)
        {
            return graph.GetMapNode(src.X, src.Y);
        }
        public override TerrainWayPoint GenWayPoint(TerrainMapNode node)
        {
            return new TerrainWayPoint(node);
        }
        //-------------------------------------------------------------------------------
        public ITerrainWayPoint FindPathByPos(Vector3 src, Vector3 dst, FindPathParams args = null)
        {
            var srcL = terrain3D.GetVoxelLayerByPos(src);
            var dstL = terrain3D.GetVoxelLayerByPos(dst);
            if (srcL is TerrainLayer srcLL && dstL is TerrainLayer dstLL)
            {
                return FindPathByLayer(srcLL, dstLL, args);
            }
            return null;
        }
        public ITerrainWayPoint FindPathByLayer(TerrainLayer src, TerrainLayer dst, FindPathParams args = null)
        {
            if (src == null) return null;
            if (dst == null) return null;
            var srcN = graph.GetMapNode(src.X, src.Y);
            if (srcN == null) return null;
            var dstN = graph.GetMapNode(dst.X, dst.Y);
            if (dstN == null) return null;
            return base.FindPath(srcN, dstN, args);
        }
        public ITerrainWayPoint FindPathByLayerPos(TerrainLayer src, Vector3 srcP, Vector3 dstP, FindPathParams args = null)
        {
            return FindPathByPos(srcP, dstP, args);
        }
        public ITerrainWayPoint FindPathByLayerPos(TerrainLayer src, Vector3 srcP, TerrainLayer dst, Vector3 dstP, FindPathParams args = null)
        {
            return FindPathByLayer(src, dst, args);
        }
        protected override TerrainWayPoint FindPathInternal(ITempMapNode src_node, ITempMapNode dst_node, FindPathParams args)
        {
            var srcN = src_node.MapNode as TerrainMapNode;
            var dstN = dst_node.MapNode as TerrainMapNode;
            if (srcN.Layer.TryGetNextNode(dstN.Layer.X, dstN.Layer.Y, out var next))
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
        private void InitMetas()
        {
            this.nodes = new ITempMapNode[terrain3D.XCount, terrain3D.YCount];
            base.InitGraph(graph);
        }
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            var layer = (node as TerrainMapNode).Layer;
            nodes[layer.X, layer.Y] = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            var layer = (node as TerrainMapNode).Layer;
            return nodes[layer.X, layer.Y];
        }
        #endregion
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        public class TerrainSceneGraph : IAstarGraph<TerrainMapNode>
        {
            private readonly TerrainAstar astar;
            private readonly TerrainMap terrain;
            private TerrainMapNode[,] nodes;
            public TerrainMap Terrain3D { get => terrain; }
            public int TotalNodeCount { get; private set; }
            public TerrainSceneGraph(TerrainAstar astar, TerrainMap terrain)
            {
                this.astar = astar;
                this.terrain = terrain;
                this.Init(LoadNode);
            }
            internal void Init(Func<TerrainLayer, TerrainMapNode> genNode)
            {
                this.TotalNodeCount = 0;
                this.nodes = new TerrainMapNode[terrain.XCount, terrain.YCount];
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var layer = terrain.Matrix[x, y];
                        if (layer != null)
                        {
                            nodes[x, y] = genNode(layer);
                            if (nodes[x, y] != null) TotalNodeCount++;
                        }
                    }
                }
                {
                    var list = new List<TerrainMapNode>(8);
                    this.ForEachNodes((this, list, Terrain3D), static (e, st) =>
                    {
                        st.list.Clear();
                        e.Layer.ForEachNextNodes(st, static (layer, st) =>
                        {
                            var tn = st.Item1.GetMapNode(layer.X, layer.Y);
                            if (tn != null)
                            {
                                st.list.Add(tn);
                            }
                        });
                        e.nexts = st.list.ToArray();
                    });
                }
            }
            internal TerrainMapNode LoadNode(TerrainLayer layer)
            {
                if (layer.Color != 0)
                {
                    var nc = layer.GetNextNodeCount();
                    layer.ForEachNextNodes((this, nc), static (next, st) =>
                    {
                        st.nc += next.GetNextNodeCount() - 1;
                    });
                    return new TerrainMapNode(terrain, layer, TotalNodeCount, nc);
                }
                return null;
            }
            public void Dispose()
            {
                ForEachNodes(this, static (e, st) => e.Dispose());
            }
            public TerrainMapNode GetMapNode(int x, int y)
            {
                return nodes[x, y];
            }
            public void ForEachNodes<ST>(ST st, Action<TerrainMapNode, ST> action)
            {
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var layer = nodes[x, y];
                        if (layer != null)
                        {
                            action(layer, st);
                        }
                    }
                }
            }

        }
        //-----------------------------------------------------------------------------------------
        public class TerrainTerrainAstar : ITerrainAstar
        {
            public readonly TerrainAstar astar;
            private FindPathParams args;
            private HashSet<TerrainMapNode> blocks = new HashSet<TerrainMapNode>();
            public int FindPathStepLimit { get; set; }
            public TerrainTerrainAstar(TerrainAstar astar)
            {
                this.astar = astar;
                this.args = new FindPathParams();
                this.args.TestCross = this.TestCross;
            }
            public ITerrainWayPoint FindPathByPos(Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByPos(srcP, dstP, args);
            }
            public void ForEachNodes<ST>(ST st, Action<ITerrainMapNode, ST> action)
            {
                astar.graph.ForEachNodes((st, action), static (e, st) => st.action(e, st.st));
            }
            public ITerrainMapNode GetMapNode(TerrainLayer src)
            {
                return astar.GetMapNode(src);
            }
            public ITerrainWayPoint GenWayPoint(ITerrainMapNode node)
            {
                return astar.GenWayPoint(node as TerrainMapNode);
            }
            public ITerrainWayPoint FindPathByLayer(ITerrainLayer src, ITerrainLayer dst)
            {
                return astar.FindPathByLayer(src as TerrainLayer, dst as TerrainLayer, args);
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src as TerrainLayer, srcP, dstP, args);
            }
            public ITerrainWayPoint FindPathByLayerPos(ITerrainLayer src, Vector3 srcP, ITerrainLayer dst, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src as TerrainLayer, srcP, dst as TerrainLayer, dstP, args);
            }
            public ITerrainWayPoint FindPathByLayer(TerrainLayer src, TerrainLayer dst)
            {
                return astar.FindPathByLayer(src, dst, args);
            }
            public ITerrainWayPoint FindPathByLayerPos(TerrainLayer src, Vector3 srcP, TerrainLayer dst, Vector3 dstP)
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
                if (srcL is TerrainLayer srcLL)
                {
                    mapnode = astar.GetMapNode(srcLL);
                    if (mapnode is TerrainMapNode vnode)
                    {
                        return true;
                    }
                }
                mapnode = null;
                return false;
            }
            public bool IsMapNodeBlock(ITerrainMapNode mapnode)
            {
                if (mapnode is TerrainMapNode vnode)
                {
                    return blocks.Contains(vnode);
                }
                return false;
            }
            public bool TestCross(IMapNode src, IMapNode dst)
            {
                if (src is TerrainMapNode snode)
                {
                    if (blocks.Contains(snode)) { return false; }
                }
                if (dst is TerrainMapNode dnode)
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
    public class TerrainMapNode : IMapNode, ITerrainMapNode
    {
        public readonly TerrainLayer Layer;
        internal readonly int nodeIndex;
        internal int areaLinkValue;
        internal TerrainMapNode[] nexts;
        internal float weight = 1;
        public float Height { get => Layer.Height; }
        public Vector3 Position { get => Layer.UpwardCenterPos; }
        public override int NextCount => nexts.Length;
        public override int CloseAreaIndex
        {
            get => this.areaLinkValue;
            protected set { areaLinkValue = value; }
        }
        public override object Tag { get; set; }
        public float Weight
        {
            get => weight;
            set { this.weight = value; }
        }
        public bool HasWeight { get => weight > 1; }
        //public Rectangle Range => new Rectangle(Layer.X, Layer.Y, 1, 1);

        public TerrainMapNode(TerrainMap terrain, TerrainLayer layer, int index, float weight)
        {
            this.nodeIndex = index;
            this.Layer = layer;
            this.weight = weight;
        }
        public TerrainMapNode(TerrainMap terrain, TerrainMapNode input)
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
            var tt = target as TerrainMapNode;
            var d = Vector3.Distance(Position, tt.Position);
            return d / weight;
        }
        public override float GetTargetH(IMapNode father)
        {
            var ft = father as TerrainMapNode;
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
        public void ForEachNextLinks<ST>(ST st, Action<ITerrainMapNode, TerrainLayer, TerrainLayer, ST> action)
        {
            foreach (var next in nexts)
            {
                action(next, this.Layer, next.Layer, st);
            }
        }

    }

    //-----------------------------------------------------------------------------------------
    public class TerrainWayPoint : IWayPoint<TerrainMapNode, TerrainWayPoint>, ITerrainWayPoint
    {
        public Vector3 Position { get; set; }
        public TerrainLayer Layer { get => Node.Layer; }
        //public Rectangle Range { get => Node.Range; }
        public float TotalDistance => this.GetTotalDistance();
        internal TerrainWayPoint(TerrainMapNode map_node) : base(map_node)
        {
            this.Position = base.Node.Position;
        }
        public override bool PosEquals(TerrainWayPoint w)
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
        ITerrainWayPoint ITerrainWayPoint.Next => this.Next;
        bool ITerrainWayPoint.PosEquals(ITerrainWayPoint o)
        {
            return this.PosEquals(o as TerrainWayPoint);
        }
        void ITerrainWayPoint.LinkNext(ITerrainWayPoint n)
        {
            this.LinkNext(n as TerrainWayPoint);
        }
        IEnumerator<ITerrainWayPoint> IEnumerable<ITerrainWayPoint>.GetEnumerator()
        {
            return new WayPointIterator<ITerrainWayPoint>(this);
        }

    }
    //-----------------------------------------------------------------------------------------
    //------------------------------------------------------------------------------------------------------------------------------------

    //------------------------------------------------------------------------------------------------------------------------------------
}
