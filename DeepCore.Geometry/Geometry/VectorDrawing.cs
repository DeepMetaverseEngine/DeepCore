using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public static class VectorDrawing
    {
        public static Vector3 VectorOffset(this Vector3 center, float radius, float rotate = 0)
        {
            var v0 = new System.Numerics.Vector3();
            DeepCore.Geometry.VectorHelper.MovePolar(ref v0.X, ref v0.Y, rotate, radius);
            return new Vector3(center.X + v0.X, center.Y + v0.Y, center.Z);
        }
        public static Vector3 VectorOffsetZ(this Vector3 center, float radius, float rotate = 0)
        {
            var v0 = new System.Numerics.Vector3();
            DeepCore.Geometry.VectorHelper.MovePolar(ref v0.X, ref v0.Z, rotate, radius);
            return new Vector3(center.X + v0.X, center.Y , center.Z + v0.Z);
        }
        public static void ForStar(this Vector3 center, Action<Vector3> list, float radius, float rotate = 0)
        {
            var delta = MathHelper.TwoPi / 5;
            var start = rotate - MathHelper.Pi / 2;
            var v0 = new System.Numerics.Vector3();
            var v1 = new System.Numerics.Vector3();
            var v2 = new System.Numerics.Vector3();
            var v3 = new System.Numerics.Vector3();
            var v4 = new System.Numerics.Vector3();
            DeepCore.Geometry.VectorHelper.MovePolar(ref v0.X, ref v0.Y, start + delta * 0, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v1.X, ref v1.Y, start + delta * 1, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v2.X, ref v2.Y, start + delta * 2, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v3.X, ref v3.Y, start + delta * 3, radius);
            DeepCore.Geometry.VectorHelper.MovePolar(ref v4.X, ref v4.Y, start + delta * 4, radius);
            list(new Vector3(center.X + v0.X, center.Y + v0.Y, center.Z));
            list(new Vector3(center.X + v2.X, center.Y + v2.Y, center.Z));
            list(new Vector3(center.X + v4.X, center.Y + v4.Y, center.Z));
            list(new Vector3(center.X + v1.X, center.Y + v1.Y, center.Z));
            list(new Vector3(center.X + v3.X, center.Y + v3.Y, center.Z));
        }
        public static Vector3[] ToStar(this Vector3 center, float radius, float rotate = 0)
        {
            var ret = new Vector3[5];
            int i = 0;
            ForStar( center, v3 => { ret[i++] = v3; }, radius, rotate);
            return ret;
        }
        public static void ForBoundingBox(Action<Vector3> list, Vector3 min, Vector3 max)
        {
            // TOP
            list(new Vector3(min.X, max.Z, min.Y));
            list(new Vector3(max.X, max.Z, min.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            // Bottom
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(min.X, min.Z, max.Y));
            // FORTH                             
            list(new Vector3(min.X, min.Z, max.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            // BACK                              
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, max.Z, min.Y));
            list(new Vector3(min.X, max.Z, min.Y));
            // LEFT                              
            list(new Vector3(min.X, min.Z, min.Y));
            list(new Vector3(min.X, min.Z, max.Y));
            list(new Vector3(min.X, max.Z, max.Y));
            list(new Vector3(min.X, max.Z, min.Y));
            // RIGHT                             
            list(new Vector3(max.X, min.Z, min.Y));
            list(new Vector3(max.X, min.Z, max.Y));
            list(new Vector3(max.X, max.Z, max.Y));
            list(new Vector3(max.X, max.Z, min.Y));
        }

    }
}
