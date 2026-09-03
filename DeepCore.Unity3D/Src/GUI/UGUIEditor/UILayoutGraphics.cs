using DeepCore.GUI.Data;
using DeepCore.GUI.Gemo;
using DeepCore.Unity3D.UGUI;
using System;
using UnityEngine;
using UnityImage = DeepCore.Unity3D.Impl.UnityImage;

namespace DeepCore.Unity3D.UGUIEditor
{
    public partial class UILayoutGraphics : ImageGraphics
    {
        private UILayout mLayout;
        private float mAlpha = 1f;

        [SerializeField]
        private bool m_IsShowUILayout = true;
        public bool IsShowUILayout
        {
            get { return m_IsShowUILayout; }
            set
            {
                if (m_IsShowUILayout != value)
                {
                    m_IsShowUILayout = value;
                    base.SetVerticesDirty();
                }
            }
        }

        public bool HasUILayout
        {
            get
            {
                return mLayout != null;
            }
        }

        private Texture2D mLastLayoutTexture;

        public override Texture mainTexture
        {
            get { return (IsShowUILayout) ? mLayout.MainTexture : base.mainTexture; }
        }
        public float Alpha
        {
            get { return mAlpha; }
            set
            {
                if (value != mAlpha)
                {
                    mAlpha = value;
                    Color c = base.color;
                    c.a = value;
                    base.color = c;
                    this.SetAllDirty();
                }
            }
        }

        public UILayoutGraphics()
        {
        }

        public void UpdateSprite()
        {
            if (mLayout != null && mLayout.mSpriteController != null)
            {
                if (mLayout.mSpriteController.Update(Mathf.FloorToInt(Time.deltaTime * 1000)))
                {
                    this.SetVerticesDirty();
                }
            }
        }

        public UILayoutGraphics SetCurrentLayout(UILayout layout)
        {
            if (mLayout != layout || (mLayout != null && mLayout.MainTexture != mLastLayoutTexture))
            {
                this.mLayout = layout;
                if (layout != null)
                {
                    this.enabled = true;
                    mLastLayoutTexture = mLayout.MainTexture;
                    if (mLayout.mImageSrc != null && mLayout.mImageRegion != null)
                    {
                        SetImage(mLayout.mImageSrc, mLayout.mImageRegion, Vector2.zero);
                    }
                    if (layout.Style == UILayoutStyle.COLOR)
                    {
                        this.color = layout.FillColor;
                        this.material = ImageGraphics.DefaultImageMaterial;
                    }
                }
                else
                {
                    this.enabled = false;
                }
                this.SetAllDirty();
            }
            return this;
        }
        public UILayoutGraphics SetFillMode(FillMethod fill, int fillOrigin, bool fillClockwise = false, bool fillCenter = true)
        {
            base.type = Type.Filled;
            base.fillMethod = fill;
            base.fillOrigin = fillOrigin;
            base.fillClockwise = fillClockwise;
            base.fillCenter = fillCenter;
            this.m_IsShowUILayout = false;
            this.SetAllDirty();
            return this;
        }
        public UILayoutGraphics SetFillPercent(float percent)
        {
            base.fillAmount = DeepCore.CMath.Clamp(percent, 0, 100) / 100f;
            return this;
        }

