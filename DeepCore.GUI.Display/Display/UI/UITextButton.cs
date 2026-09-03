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
    public class UITextButton : UIComponent
    {
        private Anchor mAnchor;
        private UITextField mTextField;
        protected UISimpleButton mSampleBtn;
        private string sound = null;
        private DisplayStage.TouchType soundTriggerType = DisplayStage.TouchType.eTouchClick;

        private int mTextOffsetX = 0;
        private int mTextOffsetY = 0;

        public UITextButton()
        {
            this.Enable = true;
            this.EnableChildren = true;

            mSampleBtn = new UISimpleButton();
            this.AddChild(mSampleBtn);

            sound = SoundManager.GetInstance().GetDefaultBtnSound();
        }

        public int TextOffsetX
        {
            set
            {
                mTextOffsetX = value;
                ResetTextField(mTextField, mSampleBtn.Bounds.width, mSampleBtn.Bounds.height);
            }
            get
            {
                return mTextOffsetX;
            }
        }

        public int TextOffsetY
        {
            set
            {
                mTextOffsetY = value;
                ResetTextField(mTextField, mSampleBtn.Bounds.width, mSampleBtn.Bounds.height);
            }
            get
            {
                return mTextOffsetY;
            }
        }

        private UITextField CreateTextField(string text, float fontSize, FontStyle style = FontStyle.STYLE_PLAIN)
        {
            return new UITextField(text, fontSize, style);
        }

        /// <summary>
        /// 设置按钮四个状态(上、下、文字图片上、文字图片下).
        /// </summary>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <param name="upText"></param>
        /// <param name="downText"></param>
        public virtual void SetButtonLayout(UILayout up, UILayout down, UILayout upText, UILayout downText)
        {
            mSampleBtn.SetState(up, down, upText, downText);
        }

        /// <summary>
        /// 设置按钮文字状态(文字图片上、文字图片下).
        /// </summary>
        /// <param name="upText"></param>
        /// <param name="downText"></param>
        public virtual void SetButtonTextLayout(UILayout upText, UILayout downText)
        {
            mSampleBtn.SetTextState(upText, downText);
        }

        public virtual void SetDisableLayout(UILayout layout)
        {
            mSampleBtn.SetDisableLayout(layout);
        }

        public UISimpleButton GetBaseButton()
        {
            return mSampleBtn;
        }

        public float TouchScale
        {
            get
            {
                return mSampleBtn.TouchScale;
            }
            set
            {
                mSampleBtn.TouchScale = value;
            }
        }

        public override float Width
        {
            get
            {
                return mSampleBtn.Bounds.width;
            }
        }

        public override float Height
        {
            get
            {
                return mSampleBtn.Bounds.height;
            }
        }

        public void SetButtonBounds(Rectangle2D rect)
        {
            mSampleBtn.Bounds.setBounds(rect);
            ResetTextField(mTextField, rect.width, rect.height);
        }

        public override Rectangle2D Bounds
        {
            get
            {
                return mSampleBtn.Bounds;
            }
            set
            {
                Bounds = mSampleBtn.Bounds;
            }
        }

        public override void SetSize(float w, float h)
        {
            mSampleBtn.Bounds.height = h;
            mSampleBtn.Bounds.width = w;
            ResetTextField(mTextField, w, h);
        }

        public void SetButtonText(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }

            if (mTextField == null)
            {
                mTextField = CreateTextField(text, UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
                if (mTextField != null)
                {
                    this.AddChild(mTextField);
                }
            }

            if (mTextField != null)
            {
                mTextField.Text = text;
            }
        }

        public string GetButtonText()
        {
            if (mTextField != null)
            {
                return mTextField.Text;
            }
            return null;
        }

        public void SetButtonTextSize(int size)
        {
            if (mTextField == null)
            {
                mTextField = CreateTextField("", size, UIEditor.Instance.DefaultFontStyle);
                if (mTextField != null)
                {
                    this.AddChild(mTextField);
                }
            }

            if (mTextField != null)
            {
                mTextField.SetFontSize(size);
            }

        }

        public void SetButtonTextAnchor(Anchor anchor)
        {
            mAnchor = anchor;

            if (mTextField == null)
            {
                mTextField = CreateTextField("", UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
                if (mTextField != null)
                {
                    this.AddChild(mTextField);
                }
            }

            if (mTextField != null)
            {
                mTextField.SetAnchor(anchor);
                ResetTextField(mTextField, mSampleBtn.Bounds.width, mSampleBtn.Bounds.height);
            }

        }

        public void SetFontColor(uint rgba)
        {
            if (mTextField != null)
            {
                mTextField.SetFontColor(rgba);
            }
        }

        public void SetBorderTimes(int times)
        {
            if (mTextField != null)
            {
                mTextField.SetBorderTimes(times);
            }
        }

        public void SetBorderColor(uint corlor)
        {
            if (mTextField != null)
            {
                mTextField.SetBorderColor(corlor);
            }
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
            if (base.PushEvent(touchData, true) != null)
            {
                return this;
            }
            return null;
        }

        public override void TouchBegin(NodeTouch touch)
        {
            if (IsDispose)
            {
                return;
            }

            PlaySound(DisplayStage.TouchType.eTouchBegin);
            mSampleBtn.TouchBegin(touch);
            mTextField.ScaleX = mSampleBtn.TouchScale;
            mTextField.ScaleY = mSampleBtn.TouchScale;
            base.TouchBegin(touch);
        }

        public override void TouchEnd(NodeTouch touch)
        {
            if (IsDispose)
            {
                return;
            }

            PlaySound(DisplayStage.TouchType.eTouchEnd);
            mSampleBtn.TouchEnd(touch);
            mTextField.ScaleX = 1;
            mTextField.ScaleY = 1;
            base.TouchEnd(touch);
        }

        public override void TouchOut(NodeTouch touch)
        {
            if (IsDispose)
            {
                return;
            }

            PlaySound(DisplayStage.TouchType.eTouchOut);
            mSampleBtn.TouchOut(touch);
            mTextField.ScaleX = 1;
            mTextField.ScaleY = 1;
            base.TouchOut(touch);
        }

        public override void TouchClick(NodeTouch touch)
        {
            PlaySound(DisplayStage.TouchType.eTouchClick);
            base.TouchClick(touch);
        }

        private void ResetTextField(UITextField o, float w, float h)
        {
            if (o == null)
            {
                return;
            }

            if ((mAnchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                o.X = Width * 0.5f;
            }
            else if ((mAnchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                o.X = Width;
            }
            else
            {
                o.X = 0;
            }
            if ((mAnchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                o.Y = Height * 0.5f;
            }
            else if ((mAnchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                o.Y = Height;
            }
            else
            {
                o.Y = 0;
            }

            o.X += TextOffsetX;
            o.Y += TextOffsetY;
        }

        public override void Draw(Graphics g)
        {
            // do Nothing...
        }

        protected override void Disposing()
        {
            if (mTextField != null)
            {
                mTextField.RemoveFromParent();
                mTextField.Dispose();
                mTextField = null;
            }

            if (mSampleBtn != null)
            {
                mSampleBtn.RemoveFromParent(false);
                mSampleBtn.Dispose();
                mSampleBtn = null;
            }

            base.Disposing();
        }

        public void SetSound(string name, DisplayStage.TouchType type = DisplayStage.TouchType.eTouchClick)
        {
            sound = name;
            soundTriggerType = type;
        }

        protected void PlaySound(DisplayStage.TouchType touchType)
        {
            if (sound == null)
            {
                return;
            }

            if (touchType == soundTriggerType)
            {
                SoundManager.GetInstance().PlaySound(sound);
            }
        }

        public override bool Disable
        {
            get
            {
                return mSampleBtn.Disable;
            }
            set
            {
                mSampleBtn.Disable = value;
            }
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



            if (e is Data.UETextButtonMeta)
            {
                Data.UETextButtonMeta meta = e as Data.UETextButtonMeta;

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
            }


        }
    }
}

