using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.GUI.Cell;
using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.UI;
using DeepCore.GUI.Gemo;

namespace DeepCore.GUI.Display.UI
{
    public class UILayout : IDisposable
    {
        private Image mSrc = null;
        private CPJAtlas mAtlas = null;
        private CSpriteMeta mSprMeta = null;
        private int mCurFrame = 0;
        private int mCurAnimate;
        private float mSprTx = 0;
        private float mSprTy = 0;

        public Rectangle2D ImageRegion { get; private set; }
        public bool IsAutoPlay { get; set; } = true;
        public uint Color { set; get; }
        public int BorderSize { set; get; }
        public int ClipL { set; get; }
        public int ClipR { set; get; }
        public int ClipT { set; get; }
        public int ClipB { set; get; }
        public string EditName { get; set; }
        public UILayoutStyle Style { set; get; } = UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER;

        public UILayout(UILayoutStyle style = UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER)
        {
            this.Style = style;
        }
        public void Dispose()
        {
            this.ImageRegion = null;
            this.mAtlas = null;
            this.mSprMeta = null;
            this.mSrc = null;
        }
        public UILayout Clone()
        {
            return new UILayout().SetInfo(this);
        }
        public UILayout SetInfo(UILayout ret)
        {
            this.Style = ret.Style;
            this.BorderSize = ret.BorderSize;
            this.ClipL = ret.ClipL;
            this.ClipR = ret.ClipR;
            this.ClipT = ret.ClipT;
            this.ClipB = ret.ClipB;
            this.ImageRegion = ret.ImageRegion;
            this.mSrc = ret.mSrc;
            return this;
        }
        public void SetImage(Image image, UILayoutStyle style, int clipSize, Rectangle2D region = null)
        {
            this.mSrc = image;
            this.Style = style;
            this.BorderSize = clipSize;
            this.ClipL = clipSize;
            this.ClipR = clipSize;
            this.ClipT = clipSize;
            this.ClipB = clipSize;
            if (region == null && image != null)
            {
                region = new Rectangle2D(0, 0, image.Width, image.Height);
            }
            this.ImageRegion = region;
        }
        public void InitFromImage(UILayoutStyle style, Image image, int clipSize)
        {
            this.Style = style;
            this.mSrc = image;
            this.BorderSize = clipSize;
            this.ClipL = clipSize;
            this.ClipR = clipSize;
            this.ClipT = clipSize;
            this.ClipB = clipSize;
            this.ImageRegion = new Rectangle2D(0, 0, image.Width, image.Height);
        }
        public void InitFromAtlas(UILayoutStyle style, CPJAtlas atlas, int tid)
        {
            this.mAtlas = atlas;
            this.mSrc = atlas.GetTile(tid);
            this.Style = style;
            this.ImageRegion = atlas.GetAtlasRegion(tid);
        }
        public void InitFromSprite(UILayoutStyle style, CSpriteMeta meta, int anim)
        {
            this.Style = style;
            this.mSprMeta = meta;
            this.mCurAnimate = anim;
            var tid = meta.getAvaliableTileID();
            this.mAtlas = meta.Atlas;
            this.mSrc = meta.Atlas.GetTile(tid);
            this.ImageRegion = meta.Atlas.GetAtlasRegion(tid);
        }
        public void SetAtlasTile(int index)
        {
            if (mAtlas != null)
            {
                this.mSrc = mAtlas.GetTile(index);
                this.ImageRegion = mAtlas.GetAtlasRegion(index);
            }
        }
        //----------------------------------------------------------------------------------------------

        public void Render(Graphics g, float w, float h)
        {
            if (Style == UILayoutStyle.NULL)
            {
                return;
            }
            else if (Style == UILayoutStyle.COLOR)
            {
                g.SetColor(Color);
                g.FillRect(0, 0, w, h);
            }
            else if (mSprMeta != null)
            {
                mSprTx = w * 0.5f;
                mSprTy = h * 0.5f;
                mSprMeta.render(g, mCurAnimate, mCurFrame, mSprTx, mSprTy);
            }
            else if (mSrc != null)
            {
                g.BeginImage(mSrc);
                switch (Style)
                {
                    case UILayoutStyle.IMAGE_STYLE_ALL_8:
                    case UILayoutStyle.IMAGE_STYLE_ALL_9:
                        RenderAll8(g, w, h);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_BACK_4:
                        RenderBack4(g, w, h);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER:
                        RenderBack4Center(g, w, h);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_H_012:
                        RenderH012(g, w, h);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_V_036:
                        RenderV036(g, w, h);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_HLM:
                        break;
                    case UILayoutStyle.IMAGE_STYLE_VTM:
                        break;
                    default:
                        break;
                }
            }
        }

        private void RenderAll8(Graphics g, float w, float h)
        {
            float b = ClipSize;
            float b2 = ClipSize * 2;
            float ix = ImageRegion.x;
            float iy = ImageRegion.y;
            float iw = ImageRegion.width;
            float ih = ImageRegion.height;
            float twb2 = w - b2;
            float thb2 = h - b2;
            float iwb2 = iw - b2;
            float ihb2 = ih - b2;
            g.DrawRegion(ix + b, iy + b, iwb2, ihb2, b, b, twb2, thb2);
            Render0123_5678(g, w, h);
        }

