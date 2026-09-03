using DeepCore.GUI.Data;
using DeepCore.Reflection;
using DeepCore.Unity3D.UGUI;
using System;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UEGauge : UIComponent
    {
        protected readonly UIComponent mStrip;
        protected DisplayText mTextSprite;

        private double mGaugeMinValue = 0, mGaugeMaxValue = 100, mGaugeValue = 50;
        private GaugeOrientation mOrientation = GaugeOrientation.LEFT_2_RIGHT;
        private string mText = "";
        private bool mIsShowPercent = false;
        protected readonly bool mUseBitmapFont;

        public UEGauge(bool use_bitmap)
        {
            this.mUseBitmapFont = use_bitmap;
            this.mStrip = new UIComponent("strip");
            this.mStrip.Layout = UILayout.CreateUILayoutColor(new Color(1f, 1f, 1f, 0.5f), Color.black);
            this.mStrip.Enable = false;
            this.mStrip.EnableChildren = false;
            this.AddChild(mStrip);
            this.Enable = false;
            this.EnableChildren = false;
        }
        public UEGauge() : this(UIEditor.GlobalUseBitmapText)
        {

        }

        public UIComponent Strip
        {
            get { return mStrip; }
        }
        public UILayout StripLayout
        {
            get { return mStrip.Layout; }
            set { mStrip.Layout = value; ; }
        }
        public DisplayText TextSprite
        {
            get { return mTextSprite; }
        }
        public GaugeOrientation Orientation
        {
            get { return mOrientation; }
            set
            {
                mOrientation = value;
                ChangeFillMode(this.mOrientation);
            }
        }
        public double Value
        {
            get { return mGaugeValue; }
            set
            {
                if (value > mGaugeMaxValue) throw new Exception(string.Format("Gauge value [{0}] out of range [{1}-{2}]", value, mGaugeMinValue, mGaugeMaxValue));
                if (value < mGaugeMinValue) throw new Exception(string.Format("Gauge value [{0}] out of range [{1}-{2}]", value, mGaugeMinValue, mGaugeMaxValue));
                if (mGaugeValue != value)
                {
                    mGaugeValue = value;
                    DoValueChanged();
                }
            }
        }
        public float ValuePercent
        {
            get
            {
                if (mGaugeMaxValue - mGaugeMinValue == 0) { return 0; }
                else { return (float)((mGaugeValue - mGaugeMinValue) / (mGaugeMaxValue - mGaugeMinValue) * 100f); }
            }
            set
            {
                if (value < 0 || value > 100) throw new Exception(string.Format("Gauge ValuePercent [{0}] out of range [0-100]", value));
                Value = mGaugeMinValue + (mGaugeMaxValue - mGaugeMinValue) * value * 0.01;
            }
        }
        public bool IsShowPercent
        {
            get { return mIsShowPercent; }
            set
            {
                if (mIsShowPercent != value)
                {
                    mIsShowPercent = value;
                    DoTextChanged();
                }
            }
        }

        //-----------------------------------------------------------------------------------------------

        public string Text
        {
            get { return mText; }
            set
            {
                if (IsDispose) return;
                if (!string.Equals(mText, value))
                {
                    mText = value;
                    DoTextChanged();
                }
            }
        }
        public int FontSize
        {
            get { return mTextSprite.FontSize; }
            set { mTextSprite.FontSize = value; }
        }
        public UnityEngine.Color FontColor
        {
            get { return mTextSprite.FontColor; }
            set { mTextSprite.FontColor = value; }
        }
        public GUI.Data.TextAnchor EditTextAnchor
        {
            get { return mTextSprite.Anchor; }
            set { mTextSprite.Anchor = value; }
        }
        public Vector2 TextOffset
        {
            get { return mTextSprite.TextOffset; }
            set { mTextSprite.TextOffset = value; }
        }

        //-----------------------------------------------------------------------------------------------

        public double GaugeMinValue
        {
            get { return mGaugeMinValue; }
        }

        public double GaugeMaxValue
        {
            get { return mGaugeMaxValue; }
        }

        public void SetGaugeMinMax(double min, double max)
        {
            if (min > max) DeepCore.CUtils.Swap<double>(ref min, ref max);
            bool needDoChange = (mGaugeMinValue != min) || (mGaugeMaxValue != min);

            mGaugeMaxValue = max;
            mGaugeMinValue = min;
            double value = this.Value;
            value = Math.Max(min, value);
            value = Math.Min(max, value);

            if (needDoChange && value == this.Value)
            {
                DoValueChanged();
            }
            this.Value = value;
        }

        /// <summary>
        /// 设置填充模式.
        /// EG.SetFillMode(UnityEngine.UI.Image.FillMethod.Radial360,
        ///               (int)UnityEngine.UI.Image.Origin360.Left)
        /// </summary>
        /// <param name="fill"></param>
        /// <param name="fillOrigin"></param>
        /// <param name="fillClockwise"></param>
        /// <param name="fillCenter"></param>
        public void SetFillMode(UnityEngine.UI.Image.FillMethod fill, int fillOrigin, bool fillClockwise = false, bool fillCenter = true)
        {
            if (mStrip != null && mStrip.Graphics != null)
            {
                mStrip.Graphics.SetFillMode(fill, fillOrigin, fillClockwise, fillCenter);
            }
        }

        private void ChangeFillMode(GaugeOrientation Orientation)
        {
            if (mStrip != null && mStrip.Layout != null)
            {
                if (mStrip.Layout.Style == UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER ||
                    mStrip.Layout.Style == UILayoutStyle.IMAGE_STYLE_BACK_4)
                {
                    switch (Orientation)
                    {
                        case GaugeOrientation.BOTTOM_2_TOP:
                            this.SetFillMode(UnityEngine.UI.Image.FillMethod.Vertical,
                                (int)UnityEngine.UI.Image.OriginVertical.Bottom);
                            break;
                        case GaugeOrientation.TOP_2_BOTTOM:
                            this.SetFillMode(UnityEngine.UI.Image.FillMethod.Vertical,
                             (int)UnityEngine.UI.Image.OriginVertical.Top);
                            break;
                        case GaugeOrientation.LEFT_2_RIGHT:
                            this.SetFillMode(UnityEngine.UI.Image.FillMethod.Horizontal,
                          (int)UnityEngine.UI.Image.OriginHorizontal.Left);
                            break;
                        case GaugeOrientation.RIGTH_2_LEFT:
                            this.SetFillMode(UnityEngine.UI.Image.FillMethod.Horizontal,
                          (int)UnityEngine.UI.Image.OriginHorizontal.Right);
                            break;
                    }
                }
            }
        }
        //-----------------------------------------------------------------------------------------------
        protected override void OnStart()
        {
            if (mTextSprite == null)
            {
                if (mUseBitmapFont)
                {
                    mTextSprite = new BitmapTextSprite("bitmap_text");
                }
                else
                {
                    mTextSprite = new TextSprite("bitmap_text");
                }
            }
            base.OnStart();
            this.DoValueChanged();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (mStrip.Graphics.IsShowUILayout)
            {
                UIUtils.AdjustGaugeOrientation(Orientation, this, mStrip, ValuePercent);
            }
            else
            {
                mStrip.Position2D = Vector2.zero;
                mStrip.Size2D = this.Size2D;
            }
            if (mTextSprite != null)
            {
                mTextSprite.Size2D = this.Size2D;
            }
        }
        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            {
                if (!string.IsNullOrEmpty((e as UEGaugeMeta).ImageFont))
                {
                    this.Decode_ImageFont(editor, e as UEGaugeMeta);
                }
                else if (mUseBitmapFont)
                {
                    this.Decode_BitmapText(editor, e as UEGaugeMeta);
                }
                else
                {
                    this.Decode_Text(editor, e as UEGaugeMeta);
                }
            }
            this.Decode_Gauge(editor, e as UEGaugeMeta);

            this.Enable = false;
            this.EnableChildren = false;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Gauge(UIEditor.Decoder editor, UEGaugeMeta e)
        {
            this.Text = e.text;
            this.mGaugeMaxValue = e.gaugeMax;
            this.mGaugeMinValue = e.gaugeMin;
            this.mGaugeValue = e.gaugeValue;
            this.Orientation = e.render_orientation;
            this.IsShowPercent = e.showPercent;
            UILayout custom_layout_up = editor.CreateLayout(e.custom_layout_up);
            if (custom_layout_up != null)
            {
                mStrip.Layout = custom_layout_up;
                ChangeFillMode(this.Orientation);
            }
            mTextSprite.Text = this.Text;
            mTextSprite.Size2D = this.Size2D;
            mTextSprite.Anchor = e.text_anchor;
            mTextSprite.TextOffset = new Vector2(e.text_offset_x, e.text_offset_y);
        }

        private void Decode_Text(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new TextSprite("text");
            this.mTextSprite = text;
            this.AddChild(mTextSprite);

            if (!string.IsNullOrEmpty(e.textFontName))
            {
                text.SetTextFont(editor.editor.CreateFont(e.textFontName), this.FontSize, (UnityEngine.FontStyle)e.textFontStyle);
            }
            else
            {
                text.SetTextFont(editor.editor.CreateFont(null), editor.editor.DefaultFont.fontSize, UnityEngine.FontStyle.Normal);
            }
            if (e.textFontSize > 0)
            {
                text.FontSize = e.textFontSize;
                text.Graphics.resizeTextForBestFit = false;
            }
            else
            {
                text.Graphics.resizeTextForBestFit = true;
            }
            text.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                text.AddBorder(border_color, new Vector2(1, -1));
            }
        }
        private void Decode_BitmapText(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new BitmapTextSprite("bitmap_text");
            this.mTextSprite = text;
            this.AddChild(mTextSprite);

            text.Graphics.Style = e.textFontStyle;
            text.Graphics.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            if (e.textFontSize > 0)
            {
                text.FontSize = e.textFontSize;
            }
            if (e.textBorderAlpha > 0)
            {
                Color border_color = UIUtils.UInt32_ARGB_To_Color(e.textBorderColor);
                border_color.a = e.textBorderAlpha / 100f;
                text.Graphics.BorderTime = TextBorderCount.Border;
                text.Graphics.BorderColor = border_color;
            }
        }
        private void Decode_ImageFont(UIEditor.Decoder editor, UETextComponentMeta e)
        {
            var text = new ImageFontSprite("image_font");
            this.mTextSprite = text;
            this.AddChild(mTextSprite);

            editor.editor.ParseImageFont(e.ImageFont, e.text, text.Graphics);

        }

        private void DoTextChanged()
        {
            string text = this.Text;
            if (IsShowPercent)
            {
                text = string.Format("{0}%", ((int)ValuePercent));
                mTextSprite.Text = text;
            }
            else
            {
                mTextSprite.Text = text;
            }
        }
        private void DoValueChanged()
        {
            string text = this.Text;
            if (IsShowPercent)
            {
                text = string.Format("{0}%", ((int)ValuePercent));
                mTextSprite.Text = text;
            }
            else
            {
                mTextSprite.Text = text;
            }
            mStrip.Graphics.SetFillPercent(ValuePercent);
            mStrip.Graphics.SetAllDirty();
            if (event_ValueChanged != null)
            {
                event_ValueChanged.Invoke(this, mGaugeValue);
            }

        }


        protected override void OnDisposeEvents()
        {
            base.OnDisposeEvents();
            event_ValueChanged = null;
        }

        public delegate void ValueChangedHandler(UEGauge sender, double value);

        [Desc("值改变")]
        public ValueChangedHandler event_ValueChanged;

        [Desc("值改变")]
        public event ValueChangedHandler ValueChanged { add { event_ValueChanged += value; } remove { event_ValueChanged -= value; } }

        public ValueChangedHandler event_SetPullNodePosToIntNum;
        public event ValueChangedHandler SetPullNodePos { add { event_SetPullNodePosToIntNum += value; } remove { event_SetPullNodePosToIntNum -= value; } }
    }

}
