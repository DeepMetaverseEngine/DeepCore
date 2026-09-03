using DeepCore.Reflection;
using System;
using static DeepCore.Colors;

namespace DeepCore.GUI.Display
{
    /// <summary>
    /// RGBA
    /// </summary>
    public struct Color
    {
        [HexInteger()]
        public uint RGBA;
        public float R
        {
            get => ((0xff000000 & RGBA) >> 24) / 255f;
        }
        public float G
        {
            get => ((0x00ff0000 & RGBA) >> 16) / 255f;
        }
        public float B
        {
            get => ((0x0000ff00 & RGBA) >> 8) / 255f;
        }
        public float A
        {
            get => ((0x000000ff & RGBA)) / 255f;
        }

        public byte R8 => (byte)((0xff000000 & RGBA) >> 24);
        public byte G8 => (byte)((0x00ff0000 & RGBA) >> 16);
        public byte B8 => (byte)((0x0000ff00 & RGBA) >> 8);
        public byte A8 => (byte)((0x000000ff & RGBA));

        public uint ARGB => ToARGB(RGBA);
        public int sARGB { get { unchecked { return (int)ToARGB(RGBA); } } }

        public Color(uint rgba)
        {
            this.RGBA = rgba;
        }
        public Color(Color c, float alpha)
        {
            this.RGBA = (c.RGBA & 0xFFFFFF00u);
            this.RGBA |= ((uint)(CMath.Clamp(alpha * 255, 0, 255)));
        }
        public Color(int r, int g, int b, int a = 255)
        {
            this.RGBA = EncodeRGBA(r, g, b, a);
        }
        public Color(float r, float g, float b, float a = 1f)
        {
            this.RGBA = EncodeRGBA(r, g, b, a);
        }
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            this.RGBA = EncodeRGBA(r, g, b, a);
        }
        public static implicit operator Color(in uint rgba)
        {
            return new Color(rgba);
        }
        public static implicit operator uint(in Color rgba)
        {
            return rgba.RGBA;
        }

        public static bool operator ==(in Color value1, in Color value2)
        {
            return value1.RGBA == value2.RGBA;
        }
        public static bool operator !=(in Color value1, in Color value2)
        {
            return value1.RGBA != value2.RGBA;
        }
        public override bool Equals(object obj)
        {
            if (obj is Color other) return this == other;
            return false;
        }
        public bool Equals(in Color other)
        {
            return this == other;
        }
        public bool Equals(Color other)
        {
            return this == other;
        }
        public override int GetHashCode()
        {
            return (int)((this.RGBA));
        }
        public override string ToString()
        {
            return $"[{RGBA.ToString("X8")}]";
        }
        public string ToHexRGB(bool prefix = false)
        {
            var text = RGBA.ToString("X8");
            text = text.Substring(0, 6);
            if (prefix)
                return $"0x{text}";
            else
                return text;
        }
        public string ToHexRGBA(bool prefix = false)
        {
            var text = RGBA.ToString("X8");
            if (prefix)
                return $"0x{text}";
            else
                return text;
        }
        public string ToHexARGB(bool prefix = false)
        {
            var argb = ToARGB(RGBA);
            var text = argb.ToString("X8");
            if (prefix)
                return $"0x{text}";
            else
                return text;
        }


        public static readonly Color COLOR_NULL = 0;
        public static readonly Color COLOR_WHITE = 0xffffffff;
        public static readonly Color COLOR_LIGHT_GRAY = 0xa0a0a0ff;
        public static readonly Color COLOR_GRAY = 0x808080ff;
        public static readonly Color COLOR_DARK_GRAY = 0x404040ff;
        public static readonly Color COLOR_BLACK = 0x000000ff;
        public static readonly Color COLOR_RED = 0xff0000ff;
        public static readonly Color COLOR_PINK = 0xffadadff;
        public static readonly Color COLOR_ORANGE = 0xffc700ff;
        public static readonly Color COLOR_YELLOW = 0xffff00ff;
        public static readonly Color COLOR_GREEN = 0x00ff00ff;
        public static readonly Color COLOR_MAGENTA = 0xff00ffff;
        public static readonly Color COLOR_CYAN = 0x00ffffff;
        public static readonly Color COLOR_BLUE = 0x0000ffff;


