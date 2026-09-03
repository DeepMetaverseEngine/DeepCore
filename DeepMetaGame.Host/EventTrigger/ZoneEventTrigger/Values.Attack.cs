using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("触发的值", "[游戏]/伤害源")]
    public class TriggingAttackDamage : ZoneIntegerValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的值(用于伤害)");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingNumberValue;
        }
    }

    /// <summary>
    /// 攻击属性.
    /// </summary>
    //public readonly AttackProp Attack;


    /// <summary>
    /// 技能模板.
    /// </summary>
    //public readonly SkillTemplate FromSkill;
    [Desc("造成伤害的Skill", "[游戏]/伤害源")]
    public class TriggingAttackFromSkill : SkillTemplateValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害的Skill");
        }
        protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingAttack?.FromSkill;
        }
    }



    /// <summary>
    /// 技能模板.
    /// </summary>
    //public readonly SkillTemplate FromSkill;
    [Desc("伤害源头的技能Skill", "[游戏]/伤害源")]
    public class TriggingAttackFromSourceSkill : SkillTemplateValue
    {
        protected override SkillTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingAttack != null && args.TriggingAttack.TryGetSrourceSkill(out var skill))
            {
                return skill.Data;
            }
            return null;
        }
    }



    /// <summary>
    /// 法术模板.
    /// </summary>
    //public readonly SpellTemplate FromSpell;
    [Desc("造成伤害的Spell", "[游戏]/伤害源")]
    public class TriggingAttackFromSpell : SpellTemplateValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害的Spell");
        }
        protected override SpellTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingAttack?.FromSpell;
        }
    }

    /// <summary>
    /// BUFF模板.
    /// </summary>
    //public readonly BuffTemplate FromBuff;
    [Desc("造成伤害的Buff", "[游戏]/伤害源")]
    public class TriggingAttackFromBuff : BuffTemplateValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害的Buff");
        }
        protected override BuffTemplate GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingAttack?.FromBuff;
        }
    }


    /// <summary>
    /// 技能实际状态.
    /// </summary>
    //public readonly InstanceUnit.SkillState FromSkillState;
    /// <summary>
    /// BUFF实际状态.
    /// </summary>
    //public readonly InstanceUnit.BuffState FromBuffState;
    /// <summary>
    /// 法术实际状态.
    /// </summary>
    //public readonly InstanceSpell FromSpellUnit;
    /// <summary>
    /// 是否发送协议
    /// </summary>
    //public bool OutSendEvent = true;

    /// <summary>
    /// 是否命中
    /// </summary>
    //public bool OutHitted;
    [Desc("造成伤害是否命中", "[游戏]/伤害源")]
    public class TriggingAttackOutHitted : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否命中");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage.HasValue)
                return args.TriggingDamage.Value.OutHitted;
            return false;
        }
    }

    /// <summary>
    /// 是否产生硬直.
    /// </summary>
    //public bool OutIsDamage;
    [Desc("造成伤害是否产生硬直", "[游戏]/伤害源")]
    public class TriggingAttackOutIsDamage : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否产生硬直");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutIsDamage;
            return false;
        }
    }

    /// <summary>
    /// 是否击溃.
    /// </summary>
    //public bool OutIsCrush;
    [Desc("造成伤害是否产生粉碎性打击", "[游戏]/伤害源")]
    public class TriggingAttackOutIsCrush : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否产生粉碎性打击");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutIsCrush;
            return false;
        }
    }

    /// <summary>
    /// 是否暴击.
    /// </summary>
    //public bool OutIsCritical;
    [Desc("造成伤害是否暴击", "[游戏]/伤害源")]
    public class TriggingAttackOutIsCritical : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否暴击");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutIsCritical;
            return false;
        }
    }

    /// <summary>
    /// 是否击飞.
    /// </summary>
    //public bool OutHasFly;
    [Desc("造成伤害是否击飞", "[游戏]/伤害源")]
    public class TriggingAttackOutHasFly : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否击飞");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutHasFly;
            return false;
        }
    }

    /// <summary>
    /// 是否击倒.
    /// </summary>
    //public bool OutHasKnockDown;
    [Desc("造成伤害是否击倒", "[游戏]/伤害源")]
    public class TriggingAttackOutHasKnockDown : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否击倒");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutHasKnockDown;
            return false;
        }
    }

    /// <summary>
    /// 击倒时间.
    /// </summary>
    //public int OutKnockDownTimeMS;
    [Desc("造成伤害的击倒时间", "[游戏]/伤害源")]
    public class TriggingAttackOutKnockDownTimeMS : ZoneIntegerValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害的击倒时间");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutKnockDownTimeMS;
            return 0;
        }
    }

    /// <summary>
    /// 是否对死亡单位启效
    /// </summary>
    //public bool OutCanWhiplashDeadBody = false;
    [Desc("造成伤害是否对死亡单位启效", "[游戏]/伤害源")]
    public class TriggingAttackOutCanWhiplashDeadBody : ZoneBooleanValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害是否对死亡单位启效");
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutCanWhiplashDeadBody;
            return false;
        }
    }

    /// <summary>
    /// 打击特效.
    /// </summary>
    //public LaunchEffect OutHitEffect;


    //public float OutWeight;
    [Desc("造成伤害的重量(Weight)", "[游戏]/伤害源")]
    public class TriggingAttackOutWeight : ZoneRealValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("造成伤害的重量(Weight)");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutWeight;
            return 0;
        }
    }



    //public StartMove OutHitMove;
    /// <summary>
    /// 实际掉血，如果伤害为100,实际血量为10,则该值为10
    /// </summary>
    //public int OutReducedHP;
    [Desc("造成伤害的血量", "[游戏]/伤害源")]
    public class TriggingAttackOutReducedHP : ZoneIntegerValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("造成伤害的血量");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutReducedHP;
            return 0;
        }
    }

    /// <summary>
    /// 用于表示攻击特殊状态：招架、闪避、反伤.
    /// </summary>
    //public string OutClientState;
    [Desc("造成伤害的攻击特殊状态", "[游戏]/伤害源")]
    public class TriggingAttackOutClientState : ZoneStringValue
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("造成伤害的攻击特殊状态");
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingDamage != null)
                return args.TriggingDamage.Value.OutClientState;
            return string.Empty;
        }
    }

    [Desc("触发的Spell连锁等级", "[游戏]/伤害源")]
    public class TriggingSpellChainLevel : ZoneIntegerValue
    {
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell != null)
            {
                return args.TriggingSpell.ChainLevel;
            }
            return 0;
        }
    }

    [Desc("触发的Spell连锁等级", "[游戏]/伤害源")]
    public class TriggingChainLevel : ZoneIntegerValue
    {
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell?.ChainInfo != null)
            {
                return args.TriggingSpell.ChainInfo.Level;
            }
            return 0;
        }
    }
    [Desc("触发的Spell连锁最后一个目标", "[游戏]/伤害源")]
    public class TriggingChainLastTarget : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell?.ChainInfo != null)
            {
                return args.TriggingSpell.ChainInfo.LastTarget;
            }
            return null;
        }
    }
    [Desc("触发的Spell连锁是否还能继续", "[游戏]/伤害源")]
    public class TriggingChainHasNextChain : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell?.ChainInfo != null)
            {
                return args.TriggingSpell.ChainInfo.HasNextChain;
            }
            return false;
        }
    }

    [Desc("触发的Buff层数", "[游戏]/伤害源")]
    public class TriggingBuffOverlay : ZoneIntegerValue
    {
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingEquipBuff is InstanceUnit.EquipBuff buff)
            {
                return buff.OverlayLevel;
            }
            return 0;
        }
    }
}
