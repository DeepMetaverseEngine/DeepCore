using DeepCore.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore
{
    /**
     * util math function none float
     * 
     * @author yifeizhang
     * @since 2006-12-1 
     * @version 1.0
     */
    public static class CMath
    {


        public static int Sqrt(int i)
        {
            int l = 0;
            for (int k = 0x100000; k != 0; k >>= 2)
            {
                int j = l + k;
                l >>= 1;
                if (j <= i)
                {
                    i -= j;
                    l += k;
                }
            }
            return l;
        }

        public static float Min(float a, float b, params float[] values)
        {
            var ret = Math.Min(a, b);
            foreach (var v in values)
            {
                ret = Math.Min(ret, v);
            }
            return ret;
        }
        public static float Max(float a, float b, params float[] values)
        {
            var ret = Math.Max(a, b);
            foreach (var v in values)
            {
                ret = Math.Max(ret, v);
            }
            return ret;
        }
        public static int Min(int a, int b, params int[] values)
        {
            var ret = Math.Min(a, b);
            foreach (var v in values)
            {
                ret = Math.Min(ret, v);
            }
            return ret;
        }
        public static int Max(int a, int b, params int[] values)
        {
            var ret = Math.Max(a, b);
            foreach (var v in values)
            {
                ret = Math.Max(ret, v);
            }
            return ret;
        }
        public static double Min(double a, double b, params double[] values)
        {
            var ret = Math.Min(a, b);
            foreach (var v in values)
            {
                ret = Math.Min(ret, v);
            }
            return ret;
        }
        public static double Max(double a, double b, params double[] values)
        {
            var ret = Math.Max(a, b);
            foreach (var v in values)
            {
                ret = Math.Max(ret, v);
            }
            return ret;
        }


        public static bool MinMax(int a, int b, out int min, out int max)
        {
            if (a < b)
            {
                min = a;
                max = b;
                return false;
            }
            if (a > b)
            {
                min = b;
                max = a;
                return true;
            }
            min = a;
            max = b;
            return false;
        }
        public static bool MinMax(float a, float b, out float min, out float max)
        {
            if (a < b)
            {
                min = a;
                max = b;
                return false;
            }
            if (a > b)
            {
                min = b;
                max = a;
                return true;
            }
            min = a;
            max = b;
            return false;
        }
        public static bool MinMax<T>(T a, T b, out T min, out T max) where T : IComparable
        {
            if (a.CompareTo(b) < 0)
            {
                min = a;
                max = b;
                return false;
            }
            if (a.CompareTo(b) > 0)
            {
                min = b;
                max = a;
                return true;
            }
            min = a;
            max = b;
            return false;
        }

        public static void NormalRect(float sx, float sy, float dx, float dy, out float x1, out float y1, out float x2, out float y2)
        {
            CMath.MinMax(sx, dx, out x1, out x2);
            CMath.MinMax(sy, dy, out y1, out y2);
        }
        public static void NormalRect(int sx, int sy, int dx, int dy, out int x1, out int y1, out int x2, out int y2)
        {
            CMath.MinMax(sx, dx, out x1, out x2);
            CMath.MinMax(sy, dy, out y1, out y2);
        }

        // 	--------------------------------------------------------------------------------------------------------



        /// <summary>
        /// compute cyc number: (value+d) within 0~max scope
        /// </summary>
        /// <param name="value"></param>
        /// <param name="d"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        static public int CycNum(int value, int d, int max)
        {
            value += d;
            return (value >= 0) ? (value % max) : ((max + value % max) % max);
        }
        /// <summary>
        /// compute cyc number: (value+d) within 0~max scope
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        static public int CycNum(int value, int max)
        {
            return (value >= 0) ? (value % max) : ((max + value % max) % max);
        }

        /// <summary>
        /// compute cyc mod: -1 mod 10 = -1
        /// </summary>
        /// <param name="value"></param>
        /// <param name="div"></param>
        /// <returns></returns>
        static public int CycDiv(int value, int div)
        {
            return (value / div) + (value < 0 ? (value % div == 0 ? 0 : -1) : 0);
        }
        static public int CycDiv(float value, float div)
        {
            return ((int)(value / div) + (value < 0 ? (value % div == 0 ? 0 : -1) : 0));
        }

        public static int AlignTo(int value, int grid)
        {
            return value / grid * grid;
        }
        public static float AlignTo(float value, float grid)
        {
            return ((int)(value / grid)) * grid;
        }
        public static int AlignToCenter(int value, int grid)
        {
            return value / grid * grid + (grid / 2);
        }
        public static float AlignToCenter(float value, float grid)
        {
            return ((int)(value / grid)) * grid + (grid / 2);
        }

        /// <summary>
        /// 获得符号
        /// </summary>
        /// <param name="value"></param>
        /// <returns>1 or 0 or -1</returns>
        static public int GetDirect(int value)
        {
            return value == 0 ? 0 : (value > 0 ? 1 : -1);
        }
        /// <summary>
        /// 获得符号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public int GetDirect(float value)
        {
            return value == 0 ? 0 : (value > 0 ? 1 : -1);
        }
        static public int GetDirect(long value)
        {
            return value == 0 ? 0 : (value > 0 ? 1 : -1);
        }
        /// <summary>
        /// comput round mod roundMode(33,8) = 5 => 33/8 + (33%8==0?:0:1)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="div"></param>
        /// <returns></returns>
        static public int RoundMod(int value, int div)
        {
            return (value / div) + (value % div == 0 ? 0 : (1 * GetDirect(value)));
        }
        static public long RoundMod(long value, long div)
        {
            return (value / div) + (value % div == 0 ? 0 : (1 * GetDirect(value)));
        }
        /// <summary>
        /// comput round mod roundMode(33,8) = 5 => 33/8 + (33%8==0?:0:1)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="div"></param>
        /// <returns></returns>
        static public int RoundMod(float value, float div)
        {
            return (int)(value / div) + (value % div == 0 ? 0 : (1 * GetDirect((int)value)));
        }
        /// <summary>
        /// 根据速度和时间段得到距离
        /// </summary>
        /// <param name="speed">速度 (距离/秒)</param>
        /// <param name="interval_ms">毫秒</param>
        /// <returns></returns>
        static public float GetDistanceSpeedInTime(float speed, float interval_ms)
        {
            float rate = interval_ms / 1000.0f;
            return speed * rate;
        }
        public static float GetSpeedDistance(float timeMS, float speedSEC)
        {
            return speedSEC * timeMS / 1000f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static float GetDistance(float x1, float y1, float x2, float y2)
        {
            //             float r1 = x1 - x2;
            //             float r2 = y1 - y2;
            //             return (float)Math.Sqrt(r1 * r1 + r2 * r2);
            return Geometry.Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
        }
        public static float GetDistance(float dx, float dy)
        {
            //             float r1 = x1 - x2;
            //             float r2 = y1 - y2;
            //             return (float)Math.Sqrt(r1 * r1 + r2 * r2);
            return System.Numerics.Vector2.Distance(System.Numerics.Vector2.Zero, new System.Numerics.Vector2(dx, dy));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static float GetDistanceSquare(float x1, float y1, float x2, float y2)
        {
            //             float r1 = x1 - x2;
            //             float r2 = y1 - y2;
            //             return r1 * r1 + r2 * r2;
            return Geometry.Vector2.DistanceSquared(new Vector2(x1, y1), new Vector2(x2, y2));
        }

        /// <summary>
        /// 得到弧度
        /// </summary>
        /// <param name="dx">x向量</param>
        /// <param name="dy">y向量</param>
        /// <returns></returns>
        public static float GetDegree(float dx, float dy)
        {
            return (float)Math.Atan2(dy, dx);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static float GetDegree(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Atan2(y2 - y1, x2 - x1);
        }
        /// <summary>
        ///      p2
        /// p1∠ p3
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <returns></returns>
        public static float GetDegree(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var d1 = (float)Math.Atan2(y2 - y1, x2 - x1);
            var d2 = (float)Math.Atan2(y3 - y1, x3 - x1);
            return d2 - d1;
        }


        //	-------------------------------------------------------------------------------------------------------------------
        #region _Geometry_Collision_Include_

        /// <summary>
        /// 点和球体相交
        /// </summary>
        /// <param name="px">点</param>
        /// <param name="py">点</param>
        /// <param name="pz">点</param>
        /// <param name="sx">球</param>
        /// <param name="sy">球</param>
        /// <param name="sz">球</param>
        /// <param name="sr">球</param>
        /// <returns></returns>
        static public bool IncludeSphere(float px, float py, float pz, float sx, float sy, float sz, float sr)
        {
            // we are using multiplications because is faster than calling Math.pow
            //             var distance = ((px - sx) * (px - sx) + (py - sy) * (py - sy) + (pz - sz) * (pz - sz));
            //             return distance < sr * sr;
            return Geometry.Vector3.DistanceSquared(new Vector3(px, py, pz), new Vector3(sx, sy, sz)) <= sr * sr;
        }
        /// <summary>
        /// 扇形和圆相交
        /// </summary>
        /// <param name="sx">扇形圆心</param>
        /// <param name="sy">扇形圆心</param>
        /// <param name="sr">扇形半径</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <param name="startAngle">扇形起始角度</param>
        /// <param name="endAngle">扇形结束角度</param>
        /// <returns></returns>
        static public bool IncludeFanPoint(float sx, float sy, float sr, float dx, float dy, float startAngle, float endAngle)
        {
            float ddx = dx - sx;
            float ddy = dy - sy;
            float r = sr;
            if (ddx * ddx + ddy * ddy <= r * r)
            {
                float direction = OpitimizeRadians((float)Math.Atan2(ddy, ddx));
                startAngle = OpitimizeRadians(startAngle);
                endAngle = OpitimizeRadians(endAngle);
                if (endAngle < startAngle)
                {
                    if (direction < endAngle)
                    {
                        direction += RADIANS_360;
                    }
                    endAngle += RADIANS_360;
                }
                if (direction >= startAngle && direction <= endAngle)
                {
                    return true;
                }
            }
            return false;
        }

        //	--------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sx">圆x</param>
        /// <param name="sy">圆y</param>
        /// <param name="sr">圆r</param>
        /// <param name="px">点</param>
        /// <param name="py">点</param>
        /// <returns></returns>
        static public bool IncludeRoundPoint(float sx, float sy, float sr, float px, float py)
        {
            return Geometry.Vector2.DistanceSquared(new Vector2(sx, sy), new Vector2(px, py)) <= (sr * sr);
        }
        static public bool IncludeRoundRect(float sx, float sy, float sr, float dx1, float dy1, float dx2, float dy2)
        {
            var w = sx - dx1;
            var h = sy - dy1;
            var srq = sr * sr;
            if ((w * w + h * h) <= srq == false) return false;
            w = sx - dx2;
            h = sy - dy2;
            if ((w * w + h * h) <= srq == false) return false;
            w = sx - dx1;
            h = sy - dy2;
            if ((w * w + h * h) <= srq == false) return false;
            w = sx - dx2;
            h = sy - dy1;
            if ((w * w + h * h) <= srq == false) return false;
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sx">圆1x</param>
        /// <param name="sy">圆1y</param>
        /// <param name="sr">圆1r</param>
        /// <param name="dx">圆2x</param>
        /// <param name="dy">圆2y</param>
        /// <param name="dr">圆2r</param>
        /// <returns></returns>
        static public bool IncludeRoundRound(float sx, float sy, float sr, float dx, float dy, float dr)
        {
            float r = sr + dr;
            return Geometry.Vector2.DistanceSquared(new Vector2(sx, sy), new Vector2(dx, dy)) <= (r * r);
        }

        //	--------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sx1">矩形x1</param>
        /// <param name="sy1">矩形y1</param>
        /// <param name="sx2">矩形x2</param>
        /// <param name="sy2">矩形y2</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <returns></returns>
        static public bool IncludeRectPoint(float sx1, float sy1, float sx2, float sy2, float dx, float dy)
        {
            if (sx2 < dx) return false;
            if (sx1 > dx) return false;
            if (sy2 < dy) return false;
            if (sy1 > dy) return false;
            return true;
        }
        static public bool IncludeRectRect(float sx1, float sy1, float sx2, float sy2, float dx1, float dy1, float dx2, float dy2)
        {
            if (sx2 < dx1) return false;
            if (sx1 > dx1) return false;
            if (sy2 < dy1) return false;
            if (sy1 > dy1) return false;
            if (sx2 < dx2) return false;
            if (sx1 > dx2) return false;
            if (sy2 < dy2) return false;
            if (sy1 > dy2) return false;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sx1">矩形x</param>
        /// <param name="sy1">矩形y</param>
        /// <param name="sw">矩形w</param>
        /// <param name="sh">矩形h</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <returns></returns>
        static public bool IncludeRectPointW(float sx1, float sy1, float sw, float sh, float dx, float dy)
        {
            float sx2 = sx1 + sw;
            if (sx2 < dx) return false;
            if (sx1 > dx) return false;
            float sy2 = sy1 + sh;
            if (sy2 < dy) return false;
            if (sy1 > dy) return false;
            return true;
        }
        static public bool IncludeRectRectW(float sx1, float sy1, float sw, float sh, float dx1, float dy1, float dw, float dh)
        {
            float sx2 = sx1 + sw;
            if (sx2 < dx1) return false;
            if (sx1 > dx1) return false;
            float sy2 = sy1 + sh;
            if (sy2 < dy1) return false;
            if (sy1 > dy1) return false;
            float dx2 = dx1 + dw;
            if (sx2 < dx2) return false;
            if (sx1 > dx2) return false;
            float dy2 = dy1 + dh;
            if (sy2 < dy2) return false;
            if (sy1 > dy2) return false;
            return true;
        }

        /// <summary>
        /// 点在椭圆内
        /// </summary>
        /// <param name="x0">椭圆圆心x   </param>
        /// <param name="y0">椭圆圆心y  </param>
        /// <param name="rx">椭圆x轴半径</param>
        /// <param name="ry">椭圆y轴半径</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <returns></returns>
        public static bool IncludeEllipsePoint(float x0, float y0, float rx, float ry, float dx, float dy)
        {
            return IntersectRectEllipse(dx, dy, dx, dy, x0, y0, rx, ry);
        }
        public static bool IncludeEllipseRect(float x0, float y0, float rx, float ry, float dx1, float dy1, float dx2, float dy2)
        {
            if (IntersectRectEllipse(dx1, dy1, dx1, dy1, x0, y0, rx, ry) == false) return false;
            if (IntersectRectEllipse(dx2, dy1, dx2, dy1, x0, y0, rx, ry) == false) return false;
            if (IntersectRectEllipse(dx1, dy2, dx1, dy2, x0, y0, rx, ry) == false) return false;
            if (IntersectRectEllipse(dx2, dy2, dx2, dy2, x0, y0, rx, ry) == false) return false;
            return true;
        }

        /// <summary>
        /// 点在粗线条内
        /// </summary>
        /// <param name="lx1">线段X1</param>
        /// <param name="ly1">线段Y1</param>
        /// <param name="lx2">线段X2</param>
        /// <param name="ly2">线段Y2</param>
        /// <param name="line_r">线段半径(粗度的一半)</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <returns></returns>
        public static bool IncludeStripWidthPoint(
          float lx1, float ly1,
          float lx2, float ly2,
          float line_r,
          float dx, float dy)
        {
            Span<Vector2> list = stackalloc Vector2[4];
            ToStripWidthPolygon(list, lx1, ly1, lx2, ly2, line_r);
            return IncludePolygonPoint(list, dx, dy);
        }

        /// <summary>
        /// 点在多边形内
        /// </summary>
        /// <param name="list">多边形</param>
        /// <param name="dx">点</param>
        /// <param name="dy">点</param>
        /// <returns></returns>
        public static bool IncludePolygonPoint(ReadOnlySpan<Vector2> list, float dx, float dy)
        {
            return DeepCore.Geometry.CollisionMath.PointInPolygon(new Vector2(dx, dy), list);
        }

        public static bool IncludePolygonRect(ReadOnlySpan<Vector2> list, float dx1, float dy1, float dx2, float dy2)
        {
            if (IncludePolygonPoint(list, dx1, dy1) == false) return false;
            if (IncludePolygonPoint(list, dx2, dy1) == false) return false;
            if (IncludePolygonPoint(list, dx1, dy2) == false) return false;
            if (IncludePolygonPoint(list, dx2, dy2) == false) return false;
            return true;
        }

        #endregion
        //	--------------------------------------------------------------------------------------------------
        #region _Geometry_Collision_Intersect_

        /// <summary>
        /// 球和球相交
        /// </summary>
        /// <param name="sx">球1</param>
        /// <param name="sy">球1</param>
        /// <param name="sz">球1</param>
        /// <param name="sr">球1</param>
        /// <param name="ox">球2</param>
        /// <param name="oy">球2</param>
        /// <param name="oz">球2</param>
        /// <param name="or">球2</param>
        /// <returns></returns>
        static public bool IntersectSphere(float sx, float sy, float sz, float sr, float ox, float oy, float oz, float or)
        {
            var r = sr + or;
            return Geometry.Vector3.DistanceSquared(new Vector3(ox, oy, oz), new Vector3(sx, sy, sz)) <= (r * r);
            //             // we are using multiplications because it's faster than calling Math.pow
            //             var distance = ((sx - ox) * (sx - ox) + (sy - oy) * (sy - oy) + (sz - oz) * (sz - oz));
            //             return distance < (sr * sr + or * or);
        }

        /// <summary>
        /// 两圆相交
        /// </summary>
        /// <param name="sx">圆1x</param>
        /// <param name="sy">圆1y</param>
        /// <param name="sr">圆1r</param>
        /// <param name="dx">圆2x</param>
        /// <param name="dy">圆2y</param>
        /// <param name="dr">圆2r</param>
        /// <returns></returns>
        static public bool IntersectRound(
                    float sx, float sy, float sr,
                    float dx, float dy, float dr)
        {
            float r = sr + dr;
            return Geometry.Vector2.DistanceSquared(new Vector2(sx, sy), new Vector2(dx, dy)) <= (r * r);
            //             float w = sx - dx;
            //             float h = sy - dy;
            //             float r = sr + dr;
            //             return (w * w + h * h) <= (r * r);
        }

        /// <summary>
        /// 扇形和圆相交
        /// </summary>
        /// <param name="sx">扇形圆心</param>
        /// <param name="sy">扇形圆心</param>
        /// <param name="sr">扇形半径</param>
        /// <param name="dx">圆形中心</param>
        /// <param name="dy">圆形中心</param>
        /// <param name="dr">圆形半径</param>
        /// <param name="startAngle">扇形起始角(弧度)</param>
        /// <param name="endAngle">扇形结束角(弧度)</param>
        /// <returns></returns>
        static public bool IntersectFanRound(
               float sx, float sy, float sr,
               float dx, float dy, float dr,
               float startAngle, float endAngle)
        {
            float ddx = dx - sx;
            float ddy = dy - sy;
            float r = sr + dr;
            if (ddx * ddx + ddy * ddy <= r * r)
            {
                float direction = OpitimizeRadians((float)Math.Atan2(ddy, ddx));
                startAngle = OpitimizeRadians(startAngle);
                endAngle = OpitimizeRadians(endAngle);
                if (endAngle < startAngle)
                {
                    if (direction < endAngle)
                    {
                        direction += RADIANS_360;
                    }
                    endAngle += RADIANS_360;
                }
                if (direction >= startAngle && direction <= endAngle)
                {
                    return true;
                }
                if (IntersectLineRound(sx, sy,
                    sx + (float)Math.Cos(startAngle) * sr,
                    sy + (float)Math.Sin(startAngle) * sr,
                    dx, dy, dr))
                {
                    return true;
                }
                if (IntersectLineRound(sx, sy,
                    sx + (float)Math.Cos(endAngle) * sr,
                    sy + (float)Math.Sin(endAngle) * sr,
                    dx, dy, dr))
                {
                    return true;
                }
            }
            return false;
        }

        //	--------------------------------------------------------------------------------------------------
        /// <summary>
        /// 两矩形相交
        /// </summary>
        /// <param name="sx1">矩形1</param>
        /// <param name="sy1">矩形1</param>
        /// <param name="sx2">矩形1</param>
        /// <param name="sy2">矩形1</param>
        /// <param name="dx1">矩形2</param>
        /// <param name="dy1">矩形2</param>
        /// <param name="dx2">矩形2</param>
        /// <param name="dy2">矩形2</param>
        /// <returns></returns>
        static public bool IntersectRect(
                float sx1, float sy1, float sx2, float sy2,
                float dx1, float dy1, float dx2, float dy2)
        {
            if (sx2 < dx1) return false;
            if (sx1 > dx2) return false;
            if (sy2 < dy1) return false;
            if (sy1 > dy2) return false;
            return true;
        }
        /// <summary>
        /// 两矩形相交(宽高)
        /// </summary>
        /// <param name="sx1">矩形1</param>
        /// <param name="sy1">矩形1</param>
        /// <param name="sw"> 矩形1</param>
        /// <param name="sh"> 矩形1</param>
        /// <param name="dx1">矩形2</param>
        /// <param name="dy1">矩形2</param>
        /// <param name="dw"> 矩形2</param>
        /// <param name="dh"> 矩形2</param>
        /// <returns></returns>
        static public bool IntersectRectW(
                float sx1, float sy1, float sw, float sh,
                float dx1, float dy1, float dw, float dh)
        {
            float sx2 = sx1 + sw;
            float dx2 = dx1 + dw;
            if (sx2 < dx1) return false;
            if (sx1 > dx2) return false;
            float sy2 = sy1 + sh;
            float dy2 = dy1 + dh;
            if (sy2 < dy1) return false;
            if (sy1 > dy2) return false;
            return true;
        }

        //	--------------------------------------------------------------------------------------------------

        /// <summary>
        /// 线段与矩形是否相交
        /// </summary>
        /// <param name="linePointX1"></param>
        /// <param name="linePointY1"></param>
        /// <param name="linePointX2"></param>
        /// <param name="linePointY2"></param>
        /// <param name="rectangleLeftTopX"></param>
        /// <param name="rectangleLeftTopY"></param>
        /// <param name="rectangleRightBottomX"></param>
        /// <param name="rectangleRightBottomY"></param>
        /// <returns></returns>
        public static bool isLineIntersectRectangle(float linePointX1,
                                          float linePointY1,
                                        float linePointX2,
                                         float linePointY2,
                                         float rectangleLeftTopX,
                                          float rectangleLeftTopY,
                                          float rectangleRightBottomX,
                                          float rectangleRightBottomY)
        {
            float lineHeight = linePointY1 - linePointY2;
            float lineWidth = linePointX2 - linePointX1;  // 计算叉乘 
            float c = linePointX1 * linePointY2 - linePointX2 * linePointY1;
            if ((lineHeight * rectangleLeftTopX + lineWidth * rectangleLeftTopY + c >= 0 && lineHeight * rectangleRightBottomX + lineWidth * rectangleRightBottomY + c <= 0)
                || (lineHeight * rectangleLeftTopX + lineWidth * rectangleLeftTopY + c <= 0 && lineHeight * rectangleRightBottomX + lineWidth * rectangleRightBottomY + c >= 0)
                || (lineHeight * rectangleLeftTopX + lineWidth * rectangleRightBottomY + c >= 0 && lineHeight * rectangleRightBottomX + lineWidth * rectangleLeftTopY + c <= 0)
                || (lineHeight * rectangleLeftTopX + lineWidth * rectangleRightBottomY + c <= 0 && lineHeight * rectangleRightBottomX + lineWidth * rectangleLeftTopY + c >= 0))
            {

                if (rectangleLeftTopX > rectangleRightBottomX)
                {
                    float temp = rectangleLeftTopX;
                    rectangleLeftTopX = rectangleRightBottomX;
                    rectangleRightBottomX = temp;
                }
                if (rectangleLeftTopY < rectangleRightBottomY)
                {
                    float temp1 = rectangleLeftTopY;
                    rectangleLeftTopY = rectangleRightBottomY;
                    rectangleRightBottomY = temp1;
                }
                if ((linePointX1 < rectangleLeftTopX && linePointX2 < rectangleLeftTopX)
                    || (linePointX1 > rectangleRightBottomX && linePointX2 > rectangleRightBottomX)
                    || (linePointY1 > rectangleLeftTopY && linePointY2 > rectangleLeftTopY)
                    || (linePointY1 < rectangleRightBottomY && linePointY2 < rectangleRightBottomY))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// 矩形和线段相交
        /// </summary>
        /// <param name="Rx1">矩形</param>
        /// <param name="Ry1">矩形</param>
        /// <param name="Rx2">矩形</param>
        /// <param name="Ry2">矩形</param>
        /// <param name="Lx1">线段</param>
        /// <param name="Ly1">线段</param>
        /// <param name="Lx2">线段</param>
        /// <param name="Ly2">线段</param>
        /// <returns></returns>
        static public bool IntersectRectLine(
                float Rx1, float Ry1,
                float Rx2, float Ry2,
                float Lx1, float Ly1,
                float Lx2, float Ly2)
        {
            //             float dx1 = lx1;
            //             float dy1 = ly1;
            //             float dx2 = lx2;
            //             float dy2 = ly2;
            //             if (dx1 > dx2) CUtils.Swap(ref dx1, ref dx2);
            //             if (dy1 > dy2) CUtils.Swap(ref dy1, ref dy2);
            // 
            //             if (!IntersectRect(sx1, sy1, sx2, sy2, dx1, dy1, dx2, dy2))
            //             {
            //                 return false;
            //             }
            //             if (IntersectLine(lx1, ly1, lx2, ly2, sx1, sy1, sx2, sy1))
            //             {
            //                 return true;
            //             }
            //             if (IntersectLine(lx1, ly1, lx2, ly2, sx2, sy1, sx2, sy2))
            //             {
            //                 return true;
            //             }
            //             if (IntersectLine(lx1, ly1, lx2, ly2, sx2, sy2, sx1, sy2))
            //             {
            //                 return true;
            //             }
            //             if (IntersectLine(lx1, ly1, lx2, ly2, sx1, sy2, sx1, sy1))
            //             {
            //                 return true;
            //             }
            //             return false;

            float LH = Ly1 - Ly2;
            float LW = Lx2 - Lx1;  // 计算叉乘 
            float C = Lx1 * Ly2 - Lx2 * Ly1;

            var LHRx1 = LH * Rx1;
            var LWRy1 = LW * Ry1;
            var LWRy2 = LW * Ry2;
            var LHRx2 = LH * Rx2;

            var LHRx1_LWRy1_C = LHRx1 + LWRy1 + C;
            var LHRx1_LWRy2_C = LHRx1 + LWRy2 + C;
            var LHRx2_LWRy2_C = LHRx2 + LWRy2 + C;
            var LHRx2_LWRy1_C = LHRx2 + LWRy1 + C;

            if ((LHRx1_LWRy1_C >= 0 && LHRx2_LWRy2_C <= 0) ||
                (LHRx1_LWRy1_C <= 0 && LHRx2_LWRy2_C >= 0) ||
                (LHRx1_LWRy2_C >= 0 && LHRx2_LWRy1_C <= 0) ||
                (LHRx1_LWRy2_C <= 0 && LHRx2_LWRy1_C >= 0))
            {
                if (Rx1 > Rx2)
                {
                    float temp = Rx1;
                    Rx1 = Rx2;
                    Rx2 = temp;
                }
                if (Ry1 < Ry2)
                {
                    float temp1 = Ry1;
                    Ry1 = Ry2;
                    Ry2 = temp1;
                }
                if ((Lx1 < Rx1 && Lx2 < Rx1)
                    || (Lx1 > Rx2 && Lx2 > Rx2)
                    || (Ly1 > Ry1 && Ly2 > Ry1)
                    || (Ly1 < Ry2 && Ly2 < Ry2))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

        }
        static public bool IntersectRectLineW(
                float sx, float sy,
                float sw, float sh,
                float lx1, float ly1,
                float lx2, float ly2)
        {
            return IntersectRectLine(sx, sy, sx + sw, sy + sh, lx1, ly1, lx2, ly2);
        }

        /// <summary>
        /// 矩形和圆相交
        /// </summary>
        /// <param name="sx">矩形</param>
        /// <param name="sy">矩形</param>
        /// <param name="dx">矩形</param>
        /// <param name="dy">矩形</param>
        /// <param name="cx">圆形</param>
        /// <param name="cy">圆形</param>
        /// <param name="r">圆形</param>
        /// <returns></returns>
        static public bool IntersectRectRound(float sx, float sy, float dx, float dy, float cx, float cy, float r)
        {
            return IntersectRectEllipse(sx, sy, dx, dy, cx, cy, r, r);
        }
        static public bool IntersectRectRoundW(float sx, float sy, float sw, float sh, float cx, float cy, float r)
        {
            return IntersectRectEllipse(sx, sy, sx + sw, sy + sh, cx, cy, r, r);
        }



        /// <summary>
        /// 计算椭圆和矩形是否相交 
        /// (x/rx)^2 + (y/ry)^2 = 1; 
        /// left is: (x* ry)^2 + (y* rx)^2 
        /// right is: (rx* ry)^2 
        /// if(left > right) out 
        /// else in 
        /// </summary>
        /// <param name="xmin">矩形左上角x</param>
        /// <param name="ymin">矩形左上角y</param>
        /// <param name="xmax">矩形右下角x</param>
        /// <param name="ymax">矩形右下角y</param>
        /// <param name="x0">椭圆圆心x  </param>
        /// <param name="y0">椭圆圆心y  </param>
        /// <param name="rx">椭圆x轴半径</param>
        /// <param name="ry">椭圆y轴半径</param>
        /// <returns></returns>
        public static bool IntersectRectEllipse(float xmin, float ymin, float xmax, float ymax, float x0, float y0, float rx, float ry)
        {
            //如果圆心点就在矩形内部, 那么就直接返回true 
            //             if ((x0, y0, x0, y0, xmin, ymin, xmax, ymax))
            //             {
            //                 return true;
            //             }
            if (IncludeRectPoint(xmin, ymin, xmax, ymax, x0, y0))
                return true;
            //首先找到矩形距离圆心的最近点 
            float x = x0, y = y0;
            if (x < xmin)
            {
                x = xmin;
            }
            else if (x > xmax)
            {
                x = xmax;
            }
            if (y < ymin)
            {
                y = ymin;
            }
            else if (y > ymax)
            {
                y = ymax;
            }
            float dx = x - x0;
            float dy = y - y0;
            dx *= dx;
            dy *= dy;
            rx *= rx;
            ry *= ry;
            dx *= ry;
            dy *= rx;
            if (dx + dy <= rx * ry)
            {
                return true;
            }
            return false;
        }
        static public bool IntersectRectEllipseW(float sx, float sy, float sw, float sh, float cx, float cy, float rx, float ry)
        {
            return IntersectRectEllipse(sx, sy, sx + sw, sy + sh, cx, cy, rx, ry);
        }

        /// <summary>
        /// 两个线段相交
        ///  ((Q2-Q1)X(P1-Q1))*((P2-Q1)X(Q2-Q1)) >= 0 
        ///  ((P2-P1)X(Q1-P1))*((Q2-P1)X(P2-P1)) >= 0
        /// </summary>
        /// <param name="p1_x">line 1</param>
        /// <param name="p1_y">line 1</param>
        /// <param name="p2_x">line 1</param>
        /// <param name="p2_y">line 1</param>
        /// <param name="q1_x">line 2</param>
        /// <param name="q1_y">line 2</param>
        /// <param name="q2_x">line 2</param>
        /// <param name="q2_y">line 2</param>
        /// <returns></returns>
        static public bool IntersectLine(
            float p1_x, float p1_y, float p2_x, float p2_y,
            float q1_x, float q1_y, float q2_x, float q2_y)
        {
            /* croe */
            float v1_x = q2_x - q1_x;
            float v1_y = q2_y - q1_y;
            float v2_x = p1_x - q1_x;
            float v2_y = p1_y - q1_y;
            float v3_x = p2_x - q1_x;
            float v3_y = p2_y - q1_y;
            if ((v1_x * v2_y - v1_y * v2_x) * (v3_x * v1_y - v3_y * v1_x) < 0)
            {
                return false;
            }
            float v5_x = p2_x - p1_x;
            float v5_y = p2_y - p1_y;
            float v6_x = q1_x - p1_x;
            float v6_y = q1_y - p1_y;
            float v7_x = q2_x - p1_x;
            float v7_y = q2_y - p1_y;
            if ((v5_x * v6_y - v5_y * v6_x) * (v7_x * v5_y - v7_y * v5_x) < 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 两个线段相交
        ///  ((Q2-Q1)X(P1-Q1))*((P2-Q1)X(Q2-Q1)) >= 0 
        ///  ((P2-P1)X(Q1-P1))*((Q2-P1)X(P2-P1)) >= 0
        /// </summary>
        /// <param name="p1_x">line 1</param>
        /// <param name="p1_y">line 1</param>
        /// <param name="p2_x">line 1</param>
        /// <param name="p2_y">line 1</param>
        /// <param name="q1_x">line 2</param>
        /// <param name="q1_y">line 2</param>
        /// <param name="q2_x">line 2</param>
        /// <param name="q2_y">line 2</param>
        /// <param name="ignore_touch">忽略焦点在线段内</param>
        /// <returns></returns>
        static public bool IntersectLine(
           float p1_x, float p1_y, float p2_x, float p2_y,
           float q1_x, float q1_y, float q2_x, float q2_y,
           bool ignore_touch)
        {
            /* croe */
            float v1_x = q2_x - q1_x;
            float v1_y = q2_y - q1_y;
            float v2_x = p1_x - q1_x;
            float v2_y = p1_y - q1_y;
            float v3_x = p2_x - q1_x;
            float v3_y = p2_y - q1_y;
            float f = (v1_x * v2_y - v1_y * v2_x) * (v3_x * v1_y - v3_y * v1_x);
            if (ignore_touch ? f <= 0 : f < 0)
            {
                return false;
            }
            float v5_x = p2_x - p1_x;
            float v5_y = p2_y - p1_y;
            float v6_x = q1_x - p1_x;
            float v6_y = q1_y - p1_y;
            float v7_x = q2_x - p1_x;
            float v7_y = q2_y - p1_y;
            float s = (v5_x * v6_y - v5_y * v6_x) * (v7_x * v5_y - v7_y * v5_x);
            if (ignore_touch ? s <= 0 : s < 0)
            {
                return false;
            }
            return true;
        }

        static public bool IntersectLinePoint(
               float lx1, float ly1,
               float lx2, float ly2,
               float px, float py)
        {
            if (GetDistanceSquare(lx1, ly1, px, py) <= GetDistanceSquare(lx1, ly1, lx2, ly2))
            {
                return PointOnLine(lx1, ly1, lx2, ly2, px, py) == PointOnLineResult.Touch;
            }
            return false;
        }
        /// <summary>
        /// 求线段与圆碰撞
        /// </summary>
        /// <param name="lx1">线段起点</param>
        /// <param name="ly1">线段起点</param>
        /// <param name="lx2">线段终点</param>
        /// <param name="ly2">线段终点</param>
        /// <param name="cx">圆心坐标</param>
        /// <param name="cy">圆心坐标</param>
        /// <param name="Radius">半径</param>
        /// <returns>如果有交点返回true,否则返回false</returns>
        public static bool IntersectLineRound(
                        float lx1, float ly1,
                        float lx2, float ly2,
                        float cx, float cy,
                        float Radius)
        {
            return DeepCore.Geometry.CollisionMath.CircleLineCollide(new Vector2(cx, cy), Radius, new Vector2(lx1, ly1), new Vector2(lx2, ly2));
        }
        /// <summary>
        /// 圆与胶囊线段碰撞检测
        /// 性能远好于 intersetctRoundStripWidth
        /// </summary>
        /// <param name="lx1">线段</param>
        /// <param name="ly1">线段</param>
        /// <param name="lx2">线段</param>
        /// <param name="ly2">线段</param>
        /// <param name="line_r">线段半径(粗度的一半)</param>
        /// <param name="cx">圆形</param>
        /// <param name="cy">圆形</param>
        /// <param name="r">圆形</param>
        /// <returns></returns>
        public static bool IntersectRoundStripCapsule(
            float cx, float cy, float r,
            float lx1, float ly1,
            float lx2, float ly2,
            float line_r)
        {
            return DeepCore.Geometry.CollisionMath.CircleLineCollide(new Vector2(cx, cy), line_r + r, new Vector2(lx1, ly1), new Vector2(lx2, ly2));
        }

        /// <summary>
        /// 圆与线段碰撞检测
        /// 性能较差
        /// </summary>
        /// <param name="lx1">线段</param>
        /// <param name="ly1">线段</param>
        /// <param name="lx2">线段</param>
        /// <param name="ly2">线段</param>
        /// <param name="line_r">线段半径(粗度的一半)</param>
        /// <param name="cx">圆形</param>
        /// <param name="cy">圆形</param>
        /// <param name="r">圆形</param>
        /// <returns></returns>
        public static bool IntersectRoundStripWidth(
            float cx, float cy, float r,
            float lx1, float ly1,
            float lx2, float ly2,
            float line_r)
        {
            DeepCore.Geometry.Vector2 sp = new DeepCore.Geometry.Vector2(lx1, ly1);
            DeepCore.Geometry.Vector2 dp = new DeepCore.Geometry.Vector2(lx2, ly2);
            if (DeepCore.Geometry.CollisionMath.CircleLineCollide(new Vector2(cx, cy), line_r + r, sp, dp))
            {
                float angle = (float)Math.Atan2(ly1 - ly2, lx1 - lx2);
                DeepCore.Geometry.Vector2 sl = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle + CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 sr = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle - CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 dl = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle + CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 dr = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle - CMath.RADIANS_90, line_r);
                return CMath.IntersectRoundPolygon(cx, cy, r, new DeepCore.Geometry.Vector2[] { sl, sr, dr, dl, });
            }
            return false;
        }

        /// <summary>
        /// 矩形与粗线段相交
        /// </summary>
        /// <param name="sx1">矩形</param>
        /// <param name="sy1">矩形</param>
        /// <param name="sx2">矩形</param>
        /// <param name="sy2">矩形</param>
        /// <param name="lx1">线段</param>
        /// <param name="ly1">线段</param>
        /// <param name="lx2">线段</param>
        /// <param name="ly2">线段</param>
        /// <param name="line_r">线段半径(粗度的一半)</param>
        /// <returns></returns>
        public static bool IntersectRectStripWidth(
            float sx1, float sy1,
            float sx2, float sy2,
            float lx1, float ly1,
            float lx2, float ly2,
            float line_r)
        {
            CMath.MinMax(lx1, lx2, out var dx1, out var dx2);
            CMath.MinMax(ly1, ly2, out var dy1, out var dy2);
            if (CMath.IntersectRect(sx1, sy1, sx2, sy2, dx1, dy1, dx2, dy2))
            {
                DeepCore.Geometry.Vector2 sp = new DeepCore.Geometry.Vector2(lx1, ly1);
                DeepCore.Geometry.Vector2 dp = new DeepCore.Geometry.Vector2(lx2, ly2);
                float angle = (float)Math.Atan2(ly1 - ly2, lx1 - lx2);
                DeepCore.Geometry.Vector2 p0 = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle + CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 p1 = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle - CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 p2 = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle - CMath.RADIANS_90, line_r);
                DeepCore.Geometry.Vector2 p3 = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle + CMath.RADIANS_90, line_r);

                if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p0.X, p0.Y, p1.X, p1.Y)) return true;
                if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p1.X, p1.Y, p2.X, p2.Y)) return true;
                if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p2.X, p2.Y, p3.X, p3.Y)) return true;
                if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p3.X, p3.Y, p0.X, p0.Y)) return true;
            }
            return false;
        }
        public static bool IntersectRectStripWidthW(
          float sx, float sy,
          float sw, float sh,
          float lx1, float ly1,
          float lx2, float ly2,
          float line_r)
        {
            return IntersectRectStripWidth(sx, sy, sx + sw, sy + sh, lx1, ly1, lx2, ly2, line_r);
        }

        /// <summary>
        /// 圆形与多边形碰撞
        /// </summary>
        /// <param name="cx">圆形</param>
        /// <param name="cy">圆形</param>
        /// <param name="r">圆形</param>
        /// <param name="list">多边形</param>
        /// <returns></returns>
        public static bool IntersectRoundPolygon(float cx, float cy, float r, ReadOnlySpan<Vector2> list)
        {
            if (list.Length == 1)
            {
                return CMath.IncludeRoundPoint(cx, cy, r, list[0].X, list[0].Y);
            }
            Vector2 center = new Vector2(cx, cy);
            if (CollisionMath.PointInPolygon(center, list))
            {
                return true;
            }
            for (int i = 0; i < list.Length - 1; ++i)
            {
                if (CollisionMath.CircleLineCollide(center, r, list[i], list[i + 1]))
                    return true;
            }
            if (CollisionMath.CircleLineCollide(center, r, list[list.Length - 1], list[0]))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 矩形与多边形碰撞
        /// </summary>
        /// <param name="sx1">矩形</param>
        /// <param name="sy1">矩形</param>
        /// <param name="sx2">矩形</param>
        /// <param name="sy2">矩形</param>
        /// <param name="list">多边形</param>
        /// <returns></returns>
        public static bool IntersectRectPolygon(float sx1, float sy1, float sx2, float sy2, ReadOnlySpan<Vector2> list)
        {
            if (list.Length == 1)
                return CMath.IncludeRectPoint(sx1, sy1, sx2, sy2, list[0].X, list[0].Y);

            if (CollisionMath.PointInPolygon(new Vector2(
                CMath.RateToReal(0.5f, sx1, sx2),
                CMath.RateToReal(0.5f, sy1, sy2)),
                list))
                return true;

            Vector2 p = list[0];
            Vector2 q = list[list.Length - 1];
            if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p.X, p.Y, q.X, q.Y))
                return true;

            for (int i = list.Length - 2; i >= 0; --i)
            {
                p = list[i];
                q = list[i + 1];
                if (CMath.IntersectRectLine(sx1, sy1, sx2, sy2, p.X, p.Y, q.X, q.Y))
                    return true;
            }
            return false;
        }

        public static bool IntersectRectTriangle(in Vector2 min, in Vector2 max, in Vector2 t1, in Vector2 t2, in Vector2 t3)
        {
            unsafe
            {
                Span<Vector2> list = stackalloc Vector2[3];
                list[0] = t1;
                list[1] = t2;
                list[2] = t3;
                return IntersectRectPolygon(min.X, min.Y, max.X, max.Y, list);
            }
        }

        /// <summary>
        /// 获得粗线段多边形表达式
        /// </summary>
        /// <param name="lx1">线段</param>
        /// <param name="ly1">线段</param>
        /// <param name="lx2">线段</param>
        /// <param name="ly2">线段</param>
        /// <param name="line_r">线段半径(粗度的一半)</param>
        /// <returns>多边形</returns>
        public static void ToStripWidthPolygon(Span<Vector2> ret, float lx1, float ly1, float lx2, float ly2, float line_r)
        {
            DeepCore.Geometry.Vector2 sp = new DeepCore.Geometry.Vector2(lx1, ly1);
            DeepCore.Geometry.Vector2 dp = new DeepCore.Geometry.Vector2(lx2, ly2);
            float angle = (float)Math.Atan2(ly1 - ly2, lx1 - lx2);
            ret[0] = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle + CMath.RADIANS_90, line_r);
            ret[1] = DeepCore.Geometry.CollisionMath.MoveToByRadians(sp, angle - CMath.RADIANS_90, line_r);
            ret[3] = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle + CMath.RADIANS_90, line_r);
            ret[2] = DeepCore.Geometry.CollisionMath.MoveToByRadians(dp, angle - CMath.RADIANS_90, line_r);
        }
        /// <summary>
        /// 获取多边形外包框
        /// </summary>
        /// <param name="list"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void ToBoundingBox(ReadOnlySpan<Vector2> list, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = list.Length - 1; i >= 0; --i)
            {
                if (list[i].X < min.X) { min.X = list[i].X; }
                if (list[i].Y < min.Y) { min.Y = list[i].Y; }
                if (list[i].X > max.X) { max.X = list[i].X; }
                if (list[i].Y > max.Y) { max.Y = list[i].Y; }
            }
        }

        //--------------------------------------------------------------------------------------------------



        public enum PointOnLineResult : byte
        {
            Left, Right, Touch,
        }
        /// <summary>
        /// 输入三个点，并且判断第三个点在前两个点连成的直线的左边还是右边或者是在线上
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static PointOnLineResult PointOnLine(float x0, float y0, float x1, float y1, float x2, float y2)
        {
            float f = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
            if (f > 0)
                return PointOnLineResult.Right;
            if (f < 0)
                return PointOnLineResult.Left;
            return PointOnLineResult.Touch;
        }

        #endregion

        //	--------------------------------------------------------------------------------------------------
        #region _Geometry_Collision_3D_

        //---------------------------------------------------------------------------------------------

        public static bool Include3D(in VoxelCylinder shape, in Vector3 o)
        {
            return shape.Intersects(in o);
        }
        public static bool Include3D(in VoxelFan shape, in Vector3 o)
        {
            return shape.Intersects(in o);
        }
        public static bool Include3D(in BoundingBox shape, in Vector3 o)
        {
            return shape.Contains(in o) != ContainmentType.Disjoint;
        }
        public static bool Include3D(in BoundingSphere shape, in Vector3 o)
        {
            return shape.Contains(in o) != ContainmentType.Disjoint;
        }


        public static bool Intersects3D(in Geometry.Vector3 p1, in Geometry.Vector3 p2, float distance)
        {
            Vector3.DistanceSquared(in p1, in p2, out var pd);
            return pd <= distance * distance;
        }
        public static bool Intersects3D(in Geometry.Vector3 p1, float r1, in Geometry.Vector3 p2, float r2)
        {
            var distance = r1 + r2;
            Vector3.DistanceSquared(in p1, in p2, out var pd);
            return pd <= distance * distance;
        }


        #endregion
        //---------------------------------------------------------------------------------------------
        /// <summary>
        /// max 大于等于 value 并且 min 小于等于 value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsIncludeEqual(int value, int min, int max)
        {
            return max >= value && min <= value;
        }
        public static bool IsIncludeEqual(float value, float min, float max)
        {
            return max >= value && min <= value;
        }

        /// <summary>
        /// max 大于 value 并且 min 小于 value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInclude(int value, int min, int max)
        {
            return max > value && min < value;
        }
        public static bool IsInclude(float value, float min, float max)
        {
            return max > value && min < value;
        }


        /// <summary>
        /// value 大于等于 0 并且& value 小于 max
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInRange(int value, int max)
        {
            return value >= 0 && value < max;
        }
        public static bool IsInRange(float value, float max)
        {
            return value >= 0 && value < max;
        }

        static public bool IsIntersect(float sx1, float sx2, float dx1, float dx2)
        {
            if (sx2 < dx1) return false;
            if (sx1 > dx2) return false;
            return true;
        }
        static public bool IsIntersectW(float sx1, float sw, float dx1, float dw)
        {
            float sx2 = sx1 + sw;
            if (sx2 < dx1) return false;
            float dx2 = dx1 + dw;
            if (sx1 > dx2) return false;
            return true;
        }

        /// <summary>
        /// 将值定位于min和max之间
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max">包含</param>
        /// <returns></returns>
        public static float Clamp(
                float value,
                float min,
                float max)
        {
            value = Math.Min(max, value);
            value = Math.Max(min, value);
            return value;
        }

        /// <summary>
        /// 获得值在最大与最小之间的比率
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min_v"></param>
        /// <param name="max_v"></param>
        /// <returns></returns>
        public static float GetRate(float value, float min_v, float max_v)
        {
            float sw = max_v - min_v;
            float sx = value - min_v;
            if (sw == 0) return 0;
            return (sx / sw);
        }

        /// <summary>
        /// 将比率换算为实际值
        /// </summary>
        /// <param name="rate"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float RateToReal(float rate, float min, float max)
        {
            float v = max - min;
            return min + v * rate;
        }

        //	--------------------------------------------------------------------------------------------------

        public static long NextPOT(long x)
        {
            x = x - 1;
            x = x | (x >> 1);
            x = x | (x >> 2);
            x = x | (x >> 4);
            x = x | (x >> 8);
            x = x | (x >> 16);
            return x + 1;
        }




        public const float PI_F = (float)(Math.PI);
        public const float PI_FLOAT = (float)(Math.PI);
        public const float PI_DIV_180 = (float)(Math.PI / 180);
        public const float PI_DIV_2 = (float)(Math.PI / 2);
        public const float PI_MUL_2 = (float)(Math.PI * 2);
        public const float RADIANS_45 = (float)(Math.PI / 4);
        public const float RADIANS_90 = (float)(Math.PI / 2);
        public const float RADIANS_180 = (float)(Math.PI);
        public const float RADIANS_270 = (float)(Math.PI + Math.PI / 2);
        public const float RADIANS_360 = (float)(Math.PI * 2);
        public const float ANGLES_45 = 45f;
        public const float ANGLES_90 = 90f;
        public const float ANGLES_180 = 180f;
        public const float ANGLES_270 = 270f;
        public const float ANGLES_360 = 360f;

        public const float Angle2Radian = PI_DIV_180;
        public const float Radian2Angle = 1f / PI_DIV_180;

        /// <summary>
        /// 180 -> PI
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float AngleToRadian(this float angle)
        {
            return (angle * PI_DIV_180);
        }
        /// <summary>
        /// PI -> 180
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static float RadianToAngle(this float degree)
        {
            return (degree / PI_DIV_180);
        }
        public static float ToPI(float angle)
        {
            return (angle * PI_DIV_180);
        }
        public static float To360(float degree)
        {
            return (degree / PI_DIV_180);
        }



        /// <summary>
        /// 将角度控制在0~360度范围内
        /// </summary>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static float OpitimizeAngles(float angles)
        {
            while (angles > 360)
            {
                angles -= 360;
            }
            while (angles < 0)
            {
                angles += 360;
            }
            return angles;
        }

        /// <summary>
        /// 将弧度控制在0~2PI度范围内
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static float OpitimizeRadians(float radians)
        {
            while (radians > RADIANS_360)
            {
                radians -= RADIANS_360;
            }
            while (radians < 0)
            {
                radians += RADIANS_360;
            }
            return radians;
        }

        /// <summary>
        /// 判断角度是否在范围内
        /// </summary>
        /// <param name="radians"></param>
        /// <param name="startRadians"></param>
        /// <param name="lengthRadians"></param>
        /// <returns></returns>
        public static bool AnglesInRange(float angles, float startAngles, float lengthAngles)
        {
            startAngles = OpitimizeAngles(startAngles) + ANGLES_360;
            angles = CMath.OpitimizeAngles(angles) + ANGLES_360;
            float endAngle = startAngles + lengthAngles;
            if (angles >= startAngles && angles <= endAngle)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断弧度是否在范围内
        /// </summary>
        /// <param name="radians"></param>
        /// <param name="startRadians"></param>
        /// <param name="lengthRadians"></param>
        /// <returns></returns>
        public static bool RadiansInRange(float radians, float startRadians, float lengthRadians)
        {
            startRadians = OpitimizeRadians(startRadians) + RADIANS_360;
            radians = CMath.OpitimizeRadians(radians) + RADIANS_360;
            float endAngle = startRadians + lengthRadians;
            if (radians >= startRadians && radians <= endAngle)
            {
                return true;
            }
            return false;
        }

        public static float RadiansDistance(float radians, float startRadians)
        {
            startRadians = OpitimizeRadians(startRadians) + RADIANS_360;
            radians = CMath.OpitimizeRadians(radians) + RADIANS_360;
            return Math.Min(startRadians - radians, radians - startRadians);
        }

        /// <summary>
        /// 获取两个角度间的最小距离
        /// </summary>
        /// <param name="srcRadian"></param>
        /// <param name="dstRadian"></param>
        /// <returns></returns>
        public static float GetMinRadians(float srcRadian, float dstRadian)
        {
            float dd = CMath.OpitimizeRadians(srcRadian);
            float cd = CMath.OpitimizeRadians(dstRadian);
            float d1 = Math.Abs(dd - cd);
            if (d1 > PI_F)
            {
                return RADIANS_360 - d1;
            }
            return d1;
        }


        //--------------------------------------------------------------------------------------------------

        public static void RandomPosInRound(this Random rand, float x, float y, float r, out float out_x, out float out_y)
        {
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            float distance = (float)rand.NextDouble() * r;
            out_x = x + (float)Math.Cos(angle) * distance;
            out_y = y + (float)Math.Sin(angle) * distance;
        }
        public static void RandomPosInCycle(this Random rand, float x, float y, float r, out float out_x, out float out_y)
        {
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            out_x = x + (float)Math.Cos(angle) * r;
            out_y = y + (float)Math.Sin(angle) * r;
        }
        public static void RandomPosInRound(this Random rand, Geometry.Vector2 v, float r, out Geometry.Vector2 out_v)
        {
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            float distance = (float)rand.NextDouble() * r;
            out_v.Value.X = v.X + (float)Math.Cos(angle) * distance;
            out_v.Value.Y = v.Y + (float)Math.Sin(angle) * distance;
        }
        public static void RandomPosInCycle(this Random rand, Geometry.Vector2 v, float r, out Geometry.Vector2 out_v)
        {
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            out_v.Value.X = v.X + (float)Math.Cos(angle) * r;
            out_v.Value.Y = v.Y + (float)Math.Sin(angle) * r;
        }
        public static void RandomPosInRound(this Random rand, Geometry.Vector3 v, float r, out Geometry.Vector3 out_v)
        {
            out_v = v;
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            float distance = (float)rand.NextDouble() * r;
            out_v.Value.X = v.X + (float)Math.Cos(angle) * distance;
            out_v.Value.Y = v.Y + (float)Math.Sin(angle) * distance;
        }
        public static void RandomPosInCycle(this Random rand, Geometry.Vector3 v, float r, out Geometry.Vector3 out_v)
        {
            out_v = v;
            float angle = (float)rand.NextDouble() * CMath.RADIANS_360;
            out_v.Value.X = v.X + (float)Math.Cos(angle) * r;
            out_v.Value.Y = v.Y + (float)Math.Sin(angle) * r;
        }
        public static float RandomRadians(this Random rand)
        {
            return rand.NextFloat() * CMath.RADIANS_360;
        }
        /// <summary>
        /// + - angle
        /// </summary>
        /// <param name="rand"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float RandomRadians(this Random rand, float angle)
        {
            return rand.NextFloat() * angle - angle / 2f;
        }

        //给定三点，求夹角的角度
        public static float GetIncludedAngle(Geometry.Vector3 posCenter, Geometry.Vector3 posA, Geometry.Vector3 posB)
        {
            var ca_x = posA.X - posCenter.X;
            var ca_y = posA.Y - posCenter.Y;
            var cb_x = posB.X - posCenter.X;
            var cb_y = posB.Y - posCenter.Y;
            var v1 = ca_x * cb_x + ca_y * cb_y;
            var ca_val = Math.Sqrt(ca_x * ca_x + ca_y * ca_y);
            var cb_val = Math.Sqrt(cb_x * cb_x + cb_y * cb_y);
            var cosC = v1 / (ca_val * cb_val);
            var angleACB = Math.Acos(cosC) * 180 / Math.PI;
            return (float)angleACB;
        }

        /// <summary>
        /// 得到射线A需要转动多少角度才可以和点B重叠
        /// </summary>
        /// <param name="posA">射线端点</param>
        /// <param name="directA">射线的方向，弧度值</param>
        /// <param name="posB">点B</param>
        /// <returns>角度值，[0,180]</returns>
        public static float GetIncludedAngle(Geometry.Vector3 posA, float directA, Geometry.Vector3 posB)
        {
            var direct2 = GetDirection(posA, posB);
            var v = Math.Abs(directA - direct2);
            if (v > PI_F)
            {
                v = PI_MUL_2 - v;
            }
            v = (v / PI_F) * 180;
            return v;
        }

        /// <summary>
        /// 返回一点到另一点的方向
        /// </summary>
        /// <param name="posPoint"></param>
        /// <param name="posTarget"></param>
        /// <returns>弧度值</returns>
        public static float GetDirection(Geometry.Vector3 posPoint, Geometry.Vector3 posTarget)
        {
            return (float)Math.Atan2(posTarget.Y - posPoint.Y, posTarget.X - posPoint.X);
        }

        /// <summary>
        /// 给定三个点，求其中一点到对线的垂线长度
        /// </summary>
        /// <param name="posCenter"></param>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns></returns>
        public static float GetVerticalLength(Geometry.Vector3 posCenter, Geometry.Vector3 posA, Geometry.Vector3 posB)
        {
            var angleA = CMath.GetIncludedAngle(posA, posCenter, posB);
            var sinA = Math.Sin(Math.PI * angleA / 180);
            var sideAX = posA.X - posCenter.X;
            var sideAY = posA.Y - posCenter.Y;
            //斜边长
            var lenAC = Math.Sqrt(sideAX * sideAX + sideAY * sideAY);
            //垂线长度
            var verticalLength = sinA * lenAC;
            return (float)verticalLength;
        }

    }
}
