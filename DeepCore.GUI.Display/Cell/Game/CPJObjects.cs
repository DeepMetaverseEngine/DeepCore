using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using System;
namespace DeepCore.GUI.Cell.Game
{
    public class CCD
    {
        public enum CDType : byte
        {
            CD_TYPE_RECT = 1,
            CD_TYPE_LINE = 2,
            CD_TYPE_POINT = 3,
        }

        public CDType Type = CDType.CD_TYPE_RECT;

        public int Mask;

        /// <summary>
        /// Left
        /// </summary>
        public float X1;
        /// <summary>
        /// Top
        /// </summary>
        public float Y1;
        /// <summary>
        /// Right
        /// </summary>
        public float X2;
        /// <summary>
        /// Bottom
        /// </summary>
        public float Y2;

        public CCD() { }

        public CCD(CCD theobj)
        {
            this.Type = theobj.Type;
            this.Mask = theobj.Mask;
            this.X1 = theobj.X1;
            this.Y1 = theobj.Y1;
            this.X2 = theobj.X2;
            this.Y2 = theobj.Y2;
        }

        public float Width
        {
            get
            {
                return X2 - X1;
            }
        }

        public float Height
        {
            get
            {
                return Y2 - Y1 + 1;
            }
        }

        /// <summary>
        /// 得到外包矩形中心点和渲染中心点的偏移
        /// </summary>
        public float MedianY
        {
            get
            {
                return -(Height / 2 - (Y1 + Height));
            }
        }

        /// <summary>
        /// 得到外包矩形中心点和渲染中心点的偏移
        /// </summary>
        public float MedianX
        {
            get
            {
                return -(Width / 2 - (X1 + Width));
            }
        }


        static public CCD CreateCDRect(int mask, float x, float y, float w, float h)
        {
            CCD ret = new CCD();
            ret.Type = CDType.CD_TYPE_RECT;
            ret.Mask = mask;
            ret.X1 = x;
            ret.Y1 = y;
            ret.X2 = (x + w);
            ret.Y2 = (y + h);
            return ret;
        }

        static public CCD CreateCDRect_2Point(int mask, float sx, float sy, float dx, float dy)
        {
            CCD ret = new CCD();
            ret.Type = CDType.CD_TYPE_RECT;
            ret.Mask = mask;
            ret.X1 = Math.Min(sx, dx);
            ret.Y1 = Math.Min(sy, dy);
            ret.X2 = Math.Max(sx, dx);
            ret.Y2 = Math.Max(sy, dy);
            return ret;
        }

        static public CCD CreateCDLine(int mask, float px, float py, float qx, float qy)
        {
            CCD ret = new CCD();
            ret.Type = CDType.CD_TYPE_LINE;
            ret.Mask = mask;
            ret.X1 = px;
            ret.Y1 = py;
            ret.X2 = qx;
            ret.Y2 = qy;
            return ret;
        }

        public Geometry.RectangleF ToRect()
        {
            return new Geometry.RectangleF(X1, Y1, Width, Height);
        }

    }

    public class CGroup
    {
        internal short[][] Frames;

        internal int SubIndex = 0;
        internal int SubCount = 0;

        internal float w_left = 0;
        internal float w_top = 0;
        internal float w_bottom = 1;
        internal float w_right = 1;

        internal float w_width = 0;
        internal float w_height = 0;

        protected void fixArea(float left, float top, float right, float botton)
        {
            if (left < w_left)
                w_left = (short)left;
            if (top < w_top)
                w_top = (short)top;
            if (right > w_right)
                w_right = (short)right;
            if (botton > w_bottom)
                w_bottom = (short)botton;

            w_width = (short)(w_right - w_left);
            w_height = (short)(w_bottom - w_top);
        }

        static public bool touchArea(
                CGroup c1, int x1, int y1,
                CGroup c2, int x2, int y2)
        {
            if (CMath.IntersectRect(
                    x1 + c1.w_left,
                    y1 + c1.w_top,
                    x1 + c1.w_right,
                    y1 + c1.w_bottom,
                    x2 + c2.w_left,
                    y2 + c2.w_top,
                    x2 + c2.w_right,
                    y2 + c2.w_bottom))
            {
                return true;
            }
            return false;
        }



        public CCD getAllBounds()
        {
            CCD outcd = new CCD();
            outcd.X1 = w_left;
            outcd.X2 = w_right;
            outcd.Y1 = w_top;
            outcd.Y2 = w_bottom;
            return outcd;
        }


        public void setFrames(short[][] frames)
        {
            Frames = frames;
        }

