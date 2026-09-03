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
    #region _获取_单位变量_


    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsString : ZoneStringValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<string>(Key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsInteger : ZoneIntegerValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<double>(Key);
            }
            return 0;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsReal : ZoneRealValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<double>(Key);
            }
            return 0;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsBoolean : ZoneBooleanValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<Boolean>(Key);
            }
            return false;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsUnit : UnitValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<InstanceUnit>(Key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsItem : ItemValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<InstanceItem>(Key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsFlag : FlagValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<InstanceFlag>(Key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量")]
    public class GetUnitAsPosition : PositionValue
    {
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.GetEnvironmentVarAs<Vector3>(Key);
            }
            return Vector3.NaN;
        }
    }

    #endregion
    //-------------------------------------------------------------------------------------------
    #region _获取_单位变量_StringValueKey_


    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsStringStringValueKey : ZoneStringValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<string>(key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsIntegerStringValueKey : ZoneIntegerValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new ZoneStringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<double>(key);
            }
            return 0;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsRealStringValueKey : ZoneRealValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<double>(key);
            }
            return 0;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsBooleanStringValueKey : ZoneBooleanValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override Boolean GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<Boolean>(key);
            }
            return false;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsUnitStringValueKey : UnitValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<InstanceUnit>(key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsItemStringValueKey : ItemValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<InstanceItem>(key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsFlagStringValueKey : FlagValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<InstanceFlag>(key);
            }
            return null;
        }
    }

    [Desc("单位变量", "[游戏]/单位/变量(变量索引)")]
    public class GetUnitAsPositionStringValueKey : PositionValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Unit);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetEnvironmentVarAs<Vector3>(key);
            }
            return Vector3.NaN;
        }
    }

    #endregion

    #region _获取_单位变量_StringValueKey_------------------------------------------------


    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsStringStringValueKey : ZoneStringValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<string>(key);
            }
            return null;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsIntegerStringValueKey : ZoneIntegerValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<double>(key);
            }
            return 0;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsRealStringValueKey : ZoneRealValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<double>(key);
            }
            return 0;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsBooleanStringValueKey : ZoneBooleanValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<Boolean>(key);
            }
            return false;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsUnitStringValueKey : UnitValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<InstanceUnit>(key);
            }
            return null;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsItemStringValueKey : ItemValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<InstanceItem>(key);
            }
            return null;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsFlagStringValueKey : FlagValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<InstanceFlag>(key);
            }
            return null;
        }
    }

    [Desc("玩家变量", "[游戏]/玩家/玩家变量(变量索引)")]
    public class GetPlayerAsPositionStringValueKey : PositionValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("玩家")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取单位({1})变量\"{0}\"", Key, Player);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Player.GetValueAs<InstancePlayer>(api, args);
            string key = Key.GetValueAs(api, args);
            if (unit != null && key != null)
            {
                return unit.GetPlayerEnvironmentVarAs<Vector3>(key);
            }
            return Vector3.NaN;
        }
    }

    #endregion
}
