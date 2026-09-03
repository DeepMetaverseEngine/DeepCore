using DeepCore;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Template
{
    public class TemplateManager
    {
        //----------------------------------------------------------------------------------------------
        #region _STATIC_

        private static bool s_IsEditor = false;
        public static bool IsEditor { get { return s_IsEditor; } set { s_IsEditor = value; } }
        private static readonly Logger log = new LazyLogger("TemplateManager");

        //         public static ZoneDataFactory LoadFactory(string plugin)
        //         {
        //             Type type = ReflectionUtil.GetType(plugin);
        //             ZoneDataFactory factory = (ZoneDataFactory)ReflectionUtil.CreateInstance(type);
        //             return factory;
        //         }

        #endregion
        //----------------------------------------------------------------------------------------------
        public ZoneDataFactory DataFactory { get; }
        public static TemplateManager Instance { get; private set; }
        private EditorTemplates root;
        private Config mConfig;
        private ICommonConfig mExtConfig;
        private IGlobalConfig mGlobalConfig;
        private TerrainDefinitionMap mTerrainDefinition;
        private UnitActionDefinitionMap mUnitActionDefinition;
        private ResourcePropertiesMap mResourcePropertiesMap;
        private HashMap<UnitActionStatus, UnitActionDefinitionMap.UnitAction> mUnitActionMap = new HashMap<UnitActionStatus, UnitActionDefinitionMap.UnitAction>();
        //        private List<string> mResources = new List<string>();
        private CardAffectBindingTemplates mCardAffects;
        private int lastHashSceneId = 0;
        //----------------------------------------------------------------------------------------------
        public EditorTemplates DataRoot => root;
        public EditorDataCenter DataCenter { get => root.DataCenter; }
        internal protected TemplateManager(EditorTemplates root)
        {
            TemplateManager.Instance = this;
            this.root = root;
            this.DataFactory = root.DataFactory;
            mConfig = new Config();
            mExtConfig = DataFactory.CreateCommonCFG();
            mGlobalConfig = DataFactory.CreateGlobalCFG();
            mTerrainDefinition = new TerrainDefinitionMap();
            mUnitActionDefinition = new UnitActionDefinitionMap();
            mResourcePropertiesMap = new ResourcePropertiesMap();
            mCardAffects = new CardAffectBindingTemplates();
        }

        //----------------------------------------------------------------------------------------------
        #region Config

        public string ResourceVersion { get; internal set; }

        public Config DefaultConfig
        {
            get { return mConfig; }
            internal set { mConfig = value; }
        }
        public ICommonConfig DefaultExtConfig
        {
            get { return mExtConfig; }
            internal set { mExtConfig = value; }
        }
        public IGlobalConfig GlobalConfig
        {
            get { return mGlobalConfig; }
            internal set { mGlobalConfig = value; }
        }
        public ResourcePropertiesMap ResourcePropertiesMap
        {
            get { return mResourcePropertiesMap; }
            internal set { mResourcePropertiesMap = value; }
        }
        public CardAffectBindingTemplates CardAffects
        {
            get { return mCardAffects; }
            internal set { mCardAffects = value; }
        }
        public T ExtConfigAs<T>() where T : ICommonConfig
        {
            return (T)mExtConfig;
        }
        public T GlobalConfigAs<T>() where T : IGlobalConfig
        {
            return (T)mGlobalConfig;
        }
        public TerrainDefinitionMap DefaultTerrainDefinition
        {
            get { return mTerrainDefinition; }
            internal set { mTerrainDefinition = value; }
        }
        public UnitActionDefinitionMap DefaultUnitActionDefinition
        {
            get { return mUnitActionDefinition; }
            internal set
            {
                mUnitActionDefinition = value;
                mUnitActionMap.Clear();
                foreach (var a in value.ActionMap)
                {
                    mUnitActionMap.Put(a.Action, a);
                }
            }
        }
        public UnitActionDefinitionMap.UnitAction GetDefinedUnitAction(UnitActionStatus act)
        {
            UnitActionDefinitionMap.UnitAction ret;
            mUnitActionMap.TryGetValue(act, out ret);
            return ret;
        }

        #endregion
        //----------------------------------------------------------------------------------------------
        #region Resource Properties
        public virtual bool TryGetResourceProperties(string resID, out IResourceProperties prop)
        {
            prop = null;
            if (mResourcePropertiesMap?.PropertiesMap == null) return false;
            if (mResourcePropertiesMap.PropertiesMap.TryGetValue(resID, out var tuple))
            {
                prop = tuple;
                return true;
            }
            return false;
        }
        public bool TryGetResourceProperties<T>(string resID, out T prop) where T : IResourceProperties
        {
            if (TryGetResourceProperties(resID, out var _prop) && _prop is T)
            {
                prop = (T)_prop;
                return true;
            }
            prop = default;
            return false;
        }
        #endregion
        //----------------------------------------------------------------------------------------------
        #region Card Affects

        public bool TryGetCardUsage(int cardID, List<TemplateData> temps)
        {
            int count = 0;
            if (this.CardAffects.CardToTemplates.TryGetValue(cardID, out var tempMap))
            {
                foreach (var template in tempMap)
                {
                    foreach (var tempID in template.Value)
                    {
                        if (TryGetTemplate(template.Key, tempID, out var temp))
                        {
                            temps.Add(temp);
                            count++;
                        }
                    }
                }
            }
            return count > 0;
        }
        public bool TryGetCardUsage<T>(int cardID, List<T> temps) where T : TemplateData
        {
            int count = 0;
            var type = typeof(T);
            if (this.CardAffects.CardToTemplates.TryGetValue(cardID, out var tempMap))
            {
                foreach (var template in tempMap)
                {
                    if (type.IsAssignableFrom(template.Key))
                    {
                        foreach (var tempID in template.Value)
                        {
                            if (TryGetTemplate(template.Key, tempID, out var temp))
                            {
                                temps.Add(temp as T);
                                count++;
                            }
                        }
                    }
                }
            }
            return count > 0;
        }

        public bool TryGetUsageCards(TemplateData temp, List<CardTemplate> cards)
        {
            int count = 0;
            if (this.CardAffects.TemplatesToCard.TryGetValue(temp.GetType(), out var tempMap))
            {
                if (tempMap.TryGetValue(temp.ID, out var cardList))
                {
                    foreach (var cardID in cardList)
                    {
                        if (GetCard(cardID) is CardTemplate card)
                        {
                            cards.Add(card);
                            count++;
                        }
                    }
                }
            }
            return count > 0;
        }

        #endregion
        //----------------------------------------------------------------------------------------------

        public virtual void GetAllUnitsByPath(string path, List<UnitInfo> ret)
        {
            foreach (var t in mUnits.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllSkillsByPath(string path, List<SkillTemplate> ret)
        {
            foreach (var t in mSkills.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllSpellsByPath(string path, List<SpellTemplate> ret)
        {
            foreach (var t in mSpells.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllBuffsByPath(string path, List<BuffTemplate> ret)
        {
            foreach (var t in mBuffs.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllAurasByPath(string path, List<AuraTemplate> ret)
        {
            foreach (var t in mAuras.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllItemsByPath(string path, List<ItemTemplate> ret)
        {
            foreach (var t in mItems.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllUnitEventsByPath(string path, List<UnitEventTemplate> ret)
        {
            foreach (var t in mUnitEvents.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllCardsByPath(string path, List<CardTemplate> ret)
        {
            foreach (var t in mCards.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }
        public virtual void GetAllBattleUIByPath(string path, List<BattleUITemplate> ret)
        {
            foreach (var t in mBattleUIs.Values)
            {
                if (t.EditorPath.StartsWith(path)) { ret.Add(t); }
            }
        }


        //----------------------------------------------------------------------------------------------

        public virtual void GetAllUnits(List<UnitInfo> ret)
        {
            ret.AddRange(mUnits.Values);
        }
        public virtual void GetAllSkills(List<SkillTemplate> ret)
        {
            ret.AddRange(mSkills.Values);
        }
        public virtual void GetAllSpells(List<SpellTemplate> ret)
        {
            ret.AddRange(mSpells.Values);
        }
        public virtual void GetAllBuffs(List<BuffTemplate> ret)
        {
            ret.AddRange(mBuffs.Values);
        }
        public virtual void GetAllAuras(List<AuraTemplate> ret)
        {
            ret.AddRange(mAuras.Values);
        }
        public virtual void GetAllItems(List<ItemTemplate> ret)
        {
            ret.AddRange(mItems.Values);
        }
        public virtual void GetAllUnitEvents(List<UnitEventTemplate> ret)
        {
            ret.AddRange(mUnitEvents.Values);
        }
        public virtual void GetAllCards(List<CardTemplate> ret)
        {
            ret.AddRange(mCards.Values);
        }
        public virtual void GetAllBattleUI(List<BattleUITemplate> ret)
        {
            ret.AddRange(mBattleUIs.Values);
        }

        //----------------------------------------------------------------------------------------------

        protected HashMap<int, UnitInfo> mUnits = new HashMap<int, UnitInfo>();
        protected HashMap<int, SkillTemplate> mSkills = new HashMap<int, SkillTemplate>();
        protected HashMap<int, SpellTemplate> mSpells = new HashMap<int, SpellTemplate>();
        protected HashMap<int, BuffTemplate> mBuffs = new HashMap<int, BuffTemplate>();
        protected HashMap<int, AuraTemplate> mAuras = new HashMap<int, AuraTemplate>();
        protected HashMap<int, ItemTemplate> mItems = new HashMap<int, ItemTemplate>();
        protected HashMap<int, UnitEventTemplate> mUnitEvents = new HashMap<int, UnitEventTemplate>();
        protected HashMap<int, CardTemplate> mCards = new HashMap<int, CardTemplate>();
        protected HashMap<int, BattleUITemplate> mBattleUIs = new HashMap<int, BattleUITemplate>();
        protected List<TemplateData> mAll = new List<TemplateData>();

        internal protected virtual void ReloadMeta(EditorTemplatesMeta meta)
        {
            {
                mUnits.PutAll(meta.Units);
                mAll.AddRange(meta.Units.Values);
                foreach (var unit in meta.Units.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mSkills.PutAll(meta.Skills);
                mAll.AddRange(meta.Skills.Values);
                foreach (var unit in meta.Skills.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mSpells.PutAll(meta.Spells);
                mAll.AddRange(meta.Spells.Values);
                foreach (var unit in meta.Spells.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mBuffs.PutAll(meta.Buffs);
                mAll.AddRange(meta.Buffs.Values);
                foreach (var unit in meta.Buffs.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mAuras.PutAll(meta.Auras);
                mAll.AddRange(meta.Auras.Values);
                foreach (var unit in meta.Auras.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mItems.PutAll(meta.Items);
                mAll.AddRange(meta.Items.Values);
                foreach (var unit in meta.Items.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mUnitEvents.PutAll(meta.UnitEvents);
                mAll.AddRange(meta.UnitEvents.Values);
                foreach (var unit in meta.UnitEvents.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mCards.PutAll(meta.Cards);
                mAll.AddRange(meta.Cards.Values);
                foreach (var unit in meta.Cards.Values)
                {
                    RehashTemplate(unit);
                }
            }
            {
                mBattleUIs.PutAll(meta.BattleUIs);
                mAll.AddRange(meta.BattleUIs.Values);
                foreach (var unit in meta.BattleUIs.Values)
                {
                    RehashTemplate(unit);
                }
            }
        }
        internal protected virtual void AddTemplateData(TemplateData info)
        {
            mAll.Add(info);
            if (info is UnitInfo)
            {
                mUnits.Add(info.ID, info as UnitInfo);
            }
            else if (info is SkillTemplate)
            {
                mSkills.Add(info.ID, info as SkillTemplate);
            }
            else if (info is SpellTemplate)
            {
                mSpells.Add(info.ID, info as SpellTemplate);
            }
            else if (info is BuffTemplate)
            {
                mBuffs.Add(info.ID, info as BuffTemplate);
            }
            else if (info is AuraTemplate)
            {
                mAuras.Add(info.ID, info as AuraTemplate);
            }
            else if (info is ItemTemplate)
            {
                mItems.Add(info.ID, info as ItemTemplate);
            }
            else if (info is UnitEventTemplate)
            {
                mUnitEvents.Add(info.ID, info as UnitEventTemplate);
            }
            else if (info is CardTemplate)
            {
                mCards.Add(info.ID, info as CardTemplate);
            }
            else if (info is BattleUITemplate)
            {
                mBattleUIs.Add(info.ID, info as BattleUITemplate);
            }
        }
        public virtual void Flush(bool force = false)
        {
            if (force || root.IsClientData)
            {
                mAll.Clear();
                mUnits.Clear();
                mSkills.Clear();
                mSpells.Clear();
                mBuffs.Clear();
                mAuras.Clear();
                mItems.Clear();
                mUnitEvents.Clear();
                mCards.Clear();
                mBattleUIs.Clear();
                CleanSceneSNData();
                lastHashSceneId = 0;
                mSNTemplatesDataMap.Clear();
            }
        }

        /// <summary>
        /// 判断当前模板是否加载
        /// </summary>
        /// <param name="type"></param>
        /// <param name="templateID"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public virtual bool IsTemplateLoaded(Type type, int templateID, out TemplateData template)
        {
            if (typeof(UnitInfo).IsAssignableFrom(type))
            {
                template = mUnits.Get(templateID);
            }
            else if (typeof(SkillTemplate).IsAssignableFrom(type))
            {
                template = mSkills.Get(templateID);
            }
            else if (typeof(SpellTemplate).IsAssignableFrom(type))
            {
                template = mSpells.Get(templateID);
            }
            else if (typeof(BuffTemplate).IsAssignableFrom(type))
            {
                template = mBuffs.Get(templateID);
            }
            else if (typeof(AuraTemplate).IsAssignableFrom(type))
            {
                template = mAuras.Get(templateID);
            }
            else if (typeof(ItemTemplate).IsAssignableFrom(type))
            {
                template = mItems.Get(templateID);
            }
            else if (typeof(UnitEventTemplate).IsAssignableFrom(type))
            {
                template = mUnitEvents.Get(templateID);
            }
            else if (typeof(CardTemplate).IsAssignableFrom(type))
            {
                template = mCards.Get(templateID);
            }
            else if (typeof(BattleUITemplate).IsAssignableFrom(type))
            {
                template = mBattleUIs.Get(templateID);
            }
            else
            {
                template = null;
            }
            return template != null;
        }

        //----------------------------------------------------------------------------------------------
        public virtual int TemplatesCount { get => mAll.Count; }
        public virtual IReadOnlyCollection<UnitInfo> AllUnits { get { return mUnits.Values; } }
        public virtual IReadOnlyCollection<SkillTemplate> AllSkills { get { return mSkills.Values; } }
        public virtual IReadOnlyCollection<SpellTemplate> AllSpells { get { return mSpells.Values; } }
        public virtual IReadOnlyCollection<BuffTemplate> AllBuffs { get { return mBuffs.Values; } }
        public virtual IReadOnlyCollection<AuraTemplate> AllAuras { get { return mAuras.Values; } }
        public virtual IReadOnlyCollection<ItemTemplate> AllItems { get { return mItems.Values; } }
        public virtual IReadOnlyCollection<UnitEventTemplate> AllUnitEvents { get { return mUnitEvents.Values; } }
        public virtual IReadOnlyCollection<CardTemplate> AllCards { get { return mCards.Values; } }
        public virtual IReadOnlyCollection<BattleUITemplate> AllBattleUI { get { return mBattleUIs.Values; } }
        public virtual IReadOnlyCollection<TemplateData> AllTemplates
        {
            get { return mAll; }
        }

        public void Put(params object[] datas)
        {
            foreach (var data in datas)
            {
                if (data is PreviewUpdate preview)
                {
                    if (preview.CleanUp)
                    {
                        Flush();
                    }
                    Put(
                        preview.GameConfig,
                        preview.UnitActionMap,
                        preview.ResourcePropertiesMap,
                        preview.Templates,
                        preview.Relation,
                        preview.Template);
                }
                else if (data is Config cfg) DefaultConfig = cfg;
                else if (data is ICommonConfig extcfg) DefaultExtConfig = extcfg;
                else if (data is IGlobalConfig gcfg) GlobalConfig = gcfg;
                else if (data is TerrainDefinitionMap td) DefaultTerrainDefinition = td;
                else if (data is UnitActionDefinitionMap ud) DefaultUnitActionDefinition = ud;
                else if (data is ResourcePropertiesMap res) ResourcePropertiesMap = res;
                else if (data is UnitInfo unit) mUnits.Put(unit.ID, unit);
                else if (data is SkillTemplate skill) mSkills.Put(skill.ID, skill);
                else if (data is SpellTemplate spell) mSpells.Put(spell.ID, spell);
                else if (data is BuffTemplate buff) mBuffs.Put(buff.ID, buff);
                else if (data is AuraTemplate aura) mAuras.Put(aura.ID, aura);
                else if (data is ItemTemplate item) mItems.Put(item.ID, item);
                else if (data is UnitEventTemplate uevent) mUnitEvents.Put(uevent.ID, uevent);
                else if (data is CardTemplate card) mCards.Put(card.ID, card);
                else if (data is BattleUITemplate gui) mBattleUIs.Put(gui.ID, gui);
                //else if (data is SceneData scene) Scenes.Put(scene.ID, scene);
                else if (data is IEnumerable list)
                {
                    foreach (var e in list)
                    {
                        Put(e);
                    }
                }
            }
        }

        public bool TryGetTemplate(Type templateType, int templateID, out TemplateData temp)
        {
            if (templateType == typeof(UnitInfo))
            {
                temp = GetUnit(templateID);
                return temp != null;
            }
            else if (templateType == typeof(SkillTemplate))
            {
                temp = GetSkill(templateID);
                return temp != null;
            }
            else if (templateType == typeof(SpellTemplate))
            {
                temp = GetSpell(templateID);
                return temp != null;
            }
            else if (templateType == typeof(BuffTemplate))
            {
                temp = GetBuff(templateID);
                return temp != null;
            }
            else if (templateType == typeof(AuraTemplate))
            {
                temp = GetAura(templateID);
                return temp != null;
            }
            else if (templateType == typeof(ItemTemplate))
            {
                temp = GetItem(templateID);
                return temp != null;
            }
            else if (templateType == typeof(UnitEventTemplate))
            {
                temp = GetUnitEvent(templateID);
                return temp != null;
            }
            else if (templateType == typeof(CardTemplate))
            {
                temp = GetCard(templateID);
                return temp != null;
            }
            else if (templateType == typeof(BattleUITemplate))
            {
                temp = GetBattleUI(templateID);
                return temp != null;
            }
            temp = null;
            return false;
        }
        public bool TryGetTemplate<T>(int templateID, out T temp) where T : TemplateData
        {
            if (this.TryGetTemplate(typeof(T), templateID, out var _temp))
            {
                temp = _temp as T;
                return true;
            }
            temp = null;
            return false;
        }
        //----------------------------------------------------------------------------------------------
        public bool TryGetUnit(int id, out UnitInfo temp)
        {
            temp = GetUnit(id);
            return temp != null;
        }
        public bool TryGetSkill(int id, out SkillTemplate temp)
        {
            temp = GetSkill(id);
            return temp != null;
        }
        public bool TryGetSpell(int id, out SpellTemplate temp)
        {
            temp = GetSpell(id);
            return temp != null;
        }
        public bool TryGetBuff(int id, out BuffTemplate temp)
        {
            temp = GetBuff(id);
            return temp != null;
        }
        public bool TryGetAura(int id, out AuraTemplate temp)
        {
            temp = GetAura(id);
            return temp != null;
        }
        public bool TryGetItem(int id, out ItemTemplate temp)
        {
            temp = GetItem(id);
            return temp != null;
        }
        public bool TryGetUnitEvent(int id, out UnitEventTemplate temp)
        {
            temp = GetUnitEvent(id);
            return temp != null;
        }
        public bool TryGetCard(int id, out CardTemplate temp)
        {
            temp = GetCard(id);
            return temp != null;
        }
        public bool TryGetBattleUI(int id, out BattleUITemplate temp)
        {
            temp = GetBattleUI(id);
            return temp != null;
        }
        //----------------------------------------------------------------------------------------------
        public virtual UnitInfo GetUnit(int id)
        {
            if (mUnits.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<UnitInfo>(id);
            }
            return null;
        }
        public virtual SkillTemplate GetSkill(int id)
        {
            if (mSkills.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<SkillTemplate>(id);
            }
            return null;
        }
        public virtual SpellTemplate GetSpell(int id)
        {
            if (mSpells.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<SpellTemplate>(id);
            }
            return null;
        }
        public virtual BuffTemplate GetBuff(int id)
        {
            if (mBuffs.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<BuffTemplate>(id);
            }
            return null;
        }
        public virtual AuraTemplate GetAura(int id)
        {
            if (mAuras.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<AuraTemplate>(id);
            }
            return null;
        }
        public virtual ItemTemplate GetItem(int id)
        {
            if (mItems.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<ItemTemplate>(id);
            }
            return null;
        }
        public virtual UnitEventTemplate GetUnitEvent(int id)
        {
            if (mUnitEvents.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<UnitEventTemplate>(id);
            }
            return null;
        }
        public virtual CardTemplate GetCard(int id)
        {
            if (mCards.TryGetValue(id, out var st))
            {
                return st;
            }
            else if (root.IsClientData)
            {
                return root.LoadTemplate<CardTemplate>(id);
            }
            return null;
        }
        public virtual BattleUITemplate GetBattleUI(int id)
        {
            if (mBattleUIs.TryGetValue(id, out var st))
            {
                return st;
            }
            else //if (root.IsClientData)
            {
                return root.LoadTemplate<BattleUITemplate>(id);
            }
            // return null;
        }



        //----------------------------------------------------------------------------------------------
        // 根据场景DefaultUnitLevel配置每个单独单位等级
        public SceneData SyncSceneLevel(SceneData scene)
        {
            if (scene.DefaultUnitLevel > 0)
            {
                var attlevels = PropertyUtil.CollectFieldAttributeValues<TemplateLevelAttribute, int>(scene);
                foreach (var fv in attlevels)
                {
                    int f_value = fv.FieldValue;
                    if (f_value == 0)
                    {
                        log.Info(string.Format("配平单位等级: {0} {1} {2}", fv.FieldOwner, fv.Field.Name, scene.DefaultUnitLevel));
                        fv.Field.SetValue(fv.FieldOwner, scene.DefaultUnitLevel);
                    }
                }
            }
            return scene;
        }
        //----------------------------------------------------------------------------------------------
        #region SN_Data

        private HashMap<Type, HashMap<uint, ISNData>> mSNTemplatesDataMap = new HashMap<Type, HashMap<uint, ISNData>>();
        private HashMap<uint, ISNData> mSNSceneDataMap = new HashMap<uint, ISNData>();

        public T GetSnData<T>(uint sn) where T : ISNData
        {
            var type = typeof(T);
            var map = mSNTemplatesDataMap.Get(type);
            if (map != null)
            {
                T ret = map.Get(sn) as T;
                if (ret != null)
                {
                    return ret;
                }
            }
            if (mSNSceneDataMap.TryGetValue(sn, out var reg))
            {
                return reg as T;
            }
            return default(T);
        }

        protected internal int RehashTemplate(object data)
        {
            int count = 0;
            lock (mSNTemplatesDataMap)
            {
                var datas = new List<ISNData>();
                {
                    PropertyUtil.CollectFieldTypeValues(data, datas);
                    foreach (ISNData sn in datas)
                    {
                        var type = sn.GetType();
                        var map = mSNTemplatesDataMap.Get(type);
                        if (map == null)
                        {
                            map = new HashMap<uint, ISNData>();
                            mSNTemplatesDataMap.Add(type, map);
                        }
                        if (!map.ContainsKey(sn.SerialNumber))
                        {
                            map.Add(sn.SerialNumber, sn);
                            count++;
                        }
                        //                         else
                        //                         {
                        //                             throw new Exception($"SerialNumber {sn.SerialNumber} already in use ! {sn} @ {data}");
                        //                         }
                    }
                }
            }
            return count;
        }
        protected internal int RehashScene(SceneData scene)
        {
            if (lastHashSceneId != scene.ID) //保留上一次的SCENE数据，防止反复HASH同一个
            {
                CleanSceneSNData();
                lastHashSceneId = 0;
            }
            else //与上一次一致直接返回上一次的结果
            {
                return mSNSceneDataMap.Count;
            }

            var count = 0;
            var datas = new List<ISNData>();
            {
                PropertyUtil.CollectFieldTypeValues(scene, datas);
                foreach (var sn in datas)
                {
                    if (!mSNSceneDataMap.ContainsKey(sn.SerialNumber))
                    {
                        mSNSceneDataMap.Add(sn.SerialNumber, sn);
                        count++;
                    }
                    //                     else
                    //                     {
                    //                         throw new Exception($"SerialNumber {sn.SerialNumber} already in use ! {sn} in {scene}");
                    //                     }
                }
            }

            lastHashSceneId = scene.ID;

            return count;
        }

        protected internal void CleanSceneSNData()
        {
            mSNSceneDataMap?.Clear();
        }

        //         protected internal int RehashAllScene(ICollection<SceneData> scenes)
        //         {
        //             var count = 0;
        //             foreach (var s in scenes)
        //             {
        //                 count += RehashScene(s);
        //             }
        //             return count;
        //         }

        #endregion
        //----------------------------------------------------------------------------------------------
    }

    //----------------------------------------------------------------------------------------------
    public static class TemplatesEXT
    {
        public static void PutAllToMap<T>(this HashMap<int, T> map, IEnumerable<T> list) where T : TemplateData
        {
            foreach (T u in list)
            {
                map[u.ID] = u;
            }
        }
    }
    //--------------------------------------------------------------------------------------------------

}
