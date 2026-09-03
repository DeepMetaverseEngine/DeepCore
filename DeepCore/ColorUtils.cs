using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{
    public class Colors
    {
        public static uint RGBA2ARGB(uint rgba)
        {
            var a = (0x000000ff & rgba) << 24;
            return (rgba >> 24) | a;
        }
        public static uint ARGB2RGBA(uint argb)
        {
            var a = (0xff000000 & argb) >> 24;
            return (argb << 8) | a;
        }
        public static void DecodeRGBA(uint rgba, out float r, out float g, out float b, out float a)
        {
            r = ((0xff000000 & rgba) >> 24) / 255f;
            g = ((0x00ff0000 & rgba) >> 16) / 255f;
            b = ((0x0000ff00 & rgba) >> 8) / 255f;
            a = ((0x000000ff & rgba)) / 255f;
        }
        public static void DecodeRGBA(uint rgba, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)((0xff000000 & rgba) >> 24);
            g = (byte)((0x00ff0000 & rgba) >> 16);
            b = (byte)((0x0000ff00 & rgba) >> 8);
            a = (byte)((0x000000ff & rgba) >> 0);
        }
        public static void DecodeARGB(uint argb, out float r, out float g, out float b, out float a)
        {
            a = ((0xff000000 & argb) >> 24) / 255f;
            r = ((0x00ff0000 & argb) >> 16) / 255f;
            g = ((0x0000ff00 & argb) >> 8) / 255f;
            b = ((0x000000ff & argb)) / 255f;
        }
        public static void DecodeARGB(uint argb, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((0xff000000 & argb) >> 24);
            r = (byte)((0x00ff0000 & argb) >> 16);
            g = (byte)((0x0000ff00 & argb) >> 8);
            b = (byte)((0x000000ff & argb) >> 0);
        }
        public static void DecodeABGR(uint argb, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((0xff000000 & argb) >> 24);
            b = (byte)((0x00ff0000 & argb) >> 16);
            g = (byte)((0x0000ff00 & argb) >> 8);
            r = (byte)((0x000000ff & argb) >> 0);
        }

        public static void EncodeRGBA(ref uint rgba, int r, int g, int b, int a = 255)
        {
            rgba = 0;
            rgba |= ((uint)(r)) << 24;
            rgba |= ((uint)(g)) << 16;
            rgba |= ((uint)(b)) << 8;
            rgba |= ((uint)(a));
        }
        public static void EncodeRGBA(ref uint rgba, float r, float g, float b, float a = 1f)
        {
            rgba = 0;
            rgba |= ((uint)(r * 255)) << 24;
            rgba |= ((uint)(g * 255)) << 16;
            rgba |= ((uint)(b * 255)) << 8;
            rgba |= ((uint)(a * 255));
        }
        public static void EncodeARGB(ref uint argb, int r, int g, int b, int a = 255)
        {
            argb = 0;
            argb |= ((uint)(a)) << 24;
            argb |= ((uint)(r)) << 16;
            argb |= ((uint)(g)) << 8;
            argb |= ((uint)(b));
        }
        public static void EncodeARGB(ref uint argb, float r, float g, float b, float a = 1f)
        {
            argb = 0;
            argb |= ((uint)(a * 255)) << 24;
            argb |= ((uint)(r * 255)) << 16;
            argb |= ((uint)(g * 255)) << 8;
            argb |= ((uint)(b * 255));
        }






        public static uint EncodeRGBA(int r, int g, int b, int a = 255)
        {
            uint ret = 0;
            Colors.EncodeRGBA(ref ret, r, g, b, a);
            return ret;
        }
        public static uint EncodeRGBA(float r, float g, float b, float a = 1f)
        {
            uint ret = 0;
            Colors.EncodeRGBA(ref ret, r, g, b, a);
            return ret;
        }
        public static uint EncodeARGB(int r, int g, int b, int a = 255)
        {
            uint ret = 0;
            Colors.EncodeARGB(ref ret, r, g, b, a);
            return ret;
        }
        public static uint EncodeARGB(float r, float g, float b, float a = 1f)
        {
            uint ret = 0;
            Colors.EncodeARGB(ref ret, r, g, b, a);
            return ret;
        }

        //----------------------------------------------------------------------------------------------------

        public static int RGBA2ARGB(int rgba)
        {
            var a = (0x000000ff & rgba) << 24;
            return (rgba >> 24) | a;
        }
        public static int ARGB2RGBA(int argb)
        {
            var a = (int)((0xff000000 & argb) >> 24);
            return (argb << 8) | a;
        }
        public static void DecodeRGBA(int rgba, out float r, out float g, out float b, out float a)
        {
            r = ((0xff000000 & rgba) >> 24) / 255f;
            g = ((0x00ff0000 & rgba) >> 16) / 255f;
            b = ((0x0000ff00 & rgba) >> 8) / 255f;
            a = ((0x000000ff & rgba)) / 255f;
        }
        public static void DecodeRGBA(int rgba, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)((0xff000000 & rgba) >> 24);
            g = (byte)((0x00ff0000 & rgba) >> 16);
            b = (byte)((0x0000ff00 & rgba) >> 8);
            a = (byte)((0x000000ff & rgba) >> 0);
        }
        public static void DecodeARGB(int argb, out float r, out float g, out float b, out float a)
        {
            a = ((0xff000000 & argb) >> 24) / 255f;
            r = ((0x00ff0000 & argb) >> 16) / 255f;
            g = ((0x0000ff00 & argb) >> 8) / 255f;
            b = ((0x000000ff & argb)) / 255f;
        }
        public static void DecodeARGB(int argb, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((0xff000000 & argb) >> 24);
            r = (byte)((0x00ff0000 & argb) >> 16);
            g = (byte)((0x0000ff00 & argb) >> 8);
            b = (byte)((0x000000ff & argb) >> 0);
        }
        public static void DecodeABGR(int argb, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((0xff000000 & argb) >> 24);
            b = (byte)((0x00ff0000 & argb) >> 16);
            g = (byte)((0x0000ff00 & argb) >> 8);
            r = (byte)((0x000000ff & argb) >> 0);
        }

        public static void EncodeRGBA(ref int rgba, int r, int g, int b, int a = 255)
        {
            rgba = 0;
            rgba |= ((int)(r)) << 24;
            rgba |= ((int)(g)) << 16;
            rgba |= ((int)(b)) << 8;
            rgba |= ((int)(a));
        }
        public static void EncodeRGBA(ref int rgba, float r, float g, float b, float a = 1f)
        {
            rgba = 0;
            rgba |= ((int)(r * 255)) << 24;
            rgba |= ((int)(g * 255)) << 16;
            rgba |= ((int)(b * 255)) << 8;
            rgba |= ((int)(a * 255));
        }
        public static void EncodeARGB(ref int argb, int r, int g, int b, int a = 255)
        {
            argb = 0;
            argb |= ((int)(a)) << 24;
            argb |= ((int)(r)) << 16;
            argb |= ((int)(g)) << 8;
            argb |= ((int)(b));
        }
        public static void EncodeARGB(ref int argb, float r, float g, float b, float a = 1f)
        {
            argb = 0;
            argb |= ((int)(a * 255)) << 24;
            argb |= ((int)(r * 255)) << 16;
            argb |= ((int)(g * 255)) << 8;
            argb |= ((int)(b * 255));
        }

        public static int EncodeIntRGBA(int r, int g, int b, int a = 255)
        {
            int ret = 0;
            Colors.EncodeRGBA(ref ret, r, g, b, a);
            return ret;
        }
        public static int EncodeIntRGBA(float r, float g, float b, float a = 1f)
        {
            int ret = 0;
            Colors.EncodeRGBA(ref ret, r, g, b, a);
            return ret;
        }
        public static int EncodeIntARGB(int r, int g, int b, int a = 255)
        {
            int ret = 0;
            Colors.EncodeARGB(ref ret, r, g, b, a);
            return ret;
        }
        public static int EncodeIntARGB(float r, float g, float b, float a = 1f)
        {
            int ret = 0;
            Colors.EncodeARGB(ref ret, r, g, b, a);
            return ret;
        }


        //----------------------------------------------------------------------------------------------------

        public class RGBA
        {
            #region System.Drawing.Color
            public static readonly uint AliceBlue = EncodeRGBA(240, 248, 255);
            public static readonly uint LightSalmon = EncodeRGBA(255, 160, 122);
            public static readonly uint AntiqueWhite = EncodeRGBA(250, 235, 215);
            public static readonly uint LightSeaGreen = EncodeRGBA(32, 178, 170);
            public static readonly uint Aqua = EncodeRGBA(0, 255, 255);
            public static readonly uint LightSkyBlue = EncodeRGBA(135, 206, 250);
            public static readonly uint Aquamarine = EncodeRGBA(127, 255, 212);
            public static readonly uint LightSlateGray = EncodeRGBA(119, 136, 153);
            public static readonly uint Azure = EncodeRGBA(240, 255, 255);
            public static readonly uint LightSteelBlue = EncodeRGBA(176, 196, 222);
            public static readonly uint Beige = EncodeRGBA(245, 245, 220);
            public static readonly uint LightYellow = EncodeRGBA(255, 255, 224);
            public static readonly uint Bisque = EncodeRGBA(255, 228, 196);
            public static readonly uint Lime = EncodeRGBA(0, 255, 0);
            public static readonly uint Black = EncodeRGBA(0, 0, 0);
            public static readonly uint LimeGreen = EncodeRGBA(50, 205, 50);
            public static readonly uint BlanchedAlmond = EncodeRGBA(255, 255, 205);
            public static readonly uint Linen = EncodeRGBA(250, 240, 230);
            public static readonly uint Blue = EncodeRGBA(0, 0, 255);
            public static readonly uint Magenta = EncodeRGBA(255, 0, 255);
            public static readonly uint BlueViolet = EncodeRGBA(138, 43, 226);
            public static readonly uint Maroon = EncodeRGBA(128, 0, 0);
            public static readonly uint Brown = EncodeRGBA(165, 42, 42);
            public static readonly uint MediumAquamarine = EncodeRGBA(102, 205, 170);
            public static readonly uint BurlyWood = EncodeRGBA(222, 184, 135);
            public static readonly uint MediumBlue = EncodeRGBA(0, 0, 205);
            public static readonly uint CadetBlue = EncodeRGBA(95, 158, 160);
            public static readonly uint MediumOrchid = EncodeRGBA(186, 85, 211);
            public static readonly uint Chartreuse = EncodeRGBA(127, 255, 0);
            public static readonly uint MediumPurple = EncodeRGBA(147, 112, 219);
            public static readonly uint Chocolate = EncodeRGBA(210, 105, 30);
            public static readonly uint MediumSeaGreen = EncodeRGBA(60, 179, 113);
            public static readonly uint Coral = EncodeRGBA(255, 127, 80);
            public static readonly uint MediumSlateBlue = EncodeRGBA(123, 104, 238);
            public static readonly uint CornflowerBlue = EncodeRGBA(100, 149, 237);
            public static readonly uint MediumSpringGreen = EncodeRGBA(0, 250, 154);
            public static readonly uint Cornsilk = EncodeRGBA(255, 248, 220);
            public static readonly uint MediumTurquoise = EncodeRGBA(72, 209, 204);
            public static readonly uint Crimson = EncodeRGBA(220, 20, 60);
            public static readonly uint MediumVioletRed = EncodeRGBA(199, 21, 112);
            public static readonly uint Cyan = EncodeRGBA(0, 255, 255);
            public static readonly uint MidnightBlue = EncodeRGBA(25, 25, 112);
            public static readonly uint DarkBlue = EncodeRGBA(0, 0, 139);
            public static readonly uint MintCream = EncodeRGBA(245, 255, 250);
            public static readonly uint DarkCyan = EncodeRGBA(0, 139, 139);
            public static readonly uint MistyRose = EncodeRGBA(255, 228, 225);
            public static readonly uint DarkGoldenrod = EncodeRGBA(184, 134, 11);
            public static readonly uint Moccasin = EncodeRGBA(255, 228, 181);
            public static readonly uint DarkGray = EncodeRGBA(169, 169, 169);
            public static readonly uint NavajoWhite = EncodeRGBA(255, 222, 173);
            public static readonly uint DarkGreen = EncodeRGBA(0, 100, 0);
            public static readonly uint Navy = EncodeRGBA(0, 0, 128);
            public static readonly uint DarkKhaki = EncodeRGBA(189, 183, 107);
            public static readonly uint OldLace = EncodeRGBA(253, 245, 230);
            public static readonly uint DarkMagena = EncodeRGBA(139, 0, 139);
            public static readonly uint Olive = EncodeRGBA(128, 128, 0);
            public static readonly uint DarkOliveGreen = EncodeRGBA(85, 107, 47);
            public static readonly uint OliveDrab = EncodeRGBA(107, 142, 45);
            public static readonly uint DarkOrange = EncodeRGBA(255, 140, 0);
            public static readonly uint Orange = EncodeRGBA(255, 165, 0);
            public static readonly uint DarkOrchid = EncodeRGBA(153, 50, 204);
            public static readonly uint OrangeRed = EncodeRGBA(255, 69, 0);
            public static readonly uint DarkRed = EncodeRGBA(139, 0, 0);
            public static readonly uint Orchid = EncodeRGBA(218, 112, 214);
            public static readonly uint DarkSalmon = EncodeRGBA(233, 150, 122);
            public static readonly uint PaleGoldenrod = EncodeRGBA(238, 232, 170);
            public static readonly uint DarkSeaGreen = EncodeRGBA(143, 188, 143);
            public static readonly uint PaleGreen = EncodeRGBA(152, 251, 152);
            public static readonly uint DarkSlateBlue = EncodeRGBA(72, 61, 139);
            public static readonly uint PaleTurquoise = EncodeRGBA(175, 238, 238);
            public static readonly uint DarkSlateGray = EncodeRGBA(40, 79, 79);
            public static readonly uint PaleVioletRed = EncodeRGBA(219, 112, 147);
            public static readonly uint DarkTurquoise = EncodeRGBA(0, 206, 209);
            public static readonly uint PapayaWhip = EncodeRGBA(255, 239, 213);
            public static readonly uint DarkViolet = EncodeRGBA(148, 0, 211);
            public static readonly uint PeachPuff = EncodeRGBA(255, 218, 155);
            public static readonly uint DeepPink = EncodeRGBA(255, 20, 147);
            public static readonly uint Peru = EncodeRGBA(205, 133, 63);
            public static readonly uint DeepSkyBlue = EncodeRGBA(0, 191, 255);
            public static readonly uint Pink = EncodeRGBA(255, 192, 203);
            public static readonly uint DimGray = EncodeRGBA(105, 105, 105);
            public static readonly uint Plum = EncodeRGBA(221, 160, 221);
            public static readonly uint DodgerBlue = EncodeRGBA(30, 144, 255);
            public static readonly uint PowderBlue = EncodeRGBA(176, 224, 230);
            public static readonly uint Firebrick = EncodeRGBA(178, 34, 34);
            public static readonly uint Purple = EncodeRGBA(128, 0, 128);
            public static readonly uint FloralWhite = EncodeRGBA(255, 250, 240);
            public static readonly uint Red = EncodeRGBA(255, 0, 0);
            public static readonly uint ForestGreen = EncodeRGBA(34, 139, 34);
            public static readonly uint RosyBrown = EncodeRGBA(188, 143, 143);
            public static readonly uint Fuschia = EncodeRGBA(255, 0, 255);
            public static readonly uint RoyalBlue = EncodeRGBA(65, 105, 225);
            public static readonly uint Gainsboro = EncodeRGBA(220, 220, 220);
            public static readonly uint SaddleBrown = EncodeRGBA(139, 69, 19);
            public static readonly uint GhostWhite = EncodeRGBA(248, 248, 255);
            public static readonly uint Salmon = EncodeRGBA(250, 128, 114);
            public static readonly uint Gold = EncodeRGBA(255, 215, 0);
            public static readonly uint SandyBrown = EncodeRGBA(244, 164, 96);
            public static readonly uint Goldenrod = EncodeRGBA(218, 165, 32);
            public static readonly uint SeaGreen = EncodeRGBA(46, 139, 87);
            public static readonly uint Gray = EncodeRGBA(128, 128, 128);
            public static readonly uint Seashell = EncodeRGBA(255, 245, 238);
            public static readonly uint Green = EncodeRGBA(0, 128, 0);
            public static readonly uint Sienna = EncodeRGBA(160, 82, 45);
            public static readonly uint GreenYellow = EncodeRGBA(173, 255, 47);
            public static readonly uint Silver = EncodeRGBA(192, 192, 192);
            public static readonly uint Honeydew = EncodeRGBA(240, 255, 240);
            public static readonly uint SkyBlue = EncodeRGBA(135, 206, 235);
            public static readonly uint HotPink = EncodeRGBA(255, 105, 180);
            public static readonly uint SlateBlue = EncodeRGBA(106, 90, 205);
            public static readonly uint IndianRed = EncodeRGBA(205, 92, 92);
            public static readonly uint SlateGray = EncodeRGBA(112, 128, 144);
            public static readonly uint Indigo = EncodeRGBA(75, 0, 130);
            public static readonly uint Snow = EncodeRGBA(255, 250, 250);
            public static readonly uint Ivory = EncodeRGBA(255, 240, 240);
            public static readonly uint SpringGreen = EncodeRGBA(0, 255, 127);
            public static readonly uint Khaki = EncodeRGBA(240, 230, 140);
            public static readonly uint SteelBlue = EncodeRGBA(70, 130, 180);
            public static readonly uint Lavender = EncodeRGBA(230, 230, 250);
            public static readonly uint Tan = EncodeRGBA(210, 180, 140);
            public static readonly uint LavenderBlush = EncodeRGBA(255, 240, 245);
            public static readonly uint Teal = EncodeRGBA(0, 128, 128);
            public static readonly uint LawnGreen = EncodeRGBA(124, 252, 0);
            public static readonly uint Thistle = EncodeRGBA(216, 191, 216);
            public static readonly uint LemonChiffon = EncodeRGBA(255, 250, 205);
            public static readonly uint Tomato = EncodeRGBA(253, 99, 71);
            public static readonly uint LightBlue = EncodeRGBA(173, 216, 230);
            public static readonly uint Turquoise = EncodeRGBA(64, 224, 208);
            public static readonly uint LightCoral = EncodeRGBA(240, 128, 128);
            public static readonly uint Violet = EncodeRGBA(238, 130, 238);
            public static readonly uint LightCyan = EncodeRGBA(224, 255, 255);
            public static readonly uint Wheat = EncodeRGBA(245, 222, 179);
            public static readonly uint LightGoldenrodYello = EncodeRGBA(250, 250, 210);
            public static readonly uint White = EncodeRGBA(255, 255, 255);
            public static readonly uint LightGreen = EncodeRGBA(144, 238, 144);
            public static readonly uint WhiteSmoke = EncodeRGBA(245, 245, 245);
            public static readonly uint LightGray = EncodeRGBA(211, 211, 211);
            public static readonly uint Yellow = EncodeRGBA(255, 255, 0);
            public static readonly uint LightPink = EncodeRGBA(255, 182, 193);
            public static readonly uint YellowGreen = EncodeRGBA(154, 205, 50);
            #endregion
        }

        public class ARGB
        {
            #region System.Drawing.Color
            public static readonly uint AliceBlue = EncodeARGB(240, 248, 255);
            public static readonly uint LightSalmon = EncodeARGB(255, 160, 122);
            public static readonly uint AntiqueWhite = EncodeARGB(250, 235, 215);
            public static readonly uint LightSeaGreen = EncodeARGB(32, 178, 170);
            public static readonly uint Aqua = EncodeARGB(0, 255, 255);
            public static readonly uint LightSkyBlue = EncodeARGB(135, 206, 250);
            public static readonly uint Aquamarine = EncodeARGB(127, 255, 212);
            public static readonly uint LightSlateGray = EncodeARGB(119, 136, 153);
            public static readonly uint Azure = EncodeARGB(240, 255, 255);
            public static readonly uint LightSteelBlue = EncodeARGB(176, 196, 222);
            public static readonly uint Beige = EncodeARGB(245, 245, 220);
            public static readonly uint LightYellow = EncodeARGB(255, 255, 224);
            public static readonly uint Bisque = EncodeARGB(255, 228, 196);
            public static readonly uint Lime = EncodeARGB(0, 255, 0);
            public static readonly uint Black = EncodeARGB(0, 0, 0);
            public static readonly uint LimeGreen = EncodeARGB(50, 205, 50);
            public static readonly uint BlanchedAlmond = EncodeARGB(255, 255, 205);
            public static readonly uint Linen = EncodeARGB(250, 240, 230);
            public static readonly uint Blue = EncodeARGB(0, 0, 255);
            public static readonly uint Magenta = EncodeARGB(255, 0, 255);
            public static readonly uint BlueViolet = EncodeARGB(138, 43, 226);
            public static readonly uint Maroon = EncodeARGB(128, 0, 0);
            public static readonly uint Brown = EncodeARGB(165, 42, 42);
            public static readonly uint MediumAquamarine = EncodeARGB(102, 205, 170);
            public static readonly uint BurlyWood = EncodeARGB(222, 184, 135);
            public static readonly uint MediumBlue = EncodeARGB(0, 0, 205);
            public static readonly uint CadetBlue = EncodeARGB(95, 158, 160);
            public static readonly uint MediumOrchid = EncodeARGB(186, 85, 211);
            public static readonly uint Chartreuse = EncodeARGB(127, 255, 0);
            public static readonly uint MediumPurple = EncodeARGB(147, 112, 219);
            public static readonly uint Chocolate = EncodeARGB(210, 105, 30);
            public static readonly uint MediumSeaGreen = EncodeARGB(60, 179, 113);
            public static readonly uint Coral = EncodeARGB(255, 127, 80);
            public static readonly uint MediumSlateBlue = EncodeARGB(123, 104, 238);
            public static readonly uint CornflowerBlue = EncodeARGB(100, 149, 237);
            public static readonly uint MediumSpringGreen = EncodeARGB(0, 250, 154);
            public static readonly uint Cornsilk = EncodeARGB(255, 248, 220);
            public static readonly uint MediumTurquoise = EncodeARGB(72, 209, 204);
            public static readonly uint Crimson = EncodeARGB(220, 20, 60);
            public static readonly uint MediumVioletRed = EncodeARGB(199, 21, 112);
            public static readonly uint Cyan = EncodeARGB(0, 255, 255);
            public static readonly uint MidnightBlue = EncodeARGB(25, 25, 112);
            public static readonly uint DarkBlue = EncodeARGB(0, 0, 139);
            public static readonly uint MintCream = EncodeARGB(245, 255, 250);
            public static readonly uint DarkCyan = EncodeARGB(0, 139, 139);
            public static readonly uint MistyRose = EncodeARGB(255, 228, 225);
            public static readonly uint DarkGoldenrod = EncodeARGB(184, 134, 11);
            public static readonly uint Moccasin = EncodeARGB(255, 228, 181);
            public static readonly uint DarkGray = EncodeARGB(169, 169, 169);
            public static readonly uint NavajoWhite = EncodeARGB(255, 222, 173);
            public static readonly uint DarkGreen = EncodeARGB(0, 100, 0);
            public static readonly uint Navy = EncodeARGB(0, 0, 128);
            public static readonly uint DarkKhaki = EncodeARGB(189, 183, 107);
            public static readonly uint OldLace = EncodeARGB(253, 245, 230);
            public static readonly uint DarkMagena = EncodeARGB(139, 0, 139);
            public static readonly uint Olive = EncodeARGB(128, 128, 0);
            public static readonly uint DarkOliveGreen = EncodeARGB(85, 107, 47);
            public static readonly uint OliveDrab = EncodeARGB(107, 142, 45);
            public static readonly uint DarkOrange = EncodeARGB(255, 140, 0);
            public static readonly uint Orange = EncodeARGB(255, 165, 0);
            public static readonly uint DarkOrchid = EncodeARGB(153, 50, 204);
            public static readonly uint OrangeRed = EncodeARGB(255, 69, 0);
            public static readonly uint DarkRed = EncodeARGB(139, 0, 0);
            public static readonly uint Orchid = EncodeARGB(218, 112, 214);
            public static readonly uint DarkSalmon = EncodeARGB(233, 150, 122);
            public static readonly uint PaleGoldenrod = EncodeARGB(238, 232, 170);
            public static readonly uint DarkSeaGreen = EncodeARGB(143, 188, 143);
            public static readonly uint PaleGreen = EncodeARGB(152, 251, 152);
            public static readonly uint DarkSlateBlue = EncodeARGB(72, 61, 139);
            public static readonly uint PaleTurquoise = EncodeARGB(175, 238, 238);
            public static readonly uint DarkSlateGray = EncodeARGB(40, 79, 79);
            public static readonly uint PaleVioletRed = EncodeARGB(219, 112, 147);
            public static readonly uint DarkTurquoise = EncodeARGB(0, 206, 209);
            public static readonly uint PapayaWhip = EncodeARGB(255, 239, 213);
            public static readonly uint DarkViolet = EncodeARGB(148, 0, 211);
            public static readonly uint PeachPuff = EncodeARGB(255, 218, 155);
            public static readonly uint DeepPink = EncodeARGB(255, 20, 147);
            public static readonly uint Peru = EncodeARGB(205, 133, 63);
            public static readonly uint DeepSkyBlue = EncodeARGB(0, 191, 255);
            public static readonly uint Pink = EncodeARGB(255, 192, 203);
            public static readonly uint DimGray = EncodeARGB(105, 105, 105);
            public static readonly uint Plum = EncodeARGB(221, 160, 221);
            public static readonly uint DodgerBlue = EncodeARGB(30, 144, 255);
            public static readonly uint PowderBlue = EncodeARGB(176, 224, 230);
            public static readonly uint Firebrick = EncodeARGB(178, 34, 34);
            public static readonly uint Purple = EncodeARGB(128, 0, 128);
            public static readonly uint FloralWhite = EncodeARGB(255, 250, 240);
            public static readonly uint Red = EncodeARGB(255, 0, 0);
            public static readonly uint ForestGreen = EncodeARGB(34, 139, 34);
            public static readonly uint RosyBrown = EncodeARGB(188, 143, 143);
            public static readonly uint Fuschia = EncodeARGB(255, 0, 255);
            public static readonly uint RoyalBlue = EncodeARGB(65, 105, 225);
            public static readonly uint Gainsboro = EncodeARGB(220, 220, 220);
            public static readonly uint SaddleBrown = EncodeARGB(139, 69, 19);
            public static readonly uint GhostWhite = EncodeARGB(248, 248, 255);
            public static readonly uint Salmon = EncodeARGB(250, 128, 114);
            public static readonly uint Gold = EncodeARGB(255, 215, 0);
            public static readonly uint SandyBrown = EncodeARGB(244, 164, 96);
            public static readonly uint Goldenrod = EncodeARGB(218, 165, 32);
            public static readonly uint SeaGreen = EncodeARGB(46, 139, 87);
            public static readonly uint Gray = EncodeARGB(128, 128, 128);
            public static readonly uint Seashell = EncodeARGB(255, 245, 238);
            public static readonly uint Green = EncodeARGB(0, 128, 0);
            public static readonly uint Sienna = EncodeARGB(160, 82, 45);
            public static readonly uint GreenYellow = EncodeARGB(173, 255, 47);
            public static readonly uint Silver = EncodeARGB(192, 192, 192);
            public static readonly uint Honeydew = EncodeARGB(240, 255, 240);
            public static readonly uint SkyBlue = EncodeARGB(135, 206, 235);
            public static readonly uint HotPink = EncodeARGB(255, 105, 180);
            public static readonly uint SlateBlue = EncodeARGB(106, 90, 205);
            public static readonly uint IndianRed = EncodeARGB(205, 92, 92);
            public static readonly uint SlateGray = EncodeARGB(112, 128, 144);
            public static readonly uint Indigo = EncodeARGB(75, 0, 130);
            public static readonly uint Snow = EncodeARGB(255, 250, 250);
            public static readonly uint Ivory = EncodeARGB(255, 240, 240);
            public static readonly uint SpringGreen = EncodeARGB(0, 255, 127);
            public static readonly uint Khaki = EncodeARGB(240, 230, 140);
            public static readonly uint SteelBlue = EncodeARGB(70, 130, 180);
            public static readonly uint Lavender = EncodeARGB(230, 230, 250);
            public static readonly uint Tan = EncodeARGB(210, 180, 140);
            public static readonly uint LavenderBlush = EncodeARGB(255, 240, 245);
            public static readonly uint Teal = EncodeARGB(0, 128, 128);
            public static readonly uint LawnGreen = EncodeARGB(124, 252, 0);
            public static readonly uint Thistle = EncodeARGB(216, 191, 216);
            public static readonly uint LemonChiffon = EncodeARGB(255, 250, 205);
            public static readonly uint Tomato = EncodeARGB(253, 99, 71);
            public static readonly uint LightBlue = EncodeARGB(173, 216, 230);
            public static readonly uint Turquoise = EncodeARGB(64, 224, 208);
            public static readonly uint LightCoral = EncodeARGB(240, 128, 128);
            public static readonly uint Violet = EncodeARGB(238, 130, 238);
            public static readonly uint LightCyan = EncodeARGB(224, 255, 255);
            public static readonly uint Wheat = EncodeARGB(245, 222, 179);
            public static readonly uint LightGoldenrodYello = EncodeARGB(250, 250, 210);
            public static readonly uint White = EncodeARGB(255, 255, 255);
            public static readonly uint LightGreen = EncodeARGB(144, 238, 144);
            public static readonly uint WhiteSmoke = EncodeARGB(245, 245, 245);
            public static readonly uint LightGray = EncodeARGB(211, 211, 211);
            public static readonly uint Yellow = EncodeARGB(255, 255, 0);
            public static readonly uint LightPink = EncodeARGB(255, 182, 193);
            public static readonly uint YellowGreen = EncodeARGB(154, 205, 50);
            #endregion
        }
    }

}
