using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Xml;

namespace DeepCore.EventTrigger.Data
{
    [Desc("事件数据")]
    [Expandable]
    public abstract class AbstractValue : EventExternalizable, IEventValue
    {
        static AbstractValue()
        {
            XmlSerializer.AddDefaultConverter(((XmlSerializer ser, XmlElement dataElement, Type decleardType, Exception err, out object data, object root) =>
            {
                if (typeof(AbstractValue).IsAssignableFrom(decleardType))
                {
                    data = new CommentValue()
                    {
                        Comment = err.Message + "\n" + XmlUtil.ToXmlString(dataElement),
                    };
                    return true;
                }
                data = null;
                return false;
            }));
        }

        //public abstract object GetEnvValue(EventExecutor api, IEventArguments args);
        public abstract object GetRunValue(EventExecutor api, IEventArguments args);
        public object GetEnvValue(EventExecutor api)
        {
            using (var args = api.API.AllocEventArguments(api, null, null))
            {
                return GetRunValue(api, args);
            }
        }
    }
    [Desc("注释", "[基础]/注释")]
    public class CommentValue : AbstractValue
    {
        [Desc("注释")] public string Comment = "注释";
        public override Type BaseType => typeof(object);
        public override object GetRunValue(EventExecutor api, IEventArguments args)
        {
            return null;
        }
        protected override void GetText(EventStringBuilder sw)
        {
            //"<![CDATA[]]>"
            sw.AppendFormat("<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Comment);
        }
    }


    [Desc("抽象值")]
    [Expandable]
    public abstract class AbstractValue<T> : AbstractValue
    {
        sealed public override Type BaseType { get => typeof(AbstractValue<T>); }
        sealed public override object GetRunValue(EventExecutor api, IEventArguments args)
        {
            return GetValueAs(api, args);
        }
        public V GetValueAs<V>(EventExecutor api, IEventArguments args)
        {
            var t = GetValueAs(api, args);
            return CUtils.ConvertTo<V>(t);
        }
        public T GetValueAs(EventExecutor api, IEventArguments args)
        {
            if (EventExecutor.ENABLE_TRACE) api.Trace(this);
            return GetValue(api, args);
        }
        protected abstract T GetValue(EventExecutor api, IEventArguments args);
    }


    //     [Desc("抽象值")]
    //     [Expandable]
    //     public abstract class AbstractArrayValue : AbstractValue
    //     {
    //         sealed public override object GetRunValue(EventExecutor api, IEventArguments args)
    //         {
    //             return GetArrayValue(api, args);
    //         }
    //         public Array GetRunArrayValue(EventExecutor api, IEventArguments args)
    //         {
    //             return GetArrayValue(api, args); ;
    //         }
    //         abstract protected Array GetArrayValue(EventExecutor api, IEventArguments args);
    //         //         sealed public override object GetEnvValue(EventExecutor api)
    //         //         {
    //         //             return GetArrayValue(api, api.CreateEventArguments(null, null));
    //         //         }
    //         //         sealed public override object GetEnvValue(EventExecutor api, IEventArguments args)
    //         //         {
    //         //             return GetArrayValue(api, args);
    //         //         }
    //     }
    // 
    //     [Desc("抽象值")]
    //     [Expandable]
    //     public abstract class AbstractArrayValue<T> : AbstractArrayValue
    //     {
    //         sealed public override Type BaseType { get => typeof(AbstractArrayValue<T>); }
    //         sealed protected override Array GetArrayValue(EventExecutor api, IEventArguments args)
    //         {
    //             return this.GetValueAs(api, args);
    //         }
    //         public T[] GetValueAs(EventExecutor api, IEventArguments args)
    //         {
    //             api.Trace(this);
    //             return GetValue(api, args);
    //         }
    //         abstract protected T[] GetValue(EventExecutor api, IEventArguments args);
    //     
    //     }

}
