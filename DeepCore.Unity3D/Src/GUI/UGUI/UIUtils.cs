using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Text;
using DeepCore.GUI.Gemo;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityImage = DeepCore.Unity3D.Impl.UnityImage;

namespace DeepCore.Unity3D.UGUI
{
    public static partial class UIUtils
    {
        public static string UnityRichTextToXmlText(string text)
        {
            return UGUIAttributedStringDecoder.UnityRichTextToXmlText(text);
        }

        public static Color UInt32_RGBA_To_Color(uint rgba)
        {
            Color c = new Color();
            GUI.Display.Color.toRGBAF(rgba, out c.r, out c.g, out c.b, out c.a);
            return c;
        }
        public static uint Color_To_UInt32_RGBA(Color c)
        {
            return GUI.Display.Color.toRGBA(c.r, c.g, c.b, c.a);
        }
        public static Color UInt32_ARGB_To_Color(uint argb)
        {
            Color c = new Color();
            GUI.Display.Color.toARGBF(argb, out c.r, out c.g, out c.b, out c.a);
            return c;
        }

        public static Color HexArgbToColor(string hex)
        {
            uint argb;
            UnityEngine.Color color = new UnityEngine.Color();
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
            {
                GUI.Display.Color.toARGBF(argb, out color.r, out color.g, out color.b, out color.a);
            }
            return color;
        }

        public static TextBorderCount ToTextShadowCount(Vector2 offset)
        {
            if (offset.x == -1 && offset.y == -1)
                return TextBorderCount.Shadow_L_T;
            if (offset.x == 0 && offset.y == -1)
                return TextBorderCount.Shadow_C_T;
            if (offset.x == 1 && offset.y == -1)
                return TextBorderCount.Shadow_R_T;

            if (offset.x == -1 && offset.y == 0)
                return TextBorderCount.Shadow_L_C;
            if (offset.x == 1 && offset.y == 0)
                return TextBorderCount.Shadow_R_C;

            if (offset.x == -1 && offset.y == 1)
                return TextBorderCount.Shadow_L_B;
            if (offset.x == 0 && offset.y == 1)
                return TextBorderCount.Shadow_C_B;
            if (offset.x == 1 && offset.y == 1)
                return TextBorderCount.Shadow_R_B;

            return TextBorderCount.Null;
        }

        public static Vector2 ToTextBorderOffset(TextBorderCount count)
        {
            switch (count)
            {
                case TextBorderCount.Border: return new Vector2(1, 1);
                case TextBorderCount.Shadow_L_T: return new Vector2(-1, -1);
                case TextBorderCount.Shadow_C_T: return new Vector2(0, -1);
                case TextBorderCount.Shadow_R_T: return new Vector2(1, -1);
                case TextBorderCount.Shadow_L_C: return new Vector2(-1, 0);
                case TextBorderCount.Shadow_C_C: return new Vector2(0, 0);
                case TextBorderCount.Shadow_R_C: return new Vector2(1, 0);
                case TextBorderCount.Shadow_L_B: return new Vector2(-1, 1);
                case TextBorderCount.Shadow_C_B: return new Vector2(0, 1);
                case TextBorderCount.Shadow_R_B: return new Vector2(1, 1);
            }
            return new Vector2(0, 0);
        }

        public static UnityEngine.TextAnchor ToUnityAnchor(GUI.Data.TextAnchor anchor)
        {
            switch (anchor)
            {
                case GUI.Data.TextAnchor.L_T: return UnityEngine.TextAnchor.UpperLeft;
                case GUI.Data.TextAnchor.C_T: return UnityEngine.TextAnchor.UpperCenter;
                case GUI.Data.TextAnchor.R_T: return UnityEngine.TextAnchor.UpperRight;
                case GUI.Data.TextAnchor.L_C: return UnityEngine.TextAnchor.MiddleLeft;
                case GUI.Data.TextAnchor.C_C: return UnityEngine.TextAnchor.MiddleCenter;
                case GUI.Data.TextAnchor.R_C: return UnityEngine.TextAnchor.MiddleRight;
                case GUI.Data.TextAnchor.L_B: return UnityEngine.TextAnchor.LowerLeft;
                case GUI.Data.TextAnchor.C_B: return UnityEngine.TextAnchor.LowerCenter;
                case GUI.Data.TextAnchor.R_B: return UnityEngine.TextAnchor.LowerRight;
            }
            return UnityEngine.TextAnchor.MiddleCenter;
        }

