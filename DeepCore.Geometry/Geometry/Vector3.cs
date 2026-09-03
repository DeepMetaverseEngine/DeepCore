// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.Serialization;

namespace DeepCore.Geometry
{

    public struct Vector3 : IEquatable<Vector3>
    {

        #region Private Fields

        private static readonly Vector3 nan = new Vector3(float.NaN, float.NaN, float.NaN);
        private static readonly Vector3 zero = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 one = new Vector3(1f, 1f, 1f);
        private static readonly Vector3 unitX = new Vector3(1f, 0f, 0f);
        private static readonly Vector3 unitY = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 unitZ = new Vector3(0f, 0f, 1f);
        private static readonly Vector3 up = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 down = new Vector3(0f, -1f, 0f);
        private static readonly Vector3 right = new Vector3(1f, 0f, 0f);
        private static readonly Vector3 left = new Vector3(-1f, 0f, 0f);
        private static readonly Vector3 forward = new Vector3(0f, 0f, -1f);
        private static readonly Vector3 backward = new Vector3(0f, 0f, 1f);

        #endregion

        #region Public Fields

        public System.Numerics.Vector3 Value;
        public float X { get => Value.X; set { Value.X = value; } }
        public float Y { get => Value.Y; set { Value.Y = value; } }
        public float Z { get => Value.Z; set { Value.Z = value; } }

        public Vector2 XY { get => new Vector2(X, Y); }
        public Vector2 XZ { get => new Vector2(X, Z); }

        #endregion

        #region Public Properties

