using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{

    [Desc("", "[基础]/环境变量")]
    public abstract class SetEnvironmentVar<T> : AbstractAction
    {
        abstract public string TKey { get; }
        abstract public AbstractValue<T> TValue { get; }

        [DescAttribute("是否同步给客户端")]
        public bool SyncToClient = false;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置环境变量\"{0}\"={1};", TKey, TValue);
        }
        override protected object Run(EventExecutor api, IEventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            api.API.SetEnvironmentVar(TKey, value, SyncToClient); 
            return null;
        }
    }

    [Desc("设置字符串型", "[基础]/环境变量")]
    public class SetEnvironmentString : SetEnvironmentVar<string>
    {
        [EnvironmentVarIDAttribute(typeof(StringValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public AbstractValue<string> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置整数型", "[基础]/环境变量")]
    public class SetEnvironmentInteger : SetEnvironmentVar<double>
    {
        [EnvironmentVarIDAttribute(typeof(IntegerValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置小数型", "[基础]/环境变量")]
    public class SetEnvironmentReal : SetEnvironmentVar<double>
    {
        [EnvironmentVarIDAttribute(typeof(RealValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置布尔型", "[基础]/环境变量")]
    public class SetEnvironmentBool : SetEnvironmentVar<Boolean>
    {
        [EnvironmentVarIDAttribute(typeof(BooleanValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public AbstractValue<Boolean> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }


    //---------------------------------------------------------------------------------------------------

    [Desc("", "环境变量/设置")]
    public abstract class SetEnvironmentVarSV<T> : AbstractAction
    {
        abstract public AbstractValue<string> TKey { get; }
        abstract public AbstractValue<T> TValue { get; }

        [DescAttribute("是否同步给客户端")]
        public bool SyncToClient = false;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置环境变量\"{0}\"={1};", TKey, TValue);
        }
        override protected object Run(EventExecutor api, IEventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            api.API.SetEnvironmentVar(TKey.GetValueAs(api, args), value,SyncToClient);
            return null;
        }
    }
    [Desc("设置字符串型(变量)", "[基础]/环境变量")]
    public class SetEnvironmentStringSV : SetEnvironmentVarSV<string>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public AbstractValue<string> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置整数型(变量)", "[基础]/环境变量")]
    public class SetEnvironmentIntegerSV : SetEnvironmentVarSV<double>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置小数型(变量)", "[基础]/环境变量")]
    public class SetEnvironmentRealSV : SetEnvironmentVarSV<double>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置布尔型(变量)", "[基础]/环境变量")]
    public class SetEnvironmentBoolSV : SetEnvironmentVarSV<Boolean>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public AbstractValue<Boolean> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }
}

