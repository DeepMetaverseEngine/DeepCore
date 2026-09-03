using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //-----------------------------------------------------------------------------------------------
    #region 单位
    [Desc("循环迭代中的单位", "[游戏]/循环迭代")]
    public class IteratingUnit : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.IteratingObject as InstanceUnit;
        }
    }
    [Desc("统计单位数", "[游戏]/循环迭代")]
    public class SumUnits : ZoneIntegerValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("统计({0})单位数;", Condition);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            using (var st = api.ZoneAPI.ObjectPool.AllocRef((0, api, args, Condition)))
            {
                api.ZoneAPI.ForEachUnits(st, static (r, u) =>
                {
                    var st = r.Value;
                    st.args.IteratingObject = (u);
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        st.Item1++;
                    }
                    st.args.IteratingObject = (null);
                });
                return st.Value.Item1;
            }
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    [Desc("查找单位", "[游戏]/循环迭代")]
    public class FindUnit : UnitValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("查找({0})单位;", Condition);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.ForEachUnits((api, args, Condition), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                try
                {
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        return true;
                    }
                }
                finally
                {
                    st.args.IteratingObject = (null);
                }
                return false;
            });
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    [Desc("遍历所有单位", "[游戏]/循环迭代")]
    public class ForEachUnitsAction : ZoneAbstractAction
    {
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("遍历所有单位"),
                sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachUnits((api, args, Action), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                st.Action.Invoke(st.api, st.args);
                st.args.IteratingObject = (null);
            });
            return null;
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }
    #endregion
    //-----------------------------------------------------------------------------------------------
    #region 物品
    [Desc("循环迭代中的物品", "[游戏]/循环迭代")]
    public class IteratingItem : ItemValue
    {
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.IteratingObject as InstanceItem;
        }
    }
    [Desc("统计物品数", "[游戏]/循环迭代")]
    public class SumItems : ZoneIntegerValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("统计({0})物品数;", Condition);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            using (var st = api.ZoneAPI.ObjectPool.AllocRef((0, api, args, Condition)))
            {
                api.ZoneAPI.ForEachItems(st, static (r, u) =>
                {
                    var st = r.Value;
                    st.args.IteratingObject = (u);
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        st.Item1++;
                    }
                    st.args.IteratingObject = (null);
                });
                return st.Value.Item1;
            }
        }
        [TriggingArg("迭代中的物品")] public InstanceItem Iterating(EventArguments args) => args.IteratingObject as InstanceItem;
    }
    [Desc("查找物品", "[游戏]/循环迭代")]
    public class FindItem : ItemValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("查找({0})物品;", Condition);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.ForEachItems((api, args, Condition), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                try
                {
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        return true;
                    }
                }
                finally
                {
                    st.args.IteratingObject = (null);
                }               
                return false;
            });
        }
        [TriggingArg("迭代中的物品")] public InstanceItem Iterating(EventArguments args) => args.IteratingObject as InstanceItem;
    }
    [Desc("遍历所有物品", "[游戏]/循环迭代")]
    public class ForEachItemsAction : ZoneAbstractAction
    {
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("遍历所有物品"),
                sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachItems((api, args, Action), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                st.Action.Invoke(st.api, st.args);
                st.args.IteratingObject = (null);
            });
            return null;
        }
        [TriggingArg("迭代中的物品")] public InstanceItem Iterating(EventArguments args) => args.IteratingObject as InstanceItem;
    }
    #endregion
    //-----------------------------------------------------------------------------------------------
    #region Flag
    [Desc("循环迭代中的Flag", "[游戏]/循环迭代")]
    public class IteratingFlag : FlagValue
    {
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.IteratingObject as InstanceFlag;
        }
    }
    [Desc("统计Flags数", "[游戏]/循环迭代")]
    public class SumFlags : ZoneIntegerValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("统计({0})Flags数;", Condition);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            using (var st = api.ZoneAPI.ObjectPool.AllocRef((0, api, args, Condition)))
            {
                api.ZoneAPI.ForEachFlags(st, static (r, u) =>
                {
                    var st = r.Value;
                    st.args.IteratingObject = (u);
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        st.Item1++;
                    }
                    st.args.IteratingObject = (null);
                });
                return st.Value.Item1;
            }
        }
        [TriggingArg("迭代中的Flag")] public InstanceFlag Iterating(EventArguments args) => args.IteratingObject as InstanceFlag;
    }
    [Desc("查找Flag", "[游戏]/循环迭代")]
    public class FindFlag : FlagValue
    {
        [Desc("条件")]
        public AbstractValue<bool> Condition = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("查找({0})Flag;", Condition);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.ForEachFlags((api, args, Condition), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                try
                {
                    if (st.Condition.GetValueAs(st.api, st.args))
                    {
                        return true;
                    }
                }
                finally
                {
                    st.args.IteratingObject = (null);
                }
                return false;
            });
        }
        [TriggingArg("迭代中的Flag")] public InstanceFlag IT(EventArguments args) => args.IteratingObject as InstanceFlag;
    }
    [Desc("遍历所有Flag", "[游戏]/循环迭代")]
    public class ForEachFlagsAction : ZoneAbstractAction
    {
        [Desc("动作")]
        public AbstractAction Action = new DoNoting();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("遍历所有Flag"),
                sw2 => sw.AppendLine(Action));
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ForEachFlags((api, args, Action), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                st.Action.Invoke(st.api, st.args);
                st.args.IteratingObject = (null);
            });
            return null;
        }
        [TriggingArg("迭代中的Flag")] public InstanceFlag Iterating(EventArguments args) => args.IteratingObject as InstanceFlag;
    }
    #endregion
    //-----------------------------------------------------------------------------------------------
}
