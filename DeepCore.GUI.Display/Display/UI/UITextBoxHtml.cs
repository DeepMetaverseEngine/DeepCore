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
    public class UITextBoxHtml : UIComponent
    {
        private RichTextLayer mRichTextLayer = null;

        private uint mDefaultTextColor = 0;
        private float mDefualtTextSize = 0;
        private TextAttribute mTa = null;
        private float mContentWidth = 0;
        private bool isDirty = false;
        public UITextBoxHtml()
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.mRichTextLayer = UIFactory.Instance.CreateRichTextLayer();
            mTa = new TextAttribute();
        }

        public uint DefaultTextColor { set { mDefaultTextColor = value; } get { return mDefaultTextColor; } }
        public float DefualtTextSize { set { mDefualtTextSize = value; } get { return mDefualtTextSize; } }

        public RichTextLayer RichTextLayer
        {
            get
            {
                return mRichTextLayer;
            }
        }

        protected override void Disposing()
        {
            if(mRichTextLayer != null)
            {
                mRichTextLayer.Dispose();
                mRichTextLayer = null;
            }

            base.Disposing();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);

            if(mRichTextLayer != null)
            {
                if(isDirty)
                {
                    Refresh();
                }
                mRichTextLayer.IsEnable = !this.Disable;
                mRichTextLayer.Render(g, 0, 0);
            }

        }

        public void SetHtmlText(string html)
        {
            try
            {
                AttributedString abt = new AttributedString();
                abt.AddAttribute(mTa);
                XmlDocument xml = XmlUtil.FromString(html);
                abt.Append(UIFactory.Instance.DecodeAttributedString(xml));
                this.RichTextLayer.SetString(abt);
            }
            catch(Exception error)
            {
                Driver.Instance.Assert(false, error.ToString());
            }

        }

        private void Refresh()
        {
            mRichTextLayer.SetWidth(mContentWidth);
            isDirty = false;
        }

        public void SetExceptSize(Size2D size)
        {
            mContentWidth = size.width;
            isDirty = true;
        }

        public void SetTextColor(uint rgba)
        {
            mTa.fontColor = rgba;
            isDirty = true;
        }

        public void SetFontSize(float size)
        {
            mTa.fontSize = size;
            isDirty = true;
        }

        public void SetFontStyle(FontStyle style)
        {
            mTa.fontStyle = style;
            isDirty = true;
        }

        public void SetBorderTimes(int times)
        {
            mTa.borderCount = (Data.TextBorderCount)times;
            isDirty = true;
        }

        public void SetBorderColor(uint color)
        {
            mTa.borderColor = color;
            isDirty = true;
        }


        protected override void DecodeFields(UIEditor editor, Data.UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            if (e is Data.UETextComponentMeta)
            {
                Data.UETextComponentMeta meta = e as Data.UETextComponentMeta;
                SetExceptSize(new Size2D(this.Bounds.width, this.Bounds.height));

                if (meta.textFontSize == 0)
                {
                    this.DefualtTextSize = UIEditor.Instance.DefaultFontSize;
                }
                else
                {
                    this.DefualtTextSize = meta.textFontSize;
                }

                SetFontSize(DefualtTextSize);


                uint c = Color.toRGBA(meta.textColor);
                SetTextColor(c);
                this.DefaultTextColor = c;


                int borderTimes = UIEditor.Instance.RichTextBorderTimes;
                this.RichTextLayer.SetBorder(borderTimes, meta.textBorderColor);
                this.RichTextLayer.SetWidth(Bounds.width);
            }
        }
    }
}

