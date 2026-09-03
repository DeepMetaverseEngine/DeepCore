using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using System.Xml.Linq;
using System.Security.Cryptography;
using DeepCore.Geometry;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //---------------------------------------------------------------------------------------------------------------
    public abstract class UnitEquipSkillValue : ZoneAbstractValue<InstanceUnit.EquipSkill>
    {
        [Desc("无技能", "[游戏]/功能")]
        public class NA : UnitEquipSkillValue
        {
            protected override InstanceUnit.EquipSkill GetValue(IEventTriggerAdapter api, EventArguments args) => null;
        }
        [Desc("触发的技能", "[游戏]/功能")]
        public class Trigging : UnitEquipSkillValue
        {
            protected override InstanceUnit.EquipSkill GetValue(IEventTriggerAdapter api, EventArguments args) => args.TriggingEquipSkill;
        }
        [Desc("单位技能", "[游戏]/功能")]
        public class UnitSkill : UnitEquipSkillValue
        {
            [Desc("单位")]
            public UnitValue Unit = new UnitValue.Trigging();
            [Desc("技能")]
            public SkillTemplateValue Skill = new SkillTemplateValue.Template();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}的技能{1}");
            }
            protected override InstanceUnit.EquipSkill GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = Unit?.GetValueAs(api, args);
                var t = Skill?.GetValueAs(api, args);
                if (u != null && t != null)
                {
                    return u.GetSkillState(t.ID);
                }
                return null;
            }
        }
    }
    //---------------------------------------------------------------------------------------------------------------
    public abstract class UnitEquipBuffValue : ZoneAbstractValue<InstanceUnit.EquipBuff>
    {
        [Desc("无BUFF", "[游戏]/功能")]
        public class NA : UnitEquipBuffValue
        {
            protected override InstanceUnit.EquipBuff GetValue(IEventTriggerAdapter api, EventArguments args) => null;
        }
        [Desc("触发的BUFF", "[游戏]/功能")]
        public class Trigging : UnitEquipBuffValue
        {
            protected override InstanceUnit.EquipBuff GetValue(IEventTriggerAdapter api, EventArguments args) => args.TriggingEquipBuff;
        }
        [Desc("单位BUFF", "[游戏]/功能")]
        public class UnitBuff : UnitEquipBuffValue
        {
            [Desc("单位")]
            public UnitValue Unit = new UnitValue.Trigging();
            [Desc("BUFF")]
            public BuffTemplateValue Buff = new BuffTemplateValue.Template();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}的BUFF{1}");
            }
            protected override InstanceUnit.EquipBuff GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = Unit?.GetValueAs(api, args);
                var t = Buff?.GetValueAs(api, args);
                if (u != null && t != null)
                {
                    return u.GetBuffByID(t.ID);
                }
                return null;
            }
        }
    }
    //---------------------------------------------------------------------------------------------------------------

    public abstract class UnitEquipAuraValue : ZoneAbstractValue<InstanceUnit.EquipAura>
    {
        [Desc("无光环", "[游戏]/功能")]
        public class NA : UnitEquipAuraValue
        {
            protected override InstanceUnit.EquipAura GetValue(IEventTriggerAdapter api, EventArguments args) => null;
        }
        [Desc("触发的光环", "[游戏]/功能")]
        public class Trigging : UnitEquipAuraValue
        {
            protected override InstanceUnit.EquipAura GetValue(IEventTriggerAdapter api, EventArguments args) => args.TriggingEquipAura;
        }
        [Desc("单位光环", "[游戏]/功能")]
        public class UnitBuff : UnitEquipAuraValue
        {
            [Desc("单位")]
            public UnitValue Unit = new UnitValue.Trigging();
            [Desc("光环")]
            public AuraTemplateValue Aura = new AuraTemplateValue.Template();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}的光环{1}");
            }
            protected override InstanceUnit.EquipAura GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var u = Unit?.GetValueAs(api, args);
                var t = Aura?.GetValueAs(api, args);
                if (u != null && t != null)
                {
                    return u.GetAura(t.ID);
                }
                return null;
            }
        }
    }
    //---------------------------------------------------------------------------------------------------------------
  
}
