using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;

namespace DeepCore.GUI.Display.UI
{
    public class UITextField : DisplayNode
    {
        private TextLayer mTextLayer;
        private Anchor mAnchor;

        public UITextField(string text, float fontSize, FontStyle fontStyle)
            : base("UITextField")
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.Bounds = new Gemo.Rectangle2D(0, 0, 1, 1);
            mTextLayer = CreateTextLayer(text, fontSize, fontStyle);
        }

        private TextLayer CreateTextLayer(string text, float fontSize, FontStyle style = FontStyle.STYLE_PLAIN)
        {
            if (fontSize <= 0) { fontSize = UIEditor.Instance.DefaultFontSize; }
            TextLayer layer = Driver.Instance.createTextLayer(text, fontSize, style);
            return layer;
        }

        public string Text
        {
            set
            {
                string text = value;
                if (mTextLayer == null || text == null) { return; }
                mTextLayer.Text = text;
            }
            get
            {
                if (mTextLayer != null) { return mTextLayer.Text; }
                return null;
            }
        }

        public void SetFontSize(float fontSize)
        {
            if (fontSize <= 0) { fontSize = UIEditor.Instance.DefaultFontSize; }
            mTextLayer.FontSize = fontSize;
        }

        public void SetAnchor(Anchor anchor)
        {
            mAnchor = anchor;
        }

        public Anchor GetAnchor()
        {
            return mAnchor;
        }

        public float GetTextWidth()
        {
            if (mTextLayer != null) { mTextLayer.GetBuffer(); return mTextLayer.Width; }
            return 0.0f;
        }

        public float GetTextHeight()
        {
            if (mTextLayer != null) { mTextLayer.GetBuffer(); return mTextLayer.Height; }
            return 0.0f;
        }

        public override void Draw(Graphics g)
        {
            if (mTextLayer != null)
            {
                mTextLayer.IsEnable = !this.Disable;
                g.DrawTextLayer(mTextLayer, 0, 0, mAnchor);
            }
        }

        public void SetFontColor(int rgb, float alpha = 1.0f)
        {
            if (mTextLayer != null)
            {
                mTextLayer.SetFontColor(rgb, alpha);
            }
        }

        public void SetFontColor(uint rgba)
        {
            if (mTextLayer != null)
            {
                mTextLayer.SetFontColor(rgba);
            }
        }

        public void SetExceptSize(Size2D size)
        {
            if (mTextLayer != null)
            {
                mTextLayer.ExpectSize = size;
            }
        }

        public void SetBorderTimes(int v)
        {
            if (mTextLayer != null)
            {
                mTextLayer.BorderTime = v;
            }
        }

        public void SetBorderColor(uint color)
        {
            if (mTextLayer != null)
            {
                mTextLayer.BorderColor = color;
            }
        }

        public void SetFontStyle(FontStyle style)
        {
            if (mTextLayer != null)
            {
                mTextLayer.TextFontStyle = style;
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