        public override void CalculateLayoutInputHorizontal() { }
        public override void CalculateLayoutInputVertical() { }
        public override void OnAfterDeserialize() { }
        public override void OnBeforeSerialize() { }
        public override void SetNativeSize()
        {
            if (mLayout != null)
            {
                this.SetAllDirty();
            }
            else
            {
                base.SetNativeSize();
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            if (IsShowUILayout)
            {
                vh.Clear();
                using (var o = new HelperVBO(vh, this.color, this.mLayout))
                {
                    o.OnFillVBO(rectTransform.rect.size);
                }
            }
            else
            {
                //                 if (base.sprite == null && mLayout != null)
                //                 {
                //                     if (mLayout.mImageSrc != null && mLayout.mImageRegion != null)
                //                     {
                //                         if (base.sprite != null)
                //                         {
                //                             DeepCore.Unity3D.UnityHelper.Destroy(sprite);
                //                         }
                //                         base.sprite = UIUtils.CreateSprite(mLayout.mImageSrc, mLayout.mImageRegion, Vector2.zero);
                //                     }
                //                 }
                base.OnPopulateMesh(vh);
            }
        }
        //-----------------------------------------------------------------------------------------------------------
        private struct HelperVBO : VBO
        {
            private UnityEngine.UI.VertexHelper toFill;
            private VertexHelperBuffer vbo;
            private UILayout layout;
            public HelperVBO(UnityEngine.UI.VertexHelper mesh, Color color, UILayout layout)
            {
                this.toFill = mesh;
                this.vbo = VertexHelperBuffer.AllocAutoRelease(mesh);
                this.vbo.BlendColor = color;
                this.layout = layout;
            }
            public void Dispose()
            {
                this.vbo.Dispose();
                this.vbo = null;
                this.toFill = null;
                this.layout = null;
            }
            public void OnFillVBO(Vector2 size)
            {
                switch (layout.Trans)
                {
                    case UILayoutTrans.TRANS_ROT90:
                    case UILayoutTrans.TRANS_ROT270:
                    case UILayoutTrans.TRANS_MIRROR_ROT90:
                    case UILayoutTrans.TRANS_MIRROR_ROT270:
                        CUtils.Swap(ref size.x, ref size.y);
                        break;
                }
                if (layout == null)
                {
                    return;
                }
                switch (layout.Style)
                {
                    case UILayoutStyle.NULL:
                        break;
                    case UILayoutStyle.COLOR:
                        VertexFillColor(size.x, size.y, layout.mFillColor);
                        break;
                    case UILayoutStyle.SPRITE:
                        if (layout.mSpriteController != null) VertexSprite(size.x, size.y);
                        break;

                    case UILayoutStyle.IMAGE_STYLE_ALL_8:
                        if (layout.mImageSrc != null) VertexAll9(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_ALL_9:
                        if (layout.mImageSrc != null) VertexAll9(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_H_012:
                        if (layout.mImageSrc != null) VertexH012(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_V_036:
                        if (layout.mImageSrc != null) VertexV036(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_HLM:
                        if (layout.mImageSrc != null) VertexHLM(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_VTM:
                        if (layout.mImageSrc != null) VertexVTM(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_BACK_4:
                        if (layout.mImageSrc != null) VertexBack4(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_BACK_4_CENTER:
                        if (layout.mImageSrc != null) VertexBack4Center(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_FLIP_X:
                        if (layout.mImageSrc != null) VertexFlipX(size.x, size.y);
                        break;
                    case UILayoutStyle.IMAGE_STYLE_FLIP_Y:
                        if (layout.mImageSrc != null) VertexFlipY(size.x, size.y);
                        break;
                    default:
                        break;
                }
            }
            //-----------------------------------------------------------------------------------------------------------
            // 九宫格
            //-----------------------------------------------------------------------------------------------------------
            private void VertexAll9(float w, float h)
            {
                var rg = layout.ImageRegion;
                float cL = layout.ClipL, cR = layout.ClipR, cT = layout.ClipT, cB = layout.ClipB;
                if (cL + cR > w)
                {
                    cL = w * (cL / (cL + cR));
                    cR = w - cL;
                }
                if (cT + cB > h)
                {
                    cT = h * (cT / (cT + cB));
                    cB = h - cT;
                }
                ArraySet4(ax_4, 0, cL, w - cR, w);
                ArraySet4(ay_4, 0, cT, h - cB, h);
                ArraySet4(au_4,
                             rg.x,
                             rg.x + layout.mClipL,
                             rg.x + rg.width - layout.mClipR,
                             rg.x + rg.width
                         );
                ArraySet4(av_4,
                             rg.y,
                             rg.y + layout.mClipT,
                             rg.y + rg.height - layout.mClipB,
                             rg.y + rg.height
                          );
                VertexBuffer(w, h, ax_4, ay_4, au_4, av_4);
            }
            private void VertexH012(float w, float h)
            {
                var rg = layout.ImageRegion;
                float cL = layout.ClipL, cR = layout.ClipR;
                if (cL + cR > w)
                {
                    cL = w * (cL / (cL + cR));
                    cR = w - cL;
                }
                ArraySet4(ax_4, 0, cL, w - cR, w);
                ArraySet2(ay_2, 0, h);
                ArraySet4(au_4,
                             rg.x,
                             rg.x + layout.mClipL,
                             rg.x + rg.width - layout.mClipR,
                             rg.x + rg.width
                         );
                ArraySet2(av_2,
                             rg.y,
                             rg.y + rg.height
                         );
                VertexBuffer(w, h, ax_4, ay_2, au_4, av_2);
            }
            private void VertexV036(float w, float h)
            {
                var rg = layout.ImageRegion;
                float cT = layout.ClipT, cB = layout.ClipB;
                if (cT + cB > h)
                {
                    cT = h * (cT / (cT + cB));
                    cB = h - cT;
                }
                ArraySet2(ax_2, 0, w);
                ArraySet4(ay_4, 0, cT, h - cB, h);
                ArraySet2(au_2,
                            rg.x,
                            rg.x + rg.width
                         );
                ArraySet4(av_4,
                            rg.y,
                            rg.y + layout.mClipT,
                            rg.y + rg.height - layout.mClipB,
                            rg.y + rg.height
                         );
                VertexBuffer(w, h, ax_2, ay_4, au_2, av_4);
            }
            private void VertexHLM(float w, float h)
            {
                var rg = layout.ImageRegion;
                float cL = layout.ClipL, cT = layout.ClipT, cB = layout.ClipB;
                if (cL + cL > w)
                {
                    cL = w / 3f;
                }
                if (cT + cB > h)
                {
                    cT = h * (cT / (cT + cB));
                    cB = h - cT;
                }
                ArraySet5(ax_5, 0, cL, w - cL, w - cL, w);
                ArraySet4(ay_4, 0, cT, h - cB, h);
                ArraySet5(au_5,
                             rg.x,
                             rg.x + layout.mClipL,
                             rg.x + rg.width - 1, // 1 keep pixel
                             rg.x + layout.mClipL,
                             rg.x
                         );
                ArraySet4(av_4,
                             rg.y,
                             rg.y + layout.mClipT,
                             rg.y + rg.height - layout.mClipB,
                             rg.y + rg.height
                          );
                VertexBuffer(w, h, ax_5, ay_4, au_5, av_4);
            }
            private void VertexVTM(float w, float h)
            {
                var rg = layout.ImageRegion;
                float cL = layout.ClipL, cR = layout.ClipR, cT = layout.ClipT;
                if (cL + cR > w)
                {
                    cL = w * (cL / (cL + cR));
                    cR = w - cL;
                }
                if (cT + cT > h)
                {
                    cT = h / 3f;
                }
                ArraySet4(ax_4, 0, cL, w - cR, w);
                ArraySet5(ay_5, 0, cT, h - cT, h - cT, h);
                ArraySet4(au_4,
                             rg.x,
                             rg.x + layout.mClipL,
                             rg.x + rg.width - layout.mClipR,
                             rg.x + rg.width
                         );
                ArraySet5(av_5,
                             rg.y,
                             rg.y + layout.mClipT,
                             rg.y + rg.height - 1, // 1 keep pixel
                             rg.y + layout.mClipT,
                             rg.y
                          );
                VertexBuffer(w, h, ax_4, ay_5, au_4, av_5);
            }
            private void VertexBack4(float w, float h)
            {
                var rg = layout.ImageRegion;
                ArraySet2(ax_2, 0, w);
                ArraySet2(ay_2, 0, h);
                ArraySet2(au_2,
                            rg.x,
                            rg.x + rg.width
                         );
                ArraySet2(av_2,
                           rg.y,
                           rg.y + rg.height
                        );
                VertexBuffer(w, h, ax_2, ay_2, au_2, av_2);
            }
            private void VertexBack4Center(float w, float h)
            {
                var rg = layout.ImageRegion;
                float tx = (w - rg.width) * 0.5f;
                float ty = (h - rg.height) * 0.5f;
                ArraySet2(ax_2, tx, tx + rg.width);
                ArraySet2(ay_2, ty, ty + rg.height);
                ArraySet2(au_2,
                             rg.x,
                             rg.x + rg.width
                         );
                ArraySet2(av_2,
                             rg.y,
                             rg.y + rg.height
                          );
                VertexBuffer(w, h, ax_2, ay_2, au_2, av_2);
            }
            private void VertexFlipX(float w, float h)
            {
                var rg = layout.ImageRegion;
                ArraySet3(ax_3, 0, w / 2, w);
                ArraySet2(ay_2, 0, h);
                ArraySet3(au_3,
                             rg.x,
                             rg.x + rg.width - 1, // 1 keep pixel
                             rg.x);
                ArraySet2(av_2,
                             rg.y,
                             rg.y + rg.height);
                VertexBuffer(w, h, ax_3, ay_2, au_3, av_2);
            }
            private void VertexFlipY(float w, float h)
            {
                var rg = layout.ImageRegion;
                ArraySet2(ax_2, 0, w);
                ArraySet3(ay_3, 0, h / 2, h);
                ArraySet2(au_2,
                             rg.x,
                             rg.x + rg.width);
                ArraySet3(av_3,
                             rg.y,
                             rg.y + rg.height - 1, // 1 keep pixel
                             rg.y);
                VertexBuffer(w, h, ax_2, ay_3, au_2, av_3);
            }
            //-----------------------------------------------------------------------------------------------------------
            private void VertexFillColor(float w, float h, Color c)
            {
                UIUtils.CreateVertexQuardColor(c * vbo.BlendColor, 0, 0, w, h, toFill);
            }
            private void VertexSprite(float w, float h)
            {
                layout.mSpriteController.Meta.addVertex(vbo,
                    layout.mSpriteController.CurrentAnimate,
                    layout.mSpriteController.CurrentFrame,
                    w / 2, h / 2);
            }
            //-----------------------------------------------------------------------------------------------------------
            #region ArrayUtils
            private void VertexBuffer(float w, float h, float[] ax, float[] ay, float[] au, float[] av)
            {
                var src = layout.ImageSrc;
                for (int iy = 0; iy < ay.Length; ++iy)
                {
                    for (int ix = 0; ix < ax.Length; ++ix)
                    {
                        s_UIVertex4x4[ix, iy] = UIUtils.CreateVertex(src, vbo.BlendColor, au[ix], av[iy], ax[ix], ay[iy]);
                    }
                }
                if (layout.Trans != UILayoutTrans.TRANS_NONE)
                {
                    var hw = w / 2f;
                    var hh = h / 2f;
                    var trans = Matrix4x4.identity;
                    switch (layout.Trans)
                    {
                        case UILayoutTrans.TRANS_ROT90:
                            trans *= Matrix4x4.Translate(new Vector3(h, 0, 0));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -90));
                            break;
                        case UILayoutTrans.TRANS_ROT180:
                            trans *= Matrix4x4.Translate(new Vector3(w, -h, 0));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -180));
                            break;
                        case UILayoutTrans.TRANS_ROT270:
                            trans *= Matrix4x4.Translate(new Vector3(0, -w, 0));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -270));
                            break;
                        case UILayoutTrans.TRANS_MIRROR:
                            trans *= Matrix4x4.Translate(new Vector3(w, 0, 0));
                            trans *= Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
                            break;
                        case UILayoutTrans.TRANS_MIRROR_ROT90:
                            trans *= Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -90));
                            break;
                        case UILayoutTrans.TRANS_MIRROR_ROT180:
                            trans *= Matrix4x4.Translate(new Vector3(0, -h, 0));
                            trans *= Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -180));
                            break;
                        case UILayoutTrans.TRANS_MIRROR_ROT270:
                            trans *= Matrix4x4.Translate(new Vector3(h, -w, 0));
                            trans *= Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
                            trans *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -270));
                            break;
                    }
                    for (int iy = 0; iy < ay.Length; ++iy)
                    {
                        for (int ix = 0; ix < ax.Length; ++ix)
                        {
                            s_UIVertex4x4[ix, iy].position = trans.MultiplyPoint(s_UIVertex4x4[ix, iy].position);
                        }
                    }
                }
                for (int iy = 0; iy < ay.Length - 1; ++iy)
                {
                    for (int ix = 0; ix < ax.Length - 1; ++ix)
                    {
                        s_UIVertexQuard[0] = s_UIVertex4x4[ix + 0, iy + 0];
                        s_UIVertexQuard[1] = s_UIVertex4x4[ix + 1, iy + 0];
                        s_UIVertexQuard[2] = s_UIVertex4x4[ix + 1, iy + 1];
                        s_UIVertexQuard[3] = s_UIVertex4x4[ix + 0, iy + 1];
                        toFill.AddUIVertexQuad(s_UIVertexQuard);
                    }
                }
            }

            private static UIVertex[] s_UIVertexQuard = new UIVertex[4];
            private static UIVertex[,] s_UIVertex4x4 = new UIVertex[6, 6];

            private static float[] ax_2 = new float[2];
            private static float[] ay_2 = new float[2];
            private static float[] au_2 = new float[2];
            private static float[] av_2 = new float[2];

            private static float[] ax_3 = new float[3];
            private static float[] ay_3 = new float[3];
            private static float[] au_3 = new float[3];
            private static float[] av_3 = new float[3];

            private static float[] ax_4 = new float[4];
            private static float[] ay_4 = new float[4];
            private static float[] au_4 = new float[4];
            private static float[] av_4 = new float[4];

            private static float[] ax_5 = new float[5];
            private static float[] ay_5 = new float[5];
            private static float[] au_5 = new float[5];
            private static float[] av_5 = new float[5];

            private static float[] ax_6 = new float[6];
            private static float[] ay_6 = new float[6];
            private static float[] au_6 = new float[6];
            private static float[] av_6 = new float[6];
            private static void ArraySet6(float[] array, float a, float b, float c, float d, float e, float f)
            {
                array[0] = a;
                array[1] = b;
                array[2] = c;
                array[3] = d;
                array[4] = e;
                array[5] = f;
            }
            private static void ArraySet5(float[] array, float a, float b, float c, float d, float e)
            {
                array[0] = a;
                array[1] = b;
                array[2] = c;
                array[3] = d;
                array[4] = e;
            }
            private static void ArraySet4(float[] array, float a, float b, float c, float d)
            {
                array[0] = a;
                array[1] = b;
                array[2] = c;
                array[3] = d;
            }
            private static void ArraySet3(float[] array, float a, float b, float c)
            {
                array[0] = a;
                array[1] = b;
                array[2] = c;
            }
            private static void ArraySet2(float[] array, float a, float b)
            {
                array[0] = a;
                array[1] = b;
            }
            #endregion
            //-----------------------------------------------------------------------------------------------------------
        }


        //---------------------------------------------------------------------------------------------------------------

        interface VBO : IDisposable
        {
            void OnFillVBO(Vector2 size);

        }

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------
    }


}

