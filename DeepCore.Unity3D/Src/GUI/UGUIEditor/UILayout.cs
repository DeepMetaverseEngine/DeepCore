using DeepCore.GUI.Cell.Game;
using DeepCore.GUI.Data;
using DeepCore.GUI.Gemo;
using DeepCore.Unity3D.UGUI;
using UnityEngine;
using UnityImage = DeepCore.Unity3D.Impl.UnityImage;

namespace DeepCore.Unity3D.UGUIEditor
{
    public partial class UILayout
    {
        //--------------------------------------------------------------------------------

        internal UILayoutStyle mStyle = UILayoutStyle.NULL;
        internal UILayoutTrans mTrans = UILayoutTrans.TRANS_NONE;
        internal Color mFillColor;
        internal Color mBorderColor;
        //internal int mClipSize;
        //internal int mClipSize2;
        internal int mBorderSize;
        internal int mClipL, mClipR, mClipT, mClipB;
        internal bool mRepeat = false;

        internal UnityImage mImageSrc;
        internal Rectangle2D mImageRegion;
        internal CSpriteMeta mSprite;
        internal CSpriteController mSpriteController;
        internal int mSpriteAnimate;


        public UILayoutStyle Style { get { return mStyle; } }
        public UILayoutTrans Trans { get { return mTrans; } }
        //public float ClipSize { get { return mClipSize; } }
        //public float ClipSize2 { get { return mClipSize2; } }
        public float BorderSize { get { return mBorderSize; } }
        public float ClipL { get { return mClipL; } }
        public float ClipR { get { return mClipR; } }
        public float ClipT { get { return mClipT; } }
        public float ClipB { get { return mClipB; } }

        public UnityImage ImageSrc { get { return mImageSrc; } }
        public Color BorderColor { get { return mBorderColor; } }
        public Color FillColor { get { return mFillColor; } }
        public Rectangle2D ImageRegion { get { return mImageRegion; } }
        public CSpriteMeta Sprite { get { return mSprite; } }
        public int SpriteAnimate { get { return mSpriteAnimate; } }
        public CSpriteController SpriteController { get { return mSpriteController; } }

        public Vector2 PreferredSize
        {
            get
            {
                if (mImageRegion != null)
                {
                    return new Vector2(mImageRegion.width, mImageRegion.height);
                }
                if (mSprite != null)
                {
                    CCD cd = mSprite.getVisibleBounds(mSpriteAnimate);
                    return new Vector2(cd.Width, cd.Height);
                }
                return new Vector2(8, 8);
            }
        }
        public Texture2D MainTexture
        {
            get
            {
                if (mImageSrc != null)
                {
                    return mImageSrc.Texture2D;
                }
                else
                {
                    return null;
                }
            }
        }
        public Material MainMaterial
        {
            get
            {
                if (mImageSrc != null)
                {
                    return mImageSrc.TextureMaterial;
                }
                else
                {
                    return ImageGraphics.DefaultImageMaterial;
                }
            }
        }
        //--------------------------------------------------------------------------------
        
        public static UILayout CreateUILayoutImage(UILayoutStyle style, UnityImage src, int clipsize, Rectangle2D imageRegion = null)
        {
            UILayout ret = new UILayout();
            ret.mStyle = style;
            ret.mImageSrc = src;
            ret.mBorderSize = clipsize;
            ret.mClipL = clipsize;
            ret.mClipR = clipsize;
            ret.mClipT = clipsize;
            ret.mClipB = clipsize;
            if (imageRegion == null)
            {
                ret.mImageRegion = new Rectangle2D(0, 0, src.Width, src.Height);
            }
            else
            {
                ret.mImageRegion = imageRegion;
            }
            return ret;
        }
        public static UILayout CreateUILayoutImage(UILayoutStyle style, UnityImage src, int clipsize, int[] clips, Rectangle2D imageRegion = null)
        {
            UILayout ret = new UILayout();
            ret.mStyle = style;
            ret.mImageSrc = src;
            ret.mBorderSize = clipsize;
            ret.mClipL = clips[0];
            ret.mClipR = clips[1];
            ret.mClipT = clips[2];
            ret.mClipB = clips[3];
            if (imageRegion == null)
            {
                ret.mImageRegion = new Rectangle2D(0, 0, src.Width, src.Height);
            }
            else
            {
                ret.mImageRegion = imageRegion;
            }
            return ret;
        }
        public static UILayout CreateUILayoutSprite(CSpriteMeta spr, int anim)
        {
            UILayout ret = new UILayout();
            ret.mStyle = UILayoutStyle.SPRITE;
            ret.mSprite = spr;
            ret.mSpriteAnimate = anim;
            ret.mImageSrc = spr.Atlas.GetTile(0) as UnityImage;
            ret.mImageRegion = spr.Atlas.GetAtlasRegion(spr.getAvaliableTileID());
            ret.mSpriteController = new CSpriteController(ret.mSprite);
            ret.mSpriteController.SetCurrentAnimate(anim);
            ret.mSpriteController.IsAutoPlay = true;
            return ret;
        }
        public static UILayout CreateUILayoutColor(Color fillcolor, Color bordercolor, int borderSize = 1)
        {
            UILayout ret = new UILayout();
            ret.mStyle = UILayoutStyle.COLOR;
            ret.mFillColor = fillcolor;
            ret.mBorderColor = bordercolor;
            ret.mBorderSize = borderSize;
            return ret;
        }

