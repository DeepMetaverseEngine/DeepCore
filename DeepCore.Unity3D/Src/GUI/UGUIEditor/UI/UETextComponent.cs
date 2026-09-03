using DeepCore.GUI.Data;
using DeepCore.Reflection;
using DeepCore.Unity3D.UGUI;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public abstract partial class UETextComponent : UIComponent
    {
        protected DisplayText mTextSprite;
        protected ITextComponent mTextGraphics;
        protected readonly bool mUseBitmapFont;

        protected UETextComponent(bool use_bitmap)
        {
            this.mUseBitmapFont = use_bitmap;
        }

        public DisplayText TextSprite
        {
            get { return mTextSprite; }
        }
        public ITextComponent TextGraphics
        {
            get { return mTextGraphics; }
        }
        public virtual bool IsNoLayout
        {
            get
            {
                if (MetaData != null)
                {
                    if (MetaData.Layout == null || MetaData.Layout.Style == UILayoutStyle.NULL)
                    {
                        return true;
                    }
                    return false;
                }
                return Layout != null;
            }
        }

        //-------------------------------------------------------------------------------------------------------

        public string Text
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.Text;
                else
                    return mTextSprite.Text;
            }
            set
            {
                if (IsDispose) return;
                if (value != this.Text)
                {
                    if (mTextGraphics != null)
                        mTextGraphics.Text = value;
                    else
                        mTextSprite.Text = value;
                    DoTextChanged();
                }
            }
        }
        public GUI.Data.TextAnchor EditTextAnchor
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.Anchor;
                else
                    return mTextSprite.Anchor;
            }
            set
            {
                if (mTextGraphics != null)
                    mTextGraphics.Anchor = value;
                else
                    mTextSprite.Anchor = value;
            }
        }
        public int FontSize
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.FontSize;
                else
                    return mTextSprite.FontSize;
            }
            set
            {
                if (mTextGraphics != null)
                    mTextGraphics.FontSize = value;
                else
                    mTextSprite.FontSize = value;
            }
        }
        public UnityEngine.Color FontColor
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.FontColor;
                else
                    return mTextSprite.FontColor;
            }
            set
            {
                if (mTextGraphics != null)
                    mTextGraphics.FontColor = value;
                else
                    mTextSprite.FontColor = value;
            }
        }
        public Vector2 TextOffset
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.TextOffset;
                else
                    return mTextSprite.TextOffset;
            }
            set
            {
                if (mTextGraphics != null)
                    mTextGraphics.TextOffset = value;
                else
                    mTextSprite.TextOffset = value;
            }
        }

        public Vector2 PreferredSize
        {
            get
            {
                if (mTextGraphics != null)
                    return mTextGraphics.PreferredSize;
                else
                    return mTextSprite.PreferredSize;
            }
        }

        public void SetBorder(UnityEngine.Color bc, Vector2 distance)
        {
            if (mTextGraphics != null)
                mTextGraphics.SetBorder(bc, distance);
            else
                mTextSprite.SetBorder(bc, distance);
        }

        //-------------------------------------------------------------------------------------------------------

        protected override UILayoutGraphics GenLayoutGraphics()
        {
            if (IsNoLayout) return null;
            return base.GenLayoutGraphics();
        }
        protected override void OnStart()
        {
            base.OnStart();
            this.DoTextChanged();
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (mTextSprite != null)
            {
                mTextSprite.Size2D = this.Size2D;
            }
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            var ue = e as UETextComponentMeta;

            if (IsNoLayout)
            {
                //无底图//
                if (!string.IsNullOrEmpty(ue.ImageFont))
                {
                    this.Decode_ImageFontGraphics(editor, ue);
                }
                else if (mUseBitmapFont)
                {
                    this.Decode_BitmapTextGraphics(editor, ue);
                }
                else
                {
                    this.Decode_TextGraphics(editor, ue);
                }
                this.mTextGraphics.Text = ue.text;
                this.mTextGraphics.TextOffset = new Vector2(ue.text_offset_x, ue.text_offset_y);
                this.mTextGraphics.Anchor = ue.text_anchor;
                this.mTextGraphics.FontColor = UIUtils.UInt32_ARGB_To_Color(ue.textColor);
            }
            else
            {
                //有底图//
                if (!string.IsNullOrEmpty(ue.ImageFont))
                {
                    this.Decode_ImageFont(editor, ue);
                }
                else if (mUseBitmapFont)
                {
                    this.Decode_BitmapText(editor, ue);
                }
                else
                {
                    this.Decode_Text(editor, ue);
                }
                this.mTextSprite.Text = ue.text;
                this.mTextSprite.TextOffset = new Vector2(ue.text_offset_x, ue.text_offset_y);
                this.mTextSprite.Anchor = ue.text_anchor;
                this.mTextSprite.FontColor = UIUtils.UInt32_ARGB_To_Color(ue.textColor);
                this.mTextSprite.Size2D = this.Size2D;
            }
            this.Enable = false;
            this.EnableChildren = false;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Text(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new TextSprite("text");
            mTextSprite = text;
            AddChild(mTextSprite);

            text.Graphics.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.Graphics.verticalOverflow = VerticalWrapMode.Overflow;

            if (e.textFontSize > 0)
            {
                text.FontSize = e.textFontSize;
                text.Graphics.resizeTextForBestFit = false;
            }
            else
            {
                text.Graphics.resizeTextForBestFit = true;
            }
            if (!string.IsNullOrEmpty(e.textFontName))
            {
                text.SetTextFont(editor.CreateFont(e.textFontName), this.FontSize, (UnityEngine.FontStyle)e.textFontStyle);
            }
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                text.AddBorder(border_color, new Vector2(1, -1));
            }
        }

        private void Decode_ImageFont(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new ImageFontSprite("image_font");
            mTextSprite = text;
            AddChild(mTextSprite);

            editor.editor.ParseImageFont(e.ImageFont, e.text, text.Graphics);
        }

        private void Decode_BitmapText(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new BitmapTextSprite("bitmap_text");
            mTextSprite = text;
            AddChild(mTextSprite);

            text.Anchor = e.text_anchor;
            text.Style = e.textFontStyle;
            text.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
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

        private void Decode_TextGraphics(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var gfx = base.mGameObject.AddComponent<TextGraphics>().DefaultInit();
            this.mTextGraphics = gfx;

            if (e.textFontSize > 0)
            {
                gfx.FontSize = e.textFontSize;
                gfx.resizeTextForBestFit = false;
            }
            else
            {
                gfx.resizeTextForBestFit = true;
            }
            if (!string.IsNullOrEmpty(e.textFontName))
            {
                gfx.SetTextFont(editor.CreateFont(e.textFontName), this.FontSize, (UnityEngine.FontStyle)e.textFontStyle);
            }
            gfx.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                gfx.AddBorder(border_color, new Vector2(1, -1));
            }
        }
        private void Decode_BitmapTextGraphics(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var gfx = mGameObject.AddComponent<BitmapTextGraphics>();
            this.mTextGraphics = gfx;

            gfx.Style = e.textFontStyle;
            if (e.textFontSize > 0)
            {
                gfx.FontSize = e.textFontSize;
            }
            gfx.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                gfx.BorderTime = TextBorderCount.Border;
                gfx.BorderColor = border_color;
            }
        }

        private void Decode_ImageFontGraphics(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var gfx = base.mGameObject.AddComponent<ImageFontGraphics>();
            this.mTextGraphics = gfx;

            editor.editor.ParseImageFont(e.ImageFont, this.Text, gfx);
        }

        //-----------------------------------------------------------------------------------------------
        protected override void OnDisposeEvents()
        {
            base.OnDisposeEvents();
            event_TextChanged = null;
        }
        private void DoTextChanged()
        {
            if (event_TextChanged != null)
            {
                event_TextChanged.Invoke(this, Text);
            }
        }
        public delegate void TextChangedHandler(UETextComponent sender, string value);

        [Desc("值改变")]
        public TextChangedHandler event_TextChanged;

        [Desc("值改变")]
        public event TextChangedHandler TextChanged { add { event_TextChanged += value; } remove { event_TextChanged -= value; } }

    }






}
