using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public struct Triangle
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public BoundingBox AABB
        {
            get
            {
                var min = Vector3.Min(A, Vector3.Min(B, C));
                var max = Vector3.Max(A, Vector3.Max(B, C));
                return new BoundingBox(min, max);
            }
        }
    }
    public class Triangles
    {
        public Triangle[] Vertices;
        public int Count => Vertices.Length;

        public void ForEachTrangles(Action<Triangle> action)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                action(Vertices[i]);
            }
        }
    }
}
