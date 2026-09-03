using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.Text;
using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using UnityEngine;
using AttributedString = DeepCore.GUI.Display.Text.AttributedString;
using BaseRichTextLayer = DeepCore.GUI.Display.Text.BaseRichTextLayer;
using TextAnchor = UnityEngine.TextAnchor;
using TextAttribute = DeepCore.GUI.Display.Text.TextAttribute;
using UnityImage = DeepCore.Unity3D.Impl.UnityImage;

namespace DeepCore.Unity3D.UGUI
{    //----------------------------------------------------------------------------------------
    /// <summary>
    /// 富文本渲染器
    /// </summary>
    public partial class UGUIRichTextLayer : BaseRichTextLayer
    {
        /// <summary>
        /// 使用系统字
        /// </summary>
        public bool UseBitmapFont
        {
            get { return mUseBitmapFont; }
            set { mUseBitmapFont = value; }
        }
        public Font DefaultFont
        {
            get { return mFont; }
            set
            {
                this.mFont = value;
                this.mTextGenSetting.font = value;
                this.mTextGenSetting.fontSize = value.fontSize;
            }
        }

        public new List<Line> AllLines => base.AllLines;
        
        public DisplayNode Binding
        {
            get { return mOwner; }
        }
        public TextAttribute DefaultTextAttribute
        {
            get { return mDefaultTextAttribute; }
            set { mDefaultTextAttribute = value; }
        }
        public string UnityRichText
        {
            set { this.XmlText = UIUtils.UnityRichTextToXmlText(value); }
        }
        public string XmlText
        {
            set
            {
                try
                {
                    var atext = UIFactory.Instance.DecodeAttributedString(value, mDefaultTextAttribute);
                    if (atext != null)
                    {
                        this.SetString(atext);
                    }
                    else
                    {
                        this.Text = value;
                    }
                }
                catch (Exception err)
                {
                    Debug.LogWarningFormat("{0}\n{1}", err.Message, value);
                    this.Text = value;
                }
            }
        }
 		private bool mUseBitmapFont = false;
        public TextGenerationSettings TextGenSetting
        {
            get { return mTextGenSetting; }
        }
        public TextGenerator TextGen
        {
            get { return mTextGen; }
        }
        private DisplayNode mOwner;
        private UnityEngine.Font mFont;
        private TextAttribute mDefaultTextAttribute;
        private TextGenerator mTextGen;
        private TextGenerationSettings mTextGenSetting = new TextGenerationSettings();
        private Rect mCaretBounds = new Rect(0, 0, 0, 0);

        private GUI.Data.TextAnchor mAnchor = GUI.Data.TextAnchor.L_T;
        public UGUIRichTextLayer(DisplayNode parent, bool useBitmapFont)
        {
            this.mUseBitmapFont = useBitmapFont;

            this.mOwner = parent;
            this.mFont = UIFactory.Instance.DefaultFont;
            this.mTextGen = UIFactory.Instance.DefaultTextGenerator;

            this.mTextGenSetting.color = UnityEngine.Color.white;
            this.mTextGenSetting.font = mFont;
            this.mTextGenSetting.fontSize = mFont.fontSize;
            this.mTextGenSetting.fontStyle = UnityEngine.FontStyle.Normal;
            this.mTextGenSetting.generateOutOfBounds = true;
            this.mTextGenSetting.generationExtents = Vector2.zero;
            this.mTextGenSetting.horizontalOverflow = HorizontalWrapMode.Overflow;
            this.mTextGenSetting.lineSpacing = 1;
            this.mTextGenSetting.pivot = Vector2.zero;
            this.mTextGenSetting.resizeTextForBestFit = false;
            this.mTextGenSetting.resizeTextMaxSize = 1;
            this.mTextGenSetting.resizeTextMinSize = 1;
            this.mTextGenSetting.richText = false;
            this.mTextGenSetting.scaleFactor = 1;
            this.mTextGenSetting.textAnchor = UnityEngine.TextAnchor.LowerLeft;
            this.mTextGenSetting.updateBounds = true;
            this.mTextGenSetting.verticalOverflow = VerticalWrapMode.Overflow;

            this.mDefaultTextAttribute = new TextAttribute(
                UIUtils.Color_To_UInt32_RGBA(mTextGenSetting.color),
                mTextGenSetting.fontSize,
                mTextGenSetting.font.name);
        }

