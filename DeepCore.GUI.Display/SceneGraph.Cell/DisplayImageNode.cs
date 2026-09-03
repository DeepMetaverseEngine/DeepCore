using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.Cell
{
    public class DisplayImageNode : DisplayNode
    {
        public RectInteracviteComponent Rect { get; }
        private Image src;
        public Image Image
        {
            get => src;
            set
            {
                this.src = value;
                if (src != null)
                {
                    Rect.Bounds = new Geometry.RectangleF(0, 0, src.Width, src.Height);
                }
            }
        }
        public DisplayImageNode()
        {
            this.Rect = base.Components.AddComponent<RectInteracviteComponent>();
        }
        public DisplayImageNode(Image image) : this()
        {
            this.Image = image;
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            if (src != null)
            {
                args.Graphics.BeginImage(src);
                args.Graphics.DrawImageZoom(Rect.Bounds);
            }
        }
    }
}
