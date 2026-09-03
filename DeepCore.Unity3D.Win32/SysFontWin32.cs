using DeepCore;
using DeepCore.GUI.Data;
using DeepCore.GUI.Gemo;
using System;
using System.Drawing.Text;
using UnityEngine;

namespace CommonUI_Win32
{
    public class SysFontWin32
    {
        private static SysFontWin32 _instance;
        public static SysFontWin32 Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SysFontWin32();
                }
                return _instance;
            }
        }
        public static System.Drawing.Graphics TestGFX { get { return Instance.testGFX; } }
        
        private System.Drawing.Bitmap testBuffer;
        private System.Drawing.Graphics testGFX ;
        private System.Drawing.FontFamily testFontFamily;
        private PrivateFontCollection loadFonts;
        private SysFontWin32()
        {
            this.testBuffer = new System.Drawing.Bitmap(100, 100);
            this.testGFX = System.Drawing.Graphics.FromImage(testBuffer);
            this.testFontFamily = new System.Drawing.FontFamily("Microsoft YaHei UI");
            this.loadFonts = new PrivateFontCollection();
        }
        ~SysFontWin32() 
        {
            loadFonts.Dispose();
        }


        public static void LoadFont(string filepath)
        {
            try
            {
                Instance.loadFonts.AddFontFile(filepath);
                Instance.testFontFamily = Instance.loadFonts.Families[0];
            }
            catch (Exception err) { Debug.LogException(err); }
        }
        public static void LoadFontBinary(byte[] data)
        {
            try
            {
                Instance.loadFonts.AddMemoryFont(CUtils.ToIntPtr(data), data.Length);
                Instance.testFontFamily = Instance.loadFonts.Families[0];
            }
            catch (Exception err) { Debug.LogException(err); }
        }

        public static System.Drawing.Font CreateFont(float size, TextFontStyle style)
        {
            var fs = (System.Drawing.FontStyle)style;
            return new System.Drawing.Font(Instance.testFontFamily, size, fs, System.Drawing.GraphicsUnit.Pixel, 137);
        }

        static public System.Drawing.SizeF GetTextBounds(
            string text,
            System.Drawing.Font font,
            TextBorderStyle borderTime,
            float expectWidth = 0)
        {
            System.Drawing.SizeF size;
            if (expectWidth > 0)
            {
                size = Instance.testGFX.MeasureString(text, font, (int)(expectWidth), System.Drawing.StringFormat.GenericTypographic);
            }
            else
            {
                size = Instance.testGFX.MeasureString(text, font, int.MaxValue, System.Drawing.StringFormat.GenericTypographic);
            }
            size.Width = Mathf.CeilToInt(size.Width + 3f);
            size.Height = Mathf.CeilToInt(size.Height + 3f);
            return size;
        }

        public static bool TestTextLineBreak(string text, float size, TextFontStyle style,
            TextBorderStyle borderTime,
            float testWidth,
            out float realWidth,
            out float realHeight)
        {
            realWidth = 0;
            realHeight = 0;
            testWidth = Mathf.CeilToInt(testWidth);
            System.Drawing.Font cur_font = CreateFont(size, style);
            System.Drawing.SizeF max = GetTextBounds(text, cur_font, borderTime, 0);
            realWidth = max.Width;
            realHeight = max.Height;
            if (testWidth > 0 && max.Width > testWidth)
            {
                System.Drawing.SizeF min = GetTextBounds(text, cur_font, borderTime, testWidth);
                realWidth = min.Width;
                realHeight = min.Height;
                return true;
            }
            return false;
        }

        public static Size2D SysFontTexture(
            string text,
            TextFontStyle style,
            float fontSize,
            uint fontColor,
            TextBorderStyle borderTime,
            uint borderColor,
            Size2D expectSize,
            out byte[] _pixelData,
            out int _pixelW,
            out int _pixelH)
        {
            System.Drawing.Font font = SysFontWin32.CreateFont(fontSize, style);

            System.Drawing.SizeF bounds = SysFontWin32.GetTextBounds(text, font, borderTime, expectSize != null ? expectSize.width : 0);

            System.Drawing.Bitmap src = SysFontWin32.GenStringBuffer(
                Mathf.CeilToInt(bounds.Width),
                Mathf.CeilToInt(bounds.Height),
                text, font, fontColor, borderTime, borderColor);
            try
            {
                bounds.Width = src.Width;
                bounds.Height = src.Height;
                _pixelW = src.Width;
                _pixelH = src.Height;
                _pixelData = new byte[_pixelW * _pixelH * 4];
                System.Drawing.Color pixel;
                int pos;
                for (int y = 0; y < _pixelH; y++)
                {
                    for (int x = 0; x < _pixelW; x++)
                    {
                        pixel = src.GetPixel(x, _pixelH - y - 1);
                        pos = (x + y * _pixelW) * 4;
                        _pixelData[pos + 0] = (pixel.R);
                        _pixelData[pos + 1] = (pixel.G);
                        _pixelData[pos + 2] = (pixel.B);
                        _pixelData[pos + 3] = (pixel.A);
                    }
                }
            }
            finally
            {
                src.Dispose();
            }
            return new Size2D(bounds.Width, bounds.Height);
        }

        static public System.Drawing.Bitmap GenStringBuffer(
            int w, int h, string text,
            System.Drawing.Font font,
            uint fontColor,
            TextBorderStyle borderTime,
            uint borderColor)
        {
            System.Drawing.Bitmap src = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(src))
            {
                gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

                System.Drawing.SolidBrush bbrush = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb((int)DeepCore.GUI.Display.Color.ToARGB(borderColor)));
                System.Drawing.SolidBrush fbrush = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb((int)DeepCore.GUI.Display.Color.ToARGB(fontColor)));

                //test board
                //gfx.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Red), 0, 0, w - 1, h - 1);

                float[,] offset_8 =
                { 
                    { 0, 0},{ 1, 0},{ 2, 0},
                    { 0, 1},/*1, 1*/{ 2, 1},
                    { 0, 2},{ 1, 2},{ 2, 2}
                };
                float[,] offset_4 =
                {
                    /*0, 0*/{ 1, 0},/*2, 0*/
                    { 0, 1},/*1, 1*/{ 2, 1},
                    /*0, 2*/{ 1, 2},/*2, 2*/
                };

                System.Drawing.RectangleF expectRect = new System.Drawing.RectangleF(1f, 1f, w - 1f, h - 1f);
                DeepCore.GUI.Data.TextBorderStyle bt = (DeepCore.GUI.Data.TextBorderStyle)borderTime;
                switch (bt)
                {
                    case DeepCore.GUI.Data.TextBorderStyle.Border_4:
                        for (int i = 0; i < 4; i++)
                        {
                            SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, offset_4[i, 0], offset_4[i, 1]);
                        }
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Border:
                        for (int i = 0; i < 8; i++)
                        {
                            SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, offset_8[i, 0], offset_8[i, 1]);
                        }
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 1, 2);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_L_T:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 0, 0);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_C_T:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 1, 0);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_R_T:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 2, 0);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_L_C:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 0, 1);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_C_C:
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_R_C:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 2, 1);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_L_B:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 0, 2);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_C_B:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 1, 2);
                        break;
                    case DeepCore.GUI.Data.TextBorderStyle.Shadow_R_B:
                        SysFontWin32.DrawString(text, gfx, font, bbrush, expectRect, 2, 2);
                        break;
                }

                SysFontWin32.DrawString(text, gfx, font, fbrush, expectRect, 1, 1);
            }


            return src;
        }



        //-------------------------------------------------------------------------------------------------------------------
        

        static private void DrawString(
            string text,
            System.Drawing.Graphics gfx,
            System.Drawing.Font font,
            System.Drawing.SolidBrush brush,
            System.Drawing.RectangleF expectRect,
            float x, float y)
        {
            gfx.TranslateTransform(x, y);
            gfx.DrawString(text, font, brush, expectRect, System.Drawing.StringFormat.GenericTypographic);
            gfx.TranslateTransform(-x, -y);
        }





    }
}
