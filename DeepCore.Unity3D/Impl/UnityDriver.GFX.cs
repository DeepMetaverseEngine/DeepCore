using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity3D.Impl
{

    public partial class UnityDriver
    {
        public delegate string RedirectImagePath(string resource);
        public RedirectImagePath RedirectImage;
        public delegate UnityImage GetDefaultImg(string resource);
        public GetDefaultImg OnGetDefaultImg;

        public override Task<Image> CreateImageAsync(string resource)
        {
            var ret = CreateUnityImage(resource);
            return Task.FromResult<Image>(ret);
        }

        sealed public override Image CreateImage(string resource) { return CreateUnityImage(resource); }

        sealed public override Image CreateImage(System.IO.Stream stream) { return CreateUnityImage(stream); }
        sealed public override Image CreateImage(byte[] imageData, int imageOffset, int imageLength) { return CreateUnityImage(imageData, imageOffset, imageLength); }
        sealed public override Image CreateRGBImage(int width, int height, uint[] rgba) { return CreateUnityRGBImage(width, height, rgba); }
        sealed public override VertexBuffer CreateVertexBuffer(int capacity) { return CreateUnityVertexBuffer(capacity); }
        sealed public override TextLayer CreateTextLayer(string text, object fontName, float size, TextFontStyle style, TextBorderStyle border) { return CreateUnityTextLayer(text, size, style, border); }

        public override void ReloadImage(Image img)
        {
            try
            {
                UnityImage ret = img as UnityImage;
                if (ret != null && !string.IsNullOrEmpty(ret.ResourceStr))
                {
                    string resource = ret.ResourceStr;

                    if (resource.StartsWith(Resource.PREFIX_MPQ))
                    {
#if MPQ
                        byte[] edata = mFileSystem.GetData(resource.Substring(UnityResourceLoader.PREFIX_MPQ.Length));
                        if (edata != null)
                        {
                            ret.ResestTexture2D(edata, resource);
                        }
#endif
                    }
                    else if (resource.StartsWith(Resource.PREFIX_RES))
                    {
                        string res_path = resource.Substring(Resource.PREFIX_RES.Length);
                        object obj = LoadObjectFromResources(res_path);
                        if (obj is Texture2D)
                        {
                            ret.ResestTexture2D(obj as Texture2D, resource);
                        }
                        if (obj is TextAsset)
                        {
                            TextAsset ta = (obj as TextAsset);
                            ret.ResestTexture2D(ta.bytes, resource);
                        }
                    }
                    else if (resource.StartsWith(Resource.PREFIX_FILE))
                    {
                        FileInfo finfo = new FileInfo(resource.Substring(Resource.PREFIX_FILE.Length));
                        if (finfo.Exists)
                        {
                            byte[] data = File.ReadAllBytes(finfo.FullName);
                            ret.ResestTexture2D(data, resource);
                        }
                    }
                    else
                    {
                        byte[] data = Resource.LoadData(resource);
                        if (data != null)
                        {
                            ret.ResestTexture2D(data, resource);
                        }
                        else
                        {
                            Texture2D tex = LoadFromResources<Texture2D>(resource);
                            if (tex != null)
                            {
                                ret.ResestTexture2D(tex, resource);
                            }
                        }
                    }
                }


            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public virtual UnityImage CreateUnityImage(string resource)
        {
            try
            {
                UnityImage ret = null;
                if (RedirectImage != null)
                {
                    resource = RedirectImage(resource);
                }

                if (resource.StartsWith(Resource.PREFIX_MPQ))
                {
#if MPQ
                    byte[] edata = mFileSystem.GetData(resource.Substring(UnityResourceLoader.PREFIX_MPQ.Length));
                    if (edata != null)
                    {
                        ret = new UnityImage(edata, resource, resource);
                    }
#endif
                }
                else if (resource.StartsWith(Resource.PREFIX_RES))
                {
                    string res_path = resource.Substring(Resource.PREFIX_RES.Length);
                    object obj = LoadObjectFromResources(res_path);
                    if (obj is Texture2D)
                    {
                        return new UnityImage(obj as Texture2D, resource, resource);
                    }
                    if (obj is TextAsset)
                    {
                        TextAsset ta = (obj as TextAsset);
                        ret = new UnityImage(ta.bytes, resource, resource);
                    }
                }
                else if (resource.StartsWith(Resource.PREFIX_FILE))
                {
                    FileInfo finfo = new FileInfo(resource.Substring(Resource.PREFIX_FILE.Length));
                    if (finfo.Exists)
                    {
                        byte[] data = File.ReadAllBytes(finfo.FullName);
                        ret = new UnityImage(data, resource, resource);
                    }
                }
                else
                {
                    byte[] data = Resource.LoadData(resource);
                    if (data != null)
                    {
                        ret = new UnityImage(data, resource, resource);
                    }
                    else
                    {
                        Texture2D tex = LoadFromResources<Texture2D>(resource);
                        if (tex != null)
                        {
                            ret = new UnityImage(tex, resource, resource);
                        }
                    }
                }
                return ret;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(string.Format("Resource Read Error : {0}\n{1}", resource, e.Message));
                UnityEngine.Debug.LogException(e);
            }
            //Assert(false, string.Format("Resource Read Error : {0}\n", resource));
            if (OnGetDefaultImg != null) { return OnGetDefaultImg(resource); }
            return null;
        }

        public virtual UnityImage CreateUnityImage(System.IO.Stream stream)
        {
            if (stream == null)
            {
                UnityEngine.Debug.Log("Invalid Param : create Image from stream");
                return null;
            }
            try
            {
                //  U3D Texture2D
                byte[] imageData = new byte[stream.Length];
                IOUtil.ReadToEnd(stream, imageData, 0, imageData.Length);
                return new UnityImage(imageData, "createImage(stream)");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Stream Read Error " + e.Message);
                UnityEngine.Debug.LogException(e);
            }
            return null;
        }

        public virtual UnityImage CreateUnityImage(byte[] imageData, int imageOffset, int imageLength)
        {
            try
            {
                if (imageLength == imageData.Length)
                {
                    return new UnityImage(imageData, "createImage(byte[])");
                }
                else
                {
                    byte[] data = new byte[imageLength];
                    System.Array.Copy(imageData, imageOffset, data, 0, imageLength);
                    //  To UnityImage
                    return new UnityImage(data, "createImage(byte[])");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("ImageData Read Error " + e.Message);
                UnityEngine.Debug.LogException(e);
            }

            return null;

        }

        public virtual UnityImage CreateUnityRGBImage(int width, int height, uint[] rgba)
        {
            UnityEngine.Texture2D destTex = new UnityEngine.Texture2D(width, height, TextureFormat.ARGB32, false, true);
            int i = 0;
            UnityEngine.Color color = UnityEngine.Color.white;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++, i++)
                {
                    GUI.Display.Color.DecodeRGBA(rgba[i], out color.r, out color.g, out color.b, out color.a);
                    destTex.SetPixel(x, y, color);
                }
            }
            destTex.Apply();
            return (new UnityImage(destTex, string.Format("createRGBImage({0},{1})", width, height)));
        }

        public virtual UnityImage CreateUnityRGBImage(int width, int height)
        {
            UnityEngine.Texture2D destTex = new UnityEngine.Texture2D(width, height, TextureFormat.ARGB32, false, true);
            UnityEngine.Color color = new UnityEngine.Color(0, 0, 0, 0);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    destTex.SetPixel(x, y, color);
                }
            }
            destTex.Apply();
            return (new UnityImage(destTex, string.Format("createRGBImage({0},{1})", width, height)));
        }

        public virtual TextLayer CreateUnityTextLayer(string text, float size, TextFontStyle style, TextBorderStyle border)
        {
            return new UnityTextLayer(text, size, style, border);
        }

        public virtual UnityVertexBuffer CreateUnityVertexBuffer(int capacity)
        {
            return new UnityVertexBuffer(capacity);
        }
        public override bool TestTextLineBreak(
            string text,
            object fontName,
            float size,
            TextFontStyle style,
            TextBorderStyle borderTime,
            float testWidth,
            out float realWidth,
            out float realHeight)
        {
            //return sPlatform.TestTextLineBreak(text, size, style, borderTime, testWidth, out realWidth, out realHeight);
            realWidth = 8;
            realHeight = 8;
            return false;
        }




    }


}
