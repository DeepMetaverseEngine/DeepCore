using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;
using DeepCore.EventTrigger.Data;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class LocalVar : DeepCore.EventTrigger.Data.EventLocalVar
    {
        sealed protected override object GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract object GetValue(IEventTriggerAdapter api, EventArguments args);
    }


    // #if false
    // 
    //     [Desc("设置字符串型", "临时变量")]
    //     public class LocalVarString : LocalVar
    //     {
    //         [Desc("变量值")]
    //         public AbstractValue<string> Value = new StringValue.VALUE("text");
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("<c color='"+sw.COLOR_KEYWORKD+"'>VAR</c> {0} = {1}", Key, Value);
    //         }
    //         protected override object GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return Value.GetValueAs(api, args);
    //         }
    //         public override object GetAbstractValue()
    //         {
    //             return Value;
    //         }
    //     }
    // 
    //     [Desc("设置整数型", "临时变量")]
    //     public class LocalVarInteger : LocalVar
    //     {
    //         [Desc("变量值")]
    //         public AbstractValue<double> Value = new IntegerValue.VALUE();
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("<c color='"+sw.COLOR_KEYWORKD+"'>VAR</c> {0} = {1}", Key, Value);
    //         }
    //         protected override object GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return Value.GetValueAs(api, args);
    //         }
    //         public override object GetAbstractValue()
    //         {
    //             return Value;
    //         }
    //     }
    // 
    // 
    //     [Desc("设置小数型", "临时变量")]
    //     public class LocalVarReal : LocalVar
    //     {
    //         [Desc("变量值")]
    //         public AbstractValue<double> Value = new RealValue.VALUE();
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("<c color='"+sw.COLOR_KEYWORKD+"'>VAR</c> {0} = {1}", Key, Value);
    //         }
    //         protected override object GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return Value.GetValueAs(api, args);
    //         }
    //         public override object GetAbstractValue()
    //         {
    //             return Value;
    //         }
    //     }
    // 
    // 
    //     [Desc("设置布尔型", "临时变量")]
    //     public class LocalVarBool : LocalVar
    //     {
    //         [Desc("变量值")]
    //         public AbstractValue<bool> Value = new BooleanValue.VALUE();
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("<c color='"+sw.COLOR_KEYWORKD+"'>VAR</c> {0} = {1}", Key, Value);
    //         }
    //         protected override object GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return Value.GetValueAs(api, args);
    //         }
    //         public override object GetAbstractValue()
    //         {
    //             return Value;
    //         }
    //     }
    // #endif

    [Desc("设置单位", "临时变量")]
    public class LocalVarUnit : LocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<InstanceUnit>); }
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.NA();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


    [Desc("设置Flag", "临时变量")]
    public class LocalVarFlag : LocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<InstanceFlag>); }
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.NA();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


    [Desc("设置物品", "临时变量")]
    public class LocalVarItem : LocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<InstanceItem>); }
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.NA();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


    [Desc("设置位置坐标", "临时变量")]
    public class LocalVarPosition : LocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<Vector3?>); }
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }

    // #if false
    // 
    //     [Desc("临时变量", "临时变量")]
    //     public class GetLocalAsString : StringValue
    //     {
    //         [LocalVarIDAttribute(typeof(StringValue))]
    //         [Desc("变量名")]
    //         public string Key = "VarName";
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取临时变量\"{0}\"", Key);
    //         }
    //         protected override string GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.GetLocalVarAs<string>(Key);
    //         }
    //     }
    // 
    //     [Desc("临时变量", "临时变量")]
    //     public class GetLocalAsInteger : IntegerValue
    //     {
    //         [LocalVarIDAttribute(typeof(IntegerValue))]
    //         [Desc("变量名")]
    //         public string Key = "VarName";
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取临时变量\"{0}\"", Key);
    //         }
    //         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.GetLocalVarAs<Int32>(Key);
    //         }
    //     }
    // 
    //     [Desc("临时变量", "临时变量")]
    //     public class GetLocalAsReal : RealValue
    //     {
    //         [LocalVarIDAttribute(typeof(RealValue))]
    //         [Desc("变量名")]
    //         public string Key = "VarName";
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取临时变量\"{0}\"", Key);
    //         }
    //         protected override double GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.GetLocalVarAs<Single>(Key);
    //         }
    //     }
    // 
    //     [Desc("临时变量", "临时变量")]
    //     public class GetLocalAsBoolean : BooleanValue
    //     {
    //         [LocalVarIDAttribute(typeof(BooleanValue))]
    //         [Desc("变量名")]
    //         public string Key = "VarName";
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("获取临时变量\"{0}\"", Key);
    //         }
    //         protected override Boolean GetValue(IEditorValueAdapter api, EventArguments args)
    //         {
    //             return api.GetLocalVarAs<Boolean>(Key);
    //         }
    //     }
    // 
    // #endif
    //-------------------------------------------------------------------------------------------------------------------------------------------
    [Desc("临时变量", "[游戏]/临时变量")]
    public class GetLocalAsUnit : UnitValue
    {
        [LocalVarType(typeof(AbstractValue<InstanceUnit>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceUnit>(Key);
        }
    }

    [Desc("临时变量", "[游戏]/临时变量")]
    public class GetLocalAsItem : ItemValue
    {
        [LocalVarType(typeof(AbstractValue<InstanceItem>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceItem>(Key);
        }
    }
    [Desc("临时变量", "[游戏]/临时变量")]
    public class GetLocalAsFlag : FlagValue
    {
        [LocalVarType(typeof(AbstractValue<InstanceFlag>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceFlag>(Key);
        }
    }

    [Desc("临时变量", "[游戏]/临时变量")]
    public class GetLocalAsPosition : PositionValue
    {
        [LocalVarType(typeof(AbstractValue<Vector3?>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<Vector3>(Key);
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------

    [Desc("临时变量(变量)", "[游戏]/临时变量")]
    public class GetLocalAsUnitSV : UnitValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceUnit>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量)", "[游戏]/临时变量")]
    public class GetLocalAsItemSV : ItemValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceItem GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceItem>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量)", "[游戏]/临时变量")]
    public class GetLocalAsFlagSV : FlagValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<InstanceFlag>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量)", "[游戏]/临时变量")]
    public class GetLocalAsPositionSV : PositionValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量<c color='{0}'>\"{1}\"</c>", sw.COLOR_CONST, Key);
        }
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.GetLocalVarAs<Vector3>(Key.GetValueAs(api, args));
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------


    [Desc("设置单位", "[游戏]/设置-临时变量")]
    public class SetLocalAsUnit : SetLocalVar<InstanceUnit>
    {
        [LocalVarType(typeof(AbstractValue<InstanceUnit>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.Trigging();
        override public string TKey { get { return Key; } }
        override public AbstractValue<InstanceUnit> TValue { get { return Value; } }
    }

    [Desc("设置物品", "[游戏]/设置-临时变量")]
    public class SetLocalAsItem : SetLocalVar<InstanceItem>
    {
        [LocalVarType(typeof(AbstractValue<InstanceItem>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.Trigging();
        override public string TKey { get { return Key; } }
        override public AbstractValue<InstanceItem> TValue { get { return Value; } }

    }

    [Desc("设置Flag", "[游戏]/设置-临时变量")]
    public class SetLocalAsFlag : SetLocalVar<InstanceFlag>
    {
        [LocalVarType(typeof(AbstractValue<InstanceFlag>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.TriggingRegion();
        override public string TKey { get { return Key; } }
        override public AbstractValue<InstanceFlag> TValue { get { return Value; } }

    }

    [Desc("设置坐标", "[游戏]/设置-临时变量")]
    public class SetLocalAsPosition : SetLocalVar<Vector3?>
    {
        [LocalVarType(typeof(AbstractValue<Vector3?>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public string TKey { get { return Key; } }
        override public AbstractValue<Vector3?> TValue { get { return Value; } }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------

    [Desc("设置单位(变量)", "[游戏]/设置-临时变量")]
    public class SetLocalAsUnitSV : SetLocalVarSV<InstanceUnit>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceUnit> Value = new UnitValue.Trigging();
        override public AbstractValue<string> TKey { get { return Key; } }
        override public AbstractValue<InstanceUnit> TValue { get { return Value; } }
    }

    [Desc("设置物品(变量)", "[游戏]/设置-临时变量")]
    public class SetLocalAsItemSV : SetLocalVarSV<InstanceItem>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceItem> Value = new ItemValue.Trigging();
        override public AbstractValue<string> TKey { get { return Key; } }
        override public AbstractValue<InstanceItem> TValue { get { return Value; } }
    }

    [Desc("设置Flag(变量)", "[游戏]/设置-临时变量")]
    public class SetLocalAsFlagSV : SetLocalVarSV<InstanceFlag>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<InstanceFlag> Value = new FlagValue.TriggingRegion();
        override public AbstractValue<string> TKey { get { return Key; } }
        override public AbstractValue<InstanceFlag> TValue { get { return Value; } }
    }

    [Desc("设置坐标(变量)", "[游戏]/设置-临时变量")]
    public class SetLocalAsPositionSV : SetLocalVarSV<Vector3?>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<Vector3?> Value = new PositionValue.VALUE();
        override public AbstractValue<string> TKey { get { return Key; } }
        override public AbstractValue<Vector3?> TValue { get { return Value; } }
    }


}
