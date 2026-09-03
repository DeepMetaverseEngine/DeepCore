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

//     [Desc("环境变量", "环境变量")]
//     public class GetEnvironmentAsString : StringValue
//     {
//         [SceneVarIDAttribute(typeof(StringValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         public override void ToFunctionText(EventStringBuilder sw)
//         {
//             sw.AppendFormat("获取环境变量\"{0}\"", Key);
//         }
//         protected override string GetValue(IEditorValueAdapter api, EventArguments args)
//         {
//             return api.ZoneAPI.GetEnvironmentVarAs<string>(Key);
//         }
//     }
// 
//     [Desc("环境变量", "环境变量")]
//     public class GetEnvironmentAsInteger : IntegerValue
//     {
//         [SceneVarIDAttribute(typeof(IntegerValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         public override void ToFunctionText(EventStringBuilder sw)
//         {
//             sw.AppendFormat("获取环境变量\"{0}\"", Key);
//         }
//         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
//         {
//             return api.ZoneAPI.GetEnvironmentVarAs<Int32>(Key);
//         }
//     }
// 
//     [Desc("环境变量", "环境变量")]
//     public class GetEnvironmentAsReal : RealValue
//     {
//         [SceneVarIDAttribute(typeof(RealValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         public override void ToFunctionText(EventStringBuilder sw)
//         {
//             sw.AppendFormat("获取环境变量\"{0}\"", Key);
//         }
//         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
//         {
//             return api.ZoneAPI.GetEnvironmentVarAs<Single>(Key);
//         }
//     }
// 
//     [Desc("环境变量", "环境变量")]
//     public class GetEnvironmentAsBoolean : BooleanValue
//     {
//         [SceneVarIDAttribute(typeof(BooleanValue))]
//         [Desc("变量名")]
//         public string Key = "VarName";
//         public override void ToFunctionText(EventStringBuilder sw)
//         {
//             sw.AppendFormat("获取环境变量\"{0}\"", Key);
//         }
//         protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
//         {
//             return api.ZoneAPI.GetEnvironmentVarAs<Boolean>(Key);
//         }
//     }

    [Desc("环境变量", "[游戏]/环境变量")]
    public class GetEnvironmentAsUnit : UnitValue
    {
        [EnvironmentVarIDAttribute(typeof(UnitValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceUnit>(Key);
        }
    }

    [Desc("环境变量", "[游戏]/环境变量")]
    public class GetEnvironmentAsItem : ItemValue
    {
        [EnvironmentVarIDAttribute(typeof(ItemValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceItem>(Key);
        }
    }
    [Desc("环境变量", "[游戏]/环境变量")]
    public class GetEnvironmentAsFlag : FlagValue
    {
        [EnvironmentVarIDAttribute(typeof(FlagValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceFlag>(Key);
        }
    }

    [Desc("环境变量", "[游戏]/环境变量")]
    public class GetEnvironmentAsPosition : PositionValue
    {
        [EnvironmentVarIDAttribute(typeof(PositionValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<Vector3>(Key);
        }
    }
    [Desc("环境变量", "[游戏]/环境变量")]
    public class GetEnvironmentAsGUI : GUIValue
    {
        [EnvironmentVarIDAttribute(typeof(GUIValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<HostGUIComponent>(Key);
        }
    }
    //---------------------------------------------------------------------------------------------------


    //     [Desc("环境变量(变量)", "环境变量")]
    //     public class GetEnvironmentAsStringSV : StringValue
    //     {
    //         [Desc("变量名")]
    //         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取环境变量\"{0}\"", Key);
    //         }
    //         protected override string GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.ZoneAPI.GetEnvironmentVarAs<string>(Key.GetValueAs(api, args));
    //         }
    //     }
    // 
    //     [Desc("环境变量(变量)", "环境变量")]
    //     public class GetEnvironmentAsIntegerSV : IntegerValue
    //     {
    //         [Desc("变量名")]
    //         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取环境变量\"{0}\"", Key);
    //         }
    //         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.ZoneAPI.GetEnvironmentVarAs<Int32>(Key.GetValueAs(api, args));
    //         }
    //     }
    // 
    //     [Desc("环境变量(变量)", "环境变量")]
    //     public class GetEnvironmentAsRealSV : RealValue
    //     {
    //         [Desc("变量名")]
    //         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取环境变量\"{0}\"", Key);
    //         }
    //         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.ZoneAPI.GetEnvironmentVarAs<Single>(Key.GetValueAs(api, args));
    //         }
    //     }
    // 
    //     [Desc("环境变量(变量)", "环境变量")]
    //     public class GetEnvironmentAsBooleanSV : BooleanValue
    //     {
    //         [Desc("变量名")]
    //         public AbstractValue<string> Key = new StringValue.VALUE("VarName");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取环境变量\"{0}\"", Key);
    //         }
    //         protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.ZoneAPI.GetEnvironmentVarAs<Boolean>(Key.GetValueAs(api, args));
    //         }
    //     }

    [Desc("环境变量(变量)", "[游戏]/环境变量")]
    public class GetEnvironmentAsUnitSV : UnitValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceUnit>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[游戏]/环境变量")]
    public class GetEnvironmentAsItemSV : ItemValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceItem>(Key.GetValueAs(api, args));
        }
    }
    [Desc("环境变量(变量)", "[游戏]/环境变量")]
    public class GetEnvironmentAsFlagSV : FlagValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<InstanceFlag>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[游戏]/环境变量")]
    public class GetEnvironmentAsPositionSV : PositionValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<Vector3>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[游戏]/环境变量")]
    public class GetEnvironmentAsGUISV : GUIValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量\"{0}\"", Key);
        }
        protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.GetEnvironmentVarAs<HostGUIComponent>(Key.GetValueAs(api, args));
        }
    }
}

