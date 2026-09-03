using DeepCore.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore.GUI.Display
{
    public static class GraphicsUtils
    {
        public static void DrawResize9(this Graphics g, int x, int y, int w, int h, int bsize)
        {
            g.PushTransform();
            g.Translate(x, y);
            int s1 = bsize;
            int s2 = bsize << 1;
            int s4 = bsize << 2;
            g.PushClip();
            g.SetClip(-s2, -s2, w + s4, h + s4);
            {
                g.DrawRect(0, 0, w - 1, h - 1);
                g.FillRect(-s1, -s1, s2, s2);
                g.FillRect(w - s1, -s1, s2, s2);
                g.FillRect(-s1, h - s1, s2, s2);
                g.FillRect(w - s1, h - s1, s2, s2);
                g.FillRect(w / 2 - s1, -s1, s2, s2);
                g.FillRect(w / 2 - s1, h - s1, s2, s2);
                g.FillRect(-s1, h / 2 - s1, s2, s2);
                g.FillRect(w - s1, h / 2 - s1, s2, s2);
            }
            g.PopClip();
            g.PopTransform();
        }
        public static void SetClip(this Graphics g, int x, int y, int w, int h)
        {
            g.SetClip(new RectangleF(x, y, w, h));
        }
        public static void SetClip(this Graphics g, float x, float y, float w, float h)
        {
            g.SetClip(new RectangleF(x, y, w, h));
        }
        public static void DrawCross(this Graphics g, float cx, float cy, float cr)
        {
            g.DrawLine(cx - cr, cy, cx + cr, cy);
            g.DrawLine(cx, cy - cr, cx, cy + cr);
        }

        public static void DrawFan(this Graphics g, float direction, float range, float angle)
        {
            if (angle != 0)
            {
                float startRadians = direction - angle / 2;
                float endRadians = direction + angle / 2;
                g.DrawArc(
                    -range,
                    -range,
                    range * 2,
                    range * 2,
                    CMath.RadianToAngle(startRadians),
                    CMath.RadianToAngle(angle));
                g.DrawLine(0, 0,
                    (float)Math.Cos(startRadians) * range,
                    (float)Math.Sin(startRadians) * range);
                g.DrawLine(0, 0,
                    (float)Math.Cos(endRadians) * range,
                    (float)Math.Sin(endRadians) * range);
            }
            else
            {
                g.DrawLine(0, 0,
                    (float)Math.Cos(direction) * range,
                    (float)Math.Sin(direction) * range);
            }
        }
        public static void FillFan(this Graphics g, float direction, float range, float angle)
        {
            float startRadians = direction - angle / 2;
            float endRadians = direction + angle / 2;
            g.FillArc(
                -range,
                -range,
                range * 2,
                range * 2,
                CMath.RadianToAngle(startRadians),
                CMath.RadianToAngle(angle));
        }

        /// <summary>
        /// 画粗线条
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="width"></param>
        public static void DrawLineRect(this Graphics g, float sx, float sy, float dx, float dy, float line_r)
        {
            float direction = VectorHelper.GetDegree(sx, sy, dx, dy);
            Vector2 s_l = new Vector2(sx, sy);
            Vector2 s_r = new Vector2(sx, sy);
            Vector2 d_l = new Vector2(dx, dy);
            Vector2 d_r = new Vector2(dx, dy);
            VectorHelper.MovePolar(ref s_l, direction + CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref d_l, direction + CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref s_r, direction - CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref d_r, direction - CMath.PI_DIV_2, line_r);
            g.DrawLine(s_l.X, s_l.Y, d_l.X, d_l.Y);
            g.DrawLine(s_r.X, s_r.Y, d_r.X, d_r.Y);
            g.DrawLine(s_l.X, s_l.Y, s_r.X, s_r.Y);
            g.DrawLine(d_l.X, d_l.Y, d_r.X, d_r.Y);
        }
        public static void FillLineRect(this Graphics g, float sx, float sy, float dx, float dy, float line_r)
        {
            float direction = VectorHelper.GetDegree(sx, sy, dx, dy);
            Vector2 s_l = new Vector2(sx, sy);
            Vector2 s_r = new Vector2(sx, sy);
            Vector2 d_l = new Vector2(dx, dy);
            Vector2 d_r = new Vector2(dx, dy);
            VectorHelper.MovePolar(ref s_l, direction + CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref d_l, direction + CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref s_r, direction - CMath.PI_DIV_2, line_r);
            VectorHelper.MovePolar(ref d_r, direction - CMath.PI_DIV_2, line_r);
            g.FillPolygon(
                new Vector2[] {
                    new Vector2(s_l.X, s_l.Y),
                    new Vector2(s_r.X, s_r.Y),
                    new Vector2(d_r.X, d_r.Y),
                    new Vector2(d_l.X, d_l.Y)
                });
        }


        /// <summary>
        /// 画圆角粗线条
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="size"></param>
        public static void DrawLineRoundRect(this Graphics g, float sx, float sy, float dx, float dy, float line_r)
        {
            float direction = VectorHelper.GetDegree(sx, sy, dx, dy);
            float r = line_r;
            float size = r * 2;
            Vector2 s_l = new Vector2(sx, sy);
            Vector2 s_r = new Vector2(sx, sy);
            Vector2 d_l = new Vector2(dx, dy);
            Vector2 d_r = new Vector2(dx, dy);
            VectorHelper.MovePolar(ref s_l, direction + CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref d_l, direction + CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref s_r, direction - CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref d_r, direction - CMath.PI_DIV_2, r);
            g.DrawLine(s_l.X, s_l.Y, d_l.X, d_l.Y);
            g.DrawLine(s_r.X, s_r.Y, d_r.X, d_r.Y);
            float angle = CMath.RadianToAngle(direction);
            g.DrawArc(sx - r, sy - r, size, size, angle + 180 - 90, 180);
            g.DrawArc(dx - r, dy - r, size, size, angle + 360 - 90, 180);
        }

        public static void FillLineRoundRect(this Graphics g, float sx, float sy, float dx, float dy, float line_r)
        {
            float direction = VectorHelper.GetDegree(sx, sy, dx, dy);
            float r = line_r;
            float size = r * 2;
            Vector2 s_l = new Vector2(sx, sy);
            Vector2 s_r = new Vector2(sx, sy);
            Vector2 d_l = new Vector2(dx, dy);
            Vector2 d_r = new Vector2(dx, dy);
            VectorHelper.MovePolar(ref s_l, direction + CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref d_l, direction + CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref s_r, direction - CMath.PI_DIV_2, r);
            VectorHelper.MovePolar(ref d_r, direction - CMath.PI_DIV_2, r);
            g.FillPolygon(
                new Vector2[] {
                    new Vector2(s_l.X, s_l.Y),
                    new Vector2(s_r.X, s_r.Y),
                    new Vector2(d_r.X, d_r.Y),
                    new Vector2(d_l.X, d_l.Y)
                });
            float angle = CMath.RadianToAngle(direction);
            g.FillArc(sx - r, sy - r, size, size, angle + 180 - 90, 180);
            g.FillArc(dx - r, dy - r, size, size, angle + 360 - 90, 180);
        }

        public static Vector2[] GenCursor(float sx, float sy, float dx, float dy, float width, float cursor_width, float cursor_height)
        {
            if (cursor_width < width)
                cursor_width = width;
            if (cursor_height < width)
                cursor_height = width;

            float ox = sx - dx;
            float oy = sy - dy;
            float ds = CMath.GetDistance(sx, sy, dx, dy);
            float hw = width / 2f;
            float cw = cursor_width / 2;
            float od = (float)Math.Atan2(oy, ox);

            var sC = new Vector2(0, 0);
            var sL = new Vector2(-hw, 0);
            var sR = new Vector2(+hw, 0);

            var dC = new Vector2(0, ds);
            var dL = new Vector2(-hw, ds - cursor_height);
            var dR = new Vector2(+hw, ds - cursor_height);
            var dLL = new Vector2(-cw, ds - cursor_height);
            var dRR = new Vector2(+cw, ds - cursor_height);

            var points = new Vector2[] { sC, sL, dL, dLL, dC, dRR, dR, sR, sC };

            for (int i = 0; i < points.Length; i++)
            {
                float x = points[i].X;
                float y = points[i].Y;
                VectorHelper.Rotate(ref x, ref y, 0, 0, od + CMath.PI_DIV_2);
                points[i].X = x + sx;
                points[i].Y = y + sy;
            }
            return points;
        }

        public static void FillCursor(this Graphics g, float sx, float sy, float dx, float dy, float width, float cursor_width = 0, float cursor_height = 0)
        {
            var points = GenCursor(sx, sy, dx, dy, width, cursor_width, cursor_height);
            g.FillPolygon(points);
        }
        public static void DrawCursor(this Graphics g, float sx, float sy, float dx, float dy, float width, float cursor_width = 0, float cursor_height = 0)
        {
            var points = GenCursor(sx, sy, dx, dy, width, cursor_width, cursor_height);
            g.DrawPolygon(points);
        }


        public static void DrawImageBounds(this Graphics gfx, RectangleF clip, DeepCore.GUI.Data.AlignmentStyle anchor, RectangleF bounds)
        {
            switch (anchor)
            {
                case Data.AlignmentStyle.TopLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y));
                    break;
                case Data.AlignmentStyle.TopCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + (bounds.Width - clip.Width) / 2f,
                    bounds.Y));
                    break;
                case Data.AlignmentStyle.TopRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + bounds.Width - clip.Width,
                    bounds.Y));
                    break;


                case Data.AlignmentStyle.MiddleLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y + (bounds.Height - clip.Height) / 2f));
                    break;
                case Data.AlignmentStyle.MiddleCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + (bounds.Width - clip.Width) / 2f,
                    bounds.Y + (bounds.Height - clip.Height) / 2f));
                    break;
                case Data.AlignmentStyle.MiddleRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + bounds.Width - clip.Width,
                    bounds.Y + (bounds.Height - clip.Height) / 2f));
                    break;

                case Data.AlignmentStyle.BottomLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y + bounds.Height - clip.Height));
                    break;
                case Data.AlignmentStyle.BottomCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + (bounds.Width - clip.Width) / 2f,
                    bounds.Y + bounds.Height - clip.Height));
                    break;
                case Data.AlignmentStyle.BottomRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X + bounds.Width - clip.Width,
                    bounds.Y + bounds.Height - clip.Height));
                    break;

                default:
                    gfx.DrawImageRegion(clip, bounds);
                    break;
            }
        }

        public static void DrawImageAnchor(this Graphics gfx, RectangleF clip, DeepCore.GUI.Data.AlignmentStyle anchor, Vector3 bounds)
        {
            switch (anchor)
            {
                case Data.AlignmentStyle.TopLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y));
                    break;
                case Data.AlignmentStyle.TopCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width / 2f,
                    bounds.Y));
                    break;
                case Data.AlignmentStyle.TopRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width,
                    bounds.Y));
                    break;


                case Data.AlignmentStyle.MiddleLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y - clip.Height / 2f));
                    break;
                case Data.AlignmentStyle.MiddleCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width / 2f,
                    bounds.Y - clip.Height / 2f));
                    break;
                case Data.AlignmentStyle.MiddleRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width,
                    bounds.Y - clip.Height / 2f));
                    break;

                case Data.AlignmentStyle.BottomLeft:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X,
                    bounds.Y - clip.Height));
                    break;
                case Data.AlignmentStyle.BottomCenter:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width / 2f,
                    bounds.Y - clip.Height));
                    break;
                case Data.AlignmentStyle.BottomRight:
                    gfx.DrawImageRegion(clip, new Vector2(
                    bounds.X - clip.Width,
                    bounds.Y - clip.Height));
                    break;

                default:
                    gfx.DrawImageRegion(clip, bounds);
                    break;
            }
        }


        public static void DrawStringBounds(this Graphics gfx, string text, Color bodyBrush, Color borderBrush,
            DeepCore.GUI.Data.TextBorderStyle borderTime,
            Vector2 pos)
        {
            DrawStringBounds(gfx, text, bodyBrush, borderBrush, borderTime, DeepCore.GUI.Data.AlignmentStyle.TopLeft, pos);
        }
        public static void DrawStringBounds(this Graphics gfx, string text, Color bodyBrush, Color borderBrush,
            DeepCore.GUI.Data.TextBorderStyle borderTime,
            DeepCore.GUI.Data.AlignmentStyle anchor,
            Vector2 pos)
        {
            var rect = new RectangleF(pos, Vector2.Zero);
            DrawStringBounds(gfx, text, bodyBrush, borderBrush, borderTime, anchor, rect);
        }
        public static void DrawStringBounds(this Graphics gfx, string text, Color bodyBrush, Color borderBrush,
            DeepCore.GUI.Data.TextBorderStyle borderTime,
            DeepCore.GUI.Data.AlignmentStyle anchor,
            RectangleF expectRect)
        {
            gfx.DrawStringBorder(text, borderTime, bodyBrush, borderBrush, expectRect, anchor);
        }

        //         private static void DrawString(this Graphics gfx, string text, Color brush, float x, float y, RectangleF rect)
        //         {
        //             gfx.Translate(x, y);
        //             gfx.SetColor(brush);
        //             if (rect.Width == 0 || rect.Height == 0)
        //             {
        //                 gfx.DrawString(text, rect.Location);
        //             }
        //             else
        //             {
        //                 gfx.DrawString(text, rect);
        //             }
        //             gfx.Translate(-x, -y);
        //         }

        public static void DrawGridLines(this Graphics gfx, float startX, float startY, float gridW, float gridH, int xcount, int ycount)
        {
            gfx.Translate(startX, startY);
            var tw = xcount * gridW;
            var th = ycount * gridH;
            for (var x = 0; x <= xcount; x++)
            {
                float dx = x * gridW;
                gfx.DrawLine(dx, 0, dx, th);
            }
            for (var y = 0; y <= ycount; y++)
            {
                float dy = y * gridH;
                gfx.DrawLine(0, dy, tw, dy);
            }
            gfx.Translate(-startX, -startY);
        }

        #region color
        public static Color InverseColor(this Color c)
        {
            return new Color(
                Math.Min(1f - c.R, 1f),
                Math.Min(1f - c.G, 1f),
                Math.Min(1f - c.B, 1f),
                c.A);
        }
        public static Color SetAlpha(this Color c, float a)
        {
            return new Color(c.R, c.G, c.B, a);
        }

        public static Color ToAlpha(this Color src, float alpha)
        {
            return new Color(src.R, src.G, src.B, Math.Min(src.A * alpha, 1));
        }
        public static Color ToDark(this Color src, float dark)
        {
            return new Color(
                Math.Min(src.R * dark, 1f),
                Math.Min(src.G * dark, 1f),
                Math.Min(src.B * dark, 1f),
                src.A);
        }
        public static Color ToAlphaAndDark(this Color src, float alpha, float dark)
        {
            return new Color(
                Math.Min(src.R * dark, 1f),
                Math.Min(src.G * dark, 1f),
                Math.Min(src.B * dark, 1f),
                Math.Min(src.A * alpha, 1));
        }
        #endregion

        public static RectangleF GetAlignmentBounds(this Data.AlignmentStyle anchor, in RectangleF parentBounds, in Vector2 contentSize)
        {
            var expectBounds = parentBounds;
            switch (anchor)
            {
                case DeepCore.GUI.Data.AlignmentStyle.TopLeft:
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleLeft:
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height / 2) - contentSize.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomLeft:
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height) - contentSize.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopCenter:
                    expectBounds.X = (parentBounds.X + parentBounds.Width / 2) - contentSize.X / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleCenter:
                    expectBounds.X = (parentBounds.X + parentBounds.Width / 2) - contentSize.X / 2;
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height / 2) - contentSize.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomCenter:
                    expectBounds.X = (parentBounds.X + parentBounds.Width / 2) - contentSize.X / 2;
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height) - contentSize.Y;
                    break;

                case DeepCore.GUI.Data.AlignmentStyle.TopRight:
                    expectBounds.X = (parentBounds.X + parentBounds.Width) - contentSize.X;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.MiddleRight:
                    expectBounds.X = (parentBounds.X + parentBounds.Width) - contentSize.X;
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height / 2) - contentSize.Y / 2;
                    break;
                case DeepCore.GUI.Data.AlignmentStyle.BottomRight:
                    expectBounds.X = (parentBounds.X + parentBounds.Width) - contentSize.X;
                    expectBounds.Y = (parentBounds.Y + parentBounds.Height) - contentSize.Y;
                    break;
            }
            expectBounds.Size = contentSize;
            return parentBounds;
        }


        public static Vector2 MeasureItems(int itemCount, in Vector2 itemSize, float expectWidth)
        {
            var ret = Vector2.Zero;
            var loc = Vector2.Zero;
            for (int i = 0; i < itemCount; i++)
            {
                if (loc.X + itemSize.X <= expectWidth)
                {
                    loc.X += itemSize.X;
                    ret.X = loc.X;
                }
                else
                {
                    loc.X = 0;
                    ret.Y += itemSize.Y;
                }
            }
            ret.X = Math.Max(ret.X, itemSize.X);
            ret.Y = Math.Max(ret.Y, itemSize.Y);
            return ret;
        }
    }
}
