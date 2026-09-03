using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;


namespace DeepCore.Game3D.Slave.Layer
{
    partial class LayerZone
    {
        private LayerObjectMap mObjects = new LayerObjectMap();
        private LayerPlayer mActor;

        /// <summary>
        /// 获取所有单位状态
        /// </summary>
        public IReadOnlyCollection<LayerZoneObject> Objects { get { return mObjects.Objects; } }
        public int ObjectsCount { get { return mObjects.ObjectsCount; } }

        public IReadOnlyCollection<LayerUnit> Units { get { return mObjects.Units; } }
        public int UnitsCount { get { return mObjects.UnitsCount; } }

        public IReadOnlyCollection<LayerSpell> Spells { get { return mObjects.Spells; } }
        public int SpellsCount { get { return mObjects.SpellsCount; } }

        public IReadOnlyCollection<LayerItem> Items { get { return mObjects.Items; } }
        public int ItemsCount { get { return mObjects.ItemsCount; } }

        /// <summary>
        /// 获取所有装饰物状态
        /// </summary>
        public IReadOnlyCollection<LayerFlag> Flags { get { return mObjects.Flags; } }

        /// <summary>
        /// 主角
        /// </summary>
        public LayerPlayer Actor { get { return mActor; } }

        /// <summary>
        /// 主角ID
        /// </summary>
        public uint ActorID { get { return mActor != null ? mActor.ObjectID : 0; } }


        /// <summary>
        /// 获取一个可显示对象
        /// </summary>
        /// <typeparam name="T">ZoneObject的子类</typeparam>
        /// <param name="objID"></param>
        /// <returns></returns>
        public T GetObjectAs<T>(uint objID) where T : LayerZoneObject
        {
            return mObjects.GetObjectAs<T>(objID);
        }
        /// <summary>
        /// 获取一个可显示对象
        /// </summary>
        /// <param name="objID"></param>
        /// <returns></returns>
        public LayerZoneObject GetObject(uint objID)
        {
            return mObjects.GetObject(objID);
        }
        /// <summary>
        /// 获取一个单位
        /// </summary>
        /// <param name="objID"></param>
        /// <returns></returns>
        public LayerUnit GetUnit(uint objID)
        {
            return mObjects.GetUnit(objID);
        }
        public LayerItem GetItem(uint objID)
        {
            return mObjects.GetItem(objID);
        }
        public LayerSpell GetSpell(uint objID)
        {
            return mObjects.GetSpell(objID);
        }
        /// <summary>
        /// 根据模板找单位
        /// </summary>
        /// <param name="templateID"></param>
        /// <returns></returns>
        public LayerUnit GetUnitByTemplateID(int templateID)
        {
            foreach (var u in mObjects.Units)
            {
                if (u.TemplateID == templateID)
                {
                    return u;
                }
            }
            return null;
        }
        /// <summary>
        /// 获取一个玩家单位
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public LayerUnit GetPlayerUnit(string uuid)
        {
            return mObjects.GetPlayer(uuid);
        }

