using DeepCore.GUI.Gemo;
using DeepCore.Unity3D.Impl;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{

    public partial class ImageGraphics : Image
    {
        private static Material s_DefaultImageMaterial;
        public static Material DefaultImageMaterial
        {
            get
            {
                if (s_DefaultImageMaterial == null)
                {
                    s_DefaultImageMaterial = new Material(UnityShaders.MFUGUI_Image_Shader);
                    s_DefaultImageMaterial.mainTextureScale = new Vector2(1, 1);
                    s_DefaultImageMaterial.color = UnityEngine.Color.white;
                    //ret.SetFloat("_Gray", 1);
                    s_DefaultImageMaterial.SetPass(0);
                }
                return s_DefaultImageMaterial;
            }
        }


        private DisplayNode mBinding;
        public DisplayNode Binding { get { return mBinding; } }
        private DeepCore.Unity3D.Impl.UnityImage mSrc;
        private Rect mClip;
        private Vector2 mPivot;
        public ImageGraphics()
        {
            //this.material = ImageGraphics.DefaultImageMaterial;
        }


        protected override void Start()
        {
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Start();
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (mBinding != null && mBinding.Enable && mBinding.EnableTouchInParents)
            {
                return base.Raycast(sp, eventCamera);
            }
            return false;
        }
        public void SetImage(DeepCore.Unity3D.Impl.UnityImage src)
        {
            if (base.sprite != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(sprite);
            }
            this.mSrc = src;
            this.mClip = new Rect(0, 0, src.Width, src.Height);
            this.mPivot = new Vector2(.5f, .5f);
            this.sprite = UIUtils.CreateSprite(mSrc, mClip, mPivot);
			this.material = mSrc.TextureMaterial;
        }
        public void SetImage(DeepCore.Unity3D.Impl.UnityImage src, Rect clip, Vector2 pivot)
        {
            if (base.sprite != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(sprite);
            }
            this.mSrc = src;
            this.mClip = clip;
            this.mPivot = pivot;
            this.sprite = UIUtils.CreateSprite(src, clip, pivot);
			this.material = mSrc.TextureMaterial;
        }
        public void SetImage(DeepCore.Unity3D.Impl.UnityImage src, Rectangle2D clip, Vector2 pivot)
        {
            if (base.sprite != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(sprite);
            }
            this.mSrc = src;
            this.mClip = new Rect(clip.x, src.Texture.height - clip.y - clip.height, clip.width, clip.height);
            this.mPivot = pivot;
            this.sprite = Sprite.Create(mSrc.Texture2D, mClip, mPivot,100f, 0, SpriteMeshType.FullRect);
           
            this.material = mSrc.TextureMaterial;
        }

        protected override void OnDestroy()
        {
            if (base.sprite != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(sprite);
            }
            base.OnDestroy();
        }

        private void ResetTexture()
        {
            if (this.mSrc != null && sprite != null && this.mSrc.Texture2D != sprite.texture)
            {
                if (base.sprite != null)
                {
                    DeepCore.Unity3D.UnityHelper.Destroy(sprite);
                }
                this.sprite = UIUtils.CreateSprite(mSrc, mClip, mPivot);
                this.material = mSrc.TextureMaterial;
                this.SetAllDirty();
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
