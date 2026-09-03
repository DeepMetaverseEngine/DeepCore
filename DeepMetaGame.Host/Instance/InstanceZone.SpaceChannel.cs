using DeepCore.Game3D.Host.Helper;
using DeepCore.Space;
using DeepMetaGame.Data.Template;
using DeepCore.Geometry;
using DeepCore.Protocol;
using DeepMetaGame.Data.Helper;
using System.Collections.Generic;
using DeepMetaGame.Data.Message;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceZone
    {          //-------------------------------------------------------------------------------------------------------//

        #region SPACE_DIVISION

        public int SpaceXCount { get { return mSpaceDiv.SpaceXCount; } }
        public int SpaceYCount { get { return mSpaceDiv.SpaceYCount; } }
        public ZoneSpaceDivision.ZoneSpaceCellNode GetSpaceCellNode(Vector3 pos)
        {
            return mSpaceDiv.GetPositionCellNode(pos.X, pos.Y) as ZoneSpaceDivision.ZoneSpaceCellNode;
        }
        /// <summary>
        /// 按坐标取分割块
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public ZoneSpaceDivision.ZoneSpaceCellNode GetSpaceCellNode(float x, float y)
        {
            return mSpaceDiv.GetPositionCellNode(x, y) as ZoneSpaceDivision.ZoneSpaceCellNode;
        }
        /// <summary>
        /// 按格取分割块
        /// </summary>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public ZoneSpaceDivision.ZoneSpaceCellNode GetSpaceCellNodeByBlock(int bx, int by)
        {
            return mSpaceDiv.GetSpaceCellNodeByBlock(bx, by) as ZoneSpaceDivision.ZoneSpaceCellNode;
        }

        //----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 刷新空间分割位置为有改变
        /// </summary>
        /// <param name="obj"></param>
        internal void nearChange(IEntityObject obj)
        {
            if (obj.SpaceUserTag != null)
            {
                mSpaceDiv.MarkPosDirty(obj.SpaceUserTag);
            }
        }

        /// <summary>
        /// 清除空间位置
        /// </summary>
        /// <param name="obj"></param>
        internal void clearSpace(IEntityObject obj)
        {
            if (obj.SpaceUserTag != null)
            {
                obj.SpaceUserTag.Dispose();
            }
        }

        internal bool swapSpace(IEntityObject obj)
        {
            if (obj.SpaceUserTag != null)
            {
                var old_cell = obj.SpaceUserTag.SpaceCell;
                if (obj.SpaceUserTag.SwapSpace(obj.X, obj.Y, true) != null)
                {
                    var new_cell = obj.SpaceUserTag.SpaceCell;
                    if (obj is InstanceZoneObject zobj)
                    {
                        zobj.onSwapSpace(old_cell, new_cell);
                    }
                    if (event_OnObjectSpaceChanged != null)
                    {
                        event_OnObjectSpaceChanged.Invoke(this, obj, old_cell, new_cell);
                    }
                    return true;
                }
            }
            return false;
        }
        //         internal void swapArea(InstanceZoneObject obj, ZoneArea o, ZoneArea n)
        //         {
        //             if (o != null && o.Enable) { o.do_onUnitLeave(obj); }
        //             if (n != null && n.Enable) { n.do_onUnitEnter(obj); }
        //             if (event_OnObjectAreaChanged != null)
        //             {
        //                 event_OnObjectAreaChanged.Invoke(this, obj, o, n);
        //             }
        //         }
        //------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 判断是否附近有位置变化
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsNearChanged(float x, float y)
        {
            return mSpaceDiv.IsNearPosDirty(x, y);
        }
        public bool IsNearChanged(float x, float y, float r)
        {
            return mSpaceDiv.IsNearPosDirty(x, y, r);
        }
        public bool IsNearChanged(float x1, float y1, float x2, float y2)
        {
            return mSpaceDiv.IsNearPosDirty(x1, y1, x2, y2);
        }

        #endregion


        //----------------------------------------------------------------------------------------------------------------------------
        #region SYNC_POS

        /// <summary>
        /// 获得场景当前所有装饰物状态
        /// </summary>
        /// <returns></returns>
        public SyncFlagsEvent AllocSyncFlagsEvent()
        {
            var ret = ObjectPool.Alloc<SyncFlagsEvent>();
            foreach (InstanceFlag flag in mFlags.Values)
            {
                ret.Stats.Add(flag.Name, new SyncFlagsEvent.FlagState()
                {
                    tag = flag.Tag,
                    enable = flag.Enable
                });
                // if (flag is ZoneDecoration)
                //                 {
                //                     //ZoneDecoration d = flag as ZoneDecoration;
                //                     if (!flag.Enable)
                //                     {
                //                         ret.ClosedDecorations.Add(flag.Name);
                //                     }
                //                 }
                //                 if (flag.Tag != flag.SrcTag)
                //                 {
                //                     ret.ChangedTags.Add(flag.Name, flag.Tag);
                //                 }
            }
            return ret;
        }
        /// <summary>
        /// 获得所有单位同步信息，一般在进入场景时同步
        /// </summary>
        /// <param name="exclude"></param>
        /// <returns></returns>
        public SyncObjectsEvent AllocSyncUnitsEvent(InstanceUnit exclude = null)
        {
            var objs = mObjects.Objects;
            var ret = ObjectPool.Alloc<SyncObjectsEvent>();
            foreach (InstanceZoneObject o in objs)
            {
                if ((exclude == null || o != exclude) && o.ClientVisible)
                {
                    var sync = o.GenSyncInfo(true);
                    ret.Objects.Add(sync);
                }
            }
            return ret;
        }
        /// <summary>
        /// 获得半径内所有单位同步信息
        /// </summary>
        public SyncObjectsEvent AllocSyncUnitsEventInRange(Geometry.Vector3 pos, float r, InstanceUnit exclude = null)
        {
            var ret = ObjectPool.Alloc<SyncObjectsEvent>(); // new SyncObjectsEvent();
            var shape = new Geometry.BoundingSphere(pos, r);
            using (var for1 = ObjectPool.AllocForEach3<InstanceZoneEntity, BoundingSphere, InstanceUnit, SyncObjectsEvent>(shape, exclude, ret))
            {
                ForEachNearObjects(shape.Center.X, shape.Center.Y, r, for1, static (st) =>
                {
                    if (st.Iterator is InstanceUnit u)
                    {
                        var shape = st.Arg1;
                        var exclude = st.Arg2;
                        var ret = st.Arg3;
                        if ((exclude == null || u != exclude) && u.ClientVisible)
                        {
                            if (Collider.Sphere_Touch_Position(st, u, in shape))
                            {
                                var sync = u.GenSyncInfo(true);
                                ret.Objects.Add(sync);
                            }
                        }
                    }
                }); ;
            }
            return ret;
        }
        /// <summary>
        /// 获得空间分割块所有单位同步信息
        /// </summary>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <param name="exclude"></param>
        /// <returns></returns>
        public SyncObjectsEvent AllocSyncUnitsEventBySpace(int bx, int by, InstanceUnit exclude = null)
        {
            var space = GetSpaceCellNodeByBlock(bx, by);
            if (space != null)
            {
                SyncObjectsEvent ret = ObjectPool.Alloc<SyncObjectsEvent>();// new SyncObjectsEvent(space.Count);
                using (var for1 = ObjectPool.AllocForEach2<InstanceUnit, InstanceUnit, SyncObjectsEvent>(exclude, ret))
                {
                    space.ForEachChild<ForEachInput<InstanceUnit, InstanceUnit, SyncObjectsEvent>, InstanceUnit>(for1, static (st) =>
                    {
                        var o = st.Iterator;
                        var exclude = st.Arg1;
                        var ret = st.Arg2;
                        if ((exclude == null || o != exclude) && o.ClientVisible)
                        {
                            var sync = o.GenSyncInfo(true);
                            ret.Objects.Add(sync);
                        }
                    });
                }
                return ret;
            }
            return null;
        }

        public SyncObjectsEvent AllocSyncObjectsEvent(ICollection<InstanceZoneObject> objs)
        {
            SyncObjectsEvent ret = ObjectPool.Alloc<SyncObjectsEvent>();//new SyncObjectsEvent(objs.Count);
            foreach (InstanceZoneObject o in objs)
            {
                if (o.ClientVisible)
                {
                    var sync = o.GenSyncInfo(true);
                    ret.Objects.Add(sync);
                }
            }
            return ret;
        }

        /// <summary>
        /// 得到半径范围内的所有移动信息
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool TryGetSyncPosEventInRange(SyncPosEvent ret, Geometry.Vector3 pos, float range)
        {
            using (var units_pos = ObjectPool.AllocList<UnitSyncPos>())
            {
                var shape = new Geometry.BoundingSphere(pos, range);
                using (var for1 = ObjectPool.AllocForEach2<InstanceZoneEntity, BoundingSphere, List<UnitSyncPos>>(shape, units_pos))
                {
                    ForEachNearObjects(pos.X, pos.Y, range, for1, static (st) =>
                    {
                        if (st.Iterator is InstanceUnit u)
                        {
                            var shape = st.Arg1;
                            var units_pos = st.Arg2;
                            if (u.ClientVisible)
                            {
                                if (Collider.Sphere_Touch_Position(st, u, in shape))
                                {
                                    if (u.TryGetSyncPos(out var tpos))
                                    {
                                        units_pos.Add(tpos);
                                    }
                                }
                            }
                        }
                    });
                }
                if (ret.Init(units_pos.Count, PassTimeMS))
                {
                    ret.SetUnitList(units_pos);
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 得到空间分割块范围内的所有移动信息
        /// </summary>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public bool TryGetSyncPosEventBySpace(SyncPosEvent ret, int bx, int by)
        {
            var space = GetSpaceCellNodeByBlock(bx, by);
            if (space != null)
            {
                using (var units_pos = ObjectPool.AllocList<UnitSyncPos>())
                {
                    using (var for1 = ObjectPool.AllocForEach1(units_pos, default(InstanceUnit)))
                    {
                        space.ForEachChild(for1, static (st) =>
                        {
                            var u = st.Iterator;
                            var units_pos = st.Arg1;
                            if (u.ClientVisible)
                            {
                                if (u.TryGetSyncPos(out var pos))
                                {
                                    units_pos.Add(pos);
                                }
                            }
                        }, default(InstanceUnit));
                    }
                    if (ret.Init(units_pos.Count, PassTimeMS))
                    {
                        ret.SetUnitList(units_pos);
                        return true;
                    }
                    return false;
                }
            }
            ret.Init(0, (uint)PassTimeMS);
            return false;
        }
        public bool TryGetSyncPosEvent(SyncPosEvent ret, IReadOnlyList<InstanceZoneObject> objs)
        {
            using (var units_pos = ObjectPool.AllocList<UnitSyncPos>())
            {
                int objCount = objs.Count;
                InstanceZoneObject u;
                for (int i = 0; i < objCount; i++)
                {
                    u = objs[i];
                    if (u.ClientVisible)
                    {
                        if (u.TryGetSyncPos(out var pos))
                        {
                            units_pos.Add(pos);
                        }
                    }
                }
                //foreach (InstanceZoneObject u in objs)
                //{
                //    if (u.ClientVisible)
                //    {
                //        if (u.TryGetSyncPos(out var pos))
                //        {
                //            units_pos.Add(pos);
                //        }
                //    }
                //}
                if (ret.Init(units_pos.Count, PassTimeMS))
                {
                    ret.SetUnitList(units_pos);
                    return true;
                }
            }
            return false;
        }
        public bool TryGetSyncPosEvent(SyncPosEvent ret, IEnumerable<InstanceZoneObject> objs)
        {
            using (var units_pos = ObjectPool.AllocList<UnitSyncPos>())
            {
                foreach (InstanceZoneObject u in objs)
                {
                    if (u.ClientVisible)
                    {
                        if (u.TryGetSyncPos(out var pos))
                        {
                            units_pos.Add(pos);
                        }
                    }
                }
                if (ret.Init(units_pos.Count, PassTimeMS))
                {
                    ret.SetUnitList(units_pos);
                    return true;
                }
            }
            return false;
        }
        public SyncPosEvent AllocSyncPosEvent()
        {
            var ret = ObjectPool.Alloc<SyncPosEvent>();
            if (TryGetSyncPosEvent(ret, this.AllObjects))
            {
                return ret;
            }
            return null;
        }

        internal class PositionList
        {
            public bool Enable = true;

            private List<InstanceZoneObject> units = new List<InstanceZoneObject>();

            public void Add(InstanceZoneObject u)
            {
                if (Enable)
                {
                    units.Add(u);
                }
            }

            public void Clear()
            {
                units.Clear();
            }

            public SyncPosEvent AllocSyncPosEvent(InstanceZone zone)
            {
                if (Enable)
                {
                    using (var units_pos = zone.ObjectPool.AllocList<UnitSyncPos>())
                    {
                        for (int i = this.units.Count - 1; i >= 0; --i)
                        {
                            InstanceZoneObject u = this.units[i];
                            if (u.TryGetSyncPos(out var pos))
                            {
                                units_pos.Add(pos);
                            }
                        }
                        var ret = zone.ObjectPool.Alloc<SyncPosEvent>();
                        if (ret.Init(units_pos.Count, zone.PassTimeMS))
                        {
                            ret.SetUnitList(units_pos);
                        }
                        return ret;
                    }
                }
                return null;
            }

            public int Count { get { return units.Count; } }

        }

        readonly private PositionList sync_pos_list = new PositionList();

        #endregion

    }

    public interface IPostChannel
    {
        void Post(IMessage msg);
        void Flush(object owner);
    }
    public class ZoneSpaceDivision : SpaceDivision<IEntityObject>
    {
        //private int updateRange;
        //private TimeInterval updateTime;
        public InstanceZone Zone { get; }
        public ZoneSpaceDivision(InstanceZone zone) : base(
                zone.Terrain3D.TotalSizeX,
                zone.Terrain3D.TotalSizeY,
                zone.SceneData.SpaceDivW,
                zone.SceneData.SpaceDivW)
        {
            this.Zone = zone;
            this.OnObjectSwapped += ZoneSpaceDivision_OnObjectSwapped;
            //this.updateRange = zone.CFG.SPACE_UPDATE_NEAR_PLAYER_RANGE;
            //this.updateTime = new TimeInterval(zone.CFG.SPACE_UPDATE_NEAR_PLAYER_INTERVAL);
        }
        private Queue<SpaceCellNode> pendingUpdateNodes = new Queue<SpaceDivision<IEntityObject>.SpaceCellNode>();
        protected override SpaceCellNode CreateSpaceCellNode(int cx, int cy)
        {
            return new ZoneSpaceCellNode(Zone, cx, cy);
        }
        public override SpaceUserTag CreateUserTag(IEntityObject obj)
        {
            return new ZoneSpaceUserTag(this, obj);
        }
        protected internal virtual void Flush(InstanceZone zone)
        {
            //if (this.updateTime.Update(zone.UpdateIntervalMS))
            //{
            //                 this.ForEachSpaceCellNodes(this, static (st, cell) =>
            //                 {
            //                     var zc = (cell as ZoneSpaceCellNode);
            //                     zc.UpdatePlayer(st);
            //                 });
            //                 this.ForEachSpaceCellNodes(this, static (st, cell) =>
            //                 {
            //                     var zc = (cell as ZoneSpaceCellNode);
            //                     zc.UpdateNearPlayer(st);
            //                 });
            //}
            if (zone.ZoneChannel != null)
            {
                ForEachSpaceCellNodes(this, static (st, cell) =>
                {
                    (cell as ZoneSpaceCellNode).Flush(st);
                });
            }
        }
        private void ZoneSpaceDivision_OnObjectSwapped(SpaceUserTag obj_node, SpaceCellNode new_node, SpaceCellNode old_node)
        {
            var obj = obj_node.UserTag;
            if (new_node is ZoneSpaceCellNode new_space)
            {
                new_space.OnAdded(obj);
            }
            if (old_node is ZoneSpaceCellNode old_space)
            {
                old_space.OnRemoved(obj);
            }
        }
        public class ZoneSpaceCellNode : SpaceCellNode
        {
            private IPostChannel channel;
            protected List<IEntityObject> static_list = new List<IEntityObject>(1);
            //private int nearPlayerCount = 0;
            protected int currentPlayerCount = 0;
            //private int lastPlayerCount = 0;
            // 空间分割频道广播，减少AOI计算量
            //private List<InstanceZoneObject> sync_pos_units = new List<InstanceZoneObject>();
            public IPostChannel Channel { get => channel; }
            //public int PlayerCount { get => currentPlayerCount; }
            public int NearPlayerCount => currentPlayerCount;
            public bool HasNearPlayer => NearPlayerCount > 0;

            //public ZoneArea Area { get; internal set; }
            public ZoneSpaceCellNode(InstanceZone zone, int six, int siy) : base(six, siy)
            {
                this.channel = zone.HostFactory.CreateChannel(this);
            }
            public bool ForEachStaticBlockable<ST>(ST input, ForEachAction<ST> indexer) where ST : ForEachInput<IEntityObject>
            {
                foreach (var e in static_list)
                {
                    if (e.IsStaticBlock)
                    {
                        input.Iterator = e; indexer(input); if (input.Break) return true;
                    }
                }
                return false;
            }
            internal protected virtual void OnAdded(IEntityObject obj)
            {
                if (obj is InstancePlayer)
                {
                    this.currentPlayerCount++;
                    this.ForEachNears(this, static (ZoneSpaceCellNode st, ZoneSpaceCellNode ee) =>
                    {
                        ee.currentPlayerCount++;
                    });
                }
                if (obj.StaticBlockable)
                {
                    this.static_list.Add(obj);
                }
            }
            internal protected virtual void OnRemoved(IEntityObject obj)
            {
                if (obj is InstancePlayer)
                {
                    this.currentPlayerCount--;
                    this.ForEachNears(this, static (ZoneSpaceCellNode st, ZoneSpaceCellNode ee) =>
                    {
                        ee.currentPlayerCount--;
                    });
                }
                if (obj.StaticBlockable)
                {
                    this.static_list.Remove(obj);
                }
            }
            //             internal void UpdatePlayer(ZoneSpaceDivision div)
            //             {
            //                 this.lastPlayerCount = this.currentPlayerCount;
            //                 this.nearPlayerCount = this.currentPlayerCount;
            //             }
            //             internal void UpdateNearPlayer(ZoneSpaceDivision div)
            //             {
            //                 this.ForEachNears(this, static (ZoneSpaceCellNode st, ZoneSpaceCellNode ee) =>
            //                 {
            //                     st.nearPlayerCount += ee.lastPlayerCount;
            //                 });
            //             }
            internal protected virtual void Flush(ZoneSpaceDivision div)
            {
                channel.Flush(this);
                //  sync_pos_units.clear();
            }
            //             internal protected virtual void AddPosChange(InstanceZoneObject u)
            //             {
            //                 // sync_pos_units.Add(u);
            //             }
            //             protected bool TryGetSyncPosEvent(InstanceZone zone, out SyncPosEvent ret)
            //             {
            //                 //                 try
            //                 //                 {
            //                 //                     using (var units_pos = zone.ObjectPool.AllocList<UnitSyncPos>())
            //                 //                     {
            //                 //                         for (int i = this.sync_pos_units.Count - 1; i >= 0; --i)
            //                 //                         {
            //                 //                             InstanceZoneObject u = this.sync_pos_units[i];
            //                 //                             if (u.TryGetSyncPos(out var pos))
            //                 //                             {
            //                 //                                 units_pos.Add(pos);
            //                 //                             }
            //                 //                         }
            //                 //                         ret = new SyncPosEvent();
            //                 //                         if (ret.Init(units_pos.Count, zone.PassTimeMS))
            //                 //                         {
            //                 //                             ret.SetUnitList(units_pos);
            //                 //                             return true;
            //                 //                         }
            //                 //                     }
            //                 //                 }
            //                 //                 finally
            //                 //                 {
            //                 //                     sync_pos_units.Clear();
            //                 //                 }
            //                 ret = null;
            //                 return false;
            //             }

        }

        public class ZoneSpaceUserTag : SpaceUserTag
        {
            public ZoneSpaceUserTag(ZoneSpaceDivision div, IEntityObject obj) : base(div, obj)
            {
            }
            public IEntityObject Object { get { return base.UserTag as IEntityObject; } }
            new public ZoneSpaceUserTag Next { get { return (ZoneSpaceUserTag)base.Next; } }
            new public ZoneSpaceUserTag Prev { get { return (ZoneSpaceUserTag)base.Prev; } }
            new public ZoneSpaceCellNode SpaceCell { get { return (ZoneSpaceCellNode)base.SpaceCell; } }
            public override void MarkPosDirty()
            {
                base.MarkPosDirty();
                //SpaceCell.AddPosChange(Object as InstanceZoneObject);
            }
            public bool HasNearPlayer
            {
                get
                {
                    var cell = this.SpaceCell;
                    if (cell != null)
                    {
                        return cell.HasNearPlayer;
                    }
                    return false;
                }
            }
        }

    }

}
