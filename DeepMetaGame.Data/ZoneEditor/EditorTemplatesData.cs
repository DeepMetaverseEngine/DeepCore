using DeepCore;
using DeepCore.IO;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneEditor
{
    public class EditorTemplatesData
    {
        public Config DefaultConfig { get; private set; }
        public ICommonConfig DefaultExtCFG { get; private set; }
        public IGlobalConfig GlobalCFG { get; private set; }
        public TerrainDefinitionMap DefaultTerrainDefinitions { get; private set; }
        public UnitActionDefinitionMap DefaultUnitActionDefinitions { get; private set; }
        public ResourcePropertiesMap ResourcePropertiesMap { get; private set; }

        readonly public HashMap<int, UnitInfo> Units = new HashMap<int, UnitInfo>();
        readonly public HashMap<int, SkillTemplate> Skills = new HashMap<int, SkillTemplate>();
        readonly public HashMap<int, SpellTemplate> Spells = new HashMap<int, SpellTemplate>();
        readonly public HashMap<int, BuffTemplate> Buffs = new HashMap<int, BuffTemplate>();
        readonly public HashMap<int, AuraTemplate> Auras = new HashMap<int, AuraTemplate>();
        readonly public HashMap<int, ItemTemplate> Items = new HashMap<int, ItemTemplate>();
        readonly public HashMap<int, UnitEventTemplate> UnitEvents = new HashMap<int, UnitEventTemplate>();
        readonly public HashMap<int, CardTemplate> Cards = new HashMap<int, CardTemplate>();
        readonly public HashMap<int, BattleUITemplate> BattleUIs = new HashMap<int, BattleUITemplate>();
        readonly public HashMap<int, SceneData> Scenes = new HashMap<int, SceneData>();

        public EditorTemplatesData()
        {
            this.DefaultConfig = new Config();
            this.DefaultExtCFG = ZoneDataFactory.Factory.CreateCommonCFG();
            this.GlobalCFG = ZoneDataFactory.Factory.CreateGlobalCFG();
            this.ResourcePropertiesMap = new ResourcePropertiesMap();
        }
        public EditorTemplatesData(TemplateManager templates, List<SceneData> scenes)
        {
            DefaultConfig = templates.DefaultConfig;
            DefaultExtCFG = templates.DefaultExtConfig;
            GlobalCFG = templates.GlobalConfig;
            DefaultTerrainDefinitions = templates.DefaultTerrainDefinition;
            DefaultUnitActionDefinitions = templates.DefaultUnitActionDefinition;
            ResourcePropertiesMap = templates.ResourcePropertiesMap;
            this.PutAll(Units, templates.AllUnits);
            this.PutAll(Skills, templates.AllSkills);
            this.PutAll(Spells, templates.AllSpells);
            this.PutAll(Buffs, templates.AllBuffs);
            this.PutAll(Auras, templates.AllAuras);
            this.PutAll(Items, templates.AllItems);
            this.PutAll(UnitEvents, templates.AllUnitEvents);
            this.PutAll(Cards, templates.AllCards);
            this.PutAll(BattleUIs, templates.AllBattleUI);
            this.PutAll(Scenes, scenes);
        }
        public EditorTemplatesData(
            Config cfg,
            ICommonConfig extCfg,
            IGlobalConfig globalConfig,
            TerrainDefinitionMap mapDefines,
            UnitActionDefinitionMap unitActionDefines,
            ResourcePropertiesMap resMap,
            IEnumerable<UnitInfo> units,
            IEnumerable<SkillTemplate> skills,
            IEnumerable<SpellTemplate> spells,
            IEnumerable<BuffTemplate> buffs,
            IEnumerable<AuraTemplate> auras,
            IEnumerable<ItemTemplate> items,
            IEnumerable<UnitEventTemplate> events,
            IEnumerable<CardTemplate> cards,
            IEnumerable<BattleUITemplate> guis,
            IEnumerable<SceneData> scenes)
        {
            DefaultConfig = cfg;
            DefaultExtCFG = extCfg;
            GlobalCFG = globalConfig;
            DefaultTerrainDefinitions = mapDefines;
            DefaultUnitActionDefinitions = unitActionDefines;
            ResourcePropertiesMap = resMap;
            this.PutAll(Units, units);
            this.PutAll(Skills, skills);
            this.PutAll(Spells, spells);
            this.PutAll(Buffs, buffs);
            this.PutAll(Auras, auras);
            this.PutAll(Items, items);
            this.PutAll(UnitEvents, events);
            this.PutAll(Cards, cards);
            this.PutAll(BattleUIs, guis);
            this.PutAll(Scenes, scenes);
        }

        public int FileCount
        {
            get
            {
                return
                Units.Count +
                Skills.Count +
                Spells.Count +
                Buffs.Count +
                Auras.Count +
                Items.Count +
                UnitEvents.Count +
                Cards.Count +
                BattleUIs.Count +
                Scenes.Count;
            }
        }

        public void Put(params object[] datas)
        {
            foreach (var data in datas)
            {
                if (data is Config cfg) DefaultConfig = cfg;
                else if (data is ICommonConfig extcfg) DefaultExtCFG = extcfg;
                else if (data is IGlobalConfig gcfg) GlobalCFG = gcfg;
                else if (data is TerrainDefinitionMap td) DefaultTerrainDefinitions = td;
                else if (data is UnitActionDefinitionMap ud) DefaultUnitActionDefinitions = ud;
                else if (data is ResourcePropertiesMap res) ResourcePropertiesMap = res;
                else if (data is SceneData scene) Scenes.Put(scene.ID, scene);
                else if (data is UnitInfo unit) Units.Put(unit.ID, unit);
                else if (data is SkillTemplate skill) Skills.Put(skill.ID, skill);
                else if (data is SpellTemplate spell) Spells.Put(spell.ID, spell);
                else if (data is BuffTemplate buff) Buffs.Put(buff.ID, buff);
                else if (data is AuraTemplate aura) Auras.Put(aura.ID, aura);
                else if (data is ItemTemplate item) Items.Put(item.ID, item);
                else if (data is UnitEventTemplate uevent) UnitEvents.Put(uevent.ID, uevent);
                else if (data is CardTemplate card) Cards.Put(card.ID, card);
                else if (data is BattleUITemplate gui) BattleUIs.Put(gui.ID, gui);
            }
        }
        public void PutRange(IEnumerable datas)
        {
            foreach (var data in datas)
            {
                Put(data);
            }
        }
        public void CleanUp()
        {
            Units.Clear();
            Skills.Clear();
            Spells.Clear();
            Buffs.Clear();
            Auras.Clear();
            Items.Clear();
            UnitEvents.Clear();
            Cards.Clear();
            BattleUIs.Clear();
            Scenes.Clear();
        }

        private void PutAll<T>(HashMap<int, T> map, IEnumerable<T> list) where T : TemplateData
        {
            foreach (T u in list)
            {
                map[u.ID] = u;
            }
        }
        public List<TemplateData> AllTemplates(bool sort = true)
        {
            var list = new List<TemplateData>();
            list.AddRange(Units.Values);
            list.AddRange(Skills.Values);
            list.AddRange(Spells.Values);
            list.AddRange(Buffs.Values);
            list.AddRange(Auras.Values);
            list.AddRange(Items.Values);
            list.AddRange(UnitEvents.Values);
            list.AddRange(Cards.Values);
            list.AddRange(BattleUIs.Values);
            list.AddRange(Scenes.Values);
            if (sort)
            {
                list.Sort(new TemplateDataComparer());
            }
            return list;
        }
        public bool TryGetTemplateData<T>(int templateID, out T data) where T : TemplateData
        {
            if (TryGetTemplateData(typeof(T), templateID, out var _data))
            {
                data = (T)_data;
                return true;
            }
            data = default;
            return false;
        }
        public bool TryGetTemplateData(Type type, int templateID, out TemplateData data)
        {
            data = null;
            if (type.IsAssignableFrom(typeof(UnitInfo)))
            {
                data = Units[templateID];
            }
            else if (type.IsAssignableFrom(typeof(SkillTemplate)))
            {
                data = Skills[templateID];
            }
            else if (type.IsAssignableFrom(typeof(SpellTemplate)))
            {
                data = Spells[templateID];
            }
            else if (type.IsAssignableFrom(typeof(BuffTemplate)))
            {
                data = Buffs[templateID];
            }
            else if (type.IsAssignableFrom(typeof(AuraTemplate)))
            {
                data = Auras[templateID];
            }
            else if (type.IsAssignableFrom(typeof(ItemTemplate)))
            {
                data = Items[templateID];
            }
            else if (type.IsAssignableFrom(typeof(UnitEventTemplate)))
            {
                data = UnitEvents[templateID];
            }
            else if (type.IsAssignableFrom(typeof(CardTemplate)))
            {
                data = Cards[templateID];
            }
            else if (type.IsAssignableFrom(typeof(BattleUITemplate)))
            {
                data = BattleUIs[templateID];
            }
            else if (type.IsAssignableFrom(typeof(SceneData)))
            {
                data = Scenes[templateID];
            }
            return data != null;
        }
        public List<TemplateData> GetTemplateDatas(Type type)
        {
            if (type.IsAssignableFrom(typeof(UnitInfo)))
            {
                return new(Units.Values);
            }
            else if (type.IsAssignableFrom(typeof(SkillTemplate)))
            {
                return new(Skills.Values);
            }
            else if (type.IsAssignableFrom(typeof(SpellTemplate)))
            {
                return new(Spells.Values);
            }
            else if (type.IsAssignableFrom(typeof(BuffTemplate)))
            {
                return new(Buffs.Values);
            }
            else if (type.IsAssignableFrom(typeof(AuraTemplate)))
            {
                return new(Auras.Values);
            }
            else if (type.IsAssignableFrom(typeof(ItemTemplate)))
            {
                return new(Items.Values);
            }
            else if (type.IsAssignableFrom(typeof(UnitEventTemplate)))
            {
                return new(UnitEvents.Values);
            }
            else if (type.IsAssignableFrom(typeof(CardTemplate)))
            {
                return new(Cards.Values);
            }
            else if (type.IsAssignableFrom(typeof(BattleUITemplate)))
            {
                return new(BattleUIs.Values);
            }
            else if (type.IsAssignableFrom(typeof(SceneData)))
            {
                return new(Scenes.Values);
            }
            return null;
        }
        public struct TemplateDataComparer : IComparer<TemplateData>
        {
            public int Compare(TemplateData ix, TemplateData iy)
            {
                if (ix == null)
                {
                    return 1;
                }
                if (iy == null)
                {
                    return -1;
                }
                var tA = ix.GetType();
                var tB = iy.GetType();
                if (tA == tB)
                {
                    return ix.ID - iy.ID;
                }
                else
                {
                    if (tA == typeof(UnitInfo)) return -1;
                    if (tB == typeof(UnitInfo)) return 1;

                    if (tA == typeof(SkillTemplate)) return -1;
                    if (tB == typeof(SkillTemplate)) return 1;

                    if (tA == typeof(SpellTemplate)) return -1;
                    if (tB == typeof(SpellTemplate)) return 1;

                    if (tA == typeof(BuffTemplate)) return -1;
                    if (tB == typeof(BuffTemplate)) return 1;

                    if (tA == typeof(AuraTemplate)) return -1;
                    if (tB == typeof(AuraTemplate)) return 1;

                    if (tA == typeof(ItemTemplate)) return -1;
                    if (tB == typeof(ItemTemplate)) return 1;

                    if (tA == typeof(UnitEventTemplate)) return -1;
                    if (tB == typeof(UnitEventTemplate)) return 1;

                    if (tA == typeof(CardTemplate)) return -1;
                    if (tB == typeof(CardTemplate)) return 1;

                    if (tA == typeof(BattleUITemplate)) return -1;
                    if (tB == typeof(BattleUITemplate)) return 1;

                    if (tA == typeof(SceneData)) return -1;
                    if (tB == typeof(SceneData)) return 1;
                }
                return tA.Name.CompareTo(tB.Name);
            }
        }
    }

}
