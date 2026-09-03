using DeepCore.Concurrent;
using DeepCore.GUI.Data;
using DeepCore.Unity3D.Platform;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace DeepCore.Unity3D_IOS
{
    public class UnityPlatformIOS : IUnityPlatform
    {
        public void Assert(string msg)
        {
            Debug.LogError(msg);
        }
        //----------------------------------------------------------------------------------------------------------
        #region Native

        /// <summary>
        /// inflate
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="dstFile"></param>
        /// <returns></returns>
        [DllImport("__Internal")]
        public static extern bool _Decompress_z(string srcFile, string dstFile);

        [DllImport("__Internal")]
        public static extern int _Decompress_bytes();

        [DllImport("__Internal")]
        public static extern bool _Decompress_z_mem(byte[] src, int s_offset, int s_end, byte[] dst, int dst_offset, int dst_end);



        [DllImport("__Internal")]
        public static extern bool _SysFontTest(
                string pText,
                string fontName,
                int fontStyle,
                int fontSize,
                int bgCount,
                int expectSizeW,
                int expectSizeH,
                ref int outW,
                ref int outH);

        [DllImport("__Internal")]
        public static extern bool _SysFontTexture2(
                string pText,
                string fontName,
                int fontStyle,
                int fontSize,
                int fontColorRGBA,
                int bgCount,
                int bgColorRGBA,
                int expectSizeW,
                int expectSizeH,
                int glTextureID);
        [DllImport("__Internal")]
        public static extern bool _SysFontTexture2_Ptr(
                string pText,
                string fontName,
                int fontStyle,
                int fontSize,
                int fontColorRGBA,
                int bgCount,
                int bgColorRGBA,
                int expectSizeW,
                int expectSizeH,
                IntPtr glTextureID_Ptr);


        [DllImport("__Internal")]
        public static extern bool _SysFontGetPixels(
                string pText,
                string fontName,
                int fontStyle,
                int fontSize,
                int fontColorRGBA,
                int bgCount,
                int bgColorRGBA,
                int pixelW,
                int pixelH,
                byte[] pixels);
        [DllImport("__Internal")]
        public static extern bool _SysFontGetPixels_Color32(
                string pText,
                string fontName,
                int fontStyle,
                int fontSize,
                int fontColorRGBA,
                int bgCount,
                int bgColorRGBA,
                int pixelW,
                int pixelH,
                Color32[] pixels);

        [DllImport("__Internal")]
        public static extern bool _Md5_CheckFile(string srcFile, byte[] dst);


        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region TEXT

        public const string FontName = "Helvetica-Bold";

        public Texture2D SysFontTexture(
            string text,
            bool readable,
            TextFontStyle style,
            float fontSize,
            uint fontColor,
            TextBorderStyle borderTime,
            uint borderColor,
            Vector2 expectSize,
            out int boundW,
            out int boundH)
        {
            try
            {
                text = text + "";
                int _pixelW = 8;
                int _pixelH = 8;
                int _expectW = 0;
                int _expectH = 0;
                if (expectSize != null)
                {
                    _pixelW = (int)expectSize.x;
                    _pixelH = (int)expectSize.y;
                    _expectW = (int)_pixelW;
                    _expectH = (int)_pixelH;
                }
                if (_SysFontTest(
                        text,
                        FontName,
                        (int)style,
                        (int)fontSize,
                        (int)borderTime,
                        (int)_expectW,
                        (int)_expectH,
                        ref _pixelW,
                        ref _pixelH))
                {
                    boundW = _pixelW;
                    boundH = _pixelH;
                    byte[] rgba = new byte[_pixelW * _pixelH * 4];
                    _SysFontGetPixels(
                        text,
                        FontName,
                        (int)style,
                        (int)fontSize,
                        (int)fontColor,
                        (int)borderTime,
                        (int)borderColor,
                        (int)_pixelW,
                        (int)_pixelH,
                        rgba);
                    Texture2D mTexture = new UnityEngine.Texture2D(_pixelW, _pixelH, TextureFormat.RGBA32, false, true);
                    mTexture.filterMode = FilterMode.Bilinear;
                    mTexture.wrapMode = TextureWrapMode.Clamp;
                    mTexture.anisoLevel = 0;
                    mTexture.mipMapBias = 0;
                    mTexture.LoadRawTextureData(rgba);
                    if (readable)
                    {
                        mTexture.Apply(false, false);
                    }
                    else
                    {
                        mTexture.Apply(false, true);
                    }
                    return mTexture;
                }
            }
            catch (Exception) { }
            Texture2D tex = new UnityEngine.Texture2D(8, 8, TextureFormat.RGBA32, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            boundW = 8;
            boundH = 8;
            tex.Apply(false, true);
            return tex;
        }

        public bool TestTextLineBreak(string text, float size, TextFontStyle style,
            TextBorderStyle borderTime,
            float testWidth,
            out float realWidth,
            out float realHeight)
        {
            int tw = 0;
            int th = 0;
            try
            {
                _SysFontTest(text, FontName, (int)style, (int)size, (int)borderTime, 0, 0, ref tw, ref th);
                realWidth = tw;
                realHeight = th;
                if (realWidth > testWidth)
                {
                    _SysFontTest(text, FontName, (int)style, (int)size, (int)borderTime, (int)testWidth, 0, ref tw, ref th);
                    realWidth = tw;
                    return true;
                }
            }
            catch (Exception)
            {
                realWidth = tw;
                realHeight = th;
            }
            return false;
        }




        private static void InnRect(Texture2D src, ref int sx, ref int sy)
        {
            if (sx < 0)
                sx = 0;
            if (sx >= src.width)
                sx = src.width - 1;
            if (sy < 0)
                sy = 0;
            if (sy >= src.height)
                sy = src.height - 1;
        }


        public void CopyPixels(Texture2D src, int sx, int sy, int sw, int sh, Texture2D dst, int dx, int dy)
        {
            int sx2 = sx + sw;
            int sy2 = sy + sh;
            int dx2 = dx + sw;
            int dy2 = dy + sh;
            InnRect(src, ref sx, ref sy);
            InnRect(src, ref sx2, ref sy2);
            InnRect(dst, ref dx, ref dy);
            InnRect(dst, ref dx2, ref dy2);

            sw = Mathf.Min(sx2 - sx, dx2 - dx);
            sh = Mathf.Min(sy2 - sy, dy2 - dy);

            try
            {
                if (sw > 0 && sh > 0)
                {
                    UnityEngine.Color[] colors = src.GetPixels(sx, sy, sw, sh);
                    dst.SetPixels(dx, dy, sw, sh, colors);
                    dst.Apply();
                }
            }
            catch (Exception err)
            {
                Debug.LogError(err.Message);
                Debug.LogException(err);
            }
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region IME
#if HZUI

        private UnityPlatformIOSTextInput mTextInput = null;
        private GameObject mGameObject = null;


        public void OpenIME(DeepCore.GUI.UI.UITextInput input)
        {
            if (mGameObject == null)
            {
                mGameObject = new GameObject();
                GameObject.DontDestroyOnLoad(mGameObject);
            }

            if (mTextInput == null)
            {
                mTextInput = mGameObject.AddComponent<UnityPlatformIOSTextInput>();
            }

            mTextInput.SetInput(input);
        }

        public void CloseIME()
        {
            if (mTextInput != null) { mTextInput.SetInput(null); }
        }
#endif
        #endregion
        //----------------------------------------------------------------------------------------------------------

        #region MPQ

        public bool IsNativeUnzip { get { return true; } }


        public long GetAvaliableSpace(string path)
        {
            try
            {
                DriveInfo drive = new DriveInfo(Directory.GetDirectoryRoot(path));
                return drive.AvailableFreeSpace;
            }
            catch (Exception)
            {
                return long.MaxValue;
            }
        }

        public long GetTotalSpace(string path)
        {
            try
            {
                DriveInfo drive = new DriveInfo(Directory.GetDirectoryRoot(path));
                return drive.TotalSize;
            }
            catch (Exception)
            {
                return long.MaxValue;
            }
        }


        private class DecompressTask
        {
            private bool result;
            private bool isDone;

            public bool Run(string src, string dst, AtomicLong process)
            {
                isDone = false;
                var task = new Thread(() =>
                {
                    try
                    {
                        result = _Decompress_z(src, dst);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message + "\n" + err.StackTrace);
                        result = false;
                    }
                    finally
                    {
                        isDone = true;
                    }
                });
                task.Name = "_Decompress_z";
                task.Start();
                long total = 0;
                while (!isDone)
                {
                    Thread.Sleep(100);
                    long bytes = _Decompress_bytes();
                    if (total < bytes)
                    {
                        process += (bytes - total);
                        total = bytes;
                    }
                }
                return this.result;
            }
        }
        public bool NativeDecompressFile(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip_file, MPQUpdater.RemoteFileInfo mpq_file, AtomicLong current_unzip_bytes)
        {
            if (zip_file.file.Name.EndsWith(".z"))
            {
                var task = new DecompressTask();
                return task.Run(zip_file.file.FullName, mpq_file.file.FullName, current_unzip_bytes);
            }
            else
            {
                throw new Exception("iOS只支持.z格式Native解压缩！");
            }
        }
        public bool NativeDecompressMemory(byte[] src, int s_start, int s_end, byte[] dst, int dst_start, int dst_end)
        {
            return _Decompress_z_mem(src, s_start, s_end, dst, dst_start, dst_end);
        }
        public bool NativeDecompressMemory(ArraySegment<byte> src, ArraySegment<byte> dst)
        {
            return _Decompress_z_mem(src.Array, src.Offset, src.Offset + src.Count, dst.Array, dst.Offset, dst.Offset + dst.Count);
        }

        public bool NativeGetFileMD5(string fullname, out string md5string)
        {
            byte[] md5 = new byte[32];
            md5string = string.Empty;
            if (_Md5_CheckFile(fullname, md5))
            {
                for (int i = 0; i < md5.Length; i++)
                {
                    md5string += (char)md5[i];
                }
                return true;
            }
            return false;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------

    }
}