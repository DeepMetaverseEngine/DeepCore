using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public abstract partial class UETextBoxBase : UIComponent
    {
        protected readonly RichTextBox mTextSprite;

        protected UETextBoxBase(bool use_bitmap)
        {
            this.mTextSprite = new RichTextBox("text", use_bitmap);
            this.AddChild(mTextSprite);
            this.Scrollable = false;
        }

        //----------------------------------------------------------------------

        public string Text
        {
            get { return mTextSprite.Text; }
            set
            {
                if (IsDispose) return;
                mTextSprite.Text = value;
            }
        }
        public int FontSize
        {
            get { return mTextSprite.FontSize; }
            set { mTextSprite.FontSize = value; }
        }
        public UnityEngine.Color FontColor
        {
            get { return mTextSprite.FontColor; }
            set { mTextSprite.FontColor = value; }
        }
        public ITextComponent TextComponent
        {
            get { return mTextSprite; }
        }
        public ScrollRectInteractive Scroll
        {
            get { return mTextSprite.Scroll; }
        }

        public DisplayNode Container
        {
            get { return Scroll.Container; }
        }


        public bool Scrollable
        {
            get { return mTextSprite.Scrollable; }
            set { this.mTextSprite.Scrollable = value; }
        }
        public GUI.Display.Text.AttributedString AText
        {
            get { return mTextSprite.AText; }
            set { mTextSprite.AText = value; }
        }
        public string XmlText
        {
            set { mTextSprite.RichTextLayer.XmlText = value; }
        }
        public string UnityRichText
        {
            set { this.XmlText = UIUtils.UnityRichTextToXmlText(value); }
        }
        //----------------------------------------------------------------------


        private void ResetSize()
        {
            Vector2 bsize = this.Size2D;
            if (this.Layout != null)
            {
                mTextSprite.Position2D = new Vector2(
                    Layout.ClipL,
                    Layout.ClipT);
                mTextSprite.Size2D = new Vector2(
                    bsize.x - Layout.ClipR,
                    bsize.y - Layout.ClipB);
            }
            else
            {
                mTextSprite.Size2D = bsize;
            }
            this.EnableChildren = mTextSprite.IsNeedScroll;
        }
        protected override void OnSizeChanged(Vector2 size)
        {
            base.OnSizeChanged(size);
            this.ResetSize();
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            this.ResetSize();
        }
        protected override void DecodeBegin(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeBegin(editor, e);
        }
        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.Decode_Text(editor, e as UETextBoxBaseMeta);
            this.Enable = false;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Text(UIEditor.Decoder editor, UETextBoxBaseMeta e)
        {
            this.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            if (e.text_size > 0)
            {
                this.FontSize = e.text_size;
            }
            if (e.text_shadow_alpha > 0)
            {
                Color shadow_color = UIUtils.UInt32_ARGB_To_Color(e.text_shadow_dcolor);
                shadow_color.a = e.text_shadow_alpha;
                mTextSprite.SetShadow(shadow_color, new Vector2(e.text_shadow_x, e.text_shadow_y));
            }
        }


    }
    //----------------------------------------------------------------------

    public class UETextBox : UETextBoxBase
    {
        public UETextBox(bool use_bitmap) : base(use_bitmap)
        {
        }
        public UETextBox() : this(UIEditor.GlobalUseBitmapText)
        {
        }
        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.Text = (e as UETextBoxMeta).Text;
        }
    }

    public class UETextBoxHtml : UETextBoxBase
    {
        public UETextBoxHtml(bool use_bitmap) : base(use_bitmap)
        {
        }
        public UETextBoxHtml() : this(UIEditor.GlobalUseBitmapText)
        {
        }
        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            var xmltext = (e as UETextBoxHtmlMeta).HtmlText;
            if (!string.IsNullOrEmpty(xmltext))
            {
                this.UnityRichText = xmltext;
            }
        }
    }

}
