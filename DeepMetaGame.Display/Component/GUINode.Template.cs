using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.SceneGraph;
using DeepMetaGame.Data;
using DeepMetaGame.Data.GUI.Meta;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Display.GUI
{
    [UEInstance(typeof(UETemplateDataBoxMeta))]
    public class UETemplateDataBox : UEDisplayNode<UETemplateDataBoxMeta>
    {
        public UIImageLayer ImageLayer { get; private set; }
        public UITextLayer TitleLayer { get; private set; }
        public UITextLayer TextLayer { get; private set; }
        public RichTextLayer RichTextLayer { get; private set; }
        public AttributedString RichText { get; private set; }

        public UETemplateDataBox(UIFactory editor, UETemplateDataBoxMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Title;
        }
        protected override void DoBindData(string key, object value)
        {
            if (value is TemplateData template)
            {
                this.TitleLayer.Text = template.Name;
                var icon = Editor.AddImage(template.IconName);
                if (icon != null)
                {
                    this.ImageLayer.SetImage(icon);
                }
                if (this.RichTextLayer != null)
                {
                    this.RichText = Editor.DecodeAttributedString($"{template.Comment}");
                    this.RichTextLayer.SetString(this.RichText);
                }
                if (this.TextLayer != null)
                {
                    this.TextLayer.Text = (template.Comment);
                }
            }
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            //if (Meta.ImageAtlas != null)
            {
                this.ImageLayer = Editor.CreateImageLayer(Meta.ImageAtlas, Meta.ImageStyle);
                this.AutoRelease(this.ImageLayer);
            }
            //if (!string.IsNullOrEmpty(Meta.Title))
            {
                this.TitleLayer = Editor.CreateTextLayer(Meta.Title, Meta.Font, Meta.TitleStyle);
                this.TitleLayer.Text = Meta.Title;
                this.AutoRelease(this.TitleLayer);
            }
            if (!string.IsNullOrEmpty(Meta.Text))
            {
                this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
                this.TextLayer.Text = Meta.Text;
                this.AutoRelease(this.TextLayer);
            }
            if (!string.IsNullOrEmpty(Meta.XmlText))
            {
                this.RichTextLayer = Editor.CreateRichTextLayer(Meta.Width - Meta.TextStyle.Padding.L - Meta.TextStyle.Padding.R);
                this.RichText = Editor.DecodeAttributedString(Meta.XmlText);
                this.RichTextLayer.SetString(this.RichText);
                this.AutoRelease(this.RichTextLayer);
            }
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawIcon(args);
            DrawText(args);
            DrawTitle(args);
        }
        protected virtual void DrawTitle(GraphicsArgs args)
        {
            var bounds = Rect.Bounds;
            if (TitleLayer != null)
            {
                this.TitleLayer?.Render(args.Graphics, bounds);
            }
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            var bounds = Rect.Bounds;
            if (TextLayer != null)
            {
                this.TextLayer?.Render(args.Graphics, bounds);
            }
            if (RichTextLayer != null)
            {
                this.RichTextLayer?.SetWidth(bounds.W);
                this.RichTextLayer?.Render(args.Graphics, bounds);
            }
        }
        protected virtual void DrawIcon(GraphicsArgs args)
        {
            if (ImageLayer != null)
            {
                this.ImageLayer?.Render(args.Graphics, this.LocalBounds);
            }
        }
    }
}