        public short[][] getFrames()
        {
            return Frames;
        }

        public void setComboFrame(short[] frame, int index)
        {
            Frames[index] = frame;
        }

        public int getCount()
        {
            return Frames.Length;
        }

        public int getComboFrameCount(int index)
        {
            return Frames[index].Length;
        }
    }

    public class CCollides : CGroup
    {
        internal CCD[] cds;

        public CCollides(int cdCount)
        {
            SubCount = cdCount;
            cds = new CCD[cdCount];
        }


        private void setCD(int i, CCD cd)
        {
            if (i >= SubCount)
            {
                return;
            }
            cds[i] = cd;
            fixArea(cd.X1, cd.Y1, cd.X2, cd.Y2);
        }

        public void setCDRect(int i, int mask, float x, float y, float w, float h)
        {
            setCD(i, CCD.CreateCDRect(mask, x, y, w, h));
        }

        public void setCDLine(int i, int mask, float px, float py, float qx, float qy)
        {
            setCD(i, CCD.CreateCDLine(mask, px, py, qx, qy));
        }

        public CCD getCD(int index)
        {
            if (index < SubCount)
            {
                return cds[index];
            }
            return null;
        }

        public CCD getFrameCD(int frame, int sub)
        {
            return getCD(Frames[frame][sub]);
        }

    }

    public class CAnimates : CGroup
    {
        internal CPJAtlas tiles;

        internal int[] STileID;
        internal Trans[] SFlip;

        internal float[] SX;
        internal float[] SY;
        internal float[] SW;
        internal float[] SH;
        internal float[] SAlpha;
        internal float[] SRotate;
        internal float[] SScaleX;
        internal float[] SScaleY;

        internal bool ComplexMode;
        internal float w_scaleX = 1.0f;
        internal float w_scaleY = 1.0f;

        public CAnimates(int partCount, CPJAtlas tils)
        {
            tiles = tils;
            SubCount = partCount;

            STileID = new int[partCount];
            SFlip = new Trans[partCount];

            SW = new float[partCount];
            SH = new float[partCount];
            SX = new float[partCount];
            SY = new float[partCount];
            SAlpha = new float[partCount];
            SRotate = new float[partCount];
            SScaleX = new float[partCount];
            SScaleY = new float[partCount];
        }

        public void setPart(int SubIndex, float px, float py, int tileid, Trans trans, float alpha = 1f, float rotate = 0, float scaleX = 1f, float scaleY = 1f)
        {
            if (SubIndex >= SubCount)
            {
                return;
            }
            STileID[SubIndex] = tileid;
            SW[SubIndex] = tiles.getWidth(tileid);
            SH[SubIndex] = tiles.getHeight(tileid);
            SX[SubIndex] = px;
            SY[SubIndex] = py;
            SFlip[SubIndex] = trans;
            SAlpha[SubIndex] = alpha;
            SRotate[SubIndex] = rotate;
            SScaleX[SubIndex] = scaleX;
            SScaleY[SubIndex] = scaleY;

            switch (trans)
            {
                case Trans.TRANS_NONE:
                case Trans.TRANS_ROT180:
                case Trans.TRANS_MIRROR:
                case Trans.TRANS_MIRROR_ROT180:
                    SW[SubIndex] = tiles.getWidth(tileid);
                    SH[SubIndex] = tiles.getHeight(tileid);
                    break;
                case Trans.TRANS_ROT90:
                case Trans.TRANS_ROT270:
                case Trans.TRANS_MIRROR_ROT90:
                case Trans.TRANS_MIRROR_ROT270:
                    SW[SubIndex] = tiles.getHeight(tileid);
                    SH[SubIndex] = tiles.getWidth(tileid);
                    break;
            }
            fixArea(SX[SubIndex], SY[SubIndex],
                    SX[SubIndex] + SW[SubIndex],
                    SY[SubIndex] + SH[SubIndex]);

            if (rotate != 0 || scaleX != 1f || scaleY != 1f)
            {
                ComplexMode = true;
            }
        }



        public CPJAtlas getTiles()
        {
            return tiles;
        }

        public int getFrameTileID(int frame, int sub)
        {
            return STileID[Frames[frame][sub]];
        }

        public float getFrameX(int frame, int sub)
        {
            return SX[Frames[frame][sub]];
        }
        public float getFrameY(int frame, int sub)
        {
            return SY[Frames[frame][sub]];
        }

        public float getFrameW(int frame, int sub)
        {
            return SW[Frames[frame][sub]] * w_scaleX;
        }

