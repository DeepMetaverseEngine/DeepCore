using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Action;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.Gemo;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DeepCore.GUI.Display.UI
{
    public class UIRichTextField : DisplayNode
    {
        private RichTextLayer mRichTextLayer = null;

        public UIRichTextField()
            : base("UIRichTextField")
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.Bounds = new Gemo.Rectangle2D(0, 0, 1, 1);
            mRichTextLayer = UIFactory.Instance.CreateRichTextLayer();
        }

        public RichTextLayer RichTextLayer
        {
            get { return mRichTextLayer; }
        }
        
        protected override void Disposing()
        {
            if (mRichTextLayer != null)
            {
                mRichTextLayer.Dispose();
                mRichTextLayer = null;
            }
            base.Disposing();
        }
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (mRichTextLayer != null)
            {
                mRichTextLayer.IsEnable = !this.Disable;
                mRichTextLayer.Render(g, 0, 0);
            }
        }

        public override Gemo.Rectangle2D Bounds
        {
            get
            {
                base.Bounds.width = mRichTextLayer.ContentWidth;
                base.Bounds.height = mRichTextLayer.ContentHeight;
                return base.Bounds;
            }
            set
            {
                base.Bounds = value;
            }
        }
    }
}
