using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        //-------------------------------------------------------------------------------------------------------//

        #region MANHATTAN_MAP

        private ITerrainWorld voxelWorld;
        public ITerrainWorld TerrainWorld
        {
            get => voxelWorld;
        }
        public ZoneInfo ZoneInfoMatrix { get; private set; }
        public ITerrainAstar PathFinder { get { return voxelWorld.PathFinder; } }

        public ITerrain Terrain3D
        {
            get { return voxelWorld.Terrain; }
        }

        public TerrainDefinitionMap TerrainDefinition { get; private set; }

        protected virtual void InitTerrain(SceneData sdata)
        {
            this.TerrainDefinition = sdata.OverrideTerrainDefinition ?? Templates.DefaultTerrainDefinition;
            this.ZoneInfoMatrix = sdata.Terrain.ZoneData;
            this.voxelWorld = ZoneDataFactory.Factory.CreateVoxelWorld(this, DataRoot, sdata.VoxelFileName, sdata, sdata.ZoneData);
            this.voxelWorld.PathFinder.FindPathStepLimit = CFG.AI_FIND_PATH_STEP_LIMIT;
        }
        protected virtual void DisposeTerrain()
        {
        }
        public virtual ITerrainWayPoint FindPathByPos(Vector3 src, Vector3 dst)
        {
            return voxelWorld.PathFinder.FindPathByPos(src, dst);
        }
        public virtual ITerrainWayPoint FindPathByLayer(InstanceUnit src, ITerrainLayer dst, Astar.FindPathParams pathFinderArgs = null)
        {
            if (dst == null) return null;
            return voxelWorld.PathFinder.FindPathByLayerPos(src.CurrentLayer, src.Position, dst, dst.UpwardCenterPos);
        }
        public virtual ITerrainWayPoint FindPathSrcLayer(InstanceUnit src, Vector3 dst, Astar.FindPathParams pathFinderArgs = null)
        {
            return voxelWorld.PathFinder.FindPathByLayerPos(src.CurrentLayer, src.Position, dst);
        }

        //         public ZoneArea GetAreaByPos(in Vector3 pos)
        //         {
        //             SpaceDiv.ClampPosition(pos.X, pos.Y, out var cx, out var cy);
        //             var cell = SpaceDiv.GetSpaceCell(cx, cy);
        //             if (cell is ZoneSpaceDivision.ZoneSpaceCellNode sc)
        //             {
        //                 return sc.Area;
        //             }
        //             return null;
        //         }
        //         public ZoneArea GetArea(ITerrainBlock layer)
        //         {
        //             return GetAreaByPos(layer.UpwardCenterPos);
        //         }

        #endregion

        #region COLLIDE


        /// <summary>
        /// 2个单位是否碰撞
        /// </summary>
        /// <param name="s"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public virtual bool TouchObject2(IEntityObject s, IEntityObject d)
        {
            if (s == d) return false;
            var dsp = d.ZoneShape;
            if (dsp != null)
            {
                if (CMath.IsIntersectW(s.Z, s.BodyHeight, d.Z, d.BodyHeight))
                {
                    return dsp.Include(s.X, s.Y);
                }
                return false;
            }
            else
            {
                return s.VoxelBody.Intersects(d.VoxelBody);
            }
        }
        public virtual bool TouchObject2(IPositionObject s, IPositionObject d)
        {
            if (s == d) return false;
            return Collider.Intersects(s.Position, s.BodySize, d.Position, d.BodySize);
        }

        /// <summary>
        /// 单位和地图碰撞检测
        /// </summary>
        public virtual bool TouchMap(InstanceZoneObject o, out ITerrainLayer layer)
        {
            return Terrain3D.TryIntersectMapByPos(o.Position, out layer);
        }
        /// <summary>
        /// 单位尝试用新的坐标和地图碰撞
        /// </summary>
        public virtual bool TryTouchMap(InstanceZoneObject o, in Vector3 pos, out ITerrainLayer layer)
        {
            return Terrain3D.TryIntersectMapByPos(pos, out layer);
        }
        /// <summary>
        /// 坐标和体素碰撞
        /// </summary>
        public virtual bool IntersectMapByPos(in Vector3 pos, out ITerrainLayer layer)
        {
            return Terrain3D.TryIntersectMapByPos(pos, out layer);
        }
        /// <summary>
        /// 单位和地图碰撞检测
        /// </summary>
        public bool TouchMap(InstanceZoneObject o)
        {
            return this.TouchMap(o, out var layer);
        }

        /// <summary>
        /// 单位尝试用新的坐标和地图碰撞
        /// </summary>
        public bool TryTouchMap(InstanceZoneObject o, Vector3 pos)
        {
            return this.TryTouchMap(o, pos, out var layer);
        }
        /// <summary>
        /// 坐标和体素碰撞
        /// </summary>
        public bool IntersectMapByPos(Vector3 pos)
        {
            return this.IntersectMapByPos(pos, out var layer);
        }
        public virtual bool TryUpdatePos(InstanceZoneObject o, ref Vector3 pos, out ITerrainLayer layer)
        {
            return Terrain3D.TryUpdatePos(ref pos, out layer);
        }


        public virtual bool TestMapCross(InstanceZoneObject o, in Vector3 pos)
        {
            if (this.PathFinder.GetMapBlockByPos(pos, out var mapnode))
            {
                return !this.PathFinder.IsMapNodeBlock(mapnode);
            }
            return false;
        }

        /// <summary>
        /// 坐标和体素碰撞
        /// </summary>
        public virtual bool TryTouchSpell(InstanceSpell spell, out ITerrainLayer layer, out float newDirection)
        {
            var pos = spell.Position;
            if ( Terrain3D.TryIntersectMapByPos(pos, out layer))
            {

            }
            newDirection = spell.Direction;
            return false;
        }

        //-------------------------------------------------------------------------------------------------------
        public ITerrainLayer FindNearRandomMoveableNode(ITerrainLayer src, float radius)
        {
            var ly = TerrainWorld.FindNearRandomMoveableNode(this.RandomN, src, radius);
            if (ly != null) { return ly; }
            return src;
        }
        public ITerrainLayer FindNearRandomMoveableNode(ref Geometry.Vector3 pos, float radius)
        {
            return TerrainWorld.FindNearRandomMoveableNode(this.RandomN,ref pos, radius);
        }
        public Vector3? FindNearRandomMoveablePos(ITerrainLayer src, float radius)
        {
            if (src == null) return null;
            var ly = FindNearRandomMoveableNode(src, radius);
            if (ly != null) { return ly.UpwardCenterPos; }
            return src.UpwardCenterPos;
        }
        public Vector3? FindNearRandomMoveablePos(Geometry.Vector3 pos, float radius)
        {
            var ly = FindNearRandomMoveableNode(ref pos, radius);
            if (ly != null) { return pos; }
            return null;
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------
        #region GET_OBJECTS

        //-------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 当前单位是否和单位碰撞
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public InstanceUnit IntersectNearUnit(IEntityObject a, bool ignoreStatic = false)
        {
            using (var for1 = ObjectPool.AllocForEach2<InstanceZoneEntity, IEntityObject, bool>(a, ignoreStatic))
            {
                if (this.ForEachNearObjects(a.X, a.Y, for1, static (st) =>
                {
                    if (st.Iterator is InstanceUnit unit)
                    {
                        if (st.Arg2 && unit.IsStaticBlock)
                        {

                        }
                        else if ((unit.IntersectObj) && unit.Parent.TouchObject2(st.Arg1, st.Iterator))
                        {
                            st.Break = true;
                        }
                    }
                }))
                {
                    return for1.Iterator as InstanceUnit;
                }
            }
            return null;
        }

        /// <summary>
        /// 当前单位是否和建筑碰撞
        /// </summary>
        public IEntityObject IntersectNearStaticBlockable(InstanceZoneEntity a)
        {
            using (var for1 = ObjectPool.AllocForEach1<IEntityObject, InstanceZoneEntity>(a))
            {
                if (ForEachNearStaticBlock(a.X, a.Y, a.BodyBlockSize, for1, static (st) =>
                {
                    var u = st.Iterator;
                    var a = st.Arg1;
                    if (u is ZoneDecoration ed)
                    {
                        if (!ed.Enable) { return; }
                    }
                    if (u is InstanceUnit ud)
                    {
                        if (!ud.IsStaticBlock || ud.AoiStatus != a.AoiStatus) return;
                    }
                    if (u.Parent.TouchObject2(a, u))
                    {
                        st.Break = true;
                        return;
                    }
                }))
                {
                    return for1.Iterator;
                }
            }
            return null;
        }

        public InstanceUnit GetNearBlockObject(InstanceUnit src)
        {
            if (TryGetNearObjects<InstanceUnit, InstanceUnit>(src.X, src.Y, src, static (st, o) =>
            {
                if (st.Parent.TouchObject2(st, o))
                {
                    return true;
                }
                return false;
            }, out var result))
            {
                return result;
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearStaticBlock<ST>(float x, float y, float r, ST state, ForEachAction<ST> indexer) where ST : ForEachInput<IEntityObject>
        {
            using (var for1 = ObjectPool.AllocAutoRelease<ForEachInput<ZoneSpaceDivision.SpaceCellNode, ST, ForEachAction<ST>>>())
            {
                for1.Arg1 = state;
                for1.Arg2 = indexer;
                return mSpaceDiv.ForEachNearPositionCellNodes(x, y, r, for1, static (st) =>
                {
                    var zoneSpace = st.Iterator as ZoneSpaceDivision.ZoneSpaceCellNode;
                    if (zoneSpace.ForEachStaticBlockable(st.Arg1, st.Arg2))
                    {
                        st.Break = true;
                    }
                });
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearUnits<ST>(float x, float y, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceUnit>
        {
            return mSpaceDiv.ForEachNearObjects<ST, InstanceUnit>(x, y, input, indexer);
        }
        public bool ForEachNearUnits<ST>(float x, float y, float r, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceUnit>
        {
            return mSpaceDiv.ForEachNearObjects<ST, InstanceUnit>(x, y, r, input, indexer);
        }
        public bool ForEachNearUnitsRect<ST>(float x1, float y1, float x2, float y2, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceUnit>
        {
            return mSpaceDiv.ForEachNearObjectsRect<ST, InstanceUnit>(x1, y1, x2, y2, input, indexer);
        }
        public bool ForEachNearUnitsRectWide<ST>(float x1, float y1, float x2, float y2, float wide, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceUnit>
        {
            return mSpaceDiv.ForEachNearObjectsRectWide<ST, InstanceUnit>(x1, y1, x2, y2, wide, input, indexer);
        }

        //----------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearObjects<ST>(float x, float y, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceZoneEntity>
        {
            return mSpaceDiv.ForEachNearObjects<ST, InstanceZoneEntity>(x, y, input, indexer);
        }
        public bool ForEachNearObjects<ST>(float x, float y, float r, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceZoneEntity>
        {
            return mSpaceDiv.ForEachNearObjects<ST, InstanceZoneEntity>(x, y, r, input, indexer);
        }
        public bool ForEachNearObjectsRect<ST>(float x1, float y1, float x2, float y2, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceZoneEntity>
        {
            return mSpaceDiv.ForEachNearObjectsRect<ST, InstanceZoneEntity>(x1, y1, x2, y2, input, indexer);
        }
        public bool ForEachNearObjectsRectWide<ST>(float x1, float y1, float x2, float y2, float wide, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<InstanceZoneEntity>
        {
            return mSpaceDiv.ForEachNearObjectsRectWide<ST, InstanceZoneEntity>(x1, y1, x2, y2, wide, input, indexer);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearObjects<ST, T>(float x, float y, ST input, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T> where T : IEntityObject
        {
            return mSpaceDiv.ForEachNearObjects<ST, T>(x, y, input, indexer, t);
        }
        public bool ForEachNearObjects<ST, T>(float x, float y, float r, ST input, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T> where T : IEntityObject
        {
            return mSpaceDiv.ForEachNearObjects<ST, T>(x, y, r, input, indexer, t);
        }
        public bool ForEachNearObjectsRect<ST, T>(float x1, float y1, float x2, float y2, ST input, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T> where T : IEntityObject
        {
            return mSpaceDiv.ForEachNearObjectsRect<ST, T>(x1, y1, x2, y2, input, indexer, t);
        }
        public bool ForEachNearObjectsRectWide<ST, T>(float x1, float y1, float x2, float y2, float wide, ST input, ForEachAction<ST> indexer, T t = default) where ST : ForEachInput<T> where T : IEntityObject
        {
            return mSpaceDiv.ForEachNearObjectsRectWide<ST, T>(x1, y1, x2, y2, wide, input, indexer,t);
        }

        //----------------------------------------------------------------------------------------------------------------------------
        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, ST input, ForEachPredicate<ST, T> indexer, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.ForEachNearObjectsPredicate<ST, T>(x, y, in input, indexer);
        }
        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, float r, ST input, ForEachPredicate<ST, T> indexer, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.ForEachNearObjectsPredicate<ST, T>(x, y, r, input, indexer);
        }
        public bool ForEachNearObjectsRectPredicate<ST, T>(float x1, float y1, float x2, float y2, ST input, ForEachPredicate<ST, T> indexer, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.ForEachNearObjectsRectPredicate<ST, T>(x1, y1, x2, y2, input, indexer);
        }
        public bool ForEachNearObjectsRectWidePredicate<ST, T>(float x1, float y1, float x2, float y2, float wide, ST input, ForEachPredicate<ST, T> indexer, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.ForEachNearObjectsRectWidePredicate<ST, T>(x1, y1, x2, y2, wide, input, indexer);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public bool TryGetNearObjects<ST, T>(float x, float y, ST input, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.TryGetNearObjects<ST, T>(x, y, in input, indexer, out result);
        }
        public bool TryGetNearObjects<ST, T>(float x, float y, float r, ST input, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.TryGetNearObjects<ST, T>(x, y, r, input, indexer, out result);
        }
        public bool TryGetNearObjectsRect<ST, T>(float x1, float y1, float x2, float y2, ST input, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.TryGetNearObjectsRect<ST, T>(x1, y1, x2, y2, input, indexer, out result);
        }
        public bool TryGetNearObjectsRectWide<ST, T>(float x1, float y1, float x2, float y2, float wide, ST input, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : InstanceZoneEntity
        {
            return mSpaceDiv.TryGetNearObjectsRectWide<ST, T>(x1, y1, x2, y2, wide, input, indexer, out result);
        }

        //----------------------------------------------------------------------------------------------------------------------------

        public int GetObjectsInSphere<ST, T>(in ST state, Collider.ObjectTouchSphere<ST> func, BoundingSphere sphere, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchSphere<ST>, BoundingSphere, List<T>, int, ST>(func, sphere, list, 0, state))
            {
                ForEachNearObjects(sphere.Center.X, sphere.Center.Y, sphere.Radius, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var sphere = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in sphere))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        public int GetObjectsCountInSphere<ST, T>(in ST state, Collider.ObjectTouchSphere<ST> func, BoundingSphere sphere, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchSphere<ST>, BoundingSphere, List<T>, int, ST>(func, sphere, null, 0, state))
            {
                ForEachNearObjects(sphere.Center.X, sphere.Center.Y, sphere.Radius, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var sphere = st.Arg2;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in sphere))
                    {
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取【矩形】范围内的所有单位
        /// </summary>
        public int GetObjectsInBox<ST, T>(in ST state, Collider.ObjectTouchBox<ST> func, BoundingBox box, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchBox<ST>, BoundingBox, List<T>, int, ST>(func, box, list, 0, state))
            {
                ForEachNearObjectsRect(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var box = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in box))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        public int GetObjectsCountInBox<ST, T>(in ST state, Collider.ObjectTouchBox<ST> func, BoundingBox box, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchBox<ST>, BoundingBox, List<T>, int, ST>(func, box, null, 0, state))
            {
                ForEachNearObjectsRect(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var box = st.Arg2;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in box))
                    {
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取【圆形】范围内的所有单位
        /// </summary>
        public int GetObjectsInCylinder<ST, T>(in ST state, Collider.ObjectTouchCylinder<ST> func, VoxelCylinder cylinder, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchCylinder<ST>, VoxelCylinder, List<T>, int, ST>(func, cylinder, list, 0, state))
            {
                ForEachNearObjects(cylinder.Center.X, cylinder.Center.Y, cylinder.Radius, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var cylinder = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in cylinder))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        public int GetObjectsCountInCylinder<ST, T>(in ST state, Collider.ObjectTouchCylinder<ST> func, VoxelCylinder cylinder, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchCylinder<ST>, VoxelCylinder, List<T>, int, ST>(func, cylinder, null, 0, state))
            {
                ForEachNearObjects(cylinder.Center.X, cylinder.Center.Y, cylinder.Radius, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var cylinder = st.Arg2;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in cylinder))
                    {
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取【扇形】范围内的所有单位，扇形范围为【弧度】
        /// </summary>
        public int GetObjectsInFan<ST, T>(in ST state, Collider.ObjectTouchFan<ST> func, VoxelFan fan, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchFan<ST>, VoxelFan, List<T>, int, ST>(func, fan, list, 0, state))
            {
                ForEachNearObjects(fan.Center.X, fan.Center.Y, fan.Radius, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var fan = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in fan))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取【直线】穿过范围内的所有单位(粗线段)
        /// </summary>
        public int GetObjectsInRectStripe<ST, T>(in ST state, Collider.ObjectTouchRectStripe<ST> func, VoxelRectStripe stripe, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5(func, stripe, list, 0, state, t))
            {
                ForEachNearObjectsRectWide(stripe.LineP.X, stripe.LineP.Y, stripe.LineQ.X, stripe.LineQ.Y, stripe.LineRaidus, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var stripe = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in stripe))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                }, t);
                return for1.Arg4;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  单位【运动轨迹】从A点移动到B点经过的碰撞
        /// </summary>
        public int GetObjectsInStripe<ST, T>(in ST state, Collider.ObjectTouchStripe<ST> func, VoxelStripe stripe, List<T> list, T t = default) where T : InstanceZoneEntity
        {
            using (var for1 = ObjectPool.AllocForEach5<InstanceZoneEntity, Collider.ObjectTouchStripe<ST>, VoxelStripe, List<T>, int, ST>(func, stripe, list, 0, state))
            {
                ForEachNearObjectsRectWide(stripe.LineP.X, stripe.LineP.Y, stripe.LineQ.X, stripe.LineQ.Y, stripe.LineRaidus, for1, static (st) =>
                {
                    var o = st.Iterator;
                    var func = st.Arg1;
                    var stripe = st.Arg2;
                    var list = st.Arg3;
                    var state = st.Arg5;
                    if (o is T t && func(state, t, in stripe))
                    {
                        list.Add(t);
                        st.Arg4++;
                    }
                });
                return for1.Arg4;
            }
        }

        #endregion
    }
}
