// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DeepCore.Geometry
{
    internal class PlaneHelper
    {
        /// <summary>
        /// Returns a value indicating what side (positive/negative) of a plane a point is
        /// </summary>
        /// <param name="point">The point to check with</param>
        /// <param name="plane">The plane to check against</param>
        /// <returns>Greater than zero if on the positive side, less than zero if on the negative size, 0 otherwise</returns>
        public static float ClassifyPoint(in Vector3 point, in Plane plane)
        {
            return point.X * plane.Normal.X + point.Y * plane.Normal.Y + point.Z * plane.Normal.Z + plane.D;
        }

        /// <summary>
        /// Returns the perpendicular distance from a point to a plane
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <param name="plane">The place to check</param>
        /// <returns>The perpendicular distance from the point to the plane</returns>
        public static float PerpendicularDistance(in Vector3 point, in Plane plane)
        {
            // dist = (ax + by + cz + d) / sqrt(a*a + b*b + c*c)
            return (float)Math.Abs((plane.Normal.X * point.X + plane.Normal.Y * point.Y + plane.Normal.Z * point.Z)
                                    / Math.Sqrt(plane.Normal.X * plane.Normal.X + plane.Normal.Y * plane.Normal.Y + plane.Normal.Z * plane.Normal.Z));
        }
    }



    public struct Plane : IEquatable<Plane>
    {
        #region Public Fields

        public System.Numerics.Plane Value;

        public float D
        {
            get => Value.D;
            set { Value.D = value; }
        }
        public Vector3 Normal
        {
            get => Value.Normal;
            set { Value.Normal = value.Value; }
        }

        #endregion Public Fields


        #region Constructors

        public Plane(Vector4 value)
            : this(new Vector3(value.X, value.Y, value.Z), value.W)
        {

        }

        public Plane(Vector3 normal, float d)
        {
            Value.Normal = normal.Value;
            Value.D = d;
        }

        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            Vector3 cross = Vector3.Cross(ab, ac);
            Value.Normal = Vector3.Normalize(cross).Value;
            Value.D = -(Vector3.Dot(Value.Normal, a));
        }

        public Plane(float a, float b, float c, float d)
            : this(new Vector3(a, b, c), d)
        {

        }

        public static implicit operator Plane(in System.Numerics.Plane value)
        {
            return new Plane() { Value = value };
        }

        #endregion Constructors


        #region Public Methods

        public float Dot(in Vector4 value)
        {
            return System.Numerics.Plane.Dot(Value, value.Value);
            // return ((((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z)) + (this.D * value.W));
        }

        public void Dot(in Vector4 value, out float result)
        {
            result = System.Numerics.Plane.Dot(Value, value.Value);
            // result = (((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z)) + (this.D * value.W);
        }

        public float DotCoordinate(in Vector3 value)
        {
            return System.Numerics.Plane.DotCoordinate(Value, value.Value);
            // return ((((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z)) + this.D);
        }

        public void DotCoordinate(in Vector3 value, out float result)
        {
            result = System.Numerics.Plane.DotCoordinate(Value, value.Value);
            //result = (((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z)) + this.D;
        }

        public float DotNormal(in Vector3 value)
        {
            return System.Numerics.Plane.DotNormal(Value, value.Value);
            //return (((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z));
        }

        public void DotNormal(in Vector3 value, out float result)
        {
            result = System.Numerics.Plane.DotNormal(Value, value.Value);
            //   result = ((this.Normal.X * value.X) + (this.Normal.Y * value.Y)) + (this.Normal.Z * value.Z);
        }

        /// <summary>
        /// Transforms a normalized plane by a matrix.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(in Plane plane, in Matrix matrix)
        {
            return System.Numerics.Plane.Transform(plane.Value, matrix.Value);
            //             Plane result;
            //             Transform(ref plane, ref matrix, out result);
            //             return result;
        }

        /// <summary>
        /// Transforms a normalized plane by a matrix.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <param name="result">The transformed plane.</param>
        public static void Transform(in Plane plane, in Matrix matrix, out Plane result)
        {
            result = System.Numerics.Plane.Transform(plane.Value, matrix.Value);
            // See "Transforming Normals" in http://www.glprogramming.com/red/appendixf.html
            // for an explanation of how this works.

            //             Matrix transformedMatrix;
            //             Matrix.Invert(ref matrix, out transformedMatrix);
            //             Matrix.Transpose(ref transformedMatrix, out transformedMatrix);
            // 
            //             var vector = new Vector4(plane.Normal, plane.D);
            // 
            //             Vector4 transformedVector;
            //             Vector4.Transform(ref vector, ref transformedMatrix, out transformedVector);
            // 
            //             result = new Plane(transformedVector);
        }

        /// <summary>
        /// Transforms a normalized plane by a quaternion rotation.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(in Plane plane, in Quaternion rotation)
        {
         return System.Numerics.Plane.Transform(plane.Value, rotation.Value);
            //             Plane result;
            //             Transform(ref plane, ref rotation, out result);
            //             return result;
        }

        /// <summary>
        /// Transforms a normalized plane by a quaternion rotation.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <param name="result">The transformed plane.</param>
        public static void Transform(in Plane plane, in Quaternion rotation, out Plane result)
        {
            result = System.Numerics.Plane.Transform(plane.Value, rotation.Value);
            //             Vector3.Transform(ref plane.Normal, ref rotation, out result.Normal);
            //             result.D = plane.D;
        }

        public void Normalize()
        {
            this.Value = System.Numerics.Plane.Normalize(Value);
            //             float factor;
            //             Vector3 normal = Normal;
            //             Normal = Vector3.Normalize(Normal);
            //             factor = (float)Math.Sqrt(Normal.X * Normal.X + Normal.Y * Normal.Y + Normal.Z * Normal.Z) /
            //                     (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            //             D = D * factor;
        }

        public static Plane Normalize(in Plane value)
        {
           return System.Numerics.Plane.Normalize(value.Value);
            //             Plane ret;
            //             Normalize(ref value, out ret);
            //             return ret;
        }

        public static void Normalize(in Plane value, out Plane result)
        {
            result = System.Numerics.Plane.Normalize(value.Value);
            //             float factor;
            //             result.Normal = Vector3.Normalize(value.Normal);
            //             factor = (float)Math.Sqrt(result.Normal.X * result.Normal.X + result.Normal.Y * result.Normal.Y + result.Normal.Z * result.Normal.Z) /
            //                     (float)Math.Sqrt(value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z);
            //             result.D = value.D * factor;
        }

        public static bool operator !=(in Plane plane1, in Plane plane2)
        {
            return !plane1.Equals(in plane2);
        }

        public static bool operator ==(in Plane plane1, in Plane plane2)
        {
            return plane1.Equals(in plane2);
        }

        public override bool Equals(object other)
        {
            return (other is Plane) ? this.Equals((Plane)other) : false;
        }
        public bool Equals(in Plane other)
        {
            return Value.Equals(other.Value);
            //return ((Normal == other.Normal) && (D == other.D));
        }
        public bool Equals(Plane other)
        {
            return Value.Equals(other.Value);
            //return ((Normal == other.Normal) && (D == other.D));
        }

        public override int GetHashCode()
        {
            return Normal.GetHashCode() ^ D.GetHashCode();
        }

        public PlaneIntersectionType Intersects(in BoundingBox box)
        {
            return box.Intersects(this);
        }

        public void Intersects(in BoundingBox box, out PlaneIntersectionType result)
        {
            box.Intersects(in this, out result);
        }

        public PlaneIntersectionType Intersects(in BoundingFrustum frustum)
        {
            return frustum.Intersects(this);
        }

        public PlaneIntersectionType Intersects(in BoundingSphere sphere)
        {
            return sphere.Intersects(this);
        }

        public void Intersects(in BoundingSphere sphere, out PlaneIntersectionType result)
        {
            sphere.Intersects(in this, out result);
        }

        internal PlaneIntersectionType Intersects(in Vector3 point)
        {
            float distance;
            DotCoordinate(in point, out distance);

            if (distance > 0)
                return PlaneIntersectionType.Front;

            if (distance < 0)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.Normal.DebugDisplayString, "  ",
                    this.D.ToString()
                    );
            }
        }

        public override string ToString()
        {
            return "{Normal:" + Normal + " D:" + D + "}";
        }

        #endregion
    }
}

