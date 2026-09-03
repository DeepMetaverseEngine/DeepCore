using DeepCore.Geometry;
using DeepCore.XCSV;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepCore.Space
{
    public class CarmackScroll<T> : IScrollView<T>
    {
        int vMetaX;
        int vMetaY;
        int vMetaZ;
        int vBuffX;
        int vBuffY;
        int vBuffZ;

        public CarmackScroll(IScrollMap<T> map, Vector3 viewSize) : base(map, viewSize, 1)
        {
            vMetaX = 0;
            vMetaY = 0;
            vMetaZ = 0;
            vBuffX = 0;
            vBuffY = 0;
            vBuffZ = 0;
        }
        public override Location3D CurrentViewLocatoin
        {
            get => new Location3D(vMetaX, vMetaY, vMetaZ);
        }
        public override bool TryGetMapBuff(int x, int y, int z, out T data)
        {
            if (x >= vMetaX && x < vMetaX + buffXCount&&
                y >= vMetaY && y < vMetaY + buffYCount &&
                z >= vMetaZ && z < vMetaZ + buffZCount)
            {
                var mx = x - vMetaX;
                var my = y - vMetaY;
                var mz = z - vMetaZ;
                int bx = cyc_buff(vBuffX, mx, buffXCount);
                int by = cyc_buff(vBuffY, my, buffYCount);
                int bz = cyc_buff(vBuffZ, mz, buffZCount);
                data = GetBuff(bx, by, bz);
                return true;
            }
            data = default(T);
            return false;
        }

        protected override void move(bool lazyInit, int x, int y, int z)
        {
            int px = x;
            int py = y;
            int pz = z;
            var cx = Math.Abs(px);
            var cy = Math.Abs(py);
            var cz = Math.Abs(pz);
            if (!lazyInit || cx >= buffXCount || cy >= buffYCount || cz >= buffZCount)
            {
                lazyInit = true;
                vBuffX = cyc_buff(vBuffX, px, buffXCount);
                vBuffY = cyc_buff(vBuffY, py, buffYCount);
                vBuffZ = cyc_buff(vBuffZ, pz, buffZCount);
                vMetaX = cyc_meta(vMetaX, px, metaXCount);
                vMetaY = cyc_meta(vMetaY, py, metaYCount);
                vMetaZ = cyc_meta(vMetaZ, pz, metaZCount);
                for (int mx = 0; mx < buffXCount; mx++)
                {
                    int bx = cyc_buff(vBuffX, mx, buffXCount);
                    int sx = cyc_meta(vMetaX, mx, metaXCount);
                    for (int my = 0; my < buffYCount; my++)
                    {
                        int by = cyc_buff(vBuffY, my, buffYCount);
                        int sy = cyc_meta(vMetaY, my, metaYCount);
                        for (int mz = 0; mz < buffZCount; mz++)
                        {
                            int bz = cyc_buff(vBuffZ, mz, buffZCount);
                            int sz = cyc_meta(vMetaZ, mz, metaZCount);
                            TryFillBuff(sx, sy, sz, bx, by, bz);
                        }
                    }
                }
            }
            else
            {
                if (px != 0)
                {
                    var dx = CMath.GetDirect(px);
                    vBuffX = cyc_buff(vBuffX, px, buffXCount);
                    vMetaX = cyc_meta(vMetaX, px, metaXCount);
                    for (int mx = 0; mx < cx && mx < buffXCount; mx++)
                    {
                        var ox = (dx > 0 ? viewXCount - mx : -mx * dx);
                        int bx = cyc_buff(vBuffX, ox, buffXCount);
                        int sx = cyc_meta(vMetaX, ox, metaXCount);
                        for (int my = 0; my < buffYCount; my++)
                        {
                            int by = cyc_buff(vBuffY, my, buffYCount);
                            int sy = cyc_meta(vMetaY, my, metaYCount);
                            for (int mz = 0; mz < buffZCount; mz++)
                            {
                                int bz = cyc_buff(vBuffZ, mz, buffZCount);
                                int sz = cyc_meta(vMetaZ, mz, metaZCount);
                                TryFillBuff(sx, sy, sz, bx, by, bz);
                            }
                        }
                    }
                }
                if (py != 0)
                {
                    var dy = CMath.GetDirect(py);
                    vBuffY = cyc_buff(vBuffY, py, buffYCount);
                    vMetaY = cyc_meta(vMetaY, py, metaYCount);
                    for (int my = 0; my < cy && my < buffYCount; my++)
                    {
                        var oy = (dy > 0 ? viewYCount - my : -my * dy);
                        int by = cyc_buff(vBuffY, oy, buffYCount);
                        int sy = cyc_meta(vMetaY, oy, metaYCount);
                        for (int mx = 0; mx < buffXCount; mx++)
                        {
                            int bx = cyc_buff(vBuffX, mx, buffXCount);
                            int sx = cyc_meta(vMetaX, mx, metaXCount);
                            for (int mz = 0; mz < buffZCount; mz++)
                            {
                                int bz = cyc_buff(vBuffZ, mz, buffZCount);
                                int sz = cyc_meta(vMetaZ, mz, metaZCount);
                                TryFillBuff(sx, sy, sz, bx, by, bz);
                            }
                        }
                    }
                }
                if (pz != 0)
                {
                    var dz = CMath.GetDirect(pz);
                    vBuffZ = cyc_buff(vBuffZ, pz, buffZCount);
                    vMetaZ = cyc_meta(vMetaZ, pz, metaZCount);
                    for (int mz = 0; mz < cz && mz < buffZCount; mz++)
                    {
                        var oz = (dz > 0 ? viewZCount - mz : -mz * dz);
                        int bz = cyc_buff(vBuffZ, oz, buffZCount);
                        int sz = cyc_meta(vMetaZ, oz, metaZCount);
                        for (int mx = 0; mx < buffXCount; mx++)
                        {
                            int bx = cyc_buff(vBuffX, mx, buffXCount);
                            int sx = cyc_meta(vMetaX, mx, metaXCount);
                            for (int my = 0; my < buffYCount; my++)
                            {
                                int by = cyc_buff(vBuffY, my, buffYCount);
                                int sy = cyc_meta(vMetaY, my, metaYCount);
                                TryFillBuff(sx, sy, sz, bx, by, bz);
                            }
                        }
                    }
                }
            }
        }


    }
}
