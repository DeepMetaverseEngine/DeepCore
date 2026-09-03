using DeepCore.Geometry;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Slave.Helper
{
    /// <summary>
    /// 从小到大排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectSorterNearest<T> : IComparer<T> where T : ILayerZoneEntity
    {
        private Vector3 pos;
        public ObjectSorterNearest(Geometry.Vector3 pos)
        {
            this.pos = pos;
        }
        public int Compare(T x, T y)
        {
            float d0 = Vector3.DistanceSquared(pos, x.Position);//MathVector.getDistanceSquare(x.X, x.Y, this.X, this.Y);
            float d1 = Vector3.DistanceSquared(pos, y.Position);//MathVector.getDistanceSquare(y.X, y.Y, this.X, this.Y);
            if (d0 < d1)
                return -1;
            if (d0 > d1)
                return 1;
            return 0;
        }
    }

    /// <summary>
    /// 从大到小排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectSorterFarthest<T> : IComparer<T> where T : ILayerZoneEntity
    {
        private Geometry.Vector3 pos;
        public ObjectSorterFarthest(Geometry.Vector3 pos)
        {
            this.pos = pos;
        }
        public int Compare(T x, T y)
        {
            float d0 = Vector3.DistanceSquared(pos, x.Position);//MathVector.getDistanceSquare(x.X, x.Y, this.X, this.Y);
            float d1 = Vector3.DistanceSquared(pos, y.Position);//MathVector.getDistanceSquare(y.X, y.Y, this.X, this.Y);
            if (d0 < d1)
                return 1;
            if (d0 > d1)
                return -1;
            return 0;
        }
    }

    /// <summary>
    /// 从小到大排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectBodySorterNearest<T> : IComparer<T> where T : ILayerZoneEntity
    {
        private Geometry.Vector3 pos;
        private float R;
        public ObjectBodySorterNearest(Geometry.Vector3 pos, float r)
        {
            this.pos = pos;
            this.R = r;
        }
        public int Compare(T x, T y)
        {
            var r2 = R * R;
            var d0 = Math.Abs(Vector3.DistanceSquared(pos, x.Position) - (x.RadiusSize * x.RadiusSize + r2));
            var d1 = Math.Abs(Vector3.DistanceSquared(pos, y.Position) - (y.RadiusSize * y.RadiusSize + r2));
            if (d0 < d1)
                return -1;
            if (d0 > d1)
                return 1;
            return 0;
        }
    }
    /// <summary>
    /// 从大到小排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectBodySorterFarthest<T> : IComparer<T> where T : ILayerZoneEntity
    {
        private Geometry.Vector3 pos;
        private float R;
        public ObjectBodySorterFarthest(Geometry.Vector3 pos, float r)
        {
            this.pos = pos;
            this.R = r;
        }
        public int Compare(T x, T y)
        {
            var r2 = R * R;
            var d0 = Math.Abs(Vector3.DistanceSquared(pos, x.Position) - (x.RadiusSize * x.RadiusSize + r2));
            var d1 = Math.Abs(Vector3.DistanceSquared(pos, y.Position) - (y.RadiusSize * y.RadiusSize + r2));
            if (d0 < d1)
                return 1;
            if (d0 > d1)
                return -1;
            return 0;
        }
    }
}
