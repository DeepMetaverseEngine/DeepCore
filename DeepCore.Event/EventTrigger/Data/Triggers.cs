using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using static DeepCore.Colors;

namespace DeepCore.EventTrigger.Data
{

    [Desc("事件开端")]
    [Expandable]
    public abstract class AbstractTrigger : EventExternalizable
    {
        static AbstractTrigger()
        {
            XmlSerializer.AddDefaultConverter(((XmlSerializer ser, XmlElement dataElement, Type decleardType, Exception err, out object data, object root) =>
            {
                if (typeof(AbstractTrigger).IsAssignableFrom(decleardType))
                {
                    data = new CommentTrigger()
                    {
                        Comment = err.Message + "\n" + XmlUtil.ToXmlString(dataElement),
                    };
                    return true;
                }
                data = null;
                return false;
            }));
        }

        sealed public override Type BaseType { get => typeof(AbstractTrigger); }
        public object StartListen(EventExecutor api, IEventArguments args)
        {
            args.Listener = this;
            Listen(api, args);
            return null;
        }
        abstract protected void Listen(EventExecutor api, IEventArguments args);
        protected virtual void Disposing(EventExecutor api) { }

        internal void InvokeTrigging(EventExecutor api, IEventArguments args, IList<TriggingHandler> actions)
        {
            if (EventExecutor.ENABLE_TRACE)
            {
                api.BeginTrace();
                api.Trace(this);
            }
            //OnTrigging?.Invoke(api, args);
            for (int i = 0; i < actions.Count; i++)
            {
                actions[i]?.Invoke(api, args);
            }
        }
        internal void InvokeDispose(EventExecutor api)
        {
            //OnTrigging = null;
            Disposing(api);
        }

        //public event TriggingHandler OnTrigging;
    }

    public delegate object TriggingHandler(EventExecutor api, IEventArguments args);


    [Desc("注释", "[基础]/注释")]
    public class CommentTrigger : AbstractTrigger
    {
        [Desc("注释")]
        public string Comment = "注释";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Comment);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
        }
    }

    //     [Desc("触发执行器")]
    //     public class TriggerAction : EventExternalizable
    //     {
    //         [Desc("触发器")]
    //         public AbstractTrigger Trigger;
    //         [Desc("执行器")]
    //         public AbstractAction Executor;
    //     }
}
