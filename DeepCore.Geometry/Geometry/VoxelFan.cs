using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.Geometry;
namespace DeepCore.Geometry
{
    public struct VoxelFan : IEquatable<VoxelFan>
    {
        public Vector3 Center;
        public float Radius;
        public float Height;
        public float StartAngle;
        public float EndAngle;

        public VoxelFan(Vector3 center, float radius, float height, float startAngle, float stopAngle)
        {
            this.Center = center;
            this.Radius = radius;
            this.Height = height;
            this.StartAngle = startAngle;
            this.EndAngle = stopAngle;
        }

//         public bool Intersects(Vector3 box)
//         {
//             return this.Intersects(in box);
//         }

        public bool Intersects(in Vector3 box)
        {
            if (box.Z < this.Center.Z)
            {
                return false;
                
            }
            if (box.Z > this.Center.Z + this.Height)
            {
                return false;
            }

            float ddx = box.X - this.Center.X;
            float ddy = box.Y - this.Center.Y;
            float r = this.Radius;
            if (ddx * ddx + ddy * ddy <= r * r)
            {
                float direction = CMath.OpitimizeRadians((float)Math.Atan2(ddy, ddx));
                var startAngle = CMath.OpitimizeRadians(this.StartAngle);
                var endAngle = CMath.OpitimizeRadians(this.EndAngle);
                if (endAngle < startAngle)
                {
                    if (direction < endAngle)
                    {
                        direction += CMath.RADIANS_360;
                    }
                    endAngle += CMath.RADIANS_360;
                }
                if (direction >= startAngle && direction <= endAngle)
                {
                    return true;
                }
            }
            return false;
        }

//         public bool Intersects(VoxelCylinder box)
//         {
//             return box.Intersects(in this);
//         }

        public bool Intersects(in VoxelCylinder box)
        {
            return box.Intersects(in this);
        }

        public bool Equals(VoxelFan other)
        {
            return this.Center == other.Center && this.Radius == other.Radius && this.Height == other.Height &&
                this.StartAngle == other.StartAngle && this.EndAngle == other.EndAngle;
        }
        public bool Equals(in VoxelFan other)
        {
            return this.Center == other.Center && this.Radius == other.Radius && this.Height == other.Height &&
                this.StartAngle == other.StartAngle && this.EndAngle == other.EndAngle;
        }
    }
}
