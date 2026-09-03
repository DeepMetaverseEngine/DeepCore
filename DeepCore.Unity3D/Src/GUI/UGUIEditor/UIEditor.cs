using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Editor;
using DeepCore.GUI.Gemo;
using DeepCore.GUI.Loader;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Unity3D.UGUI;
using DeepCore.Unity3D.UGUIEditor.UI;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityDriver = DeepCore.Unity3D.Impl.UnityDriver;
using UnityImage = DeepCore.Unity3D.Impl.UnityImage;

namespace DeepCore.Unity3D.UGUIEditor
{
    public partial class UIEditor : UIFactory
    {
        #region _全局设置_

        public static bool GlobalUseBitmapText { get; set; }

        #endregion

        //------------------------------------------------------------------------------------------------

        public delegate UIComponent UIComponentCreater(UIComponentMeta meta);

        public sealed class Decoder
        {
            public UIEditor editor {get;private set; }
            public UIComponentCreater creater { get; private set; }

            internal Decoder(UIEditor e, UIComponentCreater r)
            {
                this.editor = e;
                this.creater = r;
            }
            public UIComponent CreateFromFile(string path)
            {
                return editor.CreateFromFile(path, this);
            }
            public UIComponent CreateFromMeta(UIComponentMeta meta)
            {
                return editor.CreateFromMeta(meta, this);
            }
            public UILayout CreateLayout(UILayoutMeta e)
            {
                return editor.CreateLayout(e);
            }
            public UnityEngine.Font CreateFont(string fontName)
            {
                return editor.CreateFont(fontName);
            }
        }

        //------------------------------------------------------------------------------------------------
        private readonly DeepCore.Log.Logger log;

        protected HashMap<string, AbstractLoader> mImageMap = new HashMap<string, AbstractLoader>();
        protected HashMap<string, UIComponentMeta> mMetaMap = new HashMap<string, UIComponentMeta>();

        public string ResRoot { get; private set; }
        public string Root { get; private set; }


        public UIEditor(string root)
        {
            if (!root.EndsWith("/")) { root += "/"; }
            this.Root = root;
            this.ResRoot = root + "res/";
            this.log = LoggerFactory.GetLogger("UIEditor");

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    break;
                //                 case RuntimePlatform.WindowsWebPlayer:
                //                     break;
                //                 case RuntimePlatform.WP8Player:
                //                     UnityDriver.UnityInstance.RedirectImage = this.RedirectImage_DXT;
                //                     break;
                case RuntimePlatform.Android:
                    UnityDriver.UnityInstance.RedirectImage = this.RedirectImage_ETC;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    UnityDriver.UnityInstance.RedirectImage = this.RedirectImage_PVR;
                    break;
            }
        }

