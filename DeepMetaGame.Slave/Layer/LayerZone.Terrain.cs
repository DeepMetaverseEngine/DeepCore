using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using static DeepMetaGame.Data.Message.PlayerSkillActiveChangedEvent;

namespace DeepCore.Game3D.Slave.Layer
{
    //-------------------------------------------------------------------------------------------

    partial class LayerZone
    {
        private ILayerZoneTerrain terrain;
        private WayPointAstar waypoint_path;
        private LayerSpaceDivision mSpaceDiv;
        public ILayerZoneTerrain Terrain3D { get => terrain; }

        protected virtual void InitTerrain(ClientEnterScene msg)
        {
            this.terrain = SlaveFactory.CreateClientTerrain(msg, this);
            if (terrain == null)
            {
                throw new Exception("Can Not Create Client Terrain : " + msg.sceneID);
            }
            this.waypoint_path = ZoneDataFactory.Factory.CreateWayPointAstar(this.Data);
            if (this.SpaceDivSizeW > 1)
            {
                this.mSpaceDiv = new LayerSpaceDivision(this);
                this.mSpaceDiv.Init();
            }
        }
        protected virtual void DisposeTerrain()
        {
            this.terrain?.Dispose();
            this.terrain = null;
            this.waypoint_path?.Dispose();
            this.waypoint_path = null;
            this.mSpaceDiv?.Dispose();
            this.mSpaceDiv = null;
        }


        /// <summary>
        /// 和地图做碰撞检测，是否阻挡
        /// </summary>
        public bool TouchMap(LayerUnit u)
        {
            return TryTouchMap(u, u.Position);
        }
        /// <summary>
        /// 尝试用新的坐标和地图碰撞检测
        /// </summary>
        public bool TryTouchMap(LayerUnit u, Geometry.Vector3 pos)
        {
            return terrain.TouchMapByPos(u, pos);
        }
        public static bool Intersects(in Geometry.Vector3 p1, in Geometry.Vector3 p2, float distance)
        {
            Geometry.Vector3.DistanceSquared(in p1, in p2, out var pd);
            return pd <= distance * distance;
        }

        /// <summary>
        /// 2个单位是否碰撞
        /// </summary>
        public bool TouchObject2(ILayerZoneEntity s, ILayerZoneEntity d)
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

        /// <summary>
        /// 尝试用新的坐标碰撞检测
        /// </summary>
        public bool TryTouchObject2(ILayerZoneEntity s, Geometry.Vector3 pos, ILayerZoneEntity d)
        {
            if (s == d) return false;
            var dsp = d.ZoneShape;
            if (dsp != null)
            {
                if (CMath.IsIntersectW(s.Z, s.BodyHeight, d.Z, d.BodyHeight))
                {
                    return dsp.Include(pos.X, pos.Y);
                }
                return false;
            }
            else
            {
                var v1 = s.VoxelBody;
                v1.Center = pos;
                return d.VoxelBody.Intersects(in v1);
            }
        }

        /// <summary>
        /// 获得和当前单位碰撞的单位
        /// </summary>
        public LayerUnit TouchUnit(LayerUnit src)
        {
            return TryTouchUnit(src, src.Position);
        }

