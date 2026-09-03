using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepCore.Space
{
    public abstract class SpaceDivision
    {
        public readonly float SpaceCellW;
        public readonly float SpaceCellH;
        public readonly int SpaceXCount;
        public readonly int SpaceYCount;
        public readonly int SpaceLastX;
        public readonly int SpaceLastY;
        public SpaceDivision(float total_w, float total_h, float cellSizeW, float cellSizeH)
        {
            this.SpaceCellW = cellSizeW;
            this.SpaceCellH = cellSizeH;
            this.SpaceXCount = CMath.RoundMod(total_w, SpaceCellW);
            this.SpaceYCount = CMath.RoundMod(total_h, SpaceCellH);
            this.SpaceLastX = SpaceXCount - 1;
            this.SpaceLastY = SpaceYCount - 1;
        }
    }

    /// <summary>
    /// 遍历单位
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="cancel">设置为True，立即停止遍历</param>
    //public delegate void ObjectForEachAction<T>(T obj, ref bool cancel);


    /// <summary>
    /// 管理空间分割类，十字链表空间管理。
    /// </summary>
    public class SpaceDivision<TAG> : SpaceDivision, IDisposable
    {
        protected static readonly int[][] NEAR_TABLE = new int[][] {
                    new int[]{-1,-1},
                    new int[]{ 0,-1},
                    new int[]{ 1,-1},
                    new int[]{-1, 0},
                    //
                    new int[]{ 1, 0},
                    new int[]{-1, 1},
                    new int[]{ 0, 1},
                    new int[]{ 1, 1}
                };
        private SpaceCellNode[,] SpaceMatrix;
        private bool m_PosDirty = true;
        private readonly List<SpaceCellNode> _dirtyCellList = new List<SpaceCellNode>(64);
        public SpaceDivision(float total_w, float total_h, float cellSizeW, float cellSizeH)
            : base(total_w, total_h, cellSizeW, cellSizeH)
        {
            this.SpaceMatrix = new SpaceCellNode[SpaceXCount, SpaceYCount];
        }
        public virtual void Init()
        {
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    this.SpaceMatrix[ix, iy] = CreateSpaceCellNode(ix, iy);
                }
            }
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    var node = SpaceMatrix[ix, iy];
                    var nears = new List<SpaceCellNode>();
                    for (int ni = 0; ni < NEAR_TABLE.Length; ni++)
                    {
                        var near = GetSpaceCell(
                            ix + NEAR_TABLE[ni][0],
                            iy + NEAR_TABLE[ni][1]);
                        if (near != null && near != node)
                        {
                            nears.Add(near);
                        }
                    }
                    node.nears = nears.ToArray();
                }
            }
        }
        public virtual void Dispose()
        {
            this.event_OnObjectSwapped = null;
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    SpaceCellNode node = SpaceMatrix[ix, iy];
                    node.Dispose();
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        #region Clamp

        public void ClampSpace(int x, int y, int r, out int cx1, out int cy1, out int cx2, out int cy2)
        {
            cx1 = (x - r);
            cy1 = (y - r);
            cx2 = (x + r);
            cy2 = (y + r);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, this.SpaceLastX);
            cy2 = Math.Min(cy2, this.SpaceLastY);
        }
        public void ClampSpace(ref int x1, ref int y1, ref int x2, ref int y2)
        {
            if (x2 < x1) CUtils.Swap<int>(ref x2, ref x1);
            if (y2 < y1) CUtils.Swap<int>(ref y2, ref y1);
            x1 = Math.Max(x1, 0);
            y1 = Math.Max(y1, 0);
            x2 = Math.Min(x2, this.SpaceLastX);
            y2 = Math.Min(y2, this.SpaceLastY);
        }

        /// <summary>
        /// 实际坐标转换为分割块坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        public void ClampPosition(float x, float y, out int cx, out int cy)
        {
            cx = (int)(x / SpaceCellW);
            cy = (int)(y / SpaceCellH);
        }
        public void ClampPosition(float x, float y, float r, out int cx1, out int cy1, out int cx2, out int cy2)
        {
            cx1 = (int)((x - r) / this.SpaceCellW);
            cy1 = (int)((y - r) / this.SpaceCellH);
            cx2 = (int)((x + r) / this.SpaceCellW);
            cy2 = (int)((y + r) / this.SpaceCellH);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, this.SpaceLastX);
            cy2 = Math.Min(cy2, this.SpaceLastY);
        }
        public void ClampPosition(float x1, float y1, float x2, float y2, out int cx1, out int cy1, out int cx2, out int cy2)
        {
            if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            cx1 = (int)(x1 / this.SpaceCellW);
            cy1 = (int)(y1 / this.SpaceCellH);
            cx2 = (int)(x2 / this.SpaceCellW);
            cy2 = (int)(y2 / this.SpaceCellH);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, this.SpaceLastX);
            cy2 = Math.Min(cy2, this.SpaceLastY);
        }
        public void ClampNearPosition(float x, float y, float r, out int cx1, out int cy1, out int cx2, out int cy2)
        {
            cx1 = (int)((x - r) / this.SpaceCellW) - 1;
            cy1 = (int)((y - r) / this.SpaceCellH) - 1;
            cx2 = (int)((x + r) / this.SpaceCellW) + 1;
            cy2 = (int)((y + r) / this.SpaceCellH) + 1;
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, this.SpaceLastX);
            cy2 = Math.Min(cy2, this.SpaceLastY);
        }
        public void ClampNearPosition(float x1, float y1, float x2, float y2, out int cx1, out int cy1, out int cx2, out int cy2)
        {
            if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            cx1 = (int)(x1 / this.SpaceCellW) - 1;
            cy1 = (int)(y1 / this.SpaceCellH) - 1;
            cx2 = (int)(x2 / this.SpaceCellW) + 1;
            cy2 = (int)(y2 / this.SpaceCellH) + 1;
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, this.SpaceLastX);
            cy2 = Math.Min(cy2, this.SpaceLastY);
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region _SpaceCellNode_

        protected virtual SpaceCellNode CreateSpaceCellNode(int cx, int cy)
        {
            return new SpaceCellNode(cx, cy);
        }

        public SpaceCellNode GetSpaceCell(int cx, int cy)
        {
            if (cx < SpaceXCount && cx >= 0 && cy < SpaceYCount && cy >= 0)
            {
                return SpaceMatrix[cx, cy];
            }
            return null;
        }
        /// <summary>
        /// 按格取分割块
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <returns></returns>
        public SpaceCellNode GetSpaceCellNodeByBlock(int cx, int cy)
        {
            if (cx < SpaceXCount && cx >= 0 && cy < SpaceYCount && cy >= 0)
            {
                return SpaceMatrix[cx, cy];
            }
            return null;
        }

        /// <summary>
        /// 按坐标取分割块
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public SpaceCellNode GetPositionCellNode(float x, float y)
        {
            int cx = (int)(x / SpaceCellW);
            int cy = (int)(y / SpaceCellH);
            if (cx < SpaceXCount && cx >= 0 && cy < SpaceYCount && cy >= 0)
            {
                return SpaceMatrix[cx, cy];
            }
            return null;
        }

        public void ListSpaceCellNodes(IList<SpaceCellNode> list)
        {
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    list.Add(SpaceMatrix[ix, iy]);
                }
            }
        }


        public bool ForEachSpaceCellNodes<ST>(in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    state.Iterator = SpaceMatrix[ix, iy]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }
        public bool ForEachSpaceCellNodes<ST>(int x, int y, int r, in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            ClampSpace(x, y, r, out var x1, out var y1, out var x2, out var y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    state.Iterator = SpaceMatrix[ix, iy]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }
        public bool ForEachSpaceCellNodes<ST>(int x1, int y1, int x2, int y2, in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            ClampSpace(ref x1, ref y1, ref x2, ref y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    state.Iterator = SpaceMatrix[ix, iy]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }


        public bool ForEachNearPositionCellNodes<ST>(float x, float y, in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            var node = GetPositionCellNode(x, y);
            if (node != null)
            {
                state.Iterator = node; action(state); if (state.Break) return true;
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    state.Iterator = node.nears[i]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }
        public bool ForEachNearPositionCellNodes<ST>(float x, float y, float r, in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            ClampNearPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    state.Iterator = SpaceMatrix[cx, cy]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }
        public bool ForEachNearPositionCellNodesRect<ST>(float x1, float y1, float x2, float y2, in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
        {
            ClampNearPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    state.Iterator = SpaceMatrix[cx, cy]; action(state); if (state.Break) return true;
                }
            }
            return false;
        }


        public void ForEachSpaceCellNodes<ST>(in ST state, ForEachAction<ST, SpaceCellNode> action)
        {
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    action(state, SpaceMatrix[ix, iy]);
                }
            }
        }
        public void ForEachSpaceCellNodes<ST>(int x, int y, int r, in ST state, ForEachAction<ST, SpaceCellNode> action)
        {
            ClampSpace(x, y, r, out var x1, out var y1, out var x2, out var y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    action(state, SpaceMatrix[ix, iy]);
                }
            }
        }
        public void ForEachSpaceCellNodes<ST>(int x1, int y1, int x2, int y2, in ST state, ForEachAction<ST, SpaceCellNode> action)
        {
            ClampSpace(ref x1, ref y1, ref x2, ref y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    action(state, SpaceMatrix[ix, iy]);
                }
            }
        }


        public bool TryGetSpaceCellNodes<ST>(in ST state, TryGetPredicate<ST, SpaceCellNode> action, out SpaceCellNode result)
        {
            for (int ix = 0; ix < SpaceXCount; ++ix)
            {
                for (int iy = 0; iy < SpaceYCount; ++iy)
                {
                    if (action(state, SpaceMatrix[ix, iy]))
                    {
                        result = SpaceMatrix[ix, iy];
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        public bool TryGetSpaceCellNodes<ST>(int x, int y, int r, in ST state, TryGetPredicate<ST, SpaceCellNode> action, out SpaceCellNode result)
        {
            ClampSpace(x, y, r, out var x1, out var y1, out var x2, out var y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    if (action(state, SpaceMatrix[ix, iy]))
                    {
                        result = SpaceMatrix[ix, iy];
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        public bool TryGetSpaceCellNodes<ST>(int x1, int y1, int x2, int y2, in ST state, TryGetPredicate<ST, SpaceCellNode> action, out SpaceCellNode result)
        {
            ClampSpace(ref x1, ref y1, ref x2, ref y2);
            for (int ix = x1; ix <= x2; ++ix)
            {
                for (int iy = y1; iy <= y2; ++iy)
                {
                    if (action(state, SpaceMatrix[ix, iy]))
                    {
                        result = SpaceMatrix[ix, iy];
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }


        public bool TryGetNearPositionCellNodes<ST, R>(float x, float y, in ST state, TryGetPredicateResult<ST, SpaceCellNode, R> action, out R result)
        {
            var node = GetPositionCellNode(x, y);
            if (node != null)
            {
                if (action(state, node, out result)) { return true; }
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    if (action(state, node.nears[i], out result)) { return true; }
                }
            }
            result = default;
            return false;
        }
        public bool TryGetNearPositionCellNodes<ST, R>(float x, float y, float r, in ST state, TryGetPredicateResult<ST, SpaceCellNode, R> action, out R result)
        {
            ClampNearPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    if (action(state, SpaceMatrix[cx, cy], out result)) { return true; }
                }
            }
            result = default;
            return false;
        }
        public bool TryGetNearPositionCellNodesRect<ST, R>(float x1, float y1, float x2, float y2, in ST state, TryGetPredicateResult<ST, SpaceCellNode, R> action, out R result)
        {
            ClampNearPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    if (action(state, SpaceMatrix[cx, cy], out result)) { return true; }
                }
            }
            result = default;
            return false;
        }



        //------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 空间分割节点，十字链表节点
        /// </summary>
        public class SpaceCellNode
        {
            internal readonly int six;
            internal readonly int siy;
            private SpaceUserTag mHead;
            private SpaceUserTag mTail;
            internal SpaceCellNode[] nears;

            public SpaceCellNode(int six, int siy)
            {
                this.six = six;
                this.siy = siy;
            }
            internal void Dispose()
            {
                var list = new List<SpaceUserTag>();
                {
                    this.AsChildNodeList(list);
                    foreach (var o in list)
                    {
                        this.InternalRemove(o);
                    }
                }
            }
            public bool ForEachNears<ST>(in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceCellNode>
            {
                for (int i = this.nears.Length - 1; i >= 0; --i)
                {
                    state.Iterator = this.nears[i];
                    action(state);
                    if (state.Break) return true;
                }
                return false;
            }
            public void ForEachNears<ST>(in ST state, ForEachAction<ST, SpaceCellNode> action)
            {
                for (int i = this.nears.Length - 1; i >= 0; --i)
                {
                    action(state, this.nears[i]);
                }
            }
            public void ForEachNears<ST, SP>(in ST state, ForEachAction<ST, SP> action) where SP : SpaceCellNode
            {
                for (int i = this.nears.Length - 1; i >= 0; --i)
                {
                    action(state, this.nears[i] as SP);
                }
            }
            public bool TryGetNears<ST>(in ST state, TryGetPredicate<ST, SpaceCellNode> action, out SpaceCellNode result)
            {
                for (int i = this.nears.Length - 1; i >= 0; --i)
                {
                    var near = this.nears[i];
                    if (action(state, near))
                    {
                        result = near;
                        return true;
                    }
                }
                result = null;
                return false;
            }
            public bool TryGetNears<ST, SP>(in ST state, TryGetPredicate<ST, SP> action, out SP result) where SP : SpaceCellNode
            {
                for (int i = this.nears.Length - 1; i >= 0; --i)
                {
                    var near = this.nears[i] as SP;
                    if (action(state, near))
                    {
                        result = near;
                        return true;
                    }
                }
                result = null;
                return false;
            }


            internal void MarkPosDirty()
            {
                IsPosDirty = true;
            }
            internal void ClearPosDirty()
            {
                IsPosDirty = false;
            }

            internal void InternalAdd(SpaceUserTag tag)
            {
                if (tag.SpaceCell != null)
                {
                    throw new Exception("SpaceUserTag cell already exist");
                }
                if (tag.Next != null || tag.Prev != null)
                {
                    throw new Exception("obj.mCurCellNode.Next != null || obj.mCurCellNode.Prev != null");
                }
                if (Count == 0)
                {
                    mHead = mTail = tag;
                }
                else
                {
                    mTail.AddNext(tag);
                    mTail = tag;
                }
                tag.InternalAdd(this);
                Count++;
                //                 if (mOnObjectAdded != null)
                //                 {
                //                     mOnObjectAdded.Invoke(this, cell.obj);
                //                 }
            }

            internal void InternalRemove(SpaceUserTag tag)
            {
                if (tag.SpaceCell != this)
                {
                    throw new Exception("SpaceUserTag cell not exist");
                }
                Count--;
                if (Count == 0)
                {
                    mHead = mTail = null;
                }
                else if (mHead == tag)
                {
                    mHead = mHead.Next;
                }
                else if (mTail == tag)
                {
                    mTail = mTail.Prev;
                }
                tag.InternalRemove();
                //                 if (mOnObjectRemoved != null)
                //                 {
                //                     mOnObjectRemoved.Invoke(this, cell.obj);
                //                 }
            }

            public int BX { get { return six; } }
            public int BY { get { return siy; } }
            public bool IsPosDirty { get; private set; }
            public int Count { get; private set; }

            [Obsolete("Debug Only", true)]
            public SpaceUserTag[] ResultView
            {
                get
                {
                    var ret = new List<SpaceUserTag>();
                    if (mHead != null)
                    {
                        mHead.ForEach(ret, (List<SpaceUserTag> st, SpaceUserTag c) =>
                        {
                            st.Add(c);
                            return false;
                        });
                    }
                    return ret.ToArray();
                }
            }
            public void AsChildList<T>(IList<T> ret)
            {
                this.ForEachChild(ret, static (IList<T> st, T o) =>
                {
                    st.Add(o);
                    return false;
                });
            }
            public void AsChildNodeList(IList<SpaceUserTag> ret)
            {
                if (mHead != null)
                {
                    mHead.ForEachTAG(ret, static (IList<SpaceUserTag> st, SpaceUserTag o) =>
                    {
                        st.Add(o);
                        return false;
                    });
                }
            }

            public bool ForEachChild<ST, T>(in ST state, ForEachAction<ST> action, T t = default) where ST : ForEachInput<T>
            {
                if (mHead != null)
                {
                    return mHead.ForEachTAG<ST, T>(in state, action);
                }
                return false;
            }
            public bool ForEachChild<ST, T>(in ST state, ForEachPredicate<ST, T> action, T t = default)
            {
                if (mHead != null)
                {
                    return mHead.ForEachTAG<ST, T>(in state, action);
                }
                return false;
            }
            public bool TryGetChild<ST, T>(in ST state, TryGetPredicate<ST, T> action, out T result, T t = default)
            {
                if (mHead != null)
                {
                    return mHead.TryGetTAG<ST, T>(in state, action, out result);
                }
                result = default(T);
                return false;
            }

            //             private ObjectAddedHandler mOnObjectAdded;
            //             private ObjectRemovedHandler mOnObjectRemoved;
            //             public event ObjectAddedHandler OnObjectAdded { add { mOnObjectAdded += value; } remove { mOnObjectAdded -= value; } }
            //             public event ObjectRemovedHandler OnObjectRemoved { add { mOnObjectRemoved += value; } remove { mOnObjectRemoved -= value; } }

        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region _UserCellNode_

        //----------------------------------------------------------------------------------------------------------------------------
        public virtual SpaceUserTag CreateUserTag(TAG obj)
        {
            return new SpaceUserTag(this, obj);
        }
        private void MarkPosDirty()
        {
            m_PosDirty = true;
        }

        private void MarkCellDirty(SpaceCellNode cell)
        {
            if (!cell.IsPosDirty)
            {
                cell.MarkPosDirty();
                _dirtyCellList.Add(cell);
            }
            m_PosDirty = true;
        }

        // 更新场景内的分割区域变化信息
        public void ClearPosDirty()
        {
            if (!m_PosDirty) return;
            m_PosDirty = false;
            for (int i = 0; i < _dirtyCellList.Count; i++)
                _dirtyCellList[i].ClearPosDirty();
            _dirtyCellList.Clear();
        }

        /// <summary>
        /// 刷新空间分割位置为有改变
        /// </summary>
        /// <param name="old_cell"></param>
        public void MarkPosDirty(SpaceUserTag old_cell)
        {
            if (old_cell.SpaceCell != null)
            {
                MarkCellDirty(old_cell.SpaceCell);
            }
            else
            {
                m_PosDirty = true;
            }
        }

        /// <summary>
        /// 清除空间位置
        /// </summary>
        /// <param name="old_cell"></param>
        private void ClearSpace(SpaceUserTag old_cell)
        {
            var space = old_cell.SpaceCell;
            if (space != null)
            {
                space.InternalRemove(old_cell);
                MarkCellDirty(space);
                event_OnObjectSwapped?.Invoke(old_cell, null, space);
            }
            else
            {
                m_PosDirty = true;
            }
        }

        /// <summary>
        /// 切换单位空间位置
        /// </summary>
        private SpaceCellNode SwapSpace(SpaceUserTag obj, float x, float y, bool posDirty)
        {
            SpaceCellNode old_cell = obj.SpaceCell;
            SpaceCellNode new_cell = GetPositionCellNode(x, y);
            if (old_cell != new_cell)
            {
                if (old_cell != null)
                {
                    old_cell.InternalRemove(obj);
                    if (posDirty) MarkCellDirty(old_cell);
                }
                if (new_cell != null)
                {
                    new_cell.InternalAdd(obj);
                    if (posDirty) MarkCellDirty(new_cell);
                }
                event_OnObjectSwapped?.Invoke(obj, new_cell, old_cell);
                return new_cell;
            }
            else if (posDirty)
            {
                m_PosDirty = true;
            }
            return null;
        }

        public bool IsNearPosDirty()
        {
            return m_PosDirty;
        }
        /// <summary>
        /// 判断是否附近有位置变化
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsNearPosDirty(float x, float y)
        {
            if (this.m_PosDirty)
            {
                SpaceCellNode node = this.GetPositionCellNode(x, y);
                if (node != null)
                {
                    if (node.IsPosDirty)
                    {
                        return true;
                    }
                    for (int i = node.nears.Length - 1; i >= 0; --i)
                    {
                        if (node.nears[i].IsPosDirty)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool IsNearPosDirty(float x, float y, float r)
        {
            if (this.m_PosDirty)
            {
                if (r < this.SpaceCellW && r < this.SpaceCellH)
                {
                    SpaceCellNode node = this.GetPositionCellNode(x, y);
                    if (node != null)
                    {
                        if (node.IsPosDirty) return true;
                        for (int i = node.nears.Length - 1; i >= 0; --i)
                        {
                            if (node.nears[i].IsPosDirty) return true;
                        }
                    }
                }
                else
                {
                    //                     int cx1 = (int)((x - r) / this.SpaceCellW);
                    //                     int cy1 = (int)((y - r) / this.SpaceCellH);
                    //                     int cx2 = (int)((x + r) / this.SpaceCellW);
                    //                     int cy2 = (int)((y + r) / this.SpaceCellH);
                    //                     cx1 = Math.Max(cx1, 0);
                    //                     cy1 = Math.Max(cy1, 0);
                    //                     cx2 = Math.Min(cx2, this.SpaceLastX);
                    //                     cy2 = Math.Min(cy2, this.SpaceLastY);
                    ClampPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
                    for (int cx = cx1; cx <= cx2; ++cx)
                    {
                        for (int cy = cy1; cy <= cy2; ++cy)
                        {
                            SpaceCellNode cn = this.SpaceMatrix[cx, cy];
                            if (cn.IsPosDirty) return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool IsNearPosDirty(float x1, float y1, float x2, float y2)
        {
            if (this.m_PosDirty)
            {
                //                 if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
                //                 if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
                //                 int cx1 = (int)(x1 / this.SpaceCellW);
                //                 int cy1 = (int)(y1 / this.SpaceCellH);
                //                 int cx2 = (int)(x2 / this.SpaceCellW);
                //                 int cy2 = (int)(y2 / this.SpaceCellH);
                //                 cx1 = Math.Max(cx1, 0);
                //                 cy1 = Math.Max(cy1, 0);
                //                 cx2 = Math.Min(cx2, this.SpaceLastX);
                //                 cy2 = Math.Min(cy2, this.SpaceLastY);
                ClampPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
                for (int cx = cx1; cx <= cx2; ++cx)
                {
                    for (int cy = cy1; cy <= cy2; ++cy)
                    {
                        SpaceCellNode cn = this.SpaceMatrix[cx, cy];
                        if (cn.IsPosDirty) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取当前坐标附近的所有单位容量
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetNearObjectsCapacity(float x, float y)
        {
            int ret = 0;
            var node = this.GetPositionCellNode(x, y);
            if (node != null)
            {
                ret += node.Count;
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    ret += node.nears[i].Count;
                }
            }
            return Math.Max(ret, 10);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearObjects<ST, T>(float x, float y, in ST state, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T>
        {
            var node = GetPositionCellNode(x, y);
            if (node != null)
            {
                if (node.ForEachChild<ST, T>(in state, indexer))
                {
                    return true;
                }
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    if (node.nears[i].ForEachChild<ST, T>(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool ForEachNearObjects<ST, T>(float x, float y, float r, in ST state, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T>
        {
            if (r < SpaceCellW && r < SpaceCellH)
            {
                var node = GetPositionCellNode(x, y);
                if (node != null)
                {
                    if (node.ForEachChild<ST, T>(in state, indexer))
                    {
                        return true;
                    }
                    for (int i = node.nears.Length - 1; i >= 0; --i)
                    {
                        if (node.nears[i].ForEachChild<ST, T>(in state, indexer))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                ClampNearPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
                for (int cx = cx1; cx <= cx2; ++cx)
                {
                    for (int cy = cy1; cy <= cy2; ++cy)
                    {
                        var cn = this.SpaceMatrix[cx, cy];
                        if (cn.ForEachChild<ST, T>(in state, indexer))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ForEachNearObjectsRect<ST, T>(float x1, float y1, float x2, float y2, in ST state, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T>
        {
            bool cancel = false;
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)(x1 / this.SpaceCellW) - 1;
            //             int cy1 = (int)(y1 / this.SpaceCellH) - 1;
            //             int cx2 = (int)(x2 / this.SpaceCellW) + 1;
            //             int cy2 = (int)(y2 / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.ForEachChild<ST, T>(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return cancel;
        }
        public bool ForEachNearObjectsRectWide<ST, T>(float x1, float y1, float x2, float y2, float wide, in ST state, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T>
        {
            bool cancel = false;
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)((x1 - wide) / this.SpaceCellW) - 1;
            //             int cy1 = (int)((y1 - wide) / this.SpaceCellH) - 1;
            //             int cx2 = (int)((x2 + wide) / this.SpaceCellW) + 1;
            //             int cy2 = (int)((y2 + wide) / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1 - wide, y1 - wide, x2 + wide, y2 + wide, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.ForEachChild<ST, T>(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return cancel;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, in ST state, ForEachPredicate<ST, T> indexer, T t = default)
        {
            var node = GetPositionCellNode(x, y);
            if (node != null)
            {
                if (node.ForEachChild(in state, indexer))
                {
                    return true;
                }
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    if (node.nears[i].ForEachChild(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, float r, in ST state, ForEachPredicate<ST, T> indexer, T t = default)
        {
            if (r < SpaceCellW && r < SpaceCellH)
            {
                var node = GetPositionCellNode(x, y);
                if (node != null)
                {
                    if (node.ForEachChild(in state, indexer))
                    {
                        return true;
                    }
                    for (int i = node.nears.Length - 1; i >= 0; --i)
                    {
                        if (node.nears[i].ForEachChild(in state, indexer))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                //                 int cx1 = (int)((x - r) / this.SpaceCellW) - 1;
                //                 int cy1 = (int)((y - r) / this.SpaceCellH) - 1;
                //                 int cx2 = (int)((x + r) / this.SpaceCellW) + 1;
                //                 int cy2 = (int)((y + r) / this.SpaceCellH) + 1;
                //                 cx1 = Math.Max(cx1, 0);
                //                 cy1 = Math.Max(cy1, 0);
                //                 cx2 = Math.Min(cx2, this.SpaceLastX);
                //                 cy2 = Math.Min(cy2, this.SpaceLastY);
                ClampNearPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
                for (int cx = cx1; cx <= cx2; ++cx)
                {
                    for (int cy = cy1; cy <= cy2; ++cy)
                    {
                        var cn = this.SpaceMatrix[cx, cy];
                        if (cn.ForEachChild(in state, indexer))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ForEachNearObjectsRectPredicate<ST, T>(float x1, float y1, float x2, float y2, in ST state, ForEachPredicate<ST, T> indexer, T t = default)
        {
            bool cancel = false;
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)(x1 / this.SpaceCellW) - 1;
            //             int cy1 = (int)(y1 / this.SpaceCellH) - 1;
            //             int cx2 = (int)(x2 / this.SpaceCellW) + 1;
            //             int cy2 = (int)(y2 / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.ForEachChild(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return cancel;
        }
        public bool ForEachNearObjectsRectWidePredicate<ST, T>(float x1, float y1, float x2, float y2, float wide, in ST state, ForEachPredicate<ST, T> indexer, T t = default)
        {
            bool cancel = false;
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)((x1 - wide) / this.SpaceCellW) - 1;
            //             int cy1 = (int)((y1 - wide) / this.SpaceCellH) - 1;
            //             int cx2 = (int)((x2 + wide) / this.SpaceCellW) + 1;
            //             int cy2 = (int)((y2 + wide) / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1 - wide, y1 - wide, x2 + wide, y2 + wide, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.ForEachChild(in state, indexer))
                    {
                        return true;
                    }
                }
            }
            return cancel;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool TryGetNearObjects<ST, T>(float x, float y, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default)
        {
            var node = GetPositionCellNode(x, y);
            if (node != null)
            {
                if (node.TryGetChild(in state, indexer, out result))
                {
                    return true;
                }
                for (int i = node.nears.Length - 1; i >= 0; --i)
                {
                    if (node.nears[i].TryGetChild(in state, indexer, out result))
                    {
                        return true;
                    }
                }
            }
            result = default(T);
            return false;
        }
        public bool TryGetNearObjects<ST, T>(float x, float y, float r, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default)
        {
            if (r < SpaceCellW && r < SpaceCellH)
            {
                var node = GetPositionCellNode(x, y);
                if (node != null)
                {
                    if (node.TryGetChild(in state, indexer, out result))
                    {
                        return true;
                    }
                    for (int i = node.nears.Length - 1; i >= 0; --i)
                    {
                        if (node.nears[i].TryGetChild(in state, indexer, out result))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                //                 int cx1 = (int)((x - r) / this.SpaceCellW) - 1;
                //                 int cy1 = (int)((y - r) / this.SpaceCellH) - 1;
                //                 int cx2 = (int)((x + r) / this.SpaceCellW) + 1;
                //                 int cy2 = (int)((y + r) / this.SpaceCellH) + 1;
                //                 cx1 = Math.Max(cx1, 0);
                //                 cy1 = Math.Max(cy1, 0);
                //                 cx2 = Math.Min(cx2, this.SpaceLastX);
                //                 cy2 = Math.Min(cy2, this.SpaceLastY);
                ClampNearPosition(x, y, r, out var cx1, out var cy1, out var cx2, out var cy2);
                for (int cx = cx1; cx <= cx2; ++cx)
                {
                    for (int cy = cy1; cy <= cy2; ++cy)
                    {
                        var cn = this.SpaceMatrix[cx, cy];
                        if (cn.TryGetChild(in state, indexer, out result))
                        {
                            return true;
                        }
                    }
                }
            }
            result = default(T);
            return false;
        }
        public bool TryGetNearObjectsRect<ST, T>(float x1, float y1, float x2, float y2, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default)
        {
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)(x1 / this.SpaceCellW) - 1;
            //             int cy1 = (int)(y1 / this.SpaceCellH) - 1;
            //             int cx2 = (int)(x2 / this.SpaceCellW) + 1;
            //             int cy2 = (int)(y2 / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1, y1, x2, y2, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.TryGetChild(in state, indexer, out result))
                    {
                        return true;
                    }
                }
            }
            result = default(T);
            return false;
        }
        public bool TryGetNearObjectsRectWide<ST, T>(float x1, float y1, float x2, float y2, float wide, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default)
        {
            //             if (x2 < x1) CUtils.Swap<float>(ref x2, ref x1);
            //             if (y2 < y1) CUtils.Swap<float>(ref y2, ref y1);
            //             int cx1 = (int)((x1 - wide) / this.SpaceCellW) - 1;
            //             int cy1 = (int)((y1 - wide) / this.SpaceCellH) - 1;
            //             int cx2 = (int)((x2 + wide) / this.SpaceCellW) + 1;
            //             int cy2 = (int)((y2 + wide) / this.SpaceCellH) + 1;
            //             cx1 = Math.Max(cx1, 0);
            //             cy1 = Math.Max(cy1, 0);
            //             cx2 = Math.Min(cx2, this.SpaceLastX);
            //             cy2 = Math.Min(cy2, this.SpaceLastY);
            ClampNearPosition(x1 - wide, y1 - wide, x2 + wide, y2 + wide, out var cx1, out var cy1, out var cx2, out var cy2);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    var cn = this.SpaceMatrix[cx, cy];
                    if (cn.TryGetChild(in state, indexer, out result))
                    {
                        return true;
                    }
                }
            }
            result = default(T);
            return false;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 单位链表结构节点
        /// </summary>
        public class SpaceUserTag : Disposable
        {
            public TAG UserTag { get { return obj; } }
            public SpaceCellNode SpaceCell { get; private set; }
            private readonly SpaceDivision<TAG> div;
            internal readonly TAG obj;
            internal SpaceUserTag next;
            internal SpaceUserTag prev;
            internal bool mPosDirty;

            protected internal SpaceUserTag(SpaceDivision<TAG> div, TAG obj)
            {
                this.div = div;
                this.obj = obj;
            }
            protected override void Disposing()
            {
                div.ClearSpace(this);
            }
            public virtual void MarkPosDirty()
            {
                mPosDirty = true;
            }
            public SpaceCellNode SwapSpace(float x, float y, bool posDirty)
            {
                return div.SwapSpace(this, x, y, posDirty);
            }

            public SpaceUserTag Next { get { return next; } }
            public SpaceUserTag Prev { get { return prev; } }
            /// <summary>
            /// 当前空间布局已改变?
            /// </summary>
            public bool IsPosDirty { get { return mPosDirty; } }

            internal void InternalAdd(SpaceCellNode space)
            {
                this.SpaceCell = space;
            }
            internal void InternalRemove()
            {
                if (next != null)
                {
                    next.prev = this.prev;
                }
                if (prev != null)
                {
                    prev.next = this.next;
                }
                this.next = null;
                this.prev = null;
                this.SpaceCell = null;
            }

            internal void AddNext(SpaceUserTag next)
            {
                this.next = next;
                next.prev = this;
            }

            public bool ForEachTAG<ST, T>(in ST state, ForEachAction<ST> action, T tt = default) where ST : ForEachInput<T>
            {
                SpaceUserTag current = this;
                do
                {
                    if (current.obj is T t)
                    {
                        state.Iterator = t; action(state); if (state.Break) { return true; }
                    }
                    current = current.next;
                }
                while (current != null);
                return false;
            }
            public bool ForEach<ST>(in ST state, ForEachAction<ST> action) where ST : ForEachInput<SpaceUserTag>
            {
                SpaceUserTag current = this;
                do
                {
                    state.Iterator = current; action(state); if (state.Break) { return true; }
                    current = current.next;
                }
                while (current != null);
                return false;
            }
            public bool ForEachTAG<ST, T>(in ST state, ForEachPredicate<ST, T> action, T tt = default)
            {
                SpaceUserTag current = this;
                do
                {
                    if (current.obj is T t)
                    {
                        if (action(state, t))
                        {
                            return true;
                        }
                    }
                    current = current.next;
                }
                while (current != null);
                return false;
            }
            public bool ForEach<ST>(in ST state, ForEachPredicate<ST, SpaceUserTag> action)
            {
                SpaceUserTag current = this;
                do
                {
                    if (action(state, current))
                    {
                        return true;
                    }
                    current = current.next;
                }
                while (current != null);
                return false;
            }
            public bool TryGetTAG<ST, T>(in ST state, TryGetPredicate<ST, T> action, out T result, T tt = default)
            {
                SpaceUserTag current = this;
                do
                {
                    if (current.obj is T t)
                    {
                        if (action(state, t))
                        {
                            result = t;
                            return true;
                        }
                    }
                    current = current.next;
                }
                while (current != null);
                result = default;
                return false;
            }
            public bool TryGet<ST>(in ST state, TryGetPredicate<ST, SpaceUserTag> action, out SpaceUserTag result)
            {
                SpaceUserTag current = this;
                do
                {
                    if (action(state, current))
                    {
                        result = current;
                        return true;
                    }
                    current = current.next;
                }
                while (current != null);
                result = default;
                return false;
            }


            internal struct Iterator : IEnumerator
            {
                public readonly SpaceUserTag mBegin;
                private bool mStart;
                private SpaceUserTag index;

                public Iterator(SpaceUserTag begin)
                {
                    this.mBegin = begin;
                    this.mStart = false;
                    this.index = null;
                }
                public object Current
                {
                    get
                    {
                        if (index != null) return index.obj;
                        return null;
                    }
                }
                public bool MoveNext()
                {
                    if (!mStart)
                    {
                        index = mBegin;
                        mStart = true;
                        if (index == null)
                            return false;
                        return true;
                    }
                    index = index.next;
                    if (index == null)
                        return false;
                    return true;
                }
                public void Reset()
                {
                    this.mStart = false;
                }
                public void Dispose() { }
            }
        }


        #endregion
        //---------------------------------------------------------------------------------------------
        #region Events

        private ObjectSwapHandler event_OnObjectSwapped;
        public event ObjectSwapHandler OnObjectSwapped
        {
            add { event_OnObjectSwapped += value; }
            remove { event_OnObjectSwapped -= value; }
        }

        public delegate void ObjectSwapHandler(SpaceUserTag obj_node, SpaceCellNode new_node, SpaceCellNode old_node);

        #endregion
        //---------------------------------------------------------------------------------------------
    }

}
