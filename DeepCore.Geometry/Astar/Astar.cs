using DeepCore.Concurrent;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepCore.Astar
{
    public abstract class Astar : Disposable
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(Astar));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public Astar()
        {
            Alloc.RecordConstructor(this.GetType());
        }
        ~Astar()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
        }
    }
    //----------------------------------------------------------------------------------------------------
    /// <summary>
    /// 寻路算法
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="W"></typeparam>
    public abstract class Astar<N, W> : Astar
        where N : IMapNode
        where W : IWayPoint<N, W>
    {
        private IAstarGraph<N> terrain;
        private IOpenList mOpenList;
        private IOpenList mCloseList;
        private int areaCount;

        protected override void Disposing()
        {
            base.Disposing();
            terrain.ForEachNodes(this, static (node, st) =>
            {
                st.GetTempNode(node).Dispose();
            });
            terrain.Dispose();
            terrain = null;
        }

        public int TotalNodeCount { get { return terrain.TotalNodeCount; } }
        public int CloseAreaCount { get { return areaCount; } }
        public abstract W GenWayPoint(N node);
        protected IAstarGraph<N> SceneGraph { get { return terrain; } }
        protected virtual void InitGraph(IAstarGraph<N> map)
        {
            this.terrain = map;
            this.mOpenList = GenOpenList();
            this.mCloseList = GenOpenList();
            this.terrain.ForEachNodes(this, static (node, st) =>
            {
                st.SetTempNode(node, st.mOpenList.GenTempNode(node));
                // node.TempNode = this.mOpenList.GenTempNode(node);
            });
        }

        protected abstract void SetTempNode(IMapNode node, ITempMapNode close);
        protected abstract ITempMapNode GetTempNode(IMapNode node);

        /// <summary>
        /// 计算连续区域，如果一个地图网格有洞的存在，
        /// 即两个区域没有任何连接，则这两个区域的所有节点的CloseAreaIndex值不同，
        /// 返回的列表表示每个CloseArea对应节点数量
        /// </summary>
        protected virtual List<int> InitCloseArea(IAstarGraph<N> graph, IRangeValue progress)
        {
            var list = new List<int>();
            int area_index = 0;
            HashMap<N, bool> dirty_map = new HashMap<N, bool>(graph.TotalNodeCount);
            graph.ForEachNodes((this, dirty_map, area_index), static (mnode, st) =>
            {
                mnode.CloseAreaIndex = (st.area_index);
                st.dirty_map.Add(mnode, true);
            });
            Stack<N> stack = new Stack<N>(dirty_map.Count);
            while (dirty_map.Count > 0)
            {
                area_index++;
                if (list != null) CUtils.SetListSize(list, area_index + 1);
                var exist = dirty_map.GetEnumerator();
                stack.Clear();
                if (exist.MoveNext())
                {
                    var current = exist.Current.Key;
                    dirty_map.Remove(current);
                    progress?.Add(1);
                    stack.Push(current);
                }
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    cur.CloseAreaIndex = (area_index);
                    if (list != null) list[area_index] = list[area_index] + 1;
                    cur.ForEachNext((dirty_map, progress, stack), static (n, st) =>
                    {
                        var next = n as N;
                        if (st.dirty_map.Remove(next))
                        {
                            st.progress?.Add(1);
                            st.stack.Push(next);
                        }
                        return false;
                    });
                    //                     for (int i = cur.Nexts.Length - 1; i >= 0; --i)
                    //                     {
                    //                         var next = cur.Nexts[i] as N;
                    //                         if (dirty_map.Remove(next))
                    //                         {
                    //                             stack.Push(next);
                    //                         }
                    //                     }
                }
            }
            this.areaCount = area_index;
            list.TrimExcess();
            return list;
        }
        protected virtual IOpenList GenOpenList() { return new LinkTempMapNode.TempMapNodeList(); }
        protected virtual W FindPath(N src_node, N dst_node, FindPathParams args)
        {
            return FindPathInternal(GetTempNode(src_node), GetTempNode(dst_node), args);
        }
        protected virtual bool CheckFindPath(ITempMapNode src_node, ITempMapNode dst_node, FindPathParams args) { return true; }
        protected virtual W FindPathInternal(ITempMapNode src_node, ITempMapNode dst_node, FindPathParams args)
        {
            //             if (src_node.MapNode.IsCross == false) return null;
            //             if (dst_node.MapNode.IsCross == false) return null;
            if (src_node.MapNode.CloseAreaIndex != dst_node.MapNode.CloseAreaIndex)
            {
                return null;
            }
            if (args != null && !args.TestCross(src_node.MapNode, dst_node.MapNode))
            {
                return null;
            }
            var head = GenWayPoint(src_node.MapNode as N);
            if (src_node.MapNode.Equals(dst_node.MapNode))
            {
                return head;
            }
            var testCross = args?.TestCross;
            int step = 0;
            this.mOpenList.Clear();
            this.mCloseList.Clear();
            //byte mod_i = 0;
            try
            {
                src_node.SetFather(src_node, dst_node);
                mOpenList.Push(src_node);
                do
                {
                    // search min F
                    var cur_node = mOpenList.Pop();
                    // put the min F to closed
                    mCloseList.Push(cur_node);
                    var nextCount = cur_node.MapNode.NextCount;
                    //mod_i++;
                    // find next node
                    cur_node.MapNode.ForEachNext((this, cur_node, dst_node, testCross), static (next, st) =>
                    {
                        var near = st.Item1.GetTempNode(next);
                        if (!near.MapNode.TestCross(st.cur_node.MapNode))
                        {
                            return false;
                        }
                        if (st.testCross != null && !st.testCross(near.MapNode, st.cur_node.MapNode))
                        {
                            return false;
                        }
                        // ignore what if the block can not across or already in close table
                        if (st.Item1.mCloseList.Contains(near))
                        {
                            return false;
                        }
                        // push and if is not in open table
                        if (!st.Item1.mOpenList.Contains(near))
                        {
                            near.SetFather(st.cur_node, st.dst_node);
                            st.Item1.mOpenList.Push(near);
                        }
                        else //if it is already in the open list, use the G value as a reference to check if the new path is better
                        {
                            //这里的评估条件要具体情况具体分析
                            if (st.cur_node.CheckNearly(near))
                            {
                                near.SetFather(st.cur_node, st.dst_node);
                                st.Item1.mOpenList.ReSort();
                            }
                        }
                        return false;
                    });
#if false
                    for (int i = nextCount - 1; i >= 0; --i)
                      {
                          var near = GetTempNode(cur_node.MapNode.Nexts[(i + mod_i) % nextCount]);
                          if (!near.MapNode.TestCross(cur_node.MapNode))
                          {
                              continue;
                          }
                          if (testCross != null && !testCross(near.MapNode, cur_node.MapNode))
                          {
                              continue;
                          }
                          // ignore what if the block can not across or already in close table
                          if (mCloseList.Contains(near))
                          {
                              continue;
                          }
                          // push and if is not in open table
                          if (!mOpenList.Contains(near))
                          {
                              near.SetFather(cur_node, dst_node);
                              mOpenList.Push(near);
                          }
                          else //if it is already in the open list, use the G value as a reference to check if the new path is better
                          {
                              //这里的评估条件要具体情况具体分析
                              if (cur_node.CheckNearly(near))
                              {
                                  near.SetFather(cur_node, dst_node);
                                  mOpenList.ReSort();
                              }
                          }
                      }
#endif
                    // stop when :
                    // 1. dst node already in close list
                    if (cur_node.MapNode.Equals(dst_node.MapNode))
                    {
                        // 寻到了
                        CurrentLinkHead(head, cur_node, src_node);
                        break;
                    }
                    // 2. open list is empty
                    if (mOpenList.IsEmpty())
                    {
                        // not find the path
                        // 挣扎一下
                        CurrentLinkHead(head, cur_node, src_node);
                        break;
                    }
                    // 3. step limit
                    if (args != null && step > args.StepLimit)
                    {
                        // 有多少返回多少
                        CurrentLinkHead(head, cur_node, src_node);
                        break;
                    }
                    step++;
                } while (true);
            }
            finally
            {
                mOpenList.Cleanup();
                mCloseList.Cleanup();
            }
            return head;
        }

        protected virtual void CurrentLinkHead(W head, ITempMapNode cur_node, ITempMapNode src_node)
        {
            // finded the path
            W end = null;//GenWayPoint(dst_node.data);
            for (int i = terrain.TotalNodeCount - 1; i >= 0; i--)
            {
                // linked to head
                if (cur_node.MapNode.Equals(src_node.MapNode) || (cur_node.Father == cur_node))
                {
                    head.LinkNext(end);
                    break;
                }
                else
                {
                    var next = GenWayPoint(cur_node.MapNode as N);
                    next.LinkNext(end);
                    end = next;
                }
                cur_node = cur_node.Father;
            }
        }

    }
    //-----------------------------------------------------------------------------------------------------------------
    public delegate bool TestCrossPredicate(IMapNode src, IMapNode dst);
    public class FindPathParams
    {
        [Desc("寻路步数限制")]
        public int StepLimit = 10000;
        public TestCrossPredicate TestCross;
    }



    //-----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 寻路快速链表
    /// </summary>
    public interface IOpenList
    {
        ITempMapNode GenTempNode(IMapNode node);
        void Push(ITempMapNode node);
        ITempMapNode Pop();
        bool Contains(ITempMapNode temp);
        void ReSort();
        bool IsEmpty();
        void Clear();
        void Cleanup();
    }

    /// <summary>
    /// 寻路连通图数据
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="W"></typeparam>
    public interface IAstarGraph<N> where N : IMapNode
    {
        int TotalNodeCount { get; }

        void ForEachNodes<ST>(ST st, Action<N, ST> action);

        void Dispose();
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 抽象地图节点
    /// </summary>
    /// <typeparam name="N"></typeparam>
    public abstract class IMapNode
    {
        /// <summary>
        /// 用于区别不同的闭合区间
        /// </summary>
        public abstract int CloseAreaIndex { get; internal protected set; }
        /// <summary>
        /// 用户数据
        /// </summary>
        public abstract object Tag { get; set; }
        /// <summary>
        /// 临近节点
        /// </summary>
        //public abstract IMapNode[] Nexts { get; }
        public abstract int NextCount { get; }

        public abstract bool ForEachNext<ST>(ST st, BreakPredicate<IMapNode, ST> action);

        /// <summary>测试通过</summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public abstract bool TestCross(IMapNode other);



        /// <summary>
        /// g(n) 是在状态空间中从初始节点到n节点的实际代价。值越大，优先级越低。
        /// </summary>
        /// <param name="father"></param>
        /// <returns></returns>
        public abstract float GetFatherG(IMapNode father);

        /// <summary>
        /// h(n) 是从n到目标节点最佳路径的估计代价。值越大，优先级越低。
        /// </summary>
        /// <param name="father"></param>
        /// <returns></returns>
        public abstract float GetTargetH(IMapNode target);

        /// <summary>
        /// 销毁节点
        /// </summary>
        public abstract void Dispose();


    }

    //-----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 抽象寻路临时节点
    /// </summary>
    public abstract class ITempMapNode
    {
        public IMapNode MapNode { get; protected set; }
        public ITempMapNode Father { get; protected set; }
        public float F { get; protected set; }
        public float G { get; protected set; }
        public float H { get; protected set; }
        public ITempMapNode(IMapNode mapNode)
        {
            this.MapNode = mapNode;
        }
        protected internal virtual void SetFather(ITempMapNode father, ITempMapNode target)
        {
            this.Father = father;
            this.G = father.G + MapNode.GetFatherG(father.MapNode);
            this.H = MapNode.GetTargetH(target.MapNode);
            this.F = (G + H);
        }
        protected internal virtual void Dispose()
        {
            this.Father = null;
        }
        protected internal virtual bool CheckNearly(ITempMapNode near)
        {
            return false;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 抽象路点
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="W"></typeparam>
    public abstract class IWayPoint<N, W> : IWayPoint, IEnumerable<W>
        where N : IMapNode
        where W : IWayPoint<N, W>
    {
        public object Tag { get; set; }
        public N Node { get; private set; }
        public W Next { get; private set; }
        public W Prev { get; private set; }
        public W Tail
        {
            get
            {
                var wp = this as W;
                while (wp.Next != null)
                {
                    wp = wp.Next;
                }
                return wp;
            }
        }

        public abstract bool PosEquals(W w);

        public IWayPoint(N map_node)
        {
            this.Node = map_node;
        }
        public virtual void Dispose()
        {
            this.Node = null;
            this.Next = null;
        }
        public void LinkNext(W n)
        {
            this.Next = n;
            if (n != null)
            {
                n.Prev = this as W;
            }
        }
        public void InsertNext(W n)
        {
            var oldnext = this.Next;
            this.LinkNext(n);
            if (oldnext != null)
            {
                n.Tail.LinkNext(oldnext);
            }
        }
        public virtual void Optimize()
        {
            var n = this.Next;
            while (n != null && this.PosEquals(n))
            {
                n = n.Next;
            }
        }
        public ArrayList<W> ToArray()
        {
            var list = new ArrayList<W>();
            foreach (W e in this)
            {
                list.Add(e);
            }
            return list;
        }

        #region IEnumerable
        IWayPoint IWayPoint.Next => this.Next;
        public IEnumerator<W> GetEnumerator()
        {
            return new WayPointIterator<W>(this as W);
        }
        System.Collections.IEnumerator IEnumerable.GetEnumerator()
        {
            return new WayPointIterator<W>(this as W);
        }
        #endregion
    }

    public interface IWayPoint
    {
        IWayPoint Next { get; }
    }
    public struct WayPointIterator<W> : IEnumerator<W> where W : class, IWayPoint
    {
        private W root;
        private W current;
        public W Current { get { return current; } }
        object System.Collections.IEnumerator.Current { get { return current; } }
        public WayPointIterator(W root)
        {
            this.root = root;
            this.current = null;
        }
        public void Dispose()
        {
            this.current = null;
        }
        public bool MoveNext()
        {
            if (current == null)
            {
                this.current = root;
            }
            else
            {
                this.current = current.Next as W;
            }
            return current != null;
        }
        public void Reset()
        {
            this.current = null;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    //-----------------------------------------------------------------------------------------------------------------

    public static class Utils
    {
        public static List<N> GetAllNodes<N>(this IAstarGraph<N> graph) where N : IMapNode
        {
            var ret = new List<N>(graph.TotalNodeCount);
            graph.ForEachNodes(ret, static (e, ret) => ret.Add(e));
            return ret;
        }
    }
}
