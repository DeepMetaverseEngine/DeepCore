using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using DeepCore;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.Log;
using DeepCore.Xml;

namespace DeepCore.GUI.Display.Text
{
    public enum RichTextAlignment
    {
        taNA = 0,
        taLEFT = 1,
        taCENTER = 2,
        taRIGHT = 3,
    }

    /// <summary>
    /// 文字字符上的属性
    /// </summary>
    public partial class TextAttribute : ICloneable
    {
        /// <summary>
        ///  字符颜色 0 表示无特性 (RGBA)
        /// </summary>
        public Color fontColor = 0;
        /// <summary>
        /// 子尺寸 0 表示无特性
        /// </summary>
        public float fontSize;
        /// <summary>
        /// 字体名字，空表示无特性
        /// </summary>
        public object fontName;
        /// <summary>
        /// 字体
        /// </summary>
        public TextFontStyle fontStyle = TextFontStyle.Plain;
        /// <summary>
        /// 描边
        /// </summary>
        public TextBorderStyle borderCount = TextBorderStyle.None;
        /// <summary>
        /// 描边颜色 (RGBA)
        /// </summary>
        public uint borderColor;
        /// <summary>
        /// 此字符替换成图片，空表示无特性
        /// </summary>
        public string resImage;
        /// <summary>
        /// 图片渲染方式
        /// </summary>
        public ImageZoom resImageZoom;
        /// <summary>
        /// 此字符替换成动画，空表示无特性
        /// </summary>
        public string resSprite;
        /// <summary>
        /// 标记此处可以被点击触发事件，空表示无特性
        /// </summary>
        public string link;
        /// <summary>
        /// 行对齐方式
        /// </summary>
        public RichTextAlignment anchor = RichTextAlignment.taNA;
        /// <summary>
        /// 扩展的自定义渲染部分
        /// </summary>
        public TextDrawable drawable;

        public TextAttribute(
            uint fColorRGBA = 0,
            float fSize = 0,
            object fName = null,
            TextFontStyle fStyle = TextFontStyle.Plain,
            RichTextAlignment ta = RichTextAlignment.taNA,
            TextBorderStyle bCount = TextBorderStyle.None,
            uint bColor = 0,
            string rImage = null,
            string rSprite = null,
            string pLink = null,
            ImageZoom rImageZoom = null,
            TextDrawable drawable = null)
        {
            this.fontColor = fColorRGBA;
            this.fontSize = fSize;
            this.fontName = fName;
            this.fontStyle = fStyle;
            this.anchor = ta;

            this.resImage = rImage;
            this.resImageZoom = rImageZoom;
            this.resSprite = rSprite;

            this.borderCount = bCount;
            this.borderColor = bColor;

            this.link = pLink;

            this.drawable = drawable;
        }
        public TextAttribute(TextAttribute other)
        {
            this.fontColor = other.fontColor;
            this.fontSize = other.fontSize;
            this.fontName = other.fontName;
            this.fontStyle = other.fontStyle;
            this.anchor = other.anchor;

            this.borderCount = other.borderCount;
            this.borderColor = other.borderColor;

            this.resImage = other.resImage;
            this.resImageZoom = CUtils.TryClone(other.resImageZoom);
            this.resSprite = other.resSprite;

            this.link = other.link;
            this.drawable = other.drawable;
        }


        public object Clone()
        {
            return new TextAttribute(this);
        }

