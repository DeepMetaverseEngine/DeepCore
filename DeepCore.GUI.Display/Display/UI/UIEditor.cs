
using DeepCore.GUI.Cell;
using DeepCore.GUI.Data;
using DeepCore.GUI.Editor;
using DeepCore.GUI.Loader;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DeepCore.GUI.Display.UI
{
    public class UIEditor : UIFactory, IDisposable
    {
        public static new UIEditor Instance { get; private set; }

        private string mResRoot;
        private Dictionary<string, AbstractLoader> map = new Dictionary<string, AbstractLoader>();

        public UIEditor(string resRoot)
        {
            Instance = this;
            this.mResRoot = resRoot;
        }

        public virtual float DefaultFontSize { get; set; } = 24;
        public virtual FontStyle DefaultFontStyle { get; set; } = FontStyle.STYLE_PLAIN;
        public virtual int RichTextBorderTimes { get; set; } = 1;

        public string GetRoot()
        {
            return mResRoot;
        }

        public void Dispose()
        {
            mResRoot = null;
            map.Clear();
            map = null;
        }

        public virtual UIComponent CreateComponent(string className)
        {
            switch (className)
            {
                  case Data.UIEditorMeta.UERoot_ClassName: return new UIRoot();
                  case Data.UIEditorMeta.UEButton_ClassName: return new UITextButton();
                  case Data.UIEditorMeta.UEToggleButton_ClassName: return new UIToggleButton();
                  case Data.UIEditorMeta.UEImageBox_ClassName: return new UIImageBox();
                  case Data.UIEditorMeta.UELabel_ClassName: return new UILabel();
                  case Data.UIEditorMeta.UECanvas_ClassName: return new UICanvas();
                  case Data.UIEditorMeta.UEGauge_ClassName: return new UIGauge();
                  case Data.UIEditorMeta.UEFileNode_ClassName: return new UIFileNode();
                  case Data.UIEditorMeta.UEScrollPan_ClassName: return new UIScrollPan();
                  case Data.UIEditorMeta.UETextBox_ClassName: return new UITextBox();
                  case Data.UIEditorMeta.UETextBoxHtml_ClassName: return new UITextBoxHtml();
                  case Data.UIEditorMeta.UETextInput_ClassName: return new UITextInput();
                  default: return new UICanvas();
            }
        }
        public virtual UIComponent CreateComponent(Data.UIComponentMeta meta)
        {
            UIComponent ui = CreateComponent(meta.ClassName);
            if (ui != null)
            {
                ui.DecodeFromMeta(this, meta);
            }
            return ui;
        }
        public virtual UIComponent CreateFromFile(string path)
        {
            var xml = XmlUtil.LoadXML(path);
            var meta = UIEditorMeta.CreateFromXml(xml);
            return CreateComponent(meta);
        }
        public virtual UILayout CreateLayout(Data.UILayoutMeta e)
        {
            if (e == null) return null;
            var layout = new UILayout();
            layout.DecodeFromMeta(this, e);
            return layout;
        }
        public virtual UILayout CreateLayoutByImg(string img_name)
        {
            var style = UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER;
            var layout = new UILayout();
            int clipSize = 0;
            string path = mResRoot + "/" + img_name;
            AbstractLoader loader = AddImage(img_name);
            layout.InitFromImage(style, loader.GetImage(path), clipSize);
            return layout;
        }
        public virtual CPJAtlas CreateAtlas(string path, string a_tg)
        {
            var loader = AddAtlas(path);
            path = mResRoot + "/" + path;
            return loader.GetAtlasResource(path).GetAtlas(a_tg);
        }
        public virtual AbstractLoader AddImage(string name)
        {
            AbstractLoader temp = null;
            if (!map.ContainsKey(name))
            {
                temp = new ImageLoader(name);
                map[name] = temp;
            }
            else
            {
                temp = map[name];
            }
            return temp;
        }
        public virtual AbstractLoader AddAtlas(string name)
        {
            AbstractLoader temp = null;
            if (!map.ContainsKey(name))
            {
                temp = new AtlasLoader(name);
                map[name] = temp;
            }
            else
            {
                temp = map[name];
            }
            return temp;
        }
        public virtual void CleanMap()
        {
            foreach (KeyValuePair<string, AbstractLoader> kvp in map)
            {
                kvp.Value.Dispose();
            }
            map.Clear();
        }
    }
}

