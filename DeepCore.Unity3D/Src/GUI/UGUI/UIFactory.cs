using DeepCore.GUI.Display.Text;
using System.Xml;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class UIFactory
    {
        static UIFactory()
        {
            new UIFactory();
        }
        public static UIFactory Instance { get; private set; }

        private UGUIAttributedStringDecoder mTextDecoder = new UGUIAttributedStringDecoder();
        private Font mDefaultFont;
        private int mDefaultFontBestFitMin;
        private int mDefaultFontBestFitMax;
        private Vector2 mDefaultCaretSize = new Vector2(2, 16);
        private TextGenerator mDefaultTextGenerator;

        protected UIFactory()
        {
            Instance = this;
            mDefaultTextGenerator = new TextGenerator();
            mDefaultFontBestFitMin = 12;
            mDefaultFontBestFitMax = 22;
        }

        public virtual Font DefaultFont
        {
            get
            {
                if (mDefaultFont == null)
                {
                    Debug.LogWarning("UIFactory DefaultFont is null, create new Font();");
                    mDefaultFont = new Font();
                }
                return mDefaultFont;
            }
            protected set
            {
                mDefaultFont = value;
            }
        }
        public virtual TextGenerator DefaultTextGenerator
        {
            get { return mDefaultTextGenerator; }
        }
        public int DefaultFontBestFitMin
        {
            get { return mDefaultFontBestFitMin; }
            set { mDefaultFontBestFitMin = value; }
        }
        public int DefaultFontBestFitMax
        {
            get { return mDefaultFontBestFitMax; }
            set { mDefaultFontBestFitMax = value; }
        }
        public Vector2 DefaultCaretSize
        {
            get { return mDefaultCaretSize; }
            set { mDefaultCaretSize = value; }
        }

        virtual public AttributedString DecodeAttributedString(XmlDocument doc, TextAttribute defaultTA = null)
        {
            return mTextDecoder.CreateFromXML(doc, defaultTA);
        }
        virtual public AttributedString DecodeAttributedString(string doc, TextAttribute defaultTA = null)
        {
            return mTextDecoder.CreateFromXML(doc, defaultTA);
        }
        virtual public UGUIRichTextLayer CreateRichTextLayer(DisplayNode parent, bool use_bitmap)
        {
            return new UGUIRichTextLayer(parent, use_bitmap);
        }

    }

    public class UGUIAttributedStringDecoder : AttributedStringDecoder
    {
        public override AttributedString CreateFromXML(string text, TextAttribute defaultTA = null)
        {
            return base.CreateFromXML(text, defaultTA);
        }
        protected override void DecodeAttribute(XmlElement node, XmlAttribute x_attr, TextAttribute attr)
        {
            base.DecodeAttribute(node, x_attr, attr);
        }

        #region TextConvert

        public const string UGUI_COLOR = "<color=";
        public const string UGUI_SIZE = "<size=";
        public const string UGUI_BOLD = "<b";
        public const string UGUI_ITALIC = "<i";

        private static string[][] color_map =
        {
           new string[] {"aqua"     , "ff00ffff" } ,
           new string[] {"black"    , "ff000000" } ,
           new string[] {"blue"     , "ff0000ff" } ,
           new string[] {"brown"    , "ffa52a2a" } ,
           new string[] {"cyan"     , "ff00ffff" } ,
           new string[] {"darkblue" , "ff0000a0" } ,
           new string[] {"fuchsia"  , "ffff00ff" } ,
           new string[] {"green"    , "ff008000" } ,
           new string[] {"grey"     , "ff808080" } ,
           new string[] {"lightblue", "ffadd8e6" } ,
           new string[] {"lime"     , "ff00ff00" } ,
           new string[] {"magenta"  , "ffff00ff" } ,
           new string[] {"maroon"   , "ff800000" } ,
           new string[] {"navy"     , "ff000080" } ,
           new string[] {"olive"    , "ff808000" } ,
           new string[] {"orange"   , "ffffa500" } ,
           new string[] {"purple"   , "ff800080" } ,
           new string[] {"red"      , "ffff0000" } ,
           new string[] {"silver"   , "ffc0c0c0" } ,
           new string[] {"teal"     , "ff008080" } ,
           new string[] {"white"    , "ffffffff" } ,
           new string[] {"yellow"   , "ffffff00" } ,
        };

        public delegate void Replace(ref string prefix, ref string value);

        static private bool TryReplaceUGUI(ref string text, ref int index, string prefix, Replace replace)
        {
            int color_begin = text.IndexOf(prefix, index);
            if (color_begin >= 0)
            {
                int end = text.IndexOf('>', color_begin);
                if (end >= 0)
                {
                    int fs = color_begin + prefix.Length;
                    string value = text.Substring(fs, end - fs);
                    replace(ref prefix, ref value);
                    string field = prefix + value;
                    index = color_begin + field.Length + 1;
                    text = text.Substring(0, color_begin) + field + text.Substring(end);
                    return true;
                }
            }
            return false;
        }

        static private void ReplaceColor(ref string prefix, ref string value)
        {
            prefix = "<color " + prefix.Substring(1, prefix.Length - 1);
            if (value.StartsWith("#"))
            {
                string aa = value.Substring(value.Length - 2, 2);
                value = "\"" + aa + value.Substring(1, value.Length - 3) + "\"";
                return;
            }
            else
            {
                for (int i = color_map.Length - 1; i >= 0; --i)
                {
                    if (value == color_map[i][0])
                    {
                        value = "\"" + color_map[i][1] + "\"";
                        return;
                    }
                }
            }
        }
        static private void ReplaceSize(ref string prefix, ref string value)
        {
            prefix = "<size " + prefix.Substring(1, prefix.Length - 1);
            value = "\"" + value + "\"";
        }
        static private void ReplaceBold(ref string prefix, ref string value)
        {
            prefix = "<b style=";
            value = "\"1\"";
        }
        static private void ReplaceItalic(ref string prefix, ref string value)
        {
            prefix = "<i style=";
            value = "\"2\"";
        }

        public static string UnityRichTextToXmlText(string text)
        {
            int pos = 0;
            while (TryReplaceUGUI(ref text, ref pos, UGUI_COLOR, ReplaceColor)) { }
            pos = 0;
            while (TryReplaceUGUI(ref text, ref pos, UGUI_SIZE, ReplaceSize)) { }
            pos = 0;
            while (TryReplaceUGUI(ref text, ref pos, UGUI_BOLD, ReplaceBold)) { }
            pos = 0;
            while (TryReplaceUGUI(ref text, ref pos, UGUI_ITALIC, ReplaceItalic)) { }
            return "<text>" + text + "</text>";
        }

        #endregion
    }
}
