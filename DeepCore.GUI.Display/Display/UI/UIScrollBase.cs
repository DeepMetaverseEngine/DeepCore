using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.UI
{
    public class UIScrollBase : UIComponent
    {

        public enum ScrollPosition
        {
            eTop,
            eBottom,
            eLeft,
            eRight,
            eMiddle,
            eNone
        }

        public delegate void UIEventHandler(DisplayNode sender);

        public const float CHANGE_TO_NEXT_PAGE_FACTOR = 0.3f;
        protected float mXOffset = 0;
        protected float mYOffset = 0;
        private bool mEnableScrollV = false;
        private bool mEnableScrollH = false;
        protected bool mEnablePage = false;
        protected bool mEnableElasticity = true;
        protected int mPageIndex = 0;
        protected float mContentWidth = 0;
        protected float mContentHeight = 0;
        protected float mOriginalX = 0;
        protected float mOriginalY = 0;

        //--------------------------------------------------------------------------------------------- 
        //显示窗口偏移值（显示窗口大小 = 控件宽高 - mBorderSize.
        private float mBorderSize = 0;
        //负责滑动的节点.
        protected UIComponent mContainer = new UIComponent();
        // TouchBegin Point.
        protected Point2D mBeginPoint = new Point2D();
        // LastMoving Point.
        protected Point2D mLastPoint = new Point2D();
        // Moving Vector.
        protected Point2D mVectPoint = new Point2D();
        protected Point2D mPositionPoint = new Point2D();
        protected Point2D mTouchPoint = new Point2D();
        protected Point2D mMaxPoint = new Point2D();
        protected Point2D mMinPoint = new Point2D();
        protected Point2D mAutoMaxPoint = new Point2D();
        protected Point2D mAutoMinPoint = new Point2D();
        protected Point2D mAutoTarget = new Point2D();
        protected bool mAutoScrolling = false;

        private float mHBarWidth = 10.0f;
        private float mVBarHeight = 10.0f;

        //在android设备上，系统事件响应延迟较大，很多时候出现与上一帧坐标相同，导致滑动不执行增加帧计数可减缓该问题.
        private int mFrameCount;
        private const int mFrameNum = 3;
        protected float mAutoTime = 0;
        protected float mAutoTimeConst = 0.01f;
        protected float mAutoMaxTime = 1.0f;
        protected float mAutoMinTime = 0.3f;
        protected float mAutoElasticityValue = 10.0f;

        protected bool mIsTouch = false;
        protected bool mIsMove = false;

        /// <summary>
        /// 滑动时回调函数.
        /// </summary>
        public event UIEventHandler OnMove;

        /// <summary>
        /// 停止滑动时回调函数.
        /// </summary>
        public event UIEventHandler OnStop;

        /// <summary>
        /// 开始滚动回调函数.
        /// </summary>
        public event UIEventHandler OnStart;

        public UIScrollBase()
        {
            base.AddChild(mContainer);
        }

        public virtual void Initialize()
        {
            mContainer.X = mContainer.Y = 0;
            CalculateMinMaxXY();
        }

        /// <summary>
        ///  设置最大滚动时间.
        /// </summary>
        public float ScrollMaxTime
        {
            set
            {
                mAutoMaxTime = value;
            }
        }

        /// <summary>
        /// 设置最小滚动时间.
        /// </summary>
        public float ScrollMinTime
        {
            set
            {
                mAutoMinTime = value;
            }
        }

        /// <summary>
        /// 设置灵活系数，影响滚动速度.
        /// </summary>
        public float AutoElasticityValue
        {
            set
            {
                mAutoElasticityValue = value;
            }
        }

        /// <summary>
        /// 开启或者不开启按页滑动.
        /// </summary>
        public bool EnablePage
        {
            get
            {
                return mEnablePage;
            }
            set
            {
                mEnablePage = value;
            }
        }

        /// <summary>
        /// 按页滑动时当前页数.
        /// </summary>
        public int PageIndex
        {
            get
            {
                return -mPageIndex;
            }
        }

        /// <summary>
        /// 开启或者不开启弹性功能.
        /// </summary>
        public bool EnableElasticity
        {
            get
            {
                return mEnableElasticity;
            }
            set
            {
                mEnableElasticity = value;
            }
        }

        /// <summary>
        /// 滚动控件的内容宽度.
        /// </summary>
        public float ContentWidth
        {
            set
            {
                mContentWidth = Math.Max(value, this.Bounds.width);
            }
            get
            {
                return mContentWidth;
            }
        }

        /// <summary>
        /// 滚动控件的内容宽度.
        /// </summary>
        public float ContentHeight
        {
            set
            {
                mContentHeight = Math.Max(value, this.Bounds.height);
            }
            get
            {
                return mContentHeight;
            }
        }

        public bool EnableScrollV
        {
            set
            {
                mEnableScrollV = value;
            }
            get
            {
                return mEnableScrollV;
            }
        }

        public bool EnableScrollH
        {
            set
            {
                mEnableScrollH = value;
            }
            get
            {
                return mEnableScrollH;
            }
        }

        public float BorderSize
        {
            set
            {
                mBorderSize = value;
                if (Bounds != null)
                {
                    ScrollRect = new Rectangle2D(Bounds.x + BorderSize, Bounds.y + BorderSize,
                                                 Bounds.width - BorderSize, Bounds.height - BorderSize);
                }
            }
            get
            {
                return mBorderSize;
            }
        }

        public Rectangle2D GetContentsRealSize()
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            Rectangle2D pos = null;

            foreach (DisplayNode ui in mContainer.GetChildren())
            {
                pos = ui.Bounds;
                if (pos == null)
                {
                    continue;
                }
                minX = pos.x < minX ? pos.x : minX;
                maxX = pos.Right > maxX ? pos.Right : maxX;
                minY = pos.y < minY ? pos.y : minY;
                maxY = pos.Bottom > maxY ? pos.Bottom : maxY;
            }

            return new Rectangle2D(minX, minY, maxX, maxY);
        }

        protected virtual void onTouchEnd()
        {

            this.mIsTouch = false;
            this.mIsMove = false;
            this.EnableChildren = true;
            this.SetAutoScrolling();
        }

        protected override DisplayNode PushEvent(TouchEvent touchData, bool forTouch = true)
        {
            if (Enable == false)
            {
                return null;
            }
            if (forTouch && !HasVisibleArea())
            {
                return null;
            }
            DisplayNode temp = HitTest(new Point2D(touchData.GlobalX, touchData.GlobalY));

            if (temp == null)
            {
                if (mIsMove == true)
                {
                    TouchOut(null);
                }
                return null;
            }

            if (touchData.TouchType == TouchEvent.TouchEnum.eTouchMove)
            {
                if (temp != null && this.mIsTouch == false)
                {
                    TouchBegin(null);
                }
                return temp;
            }
            else
            {
                return base.PushEvent(touchData, forTouch);
            }
        }

        protected override DisplayNode HitTest(Point2D globalPoint)
        {

            if (Enable == false || ScrollRect == null)
            {
                return null;
            }
            Point2D p = this.GlobalToLocal(globalPoint);
            mTouchPoint = p;
            if (ScrollRect.contains(p.x, p.y) == true)
            {
                return this;
            }
            return null;
        }

        public override void TouchBegin(NodeTouch touch)
        {
            this.mIsTouch = true;
            this.EnableChildren = false;
            if (mAutoScrolling && !mEnablePage)
            {
                StopScroll();
            }

            this.mLastPoint.x = mTouchPoint.x;
            this.mLastPoint.y = mTouchPoint.y;

            this.mBeginPoint.x = mTouchPoint.x;
            this.mBeginPoint.y = mTouchPoint.y;

            this.mPositionPoint.x = mTouchPoint.x;
            this.mPositionPoint.y = mTouchPoint.y;

            base.TouchBegin(touch);
        }

        public override void TouchEnd(NodeTouch touch)
        {
            onTouchEnd();
            base.TouchEnd(touch);
        }

        public override void TouchMove(NodeTouch touch)
        {
            if (mAutoScrolling == true && mEnablePage == true)
            {
                return;
            }
            mIsMove = true;
            base.TouchMove(touch);
        }

        public override void TouchOut(NodeTouch touch)
        {
            onTouchEnd();
            base.TouchOut(touch);
        }

        public override void Update(float DeltaTime)
        {
            base.Update(DeltaTime);
            if (mIsMove)
            {
                int x = (int)(this.mTouchPoint.x - this.mPositionPoint.x);
                int y = (int)(this.mTouchPoint.y - this.mPositionPoint.y);

                if (mFrameCount >= mFrameNum)
                {
                    mFrameCount = 0;
                    this.mLastPoint.x = this.mTouchPoint.x;
                    this.mLastPoint.y = this.mTouchPoint.y;
                }
                else
                {
                    mFrameCount++;
                }

                if (x != 0 || y != 0)
                {
                    this.mPositionPoint.x = this.mTouchPoint.x;
                    this.mPositionPoint.y = this.mTouchPoint.y;

                    OffsetViewPort(x, y);
                    OnScrolling();
                }
            }
            else if (mAutoScrolling)
            {
                this.OnScrolling();
            }
        }

        /// <summary>
        /// 将显示内容移动到指定的位置.
        /// </summary>
        /// <param name="dx">坐标X.</param>
        /// <param name="dy">坐标Y.</param>

        protected virtual void OffsetViewPort(float dx, float dy)
        {
            if (this.mEnableScrollH)
            {

                if (mEnableElasticity == false)
                {
                    if (this.mContainer.X + dx > this.mMaxPoint.x || this.mContainer.X + dx < this.mMinPoint.x)
                    {
                        //do nothing.
                    }
                    else
                    {
                        this.mContainer.X += dx;
                    }
                }
                else
                {
                    this.mContainer.X += dx;
                }
            }

            if (this.mEnableScrollV)
            {

                if (mEnableElasticity == false)
                {
                    if (this.mContainer.Y + dy > this.mMaxPoint.y || this.mContainer.Y + dy < this.mMinPoint.y)
                    {
                        //do nothing.
                    }
                    else
                    {
                        this.mContainer.Y += dy;
                    }
                }
                else
                {
                    this.mContainer.Y += dy;
                }
            }
        }

        protected virtual void CalculateMinMaxXY()
        {
            if (this.ScrollRect == null || this.Bounds == null)
            {
                return; // 
            }

            //
            // calcaulte scroll height/width depends on scroll rectangle and locale baounds.
            //		
            this.mHBarWidth = ScrollRect.width / this.ContentWidth * this.ScrollRect.width;
            this.mHBarWidth = Math.Min(this.mHBarWidth, this.ScrollRect.width);
            this.mHBarWidth = Math.Max(this.mHBarWidth, 20);

            this.mVBarHeight = this.ScrollRect.height / this.ContentHeight * this.ScrollRect.height;
            this.mVBarHeight = Math.Min(this.mVBarHeight, this.ScrollRect.height);
            this.mVBarHeight = Math.Max(this.mVBarHeight, 20);

            //
            // calculate minPoint/maxPoint without elasticity.
            //
            this.mMaxPoint.x = this.Bounds.x;
            this.mMaxPoint.y = this.Bounds.y;

            this.mMinPoint.x = this.mMaxPoint.x - this.ContentWidth + this.Bounds.width;
            this.mMinPoint.y = this.mMaxPoint.y - this.ContentHeight + this.Bounds.height;

            //if (this.mAutoScrolling)
            //{
            SetScrollArea(this.mMinPoint, this.mMaxPoint);
            //}

            //
            // calculate minPoint/maxPoint with elasticity.
            // 
            if (this.mEnableElasticity)
            {
                this.mMaxPoint.x = this.Bounds.x + this.Bounds.width * 0.5f;
                this.mMaxPoint.y = this.Bounds.y + this.Bounds.height * 0.5f;

                this.mMinPoint.x = this.mMaxPoint.x - this.ContentWidth;
                this.mMinPoint.y = this.mMaxPoint.y - this.ContentHeight;
            }
        }

        /// <summary>
        /// 设置可滚动的区域.
        /// </summary>
        /// <param name="minPoint"></param>
        /// <param name="maxPoint"></param>
        private void SetScrollArea(Point2D minPoint, Point2D maxPoint)
        {
            mAutoMaxPoint = new Point2D(maxPoint.x, maxPoint.y);
            mAutoMinPoint = new Point2D(minPoint.x, minPoint.y);
        }

        /// <summary>
        /// 计算滚动的坐标及速度.
        /// </summary>
        /// <param name="x">leave事件接受到的X.</param>
        /// <param name="y">leave事件接受到的Y.</param>
        protected virtual void SetAutoScrolling()
        {

            if (!mEnablePage)
            {
                this.mVectPoint.x = mTouchPoint.x - this.mLastPoint.x;
                this.mVectPoint.y = mTouchPoint.y - this.mLastPoint.y;
            }
            else
            {
                this.mVectPoint.x = mTouchPoint.x - this.mBeginPoint.x;
                this.mVectPoint.y = mTouchPoint.y - this.mBeginPoint.y;
            }

            //
            // check scroll direction.
            //
            if (!this.mEnableScrollH)
            {
                mVectPoint.x = 0;
            }
            if (!this.mEnableScrollV)
            {
                mVectPoint.y = 0;
            }

            if (mEnablePage && mAutoScrolling)
            {
                return;
            }

            if (mEnablePage)
            {
                StartScrollAsPageView(mVectPoint.x, mVectPoint.y);
            }
            else
            {
                StartScroll(mVectPoint.x, mVectPoint.y);
            }

        }

        /// <summary>
        /// 根据目标点计算最终滚动到的坐标.
        /// </summary>
        /// <param name="targetPnt">目标坐标.</param>
        /// <returns>是否执行滚动.</returns>
        protected virtual Boolean GetScrollTargetPoint(Point2D targetPnt)
        {
            this.mAutoTarget = new Point2D(targetPnt.x, targetPnt.y);

            //如果超过滚动边界，则只移动到边界.
            this.mAutoTarget.x = Math.Max(this.mAutoMinPoint.x, this.mAutoTarget.x);
            this.mAutoTarget.x = Math.Min(this.mAutoMaxPoint.x, this.mAutoTarget.x);

            this.mAutoTarget.y = Math.Max(this.mAutoMinPoint.y, this.mAutoTarget.y);
            this.mAutoTarget.y = Math.Min(this.mAutoMaxPoint.y, this.mAutoTarget.y);


            //return if target point is current object's position.

            return (Math.Abs(this.mAutoTarget.x - this.mContainer.X) > 0.01 ||
                Math.Abs(this.mAutoTarget.y - this.mContainer.Y) > 0.01);
        }

        /// <summary>
        /// 执行滚动.
        /// </summary>
        protected virtual bool StartScroll(float dx, float dy)
        {
            Point2D helpPoint = new Point2D(dx, dy);

            //
            // get target point for auto scrolling.
            //
            helpPoint.x = helpPoint.x * mAutoElasticityValue + this.mContainer.X;
            helpPoint.y = helpPoint.y * mAutoElasticityValue + this.mContainer.Y;

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


        /// <summary>
        /// 页模式下执行滚动.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        protected virtual void StartScrollAsPageView(float dx, float dy)
        {
        }

        public override void Visit(Display.Graphics g)
        {
            if (ScrollRect != null)
            {
                g.PushClip();
                TRectangle2D rect = this.LocalToGlobal_S(ScrollRect);
                g.SetClip(rect.x, rect.y, rect.width, rect.height);
                base.Visit(g);
                g.PopClip();
            }
            else
            {
                base.Visit(g);
            }
        }


        /// <summary>
        /// 停止滚动.
        /// </summary>
        public virtual void StopScroll()
        {
            if (mContainer != null && mAutoScrolling)
            {
                mContainer.RemoveAction(MoveAction.ACTIONTYPE, true);
            }
        }

        /// <summary>
        ///滚动开始回调.  
        /// <summary>
        protected virtual void OnScrollStart()
        {
            mAutoScrolling = true;
            EnableChildren = false;

            if (OnStart != null)
            {
                OnStart(this);
            }
        }

        /// <summary>
        ///滚动中回调.
        /// <summary>
        protected virtual void OnScrolling()
        {
            //on Scrolling call back
        }

        /// <summary>
        ///滚动结束回调.
        /// <summary>
        protected virtual void OnScrollFinish(IActionCompment action)
        {
            mAutoScrolling = false;
            EnableChildren = true;
            if (OnStop != null)
            {
                OnStop(this);
            }
        }

        public override void AddChild(DisplayNode child)
        {
            mContainer.AddChild(child);
        }

        protected override void Disposing()
        {

            OnMove = null;
            OnStop = null;
            OnStart = null;

            if (mContainer != null)
            {
                mContainer.RemoveFromParent();
                mContainer.RemoveAllChildren();
                mContainer.Dispose();
                mContainer = null;
            }

            mBeginPoint = null;
            mLastPoint = null;
            mVectPoint = null;
            mPositionPoint = null;
            mTouchPoint = null;
            mMaxPoint = null;
            mMinPoint = null;
            mAutoMaxPoint = null;
            mAutoMinPoint = null;
            mAutoTarget = null;

            base.Disposing();
        }

        public ScrollPosition GetScrollPosition()
        {
            //水平.
            if (mEnableScrollH)
            {
                if (this.mAutoMaxPoint.x == this.mAutoTarget.x)
                {
                    if (this.mAutoMinPoint.x == this.mAutoTarget.x) { return ScrollPosition.eNone; }
                    //left.
                    return ScrollPosition.eLeft;
                }
                else if (this.mAutoMinPoint.x == this.mAutoTarget.x)
                {
                    //right.
                    return ScrollPosition.eRight;
                }
                else
                {
                    return ScrollPosition.eMiddle;
                }
            }
            //垂直.
            else if (mEnableScrollV)
            {
                if (this.mAutoMaxPoint.y == this.mAutoTarget.y)
                {
                    if (this.mAutoMinPoint.y == this.mAutoTarget.y) { return ScrollPosition.eNone; }
                    //bottom.
                    return ScrollPosition.eBottom;
                }
                else if (this.mAutoMinPoint.y == this.mAutoTarget.y)
                {
                    //top.
                    return ScrollPosition.eTop;
                }
                else
                {
                    return ScrollPosition.eMiddle;
                }
            }
            return ScrollPosition.eMiddle;
        }
    }



}

