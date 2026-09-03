using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using static DeepCore.Colors;


namespace DeepCore.Voxel.Data.PathFinder
{
    public class SpaceAstar : Astar<SpaceMapNode, SpaceWayPoint>, IVoxelAstarMap
    {
        public static readonly string FILE_EXT = ".voxs";
        public static readonly byte[] FILE_HEAD = System.Text.Encoding.ASCII.GetBytes("VOXS");
        private VoxelTerrain3D terrain;
        private SpaceSceneGraph graph;
        private BitSet32 flags;
        public bool IsGZip { get => VoxelStream.IsGZip(ref flags); private set => VoxelStream.SetGZip(ref flags, value); }
        public SpaceAstar(VoxelTerrain3D terrain, IRangeValue progress = null)
        {
            var gen = new SpaceAstarGenerator(terrain);
            var matrix = gen.RunToMatrix(progress);
            this.terrain = terrain;
            this.graph = new SpaceSceneGraph(this, terrain, matrix, progress);
            this.InitGraph(graph);
            progress?.SetRange(0, TotalNodeCount, 0);
            this.InitCloseArea(this.graph, progress);
            this.IsGZip = terrain.IsGZip;
        }
        protected override void Disposing()
        {
            base.Disposing();
        }
        public SpaceAstar(VoxelTerrain3D terrain, InputStream inputP)
        {
            this.terrain = terrain;
            VoxelStream.Load(inputP, FILE_HEAD, out flags, input =>
            {
                this.graph = new SpaceSceneGraph(this, terrain, input);
            });
            this.InitGraph(graph);
        }
        public void Save(OutputStream outputP)
        {
            VoxelStream.Save(outputP, FILE_HEAD, flags, output =>
            {
                graph.Save(output);
            });
        }
        public void CombineMesh(Triangles trangles, float weight)
        {
            //graph.ForEachNodes(node => node.weight = 1);
            trangles.ForEachTrangles((t) =>
            {
                var tb = t.AABB;
                terrain.ForEachCellsRectF(this, tb.Min.X, tb.Min.Y, tb.Max.X, tb.Max.Y, (cell, st) =>
                {
                    if (cell != null)
                    {
                        cell.ForEachLayers(this, (layer, st) =>
                        {
                            var cb = layer.GetFullBoundingBox();
                            if (tb.Intersects(cb))
                            {
                                if (CMath.IntersectRectTriangle(cb.Min, cb.Min, t.A, t.B, t.C))
                                {
                                    var node = graph.GetMapNode(layer);
                                    if (node != null)
                                    {
                                        // node.weight += weight;
                                        //node.weight = weight;
                                    }
                                }
                            }
                        });
                    }
                    return false;
                });
            });
        }

        public IVoxelAstar CreatePathFinder()
        {
            return new TerrainSpaceAstar(this);
        }

