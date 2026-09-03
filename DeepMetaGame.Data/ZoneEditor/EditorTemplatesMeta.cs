using DeepCore;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.ZoneEditor
{
    [MessageType(BattleConstants.EditorTemplatesMeta)]
    public class EditorTemplatesMeta : IExternalizable
    {
        public Config DefaultConfig;
        public ICommonConfig DefaultExtCFG;
        public IGlobalConfig GlobalCFG;
        public string ResourceVersion;
        public TerrainDefinitionMap DefaultTerrainDefinitions;
        public UnitActionDefinitionMap DefaultUnitActionDefinitions;
        public ResourcePropertiesMap ResourcePropertiesMap;
        public CardAffectBindingTemplates CardAffects;
        public HashMap<int, UnitInfo> Units = new HashMap<int, UnitInfo>();
        public HashMap<int, SkillTemplate> Skills = new HashMap<int, SkillTemplate>();
        public HashMap<int, SpellTemplate> Spells = new HashMap<int, SpellTemplate>();
        public HashMap<int, BuffTemplate> Buffs = new HashMap<int, BuffTemplate>();
        public HashMap<int, AuraTemplate> Auras = new HashMap<int, AuraTemplate>();
        public HashMap<int, ItemTemplate> Items = new HashMap<int, ItemTemplate>();
        public HashMap<int, UnitEventTemplate> UnitEvents = new HashMap<int, UnitEventTemplate>();
        public HashMap<int, CardTemplate> Cards = new HashMap<int, CardTemplate>();
        public HashMap<int, BattleUITemplate> BattleUIs = new HashMap<int, BattleUITemplate>();
        //public HashMap<int, SceneData> Scenes = new HashMap<int, SceneData>();
        public DataCenterMeta BattleDataCenter;
        public void ForEachTemplatesData<ST>(ST st, Action<ST, TemplateData> action)
        {
            foreach (var e in this.Units.Values) { action(st, e); }
            foreach (var e in this.Skills.Values) { action(st, e); }
            foreach (var e in this.Spells.Values) { action(st, e); }
            foreach (var e in this.Buffs.Values) { action(st, e); }
            foreach (var e in this.Auras.Values) { action(st, e); }
            foreach (var e in this.Items.Values) { action(st, e); }
            foreach (var e in this.UnitEvents.Values) { action(st, e); }
            foreach (var e in this.Cards.Values) { action(st, e); }
            foreach (var e in this.BattleUIs.Values) { action(st, e); }
        }
        public void ReadExternal(IInputStream input)
        {
            this.DefaultConfig = input.GetObjAs<Config>();
            this.DefaultExtCFG = input.GetObjAs<ICommonConfig>();
            this.GlobalCFG = input.GetObjAs<IGlobalConfig>();
            this.ResourceVersion = input.GetUTF();
            this.DefaultTerrainDefinitions = input.GetObjAs<TerrainDefinitionMap>();
            this.DefaultUnitActionDefinitions = input.GetObjAs<UnitActionDefinitionMap>();
            this.ResourcePropertiesMap = input.GetObjAs<ResourcePropertiesMap>();
            this.CardAffects = input.GetObjAs<CardAffectBindingTemplates>();
            this.Units = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<UnitInfo>(), this.Units);
            this.Skills = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<SkillTemplate>(), this.Skills);
            this.Spells = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<SpellTemplate>(), this.Spells);
            this.Buffs = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<BuffTemplate>(), this.Buffs);
            this.Auras = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<AuraTemplate>(), this.Auras);
            this.Items = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<ItemTemplate>(), this.Items);
            this.UnitEvents = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<UnitEventTemplate>(), this.UnitEvents);
            this.Cards = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<CardTemplate>(), this.Cards);
            this.BattleUIs = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<BattleUITemplate>(), this.BattleUIs);
            //this.Scenes = input.GetMap(static input => input.GetS32(), static input => input.GetObjAs<SceneData>(), this.Scenes);
            this.BattleDataCenter = input.GetObjAs<DataCenterMeta>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutObj(DefaultConfig);
            output.PutObj(DefaultExtCFG);
            output.PutObj(GlobalCFG);
            output.PutUTF(ResourceVersion);
            output.PutObj(DefaultTerrainDefinitions);
            output.PutObj(DefaultUnitActionDefinitions);
            output.PutObj(ResourcePropertiesMap);
            output.PutObj(CardAffects);
            output.PutMap(this.Units, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Skills, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Spells, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Buffs, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Auras, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Items, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.UnitEvents, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.Cards, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutMap(this.BattleUIs, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            //output.PutMap(this.Scenes, static (o, v) => o.PutS32(v), static (o, v) => o.PutObj(v));
            output.PutObj(BattleDataCenter);
        }
    }


}
