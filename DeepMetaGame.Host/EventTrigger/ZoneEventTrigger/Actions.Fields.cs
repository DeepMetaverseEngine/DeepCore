using System;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //-----------------------------------------------------------------------------------------

    #region __场景实体__

    [Desc("设置场景字段(string)", "[游戏]/场景/字段")]
    public class SetZoneFieldStringValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<string> Value = new ZoneStringValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Value.GetValueAs(api, args));
            return null;
        }
    }

    [Desc("设置场景字段(int)", "[游戏]/场景/字段")]
    public class SetZoneFieldIntegerValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Convert.ToInt32(Value.GetValueAs(api, args))); return null;
        }
    }

    [Desc("设置场景字段(float)", "[游戏]/场景/字段")]
    public class SetZoneFieldRealValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Convert.ToSingle(Value.GetValueAs(api, args))); return null;
        }
    }

    [Desc("设置场景字段(long)", "[游戏]/场景/字段")]
    public class SetZoneFieldIntegerValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Value.GetValueAs(api, args));
            return null;
        }
    }

    [Desc("设置场景字段(double)", "[游戏]/场景/字段")]
    public class SetZoneFieldRealValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Value.GetValueAs(api, args));
            return null;
        }
    }


    [Desc("设置场景字段(bool)", "[游戏]/场景/字段")]
    public class SetZoneFieldBoolValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceZone), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("值")]
        public AbstractValue<bool> Value = new ZoneBooleanValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", "场景", FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.NameSpace.SetValue(api.ZoneAPI, FieldName, Value.GetValueAs(api, args)); return null;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __单位实体__

    [Desc("设置单位字段(string)", "[游戏]/单位/字段")]
    public class SetUnitFieldStringValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<string> Value = new ZoneStringValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置单位字段(int)", "[游戏]/单位/字段")]
    public class SetUnitFieldIntegerValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, (Convert.ToInt32(Value.GetValueAs(api, args))));
            }
            return null;
        }
    }

    [Desc("设置单位字段(float)", "[游戏]/单位/字段")]
    public class SetUnitFieldRealValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Convert.ToSingle(Value.GetValueAs(api, args)));
            }
            return null;
        }
    }


    [Desc("设置单位字段(long)", "[游戏]/单位/字段")]
    public class SetUnitFieldIntegerValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置单位字段(double)", "[游戏]/单位/字段")]
    public class SetUnitFieldRealValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置单位字段(bool)", "[游戏]/单位/字段")]
    public class SetUnitFieldBoolValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceUnit), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("值")]
        public AbstractValue<bool> Value = new ZoneBooleanValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Unit, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __物品实体__

    [Desc("设置物品字段(string)", "[游戏]/物品/字段")]
    public class SetItemFieldStringValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<string> Value = new ZoneStringValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置物品字段(int)", "[游戏]/物品/字段")]
    public class SetItemFieldIntegerValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Convert.ToInt32(Value.GetValueAs(api, args)));
            }
            return null;
        }
    }

    [Desc("设置物品字段(float)", "[游戏]/物品/字段")]
    public class SetItemFieldRealValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Convert.ToSingle(Value.GetValueAs(api, args)));
            }
            return null;
        }
    }


    [Desc("设置物品字段(long)", "[游戏]/物品/字段")]
    public class SetItemFieldIntegerValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneIntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置物品字段(double)", "[游戏]/物品/字段")]
    public class SetItemFieldRealValue64 : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<double> Value = new ZoneRealValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    [Desc("设置物品字段(bool)", "[游戏]/物品/字段")]
    public class SetItemFieldBoolValue : ZoneAbstractAction
    {
        [SetObjectMemberName(typeof(InstanceItem), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();
        [Desc("值")]
        public AbstractValue<bool> Value = new ZoneBooleanValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}={2};", Item, FieldName, Value);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Item.GetValueAs(api, args);
            if (o != null)
            {
                api.NameSpace.SetValue(o, FieldName, Value.GetValueAs(api, args));
            }
            return null;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

}