        public virtual bool IsAttackable(LayerUnit src, LayerUnit target, SkillTemplate.CastTarget expectTarget)
        {
            if (target.IsActive)
            {
                switch (expectTarget)
                {
                    case SkillTemplate.CastTarget.Enemy:
                        return src.Force != target.Force;

                    case SkillTemplate.CastTarget.Enemy_Monster:
                        return src.Force != target.Force && src.UType == UnitType.TYPE_MONSTER;

                    case SkillTemplate.CastTarget.Enemy_Player:
                        return src.Force != target.Force && src.UType == UnitType.TYPE_PLAYER;

                    case SkillTemplate.CastTarget.PetForMaster:
                        if (src.UType == UnitType.TYPE_PET)
                        {
                            return (src != target) && (src.Force == target.Force);
                        }
                        return false;
                    case SkillTemplate.CastTarget.AlliesExcludeSelf:
                        return (src != target) && (src.Force == target.Force);
                    //                     case SkillTemplate.CastTarget.AlliesExcludeSelf:
                    //                         return (src != target) && (src.Force == target.Force);


                    case SkillTemplate.CastTarget.AlliesIncludeSelf:
                        return (src.Force == target.Force);

                    case SkillTemplate.CastTarget.EveryOne:
                        return true;
                    case SkillTemplate.CastTarget.EveryOneExcludeSelf:
                        return (src != target);

                    case SkillTemplate.CastTarget.Self:
                        return src == target;
                    case SkillTemplate.CastTarget.NA:
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取一个编辑器标记
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetFlag<T>(string name) where T : LayerFlag
        {
            return mObjects.GetFlag<T>(name);
        }
        public LayerFlag GetFlag(string name)
        {
            return mObjects.GetFlag(name);
        }
        public LayerEditorUnit GetUnitFlagByTemplateID(int templateID)
        {
            if (mObjects.TryGetFlags(templateID, static (int id, LayerEditorUnit u) =>
            {
                if (u.Data.UnitTemplateID == id)
                {
                    return true;
                }
                return false;
            }, out var result)) return result;
            return null;
        }


        public T FindFlagWithAbility<T>(Type ability) where T : LayerFlag
        {
            foreach (var flag in this.Flags)
            {
                if (flag is T region)
                {
                    if (region.EditorData.GetAbilityWithType(ability) != null)
                    {
                        return region;
                    }
                }
            }
            return null;
        }


        public T FindFlag<T>(Predicate<T> predicate) where T : LayerFlag
        {
            foreach (var flag in this.Flags)
            {
                if (flag is T region && predicate(region))
                {
                    return region;
                }
            }
            return null;
        }
        public List<T> FindFlags<T>(Predicate<T> predicate) where T : LayerFlag
        {
            var list = new List<T>();
            foreach (var flag in this.Flags)
            {
                if (flag is T region && predicate(region))
                {
                    list.Add(region);
                }
            }
            return list;
        }
        public T FindRandomFlag<T>(Random random, Predicate<T> predicate) where T : LayerFlag
        {
            var list = FindFlags(predicate);
            return random.GetRandomInCollection(list);
        }
        public bool ForEachFlags<ST, T>(in ST state, ForEachPredicate<ST, T> action) where T : LayerFlag
        {
            return mObjects.ForEachFlagsPredicate<ST, T>(state, action);
        }
        public bool TryGetFlag<ST, T>(in ST state, TryGetPredicate<ST, T> action, out T flag) where T : LayerFlag
        {
            return mObjects.TryGetFlags<ST, T>(in state, action, out flag);
        }
        //-------------------------------------------------------------------------------------------------------//
        //
        //-------------------------------------------------------------------------------------------------------//

        private class LayerObjectMap
        {
            private HashMap<uint, LayerZoneObject> mObjects = new HashMap<uint, LayerZoneObject>();
            private HashMap<uint, LayerUnit> mObjects_MirrorUnits = new HashMap<uint, LayerUnit>();
            private HashMap<uint, LayerSpell> mObjects_MirrorSpells = new HashMap<uint, LayerSpell>();
            private HashMap<uint, LayerItem> mObjects_MirrorItems = new HashMap<uint, LayerItem>();
            private HashMap<string, LayerUnit> mObjects_MirrorPlayers = new HashMap<string, LayerUnit>();
            private HashMap<string, LayerFlag> mFlags = new HashMap<string, LayerFlag>();

            public IReadOnlyCollection<LayerFlag> Flags { get { return mFlags.Values; } }
            public void AddFlag(LayerFlag flag)
            {
                mFlags.Add(flag.Name, flag);
            }
            public T GetFlag<T>(string name) where T : LayerFlag
            {
                return mFlags.Get(name) as T;
            }
            public LayerFlag GetFlag(string name)
            {
                return mFlags.Get(name);
            }


            public bool ForEachFlags<ST>(in ST input, ForEachAction<ST> action) where ST : ForEachInput<LayerFlag>
            {
                foreach (var flag in mFlags.Values)
                {
                    input.Iterator = flag;
                    action(input);
                    if (input.Break)
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ForEachFlags<ST, T>(in ST input, ForEachAction<ST> action) where ST : ForEachInput<T> where T : LayerFlag
            {
                foreach (var flag in mFlags.Values)
                {
                    if (flag is T t)
                    {
                        input.Iterator = t;
                        action(input);
                        if (input.Break)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public bool ForEachFlagsPredicate<ST>(in ST input, ForEachPredicate<ST, LayerFlag> action)
            {
                foreach (var flag in mFlags.Values)
                {
                    if (action(input, flag))
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ForEachFlagsPredicate<ST, T>(in ST input, ForEachPredicate<ST, T> action) where T : LayerFlag
            {
                foreach (var flag in mFlags.Values)
                {
                    if (flag is T t)
                    {
                        if (action(input, t))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public bool TryGetFlags<ST>(in ST input, TryGetPredicate<ST, LayerFlag> action, out LayerFlag result)
            {
                foreach (var flag in mFlags.Values)
                {
                    if (action(input, flag))
                    {
                        result = flag;
                        return true;
                    }
                }
                result = default(LayerFlag);
                return false;
            }
            public bool TryGetFlags<ST, T>(in ST input, TryGetPredicate<ST, T> action, out T result) where T : LayerFlag
            {
                foreach (var flag in mFlags.Values)
                {
                    if (flag is T t)
                    {
                        if (action(input, t))
                        {
                            result = t;
                            return true;
                        }
                    }
                }
                result = default(T);
                return false;
            }


            public T GetObjectAs<T>(uint id) where T : LayerZoneObject
            {
                Type type = typeof(T);
                LayerZoneObject obj;
                if (type.IsSubclassOf(typeof(LayerUnit)))
                {
                    return mObjects_MirrorUnits.Get(id) as T;
                }
                else if (type.IsSubclassOf(typeof(LayerSpell)))
                {
                    return mObjects_MirrorSpells.Get(id) as T;
                }
                else if (type.IsSubclassOf(typeof(LayerItem)))
                {
                    return mObjects_MirrorItems.Get(id) as T;
                }
                else if (mObjects.TryGetValue(id, out obj))
                {
                    return obj as T;
                }
                return null;
            }
            public LayerZoneObject GetObject(uint id)
            {
                return mObjects.Get(id);
            }
            public LayerUnit GetPlayer(string uuid)
            {
                return mObjects_MirrorPlayers.Get(uuid);
            }
            public LayerUnit GetUnit(uint id)
            {
                return mObjects_MirrorUnits.Get(id);
            }
            public LayerItem GetItem(uint id)
            {
                return mObjects_MirrorItems.Get(id);
            }
            public LayerSpell GetSpell(uint id)
            {
                return mObjects_MirrorSpells.Get(id);
            }


            public void Add(LayerZoneObject obj)
            {
                mObjects.Add(obj.ObjectID, obj);
                if (obj is LayerUnit)
                {
                    var u = obj as LayerUnit;
                    mObjects_MirrorUnits.Add(obj.ObjectID, u);
                    if (!string.IsNullOrEmpty(u.PlayerUUID))
                    {
                        mObjects_MirrorPlayers.Put(u.PlayerUUID, u);
                    }
                }
                else if (obj is LayerItem)
                {
                    mObjects_MirrorItems.Add(obj.ObjectID, obj as LayerItem);
                }
                else if (obj is LayerSpell)
                {
                    mObjects_MirrorSpells.Add(obj.ObjectID, obj as LayerSpell);
                }
            }
            public LayerZoneObject RemoveObjectByKey(uint id)
            {
                LayerZoneObject obj = mObjects.RemoveByKey(id);
                if (obj != null)
                {
                    try
                    {
                        if (obj is LayerUnit)
                        {
                            var u = obj as LayerUnit;
                            mObjects_MirrorUnits.Remove(id);
                            if (!string.IsNullOrEmpty(u.PlayerUUID))
                            {
                                mObjects_MirrorPlayers.Remove(u.PlayerUUID);
                            }
                        }
                        else if (obj is LayerItem)
                        {
                            mObjects_MirrorItems.Remove(id);
                        }
                        else if (obj is LayerSpell)
                        {
                            mObjects_MirrorSpells.Remove(id);
                        }
                    }
                    finally
                    {
                        obj.OnRemove();
                    }
                }
                return obj;
            }
            public void Dispose()
            {
                foreach (var f in mObjects.Values)
                {
                    f.Dispose();
                }
                mObjects.Clear();
                mObjects_MirrorItems.Clear();
                mObjects_MirrorSpells.Clear();
                mObjects_MirrorUnits.Clear();
                mObjects_MirrorPlayers.Clear();
                foreach (var f in mFlags.Values)
                {
                    f.Dispose();
                }
                mFlags.Clear();
            }
            public bool ContainsObjectByKey(uint id)
            {
                return mObjects.ContainsKey(id);
            }

            //---------------------------------------------------------------------------------------------------------------------

            public bool ForEachObjects<ST>(ST input, ForEachAction<ST> action) where ST : ForEachInput<LayerZoneObject>
            {
                foreach (var u in mObjects.Values)
                {
                    input.Iterator = u;
                    action(input);
                    if (input.Break)
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ForEachUnits<ST>(ST input, ForEachAction<ST> action) where ST : ForEachInput<LayerUnit>
            {
                foreach (var u in mObjects_MirrorUnits.Values)
                {
                    input.Iterator = u as LayerUnit;
                    action(input);
                    if (input.Break)
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ForEachObjects<ST, T>(ST input, ForEachAction<ST> action) where ST : ForEachInput<T> where T : LayerZoneObject
            {
                Type type = typeof(T);
                if (type.IsSubclassOf(typeof(LayerUnit)))
                {
                    foreach (var u in mObjects_MirrorUnits.Values)
                    {
                        input.Iterator = u as T;
                        action(input);
                        if (input.Break)
                        {
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerSpell)))
                {
                    foreach (var u in mObjects_MirrorSpells.Values)
                    {
                        input.Iterator = u as T;
                        action(input);
                        if (input.Break)
                        {
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerItem)))
                {
                    foreach (var u in mObjects_MirrorItems.Values)
                    {
                        input.Iterator = u as T;
                        action(input);
                        if (input.Break)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var u in mObjects.Values)
                    {
                        input.Iterator = u as T;
                        action(input);
                        if (input.Break)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            //---------------------------------------------------------------------------------------------------------------------

            public bool ForEachObjectsPredicate<ST>(in ST input, ForEachPredicate<ST, LayerZoneObject> action)
            {
                //改
                foreach (var item in mObjects)
                {
                    if (action(input, item.Value))
                    {
                        return true;
                    }
                }


                return false;
            }
            public void ForEachObjects<ST>(in ST input, Action<ST, LayerZoneObject> action)
            {
                //改
                foreach (var item in mObjects)
                {
                    action(input, item.Value);
                }
            }
            public bool ForEachUnitsPredicate<ST>(in ST input, ForEachPredicate<ST, LayerUnit> action)
            {
                foreach (var u in mObjects_MirrorUnits.Values)
                {
                    if (action(input, u))
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ForEachObjectsPredicate<ST, T>(in ST input, ForEachPredicate<ST, T> action) where T : LayerZoneObject
            {
                Type type = typeof(T);
                if (type.IsSubclassOf(typeof(LayerUnit)))
                {
                    foreach (var u in mObjects_MirrorUnits.Values)
                    {
                        if (action(input, u as T))
                        {
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerSpell)))
                {
                    foreach (var u in mObjects_MirrorSpells.Values)
                    {
                        if (action(input, u as T))
                        {
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerItem)))
                {
                    foreach (var u in mObjects_MirrorItems.Values)
                    {
                        if (action(input, u as T))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var u in mObjects.Values)
                    {
                        if (action(input, u as T))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            //---------------------------------------------------------------------------------------------------------------------

            public bool TryGetObjects<ST>(in ST input, TryGetPredicate<ST, LayerZoneObject> action, out LayerZoneObject result)
            {
                foreach (var u in mObjects.Values)
                {
                    if (action(input, u))
                    {
                        result = u;
                        return true;
                    }
                }
                result = null;
                return false;
            }
            public bool TryGetUnits<ST>(in ST input, TryGetPredicate<ST, LayerUnit> action, out LayerUnit result)
            {
                foreach (var u in mObjects_MirrorUnits.Values)
                {
                    if (action(input, u))
                    {
                        result = u;
                        return true;
                    }
                }
                result = null;
                return false;
            }
            public bool TryGetObjects<ST, T>(in ST input, TryGetPredicate<ST, T> action, out T result) where T : LayerZoneObject
            {
                Type type = typeof(T);
                if (type.IsSubclassOf(typeof(LayerUnit)))
                {
                    foreach (var u in mObjects_MirrorUnits.Values)
                    {
                        if (action(input, u as T))
                        {
                            result = u as T;
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerSpell)))
                {
                    foreach (var u in mObjects_MirrorSpells.Values)
                    {
                        if (action(input, u as T))
                        {
                            result = u as T;
                            return true;
                        }
                    }
                }
                else if (type.IsSubclassOf(typeof(LayerItem)))
                {
                    foreach (var u in mObjects_MirrorItems.Values)
                    {
                        if (action(input, u as T))
                        {
                            result = u as T;
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var u in mObjects.Values)
                    {
                        if (action(input, u as T))
                        {
                            result = u as T;
                            return true;
                        }
                    }
                }
                result = default;
                return false;
            }

            //---------------------------------------------------------------------------------------------------------------------

            public IReadOnlyCollection<LayerZoneObject> Objects { get { return mObjects.Values; } }
            public int ObjectsCount { get { return mObjects.Count; } }

            public IReadOnlyCollection<LayerUnit> Units { get { return mObjects_MirrorUnits.Values; } }
            public int UnitsCount { get { return mObjects_MirrorUnits.Count; } }

            public IReadOnlyCollection<LayerSpell> Spells { get { return mObjects_MirrorSpells.Values; } }
            public int SpellsCount { get { return mObjects_MirrorSpells.Count; } }

            public IReadOnlyCollection<LayerItem> Items { get { return mObjects_MirrorItems.Values; } }
            public int ItemsCount { get { return mObjects_MirrorItems.Count; } }

            //---------------------------------------------------------------------------------------------------------------------


        }
    }
}
