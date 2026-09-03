using DeepCore.Unity3D.Impl;
using DeepCore.GUI.Cell;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeepCore.GUI.Data;

namespace DeepCore.Unity3D.UGUI
{
    public partial class BatchQuadsGraphics : UnityEngine.UI.MaskableGraphic
    {
        private DisplayNode mBinding;
        private Texture2D mTexture;
        private CPJAtlas mAtlas;
        DeepCore.Unity3D.Impl.UnityImage mSrc;
        private List<BatchImageQuad> mQuads = new List<BatchImageQuad>();

        public override Texture mainTexture { get { return mTexture; } }

        public DisplayNode Binding { get { return mBinding; } }

        public CPJAtlas Atlas
        {
            get { return mAtlas; }
            set
            {
                mSrc = value.GetTile(0) as UnityImage;
                this.mAtlas = value;
                this.mTexture = mSrc.Texture2D;
                this.material = mSrc.TextureMaterial;
                base.SetAllDirty();
            }
        }

        public BatchImageQuad[] Quads
        {
            get { return mQuads.ToArray(); }
        }
        public int QuadsCount
        {
            get { return mQuads.Count; }
        }
        public BatchImageQuad GetQuad(int i)
        {
            return mQuads[i];
        }
        public void SetQuadVisible(int i, bool visible)
        {
            if (i >= 0 && i < mQuads.Count)
            {
                var b = mQuads[i];
                if (b.visible != visible)
                {
                    b.visible = visible;
                    mQuads[i] = b;
                    base.SetVerticesDirty();
                }
            }
        }
        public void SetQuad(int i, BatchImageQuad quad)
        {
            if (i >= 0 && i < mQuads.Count)
            {
                mQuads[i] = quad;
                base.SetVerticesDirty();
            }
        }
        public int AddQuad(BatchImageQuad qd)
        {
            int ret = mQuads.Count;
            mQuads.Add(qd);
            base.SetVerticesDirty();
            return ret;
        }

        public int AddQuad(int index)
        {
            int ret = mQuads.Count;
            mQuads.Add(new BatchImageQuad(index));
            base.SetVerticesDirty();
            return ret;
        }
        public int AddQuad(int index, GUI.Data.ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            int ret = mQuads.Count;
            var qd = new BatchImageQuad(index);
            qd.anchor = anchor;
            qd.pivot = pivot;
            qd.offset = offset;
            qd.scale = scale;
            qd.rotate = rotate;
            mQuads.Add(qd);
            base.SetVerticesDirty();
            return ret;
        }

        public int AddQuadByKey(string key, GUI.Data.ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            int index = mAtlas.GetIndexByKey(key);
            if (index >= 0)
            {
                return AddQuad(index, anchor, pivot, offset, scale, rotate);
            }
            return index;
        }

        public int AddQuadImageFont(string text)
        {
            int ret = mQuads.Count;
            var qd = new BatchImageQuad(text);
            mQuads.Add(qd);
            base.SetVerticesDirty();
            return ret;
        }
        public int AddQuadImageFont(string text, ImageAnchor anchor, Vector2 pivot, Vector2 offset, Vector2 scale, float rotate)
        {
            int ret = mQuads.Count;
            var qd = new BatchImageQuad(text);
            qd.anchor = anchor;
            qd.pivot = pivot;
            qd.offset = offset;
            qd.scale = scale;
            qd.rotate = rotate;
            mQuads.Add(qd);
            base.SetVerticesDirty();
            return ret;
        }


        public void ClearQuads()
        {
            if (mQuads.Count > 0)
            {
                mQuads.Clear();
                base.SetVerticesDirty();
            }
        }


