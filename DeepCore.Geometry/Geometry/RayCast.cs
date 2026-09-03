using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static DeepCore.Geometry.RayCast;

namespace DeepCore.Geometry
{
    //一点，和一个方向向量（两点求差）确定一条射线，
    public struct RayCast
    {
        public Vector3 center;
        public Vector3 normal;
        public float distance;
        public RayCast(Vector3 c, Vector3 t, float len)
        {
            this.center = c;
            this.normal = t;
            this.distance = len;
        }

        public static Vector3 RayPlaneIntersection(in Vector3 ray_center, in Vector3 ray_normal, in Vector3 plane_point, in Vector3 plane_normal)
        {
            Vector3 p;
            float t;
            t = (Vector3.Dot(plane_normal, plane_point) - Vector3.Dot(plane_normal, ray_center)) / Vector3.Dot(plane_normal, ray_normal);
            p = ray_center + t * ray_normal;
            return p;
        }
        public static Vector3 RayPlaneIntersection(in RayCast ray, in Plane plane)
        {
            return RayPlaneIntersection(in ray.center, in ray.normal, in plane.point, in plane.normal);
        }
        public static Vector3? RayBoundingBoxIntersection(in Vector3 ray_center, in Vector3 ray_normal, in Vector3 box_min, in Vector3 box_max)
        {
            const float Epsilon = 1e-6f;

            float? tMin = null, tMax = null;

            if (Math.Abs(ray_normal.X) < Epsilon)
            {
                if (ray_center.X < box_min.X || ray_center.X > box_max.X)
                    return null;
            }
            else
            {
                tMin = (box_min.X - ray_center.X) / ray_normal.X;
                tMax = (box_max.X - ray_center.X) / ray_normal.X;

                if (tMin > tMax)
                {
                    var temp = tMin;
                    tMin = tMax;
                    tMax = temp;
                }
            }

            if (Math.Abs(ray_normal.Y) < Epsilon)
            {
                if (ray_center.Y < box_min.Y || ray_center.Y > box_max.Y)
                    return null;
            }
            else
            {
                var tMinY = (box_min.Y - ray_center.Y) / ray_normal.Y;
                var tMaxY = (box_max.Y - ray_center.Y) / ray_normal.Y;

                if (tMinY > tMaxY)
                {
                    var temp = tMinY;
                    tMinY = tMaxY;
                    tMaxY = temp;
                }

                if ((tMin.HasValue && tMin > tMaxY) || (tMax.HasValue && tMinY > tMax))
                    return null;

                if (!tMin.HasValue || tMinY > tMin) tMin = tMinY;
                if (!tMax.HasValue || tMaxY < tMax) tMax = tMaxY;
            }

            if (Math.Abs(ray_normal.Z) < Epsilon)
            {
                if (ray_center.Z < box_min.Z || ray_center.Z > box_max.Z)
                    return null;
            }
            else
            {
                var tMinZ = (box_min.Z - ray_center.Z) / ray_normal.Z;
                var tMaxZ = (box_max.Z - ray_center.Z) / ray_normal.Z;

                if (tMinZ > tMaxZ)
                {
                    var temp = tMinZ;
                    tMinZ = tMaxZ;
                    tMaxZ = temp;
                }

                if ((tMin.HasValue && tMin > tMaxZ) || (tMax.HasValue && tMinZ > tMax))
                    return null;

                if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
            }

            // having a positive tMin and a negative tMax means the ray is inside the box
            // we expect the intesection distance to be 0 in that case
            if ((tMin.HasValue && tMin < 0) && tMax > 0) return ray_center;

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            //return tMin;
            var src = ray_center;
            var dis = CalculateAngle(ray_center, ray_center + ray_normal);
            return Vector3.Lerp(ray_center, ray_center + ray_normal, tMin.Value / dis);
        }
        public static Vector3? RayBoundingBoxIntersection(in RayCast ray, in DeepCore.Geometry.BoundingBox box)
        {
            return RayBoundingBoxIntersection(in ray.center, in ray.normal, in box.Min, in box.Max);
        }

        public static float CalculateAngle(Vector3 first, Vector3 second)
        {
            CalculateAngle(in first, in second, out float result);
            return result;
        }

        public static void CalculateAngle(in Vector3 first, in Vector3 second, out float result)
        {
            Vector3.Dot(in first, in second, out float temp);
            result = (float)Math.Acos(MathHelper.Clamp(temp / (first.Length() * second.Length()), -1.0f, 1.0f));
        }

        //一点，和一个法向量确定一个平面
        public struct Plane
        {
            public Vector3 point;
            public Vector3 normal;
            public Plane(Vector3 p, Vector3 n)
            {
                this.point = p;
                this.normal = n;
            }
        }

    }
}