        public static GUI.Data.TextAnchor ToTextAnchor(RichTextAlignment a)
        {
            switch (a)
            {
                case RichTextAlignment.taCENTER:
                    return GUI.Data.TextAnchor.C_B;
                case RichTextAlignment.taLEFT:
                    return GUI.Data.TextAnchor.L_B;
                case RichTextAlignment.taRIGHT:
                    return GUI.Data.TextAnchor.R_B;
                default:
                    return GUI.Data.TextAnchor.L_B;
                }
        }
        public static RichTextAlignment ToRichTextAnchor(GUI.Data.TextAnchor a)
        {
            switch (a)
            {
                case GUI.Data.TextAnchor.L_T: return RichTextAlignment.taLEFT;
                case GUI.Data.TextAnchor.L_C: return RichTextAlignment.taLEFT;
                case GUI.Data.TextAnchor.L_B: return RichTextAlignment.taLEFT;
                case GUI.Data.TextAnchor.C_T: return RichTextAlignment.taCENTER;
                case GUI.Data.TextAnchor.C_C: return RichTextAlignment.taCENTER;
                case GUI.Data.TextAnchor.C_B: return RichTextAlignment.taCENTER;
                case GUI.Data.TextAnchor.R_T: return RichTextAlignment.taRIGHT;
                case GUI.Data.TextAnchor.R_C: return RichTextAlignment.taRIGHT;
                case GUI.Data.TextAnchor.R_B: return RichTextAlignment.taRIGHT;
            }
            return RichTextAlignment.taLEFT;
        }

        public static GUI.Display.FontStyle ToTextLayerFontStyle(GUI.Data.FontStyle fs, bool underline)
        {
            if (underline)
            {
                switch (fs)
                {
                    case GUI.Data.FontStyle.Bold: return GUI.Display.FontStyle.STYLE_BOLD_UNDERLINED;
                    case GUI.Data.FontStyle.BoldAndItalic: return GUI.Display.FontStyle.STYLE_BOLD_ITALIC_UNDERLINED;
                    case GUI.Data.FontStyle.Italic: return GUI.Display.FontStyle.STYLE_ITALIC_UNDERLINED;
                    case GUI.Data.FontStyle.Normal: return GUI.Display.FontStyle.STYLE_UNDERLINED;
                }
            }
            else
            {
                switch (fs)
                {
                    case GUI.Data.FontStyle.Bold: return GUI.Display.FontStyle.STYLE_BOLD;
                    case GUI.Data.FontStyle.BoldAndItalic: return GUI.Display.FontStyle.STYLE_BOLD_ITALIC;
                    case GUI.Data.FontStyle.Italic: return GUI.Display.FontStyle.STYLE_ITALIC;
                    case GUI.Data.FontStyle.Normal: return GUI.Display.FontStyle.STYLE_PLAIN;
                }
            }
            return GUI.Display.FontStyle.STYLE_PLAIN;
        }
        public static GUI.Data.FontStyle ToFontStyle(GUI.Display.FontStyle fs, out bool underline)
        {
            underline = false;
            switch (fs)
            {
                case GUI.Display.FontStyle.STYLE_BOLD:
                    return GUI.Data.FontStyle.Bold;
                case GUI.Display.FontStyle.STYLE_BOLD_ITALIC:
                    return GUI.Data.FontStyle.BoldAndItalic;
                case GUI.Display.FontStyle.STYLE_ITALIC:
                    return GUI.Data.FontStyle.Italic;
                case GUI.Display.FontStyle.STYLE_PLAIN:
                    return GUI.Data.FontStyle.Normal;

                case GUI.Display.FontStyle.STYLE_BOLD_UNDERLINED:
                    underline = true;
                    return GUI.Data.FontStyle.Bold;
                case GUI.Display.FontStyle.STYLE_BOLD_ITALIC_UNDERLINED:
                    underline = true;
                    return GUI.Data.FontStyle.BoldAndItalic;
                case GUI.Display.FontStyle.STYLE_ITALIC_UNDERLINED:
                    underline = true;
                    return GUI.Data.FontStyle.Italic;
                case GUI.Display.FontStyle.STYLE_UNDERLINED:
                    underline = true;
                    return GUI.Data.FontStyle.Normal;
            }
            return GUI.Data.FontStyle.Normal;
        }

