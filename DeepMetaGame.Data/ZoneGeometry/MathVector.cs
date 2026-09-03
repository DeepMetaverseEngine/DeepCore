using DeepCore;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneGeometry
{
    /// <summary>
    /// 2D向量
    /// </summary>
    public class VectorObject2 : IVector2
    {
        public System.Numerics.Vector2 Value;
        public float X { get => Value.X; set { Value.X = value; } }
        public float Y { get => Value.Y; set { Value.Y = value; } }
        public VectorObject2()
        {
            Value.X = 0;
            Value.Y = 0;
        }
        public VectorObject2(float _x, float _y)
        {
            Value.X = _x;
            Value.Y = _y;
        }
        public virtual object Clone()
        {
            return new VectorObject2() { Value = Value };
        }
        public static bool Equals2D(VectorObject2 a, VectorObject2 b)
        {
            if (a != null && b != null)
            {
                return a.Value == b.Value;
            }
            return a == b;
        }
        public override string ToString()
        {
            return $"{X},{Y}";
        }

        public void FromGeometry2(DeepCore.Geometry.Vector2 pos)
        {
            Value = pos.Value;
        }
        public DeepCore.Geometry.Vector2 ToGeometry2()
        {
            return new DeepCore.Geometry.Vector2() { Value = Value };
        }
        public static implicit operator VectorObject2(DeepCore.Geometry.Vector2 value)
        {
            return new VectorObject2() { Value = value.Value };
        }
        public static explicit operator DeepCore.Geometry.Vector2(VectorObject2 value)
        {
            return new DeepCore.Geometry.Vector2() { Value = value.Value };
        }
    }

    public class VectorObject3 : IVector3
    {
        public System.Numerics.Vector3 Value;
        public float X { get => Value.X; set { Value.X = value; } }
        public float Y { get => Value.Y; set { Value.Y = value; } }
        public float Z { get => Value.Z; set { Value.Z = value; } }
        public bool IsNaN => float.IsNaN(X) || float.IsNaN(Y) || float.IsNaN(Z);
        public VectorObject3()
        {
            Value = System.Numerics.Vector3.Zero;
        }
        public VectorObject3(float _x, float _y, float _z)
        {
            Value.X = _x;
            Value.Y = _y;
            Value.Z = _z;
        }
        public object Clone()
        {
            return new VectorObject3() { Value = Value };
        }
        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }
        public static bool Equals3D(VectorObject3 a, VectorObject3 b)
        {
            if (a != null && b != null)
            {
                return a.Value == b.Value;
            }
            return a == b;
        }
        public void FromGeometry3(DeepCore.Geometry.Vector3 pos)
        {
            Value = pos.Value;
        }
        public DeepCore.Geometry.Vector2 ToGeometry2()
        {
            return new DeepCore.Geometry.Vector2(X, Y);
        }
        public DeepCore.Geometry.Vector3 ToGeometry3()
        {
            return new DeepCore.Geometry.Vector3() { Value = Value };
        }
        public static implicit operator VectorObject3(DeepCore.Geometry.Vector3 value)
        {
            return new VectorObject3() { Value = value.Value };
        }
        public static explicit operator DeepCore.Geometry.Vector3(VectorObject3 value)
        {
            return new DeepCore.Geometry.Vector3() { Value = value.Value };
        }
    }


    /// <summary>
    /// 2D极坐标向量
    /// </summary>
    public class PolarObject3 : ICloneable
    {
        public float direction;
        public float distance;
        public float height;

        public PolarObject3()
        {
        }

        public PolarObject3(float _direction, float _distance, float _height)
        {
            direction = _direction;
            distance = _distance;
            height = _height;
        }

        public bool Equals(PolarObject3 v)
        {
            return v.direction == direction && v.distance == distance && v.height == height;
        }

        public object Clone()
        {
            return new PolarObject3(direction, distance, height);
        }

    }


    public class LineObject2 : ICloneable
    {
        readonly public VectorObject2 p = new VectorObject2();
        readonly public VectorObject2 q = new VectorObject2();

        public LineObject2()
        {
        }

        public LineObject2(float x0, float y0, float x1, float y1)
        {
            p.X = x0;
            p.Y = y0;
            q.X = x1;
            q.Y = y1;
        }
        public LineObject2(VectorObject2 p, VectorObject2 q)
        {
            this.p = p;
            this.q = q;
        }


        public float getMinX()
        {
            return Math.Min(p.X, q.X);
        }

        public float getMaxX()
        {
            return Math.Max(p.X, q.X);
        }

        public float getMinY()
        {
            return Math.Min(p.Y, q.Y);
        }

        public float getMaxY()
        {
            return Math.Max(p.Y, q.Y);
        }

        public object Clone()
        {
            return new LineObject2(p.X, p.Y, q.X, q.Y);
        }

    }

    public static class MathVector
    {
        public static bool IsNullOrNaN(this VectorObject3 v3)
        {
            return v3 == null || v3.IsNaN;
        }

        /**
         * 移动指定偏移
         * @param v
         * @param dx x距离
         * @param dy y距离
         */
        public static void move(IVector2 v, float dx, float dy)
        {
            v.X += dx;
            v.Y += dy;
        }
        public static void move(ref DeepCore.Geometry.Vector3 v, float dx, float dy)
        {
            v.X += dx;
            v.Y += dy;
        }

        /**
         * 通过极坐标来移动
         * @param v
         * @param degree 弧度
         * @param distance 距离
         */
        public static void movePolar(IVector2 v, float degree, float distance)
        {
            float dx = (float)(Math.Cos(degree) * distance);
            float dy = (float)(Math.Sin(degree) * distance);
            move(v, dx, dy);
        }
        public static void movePolar(ref DeepCore.Geometry.Vector3 v, float degree, float distance)
        {
            float dx = (float)(Math.Cos(degree) * distance);
            float dy = (float)(Math.Sin(degree) * distance);
            move(ref v, dx, dy);
        }
        public static void movePolar(ref float x, ref float y, float degree, float distance)
        {
            x += (float)(Math.Cos(degree) * distance);
            y += (float)(Math.Sin(degree) * distance);
        }

        /**
         * 通过极坐标来移动
         * @param v
         * @param degree 弧度
         * @param speed  速度 (单位距离/秒)
         * @param interval_ms 毫秒时间
         */
        public static void movePolar(IVector2 v, float degree, float speed, float interval_ms)
        {
            float distance = getDistanceSpeedTime(speed, interval_ms);
            movePolar(v, degree, distance);
        }
        public static void movePolar(ref float x, ref float y, float degree, float speed, float interval_ms)
        {
            float distance = getDistanceSpeedTime(speed, interval_ms);
            movePolar(ref x, ref y, degree, distance);
        }

        /**
         * 向目标移动
         * @param v
         * @param x 目标x
         * @param y 目标y
         * @return 是否到达目的地
         */
        public static bool moveTo(IVector2 v, float dx, float dy, float distance)
        {
            float ddx = dx - v.X;
            float ddy = dy - v.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                v.X = dx;
                v.Y = dy;
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx);
                movePolar(v, angle, distance);
                return false;
            }
        }
        public static bool moveTo(ref float x, ref float y, float dx, float dy, float distance)
        {
            float ddx = dx - x;
            float ddy = dy - y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                x = dx;
                y = dy;
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx);
                movePolar(ref x, ref y, angle, distance);
                return false;
            }
        }
        public static bool moveTo(IVector2 v, float dx, float dy, float distance, float angle_offset)
        {
            float ddx = dx - v.X;
            float ddy = dy - v.Y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                v.X = dx;
                v.Y = dy;
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx) + angle_offset;
                movePolar(v, angle, distance);
                return false;
            }
        }
        public static bool moveTo(ref float x, ref float y, float dx, float dy, float distance, float angle_offset)
        {
            float ddx = dx - x;
            float ddy = dy - y;
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                x = dx;
                y = dy;
                return true;
            }
            else
            {
                float angle = (float)Math.Atan2(ddy, ddx) + angle_offset;
                movePolar(ref x, ref y, angle, distance);
                return false;
            }
        }

        public static bool moveToX(IVector2 v, float x, float distance)
        {
            float ddx = x - v.X;
            if (Math.Abs(ddx) < distance)
            {
                v.X = x;
                return true;
            }
            else
            {
                if (ddx > 0)
                {
                    v.X += distance;
                }
                else
                {
                    v.X += -distance;
                }
                return false;
            }
        }
        public static bool moveToY(IVector2 v, float y, float distance)
        {
            float ddy = y - v.Y;
            if (Math.Abs(ddy) < distance)
            {
                v.Y = y;
                return true;
            }
            else
            {
                if (ddy > 0)
                {
                    v.Y += distance;
                }
                else
                {
                    v.Y += -distance;
                }
                return false;
            }
        }

        public static void scale(IVector2 v, float scale)
        {
            v.X = v.X * scale;
            v.Y = v.Y * scale;
        }
        public static void scale(IVector2 v, float scale_x, float scale_y)
        {
            v.X = v.X * scale_x;
            v.Y = v.Y * scale_y;
        }
        public static void scale(ref float x, ref float y, float scale_x, float scale_y)
        {
            x = x * scale_x;
            y = y * scale_y;
        }

        public static void rotate(IVector2 v, float degree)
        {
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = v.X * cos_v - v.Y * sin_v;
            float y = v.Y * cos_v + v.X * sin_v;
            v.X = x;
            v.Y = y;
        }
        public static void rotate(IVector2 v, IVector2 p0, float degree)
        {
            float dx = v.X - p0.X;
            float dy = v.Y - p0.Y;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = p0.X + dx * cos_v - dy * sin_v;
            float y = p0.Y + dy * cos_v + dx * sin_v;
            v.X = x;
            v.Y = y;
        }
        public static void rotate(IVector2 v, float px, float py, float degree)
        {
            float dx = v.X - px;
            float dy = v.Y - py;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float x = px + dx * cos_v - dy * sin_v;
            float y = py + dy * cos_v + dx * sin_v;
            v.X = x;
            v.Y = y;
        }
        public static void rotate(ref float x, ref float y, float px, float py, float degree)
        {
            float dx = x - px;
            float dy = y - py;
            float cos_v = (float)Math.Cos(degree);
            float sin_v = (float)Math.Sin(degree);
            float rx = px + dx * cos_v - dy * sin_v;
            float ry = py + dy * cos_v + dx * sin_v;
            x = rx;
            y = ry;
        }

        public static float getDirection(float d)
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

        /**
         * 得到速度和时间产生的距离
         * @param speed 速度 (单位距离/秒)
         * @param interval_ms 毫秒时间
         * @return
         */
        public static float getDistanceSpeedTime(float speed, float interval_ms)
        {
            float rate = interval_ms / 1000f;
            return speed * rate;
        }

        public static float getDistance(float rx, float ry)
        {
            return (float)Math.Sqrt(rx * rx + ry * ry);
        }
        public static float getDistance(float x1, float y1, float x2, float y2)
        {
            float r1 = x1 - x2;
            float r2 = y1 - y2;
            return (float)Math.Sqrt(r1 * r1 + r2 * r2);
        }
        public static float getDistanceSquare(float x1, float y1, float x2, float y2)
        {
            float r1 = x1 - x2;
            float r2 = y1 - y2;
            return r1 * r1 + r2 * r2;
        }
        public static float getDistance(IVector2 v1, IVector2 v2)
        {
            float r1 = v1.X - v2.X;
            float r2 = v1.Y - v2.Y;
            return (float)Math.Sqrt(r1 * r1 + r2 * r2);
        }
        public static float getDistanceSquare(IVector2 v1, IVector2 v2)
        {
            float r1 = v1.X - v2.X;
            float r2 = v1.Y - v2.Y;
            return r1 * r1 + r2 * r2;
        }
        /// <summary>
        /// 得到弧度
        /// </summary>
        /// <param name="dx">x向量</param>
        /// <param name="dy">y向量</param>
        /// <returns></returns>
        public static float getDegree(float dx, float dy)
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
        public static float getDegree(float x1, float y1, float x2, float y2)
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
        public static float getDegree(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var d1 = (float)Math.Atan2(y2 - y1, x2 - x1);
            var d2 = (float)Math.Atan2(y3 - y1, x3 - x1);
            return d2 - d1;
        }

        /**
         * 得到弧度
         * @param v 向量
         * @return
         */
        public static float getDegree(IVector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }
        public static float getDegree(IVector2 a, IVector2 b)
        {
            return (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        }


        /**
         * 将2个向量相加得到一个新的向量
         * @param a
         * @param b
         * @return
         */
        public static VectorObject2 vectorAdd(IVector2 a, IVector2 b)
        {
            VectorObject2 v = new VectorObject2();
            v.X = a.X + b.X;
            v.Y = a.Y + b.Y;
            return v;
        }

        /**
         * 将2个向量相减得到一个新的向量
         * @param a
         * @param b
         * @return
         */
        public static VectorObject2 vectorSub(IVector2 a, IVector2 b)
        {
            VectorObject2 v = new VectorObject2();
            v.X = a.X - b.X;
            v.Y = a.Y - b.Y;
            return v;
        }

        /**
         * 将一个向量加上新的向量，得到一个新的向量
         * @param a
         * @param degree
         * @param distance
         * @return
         */
        public static VectorObject2 vectorAdd(IVector2 a, float degree, float distance)
        {
            VectorObject2 v = new VectorObject2();
            v.X = a.X;
            v.Y = a.Y;
            movePolar(v, degree, distance);
            return v;
        }

        /**
         * 把一个向量向自己本身的方向相加，得到一个新的向量
         * @param a
         * @param distance
         * @return
         */
        public static VectorObject2 vectorAdd(IVector2 a, float distance)
        {
            VectorObject2 v = new VectorObject2();
            v.X = a.X;
            v.Y = a.Y;
            movePolar(v, getDegree(v), distance);
            return v;
        }

        /**
         * 将一个向量缩放一定比率后，得到一个新的向量
         * @param a
         * @param scale
         * @return
         */
        public static VectorObject2 vectorScale(IVector2 a, float scale)
        {
            VectorObject2 v = new VectorObject2();
            v.X = a.X * scale;
            v.Y = a.Y * scale;
            return v;
        }


        public static float vectorDot(IVector2 v1, IVector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }
        public static float vectorDot(float x1, float y1, float x2, float y2)
        {
            return x1 * x2 + y1 * y2;
        }

        /// <summary>
        /// 挤压移动单位，某个单位在集合中移动，碰撞并挤开其他单位
        /// </summary>
        /// <param name="vectors"></param>
        /// <param name="obj"></param>
        /// <param name="angle"></param>
        /// <param name="distance"></param>
        /// <param name="depth"></param>
        /// <param name="max_depth"></param>
        public static void moveImpact(ICollection<IRoundObject> vectors, IRoundObject obj, float angle, float distance, int depth, int max_depth)
        {
            float dx = (float)(Math.Cos(angle) * distance);
            float dy = (float)(Math.Sin(angle) * distance);
            obj.X += dx;
            obj.Y += dy;
            if (depth < max_depth)
            {
                foreach (IRoundObject o in vectors)
                {
                    if (!o.Equals(obj))
                    {
                        float dr = getDistance(o, obj) - o.RadiusSize - obj.RadiusSize;
                        if (dr < 0)
                        {
                            float ta = getDegree(obj.X, obj.Y, o.X, o.Y);
                            moveImpact(vectors, o, ta, -dr, depth + 1, max_depth);
                        }
                    }
                }
            }
        }
    }

}
