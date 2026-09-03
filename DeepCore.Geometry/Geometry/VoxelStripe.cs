using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.Geometry;
namespace DeepCore.Geometry
{
    /// <summary>
    /// 圆角条状物
    /// </summary>
    public struct VoxelStripe
    {
        public Vector2 LineP;
        public Vector2 LineQ;
        public float LineRaidus;
        public float Z;
        public float Height;
        public static Vector3 RandomPos(Random random, in Vector3 center, float direction, float wide, float distance)
        {
            float d_width = -(wide / 2f) + random.NextFloat() * wide;
            float d_distance = (-distance / 2f) + random.NextFloat() * distance;
            Vector2 p0 = center;
            VectorHelper.MovePolar(ref p0, direction, d_distance);
            VectorHelper.MovePolar(ref p0, direction + CMath.RADIANS_90, d_width);
            return p0;
        }
        public static VoxelStripe InitFromCenter(in Vector3 center, float direction, float wide, float distance, float height)
        {
            float d_width = wide / 2f;
            float d_distance = distance / 2f;
            Vector2 p0 = center;
            Vector2 p1 = center;
            VectorHelper.MovePolar(ref p0, direction, -d_distance);
            VectorHelper.MovePolar(ref p1, direction, d_distance);

            var ret = new VoxelStripe();
            ret.Z = center.Z;
            ret.Height = height;
            ret.LineRaidus = d_width;
            ret.LineP = p0;
            ret.LineQ = p1;
            return ret;
        }
        /// <summary>
        /// 深度，宽度反转
        /// </summary>
        public static VoxelStripe InitFromWideCenter(in Vector3 center, float direction, float wide, float distance, float height)
        {
            float d_width = wide / 2f;
            float d_angle = direction + CMath.PI_DIV_2;
            float d_distance = distance / 2f;
            Vector2 p0 = center;
            Vector2 p1 = center;
            VectorHelper.MovePolar(ref p0, d_angle, -d_width);
            VectorHelper.MovePolar(ref p1, d_angle, +d_width);

            var ret = new VoxelStripe();
            ret.Z = center.Z;
            ret.Height = height;
            ret.LineRaidus = d_width;
            ret.LineP = p0;
            ret.LineQ = p1;
            return ret;
        }
        public static VoxelStripe InitFromRay(in Vector3 center, float direction, float wide, float distance, float height)
        {
            float d_width = wide / 2f;
            float d_distance = distance;
            Vector2 p1 = center;
            VectorHelper.MovePolar(ref p1, direction, d_distance);

            var ret = new VoxelStripe();
            ret.Z = center.Z;
            ret.Height = height;
            ret.LineRaidus = d_width;
            ret.LineP = center;
            ret.LineQ = p1;
            return ret;
        }
        public static VoxelStripe InitFromPoint(in Vector3 lineP, in Vector2 lineQ, float lineRadius, float height)
        {
            var ret = new VoxelStripe();
            ret.Z = lineP.Z;
            ret.Height = height;
            ret.LineRaidus = lineRadius;
            ret.LineP = lineP;
            ret.LineQ = lineQ;
            return ret;
        }

        /// <summary>
        /// 圆角粗线段
        /// </summary>
        /// <param name="o"></param>
        /// <param name="result"></param>
        public bool Intersects(in VoxelCylinder o)
        {
            {
                float sz2 = this.Z + this.Height;
                float dz2 = o.Center.Z + o.Height;
                if (sz2 < o.Center.Z) { return false; }
                if (this.Z > dz2) { return false; }
            }

            if (CMath.IntersectRound(LineP.X, LineP.Y, LineRaidus, o.Center.X, o.Center.Y, o.Radius))
            {
                return true;
            }
            if (CMath.IntersectRound(LineQ.X, LineQ.Y, LineRaidus, o.Center.X, o.Center.Y, o.Radius))
            {
                return true;
            }
            return CollisionMath.CircleLineCollide(o.Center, o.Radius + LineRaidus, LineP, LineQ);
        }
        //         public bool Intersects(in VoxelCylinder o)
        //         {
        //             return Intersects(in o);
        //         }
    }

