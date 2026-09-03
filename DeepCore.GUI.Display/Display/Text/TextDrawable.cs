using DeepCore;
using DeepCore.GUI.Data;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.GUI.Display.Text
{
 


    //---------------------------------------------------------------------------------------------------------

    [Reflectible]
    public interface TextDrawable : BaseRichTextLayer.Drawable, ICloneable
    {
        bool Equals(TextDrawable obj);
        bool FromText(string text);
    }

    //---------------------------------------------------------------------------------------------------------

    [Reflectible]
    public abstract partial class ITextDrawableFactory
    {
        static ITextDrawableFactory() { new DefaultTextDrawableFactory(); }
        public static ITextDrawableFactory Instance { get; private set; }
        public ITextDrawableFactory()
        {
            Instance = this;
        }
        public abstract TextDrawable CreateTextDrawable(string name, string value);

        private class DefaultTextDrawableFactory : ITextDrawableFactory
        {
            public override TextDrawable CreateTextDrawable(string name, string value)
            {
                return null;
            }
        }
    }

    //---------------------------------------------------------------------------------------------------------

    [Reflectible]
    public partial class TextDrawableFactory : ITextDrawableFactory
    {
        public const string TYPE_IMAGE = "image";
        public const string TYPE_ATLAS = "atlas";
        public const string TYPE_SPRITE = "sprite";

        public string ResourceRoot { get; set; }

        protected HashMap<string, Image> mImageMap = new HashMap<string, Image>();
        protected HashMap<string, Cell.CPJResource> mResMap = new HashMap<string, Cell.CPJResource>();
        protected HashMap<string, Image> mSelfImageMap = new HashMap<string, Image>();
        protected HashMap<string, Cell.CPJResource> mSelfResMap = new HashMap<string, Cell.CPJResource>();

        private static TextDrawableFactory mInstance;

        public TextDrawableFactory()
        {
            ResourceRoot = "";
            mInstance = this;
        }

        public override TextDrawable CreateTextDrawable(string name, string value)
        {
            switch (name)
            {
                case TYPE_IMAGE:
                    {
                        ImageDraw ret = new ImageDraw();
                        if (ret.FromText(value)) return ret;
                    }
                    break;
                case TYPE_ATLAS:
                    {
                        AtlasDraw ret = new AtlasDraw();
                        if (ret.FromText(value)) return ret;
                    }
                    break;
                case TYPE_SPRITE:
                    {
                        SpriteDraw ret = new SpriteDraw();
                        if (ret.FromText(value)) return ret;
                    }
                    break;
            }
            return null;
        }


        public virtual Image LoadImage(string file)
        {
            Image img = mImageMap.Get(file);
            if (img == null)
            {
                img = GraphicsDriver.Instance.CreateImage(ResourceRoot + file);
                if (img != null)
                {
                    mSelfImageMap.Put(file, img);
                    mImageMap.Put(file, img);
                }
            }
            return img;
        }

        public virtual Cell.CPJResource LoadResource(string file)
        {
            Cell.CPJResource res = mResMap.Get(file);
            if (res == null)
            {
                res = Cell.CPJResource.CreateResource(ResourceRoot + file);
                if (res != null)
                {
                    mSelfResMap.Put(file, res);
                    mResMap.Put(file, res);
                }
            }
            return res;
        }

        public virtual void CacheImage(string file, Image img)
        {
            if (img != null)
            {
                mImageMap.Put(file, img);
            }
        }
        public virtual void CacheResource(string file, Cell.CPJResource res)
        {
            if (res != null)
            {
                mResMap.Put(file, res);
            }
        }

        public void Dispose()
        {
            mImageMap.Clear();
            mResMap.Clear();
            foreach (KeyValuePair<string, Image> kvp in mSelfImageMap)
            {
                kvp.Value.Dispose();
            }
            mSelfImageMap.Clear();
            foreach (KeyValuePair<string, Cell.CPJResource> kvp in mSelfResMap)
            {
                kvp.Value.Dispose();
            }
            mSelfResMap.Clear();
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// image = "/xxx/bbb/icon.png,32,32"
        /// </summary>
        public class ImageDraw : TextDrawable
        {
            public Image ImageSrc { get; private set; }
            public float CharWidth { get { return zoom_w; } }
            public float CharHeight { get { return zoom_h; } }

            private float zoom_w = 0;
            private float zoom_h = 0;

            public bool FromText(string text)
            {
                string[] kvs = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvs.Length > 0)
                {
                    ImageSrc = mInstance.LoadImage(kvs[0]);
                    if (ImageSrc != null)
                    {
                        zoom_w = ImageSrc.Width;
                        zoom_h = ImageSrc.Height;
                        if (kvs.Length >= 3)
                        {
                            Parser.TryParseFloat(kvs[1], out zoom_w);
                            Parser.TryParseFloat(kvs[2], out zoom_h);
                        }
                        return true;
                    }
                }
                return false;
            }
            public bool Equals(TextDrawable obj)
            {
                if (obj is ImageDraw)
                {
                    var b = obj as ImageDraw;
                    if (this.ImageSrc != b.ImageSrc) return false;
                    if (this.zoom_w != b.zoom_w) return false;
                    if (this.zoom_h != b.zoom_h) return false;
                    return true;
                }
                return false;
            }
            public object Clone()
            {
                ImageDraw ret = new ImageDraw();
                ret.ImageSrc = this.ImageSrc;
                ret.zoom_w = this.zoom_w;
                ret.zoom_h = this.zoom_h;
                return ret;
            }
            public void Render(Graphics g, RichTextLayer.Region rg, float x, float y, int deltaMS)
            {
                g.BeginImage(ImageSrc);
                g.DrawImageZoom(x, y, zoom_w, zoom_h);
            }

            public void Hide(BaseRichTextLayer.Region self, float x, float y) { }
            public void Dispose()
            {
            }
        }

        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// atlas = "/xxx/bbb/output/icons.xml,image_icon,32"
        /// </summary>
        public class AtlasDraw : TextDrawable
        {
            public Cell.CPJAtlas Atlas { get; private set; }
            public float CharWidth { get; private set; }
            public float CharHeight { get; private set; }

            private int tile_id = 0;

            public bool FromText(string text)
            {
                string[] kvs = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvs.Length >= 3)
                {
                    Cell.CPJResource res = mInstance.LoadResource(kvs[0]);
                    if (res != null)
                    {
                        this.Atlas = res.GetAtlas(kvs[1]);
                        if (Atlas != null && Parser.TryParseInt(kvs[2], out tile_id))
                        {
                            CharWidth = Atlas.getWidth(tile_id);
                            CharHeight = Atlas.getHeight(tile_id);
                            return true;
                        }
                    }
                }
                return false;
            }

            public bool Equals(TextDrawable obj)
            {
                if (obj is AtlasDraw)
                {
                    var b = obj as AtlasDraw;
                    if (this.Atlas != b.Atlas)
                        return false;
                    if (this.tile_id != b.tile_id)
                        return false;
                    return true;
                }
                return false;
            }

            public object Clone()
            {
                AtlasDraw ret = new AtlasDraw();
                ret.Atlas = this.Atlas;
                ret.tile_id = this.tile_id;
                ret.CharWidth = this.CharWidth;
                ret.CharHeight = this.CharHeight;
                return ret;
            }
            public void Render(Graphics g, RichTextLayer.Region rg, float x, float y, int deltaMS)
            {
                Atlas.render(g, tile_id, x, y, Trans.TRANS_NONE);
            }
            public void Hide(BaseRichTextLayer.Region self, float x, float y) { }
            public void Dispose()
            {
            }
        }


        //---------------------------------------------------------------------------------------------------------
        public class SpriteDraw : TextDrawable
        {
            public DeepCore.GUI.Cell.Game.CSprite SpriteSrc { get; private set; }
            public float CharWidth { get { return bounds.Width; } }
            public float CharHeight { get { return bounds.Height; } }

            private int anim;
            private int frame_count;
            private int frame = 0;
            private DeepCore.GUI.Cell.Game.CCD bounds;
            private int mFramPassMS = 0;
            private int mFrameMS = 0;
            public bool FromText(string text)
            {
                string[] kvs = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvs.Length >= 3)
                {
                    Cell.CPJResource res = mInstance.LoadResource(kvs[0]);
                    if (res != null)
                    {
                        this.SpriteSrc = res.GetSprite(kvs[1]);
                        if (SpriteSrc != null && Parser.TryParseInt(kvs[2], out anim))
                        {
                            this.bounds = SpriteSrc.getVisibleBounds(anim);
                            this.frame_count = SpriteSrc.getFrameCount(anim);
                            if (SpriteSrc.Data.FPS > 0)
                            {
                                mFrameMS = 1000 / SpriteSrc.Data.FPS;
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
            public bool Equals(TextDrawable obj)
            {
                if (obj is SpriteDraw)
                {
                    var b = obj as SpriteDraw;
                    if (this.SpriteSrc != b.SpriteSrc)
                        return false;
                    if (this.anim != b.anim)
                        return false;
                    return true;
                }
                return false;
            }
            public object Clone()
            {
                SpriteDraw ret = new SpriteDraw();
                ret.SpriteSrc = this.SpriteSrc;
                ret.anim = this.anim;
                ret.frame_count = this.frame_count;
                ret.bounds = this.bounds;
                return ret;
            }
            public void Render(Graphics g, RichTextLayer.Region rg, float x, float y, int deltaMS)
            {
                SpriteSrc.render(g, anim, frame, x - bounds.X1, y - bounds.Y1);
                if (mFrameMS > 0)
                {
                    mFramPassMS += deltaMS;
                    if (mFramPassMS > mFrameMS)
                    {
                        frame++;
                        mFramPassMS = 0;
                    }
                }
                else
                {
                    frame++;
                }
                frame = frame % frame_count;
            }
            
            public void Hide(BaseRichTextLayer.Region self, float x, float y) { }
            public void Dispose()
            {

            }
        }


        //---------------------------------------------------------------------------------------------------------
    }

}
