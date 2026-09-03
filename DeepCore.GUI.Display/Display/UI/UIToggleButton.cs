using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.Gemo;
using DeepCore.GUI.Sound;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DeepCore.GUI.Display.UI
{
    public class UIToggleButton : UITextButton
    {
        private bool mIsSelect = false;
        private LockState mCurLockState = LockState.eNone;
        private bool mTouchByClick = false;
        public bool TouchByClick
        {
            get
            {
                return mTouchByClick;
            }
            set
            {
                mTouchByClick = value;
            }
        }
        public enum LockState
        {
            eNone = 0,
            eLockSelect = 1,
            eLockUnSelect = 2

        }


        public UIToggleButton()
            : base()
        {
            this.Enable = true;
            this.EnableChildren = false;
            SetSound(SoundManager.GetInstance().GetDefaultBtnSound(), DisplayStage.TouchType.eTouchBegin);
        }

        protected override DisplayNode PushEvent(TouchEvent touchData, bool forTouch = true)
        {
            if (Enable == false) { return null; }
            if (forTouch && !HasVisibleArea()) { return null; }
            return HitTest(new Point2D(touchData.GlobalX, touchData.GlobalY));
        }

        protected override DisplayNode HitTest(Point2D globalPoint)
        {
            if (Enable == false || mSampleBtn.Bounds == null) { return null; }
            Point2D p = this.GlobalToLocal(globalPoint);
            if (mSampleBtn.Bounds.contains(p.x, p.y) == true) { return this; }
            return null;
        }


        private void Touched()
        {
            if (CheckLockState()) { return; }
            IsSelect = !mIsSelect;
            PlaySound(DisplayStage.TouchType.eTouchBegin);
            if (event_OnTouchBegin != null) { event_OnTouchBegin(this); }
        }
        public override void TouchBegin(NodeTouch touch)
        {
            if (!mTouchByClick)
            {
                Touched();
            }
        }

        public override void TouchEnd(NodeTouch touch)
        {
            //do nothing
        }

        public override void TouchClick(NodeTouch touch)
        {
            if (mTouchByClick)
            {
                Touched();
            }
        }

        public override void TouchOut(NodeTouch touch)
        {
            //do nothing
        }

        public override void TouchMove(NodeTouch touch)
        {
            //do nothing
        }

        public bool IsSelect
        {
            get { return mIsSelect; }
            set
            {
                mIsSelect = value;
                mSampleBtn.IsTouching = value;
            }
        }

        public void SetBtnLockState(LockState state)
        {
            mCurLockState = state;
        }

        private bool CheckLockState()
        {
            if (mCurLockState == LockState.eNone) { return false; }
            if (mCurLockState == LockState.eLockSelect && IsSelect == true) { return true; }
            if (mCurLockState == LockState.eLockUnSelect && IsSelect == false) { return true; }
            return false;
        }
        protected override void DecodeFields(UIEditor editor, Data.UIComponentMeta e)
        {
            //base.DecodeFromMeta(editor, e);

            this.X = e.X;
            this.Y = e.Y;
            this.Visible = e.Visible;
            this.EditName = e.EditorName;
            this.Name = string.Format("{0} - {1}", this.EditName, e.ClassName);

            this.UserData = e.UserData;
            this.UserTag = e.UserTag;

            this.Layout = editor.CreateLayout(e.Layout);



            if (e is Data.UEToggleButtonMeta)
            {
                Data.UEToggleButtonMeta meta = e as Data.UEToggleButtonMeta;

                UILayout down = null;
                UILayout upText = null;
                UILayout downText = null;
                UILayout disableLayout = null;

                if (meta.layout_down != null)
                {
                    down = editor.CreateLayout(meta.layout_down);
                }

                if (!string.IsNullOrEmpty(meta.imageTextUp))
                {
                    upText = editor.CreateLayoutByImg(meta.imageTextUp);
                }

                if (!string.IsNullOrEmpty(meta.imageTextDown))
                {
                    downText = editor.CreateLayoutByImg(meta.imageTextDown);
                }

                if (meta.DisableLayout != null)
                {
                    disableLayout = editor.CreateLayout(meta.DisableLayout);
                }

                SetButtonLayout(this.Layout, down, upText, downText);

                if (disableLayout != null)
                {
                    SetDisableLayout(disableLayout);
                }

                SetButtonBounds(new Gemo.Rectangle2D(0, 0, e.Width, e.Height));
                this.Layout = null;
                SetButtonText(meta.text);
                SetBorderColor(Color.toRGBA(meta.textBorderColor));
                if (meta.textBorderAlpha == 0)
                {
                    SetBorderTimes(0);
                }
                this.TextOffsetX = (int)meta.text_offset_x;
                this.TextOffsetY = (int)meta.text_offset_y;
                SetButtonTextAnchor(AnchorTool.FromTextAnchor(meta.text_anchor));
                SetButtonTextSize(meta.textFontSize);
                SetFontColor(Color.toRGBA(meta.unfocusTextColor));
                IsSelect = meta.isChecked;
            }

        }
    }
}