        private void RenderAll9(Graphics g, float w, float h)
        {
            float b = ClipSize;
            float b2 = ClipSize * 2;
            float ix = ImageRegion.x;
            float iy = ImageRegion.y;
            float iw = ImageRegion.width;
            float ih = ImageRegion.height;
            float twb2 = w - b2;
            float thb2 = h - b2;
            float iwb2 = iw - b2;
            float ihb2 = ih - b2;
            g.DrawRegion(ix + b, iy + b, iwb2, ihb2, b, b, twb2, thb2);
            Render0123_5678(g, w, h);
        }

        private void RenderH012(Graphics g, float w, float h)
        {
            float b = ClipSize;
            float b2 = ClipSize * 2;
            if (w < b2)
            {
                b = w / 2;
                b2 = w;
            }

            float ix = ImageRegion.x;
            float iy = ImageRegion.y;
            float iw = ImageRegion.width;
            float ih = ImageRegion.height;
            float twb2 = w - b2;
            float thb2 = h - b2;
            float iwb2 = iw - b2;
            float ihb2 = ih - b2;
            // top
            if (twb2 > 0)
            {
                g.DrawRegion(ix + b, iy + 0, iwb2, ih, b, 0, twb2, h);
            }
            if (w > 0)
            {
                g.DrawRegion(ix + 0, iy + 0, b, ih, 0, 0, b, h);
                g.DrawRegion(ix + iw - b, iy + 0, b, ih, w - b, 0, b, h);
            }

        }
        private void RenderV036(Graphics g, float w, float h)
        {
            float b = ClipSize;
            float b2 = ClipSize * 2;
            float ix = ImageRegion.x;
            float iy = ImageRegion.y;
            float iw = ImageRegion.width;
            float ih = ImageRegion.height;
            float twb2 = w - b2;
            float thb2 = h - b2;
            float iwb2 = iw - b2;
            float ihb2 = ih - b2;

            // left
            g.DrawRegion(ix, iy + b, iw, ihb2, 0, b, w, thb2);

            g.DrawRegion(ix, iy, iw, b, 0, 0, w, b);
            g.DrawRegion(ix, iy + ih - b, iw, b, 0, h - b, w, b);
        }

        private void RenderHLM(Graphics g, float w, float h)
        {

        }

        private void RenderVTM(Graphics g, float w, float h)
        {

        }

        private void RenderBack4(Graphics g, float w, float h)
        {
            g.DrawRegion(ImageRegion.x, ImageRegion.y, ImageRegion.width, ImageRegion.height, 0, 0, w, h);
        }

        private void RenderBack4Center(Graphics g, float w, float h)
        {
            float iw = ImageRegion.width;
            float ih = ImageRegion.height;
            float tx = (w - iw) * 0.5f;
            float ty = (h - ih) * 0.5f;
            g.DrawRegion(ImageRegion.x, ImageRegion.y, ImageRegion.width, ImageRegion.height, tx, ty, iw, ih);
        }

        private void Render0123_5678(Graphics g, float w, float h)
        {
            float b = ClipSize;
            float b2 = ClipSize * 2;
            float twb2 = w - b2;
            float thb2 = h - b2;
            float sx = ImageRegion.x;
            float sy = ImageRegion.y;
            float sw = ImageRegion.width;
            float sh = ImageRegion.height;
            float swb2 = sw - b2;
            float shb2 = sh - b2;

            // top bottom
            g.DrawRegion(sx + b, sy + 0, swb2, b, b, 0, twb2, b);
            g.DrawRegion(sx + b, sy + sh - b, swb2, b, b, h - b, twb2, b);
            // left right
            g.DrawRegion(sx + 0, sy + b, b, shb2, 0, b, b, thb2);
            g.DrawRegion(sx + sw - b, sy + b, b, shb2, w - b, b, b, thb2);

            g.DrawRegion(sx + 0, sy + 0, b, b, Trans.TRANS_NONE, 0, 0);
            g.DrawRegion(sx + sw - b, sy + 0, b, b, Trans.TRANS_NONE, w - b, 0);
            g.DrawRegion(sx + 0, sy + sh - b, b, b, Trans.TRANS_NONE, 0, h - b);
            g.DrawRegion(sx + sw - b, sy + sh - b, b, b, Trans.TRANS_NONE, w - b, h - b);
        }

        public void RenderRegion(Graphics g, float sx, float sy, float sw, float sh, float tx, float ty, float dw, float dh)
        {
            if (Style == UILayoutStyle.COLOR)
            {
                g.SetColor(Color);
                g.FillRect(tx, ty, dw, dh);
            }
            else if (mSrc != null)
            {
                g.BeginImage(mSrc);
                if (Style == UILayoutStyle.IMAGE_STYLE_H_012)
                {
                    g.PushTransform();
                    g.Translate(tx, ty);
                    RenderH012(g, dw, dh);
                    g.PopTransform();
                }
                else
                {
                    g.DrawRegion(sx, sy, sw, sh, tx, ty, dw, dh);
                }
            }

        }

