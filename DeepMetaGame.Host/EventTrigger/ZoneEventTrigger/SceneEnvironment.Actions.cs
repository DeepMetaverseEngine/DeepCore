using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Reflection;
using System;
using DeepCore.Geometry;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepMetaGame.Data.GUI;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("", "[游戏]/设置-环境变量")]
    public abstract class SetEnvironmentVar<T> : ZoneAbstractAction
    {
        abstract public string TKey { get; }
        abstract public AbstractValue<T> TValue { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置环境变量\"{0}\"={1};", TKey, TValue);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            api.ZoneAPI.SetEnvironmentVar(TKey, value); return null;
        }
    }

//     [Desc("设置字符串型", "设置 - 环境变量")]
//     public class SetEnvironmentString : SetEnvironmentVar<string>
//     {
//         [SceneVarIDAttribute(typeof(StringValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         [Desc("变量值")]
//         public AbstractValue<string> Value = new StringValue.VALUE("text");
//         override public AbstractValue<string> TValue { get { return Value; } }
//         override public string TKey { get { return Key; } }
//     }
// 
//     [Desc("设置整数型", "设置 - 环境变量")]
//     public class SetEnvironmentInteger : SetEnvironmentVar<Int32>
//     {
//         [SceneVarIDAttribute(typeof(IntegerValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         [Desc("变量值")]
//         public AbstractValue<double> Value = new IntegerValue.VALUE();
//         override public AbstractValue<double> TValue { get { return Value; } }
//         override public string TKey { get { return Key; } }
//     }
// 
//     [Desc("设置小数型", "设置 - 环境变量")]
//     public class SetEnvironmentReal : SetEnvironmentVar<Single>
//     {
//         [SceneVarIDAttribute(typeof(RealValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         [Desc("变量值")]
//         public AbstractValue<double> Value = new RealValue.VALUE();
//         override public AbstractValue<double> TValue { get { return Value; } }
//         override public string TKey { get { return Key; } }
//     }
// 
//     [Desc("设置布尔型", "设置 - 环境变量")]
//     public class SetEnvironmentBool : SetEnvironmentVar<Boolean>
//     {
//         [SceneVarIDAttribute(typeof(BooleanValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         [Desc("变量值")]
//         public AbstractValue<bool> Value = new BooleanValue.VALUE();
//         override public AbstractValue<Boolean> TValue { get { return Value; } }
//         override public string TKey { get { return Key; } }
//     }

    [Desc("设置单位", "[游戏]/设置-环境变量")]
    public class SetEnvironmentUnit : SetEnvironmentVar<InstanceUnit>
    {
        [EnvironmentVarIDAttribute(typeof(UnitValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        override public AbstractValue<InstanceUnit> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置Flag", "[游戏]/设置-环境变量")]
    public class SetEnvironmentFlag : SetEnvironmentVar<InstanceFlag>
    {
        [EnvironmentVarIDAttribute(typeof(FlagValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        override public AbstractValue<InstanceFlag> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置物品", "[游戏]/设置-环境变量")]
    public class SetEnvironmentItem : SetEnvironmentVar<InstanceItem>
    {
        [EnvironmentVarIDAttribute(typeof(ItemValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        override public AbstractValue<InstanceItem> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置位置坐标", "[游戏]/设置-环境变量")]
    public class SetEnvironmentPosition : SetEnvironmentVar<Vector3?>
    {
        [EnvironmentVarIDAttribute(typeof(PositionValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public AbstractValue<Vector3?> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置位置坐标", "[游戏]/设置-环境变量")]
    public class SetEnvironmentGUI : SetEnvironmentVar<HostGUIComponent>
    {
        [EnvironmentVarIDAttribute(typeof(GUIValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<HostGUIComponent> Value = new GUIValue.TriggingComponent();
        override public AbstractValue<HostGUIComponent> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }
    //---------------------------------------------------------------------------------------------------

    [Desc("", "[游戏]/设置-环境变量")]
    public abstract class SetEnvironmentVarSV<T> : ZoneAbstractAction
    {
        abstract public AbstractValue<string> TKey { get; }
        abstract public AbstractValue<T> TValue { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置环境变量\"{0}\"={1}", TKey, TValue);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            api.ZoneAPI.SetEnvironmentVar(TKey.GetValueAs(api, args), value); return null;
        }
    }
//     [Desc("设置字符串型(变量)", "设置 - 环境变量")]
//     public class SetEnvironmentStringSV : SetEnvironmentVarSV<string>
//     {
//         [Desc("变量名")]
//         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
//         [Desc("变量值")]
//         public AbstractValue<string> Value = new StringValue.VALUE("text");
//         override public AbstractValue<string> TValue { get { return Value; } }
//         override public AbstractValue<string> TKey { get { return Key; } }
//     }
// 
//     [Desc("设置整数型(变量)", "设置 - 环境变量")]
//     public class SetEnvironmentIntegerSV : SetEnvironmentVarSV<Int32>
//     {
//         [Desc("变量名")]
//         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
//         [Desc("变量值")]
//         public AbstractValue<double> Value = new IntegerValue.VALUE();
//         override public AbstractValue<double> TValue { get { return Value; } }
//         override public AbstractValue<string> TKey { get { return Key; } }
//     }
// 
//     [Desc("设置小数型(变量)", "设置 - 环境变量")]
//     public class SetEnvironmentRealSV : SetEnvironmentVarSV<Single>
//     {
//         [Desc("变量名")]
//         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
//         [Desc("变量值")]
//         public AbstractValue<double> Value = new RealValue.VALUE();
//         override public AbstractValue<double> TValue { get { return Value; } }
//         override public AbstractValue<string> TKey { get { return Key; } }
//     }
// 
//     [Desc("设置布尔型(变量)", "设置 - 环境变量")]
//     public class SetEnvironmentBoolSV : SetEnvironmentVarSV<Boolean>
//     {
//         [Desc("变量名")]
//         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
//         [Desc("变量值")]
//         public AbstractValue<bool> Value = new BooleanValue.VALUE();
//         override public AbstractValue<Boolean> TValue { get { return Value; } }
//         override public AbstractValue<string> TKey { get { return Key; } }
//     }

    [Desc("设置单位(变量)", "[游戏]/设置-环境变量")]
    public class SetEnvironmentUnitSV : SetEnvironmentVarSV<InstanceUnit>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        override public AbstractValue<InstanceUnit> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置Flag(变量)", "[游戏]/设置-环境变量")]
    public class SetEnvironmentFlagSV : SetEnvironmentVarSV<InstanceFlag>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        override public AbstractValue<InstanceFlag> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置物品(变量)", "[游戏]/设置-环境变量")]
    public class SetEnvironmentItemSV : SetEnvironmentVarSV<InstanceItem>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        override public AbstractValue<InstanceItem> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置位置坐标(变量)", "[游戏]/设置-环境变量")]
    public class SetEnvironmentPositionSV : SetEnvironmentVarSV<Vector3?>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public AbstractValue<Vector3?> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置GUI(变量)", "[游戏]/设置-环境变量")]
    public class SetEnvironmentGUISV : SetEnvironmentVarSV<HostGUIComponent>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<HostGUIComponent> Value = new GUIValue.TriggingComponent();
        override public AbstractValue<HostGUIComponent> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }
}

