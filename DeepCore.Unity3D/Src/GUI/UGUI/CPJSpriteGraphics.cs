using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    //---------------------------------------------------------------------------------------------------

    public partial class CPJSpriteGraphics : UnityEngine.UI.MaskableGraphic
    {
        private DisplayNode mBinding;
        private CPJSprite mSpr;
        private Texture2D mMainTexture;
        private float mAlpha;
        private int mCurrentActionIndex;
        private int mCurrentFrameIndex = 0;
        private DrawType mDrawType = DrawType.Simple;
        private Vector2 mDrawGridSize = Vector2.zero;

        public DrawType drawType { get { return mDrawType; } set { mDrawType = value; } }
        public Vector2 drawGridSize { get { return mDrawGridSize; } set { mDrawGridSize = value; } }

        private DeepCore.Unity3D.Impl.UnityImage mSrc;
        public override Texture mainTexture { get { return mMainTexture; } }

        public float Alpha
        {
            get { return mAlpha; }
            set
            {
                if (mAlpha != value)
                {
                    mAlpha = value;
                    Color c = base.color;
                    c.a = value;
                    base.color = c;
                    this.SetAllDirty();
                }
            }
        }


        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
        }


        public void SetSpriteMeta(CPJSprite spr)
        {
            this.mSpr = spr;
            mSrc = spr.Controller.Meta.Atlas.GetTile(0) as DeepCore.Unity3D.Impl.UnityImage;
            this.mMainTexture = mSrc.Texture2D;
            this.material = mSrc.TextureMaterial;
            this.SetAllDirty();
        }
        public void SetFrame(int anim, int frame)
        {
            if (mCurrentActionIndex != anim || mCurrentFrameIndex != frame)
            {
                mCurrentActionIndex = anim;
                mCurrentFrameIndex = frame;
                this.SetVerticesDirty();
            }
        }

        public override void SetNativeSize()
        {
            if (mSpr != null)
            {
                this.SetAllDirty();
            }
        }

        //-------------------------------------------------------------------------------------------------
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (mSpr != null)
            {
                using (VertexHelperBuffer mesh = VertexHelperBuffer.AllocAutoRelease(vh))
                {
                    mesh.BlendColor = this.color;
                    Vector2 size = rectTransform.sizeDelta;
                    Vector2 center = rectTransform.rect.center;
                    if (this.mDrawType == DrawType.Simple)
                    {
                        mSpr.Controller.Meta.addVertex(mesh,
                            mCurrentActionIndex,
                            mCurrentFrameIndex,
                            0, 0);
                    }
                    else if (this.mDrawType == DrawType.Center)
                    {
                        mSpr.Controller.Meta.addVertex(mesh,
                            mCurrentActionIndex,
                            mCurrentFrameIndex,
                            center.x, center.y);
                    }
                    else if (this.mDrawType == DrawType.FillGrid)
                    {
                        if (this.mDrawGridSize.x > 0 || this.mDrawGridSize.y > 0)
                        {
                            for (float dx = 0; dx < size.x; dx += mDrawGridSize.x)
                            {
                                for (float dy = 0; dy < size.y; dy += mDrawGridSize.y)
                                {
                                    mSpr.Controller.Meta.addVertex(mesh,
                                       mCurrentActionIndex,
                                       mCurrentFrameIndex,
                                       dx, dy);
                                }
                            }
                        }
                        else
                        {
                            mSpr.Controller.Meta.addVertex(mesh,
                                mCurrentActionIndex,
                                mCurrentFrameIndex,
                                0, 0);
                        }
                    }
                }
            }
        }
        //-------------------------------------------------------------------------------------------------

        public virtual void CalculateLayoutInputHorizontal()
        {
        }
        public virtual void CalculateLayoutInputVertical()
        {
        }
        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding.Enable && mBinding.EnableTouchInParents)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }

        public enum DrawType
        {
            Simple,
            Center,
            FillGrid,
        }
		
        private void ResetTexture()
        {
            if (mSrc != null && mSrc.Texture2D != mMainTexture)
            {
                this.mMainTexture = mSrc.Texture2D;
                this.material = mSrc.TextureMaterial;
                base.SetAllDirty();
            }
        }

        void Update()
        {
            ResetTexture();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTexture();
        }
    }

}
