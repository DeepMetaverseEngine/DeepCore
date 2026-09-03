using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.UI
{
    public class UIScrollPan : UIScrollBase
    {
        //---------------------------------------------------------------------------------------------
        //高级控件ScrollPan支持节点复用pageView Editor by Alex.Yu.
        //---------------------------------------------------------------------------------------------

        private List<List<DisplayNode>> matrix;
        private int mGrid_columns = 0;
        private int mGrid_rows = 0;
        private Rectangle2D mLastGridRect = new Rectangle2D();
        private Rectangle2D mCurrGridRect = new Rectangle2D();
        private Rectangle2D mViewPortRect = new Rectangle2D();
        private float mGridWidth = 1; // 格子宽.
        private float mGridHeight = 1; // 格子高.

        //页模式滚动阀值（手指移动超过该值触发滚动).
        protected int mPageBeginScrollValue = 20;

        public int PageBeginScrollValue
        {
            get { return mPageBeginScrollValue; }
            set { mPageBeginScrollValue = value; }
        }


        private bool mScrollAsGrid = false;

        /// <summary>
        /// 以格子为单位滚动，保证滚动停止后节点整个停在视窗中.
        /// </summary>
        public bool ScrollAsGrid
        {
            set { mScrollAsGrid = value; }
            get { return mScrollAsGrid; }
        }

        public delegate DisplayNode ScrollPanAddChildHandler(int gx, int gy);
        public event ScrollPanAddChildHandler OnChildEnterBounds;
        public delegate void ScrollPanRemoveChildHandler(int gx, int gy, DisplayNode obj);
        public event ScrollPanRemoveChildHandler OnChildExitBounds;
        #region 托管模式.
        public delegate void ScrollPanUpdateHandler(int gx, int gy, DisplayNode obj);
        private event ScrollPanUpdateHandler OnUpdateChild;
        public delegate void TrusteeshipChildInit(DisplayNode obj);
        private event TrusteeshipChildInit OnChildInit;

        //节点托管（控件自己创建节点).
        private bool mTrusteeshipChild = false;
        //原始节点.用于clone创建.
        private DisplayNode mOriginChild = null;
        private List<DisplayNode> mTrusteeshipList = null;
#endregion

        public UIScrollPan() { }

        public override void Initialize()
        {
            base.Initialize();
            mLastGridRect = new Rectangle2D();
        }

        public void Initialize(float unitWidth,
                               float unitHeight,
                               int rows,
                               int columns)
        {
            ContentHeight = unitWidth * columns;
            ContentWidth = unitHeight * rows;
            Initialize();
            if (matrix == null)
            {
                matrix = new List<List<DisplayNode>>();
            }
            mGridWidth = unitWidth;
            mGridHeight = unitHeight;

            InitRowsAndColumns(rows, columns);
        }

#region 托管模式.

        /// <summary>
        /// 托管模式初始化.
        /// </summary>
        /// <param name="unitWidth"></param>
        /// <param name="unitHeight"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="node"></param>
        /// <param name="callBack"></param>
        public void Initialize(float unitWidth,
                               float unitHeight,
                               int rows,
                               int columns,
                               DisplayNode node,
                               ScrollPanUpdateHandler callBack,
                               TrusteeshipChildInit initCallBack = null)
        {
            if (rows == 0 || columns == 0)
            {
                return;
            }

            if (unitHeight == 0 || unitWidth == 0 || callBack == null || node == null)
            {
                Driver.Instance.Assert(false, "Scrollpan Init Error :Args is invaild");
                return;
            }

            mOriginChild = node;
            OnUpdateChild = callBack;
            mTrusteeshipChild = true;

            ClearAllGrid();

            int sum = CalNodeCacheCount(unitWidth, unitHeight, rows, columns);

            DestoryTrusteeshipList();

            mTrusteeshipList = new List<DisplayNode>();

            DisplayNode temp = null;

            for (int i = 0; i < sum; i++)
            {
                temp = mOriginChild.Clone();
                if (initCallBack != null) { initCallBack.Invoke(temp); }
                mTrusteeshipList.Add(temp);
            }

            Initialize(unitWidth, unitHeight, rows, columns);
        }

        private int CalNodeCacheCount(float unitWidth, float unitHeight, int rows, int columns)
        {
            int cacheNode = 4;
            int count = 0;
            //向上取整.
            int gridX = (int)Math.Ceiling(ScrollRect.width / unitWidth);
            int gridY = (int)Math.Ceiling(ScrollRect.height / unitHeight);

            if (EnableScrollV && EnableScrollH)
            {
                count = (gridX + cacheNode) * (gridY + cacheNode);
            }
            else if (EnableScrollV)
            {
                count = gridX * (gridY + cacheNode);
            }
            else if (EnableScrollH)
            {
                count = (gridX + cacheNode) * gridY;
            }

            if (count > 0 && count > rows * columns) { return rows * columns; }

            count = Math.Max(1, count);

            return count;
        }

        private void DestoryTrusteeshipList()
        {
            if (mTrusteeshipList == null) { return; }
            for (int i = 0; i < mTrusteeshipList.Count; i++)
            {
                mTrusteeshipList[i].RemoveFromParent(false);
                mTrusteeshipList[i].Dispose();
            }
            mTrusteeshipList.Clear();
            mTrusteeshipList = null;
        }
        /// <summary>
        /// 刷新当前窗口内节点信息(仅限托管模式).
        /// </summary>
        public void RefreshCurViewPortNode()
        {
            if (mTrusteeshipChild) { UpdateCurViewPortNode(mLastGridRect, mCurrGridRect); }
        }

        private void UpdateCurViewPortNode(Rectangle2D oldc, Rectangle2D newc)
        {
            int x1 = (int)Math.Min(oldc.x, newc.x);
            int y1 = (int)Math.Min(oldc.y, newc.y);
            int x2;
            int y2;
            // if (isAdd)
            //{
            x2 = (int)Math.Max(oldc.Right, newc.Right);
            y2 = (int)Math.Max(oldc.Bottom, newc.Bottom);

            x2 = Math.Min(mGrid_columns, x2 + 1);
            y2 = Math.Min(mGrid_rows, y2 + 1);
            //  }
            DisplayNode addItem;
            for (int ix = x1; ix < x2; ++ix)
            {
                for (int iy = y1; iy < y2; ++iy)
                {
                    if (newc.contains(ix, iy))
                    {
                        addItem = null;
                        addItem = matrix[iy][ix];
                        if (addItem != null)
                        {
                            OnUpdateChild.Invoke(ix, iy, addItem);
                            continue; // In view port.
                        }
                    }
                }
            }
        }

#endregion
        private void InitRowsAndColumns(int rows, int columns)
        {
            ClearAllGrid();
            mGrid_rows = rows;
            mGrid_columns = columns;

            for (int r = 0; r < rows; r++)
            {
                List<DisplayNode> ca = new List<DisplayNode>(mGrid_rows);

                for (int c = 0; c < mGrid_columns; c++)
                {
                    ca.Add(null);
                }
                matrix.Add(ca);
            }

            this.ResetViewPort(true);
        }

        private void AddRows(int rows)
        {

            mGrid_rows += rows;

            for (int r = 0; r < rows; r++)
            {
                List<DisplayNode> ca = new List<DisplayNode>();

                for (int c = 0; c < mGrid_columns; c++)
                {
                    ca.Add(null);
                }
                matrix.Add(ca);
            }
            this.ResetViewPort(true);
        }

        private void AddColumns(int columns)
        {

            mGrid_columns += columns;

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < mGrid_rows; r++)
                {
                    // matrix[r].Add(tempNode);
                    matrix[r].Add(null);
                }
            }
            this.ResetViewPort(true);
        }

        public void SetRows(int rows, bool reset)
        {
            StopScroll();

            if (rows > mGrid_rows)
            {
                AddRows(rows - mGrid_rows);
            }
            else if (rows < mGrid_rows)
            {
                RemoveRows(0, (mGrid_rows - rows));
            }
            if (reset) { ClearGrid(true); }

        }

        public void SetColumns(int columns, bool reset)
        {
            StopScroll();

            if (columns > mGrid_columns)
            {
                int r = columns - mGrid_columns;
                AddColumns(r);
            }
            else if (columns < mGrid_columns)
            {
                RemoveColumns(0, (mGrid_columns - columns));
            }
            if (reset) { ClearGrid(true); }
        }

        public int GetRows()
        {
            return mGrid_rows;
        }

        public int GetColumns()
        {
            return mGrid_columns;
        }

        /// <summary>
        /// /删除一定数量的行，改变容量.
        /// </summary>
        /// <param name="start_row"></param>
        /// <param name="count"></param>
        public void RemoveRows(int start_row, int count)
        {
            StopScroll();

            if (start_row + count < mGrid_rows)
            {
                mGrid_rows -= count;
                List<List<DisplayNode>> removed = (matrix.GetRange(start_row, count)) as List<List<DisplayNode>>;
                matrix.RemoveRange(start_row, count);
                for (int r = 0; r < count; r++)
                {
                    int sr = r + start_row;
                    for (int c = 0; c < mGrid_columns; c++)
                    {
                        DisplayNode rmd = removed[r][c];
                        if (rmd != null)
                        {
                            this.mContainer.RemoveChild(rmd, false);
                            this.ChildExitBounds(c, sr, rmd);
                        }
                    }
                }

                this.ResetViewPort(false);
            }
        }

        /**删除一定数量的列，改变容量.*/
        public void RemoveColumns(int start_column, int count)
        {
            StopScroll();

            if (start_column + count < mGrid_columns)
            {
                mGrid_columns -= count;
                for (int r = 0; r < mGrid_rows; r++)
                {
                    List<DisplayNode> removed = matrix[r].GetRange(start_column, count);
                    matrix[r].RemoveRange(start_column, count);

                    for (int c = 0; c < count; c++)
                    {
                        int sc = c + start_column;
                        DisplayNode rmd = removed[c];
                        if (rmd != null)
                        {
                            this.mContainer.RemoveChild(rmd, false);
                            this.ChildExitBounds(sc, r, rmd);
                        }
                    }
                }
                this.ResetViewPort(false);
            }
        }

        public void ClearAllGrid()
        {
            if (matrix == null) { return; }

            for (int i = 0; i < matrix.Count; i++)
            {
                for (int j = 0; j < matrix[i].Count; j++)
                {
                    DisplayNode rmd = matrix[i][j];
                    matrix[i][j] = null;
                    if (rmd != null)
                    {
                        this.mContainer.RemoveChild(rmd, false);
                        this.ChildExitBounds(i, j, rmd);
                    }
                }
            }
            matrix.Clear();
        }

        public void ClearGrid(bool bRefresh)
        {
            if (matrix == null) { return; }

            for (int r = 0; r < matrix.Count; r++)
            {
                for (int c = 0; c < matrix[r].Count; c++)
                {
                    DisplayNode rmd = matrix[r][c];
                    matrix[r][c] = null;

                    if (rmd != null)
                    {
                        this.mContainer.RemoveChild(rmd, false);
                        this.ChildExitBounds(r, c, rmd);
                    }
                }
            }

            if (bRefresh)
            {
                ResetViewPort(true);
            }
        }

        private void ChildExitBounds(int gx, int gy, DisplayNode obj)
        {
            if (mTrusteeshipChild == false)
            {
                if (OnChildExitBounds != null)
                {
                    OnChildExitBounds(gx, gy, obj);
                }
            }
            else
            {
                mTrusteeshipList.Add(obj);
            }
        }

        private DisplayNode ChildEnterBounds(int gx, int gy)
        {
            if (mTrusteeshipChild == false)
            {
                if (OnChildEnterBounds != null)
                {
                    return OnChildEnterBounds(gx, gy);
                }
            }
            else
            {
                DisplayNode addItem = mTrusteeshipList[0];
                mTrusteeshipList.RemoveAt(0);
                OnUpdateChild.Invoke(gx, gy, addItem);
                return addItem;
            }
            return null;
        }

        protected void ResetViewPort(bool isAdd)
        {

            mContentWidth = Math.Max(ScrollRect.width, mGridWidth * mGrid_columns);
            mContentHeight = Math.Max(ScrollRect.height, mGridHeight * mGrid_rows);
            Initialize();
            this.mCurrGridRect = this.ConvertToGrid(this.GetViewPort(), this.mCurrGridRect);
            this.InitViewPort(this.mLastGridRect, this.mCurrGridRect, isAdd);

            this.mLastGridRect = new Rectangle2D(this.mCurrGridRect);
        }

        protected Point2D ConvertToGridPoint(Point2D pnt, Point2D rlt)
        {
            rlt.x = (int)(pnt.x / mGridWidth);
            rlt.y = (int)(pnt.y / mGridHeight);

            rlt.x = Math.Max(rlt.x, 0);
            rlt.y = Math.Max(rlt.y, 0);

            if (rlt.x >= mGrid_columns)
                rlt.x = -1;
            if (rlt.y >= mGrid_rows)
                rlt.y = -1; // 防止点击最后一个后面的会出现点击最后一个.
            return rlt;
        }

        protected Rectangle2D ConvertToGrid(Rectangle2D srcRect, Rectangle2D rlt)
        {
            //if(rlt == null) rlt = new Rect();

            int nX = (srcRect.x % mGridWidth > 2) ? 1 : 0;
            int nY = (srcRect.y % mGridHeight > 2) ? 1 : 0;

            //+1作为补偿值.
            rlt.x = (int)((srcRect.x + 1) / mGridWidth);
            rlt.y = (int)((srcRect.y + 1) / mGridHeight);

            rlt.width = (float)Math.Ceiling(srcRect.width / mGridWidth) + nX;
            rlt.height = (float)Math.Ceiling(srcRect.height / mGridHeight) + nY;

            rlt.x = Math.Max(rlt.x, 0);
            rlt.y = Math.Max(rlt.y, 0);

            return rlt;
        }

        protected Rectangle2D GetViewPort()
        {
            mViewPortRect.x = -this.mContainer.X;
            mViewPortRect.y = -this.mContainer.Y;
            mViewPortRect.width = this.ScrollRect.width;
            mViewPortRect.height = this.ScrollRect.height;
            return mViewPortRect;
        }

        protected void InitViewPort(Rectangle2D oldc, Rectangle2D newc, bool isAdd)
        {
            int x1 = (int)Math.Min(oldc.x, newc.x);
            int y1 = (int)Math.Min(oldc.y, newc.y);
            int x2;
            int y2;
            // if (isAdd)
            //{
            x2 = (int)Math.Max(oldc.Right, newc.Right);
            y2 = (int)Math.Max(oldc.Bottom, newc.Bottom);

            x2 = Math.Min(mGrid_columns, x2 + 1);
            y2 = Math.Min(mGrid_rows, y2 + 1);
            //  }


            DisplayNode addItem;
            DisplayNode rmdItem;
            for (int ix = x1; ix < x2; ++ix)
            {
                for (int iy = y1; iy < y2; ++iy)
                {
                    if (newc.contains(ix, iy))
                    {
                        addItem = null;
                        addItem = matrix[iy][ix];
                        if (addItem != null)
                        {

                            continue; // In view port.
                        }
                        //
                        // Enter view port.
                        //
                        addItem = this.ChildEnterBounds(ix, iy);

                        if (addItem == null)
                        {
                            continue;
                        }

                        this.mContainer.AddChild(addItem);
                        addItem.X = ix * mGridWidth;
                        addItem.Y = iy * mGridHeight;
                        matrix[iy][ix] = addItem;
                    }
                    else
                    {
                        //
                        // Exit view port.
                        //
                        rmdItem = null;
                        rmdItem = matrix[iy][ix];
                        if (rmdItem == null)
                        {
                            continue;
                        }

                        matrix[iy][ix] = null;
                        this.mContainer.RemoveChild(rmdItem, false);

                        this.ChildExitBounds(ix, iy, rmdItem);
                    }
                }
            }
        }

        public int GetRowIndexInView()
        {
            Rectangle2D viewPort = GetViewPort();
            Point2D viewPnt = new Point2D(viewPort.x + 2, viewPort.y + 2);

            viewPnt = ConvertToGridPoint(viewPnt, new Point2D());

            return (int)(viewPnt.y);
        }

        public int GetColumnsIndexInView()
        {
            Rectangle2D viewPort = GetViewPort();
            Point2D viewPnt = new Point2D(viewPort.x + 2, viewPort.y + 2);
            viewPnt = ConvertToGridPoint(viewPnt, new Point2D());
            return (int)(viewPnt.x);
        }

        public int GetCurrentPage()
        {
            if (!mEnablePage) { return 0; }

            if (EnableScrollH)
            {
                return GetColumnsIndexInView();
            }
            else
            {
                return GetRowIndexInView();
            }
        }

        public DisplayNode GetGridAt(int rows, int columns)
        {
            if (rows > mGrid_rows || columns > mGrid_columns || matrix.Count == 0) { return null; }
            return matrix[rows][columns];
        }

        public Point2D GetChildIndexInView(DisplayNode node)
        {

            Point2D result = new Point2D();

            result.x = node.X;
            result.y = node.Y;

            Point2D rlt = ConvertToGridPoint(result, new Point2D());
            if (rlt.x < 0 || rlt.y < 0)
            {
                rlt.x = -1;
                rlt.y = -1;
            }
            return rlt;
        }

        public void ShowAt(int gx, int gy, bool bScroll)
        {

            gx = Math.Min(gx, this.mGrid_columns - 1);
            gx = Math.Max(gx, 0);

            gy = Math.Min(gy, this.mGrid_rows - 1);
            gy = Math.Max(gy, 0);


            float x = gx * this.mGridWidth;
            float y = gy * this.mGridHeight;

            ShowTo(new Point2D(x, y), bScroll);
        }

        public void ShowTo(Point2D target, bool bScroll)
        {
            if (bScroll)
            {
                StopScroll();
                bool tempBScroll = ScrollTo(target.x, target.y);
                if (tempBScroll && !mAutoScrolling) { OnScrollStart(); }
            }
            else
            {

                float dx = -(target.x + this.mContainer.X) / mAutoElasticityValue;
                float dy = -(target.y + this.mContainer.Y) / mAutoElasticityValue;

                Point2D helpPoint = new Point2D(dx, dy);

                helpPoint.x = helpPoint.x * mAutoElasticityValue + this.mContainer.X;
                helpPoint.y = helpPoint.y * mAutoElasticityValue + this.mContainer.Y;

                if (!GetScrollTargetPoint(helpPoint))
                {
                    return;
                }

                this.SetViewPortXY(-mAutoTarget.x, -mAutoTarget.y);
                this.OnScrolling();

            }
        }

        private bool ScrollTo(float x, float y)
        {
            float dx = -(x + this.mContainer.X) / mAutoElasticityValue;
            float dy = -(y + this.mContainer.Y) / mAutoElasticityValue;

            return StartScroll(dx, dy);
        }

        private void SetViewPortXY(float vx, float vy)
        {
            if (this.EnableScrollH) { this.mContainer.X = -vx; }
            if (this.EnableScrollV) { this.mContainer.Y = -vy; }
            OnScrollFinish(null);
        }

        protected override void StartScrollAsPageView(float dx, float dy)
        {
            Point2D tempPoint = new Point2D(dx, dy);
            float length = tempPoint.Length;
            //距离大于阀值触发滚动，反之回滚至原来的页面.
            if (length > mPageBeginScrollValue)
            {
                //
                // scrolling to next page.
                //
                int xSize = (int)Math.Ceiling(this.ScrollRect.width / (this.mGridWidth + 2));
                int ySize = (int)Math.Ceiling(this.ScrollRect.height / (this.mGridHeight + 2));

                Point2D ltPnt = new Point2D(2 - this.mContainer.X, 2 - this.mContainer.Y);
                /*
                bool firstPage;
                if (mEnableScrollH)
                    firstPage = mContainer.RelativePos.X > -mGridWidth * 0.5;
                if (mEnableScrollV)
                    firstPage = mContainer.RelativePos.Y > -mGridHeight * 0.5;
                */
                ltPnt = ConvertToGridPoint(ltPnt, new Point2D());

                int gx = (int)(ltPnt.x / xSize) * xSize;
                int gy = (int)(ltPnt.y / ySize) * ySize;



                if (tempPoint.x < -2)
                    gx += xSize;	// scroll to left, so set page to right.
                if (tempPoint.y < -2)
                    gy += ySize; // scroll to top, so set page to bottom.

                ShowAt(gx, gy, true);
            }
            else
            {
                //
                // scrolling to current page.
                //
                float xValue = this.mContainer.X - Math.Abs(tempPoint.x);
                float yValue = this.mContainer.Y - Math.Abs(tempPoint.y);

                Point2D curPnt = new Point2D(2 - xValue, 2 - yValue);
                curPnt = this.ConvertToGridPoint(curPnt, new Point2D());

                ShowAt((int)curPnt.x, (int)curPnt.y, true);
            }
        }

        protected override void OnScrolling()
        {
            UpdateScrollView();
            base.OnScrolling();
        }

        protected override void OnScrollFinish(IActionCompment action)
        {
            this.mCurrGridRect = ConvertToGrid(GetViewPort(), this.mCurrGridRect);

            InitViewPort(this.mLastGridRect, this.mCurrGridRect, false);
            this.mLastGridRect = new Rectangle2D(this.mCurrGridRect);

            base.OnScrollFinish(action);
        }

        public void UpdateScrollView()
        {
            this.mCurrGridRect = ConvertToGrid(GetViewPort(), this.mCurrGridRect);

            InitViewPort(mLastGridRect, mCurrGridRect, true);
            this.mLastGridRect = new Rectangle2D(this.mCurrGridRect);
        }

        protected override void Disposing()
        {
            if (matrix != null)
            {
                if (mTrusteeshipChild == true) { ClearGrid(false); }
                matrix.Clear();
                matrix = null;
            }

            OnChildEnterBounds = null;
            OnChildExitBounds = null;
            OnUpdateChild = null;
            DestoryTrusteeshipList();
            if (mOriginChild != null)
            {
                mOriginChild.RemoveFromParent(false);
                mOriginChild.Dispose();
                mOriginChild = null;
            }

            base.Disposing();
        }

        public override void TouchEnd(NodeTouch touch)
        {
            if (mEnablePage && mAutoScrolling)
            {
                mIsTouch = false;
                return;
            }
            base.TouchEnd(touch);
        }

        protected override bool StartScroll(float dx, float dy)
        {
            Point2D helpPoint = new Point2D(dx, dy);

            //
            // get target point for auto scrolling.
            //
            helpPoint.x = helpPoint.x * mAutoElasticityValue + this.mContainer.X;
            helpPoint.y = helpPoint.y * mAutoElasticityValue + this.mContainer.Y;

            if(mScrollAsGrid == true)
            {
                GetGridTargetPoint(ref helpPoint);
            }


            if (!GetScrollTargetPoint(helpPoint))
            {
                return false;
            }

            OnScrollStart();
            
            Point2D dis = new Point2D(mAutoTarget.x - mContainer.X, mAutoTarget.y - mContainer.Y);

            mAutoTime = dis.Length * mAutoTimeConst;
            mAutoTime = Math.Max(mAutoTime, mAutoMinTime);
            mAutoTime = Math.Min(mAutoTime, mAutoMaxTime);
            MoveAction move = new MoveAction();
            move.TargetX = this.mAutoTarget.x;
            move.TargetY = this.mAutoTarget.y;
            move.Duration = mAutoTime;

            if (mEnableElasticity == true)
            {
                if (this.mAutoTarget.x == helpPoint.x && this.mAutoTarget.y == helpPoint.y)
                {
                    move.TransitionsType = Transitions.EASE_OUT;
                }
                else
                {
                    move.TransitionsType = Transitions.EASE_OUT_BACK;
                }
            }
            else
            {
                move.TransitionsType = Transitions.EASE_OUT;
            }

            move.ActionFinishCallBack = OnScrollFinish;
            this.mContainer.AddAction(move);
            return true;

        }

        //重新计算目标点，保证节点完整的处于视口内.
        protected void GetGridTargetPoint(ref Point2D dst)
        {
         
            //目前移动的值，最接近的整数.
            int grid_numX = 0;
            int grid_numY = 0;

            if (EnableScrollH)
            {
                if (this.mContainer.X - dst.x >= 0)
                {
                    grid_numX = (int)Math.Floor(dst.x / this.mGridWidth);
                }
                else
                {
                    grid_numX = (int)Math.Ceiling(dst.x / this.mGridWidth);
                }

                dst.x = grid_numX * this.mGridWidth;
            }
            else if (EnableScrollV)
            {
                if (this.mContainer.Y - dst.y >= 0)
                {
                    grid_numY = (int)Math.Floor(dst.y / this.mGridHeight);
                }
                else
                {
                    grid_numY = (int)Math.Ceiling(dst.y / this.mGridHeight);
                }

                dst.y = grid_numY * this.mGridHeight;
            }
        }


        protected override void DecodeFields(UIEditor editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            if (e is UEScrollPanMeta)
            {
                UEScrollPanMeta meta = e as UEScrollPanMeta;

                EnableScrollH = meta.EnableScrollH;
                EnableScrollV = meta.EnableScrollV;
                EnableElasticity = meta.EnableElasticity;
                BorderSize = meta.BorderSize;

                Gemo.Rectangle2D rect = this.GetContentsRealSize();
                this.ContentWidth = rect.width;
                this.ContentHeight = rect.height;
                this.Initialize();
            }

        }
    }
}