        public override void Dispose()
        {
            this.mOwner = null;
            this.mFont = null;
            this.mTextGen = null;
            base.Dispose();
        }

        protected override Image AddImage(string file)
        {
            Image img = UnityDriver.Instance.CreateImage(file);
            return img;
        }

        protected override CPJResource AddCPJResource(string file)
        {
            CPJResource res = CPJResource.CreateResource(file);
            return res;
        }



        protected override void OnBeginResetChars()
        {
            this.mCaretBounds.y = 0;
            this.mCaretBounds.x = 0;
            this.mCaretBounds.size = UIFactory.Instance.DefaultCaretSize;
            base.OnBeginResetChars();
        }
        protected override void OnEndResetLines()
        {
            base.OnEndResetLines();
            if (AllRegions.Count > 0)
            {
                var rg = AllRegions[AllRegions.Count - 1];
                var bounds = rg.Bounds;
                this.mCaretBounds.height = bounds.height;
                this.mCaretBounds.width = UIFactory.Instance.DefaultCaretSize.x;
                if (rg.IsBreak)
                {
                    this.mCaretBounds.y = bounds.y + bounds.height;
                    this.mCaretBounds.x = 0;
                }
                else
                {
                    this.mCaretBounds.y = bounds.y;
                    this.mCaretBounds.x = bounds.x + bounds.width;
                }
            }
        }

        protected override bool TestTextLineBreak(string text, TextAttribute ta, float testW, out float tw, out float th)
        {
            if (UseBitmapFont)
            {
                return UnityDriver.Instance.TestTextLineBreak(text, DefaultFont, ta.fontSize, ta.fontStyle, this.BorderCount, testW, out tw, out th);
            }
            else
            {
                int fsize = (int)ta.fontSize;
                UnityEngine.FontStyle fstyle = (UnityEngine.FontStyle)ta.fontStyle;
                TextGenerationSettings setting = mTextGenSetting;
                setting.fontSize = fsize;
                setting.fontStyle = fstyle;

                tw = mTextGen.GetPreferredWidth(text, setting);
                th = mTextGen.GetPreferredHeight(text, setting);
                if (testW < tw)
                {
                    return true;
                }
                return false;
            }
        }

        //--------------------------------------------------------------------------------------------
        #region ITextComponent

        public string Text
        {
            get { return base.GetText().ToString(); }
            set { base.SetString(new AttributedString(value, mDefaultTextAttribute)); }
        }
        public int FontSize
        {
            get { return (int)mDefaultTextAttribute.fontSize; }
            set { mDefaultTextAttribute.fontSize = value; }
        }
        public UnityEngine.Color FontColor
        {
            get { return UIUtils.UInt32_RGBA_To_Color(mDefaultTextAttribute.fontColor); }
            set { mDefaultTextAttribute.fontColor = UIUtils.Color_To_UInt32_RGBA(value); }
        }
        public GUI.Data.FontStyle Style
        {
            get { bool underline; return UIUtils.ToFontStyle(mDefaultTextAttribute.fontStyle, out underline); }
            set { mDefaultTextAttribute.fontStyle = UIUtils.ToTextLayerFontStyle(value, mDefaultTextAttribute.underline); }
        }
        public bool IsUnderline
        {
            get { return mDefaultTextAttribute.underline; }
            set { mDefaultTextAttribute.underline = value; }
        }
        public Vector2 TextOffset
        {
            get { return Vector2.zero; }
            set { }
        }
        public GUI.Data.TextAnchor Anchor
        {
            get { return mAnchor; }
            set
            {
                if (mAnchor != value)
                {
                    mAnchor = value;
                    base.SetAnchor(UIUtils.ToRichTextAnchor(value));
                }
            }
        }
        public Vector2 PreferredSize
        {
            get { return new Vector2(base.ContentWidth, base.ContentHeight); }
        }
        public Rect LastCaretPosition
        {
            get { return mCaretBounds; }
        }
        public void SetBorder(UnityEngine.Color bc, Vector2 distance)
        {
            mDefaultTextAttribute.borderCount = TextBorderCount.Border;
            mDefaultTextAttribute.borderColor = UIUtils.Color_To_UInt32_RGBA(bc);
        }
        public void SetShadow(UnityEngine.Color bc, Vector2 distance)
        {
            mDefaultTextAttribute.borderCount = UIUtils.ToTextShadowCount(distance);
            mDefaultTextAttribute.borderColor = UIUtils.Color_To_UInt32_RGBA(bc);
        }
        public void SetFont(Font font)
        {
            this.DefaultFont = font;
            this.mDefaultTextAttribute.fontName = font.name;
        }

