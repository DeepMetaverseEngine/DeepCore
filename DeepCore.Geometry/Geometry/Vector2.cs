// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;

namespace DeepCore.Geometry
{


    public struct Vector2 : IEquatable<Vector2>
    {
        #region Private Fields

        private static readonly Vector2 nanVector = new Vector2(float.NaN, float.NaN);
        private static readonly Vector2 zeroVector = new Vector2(0f, 0f);
        private static readonly Vector2 unitVector = new Vector2(1f, 1f);
        private static readonly Vector2 unitXVector = new Vector2(1f, 0f);
        private static readonly Vector2 unitYVector = new Vector2(0f, 1f);

        #endregion

        #region Public Fields

        public System.Numerics.Vector2 Value;
        public float X { get => Value.X; set { Value.X = value; } }
        public float Y { get => Value.Y; set { Value.Y = value; } }

        public static implicit operator Vector2(in Geometry.Vector3 value)
        {
            return new Vector2(value.X, value.Y);
        }
        public static implicit operator Vector2(in Geometry.Vector4 value)
        {
            return new Vector2(value.X, value.Y);
        }
        public static implicit operator Vector2(in System.Numerics.Vector2 value)
        {
            return new Vector2(value.X, value.Y);
        }
        public static implicit operator Vector2(in System.Numerics.Vector3 value)
        {
            return new Vector2(value.X, value.Y);
        }
        public static implicit operator Vector2(in System.Numerics.Vector4 value)
        {
            return new Vector2(value.X, value.Y);
        }

        #endregion

        #region Properties

        public bool IsNaN { get => float.IsNaN(X) || float.IsNaN(Y); }

        /// <summary>
        /// Returns a <see cref="Vector2"/> with components NaN, Nan
        /// </summary>
        public static Vector2 NaN
        {
            get { return nanVector; }
        }

        /// <summary>
        /// Returns a <see cref="Vector2"/> with components 0, 0.
        /// </summary>
        public static Vector2 Zero
        {
            get { return zeroVector; }
        }

        /// <summary>
        /// Returns a <see cref="Vector2"/> with components 1, 1.
        /// </summary>
        public static Vector2 One
        {
            get { return unitVector; }
        }

        /// <summary>
        /// Returns a <see cref="Vector2"/> with components 1, 0.
        /// </summary>
        public static Vector2 UnitX
        {
            get { return unitXVector; }
        }

        /// <summary>
        /// Returns a <see cref="Vector2"/> with components 0, 1.
        /// </summary>
        public static Vector2 UnitY
        {
            get { return unitYVector; }
        }

        #endregion

        #region Internal Properties

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.X.ToString(), "  ",
                    this.Y.ToString()
                );
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a 2d vector with X and Y from two values.
        /// </summary>
        /// <param name="x">The x coordinate in 2d-space.</param>
        /// <param name="y">The y coordinate in 2d-space.</param>
        public Vector2(float x, float y)
        {
            this.Value.X = x;
            this.Value.Y = y;
        }

