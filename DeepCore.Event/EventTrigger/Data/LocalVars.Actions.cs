using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{


    [Desc("", "[基础]/临时变量")]
    public abstract class SetLocalVar<T> : AbstractAction
    {
        abstract public AbstractValue<T> TValue { get; }
        abstract public string TKey { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置临时变量<c color='{0}'>\"{1}\"</c>={2};", sw.COLOR_CONST, TKey, TValue);
        }
        override protected object Run(EventExecutor api, IEventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            api.SetLocalVar(TKey, value); return null;
        }
    }

    [Desc("设置字符串型", "[基础]/临时变量")]
    public class SetLocalString : SetLocalVar<string>
    {
        [LocalVarType(typeof(AbstractValue<string>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public AbstractValue<string> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置整数型", "[基础]/临时变量")]
    public class SetLocalInteger : SetLocalVar<double>
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置小数型", "[基础]/临时变量")]
    public class SetLocalReal : SetLocalVar<double>
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }

    [Desc("设置布尔型", "[基础]/临时变量")]
    public class SetLocalBool : SetLocalVar<Boolean>
    {
        [LocalVarType(typeof(AbstractValue<bool>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public AbstractValue<Boolean> TValue { get { return Value; } }
        override public string TKey { get { return Key; } }
    }


    //---------------------------------------------------------------------------------------------------


    [Desc("", "[基础]/临时变量(变量)")]
    public abstract class SetLocalVarSV<T> : AbstractAction
    {
        abstract public AbstractValue<T> TValue { get; }
        abstract public AbstractValue<string> TKey { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("设置临时变量<c color='{0}'>\"{1}\"</c>={2};", sw.COLOR_CONST, TKey, TValue);
        }
        override protected object Run(EventExecutor api, IEventArguments args)
        {
            T value = TValue.GetValueAs(api, args);
            string key = TKey.GetValueAs(api, args);
            api.SetLocalVar(key, value); return null;
        }
    }

    [Desc("设置字符串型(变量)", "[基础]/临时变量")]
    public class SetLocalStringSV : SetLocalVarSV<string>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        override public AbstractValue<string> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置整数型(变量)", "[基础]/临时变量")]
    public class SetLocalIntegerSV : SetLocalVarSV<double>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置小数型(变量)", "[基础]/临时变量")]
    public class SetLocalRealSV : SetLocalVarSV<double>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        override public AbstractValue<double> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

    [Desc("设置布尔型(变量)", "[基础]/临时变量")]
    public class SetLocalBoolSV : SetLocalVarSV<Boolean>
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        override public AbstractValue<Boolean> TValue { get { return Value; } }
        override public AbstractValue<string> TKey { get { return Key; } }
    }

}
