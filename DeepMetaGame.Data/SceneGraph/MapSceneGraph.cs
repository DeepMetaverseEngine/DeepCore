using DeepCore;
using DeepCore.Astar;
using DeepCore.Geometry;
using DeepCore.Log;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.SceneGraph
{
    /// <summary>
    /// 跨场景寻路网格
    /// </summary>
    public class MapSceneGrapAstar : Astar<MapSceneGrapAstar.SceneGraphNode, MapSceneGrapAstar.SceneGraphPath>
    {
        private static Logger log = new LazyLogger(nameof(MapSceneGrapAstar));
        private SceneGraphMap terrain;
        public SceneGraphMap MapNodes { get => terrain; }
        public MapSceneGrapAstar(EditorTemplates dataRoot)
        {
            terrain = new SceneGraphMap(dataRoot);
            base.InitGraph(terrain);
        }
        public SceneGraphData SaveSceneGraphData()
        {
            var ret = new SceneGraphData();
            ret.nodes = new HashMap<int, SceneMapNode>();
            foreach (var node in MapNodes.GetAllNodes())
            {
                ret.nodes.Add(node.MapID, node.NodeData);
            }
            return ret;
        }
        public void LoadSceneGraphData(SceneGraphData data)
        {
            foreach (var node in data.nodes)
            {
                var mapnode = MapNodes.GetNode(node.Key);
                if (mapnode != null)
                {
                    mapnode.NodeData.worldX = node.Value.worldX;
                    mapnode.NodeData.worldY = node.Value.worldY;
                }
            }
        }
        public override SceneGraphPath GenWayPoint(SceneGraphNode node)
        {
            return new SceneGraphPath(node);
        }
        /// <summary>
        /// 找到最近的入口
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public SceneNextLink GetNearEntry(int mapID, Vector3 pos)
        {
            var node = terrain.GetNode(mapID);
            if (node != null)
            {
                return node.GetNearEntry(pos);
            }
            return null;
        }
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            (node as SceneGraphNode).TempNode = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            return (node as SceneGraphNode).TempNode;
        }

        /// <summary>
        /// 跨场景寻路
        /// </summary>
        /// <param name="srcMapID"></param>
        /// <param name="dstMapID"></param>
        /// <param name="dstMapNearPos">目标场景最近点</param>
        /// <returns></returns>
        public ArrayList<SceneNextLink> FindPath(int srcMapID, int dstMapID, Vector3? dstMapNearPos)
        {
            var snode = terrain.GetNode(srcMapID);
            if (snode == null) return null;
            var dnode = terrain.GetNode(dstMapID);
            if (dnode == null) return null;
            SceneGraphPath path;
            lock (this)
            {
                path = base.FindPath(snode, dnode, null);
            }
            if (path != null)
            {
                var ret = new ArrayList<SceneNextLink>();
                foreach (SceneGraphPath wp in path)
                {
                    var next = wp.Next;
                    if (next != null)
                    {
                        var info = wp.Node.GetNextInfo(next.Node.MapID);
                        ret.Add(info);
                    }
                }
                if (ret.Count > 0 && dstMapNearPos.HasValue)
                {
                    var near = dnode.GetNearEntry(dstMapNearPos.Value);
                    if (near != ret[ret.Count - 1])
                    {
                        ret.Add(near);
                    }
                }
                return ret;
            }
            return null;
        }
        public class SceneGraphMap : IAstarGraph<SceneGraphNode>
        {
            private readonly HashMap<int, SceneGraphNode> nodes;
            public int TotalNodeCount { get { return nodes.Count; } }
            public SceneGraphMap(EditorTemplates dataRoot)
            {
                var scenes = dataRoot.CacheAllScenes();
                nodes = new HashMap<int, SceneGraphNode>(scenes.Count);
                foreach (var data in scenes.Values)
                {
                    var node = new SceneGraphNode(data);
                    nodes.Add(node.MapID, node);
                }
                foreach (var node in nodes.Values)
                {
                    node.InitNexts(dataRoot, this);
                }
            }
            public void Dispose()
            {
                foreach (var node in nodes.Values)
                {
                    node.Dispose();
                }
                nodes.Clear();
            }
            public void ForEachNodes<ST>(ST st, Action<SceneGraphNode, ST> action)
            {
                foreach (var node in nodes.Values)
                {
                    action(node, st);
                }
            }
            public SceneGraphNode GetNode(int mapID)
            {
                return nodes.Get(mapID);
            }
        }
        public class SceneGraphNode : IMapNode
        {
            /// <summary>
            /// 下个场景连接点
            /// </summary>
            private SceneGraphNode[] nexts_array;
            private HashMap<int, SceneNextLink> nexts = new HashMap<int, SceneNextLink>(1);

            public SceneData Data { get; }
            public SceneMapNode NodeData { get; }
            public int MapID { get => Data.ID; }
            public override int NextCount => nexts_array.Length;
            //public override IMapNode[] Nexts { get { return nexts_array; } }
            public override int CloseAreaIndex { get { return 0; } protected set { } }
            public override object Tag { get; set; }

            internal ITempMapNode TempNode;

            public SceneGraphNode(SceneData data)
            {
                Data = data;
                NodeData = new SceneMapNode()
                {
                    id = data.ID,
                    worldW = data.Terrain.XCount,
                    worldH = data.Terrain.YCount,
                };
            }
            public override bool ForEachNext<ST>(ST st, BreakPredicate<IMapNode, ST> action)
            {
                foreach (var node in nexts_array)
                {
                    if (action(node, st)) return true;
                }
                return false;
            }
            public override string ToString()
            {
                return Data.ToString();
            }
            public override void Dispose()
            {
                nexts.Clear();
            }
            public override bool TestCross(IMapNode other)
            {
                return nexts.ContainsKey((other as SceneGraphNode).MapID);
            }
            public override float GetFatherG(IMapNode father) { return 1; }
            public override float GetTargetH(IMapNode target) { return 1; }
            protected internal virtual void InitNexts(EditorTemplates dataRoot, SceneGraphMap map)
            {
                nexts.Clear();
                var list = new List<SceneGraphNode>(1);
                NodeData.connect = Data.GetSceneNextLinks();
                {
                    foreach (var next in NodeData.connect)
                    {
                        var ss = dataRoot.LoadScene(MapID, true, false, false);
                        if (ss != null && ss.Regions.TryFind(e => e.Name == next.from_flag_name, out var from_rg))
                        {
                            next.from_flag_pos = new Vector3(from_rg.X, from_rg.Y, from_rg.Z);
                        }
                        else
                        {
                            throw new Exception($"Currernt Link Data Error : MapID={MapID} : {next}");
                        }
                        var next_node = map.GetNode(next.to_map_id);
                        if (next_node != null)
                        {
                            if (!nexts.ContainsKey(next_node.MapID))
                            {
                                var ds = dataRoot.LoadScene(next_node.MapID, true, false, false);
                                if (ds != null && ds.Regions.TryFind(e => e.Name == next.to_flag_name, out var next_rg))
                                {
                                    next.to_flag_pos = new Vector3(next_rg.X, next_rg.Y, next_rg.Z);
                                    nexts.Add(next_node.MapID, next);
                                }
                                else
                                {
                                    //throw new Exception($"Next Link Data Error : MapID={MapID} : {next}");
                                    log.Error($"Next Link Data Error : MapID={MapID} : {next}");
                                }
                            }
                            list.Add(next_node);
                        }
                        else
                        {
                            log.Error($"Next Link Data Error : MapID={MapID} : {next}");
                        }
                    }
                }
                nexts_array = list.ToArray();
            }
            internal SceneNextLink GetNextInfo(int mapID)
            {
                return nexts.Get(mapID);
            }
            /// <summary>
            /// 找到最近的入口
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            internal SceneNextLink GetNearEntry(Vector3 pos)
            {
                SceneNextLink ret = null;
                var min = float.MaxValue;
                foreach (var entry in NodeData.connect)
                {
                    var d = Vector3.DistanceSquared(entry.from_flag_pos, pos);
                    if (d < min)
                    {
                        ret = entry;
                        min = d;
                    }
                }
                return ret;
            }
        }
        public class SceneGraphPath : IWayPoint<SceneGraphNode, SceneGraphPath>
        {
            public SceneData Data { get => Node.Data; }
            public SceneGraphPath(SceneGraphNode map_node) : base(map_node)
            {

            }
            public override bool PosEquals(SceneGraphPath w)
            {
                return Data.ID == w.Data.ID;
            }
        }



    }

}
