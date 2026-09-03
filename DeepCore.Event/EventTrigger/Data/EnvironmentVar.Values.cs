using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{
    //---------------------------------------------------------------------------------------------------

    [Desc("环境变量", "[基础]/环境变量")]
    public class GetEnvironmentAsString : StringValue
    {
        [EnvironmentVarIDAttribute(typeof(StringValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<string>(Key);
        }
    }

    [Desc("环境变量", "[基础]/环境变量")]
    public class GetEnvironmentAsInteger : IntegerValue
    {
        [EnvironmentVarIDAttribute(typeof(IntegerValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Int32>(Key);
        }
    }

    [Desc("环境变量", "[基础]/环境变量")]
    public class GetEnvironmentAsReal : RealValue
    {
        [EnvironmentVarIDAttribute(typeof(RealValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Single>(Key);
        }
    }

    [Desc("环境变量", "[基础]/环境变量")]
    public class GetEnvironmentAsBoolean : BooleanValue
    {
        [EnvironmentVarIDAttribute(typeof(BooleanValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override Boolean GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Boolean>(Key);
        }
    }

    //---------------------------------------------------------------------------------------------------

    [Desc("获取并设置环境变量", "[基础]/环境变量")]
    public class GetAndSetEnvironmentAsString : StringValue
    {
        [EnvironmentVarIDAttribute(typeof(StringValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<string> SetValue = new StringValue.VALUE(string.Empty);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<string>(Key);
            api.API.SetEnvironmentVar(Key, SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }

    [Desc("获取并设置环境变量(int)", "[基础]/环境变量")]
    public class GetAndSetEnvironmentAsInteger : IntegerValue
    {
        [EnvironmentVarIDAttribute(typeof(IntegerValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Int32>(Key);
            api.API.SetEnvironmentVar(Key, (int)SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }

    [Desc("获取并设置环境变量(float)", "[基础]/环境变量")]
    public class GetAndSetEnvironmentAsReal : RealValue
    {
        [EnvironmentVarIDAttribute(typeof(RealValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Single>(Key);
            api.API.SetEnvironmentVar(Key, (float)SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }

    [Desc("获取并设置环境变量", "[基础]/环境变量")]
    public class GetAndSetEnvironmentAsBoolean : BooleanValue
    {
        [EnvironmentVarIDAttribute(typeof(BooleanValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<bool> SetValue = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override Boolean GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Boolean>(Key);
            api.API.SetEnvironmentVar(Key, SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }


    [Desc("获取并增加环境变量(int++)", "[基础]/环境变量")]
    public class GetAndAddEnvironmentAsInteger : IntegerValue
    {
        [EnvironmentVarIDAttribute(typeof(IntegerValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndAdd((int)环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Int32>(Key);
            api.API.SetEnvironmentVar(Key, ret + (int)SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }

    [Desc("获取并增加环境变量(float++)", "[基础]/环境变量")]
    public class GetAndAddEnvironmentAsReal : RealValue
    {
        [EnvironmentVarIDAttribute(typeof(RealValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndAdd((float)环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Single>(Key);
            api.API.SetEnvironmentVar(Key, ret + (float)SetValue.GetValueAs(api, args), true);
            return ret;
        }
    }


    [Desc("增加并获取环境变量(++int)", "[基础]/环境变量")]
    public class AddAndGetEnvironmentAsInteger : IntegerValue
    {
        [EnvironmentVarIDAttribute(typeof(IntegerValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("AddAndGet((int)环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Int32>(Key);
            ret += (int)SetValue.GetValueAs(api, args);
            api.API.SetEnvironmentVar(Key, ret, true);
            return ret;
        }
    }

    [Desc("增加并获取环境变量(++float)", "[基础]/环境变量")]
    public class AddAndGetEnvironmentAsReal : RealValue
    {
        [EnvironmentVarIDAttribute(typeof(RealValue))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("AddAndGet(环境变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.API.GetEnvironmentVarAs<Single>(Key);
            ret += (Single)SetValue.GetValueAs(api, args);
            api.API.SetEnvironmentVar(Key, ret, true);
            return ret;
        }
    }



    //---------------------------------------------------------------------------------------------------


    [Desc("环境变量(变量)", "[基础]/环境变量")]
    public class GetEnvironmentAsStringSV : StringValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<string>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[基础]/环境变量")]
    public class GetEnvironmentAsIntegerSV : IntegerValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Int32>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[基础]/环境变量")]
    public class GetEnvironmentAsRealSV : RealValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Single>(Key.GetValueAs(api, args));
        }
    }

    [Desc("环境变量(变量)", "[基础]/环境变量")]
    public class GetEnvironmentAsBooleanSV : BooleanValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取环境变量[\"{0}\"]", Key);
        }
        protected override Boolean GetValue(EventExecutor api, IEventArguments args)
        {
            return api.API.GetEnvironmentVarAs<Boolean>(Key.GetValueAs(api, args));
        }
    }

}