        public override bool Equals(object obj)
        {
            if (obj is TextAttribute)
            {
                return this.Equals(obj as TextAttribute);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(TextAttribute other)
        {
            if (other == this)
                return true;
            if (other == null)
                return false;

            if (this.fontColor != other.fontColor)
                return false;
            if (this.fontSize != other.fontSize)
                return false;
            if (!object.Equals(this.fontName, other.fontName))
                return false;
            if (this.fontStyle != other.fontStyle)
                return false;
            if (this.anchor != other.anchor)
                return false;

            if (this.borderCount != other.borderCount)
                return false;
            if (this.borderColor != other.borderColor)
                return false;

            if (!string.Equals(this.resImage, other.resImage))
                return false;
            if (!string.Equals(this.resSprite, other.resSprite))
                return false;

            if (!ImageZoom.Equals(this.resImageZoom, other.resImageZoom))
                return false;

            if (!string.Equals(this.link, other.link))
                return false;

            if (drawable != null && !drawable.Equals(other.drawable))
                return false;

            if (drawable == null && other.drawable != null)
            {
                return false;
            }
            return true;
        }

        public bool IsValid()
        {
            if (fontColor != Color.COLOR_NULL)
                return true;
            if (fontSize != 0)
                return true;
            if (fontName != null)
                return true;
            if (anchor != RichTextAlignment.taNA)
                return true;

            if (borderCount > 0)
                return true;
            if (borderColor != 0)
                return true;

            if (!string.IsNullOrEmpty(resImage))
                return true;
            if (!string.IsNullOrEmpty(resSprite))
                return true;

            if (resImageZoom != null)
                return true;

            if (!string.IsNullOrEmpty(link))
                return true;

            if (drawable != null)
                return true;

            return false;
        }

        public void Combine(TextAttribute other, bool isCover = true)
        {
            if (other.fontColor != 0)
            {
                this.fontColor = other.fontColor;
            }
            if (other.fontSize != 0)
            {
                this.fontSize = other.fontSize;
            }
            if (other.fontName != null)
            {
                this.fontName = other.fontName;
            }
            if (other.fontStyle != 0)
            {
                this.fontStyle = other.fontStyle;
            }
            if (other.anchor != RichTextAlignment.taNA)
            {
                this.anchor = other.anchor;
            }

            if (other.borderCount > 0)
            {
                this.borderCount = other.borderCount;
            }
            if (other.borderColor != 0)
            {
                this.borderColor = other.borderColor;
            }

            if (!string.IsNullOrEmpty(other.resImage))
            {
                this.resImage = other.resImage;
            }
            if (!string.IsNullOrEmpty(other.resSprite))
            {
                this.resSprite = other.resSprite;
            }

            if (other.resImageZoom != null)
            {
                this.resImageZoom = CUtils.TryClone(other.resImageZoom);
            }

            if (other.drawable != null)
            {
                this.drawable = CUtils.TryClone(other.drawable);
            }

            if (isCover && !string.IsNullOrEmpty(other.link))
            {
                this.link = other.link;
            }
            else if (!isCover)
            {
                this.link += other.link;
            }
        }

        public static TextAttribute Combine(TextAttribute src, TextAttribute dst)
        {
            TextAttribute ret = new TextAttribute(src);
            ret.Combine(dst);
            return ret;
        }

    }

    //------------------------------------------------------------------------------------------------------------------------


    //------------------------------------------------------------------------------------------------------------------------


    public class ImageZoomParser : TypeParserAdapter
    {
        public Type ParserType => typeof(ImageZoom);
        public bool IsAssignableFrom { get => false; }
        public ImageZoomParser() { }
        public bool Accept(Type type) { return ParserType == type; }
        public bool TryParse(string text, out object ret)
        {
            ret = ImageZoom.FromString(text);
            return ret != null;
        }
        public string ToString(object obj)
        {
            return ImageZoom.ToString(obj as ImageZoom);
        }
    }
    public class ImageZoom : ICloneable
    {
        static ImageZoom()
        {
            Parser.RegistParser(new ImageZoomParser());
        }

        public enum ImageFill
        {
            Clamp,
            Repeat,
        }
        public ImageFill Filling = ImageFill.Clamp;
        public float Width;
        public float Height;
        public object Clone()
        {
            ImageZoom ret = new ImageZoom();
            ret.Filling = this.Filling;
            ret.Width = this.Width;
            ret.Height = this.Height;
            return ret;
        }
        public static bool Equals(ImageZoom a, ImageZoom b)
        {
            if (a != null && b != null)
            {
                if (a.Filling != b.Filling)
                    return false;
                if (a.Width != b.Width)
                    return false;
                if (a.Height != b.Height)
                    return false;
                return true;
            }
            return a == b;
        }
        public override string ToString()
        {
            return ToString(this);
        }

        public static ImageZoom FromString(string text)
        {
            string[] kvs = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (kvs.Length >= 2)
            {
                ImageZoom ret = new ImageZoom();
                ret.Width = Parser.ParseFloat(kvs[0]);
                ret.Height = Parser.ParseFloat(kvs[1]);
                if (kvs.Length >= 3)
                {
                    ret.Filling = (ImageFill)Enum.Parse(typeof(ImageFill), kvs[2], true);
                }
                return ret;
            }
            return null;
        }
        public static string ToString(ImageZoom obj)
        {
            if (obj != null)
            {
                return string.Format("{0},{1},{2}", obj.Width, obj.Height, obj.Filling);
            }
            return null;
        }
    }


    //------------------------------------------------------------------------------------------------------------------------
    /////////////////////////////////////////////////////////////////////////////////////////////
    // 
    /////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 含有属性的文字
    /// </summary>
    public class AttributedString : ICloneable
    {
        private string text = "";

        private List<TextAttribute> attributes = new List<TextAttribute>();

        public AttributedString()
        {
        }
        public AttributedString(string other, TextAttribute ta)
        {
            Append(other, ta);
        }

        public object Clone()
        {
            AttributedString ret = new AttributedString();
            ret.text = this.text;
            ret.attributes = CUtils.CloneList<TextAttribute>(this.attributes);
            return ret;
        }

        public override bool Equals(object obj)
        {
            if (obj is AttributedString)
            {
                AttributedString other = obj as AttributedString;
                if (other.text.Equals(this.text))
                {
                    for (int i = text.Length - 1; i >= 0; --i)
                    {
                        if (!other.attributes[i].Equals(this.attributes[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool SetAttribute(int index, int len, TextAttribute ta)
        {
            if (index + len <= attributes.Count)
            {
                int end = index + len;
                for (int i = index; i < end; ++i)
                {
                    attributes[i] = ta;
                }
                return true;
            }
            return false;
        }

        public AttributedString AddAttribute(TextAttribute attribute, bool isCover = true)
        {
            return AddAttribute(attribute, 0, text.Length, isCover);
        }

        public AttributedString AddAttribute(TextAttribute attribute, int beginIndex, int count, bool isCover = true)
        {
            int endIndex = beginIndex + count;
            for (int i = beginIndex; i < endIndex; ++i)
            {
                attributes[i].Combine(attribute, isCover);
            }
            return this;
        }

        public AttributedString Append(AttributedString other)
        {
            if (other != null && !string.IsNullOrEmpty(other.text))
            {
                text += other.text;
                for (int i = 0; i < other.text.Length; ++i)
                {
                    attributes.Add(other.attributes[i]);
                }
            }
            return this;
        }

        public AttributedString Append(string other)
        {
            if (attributes.Count > 0)
            {
                TextAttribute lastAttr = attributes[attributes.Count - 1];
                Append(other, lastAttr);
            }
            else
            {
                Append(other, new TextAttribute(Color.COLOR_WHITE, 12));
            }
            return this;
        }

        public AttributedString Append(string other, TextAttribute ta)
        {
            if (!string.IsNullOrEmpty(other))
            {
                text += other;
                for (int i = 0; i < other.Length; ++i)
                {
                    attributes.Add(ta);
                }
            }
            return this;
        }

        public AttributedString DeleteString(int beginIndex, int count)
        {
            text = text.Remove(beginIndex, count);
            attributes.RemoveRange(beginIndex, count);
            return this;
        }

        public AttributedString ClearString()
        {
            text = "";
            attributes.Clear();
            return this;
        }

        public int Length
        {
            get
            {
                return text.Length;
            }
        }

        override public string ToString()
        {
            return text;
        }
        public string Substring(int start, int count)
        {
            return text.Substring(start, count);
        }
        public string Substring(int start)
        {
            return text.Substring(start);
        }
        public char GetChar(int index)
        {
            if (index < text.Length)
            {
                return text[index];
            }
            return default(char);
        }

        public TextAttribute GetAttribute(int index)
        {
            if (index < attributes.Count)
            {
                return attributes[index];
            }
            return null;
        }
        public delegate void ForEachAttributesTextAction(int start, int count, TextAttribute attribute);
        public void ForEachAttributesText(ForEachAttributesTextAction action)
        {
            if (attributes.Count > 0)
            {
                var ta = attributes[0];
                var start = 0;
                for (int i = 1; i < attributes.Count; i++)
                {
                    if (!ta.Equals(attributes[i]))
                    {
                        action(start, i - start, ta);
                        start = i;
                    }
                    ta = attributes[i];
                }
                int endIndex = attributes.Count - 1;
                if (start <= endIndex)
                {
                    action(start, attributes.Count - start, attributes[start]);
                }
            }
        }
    }
    //------------------------------------------------------------------------------------------------------------------------

    //------------------------------------------------------------------------------------------------------------------------
    public class AttributedStringDecoder
    {
        protected Logger log = LoggerFactory.GetLogger("AttributedStringDecoder");

        public const string MF_TEXT_XML_KEY_ARGB = "argb";
        public const string MF_TEXT_XML_KEY_COLOR = "color";
        public const string MF_TEXT_XML_KEY_SIZE = "size";
        public const string MF_TEXT_XML_KEY_FACE = "face";
        public const string MF_TEXT_XML_KEY_STYLE = "style";

        public const string MF_TEXT_XML_KEY_B_COUNT = "border";
        public const string MF_TEXT_XML_KEY_B_COLOR = "bcolor";

        public const string MF_TEXT_XML_KEY_RES_IMG = "img";
        /// <summary>
        /// @"宽,高"
        /// img_zoom = "width,height"
        /// </summary>
        public const string MF_TEXT_XML_KEY_RES_IMAGE_ZOOM = "img_zoom";
        /// <summary>
        /// @"资源名,精灵名,动画ID"
        /// spr = "res/sprite.xml,sprite_name,anim"
        /// </summary>
        public const string MF_TEXT_XML_KEY_RES_SPR = "spr";


        public const string MF_TEXT_XML_KEY_LINK = "link";
        public const string MF_TEXT_XML_KEY_LINE_ANCHOR = "anchor";

        public const string MF_TEXT_XML_NODE_BREAK = "br";
        public const string MF_TEXT_XML_NODE_SPACE = "p";

        public virtual AttributedString CreateFromXML(XmlDocument xml, TextAttribute defaultTA = null)
        {
            AttributedString attr = new AttributedString();
            TextAttribute curAttr = (defaultTA != null) ? defaultTA : new TextAttribute(Color.COLOR_WHITE, 16);
            try
            {
                internalBuildXML(attr, xml.DocumentElement, curAttr);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return attr;
        }
        public virtual AttributedString CreateFromXML(string text, TextAttribute defaultTA = null)
        {
            try
            {
                XmlDocument xml = XmlUtil.FromString(text);
                return CreateFromXML(xml, defaultTA);
            }
            catch
            {
                return new AttributedString(text, defaultTA);
            }
        }

        protected virtual void DecodeAttribute(XmlElement node, XmlAttribute x_attr, TextAttribute attr)
        {
            switch (x_attr.Name)
            {
                case MF_TEXT_XML_KEY_COLOR:
                    {
                        string kv = node.GetAttribute(MF_TEXT_XML_KEY_COLOR);
                        uint argb = uint.Parse(kv, System.Globalization.NumberStyles.HexNumber);
                        attr.fontColor = Color.FromARGB(argb);
                    }
                    break;
                case MF_TEXT_XML_KEY_SIZE:
                    {
                        attr.fontSize = Parser.ParseFloat(node.GetAttribute(MF_TEXT_XML_KEY_SIZE));
                    }
                    break;
                case MF_TEXT_XML_KEY_STYLE:
                    {
                        string style = node.GetAttribute(MF_TEXT_XML_KEY_STYLE);
                        int value;
                        if (Parser.TryParseInt(style, out value))
                        {
                            attr.fontStyle = (TextFontStyle)value;
                        }
                        else
                        {
                            attr.fontStyle = (TextFontStyle)Enum.Parse(typeof(TextFontStyle), style);
                        }
                    }
                    break;
                case MF_TEXT_XML_KEY_FACE:
                    {
                        attr.fontName = node.GetAttribute(MF_TEXT_XML_KEY_FACE);
                    }
                    break;
                case MF_TEXT_XML_KEY_B_COUNT:
                    {
                        attr.borderCount = (Data.TextBorderStyle)Parser.ParseInt(node.GetAttribute(MF_TEXT_XML_KEY_B_COUNT));
                    }
                    break;
                case MF_TEXT_XML_KEY_B_COLOR:
                    {
                        string kv = node.GetAttribute(MF_TEXT_XML_KEY_B_COLOR);
                        uint argb = Parser.ParseUInt(kv, System.Globalization.NumberStyles.HexNumber);
                        attr.borderColor = Color.ToRGBA(argb);
                    }
                    break;
                case MF_TEXT_XML_KEY_RES_IMG:
                    {
                        attr.resImage = node.GetAttribute(MF_TEXT_XML_KEY_RES_IMG);
                    }
                    break;
                case MF_TEXT_XML_KEY_RES_SPR:
                    {
                        attr.resSprite = node.GetAttribute(MF_TEXT_XML_KEY_RES_SPR);
                    }
                    break;
                case MF_TEXT_XML_KEY_RES_IMAGE_ZOOM:
                    {
                        attr.resImageZoom = ImageZoom.FromString(node.GetAttribute(MF_TEXT_XML_KEY_RES_IMAGE_ZOOM));
                    }
                    break;
                case MF_TEXT_XML_KEY_LINK:
                    {
                        attr.link = node.GetAttribute(MF_TEXT_XML_KEY_LINK);
                    }
                    break;
                case MF_TEXT_XML_KEY_LINE_ANCHOR:
                    {
                        string value = node.GetAttribute(MF_TEXT_XML_KEY_LINE_ANCHOR);
                        try
                        {
                            attr.anchor = (RichTextAlignment)Enum.Parse(typeof(RichTextAlignment), value, true);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    }
                    break;
            }
            TextDrawable drawable = ITextDrawableFactory.Instance.CreateTextDrawable(x_attr.Name, x_attr.Value);
            if (drawable != null)
            {
                attr.drawable = drawable;
            }
        }

        private void internalBuildXML(AttributedString atext, XmlElement node, TextAttribute parentAttr)
        {
            string nname = node.Name;

            TextAttribute attr = new TextAttribute();
            foreach (XmlAttribute x_attr in node.Attributes)
            {
                DecodeAttribute(node, x_attr, attr);
            }
            attr = TextAttribute.Combine(parentAttr, attr);
            if (!attr.IsValid())
            {
                attr = parentAttr;
            }
            if (node.ChildNodes.Count > 0)
            {
                foreach (XmlNode e in node.ChildNodes)
                {
                    if (e is XmlText)
                    {
                        atext.Append(e.InnerText, attr);
                    }
                    else if (e is XmlWhitespace)
                    {
                        atext.Append(e.InnerText, attr);
                    }
                    else if (e is XmlCDataSection)
                    {
                        atext.Append(e.InnerText, attr);
                    }
                    else if (e is XmlElement)
                    {
                        internalBuildXML(atext, e as XmlElement, attr);
                    }
                }
            }
            if (string.Equals(nname, MF_TEXT_XML_NODE_BREAK))
            {
                atext.Append("\n", attr);
            }
            else if (string.Equals(nname, MF_TEXT_XML_NODE_SPACE))
            {
                atext.Append(" ", attr);
            }
        }

    }

}
