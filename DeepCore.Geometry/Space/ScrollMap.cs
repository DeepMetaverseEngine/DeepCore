using DeepCore.Geometry;
using DeepCore.XCSV;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static DeepCore.Space.GridMatrix;

namespace DeepCore.Space
{
    public interface IScrollMap<T>
    {
        /// <summary>
        /// 是否超越 XCount YCount
        /// </summary>
        bool IsInfinity { get; }
        bool IsSycMap { get; }
        Vector3 GridSize { get; }
        Size3D Length { get; }
        /// <summary>
        /// 如果 IsSycMap 为 False，则有可能取到负值或者超过XCount边界。
        /// </summary>
        T GetMetaData(int x, int y, int z);
    }

    public abstract class IScrollView<T> : Disposable
    {
        public Vector3 GridSize { get; }
        public Vector3 ViewSize { get; }
        public Vector3 ViewSizeHalf { get; }
        public Vector3 ViewPosition { get => vViewPos + ViewSizeHalf; }
        public BoundingBox ViewBounds { get => new BoundingBox(vViewPos, vViewPos + ViewSize); }
        public IScrollMap<T> Meta { get => meta; }
        public abstract Location3D CurrentViewLocatoin { get; }
        readonly protected IScrollMap<T> meta;
        readonly protected int metaXCount;
        readonly protected int metaYCount;
        readonly protected int metaZCount;
        readonly protected float gridSizeX;
        readonly protected float gridSizeY;
        readonly protected float gridSizeZ;

        readonly protected int buffXCount;
        readonly protected int buffYCount;
        readonly protected int buffZCount;
        readonly private BuffT[,,] buff;

        readonly protected int viewXCount;
        readonly protected int viewYCount;
        readonly protected int viewZCount;

        readonly protected bool isWorldCyc;
        readonly protected bool isWorldInfinity;
        readonly protected Vector3 totalWorldSize;

        protected Vector3 vViewPos;

        private bool lazyInit = false;
        private ScrollEnterView event_OnEnterView;
        private ScrollLeaveView event_OnLeaveView;
        public event ScrollEnterView OnEnterView { add { event_OnEnterView += value; } remove { event_OnEnterView -= value; } }
        public event ScrollLeaveView OnLeaveView { add { event_OnLeaveView += value; } remove { event_OnLeaveView -= value; } }
        public delegate void ScrollEnterView(IScrollView<T> sender, T data, Location3D location);
        public delegate void ScrollLeaveView(IScrollView<T> sender, T data, Location3D location);

        public IScrollView(IScrollMap<T> map, Vector3 viewSize, int buffSize)
        {
            this.meta = map;
            this.metaXCount = map.Length.X;
            this.metaYCount = map.Length.Y;
            this.metaZCount = map.Length.Z;
            this.gridSizeX = map.GridSize.X;
            this.gridSizeY = map.GridSize.Y;
            this.gridSizeZ = map.GridSize.Z;

            this.viewXCount = (int)Math.Ceiling(viewSize.X / gridSizeX) + 1;
            this.viewYCount = (int)Math.Ceiling(viewSize.Y / gridSizeY) + 1;
            this.viewZCount = (int)Math.Ceiling(viewSize.Z / gridSizeZ) + 1;

            this.buffXCount = Math.Min(viewXCount + buffSize, metaXCount);
            this.buffYCount = Math.Min(viewYCount + buffSize, metaYCount);
            this.buffZCount = Math.Min(viewZCount + buffSize, metaZCount);
            this.buff = new BuffT[buffXCount, buffYCount, buffZCount];
            this.buff.InitArray3D(this, (st, x, y, z) => new BuffT());

            this.isWorldCyc = map.IsSycMap;
            this.isWorldInfinity = map.IsInfinity;
            this.totalWorldSize.X = metaXCount * map.GridSize.X;
            this.totalWorldSize.Y = metaYCount * map.GridSize.Y;
            this.totalWorldSize.Z = metaZCount * map.GridSize.Z;

            this.GridSize = new Vector3(gridSizeX, gridSizeY, gridSizeZ);
            this.ViewSize = viewSize;
            this.ViewSizeHalf = viewSize / 2f;

            vViewPos = Vector3.Zero;
            vViewPos = Vector3.Zero;
        }
        protected override void Disposing()
        {
            if (event_OnLeaveView != null)
            {
                this.buff.ForEachArray3D(this, (st, v, x, y, z) =>
                {
                    event_OnLeaveView.Invoke(this, v.data, v.WorldLocation);
                });
            }
            event_OnEnterView = null;
            event_OnLeaveView = null;
        }
        public void SetViewPos(Vector3 pos)
        {
            pos = pos - ViewSizeHalf;
            MoveViewPos(pos - vViewPos);
        }
        public void MoveViewPos(Vector3 offset)
        {
            var oldPos = vViewPos;
            vViewPos += offset;
            if (!isWorldInfinity)
            {
                vViewPos.X = Math.Max(vViewPos.X, 0);
                vViewPos.Y = Math.Max(vViewPos.Y, 0);
                vViewPos.Z = Math.Max(vViewPos.Z, 0);
                vViewPos.X = Math.Min(vViewPos.X, totalWorldSize.X - ViewSize.X);
                vViewPos.Y = Math.Min(vViewPos.Y, totalWorldSize.Y - ViewSize.Y);
                vViewPos.Z = Math.Min(vViewPos.Z, totalWorldSize.Z - ViewSize.Z);
            }
            var sbx = CMath.CycDiv(oldPos.X, gridSizeX);
            var sby = CMath.CycDiv(oldPos.Y, gridSizeY);
            var sbz = CMath.CycDiv(oldPos.Z, gridSizeZ);
            var dbx = CMath.CycDiv(vViewPos.X, gridSizeX);
            var dby = CMath.CycDiv(vViewPos.Y, gridSizeY);
            var dbz = CMath.CycDiv(vViewPos.Z, gridSizeZ);
            move(lazyInit, dbx - sbx, dby - sby, dbz - sbz);
            lazyInit = true;
        }

