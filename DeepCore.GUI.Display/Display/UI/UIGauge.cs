using System;

namespace DeepCore.GUI.Display.UI
{
    public class UIGauge : UIComponent
    {
        public enum GAUGE_TYPE
        {
            eRectange,
            eCircle
        }

        public enum GAUGE_ORIENTATION
        {
            eLEFT_2_RIGHT = Data.GaugeOrientation.LEFT_2_RIGHT,
            eRIGHT_2_LEFT = Data.GaugeOrientation.RIGTH_2_LEFT,
            eTOP_2_BOTTOM = Data.GaugeOrientation.TOP_2_BOTTOM,
            eBOTTOM_2_TOP = Data.GaugeOrientation.BOTTOM_2_TOP,
        }

        private GAUGE_TYPE mGaugeType = GAUGE_TYPE.eRectange;
        private UILayout ptLayout = null;
        private GAUGE_ORIENTATION mOrientation = GAUGE_ORIENTATION.eLEFT_2_RIGHT;
        private bool mCalculateArgs = true;
        private float mCurrentPercent = 0.5f;
        private bool mShowPercent = false;
        private float sx = 0;
        private float sy = 0;
        private float sw = 0;
        private float sh = 0;

        private float dx = 0;
        private float dy = 0;
        private float dw = 0;
        private float dh = 0;

        private float mStartAngle = 0;
        private float mEndAngle = 0;

        private UITextField mTextField = null;
        private Anchor mAnchor = Anchor.NONE;

        public UIGauge()
        {
            this.Enable = false;
            this.EnableChildren = false;
        }

        public GAUGE_TYPE GaugeType
        {
            set
            {
                mGaugeType = value;
            }
            get
            {
                return mGaugeType;
            }
        }

