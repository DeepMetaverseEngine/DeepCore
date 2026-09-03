using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    //-----------------------------------------------------------------------------------------


    #region __配置扩展__

    [Desc("配置扩展字段(string)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }

    [Desc("配置扩展字段(int)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }

    [Desc("配置扩展字段(float)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }
    [Desc("配置扩展字段(long)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }
    [Desc("配置扩展字段(double)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }

    [Desc("配置扩展字段(bool)", "[游戏]/配置模板/扩展字段")]
    public class ExtConfigFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(ICommonConfig), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置扩展", FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>(api.ZoneAPI.Templates.DefaultExtConfig, FieldName);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __场景扩展__

    [Desc("场景扩展字段(string)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }

    [Desc("场景扩展字段(int)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }

    [Desc("场景扩展字段(float)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }


    [Desc("场景扩展字段(long)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }

    [Desc("场景扩展字段(double)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }

    [Desc("场景扩展字段(bool)", "[游戏]/场景模板/扩展字段")]
    public class ZonePropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(ISceneProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景扩展", FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>((api.ZoneAPI).Data.Properties, FieldName);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __单位扩展__

    [Desc("单位扩展字段(string)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.TemplateData.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("单位扩展字段(int)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.TemplateData.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位扩展字段(float)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.TemplateData.Properties, FieldName);
            }
            return 0;
        }
    }


    [Desc("单位扩展字段(long)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.TemplateData.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位扩展字段(double)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.TemplateData.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位扩展字段(bool)", "[游戏]/单位模板/扩展字段")]
    public class UnitPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(IUnitProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Unit, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.TemplateData.Properties, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __物品扩展__

    [Desc("物品扩展字段(string)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("物品扩展字段(int)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品扩展字段(float)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.Properties, FieldName);
            }
            return 0;
        }
    }


    [Desc("物品扩展字段(long)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品扩展字段(double)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品扩展字段(bool)", "[游戏]/物品模板/扩展字段")]
    public class ItemPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(IItemProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Item, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.Properties, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __技能扩展__

    [Desc("技能扩展字段(string)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("技能扩展字段(int)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能扩展字段(float)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能扩展字段(long)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能扩展字段(double)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能扩展字段(bool)", "[游戏]/技能模板/扩展字段")]
    public class SkillPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(ISkillProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Skill, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.Properties, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __法术扩展__

    [Desc("法术扩展字段(string)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("法术扩展字段(int)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术扩展字段(float)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.Properties, FieldName);
            }
            return 0;
        }
    }


    [Desc("法术扩展字段(long)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术扩展字段(double)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术扩展字段(bool)", "[游戏]/法术模板/扩展字段")]
    public class SpellPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(ISpellProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Spell, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.Properties, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

    #region __BUFF扩展__

    [Desc("BUFF扩展字段(string)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("BUFF扩展字段(int)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF扩展字段(float)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.Properties, FieldName);
            }
            return 0;
        }
    }


    [Desc("BUFF扩展字段(long)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF扩展字段(double)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF扩展字段(bool)", "[游戏]/BUFF模板/扩展字段")]
    public class BuffPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(IBuffProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Buff, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.Properties, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

    #region __光环扩展__

    [Desc("光环扩展字段(string)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.Properties, FieldName);
            }
            return null;
        }
    }

    [Desc("光环扩展字段(int)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环扩展字段(float)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环扩展字段(long)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldIntegerValue64 : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环扩展字段(double)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldRealValue64 : ZoneRealValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.Properties, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环扩展字段(bool)", "[游戏]/光环模板/扩展字段")]
    public class AuraPropertiesFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(IAuraProperties), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();



        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.扩展.{1}", Aura, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.Properties, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

}