        //--------------------------------------------------------------------------------

        protected internal virtual void DecodeFromXML(UIEditor editor, UILayoutMeta e)
        {
            this.mStyle = e.Style;
            this.mTrans = e.Trans;
            this.mFillColor = UIUtils.UInt32_ARGB_To_Color(e.BackColorARGB);
            this.mBorderColor = UIUtils.UInt32_ARGB_To_Color(e.BorderColorARGB);
            this.mBorderSize = e.BorderSize;
            this.mClipL = e.ClipL;
            this.mClipR = e.ClipR;
            this.mClipT = e.ClipT;
            this.mClipB = e.ClipB;
            this.mRepeat = e.Repeat;
            switch (mStyle)
            {
                case UILayoutStyle.NULL:
                case UILayoutStyle.COLOR:
                    break;
                case UILayoutStyle.SPRITE:
                    if (!string.IsNullOrEmpty(e.SpriteName) && e.SpriteName.StartsWith("@"))
                    {
                        mSprite = editor.ParseSpriteMeta(e.SpriteName, out mSpriteAnimate);
                        if (mSprite != null)
                        {
                            this.mImageSrc = mSprite.Atlas.GetTile(0) as UnityImage;
                            this.mImageRegion = mSprite.Atlas.GetAtlasRegion(mSprite.getAvaliableTileID());
                            this.mSpriteController = new CSpriteController(mSprite);
                            this.mSpriteController.SetCurrentAnimate(mSpriteAnimate);
                            this.mSpriteController.IsAutoPlay = true;
                        }
                    }
                    break;
                case UILayoutStyle.IMAGE_STYLE_ALL_8:
                case UILayoutStyle.IMAGE_STYLE_ALL_9:
                case UILayoutStyle.IMAGE_STYLE_H_012:
                case UILayoutStyle.IMAGE_STYLE_V_036:
                case UILayoutStyle.IMAGE_STYLE_HLM:
                case UILayoutStyle.IMAGE_STYLE_VTM:
                case UILayoutStyle.IMAGE_STYLE_BACK_4:
                case UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER:
                case UILayoutStyle.IMAGE_STYLE_FLIP_X:
                case UILayoutStyle.IMAGE_STYLE_FLIP_Y:
                    if (!string.IsNullOrEmpty(e.AtlasName) && e.AtlasName.StartsWith("#"))
                    {
                        this.mImageSrc = editor.ParseAtlasTile(e.AtlasName, out this.mImageRegion);
                    }
                    else if (!string.IsNullOrEmpty(e.ImageName))
                    {
                        this.mImageSrc = editor.GetImage(e.ImageName);
                        if (mImageSrc == null)
                        {
                            this.mImageSrc = editor.GetImageByFullPath(e.ImageName);
                        }
                        if (mImageSrc != null)
                        {
                            this.mImageRegion = new Rectangle2D(0, 0, mImageSrc.Width, mImageSrc.Height);
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        /*
        protected internal virtual void DecodeFromXML2(UIEditor editor, UILayoutMeta e)
        {
            this.mStyle = e.Style;
            this.mFillColor = UIUtils.UInt32_ARGB_To_Color(e.BackColorARGB);
            this.mBorderColor = UIUtils.UInt32_ARGB_To_Color(e.BorderColorARGB);
            this.mBorderSize = e.BorderSize;
            this.mClipL = e.ClipL;
            this.mClipR = e.ClipR;
            this.mClipT = e.ClipT;
            this.mClipB = e.ClipB;
            this.mRepeat = e.Repeat;
            this.mTrans = e.Trans;

            switch (mStyle)
            {
                case UILayoutStyle.NULL:
                case UILayoutStyle.COLOR:
                    break;
                case UILayoutStyle.SPRITE:
                    break;
                case UILayoutStyle.IMAGE_STYLE_ALL_8:
                case UILayoutStyle.IMAGE_STYLE_ALL_9:
                case UILayoutStyle.IMAGE_STYLE_H_012:
                case UILayoutStyle.IMAGE_STYLE_V_036:
                case UILayoutStyle.IMAGE_STYLE_HLM:
                case UILayoutStyle.IMAGE_STYLE_VTM:
                case UILayoutStyle.IMAGE_STYLE_BACK_4:
                case UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER:
                case UILayoutStyle.IMAGE_STYLE_FLIP_X:
                case UILayoutStyle.IMAGE_STYLE_FLIP_Y:
                    if (!string.IsNullOrEmpty(e.ImageName))
                    {
                        this.mImageSrc = editor.GetImageByFullPath(e.ImageName);
                        if (mImageSrc != null)
                        {
                            this.mImageRegion = new Rectangle2D(0, 0, mImageSrc.Width, mImageSrc.Height);
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        */
        //--------------------------------------------------------------------------------

    }

}

