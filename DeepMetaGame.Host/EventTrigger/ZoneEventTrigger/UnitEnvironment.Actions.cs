using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;
using DeepCore.Reflection;
using System;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //-------------------------------------------------------------------------------------------
    #region _设置_单位变量_

    [Desc("", "[游戏]/单位/设置变量")]
    public abstract class SetUnitVar<T> : ZoneAbstractAction
    {
        [Desc("变量名")]
        public string Key = "VarName";

        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("是否同步到客户端（如果需要客户端显示，则填True）")]
        public bool SyncToClient = true;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({2})设置变量\"{0}\"={1};", Key, TValue, Unit);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                T value = TValue.GetValueAs(api, args);
                unit.SetEnvironmentVar(Key, value, SyncToClient);
            }
            return null;
        }
        abstract public DeepCore.EventTrigger.Data.AbstractValue<T> TValue { get; }
    }

    [Desc("设置字符串型", "[游戏]/单位/设置变量")]
    public class SetUnitString : SetUnitVar<string>
    {
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public DeepCore.EventTrigger.Data.AbstractValue<string> TValue { get { return Value; } }
    }

    [Desc("设置整数型", "[游戏]/单位/设置变量")]
    public class SetUnitInteger : SetUnitVar<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置小数型", "[游戏]/单位/设置变量")]
    public class SetUnitReal : SetUnitVar<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置布尔型", "[游戏]/单位/设置变量")]
    public class SetUnitBool : SetUnitVar<Boolean>
    {
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Boolean> TValue { get { return Value; } }
    }

    [Desc("设置单位", "[游戏]/单位/设置变量")]
    public class SetUnitUnit : SetUnitVar<InstanceUnit>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceUnit> TValue { get { return Value; } }
    }

    [Desc("设置Flag", "[游戏]/单位/设置变量")]
    public class SetUnitFlag : SetUnitVar<InstanceFlag>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceFlag> TValue { get { return Value; } }
    }

    [Desc("设置物品", "[游戏]/单位/设置变量")]
    public class SetUnitItem : SetUnitVar<InstanceItem>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceItem> TValue { get { return Value; } }
    }

    [Desc("设置位置坐标", "[游戏]/单位/设置变量")]
    public class SetUnitPosition : SetUnitVar<Vector3?>
    {
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Vector3?> TValue { get { return Value; } }
    }

    #endregion
    //-------------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------------
    #region _设置_单位变量_StringValueKey_

    [Desc("", "[游戏]/单位/设置变量(变量索引)")]
    public abstract class SetUnitVarStringValueKey<T> : ZoneAbstractAction
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");

        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("是否同步到客户端（如果需要客户端显示，则填True）")]
        public bool SyncToClient = true;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({2})设置变量\"{0}\"={1};", Key, TValue, Unit);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                T value = TValue.GetValueAs(api, args);
                unit.SetEnvironmentVar(key, value, SyncToClient);
            }
            return null;
        }
        abstract public DeepCore.EventTrigger.Data.AbstractValue<T> TValue { get; }
    }

    [Desc("设置字符串型", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitStringStringValueKey : SetUnitVarStringValueKey<string>
    {
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public DeepCore.EventTrigger.Data.AbstractValue<string> TValue { get { return Value; } }
    }

    [Desc("设置整数型", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitIntegerStringValueKey : SetUnitVarStringValueKey<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置小数型", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitRealStringValueKey : SetUnitVarStringValueKey<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置布尔型", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitBoolStringValueKey : SetUnitVarStringValueKey<Boolean>
    {
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Boolean> TValue { get { return Value; } }
    }

    [Desc("设置单位", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitUnitStringValueKey : SetUnitVarStringValueKey<InstanceUnit>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceUnit> TValue { get { return Value; } }
    }

    [Desc("设置Flag", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitFlagStringValueKey : SetUnitVarStringValueKey<InstanceFlag>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceFlag> TValue { get { return Value; } }
    }

    [Desc("设置物品", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitItemStringValueKey : SetUnitVarStringValueKey<InstanceItem>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceItem> TValue { get { return Value; } }
    }

    [Desc("设置位置坐标", "[游戏]/单位/设置变量(变量索引)")]
    public class SetUnitPositionStringValueKey : SetUnitVarStringValueKey<Vector3?>
    {
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Vector3?> TValue { get { return Value; } }
    }

    #endregion
    //-------------------------------------------------------------------------------------------


    #region _设置_单位变量_StringValueKey_

    [Desc("", "[游戏]/单位/设置玩家变量(变量索引)")]
    public abstract class SetPlayerVarStringValueKey<T> : ZoneAbstractAction
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");

        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();

        [Desc("是否同步到客户端（如果需要客户端显示，则填True）")]
        public bool SyncToClient = true;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({2})设置变量\"{0}\"={1};", Key, TValue, Player);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs(api, args);
            var key = Key.GetValueAs(api, args);
            if (unit is InstancePlayer player && key != null)
            {
                T value = TValue.GetValueAs(api, args);
                player.SetPlayerEnvironmentVar(key, value, SyncToClient);
            }
            return null;
        }
        abstract public DeepCore.EventTrigger.Data.AbstractValue<T> TValue { get; }



    }



    [Desc("设置字符串型", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetString : SetPlayerVarStringValueKey<string>
    {
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public DeepCore.EventTrigger.Data.AbstractValue<string> TValue { get { return Value; } }
    }

    [Desc("设置整数型", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetInteger : SetPlayerVarStringValueKey<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置小数型", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetReal : SetPlayerVarStringValueKey<double>
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<double> TValue { get { return Value; } }
    }

    [Desc("设置布尔型", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetBool : SetPlayerVarStringValueKey<Boolean>
    {
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Boolean> TValue { get { return Value; } }
    }

    [Desc("设置单位", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetUnit : SetPlayerVarStringValueKey<InstanceUnit>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceUnit> TValue { get { return Value; } }
    }

    [Desc("设置Flag", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetFlag : SetPlayerVarStringValueKey<InstanceFlag>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceFlag> TValue { get { return Value; } }
    }

    [Desc("设置物品", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetItem : SetPlayerVarStringValueKey<InstanceItem>
    {
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        override public DeepCore.EventTrigger.Data.AbstractValue<InstanceItem> TValue { get { return Value; } }
    }

    [Desc("设置位置坐标", "[游戏]/单位/设置玩家变量(变量索引)")]
    public class PlayerSetPosition : SetPlayerVarStringValueKey<Vector3?>
    {
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public DeepCore.EventTrigger.Data.AbstractValue<Vector3?> TValue { get { return Value; } }
    }

    #endregion
    //-------------------------------------------------------------------------------------------


}