        public float Percent
        {
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new Exception("percent value out of range :[0,1] value  = " + value);
                }
                if (mCurrentPercent != value)
                {
                    mCurrentPercent = value;
                }
                mCalculateArgs = true;
            }
            get
            {
                return mCurrentPercent;
            }
        }

        public GAUGE_ORIENTATION Orientation
        {
            set
            {
                if (mOrientation != value)
                {
                    mOrientation = value;
                    mCalculateArgs = true;
                }
            }
            get
            {
                return mOrientation;
            }
        }

        public bool ShowPercent
        {
            set
            {
                if (mShowPercent == value)
                {
                    return;
                }
                mShowPercent = value;
                if (mShowPercent)
                {
                    TextToPercent();
                }
                else
                {
                    PercentToText();
                }
            }
            get
            {
                return mShowPercent;
            }
        }

        public string Text
        {
            get
            {
                CheckTextField();
                return mTextField.Text;
            }
            set
            {
                CheckTextField();
                mTextField.Text = value;
            }
        }

        public override void SetSize(float w, float h)
        {
            base.SetSize(w, h);

            if (mTextField != null)
            {
                ResetTextField(mTextField, w, h);
            }
        }

        /// <summary>
        /// 设置进度条Layout
        /// </summary>
        /// <param name="pt"></param>
        public void SetPTLayer(UILayout pt)
        {
            if (pt == null)
            {
                throw new Exception("ptLayout can not be null!");
            }
            ptLayout = pt;
            mCalculateArgs = true;
        }

        private void CalculateArguments()
        {

            if (!mCalculateArgs)
            {
                return;
            }
            switch (mGaugeType)
            {

                case GAUGE_TYPE.eRectange:
                    switch (mOrientation)
                    {
                        case GAUGE_ORIENTATION.eLEFT_2_RIGHT:
                            CalculateLEFT_2_RIGHT();
                            break;
                        case GAUGE_ORIENTATION.eRIGHT_2_LEFT:
                            CalculateRIGHT_2_LEFT();
                            break;
                        case GAUGE_ORIENTATION.eBOTTOM_2_TOP:
                            CalculateBOTTOM_2_TOP();
                            break;
                        case GAUGE_ORIENTATION.eTOP_2_BOTTOM:
                            CalculateTOP_2_BOTTOM();
                            break;
                        default:
                            break;
                    }
                    break;
                case GAUGE_TYPE.eCircle:
                    mEndAngle = 360 * mCurrentPercent;
                    break;
                default:
                    break;
            }
            mCalculateArgs = false;
        }

        private void CalculateLEFT_2_RIGHT()
        {
            sx = 0;
            sy = 0;

            sw = Width * mCurrentPercent;
            sh = Height;

            dx = 0;
            dy = 0;

            dw = Width * mCurrentPercent;
            dh = Height;
        }

        private void CalculateRIGHT_2_LEFT()
        {
            sx = Width * (1 - mCurrentPercent);
            sy = 0;

            sw = Width * mCurrentPercent;
            sh = Height;

            dx = Width * (1 - mCurrentPercent);
            ;
            dy = 0;

            dw = Width * mCurrentPercent;
            dh = Height;
        }

        private void CalculateBOTTOM_2_TOP()
        {
            sx = 0;
            sy = Height * (1 - mCurrentPercent);

            sw = Width;
            sh = Height * mCurrentPercent;

            dx = 0;
            dy = Height * (1 - mCurrentPercent);

            dw = Width;
            dh = Height * mCurrentPercent;
        }

        private void CalculateTOP_2_BOTTOM()
        {
            sx = 0;
            sy = 0;

            sw = Width;
            sh = Height * mCurrentPercent;

            dx = 0;
            dy = 0;

            dw = Width;
            dh = Height * mCurrentPercent;
        }

        private void TextToPercent()
        {

        }

        private void PercentToText()
        {

        }

        public void InitTextField(string text, int fontSize, FontStyle fs = FontStyle.STYLE_PLAIN)
        {
            mTextField = new UITextField(text, fontSize, fs);
            this.AddChild(mTextField);
        }

        private void CheckTextField()
        {
            if (mTextField == null)
            {
                throw new Exception("Plz InitTextField befor you use it");
            }
        }

        public bool ShowText
        {
            set
            {
                if (mTextField != null)
                {
                    mTextField.Visible = value;
                }
            }

            get
            {
                if (mTextField != null)
                {
                    return mTextField.Visible;
                }
                return false;
            }
        }

        public void SetTextAnchor(Anchor anchor)
        {
            CheckTextField();
            if (mAnchor == anchor)
            {
                return;
            }
            mAnchor = anchor;
            mTextField.SetAnchor(anchor);
            ResetTextField(mTextField, Bounds.width, Bounds.height);
        }

        public void SetTextColor(uint rgba)
        {
            CheckTextField();
            mTextField.SetFontColor(rgba);
        }

        public void SetFontSize(int size)
        {
            CheckTextField();
            mTextField.SetFontSize(size);
        }

        private void ResetTextField(UITextField o, float w, float h)
        {

            if (o == null)
            {
                return;
            }

            if ((mAnchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                o.X = Width * 0.5f;
            }
            else if ((mAnchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                o.X = Width;
            }
            else
            {
                o.X = 0;
            }
            if ((mAnchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                o.Y = Height * 0.5f;
            }
            else if ((mAnchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                o.Y = Height;
            }
            else
            {
                o.Y = 0;
            }

        }

        public override void Draw(Display.Graphics g)
        {
            CalculateArguments();

            if (mUILayout != null)
            {
                mUILayout.Render(g, Width, Height);
            }

            if (ptLayout != null)
            {
                if (mGaugeType == GAUGE_TYPE.eRectange)
                {
                    ptLayout.RenderRegion(g, sx, sy, sw, sh, dx, dy, dw, dh);
                }
                else if (mGaugeType == GAUGE_TYPE.eCircle)
                {
                    ptLayout.RenderCircle(g, mStartAngle, mEndAngle);
                }
            }
        }

        protected override void Disposing()
        {
            if (ptLayout != null)
            {
                ptLayout.Dispose();
                ptLayout = null;
            }

            if (mTextField != null)
            {
                mTextField.RemoveFromParent();
                mTextField.Dispose();
                mTextField = null;
            }

            base.Disposing();
        }

        protected override void DecodeFields(UIEditor editor, Data.UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            if (e is Data.UEGaugeMeta)
            {
                Data.UEGaugeMeta meta = e as Data.UEGaugeMeta;

                this.Orientation = (GAUGE_ORIENTATION)meta.render_orientation;
                this.ShowPercent = meta.showPercent;

                if (meta.custom_layout_up != null)
                {
                    this.SetPTLayer(editor.CreateLayout(meta.custom_layout_up));
                }

                this.InitTextField(meta.text, meta.textFontSize, FontStyle.STYLE_PLAIN);
                this.SetTextAnchor(Data.AnchorTool.FromTextAnchor(meta.text_anchor));
                this.SetTextColor(Color.toRGBA(meta.textColor));
            }

        }
    }
}

