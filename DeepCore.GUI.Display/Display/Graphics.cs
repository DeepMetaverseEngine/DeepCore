using DeepCore.Geometry;
using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display
{


    public enum DashStyle
    {
        /// <summary>
        /// 指定用户定义的自定义划线段样式。
        /// </summary>
        Custom = 5,
        /// <summary>
        /// 指定由划线段组成的直线。
        /// </summary>
        Dash = 1,
        /// <summary>
        /// 指定由重复的划线点图案构成的直线。
        /// </summary>
        DashDot = 3,
        /// <summary>
        /// 指定由重复的划线点点图案构成的直线。
        /// </summary>
        DashDotDot = 4,
        /// <summary>
        /// 指定由点构成的直线。
        /// </summary>
        Dot = 2,
        /// <summary>
        /// 指定实线。
        /// </summary>
        Solid = 0,
    }


    //	------------------------------------------------------------------------------------------
    //	-by zhangyifei
    //	------------------------------------------------------------------------------------------

    public abstract class Graphics : IDisposable
    {

        //----------------------------------------------------------------------------------------------------
        public abstract void Dispose();

        //----------------------------------------------------------------------------------------------------
        #region Pen

        protected abstract void InternalSetPen(float size, DashStyle style);

        private float cur_PenSize = 1f;
        private DashStyle cur_PenStyle = DashStyle.Solid;
        private Stack<ValueTuple<float, DashStyle>> stack_pen = new Stack<ValueTuple<float, DashStyle>>();
        public float CurrentPenSize => cur_PenSize;
        public DashStyle CurrentPenStyle => cur_PenStyle;
        public void SetPenSize(float size)
        {
            cur_PenSize = size;
            InternalSetPen(cur_PenSize, cur_PenStyle);
        }
        public void SetPenStyle(DashStyle style)
        {
            cur_PenStyle = style;
            InternalSetPen(cur_PenSize, cur_PenStyle);
        }
        public virtual void PushPen()
        {
            stack_pen.Push((cur_PenSize, cur_PenStyle));
        }
        public virtual void PopPen()
        {
            var t = stack_pen.Pop();
            SetPenSize(t.Item1);
            SetPenStyle(t.Item2);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Font
        protected abstract void InternalSetFont(float size, TextFontStyle style);

        private float cur_FontSize = 9f;
        private TextFontStyle cur_FontStyle = TextFontStyle.Plain;
        private Stack<ValueTuple<float, TextFontStyle>> stack_font = new Stack<ValueTuple<float, TextFontStyle>>();
        public float CurrentFontSize => cur_FontSize;
        public TextFontStyle CurrentFontStyle => cur_FontStyle;
        public void SetFontSize(float size)
        {
            cur_FontSize = size;
            InternalSetFont(cur_FontSize, cur_FontStyle);
        }
        public void SetFontStyle(TextFontStyle style)
        {
            cur_FontStyle = style;
            InternalSetFont(cur_FontSize, cur_FontStyle);
        }
        public virtual void PushFont()
        {
            stack_font.Push((cur_FontSize, cur_FontStyle));
        }
        public virtual void PopFont()
        {
            var t = stack_font.Pop();
            SetFontSize(t.Item1);
            SetFontStyle(t.Item2);
        }
        public abstract Vector2 MeasureString(string text);
        public abstract Vector2 MeasureString(string text, int width);
        public abstract Vector2 MeasureString(string text, RectangleF width);

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Blend
        protected abstract void InternalSetBlend(Blend blen);
        private Blend cur_Blend = Blend.BLEND_MODE_NORMAL;
        private Stack<Blend> stack_blend = new Stack<Blend>();
        public Blend CurrentBlend => cur_Blend;
        public void SetBlend(Blend blend)
        {
            cur_Blend = blend;
            InternalSetBlend(cur_Blend);
        }
        public virtual void PushBlend()
        {
            stack_blend.Push(cur_Blend);
        }
        public virtual void PopBlend()
        {
            SetBlend(stack_blend.Pop());
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Color
        protected abstract void InternalSetColor(in Color color, float alpha);

        private Color cur_Color = Color.Green;
        private float cur_Alpha = 1f;
        private Stack<ValueTuple<Color, float>> stack_color = new Stack<ValueTuple<Color, float>>();
        public Color CurrentColor => cur_Color;
        public float CurrentAlpha => cur_Alpha;
        public void SetColor(in Color color)
        {
            cur_Color = color;
            InternalSetColor(cur_Color, cur_Alpha);
        }
        public void SetColor(in Color color, float alpha)
        {
            cur_Alpha = alpha;
            cur_Color = color;
            InternalSetColor(in cur_Color, cur_Alpha);
        }
        public void SetAlpha(float alpha)
        {
            cur_Alpha = alpha;
            InternalSetColor(in cur_Color, cur_Alpha);
        }
        public void AddAlpha(float add)
        {
            SetAlpha(cur_Alpha * add);
        }
        public virtual void PushColor()
        {
            stack_color.Push((cur_Color, cur_Alpha));
        }
        public virtual void PopColor()
        {
            var e = stack_color.Pop();
            SetColor(e.Item1, e.Item2);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Clip
        protected abstract void InternalSetClip(in RectangleF rect);

        private RectangleF cur_Clip = RectangleF.Empty;
        private Stack<RectangleF> stack_clips = new Stack<RectangleF>();
        public RectangleF CurrentClip => cur_Clip;
        public void SetClip(in RectangleF rect)
        {
            cur_Clip = rect;
            InternalSetClip(in cur_Clip);
        }
        public virtual void PushClip()
        {
            stack_clips.Push(cur_Clip);
        }
        public virtual void PopClip()
        {
            SetClip(stack_clips.Pop());
        }
        public void SetClip(float x, float y, float w, float h)
        {
            SetClip(new RectangleF(x, y, w, h));
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Transform
        public abstract void Translate(in Vector2 pos);
        public abstract void Scale(in Vector2 scale);
        public abstract void Rotate(float angle);
        public abstract void MultiplyTransform(in Matrix tras);
        public abstract void PushTransform();
        public abstract void PopTransform();
        public /******/ void Scale(float sx, float sy) { Scale(new Vector2(sx, sy)); }
        public /******/ void Translate(float x, float y) { Translate(new Vector2(x, y)); }

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Image
        public abstract void BeginImage(Image image, RectangleF? srcRect = null);


        public abstract void DrawVertex(VertexBuffer vertex);
        public abstract void DrawVertex(VertexBuffer vertex, int[] indices, VertexTopology mode);
        public abstract void DrawVertexSequence(VertexBuffer vertex, VertexTopology mode);


        public abstract void DrawImageZoom(in RectangleF zoom);
        public /******/ void DrawImageZoom(float x, float y, float w, float h) { DrawImageZoom(new RectangleF(x, y, w, h)); }


        public abstract void DrawImage(in Vector2 pos);
        public /******/ void DrawImage(float x, float y) { DrawImage(new Vector2(x, y)); }

        public abstract void DrawImage(in Vector2 pos, AlignmentStyle anchor);
        public /******/ void DrawImage(float x, float y, AlignmentStyle anchor) { DrawImage(new Vector2(x, y), anchor); }


        public abstract void DrawImageTrans(in Vector2 pos, Trans trans);
        public /******/ void DrawImageTrans(float x, float y, Trans trans) { DrawImageTrans(new Vector2(x, y), trans); }


        public abstract void DrawImageRegion(in RectangleF srcRect, in Vector2 pos);
        public abstract void DrawImageRegion(in RectangleF srcRect, in RectangleF dstRect);
        public abstract void DrawImageRegion(in RectangleF srcRect, Trans tx, in Vector2 pos);
        public abstract void DrawImageRegion(in RectangleF srcRect, Trans tx, in RectangleF dstRect);
        public /******/ void DrawImageRegion(float sx, float sy, float sw, float sh, Trans tx, float dx, float dy, float dw, float dh) { DrawImageRegion(new RectangleF(sx, sy, sw, sh), tx, new RectangleF(dx, dy, dw, dh)); }
        public /******/ void DrawImageRegion(float sx, float sy, float w, float h, Trans tx, float dx, float dy) { DrawImageRegion(new RectangleF(sx, sy, w, h), tx, new Vector2(dx, dy)); }
        public /******/ void DrawImageRegion(float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh) { DrawImageRegion(new RectangleF(sx, sy, sw, sh), new RectangleF(dx, dy, dw, dh)); }
        public /******/ void DrawImageRegion(float sx, float sy, float w, float h, float dx, float dy) { DrawImageRegion(new RectangleF(sx, sy, w, h), new Vector2(dx, dy)); }


        public abstract void DrawImageEllipse(in RectangleF rect, float startAngle, float endAngle);
        public /******/ void DrawImageEllipse(float sx, float sy, float sw, float sh, float startAngle, float endAngle) { DrawImageEllipse(new RectangleF(sx, sy, sw, sh), startAngle, endAngle); }

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Text




        public abstract void DrawString(string text, in Vector2 pos, AlignmentStyle anchor);
        public /******/ void DrawString(string text, in Vector2 pos) { DrawString(text, in pos, AlignmentStyle.None); }

        public abstract void DrawString(string text, in RectangleF region, AlignmentStyle anchor);
        public /******/ void DrawString(string text, in RectangleF region) { DrawString(text, in region, AlignmentStyle.None); }

        public abstract void DrawStringBorder(string text, TextBorderStyle bounds, in Color bodyBrush, in Color borderBrush, in RectangleF rect, AlignmentStyle alignment);

        public abstract void DrawTextLayer(TextLayer text, in RectangleF rect, AlignmentStyle alignment);

        #endregion
        //----------------------------------------------------------------------------------------------------
        #region Shape
        //----------------------------------------------------------------------------------------------------
        public abstract void DrawLine(in Line2 line);
        public /******/ void DrawLine(in Vector2 src, in Vector2 dst) { DrawLine(new Line2(src, dst)); }
        public /******/ void DrawLine(float x1, float y1, float x2, float y2) { DrawLine(new Line2(new Vector2(x1, y1), new Vector2(x2, y2))); }

        //----------------------------------------------------------------------------------------------------
        public abstract void DrawRect(in RectangleF rect);
        public /******/ void DrawRect(float x, float y, float w, float h) { DrawRect(new RectangleF(x, y, w, h)); }
        public abstract void FillRect(in RectangleF rect);
        public /******/ void FillRect(float x, float y, float w, float h) { FillRect(new RectangleF(x, y, w, h)); }

        //----------------------------------------------------------------------------------------------------
        public abstract void DrawRoundRect(in RectangleF rect, float rx, float ry);
        public /******/ void DrawRoundRect(float x, float y, float w, float h, float rx, float ry) { DrawRoundRect(new RectangleF(x, y, w, h), rx, ry); }
        public abstract void FillRoundRect(in RectangleF rect, float rx, float ry);
        public /******/ void FillRoundRect(float x, float y, float w, float h, float rx, float ry) { FillRoundRect(new RectangleF(x, y, w, h), rx, ry); }

        //----------------------------------------------------------------------------------------------------
        /// <summary> 0~360</summary>
        public abstract void DrawArc(in RectangleF rect, float startAngle, float arcAngle);
        /// <summary> 0~360</summary>
        public /******/ void DrawArc(float x, float y, float w, float h, float startAngle, float arcAngle) { DrawArc(new RectangleF(x, y, w, h), startAngle, arcAngle); }
        /// <summary> 0~360</summary>
        public abstract void FillArc(in RectangleF rect, float startAngle, float arcAngle);
        /// <summary> 0~360</summary>
        public /******/ void FillArc(float x, float y, float w, float h, float startAngle, float arcAngle) { FillArc(new RectangleF(x, y, w, h), startAngle, arcAngle); }
        public abstract void FillRectEllipse(in RectangleF rect, float startAngle, float endAngle);
        //----------------------------------------------------------------------------------------------------
        public abstract void FillRect4Color(in RectangleF rect, uint[] rgba);
        public /******/ void FillRect4Color(float x, float y, float w, float h, uint[] rgba) { FillRect4Color(new RectangleF(x, y, w, h), rgba); }


        //----------------------------------------------------------------------------------------------------
        public abstract void DrawPolygon(Vector2[] points);
        public abstract void FillPolygon(Vector2[] points);





        #endregion
        //----------------------------------------------------------------------------------------------------




    }

}
