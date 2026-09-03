// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace DeepCore.Geometry
{


    public struct Vector4 : IEquatable<Vector4>
    {
        #region Private Fields

        private static readonly Vector4 zeroVector = new Vector4();
        private static readonly Vector4 unitVector = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 unitXVector = new Vector4(1f, 0f, 0f, 0f);
        private static readonly Vector4 unitYVector = new Vector4(0f, 1f, 0f, 0f);
        private static readonly Vector4 unitZVector = new Vector4(0f, 0f, 1f, 0f);
        private static readonly Vector4 unitWVector = new Vector4(0f, 0f, 0f, 1f);

        #endregion Private Fields


        #region Public Fields

        public System.Numerics.Vector4 Value;
        public float X { get => Value.X; set { Value.X = value; } }
        public float Y { get => Value.Y; set { Value.Y = value; } }
        public float Z { get => Value.Z; set { Value.Z = value; } }
        public float W { get => Value.W; set { Value.W = value; } }

        public static implicit operator Vector4(in Geometry.Vector3 value)
        {
            return new Vector4(value.X, value.Y, value.Z, 0);
        }
        public static implicit operator Vector4(in Geometry.Vector2 value)
        {
            return new Vector4(value.X, value.Y, 0, 0);
        }
        public static implicit operator Vector4(in System.Numerics.Vector3 value)
        {
            return new Vector4(value.X, value.Y, value.Z, 0);
        }
        public static implicit operator Vector4(in System.Numerics.Vector2 value)
        {
            return new Vector4(value.X, value.Y, 0, 0);
        }
        public static implicit operator Vector4(in System.Numerics.Vector4 value)
        {
            return new Vector4(value.X, value.Y, value.Z, value.W);
        }

        #endregion Public Fields


        #region Properties

        /// <summary>
        /// Returns a <see>Vector4</see> with components 0, 0, 0, 0.
        /// </summary>
        public static Vector4 Zero
        {
            get { return zeroVector; }
        }

        /// <summary>
        /// Returns a <see>Vector4</see> with components 1, 1, 1, 1.
        /// </summary>
        public static Vector4 One
        {
            get { return unitVector; }
        }

        /// <summary>
        /// Returns a <see>Vector4</see> with components 1, 0, 0, 0.
        /// </summary>
        public static Vector4 UnitX
        {
            get { return unitXVector; }
        }

        /// <summary>
        /// Returns a <see>Vector4</see> with components 0, 1, 0, 0.
        /// </summary>
        public static Vector4 UnitY
        {
            get { return unitYVector; }
        }

        /// <summary>
        /// Returns a <see>Vector4</see> with components 0, 0, 1, 0.
        /// </summary>
        public static Vector4 UnitZ
        {
            get { return unitZVector; }
        }

        /// <summary>
        /// Returns a <see>Vector4</see> with components 0, 0, 0, 1.
        /// </summary>
        public static Vector4 UnitW
        {
            get { return unitWVector; }
        }

        #endregion Properties


        #region Constructors

        public Vector4(float x, float y, float z, float w)
        {
            this.Value.X = x;
            this.Value.Y = y;
            this.Value.Z = z;
            this.Value.W = w;
        }

        public Vector4(Vector2 value, float z, float w)
        {
            this.Value.X = value.X;
            this.Value.Y = value.Y;
            this.Value.Z = z;
            this.Value.W = w;
        }

        public Vector4(Vector3 value, float w)
        {
            this.Value.X = value.X;
            this.Value.Y = value.Y;
            this.Value.Z = value.Z;
            this.Value.W = w;
        }

        public Vector4(float value)
        {
            this.Value.X = value;
            this.Value.Y = value;
            this.Value.Z = value;
            this.Value.W = value;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <returns>The result of the vector addition.</returns>
        public static Vector4 Add(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Add(value1.Value, value2.Value);

            //            value1.X += value2.X;
            //            value1.Y += value2.Y;
            //            value1.Z += value2.Z;
            //            value1.W += value2.W;
            //            return value1;
        }

        /// <summary>
        /// Performs vector addition on <paramref name="value1"/> and
        /// <paramref name="value2"/>, storing the result of the
        /// addition in <paramref name="result"/>.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <param name="result">The result of the vector addition.</param>
        public static void Add(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Add(value1.Value, value2.Value);

            //            result.Value.X = value1.X + value2.X;
            //            result.Value.Y = value1.Y + value2.Y;
            //            result.Value.Z = value1.Z + value2.Z;
            //            result.Value.W = value1.W + value2.W;
        }

        public static Vector4 Barycentric(in Vector4 value1, in Vector4 value2, in Vector4 value3, float amount1, float amount2)
        {

            return new Vector4(
                MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
                MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2),
                MathHelper.Barycentric(value1.W, value2.W, value3.W, amount1, amount2));
        }

        public static void Barycentric(in Vector4 value1, in Vector4 value2, in Vector4 value3, float amount1, float amount2, out Vector4 result)
        {
            result.Value.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
            result.Value.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
            result.Value.Z = MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2);
            result.Value.W = MathHelper.Barycentric(value1.W, value2.W, value3.W, amount1, amount2);
        }

        public static Vector4 CatmullRom(in Vector4 value1, in Vector4 value2, in Vector4 value3, in Vector4 value4, float amount)
        {
            return new Vector4(
                MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
                MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount),
                MathHelper.CatmullRom(value1.W, value2.W, value3.W, value4.W, amount));
        }

        public static void CatmullRom(in Vector4 value1, in Vector4 value2, in Vector4 value3, in Vector4 value4, float amount, out Vector4 result)
        {
            result.Value.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
            result.Value.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
            result.Value.Z = MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount);
            result.Value.W = MathHelper.CatmullRom(value1.W, value2.W, value3.W, value4.W, amount);
        }

        public static Vector4 Clamp(in Vector4 value1, in Vector4 min, in Vector4 max)
        {
            return System.Numerics.Vector4.Clamp(value1.Value, min.Value, max.Value);

            //            return new Vector4(
            //                MathHelper.Clamp(value1.X, min.X, max.X),
            //                MathHelper.Clamp(value1.Y, min.Y, max.Y),
            //                MathHelper.Clamp(value1.Z, min.Z, max.Z),
            //                MathHelper.Clamp(value1.W, min.W, max.W));
        }

        public static void Clamp(in Vector4 value1, in Vector4 min, in Vector4 max, out Vector4 result)
        {
            result = System.Numerics.Vector4.Clamp(value1.Value, min.Value, max.Value);

            //            result.Value.X = MathHelper.Clamp(value1.X, min.X, max.X);
            //            result.Value.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
            //            result.Value.Z = MathHelper.Clamp(value1.Z, min.Z, max.Z);
            //            result.Value.W = MathHelper.Clamp(value1.W, min.W, max.W);
        }

        public static float Distance(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Distance(value1.Value, value2.Value);
            //return (float)Math.Sqrt(DistanceSquared(value1, value2));
        }

        public static void Distance(in Vector4 value1, in Vector4 value2, out float result)
        {
            result = System.Numerics.Vector4.Distance(value1.Value, value2.Value);

            //            result = (float)Math.Sqrt(DistanceSquared(value1, value2));
        }

        public static float DistanceSquared(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.DistanceSquared(value1.Value, value2.Value);
            //            float result;
            //            DistanceSquared( value1,  value2, out result);
            //            return result;
        }

        public static void DistanceSquared(in Vector4 value1, in Vector4 value2, out float result)
        {
            result = System.Numerics.Vector4.DistanceSquared(value1.Value, value2.Value);

            //            result = (value1.W - value2.W) * (value1.W - value2.W) +
            //                     (value1.X - value2.X) * (value1.X - value2.X) +
            //                     (value1.Y - value2.Y) * (value1.Y - value2.Y) +
            //                     (value1.Z - value2.Z) * (value1.Z - value2.Z);
        }

        public static Vector4 Divide(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Divide(value1.Value, value2.Value);

            //            value1.W /= value2.W;
            //            value1.X /= value2.X;
            //            value1.Y /= value2.Y;
            //            value1.Z /= value2.Z;
            //            return value1;
        }

        public static Vector4 Divide(in Vector4 value1, float divider)
        {
            return System.Numerics.Vector4.Divide(value1.Value, divider);

            //            float factor = 1f / divider;
            //            value1.W *= factor;
            //            value1.X *= factor;
            //            value1.Y *= factor;
            //            value1.Z *= factor;
            //            return value1;
        }

        public static void Divide(in Vector4 value1, float divider, out Vector4 result)
        {
            result = System.Numerics.Vector4.Divide(value1.Value, divider);

            //            float factor = 1f / divider;
            //            result.Value.W = value1.W * factor;
            //            result.Value.X = value1.X * factor;
            //            result.Value.Y = value1.Y * factor;
            //            result.Value.Z = value1.Z * factor;
        }

        public static void Divide(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Divide(value1.Value, value2.Value);

            //            result.Value.W = value1.W / value2.W;
            //            result.Value.X = value1.X / value2.X;
            //            result.Value.Y = value1.Y / value2.Y;
            //            result.Value.Z = value1.Z / value2.Z;
        }

        public static float Dot(in Vector4 vector1, in Vector4 vector2)
        {
            return System.Numerics.Vector4.Dot(vector1.Value, vector2.Value);

            //            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
        }

        public static void Dot(in Vector4 vector1, in Vector4 vector2, out float result)
        {
            result = System.Numerics.Vector4.Dot(vector1.Value, vector2.Value);

            //            result = vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
        }

        public bool Equals(Vector4 other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector4 other) Value.Equals(other.Value);
            return false;
        }

        public bool Equals(in Vector4 other)
        {
            return Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return (int)(this.W + this.X + this.Y + this.Y);
        }

        public static Vector4 Hermite(in Vector4 value1, in Vector4 tangent1, in Vector4 value2, in Vector4 tangent2, float amount)
        {
            Hermite(value1, tangent1, value2, tangent2, amount, out var result);
            return result;
        }

        public static void Hermite(in Vector4 value1, in Vector4 tangent1, in Vector4 value2, in Vector4 tangent2, float amount, out Vector4 result)
        {
            result.Value.W = MathHelper.Hermite(value1.W, tangent1.W, value2.W, tangent2.W, amount);
            result.Value.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
            result.Value.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
            result.Value.Z = MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount);
        }

        public float Length()
        {
            return this.Value.Length();
            //            float result;
            //            DistanceSquared( this,  zeroVector, out result);
            //            return (float)Math.Sqrt(result);
        }

        public float LengthSquared()
        {
            return Value.LengthSquared();
            //            float result;
            //            DistanceSquared( this,  zeroVector, out result);
            //            return result;
        }

        public static Vector4 Lerp(in Vector4 value1, in Vector4 value2, float amount)
        {
            return System.Numerics.Vector4.Lerp(value1.Value, value2.Value, amount);

            //            return new Vector4(
            //                MathHelper.Lerp(value1.X, value2.X, amount),
            //                MathHelper.Lerp(value1.Y, value2.Y, amount),
            //                MathHelper.Lerp(value1.Z, value2.Z, amount),
            //                MathHelper.Lerp(value1.W, value2.W, amount));
        }

        public static void Lerp(in Vector4 value1, in Vector4 value2, float amount, out Vector4 result)
        {
            result = System.Numerics.Vector4.Lerp(value1.Value, value2.Value, amount);

            //            result.Value.X = MathHelper.Lerp(value1.X, value2.X, amount);
            //            result.Value.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
            //            result.Value.Z = MathHelper.Lerp(value1.Z, value2.Z, amount);
            //            result.Value.W = MathHelper.Lerp(value1.W, value2.W, amount);
        }

        public static Vector4 Max(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Max(value1.Value, value2.Value);

            //            return new Vector4(
            //               MathHelper.Max(value1.X, value2.X),
            //               MathHelper.Max(value1.Y, value2.Y),
            //               MathHelper.Max(value1.Z, value2.Z),
            //               MathHelper.Max(value1.W, value2.W));
        }

        public static void Max(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Max(value1.Value, value2.Value);

            //            result.Value.X = MathHelper.Max(value1.X, value2.X);
            //            result.Value.Y = MathHelper.Max(value1.Y, value2.Y);
            //            result.Value.Z = MathHelper.Max(value1.Z, value2.Z);
            //            result.Value.W = MathHelper.Max(value1.W, value2.W);
        }

        public static Vector4 Min(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Min(value1.Value, value2.Value);

            //            return new Vector4(
            //               MathHelper.Min(value1.X, value2.X),
            //               MathHelper.Min(value1.Y, value2.Y),
            //               MathHelper.Min(value1.Z, value2.Z),
            //               MathHelper.Min(value1.W, value2.W));
        }

        public static void Min(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Min(value1.Value, value2.Value);
            //            result.Value.X = MathHelper.Min(value1.X, value2.X);
            //            result.Value.Y = MathHelper.Min(value1.Y, value2.Y);
            //            result.Value.Z = MathHelper.Min(value1.Z, value2.Z);
            //            result.Value.W = MathHelper.Min(value1.W, value2.W);
        }

        public static Vector4 Multiply(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Multiply(value1.Value, value2.Value);
            //            value1.W *= value2.W;
            //            value1.X *= value2.X;
            //            value1.Y *= value2.Y;
            //            value1.Z *= value2.Z;
            //            return value1;
        }

        public static Vector4 Multiply(in Vector4 value1, float scaleFactor)
        {
            return System.Numerics.Vector4.Multiply(value1.Value, scaleFactor);
            //            value1.W *= scaleFactor;
            //            value1.X *= scaleFactor;
            //            value1.Y *= scaleFactor;
            //            value1.Z *= scaleFactor;
            //            return value1;
        }

        public static void Multiply(in Vector4 value1, float scaleFactor, out Vector4 result)
        {
            result = System.Numerics.Vector4.Multiply(value1.Value, scaleFactor);

            //            result.Value.W = value1.W * scaleFactor;
            //            result.Value.X = value1.X * scaleFactor;
            //            result.Value.Y = value1.Y * scaleFactor;
            //            result.Value.Z = value1.Z * scaleFactor;
        }

        public static void Multiply(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Multiply(value1.Value, value2.Value);

            //            result.Value.W = value1.W * value2.W;
            //            result.Value.X = value1.X * value2.X;
            //            result.Value.Y = value1.Y * value2.Y;
            //            result.Value.Z = value1.Z * value2.Z;
        }

        public static Vector4 Negate(in Vector4 value)
        {
            return System.Numerics.Vector4.Negate(value.Value);
            //            value = new Vector4(-value.X, -value.Y, -value.Z, -value.W);
            //            return value;
        }

        public static void Negate(in Vector4 value, out Vector4 result)
        {
            result = System.Numerics.Vector4.Negate(value.Value);

            //            result.Value.X = -value.X;
            //            result.Value.Y = -value.Y;
            //            result.Value.Z = -value.Z;
            //            result.Value.W = -value.W;
        }

        public void Normalize()
        {
            Value = System.Numerics.Vector4.Normalize(Value);
        }

        public static Vector4 Normalize(in Vector4 vector)
        {
            return System.Numerics.Vector4.Normalize(vector.Value);

            //            Normalize(vector, out vector);
            //            return vector;
        }

        public static void Normalize(in Vector4 vector, out Vector4 result)
        {
            result = System.Numerics.Vector4.Normalize(vector.Value);

            //            float factor;
            //            DistanceSquared( vector, zeroVector, out factor);
            //            factor = 1f / (float)Math.Sqrt(factor);
            //
            //            result.Value.W = vector.W * factor;
            //            result.Value.X = vector.X * factor;
            //            result.Value.Y = vector.Y * factor;
            //            result.Value.Z = vector.Z * factor;
        }

        public static Vector4 SmoothStep(in Vector4 value1, in Vector4 value2, float amount)
        {
            return new Vector4(
                MathHelper.SmoothStep(value1.X, value2.X, amount),
                MathHelper.SmoothStep(value1.Y, value2.Y, amount),
                MathHelper.SmoothStep(value1.Z, value2.Z, amount),
                MathHelper.SmoothStep(value1.W, value2.W, amount));
        }

        public static void SmoothStep(in Vector4 value1, in Vector4 value2, float amount, out Vector4 result)
        {
            result.Value.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
            result.Value.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
            result.Value.Z = MathHelper.SmoothStep(value1.Z, value2.Z, amount);
            result.Value.W = MathHelper.SmoothStep(value1.W, value2.W, amount);
        }

        /// <summary>
        /// Performs vector subtraction on <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">The vector to be subtracted from.</param>
        /// <param name="value2">The vector to be subtracted from <paramref name="value1"/>.</param>
        /// <returns>The result of the vector subtraction.</returns>
        public static Vector4 Subtract(in Vector4 value1, in Vector4 value2)
        {
            return System.Numerics.Vector4.Subtract(value1.Value, value2.Value);

            //            value1.W -= value2.W;
            //            value1.X -= value2.X;
            //            value1.Y -= value2.Y;
            //            value1.Z -= value2.Z;
            //            return value1;
        }

        /// <summary>
        /// Performs vector subtraction on <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">The vector to be subtracted from.</param>
        /// <param name="value2">The vector to be subtracted from <paramref name="value1"/>.</param>
        /// <param name="result">The result of the vector subtraction.</param>
        public static void Subtract(in Vector4 value1, in Vector4 value2, out Vector4 result)
        {
            result = System.Numerics.Vector4.Subtract(value1.Value, value2.Value);

            //            result.Value.W = value1.W - value2.W;
            //            result.Value.X = value1.X - value2.X;
            //            result.Value.Y = value1.Y - value2.Y;
            //            result.Value.Z = value1.Z - value2.Z;
        }

        public static Vector4 Transform(in Vector2 position, in Matrix matrix)
        {
            return System.Numerics.Vector4.Transform(position.Value, matrix.Value);

            //            Vector4 result;
            //            Transform( position, matrix, out result);
            //            return result;
        }

        public static Vector4 Transform(in Vector3 position, in Matrix matrix)
        {
            return System.Numerics.Vector4.Transform(position.Value, matrix.Value);

            //            Vector4 result;
            //            Transform( position,  matrix, out result);
            //            return result;
        }

        public static Vector4 Transform(ref Vector4 vector, in Matrix matrix)
        {
            return System.Numerics.Vector4.Transform(vector.Value, matrix.Value);
            //            Transform( vector,  matrix, out vector);
            //            return vector;
        }

        public static void Transform(ref Vector2 position, in Matrix matrix, out Vector4 result)
        {
            result = System.Numerics.Vector4.Transform(position.Value, matrix.Value);

            //            result.Value.X = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
            //            result.Value.Y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
            //            result.Value.Z = (position.X * matrix.M13) + (position.Y * matrix.M23) + matrix.M43;
            //            result.Value.W = (position.X * matrix.M14) + (position.Y * matrix.M24) + matrix.M44;
        }

        public static void Transform(in Vector3 position, in Matrix matrix, out Vector4 result)
        {
            result = System.Numerics.Vector4.Transform(position.Value, matrix.Value);

            //            result.Value.X = (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41;
            //            result.Value.Y = (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42;
            //            result.Value.Z = (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43;
            //            result.Value.W = (position.X * matrix.M14) + (position.Y * matrix.M24) + (position.Z * matrix.M34) + matrix.M44;
        }

        public static void Transform(in Vector4 vector, in Matrix matrix, out Vector4 result)
        {
            result = System.Numerics.Vector4.Transform(vector.Value, matrix.Value);
            //            var x = (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + (vector.W * matrix.M41);
            //            var y = (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + (vector.W * matrix.M42);
            //            var z = (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + (vector.W * matrix.M43);
            //            var w = (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + (vector.W * matrix.M44);
            //            result.Value.X = x;
            //            result.Value.Y = y;
            //            result.Value.Z = z;
            //            result.Value.W = w;
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.X.ToString(), "  ",
                    this.Y.ToString(), "  ",
                    this.Z.ToString(), "  ",
                    this.W.ToString()
                );
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append(" W:");
            sb.Append(this.W);
            sb.Append("}");
            return sb.ToString();
        }

        #endregion Public Methods


        #region Operators

        public static Vector4 operator -(in Vector4 value)
        {
            return -value.Value;
            //            return new Vector4(-value.X, -value.Y, -value.Z, -value.W);
        }

        public static bool operator ==(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value == value2.Value;

            //            return value1.W == value2.W
            //                && value1.X == value2.X
            //                && value1.Y == value2.Y
            //                && value1.Z == value2.Z;
        }

        public static bool operator !=(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value != value2.Value;
            //            return !(value1 == value2);
        }

        public static Vector4 operator +(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value + value2.Value;
            //            value1.W += value2.W;
            //            value1.X += value2.X;
            //            value1.Y += value2.Y;
            //            value1.Z += value2.Z;
            //            return value1;
        }

        public static Vector4 operator -(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value - value2.Value;
            //            value1.W -= value2.W;
            //            value1.X -= value2.X;
            //            value1.Y -= value2.Y;
            //            value1.Z -= value2.Z;
            //            return value1;
        }

        public static Vector4 operator *(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value * value2.Value;
            //            value1.W *= value2.W;
            //            value1.X *= value2.X;
            //            value1.Y *= value2.Y;
            //            value1.Z *= value2.Z;
            //            return value1;
        }

        public static Vector4 operator *(in Vector4 value1, in float scaleFactor)
        {
            return value1.Value * scaleFactor;
            //            value1.W *= scaleFactor;
            //            value1.X *= scaleFactor;
            //            value1.Y *= scaleFactor;
            //            value1.Z *= scaleFactor;
            //            return value1;
        }

        public static Vector4 operator *(float scaleFactor, in Vector4 value1)
        {
            return value1.Value * scaleFactor;
            //            value1.W *= scaleFactor;
            //            value1.X *= scaleFactor;
            //            value1.Y *= scaleFactor;
            //            value1.Z *= scaleFactor;
            //            return value1;
        }

        public static Vector4 operator /(in Vector4 value1, in Vector4 value2)
        {
            return value1.Value / value2.Value;
            //            value1.W /= value2.W;
            //            value1.X /= value2.X;
            //            value1.Y /= value2.Y;
            //            value1.Z /= value2.Z;
            //            return value1;
        }

        public static Vector4 operator /(in Vector4 value1, float divider)
        {
            //            float factor = 1f / divider;
            //            value1.W *= factor;
            //            value1.X *= factor;
            //            value1.Y *= factor;
            //            value1.Z *= factor;
            return value1.Value / divider;
        }

        #endregion Operators

        static Vector4()
        {
            Parser.RegistParser(new Vector4Parser());
        }
    }

    public class Vector4Parser : TypeParserAdapter<Vector4>
    {
        public override string ToString(Vector4 obj)
        {
            return $"{obj.X},{obj.Y},{obj.Z},{obj.W}";
        }

        public override bool TryParse(string text, out Vector4 value)
        {
            value = default(Vector4);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var components = text.Split(',');

            float x = 0, y = 0, z = 0, w = 0;

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
            if (components.Length > 3 && float.TryParse(components[3].Trim(), out var _w))
            {
                w = _w;
            }

            value = new Vector4(x, y, z, w);
            return true;

        }
    }

}
