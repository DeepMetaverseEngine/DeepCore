using System;
using System.Collections;
using System.Collections.Generic;


namespace DeepCore.Astar
{    //-----------------------------------------------------------------------------------------------------------------
    public class FastTempMapNode : ITempMapNode
    {
        private FastOpenList list;

        public FastTempMapNode(IMapNode mapNode) : base(mapNode)
        {
        }
        internal protected override void Dispose()
        {
            base.Dispose();
            this.list = null;
        }

        public class FastOpenList : IOpenList
        {
            private List<FastTempMapNode> mOpenList = new List<FastTempMapNode>();

            public FastOpenList() { }

            public ITempMapNode GenTempNode(IMapNode node)
            {
                return new FastTempMapNode(node);
            }
            public void Push(ITempMapNode tempNode)
            {
                var node = tempNode as FastTempMapNode;
                node.list = this;
                if (mOpenList.Count == 0)
                {
                    mOpenList.Add(node);
                }
                int index = mOpenList.Count;
                int index_d;
                mOpenList.Add(node);
                while (index != 1)
                {
                    index_d = index >> 1;
                    if (mOpenList[index].F < mOpenList[index_d].F)
                    {
                        var temp = mOpenList[index];
                        mOpenList[index] = mOpenList[index_d];
                        mOpenList[index_d] = temp;
                    }
                    else break;
                    index = index_d;
                }
            }
            public ITempMapNode Pop()
            {
                FastTempMapNode temp;
                int index = 2;
                int index_a;
                int index_d;
                int openNum = mOpenList.Count - 1;
                FastTempMapNode first = mOpenList[1];
                first.list = null;
                mOpenList[1] = mOpenList[openNum];
                while (index <= openNum)
                {
                    index_a = index + 1;
                    index_d = index >> 1;
                    if (index_a <= openNum)
                    {
                        if (mOpenList[index].F <= mOpenList[index_a].F && mOpenList[index_d].F > mOpenList[index].F)
                        {
                            temp = mOpenList[index_d];
                            mOpenList[index_d] = mOpenList[index];
                            mOpenList[index] = temp;
                        }
                        if (mOpenList[index_a].F < mOpenList[index].F && mOpenList[index_d].F > mOpenList[index_a].F)
                        {
                            temp = mOpenList[index_d];
                            mOpenList[index_d] = mOpenList[index_a];
                            mOpenList[index_a] = temp;
                            index++;
                        }
                    }
                    else
                    {
                        if (mOpenList[index_d].F > mOpenList[index].F)
                        {
                            temp = mOpenList[index_d];
                            mOpenList[index_d] = mOpenList[index];
                            mOpenList[index] = temp;
                        }
                    }
                    index <<= 1;
                }
                mOpenList.RemoveAt(openNum);
                return first;
            }
            public bool Contains(ITempMapNode tempNode)
            {
                var node = tempNode as FastTempMapNode;
                return node.list == this;
            }
            public void ReSort()
            {

            }
            public bool IsEmpty()
            {
                return mOpenList.Count <= 1;
            }
            public void Clear()
            {
                this.mOpenList.Clear();
            }
            public void Cleanup()
            {
                foreach (var e in this.mOpenList)
                {
                    e.list = null;
                }
            }
        }
    }

}