        //---------------------------------------------------------------------------------------------------------
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            (node as SpaceMapNode).tempNode = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            return (node as SpaceMapNode).tempNode;
        }
        public override SpaceWayPoint GenWayPoint(SpaceMapNode node)
        {
            return new SpaceWayPoint(node);
        }
        //---------------------------------------------------------------------------------------------------------
        public IVoxelWayPoint FindPathByPos(Vector3 src, Vector3 dst, FindPathParams args = null)
        {
            var srcL = terrain.GetVoxelLayerByPos(src);
            var dstL = terrain.GetVoxelLayerByPos(dst);
            return this.FindPathByLayerPos(srcL, src, dstL, dst, args);
        }
        public IVoxelWayPoint FindPathByLayer(VoxelLayer src, VoxelLayer dst, FindPathParams args = null)
        {
            return this.FindPathByLayerPos(src, src.UpwardCenterPos, dst, dst.UpwardCenterPos, args);
        }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP, FindPathParams args = null)
        {
            var dstL = terrain.GetVoxelLayerByPos(dstP);
            return this.FindPathByLayerPos(src, srcP, dstL, dstP, args);
        }
        public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP, FindPathParams args = null)
        {
            if (src == null) return null;
            if (dst == null) return null;
            var srcN = graph.GetMapNode(src);
            if (srcN == null) return null;
            var dstN = graph.GetMapNode(dst);
            if (dstN == null) return null;
            if (srcN == dstN)
            {
                var head = GenWayPoint(srcN);
                var tail = GenWayPoint(dstN);
                head.LinkNext(tail);
                head.Position = srcP;
                tail.Position = dstP;
                return head;
            }
            var sq = (srcN.XCount * srcN.XCount + srcN.YCount * srcN.YCount);
            var dq = (dstN.XCount * dstN.XCount + dstN.YCount * dstN.YCount);
            if ((int)Vector2.DistanceSquared(srcP, dstP) <= (sq + dq))
            {
                if (terrain.TryBlinkToTarget2D(src, srcP, dst, dstP, out var movedLayer, out var movedPos))
                {
                    var head = GenWayPoint(srcN);
                    head.Position = srcP;
                    var tail = GenWayPoint(dstN);
                    tail.Position = dstP;
                    head.LinkNext(tail);
                    return head;
                }
            }
            var path = base.FindPath(srcN, dstN, args);
            if (path != null)
            {
                path.Optimize(src, in srcP, srcN, dst, in dstP, dstN);
            }
            return path;
        }

        protected override SpaceWayPoint FindPathInternal(ITempMapNode src_node, ITempMapNode dst_node, FindPathParams args)
        {
            var srcN = src_node.MapNode as SpaceMapNode;
            var dstN = dst_node.MapNode as SpaceMapNode;
            if (srcN.ContainsNextNode(dstN))
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
        //---------------------------------------------------------------------------------------------------------
        internal class SpaceSceneGraph : IAstarGraph<SpaceMapNode>
        {
            private readonly SpaceAstar astar;
            private readonly VoxelTerrain3D terrain;
            //索引表
            private readonly SpaceMapNode[,][] matrix;
            public VoxelTerrain3D Terrain3D { get => terrain; }
            public int TotalNodeCount { get; private set; }
            internal SpaceSceneGraph(SpaceAstar astar, VoxelTerrain3D terrain, SpaceAstarGenerator.SpaceInfo[,][] space_matrix, IRangeValue progress)
            {
                progress?.SetRange(0, terrain.TotalLayerCount, 0);
                this.astar = astar;
                this.terrain = terrain;
                this.matrix = new SpaceMapNode[terrain.XCount, terrain.YCount][];
                var temp_mapNode = new HashMap<SpaceAstarGenerator.SpaceInfo, SpaceMapNode>();
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var cell = terrain.GetVoxelCell(x, y);
                        if (cell != null)
                        {
                            var layers = this.matrix[x, y] = new SpaceMapNode[cell.LayerCount];
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                var layer = cell.GetLayer(i);
                                if (layer != null)
                                {
                                    var space_layers = space_matrix[layer.X, layer.Y];
                                    if (layer.Layer >= 0 && layer.Layer < space_layers.Length)
                                    {
                                        var group = space_layers[layer.Layer];
                                        if (group != null)
                                        {
                                            if (!temp_mapNode.TryGetOrCreate(group, out var node, g => new SpaceMapNode(this, g)))
                                            {
                                                TotalNodeCount++;
                                            }
                                            //layer.MapNode = node;
                                            layers[i] = node;
                                        }
                                    }
                                    else
                                    {
                                        //Log.LazyLogger.Default.Error("??");
                                    }
                                    progress?.Add(1);
                                }
                            }
                        }
                    }
                }
                progress?.SetRange(0, TotalNodeCount, 0);
                this.ForEachNodes((this, progress), static (e, st) =>
                {
                    st.progress?.Add(1);
                    e.InitNexts(st.Item1);
                });
            }
            public SpaceSceneGraph(SpaceAstar astar, VoxelTerrain3D terrain, IInputStream input)
            {
                this.astar = astar;
                this.terrain = terrain;
                this.matrix = new SpaceMapNode[terrain.XCount, terrain.YCount][];
                this.TotalNodeCount = input.GetS32();
                var nodes = new List<SpaceMapNode>(TotalNodeCount);
                for (int i = 0; i < TotalNodeCount; i++)
                {
                    var node = new SpaceMapNode(this, input);
                    nodes.Add(node);
                }
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var cell = terrain.GetVoxelCell(x, y);
                        if (cell != null)
                        {
                            var layers = this.matrix[x, y] = new SpaceMapNode[cell.LayerCount];
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                if (input.GetBool())
                                {
                                    var layer = cell.GetLayer(i);
                                    var index = input.GetS32();
                                    layers[i] = nodes[index];
                                    //layer.MapNode = nodes[index];
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].InitNexts(this, input);
                }
            }
            public void Save(OutputStream output)
            {
                output.PutS32(TotalNodeCount);
                var nodeIndex = new HashMap<SpaceMapNode, int>();
                var nodes = GetMapNodes();
                for (int i = 0; i < TotalNodeCount; i++)
                {
                    nodes[i].Save(output);
                    nodeIndex.Add(nodes[i], i);
                }
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var layers = this.matrix[x, y];
                        var cell = terrain.GetVoxelCell(x, y);
                        if (layers != null && cell != null)
                        {
                            for (int i = 0; i < cell.LayerCount; ++i)
                            {
                                var layerNode = layers[i];
                                output.PutBool(layerNode != null);
                                if (layerNode is SpaceMapNode node)
                                {
                                    var index = nodeIndex.Get(node);
                                    output.PutS32(index);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].SaveNexts(output);
                }
            }
            public void Dispose()
            {
                ForEachNodes(this, static (e, st) => e.Dispose());
            }
            public SpaceMapNode GetMapNode(int x, int y, int layer)
            {
                //return terrain.GetVoxelLayer(x, y, layer).MapNode as SpaceMapNode;
                return matrix[x, y][layer];
            }
            public SpaceMapNode GetMapNode(VoxelLayer layer)
            {
                //return layer.MapNode as SpaceMapNode;
                return matrix[layer.X, layer.Y][layer.Layer];
            }
            public void ForEachNodes<ST>(ST st, Action<SpaceMapNode, ST> action)
            {
                var exist = new HashSet<SpaceMapNode>();
                for (int x = terrain.XCount - 1; x >= 0; --x)
                {
                    for (int y = terrain.YCount - 1; y >= 0; --y)
                    {
                        var cell = terrain.GetVoxelCell(x, y);
                        if (cell != null)
                        {
                            var layers = this.matrix[x, y];
                            for (int i = 0; i < layers.Length; ++i)
                            {
                                var layerNode = this.matrix[x, y][i];
                                //var layer = cell.GetLayer(i);
                                if (layerNode is SpaceMapNode node)
                                {
                                    if (!exist.Contains(node))
                                    {
                                        exist.Add(node);
                                        action(node, st);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            public SpaceMapNode[] GetMapNodes()
            {
                var nodes = new ArrayList<SpaceMapNode>(TotalNodeCount);
                ForEachNodes(nodes, static (e, nodes) =>
                {
                    nodes.Add(e);
                });
                return nodes.ToArray();
            }
        }
        //---------------------------------------------------------------------------------------------------------
        public class TerrainSpaceAstar : IVoxelAstar
        {
            public readonly SpaceAstar astar;
            private FindPathParams args;
            int ITerrainAstar.FindPathStepLimit { get; set; }

            public TerrainSpaceAstar(SpaceAstar astar)
            {
                this.astar = astar;
                this.args = new FindPathParams();
                this.args.TestCross = this.TestCross;
            }
            public void ForEachNodes<ST>(ST st, Action<IVoxelMapNode, ST> action)
            {
                astar.graph.ForEachNodes(st, action);
            }
            public IVoxelWayPoint GenWayPoint(IVoxelMapNode node)
            {
                return astar.GenWayPoint(node as SpaceMapNode);
            }
            public IVoxelMapNode GetMapNode(VoxelLayer src)
            {
                return astar.graph.GetMapNode(src);
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
            public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src, srcP, dstP, args);
            }
            public IVoxelWayPoint FindPathByLayerPos(VoxelLayer src, Vector3 srcP, VoxelLayer dst, Vector3 dstP)
            {
                return astar.FindPathByLayerPos(src, srcP, dst, dstP, args);
            }

            public bool TestCross(IMapNode src, IMapNode dst)
            {
                return true;
            }
            public bool FillMapBlockByShape(IShape shape, bool block)
            {
                return false;
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
            public IEnumerable<ITerrainMapNode> GetBlockMapNodes()
            {
                return new ITerrainMapNode[0];
            }
            public void Dispose()
            {

            }

        }
        //---------------------------------------------------------------------------------------------------------
    }
    //---------------------------------------------------------------------------------------------------------
    public class SpaceMapNode : IMapNode, IVoxelMapNode
    {
        internal readonly VoxelLayer root;
        internal readonly short sw, sh;
        internal readonly float weight = 1;
        internal ITempMapNode tempNode;
        private Vector3 position;
        private HashMap<SpaceMapNode, ValueTuple<VoxelLayer, VoxelLayer>> nextLinks;
        private int areaLinkValue;
        public float Height { get => root.Height; }
        public override int CloseAreaIndex
        {
            get => areaLinkValue;
            protected set { areaLinkValue = value; }
        }
        public override object Tag
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public VoxelLayer Root { get => root; }
        public int StartX { get => root.X; }
        public int StartY { get => root.Y; }
        public int XCount { get => sw; }
        public int YCount { get => sh; }
        public int RangeCount { get => (sw * sh); }
        public Rectangle Range { get => new Rectangle(root.X, root.Y, sw, sh); }
        //public override IMapNode[] Nexts => nexts;
        public Vector3 Position { get => position; }
        public float Weight { get => weight; }
        public bool HasWeight { get => weight > 1; }
        public override int NextCount => nextLinks.Count;

        internal SpaceMapNode(SpaceAstar.SpaceSceneGraph map, SpaceAstarGenerator.SpaceInfo space)
        {
            var matrix = space.Matrix;
            this.root = matrix[0, 0];
            this.weight = space.Weight;
            this.sw = (short)matrix.GetLength(0);
            this.sh = (short)matrix.GetLength(1);
            var center = matrix[XCount / 2, YCount / 2];
            this.position = new Vector3(
                root.X * map.Terrain3D.GridCellSize + XCount * map.Terrain3D.GridCellSize / 2f,
                root.Y * map.Terrain3D.GridCellSize + YCount * map.Terrain3D.GridCellSize / 2f,
                center.Upward);
        }
        internal void InitNexts(SpaceAstar.SpaceSceneGraph map)
        {
            var tempLinks = new HashMap<SpaceMapNode, List<ValueTuple<VoxelLayer, VoxelLayer>>>();
            var tempNext = new List<SpaceMapNode>(sw * sh);
            var matrix = map.Terrain3D.GetVoxelLayersPlane(root, sw, sh);
            matrix.ForEachArray2D((this, map, tempLinks, tempNext), static (st, layer, x, y) =>
            {
                layer.ForEachNextCrossNodes((st, layer), static (next, st) =>
                {
                    var nextSpace = st.st.map.GetMapNode(next);
                    if (nextSpace != null && nextSpace != st.st.Item1)
                    {
                        if (!st.st.tempLinks.TryGetOrCreate(nextSpace, out var links, static nns => new List<ValueTuple<VoxelLayer, VoxelLayer>>()))
                        {
                            st.st.tempNext.Add(nextSpace);
                        }
                        links.Add((st.layer, next));
                    }
                });
            });
            this.nextLinks = new HashMap<SpaceMapNode, ValueTuple<VoxelLayer, VoxelLayer>>();
            foreach (var links in tempLinks)
            {
                links.Value.Sort((a, b) =>
                {
                    var lenA = Vector3.DistanceSquared(a.Item1.UpwardCenterPos, links.Key.position);
                    var lenB = Vector3.DistanceSquared(b.Item1.UpwardCenterPos, links.Key.position);
                    if (lenA < lenB) { return -1; }
                    if (lenA > lenB) { return 1; }
                    return 0;
                });
                this.nextLinks.Add(links.Key, links.Value[0]);
            }
            //this.nexts = tempNext.ToArray();
        }
        internal SpaceMapNode(SpaceAstar.SpaceSceneGraph map, IInputStream input)
        {
            this.root = map.Terrain3D.GetVoxelLayer(input.GetS16(), input.GetS16(), input.GetS16());
            this.sw = input.GetS16();
            this.sh = input.GetS16();
            this.position = input.GetStruct<Vector3>();
            this.areaLinkValue = input.GetS32();
            this.weight = input.GetF32();
        }
        internal void Save(IOutputStream output)
        {
            output.PutS16(root.X);
            output.PutS16(root.Y);
            output.PutS16(root.Layer);
            output.PutS16(sw);
            output.PutS16(sh);
            output.PutStruct(this.position);
            output.PutS32(this.areaLinkValue);
            output.PutF32(this.weight);
        }
        internal void InitNexts(SpaceAstar.SpaceSceneGraph map, IInputStream input)
        {
            this.nextLinks = new HashMap<SpaceMapNode, ValueTuple<VoxelLayer, VoxelLayer>>();
            //this.nexts = new SpaceMapNode[input.GetS32()];
            var count = input.GetS32();
            for (int i = 0; i < count; i++)
            {
                var nextX = input.GetS16();
                var nextY = input.GetS16();
                var nextL = input.GetS16();
                var next = map.GetMapNode(nextX, nextY, nextL);
                var thisLX = input.GetS16();
                var thisLY = input.GetS16();
                var thisLL = input.GetS16();
                var thisLink = map.Terrain3D.GetVoxelLayer(thisLX, thisLY, thisLL);
                var nextLX = input.GetS16();
                var nextLY = input.GetS16();
                var nextLL = input.GetS16();
                var nextLink = map.Terrain3D.GetVoxelLayer(nextLX, nextLY, nextLL);
                this.nextLinks.Add(next, (thisLink, nextLink));
            }
        }
        internal void SaveNexts(IOutputStream output)
        {
            output.PutS32(nextLinks.Count);
            foreach (var next in nextLinks.Keys)
            {
                output.PutS16(next.Root.X);
                output.PutS16(next.Root.Y);
                output.PutS16(next.Root.Layer);
                var link = nextLinks[next];
                output.PutS16(link.Item1.X);
                output.PutS16(link.Item1.Y);
                output.PutS16(link.Item1.Layer);
                output.PutS16(link.Item2.X);
                output.PutS16(link.Item2.Y);
                output.PutS16(link.Item2.Layer);
            }
        }
        public override void Dispose()
        {
        }
        public bool ContainsNextNode(SpaceMapNode next)
        {
            return nextLinks.ContainsKey(next);
        }
        /// <summary>
        /// 获取哪一格连接的下个节点
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public bool TryGetNextLinkAnchor(SpaceMapNode next, out VoxelLayer thisLink, out VoxelLayer nextLink)
        {
            if (nextLinks.TryGetValue(next, out var nextLinkPair))
            {
                thisLink = nextLinkPair.Item1;
                nextLink = nextLinkPair.Item2;
                return true;
            }
            thisLink = null;
            nextLink = null;
            return false;
        }
        public void ForEachNextLinks<ST>(ST st, Action<SpaceMapNode, VoxelLayer, VoxelLayer, ST> action)
        {
            foreach (var e in nextLinks)
            {
                action(e.Key, e.Value.Item1, e.Value.Item2, st);
            }
        }
        public override bool ForEachNext<ST>(ST st, BreakPredicate<IMapNode, ST> action)
        {
            foreach (var node in nextLinks.Keys)
            {
                if (action(node, st)) return true;
            }
            return false;
        }
        public override bool TestCross(IMapNode other)
        {
            return true;
        }
        public override float GetFatherG(IMapNode father)
        {
            var tt = father as SpaceMapNode;
            var d = Vector2.Distance(Position, tt.Position);
            return d / weight;
        }
        public override float GetTargetH(IMapNode target)
        {
            var ft = target as SpaceMapNode;
            var d = Vector2.Distance(Position, ft.Position);
            return d / weight;
        }

        #region IVoxelMapNode
        int IVoxelMapNode.CloseAreaIndex => this.CloseAreaIndex;
        void IVoxelMapNode.ForEachNextLinks<ST>(ST st, Action<IVoxelMapNode, VoxelLayer, VoxelLayer, ST> action) => this.ForEachNextLinks(st, action);
        Vector3 ITerrainMapNode.Position => this.Position;
        //IVoxelMapNode[] IVoxelMapNode.Nexts => this.nexts;
        //VoxelLayer IVoxelMapNode.Layer => this.center;


        #endregion
    }
    //---------------------------------------------------------------------------------------------------------
    public class SpaceWayPoint : IWayPoint<SpaceMapNode, SpaceWayPoint>, IVoxelWayPoint
    {
        public Vector3 Position;
        internal SpaceWayPoint(SpaceMapNode map_node) : base(map_node)
        {
            this.Position = map_node.Position;
        }
        public override string ToString()
        {
            return Position.ToString();
        }
        public override bool PosEquals(SpaceWayPoint w)
        {
            return Position == w.Position;
        }
        public void Optimize(VoxelLayer src, in Vector3 srcP, SpaceMapNode srcN, VoxelLayer dst, in Vector3 dstP, SpaceMapNode dstN)
        {
            if (this.Position != srcP)
            {
                this.Position = srcP;
            }
            if (this.Next != null)
            {
                //微调起始坐标
                if (this.Next.Node == this.Node)
                {
                    this.Next.Position = dstP;
                    return;
                }
                //修正连接点，避免对角寻路卡死
                for (var wp = this.Next; wp != null && wp.Next != null; wp = wp.Next)
                {
                    if (wp.Prev.Node.TryGetNextLinkAnchor(wp.Node, out var thisLink, out var nextLink))
                    {
                        wp.Position = nextLink.UpwardCenterPos;
                    }
                    if (wp.Node.TryGetNextLinkAnchor(wp.Next.Node, out thisLink, out nextLink))
                    {
                        var link = new SpaceWayPoint(wp.Node);
                        link.Position = thisLink.UpwardCenterPos;
                        wp.InsertNext(link);
                        wp = link;
                    }
                }
                //修正头，射向连接点而非中心点
                {
                    if (this.Node.TryGetNextLinkAnchor(this.Next.Node, out var thisLink, out var nextLink))
                    {
                        if (src != thisLink)
                        {
                            var link = new SpaceWayPoint(this.Node);
                            link.Position = thisLink.UpwardCenterPos;
                            this.InsertNext(link);
                        }
                    }
                }
                //修正尾，射向连接点而非中心点
                var end = this.Tail;
                if (end.Node == dstN)
                {
                    if (end.Prev.Node.TryGetNextLinkAnchor(end.Node, out var thisLink, out var nextLink))
                    {
                        if (dst != nextLink)
                        {
                            end.Position = nextLink.UpwardCenterPos;
                        }
                        else
                        {
                            end.Position = dstP;
                            return;
                        }
                    }
                    //微调目标坐标
                    if (end.Position != dstP)
                    {
                        var link = new SpaceWayPoint(end.Node);
                        link.Position = dstP;
                        end.LinkNext(link);
                    }
                }
            }
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

        #region IVoxelWayPoint
        IVoxelMapNode IVoxelWayPoint.MapNode => this.Node;
        IVoxelWayPoint IVoxelWayPoint.Next => this.Next;
        ITerrainWayPoint ITerrainWayPoint.Next => this.Next;
        Vector3 ITerrainWayPoint.Position { get => this.Position; set { this.Position = value; } }
        public Rectangle Range => this.Node.Range;

        public float TotalDistance => this.GetTotalDistance();

        bool ITerrainWayPoint.PosEquals(ITerrainWayPoint o)
        {
            return this.PosEquals(o as SpaceWayPoint);
        }
        void ITerrainWayPoint.LinkNext(ITerrainWayPoint n)
        {
            this.LinkNext(n as SpaceWayPoint);
        }
        IEnumerator<ITerrainWayPoint> IEnumerable<ITerrainWayPoint>.GetEnumerator()
        {
            return new WayPointIterator<ITerrainWayPoint>(this);
        }
        #endregion
    }

    //-----------------------------------------------------------------------------------------------------------------
    public class SpaceAstarGenerator
    {
        private readonly VoxelTerrain3D terrain;
        // 节点矩阵索引，被吃掉后，被母体占位
        private readonly SpaceInfo[,][] matrix;
        // 剩余存货，没被吃掉的
        private readonly LinkedList<SpaceInfo> alive;

        //private ArrayList<SpaceInfo> templist;
        private IRangeValue progress;
        public SpaceAstarGenerator(VoxelTerrain3D terrain)
        {
            this.terrain = terrain;
            this.matrix = new SpaceInfo[terrain.XCount, terrain.YCount][];
            this.alive = new LinkedList<SpaceInfo>();
            //this.templist = new ArrayList<SpaceInfo>(terrain.XCount);
            for (int y = 0; y < terrain.YCount; ++y)
            {
                for (int x = 0; x < terrain.XCount; ++x)
                {
                    var cell = terrain.GetVoxelCell(x, y);
                    if (cell != null)
                    {
                        var layers = matrix[x, y] = new SpaceInfo[cell.LayerCount];
                        for (int i = cell.LayerCount - 1; i >= 0; --i)
                        {
                            var layer = cell.GetLayer(i);
                            if (layer != null && terrain.IsWalkable(layer))
                            {
                                layers[i] = new SpaceInfo(layer);
                                layers[i].aliveIndex = alive.AddLast(layers[i]);
                            }
                        }
                    }
                }
            }
        }
        private SpaceInfo GetSpaceInfo(int x, int y, int layer)
        {
            return matrix[x, y][layer];
        }
        private void Eat()
        {
            var hunter = alive.First.Value;
            if (hunter.Eat(this))
            {
                //Console.WriteLine($"{hunter} 吃饱了 食物剩余 {alive.Count}");
            }
            else
            {
                //Console.WriteLine($"{hunter} 吃不动 食物剩余 {alive.Count}");
                alive.Remove(hunter.aliveIndex);
                progress?.Add(1);
            }
        }
        private void EatReplace(SpaceInfo food, SpaceInfo hunter)
        {
            this.matrix[food.Root.X, food.Root.Y][food.Root.Layer] = hunter;
            this.alive.Remove(food.aliveIndex);
            this.progress?.Add(1);
        }
        public SpaceInfo[,][] RunToMatrix(IRangeValue progress = null)
        {
            this.progress = progress;
            progress?.SetMax(terrain.TotalLayerCount);
            progress?.SetMin(0);
            progress?.SetValue(0);
            while (alive.Count > 0)
            {
                Eat();
            }
            //Console.WriteLine($"吃完了 食物剩余 {alive.Count}");
            for (int x = 0; x < terrain.XCount; ++x)
            {
                for (int y = 0; y < terrain.YCount; ++y)
                {
                    var layers = matrix[x, y];
                    if (layers != null)
                    {
                        foreach (var layer in layers)
                        {
                            if (layer != null)
                            {
                                layer.Trim();
                            }
                        }
                    }
                }
            }
            progress?.SetValue(terrain.TotalLayerCount);
            return matrix;
        }
        public SpaceInfo[] RunToArray(AtomicRangeValue progress = null)
        {
            this.progress = progress;
            progress?.SetMax(terrain.TotalLayerCount);
            progress?.SetMin(0);
            progress?.SetValue(0);
            var matrix = RunToMatrix(progress);
            var list = new HashSet<SpaceInfo>();
            for (int y = 0; y < terrain.YCount; ++y)
            {
                for (int x = 0; x < terrain.XCount; ++x)
                {
                    var layers = matrix[x, y];
                    if (layers != null)
                    {
                        foreach (var layer in layers)
                        {
                            if (layer != null)
                            {
                                list.Add(layer);
                            }
                        }
                    }
                }
            }
            return list.ToArray();
        }

        public class SpaceInfo
        {
            private readonly VoxelLayer root;
            private readonly List2D<VoxelLayer> layers;
            private readonly bool canEat = true; // 是否可以被吃掉
            private readonly float weight = 1; // 权重
            internal LinkedListNode<SpaceInfo> aliveIndex;
            public SpaceInfo(VoxelLayer layer)
            {
                this.root = layer;
                this.layers = new List2D<VoxelLayer>();
                this.layers.Add(0, 0, layer);
                this.canEat =
                    layer.GetNextNode(0, 1) != null &&
                    layer.GetNextNode(1, 0) != null &&
                    layer.GetNextNode(0, -1) != null &&
                    layer.GetNextNode(-1, 0) != null;
                this.weight = canEat ? 1 : 0.5f; // 如果不能被吃掉，则权重为0
            }
            public bool CanEat => canEat && Count == 1;
            public int Count { get => layers.TotalCount; }
            public float Weight { get => weight; }
            public VoxelLayer Root { get => root; }
            public Rectangle Range { get => new Rectangle(root.X, root.Y, layers.XCount, layers.YCount); }
            public VoxelLayer[,] Matrix { get => layers.ToArray(); }
            public override string ToString()
            {
                return $"Space:{root.X},{root.Y}";
            }
            internal void Trim()
            {
                aliveIndex = null;
                layers.TrimExcess();
            }
            internal bool Eat(SpaceAstarGenerator gen)
            {
                if (canEat)
                {
                    bool ate = false;
                    while (true)
                    {
                        //                     if (layers.XCount > layers.YCount)
                        //                     {
                        //                         if (EatBottom(gen) == false) { return ate; }
                        //                         else { ate = true; }
                        //                         if (EatRight(gen) == false) { return ate; }
                        //                         else { ate = true; }
                        //                     }
                        //                     else
                        //                     {
                        //                         if (EatRight(gen) == false) { return ate; }
                        //                         else { ate = true; }
                        //                         if (EatBottom(gen) == false) { return ate; }
                        //                         else { ate = true; }
                        //                     }

                        if (layers.XCount > layers.YCount)
                        {
                            if (EatBottom(gen)) { ate = true; }
                            else if (EatTop(gen)) { ate = true; }
                            else { return ate; }

                            if (EatRight(gen)) { ate = true; }
                            else if (EatLeft(gen)) { ate = true; }
                            else { return ate; }
                        }
                        else
                        {
                            if (EatRight(gen)) { ate = true; }
                            else if (EatLeft(gen)) { ate = true; }
                            else { return ate; }

                            if (EatBottom(gen)) { ate = true; }
                            else if (EatTop(gen)) { ate = true; }
                            else { return ate; }
                        }
                    }
                }
                return false;
            }

            private bool EatRight(SpaceAstarGenerator gen)
            {
                // 纵向扫描
                int x = layers.XCount - 1;
                int b = layers.YCount - 1;
                var rightList = new ArrayList<SpaceInfo>();//gen.templist;//[layers.YCount];
                {
                    // 判断右边又没有食物
                    for (int y = 0; y < layers.YCount; y++)
                    {
                        var thisR = layers[x, y];
                        var foodR = thisR.GetNextNode(1, 0);
                        //右边没食物
                        if (foodR == null) { return false; }
                        var foodRQ = gen.GetSpaceInfo(foodR.X, foodR.Y, foodR.Layer);
                        //右边已经被吃
                        if (foodRQ == null || foodRQ.CanEat == false) { return false; }
                        rightList.Add(foodRQ);
                    }
                    // 判断食物是否全连
                    for (int y = 0; y < b; ++y)
                    {
                        if (rightList[y].root.GetNextNode(0, 1) != rightList[y + 1].root) return false;
                    }
                    // 判断右边是否和本体完全连接
                    if (rightList.Count > 1)
                    {
                        if (rightList[0].root.GetNextNode(-1, +0) != this.layers[x, 0]) return false;
                        if (rightList[0].root.GetNextNode(-1, +1) != this.layers[x, 1]) return false;
                        for (int y = 1; y < b; ++y)
                        {
                            var foodR = rightList[y];
                            if (foodR.root.GetNextNode(-1, -1) != this.layers[x, y - 1]) return false;
                            if (foodR.root.GetNextNode(-1, +0) != this.layers[x, y - 0]) return false;
                            if (foodR.root.GetNextNode(-1, +1) != this.layers[x, y + 1]) return false;
                        }
                        if (rightList[b].root.GetNextNode(-1, -1) != this.layers[x, b - 1]) return false;
                        if (rightList[b].root.GetNextNode(-1, -0) != this.layers[x, b - 0]) return false;
                    }
                    // 吃掉右边一条
                    layers.AppendRight();
                    for (int y = layers.YCount - 1; y >= 0; --y)
                    {
                        var foodR = rightList[y];
                        layers.Set(x + 1, y, foodR.root);
                        gen.EatReplace(foodR, this);
                        //                     gen.matrix[foodR.root.X, foodR.root.Y][foodR.root.Layer] = this;
                        //                     gen.alive.Remove(foodR);
                        //                     gen.progress.Add(1);
                    }
                }
                return true;
            }
            private bool EatLeft(SpaceAstarGenerator gen)
            {
                // 纵向扫描
                int x = 0;
                int b = layers.YCount - 1;
                var leftList = new ArrayList<SpaceInfo>();//[layers.YCount];
                {
                    // 判断右边又没有食物
                    for (int y = 0; y < layers.YCount; y++)
                    {
                        var thisL = layers[x, y];
                        var foodL = thisL.GetNextNode(-1, 0);
                        //右边没食物
                        if (foodL == null) { return false; }
                        var foodLQ = gen.GetSpaceInfo(foodL.X, foodL.Y, foodL.Layer);
                        //右边已经被吃
                        if (foodLQ == null || foodLQ.CanEat == false) { return false; }
                        leftList.Add(foodLQ);
                    }
                    // 判断食物是否全连
                    for (int y = 0; y < b; ++y)
                    {
                        if (leftList[y].root.GetNextNode(0, 1) != leftList[y + 1].root) return false;
                    }
                    // 判断右边是否和本体完全连接
                    if (leftList.Count > 1)
                    {
                        if (leftList[0].root.GetNextNode(1, +0) != this.layers[x, 0]) return false;
                        if (leftList[0].root.GetNextNode(1, +1) != this.layers[x, 1]) return false;
                        for (int y = 1; y < b; ++y)
                        {
                            var foodL = leftList[y];
                            if (foodL.root.GetNextNode(1, -1) != this.layers[x, y - 1]) return false;
                            if (foodL.root.GetNextNode(1, +0) != this.layers[x, y - 0]) return false;
                            if (foodL.root.GetNextNode(1, +1) != this.layers[x, y + 1]) return false;
                        }
                        if (leftList[b].root.GetNextNode(1, -1) != this.layers[x, b - 1]) return false;
                        if (leftList[b].root.GetNextNode(1, -0) != this.layers[x, b - 0]) return false;
                    }
                    // 吃掉右边一条
                    layers.AppendLeft();
                    for (int y = layers.YCount - 1; y >= 0; --y)
                    {
                        var foodL = leftList[y];
                        layers[x, y] = foodL.root;
                        gen.EatReplace(foodL, this);
                        //                     gen.matrix[foodR.root.X, foodR.root.Y][foodR.root.Layer] = this;
                        //                     gen.alive.Remove(foodR);
                        //                     gen.progress.Add(1);
                    }
                }
                return true;
            }
            private bool EatBottom(SpaceAstarGenerator gen)
            {
                // 横向扫描
                int y = layers.YCount - 1;
                int r = layers.XCount - 1;
                var bottomList = new ArrayList<SpaceInfo>();//gen.templist;// new SpaceInfo[layers.XCount];
                {
                    // 判断右边又没有食物
                    for (int x = 0; x < layers.XCount; x++)
                    {
                        var thisB = layers[x, y];
                        var foodB = thisB.GetNextNode(0, 1);
                        //右边没食物
                        if (foodB == null) { return false; }
                        var foodBQ = gen.GetSpaceInfo(foodB.X, foodB.Y, foodB.Layer);
                        //右边已经被吃
                        if (foodBQ == null || foodBQ.CanEat == false) { return false; }
                        bottomList.Add(foodBQ);
                    }
                    // 判断食物是否全连
                    for (int x = 0; x < r; ++x)
                    {
                        if (bottomList[x].root.GetNextNode(1, 0) != bottomList[x + 1].root) return false;
                    }
                    // 判断右边是否和本体完全连接
                    if (bottomList.Count > 1)
                    {
                        if (bottomList[0].root.GetNextNode(+0, -1) != this.layers[0, y]) return false;
                        if (bottomList[0].root.GetNextNode(+1, -1) != this.layers[1, y]) return false;
                        for (int x = 1; x < r; ++x)
                        {
                            var foodB = bottomList[x];
                            if (foodB.root.GetNextNode(-1, -1) != this.layers[x - 1, y]) return false;
                            if (foodB.root.GetNextNode(+0, -1) != this.layers[x - 0, y]) return false;
                            if (foodB.root.GetNextNode(+1, -1) != this.layers[x + 1, y]) return false;
                        }
                        if (bottomList[r].root.GetNextNode(-1, -1) != this.layers[r - 1, y]) return false;
                        if (bottomList[r].root.GetNextNode(-0, -1) != this.layers[r - 0, y]) return false;
                    }
                    // 吃掉右边一条
                    layers.AppendBottom();
                    for (int x = layers.XCount - 1; x >= 0; --x)
                    {
                        var foodB = bottomList[x];
                        layers.Set(x, y + 1, foodB.root);
                        gen.EatReplace(foodB, this);
                        //                     gen.matrix[foodB.root.X, foodB.root.Y][foodB.root.Layer] = this;
                        //                     gen.alive.Remove(foodB);
                        //                     gen.progress.Add(1);
                    }
                }
                return true;
            }
            private bool EatTop(SpaceAstarGenerator gen)
            {
                // 横向扫描
                int y = 0;
                int r = layers.XCount - 1;
                var topList = new ArrayList<SpaceInfo>();//gen.templist;// new SpaceInfo[layers.XCount];
                {
                    // 判断右边又没有食物
                    for (int x = 0; x < layers.XCount; x++)
                    {
                        var thisT = layers[x, y];
                        var foodT = thisT.GetNextNode(0, -1);
                        //右边没食物
                        if (foodT == null) { return false; }
                        var foodTQ = gen.GetSpaceInfo(foodT.X, foodT.Y, foodT.Layer);
                        //右边已经被吃
                        if (foodTQ == null || foodTQ.CanEat == false) { return false; }
                        topList.Add(foodTQ);
                    }
                    // 判断食物是否全连
                    for (int x = 0; x < r; ++x)
                    {
                        if (topList[x].root.GetNextNode(1, 0) != topList[x + 1].root) return false;
                    }
                    // 判断右边是否和本体完全连接
                    if (topList.Count > 1)
                    {
                        if (topList[0].root.GetNextNode(+0, 1) != this.layers[0, y]) return false;
                        if (topList[0].root.GetNextNode(+1, 1) != this.layers[1, y]) return false;
                        for (int x = 1; x < r; ++x)
                        {
                            var foodT = topList[x];
                            if (foodT.root.GetNextNode(-1, 1) != this.layers[x - 1, y]) return false;
                            if (foodT.root.GetNextNode(+0, 1) != this.layers[x - 0, y]) return false;
                            if (foodT.root.GetNextNode(+1, 1) != this.layers[x + 1, y]) return false;
                        }
                        if (topList[r].root.GetNextNode(-1, 1) != this.layers[r - 1, y]) return false;
                        if (topList[r].root.GetNextNode(-0, 1) != this.layers[r - 0, y]) return false;
                    }
                    // 吃掉右边一条
                    layers.AppendTop();
                    for (int x = layers.XCount - 1; x >= 0; --x)
                    {
                        var foodT = topList[x];
                        layers[x, y] = foodT.root;
                        gen.EatReplace(foodT, this);
                        //                     gen.matrix[foodB.root.X, foodB.root.Y][foodB.root.Layer] = this;
                        //                     gen.alive.Remove(foodB);
                        //                     gen.progress.Add(1);
                    }
                }
                return true;
            }
        }

    }



}
