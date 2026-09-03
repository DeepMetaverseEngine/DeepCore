#if HZUI

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using DeepCore.GUI.Gemo;
using DeepCore.GUI.Display;
using DeepCore;

namespace DeepCore.Unity3D.Impl
{
    public class UnityGraphics : DeepCore.GUI.Display.Graphics
    {
        private Stack<GraphicsStatus> stack_clips = new Stack<GraphicsStatus>();
        private GraphicsStatus cur_clip;

        private Matrix4x4 root_matrix;
        private Matrix4x4 cur_matrix;
        private Stack<Matrix4x4> stack_matrix = new Stack<Matrix4x4>();

        private Blend mBlend = Blend.BLEND_MODE_NORMAL;
        private uint mColor = 0xFFFFFFFF;
        private UnityEngine.Color mUnityColor = UnityEngine.Color.white;
        private float mAlpha = 1.0f;

        private IUnityImageInterface cur_uimg;
        private List<UnityQuards2D> mQuadsBatch = new List<UnityQuards2D>(100);


        public UnityGraphics()
        {
        }


        public void Begin()
        {
            GL.PushMatrix();

            GL.Viewport(new Rect(0, 0, Screen.width, Screen.height));

            Matrix4x4 m1 = Matrix4x4.TRS(
                new Vector3(0, Screen.height, 0),
                Quaternion.identity,
                new Vector3(1f, -1f, 1f));
            GL.MultMatrix(m1);
            this.tx_translate = Vector3.zero;
            this.root_matrix = m1;

            this.cur_matrix = Matrix4x4.identity;
            this.stack_matrix.Push(cur_matrix);

            this.cur_clip.mat = root_matrix;
            this.cur_clip.clip = new Rect(0, 0, Screen.width, Screen.height);
            this.stack_clips.Push(cur_clip);

            this.mAlpha = 1;
            this.mColor = 0xFFFFFFFF;
            this.mUnityColor = UnityEngine.Color.white;
            this.mBlend = Blend.BLEND_MODE_NORMAL;
            UnityShaders.SetAlpha(1);
            UnityShaders.SetColor(mUnityColor);
            UnityShaders.SetBlend(mBlend);
        }

        public void End()
        {
            FlushQuadsBath();

            this.stack_clips.Clear();
            this.stack_matrix.Clear();
            GL.Viewport(new Rect(0, 0, Screen.width, Screen.height));
            GL.PopMatrix();
        }

        public override void Dispose()
        {
        }

        //-----------------------------------------------------------------------------------------------

#region COLOR

        public override void setBlend(DeepCore.GUI.Display.Blend blend)
        {
            if(blend != mBlend)
            {
                FlushQuadsBath();
                mBlend = blend;
                UnityShaders.SetBlend(blend);
            }
        }

        public override Blend getBlend()
        {
            return mBlend;
        }

        public override void setAlpha(float alpha)
        {
            if(mAlpha != alpha)
            {
                FlushQuadsBath();
                mAlpha = alpha;
                UnityShaders.SetAlpha(alpha);
            }
        }
        public override void setColor(uint color)
        {
            if(mColor != color)
            {
                mColor = color;
                DeepCore.GUI.Display.Color.toRGBAF(color,
                    out mUnityColor.r,
                    out mUnityColor.g,
                    out mUnityColor.b,
                    out mUnityColor.a);
                UnityShaders.SetColor(mUnityColor);
            }
        }
        public override void setColor(int red, int green, int blue)
        {
            uint color = DeepCore.GUI.Display.Color.toRGBA(red, green, blue, 255);
            setColor(color);
        }
        public override void setColor(int red, int green, int blue, int alpha)
        {
            uint color = DeepCore.GUI.Display.Color.toRGBA(red, green, blue, alpha);
            setColor(color);
        }
        public override void setColor(float red, float green, float blue, float alpha)
        {
            uint color = DeepCore.GUI.Display.Color.toRGBA(red, green, blue, alpha);
            setColor(color);
        }
        public override uint getColor()
        {
            return mColor;
        }
        public override float getAlpha()
        {
            return mAlpha;
        }

#endregion

