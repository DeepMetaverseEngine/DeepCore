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
    public class UIRichTextBox : UIScrollBase
    {
        private RichTextLayer mTextLayer;

        public UIRichTextBox()
        {
            this.mTextLayer = UIFactory.Instance.CreateRichTextLayer(200, RichTextAlignment.taLEFT);
            this.mTextLayer.SetBorder((int)Data.TextBorderCount.Border, Color.COLOR_BLACK);
        }

        protected override void Resize(float w, float h, bool flush)
        {
            base.Resize(w, h, flush);
            mTextLayer.SetWidth(w);
        }

        public void LoadTextFromXML(string xml)
        {
            LoadTextFromXML(XmlUtil.FromString(xml));
        }
        public void LoadTextFromXML(XmlDocument xml)
        {
            AttributedString atext = UIFactory.Instance.DecodeAttributedString(xml);
            mTextLayer.SetString(atext);
        }

        public AttributedString Text
        {
            get
            {
                return mTextLayer.GetText();
            }
            set
            {
                mTextLayer.SetString(value);
            }
        }

        public RichTextLayer Layer
        {
            get
            {
                return mTextLayer;
            }
        }

        public void SetBorder(int count, uint color)
        {
            mTextLayer.SetBorder(count, color);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            mTextLayer.Render(g, 0, 0, Width, Height, 0, 0, 0);
        }

        protected override void Disposing()
        {
            if(mTextLayer != null)
            {
                mTextLayer.Dispose();
                mTextLayer = null;
            }

            base.Disposing();
        }


    }
}