        public LayerUnit TryTouchUnit(LayerUnit src, Geometry.Vector3 pos)
        {
            if (TryGetNearObjects(pos.X, pos.Y, src.BodyBlockSize, this, (LayerZone st, LayerUnit u) =>
            {
                if (((u != src) && (u.TouchObj) && TryTouchObject2(src, pos, u)))
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

        /// <summary>
        /// 获得和当前单位碰撞的建筑
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public ILayerZoneEntity TouchStaticBlock(LayerUnit src)
        {
            return TryTouchStaticBlock(src, src.Position);
        }


        //-------------------------------------------------------------------------------------------

        public ILayerZoneEntity TryTouchStaticBlock(LayerUnit src, Geometry.Vector3 pos)
        {
            using (var for1 = AllocForEach1<ILayerZoneEntity, LayerUnit>(src))
            {
                if (ForEachNearStaticBlock(pos.X, pos.Y, src.BodyBlockSize, for1, static (st) =>
                {
                    var u = st.Iterator;
                    var src = st.Arg1;
                    if (u is LayerEditorDecoration ed)
                    {
                        if (!ed.Enable) return;
                    }
                    if (u is LayerUnit ud)
                    {
                        if (!ud.IsActive || !ud.TouchObj) return;
                    }
                    if (u.Parent.TouchObject2(src, u))
                    {
                        st.Break = true;
                        return;
                    }
                    //                  else if (o is LayerEditorDecoration ed)
                    //                  {
                    //                      if (ed.Touch(src))
                    //                      {
                    //                          return true;
                    //                      }
                    //                  }
                    return;
                }))
                {
                    return for1.Iterator;
                }
            }
            return null;
        }

        public T GetNearUnit<T>(Geometry.Vector3 pos, float range, Predicate<T> select) where T : LayerZoneObject
        {
            T min = null;
            float min_len = float.MaxValue;
            float r2 = range * range;
            ForEachNearObjectsPredicate(pos.X, pos.Y, range, this, (LayerZone st, T u) =>
            {
                if (select(u))
                {
                    float len = Geometry.Vector3.DistanceSquared(u.Position, pos);
                    if (len <= r2 && min_len > len)
                    {
                        min_len = len;
                        min = u;
                    }
                }
                return false;
            });
            return min;
        }

        public LayerUnit GetNearTarget(LayerPlayer owner, float range, SkillTemplate.CastTarget expect = SkillTemplate.CastTarget.Enemy)
        {
            return GetNearUnit<LayerUnit>(owner.Position, range, (u) => { return IsAttackable(owner, u, expect); });
        }

        public LayerItem GetNearPickableItem(LayerPlayer owner, float range, bool no_touch = false)
        {
            float distance = int.MaxValue;
            LayerItem min = null;
            Geometry.Vector3 p1, p2;
            ForEachNearObjectsPredicate(owner.X, owner.Y, range, this, (LayerZone st, LayerItem item) =>
            {
                if (IsPickableItem(owner, item, no_touch))
                {
                    p1 = owner.Position;
                    p2 = item.Position;
                    Geometry.Vector3.DistanceSquared(in p1, in p2, out var pd);
                    if (pd < distance)
                    {
                        min = item;
                        distance = pd;
                    }
                }
                return false;
            });

            return min;
        }
        public LayerItem GetNearPickableItem(LayerPlayer owner, float range, int itemTemplateID, bool no_touch = false)
        {
            no_touch = true;
            float distance = int.MaxValue;
            LayerItem min = null;
            Geometry.Vector3 p1, p2;
            ForEachNearObjectsPredicate(owner.X, owner.Y, range, this, (LayerZone st, LayerItem item) =>
            {
                if (item.TemplateID == itemTemplateID && IsPickableItem(owner, item, no_touch))
                {
                    p1 = owner.Position;
                    p2 = item.Position;
                    Geometry.Vector3.DistanceSquared(in p1, in p2, out var pd);
                    if (pd < distance)
                    {
                        min = item;
                        distance = pd;
                    }
                }
                return false;
            });
            return min;
        }
        /// <summary>
        /// 判断是否可检取
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="item"></param>
        /// <param name="no_touch">不碰撞检测</param>
        /// <returns></returns>
        public virtual bool IsPickableItem(LayerPlayer owner, LayerItem item, bool no_touch = false)
        {
            if (no_touch || item.VoxelBody.Intersects(owner.VoxelBody))
            {
                if (item.APickable && (item.APickable.DropForAll || item.Force == owner.Force))
                {
                    return true;
                }
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------

        /// <summary>
        /// 获取技能可打到的单位
        /// </summary>
        /// <param name="src"></param>
        /// <param name="skill"></param>
        /// <param name="list"></param>
        public void GetSkillAttackableUnits(LayerUnit src, SkillTemplate skill, List<LayerUnit> list)
        {
            float range = src.GetSkillAttackRange(skill);
            ForEachNearObjectsPredicate(src.X, src.Y, range, this, (LayerZone st, LayerUnit u) =>
            {
                if (IsAttackable(src, u, skill.ExpectTarget))
                {
                    //if (CMath.intersectRound(src.X, src.Y, range, u.X, u.Y, u.BodyHitSize))
                    if (Geometry.CollisionMath.SphereIntersectSphere(src.Position, range, u.Position, u.BodyHitSize))
                    {
                        list.Add(u);
                    }
                }
                return false;
            });
        }

        public WayPointAstar.FlagGraphPath FindPathWayPoint(string srcName, string dstName)
        {
            if (waypoint_path == null)
            {
                this.waypoint_path = ZoneDataFactory.Factory.CreateWayPointAstar(mData);
            }
            return this.waypoint_path.FindPath(srcName, dstName);
        }
        /*
        public virtual WayPoint FindPathWayPointAsPathFinder(string srcName, string dstName)
        {
            var flags = FindPathWayPoint(srcName, dstName);
            if (flags != null)
            {
                var head = PathFinder.GenWayPoint(flags.X, flags.Y);
                var iter = head;
                flags = flags.Next;
                while (flags != null)
                {
                    var wp = PathFinder.GenWayPoint(flags.X, flags.Y);
                    iter.LinkNext(wp);
                    iter = wp;
                    flags = flags.Next;
                }
                return head;
            }
            return null;
        }
        */



        public T GetNearZoneFlag<T>(Geometry.Vector3 pos, Predicate<T> select = null) where T : LayerFlag
        {
            T min = null;
            float min_len = float.MaxValue;
            foreach (var flag in this.Flags)
            {
                if ((flag is T u) && (select == null || select(u)))
                {
                    float len = Geometry.Vector3.DistanceSquared(u.Position, pos);
                    if (min_len > len)
                    {
                        min_len = len;
                        min = u;
                    }
                }
            }
            return min;
        }

        public LayerEditorArea GetAreaByPos(Vector3 pos)
        {
            SpaceDiv.ClampPosition(pos.X, pos.Y, out var cx, out var cy);
            var cell = SpaceDiv.GetSpaceCell(cx, cy);
            if (cell is LayerSpaceDivision.ZoneSpaceCellNode sc)
            {
                return sc.Area;
            }
            return null;
        }


        public LayerSpaceDivision SpaceDiv { get => mSpaceDiv; }
        public float SpaceDivSizeW { get; private set; }
        public int SpaceXCount { get { return (mSpaceDiv != null) ? mSpaceDiv.SpaceXCount : 0; } }
        public int SpaceYCount { get { return (mSpaceDiv != null) ? mSpaceDiv.SpaceYCount : 0; } }

        protected internal virtual void SwapSpace(ILayerZoneEntity gu, bool dirty)
        {
            if (gu.CurrentCellNode != null)
            {
                var gpos = gu.Position;
                gu.CurrentCellNode.SwapSpace(gpos.X, gpos.Y, dirty);
            }
        }


        //-------------------------------------------------------------------------------------------------------//
        public bool ForEachNearStaticBlock<ST>(float x, float y, float r, ST input, ForEachAction<ST> indexer) where ST : ForEachInput<ILayerZoneEntity>
        {
            if (mSpaceDiv != null)
            {
                using (var for1 = AllocForEach2<ILayerZoneEntity, ST, ForEachAction<ST>>(input, indexer))
                {
                    return mSpaceDiv.ForEachNearObjects<ForEachInput<ILayerZoneEntity, ST, ForEachAction<ST>>, ILayerZoneEntity>(x, y, r, for1, static (st) =>
                    {
                        var obj = st.Iterator;
                        var input = st.Arg1;
                        var indexer = st.Arg2;
                        if (obj.IsStaticBlock)
                        {
                            input.Iterator = obj;
                            indexer(input);
                            if (input.Break)
                            {
                                st.Break = true;
                                return;
                            }
                        }
                    });
                }
            }
            else
            {
                {
                    using (var for1 = AllocForEach2<LayerUnit, ST, ForEachAction<ST>>(input, indexer))
                    {
                        if (mObjects.ForEachUnits<ForEachInput<LayerUnit, ST, ForEachAction<ST>>>(for1, static st =>
                        {
                            var obj = st.Iterator;
                            var input = st.Arg1;
                            var indexer = st.Arg2;
                            if (obj.IsStaticBlock)
                            {
                                input.Iterator = obj;
                                indexer(input);
                                if (input.Break)
                                {
                                    st.Break = true;
                                    return;
                                }
                            }
                        }))
                        {
                            return true;
                        }
                    }
                }
                {
                    using (var for1 = AllocForEach2<LayerEditorDecoration, ST, ForEachAction<ST>>(input, indexer))
                    {
                        if (mObjects.ForEachFlags<ForEachInput<LayerEditorDecoration, ST, ForEachAction<ST>>, LayerEditorDecoration>(for1, static st =>
                        {
                            var obj = st.Iterator;
                            var input = st.Arg1;
                            var indexer = st.Arg2;
                            if (obj.IsStaticBlock)
                            {
                                input.Iterator = obj;
                                indexer(input);
                                if (input.Break)
                                {
                                    st.Break = true;
                                    return;
                                }
                            }
                        }))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------//
        public bool TryGetNearObjects<ST, T>(float x, float y, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t =default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.TryGetNearObjects<ST, T>(x, y, in state, indexer, out result))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.TryGetObjects(in state, indexer, out result))
                {
                    return true;
                }
            }
            return false;
        }
        public bool TryGetNearObjects<ST, T>(float x, float y, float r, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.TryGetNearObjects<ST, T>(x, y, r, in state, indexer, out result))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.TryGetObjects<ST, T>(in state, indexer, out result))
                {
                    return true;
                }
            }
            return false;
        }
        public bool TryGetNearObjectsRect<ST, T>(float x1, float y1, float x2, float y2, in ST state, TryGetPredicate<ST, T> indexer, out T result, T t = default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.TryGetNearObjectsRect<ST, T>(x1, y1, x2, y2, in state, indexer, out result))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.TryGetObjects<ST, T>(in state, indexer, out result))
                {
                    return true;
                }
            }
            return false;
        }
        //-------------------------------------------------------------------------------------------------------//
        public bool ForEachNearObjectsPredicate<ST>(float x, float y, in ST state, ForEachPredicate<ST, LayerZoneObject> indexer)
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsPredicate<ST, LayerZoneObject>(x, y, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }
        public bool ForEachNearObjectsPredicate<ST>(float x, float y, float r, in ST state, ForEachPredicate<ST, LayerZoneObject> indexer)
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsPredicate<ST, LayerZoneObject>(x, y, r, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate<ST>(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }
        public bool ForEachNearObjectsRectPredicate<ST>(float x1, float y1, float x2, float y2, in ST state, ForEachPredicate<ST, LayerZoneObject> indexer)
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsRectPredicate<ST, LayerZoneObject>(x1, y1, x2, y2, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate<ST, LayerZoneObject>(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------//

        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, in ST state, ForEachPredicate<ST, T> indexer, T t = default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsPredicate<ST, T>(x, y, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }
        public bool ForEachNearObjectsPredicate<ST, T>(float x, float y, float r, in ST state, ForEachPredicate<ST, T> indexer, T t = default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsPredicate<ST, T>(x, y, r, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate<ST, T>(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }
        public bool ForEachNearObjectsRectPredicate<ST, T>(float x1, float y1, float x2, float y2, in ST state, ForEachPredicate<ST, T> indexer, T t = default) where T : LayerZoneObject
        {
            if (mSpaceDiv != null)
            {
                if (mSpaceDiv.ForEachNearObjectsRectPredicate<ST, T>(x1, y1, x2, y2, in state, indexer))
                {
                    return true;
                }
            }
            else
            {
                if (mObjects.ForEachObjectsPredicate<ST, T>(in state, indexer))
                {
                    return true;
                }
            }
            return false;
        }
        //-------------------------------------------------------------------------------------------------------//




        public ForEachInput<T, A1> AllocForEach1<T, A1>(A1 a1, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1>>();
            ret.Arg1 = a1;
            return ret;
        }
        public ForEachInput<T, A1, A2> AllocForEach2<T, A1, A2>(A1 a1, A2 a2, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            return ret;
        }
        public ForEachInput<T, A1, A2, A3> AllocForEach3<T, A1, A2, A3>(A1 a1, A2 a2, A3 a3, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            return ret;
        }
        public ForEachInput<T, A1, A2, A3, A4> AllocForEach4<T, A1, A2, A3, A4>(A1 a1, A2 a2, A3 a3, A4 a4, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3, A4>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            ret.Arg4 = a4;
            return ret;
        }
    }
}
