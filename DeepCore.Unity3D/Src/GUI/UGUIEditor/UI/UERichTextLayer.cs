using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Text;
using DeepCore.Unity3D.UGUI;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public abstract partial class BaseUERichTextBox : UIComponent
    {
        protected BaseUERichTextBox()
        {
        }

        public string XmlText
        {
            set { RichTextLayer.XmlText = value; }
        }
        public string UnityRichText
        {
            set { this.XmlText = UIUtils.UnityRichTextToXmlText(value); }
        }

        public abstract AttributedString AText { get; set; }

        public abstract UGUIRichTextLayer RichTextLayer { get; }

        protected override void DecodeBegin(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeBegin(editor, e);
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.OnUpdateLayout();
            this.Decode_Text(editor, e as UETextBoxHtmlMeta);
            this.Enable = false;
            this.EnableChildren = true;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Text(UIEditor.Decoder editor, UETextBoxHtmlMeta e)
        {
            this.RichTextLayer.DefaultTextAttribute = new TextAttribute(
               GUI.Display.Color.toRGBA(e.textColor),
               e.text_size,
               editor.editor.DefaultFont.name,
               GUI.Display.FontStyle.STYLE_PLAIN,
               RichTextAlignment.taNA,
               UIUtils.ToTextShadowCount(new Vector2(e.text_shadow_x, e.text_shadow_y)),
               GUI.Display.Color.toRGBA(e.text_shadow_dcolor, (int)(e.text_shadow_alpha * 255))
               );
            if (!string.IsNullOrEmpty(e.HtmlText))
            {
                this.UnityRichText = e.HtmlText;
            }
        }
    }

    public class UERichTextBox : BaseUERichTextBox
    {
        protected readonly RichTextBox mRichTextBox;

        public UERichTextBox(bool use_bitmap)
        {
            this.mRichTextBox = new RichTextBox("rich_text", use_bitmap);
            this.AddChild(mRichTextBox);
        }
        public UERichTextBox() : this(UIEditor.GlobalUseBitmapText) { }

        public override AttributedString AText
        {
            get { return mRichTextBox.AText; }
            set
            {
                if (IsDispose) return;
                mRichTextBox.AText = value;
            }
        }
        public override UGUIRichTextLayer RichTextLayer
        {
            get { return mRichTextBox.RichTextLayer; }
        }
        public RichTextBox TextBox
        {
            get { return mRichTextBox; }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Vector2 bsize = this.Size2D;
            if (this.Layout != null)
            {
                mRichTextBox.Position2D = new Vector2(
                    Layout.ClipL,
                    Layout.ClipT);
                mRichTextBox.Size2D = new Vector2(
                    bsize.x - Layout.ClipR,
                    bsize.y - Layout.ClipB);
            }
            else
            {
                mRichTextBox.Size2D = bsize;
            }
        }
    }

    public class UERichTextPan : BaseUERichTextBox
    {
        protected readonly RichTextPan mRichTextBox;

        public UERichTextPan(bool use_bitmap)
        {
            this.mRichTextBox = new RichTextPan(use_bitmap, "rich_text");
            this.AddChild(mRichTextBox);
        }
        public UERichTextPan() : this(UIEditor.GlobalUseBitmapText) { }

        public override AttributedString AText
        {
            get { return mRichTextBox.AText; }
            set { mRichTextBox.AText = value; }
        }
        public override UGUIRichTextLayer RichTextLayer
        {
            get { return mRichTextBox.RichTextLayer; }
        }
        public RichTextPan TextPan
        {
            get { return mRichTextBox; }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Vector2 bsize = this.Size2D;
            if (this.Layout != null)
            {
                mRichTextBox.Position2D = new Vector2(
                    Layout.ClipL,
                    Layout.ClipT);
                mRichTextBox.Size2D = new Vector2(
                    bsize.x - Layout.ClipR,
                    bsize.y - Layout.ClipB);
            }
            else
            {
                mRichTextBox.Size2D = bsize;
            }
        }
    }
}