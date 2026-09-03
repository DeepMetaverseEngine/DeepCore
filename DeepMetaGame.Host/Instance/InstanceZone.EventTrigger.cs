using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Debug;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;
using ZoneNotify = DeepMetaGame.Data.Message.ZoneNotify;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceZone
    {
        public SceneData Data => this.SceneData;
        //---------------------------------------------------------------------------------------------
        private List<IEventExecutorCollection> bindingEvents = new List<IEventExecutorCollection>();

        public delegate void ZoneEventsHandler(InstanceZone zone, IEventExecutorCollection events);
        public event ZoneEventsHandler OnZoneBindEvents;
        public event ZoneEventsHandler OnZoneDisposeEvents;
        public event EventCollectionHander OnAddEventCollection;
        public event EventCollectionHander OnRemoveEventCollection;
        public event EventExecutorHander OnBeginTrace;
        public event EventTraceHander OnTrace;
        public IEnumerable<IEventExecutorCollection> AllEvents { get => bindingEvents; }
        AbstractCollectionPool IEventRuntime.ObjectPool => this.objectPool;

        void IEventRuntime.EventTrace(IEventExecutorCollection collection, EventExecutor exe, EventExternalizable data)
        {
            OnTrace?.Invoke(collection, exe, data);
        }
        void IEventRuntime.BeginTrace(EventExecutor exe)
        {
            OnBeginTrace?.Invoke(exe);
        }
        internal void cb_BindEvents(IEventExecutorCollection events)
        {
            bindingEvents.Add(events);
            OnAddEventCollection?.Invoke(events);
            OnZoneBindEvents?.Invoke(this, events);
        }
        internal void cb_DisposeEvents(IEventExecutorCollection events)
        {
            bindingEvents.Remove(events);
            OnRemoveEventCollection?.Invoke(events);
            OnZoneDisposeEvents?.Invoke(this, events);
        }
        private void ClearTriggerEvents()
        {
            this.OnZoneBindEvents = null;
            this.OnZoneDisposeEvents = null;
            this.OnAddEventCollection = null;
            this.OnRemoveEventCollection = null;
            this.OnBeginTrace = null;
            this.OnTrace = null;
        }

        //---------------------------------------------------------------------------------------------
        public void SendMessageBox(string message)
        {
            this.BroadcastMessageBox(message);
        }
        public void SendEvent(ZoneNotify evt)
        {
            this.PostEvent(evt);
        }
        public void SendGameOver(byte winForce, string message)
        {
            this.GameOver(winForce, message);
        }
        public void Run(System.Action task)
        {
            this.QueueTask(task, static (z, t) => t());
        }


        //---------------------------------------------------------------------------------------------

        public InstanceUnit AddUnit(DeepCore.GameData.Data.AddUnitParam add)
        {
            var add2 = new TAddUnit()
            {
                info = add.template,
                editor_name = add.name,
                player_uuid = add.player_uuid,
                displayName = add.displayName,
                alias = add.alias,
                level = add.level,
                force = add.force,
                pos = add.pos,
                direction = add.direction,
                summoner = add.summoner as InstanceUnit,
                arg = add.arg,
                //out_event = add.out_event,
            };
            var ret = this.AddUnit(add2);
            //add.out_event = add2.out_event;
            return ret;
        }

        public InstanceItem AddItem(DeepCore.GameData.Data.AddItemParam add)
        {
            return this.AddItem(new TAddItem()
            {
                template = add.template,
                name = add.name,
                force = add.force,
                pos = add.pos,
                direction = add.direction,
                creater = add.creater as InstanceUnit,
                arg = add.arg,
                //out_event = add.out_event,
            });
        }

        public T GetFlag<T>(string name) where T : InstanceFlag
        {
            return GetFlag(name) as T;
        }

        //         public ZoneArea GetArea(Geometry.Vector3 pos)
        //         {
        //             return GetAreaByPos(pos);
        //         }

        public T RemoveObject<T>(uint oid) where T : InstanceZoneObject
        {
            return RemoveObjectByID(oid) as T;
        }

        public InstanceUnit ForEachForceUnits<ST>(byte force, ST st, BreakPredicate<ST, InstanceUnit> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetForceUnits(force, list);
                foreach (var u in list)
                {
                    if (indexer(st, u)) return u;
                }
                return null;
            }
        }
        public InstanceUnit ForEachUnits<ST>(ST st, BreakPredicate<ST, InstanceUnit> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetAllUnits(list);
                foreach (var u in list)
                {
                    if (indexer(st, u)) return u;
                }
                return null;
            }
        }
        public InstanceItem ForEachItems<ST>(ST st, BreakPredicate<ST, InstanceItem> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceItem>())
            {
                GetAllItems(list);
                foreach (var u in list)
                {
                    if (indexer(st, u)) return u;
                }
                return null;
            }
        }

        public void ForEachUnits<ST>(ST st, ForEachAction<ST, InstanceUnit> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetAllUnits(list);
                foreach (var u in list)
                {
                    indexer(st, u);
                }
            }
        }
        public void ForEachItems<ST>(ST st, ForEachAction<ST, InstanceItem> indexer)
        {
            using (var list = ObjectPool.AllocList<InstanceItem>())
            {
                GetAllItems(list);
                foreach (var u in list)
                {
                    indexer(st, u);
                }
            }
        }

        //---------------------------------------------------------------------------------------------

        //         public T ForEachSpaceStaticBlock<T>(Geometry.Vector2 pos, BreakPredicate<T> indexer) where T : InstanceZoneObject
        //         {
        //             return ForEachNearStaticBlock(pos.X, pos.Y, 1f, (IEntityObject u) =>
        //             {
        //                 if (u is T t && indexer(t)) { return true; }
        //                 return false;
        //             }) as T;
        //         }
        // 
        //         public T ForEachSpaceObjects<T>(Geometry.Vector2 pos, BreakPredicate<T> indexer) where T : InstanceZoneObject
        //         {
        //             T ret = null;
        //             ForEachNearObjects(pos.X, pos.Y, (InstanceZoneEntity u, ref bool cancel) =>
        //             {
        //                 if (u is T)
        //                 {
        //                     if (indexer(u as T)) { ret = u as T; cancel = true; }
        //                 }
        //             });
        //             return ret;
        //         }
        // 
        //         public T ForEachSpaceObjectsInRound<T>(Geometry.Vector2 pos, float radius, BreakPredicate<T> indexer) where T : InstanceZoneObject
        //         {
        //             T ret = null;
        //             ForEachNearObjects(pos.X, pos.Y, radius, (InstanceZoneEntity u, ref bool cancel) =>
        //             {
        //                 if (u is T)
        //                 {
        //                     if (indexer(u as T)) { ret = u as T; cancel = true; }
        //                 }
        //             });
        //             return ret;
        //         }
        // 
        //         public T ForEachSpaceObjectsInRect<T>(Geometry.Vector2 min, Geometry.Vector2 max, BreakPredicate<T> indexer) where T : InstanceZoneObject
        //         {
        //             T ret = null;
        //             ForEachNearObjectsRect(min.X, min.Y, max.X, max.Y, (InstanceZoneEntity u, ref bool cancel) =>
        //             {
        //                 if (u is T)
        //                 {
        //                     if (indexer(u as T)) { ret = u as T; cancel = true; }
        //                 }
        //             });
        //             return ret;
        //         }

        //---------------------------------------------------------------------------------------------

        public bool TouchPositionObject2(InstanceZonePosition a, InstanceZonePosition b)
        {
            if ((a is IEntityObject ba) && (b is IEntityObject bb))
            {
                return TouchObject2(ba, bb);
            }
            return TouchObject2(a, b);
        }

        public T ForEachObjectsInSphere<T>(BoundingSphere pos, BreakPredicate<T> indexer) where T : InstanceZoneObject
        {
            using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
            {
                GetObjectsInSphere(this, Collider.Sphere_Touch_Position, pos, list);
                foreach (var u in list)
                {
                    if (indexer(u as T)) return u as T;
                }
                return null;
            }
        }

        public T ForEachObjectsInCylinder<T>(VoxelCylinder pos, BreakPredicate<T> indexer) where T : InstanceZoneObject
        {
            using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
            {
                GetObjectsInCylinder<InstanceZone, InstanceZoneEntity>(this, Collider.Cylinder_Touch_Position, pos, list);
                foreach (var u in list)
                {
                    if (indexer(u as T)) return u as T;
                }
                return null;
            }
        }

        public T ForEachObjectsInAABB<T>(BoundingBox aabb, BreakPredicate<T> indexer) where T : InstanceZoneObject
        {
            using (var list = ObjectPool.AllocList<InstanceZoneEntity>())
            {
                GetObjectsInBox(this, Collider.Box_Touch_Position, aabb, list);
                foreach (var u in list)
                {
                    if (indexer(u as T)) return u as T;
                }
                return null;
            }
        }

        //---------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------
        private InstanceUnit mLastAddedUnit;
        private InstanceUnit mLastSummoner;
        private InstancePlayer mLastAddedPlayer;
        private InstanceUnit mLastActivatedUnit;
        private InstanceUnit mLastRebirthUnit;
        private InstanceUnit mLastHittedUnit;
        private InstanceUnit mLastAttackUnit;
        private InstanceUnit mLastKilledUnit;

        private InstanceItem mLastCreatedInstanceItem;
        private InstanceItem mLastUnitGotInstanceItem;

        private ItemTemplate mLastUnitGotItem;
        private ItemTemplate mLastUnitLostItem;
        private ItemTemplate mLastUnitUseItem;
        private BuffTemplate mLastUnitGotBuff;

        private InstanceUnit mLastPickingItemUnit;
        private InstanceItem mLastPickingItem;
        private InstanceUnit mLastPickableUnit;

        private InstanceUnit mLastLaunchSkillUnit;
        private SkillTemplate mLastLaunchSkill;
        private SpellTemplate mLastLaunchSpell;

        private AuraTemplate mLastUnitLaunchAura;
        private AuraTemplate mLastUnitEnterAura;
        private AuraTemplate mLastUnitLeaveAura;

        //         private SendMessageR2B mLastRecvMessageR2B;
        //         private SendMessageB2R mLastSentMessageB2R;

        public InstanceUnit LastAddedUnit
        {
            get { return mLastAddedUnit; }
            private set { if (value != null) mLastAddedUnit = value; }
        }
        public InstanceUnit LastSummoner
        {
            get { return mLastSummoner; }
            private set { if (value != null) mLastSummoner = value; }
        }
        public InstancePlayer LastAddedPlayer
        {
            get { return mLastAddedPlayer; }
            private set { if (value != null) mLastAddedPlayer = value; }
        }
        public InstanceUnit LastActivatedUnit
        {
            get { return mLastActivatedUnit; }
            private set { if (value != null) mLastActivatedUnit = value; }
        }
        public InstanceUnit LastRebirthUnit
        {
            get { return mLastRebirthUnit; }
            private set { if (value != null) mLastRebirthUnit = value; }
        }
        public InstanceUnit LastHittedUnit
        {
            get { return mLastHittedUnit; }
            private set { if (value != null) mLastHittedUnit = value; }
        }
        public InstanceUnit LastAttackUnit
        {
            get { return mLastAttackUnit; }
            private set { if (value != null) mLastAttackUnit = value; }
        }
        public InstanceUnit LastKilledUnit
        {
            get { return mLastKilledUnit; }
            private set { if (value != null) mLastKilledUnit = value; }
        }
        public InstanceItem LastCreatedInstanceItem
        {
            get { return mLastCreatedInstanceItem; }
            private set { if (value != null) mLastCreatedInstanceItem = value; }
        }
        public InstanceItem LastUnitGotInstanceItem
        {
            get { return mLastUnitGotInstanceItem; }
            private set { if (value != null) mLastUnitGotInstanceItem = value; }
        }

        public ItemTemplate LastUnitGotInventoryItem
        {
            get { return mLastUnitGotItem; }
            private set { if (value != null) mLastUnitGotItem = value; }
        }
        public ItemTemplate LastUnitLostInventoryItem
        {
            get { return mLastUnitLostItem; }
            private set { if (value != null) mLastUnitLostItem = value; }
        }
        public ItemTemplate LastUnitUseItem
        {
            get { return mLastUnitUseItem; }
            private set { if (value != null) mLastUnitUseItem = value; }
        }
        public BuffTemplate LastUnitGotBuff
        {
            get { return mLastUnitGotBuff; }
            private set { if (value != null) mLastUnitGotBuff = value; }
        }
        public InstanceUnit LastPickingItemUnit
        {
            get { return mLastPickingItemUnit; }
            private set { if (value != null) mLastPickingItemUnit = value; }
        }
        public InstanceItem LastPickingItem
        {
            get { return mLastPickingItem; }
            private set { if (value != null) mLastPickingItem = value; }
        }
        public InstanceUnit LastPickableUnit
        {
            get { return mLastPickableUnit; }
            private set { if (value != null) mLastPickableUnit = value; }
        }


        public InstanceUnit LastLaunchSkillUnit
        {
            get { return mLastLaunchSkillUnit; }
            private set { if (value != null) mLastLaunchSkillUnit = value; }
        }
        public SkillTemplate LastLaunchSkill
        {
            get { return mLastLaunchSkill; }
            private set { if (value != null) mLastLaunchSkill = value; }
        }
        public SpellTemplate LastLaunchSpell
        {
            get { return mLastLaunchSpell; }
            private set { if (value != null) mLastLaunchSpell = value; }
        }


        public AuraTemplate LastUnitLaunchAura
        {
            get { return mLastUnitLaunchAura; }
            private set { if (value != null) mLastUnitLaunchAura = value; }
        }
        public AuraTemplate LastUnitEnterAura
        {
            get { return mLastUnitEnterAura; }
            private set { if (value != null) mLastUnitEnterAura = value; }
        }
        public AuraTemplate LastUnitLeaveAura
        {
            get { return mLastUnitLeaveAura; }
            private set { if (value != null) mLastUnitLeaveAura = value; }
        }

        //         public SendMessageR2B LastRecvMessageR2B
        //         {
        //             get { return mLastRecvMessageR2B; }
        //             private set { if (value != null) mLastRecvMessageR2B = value; }
        //         }
        //         public SendMessageB2R LastSentMessageB2R
        //         {
        //             get { return mLastSentMessageB2R; }
        //             private set { if (value != null) mLastSentMessageB2R = value; }
        //         }

        //         public string LastRecvMessageR2BMessage => this.LastRecvMessageR2B?.Message;
        //         public string LastSentMessageB2RMessage => this.LastSentMessageB2R?.Message;


    }
}
