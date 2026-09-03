using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Formula;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    //-----------------------------------------------------------------------------
    #region __BUFF模板__

    [Desc("模板-BUFF模板")]
    public abstract class BuffTemplateValue : ZoneAbstractValue<BuffTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : BuffTemplateValue
        {
            protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is BuffTemplate v3) { return v3; }
                }
                catch { }
                return null;
            }
        }
        [Desc("BUFF模板", "[游戏]/编辑器")]
        public class Template : BuffTemplateValue
        {
            [Desc("BUFF模板ID")]
            [TemplateIDAttribute(typeof(BuffTemplate))]
            public int BuffTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("BUFF模板:{0}", BuffTemplateID);
            }
            protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetBuff(BuffTemplateID);
            }
        }
        [Desc("BUFF模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : BuffTemplateValue
        {
            [Desc("模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("BUFF模板:{0}", TemplateID);
            }
            protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetBuff((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("随机BUFF模板组", "[游戏]/编辑器")]
        public class RandomInGroup : BuffTemplateValue
        {
            [Desc("BUFF模板组")]
            [TemplateGroup(typeof(BuffTemplate))]
            public string Group;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("随机BUFF模板组:{0}", Group);
            }
            protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                using (var list = api.ZoneAPI.ObjectPool.AllocList<BuffTemplate>())
                {
                    api.Templates.GetAllBuffsByPath(Group, list);
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }
        [Desc("触发的BUFF", "[游戏]/功能")]
        public class Trigging : BuffTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的BUFF");
            }
            protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingBuffTemplate;
            }
        }
    }



    [Desc("BUFF模板", "[游戏]/比较")]
    public class BuffTemplateComparison : ZoneBooleanValue
    {
        [Desc("物品1")]
        public AbstractValue<BuffTemplate> Value1 = new BuffTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("物品2")]
        public AbstractValue<BuffTemplate> Value2 = new BuffTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate c1 = Value1.GetValueAs(api, args);
            BuffTemplate c2 = Value2.GetValueAs(api, args);
            if (c1 == c2)
            {
                return true;
            }
            if (c1 != null && c2 != null)
            {
                return FormulaHelper.Compare(c1.ID, Op, c2.ID);
            }
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }

    [Desc("BUFF编辑器模板", "[游戏]/模板ID")]
    public class BuffTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateIDAttribute(typeof(BuffTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("BUFF模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }

    [Desc("BUFF模板", "[游戏]/模板ID")]
    public class ValueBuffTemplateID : ZoneIntegerValue
    {
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("BUFF({0})", Buff);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate buff = Buff.GetValueAs(api, args);
            if (buff != null)
            {
                return buff.ID;
            }
            return 0;
        }
    }

    #endregion
    //-----------------------------------------------------------------------------

    //-----------------------------------------------------------------------------
    #region __物品模板__


    [Desc("模板-物品模板")]
    public abstract class ItemTemplateValue : ZoneAbstractValue<ItemTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : ItemTemplateValue
        {
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is ItemTemplate v3) { return v3; }
                }
                catch { }
                return null;
            }
        }
        [Desc("物品模板", "[游戏]/编辑器")]
        public class Template : ItemTemplateValue
        {
            [Desc("物品模板ID")]
            [TemplateIDAttribute(typeof(ItemTemplate))]
            public int ItemTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("物品模板:{0}", ItemTemplateID);
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetItem(ItemTemplateID);
            }
        }
        [Desc("物品模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : ItemTemplateValue
        {
            [Desc("模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("物品模板:{0}", TemplateID);
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetItem((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("随机物品模板组", "[游戏]/编辑器")]
        public class RandomInGroup : ItemTemplateValue
        {
            [Desc("物品模板组")]
            [TemplateGroup(typeof(ItemTemplate))]
            public string Group;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("随机物品模板组:{0}", Group);
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                using (var list = api.ZoneAPI.ObjectPool.AllocList<ItemTemplate>())
                {
                    api.Templates.GetAllItemsByPath(Group, list);
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }

        [Desc("物品实体模板", "[游戏]/编辑器")]
        public class Instance : ItemTemplateValue
        {
            [Desc("物品实体")]
            public AbstractValue<InstanceItem> Item = new ItemValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("物品实体模板:{0}", Item);
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceItem item = Item.GetValueAs(api, args);
                if (item != null)
                {
                    return item.TemplateData;
                }
                return null;
            }
        }

        [Desc("触发的物品模板", "[游戏]/功能")]
        public class Trigging : ItemTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的物品模板");
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingItemTemplate;
            }
        }

        [Desc("最后创建的物品模板", "[游戏]/功能")]
        public class LastCreatedItem : ItemTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后创建的物品模板");
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.ZoneAPI.LastCreatedInstanceItem != null)
                {
                    return api.ZoneAPI.LastCreatedInstanceItem.TemplateData;
                }
                return null;
            }
        }
        [Desc("最后使用的物品模板", "[游戏]/功能")]
        public class LastUsedItem : ItemTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后使用的物品模板");
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastUnitUseItem;
            }
        }
        [Desc("单位最后进入背包的物品模板", "[游戏]/功能")]
        public class LastGotInventoryItem : ItemTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位最后进入背包的物品模板");
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastUnitGotInventoryItem;
            }
        }
        [Desc("单位最后从场景检取物品模板", "[游戏]/功能")]
        public class LastGotIZoneItem : ItemTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位最后从场景检取物品模板");
            }
            protected override ItemTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.ZoneAPI.LastUnitGotInstanceItem != null)
                {
                    return api.ZoneAPI.LastUnitGotInstanceItem.TemplateData;
                }
                return null;
            }
        }
    }

    [Desc("物品模板", "[游戏]/比较")]
    public class ItemTemplateComparison : ZoneBooleanValue
    {
        [Desc("物品1")]
        public AbstractValue<ItemTemplate> Value1 = new ItemTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("物品2")]
        public AbstractValue<ItemTemplate> Value2 = new ItemTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate c1 = Value1.GetValueAs(api, args);
            ItemTemplate c2 = Value2.GetValueAs(api, args);
            if (c1 == c2)
            {
                return true;
            }
            if (c1 != null && c2 != null)
            {
                return FormulaHelper.Compare(c1.ID, Op, c2.ID);
            }
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }

    [Desc("物品编辑器模板", "[游戏]/模板ID")]
    public class ItemTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateIDAttribute(typeof(ItemTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("物品模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }


    [Desc("物品模板", "[游戏]/模板ID")]
    public class ValueItemTemplateID : ZoneIntegerValue
    {
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("物品({0})", Item);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            if (item != null)
            {
                return item.ID;
            }
            return 0;
        }
    }


    [Desc("物品单位模板", "[游戏]/模板ID")]
    public class IZoneItemTemplateID : ZoneIntegerValue
    {
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("物品单位({0})", Item);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem item = Item.GetValueAs(api, args);
            if (item != null)
            {
                return item.TemplateData.ID;
            }
            return 0;
        }
    }


    #endregion
    //-----------------------------------------------------------------------------
    #region __技能模板__


    [Desc("模板-技能模板")]
    public abstract class SkillTemplateValue : ZoneAbstractValue<SkillTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : SkillTemplateValue
        {
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is SkillTemplate v3) { return v3; }
                }
                catch { }
                return null;
            }
        }
        [Desc("技能模板", "[游戏]/编辑器")]
        public class Template : SkillTemplateValue
        {
            [Desc("技能模板ID")]
            [TemplateIDAttribute(typeof(SkillTemplate))]
            public int SkillTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("技能模板:{0}", SkillTemplateID);
            }
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetSkill(SkillTemplateID);
            }
        }
        [Desc("技能模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : SkillTemplateValue
        {
            [Desc("模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("技能模板:{0}", TemplateID);
            }
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetSkill((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("随机技能模板组", "[游戏]/编辑器")]
        public class RandomInGroup : SkillTemplateValue
        {
            [Desc("技能模板组")]
            [TemplateGroup(typeof(SkillTemplate))]
            public string Group;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("随机技能模板组:{0}", Group);
            }
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                using (var list = api.ZoneAPI.ObjectPool.AllocList<SkillTemplate>())
                {
                    api.Templates.GetAllSkillsByPath(Group, list);
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }
        [Desc("触发的技能", "[游戏]/功能")]
        public class Trigging : SkillTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的技能");
            }
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingSkillTemplate;
            }
        }
        [Desc("最后释放的技能", "[游戏]/功能")]
        public class LastLaunchSkill : SkillTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后释放的技能");
            }
            protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastLaunchSkill;
            }
        }
    }

    [Desc("技能模板", "[游戏]/比较")]
    public class SkillTemplateComparison : ZoneBooleanValue
    {
        [Desc("技能1")]
        public AbstractValue<SkillTemplate> Value1 = new SkillTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("技能2")]
        public AbstractValue<SkillTemplate> Value2 = new SkillTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate c1 = Value1.GetValueAs(api, args);
            SkillTemplate c2 = Value2.GetValueAs(api, args);
            if (c1 == c2)
            {
                return true;
            }
            if (c1 != null && c2 != null)
            {
                return FormulaHelper.Compare(c1.ID, Op, c2.ID);
            }
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }
    [Desc("触发的技能模板是哪个", "[游戏]/比较")]
    public class TriggingSkillTemplateComparison : ZoneBooleanValue
    {
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Temp = new SkillTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的技能模板{0}({1})", FormulaHelper.ToString(Op), Temp);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var c1 = Temp.GetValueAs(api, args);
            return FormulaHelper.Compare(c1, Op, args.TriggingSkillTemplate);
        }
    }


    [Desc("技能编辑器模板", "[游戏]/模板ID")]
    public class SkillTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateIDAttribute(typeof(SkillTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("技能模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }

    [Desc("技能模板", "[游戏]/模板ID")]
    public class ValueSkillTemplateID : ZoneIntegerValue
    {
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("技能({0})", Skill);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate skill = Skill.GetValueAs(api, args);
            if (skill != null)
            {
                return skill.ID;
            }
            return 0;
        }
    }

    #endregion
    //-----------------------------------------------------------------------------
    #region __法术模板__


    [Desc("模板-法术模板")]
    public abstract class SpellTemplateValue : ZoneAbstractValue<SpellTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : SpellTemplateValue
        {
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.ReturnValue as SpellTemplate;
            }
        }
        [Desc("法术模板", "[游戏]/编辑器")]
        public class Template : SpellTemplateValue
        {
            [Desc("法术模板ID")]
            [TemplateIDAttribute(typeof(SpellTemplate))]
            public int SpellTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("法术模板:{0}", SpellTemplateID);
            }
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetSpell(SpellTemplateID);
            }
        }
        [Desc("法术模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : SpellTemplateValue
        {
            [Desc("模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("法术模板:{0}", TemplateID);
            }
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetSpell((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("法术技能模板组", "[游戏]/编辑器")]
        public class RandomInGroup : SpellTemplateValue
        {
            [Desc("法术模板组")]
            [TemplateGroup(typeof(SpellTemplate))]
            public string Group;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("随机法术模板组:{0}", Group);
            }
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                using (var list = api.ZoneAPI.ObjectPool.AllocList<SpellTemplate>())
                {
                    api.Templates.GetAllSpellsByPath(Group, list);
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }
        [Desc("触发的法术", "[游戏]/功能")]
        public class Trigging : SpellTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的Spell");
            }
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingSpellTemplate;
            }
        }
        [Desc("最后释放的法术", "[游戏]/功能")]
        public class LastLaunchSpell : SpellTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后释放的法术");
            }
            protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastLaunchSpell;
            }
        }
    }

    [Desc("法术模板", "[游戏]/比较")]
    public class SpellTemplateComparison : ZoneBooleanValue
    {
        [Desc("法术1")]
        public AbstractValue<SpellTemplate> Value1 = new SpellTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("法术2")]
        public AbstractValue<SpellTemplate> Value2 = new SpellTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}){1}({2})", Value1, FormulaHelper.ToString(Op), Value2);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate c1 = Value1.GetValueAs(api, args);
            SpellTemplate c2 = Value2.GetValueAs(api, args);
            if (c1 == c2)
            {
                return true;
            }
            if (c1 != null && c2 != null)
            {
                return FormulaHelper.Compare(c1.ID, Op, c2.ID);
            }
            return FormulaHelper.Compare(c1, Op, c2);
        }
    }
    [Desc("触发的法术模板是哪个", "[游戏]/比较")]
    public class TriggingSpellTemplateComparison : ZoneBooleanValue
    {
        [Desc("技能")]
        public AbstractValue<SpellTemplate> Temp = new SpellTemplateValue.Trigging();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的法术模板{0}({1})", FormulaHelper.ToString(Op), Temp);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var c1 = Temp.GetValueAs(api, args);
            return FormulaHelper.Compare(c1, Op, args.TriggingSpellTemplate);
        }
    }

    [Desc("法术编辑器模板", "[游戏]/模板ID")]
    public class SpellTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateIDAttribute(typeof(SpellTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("法术模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }
    [Desc("法术模板", "[游戏]/模板ID")]
    public class ValueSpellTemplateID : ZoneIntegerValue
    {
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("法术({0})", Spell);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate spell = Spell.GetValueAs(api, args);
            if (spell != null)
            {
                return spell.ID;
            }
            return 0;
        }
    }



    #endregion
    //-----------------------------------------------------------------------------
    #region __单位模板__



    [Desc("模板-单位模板")]
    public abstract class UnitTemplateValue : ZoneAbstractValue<UnitInfo>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : UnitTemplateValue
        {
            protected override UnitInfo GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.ReturnValue as UnitInfo;
            }
        }
        [Desc("单位模板", "[游戏]/编辑器")]
        public class Template : UnitTemplateValue
        {
            [Desc("单位模板ID")]
            [TemplateID(typeof(UnitInfo))]
            public int TemplateID;
            public Template() { }
            public Template(int templateID) { TemplateID = templateID; }

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位模板:{0}", TemplateID);
            }
            protected override UnitInfo GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetUnit(TemplateID);
            }
        }
        [Desc("单位模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : UnitTemplateValue
        {
            [Desc("单位模板ID")]
            [TemplateID(typeof(UnitInfo))]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位模板:{0}", TemplateID);
            }
            protected override UnitInfo GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetUnit((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("随机单位模板组", "[游戏]/编辑器")]
        public class RandomInGroup : UnitTemplateValue
        {
            [Desc("单位模板组")]
            [TemplateGroup(typeof(UnitInfo))]
            public string Group;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("随机单位模板组:{0}", Group);
            }
            protected override UnitInfo GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                using (var list = api.ZoneAPI.ObjectPool.AllocList<UnitInfo>())
                {
                    api.Templates.GetAllUnitsByPath(Group, list);
                    return api.ZoneAPI.RandomN.GetRandomInCollection(list);
                }
            }
        }
        [Desc("触发的单位模板", "[游戏]/功能")]
        public class Trigging : UnitTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的单位模板");
            }
            protected override UnitInfo GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingUnit?.TemplateData;
            }
        }
    }


    [Desc("单位编辑器模板", "[游戏]/模板ID")]
    public class UnitTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateIDAttribute(typeof(UnitInfo))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }

    [Desc("单位模板", "[游戏]/模板ID")]
    public class ValueUnitTemplateID : ZoneIntegerValue
    {
        [Desc("单位模板")]
        public AbstractValue<UnitInfo> Unit = new UnitTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位模板({0})", Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = Unit.GetValueAs(api, args);
            if (a != null)
            {
                return a.ID;
            }
            return 0;
        }
    }

    #endregion
    //-----------------------------------------------------------------------------
    #region __Aura模板__

    [Desc("模板-光环模板")]
    public abstract class AuraTemplateValue : ZoneAbstractValue<AuraTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : AuraTemplateValue
        {
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args) => args.ReturnValue as AuraTemplate;
        }
        [Desc("光环模板", "[游戏]/编辑器")]
        public class Template : AuraTemplateValue
        {
            [Desc("光环模板ID")]
            [TemplateIDAttribute(typeof(AuraTemplate))]
            public int AuraTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("光环模板:{0}", AuraTemplateID);
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetAura(AuraTemplateID);
            }
        }
        [Desc("光环模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : AuraTemplateValue
        {
            [Desc("模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("光环模板:{0}", TemplateID);
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetAura((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("触发的光环", "[游戏]/功能")]
        public class Trigging : AuraTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的光环");
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingAuraTemplate;
            }
        }

        [Desc("最后释放的光环模板", "[游戏]/功能")]
        public class LastLaunchedAura : AuraTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后释放的光环模板");
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastUnitLaunchAura;
            }
        }
        [Desc("最后进入的光环模板", "[游戏]/功能")]
        public class LastEnterdAura : AuraTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后进入的光环模板");
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastUnitEnterAura;
            }
        }
        [Desc("最后离开的光环模板", "[游戏]/功能")]
        public class LastLeavedAura : AuraTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后离开的光环模板");
            }
            protected override AuraTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastUnitLeaveAura;
            }
        }
    }

    [Desc("光环编辑器模板", "[游戏]/模板ID")]
    public class AuraTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateID(typeof(AuraTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("光环模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }

    [Desc("光环模板", "[游戏]/模板ID")]
    public class ValueAuraTemplateID : ZoneIntegerValue
    {
        [Desc("光环模板")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("光环模板({0})", Aura);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = Aura.GetValueAs(api, args);
            if (a != null)
            {
                return a.ID;
            }
            return 0;
        }
    }

    #endregion
    //-----------------------------------------------------------------------------
    #region __Card模板__

    [Desc("模板-词缀模板")]
    public abstract class CardTemplateValue : ZoneAbstractValue<CardTemplate>
    {
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : CardTemplateValue
        {
            protected override CardTemplate GetValue(IEventTriggerAdapter api, EventArguments args) => args.ReturnValue as CardTemplate;
        }
        [Desc("词缀模板", "[游戏]/编辑器")]
        public class Template : CardTemplateValue
        {
            [Desc("词缀模板ID")]
            [TemplateIDAttribute(typeof(CardTemplate))]
            public int CardTemplateID;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("词缀模板:{0}", CardTemplateID);
            }
            protected override CardTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetCard(CardTemplateID);
            }
        }
        [Desc("词缀模板(变量)", "[游戏]/编辑器")]
        public class TemplateSV : CardTemplateValue
        {
            [Desc("词缀模板ID")]
            public AbstractValue<double> TemplateID = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("词缀模板:{0}", TemplateID);
            }
            protected override CardTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.Templates.GetCard((int)TemplateID.GetValueAs(api, args));
            }
        }
        [Desc("触发的词缀模板", "[游戏]/功能")]
        public class Trigging : CardTemplateValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的词缀模板");
            }
            protected override CardTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingCardTemplate;
            }
        }
    }

    [Desc("词缀编辑器模板", "[游戏]/模板ID")]
    public class CardTemplateID : ZoneIntegerValue
    {
        [Desc("模板ID")]
        [TemplateID(typeof(CardTemplate))]
        public int ID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("词缀模板:{0}", ID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return ID;
        }
    }

    [Desc("词缀模板", "[游戏]/模板ID")]
    public class ValueCardTemplateID : ZoneIntegerValue
    {
        [Desc("词缀模板")]
        public AbstractValue<CardTemplate> Card = new CardTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("词缀模板({0})", Card);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = Card.GetValueAs(api, args);
            if (a != null)
            {
                return a.ID;
            }
            return 0;
        }
    }
    #endregion
    //-----------------------------------------------------------------------------
    #region __GUI模板__


    #endregion
    //-----------------------------------------------------------------------------

}
