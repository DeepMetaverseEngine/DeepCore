using DeepCore.Geometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        //-------------------------------------------------------------------------------------------
        #region OBJECTS



        private uint mObjectIDIndexer = 0;
        private uint mAttacjIDIndexer = 0;

        internal uint genAttackGUID()
        {
            mAttacjIDIndexer++;
            return mAttacjIDIndexer;
        }
        internal uint genObjectID()
        {
            mObjectIDIndexer++;
            return mObjectIDIndexer;
        }

        // 获取一个单位
        public T GetObject<T>(uint obj_id) where T : InstanceZoneObject
        {
            if (obj_id == 0) return null;
            T go = mObjects.GetObject<T>(obj_id);
            if (go != null)
            {
                return go;
            }
            return null;
        }

        public InstanceUnit GetUnit(uint obj_id)
        {
            if (obj_id == 0) return null;
            return mObjects.GetObject<InstanceUnit>(obj_id);
        }
        public InstanceUnit GetUnitByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            foreach (InstanceUnit u in mObjects.Units)
            {
                if (name.Equals(u.Name))
                {
                    return u;
                }
            }
            return null;
        }
        public InstancePlayer GetPlayerByUUID(string playerUUID)
        {
            if (string.IsNullOrEmpty(playerUUID))
            {
                return null;
            }

            return mObjects.GetPlayer(playerUUID);
        }
        public InstanceItem GetItemByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            foreach (InstanceItem u in mObjects.Items)
            {
                if (name.Equals(u.Name))
                {
                    return u;
                }
            }
            return null;
        }
        /// <summary>
        /// 所有玩家
        /// </summary>
        public IReadOnlyList<InstancePlayer> AllPlayers { get { return mObjects.Players; } }
        public int AllPlayersCount { get { if (IsDisposing) return 0; return mObjects.PlayersCount; } }
        /// <summary>
        /// 所有Unit
        /// </summary>
        public IReadOnlyList<InstanceUnit> AllUnits { get { return mObjects.Units; } }
        public int AllUnitsCount { get { if (IsDisposing) return 0; return mObjects.UnitsCount; } }
        /// <summary>
        /// 所有Spell
        /// </summary>
        public IReadOnlyList<InstanceSpell> AllSpells { get { return mObjects.Spells; } }
        public int AllSpellsCount { get { if (IsDisposing) return 0; return mObjects.SpellsCount; } }
        /// <summary>
        /// 所有Item
        /// </summary>
        public IReadOnlyList<InstanceItem> AllItems { get { return mObjects.Items; } }
        public int AllItemsCount { get { if (IsDisposing) return 0; return mObjects.ItemsCount; } }
        /// <summary>
        /// 所有对象，包括Spell，Item，Unit
        /// </summary>
        public IReadOnlyList<InstanceZoneObject> AllObjects { get { return mObjects.Objects; } }
        public int AllObjectsCount { get { if (IsDisposing) return 0; return mObjects.ObjectsCount; } }



        public void SelectUnits<T>(Predicate<T> select, List<T> ret) where T : InstanceUnit
        {
            var units = mObjects.Units;
            var cnt = units.Count;
            InstanceUnit u = null;
            for (int i = 0; i < cnt; i++)
            {
                u = units[i];
                if (select(u as T))
                {
                    ret.Add(u as T);
                }
            }
        }
        public T SelectRandomUnit<T>(Predicate<T> select) where T : InstanceUnit
        {
            T ret = null;
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                foreach (InstanceUnit obj in mObjects.Units)
                {
                    if (obj is T && select(obj as T))
                    {
                        list.Add(obj as T);
                    }
                }
                if (list.Count > 0)
                {
                    ret = random.GetRandomInCollection<InstanceUnit>(list) as T;
                }
            }
            return ret;
        }
        public T SelectNearUnit<T>(Vector3 pos, Predicate<T> select) where T : InstanceUnit
        {
            var min = float.MaxValue;
            T ret = null;
            foreach (var obj in mObjects.Units)
            {
                if (obj is T t && select(t))
                {
                    var d = Vector3.DistanceSquared(obj.Position, pos);
                    if (d < min)
                    {
                        min = d;
                        ret = t;
                    }
                }
            }
            return ret;
        }
        public T SelectUnit<T>(Predicate<T> select) where T : InstanceUnit
        {
            foreach (InstanceUnit obj in mObjects.Units)
            {
                if (select(obj as T))
                {
                    return obj as T;
                }
            }
            return null;
        }

        public void SelectPlayers<T>(Predicate<T> select, List<T> ret) where T : InstancePlayer
        {
            var units = mObjects.Players;
            var cnt = units.Count;
            InstancePlayer u = null;
            for (int i = 0; i < cnt; i++)
            {
                u = units[i];
                if (select(u as T))
                {
                    ret.Add(u as T);
                }
            }
        }
        public T SelectRandomPlayer<T>(Predicate<T> select) where T : InstancePlayer
        {
            T ret = null;
            using (var list = ObjectPool.AllocList<InstancePlayer>())
            {
                foreach (InstancePlayer obj in mObjects.Players)
                {
                    if (obj is T && select(obj as T))
                    {
                        list.Add(obj as T);
                    }
                }
                if (list.Count > 0)
                {
                    ret = random.GetRandomInCollection<InstancePlayer>(list) as T;
                }
            }
            return ret;
        }
        public T SelectPlayer<T>(Predicate<T> select) where T : InstancePlayer
        {
            foreach (InstancePlayer obj in mObjects.Players)
            {
                if (select(obj as T))
                {
                    return obj as T;
                }
            }
            return null;
        }
        public T SelectRandomItem<T>(Predicate<T> select) where T : InstanceItem
        {
            T ret = null;
            using (var list = ObjectPool.AllocList<InstanceItem>())
            {
                foreach (var obj in mObjects.Items)
                {
                    if (obj is T && select(obj as T))
                    {
                        list.Add(obj as T);
                    }
                }
                if (list.Count > 0)
                {
                    ret = random.GetRandomInCollection<InstanceItem>(list) as T;
                }
            }
            return ret;
        }
        public T SelectNearItem<T>(Vector3 pos, Predicate<T> select) where T : InstanceItem
        {
            var min = float.MaxValue;
            T ret = null;
            foreach (var obj in mObjects.Items)
            {
                if (obj is T t && select(t))
                {
                    var d = Vector3.DistanceSquared(obj.Position, pos);
                    if (d < min)
                    {
                        min = d;
                        ret = t;
                    }
                }
            }
            return ret;
        }
        //-------------------------------------------------------------------------------------------

        protected virtual bool TryAddUnit(ref TAddUnit add)
        {
            if (mObjects.UnitsCount >= mMaxUnitCount)
            {
                log.ErrorFormat("Zone {0} Unit is full, MaxUnit={1}", this.SceneData?.Name, mMaxUnitCount);
                return false;
            }
            return true;
        }

        public InstanceUnit AddUnit(TAddUnit add)
        {
            if (TryAddUnit(ref add))
            {
                // 创建实体单位
                var unit = CreateUnit(add);
                // 存入单位列表
                if (unit != null)
                {
                    //unit.Name = name;
                    var pos = add.pos.Value;
                    // 存入单位列表
                    if (unit.tryAdd(pos, add.direction))
                    {
                        mObjects.AddObject(unit);
                        var out_event = ObjectPool.Alloc<AddUnitEvent>();
                        {
                            out_event.sender = unit;
                        }
                        ;
                        this.LastAddedUnit = unit;
                        if (unit is InstancePlayer player)
                        {
                            LastAddedPlayer = player;
                        }
                        if (add.summoner != null)
                        {
                            this.LastSummoner = add.summoner;
                            add.summoner.AddSummoned(unit);
                        }
                        try
                        {
                            unit.onAdded(this);
                        }
                        catch (Exception err)
                        {
                            out_event.ErrorMessage = err.Message;
                            throw;
                        }
                        finally
                        {
                            out_event.Sync = unit.GenSyncUnitInfo(true);
                            if (add.isDuplicate || IsLocalBattle)
                            {
                                out_event.Sync.template = unit.Info;
                            }
                        }
                        if (unit.ClientVisible)
                        {
                            PostEvent(out_event);
                        }
                        if (event_OnObjectAdded != null)
                            event_OnObjectAdded.Invoke(this, unit);
                        if (event_OnUnitAdded != null)
                            event_OnUnitAdded.Invoke(this, unit);
                        return unit;
                    }
                }
            }
            return null;
        }

        public InstanceItem AddItem(TAddItem add)
        {
            InstanceItem ret = CreateItem(add);
            var pos = add.pos.Value;
            if (ret.tryAdd(pos, add.direction))
            {
                mObjects.AddObject(ret);
                this.LastCreatedInstanceItem = ret;

                var out_event = ObjectPool.Alloc<AddItemEvent>();
                out_event.sender = ret;
                try
                {
                    ret.onAdded(this);
                    if (event_OnObjectAdded != null)
                        event_OnObjectAdded.Invoke(this, ret);
                    if (event_ItemAdded != null)
                        event_ItemAdded.Invoke(this, ret, add.creater);
                }
                catch (Exception err)
                {
                    out_event.ErrorMessage = err.Message;
                    throw;
                }
                finally
                {
                    out_event.Sync = ret.GenSyncItemInfo(true);
                    if (add.isDuplicate || IsLocalBattle)
                    {
                        out_event.Sync.template = ret.Info;
                    }
                }
                if (ret.ClientVisible)
                {
                    //add.out_event.creater_id = add.creater != null ? add.creater.ID : 0;
                    PostEvent(out_event);
                }
                return ret;
            }
            return null;
        }

        public InstanceSpell AddSpell(TAddSpell add)
        {
            if (add.template != null && add.sender != null)
            {
                InstanceUnit target = GetUnit(add.target_obj_id);
                switch (add.template.MType)
                {
                    case SpellTemplate.MotionType.BindingTarget:
                        if (target == null)
                        {
                            return null;
                        }
                        add.startPos = target.Position;
                        break;
                    case SpellTemplate.MotionType.SelectTarget:
                        if (add.targetPos != null)
                        {
                            add.startPos = add.targetPos.Value;
                        }
                        break;
                    case SpellTemplate.MotionType.SeekerMissile:
                    case SpellTemplate.MotionType.SeekerSelectTarget:
                        if (add.template.SeekingCooldownMS > 0)
                        {
                            target = null;
                            add.target_obj_id = 0;
                        }
                        break;
                    case SpellTemplate.MotionType.Chain:
                        //                         if (target == null)
                        //                         {
                        //                             return null;
                        //                         }
                        break;
                }

                if (add.launch.IsAutoSeekingTarget)
                {
                    //需要目标的spell提前扫描周围如果没有可攻击的目标则不再生成spell.//
                    var redirect = this.SeekSpellAttackable(
                        add.launcher,
                        add.template,
                        add.startPos,
                        add.launch.SeekingTargetRange,
                        add.template.ExpectTarget,
                        add.launch.SeekingTargetExpect,
                        add.launch.SeekingIgnoreInChain,
                        add.chain,
                        add.launch.SeekingAnchor,
                        add, static (add, target) =>
                        {
                            if (add.launch.InheritDamageTargetList && add.damage != null)
                            {
                                return target == add.damage;
                            }
                            return false;
                        });

                    if (redirect.unit != null)
                    {
                        target = redirect.unit;
                        add.target_obj_id = target.ObjectID;
                        if (add.startPos.HasValue)
                        {
                            add.direction = VectorHelper.GetDegree(add.startPos.Value, target.Position);
                        }
                        if (add.launch.SenderFaceToTarget)
                        {
                            if (add.sender is InstanceUnit senderUnit)
                            {
                                senderUnit.FaceTo(target.Position);
                                senderUnit.SendForceFaceSync();
                            }
                            else if (add.sender is InstanceSpell senderSpell)
                            {
                                senderSpell.FaceTo(target.Position);
                                senderSpell.PostForceSync();
                            }
                        }
                    }
                    else if (add.chain != null && add.chain.SpellID == add.template.ID)
                    {
                        return null;
                    }
                }
                if (add.template.IsNeedTarget)
                {
                    if (target == null)
                    {
                        return null;
                    }
                }
                // 创建实体单位
                InstanceSpell ret = CreateSpell(add);
                if (ret != null)
                {
                    //ret.Direction = direction;
                    ret.FaceTo(add.direction);
                    ret.setChainInfo(add.chain);
                    ret.setTargetPos(add.targetPos);
                    ret.setTarget(target, false);
                    if (add.launch.InheritDamageTargetList && add.damage != null)
                    {
                        ret.addHittedUnit(add.damage);
                    }
                    if (add.sender != null)
                    {
                        ret.SetAoiStatus(add.sender.AoiStatus);
                    }
                    var pos = add.startPos.Value;
                    // 存入单位列表
                    if (ret.tryAdd(pos, add.direction) && Formula.TryAddSpell(ret, add))
                    {
                        mObjects.AddObject(ret);
                        this.LastLaunchSpell = ret.Info;
                        this.cb_launchSpell(ret, add);
                        ret.onAdded(this);
                        if (event_OnObjectAdded != null)
                            event_OnObjectAdded.Invoke(this, ret);

                        if (ret.ClientVisible)
                        {
                            // 产生事件
                            var evt = ObjectPool.Alloc<AddSpellEvent>().Init(ret.LaunchData, ret);
                            {
                                var spell = ret;
                                evt.spell_id = spell.ID;
                                evt.sender_unit_id = spell.SenderID;
                                evt.launcher_unit_id = spell.LauncherID;
                                evt.target_obj_id = spell.TargetID;
                                evt.target_pos = spell.TargetPos;
                                evt.spell_pos = spell.Position;
                                evt.direction = spell.Direction;
                                evt.normal = spell.StartNormal;
                                evt.senderChain = (add.chain != null && add.chain.HasNextChain);
                                evt.startSpeed = spell.StartSpeed;
                                //mask//
                                evt.IsLauncherSender = (evt.launcher_unit_id == evt.sender_unit_id);
                                evt.IsTargetPos = (evt.target_pos != null);
                                evt.IsTargetObject = (evt.target_obj_id != 0);
                                evt.IsNormal = spell.StartNormal.HasValue;
                                evt.IsSyncPos = spell.TemplateData.IsLaunchSpellEventSyncPos;
                                evt.IsSpellMagnitude = spell.IsFromSpellMagnitude;
                                if (add.cloneTemplate || IsLocalBattle)
                                {
                                    evt.template = ret.Info;
                                }
                            }
                            evt.sender = ret;
                            PostEvent(evt);
                        }


                        return ret;
                    }
                }
            }
            return null;
        }

        //         private bool TryAutoSeekingTarget(AddSpell add, ref InstanceUnit target)
        //         {
        //             if (add.launch.IsAutoSeekingTarget)
        //             {
        //                 using (var list = ListObjectPool<InstanceUnit>.AllocAutoRelease())
        //                 {
        //                     var expectSeeking = add.launch.SeekingTargetExpect;
        //                     var chain = add.chain;
        //                     getObjectsRoundRange<InstanceUnit>((InstanceZoneObject o, float x, float y, float r) =>
        //                     {
        //                         InstanceUnit u = o as InstanceUnit;
        //                         if (chain != null)
        //                         {
        //                             switch (expectSeeking)
        //                             {
        //                                 case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
        //                                 case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
        //                                 case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
        //                                     if (chain.ContainsTarget(u))
        //                                     {
        //                                         return false;
        //                                     }
        //                                     break;
        //                                 default:
        //                                     if (chain.LastTarget == u)
        //                                     {
        //                                         return false;
        //                                     }
        //                                     break;
        //                             }
        //                         }
        //                         if (IsAttackable(add.launcher, u, add.template.ExpectTarget, AttackReason.Look, add.template))
        //                         {
        //                             return CMath.intersectRound(x, y, r, o.X, o.Y, o.BodyHitSize);
        //                         }
        //                         return false;
        //                     }, add.startPos.X, add.startPos.Y, add.launch.SeekingTargetRange, list);
        //                     switch (expectSeeking)
        //                     {
        //                         case SpellTemplate.SeekingExpect.Random:
        //                         case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
        //                             CUtils.RandomList(RandomN, list);
        //                             break;
        //                         case SpellTemplate.SeekingExpect.Nearest:
        //                         case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
        //                             list.Sort(new ObjectSorterNearest<InstanceUnit>(add.startPos.X, add.startPos.Y));
        //                             break;
        //                         case SpellTemplate.SeekingExpect.Farthest:
        //                         case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
        //                             list.Sort(new ObjectSorterFarthest<InstanceUnit>(add.startPos.X, add.startPos.Y));
        //                             break;
        //                     }
        //                     if (list.Count > 0)
        //                     {
        //                         target = list[0];
        // 
        //                         return true;
        //                     }
        //                 }
        //                 return false;
        //             }
        //             else
        //             {
        //                 return true;
        //             }
        //         }

        // 移除一个单位
        public bool RemoveObject(InstanceZoneObject obj)
        {
            if (mObjects.RemoveObject(obj))
            {
                obj.onRemoved(this);
                mObjectsRemoving.Add(obj);
                if (event_OnObjectRemoved != null)
                {
                    event_OnObjectRemoved.Invoke(this, obj);
                }
                if (obj is InstanceUnit)
                {
                    InstanceUnit unit = obj as InstanceUnit;
                    if (event_OnUnitRemoved != null)
                    {
                        event_OnUnitRemoved.Invoke(this, unit);
                    }
                }
                if (obj.ClientVisible)
                {
                    RemoveObjectEvent remove = ObjectPool.Alloc<RemoveObjectEvent>().Init(obj.ID);
                    remove.sender = obj;
                    PostEvent(remove);
                }
                return true;
            }
            return false;
        }

        public InstanceZoneObject RemoveObjectByID(uint oid)
        {
            var obj = mObjects.GetObject<InstanceZoneObject>(oid);
            if (obj != null && RemoveObject(obj))
            {
                return obj;
            }
            return null;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------

        private class InstanceZoneObjectMap
        {
            private class DirtyList<K, T> where T : InstanceZoneObject
            {
                private bool dirty = true;
                private List<T> mObjectsCollection = new List<T>();
                private HashMap<K, T> mObjects = new HashMap<K, T>();

                public int Count { get { return mObjects.Count; } }

                public void Add(K key, T obj)
                {
                    dirty = true;
                    mObjects.Add(key, obj);
                }
                public void Put(K key, T obj)
                {
                    dirty = true;
                    mObjects.Put(key, obj);
                }
                public bool Remove(K key)
                {
                    if (mObjects.Remove(key))
                    {
                        dirty = true;
                        return true;
                    }
                    return false;
                }
                public void Dispose()
                {
                    mObjectsCollection.Clear();
                    mObjects.Clear();
                }
                public T Get(K key)
                {
                    return mObjects.Get(key);
                }
                public bool ContainsKey(K key)
                {
                    return mObjects.ContainsKey(key);
                }
                internal IReadOnlyList<T> Refresh()
                {
                    if (dirty)
                    {
                        dirty = false;
                        mObjectsCollection.Clear();
                        mObjectsCollection.AddRange(mObjects.Values);
                    }
                    return mObjectsCollection;
                }
                public IReadOnlyList<T> List { get => Refresh(); }
            }

            private DirtyList<uint, InstanceZoneObject> mObjects = new DirtyList<uint, InstanceZoneObject>();
            private DirtyList<uint, InstanceUnit> mObjects_MirrorUnits = new DirtyList<uint, InstanceUnit>();
            private DirtyList<uint, InstanceSpell> mObjects_MirrorSpells = new DirtyList<uint, InstanceSpell>();
            private DirtyList<uint, InstanceItem> mObjects_MirrorItems = new DirtyList<uint, InstanceItem>();
            private DirtyList<string, InstancePlayer> mObjects_MirrorPlayers = new DirtyList<string, InstancePlayer>();

            internal void Refresh()
            {
                mObjects.Refresh();
                mObjects_MirrorUnits.Refresh();
                mObjects_MirrorSpells.Refresh();
                mObjects_MirrorItems.Refresh();
                mObjects_MirrorPlayers.Refresh();
            }

            public void AddObject(InstanceZoneObject obj)
            {
                mObjects.Add(obj.ID, obj);
                if (obj is InstanceUnit)
                {
                    mObjects_MirrorUnits.Add(obj.ID, obj as InstanceUnit);
                }
                else if (obj is InstanceSpell)
                {
                    mObjects_MirrorSpells.Add(obj.ID, obj as InstanceSpell);
                }
                else if (obj is InstanceItem)
                {
                    mObjects_MirrorItems.Add(obj.ID, obj as InstanceItem);
                }
                if (obj is InstancePlayer)
                {
                    var p = obj as InstancePlayer;
                    mObjects_MirrorPlayers.Add(p.PlayerUUID, p);
                }
            }
            public bool RemoveObject(InstanceZoneObject obj)
            {
                if (mObjects.Remove(obj.ID))
                {
                    if (obj is InstanceUnit)
                    {
                        mObjects_MirrorUnits.Remove(obj.ID);
                    }
                    else if (obj is InstanceSpell)
                    {
                        mObjects_MirrorSpells.Remove(obj.ID);
                    }
                    else if (obj is InstanceItem)
                    {
                        mObjects_MirrorItems.Remove(obj.ID);
                    }
                    if (obj is InstancePlayer)
                    {
                        var p = obj as InstancePlayer;
                        mObjects_MirrorPlayers.Remove(p.PlayerUUID);
                    }
                    return true;
                }
                return false;
            }
            public InstanceZoneObject GetObject(uint id)
            {
                return mObjects.Get(id);
            }
            public void Dispose()
            {
                mObjects.Dispose();
                mObjects_MirrorUnits.Dispose();
                mObjects_MirrorSpells.Dispose();
                mObjects_MirrorItems.Dispose();
                mObjects_MirrorPlayers.Dispose();
            }
            public T GetObject<T>(uint id) where T : InstanceZoneObject
            {
                Type type = typeof(T);
                if (type.IsSubclassOf(typeof(InstanceUnit)))
                {
                    return mObjects_MirrorUnits.Get(id) as T;
                }
                else if (type.IsSubclassOf(typeof(InstanceSpell)))
                {
                    return mObjects_MirrorSpells.Get(id) as T;
                }
                else if (type.IsSubclassOf(typeof(InstanceItem)))
                {
                    return mObjects_MirrorItems.Get(id) as T;
                }
                else
                {
                    return mObjects.Get(id) as T;
                }
            }
            public InstancePlayer GetPlayer(string uuid)
            {
                return mObjects_MirrorPlayers.Get(uuid);
            }

            public int ObjectsCount { get { return mObjects.Count; } }
            public IReadOnlyList<InstanceZoneObject> Objects { get { return mObjects.List; } }

            public int UnitsCount { get { return mObjects_MirrorUnits.Count; } }
            public IReadOnlyList<InstanceUnit> Units { get { return mObjects_MirrorUnits.List; } }

            public int SpellsCount { get { return mObjects_MirrorSpells.Count; } }
            public IReadOnlyList<InstanceSpell> Spells { get { return mObjects_MirrorSpells.List; } }

            public int ItemsCount { get { return mObjects_MirrorItems.Count; } }
            public IReadOnlyList<InstanceItem> Items { get { return mObjects_MirrorItems.List; } }

            public int PlayersCount { get { return mObjects_MirrorPlayers.Count; } }
            public IReadOnlyList<InstancePlayer> Players { get { return mObjects_MirrorPlayers.List; } }
        }

        #endregion
        //-------------------------------------------------------------------------------------------

        #region FLAGS

        readonly private HashMap<string, InstanceFlag> mFlags = new HashMap<string, InstanceFlag>();

        private bool mHasArea = false;

        public IReadOnlyCollection<InstanceFlag> AllFlags { get => mFlags.Values; }

        protected bool AddFlag(InstanceFlag flag)
        {
            if (!mFlags.ContainsKey(flag.Name))
            {
                mFlags.Add(flag.Name, flag);
                flag.onAdded();
                if (flag is ZoneArea)
                {
                    mHasArea = true;
                }
                return true;
            }
            return false;
        }

        public InstanceFlag GetFlag(string name)
        {
            if (name == null)
            {
                return null;
            }
            return mFlags.Get(name);
        }

        public T GetFlagAs<T>(string name) where T : InstanceFlag
        {
            return mFlags.Get(name) as T;
        }
        public bool TryGetFlagAs<T>(string name, out T flag) where T : InstanceFlag
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (mFlags.TryGetValue(name, out var f) && f is T t)
                {
                    flag = t;
                    return true;
                }
            }
            flag = null;
            return false;
        }

        public bool SetFlagEnable(string name, bool enable)
        {
            InstanceFlag flag = GetFlag(name);
            if (flag != null)
            {
                flag.Enable = enable;
                return true;
            }
            return false;
        }

        public T FindFlagAs<ST, T>(ST st, TryGetPredicate<ST, T> find) where T : InstanceFlag
        {
            foreach (var f in mFlags.Values)
            {
                if (f is T t && find(st, t)) { return t; }
            }
            return null;
        }
        public T FindFlagAs<T>(Predicate<T> find) where T : InstanceFlag
        {
            foreach (var f in mFlags.Values)
            {
                if (f is T t && find(t)) { return t; }
            }
            return null;
        }



        public ZoneRegion GetRegionWithPoint(Vector3 pos)
        {
            foreach (var f in mFlags.Values)
            {
                if (f is ZoneRegion region)
                {
                    if (region.isInRegion(pos))
                    {
                        return region;
                    }
                }
            }
            return null;
        }
        public ZoneRegion GetRegionWithObject(InstanceZoneObject pos)
        {
            foreach (var f in mFlags.Values)
            {
                if (f is ZoneRegion region)
                {
                    if (region.isInRegion(pos))
                    {
                        return region;
                    }
                }
            }
            return null;
        }
        public void GetFlagsWithPath(string path, List<InstanceFlag> flags)
        {
            foreach (var f in mFlags.Values)
            {
                if (f.EditorPath.StartsWith(path)) { flags.Add(f); }
            }
        }
        public InstanceFlag ForEachFlags<ST>(ST st, ForEachPredicate<ST, InstanceFlag> flag)
        {
            foreach (var f in mFlags.Values)
            {
                if (flag(st, f))
                {
                    return f;
                }
            }
            return null;
        }
        public void ForEachFlags<ST>(ST st, ForEachAction<ST, InstanceFlag> flag)
        {
            foreach (var f in mFlags.Values)
            {
                flag(st, f);
            }
        }

        public T SelectRandomFlag<T>(Predicate<T> select) where T : InstanceFlag
        {
            T ret = null;
            using (var list = ObjectPool.AllocList<InstanceFlag>())
            {
                foreach (var obj in mFlags.Values)
                {
                    if (obj is T && select(obj as T))
                    {
                        list.Add(obj as T);
                    }
                }
                if (list.Count > 0)
                {
                    ret = random.GetRandomInCollection<InstanceFlag>(list) as T;
                }
            }
            return ret;
        }

        #endregion
    }



}