        public BatchQuadsGraphics()
        {
            //this.material = ImageGraphics.DefaultImageMaterial;
        }

        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (mAtlas != null)
            {
                uint rgba = UIUtils.Color_To_UInt32_RGBA(base.color);
                using (VertexHelperBuffer vertex = VertexHelperBuffer.AllocAutoRelease(vh))
                {
                    for (int i = 0; i < mQuads.Count; ++i)
                    {
                        var batch = mQuads[i];
                        if (batch.visible)
                        {
                            vertex.pushTransform();
                            if (!string.IsNullOrEmpty(batch.text))
                            {
                                OnPopulateText(vertex, ref batch, rgba);
                            }
                            else if (batch.index >= 0)
                            {
                                OnPopulateTile(vertex, ref batch, rgba);
                            }
                            vertex.popTransform();
                        }
                    }
                }
            }
        }
        protected virtual void OnPopulateTile(VertexHelperBuffer vertex, ref BatchImageQuad batch, uint rgba)
        {
            var rect = new Rect(0, 0, mAtlas.getWidth(batch.index), mAtlas.getHeight(batch.index));
            var pivot = new Vector2(rect.width * batch.pivot.x, rect.height * batch.pivot.y);
            UIUtils.AdjustAnchor(batch.anchor, rectTransform.sizeDelta, ref rect);
            vertex.translate(rect.x + pivot.x + batch.offset.x, rect.y + pivot.y + batch.offset.y);
            vertex.scale(batch.scale.x, batch.scale.y);
            vertex.rotate(batch.rotate);
            mAtlas.addVertex(vertex, batch.index, -pivot.x, -pivot.y, 0, rgba);
        }
        protected virtual void OnPopulateText(VertexHelperBuffer vertex, ref BatchImageQuad batch, uint rgba)
        {
            var size = ImageFontGraphics.GetImageFontPreferredSize(mAtlas, batch.text);
            var rect = new Rect(0, 0, size.x, size.y);
            var pivot = new Vector2(rect.width * batch.pivot.x, rect.height * batch.pivot.y);
            UIUtils.AdjustAnchor(batch.anchor, rectTransform.sizeDelta, ref rect);
            vertex.translate(rect.x + pivot.x + batch.offset.x, rect.y + pivot.y + batch.offset.y);
            vertex.scale(batch.scale.x, batch.scale.y);
            vertex.rotate(batch.rotate);
            float sw = 0;
            for (int i = 0; i < batch.text.Length; i++)
            {
                char ch = batch.text[i];
                int index = mAtlas.GetIndexByKey(ch.ToString());
                if (index >= 0)
                {
                    Vector2 char_size = new Vector2(mAtlas.getWidth(index), mAtlas.getHeight(index));
                    if (char_size.x > 0)
                    {
                        mAtlas.addVertex(vertex, index, sw - pivot.x, 0 - pivot.y, 0, rgba);
                        sw += char_size.x;
                    }
                }
            }
        }


        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding.Enable && mBinding.EnableTouchInParents)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }



        public struct BatchImageQuad
        {
            /// <summary>
            /// 图片索引
            /// </summary>
            public int index;
            /// <summary>
            /// 图片字，不为空起效
            /// </summary>
            public string text;
            /// <summary>
            /// 是否显示
            /// </summary>
            public bool visible;
            /// <summary>
            /// 对其方式
            /// </summary>
            public ImageAnchor anchor;

            /// <summary>
            /// 旋转缩放参考点
            /// </summary>
            public Vector2 pivot;
            /// <summary>
            /// 坐标
            /// </summary>
            public Vector2 offset;
            /// <summary>
            /// 缩放
            /// </summary>
            public Vector2 scale;
            /// <summary>
            /// 旋转
            /// </summary>
            public float rotate;

            public BatchImageQuad(int index)
            {
                this.index = index;
                this.text = null;
                this.visible = true;
                this.anchor = ImageAnchor.C_C;
                this.pivot = new Vector2(.5f, .5f);
                this.offset = Vector2.zero;
                this.scale = Vector2.one;
                this.rotate = 0;
            }
            public BatchImageQuad(string text)
            {
                this.index = -1;
                this.text = text;
                this.visible = true;
                this.anchor = ImageAnchor.C_C;
                this.pivot = new Vector2(.5f, .5f);
                this.offset = Vector2.zero;
                this.scale = Vector2.one;
                this.rotate = 0;
            }

        }

        private void ResetTexture()
        {
            if (mSrc != null && mSrc.Texture2D != mTexture)
            {
                this.mTexture = mSrc.Texture2D;
                this.material = mSrc.TextureMaterial;
                base.SetAllDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTexture();
        }

        void Update()
        {
            ResetTexture();
        }

    }

}