        public void RenderCircle(Graphics g, float startAngle, float endAngle)
        {
            if (mSrc != null)
            {
                g.BeginImage(mSrc);
                g.DrawImageEllipse(0, 0, mSrc.Width, mSrc.Height, startAngle, endAngle);
            }
        }


        public void PlayAnimate(string anim_name)
        {
            if (mSprMeta == null)
            {
                throw new Exception("UILayout mSprMeta can not be null");
            }
            int anim = mSprMeta.getAnimateIndex(anim_name);
            if (anim >= 0)
            {
                PlayAnimate(anim);
            }
        }

        /// <summary>
        /// 播放动画:anim动画名、times次数(-1=无限).
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="times"></param>
        /// <param name="callBack"></param>
        public void PlayAnimate(int anim)
        {
            if (mSprMeta == null)
            {
                throw new Exception("UILayout mSprMeta can not be null");
            }
            SetCurrentAnimate(anim);
            mCurFrame = 0;
            IsAutoPlay = true;
        }

        public void StopAnimate()
        {
            IsAutoPlay = false;
        }

        public void SetCurrentAnimate(string anim_name)
        {
            if (mSprMeta == null)
            {
                throw new Exception("UILayout mSprMeta can not be null");
            }
            int anim = mSprMeta.getAnimateIndex(anim_name);
            if (anim >= 0)
            {
                SetCurrentAnimate(anim);
            }
        }

        public void SetCurrentAnimate(int anim)
        {
            if (mSprMeta == null)
            {
                throw new Exception("UILayout mSprMeta can not be null");
            }
            mCurAnimate = anim;
            mCurAnimate = CMath.cycNum(mCurAnimate, 0, mSprMeta.getAnimateCount());
            mCurFrame = CMath.cycNum(mCurFrame, 0, mSprMeta.getFrameCount(mCurAnimate));
        }

        public void SetCurrentFrame(int frame)
        {
            if (mSprMeta == null)
            {
                throw new Exception("UILayout mSprMeta can not be null");
            }

            mCurFrame = CMath.cycNum(frame, 0, mSprMeta.getFrameCount(mCurAnimate));
        }

        public int GetCurrentFrame()
        {
            return mCurFrame;
        }

        public virtual void Update()
        {
            if (IsAutoPlay && mSprMeta != null)
            {
                mCurFrame = (mCurFrame + 1) % mSprMeta.getFrameCount(mCurAnimate);
            }
        }

        public bool IsEndFrame
        {
            get
            {
                if (mCurFrame + 1 >= mSprMeta.getFrameCount(mCurAnimate))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public virtual void DecodeFromMeta(UIEditor editor, Data.UILayoutMeta e)
        {
            this.Color = DeepCore.GUI.Display.Color.toRGBA(e.BackColorARGB);
            this.Style = (UILayoutStyle)(e.Style);
            this.ClipL = e.ClipSize;

            if (!string.IsNullOrEmpty(e.SpriteName))
            {
                this.EditName = e.SpriteName;
                if (e.SpriteName.StartsWith("@"))
                {
                    string[] args = e.SpriteName.Split('|');
                    string a_xml_name = args[0];
                    string a_img_name = args[1];
                    string a_spr_name = args[2];
                    int anim = int.Parse(args[3]);
                    string path = a_xml_name.Replace("@", "");
                    var loader = editor.AddAtlas(path);
                    path = string.Format("{0}/{1}", editor.GetRoot(), path);
                    CPJResource cpj_res = loader.GetAtlasResource(path);
                    CSpriteMeta spr_meta = cpj_res.GetSpriteMeta(a_spr_name);
                    this.InitFromSprite(Style, spr_meta, anim);
                }
            }
            else if (!string.IsNullOrEmpty(e.AtlasName))
            {
                this.EditName = e.AtlasName;
                string[] args = e.AtlasName.Split('|');
                string a_name = args[0];
                string a_tg = args[1];
                int a_tid = int.Parse(args[2]);
                string path = a_name;
                path = path.Replace("#", "");
                var loader = editor.AddAtlas(path);
                path = string.Format("{0}/{1}", editor.GetRoot(), path);
                this.InitFromAtlas(Style, loader.GetAtlasResource(path).GetAtlas(a_tg), a_tid);
            }
            else if (!string.IsNullOrEmpty(e.ImageName) && Style != UILayoutStyle.NULL)
            {
                this.EditName = e.ImageName;
                string path = string.Format("{0}/{1}", editor.GetRoot(), e.ImageName);
                var loader = editor.AddImage(e.ImageName);
                this.InitFromImage(Style, loader.GetImage(path), ClipSize);
            }
            else
            {
                this.EditName = string.Empty;
            }
        }



    }
}
