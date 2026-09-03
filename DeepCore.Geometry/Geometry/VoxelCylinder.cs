using DeepCore.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Geometry
{
    public struct VoxelCylinder : IEquatable<VoxelCylinder>
    {
        public Vector3 Center;
        public float Radius;
        public float Height;


        public VoxelCylinder(Vector3 center, float radius, float height)
        {
            this.Center = center;
            this.Radius = radius;
            this.Height = height;
        }

        // 
        //         public bool Intersects(in Vector3 box)
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
            float w = this.Center.X - box.X;
            float h = this.Center.Y - box.Y;
            float r = this.Radius;
            return (w * w + h * h) <= (r * r);
        }

        //         public bool Intersects(VoxelCylinder box)
        //         {
        //             return this.Intersects(in box);
        //         }

        public bool Intersects(in VoxelCylinder box)
        {
            float sz2 = this.Center.Z + this.Height;
            float dz2 = box.Center.Z + box.Height;
            if (sz2 < box.Center.Z) { return false; }
            if (this.Center.Z > dz2) { return false; }

            float w = this.Center.X - box.Center.X;
            float h = this.Center.Y - box.Center.Y;
            float r = this.Radius + box.Radius;
            return (w * w + h * h) <= (r * r);
        }

        //         public bool Intersects(VoxelFan box)
        //         {
        //             return this.Intersects(in box);
        //         }

        public bool Intersects(in VoxelFan fan)
        {
            float sz2 = this.Center.Z + this.Height;
            float dz2 = fan.Center.Z + fan.Height;
            if (sz2 < fan.Center.Z) { return false; }
            if (this.Center.Z > dz2) { return false; }

            float ddx = fan.Center.X - this.Center.X;
            float ddy = fan.Center.Y - this.Center.Y;
            float r = this.Radius + fan.Radius;
            if ((ddx * ddx + ddy * ddy) <= (r * r))
            {
                if (fan.StartAngle != fan.EndAngle)
                {
                    float direction = CMath.OpitimizeRadians((float)Math.Atan2(-ddy, -ddx));
                    var startAngle = CMath.OpitimizeRadians(fan.StartAngle);
                    var endAngle = CMath.OpitimizeRadians(fan.EndAngle);
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
                {
                    var fanLeft = fan.Center;
                    VectorHelper.MovePolar(ref fanLeft, fan.StartAngle, fan.Radius);
                    if (DeepCore.Geometry.CollisionMath.CircleLineCollide(this.Center, Radius, fan.Center, fanLeft))
                    {
                        return true;
                    }
                    var fanRight = fan.Center;
                    VectorHelper.MovePolar(ref fanRight, fan.EndAngle, fan.Radius);
                    if (DeepCore.Geometry.CollisionMath.CircleLineCollide(this.Center, Radius, fan.Center, fanRight))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //         public bool Intersects(VoxelStripe s)
        //         {
        //             return s.Intersects(this);
        //         }
        public bool Intersects(in VoxelStripe s)
        {
            return s.Intersects(this);
        }
        //         public bool Intersects(VoxelRectStripe s)
        //         {
        //             return s.Intersects(this);
        //         }
        public bool Intersects(in VoxelRectStripe s)
        {
            return s.Intersects(this);
        }

        public bool Equals(VoxelCylinder other)
        {
            return this.Center == other.Center && this.Radius == other.Radius && this.Height == other.Height;
        }
        public bool Equals(in VoxelCylinder other)
        {
            return this.Center == other.Center && this.Radius == other.Radius && this.Height == other.Height;
        }


    }
}
