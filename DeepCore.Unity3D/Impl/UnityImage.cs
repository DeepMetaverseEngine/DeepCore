
using DeepCore.GUI.Display;
using DeepCore.Unity3D.Src.M3Z;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;


namespace DeepCore.Unity3D.Impl
{


    public partial class UnityImage : Image, IUnityImageInterface
    {
        private Texture2D mTexture;
        private Texture2D mTextureMask;
        internal int mLogicW, mLogicH;
        internal float mMaxU, mMaxV;
        private Material mMaterial;
        private string mResStr;
        public string ResourceStr
        {
            get
            {
                return mResStr;
            }
        }
        public bool SupportReleaseTexture { get; set; }
        internal static void FilterTexture(Texture2D tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 2;
            tex.mipMapBias = 0;
        }

        public UnityImage(Texture2D tex, string name, string res = null)
        {
            mResStr = res;
            SupportReleaseTexture = true;
            ResestTexture2D(tex, name);
        }
        public UnityImage(Texture2D tex, int logicW, int logicH, string name, string res = null)
        {
            mResStr = res;
            SupportReleaseTexture = true;
            ResestTexture2D(tex, logicW, logicH, name);
        }

        public UnityImage(byte[] data, string name, string res = null)
        {
            mResStr = res;
            SupportReleaseTexture = true;
            ResestTexture2D(data, name);
        }

        public override void ReleaseTexture()
        {
            //有ResourceStr才允许释放贴
            if (SupportReleaseTexture && !string.IsNullOrEmpty(mResStr))
            {
                if (mTexture != null)
                {
                    DeepCore.Unity3D.UnityHelper.Destroy(mTexture);
                    mTexture = null;
                }
                if (mTextureMask != null)
                {
                    DeepCore.Unity3D.UnityHelper.Destroy(mTextureMask);
                    mTextureMask = null;
                }
                if (mMaterial != null)
                {
                    DeepCore.Unity3D.UnityHelper.Destroy(mMaterial);
                    mMaterial = null;
                }
            }
        }

        private void CheckReload()
        {
            if (mTexture == null)
            {
                GraphicsDriver.Instance.ReloadImage(this);
            }
        }

        public void ResestTexture2D(Texture2D tex, string name)
        {
            this.name = name;
            this.mTexture = tex;
            this.mTexture.name = name;
            FilterTexture(mTexture);
            this.mLogicW = tex.width;
            this.mLogicH = tex.height;
            this.mMaxU = 1f;
            this.mMaxV = 1f;
        }

        public void ResestTexture2D(Texture2D tex, int logicW, int logicH, string name)
        {
            this.name = name;
            this.mTexture = tex;
            this.mTexture.name = name;
            FilterTexture(mTexture);
            this.mLogicW = logicW;
            this.mLogicH = logicH;
            this.mMaxU = Math.Min(mLogicW / (float)tex.width, 1f);
            this.mMaxV = Math.Min(mLogicH / (float)tex.height, 1f);
        }

        public void ResestTexture2D(byte[] data, string name)
        {
            this.name = name;
            string ext = name.ToLower();
            if (ext.EndsWith(".m3z"))
            {
                using (var input = new DeepCore.IO.MemoryStream(data))
                {
                    M3ZHeader m3z = new M3ZHeader(input);
                    InitWithM3Z(m3z);
                }
            }
            else if (ext.EndsWith(".g3z"))
            {
                using (var input = G3ZStream.AllocDecompressToStream(data))
                {
                    M3ZHeader m3z = new M3ZHeader(input);
                    InitWithM3Z(m3z);
                }
            }
            else
            {
                this.mTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                this.mTexture.name = name;
                this.mTexture.LoadImage(data, true);
                FilterTexture(mTexture);
                this.mLogicW = mTexture.width;
                this.mLogicH = mTexture.height;
                this.mMaxU = 1;
                this.mMaxV = 1;
            }
        }

        private void InitWithM3Z(M3ZHeader header)
        {
            this.mMaxU = 1;
            this.mMaxV = 1;
#if UNITY_EDITOR
            Debug.Log(string.Format("InitM3Z : {0} : format={1} src={2}x{3} pot={4}x{5}",
                    name,
                    header.trunks[0].type,
                    header.srcWidth,
                    header.srcHeight,
                    header.trunks[0].pixelW,
                    header.trunks[0].pixelH));
#endif
            if (header.trunks.Length > 0)
            {
                this.mTexture = header.trunks[0].LoadRawTextureData();
                this.mTexture.name = name + "(Trunk0)";
                FilterTexture(this.mTexture);
            }
            if (header.trunks.Length > 1)
            {
                this.mTextureMask = header.trunks[1].LoadRawTextureData();
                this.mTextureMask.name = name + "(Trunk1)";
                FilterTexture(this.mTextureMask);
            }
            this.mLogicW = header.srcWidth;
            this.mLogicH = header.srcHeight;
            this.mMaxU = Math.Min(mLogicW / (float)mTexture.width, 1f);
            this.mMaxV = Math.Min(mLogicH / (float)mTexture.height, 1f);
            header = null;
        }
        public Vector2 ToUV(float x, float y)
        {
            return new Vector2(
                x / this.mTexture.width,
                1 - y / this.mTexture.height);
        }
        public Rect ToUV(float x, float y, float w, float h)
        {
            return new Rect(
                x / this.mTexture.width,
                1 - (y + h) / this.mTexture.height,
                w / this.mTexture.width,
                h / this.mTexture.height);
        }

        public Vector2 ToUV(Geometry.Point p)
        {
            return ToUV(p.X, p.Y);
        }
        public Vector2 ToUV(Geometry.Vector2 p)
        {
            return ToUV(p.X, p.Y);
        }
        public Rect ToUV(Geometry.Rectangle region)
        {
            return ToUV(region.Left, region.Top, region.Width, region.Height);
        }
        public Rect ToUV(Geometry.RectangleF region)
        {
            return ToUV(region.Left, region.Top, region.Width, region.Height);
        }
        //-----------------------------------------------------------------------------------------------------------
        public override int Width { get { return mLogicW; } }
        public override int Height { get { return mLogicH; } }
        public override float MaxU { get { return mMaxU; } }
        public override float MaxV { get { return mMaxV; } }
        public Texture Texture { get { return Texture2D; } }
        public Texture TextureMask { get { return Texture2DMask; } }
        public Texture2D Texture2D { get { CheckReload(); return mTexture; } }
        public Texture2D Texture2DMask { get { CheckReload(); return mTextureMask; } }

        public Material TextureMaterial { get { return GetMaterial(); } }
        //-----------------------------------------------------------------------------------------------------------
        public Material GetMaterial()
        {
            CheckReload();
            if (mMaterial == null)
            {
                mMaterial = UnityShaders.CreateMaterialUGUI(this);
            }
            return mMaterial;
        }
        protected override void Disposing()
        {
            if (mTexture != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mTexture);
                mTexture = null;
            }
            if (mTextureMask != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mTextureMask);
                mTextureMask = null;
            }
            if (mMaterial != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(mMaterial);
                mMaterial = null;
            }
        }

        public override void CopyPixels(Image src, int sx, int sy, int sw, int sh, int dx, int dy)
        {
            UnityImage srci = src as UnityImage;
           // UnityDriver.Platform.CopyPixels(srci.mTexture, sx, sy, sw, sh, this.mTexture, dx, dy);

        }

        public override void Flush()
        {
            mTexture.Apply();
        }

        //-----------------------------------------------------------------------------------------------------------


    }
}
