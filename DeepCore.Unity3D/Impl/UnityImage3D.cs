using DeepCore.GUI.Display;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{

    public partial class UnityImage3D : Image, IUnityImageInterface
    {
        internal int mLogicW, mLogicH;
        internal float mMaxU, mMaxV;

        private RenderTexture mTexture;

        public override int Width
        {
            get
            {
                return mLogicW;
            }
        }
        public override int Height
        {
            get
            {
                return mLogicH;
            }
        }
        public override float MaxU
        {
            get
            {
                return mMaxU;
            }
        }
        public override float MaxV
        {
            get
            {
                return mMaxV;
            }
        }

        public Texture Texture
        {
            get
            {
                return mTexture;
            }
        }
        public Texture TextureMask
        {
            get
            {
                return null;
            }
        }

        internal static void FilterTexture(RenderTexture tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 0;
            tex.mipMapBias = 0;
        }
        public UnityImage3D(RenderTexture tex, string name)
        {
            this.name = name;
            if (tex == null) { Debug.Log("Create UnityImage3D Error tex = null"); }
            this.mTexture = tex;
            this.mTexture.name = name;
            FilterTexture(tex);
            this.mLogicW = tex.width;
            this.mLogicH = tex.height;
            this.mMaxU = 1f;
            this.mMaxV = 1f;
        }
        protected override void Disposing()
        {
            if (mTexture != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mTexture);
                mTexture = null;
            }
        }

        public override void CopyPixels(Image src, int sx, int sy, int sw, int sh, int dx, int dy)
        {
            //do nothing.
        }

        public override void Flush()
        {
            //do nothing.
        }

    }


}
