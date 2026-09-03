using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    #region 技能法术释放---------------------------------------------------------------------------------------------


    [Desc("某个单位释放技能", "[游戏]/单位-某个单位")]
    public class GenericUnitLaunchSkill : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位释放技能");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitLaunchSkillHandler handler = new InstanceZone.UnitLaunchSkillHandler((z, u, s, ss) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipSkill = s;
                args.TriggingSkillTemplate = s.Data;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLaunchSkill += handler,
                static (zone, handler) => zone.OnUnitLaunchSkill -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
    }
    [Desc("某个单位正常结束技能", "[游戏]/单位-某个单位")]
    public class GenericUnitOverSkill : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位正常结束技能");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitOverSkillHandler((z, u, s, st) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipSkill = s;
                args.TriggingSkillTemplate = s.Data;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitOverSkill += handler,
                static (zone, handler) => zone.OnUnitOverSkill -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate SkillTemp(EventArguments args) => args.TriggingSkillTemplate;
    }

    [Desc("某个单位释放Spell", "[游戏]/单位-某个单位")]
    public class GenericUnitLaunchSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位释放Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.LaunchSpellHandler((z, s, add) =>
            {
                args.TriggingUnit = add.launcher;
                args.TriggingSkillTemplate = s.FromSkillTemplateID?.Data;
                args.TriggingSpellTemplate = add.template;
                args.TriggingSpell = s;
                //args.TriggingChainInfo = add.chain;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnLaunchSpell += handler,
                static (zone, handler) => zone.OnLaunchSpell -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate SkillTemp(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate SpellTemp(EventArguments args) => args.TriggingSpellTemplate;
    }
    [Desc("某个单位终结Spell", "[游戏]/单位-某个单位")]
    public class GenericUnitRemoveSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位终结Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.RemoveSpellHandler((z, s) =>
            {
                args.TriggingUnit = s.LauncherOwner;
                args.TriggingSkillTemplate = s.FromSkillTemplateID?.Data;
                args.TriggingSpellTemplate = s.Info;
                args.TriggingSpell = s;
                //args.TriggingChainInfo = s.ChainInfo;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnRemoveSpell += handler,
                static (zone, handler) => zone.OnRemoveSpell -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate SkillTemp(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate SpellTemp(EventArguments args) => args.TriggingSpellTemplate;
    }


    [Desc("指定单位释放技能", "[游戏]/[指定单位]")]
    public class SpecifyUnitLaunchSkill : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})释放技能", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.OnLaunchSkillHandler handler = new InstanceUnit.OnLaunchSkillHandler((u, s, state) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipSkill = s;
                    args.TriggingSkillTemplate = s.Data;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnLaunchSkill += handler,
                    static (unit, handler) => unit.OnLaunchSkill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate Temp(EventArguments args) => args.TriggingSkillTemplate;
    }

    [Desc("指定单位正常结束技能", "[游戏]/[指定单位]")]
    public class SpecifyUnitOverSkill : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})正常结束技能", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.OnOverSkillHandler((u, s, st) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipSkill = s;
                    args.TriggingSkillTemplate = s.Data;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnOverSkill += handler,
                    static (unit, handler) => unit.OnOverSkill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate Temp(EventArguments args) => args.TriggingSkillTemplate;
    }
    [Desc("指定单位释放Spell", "[游戏]/[指定单位]")]
    public class SpecifyUnitLaunchSpell : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})释放Spell", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.LaunchSpellHandler((u, s, add) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingSkillTemplate = s.FromSkillTemplateID?.Data;
                    args.TriggingSpellTemplate = add.template;
                    args.TriggingSpell = s;
                    //args.TriggingChainInfo = add.chain;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnLaunchSpell += handler,
                    static (unit, handler) => unit.OnLaunchSpell -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate Spell(EventArguments args) => args.TriggingSpellTemplate;
    }
    [Desc("指定单位终结Spell", "[游戏]/[指定单位]")]
    public class SpecifyUnitRemoveSpell : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})终结Spell", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.RemoveSpellHandler((u, s) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingSkillTemplate = s.FromSkillTemplateID?.Data;
                    args.TriggingSpellTemplate = s.Info;
                    args.TriggingSpell = s;
                    //args.TriggingChainInfo = s.ChainInfo;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnRemoveSpell += handler,
                    static (unit, handler) => unit.OnRemoveSpell -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate Spell(EventArguments args) => args.TriggingSpellTemplate;
    }

    [Desc("绑定的单位释放技能", "[游戏]/[绑定的单位]")]
    public class BindingUnitLaunchSkill : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)释放技能");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.OnLaunchSkillHandler handler = new InstanceUnit.OnLaunchSkillHandler((u, s, ss) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipSkill = s;
                    args.TriggingSkillTemplate = s.Data;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnLaunchSkill += handler,
                    static (unit, handler) => unit.OnLaunchSkill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
    }
    [Desc("绑定的单位正常结束技能", "[游戏]/[绑定的单位]")]
    public class BindingUnitOverSkill : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)正常结束技能");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.OnOverSkillHandler((u, s, st) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipSkill = s;
                    args.TriggingSkillTemplate = s.Data;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnOverSkill += handler,
                    static (unit, handler) => unit.OnOverSkill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
    }


    [Desc("绑定的单位释放Spell", "[游戏]/[绑定的单位]")]
    public class BindingUnitLaunchSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)释放Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.LaunchSpellHandler((u, s, add) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingSpellTemplate = add.template;
                    args.TriggingSpell = s;
                    //args.TriggingChainInfo = add.chain;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnLaunchSpell += handler,
                    static (unit, handler) => unit.OnLaunchSpell -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate Spell(EventArguments args) => args.TriggingSpellTemplate;
    }
    [Desc("绑定的单位终结Spell", "[游戏]/[绑定的单位]")]
    public class BindingUnitRemoveSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)终结Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.RemoveSpellHandler((u, s) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingSpellTemplate = s.Info;
                    args.TriggingSpell = s;
                    //args.TriggingChainInfo = s.ChainInfo;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnRemoveSpell += handler,
                    static (unit, handler) => unit.OnRemoveSpell -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能源模板")] public SkillTemplate Skill(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("法术模板")] public SpellTemplate Spell(EventArguments args) => args.TriggingSpellTemplate;
    }

    #endregion

    #region Spell发射Spell---------------------------------------------------------------------------------------------

    [Desc("某个单位Spell发射Spell", "[游戏]/单位-某个单位")]
    public class GenericUnitSpellLaunchSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位Spell发射Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceZone.UnitSpellLaunchSpellHandler((attacker, sender, add) =>
            {
                args.TriggingUnit = attacker;
                args.TriggingCounterPart = api.ZoneAPI.GetUnit(add.target_obj_id);
                args.TriggingSpell = sender;
                //args.TriggingChainInfo = spell?.ChainInfo;
                args.TriggingSkillTemplate = sender.FromSkillTemplateID?.Data;
                args.TriggingSpellTemplate = sender.Info;
                args.TriggingEquipSkill = sender.FromSkillTemplateID;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.UnitSpellLaunchSpell += handler,
                static (zone, handler) => zone.UnitSpellLaunchSpell -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Spell目标")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("原始技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("原始法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
    }


    [Desc("指定单位Spell发射Spell", "[游戏]/[指定单位]")]
    public class SpecifyUnitSpellLaunchSpell : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}Spell发射Spell", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceUnit.UnitSpellLaunchSpellHandler((u, sender, add) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = api.ZoneAPI.GetUnit(add.target_obj_id);
                args.TriggingSpell = sender;
                //args.TriggingChainInfo = spell?.ChainInfo;
                args.TriggingSkillTemplate = sender.FromSkillTemplateID?.Data;
                args.TriggingSpellTemplate = sender.Info;
                args.TriggingEquipSkill = sender.FromSkillTemplateID;
                api.TestAndDoAction(args);
            });
            api.Listen(api.UnitAPI, handler,
                static (u, handler) => u.UnitSpellLaunchSpell += handler,
                static (u, handler) => u.UnitSpellLaunchSpell -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Spell目标")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("原始技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("原始法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
    }



    [Desc("绑定的单位Spell发射Spell", "[游戏]/[绑定的单位]")]
    public class BindingUnitSpellLaunchSpell : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当绑定的单位Spell发射Spell");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceUnit.UnitSpellLaunchSpellHandler((u, sender, add) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = api.ZoneAPI.GetUnit(add.target_obj_id);
                args.TriggingSpell = sender;
                //args.TriggingChainInfo = spell?.ChainInfo;
                args.TriggingSkillTemplate = sender.FromSkillTemplateID?.Data;
                args.TriggingSpellTemplate = sender.Info;
                args.TriggingEquipSkill = sender.FromSkillTemplateID;
                api.TestAndDoAction(args);
            });
            api.Listen(api.UnitAPI, handler,
                static (u, handler) => u.UnitSpellLaunchSpell += handler,
                static (u, handler) => u.UnitSpellLaunchSpell -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Spell目标")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("原始技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("原始法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
    }


    #endregion

    #region Spell击中目标---------------------------------------------------------------------------------------------

    [Desc("某个单位Spell击中目标", "[游戏]/单位-某个单位")]
    public class GenericUnitSpellHitted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位Spell击中目标");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceZone.UnitSpellHittedHandler((attacker, spell, hitted, attack) =>
            {
                args.TriggingUnit = attacker;
                args.TriggingCounterPart = hitted;
                args.TriggingAttack = attack;
                args.TriggingSpell = spell;
                //args.TriggingChainInfo = spell?.ChainInfo;
                args.TriggingSkillTemplate = attack.FromSkill;
                args.TriggingSpellTemplate = attack.FromSpell;
                args.TriggingBuffTemplate = attack.FromBuff;
                args.TriggingEquipBuff = attack.FromBuffState;
                args.TriggingEquipSkill = attack.FromSkillState;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.UnitSpellHitted += handler,
                static (zone, handler) => zone.UnitSpellHitted -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击者")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    [Desc("指定单位Spell击中目标", "[游戏]/[指定单位]")]
    public class SpecifyUnitSpellHitted : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}的Spell击中目标", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                // args = args.Clone();
                var handler = new InstanceUnit.UnitSpellHittedHandler((u, spell, hitted, attack) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = hitted;
                    args.TriggingAttack = attack;
                    args.TriggingSpell = spell;
                    //args.TriggingChainInfo = spell?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (zone, handler) => zone.UnitSpellHitted += handler,
                    static (zone, handler) => zone.UnitSpellHitted -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    [Desc("绑定的单位Spell击中目标", "[游戏]/[绑定的单位]")]
    public class BindingUnitSpellHitted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当绑定的单位Spell击中目标");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = api.UnitAPI;
            {
                // args = args.Clone();
                var handler = new InstanceUnit.UnitSpellHittedHandler((u, spell, hitted, attack) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = hitted;
                    args.TriggingAttack = attack;
                    args.TriggingSpell = spell;
                    //args.TriggingChainInfo = spell?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (zone, handler) => zone.UnitSpellHitted += handler,
                    static (zone, handler) => zone.UnitSpellHitted -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    #endregion

    #region Spell首次命中目标---------------------------------------------------------------------------------------------

    [Desc("某个单位Spell首次击中目标", "[游戏]/单位-某个单位")]
    public class GenericUnitSpellFirstHitted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("某个单位Spell首次击中目标");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceZone.UnitSpellFirstHittedHandler((attacker, spell, hitted, attack) =>
            {
                args.TriggingUnit = attacker;
                args.TriggingCounterPart = hitted;
                args.TriggingAttack = attack;
                args.TriggingSpell = spell;
                //args.TriggingChainInfo = spell?.ChainInfo;
                args.TriggingSkillTemplate = attack.FromSkill;
                args.TriggingSpellTemplate = attack.FromSpell;
                args.TriggingBuffTemplate = attack.FromBuff;
                args.TriggingEquipBuff = attack.FromBuffState;
                args.TriggingEquipSkill = attack.FromSkillState;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.UnitSpellFirstHitted += handler,
                static (zone, handler) => zone.UnitSpellFirstHitted -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击者")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    [Desc("指定单位Spell首次击中目标", "[游戏]/[指定单位]")]
    public class SpecifyUnitSpellFirstHitted : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}的Spell首次击中目标", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                // args = args.Clone();
                var handler = new InstanceUnit.UnitSpellFirstHittedHandler((u, spell, hitted, attack) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = hitted;
                    args.TriggingAttack = attack;
                    args.TriggingSpell = spell;
                    //args.TriggingChainInfo = spell?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (zone, handler) => zone.UnitSpellFirstHitted += handler,
                    static (zone, handler) => zone.UnitSpellFirstHitted -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    [Desc("绑定的单位Spell首次击中目标", "[游戏]/[绑定的单位]")]
    public class BindingUnitSpellFirstHitted : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当绑定的单位Spell首次击中目标");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = api.UnitAPI;
            {
                // args = args.Clone();
                var handler = new InstanceUnit.UnitSpellFirstHittedHandler((u, spell, hitted, attack) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = hitted;
                    args.TriggingAttack = attack;
                    args.TriggingSpell = spell;
                    //args.TriggingChainInfo = spell?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (zone, handler) => zone.UnitSpellFirstHitted += handler,
                    static (zone, handler) => zone.UnitSpellFirstHitted -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }
    #endregion

    /*
    [Desc("绑定单位尝试释放技能(用于定位目标)", "[游戏]/[绑定的单位]")]
    public class BindingUnitTryLaunchSkill : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceUnit.TryLaunchSkill((u, skill, ref param) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipSkill = skill;
                args.TriggingSkillTemplate = skill.Data;
                args.PutAttribute(nameof(LaunchSkillParam), param);
                api.TestAndDoAction(args);
                param = args.GetAttributeAs<LaunchSkillParam>(nameof(LaunchSkillParam));
                return skill.IsCD;
            });
            api.Listen(api.UnitAPI, handler,
                static (u, handler) => u.OnTryLaunchSkill += handler,
                static (u, handler) => u.OnTryLaunchSkill -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
    }


    [Desc("指定单位尝试释放技能(用于定位目标)", "[游戏]/[绑定的单位]")]
    public class SpecUnitTryLaunchSkill : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api, args) is InstanceUnit unit)
            {
                var handler = new InstanceUnit.TryLaunchSkill((u, skill, ref param) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipSkill = skill;
                    args.TriggingSkillTemplate = skill.Data;
                    args.PutAttribute(nameof(LaunchSkillParam), param);
                    api.TestAndDoAction(args);
                    param = args.GetAttributeAs<LaunchSkillParam>(nameof(LaunchSkillParam));
                    return true;
                });
                api.Listen(unit, handler,
                    static (u, handler) => u.OnTryLaunchSkill += handler,
                    static (u, handler) => u.OnTryLaunchSkill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("技能模板")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
    }
    */

}
