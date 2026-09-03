
using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;

namespace DeepCore.Game3D.Host.Helper
{
    public static class Collider
    {
        /// <summary>
        /// 单位和【圆形】碰撞
        /// </summary>
        public delegate bool ObjectTouchSphere<ST>(ST state, InstanceZoneObject o, in BoundingSphere sphere);

        /// <summary>
        /// 单位和【矩形】碰撞
        /// </summary>
        public delegate bool ObjectTouchBox<ST>(ST state, InstanceZoneObject o, in BoundingBox box);


        /// <summary>
        /// 单位和【圆柱】碰撞
        /// </summary>
        public delegate bool ObjectTouchCylinder<ST>(ST state, InstanceZoneObject o, in VoxelCylinder sphere);

        /// <summary>
        /// 单位和【扇形柱】碰撞
        /// </summary>
        public delegate bool ObjectTouchFan<ST>(ST state, InstanceZoneObject o, in VoxelFan fan);

        /// <summary>
        /// 单位和【直线柱】碰撞(粗线段)
        /// </summary>
        public delegate bool ObjectTouchRectStripe<ST>(ST state, InstanceZoneObject o, in VoxelRectStripe stripe);

        /// <summary>
        /// 单位【运动轨迹】从A点移动到B点经过的碰撞(圆角粗线段)
        /// </summary>
        public delegate bool ObjectTouchStripe<ST>(ST state, InstanceZoneObject o, in VoxelStripe stripe);



        //---------------------------------------------------------------------------------------------
        public static bool Sphere_Touch_Position<ST>(ST state, InstanceZoneObject o, in BoundingSphere shape)
        {
            return shape.Contains(o.WaistPosition) != ContainmentType.Disjoint;
        }
        public static bool Sphere_Touch_BlockBody<ST>(ST state, InstanceZoneObject o, in BoundingSphere shape)
        {
            return shape.Intersects(new BoundingSphere(o.WaistPosition, o.BodyBlockSize));
        }
        public static bool Sphere_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in BoundingSphere shape)
        {
            return shape.Intersects(new BoundingSphere(o.WaistPosition, o.BodyHitSize));
        }
        //---------------------------------------------------------------------------------------------
        public static bool Cylinder_Touch_Position<ST>(ST state, InstanceZoneObject o, in VoxelCylinder shape)
        {
            return shape.Intersects(o.WaistPosition);
        }
        public static bool Cylinder_Touch_BlockBody<ST>(ST state, InstanceZoneObject o, in VoxelCylinder shape)
        {
            return o.VoxelBody.Intersects(in shape);
        }
        public static bool Cylinder_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in VoxelCylinder shape)
        {
            return o.VoxelBodyHit.Intersects(in shape);
        }
        //---------------------------------------------------------------------------------------------
        public static bool Fan_Touch_Position<ST>(ST state, InstanceZoneObject o, in VoxelFan shape)
        {
            return shape.Intersects(o.Position);
        }
        public static bool Fan_Touch_BlockBody<ST>(ST state, InstanceZoneObject o, in VoxelFan shape)
        {
            return o.VoxelBody.Intersects(in shape);
        }
        public static bool Fan_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in VoxelFan shape)
        {
            return o.VoxelBodyHit.Intersects(in shape);
        }
        //---------------------------------------------------------------------------------------------
        public static bool Box_Touch_Position<ST>(ST state, InstanceZoneObject o, in BoundingBox shape)
        {
            return shape.Contains(o.WaistPosition) != ContainmentType.Disjoint;
        }
        public static bool Box_Touch_BlockBody<ST>(ST state, InstanceZoneObject o, in BoundingBox shape)
        {
            return shape.Intersects(new BoundingSphere(o.WaistPosition, o.BodyBlockSize));
        }
        public static bool Box_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in BoundingBox shape)
        {
            return shape.Intersects(new BoundingSphere(o.WaistPosition, o.BodyHitSize));
        }
        //---------------------------------------------------------------------------------------------
        public static bool Stripe_Touch_BlockBody<ST>(ST state, InstanceZoneObject o, in VoxelStripe shape)
        {
            return o.VoxelBody.Intersects(in shape);
        }
        public static bool Stripe_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in VoxelStripe shape)
        {
            return shape.Intersects(o.VoxelBodyHit);
        }
        //---------------------------------------------------------------------------------------------
        public static bool RectStripe_Touch_BlockBodye<ST>(ST state, InstanceZoneObject o, in VoxelRectStripe shape)
        {
            return shape.Intersects(o.VoxelBody);
        }
        public static bool RectStripe_Touch_HitBody<ST>(ST state, InstanceZoneObject o, in VoxelRectStripe shape)
        {
            return shape.Intersects(o.VoxelBodyHit);
        }
        //---------------------------------------------------------------------------------------------
        public static bool PositionObjectTouch(this IEntityObject src, IEntityObject dst)
        {
            return src.VoxelBody.Intersects(dst.VoxelBody);
        }

        public static bool Intersects(this Geometry.Vector3 p1, in Geometry.Vector3 p2, float distance)
        {
            Vector3.DistanceSquared(in p1, in p2, out var pd);
            return pd <= distance * distance;
        }
        public static bool Intersects(this IEntityObject src, IEntityObject dst, float distance)
        {
            var pd = Vector3.DistanceSquared(src.Position, dst.Position);
            return pd <= distance * distance;
        }

        public static bool Intersects(in Geometry.Vector3 p1, float r1, in Geometry.Vector3 p2, float r2)
        {
            var distance = r1 + r2;
            Vector3.DistanceSquared(in p1, in p2, out var pd);
            return pd <= distance * distance;
        }

        public static float Distance(this IPositionObject src, IPositionObject dst)
        {
            return Geometry.Vector3.Distance(src.Position, dst.Position);
        }
        public static float DistanceSquared(this IPositionObject src, IPositionObject dst)
        {
            return Geometry.Vector3.DistanceSquared(src.Position, dst.Position);
        }
    }

}
