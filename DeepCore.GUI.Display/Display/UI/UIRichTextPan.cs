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
    //Example:
    //    XmlDocument xml = XmlUtil.LoadXML("/Resources/res/RichText.xml");
    //    AttributedString atext = AttributedString.CreateFromXML(xml);
    //Init TextPan
    //    UIRichTextPan richTextPan = new UIRichTextPan();
    //    richTextPan.EnableScrollV = true;
    //    richTextPan.EnableScrollH = false;
    //    richTextPan.setSize(500, 200);
    //    richTextPan.ScrollRect = richTextPan.Bounds;
    //Init RichTextLayer
    //    RichTextLayer richTextLayer = new RichTextLayer(500, RichTextAnchor.taLEFT);
    //    richTextLayer.SetBorder(TextLayer.BORDER_8, 0x000000ff);
    //    richTextLayer.SetString(atext);
    //SetLayer
    //    richTextPan.SetRichTextLayer(richTextLayer);
    /// <summary>
    /// 富文本专用滚动控件,优化渲染时的drawCall,显示区域以外的内容将不会绘制.
    /// </summary>
    public class UIRichTextScrollPan : UIScrollBase
    {
        private TextContent mContent;

        public UIRichTextScrollPan()
        {
            mContent = new TextContent();
            base.AddChild(mContent);
        }

        public TextContent GetTextContent()
        {
            return mContent;
        }

        public override void AddChild(DisplayNode child)
        {
            throw new Exception("UIRichTextPan do not support this API.");
        }

        protected override void Disposing()
        {
            if(mContent != null)
            {
                mContent.RemoveFromParent(false);
                mContent.Dispose();
                mContent = null;
            }

            base.Disposing();
        }

        public override void SetSize(float w, float h)
        {
            mContent.Bounds.height = h;
            mContent.Bounds.width = w;
            base.SetSize(w, h);
        }

        public override Rectangle2D Bounds
        {
            set
            {
                mContent.Bounds = value;
                base.Bounds = value;
            }

            get
            {
                return base.Bounds;
            }
        }

        public void SetRichTextLayer(RichTextLayer layer)
        {
            mContent.HTMLTextLayer = layer;
            this.mContentHeight = layer.ContentHeight;
            this.mContentWidth = layer.ContentWidth;
            Initialize();
        }

        public override void Update(float DeltaTime)
        {
            base.Update(DeltaTime);
            if(mContent != null)
            {
                mContent.SetScrollXY(-mContainer.X, -mContainer.Y);
            }

        }

    }

    public class TextContent : DisplayNode
    {
        private float mScrollRectX = 0.0f;
        private float mScrollRectY = 0.0f;
        private RichTextLayer mTextLayer;


        internal TextContent()
            : base("TextContent")
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.Bounds = new Gemo.Rectangle2D(0, 0, 1, 1);
        }

        public RichTextLayer HTMLTextLayer
        {
            set
            {
                mTextLayer = value;
            }
            get
            {
                return mTextLayer;
            }
        }

        internal void SetScrollXY(float x, float y)
        {
            mScrollRectX = x;
            mScrollRectY = y;
        }

        public override void Draw(Graphics g)
        {
            if(mTextLayer != null)
            {
                mTextLayer.Render(g, 0, 0, Bounds.width, Bounds.height, mScrollRectX, mScrollRectY, 0);
            }

        }
        protected override void Disposing()
        {
            if (mTextLayer != null)
            {
                mTextLayer.Dispose();
                mTextLayer = null;
            }
            base.Disposing();
        }
    }

}