    /// <summary>
    /// 方角条状物
    /// </summary>
    public struct VoxelRectStripe
    {
        public Vector2 LineP;
        public Vector2 LineQ;
        public float LineRaidus;
        public float Z;
        public float Height;
        public Vector2[] Polygon;

        public static Vector3 RandomPos(Random random, in Vector3 center, float direction, float wide, float distance)
        {
            float d_width = -(wide / 2f) + random.NextFloat() * wide;
            float d_distance = (-distance / 2f) + random.NextFloat() * distance;
            Vector2 p0 = center;
            VectorHelper.MovePolar(ref p0, direction, d_distance);
            VectorHelper.MovePolar(ref p0, direction + CMath.RADIANS_90, d_width);
            return p0;
        }

        public static VoxelRectStripe InitFromCenter(in Vector3 center, float direction, float wide, float distance, float height)
        {
            float d_width = wide / 2f;
            float d_distance = distance / 2f;
            Vector2 p0 = center;
            Vector2 p1 = center;
            VectorHelper.MovePolar(ref p0, direction, -d_distance);
            VectorHelper.MovePolar(ref p1, direction, d_distance);
            var ret = new VoxelRectStripe();
            ret.Z = center.Z;
            ret.Height = height;
            ret.LineRaidus = d_width;
            ret.LineP = p0;
            ret.LineQ = p1;
            ret.Polygon = new Vector2[4];
            ret.Polygon[0] = p0;
            ret.Polygon[1] = p0;
            ret.Polygon[2] = p1;
            ret.Polygon[3] = p1;
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[0], direction + CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[1], direction - CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[2], direction + CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[3], direction - CMath.PI_DIV_2, d_width);
            return ret;
        }
        public static VoxelRectStripe InitFromRay(in Vector3 center, float direction, float wide, float distance, float height)
        {
            float d_width = wide / 2f;
            Vector2 p0 = center;
            Vector2 p1 = center;
            VectorHelper.MovePolar(ref p1, direction, distance);
            var ret = new VoxelRectStripe();
            ret.Z = center.Z;
            ret.Height = height;
            ret.LineRaidus = d_width;
            ret.LineP = p0;
            ret.LineQ = p1;
            ret.Polygon = new Vector2[4];
            ret.Polygon[0] = p0;
            ret.Polygon[1] = p0;
            ret.Polygon[2] = p1;
            ret.Polygon[3] = p1;
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[0], direction + CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[1], direction - CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[2], direction + CMath.PI_DIV_2, d_width);
            DeepCore.Geometry.VectorHelper.MovePolar(ref ret.Polygon[3], direction - CMath.PI_DIV_2, d_width);
            return ret;
        }

        /// <summary>
        /// 方角粗线段
        /// </summary>
        public bool Intersects(in VoxelCylinder o)
        {
            {
                float sz2 = this.Z + this.Height;
                float dz2 = o.Center.Z + o.Height;
                if (sz2 < o.Center.Z) { return false; }
                if (this.Z > dz2) { return false; }
            }
            if (DeepCore.Geometry.CollisionMath.CircleLineCollide(o.Center, LineRaidus + o.Radius, LineP, LineQ))
            {
                if (CollisionMath.PointInPolygon(o.Center, this.Polygon))
                {
                    return true;
                }
                for (int i = 0; i < this.Polygon.Length - 1; ++i)
                {
                    if (CollisionMath.CircleLineCollide(o.Center, o.Radius, this.Polygon[i], this.Polygon[i + 1]))
                    {
                        return true;
                    }
                }
                if (CollisionMath.CircleLineCollide(o.Center, o.Radius, this.Polygon[3], this.Polygon[0]))
                {
                    return true;
                }
            }
            return false;
        }
        public bool Contains(in Vector3 point)
        {
            float sz2 = this.Z + this.Height;
            if (sz2 < point.Z) { return false; }
            if (this.Z > point.Z) { return false; }
            if (CollisionMath.PointInPolygon(point, this.Polygon))
            {
                return true;
            }
            return false;
        }
        //         public bool Intersects(in VoxelCylinder o)
        //         {
        //             return Intersects(in o);
        //         }
    }
}
