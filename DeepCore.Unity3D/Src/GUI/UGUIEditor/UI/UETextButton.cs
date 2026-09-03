using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UETextButton : UIComponent
    {
        //----------------------------------------------------------------------------------------------

        protected UGUI.DisplayText mTextSprite;

        protected UGUI.ImageSprite mImageTextUp;
        protected UGUI.ImageSprite mImageTextDown;
        protected Vector2 mImageTextOffset = new Vector2();
        protected ImageAnchor mImageTextAnchor = ImageAnchor.C_C;

        protected readonly bool mUseBitmapFont;

        public UETextButton(bool use_bitmap)
        {
            this.mUseBitmapFont = use_bitmap;
            base.Enable = true;
            base.EnableChildren = false;
            base.IsInteractive = true;
        }
        public UETextButton() : this(UIEditor.GlobalUseBitmapText)
        {
        }

        protected override void OnStart()
        {
            if (mTextSprite == null)
            {
                if (mUseBitmapFont)
                {
                    mTextSprite = new BitmapTextSprite("bitmap_text");
                }
                else
                {
                    mTextSprite = new TextSprite("text");
                }
            }
            base.OnStart();
        }

        //----------------------------------------------------------------------------------------------

        public UILayout LayoutDown { get; set; }
        /// <summary>
        /// 是否正在被按下
        /// </summary>
        public virtual bool IsPressDown { get { return base.IsPressed; } }

        public string Text { get; set; }
        public string TextDown { get; set; }
        public UnityEngine.Color FontColor { get; set; }
        public UnityEngine.Color FocuseFontColor { get; set; }

        public DisplayText TextSprite
        {
            get { return mTextSprite; }
        }

        public GUI.Data.TextAnchor EditTextAnchor
        {
            get { return mTextSprite.Anchor; }
            set { this.mTextSprite.Anchor = value; }
        }
        public int FontSize
        {
            get { return mTextSprite.FontSize; }
            set { mTextSprite.FontSize = value; }
        }
        public ImageAnchor ImageTextAnchor
        {
            get { return mImageTextAnchor; }
            set { this.mImageTextAnchor = value; }
        }
        public Vector2 ImageTextOffset
        {
            get { return mImageTextOffset; }
            set { this.mImageTextOffset = value; }
        }
        public Vector2 TextOffset
        {
            get { return mTextSprite.TextOffset; }
            set { mTextSprite.TextOffset = value; }
        }

        //----------------------------------------------------------------------------------------------

        protected override void OnUpdateLayout()
        {
            if (IsDispose) return;
            if (IsPressDown)
            {
                if (!string.IsNullOrEmpty(this.TextDown))
                    BindText(this.TextDown, this.FocuseFontColor);
                else
                    BindText(this.Text, this.FocuseFontColor);

                if (mImageTextDown != null)
                {
                    mImageTextDown.Visible = true;
                    UIUtils.AdjustAnchor(mImageTextAnchor, this, mImageTextDown, mImageTextOffset);
                }
                if (mImageTextUp != null)
                {
                    mImageTextUp.Visible = false;
                }
                if (LayoutDown != null)
                {
                    mCurrentLayout.SetCurrentLayout(LayoutDown);
                }
                else
                {
                    base.OnUpdateLayout();
                }
            }
            else
            {
                BindText(this.Text, this.FontColor);

                if (mImageTextUp != null)
                {
                    mImageTextUp.Visible = true;
                    UIUtils.AdjustAnchor(mImageTextAnchor, this, mImageTextUp, mImageTextOffset);
                }
                if (mImageTextDown != null)
                {
                    mImageTextDown.Visible = false;
                }
                base.OnUpdateLayout();
            }
        }

        protected void BindText(string text, Color color)
        {
            if (!string.IsNullOrEmpty(text))
            {
                mTextSprite.Visible = true;
                mTextSprite.Size2D = this.Size2D;
            }
            else
            {
                mTextSprite.Visible = false;
            }

            if (text != mTextSprite.Text)
            {
                mTextSprite.Text = text;
            }
            if (color != mTextSprite.FontColor)
            {
                mTextSprite.FontColor = color;
            }
        }

        protected override void DecodeFields(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e);
            this.LayoutDown = editor.CreateLayout((e as UEButtonMeta).layout_down);
            this.Decode_ImageText(editor, e as UEButtonMeta);
            {
                var ue = (e as UEButtonMeta);

                this.Text = ue.text;
                this.TextDown = ue.textDown;
                this.FontColor = UIUtils.UInt32_ARGB_To_Color(ue.unfocusTextColor);
                this.FocuseFontColor = UIUtils.UInt32_ARGB_To_Color(ue.focusTextColor);
                if (mUseBitmapFont)
                {
                    Decode_BitmapText(editor, ue);
                }
                else
                {
                    Decode_Text(editor, ue);
                }
                this.mTextSprite.TextOffset = new Vector2(ue.text_offset_x, ue.text_offset_y);
                this.mTextSprite.Size2D = this.Size2D;
            }
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.EnableChildren = false;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Text(UIEditor.Decoder editor, UEButtonMeta e)
        {
            var text = new UGUI.TextSprite("text");
            this.mTextSprite = text;
            this.AddChild(mTextSprite);

            text.Anchor = e.text_anchor;
            if (!string.IsNullOrEmpty(e.textFontName))
            {
                text.SetTextFont(editor.CreateFont(e.textFontName), e.textFontSize, (UnityEngine.FontStyle)e.textFontStyle);
            }
            else
            {
                text.SetTextFont(editor.CreateFont(null), e.textFontSize, UnityEngine.FontStyle.Normal);
            }
            if (e.textFontSize > 0)
            {
                text.FontSize = e.textFontSize;
            }
            text.Graphics.resizeTextForBestFit = false;
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                text.AddBorder(border_color, new Vector2(1, -1));
            }
        }


        private void Decode_BitmapText(UIEditor.Decoder editor, UEButtonMeta e)
        {
            var text = new BitmapTextSprite("bitmap_text");
            mTextSprite = text;
            AddChild(mTextSprite);

            text.Anchor = e.text_anchor;
            text.Style = e.textFontStyle;
            if (e.textFontSize > 0)
            {
                text.FontSize = e.textFontSize;
            }
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                text.Graphics.BorderTime = TextBorderCount.Border;
                text.Graphics.BorderColor = border_color;
            }
        }


        private void Decode_ImageText(UIEditor.Decoder editor, UEButtonMeta e)
        {
            if (!string.IsNullOrEmpty(e.imageAtlasUp))
            {
                this.mImageTextUp = editor.editor.ParseImageSpriteFromAtlas(e.imageAtlasUp, Vector2.zero);
                if (mImageTextUp != null)
                {
                    this.AddChild(mImageTextUp);
                }
            }
            else if (!string.IsNullOrEmpty(e.imageTextUp))
            {
                this.mImageTextUp = editor.editor.ParseImageSpriteFromImage(e.imageTextUp, Vector2.zero);
                if (mImageTextUp != null)
                {
                    this.AddChild(mImageTextUp);
                }
            }
            if (!string.IsNullOrEmpty(e.imageAtlasDown))
            {
                this.mImageTextDown = editor.editor.ParseImageSpriteFromAtlas(e.imageAtlasDown, Vector2.zero);
                if (mImageTextDown != null)
                {
                    this.AddChild(mImageTextDown);
                }
            }
            else if (!string.IsNullOrEmpty(e.imageTextDown))
            {
                this.mImageTextDown = editor.editor.ParseImageSpriteFromImage(e.imageTextDown, Vector2.zero);
                if (mImageTextDown != null)
                {
                    this.AddChild(mImageTextDown);
                }
            }
            this.mImageTextAnchor = e.imageAnchor;
            this.mImageTextOffset.x = e.imageOffsetX;
            this.mImageTextOffset.y = e.imageOffsetY;
        }
    }

    public class UEToggleButton : UETextButton
    {
        private bool m_IsChecked = false;

        public UEToggleButton(bool use_bitmap) : base(use_bitmap)
        {
        }
        public UEToggleButton() : this(UIEditor.GlobalUseBitmapText)
        {
        }


        public override bool IsPressDown { get { return m_IsChecked; } }
        public virtual bool IsChecked
        {
            get { return m_IsChecked; }
            set { m_IsChecked = value; }
        }

        protected override void OnPointerClick(PointerEventData e)
        {
            base.OnPointerClick(e);
            this.m_IsChecked = !m_IsChecked;
        }

        protected override void DecodeFields(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e);
            this.m_IsChecked = (e as UEToggleButtonMeta).isChecked;
        }

    }

}