        #endregion
        //--------------------------------------------------------------------------------------------
        #region Drawable

        public override Drawable CreateDrawable(Region rg, object content)
        {
            DisplayNode region = null;
            if (content is TextDrawable)
            {
                region = content as DisplayNode;
            }
            else if (content is TImageRegion)
            {
                region = new DrawImage(this, rg, (TImageRegion)content);
            }
            else if (content is TSpriteMetaAnimateFrame)
            {
                region = new DrawSprite(this, rg, (TSpriteMetaAnimateFrame)content);
            }
            else if (UseBitmapFont)
            {
                region = new DrawBitmapText(this, rg, content.ToString());
            }
            else
            {
                region = new DrawText(this, rg, content.ToString());
            }
            mOwner.AddChild(region);
            return region as Drawable;
        }

        //--------------------------------------------------------------------------------------------
        public class DrawText : TextSprite, Drawable
        {
            public DrawText(UGUIRichTextLayer layer, Region rg, string text)
                : base(string.Format("region {0}({1}-{2}):\"{3}\"", rg.Index, rg.CharStartIndex, rg.CharEndIndex, rg.Text))
            {
                TextAttribute ta = rg.Attribute;
                this.Text = text;
                this.Graphics.supportRichText = false;
                this.SetTextFont(layer.mFont, (int)ta.fontSize, (UnityEngine.FontStyle)ta.fontStyle);
                this.Graphics.alignment = UnityEngine.TextAnchor.UpperLeft;
                this.Graphics.resizeTextForBestFit = false;
                this.Graphics.IsUnderline = rg.Attribute.underline;
                this.Graphics.color = UIUtils.UInt32_RGBA_To_Color(ta.fontColor);
                switch (rg.Attribute.borderCount)
                {
                    case GUI.Data.TextBorderCount.Null:
                        break;
                    case GUI.Data.TextBorderCount.Border:
                        this.AddBorder(UIUtils.UInt32_RGBA_To_Color(ta.borderColor), new Vector2(1, 1));
                        break;
                    default:
                        this.AddShadow(UIUtils.UInt32_RGBA_To_Color(ta.borderColor), UIUtils.ToTextBorderOffset(rg.Attribute.borderCount));
                        break;
                }
                this.Size2D = new Vector2(rg.Bounds.width, rg.Bounds.height);
                this.Position2D = new Vector2(rg.Bounds.x, rg.Bounds.y);
            }
            public float CharWidth { get { return this.Size2D.x; } }
            public float CharHeight { get { return this.Size2D.y; } }
            public void Render(GUI.Display.Graphics g, Region rg, float x, float y, int deltaMS)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = true;
            }
            public void Hide(BaseRichTextLayer.Region self, float x, float y)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = false;
            }
        }//--------------------------------------------------------------------------------------------
        public class DrawBitmapText : BitmapTextSprite, Drawable
        {
            public DrawBitmapText(UGUIRichTextLayer layer, Region rg, string text)
                : base(string.Format("region {0}({1}-{2}):\"{3}\"", rg.Index, rg.CharStartIndex, rg.CharEndIndex, rg.Text))
            {
                TextAttribute ta = rg.Attribute;
                this.FontSize = (int)ta.fontSize;
                this.FontColor = UIUtils.UInt32_RGBA_To_Color(ta.fontColor);
                this.Graphics.LayerFontStyle = ta.fontStyle;
                this.Text = text;
                this.Anchor = GUI.Data.TextAnchor.L_T;
                switch (rg.Attribute.borderCount)
                {
                    case GUI.Data.TextBorderCount.Null:
                        break;
                    case GUI.Data.TextBorderCount.Border:
                        this.Graphics.SetBorder(UIUtils.UInt32_RGBA_To_Color(ta.borderColor), new Vector2(1, 1));
                        break;
                    default:
                        this.Graphics.SetShadow(UIUtils.UInt32_RGBA_To_Color(ta.borderColor), UIUtils.ToTextBorderOffset(rg.Attribute.borderCount));
                        break;
                }
                this.Size2D = new Vector2(rg.Bounds.width, rg.Bounds.height);
                this.Position2D = new Vector2(rg.Bounds.x, rg.Bounds.y);
            }

            public float CharWidth { get { return this.Size2D.x; } }
            public float CharHeight { get { return this.Size2D.y; } }
            public void Render(GUI.Display.Graphics g, Region rg, float x, float y, int deltaMS)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = true;
            }
            public void Hide(BaseRichTextLayer.Region self, float x, float y)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = false;
            }
        }
        //--------------------------------------------------------------------------------------------
        public class DrawImage : ImageSprite, Drawable
        {
            private TImageRegion image;
            private TextAttribute ta;
            private Vector2 csize;
            public DrawImage(UGUIRichTextLayer layer, Region rg, TImageRegion img)
                : base(string.Format("region {0}({1}-{2}):\"{3}\"", rg.Index, rg.CharStartIndex, rg.CharEndIndex, rg.Text))
            {
                this.ta = rg.Attribute;
                this.image = img;
                this.SetImage(img.image as UnityImage, new UnityEngine.Rect(img.sx, img.sy, img.sw, img.sh), new UnityEngine.Vector2(0, 0));
                this.Graphics.material = (img.image as UnityImage).TextureMaterial;
                if (rg.CharCount > 1)
                {
                    this.Graphics.type = UnityEngine.UI.Image.Type.Tiled;
                }
                else
                {
                    this.Graphics.type = UnityEngine.UI.Image.Type.Sliced;
                }
                if (ta.resImageZoom != null)
                {
                    this.Size2D = new Vector2(ta.resImageZoom.Width * rg.CharCount, ta.resImageZoom.Height);
                    this.csize = new Vector2(ta.resImageZoom.Width, ta.resImageZoom.Height);
                }
                else
                {
                    this.Size2D = new Vector2(image.sw * rg.CharCount, image.sh);
                    this.csize = new Vector2(image.sw, image.sh);
                }
                this.Position2D = new Vector2(rg.Bounds.x, rg.Bounds.y);
            }
            public float CharWidth { get { return csize.x; } }
            public float CharHeight { get { return csize.y; } }
            public void Render(GUI.Display.Graphics g, Region rg, float x, float y, int deltaMS)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = true;
            }
            public void Hide(BaseRichTextLayer.Region self, float x, float y)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = false;
            }
        }
        //--------------------------------------------------------------------------------------------
        public class DrawSprite : CPJSprite, Drawable
        {
            private CCD bounds;

            public DrawSprite(UGUIRichTextLayer layer, Region rg, TSpriteMetaAnimateFrame spr)
                : base(string.Format("region {0}({1}-{2}):\"{3}\"", rg.Index, rg.CharStartIndex, rg.CharEndIndex, rg.Text))
            {
                this.bounds = spr.sprite.getVisibleBounds(spr.anim);
                this.SpriteMeta = spr.sprite;
                this.Controller.SetCurrentAnimate(spr.anim);
                if (rg.CharCount > 1)
                {
                    this.Graphics.drawType = CPJSpriteGraphics.DrawType.FillGrid;
                    this.Graphics.drawGridSize = new Vector2(bounds.Width, bounds.Height);
                }
                else
                {
                    this.Graphics.drawType = CPJSpriteGraphics.DrawType.Simple;
                }
                this.Size2D = new Vector2(bounds.Width * rg.CharCount, bounds.Height);
                this.Position2D = new Vector2(rg.Bounds.x, rg.Bounds.y);
            }
            public float CharWidth { get { return bounds.Width; } }
            public float CharHeight { get { return bounds.Height; } }
            public void Render(GUI.Display.Graphics g, Region rg, float x, float y, int deltaMS)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = true;
            }
            public void Hide(BaseRichTextLayer.Region self, float x, float y)
            {
                this.Position2D = new Vector2(x, y);
                this.VisibleInParent = false;
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------------

    }


}
