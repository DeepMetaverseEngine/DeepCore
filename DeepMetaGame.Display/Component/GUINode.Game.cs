using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.SceneGraph;
using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepMetaGame.Display.Component
{

    //-----------------------------------------------------------------------
    [Desc("Card模板显示框", "[GAME]")]
    public class CardPanelBoxMeta : UEComponentMeta
    {
        [Desc("图标", "Icon")]
        public CPJAtlasMeta ImageAtlas;
        [Desc("图标样式", "Icon")]
        public UIImageStyleMeta ImageStyle;

        [Desc("字体", "文本")]
        public UIFontMeta Font = new UIFontMeta();

        [Desc("文本", "文本-标题")]
        public string Title = "文本-标题";
        [Desc("文本样式", "文本-标题")]
        public UITextStyleMeta TitleStyle = new UITextStyleMeta();

        [Desc("文本", "文本-介绍")]
        public string Text = "文本-介绍";
        [Desc("富文本", "文本-介绍")]
        [RichText]
        public string XmlText;
        [Desc("文本样式", "文本-介绍")]
        public UITextStyleMeta TextStyle = new UITextStyleMeta();

        public CardPanelBoxMeta()
        {
            this.Size = new DeepCore.Geometry.Vector2(300, 600);
            this.ImageStyle = new UIImageStyleMeta()
            {
                Align = AlignmentStyle.MiddleCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
            this.TitleStyle = new UITextStyleMeta()
            {
                Align = AlignmentStyle.TopCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
            this.TextStyle = new UITextStyleMeta()
            {
                Align = AlignmentStyle.BottomCenter,
                Padding = new DeepCore.Geometry.Padding(8, 8, 8, 8),
            };
        }
        public override string GetStringValue() { return Title; }
    }

    [UEInstance(typeof(CardPanelBoxMeta))]
    public class CardPanelBox : UEDisplayNode<CardPanelBoxMeta>
    {
        public UIImageLayer ImageLayer { get; private set; }
        public UITextLayer TitleLayer { get; private set; }
        public UITextLayer TextLayer { get; private set; }
        public RichTextLayer RichTextLayer { get; private set; }
        public AttributedString RichText { get; private set; }

        public CardPanelBox(UIFactory editor, CardPanelBoxMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Title;
        }
        protected override void DoBindData(string key, object value)
        {
            var cardID = DeepConvert.ConvertTo<int>(value);
            if (ZumaDataCenter.Instance.CardTable.TryGetValue(cardID, out var card))
            {
                this.TitleLayer.Text = card.Name;
                var icon = Editor.AddImage(card.Icon);
                if (icon != null)
                {
                    this.ImageLayer.SetImage(icon);
                }
                if (this.RichTextLayer != null)
                {
                    this.RichText = Editor.DecodeAttributedString($"{card.Desc}");
                    this.RichTextLayer.SetString(this.RichText);
                }
                if (this.TextLayer != null)
                {
                    this.TextLayer.Text = (card.Desc);
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
    //-----------------------------------------------------------------------
}
