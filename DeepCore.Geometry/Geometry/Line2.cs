using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore.Geometry
{
    public struct Line2 : IEquatable<Line2>
    {
        public Vector2 P;
        public Vector2 Q;

        public Line2(Vector2 p, Vector2 q)
        {
            this.P = p;
            this.Q = q;
        }
        public Line2(float x1, float y1, float x2, float y2)
        {
            this.P = new Vector2(x1, y1);
            this.Q = new Vector2(x2, y2);
        }

        public bool Equals(Line2 other)
        {
            return this.P.Equals(other.P) && this.Q.Equals(other.Q);
        }
    }
}
