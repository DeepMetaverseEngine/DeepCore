using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.UI
{
    public class UISimpleButton : DisplayNode
    {
        private UILayout mUpUILayout       = null;
        private UILayout mDownUILayout     = null;

        private UILayout mUpTextUILayout   = null;
        private UILayout mDownTextUILayout = null;

        private Point2D mTouchPoint        = new Point2D();
        private bool    mIsTouching      = false;

        private float mScaleOffsetX = 0;
        private float mScaleOffsetY = 0;
        private float mTouchScale = 1.0f;

        private bool mIsDisable = false;
        private UILayout mDisableLayout = null;
        public UISimpleButton(  UILayout up = null, 
                                UILayout down = null, 
                                UILayout upText = null, 
                                UILayout downText = null)
            : base("UISimpleButton")
        {
            this.Enable = true;
            this.EnableChildren = false;
            Bounds = new Rectangle2D(0, 0, 1, 1);
            SetState(up, down, upText, downText);
        }

        public override bool Disable
        {
            get
            {
                if(mDisableLayout != null)
                {
                    return mIsDisable;
                }

                return base.Disable;
            }
            set
            {
                if(mDisableLayout != null)
                {
                    mIsDisable = value;
                }
                else
                {
                    base.Disable = value;
                }
            }
        }


        public void SetState(UILayout up = null, UILayout down = null, UILayout upText = null, UILayout downText = null)
        {
            mUpUILayout = up;
            mDownUILayout = down;

            mUpTextUILayout = upText;
            mDownTextUILayout = downText;
        }

        public void SetTextState(UILayout upText, UILayout downText)
        {
            mUpTextUILayout = upText;
            mDownTextUILayout = downText;
        }

        public void SetDisableLayout(UILayout layout)
        {
            mDisableLayout = layout;
        }

        public void SetBounds(Rectangle2D rect)
        {
            if(Bounds != null)
            {
                Bounds.setBounds(rect);
            }
            else
            {
                Bounds = new Rectangle2D(rect);
            }
            CalTouchScaleOffset();
        }

        public float TouchScale
        {
            get
            {
                return mTouchScale;
            }
            set
            {
                mTouchScale = value;
                CalTouchScaleOffset();
            }
        }

        public bool IsTouching
        {
            get
            {
                return mIsTouching;
            }
            set
            {
                mIsTouching = value;
            }
        }

        public void SetHeight(float h)
        {
            if(Bounds != null)
            {
                Bounds.height = h;
            }
            CalTouchScaleOffset();
        }

        public void SetWitdh(float w)
        {
            if(Bounds != null)
            {
                Bounds.width = w;
            }

            CalTouchScaleOffset();
        }

        public override DisplayNode Clone()
        {

            UILayout up       = null;
            UILayout down     = null;
            UILayout upText   = null;
            UILayout downText = null;
            UILayout disableLayout = null;

            if(mUpUILayout != null)
            {
                up = mUpUILayout.Clone();
            }
            if(mDownUILayout != null)
            {
                down = mDownUILayout.Clone();
            }
            if(mUpTextUILayout != null)
            {
                upText = mUpTextUILayout.Clone();
            }
            if(mDownTextUILayout != null)
            {
                downText = mDownTextUILayout.Clone();
            }
            if(mDisableLayout != null)
            {
                disableLayout = mDisableLayout.Clone();
            }


            UISimpleButton button = new UISimpleButton();
            button.SetState(up, down, upText, downText);
            button.SetBounds(Bounds.Clone());

            return button;
        }
        protected override void Disposing()
        {
            mTouchPoint = null;

            if (mUpUILayout != null)
            {
                mUpUILayout.Dispose();
                mUpUILayout = null;
            }

            if (mDownUILayout != null)
            {
                mDownUILayout.Dispose();
                mDownUILayout = null;
            }

            if (mUpTextUILayout != null)
            {
                mUpTextUILayout.Dispose();
                mUpTextUILayout = null;
            }

            if (mDownTextUILayout != null)
            {
                mDownTextUILayout.Dispose();
                mDownTextUILayout = null;
            }

            if (mDisableLayout != null)
            {
                mDisableLayout.Dispose();
                mDisableLayout = null;
            }

            base.Disposing();
        }

        public override void TouchBegin(NodeTouch touch)
        {
            this.mIsTouching = true;
            base.TouchBegin(touch);
        }


        public override void TouchMove(NodeTouch touch)
        {
            base.TouchMove(touch);
        }


        public override void TouchEnd(NodeTouch touch)
        {
            this.mIsTouching = false;
            base.TouchEnd(touch);
        }

        public override void TouchOut(NodeTouch touch)
        {
            this.mIsTouching = false;
            base.TouchOut(touch);
        }

        public override void Draw(Display.Graphics g)
        {
            if(mIsDisable && mDisableLayout != null)
            {
                this.mDisableLayout.Render(g
                   , this.Bounds.width
                   , this.Bounds.height);
                return;
            }

            if(this.mIsTouching)
            {

                if(mTouchScale != 1)
                {
                    CalTouchScaleOffset();
                    g.PushTransform();
                    g.Translate(mScaleOffsetX, mScaleOffsetY);
                    g.Scale(mTouchScale, mTouchScale);
                }

                if(this.mDownUILayout != null)
                {
                    this.mDownUILayout.Render(g
                        , this.Bounds.width
                        , this.Bounds.height);
                }

                if(this.mDownTextUILayout != null)
                {
                    this.mDownTextUILayout.Render(g
                        , this.Bounds.width
                        , this.Bounds.height);
                }

                if(mTouchScale != 1)
                {
                    g.PopTransform();
                }
            }
            else
            {
                if(this.mUpUILayout != null)
                {
                    this.mUpUILayout.Render(g
                        , this.Bounds.width
                        , this.Bounds.height);
                }

                if(this.mUpTextUILayout != null)
                {
                    this.mUpTextUILayout.Render(g
                        , this.Bounds.width
                        , this.Bounds.height);
                }
            }
        }

        private void CalTouchScaleOffset()
        {
            if(mTouchScale == 1)
            {
                return;
            }

            if(this.Bounds == null)
            {
                throw new Exception("UISampleButton Bounds null");
            }

            float w = this.Bounds.width * mTouchScale;
            float h = this.Bounds.height * mTouchScale;

            mScaleOffsetX = (this.Bounds.width - w) * 0.5f;
            mScaleOffsetY = (this.Bounds.height - h) * 0.5f;
        }

        protected override DisplayNode PushEvent(TouchEvent touchData, bool forTouch = true)
        {
            return HitTest(new Point2D(touchData.GlobalX, touchData.GlobalY));
        }
    }
}