        public float getFrameH(int frame, int sub)
        {
            return SH[Frames[frame][sub]] * w_scaleY;
        }

        public Trans getFrameTransform(int frame, int sub)
        {
            return SFlip[Frames[frame][sub]];
        }

        public float getFrameAlpha(int frame, int sub)
        {
            return SAlpha[Frames[frame][sub]];
        }

        public CCD getFrameBounds(int frame)
        {
            float left = float.MaxValue;
            float right = float.MinValue;
            float top = float.MaxValue;
            float bottom = float.MinValue;

            for (int i = SubCount; i >= 0; --i)
            {
                left = Math.Min(getFrameX(frame, i), left);
                right = Math.Max(getFrameX(frame, i) + getFrameW(frame, i), right);
                top = Math.Min(getFrameY(frame, i), top);
                bottom = Math.Max(getFrameY(frame, i) + getFrameH(frame, i), bottom);
            }
            CCD cd = CCD.CreateCDRect_2Point(0, left, top, right - left, bottom - top);
            return cd;
        }

        public void beginImage(Graphics g)
        {
            tiles.begin(g);
        }

        public void setScale(float scaleX, float scaleY)
        {
            if (w_scaleX != scaleX)
            {
                w_scaleX = scaleX;
            }

            if (w_scaleY != scaleY)
            {
                w_scaleY = scaleY;
            }
            ComplexMode = true;
        }

        public void addVertex(VertexBuffer vertex, int index, float dx, float dy)
        {
            if (!ComplexMode)
            {
                for (int i = Frames[index].Length - 1; i >= 0; --i)
                {
                    int idx = Frames[index][i];
                    uint rgba = Color.COLOR_WHITE.RGBA;
                    if (SAlpha[idx] != 1)
                    {
                        rgba = Color.ToRGBA(0xFFFFFF, Math.Min((int)(SAlpha[idx] * 255), 255));
                    }
                    tiles.addVertex(vertex,
                        STileID[idx],
                        SX[idx] + dx,
                        SY[idx] + dy,
                        SFlip[idx],
                        rgba);
                }
            }
            else
            {
                vertex.translate(dx, dy);
                for (int i = Frames[index].Length - 1; i >= 0; --i)
                {
                    int idx = Frames[index][i];
                    float tx = SW[idx] / 2.0f - SW[idx] * (1 - w_scaleX);
                    float ty = SH[idx] / 2.0f - SH[idx] * (1 - w_scaleY);
                    uint rgba = Color.COLOR_WHITE.RGBA;
                    if (SAlpha[idx] != 1)
                    {
                        rgba = Color.ToRGBA(0xFFFFFF, Math.Min((int)(SAlpha[idx] * 255), 255));
                    }
                    vertex.pushTransform();
                    vertex.translate(SX[idx] + tx, SY[idx] + ty);
                    vertex.rotate(SRotate[idx]);
                    vertex.scale(SScaleX[idx] * w_scaleX, SScaleY[idx] * w_scaleY);
                    tiles.addVertex(vertex, STileID[idx], -tx, -ty, SFlip[idx], rgba);
                    vertex.popTransform();
                }
                vertex.translate(-dx, -dy);
            }
        }

        public void render(Graphics g, int index, float dx = 0, float dy = 0)
        {
            if (!ComplexMode)
            {
                for (int i = Frames[index].Length - 1; i >= 0; --i)
                {
                    int idx = Frames[index][i];
                    g.AddAlpha(SAlpha[idx]);
                    tiles.render(g,
                        STileID[idx],
                        SX[idx] + dx,
                        SY[idx] + dy,
                        SFlip[idx]);
                }
            }
            else
            {
                g.Translate(dx, dy);
                for (int i = Frames[index].Length - 1; i >= 0; --i)
                {
                    int idx = Frames[index][i];
                    float tx = SW[idx] / 2.0f - SW[idx] * (1 - w_scaleX);
                    float ty = SH[idx] / 2.0f - SH[idx] * (1 - w_scaleY);
                    float olda = g.CurrentAlpha;
                    g.AddAlpha(SAlpha[idx]);
                    g.PushTransform();
                    g.Translate(SX[idx] + tx, SY[idx] + ty);
                    g.Rotate(SRotate[idx]);
                    g.Scale(SScaleX[idx] * w_scaleX, SScaleY[idx] * w_scaleY);
                    tiles.render(g,
                        STileID[idx],
                        -tx, -ty,
                        SFlip[idx]);
                    g.PopTransform();
                    g.SetAlpha(olda);
                }
                g.Translate(-dx, -dy);
            }
        }


    }





}
