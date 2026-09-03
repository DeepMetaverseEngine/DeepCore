using DeepCore.Geometry;
using DeepCore.XCSV;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepCore.Space
{
    public class LookRangeScroll<T> : IScrollView<T>
    {
        int vMetaX1;
        int vMetaX2;
        int vMetaY1;
        int vMetaY2;
        int vMetaZ1;
        int vMetaZ2;

        int vViewX1;
        int vViewX2;
        int vViewY1;
        int vViewY2;
        int vViewZ1;
        int vViewZ2;

        int vBuffX1;
        int vBuffX2;
        int vBuffY1;
        int vBuffY2;
        int vBuffZ1;
        int vBuffZ2;

        public LookRangeScroll(IScrollMap<T> map, Vector3 viewSize, int buffSize = 5)
            : base(map, viewSize, Math.Max(buffSize, 2))
        {

        }
        public override Location3D CurrentViewLocatoin
        {
            get => new Location3D(vViewX1, vViewY1, vViewZ1);
        }
        public override bool TryGetMapBuff(int x, int y, int z, out T data)
        {
            if (x >= vMetaX1 && x <= vMetaX2 &&
                y >= vMetaY1 && y <= vMetaY2 &&
                z >= vMetaZ1 && z <= vMetaZ2)
            {
                var mx = x - vMetaX1;
                var my = y - vMetaY1;
                var mz = z - vMetaZ1;
                int bx = cyc_buff(vBuffX1, mx, buffXCount);
                int by = cyc_buff(vBuffY1, my, buffYCount);
                int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                data = GetBuff(bx, by, bz);
                return true;
            }
            data = default(T);
            return false;
        }
        protected override void move(bool lazyInit, int x, int y, int z)
        {
            if (!lazyInit || Math.Abs(x) >= buffXCount || Math.Abs(y) >= buffYCount || Math.Abs(z) >= buffZCount)
            {
                vMetaX1 = CMath.CycDiv(vViewPos.X, gridSizeX);
                vMetaX2 = vMetaX1 + buffXCount - 1;
                vViewX1 = vMetaX1;
                vViewX2 = vViewX1 + viewXCount - 1;
                vBuffX1 = 0;
                vBuffX2 = vBuffX1 + buffXCount - 1;

                vMetaY1 = CMath.CycDiv(vViewPos.Y, gridSizeY);
                vMetaY2 = vMetaY1 + buffYCount - 1;
                vViewY1 = vMetaY1;
                vViewY2 = vViewY1 + viewYCount - 1;
                vBuffY1 = 0;
                vBuffY2 = vBuffY1 + buffYCount - 1;

                vMetaZ1 = CMath.CycDiv(vViewPos.Z, gridSizeZ);
                vMetaZ2 = vMetaZ1 + buffZCount - 1;
                vViewZ1 = vMetaZ1;
                vViewZ2 = vViewZ1 + viewZCount - 1;
                vBuffZ1 = 0;
                vBuffZ2 = vBuffZ1 + buffZCount - 1;

                for (int mx = 0; mx < buffXCount; mx++)
                {
                    int bx = cyc_buff(vBuffX1, mx, buffXCount);
                    int sx = cyc_meta(vMetaX1, mx, metaXCount);
                    for (int my = 0; my < buffYCount; my++)
                    {
                        int by = cyc_buff(vBuffY1, my, buffYCount);
                        int sy = cyc_meta(vMetaY1, my, metaYCount);
                        for (int mz = 0; mz < buffZCount; mz++)
                        {
                            int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                            int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                            TryFillBuff(sx, sy, sz, bx, by, bz);
                        }
                    }
                }
            }
            else
            {
                //----------------------------
                if (x != 0)
                {
                    vViewX1 += x;
                    vViewX2 += x;
                    if (vViewX1 < vMetaX1)
                    {
                        var px = vViewX1 - vMetaX1;
                        vMetaX1 = vViewX1;
                        vMetaX2 = vMetaX1 + (buffXCount - 1);
                        var cx = Math.Abs(px);
                        var dx = CMath.GetDirect(px);
                        vBuffX1 = cyc_buff(vBuffX1, px, buffXCount);
                        vBuffX2 = cyc_buff(vBuffX2, px, buffXCount);
                        for (int mx = 0; mx < cx && mx < buffXCount; mx++)
                        {
                            int bx = cyc_buff(vBuffX1, mx, buffXCount);
                            int sx = cyc_meta(vMetaX1, mx, metaXCount);
                            for (int my = 0; my < buffYCount; my++)
                            {
                                int by = cyc_buff(vBuffY1, my, buffYCount);
                                int sy = cyc_meta(vMetaY1, my, metaYCount);
                                for (int mz = 0; mz < buffZCount; mz++)
                                {
                                    int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                                    int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                    else if (vViewX2 > vMetaX2)
                    {
                        var px = vViewX2 - vMetaX2;
                        vMetaX2 = vViewX2;
                        vMetaX1 = vMetaX2 - (buffXCount - 1);
                        var cx = Math.Abs(px);
                        var dx = CMath.GetDirect(px);
                        vBuffX1 = cyc_buff(vBuffX1, px, buffXCount);
                        vBuffX2 = cyc_buff(vBuffX2, px, buffXCount);
                        for (int mx = 0; mx < cx && mx < buffXCount; mx++)
                        {
                            int bx = cyc_buff(vBuffX2, -mx, buffXCount);
                            int sx = cyc_meta(vMetaX2, -mx, metaXCount);
                            for (int my = 0; my < buffYCount; my++)
                            {
                                int by = cyc_buff(vBuffY1, my, buffYCount);
                                int sy = cyc_meta(vMetaY1, my, metaYCount);
                                for (int mz = 0; mz < buffZCount; mz++)
                                {
                                    int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                                    int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                }
                //----------------------------
                if (y != 0)
                {
                    vViewY1 += y;
                    vViewY2 += y;
                    if (vViewY1 < vMetaY1)
                    {
                        var py = vViewY1 - vMetaY1;
                        vMetaY1 = vViewY1;
                        vMetaY2 = vMetaY1 + (buffYCount - 1);
                        var cy = Math.Abs(py);
                        var dy = CMath.GetDirect(py);
                        vBuffY1 = cyc_buff(vBuffY1, py, buffYCount);
                        vBuffY2 = cyc_buff(vBuffY2, py, buffYCount);
                        for (int my = 0; my < cy && my < buffYCount; my++)
                        {
                            int by = cyc_buff(vBuffY1, my, buffYCount);
                            int sy = cyc_meta(vMetaY1, my, metaYCount);
                            for (int mx = 0; mx < buffXCount; mx++)
                            {
                                int bx = cyc_buff(vBuffX1, mx, buffXCount);
                                int sx = cyc_meta(vMetaX1, mx, metaXCount);
                                for (int mz = 0; mz < buffZCount; mz++)
                                {
                                    int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                                    int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                    else if (vViewY2 > vMetaY2)
                    {
                        var py = vViewY2 - vMetaY2;
                        vMetaY2 = vViewY2;
                        vMetaY1 = vMetaY2 - (buffYCount - 1);
                        var cy = Math.Abs(py);
                        var dy = CMath.GetDirect(py);
                        vBuffY1 = cyc_buff(vBuffY1, py, buffYCount);
                        vBuffY2 = cyc_buff(vBuffY2, py, buffYCount);
                        for (int my = 0; my < cy && my < buffYCount; my++)
                        {
                            int by = cyc_buff(vBuffY2, -my, buffYCount);
                            int sy = cyc_meta(vMetaY2, -my, metaYCount);
                            for (int mx = 0; mx < buffXCount; mx++)
                            {
                                int bx = cyc_buff(vBuffX1, mx, buffXCount);
                                int sx = cyc_meta(vMetaX1, mx, metaXCount);
                                for (int mz = 0; mz < buffZCount; mz++)
                                {
                                    int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                                    int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                }
                //----------------------------
                if (z != 0)
                {
                    vViewZ1 += z;
                    vViewZ2 += z;
                    if (vViewZ1 < vMetaZ1)
                    {
                        var pz = vViewZ1 - vMetaZ1;
                        vMetaZ1 = vViewZ1;
                        vMetaZ2 = vMetaZ1 + (buffZCount - 1);
                        var cz = Math.Abs(pz);
                        var dz = CMath.GetDirect(pz);
                        vBuffZ1 = cyc_buff(vBuffZ1, pz, buffZCount);
                        vBuffZ2 = cyc_buff(vBuffZ2, pz, buffZCount);
                        for (int mz = 0; mz < cz && mz < buffZCount; mz++)
                        {
                            int bz = cyc_buff(vBuffZ1, mz, buffZCount);
                            int sz = cyc_meta(vMetaZ1, mz, metaZCount);
                            for (int mx = 0; mx < buffXCount; mx++)
                            {
                                int bx = cyc_buff(vBuffX1, mx, buffXCount);
                                int sx = cyc_meta(vMetaX1, mx, metaXCount);
                                for (int my = 0; my < buffYCount; my++)
                                {
                                    int by = cyc_buff(vBuffY1, my, buffYCount);
                                    int sy = cyc_meta(vMetaY1, my, metaYCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                    else if (vViewZ2 > vMetaZ2)
                    {
                        var pz = vViewZ2 - vMetaZ2;
                        vMetaZ2 = vViewZ2;
                        vMetaZ1 = vMetaZ2 - (buffZCount - 1);
                        var cz = Math.Abs(pz);
                        var dz = CMath.GetDirect(pz);
                        vBuffZ1 = cyc_buff(vBuffZ1, pz, buffZCount);
                        vBuffZ2 = cyc_buff(vBuffZ2, pz, buffZCount);
                        for (int mz = 0; mz < cz && mz < buffZCount; mz++)
                        {
                            int bz = cyc_buff(vBuffZ2, -mz, buffZCount);
                            int sz = cyc_meta(vMetaZ2, -mz, metaZCount);
                            for (int mx = 0; mx < buffXCount; mx++)
                            {
                                int bx = cyc_buff(vBuffX1, mx, buffXCount);
                                int sx = cyc_meta(vMetaX1, mx, metaXCount);
                                for (int my = 0; my < buffYCount; my++)
                                {
                                    int by = cyc_buff(vBuffY1, my, buffYCount);
                                    int sy = cyc_meta(vMetaY1, my, metaYCount);
                                    TryFillBuff(sx, sy, sz, bx, by, bz);
                                }
                            }
                        }
                    }
                }
                //----------------------------
            }
        }
    }
}