        public static void AdjustAnchor(GUI.Data.TextAnchor anchor, Vector2 containerSize, ref Rect bounds)
        {
            float cw = containerSize.x;
            float ch = containerSize.y;
            switch (anchor)
            {
                case GUI.Data.TextAnchor.L_T:
                    bounds.x = 0;
                    bounds.y = 0;
                    break;
                case GUI.Data.TextAnchor.C_T:
                    bounds.x = (cw - bounds.width) / 2;
                    bounds.y = 0;
                    break;
                case GUI.Data.TextAnchor.R_T:
                    bounds.x = (cw - bounds.width);
                    bounds.y = 0;
                    break;
                case GUI.Data.TextAnchor.L_C:
                    bounds.x = 0;
                    bounds.y = (ch - bounds.height) / 2;
                    break;
                case GUI.Data.TextAnchor.C_C:
                    bounds.x = (cw - bounds.width) / 2;
                    bounds.y = (ch - bounds.height) / 2;
                    break;
                case GUI.Data.TextAnchor.R_C:
                    bounds.x = (cw - bounds.width);
                    bounds.y = (ch - bounds.height) / 2;
                    break;
                case GUI.Data.TextAnchor.L_B:
                    bounds.x = 0;
                    bounds.y = (ch - bounds.height);
                    break;
                case GUI.Data.TextAnchor.C_B:
                    bounds.x = (cw - bounds.width) / 2;
                    bounds.y = (ch - bounds.height);
                    break;
                case GUI.Data.TextAnchor.R_B:
                    bounds.x = (cw - bounds.width);
                    bounds.y = (ch - bounds.height);
                    break;
            }
        }
        public static void AdjustAnchor(ImageAnchor anchor, Vector2 containerSize, ref Rect bounds)
        {
            AdjustAnchor((GUI.Data.TextAnchor)anchor, containerSize, ref bounds);
        }

        public static void AdjustAnchor(GUI.Data.TextAnchor anchor, DisplayNode container, DisplayNode child, Vector2 offset)
        {
            Rect bounds = child.Bounds2D;
            UIUtils.AdjustAnchor(anchor, container.Size2D, ref bounds);
            bounds.position += offset;
            child.Bounds2D = bounds;
        }
        public static void AdjustAnchor(ImageAnchor anchor, DisplayNode container, DisplayNode child, Vector2 offset)
        {
            AdjustAnchor((GUI.Data.TextAnchor)anchor, container, child, offset);
        }

