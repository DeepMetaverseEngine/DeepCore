using DeepCore.Geometry;
using DeepCore.GUI.Data;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public class UITextLayer : UIDisplayable
    {
        public string Text { get; set; }
        public UIFontMeta Font { get; }
        public UITextStyleMeta Style { get; }

        public UITextLayer(UIFactory editor, string text, UIFontMeta font, UITextStyleMeta meta):base(editor)
        {
            this.Text = text;
            this.Font = font;
            this.Style = meta;
            //GraphicsDriver.Instance.CreateTextLayer(text,font.FontName,font.Size, font.Style);
        }
        protected override void Disposing()
        {

        }
        public override void Render(Graphics g, RectangleF bounds)
        {
            if (Font != null)
            {
                g.SetFontSize(Font.Size);
            }
            if (Style != null)
            {
                bounds = Style.Padding.Cut(bounds);
                g.DrawStringBounds(this.Text, Style.TextColor, Style.BorderColor, Style.BorderStyle, Style.Align, bounds);
            }
            else
            {
                g.DrawString(this.Text, in bounds, AlignmentStyle.MiddleCenter);
            }
        }
    }
}
