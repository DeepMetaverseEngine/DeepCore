using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using System;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{


    [Desc("刷新点 - 当前存活单位数量", "[游戏]/区域-刷新点")]
    public class SpawnTriggerAliveCount : ZoneIntegerValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]当前存活单位数量", Region);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    //                     int ret = 0;
                    //                     foreach (var spawn in region.GetSpawnTriggers())
                    //                     {
                    //                         ret += spawn.AliveCount;
                    //                     }
                    //                     return ret;
                    return region.SpawnCollection.SpawnAliveCount;
                }
            }
            return 0;
        }
    }

    [Desc("刷新点 - 总共生成单位数量", "[游戏]/区域-刷新点")]
    public class SpawnTriggerTotalSpawnCount : ZoneIntegerValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]总共生成单位数量", Region);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    //                     int ret = 0;
                    //                     foreach (var spawn in region.GetSpawnTriggers())
                    //                     {
                    //                         ret += spawn.TotalSpawnCount;
                    //                     }
                    //                     return ret;
                    return region.SpawnCollection.TotalSpawnCount;
                }
            }
            return 0;
        }
    }


    [Desc("刷新点 - 单次生成单位数量", "[游戏]/区域-刷新点")]
    public class SpawnTriggerOnceSpawnCount : ZoneIntegerValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]单次生成单位数量", Region);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    //                     int ret = 0;
                    //                     foreach (var spawn in region.GetSpawnTriggers())
                    //                     {
                    //                         ret += spawn.OnceSpawnCount;
                    //                     }
                    //                     return ret;
                    return region.SpawnCollection.SpawnOnceCount;
                }
            }
            return 0;
        }
    }

    [Desc("刷新点 - 总共生成单位上限", "[游戏]/区域-刷新点")]
    public class SpawnTriggerLimitedSpawnCount : ZoneIntegerValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]总共生成单位上限", Region);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    return region.SpawnCollection.SpawnLimitedCount;
//                     int ret = 0;
//                     foreach (var spawn in region.GetSpawnTriggers())
//                     {
//                         ret += spawn.LimitedSpawnCount;
//                     }
//                     return ret;
                }
            }
            return 0;
        }
    }

    [Desc("刷新点 - 存活数量上限(不死完不刷新)", "[游戏]/区域-刷新点")]
    public class SpawnTriggerLimitedAliveCount : ZoneIntegerValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]存活数量上限", Region);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    return region.SpawnCollection.SpawnAliveLimitedCount;
//                     int ret = 0;
//                     foreach (var spawn in region.GetSpawnTriggers())
//                     {
//                         ret += spawn.LimitedAliveCount;
//                     }
//                     return ret;
                }
            }
            return 0;
        }
    }

    [Desc("刷新点 - 是否已完成刷新", "[游戏]/区域-刷新点")]
    public class SpawnTriggerIsSpawnOver : ZoneBooleanValue
    {
        [Desc("区域")]
        public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("刷新点[{0}]是否已完成刷新", Region);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            IEventTriggerAdapter evtapi = api as IEventTriggerAdapter;
            if (evtapi != null)
            {
                ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
                if (region != null)
                {
                    return region.SpawnCollection.IsSpawnOver;
//                     foreach (var spawn in region.GetSpawnTriggers())
//                     {
//                         if (!spawn.IsSpawnOver)
//                         {
//                             return false;
//                         }
//                     }
//                     return true;
                }
            }
            return false;
        }
    }


}