using DeepCore;
using DeepCore.Astar;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneGeometry
{
    public class WayPointAstar : Astar<WayPointAstar.FlagGraphNode, WayPointAstar.FlagGraphPath>
    {
        private FlagGraphMap terrain;

        public WayPointAstar(SceneData map)
        {
            terrain = new FlagGraphMap(map);
            base.InitGraph(terrain);
        }
        public override FlagGraphPath GenWayPoint(FlagGraphNode node)
        {
            return new FlagGraphPath(node);
        }
        public virtual FlagGraphPath FindPath(string srcName, string dstName)
        {
            var snode = terrain.GetNode(srcName);
            if (snode == null) return null;
            var dnode = terrain.GetNode(dstName);
            if (dnode == null) return null;
            return base.FindPath(snode, dnode, null);
        }
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            (node as FlagGraphNode).TempNode = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            return (node as FlagGraphNode).TempNode;
        }
        public class FlagGraphMap : IAstarGraph<FlagGraphNode>
        {
            private readonly HashMap<string, FlagGraphNode> nodes;
            public SceneData Data { get; private set; }
            public int TotalNodeCount { get { return nodes.Count; } }
            public FlagGraphMap(SceneData map)
            {
                Data = map;
                nodes = new HashMap<string, FlagGraphNode>();
                foreach (var wp in map.Points)
                {
                    var node = new FlagGraphNode(wp);
                    nodes.Add(wp.Name, node);
                }
                foreach (var node in nodes.Values)
                {
                    node.InitNexts(this);
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
            public void ForEachNodes<ST>(ST st, Action<FlagGraphNode, ST> action)
            {
                foreach (var node in nodes.Values)
                {
                    action(node, st);
                }
            }
            internal FlagGraphNode GetNode(string flagName)
            {
                return nodes.Get(flagName);
            }
        }
        public class FlagGraphNode : IMapNode
        {
            private FlagGraphNode[] nexts_array;
            private HashMap<string, FlagGraphNode> nexts = new HashMap<string, FlagGraphNode>(1);
            private object tag;
            public string FlagName { get; private set; }
            public PointData Data { get; private set; }
            //public override IMapNode[] Nexts { get { return nexts_array; } }
            public override int NextCount => nexts_array.Length;
            public override int CloseAreaIndex
            {
                get { return 0; }
                protected set { }
            }
            public override object Tag { get => tag; set { tag = value; } }
            internal ITempMapNode TempNode;
            public FlagGraphNode(PointData data)
            {
                Data = data;
                FlagName = data.Name;
            }
            public override void Dispose()
            {
                nexts.Clear();
            }
            public override bool TestCross(IMapNode other)
            {
                var ot = other as FlagGraphNode;
                return nexts.ContainsKey(ot.FlagName);
            }
            public override float GetFatherG(IMapNode target)
            {
                var tt = target as FlagGraphNode;
                return Vector3.Distance(Data.Position, tt.Data.Position);
            }
            public override float GetTargetH(IMapNode father)
            {
                var ft = father as FlagGraphNode;
                return Vector3.Distance(Data.Position, ft.Data.Position);
            }
            public override bool ForEachNext<ST>(ST st, BreakPredicate<IMapNode, ST> action)
            {
                foreach (var node in nexts_array)
                {
                    if (action(node, st)) return true;
                }
                return false;
            }
            internal void InitNexts(FlagGraphMap map)
            {
                nexts.Clear();
                var list = new List<FlagGraphNode>(1);
                foreach (var nextName in Data.NextNames)
                {
                    var next_node = map.GetNode(nextName);
                    if (next_node != null)
                    {
                        if (nexts.TryAdd(next_node.FlagName, next_node))
                        {
                            list.Add(next_node);
                        }
                    }
                }
                nexts_array = list.ToArray();
            }
        }
        public class FlagGraphPath : IWayPoint<FlagGraphNode, FlagGraphPath>
        {
            public PointData Data { get; private set; }
            public Vector3 Position { get => Data.Position; }
            public FlagGraphPath(FlagGraphNode map_node) : base(map_node)
            {
                Data = Node.Data;
            }
            public override bool PosEquals(FlagGraphPath w)
            {
                return this.Position == w.Position;
            }
        }

    }
}
