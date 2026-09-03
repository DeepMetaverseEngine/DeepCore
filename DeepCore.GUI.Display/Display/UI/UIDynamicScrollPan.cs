using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.UI
{
    public enum UIDynamicScrollPan_Direction
    {
        eAddToTop,
        eAddToBottom,
        eAddToLeft,
        eAddToRight
    }


    /// <summary>
    /// 动态增加条目的滚动条.
    /// Example:
    /// dsp = new UIDynamicScrollPan();
    /// dsp.Bounds = new Rectangle2D(0, 0, 200, 500);
    /// dsp.EnableScrollV = true;
    /// dsp.EnableScrollH = false;
    /// 设置保存的条目.
    /// dsp.CacheRecordNum = 20;
    /// dsp.ScrollRect = dsp.Bounds;
    /// 设置每条的间隔.
    /// dsp.Gap = 10;
    /// </summary>
    public class UIDynamicScrollPan : UIScrollBase
    {
        private float mGap = 0;
        private int mCacheRecordNum = 30;
        private bool mAutoScroll = true;//新来消息的自动跟新滚动条
        private List<DisplayNode> mRecordList = new List<DisplayNode>();
        private UIDynamicScrollPan_Direction mDirection = UIDynamicScrollPan_Direction.eAddToBottom;

        public bool IsAutoScroll
        {
            set
            {
                mAutoScroll = value;
            }
            get
            {
                return mAutoScroll;
            }
        }
        /// <summary>
        /// 设置总共保存多少条内容，大于该数量后，每增加一个节点，会从顶部删除一个节点.
        /// </summary>
        public int CacheRecordNum
        {
            set
            {
                value = Math.Max(0, value);
                mCacheRecordNum = value;
            }
            get
            {
                return mCacheRecordNum;
            }
        }

        /// <summary>
        /// 设置节点间的间隙.
        /// </summary>
        public float Gap
        {
            set
            {
                mGap = value;
            }
            get
            {
                return mGap;
            }
        }

        public override void AddChild(DisplayNode node)
        {
            if(mRecordList.Count < mCacheRecordNum)
            {
                AddNewChild(node);
            }
            else
            {
                RemoveFirstChild();
                AddNewChild(node);
            }
            if(mAutoScroll)
            {
                MoveContainer();
            }

            CalculateMinMaxXY();
        }

        private void AddNewChild(DisplayNode node)
        {
            switch(mDirection)
            {
                case UIDynamicScrollPan_Direction.eAddToBottom:
                    mRecordList.Add(node);
                    if(EnableScrollV)
                    {
                        node.Y = mContentHeight + mGap;
                        mContentHeight += node.Bounds.height + mGap;
                    }
                    break;
                case UIDynamicScrollPan_Direction.eAddToTop:
                    mRecordList.Insert(0, node);
                    if(EnableScrollV)
                    {
                        mContentHeight = 0;
                        int length =  mRecordList.Count;
                        for(int i = 0; i < length; i++)
                        {
                            DisplayNode tnode = mRecordList[i];
                            tnode.Y = mContentHeight + mGap;
                            mContentHeight += tnode.Bounds.height + mGap;
                        }
                    }
                    break;
                case UIDynamicScrollPan_Direction.eAddToRight:
                    mRecordList.Add(node);
                    if(EnableScrollH)
                    {
                        node.X = mContentWidth + mGap;
                        mContentWidth += node.Bounds.width + mGap;
                    }

                    break;
                case UIDynamicScrollPan_Direction.eAddToLeft:
                    mRecordList.Insert(0, node);
                    if(EnableScrollH)
                    {
                        mContentWidth = 0;
                        int length =  mRecordList.Count;
                        for(int i = 0; i < length; i++)
                        {
                            DisplayNode tnode = mRecordList[i];
                            tnode.X = mContentWidth + mGap;
                            mContentWidth += tnode.Bounds.width + mGap;
                        }
                    }
                    break;
            }

            base.AddChild(node);
        }

        private void RemoveFirstChild()
        {
            DisplayNode temp = null;

            switch(mDirection)
            {
                case UIDynamicScrollPan_Direction.eAddToBottom:
                case UIDynamicScrollPan_Direction.eAddToRight:
                    temp = mRecordList[0];
                    mRecordList.RemoveAt(0);
                    break;
                case UIDynamicScrollPan_Direction.eAddToTop:
                case UIDynamicScrollPan_Direction.eAddToLeft:
                    int index =  mRecordList.Count;
                    index = Math.Max(0, index - 1);
                    temp = mRecordList[index];
                    mRecordList.RemoveAt(index);
                    break;
            }

            temp.RemoveFromParent(true);
            temp = null;

            RefeshChildrenPos();
        }

        public bool RemoveScrollPanChild(DisplayNode node, bool doDispose)
        {
            if(node == null || !mRecordList.Contains(node)) { return false; }

            switch(mDirection)
            {
                case UIDynamicScrollPan_Direction.eAddToBottom:
                case UIDynamicScrollPan_Direction.eAddToRight:
                    mRecordList.Remove(node);
                    break;
                case UIDynamicScrollPan_Direction.eAddToTop:
                case UIDynamicScrollPan_Direction.eAddToLeft:
                    mRecordList.Remove(node);

                    break;
            }

            node.RemoveFromParent(doDispose);
            node = null;

            RefeshChildrenPos();
            return true;
        }

        /// <summary>
        /// 刷新节点位置.
        /// </summary>
        public void RefeshChildrenPos()
        {
            if(EnableScrollV)
            {
                mContentHeight = 0;
                int length =  mRecordList.Count;
                for(int i = 0; i < length; i++)
                {
                    DisplayNode node = mRecordList[i];
                    node.Y = mContentHeight + mGap;
                    mContentHeight += node.Bounds.height + mGap;
                }

            }
            if(EnableScrollH)
            {
                mContentWidth = 0;
                int length =  mRecordList.Count;
                for(int i = 0; i < length; i++)
                {
                    DisplayNode node = mRecordList[i];
                    node.X = mContentWidth + mGap;
                    mContentWidth += node.Bounds.width + mGap;
                }
            }
        }

        private void MoveContainer()
        {
            switch(mDirection)
            {
                case UIDynamicScrollPan_Direction.eAddToBottom:

                    if(mContentHeight > this.Bounds.height)
                    {
                        mContainer.Y = this.Bounds.height - mContentHeight;
                    }
                    break;
                case UIDynamicScrollPan_Direction.eAddToRight:
                    if(mContentWidth > this.Bounds.width)
                    {
                        mContainer.X = this.Bounds.width - mContentWidth;
                    }
                    break;
                case UIDynamicScrollPan_Direction.eAddToTop:
                case UIDynamicScrollPan_Direction.eAddToLeft:
                    break;
            }
        }

        /// <summary>
        /// 清除当前所有记录.
        /// </summary>
        public void ClearRecord()
        {
            if(mContainer != null)
            {
                mContainer.RemoveAllChildren(true);
                mContainer.X = 0;
                mContainer.Y = 0;
            }

            if(mRecordList != null)
            {
                mRecordList.Clear();
            }


            mContentHeight = 0;
            mContentWidth = 0;

        }

        public void SetDirection(UIDynamicScrollPan_Direction direction)
        {
            mDirection = direction;
        }

        protected override void Disposing()
        {
            ClearRecord();
            mRecordList = null;
            base.Disposing();
        }

        public List<DisplayNode> GetRecordList()
        {
            return mRecordList;
        }
    }
}

