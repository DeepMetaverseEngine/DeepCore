using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.Data;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.FlagValue;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    //--------------------------------------------------------------------------------------
   

    [Desc("单位释放技能", "[游戏]/单位/法术&技能")]
    public class UnitLaunchSkillAction : ZoneAbstractAction<InstanceUnit.EquipSkill>
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("释放技能")] public LaunchSkill Skill = new LaunchSkill();

        [Desc("目标")] public AbstractValue<InstanceUnit> Target = new UnitValue.UnitTarget();

        [Desc("目标位置")]
        public AbstractValue<Vector3?> Position = new PositionValue.PositionOfUnit()
        {
            Unit = new UnitValue.UnitTarget(),
        };

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})释放技能({1});", Unit, Skill);
        }

        override protected InstanceUnit.EquipSkill Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && Skill != null)
            {
                var target = Target.GetValueAs(api, args);
                if (target != null)
                {
                    return unit.LaunchSkill(Skill.SkillID, new TLaunchSkillParam()
                    {
                        TargetUnitID = target.ID,
                    });
                }
                if (Position != null)
                {
                    var pos = Position.GetValueAs(api, args);
                    if (pos.HasValue)
                    {
                        return unit.LaunchSkill(Skill.SkillID, new TLaunchSkillParam()
                        {
                            SpellTargetPos = pos,
                        });
                    }
                }
                return unit.LaunchSkill(Skill.SkillID, new TLaunchSkillParam() { });
            }
            return null;
        }
    }

    [Desc("单位停止技能", "[游戏]/单位/法术&技能")]
    public class UnitStopSkillAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})单位停止技能;", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.CancelCurrentSkill();
            }

            return unit;
        }
    }
    [Desc("给单位添加BUFF", "[游戏]/单位/法术&技能")]
    public class UnitAddBuffAction : ZoneAbstractAction<InstanceUnit.EquipBuff>
    {
        [Desc("添加BUFF的单位")] public AbstractValue<InstanceUnit> Sender = null;
        [Desc("被添加BUFF的单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("Buff模板ID")]
        [TemplateIDAttribute(typeof(BuffTemplate))]
        public int BuffTemplateID;

        [Desc("Buff等级")] public AbstractValue<double> BuffLevel = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            if (Sender != null)
            {
                sw.AppendFormat("{0}给({1})添加BUFF({2}));", Sender, Unit, BuffTemplateID);
            }
            else
            {
                sw.AppendFormat("给({0})添加BUFF({1}));", Unit, BuffTemplateID);
            }
        }

        override protected InstanceUnit.EquipBuff Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);

            if (Sender != null)
            {
                InstanceUnit sender = Sender.GetValueAs(api, args);
                if (sender != null && unit != null)
                {
                    int buffLevel = (int)BuffLevel.GetValueAs(api, args);
                    return unit.AddBuff(BuffTemplateID, buffLevel, sender);
                }
            }
            if (unit != null)
            {
                int buffLevel = (int)BuffLevel.GetValueAs(api, args);
                return unit.AddBuff(BuffTemplateID, buffLevel);
            }
            return null;
        }
    }

    [Desc("给单位自己添加BUFF", "[游戏]/单位/法术&技能")]
    public class UnitSelfAddBuffAction : ZoneAbstractAction<InstanceUnit.EquipBuff>
    {
        [Desc("被添加BUFF的单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("Buff模板ID")]
        [TemplateIDAttribute(typeof(BuffTemplate))]
        public int BuffTemplateID;

        [Desc("Buff等级")] public AbstractValue<double> BuffLevel = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})添加BUFF({1}));", Unit, BuffTemplateID);
        }

        override protected InstanceUnit.EquipBuff Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                int buffLevel = (int)BuffLevel.GetValueAs(api, args);
                return unit.AddBuff(BuffTemplateID, buffLevel);
            }
            return null;
        }
    }

    [Desc("给单位删除BUFF", "[游戏]/单位/法术&技能")]
    public class UnitRemoveBuffAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Buff")] public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})删除BUFF({1});", Unit, Buff);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var buff = Buff.GetValueAs(api, args);
            if (unit != null && buff != null)
            {
                unit.RemoveBuff(buff.ID);
            }

            return unit;
        }
    }

    [Desc("单位清除所有BUFF（Debuff）", "[游戏]/单位/法术&技能")]
    public class UnitCleanBuffAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("清理{0}所有的BUFF;", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.ClearBuffs();
            }

            return unit;
        }
    }


    [Desc("给单位启动光环", "[游戏]/单位/法术&技能")]
    public class UnitLaunchAuraAction : ZoneAbstractAction<InstanceUnit.EquipAura>
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("光环模板ID")]
        [TemplateIDAttribute(typeof(AuraTemplate))]
        public int AuraTemplateID;

        [Desc("光环等级")] public AbstractValue<double> AuraLevel = new IntegerValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})启动等级{1}光环({2});", Unit, AuraLevel, AuraTemplateID);
        }

        override protected InstanceUnit.EquipAura Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.LaunchAura(AuraTemplateID, (int)AuraLevel.GetValueAs(api, args), null);
            }
            return null;
        }
    }

    [Desc("给单位删除光环", "[游戏]/单位/法术&技能")]
    public class UnitRemoveAuraAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Aura")] public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})删除光环({1});", Unit, Aura);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var aura = Aura.GetValueAs(api, args);
            if (unit != null && aura != null)
            {
                unit.RemoveAura(aura.ID);
            }

            return unit;
        }
    }

    [Desc("单位清除技能CD", "[游戏]/单位/法术&技能")]
    public class UnitClearCDAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})清除技能CD;", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.ClearAllSkillCD();
            }

            return unit;
        }
    }

    [Desc("单位技能进入CD（万分比冷却）", "[游戏]/单位/法术&技能")]
    public class UnitEnterCDAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("万分比")] public AbstractValue<double> pct = new IntegerValue.VALUE(0);
        [Desc("是否通知所有单位")]
        public bool SendNty = false;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})进入技能CD万分比{1};", Unit, pct);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.EnterAllSkillCD(0, (int)pct.GetValueAs(api, args), SendNty);
            }

            return unit;
        }
    }


    [Desc("单位指定能进入CD（万分比冷却）", "[游戏]/单位/法术&技能")]
    public class UnitEnterSkillCDAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("万分比")] public AbstractValue<double> pct = new IntegerValue.VALUE(0);
        [Desc("技能id,0表示所有技能")]
        [TemplateID(typeof(SkillTemplate))]
        public int SkillId = 0;
        [Desc("是否通知所有单位")]
        public bool SendNty = false;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})进入技能CD万分比{1};", Unit, pct);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.EnterAllSkillCD(SkillId, (int)pct.GetValueAs(api, args), SendNty);
            }

            return unit;
        }
    }


}