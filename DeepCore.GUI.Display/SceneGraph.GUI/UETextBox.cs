using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UETextBoxMeta))]
    public class UETextBox : UEDisplayNode<UETextBoxMeta>
    {
        public UITextLayer TextLayer { get; private set; }
        public Vector2 ScrollPosition { get; set; }
        public UETextBox(UIFactory editor, UETextBoxMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
        protected override void DoBindData(string key, object value)
        {
            Meta.Text = $"{value}";
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            this.TextLayer.Text = Meta.Text;
            this.TextLayer?.Render(args.Graphics, this.LocalBounds);
        }
    }

    [UEInstance(typeof(UERichTextBoxMeta))]
    public class UERichTextBox : UEDisplayNode<UERichTextBoxMeta>
    {
        public RichTextLayer TextLayer { get; private set; }
        public AttributedString Text { get; private set; }
        public Vector2 ScrollPosition { get; set; }
        public UERichTextBox(UIFactory editor, UERichTextBoxMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Text.ToString();
        }
        protected override void DoBindData(string key, object value)
        {
            this.Text = Editor.DecodeAttributedString(value.ToString());
            this.TextLayer.SetString(this.Text);
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer =  Editor.CreateRichTextLayer(Meta.Width - Meta.Padding.L - Meta.Padding.R);
            this.AutoRelease(this.TextLayer);
            this.Text = Editor.DecodeAttributedString(Meta.XmlText);
            this.TextLayer.SetString(this.Text);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            var bounds = Meta.Padding.Cut(this.LocalBounds);
            this.TextLayer?.SetWidth(bounds.W);
            this.TextLayer?.Render(args.Graphics, bounds);
        }
    }
}
