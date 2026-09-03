using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{

    [Desc("临时变量")]
    [Expandable]
    public abstract class EventLocalVar : EventExternalizable
    {
        sealed public override Type BaseType { get => typeof(EventLocalVar); }
        public abstract Type ValueType { get; }

        [Desc("变量名")]
        public string Key = "LocalVarName";
        protected abstract object GetValue(EventExecutor api, IEventArguments args);
        public object GetLocalVar(EventExecutor api, IEventArguments args)
        {
            if (EventExecutor.ENABLE_TRACE) api.Trace(this);
            return GetValue(api, args);
        }
    }


    [Desc("设置字符串型", "[基础]/临时变量")]
    public class LocalVarString : EventLocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<string>); }
        [Desc("变量值")]
        public AbstractValue<string> Value = new StringValue.VALUE("text");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='"+sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(EventExecutor api, IEventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }

    [Desc("设置整数型", "[基础]/临时变量")]
    public class LocalVarInteger : EventLocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<double>); }
        [Desc("变量值")]
        public AbstractValue<double> Value = new IntegerValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='"+sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(EventExecutor api, IEventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


    [Desc("设置小数型", "[基础]/临时变量")]
    public class LocalVarReal : EventLocalVar
    {
        [Desc("变量值")]
        public AbstractValue<double> Value = new RealValue.VALUE();
        public override Type ValueType { get => typeof(AbstractValue<double>); }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='"+sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(EventExecutor api, IEventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


    [Desc("设置布尔型", "[基础]/临时变量")]
    public class LocalVarBool : EventLocalVar
    {
        public override Type ValueType { get => typeof(AbstractValue<bool>); }
        [Desc("变量值")]
        public AbstractValue<bool> Value = new BooleanValue.VALUE();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='"+sw.COLOR_CONST + "'>VAR</c> {0} = {1}", Key, Value);
        }
        protected override object GetValue(EventExecutor api, IEventArguments args)
        {
            return Value.GetValueAs(api, args);
        }
    }


}