        protected virtual UIComponent CreateComponent(UIComponentMeta meta)
        {
            if(UIEditorMeta.UIEditorRunTime)
            {
                UIComponent retValue;
                switch (meta.ClassName)
                {
                    case UIEditorMeta.UERoot_ClassName:
                        retValue = new UERoot();
                        break;
                    case UIEditorMeta.UEButton_ClassName:
                        retValue = new UETextButton();
                        break;
                    case UIEditorMeta.UEToggleButton_ClassName:
                        retValue = new UEToggleButton();
                        break;
                    case UIEditorMeta.UEImageBox_ClassName:
                        retValue = new UEImageBox();
                        break;
                    case UIEditorMeta.UECheckBox_ClassName:
                        retValue = new UECheckBox();
                        break;
                    case UIEditorMeta.UELabel_ClassName:
                        retValue = new UELabel();
                        break;
                    case UIEditorMeta.UECanvas_ClassName:
                        retValue = new UECanvas();
                        break;
                    case UIEditorMeta.UEGauge_ClassName:
                        retValue = new UEGauge();
                        break;
                    case UIEditorMeta.UEFileNode_ClassName:
                        retValue = new UEFileNode();
                        break;
                    case UIEditorMeta.UEScrollPan_ClassName:
                        retValue = new UEScrollPan();
                        break;
                    case UIEditorMeta.UETextInput_ClassName:
                        retValue = new UETextInput();
                        break;
                    case UIEditorMeta.UETextInputMultiline_ClassName:
                        retValue = new UETextInputMultiline();
                        break;
                    case UIEditorMeta.UETextBox_ClassName:
                        retValue = new UETextBox();
                        break;
                    case UIEditorMeta.UETextBoxHtml_ClassName:
                        retValue = new UETextBoxHtml();
                        break;
                    default:
                        retValue = new UECanvas();
                        break;
                }

                if (retValue != null)
                {
                    retValue.UnityObject.SetLayer(LayerMask.NameToLayer("UI"));
                }
                return retValue;
            }
            else
            {
                switch (meta.ClassName)
                {
                    case UIEditorMeta.UERoot_ClassName:
                        return new UERoot();
                    case UIEditorMeta.UEButton_ClassName:
                        return new UETextButton();
                    case UIEditorMeta.UEToggleButton_ClassName:
                        return new UEToggleButton();
                    case UIEditorMeta.UEImageBox_ClassName:
                        return new UEImageBox();
                    case UIEditorMeta.UECheckBox_ClassName:
                        return new UECheckBox();
                    case UIEditorMeta.UELabel_ClassName:
                        return new UELabel();
                    case UIEditorMeta.UECanvas_ClassName:
                        return new UECanvas();
                    case UIEditorMeta.UEGauge_ClassName:
                        return new UEGauge();
                    case UIEditorMeta.UEFileNode_ClassName:
                        return new UEFileNode();
                    case UIEditorMeta.UEScrollPan_ClassName:
                        return new UEScrollPan();
                    case UIEditorMeta.UETextInput_ClassName:
                        return new UETextInput();
                    case UIEditorMeta.UETextInputMultiline_ClassName:
                        return new UETextInputMultiline();
                    case UIEditorMeta.UETextBox_ClassName:
                        return new UETextBox();
                    case UIEditorMeta.UETextBoxHtml_ClassName:
                        return new UETextBoxHtml();
                    default:
                        return new UECanvas();
                }
            }
        }

        protected virtual UIComponent CreateFromFile(string path, Decoder decoder)
        {
            UIComponentMeta meta = AddMeta(Root + FormatSubPath(path));
            if (meta != null)
            {
                UIComponent ui = CreateFromMeta(meta, decoder);
                if (ui != null)
                {
                    ui.Name = path;
                }
                return ui;
            }
            return null;
        }

        protected virtual UIComponent CreateFromMeta(UIComponentMeta meta, Decoder decoder)
        {
            UIComponent ui = null;
            if (decoder.creater != null)
            {
                ui = decoder.creater(meta);
            }
            if (ui == null)
            {
                ui = CreateComponent(meta);
            }
            if (ui != null)
            {
                ui.DecodeFromXML(decoder, meta);
            }
            return ui;
        }


        /// <summary>
        /// 从路径加载UI界面
        /// </summary>
        /// <param name="path"></param>
        /// <param name="creater">自定义构建UI节点代理</param>
        /// <returns></returns>
        public virtual UIComponent CreateFromFile(string path, UIComponentCreater creater = null)
        {
            return CreateFromFile(path, new Decoder(this, creater));
        }

        /// <summary>
        /// 从元数据加载UI界面
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="creater">自定义构建UI节点代理</param>
        /// <returns></returns>
        public virtual UIComponent CreateFromMeta(UIComponentMeta meta, UIComponentCreater creater = null)
        {
            return CreateFromMeta(meta, new Decoder(this, creater));
        }
        
        public virtual UILayout CreateLayout(UILayoutMeta e)
        {
            if (e == null) return null;
            UILayout layout = new UILayout();
            layout.DecodeFromXML(this, e);
            return layout;
        }
        public virtual UILayout CreateLayoutByFullPath(UILayoutMeta e)
        {
            if (e == null) return null;
            UILayout layout = new UILayout();
            layout.DecodeFromXML(this, e);
            return layout;
        }


        public virtual UnityEngine.Font CreateFont(string fontName)
        {
            return DefaultFont;
        }

