using DeepCore.Geometry;

namespace DeepCore.GUI.Display.Node
{
    public class DisplayStage : DisplayNode
    {
        public DisplayStage() : base("ROOT")
        {

        }

        public enum TouchType
        {
            eTouchNull,
            eTouchBegin,
            eTouchEnd,
            eTouchMove,
            eTouchClick,
            eTouchLong,
            eTouchOut
        }


        private bool mIsMoving = false;
        private bool mIsTouching = false;

        private Vector2 mHelpPoint1 = new Vector2();
        private Vector2 mHelpPoint2 = new Vector2();

        private NodeTouch mTouchBegin = new NodeTouch();
        private NodeTouch mTouchData = new NodeTouch();

        private const float mMinMoveDistance = 5.0f;
        private float mLongTouchTime = 0.0f;
        private bool mStartLongTouchCount = false;
        private bool mHasSendLongTouch = false;
        private DisplayNode mLastNode = null;
        internal override DisplayStage Root
        {
            get { return this; }
        }

        /// <summary>
        /// send TouchEvents to UISystem.
        /// </summary>
        /// <param name="touchData"></param>
        public bool SendTouchEvent(TouchEvent touchData)
        {
            bool ret = true;
            mHelpPoint1 = touchData.GlobalPos;

            DisplayNode tempNode = PushEvent(touchData, true);

            if (tempNode == null)
            {
                tempNode = this;
                ret = false;
            }

            mTouchData.SetData(touchData.GlobalPos, tempNode, tempNode);
            switch (touchData.TouchType)
            {
                case TouchEvent.TouchEnum.eTouchBegin:
                    OnHandelTouchBegin(tempNode);
                    break;
                case TouchEvent.TouchEnum.eTouchEnd:
                    OnHandelTouchEnd(tempNode);
                    break;
                case TouchEvent.TouchEnum.eTouchMove:
                    OnHandelTouchMove(tempNode);
                    break;
                case TouchEvent.TouchEnum.eLongTouch:
                    OnHandleLongTouch(tempNode);
                    break;
                default:
                    break;
            }

            return ret;
        }

        protected virtual void OnHandelTouchBegin(DisplayNode node)
        {
            TouchBeginDataCheck();
            mTouchBegin = mTouchData.Clone();
            var p = node.GlobalToLocal(mTouchBegin.GlobalPos);
            var gp = node.LocalToGlobal(p.Value);

            bool intercept = OutputResponseNode(node, TouchType.eTouchBegin, mTouchData);
            if (!intercept)
            {
                //
                // base TouchBegin
                //
                node.TouchBegin(this.mTouchData);
                // stage TouchBegin
                //if (this.mTouchData.CurrentTarget != this)
                //{
                //    this.TouchBegin(this.mTouchData);
                //}
            }
            //
            // set touch state
            //
            this.mIsTouching = true;
            this.mIsMoving = false;
        }

        protected virtual void OnHandelTouchMove(DisplayNode node)
        {
            if (!this.IsMovingEnable())
            {
                return;
            }

            bool intercept = OutputResponseNode(this.mTouchBegin.Target, TouchType.eTouchMove, mTouchData);
            if (!intercept)
            {
                node.TouchMove(mTouchData);
            }

            //
            // TouchOut
            //
            //if(node != this.mTouchBegin.Target)
            //{
            //    if(this.mTouchBegin.Target != null && !this.mTouchBegin.Target.IsDispose)
            //    {
            //        intercept = OutputResponseNode(this.mTouchBegin.Target, TouchType.eTouchOut, mTouchData);
            //        if(!intercept)
            //        {
            //            this.mTouchBegin.Target.TouchOut(this.mTouchData);
            //        }
            //    }
            //}

            if (mLastNode != node)
            {
                if (mLastNode != null && mLastNode.IsDispose == false)
                {
                    intercept = OutputResponseNode(mLastNode, TouchType.eTouchOut, mTouchData);
                    if (!intercept)
                    {
                        mLastNode.TouchOut(this.mTouchData);
                    }
                }

            }

            mLastNode = node;
            //
            // stage TouchMove
            //
            //if (this.mTouchData.CurrentTarget != this)
            //{
            //    this.TouchMove(this.mTouchData);
            //}

        }

