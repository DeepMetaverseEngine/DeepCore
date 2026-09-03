using DeepCore.Formula;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{

    public abstract class ObjectFieldStringValue<T, O> : StringValue where O : AbstractValue
    {
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public O Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    public abstract class ObjectFieldIntegerValue<T, O> : IntegerValue where O : AbstractValue
    {
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public O Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }
    public abstract class ObjectFieldLongValue<T, O> : IntegerValue where O : AbstractValue
    {
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public O Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    public abstract class ObjectFieldRealValue<T, V> : RealValue where V : AbstractValue
    {
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public V Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0f;
        }
    }
    public abstract class ObjectFieldDoubleValue<T, V> : RealValue where V : AbstractValue
    {
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public V Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0f;
        }
    }

    public abstract class ObjectFieldBoolValue<T, V> : BooleanValue where V : AbstractValue
    { 
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("对象")]
        public V Object;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}).{1}", Object, FieldName);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            var o = Object.GetRunValue(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }


}