        public static void AdjustGaugeOrientation(GaugeOrientation orientation, DisplayNode container, DisplayNode child, float percent)
        {
            float rate = (percent / 100f);
            Vector2 containerSize = container.Size2D;
            Rect bounds = child.Bounds2D;
            switch (orientation)
            {
                case GaugeOrientation.LEFT_2_RIGHT:
                    bounds.width = containerSize.x * rate;
                    bounds.height = containerSize.y;
                    bounds.x = 0;
                    bounds.y = 0;
                    break;
                case GaugeOrientation.RIGTH_2_LEFT:
                    bounds.width = containerSize.x * rate;
                    bounds.height = containerSize.y;
                    bounds.x = containerSize.x - bounds.width;
                    bounds.y = 0;
                    break;
                case GaugeOrientation.TOP_2_BOTTOM:
                    bounds.width = containerSize.x;
                    bounds.height = containerSize.y * rate;
                    bounds.x = 0;
                    bounds.y = 0;
                    break;
                case GaugeOrientation.BOTTOM_2_TOP:
                    bounds.width = containerSize.x;
                    bounds.height = containerSize.y * rate;
                    bounds.x = 0;
                    bounds.y = containerSize.y - bounds.height;
                    break;
            }
            child.Bounds2D = bounds;
        }


        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="src"></param>
        /// <param name="clip"></param>
        /// <param name="pivot"></param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="border"></param>
        /// <returns></returns>
        public static Sprite CreateSprite(
            UnityImage src,
            Rect clip,
            Vector2 pivot,
            float pixelsPerUnit,
            uint extrude,
            SpriteMeshType meshType,
            Vector4 border)
        {
            var sprite = Sprite.Create(src.Texture as Texture2D,
                new Rect(clip.x, src.Texture.height - clip.y, clip.width, clip.height),
                pivot, pixelsPerUnit, extrude, meshType, border);
            return sprite;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="src"></param>
        /// <param name="clip"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Sprite CreateSprite(UnityImage src, Rect clip, Vector2 pivot)
        {
            var sprite = Sprite.Create(src.Texture as Texture2D, new Rect(clip.x, src.Texture.height - clip.y - clip.height, clip.width, clip.height), pivot, 100f, 0, SpriteMeshType.FullRect);
            return sprite;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="src"></param>
        /// <param name="clip"></param>
        /// <param name="pivot"></param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="border"></param>
        /// <returns></returns>
        public static Sprite CreateSprite(
            UnityImage src,
            Rectangle2D clip,
            Vector2 pivot,
            float pixelsPerUnit,
            uint extrude,
            SpriteMeshType meshType,
            Vector4 border)
        {
            var sprite = Sprite.Create(src.Texture as Texture2D,
                new Rect(clip.x, src.Texture.height - clip.y, clip.width, clip.height),
                pivot, pixelsPerUnit, extrude, meshType, border);
            return sprite;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="src"></param>
        /// <param name="clip"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Sprite CreateSprite(UnityImage src, Rectangle2D clip, Vector2 pivot)
        {
            var sprite = Sprite.Create(src.Texture as Texture2D, new Rect(clip.x, src.Texture.height - clip.y - clip.height, clip.width, clip.height), pivot);
            return sprite;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="src"></param>
        /// <param name="color">color</param>
        /// <param name="sx">图片源X</param>
        /// <param name="sy">图片源Y</param>
        /// <param name="dx">目标X</param>
        /// <param name="dy">目标Y</param>
        /// <returns></returns>
        public static UIVertex CreateVertex(UnityImage src, Color color, float sx, float sy, float dx, float dy)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(dx, -dy);
            vertex.uv0 = new Vector2(sx / src.Texture.width, 1f - sy / src.Texture.height);
            vertex.color = color;
            return vertex;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系
        /// </summary>
        /// <param name="color"></param>
        /// <param name="dx">目标X</param>
        /// <param name="dy">目标Y</param>
        /// <returns></returns>
        public static UIVertex CreateVertexColor(UnityEngine.Color color, float dx, float dy)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(dx, -dy);
            vertex.color = color;
            return vertex;
        }


        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系(一次创建4个顶点)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="color"></param>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static UIVertex[] CreateVertexQuard(UnityImage src, Color color, float sx, float sy, float dx, float dy, float w, float h)
        {
            UIVertex[] quard = new UIVertex[4];
            quard[0] = CreateVertex(src, color, sx, sy, dx, dy);
            quard[1] = CreateVertex(src, color, sx + w, sy, dx + w, dy);
            quard[2] = CreateVertex(src, color, sx + w, sy + h, dx + w, dy + h);
            quard[3] = CreateVertex(src, color, sx, sy + h, dx, dy + h);
            return quard;
        }

        /// <summary>
        /// 2DUI坐标系，转换到Unity坐标系(一次创建4个顶点)
        /// </summary>
        /// <param name="color"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static UIVertex[] CreateVertexQuardColor(UnityEngine.Color color, float dx, float dy, float w, float h)
        {
            UIVertex[] quard = new UIVertex[4];
            quard[0] = CreateVertexColor(color, dx, dy);
            quard[1] = CreateVertexColor(color, dx + w, dy);
            quard[2] = CreateVertexColor(color, dx + w, dy + h);
            quard[3] = CreateVertexColor(color, dx, dy + h);
            return quard;
        }
        

        public static void CreateVertexQuard(UnityImage src, Color color, float sx, float sy, float dx, float dy, float w, float h, List<UIVertex> vbo)
        {
            vbo.Add(CreateVertex(src, color, sx, sy, dx, dy));
            vbo.Add(CreateVertex(src, color, sx + w, sy, dx + w, dy));
            vbo.Add(CreateVertex(src, color, sx + w, sy + h, dx + w, dy + h));
            vbo.Add(CreateVertex(src, color, sx, sy + h, dx, dy + h));
        }
        public static void CreateVertexQuardColor(UnityEngine.Color color, float dx, float dy, float w, float h, List<UIVertex> vbo)
        {
            vbo.Add(CreateVertexColor(color, dx, dy));
            vbo.Add(CreateVertexColor(color, dx + w, dy));
            vbo.Add(CreateVertexColor(color, dx + w, dy + h));
            vbo.Add(CreateVertexColor(color, dx, dy + h));
        }


        public static void CreateVertexQuard(UnityImage src, Color color, float sx, float sy, float dx, float dy, float w, float h, VertexHelper vbo)
        {
            int vcount = vbo.currentVertCount;
            vbo.AddVert(CreateVertex(src, color, sx, sy, dx, dy));
            vbo.AddVert(CreateVertex(src, color, sx + w, sy, dx + w, dy));
            vbo.AddVert(CreateVertex(src, color, sx + w, sy + h, dx + w, dy + h));
            vbo.AddVert(CreateVertex(src, color, sx, sy + h, dx, dy + h));
            vbo.AddTriangle(vcount, vcount + 1, vcount + 2);
            vbo.AddTriangle(vcount + 2, vcount + 3, vcount);
        }
        public static void CreateVertexQuardColor(UnityEngine.Color color, float dx, float dy, float w, float h, VertexHelper vbo)
        {
            int vcount = vbo.currentVertCount;
            vbo.AddVert(CreateVertexColor(color, dx, dy));
            vbo.AddVert(CreateVertexColor(color, dx + w, dy));
            vbo.AddVert(CreateVertexColor(color, dx + w, dy + h));
            vbo.AddVert(CreateVertexColor(color, dx, dy + h));
            vbo.AddTriangle(vcount, vcount + 1, vcount + 2);
            vbo.AddTriangle(vcount + 2, vcount + 3, vcount);
        }
        

        //-----------------------------------------------------------------------------------------------------------

    }
}
