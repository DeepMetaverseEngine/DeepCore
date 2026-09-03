using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepMetaGame.Data;
using DeepMetaGame.Data.GUI;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.EventTrigger
{


    //---------------------------------------------------------------------------------------------------------------
    public class ZoneValueTypeNameSpace : DeepCore.EventTrigger.ValueTypeNameSpace
    {
        public static ZoneValueTypeNameSpace ZoneInstance { get; private set; }
        public ZoneValueTypeNameSpace()
        {
            ZoneInstance = this;

            RegistValueType(typeof(InstanceUnit),           /**/(t) => new UnitValue.NA(),                /**/"单位", Colors.ARGB.DarkMagena);
            RegistValueType(typeof(InstanceUnit.EquipSkill),/**/(t) => new UnitEquipSkillValue.NA(),      /**/"单位技能", Colors.ARGB.DarkOrchid);
            RegistValueType(typeof(InstanceUnit.EquipBuff), /**/(t) => new UnitEquipBuffValue.NA(),       /**/"单位BUFF", Colors.ARGB.DarkOrchid);
            RegistValueType(typeof(InstanceUnit.EquipAura), /**/(t) => new UnitEquipAuraValue.NA(),       /**/"单位光环", Colors.ARGB.DarkOrchid);
            RegistValueType(typeof(InstanceItem),           /**/(t) => new ItemValue.NA(),                /**/"物品", Colors.ARGB.Peru);
            RegistValueType(typeof(InstanceFlag),           /**/(t) => new FlagValue.NA(),                /**/"区域", Colors.ARGB.Chocolate);
            RegistValueType(typeof(Vector3?),               /**/(t) => new PositionValue.VALUE(),         /**/"坐标", Colors.ARGB.Crimson);
            RegistValueType(typeof(HostGUIComponent),       /**/(t) => new GUIValue.BindingForm(),        /**/"GUI", Colors.ARGB.CornflowerBlue);
                                                            
            RegistValueType(typeof(UnitInfo),               /**/(t) => new UnitTemplateValue.Template(),  /**/"模板-单位", Colors.ARGB.Olive);
            RegistValueType(typeof(ItemTemplate),           /**/(t) => new ItemTemplateValue.Template(),  /**/"模板-物品", Colors.ARGB.Olive);
            RegistValueType(typeof(BuffTemplate),           /**/(t) => new BuffTemplateValue.Template(),  /**/"模板-Buff", Colors.ARGB.Olive);
            RegistValueType(typeof(AuraTemplate),           /**/(t) => new AuraTemplateValue.Template(),  /**/"模板-Aura", Colors.ARGB.Olive);
            RegistValueType(typeof(SpellTemplate),          /**/(t) => new SpellTemplateValue.Template(), /**/"模板-Spell", Colors.ARGB.Olive);
            RegistValueType(typeof(SkillTemplate),          /**/(t) => new SkillTemplateValue.Template(), /**/"模板-Skill", Colors.ARGB.Olive);
            RegistValueType(typeof(CardTemplate),           /**/(t) => new CardTemplateValue.Template(),  /**/"模板-Card", Colors.ARGB.Olive);
            RegistValueType(typeof(BattleUITemplate),       /**/(t) => new CardTemplateValue.Template(),  /**/"模板-GUI", Colors.ARGB.Olive);

            RegistFields(false,
                typeof(Config),
                typeof(UnitInfo),
                typeof(ItemTemplate),
                typeof(SkillTemplate),
                typeof(SpellTemplate),
                typeof(BuffTemplate),
                typeof(AuraTemplate),
                typeof(CardTemplate),
                typeof(BattleUITemplate)
                ); 
            RegistFields(false, ZoneDataFactory.Factory.PropertiesType);

            RegistFields(true,
                  typeof(UEComponentMeta),
                  typeof(EditorScene),
                  typeof(InstanceUnit),
                  typeof(InstanceUnit.EquipSkill),
                  typeof(InstanceUnit.EquipBuff),
                  typeof(InstanceUnit.EquipAura),
                  typeof(InstanceItem),
                  typeof(HostGUIComponent),
                  typeof(UnitComponent));
        }

    }
}