        private class BuffT
        {
            private bool dummy = true;
            public int X = 0, Y = 0, Z = 0;
            public Location3D WorldLocation => new Location3D(X, Y, Z);
            public T data = default(T);
            public bool TryFill(T new_data, int x, int y, int z, out T old_data)
            {
                if (dummy || x != this.X || y != this.Y || z != this.Z)
                {
                    dummy = false;
                    old_data = this.data;
                    this.data = new_data;
                    this.X = x;
                    this.Y = y;
                    this.Z = z;
                    return true;
                }
                else
                {
                    old_data = default(T);
                    return false;
                }
            }
        }
        protected bool TryFillBuff(int sx, int sy, int sz, int bx, int by, int bz)
        {
            var new_data = meta.GetMetaData(sx, sy, sz);
            var old_loc = buff[bx, by, bz].WorldLocation;
            if (buff[bx, by, bz].TryFill(new_data, sx, sy, sz, out var old_data))
            {
                var new_loc = buff[bx, by, bz].WorldLocation;
                event_OnLeaveView?.Invoke(this, old_data, old_loc);
                event_OnEnterView?.Invoke(this, new_data, new_loc);
                return true;
            }
            return false;
        }
        protected T GetBuff(int bx, int by, int bz)
        {
            return buff[bx, by, bz].data;
        }
        private void lazy_init()
        {
            if (!lazyInit)
            {
                move(lazyInit, 0, 0, 0);
                lazyInit = true;
            }
        }
        protected int cyc_meta(int v, int add, int total)
        {
            if (isWorldCyc)
                return CMath.CycNum(v, add, total);
            else
                return v + add;
        }
        protected int cyc_buff(int v, int add, int total)
        {
            return CMath.CycNum(v, add, total);
        }
        protected abstract void move(bool lazyInit, int x, int y, int z);

        public void ForEachBuffer(Action<T, int, int, int> action)
        {
            lazy_init();
            buff.ForEachArray3D(this, (st, v, x, y, z) => action(v.data, x, y, z));
        }
        public bool ForEachWorldBuffer(BreakPredicate<T, int, int, int> action)
        {
            lazy_init();
            return buff.ForEachArray3D(this, (st, v, x, y, z) => action(v.data, v.X, v.Y, v.Z));
        }
        public bool ForEachWorldBuffer(BoundingBox box, BreakPredicate<T, int, int, int> action)
        {
            lazy_init();
            var sbx = CMath.CycDiv(box.Min.X, gridSizeX);
            var sby = CMath.CycDiv(box.Min.Y, gridSizeY);
            var sbz = CMath.CycDiv(box.Min.Z, gridSizeZ);
            var dbx = CMath.CycDiv(box.Max.X, gridSizeX);
            var dby = CMath.CycDiv(box.Max.Y, gridSizeY);
            var dbz = CMath.CycDiv(box.Max.Z, gridSizeZ);
            for (int x = sbx; x <= dbx; x += 1)
            {
                for (int y = sby; y <= dby; y += 1)
                {
                    for (int z = sbz; z <= dbz; z += 1)
                    {
                        if (TryGetMapBuff(x, y, z, out var data))
                        {
                            if (action(data, x, y, z))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public bool Visit(BreakPredicate<T, Vector3> action)
        {
            var offset = vViewPos;
            return ForEachWorldBuffer((v, x, y, z) =>
            {
                var pos = new Vector3(x * gridSizeX, y * gridSizeY, z * gridSizeZ) - offset;
                return action(v, pos);
            });
        }
        public abstract bool TryGetMapBuff(int x, int y, int z, out T data);
        public bool TryGetMapBuff(in Location3D loc, out T data)
        {
            return TryGetMapBuff(loc.X, loc.Y, loc.Z, out data);
        }
        public bool TryGetMapBuffByPos(in Vector3 pos, out T data)
        {
            var dbx = CMath.CycDiv(pos.X, gridSizeX);
            var dby = CMath.CycDiv(pos.Y, gridSizeY);
            var dbz = CMath.CycDiv(pos.Z, gridSizeZ);
            return TryGetMapBuff(dbx, dby, dbz, out data);
        }
        public bool IsInView(int x, int y, int z)
        {
            return TryGetMapBuff(x, y, z, out var data);
        }
        public bool IsInView(in Location3D loc)
        {
            return TryGetMapBuff(loc.X, loc.Y, loc.Z, out var data);
        }


        public bool TryRayCastMap<ST>(ST st, AbstractCollectionPool pool, RayCast ray, BreakPredicate<T, Location3D, Vector3, ST> action, out T result_touch)
        {
            return GridMatrix.TryRayCast3D<T, ST>(st, pool,
                (ST st, int x, int y, int z, out T data) => TryGetMapBuff(x, y, z, out data), 
                meta.GridSize, ray, action, out result_touch);
        }


    }



}
