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
    [Desc("单位释放法术", "[游戏]/单位/法术&技能")]
    public class UnitLaunchSpellAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("释放法术")] public LaunchSpell Spell = new LaunchSpell();
        [Desc("目标")] public AbstractValue<InstanceUnit> Target = new UnitValue.UnitTarget();
        [Desc("开始位置")]
        public AbstractValue<Vector3?> StartPosition = new PositionValue.PositionOfUnit()
        {
            Unit = new UnitValue.Trigging(),
        };
        [Desc("目标位置")]
        public AbstractValue<Vector3?> TargetPosition = new PositionValue.PositionOfUnit()
        {
            Unit = new UnitValue.UnitTarget(),
        };
        [Desc("关联SkillID")]
        public AbstractValue<SkillTemplate> SkillID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})对（{2}或{3}）释放法术({1});", Unit, Spell, Target, TargetPosition);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && Spell != null)
            {
                var skillID = SkillID?.GetValueAs(api, args)?.ID;
                var startPos = StartPosition?.GetValueAs(api, args);
                if (startPos == null) startPos = unit.Position;
                var targetPos = TargetPosition?.GetValueAs(api, args);
                var target = Target?.GetValueAs(api, args);
                if (target != null)
                {
                    unit.LaunchSpell(unit, Spell, startPos.Value, target, skillID);
                }
                else
                {
                    unit.LaunchSpell(unit, Spell, startPos.Value, targetPos, skillID);
                }
            }
            return unit;
        }
    }

    [Desc("单位在区域内释放连线法术", "[游戏]/单位/法术&技能")]
    public class UnitLaunchSpellLinesAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("释放法术")] public LaunchSpell Spell = new LaunchSpell();
        [Desc("区域")] public FlagValue Region = new EditorRegion();
        [Desc("段数")] public AbstractValue<double> Count = new ZoneIntegerValue.VALUE(3);
        [Desc("闭合")] public AbstractValue<bool> Close = new ZoneBooleanValue.VALUE(true);
        [Desc("关联SkillID")] public AbstractValue<SkillTemplate> SkillID;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})在区域{3}内释放{2}段连锁法术({1});", Unit, Spell, Count, Region);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && Spell != null)
            {
                var skillID = SkillID?.GetValueAs(api, args)?.ID;
                var count = Count.GetValueAs<int>(api, args);
                var region = Region.GetValueAs(api, args);
                if ((region != null))
                {
                    var skill = skillID.HasValue ? unit.GetSkillState(skillID.Value) : null;
                    var chain = (Spell.ChainLevel > 0 ? SpellChainContext.Alloc(api.ZoneAPI, Spell) : null);
                    try
                    {
                        using (var pos_list = unit.ObjectPool.AllocList<Vector3>())
                        {
                            for (int i = -1; i < count; i++)
                            {
                                pos_list.Add(region.GetRandomPos());
                            }
                            for (int i = 0; i < count; i++)
                            {
                                var p1 = pos_list[i];
                                var p2 = pos_list[i + 1];
                                api.ZoneAPI.UnitLaunchSpell(
                                    launcher: unit,
                                    sender: unit,
                                    launch: Spell,
                                    from: unit,
                                    startPos: p1,
                                    fromeSkillTemplateID: skill,
                                    targetUnit: null,
                                    targetPos: p2,
                                    faceDir: null,
                                    chain);
                            }
                            if (Close.GetValueAs(api, args))
                            {
                                var p1 = pos_list[count];
                                var p2 = pos_list[0];
                                api.ZoneAPI.UnitLaunchSpell(
                                    launcher: unit,
                                    sender: unit,
                                    launch: Spell,
                                    from: unit,
                                    startPos: p1,
                                    fromeSkillTemplateID: skill,
                                    targetUnit: null,
                                    targetPos: p2,
                                    faceDir: null,
                                    chain);
                            }
                        }
                    }
                    finally
                    {
                        //如果有Spell被释放出来，则这里会计数-1，如果没有Spell释放，则刚好销毁
                        chain?.Release();
                    }
                }
            }
            return unit;
        }
    }

    [Desc("触发的法术按次数时间间隔", "[游戏]/单位/法术&技能")]
    public class TriggingSpellTimeTaskAction : ZoneAbstractAction
    {
        [Desc("延时时间(秒)")] public AbstractValue<double> DelayTimeSEC = new RealValue.VALUE(5);
        [Desc("间隔时间(秒)")] public AbstractValue<double> EveryTimeSEC = new RealValue.VALUE(5);
        [Desc("重复次数")] public AbstractValue<double> RepeatCount = new IntegerValue.VALUE(0);
        [Desc("动作")] public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的法术延时{0}秒，每隔{1}秒，执行{2}次", DelayTimeSEC, EveryTimeSEC, RepeatCount);
            if (!Action.IsNullOrEmpty())
            {
                sw.AppendLine();
                sw.IndentBegin("{");
                sw.AppendLine(Action);
                sw.IndentEnd("}");
            }
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if(args.TriggingSpell is InstanceSpell spell)
            {
                spell.AddTimeTask<EventArguments>(
                    EveryTimeSEC.GetValueAs<float>(api, args) * 1000f,
                    DelayTimeSEC.GetValueAs<float>(api, args) * 1000f,
                    RepeatCount.GetValueAs<int>(api, args), args, (args2,t) =>
                    {
                        Action?.Invoke(api, args);
                    });
            }
            return null;
        }
    }


    [Desc("触发的法术增加时间", "[游戏]/单位/法术&技能")]
    public class TriggingSpellAddLifeTimeAction : ZoneAbstractAction
    {
        [Desc("时间(秒)")] public AbstractValue<double> TimeSEC = new RealValue.VALUE(5);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的法术增加时间{0}秒", TimeSEC);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell is InstanceSpell spell)
            {
                spell.SetPassTimeMS(spell.PassTimeMS + TimeSEC.GetValueAs(api,args) * 1000);
            }
            return null;
        }
    }


    [Desc("触发的法术改变速度", "[游戏]/单位/法术&技能")]
    public class TriggingSpellChangeSpeedAction : ZoneAbstractAction
    {
        [Desc("速度")] public AbstractValue<double> SpeedRate = new RealValue.VALUE(1);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发的法术改变速度{0}比率", SpeedRate);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (args.TriggingSpell is InstanceSpell spell)
            {
                spell.SetSpeedRate(SpeedRate.GetValueAs<float>(api, args));
            }
            return null;
        }
    }

}