        /// <summary>
        /// Constructs a 2d vector with X and Y set to the same value.
        /// </summary>
        /// <param name="value">The x and y coordinates in 2d-space.</param>
        public Vector2(float value)
        {
            this.Value.X = value;
            this.Value.Y = value;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Inverts values in the specified <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/> on the right of the sub sign.</param>
        /// <returns>Result of the inversion.</returns>
        public static Vector2 operator -(in Vector2 value)
        {
            //             value.X = -value.X;
            //             value.Y = -value.Y;
            //             return value;
            return -value.Value;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/> on the left of the add sign.</param>
        /// <param name="value2">Source <see cref="Vector2"/> on the right of the add sign.</param>
        /// <returns>Sum of the vectors.</returns>
        public static Vector2 operator +(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value + value2.Value;
            //             value1.X += value2.X;
            //             value1.Y += value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Subtracts a <see cref="Vector2"/> from a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/> on the left of the sub sign.</param>
        /// <param name="value2">Source <see cref="Vector2"/> on the right of the sub sign.</param>
        /// <returns>Result of the vector subtraction.</returns>
        public static Vector2 operator -(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value - value2.Value;
            //             value1.X -= value2.X;
            //             value1.Y -= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/> on the left of the mul sign.</param>
        /// <param name="value2">Source <see cref="Vector2"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication.</returns>
        public static Vector2 operator *(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value * value2.Value;
            //             value1.X *= value2.X;
            //             value1.Y *= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/> on the left of the mul sign.</param>
        /// <param name="scaleFactor">Scalar value on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Vector2 operator *(in Vector2 value, float scaleFactor)
        {
            return value.Value * scaleFactor;
            //             value.X *= scaleFactor;
            //             value.Y *= scaleFactor;
            //             return value;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="scaleFactor">Scalar value on the left of the mul sign.</param>
        /// <param name="value">Source <see cref="Vector2"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Vector2 operator *(float scaleFactor, in Vector2 value)
        {
            return scaleFactor * value.Value;
            //             value.X *= scaleFactor;
            //             value.Y *= scaleFactor;
            //             return value;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by the components of another <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/> on the left of the div sign.</param>
        /// <param name="value2">Divisor <see cref="Vector2"/> on the right of the div sign.</param>
        /// <returns>The result of dividing the vectors.</returns>
        public static Vector2 operator /(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value / value2.Value;
            //             value1.X /= value2.X;
            //             value1.Y /= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/> on the left of the div sign.</param>
        /// <param name="divider">Divisor scalar on the right of the div sign.</param>
        /// <returns>The result of dividing a vector by a scalar.</returns>
        public static Vector2 operator /(in Vector2 value1, float divider)
        {
            return value1.Value / divider;
            //             float factor = 1 / divider;
            //             value1.X *= factor;
            //             value1.Y *= factor;
            //             return value1;
        }

        /// <summary>
        /// Compares whether two <see cref="Vector2"/> instances are equal.
        /// </summary>
        /// <param name="value1"><see cref="Vector2"/> instance on the left of the equal sign.</param>
        /// <param name="value2"><see cref="Vector2"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value == value2.Value;
            // return value1.X == value2.X && value1.Y == value2.Y;
        }

        /// <summary>
        /// Compares whether two <see cref="Vector2"/> instances are not equal.
        /// </summary>
        /// <param name="value1"><see cref="Vector2"/> instance on the left of the not equal sign.</param>
        /// <param name="value2"><see cref="Vector2"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>	
        public static bool operator !=(in Vector2 value1, in Vector2 value2)
        {
            return value1.Value != value2.Value;
            // return value1.X != value2.X || value1.Y != value2.Y;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <returns>The result of the vector addition.</returns>
        public static Vector2 Add(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Add(value1.Value, value2.Value);
            //             value1.X += value2.X;
            //             value1.Y += value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and
        /// <paramref name="value2"/>, storing the result of the
        /// addition in <paramref name="result"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <param name="result">The result of the vector addition.</param>
        public static void Add(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Add(value1.Value, value2.Value);
            //             result.Value.X = value1.X + value2.X;
            //             result.Value.Y = value1.Y + value2.Y;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 2d-triangle.
        /// </summary>
        /// <param name="value1">The first vector of 2d-triangle.</param>
        /// <param name="value2">The second vector of 2d-triangle.</param>
        /// <param name="value3">The third vector of 2d-triangle.</param>
        /// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 2d-triangle.</param>
        /// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 2d-triangle.</param>
        /// <returns>The cartesian translation of barycentric coordinates.</returns>
        public static Vector2 Barycentric(in Vector2 value1, in Vector2 value2, in Vector2 value3, float amount1, float amount2)
        {
            return new Vector2(
                MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains the cartesian coordinates of a vector specified in barycentric coordinates and relative to 2d-triangle.
        /// </summary>
        /// <param name="value1">The first vector of 2d-triangle.</param>
        /// <param name="value2">The second vector of 2d-triangle.</param>
        /// <param name="value3">The third vector of 2d-triangle.</param>
        /// <param name="amount1">Barycentric scalar <c>b2</c> which represents a weighting factor towards second vector of 2d-triangle.</param>
        /// <param name="amount2">Barycentric scalar <c>b3</c> which represents a weighting factor towards third vector of 2d-triangle.</param>
        /// <param name="result">The cartesian translation of barycentric coordinates as an output parameter.</param>
        public static void Barycentric(in Vector2 value1, in Vector2 value2, in Vector2 value3, float amount1, float amount2, out Vector2 result)
        {
            result.Value.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
            result.Value.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains CatmullRom interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector in interpolation.</param>
        /// <param name="value2">The second vector in interpolation.</param>
        /// <param name="value3">The third vector in interpolation.</param>
        /// <param name="value4">The fourth vector in interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of CatmullRom interpolation.</returns>
        public static Vector2 CatmullRom(in Vector2 value1, in Vector2 value2, in Vector2 value3, in Vector2 value4, float amount)
        {
            return new Vector2(
                MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains CatmullRom interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector in interpolation.</param>
        /// <param name="value2">The second vector in interpolation.</param>
        /// <param name="value3">The third vector in interpolation.</param>
        /// <param name="value4">The fourth vector in interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">The result of CatmullRom interpolation as an output parameter.</param>
        public static void CatmullRom(in Vector2 value1, in Vector2 value2, in Vector2 value3, in Vector2 value4, float amount, out Vector2 result)
        {
            result.Value.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
            result.Value.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
        }

        /// <summary>
        /// Clamps the specified value within a range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <returns>The clamped value.</returns>
        public static Vector2 Clamp(in Vector2 value1, in Vector2 min, in Vector2 max)
        {
            return System.Numerics.Vector2.Clamp(value1.Value, min.Value, max.Value);
            //             return new Vector2(
            //                 MathHelper.Clamp(value1.X, min.X, max.X),
            //                 MathHelper.Clamp(value1.Y, min.Y, max.Y));
        }

        /// <summary>
        /// Clamps the specified value within a range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="result">The clamped value as an output parameter.</param>
        public static void Clamp(in Vector2 value1, in Vector2 min, in Vector2 max, out Vector2 result)
        {
            result = System.Numerics.Vector2.Clamp(value1.Value, min.Value, max.Value);
            //             result.Value.X = MathHelper.Clamp(value1.X, min.X, max.X);
            //             result.Value.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
        }

        /// <summary>
        /// Returns the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The distance between two vectors.</returns>
        public static float Distance(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Distance(value1.Value, value2.Value);
            //             float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
            //             return (float)Math.Sqrt((v1 * v1) + (v2 * v2));
        }

        /// <summary>
        /// Returns the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The distance between two vectors as an output parameter.</param>
        public static void Distance(in Vector2 value1, in Vector2 value2, out float result)
        {
            result = System.Numerics.Vector2.Distance(value1.Value, value2.Value);
            //             float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
            //             result = (float)Math.Sqrt((v1 * v1) + (v2 * v2));
        }

        /// <summary>
        /// Returns the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The squared distance between two vectors.</returns>
        public static float DistanceSquared(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.DistanceSquared(value1.Value, value2.Value);
            //             float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
            //             return (v1 * v1) + (v2 * v2);
        }

        /// <summary>
        /// Returns the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The squared distance between two vectors as an output parameter.</param>
        public static void DistanceSquared(in Vector2 value1, in Vector2 value2, out float result)
        {
            result = System.Numerics.Vector2.DistanceSquared(value1.Value, value2.Value);
            //             float v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
            //             result = (v1 * v1) + (v2 * v2);
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by the components of another <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Divisor <see cref="Vector2"/>.</param>
        /// <returns>The result of dividing the vectors.</returns>
        public static Vector2 Divide(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Divide(value1.Value, value2.Value);
            //             value1.X /= value2.X;
            //             value1.Y /= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by the components of another <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Divisor <see cref="Vector2"/>.</param>
        /// <param name="result">The result of dividing the vectors as an output parameter.</param>
        public static void Divide(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Divide(value1.Value, value2.Value);
            //             result.Value.X = value1.X / value2.X;
            //             result.Value.Y = value1.Y / value2.Y;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="divider">Divisor scalar.</param>
        /// <returns>The result of dividing a vector by a scalar.</returns>
        public static Vector2 Divide(in Vector2 value1, float divider)
        {
            return System.Numerics.Vector2.Divide(value1.Value, divider);
            //             float factor = 1 / divider;
            //             value1.X *= factor;
            //             value1.Y *= factor;
            //             return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Vector2"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="divider">Divisor scalar.</param>
        /// <param name="result">The result of dividing a vector by a scalar as an output parameter.</param>
        public static void Divide(in Vector2 value1, float divider, out Vector2 result)
        {
            result = System.Numerics.Vector2.Divide(value1.Value, divider);
            //             float factor = 1 / divider;
            //             result.Value.X = value1.X * factor;
            //             result.Value.Y = value1.Y * factor;
        }

        /// <summary>
        /// Returns a dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        public static float Dot(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Dot(value1.Value, value2.Value);
            //return (value1.X * value2.X) + (value1.Y * value2.Y);
        }

        /// <summary>
        /// Returns a dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The dot product of two vectors as an output parameter.</param>
        public static void Dot(in Vector2 value1, in Vector2 value2, out float result)
        {
            result = System.Numerics.Vector2.Dot(value1.Value, value2.Value);
            //result = (value1.X * value2.X) + (value1.Y * value2.Y);
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Vector2 v2)
            {
                return Equals(in v2);
            }
            return false;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Vector2"/>.
        /// </summary>
        /// <param name="other">The <see cref="Vector2"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(in Vector2 other)
        {
            return Value.Equals(other.Value);
            // return (X == other.X) && (Y == other.Y);
        }
        public bool Equals(Vector2 other)
        {
            return Value.Equals(other.Value);
            //return (X == other.X) && (Y == other.Y);
        }

        /// <summary>
        /// Gets the hash code of this <see cref="Vector2"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Vector2"/>.</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains hermite spline interpolation.
        /// </summary>
        /// <param name="value1">The first position vector.</param>
        /// <param name="tangent1">The first tangent vector.</param>
        /// <param name="value2">The second position vector.</param>
        /// <param name="tangent2">The second tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The hermite spline interpolation vector.</returns>
        public static Vector2 Hermite(in Vector2 value1, in Vector2 tangent1, in Vector2 value2, in Vector2 tangent2, float amount)
        {
            return new Vector2(MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount), MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains hermite spline interpolation.
        /// </summary>
        /// <param name="value1">The first position vector.</param>
        /// <param name="tangent1">The first tangent vector.</param>
        /// <param name="value2">The second position vector.</param>
        /// <param name="tangent2">The second tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">The hermite spline interpolation vector as an output parameter.</param>
        public static void Hermite(in Vector2 value1, in Vector2 tangent1, in Vector2 value2, in Vector2 tangent2, float amount, out Vector2 result)
        {
            result.Value.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
            result.Value.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
        }

        /// <summary>
        /// Returns the length of this <see cref="Vector2"/>.
        /// </summary>
        /// <returns>The length of this <see cref="Vector2"/>.</returns>
        public float Length()
        {
            return Value.Length();
            //return (float)Math.Sqrt((X * X) + (Y * Y));
        }

        /// <summary>
        /// Returns the squared length of this <see cref="Vector2"/>.
        /// </summary>
        /// <returns>The squared length of this <see cref="Vector2"/>.</returns>
        public float LengthSquared()
        {
            return Value.LengthSquared();
            //return (X * X) + (Y * Y);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains linear interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
        /// <returns>The result of linear interpolation of the specified vectors.</returns>
        public static Vector2 Lerp(in Vector2 value1, in Vector2 value2, float amount)
        {
            return System.Numerics.Vector2.Lerp(value1.Value, value2.Value, amount);
            //             return new Vector2(
            //                 MathHelper.Lerp(value1.X, value2.X, amount),
            //                 MathHelper.Lerp(value1.Y, value2.Y, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains linear interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="amount">Weighting value(between 0.0 and 1.0).</param>
        /// <param name="result">The result of linear interpolation of the specified vectors as an output parameter.</param>
        public static void Lerp(in Vector2 value1, in Vector2 value2, float amount, out Vector2 result)
        {
            result = System.Numerics.Vector2.Lerp(value1.Value, value2.Value, amount);
            //             result.Value.X = MathHelper.Lerp(value1.X, value2.X, amount);
            //             result.Value.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a maximal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The <see cref="Vector2"/> with maximal values from the two vectors.</returns>
        public static Vector2 Max(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Max(value1.Value, value2.Value);
            //             return new Vector2(value1.X > value2.X ? value1.X : value2.X,
            //                                value1.Y > value2.Y ? value1.Y : value2.Y);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a maximal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The <see cref="Vector2"/> with maximal values from the two vectors as an output parameter.</param>
        public static void Max(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Max(value1.Value, value2.Value);
            //             result.Value.X = value1.X > value2.X ? value1.X : value2.X;
            //             result.Value.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a minimal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The <see cref="Vector2"/> with minimal values from the two vectors.</returns>
        public static Vector2 Min(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Min(value1.Value, value2.Value);
            //             return new Vector2(value1.X < value2.X ? value1.X : value2.X,
            //                                value1.Y < value2.Y ? value1.Y : value2.Y);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a minimal values from the two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The <see cref="Vector2"/> with minimal values from the two vectors as an output parameter.</param>
        public static void Min(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Min(value1.Value, value2.Value);
            //             result.Value.X = value1.X < value2.X ? value1.X : value2.X;
            //             result.Value.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a multiplication of two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <returns>The result of the vector multiplication.</returns>
        public static Vector2 Multiply(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Multiply(value1.Value, value2.Value);
            //             value1.X *= value2.X;
            //             value1.Y *= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a multiplication of two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <param name="result">The result of the vector multiplication as an output parameter.</param>
        public static void Multiply(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Multiply(value1.Value, value2.Value);
            //             result.Value.X = value1.X * value2.X;
            //             result.Value.Y = value1.Y * value2.Y;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a multiplication of <see cref="Vector2"/> and a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <returns>The result of the vector multiplication with a scalar.</returns>
        public static Vector2 Multiply(in Vector2 value1, float scaleFactor)
        {
            return System.Numerics.Vector2.Multiply(value1.Value, scaleFactor);
            //             value1.X *= scaleFactor;
            //             value1.Y *= scaleFactor;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a multiplication of <see cref="Vector2"/> and a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">The result of the multiplication with a scalar as an output parameter.</param>
        public static void Multiply(in Vector2 value1, float scaleFactor, out Vector2 result)
        {
            result = System.Numerics.Vector2.Multiply(value1.Value, scaleFactor);
            //             result.Value.X = value1.X * scaleFactor;
            //             result.Value.Y = value1.Y * scaleFactor;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains the specified vector inversion.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <returns>The result of the vector inversion.</returns>
        public static Vector2 Negate(in Vector2 value)
        {
            return System.Numerics.Vector2.Negate(value.Value);
            //             value.X = -value.X;
            //             value.Y = -value.Y;
            //             return value;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains the specified vector inversion.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <param name="result">The result of the vector inversion as an output parameter.</param>
        public static void Negate(in Vector2 value, out Vector2 result)
        {
            result = System.Numerics.Vector2.Negate(value.Value);
            //             result.Value.X = -value.X;
            //             result.Value.Y = -value.Y;
        }

        /// <summary>
        /// Turns this <see cref="Vector2"/> to a unit vector with the same direction.
        /// </summary>
        public void Normalize()
        {
            Value = System.Numerics.Vector2.Normalize(Value);
            //             float val = 1.0f / (float)Math.Sqrt((X * X) + (Y * Y));
            //             X *= val;
            //             Y *= val;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a normalized values from another vector.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <returns>Unit vector.</returns>
        public static Vector2 Normalize(in Vector2 value)
        {
            return System.Numerics.Vector2.Normalize(value.Value);
            //             float val = 1.0f / (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
            //             value.X *= val;
            //             value.Y *= val;
            //             return value;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a normalized values from another vector.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <param name="result">Unit vector as an output parameter.</param>
        public static void Normalize(in Vector2 value, out Vector2 result)
        {
            result = System.Numerics.Vector2.Normalize(value.Value);
            //             float val = 1.0f / (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
            //             result.Value.X = value.X * val;
            //             result.Value.Y = value.Y * val;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source <see cref="Vector2"/>.</param>
        /// <param name="normal">Reflection normal.</param>
        /// <returns>Reflected vector.</returns>
        public static Vector2 Reflect(in Vector2 vector, in Vector2 normal)
        {
            return System.Numerics.Vector2.Reflect(vector.Value, normal.Value);
            //             Vector2 result;
            //             float val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
            //             result.Value.X = vector.X - (normal.X * val);
            //             result.Value.Y = vector.Y - (normal.Y * val);
            //             return result;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source <see cref="Vector2"/>.</param>
        /// <param name="normal">Reflection normal.</param>
        /// <param name="result">Reflected vector as an output parameter.</param>
        public static void Reflect(in Vector2 vector, in Vector2 normal, out Vector2 result)
        {
            result = System.Numerics.Vector2.Reflect(vector.Value, normal.Value);
            //             float val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
            //             result.Value.X = vector.X - (normal.X * val);
            //             result.Value.Y = vector.Y - (normal.Y * val);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains cubic interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <param name="amount">Weighting value.</param>
        /// <returns>Cubic interpolation of the specified vectors.</returns>
        public static Vector2 SmoothStep(in Vector2 value1, in Vector2 value2, float amount)
        {
            return new Vector2(
                MathHelper.SmoothStep(value1.X, value2.X, amount),
                MathHelper.SmoothStep(value1.Y, value2.Y, amount));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains cubic interpolation of the specified vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <param name="amount">Weighting value.</param>
        /// <param name="result">Cubic interpolation of the specified vectors as an output parameter.</param>
        public static void SmoothStep(in Vector2 value1, in Vector2 value2, float amount, out Vector2 result)
        {
            result.Value.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
            result.Value.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains subtraction of on <see cref="Vector2"/> from a another.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <returns>The result of the vector subtraction.</returns>
        public static Vector2 Subtract(in Vector2 value1, in Vector2 value2)
        {
            return System.Numerics.Vector2.Subtract(value1.Value, value2.Value);
            //             value1.X -= value2.X;
            //             value1.Y -= value2.Y;
            //             return value1;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains subtraction of on <see cref="Vector2"/> from a another.
        /// </summary>
        /// <param name="value1">Source <see cref="Vector2"/>.</param>
        /// <param name="value2">Source <see cref="Vector2"/>.</param>
        /// <param name="result">The result of the vector subtraction as an output parameter.</param>
        public static void Subtract(in Vector2 value1, in Vector2 value2, out Vector2 result)
        {
            result = System.Numerics.Vector2.Subtract(value1.Value, value2.Value);
            //             result.Value.X = value1.X - value2.X;
            //             result.Value.Y = value1.Y - value2.Y;
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="Vector2"/> in the format:
        /// {X:[<see cref="X"/>] Y:[<see cref="Y"/>]}
        /// </summary>
        /// <returns>A <see cref="String"/> representation of this <see cref="Vector2"/>.</returns>
        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + "}";
        }

        /// <summary>
        /// Gets a <see cref="Point"/> representation for this object.
        /// </summary>
        /// <returns>A <see cref="Point"/> representation for this object.</returns>
        public Point ToPoint()
        {
            return new Point((int)X, (int)Y);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of vector(position.X,position.Y,0,1) by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="position">Source <see cref="Vector2"/>.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <returns>Transformed <see cref="Vector2"/>.</returns>
        public static Vector2 Transform(in Vector2 position, in Matrix matrix)
        {
            return System.Numerics.Vector2.Transform(position.Value, matrix.Value);
            //return new Vector2((position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41, (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of vector(position.X,position.Y,0,1) by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="position">Source <see cref="Vector2"/>.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="result">Transformed <see cref="Vector2"/> as an output parameter.</param>
        public static void Transform(in Vector2 position, in Matrix matrix, out Vector2 result)
        {
            result = System.Numerics.Vector2.Transform(position.Value, matrix.Value);
            //             var x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
            //             var y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
            //             result.Value.X = x;
            //             result.Value.Y = y;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of vector(position.X,position.Y,0,0) by the specified <see cref="Quaternion"/>, representing the rotation.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <returns>Transformed <see cref="Vector2"/>.</returns>
        public static Vector2 Transform(in Vector2 value, in Quaternion rotation)
        {
            return System.Numerics.Vector2.Transform(value.Value, rotation.Value);
            //             Transform(ref value, ref rotation, out value);
            //             return value;
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of vector(position.X,position.Y,0,0) by the specified <see cref="Quaternion"/>, representing the rotation.
        /// </summary>
        /// <param name="value">Source <see cref="Vector2"/>.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="result">Transformed <see cref="Vector2"/> as an output parameter.</param>
        public static void Transform(in Vector2 value, in Quaternion rotation, out Vector2 result)
        {
            result = System.Numerics.Vector2.Transform(value.Value, rotation.Value);
            //             var rot1 = new Vector3(rotation.X + rotation.X, rotation.Y + rotation.Y, rotation.Z + rotation.Z);
            //             var rot2 = new Vector3(rotation.X, rotation.X, rotation.W);
            //             var rot3 = new Vector3(1, rotation.Y, rotation.Z);
            //             var rot4 = rot1 * rot2;
            //             var rot5 = rot1 * rot3;
            // 
            //             var v = new Vector2();
            //             v.X = (float)((double)value.X * (1.0 - (double)rot5.Y - (double)rot5.Z) + (double)value.Y * ((double)rot4.Y - (double)rot4.Z));
            //             v.Y = (float)((double)value.X * ((double)rot4.Y + (double)rot4.Z) + (double)value.Y * (1.0 - (double)rot4.X - (double)rot5.Z));
            //             result.Value.X = v.X;
            //             result.Value.Y = v.Y;
        }

        /// <summary>
        /// Apply transformation on vectors within array of <see cref="Vector2"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector2"/> should be written.</param>
        /// <param name="length">The number of vectors to be transformed.</param>
        public static void Transform(
            Vector2[] sourceArray,
            int sourceIndex,
            in Matrix matrix,
            Vector2[] destinationArray,
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
                var position = sourceArray[sourceIndex + x];
                //                 var destination = destinationArray[destinationIndex + x];
                //                 destination.X = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
                //                 destination.Y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
                //                 destinationArray[destinationIndex + x] = destination;
                destinationArray[destinationIndex + x] = System.Numerics.Vector2.Transform(position.Value, matrix.Value);
            }
        }

        /// <summary>
        /// Apply transformation on vectors within array of <see cref="Vector2"/> by the specified <see cref="Quaternion"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector2"/> should be written.</param>
        /// <param name="length">The number of vectors to be transformed.</param>
        public static void Transform
        (
            Vector2[] sourceArray,
            int sourceIndex,
            in Quaternion rotation,
            Vector2[] destinationArray,
            int destinationIndex,
            int length
        )
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
                var position = sourceArray[sourceIndex + x];
                //                 var destination = destinationArray[destinationIndex + x];
                // 
                //                 Vector2 v;
                //                 Transform(ref position, ref rotation, out v);
                // 
                //                 destination.X = v.X;
                //                 destination.Y = v.Y;
                // 
                //                 destinationArray[destinationIndex + x] = destination;
                destinationArray[destinationIndex + x] = System.Numerics.Vector2.Transform(position.Value, rotation.Value);

            }
        }

        /// <summary>
        /// Apply transformation on all vectors within array of <see cref="Vector2"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void Transform(
            Vector2[] sourceArray,
            in Matrix matrix,
            Vector2[] destinationArray)
        {
            Transform(sourceArray, 0, in matrix, destinationArray, 0, sourceArray.Length);
        }

        /// <summary>
        /// Apply transformation on all vectors within array of <see cref="Vector2"/> by the specified <see cref="Quaternion"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> which contains rotation transformation.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void Transform
        (
            Vector2[] sourceArray,
            in Quaternion rotation,
            Vector2[] destinationArray
        )
        {
            Transform(sourceArray, 0, in rotation, destinationArray, 0, sourceArray.Length);
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of the specified normal by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="normal">Source <see cref="Vector2"/> which represents a normal vector.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <returns>Transformed normal.</returns>
        public static Vector2 TransformNormal(in Vector2 normal, in Matrix matrix)
        {
            return System.Numerics.Vector2.TransformNormal(normal.Value, matrix.Value);
            //return new Vector2((normal.X * matrix.M11) + (normal.Y * matrix.M21), (normal.X * matrix.M12) + (normal.Y * matrix.M22));
        }

        /// <summary>
        /// Creates a new <see cref="Vector2"/> that contains a transformation of the specified normal by the specified <see cref="Matrix"/>.
        /// </summary>
        /// <param name="normal">Source <see cref="Vector2"/> which represents a normal vector.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="result">Transformed normal as an output parameter.</param>
        public static void TransformNormal(in Vector2 normal, in Matrix matrix, out Vector2 result)
        {
            result = System.Numerics.Vector2.TransformNormal(normal.Value, matrix.Value);
            //             var x = (normal.X * matrix.M11) + (normal.Y * matrix.M21);
            //             var y = (normal.X * matrix.M12) + (normal.Y * matrix.M22);
            //             result.Value.X = x;
            //             result.Value.Y = y;
        }

        /// <summary>
        /// Apply transformation on normals within array of <see cref="Vector2"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="sourceIndex">The starting index of transformation in the source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        /// <param name="destinationIndex">The starting index in the destination array, where the first <see cref="Vector2"/> should be written.</param>
        /// <param name="length">The number of normals to be transformed.</param>
        public static void TransformNormal
        (
            Vector2[] sourceArray,
            int sourceIndex,
            in Matrix matrix,
            Vector2[] destinationArray,
            int destinationIndex,
            int length
        )
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < sourceIndex + length)
                throw new ArgumentException("Source array length is lesser than sourceIndex + length");
            if (destinationArray.Length < destinationIndex + length)
                throw new ArgumentException("Destination array length is lesser than destinationIndex + length");

            for (int i = 0; i < length; i++)
            {
                var normal = sourceArray[sourceIndex + i];

                //                 destinationArray[destinationIndex + i] = new Vector2((normal.X * matrix.M11) + (normal.Y * matrix.M21),
                //                                                                      (normal.X * matrix.M12) + (normal.Y * matrix.M22));

                destinationArray[destinationIndex + i] = System.Numerics.Vector2.TransformNormal(normal.Value, matrix.Value);
            }
        }

        /// <summary>
        /// Apply transformation on all normals within array of <see cref="Vector2"/> by the specified <see cref="Matrix"/> and places the results in an another array.
        /// </summary>
        /// <param name="sourceArray">Source array.</param>
        /// <param name="matrix">The transformation <see cref="Matrix"/>.</param>
        /// <param name="destinationArray">Destination array.</param>
        public static void TransformNormal
            (
            Vector2[] sourceArray,
            in Matrix matrix,
            Vector2[] destinationArray
            )
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException("Destination array length is lesser than source array length");

            for (int i = 0; i < sourceArray.Length; i++)
            {
                var normal = sourceArray[i];

                //                 destinationArray[i] = new Vector2((normal.X * matrix.M11) + (normal.Y * matrix.M21),
                //                                                   (normal.X * matrix.M12) + (normal.Y * matrix.M22));
                destinationArray[i] = System.Numerics.Vector2.TransformNormal(normal.Value, matrix.Value);

            }
        }

        #endregion

        static Vector2()
        {
            Parser.RegistParser(new Vector2Parser());
        }
    }

    public class Vector2Parser : TypeParserAdapter<Vector2>
    {
        public override string ToString(Vector2 obj)
        {
            return $"{obj.X},{obj.Y}";
        }

        public override bool TryParse(string text, out Vector2 value)
        {
            value = default(Vector2);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var components = text.Split(',');

            float x = 0, y = 0;

            if (components.Length > 0 && float.TryParse(components[0].Trim(), out var _x))
            {
                x = _x;
            }
            if (components.Length > 1 && float.TryParse(components[1].Trim(), out var _y))
            {
                y = _y;
            }

            value = new Vector2(x, y);
            return true;

        }
    }


}