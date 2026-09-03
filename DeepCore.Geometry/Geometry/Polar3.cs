using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public struct Polar3 : IEquatable<Polar3>
    {
        private System.Numerics.Vector3 Value;
        public float Distance { get => Value.Y; set => Value.Y = value; }
        public float Angle { get => Value.X; set => Value.X = value; }
        public float Z { get => Value.Z; set => Value.Z = value; }
        public Polar3(float distance, float angle, float z = 0)
        {
            this.Distance = distance;
            this.Angle = angle;
            this.Z = z;
        }
        public Polar3()
        {
            this.Value = System.Numerics.Vector3.Zero;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Polar3 other)
                return this.Value.Equals(other.Value);
            return false;
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="other">The <see cref="Vector3"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(in Polar3 other)
        {
            return Value.Equals(other.Value);
        }
        public bool Equals(Polar3 other)
        {
            return Value.Equals(other.Value);
        }
        /// <summary>
        /// Gets the hash code of this <see cref="Vector3"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Vector3"/>.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static readonly Polar3 Zero = new Polar3(0f, 0f, 0f);


        public Vector3 Offset(Vector3 pos)
        {
            VectorHelper.MovePolar(ref pos, this.Angle, this.Distance);
            return pos;
        }
    }
}
