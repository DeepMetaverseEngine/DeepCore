using DeepCore;
using DeepCore.Log;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DeepCore.MPQ;
using DeepCore.Reflection;

namespace DeepCore.GUI
{
    [Reflectible]
    public abstract partial class UIFactory
    {
      
        static UIFactory()
        {
            new DefaultUIFactory();
            new TextDrawableFactory();
        }
        public static UIFactory Instance { get; private set; }

        protected AttributedStringDecoder mTextDecoder = new AttributedStringDecoder();

        public UIFactory()
        {
            Instance = this;
        }

        virtual public RichTextLayer CreateRichTextLayer(float width = 100, RichTextAlignment anchor = RichTextAlignment.taLEFT)
        {
            return new RichTextLayer(width, anchor);
        }

        virtual public AttributedString DecodeAttributedString(XmlDocument doc, TextAttribute defaultTA = null)
        {
            return mTextDecoder.CreateFromXML(doc, defaultTA);
        }

        private class DefaultUIFactory : UIFactory
        {

        }



    }

}