        static public Color FromRGBA(uint rgba)
        {
            return new Color(rgba);
        }
        static public Color FromARGB(uint argb)
        {
            DecodeARGB(argb, out byte r, out byte g, out byte b, out byte a);
            return new Color(r, g, b, a);
        }
        static public Color FromARGB(int argb)
        {
            unchecked
            {
                DecodeARGB((uint)argb, out byte r, out byte g, out byte b, out byte a);
                return new Color(r, g, b, a);
            }
        }

        static public byte[] FromRGBA8(uint rgba)
        {
            return new byte[] {
                (byte)((0xff000000 & rgba) >> 24),
                (byte)((0x00ff0000 & rgba) >> 16),
                (byte)((0x0000ff00 & rgba) >> 8),
                (byte)((0x000000ff & rgba) >> 0)
            };
        }
        static public byte[] FromARGB8(uint argb)
        {
            return new byte[] {
                (byte)((0x00ff0000 & argb) >> 16),
                (byte)((0x0000ff00 & argb) >> 8),
                (byte)((0x000000ff & argb) >> 0),
                (byte)((0xff000000 & argb) >> 24),
            };
        }

        //         static public uint ToARGB(uint rgba)
        //         {
        //             uint ret = rgba >> 8;
        //             ret = (ret | ((rgba & 0x00ff) << 24));
        //             return ret;
        //         }
        //         static public uint ToRGBA(uint argb)
        //         {
        //             uint ret = (argb & 0x00ffffff) << 8;
        //             ret = (ret | (argb >> 24));
        //             return ret;
        //         }

        static public uint ToARGB(uint rgba)
        {
            uint ret = rgba >> 8;
            ret = (ret | ((rgba & 0x00ff) << 24));
            return ret;
        }
        static public uint ToRGBA(uint argb)
        {
            uint ret = (argb & 0x00ffffff) << 8;
            ret = (ret | (argb >> 24));
            return ret;
        }

        static public uint ToRGBA(float r, float g, float b, float a)
        {
            uint ret = 0;
            ret |= ((uint)(r * 255)) << 24;
            ret |= ((uint)(g * 255)) << 16;
            ret |= ((uint)(b * 255)) << 8;
            ret |= ((uint)(a * 255));
            return ret;
        }

        static public uint ToRGBA(uint rgb, int a)
        {
            uint ret = 0;
            ret |= ((uint)(rgb & 0x00FFFFFF) << 8);
            ret |= ((uint)(a & 0xFF));
            return ret;
        }

        static public uint ToRGBA(int r, int g, int b, int a)
        {
            uint ret = 0;
            ret |= ((uint)(r)) << 24;
            ret |= ((uint)(g)) << 16;
            ret |= ((uint)(b)) << 8;
            ret |= ((uint)(a));
            return ret;
        }

        static public uint ToARGB(float r, float g, float b, float a)
        {
            uint ret = 0;
            ret |= ((uint)(r * 255)) << 16;
            ret |= ((uint)(g * 255)) << 8;
            ret |= ((uint)(b * 255)) << 0;
            ret |= ((uint)(a * 255)) << 24;
            return ret;
        }

        static public uint ToARGB(int r, int g, int b, int a)
        {
            uint ret = 0;
            ret |= ((uint)(r)) << 16;
            ret |= ((uint)(g)) << 8;
            ret |= ((uint)(b)) << 0;
            ret |= ((uint)(a)) << 24;
            return ret;
        }
        static public void ToRGBAF(uint rgba, out float r, out float g, out float b, out float a)
        {
            r = ((0xff000000 & rgba) >> 24) / 255f;
            g = ((0x00ff0000 & rgba) >> 16) / 255f;
            b = ((0x0000ff00 & rgba) >> 8) / 255f;
            a = ((0x000000ff & rgba)) / 255f;
        }
        static public void ToARGBF(uint argb, out float r, out float g, out float b, out float a)
        {
            a = ((0xff000000 & argb) >> 24) / 255f;
            r = ((0x00ff0000 & argb) >> 16) / 255f;
            g = ((0x0000ff00 & argb) >> 8) / 255f;
            b = ((0x000000ff & argb)) / 255f;
        }


