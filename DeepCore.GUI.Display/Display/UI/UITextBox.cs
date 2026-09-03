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
    public class UITextBox : UIComponent
    {
        private RichTextLayer mRichTextLayer = null;

        private bool mIsHtmlMode = false;
        private String mText = null;
        private String mHTMLText = null;

        private TextAttribute mTa = null;
        private float mContentWidth = 0;
        private bool isDirty = false;
        public UITextBox()
        {
            Bounds = new Rectangle2D(0, 0, 1, 1);
            this.Enable = false;
            this.EnableChildren = false;
            mRichTextLayer = UIFactory.Instance.CreateRichTextLayer();
            mTa = new TextAttribute();
        }

        public RichTextLayer RichTextLayer
        {
            get
            {
                return mRichTextLayer;
            }
        }

        public string Text
        {
            get
            {
                return mText;
            }
            set
            {
                if (mText == value && mIsHtmlMode == false)
                {
                    return;
                }
                mIsHtmlMode = false;
                mText = value;
                isDirty = true;
            }

        }

        public string HTMLText
        {
            get
            {
                return mHTMLText;
            }
            set
            {
                if (value == mHTMLText && mIsHtmlMode == true)
                {
                    return;
                }

                if (TransLateString(value))
                {
                    mHTMLText = value;
                    mIsHtmlMode = true;
                }

            }
        }

        protected override void Disposing()
        {
            if (mRichTextLayer != null)
            {
                mRichTextLayer.Dispose();
                mRichTextLayer = null;
            }

            mTa = null;
            base.Disposing();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (mRichTextLayer != null)
            {
                if (isDirty)
                {
                    Refresh();
                }
                mRichTextLayer.IsEnable = !this.Disable;
                mRichTextLayer.Render(g, 0, 0);
            }

        }

        private void Refresh()
        {
            if (!mIsHtmlMode)
            {
                AttributedString mAttr = new AttributedString();
                mAttr.Append(mText, mTa);
                mRichTextLayer.SetWidth(mContentWidth);
                mRichTextLayer.SetString(mAttr);
            }
            else
            {
                mRichTextLayer.SetWidth(mContentWidth);
            }

            isDirty = false;
        }

        private bool TransLateString(string htmlText)
        {
            AttributedString abt = new AttributedString();
            try
            {
                XmlDocument xml = XmlUtil.FromString(htmlText);
                abt.Append(UIFactory.Instance.DecodeAttributedString(xml));
            }
            catch (Exception err)
            {
                Driver.Instance.Assert(false, err.ToString());
                return false;
            }

            mRichTextLayer.SetString(abt);
            return true;
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

        public void SetFontStyle(Display.FontStyle style)
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

            if (e is Data.UETextBoxMeta)
            {
                Data.UETextBoxMeta meta = e as Data.UETextBoxMeta;
                SetExceptSize(new Size2D(this.Bounds.width, this.Bounds.height));
                SetFontSize(meta.text_size);
                this.Text = meta.Text;
                SetTextColor(Color.toRGBA(meta.textColor));
                SetBorderColor(Color.COLOR_BLACK);
                int borderTimes = UIEditor.Instance.RichTextBorderTimes;
                SetBorderTimes(borderTimes);
            }
        }
    }
}
