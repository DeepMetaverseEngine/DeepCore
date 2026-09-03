using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.Geometry
{
    /// <summary>
    /// A code container for collision-related mathematical functions.
    /// </summary>
    static public class CollisionMath
    {
        /// <summary>
        /// Data defining a circle/line collision result.
        /// </summary>
        /// <remarks>Also used for circle/rectangles.</remarks>
        public struct CircleLineCollisionResult
        {
            public bool Collision;
            public Vector2 Point;
            public Vector2 Normal;
            public float Distance;
        }


        /// <summary>
        /// Determine if two circles intersect or contain each other.
        /// </summary>
        /// <param name="center1">The center of the first circle.</param>
        /// <param name="radius1">The radius of the first circle.</param>
        /// <param name="center2">The center of the second circle.</param>
        /// <param name="radius2">The radius of the second circle.</param>
        /// <returns>True if the circles intersect or contain one another.</returns>
        public static bool CircleCircleIntersect(in Vector2 center1, float radius1, in Vector2 center2, float radius2)
        {
            var line = center2 - center1;
            var r2 = (radius1 + radius2);
            // we use LengthSquared to avoid a costly square-root call
            return (line.LengthSquared() <=  r2 * r2);
        }



        /// <summary>
        /// Determines the point of intersection between two line segments, 
        /// as defined by four points.
        /// </summary>
        /// <param name="a">The first point on the first line segment.</param>
        /// <param name="b">The second point on the first line segment.</param>
        /// <param name="c">The first point on the second line segment.</param>
        /// <param name="d">The second point on the second line segment.</param>
        /// <param name="point">The output value with the interesection, if any.</param>
        /// <remarks>The output parameter "point" is only valid
        /// when the return value is true.</remarks>
        /// <returns>True if intersecting, false otherwise.</returns>
        public static bool LineLineIntersect(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, out Vector2 point)
        {
            point = Vector2.Zero;

            double r, s;
            double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            // If the denominator in above is zero, AB & CD are colinear
            if (denominator == 0)
            {
                return false;
            }

            double numeratorR = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            r = numeratorR / denominator;

            double numeratorS = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);
            s = numeratorS / denominator;

            // non-intersecting
            if (r < 0 || r > 1 || s < 0 || s > 1)
            {
                return false;
            }

            // find intersection point
            point.X = (float)(a.X + (r * (b.X - a.X)));
            point.Y = (float)(a.Y + (r * (b.Y - a.Y)));

            return true;
        }
        public static bool LineLineIntersect(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d)
        {
            double r, s;
            double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            // If the denominator in above is zero, AB & CD are colinear
            if (denominator == 0)
            {
                return false;
            }

            double numeratorR = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            r = numeratorR / denominator;

            double numeratorS = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);
            s = numeratorS / denominator;

            // non-intersecting
            if (r < 0 || r > 1 || s < 0 || s > 1)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算两条直线的交点
        /// </summary>
        /// <param name="sp0">L1的点1坐标</param>
        /// <param name="sp1">L1的点2坐标</param>
        /// <param name="dp0">L2的点1坐标</param>
        /// <param name="dp1">L2的点2坐标</param>
        /// <param name="point">焦点</param>
        /// <returns></returns>
        public static bool LineLineIntersectStraight(in Vector2 sp0, in Vector2 sp1, in Vector2 dp0, in Vector2 dp1, out Vector2 point)
        {
            /*
             * L1，L2都存在斜率的情况：
             * 直线方程L1: ( y - y1 ) / ( y2 - y1 ) = ( x - x1 ) / ( x2 - x1 ) 
             * => y = [ ( y2 - y1 ) / ( x2 - x1 ) ]( x - x1 ) + y1
             * 令 a = ( y2 - y1 ) / ( x2 - x1 )
             * 有 y = a * x - a * x1 + y1   .........1
             * 直线方程L2: ( y - y3 ) / ( y4 - y3 ) = ( x - x3 ) / ( x4 - x3 )
             * 令 b = ( y4 - y3 ) / ( x4 - x3 )
             * 有 y = b * x - b * x3 + y3 ..........2
             * 
             * 如果 a = b，则两直线平等，否则， 联解方程 1,2，得:
             * x = ( a * x1 - b * x3 - y1 + y3 ) / ( a - b )
             * y = a * x - a * x1 + y1
             * 
             * L1存在斜率, L2平行Y轴的情况：
             * x = x3
             * y = a * x3 - a * x1 + y1
             * 
             * L1 平行Y轴，L2存在斜率的情况：
             * x = x1
             * y = b * x - b * x3 + y3
             * 
             * L1与L2都平行Y轴的情况：
             * 如果 x1 = x3，那么L1与L2重合，否则平等
             * 
            */
            float a = 0, b = 0;
            int state = 0;
            if (sp0.X != sp1.X)
            {
                a = (sp1.Y - sp0.Y) / (sp1.X - sp0.X);
                state |= 1;
            }
            if (dp0.X != dp1.X)
            {
                b = (dp1.Y - dp0.Y) / (dp1.X - dp0.X);
                state |= 2;
            }
            switch (state)
            {
                case 0: //L1与L2都平行Y轴
                    {
                        if (sp0.X == dp0.X)
                        {
                            //throw new Exception("两条直线互相重合，且平行于Y轴，无法计算交点。");
                            point = new Vector2(0, 0);
                            return false;
                        }
                        else
                        {
                            //throw new Exception("两条直线互相平行，且平行于Y轴，无法计算交点。");
                            point = new Vector2(0, 0);
                            return false;
                        }
                    }
                case 1: //L1存在斜率, L2平行Y轴
                    {
                        float x = dp0.X;
                        float y = (sp0.X - x) * (-a) + sp0.Y;
                        point = new Vector2(x, y);
                        return true;
                    }
                case 2: //L1 平行Y轴，L2存在斜率
                    {
                        float x = sp0.X;
                        //网上有相似代码的，这一处是错误的。你可以对比case 1 的逻辑 进行分析
                        //源code:lineSecondStar * x + lineSecondStar * lineSecondStar.X + p3.Y;
                        float y = (dp0.X - x) * (-b) + dp0.Y;
                        point = new Vector2(x, y);
                        return true;
                    }
                case 3: //L1，L2都存在斜率
                    {
                        if (a == b)
                        {
                            // throw new Exception("两条直线平行或重合，无法计算交点。");
                            point = new Vector2(0, 0);
                        }
                        float x = (a * sp0.X - b * dp0.X - sp0.Y + dp0.Y) / (a - b);
                        float y = a * x - a * sp0.X + sp0.Y;
                        point = new Vector2(x, y);
                        return true;
                    }
            }
            point = new Vector2(0, 0);
            return false;
        }

        /// <summary>  
        /// 求直线外一点到该直线的投影点  
        /// </summary>  
        /// <param name="lineA">两点一线</param>  
        /// <param name="lineB">两点一线</param>  
        /// <param name="pOut">线外指定点</param>  
        /// <param name="pProject">投影点</param> 
        public static void GetProjectivePoint(in Vector2 lineA, in Vector2 lineB, in Vector2 pOut, out Vector2 pProject)
        {
            float k = (lineA.Y - lineB.Y) / (lineA.X - lineB.X);
            if (k == 0) //垂线斜率不存在情况  
            {
                pProject.Value.X = pOut.X;
                pProject.Value.Y = lineA.Y;
            }
            else
            {
                pProject.Value.X = (float)((k * lineA.X + pOut.X / k + pOut.Y - lineA.Y) / (1 / k + k));
                pProject.Value.Y = (float)(-1 / k * (pProject.Value.X - pOut.X) + pOut.Y);
            }
        }
        /// <summary>  
        /// 求直线外一点到该直线的投影点  
        /// </summary>  
        /// <param name="pLine">线上任一点</param>  
        /// <param name="k">直线斜率</param>  
        /// <param name="pOut">线外指定点</param>  
        /// <param name="pProject">投影点</param>  
        public static void GetProjectivePoint(in Vector2 pLine, double k, in Vector2 pOut, out Vector2 pProject)
        {
            if (k == 0) //垂线斜率不存在情况  
            {
                pProject.Value.X = pOut.X;
                pProject.Value.Y = pLine.Y;
            }
            else
            {
                pProject.Value.X = (float)((k * pLine.X + pOut.X / k + pOut.Y - pLine.Y) / (1 / k + k));
                pProject.Value.Y = (float)(-1 / k * (pProject.Value.X - pOut.X) + pOut.Y);
            }
        }

        /// <summary>
        /// 移动到线段的边
        /// </summary>
        /// <returns></returns>
        public static bool MoveToLineBorder(in Vector2 from, in Vector2 to, in Vector2 line_a, in Vector2 line_b, out Vector2 out_touch)
        {
            if (DeepCore.Geometry.CollisionMath.LineLineIntersect(line_a, line_b, from, to, out out_touch))
            {
                GetProjectivePoint(line_a, line_b, to, out out_touch); // 射线目标点和边做垂足
                return true;
            }
            return false;
        }
        public static bool MoveToPolyBorder(in Vector2 from, in Vector2 to, ReadOnlySpan<Vector2> polygon, out Vector2 out_touch)
        {
            out_touch = Vector2.NaN;
            float minDSqr = float.MaxValue;
            for (int i = 1; i < polygon.Length; i++)
            {
                if (MoveToLineBorder(from, to, polygon[i - 1], polygon[i], out var _touch))
                {
                    var d = Vector2.DistanceSquared(in from, in _touch);
                    if (d < minDSqr)
                    {
                        minDSqr = d;
                        out_touch = _touch;
                    }
                }
            }
            if (polygon.Length >= 3)
            {
                if (MoveToLineBorder(from, to, polygon[polygon.Length - 1], polygon[0], out var _touch))
                {
                    var d = Vector2.DistanceSquared(in from, in _touch);
                    if (d < minDSqr)
                    {
                        minDSqr = d;
                        out_touch = _touch;
                    }
                }
            }
            return !out_touch.IsNaN;
        }



        /// <summary>
        /// Determines if a circle and line segment intersect, and if so, how they do.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="lineStart">The first point on the line segment.</param>
        /// <param name="lineEnd">The second point on the line segment.</param>
        /// <param name="result">The result data for the collision.</param>
        /// <returns>True if a collision occurs, provided for convenience.</returns>
        public static bool CircleLineCollide(in Vector2 center, float radius, in Vector2 lineStart, in Vector2 lineEnd, ref CircleLineCollisionResult result)
        {
            Vector2 AC = center - lineStart;
            Vector2 AB = lineEnd - lineStart;
            float ab2 = AB.LengthSquared();
            if (ab2 <= 0f)
            {
                return false;
            }
            float acab = Vector2.Dot(AC, AB);
            float t = acab / ab2;

            if (t < 0.0f)
                t = 0.0f;
            else if (t > 1.0f)
                t = 1.0f;

            result.Point = lineStart + t * AB;
            result.Normal = center - result.Point;

            float h2 = result.Normal.LengthSquared();
            float r2 = radius * radius;

            if ((h2 == 0) || (h2 <= r2))
            {
                result.Normal.Normalize();
                result.Distance = (radius - (center - result.Point).Length());
                result.Collision = true;
            }
            else
            {
                result.Collision = false;
            }

            return result.Collision;
        }


        public static bool CircleLineCollide(in Vector2 center, float radius, in Vector2 lineStart, in Vector2 lineEnd)
        {
            Vector2 AC = center - lineStart;
            Vector2 AB = lineEnd - lineStart;
            float ab2 = AB.LengthSquared();
            if (ab2 <= 0f)
            {
                return false;
            }
            float acab = Vector2.Dot(AC, AB);
            float t = acab / ab2;

            if (t < 0.0f)
                t = 0.0f;
            else if (t > 1.0f)
                t = 1.0f;

            Vector2 resultPoint = lineStart + t * AB;
            Vector2 resultNormal = center - resultPoint;

            float h2 = resultNormal.LengthSquared();
            float r2 = radius * radius;

            if ((h2 == 0) || (h2 <= r2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 点在凸多边形内部。
        /// </summary>
        /// <param name="center"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool PointInPolygon(in Vector2 center, ReadOnlySpan<Vector2> list)
        {
            int wn = 0, j = 0; //wn 计数器 j第二个
            for (int i = 0; i < list.Length; i++)
            {
                //开始循环  
                if (i == list.Length - 1)
                {
                    j = 0;//如果 循环到最后一点 第二个指针指向第一点  
                }
                else
                {
                    j = j + 1; //如果不是 ，则找下一点  
                }
                if (list[i].Y <= center.Y) // 如果多边形的点 小于等于 选定点的 Y 坐标  
                {
                    if (list[j].Y > center.Y) // 如果多边形的下一点 大于于 选定点的 Y 坐标  
                    {
                        if (PointOnLineLeft(list[i], list[j], center) > 0)
                        {
                            wn++;
                        }
                    }
                }
                else
                {
                    if (list[j].Y <= center.Y)
                    {
                        if (PointOnLineLeft(list[i], list[j], center) < 0)
                        {
                            wn--;
                        }
                    }
                }
            }
            if (wn == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 判断点在线的一边 
        /// </summary>
        /// <param name="P0"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>0 is touch</returns>
        public static float PointOnLineLeft(in Vector2 P0, in Vector2 P1, in Vector2 P2)
        {
            float abc = ((P1.X - P0.X) * (P2.Y - P0.Y) - (P2.X - P0.X) * (P1.Y - P0.Y));
            return abc;
        }

        public static Vector2 MoveToByRadians(in Vector2 p, float degree, float distance)
        {
            return new Vector2(
                p.X + (float)(Math.Cos(degree) * distance),
                p.Y + (float)(Math.Sin(degree) * distance));
        }

        public enum PointOnLineResult : byte
        {
            Left,
            Right,
            Touch,
        }
        /// <summary>
        /// 输入三个点，并且判断第三个点在前两个点连成的直线的左边还是右边或者是在线上
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static PointOnLineResult PointOnLine(in Vector2 p0, in Vector2 p1, in Vector2 p2)
        {
            float f = (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
            if (f > 0)
                return PointOnLineResult.Right;
            if (f < 0)
                return PointOnLineResult.Left;
            return PointOnLineResult.Touch;
        }

        public static bool SphereContainsPoint(in Vector3 center, float radius, in Vector3 point)
        {
            float sqRadius = radius * radius;
            float sqDistance;
            Vector3.DistanceSquared(in point, in center, out sqDistance);
            return (sqDistance <= sqRadius);
        }
        public static bool SphereIntersectSphere(in Vector3 p1, float r1, in Vector3 p2, float r2)
        {
            var rd = (r1 + r2);
            Vector3.DistanceSquared(in p1, in p2, out var sqDistance);
            return (sqDistance <= rd * rd);
        }


    }
}