        public bool IsNaN { get => float.IsNaN(X) || float.IsNaN(Y) || float.IsNaN(Z); }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components NaN
        /// </summary>
        public static Vector3 NaN
        {
            get { return nan; }
        }


        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 0, 0.
        /// </summary>
        public static Vector3 Zero
        {
            get { return zero; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 1, 1, 1.
        /// </summary>
        public static Vector3 One
        {
            get { return one; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 1, 0, 0.
        /// </summary>
        public static Vector3 UnitX
        {
            get { return unitX; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 1, 0.
        /// </summary>
        public static Vector3 UnitY
        {
            get { return unitY; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 0, 1.
        /// </summary>
        public static Vector3 UnitZ
        {
            get { return unitZ; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 1, 0.
        /// </summary>
        public static Vector3 Up
        {
            get { return up; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, -1, 0.
        /// </summary>
        public static Vector3 Down
        {
            get { return down; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 1, 0, 0.
        /// </summary>
        public static Vector3 Right
        {
            get { return right; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components -1, 0, 0.
        /// </summary>
        public static Vector3 Left
        {
            get { return left; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 0, -1.
        /// </summary>
        public static Vector3 Forward
        {
            get { return forward; }
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> with components 0, 0, 1.
        /// </summary>
        public static Vector3 Backward
        {
            get { return backward; }
        }

        #endregion

        #region Internal Properties

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.X.ToString(), "  ",
                    this.Y.ToString(), "  ",
                    this.Z.ToString()
                );
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a 3d vector with X, Y and Z from three values.
        /// </summary>
        /// <param name="x">The x coordinate in 3d-space.</param>
        /// <param name="y">The y coordinate in 3d-space.</param>
        /// <param name="z">The z coordinate in 3d-space.</param>
        public Vector3(float x, float y, float z)
        {
            this.Value.X = x;
            this.Value.Y = y;
            this.Value.Z = z;
        }

        /// <summary>
        /// Constructs a 3d vector with X, Y and Z set to the same value.
        /// </summary>
        /// <param name="value">The x, y and z coordinates in 3d-space.</param>
        public Vector3(float value)
        {
            this.Value.X = value;
            this.Value.Y = value;
            this.Value.Z = value;
        }

        /// <summary>
        /// Constructs a 3d vector with X, Y from <see cref="Vector2"/> and Z from a scalar.
        /// </summary>
        /// <param name="value">The x and y coordinates in 3d-space.</param>
        /// <param name="z">The z coordinate in 3d-space.</param>
        public Vector3(Vector2 value, float z)
        {
            this.Value.X = value.X;
            this.Value.Y = value.Y;
            this.Value.Z = z;
        }

        public static implicit operator Vector3(in Geometry.Vector2 value)
        {
            return new Vector3(value.X, value.Y, 0);
        }
        public static implicit operator Vector3(in Geometry.Vector4 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static implicit operator Vector3(in System.Numerics.Vector2 value)
        {
            return new Vector3(value.X, value.Y, 0);
        }
        public static implicit operator Vector3(in System.Numerics.Vector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
        public static implicit operator Vector3(in System.Numerics.Vector4 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <returns>The result of the vector addition.</returns>
        public static Vector3 Add(in Vector3 value1, in Vector3 value2)
        {
            //             Vector3 ret = value1;
            //             ret.X = value1.X + value2.X;
            //             ret.Y = value1.Y + value2.Y;
            //             ret.Z = value1.Z + value2.Z;
            //             return ret;
            return System.Numerics.Vector3.Add(value1.Value, value2.Value);
        }

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and
        /// <paramref name="value2"/>, storing the result of the
        /// addition in <paramref name="result"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <param name="result">The result of the vector addition.</param>
        public static void Add(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Add(value1.Value, value2.Value);
            //             result.Value.X = value1.X + value2.X;
            //             result.Value.Y = value1.Y + value2.Y;
            //             result.Value.Z = value1.Z + value2.Z;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 3d-triangle.
        /// </summary>
        /// <param name="value1">The first vector of 3d-triangle.</param>
        /// <param name="value2">The second vector of 3d-triangle.</param>
        /// <param name="value3">The third vector of 3d-triangle.</param>
        /// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 3d-triangle.</param>
        /// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 3d-triangle.</param>
        /// <returns>The cartesian translation of barycentric coordinates.</returns>
        public static Vector3 Barycentric(in Vector3 value1, in Vector3 value2, in Vector3 value3, float amount1, float amount2)
        {
            return new Vector3(
                MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
                MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 3d-triangle.
        /// </summary>
        /// <param name="value1">The first vector of 3d-triangle.</param>
        /// <param name="value2">The second vector of 3d-triangle.</param>
        /// <param name="value3">The third vector of 3d-triangle.</param>
        /// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 3d-triangle.</param>
        /// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 3d-triangle.</param>
        /// <param name="result">The cartesian translation of barycentric coordinates as an output parameter.</param>
        public static void Barycentric(in Vector3 value1, in Vector3 value2, in Vector3 value3, float amount1, float amount2, out Vector3 result)
        {
            result.Value.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
            result.Value.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
            result.Value.Z = MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains CatmullRom interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector in interpolation.</param>
        /// <param name="value2">The second vector in interpolation.</param>
        /// <param name="value3">The third vector in interpolation.</param>
        /// <param name="value4">The fourth vector in interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of CatmullRom interpolation.</returns>
        public static Vector3 CatmullRom(in Vector3 value1, in Vector3 value2, in Vector3 value3, Vector3 value4, float amount)
        {
            return new Vector3(
                MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
                MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains CatmullRom interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector in interpolation.</param>
        /// <param name="value2">The second vector in interpolation.</param>
        /// <param name="value3">The third vector in interpolation.</param>
        /// <param name="value4">The fourth vector in interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">The result of CatmullRom interpolation as an output parameter.</param>
        public static void CatmullRom(in Vector3 value1, in Vector3 value2, in Vector3 value3, in Vector3 value4, float amount, out Vector3 result)
        {
            result.Value.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
            result.Value.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
            result.Value.Z = MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount);
        }

        /// <summary>
        /// Clamps the specified value within a range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <returns>The clamped value.</returns>
        public static Vector3 Clamp(in Vector3 value1, in Vector3 min, in Vector3 max)
        {
            return System.Numerics.Vector3.Clamp(value1.Value, min.Value, max.Value);
            //             return new Vector3(
            //                 MathHelper.Clamp(value1.X, min.X, max.X),
            //                 MathHelper.Clamp(value1.Y, min.Y, max.Y),
            //                 MathHelper.Clamp(value1.Z, min.Z, max.Z));
        }

        /// <summary>
        /// Clamps the specified value within a range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="result">The clamped value as an output parameter.</param>
        public static void Clamp(in Vector3 value1, in Vector3 min, in Vector3 max, out Vector3 result)
        {
            result = System.Numerics.Vector3.Clamp(value1.Value, min.Value, max.Value);
            //             result.Value.X = MathHelper.Clamp(value1.X, min.X, max.X);
            //             result.Value.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
            //             result.Value.Z = MathHelper.Clamp(value1.Z, min.Z, max.Z);
        }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product of two vectors.</returns>
        public static Vector3 Cross(in Vector3 vector1, in Vector3 vector2)
        {
            return System.Numerics.Vector3.Cross(vector1.Value, vector2.Value);
            //             Cross(in vector1, in vector2, out var vector3);
            //             return vector3;
        }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <param name="result">The cross product of two vectors as an output parameter.</param>
        public static void Cross(in Vector3 vector1, in Vector3 vector2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Cross(vector1.Value, vector2.Value);
            //             var x = vector1.Y * vector2.Z - vector2.Y * vector1.Z;
            //             var y = -(vector1.X * vector2.Z - vector2.X * vector1.Z);
            //             var z = vector1.X * vector2.Y - vector2.X * vector1.Y;
            //             result.Value.X = x;
            //             result.Value.Y = y;
            //             result.Value.Z = z;
        }

        /// <summary>
        /// Returns the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The distance between two vectors.</returns>
        public static float Distance(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Distance(value1.Value, value2.Value);
            //             float result;
            //             DistanceSquared(in value1, in value2, out result);
            //             return (float)Math.Sqrt(result);
        }

        /// <summary>
        /// Returns the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The distance between two vectors as an output parameter.</param>
        public static void Distance(in Vector3 value1, in Vector3 value2, out float result)
        {
            result = System.Numerics.Vector3.Distance(value1.Value, value2.Value);
            //             DistanceSquared(in value1, in value2, out result);
            //             result = (float)Math.Sqrt(result);
        }

        /// <summary>
        /// Returns the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The squared distance between two vectors.</returns>
        public static float DistanceSquared(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.DistanceSquared(value1.Value, value2.Value);
            //             float result;
            //             DistanceSquared(in value1, in value2, out result);
            //             return result;
        }

        /// <summary>
        /// Returns the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The squared distance between two vectors as an output parameter.</param>
        public static void DistanceSquared(in Vector3 value1, in Vector3 value2, out float result)
        {
            result = System.Numerics.Vector3.DistanceSquared(value1.Value, value2.Value);
            //             result = (value1.X - value2.X) * (value1.X - value2.X) +
            //                      (value1.Y - value2.Y) * (value1.Y - value2.Y) +
            //                      (value1.Z - value2.Z) * (value1.Z - value2.Z);
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by the components of another <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Divisor <see cref="Vector3"/>.</param>
        /// <returns>The result of dividing the vectors.</returns>
        public static Vector3 Divide(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Divide(value1.Value, value2.Value);
            //             var ret = value1;
            //             ret.X /= value2.X;
            //             ret.Y /= value2.Y;
            //             ret.Z /= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="divider">Divisor scalar.</param>
        /// <returns>The result of dividing a vector by a scalar.</returns>
        public static Vector3 Divide(in Vector3 value1, float divider)
        {
            return System.Numerics.Vector3.Divide(value1.Value, divider);
            //             var ret = value1;
            //             float factor = 1 / divider;
            //             ret.X *= factor;
            //             ret.Y *= factor;
            //             ret.Z *= factor;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="divider">Divisor scalar.</param>
        /// <param name="result">The result of dividing a vector by a scalar as an output parameter.</param>
        public static void Divide(in Vector3 value1, float divider, out Vector3 result)
        {
            result = System.Numerics.Vector3.Divide(value1.Value, divider);
            //             float factor = 1 / divider;
            //             result.Value.X = value1.X * factor;
            //             result.Value.Y = value1.Y * factor;
            //             result.Value.Z = value1.Z * factor;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by the components of another <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Divisor <see cref="Vector3"/>.</param>
        /// <param name="result">The result of dividing the vectors as an output parameter.</param>
        public static void Divide(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Divide(value1.Value, value2.Value);
            //             result.Value.X = value1.X / value2.X;
            //             result.Value.Y = value1.Y / value2.Y;
            //             result.Value.Z = value1.Z / value2.Z;
        }

        /// <summary>
        /// Returns a dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        public static float Dot(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Dot(value1.Value, value2.Value);
            //return value1.X * value2.X + value1.Y * value2.Y + value1.Z * value2.Z;
        }

        /// <summary>
        /// Returns a dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The dot product of two vectors as an output parameter.</param>
        public static void Dot(in Vector3 value1, in Vector3 value2, out float result)
        {
            result = System.Numerics.Vector3.Dot(value1.Value, value2.Value);
            //result = value1.X * value2.X + value1.Y * value2.Y + value1.Z * value2.Z;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Vector3 other)
                return this.Value.Equals(other.Value);
            return false;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="other">The <see cref="Vector3"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(in Vector3 other)
        {
            return Value.Equals(other.Value);
        }
        public bool Equals(Vector3 other)
        {
            return Value.Equals(other.Value);
        }
        /// <summary>
        /// Gets the hash code of this <see cref="Vector3"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Vector3"/>.</returns>
        public override int GetHashCode()
        {
            return (int)(this.X + this.Y + this.Z);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains hermite spline interpolation.
        /// </summary>
        /// <param name="value1">The first position vector.</param>
        /// <param name="tangent1">The first tangent vector.</param>
        /// <param name="value2">The second position vector.</param>
        /// <param name="tangent2">The second tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The hermite spline interpolation vector.</returns>
        public static Vector3 Hermite(in Vector3 value1, in Vector3 tangent1, in Vector3 value2, in Vector3 tangent2, float amount)
        {
            return new Vector3(MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount),
                               MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount),
                               MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains hermite spline interpolation.
        /// </summary>
        /// <param name="value1">The first position vector.</param>
        /// <param name="tangent1">The first tangent vector.</param>
        /// <param name="value2">The second position vector.</param>
        /// <param name="tangent2">The second tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">The hermite spline interpolation vector as an output parameter.</param>
        public static void Hermite(in Vector3 value1, in Vector3 tangent1, in Vector3 value2, in Vector3 tangent2, float amount, out Vector3 result)
        {
            result.Value.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
            result.Value.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
            result.Value.Z = MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount);
        }

        /// <summary>
        /// Returns the length of this <see cref="Vector3"/>.
        /// </summary>
        /// <returns>The length of this <see cref="Vector3"/>.</returns>
        public float Length()
        {
            return Value.Length();
            //             float result = DistanceSquared(this, zero);
            //             return (float)Math.Sqrt(result);
        }

        /// <summary>
        /// Returns the squared length of this <see cref="Vector3"/>.
        /// </summary>
        /// <returns>The squared length of this <see cref="Vector3"/>.</returns>
        public float LengthSquared()
        {
            return Value.LengthSquared();
            //return DistanceSquared(this, zero);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains linear interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
        /// <returns>The result of linear interpolation of the specified vectors.</returns>
        public static Vector3 Lerp(in Vector3 value1, in Vector3 value2, float amount)
        {
            return System.Numerics.Vector3.Lerp(value1.Value, value2.Value, amount);
            //             return new Vector3(
            //                 MathHelper.Lerp(value1.X, value2.X, amount),
            //                 MathHelper.Lerp(value1.Y, value2.Y, amount),
            //                 MathHelper.Lerp(value1.Z, value2.Z, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains linear interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
        /// <param name="result">The result of linear interpolation of the specified vectors as an output parameter.</param>
        public static void Lerp(in Vector3 value1, in Vector3 value2, float amount, out Vector3 result)
        {
            result = System.Numerics.Vector3.Lerp(value1.Value, value2.Value, amount);
            //             result.Value.X = MathHelper.Lerp(value1.X, value2.X, amount);
            //             result.Value.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
            //             result.Value.Z = MathHelper.Lerp(value1.Z, value2.Z, amount);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a maximal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The <see cref="Vector3"/> with maximal values from the two vectors.</returns>
        public static Vector3 Max(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Max(value1.Value, value2.Value);
            //             return new Vector3(
            //                 MathHelper.Max(value1.X, value2.X),
            //                 MathHelper.Max(value1.Y, value2.Y),
            //                 MathHelper.Max(value1.Z, value2.Z));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a maximal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The <see cref="Vector3"/> with maximal values from the two vectors as an output parameter.</param>
        public static void Max(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Max(value1.Value, value2.Value);
            //             result.Value.X = MathHelper.Max(value1.X, value2.X);
            //             result.Value.Y = MathHelper.Max(value1.Y, value2.Y);
            //             result.Value.Z = MathHelper.Max(value1.Z, value2.Z);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a minimal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The <see cref="Vector3"/> with minimal values from the two vectors.</returns>
        public static Vector3 Min(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Min(value1.Value, value2.Value);
            //             return new Vector3(
            //                 MathHelper.Min(value1.X, value2.X),
            //                 MathHelper.Min(value1.Y, value2.Y),
            //                 MathHelper.Min(value1.Z, value2.Z));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a minimal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The <see cref="Vector3"/> with minimal values from the two vectors as an output parameter.</param>
        public static void Min(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Min(value1.Value, value2.Value);
            //             result.Value.X = MathHelper.Min(value1.X, value2.X);
            //             result.Value.Y = MathHelper.Min(value1.Y, value2.Y);
            //             result.Value.Z = MathHelper.Min(value1.Z, value2.Z);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a multiplication of two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <returns>The result of the vector multiplication.</returns>
        public static Vector3 Multiply(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Multiply(value1.Value, value2.Value);
            //             value1.X *= value2.X;
            //             value1.Y *= value2.Y;
            //             value1.Z *= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a multiplication of <see cref="Vector3"/> and a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <returns>The result of the vector multiplication with a scalar.</returns>
        public static Vector3 Multiply(in Vector3 value1, float scaleFactor)
        {
            return System.Numerics.Vector3.Multiply(value1.Value, scaleFactor);
            //             value1.X *= scaleFactor;
            //             value1.Y *= scaleFactor;
            //             value1.Z *= scaleFactor;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a multiplication of <see cref="Vector3"/> and a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">The result of the multiplication with a scalar as an output parameter.</param>
        public static void Multiply(in Vector3 value1, float scaleFactor, out Vector3 result)
        {
            result = System.Numerics.Vector3.Multiply(value1.Value, scaleFactor);
            //             result.Value.X = value1.X * scaleFactor;
            //             result.Value.Y = value1.Y * scaleFactor;
            //             result.Value.Z = value1.Z * scaleFactor;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a multiplication of two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <param name="result">The result of the vector multiplication as an output parameter.</param>
        public static void Multiply(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Multiply(value1.Value, value2.Value);
            //             result.Value.X = value1.X * value2.X;
            //             result.Value.Y = value1.Y * value2.Y;
            //             result.Value.Z = value1.Z * value2.Z;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains the specified vector inversion.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <returns>The result of the vector inversion.</returns>
        public static Vector3 Negate(in Vector3 value)
        {
            return System.Numerics.Vector3.Negate(value.Value);
            //             value = new Vector3(-value.X, -value.Y, -value.Z);
            //             return value;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains the specified vector inversion.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <param name="result">The result of the vector inversion as an output parameter.</param>
        public static void Negate(in Vector3 value, out Vector3 result)
        {
            result = System.Numerics.Vector3.Negate(value.Value);
            //             result.Value.X = -value.X;
            //             result.Value.Y = -value.Y;
            //             result.Value.Z = -value.Z;
        }

        /// <summary>
        /// Turns this <see cref="Vector3"/> to a unit vector with the same direction.
        /// </summary>
        public void Normalize()
        {
            Value = System.Numerics.Vector3.Normalize(Value);
            //Normalize(ref this, out this);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a normalized values from another vector.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <returns>Unit vector.</returns>
        public static Vector3 Normalize(in Vector3 value)
        {
            return System.Numerics.Vector3.Normalize(value.Value);
            //             Normalize(ref value, out value);
            //             return value;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a normalized values from another vector.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <param name="result">Unit vector as an output parameter.</param>
        public static void Normalize(in Vector3 value, out Vector3 result)
        {
            result = System.Numerics.Vector3.Normalize(value.Value);
            //             float factor = Distance(value, zero);
            //             factor = 1f / factor;
            //             result.Value.X = value.X * factor;
            //             result.Value.Y = value.Y * factor;
            //             result.Value.Z = value.Z * factor;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source <see cref="Vector3"/>.</param>
        /// <param name="normal">Reflection normal.</param>
        /// <returns>Reflected vector.</returns>
        public static Vector3 Reflect(in Vector3 vector, in Vector3 normal)
        {
            return System.Numerics.Vector3.Reflect(vector.Value, normal.Value);
            // I is the original array
            //             // N is the normal of the incident plane
            //             // R = I - (2 * N * ( DotProduct[ I,N] ))
            //             Vector3 reflectedVector;
            //             // inline the dotProduct here instead of calling method
            //             float dotProduct = ((vector.X * normal.X) + (vector.Y * normal.Y)) + (vector.Z * normal.Z);
            //             reflectedVector.Value.X = vector.X - (2.0f * normal.X) * dotProduct;
            //             reflectedVector.Value.Y = vector.Y - (2.0f * normal.Y) * dotProduct;
            //             reflectedVector.Value.Z = vector.Z - (2.0f * normal.Z) * dotProduct;
            // 
            //             return reflectedVector;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source <see cref="Vector3"/>.</param>
        /// <param name="normal">Reflection normal.</param>
        /// <param name="result">Reflected vector as an output parameter.</param>
        public static void Reflect(in Vector3 vector, in Vector3 normal, out Vector3 result)
        {
            result = System.Numerics.Vector3.Reflect(vector.Value, normal.Value);
            // I is the original array
            // N is the normal of the incident plane
            // R = I - (2 * N * ( DotProduct[ I,N] ))

            // inline the dotProduct here instead of calling method
            //             float dotProduct = ((vector.X * normal.X) + (vector.Y * normal.Y)) + (vector.Z * normal.Z);
            //             result.Value.X = vector.X - (2.0f * normal.X) * dotProduct;
            //             result.Value.Y = vector.Y - (2.0f * normal.Y) * dotProduct;
            //             result.Value.Z = vector.Z - (2.0f * normal.Z) * dotProduct;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains cubic interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <param name="amount">Weighting value.</param>
        /// <returns>Cubic interpolation of the specified vectors.</returns>
        public static Vector3 SmoothStep(in Vector3 value1, in Vector3 value2, float amount)
        {
            return new Vector3(
                MathHelper.SmoothStep(value1.X, value2.X, amount),
                MathHelper.SmoothStep(value1.Y, value2.Y, amount),
                MathHelper.SmoothStep(value1.Z, value2.Z, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains cubic interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <param name="amount">Weighting value.</param>
        /// <param name="result">Cubic interpolation of the specified vectors as an output parameter.</param>
        public static void SmoothStep(in Vector3 value1, in Vector3 value2, float amount, out Vector3 result)
        {
            result.Value.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
            result.Value.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
            result.Value.Z = MathHelper.SmoothStep(value1.Z, value2.Z, amount);
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains subtraction of on <see cref="Vector3"/> from a another.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <returns>The result of the vector subtraction.</returns>
        public static Vector3 Subtract(in Vector3 value1, in Vector3 value2)
        {
            return System.Numerics.Vector3.Subtract(value1.Value, value2.Value);
            //             value1.X -= value2.X;
            //             value1.Y -= value2.Y;
            //             value1.Z -= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains subtraction of on <see cref="Vector3"/> from a another.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/>.</param>
        /// <param name="value2">Source <see cref="Vector3"/>.</param>
        /// <param name="result">The result of the vector subtraction as an output parameter.</param>
        public static void Subtract(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result = System.Numerics.Vector3.Subtract(value1.Value, value2.Value);
            //             result.Value.X = value1.X - value2.X;
            //             result.Value.Y = value1.Y - value2.Y;
            //             result.Value.Z = value1.Z - value2.Z;
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="Vector3"/> in the format:
        /// {X:[<see cref="X"/>] Y:[<see cref="Y"/>] Z:[<see cref="Z"/>]}
        /// </summary>
        /// <returns>A <see cref="String"/> representation of this <see cref="Vector3"/>.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append("}");
            return sb.ToString();
        }

        #region Transform

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of vector(position.X,position.Y,position.Z,1) by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="position">Source <see cref="Vector3"/>.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <returns>Transformed <see cref="Vector3"/>.</returns>
        public static Vector3 Transform(in Vector3 position, in Matrix matrix)
        {
            return System.Numerics.Vector3.Transform(position.Value, matrix.Value);
            //             Transform(in position, in matrix, out position);
            //             return position;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of vector(position.X,position.Y,position.Z,1) by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="position">Source <see cref="Vector3"/>.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="result">Transformed <see cref="Vector3"/> as an output parameter.</param>
        public static void Transform(in Vector3 position, in Matrix matrix, out Vector3 result)
        {
            result = System.Numerics.Vector3.Transform(position.Value, matrix.Value);
            //             var x = (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41;
            //             var y = (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42;
            //             var z = (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43;
            //             result.Value.X = x;
            //             result.Value.Y = y;
            //             result.Value.Z = z;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of vector(position.X,position.Y,position.Z,0) by the specified <see cref="Quaternion"/>, representing the rotation.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <returns>Transformed <see cref="Vector3"/>.</returns>
        public static Vector3 Transform(in Vector3 value, in Quaternion rotation)
        {
            return System.Numerics.Vector3.Transform(value.Value, rotation.Value);
            //             Vector3 result;
            //             Transform(ref value, ref rotation, out result);
            //             return result;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of vector(position.X,position.Y,position.Z,0) by the specified <see cref="Quaternion"/>, representing the rotation.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/>.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="result">Transformed <see cref="Vector3"/> as an output parameter.</param>
        public static void Transform(in Vector3 value, in Quaternion rotation, out Vector3 result)
        {
            result = System.Numerics.Vector3.Transform(value.Value, rotation.Value);
            //             float x = 2 * (rotation.Y * value.Z - rotation.Z * value.Y);
            //             float y = 2 * (rotation.Z * value.X - rotation.X * value.Z);
            //             float z = 2 * (rotation.X * value.Y - rotation.Y * value.X);
            // 
            //             result.Value.X = value.X + x * rotation.W + (rotation.Y * z - rotation.Z * y);
            //             result.Value.Y = value.Y + y * rotation.W + (rotation.Z * x - rotation.X * z);
            //             result.Value.Z = value.Z + z * rotation.W + (rotation.X * y - rotation.Y * x);
        }

        /// <summary>
        /// Apply transformation on vectors within array of <see cref="Vector3"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector3"/> should be written.</param>
        /// <param name="length">The number of vectors to be transformed.</param>
        public static void Transform(Vector3[] sourceArray, int sourceIndex, in Matrix matrix, Vector3[] destinationArray, int destinationIndex, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < sourceIndex + length)
                throw new ArgumentException("Source array length is lesser than sourceIndex + length");
            if (destinationArray.Length < destinationIndex + length)
                throw new ArgumentException("Destination array length is lesser than destinationIndex + length");

            // TODO: Are there options on some platforms to implement a vectorized version of this?

            for (var i = 0; i < length; i++)
            {
                var position = sourceArray[sourceIndex + i];
                //                 destinationArray[destinationIndex + i] =
                //                     new Vector3(
                //                         (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                //                         (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                //                         (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);
                destinationArray[destinationIndex + i] = System.Numerics.Vector3.Transform(position.Value, matrix.Value);
            }
        }

        /// <summary>
        /// Apply transformation on vectors within array of <see cref="Vector3"/> by the specified <see cref="Quaternion"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector3"/> should be written.</param>
        /// <param name="length">The number of vectors to be transformed.</param>
        public static void Transform(Vector3[] sourceArray, int sourceIndex, in Quaternion rotation, Vector3[] destinationArray, int destinationIndex, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < sourceIndex + length)
                throw new ArgumentException("Source array length is lesser than sourceIndex + length");
            if (destinationArray.Length < destinationIndex + length)
                throw new ArgumentException("Destination array length is lesser than destinationIndex + length");

            // TODO: Are there options on some platforms to implement a vectorized version of this?

            for (var i = 0; i < length; i++)
            {
                var position = sourceArray[sourceIndex + i];

                //                 float x = 2 * (rotation.Y * position.Z - rotation.Z * position.Y);
                //                 float y = 2 * (rotation.Z * position.X - rotation.X * position.Z);
                //                 float z = 2 * (rotation.X * position.Y - rotation.Y * position.X);
                // 
                //                 destinationArray[destinationIndex + i] =
                //                     new Vector3(
                //                         position.X + x * rotation.W + (rotation.Y * z - rotation.Z * y),
                //                         position.Y + y * rotation.W + (rotation.Z * x - rotation.X * z),
                //                         position.Z + z * rotation.W + (rotation.X * y - rotation.Y * x));
                destinationArray[destinationIndex + i] = System.Numerics.Vector3.Transform(position.Value, rotation.Value);
            }
        }

        /// <summary>
        /// Apply transformation on all vectors within array of <see cref="Vector3"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void Transform(Vector3[] sourceArray, in Matrix matrix, Vector3[] destinationArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException("Destination array length is lesser than source array length");

            // TODO: Are there options on some platforms to implement a vectorized version of this?

            for (var i = 0; i < sourceArray.Length; i++)
            {
                var position = sourceArray[i];
                //                 destinationArray[i] =
                //                     new Vector3(
                //                         (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                //                         (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                //                         (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);
                destinationArray[i] = System.Numerics.Vector3.Transform(position.Value, matrix.Value);
            }
        }

        /// <summary>
        /// Apply transformation on all vectors within array of <see cref="Vector3"/> by the specified <see cref="Quaternion"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void Transform(Vector3[] sourceArray, in Quaternion rotation, Vector3[] destinationArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException("Destination array length is lesser than source array length");

            // TODO: Are there options on some platforms to implement a vectorized version of this?

            for (var i = 0; i < sourceArray.Length; i++)
            {
                var position = sourceArray[i];

                //                 float x = 2 * (rotation.Y * position.Z - rotation.Z * position.Y);
                //                 float y = 2 * (rotation.Z * position.X - rotation.X * position.Z);
                //                 float z = 2 * (rotation.X * position.Y - rotation.Y * position.X);
                // 
                //                 destinationArray[i] =
                //                     new Vector3(
                //                         position.X + x * rotation.W + (rotation.Y * z - rotation.Z * y),
                //                         position.Y + y * rotation.W + (rotation.Z * x - rotation.X * z),
                //                         position.Z + z * rotation.W + (rotation.X * y - rotation.Y * x));
                destinationArray[i] = System.Numerics.Vector3.Transform(position.Value, rotation.Value);
            }
        }

        #endregion

        #region TransformNormal

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of the specified normal by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="normal">Source <see cref="Vector3"/> which represents a normal vector.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <returns>Transformed normal.</returns>
        public static Vector3 TransformNormal(in Vector3 normal, in Matrix matrix)
        {
            return System.Numerics.Vector3.TransformNormal(normal.Value, matrix.Value);
            //             TransformNormal(in normal, in matrix, out normal);
            //             return normal;
        }

        /// <summary>
        /// Creates a new <see cref="Vector3"/> that contains a transformation of the specified normal by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="normal">Source <see cref="Vector3"/> which represents a normal vector.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="result">Transformed normal as an output parameter.</param>
        public static void TransformNormal(in Vector3 normal, in Matrix matrix, out Vector3 result)
        {
            result = System.Numerics.Vector3.TransformNormal(normal.Value, matrix.Value);
            //             var x = (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31);
            //             var y = (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32);
            //             var z = (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33);
            //             result.Value.X = x;
            //             result.Value.Y = y;
            //             result.Value.Z = z;
        }

        /// <summary>
        /// Apply transformation on normals within array of <see cref="Vector3"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector3"/> should be written.</param>
        /// <param name="length">The number of normals to be transformed.</param>
        public static void TransformNormal(Vector3[] sourceArray,
         int sourceIndex,
         in Matrix matrix,
         Vector3[] destinationArray,
         int destinationIndex,
         int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < sourceIndex + length)
                throw new ArgumentException("Source array length is lesser than sourceIndex + length");
            if (destinationArray.Length < destinationIndex + length)
                throw new ArgumentException("Destination array length is lesser than destinationIndex + length");

            for (int x = 0; x < length; x++)
            {
                var normal = sourceArray[sourceIndex + x];

                //                 destinationArray[destinationIndex + x] =
                //                      new Vector3(
                //                         (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
                //                         (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
                //                         (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33));
                destinationArray[destinationIndex + x] = System.Numerics.Vector3.TransformNormal(normal.Value, matrix.Value);
            }
        }

        /// <summary>
        /// Apply transformation on all normals within array of <see cref="Vector3"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void TransformNormal(Vector3[] sourceArray, in Matrix matrix, Vector3[] destinationArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException("Destination array length is lesser than source array length");

            for (var i = 0; i < sourceArray.Length; i++)
            {
                var normal = sourceArray[i];

                //                 destinationArray[i] =
                //                     new Vector3(
                //                         (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
                //                         (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
                //                         (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33));
                destinationArray[i] = System.Numerics.Vector3.TransformNormal(normal.Value, matrix.Value);
            }
        }

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Compares whether two <see cref="Vector3"/> instances are equal.
        /// </summary>
        /// <param name="value1"><see cref="Vector3"/> instance on the left of the equal sign.</param>
        /// <param name="value2"><see cref="Vector3"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value == value2.Value;
            //             return value1.X == value2.X
            //                 && value1.Y == value2.Y
            //                 && value1.Z == value2.Z;
        }

        /// <summary>
        /// Compares whether two <see cref="Vector3"/> instances are not equal.
        /// </summary>
        /// <param name="value1"><see cref="Vector3"/> instance on the left of the not equal sign.</param>
        /// <param name="value2"><see cref="Vector3"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>	
        public static bool operator !=(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value != value2.Value;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/> on the left of the add sign.</param>
        /// <param name="value2">Source <see cref="Vector3"/> on the right of the add sign.</param>
        /// <returns>Sum of the vectors.</returns>
        public static Vector3 operator +(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value + value2.Value;
            //             value1.X += value2.X;
            //             value1.Y += value2.Y;
            //             value1.Z += value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Inverts values in the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/> on the right of the sub sign.</param>
        /// <returns>Result of the inversion.</returns>
        public static Vector3 operator -(in Vector3 value)
        {
            return -value.Value;
            //             value = new Vector3(-value.X, -value.Y, -value.Z);
            //             return value;
        }

        /// <summary>
        /// Subtracts a <see cref="Vector3"/> from a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/> on the left of the sub sign.</param>
        /// <param name="value2">Source <see cref="Vector3"/> on the right of the sub sign.</param>
        /// <returns>Result of the vector subtraction.</returns>
        public static Vector3 operator -(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value - value2.Value;
            //             value1.X -= value2.X;
            //             value1.Y -= value2.Y;
            //             value1.Z -= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/> on the left of the mul sign.</param>
        /// <param name="value2">Source <see cref="Vector3"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication.</returns>
        public static Vector3 operator *(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value * value2.Value;
            //             value1.X *= value2.X;
            //             value1.Y *= value2.Y;
            //             value1.Z *= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="value">Source <see cref="Vector3"/> on the left of the mul sign.</param>
        /// <param name="scaleFactor">Scalar value on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Vector3 operator *(in Vector3 value, float scaleFactor)
        {
            return value.Value * scaleFactor;
            //             value.X *= scaleFactor;
            //             value.Y *= scaleFactor;
            //             value.Z *= scaleFactor;
            //             return value;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="scaleFactor">Scalar value on the left of the mul sign.</param>
        /// <param name="value">Source <see cref="Vector3"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Vector3 operator *(float scaleFactor, in Vector3 value)
        {
            return scaleFactor * value.Value;
            //             value.X *= scaleFactor;
            //             value.Y *= scaleFactor;
            //             value.Z *= scaleFactor;
            //             return value;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by the components of another <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/> on the left of the div sign.</param>
        /// <param name="value2">Divisor <see cref="Vector3"/> on the right of the div sign.</param>
        /// <returns>The result of dividing the vectors.</returns>
        public static Vector3 operator /(in Vector3 value1, in Vector3 value2)
        {
            return value1.Value / value2.Value;
            //             value1.X /= value2.X;
            //             value1.Y /= value2.Y;
            //             value1.Z /= value2.Z;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector3"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector3"/> on the left of the div sign.</param>
        /// <param name="divider">Divisor scalar on the right of the div sign.</param>
        /// <returns>The result of dividing a vector by a scalar.</returns>
        public static Vector3 operator /(in Vector3 value1, float divider)
        {
            return value1.Value / divider;
            //             float factor = 1 / divider;
            //             value1.X *= factor;
            //             value1.Y *= factor;
            //             value1.Z *= factor;
            //             return value1;
        }

        #endregion

        static Vector3()
        {
            Parser.RegistParser(new Vector3Parser());
        }
    }

    public class Vector3Parser : TypeParserAdapter<Vector3>
    {
        public override string ToString(Vector3 obj)
        {
            return $"{obj.X},{obj.Y},{obj.Z}";
        }

        public override bool TryParse(string text, out Vector3 value)
        {
            value = default(Vector3);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var components = text.Split(',');

            float x = 0, y = 0, z = 0;

            if (components.Length > 0 && float.TryParse(components[0].Trim(), out var _x))
            {
                x = _x;
            }
            if (components.Length > 1 && float.TryParse(components[1].Trim(), out var _y))
            {
                y = _y;
            }
            if (components.Length > 2 && float.TryParse(components[2].Trim(), out var _z))
            {
                z = _z;
            }

            value = new Vector3(x, y, z);
            return true;

        }
    }
}
