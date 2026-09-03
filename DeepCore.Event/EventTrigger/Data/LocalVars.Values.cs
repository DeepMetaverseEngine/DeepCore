using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{



    [Desc("临时变量", "[基础]/临时变量")]
    public class GetLocalAsString : StringValue
    {
        [LocalVarType(typeof(AbstractValue<string>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<string>(Key);
        }
    }

    [Desc("临时变量", "[基础]/临时变量")]
    public class GetLocalAsInteger : IntegerValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<double>(Key);
        }
    }

    [Desc("临时变量", "[基础]/临时变量")]
    public class GetLocalAsReal : RealValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<Single>(Key);
        }
    }

    [Desc("临时变量", "[基础]/临时变量")]
    public class GetLocalAsBoolean : BooleanValue
    {
        [LocalVarType(typeof(AbstractValue<bool>))]
        [Desc("变量名")]
        public string Key = "VarName";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override Boolean GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<Boolean>(Key);
        }
    }

    //---------------------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------------------

    [Desc("获取并设置临时变量", "[基础]/临时变量")]
    public class GetAndSetLocalAsString : StringValue
    {
        [LocalVarType(typeof(AbstractValue<string>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<string> SetValue = new StringValue.VALUE(string.Empty);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<string>(Key);
            api.SetLocalVar(Key, SetValue.GetValueAs(api, args));
            return ret;
        }
    }

    [Desc("获取并设置临时变量(int)", "[基础]/临时变量")]
    public class GetAndSetLocalAsInteger : IntegerValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(((int))临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Int32>(Key);
            api.SetLocalVar(Key, (int)SetValue.GetValueAs(api, args));
            return ret;
        }
    }

    [Desc("获取并设置临时变量(float)", "[基础]/临时变量")]
    public class GetAndSetLocalAsReal : RealValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet((float)临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Single>(Key);
            api.SetLocalVar(Key, (float)SetValue.GetValueAs(api, args));
            return ret;
        }
    }

    [Desc("获取并设置临时变量", "[基础]/临时变量")]
    public class GetAndSetLocalAsBoolean : BooleanValue
    {
        [LocalVarType(typeof(AbstractValue<bool>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<bool> SetValue = new BooleanValue.VALUE(false);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndSet(临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override Boolean GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Boolean>(Key);
            api.SetLocalVar(Key, SetValue.GetValueAs(api, args));
            return ret;
        }
    }


    [Desc("获取并增加临时变量(int++)", "[基础]/临时变量")]
    public class GetAndAddLocalAsInteger : IntegerValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndAdd((int)临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Int32>(Key);
            api.SetLocalVar(Key, ret + (int)SetValue.GetValueAs(api, args));
            return ret;
        }
    }

    [Desc("获取并增加临时变量(float++)", "[基础]/临时变量")]
    public class GetAndAddLocalAsReal : RealValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("GetAndAdd((float)临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Single>(Key);
            api.SetLocalVar(Key, ret + (float)SetValue.GetValueAs(api, args));
            return ret;
        }
    }


    [Desc("增加并获取临时变量(++int)", "[基础]/临时变量")]
    public class AddAndGetLocalAsInteger : IntegerValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new IntegerValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("AddAndGet((int)临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Int32>(Key);
            ret += (int)SetValue.GetValueAs(api, args);
            api.SetLocalVar(Key, ret);
            return ret;
        }
    }

    [Desc("增加并获取临时变量(++float)", "[基础]/临时变量")]
    public class AddAndGetLocalAsReal : RealValue
    {
        [LocalVarType(typeof(AbstractValue<double>))]
        [Desc("变量名")]
        public string Key = "VarName";
        [Desc("值")]
        public AbstractValue<double> SetValue = new RealValue.VALUE(0);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("AddAndGet((float)临时变量[\"{0}\"], {1})", Key, SetValue);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            var ret = api.GetLocalVarAs<Single>(Key);
            ret += (Single)SetValue.GetValueAs(api, args);
            api.SetLocalVar(Key, ret);
            return ret;
        }
    }



    //---------------------------------------------------------------------------------------------------





    [Desc("临时变量(变量string)", "[基础]/临时变量")]
    public class GetLocalAsStringSV : StringValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override string GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<string>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量int)", "[基础]/临时变量")]
    public class GetLocalAsIntegerSV : IntegerValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<double>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量float)", "[基础]/临时变量")]
    public class GetLocalAsRealSV : RealValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override double GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<float>(Key.GetValueAs(api, args));
        }
    }

    [Desc("临时变量(变量)", "[基础]/临时变量")]
    public class GetLocalAsBooleanSV : BooleanValue
    {
        [Desc("变量名")]
        public AbstractValue<string> Key = new StringValue.VALUE("VarName");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("获取临时变量[<c color='{0}'>\"{1}\"</c>]", sw.COLOR_CONST, Key);
        }
        protected override bool GetValue(EventExecutor api, IEventArguments args)
        {
            return api.GetLocalVarAs<bool>(Key.GetValueAs(api, args));
        }
    }

}
