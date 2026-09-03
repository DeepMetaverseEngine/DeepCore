using DeepCore.GUI.Cell;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.SceneGraph;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [Reflectible]
    public abstract partial class UIFactory
    {
        private static HashMap<Type, Type> UITypes;
        public bool IsEditor = true;
        public readonly string RootDir = "";
        public UIFactory(string rootDir)
        {
            this.RootDir = rootDir;
            if (UITypes == null)
            {
                lock (typeof(UIFactory))
                {
                    if (UITypes == null)
                    {
                        UITypes = new HashMap<Type, Type>();
                        var uitypes = ReflectionUtil.GetNoneVirtualSubTypes(typeof(UEComponentNode));
                        foreach (var uitype in uitypes)
                        {
                            if (uitype.TryGetAttribute<UEInstanceAttribute>(out var instance))
                            {
                                UITypes.Put(instance.MetaType, uitype);
                            }
                        }
                        new TextDrawableFactory()
                        {
                            ResourceRoot = rootDir,
                        };
                    }
                }
            }
        }
        public virtual bool ExistData(string path)
        {
            return Resource.ExistData(RootDir + path);
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        #region RichText ---------------------------------------------------------------------------------------------------------------------
        protected AttributedStringDecoder mTextDecoder = new AttributedStringDecoder();
        virtual public RichTextLayer CreateRichTextLayer(float width = 100, RichTextAlignment anchor = RichTextAlignment.taLEFT)
        {
            return new RichTextLayer(width, anchor)
            {
                ImageRoot = RootDir,
            };
        }
        virtual public AttributedString DecodeAttributedString(XmlDocument doc, TextAttribute defaultTA = null)
        {
            return mTextDecoder.CreateFromXML(doc, defaultTA);
        }
        virtual public AttributedString DecodeAttributedString(string xml, TextAttribute defaultTA = null)
        {
            return mTextDecoder.CreateFromXML(xml, defaultTA);
        }

        #endregion ---------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------
        #region Layout -----------------------------------------------------------------------------------------------------------------------
        virtual public UILayout CreateLayout(UILayoutMeta meta)
        {
            if (meta == null) return null;
            var ret = new UILayout(this, meta);
            //await ret.LoadAsync();
            return ret;
        }
        virtual public UITextLayer CreateTextLayer(string text, UIFontMeta font, UITextStyleMeta style)
        {
            var ret = new UITextLayer(this, text, font, style);
            //await ret.LoadAsync();
            return ret;
        }
        virtual public UIImageLayer CreateImageLayer(CPJAtlasMeta meta, UIImageStyleMeta style)
        {
            var ret = new UIImageLayer(this, meta, style);
            //await ret.LoadAsync();
            return ret;
        }

        #endregion ---------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------
        #region Resource ---------------------------------------------------------------------------------------------------------------------
        virtual public UIResourceImage AddImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            var ret = new UIResourceImage(RootDir + imagePath);
            return ret;
        }
        virtual public UIResourceCPJ AddCPJ(string cpjPath)
        {
            if (string.IsNullOrEmpty(cpjPath)) return null;
            return new UIResourceCPJ(RootDir + cpjPath);
        }
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------
        #region UINode -----------------------------------------------------------------------------------------------------------------------
        public UEComponentNode CreateUI(UEComponentMeta meta)
        {
            var node = this.DoCreateUI(meta);
            OnCreateNode?.Invoke(meta, node);
            return node;
        }
        protected virtual UEComponentNode DoCreateUI(UEComponentMeta meta)
        {
            if (meta == null) return null;
            //-----------------------------------------------------------------------------------------------
            if (meta is UERootMeta metaRoot) return new UERoot(this, metaRoot);
            if (meta is UECanvasMeta metaCanvas) return new UECanvas(this, metaCanvas);
            if (meta is UEScrollPanMeta metaScrollPan) return new UEScrollPan(this, metaScrollPan);
            ////-----------------------------------------------------------------------------------------------
            if (meta is UEReferenceNodeMeta metaReference) return new UEReferenceNode(this, metaReference);
            //-----------------------------------------------------------------------------------------------
            if (meta is UEImageBoxMeta metaImageBox) return new UEImageBox(this, metaImageBox);
            if (meta is UEGaugeMeta metaGauge) return new UEGauge(this, metaGauge);
            if (meta is UECheckBoxMeta metaCheckBox) return new UECheckBox(this, metaCheckBox);
            if (meta is UELabelMeta metaLabel) return new UELabel(this, metaLabel);
            if (meta is UERichTextBoxMeta metaRichText) return new UERichTextBox(this, metaRichText);
            if (meta is UETextBoxMeta metaTextBox) return new UETextBox(this, metaTextBox);
            if (meta is UETextButtonMeta metaButton) return new UETextButton(this, metaButton);
            if (meta is UETextInputMeta metaTextInput) return new UETextInput(this, metaTextInput);
            if (meta is UETextInputMultilineMeta metaInputMulti) return new UETextInputMultiline(this, metaInputMulti);
            if (meta is UEToggleButtonMeta metaToggle) return new UEToggleButton(this, metaToggle);
            //-----------------------------------------------------------------------------------------------
            if (meta is UETextListMeta metaTextList) return new UETextList(this, metaTextList);
            //-----------------------------------------------------------------------------------------------
            if (UITypes.TryGetValue(meta.GetType(), out var uetype))
            {
                if (DeepActivator.CreateInstance(uetype, new object[] { this, meta }) is UEComponentNode ret)
                {
                    return ret;
                }
            }
            return new UEDummy(this, meta);
        }
        #endregion ---------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------
        public event CreateNodeHandler OnCreateNode;
        public delegate void CreateNodeHandler(UEComponentMeta meta, UEComponentNode node);

        //------------------------------------------------------------------------------------------------------------------------------------
        public class DefaultUIFactory : UIFactory
        {
            public DefaultUIFactory(string rootDir) : base(rootDir)
            {
            }
        }
    }
}
