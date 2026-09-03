using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DeepCore.IO;
using System.Reflection;
using DeepCore.Unity3D.Impl;

namespace DeepCore.Unity3D.Impl
{
    public static partial class UnityShaders
    {
        public static void InitShaders()
        {
            Debug.Log("- InitShaders ");
#if HZUI
            MFUI_IMG_BASE_Shader = new Material(Shader.Find("iPhone/MFUI_Image"));
            MFUI_IMG_GRAY_Shader = new Material(Shader.Find("iPhone/MFUI_ImageGray"));

            MFUI_M3Z_BASE_Shader = new Material(Shader.Find("iPhone/MFUI_ImageMask"));
            MFUI_M3Z_GRAY_Shader = new Material(Shader.Find("iPhone/MFUI_ImageMaskGray"));

            MFUI_TXT_BASE_Shader = new Material(Shader.Find("iPhone/MFUI_Text"));
            MFUI_TXT_GRAY_Shader = new Material(Shader.Find("iPhone/MFUI_TextGray"));

            MFUI_Shape_Shader = new Material(Shader.Find("iPhone/MFUI_Shape"));

            MFUI_IMG_BASE_Shader.mainTextureScale = new Vector2(1, 1);
            MFUI_IMG_GRAY_Shader.mainTextureScale = new Vector2(1, 1);
            MFUI_M3Z_BASE_Shader.mainTextureScale = new Vector2(1, 1);
            MFUI_M3Z_GRAY_Shader.mainTextureScale = new Vector2(1, 1);

            MFUI_IMG_BASE_Shader.color = UnityEngine.Color.white;
            MFUI_IMG_GRAY_Shader.color = UnityEngine.Color.white;
            MFUI_M3Z_BASE_Shader.color = UnityEngine.Color.white;
            MFUI_M3Z_GRAY_Shader.color = UnityEngine.Color.white;
            MFUI_TXT_BASE_Shader.color = UnityEngine.Color.white;
            MFUI_TXT_GRAY_Shader.color = UnityEngine.Color.white;

            MFUI_Shape_Shader.color = UnityEngine.Color.white;
#endif
            MFUGUI_Image_Shader = Shader.Find("MFUGUI/Image");
            MFUGUI_ImageM3Z_Shader = Shader.Find("MFUGUI/ImageM3Z");
            MFUGUI_TextGray_Shader = Shader.Find("MFUGUI/TextGray");
        }

        public static Shader MFUGUI_Image_Shader { get; private set; }
        public static Shader MFUGUI_ImageM3Z_Shader { get; private set; }
        public static Shader MFUGUI_TextGray_Shader { get; private set; }

        public static Material CreateMaterialUGUI(UnityImage text)
        {
            if (text.TextureMask == null)
            {
                Material ret = new Material(MFUGUI_Image_Shader);
                ret.mainTextureScale = new Vector2(1, 1);
                ret.color = UnityEngine.Color.white;
                ret.mainTexture = text.Texture;
                //ret.SetFloat("_Gray", 1);
                ret.SetPass(0);
                return ret;
            }
            else
            {
                Material ret = new Material(MFUGUI_ImageM3Z_Shader);
                ret.mainTextureScale = new Vector2(1, 1);
                ret.color = UnityEngine.Color.white;
                ret.mainTexture = text.Texture;
                ret.SetTexture("_MaskTex", text.TextureMask);
                //ret.SetFloat("_Gray", 1);
                ret.SetPass(0);
                return ret;
            }
        }
#if HZUI
        public static Material MFUI_IMG_BASE_Shader { get; private set; }
        public static Material MFUI_IMG_GRAY_Shader { get; private set; }
        public static Material MFUI_M3Z_BASE_Shader { get; private set; }
        public static Material MFUI_M3Z_GRAY_Shader { get; private set; }
        public static Material MFUI_TXT_BASE_Shader { get; private set; }
        public static Material MFUI_TXT_GRAY_Shader { get; private set; }
        public static Material MFUI_Shape_Shader;

        private static Color mColorShape = Color.white;
        private static Color mColorImage = Color.white;
        private static DeepCore.GUI.Display.Blend mBlend = DeepCore.GUI.Display.Blend.BLEND_MODE_NORMAL;

        public static void BeginText(UnityTextLayer text)
        {
            switch (mBlend)
            {
                case DeepCore.GUI.Display.Blend.BLEND_MODE_GRAY:
                    MFUI_TXT_GRAY_Shader.mainTexture = text.mTexture;
                    MFUI_TXT_GRAY_Shader.SetPass(0);
                    break;
                default:
                    MFUI_TXT_BASE_Shader.mainTexture = text.mTexture;
                    MFUI_TXT_BASE_Shader.SetPass(0);
                    break;
            }
        }
        public static void BeginImage(IUnityImageInterface image)
        {
            if (image.TextureMask == null)
            {
                switch (mBlend)
                {
                    case DeepCore.GUI.Display.Blend.BLEND_MODE_GRAY:
                        MFUI_IMG_GRAY_Shader.mainTexture = image.Texture;
                        MFUI_IMG_GRAY_Shader.SetPass(0);
                        break;
                    default:
                        MFUI_IMG_BASE_Shader.mainTexture = image.Texture;
                        MFUI_IMG_BASE_Shader.SetPass(0);
                        break;
                }
            }
            else
            {
                switch (mBlend)
                {
                    case DeepCore.GUI.Display.Blend.BLEND_MODE_GRAY:
                        MFUI_M3Z_GRAY_Shader.mainTexture = image.Texture;
                        MFUI_M3Z_GRAY_Shader.SetTexture("_MaskTex", image.TextureMask);
                        MFUI_M3Z_GRAY_Shader.SetPass(0);
                        break;
                    default:
                        MFUI_M3Z_BASE_Shader.mainTexture = image.Texture;
                        MFUI_M3Z_BASE_Shader.SetTexture("_MaskTex", image.TextureMask);
                        MFUI_M3Z_BASE_Shader.SetPass(0);
                        break;
                }
            }
        }
        public static void BeginShape()
        {
            MFUI_Shape_Shader.SetPass(0);
        }


        public static void SetColor(UnityEngine.Color color)
        {
            mColorShape = color;
            MFUI_Shape_Shader.SetColor("_clrBase", mColorShape);
        }

        public static void SetAlpha(float a)
        {
            mColorShape.a = a;
            mColorImage.a = a;
            MFUI_IMG_BASE_Shader.SetColor("_clrBase", mColorImage);
            MFUI_IMG_GRAY_Shader.SetColor("_clrBase", mColorImage);
            MFUI_M3Z_BASE_Shader.SetColor("_clrBase", mColorImage);
            MFUI_M3Z_GRAY_Shader.SetColor("_clrBase", mColorImage);
            MFUI_TXT_BASE_Shader.SetColor("_clrBase", mColorImage);
            MFUI_TXT_GRAY_Shader.SetColor("_clrBase", mColorImage);
            MFUI_Shape_Shader.SetColor("_clrBase", mColorShape);
        }

        public static void SetBlend(DeepCore.GUI.Display.Blend blend)
        {
            mBlend = blend;
        }
#endif
    }
}
