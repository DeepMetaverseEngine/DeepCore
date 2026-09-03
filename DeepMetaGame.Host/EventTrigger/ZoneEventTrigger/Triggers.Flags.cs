using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance.Abilities;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("Flag开启(包括空气墙)", "[游戏]/Flag")]
    public class FlagOpenedTrigger : ZoneAbstractTrigger
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})开启时", Flag);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceFlag flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                InstanceFlag.FlagEnabledHandler handler = new InstanceFlag.FlagEnabledHandler((f) =>
                {
                    args.TriggingFlag = f;
                    api.TestAndDoAction(args);
                });
                api.Listen(flag, handler,
                    static (flag, handler) => flag.OnFlagEnabled += handler,
                    static (flag, handler) => flag.OnFlagEnabled -= handler);
            }
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }

    [Desc("Flag关闭(包括空气墙)", "[游戏]/Flag")]
    public class FlagClosedTrigger : ZoneAbstractTrigger
    {
        [Desc("Flag")]
        public AbstractValue<InstanceFlag> Flag = new FlagValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})关闭时", Flag);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceFlag flag = Flag.GetValueAs(api, args);
            if (flag != null)
            {
                InstanceFlag.FlagDisabledHandler handler = new InstanceFlag.FlagDisabledHandler((f) =>
                {
                    args.TriggingFlag = flag;
                    api.TestAndDoAction(args);
                });
                api.Listen(flag, handler,
                    static (flag, handler) => flag.OnFlagDisabled += handler,
                    static (flag, handler) => flag.OnFlagDisabled -= handler);
            }
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    //-------------------------------------------------------------------------------------------
    #region __区域事件__



    [Desc("单位进入区域", "[游戏]/区域")]
    public class UnitEnterRegion : ZoneAbstractTrigger
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位进入({0})", Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                ZoneRegion.UnitEnterHandler handler = new ZoneRegion.UnitEnterHandler((rg, u) =>
                {
                    args.TriggingFlag = rg;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(region, handler,
                    static (region, handler) => region.OnUnitEnter += handler,
                    static (region, handler) => region.OnUnitEnter -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("单位离开区域", "[游戏]/区域")]
    public class UnitLeaveRegion : ZoneAbstractTrigger
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位离开({0})", Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                ZoneRegion.UnitLeaveHandler handler = new ZoneRegion.UnitLeaveHandler((rg, u) =>
                {
                    args.TriggingFlag = rg;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(region, handler,
                    static (region, handler) => region.OnUnitLeave += handler,
                    static (region, handler) => region.OnUnitLeave -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }

    [Desc("区域刷新单位结束(刷新单位全部死亡)", "[游戏]/区域-刷新点")]
    public class SpawnUnitRegionIsDone : ZoneAbstractTrigger
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})单位刷新结束(刷新单位全部死亡)时", Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                // // args = args.Clone();
                var handler = new Action<ISpawnContainer, AbstractSpawnAbility, InstanceZoneObject>((rg, t, u) =>
                {
                    args.TriggingFlag = rg as ZoneRegion;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(region, handler,
                    static (region, handler) => region.SpawnCollection.OnSpawnOver += handler,
                    static (region, handler) => region.SpawnCollection.OnSpawnOver -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("区域已刷新单位", "[游戏]/区域-刷新点")]
    public class SpawnUnitRegionSpawned : ZoneAbstractTrigger
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当({0})区域已刷新单位时", Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                // // args = args.Clone();
                var handler = new Action<ISpawnContainer, AbstractSpawnAbility, InstanceZoneObject>((rg, t, u) =>
                {
                    args.TriggingFlag = rg as ZoneRegion;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(region, handler,
                    static (region, handler) => region.SpawnCollection.OnObjectSpawned += handler,
                    static (region, handler) => region.SpawnCollection.OnObjectSpawned -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }


    [Desc("指定单位进入区域一次", "[游戏]/区域")]
    public class UnitEnterRegionOnce : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})进入({1})一次", Unit, Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (region != null && unit != null)
            {
                //args = args.Clone();
                region.ListenUnitEnterOnce(unit, (r, u) =>
                {
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("指定单位离开区域一次", "[游戏]/区域")]
    public class UnitLeaveRegionOnce : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})离开({1})一次", Unit, Region);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (region != null && unit != null)
            {
                //args = args.Clone();
                region.ListenUnitLeaveOnce(unit, (r, u) =>
                {
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }



    #endregion
    //-------------------------------------------------------------------------------------------
    #region __Area事件__


    [Desc("单位进入Area", "[游戏]/Area")]
    public class UnitEnterArea : ZoneAbstractTrigger
    {
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位进入({0})", Area);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var area = Area.GetValueAs(api, args) as ZoneArea;
            if (area != null)
            {
                var handler = new ZoneArea.UnitEnterHandler((rg, u) =>
                {
                    args.TriggingFlag = rg;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(area, handler,
                    static (area, handler) => area.OnUnitEnter += handler,
                    static (area, handler) => area.OnUnitEnter -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("单位离开Area", "[游戏]/Area")]
    public class UnitLeaveArea : ZoneAbstractTrigger
    {
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当单位离开({0})", Area);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneArea region = Area.GetValueAs(api, args) as ZoneArea;
            if (region != null)
            {
                // args = args.Clone();
                ZoneArea.UnitLeaveHandler handler = new ZoneArea.UnitLeaveHandler((rg, u) =>
                {
                    args.TriggingFlag = rg;
                    args.TriggingUnit = u as InstanceUnit;
                    api.TestAndDoAction(args);
                });
                api.Listen(region, handler,
                    static (region, handler) => region.OnUnitLeave += handler,
                    static (region, handler) => region.OnUnitLeave -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }

    [Desc("指定单位进入Area一次", "[游戏]/Area")]
    public class UnitEnterAreaOnce : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})进入({1})一次", Unit, Area);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneArea area = Area.GetValueAs(api, args) as ZoneArea;
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (area != null && unit != null)
            {
                //args = args.Clone();
                area.ListenUnitEnterOnce(unit, (r, u) =>
                {
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("指定单位离开Area一次", "[游戏]/Area")]
    public class UnitLeaveAreaOnce : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("Area")]
        public AbstractValue<InstanceFlag> Area = new FlagValue.EditorArea();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})离开({1})一次", Unit, Area);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneArea area = Area.GetValueAs(api, args) as ZoneArea;
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (area != null && unit != null)
            {
                //args = args.Clone();
                area.ListenUnitLeaveOnce(unit, (r, u) =>
                {
                    args.TriggingFlag = r;
                    args.TriggingUnit = u;
                    api.TestAndDoAction(args);
                });
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }

    #endregion
    //-------------------------------------------------------------------------------------------

    #region 任何区域

    [Desc("某个Flag开启", "[游戏]/某个Flag")]
    public class AnyFlagOnTrigger : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.OnFlagEnableDelegate handler = new((z, f) =>
            {
                args.TriggingFlag = f;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnFlagOn += handler,
                static (z, handler) => z.OnFlagOn -= handler);
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("某个Flag关闭", "[游戏]/某个Flag")]
    public class AnyFlagOffTrigger : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.OnFlagDisableDelegate handler = new((z, f) =>
            {
                args.TriggingFlag = f;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnFlagOff += handler,
                static (z, handler) => z.OnFlagOff -= handler);
        }
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }

    [Desc("某个区域已刷新单位", "[游戏]/某个Flag")]
    public class AnySpawnUnitRegionIsDone : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.OnFlagSpawnObjectDelegate((z, rg, t, u) =>
            {
                args.TriggingFlag = rg as InstanceFlag;
                args.TriggingUnit = u as InstanceUnit;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnFlagSpawnObject += handler,
                static (z, handler) => z.OnFlagSpawnObject -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("某个区域刷新单位结束", "[游戏]/某个Flag")]
    public class AnySpawnUnitRegionSpawned : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new InstanceZone.OnFlagSpawnOverDelegate((z, rg, t, u) =>
            {
                args.TriggingFlag = rg as InstanceFlag;
                args.TriggingUnit = u as InstanceUnit;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnFlagSpawnOver += handler,
                static (z, handler) => z.OnFlagSpawnOver -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }


    [Desc("单位经过某个路点", "[游戏]/某个Flag")]
    public class AnyUnitPassPointTrigger : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.OnUnitPassPointDelegate handler = new((z, u, p, n) =>
            {
                args.TriggingUnit = u;
                args.TriggingFlag = p;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnUnitPassPoint += handler,
                static (z, handler) => z.OnUnitPassPoint -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("单位停在某个路点", "[游戏]/某个Flag")]
    public class AnyUnitHoldOnPointTrigger : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceZone.OnUnitHoldPointDelegate handler = new((z, u, p, n) =>
            {
                args.TriggingUnit = u;
                args.TriggingFlag = p;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                static (z, handler) => z.OnUnitHoldPoint += handler,
                static (z, handler) => z.OnUnitHoldPoint -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }



    [Desc("单位进入某个区域", "[游戏]/某个Flag")]
    public class AnyUnitEnterRegion : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnUnitEnterRegionDelegate((z, u, rg) =>
            {
                args.TriggingFlag = rg;
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                    static (region, handler) => region.OnUnitEnterRegion += handler,
                    static (region, handler) => region.OnUnitEnterRegion -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("单位离开某个区域", "[游戏]/区域")]
    public class AnyUnitLeaveRegion : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnUnitLeaveRegionDelegate((z, u, rg) =>
            {
                args.TriggingFlag = rg;
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                    static (region, handler) => region.OnUnitLeaveRegion += handler,
                    static (region, handler) => region.OnUnitLeaveRegion -= handler);

        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }


    [Desc("单位进入某个Area", "[游戏]/某个Flag")]
    public class AnyUnitEnterArea : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnUnitEnterAreaDelegate((z, u, rg) =>
            {
                args.TriggingFlag = rg;
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                    static (region, handler) => region.OnUnitEnterArea += handler,
                    static (region, handler) => region.OnUnitEnterArea -= handler);
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }
    [Desc("单位离开某个Area", "[游戏]/区域")]
    public class AnyUnitLeaveArea : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnUnitLeaveAreaDelegate((z, u, rg) =>
            {
                args.TriggingFlag = rg;
                args.TriggingUnit = u;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler,
                    static (region, handler) => region.OnUnitLeaveArea += handler,
                    static (region, handler) => region.OnUnitLeaveArea -= handler);

        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("触发的Flag")] public InstanceFlag TriggingFlag(EventArguments args) => args.TriggingFlag;
    }


    #endregion
}
