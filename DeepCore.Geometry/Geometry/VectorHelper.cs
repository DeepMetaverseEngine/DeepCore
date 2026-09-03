using DeepCore.Formula;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DeepCore.Geometry
{
    public static class VectorHelper
    {
        public static Vector2 Polar(float degree, float distance)
        {
            var v = Vector2.Zero;
            float dx = (float)(Math.Cos(degree) * distance);
            float dy = (float)(Math.Sin(degree) * distance);
            v.X += (dx);
            v.Y += (dy);
            return v;
        }
        /// <summary>
        /// 通过极坐标来移动
        /// </summary>
        /// <param name="v"></param>
        /// <param name="degree">弧度</param>
        /// <param name="distance">距离</param>
        public static void MovePolar(ref Vector2 v, float degree, float distance)
        {
            float dx = (float)(Math.Cos(degree) * distance);
            float dy = (float)(Math.Sin(degree) * distance);
            v.X += (dx);
            v.Y += (dy);
        }
        public static void MovePolar(ref Vector3 v, float degree, float distance)
        {
            float dx = (float)(Math.Cos(degree) * distance);
            float dy = (float)(Math.Sin(degree) * distance);
            v.X += (dx);
            v.Y += (dy);
        }
        public static void MovePolar(ref float x, ref float y, float degree, float distance)
        {
            x += (float)(Math.Cos(degree) * distance);
            y += (float)(Math.Sin(degree) * distance);
        }

        /// <summary>
        /// 通过极坐标来移动
        /// </summary>
        /// <param name="v"></param>
        /// <param name="degree">弧度</param>
        /// <param name="speed_sec">速度 (单位距离/秒)</param>
        /// <param name="interval_ms">毫秒时间</param>
        public static void MovePolar(ref Vector2 v, float degree, float speed_sec, float interval_ms)
        {
            float distance = GetDistanceSpeedTime(speed_sec, interval_ms);
            MovePolar(ref v, degree, distance);
        }
        public static void MovePolar(ref float x, ref float y, float degree, float speed, float interval_ms)
        {
            float distance = GetDistanceSpeedTime(speed, interval_ms);
            MovePolar(ref x, ref y, degree, distance);
        }

        /// <summary>
        /// 向目标移动
        /// </summary>
        /// <param name="v"></param>
        /// <param name="dx">目标x</param>
        /// <param name="dy">目标y</param>
        /// <param name="distance">是否到达目的地</param>
        /// <returns></returns>
        public static bool MoveTo(ref Vector2 v, float dx, float dy, float distance)
        {
            float ddx = dx - v.X;
            float ddy = dy - v.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                v.X = (dx);
                v.Y = (dy);
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx);
                MovePolar(ref v, angle, distance);
                return false;
            }
        }
        public static bool MoveTo(ref float x, ref float y, float dx, float dy, float distance)
        {
            float ddx = dx - x;
            float ddy = dy - y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                x = (dx);
                y = (dy);
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx);
                MovePolar(ref x, ref y, angle, distance);
                return false;
            }
        }
        public static bool MoveTo(ref Vector2 v, float dx, float dy, float distance, float angle_offset)
        {
            float ddx = dx - v.X;
            float ddy = dy - v.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                v.X = (dx);
                v.Y = (dy);
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx) + angle_offset;
                MovePolar(ref v, angle, distance);
                return false;
            }
        }
        public static bool MoveTo(ref float x, ref float y, float dx, float dy, float distance, float angle_offset)
        {
            float ddx = dx - x;
            float ddy = dy - y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                x = (dx);
                y = (dy);
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx) + angle_offset;
                MovePolar(ref x, ref y, angle, distance);
                return false;
            }
        }

        public static bool MoveTo3D(ref Vector3 current, in Vector3 target, float distance)
        {
            Vector3 a = target - current;
            float magnitude = Vector3.Distance(current, target);
            if (magnitude > distance)
            {
                current = current + a / magnitude * distance;
                return false;
            }
            current = target;
            return true;
        }
        public static bool MoveTo2D(ref Vector3 current, in Vector3 target, float distance)
        {
            Vector2 a = target - current;
            float magnitude = (float)Math.Sqrt(a.X * a.X + a.Y * a.Y);
            if (magnitude > distance)
            {
                current = current + new Vector3(a, current.Z) / magnitude * distance;
                return false;
            }
            current = target;
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <returns>还剩多少没走完</returns>
        public static float MoveLerpTo(ref Vector3 current, in Vector3 target, float distance)
        {
            float magnitude = Vector3.Distance(current, target);           
            if (magnitude > distance)
            {
                Vector3 a = target - current;
                current = (current + a / magnitude * distance);
                return 0;
            }
            else
            {
                current = target;
                return distance - magnitude;
            }
        }

        public static Vector3 MoveLerp(this Vector3 current, Vector3 normal, float distance)
        {
            return new Vector3(
                current.X + normal.X * distance,
                current.Y + normal.Y * distance,
                current.Z + normal.Z * distance);
        }

        public static Vector3 MoveTowards(this Vector3 current, Vector3 target, float maxDistanceDelta, bool limit = true)
        {
            float num = target.X - current.X;
            float num2 = target.Y - current.Y;
            float num3 = target.Z - current.Z;
            float num4 = num * num + num2 * num2 + num3 * num3;
            if (num4 == 0f)
            {
                return target;
            }
            if (limit && (maxDistanceDelta >= 0f && num4 <= maxDistanceDelta * maxDistanceDelta))
            {
                return target;
            }
            float num5 = (float)Math.Sqrt(num4);
            return new Vector3(current.X + num / num5 * maxDistanceDelta, current.Y + num2 / num5 * maxDistanceDelta, current.Z + num3 / num5 * maxDistanceDelta);
        }
        public static Vector2 MoveTowards(this Vector2 current, Vector2 target, float maxDistanceDelta, bool limit = true)
        {
            float num = target.X - current.X;
            float num2 = target.Y - current.Y;
            float num4 = num * num + num2 * num2;
            if (num4 == 0f)
            {
                return target;
            }
            if (limit && (maxDistanceDelta >= 0f && num4 <= maxDistanceDelta * maxDistanceDelta))
            {
                return target;
            }
            float num5 = (float)Math.Sqrt(num4);
            return new Vector2(current.X + num / num5 * maxDistanceDelta, current.Y + num2 / num5 * maxDistanceDelta);
        }


        public static bool MoveToX(ref Vector2 v, float x, float distance)
        {
            float ddx = x - v.X;
            if (Math.Abs(ddx) < distance)
            {
                v.X = (x);
                return true;
            }
            else
            {
                if (ddx > 0)
                {
                    v.X += (distance);
                }
                else
                {
                    v.X += (-distance);
                }
                return false;
            }
        }
        public static bool MoveToY(ref Vector2 v, float y, float distance)
        {
            float ddy = y - v.Y;
            if (Math.Abs(ddy) < distance)
            {
                v.Y = (y);
                return true;
            }
            else
            {
                if (ddy > 0)
                {
                    v.Y += (distance);
                }
                else
                {
                    v.Y += (-distance);
                }
                return false;
            }
        }

        public static void Scale(ref Vector2 v, float scale)
        {
            v.X = (v.X * scale);
            v.Y = (v.Y * scale);
        }
        public static void Scale(ref Vector3 v, float scale)
        {
            v.X = (v.X * scale);
            v.Y = (v.Y * scale);
            v.Z = (v.Z * scale);
        }
        public static void Scale(ref Vector2 v, float scale_x, float scale_y)
        {
            v.X = (v.X * scale_x);
            v.Y = (v.Y * scale_y);
        }
        public static void Scale(ref float x, ref float y, float scale_x, float scale_y)
        {
            x = (x * scale_x);
            y = (y * scale_y);
        }

        public static void Rotate(ref Vector2 v, float degree)
        {
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = (v.X) * cos_v - (v.Y) * sin_v;
            float y = (v.Y) * cos_v + (v.X) * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref Vector3 v, float degree)
        {
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = (v.X) * cos_v - (v.Y) * sin_v;
            float y = (v.Y) * cos_v + (v.X) * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref float x, ref float y, float degree)
        {
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float dx = (x) * cos_v - (y) * sin_v;
            float dy = (y) * cos_v + (x) * sin_v;
            x = (dx);
            y = (dy);
        }
        public static void Rotate(ref Vector2 v, in Vector2 p0, float degree)
        {
            float dx = v.X - p0.X;
            float dy = v.Y - p0.Y;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = p0.X + dx * cos_v - dy * sin_v;
            float y = p0.Y + dy * cos_v + dx * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref Vector3 v, in Vector3 p0, float degree)
        {
            float dx = v.X - p0.X;
            float dy = v.Y - p0.Y;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = p0.X + dx * cos_v - dy * sin_v;
            float y = p0.Y + dy * cos_v + dx * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref Vector2 v, float px, float py, float degree)
        {
            float dx = v.X - px;
            float dy = v.Y - py;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = px + dx * cos_v - dy * sin_v;
            float y = py + dy * cos_v + dx * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref Vector3 v, float px, float py, float degree)
        {
            float dx = v.X - px;
            float dy = v.Y - py;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = px + dx * cos_v - dy * sin_v;
            float y = py + dy * cos_v + dx * sin_v;
            v.X = (x);
            v.Y = (y);
        }
        public static void Rotate(ref float x, ref float y, float px, float py, float degree)
        {
            float dx = x - px;
            float dy = y - py;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float rx = px + dx * cos_v - dy * sin_v;
            float ry = py + dy * cos_v + dx * sin_v;
            x = (rx);
            y = (ry);
        }

        public static float GetDirection(float d)
        {
            if (d > 0)
            {
                return 1;
            }
            if (d < 0)
            {
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// 得到速度和时间产生的距离
        /// </summary>
        /// <param name="speed_sec">速度 (单位距离/秒)</param>
        /// <param name="interval_ms">毫秒时间</param>
        /// <returns></returns>
        public static float GetDistanceSpeedTime(float speed_sec, float interval_ms)
        {
            float rate = interval_ms / 1000f;
            return speed_sec * rate;
        }

        public static float GetDistance(float rx, float ry)
        {
            return (float)Math.Sqrt(rx * rx + ry * ry);
        }
        public static float GetDistance(float x1, float y1, float x2, float y2)
        {
            float r1 = x1 - x2;
            float r2 = y1 - y2;
            return (float)Math.Sqrt(r1 * r1 + r2 * r2);
        }
        public static float GetDistanceSquare(float x1, float y1, float x2, float y2)
        {
            float r1 = x1 - x2;
            float r2 = y1 - y2;
            return r1 * r1 + r2 * r2;
        }
        public static float GetDistanceSquare(in Vector3 a, in Vector3 b)
        {
            Vector3 c = a - b;
            float magnitude = (float)(c.X * c.X + c.Y * c.Y + c.Z * c.Z);
            return magnitude;
        }

        public static float GetDistance(in Vector3 a, in Vector3 b)
        {
            Vector3 c = a - b;
            float magnitude = (float)Math.Sqrt(c.X * c.X + c.Y * c.Y + c.Z * c.Z);
            return magnitude;
        }
        public static float GetDistance(in Vector2 v1, in Vector2 v2)
        {
            float r1 = v1.X - v2.X;
            float r2 = v1.Y - v2.Y;
            return (float)Math.Sqrt(r1 * r1 + r2 * r2);
        }
        public static float GetDistanceSquare(in Vector2 v1, in Vector2 v2)
        {
            float r1 = v1.X - v2.X;
            float r2 = v1.Y - v2.Y;
            return (r1 * r1 + r2 * r2);
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
        /// <summary>
        /// 得到弧度
        /// </summary>
        /// <param name="v">向量</param>
        /// <returns></returns>
        public static float GetDegree(in Vector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }
        public static float GetDegree(in Vector2 a, in Vector2 b)
        {
            return (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        }
        public static double GetDegreeD(in Vector2 a, in Vector2 b)
        {
            return Math.Atan2(b.Y - a.Y, b.X - a.X);
        }

        /// <summary>
        /// 将2个向量相加得到一个新的向量
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector2 VectorAdd(in Vector2 a, in Vector2 b)
        {
            var v = new Vector2();
            v.X = (a.X + b.X);
            v.Y = (a.Y + b.Y);
            return v;
        }

        /// <summary>
        /// 将2个向量相减得到一个新的向量
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector2 VectorSub(in Vector2 a, in Vector2 b)
        {
            var v = new Vector2();
            v.X = (a.X - b.X);
            v.Y = (a.Y - b.Y);
            return v;
        }

        /// <summary>
        /// 将一个向量加上新的向量，得到一个新的向量
        /// </summary>
        /// <param name="a"></param>
        /// <param name="degree"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector2 VectorAdd(in Vector2 a, float degree, float distance)
        {
            var v = a;
            MovePolar(ref v, degree, distance);
            return v;
        }

        /// <summary>
        /// 把一个向量向自己本身的方向相加，得到一个新的向量
        /// </summary>
        /// <param name="a"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector2 VectorAdd(in Vector2 a, float distance)
        {
            var v = a;
            MovePolar(ref v, GetDegree(v), distance);
            return v;
        }

        /// <summary>
        /// 将一个向量缩放一定比率后，得到一个新的向量
        /// </summary>
        /// <param name="a"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Vector2 VectorScale(in Vector2 a, float scale)
        {
            Vector2 v = new Vector2();
            v.X = (a.X * scale);
            v.Y = (a.Y * scale);
            return v;
        }


        public static float VectorDot(in Vector2 v1, in Vector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public static float VectorDot(float x1, float y1, float x2, float y2)
        {
            return x1 * x2 + y1 * y2;
        }


        public static Vector3 Calculate(Vector3 a, NumericOP op, Vector3 b)
        {
            switch (op)
            {
                case NumericOP.ADD:
                    return a + b;
                case NumericOP.SUB:
                    return a - b;
                case NumericOP.MUL:
                    return a * b;
                case NumericOP.DIV:
                    return a / b;
                case NumericOP.MOD:
                    return new Vector3(
                        a.X % b.X,
                        a.Y % b.Y,
                        a.Z % b.Z);
            }
            throw new Exception("NumericOP未识别的操作数: " + op);
        }

    }

}