        protected virtual void OnHandelTouchEnd(DisplayNode node)
        {
            if (this.mIsTouching == false)
            {
                return;
            }

            bool intercept = OutputResponseNode(node, TouchType.eTouchEnd, mTouchData);
            if (!intercept)
            {
                //
                // handle touch end event.
                //
                node.TouchEnd(this.mTouchData);
            }

            if (this.mTouchData.CurrentTarget != this)
            {
                this.TouchEnd(this.mTouchData);
            }

            if (node == this.mTouchBegin.Target)
            {
                intercept = OutputResponseNode(node, TouchType.eTouchClick, mTouchData);
                if (!intercept)
                {
                    //
                    // handle click event.
                    //
                    node.TouchClick(this.mTouchData);
                }
            }
            else
            {
                if (this.mTouchBegin.Target != null && !this.mTouchBegin.Target.IsDispose)
                {
                    intercept = OutputResponseNode(this.mTouchBegin.Target, TouchType.eTouchOut, mTouchData);
                    if (!intercept)
                    {
                        this.mTouchBegin.Target.TouchOut(this.mTouchData);
                    }
                }
            }

            //
            // set touch state
            //
            this.mIsTouching = false;
            this.mIsMoving = false;
            this.mStartLongTouchCount = false;
            this.mHasSendLongTouch = false;
            this.mLastNode = null;
        }

        protected virtual void OnHandleLongTouch(DisplayNode node)
        {
            if (mIsMoving == true || mTouchBegin.Target != node || node.event_OnLongTouch == null || mStartLongTouchCount == true || mHasSendLongTouch == true)
            {
                return;
            }
            mLongTouchTime = 0;
            mStartLongTouchCount = true;
        }

        private bool IsMovingEnable()
        {
            mStartLongTouchCount = false;
            mHasSendLongTouch = false;
            mIsMoving = true;
            return mIsMoving;
        }


        public override void Update(float deltaTime)
        {
            if (mStartLongTouchCount == true)
            {
                if (mLongTouchTime >= mTouchBegin.Target.LongTouchTime)
                {
                    if (!OutputResponseNode(mTouchBegin.Target, TouchType.eTouchLong, mTouchData))
                    {
                        mTouchBegin.Target.LongTouch(this.mTouchData);
                    }

                    mLongTouchTime = 0;
                    mStartLongTouchCount = false;
                    mHasSendLongTouch = true;
                }
                mLongTouchTime += deltaTime;
            }
            base.Update(deltaTime);
        }

        public void SetTouchBeginTarget(DisplayNode node)
        {
            mTouchBegin.Target = node;
        }

        /// <summary>
        /// TextInput获得焦点后会OpenIME，如果点击Bounds以外的区域，需要CloseIME.
        /// </summary>
        private void TouchBeginDataCheck()
        {
            if (mTouchBegin != null && mTouchBegin.Target is ITextInput)
            {
                mTouchBegin.Target.TouchOut(null);
            }
        }

        public override Vector2? GetGlobalPoint()
        {
            return new Vector2(mHelpPoint1.X, mHelpPoint1.Y);
        }

        protected virtual bool OutputResponseNode(DisplayNode node, TouchType type, NodeTouch data)
        {
            return false;
        }

        public bool IsInMove()
        {
            return mIsMoving;
        }

        /// <summary>
        /// Stage是否处理该响应，返回true，代表没有UI响应.
        /// </summary>
        /// <param name="touchData"></param>
        /// <returns></returns>
        public bool IsHandlelByStage(TouchEvent touchData)
        {
            bool ret = false;

            DisplayNode node = this.PushEvent(touchData, true);
            if (node == null)
            {
                ret = true;
            }

            return ret;
        }
    }
}
