using DeepCore;
using DeepCore.Geometry;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneGeometry
{
    public class BezierCurveTrack : Recyclable, IEnumerable<BezierCurveTrack.Node>
    {
        //-------------------------------------------------------
        private List<Vector3> temp_points = new List<Vector3>();
        private HashMap<string, PointData> t_exists = new HashMap<string, PointData>();
        private double totalLen = 0d;
        private List<Node> nodes = new List<Node>();
        //-------------------------------------------------------
        public Node First { get => nodes.Count == 0 ? null : nodes[0]; }
        public Node Last { get => nodes.Count == 0 ? null : nodes[nodes.Count - 1]; }
        public int Count => nodes.Count;
        public double TotalLength => totalLen;
        public BezierCurveTrack()
        {
        }
        protected override void Disposing()
        {
            Clear();
        }
        public void Clear()
        {
            totalLen = 0;
            nodes.Clear();
            t_exists.Clear();
            temp_points.Clear();
        }
        public void AddPoint(SceneData scene, PointData p, float step)
        {
            AddPoint(t_exists, temp_points, (nextName) => scene.GetFlagByName(nextName) as PointData, p, step, (int)MathF.Max(2, MathF.Min(scene.Terrain.GridCellW, scene.Terrain.GridCellH) / step));
        }
        public void AddPoint(SceneData scene, PointData p, float step, int n)
        {
            AddPoint(t_exists, temp_points, (nextName) => scene.GetFlagByName(nextName) as PointData, p, step, n);
        }
        public void AddPoint(Func<string, PointData> getPointData, PointData p, float step, float gridSize)
        {
            AddPoint(t_exists, temp_points, getPointData, p, step, (int)MathF.Max(2, gridSize / step));
        }
        public void AddPoint(Func<string, PointData> getPointData, PointData p, float step, int n)
        {
            AddPoint(t_exists, temp_points, getPointData, p, step, n);
        }
        private void AddPoint(HashMap<string, PointData> t_exists, List<Vector3> t_points, Func<string, PointData> getPointData, PointData p, float step, int n)
        {
            if (t_exists.ContainsKey(p.Name))
                return;
            t_exists.Add(p.Name, p);
            var p0 = p.Position;
            var p1 = DeepCore.Geometry.VectorDrawing.VectorOffset(p0, p.TangentSize, p.Direction + CMath.RADIANS_90);
            foreach (var nextName in p.NextNames)
            {
                if (getPointData(nextName) is PointData next)
                {
                    var nextTS = next.TangentSize;
                    var p3 = next.Position;
                    var p2 = DeepCore.Geometry.VectorDrawing.VectorOffset(p3, nextTS, next.Direction - CMath.RADIANS_90);
                    var bz = AllocPools != null ? AllocPools.Alloc<CubicBezier>().Init(p0, p1, p2, p3) : new DeepCore.Geometry.CubicBezier(p0, p1, p2, p3);
                    using (bz)
                    {
                        t_points.Clear();
                        bz.SampleStep(t_points, step, n);
                        if (t_points.Count > 0)
                        {
                            var p_start = p0;
                            foreach (var pp in t_points)
                            {
                                var node = new Node()
                                {
                                    Position = pp,
                                    Index = nodes.Count,
                                    Previous = this.Last,
                                    StartFlag = p,
                                    TargetFlag = next,
                                };
                                if (Last != null)
                                {
                                    Last.Next = node;
                                    totalLen += Vector3.Distance(p_start, pp);
                                }
                                node.ToStartDistance = totalLen;
                                nodes.Add(node);
                                p_start = pp;
                            }
                            t_points.Clear();
                        }                    
                    }
                    AddPoint(t_exists, t_points, getPointData, next, step, n);
                }
            }
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public class Node
        {
            public Vector3 Position { get; internal set; }
            public int Index { get; internal set; }
            /// <summary>
            /// 到头的距离
            /// </summary>
            public double ToStartDistance { get; internal set; }
            public Node Next { get; internal set; }
            public Node Previous { get; internal set; }
            public PointData TargetFlag { get; internal set; }
            public PointData StartFlag { get; internal set; }

            public object Tag { get; set; }
            public override string ToString()
            {
                return $"{Index} : {ToStartDistance}";
            }
        }

        /// <summary>
        /// 测算当前节点到终点距离
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double ToEndDistance(IBezierTrackAgent node)
        {
            if (node?.CurrentTrack != null)
            {
                return this.TotalLength - node.CurrentTrack.ToStartDistance;
            }
            return 0;
        }
        /// <summary>
        /// 测算当前节点到终点距离
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double ToEndDistance(Node node)
        {
            if (node != null)
            {
                return this.TotalLength - node.ToStartDistance;
            }
            return 0;
        }

        /// <summary>
        /// 测算2个节点距离
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public float ForwardDistance(IBezierTrackAgent start, IBezierTrackAgent end)
        {
            return ForwardDistance(start.CurrentTrack, start.Position, end.CurrentTrack, end.Position);
        }
        /// <summary>
        /// 测算2个节点距离
        /// </summary>
        /// <param name="start"></param>
        /// <param name="startPos"></param>
        /// <param name="end"></param>
        /// <param name="endPos"></param>
        /// <returns></returns>
        public float ForwardDistance(BezierCurveTrack.Node start, in Vector3 startPos, BezierCurveTrack.Node end, in Vector3 endPos)
        {
            var node1 = start;
            var node2 = end;
            if (node1 != null && node2 != null)
            {
                var pos1 = startPos;
                var pos2 = endPos;
                if (node1.Index == node2.Index)
                {
                    return Vector3.Distance(pos2, node2.Position) - Vector3.Distance(pos1, node1.Position);
                }
                if (node1.Index < node2.Index)
                {
                    float len = Vector3.Distance(pos1, node1.Position);
                    for (int i = node1.Index; i >= 0 && i < nodes.Count && i < node2.Index; i++)
                    {
                        var a = nodes[i];
                        var b = nodes[i + 1];
                        len += Vector3.Distance(a.Position, b.Position);
                    }
                    if (node2.Previous != null)
                    {
                        len += Vector3.Distance(pos2, node2.Previous.Position);
                    }
                    return len;
                }
                if (node2.Index < node1.Index)
                {
                    float len = Vector3.Distance(pos2, node2.Position);
                    for (int i = node2.Index; i >= 0 && i < nodes.Count && i < node1.Index; i++)
                    {
                        var a = nodes[i];
                        var b = nodes[i + 1];
                        len += Vector3.Distance(a.Position, b.Position);
                    }
                    if (node1.Previous != null)
                    {
                        len += Vector3.Distance(pos1, node1.Previous.Position);
                    }
                    return -len;
                }
            }
            return 0;
        }
    }

    public interface IBezierTrackAgent
    {
        BezierCurveTrack.Node CurrentTrack { get; }

        Vector3 Position { get; }
    }
}