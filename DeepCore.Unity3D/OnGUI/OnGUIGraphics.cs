using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D.Impl.OnGUI
{
    public class OnGUIGraphics : GUI.Display.Graphics
    {
        private const float ANGLE_90 = (float)(90);
        private const float ANGLE_180 = (float)(180);
        private const float ANGLE_270 = (float)(270);
        public static UnityEngine.Font DefaultFont = new UnityEngine.Font("Arial");
        //-----------------------------------------------------------------------
        public OnGUIGraphics()
        {
            cur_FontSize = UnityEngine.GUI.skin.label.fontSize;
            PushColor();
        }
        public override void Dispose()
        {
            PopColor();
        }
        //-----------------------------------------------------------------------
        #region Transform
        private Stack<UnityEngine.Matrix4x4> stack_trans = new Stack<UnityEngine.Matrix4x4>();
        private void transAnchor(AlignmentStyle anchor, float w, float h, out float tx, out float ty)
        {
            tx = 0;
            ty = 0;
            if ((anchor & AlignmentStyle.MASK_CENTER) != 0)
            {
                tx = -w / 2;
            }
            else if ((anchor & AlignmentStyle.MASK_RIGHT) != 0)
            {
                tx = -w;
            }

            if ((anchor & AlignmentStyle.MASK_MIDDLE) != 0)
            {
                ty = -h / 2;
            }
            else if ((anchor & AlignmentStyle.MASK_BOTTOM) != 0)
            {
                ty = -h;
            }
        }
        private void transAnchor(AlignmentStyle anchor, float sw, float sh, float dw, float dh, out float dx, out float dy)
        {
            dx = 0;
            dy = 0;
            switch (anchor)
            {
                case AlignmentStyle.TopLeft:
                    break;
                case AlignmentStyle.TopCenter:
                    dx = (dw - sw) / 2;
                    break;
                case AlignmentStyle.TopRight:
                    dx = (dw - sw);
                    break;
                case AlignmentStyle.MiddleLeft:
                    dy = (dh - sh) / 2;
                    break;
                case AlignmentStyle.MiddleCenter:
                    dx = (dw - sw) / 2;
                    dy = (dh - sh) / 2;
                    break;
                case AlignmentStyle.MiddleRight:
                    dx = (dw - sw);
                    dy = (dh - sh) / 2;
                    break;
                case AlignmentStyle.BottomLeft:
                    dy = (dh - sh);
                    break;
                case AlignmentStyle.BottomCenter:
                    dx = (dw - sw) / 2;
                    dy = (dh - sh);
                    break;
                case AlignmentStyle.BottomRight:
                    dx = (dw - sw);
                    dy = (dh - sh);
                    break;
            }
        }
        private void transTans(Trans trans, float w, float h, out float tw, out float th)
        {
            var mtx = UnityEngine.Matrix4x4.identity;
            switch (trans)
            {
                case Trans.TRANS_ROT90:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(h, 0, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_90),
                        UnityEngine.Vector3.one);
                    break;
                case Trans.TRANS_ROT180:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(w, h, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_180),
                        UnityEngine.Vector3.one);
                    break;
                case Trans.TRANS_ROT270:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(0, w, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_270),
                        UnityEngine.Vector3.one);
                    break;

                case Trans.TRANS_MIRROR:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(w, 0, 0),
                        UnityEngine.Quaternion.identity,
                        new UnityEngine.Vector3(-1, 1, 1));
                    break;

                case Trans.TRANS_MIRROR_ROT90:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(h, 0, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_90),
                        UnityEngine.Vector3.one);
                    mtx *= UnityEngine.Matrix4x4.TRS(
                       new UnityEngine.Vector3(w, 0, 0),
                       UnityEngine.Quaternion.identity,
                       new UnityEngine.Vector3(-1, 1, 1));
                    break;

                case Trans.TRANS_MIRROR_ROT180:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(w, h, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_180),
                        UnityEngine.Vector3.one);
                    mtx *= UnityEngine.Matrix4x4.TRS(
                       new UnityEngine.Vector3(w, 0, 0),
                       UnityEngine.Quaternion.identity,
                       new UnityEngine.Vector3(-1, 1, 1));
                    break;

                case Trans.TRANS_MIRROR_ROT270:
                    mtx *= UnityEngine.Matrix4x4.TRS(
                        new UnityEngine.Vector3(w, 0, 0),
                        UnityEngine.Quaternion.Euler(0, 0, ANGLE_270),
                        UnityEngine.Vector3.one);
                    mtx *= UnityEngine.Matrix4x4.TRS(
                       new UnityEngine.Vector3(w, 0, 0),
                       UnityEngine.Quaternion.identity,
                       new UnityEngine.Vector3(-1, 1, 1));
                    break;
            }
            tw = w;
            th = h;
            switch (trans)
            {
                case Trans.TRANS_ROT90:
                case Trans.TRANS_ROT270:
                case Trans.TRANS_MIRROR_ROT90:
                case Trans.TRANS_MIRROR_ROT270:
                    tw = h;
                    th = w;
                    break;
            }
            UnityEngine.GUI.matrix *= mtx;
        }
        public override void MultiplyTransform(in Matrix m)
        {
            var wt = new UnityEngine.Matrix4x4()
            {
                m00 = m.M11,
                m01 = m.M12,
                m10 = m.M21,
                m11 = m.M22,
                m30 = m.M41,
                m31 = m.M42,
            };
            UnityEngine.GUI.matrix *= wt;
        }
        public override void Translate(in Geometry.Vector2 pos)
        {
            var wt = UnityEngine.Matrix4x4.Translate(pos.ToUnity());
            UnityEngine.GUI.matrix *= wt;
        }
        public override void Rotate(float angle)
        {
            var wt = UnityEngine.Matrix4x4.Rotate(UnityEngine.Quaternion.Euler(0, 0, angle));
            UnityEngine.GUI.matrix *= wt;
        }
        public override void Scale(in Geometry.Vector2 scale)
        {
            var wt = UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(scale.X, scale.Y, 1));
            UnityEngine.GUI.matrix *= wt;
        }
        public override void PushTransform()
        {
            stack_trans.Push(UnityEngine.GUI.matrix);
        }
        public override void PopTransform()
        {
            if (stack_trans.Count > 0)
            {
                var mtx = stack_trans.Pop();
                UnityEngine.GUI.matrix = mtx;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------
        #region Status
        //-----------------------------------------------------------------------
        private Stack<(UnityEngine.Color, UnityEngine.Color, UnityEngine.Color)> stack_color = new();
        public override void PushColor()
        {
            base.PushColor();
            stack_color.Push((UnityEngine.GUI.color, UnityEngine.GUI.contentColor, UnityEngine.GUI.backgroundColor));
        }
        public override void PopColor()
        {
            base.PopColor();
            if (stack_color.TryPop(out var tuple))
            {
                UnityEngine.GUI.color = tuple.Item1;
                UnityEngine.GUI.contentColor = tuple.Item2;
                UnityEngine.GUI.backgroundColor = tuple.Item3;
            }
        }
        //-----------------------------------------------------------------------
        public override void PushBlend()
        {
            base.PushBlend();
        }
        public override void PopBlend()
        {
            base.PopBlend();
        }
        //-----------------------------------------------------------------------
        public override void PushClip()
        {
            base.PushClip();
        }
        public override void PopClip()
        {
            base.PopClip();
        }
        //-----------------------------------------------------------------------
        public override void PushFont()
        {
            base.PushFont();
        }
        public override void PopFont()
        {
            base.PopFont();
        }
        //-----------------------------------------------------------------------
        public override void PushPen()
        {
            base.PushPen();
        }
        public override void PopPen()
        {
            base.PopPen();
        }
        //-----------------------------------------------------------------------

        private UnityEngine.Color cur_brush = UnityEngine.Color.white;
        private float cur_FontSize = 12;
        private GUIStyle cur_Style = new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = UnityEngine.Color.white }
        };
        protected override void InternalSetBlend(Blend blen)
        {

        }
        protected override void InternalSetPen(float size, DashStyle style)
        {
        }
        protected override void InternalSetClip(in RectangleF rect)
        {
            UnityEngine.GUI.BeginClip(new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }
        protected override void InternalSetColor(in GUI.Display.Color color, float alpha)
        {
            cur_brush = new UnityEngine.Color(color.R, color.G, color.B, color.A);
            //Display.Color.DecodeRGBA(color, out byte r, out var g, out var b, out var a);
            //             cur_brush.Color = System.Drawing.Color.FromArgb(
            //                 (byte)Math.Min(color.A8 * alpha, 255),
            //                 color.R8,
            //                 color.G8,
            //                 color.B8);
            //             cur_pen.Color = cur_brush.Color;
        }
        protected override void InternalSetFont(float size, TextFontStyle style)
        {
            cur_FontSize = size;
        }
        public override Geometry.Vector2 MeasureString(string text)
        {
            return Geometry.Vector2.Zero;
        }
        public override Geometry.Vector2 MeasureString(string text, int width)
        {
            return Geometry.Vector2.Zero;
        }
        public override Geometry.Vector2 MeasureString(string text, RectangleF width)
        {
            return width.Size;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------
        #region DrawString
        public override void DrawString(string text, in Geometry.Vector2 pos, AlignmentStyle anchor)
        {
            cur_Style.fontSize = (int)Mathf.Ceil(cur_FontSize);
            cur_Style.alignment = TextAnchor.UpperLeft;
            cur_Style.wordWrap = false;
            cur_Style.normal.textColor = cur_brush;
            UnityEngine.GUI.contentColor = cur_brush;
            UnityEngine.GUI.Label(new Rect(pos.ToUnity(), new UnityEngine.Vector2(0, 0)), text, cur_Style);
        }
        public override void DrawString(string text, in RectangleF region, AlignmentStyle anchor)
        {
            cur_Style.fontSize = (int)Mathf.Ceil(cur_FontSize);
            cur_Style.alignment = anchor.ToTextAnchor();
            cur_Style.wordWrap = true;
            cur_Style.normal.textColor = cur_brush;
            UnityEngine.GUI.contentColor = cur_brush;
            UnityEngine.GUI.Box(region.ToUnity(), text, cur_Style);
        }
        public override void DrawTextLayer(TextLayer text, in RectangleF rect, AlignmentStyle alignment)
        {
            cur_Style.alignment = alignment.ToTextAnchor();
            cur_Style.wordWrap = true;
            cur_Style.normal.textColor = text.FontColor.ToUnityColor();
            UnityEngine.GUI.contentColor = text.FontColor.ToUnityColor();
            UnityEngine.GUI.Box(rect.ToUnity(), text.Text, cur_Style);
        }
        public override void DrawStringBorder(string text, TextBorderStyle bt, in GUI.Display.Color bodyBrush, in GUI.Display.Color bbrush, in RectangleF pos, AlignmentStyle alignment)
        {
            cur_Style.fontSize = (int)Mathf.Ceil(cur_FontSize);
            cur_Style.alignment = alignment.ToTextAnchor();
            cur_Style.wordWrap = true;
            cur_Style.normal.textColor = bodyBrush.ToUnityColor();
            UnityEngine.GUI.contentColor = bodyBrush.ToUnityColor();
            UnityEngine.GUI.Box(pos.ToUnity(), text, cur_Style);
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------
        #region DrawImage
        private UnityImage cur_img;
        public override void BeginImage(Image image, RectangleF? srcRect)
        {
            this.cur_img = (UnityImage)image;
        }
        public override void DrawVertex(VertexBuffer vertex)
        {
        }
        public override void DrawVertex(VertexBuffer vertex, int[] indices, VertexTopology mode)
        {
        }
        public override void DrawVertexSequence(VertexBuffer vertex, VertexTopology mode)
        {
        }
        public override void DrawImageEllipse(in RectangleF rect, float startAngle, float endAngle)
        {
            var bounds = rect.ToUnity();
            UnityEngine.GUI.DrawTexture(bounds, cur_img.Texture, ScaleMode.StretchToFill);
        }
        // 
        //         public override void DrawImageEllipse(in RectangleF rect, float startAngle, float endAngle)
        //         {
        //             System.Drawing.Drawing2D.GraphicsPath region = new System.Drawing.Drawing2D.GraphicsPath();
        //             region.AddPie(rect.X, rect.Y, rect.Width, rect.Height, startAngle - 90, endAngle);
        //             System.Drawing.Drawing2D.GraphicsState gs = gfx.Save();
        //             gfx.SetClip(region, System.Drawing.Drawing2D.CombineMode.Intersect);
        //             gfx.DrawImage(cur_img.src, 0, 0);
        //             gfx.Restore(gs);
        //         }

        public override void DrawImageZoom(in RectangleF rect)
        {
            var bounds = rect.ToUnity();
            UnityEngine.GUI.DrawTexture(bounds, cur_img.Texture, ScaleMode.StretchToFill);
        }
        public override void DrawImage(in Geometry.Vector2 pos)
        {
            UnityEngine.GUI.DrawTexture(new Rect(pos.X, pos.Y, cur_img.Width, cur_img.Height), cur_img.Texture);
        }
        public override void DrawImage(in Geometry.Vector2 pos, AlignmentStyle anchor)
        {
            transAnchor(anchor, cur_img.Width, cur_img.Height, out var px, out var py);
            UnityEngine.GUI.DrawTexture(new Rect(pos.X + px, pos.Y + py, cur_img.Width, cur_img.Height), cur_img.Texture);
        }
        public override void DrawImageTrans(in Geometry.Vector2 pos, Trans trans)
        {
            PushTransform();
            {
                Translate(pos.X, pos.Y);
                transTans(trans, cur_img.Width, cur_img.Height, out var tw, out var th);
                UnityEngine.GUI.DrawTexture(new Rect(0, 0, tw, th), cur_img.Texture);
            }
            PopTransform();
        }
        public override void DrawImageRegion(in RectangleF srcRect, in Geometry.Vector2 dst)
        {
            var uv = cur_img.ToUV(srcRect);
            UnityEngine.GUI.DrawTextureWithTexCoords(
                new Rect(dst.X, dst.Y, srcRect.Width, srcRect.Height),
                cur_img.Texture, uv);
        }
        public override void DrawImageRegion(in RectangleF srcRect, in RectangleF dstRect)
        {
            var uv = cur_img.ToUV(srcRect);
            UnityEngine.GUI.DrawTextureWithTexCoords(dstRect.ToUnity(), cur_img.Texture, uv);
        }
        public override void DrawImageRegion(in RectangleF srcRect, Trans tx, in Geometry.Vector2 dst)
        {
            PushTransform();
            {
                var uv = cur_img.ToUV(srcRect);
                Translate(dst.X, dst.Y);
                transTans(tx, srcRect.Width, srcRect.Height, out var tw, out var th);
                UnityEngine.GUI.DrawTextureWithTexCoords(new Rect(0, 0, tw, th), cur_img.Texture, uv);
            }
            PopTransform();
        }
        public override void DrawImageRegion(in RectangleF srcRect, Trans tx, in RectangleF dstRect)
        {
            PushTransform();
            {
                var uv = cur_img.ToUV(srcRect);
                Translate(dstRect.X, dstRect.Y);
                transTans(tx, dstRect.Width, dstRect.Height, out var tw, out var th);
                UnityEngine.GUI.DrawTextureWithTexCoords(new Rect(0, 0, tw, th), cur_img.Texture, uv);
            }
            PopTransform();
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------
        #region Shape
        public override void DrawLine(in Line2 line)
        {

        }
        public override void FillRect(in RectangleF rect)
        {

        }
        public override void DrawRect(in RectangleF rect)
        {

        }
        public override void FillArc(in RectangleF rect, float startAngle, float arcAngle)
        {

        }
        public override void DrawArc(in RectangleF rect, float startAngle, float arcAngle)
        {

        }
        public override void FillRect4Color(in RectangleF rect, uint[] argb_4)
        {

        }
        public override void FillRoundRect(in RectangleF rect, float rx, float ry)
        {

        }
        public override void DrawRoundRect(in RectangleF rect, float rx, float ry)
        {

        }
        public override void DrawPolygon(Geometry.Vector2[] points)
        {

        }
        public override void FillPolygon(Geometry.Vector2[] points)
        {

        }
        public override void FillRectEllipse(in RectangleF rect, float startAngle, float endAngle)
        {
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------
    }
}