        public static void DecodeRGBA(uint rgba, out float r, out float g, out float b, out float a)
        {
            Colors.DecodeRGBA(rgba, out r, out g, out b, out a);
        }
        public static void DecodeRGBA(uint rgba, out byte r, out byte g, out byte b, out byte a)
        {
            Colors.DecodeRGBA(rgba, out r, out g, out b, out a);
        }
        public static void DecodeARGB(uint argb, out float r, out float g, out float b, out float a)
        {
            Colors.DecodeARGB(argb, out r, out g, out b, out a);
        }
        public static void DecodeARGB(uint argb, out byte r, out byte g, out byte b, out byte a)
        {
            Colors.DecodeARGB(argb, out r, out g, out b, out a);
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


        #region System.Drawing.Color
        public static readonly Color AliceBlue = EncodeRGBA(240, 248, 255);
        public static readonly Color LightSalmon = EncodeRGBA(255, 160, 122);
        public static readonly Color AntiqueWhite = EncodeRGBA(250, 235, 215);
        public static readonly Color LightSeaGreen = EncodeRGBA(32, 178, 170);
        public static readonly Color Aqua = EncodeRGBA(0, 255, 255);
        public static readonly Color LightSkyBlue = EncodeRGBA(135, 206, 250);
        public static readonly Color Aquamarine = EncodeRGBA(127, 255, 212);
        public static readonly Color LightSlateGray = EncodeRGBA(119, 136, 153);
        public static readonly Color Azure = EncodeRGBA(240, 255, 255);
        public static readonly Color LightSteelBlue = EncodeRGBA(176, 196, 222);
        public static readonly Color Beige = EncodeRGBA(245, 245, 220);
        public static readonly Color LightYellow = EncodeRGBA(255, 255, 224);
        public static readonly Color Bisque = EncodeRGBA(255, 228, 196);
        public static readonly Color Lime = EncodeRGBA(0, 255, 0);
        public static readonly Color Black = EncodeRGBA(0, 0, 0);
        public static readonly Color LimeGreen = EncodeRGBA(50, 205, 50);
        public static readonly Color BlanchedAlmond = EncodeRGBA(255, 255, 205);
        public static readonly Color Linen = EncodeRGBA(250, 240, 230);
        public static readonly Color Blue = EncodeRGBA(0, 0, 255);
        public static readonly Color Magenta = EncodeRGBA(255, 0, 255);
        public static readonly Color BlueViolet = EncodeRGBA(138, 43, 226);
        public static readonly Color Maroon = EncodeRGBA(128, 0, 0);
        public static readonly Color Brown = EncodeRGBA(165, 42, 42);
        public static readonly Color MediumAquamarine = EncodeRGBA(102, 205, 170);
        public static readonly Color BurlyWood = EncodeRGBA(222, 184, 135);
        public static readonly Color MediumBlue = EncodeRGBA(0, 0, 205);
        public static readonly Color CadetBlue = EncodeRGBA(95, 158, 160);
        public static readonly Color MediumOrchid = EncodeRGBA(186, 85, 211);
        public static readonly Color Chartreuse = EncodeRGBA(127, 255, 0);
        public static readonly Color MediumPurple = EncodeRGBA(147, 112, 219);
        public static readonly Color Chocolate = EncodeRGBA(210, 105, 30);
        public static readonly Color MediumSeaGreen = EncodeRGBA(60, 179, 113);
        public static readonly Color Coral = EncodeRGBA(255, 127, 80);
        public static readonly Color MediumSlateBlue = EncodeRGBA(123, 104, 238);
        public static readonly Color CornflowerBlue = EncodeRGBA(100, 149, 237);
        public static readonly Color MediumSpringGreen = EncodeRGBA(0, 250, 154);
        public static readonly Color Cornsilk = EncodeRGBA(255, 248, 220);
        public static readonly Color MediumTurquoise = EncodeRGBA(72, 209, 204);
        public static readonly Color Crimson = EncodeRGBA(220, 20, 60);
        public static readonly Color MediumVioletRed = EncodeRGBA(199, 21, 112);
        public static readonly Color Cyan = EncodeRGBA(0, 255, 255);
        public static readonly Color MidnightBlue = EncodeRGBA(25, 25, 112);
        public static readonly Color DarkBlue = EncodeRGBA(0, 0, 139);
        public static readonly Color MintCream = EncodeRGBA(245, 255, 250);
        public static readonly Color DarkCyan = EncodeRGBA(0, 139, 139);
        public static readonly Color MistyRose = EncodeRGBA(255, 228, 225);
        public static readonly Color DarkGoldenrod = EncodeRGBA(184, 134, 11);
        public static readonly Color Moccasin = EncodeRGBA(255, 228, 181);
        public static readonly Color DarkGray = EncodeRGBA(169, 169, 169);
        public static readonly Color NavajoWhite = EncodeRGBA(255, 222, 173);
        public static readonly Color DarkGreen = EncodeRGBA(0, 100, 0);
        public static readonly Color Navy = EncodeRGBA(0, 0, 128);
        public static readonly Color DarkKhaki = EncodeRGBA(189, 183, 107);
        public static readonly Color OldLace = EncodeRGBA(253, 245, 230);
        public static readonly Color DarkMagena = EncodeRGBA(139, 0, 139);
        public static readonly Color Olive = EncodeRGBA(128, 128, 0);
        public static readonly Color DarkOliveGreen = EncodeRGBA(85, 107, 47);
        public static readonly Color OliveDrab = EncodeRGBA(107, 142, 45);
        public static readonly Color DarkOrange = EncodeRGBA(255, 140, 0);
        public static readonly Color Orange = EncodeRGBA(255, 165, 0);
        public static readonly Color DarkOrchid = EncodeRGBA(153, 50, 204);
        public static readonly Color OrangeRed = EncodeRGBA(255, 69, 0);
        public static readonly Color DarkRed = EncodeRGBA(139, 0, 0);
        public static readonly Color Orchid = EncodeRGBA(218, 112, 214);
        public static readonly Color DarkSalmon = EncodeRGBA(233, 150, 122);
        public static readonly Color PaleGoldenrod = EncodeRGBA(238, 232, 170);
        public static readonly Color DarkSeaGreen = EncodeRGBA(143, 188, 143);
        public static readonly Color PaleGreen = EncodeRGBA(152, 251, 152);
        public static readonly Color DarkSlateBlue = EncodeRGBA(72, 61, 139);
        public static readonly Color PaleTurquoise = EncodeRGBA(175, 238, 238);
        public static readonly Color DarkSlateGray = EncodeRGBA(40, 79, 79);
        public static readonly Color PaleVioletRed = EncodeRGBA(219, 112, 147);
        public static readonly Color DarkTurquoise = EncodeRGBA(0, 206, 209);
        public static readonly Color PapayaWhip = EncodeRGBA(255, 239, 213);
        public static readonly Color DarkViolet = EncodeRGBA(148, 0, 211);
        public static readonly Color PeachPuff = EncodeRGBA(255, 218, 155);
        public static readonly Color DeepPink = EncodeRGBA(255, 20, 147);
        public static readonly Color Peru = EncodeRGBA(205, 133, 63);
        public static readonly Color DeepSkyBlue = EncodeRGBA(0, 191, 255);
        public static readonly Color Pink = EncodeRGBA(255, 192, 203);
        public static readonly Color DimGray = EncodeRGBA(105, 105, 105);
        public static readonly Color Plum = EncodeRGBA(221, 160, 221);
        public static readonly Color DodgerBlue = EncodeRGBA(30, 144, 255);
        public static readonly Color PowderBlue = EncodeRGBA(176, 224, 230);
        public static readonly Color Firebrick = EncodeRGBA(178, 34, 34);
        public static readonly Color Purple = EncodeRGBA(128, 0, 128);
        public static readonly Color FloralWhite = EncodeRGBA(255, 250, 240);
        public static readonly Color Red = EncodeRGBA(255, 0, 0);
        public static readonly Color ForestGreen = EncodeRGBA(34, 139, 34);
        public static readonly Color RosyBrown = EncodeRGBA(188, 143, 143);
        public static readonly Color Fuschia = EncodeRGBA(255, 0, 255);
        public static readonly Color RoyalBlue = EncodeRGBA(65, 105, 225);
        public static readonly Color Gainsboro = EncodeRGBA(220, 220, 220);
        public static readonly Color SaddleBrown = EncodeRGBA(139, 69, 19);
        public static readonly Color GhostWhite = EncodeRGBA(248, 248, 255);
        public static readonly Color Salmon = EncodeRGBA(250, 128, 114);
        public static readonly Color Gold = EncodeRGBA(255, 215, 0);
        public static readonly Color SandyBrown = EncodeRGBA(244, 164, 96);
        public static readonly Color Goldenrod = EncodeRGBA(218, 165, 32);
        public static readonly Color SeaGreen = EncodeRGBA(46, 139, 87);
        public static readonly Color Gray = EncodeRGBA(128, 128, 128);
        public static readonly Color Seashell = EncodeRGBA(255, 245, 238);
        public static readonly Color Green = EncodeRGBA(0, 128, 0);
        public static readonly Color Sienna = EncodeRGBA(160, 82, 45);
        public static readonly Color GreenYellow = EncodeRGBA(173, 255, 47);
        public static readonly Color Silver = EncodeRGBA(192, 192, 192);
        public static readonly Color Honeydew = EncodeRGBA(240, 255, 240);
        public static readonly Color SkyBlue = EncodeRGBA(135, 206, 235);
        public static readonly Color HotPink = EncodeRGBA(255, 105, 180);
        public static readonly Color SlateBlue = EncodeRGBA(106, 90, 205);
        public static readonly Color IndianRed = EncodeRGBA(205, 92, 92);
        public static readonly Color SlateGray = EncodeRGBA(112, 128, 144);
        public static readonly Color Indigo = EncodeRGBA(75, 0, 130);
        public static readonly Color Snow = EncodeRGBA(255, 250, 250);
        public static readonly Color Ivory = EncodeRGBA(255, 240, 240);
        public static readonly Color SpringGreen = EncodeRGBA(0, 255, 127);
        public static readonly Color Khaki = EncodeRGBA(240, 230, 140);
        public static readonly Color SteelBlue = EncodeRGBA(70, 130, 180);
        public static readonly Color Lavender = EncodeRGBA(230, 230, 250);
        public static readonly Color Tan = EncodeRGBA(210, 180, 140);
        public static readonly Color LavenderBlush = EncodeRGBA(255, 240, 245);
        public static readonly Color Teal = EncodeRGBA(0, 128, 128);
        public static readonly Color LawnGreen = EncodeRGBA(124, 252, 0);
        public static readonly Color Thistle = EncodeRGBA(216, 191, 216);
        public static readonly Color LemonChiffon = EncodeRGBA(255, 250, 205);
        public static readonly Color Tomato = EncodeRGBA(253, 99, 71);
        public static readonly Color LightBlue = EncodeRGBA(173, 216, 230);
        public static readonly Color Turquoise = EncodeRGBA(64, 224, 208);
        public static readonly Color LightCoral = EncodeRGBA(240, 128, 128);
        public static readonly Color Violet = EncodeRGBA(238, 130, 238);
        public static readonly Color LightCyan = EncodeRGBA(224, 255, 255);
        public static readonly Color Wheat = EncodeRGBA(245, 222, 179);
        public static readonly Color LightGoldenrodYello = EncodeRGBA(250, 250, 210);
        public static readonly Color White = EncodeRGBA(255, 255, 255);
        public static readonly Color LightGreen = EncodeRGBA(144, 238, 144);
        public static readonly Color WhiteSmoke = EncodeRGBA(245, 245, 245);
        public static readonly Color LightGray = EncodeRGBA(211, 211, 211);
        public static readonly Color Yellow = EncodeRGBA(255, 255, 0);
        public static readonly Color LightPink = EncodeRGBA(255, 182, 193);
        public static readonly Color YellowGreen = EncodeRGBA(154, 205, 50);
        #endregion
    }
}
