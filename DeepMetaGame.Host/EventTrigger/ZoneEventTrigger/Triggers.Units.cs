using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //---------------------------------------------------------------------------------
    #region __某个单位__

    [Desc("某个单位进入场景", "[游戏]/单位-某个单位")]
    public class GenericUnitAdded : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位进入场景");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitAddedHandler handler = new InstanceZone.UnitAddedHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitAdded += handler,
                static (zone, handler) => zone.OnUnitAdded -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("某个单位离开场景（被删除）", "[游戏]/单位-某个单位")]
    public class GenericUnitRemoved : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位离开场景");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitRemovedHandler handler = new InstanceZone.UnitRemovedHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitRemoved += handler,
                static (zone, handler) => zone.OnUnitRemoved -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("某个单位首次激活", "[游戏]/单位-某个单位")]
    public class GenericUnitFirstActivated : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位首次激活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitActivatedHandler handler = new InstanceZone.UnitActivatedHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitFirstActivated += handler,
                static (zone, handler) => zone.OnUnitFirstActivated -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("某个单位被激活", "[游戏]/单位-某个单位")]
    public class GenericUnitActivated : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位被激活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitActivatedHandler handler = new InstanceZone.UnitActivatedHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitActivated += handler,
                static (zone, handler) => zone.OnUnitActivated -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("某个单位受到伤害", "[游戏]/单位-某个单位")]
    public class GenericUnitDamaged : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位受到伤害");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            var handler = new InstanceZone.UnitDamageHandler((InstanceZone z, InstanceUnit u, InstanceUnit attacker, long reduceHP, in TAttackSource attack, in TAttackResult result) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = attacker;
                args.TriggingAttack = attack;
                args.TriggingDamage = result;

                args.TriggingSpell = attack.FromSpellUnit;
                //args.TriggingChainInfo = attack.FromSpellUnit?.ChainInfo;

                args.TriggingSkillTemplate = attack.FromSkill;
                args.TriggingSpellTemplate = attack.FromSpell;
                args.TriggingBuffTemplate = attack.FromBuff;

                args.TriggingEquipBuff = attack.FromBuffState;
                args.TriggingEquipSkill = attack.FromSkillState;

                args.TriggingNumberValue = reduceHP;

                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitDamage += handler,
                static (zone, handler) => zone.OnUnitDamage -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
        [TriggingArg("扣除血量")] public double ReduceHP(EventArguments args) => args.TriggingNumberValue;
    }


    [Desc("某个单位中了Buff", "[游戏]/单位-某个单位")]
    public class GenericUnitGotBuff : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位中了Buff");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitGotBuffHandler handler = new InstanceZone.UnitGotBuffHandler((z, u, b) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = b.Sender;
                args.TriggingEquipBuff = b;
                args.TriggingBuffTemplate = b.Data;
                args.TriggingBuffSender = b.Sender;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitGotBuff += handler,
                static (zone, handler) => zone.OnUnitGotBuff -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("BUFF发送者")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("BUFF")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
    }

    [Desc("某个单位死亡", "[游戏]/单位-某个单位")]
    public class GenericUnitDead : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位死亡");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            // args = args.Clone();
            InstanceZone.UnitDeadHandler handler = new InstanceZone.UnitDeadHandler((z, u, attacker) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = attacker;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitDead += handler,
                static (zone, handler) => zone.OnUnitDead -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }

    [Desc("某个单位复活", "[游戏]/单位-某个单位")]
    public class GenericUnitRebirth : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位复活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitRebirthHandler handler = new InstanceZone.UnitRebirthHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitRebirth += handler,
                static (zone, handler) => zone.OnUnitRebirth -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("某个单位获取物品到背包", "[游戏]/单位-某个单位")]
    public class GenericUnitGotItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位获取物品到背包");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitGotInventoryItemHandler((z, u, i, c) =>
            {
                args.TriggingUnit = u;
                args.TriggingItemTemplate = i;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitGotInventoryItem += handler,
                static (zone, handler) => zone.OnUnitGotInventoryItem -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("某个单位失去物品从背包", "[游戏]/单位-某个单位")]
    public class GenericUnitLostItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位失去物品从背包");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitLostInventoryItemHandler handler = new InstanceZone.UnitLostInventoryItemHandler((z, u, i, c) =>
            {
                args.TriggingUnit = u;
                args.TriggingItemTemplate = i;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLostInventoryItem += handler,
                static (zone, handler) => zone.OnUnitLostInventoryItem -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("某个单位获取场景物品", "[游戏]/单位-某个单位")]
    public class GenericUnitGotIZoneItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位获取场景物品");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitGotInstanceItemHandler handler = new InstanceZone.UnitGotInstanceItemHandler((z, u, i) =>
            {
                args.TriggingUnit = u;
                args.TriggingItem = i;
                args.TriggingItemTemplate = i.Info;
                api.TestAndDoAction(args);
                return true;
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitGotInstanceItem += handler,
                static (zone, handler) => zone.OnUnitGotInstanceItem -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("某个单位使用物品", "[游戏]/单位-某个单位")]
    public class GenericUnitUseItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位使用物品");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitUseItemHandler handler = new InstanceZone.UnitUseItemHandler((z, u, i, c) =>
            {
                args.TriggingUnit = u;
                args.TriggingItemTemplate = i;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitUseItem += handler,
                static (zone, handler) => zone.OnUnitUseItem -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingItemTemplate(EventArguments args) => args.TriggingItemTemplate;
    }


    [Desc("某个单位点选其他单位", "[游戏]/单位-某个单位")]
    public class GenericUnitPickUnit : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位点选其他单位");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.UnitPickUnitHandler handler = new InstanceZone.UnitPickUnitHandler((z, u, p) =>
            {
                args.TriggingUnit = u;
                args.TriggingCounterPart = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitPickUnit += handler,
                static (zone, handler) => zone.OnUnitPickUnit -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("点选的单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }
    [Desc("某个玩家准备完毕", "[游戏]/单位-某个单位")]
    public class GenericPlayReady : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个玩家准备完毕");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.PlayerReadyHandler handler = new InstanceZone.PlayerReadyHandler((p, i) =>
            {
                args.TriggingUnit = p;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnPlayerReady += handler,
                static (zone, handler) => zone.OnPlayerReady -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }


    [Desc("某个单位释放光环", "[游戏]/单位-某个单位")]
    public class GenericUnitLaunchAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位释放光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitLaunchAuraHandler((z, u, a) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipAura = a;
                args.TriggingAuraTemplate = a.Data;
                args.TriggingAuraOwner = a.Owner;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLaunchAura += handler,
                static (zone, handler) => zone.OnUnitLaunchAura -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate AuraTemp(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("某个单位进入光环", "[游戏]/单位-某个单位")]
    public class GenericUnitEnterAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位进入光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitEnterAuraHandler((z, u, a) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipAura = a;
                args.TriggingAuraTemplate = a.Data;
                args.TriggingAuraOwner = a.Owner;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitEnterAura += handler,
                static (zone, handler) => zone.OnUnitEnterAura -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate AuraTemp(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("某个单位离开光环", "[游戏]/单位-某个单位")]
    public class GenericUnitLeaveAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位离开光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitLeaveAuraHandler((z, u, a) =>
            {
                args.TriggingUnit = u;
                args.TriggingEquipAura = a;
                args.TriggingAuraTemplate = a.Data;
                args.TriggingAuraOwner = a.Owner;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLeaveAura += handler,
                static (zone, handler) => zone.OnUnitLeaveAura -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate AuraTemp(EventArguments args) => args.TriggingAuraTemplate;
    }

    [Desc("某个单位升级", "[游戏]/单位-某个单位")]
    public class GenericUnitLevelUp : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位升级");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitLevelUpHandler((z, u) =>
            {
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLevelUp += handler,
                static (zone, handler) => zone.OnUnitLevelUp -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("某个单位传送", "[游戏]/单位-某个单位")]
    public class GenericUnitTransport : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当某个单位传送");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitTransportHandler((z, u, oldpos) =>
            {
                args.TriggingUnit = u;
                args.TriggingPositionValue = oldpos;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitTransport += handler,
                static (zone, handler) => zone.OnUnitTransport -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("传送的位置")] public Vector3? Pos(EventArguments args) => args.TriggingPositionValue;
    }


    [Desc("单位离开位面", "[游戏]/单位-某个单位")]
    public class UnitLeaveAOI : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位离开位面");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitLeaveAOIStatus((u, aoi) =>
            {
                args.TriggingUnit = u;
                args.TriggerAoiStatus = aoi;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitLeaveAOI += handler,
                static (zone, handler) => zone.OnUnitLeaveAOI -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("单位进入位面", "[游戏]/单位-某个单位")]
    public class UnitEnterAOI : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位进入位面");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitEnterAOIStatus((u, aoi) =>
            {
                args.TriggingUnit = u;
                args.TriggerAoiStatus = aoi;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitEnterAOI += handler,
                static (zone, handler) => zone.OnUnitEnterAOI -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("单位进入Flag地块", "[游戏]/单位-某个单位")]
    public class UnitSwapZoneInfoFlag : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位进入Flag地块");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.UnitSwapZoneInfoFlagHandler((z, o, oldF, newF) =>
            {
                if (o is InstanceUnit u)
                {
                    args.TriggingUnit = u;
                    args.TriggingZoneInfoFlag = newF;
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(api.ZoneAPI, handler,
                static (zone, handler) => zone.OnUnitSwapZoneInfoFlag += handler,
                static (zone, handler) => zone.OnUnitSwapZoneInfoFlag -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("地块值")] public double FlagValue(EventArguments args) => args.TriggingZoneInfoFlag;
    }

    #endregion
    //---------------------------------------------------------------------------------
    #region __指定单位__

    [Desc("指定单位首次激活", "[游戏]/[指定单位]")]
    public class SpecifyUnitFirstActivated : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})首次激活", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.ActivatedHandler handler = new InstanceUnit.ActivatedHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnFirstActivated += handler,
                    static (unit, handler) => unit.OnFirstActivated -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }


    [Desc("指定单位被激活", "[游戏]/[指定单位]")]
    public class SpecifyUnitActivated : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})被激活", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.ActivatedHandler handler = new InstanceUnit.ActivatedHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnActivated += handler,
                    static (unit, handler) => unit.OnActivated -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("指定单位攻击目标", "[游戏]/[指定单位]")]
    public class SpecifyUnitAttack : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})攻击目标", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.AttackHandler((InstanceUnit u, InstanceUnit a, long hp, in TAttackSource attack, in TAttackResult result) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    args.TriggingAttack = attack;
                    args.TriggingDamage = result;
                    args.TriggingSpell = attack.FromSpellUnit;
                    //args.TriggingChainInfo = attack.FromSpellUnit?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    args.TriggingNumberValue = hp;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnAttack += handler,
                    static (unit, handler) => unit.OnAttack -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
        [TriggingArg("扣除血量")] public double ReduceHP(EventArguments args) => args.TriggingNumberValue;
    }

    [Desc("指定单位受到伤害", "[游戏]/[指定单位]")]
    public class SpecifyUnitDamaged : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})受到伤害", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.DamageHandler((InstanceUnit u, InstanceUnit a, long hp, in TAttackSource attack, in TAttackResult result) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    args.TriggingAttack = attack;
                    args.TriggingDamage = result;
                    args.TriggingSpell = attack.FromSpellUnit;
                    //args.TriggingChainInfo = attack.FromSpellUnit?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    args.TriggingNumberValue = hp;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnDamage += handler,
                    static (unit, handler) => unit.OnDamage -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
        [TriggingArg("扣除血量")] public double ReduceHP(EventArguments args) => args.TriggingNumberValue;
    }

    [Desc("指定单位死亡", "[游戏]/[指定单位]")]
    public class SpecifyUnitDead : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})死亡", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.DeadHandler handler = new InstanceUnit.DeadHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnDead += handler,
                    static (unit, handler) => unit.OnDead -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }
    [Desc("指定单位杀人", "[游戏]/[指定单位]")]
    public class SpecifyUnitKill : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})杀人", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.KillHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnKill += handler,
                    static (unit, handler) => unit.OnKill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }

    [Desc("指定单位复活", "[游戏]/[指定单位]")]
    public class SpecifyUnitRebirth : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})复活", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.RebirthHandler handler = new InstanceUnit.RebirthHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnRebirth += handler,
                    static (unit, handler) => unit.OnRebirth -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("指定单位获取物品到背包", "[游戏]/[指定单位]")]
    public class SpecifyUnitGotItem : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})获取物品到背包", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.GotInventoryItemHandler handler = new InstanceUnit.GotInventoryItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnGotInventoryItem += handler,
                    static (unit, handler) => unit.OnGotInventoryItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingItemTemplate;
    }
    [Desc("指定单位失去物品从背包", "[游戏]/[指定单位]")]
    public class SpecifyUnitLostItem : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})失去物品从背包", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.LostInventoryItemHandler handler = new InstanceUnit.LostInventoryItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnLostInventoryItem += handler,
                    static (unit, handler) => unit.OnLostInventoryItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("指定单位使用物品", "[游戏]/[指定单位]")]
    public class SpecifyUnitUseItem : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})使用物品", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.UseItemHandler handler = new InstanceUnit.UseItemHandler((u, i, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnUseItem += handler,
                    static (unit, handler) => unit.OnUseItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("指定单位获取场景物品", "[游戏]/[指定单位]")]
    public class SpecifyUnitGotIZoneItem : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})获取场景物品", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.GotInstanceItemHandler handler = new InstanceUnit.GotInstanceItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItem = i;
                    args.TriggingItemTemplate = i.Info;
                    api.TestAndDoAction(args);
                    return true;
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnGotInstanceItem += handler,
                    static (unit, handler) => unit.OnGotInstanceItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板")] public ItemTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("指定单位中了BUFF", "[游戏]/[指定单位]")]
    public class SpecifyUnitGotBuff : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})中了BUFF", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                InstanceUnit.GotBuffHandler handler = new InstanceUnit.GotBuffHandler((u, b) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipBuff = b;
                    args.TriggingCounterPart = b.Sender;
                    args.TriggingBuffTemplate = b.Data;
                    args.TriggingBuffSender = b.Sender;

                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnGotBuff += handler,
                    static (unit, handler) => unit.OnGotBuff -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("BUFF实施者")] public InstanceUnit Sender(EventArguments args) => args.TriggingBuffSender;
        [TriggingArg("BUFF模板")] public BuffTemplate Temp(EventArguments args) => args.TriggingBuffTemplate;
    }


    [Desc("指定单位释放光环", "[游戏]/[指定单位]")]
    public class SpecifyUnitLaunchAura : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})释放光环", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.LaunchAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnLaunchAura += handler,
                    static (unit, handler) => unit.OnLaunchAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("指定单位进入光环", "[游戏]/[指定单位]")]
    public class SpecifyUnitEnterAura : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})进入光环", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.EnterAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnEnterAura += handler,
                    static (unit, handler) => unit.OnEnterAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("指定单位离开光环", "[游戏]/[指定单位]")]
    public class SpecifyUnitLeaveAura : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})离开光环", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.LeaveAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnLeaveAura += handler,
                    static (unit, handler) => unit.OnLeaveAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }

    [Desc("指定单位环境变量改变", "[游戏]/[指定单位]")]
    public class SpecifyUnitEnvironmentVarChange : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})环境变量改变", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new Action<InstanceUnit, string, object>((u, k, v) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnEnvironmentVarChangeHandler += handler,
                    static (unit, handler) => unit.OnEnvironmentVarChangeHandler -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("指定单位升级", "[游戏]/[指定单位]")]
    public class SpecifyUnitLevelUp : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}升级", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.UnitLevelUpHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnUnitLevelUp += handler,
                    static (unit, handler) => unit.OnUnitLevelUp -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("指定单位传送", "[游戏]/[指定单位]")]
    public class SpecifyUnitTransport : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当{0}传送", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.TransportHandler((u, oldp) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingPositionValue = oldp;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnTransport += handler,
                    static (unit, handler) => unit.OnTransport -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("位置")] public Vector3? Pos(EventArguments args) => args.TriggingPositionValue;
    }


    [Desc("指定单位行为入口", "[游戏]/[指定单位]")]
    public class SpecifyUnitDoSometing : ZoneAbstractTrigger
    {
        [Desc("[游戏]/单位-某个单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Editor();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})DoSomething", Unit);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                var handler = new InstanceUnit.DoSomethingHandler((u, h) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingBoolValue = h;
                    api.TestAndDoAction(args);
                    return u.NextState != null;
                });
                api.Listen(
                    () => unit.OnDoSomething += handler,
                    () => unit.OnDoSomething -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    #endregion
    //---------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------
    #region __绑定的单位__
    [Desc("绑定的单位首次激活", "[游戏]/[绑定的单位]")]
    public class BindingUnitFirstActivated : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)首次激活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.ActivatedHandler handler = new InstanceUnit.ActivatedHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnFirstActivated += handler,
                    static (unit, handler) => unit.OnFirstActivated -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("绑定的单位被激活", "[游戏]/[绑定的单位]")]
    public class BindingUnitActivated : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)被激活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.ActivatedHandler handler = new InstanceUnit.ActivatedHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnActivated += handler,
                    static (unit, handler) => unit.OnActivated -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("绑定的单位攻击目标", "[游戏]/[绑定的单位]")]
    public class BindingUnitAttack : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)攻击目标");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.AttackHandler((InstanceUnit u, InstanceUnit a, long hp, in TAttackSource attack, in TAttackResult result) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    args.TriggingAttack = attack;
                    args.TriggingDamage = result;
                    args.TriggingSpell = attack.FromSpellUnit;
                    //args.TriggingChainInfo = attack.FromSpellUnit?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    args.TriggingNumberValue = hp;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnAttack += handler,
                    static (unit, handler) => unit.OnAttack -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
        [TriggingArg("扣除血量")] public double ReduceHP(EventArguments args) => args.TriggingNumberValue;
    }

    [Desc("绑定的单位受到伤害", "[游戏]/[绑定的单位]")]
    public class BindingUnitDamaged : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)受到伤害");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.DamageHandler((InstanceUnit u, InstanceUnit a, long hp, in TAttackSource attack, in TAttackResult result) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    args.TriggingAttack = attack;
                    args.TriggingDamage = result;
                    args.TriggingSpell = attack.FromSpellUnit;
                    //args.TriggingChainInfo = attack.FromSpellUnit?.ChainInfo;
                    args.TriggingSkillTemplate = attack.FromSkill;
                    args.TriggingSpellTemplate = attack.FromSpell;
                    args.TriggingBuffTemplate = attack.FromBuff;
                    args.TriggingEquipBuff = attack.FromBuffState;
                    args.TriggingEquipSkill = attack.FromSkillState;
                    args.TriggingNumberValue = hp;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnDamage += handler,
                    static (unit, handler) => unit.OnDamage -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
        [TriggingArg("攻击技能模板?")] public SkillTemplate TriggingSkillTemplate(EventArguments args) => args.TriggingSkillTemplate;
        [TriggingArg("攻击法术模板?")] public SpellTemplate TriggingSpellTemplate(EventArguments args) => args.TriggingSpellTemplate;
        [TriggingArg("攻击BUFF模板?")] public BuffTemplate TriggingBuffTemplate(EventArguments args) => args.TriggingBuffTemplate;
        [TriggingArg("扣除血量")] public double ReduceHP(EventArguments args) => args.TriggingNumberValue;
    }




    [Desc("绑定的单位死亡", "[游戏]/[绑定的单位]")]
    public class BindingUnitDead : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)死亡");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.DeadHandler handler = new InstanceUnit.DeadHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnDead += handler,
                    static (unit, handler) => unit.OnDead -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("攻击者单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }

    [Desc("绑定的单位杀人", "[游戏]/[绑定的单位]")]
    public class BindingUnitKill : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)杀人");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.KillHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = a;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnKill += handler,
                    static (unit, handler) => unit.OnKill -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("被攻击单位")] public InstanceUnit TriggingCounterPart(EventArguments args) => args.TriggingCounterPart;
    }

    [Desc("绑定的单位复活", "[游戏]/[绑定的单位]")]
    public class BindingUnitRebirth : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)复活");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.RebirthHandler handler = new InstanceUnit.RebirthHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnRebirth += handler,
                    static (unit, handler) => unit.OnRebirth -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    [Desc("绑定的单位获取物品到背包", "[游戏]/[绑定的单位]")]
    public class BindingUnitGotItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)获取物品到背包");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.GotInventoryItemHandler handler = new InstanceUnit.GotInventoryItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnGotInventoryItem += handler,
                    static (unit, handler) => unit.OnGotInventoryItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板?")] public ItemTemplate Item(EventArguments args) => args.TriggingItemTemplate;
    }
    [Desc("绑定的单位失去物品从背包", "[游戏]/[绑定的单位]")]
    public class BindingUnitLostItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)失去物品从背包");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.LostInventoryItemHandler handler = new InstanceUnit.LostInventoryItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnLostInventoryItem += handler,
                    static (unit, handler) => unit.OnLostInventoryItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板?")] public ItemTemplate Item(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("绑定的单位使用物品", "[游戏]/[绑定的单位]")]
    public class BindingUnitUseItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)使用物品");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.UseItemHandler handler = new InstanceUnit.UseItemHandler((u, i, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItemTemplate = i;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnUseItem += handler,
                    static (unit, handler) => unit.OnUseItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板?")] public ItemTemplate Item(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("绑定的单位获取场景物品", "[游戏]/[绑定的单位]")]
    public class BindingUnitGotIZoneItem : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)获取场景物品");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.GotInstanceItemHandler handler = new InstanceUnit.GotInstanceItemHandler((u, i) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingItem = i;
                    args.TriggingItemTemplate = i.Info;
                    api.TestAndDoAction(args);
                    return true;
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnGotInstanceItem += handler,
                    static (unit, handler) => unit.OnGotInstanceItem -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("物品模板?")] public ItemTemplate Item(EventArguments args) => args.TriggingItemTemplate;
    }

    [Desc("绑定的单位中了BUFF", "[游戏]/[绑定的单位]")]
    public class BindingUnitGotBuff : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)中了BUFF");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.GotBuffHandler handler = new InstanceUnit.GotBuffHandler((u, b) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCounterPart = b.Sender;
                    args.TriggingEquipBuff = b;
                    args.TriggingBuffTemplate = b.Data;
                    args.TriggingBuffSender = b.Sender;

                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnGotBuff += handler,
                    static (unit, handler) => unit.OnGotBuff -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("BUFF实施者")] public InstanceUnit Sender(EventArguments args) => args.TriggingBuffSender;
        [TriggingArg("BUFF模板")] public BuffTemplate Buff(EventArguments args) => args.TriggingBuffTemplate;
    }

    [Desc("绑定的单位移除BUFF", "[游戏]/[绑定的单位]")]
    public class BindingUnitRemoveBuff : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)移除BUFF");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                InstanceUnit.RemoveBuffHandler handler = new InstanceUnit.RemoveBuffHandler((u, b) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipBuff = b;
                    args.TriggingBuffTemplate = b.Data;
                    args.TriggingBuffSender = b.Sender;
                    args.TriggingEquipSkill = b.FromSkillID;
                    args.TriggingSkillTemplate = b.FromSkillID?.Data;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnRemoveBuff += handler,
                    static (unit, handler) => unit.OnRemoveBuff -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("BUFF实施者")] public InstanceUnit Sender(EventArguments args) => args.TriggingBuffSender;
        [TriggingArg("BUFF模板")] public BuffTemplate Buff(EventArguments args) => args.TriggingBuffTemplate;
    }

    [Desc("绑定的单位释放光环", "[游戏]/[绑定的单位]")]
    public class BindingUnitLaunchAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)释放光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.LaunchAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnLaunchAura += handler,
                    static (unit, handler) => unit.OnLaunchAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("绑定的单位进入光环", "[游戏]/[绑定的单位]")]
    public class BindingUnitEnterAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)进入光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.EnterAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnEnterAura += handler,
                    static (unit, handler) => unit.OnEnterAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("绑定的单位离开光环", "[游戏]/[绑定的单位]")]
    public class BindingUnitLeaveAura : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)离开光环");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var handler = new InstanceUnit.LeaveAuraHandler((u, a) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingEquipAura = a;
                    args.TriggingAuraTemplate = a.Data;
                    args.TriggingAuraOwner = a.Owner;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                    static (unit, handler) => unit.OnLeaveAura += handler,
                    static (unit, handler) => unit.OnLeaveAura -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("光环模板")] public AuraTemplate Aura(EventArguments args) => args.TriggingAuraTemplate;
    }
    [Desc("绑定的单位升级", "[游戏]/[绑定的单位]")]
    public class BindingUnitLevelUp : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = api.UnitAPI;
            if (unit != null)
            {
                var handler = new InstanceUnit.UnitLevelUpHandler((u) =>
                {
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(api.UnitAPI, handler,
                      static (unit, handler) => unit.OnUnitLevelUp += handler,
                      static (unit, handler) => unit.OnUnitLevelUp -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("绑定的单位传送", "[游戏]/[绑定的单位]")]
    public class BindingUnitTransport : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = api.UnitAPI;
            if (unit != null)
            {
                var handler = new InstanceUnit.TransportHandler((u, oldp) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingPositionValue = oldp;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnTransport += handler,
                    static (unit, handler) => unit.OnTransport -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("位置")] public Vector3? Pos(EventArguments args) => args.TriggingPositionValue;
    }





    [Desc("绑定的单位获得词缀", "[游戏]/[绑定的单位]")]
    public class BindingUnitTryGetCard : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("绑定的单位获得词缀");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = api.UnitAPI;
            if (unit != null)
            {
                var handler = new InstanceUnit.CardAddHandler((u, cartridge, slot) =>
                {
                    args.TriggingCardTemplate = slot.Card;
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (t, d) => t.OnCardAdd += d,
                    static (t, d) => t.OnCardAdd -= d);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("词缀模板")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    }


    //     [Desc("绑定的单位移除词缀", "[游戏]/[绑定的单位]")]
    //     public class BindingUnitRemoveCard : ZoneAbstractTrigger
    //     {
    //         protected override void Listen(IEventTriggerAdapter api, EventArguments args)
    //         {
    //             var unit = api.UnitAPI;
    //             if (unit != null)
    //             {
    //                 var handler = new InstanceUnit.CardAddHandler((u, cartridge, slot) =>
    //                 {
    //                     args.TriggingCardTemplate = slot.Card;
    //                     args.TriggingUnit = u;
    //                     api.TestAndDoAction(args);
    //                 });
    //                 api.Listen(unit, handler,
    //                     static (t, d) => t.OnCardAdd += d,
    //                     static (t, d) => t.OnCardAdd -= d);
    //             }
    //         }
    //         [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    //         [TriggingArg("词缀模板")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    //     }




    [Desc("绑定的单位行为入口", "[游戏]/[绑定的单位]")]
    public class BindingUnitDoSometing : ZoneAbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当(绑定的单位)DoSomething");
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var unit = api.UnitAPI;
                var handler = new InstanceUnit.DoSomethingHandler((u, h) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingBoolValue = h;
                    api.TestAndDoAction(args);
                    return u.NextState != null;
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnDoSomething += handler,
                    static (unit, handler) => unit.OnDoSomething -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }

    #endregion
    //---------------------------------------------------------------------------------





}