        //-----------------------------------------------------------------------------------------------

#region CLIP

        public override void SetClip(Rectangle2D rect)
        {
            SetClip(rect.x, rect.y, rect.width, rect.height);
        }
        public override void SetClip(float x, float y, float w, float h)
        {
            FlushQuadsBath();

            float x2 = x + w;
            float y2 = y + h;

            x = Math.Max(x, 0);
            x = Math.Min(x, Screen.width);
            y = Math.Max(y, 0);
            y = Math.Min(y, Screen.height);
            x2 = Math.Max(x2, 0);
            x2 = Math.Min(x2, Screen.width);
            y2 = Math.Max(y2, 0);
            y2 = Math.Min(y2, Screen.height);

            w = x2 - x;
            h = y2 - y;

            if(x != cur_clip.clip.x ||
                y != cur_clip.clip.y ||
                w != cur_clip.clip.width ||
                h != cur_clip.clip.height)
            {
                Rect clip = new Rect(x, y, w, h);

                Matrix4x4 m2 = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(Screen.width / clip.width, (Screen.height / clip.height), 1f));
                Matrix4x4 m3 = Matrix4x4.TRS(
                    new Vector3(0, clip.height, 0),
                    Quaternion.identity,
                    new Vector3(1f, -1f, 1f));
                Matrix4x4 m4 = Matrix4x4.TRS(
                    new Vector3(-x, -y, 0),
                    Quaternion.identity,
                    Vector3.one);

                cur_clip.clip = clip;
                cur_clip.mat = m2 * m3 * m4;
                root_matrix = cur_clip.mat;

                GL.Viewport(new Rect(clip.x, Screen.height - clip.y - clip.height, clip.width, clip.height));
                GL.MultMatrix(root_matrix * cur_matrix);
                //cur_material.SetColor("_clrBase", mColor);

            }
        }
        public override void PushClip()
        {
            cur_clip.mat = root_matrix;
            stack_clips.Push(cur_clip);
        }
        public override void PopClip()
        {
            if (stack_clips.Count > 0)
            {
                FlushQuadsBath();
                cur_clip = stack_clips.Pop();
                root_matrix = cur_clip.mat;
                Rect clip = cur_clip.clip;
                GL.Viewport(new Rect(clip.x, Screen.height - clip.y - clip.height, clip.width, clip.height));
                GL.MultMatrix(root_matrix * cur_matrix);
            }
        }

#endregion

        //-----------------------------------------------------------------------------------------------

#region TRANSFORM

        private Vector3 tx_translate = Vector3.zero;
        public static bool IsIgnoreTranslateDrawcall = false;

        private void flush_translate()
        {
            if (IsIgnoreTranslateDrawcall)
            {
                if (tx_translate.x != 0 || tx_translate.y != 0)
                {
                    Matrix4x4 m = Matrix4x4.TRS(
                        tx_translate,
                        Quaternion.identity,
                        Vector3.one);
                    cur_matrix = cur_matrix * m;
                    GL.MultMatrix(root_matrix * cur_matrix);
                    tx_translate = Vector3.zero;
                }
            }
        }

