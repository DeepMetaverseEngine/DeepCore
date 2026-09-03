using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Astar
{
    //-----------------------------------------------------------------------------------------------------------------

    public class LinkTempMapNode : ITempMapNode
    {
        // 当前所在的列表 //
        internal TempMapNodeList m_curList = null;
        internal LinkTempMapNode m_prev = null;
        internal LinkTempMapNode m_next = null;

        public LinkTempMapNode(IMapNode mapNode) : base(mapNode)
        {
        }
        protected internal override bool CheckNearly(ITempMapNode near)
        {
            if (this.G + near.MapNode.GetFatherG(this.MapNode) < near.G)
            {
                return true;
            }
            return false;
        }
        internal protected override void Dispose()
        {
            base.Dispose();
            m_curList = null;
            m_prev = null;
            m_next = null;
        }

        //--------------------------------------------------------------------------

        public class TempMapNodeList : IOpenList
        {
            private LinkTempMapNode head = null;
            private LinkTempMapNode last = null;

            public TempMapNodeList()
            {
            }
            public ITempMapNode GenTempNode(IMapNode node)
            {
                return new LinkTempMapNode(node);
            }
            public void Push(ITempMapNode tnode)
            {
                var node = tnode as LinkTempMapNode;
                if (node.m_curList == null)
                {
                    if (last == null)
                    {
                        head = last = node;
                        node.m_prev = null;
                        node.m_next = null;
                    }
                    else
                    {
                        last.m_next = node;
                        node.m_prev = last;
                        node.m_next = null;
                        last = node;
                    }
                    node.m_curList = this;
                }
                else
                {
                    throw new Exception("Node is already in a List !");
                }
            }

            public ITempMapNode Pop()
            {
                float min = float.MaxValue;
                LinkTempMapNode ret = null;
                for (LinkTempMapNode a = head; a != null; a = a.m_next)
                {
                    float v = a.F;
                    if (min > v)
                    {
                        ret = a;
                        min = v;
                    }
                }
                if (ret != null)
                {
                    var node = ret;
                    if (node.m_curList == this)
                    {
                        if (head == node)
                        {
                            head = node.m_next;
                        }
                        if (last == node)
                        {
                            last = node.m_prev;
                        }
                        if (node.m_next != null)
                        {
                            node.m_next.m_prev = node.m_prev;
                        }
                        if (node.m_prev != null)
                        {
                            node.m_prev.m_next = node.m_next;
                        }
                        node.m_next = null;
                        node.m_prev = null;
                        node.m_curList = null;
                    }
                    else
                    {
                        throw new Exception("Node is not contains in this list !");
                    }
                }
                return ret;
            }
            public bool IsEmpty()
            {
                return head == null;
            }
            public bool Contains(ITempMapNode tnode)
            {
                var node = tnode as LinkTempMapNode;
                return node.m_curList == this;
            }
            public void ReSort()
            {
            }
            public void Clear()
            {
                if (head != null)
                {
                    for (LinkTempMapNode i = head; i != null; i = i.m_next)
                    {
                        i.m_curList = null;
                    }
                    this.head = null;
                    this.last = null;
                }
            }
            public void Cleanup()
            {
            }
        }

    }

}