        //-------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 从字符串格式获取图片字
        /// </summary>
        /// <param name="image_font">"^number/output/number.xml|Texts"</param>
        /// <param name="text"></param>
        /// <returns></returns>
        public UGUI.ImageFontSprite ParseImageFont(string image_font, string text)
        {
            if (image_font.StartsWith("^"))
            {
                image_font = image_font.Substring(1);
                string[] args = image_font.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                CPJAtlas mAtlas = this.GetAtlas(a_name, a_tg);
                if (mAtlas != null)
                {
                    UGUI.ImageFontSprite ret = new UGUI.ImageFontSprite(image_font);
                    ret.Text = text;
                    ret.SetAtlas(mAtlas);
                    return ret;
                }
            }
            return null;
        }
        /// <summary>
        /// 从字符串格式获取图片字
        /// </summary>
        /// <param name="image_font">"^number/output/number.xml|Texts"</param>
        /// <param name="text"></param>
        /// <param name="imageFont"></param>
        /// <returns></returns>
        public bool ParseImageFont(string image_font, string text, ImageFontGraphics imageFont)
        {
            if (image_font.StartsWith("^"))
            {
                image_font = image_font.Substring(1);
                string[] args = image_font.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                CPJAtlas mAtlas = this.GetAtlas(a_name, a_tg);
                if (mAtlas != null)
                {
                    imageFont.Atlas = mAtlas;
                    imageFont.Text = text;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 从字符串格式获取图片精灵
        /// </summary>
        /// <param name="atlas_name">"#dynamic/effects/skill/skilllevelup.xml|skill_levelup1|21"</param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public UGUI.ImageSprite ParseImageSpriteFromAtlas(string atlas_name, Vector2 pivot)
        {
            if (atlas_name.StartsWith("#"))
            {
                Rectangle2D region;
                DeepCore.Unity3D.Impl.UnityImage src = ParseAtlasTile(atlas_name, out region);
                if (src != null)
                {
                    var ret = new UGUI.ImageSprite(atlas_name);
                    ret.SetImage(src, new Rect(region.x, region.y, region.width, region.height), pivot);
                    ret.mTransform.sizeDelta = new Vector2(region.width, region.height);
                    return ret;
                }
            }
            return null;
        }
        /// <summary>
        /// 从字符串格式获取图片精灵
        /// </summary>
        /// <param name="image_name">"static/off_down.png"</param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public UGUI.ImageSprite ParseImageSpriteFromImage(string image_name, Vector2 pivot)
        {
            if (!string.IsNullOrEmpty(image_name))
            {
                DeepCore.Unity3D.Impl.UnityImage src = GetImage(image_name);
                if (src != null)
                {
                    var ret = new UGUI.ImageSprite(image_name);
                    ret.SetImage(src, new Rect(0, 0, src.Width, src.Height), pivot);
                    ret.mTransform.sizeDelta = new Vector2(src.Width, src.Height);

                    return ret;
                }
            }
            return null;
        }


        /// <summary>
        /// 从字符串格式获取图集
        /// </summary>
        /// <param name="atlas_name">"#dynamic/effects/skill/skilllevelup.xml|skill_levelup1|21"</param>
        /// <param name="outRegion"></param>
        /// <returns></returns>
        public DeepCore.Unity3D.Impl.UnityImage ParseAtlasTile(string atlas_name, out Rectangle2D outRegion)
        {
            if (atlas_name.StartsWith("#"))
            {
                atlas_name = atlas_name.Substring(1);
                string[] args = atlas_name.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                int mAtlasTileID = int.Parse(args[2]);
                CPJAtlas mAtlas = this.GetAtlas(a_name, a_tg);
                if (mAtlas != null)
                {
                    DeepCore.Unity3D.Impl.UnityImage outImage = mAtlas.GetTile(mAtlasTileID) as UnityImage;
                    if (outImage != null)
                    {
                        outRegion = mAtlas.GetAtlasRegion(mAtlasTileID);
                        return outImage;
                    }
                }
            }
            outRegion = null;
            return null;
        }

        /// <summary>
        /// 从字符串格式获取图集
        /// </summary>
        /// <param name="atlas_name">"#dynamic/effects/skill/skilllevelup.xml|skill_levelup1|21"</param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public UnityEngine.Sprite ParseAtlasTile(string atlas_name, Vector2 pivot)
        {
            if (atlas_name.StartsWith("#"))
            {
                atlas_name = atlas_name.Substring(1);
                string[] args = atlas_name.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                int mAtlasTileID = int.Parse(args[2]);
                CPJAtlas mAtlas = this.GetAtlas(a_name, a_tg);
                if (mAtlas != null)
                {
                    DeepCore.Unity3D.Impl.UnityImage src = mAtlas.GetTile(mAtlasTileID) as UnityImage;
                    if (src != null)
                    {
                        Rectangle2D clip = mAtlas.GetAtlasRegion(mAtlasTileID);
                        return UIUtils.CreateSprite(src, new Rect(clip.x, clip.y, clip.width, clip.height), pivot);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 从字符串格式获取精灵
        /// </summary>
        /// <param name="spr_name">"@actor_001010/output/actor.xml|actor_001010|001010|3"</param>
        /// <param name="animIndex"></param>
        /// <returns></returns>
        public CSpriteMeta ParseSpriteMeta(string spr_name, out int animIndex)
        {
            if (spr_name.StartsWith("@"))
            {
                spr_name = spr_name.Substring(1);
                string[] args = spr_name.Split('|');
                if (args.Length >= 4)
                {
                    string a_xml_name = args[0];
                    string a_img_name = args[1];
                    string a_spr_name = args[2];
                    animIndex = int.Parse(args[3]);
                    CPJResource cpj_res = GetCPJResource(a_xml_name);
                    if (cpj_res != null)
                    {
                        CSpriteMeta spr_meta = cpj_res.GetSpriteMeta(a_spr_name);
                        return spr_meta;
                    }
                }
            }
            animIndex = 0;
            return null;
        }


        //-------------------------------------------------------------------------------------------------------------------------------


        /// <summary>
        /// 获取 UI XML 对应的单张图片
        /// </summary>
        /// <param name="image_file_name"></param>
        /// <returns></returns>
        public DeepCore.Unity3D.Impl.UnityImage GetImage(string image_file_name)
        {
            string full_path = ResRoot + FormatSubPath(image_file_name);
            var img = AddImage(full_path);
            return img;
        }
        public DeepCore.Unity3D.Impl.UnityImage GetImageByFullPath(string image_file_name)
        {
            string full_path = image_file_name;
            var img = AddImage(full_path);
            return img;
        }

        /// <summary>
        /// 获取 UI XML 对应的CPJ的图集
        /// </summary>
        /// <param name="cpj_file_name"></param>
        /// <param name="atlas_name"></param>
        /// <returns></returns>
        public CPJAtlas GetAtlas(string cpj_file_name, string atlas_name)
        {
            var cpj = GetCPJResource(cpj_file_name);
            if (cpj != null)
            {
                return cpj.GetAtlas(atlas_name);
            }
            return null;
        }

        /// <summary>
        /// 获取 CPJ 资源
        /// </summary>
        /// <param name="cpj_file_name"></param>
        /// <returns></returns>
        public CPJResource GetCPJResource(string cpj_file_name)
        {
            string full_path = ResRoot + FormatSubPath(cpj_file_name);
            var cpj = AddAtlas(full_path);
            return cpj;
        }

        public CSpriteMeta GetSpriteMeta(string cpj_file_name, string spr_name)
        {
            CPJResource cpj_res = GetCPJResource(cpj_file_name);
            if (cpj_res != null)
            {
                CSpriteMeta spr_meta = cpj_res.GetSpriteMeta(spr_name);
                return spr_meta;
            }
            return null;
        }

        public virtual UIComponentMeta GetUIMeta(string path)
        {
            return AddMeta(Root + FormatSubPath(path));
        }

        //-------------------------------------------------------------------------------------------------------------------------------

        #region _富文本_

        public override UGUIRichTextLayer CreateRichTextLayer(DisplayNode parent, bool use_bitmap)
        {
            return new EditorRichTextLayer(this, parent, use_bitmap);
        }

        public class EditorRichTextLayer : UGUIRichTextLayer
        {
            private readonly UIEditor mEditor;

            public EditorRichTextLayer(UIEditor editor, DisplayNode parent, bool use_bitmap_font)
                : base(parent, use_bitmap_font)
            {
                this.mEditor = editor;
            }

            protected override Image AddImage(string file)
            {
                Image ret = mEditor.GetImage(file);
                if (ret == null)
                {
                    ret = base.AddImage(file);
                }
                return ret;
            }

            protected override CPJResource AddCPJResource(string file)
            {
                var ret = mEditor.GetCPJResource(file);
                if (ret == null)
                {
                    ret = base.AddCPJResource(file);
                }
                return ret;
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------------
        #region _图片缓冲_

        protected virtual string RedirectImage_ETC(string path)
        {
            int index = path.LastIndexOf('.');
            string etc;
            if (index < 0)
            {
                etc = path + ".etc.m3z";
            }
            else
            {
                etc = path.Substring(0, index) + ".etc.m3z";
            }
             
            if (Resource.ExistData(etc))
            {
                return etc;
            }
            return path;
        }
        protected virtual string RedirectImage_PVR(string path)
        {
            int index = path.LastIndexOf('.');
            string pvr;
            if (index < 0)
            {
                pvr = path + ".pvr.m3z";
            }
            else
            {
                pvr = path.Substring(0, index) + ".pvr.m3z";
            }
            if (Resource.ExistData(pvr))
            {
                return pvr;
            }
            return path;
        }
        protected virtual string RedirectImage_DXT(string path)
        {
            string dxt = path.Substring(0, path.LastIndexOf('.')) + ".dxt.m3z";
            if (Resource.ExistData(dxt))
            {
                return dxt;
            }
            return path;
        }

        /// <summary>
        /// 格式化完整路劲
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual string FormatPath(string path)
        {
            path = path.Replace('\\', '/');
            return path;
        }
        /// <summary>
        /// 格式化子路劲
        /// </summary>
        /// <param name="sub_path"></param>
        /// <returns></returns>
        protected virtual string FormatSubPath(string sub_path)
        {
            if (sub_path.StartsWith("/"))
            {
                return sub_path.Substring(1);
            }
            return sub_path;
        }


        /// <summary>
        /// 添加XML和二进制映射
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual UIComponentMeta AddMeta(string path)
        {
            UIComponentMeta ret = null;
            try
            {
                path = FormatPath(path);
                if (!mMetaMap.TryGetValue(path, out ret))
                {
#if UNITY_ANDROID || UNITY_IOS
                    string bin_path = path.Substring(0, path.LastIndexOf(".gui.xml")) + ".gui.bin";
                    if (Resource.ExistData(bin_path))
                    {
                        var input = Resource.LoadDataAsStream(bin_path);
                        if (input == null) { return null; }
                        try
                        {
                            ret = UIEditorMeta.CreateFromStream(input);
                            if (ret != null)
                            {
                                mMetaMap.Put(path, ret);
                                return ret;
                            }
                        }
                        finally
                        {
                            input.Dispose();
                        }
                    }
#endif
                    XmlDocument xml = XmlUtil.LoadXML(path);
                    if (xml != null)
                    {
                        ret = UIEditorMeta.CreateFromXml(xml);
                        if (ret != null)
                        {
                            mMetaMap.Put(path, ret);
                            return ret;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return ret;
        }
        /// <summary>
        /// 清理XML二进制映射
        /// </summary>
        public virtual void CleanMetaMap()
        {
            mMetaMap.Clear();
        }

        protected virtual UnityImage AddImage(string path)
        {
            try
            {
                path = FormatPath(path);
                AbstractLoader temp = null;
                if (!mImageMap.TryGetValue(path, out temp))
                {
                    temp = new ImageLoader(path);
                    mImageMap.Put(path, temp);
                }
                return temp.GetImage(path) as UnityImage;
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            return null;
        }

        protected virtual CPJResource AddAtlas(string path)
        {
            try
            {
                path = FormatPath(path);
                AbstractLoader temp = null;
                if (!mImageMap.TryGetValue(path, out temp))
                {
                    temp = new AtlasLoader(path);
                    mImageMap.Put(path, temp);
                }
                return temp.GetAtlasResource(path);
            }
            catch (Exception err)
            {
                log.Error(path + " " + err.Message, err);
            }
            return null;
        }

        /// <summary>
        /// 清理缓存的图片
        /// </summary>
        public virtual void CleanImageMap()
        {
            foreach (KeyValuePair<string, AbstractLoader> kvp in mImageMap)
            {
                kvp.Value.Dispose();
            }
            mImageMap.Clear();
        }

        /// <summary>
        /// 释放ui中标记为支持释放（默认）的UnityImage中的指定路径的Texture
        /// 当重新被使用时会自动重新加载
        /// </summary>
        public void ReleaseTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            string full_path = ResRoot + FormatSubPath(path);
            full_path = FormatPath(full_path);
            AbstractLoader temp;
            if (mImageMap.TryGetValue(full_path, out temp))
            {
                temp.ReleaseTexture();
            }
        }

        /// <summary>
        /// 释放ui中标记为支持释放（默认）的UnityImage中的Texture
        /// 当重新被使用时会自动重新加载
        /// </summary>
        public void ReleaseAllTexture()
        {
            foreach (var item in mImageMap)
            {
                item.Value.ReleaseTexture();
            }
        }
#endregion

    }
}