        public override void translate(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                if (IsIgnoreTranslateDrawcall)
                {
                    tx_translate.x += x;
                    tx_translate.y += y;
                }
                else
                {
                    FlushQuadsBath();
                    Matrix4x4 m = Matrix4x4.TRS(
                        new Vector3(x, y, 0),
                        Quaternion.identity,
                        Vector3.one);
                    cur_matrix = cur_matrix * m;
                    GL.MultMatrix(root_matrix * cur_matrix);
                }  
            }
        }

        public override void rotate(float angle)
        {
            if (angle != 0)
            {
                FlushQuadsBath();
                Matrix4x4 m = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.Euler(0f, 0f, angle),
                    Vector3.one);
                cur_matrix = cur_matrix * m;
                GL.MultMatrix(root_matrix * cur_matrix);
            }
        }

        public override void scale(float sx, float sy)
        {
            if (sx != 1 || sy != 1)
            {
                FlushQuadsBath();
                Matrix4x4 m = Matrix4x4.TRS(
                      Vector3.zero,
                      Quaternion.identity,
                      new Vector3(sx, sy, 1.0f));
                cur_matrix = cur_matrix * m;
                GL.MultMatrix(root_matrix * cur_matrix);
            }
        }

        public override void pushTransform()
        {
            stack_matrix.Push(cur_matrix);
        }

        public override void popTransform()
        {
            FlushQuadsBath();
            cur_matrix = stack_matrix.Pop();
            GL.MultMatrix(root_matrix * cur_matrix);
        }

        private static void transAnchor(Anchor anchor, float w, float h, ref float x, ref float y)
        {
            float tx = 0, ty = 0;
            if((anchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                tx = -w / 2;
            }
            else if((anchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                tx = -w;
            }

            if((anchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                ty = -h / 2;
            }
            else if((anchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                ty = -h;
            }
            x += tx;
            y += ty;
        }

        private static void transAnchor(
            ImageAnchor anchor,
            float sw, float sh, float dw, float dh,
            ref float x, ref float y)
        {
            float dx = 0;
            float dy = 0;
            switch(anchor)
            {
                case ImageAnchor.L_T:
                    break;
                case ImageAnchor.C_T:
                    dx = (dw - sw) / 2;
                    break;
                case ImageAnchor.R_T:
                    dx = (dw - sw);
                    break;
                case ImageAnchor.L_C:
                    dy = (dh - sh) / 2;
                    break;
                case ImageAnchor.C_C:
                    dx = (dw - sw) / 2;
                    dy = (dh - sh) / 2;
                    break;
                case ImageAnchor.R_C:
                    dx = (dw - sw);
                    dy = (dh - sh) / 2;
                    break;
                case ImageAnchor.L_B:
                    dy = (dh - sh);
                    break;
                case ImageAnchor.C_B:
                    dx = (dw - sw) / 2;
                    dy = (dh - sh);
                    break;
                case ImageAnchor.R_B:
                    dx = (dw - sw);
                    dy = (dh - sh);
                    break;
            }
            x += dx;
            y += dy;
        }

#endregion

        //-----------------------------------------------------------------------------------------------

#region IMAGE

        private void FlushQuadsBath()
        {
            int count = mQuadsBatch.Count;
            if(count > 0)
            {
                GL.Begin(GL.QUADS);
                for(int i = 0; i < count; ++i)
                {
                    mQuadsBatch[i].Draw();
                }
                GL.End();
                mQuadsBatch.Clear();
            }
            flush_translate();
        }

        private void popImage()
        {
            if(cur_uimg != null)
            {
                UnityShaders.BeginImage(cur_uimg);
            }
        }
        public override void beginImage(DeepCore.GUI.Display.Image image)
        {
            if (image != null)
            {
                if (image != cur_uimg)
                {
                    FlushQuadsBath();

                    if (image is IUnityImageInterface)
                        cur_uimg = (IUnityImageInterface)image;
                    else
                        cur_uimg = null;
                }
                UnityShaders.BeginImage((IUnityImageInterface)image);
            }
            else
            {
                cur_uimg = null;
            }
        }


        public override void drawVertex(VertexBuffer vertex)
        {
            flush_translate();
            ((UnityVertexBuffer)vertex).Draw();
        }
        public override void drawVertex(VertexBuffer vertex, int[] indices, VertexTopology mode)
        {
            flush_translate();
            ((UnityVertexBuffer)vertex).DrawVertex(indices, (int)mode);
        }
        public override void drawVertexSequence(VertexBuffer vertex, VertexTopology mode)
        {
            flush_translate();
            ((UnityVertexBuffer)vertex).DrawSequence((int)mode);
        }


        public override void drawImageZoom(float x, float y, float w, float h)
        {
            x += tx_translate.x;
            y += tx_translate.y;
            mQuadsBatch.Add(UnityQuards2D.DrawImageZoom(cur_uimg, x, y, w, h));
        }

        public override void drawRegion(float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh)
        {
            dx += tx_translate.x;
            dy += tx_translate.y;
            mQuadsBatch.Add(UnityQuards2D.DrawImageRegion(cur_uimg, sx, sy, sw, sh, dx, dy, dw, dh, Trans.TRANS_NONE));
        }

        public override void drawRegion(float sx, float sy, float w, float h, DeepCore.GUI.Display.Trans tx, float dx, float dy)
        {
            dx += tx_translate.x;
            dy += tx_translate.y;
            mQuadsBatch.Add(UnityQuards2D.DrawImageRegion(cur_uimg, sx, sy, w, h, dx, dy, w, h, tx));
        }

        public override void drawImage(float x, float y)
        {
            x += tx_translate.x;
            y += tx_translate.y;
            mQuadsBatch.Add(UnityQuards2D.DrawImage(cur_uimg, x, y));
        }

        public override void drawImage(float x, float y, DeepCore.GUI.Display.Anchor anchor)
        {
            x += tx_translate.x;
            y += tx_translate.y;
            transAnchor(anchor, cur_uimg.Width, cur_uimg.Height, ref x, ref y);
            mQuadsBatch.Add(UnityQuards2D.DrawImageZoom(cur_uimg, x, y, cur_uimg.Width, cur_uimg.Height));
        }

        public override void drawImageTrans(float x, float y, DeepCore.GUI.Display.Trans trans)
        {
            x += tx_translate.x;
            y += tx_translate.y;
            mQuadsBatch.Add(UnityQuards2D.DrawImageTrans(cur_uimg, x, y, trans));
        }

        public override void drawImageEllipse(
            float sx, float sy,
            float sw, float sh,
            float startAngle,
            float endAngle)
        {
            FlushQuadsBath();

            int density = 32;//Math.Max(32, 32);
            float dlen = 1 - cur_uimg.MaxV;

            float rx = sw / 2;
            float ry = sh / 2;
            float cx = sx + rx;
            float cy = sy + ry;

            float div_u = 1f / cur_uimg.Width * cur_uimg.MaxU;
            float div_v = 1f / cur_uimg.Height * cur_uimg.MaxV;

            float tcx = cx * div_u;
            float tcy = cy * div_v;

            float degree_start = CMath.DegreesToRadians(-startAngle + 90);
            float degree_end = CMath.DegreesToRadians(-endAngle + 90);
            float degree_delta = (degree_end - degree_start) / density;

            pushTransform();
            translate(0, sh);
            scale(1, -1);
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.TexCoord2(tcx, tcy);
            GL.Vertex3(cx, cx, 0);
            for(int i = density; i >= 0; --i)
            {
                float idegree = degree_start + i * degree_delta;
                float ox = cx + Mathf.Cos(idegree) * rx;
                float oy = cy + Mathf.Sin(idegree) * ry;
                float tdx = ox * div_u;
                float tdy = oy * div_v;

                GL.TexCoord2(tdx, tdy + dlen);
                GL.Vertex3(ox, oy, 0);

                GL.TexCoord2(tcx, tcy + dlen);
                GL.Vertex3(cx, cy, 0);
            }
            GL.End();
            popTransform();
        }

#region WRAPPER

        public override void drawImageZoom(DeepCore.GUI.Display.Image image, float x, float y, float w, float h)
        {
            beginImage(image);
            drawImageZoom(x, y, w, h);
        }
        public override void drawImage(DeepCore.GUI.Display.Image image, float x, float y)
        {
            beginImage(image);
            drawImage(x, y);
        }
        public override void drawImage(DeepCore.GUI.Display.Image image, float x, float y, DeepCore.GUI.Display.Anchor anchor)
        {
            beginImage(image);
            drawImage(x, y, anchor);
        }
        public override void drawImageTrans(DeepCore.GUI.Display.Image image, float x, float y, DeepCore.GUI.Display.Trans trans)
        {
            beginImage(image);
            drawImageTrans(x, y, trans);
        }
        public override void drawRegion(DeepCore.GUI.Display.Image src, float sx, float sy, float w, float h, DeepCore.GUI.Display.Trans tx, float dx, float dy)
        {
            beginImage(src);
            drawRegion(sx, sy, w, h, tx, dx, dy);
        }
        public override void drawRegion(DeepCore.GUI.Display.Image src, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh)
        {
            beginImage(src);
            drawRegion(sx, sy, sw, sh, dx, dy, dw, dh);
        }
#endregion

#endregion

#region TextLayer

        private UnityTextLayer beginTextLayer(DeepCore.GUI.Display.TextLayer text)
        {
            if (text != null)
            {
                UnityTextLayer cur_txt = (UnityTextLayer)text;
                cur_txt.Refresh();
                if (cur_txt.mTexture != null)
                {
                    FlushQuadsBath();
                    UnityShaders.BeginText(cur_txt);
                    return cur_txt;
                }
            }
            return null;
        }

        private void drawTextQuad(float x1, float y1, UnityTextLayer text)
        {
            if(text.mTexture == null)
            {
                return;
            }

            float x2 = x1 + text.Width;
            float y2 = y1 + text.Height;
            float u = text.Width / (float)text.mTexture.width;
            float v = text.Height / (float)text.mTexture.height;

            GL.Begin(GL.QUADS);

            GL.TexCoord2(0, v);
            GL.Vertex3(x1, y1, 0);

            GL.TexCoord2(0, 0);
            GL.Vertex3(x1, y2, 0);

            GL.TexCoord2(u, 0);
            GL.Vertex3(x2, y2, 0);

            GL.TexCoord2(u, v);
            GL.Vertex3(x2, y1, 0);

            GL.End();
        }

        public override void drawTextLayer(DeepCore.GUI.Display.TextLayer text, float x, float y, DeepCore.GUI.Display.Anchor anchor)
        {
            UnityTextLayer cur_txt = beginTextLayer(text);
            if(cur_txt != null)
            {
                transAnchor(anchor, text.Width, text.Height, ref x, ref y);
                drawTextQuad(x, y, cur_txt);
                popImage();
            }

        }
        public override void drawTextLayer(DeepCore.GUI.Display.TextLayer text, float x, float y, float w, float h, DeepCore.GUI.Display.ImageAnchor anchor)
        {
            UnityTextLayer cur_txt = beginTextLayer(text);
            if(cur_txt != null)
            {
                transAnchor(anchor, text.Width, text.Height, w, h, ref x, ref y);
                drawTextQuad(x, y, cur_txt);
                popImage();
            }
        }

#endregion

        //-----------------------------------------------------------------------------------------------

#region SHAPE

        public override void drawLine(float x1, float y1, float x2, float y2)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            GL.Begin(GL.LINES);
            GL.Vertex3(x1, y1, 0);
            GL.Vertex3(x2, y2, 0);
            GL.End();
            popImage();
        }

        public override void fillRect4Color(float x, float y, float w, float h, uint[] rgba)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            UnityEngine.Color color = new UnityEngine.Color();
            GL.Begin(GL.QUADS);
            DeepCore.GUI.Display.Color.toRGBAF(rgba[0], out color.r, out color.g, out color.b, out color.a);
            GL.Color(color);
            GL.Vertex3(x, y, 0);
            DeepCore.GUI.Display.Color.toRGBAF(rgba[1], out color.r, out color.g, out color.b, out color.a);
            GL.Color(color);
            GL.Vertex3(x, y + h, 0);
            DeepCore.GUI.Display.Color.toRGBAF(rgba[2], out color.r, out color.g, out color.b, out color.a);
            GL.Color(color);
            GL.Vertex3(x + w, y + h, 0);
            DeepCore.GUI.Display.Color.toRGBAF(rgba[3], out color.r, out color.g, out color.b, out color.a);
            GL.Color(color);
            GL.Vertex3(x + w, y, 0);
            GL.End();
            popImage();
        }

        public override void drawRect(float x, float y, float w, float h)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            float x2 = x + w;
            float y2 = y + h;
            GL.Begin(GL.LINES);

            GL.Vertex3(x, y, 0);
            GL.Vertex3(x2, y, 0);

            GL.Vertex3(x2, y, 0);
            GL.Vertex3(x2, y2, 0);

            GL.Vertex3(x2, y2, 0);
            GL.Vertex3(x, y2, 0);

            GL.Vertex3(x, y2, 0);
            GL.Vertex3(x, y, 0);
            GL.End();
            popImage();
        }
        public override void fillRect(float x, float y, float w, float h)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            GL.Begin(GL.QUADS);
            GL.Color(mUnityColor);
            GL.Vertex3(x, y, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x, y + h, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x + w, y + h, 0);
            GL.Color(mUnityColor);
            GL.Vertex3(x + w, y, 0);
            GL.End();
            popImage();
        }

        public override void fillRoundRect(float x, float y, float w, float h, float rx, float ry)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            GL.Begin(GL.QUADS);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x, y + h, 0);
            GL.Vertex3(x + w, y + h, 0);
            GL.Vertex3(x + w, y, 0);
            GL.End();
            popImage();
        }
        public override void drawRoundRect(float x, float y, float w, float h, float rx, float ry)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            GL.Begin(GL.LINES);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x, y + h, 0);
            GL.Vertex3(x + w, y + h, 0);
            GL.Vertex3(x + w, y, 0);
            GL.End();
            popImage();
        }

        public override void fillArc(float x, float y, float w, float h, float startAngle, float arcAngle)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();
            int point_count = 32;
            float sw = w / 2;
            float sh = h / 2;
            float sx = x + sw;
            float sy = y + sh;

            float degree_start = CMath.DegreesToRadians(startAngle);
            float degree_delta = Mathf.PI * 2 / point_count;
            point_count++;
            GL.Begin(GL.TRIANGLE_STRIP);
            for(int i = 0; i < point_count; i++)
            {
                float idegree = degree_start + i * degree_delta;
                GL.Vertex3(sx + Mathf.Cos(idegree) * sw, sy + Mathf.Sin(idegree) * sh, 0);
                GL.Vertex3(sx, sy, 0);
            }
            GL.End();
            popImage();
        }

        public override void drawArc(float x, float y, float w, float h, float startAngle, float arcAngle)
        {
            FlushQuadsBath();
            UnityShaders.BeginShape();

            int point_count = 32;
            float sw = w / 2;
            float sh = h / 2;
            float sx = x + sw;
            float sy = y + sh;

            float degree_start = CMath.DegreesToRadians(startAngle);
            float degree_delta = Mathf.PI * 2 / point_count;
            point_count++;
            GL.Begin(GL.LINES);
            for(int i = 0; i < point_count; i++)
            {
                float idegree = degree_start + i * degree_delta;
                GL.Vertex3(sx + Mathf.Cos(idegree) * sw, sy + Mathf.Sin(idegree) * sh, 0);
                GL.Vertex3(sx, sy, 0);
            }
            GL.End();
            popImage();
        }





#endregion

        //-----------------------------------------------------------------------------------------------


        private struct GraphicsStatus
        {
            public Rect clip;
            public Matrix4x4 mat;
        }



    }


}
#endif